// The line of hints under the chord sheet (src/sheethints.h): what is said for each
// thing the person can be about, in the words the Mac and Windows chord editors have,
// and how the hints are fitted into the width there is. Widths here are made up (so
// many pixels a character), as a test of the arithmetic and not of a font.

#include "sheethints.h"

#include <QTest>

using namespace sheethints;

namespace {

QStringList read(const QList<Hint> &hints)
{
    QStringList all;
    for (const Hint &hint : hints)
        all << hint.read();
    return all;
}

// Seven pixels a character at 13, and eight on a key at 12, and so in proportion
double measure(const QString &words, int pixels, bool onAKey)
{
    return words.size() * (onAKey ? 8.0 * pixels / 12 : 7.0 * pixels / 13);
}

}

class TestSheetHints : public QObject
{
    Q_OBJECT

private slots:
    void whileAChordIsTyped()
    {
        QCOMPARE(read(hints({Doing::Typing, false, false, false}, false, {}, false)),
                 QStringList({"Enter Keeps it", "Tab Keeps it and goes on to the next word", "Up Down Choose from what is offered",
                              "Esc Throws it away", "A click on another word keeps it too"}));
        // The box emptied where there is a chord: Enter takes it off (rule 35a)
        QCOMPARE(read(hints({Doing::Typing, true, true, false}, false, {}, false)),
                 QStringList({"Enter Takes this chord off", "Esc Leaves it as it was", "Or type the chord that is to be here"}));
        // Empty where there is none, or typed in where there is one: as ever
        QCOMPARE(hints({Doing::Typing, false, true, false}, false, {}, false).size(), 5);
        QCOMPARE(hints({Doing::Typing, true, false, false}, false, {}, false).size(), 5);
        // Typing comes first, whatever is held and wherever the pointer is
        QCOMPARE(hints({Doing::Typing, false, false, false}, true, {2, true}, true), hints({Doing::Typing, false, false, false}, false, {}, false));
    }

    void whileAChordIsCarried()
    {
        QCOMPARE(read(hints({Doing::Carrying, false, false, true}, false, {}, false)), QStringList({"Let go on a word to put this chord there"}));
        // (Ctrl copies only a chord of the sheet's own.)
        QCOMPARE(read(hints({Doing::Carrying, false, false, true}, true, {}, false)), QStringList({"Let go on a word to put this chord there"}));
        QCOMPARE(read(hints({Doing::Carrying, false, false, false}, false, {}, false)),
                 QStringList({"Let go on a word to move it there", "Ctrl Held as you let go, leaves a copy behind"}));
        QCOMPARE(read(hints({Doing::Carrying, false, false, false}, true, {}, false)), QStringList({"Let go on a word to put a copy there"}));
    }

    void atTheSheet()
    {
        QCOMPARE(read(hints({Doing::OnChord}, false, {}, false)),
                 QStringList({"Delete Takes this chord off", "1 – 7 Change it", "You can drag this chord to move it anywhere. (Hold Ctrl to drag a copy)",
                              "Click to type over it"}));
        QCOMPARE(read(hints({Doing::OnPlace}, false, {}, false)),
                 QStringList({"1 – 7 Put one of the key’s chords here", "A – G Or a click: type one"}));
    }

    void withCtrlHeldTheCopyingIsSaid()
    {
        const QList<Hint> one = hints({Doing::OnPlace}, true, {1, false}, false);
        QCOMPARE(read(one), QStringList({"Press Ctrl+C to copy all chords on the highlighted line.",
                                         "Press Ctrl+V to paste previously copied chords to the highlighted line"}));
        QVERIFY(one.at(0).needed && one.at(1).needed);
        QCOMPARE(one.at(0).keys, QStringList({"Ctrl+C"}));
        // Where it would take chords off, and where more than one line was copied
        QCOMPARE(hints({Doing::OnChord}, true, {1, true}, false).at(1).says,
                 "to paste previously copied chords to the highlighted line (Replaces existing chords)");
        QCOMPARE(hints({Doing::OnChord}, true, {4, false}, false).at(1).says,
                 "to paste previously copied chords to the highlighted line and the lines after it");
        QCOMPARE(hints({Doing::OnPlace}, true, {4, true}, false).at(1).says,
                 "to paste previously copied chords to the highlighted line and the lines after it (Replaces existing chords)");
        // With nothing copied yet, as for one line
        QCOMPARE(hints({Doing::OnPlace}, true, {0, true}, false).at(1).says,
                 "to paste previously copied chords to the highlighted line (Replaces existing chords)");
    }

    void nobodyAtTheSheetAndTheStrip()
    {
        const QStringList idle {"Point at a word or a chord, and this line says what the keys do there."};
        QCOMPARE(read(hints({Doing::Nothing}, false, {}, false)), idle);
        // Whatever is held: there is no highlighted line for the copying to be about
        QCOMPARE(read(hints({Doing::Nothing}, true, {2, true}, false)), idle);
        // Over the key's chords, what is said of the spot gives way
        const QStringList strip {"You can drag these down to the text on any slide"};
        QCOMPARE(read(hints({Doing::Nothing}, false, {}, true)), strip);
        QCOMPARE(read(hints({Doing::OnChord}, true, {1, true}, true)), strip);
    }

    void everyHintBeginsWithACapital()
    {
        QList<Hint> all;
        for (Doing doing : {Doing::Nothing, Doing::Typing, Doing::Carrying, Doing::OnChord, Doing::OnPlace}) {
            for (bool flag : {false, true}) {
                for (bool ctrl : {false, true}) {
                    for (bool strip : {false, true})
                        all << hints({doing, flag, flag, flag}, ctrl, {2, flag}, strip);
                }
            }
        }
        for (const Hint &hint : all) {
            QVERIFY2(!hint.says.isEmpty(), qPrintable(hint.read()));
            // (After keys, what they do; after words, the rest of the sentence.)
            if (hint.lead.isEmpty())
                QVERIFY2(hint.says.at(0).isUpper(), qPrintable(hint.read()));
            else
                QVERIFY2(hint.lead.at(0).isUpper(), qPrintable(hint.read()));
        }
    }

    void asManyAsThereIsRoomFor()
    {
        const QList<Hint> said = hints({Doing::OnChord}, false, {}, false);
        const double all = widthOf(said, said.size(), 1, measure);
        QCOMPARE(fit(said, all, measure), (Fit {4, false, 1}));
        QCOMPARE(fit(said, all - 1, measure), (Fit {3, false, 1}));
        QCOMPARE(fit(said, widthOf(said, 1, 1, measure), measure), (Fit {1, false, 1}));
        // One that does not fit at all is said and cut off at the right
        QCOMPARE(fit(said, 10, measure), (Fit {1, false, 1}));
        QCOMPARE(heightOf(fit(said, 10, measure)), Height);
        QCOMPARE(fit({}, 500, measure), (Fit {0, false, 1}));
        // The parts of a hint: its key, and the room between
        QCOMPARE(widthOf(said.at(0), 1, measure), widthOfKey("Delete", 1, measure) + Within + std::ceil(measure(said.at(0).says, 13, false)));
        // (A key of one letter is as wide as the least a key is, and a longer one its sign and the room either side.)
        QCOMPARE(widthOfKey("C", 1, measure), KeyLeast);
        QCOMPARE(widthOfKey("Up", 1, measure), 16 + 2 * KeyPad);
    }

    void theCopyingIsNeverCutShort()
    {
        const QList<Hint> said = hints({Doing::OnPlace}, true, {4, true}, false);
        const double first = widthOf(said.at(0), 1, measure);
        const double second = widthOf(said.at(1), 1, measure);
        // Side by side where they fit
        QCOMPARE(fit(said, first + Between + second, measure), (Fit {2, false, 1}));
        // One under the other where they do not, the line taller for it
        const Fit stacked = fit(said, second, measure);
        QCOMPARE(stacked, (Fit {2, true, 1}));
        QCOMPARE(heightOf(stacked), std::ceil(2 * KeyHeight + Lines + 2 * OverAndUnder));
        // Smaller, a step at a time, where one of them is still too long
        QCOMPARE(fit(said, second - 1, measure), (Fit {2, true, 0.9}));
        QCOMPARE(fit(said, widthOf(said.at(1), 0.8, measure), measure), (Fit {2, true, 0.8}));
        // And at the smallest, cut off at the right
        QCOMPARE(fit(said, 50, measure), (Fit {2, true, 0.8}));
        QVERIFY(heightOf(fit(said, 50, measure)) >= Height);
    }
};

QTEST_APPLESS_MAIN(TestSheetHints)
#include "tst_sheethints.moc"
