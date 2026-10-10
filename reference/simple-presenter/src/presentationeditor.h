#pragma once

#include "presentation.pb.h"
#include "propDocument.pb.h"
#include "stage.pb.h"
#include "template.pb.h"

#include <QAbstractListModel>
#include <QUrl>
#include <QVariantMap>
#include <QtQml/qqmlregistration.h>

#include <string>

// A presentation open for editing. The file is held as ProPresenter wrote it, parsed but
// otherwise untouched; a change alters only the fields it is about, so everything this
// app does not understand goes back to the file as it came.
//
// As a list model it has a row for each slide, in the order the presentation stores
// them (its arrangements only reorder and repeat these), each with one role, `slide`:
// the slide as a map, as proconvert describes it, with its `id`, `group`, `groupColor`
// and `groupStart` as ProDocument describes them.
//
// A change is saved to the file as soon as it is made, and can be undone. Before the
// first change the file is copied aside, so the presentation as it was before an
// editing session can always be got back.
//
// Two other files of a workspace are made of slides, and are edited as a presentation
// is: its props, each a slide that is laid over the output, and its stage layouts, each
// a slide of text boxes linked to what is live (see Props and StageLayouts). Open on
// one of those, the rows are the props or the layouts, each row's `id` being the prop's
// or the layout's and its label the name. What differs is only where in the file a
// row's slide is, and what is kept of the file to undo a change: the cue the slide is
// in, or the layout.
class PresentationEditor : public QAbstractListModel
{
    Q_OBJECT
    QML_ELEMENT
    Q_PROPERTY(QString path READ path NOTIFY documentChanged)
    Q_PROPERTY(QString name READ name NOTIFY documentChanged)
    // What is open: "presentation", "props" or "stage"
    Q_PROPERTY(QString kind READ kind NOTIFY documentChanged)
    Q_PROPERTY(int count READ count NOTIFY documentChanged)
    Q_PROPERTY(bool canUndo READ canUndo NOTIFY historyChanged)
    Q_PROPERTY(bool canRedo READ canRedo NOTIFY historyChanged)
    // Whether the file has been changed since it was opened here.
    Q_PROPERTY(bool changed READ changed NOTIFY historyChanged)
    // Where the copy made before the first change went, or empty if there is none yet.
    Q_PROPERTY(QString backupPath READ backupPath NOTIFY historyChanged)
    // The key the presentation's chords are written in, as chords.h names keys; "" for
    // a presentation that names none (and for props, layouts and themes).
    Q_PROPERTY(QString key READ key NOTIFY keyChanged)

public:
    enum Roles { SlideRole = Qt::UserRole + 1 };

    explicit PresentationEditor(QObject *parent = nullptr);

    int rowCount(const QModelIndex &parent = {}) const override;
    QVariant data(const QModelIndex &index, int role) const override;
    QHash<int, QByteArray> roleNames() const override;

    QString path() const { return m_path; }
    QString name() const { return m_name; }
    QString kind() const;
    int count() const { return int(m_rows.size()); }
    bool canUndo() const { return !m_undo.isEmpty(); }
    bool canRedo() const { return !m_redo.isEmpty(); }
    bool changed() const { return m_changed; }
    QString backupPath() const { return m_backupPath; }
    QString key() const;

    // Opens a presentation file from the workspace folder `workspace`, in place of any
    // open already. Returns an error message, empty on success.
    Q_INVOKABLE QString open(const QString &path, const QString &workspace);
    // Opens the workspace's props, or its stage layouts, in the same way: `path` is the
    // file they are in.
    Q_INVOKABLE QString openProps(const QString &path, const QString &workspace);
    Q_INVOKABLE QString openStageLayouts(const QString &path, const QString &workspace);
    // A theme: `path` is its Theme file, and its slides are the rows.
    Q_INVOKABLE QString openTheme(const QString &path, const QString &workspace);
    Q_INVOKABLE void close();

    Q_INVOKABLE QVariantMap slideAt(int row) const;
    // The row of the slide with this id, or -1.
    Q_INVOKABLE int rowOf(const QString &slideId) const;

    // Changes to the elements of the slide in a row. Each is saved at once and returns
    // an error message, empty on success; on an error nothing has changed. `changes`
    // is as proconvert::applyChanges takes.
    Q_INVOKABLE QString setProperties(int row, const QString &element, const QVariantMap &changes);
    // Replaces an element's text with a RichText value.
    Q_INVOKABLE QString setText(int row, const QString &element, const QVariant &richText);
    // Applies a format to part of an element's text: see RichText::formatted. To format
    // all of it, give a range that covers it, such as 0 to a very large number.
    Q_INVOKABLE QString formatText(int row, const QString &element, int start, int end, const QVariantMap &format);
    // Adds a text box, its text set like that of the element `like` if there is one.
    // Returns { id, error }.
    Q_INVOKABLE QVariantMap addText(int row, const QString &like);
    // Adds a shape ("rectangle", "roundedRectangle", "ellipse" or "arrow"), or an
    // element filled with a picture or video file. Each returns { id, error }.
    Q_INVOKABLE QVariantMap addShape(int row, const QString &shape);
    Q_INVOKABLE QVariantMap addMedia(int row, const QUrl &file);
    // Adds a copy of an element, a little down and right of it. Returns { id, error }.
    Q_INVOKABLE QVariantMap duplicate(int row, const QString &element);
    Q_INVOKABLE QString remove(int row, const QString &element);
    // Moves an element to another place in the order they are drawn in, 0 being the
    // back.
    Q_INVOKABLE QString move(int row, const QString &element, int index);

    // ---- Chords (see chords.h, and proconvert::readChords for how they are kept)

    // The presentation as one song: every text box that has words of its own, slide
    // after slide, as { row, slideId, group, groupColor, groupStart, label, element,
    // text, chords: [{ at, name }] }. It is what the two chord editors lay out as one
    // sheet, there being no such thing in the file, where each slide has its own words.
    Q_INVOKABLE QVariantList song() const;
    // Every chord in the presentation, as often as it comes up: what the chord editor
    // offers first.
    Q_INVOKABLE QStringList usedChords() const;
    // Replaces the chords of a text box. With `joined` it is undone together with the
    // change made just before it (a chord dragged from one slide to another is taken
    // from the one and given to the other, and is one thing to undo).
    Q_INVOKABLE QString setChords(int row, const QString &element, const QVariantList &chords, bool joined = false);
    // Replaces one line of a text box, a line of chords with no words, by these chords:
    // the line's stand-in characters are made anew, one for each (see
    // chords::placeholders). It is the one change the chord editors make to the words.
    Q_INVOKABLE QString setChordsAlone(int row, const QString &element, int line, const QStringList &names, bool joined = false);
    // Says what key the chords are written in. Not a change to any slide, so not one
    // that undo takes back.
    Q_INVOKABLE QString setKey(const QString &key);

    // The same changes as setProperties and formatText, shown but neither saved nor
    // undoable yet, for following a slider or a colour being dragged. Any number may
    // follow one another; commitPreview() then saves the outcome as one change, and
    // cancelPreview() puts things back as they were.
    Q_INVOKABLE void previewProperties(int row, const QString &element, const QVariantMap &changes);
    Q_INVOKABLE void previewFormat(int row, const QString &element, int start, int end, const QVariantMap &format);
    Q_INVOKABLE QString commitPreview();
    Q_INVOKABLE void cancelPreview();

    // Each returns an error message, empty on success.
    Q_INVOKABLE QString undo();
    Q_INVOKABLE QString redo();

signals:
    void documentChanged();
    void historyChanged();
    void keyChanged();
    // The slide in this row is not as it was.
    void slideChanged(int row);
    // An undo or a redo changed the slide in this row.
    void restored(int row);

private:
    enum class Kind { Presentation, Props, Stage, Theme };
    // Where a row's slide is: which cue of the presentation or of the props, or which
    // stage layout; and for a presentation's slide, its group.
    struct Row
    {
        int unit;
        QString group;
        QString groupColor;
        bool groupStart;
    };
    // One change: the cue or layout it was made to, as it was before and after.
    struct Step
    {
        int row;
        std::string before;
        std::string after;
        // Undone and redone together with the step before it
        bool joined = false;
    };

    void start(Kind kind, const QString &path, const QString &workspace, const QString &name);
    void finishOpening();
    google::protobuf::Message *unitAt(int row);
    const google::protobuf::Message *unitAt(int row) const;
    rv::data::Slide *slideIn(int row);
    QString write();
    QVariantMap describe(int row) const;
    void refresh(int row);
    template <typename Change>
    QString change(int row, Change apply);
    template <typename Change>
    void preview(int row, Change apply);
    QString commit(int row, const std::string &before);
    QString restore(QList<Step> *from, QList<Step> *to, bool forwards);
    void backUp();

    Kind m_kind = Kind::Presentation;
    // The file, as whichever of the three it is
    rv::data::Presentation m_presentation;
    rv::data::PropDocument m_props;
    rv::data::Stage::Document m_stage;
    rv::data::Template::Document m_theme;
    QString m_path;
    QString m_name;
    QString m_workspace;
    QList<Row> m_rows;
    QVariantList m_slides;
    QList<Step> m_undo;
    QList<Step> m_redo;
    bool m_changed = false;
    QString m_backupPath;
    // The row a preview is under way in, or -1, and its cue as it was before.
    // Whether the change being made is joined to the one before it (see Step)
    bool m_joinNext = false;
    int m_previewRow = -1;
    std::string m_previewBefore;
};
