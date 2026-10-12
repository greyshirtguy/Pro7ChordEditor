// Stand-ins written into an RTF where they stand (src/rtfstandins.h): one line of a
// text box's words given more stand-in characters or fewer, with every byte that is not
// one of that line's characters left as it was.
//
// It is held to answers that are not this app's own. The rule is the one the chord
// editors for the Mac and for Windows go by (Pro7 Chord Editor, tools/standins.py in
// its repository, which is the rule's reference), and all three are to write the same
// bytes; so the cases are that repository's, as it has them for its own two apps
// (tests/unit/data/stand-ins-in-rtf.json: RTF in every shape real songs were found to
// have, and some they were not, each with the bytes it becomes or that it cannot be
// done). And the reader the rule works by is held to what this app's own reader of RTF
// makes of the same samples (rtf-text.json, which was made from src/rtf.cpp). Then a few
// cases of this file's own, worked out by hand, that say in words what is being held.

#include "rtfstandins.h"

#include <QFile>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QTest>

using namespace rtfstandins;

namespace {

QJsonObject vectors(const char *name)
{
    QFile file(QStringLiteral(VECTORS) + u'/' + QLatin1String(name));
    if (!file.open(QIODevice::ReadOnly))
        return {};
    return QJsonDocument::fromJson(file.readAll()).object();
}

// What a line of that many chords alone is: a stand-in for each, an em quad between
QString standIns(int count, QChar standIn)
{
    QString run;
    for (int i = 0; i < count; ++i) {
        if (i > 0)
            run += QChar(0x2001);
        run += standIn;
    }
    return run;
}

// A numbered character as Apple's text system writes it, with the space that ends it.
// (Made of its pieces: a backslash, a "u" and four digits written together here would
// be one character to some of the tools this is written with.)
QByteArray numbered(int unit)
{
    return QByteArray("\\uc0\\") + "u" + QByteArray::number(unit) + " ";
}

const QByteArray opening = "{\\rtf1\\ansi{\\fonttbl\\f0\\fswiss Helvetica;\\f1\\fnil LucidaGrande;}\\f0\\fs160 \\cf2 ";

} // namespace

class TestRtfStandIns : public QObject
{
    Q_OBJECT

private slots:
    // ---- The reader, against this app's own reader of the same samples
    void theWordsAreReadAsTheAppReadsThem()
    {
        const QJsonArray cases = vectors("rtf-text.json").value("cases").toArray();
        QVERIFY(cases.size() >= 20);
        for (const QJsonValue &entry : cases) {
            const QJsonObject sample = entry.toObject();
            const QByteArray rtf = QByteArray::fromBase64(sample.value("rtfBase64").toString().toLatin1());
            const QString text = plainText(rtf);
            const QJsonArray units = sample.value("units").toArray();
            QVERIFY2(text.size() == units.size(), qPrintable(sample.value("name").toString()));
            for (qsizetype i = 0; i < text.size(); ++i)
                QVERIFY2(int(text.at(i).unicode()) == units.at(i).toInt(), qPrintable(sample.value("name").toString()));
            bool ok = false;
            written(rtf, &ok);
            QVERIFY2(ok, qPrintable(sample.value("name").toString()));
        }
    }

    // ---- The rule, against the answers the Mac and Windows editors are held to
    void aLineIsChangedAsTheOtherEditorsChangeIt()
    {
        const QJsonArray cases = vectors("stand-ins-in-rtf.json").value("inPlace").toArray();
        QVERIFY(cases.size() >= 380);
        int made = 0;
        int refused = 0;
        for (const QJsonValue &entry : cases) {
            const QJsonObject sample = entry.toObject();
            const QByteArray name = sample.value("name").toString().toUtf8();
            const QByteArray rtf = QByteArray::fromBase64(sample.value("rtfBase64").toString().toLatin1());
            const QString wanted = standIns(sample.value("count").toInt(), QChar(sample.value("standIn").toInt()));
            const std::optional<QByteArray> out = withLine(rtf, sample.value("line").toInt(), wanted);
            if (sample.value("outBase64").isNull()) {
                QVERIFY2(!out.has_value(), name.constData());
                ++refused;
            } else {
                QVERIFY2(out.has_value(), name.constData());
                QVERIFY2(*out == QByteArray::fromBase64(sample.value("outBase64").toString().toLatin1()), name.constData());
                ++made;
            }
        }
        // (Both kinds are among them, or the comparison above proves little.)
        QVERIFY(made > 200);
        QVERIFY(refused > 30);
    }

    // ---- By hand
    void aChordMoreIsPutAfterTheLinesLastCharacterAndNothingElseChanges()
    {
        // As Multitracks writes two chords with no words: each stand-in in the song's
        // font, the em quad between them in another.
        const QByteArray rtf = opening + numbered(0x200B) + "\n\\f1 \\cf2 " + numbered(0x2001) + "\n\\f0 \\cf2 " + numbered(0x200B) + "}";
        const std::optional<QByteArray> out = withLine(rtf, 0, standIns(3, QChar(0x200B)));
        QVERIFY(out.has_value());
        QCOMPARE(*out, rtf.left(rtf.size() - 1) + numbered(0x2001) + numbered(0x200B) + "}");
        QCOMPARE(plainText(*out), standIns(3, QChar(0x200B)));
    }

    void aChordFewerTakesItsCharactersOutAndLeavesTheWordsBetweenThem()
    {
        const QByteArray rtf = opening + numbered(0x200B) + "\n\\f1 \\cf2 " + numbered(0x2001) + "\n\\f0 \\cf2 " + numbered(0x200B) + "}";
        const std::optional<QByteArray> out = withLine(rtf, 0, standIns(1, QChar(0x200B)));
        QVERIFY(out.has_value());
        // The em quad and the second stand-in are gone: their own bytes, which are the
        // numbered character and the space that ends it. The control words that were
        // between them stay, the "uc0" that stood before each among them (it is a word
        // of its own, and no part of the character); they say nothing now.
        QCOMPARE(*out, opening + numbered(0x200B) + "\n\\f1 \\cf2 \\uc0" + "\n\\f0 \\cf2 \\uc0" + "}");
        QCOMPARE(plainText(*out), QString(QChar(0x200B)));
    }

    void theLinesAboveAndBelowAreNotTouched()
    {
        const QByteArray words = "{\\rtf1\\ansi\\f0\\fs84 \\cf2 Made up words\\\n";
        const QByteArray after = "\\\nAnd more}";
        const QByteArray rtf = words + " " + numbered(0x2001) + " " + after;
        QCOMPARE(plainText(rtf), QStringLiteral("Made up words\n") + standIns(2, u' ') + QStringLiteral("\nAnd more"));
        const std::optional<QByteArray> out = withLine(rtf, 1, standIns(3, u' '));
        QVERIFY(out.has_value());
        QVERIFY(out->startsWith(words));
        QVERIFY(out->endsWith(after));
        QCOMPARE(plainText(*out), QStringLiteral("Made up words\n") + standIns(3, u' ') + QStringLiteral("\nAnd more"));
    }

    void aLineWithNoChordIsOneZeroWidthSpaceWhateverItHungThemOn()
    {
        const QByteArray rtf = "{\\rtf1\\ansi\\f0\\fs84 \\cf2 Words\\\n " + numbered(0x2001) + " }";
        const std::optional<QByteArray> out = withLine(rtf, 1, QString(QChar(0x200B)));
        QVERIFY(out.has_value());
        QCOMPARE(plainText(*out), QStringLiteral("Words\n") + QChar(0x200B));
    }

    void aSpacePutInAfterAWordWithNoSpaceOfItsOwnIsGivenOneFirst()
    {
        // The em quad's own word runs up to the closing bracket: a plain space put
        // after it would be taken for the word's end.
        const QByteArray rtf = QByteArray("{\\rtf1\\ansi\\cf2  \\uc0\\") + "u8193}";
        QCOMPARE(plainText(rtf), QStringLiteral(" ") + QChar(0x2001));
        const std::optional<QByteArray> out = withLine(rtf, 0, standIns(2, u' '));
        QVERIFY(out.has_value());
        QCOMPARE(plainText(*out), standIns(2, u' '));
        QVERIFY(out->endsWith("u8193  }"));
    }

    void whatCannotBeDoneCleanlyIsNotDone()
    {
        const QByteArray rtf = opening + numbered(0x200B) + "}";
        QVERIFY(!withLine(rtf, 1, QString(QChar(0x200B))).has_value());     // a line it has not got
        QVERIFY(!withLine(rtf, -1, QString(QChar(0x200B))).has_value());
        QVERIFY(!withLine(rtf, 0, QStringLiteral("words")).has_value());    // not stand-ins
        // An empty line has no character to say where it is.
        QVERIFY(!withLine("{\\rtf1\\ansi Made up\\\n\\\nwords}", 1, QString(QChar(0x200B))).has_value());
        // Each numbered character with another after it to stand in for it, for
        // readers that do not know numbered ones: taking one out would leave its
        // stand-in to be read as a word.
        const QByteArray stoodIn = QByteArray("{\\rtf1\\ansi \\") + "u8203?\\" + "u8193?\\" + "u8203?}";
        QCOMPARE(plainText(stoodIn), standIns(2, QChar(0x200B)));
        QVERIFY(!withLine(stoodIn, 0, standIns(1, QChar(0x200B))).has_value());
        // An RTF that breaks off in the middle of a character
        bool ok = true;
        written("{\\rtf1 caf\\'e", &ok);
        QVERIFY(!ok);
    }

    // ---- What is said of a range of the words goes with the words
    void aRangeOverAllTheWordsIsOverAllOfThemStill()
    {
        // A text box that holds two chords alone (three units), in capitals from 0 to 3
        QCOMPARE(placeAfter(0, 0, 3, 5), 0);
        QCOMPARE(placeAfter(3, 0, 3, 5), 5);    // a chord more
        QCOMPARE(placeAfter(3, 0, 3, 1), 1);    // a chord fewer
        QCOMPARE(placeAfter(3, 0, 3, 3), 3);    // the same number, hung on another character
        // The last chord gone from a line on plain spaces: one zero-width space is left
        QCOMPARE(placeAfter(1, 0, 1, 1), 1);
    }

    void aRangeThatBeginsAfterTheLineIsOverTheSameWordsAsBefore()
    {
        // "Words" and a break (six units), then two chords alone from 6 to 9, a break,
        // and "more" from 10 to 14
        QCOMPARE(placeAfter(10, 6, 3, 5), 12);
        QCOMPARE(placeAfter(14, 6, 3, 5), 16);
        QCOMPARE(placeAfter(10, 6, 3, 1), 8);
        QCOMPARE(placeAfter(14, 6, 3, 1), 12);
        // (One that begins at the break after the line begins there still.)
        QCOMPARE(placeAfter(9, 6, 3, 5), 11);
        QCOMPARE(placeAfter(9, 6, 3, 1), 7);
    }

    void aPlaceBeforeTheLinesEndStaysOrComesBackToTheNewEnd()
    {
        // The words above the line, and the line's own first stand-in
        QCOMPARE(placeAfter(0, 6, 3, 5), 0);
        QCOMPARE(placeAfter(5, 6, 3, 1), 5);
        QCOMPARE(placeAfter(6, 6, 3, 1), 6);
        QCOMPARE(placeAfter(7, 6, 3, 5), 7);
        // On the line, where the line has shrunk: what it was on is gone
        QCOMPARE(placeAfter(7, 6, 3, 1), 7);
        QCOMPARE(placeAfter(8, 6, 3, 1), 7);
        QCOMPARE(placeAfter(8, 6, 5, 3), 8);
        QCOMPARE(placeAfter(10, 6, 5, 3), 9);
    }

    void whatIsAlreadyThoseStandInsIsLeftAsItIs()
    {
        const QByteArray rtf = opening + numbered(0x200B) + "\n\\f1 \\cf2 " + numbered(0x2001) + "\n\\f0 \\cf2 " + numbered(0x200B) + "}";
        const std::optional<QByteArray> out = withLine(rtf, 0, standIns(2, QChar(0x200B)));
        QVERIFY(out.has_value());
        QCOMPARE(*out, rtf);
    }
};

QTEST_APPLESS_MAIN(TestRtfStandIns)
#include "tst_rtfstandins.moc"
