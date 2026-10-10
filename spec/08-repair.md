# What the old editor left in files, and mending it

Versions 0.9.x of the Windows chord editor (`windows/` as it was first moved here, and
the releases up to v0.9.5) wrote some things into songs that are not right. People have
those songs in their libraries. This app is to know such a song when it sees one, say
so in plain words, and put right what can be put right.

**New**: none of this is in Simple Presenter. The reference for the checks is
`tools/make-fixtures.py` and `tools/keyfit.py`; the files to try them on are in
`testdata/files/as-the-old-windows-app-left-them`, and what should be found in each is
in `testdata/files/expected/repairs.json`.

## What it did

Read from its code (`MainWindow.xaml.cs`: `LoadPresentation` and `SaveButton_Click`)
and its issues, and, where it says "seen", found in a song it saved.

| | What it did | What that leaves | How it is known |
| --- | --- | --- | --- |
| A | Wrote every chord's range from the chord to the **next chord**, wherever that was, and the last chord's to the end of the words | Ranges that run over line breaks. ProPresenter then shows the chord again at the start of the next line: its issue 3, "duplicating chords on next line" | **Seen**: 30 of 89 chords in a song it saved; none in any song from Multitracks |
| B | Counted places along the words **with the line breaks left out**, reading and saving | A chord it only read and saved kept its number. A chord *put on by hand* in it, on the second line of a slide or below, is saved one short for each line break above: it sits a character or two left of where it was put | **Deduced** from the code. Not yet seen in a file |
| C | Filled its list of keys with "Ab", "F#" and looked the song's key up as "AFlat", "FSharp". So a flat or sharp key showed as nothing chosen, and saving with nothing chosen wrote the first key of the list. A song with no key kept whatever the song opened before it had shown | The key written as **A flat major** where it was another, or as the previous song's | Read from the code |
| D | Saved to a new file, `<the name inside the song> (Chords).pro`, beside the original, with the same name and id inside | Pairs "X.pro" and "X (Chords).pro". Chains, "X (Chords) (Chords).pro", once ProPresenter had renamed the inside of the first copy. Playlists still point at the original, which has no chords | **Seen**: such a chain |
| E | Worked on the **first** element of each slide only, whatever it was | Chords on a box that is not the words, in songs whose slides have more than one | Read from the code |
| F | Let anything be typed between its brackets | Names that are not chords. People typed `C /// \| Am ///` to show bars (its issue 2) | **Seen** in the issue |
| G | Took any run of text starting with `[` for a chord when saving, and its last character for the `]` | Perhaps: names with a `]` or `[` in them where two chords stood side by side (`C][G`), a name short of its last character, a lyric line that began with a bracket saved as a "chord" | **Deduced**. Not seen |
| H | Deleted the copy and then wrote it afresh | A song cut short, if it stopped in between | Read from the code |

One test settles B, and only the maintainer can run it: in the old editor, put a chord
on the first word of a slide's second line, save, and look at it in ProPresenter. If
it is over the last letter of the line above, B is real.

## Check 0: chords that are not on a character

A chord whose place is past the end of its box's words, or on a line break.

- **Found** by reading: `tidy` ([02-chord-rules.md](02-chord-rules.md)) would move it.
- **Shown**: on the sheet it is drawn where `tidy` puts it (the last character; the
  character before the break), so that it can be seen and dealt with.
- **Mended** whenever the box's chords are next written, since everything written goes
  through `tidy`. It is not worth a mark of its own.

## Check 1: ranges that run over a line break (A)

- **Found**: any chord of a box whose range has a line break strictly inside it
  (`start` < the break's place < `end`). One is enough: no other program is known to
  write them.
- **Mended** by writing the box's chords again exactly where they are. Everything
  written has the right ranges ([01-file-format.md](01-file-format.md)), so the mending
  is nothing but a save, and any change to a box's chords mends that box by the way.
  It moves no chord and changes no name.
- **Safe** to do without asking about each: what ProPresenter shows afterwards is what
  was meant. It is still a change to a file, so it is offered, not done on opening:
  "12 chords in this song are written so that ProPresenter shows them twice. **Mend**".
  And for a whole library at once, from the list ([The whole library](#the-whole-library)).

## Check 2: chords put on by hand in the old editor (B)

- **What to look for**: on every line of a box but its first, chords standing a little
  to the left of the starts of words, by one character for each line break above.
- **The rule** (`line_break_shift` in `tools/make-fixtures.py`): for each chord that is
  below the first line of its box, see whether it stands at the start of a word as it
  is, and whether it would if moved to where the old editor's count puts it
  (`unshifted`: its place taken as counted without line breaks, turned back into a
  place among the words as they are). If moving them puts at least four more chords,
  and at least a quarter more of all those chords, on the starts of words, the song
  *looks* as if it was chorded there.
- It can never be sure. Chords fall in the middle of words on purpose, and a song with
  few chords below its first lines shows nothing either way.
- **So it is only ever offered, with the song shown both ways** on the sheet, as it is
  and as it would be, for the person to judge with their own eyes: "The chords of this
  song look as if they were put on in the old Windows chord editor, which counted them
  a little to the left on every line after the first. **Show Me**". Taking it moves
  every chord below a first line to `unshifted` of its place and writes the box; it is
  one step of undo.
- Until B is seen in a real file (see above), build the check and the fixture test and
  **leave the offer switched off**, behind a setting. A wrong offer to move every chord
  of a good song is worse than none.

Vectors: `line-break-shift.json`.

## Check 3: a key that does not fit the chords (C)

- **The rule** (`tools/keyfit.py`, which explains itself): a key's score is the share
  of the song's chords that are among that key's own seven. A stated key is called
  doubtful only if the song has four chords or more, some key fits three quarters of
  them, and the stated key's score is more than a quarter below the best.
- It is not only for the old editor's damage. A song with **no key** gets the same
  sum, and the sheet's line "The song names no key: pick the one its chords are in"
  can then say which it probably is.
- **Offered, never done**: "This song says it is in A flat, but its chords look like
  G. **Set Key to G**". A song may well be in a key its chords stray from; the person
  knows and the sum does not.
- A flat major is what the old editor wrote by accident, so a doubtful key that is A
  flat major, in a song that also fails check 1, is nearly certainly its doing. Say
  so in that case; otherwise just say the key looks unlikely.

Vectors: `key-fit.json`.

## The "(Chords)" copies (D)

- **Found** by name: a file "X (Chords).pro" in a library that also has "X.pro"
  (and "X (Chords) (Chords).pro" beside "X (Chords).pro").
- They are listed, with a mark ([06-library.md](06-library.md)), and can be opened and
  changed like any song.
- **Folding one back** puts the copy's chords and key into the original, so that the
  song playlists point at is the one with the chords. It is offered when the two have
  the same words, box for box, in the same order. Then: both files are copied
  ([07-backups.md](07-backups.md)); each box of the original is given the chords of
  the same box of the copy (mended by check 1 on the way), and `music` is set to the
  copy's; and the copy is asked about: leave it, or move it out of the library into
  the app's copies. It is never simply deleted.
- If the words differ, nothing is offered but to open either. If the original has
  chords of its own (it was a Multitracks song), the offer says so, since folding
  replaces them.
- This is the largest thing the app does to a library in one go. Build it last, show
  what it is about to do before it does it, and try it on copies first.

## The rest

- **E**, chords on the wrong box: nothing to detect. The app shows every box of a
  slide that has words, so they are at least in sight and can be dragged to where
  they belong.
- **F**, names that are not chords: kept as they are, everywhere
  ([03-chord-sheet.md](03-chord-sheet.md), Chords that are not chords).
- **G**, a name with a square bracket in it: cannot be written as ChordPro, so the
  ChordPro editor treats the song as it does one whose words have brackets
  ([04-chordpro-editor.md](04-chordpro-editor.md), rule 20). On the sheet it shows as
  it is and can be typed over.
- **H**, a song cut short: it will not parse. It is listed and says it cannot be read.
  If the app has copies of it, Earlier Versions is offered.

## The whole library

- The marks in the list say which songs want mending ([06-library.md](06-library.md)),
  and the list can be narrowed to them.
- **Mend All** over that narrowed list does check 1 for every song in it, and only
  check 1: the one mending that needs nobody's eyes. It says first how many songs and
  chords, copies every file it will change, and says afterwards what it did, by name.
- Checks 2 and 3, and folding, are a song at a time.

## Proofs to write

- Each file of `testdata/files`: the checks find what `expected/repairs.json` says,
  in the damaged ones and in the clean ones, where it is nothing.
- Each damaged file, mended as that file says (`putRight`), has exactly the chord
  entries of the clean file it was made from.
- Mending changes nothing else in the file.
