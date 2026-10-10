// ChordRules: the rules of chords, with nothing of ProPresenter's file format and
// nothing of a window in them.
//
// EMPTY SO FAR. This is milestone M0, which only sets the layers up; the rules are M1
// (spec/11-plan.md). What is to come here, each held to a file of testdata/vectors
// (spec/02-chord-rules.md says which rule each file holds):
//
//   - what a chord's name is, and what is no chord;
//   - keys, as the file numbers them and as a musician writes them;
//   - moving a chord to another key, and the notations it can be shown in;
//   - what is offered as a chord is typed;
//   - a text box's chords in order, and as the ranges the file writes them in;
//   - ChordPro text, to and from words with chords;
//   - the words of an RTF, counted as the file counts them;
//   - which key a song is likely in.
//
// Two things to hold to when it is written:
//
//   - The vectors are the rules. The code is right when it gives their answers, and an
//     expected answer is never changed to make a test pass (AGENTS.md).
//   - A place in a song's words is a number of UTF-16 units, a line break being one.
//     Swift's String does not count that way: `count`, `prefix(n)` and distances between
//     `String.Index`es count whole characters, so that an emoji is one where the file
//     counts two. Places are to go through one small type here that counts in
//     `utf16`, so that the rest of the code cannot get it wrong (spec/10-platforms.md,
//     "Counting characters").
//
// Simple Presenter's own rules, to read beside the spec: reference/simple-presenter/
// src/chords.h and src/chords.cpp.
