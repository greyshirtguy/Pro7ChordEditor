// What a chord is, how it changes key and notation, and ChordPro (src/chords.h).

#include "chords.h"

#include <QTest>

using namespace chords;

class TestChords : public QObject
{
    Q_OBJECT

private slots:
    void keysGoBetweenTheFilesNumbersAndTheirNames()
    {
        QCOMPARE(keyName(0, false), "Ab");
        QCOMPARE(keyName(13, false), "E");
        QCOMPARE(keyName(17, true), "F#m");
        QCOMPARE(keyName(21, false), "");
        QCOMPARE(keyNumber("E"), 13);
        QCOMPARE(keyNumber("F#m"), 17);
        QCOMPARE(keyNumber("H"), -1);
        QVERIFY(keyIsMinor("Am") && !keyIsMinor("A") && !keyIsMinor("m"));
        for (const QString &key : majorKeys() + minorKeys())
            QVERIFY2(keyNumber(key) >= 0, qPrintable(key));
    }

    void aChordIsARootWhatKindItIsAndPerhapsABass()
    {
        const Parts plain = parts("E");
        QVERIFY(plain.valid && plain.root == "E" && plain.quality.isEmpty() && plain.bass.isEmpty());
        const Parts full = parts("F#m7/A#");
        QVERIFY(full.root == "F#" && full.quality == "m7" && full.bass == "A#");
        const Parts flat = parts("Bbsus4/Eb");
        QVERIFY(flat.root == "Bb" && flat.quality == "sus4" && flat.bass == "Eb");
        // A slash that is part of the chord's kind, not a bass
        const Parts sixNine = parts("C6/9");
        QVERIFY(sixNine.root == "C" && sixNine.quality == "6/9" && sixNine.bass.isEmpty());
        // A kind this has never heard of is carried
        QCOMPARE(parts("G1").quality, "1");
        QVERIFY(!parts("Hm").valid && !parts("").valid && !parts("*Coda").valid);
    }

    void typingIsHeldToWhatCouldBeAChord()
    {
        QVERIFY(couldBecome("") && couldBecome("c") && couldBecome("C#") && couldBecome("C#m7b5/") && couldBecome("Asus2/E"));
        QVERIFY(!couldBecome("x") && !couldBecome("1") && !couldBecome("C m") && !couldBecome("C]"));
        QCOMPARE(tidied(" f#m7/a "), "F#m7/A");
        QCOMPARE(tidied("bb"), "Bb");
        QCOMPARE(tidied("c6/9"), "C6/9");
    }

    void aChordIsMovedToAnotherKeyAndSpeltAsThatKeySpellsIt()
    {
        // As ProPresenter shows the Multitracks songs to hand: E to C sharp, A to D flat
        QCOMPARE(transposed("C#m", "E", "C#"), "A#m");
        QCOMPARE(transposed("B/D#", "E", "D"), "A/C#");
        QCOMPARE(transposed("F#m", "A", "Db"), "Bbm");
        QCOMPARE(transposed("D", "A", "Db"), "Gb");
        QCOMPARE(transposed("Asus2/E", "E", "G"), "Csus2/G");
        QCOMPARE(transposed("G1", "G", "A"), "A1");
        // Nothing to do, and nothing that can be done
        QCOMPARE(transposed("Am7", "C", "C"), "Am7");
        QCOMPARE(transposed("N.C.", "C", "D"), "N.C.");
        QCOMPARE(transposed("Am7", "C", "H"), "Am7");
        // There and back is where it started, in every key
        for (const QString &key : majorKeys()) {
            for (const QString &chord : diatonic("E"))
                QCOMPARE(transposed(transposed(chord, "E", key), key, "E"), chord);
        }
        // A note outside the key keeps its place: the flat seventh of C is that of D
        QCOMPARE(transposed("Bb", "C", "D"), "C");
        // No double sharps
        QVERIFY(!transposed("D#", "E", "G#").contains("##"));
    }

    void everyChordLandsOnTheRightNoteInEveryKey()
    {
        // The note a name stands for, counted in semitones from C.
        const auto pitch = [](const QString &note) {
            static const QString letters = QStringLiteral("CDEFGAB");
            static const int above[] = {0, 2, 4, 5, 7, 9, 11};
            int value = above[letters.indexOf(note.at(0))];
            for (const QChar c : note.mid(1))
                value += c == u'#' ? 1 : c == u'b' ? -1 : 0;
            return ((value % 12) + 12) % 12;
        };
        QStringList keys;
        for (int number = 0; number < 21; ++number)
            keys << keyName(number, false) << keyName(number, true);
        const QStringList roots {"C", "C#", "Db", "D", "D#", "Eb", "E", "F", "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B", "Cb", "E#", "B#", "Fb"};
        int tried = 0;
        for (const QString &from : keys) {
            for (const QString &to : keys) {
                const int rise = (pitch(to.endsWith(u'm') ? to.chopped(1) : to) - pitch(from.endsWith(u'm') ? from.chopped(1) : from) + 12) % 12;
                for (const QString &root : roots) {
                    const QString chord = root + QStringLiteral("m7/") + roots.at((roots.indexOf(root) + 5) % roots.size());
                    const Parts before = parts(chord);
                    const Parts after = parts(transposed(chord, from, to));
                    const QByteArray what = (chord + " from " + from + " to " + to + " gave " + transposed(chord, from, to)).toUtf8();
                    QVERIFY2(after.valid && after.quality == "m7", what.constData());
                    QVERIFY2(pitch(after.root) == (pitch(before.root) + rise) % 12, what.constData());
                    QVERIFY2(pitch(after.bass) == (pitch(before.bass) + rise) % 12, what.constData());
                    // Never a double sharp or flat, which nobody reads at a glance
                    QVERIFY2(after.root.size() <= 2 && after.bass.size() <= 2, what.constData());
                    ++tried;
                }
            }
        }
        QCOMPARE(tried, 42 * 42 * 21);
    }

    void theFourNotations()
    {
        QCOMPARE(notated("C#m7/E", "E", Letters), "C#m7/E");
        QCOMPARE(notated("C#m7/E", "E", Numbers), "6m7/1");
        QCOMPARE(notated("C#m7/E", "E", Numerals), "vi7/I");
        QCOMPARE(notated("C#m7/E", "E", DoReMi), "Do#m7/Mi");
        QCOMPARE(notated("A", "E", Numbers), "4");
        QCOMPARE(notated("Bsus4", "E", Numerals), "Vsus4");
        QCOMPARE(notated("Bb", "C", Numbers), "b7");
        QCOMPARE(notated("Bdim", "C", Numerals), "vii°");
        QCOMPARE(notated("Gb", "Db", DoReMi), "Solb");
        // A minor key counts from its relative major
        QCOMPARE(notated("Am", "Am", Numbers), "6m");
        QCOMPARE(notated("C", "Am", Numerals), "I");
        // Moved and then written
        QCOMPARE(shown("C#m", "E", "D", Letters), "Bm");
        QCOMPARE(shown("C#m", "E", "D", Numbers), "6m");
        QCOMPARE(shown("C#m", "E", "", Letters), "C#m");
    }

    void aKeysOwnChords()
    {
        QCOMPARE(diatonic("C"), QStringList({"C", "Dm", "Em", "F", "G", "Am", "Bdim"}));
        QCOMPARE(diatonic("E"), QStringList({"E", "F#m", "G#m", "A", "B", "C#m", "D#dim"}));
        QCOMPARE(diatonic("Bb"), QStringList({"Bb", "Cm", "Dm", "Eb", "F", "Gm", "Adim"}));
        QCOMPARE(diatonic("Am"), QStringList({"Am", "Bdim", "C", "Dm", "Em", "F", "G"}));
        QVERIFY(diatonic("H").isEmpty());
    }

    void whatAChordBeingTypedMightBe()
    {
        // The song's own chords first, the most used first, then the key's
        const QStringList used {"G", "D/F#", "D/F#", "Em7"};
        QCOMPARE(completions("", "G", used).mid(0, 4), QStringList({"D/F#", "G", "Em7", "Am"}));
        const QStringList d = completions("d", "G", used);
        QCOMPARE(d.first(), "D/F#");
        QVERIFY(d.contains("D") == false && d.contains("Dm") && d.contains("Dsus4") && d.contains("D7"));
        QVERIFY(!d.contains("G"));
        // After a slash: the third and the fifth first
        const QStringList bass = completions("D/", "G", {});
        QCOMPARE(bass.mid(0, 2), QStringList({"D/F#", "D/A"}));
        QCOMPARE(completions("Am/", "C", {}).first(), "Am/C");
        // Then the notes of the key, and never the chord's own root
        QVERIFY(bass.contains("D/G") && bass.contains("D/E") && !bass.contains("D/D"));
        QVERIFY(completions("x", "C", {}).isEmpty());
    }

    void chordsAreKeptInOrderOnCharactersTheTextHas()
    {
        const QString text = "Born to raise\nBorn to give";
        const QList<Chord> tidyChords = tidy(text, {{50, "E"}, {8, " "}, {0, "A"}, {0, "F#m"}, {13, "B"}});
        QCOMPARE(tidyChords, (QList<Chord> {{0, "F#m"}, {12, "B"}, {25, "E"}}));
        QVERIFY(tidy("", {{0, "A"}}).isEmpty());
    }

    void chordsStayWithTheirWordsWhenTheWordsAreChanged()
    {
        const QList<Chord> chords {{0, "G"}, {8, "D"}, {14, "Em"}};
        // A word corrected in the middle: what is before it stays, what is after moves
        QCOMPARE(carried("One two three four", "One two THREEE four", chords), (QList<Chord> {{0, "G"}, {8, "D"}, {15, "Em"}}));
        // A line added at the end, and one at the start
        QCOMPARE(carried("One two three four", "One two three four\nFive", chords), chords);
        QCOMPARE(carried("One two three four", "Zero\nOne two three four", chords), (QList<Chord> {{5, "G"}, {13, "D"}, {19, "Em"}}));
        // The words a chord stood on taken away: it goes to where they were, and two
        // that land together are one
        QCOMPARE(carried("One two three four", "One four", chords), (QList<Chord> {{0, "G"}, {4, "D"}}));
        // Nothing left to stand on
        QVERIFY(carried("One", "", chords).isEmpty());
        QCOMPARE(carried("One", "One", {{1, "A"}}), (QList<Chord> {{1, "A"}}));
    }

    void aChordIsWrittenOverTheWordsUpToTheNextOrTheEndOfItsLine()
    {
        // As Multitracks wrote "Hark the Herald": 0-18, 18-31, then the second line
        const QString text = "Born to raise the sons of earth\nBorn to give them second birth";
        const QList<Range> written = ranges(text, {{0, "A"}, {18, "F#m"}, {32, "B/D#"}, {50, "E"}});
        QCOMPARE(written.size(), 4);
        QVERIFY(written.at(0).start == 0 && written.at(0).end == 18 && written.at(0).name == "A");
        QVERIFY(written.at(1).start == 18 && written.at(1).end == 31);
        QVERIFY(written.at(2).start == 32 && written.at(2).end == 50);
        QVERIFY(written.at(3).start == 50 && written.at(3).end == 62);
        // Never empty, even on the last character
        const QList<Range> last = ranges("ab", {{1, "C"}});
        QVERIFY(last.at(0).start == 1 && last.at(0).end == 2);
    }

    void aChordWithNoWordsIsWrittenOverItsOwnStandInAndNoMore()
    {
        const auto spans = [](const QList<Range> &written) {
            QStringList all;
            for (const Range &range : written)
                all << QStringLiteral("%1-%2").arg(range.start).arg(range.end);
            return all.join(u' ');
        };
        // As Multitracks writes an intro: the wide space between two chords is in
        // neither chord's stretch, where on a line of words the first would run up to
        // the second.
        const QList<Chord> three {{0, "E"}, {2, "A"}, {4, "B"}};
        QCOMPARE(spans(ranges(placeholders(3), three)), "0-1 2-3 4-5");
        QCOMPARE(spans(ranges(placeholders(3, u' '), three)), "0-1 2-3 4-5");
        // One chord on a slide of its own, hung on one space or one zero-width space
        QCOMPARE(spans(ranges(" ", {{0, "E"}})), "0-1");
        QCOMPARE(spans(ranges(placeholders(1), {{0, "E"}})), "0-1");
        // Fewer chords than the line has stand-ins: each is still its own one only
        QCOMPARE(spans(ranges(placeholders(3), {{0, "E"}, {4, "B"}})), "0-1 4-5");
        // A text box with such a line among lines of words (a turnaround after a
        // verse): each line is written its own way. "One two" is 0 to 6, its line
        // break 7, the stand-ins 8 to 10, their line break 11, "Three" from 12.
        const QString mixed = QStringLiteral("One two\n") + placeholders(2) + QStringLiteral("\nThree");
        QCOMPARE(spans(ranges(mixed, {{0, "G"}, {4, "D"}, {8, "C"}, {10, "G"}, {12, "Em"}})), "0-4 4-7 8-9 10-11 12-17");
        // A chord on an empty line is one long, as it always was. And a line that is
        // only spaces is a line of chords alone: there is no telling it from one
        // (isPlaceholders).
        QCOMPARE(spans(ranges("One\n\nTwo", {{4, "G"}})), "4-5");
        QCOMPARE(spans(ranges("   ", {{0, "G"}, {2, "D"}})), "0-1 2-3");
    }

    void chordProThereAndBack()
    {
        const Line line {"Amazing grace how sweet", {{0, "C"}, {8, "F"}, {18, "C/E"}}};
        QCOMPARE(toChordPro(line), "[C]Amazing [F]grace how [C/E]sweet");
        QCOMPARE(fromChordPro("[C]Amazing [F]grace how [C/E]sweet"), line);
        // Typed small, an annotation among them, a chord after the last word
        const Line loose = fromChordPro("[c]One [*Coda]two[g]");
        QCOMPARE(loose.text, "One two");
        QCOMPARE(loose.chords, (QList<Chord> {{0, "C"}, {6, "G"}}));
        // Several lines of one text box
        const QString text = "One two\nThree";
        const QList<Chord> chords {{4, "G"}, {8, "Am"}};
        QCOMPARE(toChordPro(text, chords), "One [G]two\n[Am]Three");
        QCOMPARE(chordsOf("One [G]two\n[Am]Three"), chords);
        QCOMPARE(withoutChords("One [G]two\n[Am]Three"), text);
    }

    void chordsWithNoWordsHangOnStandIns()
    {
        const Line intro = fromChordPro("[C#m] [B/D#]  [E]");
        QCOMPARE(intro.text, placeholders(3));
        QCOMPARE(intro.text.size(), 5);
        QCOMPARE(intro.chords, (QList<Chord> {{0, "C#m"}, {2, "B/D#"}, {4, "E"}}));
        QVERIFY(isPlaceholders(intro.text) && !isPlaceholders("a") && !isPlaceholders(""));
        // Some of Multitracks' songs hang them on plain spaces, and one chord on one space
        const QString spaced = placeholders(3, u' ');
        QCOMPARE(spaced, QString(u' ') + QChar(0x2001) + u' ' + QChar(0x2001) + u' ');
        QVERIFY(isPlaceholders(spaced) && isPlaceholders(" "));
        QCOMPARE(toChordPro(Line {spaced, {{0, "D"}, {2, "F#m"}, {4, "E"}}}), "[D] [F#m] [E]");
        QCOMPARE(toChordPro(intro), "[C#m] [B/D#] [E]");
    }

    void aSongIsReadFromAChordProFile()
    {
        const Song song = parseSong(
            "{title: Plain Song}\r\n{artist: Nobody}\n{key: g}\n# a remark\n\n"
            "{start_of_verse}\n[G]One two [D]three\nFour five\n\nSix [Em]seven\n{end_of_verse}\n"
            "{soc}\n  [C]Sing it [G]out  \n{eoc}\n"
            "{start_of_tab}\ne|---0---|\n{end_of_tab}\n"
            "{comment: Bridge}\n[Am]Over [D]now\n\n"
            "Verse 2:\nEight nine\n\n"
            "{start_of_chorus: Last Chorus}\n[C]Sing\n{end_of_chorus}\n"
            "[Outro]\n[G] [D]\n");
        QCOMPARE(song.title, "Plain Song");
        QCOMPARE(song.artist, "Nobody");
        QCOMPARE(song.key, "G");
        QStringList names;
        for (const Section &section : song.sections)
            names << section.name;
        QCOMPARE(names, QStringList({"Verse 1", "Chorus", "Bridge", "Verse 2", "Last Chorus", "Outro"}));
        // A gap inside a marked verse does not end it
        QCOMPARE(song.sections.at(0).lines.size(), 3);
        QCOMPARE(song.sections.at(0).lines.at(0), (Line {"One two three", {{0, "G"}, {8, "D"}}}));
        // Spaces at the ends go, and the chords with them
        QCOMPARE(song.sections.at(1).lines.at(0), (Line {"Sing it out", {{0, "C"}, {8, "G"}}}));
        QCOMPARE(song.sections.at(5).lines.at(0).text, placeholders(2));
    }

    void aFileWithNoDirectivesComesInAsItsVerses()
    {
        const Song song = parseSong("One\nTwo\nThree\n\nFour\nFive\n");
        QCOMPARE(song.sections.size(), 2);
        QVERIFY(song.sections.at(0).name.isEmpty() && song.sections.at(0).lines.size() == 3);
        const QList<QList<Line>> slides = slidesOf(song.sections.at(0), 2);
        QCOMPARE(slides.size(), 2);
        QVERIFY(slides.at(0).size() == 2 && slides.at(1).size() == 1);
        QCOMPARE(slidesOf(song.sections.at(0), 0).size(), 3);
    }
};

QTEST_APPLESS_MAIN(TestChords)
#include "tst_chords.moc"
