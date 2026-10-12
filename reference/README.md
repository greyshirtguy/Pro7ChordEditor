# The editor being copied

The source of the chord editor of
[Simple Presenter](https://github.com/greyshirtguy/Simple-Presenter), as it is at
commit `98fc328` (2026-10-12, the change that followed version 0.89). It is here to be
**read**: not built, not changed, not copied line for line into another language
where that language has a better way.
It is the same person's work, under the same licence (`simple-presenter/LICENSE`).

It was first put here as it was after version 0.82 (commit `1311d80`, 2026-10-10),
and that is what the two apps were built from. Since then the copying has gone the
other way as well. In its version 0.89 Simple Presenter took back what building the
two apps had found: the sheet as a grid of slides that zooms, the switch between the
editors in the middle of the toolbar, the song's two keys, Revert, a name that is no
chord moved and copied like any other, what a paste says, the empty lines a box's
words end with left off the sheet, and a line of chords alone that keeps one stand-in
when it loses its last chord. In the change after it, a line of chords alone is
changed where it stands in the RTF, by the rule of `tools/standins.py`. So what is
here is no longer only the editor that was copied: it is the third editor, meant to
work, look and feel as the other two do, and "Where this and `spec/` differ" below
is what is still not the same.

The vectors in `testdata/` were made again from this commit
([`testdata/README.md`](../testdata/README.md) says what came of it: not one case
that was there came out differently).

It is C++ and QML, for Qt. Neither new app uses Qt. But the rules are plain functions
that read the same in any language, and every file opens with a comment that says what
it is for and why it is the way it is, which is most of the point of having it here.

## What to read for what

| For | Read | Lines |
| --- | --- | --- |
| The chord rules: names, keys, moving key, notations, what is offered, order and ranges, ChordPro, reading a ChordPro file | `src/chords.h` (the comments are the specification), `src/chords.cpp` | 930 |
| The tests those rules were built with | `tests/tst_chords.cpp` | 300 |
| Where chords are in a file, and reading and writing them | `src/proconvert.h` (search for "chords"), `src/proconvert.cpp` (`readChords`, `writeChords`) | |
| The words of an RTF | `src/rtf.h`, `src/rtf.cpp` | 480 |
| **A line of chords alone changed where it stands in the RTF**: `tools/standins.py` in C++, held to `testdata/vectors/stand-ins-in-rtf.json`; and where the other ranges of a text box go when it is (`placeAfter`) | `src/rtfstandins.h` (the comment is the rule), `src/rtfstandins.cpp`, `tests/tst_rtfstandins.cpp` | 680 |
| The song: what the editors are handed, their changes, saving, undo, the two keys, Revert | `src/presentationeditor.h`, `src/presentationeditor.cpp`: `song`, `usedChords`, `setChords`, `setChordsAlone`, `key`, `setKey`, `shownKey`, `setShownKey`, `change`, `undo`, `redo`, `backUp`, `revertToOpened`, `takeBackRevert` | |
| **The chord sheet** | `qml/ChordSheet.qml`, `qml/chordlayout.js` | 1,780 |
| The ChordPro editor | `qml/ChordProEditor.qml` | 620 |
| The switch between the editors | `qml/EditorModes.qml` | 280 |
| What the editors must do, as that app tests it: three hundred checks, each a sentence | `tests/chords-scripted-test.qml` (as it was before the two apps), `tests/chords2-scripted-test.qml` (the sheet as it came back from them), `tests/chords3-scripted-test.qml` (the keys, Revert, the ChordPro text) | 2,100 |
| A song from a ChordPro file | `src/songimport.h`, `src/songimport.cpp`, `qml/ImportSong.qml` | |
| How it is all described to the person using it | `README-chords.md` | |

Also here, for the few things that lean on them: `src/chordsbridge.*` (the rules as
QML calls them, and the colouring of ChordPro text), `src/richtext.*` and
`src/rtfwriter.*` (the model of formatted text that app reads RTF into and writes it
from: **not** to be copied, see `spec/01-file-format.md`), and `qml/ChordedText.qml`
(how that app draws chords on its stage screen, which this app has none of).

`tests/tst_rtfstandins.cpp` reads its cases from a folder of that repository,
`tests/unit/data`, which is not copied here: the two files in it are this repository's
own `testdata/vectors/stand-ins-in-rtf.json` and `rtf-text.json`, to the byte.

## What is in these files that is not wanted

`presentationeditor.*` and `proconvert.*` are a whole slide editor: moving boxes,
formatting text, props, stage layouts, themes. Only the parts named above matter here.
Do not port the rest, and do not be led by it into changing more of a file than chords.

## Where this and `spec/` differ

`spec/` is what the two apps are built to, and since its version 0.89 what this code
is kept to as well, in everything the three have in common. What is still different:

- **Where a line of chords alone cannot be changed in place, this code writes the
  text box's RTF again whole** (`setChordsAlone`, which says so in the log); the apps,
  which have no writer of RTF, refuse (`spec/01-file-format.md`, "Chords with no
  words").
- **The other ranges of a text box go with the words here.** When a line of chords
  alone is made longer or shorter, this code moves the ends of the box's custom
  attributes that are not chords with the words (`rtfstandins::placeAfter`); the
  pages leave those as they are. `spec/01-file-format.md` has it, with what was found
  in real songs, under "Where Simple Presenter differs". It is the maintainer's to
  say which the three are to do.
- A blank slide's first chords are written into its text box here with the box's own
  font, size and colour, which the file keeps in `text.attributes` for an empty box
  too (all 229 blank boxes looked at on the Linux machine had them), and the RTF is
  written whole from that. `spec/01-file-format.md` has the apps take the opening of a
  neighbouring box's RTF instead, since they write no RTF of their own.
- A slide with no text box at all is given one here when a chord is put there, a
  text box of this code's own devising (`PresentationEditor::song` and
  `setChordsAlone`). The apps give it a copy of one of the song's own, and refuse
  where the song has none to copy (`spec/01-file-format.md`, "A slide with no text
  box").
- A new song is made from a song that is known to be good in the apps, and from
  nothing here (`spec/09-chordpro-files.md`).
- **Not here yet**: a chord dragged out of the strip onto a word
  (`spec/03-chord-sheet.md`, "A chord dragged out of the strip"), and the guide
  (`spec/13-guide.md`).
- Everything about the library, the copies and the mending is the apps' alone. (Of
  the copies this code has one thing: Revert, which puts the song being edited back
  as it was when it was opened.)

Where the pages say what Simple Presenter "did" or "had", they speak of it as it was
when they were written, up to its version 0.88, and are left standing as the reason
a rule is as it is.
