#pragma once

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
    Q_INVOKABLE QStringList diatonic(const QString &key) const;
    Q_INVOKABLE QStringList completions(const QString &typed, const QString &key, const QStringList &used) const;
    Q_INVOKABLE QString shown(const QString &chord, const QString &originalKey, const QString &key, int notation) const;
    // A text box's words and chords ([{ at, name }]) as ChordPro text, and the chords
    // of ChordPro text, and its words alone.
    Q_INVOKABLE QString toChordPro(const QString &text, const QVariantList &chords) const;
    Q_INVOKABLE QVariantList chordsOf(const QString &chordPro) const;
    Q_INVOKABLE QString withoutChords(const QString &chordPro) const;
    Q_INVOKABLE bool isPlaceholders(const QString &line) const;
    // What a ChordPro file holds, for the import to show before it is done: { title,
    // artist, key, sections: [{ name, lines }] (how many lines each), chords (how
    // many), error }.
    Q_INVOKABLE QVariantMap describeFile(const QString &path) const;
};

// Colours ChordPro text as it is typed (ChordProEditor.qml): chords in the app's orange,
// brackets and all, and the lines in curly brackets that name the song's parts greyed,
// since they are not to be changed. It is given a TextEdit's document and does nothing
// but colour it; the text is not touched.
class ChordHighlighter : public QObject
{
    Q_OBJECT
    QML_ELEMENT

    Q_PROPERTY(QQuickTextDocument *document READ document WRITE setDocument NOTIFY documentChanged)

public:
    using QObject::QObject;

    QQuickTextDocument *document() const { return m_document; }
    void setDocument(QQuickTextDocument *document);

signals:
    void documentChanged();

private:
    QQuickTextDocument *m_document = nullptr;
    QSyntaxHighlighter *m_highlighter = nullptr;
};
