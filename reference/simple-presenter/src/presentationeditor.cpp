#include "presentationeditor.h"

#include "chords.h"

#include "proconvert.h"
#include "richtext.h"
#include "workspacefiles.h"

#include <QDateTime>
#include <QDir>
#include <QFile>
#include <QFileInfo>
#include <QSet>
#include <QStandardPaths>

namespace {

// How many copies of a presentation from before editing sessions are kept.
const int backupsKept = 20;
// How many changes can be undone.
const int undoLimit = 200;

rv::data::Slide::Element *findElement(rv::data::Slide *slide, const QString &id)
{
    const std::string wanted = id.toStdString();
    for (rv::data::Slide::Element &element : *slide->mutable_elements()) {
        if (element.element().uuid().string() == wanted)
            return &element;
    }
    return nullptr;
}

int indexOfElement(const rv::data::Slide &slide, const QString &id)
{
    const std::string wanted = id.toStdString();
    for (int i = 0; i < slide.elements_size(); ++i) {
        if (slide.elements(i).element().uuid().string() == wanted)
            return i;
    }
    return -1;
}

// The first slide a cue shows, as an index into its actions, or -1: a presentation's
// slide, or with `prop` a prop's.
int slideAction(const rv::data::Cue &cue, bool prop = false)
{
    for (int i = 0; i < cue.actions_size(); ++i) {
        const rv::data::Action &action = cue.actions(i);
        if (prop ? action.has_slide() && action.slide().has_prop()
                 : action.isenabled() && action.has_slide() && action.slide().has_presentation())
            return i;
    }
    return -1;
}

const QString gone = QStringLiteral("That is no longer in the file.");
const QString elementGone = QStringLiteral("That element is no longer on the slide.");

} // namespace

PresentationEditor::PresentationEditor(QObject *parent)
    : QAbstractListModel(parent)
{
}

int PresentationEditor::rowCount(const QModelIndex &parent) const
{
    return parent.isValid() ? 0 : int(m_rows.size());
}

QVariant PresentationEditor::data(const QModelIndex &index, int role) const
{
    if (role != SlideRole || index.row() < 0 || index.row() >= m_slides.size())
        return {};
    return m_slides.at(index.row());
}

QHash<int, QByteArray> PresentationEditor::roleNames() const
{
    return {{SlideRole, "slide"}};
}

QString PresentationEditor::kind() const
{
    return m_kind == Kind::Props ? QStringLiteral("props")
         : m_kind == Kind::Stage ? QStringLiteral("stage")
         : m_kind == Kind::Theme ? QStringLiteral("theme") : QStringLiteral("presentation");
}

// Opening goes in three steps: the file is read by whichever of the three is being
// opened, start() clears what was open, the rows are listed, and finishOpening()
// describes them.
void PresentationEditor::start(Kind kind, const QString &path, const QString &workspace, const QString &name)
{
    beginResetModel();
    m_kind = kind;
    m_path = path;
    m_workspace = workspace;
    m_name = name;
    m_undo.clear();
    m_redo.clear();
    m_changed = false;
    m_backupPath.clear();
    m_previewRow = -1;
    m_previewBefore.clear();
    m_rows.clear();
    if (kind != Kind::Presentation)
        m_presentation.Clear();
    if (kind != Kind::Props)
        m_props.Clear();
    if (kind != Kind::Stage)
        m_stage.Clear();
    if (kind != Kind::Theme)
        m_theme.Clear();
}

void PresentationEditor::finishOpening()
{
    m_slides.clear();
    for (int row = 0; row < m_rows.size(); ++row)
        m_slides.append(describe(row));
    endResetModel();

    emit documentChanged();
    emit historyChanged();
    emit keyChanged();
}

QString PresentationEditor::open(const QString &path, const QString &workspace)
{
    rv::data::Presentation presentation;
    QString error;
    if (!proconvert::readPresentation(path, &presentation, &error))
        return error;

    start(Kind::Presentation, path, workspace, QFileInfo(path).completeBaseName());
    m_presentation = presentation;

    // Every cue that shows a slide, group by group as the presentation stores them,
    // then any cue that no group has.
    QHash<std::string, int> cueIndexes;
    for (int i = 0; i < m_presentation.cues_size(); ++i)
        cueIndexes.insert(m_presentation.cues(i).uuid().string(), i);
    QSet<int> placed;
    const auto place = [this, &placed](int cue, const QString &group, const QString &color, bool start) {
        const rv::data::Cue &candidate = m_presentation.cues(cue);
        if (placed.contains(cue) || !candidate.isenabled() || slideAction(candidate) < 0)
            return false;
        placed.insert(cue);
        m_rows.append({cue, group, color, start});
        return true;
    };
    for (const auto &group : m_presentation.cue_groups()) {
        const rv::data::Color &color = group.group().color();
        const QString colorName = group.group().has_color() && color.alpha() > 0 ? proconvert::toColor(color).name()
                                                                                  : QString();
        bool first = true;
        for (const rv::data::UUID &id : group.cue_identifiers()) {
            const auto cue = cueIndexes.constFind(id.string());
            if (cue != cueIndexes.constEnd() && place(*cue, QString::fromStdString(group.group().name()), colorName, first))
                first = false;
        }
    }
    for (int i = 0; i < m_presentation.cues_size(); ++i)
        place(i, QString(), QString(), false);

    finishOpening();
    return {};
}

QString PresentationEditor::openProps(const QString &path, const QString &workspace)
{
    rv::data::PropDocument props;
    const QString error = workspace::readMessage(path, &props, QStringLiteral("the props"));
    if (!error.isEmpty())
        return error;

    start(Kind::Props, path, workspace, QStringLiteral("Props"));
    m_props = props;

    // Every cue that is a prop: collection by collection, as they are listed, then any
    // that no collection has.
    QHash<std::string, int> cueIndexes;
    for (int i = 0; i < m_props.cues_size(); ++i)
        cueIndexes.insert(m_props.cues(i).uuid().string(), i);
    QSet<int> placed;
    const auto place = [this, &placed](int cue) {
        if (placed.contains(cue) || slideAction(m_props.cues(cue), true) < 0)
            return;
        placed.insert(cue);
        m_rows.append({cue, QString(), QString(), false});
    };
    for (const auto &collection : m_props.prop_collections()) {
        for (const auto &item : collection.items()) {
            const auto cue = cueIndexes.constFind(item.prop_cue_uuid().string());
            if (cue != cueIndexes.constEnd())
                place(*cue);
        }
    }
    for (int i = 0; i < m_props.cues_size(); ++i)
        place(i);

    finishOpening();
    return {};
}

QString PresentationEditor::openStageLayouts(const QString &path, const QString &workspace)
{
    rv::data::Stage::Document stage;
    const QString error = workspace::readMessage(path, &stage, QStringLiteral("the stage layouts"));
    if (!error.isEmpty())
        return error;

    start(Kind::Stage, path, workspace, QStringLiteral("Stage Layouts"));
    m_stage = stage;
    for (int i = 0; i < m_stage.layouts_size(); ++i)
        m_rows.append({i, QString(), QString(), false});

    finishOpening();
    return {};
}

// A theme is slides in a file of its own, each with a name (see themefile.h).
QString PresentationEditor::openTheme(const QString &path, const QString &workspace)
{
    rv::data::Template::Document theme;
    const QString error = workspace::readMessage(path, &theme, QStringLiteral("the theme"));
    if (!error.isEmpty())
        return error;
    start(Kind::Theme, path, workspace, QFileInfo(path).dir().dirName());
    m_theme = theme;
    for (int i = 0; i < m_theme.slides_size(); ++i)
        m_rows.append({i, QString(), QString(), false});
    finishOpening();
    return {};
}

void PresentationEditor::close()
{
    beginResetModel();
    m_presentation.Clear();
    m_props.Clear();
    m_stage.Clear();
    m_theme.Clear();
    m_kind = Kind::Presentation;
    m_path.clear();
    m_name.clear();
    m_rows.clear();
    m_slides.clear();
    m_undo.clear();
    m_redo.clear();
    m_changed = false;
    m_backupPath.clear();
    m_previewRow = -1;
    m_previewBefore.clear();
    endResetModel();
    emit documentChanged();
    emit historyChanged();
}

QVariantMap PresentationEditor::slideAt(int row) const
{
    return row >= 0 && row < m_slides.size() ? m_slides.at(row).toMap() : QVariantMap();
}

int PresentationEditor::rowOf(const QString &slideId) const
{
    const std::string wanted = slideId.toStdString();
    for (int row = 0; row < m_rows.size(); ++row) {
        const int unit = m_rows.at(row).unit;
        const std::string &id = m_kind == Kind::Stage ? m_stage.layouts(unit).uuid().string()
                              : m_kind == Kind::Theme ? m_theme.slides(unit).base_slide().uuid().string()
                              : m_kind == Kind::Props ? m_props.cues(unit).uuid().string()
                                                      : m_presentation.cues(unit).uuid().string();
        if (id == wanted)
            return row;
    }
    return -1;
}

// What a row's slide is in: the cue, or the stage layout. It is what is kept of the
// file, before and after, for a change to be undone by.
google::protobuf::Message *PresentationEditor::unitAt(int row)
{
    if (row < 0 || row >= m_rows.size())
        return nullptr;
    const int unit = m_rows.at(row).unit;
    switch (m_kind) {
    case Kind::Props: return m_props.mutable_cues(unit);
    case Kind::Stage: return m_stage.mutable_layouts(unit);
    case Kind::Theme: return m_theme.mutable_slides(unit);
    default: return m_presentation.mutable_cues(unit);
    }
}

const google::protobuf::Message *PresentationEditor::unitAt(int row) const
{
    return const_cast<PresentationEditor *>(this)->unitAt(row);
}

rv::data::Slide *PresentationEditor::slideIn(int row)
{
    if (row < 0 || row >= m_rows.size())
        return nullptr;
    const int unit = m_rows.at(row).unit;
    if (m_kind == Kind::Stage)
        return m_stage.mutable_layouts(unit)->mutable_slide();
    if (m_kind == Kind::Theme)
        return m_theme.mutable_slides(unit)->mutable_base_slide();
    // A change to the cue undone or redone may have altered its actions.
    rv::data::Cue *cue = m_kind == Kind::Props ? m_props.mutable_cues(unit) : m_presentation.mutable_cues(unit);
    const int action = slideAction(*cue, m_kind == Kind::Props);
    if (action < 0)
        return nullptr;
    rv::data::Action::SlideType *slide = cue->mutable_actions(action)->mutable_slide();
    return m_kind == Kind::Props ? slide->mutable_prop()->mutable_base_slide()
                                 : slide->mutable_presentation()->mutable_base_slide();
}

QString PresentationEditor::write()
{
    switch (m_kind) {
    case Kind::Props: return workspace::writeMessage(m_path, m_props, QStringLiteral("the props"));
    case Kind::Stage: return workspace::writeMessage(m_path, m_stage, QStringLiteral("the stage layouts"));
    case Kind::Theme: return workspace::writeMessage(m_path, m_theme, QStringLiteral("the theme"));
    default: return proconvert::writePresentation(m_path, m_presentation);
    }
}

QVariantMap PresentationEditor::describe(int row) const
{
    const Row &place = m_rows.at(row);
    QVariantMap slide;
    if (m_kind == Kind::Stage) {
        const rv::data::Stage::Layout &layout = m_stage.layouts(place.unit);
        slide = proconvert::toSlideMap(layout.slide(), QString::fromStdString(layout.name()));
        slide.insert("id", QString::fromStdString(layout.uuid().string()));
    } else if (m_kind == Kind::Theme) {
        const rv::data::Template::Slide &themed = m_theme.slides(place.unit);
        slide = proconvert::toSlideMap(themed.base_slide(), QString::fromStdString(themed.name()));
        slide.insert("id", QString::fromStdString(themed.base_slide().uuid().string()));
    } else {
        const bool prop = m_kind == Kind::Props;
        const rv::data::Cue &cue = prop ? m_props.cues(place.unit) : m_presentation.cues(place.unit);
        const int index = slideAction(cue, prop);
        if (index < 0)
            return {};
        const rv::data::Action &action = cue.actions(index);
        // A prop goes by its cue's name, a slide by the label of its action.
        slide = prop ? proconvert::toSlideMap(action.slide().prop().base_slide(), QString::fromStdString(cue.name()))
                     : proconvert::toSlideMap(action.slide().presentation().base_slide(),
                                              QString::fromStdString(action.label().text()));
        slide.insert("id", QString::fromStdString(cue.uuid().string()));
    }
    slide.insert("group", place.group);
    slide.insert("groupColor", place.groupColor);
    slide.insert("groupStart", place.groupStart);
    return slide;
}

void PresentationEditor::refresh(int row)
{
    if (row < 0 || row >= m_slides.size())
        return;
    m_slides[row] = describe(row);
    emit dataChanged(index(row), index(row), {SlideRole});
    emit slideChanged(row);
}

// The file as it was before this session's first change goes into a folder of the app's
// own, under the workspace's name and the file's place in it, with the time in its
// name. Only so many are kept for each presentation.
void PresentationEditor::backUp()
{
    if (m_changed || !m_backupPath.isEmpty())
        return;
    const QFileInfo file(m_path);
    QString place = file.dir().dirName();
    if (!m_workspace.isEmpty()) {
        const QString relative = QDir(m_workspace).relativeFilePath(file.absolutePath());
        if (!relative.startsWith(QLatin1String("..")))
            place = QFileInfo(m_workspace).fileName() + u'/' + relative;
    }
    const QDir directory(QStandardPaths::writableLocation(QStandardPaths::AppLocalDataLocation)
                         + QStringLiteral("/Edit Backups/") + place);
    if (!directory.mkpath(QStringLiteral("."))) {
        qWarning("Cannot create %s", qPrintable(directory.path()));
        return;
    }
    // Under the file's own name and ending, which the props and the stage layouts do
    // not have one of.
    const QString base = file.completeBaseName();
    const QString ending = file.suffix().isEmpty() ? QString() : u'.' + file.suffix();
    const QString target = directory.filePath(base + QDateTime::currentDateTime().toString(QStringLiteral(" yyyy-MM-dd HH.mm.ss"))
                                              + ending);
    if (!QFile::exists(target) && !QFile::copy(m_path, target)) {
        qWarning("Cannot copy %s to %s", qPrintable(m_path), qPrintable(target));
        return;
    }
    m_backupPath = target;

    // Their names sort by time, so the oldest come first. The pattern is in pieces only
    // because "??-", written whole, is a trigraph, which the compiler remarks on.
    const QStringList copies = directory.entryList({base + QStringLiteral(" ????" "-??" "-?? ??.??.??") + ending}, QDir::Files, QDir::Name);
    for (qsizetype i = 0; i < copies.size() - backupsKept; ++i)
        QFile::remove(directory.filePath(copies.at(i)));
}

// Saves the cue or layout of a row as it now is, as one change from how it was
// `before`. If the file cannot be written, it goes back to how it was.
QString PresentationEditor::commit(int row, const std::string &before)
{
    google::protobuf::Message *unit = unitAt(row);
    if (!unit)
        return gone;
    const std::string after = unit->SerializeAsString();
    if (after == before) {
        refresh(row);
        return {};
    }
    backUp();
    const QString error = write();
    if (!error.isEmpty()) {
        unit->ParseFromString(before);
        refresh(row);
        return error;
    }
    m_changed = true;
    m_undo.append({row, before, after, m_joinNext && !m_undo.isEmpty()});
    if (m_undo.size() > undoLimit)
        m_undo.removeFirst();
    m_redo.clear();
    refresh(row);
    emit historyChanged();
    return {};
}

// Makes a change to the slide in a row and saves it. `apply` is given the slide and
// returns an error message, empty if it made the change.
template <typename Change>
QString PresentationEditor::change(int row, Change apply)
{
    // A preview left under way becomes a change of its own first.
    if (m_previewRow >= 0)
        commitPreview();
    google::protobuf::Message *unit = unitAt(row);
    rv::data::Slide *slide = slideIn(row);
    if (!unit || !slide)
        return gone;
    const std::string before = unit->SerializeAsString();
    const QString error = apply(slide);
    if (!error.isEmpty()) {
        unit->ParseFromString(before);
        return error;
    }
    return commit(row, before);
}

template <typename Change>
void PresentationEditor::preview(int row, Change apply)
{
    if (m_previewRow >= 0 && m_previewRow != row)
        commitPreview();
    google::protobuf::Message *unit = unitAt(row);
    rv::data::Slide *slide = slideIn(row);
    if (!unit || !slide)
        return;
    if (m_previewRow < 0) {
        m_previewRow = row;
        m_previewBefore = unit->SerializeAsString();
    }
    apply(slide);
    refresh(row);
}

QString PresentationEditor::commitPreview()
{
    if (m_previewRow < 0)
        return {};
    const int row = m_previewRow;
    const std::string before = m_previewBefore;
    m_previewRow = -1;
    m_previewBefore.clear();
    return commit(row, before);
}

void PresentationEditor::cancelPreview()
{
    if (m_previewRow < 0)
        return;
    const int row = m_previewRow;
    if (google::protobuf::Message *unit = unitAt(row))
        unit->ParseFromString(m_previewBefore);
    m_previewRow = -1;
    m_previewBefore.clear();
    refresh(row);
}

QString PresentationEditor::setProperties(int row, const QString &element, const QVariantMap &changes)
{
    return change(row, [&](rv::data::Slide *slide) {
        return proconvert::applyChanges(slide, element, changes) ? QString() : elementGone;
    });
}

void PresentationEditor::previewProperties(int row, const QString &element, const QVariantMap &changes)
{
    preview(row, [&](rv::data::Slide *slide) { proconvert::applyChanges(slide, element, changes); });
}

QString PresentationEditor::setText(int row, const QString &element, const QVariant &richText)
{
    return change(row, [&](rv::data::Slide *slide) {
        rv::data::Slide::Element *target = findElement(slide, element);
        if (!target)
            return elementGone;
        proconvert::writeText(target->mutable_element()->mutable_text(), richText.value<RichText>());
        return QString();
    });
}

QString PresentationEditor::formatText(int row, const QString &element, int start, int end, const QVariantMap &format)
{
    return change(row, [&](rv::data::Slide *slide) {
        rv::data::Slide::Element *target = findElement(slide, element);
        if (!target)
            return elementGone;
        rv::data::Graphics::Text *text = target->mutable_element()->mutable_text();
        proconvert::writeText(text, proconvert::readText(*text).formatted(start, end, format));
        return QString();
    });
}

void PresentationEditor::previewFormat(int row, const QString &element, int start, int end, const QVariantMap &format)
{
    preview(row, [&](rv::data::Slide *slide) {
        if (rv::data::Slide::Element *target = findElement(slide, element)) {
            rv::data::Graphics::Text *text = target->mutable_element()->mutable_text();
            proconvert::writeText(text, proconvert::readText(*text).formatted(start, end, format));
        }
    });
}

QVariantMap PresentationEditor::addText(int row, const QString &like)
{
    QString id;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        // Set like the element asked for, or failing that like the slide's first text.
        const rv::data::Slide::Element *model = findElement(slide, like);
        for (int i = 0; !model && i < slide->elements_size(); ++i) {
            if (!slide->elements(i).element().text().rtf_data().empty())
                model = &slide->elements(i);
        }
        const rv::data::Slide::Element added = proconvert::makeTextElement(*slide, model);
        id = QString::fromStdString(added.element().uuid().string());
        *slide->add_elements() = added;
        return QString();
    });
    return {{"id", error.isEmpty() ? id : QString()}, {"error", error}};
}

QVariantMap PresentationEditor::addShape(int row, const QString &shape)
{
    QString id;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        const rv::data::Slide::Element added = proconvert::makeShapeElement(*slide, shape);
        id = QString::fromStdString(added.element().uuid().string());
        *slide->add_elements() = added;
        return QString();
    });
    return {{"id", error.isEmpty() ? id : QString()}, {"error", error}};
}

QVariantMap PresentationEditor::addMedia(int row, const QUrl &file)
{
    const QString path = file.toLocalFile();
    if (!workspace::isMedia(path) || !QFileInfo(path).isFile())
        return {{"id", QString()}, {"error", QStringLiteral("That is not an image or a video this can show")}};
    QString id;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        const rv::data::Slide::Element added = proconvert::makeMediaElement(*slide, path);
        id = QString::fromStdString(added.element().uuid().string());
        *slide->add_elements() = added;
        return QString();
    });
    return {{"id", error.isEmpty() ? id : QString()}, {"error", error}};
}

QVariantMap PresentationEditor::duplicate(int row, const QString &element)
{
    QString id;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        const int index = indexOfElement(*slide, element);
        if (index < 0)
            return elementGone;
        rv::data::Slide::Element copy = slide->elements(index);
        rv::data::Graphics::Element *graphics = copy.mutable_element();
        graphics->mutable_uuid()->set_string(workspace::newUuid());
        graphics->set_name(proconvert::uniqueElementName(*slide, QString::fromStdString(graphics->name())).toStdString());
        graphics->mutable_bounds()->mutable_origin()->set_x(graphics->bounds().origin().x() + 30);
        graphics->mutable_bounds()->mutable_origin()->set_y(graphics->bounds().origin().y() + 30);
        // Builds belong to the element they were made for.
        copy.clear_build_in();
        copy.clear_build_out();
        copy.clear_childbuilds();
        id = QString::fromStdString(graphics->uuid().string());
        *slide->add_elements() = copy;
        return QString();
    });
    return {{"id", error.isEmpty() ? id : QString()}, {"error", error}};
}

QString PresentationEditor::remove(int row, const QString &element)
{
    return change(row, [&](rv::data::Slide *slide) {
        const int index = indexOfElement(*slide, element);
        if (index < 0)
            return elementGone;
        slide->mutable_elements()->DeleteSubrange(index, 1);
        // It has no place in the order builds run in either.
        const std::string id = element.toStdString();
        for (int i = slide->element_build_order_size() - 1; i >= 0; --i) {
            if (slide->element_build_order(i).string() == id)
                slide->mutable_element_build_order()->DeleteSubrange(i, 1);
        }
        return QString();
    });
}

QString PresentationEditor::move(int row, const QString &element, int index)
{
    return change(row, [&](rv::data::Slide *slide) {
        int from = indexOfElement(*slide, element);
        if (from < 0)
            return elementGone;
        const int to = qBound(0, index, slide->elements_size() - 1);
        // Step by step, each a swap with its neighbour.
        for (; from < to; ++from)
            slide->mutable_elements()->SwapElements(from, from + 1);
        for (; from > to; --from)
            slide->mutable_elements()->SwapElements(from, from - 1);
        return QString();
    });
}

// Takes the last step off one list, puts its cue or layout as it was at the other end
// of that step, saves, and puts the step on the other list.
QString PresentationEditor::restore(QList<Step> *from, QList<Step> *to, bool forwards)
{
    if (m_previewRow >= 0)
        commitPreview();
    if (from->isEmpty())
        return {};
    const Step step = from->last();
    google::protobuf::Message *unit = unitAt(step.row);
    if (!unit)
        return gone;
    unit->ParseFromString(forwards ? step.after : step.before);
    const QString error = write();
    if (!error.isEmpty()) {
        unit->ParseFromString(forwards ? step.before : step.after);
        return error;
    }
    from->removeLast();
    to->append(step);
    refresh(step.row);
    emit historyChanged();
    emit restored(step.row);
    // A step joined to the one before it goes with it: back, the one before follows;
    // forwards, the one that was joined to this follows.
    if (!from->isEmpty() && (forwards ? from->last().joined : step.joined))
        return restore(from, to, forwards);
    return {};
}

QString PresentationEditor::key() const
{
    if (m_kind != Kind::Presentation || !m_presentation.has_music())
        return {};
    const rv::data::MusicKeyScale &original = m_presentation.music().original();
    return chords::keyName(int(original.music_key()), original.music_scale() == rv::data::MusicKeyScale::MUSIC_SCALE_MINOR);
}

QString PresentationEditor::setKey(const QString &key)
{
    using Scale = rv::data::MusicKeyScale;
    const int number = chords::keyNumber(key);
    if (m_kind != Kind::Presentation || number < 0)
        return QStringLiteral("%1 is not a key").arg(key);
    if (key == this->key())
        return {};
    const rv::data::Presentation::Music before = m_presentation.music();
    const bool had = m_presentation.has_music();
    // The key the chords are shown in goes with it: until someone shows the song in
    // another key, that is the key it is in.
    for (Scale *target : {m_presentation.mutable_music()->mutable_original(), m_presentation.mutable_music()->mutable_user()}) {
        target->set_music_key(Scale::MusicKey(number));
        target->set_music_scale(chords::keyIsMinor(key) ? Scale::MUSIC_SCALE_MINOR : Scale::MUSIC_SCALE_MAJOR);
    }
    backUp();
    const QString error = write();
    if (!error.isEmpty()) {
        if (had)
            *m_presentation.mutable_music() = before;
        else
            m_presentation.clear_music();
        return error;
    }
    m_changed = true;
    emit historyChanged();
    emit keyChanged();
    return {};
}

QVariantList PresentationEditor::song() const
{
    QVariantList blocks;
    if (m_kind != Kind::Presentation)
        return blocks;
    for (int row = 0; row < m_slides.size(); ++row) {
        const QVariantMap slide = m_slides.at(row).toMap();
        const QVariantList elements = slide.value("elements").toList();
        for (const QVariant &entry : elements) {
            const QVariantMap element = entry.toMap();
            // Words of its own: not another element's, a timer's or the live slide's
            if (element.value("linkKind").toString() != QLatin1String("none"))
                continue;
            const QString text = element.value("text").value<RichText>().plainText();
            // A box with no words is left out, unless it is a line of chords alone: it
            // has chords, or the characters Multitracks hangs them on. (A box with
            // only a stray space in it, which files are full of, is neither.)
            if (text.trimmed().isEmpty() && !element.contains("chords") && !text.contains(QChar(0x200B)) && !text.contains(QChar(0x2001)))
                continue;
            blocks.append(QVariantMap {
                {"row", row},
                {"slideId", slide.value("id")},
                {"group", slide.value("group")},
                {"groupColor", slide.value("groupColor")},
                {"groupStart", slide.value("groupStart")},
                {"label", slide.value("label")},
                {"element", element.value("id")},
                {"text", text},
                {"chords", element.value("chords").toList()},
            });
        }
    }
    return blocks;
}

QStringList PresentationEditor::usedChords() const
{
    QStringList used;
    const QVariantList blocks = song();
    for (const QVariant &block : blocks) {
        const QVariantList chordList = block.toMap().value("chords").toList();
        for (const QVariant &chord : chordList)
            used << chord.toMap().value("name").toString();
    }
    return used;
}

QString PresentationEditor::setChords(int row, const QString &element, const QVariantList &chordList, bool joined)
{
    QList<chords::Chord> wanted;
    for (const QVariant &entry : chordList) {
        const QVariantMap chord = entry.toMap();
        wanted.append({chord.value("at").toInt(), chords::tidied(chord.value("name").toString())});
    }
    m_joinNext = joined;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        rv::data::Slide::Element *target = findElement(slide, element);
        if (!target)
            return elementGone;
        proconvert::writeChords(target->mutable_element()->mutable_text(), wanted);
        return QString();
    });
    m_joinNext = false;
    return error;
}

QString PresentationEditor::setChordsAlone(int row, const QString &element, int line, const QStringList &names, bool joined)
{
    m_joinNext = joined;
    const QString error = change(row, [&](rv::data::Slide *slide) {
        rv::data::Slide::Element *target = findElement(slide, element);
        if (!target)
            return elementGone;
        rv::data::Graphics::Text *text = target->mutable_element()->mutable_text();
        RichText rich = proconvert::readText(*text);
        // The line is a paragraph, or part of one that has line breaks in it. Its
        // words become the stand-ins, in the format the paragraph starts in.
        // In the character the line hangs its chords on now, if it has one.
        const QStringList linesNow = rich.plainText().split(u'\n');
        const bool spaces = line < linesNow.size() && !linesNow.at(line).contains(QChar(0x200B)) && linesNow.at(line).contains(u' ');
        const QString standIns = chords::placeholders(int(names.size()), spaces ? QChar(u' ') : QChar(0x200B));
        int first = 0;
        int start = 0;
        bool found = false;
        for (TextParagraph &paragraph : rich.paragraphs) {
            QString whole;
            for (const TextRun &run : std::as_const(paragraph.runs))
                whole += run.text;
            QStringList lines = whole.split(QChar::LineSeparator);
            if (line < first + lines.size()) {
                for (int i = 0; i < line - first; ++i)
                    start += int(lines.at(i).size()) + 1;
                lines[line - first] = standIns;
                TextRun run = paragraph.runs.isEmpty() ? rich.firstRun() : paragraph.runs.first();
                run.text = lines.join(QChar::LineSeparator);
                paragraph.runs = {run};
                found = true;
                break;
            }
            first += int(lines.size());
            start += int(whole.size()) + 1;
        }
        if (!found)
            return QStringLiteral("The text has no such line");
        // The chords of the other lines keep their characters; those after this line
        // move by as much as it has grown or shrunk. This line's are put on its new
        // stand-ins, every other character (see chords::placeholders).
        QList<chords::Chord> kept;
        const QStringList beforeLines = proconvert::readText(*text).plainText().split(u'\n');
        const int oldLength = line < beforeLines.size() ? int(beforeLines.at(line).size()) : 0;
        const int shift = int(standIns.size()) - oldLength;
        const QList<chords::Chord> had = proconvert::readChords(*text);
        for (const chords::Chord &chord : had) {
            if (chord.at < start)
                kept.append(chord);
            else if (chord.at > start + oldLength)
                kept.append({chord.at + shift, chord.name});
        }
        proconvert::writeText(text, rich);
        for (int i = 0; i < names.size(); ++i)
            kept.append({start + i * 2, chords::tidied(names.at(i))});
        proconvert::writeChords(text, kept);
        return QString();
    });
    m_joinNext = false;
    return error;
}

QString PresentationEditor::undo()
{
    return restore(&m_undo, &m_redo, false);
}

QString PresentationEditor::redo()
{
    return restore(&m_redo, &m_undo, true);
}
