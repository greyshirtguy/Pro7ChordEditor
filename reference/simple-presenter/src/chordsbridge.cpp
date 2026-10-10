#include "chordsbridge.h"

#include "chords.h"
#include "songimport.h"

#include <QRegularExpression>
#include <QTextCharFormat>

namespace {

QList<chords::Chord> fromList(const QVariantList &list)
{
    QList<chords::Chord> result;
    for (const QVariant &entry : list) {
        const QVariantMap chord = entry.toMap();
        result.append({chord.value("at").toInt(), chord.value("name").toString()});
    }
    return result;
}

}

QStringList Chords::majorKeys() const
{
    return chords::majorKeys();
}

QStringList Chords::minorKeys() const
{
    return chords::minorKeys();
}

bool Chords::couldBecome(const QString &typed) const
{
    return chords::couldBecome(typed);
}

bool Chords::isChord(const QString &typed) const
{
    return chords::parts(typed).valid && chords::couldBecome(typed);
}

QString Chords::tidied(const QString &typed) const
{
    return chords::tidied(typed);
}

QStringList Chords::diatonic(const QString &key) const
{
    return chords::diatonic(key);
}

QStringList Chords::completions(const QString &typed, const QString &key, const QStringList &used) const
{
    return chords::completions(typed, key, used);
}

QString Chords::shown(const QString &chord, const QString &originalKey, const QString &key, int notation) const
{
    return chords::shown(chord, originalKey, key, notation);
}

QString Chords::toChordPro(const QString &text, const QVariantList &chordList) const
{
    return chords::toChordPro(text, fromList(chordList));
}

QVariantList Chords::chordsOf(const QString &chordPro) const
{
    QVariantList result;
    const QList<chords::Chord> read = chords::chordsOf(chordPro);
    for (const chords::Chord &chord : read)
        result.append(QVariantMap {{"at", chord.at}, {"name", chord.name}});
    return result;
}

QString Chords::withoutChords(const QString &chordPro) const
{
    return chords::withoutChords(chordPro);
}

bool Chords::isPlaceholders(const QString &line) const
{
    return chords::isPlaceholders(line);
}

QVariantMap Chords::describeFile(const QString &path) const
{
    QString error;
    const chords::Song song = songimport::readFile(path, &error);
    QVariantList sections;
    int chordCount = 0;
    for (const chords::Section &section : song.sections) {
        sections.append(QVariantMap {{"name", section.name}, {"lines", int(section.lines.size())}});
        for (const chords::Line &line : section.lines)
            chordCount += int(line.chords.size());
    }
    return {
        {"title", song.title},
        {"artist", song.artist},
        {"key", song.key},
        {"sections", sections},
        {"chords", chordCount},
        {"error", error},
    };
}

namespace {

class Highlighter : public QSyntaxHighlighter
{
public:
    using QSyntaxHighlighter::QSyntaxHighlighter;

protected:
    void highlightBlock(const QString &text) override
    {
        static const QRegularExpression chord(QStringLiteral("\\[[^\\[\\]]*\\]"));
        if (text.startsWith(u'{')) {
            QTextCharFormat fixed;
            fixed.setForeground(QColor(0x8d, 0x90, 0x97));
            setFormat(0, int(text.size()), fixed);
            return;
        }
        QTextCharFormat format;
        format.setForeground(QColor(0xff, 0xb4, 0x5e));
        format.setFontWeight(QFont::Bold);
        auto found = chord.globalMatch(text);
        while (found.hasNext()) {
            const auto match = found.next();
            setFormat(int(match.capturedStart()), int(match.capturedLength()), format);
        }
    }
};

}

void ChordHighlighter::setDocument(QQuickTextDocument *document)
{
    if (document == m_document)
        return;
    delete m_highlighter;
    m_highlighter = nullptr;
    m_document = document;
    if (document && document->textDocument())
        m_highlighter = new Highlighter(document->textDocument());
    emit documentChanged();
}
