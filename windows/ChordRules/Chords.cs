// ChordRules: what a chord is, and everything that follows from that without knowing of
// a file or a window. This file is the way in: the few types the rules hand about, and
// the list of where each rule is.
//
// The rules are a port of the working editor's, reference/simple-presenter/src/chords.cpp
// (C++, for Qt), function for function and under the same names, as
// spec/02-chord-rules.md asks. They stand under `Chords`, as the C++ has them under
// `chords::`: Chords.Transposed("C#m", "E", "Db"). The Mac app's port is
// mac/Core/Sources/ChordRules, file for file under the same names as here, to be read
// beside this one. The code is right when it gives the answers in testdata/vectors
// (AGENTS.md, "The rules are the vectors"); Tests/ChordRules.Tests reads every case of
// every file.
//
//   Text.cs          what "white space", "a letter" and "trimmed" mean here: Qt's, which
//                    the vectors were made with, and not quite .NET's
//   Notes.cs         a note: reading one, writing one, moving one to another key
//   Keys.cs          the twenty-one keys of the file, and the twenty-six a picker offers
//   ChordNames.cs    a chord's parts; what may be typed; a typed name tidied
//   Transposing.cs   a chord in another key, and in each of the four notations
//   Suggestions.cs   a key's own seven chords; what a chord being typed might become
//   ChordsInABox.cs  a text box's chords in order, as the file writes them, and when its
//                    words change
//   ChordPro.cs      chords as ChordPro text, and the stand-ins of a line with no words
//   ChordProSong.cs  a ChordPro file read
//
// Three things here are not from chords.cpp, and have names of their own:
//
//   RtfWords.cs      the words of a text box: the plain text of its RTF. And the one
//                    thing the apps ever write into an RTF: the stand-ins
//   KeyFit.cs        which key a song's chords are most likely in (new: tools/keyfit.py)
//   OldEditor.cs     where the first Windows editor meant a chord to be (new:
//                    tools/make-fixtures.py)
//
// WHAT TO KNOW BEFORE CHANGING ANY OF IT.
//
// A place is a number of UTF-16 units, and a .NET string counts in those already
// (spec/10-platforms.md, "Counting characters"). So a text box's words are a plain
// `string` here and a place is an `int` into it; where the Mac needs a type of its own
// to keep Swift from counting by whole characters, nothing is needed. A string may hold
// half of a pair with its other half missing, which an RTF can say and the file counts
// as one place: it is kept as it is, never mended.
//
// But many of .NET's everyday calls on a string do not go unit by unit unless told to:
// `StartsWith`, `IndexOf`, `Compare` and `ToUpper` go by the computer's language. Every
// such call here says `Ordinal`, and the build stops on one that does not
// (windows/.editorconfig). Sorting is done with LINQ's `OrderBy`, which keeps those
// that are equal in the order they came in; `List.Sort` and `Array.Sort` do not promise
// that, and the rules depend on it.
//
// No file of this project has a character outside ASCII in it, nor a backslash and a
// "u": the characters that cannot be seen are written as numbers (Text.cs). Typed any
// other way by an agent they have arrived as something else (windows/NOTES.md).

namespace Pro7ChordEditor.ChordRules;

/// <summary>
/// One chord, standing before the character at <c>At</c> of its text box's words (or of
/// its line's, where a line is what is in hand).
/// </summary>
public readonly record struct Chord(int At, string Name);

/// <summary>
/// The stretch of the words a chord is written over in the file, from <c>Start</c> to
/// <c>End</c>. (<c>Range</c> in the reference. .NET has a Range of its own.)
/// </summary>
public readonly record struct ChordRange(int Start, int End, string Name);

/// <summary>A chord's name taken apart. Not valid if it does not start with a note.</summary>
/// <param name="Root">"F#"</param>
/// <param name="Quality">"m7", or "" for a plain major chord</param>
/// <param name="Bass">"A#", or "" for none</param>
public readonly record struct Parts(string Root, string Quality, string Bass, bool Valid);

/// <summary>A line of words and its chords, each <c>At</c> counted along the line.</summary>
public sealed record Line(string Text, IReadOnlyList<Chord> Chords);

/// <summary>ProPresenter's four ways of writing a chord, numbered as its file numbers them.</summary>
public enum Notation
{
    Letters = 0,
    Numbers = 1,
    Numerals = 2,
    DoReMi = 3,
}

/// <summary>One part of a song read from a ChordPro file: "Verse 1", "Chorus", or "" where the file names none.</summary>
public sealed record Section(string Name, IReadOnlyList<Line> Lines);

/// <summary>
/// A song as a ChordPro file has it. (<c>Song</c> in the reference. Here that name is
/// the layer above this one.)
/// </summary>
public sealed record ParsedSong(string Title, string Artist, string Key, string Copyright, string Ccli, IReadOnlyList<Section> Sections);

/// <summary>The rules of chords. Each file of this project adds its part.</summary>
public static partial class Chords
{
}
