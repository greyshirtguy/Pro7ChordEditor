#pragma once

#include "chords.h"
#include "richtext.h"

#include "presentation.pb.h"

#include <QColor>
#include <QSizeF>
#include <QVariantMap>

// Conversion between ProPresenter's slide messages and what the rest of the app works
// with. Shared by the reader used for showing presentations and by the editor.
//
// Reading goes one way: a `Slide` message becomes a map (toSlideMap), which is all that
// QML ever sees of a slide. The map is flat and complete on purpose. Whatever has to be
// worked out is worked out here, once: which elements show (an element can be hidden,
// or shown only when another has text), and what text each shows (an element can show
// another's text in its own style). The views then only draw what they are given, and
// the output, the thumbnails and the editor cannot disagree about it.
//
// Writing goes the other way, and is never a whole slide: applyChanges() and writeText()
// alter fields of the message that was read, in place, and nothing else in it. That is
// the rule for every file this app writes: what it does not understand, it does not
// touch, so ProPresenter finds its own work as it left it.
namespace proconvert {

// Reads a presentation file. On failure returns false and sets *error.
bool readPresentation(const QString &path, rv::data::Presentation *presentation, QString *error);
// Writes one. Everything in the message, including fields this app does not know about,
// is written back as it was read. The file is replaced in one step, so a failure part
// way through leaves the original untouched. Returns an error message, empty on success.
QString writePresentation(const QString &path, const rv::data::Presentation &presentation);
QColor toColor(const rv::data::Color &color);
void setColor(rv::data::Color *target, const QColor &color);

// What linking to other text can do to it on the way, `transform` being one of the
// file format's choices: nothing, onto one line, a word to a line, a letter to a line.
QString linkTransformed(const QString &text, int transform);

// The text of a text element: its RTF, together with the capitalisation that the file
// keeps beside the RTF because RTF cannot express it.
RichText readText(const rv::data::Graphics::Text &text);

// Replaces the text of a text element. The RTF is rewritten, and so are the attributes
// the file keeps beside it, which describe the start of the text, and the ranges that
// carry capitalisation and the families of fonts. Ranges of kinds this app does not
// understand are kept if only the format is changing, and dropped if the characters
// are, since they are tied to character positions that the new text no longer has.
// Chords are the exception: they are carried over to the new words (chords::carried),
// so that correcting a word does not cost a song its chords.
void writeText(rv::data::Graphics::Text *text, const RichText &rich);

// The chords over a text element's words, in order, and replacing them.
//
// How ProPresenter keeps them (worked out from songs it imported from Multitracks, and
// a few it changed afterwards). A chord is one of the "custom attributes" the file
// keeps beside the RTF, the same list that has the capitalisation and the fonts: a
// range of characters and the chord's name, in the song's original key. The range
// starts at the character the chord stands over. Where it ends varies a little from
// file to file (at the next chord, or the end of the line, and now and then past it),
// so only its start is read; what is written is chords::ranges(), the commonest form.
// Places are counted along the text as plainText() gives it, a line break being one.
//
// Writing touches nothing but the chord ranges: the RTF and every other range are left
// exactly as they were.
QList<chords::Chord> readChords(const rv::data::Graphics::Text &text);
void writeChords(rv::data::Graphics::Text *text, const QList<chords::Chord> &chords);

// A slide as the QML side consumes it: size, background, label, plain text, and a list
// of elements. Each element is a map of
//   id, name, x, y, width, height, rotation, opacity, locked, hidden
//   fillOn, fillKind ("color", "gradient", "media", "other" or "none"), fillColor, and
//     fillEnabled (on, and a plain colour, which is the kind that is drawn)
//   fillLinesOnly (the fill is only behind the lines of the text) and how: lineMaskStyle
//     (0 the box's width, 1 each line's, 2 the widest line's), lineMaskWidthOffset,
//     lineMaskHeightOffset, lineMaskHorizontalOffset, lineMaskVerticalOffset
//   fillShown (on, and of a kind that is drawn: a colour, a gradient, or a picture
//     that can be found), and for the other two kinds: fillGradientFrom,
//     fillGradientTo and fillGradientAngle (the first and last of its colours, and the
//     way it runs, in degrees anticlockwise from pointing right); fillMediaName,
//     fillMediaPath and fillMediaSource (the file, the last two empty if it cannot be
//     found or, the source, if it is a video, which is not drawn), fillMediaVideo and
//     fillMediaScale (0 to fit, 1 to fill, 2 stretched)
//   shape ("rectangle", "roundedRectangle", "ellipse", "arrow" or "other"), roundness
//     (of a rounded rectangle: its corners' radius as a part of its shorter side), and
//     outline (for anything but a rectangle: its points on the unit square, each
//     [x, y, in x, in y, out x, out y], the last four bending the outline on its way
//     into the point and out of it)
//   featherOn and featherRadius (its edges fading out, over that part of its shorter
//     side)
//   strokeOn, strokeColor, strokeWidth, and strokeEnabled (on, and wider than nothing)
//   shadow... and textShadow...: Enabled, Color (as drawn, opacity included), Angle,
//     Offset, Radius, and worked out from those OffsetX and OffsetY
//   words (the start of its own text, on one line: what an element with no name is
//     called in a list)
//   text (RichText, as authored), verticalAlignment, marginLeft/Top/Right/Bottom,
//   textScale (whether the text's size is changed to suit its box, as the file has it:
//     0 no, 1 the box's height suits the text instead, which is not done here, 2 made
//     smaller if it does not fit, 3 made larger if there is room, 4 either; see
//     StrokedText),
//     textTransform
//   chords: the chords over its own words, each { at, name } (see readChords), in
//     order; the key is left out of an element that has none, which is nearly all of
//     them. And how chords are drawn by an element that shows the words of the live
//     slide, which is where ProPresenter shows them: chordsOn, chordNotation (as
//     chords::Notation) and chordColor; and, only when chordsOn, chordStyle: the
//     style of its words as plain values { family, size, bold, italic, color,
//     capitals, alignment (0 left, 1 centred, 2 right) } for ChordedText.qml
//   linkKind ("none", "element", "timer", "slideText" or "other"): where the element's
//     text comes from, if it is not its own. For another element of the slide,
//     linkElementId, linkElementName and linkTransform. For a timer, linkTimerId and
//     linkTimerName (it is found by id, or failing that by name); linkTimerHours,
//     linkTimerMinutes, linkTimerSeconds and linkTimerHundredths (how each part of the
//     time is written, as Timers::Style), linkTimerHundredthsUnderMinute (the
//     hundredths only show in the last minute) and linkTimerPattern (text with
//     "${timer}" where the time goes). For the text of the slide that is live,
//     linkSlideNext (it is the one after it instead), linkSlideSource (which of its
//     text, as Show::Source), linkSlideName (the name of the elements whose text it
//     is, if it goes by name) and linkTransform. For anything else, which this app
//     does not show, linkLabel names it and linkPicture says whether it is a picture or
//     a colour that it puts in the element, in place of its fill, and not words
//   visibilityRules (bool), visibilityCriterion (0 all, 1 any, 2 none),
//     visibilityTimed (one of the conditions is about a timer, so whether the element
//     shows is not settled until it is drawn: see Slide.qml),
//     visibilityConditions: a list of { kind: "element", elementId, elementName, hasText }
//     and, for conditions on things this app does not track, { kind: "other", index,
//     label }
// and, worked out for the slide as it stands,
//   displayText (RichText: the text actually shown, after linking and transforming),
//   hasText, and visible (false if hidden or if its visibility rules say so).
// All geometry is in slide units.
//
// An element linked to a timer, or to the text of the slide that is live, shows
// something that is not in the slide at all, and that changes while the slide is on
// show. So its displayText only stands in for it (the timer at nothing, or no words,
// in the element's own style); what draws the element asks for the text as it is at
// the moment (SlideElement.qml). A stage layout is a slide made of text boxes like
// that. Another such thing, the clock say, is another linkKind handled in those same
// two places.
//
// An element linked to something this app does not show (linkKind "other") has no
// displayText at all. The text such an element has of its own is only a sample of the
// real thing ("1:23 PM" for the clock), which would pass for it if it were shown; the
// editor, where a sample is what is wanted, draws it from `text` (EditorCanvas.qml).
QVariantMap toSlideMap(const rv::data::Slide &slide, const QString &label);

// Changes an element of a slide. `changes` holds new values under the element map's
// keys; these can be changed:
//   x, y, width, height, opacity, name, locked, hidden
//   fillOn, fillColor (which also makes the fill a plain colour), fillLinesOnly
//   fillKind ("color" or "gradient"), fillGradientFrom, fillGradientTo,
//     fillGradientAngle, fillMediaPath (which makes the fill that file), fillMediaScale
//   roundness, featherOn, featherRadius
//   strokeOn, strokeColor, strokeWidth
//   shadowEnabled, shadowColor, shadowAngle, shadowOffset, shadowRadius, and the same
//     for textShadow...
//   verticalAlignment, textScale, marginLeft, marginTop, marginRight, marginBottom
//   chordsOn, chordNotation, chordColor
//   linkKind ("none", "element", "timer" or "slideText"), linkElementId,
//     linkTransform, linkTimerId, linkTimerName, linkTimerHours, linkTimerMinutes,
//     linkTimerSeconds, linkTimerHundredths, linkSlideNext, linkSlideSource,
//     linkSlideName
//   visibilityRules, visibilityCriterion, visibilityConditions (conditions of kind
//     "other" are kept as they were, by their index)
// Renaming an element also renames it where other elements of the slide refer to it.
// Returns false if the slide has no such element.
bool applyChanges(rv::data::Slide *slide, const QString &elementId, const QVariantMap &changes);

// A new text box for a slide, in the middle of it, with a name no other element of the
// slide has. Its text is set in the format of `like`'s, if given, so that it matches
// the text already on the slide.
rv::data::Slide::Element makeTextElement(const rv::data::Slide &slide, const rv::data::Slide::Element *like);

// A new shape for a slide, filled with a plain colour: "rectangle", "roundedRectangle",
// "ellipse" or "arrow". And a new element filled with a picture or a video file, the
// shape of the picture. Each is an element like any other, with words of its own to be
// typed into it: in ProPresenter's files a text box, a shape and a media element are
// one kind of thing, which differ only in what they start out with.
rv::data::Slide::Element makeShapeElement(const rv::data::Slide &slide, const QString &shape);
rv::data::Slide::Element makeMediaElement(const rv::data::Slide &slide, const QString &file);

// A new cue at the end of a presentation, laid out as ProPresenter writes one: a slide
// of this size with nothing on it, labelled with `name`.
rv::data::Cue *addBlankCue(rv::data::Presentation *presentation, const std::string &name, const QSizeF &size);

// A name for a new element: `base`, or `base` and the first number that makes it one
// no element of the slide has.
QString uniqueElementName(const rv::data::Slide &slide, const QString &base);

} // namespace proconvert
