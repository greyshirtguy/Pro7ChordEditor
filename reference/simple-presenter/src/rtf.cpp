#include "rtf.h"

#include "fontresolver.h"

#include <QStringDecoder>

namespace {

// Windows-1252 differs from Latin-1 only in 0x80..0x9F.
const char16_t cp1252High[32] = {
    0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
    0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
    0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
    0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178,
};

QChar fromCp1252(uchar byte)
{
    if (byte >= 0x80 && byte <= 0x9F)
        return QChar(cp1252High[byte - 0x80]);
    return QChar(byte);
}

bool isAsciiLetter(char c)
{
    return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}

enum class Destination { Body, FontTable, ColorTable, ExpandedColorTable, Skip };

struct State
{
    Destination destination = Destination::Body;
    int font = -1;
    int halfPoints = 24;
    bool bold = false;
    bool italic = false;
    int fillColor = 0;
    int strokeColor = 0;
    int strokeWidth = 0; // Cocoa: percent of font size, times 20; negative means stroke and fill
    bool underline = false;
    bool strikethrough = false;
    int expand = 0; // twentieths of a point
    int superscript = 0;
    Qt::Alignment alignment = Qt::AlignLeft;
    int lineHeight = 0;
    bool lineHeightIsMultiple = false;
    int unicodeSkip = 1;
    bool starred = false;
};

class Parser
{
public:
    Parser(const QByteArray &rtf, const RtfDefaults &defaults)
        : m_rtf(rtf), m_defaults(defaults)
    {
    }

    RichText parse()
    {
        while (m_pos < m_rtf.size()) {
            const char c = m_rtf.at(m_pos++);
            switch (c) {
            case '{':
                m_stack.append(m_state);
                m_state.starred = false;
                m_pendingSkip = 0;
                break;
            case '}':
                endEntry(true);
                if (!m_stack.isEmpty())
                    m_state = m_stack.takeLast();
                m_pendingSkip = 0;
                break;
            case '\\':
                control();
                break;
            case '\r':
            case '\n':
                break;
            default:
                if (uchar(c) < 0x80) {
                    character(QChar(uchar(c)));
                } else {
                    --m_pos;
                    rawHighBytes();
                }
                break;
            }
        }
        // Text after the last paragraph break is a paragraph too, even when there is
        // none: a document that ends with a break ends with an empty line. It takes the
        // format of the paragraph before it, the groups that set it having closed by now.
        if (m_paragraph.runs.isEmpty() && !m_result.paragraphs.isEmpty()) {
            const TextParagraph &before = m_result.paragraphs.last();
            m_paragraph = before;
            m_paragraph.runs = {before.runs.last()};
            m_paragraph.runs.first().text.clear();
        }
        if (!m_paragraph.runs.isEmpty())
            m_result.paragraphs.append(m_paragraph);
        return m_result;
    }

private:
    // Strict RTF escapes everything above ASCII, but files written by other tools embed
    // raw UTF-8. Take a run of high bytes as UTF-8 if it is valid, else as the code page.
    void rawHighBytes()
    {
        const qsizetype start = m_pos;
        while (m_pos < m_rtf.size() && uchar(m_rtf.at(m_pos)) >= 0x80)
            ++m_pos;
        const QByteArrayView bytes = QByteArrayView(m_rtf).sliced(start, m_pos - start);

        QStringDecoder utf8(QStringDecoder::Utf8, QStringDecoder::Flag::Stateless);
        const QString decoded = utf8.decode(bytes);
        if (!utf8.hasError()) {
            for (const QChar c : decoded)
                character(c);
        } else {
            for (const char byte : bytes)
                character(fromCp1252(uchar(byte)));
        }
    }

    void control()
    {
        if (m_pos >= m_rtf.size())
            return;
        const char c = m_rtf.at(m_pos);

        if (!isAsciiLetter(c)) {
            ++m_pos;
            switch (c) {
            case '\'': {
                const uchar byte = uchar(m_rtf.mid(m_pos, 2).toUInt(nullptr, 16));
                m_pos += 2;
                character(fromCp1252(byte));
                break;
            }
            case '*':
                m_state.starred = true;
                break;
            case '\n':
            case '\r':
                // Cocoa writes a paragraph break as a backslash at the end of the line.
                if (m_state.destination == Destination::Body)
                    endParagraph();
                break;
            case '~':
                character(QChar(0x00A0));
                break;
            case '_':
                character(QChar(0x2011));
                break;
            case '-':
                break;
            default:
                character(QChar(uchar(c)));
                break;
            }
            return;
        }

        const qsizetype wordStart = m_pos;
        while (m_pos < m_rtf.size() && isAsciiLetter(m_rtf.at(m_pos)))
            ++m_pos;
        const QByteArray word = m_rtf.mid(wordStart, m_pos - wordStart);

        const qsizetype paramStart = m_pos;
        if (m_pos < m_rtf.size() && m_rtf.at(m_pos) == '-')
            ++m_pos;
        while (m_pos < m_rtf.size() && m_rtf.at(m_pos) >= '0' && m_rtf.at(m_pos) <= '9')
            ++m_pos;
        const bool hasParam = m_pos > paramStart;
        const int param = hasParam ? m_rtf.mid(paramStart, m_pos - paramStart).toInt() : 0;
        if (m_pos < m_rtf.size() && m_rtf.at(m_pos) == ' ')
            ++m_pos;

        controlWord(word, hasParam, param);
    }

    void controlWord(const QByteArray &word, bool hasParam, int param)
    {
        // The first control word after "{\*" names an optional destination.
        if (m_state.starred) {
            m_state.starred = false;
            m_state.destination = word == "expandedcolortbl" ? Destination::ExpandedColorTable
                                                             : Destination::Skip;
            return;
        }

        switch (m_state.destination) {
        case Destination::Skip:
            return;
        case Destination::FontTable:
            if (word == "f")
                m_entryFont = param;
            return;
        case Destination::ColorTable:
            if (word == "red")
                m_entryColor.setRed(param);
            else if (word == "green")
                m_entryColor.setGreen(param);
            else if (word == "blue")
                m_entryColor.setBlue(param);
            else
                return;
            m_entryHasColor = true;
            return;
        case Destination::ExpandedColorTable:
            if (word == "c")
                m_entryComponents.append(param / 100000.0);
            else if (word.startsWith("cs"))
                m_entryColorSpace = word;
            return;
        case Destination::Body:
            break;
        }

        const bool on = !hasParam || param != 0;
        if (word == "fonttbl") {
            m_state.destination = Destination::FontTable;
        } else if (word == "colortbl") {
            m_state.destination = Destination::ColorTable;
        } else if (word == "info" || word == "stylesheet" || word == "pict" || word == "header"
                   || word == "footer" || word == "listtable" || word == "listoverridetable") {
            m_state.destination = Destination::Skip;
        } else if (word == "par") {
            endParagraph();
        } else if (word == "line") {
            character(QChar(QChar::LineSeparator));
        } else if (word == "tab") {
            character(u'\t');
        } else if (word == "u") {
            const int skip = m_state.unicodeSkip;
            character(QChar(char16_t(param < 0 ? param + 65536 : param)));
            m_pendingSkip = skip;
        } else if (word == "uc") {
            m_state.unicodeSkip = param;
        } else if (word == "pard") {
            m_state.alignment = Qt::AlignLeft;
            m_state.lineHeight = 0;
            m_state.lineHeightIsMultiple = false;
        } else if (word == "plain") {
            const State fresh;
            m_state.font = fresh.font;
            m_state.halfPoints = fresh.halfPoints;
            m_state.bold = m_state.italic = m_state.underline = m_state.strikethrough = false;
            m_state.fillColor = m_state.strokeColor = m_state.strokeWidth = 0;
            m_state.expand = m_state.superscript = 0;
        } else if (word == "ql") {
            m_state.alignment = Qt::AlignLeft;
        } else if (word == "qj") {
            m_state.alignment = Qt::AlignJustify;
        } else if (word == "sl") {
            m_state.lineHeight = param;
        } else if (word == "slmult") {
            m_state.lineHeightIsMultiple = param != 0;
        } else if (word == "ulnone") {
            m_state.underline = false;
        } else if (word == "ul" || word == "uld" || word == "uldb" || word == "ulth" || word == "ulw") {
            m_state.underline = on;
        } else if (word == "strike" || word == "striked") {
            m_state.strikethrough = on;
        } else if (word == "expndtw") {
            m_state.expand = param;
        } else if (word == "super") {
            m_state.superscript = on ? 1 : 0;
        } else if (word == "sub") {
            m_state.superscript = on ? -1 : 0;
        } else if (word == "nosupersub") {
            m_state.superscript = 0;
        } else if (word == "qc") {
            m_state.alignment = Qt::AlignHCenter;
        } else if (word == "qr") {
            m_state.alignment = Qt::AlignRight;
        } else if (word == "f") {
            m_state.font = param;
        } else if (word == "fs") {
            m_state.halfPoints = param;
        } else if (word == "b") {
            m_state.bold = on;
        } else if (word == "i") {
            m_state.italic = on;
        } else if (word == "cf") {
            m_state.fillColor = param;
        } else if (word == "strokec") {
            m_state.strokeColor = param;
        } else if (word == "strokewidth") {
            m_state.strokeWidth = param;
        }
    }

    void character(QChar c)
    {
        if (m_pendingSkip > 0) {
            --m_pendingSkip;
            return;
        }
        switch (m_state.destination) {
        case Destination::Skip:
            break;
        case Destination::FontTable:
            if (c == u';')
                endEntry(false);
            else
                m_entryText += c;
            break;
        case Destination::ColorTable:
        case Destination::ExpandedColorTable:
            if (c == u';')
                endEntry(false);
            break;
        case Destination::Body:
            appendText(c);
            break;
        }
    }

    // Tables are lists of entries terminated by ';'. A group closing also ends an entry
    // in the font table, where each entry may sit in its own group.
    void endEntry(bool groupClosed)
    {
        switch (m_state.destination) {
        case Destination::FontTable:
            if (m_entryFont >= 0 && !m_entryText.trimmed().isEmpty())
                m_fonts.insert(m_entryFont, m_entryText.trimmed());
            if (!groupClosed || !m_entryText.trimmed().isEmpty())
                m_entryFont = -1;
            m_entryText.clear();
            break;
        case Destination::ColorTable:
            if (groupClosed)
                break;
            m_colors.append(m_entryHasColor ? m_entryColor : QColor());
            m_entryColor = QColor(0, 0, 0);
            m_entryHasColor = false;
            break;
        case Destination::ExpandedColorTable: {
            if (groupClosed)
                break;
            // More precise than the 8-bit colour table, and the only place alpha is stored.
            const QList<qreal> &c = m_entryComponents;
            QColor color;
            if (m_entryColorSpace == "csgray" && !c.isEmpty())
                color = QColor::fromRgbF(c[0], c[0], c[0], c.size() > 1 ? c[1] : 1.0);
            else if (!m_entryColorSpace.isEmpty() && c.size() >= 3)
                color = QColor::fromRgbF(c[0], c[1], c[2], c.size() > 3 ? c[3] : 1.0);
            if (color.isValid() && m_expandedIndex < m_colors.size())
                m_colors[m_expandedIndex] = color;
            ++m_expandedIndex;
            m_entryComponents.clear();
            m_entryColorSpace.clear();
            break;
        }
        default:
            break;
        }
    }

    TextRun currentRun() const
    {
        TextRun run;
        const QString fontName = m_fonts.value(m_state.font);
        const ResolvedFont font = resolvePostScriptName(fontName, m_defaults.familyForPostScriptName.value(fontName));
        run.fontName = fontName;
        run.family = font.family;
        run.bold = font.bold || m_state.bold;
        run.italic = font.italic || m_state.italic;
        run.size = m_state.halfPoints / 2.0;
        run.underline = m_state.underline;
        run.strikethrough = m_state.strikethrough;
        run.kerning = m_state.expand / 20.0;
        run.superscript = m_state.superscript;

        const QColor fill = m_colors.value(m_state.fillColor);
        run.fill = fill.isValid() ? fill : m_defaults.textColor;
        if (m_state.strokeWidth != 0) {
            const QColor stroke = m_colors.value(m_state.strokeColor);
            run.stroke = stroke.isValid() ? stroke : run.fill;
            run.strokeWidth = qAbs(m_state.strokeWidth) / 20.0 / 100.0 * run.size;
            run.fillVisible = m_state.strokeWidth < 0;
        }
        return run;
    }

    void setParagraphFormat()
    {
        m_paragraph.alignment = m_state.alignment;
        m_paragraph.lineHeight = m_state.lineHeight;
        m_paragraph.lineHeightIsMultiple = m_state.lineHeightIsMultiple;
    }

    void appendText(QChar c)
    {
        TextRun run = currentRun();
        setParagraphFormat();
        if (!m_paragraph.runs.isEmpty() && m_paragraph.runs.last().sameFormat(run)) {
            m_paragraph.runs.last().text += c;
        } else {
            run.text = c;
            m_paragraph.runs.append(run);
        }
    }

    void endParagraph()
    {
        if (m_paragraph.runs.isEmpty()) {
            setParagraphFormat();
            m_paragraph.runs.append(currentRun());
        }
        m_result.paragraphs.append(m_paragraph);
        m_paragraph = TextParagraph();
    }

    const QByteArray m_rtf;
    const RtfDefaults m_defaults;
    qsizetype m_pos = 0;
    State m_state;
    QList<State> m_stack;
    int m_pendingSkip = 0;

    QHash<int, QString> m_fonts;
    QList<QColor> m_colors;

    int m_entryFont = -1;
    QString m_entryText;
    QColor m_entryColor = QColor(0, 0, 0);
    bool m_entryHasColor = false;
    QList<qreal> m_entryComponents;
    QByteArray m_entryColorSpace;
    qsizetype m_expandedIndex = 0;

    RichText m_result;
    TextParagraph m_paragraph;
};

} // namespace

RichText parseRtf(const QByteArray &rtf, const RtfDefaults &defaults)
{
    return Parser(rtf, defaults).parse();
}
