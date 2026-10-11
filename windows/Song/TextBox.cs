// What the editors are handed: one entry for each text box that holds the song's words,
// in the order of the slides and, on a slide, of its boxes (spec/05-song-and-saving.md,
// "What the editors are handed"; spec/01-file-format.md says which boxes those are).

using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

/// <param name="Slide">Which slide it is on, counted from 0 in the order the slides are shown.</param>
/// <param name="Group">The name of the slide's group, or "" for none.</param>
/// <param name="GroupColour">The group's colour, if the file gives it one.</param>
/// <param name="StartsItsGroup">Whether this slide is the first of its group.</param>
/// <param name="Element">
/// The text box's id, by which a change names it, WITH ITS SLIDE. It is not the only one
/// of its name in a song: a slide made by duplicating another keeps its boxes' ids, and
/// in one real song the boxes of 27 slides had one id between them. Nothing must ever
/// tell two boxes apart by id alone. Empty for a blank slide that has no text box yet:
/// one is made when a chord is put there.
/// </param>
/// <param name="Words">Its plain text.</param>
/// <param name="Chords">
/// Its chords in order, each a place along the words and a name. As the file has them:
/// a name is not tidied on the way in, nor held to being a chord.
/// </param>
/// <param name="Blank">
/// Whether it is a blank slide's box: no words, no chords, and handed over only so that
/// chords can be put on it.
/// </param>
public sealed record TextBox(
    int Slide,
    string Group,
    Colour? GroupColour,
    bool StartsItsGroup,
    string Element,
    string Words,
    IReadOnlyList<Chord> Chords,
    bool Blank);

/// <summary>A colour as the file has one: each part from nought to one.</summary>
public readonly record struct Colour(double Red, double Green, double Blue, double Alpha = 1);
