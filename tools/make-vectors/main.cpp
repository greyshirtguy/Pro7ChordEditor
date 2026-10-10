// Writes the test vectors in testdata/vectors from the working code of Simple
// Presenter's chord editor: what its rules answer for a great many inputs, as JSON, for
// the Mac and the Windows apps' own tests to hold their rules to.
//
// Nothing is worked out here. Every answer is whatever Simple Presenter's functions
// give (src/chords.cpp for the chords, src/rtf.cpp for the text of an RTF), so the
// vectors are a record of what that editor does and not a second opinion of it. Where
// the code and spec/ disagree, the vectors are the code's side of it.
//
//   make-vectors <folder to write into>
//
// README.md beside this says how to build it and when to run it again.
#include "chords.h"
#include "rtf.h"

#include <QCoreApplication>
#include <QDir>
#include <QFile>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QTextStream>

using namespace chords;

namespace {

QString outFolder;

void write(const QString &name, const QString &about, QJsonObject body)
{
    QJsonObject whole;
    whole["about"] = about;
    whole["madeBy"] = QStringLiteral("tools/make-vectors, from Simple Presenter's own code. Do not edit by hand: run it again.");
    for (auto it = body.begin(); it != body.end(); ++it)
        whole[it.key()] = it.value();
    QFile file(outFolder + u'/' + name);
    if (!file.open(QIODevice::WriteOnly))
        qFatal("cannot write %s", qPrintable(file.fileName()));
    file.write(QJsonDocument(whole).toJson(QJsonDocument::Indented));
    QTextStream(stdout) << name << "\n";
}

QJsonArray strings(const QStringList &list)
{
    QJsonArray array;
    for (const QString &s : list)
        array.append(s);
    return array;
}

QJsonArray chordsJson(const QList<Chord> &list)
{
    QJsonArray array;
    for (const Chord &chord : list)
        array.append(QJsonObject {{"at", chord.at}, {"name", chord.name}});
    return array;
}

QList<Chord> chordList(std::initializer_list<std::pair<int, const char *>> list)
{
    QList<Chord> result;
    for (const auto &entry : list)
        result.append({entry.first, QString::fromUtf8(entry.second)});
    return result;
}

// The two characters that cannot be seen: what a chord with no words hangs on, and what
// stands between two of them (see chords::placeholders).
const QString zeroWidth = QString(QChar(0x200B));
const QString emQuad = QString(QChar(0x2001));

// All forty-two keys the file can name: the twenty-one notes, major and minor.
QStringList allKeys()
{
    QStringList keys;
    for (int number = 0; number < 21; ++number)
        keys << keyName(number, false);
    for (int number = 0; number < 21; ++number)
        keys << keyName(number, true);
    return keys;
}

bool isChord(const QString &typed)
{
    return parts(typed).valid && couldBecome(typed);
}

// ---- keys.json

void keys()
{
    QJsonArray names;
    for (int number = 0; number < 21; ++number)
        names.append(QJsonObject {{"number", number}, {"major", keyName(number, false)}, {"minor", keyName(number, true)}});
    QJsonArray outOfRange;
    for (int number : {-1, 21, 99})
        outOfRange.append(QJsonObject {{"number", number}, {"major", keyName(number, false)}, {"minor", keyName(number, true)}});
    QJsonArray numbers;
    QStringList asked = allKeys();
    asked << "" << "H" << "c" << "cm" << "m" << "Cmaj" << "C m" << "Hm" << "C##" << "Dbb" << "A♭" << "Am7" << "Bbm " << "do";
    for (const QString &key : std::as_const(asked))
        numbers.append(QJsonObject {{"key", key}, {"number", keyNumber(key)}, {"minor", keyIsMinor(key)}});
    write("keys.json",
          "The key as the file has it (a number 0 to 20 and major or minor) and as a musician writes it. `names`: keyName(number, minor). "
          "`numbers`: keyNumber(key), which is -1 for what is no key, and keyIsMinor(key). `majorKeys` and `minorKeys`: what a picker offers, in order.",
          {{"names", names}, {"outOfRange", outOfRange}, {"numbers", numbers}, {"majorKeys", strings(majorKeys())}, {"minorKeys", strings(minorKeys())}});
}

// ---- chord-names.json

const QStringList someNames = {
    "C", "c", "Cm", "C#", "Db", "F#m7", "f#m7", "Bb/D", "bb/d", "D/F#", "d/f#", "E/G#", "C6/9", "G1", "Gsus4", "Asus2", "Am7b5", "Cmaj7", "CMaj7",
    "Ebmaj7/G", "Bdim", "B°", "Caug", "C+", "Cadd9", "C2", "D7#9", "A7(b9)", "G13", "N.C.", "NC", "H", "Hm", "", " ", " G ", "G ", "x", "1", "#", "b",
    "B", "Bb", "Bbb", "B#", "E#", "Fb", "Cb", "C##", "A♭", "C♯m", "A♭/C", "G/", "G//", "G/B/D", "Am/", "C/E/", "C/x", "C/9", "D/F", "A/C#", "f#M7/a",
    "C /// | Am ///", "C][G", "[C]", "C]", "G-", "G-7", "Cm(maj7)", "C^7", "CΔ7", "Gø7", "G.", "G,", "G?", "G!", "Do", "do", "Am7/G", "E7sus4", "F#",
    "gb", "Gbm", "F#m", "ebm", "D#m", "A#", "a#dim", "Csus", "c/e", "C/Bb", "C/bb", "Dm7/C", "G/A#", "abmaj7",
};

void chordNames()
{
    QJsonArray cases;
    for (const QString &name : someNames) {
        const Parts p = parts(name);
        cases.append(QJsonObject {
            {"in", name},
            {"valid", p.valid}, {"root", p.root}, {"quality", p.quality}, {"bass", p.bass},
            {"tidied", tidied(name)},
            {"couldBecome", couldBecome(name)},
            {"isChord", isChord(name)},
        });
    }
    write("chord-names.json",
          "What a chord's name is taken to be. For each `in`: parts(in) as valid, root, quality and bass; tidied(in), what a typed chord is "
          "tidied to; couldBecome(in), whether it could be the start of a chord being typed; and isChord(in), which is valid and couldBecome, "
          "what an editor asks before it puts a newly typed chord on. (A chord that is in a file already is kept whatever it is.)",
          {{"cases", cases}});
}

// ---- transposed.json and notated.json: tables

const QStringList tableChords = {"C", "Dm", "F#m7", "Bb/D", "E/G#", "Gsus4", "Ab", "B", "C#dim", "G1", "Ebmaj7/G", "A#m", "Cb", "E#"};

void transposing()
{
    const QStringList keys = allKeys();
    QJsonArray rows;
    for (const QString &from : keys) {
        for (const QString &to : keys) {
            QJsonArray out;
            for (const QString &chord : tableChords)
                out.append(transposed(chord, from, to));
            rows.append(QJsonObject {{"from", from}, {"to", to}, {"out", out}});
        }
    }
    QJsonArray odd;
    const QList<QStringList> oddCases = {{"G", "", "D"}, {"G", "C", ""}, {"G", "H", "D"}, {"", "C", "D"}, {"N.C.", "C", "D"}, {"x", "C", "D"},
                                         {"C /// | Am ///", "C", "D"}, {"g", "C", "D"}, {"f#m/a", "A", "Bb"}, {"G", "C", "C"}, {"C/E/", "C", "D"},
                                         {"C6/9", "C", "Eb"}, {"A♭", "Ab", "A"}};
    for (const QStringList &c : oddCases)
        odd.append(QJsonObject {{"chord", c.at(0)}, {"from", c.at(1)}, {"to", c.at(2)}, {"out", transposed(c.at(0), c.at(1), c.at(2))}});
    write("transposed.json",
          "transposed(chord, fromKey, toKey): a chord moved from one key to another, spelt as the new key spells it. `rows` has every pair of "
          "the forty-two keys: `out` is the answer for each of `chords`, in their order. `odd` has what is given back for a chord that cannot "
          "be read or a key that is no key (the chord as it was).",
          {{"keys", strings(keys)}, {"chords", strings(tableChords)}, {"rows", rows}, {"odd", odd}});
}

void notating()
{
    const QStringList keys = allKeys();
    QJsonArray rows;
    for (const QString &key : keys) {
        for (int notation = 0; notation <= 3; ++notation) {
            QJsonArray out;
            for (const QString &chord : tableChords)
                out.append(notated(chord, key, notation));
            rows.append(QJsonObject {{"key", key}, {"notation", notation}, {"out", out}});
        }
    }
    QJsonArray more;
    const QStringList kinds = {"C", "Cm", "Cm7", "Cmin7", "Cmaj7", "Cdim", "Cdim7", "Caug", "Csus4", "C7", "Cm/Eb", "C/E", "Cadd9", "Cm7b5", "x", "", "N.C."};
    for (const QString &chord : kinds) {
        for (const QString &key : {QStringLiteral("C"), QStringLiteral("Am"), QStringLiteral("Eb"), QStringLiteral("")}) {
            for (int notation = 0; notation <= 3; ++notation)
                more.append(QJsonObject {{"chord", chord}, {"key", key}, {"notation", notation}, {"out", notated(chord, key, notation)}});
        }
    }
    write("notated.json",
          "notated(chord, key, notation): a chord, which is in `key`, written in one of ProPresenter's four notations (0 letters, 1 numbers, "
          "2 numerals, 3 do re mi, as its file numbers them). `rows` has every key and notation for each of `chords`; `kinds` has the kinds of "
          "chord that numerals write differently. A GUESS in 1 to 3: ProPresenter does not say what it writes (spec/02-chord-rules.md).",
          {{"keys", strings(keys)}, {"chords", strings(tableChords)}, {"rows", rows}, {"kinds", more}});
}

void showing()
{
    QJsonArray cases;
    const QStringList chordsShown = {"E", "A", "B", "C#m", "F#m7", "E/G#", "G1", "x"};
    const QStringList originals = {"E", "C#m", "G", "Bb", "", "H"};
    const QStringList asked = {"E", "C#", "Db", "G", "Am", "", "H"};
    for (const QString &chord : chordsShown)
        for (const QString &original : originals)
            for (const QString &key : asked)
                for (int notation = 0; notation <= 3; ++notation)
                    cases.append(QJsonObject {{"chord", chord}, {"originalKey", original}, {"key", key}, {"notation", notation},
                                              {"out", shown(chord, original, key, notation)}});
    write("shown.json",
          "shown(chord, originalKey, key, notation): what is drawn for a chord of the file. It is moved from the key the file's chords are "
          "written in to the key asked for, if they differ and both are given, and then written in the notation.",
          {{"cases", cases}});
}

void keysOwnChords()
{
    QJsonArray cases;
    QStringList keys = allKeys();
    keys << "" << "H" << "x" << "c" << "Cmaj";
    for (const QString &key : std::as_const(keys))
        cases.append(QJsonObject {{"key", key}, {"out", strings(diatonic(key))}});
    write("diatonic.json", "diatonic(key): the seven chords that belong to a key, on each note of its scale in turn: what the keys 1 to 7 give "
                           "in the chord sheet. Empty for what is no key.",
          {{"cases", cases}});
}

void completing()
{
    QJsonArray cases;
    const QStringList typed = {"", "C", "c", "F", "F#", "f#", "Bb", "bb", "G/", "g/", "Am/", "D/F", "D/F#", "Em7", "Gs", "Gsu", "C#m", "Ab/", "A/C#", "B",
                               "Bm7b", "E", "Em", "x", "G/B", "Eb/", "F#m/", "Cmaj", "A", "D", "Dm", "G7"};
    const QStringList keys = {"C", "G", "E", "Bb", "Am", "F#m", "Db", ""};
    const QList<QStringList> used = {{}, {"G", "D", "Em", "C", "G", "D/F#", "G", "Em7", "C", "D"}, {"Bb", "Eb", "F", "Gm", "Bb/D", "Eb", "Bb"}};
    for (const QString &t : typed)
        for (const QString &key : keys)
            for (const QStringList &u : used)
                cases.append(QJsonObject {{"typed", t}, {"key", key}, {"used", strings(u)}, {"out", strings(completions(t, key, u))}});
    write("completions.json",
          "completions(typed, key, used): what a chord being typed might be going to be, likeliest first. `used` is every chord of the song, "
          "one entry for each time it is used. The chord sheet shows the first fourteen.",
          {{"cases", cases}});
}

// ---- In the file: tidy, carried, ranges

const QString verse = QStringLiteral("Amazing grace how sweet the sound\nThat saved a wretch like me");
const QString three = QStringLiteral("I once was lost\nbut now am found\nWas blind but now I see");

void keeping()
{
    struct Case
    {
        QString text;
        QList<Chord> chords;
    };
    const QList<Case> cases = {
        {verse, chordList({{0, "G"}, {8, "G7"}, {17, "C"}, {27, "G"}, {34, "G"}, {45, "D"}})},
        {verse, chordList({{45, "D"}, {0, "G"}, {17, "C"}})},
        {verse, chordList({{0, "G"}, {0, "Em"}, {8, "C"}})},
        {verse, chordList({{33, "D"}, {34, "G"}})},
        {verse, chordList({{33, "D"}, {32, "G"}})},
        {verse, chordList({{500, "D"}, {-4, "G"}})},
        {verse, chordList({{3, ""}, {5, "  "}, {8, " C "}})},
        {verse, chordList({})},
        {QString(), chordList({{0, "G"}})},
        {QStringLiteral("One line only"), chordList({{0, "A"}, {4, "D"}, {9, "E"}, {12, "A"}})},
        {QStringLiteral("One line only"), chordList({{12, "A"}, {13, "E"}})},
        {three, chordList({{0, "C"}, {7, "F"}, {16, "C"}, {24, "G"}, {32, "Am"}, {42, "F"}, {50, "C"}})},
        {three, chordList({{15, "X"}, {16, "Y"}, {31, "Z"}})},
        {QStringLiteral("a\n\nb"), chordList({{0, "C"}, {1, "D"}, {2, "E"}, {3, "F"}})},
        {QStringLiteral("\nwords"), chordList({{0, "C"}, {1, "D"}})},
        {QStringLiteral("x"), chordList({{0, "C"}})},
        {placeholders(3), chordList({{0, "C"}, {2, "G"}, {4, "Am"}})},
        {placeholders(2), chordList({{0, "C"}, {1, "G"}, {2, "Am"}})},
        {QStringLiteral(" "), chordList({{0, "E"}})},
        {verse, chordList({{0, "C /// | Am ///"}, {8, "g"}, {17, "f#m/a"}})},
    };
    QJsonArray tidyCases;
    QJsonArray rangeCases;
    for (const Case &c : cases) {
        tidyCases.append(QJsonObject {{"text", c.text}, {"chords", chordsJson(c.chords)}, {"out", chordsJson(tidy(c.text, c.chords))}});
        QJsonArray out;
        const QList<Range> made = ranges(c.text, c.chords);
        for (const Range &range : made)
            out.append(QJsonObject {{"start", range.start}, {"end", range.end}, {"name", range.name}});
        rangeCases.append(QJsonObject {{"text", c.text}, {"chords", chordsJson(c.chords)}, {"out", out}});
    }
    write("tidy.json",
          "tidy(text, chords): a text box's chords put in order and made fit to be kept. Sorted by place; each on a character the text has; "
          "none on a line break if there is a character before it; no two in one place (the later one given stands); none without a name. "
          "Names are trimmed and otherwise left alone. Places count a line break as one character.",
          {{"cases", tidyCases}});
    write("ranges.json",
          "ranges(text, chords): the stretch of the text each chord is written over in the file. It starts at the chord and runs to the next "
          "chord or to the end of its line, whichever comes first, and is never empty. This is what is written; on reading only `start` is "
          "believed.",
          {{"cases", rangeCases}});

    struct Carry
    {
        QString before;
        QString after;
        QList<Chord> chords;
    };
    const QList<Chord> verseChords = chordList({{0, "G"}, {8, "G7"}, {17, "C"}, {27, "G"}, {34, "G"}, {45, "D"}});
    const QList<Carry> carries = {
        {verse, verse, verseChords},
        {verse, QStringLiteral("Amazing grace how sweet the sound\nThat saved a wretch like you"), verseChords},
        {verse, QStringLiteral("Amazing grace, how sweet the sound\nThat saved a wretch like me"), verseChords},
        {verse, QStringLiteral("Amazing Grace how sweet the sound\nThat saved a wretch like me"), verseChords},
        {verse, QStringLiteral("Amazing grace how sweet the sound\nThat saved a wretch like me\nI once was lost"), verseChords},
        {verse, QStringLiteral("O Amazing grace how sweet the sound\nThat saved a wretch like me"), verseChords},
        {verse, QStringLiteral("Amazing grace\nThat saved a wretch like me"), verseChords},
        {verse, QStringLiteral("Amazing grace how sweet the sound"), verseChords},
        {verse, QString(), verseChords},
        {verse, QStringLiteral("Something else entirely"), verseChords},
        {QStringLiteral("abc def"), QStringLiteral("abc xyz def"), chordList({{0, "C"}, {4, "G"}})},
        {QStringLiteral("abc def ghi"), QStringLiteral("abc ghi"), chordList({{0, "C"}, {4, "G"}, {8, "Am"}})},
        {QStringLiteral("abc def ghi"), QStringLiteral("abc X ghi"), chordList({{4, "G"}, {5, "D"}, {8, "Am"}})},
        {verse, QStringLiteral("Amazing grace how sweet the sound\nThat saved a wretch like you"), {}},
    };
    QJsonArray carryCases;
    for (const Carry &c : carries)
        carryCases.append(QJsonObject {{"before", c.before}, {"after", c.after}, {"chords", chordsJson(c.chords)},
                                       {"out", chordsJson(carried(c.before, c.after, c.chords))}});
    write("carried.json",
          "carried(before, after, chords): the chords of a text box whose words have been changed, where they stand in the new words. What "
          "the old and new words share at their start and at their end is taken to be the same words; a chord in the part that changed goes "
          "to the start of what replaced it. (Neither new app lets words be changed yet; this is for when one does, or meets a file whose "
          "words were changed elsewhere.)",
          {{"cases", carryCases}});
}

// ---- ChordPro

void chordPro()
{
    QJsonArray toLine;
    const QList<Line> lines = {
        {QStringLiteral("Amazing grace how sweet the sound"), chordList({{0, "G"}, {8, "G7"}, {17, "C"}, {27, "G"}})},
        {QStringLiteral("Amazing grace"), chordList({{8, "D"}, {0, "G"}})},
        {QStringLiteral("Amazing grace"), chordList({{3, "G"}, {12, "D"}})},
        {QStringLiteral("Amazing grace"), chordList({})},
        {QStringLiteral("Amazing grace"), chordList({{0, "G"}, {0, "Em"}})},
        {QStringLiteral("Amazing grace"), chordList({{40, "G"}})},
        {QString(), chordList({})},
        {placeholders(3), chordList({{0, "C"}, {2, "G"}, {4, "Am"}})},
        {placeholders(1), chordList({{0, "C"}})},
        {QStringLiteral(" "), chordList({{0, "E"}})},
        {QStringLiteral("     "), chordList({{0, "C"}, {2, "G"}, {4, "D"}})},
        {QStringLiteral("  "), chordList({})},
    };
    for (const Line &line : lines)
        toLine.append(QJsonObject {{"text", line.text}, {"chords", chordsJson(line.chords)}, {"out", toChordPro(line)}});

    QJsonArray fromLine;
    const QStringList written = {
        "[G]Amazing [G7]grace how [C]sweet the [G]sound", "Amazing grace", "[G]", "[C] [G] [Am]", "[C][G][Am]", "  [C]  [G]  ", "Ama[G]zing grace[D]",
        "Amazing grace [D]", "[*Coda]Amazing grace", "[Verse 1]", "[g]amazing [d/f#]grace", "Amazing [grace", "Amazing ]grace[", "[]Amazing",
        "[ G ]Amazing", "[G]  Amazing", "", "   ", "[N.C.]Amazing", "[x]Amazing", "[C /// | Am ///]", "[G][D]Amazing", "A[G]", "[G]A",
        "[Gsus4]Was [G]blind but [D7]now I [G]see", "[A♭]Amen",
    };
    for (const QString &text : written) {
        const Line line = fromChordPro(text);
        fromLine.append(QJsonObject {{"in", text}, {"text", line.text}, {"chords", chordsJson(line.chords)}});
    }

    QJsonArray toText;
    QJsonArray ofText;
    QJsonArray without;
    const QList<std::pair<QString, QList<Chord>>> texts = {
        {verse, chordList({{0, "G"}, {8, "G7"}, {17, "C"}, {27, "G"}, {34, "G"}, {45, "D"}})},
        {three, chordList({{0, "C"}, {7, "F"}, {16, "C"}, {24, "G"}, {32, "Am"}, {42, "F"}, {50, "C"}})},
        {placeholders(3) + QStringLiteral("\nAmazing grace"), chordList({{0, "C"}, {2, "G"}, {4, "Am"}, {6, "G"}})},
        {QStringLiteral("Amazing grace\n\nhow sweet"), chordList({{0, "G"}, {15, "C"}})},
        {QStringLiteral("Amazing grace"), chordList({})},
        {QString(), chordList({})},
    };
    for (const auto &entry : texts) {
        const QString made = toChordPro(entry.first, entry.second);
        toText.append(QJsonObject {{"text", entry.first}, {"chords", chordsJson(entry.second)}, {"out", made}});
    }
    const QStringList whole = {
        "[G]Amazing [G7]grace how [C]sweet the [G]sound\n[G]That saved a [D]wretch like me", "Amazing grace\nhow sweet", "[C] [G] [Am]\n[G]Amazing grace",
        "[]Amazing [ ]grace", "[g]Amazing [xyz]grace [C /// | Am ///]", "Ama[G]zing", "[G", "G]", "", "[G]\n[D]", "{c: Verse 1}\n[G]Amazing",
    };
    for (const QString &text : whole) {
        ofText.append(QJsonObject {{"in", text}, {"out", chordsJson(chordsOf(text))}});
        without.append(QJsonObject {{"in", text}, {"out", withoutChords(text)}});
    }

    QJsonArray standIns;
    for (int count : {0, 1, 2, 3, 5})
        standIns.append(QJsonObject {{"count", count}, {"standIn", zeroWidth}, {"out", placeholders(count)}});
    for (int count : {1, 3})
        standIns.append(QJsonObject {{"count", count}, {"standIn", " "}, {"out", placeholders(count, QChar(u' '))}});
    QJsonArray whether;
    const QStringList asked = {"", " ", "   ", zeroWidth, placeholders(3), emQuad + emQuad, "a", " a ", zeroWidth + "a", "\t", emQuad, "-",
                               placeholders(2, QChar(u' '))};
    for (const QString &line : asked)
        whether.append(QJsonObject {{"in", line}, {"out", isPlaceholders(line)}});

    write("chordpro.json",
          "Chords as ChordPro text and back. `toLine`: toChordPro(line), each chord in square brackets before the character it stands at. "
          "`fromLine`: fromChordPro(text), used when a ChordPro FILE is read: brackets holding what could not be a chord are dropped, and a "
          "line of chords alone gets a stand-in character for each. `toText`: a whole text box of several lines. `chordsOf` and "
          "`withoutChords`: what the ChordPro EDITOR reads back from what was typed (places along the words as they are, no stand-ins made). "
          "`placeholders` and `isPlaceholders`: the stand-ins a chord with no words hangs on. NOTE that some strings here hold characters "
          "that cannot be seen: U+200B (zero-width space) and U+2001 (em quad).",
          {{"toLine", toLine}, {"fromLine", fromLine}, {"toText", toText}, {"chordsOf", ofText}, {"withoutChords", without},
           {"placeholders", standIns}, {"isPlaceholders", whether}});
}

QJsonArray linesJson(const QList<Line> &lines)
{
    QJsonArray array;
    for (const Line &line : lines)
        array.append(QJsonObject {{"text", line.text}, {"chords", chordsJson(line.chords)}});
    return array;
}

void songs(const QString &songFolder)
{
    QJsonArray cases;
    const QStringList files = QDir(songFolder).entryList({"*.cho", "*.chordpro", "*.txt"}, QDir::Files, QDir::Name);
    for (const QString &name : files) {
        QFile file(songFolder + u'/' + name);
        if (!file.open(QIODevice::ReadOnly))
            continue;
        const QString text = QString::fromUtf8(file.readAll());
        const Song song = parseSong(text);
        QJsonArray sections;
        for (const Section &section : song.sections) {
            QJsonObject slides;
            for (int each : {1, 2, 3, 4}) {
                QJsonArray dealt;
                const QList<QList<Line>> made = slidesOf(section, each);
                for (const QList<Line> &slide : made)
                    dealt.append(int(slide.size()));
                slides[QString::number(each)] = dealt;
            }
            sections.append(QJsonObject {{"name", section.name}, {"lines", linesJson(section.lines)}, {"linesOnEachSlide", slides}});
        }
        cases.append(QJsonObject {{"file", name}, {"title", song.title}, {"artist", song.artist}, {"key", song.key}, {"copyright", song.copyright},
                                  {"ccli", song.ccli}, {"sections", sections}});
    }
    write("parse-song.json",
          "parseSong(text) for each ChordPro file in testdata/songs: what the song is, and its parts with their lines and chords (each `at` "
          "counted along its line). `linesOnEachSlide` is slidesOf(section, n) for n of 1 to 4: how many lines land on each slide.",
          {{"cases", cases}});
}

// ---- The text of an RTF

void rtfText()
{
    struct Sample
    {
        const char *name;
        const char *about;
        QByteArray rtf;
    };
    const QByteArray head = "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2869\n\\cocoatextscaling0\\cocoaplatform0{\\fonttbl\\f0\\fswiss\\fcharset0 CenturyGothic;}\n"
                            "{\\colortbl;\\red255\\green255\\blue255;\\red255\\green255\\blue255;}\n"
                            "{\\*\\expandedcolortbl;;\\cssrgb\\c100000\\c99997\\c99999;}\n"
                            "\\pard\\pardirnatural\\qc\\partightenfactor0\n\n\\f0\\fs160 \\cf2 ";
    const QList<Sample> samples = {
        {"two lines as ProPresenter on a Mac writes them", "A paragraph break is a backslash at the end of the line.",
         head + "Amazing grace how sweet the sound\\\nThat saved a wretch like me}"},
        {"one line", "", head + "Amazing grace}"},
        {"three lines", "", head + "I once was lost\\\nbut now am found\\\nWas blind but now I see}"},
        {"an empty line between", "Two breaks running: an empty line, which is a character like any other break.",
         head + "Amazing grace\\\n\\\nhow sweet the sound}"},
        {"a break at the very end", "The text then ends with an empty line.", head + "Amazing grace\\\n}"},
        {"par for a paragraph", "As other writers of RTF put it.", head + "Amazing grace\\par That saved a wretch\\par like me}"},
        {"line for a line break", "A break inside a paragraph is one character too.", head + "Amazing grace\\line how sweet\\\nthe sound}"},
        {"a curly apostrophe by code page", "\\'92 is one character, U+2019.", head + "Twas grace that taught my heart\\'92s fear}"},
        {"a curly apostrophe by number", "\\u8217 with one character after it to be passed over.", head + "my heart\\u8217 's fear}"},
        {"no character to pass over", "\\uc0 says so, as ProPresenter writes its stand-ins.", head + "\\uc0\\u8203 \\uc0\\u8193 \\uc0\\u8203 }"},
        {"stand-ins as Multitracks songs have them", "Spaces and em quads, on two lines.", head + " \\uc0\\u8193  \\uc0\\u8193  \\\n \\uc0\\u8193  }"},
        {"a character beyond the first sixty-five thousand", "Two numbers, which are two places as the file counts.",
         head + "Joy \\uc0\\u55356 \\u57225  to the world}"},
        {"a negative number", "RTF writes numbers over 32767 as negative.", head + "caf\\u-23 ?}"},
        {"escaped braces and backslash", "", head + "a \\{b\\} c \\\\ d}"},
        {"a tab, and a space that does not break", "", head + "a\\tab b\\~c}"},
        {"an optional hyphen, and one that does not break", "The first is nothing; the second is U+2011.", head + "a\\-b\\_c}"},
        {"UTF-8 written straight in", "Not strict RTF, but some programs do it.", head + QByteArray("caf\xC3\xA9 na\xC3\xAFve}")},
        {"a byte that is not UTF-8", "Read by the code page.", head + QByteArray("caf\xE9}")},
        {"two fonts and a bold word", "Format does not change the characters.",
         "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2869\n{\\fonttbl\\f0\\fswiss\\fcharset0 Helvetica;\\f1\\fswiss\\fcharset0 Helvetica-Bold;}\n"
         "{\\colortbl;\\red255\\green255\\blue255;}\n\\pard\\qc\n\\f0\\fs96 \\cf1 Amazing \\f1\\b grace\\f0\\b0  how sweet\\\nthe sound}"},
        {"a group inside the text", "", head + "Amazing {\\i grace} how sweet}"},
        {"line ends in the RTF that are not breaks", "A bare line end in the file is nothing.", head + "Amazing\n grace\r\nhow sweet}"},
        {"nothing", "", head + "}"},
        {"only a space", "", head + " }"},
    };
    QJsonArray cases;
    for (const Sample &sample : samples) {
        const QString text = parseRtf(sample.rtf).plainText();
        QJsonArray units;
        for (const QChar c : text)
            units.append(int(c.unicode()));
        cases.append(QJsonObject {{"name", sample.name}, {"about", sample.about}, {"rtfBase64", QString::fromLatin1(sample.rtf.toBase64())},
                                  {"text", text}, {"length", int(text.size())}, {"units", units}});
    }
    write("rtf-text.json",
          "The words of a text box as they are counted: the RTF the file keeps (given in base64, since some samples hold bytes that are not "
          "text) and the plain text read from it, with its length and every UTF-16 unit of it. A chord's place in the file is a place in "
          "this text. A paragraph break and a line break are one character each, written here as a line feed.",
          {{"cases", cases}});
}

}

int main(int argc, char **argv)
{
    QCoreApplication app(argc, argv);
    if (argc < 2) {
        QTextStream(stderr) << "make-vectors <folder to write into>\n";
        return 2;
    }
    outFolder = QString::fromLocal8Bit(argv[1]);
    QDir().mkpath(outFolder);
    keys();
    chordNames();
    transposing();
    notating();
    showing();
    keysOwnChords();
    completing();
    keeping();
    chordPro();
    songs(QDir(outFolder).absoluteFilePath("../songs"));
    rtfText();
    return 0;
}
