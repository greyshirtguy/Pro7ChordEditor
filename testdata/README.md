# The test data

What both apps' tests are held to. Nothing here is written by hand except the songs:
the rest is made by programs, from the editor that already works, and is made again by
running them, never edited.

## `vectors/`

What the rules answer, as JSON. Each file opens with an `about` that says what is in
it; [`spec/02-chord-rules.md`](../spec/02-chord-rules.md) says which rule each holds.

| File | What it holds | Made by |
| --- | --- | --- |
| `keys.json`, `chord-names.json`, `transposed.json`, `notated.json`, `shown.json`, `diatonic.json`, `completions.json`, `tidy.json`, `ranges.json`, `carried.json`, `chordpro.json`, `parse-song.json` | The chord rules | `tools/make-vectors` (C++), from Simple Presenter's `src/chords.cpp` |
| `rtf-text.json` | The words of an RTF, unit by unit | the same, from its `src/rtf.cpp` |
| `sheet-model.json`, `layout.json` | What the chord sheet does with a song | `tools/make-vectors/sheet.js`, by running the functions of its `qml/ChordSheet.qml` and `qml/chordlayout.js` |
| `key-fit.json` | Which key a song is likely in | `tools/keyfit.py`, which is the reference for that rule |
| `line-break-shift.json` | One of the checks for mending | `tools/make-fixtures.py`, likewise |

The first three rows are a record of what Simple Presenter does. The last two are of
rules that are new here.

Some strings hold characters that cannot be seen: U+200B (a zero-width space) and
U+2001 (an em quad). Compare strings unit by unit.

## `songs/`

Five small ChordPro files, written to try the rules for reading one
([`spec/09-chordpro-files.md`](../spec/09-chordpro-files.md)): directives and none, parts
named every way, a line of chords alone, chords in the middle of words, a section of
tablature to pass over, and one with Windows line ends. Their words are hymns long
out of copyright (Amazing Grace, 1779; Holy, Holy, Holy, 1826; It Is Well, 1873) and
made-up lines; the chords are plain ones put on for the purpose.

## `files/`

Presentations, as `.pro` files.

- **`made-by-simple-presenter/`**: the five songs, each brought in by Simple
  Presenter's ChordPro import (two lines to a slide; four for It Is Well). They are
  what a clean song with chords looks like, as far as that app knows how to write one.
  **None of them was written by ProPresenter**, and none has been opened in it. The
  first real one is asked of the maintainer in [`spec/11-plan.md`](../spec/11-plan.md).
- **`as-the-old-windows-app-left-them/`**: four of them as the first Windows editor
  would have saved them, each with a different fault
  ([`spec/08-repair.md`](../spec/08-repair.md)). They are named as it named its copies,
  "... (Chords).pro". Made by `tools/make-fixtures.py`, which says at its top exactly
  what was done to each.
- **`made-by-propresenter/`**: one song as ProPresenter itself wrote it, version 7.9 on
  a Mac: *Rock of Ages*, from the maintainer's own library, copied as it was, byte for
  byte. It may be here because nothing in it is anyone's: the words are Toplady's of
  1776 and the tune named with them is Hastings' of 1830, and besides them it holds a
  CCLI song number, a font's name, and ProPresenter's own ids and settings. It has no
  chords and states no key. It is the only test file that shows what a real one is
  like, and it is unlike the others in two ways that matter: its cues are **not** in
  the order its groups name them, and every one of its text boxes has a link (of the
  kind that says when the box is to be seen, which leaves a box its own words). Its
  groups have colours. Its description in `expected/` was made by
  `tools/look.py --json`, with `"folder"` added as `make-fixtures.py` adds it; that
  tool does not know this folder yet, and
  `as-read-by-simple-presenter.json` does not have this file yet ("To be made again",
  below).
- **`expected/`**: what is in each file.
  - `<name>.json`: its words and every chord with its range as the file has it, by
    `tools/look.py --json`, which reads the file through `protoc`.
  - `as-read-by-simple-presenter.json`: every file as Simple Presenter's own editor
    reads it. This is the reference for reading a file. (The two were made
    independently and agree.)
  - `repairs.json`: what the checks find in each, and for the damaged ones what their
    chords are once put right.

The ids inside these files are random and the songs carry the time they were made, so
making them again gives other bytes that say the same.

## Making it again

When Simple Presenter's chord rules change, or a case is wanted that is not here. It
needs the Linux machine Simple Presenter is built on, with Qt 6, `protoc` and Node.

```
# 1. the rules' answers
cmake -S tools/make-vectors -B /tmp/make-vectors -G Ninja -DSIMPLE_PRESENTER=/path/to/Simple-Presenter
cmake --build /tmp/make-vectors
/tmp/make-vectors/make-vectors testdata/vectors
node tools/make-vectors/sheet.js /path/to/Simple-Presenter testdata/vectors
python3 tools/keyfit.py testdata/vectors

# 2. the clean presentations, with Simple Presenter itself (a build with its test hook)
#    in an empty workspace of its own: tools/README.md has the whole of it
# 3. the damaged ones, and what is in every file
python3 tools/make-fixtures.py
```

`sheet.js` checks its own stand-ins against the vectors of step 1 and stops if they
disagree; `make-fixtures.py` checks its reader of RTF the same way. Run them in that
order.

## To be made again, and cases wanted

What has been decided or found since this data was made, that it does not yet hold.
Each needs Simple Presenter or a tool changed first, on the Linux machine; nothing here
is to be put right by editing a file of answers.

**1. The range of a chord on a line of chords alone** is to be one long, its stand-in
only, as Multitracks writes it ([`spec/01-file-format.md`](../spec/01-file-format.md),
Chords; decided 2026-10-10). Three chords on such a line are 0 to 1, 2 to 3, 4 to 5,
not 0 to 2, 2 to 4, 4 to 5.

- Simple Presenter: `chords::ranges`, and what `chords.h` says of it. On a line that
  `isPlaceholders`, a chord's range ends one after it starts.
- `tools/make-fixtures.py`: `right_ranges`, which is the same rule in Python and is
  what mending check 1 writes.
- Then, made again: `vectors/ranges.json`; the three clean files that have such a line
  (Amazing Grace, It Is Well, Plain Song) and their damaged twins; `files/expected/`,
  `repairs.json` with it.
- The Mac app has the rule already (the maintainer's word, 2026-10-10: not to wait).
  One case of `ranges.json` is therefore known to it as made before the decision, and
  its tests say exactly what they allow of that case and fail when the file has been
  made again, so that the allowance is taken out
  (`mac/Core/Tests/ChordRulesTests/ChordsInABoxTests.swift`). The Windows app should
  not need one: by then the file will be new.

**2. Blank slides** are to be shown, and to take chords
([`spec/01-file-format.md`](../spec/01-file-format.md), "A blank slide"; decided
2026-10-10). Simple Presenter leaves them out, and nothing here has one. Wanted, once
it has them: a test song with a blank slide in it (an intro made as an empty slide,
and a blank slide in the middle of a group), in `files/` and in
`as-read-by-simple-presenter.json`; and in `vectors/sheet-model.json`, the rows such a
song makes, where the spot can stand on its blank row, and what is asked when a chord
is put there.

**3. The file ProPresenter wrote**, `files/made-by-propresenter/Rock Of Ages
(Toplady).pro`, is to be read by Simple Presenter into
`as-read-by-simple-presenter.json` like the others: twelve text boxes, in the hymn's
order, three to each of four verses. (Simple Presenter leaves out a box with a link,
but for a link that only says when the box is to be seen; if it hands over none of
these, that is a thing to put right there.) And `tools/make-fixtures.py` is to be
taught the folder, as a third kind beside the clean and the damaged, so that it
describes the file in `expected/` and does not take it for a damaged one.

**4. Rules that no vector holds.** Found while the Mac app's rules were built: with
every vector passing, each rule was broken in one small way in turn, and these could
be got wrong without any file here noticing. The Mac app holds each with a case of its
own (`mac/Core/Tests/ChordRulesTests/NotHeldByVectorsTests.swift`), whose expected
answer was read from the reference's code and not got from it. Put these inputs into
`tools/make-vectors` and `tools/keyfit.py`, and the answers will be the reference's
own, for both apps.

| Where | Input wanted | The rule it holds |
| --- | --- | --- |
| `chord-names.json` | `G#b5` | A flat after a sharp is not part of the note |
| `chord-names.json`, `transposed.json` | `C/Em`, `c/em` | What follows the last slash is a bass only if it is a note and nothing more |
| `transposed.json` | `g` from C to C; a chord with a music flat in it, from E to E | The same key twice gives the chord back exactly as written |
| `carried.json` | The same words before and after, two chords in one place | Unchanged words are only tidied, so the later of the two stands |
| `carried.json` | `abab` to `ab`, a chord at 2 | The shared end is not looked for inside the shared start |
| `chordpro.json`, `toText` | Three lines, the middle one empty, a chord on it | A chord on an empty line is written on that line |
| `chordpro.json`, `withoutChords` | A `[` with a line break before its `]`; `[[G]]` | A bracket is not closed on another line, nor round another bracket |
| `parse-song.json` (a sixth song) | Two `{artist}` lines; an empty line inside `{start_of_chorus}`; a line starting `[G]` and two spaces; the headings `CHORUS:`, `verse 2b`, `pre-chorus 2:`, `[PRE CHORUS]`; and `Verse 2bb` and `Chorus of angels`, which are not headings | The first artist stands; a gap in a marked part; chords move with trimmed words; what a heading is |
| `rtf-text.json` | A group that sets the count of characters to pass over to nought, and after the group a numbered character with one stand-in after it | That count is a group's own, put back when the group closes |
| `key-fit.json` | A stated key 0.2, exactly 0.25, and 0.3 below the best; a song whose best score is 0.6; E flat's chords written in sharps, with C minor used as often; a chord with the little circle | "More than a quarter below"; "at least 0.75"; the major before its relative minor; the circle is diminished |
| `tidy.json`, `ranges.json`, `carried.json`, `chordpro.json` | Words with an emoji in them | Places are UTF-16 units. Only `rtf-text.json` has a character from beyond the first 65,536 |

## What must not come in here

Anyone's real songs. Not the maintainer's, not a tester's, not one "just for a test":
they are other people's copyright, and Multitracks' files carry who licensed them. A
case that needs a real file to show it is described in words, and a made-up file is
built to show the same thing.
