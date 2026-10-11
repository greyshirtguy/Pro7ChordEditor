# A song's file

Where a song's words, its chords and its key are in a `.pro` file, how a place in the
words is counted, and what must be left alone. Everything here is **known** (seen in
files ProPresenter and Multitracks wrote, and built on in Simple Presenter) unless it
says otherwise.

## The file

A presentation is one file ending `.pro`. It is a Protocol Buffers message,
`rv.data.Presentation`. The descriptions of that message and everything in it are in
`proto/autogen-proto` (`proto/` is a submodule of this repository:
`git submodule update --init`), and each app reads and writes the file with code
generated from those descriptions (swift-protobuf on the Mac, Google.Protobuf on
Windows).

**Use `proto/autogen-proto` and no other folder of that submodule.** It is taken out of
ProPresenter's own program, by a job that looks each day for a new version of
ProPresenter and takes the descriptions again when there is one. So it is what
ProPresenter itself reads and writes with, and `version.txt` in it says which version:
21.4 at the commit this repository holds. Simple Presenter is built from it.

The folder `proto` beside it is an older set, worked out by hand and not changed since
2023. No field of a presentation has another name or number there, but twenty-four
fields have no name in it at all, and in the one real library looked at, three songs
in four had some of those. The folders with a version in their names are sets taken
at those versions, each of them before 21.4.

To look inside a file as text:

```
protoc -I proto/autogen-proto --decode rv.data.Presentation proto/autogen-proto/presentation.proto < "Some Song.pro" | less
tools/look.py --chords "Some Song.pro"
```

(`protoc` warns that an import is unused. That is nothing.)

**What to generate code from**: `presentation.proto` and every file it imports,
directly or through another. At 21.4 that is thirty-two of ProPresenter's files, of
about a hundred and fifty; the rest describe its other documents, its network API and
what it reports of its own use, and are not wanted. One of the thirty-two,
`customOptions.proto`, imports `google/protobuf/descriptor.proto`, which is Google's
own: the folder has a copy for `protoc` to find, and no code is generated from that
one, because each language's protobuf library has it already.

The files carry ProPresenter's own options for naming what is generated. In Swift the
types are named `RVData_...` (`swift_prefix`), and the C# is in the namespace
`Pro.SerializationInterop.RVProtoData` (`csharp_namespace`). In the C++ of the
reference source the same things are `rv::data::...`.

**When ProPresenter has a new version** the folder follows it by itself, in the
submodule's own repository. This repository holds the submodule at one commit, so
nothing changes here until someone moves it on. Doing that is a change like any other:
generate again, run every test, and say in the commit which version it is now. Nothing
forces it, because of what comes next.

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
of `cues`. A cue is left out if its `isenabled` is false, or if none of its actions is
a presentation's slide that is itself switched on (the action has an `isenabled` of
its own; the first such action is the cue's slide). A cue named twice is taken the
first time, and an id that is no cue's is passed over. Number the slides from 1 in that
order: it is the order ProPresenter shows them in. (A presentation also has
`arrangements`, which play the groups in another order. They are not gone by here: the
sheet is the song as it is laid down, each slide once.)

**A box's id is its own only on its own slide.** An element's `uuid` says which box of
a slide it is, and no more: ProPresenter gives a slide's box the id of the box it was
made from, so that in a song whose slides were made by copying one another, or from
one template, many boxes have one id. (**Known**: in 100 of 425 real songs looked at,
1,480 ids in all were some other box's too.) So a box is always named by **its slide
and its id**, and never looked for by its id through the whole file.

**Which boxes hold a song's words.** A slide can have several elements. The ones the
chord editors show are those that

- have `element.text` with `rtf_data`, and
- show words of their own: none of their `data_links` is of a kind that puts something
  else in the box (a clock, a timer, another element's or another slide's words, and
  every other kind the descriptions name). The one kind that leaves a box its own
  words is a `visibility_link`, which only says when the box is to be seen. (**Known**
  to matter: in the test file ProPresenter wrote, every text box has one.) And
- have words: the plain text is not empty or blank. But a box whose text is blank is
  still shown if it has chords, or has a stand-in character in it (U+200B or U+2001):
  it is a [line of chords alone](#chords-with-no-words).

Nearly every song slide has exactly one such box. Where a slide has two, both are
shown, one after the other, in the order of `elements`.

**A blank slide.** A slide none of whose boxes is shown by those rules is a blank
slide if it has a text box (an element with `element.text`) that shows words of its
own, by the second of those rules.
The first such box, in the order of `elements`, is shown too: as a
[line of chords alone](#chords-with-no-words) that has no chords yet and no stand-ins,
so that chords can be put on it ([chords onto a blank slide](#chords-onto-a-blank-slide)).

- A blank box on a slide that has another box with words is not shown: that slide has
  somewhere to put a chord already.
- A slide with **no such text box at all** (nothing on it, or only a picture or a
  shape, or only boxes that show something else's words) is a blank slide too. It is
  handed over as a blank box with **no id** (an empty `element`) and no words, and is
  given a text box only when a chord is put there
  ([a slide with no text box](#a-slide-with-no-text-box)). So every slide of a song
  is handed over, as one entry at least.
- A blank slide's box is handed over with no words, whatever stray space its text may
  hold.

This is **new**, decided by the maintainer on 2026-10-10: an intro or an instrumental
that someone made as an empty slide had nowhere to take its chords. In the one library
looked at, 27 slides of 383 had a blank text box, many of them in groups named Ending,
Intro and Bridge, and one slide had nothing on it at all. Simple Presenter shows blank
slides since the change that followed (`reference/`), and `testdata/files` has a song
made for them, Blank Slides, whose last slide has no text box:
`as-read-by-simple-presenter.json` has it with an empty `element`.

(That a slide with no text box is one of them was decided a day later, 2026-10-11.
Simple Presenter, being an editor of slides, did it first; until then these pages had
the apps leave such a slide out.)

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
- `range.end` is where its stretch of the words ends. **Written** it is:
  - on a [line of chords alone](#chords-with-no-words), one more than `start`: the
    chord's own stand-in and nothing else, so that three chords on such a line are
    0 to 1, 2 to 3 and 4 to 5, with the em quads between them in no range;
  - on any other line, the start of the next chord or the end of the chord's own
    line, whichever comes first, and at least one more than `start`.

  **Known**: that is what the songs from Multitracks have. In the one library looked
  at, all 290 chords on lines of chords alone had a range one long, and 949 of the
  1,009 on lines of words had exactly the other; the 60 left were in two songs the old
  Windows editor had saved ([08-repair.md](08-repair.md), check 1). So a box written
  by this rule with its chords unchanged is, in its chord entries, what it was.

  **Read** it is not believed: files differ, and some are wrong, so only `start` says
  where a chord is. A range that runs past the end of its line makes ProPresenter draw
  the chord again on the next line, which is why what is written matters.

  (The first half of the rule was decided by the maintainer on 2026-10-10, on that
  evidence. Up to its version 0.82 Simple Presenter ran the range of a chord on a line
  of chords alone on to the next chord, over the em quad, as on a line of words. It
  writes it one long now, as the Mac app has done since the day of the decision, and
  `ranges.json` and the test files were made again from it. Whether ProPresenter draws
  the two any differently is **not known**.)
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

To the letter:

- **Only a line of chords alone is ever made stand-ins.** A request for a line that
  has words, or for an empty line, is refused, whatever asks
  ([05-song-and-saving.md](05-song-and-saving.md)). (Simple Presenter would make
  stand-ins of any line it was asked for, its words with it. Nothing in it asks for
  another, and here it is not left to that: the words are not the app's to change.)
- The text is changed whenever the line is not already exactly that run of stand-ins:
  when the number of chords is another, and also when the line was never a clean run
  (three plain spaces with one chord on them). So after any such request the line has
  one stand-in for each of its chords and nothing else.
- **No chords is no stand-ins**, and the line is then empty. From then on it is an
  empty line and no longer a line of chords alone, as in Simple Presenter: the
  editors put nothing on it. If the box has nothing else in it, the slide is then a
  [blank slide](#from-the-presentation-down-to-a-text-box), and takes chords again as
  one.

Simple Presenter does this by reading the RTF into its own model and writing the whole
RTF again in its own words. **Do not copy that.** It suits an app that edits text
anyway; here it would rewrite the RTF of someone's library for the sake of one line.
Instead change the RTF **in place**:

1. Read the RTF, keeping for every unit of its plain text the bytes it was written
   with: a plain character's one byte; a control word's backslash, letters, number
   and the space that ends it, if it has one; the four of `\'hh`; the bytes of a
   character written straight in as UTF-8. (A `\uc0` before a `\u` is a control word
   of its own and no part of the character.)
2. The line is the units between two line breaks. If it has none there is nothing to
   say where it is, and there is no result.
3. What the line has already of what it is to become, counted from its start, is
   kept as it is written.
4. The bytes of every unit of the line after that are taken out: the units' own, and
   **none of the bytes that lie between them**. Those are control words, and they say
   how what follows is set. (In a song from Multitracks each stand-in is in the
   song's font and each em quad in another, so a font is named between every two.)
   What is left behind is a run of control words with nothing between them, which
   harms nothing and leaves whatever follows the line set exactly as it was.
5. What is still to come is put where the first unit taken out began; or, if none
   was taken out, just after the line's last unit. A plain space is written as a
   space; U+200B as `\uc0\u8203 ` and U+2001 as `\uc0\u8193 ` (a space ends each control word and is not
   text), which is how Apple's text system writes any character above ASCII.
6. If what is put in begins with a plain space, and a control word that has no space
   of its own ends exactly there, a space is put in first to end that word, or the
   stand-in would be taken for its end and be no text.
7. Then the RTF is read back. Its plain text must be the old text with that line
   changed and nothing else. If it is not, there is no result.

Where there is no result, the change is refused and nothing is saved: "A chord could
not be added to this line, or taken from it: its text box is written in a way that
cannot be changed here safely. Nothing was changed."

Step 7 is what keeps a file whole when its RTF is of a kind these steps were not
written for. One such kind is known of: an RTF that has a character follow each `\u`
to stand in for it, for readers that do not know `\u` (`\uc1`, which is RTF's own
way unless told otherwise, and which Apple's text system always tells otherwise).
Taking a `\u` out would leave its stand-in to be read as a word, and a `\uc0` put in
would do the same to those after it; so such a line is refused. What ProPresenter on
Windows writes is **not known** ([Not known](#not-known)): if it is that, these steps
are to be widened when a file of it has been seen, in both apps and in the vectors.

`testdata/vectors/stand-ins-in-rtf.json` has RTF in every shape real songs were found
to have, and in some they were not, each with the bytes it becomes for a number of
stand-ins, or that it cannot be done. `tools/standins.py` made it and is the reference
for this rule, which is new: both apps are right when they give those bytes.

**Known**, of real songs (425 on the maintainer's Mac, 2026-10-11, on copies): every
one of their 116 lines of chords alone could be given a chord more, a chord fewer, two
more and none (423 changes), each leaving every byte of the RTF outside the line as it
was and nothing else in the file changed, and each undone to the byte. **A guess**,
until it has been seen: that ProPresenter shows the chords of a line so changed as it
shows any other's. The maintainer is asked to look ([11-plan.md](11-plan.md), M7).

### Chords onto a blank slide

**New**, with [a blank slide](#from-the-presentation-down-to-a-text-box) being shown at all.
Putting the first chords on one gives its box words: one stand-in for each chord,
U+200B with U+2001 between, exactly as a line of chords alone has. The app does it
without asking. Nothing that shows on the slide is changed: it shows no words before
and none after.

There is nothing in such a box's RTF to change in place. As Apple's text system writes
a text box with no text, the RTF is an empty document: its font table is empty, and it
names no font, no size and no colour, so stand-ins put into it would have none. (29 of
the 30 blank boxes looked at were so. The other was written by ProPresenter on
Windows, and did name a font and a format.) So the RTF is made, from the song itself:

1. Take the RTF of the nearest box of the song that is shown and is not blank: on a
   slide before this one if there is one (the nearest first), else on one after; and
   of a slide's boxes, the first such. A box that is shown for its chords and has not
   one character in it is passed over: it has no words to say where words begin.
2. Keep its **opening**: its bytes up to the first byte of its first character, found
   as step 1 of [the rule above](#chords-with-no-words) finds it. That is its fonts,
   its colours, and how its first words are set. After it put the stand-ins, written
   as step 5 writes them (they begin with a backslash, so step 6 has nothing to do
   here), and then as many closing curly brackets as there are groups open at that
   place: one, in every RTF Apple's text system writes. [09-chordpro-files.md](09-chordpro-files.md) gives a new song's text box
   its words the same way.
3. Read it back and check that the plain text is the stand-ins and nothing else,
   before anything is saved. If it is not, the change is refused with the sentence
   of the rule above.
4. Only the box's `rtf_data` and its chord entries change. Where the box is on the
   slide, its size and its fill, and `text.attributes`, stay as they were.

(Where the model's own words begin with a stand-in, as an intro from Multitracks
does, its opening ends with the `\uc0` that stands before that stand-in, and what is
put after it begins with another. That says one thing twice and harms nothing; a
rule that left one out would be one more thing for two apps to get the same.)

If the song has no other box to take an opening from, the change is refused: "This
song has no text box with words in it to model one on, so chords cannot be put on a
slide that has none. Nothing was changed." Undo puts the bytes back as they were.

`stand-ins-in-rtf.json` has openings too (`fromAnOpening`). **Known**, of the same 425
songs: they have 1,552 blank slides with a text box (1,431 of them the empty document
above, 110 holding stray space in a font, 11 something else) and 34 with none. 853
were given chords, each as meant with nothing else in the file changed, and each left
the file as it was to the byte when undone, and when its chords were taken off again.
The other 733 are in 63 presentations that have not one box with words, which are not
songs (a run of pictures, a countdown), and are refused in the sentence above.

#### A slide with no text box

Decided by the maintainer on 2026-10-11, as Simple Presenter does it. The first chords
put on a blank slide that has no text box of its own (its entry has no id) give the
slide one. The app does it without asking, and only then: nothing is made for a slide
until a chord is put on it.

1. The box is a **copy of the song's own**: the element of the nearest box that is
   shown and is not blank, the same one whose RTF gives the opening above (on a slide
   before this one if there is one, else on one after). So it is placed, sized and set
   as that one is, which is what a song from Multitracks has under an intro's chords.
   It is a copy of something ProPresenter made, and not a text box of this app's
   devising.
2. Of the copy: its id is a new one (a UUID, written as the file's others are: in
   capitals, as ProPresenter on the Mac writes them, unless the id of the box it is a
   copy of has a small letter in it, and then in small letters); its
   name is the copied name, or that with ` 2`, ` 3` ... after it until no other
   element of the slide has it; its `build_in`, `build_out`, `child_builds` and
   `data_links` are cleared, being what tied the other box to things on its own slide;
   its chord entries are taken off; and its RTF is made as steps 2 and 3 above make
   it, the stand-ins after the copied opening.
3. It is added after the slide's other elements, so that it is drawn over them.
4. If the song has no box to copy, the change is refused with the sentence above. (The
   apps do not devise a text box. Simple Presenter, which makes them anyway, does.)
5. If the slide is found to have a box that would be shown by now, the change is
   refused: "That slide is no longer blank. Nothing was changed."

#### A change of mind

Decided by the maintainer on 2026-10-11 for the apps, as asked of Simple Presenter the
day before: "only add when needed - when user adds a chord to a blank slide - and
leaves the chord(s) there! (if a textbox was automatically added - but the user
changes mind and removes the chords - during that edit session - dont leave the added
textbox behind - no need to track that particular state after a save".

While a song is open, the app remembers each blank slide's box as the file had it
before its first chord was put on here (its `text`, whole; or that the box was added).
Taking the last chord off such a slide again, while the song is still open, puts that
back: the text as it was, or the added box taken off the slide. The file is then
**byte for byte what it was** before the first chord.

To the letter:

- What is remembered is kept by slide and box, a box's id being its own only on its
  own slide.
- "Its last chord" is when the box is left with no chord and no words at all.
- For a box that was added, remember the slide's cue as the file had it, and put
  that back whole if the cue is otherwise what it was just after the box was added
  (which it always is: these apps change nothing else on a slide). The file is then
  what it was down to which of the slide's empty parts were written and which left
  out, which taking the box off the slide again cannot be sure of.
- It holds through undo and redo, which put a cue back as it was at one end of a
  change or the other.
- It is forgotten when the song is closed, and when an earlier version of the file
  is put back ([07-backups.md](07-backups.md)): that is another file, and what was
  remembered was of the one before.

Once the song is closed the app remembers nothing. Chords taken off another day leave
the box with the opening it was given and no words: blank to look at as it was, and
still shown as a blank slide. A text box that was added stays, empty. Either can be
given chords again, as any blank slide can.

**A guess**, until it has been seen in ProPresenter: that a box so written shows its
chords on a stage screen as an intro from Multitracks does, its text being the same
shape; that `text.attributes` may be left as they were; that a copied box's other
custom attributes, whose ranges were over words that are no longer in it, do no harm
left where they are; and that a text box does no harm on a slide that is there to
start something else (of the 30 slides in the songs looked at that have nothing on
them at all, 16 start a video, a macro or the like when they are shown). It was built at M7, on the Mac,
and **has not yet been looked at in ProPresenter**: that is asked of the maintainer
before anyone is given it.

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
