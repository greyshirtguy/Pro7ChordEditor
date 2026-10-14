import QtQuick
import QtQuick.Controls.Basic
import SimplePresenterApp
import "chordlayout.js" as ChordLayout

// The chord editor: the whole song as one sheet of words, with its chords in bubbles
// over them, to be put on, changed, moved and taken off with the mouse and the keys.
// ProPresenter has nothing like it (it shows the chords a song came with and no more),
// so there was nothing to mirror, and this is this app's own. The same editor is a
// program of its own for the Mac and for Windows (Pro7 Chord Editor), which began as a
// copy of this one; what was found and thought of in building that has come back here,
// and the two are meant to work and look alike.
//
// One sheet, where the file has slides. In the file each slide has a text box with its
// own words, and a chord is a note on a stretch of one text box's characters
// (proconvert::readChords). Working a slide at a time would mean paging through thirty
// of them to chord one song. So the editor (PresentationEditor::song) hands the text
// boxes over in order and every slide of the song is on the one sheet. Nothing is
// joined up underneath: a chord that is put on a word is written to that word's own
// text box, on its own slide.
//
// The slides are a grid, like the slides of a presentation in the operator window, but
// each is its words at a size to be read, with room over every line for its chords. A
// slide is a card in a frame of its group's colour, with the slide's number on the
// frame's foot and, on the first slide of each group, the group's name. They go across
// the sheet in the order of the song, as many to a row as fit, and the sheet zooms: a
// wider window or smaller words show more of the song at once. (They were one column at
// first, every card as tall as the slide with the most lines. Tried on a library of
// four hundred songs, a window then showed some three lines of a song's words and was
// half empty; as a grid it shows nine or so.) All the cards of a song are one width,
// that of the widest slide and no wider, and the shape of a screen, though never taller
// for that than four lines need; a row is taller only where a slide of it has more
// lines than that holds. No room is kept for what might be put on a line later: a line
// that comes to need more, for a chord at its end, is given it then.
//
// The words cannot be changed here, only chords. (A line of chords with no words, an
// intro's, is the exception: its chords hang on stand-in characters that are made and
// unmade with them. See chords::placeholders.)
//
// A blank slide is on the sheet too, as a card with one empty line: a line of chords
// alone that has none yet. An intro or an instrumental is often a slide with nothing
// on it, and this is where its chords are put. Nothing is done to the slide until
// then: the first chord gives its empty text box its stand-ins, or, where the slide
// has no text box (the row's `element` is then empty), adds one for them; and taking
// the last chord off again unmakes whichever it was. All of that is the document's
// doing (PresentationEditor::song and setChordsAlone): here it is a line of chords
// alone like any other.
//
// The spot. There is one place on the sheet that the next chord goes to, shown as a
// bubble in outline with its tail on a character. It follows the pointer, and the arrow
// keys move it too, so that the mouse and the keys are two hands on one thing: point
// with one, press with the other. Under the pointer it goes to any letter, since a
// chord can fall anywhere in a word (the file keeps it on a character, and songs from
// Multitracks have plenty in the middle of words); with Shift held it keeps to the
// starts of words. The arrow keys are the other way about, a word at a time and with
// Shift a letter at a time, a letter at a time being a long walk along a line. Down and
// Up go by the screen: to the line under or over, which from a slide's last line is in
// the slide below it in the grid, not the next slide of the song (that one is beside
// it, and Right goes there).
//
// The highlighted line. The line the person is at has its words drawn in a colour of its
// own, the sheet's orange as it comes (a setting): the line a chord is being typed on;
// else the spot's line, while the pointer is over a place of the sheet, or the keys
// have moved the spot since it last moved, or a chord is carried there. It is the line
// Ctrl+C copies from and Ctrl+V pastes to, which is why it is shown: without it the
// spot, a small thing, was all that said which line that is. (The Mac editor's, its
// pages' rule 53f, and so the same here; see `highlighted`.)
//
// Sharps and flats are drawn as the signs, ♯ and ♭, wherever a chord's name is drawn:
// F♯m7, B♭, C7♭9 (see drawn()). Only drawn: the song's file, what is copied and carried,
// and the ChordPro text keep "#" and "b". A setting turns it off.
//
// The line of hints. Under the sheet, a blue line says what the keys and the mouse do
// just now, where the spot is: on a chord, that Delete takes it off and 1 to 7 change
// it; while one is typed, what Enter, Tab and Esc do; with Ctrl held, how a line's
// chords are copied onto another, which nothing on the sheet shows. src/sheethints.h
// has what is said when, and why; qml/HintLine.qml draws it; here is what is going on
// (`hintsNow`). The line along the bottom of the editor, under it, says how much there
// is of the song (`howMuch`), and for a few seconds what was just done.
//
// Getting a chord on quickly, which is what this is for:
//   - 1 to 7 put the key's own chord for that note of its scale on the spot (in C: 1 is
//     C, 4 is F, 6 is Am). Most of most songs is those. The strip along the top shows
//     them, and they can be clicked there too.
//   - Or one of the strip's seven is dragged down onto a word, on any slide, and let go
//     there. It is slower than a key, and it is what a hand does unasked (it was asked
//     for in the Mac editor by a hand caught at it). It lands only where the lit
//     bubble shows it would: let go between two slides, or anywhere else, nothing is
//     put on. See "A chord carried in from the strip".
//   - A letter A to G starts a chord by name. A bubble opens for typing it, which
//     takes only what could be a chord, and under it are the chords it might be going
//     to be: the song's own first, then the key's, then the usual kinds on that note
//     (chords::completions). Down and Up pick one, Enter takes it, Tab takes it and
//     moves on to the next word, a click takes it; Right puts the picked one in the
//     box to be gone on with (for a bass note, say). A box emptied and kept takes off
//     the chord that was there; Esc alone leaves it.
//   - A click on the spot, or Enter, opens the same bubble on what is there.
//   - While a chord is being typed the spot goes on following the pointer, and a click
//     on another place keeps what was typed and opens the bubble there. So a run of
//     chords is click, type, click, type, with no key between to say "done". Typing
//     is only ever thrown away by Esc: a click anywhere else on the sheet keeps it too.
//   - A chord is dragged to another word, on any slide; with Ctrl held it is copied.
//     Delete takes off the one at the spot.
//   - Ctrl+C copies the chords of the spot's line and Ctrl+Shift+C those of its whole
//     group; Ctrl+V puts them on the lines from the spot's on, word for word: the
//     chord of a line's third word goes on the third word of the line it is pasted to.
//     That is how a second verse gets the chords of the first. A paste says what came
//     of it, since by its own rules it may rightly change nothing.
//   - Ctrl with minus and plus, the two buttons at the strip's right, Ctrl with the
//     wheel and two fingers spread or closed on a trackpad make the words smaller and
//     larger; Ctrl+0 is the usual size.
//
// Each change is saved at once and can be undone, like every change in the editor.
//
// How it is put together. The rules are the functions at the top, which know nothing
// of where anything is drawn: the rows, the spot, what each thing done asks of the
// document. (They are run as they stand, with no window, to make the vectors that the
// Mac and Windows editors are held to; so they keep their names, and what they touch.)
// Under them is the layout, in two steps so that neither is done oftener than it need
// be: measure() works out where the chords of every line stand and how wide each slide
// needs to be, which changes with the song and the zoom; arrange() puts the slides in
// their rows, which changes with the width as well. Then the drawing: a frame for each
// slide, an item for each line with its bubbles, the spot, and the bubble a chord is
// typed in.
FocusScope {
    id: sheet

    // The PresentationEditor that has the song open
    required property var editor
    // The colour of a group, given { group, groupColor } (the operator window's)
    property var groupColor: (slide) => "#2b2d31"
    // The slide to have in view: its row in the editor. It is the one that is ringed.
    property int row: 0
    readonly property color accentColor: "#ff8a1f"
    // The colour of a chord's name: the one it comes with, the sheet's own orange, or
    // the one chosen in the settings. Only how this app draws a chord: nothing in a
    // song's file says a colour. (It came pale, "#ffd9a8", until the Mac and Windows
    // editors made theirs orange on 2026-10-14, the maintainer's: "lets make the
    // default chord color orange now".)
    property color chordColor: accentColor
    // The colour of the words of the highlighted line (see `highlighted`): the sheet's
    // orange as it comes, or the one chosen in the settings.
    property color lineColor: accentColor
    // Whether sharps and flats are drawn as ♯ and ♭ (see drawn()). A setting, on as it
    // comes: off, the sheet is as it was before there were signs, the box a chord is
    // typed in too.
    property bool signs: true
    // The key the chords are in. A song that names none is taken to be in C, which is
    // only what the keys 1 to 7 give until the key is set.
    readonly property string key: editor.key !== "" ? editor.key : "C"
    readonly property var inKey: Chords.diatonic(key)

    // How many times its usual size the sheet is to be drawn: one of `zooms`. It is the
    // app's, kept from one start to the next, and nothing in any file says it; whoever
    // owns it hears of a change asked for here by zoomEdited() and sets it.
    property real zoom: usualZoom
    readonly property var zooms: [0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.1, 1.25, 1.5]
    readonly property real usualZoom: 0.8
    // The size it is drawn at now: `zoom`, once the sheet has been laid out for it.
    // Every size below goes by this and not by `zoom`, so that all of them have changed
    // by the time the lines are measured again (see resize()).
    property real times: usualZoom

    // The text boxes, as the editor gave them, and the sheet's rows made of them: a
    // { kind: "group", name, color } before a group's first slide (it is given no place
    // on the sheet, its name being on that slide's frame, and is there to say where a
    // group begins), and for each line a
    // { kind: "line", block, line, row, element, text, start (where the line starts in
    // its text box), chords (each `at` along the line), alone (chords with no words),
    // blank (a blank slide's one line: alone, with no chords yet and no stand-ins),
    // first (of its slide), last, count (how many lines its slide has) }
    property var blocks: []
    property var rows: []
    property var used: []
    // How many lines the slide with the most has. (Nothing is drawn by it since the
    // slides became a grid, where a row is as tall as its own slides need. It is kept
    // because it is one of the things the Mac and Windows editors are held to.)
    property int mostLines: 1

    // The spot: which row, and which character of its line. On a line of chords alone
    // it is a chord's stand-in (every other character) or, past the last, a new one.
    property int spotRow: -1
    property int spotAt: 0
    // Whether the person is at the sheet just now: the pointer has moved over a place of
    // a line, or the keys have moved the spot or acted on it since the pointer last
    // moved. Over the bare sheet between the slides, or out of the sheet, the spot is
    // only where it was left, and nobody is at it. (The Mac and Windows editors' rule
    // 53c, where it also says when their line of hints speaks of the spot.)
    property bool attending: false
    // The highlighted line: the row whose words are drawn in `lineColor`, as the line
    // the person is at. The line a chord is being typed on, wherever the pointer has
    // gone since; else the spot's line, while a chord is carried to it (one of the
    // sheet's own lands at the spot wherever it is let go; one from the strip only
    // where the lit bubble shows it would) or the person is at the sheet; else none,
    // -1. It is the line Ctrl+C copies from and Ctrl+V pastes to, which is why it is
    // shown. (The Mac editor's, 2026-10-13, from the maintainer: "we need to highlight
    // the hovered/active line of text - let's make the text highlight by changing
    // color - default orange (a new setting)". Their pages' rule 53f.)
    readonly property int highlighted: typing ? typingRow
        : dragged !== null ? (carrying && !carriedOver ? -1 : spotRow)
        : attending ? spotRow : -1
    // Whether Ctrl is held: at the sheet it brings up, on the line of hints, the copying
    // of a line's chords, being the key for shortcuts here as Command is on the Mac (the
    // maintainer's, 2026-10-14: "it be shown when user holds ctrl key on linux"). It is
    // told by the key itself, to whichever has the keyboard, the sheet or the box a chord
    // is typed in; and, so that a key let go while something else had the keyboard does
    // not leave it held, by every move of the pointer over the sheet, and by the app
    // being left.
    property bool ctrlHeld: false
    // Whether the pointer is over the key's chords in the strip, which is not the sheet
    readonly property bool overStrip: stripHover.hovered
    // What the line of hints says (src/sheethints.h): from what is going on, for
    // SheetHints. (A new list is made for every place the spot goes to, and the line
    // changes nothing while it says what it said: see HintLine.qml.)
    readonly property var hintsNow: {
        const doing = typing ? "typing" : dragged !== null ? "carrying" : !attending || !usable(spotRow) ? "nothing"
                    : chordAt(spotRow, spotAt) === "" ? "onPlace" : "onChord"
        return SheetHints.hints(doing, typing && chordAt(typingRow, typingAt) !== "", typing && field.text === "", carrying, ctrlHeld,
                                copied ? copied.lines.length : 0, ctrlHeld && pasteWouldReplace(), overStrip)
    }
    // How much there is of the song, for the line along the bottom of the editor, under
    // the line of hints: "7 slides, 40 chords."
    readonly property string howMuch: {
        const chords = blocks.reduce((sum, block) => sum + block.chords.length, 0)
        return (editor.count === 1 ? "1 slide" : editor.count + " slides") + ", " + (chords === 1 ? "1 chord" : chords + " chords") + "."
    }
    // The bubble for typing a chord is open, and the place the chord being typed belongs
    // to. That place is not the spot: the spot goes on following the pointer while a
    // chord is typed, so that the next place can be pointed at before this chord is
    // done with (see pick()).
    property bool typing: false
    property int typingRow: -1
    property int typingAt: 0
    // Where the pointer last was, on the sheet. The spot follows the pointer only when
    // the pointer itself moves: a row that is laid out again under a pointer that is
    // lying still is told the pointer is over it, as is one that scrolls under it, and
    // neither is to take the spot back from the keys.
    property point pointer: Qt.point(-1, -1)

    function pointerMoved(item, x, y) {
        const at = item.mapToItem(sheet, x, y)
        if (Math.abs(at.x - pointer.x) < 1 && Math.abs(at.y - pointer.y) < 1)
            return false
        pointer = at
        return true
    }

    // Counts the rows as they are laid out again after a change. Whatever points at a
    // row's item (the spot's outline, the bubble a chord is typed in) reads this, so as
    // to look the item up again: the items are made anew whenever the song is read.
    property int laid: 0
    // A chord being dragged: { row, at, name }, or null. One carried in from the strip
    // has come from no place of the sheet, and its row is -1.
    property var dragged: null
    readonly property bool carrying: dragged !== null && dragged.row < 0
    // Whether the chord carried in from the strip is over a place that can take it,
    // which the spot is then at: only there does letting go put it on.
    property bool carriedOver: false
    // Where it is, as a point of the sheet, and where in its button it was taken hold
    // of: the button's picture goes with the pointer, held where it was picked up.
    property point carriedAt: Qt.point(0, 0)
    property point carriedHold: Qt.point(0, 0)
    property string carriedCaption
    // Whether it has once been clear of the top of what is seen of the sheet. The strip
    // is over the sheet, so every chord comes in over the top edge, and is not to
    // scroll the sheet upwards on its way in.
    property bool carriedClear: false
    // Which of the strip's seven the pointer is on, or -1; and what that one does, in
    // words, for whoever shows such things (the line along the bottom of the editor)
    property int pointedChord: -1
    readonly property string hint: pointedChord >= 0 && pointedChord < inKey.length && dragged === null
        ? "Puts " + drawn(inKey[pointedChord]) + " on the spot. The key " + (pointedChord + 1) + " does the same. Or drag it onto a word."
        : ""
    // What was copied: { lines: [ [{ word, offset, name }] or { alone: [names] } ] }.
    // It is the sheet's own, and is not put where the system keeps what was copied for
    // other programs, so as not to take the place of what was copied there. `copiedOver`
    // is the text the system had as last copied when this was (see copiedLast()).
    property var copied: null
    property string copiedOver

    // The sizes of things where the sheet is not zoomed, and at the size it is drawn
    readonly property int wordSize: Math.round(21 * times)
    readonly property int chordSize: Math.round(14 * times)
    readonly property real bubblePad: Math.round(7 * times)
    // The room inside a card round its lines; a line's words start that far in
    readonly property real cardPad: Math.round(10 * times)
    readonly property real gutter: cardPad
    // What does not zoom: a frame's edge and its foot, the room each frame keeps clear
    // all round it, and the sheet's own margin outside that. The margin at the top is
    // more: the bubble a chord is typed in grows upwards from its line, and over the
    // first row of slides it would have nowhere to grow to but down over the words.
    readonly property real frameEdge: 4
    readonly property real footHeight: 22
    readonly property real around: 6
    readonly property real sideMargin: 6
    readonly property real topMargin: 30

    signal failed(string error)
    signal told(string message)
    // A slide's row was pointed at: the editor shows it as the one picked
    signal rowPicked(int row)
    // The slide is being picked from here, by a click or the keys, and the sheet is
    // where it is to be already: it is only when a slide is picked from elsewhere (the
    // list at the side, an undo) that the sheet goes to it. A click on a slide that is
    // half in sight is not to move the sheet from under the pointer.
    property bool picking: false

    function pickRow(row) {
        picking = true
        rowPicked(row)
        picking = false
    }
    // Another size was asked for, by a key, a button, the wheel or a pinch
    signal zoomEdited(real zoom)

    function reload() {
        const read = editor.song()
        const flat = []
        const made = laid + 1
        let last = -1
        read.forEach((block, b) => {
            const newSlide = block.row !== last
            if (newSlide && block.groupStart && block.group !== "")
                flat.push({ kind: "group", name: block.group, color: block.groupColor, row: block.row, made: made })
            last = block.row
            let start = 0
            shownLines(block).forEach((text, l) => {
                const end = start + Math.max(1, text.length)
                flat.push({
                    kind: "line", block: b, line: l, row: block.row, element: block.element, text: text, start: start,
                    chords: block.chords.filter(c => c.at >= start && c.at < end).map(c => ({ at: c.at - start, name: c.name })),
                    alone: Chords.isPlaceholders(text) || block.blank === true, blank: block.blank === true,
                    first: newSlide && l === 0, made: made
                })
                start += text.length + 1
            })
        })
        // Each slide's lines know how many they are and which is the last, and the
        // sheet how many the longest slide has.
        let most = 1
        for (let from = 0; from < flat.length;) {
            if (flat[from].kind !== "line") {
                ++from
                continue
            }
            let to = from
            while (to + 1 < flat.length && flat[to + 1].kind === "line" && flat[to + 1].row === flat[from].row)
                ++to
            for (let index = from; index <= to; ++index)
                flat[index].count = to - from + 1
            flat[to].last = true
            most = Math.max(most, to - from + 1)
            from = to + 1
        }
        blocks = read
        mostLines = most
        // Laid out before the rows are handed over, so that the items made for them
        // find their places waiting.
        layOut(flat)
        rows = flat
        used = editor.usedChords()
        if (spotRow >= rows.length || (spotRow >= 0 && !usable(spotRow)))
            spotRow = -1
        // The line a chord was being typed on may have gone (an undo from the toolbar).
        if (typing && !usable(typingRow)) {
            typing = false
            takeKeys()
        }
        // And so may a chord that was being carried.
        if (dragged && chordAt(dragged.row, dragged.at) !== dragged.name)
            dragged = null
    }

    // A text box's lines as the sheet shows them: all but the empty ones its words END
    // with. Words that end with a line break, or several, would otherwise make their
    // slide that many lines taller, and so every card of its row. An empty line that a
    // chord stands on is kept, with any before it, so that the chord is not lost from
    // sight; and a box always has one line. Only what is drawn: the words in the file
    // are never touched by this.
    function shownLines(block) {
        const all = block.text.split("\n")
        // (Where the line being looked at starts: the whole length, less its own place.)
        let start = block.text.length + 1
        while (all.length > 1 && all[all.length - 1] === "") {
            start -= 1
            if (block.chords.some(c => c.at >= start))
                break
            all.pop()
        }
        return all
    }

    // A chord's name as it is drawn: with ♯ and ♭ for the sharps and flats that are
    // clearly so (chords::withSigns has which, and why), while `signs` is on. ONLY
    // drawn: what is put in the song, copied or carried is the name itself, and
    // everything here works on that. A song has a few names used over and over, and
    // every bubble is measured as well as drawn, so each name is asked of the rules
    // once and kept.
    property var signed: ({})

    function drawn(name) {
        if (!signs)
            return name
        let shown = signed[name]
        if (shown === undefined) {
            shown = Chords.withSigns(name)
            signed[name] = shown
        }
        return shown
    }

    // ---- Where a chord can go

    // A line with words on it, or a blank slide's, which has none and is there to be
    // given chords. An empty line among a slide's words is neither.
    function usable(index) {
        return index >= 0 && index < rows.length && rows[index].kind === "line" && (rows[index].text.length > 0 || rows[index].blank)
    }

    function wordStarts(text) {
        const starts = []
        const word = /\S+/g
        let found
        while ((found = word.exec(text)) !== null)
            starts.push(found.index)
        return starts
    }

    // Whether a place is the second half of a character that takes two (one from beyond
    // the first 65,536, an emoji say): no chord goes there, the pointer goes to its
    // first half and the keys pass over it.
    function secondOfPair(text, at) {
        const unit = text.charCodeAt(at)
        return unit >= 0xDC00 && unit <= 0xDFFF
    }

    // The places on a line the spot stops at: the starts of its words, or on a line of
    // chords alone each chord and one more for a new one (which on a blank slide's
    // line is the only one).
    function stops(index) {
        const line = rows[index]
        if (!line.alone)
            return wordStarts(line.text)
        const all = []
        for (let slot = 0; slot <= line.chords.length; ++slot)
            all.push(slot * 2)
        return all
    }

    // The stop at or before a character, which is the word it is in.
    function stopAt(index, at) {
        const all = stops(index)
        let best = all.length > 0 ? all[0] : 0
        for (const stop of all) {
            if (stop <= at)
                best = stop
        }
        return best
    }

    function chordAt(index, at) {
        if (!usable(index))
            return ""
        const found = rows[index].chords.find(c => c.at === at)
        return found ? found.name : ""
    }

    function setSpot(index, at) {
        if (!usable(index))
            return
        spotRow = index
        spotAt = at
    }

    // Moves the spot along the line and on to the next, by stops or, `exact`, by
    // characters.
    function step(delta, exact) {
        if (!usable(spotRow)) {
            const first = rows.findIndex((line, index) => usable(index))
            if (first >= 0)
                setSpot(first, stops(first)[0] ?? 0)
            return
        }
        const line = rows[spotRow]
        const all = exact && !line.alone ? Array.from({ length: line.text.length }, (_, i) => i).filter(i => line.text[i] !== " " && !secondOfPair(line.text, i))
                                         : stops(spotRow)
        let place = all.indexOf(spotAt)
        if (place < 0)
            place = all.findIndex(stop => stop > spotAt) - (delta > 0 ? 1 : 0)
        place += delta
        if (place >= 0 && place < all.length) {
            spotAt = all[place]
        } else {
            // Off the end of the line: the first stop of the next one, or the last
            // of the one before.
            let next = spotRow + delta
            while (next >= 0 && next < rows.length && !usable(next))
                next += delta
            if (usable(next)) {
                const there = stops(next)
                setSpot(next, delta > 0 ? there[0] ?? 0 : there[there.length - 1] ?? 0)
            }
        }
        reveal()
    }

    // Moves the spot to the line above or below ON THE SCREEN, to the stop of it that
    // is nearest under or over where it was.
    function stepLine(delta) {
        if (!usable(spotRow)) {
            step(1, false)
            return
        }
        const next = lineBeside(spotRow, delta)
        if (next < 0)
            return
        const x = lines[spotRow].x + along(spotRow, spotAt)
        let best = 0
        let distance = 1e9
        for (const stop of stops(next)) {
            const off = Math.abs(lines[next].x + along(next, stop) - x)
            if (off < distance) {
                distance = off
                best = stop
            }
        }
        setSpot(next, best)
        reveal()
    }

    // ---- Changing chords

    function report(error) {
        if (error !== "")
            failed(error)
        return error === ""
    }

    // Gives a line of words these chords in place of its own.
    function setLine(index, chords, joined) {
        const line = rows[index]
        const block = blocks[line.block]
        const end = line.start + Math.max(1, line.text.length)
        const others = block.chords.filter(c => c.at < line.start || c.at >= end)
        return report(editor.setChords(line.row, line.element,
                                       others.concat(chords.map(c => ({ at: c.at + line.start, name: c.name }))), joined === true))
    }

    // Puts a chord on a place, in place of the one there if there is one. A name that
    // is newly typed must be a chord. One the song has already (`typed` false: a chord
    // being moved or copied) is put as it is, whatever it is: some files have names
    // that are not chords, "N.C." or "C /// | Am ///", and those are to be moved about
    // like any other.
    function put(index, at, name, joined, typed) {
        if (typed !== false) {
            // A slash typed on the way to a bass note that never came is not part of the chord.
            name = name.replace(/\/+$/, "")
            if (!Chords.isChord(name))
                return false
        }
        if (!usable(index) || name === "")
            return false
        const line = rows[index]
        if (line.alone) {
            const names = line.chords.map(c => c.name)
            const slot = Math.min(names.length, Math.round(at / 2))
            names[slot] = name
            return report(editor.setChordsAlone(line.row, line.element, line.line, names, joined === true))
        }
        return setLine(index, line.chords.filter(c => c.at !== at).concat([{ at: at, name: name }]), joined)
    }

    function take(index, at, joined) {
        if (chordAt(index, at) === "")
            return false
        const line = rows[index]
        if (line.alone) {
            const names = line.chords.map(c => c.name)
            names.splice(Math.round(at / 2), 1)
            return report(editor.setChordsAlone(line.row, line.element, line.line, names, joined === true))
        }
        return setLine(index, line.chords.filter(c => c.at !== at), joined)
    }

    // Moves a chord from one place to another, or with `copy` puts another there.
    function move(fromRow, fromAt, toRow, toAt, copy) {
        const name = chordAt(fromRow, fromAt)
        if (name === "" || (fromRow === toRow && fromAt === toAt))
            return
        const from = rows[fromRow]
        if (!copy && fromRow === toRow && !from.alone) {
            // Along its own line: one change
            setLine(fromRow, from.chords.filter(c => c.at !== fromAt && c.at !== toAt).concat([{ at: toAt, name: name }]))
        } else if (!copy && fromRow === toRow) {
            // Along a line of chords alone: it changes places
            const names = from.chords.map(c => c.name)
            names.splice(Math.round(fromAt / 2), 1)
            names.splice(Math.min(names.length, Math.round(toAt / 2)), 0, name)
            report(editor.setChordsAlone(from.row, from.element, from.line, names, false))
        } else {
            // To another line: put there, then taken from here, as one thing to undo
            if (put(toRow, toAt, name, false, false) && !copy)
                take(fromRow, fromAt, true)
        }
        setSpot(toRow, toAt)
    }

    // ---- Copying a line's chords, or a group's, to other lines

    function copy(whole) {
        if (!usable(spotRow))
            return
        let from = spotRow
        let to = spotRow
        if (whole) {
            while (from > 0 && rows[from - 1].kind !== "group")
                --from
            while (to + 1 < rows.length && rows[to + 1].kind !== "group")
                ++to
        }
        const copiedLines = []
        for (let index = from; index <= to; ++index) {
            if (!usable(index))
                continue
            const line = rows[index]
            if (line.alone) {
                copiedLines.push({ alone: line.chords.map(c => c.name) })
                continue
            }
            const starts = wordStarts(line.text)
            copiedLines.push(line.chords.map(chord => {
                let word = 0
                for (let w = 0; w < starts.length; ++w) {
                    if (starts[w] <= chord.at)
                        word = w
                }
                return { word: word, offset: chord.at - (starts[word] ?? 0), name: chord.name }
            }))
        }
        copied = { lines: copiedLines }
        copiedOver = Chords.copiedText()
        const count = copiedLines.reduce((sum, line) => sum + (line.alone ? line.alone.length : line.length), 0)
        told(count + (count === 1 ? " chord" : " chords") + " copied from " + (copiedLines.length === 1 ? "the line" : copiedLines.length + " lines")
             + ": Ctrl+V puts them on the lines from the spot's on, word for word")
    }

    // Puts what was copied on the lines from the spot's on, one copied line to each
    // line that can take the spot, and says what came of it: a paste that changes
    // nothing would otherwise look like a key that did not work, and there are plain
    // reasons it may rightly change nothing.
    function paste() {
        if (!usable(spotRow))
            return
        if (!copied) {
            told("Nothing has been copied yet. Ctrl+C copies the chords of the line the spot is on.")
            return
        }
        // How many lines had their chords replaced and how many chords were put on
        // them; how many chords had no word to go on in the line they were for (or the
        // place another of them had taken) and were left out; how many lines were passed
        // over for being of the other kind than what was copied for them; and how many
        // the document refused.
        const did = { lines: 0, chords: 0, leftOut: 0, passedOver: 0, refused: 0 }
        let index = spotRow
        // However many lines it changes, a paste undoes as one: each change after the
        // first that is MADE is joined to the one before. (Not each after the first
        // line copied: where that one is passed over, its paste would be joined to
        // whatever was done before it, and one undo would take that back too.)
        let made = false
        for (const line of copied.lines) {
            while (index < rows.length && !usable(index))
                ++index
            if (index >= rows.length)
                break
            const target = rows[index]
            if (line.alone) {
                // A copied line of chords alone goes only onto a line of chords alone.
                if (!target.alone) {
                    ++did.passedOver
                } else if (report(editor.setChordsAlone(target.row, target.element, target.line, line.alone, made))) {
                    made = true
                    ++did.lines
                    did.chords += line.alone.length
                } else {
                    ++did.refused
                }
            } else if (target.alone) {
                ++did.passedOver
            } else {
                const starts = wordStarts(target.text)
                const chords = []
                for (const chord of line) {
                    if (chord.word >= starts.length)
                        continue
                    // On the same numbered word, as far into it as it was but not past
                    // the word's last letter.
                    const start = starts[chord.word]
                    const end = chord.word + 1 < starts.length ? starts[chord.word + 1] - 1 : target.text.length
                    const at = Math.min(start + chord.offset, Math.max(start, end - 1))
                    if (!chords.some(c => c.at === at))
                        chords.push({ at: at, name: chord.name })
                }
                if (setLine(index, chords, made)) {
                    made = true
                    ++did.lines
                    did.chords += chords.length
                    did.leftOut += line.length - chords.length
                } else {
                    ++did.refused
                }
            }
            ++index
        }
        // (What the document refused has been said already, in its own words.)
        const sentence = did.refused === 0 ? pasteSaid(did) : ""
        if (sentence !== "")
            told(sentence)
        return did
    }

    // What is said of a paste.
    function pasteSaid(did) {
        const otherKind = "chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none"
        if (did.lines === 0)
            return did.passedOver > 0 ? "Nothing was put here: " + otherKind + "." : ""
        if (did.chords === 0)
            return "None of the copied chords had a word to go on here."
        let sentence = did.chords + (did.chords === 1 ? " chord" : " chords") + " put on " + (did.lines === 1 ? "the line" : did.lines + " lines") + "."
        if (did.leftOut > 0)
            sentence += " " + did.leftOut + " had no word to go on, and " + (did.leftOut === 1 ? "was" : "were") + " left out."
        if (did.passedOver > 0)
            sentence += " " + (did.passedOver === 1 ? "1 line was" : did.passedOver + " lines were") + " passed over: " + otherKind + "."
        return sentence
    }

    // Whether a paste now would take chords off, which the line of hints says before
    // Ctrl+V is pressed: whether any line that what was copied would go onto (as paste()
    // goes) has chords of its own. A line that would be passed over, being of the other
    // kind, loses nothing and does not count. With nothing copied yet, whether the spot's
    // line has any.
    function pasteWouldReplace() {
        if (!usable(spotRow))
            return false
        if (!copied)
            return rows[spotRow].chords.length > 0
        let index = spotRow
        for (const line of copied.lines) {
            while (index < rows.length && !usable(index))
                ++index
            if (index >= rows.length)
                break
            const target = rows[index]
            if ((line.alone !== undefined) === target.alone && target.chords.length > 0)
                return true
            ++index
        }
        return false
    }

    // Whether a line's chords are the last thing that was copied on this computer, by
    // any program: nothing has been copied anywhere since. (Told by what the system
    // has as last copied being the text it had when the chords were. Something copied
    // since that is that very text again cannot be told from nothing, and is taken for
    // nothing.)
    function copiedLast() {
        return copied !== null && Chords.copiedText() === copiedOver
    }

    // Gives the sheet the keyboard: the arrows, 1 to 7, the letters. Or, while a chord
    // is being typed, gives it back to the box it is typed in: a click on the toolbar or
    // on the key does not end the typing.
    function takeKeys() {
        if (typing)
            field.forceActiveFocus()
        else
            keys.forceActiveFocus()
    }

    // ---- Typing a chord

    // Opens the bubble on the spot: with `seed` to go on from (the letter that was
    // pressed), or without, on the chord that is there, all of it picked to be typed over.
    function startTyping(seed) {
        if (!usable(spotRow))
            return
        typingRow = spotRow
        typingAt = spotAt
        typing = true
        entry.begin(seed !== undefined ? seed : chordAt(spotRow, spotAt), seed === undefined)
    }

    // Ends the typing. With a name, that chord goes on the place it was being typed
    // for; with none, nothing is changed (Esc). `onward` then moves the spot on from
    // that place to the next word (Tab), wherever the pointer has been meanwhile.
    //
    // With a name that is empty, the box was kept with nothing in it (by Enter, by a
    // click on another place or on the bare sheet), and that is taken at its word: no
    // chord is to be there, and the one that is goes. Where there is none, nothing is
    // asked of the song. (The maintainer's, 2026-10-13, for the Mac editor and so for
    // all three, their pages' rule 35a: "clicking and editing and deleting the chord
    // text and pressing enter or clicking on the next insertion point should delete".
    // Until then a kept empty box changed nothing, and the chord whose name had been
    // cleared came back.)
    function finishTyping(name, onward) {
        const row = typingRow
        const at = typingAt
        typing = false
        takeKeys()
        if (name === "")
            take(row, at)
        else if (name !== undefined)
            put(row, at, name)
        if (onward) {
            setSpot(row, at)
            step(1, false)
        }
    }

    // Keeps the chord that is being typed, if one is: what a click anywhere else does,
    // and what leaving the sheet does, so that going on to the next thing never loses it.
    function settle() {
        if (typing)
            finishTyping(entry.taken(), false)
    }

    // ---- A chord carried in from the strip
    //
    // One of the key's seven chords, dragged out of the strip and let go on a place. It
    // does there what its key does with the spot there (see handleKey): nothing new is
    // asked of the song. The rules are those of the Mac and Windows editors (their
    // pages, "A chord dragged out of the strip"), which thought of it first.

    // It is picked up. What was being typed is kept and its bubble closed, as leaving
    // the sheet for anything else does: so one of these can be carried while another
    // chord is being typed, which a chord of the sheet's own cannot be (to refuse it
    // would make the strip dead after every click on a word, a click being what opens
    // the bubble). That is done here, whether or not the chord is then put anywhere.
    function carry(name, caption, hold) {
        settle()
        takeKeys()
        carriedOver = false
        carriedClear = false
        carriedHold = hold
        carriedCaption = caption
        dragged = { row: -1, at: 0, name: name }
    }

    // It has been carried to a point of the sheet. Over a place of a line the spot
    // goes there, as it does for a chord dragged along the sheet, and the lit bubble
    // shows the chord's name where it would land. That bubble is the whole of the
    // promise: there is none over the strip, over the bare sheet between the cards,
    // over a frame or its foot, or over a line that can take no chord. (A chord dragged
    // along the sheet is otherwise: off the cards its bubble stays at the last place it
    // was over, and let go it lands there. One from the strip has crossed other slides
    // to get where it is, and is not to be left on the last of them by a hand that let
    // go on the way.)
    function carriedTo(point, exact) {
        if (!carrying)
            return
        carriedAt = point
        const seen = flick.mapFromItem(sheet, point.x, point.y)
        const inSight = seen.x >= 0 && seen.y >= 0 && seen.x < flick.width && seen.y < flick.height
        if (seen.y > scrollEdge)
            carriedClear = true
        let over = false
        if (inSight) {
            const where = page.mapFromItem(sheet, point.x, point.y)
            const under = page.childAt(where.x, where.y)
            if (under && under.isLine && usable(under.index)) {
                setSpot(under.index, under.placeAt(where.x - under.x, exact))
                over = true
            }
        }
        carriedOver = over
        carriedExact = exact
    }

    // It is let go: on the place the lit bubble shows, where it is put as its key
    // would have put it, in place of whatever chord is there, as one change and one
    // step of undo, the spot being left there. Anywhere else, nothing happens. (The key
    // that copies a chord dragged along the sheet does nothing here: this is always
    // one more.)
    function land() {
        const carried = carrying ? dragged : null
        const over = carriedOver
        dragged = null
        carriedOver = false
        // (Let go on a place, the hand is at that place of the sheet.)
        attending = over
        if (carried && over)
            put(spotRow, spotAt, carried.name)
    }

    // It is given up (Esc): nothing happens, and letting go later does nothing either.
    function dropCarried() {
        if (!carrying)
            return
        dragged = null
        carriedOver = false
    }

    // How near the top or the bottom of what is seen a carried chord scrolls the sheet,
    // and how fast at the very edge, in points a second: faster the nearer the edge, so
    // that it can be steered. (The speed is the Mac editor's, where it was a guess.)
    readonly property real scrollEdge: 44
    readonly property real scrollFastest: 500
    property bool carriedExact: true

    // Which way a carried chord is scrolling the sheet, and how fast: points a second,
    // downwards counted up; 0 where it is not near an edge, or there is no further to
    // go. Not upwards until the chord has once been clear of the top.
    readonly property real carriedScroll: {
        if (!carrying)
            return 0
        const seen = flick.mapFromItem(sheet, carriedAt.x, carriedAt.y)
        if (seen.x < 0 || seen.x >= flick.width)
            return 0
        const furthest = Math.max(0, flick.contentHeight - flick.height)
        if (seen.y > flick.height - scrollEdge && seen.y < flick.height + scrollEdge && flick.contentY < furthest)
            return scrollFastest * Math.min(1, Math.max(0.12, (seen.y - (flick.height - scrollEdge)) / scrollEdge))
        if (carriedClear && seen.y < scrollEdge && seen.y > -scrollEdge && flick.contentY > 0)
            return -scrollFastest * Math.min(1, Math.max(0.12, (scrollEdge - seen.y) / scrollEdge))
        return 0
    }

    // A click on a place of a line: the chord being typed elsewhere is kept, and the
    // bubble opens here, on the chord that is here or for a new one.
    //
    // (It is done here, by row and character, and not in the row's own handler, because
    // keeping a chord lays the whole sheet out again, the row that was clicked with it:
    // nothing of that row can be counted on after settle().)
    function pick(index, at) {
        // A new chord at the end of a line of chords alone is one place further along
        // once the chord that was being typed on that same line has gone in before it.
        const fresh = usable(index) && rows[index].alone && at >= rows[index].chords.length * 2
        settle()
        takeKeys()
        if (!usable(index))
            return
        setSpot(index, fresh ? rows[index].chords.length * 2 : at)
        pickRow(rows[index].row)
        startTyping()
    }

    // A key pressed with Ctrl while the box a chord is typed in has the keyboard. Most
    // are the box's own: it is a box of text, and Ctrl+V pastes text into it. But a
    // click on a line opens the box, and whoever clicks a line and presses Ctrl+V means
    // the chords just copied to go on that line: to click the line one means is the
    // natural thing to do before pasting onto it. So, while NOTHING HAS BEEN TYPED in
    // the box:
    //   - Ctrl+V, if a line's chords are the last thing copied anywhere, closes the
    //     box, changing nothing, and pastes them from the box's line on;
    //   - Ctrl+C and Ctrl+Shift+C, if the box is empty and so has nothing of its own to
    //     copy, copy the chords of the box's line, or of its group, and the box stays.
    // With anything typed in the box, or anything copied elsewhere since, the keys are
    // the box's. The keys that zoom are the sheet's whichever has the keyboard.
    // Answers whether the key was taken here.
    function keyInTheBox(event, text, begun) {
        if ((event.modifiers & Qt.ControlModifier) === 0)
            return false
        if (zoomKey(event))
            return true
        if (text !== begun)
            return false
        const row = typingRow
        const at = typingAt
        if (event.key === Qt.Key_V && (event.modifiers & Qt.ShiftModifier) === 0 && copiedLast()) {
            finishTyping(undefined, false)
            setSpot(row, at)
            paste()
            return true
        }
        if (event.key === Qt.Key_C && text === "") {
            setSpot(row, at)
            copy((event.modifiers & Qt.ShiftModifier) !== 0)
            return true
        }
        return false
    }

    // Ctrl with minus, with plus (or the equals key, which is plus without Shift) and
    // with 0: the words a step smaller, a step larger, and their usual size.
    function zoomKey(event) {
        if ((event.modifiers & Qt.ControlModifier) === 0)
            return false
        if (event.key === Qt.Key_Minus || event.key === Qt.Key_Underscore)
            zoomBy(-1)
        else if (event.key === Qt.Key_Plus || event.key === Qt.Key_Equal)
            zoomBy(1)
        else if (event.key === Qt.Key_0)
            zoomBy(0)
        else
            return false
        return true
    }

    function handleKey(event) {
        // (Ctrl by itself does nothing but bring up, on the line of hints, what it does.)
        if (event.key === Qt.Key_Control) {
            ctrlHeld = true
            return
        }
        const control = (event.modifiers & Qt.ControlModifier) !== 0
        const shift = (event.modifiers & Qt.ShiftModifier) !== 0
        if (carrying) {
            // While a chord from the strip is carried the keys do nothing but Esc,
            // which gives it up.
            if (event.key === Qt.Key_Escape)
                dropCarried()
            event.accepted = true
            return
        }
        // (The keys that move the spot or act on it are the person at the sheet, as the
        // pointer over it is: all but those that undo and zoom.)
        let atSpot = true
        if (control && event.key === Qt.Key_C) {
            copy(shift)
        } else if (control && event.key === Qt.Key_V) {
            paste()
        } else if (control && event.key === Qt.Key_Z) {
            report(shift ? editor.redo() : editor.undo())
            atSpot = false
        } else if (control && event.key === Qt.Key_Y) {
            report(editor.redo())
            atSpot = false
        } else if (control) {
            if (!zoomKey(event))
                return
            atSpot = false
        } else if (event.key === Qt.Key_Left) {
            step(-1, shift)
        } else if (event.key === Qt.Key_Right) {
            step(1, shift)
        } else if (event.key === Qt.Key_Up) {
            stepLine(-1)
        } else if (event.key === Qt.Key_Down) {
            stepLine(1)
        } else if (event.key === Qt.Key_Delete || event.key === Qt.Key_Backspace) {
            take(spotRow, spotAt)
        } else if (event.key === Qt.Key_Return || event.key === Qt.Key_Enter || event.key === Qt.Key_F2) {
            startTyping()
        } else if (event.key >= Qt.Key_1 && event.key <= Qt.Key_7 && inKey.length === 7) {
            put(spotRow, spotAt, inKey[event.key - Qt.Key_1])
        } else if (/^[a-gA-G]$/.test(event.text)) {
            startTyping(event.text.toUpperCase())
        } else {
            return
        }
        if (atSpot)
            attending = true
        event.accepted = true
    }

    // ---- The size

    // The step of `zooms` nearest a size, as its place in the list
    function zoomStep(size) {
        let best = 0
        for (let i = 1; i < zooms.length; ++i) {
            if (Math.abs(zooms[i] - size) < Math.abs(zooms[best] - size))
                best = i
        }
        return best
    }

    readonly property bool canZoomOut: zoomStep(zoom) > 0
    readonly property bool canZoomIn: zoomStep(zoom) < zooms.length - 1

    // Asks for the words a step larger (1) or smaller (-1), or their usual size (0).
    function zoomBy(direction) {
        const wanted = direction === 0 ? usualZoom : zooms[Math.max(0, Math.min(zooms.length - 1, zoomStep(zoom) + direction))]
        if (wanted !== zoom)
            zoomEdited(wanted)
    }

    // ---- Where things are

    // Where a place on a line is, from where the line's words start: a chord's bubble
    // if one is there, the character if not, and on a line of chords alone the next
    // free place for a new one.
    function along(index, at) {
        const line = rows[index]
        const placed = lines[index] ? lines[index].placed : nowhere.placed
        const found = line.chords.findIndex(c => c.at === at)
        if (found >= 0)
            return placed.xs[found] ?? 0
        if (line.alone) {
            const last = Math.min(line.chords.length, placed.xs.length) - 1
            return last >= 0 ? placed.xs[last] + placed.widths[last] + 10 * times : 0
        }
        return wordMetrics.advanceWidth(line.text.substring(0, at))
    }

    // The place of a line at a distance along it: the word there, or `exact` the
    // character. Over the space between two words it is the word after that is meant.
    function placeAt(index, distance, exact) {
        const line = rows[index]
        if (line.alone) {
            const placed = lines[index] ? lines[index].placed : nowhere.placed
            for (let slot = 0; slot < Math.min(line.chords.length, placed.xs.length); ++slot) {
                if (distance < placed.xs[slot] + placed.widths[slot] + 5 * times)
                    return slot * 2
            }
            return line.chords.length * 2
        }
        const words = line.text
        if (words.length === 0)
            return 0
        let character = ChordLayout.characterAt(words, Math.max(0, distance), wordMetrics)
        if (character > 0 && secondOfPair(words, character))
            --character
        if (exact && words[character] === " " && character + 1 < words.length && words[character + 1] !== " ")
            ++character
        return exact ? character : stopAt(index, character)
    }

    // The line over or under a line ON THE SCREEN that can take the spot, or -1: the
    // next of its own slide; or from a slide's last line the first line of the slide
    // under it in the grid, and from its first the last line of the slide over it. The
    // slide under or over is the one in the same place along the next row of slides, or
    // that row's last where it is a short one.
    function lineBeside(index, delta) {
        const at = lines[index]
        if (!at || at.cell < 0)
            return -1
        const here = cells[at.cell]
        for (let next = index + delta; next >= here.first && next < here.first + here.count; next += delta) {
            if (usable(next))
                return next
        }
        for (let gridRow = here.gridRow + delta; ; gridRow += delta) {
            const inRow = cells.filter(cell => cell.gridRow === gridRow)
            if (inRow.length === 0)
                return -1
            const there = inRow.find(cell => cell.column === here.column) ?? inRow[inRow.length - 1]
            for (let i = 0; i < there.count; ++i) {
                const line = delta > 0 ? there.first + i : there.first + there.count - 1 - i
                if (usable(line))
                    return line
            }
        }
    }

    // A row's item. (The rows are made anew whenever the song is read again, which
    // every chord that is kept does; each is put in its place as it is made, the places
    // having been worked out first.)
    function rowItem(index) {
        return list.itemAt(index)
    }

    // Scrolls the sheet the least that brings a part of it into view.
    function bringIntoView(x, y, width, height) {
        const furthestX = Math.max(0, flick.contentWidth - flick.width)
        const furthestY = Math.max(0, flick.contentHeight - flick.height)
        if (x < flick.contentX)
            flick.contentX = Math.max(0, x)
        else if (x + width > flick.contentX + flick.width)
            flick.contentX = Math.min(furthestX, x + width - flick.width)
        if (y < flick.contentY)
            flick.contentY = Math.max(0, y)
        else if (y + height > flick.contentY + flick.height)
            flick.contentY = Math.min(furthestY, y + height - flick.height)
    }

    // Brings the spot into view, when the keys have moved it: its slide's whole frame
    // if that can be seen whole, and the place itself whatever else. Its slide is then
    // the one picked.
    function reveal() {
        const at = lines[spotRow]
        if (!at || at.cell < 0 || !usable(spotRow))
            return
        const cell = cells[at.cell]
        if (cell.width + 2 * around <= flick.width && cell.height + 2 * around <= flick.height)
            bringIntoView(cell.x - around, cell.y - around, cell.width + 2 * around, cell.height + 2 * around)
        const x = at.x + gutter + along(spotRow, spotAt)
        bringIntoView(x - 40, at.y + at.lead - 10, Math.min(160, Math.max(80, flick.width - 20)), lineHeight + 20)
        pickRow(rows[spotRow].row)
    }

    // ---- Laying the slides out

    // A line that has not been given its place: what an item has for the moment
    // between the rows being read again and its going.
    readonly property var nowhere: ({ made: -1, cell: -1, x: 0, y: 0, width: 0, height: 0, lead: 0, placed: { xs: [], widths: [], width: 0, wordsWidth: 0 } })
    // What measure() found: where the chords of each row stand, and for each slide its
    // first row, how many lines it has, what its foot says and how wide it needs to be.
    property var measured: ({ placed: [], slides: [] })
    // The slides as they are laid out, each { row (the slide), first (its first line's
    // row), count, x, y, width, height (its frame, foot and all), cardHeight, label,
    // group, groupColor, gridRow, column }; and for each row of the sheet where its
    // line is: { made, cell, x, y, width, height (its part of the card: all of the
    // card's width, and the room over a slide's first line and under its last with
    // it), lead (the room over it), placed (where its chords stand) }
    property var cells: []
    property var lines: []
    // How many slides there are to a row of the grid, and how big the whole of it is
    property int columns: 1
    property real pageWidth: 0
    property real pageHeight: 0

    // Where the chords of every line stand, and how wide a place in the grid each
    // slide needs, frame and all: the widest of its lines with their chords and the
    // room inside the card, or what the words on its foot need if that is more.
    function measure(list) {
        const placed = list.map(line => line.kind === "line" ? ChordLayout.place(line.text, line.chords, wordMetrics, bubbleMetrics, 4 * times) : null)
        const slides = []
        list.forEach((line, index) => {
            if (line.kind !== "line" || !line.first)
                return
            let widest = 0
            // (A line of chords alone has a place past its last chord, for the next:
            // that place needs room to be pointed at.)
            for (let i = index; i < index + line.count; ++i)
                widest = Math.max(widest, placed[i].width + (list[i].alone ? 44 * times : 0))
            const block = blocks[line.block]
            // The slide's number, and on the first slide of each group the group's name
            const label = (line.row + 1) + (block.groupStart && block.group !== "" ? "  " + block.group : "")
            slides.push({ first: index, count: line.count, row: line.row, label: label, group: block.group, groupColor: block.groupColor,
                          needs: Math.max(Math.max(widest, 40 * times) + 2 * cardPad + 2 * frameEdge, labelMetrics.advanceWidth(label) + 16) + 2 * around })
        })
        return { placed: placed, slides: slides }
    }

    // Lays the slides out as a grid across the width the sheet is seen through: in the
    // order of the song, as many to a row as fit. Every card is one width, what the
    // widest slide needs, and the shape of a screen (though never taller for that than
    // four lines need); a row with a slide of more lines than that holds is as tall as
    // that slide needs. A slide's lines stand in the middle of its card, up and down.
    function arrange(list) {
        const slides = measured.slides
        let width = 0
        for (const slide of slides)
            width = Math.max(width, slide.needs)
        width = Math.ceil(width)
        const perRow = Math.max(1, Math.min(slides.length, Math.floor((flick.width - 2 * sideMargin) / Math.max(width, 1))))
        const cardWidth = width - 2 * around - 2 * frameEdge
        const shaped = Math.min(Math.round(cardWidth * 9 / 16), 4 * lineHeight + 2 * cardPad)
        const madeCells = []
        const madeLines = list.map(line => ({ made: line.made, cell: -1, x: 0, y: 0, width: 0, height: 0, lead: 0, placed: nowhere.placed }))
        let top = topMargin
        for (let from = 0, gridRow = 0; from < slides.length; from += perRow, ++gridRow) {
            const these = slides.slice(from, from + perRow)
            let most = 1
            for (const slide of these)
                most = Math.max(most, slide.count)
            const cardHeight = Math.max(most * lineHeight + 2 * cardPad, shaped)
            these.forEach((slide, column) => {
                const x = sideMargin + column * width + around
                const y = top + around
                const cardTop = y + frameEdge
                const linesTop = cardTop + Math.round((cardHeight - slide.count * lineHeight) / 2)
                for (let i = 0; i < slide.count; ++i) {
                    const lineTop = linesTop + i * lineHeight
                    // Every part of a card is one of its lines: the room over its first
                    // line is that line's, and the room under its last, the last's.
                    const lead = i === 0 ? linesTop - cardTop : 0
                    const foot = i === slide.count - 1 ? cardTop + cardHeight - lineTop - lineHeight : 0
                    madeLines[slide.first + i] = { made: list[slide.first + i].made, cell: madeCells.length, x: x + frameEdge, y: lineTop - lead, width: cardWidth,
                                                   height: lead + lineHeight + foot, lead: lead, placed: measured.placed[slide.first + i] }
                }
                madeCells.push({ row: slide.row, first: slide.first, count: slide.count, x: x, y: y, width: width - 2 * around,
                                 height: frameEdge + cardHeight + footHeight, cardHeight: cardHeight, label: slide.label, group: slide.group,
                                 groupColor: slide.groupColor, gridRow: gridRow, column: column })
            })
            top += 2 * around + frameEdge + cardHeight + footHeight
        }
        columns = perRow
        cells = madeCells
        lines = madeLines
        pageWidth = 2 * sideMargin + perRow * width
        pageHeight = top + sideMargin + 40
    }

    // Measures and arranges: for rows just read.
    function layOut(list) {
        measured = measure(list)
        arrange(list)
    }

    // Arranges the slides again, for another width or (with `size`) another size of
    // everything, with the slide that was at the top of what is seen still there.
    function resize(size) {
        const atTop = cells.find(cell => cell.y + cell.height > flick.contentY + 1)
        const within = atTop ? atTop.y - flick.contentY : 0
        if (size !== undefined) {
            // Every size that goes by this has changed by the time the next line is
            // reached: the fonts, and with them how tall a line is.
            times = size
            measured = measure(rows)
        }
        arrange(rows)
        const now = atTop ? cells.find(cell => cell.first === atTop.first) : undefined
        if (now)
            flick.contentY = Math.max(0, Math.min(now.y - within, Math.max(0, flick.contentHeight - flick.height)))
        flick.contentX = Math.max(0, Math.min(flick.contentX, Math.max(0, flick.contentWidth - flick.width)))
    }

    onZoomChanged: resize(zoom)
    // (A name is as wide as it is drawn, signs and all: every line is measured again.)
    onSignsChanged: resize(times)
    onRowChanged: {
        // The slide picked in the list at the side comes into view.
        const cell = picking ? undefined : cells.find(cell => cell.row === row)
        if (cell)
            bringIntoView(cell.x - around, cell.y - around, cell.width + 2 * around, cell.height + 2 * around)
    }
    Component.onCompleted: {
        times = zoom
        reload()
    }

    // What has the keyboard while no chord is being typed. It is a thing of its own, and
    // not the sheet as a whole, because the sheet's keyboard would go back to whatever
    // in it last had it, which after a chord has been typed is the box it was typed in.
    Item {
        id: keys

        focus: true
        Keys.onPressed: (event) => sheet.handleKey(event)
        Keys.onReleased: (event) => {
            if (event.key === Qt.Key_Control)
                sheet.ctrlHeld = false
        }
    }

    // A key let go while the app was left is never heard of
    Connections {
        target: Qt.application

        function onStateChanged() {
            if (Qt.application.state !== Qt.ApplicationActive)
                sheet.ctrlHeld = false
        }
    }

    Connections {
        target: sheet.editor

        function onSlideChanged() {
            sheet.reload()
        }

        function onDocumentChanged() {
            sheet.reload()
        }
    }

    FontMetrics {
        id: wordMetrics

        font.pixelSize: sheet.wordSize
    }

    FontMetrics {
        id: chordMetrics

        font.pixelSize: sheet.chordSize
        font.bold: true
    }

    // What a frame's foot says, which does not zoom
    FontMetrics {
        id: labelMetrics

        font.pixelSize: 12
    }

    // A chord's bubble is its name as it is drawn and a margin either side: what a chord
    // is as wide as, for keeping two apart.
    readonly property var bubbleMetrics: ({ advanceWidth: (name) => chordMetrics.advanceWidth(sheet.drawn(name)) + 2 * sheet.bubblePad })
    readonly property real bubbleHeight: Math.round(chordMetrics.height + 6 * times)
    readonly property real tailHeight: Math.round(6 * times)
    // A line of words with its row of chords over it
    readonly property real lineHeight: bubbleHeight + tailHeight + Math.round(wordMetrics.height + 6 * times)

    component Chip: Rectangle {
        id: chip

        property string label
        property string caption
        property bool picked: false
        // Whether it can be dragged off (the strip's seven can), and whether it is only
        // a picture of one (what goes with the pointer as one is carried)
        property bool carries: false
        property bool picture: false
        readonly property bool pointed: chipMouse.containsMouse

        signal clicked
        // Pulled 6 points with the button down, as a chord of the sheet's is before it
        // is taken to be dragged: where in it the button went down. Then where the
        // pointer has gone, as a point of the sheet, and whether to the character (or,
        // with Shift held, to the start of its word). Then let go, or given up.
        signal lifted(point hold)
        signal carriedTo(point at, bool exact)
        signal dropped
        signal givenUp

        width: Math.max(34, chipName.implicitWidth + 16 + (caption !== "" ? chipCaption.implicitWidth + 5 : 0))
        height: 26
        radius: 6
        color: picked ? sheet.accentColor : picture || chipMouse.pressed ? "#50535a" : chipMouse.containsMouse ? "#45484e" : "#3a3c42"

        Row {
            anchors.centerIn: parent
            spacing: 5

            Text {
                id: chipCaption

                anchors.verticalCenter: parent.verticalCenter
                visible: chip.caption !== ""
                color: chip.picked ? "#3a2408" : "#8d9097"
                font.pixelSize: 11
                text: chip.caption
            }

            Text {
                id: chipName

                anchors.verticalCenter: parent.verticalCenter
                color: chip.picked ? "#1b1c1f" : "#e6e6e6"
                font.pixelSize: 13
                font.bold: true
                text: chip.label
            }
        }

        MouseArea {
            id: chipMouse

            property point pressedAt
            property bool moved: false

            anchors.fill: parent
            enabled: !chip.picture
            hoverEnabled: true
            preventStealing: chip.carries
            // While one of the strip's is carried, the pointer says whether letting go
            // would put the chord on: an arrow with a plus where it would, a closed hand
            // where it would not. It is the picture's to say, which takes no part in
            // the dragging but is what the pointer is over all the while: the pointer
            // has the shape of the topmost thing under it that has one, and every
            // MouseArea has one, the arrow, whether it is switched on or not.
            cursorShape: !chip.picture ? Qt.ArrowCursor : sheet.carriedOver ? Qt.DragCopyCursor : Qt.ClosedHandCursor
            onPressed: (mouse) => {
                pressedAt = Qt.point(mouse.x, mouse.y)
                moved = false
            }
            onPositionChanged: (mouse) => {
                if (!pressed || !chip.carries)
                    return
                if (!moved && Math.abs(mouse.x - pressedAt.x) + Math.abs(mouse.y - pressedAt.y) < 6)
                    return
                if (!moved) {
                    moved = true
                    chip.lifted(pressedAt)
                }
                const at = mapToItem(sheet, mouse.x, mouse.y)
                chip.carriedTo(Qt.point(at.x, at.y), (mouse.modifiers & Qt.ShiftModifier) === 0)
            }
            onReleased: {
                if (moved)
                    chip.dropped()
            }
            onCanceled: {
                if (moved)
                    chip.givenUp()
            }
            // (A press let go without a pull is the click it always was, wherever in
            // the button; one that was pulled is not a click even if it comes back.)
            onClicked: {
                if (!moved)
                    chip.clicked()
            }
        }
    }

    Rectangle {
        anchors.fill: parent
        color: "#1b1c1f"
    }

    // The key's own chords on the keys 1 to 7, and the size of the words. (The key
    // itself is picked in the editor's toolbar, over this, where it is there for the
    // ChordPro text as well: it was picked here at first.)
    Rectangle {
        id: bar

        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: parent.top
        height: 44
        color: "#202226"

        // Nothing in the strip is squeezed: where it is too narrow for all of the
        // key's chords, the last of them are out of sight under the two buttons.
        Item {
            anchors.left: parent.left
            anchors.right: sizes.left
            anchors.rightMargin: 14
            height: parent.height
            clip: true

            Row {
                id: keyRow

                x: 14
                anchors.verticalCenter: parent.verticalCenter
                spacing: 8

                // (Whether the pointer is over them, between two of them as well)
                HoverHandler {
                    id: stripHover
                }

                Repeater {
                    model: sheet.inKey

                    Chip {
                        required property string modelData
                        required property int index

                        objectName: "keyChord" + (index + 1)
                        anchors.verticalCenter: parent.verticalCenter
                        caption: String(index + 1)
                        label: sheet.drawn(modelData)
                        carries: true
                        // On the spot; or, while a chord is being typed, in place of what
                        // is typed, where it was being typed.
                        onClicked: {
                            if (sheet.typing)
                                sheet.finishTyping(modelData, false)
                            else
                                sheet.put(sheet.spotRow, sheet.spotAt, modelData)
                            sheet.takeKeys()
                        }
                        // Or dragged onto a place (see carry())
                        onLifted: (hold) => sheet.carry(modelData, caption, hold)
                        onCarriedTo: (at, exact) => sheet.carriedTo(at, exact)
                        onDropped: sheet.land()
                        onGivenUp: sheet.dropCarried()
                        onPointedChanged: {
                            if (pointed)
                                sheet.pointedChord = index
                            else if (sheet.pointedChord === index)
                                sheet.pointedChord = -1
                        }
                    }
                }
            }

            Text {
                anchors.right: parent.right
                anchors.verticalCenter: parent.verticalCenter
                // (Only where there is room for it after the chords.)
                visible: sheet.editor.key === "" && x > keyRow.x + keyRow.width + 16
                color: "#8d9097"
                font.pixelSize: 12
                text: "The song names no key: pick the one its chords are in"
            }
        }

        ZoomButtons {
            id: sizes

            objectName: "chordSheetZoom"
            anchors.right: parent.right
            anchors.rightMargin: 14
            anchors.verticalCenter: parent.verticalCenter
            solid: true
            size: 24
            canShrink: sheet.canZoomOut
            canGrow: sheet.canZoomIn
            onShrink: {
                sheet.zoomBy(-1)
                sheet.takeKeys()
            }
            onGrow: {
                sheet.zoomBy(1)
                sheet.takeKeys()
            }
        }
    }

    // The line of hints, under the sheet: what the keys and the mouse do just now
    HintLine {
        id: hintLine

        objectName: "hintLine"
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        hints: sheet.hintsNow
    }

    Flickable {
        id: flick

        objectName: "chordSheetView"
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: bar.bottom
        anchors.bottom: hintLine.top
        clip: true
        // As big as the song, and never smaller than what it is seen through
        contentWidth: Math.max(width, sheet.pageWidth)
        contentHeight: Math.max(height, sheet.pageHeight)
        boundsBehavior: Flickable.StopAtBounds
        // Made wider or narrower, the slides are laid out again: fewer or more to a row.
        onWidthChanged: sheet.resize()

        ScrollBar.vertical: ScrollBar {}
        ScrollBar.horizontal: ScrollBar {}

        KineticWheel {}

        // Ctrl with the wheel makes the words larger and smaller, a step for each
        // notch, as it does in most programs; and so do two fingers spread or closed on
        // a trackpad, a step for each fair stretch. (The second is as the Mac's editor
        // has it and has NOT been tried by a hand here, where no test can make the
        // gesture: if it misbehaves, it is the PinchHandler below that is to go.)
        WheelHandler {
            property real turned: 0

            acceptedModifiers: Qt.ControlModifier
            acceptedDevices: PointerDevice.AllDevices
            onWheel: (event) => {
                turned += event.angleDelta.y
                if (Math.abs(turned) >= 120) {
                    sheet.zoomBy(turned > 0 ? 1 : -1)
                    turned = 0
                }
            }
        }

        PinchHandler {
            property real from: 1

            target: null
            onActiveChanged: from = 1
            onActiveScaleChanged: {
                if (activeScale > from * 1.2 || activeScale < from / 1.2) {
                    sheet.zoomBy(activeScale > from ? 1 : -1)
                    from = activeScale
                }
            }
        }

        // A click on the bare sheet takes the keys back from wherever they were, and
        // keeps the chord that was being typed. Between the cards is bare sheet, and a
        // frame and its foot are bare sheet too, and so is a line that can take no
        // chord. The pointer moved over it is not at the sheet (see `attending`).
        MouseArea {
            width: flick.contentWidth
            height: flick.contentHeight
            hoverEnabled: true
            onPositionChanged: (mouse) => {
                sheet.ctrlHeld = (mouse.modifiers & Qt.ControlModifier) !== 0
                if (sheet.pointerMoved(this, mouse.x, mouse.y) && !sheet.dragged)
                    sheet.attending = false
            }
            onPressed: sheet.takeKeys()
            onClicked: sheet.settle()
        }

        // Nor is the pointer that has left the sheet: for the strip over it, or anything
        // else in the window, or out of it. (A handler, which is told whether the
        // pointer is over the sheet whatever under it has taken the pointer's moves.)
        HoverHandler {
            onHoveredChanged: {
                if (!hovered)
                    sheet.attending = false
            }
        }

        // A frame for each slide, in its group's colour, with the card laid on it and
        // its foot under the card: the slide's number there and, on the first slide of
        // each group, the group's name. This is how the operator window frames a
        // slide's thumbnail, and ProPresenter. (The frames are counted, and each looks
        // itself up, so that laying the slides out again moves them and does not make
        // them anew.)
        Repeater {
            model: sheet.cells.length

            Item {
                id: frame

                required property int index
                readonly property var cell: sheet.cells[index] ?? null
                readonly property color tint: cell ? sheet.groupColor({ group: cell.group, groupColor: cell.groupColor }) : "#2b2d31"
                readonly property bool light: 0.299 * tint.r + 0.587 * tint.g + 0.114 * tint.b > 0.6

                objectName: cell ? "chordSlide:" + cell.row : ""
                x: cell ? cell.x : 0
                y: cell ? cell.y : 0
                width: cell ? cell.width : 0
                height: cell ? cell.height : 0

                // The slide that is picked has an orange ring: outside its frame and
                // set off from it by a little of the sheet, so that it reads against a
                // frame of any colour.
                Rectangle {
                    anchors.fill: parent
                    anchors.margins: -5
                    radius: 7
                    visible: frame.cell !== null && frame.cell.row === sheet.row
                    color: "transparent"
                    border.width: 2
                    border.color: sheet.accentColor
                }

                Rectangle {
                    anchors.fill: parent
                    radius: 4
                    color: frame.tint
                }

                Rectangle {
                    x: sheet.frameEdge
                    y: sheet.frameEdge
                    width: parent.width - 2 * sheet.frameEdge
                    height: frame.cell ? frame.cell.cardHeight : 0
                    radius: 2
                    color: "#222428"
                }

                Text {
                    x: 8
                    y: parent.height - sheet.footHeight
                    width: parent.width - 16
                    height: sheet.footHeight
                    verticalAlignment: Text.AlignVCenter
                    elide: Text.ElideRight
                    color: frame.light ? "black" : "white"
                    font: labelMetrics.font
                    text: frame.cell ? frame.cell.label : ""
                }
            }
        }

        // The lines, each where arrange() put it
        Item {
            id: page

            width: flick.contentWidth
            height: flick.contentHeight

            Repeater {
                id: list

                model: sheet.rows
                onItemAdded: ++sheet.laid

                Item {
                    id: line

                    required property var modelData
                    required property int index
                    readonly property bool isLine: modelData.kind === "line"
                    readonly property var chords: isLine ? modelData.chords : []
                    readonly property string words: isLine ? modelData.text : ""
                    // Where the line is. (For the moment between the song being read
                    // again and this item going, that is nowhere: what is laid out is
                    // then of other rows than this one was made for.)
                    readonly property var at: {
                        const found = sheet.lines[index]
                        return found !== undefined && found.made === modelData.made ? found : sheet.nowhere
                    }
                    readonly property var placed: at.placed ?? sheet.nowhere.placed
                    readonly property real chordTop: at.lead + Math.round(2 * sheet.times)
                    readonly property real wordsTop: chordTop + sheet.bubbleHeight + sheet.tailHeight
                    // The highlighted line: its words in a colour of their own
                    readonly property bool lit: index === sheet.highlighted

                    // Where a place on the line is, from the item's left edge
                    function xOf(place) {
                        return sheet.gutter + sheet.along(index, place)
                    }

                    // The place under the pointer: the word it is over, or `exact` the
                    // character.
                    function placeAt(x, exact) {
                        return sheet.placeAt(index, x - sheet.gutter, exact)
                    }

                    visible: isLine && at.cell >= 0
                    x: at.x
                    y: at.y
                    width: at.width
                    height: at.height

                    Text {
                        x: sheet.gutter
                        y: line.wordsTop
                        color: line.lit ? sheet.lineColor : "#e6e6e6"
                        font: wordMetrics.font
                        // A line of chords alone has nothing to read: a rule stands
                        // where its words would be.
                        text: line.isLine && !line.modelData.alone ? line.words : ""
                    }

                    // (Highlighted, it is the rule that takes the colour, twice as
                    // thick, or there would be nothing to show which line is meant.)
                    Rectangle {
                        visible: line.isLine && line.modelData.alone
                        x: sheet.gutter
                        y: line.wordsTop + Math.round(wordMetrics.height / 2) - Math.floor((height - 1) / 2)
                        width: Math.max(1, line.width - 2 * sheet.gutter)
                        height: line.lit ? Math.max(2, Math.round(2 * sheet.times)) : 1
                        color: line.lit ? sheet.lineColor : "#3a3c42"
                    }

                    // Following the pointer, and a click to type a chord there. It is
                    // the line's part of its slide's card and no more: outside the
                    // cards is bare sheet. The spot follows the pointer while a chord is
                    // being typed too, to show where a click would start the next
                    // (sheet.pick).
                    MouseArea {
                        anchors.fill: parent
                        enabled: sheet.usable(line.index)
                        hoverEnabled: true
                        onPositionChanged: (mouse) => {
                            sheet.ctrlHeld = (mouse.modifiers & Qt.ControlModifier) !== 0
                            if (sheet.pointerMoved(this, mouse.x, mouse.y) && !sheet.dragged) {
                                sheet.setSpot(line.index, line.placeAt(mouse.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
                                sheet.attending = true
                            }
                        }
                        onPressed: (mouse) => {
                            sheet.takeKeys()
                            sheet.setSpot(line.index, line.placeAt(mouse.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
                            sheet.attending = true
                        }
                        onClicked: (mouse) => sheet.pick(line.index, line.placeAt(mouse.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
                    }

                    // The chords, each a bubble with its tail on its character
                    Repeater {
                        model: line.chords

                        Item {
                            id: bubble

                            required property var modelData
                            required property int index
                            // (A chord carried in from the strip that is over no place
                            // has no spot to show: no bubble is lit as where it would
                            // land.)
                            readonly property bool atSpot: sheet.spotRow === line.index && sheet.spotAt === modelData.at
                                                           && !(sheet.carrying && !sheet.carriedOver)
                            readonly property bool lifted: sheet.dragged !== null && sheet.dragged.row === line.index
                                                           && sheet.dragged.at === modelData.at
                            // Where the bubble stands along the line, and how wide it is
                            readonly property real standsAt: line.placed.xs[index] ?? 0
                            // Where its character is, which is where the tail points
                            // even when the bubble has had to move right of it
                            readonly property real characterX: line.modelData.alone ? standsAt + width / 2
                                                               : wordMetrics.advanceWidth(line.words.substring(0, modelData.at))

                            objectName: "chord:" + line.index + ":" + modelData.at
                            x: sheet.gutter + standsAt
                            y: line.chordTop
                            width: line.placed.widths[index] ?? 0
                            height: sheet.bubbleHeight + sheet.tailHeight
                            opacity: lifted ? 0.35 : 1

                            Rectangle {
                                width: parent.width
                                height: sheet.bubbleHeight
                                radius: 6 * sheet.times
                                color: bubbleMouse.containsMouse || bubble.atSpot ? "#4a3a26" : "#34363c"
                                border.width: 1
                                border.color: bubble.atSpot ? sheet.accentColor : "#5c5f66"

                                Text {
                                    anchors.centerIn: parent
                                    color: sheet.chordColor
                                    font: chordMetrics.font
                                    text: sheet.drawn(bubble.modelData.name)
                                }
                            }

                            // The tail
                            Rectangle {
                                x: Math.max(3 * sheet.times, Math.min(parent.width - 5 * sheet.times, bubble.characterX - bubble.standsAt + 1))
                                y: sheet.bubbleHeight - 1
                                width: Math.max(1.5, 2 * sheet.times)
                                height: sheet.tailHeight + 3 * sheet.times
                                color: bubble.atSpot ? sheet.accentColor : "#5c5f66"
                            }

                            MouseArea {
                                id: bubbleMouse

                                property point pressedAt
                                property bool moved: false

                                anchors.fill: parent
                                hoverEnabled: true
                                cursorShape: sheet.dragged ? Qt.ClosedHandCursor : Qt.PointingHandCursor
                                preventStealing: true
                                onPressed: (mouse) => {
                                    sheet.takeKeys()
                                    sheet.setSpot(line.index, bubble.modelData.at)
                                    sheet.attending = true
                                    pressedAt = Qt.point(mouse.x, mouse.y)
                                    moved = false
                                }
                                onPositionChanged: (mouse) => {
                                    sheet.ctrlHeld = (mouse.modifiers & Qt.ControlModifier) !== 0
                                    if (!pressed) {
                                        if (sheet.pointerMoved(this, mouse.x, mouse.y) && !sheet.dragged) {
                                            sheet.setSpot(line.index, bubble.modelData.at)
                                            sheet.attending = true
                                        }
                                        return
                                    }
                                    // A chord is not carried off while another is being
                                    // typed: letting go of it then is a click on it,
                                    // which keeps the one and opens this one.
                                    if (sheet.typing)
                                        return
                                    if (!moved && Math.abs(mouse.x - pressedAt.x) + Math.abs(mouse.y - pressedAt.y) < 6)
                                        return
                                    moved = true
                                    if (!sheet.dragged)
                                        sheet.dragged = { row: line.index, at: bubble.modelData.at, name: bubble.modelData.name }
                                    // The line under the pointer, wherever on the
                                    // sheet that is, and the place on it
                                    const where = mapToItem(page, mouse.x, mouse.y)
                                    const under = page.childAt(where.x, where.y)
                                    if (under && under.isLine && sheet.usable(under.index))
                                        sheet.setSpot(under.index, under.placeAt(where.x - under.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
                                }
                                onReleased: (mouse) => {
                                    const from = sheet.dragged
                                    sheet.dragged = null
                                    if (from)
                                        sheet.move(from.row, from.at, sheet.spotRow, sheet.spotAt, (mouse.modifiers & Qt.ControlModifier) !== 0)
                                    else if (!moved)
                                        sheet.pick(line.index, bubble.modelData.at)
                                }
                                onCanceled: sheet.dragged = null
                            }
                        }
                    }
                }
            }
        }

        // The spot, where there is no chord yet: a bubble in outline with its tail on
        // the character. Where there is one, that chord's own bubble is lit instead.
        // It is shown while a chord is being typed as well, wherever the pointer has
        // gone on to, and not where the typing is, which has its own bubble.
        Item {
            id: ghost

            readonly property var item: sheet.laid >= 0 && sheet.spotRow >= 0 ? list.itemAt(sheet.spotRow) : null
            readonly property bool free: item !== null && sheet.rows.length > 0 && sheet.chordAt(sheet.spotRow, sheet.spotAt) === ""
            readonly property bool underTyping: sheet.typing && sheet.spotRow === sheet.typingRow && sheet.spotAt === sheet.typingAt

            objectName: "chordSpot"
            // (For a chord carried in from the strip, only while it is over a place.)
            visible: item !== null && !underTyping && (sheet.carrying ? sheet.carriedOver : free || sheet.dragged !== null)
            x: item ? item.x + item.xOf(sheet.spotAt) : 0
            y: item ? item.y + item.chordTop : 0
            width: Math.max(26 * sheet.times, sheet.dragged ? chordMetrics.advanceWidth(sheet.drawn(sheet.dragged.name)) + 2 * sheet.bubblePad : 0)
            height: sheet.bubbleHeight + sheet.tailHeight

            Rectangle {
                width: parent.width
                height: sheet.bubbleHeight
                radius: 6 * sheet.times
                color: sheet.dragged ? "#4a3a26" : "transparent"
                border.width: 1
                border.color: sheet.accentColor
                opacity: sheet.dragged ? 1 : 0.75

                Text {
                    anchors.centerIn: parent
                    color: sheet.dragged ? sheet.chordColor : sheet.accentColor
                    font: chordMetrics.font
                    text: sheet.dragged ? sheet.drawn(sheet.dragged.name) : "+"
                }
            }

            Rectangle {
                x: 3 * sheet.times
                y: sheet.bubbleHeight - 1
                width: Math.max(1.5, 2 * sheet.times)
                height: sheet.tailHeight + wordMetrics.height + 3 * sheet.times
                color: sheet.accentColor
                opacity: 0.75
            }
        }

        // The bubble a chord is typed in, at the place it is being typed for, with what
        // it might be going to be over it. It grows upwards from where the chord will
        // stand, so that the word being given the chord, and the rest of its line, stay
        // in sight. It does not zoom: it is to be typed in at any size of the words.
        Rectangle {
            id: entry

            readonly property var item: sheet.laid >= 0 && sheet.typingRow >= 0 ? list.itemAt(sheet.typingRow) : null
            property string last
            // What the box held when it was opened: while it still does, nothing has
            // been typed in it (see keyInTheBox).
            property string begun
            property int pick: -1
            readonly property var offered: sheet.typing ? Chords.completions(read(field.text), sheet.key, sheet.used).slice(0, 14) : []

            // What the box holds, as a chord's name is written: with "#" and "b" for the
            // ♯ and ♭ it shows (rule 30a of the Mac and Windows editors' pages). Every
            // rule of typing works on that, as it did before there were signs. (A ♯ or ♭
            // typed by hand is so taken for "#" or "b" too.)
            function read(text) {
                return sheet.signs ? Chords.withoutSigns(text) : text
            }

            function begin(text, selected) {
                const shown = sheet.drawn(text)
                last = shown
                begun = shown
                field.text = shown
                pick = -1
                field.forceActiveFocus()
                if (selected)
                    field.selectAll()
                else
                    field.cursorPosition = shown.length
            }

            // What Enter takes: the one picked from the list, or what is typed
            function taken() {
                return pick >= 0 && pick < offered.length ? offered[pick] : Chords.tidied(read(field.text))
            }

            objectName: "chordEntry"
            visible: sheet.typing && item !== null
            // Kept within what is seen, and covering what it must where there is no
            // room over the line.
            x: item ? Math.max(flick.contentX + 4, Math.min(item.x + item.xOf(sheet.typingAt), flick.contentX + flick.width - width - 12)) : 0
            y: item ? Math.max(flick.contentY + 4, item.y + item.chordTop + sheet.bubbleHeight + 3 - height) : 0
            z: 5
            width: 330
            height: field.height + 14 + (offered.length > 0 ? offers.height + 8 : 0)
            radius: 8
            color: "#2b2d31"
            border.width: 1
            border.color: sheet.accentColor

            // Swallows clicks on the bubble itself
            MouseArea {
                anchors.fill: parent
            }

            TextInput {
                id: field

                objectName: "chordField"
                x: 10
                y: parent.height - height - 7
                width: parent.width - 20
                height: 24
                verticalAlignment: TextInput.AlignVCenter
                color: sheet.chordColor
                selectionColor: sheet.accentColor
                selectedTextColor: "#1b1c1f"
                font.pixelSize: 16
                font.bold: true
                maximumLength: 16
                // Only what could be a chord gets in, and its notes are made capitals
                // as they are typed. It is shown with its signs as it is typed: F# is
                // F♯ as soon as the # is, and C7b is C7♭9 once the 9 is. (A sign is as
                // long as what it stands for, so the caret stays where it was.)
                onTextEdited: {
                    const tidy = Chords.tidied(entry.read(text))
                    if (!Chords.couldBecome(tidy)) {
                        const at = Math.max(0, cursorPosition - 1)
                        text = entry.last
                        cursorPosition = Math.min(at, text.length)
                        return
                    }
                    const shown = sheet.drawn(tidy)
                    if (shown !== text) {
                        const at = cursorPosition
                        text = shown
                        cursorPosition = Math.min(at, text.length)
                    }
                    entry.last = text
                    entry.pick = -1
                }
                Keys.onReleased: (event) => {
                    if (event.key === Qt.Key_Control)
                        sheet.ctrlHeld = false
                }
                Keys.onPressed: (event) => {
                    if (event.key === Qt.Key_Control) {
                        sheet.ctrlHeld = true
                        return
                    }
                    if (sheet.keyInTheBox(event, text, entry.begun)) {
                        event.accepted = true
                        return
                    }
                    if (event.key === Qt.Key_Escape) {
                        sheet.finishTyping(undefined, false)
                    } else if (event.key === Qt.Key_Return || event.key === Qt.Key_Enter) {
                        sheet.finishTyping(entry.taken(), false)
                    } else if (event.key === Qt.Key_Tab) {
                        // The first offered, if nothing is picked and nothing typed is
                        // a chord yet
                        if (entry.pick < 0 && !Chords.isChord(entry.read(text)) && entry.offered.length > 0)
                            entry.pick = 0
                        sheet.finishTyping(entry.taken(), true)
                    } else if (event.key === Qt.Key_Backtab) {
                        // (Shift with Tab would take the keyboard out of the box, and
                        // so end the typing by the back door.)
                    } else if (event.key === Qt.Key_Down) {
                        entry.pick = entry.offered.length > 0 ? (entry.pick + 1) % entry.offered.length : -1
                    } else if (event.key === Qt.Key_Up) {
                        entry.pick = entry.offered.length > 0 ? (entry.pick + entry.offered.length - 1 + (entry.pick < 0 ? 1 : 0)) % entry.offered.length : -1
                    } else if (event.key === Qt.Key_Right && entry.pick >= 0 && cursorPosition === text.length) {
                        text = sheet.drawn(entry.offered[entry.pick])
                        entry.last = text
                        entry.pick = -1
                    } else if ((event.key === Qt.Key_Delete || event.key === Qt.Key_Backspace) && text === "") {
                        // Nothing left of it: the chord goes
                        const row = sheet.typingRow
                        const at = sheet.typingAt
                        sheet.typing = false
                        sheet.takeKeys()
                        sheet.take(row, at)
                    } else {
                        return
                    }
                    event.accepted = true
                }
            }

            Flow {
                id: offers

                x: 8
                y: 8
                width: parent.width - 16
                spacing: 5

                Repeater {
                    model: entry.offered

                    Chip {
                        required property string modelData
                        required property int index

                        label: sheet.drawn(modelData)
                        picked: index === entry.pick
                        onClicked: sheet.finishTyping(modelData, false)
                    }
                }
            }
        }
    }

    // ---- A chord carried in from the strip, as it is seen

    // The sheet scrolls while the chord is held near the top or the bottom of what is
    // seen of it, so that a slide out of sight can be reached. It goes by the time that
    // has passed and not by the tick, so that the speed is the speed whatever else is
    // going on; and the place under the pointer, which has not moved, is looked for
    // again each time. Nothing runs while no chord is held there.
    Timer {
        id: carriedScrolling

        property real last: 0

        interval: 16
        repeat: true
        running: sheet.carriedScroll !== 0
        onRunningChanged: last = Date.now()
        onTriggered: {
            const now = Date.now()
            const furthest = Math.max(0, flick.contentHeight - flick.height)
            flick.contentY = Math.max(0, Math.min(furthest, flick.contentY + sheet.carriedScroll * Math.min(100, now - last) / 1000))
            last = now
            sheet.carriedTo(sheet.carriedAt, sheet.carriedExact)
        }
    }

    // The button's picture, which goes with the pointer, held where it was picked up.
    // It can be seen through, so that it hides neither the word it is held over nor the
    // lit bubble that says where the chord would land, which is close over that word.
    // (The Mac and Windows editors hand the chord to the system's own dragging, which
    // draws this for them. Here it never leaves the sheet, and the sheet draws it.)
    Chip {
        objectName: "carriedChord"
        visible: sheet.carrying
        picture: true
        caption: sheet.carriedCaption
        label: sheet.dragged ? sheet.drawn(sheet.dragged.name) : ""
        x: sheet.carriedAt.x - sheet.carriedHold.x
        y: sheet.carriedAt.y - sheet.carriedHold.y
        opacity: 0.6
    }
}
