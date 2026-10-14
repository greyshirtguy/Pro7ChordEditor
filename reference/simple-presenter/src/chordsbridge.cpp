#include "chordsbridge.h"

#include "chords.h"
#include "sheethints.h"
#include "songimport.h"

#include <QClipboard>
#include <QFontMetricsF>
#include <QGuiApplication>
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

QString Chords::withSigns(const QString &name) const
{
    return chords::withSigns(name);
}

QString Chords::withoutSigns(const QString &text) const
{
    return chords::withoutSigns(text);
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

QString Chords::copiedText() const
{
    const QClipboard *clipboard = QGuiApplication::clipboard();
    return clipboard ? clipboard->text() : QString();
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

// ---- The line of hints

namespace {

sheethints::Hint hintOf(const QVariant &value)
{
    const QVariantMap map = value.toMap();
    return {map.value("lead").toString(), map.value("keys").toStringList(), map.value("says").toString(), map.value("needed").toBool()};
}

}

QVariantMap SheetHints::sizes() const
{
    return {
        {"wordSize", sheethints::WordSize}, {"keySize", sheethints::KeySize}, {"between", sheethints::Between},
        {"within", sheethints::Within}, {"lines", sheethints::Lines}, {"height", sheethints::Height},
        {"sides", sheethints::Sides}, {"keyHeight", sheethints::KeyHeight}, {"keyLeast", sheethints::KeyLeast},
        {"keyPad", sheethints::KeyPad},
    };
}

QVariantList SheetHints::hints(const QString &doing, bool onChord, bool empty, bool fromStrip, bool ctrl, int pasteLines, bool replaces,
                               bool overStrip) const
{
    using sheethints::Doing;
    const Doing what = doing == u"typing" ? Doing::Typing : doing == u"carrying" ? Doing::Carrying : doing == u"onChord" ? Doing::OnChord
                     : doing == u"onPlace" ? Doing::OnPlace : Doing::Nothing;
    QVariantList list;
    for (const sheethints::Hint &hint : sheethints::hints({what, onChord, empty, fromStrip}, ctrl, {pasteLines, replaces}, overStrip)) {
        // (The keys as a list of plain values, which QML has as an array like any other.)
        QVariantList keys;
        for (const QString &key : hint.keys)
            keys.append(key);
        list.append(QVariantMap {{"lead", hint.lead}, {"keys", keys}, {"says", hint.says}, {"needed", hint.needed}});
    }
    return list;
}

QVariantMap SheetHints::fit(const QVariantList &hintList, double width) const
{
    QList<sheethints::Hint> hints;
    for (const QVariant &value : hintList)
        hints.append(hintOf(value));
    const auto measure = [this](const QString &words, int pixels, bool onAKey) { return measured(words, pixels, onAKey); };
    const sheethints::Fit fitted = sheethints::fit(hints, std::max(0.0, width - 2 * sheethints::Sides), measure);
    return {{"count", fitted.count}, {"stacked", fitted.stacked}, {"size", fitted.size}, {"height", sheethints::heightOf(fitted)}};
}

// How wide some words are in the app's font, as a Text of QML draws them by default: at
// that size, and on a key a little bolder. A few runs of words come round again and
// again as the pointer crosses the sheet, so each is measured once.
double SheetHints::measured(const QString &words, int pixels, bool onAKey) const
{
    const QString key = QString::number(pixels) + (onAKey ? u'k' : u'w') + words;
    const auto found = m_widths.constFind(key);
    if (found != m_widths.constEnd())
        return found.value();
    QFont font = QGuiApplication::font();
    font.setPixelSize(pixels);
    font.setWeight(onAKey ? QFont::DemiBold : QFont::Normal);
    const double width = QFontMetricsF(font).horizontalAdvance(words);
    m_widths.insert(key, width);
    return width;
}

namespace {

class Highlighter : public QSyntaxHighlighter
{
public:
    using QSyntaxHighlighter::QSyntaxHighlighter;

    // The colour chords are given
    QColor color = QColor(0xff, 0xb4, 0x5e);

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
        format.setForeground(color);
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
    if (document && document->textDocument()) {
        Highlighter *highlighter = new Highlighter(document->textDocument());
        highlighter->color = m_color;
        m_highlighter = highlighter;
    }
    emit documentChanged();
}

// Another colour for the chords: the text is coloured again, and nothing of it changed.
void ChordHighlighter::setColor(const QColor &color)
{
    if (color == m_color)
        return;
    m_color = color;
    if (m_highlighter) {
        static_cast<Highlighter *>(m_highlighter)->color = color;
        m_highlighter->rehighlight();
    }
    emit colorChanged();
}
