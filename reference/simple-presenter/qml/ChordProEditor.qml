import QtQuick
import QtQuick.Controls.Basic
import SimplePresenterApp

// The song as ChordPro text: its words in one piece, with each chord in square brackets
// where it is played, to be typed over. The other chord editor (ChordSheet) is for the
// mouse; this one is for someone who thinks in ChordPro, or has a chart to copy from.
//
// Only the chords can be changed. The words are the presentation's, a text box on each
// slide, each with its own format, and letting them be typed over here would mean
// working out which slide every changed word belongs to; so for now a change is let
// through only if, with the chords taken out, the text is the words it started as
// (check()). Anything else is put back as it was the moment it is typed. That one rule
// covers typing in the wrong place, pasting, cutting and dropping alike.
//
// The text, line by line (`layout` says what each line is):
//   {c: Verse 1}     the name of a group, over its first slide. Not to be changed.
//   (an empty line)  between one slide and the next. Not to be changed.
//   [G]Words and [D]more words     a line of a slide's words, with its chords
//   [C] [G] [Am]     a line of chords with no words (an intro's): here chords can be
//                    added and taken away freely, with spaces between
//   (an empty line)  a blank slide: a line of chords with no words that has none yet,
//                    and takes them the same way. It looks like the empty line between
//                    two slides, which stands before it, and only `layout` tells the
//                    two apart: with the caret on this one its slide is the one
//                    picked in the list at the side, and what is typed on it stays.
//
// Help with the brackets, since they are all there is to type: "[" brings its "]" with
// it and leaves the caret between them; "]" typed at a "]" steps over it; Backspace or
// Delete on a bracket takes the whole chord. Inside brackets anything that could be a
// chord can be typed (chords::couldBecome).
//
// A change is saved a moment after the typing stops, to the text boxes whose chords are
// no longer what the text says, and once more on the way out. Empty brackets, and ones
// that hold what is not yet a chord, are left out of what is saved and left in the text.
FocusScope {
    id: pro

    required property var editor
    // The slide to have in view: its row in the editor
    property int row: 0
    readonly property color accentColor: "#ff8a1f"
    property var blocks: []
    // What each line of the text is: { kind: "group" | "gap" | "line", text (what it is
    // with no chords in it), block, line, start, alone (chords with no words: a line
    // of stand-ins, or a blank slide's line, which has none yet) }
    property var layout: []
    // The text as it last was when it passed check()
    property string good
    // The text is being set from here, not typed
    property bool setting: false
    // The words have square brackets of their own in them, which ChordPro text cannot
    // tell from chords: the song can be read here and not changed.
    property bool barred: false

    signal failed(string error)
    signal rowPicked(int row)

    function composed(read) {
        const lines = []
        const kinds = []
        let last = -1
        read.forEach((block, b) => {
            const newSlide = block.row !== last
            if (newSlide && last >= 0) {
                lines.push("")
                kinds.push({ kind: "gap", text: "" })
            }
            if (newSlide && block.groupStart && block.group !== "") {
                const name = "{c: " + block.group + "}"
                lines.push(name)
                kinds.push({ kind: "group", text: name })
            }
            last = block.row
            let start = 0
            block.text.split("\n").forEach((text, l) => {
                const end = start + Math.max(1, text.length)
                const chords = block.chords.filter(c => c.at >= start && c.at < end).map(c => ({ at: c.at - start, name: c.name }))
                lines.push(Chords.toChordPro(text, chords))
                kinds.push({ kind: "line", text: text, block: b, line: l, start: start,
                             alone: Chords.isPlaceholders(text) || block.blank === true, row: block.row })
                start += text.length + 1
            })
        })
        return { text: lines.join("\n"), layout: kinds }
    }

    // Reads the song again. With `keep`, the text is left as it is being typed if it
    // still says what the song now has (which it does after a change made from here).
    function reload(keep) {
        const read = editor.song()
        const made = composed(read)
        blocks = read
        layout = made.layout
        barred = read.some(block => /[\[\]]/.test(block.text))
        if (keep && check(area.text)) {
            good = area.text
            return
        }
        const caret = area.cursorPosition
        setting = true
        area.text = made.text
        good = made.text
        area.cursorPosition = Math.min(caret, area.length)
        setting = false
    }

    // Whether text is the song's words with nothing changed but the chords.
    function check(text) {
        const lines = text.split("\n")
        if (lines.length !== layout.length)
            return false
        for (let i = 0; i < lines.length; ++i) {
            const want = layout[i]
            const bare = Chords.withoutChords(lines[i])
            if (want.kind !== "line") {
                if (lines[i] !== want.text)
                    return false
                continue
            }
            if (bare.includes("[") || bare.includes("]"))
                return false
            // Whatever is in brackets is a chord or on its way to being one.
            const inside = lines[i].match(/\[[^\[\]]*\]/g) ?? []
            if (inside.some(chord => !Chords.couldBecome(chord.slice(1, -1).trim())))
                return false
            if (want.alone ? bare.trim() !== "" : bare !== want.text)
                return false
        }
        return true
    }

    // Saves the chords the text has, wherever they are not what the song has.
    function apply() {
        saver.stop()
        if (barred || !check(area.text))
            return
        const lines = area.text.split("\n")
        let first = true
        const byBlock = new Map()
        for (let i = 0; i < lines.length; ++i) {
            const want = layout[i]
            if (want.kind !== "line")
                continue
            const chords = Chords.chordsOf(lines[i]).filter(c => Chords.isChord(c.name))
            if (want.alone) {
                const block = blocks[want.block]
                const end = want.start + Math.max(1, want.text.length)
                const had = block.chords.filter(c => c.at >= want.start && c.at < end).map(c => c.name)
                const names = chords.map(c => c.name)
                if (JSON.stringify(had) !== JSON.stringify(names)) {
                    const error = editor.setChordsAlone(want.row, block.element, want.line, names, !first)
                    if (error !== "") {
                        failed(error)
                        return
                    }
                    first = false
                    // Its text box has changed under the lines that follow: they are
                    // read again and the rest saved on the next round.
                    reload(true)
                    saver.restart()
                    return
                }
                continue
            }
            if (!byBlock.has(want.block))
                byBlock.set(want.block, [])
            for (const chord of chords) {
                // One after the last word hangs on the last character.
                const at = Math.min(chord.at, Math.max(0, want.text.length - 1))
                if (want.text.length > 0)
                    byBlock.get(want.block).push({ at: want.start + at, name: chord.name })
            }
        }
        for (const [b, chords] of byBlock) {
            const block = blocks[b]
            // Lines of chords alone keep theirs: they were dealt with above.
            const alone = block.chords.filter(c => layout.some(l => l.kind === "line" && l.block === b && l.alone
                                                                    && c.at >= l.start && c.at < l.start + Math.max(1, l.text.length)))
            const wanted = chords.concat(alone).sort((x, y) => x.at - y.at)
            const had = block.chords.map(c => ({ at: c.at, name: c.name }))
            if (JSON.stringify(had) === JSON.stringify(wanted))
                continue
            const error = editor.setChords(block.row, block.element, wanted, !first)
            if (error !== "") {
                failed(error)
                return
            }
            first = false
        }
    }

    // The chord the caret is in or beside: where its brackets are, or null.
    function chordAround(position) {
        const text = area.text
        const lineStart = text.lastIndexOf("\n", position - 1) + 1
        let open = text.lastIndexOf("[", position - 1)
        if (open < lineStart)
            return null
        const close = text.indexOf("]", open)
        const lineEnd = text.indexOf("\n", open)
        if (close < 0 || (lineEnd >= 0 && close > lineEnd) || close < position - 1)
            return null
        return { open: open, close: close }
    }

    // Gives the text the keyboard.
    function takeKeys() {
        area.forceActiveFocus()
    }

    // Saves what has been typed, without waiting for the typing to pause: what the
    // editor asks of whichever chord editor is up before it turns to something else.
    function settle() {
        apply()
    }

    function lineOf(position) {
        return area.text.substring(0, position).split("\n").length - 1
    }

    onRowChanged: {
        const index = layout.findIndex(line => line.kind === "line" && line.row === row)
        if (index < 0 || lineOf(area.cursorPosition) >= 0 && layout[lineOf(area.cursorPosition)]?.row === row)
            return
        const start = area.text.split("\n").slice(0, index).join("\n").length + (index > 0 ? 1 : 0)
        area.cursorPosition = start
    }
    Component.onCompleted: reload(false)
    Component.onDestruction: apply()

    Connections {
        target: pro.editor

        // A change made here leaves the text as typed; one made elsewhere (an undo)
        // shows in it.
        function onSlideChanged() {
            pro.reload(true)
        }

        function onDocumentChanged() {
            pro.reload(false)
        }
    }

    Timer {
        id: saver

        interval: 700
        onTriggered: pro.apply()
    }

    Rectangle {
        anchors.fill: parent
        color: "#1b1c1f"
    }

    Rectangle {
        id: note

        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: parent.top
        height: 34
        color: "#202226"

        Text {
            x: 14
            anchors.verticalCenter: parent.verticalCenter
            width: parent.width - 28
            elide: Text.ElideRight
            color: pro.barred ? "#ffb4a8" : "#9a9da3"
            font.pixelSize: 12
            text: pro.barred ? "This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text. The chord editor can still be used."
                             : "Type chords in square brackets among the words: [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the slides'."
        }
    }

    Flickable {
        id: flick

        objectName: "chordProView"
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.top: note.bottom
        anchors.bottom: parent.bottom
        clip: true
        contentWidth: Math.max(width, area.implicitWidth + 40)
        contentHeight: area.implicitHeight + 40
        boundsBehavior: Flickable.StopAtBounds

        ScrollBar.vertical: ScrollBar {}
        ScrollBar.horizontal: ScrollBar {}

        function follow(caret) {
            if (caret.y < contentY + 10)
                contentY = Math.max(0, caret.y - 20)
            else if (caret.y + caret.height > contentY + height - 10)
                contentY = Math.max(0, caret.y + caret.height - height + 20)
        }

        TextEdit {
            id: area

            objectName: "chordProText"
            x: 20
            y: 16
            width: Math.max(flick.width - 40, implicitWidth)
            focus: true
            readOnly: pro.barred
            textFormat: TextEdit.PlainText
            wrapMode: TextEdit.NoWrap
            selectByMouse: true
            color: "#e6e6e6"
            selectionColor: "#7a4a1a"
            selectedTextColor: "#ffffff"
            font.family: "monospace"
            font.pixelSize: 16
            onCursorRectangleChanged: flick.follow(cursorRectangle)
            onCursorPositionChanged: {
                const line = pro.layout[pro.lineOf(cursorPosition)]
                if (line && line.kind === "line" && activeFocus)
                    pro.rowPicked(line.row)
            }
            onTextChanged: {
                if (pro.setting)
                    return
                if (pro.check(text)) {
                    pro.good = text
                    saver.restart()
                    return
                }
                // Not the song's words any more: as it was.
                const caret = cursorPosition
                const shorter = text.length < pro.good.length
                pro.setting = true
                text = pro.good
                cursorPosition = Math.max(0, Math.min(length, shorter ? caret : caret - 1))
                pro.setting = false
            }
            Keys.onPressed: (event) => {
                if (pro.barred || (event.modifiers & (Qt.ControlModifier | Qt.AltModifier | Qt.MetaModifier)))
                    return
                const before = cursorPosition > 0 ? text[cursorPosition - 1] : ""
                const after = cursorPosition < length ? text[cursorPosition] : ""
                if (event.text === "[" && selectedText === "") {
                    insert(cursorPosition, "[]")
                    cursorPosition -= 1
                } else if (event.text === "]" && after === "]" && selectedText === "") {
                    cursorPosition += 1
                } else if (event.key === Qt.Key_Backspace && selectedText === "" && (before === "[" || before === "]")) {
                    const chord = pro.chordAround(before === "[" ? cursorPosition : cursorPosition - 1)
                    if (!chord)
                        return
                    remove(chord.open, chord.close + 1)
                } else if (event.key === Qt.Key_Delete && selectedText === "" && (after === "[" || after === "]")) {
                    const chord = pro.chordAround(after === "[" ? cursorPosition + 1 : cursorPosition)
                    if (!chord)
                        return
                    remove(chord.open, chord.close + 1)
                } else {
                    return
                }
                event.accepted = true
            }

            ChordHighlighter {
                document: area.textDocument
            }
        }
    }
}
