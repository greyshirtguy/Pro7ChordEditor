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

// A slide's notes: words for whoever is running the show or standing on the stage, kept
// with the slide and never shown to the audience. Only a slide of a presentation has
// them (a prop, a stage layout and a theme's slide have nowhere for them in their
// files).
//
// How ProPresenter keeps them (seen in fifteen slides of real libraries, version 7 on a
// Mac): beside the slide itself, as RTF with a set of attributes, which is the pair a
// text box keeps its text as. The RTF is all there is to them: of those fifteen, eight
// have attributes with nothing in them, and seven have the look the notes were first
// typed in (a font, a colour, a paragraph), with no ranges in it. A slide that has
// never had notes has no such message at all.
//
// readNotes gives them as the text they are, with no paragraphs if the slide has
// none. writeNotes writes the RTF, and leaves whatever the attributes hold as it was;
// for a slide that had no notes it makes the message, with attributes that hold
// nothing, as most of those seen have. Notes with no words in them are still notes to
// the file: whether the message is to be taken out again is for whoever knows it was
// not there before (PresentationEditor::setNotes).
//
// notesFormat is what notes are typed in where there are none yet: the font, the size
// and the white that ProPresenter's own start in (Helvetica Neue, 24 points), as the
// notes looked at have them. A GUESS that it is the same in every version and on
// Windows.
RichText readNotes(const rv::data::PresentationSlide &slide);
void writeNotes(rv::data::PresentationSlide *slide, const RichText &rich);
TextRun notesFormat();
// Puts a slide's notes into its map, as `notes` (the text they are) and `notesText`
// (their words): only for a slide that has any.
void addNotes(const rv::data::PresentationSlide &slide, QVariantMap *map);

// The chords over a text element's words, in order, and replacing them.
//
// How ProPresenter keeps them (worked out from songs it imported from Multitracks, and
// a few it changed afterwards). A chord is one of the "custom attributes" the file
// keeps beside the RTF, the same list that has the capitalisation and the fonts: a
// range of characters and the chord's name, in the song's original key. The range
// starts at the character the chord stands over. Where it ends is not the same in
// every file (a program other than ProPresenter has been at some of them, and left
// ranges that run past their line), so only its start is read; what is written is
// chords::ranges(), which is how Multitracks writes them: to the next chord or the end
// of the line, and on a line of chords alone the chord's own character only.
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
//   text (RichText, as authored), textBox (whether the file gives it a text at all,
//     with words or with none: what the chord editors go by to find a blank slide's
//     text box), verticalAlignment, marginLeft/Top/Right/Bottom,
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
//   linkKind ("none", "element", "timer", "slideText", "clock", "videoCountdown",
//     "stageMessage" or "other"): where the element's
//     text comes from, if it is not its own. For another element of the slide,
//     linkElementId, linkElementName and linkTransform. For a timer, linkTimerId and
//     linkTimerName (it is found by id, or failing that by name); linkTimerHours,
//     linkTimerMinutes, linkTimerSeconds and linkTimerHundredths (how each part of the
//     time is written, as Timers::Style), linkTimerHundredthsUnderMinute (the
//     hundredths only show in the last minute) and linkTimerPattern (text with
//     "${timer}" where the time goes). For the text of the slide that is live,
//     linkSlideNext (it is the one after it instead), linkSlideSource (which of its
//     text, as Show::Source), linkSlideName (the name of the elements whose text it
//     is, if it goes by name), linkSlidePreserve (for a slide's notes: they are shown
//     in their own fonts, sizes and colours, and not in this element's) and
//     linkTransform. For the system's clock, linkClockDate and linkClockTime (how the
//     date and the time are written, each as clocktext::Style), linkClock24 (on the
//     twenty-four hour clock) and linkClockPattern (text with "${clock}" where they
//     go). For how long the video that is playing has left, the timer's four parts
//     again (linkTimerHours and the rest, and linkTimerPattern) and
//     linkVideoIgnoreLooping (a video that goes round is not counted down). For what
//     the stage is being told (the stage message), linkFlash (the element's fill
//     flashes when a message is shown) and linkFlashColor (the colour it flashes).
//     Those of the notes, the clock, the video and the stage message are there only
//     for an element that is linked to such a thing: whoever reads them takes none to
//     be no. (What draws a message of the messages' layer makes one more kind for
//     itself, "message" with linkMessageId: see Messages::slideOf.) For
//     anything else, which this app does not show, linkLabel names it and linkPicture
//     says whether it is a picture or a colour that it puts in the element, in place of
//     its fill, and not words
//   visibilityRules (bool), visibilityCriterion (0 all, 1 any, 2 none),
//     visibilityTimed (one of the conditions is about a timer, or about an element
//     that shows the stage message, which is also marked by visibilityStaged and on
//     the condition by `staged`; so whether the element
//     shows is not settled until it is drawn: see Slide.qml),
//     visibilityConditions: a list of { kind: "element", elementId, elementName, hasText }
//     and, for conditions on things this app does not track, { kind: "other", index,
//     label }
// and, worked out for the slide as it stands,
//   displayText (RichText: the text actually shown, after linking and transforming),
//   hasText, and visible (false if hidden or if its visibility rules say so).
// All geometry is in slide units.
//
// An element linked to a timer, to the clock, to a video's time, to the stage message
// or to the text of the slide that is live, shows something that is not in the slide
// at all, and that changes while the slide is on show. So its displayText only stands
// in for it (the
// timer at nothing, the clock at noon, or no words, in the element's own style); what
// draws the element asks for the text as it is at the moment (SlideElement.qml). A
// stage layout is a slide made of text boxes like that. Another such thing is another
// linkKind handled in those same two places.
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
//   linkKind ("none", "element", "timer", "slideText", "clock", "videoCountdown" or
//     "stageMessage"), linkElementId, linkTransform, linkTimerId, linkTimerName,
//     linkTimerHours, linkTimerMinutes, linkTimerSeconds, linkTimerHundredths,
//     linkSlideNext, linkSlideSource, linkSlideName, linkSlidePreserve, linkClockDate,
//     linkClockTime, linkClock24, linkVideoIgnoreLooping, linkFlash, linkFlashColor
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

// A presentation with nothing in it yet, to be given its slides: an id of its own, its
// name, and what every presentation of ProPresenter's own starts with. That was looked
// for in 98 of them, written by ProPresenter 7.8 to 21.4: all have a background and a
// CCLI block, empty where nothing was ever put in them; 97 have a chord chart; and 90
// have a timeline of 300 seconds. So a new one here has those four. It is stamped as
// ProPresenter 7.16's, as everything this app makes is, with no word of which system
// wrote it, there being none of ProPresenter's to name.
//
// A GUESS, and untested: that ProPresenter opens a presentation that started here.
// None has been opened in it yet.
rv::data::Presentation newPresentation(const QString &name);

// A name as a presentation can have it, which is one its file can have: what was asked
// for with a slash turned into a dash, and no dots or spaces at its start (a file
// whose name starts with a dot is hidden). Empty if that leaves nothing.
QString presentationName(const QString &name);

// The file a presentation called `name` gets in the library folder `library`: the
// name as presentationName() has it, `fallback` if that leaves nothing, and a number
// after it if the library has one of that name already. Nothing is made here.
// `*unique` is the name it ends up with, which is the name to write inside the file
// too.
QString presentationFileFor(const QString &library, const QString &name, const QString &fallback, QString *unique);

// A name for a new element: `base`, or `base` and the first number that makes it one
// no element of the slide has.
QString uniqueElementName(const rv::data::Slide &slide, const QString &base);

} // namespace proconvert
