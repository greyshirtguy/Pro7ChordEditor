import QtQuick
import QtQuick.Controls.Basic
import SimplePresenterApp
import "chordlayout.js" as ChordLayout

// The chord editor: the whole song as one sheet of words, with its chords in bubbles
// over them, to be put on, changed, moved and taken off with the mouse and the keys.
// ProPresenter has nothing like it (it shows the chords a song came with and no more),
// so there was nothing to mirror, and this is this app's own.
//
// One sheet, where the file has slides. In the file each slide has a text box with its
// own words, and a chord is a note on a stretch of one text box's characters
// (proconvert::readChords). Working a slide at a time would mean paging through thirty
// of them to chord one song. So the editor (PresentationEditor::song) hands the text
// boxes over in order and they are laid out here one under another, each line of each
// box a row, with the name of the group over its first slide. Each slide's lines are in
// a card of their own, numbered beside it, so that where one slide ends and the next
// begins is plain; the cards are all one size, that of the slide with the most lines
// and the widest line, so that the sheet reads as a column of slides and not as a
// ragged list. Nothing is joined up underneath: a chord that is put on a word is
// written to that word's own text box, on its own slide.
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
// Shift a letter at a time, a letter at a time being a long walk along a line.
//
// Getting a chord on quickly, which is what this is for:
//   - 1 to 7 put the key's own chord for that note of its scale on the spot (in C: 1 is
//     C, 4 is F, 6 is Am). Most of most songs is those. The strip along the top shows
//     them, and they can be clicked there too.
//   - A letter A to G starts a chord by name. A bubble opens for typing it, which
//     takes only what could be a chord, and under it are the chords it might be going
//     to be: the song's own first, then the key's, then the usual kinds on that note
//     (chords::completions). Down and Up pick one, Enter takes it, Tab takes it and
//     moves on to the next word, a click takes it; Right puts the picked one in the
//     box to be gone on with (for a bass note, say).
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
//     That is how a second verse gets the chords of the first.
//
// Each change is saved at once and can be undone, like every change in the editor.
FocusScope {
    id: sheet

    // The PresentationEditor that has the song open
    required property var editor
    // The colour of a group, given { group, groupColor } (the operator window's)
    property var groupColor: (slide) => "#2b2d31"
    // The slide to have in view: its row in the editor
    property int row: 0
    readonly property color accentColor: "#ff8a1f"
    // The key the chords are in. A song that names none is taken to be in C, which is
    // only what the keys 1 to 7 give until the key is set.
    readonly property string key: editor.key !== "" ? editor.key : "C"
    readonly property var inKey: Chords.diatonic(key)

    // The text boxes, as the editor gave them, and the sheet's rows made of them: a
    // { kind: "group", name, color } over a group's first slide, and for each line a
    // { kind: "line", block, line, row, element, text, start (where the line starts in
    // its text box), chords (each `at` along the line), alone (chords with no words),
    // blank (a blank slide's one line: alone, with no chords yet and no stand-ins),
    // first (of its slide) }
    property var blocks: []
    property var rows: []
    property var used: []
    // How many lines the slide with the most has
    property int mostLines: 1

    // The spot: which row, and which character of its line. On a line of chords alone
    // it is a chord's stand-in (every other character) or, past the last, a new one.
    property int spotRow: -1
    property int spotAt: 0
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
    // A chord being dragged: { row, at, name }, or null
    property var dragged: null
    // What was copied: { lines: [ [{ word, offset, name }] or { alone: [names] } ] }
    property var copied: null

    readonly property int wordSize: 21
    readonly property int chordSize: 14
    readonly property int gutter: 54
    readonly property int bubblePad: 7

    signal failed(string error)
    signal told(string message)
    // A slide's row was pointed at: the editor shows it as the one picked
    signal rowPicked(int row)

    function reload() {
        const read = editor.song()
        const flat = []
        let last = -1
        read.forEach((block, b) => {
            const newSlide = block.row !== last
            if (newSlide && block.groupStart && block.group !== "")
                flat.push({ kind: "group", name: block.group, color: block.groupColor, row: block.row })
            last = block.row
            let start = 0
            block.text.split("\n").forEach((text, l) => {
                const end = start + Math.max(1, text.length)
                flat.push({
                    kind: "line", block: b, line: l, row: block.row, element: block.element, text: text, start: start,
                    chords: block.chords.filter(c => c.at >= start && c.at < end).map(c => ({ at: c.at - start, name: c.name })),
                    alone: Chords.isPlaceholders(text) || block.blank === true, blank: block.blank === true,
                    first: newSlide && l === 0
                })
                start += text.length + 1
            })
        })
        // Each slide's lines know how many they are and which is the last, and the
        // sheet how many the longest slide has: what the cards are sized by.
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
        rows = flat
        used = editor.usedChords()
        if (spotRow >= rows.length || (spotRow >= 0 && !usable(spotRow)))
            spotRow = -1
        // The line a chord was being typed on may have gone (an undo from the toolbar).
        if (typing && !usable(typingRow)) {
            typing = false
            takeKeys()
        }
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
        const all = exact && !line.alone ? Array.from({ length: line.text.length }, (_, i) => i).filter(i => line.text[i] !== " ")
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

    // Moves the spot to the line above or below, to the stop nearest under or over it.
    function stepLine(delta) {
        if (!usable(spotRow)) {
            step(1, false)
            return
        }
        let next = spotRow + delta
        while (next >= 0 && next < rows.length && !usable(next))
            next += delta
        if (!usable(next))
            return
        const from = list.itemAt(spotRow)
        const to = list.itemAt(next)
        const x = from ? from.xOf(spotAt) : 0
        let best = 0
        let distance = 1e9
        for (const stop of stops(next)) {
            const off = Math.abs((to ? to.xOf(stop) : 0) - x)
            if (off < distance) {
                distance = off
                best = stop
            }
        }
        setSpot(next, best)
        reveal()
    }

    function reveal() {
        const item = list.itemAt(spotRow)
        if (!item)
            return
        const top = item.y
        if (top < flick.contentY + 10)
            flick.contentY = Math.max(0, top - 30)
        else if (top + item.height > flick.contentY + flick.height - 10)
            flick.contentY = Math.min(Math.max(0, flick.contentHeight - flick.height), top + item.height - flick.height + 30)
        if (usable(spotRow))
            rowPicked(rows[spotRow].row)
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

    // Puts a chord on a place, in place of the one there if there is one.
    function put(index, at, name, joined) {
        // A slash typed on the way to a bass note that never came is not part of the chord.
        name = name.replace(/\/+$/, "")
        if (!usable(index) || !Chords.isChord(name))
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
            if (put(toRow, toAt, name, false) && !copy)
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
        const lines = []
        for (let index = from; index <= to; ++index) {
            if (!usable(index))
                continue
            const line = rows[index]
            if (line.alone) {
                lines.push({ alone: line.chords.map(c => c.name) })
                continue
            }
            const starts = wordStarts(line.text)
            lines.push(line.chords.map(chord => {
                let word = 0
                for (let w = 0; w < starts.length; ++w) {
                    if (starts[w] <= chord.at)
                        word = w
                }
                return { word: word, offset: chord.at - (starts[word] ?? 0), name: chord.name }
            }))
        }
        copied = { lines: lines }
        const count = lines.reduce((sum, line) => sum + (line.alone ? line.alone.length : line.length), 0)
        told(count + (count === 1 ? " chord" : " chords") + " copied from " + (lines.length === 1 ? "the line" : lines.length + " lines")
             + ": Ctrl+V puts them on the lines from the spot's on, word for word")
    }

    function paste() {
        if (!copied || !usable(spotRow))
            return
        let index = spotRow
        let first = true
        for (const line of copied.lines) {
            while (index < rows.length && !usable(index))
                ++index
            if (index >= rows.length)
                break
            const target = rows[index]
            if (line.alone) {
                if (target.alone)
                    report(editor.setChordsAlone(target.row, target.element, target.line, line.alone, !first))
            } else if (!target.alone) {
                const starts = wordStarts(target.text)
                const chords = []
                for (const chord of line) {
                    if (chord.word >= starts.length)
                        continue
                    const start = starts[chord.word]
                    const end = chord.word + 1 < starts.length ? starts[chord.word + 1] - 1 : target.text.length
                    const at = Math.min(start + chord.offset, Math.max(start, end - 1))
                    if (!chords.some(c => c.at === at))
                        chords.push({ at: at, name: chord.name })
                }
                setLine(index, chords, !first)
            }
            first = false
            ++index
        }
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
    function finishTyping(name, onward) {
        const row = typingRow
        const at = typingAt
        typing = false
        takeKeys()
        if (name !== undefined && name !== "")
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
        rowPicked(rows[index].row)
        startTyping()
    }

    function handleKey(event) {
        const control = (event.modifiers & Qt.ControlModifier) !== 0
        const shift = (event.modifiers & Qt.ShiftModifier) !== 0
        if (control && event.key === Qt.Key_C) {
            copy(shift)
        } else if (control && event.key === Qt.Key_V) {
            paste()
        } else if (control && event.key === Qt.Key_Z) {
            report(shift ? editor.redo() : editor.undo())
        } else if (control && event.key === Qt.Key_Y) {
            report(editor.redo())
        } else if (control) {
            return
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
        event.accepted = true
    }

    onRowChanged: {
        // The slide picked in the list at the side comes into view.
        const index = rows.findIndex(line => line.kind === "line" && line.row === row)
        const item = index >= 0 ? list.itemAt(index) : null
        if (item && (item.y < flick.contentY || item.y + item.height > flick.contentY + flick.height))
            flick.contentY = Math.max(0, Math.min(Math.max(0, flick.contentHeight - flick.height), item.y - 40))
    }
    Component.onCompleted: reload()

    // What has the keyboard while no chord is being typed. It is a thing of its own, and
    // not the sheet as a whole, because the sheet's keyboard would go back to whatever
    // in it last had it, which after a chord has been typed is the box it was typed in.
    Item {
        id: keys

        focus: true
        Keys.onPressed: (event) => sheet.handleKey(event)
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

    // A chord's bubble is its name and a margin either side: what a chord is as wide
    // as, for keeping two apart.
    readonly property var bubbleMetrics: ({ advanceWidth: (name) => chordMetrics.advanceWidth(name) + 2 * sheet.bubblePad })
    readonly property real bubbleHeight: chordMetrics.height + 6
    readonly property real tailHeight: 6
    // A line of words with its row of chords over it
    readonly property real lineHeight: bubbleHeight + tailHeight + wordMetrics.height + 6
    // The cards the slides are in: the room inside one round its lines, the gap between
    // two, and their size, which is the same for all: as wide as the widest line of the
    // song with its chords, and as tall as the slide with the most lines.
    readonly property real cardPad: 10
    readonly property real cardGap: 10
    readonly property real cardHeight: mostLines * lineHeight + 2 * cardPad
    readonly property real cardWidth: {
        let widest = 240
        for (const line of rows) {
            if (line.kind === "line")
                widest = Math.max(widest, ChordLayout.place(line.text, line.chords, wordMetrics, bubbleMetrics, 4).width + (line.alone ? 50 : 0))
        }
        return widest + 2 * cardPad + 8
    }

    component Chip: Rectangle {
        id: chip

        property string label
        property string caption
        property bool picked: false

        signal clicked

        width: Math.max(34, chipName.implicitWidth + 16 + (caption !== "" ? chipCaption.implicitWidth + 5 : 0))
        height: 26
        radius: 6
        color: picked ? sheet.accentColor : chipMouse.pressed ? "#50535a" : chipMouse.containsMouse ? "#45484e" : "#3a3c42"

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

            anchors.fill: parent
            hoverEnabled: true
            onClicked: chip.clicked()
        }
    }

    Rectangle {
        anchors.fill: parent
        color: "#1b1c1f"
    }

    // The key, and its own chords on the keys 1 to 7
    Rectangle {
        id: bar

        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: parent.top
        height: 44
        color: "#202226"

        Row {
            x: 14
            anchors.verticalCenter: parent.verticalCenter
            spacing: 8

            Text {
                anchors.verticalCenter: parent.verticalCenter
                color: "#9a9da3"
                font.pixelSize: 13
                text: "Key"
            }

            AppComboBox {
                id: keyBox

                objectName: "chordKeyBox"
                anchors.verticalCenter: parent.verticalCenter
                width: 82
                height: 28
                font.pixelSize: 13
                // Every usual key, and the song's own first if it is not one of them
                readonly property var usual: Chords.majorKeys.concat(Chords.minorKeys)
                model: usual.includes(sheet.key) ? usual : [sheet.key].concat(usual)
                currentIndex: model.indexOf(sheet.key)
                onActivated: (index) => {
                    sheet.report(sheet.editor.setKey(model[index]))
                    sheet.takeKeys()
                }
            }

            Item {
                width: 10
                height: 1
            }

            Repeater {
                model: sheet.inKey

                Chip {
                    required property string modelData
                    required property int index

                    objectName: "keyChord" + (index + 1)
                    anchors.verticalCenter: parent.verticalCenter
                    caption: String(index + 1)
                    label: modelData
                    // On the spot; or, while a chord is being typed, in place of what
                    // is typed, where it was being typed.
                    onClicked: {
                        if (sheet.typing)
                            sheet.finishTyping(modelData, false)
                        else
                            sheet.put(sheet.spotRow, sheet.spotAt, modelData)
                        sheet.takeKeys()
                    }
                }
            }
        }

        Text {
            anchors.right: parent.right
            anchors.rightMargin: 14
            anchors.verticalCenter: parent.verticalCenter
            visible: sheet.editor.key === ""
            color: "#8d9097"
            font.pixelSize: 12
            text: "The song names no key: pick the one its chords are in"
        }
    }

    Flickable {
        id: flick

        objectName: "chordSheetView"
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: bar.bottom
        anchors.bottom: parent.bottom
        clip: true
        contentWidth: Math.max(width, column.width + 30)
        contentHeight: column.height + 60
        boundsBehavior: Flickable.StopAtBounds

        ScrollBar.vertical: ScrollBar {}
        ScrollBar.horizontal: ScrollBar {}

        KineticWheel {}

        // A click on the bare sheet takes the keys back from wherever they were, and
        // keeps the chord that was being typed.
        MouseArea {
            width: flick.contentWidth
            height: flick.contentHeight
            onPressed: sheet.takeKeys()
            onClicked: sheet.settle()
        }

        Column {
            id: column

            x: 16
            y: 14
            width: Math.max(flick.width - 32, sheet.gutter + sheet.cardWidth + 20)

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
                    readonly property var placed: ChordLayout.place(words, chords, wordMetrics, sheet.bubbleMetrics, 4)
                    // Room over the first line of a slide, for the gap between two cards
                    // and the top of its own; and under its last, for the foot of the
                    // card and for the lines it has fewer of than the longest slide.
                    readonly property real lead: isLine && modelData.first ? sheet.cardGap + sheet.cardPad : 0
                    readonly property real foot: isLine && modelData.last === true
                                                 ? (sheet.mostLines - modelData.count) * sheet.lineHeight + sheet.cardPad : 0
                    readonly property real chordTop: lead + 2
                    readonly property real wordsTop: chordTop + sheet.bubbleHeight + sheet.tailHeight

                    // Where a place on the line is, from the line's left edge: a chord's
                    // bubble if one is there, the character if not, and on a line of
                    // chords alone the next free place for a new one.
                    function xOf(at) {
                        const found = chords.findIndex(c => c.at === at)
                        if (found >= 0)
                            return sheet.gutter + placed.xs[found]
                        if (modelData.alone) {
                            const last = chords.length - 1
                            return sheet.gutter + (last >= 0 ? placed.xs[last] + placed.widths[last] + 10 : 0)
                        }
                        return sheet.gutter + wordMetrics.advanceWidth(words.substring(0, at))
                    }

                    // The place under the pointer: the word it is over, or `exact` the
                    // character.
                    function placeAt(x, exact) {
                        const along = x - sheet.gutter
                        if (modelData.alone) {
                            for (let slot = 0; slot < chords.length; ++slot) {
                                if (along < placed.xs[slot] + placed.widths[slot] + 5)
                                    return slot * 2
                            }
                            return chords.length * 2
                        }
                        let character = ChordLayout.characterAt(words, Math.max(0, along), wordMetrics)
                        // Over the space between two words it is the word after that
                        // is meant.
                        if (exact && words[character] === " " && character + 1 < words.length && words[character + 1] !== " ")
                            ++character
                        return exact ? character : sheet.stopAt(index, character)
                    }

                    width: column.width
                    height: !isLine ? 34 : lead + sheet.lineHeight + foot

                    // The slide's card, drawn by its first line and reaching down
                    // behind the rest of them
                    Rectangle {
                        visible: line.isLine && line.modelData.first
                        x: sheet.gutter - sheet.cardPad
                        y: sheet.cardGap
                        z: -1
                        width: sheet.cardWidth
                        height: sheet.cardHeight
                        radius: 8
                        color: "#222428"
                        border.width: 1
                        border.color: line.isLine && line.modelData.row === sheet.row ? "#8a5a22" : "#33363c"
                    }

                    // The name of a group, in its colour, with its chords to copy
                    Rectangle {
                        visible: !line.isLine
                        y: 10
                        width: groupName.implicitWidth + 20
                        height: 22
                        radius: 4
                        color: line.isLine ? "transparent" : sheet.groupColor({ group: line.modelData.name, groupColor: line.modelData.color })

                        Text {
                            id: groupName

                            anchors.centerIn: parent
                            color: "white"
                            font.pixelSize: 12
                            font.bold: true
                            text: line.isLine ? "" : line.modelData.name
                        }
                    }

                    // The slide's number, beside its card
                    Text {
                        visible: line.isLine && line.modelData.first
                        y: line.wordsTop + 4
                        width: sheet.gutter - 20
                        horizontalAlignment: Text.AlignRight
                        color: line.isLine && line.modelData.row === sheet.row ? sheet.accentColor : "#5c5f66"
                        font.pixelSize: 12
                        text: line.isLine ? line.modelData.row + 1 : ""
                    }

                    Text {
                        visible: line.isLine
                        x: sheet.gutter
                        y: line.wordsTop
                        color: "#e6e6e6"
                        font: wordMetrics.font
                        // A line of chords alone has nothing to read: a rule stands
                        // where its words would be.
                        text: line.isLine && !line.modelData.alone ? line.words : ""
                    }

                    Rectangle {
                        visible: line.isLine && line.modelData.alone
                        x: sheet.gutter
                        y: line.wordsTop + wordMetrics.height / 2
                        width: Math.max(60, line.placed.width + 40)
                        height: 1
                        color: "#3a3c42"
                    }

                    // Following the pointer, and a click to type a chord there. It is as
                    // wide as the slide's card and no wider: beside the cards is bare
                    // sheet. The spot follows the pointer while a chord is being typed
                    // too, to show where a click would start the next (sheet.pick).
                    MouseArea {
                        width: Math.min(parent.width, sheet.gutter + sheet.cardWidth - sheet.cardPad)
                        height: parent.height
                        enabled: sheet.usable(line.index)
                        hoverEnabled: true
                        onPositionChanged: (mouse) => {
                            if (sheet.pointerMoved(this, mouse.x, mouse.y) && !sheet.dragged)
                                sheet.setSpot(line.index, line.placeAt(mouse.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
                        }
                        onPressed: (mouse) => {
                            sheet.takeKeys()
                            sheet.setSpot(line.index, line.placeAt(mouse.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
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
                            readonly property bool atSpot: sheet.spotRow === line.index && sheet.spotAt === modelData.at
                            readonly property bool lifted: sheet.dragged !== null && sheet.dragged.row === line.index
                                                           && sheet.dragged.at === modelData.at
                            // Where its character is, which is where the tail points
                            // even when the bubble has had to move right of it
                            readonly property real characterX: line.modelData.alone ? line.placed.xs[index] + width / 2
                                                               : wordMetrics.advanceWidth(line.words.substring(0, modelData.at))

                            objectName: "chord:" + line.index + ":" + modelData.at
                            x: sheet.gutter + line.placed.xs[index]
                            y: line.chordTop
                            width: line.placed.widths[index]
                            height: sheet.bubbleHeight + sheet.tailHeight
                            opacity: lifted ? 0.35 : 1

                            Rectangle {
                                width: parent.width
                                height: sheet.bubbleHeight
                                radius: 6
                                color: bubbleMouse.containsMouse || bubble.atSpot ? "#4a3a26" : "#34363c"
                                border.width: 1
                                border.color: bubble.atSpot ? sheet.accentColor : "#5c5f66"

                                Text {
                                    anchors.centerIn: parent
                                    color: "#ffd9a8"
                                    font: chordMetrics.font
                                    text: bubble.modelData.name
                                }
                            }

                            // The tail
                            Rectangle {
                                x: Math.max(3, Math.min(parent.width - 5, bubble.characterX - line.placed.xs[bubble.index] + 1))
                                y: sheet.bubbleHeight - 1
                                width: 2
                                height: sheet.tailHeight + 3
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
                                    pressedAt = Qt.point(mouse.x, mouse.y)
                                    moved = false
                                }
                                onPositionChanged: (mouse) => {
                                    if (!pressed) {
                                        if (sheet.pointerMoved(this, mouse.x, mouse.y) && !sheet.dragged)
                                            sheet.setSpot(line.index, bubble.modelData.at)
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
                                    const where = mapToItem(column, mouse.x, mouse.y)
                                    const under = column.childAt(Math.max(1, Math.min(column.width - 1, where.x)), where.y)
                                    if (under && under.isLine && sheet.usable(under.index))
                                        sheet.setSpot(under.index, under.placeAt(where.x, (mouse.modifiers & Qt.ShiftModifier) === 0))
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
            visible: item !== null && !underTyping && (free || sheet.dragged !== null)
            x: item ? column.x + item.x + item.xOf(sheet.spotAt) : 0
            y: item ? column.y + item.y + item.chordTop : 0
            width: Math.max(26, sheet.dragged ? chordMetrics.advanceWidth(sheet.dragged.name) + 2 * sheet.bubblePad : 0)
            height: sheet.bubbleHeight + sheet.tailHeight

            Rectangle {
                width: parent.width
                height: sheet.bubbleHeight
                radius: 6
                color: sheet.dragged ? "#4a3a26" : "transparent"
                border.width: 1
                border.color: sheet.accentColor
                opacity: sheet.dragged ? 1 : 0.75

                Text {
                    anchors.centerIn: parent
                    color: sheet.dragged ? "#ffd9a8" : sheet.accentColor
                    font: chordMetrics.font
                    text: sheet.dragged ? sheet.dragged.name : "+"
                }
            }

            Rectangle {
                x: 3
                y: sheet.bubbleHeight - 1
                width: 2
                height: sheet.tailHeight + wordMetrics.height + 3
                color: sheet.accentColor
                opacity: 0.75
            }
        }

        // The bubble a chord is typed in, at the place it is being typed for, with what
        // it might be going to be over it. It grows upwards from where the chord will
        // stand, so that the word being given the chord, and the rest of its line, stay
        // in sight.
        Rectangle {
            id: entry

            readonly property var item: sheet.laid >= 0 && sheet.typingRow >= 0 ? list.itemAt(sheet.typingRow) : null
            property string last
            property int pick: -1
            readonly property var offered: sheet.typing ? Chords.completions(field.text, sheet.key, sheet.used).slice(0, 14) : []

            function begin(text, selected) {
                last = text
                field.text = text
                pick = -1
                field.forceActiveFocus()
                if (selected)
                    field.selectAll()
                else
                    field.cursorPosition = text.length
            }

            // What Enter takes: the one picked from the list, or what is typed
            function taken() {
                return pick >= 0 && pick < offered.length ? offered[pick] : Chords.tidied(field.text)
            }

            objectName: "chordEntry"
            visible: sheet.typing && item !== null
            x: item ? Math.min(column.x + item.x + item.xOf(sheet.typingAt), flick.contentX + flick.width - width - 12) : 0
            y: item ? Math.max(flick.contentY + 4, column.y + item.y + item.chordTop + sheet.bubbleHeight + 3 - height) : 0
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
                color: "#ffd9a8"
                selectionColor: sheet.accentColor
                selectedTextColor: "#1b1c1f"
                font.pixelSize: 16
                font.bold: true
                maximumLength: 16
                // Only what could be a chord gets in, and its notes are made capitals
                // as they are typed.
                onTextEdited: {
                    const tidy = Chords.tidied(text)
                    if (!Chords.couldBecome(tidy)) {
                        const at = Math.max(0, cursorPosition - 1)
                        text = entry.last
                        cursorPosition = Math.min(at, text.length)
                        return
                    }
                    if (tidy !== text) {
                        const at = cursorPosition
                        text = tidy
                        cursorPosition = Math.min(at, text.length)
                    }
                    entry.last = text
                    entry.pick = -1
                }
                Keys.onPressed: (event) => {
                    if (event.key === Qt.Key_Escape) {
                        sheet.finishTyping(undefined, false)
                    } else if (event.key === Qt.Key_Return || event.key === Qt.Key_Enter) {
                        sheet.finishTyping(entry.taken(), false)
                    } else if (event.key === Qt.Key_Tab) {
                        // The first offered, if nothing is picked and nothing typed is
                        // a chord yet
                        if (entry.pick < 0 && !Chords.isChord(text) && entry.offered.length > 0)
                            entry.pick = 0
                        sheet.finishTyping(entry.taken(), true)
                    } else if (event.key === Qt.Key_Down) {
                        entry.pick = entry.offered.length > 0 ? (entry.pick + 1) % entry.offered.length : -1
                    } else if (event.key === Qt.Key_Up) {
                        entry.pick = entry.offered.length > 0 ? (entry.pick + entry.offered.length - 1 + (entry.pick < 0 ? 1 : 0)) % entry.offered.length : -1
                    } else if (event.key === Qt.Key_Right && entry.pick >= 0 && cursorPosition === text.length) {
                        text = entry.offered[entry.pick]
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

                        label: modelData
                        picked: index === entry.pick
                        onClicked: sheet.finishTyping(modelData, false)
                    }
                }
            }
        }
    }
}
