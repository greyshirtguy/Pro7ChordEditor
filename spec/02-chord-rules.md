# The chord rules

What a chord is, what a key is, how a chord moves to another key, how it is written in
ProPresenter's four notations, what is offered while one is typed, how a box's chords
are kept in order, and how chords are written as ChordPro text.

These are plain functions over strings and numbers. Nothing in them knows of a file or
a window. In each app they are a module by themselves, with tests that read
`testdata/vectors/*.json` and nothing else. **Build this first** (milestone M1): it can
be proved right by the vectors alone, and everything else stands on it.

The working code is `reference/simple-presenter/src/chords.h` and `chords.cpp`, about
nine hundred lines with their comments, and its own tests are in
`reference/simple-presenter/tests/tst_chords.cpp`. Port it function for function, with
the same names where your language allows. Each rule below names its function and its
vector file.

## A chord's name

A name is a **root** note, then a **quality** (whatever says what kind of chord it is),
then perhaps a slash and a **bass** note.

- A note is a letter A to G, of either case, then sharps (`#` or ♯, any number) or else
  flats (`b` or ♭, any number).
- The bass is what follows the *last* slash, if that is a note and nothing more.
  `C6/9` has a slash and no bass: its quality is `6/9`.
- Only the two notes are understood. The quality is carried as it is, which is why a
  chord nobody has heard of (`G1`, which a Multitracks song has) still moves to another
  key rightly.

| Function | What it gives | Vectors |
| --- | --- | --- |
| `parts(name)` | root, quality, bass, and whether it is valid: it starts with a note | `chord-names.json` |
| `tidied(typed)` | The name with its notes as capitals and `b`/`#` for flats and sharps, and spaces off its ends. What is between the notes is the writer's business: `f#M7/a` is `F#M7/A` | `chord-names.json` |
| `couldBecome(typed)` | Whether it could be the start of a chord being typed: empty, or a note followed only by letters, digits and `# / + - ( ) ° ø ^ . , ♯ ♭ Δ` | `chord-names.json` |
| `isChord(typed)` | `parts(typed).valid` and `couldBecome(typed)`. What the editors ask before putting a *newly typed* chord on | `chord-names.json` |

A chord that is in a file already is never held to `isChord`. `C /// | Am ///` is not
one, and is kept, shown, moved and copied as it is. (Simple Presenter's sheet will not
move such a chord. That is put right here: see "Chords that are not chords" in
[03-chord-sheet.md](03-chord-sheet.md).)

## Keys

The file has twenty-one names for a key's note, numbered, and major or minor:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ab | A | A# | Bb | B | B# | Cb | C | C# | Db | D | D# | Eb | E | E# | Fb | F | F# | Gb | G | G# |

Everywhere else a key is written as a musician writes it: `E`, `Bb`, `F#m`. A name
ending in `m` (and longer than one letter) is minor.

| Function | What it gives | Vectors |
| --- | --- | --- |
| `keyName(number, minor)` | `"Ab"` for 0 and so on, with `m` after it for minor; empty outside 0 to 20 | `keys.json` |
| `keyNumber(key)`, `keyIsMinor(key)` | The other way; -1 for what is no key | `keys.json` |
| `majorKeys()`, `minorKeys()` | What a picker offers, in the order of the notes from C: thirteen each (both F# and Gb; both D#m and Ebm) | `keys.json` |

A picker shows the twenty-six. If the song's key is one of the other sixteen (C# major,
say), it is put first in the list, so that the song's own key can always be seen and
chosen.

## Moving a chord to another key

`transposed(chord, fromKey, toKey)`. Vectors: `transposed.json`, which has every pair
of the forty-two keys for fourteen chords.

Each of the chord's two notes is moved by as many **letters** and as many **semitones**
as the keys' notes are apart, so that it is spelt as the new key spells it: C# minor in
E becomes Bb minor in Db, not A# minor. The steps:

1. The key's note is read from the start of its name; major or minor does not matter.
2. `steps` = how many letters `to` is above `from` (0 to 6); `rise` = how many
   semitones (0 to 11).
3. The new letter is the note's letter moved up `steps`. Its pitch is the note's pitch
   moved up `rise`. The accidental is the difference between that pitch and the new
   letter's own, taken the nearest way round (B# is one above B, not eleven below).
4. If that needs a double sharp or double flat, the note is written by its plain name
   instead: in sharps if the key it is going to is written with sharps (its note is
   sharp, or is G, D, A, E or B), in flats otherwise.
5. The quality is put back unchanged.

A chord that cannot be read, a key that is no key, or the same key twice, gives the
chord back exactly as it was.

## The four notations

ProPresenter can show chords four ways, numbered in its file: 0 letters, 1 numbers,
2 numerals, 3 Do Re Mi. That is a setting of a stage layout's text box, not of the
song, and nothing here writes it. The editors always show letters. The function is
here because the same module in Simple Presenter draws the stage, and because a
"show as numbers" switch in the editor would be a cheap thing to add later.

`notated(chord, key, notation)` and `shown(chord, originalKey, key, notation)` (move,
then notate). Vectors: `notated.json`, `shown.json`.

- **Numbers**: each note by its place in the key's major scale, 1 to 7, with `#` or `b`
  before it if it is off the scale: `6m7/1` for C#m7/E in E.
- **Numerals**: the same in Roman numbers. A minor chord is a small numeral and loses
  its `m` (or `min`); a diminished one is small with `°` in place of `dim`. The bass is
  always a capital.
- **Do Re Mi**: the notes by their Latin names, where C is always Do: Do Re Mi Fa Sol
  La Si, with the accidental after.
- For a minor key the places are counted from its relative major (A minor's are C's).

**A guess**, all three: ProPresenter does not say exactly what it writes and no file
shows it. These are the forms musicians use. Port them as they are; they are not on
the path of anything the editor saves.

## A key's own chords

`diatonic(key)`: the seven chords on the notes of the key's scale: for C, `C Dm Em F G
Am Bdim`. For a minor key they are its relative major's, starting from the key's own
note: for Am, `Am Bdim C Dm Em F G`. Empty for what is no key. Vectors:
`diatonic.json`.

These are what the keys 1 to 7 put on.

## What a chord being typed might be

`completions(typed, key, used)`, likeliest first. `used` is every chord of the song,
one entry for each time it is used. Vectors: `completions.json`.

What is typed is tidied first. Only names that **start with** it are ever offered,
never the very thing typed, and nothing twice. In this order:

1. The song's own chords, the most used first (among equals, in the order they first
   come in the song).
2. The key's own chords.
3. Then, if something is typed and it reads as a chord:
   - **after a slash** (the name ends with a slash, or has a bass note after its last
     slash that may yet be gone on with, as `D/F` may to `D/F#`): the chord up to and
     with the slash, over its third (the minor third for a minor chord), over its
     fifth, and then over the root of each of the key's own chords but its own root.
     The third and the fifth are written in flats if the chord's root is flat or is F,
     in sharps otherwise.
   - **otherwise**: the usual kinds on the root: plain, `m`, `7`, `m7`, `maj7`, `sus4`,
     `sus2`, `2`, `add9`, `6`, `9`, `5`, `dim`, `aug`, `m7b5`, `11`, `13`.

With nothing typed it is the song's chords and the key's. The sheet shows the first
fourteen.

## A box's chords, in order

`tidy(text, chords)`: what is fit to be written. Vectors: `tidy.json`.

- Names are trimmed; a chord with no name is dropped.
- Each place is brought onto a character the text has: below 0 to 0, past the end to
  the last character.
- A chord on a line break goes to the character before it, if that is not a line break
  too (the end of a line has no character of its own that shows).
- Two in one place: the later one given stands, in the earlier one's position.
- Then sorted by place, keeping order among equals.
- An empty text has no chords.

`ranges(text, chords)`: how each is written in the file (tidied first): from its place
to the next chord's place or the end of its line, whichever comes first, and never
empty. Vectors: `ranges.json`.

`carried(before, after, chords)`: where chords stand when the words under them are
changed. Vectors: `carried.json`. Neither app changes words, so this is not needed to
begin with. It is here for the day one does, and it is what a
[repair](08-repair.md) would use on a song whose words were changed elsewhere.

## Chords as ChordPro text

ChordPro writes a chord in square brackets before the character it is played on:
`[G]Amazing [C]grace`. Vectors: `chordpro.json`.

| Function | What it does |
| --- | --- |
| `toChordPro(line)` | A line of words and its chords (places along the line) as text. A line of chords alone is written as its chords with one space between: `[C] [G] [Am]` |
| `toChordPro(text, chords)` | A whole box: each line so, joined by line breaks. A chord belongs to the line its place is in |
| `chordsOf(chordPro)` | The chords in typed ChordPro text, each at the place it has among the words *as typed*, every bracket that is not empty taken as a chord. For the ChordPro editor |
| `withoutChords(chordPro)` | The text with every `[...]` taken out. What the ChordPro editor compares with the words it began with |
| `fromChordPro(line)` | A line of a ChordPro *file* read: a bracket that holds what could not be a chord is dropped (`[*Coda]`); a chord after the last word hangs on the last character; a line of chords alone becomes stand-ins, one for each chord, with the chords on places 0, 2, 4 |
| `placeholders(count, standIn)` | That many stand-ins with U+2001 between them |
| `isPlaceholders(line)` | Not empty, and nothing in it but U+200B and white space |

White space here is what Unicode calls a space separator, plus tab and the line ends:
U+2001 is one, and [To the letter](#to-the-letter) has the whole list. A zero-width
space is not on it, which is why it is named separately. (It is white space to one
library, Apple's Foundation, and that is why the list is written out.)

Reading a whole ChordPro file is in [09-chordpro-files.md](09-chordpro-files.md).

## Which key a song is most likely in

**New**: not in Simple Presenter. The rule, its reasons and its reference are in
`tools/keyfit.py`; [08-repair.md](08-repair.md) says what it is for. Vectors:
`key-fit.json`.

## To the letter

The rules were written with Qt's strings, and the vectors are what those answered. A
platform's own strings do some of the same things another way, and then the answers
come out the same for most songs and different for a few. These are the things the Mac
app, the first not written with Qt, had to pin down. Both apps do them as said here.

- **Everything is done on UTF-16 units**: a length, a place, "starts with", "is the
  same as", "is already in the list". Two names are the same only if their units are.
  (Swift compares strings by whole characters, and calls "é" written as one unit the
  same as "e" with an accent after it. In .NET ask for an *ordinal* comparison.)
- **White space** is U+0009 to U+000D, U+0020, U+0085, U+00A0, U+1680, U+2000 to
  U+200A, U+2028, U+2029, U+202F, U+205F and U+3000, and nothing else. It is what is
  taken off the ends of a name or a line, and what `isPlaceholders` allows beside
  U+200B. Hold the platform's own list against this one before using it: Apple's
  Foundation has U+200B in its list, and trimming with it empties a line of stand-ins.
- **The two rules whose reference is Python** ([which key is
  likely](#which-key-a-song-is-most-likely-in), and the line-break check of
  [08-repair.md](08-repair.md)) take U+001C to U+001F as white space too, as Python
  does. And a key's score is rounded to four places as Python's `round` does it, by
  the exact value the machine holds: print the number to four places and read it back,
  rather than multiply, round and divide, which does not always give the same.
- **A letter or a number**, in `couldBecome`, is a UTF-16 unit whose Unicode category is
  one of the letters (Lu, Ll, Lt, Lm, Lo) or one of the numbers (Nd, Nl, No). Each
  unit is asked by itself, so a letter from beyond the first 65,536 characters is
  neither: it is two units. (In .NET that is `char.IsLetter` or `char.IsNumber`, and
  not `char.IsLetterOrDigit`, which leaves two kinds of number out.)
- **A note's letter** is one of A to G or a to g, and no other character.
- **A line** ends at U+000A and nowhere else.
- **Sorting keeps those that are equal in the order they came in.** Not every
  platform's own sort promises that.
- **A directive in a ChordPro file** is a line that starts `{` and ends `}`, with a
  name, and perhaps a colon and a value. The name is what comes before a colon, a `}`
  or a space; space, tab and the line ends around the name and the value do not count.
- **A line that only names a part** ("Verse 1", "CHORUS:", "[Bridge]") is, once any
  square brackets round it and one colon after it are off: one of *verse, chorus,
  pre-chorus, pre chorus, prechorus, bridge, intro, outro, ending, tag, interlude,
  instrumental, refrain, turnaround, vamp, coda, hook, post-chorus, post chorus,
  postchorus*, in the letters A to Z of either case; then perhaps a number, of the
  digits 0 to 9, with perhaps one space before it and perhaps one letter after it;
  and nothing more. Its name is that in small letters, with a capital to start and
  after each space or hyphen. (Simple Presenter finds these with a regular expression
  that would also take a long s for an "s" and the Kelvin sign for a "k". That is not
  copied.)

## The vectors

Each file is one JSON object with an `about` that says what is in it. The shapes are
plain: `cases`, each with its inputs and an `out`. `transposed.json` and `notated.json`
are tables instead, to keep them small: a list of `chords`, and rows whose `out` is the
answer for each chord in that order.

Some strings hold characters that cannot be seen (U+200B, U+2001). Compare strings
unit by unit, never by how they print.

A test should run every case of every file and say, for a failure, which file, which
case and what was got. Do not copy cases into the test's code: read the files, so that
when they are made again both apps are held to the new ones without a line changing.
