// ChordRules: the rules of chords, with nothing of ProPresenter's file format and
// nothing of a window in them. Plain functions over strings and numbers.
//
// This file is the way in. `Chords` is the name the rules stand under, as the C++ they
// were ported from has them under `chords::` (reference/simple-presenter/src/chords.h,
// whose comments are the specification, and chords.cpp). They keep the names they have
// there. What each is, is in spec/02-chord-rules.md, and what each must answer is in a
// file of testdata/vectors, which the tests read:
//
//   Keys.swift             keyName, keyNumber, keyIsMinor, majorKeys, minorKeys
//   ChordNames.swift       parts, tidied, couldBecome, isChord
//   Transposing.swift      transposed, notated, shown
//   Suggestions.swift      diatonic, completions
//   ChordsInABox.swift     tidy, ranges, carried
//   ChordPro.swift         toChordPro, fromChordPro, chordsOf, withoutChords, placeholders
//   ChordProSong.swift     parseSong, slidesOf
//
// and three that are not from chords.cpp, each under a name of its own:
//
//   RTFWords.swift         RTFWords.words: the plain text of a text box's RTF
//   KeyFit.swift           KeyFit.likely: which key a song's chords are most likely in
//   OldEditorShift.swift   OldEditor: where the first Windows editor meant a chord to be
//
// What to hold to, changing any of it:
//
//   - The vectors are the rules. The code is right when it gives their answers, and an
//     expected answer is never changed to make a test pass (AGENTS.md).
//   - Everything is done on UTF-16 units, not on Swift's characters: Units.swift says
//     why and has the operations; Words.swift is the type a text box's words are kept in.
//   - What a chord is taken to be: a root note, then whatever says what kind of chord it
//     is, then perhaps a slash and a bass note. Only the two notes are understood. What
//     stands between them is carried as it is, which is why a chord nobody has heard of
//     ("G1", which a Multitracks song has) is still moved to another key rightly.
//   - In the file a chord is written in the song's ORIGINAL key, whatever key it is
//     shown in. The file is never rewritten to change key (spec/05-song-and-saving.md).

/// The rules of chords. A name to stand under, not a thing to make.
public enum Chords {}

/// One chord, standing before the unit at `at` of its text box's words (or of its line,
/// where a line is what is being spoken of).
public struct Chord: Hashable, Sendable {
    public var at: Int
    public var name: String

    public init(at: Int, name: String) {
        self.at = at
        self.name = name
    }
}
