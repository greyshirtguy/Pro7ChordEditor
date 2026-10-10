# ChordPro files

A new song made from a ChordPro file, and a ChordPro file written from a song.

ChordPro is the plain-text way of writing a song with its chords: the words, with each
chord in square brackets where it is played, and lines in curly brackets that say what
the song is and where its parts begin. CCLI's SongSelect gives songs out in it, which
is why people have asked for this more than for anything else (the old editor's issues
4 and 11).

Reading a file is **known**: it is Simple Presenter's, with vectors. Making a
presentation of it is the part to be careful with, and comes last in the plan.

## Reading one

`parseSong(text)`, in `reference/simple-presenter/src/chords.cpp`. Vectors:
`parse-song.json`, made from the files in `testdata/songs`.

1. The file is text: UTF-8, with or without a mark at its start; if it is not valid
   UTF-8 it is read as Latin-1. Line ends of any kind. Anything over two megabytes is
   not a song and is refused.
2. A line starting `#` is a remark and is left out.
3. A line that is `{name}` or `{name: value}` is a **directive**:

   | Directive | What is done with it |
   | --- | --- |
   | `title`, `t` | The song's title |
   | `artist`, `subtitle`, `st` | Its artist (the first one given) |
   | `key` | Its key, tidied: `f#m` is `F#m` |
   | `copyright`, `ccli` | Kept |
   | `start_of_verse`, `sov` | A part begins, called "Verse 1", "Verse 2" ... as they come, or by the label if one is given (`{start_of_verse: Verse 3}`) |
   | `start_of_chorus`, `soc` | A part called "Chorus", then "Chorus 2", "Chorus 3"; or its label |
   | `start_of_bridge`, `sob` | Likewise "Bridge" |
   | any other `start_of_...`, `sot`, `sog` | Tablature, a grid, something newer: everything up to its end is left out |
   | `end_of_...`, `eov`, `eoc` and the like | The part ends |
   | `comment`, `c`, `ci`, `comment_italic` | If what it says is the name of a part (below), a part begins with that name. Otherwise nothing |
   | anything else | Nothing |

4. A line that is only the **name of a part** begins one, with or without a colon
   after it or square brackets round it: `Verse 1`, `CHORUS:`, `[Bridge]`,
   `Pre-Chorus 2`. The names known are verse, chorus, pre-chorus, bridge, intro,
   outro, ending, tag, interlude, instrumental, refrain, turnaround, vamp, coda, hook
   and post-chorus, each perhaps with a number and a letter. Most files found in the
   wild mark their parts this way and not with directives. The name is tidied to
   "Verse 1".
5. An **empty line** ends the part, so that a file with no marks at all comes in as its
   verses, and one whose parts are named by bare lines (rule 4) comes in as those.
   The one exception: inside a part that a *directive* began, an empty line is only a
   gap.
6. Every other line is a line of the song, read with `fromChordPro`
   ([02-chord-rules.md](02-chord-rules.md)): its words, and its chords with their
   places along the words. Spaces at its ends are taken off and the chords move with
   the words. A line of chords alone becomes stand-ins.
7. A part with no lines is not kept. A song with no parts has no words in it and is
   refused.

## Before it is brought in

8. Choosing a file shows what was found, before anything is made: the title, the
   artist, the key, each part with how many lines it has, and how many chords in all.
   With it: the name the song will have, the library it goes into (the one that is
   open), and **how many lines to a slide** (two, unless changed).
9. `slidesOf(part, n)` deals a part's lines into slides of n, the last perhaps
   shorter.

## Making the presentation

Simple Presenter builds one from nothing
(`reference/simple-presenter/src/songimport.cpp`): a group for each part, a slide for
each run of lines with one text box named "Lyrics" over the whole slide, white words
centred at 84 points, the chords over them, the key, and the title and artist in the
CCLI block. Its own comment says the rest: *that ProPresenter opens a presentation
made this way is a guess, and untested.*

**Do not do it that way here.** Building a presentation from nothing means writing
every field ProPresenter expects of a slide, and RTF for the words, from two more
languages, all on a guess. Instead:

10. **A new song is made from a song that is known to be good.** The person picks one
    of their own ("Make it look like…"), from the open library. Failing that, the app
    carries one small presentation of its own, made in ProPresenter by the maintainer
    and saved from there: a single slide with one text box of words.
11. The new song is that one, copied, with a new id and name, no groups and no slides;
    then, for each slide wanted, a copy of the model's first slide with new ids, whose
    text box is given the slide's words and chords. Everything about how the slide
    looks comes across with the copy.
12. Giving a text box new words means writing its RTF. Do it as
    [01-file-format.md](01-file-format.md) says for a line of chords alone: keep the
    RTF's own opening (its fonts, colours and format, up to where the words begin) and
    put the new words after it, lines parted by a backslash and a line feed, every
    character above ASCII as `\uc0\uN `, and `\`, `{` and `}` with a backslash before
    them. Then read it back and see that the plain text is the words.
13. A group is made for each part, by name, with no colour of its own (ProPresenter
    colours a group by its name), holding its slides' ids in order.
14. `music` is set to the song's key, both parts, if it has one. The title, artist and
    copyright go where ProPresenter keeps a song's: `ccli.song_title`,
    `ccli.artist_credits`, `ccli.publisher`, and `ccli.song_number` if the file's
    `ccli` is a number.
15. `multi_tracks_licensing` is never set, and is taken out if the model song had one:
    a song typed or brought in by hand is not Multitracks'.
16. The file is written into the library as "<name>.pro", with a number after the name
    if there is one already. A slash in the name becomes a hyphen.

Before this is offered to anyone, a song made this way must be **opened in
ProPresenter** on both systems, shown, and its chords seen on a stage screen. That is
the maintainer's to do, and the plan has it as its own step.

## Writing one

**New**, and small. A song as a ChordPro file:

17. `{title: ...}` from the song's name, `{key: ...}` if it has one.
18. For each group, an empty line and `{comment: <its name>}`; for each of its slides'
    text boxes, each line as `toChordPro` writes it, with an empty line between one
    slide and the next. This is the text the ChordPro editor shows
    ([04-chordpro-editor.md](04-chordpro-editor.md), rule 2) with a heading on it.
19. Saved where the person says, as UTF-8, ending `.cho`. It changes nothing in the
    library.

Read back by rule 1 to 7, such a file gives the same parts, lines and chords. That is
its test.
