# The editor being copied

The source of the chord editor of
[Simple Presenter](https://github.com/greyshirtguy/Simple-Presenter), as it is at
commit `2e67b1a` (2026-10-14, the change that followed version 0.94). It is here to be
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
changed where it stands in the RTF, by the rule of `tools/standins.py`. In its
version 0.90 a chord of the strip can be dragged onto a word (`spec/03`, "A chord
dragged out of the strip"). And in the change after its version 0.94, this commit, it
took back what the two apps had come to since: the line of hints under the sheet and
its highlighted line (rules 53a to 53h, the line's colour in the settings), sharps and flats
drawn as ♯ and ♭ (`chords::withSigns`, held to `signs.json`, with its switch in the
settings), an empty box that is kept taking the chord off (rule 35a), and a chord's
name orange as it comes. So what is here is no longer only the editor that was
copied: it is the third editor, meant to work, look and feel as the other two do,
and "Where this and `spec/` differ" below is what is still not the same.

The vectors in `testdata/` were made again from this commit
([`testdata/README.md`](../testdata/README.md) says what came of it).

It is C++ and QML, for Qt. Neither new app uses Qt. But the rules are plain functions
that read the same in any language, and every file opens with a comment that says what
it is for and why it is the way it is, which is most of the point of having it here.

## What to read for what

| For | Read | Lines |
| --- | --- | --- |
| The chord rules: names, keys, moving key, notations, what is offered, order and ranges, ChordPro, reading a ChordPro file, sharps and flats drawn as signs | `src/chords.h` (the comments are the specification), `src/chords.cpp` | 1,010 |
| The tests those rules were built with | `tests/tst_chords.cpp` (the signs held to `signs.json`) | 330 |
| Where chords are in a file, and reading and writing them | `src/proconvert.h` (search for "chords"), `src/proconvert.cpp` (`readChords`, `writeChords`) | |
| The words of an RTF | `src/rtf.h`, `src/rtf.cpp` | 480 |
| **A line of chords alone changed where it stands in the RTF**: `tools/standins.py` in C++, held to `testdata/vectors/stand-ins-in-rtf.json`; and where the other ranges of a text box go when it is (`placeAfter`) | `src/rtfstandins.h` (the comment is the rule), `src/rtfstandins.cpp`, `tests/tst_rtfstandins.cpp` | 680 |
| The song: what the editors are handed, their changes, saving, undo, the two keys, Revert | `src/presentationeditor.h`, `src/presentationeditor.cpp`: `song`, `usedChords`, `setChords`, `setChordsAlone`, `key`, `setKey`, `shownKey`, `setShownKey`, `change`, `undo`, `redo`, `backUp`, `revertToOpened`, `takeBackRevert` | |
| **The chord sheet**: and in it `highlighted` and `attending` (the highlighted line), `drawn` (the signs), `finishTyping` (an empty box kept), `hintsNow` and `pasteWouldReplace` (what the line of hints is told) | `qml/ChordSheet.qml`, `qml/chordlayout.js` | 2,250 |
| **The line of hints**: what is said when, and how the hints are fitted into the width there is; and the line drawn | `src/sheethints.h` (the comments are the specification), `src/sheethints.cpp`, `tests/tst_sheethints.cpp`; `qml/HintLine.qml`; `SheetHints` in `src/chordsbridge.*` | 660 |
| The ChordPro editor | `qml/ChordProEditor.qml` | 620 |
| The switch between the editors | `qml/EditorModes.qml` | 280 |
| What the editors must do, as that app tests it: four hundred checks, each a sentence | `tests/chords-scripted-test.qml` (as it was before the two apps), `tests/chords2-scripted-test.qml` (the sheet as it came back from them), `tests/chords3-scripted-test.qml` (the keys, Revert, the ChordPro text), `tests/chords4-scripted-test.qml` (a chord dragged out of the strip), `tests/chords5-scripted-test.qml` (an empty box kept, the highlighted line, the signs, the line of hints, and the settings for them) | 3,200 |
| A song from a ChordPro file | `src/songimport.h`, `src/songimport.cpp`, `qml/ImportSong.qml` | |
| How it is all described to the person using it | `README-chords.md` | |

Also here, for the few things that lean on them: `src/chordsbridge.*` (the rules as
QML calls them, and the colouring of ChordPro text), `src/richtext.*` and
`src/rtfwriter.*` (the model of formatted text that app reads RTF into and writes it
from: **not** to be copied, see `spec/01-file-format.md`), and `qml/ChordedText.qml`
(how that app draws chords on its stage screen, which this app has none of).

`tests/tst_rtfstandins.cpp` and `tests/tst_chords.cpp` read cases from a folder of
that repository, `tests/unit/data`, which is not copied here: the three files in it
are this repository's own `testdata/vectors/stand-ins-in-rtf.json`, `rtf-text.json`
and `signs.json`, to the byte.

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
- **Not here**: the guide (`spec/13-guide.md`), nor the strip's button for it; and
  of the line of hints, the row for ProPresenter running, nothing here asking
  whether it is. The line's keys are named in words, as on Windows, and the key that
  brings up the copying is Ctrl, Linux's key for shortcuts. While one of the strip's
  chords is pointed at, the quiet line under the line of hints says what that chord
  does (rule 46h), as the Mac's button says it of itself.
- **The highlighted line while one of the strip's chords is carried** is the line
  it would land on only where the lit bubble shows it would land, and none between
  two slides; the Mac app goes on lighting the spot's line there. Rule 53f says "is
  carrying a chord over it", and this reads "over" as where it would land.
- **Ctrl+1, Ctrl+2 and Ctrl+S** are not the editors' here: this app has them for
  its screens and for showing, as ProPresenter does, and its README says so.
- Everything about the library, the copies and the mending is the apps' alone. (Of
  the copies this code has one thing: Revert, which puts the song being edited back
  as it was when it was opened.)

Where the pages say what Simple Presenter "did" or "had", they speak of it as it was
when they were written, up to its version 0.88, and are left standing as the reason
a rule is as it is.
