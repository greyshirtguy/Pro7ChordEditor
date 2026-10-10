#include "songimport.h"

#include "proconvert.h"
#include "workspacefiles.h"

#include <QDateTime>
#include <QDir>
#include <QFile>
#include <QFileInfo>
#include <QStringDecoder>

namespace songimport {

chords::Song readFile(const QString &path, QString *error)
{
    QFile file(path);
    if (!file.open(QIODevice::ReadOnly)) {
        *error = QStringLiteral("Cannot read %1: %2").arg(QFileInfo(path).fileName(), file.errorString());
        return {};
    }
    // Such a file is a few kilobytes of text. Anything large is something else.
    if (file.size() > 2 * 1024 * 1024) {
        *error = QStringLiteral("%1 is too large to be a song").arg(QFileInfo(path).fileName());
        return {};
    }
    const QByteArray bytes = file.readAll();
    // UTF-8 as nearly all are, with or without a mark at the start; a file that is
    // not (an old one, typed on Windows) is read as Latin-1, which at least keeps
    // every accented letter of western Europe.
    QStringDecoder utf8(QStringDecoder::Utf8);
    QString text = utf8(bytes);
    if (utf8.hasError())
        text = QString::fromLatin1(bytes);
    const chords::Song song = chords::parseSong(text);
    if (song.sections.isEmpty()) {
        *error = QStringLiteral("%1 has no words in it").arg(QFileInfo(path).fileName());
        return {};
    }
    return song;
}

rv::data::Presentation build(const chords::Song &song, const QString &name, int linesPerSlide)
{
    rv::data::Presentation presentation;
    auto *info = presentation.mutable_application_info();
    info->set_application(rv::data::ApplicationInfo::APPLICATION_PROPRESENTER);
    info->mutable_application_version()->set_major_version(7);
    info->mutable_application_version()->set_minor_version(16);
    info->mutable_application_version()->set_patch_version(2);
    presentation.mutable_uuid()->set_string(workspace::newUuid());
    presentation.set_name(name.toStdString());
    presentation.mutable_last_modified_date()->set_seconds(QDateTime::currentSecsSinceEpoch());
    presentation.mutable_background();
    presentation.mutable_chord_chart();

    if (!song.title.isEmpty() || !song.artist.isEmpty() || !song.copyright.isEmpty() || !song.ccli.isEmpty()) {
        auto *ccli = presentation.mutable_ccli();
        ccli->set_song_title(song.title.toStdString());
        ccli->set_artist_credits(song.artist.toStdString());
        ccli->set_publisher(song.copyright.toStdString());
        bool isNumber = false;
        const uint number = song.ccli.toUInt(&isNumber);
        if (isNumber)
            ccli->set_song_number(number);
    }
    if (chords::keyNumber(song.key) >= 0) {
        using Scale = rv::data::MusicKeyScale;
        for (Scale *target : {presentation.mutable_music()->mutable_original(), presentation.mutable_music()->mutable_user()}) {
            target->set_music_key(Scale::MusicKey(chords::keyNumber(song.key)));
            target->set_music_scale(chords::keyIsMinor(song.key) ? Scale::MUSIC_SCALE_MINOR : Scale::MUSIC_SCALE_MAJOR);
        }
    }

    const QSizeF size(1920, 1080);
    for (const chords::Section &section : song.sections) {
        // A part with no name in the file is a verse like any other to whoever runs
        // the words; but naming it one would be a guess, so it is left unnamed, in a
        // group with no name, which shows as plain slides.
        auto *group = presentation.add_cue_groups();
        group->mutable_group()->mutable_uuid()->set_string(workspace::newUuid());
        group->mutable_group()->set_name(section.name.toStdString());
        group->mutable_group()->mutable_hotkey();

        const QList<QList<chords::Line>> slides = chords::slidesOf(section, linesPerSlide);
        for (const QList<chords::Line> &lines : slides) {
            rv::data::Cue *cue = proconvert::addBlankCue(&presentation, std::string(), size);
            group->add_cue_identifiers()->set_string(cue->uuid().string());
            rv::data::Slide *slide = cue->mutable_actions(0)->mutable_slide()->mutable_presentation()->mutable_base_slide();

            // The text box: the whole slide but for a margin, named as ProPresenter
            // names the words of a song, which is the name a stage layout or a theme
            // looks for.
            rv::data::Slide::Element made = proconvert::makeTextElement(*slide, nullptr);
            rv::data::Graphics::Element *element = made.mutable_element();
            element->set_name("Lyrics");
            element->mutable_bounds()->mutable_origin()->set_x(96);
            element->mutable_bounds()->mutable_origin()->set_y(54);
            element->mutable_bounds()->mutable_size()->set_width(size.width() - 192);
            element->mutable_bounds()->mutable_size()->set_height(size.height() - 108);

            QStringList words;
            QList<chords::Chord> chordList;
            int start = 0;
            for (const chords::Line &line : lines) {
                words << line.text;
                for (const chords::Chord &chord : line.chords)
                    chordList.append({start + chord.at, chord.name});
                start += int(line.text.size()) + 1;
            }
            rv::data::Graphics::Text *text = element->mutable_text();
            const RichText model = proconvert::readText(*text);
            TextRun format = model.firstRun();
            format.size = 84;
            proconvert::writeText(text, RichText::plain(words.join(u'\n'), format, Qt::AlignHCenter));
            proconvert::writeChords(text, chordList);
            *slide->add_elements() = made;
        }
    }
    return presentation;
}

QString importFile(const QString &file, const QString &library, int linesPerSlide, QString *made)
{
    QString error;
    const chords::Song song = readFile(file, &error);
    if (!error.isEmpty())
        return error;
    if (!QFileInfo(library).isDir())
        return QStringLiteral("There is no library to put it in");

    // A file's name cannot have a slash in it, and one starting with a dot is hidden.
    QString name = (song.title.isEmpty() ? QFileInfo(file).completeBaseName() : song.title).simplified();
    name.replace(u'/', u'-');
    while (name.startsWith(u'.'))
        name.remove(0, 1);
    if (name.isEmpty())
        name = QStringLiteral("Song");
    const QDir folder(library);
    QString unique = name;
    for (int n = 2; folder.exists(unique + QStringLiteral(".pro")); ++n)
        unique = QStringLiteral("%1 %2").arg(name).arg(n);

    const QString path = folder.filePath(unique + QStringLiteral(".pro"));
    error = proconvert::writePresentation(path, build(song, unique, linesPerSlide));
    if (error.isEmpty())
        *made = path;
    return error;
}

}
