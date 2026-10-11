#!/usr/bin/env node
// Writes testdata/vectors/layout.json and sheet-model.json: what the chord sheet of
// Simple Presenter does with a song, as a record for the Mac and Windows apps' tests.
//
//   node tools/make-vectors/sheet.js <a checkout of Simple-Presenter> testdata/vectors
//
// How it is done, and why it can be believed. The sheet's rules are JavaScript inside a
// QML file (qml/ChordSheet.qml) and a small library beside it (qml/chordlayout.js).
// This does not copy them out by hand, which would be a second version to go wrong: it
// reads the functions out of those two files as text and runs those very functions
// here, with stand-ins for the few things of the app they touch:
//
//   - `editor`, the document. The stand-in keeps a song as the list of text boxes the
//     real one hands over (PresentationEditor::song), notes every change it is asked
//     for, and makes the change the way the real one does, so that the next thing done
//     starts from the song as it then is.
//   - `Chords`, two questions of the chord rules that the sheet asks. They are answered
//     here by short copies of the C++ (the full rules have vectors of their own, made
//     from the C++ itself, by main.cpp).
//   - the window: where things are on the screen, the box a chord is typed in. Nothing
//     recorded here depends on those, and what does (the arrow keys Up and Down, which
//     go by where the words are drawn) is left out and described in spec/ instead.
//
// What is recorded is what the sheet ASKS of the document for each thing done to it,
// and where it leaves the spot. That is the part a port has to get the same.
const fs = require("fs")
const path = require("path")
const vm = require("vm")

const [source, outFolder] = process.argv.slice(2)
if (!source || !outFolder || !fs.existsSync(path.join(source, "qml/ChordSheet.qml"))) {
    console.error("node sheet.js <a checkout of Simple-Presenter> <folder to write into>")
    process.exit(2)
}

const ZW = "\u200b"
const QUAD = "\u2001"

// ---- The chord rules the sheet asks of, and the ones the stand-in document needs

function noteLength(text) {
    if (text === "" || !"CDEFGAB".includes(text[0].toUpperCase()))
        return 0
    let i = 1
    while (i < text.length && (text[i] === "#" || text[i] === "♯"))
        ++i
    if (i === 1) {
        while (i < text.length && (text[i] === "b" || text[i] === "♭"))
            ++i
    }
    return i
}

function writtenNote(text) {
    const length = noteLength(text)
    return text[0].toUpperCase() + text.slice(1, length).replace(/♯/g, "#").replace(/♭/g, "b")
}

function couldBecome(typed) {
    if (typed === "")
        return true
    const length = noteLength(typed)
    if (length === 0)
        return false
    return [...typed.slice(length)].every(c => /[\p{L}\p{N}]/u.test(c) || "#/+-()°ø^.,♯♭Δ".includes(c))
}

function tidied(typed) {
    const chord = typed.trim()
    const length = noteLength(chord)
    if (length === 0)
        return chord
    let rest = chord.slice(length)
    const slash = rest.lastIndexOf("/")
    if (slash >= 0) {
        const after = rest.slice(slash + 1)
        if (noteLength(after) > 0 && noteLength(after) === after.length)
            rest = rest.slice(0, slash + 1) + writtenNote(after)
    }
    return writtenNote(chord) + rest
}

function isPlaceholders(line) {
    return line !== "" && /^[\s\u200b]+$/.test(line)
}

function placeholders(count, standIn) {
    return Array.from({ length: count }, () => standIn).join(QUAD)
}

function tidy(text, chords) {
    const result = []
    if (text === "")
        return result
    for (const given of chords) {
        const chord = { at: given.at, name: given.name.trim() }
        if (chord.name === "")
            continue
        chord.at = Math.max(0, Math.min(chord.at, text.length - 1))
        if (text[chord.at] === "\n" && chord.at > 0 && text[chord.at - 1] !== "\n")
            --chord.at
        const same = result.findIndex(other => other.at === chord.at)
        if (same >= 0)
            result[same] = chord
        else
            result.push(chord)
    }
    return result.map((chord, index) => ({ chord, index })).sort((a, b) => a.chord.at - b.chord.at || a.index - b.index).map(entry => entry.chord)
}

// ---- The stand-in document

// A text box's words are nothing to a song: none, or only white space, with none of the
// characters that chords with no words hang on. (As noWords in presentationeditor.cpp.)
function noWords(text) {
    return text.trim() === "" && !text.includes(ZW) && !text.includes(QUAD)
}

function makeEditor(blocks, key) {
    const editor = {
        blocks: JSON.parse(JSON.stringify(blocks)),
        key: key,
        calls: [],
        // What was made for the chords of blank slides, as the real one remembers it
        // while the song is open (spec/01-file-format.md, "A change of mind"): the
        // boxes that were given words, by their slide and id, and those of them that
        // were added. What was made goes again when the chords do.
        made: [],
        added: [],
        song() {
            return JSON.parse(JSON.stringify(this.blocks))
        },
        usedChords() {
            return this.blocks.flatMap(block => block.chords.map(chord => chord.name))
        },
        find(row, element) {
            return this.blocks.find(block => block.row === row && block.element === element)
        },
        setChords(row, element, chords, joined) {
            this.calls.push({ ask: "setChords", row, element, chords: JSON.parse(JSON.stringify(chords)), joined: joined === true })
            const block = this.find(row, element)
            block.chords = tidy(block.text, chords.map(chord => ({ at: chord.at, name: tidied(chord.name) })))
            return ""
        },
        // The line becomes one stand-in for each chord, in the character it hung its
        // chords on before, and the chords of the lines after it move by as much as it
        // grew or shrank. (As PresentationEditor::setChordsAlone, but for one thing.)
        //
        // A line with NO chord is one zero-width space, whatever it hung its chords on,
        // so that it is still a line of chords alone and a chord can be put on it
        // again. Decided by the maintainer on 2026-10-12, for Simple Presenter and the
        // two apps alike (spec/01-file-format.md, "Chords with no words"). At the
        // commit of Simple Presenter that reference/ holds, setChordsAlone still leaves
        // such a line with no stand-in at all, which in a box with other lines is an
        // empty line that takes nothing; this stand-in went first, and
        // sheet-model.json was made again with it.
        //
        // (A zero-width space and not the plain space the line may have had: a text
        // box left holding one plain space and no chord is, as a song is read, a stray
        // space and nothing more.)
        setChordsAlone(row, element, line, names, joined) {
            this.calls.push({ ask: "setChordsAlone", row, element, line, names: names.slice(), joined: joined === true })
            const block = this.find(row, element)
            // A blank slide's box: nothing there and nothing to put changes nothing. Its
            // first chords become all it holds, on zero-width spaces; and a slide with
            // no text box (no id) is given one for them.
            if (block.chords.length === 0 && noWords(block.text)) {
                if (names.length === 0)
                    return ""
                if (block.element === "") {
                    block.element = "(a text box added for the chords)"
                    this.added.push(block.element)
                }
                this.made.push(block.row + "/" + block.element)
                block.text = placeholders(names.length, ZW)
                block.chords = names.map((name, i) => ({ at: i * 2, name: tidied(name) }))
                block.blank = false
                return ""
            }
            const lines = block.text.split("\n")
            const old = lines[line]
            const standIn = !old.includes(ZW) && old.includes(" ") ? " " : ZW
            const made = names.length === 0 ? ZW : placeholders(names.length, standIn)
            let start = 0
            for (let i = 0; i < line; ++i)
                start += lines[i].length + 1
            const shift = made.length - old.length
            const kept = []
            for (const chord of block.chords) {
                if (chord.at < start)
                    kept.push(chord)
                else if (chord.at > start + old.length)
                    kept.push({ at: chord.at + shift, name: chord.name })
            }
            lines[line] = made
            block.text = lines.join("\n")
            names.forEach((name, i) => kept.push({ at: start + i * 2, name: tidied(name) }))
            block.chords = tidy(block.text, kept)
            lines[line] = ""
            if (names.length === 0 && block.chords.length === 0 && noWords(lines.join("\n")) && this.made.includes(block.row + "/" + block.element)) {
                // The last chord taken off a blank slide that was given its chords while
                // the song has been open: what was made for them is unmade. The slide
                // is blank again, handed over with no words, and without the text box if
                // that was only added for the chords.
                block.text = ""
                block.blank = true
                if (this.added.includes(block.element))
                    block.element = ""
            }
            return ""
        },
        setKey(name) {
            this.calls.push({ ask: "setKey", key: name })
            this.key = name
            return ""
        },
        undo() { return "" },
        redo() { return "" },
    }
    return editor
}

// ---- The sheet's own functions, read out of the QML file and run as they are

function functionsOf(file) {
    const lines = fs.readFileSync(file, "utf8").split("\n")
    const found = []
    for (let i = 0; i < lines.length; ++i) {
        if (!/^    function \w+\(/.test(lines[i]))
            continue
        let end = i
        while (end < lines.length && lines[end] !== "    }")
            ++end
        found.push(lines.slice(i, end + 1).join("\n"))
        i = end
    }
    return found.join("\n\n")
}

const sheetSource = functionsOf(path.join(source, "qml/ChordSheet.qml"))
const layoutSource = fs.readFileSync(path.join(source, "qml/chordlayout.js"), "utf8").replace(/^\.pragma library\s*$/m, "")

function makeSheet(blocks, key) {
    const editor = makeEditor(blocks, key)
    const sheet = {
        editor,
        Chords: { isPlaceholders, isChord: name => noteLength(name) > 0 && couldBecome(name), tidied },
        rows: [], blocks: [], used: [], mostLines: 1,
        spotRow: -1, spotAt: 0, typing: false, typingRow: -1, typingAt: 0,
        laid: 0, dragged: null, copied: null,
        // Nothing is drawn here: no row has an item, and the view has nowhere to scroll.
        list: { itemAt: () => null },
        flick: { contentY: 0, height: 100000, contentHeight: 100000 },
        keys: { forceActiveFocus() {} },
        field: { forceActiveFocus() {} },
        // The box a chord is typed in: what was typed is given by whoever drives this.
        entry: { typed: "", begun: null, begin(text, selected) { this.begun = { text, selected }; this.typed = text }, taken() { return tidied(this.typed) } },
        said: [],
        failures: [],
        rowPicked() {},
        Math, JSON, Array, Map, Object,
    }
    sheet.told = message => sheet.said.push(message)
    sheet.failed = error => sheet.failures.push(error)
    vm.createContext(sheet)
    vm.runInContext(sheetSource, sheet)
    sheet.reload()
    return sheet
}

// Runs one thing on a sheet, and reads the song again as the app does when the document
// says it has changed. Answers with what the document was asked.
function act(sheet, what) {
    sheet.editor.calls = []
    const before = sheet.editor.calls.length
    what()
    const asked = sheet.editor.calls.slice(before)
    sheet.reload()
    return asked
}

// The put-and-take that moves a chord to another line asks twice, and the app reads the
// song between the two. So the document tells the sheet at once, as the real one does.
function live(sheet) {
    for (const name of ["setChords", "setChordsAlone"]) {
        const plain = sheet.editor[name].bind(sheet.editor)
        sheet.editor[name] = (...given) => {
            const answer = plain(...given)
            sheet.reload()
            return answer
        }
    }
    return sheet
}

function spotOf(sheet) {
    return { row: sheet.spotRow, at: sheet.spotAt }
}

function chordsOfSong(sheet) {
    return sheet.editor.blocks.map(block => ({ row: block.row, element: block.element, text: block.text, chords: block.chords }))
}

// ---- The songs the vectors are made on

function block(row, element, group, groupStart, text, chords, blank) {
    return { row, slideId: "slide-" + row, group, groupColor: "", groupStart, label: "", element, text, chords, blank: blank === true }
}

const SONGS = {
    // Four slides of two lines, in two groups. As the sheet lays it out, row 0 is the
    // name "Verse 1", rows 1 to 4 its four lines, row 5 the name "Verse 2" and rows 6
    // to 9 its lines.
    "grace": {
        key: "G",
        blocks: [
            block(0, "A", "Verse 1", true, "Amazing grace how sweet the sound\nThat saved a wretch like me",
                  [{ at: 0, name: "G" }, { at: 8, name: "G7" }, { at: 17, name: "C" }, { at: 27, name: "G" }, { at: 45, name: "D" }]),
            block(1, "B", "Verse 1", false, "I once was lost but now am found\nWas blind but now I see",
                  [{ at: 0, name: "G" }, { at: 7, name: "G7" }, { at: 16, name: "C" }]),
            block(2, "C", "Verse 2", true, "Twas grace that taught my heart to fear\nAnd grace my fears relieved", []),
            block(3, "D", "Verse 2", false, "Amen\nAmen and amen", []),
        ],
    },
    // A line of chords with no words, hung on zero-width spaces; then words. Row 0 is
    // the name "Intro", row 1 its line of chords, row 2 "Verse 1", rows 3 and 4 its
    // lines, row 5 "Ending" and row 6 its one chord.
    "intro": {
        key: "D",
        blocks: [
            block(0, "A", "Intro", true, placeholders(3, ZW), [{ at: 0, name: "D" }, { at: 2, name: "G" }, { at: 4, name: "A" }]),
            block(1, "B", "Verse 1", true, "Holy holy holy\nLord God Almighty", [{ at: 0, name: "D" }, { at: 15, name: "G" }]),
            // One chord on a slide of its own, hung on a plain space, as some files have it.
            block(2, "C", "Ending", true, " ", [{ at: 0, name: "D" }]),
        ],
    },
    // Chords in the middle of words, two spaces between words, one word alone, an
    // empty line, and a slide with two text boxes. Rows 0 to 2 are the first slide's
    // three lines (it is in no group, so has no name over it), row 3 is "Chorus", row 4
    // the first box of the second slide, and rows 5 and 6 its second box.
    "odd": {
        key: "",
        blocks: [
            block(0, "A", "", false, "Halle  lujah\n\nAmen", [{ at: 3, name: "F" }, { at: 9, name: "C/E" }]),
            block(1, "B", "Chorus", true, "First box", []),
            block(1, "C", "Chorus", true, "Second box\nof two", [{ at: 7, name: "Am" }]),
        ],
    },
    // Blank slides: an intro made as a slide with an empty text box, a verse, a blank
    // slide in the middle of the verse's group, and the verse's last slide. Row 0 is
    // the name "Intro" and row 1 the intro's one empty line; row 2 is "Verse 1", rows
    // 3 and 4 its first slide's lines, row 5 the blank slide's line, and row 6 the last
    // slide's.
    "blank": {
        key: "G",
        blocks: [
            block(0, "A", "Intro", true, "", [], true),
            block(1, "B", "Verse 1", true, "Amazing grace how sweet the sound\nThat saved a wretch like me", [{ at: 0, name: "G" }, { at: 17, name: "C" }]),
            block(2, "C", "Verse 1", false, "", [], true),
            block(3, "D", "Verse 1", false, "I once was lost", [{ at: 7, name: "D" }]),
        ],
    },
    // A blank slide that has no text box at all: what is handed over for it has no id.
    // (This is Simple Presenter's own. It adds a text box to such a slide when a chord
    // is put there, and takes it off again with the last chord. An app that makes no
    // text boxes leaves such a slide out, and has no use for this song.) Row 0 is the
    // slide with words, row 1 the name "Ending" and row 2 the bare slide's empty line.
    "bare": {
        key: "G",
        blocks: [
            block(0, "A", "", false, "Amazing grace", [{ at: 0, name: "G" }]),
            block(1, "", "Ending", true, "", [], true),
        ],
    },
    // A line of chords alone under a line of words, in one text box, hung on plain
    // spaces; and a slide of words after it. Row 0 is the name "Verse 1", row 1 the
    // first slide's line of words, row 2 its line of chords alone, and row 3 the
    // second slide's line. (For a line that loses its last chord and is to take one
    // again: see the stand-in's setChordsAlone.)
    "under": {
        key: "E",
        blocks: [
            block(0, "A", "Verse 1", true, "Holy holy holy\n" + placeholders(2, " "), [{ at: 0, name: "E" }, { at: 15, name: "B" }, { at: 17, name: "E" }]),
            block(1, "B", "Verse 1", false, "Lord God Almighty", [{ at: 0, name: "A" }]),
        ],
    },
}

// A scenario that names a row which is no line of words was written wrongly: say so,
// rather than record nothing happening.
function mustBeLine(sheet, spot, what) {
    if (!sheet.usable(spot.row))
        throw new Error(what + ": row " + spot.row + " is not a line of the song")
}

function rowsOf(sheet) {
    return JSON.parse(JSON.stringify(sheet.rows))
}

function write(name, about, body) {
    const whole = Object.assign({ about, madeBy: "tools/make-vectors/sheet.js, by running the functions of Simple Presenter's qml/ChordSheet.qml and qml/chordlayout.js themselves. Do not edit by hand: run it again." }, body)
    fs.writeFileSync(path.join(outFolder, name), JSON.stringify(whole, null, 4) + "\n")
    console.log(name)
}

// ---- sheet-model.json

const model = {}

model.songs = Object.fromEntries(Object.entries(SONGS).map(([name, song]) => [name, { key: song.key, textBoxes: song.blocks }]))

// The sheet's rows for each song
model.rows = Object.entries(SONGS).map(([name, song]) => {
    const sheet = makeSheet(song.blocks, song.key)
    return { song: name, mostLines: sheet.mostLines, used: Array.from(sheet.used), rows: rowsOf(sheet) }
})

// Where the words of a line start, and where the spot stops on each row
model.wordStarts = ["Amazing grace how sweet the sound", "Halle  lujah", "  leading spaces", "trailing  ", "", "one", "a b c", "tab\tbed", ZW + QUAD + ZW]
    .map(text => ({ text, out: Array.from(makeSheet(SONGS.grace.blocks, "G").wordStarts(text)) }))
model.stops = Object.entries(SONGS).flatMap(([name, song]) => {
    const sheet = makeSheet(song.blocks, song.key)
    return sheet.rows.map((row, index) => ({ song: name, row: index, usable: sheet.usable(index), out: sheet.usable(index) ? Array.from(sheet.stops(index)) : [] }))
})
model.stopAt = (() => {
    const sheet = makeSheet(SONGS.grace.blocks, "G")
    const row = sheet.rows.findIndex(r => r.kind === "line")
    return Array.from({ length: sheet.rows[row].text.length + 2 }, (_, at) => ({ song: "grace", row, at, out: sheet.stopAt(row, at) }))
})()

// Walking the spot with the arrow keys: Right and Left a word at a time, with Shift a
// letter at a time
function walk(name, start, steps) {
    const sheet = makeSheet(SONGS[name].blocks, SONGS[name].key)
    if (start) {
        mustBeLine(sheet, start, "walk")
        sheet.setSpot(start.row, start.at)
    }
    const spots = []
    for (const key of steps) {
        sheet.step(key.includes("Left") ? -1 : 1, key.startsWith("Shift"))
        spots.push(spotOf(sheet))
    }
    return { song: name, start: start ?? null, keys: steps, spots }
}
model.walks = [
    walk("grace", null, Array(16).fill("Right")),
    walk("grace", { row: 1, at: 0 }, Array(4).fill("Left")),
    walk("grace", { row: 9, at: 0 }, Array(4).fill("Right")),
    walk("grace", { row: 6, at: 0 }, Array(3).fill("Left")),
    walk("grace", { row: 1, at: 0 }, Array(12).fill("Shift+Right")),
    walk("grace", { row: 2, at: 2 }, Array(5).fill("Shift+Left")),
    walk("grace", { row: 1, at: 3 }, ["Right", "Left", "Left"]),
    walk("intro", null, Array(9).fill("Right")),
    walk("intro", { row: 3, at: 0 }, Array(3).fill("Left")),
    walk("odd", null, Array(8).fill("Right")),
    walk("odd", null, Array(14).fill("Shift+Right")),
    walk("blank", null, Array(9).fill("Right")),
    walk("blank", { row: 6, at: 0 }, Array(9).fill("Left")),
    walk("blank", { row: 4, at: 20 }, Array(4).fill("Shift+Right")),
    walk("bare", null, Array(4).fill("Right")),
    walk("under", null, Array(8).fill("Right")),
    walk("under", { row: 3, at: 0 }, Array(6).fill("Left")),
]

// One change at a time: what the document is asked, and the song after it
function edit(name, spot, what, run) {
    const sheet = live(makeSheet(SONGS[name].blocks, SONGS[name].key))
    mustBeLine(sheet, spot, "edit " + JSON.stringify(what))
    sheet.setSpot(spot.row, spot.at)
    sheet.editor.calls = []
    const answer = run(sheet)
    return { song: name, spot, do: what, asked: sheet.editor.calls, spotAfter: spotOf(sheet), songAfter: chordsOfSong(sheet),
             answer: answer === undefined ? null : answer }
}
model.edits = [
    edit("grace", { row: 1, at: 13 }, { put: "Em" }, s => s.put(s.spotRow, s.spotAt, "Em")),
    edit("grace", { row: 1, at: 8 }, { put: "Em", note: "in place of the one there" }, s => s.put(s.spotRow, s.spotAt, "Em")),
    edit("grace", { row: 1, at: 10 }, { put: "D/F#", note: "in the middle of a word" }, s => s.put(s.spotRow, s.spotAt, "D/F#")),
    edit("grace", { row: 2, at: 5, }, { put: "C" }, s => s.put(s.spotRow, s.spotAt, "C")),
    edit("grace", { row: 6, at: 0 }, { put: "G", note: "a slide that had no chords" }, s => s.put(s.spotRow, s.spotAt, "G")),
    edit("grace", { row: 1, at: 13 }, { put: "G/", note: "a slash with no bass note after it is left off" }, s => s.put(s.spotRow, s.spotAt, "G/")),
    edit("grace", { row: 1, at: 13 }, { put: "hello", note: "not a chord: refused" }, s => s.put(s.spotRow, s.spotAt, "hello")),
    edit("grace", { row: 1, at: 13 }, { put: "", note: "nothing: refused" }, s => s.put(s.spotRow, s.spotAt, "")),
    edit("grace", { row: 1, at: 0 }, { put: "G", onRow: 0, note: "on row 0, the name of a group, which is no line: refused" }, s => s.put(0, 0, "G")),
    edit("grace", { row: 1, at: 8 }, { take: true }, s => s.take(s.spotRow, s.spotAt)),
    edit("grace", { row: 1, at: 9 }, { take: true, note: "nothing there: nothing asked" }, s => s.take(s.spotRow, s.spotAt)),
    edit("grace", { row: 1, at: 8 }, { key: 4, note: "the key 4, in G: its fourth chord" }, s => s.put(s.spotRow, s.spotAt, ["G", "Am", "Bm", "C", "D", "Em", "F#dim"][3])),
    edit("grace", { row: 1, at: 8 }, { move: { row: 1, at: 13 } }, s => s.move(1, 8, 1, 13, false)),
    edit("grace", { row: 1, at: 8 }, { move: { row: 1, at: 17 }, note: "onto another chord, which gives way" }, s => s.move(1, 8, 1, 17, false)),
    edit("grace", { row: 1, at: 8 }, { move: { row: 2, at: 5 }, note: "to another line of the same slide" }, s => s.move(1, 8, 2, 5, false)),
    edit("grace", { row: 1, at: 8 }, { move: { row: 6, at: 5 }, note: "to another slide: put there, then taken from here, the second joined to the first" }, s => s.move(1, 8, 6, 5, false)),
    edit("grace", { row: 1, at: 8 }, { copy: { row: 6, at: 5 }, note: "with Ctrl held: put there, and left here" }, s => s.move(1, 8, 6, 5, true)),
    edit("grace", { row: 1, at: 8 }, { move: { row: 1, at: 8 }, note: "nowhere: nothing asked" }, s => s.move(1, 8, 1, 8, false)),
    edit("intro", { row: 1, at: 2 }, { put: "Em", note: "a line of chords alone: in place of the second" }, s => s.put(s.spotRow, s.spotAt, "Em")),
    edit("intro", { row: 1, at: 6 }, { put: "Bm", note: "a line of chords alone: one more at its end" }, s => s.put(s.spotRow, s.spotAt, "Bm")),
    edit("intro", { row: 1, at: 2 }, { take: true, note: "a line of chords alone: its second goes, and the line is one shorter" }, s => s.take(s.spotRow, s.spotAt)),
    edit("intro", { row: 1, at: 0 }, { move: { row: 1, at: 4 }, note: "along a line of chords alone: it changes places" }, s => s.move(1, 0, 1, 4, false)),
    edit("intro", { row: 1, at: 2 }, { move: { row: 3, at: 5 }, note: "from a line of chords alone to a word" }, s => s.move(1, 2, 3, 5, false)),
    edit("intro", { row: 3, at: 0 }, { move: { row: 1, at: 6 }, note: "from a word to the end of a line of chords alone" }, s => s.move(3, 0, 1, 6, false)),
    edit("intro", { row: 6, at: 0 }, { put: "A", note: "one chord hung on a plain space: the space stays a space" }, s => s.put(s.spotRow, s.spotAt, "A")),
    edit("odd", { row: 0, at: 7 }, { put: "G" }, s => s.put(s.spotRow, s.spotAt, "G")),
    edit("odd", { row: 5, at: 0 }, { put: "F", note: "the second text box of a slide" }, s => s.put(s.spotRow, s.spotAt, "F")),
    edit("blank", { row: 1, at: 0 }, { put: "G", note: "a blank slide: its first chord. Its line is a line of chords alone with none yet" }, s => s.put(s.spotRow, s.spotAt, "G")),
    edit("blank", { row: 5, at: 0 }, { key: 4, note: "the key 4, on a blank slide in the middle of a group" }, s => s.put(s.spotRow, s.spotAt, ["G", "Am", "Bm", "C", "D", "Em", "F#dim"][3])),
    edit("blank", { row: 1, at: 0 }, { put: "hello", note: "not a chord: refused, and nothing asked" }, s => s.put(s.spotRow, s.spotAt, "hello")),
    edit("blank", { row: 1, at: 0 }, { take: true, note: "a blank slide has nothing to take: nothing asked" }, s => s.take(s.spotRow, s.spotAt)),
    edit("blank", { row: 1, at: 0 }, { put: "G", then: { put: "D", at: 2 }, note: "a second chord after the first: the line is asked for both" },
         s => s.put(1, 0, "G") && s.put(1, 2, "D")),
    edit("blank", { row: 1, at: 0 }, { put: "G", then: { take: true }, note: "put, and taken off again: asked for none, and the slide is blank as it was" },
         s => s.put(1, 0, "G") && s.take(1, 0)),
    edit("blank", { row: 3, at: 0 }, { move: { row: 1, at: 0 }, note: "from a word to a blank slide: put there, then taken from here, joined" }, s => s.move(3, 0, 1, 0, false)),
    edit("blank", { row: 3, at: 17 }, { copy: { row: 5, at: 0 }, note: "with Ctrl held, onto a blank slide: put there, and left here" }, s => s.move(3, 17, 5, 0, true)),
    edit("bare", { row: 2, at: 0 }, { put: "D", note: "a slide with no text box: the line is asked for, with no id. Simple Presenter then adds a text box for it" },
         s => s.put(s.spotRow, s.spotAt, "D")),
    edit("bare", { row: 2, at: 0 }, { put: "D", then: { take: true }, note: "and taken off again: the text box that was added goes with it" },
         s => s.put(2, 0, "D") && s.take(2, 0)),
    // A line of chords alone that loses its last chord keeps one stand-in, and is a
    // line of chords alone still: a chord can be put on it again.
    edit("intro", { row: 1, at: 0 }, { take: true, then: { take: true, then: { take: true } }, note: "every chord taken off a line of chords alone, the first each time: asked for two, for one, for none. It keeps one stand-in" },
         s => s.take(1, 0) && s.take(1, 0) && s.take(1, 0)),
    edit("intro", { row: 1, at: 0 }, { take: true, then: { take: true, then: { take: true, then: { put: "Em" } } }, note: "and one put on it again: on the stand-in it kept" },
         s => s.take(1, 0) && s.take(1, 0) && s.take(1, 0) && s.put(1, 0, "Em")),
    edit("intro", { row: 6, at: 0 }, { take: true, note: "the one chord of a slide that hung it on a plain space: asked for none. The line is then one zero-width space, which a stray space cannot be taken for" },
         s => s.take(6, 0)),
    edit("intro", { row: 6, at: 0 }, { take: true, then: { put: "A" }, note: "and a chord put there again: on the zero-width space" },
         s => s.take(6, 0) && s.put(6, 0, "A")),
    edit("under", { row: 2, at: 2 }, { put: "A", note: "a line of chords alone under a line of words: in place of its second" }, s => s.put(s.spotRow, s.spotAt, "A")),
    edit("under", { row: 2, at: 0 }, { take: true, then: { take: true }, note: "both chords taken off a line of chords alone under a line of words: it is one zero-width space, and the words keep their chord" },
         s => s.take(2, 0) && s.take(2, 0)),
    edit("under", { row: 2, at: 0 }, { take: true, then: { take: true, then: { put: "F#m" } }, note: "and one put on it again" },
         s => s.take(2, 0) && s.take(2, 0) && s.put(2, 0, "F#m")),
    edit("under", { row: 2, at: 0 }, { take: true, then: { take: true, then: { put: "F#m", then: { put: "B", at: 2 } } }, note: "and a second after it: the line hangs its chords on zero-width spaces now, and the chord of the slide after is not touched" },
         s => s.take(2, 0) && s.take(2, 0) && s.put(2, 0, "F#m") && s.put(2, 2, "B")),
    edit("under", { row: 1, at: 0 }, { move: { row: 2, at: 4 }, note: "from a word to the end of the line of chords alone under it" }, s => s.move(1, 0, 2, 4, false)),
]

// Typing a chord: the bubble opened, what was typed kept or thrown away
function typing(name, title, steps) {
    const sheet = live(makeSheet(SONGS[name].blocks, SONGS[name].key))
    const done = []
    for (const step of steps) {
        sheet.editor.calls = []
        if (step.spot) {
            mustBeLine(sheet, step.spot, "typing")
            sheet.setSpot(step.spot.row, step.spot.at)
        }
        if (step.click)
            mustBeLine(sheet, step.click, "typing, a click")
        if (step.press !== undefined)
            sheet.startTyping(step.press === "Enter" ? undefined : step.press)
        if (step.typed !== undefined)
            sheet.entry.typed = step.typed
        if (step.finish !== undefined)
            sheet.finishTyping(step.finish === "Esc" ? undefined : sheet.entry.taken(), step.finish === "Tab")
        if (step.click)
            sheet.pick(step.click.row, step.click.at)
        if (step.settle)
            sheet.settle()
        done.push({ step, asked: sheet.editor.calls, typing: sheet.typing, typingAt: sheet.typing ? { row: sheet.typingRow, at: sheet.typingAt } : null,
                    boxHolds: sheet.typing ? sheet.entry.begun : null, spot: spotOf(sheet) })
    }
    return { song: name, title, steps: done, songAfter: chordsOfSong(sheet) }
}
model.typing = [
    typing("grace", "a letter opens the bubble on the spot; Enter keeps what was typed",
           [{ spot: { row: 1, at: 13 }, press: "E" }, { typed: "Em7" }, { finish: "Enter" }]),
    typing("grace", "Esc throws it away",
           [{ spot: { row: 1, at: 13 }, press: "E" }, { typed: "Em7" }, { finish: "Esc" }]),
    typing("grace", "Tab keeps it and moves the spot on to the next word",
           [{ spot: { row: 1, at: 13 }, press: "E" }, { typed: "Em" }, { finish: "Tab" }]),
    typing("grace", "Enter on a chord opens the bubble on what is there, all of it picked",
           [{ spot: { row: 1, at: 8 }, press: "Enter" }, { typed: "G7sus4" }, { finish: "Enter" }]),
    typing("grace", "a click on another place keeps what was typed and opens the bubble there",
           [{ spot: { row: 1, at: 13 }, press: "E" }, { typed: "Em" }, { click: { row: 2, at: 5 } }, { typed: "C" }, { click: { row: 2, at: 13 } }, { typed: "D" },
            { finish: "Enter" }]),
    typing("grace", "a click on the bare sheet keeps it too",
           [{ spot: { row: 1, at: 13 }, press: "B" }, { typed: "Bm" }, { settle: true }]),
    typing("grace", "a slash typed on the way to a bass note that never came is left off",
           [{ spot: { row: 1, at: 13 }, press: "G" }, { typed: "G/" }, { finish: "Enter" }]),
    typing("grace", "what was typed is tidied: the notes as capitals",
           [{ spot: { row: 1, at: 13 }, press: "F" }, { typed: "f#m/a" }, { finish: "Enter" }]),
    typing("intro", "on a line of chords alone, a click past the last while one is being typed there lands one further along",
           [{ spot: { row: 1, at: 6 }, press: "B" }, { typed: "Bm" }, { click: { row: 1, at: 6 } }, { typed: "A" }, { finish: "Enter" }]),
    typing("blank", "on a blank slide: a click opens the bubble, and a click on the same place while typing keeps the chord and opens on the next",
           [{ click: { row: 1, at: 0 } }, { typed: "G" }, { click: { row: 1, at: 0 } }, { typed: "D" }, { finish: "Enter" }]),
    typing("blank", "Tab on a blank slide keeps the chord and moves the spot on, to the new place after it",
           [{ spot: { row: 5, at: 0 }, press: "C" }, { typed: "C" }, { finish: "Tab" }]),
]

// Copying a line's chords, or a whole group's, and putting them on other lines
function copyPaste(name, title, from, whole, to) {
    const sheet = live(makeSheet(SONGS[name].blocks, SONGS[name].key))
    mustBeLine(sheet, from, "copy")
    mustBeLine(sheet, to, "paste")
    sheet.setSpot(from.row, from.at)
    sheet.copy(whole)
    const copied = JSON.parse(JSON.stringify(sheet.copied))
    const said = sheet.said.slice()
    sheet.setSpot(to.row, to.at)
    sheet.editor.calls = []
    sheet.paste()
    return { song: name, title, copyAt: from, wholeGroup: whole, copied, said, pasteAt: to, asked: sheet.editor.calls, songAfter: chordsOfSong(sheet) }
}
model.copyPaste = [
    copyPaste("grace", "one line onto another, word for word", { row: 1, at: 0 }, false, { row: 6, at: 0 }),
    copyPaste("grace", "a line onto a shorter one: chords of words it has not are left out", { row: 1, at: 0 }, false, { row: 9, at: 0 }),
    copyPaste("grace", "a line onto one of a single word", { row: 3, at: 0 }, false, { row: 8, at: 0 }),
    copyPaste("grace", "a whole group onto the next: line by line, all but the first joined to it", { row: 1, at: 0 }, true, { row: 6, at: 0 }),
    copyPaste("grace", "a whole group, pasted from the next group's second line on: it runs out of lines", { row: 2, at: 0 }, true, { row: 7, at: 0 }),
    copyPaste("grace", "onto the line it was copied from: a chord that stood on the space after a word is drawn back onto the word", { row: 1, at: 0 }, false, { row: 1, at: 0 }),
    copyPaste("intro", "a line of chords alone onto words: nothing", { row: 1, at: 0 }, false, { row: 3, at: 0 }),
    copyPaste("intro", "words onto a line of chords alone: nothing", { row: 3, at: 0 }, false, { row: 1, at: 0 }),
    copyPaste("intro", "a line of chords alone onto another", { row: 1, at: 0 }, false, { row: 6, at: 0 }),
    copyPaste("odd", "a chord in the middle of a word keeps its place in the word", { row: 0, at: 0 }, false, { row: 4, at: 0 }),
    copyPaste("blank", "words onto a blank slide: nothing", { row: 3, at: 0 }, false, { row: 1, at: 0 }),
    copyPaste("blank", "a blank slide's none, onto words: nothing", { row: 1, at: 0 }, false, { row: 3, at: 0 }),
    copyPaste("intro", "a line of chords alone onto a line it empties and fills again: the same chords", { row: 1, at: 0 }, false, { row: 1, at: 0 }),
]

write("sheet-model.json",
      "What the chord sheet does with a song. `songs` are the songs these are made on, as the document hands them over: one entry for each "
      + "text box that has words, with `row` the slide it is on (counted from 0), its words and its chords (each `at` counted along the "
      + "whole text box, a line break being one), and for a blank slide one entry with no words and `blank` set (in the song `bare`, with "
      + "no `element` either: a slide that has no text box, which Simple Presenter gives one when a chord is put there and an app that "
      + "makes no text boxes leaves out; the id of the box it adds is its own to make, and stands here as a sentence in brackets). `rows`: the sheet's rows made of each song (a row for the name of a group, and one for "
      + "each line of each text box). `wordStarts`, `stops`, `stopAt`: where the spot can stand. `walks`: where the arrow keys take it. "
      + "`edits`, `typing`, `copyPaste`: for each thing done, what the document is ASKED (setChords: this text box now has these chords, "
      + "each `at` along the whole box; setChordsAlone: this line of chords with no words now has these, by name; `joined`: it is one step "
      + "of undo with the change before it) and the song afterwards. In all of them `row` in a spot is the sheet's row, and `row` in what "
      + "is asked is the slide. Some strings hold characters that cannot be seen: U+200B and U+2001.",
      model)

// ---- layout.json: where the bubbles go along a line, with letters of a fixed width

const layout = vm.createContext({ Math })
vm.runInContext(layoutSource, layout)
// Every letter of the words is ten wide, every letter of a chord eight, and a bubble
// has seven either side of its name: any numbers would do, as long as both sides use
// the same.
const words = { advanceWidth: text => 10 * text.length }
const bubbles = { advanceWidth: name => 8 * name.length + 14 }
const GAP = 4
const lines = [
    ["Amazing grace how sweet the sound", [{ at: 0, name: "G" }, { at: 8, name: "G7" }, { at: 17, name: "C" }, { at: 27, name: "G" }]],
    ["Amazing grace", []],
    ["a b c d", [{ at: 0, name: "Gsus4" }, { at: 2, name: "F#m7b5" }, { at: 4, name: "C" }, { at: 6, name: "Dmaj7/F#" }]],
    ["I", [{ at: 0, name: "Ebmaj7/G" }]],
    ["Hallelujah", [{ at: 3, name: "F" }, { at: 4, name: "C/E" }]],
    [placeholders(3, ZW), [{ at: 0, name: "D" }, { at: 2, name: "G" }, { at: 4, name: "A" }]],
    [" ", [{ at: 0, name: "D" }]],
    ["", []],
    ["end", [{ at: 2, name: "Asus4" }]],
]
write("layout.json",
      "Where the chords of one line stand, by chordlayout.js: place(text, chords, words, bubbles, gap). A chord's left edge is where its "
      + "character starts, unless that would run into the chord before it, in which case it is pushed right until it clears that one by "
      + "`gap`. Made with letters of a fixed width (see `metrics`) so that it does not depend on any font: a port is tried with the same "
      + "widths, and then given its real ones. `characterAt`: which character of a line is at a distance along it.",
      {
          metrics: { wordLetter: 10, chordLetter: 8, bubblePadEachSide: 7, gap: GAP },
          place: lines.map(([text, chords]) => Object.assign({ text, chords }, JSON.parse(JSON.stringify(layout.place(text, chords, words, bubbles, GAP))))),
          characterAt: ["Amazing grace", "I", ""].flatMap(text => [-5, 0, 4, 9, 10, 11, 25, 64, 125, 129, 130, 131, 500].map(x => ({ text, x, out: layout.characterAt(text, x, words) }))),
      })

// ---- A check on the stand-ins above
//
// The short copies of the chord rules that the stand-in document uses are held to the
// vectors made from the C++ itself (main.cpp writes those: run it first). If one of
// them ever disagrees, the vectors written here cannot be trusted, and this says so.
function checkStandIns() {
    const read = name => JSON.parse(fs.readFileSync(path.join(outFolder, name), "utf8"))
    const wrong = []
    const same = (a, b) => JSON.stringify(a) === JSON.stringify(b)
    if (!fs.existsSync(path.join(outFolder, "chord-names.json")))
        return console.error("NOT CHECKED: run make-vectors first, so that the stand-ins here can be held to its vectors")
    for (const c of read("chord-names.json").cases) {
        if (tidied(c.in) !== c.tidied)
            wrong.push(["tidied", c.in, tidied(c.in), c.tidied])
        if (couldBecome(c.in) !== c.couldBecome)
            wrong.push(["couldBecome", c.in])
        if ((noteLength(c.in) > 0 && couldBecome(c.in)) !== c.isChord)
            wrong.push(["isChord", c.in])
    }
    for (const c of read("tidy.json").cases) {
        if (!same(tidy(c.text, c.chords), c.out))
            wrong.push(["tidy", c.text, c.chords])
    }
    const pro = read("chordpro.json")
    for (const c of pro.isPlaceholders) {
        if (isPlaceholders(c.in) !== c.out)
            wrong.push(["isPlaceholders", JSON.stringify(c.in)])
    }
    for (const c of pro.placeholders) {
        if (placeholders(c.count, c.standIn) !== c.out)
            wrong.push(["placeholders", c.count])
    }
    if (wrong.length > 0) {
        console.error("THE STAND-INS DISAGREE WITH THE C++ in " + wrong.length + " cases:", JSON.stringify(wrong.slice(0, 6)))
        process.exit(1)
    }
    console.log("the stand-ins agree with the vectors made from the C++")
}
checkStandIns()
