import QtQuick
import SimplePresenterApp
import "lib.js" as Lib

// What stands round the two chord editors, and the ChordPro text (chords.qml and chords2.qml have the rest): the switch
// between the editor's ways of working, in the middle of its toolbar; the song's two keys beside it, the key its chords
// are written in and the key they are shown in, and that a change of key is a step to undo; "Autosave on" and Revert,
// which asks, makes the file what it was when it was opened, and can be taken back; the colours of chords in the two
// editors; and in the ChordPro text its size, a name that is no chord, which text boxes a saving writes and how it is
// undone, a change from elsewhere, and a line of chords alone that is emptied and takes chords again. What the files
// then hold is looked at when the test is over, by after_chords3 in run.py.
QtObject {
    id: t

//COMMON
    function sheet() {
        return named("chordSheet")
    }

    function area() {
        return named("chordProText")
    }

    function names(index) {
        return sheet().rows[index].chords.map(c => c.at + ":" + c.name).join(" ")
    }

    function lineOf(words) {
        return sheet().rows.findIndex(row => row.kind === "line" && row.text === words)
    }

    // A text box as the file has it now, by its slide and its id
    function boxOnDisk(row, element) {
        const found = catalog.open(kept.path).slides[row].elements.find(e => e.id === element)
        return found ? { text: testInput.plain(found.text), chords: (found.chords ?? []).map(c => c.at + ":" + c.name).join(" ") } : { text: "(no such box)", chords: "" }
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

    function units(text) {
        return "[" + Array.from(text).map(c => c.charCodeAt(0).toString(16)).join(" ") + "]"
    }

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

    // The most green there is, over red, in the points across the middle of a part of an item: how green what is
    // drawn there is.
    function greenest(item, x, y, width) {
        let most = -255
        for (let i = 0; i < width; i += 1) {
            const c = pixel(item, x + i + 0.5, y)
            most = Math.max(most, parseInt(c.substring(3, 5), 16) - parseInt(c.substring(1, 3), 16))
        }
        return most
    }

    // The line of the ChordPro text that has these words, and where it starts in the text
    function proLine(words) {
        const lines = area().text.split("\n")
        const index = lines.findIndex(line => Chords.withoutChords(line) === words)
        return { index: index, text: index >= 0 ? lines[index] : "", start: lines.slice(0, Math.max(0, index)).join("\n").length + (index > 0 ? 1 : 0) }
    }

    // Types at a place of the ChordPro text
    function typeAt(position, text) {
        area().forceActiveFocus()
        area().cursorPosition = position
        testInput.type(text)
    }

    function pick(box, key) {
        box.activated(box.model.indexOf(key))
    }

    function run() {
        const herald = "Hark the herald angels sing"
        const born = "Born to raise the sons of earth"
        const peace = "Peace on earth and mercy mild"
        const zeroWidth = String.fromCharCode(0x200b)
        steps = [
            () => {
                openLibrary(catalog.libraries[0].path)
                kept.path = entry("Hark2").path
                kept.hash = testInput.fileHash(kept.path)
                startEditing(entry("Hark2"))
                return 700
            },
            () => {
                // ---- The switch
                const modes = named("editorModes")
                const beside = editScreen.width - editScreen.sidebarWidth
                const middle = modes.mapToItem(editScreen, modes.width / 2, 0).x
                kept.switchAt = middle
                check("the editor's three ways of working are a switch in the middle of its toolbar, as tall as a button is", modes.visible && modes.height === 36
                      && Math.abs(middle - (editScreen.width - beside / 2)) <= 1.5, "its middle at " + middle + " of " + editScreen.width)
                check("each with a picture before its name", ["slidesMode", "chordsMode", "chordProMode"].every(name => {
                    const button = named(name)
                    const word = Lib.find(button, child => child.text !== undefined && ["Slides", "Chords", "ChordPro"].includes(child.text))
                    return button.visible && word !== null && word.x >= 14 + 18 + 7
                }))
                const drawn = [pixel(named("slidesMode"), 5, 15), pixel(named("chordsMode"), 5, 15)]
                check("the one that is on is filled with the app's orange: the slides, to begin with", editScreen.mode === "slides" && orange(drawn[0]) && !orange(drawn[1]), drawn.join(" "))
                // (They are not made until a chord editor is turned to.)
                check("over the slides are the tools for a slide, and not the song's keys nor anything about saving", named("addTextTool").visible && named("chordKeyBox") === null
                      && named("chordShownKeyBox") === null && named("revertButton") === null && named("autosaveNote") === null)
                const below = named("editorSubjectBelow")
                check("what is being edited is said on the line along the bottom there, the toolbar having no room for it beside the inspector", below.visible
                      && below.text.startsWith("Hark2") && below.text.includes("Slide 1 of 24") && strokes(below).light >= 2, below.text)
                testInput.grab("1-slides")
                click(centre(named("chordsMode")))
                return 700
            },
            () => {
                const modes = named("editorModes")
                const drawn = [pixel(named("slidesMode"), 5, 15), pixel(named("chordsMode"), 5, 15)]
                check("a click on Chords shows the chord sheet, and the orange has gone over to it", editScreen.mode === "chords" && sheet() !== null && !orange(drawn[0]) && orange(drawn[1]),
                      drawn.join(" "))
                check("the switch has not moved from under the pointer", Math.abs(modes.mapToItem(editScreen, modes.width / 2, 0).x - kept.switchAt) <= 0.5,
                      modes.mapToItem(editScreen, modes.width / 2, 0).x + ", it was at " + kept.switchAt)
                // ---- The song's two keys
                const key = named("chordKeyBox")
                const shown = named("chordShownKeyBox")
                check("over the chord editors the toolbar has the song's two keys in place of the tools for a slide: the key its chords are written in, and the key they are shown in",
                      !named("addTextTool").visible && key.visible && shown.visible && key.currentText === "E" && shown.currentText === "C#" && shown.enabled
                      && key.mapToItem(editScreen, key.width, 0).x < shown.mapToItem(editScreen, 0, 0).x
                      && shown.mapToItem(editScreen, shown.width, 0).x < modes.mapToItem(editScreen, 0, 0).x, key.currentText + " " + shown.currentText)
                const subject = Lib.find(editScreen, child => child.text !== undefined && String(child.text).startsWith("Hark2") && child.visible === true)
                check("and after the switch, what is being edited, for which there is room there", subject !== null && subject.objectName !== "editorSubjectBelow"
                      && !named("editorSubjectBelow").visible && subject.mapToItem(editScreen, 0, 0).x > modes.mapToItem(editScreen, modes.width, 0).x, subject ? subject.text : "nowhere")
                check("and the sheet's strip has the key's chords, and no picker of its own", named("keyChord1").label === "E" && Lib.findAll(sheet(), child => child.objectName === "chordKeyBox").length === 0)
                testInput.grab("2-chords")
                pick(key, "D")
                return 400
            },
            () => {
                const opened = catalog.open(kept.path)
                check("the key picked says what key the chords are written in, in the file at once", editScreen.editor.key === "D" && opened.originalKey === "D"
                      && named("keyChord1").label === "D" && named("keyChord4").label === "G", opened.originalKey)
                check("the key the song is shown in is left as it was: someone chose another, and it is not for this to undo that", editScreen.editor.shownKey === "C#"
                      && opened.userKey === "C#" && named("chordShownKeyBox").currentText === "C#", opened.userKey)
                check("a change of key is a step to undo like any other", editScreen.editor.canUndo && editScreen.editor.undo() === "" && editScreen.editor.key === "E"
                      && catalog.open(kept.path).originalKey === "E" && testInput.fileHash(kept.path) === kept.hash && named("keyChord1").label === "E", editScreen.editor.key)
                check("and to do again", editScreen.editor.redo() === "" && editScreen.editor.key === "D" && editScreen.editor.undo() === "" && editScreen.editor.key === "E")
                pick(named("chordShownKeyBox"), "F")
                return 400
            },
            () => {
                const opened = catalog.open(kept.path)
                check("the second picker sets the key the song is shown in, and that alone", editScreen.editor.shownKey === "F" && editScreen.editor.key === "E"
                      && opened.userKey === "F" && opened.originalKey === "E", opened.originalKey + " shown in " + opened.userKey)
                check("(undone)", editScreen.editor.undo() === "" && editScreen.editor.shownKey === "C#" && testInput.fileHash(kept.path) === kept.hash)
                // ---- Nothing is saved by hand, and the editor says so
                const note = named("autosaveNote")
                const revert = named("revertButton")
                check("under the chord editors are the words Autosave on, where a Save button would be, and Revert", note.visible && note.text === "Autosave on"
                      && strokes(note).light >= 2 && revert.visible && revert.text === "Revert…", note.text)
                check("Revert is greyed while the song is as it was opened", !revert.enabled && !editScreen.editor.canRevert)
                kept.line = lineOf(herald)
                kept.was = names(kept.line)
                sheet().setSpot(kept.line, 9)
                testInput.key(Qt.Key_4, 0, "4")
                sheet().setSpot(kept.line + 1, 0)
                testInput.key(Qt.Key_5, 0, "5")
                pick(named("chordKeyBox"), "D")
                return 500
            },
            () => {
                const revert = named("revertButton")
                kept.changed = testInput.fileHash(kept.path)
                kept.changedNames = names(kept.line)
                check("with changes made (two chords, and the key) it can be pressed", revert.enabled && editScreen.editor.canRevert && kept.changed !== kept.hash
                      && names(kept.line) === "1:E 9:A 16:Asus2" && editScreen.editor.key === "D", names(kept.line))
                click(centre(revert))
                return 500
            },
            () => {
                check("Revert asks first, and says that it is every change", menu.visible && labels() === "[Revert], (note), Revert All Changes, Cancel"
                      && menu.shown[1].note === "Revert “Hark2” to how it was when it was opened? This takes back EVERY change made to this song since it was opened in this editor.",
                      labels() + " / " + menu.shown[1].note)
                testInput.grab("3-revert-asks")
                click(menuRow("Cancel"))
                return 400
            },
            () => {
                check("Cancel changes nothing", !menu.visible && testInput.fileHash(kept.path) === kept.changed && names(kept.line) === kept.changedNames)
                click(centre(named("revertButton")))
                return 500
            },
            () => {
                click(menuRow("Revert All Changes"))
                return 700
            },
            () => {
                const undo = named("revertUndoButton")
                check("Revert All Changes makes the song's file what it was when it was opened here, byte for byte", testInput.fileHash(kept.path) === kept.hash
                      && names(kept.line) === kept.was && editScreen.editor.key === "E", names(kept.line) + " in " + editScreen.editor.key)
                check("the song starts afresh from it: nothing to undo, and nothing to revert", !editScreen.editor.canUndo && !editScreen.editor.canRevert && !named("revertButton").enabled)
                check("the line along the bottom says so, and offers to take it back", editScreen.notice === "The song is as it was when it was opened here." && !editScreen.noticeIsError
                      && undo.visible && undo.text === "Undo", editScreen.notice)
                check("the sheet is still up, on the song as it now is", editScreen.mode === "chords" && sheet() !== null && sheet().rows.length > 20)
                testInput.grab("4-reverted")
                click(centre(undo))
                return 700
            },
            () => {
                check("Undo there puts the song back as it was just before", testInput.fileHash(kept.path) === kept.changed && names(kept.line) === kept.changedNames
                      && editScreen.editor.key === "D" && editScreen.notice === "The song is as it was before it was reverted.", names(kept.line) + " / " + editScreen.notice)
                check("and Revert has something to do again", named("revertButton").enabled && !named("revertUndoButton").visible)
                editScreen.revert()
                return 600
            },
            () => {
                check("(reverted again, for what follows)", testInput.fileHash(kept.path) === kept.hash && names(kept.line) === kept.was)
                // The offer to take a revert back goes with the next change.
                sheet().setSpot(kept.line, 9)
                testInput.key(Qt.Key_4, 0, "4")
                return 400
            },
            () => {
                check("the offer to take a revert back goes with the next change made", !editScreen.editor.canTakeBackRevert && !named("revertUndoButton").visible
                      && named("revertButton").enabled)
                check("(undone)", editScreen.editor.undo() === "" && testInput.fileHash(kept.path) === kept.hash)
                // ---- The colour of a chord on the sheet
                const bubble = named("chord:" + kept.line + ":1")
                kept.usual = greenest(bubble, 2, bubble.height / 2 - 2, bubble.width - 4)
                chordSheetColor = "#30e070"
                return 400
            },
            () => {
                const bubble = named("chord:" + kept.line + ":1")
                const now = greenest(bubble, 2, bubble.height / 2 - 2, bubble.width - 4)
                // (The usual colour is a warm one, but the edges of letters are drawn with a little of every colour.)
                check("a colour chosen for chords on the sheet is the colour their names are drawn in", now > 120 && now > kept.usual + 80, "green over red: " + now + ", it was " + kept.usual)
                testInput.grab("5-sheet-colour")
                chordSheetColor = ""
                // ---- A name that is no chord, as some files have one
                const block = editScreen.editor.song().find(b => b.row === sheet().rows[kept.line].row)
                kept.heraldRow = block.row
                kept.heraldElement = block.element
                check("(a name that is no chord is put in the file)", editScreen.editor.setChords(block.row, block.element, block.chords.concat([{ at: 9, name: "N.C." }])) === "")
                kept.song = JSON.stringify(editScreen.editor.song())
                click(centre(named("chordProMode")))
                return 800
            },
            () => {
                const text = area()
                const pro = named("chordProEditor")
                check("ChordPro shows the song as text, the name that is no chord with the rest", editScreen.mode === "chordpro" && text !== null
                      && proLine(herald).text === "H[E]ark the [N.C.]herald [Asus2]angels sing" && !pro.barred && !text.readOnly, proLine(herald).text)
                check("the song's keys are over it too, and the words about saving under it", named("chordKeyBox").visible && named("chordKeyBox").currentText === "E"
                      && named("chordShownKeyBox").visible && named("autosaveNote").visible && named("revertButton").visible)
                // ---- The size of the text
                check("the text is at its usual size, which is its own and not the sheet's", pro.zoom === 1 && chordTextZoom === 1 && text.font.pixelSize === 16 && chordSheetZoom === 0.8,
                      text.font.pixelSize)
                kept.text = text.text
                text.forceActiveFocus()
                text.cursorPosition = 3
                testInput.key(Qt.Key_Minus, Qt.ControlModifier, "-")
                check("Ctrl with minus makes it a step smaller: its size and nothing else", pro.zoom === 0.9 && chordTextZoom === 0.9 && text.font.pixelSize === 14 && text.text === kept.text
                      && text.cursorPosition === 3 && chordSheetZoom === 0.8, pro.zoom + " at " + text.font.pixelSize)
                click(centre(named("chordProLarger")))
                click(centre(named("chordProLarger")))
                check("the larger of the two letters A over the text makes it a step larger each time", pro.zoom === 1.1 && text.font.pixelSize === 18, pro.zoom + " at " + text.font.pixelSize)
                click(centre(named("chordProSmaller")))
                testInput.key(Qt.Key_Equal, Qt.ControlModifier, "=")
                testInput.key(Qt.Key_0, Qt.ControlModifier, "0")
                check("the smaller a step smaller, and Ctrl+0 is the usual size", pro.zoom === 1 && text.font.pixelSize === 16, pro.zoom)
                chordTextZoom = 1.5
                check("at the largest the larger is greyed", pro.zoom === 1.5 && named("chordProLarger").available === false && named("chordProSmaller").available === true)
                testInput.grab("6-chordpro-largest")
                chordTextZoom = 1
                // ---- The colour of a chord in the text
                const at = text.positionToRectangle(proLine(herald).start + 1)
                kept.usual = greenest(text, at.x, at.y + at.height / 2, 30)
                chordTextColor = "#30e070"
                return 400
            },
            () => {
                const text = area()
                const at = text.positionToRectangle(proLine(herald).start + 1)
                const now = greenest(text, at.x, at.y + at.height / 2, 30)
                check("a colour chosen for chords in the text is the colour they are drawn in there", now > 120 && now > kept.usual + 80, "green over red: " + now + ", it was " + kept.usual)
                testInput.grab("7-chordpro-colour")
                chordTextColor = ""
                // ---- Typing where a name is no chord, and what a saving writes
                kept.before = testInput.fileHash(kept.path)
                // A chord typed in another text box: "Born to raise the sons of earth".
                const line = proLine(born)
                kept.bornWas = chordsOnDisk(born)
                typeAt(line.start + line.text.indexOf("to raise"), "[")
                testInput.type("G")
                return 1300
            },
            () => {
                const line = proLine(born)
                check("with a name in the song that is no chord, the text can still be typed in", line.text.includes("[G]to raise"), line.text)
                check("what was typed is saved to its own text box a moment later", chordsOnDisk(born).includes("5:G") && chordsOnDisk(born) !== kept.bornWas, chordsOnDisk(born))
                check("and the text box with the name that is no chord, which was not typed in, is not written: the name is still there",
                      boxOnDisk(kept.heraldRow, kept.heraldElement).chords.includes("9:N.C."), boxOnDisk(kept.heraldRow, kept.heraldElement).chords)
                // On that name's own line, too.
                const own = proLine(herald)
                typeAt(own.start + own.text.indexOf("sing"), "[")
                testInput.type("B")
                return 1300
            },
            () => {
                check("a chord typed on the very line that has it is saved with it, the name kept as it is", boxOnDisk(kept.heraldRow, kept.heraldElement).chords.startsWith("1:E 9:N.C. 16:Asus2 23:B"),
                      boxOnDisk(kept.heraldRow, kept.heraldElement).chords)
                check("(both undone, a step each)", editScreen.editor.undo() === "" && editScreen.editor.undo() === "" && JSON.stringify(editScreen.editor.song()) === kept.song)
                return 400
            },
            () => {
                const text = area()
                check("a change made from elsewhere, an undo from the toolbar, shows in the text", !proLine(born).text.includes("[G]") && !proLine(herald).text.includes("[B]")
                      && proLine(herald).text.includes("[N.C.]"), proLine(born).text + " / " + proLine(herald).text)
                // ---- One saving, one step to undo: two text boxes typed in within one pause
                let line = proLine(born)
                typeAt(line.start + line.text.indexOf("sons"), "[")
                testInput.type("A")
                line = proLine(peace)
                typeAt(line.start + line.text.indexOf("earth"), "[")
                testInput.type("D")
                return 1300
            },
            () => {
                check("two text boxes typed in within one pause are both saved", chordsOnDisk(born).includes(":A") && chordsOnDisk(peace).includes("9:D"), chordsOnDisk(born) + " / " + chordsOnDisk(peace))
                check("and are one step to undo", editScreen.editor.undo() === "" && JSON.stringify(editScreen.editor.song()) === kept.song)
                return 400
            },
            () => {
                // ---- A line of chords alone with the words, in one saving
                const pro = named("chordProEditor")
                const text = area()
                const lines = text.text.split("\n")
                kept.intro = pro.layout.findIndex(l => l.kind === "line" && l.alone && lines[pro.layout.indexOf(l)].includes("["))
                kept.introStart = lines.slice(0, kept.intro).join("\n").length + (kept.intro > 0 ? 1 : 0)
                kept.introText = lines[kept.intro]
                kept.introRow = pro.layout[kept.intro].row
                kept.introElement = pro.blocks[pro.layout[kept.intro].block].element
                kept.introHad = (kept.introText.match(/\[/g) ?? []).length
                check("(the intro's line of chords alone is a line of the text, its chords with spaces between)", kept.intro >= 0 && kept.introHad >= 3, kept.introText)
                typeAt(kept.introStart + kept.introText.length, " [")
                testInput.type("A")
                const line = proLine(born)
                typeAt(line.start + line.text.indexOf("sons"), "[")
                testInput.type("E")
                return 1500
            },
            () => {
                const box = boxOnDisk(kept.introRow, kept.introElement)
                check("a chord more on a line of chords alone and a chord on a line of words, typed within one pause, are both saved by it, without waiting for another",
                      box.chords.split(" ").length === kept.introHad + 1 && box.chords.endsWith(":A") && box.text.length === 2 * kept.introHad + 1 && chordsOnDisk(born).includes(":E"),
                      box.chords + " over " + box.text.length + " characters / " + chordsOnDisk(born))
                check("and are one step to undo, though one of them changed the words its chords hang on", editScreen.editor.undo() === ""
                      && JSON.stringify(editScreen.editor.song()) === kept.song, boxOnDisk(kept.introRow, kept.introElement).chords)
                return 400
            },
            () => {
                // ---- A line of chords alone, emptied
                const text = area()
                check("(the text shows the intro as it was again)", text.text.split("\n")[kept.intro] === kept.introText, text.text.split("\n")[kept.intro])
                text.forceActiveFocus()
                text.select(kept.introStart, kept.introStart + kept.introText.length)
                testInput.key(Qt.Key_Delete)
                return 1300
            },
            () => {
                const text = area()
                const pro = named("chordProEditor")
                const box = boxOnDisk(kept.introRow, kept.introElement)
                check("with every chord of a line of chords alone taken out of the text, the line is an empty one, and still a line of chords alone",
                      text.text.split("\n")[kept.intro] === "" && pro.layout[kept.intro].kind === "line" && pro.layout[kept.intro].alone, "'" + text.text.split("\n")[kept.intro] + "'")
                check("in the file it is left one zero-width space, to hang chords on again", box.text === zeroWidth && box.chords === "", units(box.text) + " " + box.chords)
                typeAt(kept.introStart, "[")
                testInput.type("G")
                return 1300
            },
            () => {
                const box = boxOnDisk(kept.introRow, kept.introElement)
                check("a chord typed on it again is saved as a chord alone, on that character", area().text.split("\n")[kept.intro] === "[G]" && box.text === zeroWidth && box.chords === "0:G",
                      box.chords + " over " + units(box.text))
                testInput.grab("8-chordpro")
                // Left for after_chords3 to find a copy of: the song as it now is, which Revert is to keep before it puts
                // the opened one back.
                kept.left = testInput.fileHash(kept.path)
                Log.note("test", "the song was left as " + kept.left)
                editScreen.revert()
                return 700
            },
            () => {
                check("Revert from the ChordPro text makes the file what it was when it was opened, and the text with it", testInput.fileHash(kept.path) === kept.hash
                      && editScreen.mode === "chordpro" && area().text.split("\n")[kept.intro] === kept.introText && !proLine(herald).text.includes("N.C."),
                      area().text.split("\n")[kept.intro])
                stopEditing()
                return 500
            },
            () => {
                // ---- A window too narrow for all of the toolbar
                startEditing(entry("Hark2"))
                return 600
            },
            () => {
                kept.windowWidth = win.width
                win.width = win.minimumWidth
                return 600
            },
            () => {
                const modes = named("editorModes")
                const end = modes.mapToItem(editScreen, modes.width, 0).x
                const inspector = editScreen.inspector.mapToItem(editScreen, 0, 0).x
                check("in a window as narrow as it goes, the switch is drawn without its pictures, and keeps clear of the inspector beside the slides", win.width === win.minimumWidth
                      && modes.compact && modes.width < modes.widest && end <= inspector && named("chordsMode").visible, "it ends at " + end + ", the inspector starts at " + inspector)
                testInput.grab("9a-narrow-slides")
                click(centre(named("chordsMode")))
                return 700
            },
            () => {
                const modes = named("editorModes")
                const key = named("chordKeyBox")
                const end = modes.mapToItem(editScreen, modes.width, 0).x
                check("over the chord editors there, the switch has its pictures again and is all in the window, after the song's two keys", editScreen.mode === "chords" && !modes.compact
                      && end <= editScreen.width && key.visible && named("chordShownKeyBox").visible
                      && named("chordShownKeyBox").mapToItem(editScreen, 82, 0).x <= modes.mapToItem(editScreen, 0, 0).x, "it ends at " + end + " of " + editScreen.width)
                check("the sheet has laid its slides out in what room there is", sheet().columns >= 1 && sheet().cells.length > 20, sheet().columns + " to a row")
                testInput.grab("9b-narrow-chords")
                win.width = kept.windowWidth
                stopEditing()
                return 600
            },
            () => {
                // ---- The settings: the colour of a chord in each of the two editors
                settingsOpen = true
                kept.settings = Lib.find(win.contentItem, item => item.chordSheetColorEdited !== undefined)
                kept.settings.section = "chords"
                return 500
            },
            () => {
                const part = named("chordSettings")
                const swatch = named("chordColour:On the chord sheet")
                const usual = named("usualColour:On the chord sheet")
                const sample = named("chordSample:On the chord sheet")
                check("the settings have a section for chords, with the colour of a chord in each of the two editors", kept.settings.sections.some(s => s.name === "Chords")
                      && part.visible && swatch.visible && named("chordColour:In ChordPro text").visible, kept.settings.sections.map(s => s.name).join(" "))
                check("each is the colour it comes with, with nothing to go back to", chordSheetColor === "" && chordTextColor === "" && String(swatch.value) === "#ff8a1f"
                      && String(named("chordColour:In ChordPro text").value) === "#ffb45e" && !usual.enabled && !named("usualColour:In ChordPro text").enabled,
                      String(swatch.value))
                kept.usual = greenest(sample, 14, 20, 40)
                // (As the picker says a colour has been settled on.)
                swatch.picked("#30e070")
                named("chordColour:In ChordPro text").picked("#40c0ff")
                return 400
            },
            () => {
                const sample = named("chordSample:On the chord sheet")
                const now = greenest(sample, 14, 20, 40)
                check("a colour picked is kept as the app's own, and the sample beside it is drawn in it", chordSheetColor === "#30e070" && chordTextColor === "#40c0ff"
                      && now > 120 && now > kept.usual + 80, chordSheetColor + " " + chordTextColor + ", green over red " + now + ", it was " + kept.usual)
                testInput.grab("9-settings-chords")
                const usual = named("usualColour:On the chord sheet")
                check("Usual Colour can then be pressed", usual.enabled && named("usualColour:In ChordPro text").enabled)
                click(centre(usual))
                click(centre(named("usualColour:In ChordPro text")))
                return 400
            },
            () => {
                check("and puts back the colour each came with", chordSheetColor === "" && chordTextColor === "" && String(named("chordColour:On the chord sheet").value) === "#ff8a1f"
                      && !named("usualColour:On the chord sheet").enabled, chordSheetColor + " " + chordTextColor)
                check("what is no colour is kept as none", chordColorKept("orange") === "" && chordColorKept("#12345") === "" && chordColorKept("#AABBCC") === "#aabbcc")
                kept.settings.section = "groups"
                settingsOpen = false
                return 400
            },
            () => {
                // ---- A song that names no key
                const made = catalog.createPresentation(libraryPath, "Keyless")
                check("(a presentation is made that names no key)", made.error === "" && catalog.open(made.path).originalKey === "", made.error)
                kept.keyless = made.path
                openLibrary(libraryPath)
                startEditing(entry("Keyless"))
                return 700
            },
            () => {
                click(centre(named("chordsMode")))
                return 700
            },
            () => {
                const key = named("chordKeyBox")
                const shown = named("chordShownKeyBox")
                check("a song that names no key has None for its key, and the key it is shown in cannot be set until that is", editScreen.editor.key === "" && key.currentText === "None"
                      && shown.currentText === "None" && !shown.enabled, key.currentText + " " + shown.currentText)
                const refused = editScreen.editor.setShownKey("G")
                check("asked for all the same, it is refused in a sentence, and nothing is written", refused.startsWith("This song does not say what key it is in.")
                      && catalog.open(kept.keyless).userKey === "", refused)
                pick(key, "None")
                check("None picked is nothing picked", editScreen.editor.key === "" && !editScreen.editor.canUndo)
                pick(key, "G")
                return 400
            },
            () => {
                const opened = catalog.open(kept.keyless)
                check("its key picked, the key it is shown in goes with it: nobody had chosen another", editScreen.editor.key === "G" && editScreen.editor.shownKey === "G"
                      && opened.originalKey === "G" && opened.userKey === "G" && named("chordShownKeyBox").enabled && named("chordShownKeyBox").currentText === "G", opened.originalKey + " " + opened.userKey)
                pick(named("chordKeyBox"), "Am")
                return 400
            },
            () => {
                const opened = catalog.open(kept.keyless)
                check("and goes with it again while the two are the same", opened.originalKey === "Am" && opened.userKey === "Am", opened.originalKey + " " + opened.userKey)
                stopEditing()
                return 500
            }
        ]
        next()
    }
}
