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

Seven small ChordPro files, written to try the rules for reading one
([`spec/09-chordpro-files.md`](../spec/09-chordpro-files.md)): directives and none, parts
named every way, a line of chords alone, chords in the middle of words, a section of
tablature to pass over, and one with Windows line ends. Their words are hymns long
out of copyright (Amazing Grace, 1779; Holy, Holy, Holy, 1826; It Is Well, 1873) and
made-up lines; the chords are plain ones put on for the purpose.

Two of them are for one thing each. `headings.cho` is the corners of what a heading
is, what a gap does inside a marked part, and which artist stands; it is read
(`parse-song.json`) and not made into a presentation. `blank-slides.cho` is what
Blank Slides is made from: where a blank slide is to be it has a line of one chord
alone, which is taken off again once the song is made (below).

## `files/`

Presentations, as `.pro` files.

- **`made-by-simple-presenter/`**: six songs, each brought in by Simple
  Presenter's ChordPro import (two lines to a slide; four for It Is Well; one for
  Blank Slides). They are what a clean song with chords looks like, as far as that
  app knows how to write one. **None of them was written by ProPresenter**, and none
  has been opened in it. The first real one is asked of the maintainer in
  [`spec/11-plan.md`](../spec/11-plan.md).

  **Blank Slides** has seven slides, three of them blank
  ([`spec/01-file-format.md`](../spec/01-file-format.md), "A blank slide"): its
  intro, and the third of its verse's five slides, each have a text box with no text
  in it;
  and its ending has nothing on it at all, no text box either, and is handed over
  with an empty `element`. They were made in Simple
  Presenter's editor after the import (`tools/simple-presenter-scripts/import.qml`
  says how), so the two empty text boxes were written by it: their RTF still names a
  font, where a blank text box left by ProPresenter is an RTF with no font in it at
  all. A song with a blank slide that ProPresenter made is still wanted
  ([`spec/11-plan.md`](../spec/11-plan.md), before M7).
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
  groups have colours. `make-fixtures.py` describes it in `expected/` with the others,
  and `as-read-by-simple-presenter.json` has it as Simple Presenter reads it.
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

## Made again on 2026-10-10, and what is still wanted

What stood under this heading as "to be made again, and cases wanted" was asked for by
the session that builds the Mac app. It was done the same day on the Linux machine,
from Simple Presenter as it was changed for it
([`reference/README.md`](../reference/README.md)). Nothing was edited by hand.

**1. The range of a chord on a line of chords alone** is one long, its stand-in only,
as Multitracks writes it. Changed: `chords::ranges` in Simple Presenter, and
`right_ranges` in `tools/make-fixtures.py`, which is the same rule in Python and now
holds itself to `ranges.json` each time it runs. Made again: `vectors/ranges.json`,
with more cases of it (stand-ins that are spaces, fewer chords than stand-ins, two
such lines in one box, one between lines of words, a line that is only spaces); every
clean file and its damaged twin; `files/expected/` and `repairs.json`.

The Mac app had the rule first, and its test of `ranges.json` allowed for the one
case that was made the old way, so as to fail when the file was made again
(`mac/Core/Tests/ChordRulesTests/ChordsInABoxTests.swift`). It fails now: the
allowance is to be taken out, and there is more than one such case to hold to.

**2. Blank slides.** Simple Presenter shows them and puts chords on them. New:
`songs/blank-slides.cho` and `files/made-by-simple-presenter/Blank Slides.pro` (above),
in `as-read-by-simple-presenter.json`, where every text box now says whether it is
`blank`; and in `vectors/sheet-model.json` the songs `blank` (its rows, where the spot
stands and walks, and what is asked when a chord is put on a blank line, typed there,
moved there and taken off again) and `bare` (a slide with no text box:
[`spec/01-file-format.md`](../spec/01-file-format.md), "A slide with no text box").

**3. The file ProPresenter wrote**, `files/made-by-propresenter/Rock Of Ages
(Toplady).pro`, is read by Simple Presenter into `as-read-by-simple-presenter.json`
like the others. `tools/make-fixtures.py` knows the folder as a third kind beside the
clean and the damaged: it describes the file in `expected/`, makes no damaged twin of
it, and leaves it out of `repairs.json`.

**4. The rules that no vector held.** Every input of the list that stood here is in
`tools/make-vectors/main.cpp` or `tools/keyfit.py` now, and its answer is the
reference's own:

| In | What was added |
| --- | --- |
| `chord-names.json` | `G#b5`, `C/Em`, `c/em` |
| `transposed.json`, `odd` | `C/Em` and `c/em` moved a tone; `g`, a chord with a music flat in it, and `f#m/a`, each from a key to the same key |
| `carried.json` | The same words before and after with two chords in one place; `abab` to `ab` with a chord at 2, and with a chord on every letter; words with a character of two places put in, and taken out |
| `chordpro.json` | `toText`: three lines, the middle one empty, with a chord on it. `ofText` and `withoutChords`: a `[` with a line break before its `]`, `[[G]]`, `[G[D]]` |
| `parse-song.json` | A sixth song, `headings.cho` (and a seventh, `blank-slides.cho`, which is plain) |
| `rtf-text.json` | A group that sets the count of characters to pass over to nought, and after it a numbered character with one stand-in |
| `key-fit.json` | A stated key 0.2, exactly 0.25 and 0.3 below the best; a song whose best key has three chords of five; E flat's chords written with sharps and C minor as often; a diminished chord written with the little circle |
| `tidy.json`, `ranges.json`, `carried.json`, `chordpro.json` | Words with a character from beyond the first 65,536 in them |

Where a case of `mac/Core/Tests/ChordRulesTests/NotHeldByVectorsTests.swift` and a
vector now say the same, the case can go, as that file's notes have it. Where they do
not say the same, the vector is what the reference answers.

**Still wanted**

- A song with a blank slide in it that ProPresenter itself made, of words anyone may
  use: the blank text boxes of Blank Slides were written by Simple Presenter
  ([`spec/11-plan.md`](../spec/11-plan.md), before M7).
- A test file whose chords are not in order of place in the file: the Mac app's notes
  say no file here would notice that rule being left out.
- In `songs/headings.cho`, a second chord on the line that has two spaces after its
  first (`[G]  Two spaces after the [C]chord`). As it is, the line's one chord is at
  the start, which is where it would be whether or not chords moved with the words
  when the spaces were taken off; so the rule can still be got wrong unnoticed. (Found
  by breaking it, after the vectors were made again: of the twenty breakages that
  only the Mac app's own cases caught before, the vectors now catch nineteen.)
- In `sheet-model.json`, six things that no case holds, each found by breaking the
  Mac app's sheet one rule at a time (2026-10-11; it has cases of its own for them,
  `ChordSheetTests`):
  - in `rows`, a text box with a chord ON an empty line of it, and one on a line
    break: the first is on that line's row, an empty line being one place wide, and
    the second on no row;
  - in `rows`, a slide that starts a group whose name is empty: no row for the name;
  - in `edits`, a chord of a line of chords alone moved to the line's FRONT (the one
    case there moves the first to the end, which adding it at the end also gives);
  - in `copyPaste`, a line with two chords on one word, pasted onto a line whose
    word there is one letter long: both land on it, and the first is the one put;
  - in `copyPaste`, a whole group whose first line is a line of chords alone, pasted
    onto lines of words. (The reference joins the first change it then makes to
    whatever was done before the paste; [`spec/03`](../spec/03-chord-sheet.md), rule
    52, says what these apps do instead. So this one is first Simple Presenter's to
    put right, if the maintainer agrees it is wrong.)

## What must not come in here

Anyone's real songs. Not the maintainer's, not a tester's, not one "just for a test":
they are other people's copyright, and Multitracks' files carry who licensed them. A
case that needs a real file to show it is described in words, and a made-up file is
built to show the same thing.
