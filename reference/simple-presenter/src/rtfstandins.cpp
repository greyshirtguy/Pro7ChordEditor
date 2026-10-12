#include "rtfstandins.h"

#include <QStringDecoder>

#include <vector>

namespace rtfstandins {

namespace {

// Windows-1252 differs from Latin-1 only in 0x80..0x9F. (As rtf.cpp has it.)
const char16_t cp1252High[32] = {
    0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
    0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
    0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
    0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178,
};

char16_t fromCp1252(uchar byte)
{
    return byte >= 0x80 && byte <= 0x9F ? cp1252High[byte - 0x80] : char16_t(byte);
}

bool isLetter(uchar c)
{
    return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}

// Groups whose contents are not text
bool isSkipped(const QByteArray &word)
{
    static const char *const skipped[] = {"fonttbl", "colortbl", "info", "stylesheet", "pict", "header", "footer", "listtable",
                                          "listoverridetable"};
    for (const char *name : skipped) {
        if (word == name)
            return true;
    }
    return false;
}

const char16_t zeroWidth = 0x200B;
const char16_t emQuad = 0x2001;

// The bytes a run of stand-ins is written with, or nothing if it holds anything else.
std::optional<QByteArray> spelt(QStringView units)
{
    QByteArray out;
    for (const QChar unit : units) {
        if (unit == u' ')
            out += ' ';
        else if (unit == QChar(zeroWidth) || unit == QChar(emQuad))
            out += "\\uc0\\u" + QByteArray::number(int(unit.unicode())) + ' ';
        else
            return std::nullopt;
    }
    return out;
}

} // namespace

QList<Unit> written(const QByteArray &rtf, bool *ok)
{
    QList<Unit> units;
    if (ok)
        *ok = true;
    const auto broken = [&] {
        if (ok)
            *ok = false;
        return units;
    };
    const qsizetype size = rtf.size();
    const auto at = [&](qsizetype n) { return uchar(rtf.at(n)); };
    // For each open group: whether what is in it is not text; and how many characters
    // are passed over after a numbered one
    std::vector<bool> skipping;
    std::vector<int> skipCount = {1};
    bool starred = false;
    // Characters still to pass over after a numbered one
    int pending = 0;
    // Where the last control word that had no space after it ended
    qsizetype openEnd = -1;

    const auto inSkipped = [&] { return !skipping.empty() && skipping.back(); };
    const auto note = [&](char16_t character, qsizetype start, qsizetype end, bool openBefore) {
        units.append({character, start, end, openBefore, end == openEnd, int(skipping.size())});
    };
    // A character of the text, unless it is one that is passed over or is in a group
    // that is not text
    const auto put = [&](char16_t character, qsizetype start, qsizetype end, bool openBefore) {
        if (pending > 0) {
            --pending;
            return;
        }
        if (!inSkipped())
            note(character, start, end, openBefore);
    };
    const auto newParagraph = [&](qsizetype start, qsizetype end, bool openBefore) {
        if (!inSkipped())
            note(u'\n', start, end, openBefore);
    };

    qsizetype i = 0;
    while (i < size) {
        const uchar c = at(i);
        ++i;
        if (c == '{') {
            skipping.push_back(inSkipped());
            skipCount.push_back(skipCount.back());
            starred = false;
            pending = 0;
        } else if (c == '}') {
            if (!skipping.empty())
                skipping.pop_back();
            if (skipCount.size() > 1)
                skipCount.pop_back();
            pending = 0;
        } else if (c == '\\') {
            if (i >= size)
                break;
            const qsizetype from = i - 1;
            // (Asked now: this may itself be a control word with no space after it, and
            // would then be the last such.)
            const bool wasOpen = from == openEnd;
            const uchar n = at(i);
            if (!isLetter(n)) {
                ++i;
                if (n == '\'') {
                    if (i + 2 > size)
                        return broken();
                    bool hex = false;
                    const int byte = rtf.mid(i, 2).toInt(&hex, 16);
                    if (!hex)
                        return broken();
                    i += 2;
                    put(fromCp1252(uchar(byte)), from, i, wasOpen);
                } else if (n == '*') {
                    starred = true;
                } else if (n == '\n' || n == '\r') {
                    newParagraph(from, i, wasOpen);
                } else if (n == '~') {
                    put(0x00A0, from, i, wasOpen);
                } else if (n == '_') {
                    put(0x2011, from, i, wasOpen);
                } else if (n == '-') {
                    // An optional hyphen: nothing of the text
                } else {
                    put(char16_t(n), from, i, wasOpen);
                }
                continue;
            }
            qsizetype start = i;
            while (i < size && isLetter(at(i)))
                ++i;
            const QByteArray word = rtf.mid(start, i - start);
            start = i;
            if (i < size && at(i) == '-')
                ++i;
            while (i < size && at(i) >= '0' && at(i) <= '9')
                ++i;
            const QByteArray digits = rtf.mid(start, i - start);
            const bool numbered = !digits.isEmpty() && digits != "-";
            bool inRange = true;
            const int number = numbered ? digits.toInt(&inRange) : 0;
            if (i < size && at(i) == ' ')
                ++i;
            else
                openEnd = i;
            if (starred) {
                // The first word after an asterisk names something that is not text.
                starred = false;
                if (!skipping.empty())
                    skipping.back() = true;
                continue;
            }
            if (inSkipped())
                continue;
            if (isSkipped(word)) {
                if (!skipping.empty())
                    skipping.back() = true;
            } else if (word == "par") {
                newParagraph(from, i, wasOpen);
            } else if (word == "line") {
                put(u'\n', from, i, wasOpen);
            } else if (word == "tab") {
                put(u'\t', from, i, wasOpen);
            } else if (word == "u" && numbered) {
                // (A number below nought is one above 32,767 written as RTF's own
                // numbers go.)
                const int unit = number < 0 ? number + 65536 : number;
                if (!inRange || unit < 0 || unit > 0xFFFF)
                    return broken();
                put(char16_t(unit), from, i, wasOpen);
                pending = skipCount.back();
            } else if (word == "uc" && numbered) {
                skipCount.back() = number;
            }
        } else if (c == '\n' || c == '\r') {
            // A line end in the file is not one in the text.
        } else if (c < 0x80) {
            put(char16_t(c), i - 1, i, i - 1 == openEnd);
        } else {
            // Bytes above ASCII written straight in: UTF-8 if they are that, else the
            // code page's.
            const qsizetype start = i - 1;
            while (i < size && at(i) >= 0x80)
                ++i;
            const QByteArray run = rtf.mid(start, i - start);
            QStringDecoder utf8(QStringDecoder::Utf8, QStringDecoder::Flag::Stateless);
            const QString decoded = utf8.decode(run);
            if (utf8.hasError() || decoded.contains(QChar(QChar::ReplacementCharacter))) {
                for (qsizetype n = 0; n < run.size(); ++n)
                    put(fromCp1252(uchar(run.at(n))), start + n, start + n + 1, start + n == openEnd);
                continue;
            }
            qsizetype where = start;
            for (qsizetype n = 0; n < decoded.size(); ++n) {
                const char16_t unit = decoded.at(n).unicode();
                if (QChar::isHighSurrogate(unit) && n + 1 < decoded.size()) {
                    // A character from beyond the first 65,536: two units of the one
                    // stretch of four bytes.
                    const qsizetype after = where + 4;
                    put(unit, where, after, where == openEnd);
                    put(decoded.at(n + 1).unicode(), where, after, where == openEnd);
                    ++n;
                    where = after;
                } else {
                    const qsizetype after = where + (unit < 0x800 ? 2 : 3);
                    put(unit, where, after, where == openEnd);
                    where = after;
                }
            }
        }
    }
    return units;
}

QString plainText(const QByteArray &rtf)
{
    const QList<Unit> units = written(rtf);
    QString text;
    text.reserve(units.size());
    for (const Unit &unit : units)
        text += QChar(unit.character);
    return text;
}

std::optional<QByteArray> withLine(const QByteArray &rtf, int line, const QString &wanted)
{
    bool ok = false;
    const QList<Unit> units = written(rtf, &ok);
    if (!ok || line < 0)
        return std::nullopt;
    // Where the line is among the units
    qsizetype start = 0;
    for (int passed = 0; passed < line; ++passed) {
        qsizetype breakAt = start;
        while (breakAt < units.size() && units.at(breakAt).character != u'\n')
            ++breakAt;
        if (breakAt >= units.size())
            return std::nullopt;
        start = breakAt + 1;
    }
    qsizetype end = start;
    while (end < units.size() && units.at(end).character != u'\n')
        ++end;
    // (A line with no character has nothing to say where it is.)
    if (end == start)
        return std::nullopt;

    // What the line has already of what it is to become is kept as it is written.
    qsizetype same = 0;
    while (start + same < end && same < wanted.size() && units.at(start + same).character == wanted.at(same).unicode())
        ++same;
    std::optional<QByteArray> put = spelt(QStringView(wanted).mid(same));
    if (!put)
        return std::nullopt;
    // The units that go. (Two that are one stretch of bytes, the halves of a character
    // from beyond the first 65,536, are not taken out by halves.)
    const QList<Unit> going = units.mid(start + same, end - start - same);
    for (qsizetype n = 0; n + 1 < going.size(); ++n) {
        if (going.at(n + 1).start < going.at(n).end)
            return std::nullopt;
    }
    const qsizetype where = going.isEmpty() ? units.at(end - 1).end : going.first().start;
    const bool lost = going.isEmpty() ? units.at(end - 1).openAfter : going.first().openBefore;
    // A plain space put in where a control word with no space of its own ends would be
    // taken for the word's end: one is put in first to be that.
    if (lost && put->startsWith(' '))
        put->prepend(' ');
    QByteArray made = rtf.left(where) + *put;
    qsizetype after = where;
    for (const Unit &one : going) {
        made += rtf.mid(after, one.start - after);
        after = one.end;
    }
    made += rtf.mid(after);

    // Read again: the old text with that line changed, and nothing else.
    QString text;
    for (const Unit &unit : units)
        text += QChar(unit.character);
    bool readable = false;
    const QList<Unit> again = written(made, &readable);
    QString now;
    for (const Unit &unit : again)
        now += QChar(unit.character);
    if (!readable || now != text.left(start + same) + wanted.mid(same) + text.mid(end))
        return std::nullopt;
    return made;
}

int placeAfter(int place, int lineStart, int was, int now)
{
    const int oldEnd = lineStart + was;
    if (place >= oldEnd)
        return place + (now - was);
    // (Only where the line has shrunk is anything before its old end past its new one.)
    return qMin(place, lineStart + qMin(was, now));
}

} // namespace rtfstandins
