# Notes on the Mac app

For the next session, which will remember nothing of the ones before: what was decided
on the way and why, what was found out about ProPresenter on the Mac, and what is left
rough. Read it after `AGENTS.md` and `spec/11-plan.md`. Add to it at the end of every
milestone; put right what has become untrue.

## Where things stand

| | | |
| --- | --- | --- |
| **M0** Setting up | Done, 2026-10-10 | The package and its three layers, empty; the generated reader; nine tests; the Xcode project and an app with one window; the workflow |
| **M1** The rules | Done, 2026-10-10 | `ChordRules`: every rule of `spec/02`, the words of an RTF, the key fit and the old editor's shift. Every case of the fifteen vector files M1 is held to passes (some thirty thousand answers), with cases of our own for what no vector holds |
| **M2** The song | Done, 2026-10-10 | `Song`: a song opened and read (blank slides among its boxes), its chords and its two keys set, every change saved at once and safely, refusals, undo and redo. The proofs at the foot of `spec/05`, on copies of the test files |
| **M3** The library | Done, 2026-10-10 | Places found (workspaces from ProPresenter's settings, Documents, folders chosen), their libraries and songs listed, searched and narrowed by mark; a song shown to be looked at; whether ProPresenter is running; the app's own folder. The window, drawn into pictures by the tests. **The maintainer has not yet tried it** |
| **M4** The copies | Done, 2026-10-10 | A copy kept before a file's first change, listed (with or without its index), thinned at the start, put back and taken back. In the window: the mark, Earlier Versions on a song, All Copies in the settings. The proofs at the foot of `spec/07`. **Nothing in the window can make a copy yet**, so on a real Mac there are none to see until M5 |
| **M5** The ChordPro editor | Done, 2026-10-11 | A song is shown as ChordPro text and its chords typed in: the one rule of what may be typed, the help with brackets, a save a moment after the typing stops, all of one saving one step of undo. The two keys in the song's header. Refusals said under the song. **The first build that changes a chord from the window. The maintainer has not yet tried it**; it is to be tried on a COPY of a library first |
| **M6** The chord sheet | Built, 2026-10-11; **not yet tried by a hand, and not done until the maintainer says it is as quick as Simple Presenter's** | Its model, held to every case of `sheet-model.json` and `layout.json`; drawn as one view, set beside `spec/pictures`; the pointer and the keys; the bubble a chord is typed in; dragging; a line's chords onto another. A switch in the song's header between the sheet and the ChordPro text; a song opens on the sheet. Then, the same day, on the maintainer's trying it: one key picker; the slides laid out as **a grid** in frames of their groups' colours; and **zoom** |
| **M7** Chords with no words | Built, 2026-10-11. **Seen in ProPresenter by the maintainer, 2026-10-12: "They showed"** (which cases were among what was looked at was not said) | A line of chords alone is given a chord more or a chord fewer by changing that line's bytes in its box's RTF and no others; a blank slide's box is given its words at its first chord; a slide with no text box is given one, a copy of the song's own; and chords taken off again in the same sitting leave the file as it was to the byte. In both editors, whose "not yet" limits are gone. Held to a new vector file, to a test song made in the shapes real ones have, and tried on copies of 425 real songs. The plan's two proofs that a test can make are made, and its third, that ProPresenter shows what was written, the maintainer made. Since then, decision 58: a line that loses its last chord takes chords again |
| A chord from the strip (asked for after M7) | Built, 2026-10-12, and used by the maintainer the same day: "This is quite nice/exciting". Which of the seven things under "Left rough" were among what was tried was not said | One of the key's seven chords is dragged out of the strip and let go on a word, on a line of chords alone or on a blank slide, and is put there as its key would have put it (`spec/03`, "A chord dragged out of the strip"). The model and its tests, the sheet's end and the strip's end of the system's dragging and theirs, a picture |
| The guide (asked for after M7) | Built, 2026-10-12. **Not yet seen by the maintainer.** Its window was opened once, on 2026-10-13, by the 0.3 app shut in from the maintainer's files (decision 70): it opens at the first start and not at the second. Nothing else of it has been tried | One page on how chords are put on, in a window of its own: opened by the app once, at its first start on a Mac, and after that under Help and behind a ? at the right of the strip (`spec/13-guide.md`, new). Its words are `spec/guide.json`, which both apps carry a copy of; its three pictures are of the Mac's own sheet, drawn by a test and held to what the app draws now. With it: the README at the top says where each app is downloaded |
| The icon, and version 0.3 (asked for after M7) | Done, 2026-10-13, and given out the same day as the Mac's half of the release `v0.3` | The maintainer's own picture, in the app as the file they gave (decision 68). One release now holds both apps (decision 69) |
| What the maintainer asked for after two days with 0.3, and version 1.0.0 | Built and given out, 2026-10-13, as the Mac's half of the release `v1.0.0`, GitHub's "Latest". **The line of hints is a first cut, to be tried and changed**; none of it has been seen by the maintainer yet | An empty box that is kept takes the chord off (decision 72). A line of hints under the sheet (73). The menus kept to what is of use, and closing the window quits (74, 75). About shows the icon (76). Earlier Versions in a colour, and a title over the places (77) |
| What the maintainer said of 1.0.0, having tried it (2026-10-13) | Built the same day as 1.0.1, a build to try. **Never given out under that number**: what it brought went out in 1.0.3 | A text box's other ranges go with the words, as Simple Presenter has it (decision 79). The edge between the lists and the song starts half way, and is the app's own (80). The line of hints says nothing by the clock, and says the copying in the maintainer's words (81); the line the person is at is highlighted, in a colour of the settings (82) |
| The window's speed, and the edge that is dragged (2026-10-13, the maintainer having tried 1.0.1) | Built the same day as 1.0.2, a build to try. **Never given out under that number**: what it brought went out in 1.0.3 | The pointer stalled a fifth of a second whenever the hints changed, and the edge was slow to drag with a song open: both were the song's header, which is now laid out once (decision 84). The edge is a bar that is seen, and AppKit's (85). "What made the window slow", below, has the measurements |
| The tips refined, and a chord's colour (2026-10-14, the maintainer having tried 1.0.2: "It feels wonderful now! all smooth now.. and looks great") | Built the same day as 1.0.3, tried by the maintainer ("tips are fine"), and **given out on 2026-10-14 as the Mac's half of the release `v1.0.3`**, GitHub's "Latest" (decision 91). Windows' file was added to it the same day, and its tag moved (decision 92) | The tips in sentence case, Option's said on a chord and nowhere else, a tip over the key's chords, the copying under Command alone, the line blue and lighting up as it changes (decision 86). A chord's name orange as it comes (87). **Open, the maintainer's**: what a paste of a line's chords is to keep (88) |
| What the Windows app's notes had for this session, and how a number is given out (2026-10-14) | Done the same day, and **given out in 1.0.4** (decision 96) | How a new number is given out, the first half as a pre-release (decision 92). Three things put right in which this app did other than `spec/` has it, found by the Windows app's session and never passed on: putting back what the file already is offers nothing to undo; a key that is not one of the twenty-six is first in its picker; what a ChordPro directive's name starts with is asked of its units (decision 93) |
| Sharps and flats drawn as ♯ and ♭ (2026-10-14, the maintainer's) | Done the same day, and **given out on 2026-10-14 as the Mac's half of the release `v1.0.4`**, a pre-release until Windows' file is added (decision 96) | The rule, for all three apps: `spec/02`, "Sharps and flats drawn as signs", with `tools/signs.py` and `signs.json`, pushed at once. Drawn on the sheet, in the strip, in what is offered and in the box as it is typed, and in the song's two keys; never written. A switch in the settings, on as it comes (decision 95) |
| Changes allowed while ProPresenter is running (2026-10-17, the maintainer's) | Done the same day. **In the source and in no build** | A switch under Advanced in the settings, off as it comes, with its warning under it and a question before it is turned on; the band stays while ProPresenter runs, in other words (decision 97) |
| **M8** Mending | Next | |

## What the maintainer decided, 2026-10-10

1. **Both apps read files with code generated from `proto/autogen-proto`**, the set
   taken from ProPresenter itself, not `proto/proto` as the pages first said. The
   pages were put right the same day, in commits made beside this work
   (`spec/01-file-format.md` has the whole of it).
2. **"Everything else as it came" is said of the message, not of the bytes.** The
   maintainer's words: the rule "is about not disturbing/breaking ProPresenter's
   parsing of these protocol buffer files". `Core/Sources/ProFormat/ProFormat.swift`
   has the long of it.
3. **There is no `PathSettings.proPaths` on the Mac.** ProPresenter has workspaces,
   each with a folder of its own. The app is to show the workspaces it finds and let
   one be picked; anything else is opened with Choose Folder. How ProPresenter records
   them is to be found out at M3, by reading only (`spec/06-library.md`, rule 3).
4. **macOS 15 and 26** are "the newest macOS and the one before it".
5. **The ignore rules are one file for each app.** The Visual Studio list that was at
   the top of the repository would have left out any folder under `mac/` named
   `Packages`, `Debug` or `Backup…`. It is now `windows/.gitignore`; `mac/.gitignore`
   is the Mac's; the one at the top has only what is true of both.
6. **The maintainer has real songs on this Mac** that may be read, and checked
   against as copies outside the repository: some from Multitracks as they came, some
   changed by the old Windows editor. Ask where. Nothing of them comes into the
   repository (`AGENTS.md`, "The person's files").

And at M1, the same day:

7. **The words of an RTF are read by a port of the reference's small reader**, not by
   AppKit's (`NSAttributedString`). The page allowed either on the Mac. A port keeps
   AppKit out of the rules and gives both apps the same reader.
8. **`spec/02-chord-rules.md` has a section "To the letter"** with what the Mac port
   had to pin down (white space, what a letter is, comparing by units, a heading's
   words), so that the Windows app does the same.
9. **Two names are not the reference's**: its `Song` (a ChordPro file as read) is
   `Chords.ParsedSong`, and its `Range` is `ChordRange`. Swift has a `Range`, and
   `Song` is the layer above.

And after M1, on what its check of real songs found:

10. **A chord on a line of chords alone is written with a range one long**, as
    Multitracks writes it, and not run on to the next chord as Simple Presenter
    writes it. The maintainer's measure: the proto and the songs from Multitracks are
    the best source of what is right. `spec/01` and `spec/02` say so. At first the
    code waited for `ranges.json` to be made again from a changed Simple Presenter;
    then the maintainer said not to wait, and `Chords.ranges` has the rule. The vector
    is still not edited: see "Left rough" for the one allowance that makes.
11. **Blank slides are to be shown, and to take chords.** Simple Presenter leaves a
    blank text box out, so an intro made as an empty slide can be given none. The
    design is in `spec/01` ("A blank slide", "Chords onto a blank slide"), with what
    it asks of the sheet, the ChordPro editor and the song in `spec/03`, `04` and
    `05`. Showing them is M2's (`blank`, in what the song hands over); giving them
    chords is M7's.

And at M2, by saying go to its plan:

12. **The type is `OpenSong`**, in the layer still called `Song`: "the song that is
    open", as `spec/05` has it. A type named as its module is makes trouble in Swift.
13. **A song cannot save unless it is handed its safeguards**: a way to ask whether
    ProPresenter is running, and a way to keep a copy first. The real ones are M3's
    and M4's. One opened with none can be read and nothing else.
14. **The key is a step of undo**, as `spec/05` says; in the reference it is not.
15. **What a refusal says** is a list of sentences in `spec/05`, for both apps.

16. **A file ProPresenter wrote is in the test data**: *Rock of Ages*, from the
    maintainer's MainHall library. The maintainer said to use any file of it. One was
    chosen whose words are out of copyright, since `AGENTS.md` and `testdata/README.md`
    say nothing else may come in, and whose bytes hold nothing of anyone's: it was
    looked through first. `testdata/README.md` describes it.

And at M3:

17. **The window is a library of the package**, `AppViews`, with no `@main` in it,
    so that `swift test` draws it into pictures. The Xcode target is the few lines
    that show it.
18. **ProPresenter's settings are read at run time**, to find workspaces, and only
    read. Nothing is written in any folder of ProPresenter's.
19. **The places the app finds are those whose songs are `.pro` files**: workspaces
    kept as folders, `ProPresenter` in Documents, and folders chosen by hand.

And at M4:

20. **A copy's folder has the place in it**: `Backups/<place>/<library>/<song>/`. The
    page had none, from before there could be several places. `spec/07` says so now.
21. **A copy is shown as its lines**, as a song is in the window, until the chord
    sheet is built (M6).
22. **The song is opened with its real safeguards now** (`LibraryModel.open`): whether
    ProPresenter is running, and `Copies.keep`. M5 has nothing to add for a change to
    be safe; it only has to ask for one.

And at M5, 2026-10-11:

23. **A slide with no text box at all is a blank slide too**, handed over with no id,
    and is given a text box when a chord is put there, as Simple Presenter does it;
    and **chords taken off a blank slide again in the same sitting leave the file byte
    for byte what it was**. Both the maintainer's ("Let's adopt the ability to add/edit
    chords on blank slides! and yes do #2 as well (I will typically keep the simple
    presenter and stand alone editors identical)"). `spec/01` has both now. Reading
    hands such a slide over already; the writing of both is M7's.
24. **M5 was built on "and then proceed"**, with no plan shown first. The plan the
    page asks for was given with the report instead.
25. **The two keys are pickers in the song's header from M5**, not left for the sheet:
    without them a song cannot be told what key its chords are in, and ProPresenter
    is the one that moves them to another.
26. **Only the boxes that were typed in are saved** (`spec/04`, rule 24). The
    reference saves every box whose chords the text cannot show exactly, the first
    time anything is typed anywhere.
27. **A save that is refused makes the text again from the song** (`spec/04`, rule
    19a), so that what is seen is what its sentence says: nothing was changed. And
    if it was the save made on the way to another song, the song is not left, or the
    sentence would be swept away unread.
28. **Until M7** a line of chords alone keeps as many chords as it has and a blank
    slide takes none; what would change that is not let through, and says why
    (`spec/04`, "Until chords with no words are built").

And at M6, 2026-10-11:

29. **M6 was begun on "for now let's keep going"**: the plan was shown as the building
    started and not waited on, the first stage (the model, to the vectors) having no
    choice of the maintainer's in it.
30. **A song opens on the sheet**, the main editor, with the switch `Chords | ChordPro`
    in its header, as `spec/06` draws it. Which was last shown is not remembered from
    one start of the app to the next.
31. **Three things the model does that the reference does not**, each in `spec/03`
    now: a name the song already has is moved, copied and pasted as it is, chord or
    not (the page asked this); the changes of one paste are joined to each other and
    never to what came before it (rule 52); a place is never the second half of a
    character that takes two units (rule 18a).
32. **The sheet takes the keyboard when the pointer moves over it** (rule 23a), not
    only when clicked, unless a box of text is being typed in. Mine, so that pointing
    and pressing needs no click first. The maintainer, the same day: "sounds good".
33. **The key is picked in the song's header and nowhere else.** Both pages asked for
    a picker, so the sheet showed the key twice, in the header and in its strip. The
    maintainer, on trying it: "my gut says 'we only need one'... I would keep the top
    key picker". The strip has the key's seven chords and no picker (`spec/03`, rule
    14); Simple Presenter has its one picker in the strip.
34. **What is copied with Command+C is the sheet's own**, as in the reference: it is
    not put on the system's pasteboard, and so does not replace what the person had
    copied elsewhere.
35. **The empty lines a text box's words end with get no row on the sheet** (`spec/03`,
    rule 2a), unless a chord stands on one. The maintainer's, on seeing cards too
    tall: "prob best to remove the trailing blank lines for now". Only what is drawn:
    the words in the file are never touched. The ChordPro text still has those lines,
    as empty lines, since what may be typed there is held to the song line for line
    (`spec/04`, rule 6).
36. **Each card is as tall as its own slide; all are one width** (`spec/03`, rule 8).
    (As it stood for an hour. Decision 37 made the column a grid, where the cards of
    a row are one height.)
    The maintainer's, on the question of the card too tall: "I'd say yes (in fact
    the GUI 'feels' a little like 'I can't see enough words' (to me)... so let's start
    by fine tuning that (and I will say the same to [the session] managing simple
    presenter development on my linux box)". So Simple Presenter is being asked the
    same, and until it has it the two differ in this. `ChordSheet.mostLines` is still
    worked out, the vectors holding it, and the view draws nothing by it.
37. **The sheet's slides are a grid, and the sheet zooms** (`spec/03`, "How it looks"
    and "Zooming"). The maintainer's, an hour after decision 36: "what if we took a new
    turn.... Currently the 'slides' are a thin vertical stack..... What if they laid
    out in a grid - like the slide thumb previews in simple presenter (but nice large
    plain text with room for inserting chords above) ... flowing like scrollable grid
    (zooming alters text size and therefore slide size and slides reflow when zoomed)
    - This method could also draw the group colour border around each slide (with a
    thicker border at bottom for group label on first slide of each group)", and "if I
    like this I will def need to back port to simple presenter". Simple Presenter's
    own grid of thumbnails was read for it (`qml/SlideGrid.qml` in its repository, not
    in `reference/`): the frame of 4 in the group's colour, the number and the
    group's name on its foot, the orange ring, and two buttons that step the size.
38. **What I chose within it, each the maintainer's to change**: the usual zoom is 80
    in the hundred, of nine steps from 50 to 150; every slide's frame has the foot,
    with its number, as Simple Presenter's thumbnails have (the maintainer's words
    could be read as the first slide of a group only); the cards of one ROW of the
    grid are one height, so that the feet of a row stand level (decision 36 had each
    its own); the ring is on the slide that is PICKED, there being no slide that is live
    here; the sheet's top margin is 30, for the typing bubble; Down and Up go by the
    screen (`spec/03`, rule 20); and the window opens at 1280 by 800, where it was
    1100 by 680.
39. **A column of the grid is only as wide as its widest slide needs** (`spec/03`,
    rule 8). At first every card was as wide as the song's widest line and a row's
    width was shared out among its cards, which left room beside the words in most
    of them. The maintainer took it for room kept for chords: "let's drop that
    pre-emptive free space and keep the dynamically growing only when needed - (this
    gives a nicer grid with more slides)". So nothing is kept: a slide is as wide as
    its lines need now; a column as wide as its widest slide, so that columns stand
    in line; and what is left of the sheet is at its right. (Mine: columns of their
    own widths and not every card its own, which would pack still more to a row but
    in no lines up and down; and 44 kept on a line of chords alone, without which
    the place past its last chord could not be pointed at.)
40. **Whether ProPresenter is running is asked again whenever the system says it has
    started or quit** (`spec/06`, rule 18), the maintainer having quit it and seen
    the band stay.
41. **The window is two parts, as ProPresenter's is: the songs are under the
    libraries, at the left** (`spec/06`, rules 6a to 8). The maintainer's: "Let's
    mimic the Pro/Simple Presenter UI and move the list of docs ... to the left side
    below the list of library (with a pretty divider that is draggable vertically ...)
    - let's make the background color of those two different - like propresenter ...
    (library is light grey, presentations is darker grey) ... and still keep the left
    pane draggable horizontally. Also let's use orange icons for the libraries ...
    and the grey icons for the presentations". The greys in the dark are
    ProPresenter's own, read off the picture of its window the maintainer pointed to.
42. **The two icons are ProPresenter's own pictures**, at the maintainer's asking: its
    mark for a library, and the outline it draws before a presentation, from a file
    the maintainer supplied. They are drawn in code from the outline's path
    (`ListIcons.swift`), and the mark's orange from three colours read off a picture
    of it: no picture file of either is in the repository. They are Renewed Vision's,
    which the file and `spec/06`, rule 6c, say; the maintainer was told so.
43. **Every card of a song is one size, and the shape of a screen** (`spec/03`, rule
    8). The maintainer, with the grid before them: "now it looks so good I am
    tempted to sacrifice some space efficiency to get the slides more uniform and
    more closer to 16:9 (currently a bit too wide - which is awesome for fitting more
    text! ..but now I am getting fickle about the looks - chasing a balance)". So:
    one width, the widest slide's (decision 39's columns of their own widths are
    gone); 16 by 9, but never taller for that than four lines need; a row taller
    only where a slide of it has more lines than the shape holds; and a slide's
    lines in the middle of its card. (Mine: the limit of four lines, without which a
    song of long lines has cards mostly empty; and the lines standing in the middle
    up and down, and at the left along. Lines in the middle along as well, as a
    slide has them, was thought of and not done: they are read down their left edge
    when chords are put on.)
44. **The ChordPro text has a size of its own** (`spec/04`, rule 25), the maintainer
    having looked for a zoom there: "I could not see a zoom for ChordPro (I suppose
    it should really be a text + - or something)". Kept apart from the sheet's zoom,
    in the same steps.
45. **The switch between the two editors is in the middle of the header, large, with
    a picture on each button** (`spec/06`, rule 12). The maintainer: "the whole dual
    mode editor needs to be a bit more front and centre obvious ... perhaps icons in
    the two buttons ... surprise me with some delightful icons... and perhaps we
    reposition those buttons to center???". The two pictures are mine, drawn in
    `EditorSwitch.swift`: the sheet's spot over a line of words, and a C in square
    brackets. Command+1 and Command+2 are mine too.
46. **A paste says what came of it, and a click and then Command+V pastes there**
    (`spec/03`, rules 52a and 52b). The maintainer: "the copy and paste a lines chords
    mostly worked - some slides seem to not accept the paste... can you double check
    that". Checked on copies of their songs: the pasting itself does what the page
    says every time. What it did not do was say anything when the page has it do
    nothing; and after a click on a line the typing bubble had the keys.
47. **The settings are in sections: Chords, with the two colours, and Backups**
    (`spec/06`, rule 20a). The maintainer: "let's add some more settings (better put
    backups in its own section).... I want a setting for the Chord colors in the gui
    chord editor and chordpro editor". (Mine: one colour for each editor and not one
    for both; "the usual colour" kept as nothing, so that the ChordPro text's goes on
    following the system's orange; the earlier versions' panels, which only show a
    song, keep the system's orange.)
48. **Nothing is saved by hand, and the window says so: Revert, and the words
    "Autosave on"** (`spec/06`, rule 12a; `spec/07`, rule 22a). The maintainer asked
    for saving at once or by hand as a setting, and for thoughts; was given them
    ("Asked for by the maintainer, for later" has them still); and chose: "autosave
    indicator and Revert". A Save button that was always greyed stood beside the
    words at first, as the maintainer had thought of; on seeing it: "you can leave
    out the save button". (Mine: where they stand, at the right of the quiet line;
    "Autosave paused" while ProPresenter runs; and Command+S, kept as File > Save,
    saying there is nothing to do: without it the key would only make the system's
    sound for a key that is nobody's.)
49. **Revert asks first, and says it is every change** (`spec/06`, rule 12a). The
    maintainer: "let's popup a confirmation about reverting - some wording that
    makes it clear it will revert ALL changes the user just made (not just reverting
    the last step/change)". The question is the model's (`askToRevert`,
    `revertQuestion`), so that its words are tested; the window only shows it. Its
    one sentence is the maintainer's own; mine said three things more and was "too
    long".

And at M7, the same day. The maintainer said yes to five lines of plan (a chord more
or fewer on a line with no words, changing only that line's bytes; a first chord on a
blank slide with an empty box; on one with no box, which is given a copy of the
song's own; chords taken off again in the same sitting leaving the file byte for
byte; the "not yet" limits lifted in both editors) and to the test file being made
here and not by them. **Everything below is mine, within that**, and is in `spec/01`
and `spec/05` for the Windows app to do the same. Each is for the maintainer to
overrule.

50. **A line's stand-ins are changed by their own bytes and nothing between them**
    (`spec/01`, "Chords with no words", steps 1 to 7). The page said "find the bytes
    that are that line's text and put the new stand-ins there, leaving every other
    byte alone". Real songs showed what that has to mean: Multitracks writes each
    stand-in in the song's font and each em quad in another, so control words lie
    between the characters of one line. They stay, since they say how what follows
    is set; what is taken out is each character's own bytes; what is new goes where
    the first of those was, or after the line's last character. A `\uc0` before a
    character is not the character's and stays too, so a line that has lost chords
    is left with a few control words in a row that say nothing. They harm nothing,
    and ProPresenter writes the RTF afresh the next time it saves the slide.
51. **What the app cannot be sure of it does not write.** Whatever is made is read
    back, and must be the old text with that one line changed. One kind of RTF is
    known to fail that on purpose (a character standing in after each numbered one,
    which Apple's text system never writes and ProPresenter on Windows may): such a
    line is refused, in a sentence. Writing in that way of RTF too was thought of
    and not done: no file of it has been seen.
52. **Only a line of chords alone is ever made stand-ins.** A line that has words is
    refused, and so is an empty line. The reference makes stand-ins of whatever line
    it is asked for, words and all, and is safe only because nothing in it asks.
53. **A line that is not a clean run of stand-ins is made one the first time it is
    changed** (three plain spaces with a chord on them; more stand-ins than chords),
    even where the number of chords stays. That is what the reference ends with, and
    what the sheet takes for granted: chord n of the line on place 2n.
54. **What was made for a blank slide is remembered by its slide and its id**, an id
    being one box's only on its own slide (in 100 of 425 real songs, boxes share
    ids); and **is forgotten when an earlier version is put back**, which is another
    file. For a box that was added, the slide's cue is remembered as it was and put
    back whole, so that the file is what it was down to its empty parts.
55. **Two sentences are new** (`spec/05`, "What a refusal says"): "That is not a line
    of chords with no words. Nothing was changed.", which nothing in the app can
    bring about, and "A chord could not be added to this line, or taken from it: its
    text box is written in a way that cannot be changed here safely. Nothing was
    changed." **The maintainer has not read either.**
56. **A new id is written in capitals**, as ProPresenter on the Mac writes them,
    unless the box it is a copy of has an id in small letters. The page said
    "written as the file's others are".
57. **The rule has a reference and vectors of its own**: `tools/standins.py` and
    `testdata/vectors/stand-ins-in-rtf.json`, as the key fit and the mending checks
    have, being new. And **a test song in the shapes real ones have**,
    `testdata/files/shaped-as-real-ones/Chords With No Words.pro`, made by
    `tools/make-fixtures.py` from the one file of ProPresenter's own. Neither was
    asked for in so many words. Both are so that the Windows app can be held to the
    same bytes without ever seeing a real song.

And on 2026-10-12, the maintainer having read what M7 left to ask:

58. **A line of chords alone that loses its last chord takes chords again: it is left
    one zero-width space** (`spec/01`, "Chords with no words"). The maintainer:
    "hmm yes - if a blank line had chords and you deleted all - you should be able to
    put some back on that line", to the question whether such a line should keep
    one invisible character. For all three apps: "my goal is all three apps work the
    same - same rules!". Mine, within that:
    - **one rule for every line of chords alone**, the whole of a slide or one line
      of a box, and not only the line under a verse that was the trouble;
    - **a zero-width space, whatever the line hung its chords on.** Its own plain
      space was kept at first, and copies of the real songs said no: a text box
      left holding one plain space and no chord is read back as a stray space. Of
      108 lines that have chords, 32 would have come back as blank slides, and 2 of
      those as another text box's. With a zero-width space all 108 come back as the
      lines they were;
    - what was made for a blank slide in the same sitting is still unmade whole,
      keeping nothing: that was decided first, and leaves the file as it was.

    The song's stand-in in `tools/make-vectors/sheet.js` has the rule, and
    `sheet-model.json` was made again on this Mac with cases for it
    (`testdata/README.md` says how, and that not one old case changed). **Simple
    Presenter's own code has not got it yet**: that is the maintainer's to pass on.
59. **What is agreed is pushed at once.** The maintainer: "we should push asap - so
    the windows session can be kept up to date with decision or rule updates we
    agree on". A session is now building the Windows app from `spec/`, and reads
    the repository for what the rules are.

And the same day, the maintainer having used the app and caught a hand at something
it could not do:

60. **One of the key's chords can be dragged out of the strip onto a word**
    (`spec/03`, rules 46a to 46h, new). The maintainer: "my brain wanted to add a
    chord by drag and drop... I think other user might do that same - Let's add
    that (despite not being as fast as clicking/hotkeys)". That it be added is
    theirs. What it does to the letter is mine, each to be said otherwise:
    - **let go over a place it does what its key does with the spot there**, and so
      asks the song nothing new: no vector changed, and none could hold it;
    - **it lands only where it is shown.** Over the bare sheet, a frame or its foot
      there is no lit bubble, and letting go puts nothing on. A chord dragged along
      the sheet is otherwise (off the cards it lands on the last place it was
      over), and that was left as it is. One from the strip has crossed other
      slides on its way, and a hand that lets go between two of them is not to
      leave a chord on the one behind;
    - **it can be carried while another chord is being typed**, which a chord of the
      sheet's cannot: picking it up keeps what was typed and closes the bubble. To
      refuse it, as rule 40 does, would make the strip dead after every click on a
      word, since a click opens the bubble;
    - **the key that copies does nothing**, there being nothing to move;
    - **the sheet scrolls** when it is held near the top or bottom of what is seen;
    - each button says its three ways when pointed at.
61. **It is carried by the Mac's own dragging** (`spec/10`, the table): the button's
    picture under the pointer, the green plus where it can be put, the slide back
    where it cannot, Esc. None of that is written here, and Windows has its own.
    What is carried is the chord's name under a type of this app's own, offered to
    this app only.

62. **The window's title says the version, after the name in square brackets**:
    "Pro7 Chord Editor [0.2]" (`spec/06`, rule 6d, new, for both apps). The
    maintainer: "show the version # in the main app title (after its name in [])".
    Mine: that a build which knows of no version says the name alone; and that the
    title is made in the package (`AppTitle`, in `LibraryWindow.swift`), where a
    test holds it, the app's own folder being no place for a rule.
63. **Version 0.2, and a release of it** (2026-10-12). The maintainer: "Let's bump
    to version 0.2 ... and make a downloadable release". It is 0.1 with the chord
    from the strip and the version in the title. The build number went from 1 to 2
    with it. "The disk image" below has what was checked.

And the same day, on how someone new is to find out what the sheet can do:

64. **A guide, in the app, opened once at the first start** (`spec/13-guide.md`, new,
    for both apps). The maintainer asked for "a nice onboarding experience to reveal
    all the cool editing options", with a pop-up guide, a link to a video and a
    video in the app as the ways thought of; was advised a page in the app with its
    pictures drawn by the tests, no video yet, and a practice song later; and said
    "lets build the guide - open it once at first start", and "the guide will have
    mac screenshots - for mac - but win screenshots for win". Those three things
    are theirs. Mine, each to be said otherwise:
    - **its words are a file of the spec, `spec/guide.json`, and no sentence of it
      is in either app's code.** Each app carries a copy, the same to the byte, and
      a test holds the copy to the original. It is the longest thing either app
      says, and two sessions that never meet would otherwise each have their own
      wording of it by the second change. The keys are named in it by short names in
      curly brackets, with each platform's word for each;
    - **it is a window of its own, and not a panel to be put away first**: what it
      says is to be tried as it is read, on the song beside it;
    - **what it says first is not a way of putting a chord on**: a backup, quit
      ProPresenter, there is no Save, a copy is kept. The maintainer was told of
      three such things; **the backup is a fourth, added by me**, because the guide
      is the one thing certain to be seen by someone who never read the README;
    - **one way is said large** (point at a word and press 1 to 7), and the rest is
      a list under it, for the second visit;
    - **that it was shown is written when it opens**, not when it is closed, and
      writing it touches nothing else that is kept. So whoever had the app before
      has it shown once too: the maintainer will, at the next start;
    - **it is asked for under Help** (Command+?, in place of the system's "this app
      has no help") **and with a ? at the right of the strip**, a little apart from
      − and +. The ChordPro text has no such button: Help has it there;
    - **three pictures** (the spot under the pointer, a chord being typed, a chord
      being dragged), **and no picture of a chord carried from the strip**: it would
      be the third one again with a button faint. The line of ChordPro is not a
      picture but text, in the colour chords have there.
65. **The guide's pictures are kept in the repository**, in
    `Core/Sources/AppViews/Guide`, which `spec/10` had said pictures are not: these
    are part of the app. A test draws them and **holds the kept ones to what the app
    draws now, dot for dot**, so that a change to how the sheet looks fails until
    they are made again and looked at. Not on GitHub's machine, which draws letters
    a hair otherwise: there they are only seen to be there. **The pointer in them is
    drawn by the test**, there being none in a window that is drawn, and is not the
    system's own picture of it, which is whatever size and colour the person has set.
66. **The README at the top has a "Try it"**, with a link for each system that
    fetches the newest file straight away (`spec/10`, "Getting it to other people":
    whoever publishes a release brings their row up to date in the same push). The
    maintainer: "an obvious answer to someone who wants to try it out: links to both
    releases (how might we handle that 'latest release' cant point to both
    options?". GitHub gives "Latest" to one release, and while the new apps are
    pre-releases it has it on the old Windows editor: so the README is the way in,
    and says so. **What was put to the maintainer and is theirs to decide**: one
    release for both apps from the next version on, the image and the zip side by
    side under one number, which "Latest" could then be given to.
67. **The version is still 0.2**, though the app now has the guide and 0.2 as given
    out has not. No release was asked for; the number goes up when one is. Until
    then a build made here says 0.2 in its title and is not the 0.2 on the Releases
    page. **`mac/make-dmg.sh` is not to be run before the number goes up**: it would
    write over `mac/build/Pro7ChordEditor-mac-0.2.dmg`, the copy of what was given
    out. (**No longer so since 2026-10-13**: the number is 0.3, and the script
    writes a file of that name.)
68. **The app has an icon: the maintainer's own picture** (2026-10-13: "I have made
    an icon"), given as three files at the top of the repository, which now are
    `mac/App/ChordEditor.icns`, `windows/App/ChordEditor.ico` (beside the Windows
    app's project file, for that session to put to use) and
    `pictures/ChordEditor.png` (for the README, which shows it over the name).
    Each is as it was given, to the byte. Mine in it:
    - **the .icns is used as it is.** The app's `Info.plist` names it
      (`CFBundleIconFile`), and Xcode copies it into the app because it is in
      `mac/App`, a folder the project takes whole. An asset catalogue was the other
      way: Xcode would then make the icon again from pictures taken out of the
      file. Nothing is gained by that, and the file in the app would no longer be
      the one given;
    - **two things hold it**, since a build goes through with the icon gone or
      misnamed and the app then wears the system's blank one: a test
      (`TheAppsFolderTests`: what `mac/App` names is there, and is a whole .icns
      with its largest and smallest pictures) and `mac/make-dmg.sh` (the file is in
      the built app, the same to the byte);
    - **`pictures/` is new**, at the top: the README's pictures. The pictures of
      the apps that the maintainer means to take go there too.

    What was seen of the file, for the maintainer to weigh (said to them, nothing
    done): it has no picture of its own for 16 and 32 points on a screen that is not
    fine (it has those sizes only at two dots to the point), so such a screen is
    shown a larger one made smaller; its shape fills 90% of the picture where
    Apple's own icons fill 80%, which macOS 26 puts right by cutting every icon to
    its own shape and macOS 15 does not; and its lower half is close to
    ProPresenter's own mark, which like the two pictures in the lists
    (`ListIcons.swift`) is theirs to weigh in something given out.
69. **One release holds both apps, under one number, from 0.3**
    (`spec/10-platforms.md`, "Getting it to other people"; `spec/06`, rule 6d). The
    maintainer, 2026-10-13, asking for "a new build and release": the Windows build
    "will get added to the new release". So the tag is `v0.3` and not `mac-v0.3`,
    the Mac's image went into it first, and its text says Windows' is to come.
    **Mine, and theirs to overrule**: that the two apps then go by one number (a
    release has one, and a file in it under another would mislead); that it is a
    pre-release still, with GitHub's "Latest" left on 0.9.5, since moving that label
    is retiring the first editor in the eyes of whoever lands on the page, and was
    not asked for. With both files in one release the label can at last name both.
    (0.3 will sit oddly under 0.9.5 when it does: a number above it, 1.0 say, would
    not. Said to the maintainer.)
70. **This session started the app, once, shut in.** Until now it never had: a
    build that can write is not to open the maintainer's library unless they say so
    (`AGENTS.md`), and the app opens the library it last had. But a release was
    asked for of a build whose guide opens by itself at the first start, and
    nobody had ever opened that window: the app's own log on this Mac shows the
    maintainer last started it before the guide was built. So the app that is in
    the 0.3 image was copied to this session's folder and started there under a
    rule of the system's (`sandbox-exec`) that **denies it every read of the
    maintainer's home folder and of other disks, and every write to them**, with a
    made-up home folder that had six of the test songs as its ProPresenter library.
    The rule was tried first on a shell: it could neither list the home folder nor
    make a file in it. What was seen is under "The disk image". What it touched
    outside this session's folder: the system's own record of where the app's main
    window last was, which it wrote back as it stood. **This is not leave to run
    the app in the ordinary way**, which stays the maintainer's to give.
71. **The test data was made again on the Linux machine (2026-10-12), and the Mac's
    tests failed on GitHub for a day** before this session next ran: they counted
    the eleven files Simple Presenter had read, and it had read a twelfth, Chords
    With No Words. Put right in the tests alone; nothing in the app changed. The
    twelfth is read here as Simple Presenter reads it. What a paste answers and
    says is held to the vectors' new `pasted`; the two new songs, `ends` and
    `lead`, passed as they came. (**Fetch before starting, and look at GitHub's
    builds**: the Windows build is red for the same cause, and is that session's.)
    One thing in those commits is **open, and the maintainer's**: a text box's
    other ranges ("these are in capitals") when a line of chords alone is made
    longer or shorter (`spec/01`, "Where Simple Presenter differs"). This app
    leaves them, as the pages say, so a range over the whole line ends past the
    words once a chord is taken off. Said to the maintainer, with what this
    session would do: follow Simple Presenter, so that no range is ever written
    that ends past the words. (**Settled on 2026-10-13**: decision 79.)
72. **An empty box that is kept takes the chord off** (`spec/03`, rule 35a, new: the
    maintainer's, 2026-10-13). One line of `ChordSheet.finishTyping`: a name that is
    empty takes, where it used to do nothing. No vector is contradicted, and none
    holds it: it is Simple Presenter's to follow, and the case wanted is in
    `testdata/README.md`. **Mine**: that Tab is left as it was (it takes the first of
    what is offered, and for an empty box the key's seven always are), and that it
    goes by what is in the box and not by how the box was opened, as rule 35's
    Backspace does.
73. **The line of hints** (`spec/03`, "The line of hints", new, and marked there as
    being tried on the Mac and not agreed: the others wait). The maintainer's:
    that the tips have more room, change with what the mouse is doing ("hovering an
    existing chord it shows Delete to remove"), roll through otherwise, show the
    copying with Option held, and name the keys by the Command sign. **Mine, all to
    be overruled**: every word of it; that it is a line of its own over the quiet
    line, which now says how much there is of the song; that keys are drawn as
    small keys; that Command held shows the copying as well as Option (it is the
    key about to be pressed); that a tip stays nine seconds, and they go round in
    order, not at random; that the hints for the spot are said only while the
    pointer is over a place or a key has just moved the spot; and that where there
    is not room the last hints are left off, so copy and paste come before the
    whole verse. **Not built, and put to the maintainer as the next thing to try**:
    a small cross on a chord's bubble while the pointer is on it, to take it off
    with a click. (**Tried by the maintainer and changed the same day**: decisions
    81 and 82. The tips by the clock are gone, and so is what is said here of
    them.)
74. **The menus are kept to what is of use** (`spec/10`, "Menus"). Out: Save, the
    maintainer's guess that it is not needed; Delete, and what macOS adds to every
    Edit menu. Command+S is still answered, by the window (`WindowKeeper`), and
    says what it said. **Found by having the app write its menus out** (below):
    what SwiftUI lets an app replace as "save" has Close in it too, so no build
    until now had File > Close or Command+W, and Close is said again; and with the
    two commands for tabs gone View had nothing in it, so a menu with nothing to
    choose is not shown. What is not wanted is hidden, not removed (the head of
    `AppMenus.swift` says why).
75. **Closing the window quits the app** (`spec/10`; the maintainer's). The window
    is told by AppKit when it closes (`WindowKeeper`) and the app is asked to quit,
    which saves what was being typed as any quitting does. Not by SwiftUI's own
    word for a view going away, which this session could not be sure is said only
    then. What is done is handed to the window by the app, and is nothing under
    the tests, which close a good many windows.
76. **About is given the app's icon** by name: the one macOS has for the app's own
    file, which is what the Dock shows. Left to itself the system's About window
    showed a blank icon in 0.3 (the maintainer's report; why was not found). **Not
    seen by this session**: no test can open it.
77. **Earlier Versions is a blue button** (`#2978db`, white words, the picture a
    song has in the list), **and the top left has a title**, "WORKSPACE OR
    PROPRESENTER FOLDER" (the maintainer's words). The blue is mine: the colour
    furthest from the orange that in the same header says which editor is shown.
    It is drawn by the app (`FilledButton`), because the system's coloured button
    is grey in any window that has not the keyboard, and in every picture a test
    draws.
78. **1.0.0 is GitHub's "Latest" with only the Mac's file in it**, until the Windows
    app adds its own. The maintainer asked for the release and for it to be the
    latest; its text says at the top where Windows' downloads are meanwhile. **To
    say to the maintainer**: that this is so, and that it can be made a pre-release
    again until both are there. (Overtaken on 2026-10-14: from then on the first
    half of a number is made a pre-release, decision 92.)
79. **A text box's other ranges go with the words** when a line of chords alone in
    it is made longer or shorter (`spec/01`, "The box's other ranges"; the
    maintainer's, 2026-10-13: "let's copy simple presenter for now"). What was
    open under decision 71. Each end of each range that is no chord is moved as
    Simple Presenter's `placeAfter` moves it (`RTFWords.placeAfter`, and
    `moveOtherRanges` in `Writing.swift`, asked by `OpenSong` only where the line's
    RTF is changed). **A guess at what ProPresenter does**, as the page says. No
    vector holds it: the cases are the reference's own test's, taken over as they
    stand, and a vector is asked for in `testdata/README.md`. On Chords With No
    Words, every box of which is in capitals over all its words, a chord more,
    fewer or none on each line leaves the box in capitals over all its words
    still; left out, that fails 52 times over. **Up to 1.0.0 this app left such a
    range as it was**, so a song whose line of chords alone it made shorter has a
    range that ends past its words. Nothing looks for that or mends it; what
    ProPresenter makes of it is not known. `AGENTS.md` said that a write changes,
    for such a line, "the characters they hang on" and nothing else; a few words
    were added to it so that it is still true.
80. **The edge between the lists and the song starts half way between its least and
    its most, and is the app's own** (`spec/06`, rule 6a). The maintainer asked
    where it starts and for "about half way between min and max": 345, of 210 and
    480. **What was found on asking the window itself**: the system's split view
    (`HSplitView`) had been asked to start the lists 270 wide since M3 and started
    them **480** wide, in a window of the usual size. It gives each side half the
    window, up to the lists' most, whatever it is asked; it changes the lists'
    width as the window's changes; and what it is under SwiftUI will not have its
    way of holding a width changed (it raises an error, which in the app would be
    a crash at the start). So the edge is this app's, as the bar between the two
    lists always was: a line, a few points beside it to drag it by, and the width
    kept by the model. **Mine, with it**: the lists keep their width as the window
    is resized; where the edge is left is remembered (`settings.json`,
    `listsWidth`), as the bar's place is; and clicked twice it goes back to 345.
    What is dragged lies over the lists' side of the line and none of the song's:
    the sheet says what shape the pointer is at every move, and would take the
    edge's away. **By hand only**: the dragging, and the pointer's shape over it.
81. **The line of hints, tried, and changed the same day** (`spec/03`, rules 53a to
    53g; still marked there as being tried on the Mac). The maintainer, of 1.0.0:
    "love how they appear at the right time - lets drop the randomly timed tips",
    and gave the words for the copying: "Press ⌘-c to copy all chords on the
    highlighted line. Press ⌘-v to paste previously copied chords to the
    highlighted line", with " (Replaces existing chords)" after it "when the target
    line has any chords". Built as said. **Mine, all to be overruled**:
    - the keys are drawn as the line draws every key, ⌘C and ⌘V in their small
      boxes, and not written "⌘-c";
    - with nobody at the sheet the line says one thing that never changes ("Point
      at a word or a chord, and this line says what the keys do there."), and
      says it whatever is held: the copying is of the highlighted line, and there
      is none then;
    - where a whole verse was copied, the second sentence ends "and the lines
      after it", since that is where they go; and "(Replaces…)" goes by every
      line the paste would reach (`ChordSheet.pasteWouldReplace`), a line that
      would be passed over not counting. With nothing copied yet it goes by the
      highlighted line alone;
    - Shift+Command+C is said nowhere on the line now (the guide has it);
    - the two sentences are never left off for want of room: they go one under
      the other, the line being two lines while they are said, and then smaller;
    - Command+C and Command+V do the same with Option held as well. Option is
      what brings the hint up, and until now a key pressed with it still down did
      nothing at all.
82. **The highlighted line** (`spec/03`, rule 53f; the maintainer's: "highlight the
    hovered/active line of text ... by changing color - default orange (a new
    setting)"). The words of the line the person is at are drawn in the sheet's
    orange, `#ff8a1f`, or the colour chosen under Settings > Chords, "The
    highlighted line". **Mine**: which orange; that while a chord is typed it is
    the line of the typing, wherever the pointer has gone; that a line of chords
    alone, having no words, has its rule in the colour and twice as thick; and
    where the setting stands, between the two colours of chords. One function
    says which line it is (`SheetHints.line`), for the drawing and for the hint.
    **The guide's three pictures were made again**: each is of someone at the
    sheet, so each has its line in orange now (`spec/13`, rule 15). Its words were
    left alone: they are shared with Windows, which has no such line yet.
83. **Two sheets on one model never agree, and the tests stopped on it.** Each
    sheet's view tells the model what is going on in itself. The picture tests
    left every window they drew open, on one model; with the pointer put over one
    of them and not another, each told the model otherwise than the last, the
    model had every window drawn again, and the tests ran for ten minutes at full
    speed until stopped. The app has one sheet and is not touched by it; the
    tests now close each window when it is drawn (`PictureTests.draw`), and the
    sheet's view says why at `tell()`.
84. **The window was slow, and it was the song's header** (`spec/06`, rule 6e, new;
    "What made the window slow", below, has what was measured and how). The
    maintainer, of 1.0.1: "the insert cursor is pretty laggy now", the same for a
    chord being dragged, and of every build that the edge between the two parts
    was slow to drag with a song open and smooth with none. One cause for all of
    it. What was done:
    - **the header is laid out once** (`SongHeader.swift`): its parts are each made
      once, and where they go is arithmetic (`HeaderArrangement`), where it was
      three arrangements for SwiftUI to choose from. Its three arrangements are
      what they were, but for this: Earlier Versions is no longer squeezed to its
      picture alone where the row is tight, which it was, against the rule that
      nothing but the name is cut short;
    - **the strip of the key's chords** and **the line of hints** the same: one
      arrangement each, what fits worked out (`SheetStrip.saysNoKey`, `HintFit`);
    - **the header and the quiet line are views of their own**, so that a change
      to one is not the whole of the song's part worked out again;
    - **the sheet's view is no longer brought up to date by its own saying** of
      what is going on (`LibraryModel.sheetIs` has how that came about);
    - **the sheet measures its lines once** for a song as it is, and a sheet that
      is only made wider or narrower puts its slides in rows again and measures
      nothing.
    **Tried and taken out again**, each measured: drawing only the rows of slides
    the pointer had left and come to (the system then copies its picture of the
    whole sheet first, which took longer than drawing it); keeping drawn only what
    is seen of the sheet (no difference that could be measured off the screen);
    and laying the hints over a room that never changes (two lines of them were
    then hidden behind the sheet).
85. **The edge between the lists and the song is a bar that is seen, and AppKit's**
    (`spec/06`, rule 6a; `EdgeGrip`). The maintainer, of the hairline of 1.0.1: "a
    bit hard to grab ... it might be too narrow? or take too long to show the mouse
    cursor change?". Both: it was five points of nothing to be seen, and SwiftUI
    said the pointer was over it a moment late. Now it is eight points wide, of
    the colour of the bar between the two lists, with a grip in its middle; every
    point of it takes the mouse; and the pointer's shape over it is the system's
    to keep (a cursor rectangle), which changes as the pointer crosses its edge.
    It lies over neither part of the window. **Mine**: that it is a bar and not a
    wider invisible strip beside a hairline, and its width. The maintainer offered
    the system's own divider back; it was not taken up, because the system's
    cannot be told where to start (decision 80), and what made dragging slow was
    not the divider. **By hand only**: how it is to grab.
86. **The tips refined** (`spec/03`, rules 53b, 53e, 53g, 53h; all the maintainer's,
    2026-10-14, numbered as asked):
    1. On every word of every line the line said to hold Option, to see how a
       line's chords are copied. Gone. What Option does is said on a chord, in
       the maintainer's words: "You can drag this chord to move it anywhere. (Hold
       Option to drag a copy)", in place of "drag it to another word".
    2. "Let's sentence case the tips": each begins with a capital. **Mine**: that
       the words are otherwise as they were, what a key does said after the key
       ("Takes this chord off"), and not made whole sentences ("Press ⌫ to take
       this chord off."), which would have been half as long again.
    3. Over the key's chords at the top of the sheet: "You can drag these down to
       the text on any slide". **The "to" is mine**: it was asked for as "down the
       text". And **mine**: while one of them is being carried over a word the
       line says "Let go on a word to put this chord there" (it said nothing of it
       before), and what tells the line that the pointer is over them is a
       tracking area of AppKit's (`PointerWatch`), under all seven.
    4. The copying is brought up by Command, and no other key. **Mine, with it**:
       Command+C and Command+V no longer do anything with Option held as well,
       which they did for a day because Option then brought the hint up; and that
       nothing on the line now says to hold Command.
    5. Blue, to be seen; the keys brighter than the ground; and the line lights up
       when what it says changes, and fades over a second and a half. **Mine**:
       which blues (`HintLook`: a light blue where the system is light, a deeper
       one where it is dark, and the keys white on the one and bright blue on the
       other), how long each part of the lighting up takes, and that it does not
       light up for whoever has asked the system for less motion. It is SwiftUI's
       own animation of how much of the lit colour shows, which costs the main
       thread a little over a thousandth of a second each time the screen is
       drawn while it fades (`SpeedTests` holds it); nothing is laid out by it.
87. **A chord's name on the sheet is orange as it comes** (`spec/06`, rule 20a; the
    maintainer's: "lets make the default chord color orange now"). The sheet's own
    orange, `#ff8a1f`: which orange is mine. It is the colour the highlighted
    line's words also come in, so on that line the chords and the words are one
    colour; either can be changed in the settings. The guide's three pictures were
    made again for it. Simple Presenter's is still the pale `#ffd9a8`.
88. **What a paste keeps: asked by the maintainer, and theirs to decide** (`spec/03`,
    under rule 52b). "Copy and paste sometimes seems the drop the chords in
    different place (text ranges) ... the positions all are wrong (they seem to be
    reduced in offset)". Looked into on copies of their two files of that hymn,
    read for the shapes of their lines and where their chords are and for nothing
    else: what they saw is rules 48 and 50 doing what they say. A chord is copied
    by which WORD it is on, and goes onto the same numbered word. Between
    "Amazing grace how sweet the sound" (letters 1, 8, 18, 28) and "I once was
    lost but now am found" that is letters 0, 2, 11, 20: each a word sooner than
    it is sung, "Amazing" being one word where the other line has three. The
    places along the line, which is what was expected, would have been no nearer
    for two of the four. `ChordSheetTests`, "What a paste keeps", has the
    example. **Nothing was changed**: it is a rule the vectors hold, made from
    Simple Presenter, so a change is that app's first and then both of these.
89. **The README has a screenshot of the Mac app**, after its first sentence, with
    room beside it for Windows' (the maintainer's, 2026-10-14: "two side by side app
    screenshots"). The picture is the maintainer's own, of 1.0.3 with a song of
    theirs open, in the repository as it was given (`pictures/screenshot-mac.png`).
    Its words are a hymn's long out of copyright; beside them it shows the names of
    three other songs of that library, which was said to the maintainer. The
    Windows one is that session's to add: the line for it is in the README, in a
    comment, to be taken out of it. **The picture is of 1.0.3, and what the page
    offers to download is 1.0.0**, which has not the blue line of hints nor the
    orange chords: said to the maintainer too. (No longer so: 1.0.3 was given out
    the same day, decision 91.) **Moved, 2026-10-14**: each app's picture is under
    its own link in "Try it" now, and no longer under the page's first sentence
    (decision 92).
90. **The line of hints is agreed** (the maintainer, 2026-10-14, of 1.0.3: "tips are
    fine"). `spec/03` no longer says it is being tried and that the others are to
    wait: Windows is to build it as it is there, the highlighted line and its
    colour with it. What a paste keeps (decision 88) is still open: "I'll think
    about paste rule". A handover for the Windows session was written for the
    maintainer to pass on, of everything since that app's 0.2; it is not in the
    repository, which already says all of it.
91. **1.0.3 is given out** (the maintainer, 2026-10-14: "Okay let's release 1.0.3"),
    as the Mac's half of the release `v1.0.3`, which is GitHub's "Latest" now;
    1.0.0's page is still there under it. 1.0.1 and 1.0.2 were builds to try and
    were never given out: what they brought went out in this. "The disk image" has
    what was checked. **Mine, and the maintainer's to overrule**:
    - **the image is not, byte for byte, the one that was tried; the app in it
      is.** The read-me that goes into the image still said "An icon" under "Not
      in it yet", as it had in the images of 0.3 and 1.0.0, the app having had its
      icon since 0.3: found on reading the image through before it went out. The
      line was taken out and the image made again by the script. The app in it
      was held against the app in the image the maintainer tried: the same, file
      for file, and the same number on its seal. The image that was tried is kept
      beside it, named as not given out;
    - **the tag is on the commit of that read-me**, and not on "Version 1.0.3":
      three commits on, none of which changes a file the app is built from. The
      tag was pushed by itself first, so that GitHub's Mac build ran on that very
      commit before anything was put on the page. (Moved since, to the commit
      Windows' file was built from: decision 92);
    - **the release's text says "The Windows app's is to be added"**, and not a
      Windows build "of 1.0.3": the number that app is next built under is the
      maintainer's to tell its session. (It was 1.0.3, and the text says now that
      both are here: decision 92);
    - **it says what changed in a song's file since 1.0.0** (decision 79), and
      that a song in which 1.0.0 took a chord off a line of chords alone is not
      looked for or mended;
    - **1.0.0's page was left as it is.** Its text still says that the Windows
      build of 1.0.0 is to be added, which will not now be so under that number. A
      release is changed on the maintainer's word: said to them. (Put right since,
      at their word to the Windows app's session: decision 92);
    - **the README's sentences round "Try it"** no longer name a number for the
      Windows app that it has not got, and say that the release GitHub calls
      "Latest" is to hold both downloads and so far has the Mac's. (It has both
      since: decision 92.)
92. **How a new number is given out from now on, and what became of the release
    of 1.0.3 after the Mac's half** (the maintainer, 2026-10-14, to this session:
    "I did some work on the windows version.. here is some handvoer for you to
    align"; "Pull, then run git fetch --tags --force: the tag v1.0.3 was moved.
    Read the new section "Giving a build out" in AGENTS.md, and
    spec/10-platforms.md, "Getting it to other people". The README's pictures are
    now under each app's link in "Try it"."). It was agreed between the maintainer
    and the Windows app's session, which wrote it into `spec/10`, `AGENTS.md` and
    `spec/11`, and into the paragraph on a release in `mac/README.md` at the
    maintainer's asking; `windows/NOTES.md`, decisions 141 to 143, has their
    words. **The rule: the release GitHub calls "Latest" always has both files.**
    For this app:
    - **when the Mac comes first to a number**, its release is made as a
      pre-release and not as "Latest" (`gh release create v<number> <image>
      --prerelease --latest=false --verify-tag`), and its text says that Windows'
      is to come. "Latest" stays on the release before, which has both files.
      1.0.0 and 1.0.3 were not done so (decisions 78 and 91): each was "Latest"
      with the Mac's file alone, 1.0.3 for a day;
    - **when the Mac comes second**, to a number Windows has made a pre-release
      of, it is built under that number whether or not anything of its own has
      changed. Then, in this order: the image is added (`gh release upload`); the
      Mac's part is put into the text, Windows' being left as it came but for the
      words that said the Mac's was to come; the tag is moved to the commit the
      image was built from, by `git tag -f` and one forced push of that tag and
      never by deleting it, and only if nothing the Windows app is built from
      differs between the two commits (`git diff --stat <theirs> <ours>` of
      `windows/` and of `proto`, its notes and its read-me apart: if something
      does, the tag is left and the maintainer told); and the release is made no
      pre-release and "Latest" (`gh release edit v<number> --prerelease=false
      --latest`);
    - **a session starts with `git fetch --tags --force`** and a look at the
      Releases page. A pre-release with Windows' file alone in it is a number
      that waits for the Mac, and is said to the maintainer. Tags are never
      pushed all together;
    - each half is still on the maintainer's word to the session that does it,
      and each brings its own part of "Try it" up to date in the same push.
    **What was done to `v1.0.3` after the Mac's half**, by the Windows app's
    session at the maintainer's word, as it was found from here that day:
    - **Windows' file is in it** (`Pro7ChordEditor-windows-1.0.3.zip`), and it is
      "Latest" still and no pre-release. The Mac's image is as it was sent:
      GitHub gives the same sum for it (`7faaf6dc…bfab`) and the same length, and
      its address answers. Of the text the Mac's half sent, every line is on the
      page but the first sentence, which said that Windows' was to be added and
      says now that both are here;
    - **the tag is on `b376e7f`**, the commit Windows' file was built from, and no
      longer on `80497fa`. Between the two nothing the Mac app is built from
      differs: under `mac/` only these notes, `proto` is at the same commit, and
      nothing under `spec/`, `testdata/` or `tools/`. So the source at the tag is
      still what the image was made from. This clone had the tag where it was
      until it was fetched with `--force`;
    - **1.0.0's page no longer says that a Windows build of 1.0.0 is to be
      added** (the maintainer: "Just remove incorrect text"), which was the
      first of the two things decision 91 left to ask;
    - **the README's two pictures are under each app's link in "Try it"**, each
      as wide as the page (the maintainer, of the two side by side under the
      first sentence: "not working well visually"). The table of two rows is two
      headed parts, and the Mac's gives the same file as before.
    **Mine**: the paragraph on a release in `mac/README.md` was read and is as the
    rule has it; the line in it that said which session had brought it up to
    date was taken out, a read-me being no place for it, and is said here
    instead. **Still to ask**: "An early build", the second line of the read-me
    in the image. The read-me in Windows' zip says the same. (Answered the same
    day: it stays, decision 94.)
93. **Three things the Windows app's session had found of this app are put
    right** (2026-10-14). Its notes had them "for the maintainer to pass on to the
    Mac's session", from 2026-10-12, and they were not passed on: found on
    reading `windows/NOTES.md` when the maintainer said to align (decision 92).
    **Mine to take up unasked, and the maintainer's to overrule**: each is this
    app doing other than `spec/` has it, none is a new rule, and none changes how
    a song's file is written.
    - **Putting back what the file already is, is nothing to do** (`spec/07`,
      rule 21; their decision 36). The song did nothing, rightly, but the
      window's model went on as if it had: the line under the song said "Put
      back the version of ..." and offered Undo, of the newest copy there
      happened to be, which was not made for it. Pressed, that Undo put that copy
      back, and the song's file was then something the line had not said.
      (Nothing could be lost by it: the file as it is, is copied before any
      putting back.) Now the panel is put away and nothing else is done, as on
      Windows. `EarlierVersionsTests` has it, a test that failed five ways
      before;
    - **a key that is not one of the twenty-six is first in its picker**
      (`spec/02`, "Keys"; their decision 55). A song in C# major showed nothing
      at all in either picker of its header, a picker whose choice is not in its
      list being blank: seen in a picture before anything was changed, and "C#"
      in both after. `LibraryModel.keysListed(for:)`, which is their
      `KeysListedFor`. The strip of the key's seven chords was right all along;
    - **what a ChordPro directive's name starts with is asked of its units**
      (`spec/02`, "To the letter"; their "At M1"). Swift's `hasPrefix` goes by
      whole characters, so a name with an accent laid over the underline of
      `start_of_`, or over the "o" of a three-unit `eo` name, was not taken for
      one, as the reference takes it. Nothing writes such a name, and nothing in
      the window reads a ChordPro file yet (M9).
    **Two more that their notes have, and nothing done:**
    - "the Mac's port writes two bytes more": a text box with no settings written
      at all, asked to have no chords, is given an empty set of settings, and
      that is a change saved. **The reference does the same**: `writeChords` in
      `proconvert.cpp` reaches for the settings before anything else, which
      makes them, and a change is whatever leaves the slide's bytes other than
      they were. So here it is the Windows app that parts from the reference.
      It can be come to from the window, by pasting a copied line that had no
      chords onto a line of such a text box; no file to hand has one. Said to
      the maintainer, for that session or for nobody;
    - their decision 81, a chord that is on no character handed to both editors
      where it will be written, "for the maintainer to say, and for the Mac's
      text": it belongs with mending, and is to be looked at when M8 is begun.
    **A session starts by reading what `windows/NOTES.md` has for this one**
    (look for "pass on" and "the Mac's"), as it reads `spec/`: the maintainer
    passes on what they remember to, and these had waited two days.
    All 382 tests pass. No build was made.
94. **The read-me still says "An early build", at 1.0** (the maintainer,
    2026-10-14, asked whether a 1.0 is still to say that of itself: "I'll leave
    1.0 as "an early build" (not best practice - but a choice I'll keep until
    I've used it for a while and fixe a few bugs)"). So the second line of the
    read-me in the image stays as it is until they say otherwise, and it is not
    to be asked again at each release. It is in `spec/10`, "Getting it to other
    people", for the Windows app's session, whose zip says the same. That was
    the second of the two things decision 91 left to ask, and the last.
95. **Sharps and flats drawn as ♯ and ♭** (2026-10-14). The maintainer asked:
    "Could we *smartly* render # as ♯ and b as ♭ when it is clear that is it a
    shartp or flat note", on the sheet, the strip, the box and as it is typed,
    "careful not to bother the user", with "no performance hit on a page with LOTs
    of these chars (perhaps a font glyph is best for that??)", and in a way the
    Windows app can do too. Shown a proposal, they said to hold it; then: "let's do
    it for the GUI chord editor..(just a visual skin - not changing the underlying
    data) and leave chordpro text unchanged/native/correct. and yes - a setting to
    optional turn it off (default to on)". `spec/02`, "Sharps and flats drawn as
    signs"; `spec/03`, rules 10a and 30a; `spec/06`, rules 12 and 20a.
    - **Mine, in the proposal they took**: which `#` and `b` are clearly a sign.
      Only in a name that is a chord and whose root is a capital: the root's and
      the bass's, and between them one with a digit after it. The first draft
      gave small-letter roots their signs too, and drew `b3/♭5` for a chord in
      numbers that the vectors have. Over the 460 names with a `#` or `b` in
      `testdata`, 263 get signs, and none looked wrong. Also mine: the two key
      pickers drawn the same, and the switch's words and its three sample bubbles.
    - **No font was needed.** The system font has both signs (checked: drawn
      with its own, nothing standing in). SF Mono, the ChordPro text's, has
      neither: Menlo stands in, thin, and 0.3 of a point narrower, which is one
      more reason the text is left as it is. And a font could not tell a chord
      from a word: it would draw the "b" of "Abide" as a flat.
    - **How it is drawn.** `SheetMetrics` has the switch, and makes each name
      into a line once, drawn, and measures the bubble from that, so the layout
      fits what is drawn. The strip, the key pickers and the settings draw
      `LibraryModel.drawn`. The box: `ChordSheet.signs`, `inTheBox`, and `type`,
      which reads what the box shows with `withoutSigns` first (rule 30a). What
      is carried from the strip, copied and kept is the name itself; the tests
      read the file back and find `#` and `b`.
    - **Speed, with the signs on** (`SpeedTests`): the pointer usually 1.4
      thousandths of a second, nine times in ten under 6.6, at worst 13.2; a
      chord dragged, at worst 8.5; the edge with the sheet shown, usually 14.4,
      and with the ChordPro text 10.7. The same as "Now" in "What made the window
      slow".
    - **The guide's three pictures were drawn again**: the strip of a song in G
      has F♯dim, and what is offered as D is typed has Dm7♭5. Looked at.
    - **Found on the way, and left**: in `PictureTests` every picture is of a
      window of its own, and the box of the one before, giving up the keyboard
      as its window goes, keeps what was typed (rule 39). So "18 typing, the
      second offered picked" shows no box, and did before this work too
      (drawn from the commit before it). Picture 68 types again instead.
    All 390 tests pass and the app builds. No build was made to try.
96. **1.0.4 is given out, its first half** (the maintainer, 2026-10-14, having
    looked at the pictures of the signs: "Looks good. lets bump version, commit,
    push, and do a new release"). The Mac is the first to the number, so, as
    decision 92 has it: the release `v1.0.4` is a pre-release and not "Latest",
    with the Mac's image alone, and its text says the Windows app's 1.0.4 is to be
    added. 1.0.3, with both files, stays "Latest". The tag is on "Version 1.0.4"
    (`09fb480`), which the image was made from; it was pushed by itself once both
    of GitHub's builds of the branch had passed, and both passed on it too. The
    README's Mac part is 1.0.4's, and the sentence over the two parts says that
    Windows' is 1.0.3 until its 1.0.4 is added. What is new in it: the signs
    (decision 95) and the three things of decision 93. **Mine**: the number, 1.0.4,
    as "bump" has been read each time; and the release's text. **Waiting**: the
    Windows app's half. Its session builds 1.0.4, adds its zip, and moves the tag to
    its own commit once `git diff --stat 09fb480 <its commit>` shows nothing changed
    in `mac/` but `NOTES.md` and `README.md`, nor in `proto`; then makes the release
    "Latest".

97. **Changes can be allowed while ProPresenter is running** (the maintainer,
    2026-10-17: "Let's add an "advanced" setting to enable editing while
    ProPresenter is open (throw in a warning about potential data corruption if
    propresenter this pro7chordediotr try to update the same file"). `spec/06`,
    rules 20 and 20a, written first and pushed; the other pages that said nothing
    is saved while ProPresenter runs, and that line of `AGENTS.md`, point to rule 20
    now. How it is built:
    - **The song's own last check asks it first.** `Safeguards` has
      `savesWhileProPresenterRuns`, no unless told; `OpenSong.maySave` refuses for
      ProPresenter only when that says no. The window's model hands it a box it
      holds, so that a song opened before the switch was turned is asked what it is
      now. Everything else in `maySave` and after it is as it was: the copy first,
      the write beside the file, and the refusal over a file something else has
      changed, which is what will tell of ProPresenter having saved the same song.
    - **The window**: `canChange` is true while ProPresenter runs if the switch is
      on, which unbars the text, the sheet, the keys and Earlier Versions; Command+S
      and the words where a Save button would be follow `canChange`. The band stays
      while ProPresenter runs, and says "ProPresenter is running. Changes here are
      saved all the same (Settings, Advanced): leave alone any song it has open."
      (`LibraryModel.band`). Turning it on takes away a refusal that was for
      ProPresenter.
    - **The settings**: a third tab, Advanced, with the one switch, and the warning
      under it, always. Turning it on asks first (`AdvancedSettings.ask`, an alert
      on the settings window): "Cancel" is added first and so is what Esc presses,
      and Return presses nothing (SEEN: an alert gives Return to its first button
      unless that is called "Cancel", which it gives Esc), so "Allow Changes" has
      to be clicked. Off asks nothing. Kept in `settings.json` only when on.
    - **Mine, and the maintainer's to overrule**: the name of the switch, the
      words of the warning and of the band, that it asks before it is turned on,
      that Enter does not allow, and that it is off as the app comes. The read-me in
      the image and `mac/README.md` say it now too.
    - **Not told apart**: a song ProPresenter may have open from one it cannot have,
      which is what "Asked for by the maintainer, for later" thought of. The switch
      is for everything; the careful way is still open there.
    - Not tried with ProPresenter itself, by anyone: what it does with a song
      changed under it is still **a guess** (rule 16). The tests stand in for
      ProPresenter, as they always have.
    393 tests pass (three new: the song's check, the window's model, and pictures
    69 to 71, looked at), and the app builds.

## How it is laid out, and why

```
mac/
  Pro7ChordEditor.xcodeproj   the app target, and nothing else
  App/                        the app: SwiftUI, thin
  Core/                       one Swift package: everything with no window in it
    Sources/ChordRules/         the rules (M1)                  no protobuf, no AppKit
    Sources/ProFormat/          the generated reader of a .pro  nothing by hand
    Sources/Song/               the song, the library; the copies, the checks (M2 on)
    Sources/AppViews/           the window: its model and its views (M3 on)
    Tests/TestData/             how tests find testdata/
    Tests/ChordRulesTests/      held to testdata/vectors
    Tests/SongTests/            held to testdata/files
    Tests/AppViewsTests/        the window's model, and pictures of the window
    generate-proto.sh           what made Sources/ProFormat/Generated
```

- **One package, three libraries**, not three packages: one `swift test`, one
  `Package.resolved`, and a target still cannot import what it does not name.
- **The generated code is kept in the repository.** swift-protobuf's build plug-in
  only takes `.proto` files inside the package, and these are in a submodule outside
  it. Kept, the package builds from a clone with no submodule and no `protoc`.
  `Generated/MadeFrom.swift` says what it was made from. **Run
  `mac/Core/generate-proto.sh` again whenever the `proto/` submodule is moved on**;
  a test notices if the submodule names another version of ProPresenter than the
  code was made from (see "Left rough" for what it does not notice).
- **Only what a presentation needs is generated**: `presentation.proto` and what it
  imports, 32 files of the 148. For another kind of file, add its `.proto` to `ROOTS`
  in the script.
- **Tests find `testdata/` by walking up from their own source file.** A package
  cannot take resources from outside itself.
- **The Xcode project was written by hand**, not made by a tool, so that there is no
  tool to depend on. Its `App` folder is one that follows the disk (a "synchronized"
  folder, Xcode 16 and later): a file added to `mac/App` is in the app without the
  project being touched. `Info.plist` is named in the project as not a resource.
- **The package is in the project as a folder**, not as a package the project refers
  to. Both build the app. Only the first lets the scheme run the package's tests
  (Command+U in Xcode): with a reference, `xcodebuild test` answered "There are no
  test bundles available to test".
- **The app target** is Swift 6, with the main actor as the default for its own code,
  as Xcode 26 sets a new app up. The package is plain Swift 6.
- **`com.greyshirtguy.Pro7ChordEditor`** is the app's bundle identifier. It is my
  choice, from the GitHub name, and is the maintainer's to change. Better changed
  before M3 than after: macOS keeps what the person has allowed the app, and the
  app's settings, under it.
- **The app is signed "to run locally"** (ad hoc), which needs no account. The
  hardened runtime is switched on in the project and Xcode leaves it off for an ad hoc
  signature, saying so in the build log; it comes on by itself when the app is signed
  with a Developer ID at M10.

### The rules (M1)

`Core/Sources/ChordRules/Chords.swift` is the way in and lists the files. What to know
before changing any of it:

- **The rules stand under `Chords`**, as the C++ has them under `chords::`, with the
  names they have there: `Chords.transposed("C#m", from: "E", to: "Db")`. Three things
  that are not from `chords.cpp` have names of their own: `RTFWords`, `KeyFit`,
  `OldEditor`.
- **Inside, everything is `[UInt16]`**: a String is turned into its UTF-16 units on the
  way in and back on the way out, and `Units.swift` has the few operations, each named
  for the QString function it stands in for. Nothing uses String's `count`, `hasPrefix`
  or `==` on text, nor Foundation's trimming or character sets. Keep it so.
- **`Words`** is the type for a text box's words, and a place is an `Int` into its
  `units`. Names of chords and keys, and ChordPro text as typed, are plain Strings.
- **Tests**: one file of tests for each vector file, reading every case
  (`Tests/ChordRulesTests/Vectors.swift` says how they compare and report). Tests
  marked "Mine, not a vector" are just that.

### The song (M2)

`Core/Sources/Song/Song.swift` is the way in and lists the files. What to know before
changing any of it:

- **Everything that may be asked ends in `OpenSong.save`.** A new state of the whole
  message is made beside the one in memory; the safeguards are asked; the file is
  written beside itself, pushed onto the disk, swapped in and read back; and only then
  does the new state become the song. So a refusal or a failure anywhere leaves the
  song in memory as it was, with nothing to put back. Keep it so: do not change
  `presentation` anywhere else.
- **The order in `save`** is the order in `spec/05`, rule 10: no safeguards; ProPresenter
  running; the file changed by something else (size, time AND bytes); the copy; the
  write. A refusal before the copy leaves no copy.
- **A step of undo** holds the part that changed, before and after: one cue, or
  `music` (which a song may not have at all). 200 steps are kept, as the reference keeps.
- **Slides do not change while a song is open**, so they are worked out once. The text
  boxes are read from the message each time they are asked for. If that is ever slow
  (it parses every box's RTF), keep them and throw them away in `save`.
- **Tests** work in `Workbench`: a folder of its own under the system's temporary
  folder, with copies of the test files and safeguards a test can set. Nothing is ever
  saved into `testdata/`.

### The library and the window (M3)

- **In `Song`, with no window**: `Places.swift` (finding), `Listing.swift` (libraries,
  songs, search), `Marks.swift`, `AppData.swift` (the app's own folder), `SongText.swift`
  (a song as lines of text: what is shown now, and what the ChordPro editor will work
  on at M5).
- **In `AppViews`**: `LibraryModel` is everything the window shows and does, with no
  drawing in it, and is tested so. The views only show it. `LibraryModel`, the finder
  and the song are each handed what they would otherwise ask the Mac for, so that
  tests hand them a made-up Mac (`Tests/AppViewsTests/MadeUpMac.swift`).
- **To see the window**: `PRO7_PICTURES=/some/folder swift test --package-path mac/Core
  --filter PictureTests`, then open the PNGs. Do this after any change to how
  something is drawn, and say what was seen. Without the variable they go to
  `Pro7ChordEditorPictures` in the system's temporary folder.
- **A song is opened with no safeguards at M3**, so it cannot be changed. M5 hands it
  real ones: `ProPresenterApp.isRunning`, and the copies of M4.

### The window's two lists (after M6)

- `LibraryWindow.swift`: `ListsPane` is the left of the window, `PlacesPane` (the
  place and its libraries) over `SongsPane` (the search, the choice by mark, the
  songs), with the bar between them drawn and dragged by `ListsPane` itself; and
  `ListsLook` has the three greys and `top(share:of:)`, which is all the arithmetic
  of the bar. The share is `LibraryModel.listsSplit`, kept in the app's settings.
- SwiftUI's own `VSplitView` was not used for the bar: its divider cannot be drawn
  on, and where it is left is not ours to keep or to put back.
- `ListIcons.swift`: the outline is one path, read once from the picture file's own
  words for it (`PresentationGlyph.path(of:)`), and both icons are made of it.

### The copies (M4)

- **In `Song`**: `Copies.swift` is all of it: keeping, listing, thinning, saying when.
  `OpenSong.putBack` is the putting back, and is a save like any other: it goes
  through the same three questions (`maySave`) and writes with the same `SongFile.write`.
- **In `AppViews`**: `LibraryModel` has the panel's state (`earlier`), `putBackPicked`
  and `takeBack`; `EarlierVersionsPanel` shows it. `AllCopiesModel` and `AllCopiesView`
  are the settings (Command+comma), wired in `App/Pro7ChordEditorApp.swift`.
- **Only `Copies.thin` deletes anything**, only files in a song's folder of copies that
  are named as copies, and never the oldest. If that is ever changed, look hard.
- **Tests make history** with `MadeUpMac.change(_:daysAgo:)`, which changes a song as
  the app would, with a copy kept first, dated then.

### The ChordPro editor (M5)

- **In `Song`, with no window**: `ChordProEditor.swift` is every rule of `spec/04`:
  what may be typed (`take`, `allows`), the help with brackets (`helpWith…`), what a
  pause saves (`save`), the colours, which slide a place is on. It works on the lines
  `SongText` makes, and counts in UTF-16 units throughout.
- **In `AppViews`**: `ChordProView.swift` has `ChordProText`, the system's text view
  and what stands between it and the model, made apart from SwiftUI so that a test
  can type into it; and `ChordProView`, the few lines that put it in the window.
  `LibraryModel` says when to save (`textMayBecome`, `settle`) and sets the keys.
- **How a key press goes**: the text view asks before any change
  (`shouldChangeTextIn`) -> `LibraryModel.textMayBecome` -> `ChordProEditor.take`. Yes,
  and the change happens and a save is set for 700 ms on; no, and it never happens.
  The save is `LibraryModel.settle` -> `ChordProEditor.save` -> `OpenSong`.
- **The text is set from the model only when `ChordProShown.version` goes up**: a
  song opened, an earlier version put back, a save refused. Never because of what was
  typed, or the caret would jump from under the person typing.
- **`settle()` is called before anything else is done to a song or the song is
  left**, and answers whether what was typed is saved. If a save was refused, the
  thing asked for is not done, so that the sentence is seen.
- **Tests type into the real text view**: `insertText`, `doCommand(by:)` for the two
  delete keys, `readSelection(from:)` with a pasteboard of the test's own (never the
  person's), and `undo(_:)`.
- **When M7 is built**: `ChordProEditor.canBeSavedYet` goes, with the one place that
  asks it and the one line in `save` marked UNTIL M7; and `OpenSong.setChordsAlone`
  loses its two refusals.
- **Still to do from M6**: `slide(at:)` and `start(ofSlide:)`, which nothing uses yet,
  are what would tie the caret to the sheet's picked slide (`spec/04`, rule 21).

### The chord sheet (M6)

- **In `Song`, with no window**: `ChordSheet.swift` is the sheet's model, the
  reference's functions under their own names: the rows, the spot, `step`, `put`,
  `take`, `move`, `copy`, `paste`, the typing (`startTyping`, `type`, `press`,
  `finishTyping`, `settle`, `pick`). It asks the song through `SheetSong`, which an
  `OpenSong` is, and reads the song again after every ask. `ChordLayout.swift` is
  where the bubbles of a line stand.
- **What goes by the screen is handed a `SheetMeasure`**: which place is under the
  pointer (`placeAt`), where a place is (`xOf`), Up and Down (`stepLine`). The window
  hands Core Text's widths; the tests hand letters ten wide, as the vectors were made.
- **In `AppViews`**: `ChordSheetView.swift`. `SheetCanvas` is the one view that draws
  the song and takes the pointer and the keys. `SheetMetrics(scale:)` has everything
  that grows and shrinks with the zoom (the two fonts, a line's height, the room in a
  card) and measures the lines; `SheetLook` has the colours and what does not zoom (a
  frame's 4, its foot, the gaps). `SheetEntry` is the bubble a chord is typed in, a
  real `NSTextField` with what is offered drawn over it; `SheetStrip` is the key's
  seven chords and the two zoom buttons, in SwiftUI.
- **The grid** is worked out in `SheetCanvas.layOut`: `cells`, one for each slide (its
  frame, its card, its colour, what its foot says, its row and column), and `laid`,
  one for each of the model's rows (which cell, where its words start, where its row
  of chords is). The model's group rows are given no place: a group's name is on its
  first slide's frame. A point is turned into a line by `line(at:)` and a line into
  the one under or over it by `lineBeside`, which is what Down and Up go by: the
  model's own `stepLine` goes by the order of the song, which is right for one column
  only, and is kept for the vectors' sake and its test.
- **Laid out again** when the rows change (`sync`), when the width it is seen through
  changes (`fit`, which the scroll view calls from `tile`), and when the zoom does
  (`setScale`); the last two by `layOutWhereItWas`, which keeps the slide at the top
  of what is seen at the top.
- **The shape of a card** is in `SheetLook` (`shape`, 9 to 16, and `shapedUpTo`, the
  four lines) and is worked out once for the song in `layOut`; a cell remembers
  where its lines start (`linesTop`), since they stand in the middle of it and not
  at its top, and `line(at:)` goes by that.
- **The switch** between the two editors is `EditorSwitch.swift`, with its two
  pictures; the header that holds it, the name and the keys is `SongPane.header`,
  which folds from one row to two and to three as the pane narrows.
- **The size of the ChordPro text** is `LibraryModel.textZoom`; `ChordProText.setScale`
  gives the text view its two fonts at that size and colours it again.
- **Revert** is `LibraryModel.revert`: the song's bytes are remembered when it is
  opened (`asOpened`, from `OpenSong.bytes`), and put back through the same
  `putBack` an earlier version goes through, so that it is refused, copied before
  and undone as that is. `changedSinceOpened` is brought up to date wherever a
  change is noted. The button and the words "Autosave on" are `SongPane.saving`;
  the question is put by `LibraryModel.askToRevert` and shown by `LibraryWindow` as
  an alert; Command+S and the File menu's two items are in
  `App/Pro7ChordEditorApp.swift` and call `LibraryModel.saveNow` and `.askToRevert`.
- **The settings** are `SettingsWindow.swift`: the tabs, `ChordColourSettings`, and
  `ChordColour`, which turns a colour as it is kept ("#rrggbb") into one that is
  drawn. The two colours are `LibraryModel.sheetChordColour` and `.textChordColour`,
  nothing where the usual one is kept; the sheet's canvas and the text view are each
  handed theirs when SwiftUI brings them up to date.
- **What a paste did** is `ChordSheet.Pasted`, which `paste()` answers with; what is
  said of it is `LibraryModel.said(of:)`. The keys go canvas -> `Asks.paste` ->
  `LibraryModel.pasteChords`. `SheetCanvas.answersInTheBox` is the Command keys
  while the bubble has the keyboard.
- **The zoom** is `LibraryModel.sheetZoom`, one of `sheetZooms`, kept in the app's
  settings (`AppData.Settings.sheetZoom`, nothing when it is the usual one). A key or
  a pinch goes canvas -> `Asks.zoom` -> `LibraryModel.zoomSheet` -> back as the new
  size, which the canvas takes at once; a button in the strip goes to the model, and
  SwiftUI hands the size to the canvas.
- **How a thing done goes**: an event -> one of the canvas's plain handlers
  (`pointerMoved`, `pressed`, `dragged`, `released`, `key`) -> `ChordSheet` -> the
  song, which saves or refuses -> `ChordSheet.changed` or `.refused`, which
  `LibraryModel` set (`sheetChanged`, `refuse`) -> back in the handler,
  `SheetCanvas.sync()`: lay out again if the rows changed, open, move or close the
  bubble, draw. What changes the sheet from outside its view (the strip, the keys'
  pickers, undo, another song) puts `LibraryModel.sheetVersion` up, and SwiftUI then
  has the canvas `show` and `sync`.
- **`LibraryModel.settle()` keeps a chord being typed on the sheet** before anything
  else is done to the song, as it saves what was typed in the text; and answers
  false if the song refused it.
- **The two editors keep to one song.** Going to the sheet reads the song again; going
  to the text makes the text again if anything but the text changed the song
  (`textIsStale`). Undo on the sheet is the song's own history, so it walks back
  through what the text saved too.
- **Tests**: `SheetVectors.swift` has the shape of the vector file and `StandInSong`,
  the document's stand-in written from `tools/make-vectors/sheet.js`.
  `ChordSheetTests` runs every case; `ChordSheetOnAFileTests` does the same things to
  copies of the test files and reads the files; `ChordSheetViewTests` calls the
  canvas's handlers with points from `SheetCanvas.point(over:)`, hands it real key
  events, and types into the bubble's box through its field editor.
- **M7 changed nothing in the sheet.** It asked already; the song stopped refusing.

### Chords with no words (M7)

- **In `ChordRules`**: `RTFWords.swift`. Its reader now keeps, for every unit of the
  plain text, where it is written (`Written`: its bytes, whether a control word with
  no space of its own ends just before it or is it, and how many groups are open).
  At its foot are the only two things that write: `rtf(_:withLine:become:)`, which
  changes one line in place, and `rtf(withTheOpeningOf:then:)`, which makes an RTF
  for a box with no words. Each reads back what it made and answers with nothing
  rather than with a doubt. `span(ofLine:of:)` is for the tests, to say what "outside
  the line" is.
- **In `Song`**: `OpenSong.setChordsAlone` sorts what is asked into three: a line of
  chords alone (`change(line:ofBox:)`), a blank slide's own box (`giveWords`), a
  blank slide with none (`giveABox`). `boxToModelOn` finds the nearest box with
  words. `madeForChords` is what a change of mind goes back to. Every change of a
  slide now goes through `changeCue`, which hands over the cue; `changeText` is that
  for one box's text.
- **In the editors**: nothing added. `ChordProEditor.canBeSavedYet` and its verdict
  are gone, and a line with a bracket still empty no longer waits.
- **Tests**: `StandInsInRTFTests` (the bytes, worked out by hand), `StandInVectorsTests`
  (the vector file), `ChordsWithNoWordsTests` (the song, on the test files: the two
  proofs of the plan, and that nothing else in the file changes),
  `AsAppleReadsItTests` (what was written, read by AppKit: the one place a test of
  the rules uses it), and cases in `ChordProEditorTests`, `ChordSheetOnAFileTests`,
  `ChordProTypingTests` and `ChordSheetViewTests` for each editor. Pictures 33 to 35.
- **The test song is not among `TestData.presentationPaths()`**, which are the files
  Simple Presenter was asked to read: tests name it (`ChordsWithNoWordsTests.shaped`).

### A chord dragged out of the strip (after M7)

- **In `Song`**, at the foot of `ChordSheet.swift`: `liftOfKey` (keeps a chord that
  was being typed), `carry(_:over:_:)`, `carryOff`, `dropOfKey`. A chord from the
  strip is held in `dragged`, as one lifted from the sheet is, with a place it came
  from that is nowhere (row -1); and only while it is over a place. So what draws
  the lit bubble for a carried chord draws this one, with nothing added.
- **In `AppViews`, the strip's end**: `KeyChordGrip.swift`. A view of AppKit lies
  over each button of the strip, seen through, and takes the mouse: a press let go
  is the button's click, and a press pulled 6 away begins the system's dragging.
  **It is there because a button of SwiftUI cannot begin that dragging** and still
  be clicked: SwiftUI has its own way of dragging (`onDrag`), and how that and a
  button's click share a press could not be found out without a hand. The button
  is still under it, for the keyboard and for what reads the screen aloud. The
  grip draws only a shade: on a button held down, and a paler one while its chord
  is out.
- **The sheet's end**: in `SheetCanvas`, "A chord carried in from the strip".
  `carriedOfKey` and `droppedOfKey` are what happens; the six things the system
  calls on a view that something is dragged over (`draggingEntered` and the rest)
  only read the name off the pasteboard and the place off the pointer, and call
  those. `scrollIfCarriedNearAnEdge` goes by the clock, and not upwards until the
  chord has once been clear of the top (it comes in over the top).
- **Two things the view asks the system are answered by the tests instead**: the
  time (`now`) and whether Shift is held (`shiftIsHeld`). The system tells a view
  neither while a thing is dragged over it.
- **Tests**: `carriedInFromTheStrip` in `ChordSheetTests` (the rules, on the
  vectors' songs) and in `ChordSheetOnAFileTests` (the file: each place, undone to
  the byte, and the same as the key makes it); `ChordFromTheStripTests` (the
  window's two ends, its head says how); picture 37.

### The guide (after M7)

- **The words**: `Core/Sources/AppViews/Guide/guide.json`, a copy of
  `spec/guide.json`. To change a wording, change the spec's and copy it over (and
  put the same change in the page, `spec/13-guide.md`, "The words"): `GuideTests`
  fails until all three say the same. `Guide.swift` reads it (`Guide`, plain, no
  window) and puts the Mac's names for the keys in (`said(on:)`).
- **Where the app finds it**: `GuideFiles`, in `Guide.swift`. The folder is the
  package's one resource (`resources: [.copy("Guide")]`), which the package manager
  puts in `Core_AppViews.bundle`: in the app's Resources when Xcode builds the app,
  and beside the tests when `swift test` builds those, **laid out differently in
  the two**, and both are looked in. **Not with `Bundle.module`**, which stops the
  whole app when the bundle is not where it expects: the app opens this window by
  itself at its first start, and a guide that is not found says so in its window
  instead. `make-dmg.sh` refuses an app that has not got its guide.
- **The window**: `GuideWindow.swift`. `GuidePage` is the page, `GuidePicture` one
  picture, `GuideCommands` the Help menu's entry. The scene is in the app's own
  file (`Window(GuideWindow.title, id: GuideWindow.id)`), with the system's putting
  back of windows switched off for it.
- **When it is shown**: `LibraryModel.guideAsked` goes up, and `LibraryWindow`
  opens the window when it does. It goes up at the end of the first `start()` on a
  Mac whose `settings.json` has no `guideShown` (which is then written, and only
  that), and at `showGuide()`, which the ? in the strip calls. Help opens the
  window itself and asks the model nothing, so that it works with the main window
  closed.
- **The pictures**: `GuidePictureTests`. Its head says how they are made again:

      PRO7_GUIDE_PICTURES=write swift test --package-path mac/Core --filter GuidePictureTests

  They are also written, every run, to the pictures' folder as "guide <name>.png",
  to be looked at. `PictureTests` has the guide's own window: 38 to 43.

## What was found out

- **A file read and written back unchanged is the same, byte for byte**, in every
  file tried: the nine of `testdata/files`, and copies of 22 real songs of the
  maintainer's (14 from Multitracks; last written by ProPresenter 7.8.0, 7.9.2,
  7.13.0, 7.16.2, 17.1.0, 18.1.0, 19.0.0 and 20.0.1 on the Mac and 7.14.0 on Windows;
  1,299 chords among them). This was a check made once, by hand, on copies in a
  temporary folder, with a throwaway program on `ProFormat`; it is not a test and is
  not relied on, since the rule is about the message (above). But it means that a
  change of chords should change the bytes of the chords and nothing else, which is
  worth a test at M2 on `testdata/files`.
- **`proto/autogen-proto` knows every field in those 22 files.** `protoc --decode`
  shows none without a name. Through the older `proto/proto` there are 123 without a
  name, in 4 of the files. That is the difference the maintainer's first decision
  makes: nothing is lost either way, but with the newer set there is nothing in
  those files the app is blind to.
- **The descriptions are of ProPresenter 21.4.** When the submodule is moved on to a
  later version's, generate again.
- **ProPresenter's bundle identifier is `com.renewedvision.propresenter`**, in every
  version looked at here, from 7.9.2 to 21.3. Now in `spec/06`, rule 17.
- **The test files say they were written by ProPresenter 7.16.2 on no platform**
  (`application_info`): Simple Presenter wrote them. A file ProPresenter wrote says
  `macos` or `windows`.
- **`protoc` warns "Import customOptions.proto is unused"** once for nearly every
  file when the script runs. It is true and harmless: the descriptions are taken from
  ProPresenter by a program, and it leaves those lines in.
- **Swift 6.1 at least** is needed: the generated code marks its types `nonisolated`.
  So Xcode 16.3 or later; the workflow uses GitHub's `macos-26`.
- **The workflow passes on GitHub's Mac** (first run, 2026-10-10, on `macos-26`, under
  two minutes): the tests, and the app built and signed to run locally, from a clone
  with no submodule and nothing installed. That was the last of M0's "done when".
- **Pushing is over SSH.** The key on this Mac is one GitHub knows. Over https, the
  token in the keychain may not change `.github/workflows`, and a push that touches a
  workflow is refused. `gh` is not needed to push, and the runs of a public
  repository can be read without it. (It was not signed in at first. It has been
  since 2026-10-12, as the maintainer's account, and is what published the first
  release: `gh release create ... --prerelease --latest=false --verify-tag`, of a
  tag pushed first, after both workflows had passed on its commit; and the image
  was then fetched back and held against the one sent. A release is published on
  the maintainer's word and not otherwise.)

### At M1

- **Three things about Swift and Foundation that would have made the rules wrong for
  some songs**, all now in `spec/02`, "To the letter":
  - Foundation's white space has U+200B in it, and its trimming empties a line of
    stand-ins. (`spec/02` had said no library counts it so.)
  - Swift compares strings, and tests a prefix, by whole characters: "é" as one unit
    is the same to it as "e" and an accent.
  - Swift does not promise that its sort keeps equals in order.
- **A backslash, a "u" and four digits, typed into a file by an agent, can arrive as
  one Chinese character.** It is what RTF's numbered characters look like. It happened
  in a test sample here, and looked for all the world like a fault in the reader of
  RTF, which was right. `NotHeldByVectorsTests` builds its sample from pieces for that
  reason. After writing anything about RTF, look for characters that should not be
  there: `LC_ALL=C grep -rn -P '[^\x00-\x7F]' mac/Core`.
- **How the tests were shown to bite.** Every vector passed the first time the tests
  were run, which proves little. So each rule was broken in one small way in turn,
  seventy-nine ways in all, and the tests run each time. Fifty-eight were caught by a
  vector. Twenty-one were not. Two of those cannot be told apart from the rule by any
  input (worked through by hand). The other nineteen are rules no vector holds: see
  the next heading. Each now has a case in `NotHeldByVectorsTests`, and the breakages
  were run again to see those cases catch them.
- **On copies of the 22 real songs** (a check made once, outside the repository,
  printing numbers only):
  - 400 text boxes have RTF. The reader here and AppKit's give the same words, unit
    for unit, for all 400. None has a byte over 127 in its RTF.
  - All 1,299 chords stand on a place inside their words; none on a line break.
  - **Every chord on a line of chords alone has a range one character long** in those
    files: 290 of 290, its stand-in and no more (171 hang on U+200B, 119 on a space).
    `ranges()`, and `ranges.json`, run such a range on to the next chord, over the em
    quad between: (0,2) (2,4) (4,5) where Multitracks has (0,1) (2,3) (4,5). That is
    194 ranges that would be written otherwise than they were found. The maintainer
    has decided for Multitracks' way (decision 10). Looked at again for that
    decision: of the 1,009 chords on lines of words, 949 have exactly the range
    `ranges()` writes and the other 60 are the old editor's; and "one long on a line
    of chords alone, otherwise as now" gives all 1,239 undamaged ranges, with no
    exception. The chord entries are in order of place in every box, after every
    other entry, as `spec/01` has them written. Of the 82 boxes with a line of chords
    alone, 80 are that line and nothing else.
  - **Blank slides**: 27 slides of 383 have a blank text box (30 boxes; 14 share
    their slide with another text box, 5 have data links), and one slide has nothing
    on it. A blank box's RTF, as Apple's text system writes it (29 of the 30), is an
    empty document with an EMPTY font table: no font, size or colour to give a
    stand-in. That is why `spec/01` has the RTF of a blank slide made from a
    neighbour's opening and not changed in place. A box that is one stand-in with a
    chord on it, as Multitracks writes it, has the whole opening and then the
    stand-in: a space, or a numbered U+200B.
  - Away from such lines, every range in these songs is exactly what `ranges()`
    would write, but for two songs, in which 60 chords have a range that
    runs past the end of its line: check 1 of `spec/08`, the old Windows editor's
    mark. The other checks call nothing in any song doubtful.
  - The one song last written by ProPresenter on Windows has six text boxes and no
    line break in any of them. So how ProPresenter on Windows writes a line break is
    still not known (`spec/01`, "Not known"). Its RTF is not Apple's dialect (no
    `cocoartf`; it has a list table, margins and paper size) and reads the same here
    as in AppKit.
  - Two songs are shown in another key than their own (`user` is not `original`), one
    of them in C#, which is not among the twenty-six keys a picker offers.

### At M2

- **How the proofs were shown to bite**, as at M1: each rule of the song layer was
  broken in one small way in turn, forty-five ways, and its tests run each time.
  Forty-four were caught. The other no input can tell from the rule (a box that is
  only a zero-width space is not blank whether or not it is asked for by name, since
  a zero-width space is not white space).
- **Where the test files could not hold a rule**, cases were made from Plain Song in a
  temporary folder, and say they are mine: slides whose cues are not in the order
  their groups name them, a cue named twice, a cue in no group, an action switched
  off, chords out of order in the file, an entry that is not a chord among the chords,
  a blank slide, a box with a link. In every test file the cues are already in group
  order and the chords already in order of place, so `as-read-by-simple-presenter.json`
  would pass without either rule.
- **On copies of the 22 real songs** (once, outside the repository, numbers only):
  383 slides; 387 text boxes handed over, 17 of them blank slides; 1,299 chords. Each
  box was then given the chords it already has, and everything undone:
  - **all 22 files came back byte for byte what they were**;
  - for 236 boxes of 307 the file did not change at all when the box was given its own
    chords;
  - the 71 that changed are the 51 with a line of chords alone (the range rule that
    is to change: decision 10) and the 20 the old Windows editor had saved.
  Run again with the new range rule in: the 51 are none. 287 of 307 boxes are written
  exactly as they were, and the 20 that are not are the old editor's, in two songs.
- **On copies of the 309 songs of the maintainer's MainHall library** (the same way):
  every one opens; every one read and written unchanged is the same bytes; there is
  no field in any of them that the descriptions have no name for. Two have chords.
  One is from Multitracks, and all 121 of its chords are written as the rule writes
  them, the 56 on lines of chords alone among them. The other is not from
  Multitracks, states no key, and has 8 chords each with a range exactly 4 long, none
  of them on the start of a word: **who wrote those is not known**. It is not what
  Multitracks writes nor what the old Windows editor is said to have written
  (`spec/08`). Ask the maintainer before M8.
- **The file ProPresenter wrote** (`testdata/files/made-by-propresenter`) has its cues
  out of the order its groups name them, a colour on every group, and a link on every
  text box that only says when the box is to be seen. Under "a box with a link is not
  shown", as the page first had it, the song would have had no words at all.
- **No chord's name in those songs would be changed by tidying**, and none is empty.
  It matters because setting a box's chords tidies every name in the box, the ones
  nobody touched among them (`spec/05` now says so).
- **Every `music` in those songs has both its keys**; seven songs have none; two are
  shown in another key than their own.
- **A box with a link.** The page said "no data_links". The reference is finer: a box
  whose only link says when it is to be seen keeps its own words. Followed, and
  `spec/01` now says it.
- **`FileManager.replaceItemAt` keeps the old file's permissions**, and fails cleanly
  on a file that may not be changed (the new file is then cleared away). Both are
  tests.

### At M3

- **How ProPresenter 21 records its workspaces** is in `spec/06`, rule 3: a list in
  its own settings, under `userWorkspaces`. Read with `CFPreferencesCopyAppValue`,
  which an app that is not sandboxed may do for another's settings.
- **On this Mac the finder finds two places**: the workspace in use (1 library, 2
  songs) and `ProPresenter` in Documents (10 libraries, 422 songs, 19 of them with
  chords, 3 the old editor's copies, none that cannot be read). The folder the
  settings name is the workspace, so it is shown once.
- **The marks of all 422 songs are worked out in a quarter of a second** (an
  optimised build). They are still done in the background and kept, as the page
  asks: a library on a slow disk is another matter.
- **ProPresenter's helper programs go on running after it has quit.** They have
  bundle identifiers of their own, and asking for the app's own does not count them:
  checked with ProPresenter quit and two helpers running.
- **Drawing the window off the screen** (`spec/10`): `cacheDisplay` of an
  `NSHostingView` in a window never shown draws lists, fields and buttons. A
  sidebar's see-through material comes out blank, which is why the panes are plain
  ones with grounds of their own and not a `NavigationSplitView`. (They were the
  panes of an `HSplitView` until 2026-10-13: decision 80 has why they are not now.)
- **A view that scrolls both ways centres what is smaller than itself.** The song is
  given the pane's size as its least size to sit at the top left. Seen in a picture,
  and put right.
- **The four things the first pictures showed wrong**: that; a quiet line too long
  for its pane; a raw error code in the sentence for a file that cannot be read; "no
  songs in this library" where there was no library.

### At M4

- **How the proofs were shown to bite**: twenty-six breakages, one at a time. All
  were caught but one, which was a rule with nothing to show it (after thinning, the
  index still listed copies that were gone). It has a check now, and is caught.
- **A put back and its taking back leave two copies more**, each "before putting back
  a copy": a copy is held only against the NEWEST kept (rule 4), so the second is
  made though an older copy has the same bytes. I had expected one; the rule is the
  rule, and the page now says why.
- **`Data.write` will not take "never write over a file" together with "atomically"**:
  it stops the program. A copy is written with the first alone, then read back, and
  cleared away if it did not come out whole.
- **Three pictures were looked at** (the panel, the window just after a putting back,
  All Copies) and nothing in them wanted changing.

### At M5

- **A text view does not answer the Edit menu's Undo itself**: it leaves `undo:` to
  its window, which uses the window's undo. Given an undo of its own by its delegate
  (which rule 23 wants, so that it can be forgotten when the text is set afresh), it
  must answer `undo:` and `redo:` itself, and say when each can be done.
  `ChordProText.View` does.
- **The help with brackets is given to what arrives as typed text**
  (`insertText`), not by looking at which keys are down: on the Mac a key pressed
  with Command or Control never arrives so, and several keyboards type `[` and `]`
  with Option held. What is pasted or dropped comes by another door
  (`readSelection`) and gets none.
- **Everything the system does to text as it is typed is switched off** in the text
  view (curly quotes, dashes, spelling put right, spaces added round a paste): each
  is a change to the words, and would be refused one key press after another.
- **Asked to bring the caret into view before it has a size, a text view scrolls its
  own margin away.** `ChordProText.show` puts the top left corner in view instead
  when the caret is at the start.
- **A window made in code lets go of itself when closed** unless told not to. A test
  that closes one it still holds brings the tests down a little later, in some other
  test. `isReleasedWhenClosed = false`.
- **A save that is waiting for the typing to stop can come after a test is over**,
  when its made-up Mac is gone. `MadeUpMac` is held weakly by what it hands out, and
  a test that types either waits (`whatWasTypedIsSaved`) or settles.
- **Swift calls two Strings equal that only look alike** (an accented letter as one
  character and as two). What is typed is held against the words as UTF-16 units,
  never as Strings; `Chords.withoutChords` and `chordsOf` were given forms that take
  and give `Words` for it.
- **Thirteen pictures were looked at.** Three things were wrong in the first of the
  editor and were put right: the song's name was cut to "A...ace" by the two keys
  beside it (they go under it now where the pane is narrow); the line of help was cut
  off; and the text's left margin was scrolled out of sight. The dark one, the one
  with ProPresenter running, a refusal with Open Again, a song whose words have
  brackets, and blank slides were then as meant.
- **How the proofs were shown to bite**: eighty-two breakages, one at a time, of the
  editor's rules, the reading of blank slides and the window. Seventy-seven were
  caught. Two that were not were holes in the tests and are caught now: text with a
  line more or a line fewer at its END (every line before it still being what it
  was), and a paste straight after typing. One was a guard nothing reached (a paste
  does not come in by `insertText`), and is taken out. The five still not caught are
  each a second lock on a door another check already holds, and say so where they
  stand: the reference's test for a stray bracket (the words not matching says it
  already); rule 16's "on the last character" (`Chords.tidy` does the same when the
  box is written); which side of a `]` Delete is on (it comes to the same chord);
  that a barred song saves nothing (nothing is ever taken from one); and asking
  about a change made from here before making it (it is asked about again after).
- **The tests hung once, and it could not be made to happen again.** In the first
  breakage round one run of the tests never came back (half an hour, until it was
  stopped). The same broken rule was run 43 times more, then the whole round again
  with a time limit on each, then all the tests but the pictures 80 times: no hang.
  The cause is NOT known. What was running were the tests that type into the real
  text view from a program with no window on the screen, and my guess is something
  in the system that such a program waits on now and then (the tests made the
  system's "no" sound then; they are silent now). If it comes back: `sample` the
  process named `swiftpm-testing-helper` before stopping it. The workflow has a time
  limit now so that a hang there cannot hold a machine for hours.
- **A check on copies of 331 real songs** (the maintainer's two sample libraries,
  copied outside the repository under numbers, read and then deleted; numbers only):
  all opened; one cannot be changed as ChordPro, having brackets in its words; 16,268
  lines of text, 130 of them lines of chords alone, and **1,173 blank slides**, five
  with no text box. For each of the other 330, nothing typed saved nothing and kept
  no copy. In the 281 that have a line of words not starting with a chord, a chord
  typed at the start of the first such line was taken, saved, and was then in the
  file at that place with the box's words as they were; the only thing in the whole
  message that differed was that one text box's chord entries; and undo left the
  file byte for byte what it was.
- **A text box's id is not its own in a song.** In one of those songs the boxes of 27
  slides share one id: slides made by duplicating one. My check tripped on it, not
  the app, which names a box by its slide and its id and changed the one box. But
  nothing must ever tell boxes apart by id alone: the editor goes by which box of the
  list it is (`SongLine.box`), and the sheet must too. `spec/05` says so now.
- **Blank slides are everywhere**: three and a half to a song in those libraries. In
  the ChordPro text each is an empty line after the gap's empty line, so most songs
  have runs of two empty lines in them. It reads well enough in the pictures; whether
  it does in the hand is for the maintainer to say (`spec/04`, rule 3).

### At M6

- **Every case of `sheet-model.json` and `layout.json` passed the first time they were
  run**, the model having been written from the reference's functions line by line.
  Then seventy-two of its rules, and the view's and the window's, were broken one at
  a time: sixty-six were caught, and the six that were not got tests (which line a
  chord is on at a line break and on an empty line; a group with no name; a chord of
  a line of chords alone moved to its front; two pasted chords landing on one place;
  a click let go on another word than it was pressed on). All seventy-two are caught
  now. The vectors hold none of those six: `testdata/README.md` could ask for them.
- **Breaking the rules one at a time is done after a commit, and in the foreground, a
  dozen or two at a go.** The script puts each file back when it is done or stopped
  with an interrupt, but not if it is killed; and a file left broken among changes
  not yet committed cannot be told from the real one. (It was started once in the
  background before the commit, and stopped by hand to do it the other way.)
- **Core Text says a zero-width space that ends a line is at the line's left edge**
  (`CTLineGetOffsetForStringIndex` answers 0). `SheetMetrics.edges` puts it right
  (`spec/10`). Found on copies of real songs, where 18 lines of words in 4 songs have
  one.
- **A view that is hidden gives the keyboard up to its window.** So the bubble asks
  who has the keyboard before it hides, and gives it back to the sheet after.
- **Making a box of text the first responder when it already is ends its editing
  first**, which reads as the person having left it. `SheetEntry.begin` does not ask
  again; and what the canvas does to the keyboard itself is marked (`movingTheKeys`)
  so that it is not taken for that.
- **A click on a view that accepts the keyboard takes it from a box being typed in**,
  before the click is heard of. The canvas does not accept it while a chord is being
  typed, so that the click is worked out with the song as it was (`ChordSheet.pick`).
- **A row that cannot be made narrower is not cut at its end: its whole pane is drawn
  too wide and centred, cut at BOTH sides.** The song's header with the switch added
  did that at the window's usual width, taking the left of the sheet with it. Seen in
  a picture and nowhere else. The header now folds onto two rows and then three
  (`SongPane.header`), and picture 23 is the window at its narrowest.
- **A row of SwiftUI buttons squeezes and wraps their names** when the pane is narrow
  ("Am" on two lines). The strip's chords are fixed in size and the strip scrolls
  sideways instead.
- **The sheet on copies of 422 real songs** (numbered copies outside the repository,
  numbers only, deleted after; the M5 check had 331, of fewer libraries):
  - All 422 opened: 13,868 rows, 12,015 of them lines, 138 lines of chords alone,
    1,583 lines of blank slides. The longest song is 521 rows.
  - Of 296,291 places, pointing at each gave that place for all but 18, every one
    of which is a character with no width (a soft hyphen, a vertical tab, a zero-width
    space beside another): `spec/03`, rule 18a.
  - A chord put with a key on the first word of the first free line of words, in
    360 songs: in the file at that place every time, no other text box touched, and
    one undo left the file's bytes as they were.
  - Sixteen lines of chords alone took a rename and refused one more, the file
    untouched.
  - Slowest of all the songs: laying out 16 ms, drawing a window's worth 25 ms, two
    hundred moves of the pointer 2 ms.
  - **One slide in one song has 104 lines**, and since every card is as tall as the
    slide with the most (rule 8), every card of that song is. Six songs have a slide
    of more than 6 lines; in ten the tallest slide is more than three times the
    song's middling one. The maintainer asked for cards of one size; whether that is
    to have a limit is theirs to say.
  - **40 text boxes of 6,091 end with empty lines** (one, in 28 of them; up to
    fifteen), in 19 songs, and no chord stands on any. They made the cards of 11 songs
    taller, and get no row now (decision 35).
  - **The song the maintainer named for a card too tall has no such lines.** One of
    its slides has one line and the rest two, and the card of the one is as tall as
    the others': that is rule 8, all cards one size, which the maintainer asked for
    in Simple Presenter after trying cards of differing sizes. In 104 of the 422
    songs the tallest slide has more lines than the song's middling one, so most of
    their cards have room to spare. Asked of the maintainer, who changed the rule
    (decision 36).
  - **With each card its own height**, the sheets of all 422 songs together are 75% as
    tall as with cards of one size; 291 songs are shorter, the middling one of those
    to 88%. In the first 480 of a sheet's height, which is about what the window
    shows at the size it opens at, there are on average 3.4 lines of words where
    there were 2.7. **That is still few.** A line of words with its row for chords is
    61 tall, a slide's card adds 30, and a group's name 34: what would show more is
    smaller sizes, which is the maintainer's to choose for both apps.
  - **49 songs have a line wider than 900 at the sheet's size, 20 of them wider than
    2,000**: whole verses as one line, which ProPresenter wraps and the sheet does
    not (rule 12). Their cards are that wide, and the sheet scrolls sideways.
  - Lines of words that have in them: a tab, 81 lines in 5 songs; a no-break space,
    53 in 9; a vertical tab (a line break that does not end the paragraph), 26 in 3;
    a zero-width space, 18 in 4. A vertical tab is no line's end here (`spec/02`), so
    such a line is one line on the sheet.

- **The grid, on copies of the same 422 songs** (numbers only, deleted after). In the
  window as it opens, where the sheet has some 810 by 640, at the usual zoom: two
  slides to a row in 215 songs, three in 31, four in 79, and one in 97; an average
  of 9.2 lines of words in sight without scrolling, which is 38% of all there are,
  where the one column had 3.4; 106 songs wholly in sight. At 60 in the hundred,
  14.1 lines, 58%. In a large window (1450 by 900): 19.2 lines, 80%, and 284 songs
  wholly in sight. Of 22,236 places pointed at, none gave the wrong place. Slowest
  layout 17 ms, slowest drawing 24 ms.
- **97 of them were one slide to a row at the usual zoom, and 45 scrolled sideways**,
  every card being as wide as the song's widest line and a line never wrapping.
- **With a column only as wide as its own slides need** (decision 39), the same
  measures: in the window as it opens, at the usual zoom, 10.1 lines of words in
  sight (42%), 122 songs wholly in sight, one slide to a row in 89 songs and two in
  219; at 60 in the hundred, 15.4 lines (64%); in a large window, 19.8 lines (82%)
  and 320 songs wholly in sight. Still 45 scroll sideways: a line never wraps, and a
  verse written as one line is wider than the sheet. None of 22,236 places pointed
  at gave the wrong place; slowest layout 22 ms.
- **Cards of one size and a screen's shape** (decision 43), on copies of the 422
  songs, in the window as it opens (the sheet now some 1,010 by 640, the songs' list
  having gone under the libraries): 9.0 lines of words in sight on average (37%),
  113 songs wholly in sight; two slides to a row in 132 songs, three in 141, one in
  80; 39 scroll sideways. At 60 in the hundred, 13.6 lines (56%). In a large window
  (1,650 by 900), 17.5 lines (72%), 227 songs wholly in sight. 325 songs have cards
  of 16 by 9 and 97 are held to four lines; 261 rows of 3,566 are taller than the
  shape, for a slide of more lines than it holds. None of 22,236 places pointed at
  gave the wrong place.
- **Asked only when the app came to the front, whether ProPresenter was running was
  wrong after it was quit** (decision 40): quit from its own window, it is still
  going when this app is in front again. `LibraryWindow` now listens for
  `NSWorkspace`'s news of an application launching and terminating, and asks again
  at once and a second later. That the band then goes has been proved on the model
  with a made-up Mac, and NOT by quitting ProPresenter: the maintainer's to see.
- **Thirty more breakages, of the grid's rules**: twenty-eight caught; one was no
  breakage at all (mine, written wrong), and one showed a test that could not fail
  (a frame and its foot being bare sheet), which was mended. All are caught now.
- **A scroll bar that takes room beside what it scrolls** (the system's choice where
  a mouse is plugged in) would change the width the slides are fitted across as it
  came and went with the layout. Room is kept for one always in that case
  (`SheetCanvas.seenWidth`). Not seen to go wrong; reasoned.

### Before M7: what real files show of blank slides

Looked at on 2026-10-11, the maintainer having pointed to a song of theirs whose first
slide has no text box ("I tested in the app and looked like I could enter a chord (the
cursors appeared) - I did not enter - so you could look at it if needed"). On numbered
copies of 426 songs outside the repository, shapes and numbers only, deleted after:

- **30 slides of 7,238 have no text box, in 17 songs.** None has anything else drawn
  on it either. As ProPresenter 21 on the Mac writes one, the slide is its size and
  its id and nothing more (`base_slide { size, uuid }`, no `elements`), with the
  `chord_chart` mark every slide has. 16 of the 30 are there to do something else when
  they are shown: a video or a picture started (14), a macro (2), a stage layout, a
  prop, a jump to another slide. So **the box M7 adds to such a slide is added to a
  slide whose job is to start something**, and stays unseen there, its text being
  stand-ins. That it does no harm is to be seen in ProPresenter.
- **2,426 text boxes have no words.** All but 13 were written by Apple's text system,
  and of those all but some 120 are the empty document `spec/01` describes ("Chords
  onto a blank slide"): no font in its font table, no paragraph, no size, no colour.
  Some 120 hold one plain space, in a font and a format. 13 begin `{\rtf0` and were
  written by something else. 2,266 have no settings that go by a range of the text;
  160 have from one to fifteen.
- **The page's recipe holds for what was seen**: an opening taken from the nearest
  box that has words, the stand-ins after it. In the song pointed to, that box's
  other settings by range (how it is capitalised, its first font) each run over the
  whole of its words: what the page leaves where they are, as a guess.
- **The song itself cannot be a test file** (its words are someone's, and it names
  the maintainer's own media). The test file for M7 is to be made from the one
  ProPresenter wrote that `testdata/` has already (Rock of Ages): one slide's box
  given the empty document these files have, and one slide's elements taken off.
  The maintainer need not make one (`spec/11`, "What the maintainer is asked for").

(Done at M7: `testdata/files/shaped-as-real-ones`, which has both, and seven shapes
more.)

### At M7

- **Tried on copies of 425 real songs**, numbered, outside the repository, numbers
  only, deleted after (a throwaway test, as before):
  - 116 lines of chords alone, each given a chord more, a chord fewer, two more and
    none: 423 changes, none refused. Every one left the words and chords as meant,
    every byte of the RTF outside the line as it was, and nothing else in the file
    changed; and every one was undone to the byte.
  - 1,552 blank slides with a text box (1,431 the empty document, 110 holding stray
    space in a font, 11 something else) and 34 with none. 853 were given chords:
    each as meant, nothing else in the file changed, undone and redone to the byte,
    and back to the byte when the chords were taken off again by way of one more and
    fewer.
  - **The other 733 are in 63 presentations with not one text box that has words**,
    and not a chord: pictures, countdowns, announcements. They are refused in the
    page's sentence, as decided ("the apps do not devise a text box"). So on such a
    presentation the sheet shows blank slides and a spot, and a chord put there
    says why not. It was not known there were so many.
  - **In 100 of them, text boxes on different slides have one id** (1,480 ids that
    are some other box's too). The app always looked for a box on its slide; the
    first run of this check did not, and called 456 good changes bad. `spec/01` now
    says an id is one box's only on its own slide.
- **A slip that no test caught, and two programs agreed on.** Whether a space put in
  would be lost (decision 50's step 6) was asked after the control word in hand had
  been read, so that for a numbered character with no space of its own, straight
  after another control word, the answer was no where it should have been yes. The
  app's code and the tool's had it alike, having been written alike by one hand, and
  so agreed on every vector. It was found by reading the answers: a case that said
  "cannot be done" for no reason. Nothing unsafe came of it (what was made did not
  read back, and was refused), but a line that could be changed was not. Put right
  in both, with a case of its own (`StandInsInRTFTests`). **What it says of the
  vectors**: two programs by one writer are one check and a half, not two.
- **One of the answers worked out by hand was wrong, and was changed.** For a model
  whose own words begin with a stand-in, the opening was expected to end before the
  `\uc0` that stands before it. By the rule as it is written the `\uc0` is part of
  the opening, and another follows it. The rule was kept and the expectation put
  right (`OpeningOfAnRTFTests.otherModels` says why). It is said here because an
  expected answer was changed: it was mine, of a rule a day old, and no vector's.
- **The tool this was written with turns a backslash, a "u" and four digits into one
  character**, and RTF is full of them. A test file came out with Chinese
  characters where its control words were, and the tool with two invisible ones. So
  those sequences are never typed: `StandInsInRTFTests` had them put back by a
  script, and everything since builds them (`"\\uc0\\u" + "\(unit) "`). If a file
  here ever shows a stray character from far up the table, that is what happened.
- **What was written, read by Apple's own reader of RTF** (`AsAppleReadsItTests`,
  and once on the copies of real songs). The app checks what it writes with the
  rules' reader, which is the app agreeing with itself; ProPresenter reads with
  Apple's. So `NSAttributedString` was handed every RTF the vectors make, every
  change to the test song, and on real songs 340 changes to lines of chords alone
  and 853 blank slides given chords. It read every one as the words meant; every
  other line of a changed box set exactly as before (127 lines compared in the real
  songs, none otherwise); and a blank slide's stand-ins in the font of the box they
  were modelled on. This is the nearest a test comes to the plan's third proof, and
  is not it.
- **Each rule broken one small way, 56 ways** (21 of the change to an RTF, 35 of the
  song's part), after it was committed, the whole of the tests run for each. Six
  went unnoticed at first, and each has a case now: that a space is given another
  before it when what it takes the place of is a character written straight in,
  after a control word with no space; that a character of two units and one stretch
  of bytes is not taken out by halves; that the same care at the end of a line goes
  by the last character's own control word and not the one before it; that what is
  made after an opening is read back; which stand-in a line that has both kinds is
  given; and that a chord on a line's own break is that line's. Three of the six
  are in the vectors, for both apps. (A seventh change, one that changes nothing,
  was put in to see that the tests can also say "unnoticed": they did.)
- **A line with no chord and one stand-in is nothing new to the editors**: it is
  what a file has that came with stand-ins and no chords, which both have shown
  since M2 as a line of chords alone with none on it. So decision 58 changed two
  lines of the song and nothing in either editor: the sheet's row stays and takes
  the spot; the ChordPro text has an empty line there that takes chords.
- **Decision 58 on copies of the real songs** (2026-10-12, numbers only, deleted
  after): every chord taken off each of the 108 lines of chords alone that have
  any, all at once and one at a time (the same either way), and one put back.
  Each as meant; not a box lost or read as blank; not a byte outside the line
  changed, nor anything else in the file; Apple's reader agreeing; the chord put
  back changing no byte of the RTF; and all of it undone to the byte.
- **The sheet's vectors can be made on this Mac**: `node` is here, and
  `tools/make-vectors/sheet.js` run on `reference/simple-presenter` gave
  `sheet-model.json` and `layout.json` to the byte, before it was touched. The
  tests' `StandInSong` is that tool's stand-in in Swift, and was changed with it.

### At the chord from the strip

- **What the system does between the strip and the sheet is not tested, and cannot
  be here.** The tests press a button and pull, and take what it hands the system;
  and they tell the sheet what the system would (with a stand-in for what the
  system hands a view, made by the test). That the system, given the one, does the
  other is AppKit's oldest road, and it is still only read about, not seen.
- **The picture that goes with the pointer is the button's own**: SwiftUI is asked
  to draw the same view again (`ImageRenderer`). Read back, it has a button's
  colour to the number and nothing at its rounded corners. **But ask the picture
  for its own bytes (as TIFF) and they come in another colour space, a shade
  lighter**: a test that read them so said the picture was wrong when it was not.
  It is read by drawing it into a picture of known colours.
- **What the system hands a view is not tied to the main thread in its
  description** (`NSDraggingInfo`), so a stand-in for it in a test says
  `@preconcurrency`; and one of its old members is already a member of every
  object, and wants `override`.
- **The made-up Mac's "Blank Slides" has no line of chords alone.** The tests of
  the window that want one put `Chords With No Words.pro` there themselves.
- **GitHub's Mac has its scroll bars beside what they scroll**, having no trackpad,
  and there they take room. The test of the scrolling passed here and failed there:
  it had measured how far the sheet can go before a bar under the sheet had come
  and gone again (it comes while the sheet is first sized, and goes a turn of the
  run loop later: seen here by telling the scroll view to have its bars so,
  `scrollerStyle = .legacy`). The test now measures as it goes, and is run both
  ways. Nothing in the app was wrong: the bar under the sheet does go by itself.
  **A test of the window that measures what is seen of a scroll view is to be run
  with the bars beside it too.**
- **What a view is seen through scrolls by whole points.** Asked to go 0.52 it went
  1, so that a slow scroll asked for often went twice as fast as was meant. What
  the sheet is owed under a carried chord is kept, and it goes the nearest whole
  number of points to that.
- **How the tests were shown to bite**: sixty-seven breakages, one at a time, after
  the commit, of the model's four functions, the sheet's end, the grip and the
  strip's wiring. Fifty-six were caught as the tests stood. Of the eleven that were
  not: seven were given a test each (that the sheet is laid out again after a
  chord is let go, without which a line with a chord more is drawn from a layout
  with a bubble fewer; that Shift is asked as it is let go; that each time over the
  sheet the chord comes in over the top afresh; that a button is not left dead by a
  carrying the system never ended; what the grip draws; and that the strip's end
  clears what is shown when the system tells only it); three were lines that did
  nothing and are gone (letting go of the chord a second time after it is let go,
  and keeping the scrolling within the sheet, which the scroll view does itself);
  and one changes nothing that can be seen (`dropOfKey` holding the name to being
  a chord, which `carry` has done already). All seven are caught now.

### At the guide

- **A window that is never shown draws at one dot to the point on this Mac**, whose
  first screen has no dots to spare, and so would a picture made that way on any Mac
  like it. Asking for more in the picture (`cacheDisplay` into a bitmap of twice the
  dots, its size left in points) draws the sheet itself twice as fine, which draws
  when asked; **but the strip's letters came out soft**: SwiftUI draws them once,
  as finely as the window says its screen is, keeps them, and they were stretched.
  Telling SwiftUI otherwise (`\.displayScale`) did not change it. **What did: a
  window that says it has two dots to the point** (`TwoDotsWindow` overrides
  `backingScaleFactor`). The pictures are then the same on any Mac's screens.
- **Drawn three times over, the pictures were the same to the dot each time**, so
  holding them dot for dot is not a test that fails by chance on one Mac. Whether
  another Mac, or another macOS, draws the same dots is not known, and the test's
  head says what to do when it does not.
- **A PNG says how many dots it has to the metre, in whole numbers**, so a picture
  written as 720 points wide comes back a hair off 720. The test allows half a point.
- **A picture in a column of other things can be squeezed shorter than its width
  makes it**, and then stands narrow in a frame too wide for it: it is held to its
  own height (`fixedSize`, up and down). It showed in the picture of the whole page
  and would have shown in no window, since a scrolling page squeezes nothing.
- **The system's arrow was not used for the pointer** in the pictures: it is the
  person's own, in the size and colour they set. The arrow is seven corners in the
  test.
- **Opening a window where no app stands behind the view does nothing and says
  nothing**: `openWindow` in a view the tests draw. So the tests can ask for the
  guide and prove the asking; that a window then opens, they cannot.
- **Xcode's bundle for a package's resources and `swift build`'s are not laid out
  alike**: `Core_AppViews.bundle/Contents/Resources/Guide` in the app,
  `Core_AppViews.bundle/Guide` beside the tests. Seen by building both and looking.
- **A test cannot press the ? in the strip.** It is a button of SwiftUI's, and in a
  window that is never shown SwiftUI tells what reads the screen aloud nothing of
  its buttons (asked, the strip said it had no children), so there is nothing to
  press. That the button is there is held by the guide's own pictures, which have
  it in them; that it asks for the guide is one line, and a hand's to try. (The
  seven chords beside it are pressed by the tests through the view of AppKit that
  lies over each, which is there for dragging and not for the tests.)
- **Broken on purpose, 46 ways, after it was built** (what is shown once and what
  is remembered, each part of the words and the names put in them, where the
  guide's folder is found, the window with and without its guide and its pictures,
  and the look of the sheet changed by a hair). All are caught now. Three kinds
  were not, as built, and each got a test: what is remembered before the first
  start saying that the guide was shown; a picture that is not there being said to
  be somewhere; and a page drawn without its pictures, which a count of colours
  had passed (it is told now by how much of the sheet's own dark is on the page).
  A colour of the sheet changed by one step in 255, a letter a point larger, and
  the ? taken out of the strip each fail the test that holds the guide's pictures.

### What made the window slow (2026-10-13)

The maintainer found 1.0.1 slow under the pointer, and every build slow when the
edge between the two parts was dragged with a song open. Nothing had ever timed the
window. It is timed now by `SpeedTests` (`mac/README.md` has the line that runs
it): the window off the screen at 1920 by 1050, a song of 56 slides in it, and the
main thread's own time at work measured for each thing done. Thousandths of a
second, at one dot to the point:

| | 1.0.0 | 1.0.1 | Now |
| --- | --- | --- | --- |
| The pointer moved a step over the sheet: usually | 1.5 | 1.4 | 1.5 |
| ... one time in ten, more than | 100 | 183 | 7 |
| ... at worst | 200 | 251 | 11 |
| A chord dragged a step: at worst | 98 | 105 | 9 |
| The edge moved a step, no song open | 7 | 6 | 6 |
| ... the sheet shown | 204 | 69 | 15 |
| ... the ChordPro text shown | 174 | 49 | 12 |

- **One cause for nearly all of it: the song's header.** It was written as three
  arrangements (one row, two, three) for SwiftUI to choose the first that fits
  (`ViewThatFits`). To choose, SwiftUI builds the arrangements it is not showing,
  measures them and throws them away, and it does so again every time the view is
  laid out. Each arrangement had the two pickers of twenty-six keys in it, and a
  picker's list of what can be chosen is costly to build: 100 to 200 thousandths
  of a second for the header, each time.
- **And it was laid out often.** At every step of the edge being dragged, as it
  must be. And at every change in what the line of hints under the sheet says:
  the line's size was asked of what was in it, so a change in it had everything
  beside it in the song's part of the window measured again, the header among
  them. The hints change as often as the pointer crosses a chord. That is the
  stall the maintainer felt: it was there in 1.0.0, at about a tenth of a second,
  and 1.0.1 nearly doubled it (one window where the system's split view had been
  two, so more was measured each time; and the line of hints no longer a fixed
  height).
- **Found by sampling the test as it ran** (`sample`, which comes with macOS, on
  the process for five seconds): the time was under `SizeFittingLayoutComputer`,
  which is `ViewThatFits`, and under it `PlatformItemListReader`, which is a
  picker's list. Reasoning about the code had not found it, and had suspected
  the drawing.
- **A thing READ while a view is brought up to date is watched for that view.**
  The sheet's view says what is going on in it whenever it is brought up to date;
  the model, to put down only what had changed, read what it had; so the sheet's
  view was brought up to date, and marked to be drawn again whole, at every
  change of the hints: by its own saying. What was last said is now kept a second
  time where nothing watches it.
- **What is left is drawing.** A step of the pointer draws the sheet again, which
  is 3 thousandths of a second for what is seen of this song, and 7 at two dots
  to the point. A step of the edge with the sheet shown is mostly that. On a
  screen of two dots to the point the window off the screen takes 30 to 45 to
  draw the whole sheet, seen or not, some steps of the pointer; whether a window
  on the screen does the same could not be told from here, and the two things
  tried against it changed nothing that could be measured (decision 84).
- **Not measured, because it cannot be from a test**: the screen itself, and how
  any of it feels.

### Rules the vectors did not hold, and what became of them

At M1, breaking the rules one at a time found nineteen that no vector held. Each was
given a case of the Mac app's own, its answer READ from the reference's code, and the
inputs were listed for the tools that make the vectors.

**On 2026-10-10 the vectors and test files were made again** from a Simple Presenter
that has the new range rule and shows blank slides, with nearly all of those inputs
in, a song for headings, a song with blank slides, and the file ProPresenter wrote
read into `as-read-by-simple-presenter.json`. What that showed:

- **The Mac app's rules passed every new case with not a line of them changed.** The
  reference answered as its code had been read.
- The allowance the range tests made for one stale case tripped, as it was built to,
  and is taken out.
- Of twenty breakages that only the Mac app's own cases caught before, the vectors
  now catch nineteen. The own cases that a vector now holds are gone from
  `NotHeldByVectorsTests`; one is left, and `testdata/README.md` ("Still wanted")
  says what would hold it.
- `as-read-by-simple-presenter.json` now has `blank` on each entry, and an entry with
  an empty `element` for a slide that has no text box at all. That last is Simple
  Presenter's own (it gives such a slide a text box when a chord is put there); this
  app hands nothing over for it, and `ReadingASongTests` leaves those entries out.

**Two things the reference now does that these apps do not, for the maintainer to
decide** (both in `spec/01`, both M7's): giving a slide with no text box one when a
chord is put there; and, while a song is open, putting a blank slide's file back byte
for byte as it was when its last chord is taken off again. The maintainer asked the
second of Simple Presenter in so many words.

## The disk image, and what was checked of it

Asked for by the maintainer on 2026-10-12, the chords having shown in ProPresenter:
"Maybe we can do an early release (can you make .dmg with apps? - unsigned for now -
I'll prob renew dev account soon)". Part of M10 done early; the rest of M10 (the icon,
the About text, a Developer ID, notarizing) is still to come.

- **`mac/make-dmg.sh`** builds a Release into `mac/build` (which git leaves alone),
  checks it, and makes `Pro7ChordEditor-mac-<version>.dmg`: the app, a way to
  Applications, and `mac/dmg/Read Me First.txt`. Its head says the rest.
- **Signed "to run locally"** (ad hoc), as the project has always been, and not left
  with no signature at all, which a Mac with Apple's own chip will not run.
- **Built without `get-task-allow`.** Xcode gives every build it makes for its own
  use that entitlement, which lets any other program look inside the app as it
  runs; a Release built by `xcodebuild build` has it too (the first image made here
  did). The script builds with `CODE_SIGN_INJECT_BASE_ENTITLEMENTS=NO` and refuses
  an app that has it. Apple would refuse to notarize one that did.
- **The version is the project's**: 0.1, build 1, as it has been since M0. (The tags
  in the repository, 0.93 to v0.9.5, are the old Windows editor's. A tag for this is
  the maintainer's to name; `mac-v0.1` would keep the two apart.)
- **Checked, without the app once being run**: both kinds of Mac are in it (arm64
  and x86_64); its seal holds, in the build and again in the mounted image, where
  it is byte for byte the app that was built; it has no entitlement; macOS's own
  judge of downloads (`spctl`) rejects it, as the read-me says it will; and a copy
  of the image marked as a browser marks a download mounts as one, and an app
  copied out of it carries the mark.
- **"Nothing in it names a path of this Mac" was written here of 0.1, and was
  wrong.** The program in the 0.1 image names the folder it was built in 682
  times (`/Users/dan/dev/Pro7ChordEditor/mac/...`), and so the name of the account
  on the maintainer's Mac: in the tables a debugger goes by, which say where every
  file the program was made from lay. Xcode takes those out when it installs or
  archives, and not when it builds, which is what the script did. They were looked
  for with `strings`, which does not show them; looked for as bytes (`grep -a`)
  they are plain. Found on 2026-10-12, when 0.2 was being checked. What it gives
  away is small (the commits in the repository are signed with the same first
  name), but it was said to have been checked and had not been. The 0.1 image was
  taken off the Releases page for it the same day (below).
- **Since 0.2 the program is stripped** (`DEPLOYMENT_POSTPROCESSING=YES` and
  `STRIP_INSTALLED_PRODUCT=YES` to `xcodebuild`), as an app sent to Apple's store
  is, and **the script refuses an app in which any file names the home folder of
  whoever built it, or the folder the script is in**. The image is 4.5 MB where it
  was 8.1. (`STRIP_STYLE=all` given on the command line stops the build: it is
  then asked of the package's parts too, which are not whole programs and cannot
  be stripped so. Left unsaid, each part is stripped in its own way.)
- **Every test passes against the optimised code** too (`swift test -c release
  -Xswiftc -enable-testing`): 304. The tests otherwise run against a build made for
  debugging, which is not what is in the image.
- **Not checked.** The app in the image has not been opened by this session: it
  would have found the maintainer's library, with a build that can write
  (`AGENTS.md`). So what a Release does at its start, in its menus and its windows
  is unseen but for the same code's tests. Nor has the image been tried on another
  Mac, on one with Intel's chip, or on macOS 15; and the steps in the read-me for
  opening it the first time are as macOS 15 and 26 are known to behave, not as
  seen here.
- **Tried by the maintainer before anyone was given it** (2026-10-12: "that
  worked"): the image marked as a download
  (`mac/build/Pro7ChordEditor-mac-0.1 (as if downloaded).dmg`, the same bytes as the
  one given out, with the mark a browser puts on what it fetches), opened on this
  Mac by the read-me's steps. So the app in the image does start, and macOS does
  let it be allowed as the read-me says, on macOS 26 with Apple's chip. Another
  Mac, Intel's chip and macOS 15 are still untried.
- **Given out on 2026-10-12** as a pre-release on the Releases page, tagged
  `mac-v0.1`, the maintainer having said "please publish": that image, of the app
  as it was at that tag. A pre-release, so that the page's "latest" is still the
  Windows editor 0.9.5, which `windows/README.md` points people at.
- **0.1 was taken down on 2026-10-12**, once 0.2 was out, at the maintainer's word
  ("yes take 0.1 down"): the release and its image are gone from the Releases page,
  and the image's address answers that there is nothing there. The tag `mac-v0.1`
  stays, for what the history and `windows/NOTES.md` say of it; a tag holds no
  image. The two copies of that image in `mac/build` on the maintainer's Mac are
  still there, and are not to be given to anyone.
- **0.2, given out on 2026-10-12** at the maintainer's word ("make a downloadable
  release"), as a pre-release tagged `mac-v0.2`. Version 0.2, build 2. Checked as
  0.1 was, and besides: no file in it names a folder of this Mac; the system's
  loader reads the stripped program for both kinds of Mac (`dyld_info`), and it
  links to nothing but what macOS has; all 317 tests pass against the optimised
  code. **Unlike 0.1, nobody had opened this image before it was given out**: this
  session does not run the app, and the maintainer asked for the release and not
  for an image to try. It is the first build whose program is stripped. The copy
  marked as a download is beside it (`... (as if downloaded).dmg`) for whoever
  tries it first.
- **Since the guide**: the script refuses an app that has not got it (the words,
  held to `spec/guide.json`, and every picture they name), and the read-me in the
  image says the guide is there. Tried on a Release built as the script builds one,
  into a folder of this session's and not into `mac/build` (decision 67 says why).
- **The README at the top names each download** ("Try it"). When an image is put on
  the Releases page, the Mac's row there is changed in the same push.
- **0.3, given out on 2026-10-13** at the maintainer's word ("a new build and
  release with this"), as the Mac's half of the release `v0.3` (decision 69), a
  pre-release. Version 0.3, build 3, built from the commit "Version 0.3". Checked
  as 0.2 was, by the script, and besides: it has its icon, the file given, and
  macOS shows that icon for the app at every size from 16 points to 256 (drawn
  into a picture and looked at); all 337 tests pass. **And it was started**
  (decision 70), which no build given out before was by this session:
  - at the first start it found the made-up library, listed it, and opened two
    windows: the main one, and one of 780 by 820 at the top right of the screen
    the main one was on, which is the guide as the app asks for it. Its settings
    then said the guide had been shown, and its log said so;
  - at the second start it opened one window, and its log said nothing of the
    guide.

  What that does not tell: anything of how the guide looks in its window, of
  Help, Command+? or the ? in the strip, or of the Window menu. Those are still
  the maintainer's to try ("Left rough").
- **1.0.0, given out on 2026-10-13** at the maintainer's word, as the Mac's half of
  the release `v1.0.0`, which is GitHub's "Latest" (decision 78). Version 1.0.0,
  build 4, from the commit "Version 1.0.0". All 353 tests pass. Started twice,
  shut in as 0.3 was, with a new made-up home: two windows the first time, the
  guide's among them, and one the second.
- **1.0.1, built on 2026-10-13 and not given out**: it is in `mac/build` for the
  maintainer to try (`Pro7ChordEditor-mac-1.0.1.dmg`, sha256 `a234835c…9bbe`), and
  a release waits for their word. Version 1.0.1, build 5: the number is another
  than 1.0.0's so that the window's title says which build is being tried. All 362
  tests pass. Started twice, shut in as before, with a new made-up home: two
  windows the first time, the guide's among them, and one the second; its menus as
  1.0.0's. What that does not tell is everything this build is for: the edge, the
  highlighted line and the hints are under "Left rough".
- **1.0.2, built on 2026-10-13 and not given out**: in `mac/build` for the
  maintainer to try (`Pro7ChordEditor-mac-1.0.2.dmg`, sha256 `d2530164…d972`), and
  a release waits for their word. Version 1.0.2, build 6. All 373 tests pass.
  Started twice, shut in, with a new made-up home: two windows the first time and
  one the second; its menus as before. What it is for, the window's speed and the
  edge, is not a thing that told: "Left rough".
- **1.0.3, given out on 2026-10-14** at the maintainer's word ("Okay let's release
  1.0.3"), as the Mac's half of the release `v1.0.3`, GitHub's "Latest" (decision
  91). Version 1.0.3, build 7. The image is `Pro7ChordEditor-mac-1.0.3.dmg`,
  sha256 `7faaf6dc…bfab`, made by the script from the commit `80497fa`, which the
  tag was on until it was moved (decision 92).
  **It is the second image of 1.0.3.** The first (sha256 `12a8819e…2c47`, built on
  2026-10-14 from what was committed half a minute later) is the one the
  maintainer tried, and the one this session started twice, shut in, with a new
  made-up home: two windows the first time and one the second. It is kept in
  `mac/build` as `Pro7ChordEditor-mac-1.0.3 (tried, not given out).dmg`. The two
  differ by one line of the read-me (decision 91). **The app in the one is the app
  in the other**: both images mounted and the two apps compared, the same file
  for file, the same number on the seal (`codesign`'s CDHash), and that app the
  same again as the copy that was started. Checked besides, of what was given
  out: the script's own checks; the words of the hints as they are now, found in
  the program; all 378 tests, against the optimised code as well as the other;
  GitHub's Mac build on the tag as it then was; and the file fetched back from
  the Releases page
  and held against the one sent. **Not done again**: the app was not started from
  the second image, being the app that was; and nobody has gone through the
  read-me's steps for opening it with an image that came through a browser,
  which macOS 26 was last seen to follow for 0.1.
- **1.0.4, given out on 2026-10-14** as the Mac's half of the release `v1.0.4`, a
  pre-release until Windows' file is added (decision 96). Version 1.0.4, build 8.
  The image is `Pro7ChordEditor-mac-1.0.4.dmg`, 6,351,400 bytes, sha256
  `dcc1d388…5e77`, made by the script from the commit `09fb480` with nothing else
  in the working tree. Checked: the script's own checks; the read-me read through,
  and nothing in it untrue; the app in the image the same, file for file, as the
  copy that was started, and its seal holds; started twice, shut in, with a new
  made-up home: two windows the first time, the guide's among them, and one the
  second, and the settings it wrote have nothing for the signs, which are on as it
  comes; all 390 tests, against the optimised code as well as the other; GitHub's
  two builds of the branch and of the tag; and the file fetched back from the
  Releases page, the same bytes as the one sent. **Not done**: nobody has opened
  it by hand, nor through a browser's download.
- **A build can be asked for its menus.** Started with `PRO7_SAY_MENUS` set, the
  program writes its menu bar out as it is after the tidying, once at the start and
  once three seconds on. Shut in, 1.0.0 wrote: the app's own menu (About, Settings,
  Services, Hide, Hide Others, Show All, Quit); File: Close [Command+W], Revert to
  as Opened; Edit: Undo, Redo, Cut, Copy, Paste, Select All; Window: Minimize, Zoom,
  the two windows by name, Bring All to Front; Help: the guide [Command+?]. No
  View. **The Window menu gives neither window a key**, which settles what was
  asked under "Left rough" since the guide: Command+1 and Command+2 are the
  editors' alone.
- **The two pictures in the lists are ProPresenter's** (`ListIcons.swift` says
  whose, and that they are here at the maintainer's asking). In a repository that
  is one thing; in an app handed to other people it is another, and is the
  maintainer's to weigh before a build goes on the Releases page.

## Left rough, and what to ask

- **Of changes allowed while ProPresenter is running (decision 97), tried by no
  hand**, there being no build with them:
  1. the Advanced tab, and the question when the switch is turned on: Esc and
     Cancel leave it off, and only a click on "Allow Changes" turns it on;
  2. with ProPresenter running, the band in its other words, and a chord put on
     and the key set as with it quit;
  3. **what ProPresenter does** with a song changed under it (`spec/06`, rule 16,
     is still a guess): only the maintainer's ProPresenter can say, on a made-up
     song in a copy of a library ("Asked for by the maintainer, for later", step 5,
     has the ten minutes it would take).
- **Of the signs for sharps and flats (decision 95), tried by no hand**: they are
  in 1.0.4, which nobody has opened yet:
  1. how they read at the sizes the sheet zooms to. The system font's ♭ is
     small and raised, and its ♯ is close to a `#` at the bubble's size;
  2. typing: `F#` shows F♯ as the `#` is typed, `C7b9` its ♭ once the 9 is, and
     the caret never jumps;
  3. the switch in Settings: the sheet, the strip and the song's keys change at
     once, and stay as they were left when the app is started again;
  4. the key pickers' lists, opened, with the signs.
- **Of what was put right on 2026-10-14 (decision 93), tried by no hand**: it is in
  1.0.4, which nobody has opened yet:
  1. a song in a key that is not one of the twenty-six, C# major say: its key is
     to be seen in both pickers of its header, and to be the first thing in each
     one's list when it is opened. (A picture has the first; no test can open a
     picker.)
  2. Earlier Versions, with the copy picked being what the song already is: Put
     This One Back puts the panel away, and the line under the song offers
     nothing to undo.
- **1.0.3 was tried by the maintainer on 2026-10-14** ("tips are fine"; the
  README's screenshot is of it), and then given out. Which of these were among
  what was tried was not said, so they stand (decisions 86 and 87):
  1. the line of hints: is the blue right, in the light and in the dark? Does it
     light up when the words change, and is that pleasing or a flicker as the
     pointer crosses chords? Are the keys easy to pick out?
  2. the pointer over the seven chords at the top: the line says they can be
     dragged; off them, it stops saying so; and while one is dragged over a
     word, what letting go would do;
  3. Option held on a line: nothing changes. Command held: the two sentences;
  4. the chords in orange, and the highlighted line in the same orange: is that
     too much of one colour?
- **1.0.2 was tried by the maintainer on 2026-10-14**: "It feels wonderful now! all
  smooth now.. and looks great". So the pointer, the edge and the bar are as
  wanted. What stood here to be tried of it:
  1. the pointer over the sheet, and a chord dragged: is it as quick as 1.0.0 was,
     or quicker? It was timed off the screen; on it, only a hand can tell;
  2. the edge between the lists and the song, with a song open, on the sheet and
     on the ChordPro text: is it smooth? Is the bar easy to grab, and does the
     pointer change shape as soon as it is over it? Is a bar that is seen what is
     wanted there, or was a hairline?
  3. the song's header in a narrow window: the name and the switch, and under them
     the keys; narrower still, three rows. Is anything out of place?
- **1.0.1 was tried by the maintainer on 2026-10-13**, who found it slow (above).
  Of the rest of what it brought nothing was said, so this stands:
  1. the edge between the lists and the song: it starts a little left of where it
     did; drag it, and see the pointer change over it; make the window wider and
     narrower, and the lists keep their width; quit and start, and it is where it
     was left; click it twice, and it is back at its start;
  2. the highlighted line: the words of the line the pointer is on go orange. Is
     that too much, as the pointer crosses a slide? Is the spot's own orange
     outline lost against it? Settings > Chords has its colour;
  3. hold Option on a line: the two sentences. On a line with chords the second
     ends "(Replaces existing chords)". In a narrower window they go one under
     the other, and the sheet is a line shorter while Option is down: does that
     jar?
  4. with Option still down, Command+C and Command+V: they work;
  5. a line of chords with no words: its rule is what goes orange;
  6. a song whose line of chords alone is given a chord more, or one fewer, **and
     then looked at in ProPresenter**: the capitals of its box now run to the end
     of the line as it is (decision 79). What ProPresenter makes of it is a guess.
- **1.0.0 was tried by the maintainer on 2026-10-13.** What was said of it is in
  decisions 80 to 82: the hints "appear at the right time", the tips by the clock
  were not wanted, the copying wanted other words and a line to be about. Of the
  rest of this list nothing was said, so it stands:
  1. the line of hints: is what it says right on a chord, on a word, typing and
     carrying?
  2. click a chord, clear the box, press Return: the chord goes. And clear it and
     click another word;
  3. close the window with the red button, and with Command+W: the app quits, and
     what was being typed in the ChordPro text is saved. With the guide's window
     in front, Command+W closes only the guide;
  4. Command+S: the quiet line says nothing is saved by hand, though no menu has
     Save;
  5. the menus, as they are listed under "The disk image"; and whether View comes
     back with only Enter Full Screen in it, which this session could not see;
  6. About: does it show the icon?
  7. the blue of Earlier Versions, in the light and in the dark, and the title over
     the places.
- **The icon has been seen in no Dock.** What macOS shows for the built app was
  drawn into a picture and looked at (decision 68). To look at: the Dock, the
  Finder, the app's About window, and the image's own window when it is opened.
- **The guide's window has been seen by nobody** (decision 64). It was opened once
  by the app shut in (decision 70), which told that it opens at the first start
  (1, below) and not at the second (3), and no more. Its page was drawn into
  pictures and looked at, light and dark, wide and narrow. To try, and to say of
  each what is odd:
  1. start the app: the guide opens, once the libraries are listed, in front of the
     main window and towards the top right of the screen. (It will, the first time,
     on a Mac that had the app before: nothing says yet that it was shown.) Is that
     where it should open, and is it the size it should be?
  2. with it open, click the main window and put a chord on: the guide stays open,
     behind or beside;
  3. quit and start again: it does not open;
  4. Help > Pro7 Chord Editor Guide opens it, and so does Command+?; and the ? at
     the right of the strip. Asked for while open, it comes to the front and there
     is still one of it;
  5. close the main window and ask for it under Help: it still opens;
  6. **the Window menu**: the guide is listed there while it is open, which is the
     system's doing. Does the system also put an entry there that opens it, and
     has that entry taken a key? **Command+1 and Command+2 must still show the
     sheet and the text**;
  7. quit with the guide open, and start again: it should not come back. (That is
     asked of the system for this window, and was not seen to work.)
  8. the words themselves: `spec/13-guide.md`, "The words", has them to be read
     without the app. What is wrong, missing or too much is changed in
     `spec/guide.json`, for both apps at once.
- **The pictures in the guide are the sheet at its usual size on a Mac with no
  ProPresenter colours in the song.** They are of the test song, and say nothing of
  how a real one looks.
- **A chord dragged out of the strip has not been tried by a hand** (decision 60).
  To try, with ProPresenter quit, and to say of each what is odd:
  1. press a button of the strip, pull it down over a word and let go: is the chord
     there? Does the button's picture come with the pointer, and the lit bubble
     show where it will land, as it does for a chord dragged along the sheet?
  2. let go between two slides, on a frame's foot, and back on the strip: nothing
     is put on, and the picture goes back to the strip;
  3. Esc while it is carried: the same;
  4. a click on a button (no pull) still puts the chord on the spot, and the
     button shades as it is pressed;
  5. click a word so that the bubble opens, type "G", and then drag a chord from
     the strip to another word: G is kept where it was typed, and the other lands
     where it is let go;
  6. on a long song, hold the chord near the bottom of the sheet: does it scroll,
     and at a speed that can be steered? (The speed is a guess: `carriedFastest`.)
     And scrolled down, does bringing a chord in over the top leave the sheet
     where it was?
  7. the button's tooltip, and whether it gets in the way of a press.
  And whether a chord let go off the cards ought, after all, to land on the last
  place it was over, as one dragged along the sheet does.
- **What M7 writes has been seen in ProPresenter by the maintainer** (2026-10-12:
  "They showed"), which was the plan's third proof. Which of the five things below
  were looked at was not said, and what was not is still to be tried, on a song made
  up for it or a COPY of one of their own, with ProPresenter quit while the chords
  are put on:
  1. a chord more on an intro's line of chords, and one taken off: does the stage
     screen show the line's chords as before, each over its place?
  2. a first chord on a slide whose text box is empty: are the chords there, and is
     the slide still blank to the audience?
  3. a first chord on a slide with nothing on it: the same; and does the slide
     still do whatever it was there to do (start its video, run its macro)?
  4. each of those opened in ProPresenter's own editor: does the text box look and
     behave as one of its own, and is anything odd when words are typed into it?
  5. the song saved from ProPresenter after that, and opened here again.
- **The two new sentences** (decision 55) have been read by nobody but their writer.
- **Simple Presenter is to be given decision 58**, by the maintainer's other
  session. Until it is, a line emptied there is dead (no stand-in), and emptied here
  keeps one: the same file, left differently by the two.
- **A blank slide in the ChordPro text is an empty line after an empty line.** It
  takes chords and saves them, and the tests hold that. Whether anyone can find the
  right one of the two by eye has not been tried (`spec/04`, rule 3).
- **RTF as ProPresenter on Windows writes it is still not known.** If it is the kind
  decision 51 refuses, a library kept on Windows will say "written in a way that
  cannot be changed here safely" for every chord added to a line with no words.
  Renaming would still work. The Windows session is to look first.
- **The "Earlier Versions" button is cut short** ("Earlier Ver...") in the song's
  header at some widths, once a song has a copy kept: seen in picture 34. Not of
  M7's making, and not yet looked into.
- **Command+S and the File menu's Save and Revert to as Opened are the app's own
  few lines**, which no test of the package reaches: they are built, and that is
  all. To be pressed by a hand; and that Revert in the menu greys and ungreys as
  the button does.
- **The question Revert asks has been seen by nobody**: its words are tested, and
  an alert cannot be drawn into a picture off the screen. To be seen: that it is
  over the window, that Cancel is what Return gives, and that the button that
  reverts is marked as the one that destroys.
- **Nobody has pressed the new switch, or Command+1 and Command+2, or the keys that
  size the ChordPro text**: the switch is in the pictures and its model is tested;
  the text's keys are tested by handing the text view a key. To be seen by a hand:
  that the orange slides from one button to the other, and that the keys reach them
  whichever of the lists, the sheet or the text has the keyboard.
- **The bar between the two lists has been dragged by nobody**: its arithmetic is
  tested and it is seen in the pictures, and that is all. To be tried: that it
  follows the pointer, that the cursor changes over it, that two clicks put it back,
  and that the edge between the lists and the song still drags sideways.
- **Nobody has seen the grid or the zoom with their own eyes but in pictures** (24 to
  27 are of them). To be tried: that making the window wider and narrower lays the
  slides out again without a stutter; the two buttons, Command with minus, plus and
  0, and a pinch; that Down and Up go where the eye expects with slides side by
  side; whether 80 in the hundred is the right usual size; whether every slide
  should have the foot or only the first of a group.
- **A song of long lines is one slide to a row**, and a very long line scrolls
  sideways (89 and 45 of 422 at the usual zoom). What would help: lines that wrap,
  which the page forbids and which chords over a wrapped line make hard.
- **Nothing may be saved while ProPresenter is running, wherever the file is**, which
  the maintainer wants looked at again and has put on hold: "Asked for by the
  maintainer, for later" has what was thought and what is to be found out first.
- **An empty line in the middle of a slide's words takes a whole line's height**,
  chords' row and all, though no chord can go there.
- **`spec/pictures` are of Simple Presenter's sheet in one column.** Once the
  maintainer likes the grid, pictures of it for the page and the README can be drawn
  by the picture tests.
- **Nobody has used the sheet with a hand**, which is what M6 is done by. Everything
  of it has been proved by tests that call the view's handlers and seen in pictures
  (14 to 27). **To be tried, on a COPY of a library**: chord a verse by pointing and
  pressing 1 to 7; a run of chords click, type, click, type; Tab along a line; drag a
  chord, and with Option; Command+C on a verse's first line and Command+V on the
  next verse's; Command+Z after each. And to be said: what is slower or odder than
  in Simple Presenter; whether the keyboard going to the sheet under the pointer is
  welcome (decision 32); whether a click on a chord in the strip, or on the key's
  picker, takes the keyboard from the box a chord is being typed in (it must not:
  rule 39; no test can tell, since it is the system's doing).
- **The sheet's commands are not in the menu bar** (`spec/10` asks for every command
  there). Undo, Redo, Copy and Paste in the Edit menu reach the sheet; copying a whole
  group (Shift+Command+C) has only its keys.
- **The caret of the ChordPro text and the sheet's picked slide do not know of each
  other** (`spec/04`, rule 21): going from one editor to the other starts at the top.
- **The sheet is dark whatever the system is**, as `spec/10` says, in a window that
  is otherwise the system's. Whether it should have a light form is the
  maintainer's to say now that it exists.
- **"The song names no key: pick the one its chords are in"** is cut short or gone
  where the pane is narrow: the strip's chords come first.
- **The sheet is one thing to VoiceOver**, with a name and nothing inside it.
- **While a chord is carried the sheet scrolls only when the pointer is moved at its
  edge**, as any view does; it does not go on scrolling under a pointer held still.
- **Nobody has typed a chord into this app with a hand.** Everything of M5 has been
  proved by tests on made-up Macs and seen in pictures; the app itself has not been
  run by the session that built it, since on this Mac it would open the maintainer's
  own library with a build that can write. **To be tried, on a COPY of a library
  first**: that typing keeps up; that Command+Z and Shift+Command+Z take back and
  put again; pasting; Find (Command+F); a dead key (Option+E, then E) in a chord and
  in a word; the sound when a key is not taken, and whether it is welcome; that
  switching songs, leaving the app and quitting each save what was typed; the two
  keys; Earlier Versions and Put This One Back, which had no copies to show until
  now; and that a song so changed shows its chords in ProPresenter.
- **Two empty lines look alike** where a blank slide is (`spec/04`, rule 3): to be
  tried with M7, when one of them can take a chord.
- **A chord the file has on an empty line of a box** is shown on that line and is
  dropped if the box is saved, as in the reference (`spec/04`, rule 16 has no
  character to hang it on). Not seen in a real song.
- **All Copies reads every copy's folder each time the settings open.** Fine for
  hundreds of songs; if it is ever slow, read it off the thread that draws.
- **What only a hand on a mouse can tell, and nobody has yet** (M3): that the app
  opens and finds the library; what macOS asks the first time it reads Documents;
  that the lists keep up with typing in the search box and with clicks; that the
  band comes and goes as ProPresenter is started and quit; that the window's size is
  remembered; that Choose Folder does what it says. The pictures show the window as
  drawn, not as used.
- **Not in M3, on purpose**: the mark "wants mending" (M8); keeping a chord being typed when another song is clicked (rule 13: there is
  no typing yet); playlists, search by a song's words, dropping a file on the window
  (the page's "later").
- **The window's looks are a first go** at the page's proposal, to be tried and
  changed with the maintainer.
- **`Generated/` is held to the submodule only where the submodule is**
  (`GeneratedReaderTests`, 2026-10-12: the version of ProPresenter that
  `proto/autogen-proto/version.txt` names must be the one the code says it was made
  from). So moving the submodule on and forgetting to generate fails a test on the
  machine that did it. It does not see a change to the descriptions that leaves that
  version as it was, and GitHub's Mac, which has no submodule, sees nothing. The
  workflow could generate again and compare, but would then need the same `protoc`
  and `protoc-gen-swift` as made the code, to the version. Not worth it yet. (The
  Windows session asked, on its first day, why the Mac keeps its generated code
  where Windows makes it at each build: `spec/10` now says, under the Mac.)
- **A presentation that ProPresenter itself made, of words anyone may use**, is asked
  of the maintainer before M9 by the plan. It is wanted before M2: saving is proved
  there, and every test file so far was written by Simple Presenter. A few slides of
  Amazing Grace typed into ProPresenter would do.
- **From M3 on**, macOS may ask again for leave to read Documents each time the app
  is rebuilt, since an ad hoc signature is a new one each build. **A guess**, not yet
  seen. If it does, signing with the maintainer's own Apple Development certificate
  stops it.

## Asked for by the maintainer, for later

- **More help with typing a chord in the ChordPro editor** (said 2026-10-11, after
  first trying M5; "this can be for later"). Three things, in their words and mine:
  1. *Once a chord's letter is in, the next character should only be what makes sense
     for a chord.* As built, what is between brackets is held to `couldBecome`: a note,
     then any letters, digits and the signs a chord's name is written with
     (`spec/02`). So `[e_]` is already refused, but `[Exyz]` is let through, since any
     letter may follow. Holding it tighter needs a rule of what a chord's *quality* may
     be, which the rules do not have: `couldBecome` is the reference's, and the vectors
     hold it. So this is a change to `spec/02` or `spec/04`, for both apps.
  2. *Typing `ma` should offer `maj`*: what is offered while typing, as the sheet's
     bubble offers it (`completions`), shown at the caret in the text.
  3. *Perhaps the case put right as it is typed*: `[e]` made `[E]`, as the sheet's
     bubble does (`tidied`). Today the text keeps what was typed and the SAVE tidies
     the name, so the text and the file differ until the song is opened again.

  None of it is started. All three change what both apps do, so `spec/04` is changed
  first, with the maintainer's yes.

- **Saving while ProPresenter is running, where that is safe** (raised by the
  maintainer on 2026-10-11, and the same day put on hold: "hold off on the editing
  while pro is running (more thoughts to sort through ...) - just keep notes for when
  I revisit this"). **Since 2026-10-17 there is a switch for it all** (decision 97:
  the person allows changes everywhere, having been warned), and **what follows is
  still not built**: telling the songs where it is safe from those where it is not.
  What was said, for whoever picks it up:
  1. *What the rule is for.* Not our own save, which is safe already: the file is
     written beside itself and swapped in whole, and is refused if anything else has
     changed it since it was read (`spec/05`). The harm would be ProPresenter holding
     a song in its memory and later writing its old copy over ours, so that chords
     put on here are lost without a word; or showing the old song until it is
     started again. That it does either is still **a guess** (`spec/06`, rule 16).
  2. *The maintainer's first idea*: "it's okay to edit a file outside of the active
     workspace - or even outside the active playlist".
  3. *Three cases, from safe to not known.*
     - **A file that the running ProPresenter cannot have open**: one that is not
       under the folder it is running on. A copy of a library on the Desktop, a
       folder chosen by hand, another workspace than the one in use. Safe by what it
       is. This alone would end "quit ProPresenter first" for trying things on a
       copy, which is where the rule is felt most.
     - **A file in the folder it is running on, that it has not loaded**: not in the
       playlist on its screen, not the song picked in its library. Probably safe,
       and **a guess**: nothing outside ProPresenter can see what it has loaded, and
       the person can click any song at any moment.
     - **A file it has loaded, or is showing.** The likeliest to go wrong.
  4. *Which folder ProPresenter is running on depends on which ProPresenter it is*,
     and this has to be right before the first case can be trusted:
     - ProPresenter 21 on the Mac has workspaces, listed in its settings with one
       marked as in use (`spec/06`, rule 3). The settings are to be read again at
       the moment of saving, not remembered from the start.
     - Older versions of ProPresenter 7 had no workspaces. The maintainer: "older
       version had no workspace - but a pref pointing to a folder (default was
       documents\propresenter\)". For those the folder it is running on is that one
       folder (`PathSettings.proPaths` on Windows, `spec/06`, rule 3).
     - **How a version keeps its songs is to be looked at afresh for every version
       before the rule is relaxed for it.** This app works on places whose songs are
       `.pro` files in a folder and on nothing else (decision 19). A place that is
       not such a folder is to be spotted and left alone, and where it cannot be
       told what a running ProPresenter is working on, nothing is saved, as now.
  5. *Finding out, in place of guessing, for the second and third cases.* With a
     made-up song in a library of the folder ProPresenter is running on: change a
     chord from outside while the song is (a) not opened since ProPresenter started,
     (b) opened earlier and not picked now, (c) picked in the library, (d) in the
     playlist that is on its screen, (e) live. After each, see whether ProPresenter
     shows the new chord when the song is next picked (a stage screen shows chords);
     then make ProPresenter save that song (change a slide in its editor; quit it)
     and read the file to see whether the chord is still there. Ten minutes, and it
     needs the maintainer's ProPresenter and eyes. The build that does it would need
     a way round the rule that no released build has.
  6. *ProPresenter can be asked over the network*, where that is switched on in its
     settings, which presentation is in front and which playlist is open. That could
     tell the third case from the second. It is another thing to go wrong and
     another setting to depend on: worth it only if (5) shows that what is loaded is
     what matters.
  7. *What I would build first*, if asked: the first case only, with the rule kept
     as it is everywhere inside the folder ProPresenter is running on, and kept
     everywhere if that folder cannot be told for certain. The two nets stay
     whatever is allowed: the copy kept before a file's first change, and the
     refusal to save over a file something else has changed. If the second or third
     case were ever allowed, a third net: a look at the file a moment after it was
     saved, to say so if it is no longer what was written.

- **Saving by hand, as a setting beside saving at once: NOT TO BE BUILT, by the
  maintainer's choice of 2026-10-11** ("autosave indicator and Revert", decision
  48). What follows is what was said before the choice, kept for why. (The
  maintainer, 2026-10-11:
  "let's add a setting for auto/live save vs manual save and default - I'm not sure
  which to default - the old app was a manual save... got any thoughts/feedback ...
  I lean towards defaulting to auto save (but maybe show the save button in a
  disabled state with a caption like 'Autosave Enabled'".) What was said:
  1. *As the app is built, saving at once is not a habit laid over it but what its
     safety is made of.* Every change goes through one door (`OpenSong.save`): the
     safeguards are asked, a copy is kept before the first change, the file is
     written whole and read back, and only then is the change the song's; a refusal
     leaves nothing half done. Undo is a walk back through states that were each on
     the disk. Earlier Versions puts back the file as it was before the sitting.
  2. *Saving by hand needs a second state that none of that has*: a song changed in
     memory and not on the disk. `OpenSong` would hold changes until told to write;
     undo and redo would work in memory; the two editors' own saving would stop at
     the song. And everything that leaves a song would have to ask first (another
     song, another library, the window, quitting, a copy put back): Save, Don't
     Save, Cancel. That asking is most of the work, and all of it is in the part of
     the app where a mistake costs a song.
  3. *What it would give that saving at once does not*: changes could be made while
     ProPresenter is running and saved after it is quit (the rule is about writing,
     not about thinking); and a sitting could be thrown away whole. The second is
     already there by another road: the copy kept before the first change is the
     song as it was opened, and one click puts it back.
  4. *What it would cost the person*: changes lost if the app is quit by force or
     crashes; and a file that something else changed meanwhile can no longer be
     saved over, with a whole sitting in hand and not one chord.
  5. *My advice*: keep saving at once as the only way, and show it: a quiet mark by
     the song that says its changes are saved (or the maintainer's disabled Save
     button with its caption), and a plain "Revert to as opened" beside Earlier
     Versions. If saving by hand is still wanted after that, it is a piece of work
     of its own, with `spec/05` changed first for both apps, and its own proofs.
