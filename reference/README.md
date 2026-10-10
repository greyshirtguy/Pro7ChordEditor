# The editor being copied

The source of the chord editor of
[Simple Presenter](https://github.com/greyshirtguy/Simple-Presenter), as it was at
commit `1311d80` (2026-10-10, after version 0.82): with the two changes of that day,
which write a chord with no words over its own stand-in only and let a blank slide
take chords. The vectors in `testdata/` were made from the same commit. It is here to
be **read**: not built, not changed, not copied line for line into another language
where that language has a better way.
It is the same person's work, under the same licence (`simple-presenter/LICENSE`).

It is C++ and QML, for Qt. Neither new app uses Qt. But the rules are plain functions
that read the same in any language, and every file opens with a comment that says what
it is for and why it is the way it is, which is most of the point of having it here.

## What to read for what

| For | Read | Lines |
| --- | --- | --- |
| The chord rules: names, keys, moving key, notations, what is offered, order and ranges, ChordPro, reading a ChordPro file | `src/chords.h` (the comments are the specification), `src/chords.cpp` | 900 |
| The tests those rules were built with | `tests/tst_chords.cpp` | 270 |
| Where chords are in a file, and reading and writing them | `src/proconvert.h` (search for "chords"), `src/proconvert.cpp` (`readChords`, `writeChords`) | |
| The words of an RTF | `src/rtf.h`, `src/rtf.cpp` | 480 |
| The song: what the editors are handed, their changes, saving, undo | `src/presentationeditor.h`, `src/presentationeditor.cpp`: `song`, `usedChords`, `setChords`, `setChordsAlone`, `key`, `setKey`, `change`, `undo`, `redo`, `backUp` | |
| **The chord sheet** | `qml/ChordSheet.qml`, `qml/chordlayout.js` | 1,250 |
| The ChordPro editor | `qml/ChordProEditor.qml` | 365 |
| What the sheet must do, as that app tests it: eighty checks, each a sentence | `tests/chords-scripted-test.qml` | 500 |
| A song from a ChordPro file | `src/songimport.h`, `src/songimport.cpp`, `qml/ImportSong.qml` | |
| How it is all described to the person using it | `README-chords.md` | |

Also here, for the few things that lean on them: `src/chordsbridge.*` (the rules as
QML calls them, and the colouring of ChordPro text), `src/richtext.*` and
`src/rtfwriter.*` (the model of formatted text that app reads RTF into and writes it
from: **not** to be copied, see `spec/01-file-format.md`), and `qml/ChordedText.qml`
(how that app draws chords on its stage screen, which this app has none of).

## What is in these files that is not wanted

`presentationeditor.*` and `proconvert.*` are a whole slide editor: moving boxes,
formatting text, props, stage layouts, themes. Only the parts named above matter here.
Do not port the rest, and do not be led by it into changing more of a file than chords.

## Where this and `spec/` differ

`spec/` is what is to be built. It follows this code nearly everywhere and says where
it does not, each time under a heading or a rule that says so. The main ones:

- A chord's name that is not a chord is kept, moved and copied like any other
  (`spec/03-chord-sheet.md`, `spec/04-chordpro-editor.md`).
- The RTF of a text box is never written again whole; a line of chords alone is
  changed in place (`spec/01-file-format.md`).
- A new song is made from a song that is known to be good, not from nothing
  (`spec/09-chordpro-files.md`).
- There is a second key, the one ProPresenter shows the song in
  (`spec/05-song-and-saving.md`).
- A blank slide's first chords are written into its text box here with the box's own
  font, size and colour, which the file keeps in `text.attributes` for an empty box
  too (all 229 blank boxes looked at on the Linux machine had them), and the RTF is
  written whole from that. `spec/01-file-format.md` has the apps take the opening of a
  neighbouring box's RTF instead, since they write no RTF of their own.
- A slide with no text box at all is on this code's sheet, and is given a text box
  when a chord is put there (`PresentationEditor::song` and `setChordsAlone`). The
  apps leave such a slide out (`spec/01-file-format.md`, "Where Simple Presenter goes
  further").
- Taking the last chord off a blank slide again, while the song is open, leaves the
  file byte for byte as it was (`m_madeForChords`). The pages have the box left empty
  (`spec/01-file-format.md`, "A change of mind").
- Everything about the library, the copies and the mending is new.
