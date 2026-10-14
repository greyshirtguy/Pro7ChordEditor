#pragma once

#include <QList>
#include <QString>
#include <QStringList>

#include <functional>

// The line of hints under the chord sheet: what the keys and the mouse do just now,
// where the spot is. Which hints are said, and how they are fitted into the width the
// line has. Plain functions, with nothing of the windows in them (tests/unit/
// tst_sheethints.cpp); qml/HintLine.qml draws what they answer, and ChordSheet.qml
// says what is going on.
//
// Why. Under the sheet there used to be one sentence that never changed and said
// everything at once: ten things, of which the one wanted was seldom the one read. The
// maintainer, after two days with the Mac's chord editor, "did not realise I could
// hover and press delete". So the line says less, and says what goes with what is
// under the pointer:
//
//   on a chord            Delete takes it off; 1 to 7 change it; it can be dragged,
//                         and with Ctrl a copy of it; a click opens it to be typed
//                         over;
//   on a place with none  1 to 7 put one on; a letter or a click types one;
//   while one is typed    what Enter, Tab and Esc do with it, and with the box
//                         emptied on a chord, that Enter takes the chord off;
//   while one is carried  that letting go moves it, and Ctrl leaves a copy; or, for
//                         one of the key's chords from the strip, puts it there;
//   with Ctrl held        the copying of a line's chords onto another: Ctrl+C and
//                         Ctrl+V, which no picture on the sheet shows;
//   over the key's chords that they can be dragged down onto a slide;
//   anywhere else         where to point to be told.
//
// Nothing is said by the clock (for a day the Mac's went through ten tips, one every
// nine seconds; the maintainer: "love how they appear at the right time - lets drop
// the randomly timed tips"). Each begins with a capital, as a sentence does ("Let's
// sentence case the tips").
//
// It is the Mac and Windows chord editors' (Pro7 Chord Editor's spec/03-chord-sheet.md,
// "The line of hints", rules 53a to 53h, tried on the Mac and agreed on 2026-10-14),
// brought here at the maintainer's word the same day: "I like the the automatic hints-
// so yeah". The words are theirs. The keys are named in words, as Windows names them
// and as Linux does too; and the key that brings up the copying is Ctrl, Linux's key
// for shortcuts as Command is the Mac's (the maintainer: "it be shown when user holds
// ctrl key on linux"). Ctrl is also the key that copies a chord as it is dragged here,
// as on Windows. What they have and this has not: a row for ProPresenter running, since
// nothing here asks whether it is.
namespace sheethints {

// What the person is about, as far as what is worth saying goes.
enum class Doing {
    Nothing,  // not at the sheet (its rule 53c), and nothing typed or carried
    Typing,   // a chord is being typed
    Carrying, // a chord is being carried
    OnChord,  // at the sheet, the spot on a chord
    OnPlace,  // at the sheet, the spot on a place with none
};

struct Situation
{
    Doing doing = Doing::Nothing;
    bool onChord = false;   // while typing: there is a chord at that place already
    bool empty = false;     // while typing: the box has nothing in it
    bool fromStrip = false; // while carrying: one of the key's, out of the strip
};

// What a paste would do just now, which the hint for it says before the key is pressed.
struct Paste
{
    int lines = 0;         // how many lines have been copied; 0 if none have
    bool replaces = false; // whether it would take chords off the lines it goes onto
};

// One hint: words before its keys where it is a sentence ("Press"), the keys it is
// about, drawn as keys, and what they do. One that is `needed` is said whatever the
// room: where the line has not room for all its hints the last are left off, but not
// one of these, for which the line is drawn smaller first.
struct Hint
{
    QString lead;
    QStringList keys;
    QString says;
    bool needed = false;

    // As it would be read out
    QString read() const;
    bool operator==(const Hint &other) const = default;
};

// The keys, as they are named on them
inline const QString Delete = QStringLiteral("Delete");
inline const QString Enter = QStringLiteral("Enter");
inline const QString Tab = QStringLiteral("Tab");
inline const QString Escape = QStringLiteral("Esc");
inline const QString Ctrl = QStringLiteral("Ctrl");
inline const QString Up = QStringLiteral("Up");
inline const QString Down = QStringLiteral("Down");
inline const QString Numbers = QStringLiteral("1 – 7");
inline const QString Letters = QStringLiteral("A – G");

// What to say, the first the most worth reading. `ctrl`: whether Ctrl is down. At the
// sheet it brings up the copying of a line's chords, and nothing else does; while a
// chord of the sheet's own is carried it says what letting go will do. `overStrip`:
// the pointer is over the key's chords in the strip, which is not the sheet.
QList<Hint> hints(const Situation &situation, bool ctrl, const Paste &paste, bool overStrip);

// ---- Fitting them in
//
// Arithmetic, on widths that are measured once for each run of words and kept, and not
// the window asked how big the line would like to be: what the line says changes as
// often as the pointer crosses a chord, and whatever is asked of the window's laying
// out then is asked that often. So how tall the line is, is worked out here, and a
// change of its words lays out nothing unless the line itself is to be taller. (The
// Windows app's HintFit, sum for sum.)

// How they are fitted: how many of them, from the first; whether one under another
// rather than side by side; and how many times their usual size.
struct Fit
{
    int count = 0;
    bool stacked = false;
    double size = 1;

    bool operator==(const Fit &other) const = default;
};

// How wide some words are at a size in pixels, plain or (for the sign on a key) a little
// bolder: asked of whatever draws them.
using Measure = std::function<double(const QString &words, int pixels, bool onAKey)>;

// The sizes the needed hints are tried at, one under the other: as they come, and two
// steps smaller. Then the sizes of the words and of a key's sign; the room between two
// hints, between the parts of one, and between two lines of them; how tall the line is
// and the room at its sides and over and under what it says; and how tall a key is
// drawn, the least width of one, and the room its sign has either side.
inline const QList<double> Sizes {1.0, 0.9, 0.8};
constexpr double WordSize = 13;
constexpr double KeySize = 12;
constexpr double Between = 18;
constexpr double Within = 5;
constexpr double Lines = 4;
constexpr double Height = 30;
constexpr double Sides = 12;
constexpr double OverAndUnder = 5;
constexpr double KeyHeight = 20;
constexpr double KeyLeast = 22;
constexpr double KeyPad = 6;

// A size of the words or of a key's sign, in whole pixels, as text is drawn here
int pixels(double usual, double size);
// How wide a key is drawn: its sign and the room either side, and never less than a key
double widthOfKey(const QString &key, double size, const Measure &measure);
// How wide one hint is, and the first so many side by side
double widthOf(const Hint &hint, double size, const Measure &measure);
double widthOf(const QList<Hint> &hints, int count, double size, const Measure &measure);
// How they are fitted into the room they have, the line's sides taken off
Fit fit(const QList<Hint> &hints, double room, const Measure &measure);
// How tall the line is for a fit: its usual height, or what the hints need one under
// another, if that is more
double heightOf(const Fit &fit);

}
