#include "rtfwriter.h"

#include "fontresolver.h"

#include <QStringList>

namespace {

// Windows-1252's 0x80..0x9F, the bytes that differ from Latin-1.
const char16_t cp1252High[32] = {
    0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
    0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
    0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
    0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178,
};

QByteArray hexByte(int byte)
{
    return "\\'" + QByteArray::number(byte, 16).rightJustified(2, '0');
}

// Text as RTF: its own syntax escaped, the characters of the document's code page as
// \'hh, and the rest as \u with no fallback character.
QByteArray escaped(const QString &text)
{
    QByteArray out;
    for (const QChar c : text) {
        const char16_t u = c.unicode();
        if (u == u'\\' || u == u'{' || u == u'}') {
            out += '\\';
            out += char(u);
        } else if (u == u'\t') {
            out += "\\tab ";
        } else if (u >= 0x20 && u < 0x80) {
            out += char(u);
        } else if (u >= 0xA0 && u <= 0xFF) {
            out += hexByte(u);
        } else {
            int high = -1;
            for (int i = 0; i < 32; ++i) {
                if (cp1252High[i] == u && u > 0xFF)
                    high = 0x80 + i;
            }
            out += high >= 0 ? hexByte(high) : "\\uc0\\u" + QByteArray::number(int(u)) + ' ';
        }
    }
    return out;
}

QByteArray alignmentWord(Qt::Alignment alignment)
{
    if (alignment & Qt::AlignHCenter)
        return "\\qc";
    if (alignment & Qt::AlignRight)
        return "\\qr";
    if (alignment & Qt::AlignJustify)
        return "\\qj";
    return "\\ql";
}

// Components of the expanded colour table are parts in 100000.
QByteArray component(float value)
{
    return "\\c" + QByteArray::number(qRound(value * 100000));
}

} // namespace

QByteArray writeRtf(const RichText &text)
{
    QStringList fonts;
    QList<QColor> colors;
    const auto fontIndex = [&fonts](const TextRun &run) {
        const QString name = run.fontName.isEmpty() ? postScriptNameFor(run.family, run.bold, run.italic) : run.fontName;
        qsizetype index = fonts.indexOf(name);
        if (index < 0) {
            index = fonts.size();
            fonts.append(name);
        }
        return int(index);
    };
    // Colour 0 is "the default colour", so table entries count from 1.
    const auto colorIndex = [&colors](const QColor &color) {
        qsizetype index = colors.indexOf(color);
        if (index < 0) {
            index = colors.size();
            colors.append(color);
        }
        return int(index) + 1;
    };

    QByteArray body;
    const TextParagraph *previousParagraph = nullptr;
    // The character formatting in force, as far as the reader of the RTF is concerned.
    int font = -1, halfPoints = -1, fill = -1, stroke = -1, strokeWidth = 0, expand = 0, superscript = 0;
    bool bold = false, italic = false, underline = false, strikethrough = false;

    for (const TextParagraph &paragraph : text.paragraphs) {
        if (previousParagraph)
            body += "\\\n";
        // Paragraph formatting carries over from one paragraph to the next until changed.
        if (!previousParagraph || previousParagraph->alignment != paragraph.alignment
            || previousParagraph->lineHeight != paragraph.lineHeight
            || previousParagraph->lineHeightIsMultiple != paragraph.lineHeightIsMultiple) {
            body += "\\pard\\pardeftab1680";
            if (paragraph.lineHeight != 0) {
                body += "\\sl" + QByteArray::number(paragraph.lineHeight);
                body += paragraph.lineHeightIsMultiple ? "\\slmult1" : "\\slmult0";
            }
            body += "\\pardirnatural" + alignmentWord(paragraph.alignment) + "\\partightenfactor0\n\n";
        }
        previousParagraph = &paragraph;

        for (const TextRun &run : paragraph.runs) {
            QByteArray words;
            const int runFont = fontIndex(run);
            if (runFont != font)
                words += "\\f" + QByteArray::number(font = runFont);
            if (run.bold != bold)
                words += (bold = run.bold) ? "\\b" : "\\b0";
            if (run.italic != italic)
                words += (italic = run.italic) ? "\\i" : "\\i0";
            const int runHalfPoints = qRound(run.size * 2);
            if (runHalfPoints != halfPoints)
                words += "\\fs" + QByteArray::number(halfPoints = runHalfPoints);
            const int runFill = colorIndex(run.fill);
            if (runFill != fill)
                words += "\\cf" + QByteArray::number(fill = runFill);
            if (run.underline != underline)
                words += (underline = run.underline) ? "\\ul" : "\\ulnone";
            if (run.strikethrough != strikethrough)
                words += (strikethrough = run.strikethrough) ? "\\strike" : "\\strike0";
            // Kerning in twentieths of a point, and again, as Cocoa does, in quarter points.
            const int runExpand = qRound(run.kerning * 20);
            if (runExpand != expand) {
                expand = runExpand;
                words += "\\kerning1\\expnd" + QByteArray::number(qRound(run.kerning * 4))
                       + "\\expndtw" + QByteArray::number(expand);
            }
            if (run.superscript != superscript) {
                superscript = run.superscript;
                words += superscript > 0 ? "\\super" : superscript < 0 ? "\\sub" : "\\nosupersub";
            }
            // Cocoa's stroke width is a percentage of the font size, times twenty, and
            // negative when the glyphs are filled as well as stroked.
            int runStrokeWidth = 0;
            if (run.strokeWidth > 0 && run.size > 0) {
                runStrokeWidth = qMax(1, qRound(run.strokeWidth / run.size * 100 * 20));
                if (run.fillVisible)
                    runStrokeWidth = -runStrokeWidth;
            }
            const int runStroke = runStrokeWidth != 0 ? colorIndex(run.stroke) : stroke;
            if (runStrokeWidth != strokeWidth || runStroke != stroke) {
                strokeWidth = runStrokeWidth;
                stroke = runStroke;
                words += "\\outl0\\strokewidth" + QByteArray::number(strokeWidth);
                if (strokeWidth != 0)
                    words += "\\strokec" + QByteArray::number(stroke);
            }
            if (!words.isEmpty())
                body += words + ' ';
            body += escaped(run.text);
        }
    }

    QByteArray rtf = "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2761\n\\cocoatextscaling0\\cocoaplatform0{\\fonttbl";
    for (qsizetype i = 0; i < fonts.size(); ++i)
        rtf += "\\f" + QByteArray::number(i) + "\\fnil\\fcharset0 " + escaped(fonts.at(i)) + ';';
    rtf += "}\n{\\colortbl;";
    for (const QColor &color : std::as_const(colors)) {
        rtf += "\\red" + QByteArray::number(color.red()) + "\\green" + QByteArray::number(color.green())
             + "\\blue" + QByteArray::number(color.blue()) + ';';
    }
    rtf += "}\n{\\*\\expandedcolortbl;";
    for (const QColor &color : std::as_const(colors)) {
        rtf += "\\cssrgb" + component(color.redF()) + component(color.greenF()) + component(color.blueF());
        if (color.alphaF() < 1.0f)
            rtf += component(color.alphaF());
        rtf += ';';
    }
    rtf += "}\n\\deftab1680\n" + body + "}";
    return rtf;
}
