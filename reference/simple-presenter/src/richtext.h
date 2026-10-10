#pragma once

#include <QColor>
#include <QList>
#include <QMetaType>
#include <QString>
#include <QVariantMap>

// Styled text as the renderer and the editor work with it: a list of paragraphs, each a
// list of runs, a run being a stretch of text in one format. It is what the RTF in a
// slide is parsed into (rtf.h) and written back from (rtfwriter.h), what is laid out
// and drawn (textlayout.h, strokedtext.h), and what the editor changes.
//
// All lengths are in slide units (the coordinate space of the slide's own size,
// typically 1920x1080), which are also the points ProPresenter measures text in; they
// are not output pixels.

struct TextRun
{
    // What ProPresenter calls the transformations it applies to how text is shown without
    // changing the text itself. The values are the ones the file format uses.
    enum Capitalization { NoCapitalization = 0, AllCaps = 1, SmallCaps = 2, TitleCase = 3, StartCase = 4 };

    QString text;
    // The font as the document names it, by PostScript name ("HelveticaNeue-Bold"), and
    // as it was resolved on this machine. The name is what is written back, so a font
    // that is not installed here survives being edited.
    QString fontName;
    QString family;
    qreal size = 12;
    bool bold = false;
    bool italic = false;
    bool underline = false;
    bool strikethrough = false;
    // Extra space between characters
    qreal kerning = 0;
    // 1 for superscript, -1 for subscript
    int superscript = 0;
    int capitalization = NoCapitalization;
    QColor fill = Qt::black;
    bool fillVisible = true;
    QColor stroke = Qt::black;
    qreal strokeWidth = 0;

    // Whether two runs differ only in their text.
    bool sameFormat(const TextRun &other) const;

    // The format as a map: family, fontName, size, bold, italic, underline,
    // strikethrough, kerning, capitalization, color, strokeColor, strokeWidth.
    QVariantMap format() const;
    // Changes whichever of those the map has. The stroke is off at width 0. Changing the
    // family, or bold or italic, picks a matching font name.
    void applyFormat(const QVariantMap &format);
};

// A paragraph with no text still carries one empty run, so its line height is known.
struct TextParagraph
{
    Qt::Alignment alignment = Qt::AlignLeft;
    // Line spacing as RTF records it (\sl and \slmult): with `lineHeightIsMultiple` the
    // lines are lineHeight / 240 times their natural height; otherwise lineHeight is a
    // height in twentieths of a point, a minimum if positive and exact if negative. Zero
    // is natural spacing.
    int lineHeight = 0;
    bool lineHeightIsMultiple = false;
    QList<TextRun> runs;
};

struct RichText
{
    QList<TextParagraph> paragraphs;

    // Paragraphs joined by "\n"; a line break within a paragraph also becomes "\n".
    QString plainText() const;
    bool isEmpty() const;
    // The format text typed at the very start would take: that of the first run.
    TextRun firstRun() const;

    // The same text, one paragraph per line of `text`, all in one format.
    static RichText plain(const QString &text, const TextRun &format, Qt::Alignment alignment);

    bool operator==(const RichText &other) const;

    // The two below take positions counted in characters through the whole text, a
    // paragraph break counting as one: the positions a text editor uses.

    // The text with a format applied from `start` up to `end`. `format` is a map as
    // TextRun::applyFormat takes, plus `alignment`, which applies to every paragraph
    // the range touches.
    RichText formatted(int start, int end, const QVariantMap &format) const;
    // The format at a selection, or at a caret if `start` and `end` are the same: a map
    // as TextRun::format gives, plus `alignment`.
    QVariantMap formatAt(int start, int end) const;
};

Q_DECLARE_METATYPE(RichText)
Q_DECLARE_METATYPE(TextRun)
