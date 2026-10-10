// Song: a song as the editors see it, on top of ChordRules and ProFormat, with no
// window in it.
//
// EMPTY SO FAR. This is milestone M0, which only sets the layers up. What is to come
// here, and when (spec/11-plan.md):
//
//   M2  opening a .pro; handing over its text boxes; setting a box's chords; setting
//       the key; saving safely; undo and redo        (spec/01-file-format.md, 05)
//   M3  finding ProPresenter's library and listing it; whether ProPresenter is running
//                                                     (spec/06-library.md)
//   M4  the copies kept before a first change         (spec/07-backups.md)
//   M8  the checks for what the old Windows editor left, and the mending
//                                                     (spec/08-repair.md)
//
// What holds for every line written here (AGENTS.md, "What must stay true"):
//
//   - A song's file is never left worse than it was found. Every write parses the
//     whole file, changes only what the change is about, and writes the whole thing
//     back with everything else as it came. Never build a file from the parts this
//     app knows.
//   - The words of a song are not this app's to change. Only chords are.
//   - Nothing is written while ProPresenter is running.
//   - Tests never run on a real library: on testdata/files, or on copies in a
//     temporary folder.
//
// The imports are here so that the three layers are joined from the start, and a
// mistake in how they are joined shows at M0 and not at M2.

import ChordRules
import ProFormat
