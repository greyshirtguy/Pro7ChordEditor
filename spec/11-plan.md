# The plan

How the work is cut up, in what order, and what to say to an AI coding agent to start
it. The Mac app is built first, then the Windows app, each by a session on its own
machine, each through the same milestones.

## How a milestone goes

- One milestone at a time. Each ends with **tests that pass** and, from M3 on,
  **something the maintainer can try**.
- Start it in plan mode: read the pages it names, say what you will build and how you
  will prove it, and wait for a yes.
- Build the plain code and its tests first, the window after.
- End it with a short report: what was built; what was checked and how (which
  vectors, which files, which pictures you looked at); what was **not** checked and
  needs a person; what in `spec/` turned out wrong or silent. Then stop, and let the
  maintainer try it before the next.
- Keep `mac/NOTES.md` (or `windows/NOTES.md`): decisions made on the way and why,
  what was found out about ProPresenter on this platform, what is left rough. The
  next session, which will remember nothing of this one, starts by reading it.
- Commit at the end of each milestone, and oftener if the maintainer likes: small
  commits that each say what they do in plain words. Push only when asked; **but a
  rule or a decision the maintainer has agreed, once it is in `spec/` (and in
  `testdata/`, if it changes an answer), is pushed at once**. The two apps are built
  by two sessions that never meet, and the repository is all that either knows of
  what the other was told. (The maintainer, 2026-10-12: "we should push asap - so
  the windows session can be kept up to date with decision or rule updates we agree
  on ... my goal is all three apps work the same - same rules!" The third is Simple
  Presenter, which a session of its own changes from what the maintainer passes
  on: say in the report, in a few lines that can be passed on as they are, what a
  decision asks of it.)
- Start a session by fetching: the other app's session may have changed `spec/` or
  `testdata/` since.

## The milestones

| | What is built | Pages | It is done when |
| --- | --- | --- | --- |
| **M0** | **Setting up.** The project and its three layers (rules, song, app), empty. The reader of the file format generated from `proto/autogen-proto`. A test that reads one vector file and one `.pro`. The workflow that builds and tests on every push | [10](10-platforms.md) | `swift test` / `dotnet test` runs and passes from a fresh clone; the same on GitHub's machine |
| **M1** | **The rules.** Chord names, keys, moving to another key, notations, what is offered, a box's chords in order and as ranges, ChordPro text, the words of an RTF, which key is likely | [02](02-chord-rules.md), [01](01-file-format.md) (the words) | Every case of every file in `testdata/vectors` passes, bar `sheet-model.json` and `layout.json` |
| **M2** | **The song.** Open a `.pro`; hand over its text boxes; set a box's chords; set the key; save safely; undo and redo | [01](01-file-format.md), [05](05-song-and-saving.md) | The proofs listed at the foot of page 05, on copies of `testdata/files` |
| **M3** | **The library.** Find ProPresenter's folder; the lists; search; open a song and show it, read only, as plain lines with chords in brackets; know when ProPresenter is running | [06](06-library.md) | The maintainer can browse their own library and read any song. Nothing can be changed yet |
| **M4** | **The copies.** Kept before a first change; listed; looked at; put back; taken back | [07](07-backups.md) | The proofs at the foot of page 07. (There is no editor yet: prove it with the song's own functions) |
| **M5** | **The ChordPro editor.** | [04](04-chordpro-editor.md) | The maintainer can chord a song of their own, on a **copy** of their library, and see the chords in ProPresenter. This is the first build worth giving anyone |
| **M6** | **The chord sheet.** First its model, held to the vectors; then drawn; then the pointer and the keys; then the typing bubble; then dragging; then copying a line | [03](03-chord-sheet.md) | Every case of `sheet-model.json` and `layout.json`; the pictures set beside `spec/pictures`; and the maintainer saying it is as quick as Simple Presenter's |
| **M7** | **Chords with no words.** Adding to and taking from a line of chords alone, and putting chords on a blank slide (one with an empty text box, and one with none, which is given one), in both editors: the one place the app writes RTF, and the one place it adds anything to a slide | [01](01-file-format.md) (Chords with no words) | The RTF of every other line of a changed box is byte for byte what it was; a blank slide whose chords are taken off again in the same sitting leaves the file byte for byte what it was; a song so changed opens in ProPresenter with its chords in place, a blank slide that was given chords among them |
| **M8** | **Mending.** The checks; the marks; mending one song and all; the "(Chords)" copies | [08](08-repair.md) | The proofs at the foot of page 08. Check 2 built and left switched off until the maintainer has seen the fault in a real file |
| **M9** | **ChordPro files.** Reading one and showing what is in it; making the song; writing one out | [09](09-chordpro-files.md) | `parse-song.json` passes (it may be done in M1). A song made here opens in ProPresenter, on this system, with its chords showing on a stage screen |
| **M10** | **Giving it out.** The icon, the About text, the README for this app, signing and notarizing (Mac) or the zip and what the person will be told by Windows; a release | [10](10-platforms.md) | Someone else can download it and run it |

M5 comes before M6 on purpose: the ChordPro editor is a fifth of the work of the
sheet, and with it and the copies the app is already better and safer than the one it
replaces. M6 is the heart, and the largest by far: expect it to take as long as all
that came before. Do it in the order given, model first, and resist drawing anything
until `sheet-model.json` passes.

M7 to M9 can change order to suit what the maintainer wants next.

**Asked for since, and no milestone of the plan.** Each is built by both apps, when
its turn comes, from the page that has it:

| | Page |
| --- | --- |
| One of the key's chords dragged out of the strip onto a word | [03](03-chord-sheet.md), "A chord dragged out of the strip" |
| The app's version in the window's title | [06](06-library.md), rule 6d |
| The guide: one page on how chords are put on, shown once and under Help | [13](13-guide.md), with its words in [guide.json](guide.json) |
| Where each app is downloaded, in the README at the top | [10](10-platforms.md), "Getting it to other people" |
| The app's icon | [10](10-platforms.md), "Where each follows its platform" |
| One release for both apps, under one number | [10](10-platforms.md), "Getting it to other people" |

## What the maintainer is asked for along the way

- **Before M3**, on the Mac: leave to read (only read) ProPresenter's own settings,
  to learn how it records its workspaces, which was given on 2026-10-10 and has been
  done ([06-library.md](06-library.md), rule 3); on Windows: a look at the RTF of a
  song made there.
  ([10-platforms.md](10-platforms.md), the last section.)
- **At M5**: a copy of a real library to try on, and ProPresenter to look at the
  result in.
- **During M6**: the maintainer's hand on the mouse, more than once.
- **Before M7**: a small song made in ProPresenter that has a blank slide in it (an
  intro made as an empty slide), of words anyone may use, for `testdata/files`; and
  ProPresenter to look at what M7 writes. (**The first is no longer asked**,
  2026-10-11: what a blank slide is as ProPresenter writes one was read from the
  maintainer's own files, thirty slides with no text box and some 2,400 empty boxes,
  and a test file can be made to that shape from the one of ProPresenter's that
  `testdata/files` has. `mac/NOTES.md` has what was found. The second is asked
  still.)
- **Before M8's check 2**: the one test with the old editor
  ([08-repair.md](08-repair.md)).
- **Before M9**: a small presentation made in ProPresenter, of words anyone may use,
  for the app to carry as its model of a slide. Its file goes in `testdata/files` too:
  it will be the only test file ProPresenter itself wrote, and reading it, changing it
  and writing it back should be added to the proofs of M2 as soon as it exists.
  (**Given, 2026-10-10**: *Rock of Ages*, from the maintainer's library, is in
  `testdata/files/made-by-propresenter`, and the Mac app's proofs of M2 have it.
  Whether it will also do as the model of a slide is M9's to say.)
- **For M10** on the Mac: the Apple developer account.

## To start the Mac session

In a clone of this repository on the Mac (`git clone --recurse-submodules`), start the
agent in the repository's root and say:

> We are building the Mac app of Pro7 Chord Editor, in `mac/`. Read `AGENTS.md`, then
> `spec/00-overview.md`, `spec/10-platforms.md` and `spec/11-plan.md`. Do not write
> any code yet. Tell me what you understand the app to be, what you would set up for
> milestone M0 and how you would lay the project out, and anything in those pages that
> is unclear or that you think is wrong. Then wait for me.

Then, for each milestone in turn:

> Milestone M1. Read the pages the plan names for it and the reference source they
> point to. Plan first: tell me what you will build, in what order, and how you will
> prove it. When I say go, build it, run the tests, and report as the plan asks.

And at the start of any later session:

> We are part way through the Mac app of Pro7 Chord Editor. Read `AGENTS.md`,
> `spec/11-plan.md` and `mac/NOTES.md`, look at the last few commits, and tell me
> where things stand and what comes next.

## To start the Windows session

The same three, with "Windows" for "Mac" and `windows/` for `mac/`, and one thing
more in the first:

> ... The old app in `windows/` is to be replaced, not mended: read
> `spec/08-repair.md` for what was wrong with it and `spec/06-library.md` for what it
> knew that is worth keeping. Before anything else, read `mac/NOTES.md`: the Mac app
> was built first, and what it found out is there.

## When the Mac app has shown the pages to be wrong

It will. The pages were written from an editor on Linux by someone who could not run
either new app. When the Mac session finds a rule that is wrong, missing or cannot be
built as written, the page is changed, with the maintainer's yes, in the same commit
as the code that goes with it, and `mac/NOTES.md` says what was changed and why. The
Windows session then builds from pages that are already true for one app.

If a vector is wrong, it is not edited: the thing that made it is put right
(`tools/`, or Simple Presenter itself) and it is made again. That needs the Linux
machine and is the maintainer's to arrange.
