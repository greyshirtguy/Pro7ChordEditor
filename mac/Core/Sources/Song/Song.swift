// Song: the song that is open, between its file and the two editors. No window in it.
//
// It reads the file, hands the editors the words and chords, takes their changes, saves
// each one at once, and can undo them. Neither editor touches a file. This is what
// spec/05-song-and-saving.md describes, and it stands on the other two layers:
// ChordRules for what a chord is and how it is written, ProFormat for the file.
//
//   OpenSong.swift     the song: what it hands over, what it may be asked, undo
//   TextBox.swift      what the editors are handed
//   Reading.swift      which slides, in what order; which boxes hold a song's words
//   Writing.swift      a box's chords into the message; the message into the file
//   Safeguards.swift   what a song must be given before it may save anything
//   Refusal.swift      why a thing was not done, as a sentence for the person
//
// and, round the song, what the window is made of with no window in it:
//
//   Places.swift, Listing.swift, Marks.swift   where songs are kept, and the lists
//   Copies.swift, AppData.swift                the copies kept, and what the app keeps
//   SongText.swift, ChordProEditor.swift       the song as ChordPro text
//   ChordSheet.swift, ChordLayout.swift        the chord sheet
//
// The type is `OpenSong`, not `Song`: a type named as its module is cannot be told from
// it in code that imports it. "The song that is open" is what the page calls it.
//
// What is to come here, and when (spec/11-plan.md):
//
//   M8  the checks for what the old Windows editor left, and the mending
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
