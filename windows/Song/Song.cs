// Song: the song that is open, between its file and the two editors. No window in it.
//
// It reads the file, hands the editors the words and chords, takes their changes, saves
// each one at once, and can undo them. Neither editor touches a file. This is what
// spec/05-song-and-saving.md describes, and it stands on the other two layers:
// ChordRules for what a chord is and how it is written, ProFormat for the file.
//
// This file has no code in it. It is the way in: what is where.
//
//   OpenSong.cs     the song: what it hands over, what it may be asked, undo
//   TextBox.cs      what the editors are handed
//   Reading.cs      which slides, in what order; which boxes hold a song's words
//   Writing.cs      a box's chords into the message; the message into the file
//   Safeguards.cs   what a song must be given before it may save anything
//   Refusal.cs      why a thing was not done, as a sentence for the person
//
// And the library (spec/06-library.md), which finds songs and lists them and opens none:
//
//   Places.cs           where on this PC songs are kept as files
//   Listing.cs          a place's libraries, a library's songs, the search
//   Marks.cs            what is said of a song beside its name, and kept from one start to the next
//   SongText.cs         a song as lines of text: what is shown of it, and what the ChordPro editor will work on
//   ProPresenterApp.cs  whether ProPresenter is running
//   AppData.cs          what the app keeps of its own, and where: settings, marks, a log
//
// And the copies (spec/07-backups.md):
//
//   Copies.cs           the copies kept of a file before it is changed: kept, listed, thinned
//
// (Putting one back is the song's own, `OpenSong.PutBack`: it is a save like any other.)
//
// And the first of the two editors, as far as it is rules (spec/04-chordpro-editor.md):
//
//   ChordProEditor.cs   the song as one piece of text: what may be typed in it, the help
//                       with brackets, and what a pause in the typing saves
//
// The Mac app's is mac/Core/Sources/Song, file for file under the same names, and this
// was ported from it with the reference beside
// (reference/simple-presenter/src/presentationeditor.cpp), so that the two apps do the
// same thing to a file. The type is `OpenSong`, not `Song`: a type named as its
// namespace is makes trouble. "The song that is open" is what the page calls it.
//
// What is to come here, and when (spec/11-plan.md):
//
//   M6  the chord sheet's model
//   M8  the checks for what the first Windows editor left, and the mending
//   M9  a song made from a ChordPro file, and written out as one
//
// What holds for every line written here (AGENTS.md, "What must stay true"):
//
//   - A song's file is never left worse than it was found. Every write parses the
//     whole file, changes only what the change is about, and writes the whole thing
//     back with everything else as it came. Never build a file from the parts this
//     app knows.
//   - The words of a song are not this app's to change. Only chords are.
//   - Nothing is written while ProPresenter is running, nor before a copy of the file
//     has been kept.
//   - Tests never run on a real library: on testdata/files, or on copies in a
//     temporary folder.
//
// ABOUT THE FILE'S MESSAGES, which is where a port from Swift can go wrong unseen. In
// Swift a message is a value: giving it another name makes a copy. Here a message is an
// object: giving it another name is the SAME message, and changing the one changes the
// other. So everything that changes a message here first takes a copy of it (`Clone`),
// changes the copy, and only puts it in the song's place once it is saved; and a message
// kept for undo is never changed again. And a part of a message that is not there is
// nothing (`null`) here, where in Swift it reads as an empty one: each place that could
// meet one says what it does about it.
