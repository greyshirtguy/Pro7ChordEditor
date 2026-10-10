# A song's file

Where a song's words, its chords and its key are in a `.pro` file, how a place in the
words is counted, and what must be left alone. Everything here is **known** (seen in
files ProPresenter and Multitracks wrote, and built on in Simple Presenter) unless it
says otherwise.

## The file

A presentation is one file ending `.pro`. It is a Protocol Buffers message,
`rv.data.Presentation`. The descriptions of that message and everything in it are in
`proto/proto` (a submodule of this repository: `git submodule update --init`), and each
app reads and writes the file with code generated from those descriptions
(swift-protobuf on the Mac, Google.Protobuf on Windows). Use the `proto/proto` folder
and no other in that submodule: it is the set Simple Presenter is built on.

To look inside one as text:

```
protoc -I proto/proto --decode rv.data.Presentation proto/proto/presentation.proto < "Some Song.pro" | less
tools/look.py --chords "Some Song.pro"
```

Both libraries keep what they do not understand: a field that is not in the
descriptions is carried through a read and a write unchanged. That is what makes
[writing](#writing) safe, and it must not be switched off.

## From the presentation down to a text box

```
Presentation
  name, uuid                         what it is called, and its id
  music                              its key: see below
  multi_tracks_licensing             only in a song that came from Multitracks
  cue_groups[]                       the groups: Verse 1, Chorus ...
    group { uuid, name, color }
    cue_identifiers[]                  which cues are in it, in order
  cues[]                             every slide, in no order that matters
    uuid, isenabled
    actions[]                          one of them is the slide (action.slide is set)
      slide.presentation.base_slide
        elements[]                       the boxes on the slide
          data_links[]                     if it has any, its words are not its own
          element                          (rv.data.Graphics.Element)
            uuid, name
            text                             (rv.data.Graphics.Text) if it has words
              rtf_data                         the words, as RTF
              attributes.custom_attributes[]   the chords are among these
```

**The order of the slides** is the order of the groups, and inside each group the
order of its `cue_identifiers`; after those, any cue that no group names, in the order
of `cues`. A cue is left out if `isenabled` is false or if none of its actions is a
slide, and a cue named twice is taken the first time. Number the slides from 1 in that
order: it is the order ProPresenter shows them in. (A presentation also has
`arrangements`, which play the groups in another order. They are not gone by here: the
sheet is the song as it is laid down, each slide once.)

**Which boxes hold a song's words.** A slide can have several elements. The ones the
chord editors show are those that

- have `element.text` with `rtf_data`, and
- have no `data_links` (a box with a link shows a clock, a timer or another slide's
  words, not words of its own), and
- have words: the plain text is not empty or blank. But a box whose text is blank is
  still shown if it has chords, or has a stand-in character in it (U+200B or U+2001):
  it is a [line of chords alone](#chords-with-no-words).

Nearly every song slide has exactly one such box. Where a slide has two, both are
shown, one after the other, in the order of `elements`.

`testdata/files/expected/as-read-by-simple-presenter.json` has every test file read
this way: reading a file is right when it gives that.

## The words, and counting along them

The words of a box are RTF. In every file to hand it is the dialect Apple's text system
writes, ProPresenter being a Mac program at heart; what ProPresenter on Windows writes
has not been looked at yet (see [Not known](#not-known)), and the rules below take in
the other usual spellings for that reason. A chord's place is a number counted along
the **plain text** of that RTF, so both apps must get the same plain text from the same
RTF, character for character. The rules:

| In the RTF | In the plain text |
| --- | --- |
| A backslash at the end of a line (`\` then a line feed or carriage return) | A paragraph break: one line break. This is how ProPresenter writes the end of every line |
| `\par` | The same |
| `\line` | A line break inside a paragraph: also one line break |
| A bare line feed or carriage return | Nothing |
| `\'hh` | The character with that number in Windows code page 1252 (`\'92` is a curly apostrophe, U+2019) |
| `\uN` | The UTF-16 unit N (N + 65536 if N is negative), after which as many characters are passed over as the last `\ucN` said: one, unless told otherwise. ProPresenter writes `\uc0` before each, so none |
| Two `\u` for one character (an emoji) | Two units, which are **two places** |
| `\~`, `\_`, `\-` | U+00A0, U+2011, nothing |
| `\tab` | A tab |
| `\\`, `\{`, `\}` | The character itself |
| Bytes above 127 written straight in | UTF-8 if the run of them is valid UTF-8, else code page 1252 |
| `{\fonttbl ...}`, `{\colortbl ...}`, `{\*\anything ...}`, `\info`, `\stylesheet`, `\pict`, `\header`, `\footer`, `\listtable`, `\listoverridetable` | Nothing: what is in those groups is not words |
| Any other control word | Nothing: it is format |
| Text that ends with a paragraph break | Ends with an empty line |

So the plain text is the lines of the box joined by single line breaks, and a place is
an index into it **in UTF-16 units, from 0, with each line break counting one**.

`testdata/vectors/rtf-text.json` has RTF samples with the plain text each gives, unit
by unit. `reference/simple-presenter/src/rtf.cpp` is the reader those came from.

On the Mac, `NSAttributedString` reads this RTF natively and gives the same string;
prove it against the vectors before relying on it, and index it through `utf16`. On
Windows there is nothing native to trust (the old editor used WPF's reader and lost
the line breaks: [08-repair.md](08-repair.md)), so port the small reader.

The apps never write RTF, with one exception: [chords with no words](#chords-with-no-words).

## Chords

A chord is one entry of `text.attributes.custom_attributes`:

```
custom_attributes {
  range { start: 8  end: 17 }
  chord: "G7"
}
```

- The list holds other things too (capitals, a font's first size, a gradient). An
  entry is a chord if its `chord` field is the one that is set. The others are not to
  be touched or reordered.
- `chord` is the chord's name as it is to be read, in the song's **original key**
  whatever key it is shown in. It is free text. Most are chords; some are not ("N.C.",
  or `C /// | Am ///` typed by someone to show bars). Whatever it is, it is kept as it
  is unless the person changes it.
- `range.start` is the place the chord stands at: it is drawn over the character there.
  It can be anywhere, the middle of a word included.
- `range.end` is where its stretch of the words ends. **Written** it is always the
  start of the next chord or the end of the chord's own line, whichever comes first,
  and at least one more than `start`. **Read** it is not believed: files differ, and
  some are wrong ([08-repair.md](08-repair.md)), so only `start` says where a chord is.
  A range that runs past the end of its line makes ProPresenter draw the chord again
  on the next line, which is why what is written matters.
- A `start` or `end` of 0 is simply absent from the bytes. That is Protocol Buffers'
  way and needs no care.

**Reading** a box's chords: every chord entry, as (`start`, name), sorted by `start`
keeping the file's order among equals.

**Writing** a box's chords replaces all its chord entries and nothing else:

1. Tidy what is to be written (`tidy`, in [02-chord-rules.md](02-chord-rules.md)):
   in order, each on a character the words have, no two in one place.
2. Keep every entry that is not a chord, in its order.
3. After them, add one entry for each chord, in order of place, with its range as
   above (`ranges`).

`testdata/vectors/tidy.json` and `ranges.json` hold both rules.

## Chords with no words

A song has lines that are only chords: an intro, a turnaround. There are no words to
hang them on, so the file has invisible ones. As Multitracks writes it:

- the line's text is one **stand-in** character for each chord, with an em quad
  (U+2001) between them;
- the stand-in is a zero-width space (U+200B) in some songs and a plain space in
  others; a single chord on a slide of its own often hangs on one plain space;
- each chord's place is its stand-in: 0, 2, 4 ... along the line.

A line is such a line if it is not empty and has nothing in it but U+200B and white
space (`isPlaceholders`). Whether it is a line of chords or just a stray space in a box
is told by whether it has chords.

Changing the *names* of such a line's chords changes only the chord entries. Adding or
taking one away changes how many stand-ins the line needs, and so its text: this is the
one case where the app writes words. When it does:

- the line becomes one stand-in for each chord with U+2001 between, using the stand-in
  the line had (a plain space if it had spaces and no U+200B, else U+200B);
- the chords of the line go on places 0, 2, 4 ... of it, and the chords of the lines
  after it move by as many places as the line grew or shrank;
- every other line of the box, and all its format, stays as it was.

Simple Presenter does this by reading the RTF into its own model and writing the whole
RTF again in its own words. **Do not copy that.** It suits an app that edits text
anyway; here it would rewrite the RTF of someone's library for the sake of one line.
Instead change the RTF in place: find the bytes that are that line's text and put the
new stand-ins there, as `\uc0舃 ` for U+200B and `\uc0膓 ` for U+2001 (a space
ends each control word and is not text), leaving every other byte alone. Then read the
RTF back and check that the plain text is what was meant, before anything is saved. If
the line cannot be found cleanly in the RTF, refuse the change and say so.

This is the riskiest thing the app does to a file, and it comes last in the plan
([11-plan.md](11-plan.md), M7). Until then such lines are shown, and their chords can
be renamed, moved among themselves and copied, but not added to or taken from.

## The key

```
music {
  original { music_key: MUSIC_KEY_E        music_scale: MUSIC_SCALE_MAJOR }
  user     { music_key: MUSIC_KEY_C_SHARP  music_scale: MUSIC_SCALE_MAJOR }
}
```

- `original` is the key the chords in the file are written in. `user` is the key
  ProPresenter shows them in: it moves every chord from the one to the other as it
  draws. A song from Multitracks shown a tone lower has `user` a tone below `original`
  and its chords untouched.
- `music_key` is one of twenty-one names, numbered 0 to 20: every letter flat, plain
  and sharp, from A flat (0) to G sharp (20). `music_scale` is major (0) or minor (1).
  [02-chord-rules.md](02-chord-rules.md) has the table.
- A song with no `music` at all states no key. That is not the same as A flat major,
  though a `music` whose two parts are both empty reads as exactly that
  ([08-repair.md](08-repair.md) is about files where that happened by accident).
- The message has two more fields, `original_music_key` and `user_music_key`, and the
  presentation has a `music_key` of its own, all three of them text. No file to hand
  sets any of them. Leave them as found.

Setting the key writes `music.original`; [05-song-and-saving.md](05-song-and-saving.md)
says what happens to `user`.

## A song from Multitracks

It has `multi_tracks_licensing` (who licensed it, and until when). The app does not
look at it, never adds one, never removes one, and opens and changes the song like any
other. That the block does no harm to a song whose chords were changed here is **a
guess**: it has not been tried in ProPresenter.

## Writing

1. Read the whole file into the message, as it is on disk now.
2. Change only what the change is about: a box's chord entries, `music`, or the RTF of
   a line of chords alone.
3. Serialise the whole message and write it to a new file in the same folder, then put
   that in the place of the old one in one step (`FileManager.replaceItemAt` on the
   Mac, `File.Replace` on Windows), so that the song is never half written.
4. Read the new file back and check it says what was meant.

Before the first of these for a file, a copy of it as it was is kept
([07-backups.md](07-backups.md)). None of them happens while ProPresenter is running
([06-library.md](06-library.md)).

## Never touched

- The words, and their RTF (but for a line of chords alone).
- Every custom attribute that is not a chord, and the order of those.
- Every field of the file not named on this page: slides, groups, arrangements, media,
  actions, looks, `name`, `uuid`, the dates, the CCLI block, `chord_chart`.
- The file's name and place. A change is saved into the song's own file. (The old
  editor saved a copy called "... (Chords)" beside it; that is over, because playlists
  point at the original.)

## Not known

- What the RTF of a song made in ProPresenter on **Windows** looks like. The first job
  of the Windows session is to look (`tools/look.py`, and `protoc --decode`), run a
  few through the reader, and add samples to the vectors if it writes anything the
  table above does not cover.
- Whether ProPresenter needs chords to be listed in order of place. Every file to hand
  has them so, and they are written so.
- What ProPresenter does with a chord whose `start` is on a line break, or past the
  end of the words. `tidy` never writes one.
- Whether places are UTF-16 units for certain. Every song to hand agrees with it, but
  none has a character beyond U+FFFF before a chord. ProPresenter being a Mac program,
  whose strings count that way, it is taken as so.
