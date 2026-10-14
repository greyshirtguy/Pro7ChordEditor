import QtQuick
import SimplePresenterApp
import "lib.js" as Lib

// A chord carried in from the strip (chords.qml, chords2.qml and chords3.qml have the rest of the chord editors): one
// of the key's seven dragged out of the strip over the chord sheet and let go on a word. That a button says so when it
// is pointed at; that a press let go without a pull is still the click it was; what is seen while one is carried (the
// button's picture with the pointer, the lit bubble where it would land, and nothing lit where it would not); that it
// lands there, in place of a chord that is there, as one step to undo; that let go between two slides, on a frame's
// foot or back on the strip it puts nothing on, and nor does Esc; that one can be carried while another chord is being
// typed, which is kept; a line of chords alone; and the sheet scrolling under one held near its bottom or its top, but
// not upwards on its way in. What is looked at is what is drawn, wherever the thing is one that is drawn.
QtObject {
    id: t

//COMMON
    function sheet() {
        return named("chordSheet")
    }

    function view() {
        return named("chordSheetView")
    }

    function lineItem(index) {
        return Lib.find(sheet(), item => item.isLine === true && item.index === index)
    }

    function lineOf(words) {
        return sheet().rows.findIndex(row => row.kind === "line" && row.text === words)
    }

    // Where a character of a line is on the window, in the words
    function over(index, at) {
        const item = lineItem(index)
        return item.mapToItem(null, item.xOf(at) + 3, item.wordsTop + 6)
    }

    function names(index) {
        return sheet().rows[index].chords.map(c => c.at + ":" + c.name).join(" ")
    }

    function cellOf(row) {
        return sheet().cells.find(cell => cell.row === row)
    }

    // A point of the sheet's page (where its slides are laid out), on the window
    function onWindow(x, y) {
        return view().contentItem.mapToItem(null, x, y)
    }

    // The chords the file has on the text box that has these words
    function chordsOnDisk(words) {
        for (const slide of catalog.open(kept.path).slides) {
            for (const element of slide.elements) {
                if (testInput.plain(element.text).split("\n").includes(words))
                    return (element.chords ?? []).map(c => c.at + ":" + c.name).join(" ")
            }
        }
        return "(no such words)"
    }

    function pixel(item, x, y) {
        const at = item.mapToItem(null, x, y)
        return String(testInput.windowPixel(at.x, at.y))
    }

    // How far apart two colours are, as the most any of their three parts differs
    function apart(a, b) {
        const part = (c, i) => parseInt(String(c).substring(1 + 2 * i, 3 + 2 * i), 16)
        return Math.max(Math.abs(part(a, 0) - part(b, 0)), Math.abs(part(a, 1) - part(b, 1)), Math.abs(part(a, 2) - part(b, 2)))
    }

    // One of the strip's seven buttons, and the picture of one that goes with the pointer
    function button(number) {
        return named("keyChord" + number)
    }

    function carried() {
        return named("carriedChord")
    }

    function spot() {
        return named("chordSpot")
    }

    // Presses a button of the strip and pulls it to a point of the window, in steps; the button of the mouse is left down.
    function pull(number, to, modifiers) {
        const from = centre(button(number))
        testInput.mouse(0, from.x, from.y)
        for (let i = 1; i <= 10; ++i)
            testInput.mouse(1, from.x + (to.x - from.x) * i / 10, from.y + (to.y - from.y) * i / 10, modifiers ?? 0)
    }

    function run() {
        const herald = "Hark the herald angels sing"
        const glory = "Glory to the newborn King"
        const peace = "Peace on earth and mercy mild"
        steps = [
            () => {
                openLibrary(catalog.libraries[0].path)
                startEditing(entry("Hark2"))
                kept.path = entry("Hark2").path
                return 700
            },
            () => {
                click(centre(named("chordsMode")))
                return 900
            },
            () => {
                const s = sheet()
                kept.h = lineOf(herald)
                kept.g = lineOf(glory)
                kept.p = lineOf(peace)
                kept.four = s.inKey[3]
                kept.five = s.inKey[4]
                kept.song = JSON.stringify(editScreen.editor.song())
                kept.hash = testInput.fileHash(kept.path)
                check("the song is on the sheet, with the key's seven chords in the strip over it", kept.h >= 0 && kept.g >= 0 && kept.p >= 0 && s.inKey.length === 7
                      && button(1) !== null && button(7) !== null && button(4).label === kept.four, s.inKey.join(" "))
                // ---- What a button says
                const b = centre(button(4))
                testInput.mouse(1, b.x, b.y)
                return 300
            },
            () => {
                const says = "Puts " + kept.four + " on the spot. The key 4 does the same. Or drag it onto a word."
                check("a button of the strip says its three ways when it is pointed at, on the line along the bottom", sheet().hint === says && editScreen.toolHint === says, editScreen.toolHint)
                // ---- A press let go without a pull is the click it was
                const p = over(kept.h, 9)
                testInput.mouse(1, p.x, p.y)
                return 300
            },
            () => {
                check("the pointer on a word puts the spot there", sheet().spotRow === kept.h && sheet().spotAt === 9 && sheet().hint === "", sheet().spotRow + ":" + sheet().spotAt)
                const b = centre(button(4))
                testInput.mouse(1, b.x, b.y)
                testInput.mouse(0, b.x, b.y)
                testInput.mouse(1, b.x + 2, b.y + 2)
                check("a button pressed and moved a little is not yet carried", sheet().dragged === null && !carried().visible)
                testInput.mouse(2, b.x + 2, b.y + 2)
                return 500
            },
            () => {
                check("and let go, it is a click: the chord goes on the spot", names(kept.h).split(" ").includes("9:" + kept.four) && chordsOnDisk(herald).split(" ").includes("9:" + kept.four), names(kept.h))
                editScreen.editor.undo()
                return 500
            },
            () => {
                check("(which is taken back)", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash)
                // ---- Carried: over the strip still
                const from = centre(button(4))
                testInput.mouse(0, from.x, from.y)
                testInput.mouse(1, from.x + 5, from.y + 5)
                const pic = carried()
                const at = pic.mapToItem(null, 0, 0)
                const b = button(4).mapToItem(null, 0, 0)
                check("pulled six points, it is carried: the button's picture goes with the pointer, held where it was picked up", sheet().carrying && sheet().dragged.name === kept.four
                      && pic.visible && pic.label === kept.four && pic.caption === "4" && Math.abs(at.x - (b.x + 5)) < 1.5 && Math.abs(at.y - (b.y + 5)) < 1.5,
                      JSON.stringify(sheet().dragged) + " " + at.x + "," + at.y + " from " + b.x + "," + b.y)
                check("over the strip nothing is lit as where it would land, and the pointer is a closed hand", !sheet().carriedOver && !spot().visible
                      && testInput.pointerShape() === Qt.ClosedHandCursor, spot().visible + " " + testInput.pointerShape())
                return 300
            },
            () => {
                // ---- Over a word
                const from = centre(button(4))
                const to = over(kept.g, 23)
                for (let i = 1; i <= 10; ++i)
                    testInput.mouse(1, from.x + 5 + (to.x - from.x - 5) * i / 10, from.y + 5 + (to.y - from.y - 5) * i / 10)
                return 300
            },
            () => {
                const s = spot()
                const pic = carried()
                check("over a word the spot goes to the character under the pointer", sheet().carriedOver && sheet().spotRow === kept.g && sheet().spotAt === 23, sheet().spotRow + ":" + sheet().spotAt)
                const lit = pixel(s, 3, s.height / 4)
                const name = Lib.find(s, item => item.text !== undefined && item.text === kept.four)
                check("and a lit bubble there shows the chord's name, which is where it would land", s.visible && name !== null && name.visible && apart(lit, "#4a3a26") < 24,
                      lit + " " + (name ? name.text : "no name"))
                // (It can be seen through, so as not to hide the word it is held over: what is drawn there is
                // looked at again once it has gone.)
                kept.pictureAt = pic.mapToItem(null, 4, pic.height / 2)
                kept.picture = String(testInput.windowPixel(kept.pictureAt.x, kept.pictureAt.y))
                check("and the pointer has a plus: letting go would put the chord on", testInput.pointerShape() === Qt.DragCopyCursor, testInput.pointerShape())
                testInput.grab("1-carried-over-a-word")
                // With Shift, the start of the word
                const to = over(kept.g, 23)
                testInput.mouse(1, to.x + 1, to.y, Qt.ShiftModifier)
                check("with Shift held it is the start of the word", sheet().spotRow === kept.g && sheet().spotAt === 21, sheet().spotAt)
                testInput.mouse(1, to.x, to.y)
                testInput.mouse(2, to.x, to.y)
                return 500
            },
            () => {
                check("let go, the chord is on that place, in the song's file", names(kept.g).split(" ").includes("23:" + kept.four)
                      && chordsOnDisk(glory).split(" ").includes((sheet().rows[kept.g].start + 23) + ":" + kept.four),
                      names(kept.g) + " / " + chordsOnDisk(glory))
                const now = String(testInput.windowPixel(kept.pictureAt.x, kept.pictureAt.y))
                check("nothing is carried any more, and the spot is left there", sheet().dragged === null && !carried().visible && sheet().spotRow === kept.g && sheet().spotAt === 23)
                check("the button's picture was drawn at the pointer, and is gone", apart(kept.picture, now) > 12, kept.picture + " then " + now)
                kept.landed = JSON.stringify(editScreen.editor.song())
                // ---- In place of a chord that is there
                pull(5, over(kept.g, 23))
                return 300
            },
            () => {
                const s = spot()
                const name = Lib.find(s, item => item.text !== undefined && item.text === kept.five)
                check("carried over a chord, the lit bubble says the one that would take its place", s.visible && name !== null, name ? name.text : "none")
                const to = over(kept.g, 23)
                testInput.mouse(2, to.x, to.y, Qt.ControlModifier)
                return 500
            },
            () => {
                const there = names(kept.g).split(" ").filter(c => c.startsWith("23:"))
                check("let go there, the chord that was there gives way to it (and the key that copies does nothing: there is one chord there)", there.length === 1 && there[0] === "23:" + kept.five, names(kept.g))
                editScreen.editor.undo()
                return 500
            },
            () => {
                check("which is one step to undo", JSON.stringify(editScreen.editor.song()) === kept.landed)
                editScreen.editor.undo()
                return 500
            },
            () => {
                check("and so was the first", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash)
                // ---- Let go where there is no place
                const cells = sheet().cells
                // Between the first two slides of the first row
                kept.between = onWindow(cells[0].x + cells[0].width + 6, cells[0].y + cells[0].height / 2)
                pull(4, over(kept.h, 9))
                testInput.mouse(1, kept.between.x, kept.between.y)
                return 300
            },
            () => {
                check("carried on from a word to between two slides, nothing is lit: the last word it was over is not where it would land", sheet().carrying && !sheet().carriedOver && !spot().visible
                      && testInput.pointerShape() === Qt.ClosedHandCursor, sheet().carriedOver + " " + spot().visible)
                testInput.grab("2-carried-between-slides")
                testInput.mouse(2, kept.between.x, kept.between.y)
                return 400
            },
            () => {
                check("let go there, nothing is put on", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash && sheet().dragged === null)
                const cell = sheet().cells[0]
                kept.foot = onWindow(cell.x + cell.width / 2, cell.y + cell.height - 8)
                pull(4, kept.foot)
                return 300
            },
            () => {
                check("on a frame's foot nothing is lit either", sheet().carrying && !sheet().carriedOver && !spot().visible)
                testInput.mouse(2, kept.foot.x, kept.foot.y)
                return 400
            },
            () => {
                check("and let go there, nothing is put on", JSON.stringify(editScreen.editor.song()) === kept.song && sheet().dragged === null)
                // Out over a word and back to the strip
                pull(4, over(kept.h, 9))
                const b = centre(button(4))
                testInput.mouse(1, b.x, b.y)
                testInput.mouse(2, b.x, b.y)
                return 400
            },
            () => {
                check("carried out and back to its own button and let go, nothing is put on: it is not a click either", JSON.stringify(editScreen.editor.song()) === kept.song && sheet().dragged === null)
                // ---- Esc
                pull(4, over(kept.h, 9))
                check("(it is over a word)", sheet().carriedOver && spot().visible)
                testInput.key(Qt.Key_Escape)
                return 300
            },
            () => {
                check("Esc gives it up: the picture and the lit bubble go at once, and the editor is still open", sheet().dragged === null && !carried().visible && editing && editScreen.mode === "chords",
                      JSON.stringify(sheet().dragged))
                const to = over(kept.h, 9)
                testInput.mouse(1, to.x + 2, to.y)
                testInput.mouse(2, to.x + 2, to.y)
                return 400
            },
            () => {
                check("and letting go after that puts nothing on", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash && !sheet().typing)
                // ---- While another chord is being typed
                click(over(kept.p, 0))
                return 400
            },
            () => {
                check("a click on a word opens the bubble a chord is typed in", sheet().typing && sheet().typingRow === kept.p && sheet().typingAt === 0)
                testInput.type("D")
                return 300
            },
            () => {
                const from = centre(button(4))
                testInput.mouse(0, from.x, from.y)
                testInput.mouse(1, from.x + 6, from.y + 6)
                return 400
            },
            () => {
                check("a chord picked up from the strip while another is being typed keeps what was typed, and closes its bubble", !sheet().typing && sheet().carrying
                      && names(kept.p).split(" ").includes("0:D"), names(kept.p))
                const to = over(kept.h, 9)
                testInput.mouse(1, to.x, to.y)
                testInput.mouse(2, to.x, to.y)
                return 500
            },
            () => {
                check("and lands where it is let go", names(kept.h).split(" ").includes("9:" + kept.four) && names(kept.p).split(" ").includes("0:D"), names(kept.h))
                editScreen.editor.undo()
                editScreen.editor.undo()
                return 500
            },
            () => {
                check("(both taken back)", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash)
                // ---- A line of chords alone
                kept.alone = sheet().rows.findIndex(row => row.kind === "line" && row.alone && !row.blank && row.chords.length > 0)
                kept.count = kept.alone >= 0 ? sheet().rows[kept.alone].chords.length : -1
                const item = lineItem(kept.alone)
                view().contentY = Math.max(0, Math.min(item.y - 60, view().contentHeight - view().height))
                return 400
            },
            () => {
                // Past its last chord
                const item = lineItem(kept.alone)
                kept.past = item.mapToItem(null, item.xOf(kept.count * 2) + 6, item.wordsTop + 4)
                pull(5, kept.past)
                return 300
            },
            () => {
                check("over the end of a line of chords alone, the spot is one past its last chord", kept.alone >= 0 && sheet().carriedOver && sheet().spotRow === kept.alone && sheet().spotAt === kept.count * 2,
                      kept.alone + ": " + sheet().spotRow + ":" + sheet().spotAt + " of " + kept.count)
                testInput.mouse(2, kept.past.x, kept.past.y)
                return 500
            },
            () => {
                const chords = sheet().rows[kept.alone].chords
                check("let go there, it is one more chord at the line's end", chords.length === kept.count + 1 && chords[kept.count].name === kept.five, names(kept.alone))
                editScreen.editor.undo()
                return 500
            },
            () => {
                check("(taken back)", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash)
                // ---- The sheet scrolls under a chord held near its bottom, and its top
                for (let i = 0; i < 6; ++i)
                    sheet().zoomBy(1)
                return 700
            },
            () => {
                view().contentY = 0
                return 300
            },
            () => {
                kept.furthest = view().contentHeight - view().height
                const edge = view().mapToItem(null, view().width / 2, view().height - 6)
                kept.low = edge
                pull(4, edge)
                return 900
            },
            () => {
                kept.scrolled = view().contentY
                check("held near the bottom of what is seen, the sheet scrolls down", kept.furthest > 200 && kept.scrolled > 100 && sheet().carrying, kept.scrolled + " of " + kept.furthest)
                const middle = view().mapToItem(null, view().width / 2, view().height / 2)
                testInput.mouse(1, middle.x, middle.y)
                return 100
            },
            () => {
                kept.stopped = view().contentY
                return 500
            },
            () => {
                check("and stops when it is carried away from the edge", view().contentY === kept.stopped, kept.stopped + " then " + view().contentY)
                const top = view().mapToItem(null, view().width / 2, 6)
                testInput.mouse(1, top.x, top.y)
                return 700
            },
            () => {
                check("held near the top, it scrolls up again", view().contentY < kept.stopped - 80, kept.stopped + " then " + view().contentY)
                testInput.key(Qt.Key_Escape)
                const top = view().mapToItem(null, view().width / 2, 6)
                testInput.mouse(2, top.x, top.y)
                kept.down = Math.min(300, kept.furthest)
                view().contentY = kept.down
                return 300
            },
            () => {
                // A chord just picked up, and held just under the strip: it has not been clear of the top yet.
                const top = view().mapToItem(null, view().width / 2, 10)
                pull(4, top)
                return 700
            },
            () => {
                check("a chord on its way in over the top does not scroll the sheet upwards", view().contentY === kept.down && kept.down > 100 && sheet().carrying, view().contentY)
                const top = view().mapToItem(null, view().width / 2, 10)
                testInput.mouse(2, top.x, top.y)
                return 400
            },
            () => {
                check("and nothing was changed by any of that", JSON.stringify(editScreen.editor.song()) === kept.song && testInput.fileHash(kept.path) === kept.hash && sheet().dragged === null)
                stopEditing()
            }
        ]
        next()
    }
}
