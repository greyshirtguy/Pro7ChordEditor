import QtQuick
import SimplePresenterApp
import "lib.js" as Lib

// Chords: a song's chords read from a file Multitracks wrote, shown on the stage in the
// key picked and in each notation; the chord editor (the spot, the keys 1 to 7, typing
// a chord, dragging one, copying a line's, undo); a blank slide given chords in both
// editors, with and without a text box of its own, and left as it was found when they
// are taken off again; the ChordPro editor (only chords can be typed); importing a
// ChordPro file; and a new library. What the file then holds is looked at when the
// test is over, by after_chords in run.py.
QtObject {
    id: t

//COMMON
    function sheet() {
        return named("chordSheet")
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

    function onDisk(path, words) {
        for (const slide of catalog.open(path).slides) {
            for (const element of slide.elements) {
                if (testInput.plain(element.text).split("\n").includes(words))
                    return { text: testInput.plain(element.text), chords: element.chords ?? [] }
            }
        }
        return null
    }

    // A text box as the file has it now, found by its id: its words and its chords.
    function boxOnDisk(path, element) {
        for (const slide of catalog.open(path).slides) {
            const found = slide.elements.find(e => e.id === element)
            if (found)
                return { text: testInput.plain(found.text), chords: (found.chords ?? []).map(c => c.at + ":" + c.name).join(" ") }
        }
        return { text: "(no such box)", chords: "" }
    }

    // What a string is made of, for saying so where it cannot be seen.
    function units(text) {
        return "[" + Array.from(text).map(c => c.charCodeAt(0).toString(16)).join(" ") + "]"
    }

    function lineNames(lines) {
        return lines.map(line => line.text + " <" + line.chords.map(c => c.at + ":" + c.name).join(" ") + ">").join(" | ")
    }

    function run() {
        const herald = "Hark the herald angels sing"
        const peace = "Peace on earth and mercy mild"
        const born = "Born to raise the sons of earth"
        // What a chord with no words hangs on, and what is between two of them. (Made
        // from their numbers: neither can be seen in a file.)
        const zeroWidth = String.fromCharCode(0x200b)
        const emQuad = String.fromCharCode(0x2001)
        steps = [
            () => {
                openLibrary(catalog.libraries[0].path)
                openEntry(entry("Hark2"))
                kept.path = document.path
                check("a song's chords are read, with the key they are written in and the one it was last shown in",
                      document.hasChords === true && document.originalKey === "E" && document.userKey === "C#",
                      document.hasChords + " " + document.originalKey + " " + document.userKey)
                const picker = named("chordKeyPicker")
                check("a song with chords has a key to pick over its slides, at the key the file says", picker.visible && picker.currentText === "C#", picker.currentText)
                openEntry(entry("Abandoned") ?? documents.find(d => openable(d) && d.name !== "Hark2" && d.name !== "Great Are You Lord"))
                check("and a presentation with none has not", document.hasChords === false && !named("chordKeyPicker").visible, document.name)
                openEntry(entry("Hark2"))
                kept.slide = document.slides.findIndex(slide => slide.plainText.startsWith(herald))
                check("found the slide to go by", kept.slide >= 0, kept.slide)
                goLive(kept.slide)
                return 500
            },
            () => {
                const lines = Show.chordLines(false, Show.Words, "", 0, 0)
                check("the live words come line by line with their chords, moved from the song's key to the one picked",
                      lines.length === 2 && lines[0].text === herald && lineNames(lines).includes("<1:C# 16:F#sus2>") && lineNames(lines).includes("<0:G#sus4 13:A#m>"),
                      lineNames(lines))
                setChordKey(document, "E")
                check("picking the song's own key shows them as written", lineNames(Show.chordLines(false, Show.Words, "", 0, 0)).includes("<1:E 16:Asus2>"),
                      lineNames(Show.chordLines(false, Show.Words, "", 0, 0)))
                check("as numbers, numerals and Do Re Mi", lineNames(Show.chordLines(false, Show.Words, "", 0, 1)).includes("<1:1 16:4sus2>")
                      && lineNames(Show.chordLines(false, Show.Words, "", 0, 2)).includes("<1:I 16:IVsus2>")
                      && lineNames(Show.chordLines(false, Show.Words, "", 0, 3)).includes("<1:Mi 16:Lasus2>"),
                      lineNames(Show.chordLines(false, Show.Words, "", 0, 1)) + " // " + lineNames(Show.chordLines(false, Show.Words, "", 0, 3)))
                check("the next slide's too", Show.chordLines(true, Show.Words, "", 0, 0).length > 0)
                check("text transformed on the way has no chords", Show.chordLines(false, Show.Words, "", 1, 0).every(line => line.chords.length === 0))
                check("the file is not touched by picking a key", catalog.open(kept.path).userKey === "C#")
                // A stage layout whose text box shows the live slide's words is set to draw their chords.
                const layout = StageLayouts.layouts.find(l => l.slide.elements.some(e => e.linkKind === "slideText" && !e.linkSlideNext))
                check("a stage layout shows the live slide's words", layout !== undefined, StageLayouts.layouts.map(l => l.name).join("|"))
                kept.layout = layout.id
                kept.element = layout.slide.elements.find(e => e.linkKind === "slideText" && !e.linkSlideNext).id
                startEditingStage(layout.id)
                return 500
            },
            () => {
                check("the stage layout is in the editor, where the chord editors are not offered", editing && editScreen.editor.kind === "stage" && !named("chordsMode").visible)
                editScreen.canvas.pick(kept.element)
                editScreen.inspector.tab = "text"
                return 300
            },
            () => {
                const box = named("chordsCheck")
                check("a text box that shows a slide's words has its chords to switch on", box !== null && box.visible && box.checked === false)
                check(editScreen.editor.setProperties(editScreen.canvas.row, kept.element, { chordsOn: true, chordColor: "#ffcc00", chordNotation: 0 }) === "", true)
                testInput.grab("1-stage-layout-chords")
                stopEditing()
                return 500
            },
            () => {
                const element = StageLayouts.layouts.find(l => l.id === kept.layout).slide.elements.find(e => e.id === kept.element)
                check("the switch, the colour and the notation are in ProPresenter's file of layouts", element.chordsOn === true && String(element.chordColor) === "#ffcc00"
                      && element.chordNotation === 0 && element.chordStyle !== undefined && element.chordStyle.size > 0,
                      element.chordsOn + " " + element.chordColor + " " + JSON.stringify(element.chordStyle ?? null))
                Show.stageLayoutId = kept.layout
                stageEnabled = true
                goLive(kept.slide)
                return 1200
            },
            () => {
                testInput.grabStage("2-stage-chords-E")
                setChordKey(document, "G")
                return 600
            },
            () => {
                testInput.grabStage("3-stage-chords-G")
                check("the stage is told when the key changes", lineNames(Show.chordLines(false, Show.Words, "", 0, 0)).includes("<1:G 16:Csus2>"))
                // Every chord of both songs reaches the stage, as the current slide's and as the next's: none is lost
                // on a slide of chords alone, whose chords hang on spaces in one song and on zero-width spaces in the other.
                for (const name of ["Hark2", "Great Are You Lord"]) {
                    openEntry(entry(name))
                    let inFile = 0, asCurrent = 0, asNext = 0
                    for (let i = 0; i < document.slides.length; ++i) {
                        const own = document.slides[i].elements.reduce((n, e) => n + (e.chords ?? []).length, 0)
                        inFile += own
                        goLive(i)
                        asCurrent += Show.chordLines(false, Show.Words, "", 0, 0).reduce((n, l) => n + l.chords.length, 0)
                        if (i > 0) {
                            goLive(i - 1)
                            asNext += Show.chordLines(true, Show.Words, "", 0, 0).reduce((n, l) => n + l.chords.length, 0)
                        } else {
                            asNext += own
                        }
                    }
                    check("every chord of " + name + " reaches the stage, on whichever slide it is", inFile > 40 && asCurrent === inFile && asNext === inFile,
                          inFile + " in the file, " + asCurrent + " as the current slide, " + asNext + " as the next")
                }
                // Its second slide is six chords and no words, in the key the file says it was last shown in (A to D flat).
                const instrumental = document.slides.findIndex(s => s.plainText.trim() === "" && s.elements.some(e => (e.chords ?? []).length === 6))
                goLive(instrumental)
                const alone = Show.chordLines(false, Show.Words, "", 0, 0)
                check("a slide of chords alone is shown, moved to the key like any other", lineNames(alone).includes("<0:Gb 2:Bbm 4:Ab>") && alone.length === 2
                      && Show.originalKey === "A" && Show.chordKey === "Db", lineNames(alone) + " " + Show.originalKey + ">" + Show.chordKey)
                check("and stays moved from slide to slide and back", (goLive(instrumental + 1), Show.chordLines(false, Show.Words, "", 0, 0).every(l => l.chords.every(c => !c.name.includes("#"))))
                      && Show.chordKey === "Db")
                openEntry(entry("Hark2"))
                goLive(kept.slide)
                check("going back to the first song goes back to the key picked for it", Show.chordKey === "G" && Show.originalKey === "E", Show.originalKey + ">" + Show.chordKey)
                startEditing(entry("Hark2"))
                return 600
            },
            () => {
                check("a presentation in the editor has the three ways of working, the slides first", editing && named("chordsMode").visible && editScreen.mode === "slides")
                click(centre(named("chordsMode")))
                return 700
            },
            () => {
                check("Chords lays the song out as one sheet: its groups, and every line of every slide", editScreen.mode === "chords" && sheet() !== null
                      && sheet().rows.filter(r => r.kind === "group").length >= 4 && sheet().rows.filter(r => r.kind === "line").length >= 20 && lineOf(herald) >= 0,
                      sheet() ? sheet().rows.filter(r => r.kind === "group").map(r => r.name).join("|") + " " + sheet().rows.length : "")
                check("the key is the one the chords are written in, and its chords are on the keys 1 to 7", sheet().key === "E" && named("chordKeyBox").currentText === "E"
                      && named("keyChord4").label === "A" && named("keyChord6").label === "C#m", sheet().key + " " + sheet().inKey.join(" "))
                kept.song = JSON.stringify(editScreen.editor.song())
                kept.line = lineOf(herald)
                check("the line's chords are in their bubbles", names(kept.line) === "1:E 16:Asus2", names(kept.line))
                testInput.grab("4-chord-sheet")
                // The pointer over the middle of "herald": the spot goes to the start of the word.
                const p = over(kept.line, 11)
                testInput.mouse(1, p.x, p.y)
                return 300
            },
            () => {
                check("the spot follows the pointer, to the letter under it, in the middle of a word", sheet().spotRow === kept.line && sheet().spotAt === 11,
                      sheet().spotRow + " " + sheet().spotAt)
                const p = over(kept.line, 11)
                testInput.mouse(1, p.x + 1, p.y, Qt.ShiftModifier)
                check("and with Shift to the start of the word", sheet().spotAt === 9, sheet().spotAt)
                testInput.grab("5-spot")
                testInput.key(Qt.Key_4, 0, "4")
                return 400
            },
            () => {
                check("4 puts the key's fourth chord on the spot", names(kept.line) === "1:E 9:A 16:Asus2", names(kept.line))
                const disk = onDisk(kept.path, herald)
                check("and it is in the file at once, on that slide's own words", disk !== null && disk.chords.map(c => c.at + ":" + c.name).join(" ").startsWith("1:E 9:A 16:Asus2"),
                      disk ? JSON.stringify(disk.chords) : "")
                testInput.key(Qt.Key_F, 0, "f")
                return 300
            },
            () => {
                const field = named("chordField")
                check("a letter opens the bubble to type the chord in, as a capital", sheet().typing && field.activeFocus && field.text === "F", field.text)
                testInput.type("#m7")
                check("a chord is typed", field.text === "F#m7", field.text)
                testInput.type("!")
                testInput.type(" ")
                check("and nothing that could not be part of one", field.text === "F#m7", field.text)
                const entryBox = named("chordEntry")
                check("what it might be going to be is offered under it", entryBox.offered.length > 0 && entryBox.offered.every(c => c.startsWith("F#m7")), entryBox.offered.join(" "))
                check("the bubble is on the sheet, at the spot, though the sheet was laid out again by the chord before", entryBox.visible && entryBox.item !== null
                      && entryBox.item.index === kept.line && entryBox.height > 40, entryBox.visible + " " + entryBox.height)
                testInput.grab("6-typing")
                testInput.key(Qt.Key_Return)
                return 400
            },
            () => {
                check("Enter puts it there, in place of the one that was", !sheet().typing && names(kept.line) === "1:E 9:F#m7 16:Asus2", names(kept.line))
                testInput.key(Qt.Key_Right)
                check("Right moves the spot to the next word", sheet().spotAt === 16, sheet().spotAt)
                testInput.key(Qt.Key_Right)
                testInput.key(Qt.Key_B, 0, "b")
                return 300
            },
            () => {
                const entryBox = named("chordEntry")
                check("the song's own chords that start so are offered first", sheet().spotAt === 23 && entryBox.offered[0].startsWith("B") && sheet().used.includes(entryBox.offered[0]),
                      sheet().spotAt + " " + entryBox.offered.join(" "))
                testInput.key(Qt.Key_Down)
                kept.picked = entryBox.offered[0]
                testInput.key(Qt.Key_Tab)
                return 400
            },
            () => {
                check("Down picks one, and Tab takes it and moves on to the next word, on the next line",
                      names(kept.line) === "1:E 9:F#m7 16:Asus2 23:" + kept.picked && sheet().spotRow === kept.line + 1 && sheet().spotAt === 0,
                      names(kept.line) + " then " + sheet().spotRow + ":" + sheet().spotAt)
                // The chord on "herald" is dragged to "sons" of another slide's line.
                kept.other = lineOf(born)
                const bubble = named("chord:" + kept.line + ":9")
                const from = bubble.mapToItem(null, bubble.width / 2, 8)
                const to = over(kept.other, 18)
                testInput.mouse(0, from.x, from.y)
                for (let i = 1; i <= 10; ++i)
                    testInput.mouse(1, from.x + (to.x - from.x) * i / 10, from.y + (to.y - from.y) * i / 10)
                testInput.grab("7-dragging")
                check("a chord being dragged shows where it would land", sheet().dragged !== null && sheet().spotRow === kept.other && sheet().spotAt === 18,
                      sheet().spotRow + ":" + sheet().spotAt)
                testInput.mouse(2, to.x, to.y)
                return 400
            },
            () => {
                check("dropped, it has left its word and is on the other, in place of the chord that was there",
                      names(kept.line) === "1:E 16:Asus2 23:" + kept.picked && names(kept.other) === "0:A 18:F#m7", names(kept.line) + " / " + names(kept.other))
                check(editScreen.editor.undo() === "", true)
                return 300
            },
            () => {
                check("which is one thing to undo, though it was two slides", names(kept.line) === "1:E 9:F#m7 16:Asus2 23:" + kept.picked && names(kept.other) === "0:A 18:F#m",
                      names(kept.line) + " / " + names(kept.other))
                // The line's chords are copied onto the line under it.
                let p = over(kept.line, 0)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_C, Qt.ControlModifier)
                p = over(kept.line + 1, 0)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_V, Qt.ControlModifier)
                return 400
            },
            () => {
                // "Hark the herald angels sing" onto "Glory to the newborn King": word for word.
                check("a line's chords are copied onto another, word for word", names(kept.line + 1) === "1:E 9:F#m7 13:Asus2 21:" + kept.picked, names(kept.line + 1))
                const p = over(kept.line + 1, 9)
                testInput.mouse(1, p.x, p.y)
                testInput.key(Qt.Key_Delete)
                return 300
            },
            () => {
                check("Delete takes off the chord at the spot", names(kept.line + 1) === "1:E 13:Asus2 21:" + kept.picked, names(kept.line + 1))
                // A chord in the middle of a word: on the r of "Glory".
                const r = over(kept.line + 1, 3)
                testInput.mouse(1, r.x, r.y)
                testInput.key(Qt.Key_2, 0, "2")
                return 300
            },
            () => {
                const disk = onDisk(kept.path, herald)
                check("a chord goes on any letter, and is kept on that letter in the file", names(kept.line + 1) === "1:E 3:F#m 13:Asus2 21:" + kept.picked
                      && disk !== null && disk.chords.some(c => c.at === herald.length + 1 + 3 && c.name === "F#m"), names(kept.line + 1))
                const cards = sheet().rows.filter(row => row.kind === "line" && row.first)
                const tall = lineItem(sheet().rows.findIndex(row => row.kind === "line" && row.count === sheet().mostLines && row.first))
                check("each slide's lines are in a card, all of one size: that of the slide with the most lines", cards.length >= 20 && sheet().mostLines >= 2
                      && sheet().cardHeight > sheet().mostLines * 40 && sheet().cardWidth > 300 && tall !== null, cards.length + " cards of " + sheet().mostLines + " lines, "
                      + Math.round(sheet().cardWidth) + " by " + Math.round(sheet().cardHeight))
                const one = sheet().rows.findIndex(row => row.kind === "line" && row.first && row.last && row.count === 1)
                const next = sheet().rows.findIndex((row, index) => index > one && row.kind === "line" && row.first)
                check("so a slide of one line takes as much room as one of two", one >= 0 && next > one
                      && Math.abs((lineItem(next).mapToItem(null, 0, 0).y - lineItem(one).mapToItem(null, 0, 0).y) - (sheet().cardHeight + sheet().cardGap)) < 40,
                      one + " " + next)
                // A line of chords with no words: one more on the end.
                kept.alone = sheet().rows.findIndex(row => row.kind === "line" && row.alone)
                kept.aloneCount = sheet().rows[kept.alone].chords.length
                check("an intro's chords, which have no words, are on the sheet too", kept.alone >= 0 && kept.aloneCount >= 3, names(kept.alone))
                sheet().setSpot(kept.alone, kept.aloneCount * 2)
                testInput.key(Qt.Key_1, 0, "1")
                return 400
            },
            () => {
                const row = sheet().rows[kept.alone]
                check("one more is put on its end, and the characters it hangs on are made for it", row.chords.length === kept.aloneCount + 1
                      && row.chords[kept.aloneCount].name === "E" && row.text.length === (kept.aloneCount + 1) * 2 - 1 && row.alone,
                      names(kept.alone) + " over " + row.text.length + " characters")
                // A blank slide: nothing on it but an empty text box, as an intro or an ending is often made.
                kept.blank = sheet().rows.findIndex(r => r.kind === "line" && r.blank)
                const blank = sheet().rows[kept.blank]
                check("a blank slide is on the sheet too, in a card of its own, as a line of chords alone that has none yet",
                      kept.blank >= 0 && blank.alone && blank.text === "" && blank.chords.length === 0 && blank.first && blank.last === true
                      && sheet().usable(kept.blank) && sheet().stops(kept.blank).join(" ") === "0", kept.blank + " " + JSON.stringify(blank ?? null))
                check("and only the one: a blank text box on a slide that has words is not", sheet().rows.filter(r => r.kind === "line" && r.blank).length === 1
                      && editScreen.editor.song().filter(b => b.blank).length === 1, sheet().rows.filter(r => r.kind === "line" && r.blank).length)
                kept.blankRow = blank.row
                kept.blankElement = blank.element
                kept.blankBox = JSON.stringify(boxOnDisk(kept.path, kept.blankElement))
                kept.blankHash = testInput.fileHash(kept.path)
                // Its card is brought into view, for the pointer to go over it.
                const view = named("chordSheetView")
                kept.scrolled = view.contentY
                view.contentY = Math.max(0, Math.min(Math.max(0, view.contentHeight - view.height), lineItem(kept.blank).y - 80))
                return 400
            },
            () => {
                const p = over(kept.blank, 0)
                testInput.mouse(1, p.x, p.y)
                return 300
            },
            () => {
                check("the spot goes to it with the pointer, as to any line", sheet().spotRow === kept.blank && sheet().spotAt === 0 && named("chordSpot").visible,
                      sheet().spotRow + ":" + sheet().spotAt)
                testInput.grab("7a-blank-slide")
                testInput.key(Qt.Key_5, 0, "5")
                return 400
            },
            () => {
                const row = sheet().rows[kept.blank]
                const box = boxOnDisk(kept.path, kept.blankElement)
                check("5 puts the key's fifth chord on it, hung on a character made for it in the slide's empty text box",
                      names(kept.blank) === "0:B" && row.alone && !row.blank && row.text === zeroWidth && box.text === zeroWidth && box.chords === "0:B",
                      names(kept.blank) + " over " + units(row.text) + ", in the file " + box.chords + " over " + units(box.text))
                // A second, typed after it.
                sheet().setSpot(kept.blank, 2)
                testInput.key(Qt.Key_C, 0, "c")
                return 300
            },
            () => {
                testInput.type("#m")
                testInput.key(Qt.Key_Return)
                return 400
            },
            () => {
                const box = boxOnDisk(kept.path, kept.blankElement)
                check("a second is typed after it, with the wide space between the two that Multitracks puts there",
                      names(kept.blank) === "0:B 2:C#m" && box.text === zeroWidth + emQuad + zeroWidth && box.chords === "0:B 2:C#m",
                      names(kept.blank) + ", in the file " + box.chords + " over " + units(box.text))
                testInput.grab("7a2-blank-slide-with-chords")
                sheet().setSpot(kept.blank, 2)
                testInput.key(Qt.Key_Delete)
                return 300
            },
            () => {
                check("Delete takes one off", names(kept.blank) === "0:B", names(kept.blank))
                sheet().setSpot(kept.blank, 0)
                testInput.key(Qt.Key_Delete)
                return 300
            },
            () => {
                const row = sheet().rows[kept.blank]
                const box = boxOnDisk(kept.path, kept.blankElement)
                check("and with the last one off the slide is blank as it was, and still on the sheet to be given others",
                      row.blank && row.alone && row.text === "" && row.chords.length === 0 && sheet().usable(kept.blank) && box.text === "" && box.chords === "",
                      JSON.stringify(row) + ", in the file " + box.chords + " over " + units(box.text))
                check("nothing is left behind by the change of mind: the song's file is byte for byte what it was before the first of them",
                      testInput.fileHash(kept.path) === kept.blankHash, testInput.fileHash(kept.path) + " " + kept.blankHash)
                // The same slide with no text box at all: its empty one is taken off it, as Slides takes an element off.
                const off = editScreen.editor.remove(kept.blankRow, kept.blankElement)
                check("(the slide's empty text box is taken off it, to make a slide with nothing on it)", off === "", off)
                return 400
            },
            () => {
                const row = sheet().rows[kept.blank]
                check("a slide with no text box at all is on the sheet as well: a blank line, with no text box behind it yet",
                      row.blank && row.alone && row.element === "" && sheet().usable(kept.blank) && catalog.open(kept.path).slides[kept.blankRow].elements.length === 0,
                      JSON.stringify(row))
                kept.bareHash = testInput.fileHash(kept.path)
                sheet().setSpot(kept.blank, 0)
                testInput.key(Qt.Key_4, 0, "4")
                return 400
            },
            () => {
                const row = sheet().rows[kept.blank]
                const slide = catalog.open(kept.path).slides[kept.blankRow]
                const made = slide.elements[0]
                // What it is made like: the first text box with words or chords on the nearest slide before.
                const before = editScreen.editor.song().filter(b => b.row < kept.blankRow && !b.blank)
                const model = before.find(b => b.row === Math.max(...before.map(b => b.row)))
                const like = catalog.open(kept.path).slides[model.row].elements.find(e => e.id === model.element)
                check("a chord put there adds the text box it needs, and only now: one, with the chord's stand-in in it",
                      names(kept.blank) === "0:A" && !row.blank && row.element !== "" && slide.elements.length === 1 && made.id === row.element
                      && testInput.plain(made.text) === zeroWidth && (made.chords ?? []).length === 1,
                      names(kept.blank) + ", " + slide.elements.length + " on the slide, over " + units(made ? testInput.plain(made.text) : ""))
                check("it is a copy of the song's own text box from the slide before: named, placed and sized as that is", made.id !== like.id && made.name === like.name
                      && made.x === like.x && made.y === like.y && made.width === like.width && made.height === like.height && made.textBox === true,
                      made.name + " " + [made.x, made.y, made.width, made.height].join(",") + " like " + like.name + " " + [like.x, like.y, like.width, like.height].join(","))
                testInput.grab("7a3-slide-given-a-text-box")
                testInput.key(Qt.Key_Delete)
                return 400
            },
            () => {
                const row = sheet().rows[kept.blank]
                check("taking the chord off again takes that text box off with it: the slide has nothing on it, and the file is byte for byte what it was",
                      row.blank && row.element === "" && catalog.open(kept.path).slides[kept.blankRow].elements.length === 0
                      && testInput.fileHash(kept.path) === kept.bareHash, JSON.stringify(row) + " " + catalog.open(kept.path).slides[kept.blankRow].elements.length)
                // Back to the slide with its own empty text box, for what follows: the chord's going, its coming, and the box's.
                const undone = [editScreen.editor.undo(), editScreen.editor.undo(), editScreen.editor.undo()]
                check("(those three changes are undone)", undone.join("") === "", undone.join(" | "))
                return 400
            },
            () => {
                check("undone, the slide has its own empty text box again, and the file is what it was before any of it",
                      sheet().rows[kept.blank].blank && sheet().rows[kept.blank].element === kept.blankElement && testInput.fileHash(kept.path) === kept.blankHash,
                      JSON.stringify(sheet().rows[kept.blank]))
                named("chordSheetView").contentY = kept.scrolled
                return 400
            },
            () => {
                // A run of chords with the mouse and the letters alone: type one, click where the next goes.
                kept.peace = lineOf(peace)
                kept.god = lineOf("God and sinners reconciled")
                // (The third is the slide that is one line, "Glory to the newborn King", over those two.)
                kept.glory = kept.line + 2
                check("found the lines to go by", kept.peace >= 0 && kept.god >= 0 && sheet().rows[kept.glory].first && names(kept.peace) === "0:E 19:Asus2"
                      && names(kept.god) === "0:Bsus4" && names(kept.glory) === "0:Bsus4 13:C#m", names(kept.peace) + " / " + names(kept.god) + " / " + names(kept.glory))
                const a = over(kept.peace, 9)
                testInput.mouse(1, a.x, a.y)
                testInput.key(Qt.Key_G, 0, "g")
                return 300
            },
            () => {
                check("a chord is being typed on one word", sheet().typing && sheet().typingRow === kept.peace && sheet().typingAt === 9 && named("chordField").text === "G",
                      sheet().typingRow + ":" + sheet().typingAt + " " + named("chordField").text)
                const b = over(kept.god, 8)
                testInput.mouse(1, b.x, b.y)
                return 300
            },
            () => {
                const entryBox = named("chordEntry")
                check("while it is, the spot goes on following the pointer, and shows where the next chord would go",
                      sheet().typing && sheet().spotRow === kept.god && sheet().spotAt === 8 && named("chordSpot").visible
                      && entryBox.visible && entryBox.item.index === kept.peace && named("chordField").text === "G" && named("chordField").activeFocus,
                      "spot " + sheet().spotRow + ":" + sheet().spotAt + " shown " + named("chordSpot").visible + ", typing at " + sheet().typingRow + ":" + sheet().typingAt)
                testInput.grab("7b-typing-and-pointing")
                click(over(kept.god, 8))
                return 500
            },
            () => {
                check("a click there keeps the chord that was being typed, and opens the bubble on the place clicked",
                      names(kept.peace) === "0:E 9:G 19:Asus2" && sheet().typing && sheet().typingRow === kept.god && sheet().typingAt === 8
                      && named("chordField").text === "" && named("chordField").activeFocus && named("chordEntry").item.index === kept.god,
                      names(kept.peace) + ", typing at " + sheet().typingRow + ":" + sheet().typingAt + " '" + named("chordField").text + "'")
                testInput.type("d")
                click(over(kept.glory, 9))
                return 500
            },
            () => {
                check("and so on, chord after chord, with no key between to say it is done", names(kept.god) === "0:Bsus4 8:D" && sheet().typing
                      && sheet().typingRow === kept.glory && sheet().typingAt === 9, names(kept.god) + ", typing at " + sheet().typingRow + ":" + sheet().typingAt)
                const disk = onDisk(kept.path, peace)
                check("each of them in the file", disk !== null && disk.chords.some(c => c.at === 9 && c.name === "G") && disk.chords.some(c => c.name === "D"),
                      disk ? JSON.stringify(disk.chords) : "")
                testInput.type("a")
                testInput.key(Qt.Key_Escape)
                check("Esc is what throws a chord being typed away", !sheet().typing && names(kept.glory) === "0:Bsus4 13:C#m", names(kept.glory))
                const c = over(kept.glory, 9)
                testInput.mouse(1, c.x + 1, c.y)
                testInput.key(Qt.Key_C, 0, "c")
                return 300
            },
            () => {
                check("another is being typed", sheet().typing && named("chordField").text === "C")
                // Beside the cards there is nothing but the sheet.
                const view = named("chordSheetView")
                click(view.mapToItem(null, view.width - 40, 70))
                return 400
            },
            () => {
                check("a click on the bare sheet keeps it too, and ends the typing", !sheet().typing && names(kept.glory) === "0:Bsus4 9:C 13:C#m", names(kept.glory))
                const g = over(kept.god, 16)
                testInput.mouse(1, g.x, g.y)
                testInput.key(Qt.Key_F, 0, "f")
                return 300
            },
            () => {
                check("and another", sheet().typing && sheet().typingRow === kept.god && sheet().typingAt === 16, sheet().typingRow + ":" + sheet().typingAt)
                click(centre(named("keyChord5")))
                return 400
            },
            () => {
                check("one of the key's chords clicked while typing goes where the typing was, in place of what was typed",
                      !sheet().typing && names(kept.god) === "0:Bsus4 8:D 16:B", names(kept.god))
                const m = over(kept.peace, 15)
                testInput.mouse(1, m.x, m.y)
                testInput.key(Qt.Key_A, 0, "a")
                return 300
            },
            () => {
                check("one more, left in its bubble", sheet().typing && named("chordField").text === "A")
                click(centre(named("chordProMode")))
                return 700
            },
            () => {
                const area = named("chordProText")
                check("ChordPro shows the same song as text, its groups named and its chords in brackets", editScreen.mode === "chordpro" && area !== null
                      && area.text.includes("{c: Verse 1}") && area.text.includes("H[E]ark the [F#m7]herald [Asus2]angels [" + kept.picked + "]sing"),
                      area ? area.text.split("\n").find(line => line.includes("herald")) : "")
                check("and the chord that was still in its bubble when the sheet was left was kept", area.text.includes("[E]Peace on [G]earth [A]and [Asus2]mercy mild"),
                      area.text.split("\n").find(line => line.includes("mercy")))
                testInput.grab("8-chordpro")
                kept.text = area.text
                area.forceActiveFocus()
                area.cursorPosition = area.text.indexOf("Born to raise") + 5
                testInput.type("x")
                check("the words cannot be typed over", area.text === kept.text, area.text.length + " " + kept.text.length)
                testInput.type("[")
                check("a bracket brings its other with it, and the caret is between them", area.text.includes("Born []to raise")
                      && area.cursorPosition === area.text.indexOf("Born []to raise") + 6, area.text.split("\n").find(l => l.includes("to raise")))
                testInput.type("G/B")
                return 1200
            },
            () => {
                const area = named("chordProText")
                const line = area.text.split("\n").find(l => l.includes("to raise"))
                check("a chord is typed between them", line.includes("[G/B]to raise"), line)
                const disk = onDisk(kept.path, born)
                check("and a moment later is in the file", disk !== null && disk.chords.some(c => c.at === 5 && c.name === "G/B"), disk ? JSON.stringify(disk.chords) : "")
                area.cursorPosition = area.text.indexOf("[G/B]") + 5
                testInput.key(Qt.Key_Backspace)
                return 1200
            },
            () => {
                const area = named("chordProText")
                const disk = onDisk(kept.path, born)
                check("Backspace on its bracket takes the whole chord away", !area.text.includes("[G/B]") && disk !== null && !disk.chords.some(c => c.name === "G/B"),
                      disk ? JSON.stringify(disk.chords) : "")
                // The blank slide is a line of the text too: an empty one, after the empty line that parts it from the slide before.
                const pro = named("chordProEditor")
                const lines = area.text.split("\n")
                kept.proLine = pro.layout.findIndex(l => l.kind === "line" && l.row === kept.blankRow)
                check("in ChordPro a blank slide is an empty line of its own, kept as a line of chords alone", kept.proLine > 0 && pro.layout[kept.proLine].alone
                      && lines[kept.proLine] === "" && pro.layout[kept.proLine - 1].kind !== "line", kept.proLine + " '" + lines[kept.proLine] + "'")
                kept.proStart = lines.slice(0, kept.proLine).join("\n").length + 1
                kept.proText = area.text
                kept.proHash = testInput.fileHash(kept.path)
                // The line before it is not the slide's, and takes nothing.
                area.cursorPosition = kept.proStart - 1
                testInput.type("[")
                check("the line before it, which is the gap or the group's name, cannot be typed on", area.text === kept.proText, area.text.length + " " + kept.proText.length)
                area.cursorPosition = kept.proStart
                testInput.type("[")
                testInput.type("A")
                return 1200
            },
            () => {
                const area = named("chordProText")
                const box = boxOnDisk(kept.path, kept.blankElement)
                check("a chord typed on the slide's own line is saved as a chord alone, on a character made for it", area.text.split("\n")[kept.proLine] === "[A]"
                      && box.text === zeroWidth && box.chords === "0:A", "'" + area.text.split("\n")[kept.proLine] + "', in the file " + box.chords + " over " + units(box.text))
                area.cursorPosition = kept.proStart + 3
                testInput.key(Qt.Key_Backspace)
                return 1200
            },
            () => {
                const area = named("chordProText")
                const box = boxOnDisk(kept.path, kept.blankElement)
                check("and taken away again leaves the slide blank, and the file what it was", area.text === kept.proText && box.text === "" && box.chords === ""
                      && testInput.fileHash(kept.path) === kept.proHash, "'" + area.text.split("\n")[kept.proLine] + "', in the file " + box.chords + " over " + units(box.text))
                click(centre(named("slidesMode")))
                return 400
            },
            () => {
                check("Slides is the editor as it was", editScreen.mode === "slides" && editScreen.canvas.visible && named("chordSheet") === null && named("chordProText") === null)
                let undone = 0
                while (editScreen.editor.canUndo && undone < 60) {
                    editScreen.editor.undo()
                    ++undone
                }
                check("every chord change can be undone, back to the song as it was", JSON.stringify(editScreen.editor.song()) === kept.song, undone + " steps")
                check("the blank slide's text box among them: in the file it has no words and no chords again", JSON.stringify(boxOnDisk(kept.path, kept.blankElement)) === kept.blankBox,
                      JSON.stringify(boxOnDisk(kept.path, kept.blankElement)))
                // Left in the file, to be looked at there once the test is over (after_chords in run.py): the blank slide with two
                // chords, and one more on the end of the first line of chords alone.
                const intro = editScreen.editor.song().find(b => b.chords.length > 0 && Chords.isPlaceholders(b.text))
                const left = [editScreen.editor.setChordsAlone(kept.blankRow, kept.blankElement, 0, ["E", "B/D#"]),
                              editScreen.editor.setChordsAlone(intro.row, intro.element, 0, intro.chords.map(c => c.name).concat(["A"]))]
                check("two changes are left in the file for that", left.join("") === "", left.join(" | "))
                stopEditing()
                return 500
            },
            () => {
                // Importing a ChordPro file
                kept.file = "@WORKSPACES@/ProPresenter MR/Plain Song.cho"
                const found = Chords.describeFile(kept.file)
                check("a ChordPro file is read: its title, its key, its parts and its chords", found.error === "" && found.title === "Plain Song" && found.key === "G"
                      && found.sections.map(s => s.name + ":" + s.lines).join(" ") === "Verse 1:4 Chorus:2 Outro:1" && found.chords === 11, JSON.stringify(found))
                click(centre(named("addLibraryButton")))
                return 400
            },
            () => {
                check("the + beside Libraries offers a new library and a ChordPro file", labels() === "New Library, Import ChordPro File…", labels())
                menu.close()
                importSongPanel.show(kept.file)
                return 400
            },
            () => {
                check("the import says what it found and how many slides it makes at two lines each", importSongPanel.visible && importSongPanel.lines === 2 && importSongPanel.slideCount === 4,
                      importSongPanel.slideCount)
                testInput.grab("9-import")
                importSongPanel.lines = 1
                check("and at another number", importSongPanel.slideCount === 7, importSongPanel.slideCount)
                importSongPanel.lines = 2
                click(centre(named("importSongButton")))
                return 900
            },
            () => {
                check("the song comes into the library that is open, and is opened", document !== null && document.name === "Plain Song" && document.path.endsWith("/Plain Song.pro")
                      && documents.some(d => d.name === "Plain Song"), document ? document.path : "")
                check("as a slide for every two lines, in a group for each part, with its key", document.slides.length === 4
                      && document.slides.map(s => s.group).join("|") === "Verse 1|Verse 1|Chorus|Outro" && document.originalKey === "G" && document.hasChords,
                      document.slides.map(s => s.group).join("|") + " " + document.originalKey)
                const first = document.slides[0].elements[0]
                check("its words are in a text box named Lyrics, with the chords over them", first.name === "Lyrics" && testInput.plain(first.text) === "One two three four\nFive six seven eight"
                      && first.chords.map(c => c.at + ":" + c.name).join(" ") === "0:G 14:D 28:Em", JSON.stringify(first.chords))
                const outro = document.slides[3].elements[0]
                check("and a line of chords alone hangs on stand-ins", outro.chords.map(c => c.at + ":" + c.name).join(" ") === "0:G 2:D/F# 4:Em" && testInput.plain(outro.text).length === 5,
                      JSON.stringify(outro.chords))
                testInput.grab("10-imported")
                click(centre(named("addLibraryButton")))
                return 400
            },
            () => {
                kept.libraries = catalog.libraries.length
                click(menuRow("New Library"))
                return 600
            },
            () => {
                check("New Library makes one, opens it and starts naming it", catalog.libraries.length === kept.libraries + 1 && libraryPath.endsWith("/New Library")
                      && sidebar.renaming && documents.length === 0, libraryPath)
                testInput.key(Qt.Key_A, Qt.ControlModifier)
                testInput.type("Hymns")
                testInput.key(Qt.Key_Return)
                return 600
            },
            () => {
                check("and it is called what was typed", catalog.libraries.some(l => l.name === "Hymns") && !catalog.libraries.some(l => l.name === "New Library")
                      && libraryPath.endsWith("/Hymns") && !sidebar.renaming, catalog.libraries.map(l => l.name).join("|") + " " + libraryPath)
                const full = catalog.libraries.find(l => l.name !== "Hymns")
                const renamed = catalog.renameLibrary(full.path, "Other")
                check("a library with presentations in it is not renamed", renamed.error !== "" && renamed.path === full.path
                      && catalog.libraries.some(l => l.path === full.path), renamed.error)
            }
        ]
        next()
    }
}
