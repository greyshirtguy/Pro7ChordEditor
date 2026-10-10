#include "richtext.h"

#include "fontresolver.h"

#include <QStringList>

bool TextRun::sameFormat(const TextRun &other) const
{
    return fontName == other.fontName && family == other.family && size == other.size && bold == other.bold
        && italic == other.italic && underline == other.underline && strikethrough == other.strikethrough
        && kerning == other.kerning && superscript == other.superscript && capitalization == other.capitalization
        && fill == other.fill && fillVisible == other.fillVisible && stroke == other.stroke
        && strokeWidth == other.strokeWidth;
}

bool RichText::isEmpty() const
{
    for (const TextParagraph &paragraph : paragraphs) {
        for (const TextRun &run : paragraph.runs) {
            if (!run.text.isEmpty())
                return false;
        }
    }
    return true;
}

TextRun RichText::firstRun() const
{
    for (const TextParagraph &paragraph : paragraphs) {
        if (!paragraph.runs.isEmpty())
            return paragraph.runs.first();
    }
    return {};
}

RichText RichText::plain(const QString &text, const TextRun &format, Qt::Alignment alignment)
{
    RichText result;
    const QStringList lines = text.split(u'\n');
    for (const QString &line : lines) {
        TextRun run = format;
        run.text = line;
        TextParagraph paragraph;
        paragraph.alignment = alignment;
        paragraph.runs.append(run);
        result.paragraphs.append(paragraph);
    }
    return result;
}

QString RichText::plainText() const
{
    QStringList lines;
    for (const TextParagraph &paragraph : paragraphs) {
        QString line;
        for (const TextRun &run : paragraph.runs)
            line += run.text;
        line.replace(QChar::LineSeparator, u'\n');
        lines << line;
    }
    return lines.join(u'\n');
}

bool RichText::operator==(const RichText &other) const
{
    if (paragraphs.size() != other.paragraphs.size())
        return false;
    for (qsizetype i = 0; i < paragraphs.size(); ++i) {
        const TextParagraph &a = paragraphs.at(i);
        const TextParagraph &b = other.paragraphs.at(i);
        if (a.alignment != b.alignment || a.lineHeight != b.lineHeight
            || a.lineHeightIsMultiple != b.lineHeightIsMultiple || a.runs.size() != b.runs.size())
            return false;
        for (qsizetype r = 0; r < a.runs.size(); ++r) {
            if (a.runs.at(r).text != b.runs.at(r).text || !a.runs.at(r).sameFormat(b.runs.at(r)))
                return false;
        }
    }
    return true;
}

void TextRun::applyFormat(const QVariantMap &format)
{
    bool restyle = false;
    if (format.contains("family")) {
        family = format.value("family").toString();
        // A different family has faces of its own; nothing of the old name carries over.
        fontName.clear();
        restyle = true;
    }
    if (format.contains("bold")) {
        bold = format.value("bold").toBool();
        restyle = true;
    }
    if (format.contains("italic")) {
        italic = format.value("italic").toBool();
        restyle = true;
    }
    if (restyle)
        fontName = restyledPostScriptName(fontName, family, bold, italic);
    if (format.contains("size"))
        size = qMax(1.0, format.value("size").toDouble());
    if (format.contains("underline"))
        underline = format.value("underline").toBool();
    if (format.contains("strikethrough"))
        strikethrough = format.value("strikethrough").toBool();
    if (format.contains("kerning"))
        kerning = format.value("kerning").toDouble();
    if (format.contains("capitalization"))
        capitalization = format.value("capitalization").toInt();
    if (format.contains("color"))
        fill = format.value("color").value<QColor>();
    if (format.contains("strokeColor"))
        stroke = format.value("strokeColor").value<QColor>();
    if (format.contains("strokeWidth"))
        strokeWidth = qMax(0.0, format.value("strokeWidth").toDouble());
}

QVariantMap TextRun::format() const
{
    return {
        {"family", family},
        {"fontName", fontName},
        {"size", size},
        {"bold", bold},
        {"italic", italic},
        {"underline", underline},
        {"strikethrough", strikethrough},
        {"kerning", kerning},
        {"capitalization", capitalization},
        {"color", fill},
        {"strokeColor", stroke},
        {"strokeWidth", strokeWidth},
    };
}

namespace {

void appendMerged(QList<TextRun> *runs, const TextRun &run)
{
    // An empty run only matters when it is all a paragraph has.
    if (!runs->isEmpty() && runs->last().text.isEmpty()) {
        runs->last() = run;
    } else if (!runs->isEmpty() && run.text.isEmpty()) {
        return;
    } else if (!runs->isEmpty() && runs->last().sameFormat(run)) {
        runs->last().text += run.text;
    } else {
        runs->append(run);
    }
}

} // namespace

RichText RichText::formatted(int start, int end, const QVariantMap &format) const
{
    RichText result = *this;
    int offset = 0;
    for (TextParagraph &paragraph : result.paragraphs) {
        int length = 0;
        for (const TextRun &run : std::as_const(paragraph.runs))
            length += int(run.text.size());
        const int paragraphStart = offset;
        const int paragraphEnd = offset + length;
        // A paragraph is in range if any of it is, or if the range is a caret inside it.
        const bool touched = paragraphStart <= end && paragraphEnd >= start;
        if (touched && format.contains("alignment"))
            paragraph.alignment = Qt::Alignment(format.value("alignment").toInt());

        QList<TextRun> runs;
        for (const TextRun &run : std::as_const(paragraph.runs)) {
            const int runStart = offset;
            const int runEnd = offset + int(run.text.size());
            if (run.text.isEmpty()) {
                TextRun empty = run;
                if (runStart >= start && runStart <= end)
                    empty.applyFormat(format);
                appendMerged(&runs, empty);
                continue;
            }
            const int from = qBound(runStart, start, runEnd);
            const int to = qBound(runStart, end, runEnd);
            const auto piece = [&run, runStart](int a, int b) {
                TextRun part = run;
                part.text = run.text.mid(a - runStart, b - a);
                return part;
            };
            if (from > runStart)
                appendMerged(&runs, piece(runStart, from));
            if (to > from) {
                TextRun changed = piece(from, to);
                changed.applyFormat(format);
                appendMerged(&runs, changed);
            }
            if (to < runEnd)
                appendMerged(&runs, piece(to, runEnd));
            offset = runEnd;
        }
        paragraph.runs = runs;
        offset = paragraphEnd + 1;
    }
    return result;
}

QVariantMap RichText::formatAt(int start, int end) const
{
    int offset = 0;
    const TextParagraph *lastParagraph = nullptr;
    const TextRun *lastRun = nullptr;
    for (const TextParagraph &paragraph : paragraphs) {
        for (const TextRun &run : paragraph.runs) {
            const int runStart = offset;
            const int runEnd = offset + int(run.text.size());
            lastParagraph = &paragraph;
            lastRun = &run;
            // For a selection, the run its first character is in. For a caret, the run of
            // the character before it, whose format is what typing there would take.
            const bool hit = start < end ? (start >= runStart && start < runEnd)
                                         : (start > runStart && start <= runEnd) || (start == runStart && runStart == runEnd)
                                           || (start == runStart && &run == &paragraph.runs.first());
            if (hit) {
                QVariantMap format = run.format();
                format.insert("alignment", int(paragraph.alignment));
                return format;
            }
            offset = runEnd;
        }
        ++offset;
    }
    if (!lastRun)
        return {};
    QVariantMap format = lastRun->format();
    format.insert("alignment", int(lastParagraph->alignment));
    return format;
}
