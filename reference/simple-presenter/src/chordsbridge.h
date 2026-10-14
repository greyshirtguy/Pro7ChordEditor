#pragma once

#include <QColor>
#include <QHash>
#include <QObject>
#include <QQuickTextDocument>
#include <QStringList>
#include <QSyntaxHighlighter>
#include <QVariantList>
#include <QVariantMap>
#include <QtQml/qqmlregistration.h>

// The chord rules (chords.h) as QML can call them: `Chords`. There is nothing here but
// the handing over; what each does, and why, is said there.
class Chords : public QObject
{
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON

    // The keys a picker offers: the major ones, then the minor.
    Q_PROPERTY(QStringList majorKeys READ majorKeys CONSTANT)
    Q_PROPERTY(QStringList minorKeys READ minorKeys CONSTANT)

public:
    using QObject::QObject;

    QStringList majorKeys() const;
    QStringList minorKeys() const;

    Q_INVOKABLE bool couldBecome(const QString &typed) const;
    // Whether it is a chord as it stands: it starts with a note.
    Q_INVOKABLE bool isChord(const QString &typed) const;
    Q_INVOKABLE QString tidied(const QString &typed) const;
    // A chord's name as the chord sheet draws it, with ♯ and ♭; and the other way
    Q_INVOKABLE QString withSigns(const QString &name) const;
    Q_INVOKABLE QString withoutSigns(const QString &text) const;
    Q_INVOKABLE QStringList diatonic(const QString &key) const;
    Q_INVOKABLE QStringList completions(const QString &typed, const QString &key, const QStringList &used) const;
    Q_INVOKABLE QString shown(const QString &chord, const QString &originalKey, const QString &key, int notation) const;
    // A text box's words and chords ([{ at, name }]) as ChordPro text, and the chords
    // of ChordPro text, and its words alone.
    Q_INVOKABLE QString toChordPro(const QString &text, const QVariantList &chords) const;
    Q_INVOKABLE QVariantList chordsOf(const QString &chordPro) const;
    Q_INVOKABLE QString withoutChords(const QString &chordPro) const;
    Q_INVOKABLE bool isPlaceholders(const QString &line) const;
    // The text the system has as what was last copied, by this app or any other; ""
    // if it is not text. The chord sheet keeps the chords it copies to itself, so as
    // not to put them in the way of what was copied elsewhere, and asks this to tell
    // whether anything has been copied since (ChordSheet.qml, copiedLast).
    Q_INVOKABLE QString copiedText() const;
    // What a ChordPro file holds, for the import to show before it is done: { title,
    // artist, key, sections: [{ name, lines }] (how many lines each), chords (how
    // many), error }.
    Q_INVOKABLE QVariantMap describeFile(const QString &path) const;
};

// The line of hints under the chord sheet (sheethints.h) as QML can ask for it:
// `SheetHints`. hints() answers what to say, as plain maps ({ lead, keys, says,
// needed }), for what is going on; fit() how many of them go into a width, one under
// another or not and at what size, and how tall the line then is, the words measured
// in the font the line draws them in (the app's own, as every Text of QML has it,
// at the sizes sheethints.h gives) and each measurement kept; `sizes` the sizes and
// rooms the line is drawn with, so that it is drawn as it was fitted.
class SheetHints : public QObject
{
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON

    Q_PROPERTY(QVariantMap sizes READ sizes CONSTANT)

public:
    using QObject::QObject;

    QVariantMap sizes() const;
    // `doing`: "nothing", "typing", "carrying", "onChord" or "onPlace";
    // `pasteLines` 0 when nothing has been copied
    Q_INVOKABLE QVariantList hints(const QString &doing, bool onChord, bool empty, bool fromStrip, bool ctrl, int pasteLines, bool replaces,
                                   bool overStrip) const;
    // { count, stacked, size, height } for the line's whole width
    Q_INVOKABLE QVariantMap fit(const QVariantList &hints, double width) const;

private:
    double measured(const QString &words, int pixels, bool onAKey) const;

    mutable QHash<QString, double> m_widths;
};

// Colours ChordPro text as it is typed (ChordProEditor.qml): chords in the app's orange,
// brackets and all, or in the colour chosen for them in the settings (`color`), and the
// lines in curly brackets that name the song's parts greyed, since they are not to be
// changed. It is given a TextEdit's document and does nothing but colour it; the text
// is not touched.
class ChordHighlighter : public QObject
{
    Q_OBJECT
    QML_ELEMENT

    Q_PROPERTY(QQuickTextDocument *document READ document WRITE setDocument NOTIFY documentChanged)
    Q_PROPERTY(QColor color READ color WRITE setColor NOTIFY colorChanged)

public:
    using QObject::QObject;

    QQuickTextDocument *document() const { return m_document; }
    void setDocument(QQuickTextDocument *document);
    QColor color() const { return m_color; }
    void setColor(const QColor &color);

signals:
    void documentChanged();
    void colorChanged();

private:
    QQuickTextDocument *m_document = nullptr;
    QSyntaxHighlighter *m_highlighter = nullptr;
    QColor m_color = QColor(0xff, 0xb4, 0x5e);
};
