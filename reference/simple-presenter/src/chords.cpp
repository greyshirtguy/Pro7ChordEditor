#include "chords.h"

#include <QHash>
#include <QRegularExpression>

#include <algorithm>

namespace chords {

namespace {

// The keys as the file numbers them (MusicKeyScale.MusicKey).
const char *const keyNames[] = {"Ab", "A", "A#", "Bb", "B", "B#", "Cb", "C", "C#", "Db", "D",
                                "D#", "Eb", "E", "E#", "Fb", "F", "F#", "Gb", "G", "G#"};
const int keyCount = 21;

// The seven letters from C, and how many semitones above C each is.
const QString letters = QStringLiteral("CDEFGAB");
const int semitones[] = {0, 2, 4, 5, 7, 9, 11};
// The plain name of each of the twelve notes, written with sharps and with flats.
const char *const sharpNames[] = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
const char *const flatNames[] = {"C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"};
const char *const latin[] = {"Do", "Re", "Mi", "Fa", "Sol", "La", "Si"};
const char *const numerals[] = {"I", "II", "III", "IV", "V", "VI", "VII"};

// A note as its letter (0 for C to 6 for B) and how far it is sharpened (flattened,
// below nothing).
struct Note
{
    int letter = -1;
    int accidental = 0;

    bool valid() const { return letter >= 0; }
    int pitch() const { return ((semitones[letter] + accidental) % 12 + 12) % 12; }
};

// Reads a note from the start of `text` and says how many characters it took.
Note readNote(const QString &text, int *length)
{
    Note note;
    *length = 0;
    if (text.isEmpty())
        return note;
    note.letter = int(letters.indexOf(text.at(0).toUpper()));
    if (note.letter < 0)
        return note;
    int i = 1;
    while (i < text.size() && (text.at(i) == u'#' || text.at(i) == QChar(0x266F))) {
        ++note.accidental;
        ++i;
    }
    if (i == 1) {
        while (i < text.size() && (text.at(i) == u'b' || text.at(i) == QChar(0x266D))) {
            --note.accidental;
            ++i;
        }
    }
    *length = i;
    return note;
}

QString written(const Note &note)
{
    QString name(letters.at(note.letter));
    for (int i = 0; i < note.accidental; ++i)
        name += u'#';
    for (int i = 0; i > note.accidental; --i)
        name += u'b';
    return name;
}

Note noteOfKey(const QString &key)
{
    int length = 0;
    return readNote(key, &length);
}

// The major key whose scale a key's places are counted on: itself, or for a minor key
// the major key a minor third above it.
Note scaleOf(const QString &key)
{
    Note note = noteOfKey(key);
    if (!note.valid() || !keyIsMinor(key))
        return note;
    Note major;
    major.letter = (note.letter + 2) % 7;
    int rise = semitones[major.letter] - semitones[note.letter];
    if (rise < 0)
        rise += 12;
    major.accidental = note.accidental + 3 - rise;
    return major;
}

// A note moved by as many letters and semitones as `to` is above `from`.
Note moved(const Note &note, const Note &from, const Note &to)
{
    const int steps = ((to.letter - from.letter) % 7 + 7) % 7;
    const int rise = ((to.pitch() - from.pitch()) % 12 + 12) % 12;
    Note result;
    result.letter = (note.letter + steps) % 7;
    const int pitch = (note.pitch() + rise) % 12;
    int accidental = pitch - semitones[result.letter];
    // The nearest way round: B sharp is one above B, not eleven below it.
    while (accidental > 6)
        accidental -= 12;
    while (accidental < -6)
        accidental += 12;
    result.accidental = accidental;
    if (qAbs(accidental) > 1) {
        // No double sharps or flats: the note's plain name, in sharps for a key that
        // is written with them and in flats otherwise.
        const bool sharps = to.accidental > 0 || (to.accidental == 0 && QStringLiteral("GDAEB").contains(letters.at(to.letter)));
        int length = 0;
        result = readNote(QString::fromLatin1(sharps ? sharpNames[pitch] : flatNames[pitch]), &length);
    }
    return result;
}

// A note's place in a major key's scale (0 to 6) and how far it is sharpened from the
// note the scale has there.
void placeIn(const Note &note, const Note &key, int *degree, int *accidental)
{
    *degree = ((note.letter - key.letter) % 7 + 7) % 7;
    const int inScale = (key.pitch() + semitones[*degree]) % 12;
    int off = note.pitch() - inScale;
    while (off > 6)
        off -= 12;
    while (off < -6)
        off += 12;
    *accidental = off;
}

QString marks(int accidental)
{
    return accidental > 0 ? QString(accidental, u'#') : QString(-accidental, u'b');
}

bool minorQuality(const QString &quality)
{
    return quality.startsWith(u'm') && !quality.startsWith(QLatin1String("maj"));
}

// What a chord's name is written with after its root.
bool chordCharacter(QChar c)
{
    return c.isLetterOrNumber() || QStringLiteral("#/+-()°ø^.,♯♭Δ").contains(c);
}

}

QString keyName(int number, bool minor)
{
    if (number < 0 || number >= keyCount)
        return {};
    return QString::fromLatin1(keyNames[number]) + (minor ? QStringLiteral("m") : QString());
}

int keyNumber(const QString &key)
{
    const QString name = keyIsMinor(key) ? key.chopped(1) : key;
    for (int i = 0; i < keyCount; ++i) {
        if (name == QLatin1String(keyNames[i]))
            return i;
    }
    return -1;
}

bool keyIsMinor(const QString &key)
{
    return key.size() > 1 && key.endsWith(u'm');
}

QStringList majorKeys()
{
    return {"C", "Db", "D", "Eb", "E", "F", "F#", "Gb", "G", "Ab", "A", "Bb", "B"};
}

QStringList minorKeys()
{
    return {"Cm", "C#m", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gm", "G#m", "Am", "Bbm", "Bm"};
}

Parts parts(const QString &chord)
{
    Parts result;
    int length = 0;
    const Note root = readNote(chord, &length);
    if (!root.valid())
        return result;
    result.root = written(root);
    QString rest = chord.mid(length);
    const qsizetype slash = rest.lastIndexOf(u'/');
    if (slash >= 0) {
        int bassLength = 0;
        const QString after = rest.mid(slash + 1);
        const Note bass = readNote(after, &bassLength);
        // "C6/9" has a slash and no bass note.
        if (bass.valid() && bassLength == after.size()) {
            result.bass = written(bass);
            rest = rest.left(slash);
        }
    }
    result.quality = rest;
    result.valid = true;
    return result;
}

bool couldBecome(const QString &typed)
{
    if (typed.isEmpty())
        return true;
    int length = 0;
    if (!readNote(typed, &length).valid())
        return false;
    return std::all_of(typed.cbegin() + length, typed.cend(), chordCharacter);
}

QString tidied(const QString &typed)
{
    const QString chord = typed.trimmed();
    int length = 0;
    const Note root = readNote(chord, &length);
    if (!root.valid())
        return chord;
    QString rest = chord.mid(length);
    const qsizetype slash = rest.lastIndexOf(u'/');
    if (slash >= 0) {
        int bassLength = 0;
        const QString after = rest.mid(slash + 1);
        const Note bass = readNote(after, &bassLength);
        if (bass.valid() && bassLength == after.size())
            rest = rest.left(slash + 1) + written(bass);
    }
    return written(root) + rest;
}

QString transposed(const QString &chord, const QString &fromKey, const QString &toKey)
{
    const Note from = noteOfKey(fromKey);
    const Note to = noteOfKey(toKey);
    const Parts chordParts = parts(chord);
    if (!from.valid() || !to.valid() || !chordParts.valid || fromKey == toKey)
        return chord;
    int length = 0;
    QString result = written(moved(readNote(chordParts.root, &length), from, to)) + chordParts.quality;
    if (!chordParts.bass.isEmpty())
        result += u'/' + written(moved(readNote(chordParts.bass, &length), from, to));
    return result;
}

QString notated(const QString &chord, const QString &key, int notation)
{
    const Parts chordParts = parts(chord);
    if (notation == Letters || !chordParts.valid)
        return chord;
    int length = 0;
    const Note root = readNote(chordParts.root, &length);
    const Note bass = chordParts.bass.isEmpty() ? Note() : readNote(chordParts.bass, &length);

    if (notation == DoReMi) {
        const auto name = [](const Note &note) { return QString::fromLatin1(latin[note.letter]) + marks(note.accidental); };
        return name(root) + chordParts.quality + (bass.valid() ? u'/' + name(bass) : QString());
    }

    const Note scale = scaleOf(key);
    if (!scale.valid())
        return chord;
    const auto place = [&](const Note &note, bool roman, bool small) {
        int degree = 0;
        int accidental = 0;
        placeIn(note, scale, &degree, &accidental);
        const QString number = roman ? QString::fromLatin1(numerals[degree]) : QString::number(degree + 1);
        return marks(accidental) + (small ? number.toLower() : number);
    };
    if (notation == Numbers)
        return place(root, false, false) + chordParts.quality + (bass.valid() ? u'/' + place(bass, false, false) : QString());

    // Numerals: a minor chord is a small numeral and loses its "m"; a diminished one is
    // small too, with the little circle.
    QString quality = chordParts.quality;
    bool small = false;
    if (quality.startsWith(QLatin1String("dim"))) {
        small = true;
        quality = QChar(0x00B0) + quality.mid(3);
    } else if (minorQuality(quality)) {
        small = true;
        quality = quality.mid(quality.startsWith(QLatin1String("min")) ? 3 : 1);
    }
    return place(root, true, small) + quality + (bass.valid() ? u'/' + place(bass, true, false) : QString());
}

QString shown(const QString &chord, const QString &originalKey, const QString &key, int notation)
{
    const bool move = !key.isEmpty() && !originalKey.isEmpty() && key != originalKey;
    const QString in = move ? key : originalKey;
    return notated(move ? transposed(chord, originalKey, key) : chord, in, notation);
}

QStringList diatonic(const QString &key)
{
    const Note scale = scaleOf(key);
    if (!scale.valid())
        return {};
    static const char *const kinds[] = {"", "m", "m", "", "", "m", "dim"};
    QStringList result;
    for (int degree = 0; degree < 7; ++degree) {
        Note note;
        note.letter = (scale.letter + degree) % 7;
        int accidental = (scale.pitch() + semitones[degree]) % 12 - semitones[note.letter];
        while (accidental > 6)
            accidental -= 12;
        while (accidental < -6)
            accidental += 12;
        note.accidental = accidental;
        result << written(note) + QLatin1String(kinds[degree]);
    }
    // A minor key's own chords start from its own note: the sixth of the major scale.
    if (keyIsMinor(key))
        result = result.mid(5) + result.mid(0, 5);
    return result;
}

QStringList completions(const QString &typed, const QString &key, const QStringList &used)
{
    const QString start = tidied(typed);
    QStringList result;
    const auto offer = [&](const QString &chord) {
        if (!chord.isEmpty() && chord != start && chord.startsWith(start) && !result.contains(chord))
            result << chord;
    };
    // The song's own chords, the most used first; then the key's.
    QStringList own = used;
    own.removeDuplicates();
    std::stable_sort(own.begin(), own.end(), [&](const QString &a, const QString &b) { return used.count(a) > used.count(b); });
    for (const QString &chord : std::as_const(own))
        offer(chord);
    const QStringList inKey = diatonic(key);
    for (const QString &chord : inKey)
        offer(chord);
    if (start.isEmpty())
        return result;

    const Parts typedParts = parts(start);
    if (!typedParts.valid)
        return result;
    const qsizetype slash = start.lastIndexOf(u'/');
    if (slash >= 0 && typedParts.bass.isEmpty() == start.endsWith(u'/')) {
        // After a slash: the chord's third and fifth, which are what most slash chords
        // stand on, then the notes of the key.
        const QString before = start.left(slash + 1);
        int length = 0;
        const Note root = readNote(typedParts.root, &length);
        const bool minor = minorQuality(typedParts.quality);
        const QStringList &names = (root.accidental < 0 || QStringLiteral("F").contains(letters.at(root.letter)))
            ? QStringList{flatNames, flatNames + 12} : QStringList{sharpNames, sharpNames + 12};
        offer(before + names.at((root.pitch() + (minor ? 3 : 4)) % 12));
        offer(before + names.at((root.pitch() + 7) % 12));
        for (const QString &chord : inKey) {
            // (A chord over its own root is only the chord.)
            const QString note = parts(chord).root;
            if (note != typedParts.root)
                offer(before + note);
        }
        return result;
    }
    // The usual kinds of chord on the note typed.
    static const char *const kinds[] = {"", "m", "7", "m7", "maj7", "sus4", "sus2", "2", "add9", "6", "9", "5", "dim", "aug", "m7b5", "11", "13"};
    for (const char *kind : kinds)
        offer(typedParts.root + QLatin1String(kind));
    return result;
}

QList<Chord> tidy(const QString &text, QList<Chord> chords)
{
    QList<Chord> result;
    if (text.isEmpty())
        return result;
    for (Chord &chord : chords) {
        chord.name = chord.name.trimmed();
        if (chord.name.isEmpty())
            continue;
        chord.at = qBound(0, chord.at, int(text.size()) - 1);
        // A chord cannot stand on the end of a line, which has no character of its
        // own that shows: it goes to the last character before it.
        if (text.at(chord.at) == u'\n' && chord.at > 0 && text.at(chord.at - 1) != u'\n')
            --chord.at;
        const auto same = std::find_if(result.begin(), result.end(), [&](const Chord &other) { return other.at == chord.at; });
        if (same != result.end())
            *same = chord;
        else
            result.append(chord);
    }
    std::stable_sort(result.begin(), result.end(), [](const Chord &a, const Chord &b) { return a.at < b.at; });
    return result;
}

QList<Chord> carried(const QString &before, const QString &after, const QList<Chord> &chords)
{
    if (before == after || chords.isEmpty())
        return tidy(after, chords);
    qsizetype same = 0;
    const qsizetype shorter = qMin(before.size(), after.size());
    while (same < shorter && before.at(same) == after.at(same))
        ++same;
    qsizetype tail = 0;
    while (tail < shorter - same && before.at(before.size() - 1 - tail) == after.at(after.size() - 1 - tail))
        ++tail;
    QList<Chord> moved;
    for (Chord chord : chords) {
        if (chord.at >= before.size() - tail)
            chord.at += int(after.size() - before.size());
        else if (chord.at >= same)
            chord.at = int(same);
        moved.append(chord);
    }
    // Two that end up in one place: the first stands, the way they read.
    QList<Chord> apart;
    for (const Chord &chord : std::as_const(moved)) {
        if (std::none_of(apart.cbegin(), apart.cend(), [&](const Chord &other) { return other.at == chord.at; }))
            apart.append(chord);
    }
    return tidy(after, apart);
}

QList<Range> ranges(const QString &text, const QList<Chord> &chords)
{
    const QList<Chord> ordered = tidy(text, chords);
    QList<Range> result;
    for (qsizetype i = 0; i < ordered.size(); ++i) {
        const Chord &chord = ordered.at(i);
        qsizetype end = text.indexOf(u'\n', chord.at);
        if (end < 0)
            end = text.size();
        if (i + 1 < ordered.size())
            end = qMin(end, qsizetype(ordered.at(i + 1).at));
        result.append({chord.at, int(qMax(end, qsizetype(chord.at + 1))), chord.name});
    }
    return result;
}

QString toChordPro(const Line &line)
{
    QString result;
    QList<Chord> ordered = line.chords;
    std::stable_sort(ordered.begin(), ordered.end(), [](const Chord &a, const Chord &b) { return a.at < b.at; });
    qsizetype next = 0;
    // A line of chords alone is written as the chords with a space between, which is
    // how such a line is written by hand; the stand-ins they hang on are the file's
    // business.
    const bool alone = isPlaceholders(line.text);
    for (qsizetype i = 0; i <= line.text.size(); ++i) {
        while (next < ordered.size() && ordered.at(next).at <= i) {
            if (alone && !result.isEmpty())
                result += u' ';
            result += u'[' + ordered.at(next).name + u']';
            ++next;
        }
        if (i < line.text.size() && !alone)
            result += line.text.at(i);
    }
    return result;
}

Line fromChordPro(const QString &line)
{
    Line result;
    qsizetype i = 0;
    while (i < line.size()) {
        const QChar c = line.at(i);
        const qsizetype close = c == u'[' ? line.indexOf(u']', i) : -1;
        if (close > i) {
            const QString inside = tidied(line.mid(i + 1, close - i - 1));
            if (parts(inside).valid && couldBecome(inside))
                result.chords.append({int(result.text.size()), inside});
            i = close + 1;
            continue;
        }
        result.text += c;
        ++i;
    }
    // Chords with no words: one stand-in each (see placeholders()).
    if (result.text.trimmed().isEmpty() && !result.chords.isEmpty()) {
        result.text = placeholders(int(result.chords.size()));
        for (qsizetype n = 0; n < result.chords.size(); ++n)
            result.chords[n].at = int(n) * 2;
        return result;
    }
    // A chord after the last word hangs on the last character, there being none after it.
    for (Chord &chord : result.chords)
        chord.at = qMin(chord.at, qMax(0, int(result.text.size()) - 1));
    return result;
}

QString toChordPro(const QString &text, const QList<Chord> &chords)
{
    QStringList lines;
    int start = 0;
    const QStringList textLines = text.split(u'\n');
    for (const QString &textLine : textLines) {
        Line line;
        line.text = textLine;
        for (const Chord &chord : chords) {
            if (chord.at >= start && chord.at < start + qMax(qsizetype(1), textLine.size()))
                line.chords.append({chord.at - start, chord.name});
        }
        lines << toChordPro(line);
        start += int(textLine.size()) + 1;
    }
    return lines.join(u'\n');
}

QList<Chord> chordsOf(const QString &chordPro)
{
    // Read without making stand-ins: the places are wanted along the words as they are.
    QList<Chord> result;
    int at = 0;
    qsizetype i = 0;
    while (i < chordPro.size()) {
        const qsizetype close = chordPro.at(i) == u'[' ? chordPro.indexOf(u']', i) : -1;
        if (close > i) {
            const QString inside = tidied(chordPro.mid(i + 1, close - i - 1));
            if (!inside.isEmpty())
                result.append({at, inside});
            i = close + 1;
            continue;
        }
        ++at;
        ++i;
    }
    return result;
}

QString withoutChords(const QString &chordPro)
{
    static const QRegularExpression chord(QStringLiteral("\\[[^\\[\\]\\n]*\\]"));
    QString result = chordPro;
    return result.remove(chord);
}

QString placeholders(int count, QChar standIn)
{
    QString result;
    for (int i = 0; i < count; ++i) {
        if (i > 0)
            result += QChar(0x2001);
        result += standIn;
    }
    return result;
}

bool isPlaceholders(const QString &line)
{
    for (const QChar c : line) {
        if (c != QChar(0x200B) && !c.isSpace())
            return false;
    }
    return !line.isEmpty();
}

namespace {

// "Verse 1", "CHORUS:", "[Bridge]", "Pre-Chorus 2": a line that only names a part.
QString headingOf(const QString &line)
{
    QString text = line.trimmed();
    if (text.startsWith(u'[') && text.endsWith(u']'))
        text = text.mid(1, text.size() - 2).trimmed();
    if (text.endsWith(u':'))
        text.chop(1);
    static const QRegularExpression heading(
        QStringLiteral("^(verse|chorus|pre[- ]?chorus|bridge|intro|outro|ending|tag|interlude|instrumental|refrain|turnaround|vamp|"
                       "coda|hook|post[- ]?chorus)( ?\\d+[a-z]?)?$"),
        QRegularExpression::CaseInsensitiveOption);
    if (!heading.match(text).hasMatch())
        return {};
    // "VERSE 1" as "Verse 1"
    QString name = text.toLower();
    bool start = true;
    for (QChar &c : name) {
        if (start && c.isLetter())
            c = c.toUpper();
        start = c == u' ' || c == u'-';
    }
    return name;
}

}

Song parseSong(const QString &text)
{
    Song song;
    Section section;
    // Inside a part the file marked with a directive, where an empty line is only a gap
    bool marked = false;
    bool skipping = false;
    QHash<QString, int> counts;
    const auto close = [&] {
        if (!section.lines.isEmpty())
            song.sections.append(section);
        section = {};
        marked = false;
    };
    const auto open = [&](const QString &kind, const QString &label) {
        close();
        marked = true;
        if (!label.isEmpty()) {
            section.name = label;
            return;
        }
        // Verses are numbered as they come; a chorus or a bridge is not, the first time.
        const int n = ++counts[kind];
        section.name = kind == QLatin1String("Verse") || n > 1 ? kind + u' ' + QString::number(n) : kind;
    };

    static const QRegularExpression directive(QStringLiteral("^\\{\\s*([^:}\\s]+)\\s*(?::\\s*(.*?))?\\s*\\}$"));
    QString normal = text;
    normal.replace(QLatin1String("\r\n"), QLatin1String("\n")).replace(u'\r', u'\n');
    const QStringList lines = normal.split(u'\n');
    for (const QString &raw : lines) {
        const QString line = raw.trimmed();
        if (line.startsWith(u'#'))
            continue;
        const QRegularExpressionMatch match = directive.match(line);
        if (match.hasMatch()) {
            const QString name = match.captured(1).toLower();
            const QString value = match.captured(2).trimmed();
            if (name == QLatin1String("title") || name == QLatin1String("t")) {
                song.title = value;
            } else if (name == QLatin1String("artist") || name == QLatin1String("subtitle") || name == QLatin1String("st")) {
                if (song.artist.isEmpty())
                    song.artist = value;
            } else if (name == QLatin1String("key")) {
                song.key = tidied(value);
            } else if (name == QLatin1String("copyright")) {
                song.copyright = value;
            } else if (name == QLatin1String("ccli")) {
                song.ccli = value;
            } else if (name == QLatin1String("start_of_verse") || name == QLatin1String("sov")) {
                open(QStringLiteral("Verse"), value);
            } else if (name == QLatin1String("start_of_chorus") || name == QLatin1String("soc")) {
                open(QStringLiteral("Chorus"), value);
            } else if (name == QLatin1String("start_of_bridge") || name == QLatin1String("sob")) {
                open(QStringLiteral("Bridge"), value);
            } else if (name.startsWith(QLatin1String("start_of_")) || name == QLatin1String("sot") || name == QLatin1String("sog")) {
                // Tablature, a grid, or something newer: not words to show
                close();
                skipping = true;
            } else if (name.startsWith(QLatin1String("end_of_")) || (name.size() == 3 && name.startsWith(QLatin1String("eo")))) {
                close();
                skipping = false;
            } else if (name == QLatin1String("comment") || name == QLatin1String("c") || name == QLatin1String("ci")
                       || name == QLatin1String("comment_italic")) {
                const QString heading = headingOf(value);
                if (!heading.isEmpty()) {
                    close();
                    section.name = heading;
                    marked = true;
                }
            }
            continue;
        }
        if (skipping)
            continue;
        if (line.isEmpty()) {
            // Between the parts of a file that marks none, and inside a marked part
            // only a gap.
            if (!marked || section.name.isEmpty())
                close();
            continue;
        }
        const QString heading = headingOf(line);
        if (!heading.isEmpty()) {
            close();
            section.name = heading;
            marked = false;
            continue;
        }
        const Line read = fromChordPro(line);
        if (!read.text.trimmed().isEmpty() || !read.chords.isEmpty()) {
            Line kept = read;
            if (!isPlaceholders(kept.text)) {
                // Spaces at the ends are of no use on a slide, and the chords move
                // with the words.
                const qsizetype lead = kept.text.size() - QString(kept.text + u'x').trimmed().size() + 1;
                kept.text = kept.text.trimmed();
                for (Chord &chord : kept.chords)
                    chord.at = qBound(0, chord.at - int(lead), qMax(0, int(kept.text.size()) - 1));
            }
            section.lines.append(kept);
        }
    }
    close();
    return song;
}

QList<QList<Line>> slidesOf(const Section &section, int linesPerSlide)
{
    QList<QList<Line>> slides;
    const int each = qMax(1, linesPerSlide);
    for (qsizetype i = 0; i < section.lines.size(); i += each)
        slides.append(section.lines.mid(i, each));
    return slides;
}

}
