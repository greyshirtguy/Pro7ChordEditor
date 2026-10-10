#pragma once

#include <QList>
#include <QString>
#include <QStringList>

// Chords over the words of a song: what one is, how it is moved to another key, how it
// is written in each of ProPresenter's four notations, and how it is read from and
// written as ChordPro text.
//
// Nothing here knows of a file, a window or the app: it is plain functions over
// strings and numbers, so that the rules can be tried by themselves
// (tests/unit/tst_chords.cpp). proconvert.h has how chords are kept in a
// presentation's file; this is only what they mean.
//
// What a chord is taken to be. A chord's name is a root note (a letter A to G, and
// perhaps a sharp or a flat), then whatever says what kind of chord it is ("m7",
// "sus4", "maj7", "add9", nothing at all), then perhaps a slash and a bass note. Only
// the two notes are understood. What stands between them is carried as it is, which is
// why a chord this app has never heard of ("G1", which one of the Multitracks files
// has) is still moved to another key rightly.
//
// The key. ProPresenter keeps a presentation's key as one of twenty-one names (every
// letter plain, sharp and flat, as its file format lists them: A flat is 0 and G sharp
// is 20) and whether it is major or minor. Here a key is written the way a musician
// writes one, "E" or "Bb" or "F#m", and keyNumber() and keyName() go between the two.
//
// In the file a chord is written in the presentation's original key, whatever key it
// is being shown in. (Checked against files that Multitracks made and that
// ProPresenter then showed in other keys: a song in E shown in C sharp still has E, A
// and B in it.) So a chord is always moved from the original key to the key asked for
// on its way to being drawn, by shown(), and the file is never rewritten to change key.
namespace chords {

// One chord, standing before the character at `at` of its text box's words (counted as
// QString counts, which is how the file counts them too).
struct Chord
{
    int at = 0;
    QString name;

    bool operator==(const Chord &other) const { return at == other.at && name == other.name; }
};

// ---- Keys

// A key by the number the file has for it, as its name: "Ab" for 0 and so on, with "m"
// after it for a minor one. And the number and whether it is minor for a name; -1 for a
// name that is no key.
QString keyName(int number, bool minor);
int keyNumber(const QString &key);
bool keyIsMinor(const QString &key);
// The keys a picker offers, in the order of the notes from C: the twelve major keys by
// their usual names (with both F# and Gb, which are equally usual), and the same for
// minor.
QStringList majorKeys();
QStringList minorKeys();

// ---- A chord's parts

struct Parts
{
    QString root;    // "F#"
    QString quality; // "m7", or "" for a plain major chord
    QString bass;    // "A#", or "" for none
    bool valid = false;
};
// Takes a chord's name apart. Not valid if it does not start with a note.
Parts parts(const QString &chord);
// Whether this could be the start of a chord that is being typed: empty, or a note,
// or a note and anything after it that a chord's name is written with. It is what the
// editors let through as a chord is typed.
bool couldBecome(const QString &typed);
// A typed chord tidied: the note letters as capitals and "b" for a flat ("f#M7/a" stays
// "F#M7/A": what is between the notes is the writer's business).
QString tidied(const QString &typed);

// ---- From one key to another, and from one notation to another

// The chord as it is in another key. Each note is moved by as many letters and as many
// semitones as the keys are apart, so that it is spelt as that key spells it (C sharp
// minor in E is B flat minor in D flat, not A sharp minor); a spelling that would need
// a double sharp or flat gives way to the plain name of the same note. A chord that
// cannot be read, or a key that is no key, gives the chord back as it was.
QString transposed(const QString &chord, const QString &fromKey, const QString &toKey);

// ProPresenter's four ways of writing a chord, numbered as its file numbers them.
enum Notation { Letters = 0, Numbers = 1, Numerals = 2, DoReMi = 3 };
// The chord, which is in `key`, in a notation.
//   Letters   as it is: "C#m7/E"
//   Numbers   by the note's place in the key's scale: "6m7/1" in E
//   Numerals  the same in Roman numbers, small for a minor chord: "vi7/I"
//   DoReMi    by the notes' names in the Latin countries, where C is always Do:
//             "Do#m7/Mi"
// A GUESS, in the last three: ProPresenter does not say what exactly it writes and
// none of the files to hand shows it, so these are the forms musicians use. For a
// minor key the places are counted from its relative major (A minor's are C's), which
// is how number charts are usually written.
QString notated(const QString &chord, const QString &key, int notation);
// What is drawn for a chord of the file: moved from the original key to the key asked
// for (if they differ), and then written in the notation.
QString shown(const QString &chord, const QString &originalKey, const QString &key, int notation);

// ---- Helping a chord to be entered quickly

// The seven chords that belong to a key, on each note of its scale in turn: for C, "C",
// "Dm", "Em", "F", "G", "Am", "Bdim". They are what the keys 1 to 7 give in the chord
// editor.
QStringList diatonic(const QString &key);
// What a chord being typed might be going to be, likeliest first, for a list to pick
// from: the chords already used in the song that start so, then the key's own, then
// the usual kinds of chord on the note typed, and after a slash the usual bass notes.
// With nothing typed it is the song's chords and the key's.
QStringList completions(const QString &typed, const QString &key, const QStringList &used);

// ---- In the file

// The chords of a text box put in order and made fit to be kept: sorted by place, each
// before a character the text has (one after the end goes to the last), no two in one
// place (the later one given stands), none without a name.
QList<Chord> tidy(const QString &text, QList<Chord> chords);

// The chords of a text box whose words have been changed, where they stand in the new
// words. What the old and new words have in common at their start and at their end is
// taken to be the same words: a chord there stays on its character. A chord in the
// part that changed goes to the start of what replaced it. So correcting a word, or
// adding a line, leaves the chords of the rest where they were.
QList<Chord> carried(const QString &before, const QString &after, const QList<Chord> &chords);

// The stretch of the text each chord is written over in the file. ProPresenter does
// not keep where a chord stands but a stretch of characters it belongs to, which in
// every file to hand starts at the chord and runs to the next chord or to the end of
// its line, whichever comes first, and is never empty.
struct Range
{
    int start = 0;
    int end = 0;
    QString name;
};
QList<Range> ranges(const QString &text, const QList<Chord> &chords);

// ---- ChordPro

// A line of words and its chords (each `at` counted along the line).
struct Line
{
    QString text;
    QList<Chord> chords;

    bool operator==(const Line &other) const { return text == other.text && chords == other.chords; }
};
// A line as ChordPro writes it, each chord in square brackets before the character it
// stands at: "[C]Amazing [F]grace". And the other way about. Brackets that hold what
// could not be a chord (an annotation such as "[*Coda]") are dropped on the way in.
QString toChordPro(const Line &line);
Line fromChordPro(const QString &line);
// A whole text box's words, of several lines, with its chords; and back.
QString toChordPro(const QString &text, const QList<Chord> &chords);
QList<Chord> chordsOf(const QString &chordPro);
// ChordPro text with its chords taken out: the words alone. An editor that lets only
// the chords be changed compares this with the words it started from.
QString withoutChords(const QString &chordPro);

// What stands for a chord with no words under it. Multitracks writes a line of chords
// alone (an intro, a turnaround) as one character for each chord, with a wide space
// between them, and hangs each chord on its character. In the files to hand the
// character is a zero-width space (U+200B) in some songs and a plain space in others,
// and what is between is always an em quad (U+2001); a single chord on a slide of its
// own hangs on one space. An import here writes zero-width spaces, and a line that is
// changed keeps whichever it had (`standIn`).
QString placeholders(int count, QChar standIn = QChar(0x200B));
// Whether a line is one of those: not empty, and nothing in it but stand-ins and
// spaces. (A line of plain spaces counts. Whether it is a line of chords or only a
// stray space in a text box is for whoever asks to tell, by whether it has chords.)
bool isPlaceholders(const QString &line);

// A song read from a ChordPro file.
struct Section
{
    // "Verse 1", "Chorus", or "" where the file names none
    QString name;
    QList<Line> lines;
};
struct Song
{
    QString title;
    QString artist;
    QString key;
    QString copyright;
    QString ccli;
    QList<Section> sections;
};
// Reads ChordPro text. The directives that say what the song is are kept ({title},
// {artist}, {key}, {copyright}, {ccli}, and their short forms); those that mark its
// parts make sections ({start_of_verse}, {start_of_chorus}, {start_of_bridge} and
// their ends and short forms, with a label if one is given; {comment: Verse 1} and a
// line that is only "Verse 1:" or "[Verse 1]" too, since most files found in the wild
// mark their parts that way). An empty line ends a section that has no name, so that a
// file with no directives at all still comes in as its verses. Tablature and anything
// else in a {start_of_...} this does not know is left out; so is a line starting "#".
Song parseSong(const QString &text);

// A section's lines dealt into slides of so many lines each (the last may have fewer).
QList<QList<Line>> slidesOf(const Section &section, int linesPerSlide);

}
