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
// A change is saved a moment after the typing stops, to the text boxes that were typed
// in, and once more on the way out. Empty brackets, and ones that hold what is not yet
// a chord, are left out of what is saved and left in the text.
//
// What has come back from the editor that was made of this one for the Mac (Pro7 Chord
// Editor), each said again where it is done:
//   - Only the text boxes that were typed in are saved. A box is saved when what the
//     text says of it is no longer what the text said when it was made from the song,
//     and not when it is other than what the FILE has: the two can differ before
//     anything is typed (a chord whose name is not tidy, or one past the end of its
//     words, which the text cannot show as it is), and typing in one box then wrote
//     every such box of the song.
//   - All of one saving is one step to undo, however many text boxes it changed, and a
//     line of chords alone no longer waits for the next pause to be followed by the
//     rest.
//   - A name the song already had is let through whatever it is ("N.C."). Before, a
//     song with one name that was no chord could not be changed here at all, and
//     nothing said why.
//   - A save that is refused makes the text again from the song, so that what is seen
//     is what the refusal says: nothing was changed.
//   - A change to the song from elsewhere (an undo from the toolbar) shows in the text.
//   - The text has a size of its own, kept apart from the chord sheet's.
FocusScope {
    id: pro

    required property var editor
    // The slide to have in view: its row in the editor
    property int row: 0
    readonly property color accentColor: "#ff8a1f"
    // The colour of a chord in the text: the one it comes with, or the one chosen in
    // the settings. Only how this app draws a chord: no file says a colour.
    property color chordColor: "#ffb45e"
    // How many times its usual size the text is: one of `zooms`, the steps the chord
    // sheet goes by. It is the app's, kept from one start to the next; whoever owns it
    // hears of a change asked for here by zoomEdited() and sets it.
    property real zoom: 1
    readonly property var zooms: [0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.1, 1.25, 1.5]
    property var blocks: []
    // What each line of the text is: { kind: "group" | "gap" | "line", text (what it is
    // with no chords in it), block, line, start, alone (chords with no words: a line
    // of stand-ins, or a blank slide's line, which has none yet) }
    property var layout: []
    // The lines of the text as it is made from the song as the song now is: what a
    // line is held against to tell whether it has been typed in
    property var made: []
    // The names the song's chords had when it was shown here, whatever they are
    property var known: new Set()
    // The text as it last was when it passed check()
    property string good
    // The text is being set from here, not typed
    property bool setting: false
    // What was typed is being saved: the song changing is this editor's own doing
    property bool applying: false
    // Why the song can be read here and not changed, as a sentence; "" if it can be.
    // Either its words have square brackets of their own in them, which ChordPro text
    // cannot tell from chords; or the text as it is made from the song is not text
    // that could be typed here, so that nothing typed in it ever could be either.
    property string barredWhy
    readonly property bool barred: barredWhy !== ""

    signal failed(string error)
    signal rowPicked(int row)
    signal zoomEdited(real zoom)

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
        const now = composed(read)
        blocks = read
        layout = now.layout
        made = now.text.split("\n")
        barredWhy = read.some(block => /[\[\]]/.test(block.text))
            ? "This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text. The chord editor can still be used."
            : check(now.text) ? ""
            : "This song cannot be changed as ChordPro text: one of its chords has a square bracket or a line break in its name. The chord editor can still be used."
        if (keep && check(area.text)) {
            good = area.text
            return
        }
        const caret = area.cursorPosition
        setting = true
        area.text = now.text
        good = now.text
        area.cursorPosition = Math.min(caret, area.length)
        setting = false
    }

    // Every "[...]" of a line that has no bracket inside it, in order: where its
    // brackets are, the place it has among the line's words, and what is between them
    // exactly as it stands.
    function pairs(line) {
        const found = []
        let at = 0
        for (let i = 0; i < line.length;) {
            if (line[i] === "[") {
                let j = i + 1
                while (j < line.length && line[j] !== "[" && line[j] !== "]")
                    ++j
                if (j < line.length && line[j] === "]") {
                    found.push({ open: i, close: j, at: at, inside: line.substring(i + 1, j) })
                    i = j + 1
                    continue
                }
            }
            ++at
            ++i
        }
        return found
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
            // Whatever is in brackets is a chord or on its way to being one; or it is,
            // exactly, a name the song had already, which is let through whatever it is.
            if (pairs(lines[i]).some(pair => !known.has(pair.inside) && !Chords.couldBecome(pair.inside.trim())))
                return false
            if (want.alone ? bare.trim() !== "" : bare !== want.text)
                return false
        }
        return true
    }

    // The chords a line of the text says, each at the place it has among the line's
    // words, its name tidied: every bracket that holds a chord, or a name the song had
    // already. Empty brackets and half-typed ones are left out: someone half way
    // through typing "[F#" must not have it saved as a chord nor taken away from them.
    function said(line) {
        const chords = []
        for (const pair of pairs(line)) {
            const name = Chords.tidied(pair.inside)
            if (name !== "" && (known.has(pair.inside) || Chords.isChord(name)))
                chords.push({ at: pair.at, name: name })
        }
        return chords
    }

    // The chords some lines of text say a text box's lines of words have, each at its
    // place along the whole box. One typed after the last word of a line hangs on the
    // line's last character; a line with no characters has none to hang one on.
    function saidOfBox(b, lines) {
        const chords = []
        layout.forEach((want, i) => {
            if (want.kind !== "line" || want.block !== b || want.alone || want.text.length === 0)
                return
            for (const chord of said(lines[i] ?? ""))
                chords.push({ at: want.start + Math.min(chord.at, want.text.length - 1), name: chord.name })
        })
        return chords
    }

    // Saves the chords the text has, to the text boxes that were typed in: each box of
    // which the text no longer says what it said when it was made from the song, as one
    // change each, and all of them one step to undo.
    function apply() {
        saver.stop()
        if (barred || applying || !check(area.text))
            return
        applying = true
        let saved = false
        let refused = ""
        // (A round for each line of chords alone at the most, and one more.)
        for (let rounds = layout.length + 1; rounds > 0 && refused === ""; --rounds) {
            const typed = area.text.split("\n")
            if (typed.length !== layout.length)
                break
            // A line of chords alone is saved as its chords' names in order. That can
            // change its text box's words (the stand-ins), so the song is read again
            // after it and the other lines go in the next round, which follows at once.
            let another = false
            for (let i = 0; i < layout.length && !another && refused === ""; ++i) {
                const want = layout[i]
                if (want.kind !== "line" || !want.alone)
                    continue
                const names = said(typed[i]).map(c => c.name)
                if (JSON.stringify(names) === JSON.stringify(said(made[i] ?? "").map(c => c.name)))
                    continue
                refused = editor.setChordsAlone(want.row, blocks[want.block].element, want.line, names, saved)
                if (refused === "") {
                    saved = true
                    reload(true)
                    another = true
                }
            }
            if (another || refused !== "")
                continue
            for (let b = 0; b < blocks.length && refused === ""; ++b) {
                const block = blocks[b]
                if (block.blank)
                    continue
                const now = saidOfBox(b, typed)
                // (Held against what the text said when it was made from the song, not
                // against the file's own chords: see the head of this file.)
                if (JSON.stringify(now) === JSON.stringify(saidOfBox(b, made)))
                    continue
                // Its lines of chords alone keep theirs: they were dealt with above.
                const alone = block.chords.filter(c => layout.some(l => l.kind === "line" && l.block === b && l.alone
                                                                        && c.at >= l.start && c.at < l.start + Math.max(1, l.text.length)))
                const wanted = now.concat(alone).map((chord, order) => ({ chord: chord, order: order }))
                                  .sort((x, y) => x.chord.at - y.chord.at || x.order - y.order).map(entry => entry.chord)
                refused = editor.setChords(block.row, block.element, wanted, saved)
                if (refused === "")
                    saved = true
            }
            break
        }
        applying = false
        if (refused !== "") {
            // What the refusal says is that nothing was changed, and that is then what
            // is to be seen: the text is made again from the song.
            reload(false)
            failed(refused)
        } else if (saved) {
            reload(true)
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

    // Asks for the text a step larger (1) or smaller (-1), or its usual size (0). Only
    // its size changes: not a character of it, nor where the caret is, nor what can be
    // undone.
    function zoomBy(direction) {
        const wanted = direction === 0 ? 1 : zooms[Math.max(0, Math.min(zooms.length - 1, zoomStep(zoom) + direction))]
        if (wanted !== zoom)
            zoomEdited(wanted)
    }

    // Ctrl with minus, with plus (or the equals key, which is plus without Shift) and
    // with 0: answers whether the key was one of them.
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

    onRowChanged: {
        const index = layout.findIndex(line => line.kind === "line" && line.row === row)
        if (index < 0 || lineOf(area.cursorPosition) >= 0 && layout[lineOf(area.cursorPosition)]?.row === row)
            return
        const start = area.text.split("\n").slice(0, index).join("\n").length + (index > 0 ? 1 : 0)
        area.cursorPosition = start
    }
    Component.onCompleted: {
        known = namesOf(editor.song())
        reload(false)
    }
    Component.onDestruction: apply()

    // The names of a song's chords, exactly as they stand
    function namesOf(read) {
        const names = new Set(known)
        for (const block of read) {
            for (const chord of block.chords)
                names.add(chord.name)
        }
        return names
    }

    Connections {
        target: pro.editor

        // A change made here leaves the text as typed; one made elsewhere (an undo
        // from the toolbar) shows in it.
        function onSlideChanged() {
            pro.reload(pro.applying)
        }

        function onDocumentChanged() {
            pro.known = pro.namesOf(pro.editor.song())
            pro.reload(false)
        }
    }

    // What was typed is saved when the app is left for another program, too.
    Connections {
        target: Qt.application

        function onStateChanged() {
            if (Qt.application.state !== Qt.ApplicationActive)
                pro.apply()
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
            width: sizes.x - 28
            elide: Text.ElideRight
            color: pro.barred ? "#ffb4a8" : "#9a9da3"
            font.pixelSize: 12
            text: pro.barred ? pro.barredWhy
                             : "Type chords in square brackets among the words: [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the slides'."
        }

        // The size of the text: a small letter A and a large one, where the chord
        // sheet has the two buttons that zoom it.
        Row {
            id: sizes

            anchors.right: parent.right
            anchors.rightMargin: 14
            anchors.verticalCenter: parent.verticalCenter
            spacing: 6

            component SizeButton: Rectangle {
                id: sizeButton

                property bool larger: false
                property bool available: true

                signal clicked

                width: 28
                height: 24
                radius: 6
                color: sizeMouse.pressed ? "#50535a" : sizeMouse.containsMouse ? "#45484e" : "#3a3c42"
                opacity: available ? 1 : 0.35

                Text {
                    anchors.centerIn: parent
                    anchors.verticalCenterOffset: sizeButton.larger ? 0 : 1
                    color: "#e6e6e6"
                    font.pixelSize: sizeButton.larger ? 16 : 11
                    font.weight: Font.Medium
                    text: "A"
                }

                MouseArea {
                    id: sizeMouse

                    anchors.fill: parent
                    hoverEnabled: true
                    enabled: sizeButton.available
                    onClicked: sizeButton.clicked()
                }
            }

            SizeButton {
                objectName: "chordProSmaller"
                available: pro.canZoomOut
                onClicked: {
                    pro.zoomBy(-1)
                    pro.takeKeys()
                }
            }

            SizeButton {
                objectName: "chordProLarger"
                larger: true
                available: pro.canZoomIn
                onClicked: {
                    pro.zoomBy(1)
                    pro.takeKeys()
                }
            }
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

        // Ctrl with the wheel makes the text larger and smaller, a step for each notch.
        WheelHandler {
            property real turned: 0

            acceptedModifiers: Qt.ControlModifier
            acceptedDevices: PointerDevice.AllDevices
            onWheel: (event) => {
                turned += event.angleDelta.y
                if (Math.abs(turned) >= 120) {
                    pro.zoomBy(turned > 0 ? 1 : -1)
                    turned = 0
                }
            }
        }

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
            font.pixelSize: Math.round(16 * pro.zoom)
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
                if (pro.zoomKey(event)) {
                    event.accepted = true
                    return
                }
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
                color: pro.chordColor
            }
        }
    }
}
