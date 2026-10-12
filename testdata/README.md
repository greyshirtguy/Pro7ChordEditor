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
| `stand-ins-in-rtf.json` | The bytes a text box's RTF becomes when a line of chords alone is given more stand-ins or fewer, and the RTF made for a box that has no words ([`spec/01-file-format.md`](../spec/01-file-format.md), "Chords with no words") | `tools/standins.py`, likewise |

The first three rows are a record of what Simple Presenter does. The last three are of
rules that are new here. (Of the last, Simple Presenter has the first part too since
2026-10-12, ported from `tools/standins.py` and held to the same file.)

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
  all. Chords With No Words (below) has one as ProPresenter leaves it.
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
- **`shaped-as-real-ones/`**: one song, **Chords With No Words**: the one above, with
  nine of its twelve slides made again in the shapes that real songs have for chords
  with no words. Putting a chord on such a slide is the one thing the apps do to the
  RTF of a song, so it has to be tried on RTF as real songs have it, and real songs
  are other people's. What was read from 425 of them (on the maintainer's Mac, for
  how they are built and nothing else) is made here on ProPresenter's own file by
  `tools/make-fixtures.py`, whose head lists every slide: a text box with no text as
  ProPresenter leaves one; one holding a stray space; a slide with nothing on it;
  chords alone as Multitracks writes them, each stand-in in the song's font and each
  em quad between two in another; chords on plain spaces; one chord on one space; a
  line of chords alone under a line of words, over two lines of words, and two such
  lines in one box. The words are Toplady's still, the chords are made up, and made
  again it is the same file to the byte.

  It is in `as-read-by-simple-presenter.json` since 2026-10-12 (below), and
  `expected/Chords With No Words.json` has what is in it, by the tool that reads it
  through `protoc`. The two agree: the same words and the same chords in the same
  places. (Simple Presenter hands over twelve entries and that tool lists eleven
  text boxes, the twelfth being the slide with nothing on it, which has no text box;
  and of the eleven, the box that holds a stray space is handed over as a blank
  slide's.)
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
python3 tools/standins.py testdata/vectors

# 2. the clean presentations, with Simple Presenter itself (a build with its test hook)
#    in an empty workspace of its own: tools/README.md has the whole of it
# 3. the damaged ones, the one in the shapes of real songs, and what is in every file
python3 tools/make-fixtures.py
```

`tools/standins.py testdata/vectors` and step 3 need only Python and `protoc`, and
run on any machine: neither reads anything of Simple Presenter's.

`sheet.js` checks its own stand-ins against the vectors of step 1 and stops if they
disagree; `make-fixtures.py` checks its reader of RTF the same way. Run them in that
order.

`sheet.js` by itself needs only Node and Simple Presenter's two QML files, which
`reference/simple-presenter` has: `node tools/make-vectors/sheet.js
reference/simple-presenter testdata/vectors` gives `sheet-model.json` and
`layout.json` on any machine, the same to the byte as the Linux machine gave them
(tried on a Mac, 2026-10-12, before anything below was changed).

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

## Made again on 2026-10-12, on the Mac: a line that has lost its last chord

**The maintainer decided** that a line of chords alone whose last chord is taken off
takes chords again, and so is left one zero-width space
([`spec/01-file-format.md`](../spec/01-file-format.md), "Chords with no words": "if a
blank line had chords and you deleted all - you should be able to put some back on
that line"). It is a rule of the song, not of the sheet, and the sheet's functions
are Simple Presenter's as they were. What changed is in this repository:

- **The stand-in for the song in `tools/make-vectors/sheet.js`**: a line with no
  chord is one zero-width space; and what was made for a blank slide's chords is
  remembered there as the real song remembers it, so that taking its chords off
  again in one sitting still leaves the slide blank.
- **`vectors/sheet-model.json`, made again with it.** Not one case that was there
  came out differently: none took the last chord off a line the song came with. So
  cases were added that do: a sixth song, `under` (a line of chords alone, on plain
  spaces, under a line of words in one text box), with its rows, its stops and two
  walks; and nine `edits`, on `intro` and `under`: every chord taken off a line and
  one put on it again, the same on a slide that hangs its one chord on a plain
  space (which is then a zero-width space), and on the line under the words. An
  edit can now be of more than two steps: `do.then` may have a `then`.
- **`vectors/stand-ins-in-rtf.json`**, by `tools/standins.py`: for every line that
  hangs its chords on plain spaces, the RTF it becomes as one zero-width space,
  which is now asked of it.

**Simple Presenter itself had not got the rule then.** It has it since its version
0.89, and the vectors made on that machine came out as these are (below).

## Made again on 2026-10-12, on the Linux machine: from Simple Presenter as it now is

Simple Presenter took back what the two apps had found (its version 0.89) and, in the
change after it, changes a line of chords alone where it stands in the RTF
([`reference/README.md`](../reference/README.md) has the whole of it, and the commit).
`reference/` was brought up to that commit and everything here that is made from
Simple Presenter was made again from it. Nothing was edited by hand.

**Not one case that was there came out differently.** That is the finding: the sheet
that was rewritten there as a grid, and the song that now keeps a stand-in on a line
that loses its last chord, answer every case as the vectors the two apps are held to
already had it, the nine made on the Mac with a stand-in for that rule among them.

- **The rules' answers** (`tools/make-vectors`, from its C++): all twelve files and
  `rtf-text.json` the same to the byte.
- **`layout.json`**: the same to the byte.
- **`sheet-model.json`**: every case that was there the same. Added to it:
  - `pasted`, in each case of `copyPaste`: what the sheet's `paste` answers
    (`did`: how many lines had their chords replaced, how many chords were put on
    them, how many were left out for having no word to go on, how many lines were
    passed over for being of the other kind, how many the song refused) and what it
    says of it (`said`), which are the counts and the sentences of
    [`spec/03`](../spec/03-chord-sheet.md), rule 52a. Nothing held these before.
  - a seventh song, `ends`: a text box whose words end with one line break, and one
    whose words end with three with a chord on the second
    ([`spec/03`](../spec/03-chord-sheet.md), rule 2a), with its rows, its stops and
    two walks. It was wanted (below) and waited for Simple Presenter to leave such
    lines off its sheet.
  - an eighth song, `lead`: a group whose first line is a line of chords alone, over
    a line of words, and a group of two lines of words; with its rows and stops, and
    one case of `copyPaste`: the whole first group pasted onto the second. Its first
    line is passed over, and the one change then made is not joined to what was done
    before the paste ([`spec/03`](../spec/03-chord-sheet.md), rule 52). It was wanted
    too, and waited for Simple Presenter to do as that rule says.

  **Neither app's tests have been run against the two new songs**: they read
  whatever songs the file has, so these are cases each has yet to meet.
- **`tools/make-vectors/sheet.js`** was changed for the sheet as it now is: working
  out where the slides go is a part of reading the song again there, so the functions
  that do it are put aside for ones that do nothing, as the window always was; a row
  is recorded without `made`, which is that drawing's bookkeeping; and the sheet is
  told nothing has been copied elsewhere on the computer. It still runs on the sheet
  as it was at `1311d80`, and gives what it gave.
- **`stand-ins-in-rtf.json`** and **`key-fit.json`**, by `tools/standins.py` and
  `tools/keyfit.py`: the same to the byte, as they must be, neither reading anything
  of Simple Presenter's. Simple Presenter's own port of the first
  (`reference/simple-presenter/src/rtfstandins.cpp`) gives the bytes of all 383
  cases of `inPlace`, and has no result for the same 68. It has nothing for
  `fromAnOpening`: it still writes a blank slide's first chords with its own writer.
- **`files/expected/as-read-by-simple-presenter.json`**: the eleven files that were
  in it are read as they were, and Chords With No Words is in it now.
- **Not made again**: the clean presentations (`files/made-by-simple-presenter`),
  whose ids and times would come out other bytes that say the same, and what
  `tools/make-fixtures.py` makes of them, nothing it reads having changed.

**One thing was found that the vectors do not hold and the pages do not settle**: what
becomes of the other ranges of a text box when a line of its stand-ins is made longer
or shorter. [`spec/01-file-format.md`](../spec/01-file-format.md) has it under "Where
Simple Presenter differs", with what real songs have. It wants the maintainer's word,
and then a vector.

**Still wanted**

- **RTF as ProPresenter on Windows writes it**, with a character above ASCII in it,
  for `rtf-text.json` and `stand-ins-in-rtf.json`. Every sample of both is Apple's
  way of writing, or made up. If Windows writes a character to stand in after each
  numbered one, a line of chords alone written there cannot yet be given a chord
  more or fewer ([`spec/01-file-format.md`](../spec/01-file-format.md), "Chords with
  no words", step 7).
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
  - (in `copyPaste`, a whole group whose first line is a line of chords alone,
    pasted onto lines of words, stood here, waiting for Simple Presenter to be put
    right: it is the song `lead` now.)
  - (in `rows`, a text box whose words end with one line break, and one whose words
    end with three with a chord on the second, stood here: it is the song `ends`
    now.)
- Rules that no case holds, each found by breaking the Windows app's port of the
  rules one rule at a time (2026-10-12: 162 breakages, of which a vector caught 129;
  it has cases of its own for these, `NotHeldByVectorsTests`, their answers read from
  the reference's code):
  - in `chord-names.json`, a letter and a number from outside ASCII after a note
    (`C` and a Greek alpha; `C` and a superscript two), which `couldBecome` lets
    through, and a letter from beyond the first 65,536 characters, which it does not;
  - in `notated.json`, a note six semitones from the note its letter has in the
    scale (`F######` in C, as numbers): written sharp, `######4`;
  - in `songs/`, for `parse-song.json`: an empty line inside a part that a
    `{comment: Verse 1}` named, with a line after it (only a gap); a line `Verse a`
    (words, and no heading: the letter wants a number before it); a file whose line
    ends are carriage returns alone; and a directive's value with a no-break space
    at its ends (taken off);
  - in `rtf-text.json`: a numbered character followed at once by an opening brace
    and a letter, with nothing said of how many to pass over (the brace ends the
    passing over, and the letter is words); a group for each of `pict`, `listtable`
    and `listoverridetable` with text in it (not words); and a backslash at the end
    of a line inside the font table (no line break in the words);
  - in `stand-ins-in-rtf.json`, `fromAnOpening`: a model whose first character
    follows a control word with no space, asked for a stand-in on a PLAIN space
    (refused: it does not read back). The apps never ask for one there;
  - in `key-fit.json`: a song of exactly three chords, all of one key (nothing
    suggested), and the same with a fourth; a song whose best key fits exactly
    three of its four chords; a chord's name with a line break inside it; `C#b5`
    four times (D flat, a note's sharps not being followed by flats); and a name
    with U+001C before it;
  - in `line-break-shift.json`, `songs`: one where moving puts exactly four more
    chords of sixteen on the starts of words (likely), one with four of seventeen
    and one with three of twelve (not); and a song whose text boxes have one line
    each (no chord is below a first line).

## What must not come in here

Anyone's real songs. Not the maintainer's, not a tester's, not one "just for a test":
they are other people's copyright, and Multitracks' files carry who licensed them. A
case that needs a real file to show it is described in words, and a made-up file is
built to show the same thing.
