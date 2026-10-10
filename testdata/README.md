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

## What must not come in here

Anyone's real songs. Not the maintainer's, not a tester's, not one "just for a test":
they are other people's copyright, and Multitracks' files carry who licensed them. A
case that needs a real file to show it is described in words, and a made-up file is
built to show the same thing.
