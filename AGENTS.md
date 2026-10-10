# For an AI model working in this repository

This file is for you, the AI coding agent, whichever one you are. Read it before doing
anything here. It is short on purpose; `spec/` has the rest, and
[spec/00-overview.md](spec/00-overview.md) says which page is for what.

## What this is

Pro7 Chord Editor puts chords on the songs of a ProPresenter 7 library: it opens the
`.pro` files ProPresenter keeps its presentations in, lets chords be put over the words,
and writes the files back, so that ProPresenter shows the chords on a stage screen.

There are to be two apps that do the same thing the same way: one for the Mac in
`mac/` (SwiftUI) and one for Windows in `windows/` (.NET, WPF). What they are to do was
worked out and built once already, as the chord editor of
[Simple Presenter](https://github.com/greyshirtguy/Simple-Presenter), a Linux app by the
same person. That editor is the specification. `spec/` describes it, `testdata/` holds
what its rules answer for thousands of inputs, and `reference/` has its source to read.

It is one person's hobby, written by describing it to an AI model. Nobody supports it.
It is not ProPresenter and has nothing to do with Renewed Vision.

## What must stay true

These are not style. A change that breaks one of them is wrong however well it works.

1. **A song's file is never left worse than it was found.** A `.pro` file holds far
   more than this app understands. Every write parses the whole file, changes only what
   the change is about (a text box's chords, the song's key, and for a line of chords
   with no words the characters they hang on), and writes the whole thing back with
   everything else as it came, fields this app has never heard of included. Never build
   a file from the parts you know. The file is written beside itself and swapped in, a
   copy of what it was is kept first ([spec/07-backups.md](spec/07-backups.md)), and
   nothing is written while ProPresenter is running.
2. **The words of a song are not this app's to change.** Only chords are. The one
   exception is spelt out in [spec/01-file-format.md](spec/01-file-format.md).
3. **The rules are the vectors.** What a chord is, how it moves to another key, where
   it is written in the file: `testdata/vectors` has the answers, made from the code of
   the working editor. Your code is right when it gives those answers. Never change an
   expected answer to make a test pass. If a vector looks wrong to you, stop and say so:
   it may be, but that is the maintainer's to decide, and both apps change together.
4. **Two apps, one behaviour.** Someone who uses both should not be able to tell which
   one put a chord on, and a file is the same whichever wrote it. Where a platform has
   its own way of doing a thing (Command where Windows has Control, where settings
   live, what a menu looks like), follow the platform;
   [spec/10-platforms.md](spec/10-platforms.md) lists those. Anything else that would
   differ is a change to `spec/` first, agreed with the maintainer, and then to both.
5. **Places in the words are counted as the file counts them**: in UTF-16 units, a line
   break being one. C# strings count that way already. Swift strings do not: use the
   UTF-16 view, everywhere a place is a number.
6. **The person's library is not yours to risk.** See the next section.

## The person's files

- Tests never run on a real ProPresenter library. They run on the files in
  `testdata/files`, or on copies made in a temporary folder.
- While you are developing, do not open the maintainer's own library with a build that
  can write, unless they say to. Reading it to see what real files look like is fine
  (`tools/look.py`).
- Songs are other people's copyright. Nothing from anyone's library goes into this
  repository, an issue, a pull request, a log you paste somewhere, or a test file.
  `testdata/` is made of hymns long out of copyright and of made-up words, and must
  stay so.
- Before deleting anything, look at what it is. Do not delete by a path built from a
  variable that could be empty.

## How to work here

- Read [spec/11-plan.md](spec/11-plan.md): the work is cut into milestones, each of
  which ends with tests that pass and something the maintainer can try. Do one at a
  time, and say plainly at the end of each what was built, what was checked and how,
  and what was not.
- Start every milestone from the logic and its tests, and only then the window. The
  logic can be proved against `testdata/` by you, alone. The window cannot: for that,
  write a test that draws the view into a picture file and look at the picture
  ([spec/10-platforms.md](spec/10-platforms.md) says how on each platform), and ask
  the maintainer to try what only a hand on a mouse can tell.
- Keep the rules out of the window's code. What a click does to a song is worked out in
  plain code with no window in it, which the tests call directly.
- Comment as the reference source does: every file opens by saying what it is and why
  it is the way it is; a guess about ProPresenter, and anything left alone on purpose,
  is said where it happens. A comment that has become untrue is a bug.
- If you do not know what ProPresenter does, say that you are guessing, in the code and
  to the maintainer. `spec/` marks what is known, what is guessed and what is new.
- When what you build shows `spec/` to be wrong or silent, do not quietly do something
  else: write down what you found (in your platform's `NOTES.md`), tell the maintainer,
  and change `spec/` with their agreement, so that the other app gets it too.
- No new dependency without the maintainer agreeing to it first.
- Words the user reads say plainly what a thing does. They never promise help, and
  never call the app a product.

## Where things are

| Path | What it is |
| --- | --- |
| `spec/` | What the apps are to do. Start at `00-overview.md` |
| `testdata/vectors` | The rules' answers, as JSON, for both apps' tests |
| `testdata/files` | Sample presentations, clean and damaged, and what is in each |
| `testdata/songs` | The ChordPro files those were made from |
| `reference/simple-presenter` | The source of the editor being copied. To read, not to build or change |
| `proto/` | The descriptions of ProPresenter's file formats (a submodule: `git submodule update --init`). The code that reads a `.pro` is generated from `proto/proto` |
| `tools/` | What made the test data, and `look.py` for looking inside a `.pro` |
| `mac/` | The Mac app |
| `windows/` | The Windows app. Until it is rewritten it holds the old one, whose bugs are the subject of `spec/08-repair.md` |
