#pragma once

#include <QByteArray>
#include <QList>
#include <QString>

#include <optional>

// Stand-ins written into an RTF where they stand: one line of a text box's words, a
// line of chords with no words, given more stand-in characters or fewer by changing
// the bytes of that line's own characters in the RTF and not another byte of it.
//
// Why. A chord with no words under it (an intro, a turnaround) hangs in the file on an
// invisible character, a stand-in (chords::placeholders). Putting one more chord on
// such a line, or taking one off, changes how many stand-ins the line needs, and so
// the RTF the words are kept in: the one case in which the chord editors write words.
// This app can do that by reading the RTF into its model of formatted text and writing
// the whole of it again in its own words (rtfwriter.h), which is what it does when
// text is edited. But that rewrites a text box for the sake of one invisible line: what
// the model does not carry (tab stops, indents, lists) goes, and the settings
// ProPresenter keeps beside the words are set again from the first of them. Changed
// where it stands, the RTF is ProPresenter's own to the byte but for that line, which
// is what AGENTS.md asks of every write. It is how the chord editors for the Mac and
// for Windows do it (Pro7 Chord Editor, which began as a copy of this app's), and the
// rule here is theirs: tools/standins.py in that repository is its reference, and this
// file is held to the same answers (tests/unit/tst_rtfstandins, on that repository's
// own cases), so that all three write the same bytes.
//
// The rule, for a line that is to become a given run of stand-ins:
//   1. Read the RTF, keeping for every unit of its plain text the bytes it was written
//      with: a plain character's one byte; a control word's backslash, letters, number
//      and the space that ends it, if it has one; the four of an apostrophe and two
//      hexadecimal digits; a character written straight in as UTF-8, its bytes.
//   2. The line is the units between two line breaks. It must have at least one, or
//      there is nothing to say where it is.
//   3. What the line has already of what it is to become, counted from its start, is
//      kept as it is written.
//   4. The bytes of every unit of the line after that are taken out: the units' own,
//      and none of the bytes that lie between them. Those are control words, which say
//      how what follows is set (in a song from Multitracks every other stand-in is in
//      another font), and they stay.
//   5. What is still to come of the stand-ins is put where the first unit taken out
//      began, or, if none was taken out, just after the line's last unit. A plain
//      space is written as a space; a zero-width space and an em quad as Apple's text
//      system writes any character above ASCII: a backslash, "uc0", a backslash, "u",
//      the unit's number and a space.
//   6. If what is put in starts with a plain space, and a control word with no space
//      of its own ends exactly there, a space is put in first to end that word.
//   7. The result is read again. If its plain text is not the old text with that line
//      changed and nothing else, there is no result.
//
// Where there is no result the RTF is of a kind these steps were not written for. One
// such kind is known of: an RTF that has a character follow each numbered one to stand
// in for it, for readers that do not know numbered characters, which Apple's text
// system never writes. What ProPresenter on Windows writes is NOT KNOWN. Whoever asks
// then does it the other way (PresentationEditor::setChordsAlone writes the text box
// again, as before), where the Mac and Windows editors, which have no writer of RTF,
// refuse.
//
// The reader in this file is a second one beside rtf.h's, and is no model of the text:
// it keeps the words and where each was written, and passes over everything else. It
// is written to the same rules as rtf.h's reader, and whoever uses the result is to
// read it with that one as well before trusting it.
namespace rtfstandins {

// One unit of an RTF's plain text (a UTF-16 unit, which is how the file counts), and
// where in the RTF it is written.
struct Unit
{
    char16_t character = 0;
    // The unit's own bytes, from the first to one past the last
    qsizetype start = 0;
    qsizetype end = 0;
    // A control word with no space after it ends exactly where the unit begins; and the
    // unit is itself such a word. A plain space put in at either place would be taken
    // for the word's end, and would not be text.
    bool openBefore = false;
    bool openAfter = false;
    // How many groups are open there
    int depth = 0;
};

// The plain text of an RTF, unit by unit. A paragraph break and a line break are one
// line feed each. `ok` is set false if the RTF breaks off in the middle of something,
// or numbers a character beyond what one unit holds.
QList<Unit> written(const QByteArray &rtf, bool *ok = nullptr);

// The same as text.
QString plainText(const QByteArray &rtf);

// The RTF with one line of its plain text made `wanted`, a run of stand-ins (plain
// spaces, zero-width spaces and em quads, and nothing else), and every byte that is
// not one of that line's characters left as it was. Nothing if it cannot be done
// cleanly: there is no such line, the line is empty, `wanted` holds something else, or
// what was made does not read back as meant.
std::optional<QByteArray> withLine(const QByteArray &rtf, int line, const QString &wanted);

// Where a place in the words is, once a line of them that began at `lineStart` and was
// `was` units long has been made `now` units long by putting units on its end or
// taking them off it (which is what a run of stand-ins made longer or shorter is).
//
// It is for what a text box keeps beside its words that is said of a range of them and
// is not a chord: "these are in capitals", a gradient over some of them. A range is two
// such places, its start and its end. A place before the line's end stays, or comes
// back to the line's new end if what it was on is gone; a place at the line's old end
// or past it moves by as much as the line grew or shrank. So a range that ended with
// the line, or went on past it, still does; one that began after the line is over the
// same words as before; and a range over all the words is over all of them still. It is
// what a text system does with what is set on characters when more are typed after
// them, and so what ProPresenter is taken to do, WHICH IS A GUESS.
//
// (This is not in the rule the other two editors have, which leave those ranges as
// they are. In real songs that matters: most text boxes that hold a line of chords
// alone, as Multitracks makes them, say of all their words that they are in capitals,
// and with a chord taken off, that range left alone would end past the last of the
// words.)
int placeAfter(int place, int lineStart, int was, int now);

} // namespace rtfstandins
