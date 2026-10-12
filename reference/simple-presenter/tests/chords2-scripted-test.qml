import QtQuick
import SimplePresenterApp
import "lib.js" as Lib

// The chord editors, as they came back from the editor that was made of them for the Mac (chords.qml has what they did
// before that, which they still do). The sheet: its slides as a grid, each in a frame of its group's colour with a foot,
// the one picked ringed; the size of the words, by the keys, the two buttons and the wheel; Down and Up by the screen; a
// line of chords alone that loses its last chord and takes chords again, by itself and under a line of words; what a
// paste says, and that it undoes as one whichever of its lines was the first to change; a click on a line and then
// Ctrl+V; a name that is no chord, moved like any other; and the empty lines a text box ends with. What is looked at is
// what is drawn, wherever the thing is one that is drawn.
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

    // The frame of a slide, by the slide's row in the editor, and the cell it is drawn by
    function frameOf(row) {
        return named("chordSlide:" + row)
    }

    function cellOf(row) {
        return sheet().cells.find(cell => cell.row === row)
    }

    // The first line of a slide, as a row of the sheet
    function firstLine(row) {
        return sheet().rows.findIndex(line => line.kind === "line" && line.row === row)
    }

    // A text box as the file has it now, found by its slide and its id: its words and its chords.
    function boxOnDisk(path, row, element) {
        const found = catalog.open(path).slides[row].elements.find(e => e.id === element)
        return found ? { text: testInput.plain(found.text), chords: (found.chords ?? []).map(c => c.at + ":" + c.name).join(" "), runs: testInput.runsOf(found.text) }
                     : { text: "(no such box)", chords: "", runs: [] }
    }

    // What a string is made of, for saying so where it cannot be seen.
    function units(text) {
        return "[" + Array.from(text).map(c => c.charCodeAt(0).toString(16)).join(" ") + "]"
    }

    // Whether a colour of the window ("#rrggbb") is the orange the editor marks things with, or a good part of it.
    function orange(c) {
        const r = parseInt(c.substring(1, 3), 16)
        const g = parseInt(c.substring(3, 5), 16)
        const b = parseInt(c.substring(5, 7), 16)
        return r > 130 && r > g + 30 && g > b + 20
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

    // The slide at the top of what is seen of the sheet: the first of the row of slides that is there
    function atTop() {
        const cell = sheet().cells.find(cell => cell.y + cell.height > view().contentY + 1)
        return cell ? cell.row : -1
    }

    // How far down what is seen a slide's frame starts
    function down(row) {
        return Math.round(cellOf(row).y - view().contentY)
    }

    function undoTo(song, most) {
        let undone = 0
        while (JSON.stringify(editScreen.editor.song()) !== song && editScreen.editor.canUndo && undone < most) {
            editScreen.editor.undo()
            ++undone
        }
        return undone
    }

    function run() {
        const herald = "Hark the herald angels sing"
        const glory = "Glory to the newborn King"
        // What a chord with no words hangs on, and what is between two of them. (Made from their numbers: neither can
        // be seen in a file.)
        const zeroWidth = String.fromCharCode(0x200b)
        const emQuad = String.fromCharCode(0x2001)
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
                // ---- The grid, as it is drawn
                const s = sheet()
                const cells = s.cells
                kept.song = JSON.stringify(editScreen.editor.song())
                kept.hash = testInput.fileHash(kept.path)
                check("the sheet is at its usual size, eighty in the hundred", s.zoom === 0.8 && s.times === 0.8 && chordSheetZoom === 0.8, s.zoom + " " + s.times)
                const fits = Math.floor((view().width - 12) / (cells[0].width + 12))
                check("the slides go across the sheet in the order of the song, as many to a row as fit, and then the next row under", s.columns === fits && s.columns >= 2
                      && cells.every((cell, index) => cell.gridRow === Math.floor(index / s.columns) && cell.column === index % s.columns && cell.row === index)
                      && cells[1].x === cells[0].x + cells[0].width + 12 && cells[s.columns].x === cells[0].x && cells[s.columns].y === cells[0].y + cells[0].height + 12,
                      s.columns + " to a row where " + fits + " fit; " + cells.length + " slides")
                // A card is its frame less the frame's edge at each side and its top, and less the foot.
                const card = { width: cells[0].width - 8, height: cells[0].cardHeight }
                const shaped = Math.min(Math.round(card.width * 9 / 16), 4 * s.lineHeight + 2 * s.cardPad)
                check("every card is one width, and the shape of a screen (or four lines tall, if that is less)", cells.every(cell => cell.width === cells[0].width)
                      && cells.every(cell => cell.cardHeight === shaped) && cells.every(cell => cell.height === cell.cardHeight + 26),
                      card.width + " by " + card.height + ", where a screen's shape is " + shaped)
                // A slide's lines stand in the middle of its card, up and down.
                const oneLine = cells.find(cell => cell.count === 1)
                const item = lineItem(oneLine.first)
                const above = item.chordTop
                const below = item.height - item.chordTop - s.lineHeight
                check("a slide's lines stand in the middle of its card, with as much room over the first as under the last", Math.abs(above - below) <= 4 && above > 20,
                      above + " over, " + below + " under")
                // The frames: each in its group's colour, with the card laid on it and its foot under the card.
                const verse = cells.find(cell => cell.group === "Verse 1")
                const wanted = String(win.groupColor({ group: verse.group, groupColor: verse.groupColor }))
                const frame = frameOf(verse.row)
                const drawn = [pixel(frame, 1.5, frame.height / 2), pixel(frame, frame.width / 2, 1.5), pixel(frame, frame.width - 6, frame.height - 5),
                               pixel(frame, 8, 8), pixel(frame, -3, frame.height / 2)]
                check("a slide is in a frame of its group's colour: at its sides, its top and its foot", apart(drawn[0], wanted) < 12 && apart(drawn[1], wanted) < 12
                      && apart(drawn[2], wanted) < 12, drawn.slice(0, 3).join(" ") + " for " + wanted)
                check("with its card laid on the frame, and bare sheet round it", apart(drawn[3], "#222428") < 6 && apart(drawn[4], "#1b1c1f") < 6, drawn[3] + " and " + drawn[4])
                const foot = Lib.find(frame, child => child.text === verse.label)
                check("on the foot are the slide's number and, on the first slide of a group, the group's name: drawn", verse.label === (verse.row + 1) + "  Verse 1"
                      && foot !== null && strokes(foot).light >= 2 && cellOf(verse.row + 1).label === String(verse.row + 2), verse.label + " / " + cellOf(verse.row + 1).label)
                const intro = cells.find(cell => cell.group === "Intro")
                const introFoot = Lib.find(frameOf(intro.row), child => child.text === intro.label)
                check("in black where the frame's colour is a light one", frameOf(intro.row).light === true && String(introFoot.color) === "#000000"
                      && String(foot.color) === "#ffffff", String(introFoot.color) + " " + String(foot.color))
                check("the group's name is nowhere else: its row of the sheet is given no place", s.rows.filter(row => row.kind === "group").length >= 4
                      && Lib.find(s, child => child.isLine === false && child.visible === true) === null)
                // The slide that is picked has an orange ring outside its frame.
                const picked = frameOf(editScreen.canvas.row)
                const other = frameOf(editScreen.canvas.row === 3 ? 4 : 3)
                check("the slide that is picked has an orange ring, outside its frame and set off from it", orange(pixel(picked, -4, picked.height / 2))
                      && !orange(pixel(other, -4, other.height / 2)) && !orange(pixel(picked, -1.5, picked.height / 2)),
                      pixel(picked, -4, picked.height / 2) + " then " + pixel(picked, -1.5, picked.height / 2) + "; another's " + pixel(other, -4, other.height / 2))
                testInput.grab("1-the-grid")
                // ---- A paste with nothing copied says so
                s.setSpot(lineOf(herald), 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                check("a paste with nothing copied yet says so, and how to copy", editScreen.notice === "Nothing has been copied yet. Ctrl+C copies the chords of the line the spot is on."
                      && !editScreen.noticeIsError, editScreen.notice)
                // ---- The size of the words
                view().contentY = cells[s.columns * 2].y - 6
                return 300
            },
            () => {
                kept.top = atTop()
                kept.width = sheet().cells[0].width
                kept.columns = sheet().columns
                kept.lineHeight = sheet().lineHeight
                kept.down = down(kept.top)
                check("(the sheet is scrolled down, to its third row of slides)", kept.top === sheet().columns * 2 && kept.down === 6, kept.top + ", " + kept.down + " down")
                testInput.key(Qt.Key_Minus, Qt.ControlModifier, "-")
                return 400
            },
            () => {
                const s = sheet()
                const words = Lib.find(lineItem(firstLine(1)), child => child.text === herald)
                check("Ctrl with minus makes the words a step smaller: seventy in the hundred, the app's to remember", s.zoom === 0.7 && s.times === 0.7 && chordSheetZoom === 0.7
                      && words.font.pixelSize === 15 && s.lineHeight < kept.lineHeight, s.zoom + ", words at " + words.font.pixelSize + ", a line " + s.lineHeight + " tall")
                check("the cards are smaller with them, and as many more go to a row as then fit", s.cells[0].width < kept.width
                      && s.columns === Math.floor((view().width - 12) / (s.cells[0].width + 12)) && s.columns >= kept.columns, s.cells[0].width + " wide, " + s.columns + " to a row")
                check("what does not zoom has not: a frame's edge and its foot", s.cells[0].height - s.cells[0].cardHeight === 26 && s.cells[1].x - s.cells[0].x - s.cells[0].width === 12)
                check("and the slide that was at the top of what is seen is still there, as far down as it was", down(kept.top) === kept.down,
                      down(kept.top) + " down, it was " + kept.down)
                testInput.grab("2-smaller")
                // (The button is the round thing that has the + for its text.)
                click(centre(Lib.find(named("chordSheetZoom"), child => child.text === "+" && child.radius > 0)))
                return 400
            },
            () => {
                check("the + in the strip makes them a step larger again", sheet().zoom === 0.8 && sheet().cells[0].width === kept.width && sheet().columns === kept.columns, sheet().zoom)
                // (Which slide is at the top is asked afresh: at the smaller size it shared its row with others, and it
                // is the first of a row that is kept where it was.)
                kept.top = atTop()
                kept.down = down(kept.top)
                const middle = view().mapToItem(null, view().width / 2, view().height / 2)
                kept.scrolled = view().contentY
                testInput.wheel(middle.x, middle.y, 120, Qt.ControlModifier)
                return 400
            },
            () => {
                check("Ctrl with the wheel turned away makes them larger, a step for a notch, and scrolls nothing", sheet().zoom === 0.9 && down(kept.top) === kept.down,
                      sheet().zoom + ", " + down(kept.top) + " down")
                const middle = view().mapToItem(null, view().width / 2, view().height / 2)
                kept.scrolled = view().contentY
                testInput.wheel(middle.x, middle.y, -120)
                return 400
            },
            () => {
                check("the wheel alone scrolls the sheet, as it did, and changes no size", sheet().zoom === 0.9 && view().contentY > kept.scrolled, view().contentY + " from " + kept.scrolled)
                testInput.key(Qt.Key_Equal, Qt.ControlModifier, "=")
                check("Ctrl with the equals key, which is plus without Shift, is a step larger too", sheet().zoom === 1, sheet().zoom)
                testInput.key(Qt.Key_0, Qt.ControlModifier, "0")
                check("and Ctrl+0 is the usual size", sheet().zoom === 0.8 && chordSheetZoom === 0.8, sheet().zoom)
                chordSheetZoom = 0.5
                return 400
            },
            () => {
                const buttons = named("chordSheetZoom")
                check("at the smallest size the − is greyed, and Ctrl with minus does nothing", sheet().zoom === 0.5 && buttons.canShrink === false && buttons.canGrow === true
                      && (testInput.key(Qt.Key_Minus, Qt.ControlModifier, "-"), sheet().zoom === 0.5), sheet().zoom)
                testInput.grab("3-smallest")
                chordSheetZoom = 1.5
                return 400
            },
            () => {
                check("and at the largest the +", sheet().zoom === 1.5 && named("chordSheetZoom").canGrow === false && (testInput.key(Qt.Key_Plus, Qt.ControlModifier, "+"), sheet().zoom === 1.5))
                testInput.grab("4-largest")
                chordSheetZoom = 0.8
                view().contentY = sheet().cells[sheet().columns].y - 6
                return 400
            },
            () => {
                // ---- The window made narrower
                kept.top = atTop()
                kept.down = down(kept.top)
                kept.windowWidth = win.width
                win.width = Math.max(win.minimumWidth, win.width - 330)
                return 600
            },
            () => {
                const s = sheet()
                check("the window made narrower, the slides are laid out again, fewer to a row", s.columns === kept.columns - 1
                      && s.columns === Math.max(1, Math.floor((view().width - 12) / (s.cells[0].width + 12))), s.columns + " to a row in " + view().width)
                check("with the slide that was at the top still there", down(kept.top) === kept.down, down(kept.top) + " down, it was " + kept.down)
                testInput.grab("5-narrower")
                win.width = kept.windowWidth
                return 600
            },
            () => {
                const s = sheet()
                check("(and as wide as it was, as many to a row as before)", s.columns === kept.columns, s.columns)
                view().contentY = 0
                // ---- Down and Up, by the screen
                // From the last line of the second slide of the first row: the slide under it, not the next of the song.
                const from = s.cells[1]
                const under = s.cells[1 + s.columns]
                s.setSpot(from.first + from.count - 1, 0)
                testInput.key(Qt.Key_Down)
                check("Down from a slide's last line goes to the first line of the slide under it in the grid, not to the next slide of the song",
                      s.spotRow === under.first && s.rows[s.spotRow].row === under.row && under.row !== from.row + 1, "to slide " + s.rows[s.spotRow].row + " from " + from.row)
                check("and that slide is then the one picked", editScreen.canvas.row === under.row, editScreen.canvas.row)
                testInput.key(Qt.Key_Up)
                check("Up from there goes back to the last line of the slide over it", s.spotRow === from.first + from.count - 1, s.spotRow)
                s.setSpot(from.first, 0)
                testInput.key(Qt.Key_Up)
                check("from the first row of slides Up goes nowhere", s.spotRow === from.first, s.spotRow)
                // (Its last word, and then on: the next slide of the song is the one beside it.)
                const last = s.stops(from.first + from.count - 1)
                s.setSpot(from.first + from.count - 1, last[last.length - 1])
                testInput.key(Qt.Key_Right)
                check("Right from a slide's last word goes to the next slide of the song, which is beside it", s.rows[s.spotRow].row === from.row + 1 && s.spotAt === 0,
                      "slide " + s.rows[s.spotRow].row + " at " + s.spotAt)
                // Under a short last row, the row's last slide.
                const lastRow = s.cells[s.cells.length - 1].gridRow
                const inLast = s.cells.filter(cell => cell.gridRow === lastRow)
                const beyond = s.cells.find(cell => cell.gridRow === lastRow - 1 && cell.column === s.columns - 1)
                kept.shortRow = inLast.length < s.columns
                s.setSpot(beyond.first + beyond.count - 1, 0)
                testInput.key(Qt.Key_Down)
                check("under a slide that has none below it in a short last row, Down goes to that row's last slide (if the song has such a row)", !kept.shortRow
                      || s.rows[s.spotRow].row === inLast[inLast.length - 1].row, inLast.length + " in the last row; to slide " + s.rows[s.spotRow].row)
                view().contentY = 0
                // ---- A line of chords alone that loses its last chord
                kept.intro = s.rows.findIndex(row => row.kind === "line" && row.alone && row.chords.length >= 3)
                kept.introRow = s.rows[kept.intro].row
                kept.introElement = s.rows[kept.intro].element
                kept.introHad = s.rows[kept.intro].chords.length
                kept.introBox = boxOnDisk(kept.path, kept.introRow, kept.introElement)
                check("(the intro's line of chords, which hangs them on plain spaces or on zero-width ones)", kept.intro >= 0 && kept.introHad >= 3
                      && Chords.isPlaceholders(s.rows[kept.intro].text), names(kept.intro) + " over " + units(s.rows[kept.intro].text))
                for (let i = 0; i < kept.introHad; ++i) {
                    s.setSpot(kept.intro, 0)
                    testInput.key(Qt.Key_Delete)
                }
                return 500
            },
            () => {
                const s = sheet()
                const row = s.rows[kept.intro]
                const box = boxOnDisk(kept.path, kept.introRow, kept.introElement)
                check("with every chord taken off it, a line of chords alone is a line of chords alone still: it is left one zero-width space, whatever it hung them on",
                      row.alone && !row.blank && row.chords.length === 0 && row.text === zeroWidth && box.text === zeroWidth && box.chords === "",
                      "the row over " + units(row.text) + ", the file over " + units(box.text) + " with " + box.chords)
                check("it takes the spot and has its one stop", s.usable(kept.intro) && s.stops(kept.intro).join(" ") === "0" && lineItem(kept.intro).visible)
                const p = over(kept.intro, 0)
                testInput.mouse(1, p.x + 2, p.y)
                return 300
            },
            () => {
                const s = sheet()
                check("the spot goes to it with the pointer, drawn", s.spotRow === kept.intro && s.spotAt === 0 && named("chordSpot").visible, s.spotRow + ":" + s.spotAt)
                testInput.grab("6-emptied-line")
                kept.emptied = boxOnDisk(kept.path, kept.introRow, kept.introElement)
                testInput.key(Qt.Key_1, 0, "1")
                return 400
            },
            () => {
                const s = sheet()
                const box = boxOnDisk(kept.path, kept.introRow, kept.introElement)
                check("and a chord is put on it again like its first", names(kept.intro) === "0:E" && box.chords === "0:E" && box.text === zeroWidth, names(kept.intro) + " over " + units(box.text))
                check("for which the words are not written again: they are as they were, format and all", JSON.stringify(box.runs) === JSON.stringify(kept.emptied.runs))
                const undone = undoTo(kept.song, 20)
                check("all of it undone, the song and its file are what they were", JSON.stringify(editScreen.editor.song()) === kept.song
                      && testInput.fileHash(kept.path) === kept.hash && undone === kept.introHad + 1, undone + " steps")
                // A chord renamed on such a line changes the chords and not a character of the words.
                s.setSpot(kept.intro, 0)
                testInput.key(Qt.Key_5, 0, "5")
                return 400
            },
            () => {
                const box = boxOnDisk(kept.path, kept.introRow, kept.introElement)
                check("a chord renamed on a line of chords alone changes its name and nothing of the words or how they are set", names(kept.intro).startsWith("0:B ")
                      && box.text === kept.introBox.text && JSON.stringify(box.runs) === JSON.stringify(kept.introBox.runs), names(kept.intro))
                check("(undone)", editScreen.editor.undo() === "" && testInput.fileHash(kept.path) === kept.hash)
                // ---- A line of chords alone under a line of words, in one text box. There is none in the song, so one is
                // made as it would be by hand: a second line typed into a slide's words, with nothing in it but a space.
                kept.gloryRow = sheet().rows[sheet().rows.findIndex(row => row.kind === "line" && row.text === glory && row.first && row.last)].row
                kept.gloryElement = editScreen.editor.song().find(block => block.row === kept.gloryRow).element
                click(centre(named("slidesMode")))
                return 500
            },
            () => {
                editScreen.showRow(kept.gloryRow)
                editScreen.canvas.editText(kept.gloryElement)
                testInput.key(Qt.Key_End, Qt.ControlModifier)
                testInput.key(Qt.Key_Return)
                testInput.type(" ")
                editScreen.canvas.finishText()
                return 400
            },
            () => {
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                check("(a slide's words are given a second line with one space on it)", box.text === glory + "\n ", units(box.text.substring(glory.length)))
                kept.under = JSON.stringify(editScreen.editor.song())
                kept.underRuns = box.runs
                click(centre(named("chordsMode")))
                return 800
            },
            () => {
                const s = sheet()
                // (Not the first line of the song that has those words: they are the second line of the slide before, too.)
                kept.first = firstLine(kept.gloryRow)
                kept.second = kept.first + 1
                const row = s.rows[kept.second]
                check("on the sheet that line is a line of chords alone, under the words, with no chord yet", row.row === kept.gloryRow && row.alone && !row.blank && row.text === " "
                      && row.line === 1 && s.usable(kept.second) && s.cells.find(cell => cell.row === kept.gloryRow).count === 2, JSON.stringify(row))
                kept.firstNames = names(kept.first)
                s.setSpot(kept.second, 0)
                testInput.key(Qt.Key_1, 0, "1")
                s.setSpot(kept.second, 2)
                testInput.key(Qt.Key_4, 0, "4")
                s.setSpot(kept.second, 4)
                testInput.key(Qt.Key_5, 0, "5")
                return 500
            },
            () => {
                const s = sheet()
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                const start = glory.length + 1
                check("three chords put on it hang on three plain spaces with the wide space between, the stand-in the line had",
                      names(kept.second) === "0:E 2:A 4:B" && box.text === glory + "\n " + emQuad + " " + emQuad + " ", names(kept.second) + " over " + units(box.text.substring(start)))
                check("the words over it are as they were, with their own chords on their own letters", names(kept.first) === kept.firstNames
                      && box.chords === kept.firstNames.split(" ").concat([start + ":E", (start + 2) + ":A", (start + 4) + ":B"]).join(" "), box.chords)
                const wordsBefore = kept.underRuns.filter(run => run.text.includes("Glory"))
                const wordsNow = box.runs.filter(run => run.text.includes("Glory"))
                const sameFormat = (a, b) => a.fontName === b.fontName && a.size === b.size && a.color === b.color && a.bold === b.bold && a.capitalization === b.capitalization
                check("and set as they were: their font, size and colour", wordsNow.length === 1 && wordsBefore.length === 1 && sameFormat(wordsNow[0], wordsBefore[0]),
                      JSON.stringify(wordsNow.map(run => [run.fontName, run.size, run.color])))
                // (The log has a line whenever a text box's words had to be written again whole instead.)
                check("and the line was changed where it stands in the text box's RTF: the text box was not written again whole",
                      !testInput.readText(Log.path).includes("could not be changed where it stands"))
                testInput.grab("7-chords-under-words")
                // One from the middle, then the rest.
                s.setSpot(kept.second, 2)
                testInput.key(Qt.Key_Delete)
                return 400
            },
            () => {
                const s = sheet()
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                check("one taken from the middle, the others close up: two stand-ins for two chords", names(kept.second) === "0:E 2:B"
                      && box.text === glory + "\n " + emQuad + " ", names(kept.second) + " over " + units(box.text.substring(glory.length + 1)))
                s.setSpot(kept.second, 0)
                testInput.key(Qt.Key_Delete)
                s.setSpot(kept.second, 0)
                testInput.key(Qt.Key_Delete)
                return 400
            },
            () => {
                const s = sheet()
                const row = s.rows[kept.second]
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                check("with the last of them off, the line under the words is still there to be given chords: one zero-width space, and not gone for good",
                      row !== undefined && row.row === kept.gloryRow && row.alone && row.text === zeroWidth && row.chords.length === 0 && s.usable(kept.second)
                      && box.text === glory + "\n" + zeroWidth && box.chords === kept.firstNames, row ? units(row.text) + " / " + box.chords : "the row has gone")
                s.setSpot(kept.second, 0)
                testInput.key(Qt.Key_6, 0, "6")
                return 400
            },
            () => {
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                check("a chord put back there hangs on that zero-width space", names(kept.second) === "0:C#m" && box.text === glory + "\n" + zeroWidth
                      && box.chords === kept.firstNames + " " + (glory.length + 1) + ":C#m", box.chords)
                // A line that has words, and an empty one, are never made stand-ins, whatever asks.
                const editor = editScreen.editor
                const refused = [editor.setChordsAlone(kept.gloryRow, kept.gloryElement, 0, ["G"]), editor.setChordsAlone(kept.gloryRow, kept.gloryElement, 7, ["G"])]
                check("a line that has words is never made stand-ins, whatever asks: it is refused, and nothing changes", refused[0] === "That is not a line of chords with no words."
                      && refused[1] !== "" && boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement).text === glory + "\n" + zeroWidth, refused.join(" | "))
                const undone = undoTo(kept.under, 20)
                check("(the chords put on that line are undone: the slide keeps its second line for what follows)", JSON.stringify(editor.song()) === kept.under, undone + " steps")
                return 300
            },
            () => {
                // ---- What a paste says, and what it undoes
                const s = sheet()
                kept.heraldLine = lineOf(herald)
                let p = over(kept.heraldLine, 0)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                check("a copy says what was copied", editScreen.notice === "2 chords copied from the line: Ctrl+V puts them on the lines from the spot's on, word for word", editScreen.notice)
                p = over(kept.first, 0)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                // "Hark the herald angels sing" has its chords on its first and fourth words; "Glory to the newborn King" has five.
                check("a paste says what came of it", editScreen.notice === "2 chords put on the line." && !editScreen.noticeIsError, editScreen.notice)
                check("(its chords are on the line pasted to, word for word)", names(kept.first) === "1:E 13:Asus2", names(kept.first))
                const s = sheet()
                s.setSpot(kept.second, 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                check("onto a line of chords alone, chords copied from words are not put, and it says why", names(kept.second) === ""
                      && editScreen.notice === "Nothing was put here: chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none.",
                      editScreen.notice)
                // A line whose chords are on its fourth and fifth words, onto a line of three words.
                s.copied = { lines: [[{ word: 0, offset: 0, name: "G" }, { word: 3, offset: 0, name: "D" }, { word: 4, offset: 0, name: "Em" }]] }
                kept.three = s.rows.findIndex(row => row.kind === "line" && !row.alone && s.wordStarts(row.text).length === 3)
                kept.threeNames = names(kept.three)
                s.setSpot(kept.three, 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                const s = sheet()
                check("chords for words the line has not got are left out, and the paste says how many", names(kept.three) === "0:G"
                      && editScreen.notice === "1 chord put on the line. 2 had no word to go on, and were left out.", names(kept.three) + " / " + editScreen.notice)
                check("(undone)", editScreen.editor.undo() === "" && names(kept.three) === kept.threeNames)
                s.copied = { lines: [[{ word: 7, offset: 0, name: "G" }]] }
                s.setSpot(kept.three, 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                const s = sheet()
                check("where none of them has a word to go on, the line's own chords go and the paste says so", names(kept.three) === ""
                      && editScreen.notice === "None of the copied chords had a word to go on here.", names(kept.three) + " / " + editScreen.notice)
                check("(undone)", editScreen.editor.undo() === "" && names(kept.three) === kept.threeNames)
                // A whole group whose first line is a line of chords alone, pasted onto lines of words: the first line copied
                // is passed over. The change the paste then makes is the paste's own to undo, and not joined to whatever
                // was done before it.
                s.setSpot(kept.heraldLine, 9)
                testInput.key(Qt.Key_4, 0, "4")
                return 400
            },
            () => {
                const s = sheet()
                check("(a chord is put on a word, to be the thing done before a paste)", names(kept.heraldLine) === "1:E 9:A 16:Asus2", names(kept.heraldLine))
                kept.before = names(kept.three)
                kept.next = kept.three + 1
                while (!s.usable(kept.next) || s.rows[kept.next].alone)
                    ++kept.next
                kept.nextNames = names(kept.next)
                s.copied = { lines: [{ alone: ["E", "A"] }, [{ word: 0, offset: 0, name: "F#m" }]] }
                s.setSpot(kept.three, 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                check("a copied line of chords alone is passed over on a line of words, the next copied line goes to the next line, and the paste says both",
                      names(kept.three) === kept.before && names(kept.next) === "0:F#m"
                      && editScreen.notice === "1 chord put on the line. 1 line was passed over: chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none.",
                      names(kept.next) + " / " + editScreen.notice)
                check("one undo takes back the paste", editScreen.editor.undo() === "" && names(kept.next) === kept.nextNames, names(kept.next))
                check("and not the thing done before it, which the paste was no part of", names(kept.heraldLine) === "1:E 9:A 16:Asus2", names(kept.heraldLine))
                check("(which a second undo takes back)", editScreen.editor.undo() === "" && names(kept.heraldLine) === "1:E 16:Asus2", names(kept.heraldLine))
                // ---- A click on a line, and then Ctrl+V
                const s = sheet()
                const p = over(kept.heraldLine, 0)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                kept.fourth = lineOf("Peace on earth and mercy mild")
                kept.fourthNames = names(kept.fourth)
                click(over(kept.fourth, 9))
                return 400
            },
            () => {
                const s = sheet()
                check("(a line's chords are copied, and a click on another line has opened the bubble there)", s.typing && s.typingRow === kept.fourth
                      && named("chordField").activeFocus && named("chordField").text === "", s.typingRow + " '" + named("chordField").text + "'")
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                const s = sheet()
                check("Ctrl+V then closes the bubble, nothing having been typed in it, and pastes the copied chords on that line", !s.typing
                      && names(kept.fourth) === "1:E 15:Asus2" && editScreen.notice === "2 chords put on the line.", names(kept.fourth) + " / " + editScreen.notice)
                check("(undone)", editScreen.editor.undo() === "" && names(kept.fourth) === kept.fourthNames, names(kept.fourth))
                // With something typed, the key is the box's own.
                click(over(kept.fourth, 9))
                return 400
            },
            () => {
                testInput.type("g")
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 300
            },
            () => {
                const s = sheet()
                check("with anything typed in the bubble the key is the box's own, and no chords are pasted", s.typing && names(kept.fourth) === kept.fourthNames
                      && named("chordField").text.startsWith("G"), named("chordField").text + " / " + names(kept.fourth))
                // What is typed is copied as text, by the box: something has then been copied since the chords were.
                // (All in one go, with no wait between: the desktop the tests are drawn on takes back what was copied a
                // moment after, the keys that copied it not having come from a keyboard it knows of.)
                named("chordField").selectAll()
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                testInput.key(Qt.Key_Escape)
                check("(the bubble's own text is copied, as any box of text copies, and the typing thrown away)", !s.typing && Chords.copiedText().startsWith("G")
                      && !s.copiedLast(), Chords.copiedText())
                click(over(kept.fourth, 9))
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                check("once something has been copied elsewhere since, Ctrl+V in a fresh bubble is the box's own again: it pastes that text, not the chords",
                      s.typing && named("chordField").text.startsWith("G") && names(kept.fourth) === kept.fourthNames, named("chordField").text + " / " + names(kept.fourth))
                testInput.key(Qt.Key_Escape)
                // Ctrl+C in an empty bubble copies the line's chords, and the bubble stays.
                click(over(kept.heraldLine, 9))
                return 400
            },
            () => {
                const s = sheet()
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                check("Ctrl+C in a bubble that is empty copies the chords of the bubble's line, and the bubble stays open", s.typing && s.typingRow === kept.heraldLine
                      && s.copied !== null && s.copied.lines.length === 1 && s.copied.lines[0].length === 2 && s.copiedLast()
                      && editScreen.notice.startsWith("2 chords copied from the line"), editScreen.notice)
                testInput.key(Qt.Key_C, Qt.ControlModifier | Qt.ShiftModifier)
                check("and with Shift those of its whole group", s.typing && s.copied.lines.length >= 3 && editScreen.notice.includes("copied from " + s.copied.lines.length + " lines"),
                      editScreen.notice)
                // The keys that zoom are the sheet's whichever has the keyboard.
                testInput.key(Qt.Key_Minus, Qt.ControlModifier, "-")
                check("the keys that zoom are the sheet's while the bubble has the keyboard too", s.typing && s.zoom === 0.7 && named("chordField").text === "", s.zoom)
                testInput.key(Qt.Key_0, Qt.ControlModifier, "0")
                testInput.key(Qt.Key_Escape)
                check("(the usual size again, and the bubble closed)", s.zoom === 0.8 && !s.typing)
                // ---- A name that is no chord
                // Some files have names that are not chords. One is put in the file as a file might have it.
                const block = editScreen.editor.song().find(b => b.row === s.rows[kept.heraldLine].row)
                const put = editScreen.editor.setChords(block.row, block.element, block.chords.concat([{ at: 9, name: "N.C." }]))
                check("(a name that is no chord is put in the file, as some files have one)", put === "" && !Chords.isChord("N.C."), put)
                return 400
            },
            () => {
                check("a name that is no chord is shown on the sheet as it is", names(kept.heraldLine) === "1:E 9:N.C. 16:Asus2", names(kept.heraldLine))
                // Dragged to the fifth word, "sing".
                const bubble = named("chord:" + kept.heraldLine + ":9")
                const from = bubble.mapToItem(null, bubble.width / 2, 6)
                const to = over(kept.heraldLine, 23)
                testInput.mouse(0, from.x, from.y)
                for (let i = 1; i <= 10; ++i)
                    testInput.mouse(1, from.x + (to.x - from.x) * i / 10, from.y + (to.y - from.y) * i / 10)
                testInput.mouse(2, to.x, to.y)
                return 400
            },
            () => {
                const s = sheet()
                check("and can be dragged to another word like any other: a name the song has already is put as it is", names(kept.heraldLine) === "1:E 16:Asus2 23:N.C.",
                      names(kept.heraldLine))
                // Onto another line, which is put there and taken from here.
                s.move(kept.heraldLine, 23, kept.fourth, 9, false)
                return 400
            },
            () => {
                const s = sheet()
                check("to another line too", names(kept.heraldLine) === "1:E 16:Asus2" && names(kept.fourth).includes("9:N.C."), names(kept.fourth))
                s.setSpot(kept.fourth, 9)
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                s.setSpot(kept.heraldLine, 0)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                const s = sheet()
                check("and is copied and pasted with its line's chords", names(kept.heraldLine).includes("N.C."), names(kept.heraldLine))
                // What is typed is still held to being a chord.
                s.setSpot(kept.heraldLine, 0)
                check("a name newly typed must still be a chord", s.put(kept.heraldLine, 0, "N.C.") === false)
                // ---- The empty lines a text box's words end with, and a character of two halves
                check("the empty lines a text box's words end with get no row: only what is drawn, the words being left as they are",
                      s.shownLines({ text: "One\nTwo\n\n\n", chords: [] }).join("|") === "One|Two" && s.shownLines({ text: "One\n\nTwo", chords: [] }).join("|") === "One||Two"
                      && s.shownLines({ text: "", chords: [] }).length === 1 && s.shownLines({ text: "\n\n", chords: [] }).length === 1,
                      JSON.stringify(s.shownLines({ text: "One\nTwo\n\n\n", chords: [] })))
                check("but one that a chord stands on is kept, with any before it", s.shownLines({ text: "One\n\n\n", chords: [{ at: 5, name: "G" }] }).length === 3
                      && s.shownLines({ text: "One\n\n\n", chords: [{ at: 4, name: "G" }] }).length === 2
                      && s.shownLines({ text: "One\n\n\n", chords: [{ at: 0, name: "G" }] }).length === 1)
                const pair = "a" + String.fromCodePoint(0x1F3B5) + "b"
                check("the second half of a character that takes two places is no place for the spot", pair.length === 4 && !s.secondOfPair(pair, 1) && s.secondOfPair(pair, 2)
                      && !s.secondOfPair(pair, 3))
                const undone = undoTo(kept.under, 30)
                check("(what was done since is undone)", JSON.stringify(editScreen.editor.song()) === kept.under, undone + " steps")
                click(centre(named("slidesMode")))
                return 500
            },
            () => {
                // The slide whose words were given a second line gets two empty ones more, typed at their end.
                editScreen.showRow(kept.gloryRow)
                editScreen.canvas.editText(kept.gloryElement)
                testInput.key(Qt.Key_End, Qt.ControlModifier)
                testInput.key(Qt.Key_Backspace)
                testInput.key(Qt.Key_Return)
                testInput.key(Qt.Key_Return)
                editScreen.canvas.finishText()
                return 400
            },
            () => {
                const box = boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement)
                check("(a slide's words are made to end with empty lines)", box.text === glory + "\n\n\n" || box.text === glory + "\n\n", units(box.text.substring(glory.length)))
                kept.ended = box.text
                click(centre(named("chordsMode")))
                return 800
            },
            () => {
                const s = sheet()
                const cell = s.cells.find(cell => cell.row === kept.gloryRow)
                check("on the sheet that slide is one line, as it was before it was given them: its card no taller than its neighbours'", cell.count === 1
                      && s.rows.filter(row => row.kind === "line" && row.row === kept.gloryRow).length === 1 && cell.height === s.cells[0].height
                      && boxOnDisk(kept.path, kept.gloryRow, kept.gloryElement).text === kept.ended, cell.count + " lines")
                testInput.grab("8-ends-with-empty-lines")
                let undone = 0
                while (editScreen.editor.canUndo && undone < 80) {
                    editScreen.editor.undo()
                    ++undone
                }
                check("everything undone, the song is as it was found", JSON.stringify(editScreen.editor.song()) === kept.song, undone + " steps")
                check("and in all of this no line of chords alone had its text box written again whole: each was changed where it stands",
                      !testInput.readText(Log.path).includes("could not be changed where it stands"))
                stopEditing()
                return 500
            }
        ]
        next()
    }
}
