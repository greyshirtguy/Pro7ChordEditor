// The chord sheet's model, held to testdata/vectors/sheet-model.json and layout.json
// (spec/03-chord-sheet.md, "What to hold your build to": every case of both).
//
// Each test runs EVERY case of its part of the file, and says of a failure which case
// it was and what was got. No case is copied into a test: the file is read. Tests
// marked "Mine, not a vector" are for what the vectors do not hold: what goes by the
// screen, the keys in the typing box, and the three places this departs from the
// reference on purpose (Sources/Song/ChordSheet.swift says which).

import ChordRules
import Foundation
import Song
import Testing
import TestData

@Suite("The chord sheet's model")
struct ChordSheetTests {
    let vectors: SheetVectors

    init() throws {
        vectors = try SheetVectors.read()
    }

    private func spelt(_ row: SheetRow) -> String {
        guard row.kind == .line else {
            return "group \(SongTests.spelt(row.name)) slide \(row.slide)"
        }
        return "line block \(row.block) line \(row.line) slide \(row.slide) box \(SongTests.spelt(row.element)) \(SongTests.spelt(row.text.string)) "
            + "start \(row.start) \(SongTests.spelt(row.chords)) alone \(row.alone) blank \(row.blank) first \(row.first) count \(row.count) last \(row.last)"
    }

    private func spelt(_ row: SheetVectors.ARow) -> String {
        guard row.kind == "line" else {
            return "group \(SongTests.spelt(row.name ?? "")) slide \(row.row)"
        }
        return "line block \(row.block ?? -1) line \(row.line ?? -1) slide \(row.row) box \(SongTests.spelt(row.element ?? "?")) \(SongTests.spelt(row.text ?? "?")) "
            + "start \(row.start ?? -1) \(SongTests.spelt((row.chords ?? []).map(\.chord))) alone \(row.alone ?? false) blank \(row.blank ?? false) "
            + "first \(row.first ?? false) count \(row.count ?? -1) last \(row.last ?? false)"
    }

    @Test("rows: a row for a group's name, and one for each line of each text box")
    func rows() throws {
        #expect(vectors.rows.count == vectors.songs.count)
        for expected in vectors.rows {
            let (sheet, _) = try vectors.sheet(expected.song)
            #expect(sheet.rows.map(spelt) == expected.rows.map(spelt), "\(expected.song)")
            #expect(sheet.mostLines == expected.mostLines, "\(expected.song)")
            #expect(sheet.used.map { SongTests.spelt($0) } == expected.used.map { SongTests.spelt($0) }, "\(expected.song)")
        }
    }

    @Test("wordStarts: where the words of a line start")
    func wordStarts() {
        #expect(!vectors.wordStarts.isEmpty)
        for expected in vectors.wordStarts {
            #expect(ChordSheet.wordStarts(Words(expected.text)) == expected.out, "\(SongTests.spelt(expected.text))")
        }
    }

    @Test("stops: the places of each row that the spot stops at, and whether the row can take it")
    func stops() throws {
        #expect(!vectors.stops.isEmpty)
        for expected in vectors.stops {
            let (sheet, _) = try vectors.sheet(expected.song)
            #expect(sheet.usable(expected.row) == expected.usable, "\(expected.song) row \(expected.row)")
            #expect((sheet.usable(expected.row) ? sheet.stops(expected.row) : []) == expected.out, "\(expected.song) row \(expected.row)")
        }
    }

    @Test("stopAt: the stop at or before a character")
    func stopAt() throws {
        #expect(!vectors.stopAt.isEmpty)
        for expected in vectors.stopAt {
            let (sheet, _) = try vectors.sheet(expected.song)
            #expect(sheet.stopAt(expected.row, expected.at) == expected.out, "\(expected.song) row \(expected.row) at \(expected.at)")
        }
    }

    @Test("walks: where Right and Left take the spot, a word at a time and with Shift a letter at a time")
    func walks() throws {
        #expect(!vectors.walks.isEmpty)
        for (number, expected) in vectors.walks.enumerated() {
            let (sheet, song) = try vectors.sheet(expected.song)
            if let start = expected.start {
                sheet.setSpot(start.row, start.at)
            }
            var spots: [Spot] = []
            for key in expected.keys {
                sheet.step(key.contains("Left") ? -1 : 1, exact: key.hasPrefix("Shift"))
                spots.append(sheet.spot)
            }
            #expect(spots == expected.spots.map(\.spot), "walk \(number) on \(expected.song): \(expected.keys)")
            #expect(song.asked.isEmpty)
        }
    }

    /// Does what one case of `edits` says was done, as the script that made it did.
    private func run(_ what: SheetVectors.Edit.What, at spot: Spot, on sheet: ChordSheet) throws -> Bool? {
        if let name = what.put {
            if let row = what.onRow {
                return sheet.put(row, 0, name)
            }
            guard let then = what.then else {
                return sheet.put(sheet.spot.row, sheet.spot.at, name)
            }
            guard sheet.put(spot.row, spot.at, name) else { return false }
            return follow(then, at: spot, on: sheet)
        }
        if what.take == true {
            guard let then = what.then else {
                return sheet.take(sheet.spot.row, sheet.spot.at)
            }
            guard sheet.take(spot.row, spot.at) else { return false }
            return follow(then, at: spot, on: sheet)
        }
        if let number = what.key {
            return sheet.put(sheet.spot.row, sheet.spot.at, sheet.inKey[number - 1])
        }
        if let to = what.move {
            sheet.move(from: spot, to: to.spot, copy: false)
            return nil
        }
        let to = try #require(what.copy, "a case of edits that says nothing this test knows how to do")
        sheet.move(from: spot, to: to.spot, copy: true)
        return nil
    }

    /// What a case says was done next, and after that: each at the case's own spot,
    /// or at the place of its row that the step names; and each only if the one
    /// before it was done, as the script that made the case has them (`a && b && c`).
    private func follow(_ step: SheetVectors.Edit.What.Then, at spot: Spot, on sheet: ChordSheet) -> Bool {
        let at = step.at ?? spot.at
        let done = step.put.map { sheet.put(spot.row, at, $0) } ?? sheet.take(spot.row, at)
        guard done, let next = step.then else { return done }
        return follow(next, at: spot, on: sheet)
    }

    @Test("edits: for each thing done at the spot, what the song is asked, where the spot is left, and the song afterwards")
    func edits() throws {
        #expect(!vectors.edits.isEmpty)
        for (number, expected) in vectors.edits.enumerated() {
            let which = "edit \(number) on \(expected.song): \(expected.what.note ?? "")"
            let (sheet, song) = try vectors.sheet(expected.song)
            #expect(sheet.usable(expected.spot.row), "\(which)")
            sheet.setSpot(expected.spot.row, expected.spot.at)
            let answer = try run(expected.what, at: expected.spot.spot, on: sheet)
            #expect(song.asked == expected.asked.map(\.spelt), "\(which)")
            #expect(sheet.spot == expected.spotAfter.spot, "\(which)")
            #expect(song.after == expected.songAfter.map(\.spelt), "\(which)")
            #expect(answer == expected.answer, "\(which)")
        }
    }

    @Test("typing: the bubble opened, and what was typed kept or thrown away")
    func typing() throws {
        #expect(!vectors.typing.isEmpty)
        for expected in vectors.typing {
            let (sheet, song) = try vectors.sheet(expected.song)
            for (number, step) in expected.steps.enumerated() {
                let which = "\(expected.title), step \(number)"
                song.asked = []
                let what = step.step
                if let spot = what.spot {
                    #expect(sheet.usable(spot.row), "\(which)")
                    sheet.setSpot(spot.row, spot.at)
                }
                if let press = what.press {
                    sheet.startTyping(seed: press == "Enter" ? nil : press)
                }
                if let typed = what.typed {
                    sheet.type(typed)
                }
                if let finish = what.finish {
                    sheet.finishTyping(finish == "Esc" ? nil : sheet.taken(), onward: finish == "Tab")
                }
                if let click = what.click {
                    #expect(sheet.usable(click.row), "\(which)")
                    sheet.pick(click.row, click.at)
                }
                if what.settle == true {
                    sheet.settle()
                }
                #expect(song.asked == step.asked.map(\.spelt), "\(which)")
                #expect(sheet.typing == step.typing, "\(which)")
                #expect((sheet.typing ? sheet.typingAt : nil) == step.typingAt?.spot, "\(which)")
                #expect((sheet.typing ? SongTests.spelt(sheet.entry.begun) : nil) == step.boxHolds.map { SongTests.spelt($0.text) }, "\(which)")
                #expect((sheet.typing ? sheet.entry.selected : nil) == step.boxHolds?.selected, "\(which)")
                #expect(sheet.spot == step.spot.spot, "\(which)")
            }
            #expect(song.after == expected.songAfter.map(\.spelt), "\(expected.title)")
        }
    }

    private func spelt(_ line: ChordSheet.CopiedLine) -> String {
        switch line {
        case .words(let chords): "words [" + chords.map { "\(SongTests.spelt($0.name)) word \($0.word)+\($0.offset)" }.joined(separator: ", ") + "]"
        case .alone(let names): "alone [" + names.map { SongTests.spelt($0) }.joined(separator: ", ") + "]"
        }
    }

    @Test("copyPaste: a line's chords, or a group's, copied and put on other lines")
    func copyPaste() throws {
        #expect(!vectors.copyPaste.isEmpty)
        for expected in vectors.copyPaste {
            let (sheet, song) = try vectors.sheet(expected.song)
            sheet.setSpot(expected.copyAt.row, expected.copyAt.at)
            let said = sheet.copy(whole: expected.wholeGroup)
            #expect((sheet.copied ?? []).map(spelt) == expected.copied.lines.map(\.spelt), "\(expected.title)")
            #expect([said].compactMap { $0 } == expected.said, "\(expected.title)")
            sheet.setSpot(expected.pasteAt.row, expected.pasteAt.at)
            song.asked = []
            let pasted = sheet.paste()
            #expect(song.asked == expected.asked.map(\.spelt), "\(expected.title)")
            #expect(song.after == expected.songAfter.map(\.spelt), "\(expected.title)")
            // What the paste answers with: how many lines and chords, how many left
            // out, passed over, refused. (What is said of it is the window's model's
            // to say, and is held to these same cases there: ChordSheetViewTests.)
            #expect(pasted == expected.pasted.did.pasted, "\(expected.title)")
        }
    }

    // ---- layout.json

    struct LayoutVectors: Decodable {
        struct Metrics: Decodable {
            let wordLetter: Double
            let chordLetter: Double
            let bubblePadEachSide: Double
            let gap: Double
        }

        struct Place: Decodable {
            let text: String
            let chords: [SheetVectors.AChord]
            let xs: [Double]
            let widths: [Double]
            let width: Double
            let wordsWidth: Double
        }

        struct CharacterAt: Decodable {
            let text: String
            let x: Double
            let out: Int
        }

        let metrics: Metrics
        let place: [Place]
        let characterAt: [CharacterAt]
    }

    @Test("layout.json: where the bubbles of a line stand, and which character is at a distance along it")
    func layout() throws {
        let layout = try JSONDecoder().decode(LayoutVectors.self, from: TestData.vector("layout.json"))
        let metrics = layout.metrics
        #expect(metrics.gap == ChordSheet.gap)
        #expect(!layout.place.isEmpty && !layout.characterAt.isEmpty)
        for expected in layout.place {
            let placed = ChordLayout.place(length: expected.text.utf16.count, chords: expected.chords.map(\.chord),
                                           advance: { metrics.wordLetter * Double($0) },
                                           bubble: { metrics.chordLetter * Double($0.utf16.count) + 2 * metrics.bubblePadEachSide },
                                           gap: metrics.gap)
            let which = SongTests.spelt(expected.text)
            #expect(placed.xs == expected.xs, "\(which)")
            #expect(placed.widths == expected.widths, "\(which)")
            #expect(placed.width == expected.width, "\(which)")
            #expect(placed.wordsWidth == expected.wordsWidth, "\(which)")
        }
        for expected in layout.characterAt {
            let got = ChordLayout.characterAt(length: expected.text.utf16.count, x: expected.x) { metrics.wordLetter * Double($0) }
            #expect(got == expected.out, "\(SongTests.spelt(expected.text)) at \(expected.x)")
        }
    }

    // ---- Mine, not a vector

    @Test("Mine, not a vector. Down and Up go to the stop nearest above or below on the screen (rule 20)")
    func upAndDown() throws {
        let (sheet, _) = try vectors.sheet("grace")
        let widths = FixedWidths()
        // With no spot yet, the first arrow key puts it on the first word of the song.
        sheet.stepLine(1, widths)
        #expect(sheet.spot == Spot(row: 1, at: 0))
        // "sweet" starts at 18 of "Amazing grace how sweet the sound": 180 along. Under
        // it, of "That saved a wretch like me", "like" at 20 (200) is nearer than
        // "wretch" at 13 (130).
        sheet.setSpot(1, 18)
        sheet.stepLine(1, widths)
        #expect(sheet.spot == Spot(row: 2, at: 20))
        // A chord's bubble is where its place is, if it has been pushed along.
        sheet.stepLine(-1, widths)
        #expect(sheet.spot == Spot(row: 1, at: 18))
        // The name of a group is passed over, and the last line stays where it is.
        sheet.setSpot(4, 0)
        sheet.stepLine(1, widths)
        #expect(sheet.spot == Spot(row: 6, at: 0))
        #expect(sheet.picked == 2)
        sheet.setSpot(9, 5)
        sheet.stepLine(1, widths)
        #expect(sheet.spot == Spot(row: 9, at: 5))
    }

    @Test("Mine, not a vector. The place under the pointer: the character, or with Shift the word; over a space, the word after (rule 16)")
    func underThePointer() throws {
        let (sheet, _) = try vectors.sheet("grace")
        let widths = FixedWidths()
        // "Amazing grace how sweet the sound": each letter ten wide.
        #expect(sheet.placeAt(1, along: 0, exact: true, widths) == 0)
        #expect(sheet.placeAt(1, along: 34, exact: true, widths) == 3)
        #expect(sheet.placeAt(1, along: 34, exact: false, widths) == 0)
        // 75 along is the space after "Amazing": the word after is meant.
        #expect(sheet.placeAt(1, along: 75, exact: true, widths) == 8)
        #expect(sheet.placeAt(1, along: 75, exact: false, widths) == 0)
        #expect(sheet.placeAt(1, along: 5000, exact: true, widths) == 32)
        #expect(sheet.placeAt(1, along: -40, exact: true, widths) == 0)
        // Two spaces between words ("Halle  lujah"): only the second is the word after.
        let (odd, _) = try vectors.sheet("odd")
        #expect(odd.placeAt(0, along: 55, exact: true, widths) == 5)
        #expect(odd.placeAt(0, along: 65, exact: true, widths) == 7)

        // A line of chords alone: each chord, and past the last, one more.
        let (intro, _) = try vectors.sheet("intro")
        let placed = intro.placed(1, widths)
        #expect(placed.xs == [0, 26, 52])
        #expect(intro.placeAt(1, along: 3, exact: true, widths) == 0)
        #expect(intro.placeAt(1, along: 30, exact: true, widths) == 2)
        // (A bubble is pointed at from a little past its right edge too: 5.)
        #expect(intro.placeAt(1, along: 50, exact: true, widths) == 2)
        #expect(intro.placeAt(1, along: 54, exact: true, widths) == 4)
        #expect(intro.placeAt(1, along: 60, exact: false, widths) == 4)
        #expect(intro.placeAt(1, along: 300, exact: true, widths) == 6)
        #expect(intro.xOf(1, 2, widths) == 26)
        #expect(intro.xOf(1, 6, widths) == 52 + 22 + 10)
        // A blank slide's line has the one place.
        let (blank, _) = try vectors.sheet("blank")
        #expect(blank.placeAt(1, along: 200, exact: true, widths) == 0)
        #expect(blank.xOf(1, 0, widths) == 0)
    }

    @Test("Mine, not a vector. A chord belongs to the line its place is in, up to but not including its line break; an empty line is one place wide (rule 3)")
    func whichLineAChordIsOn() {
        // "ab", an empty line, "cd": places 0 1 (2 the break) (3 the empty line, and its
        // break) 4 5.
        let boxes = [TextBox(slide: 0, element: "A", words: "ab\n\ncd",
                             chords: [Chord(at: 1, name: "G"), Chord(at: 2, name: "X"), Chord(at: 3, name: "D"), Chord(at: 4, name: "C")])]
        let sheet = ChordSheet(song: StandInSong(boxes, key: "C"))
        #expect(sheet.rows.map(\.chords) == [[Chord(at: 1, name: "G")], [Chord(at: 0, name: "D")], [Chord(at: 0, name: "C")]])
        #expect(sheet.rows.map(\.start) == [0, 3, 4])
        // The empty line cannot take the spot, and a change to the first line hands the
        // other lines' chords back as they were, the empty line's with them.
        #expect(!sheet.usable(1))
        sheet.put(0, 0, "Am")
        #expect(sheet.rows[1].chords == [Chord(at: 0, name: "D")] && sheet.rows[2].chords == [Chord(at: 0, name: "C")])
    }

    @Test("Mine, not a vector. The empty lines a text box's words end with get no row, and do not make the song's cards taller; one that a chord stands on is kept")
    func emptyLinesAtTheEnd() {
        // Two lines of words and then three line breaks; and a box of one line.
        let boxes = [
            TextBox(slide: 0, element: "A", words: "ab\ncd\n\n\n", chords: [Chord(at: 0, name: "G"), Chord(at: 3, name: "D")]),
            TextBox(slide: 1, element: "B", words: "ef\n\ngh\n", chords: []),
            TextBox(slide: 2, element: "C", words: "ij"),
        ]
        let song = StandInSong(boxes, key: "C")
        let sheet = ChordSheet(song: song)
        #expect(sheet.rows.map(\.text.string) == ["ab", "cd", "ef", "", "gh", "ij"])
        // (An empty line in the middle of a box's words is a row still.)
        #expect(sheet.rows.map(\.count) == [2, 2, 3, 3, 3, 1])
        #expect(sheet.mostLines == 3)
        #expect(sheet.rows.map(\.last) == [false, true, false, false, true, true])
        // Nothing of the words is changed by it, and a change to a line still hands
        // every other chord of the box back.
        sheet.put(1, 1, "Am")
        #expect(song.asked == ["setChords slide 0 box \"A\" [\"G\"@0, \"D\"@3, \"Am\"@4] joined false"])
        #expect(song.textBoxes[0].words == "ab\ncd\n\n\n")

        // A chord on the second of three empty lines at the end: that line and the one
        // before it are rows, the one after it is not.
        let held = [TextBox(slide: 0, element: "A", words: "ab\n\n\n", chords: [Chord(at: 4, name: "C")])]
        let kept = ChordSheet(song: StandInSong(held, key: "C"))
        #expect(kept.rows.map(\.text.string) == ["ab", "", ""])
        #expect(kept.rows.map(\.chords) == [[], [], [Chord(at: 0, name: "C")]])
        // And a box always has one line, though its words be one line break.
        let bare = ChordSheet(song: StandInSong([TextBox(slide: 0, element: "A", words: "\n")], key: "C"))
        #expect(bare.rows.count == 1)
    }

    @Test("Mine, not a vector. A group with no name has no row; a chord of a line of chords alone can be moved to its front; two pasted chords that land on one place leave the first")
    func threeSmallThings() throws {
        let boxes = [
            TextBox(slide: 0, group: "", startsItsGroup: true, element: "A", words: Chords.placeholders(3),
                    chords: [Chord(at: 0, name: "D"), Chord(at: 2, name: "G"), Chord(at: 4, name: "A")]),
            TextBox(slide: 1, group: "", element: "B", words: "Amazing grace\nI am", chords: [Chord(at: 0, name: "G"), Chord(at: 3, name: "D"), Chord(at: 8, name: "C")]),
        ]
        let song = StandInSong(boxes, key: "D")
        let sheet = ChordSheet(song: song)
        #expect(sheet.rows.map(\.kind) == [.line, .line, .line])
        // The last of three, to the front: it changes places, and the rest close up.
        sheet.move(from: Spot(row: 0, at: 4), to: Spot(row: 0, at: 0), copy: false)
        #expect(sheet.rows[0].chords.map(\.name) == ["A", "D", "G"])
        // "Amazing" has a chord at its start and one three letters in; "I" has one
        // letter, so both would land on it: the first is the one that is put.
        song.asked = []
        sheet.setSpot(1, 0)
        sheet.copy(whole: false)
        sheet.setSpot(2, 0)
        sheet.paste()
        #expect(song.asked == ["setChords slide 1 box \"B\" [\"G\"@0, \"D\"@3, \"C\"@8, \"G\"@14, \"C\"@16] joined false"])
    }

    @Test("Mine, not a vector. Drawn twice the size, the gaps the sheet keeps along a line are twice theirs")
    func twiceTheSize() throws {
        struct Twice: SheetMeasure {
            var scale: Double { 2 }
            func advance(row: Int, units: Int) -> Double { 20 * Double(units) }
            func bubble(_ name: String) -> Double { 16 * Double(name.utf16.count) + 28 }
        }
        let (intro, _) = try vectors.sheet("intro")
        // Three chords on a line of chords alone, each one letter: 44 wide, and 8 apart
        // where at the usual size they are 22 wide and 4 apart.
        #expect(intro.placed(1, Twice()).xs == [0, 52, 104])
        #expect(intro.placed(1, FixedWidths()).xs == [0, 26, 52])
        // The place for a new one is 20 past the last, where it was 10; and a bubble
        // is pointed at from 10 past its right edge, where it was 5.
        #expect(intro.xOf(1, 6, Twice()) == 104 + 44 + 20)
        #expect(intro.placeAt(1, along: 105, exact: true, Twice()) == 2)
        #expect(intro.placeAt(1, along: 107, exact: true, Twice()) == 4)
    }

    @Test("Mine, not a vector. A place is never the second half of a character that takes two units")
    func neverHalfACharacter() {
        // "a", a musical sharp sign from beyond the first 65,536 (two units), "b".
        let words = Words(units: [0x61, 0xD834, 0xDD30, 0x62])
        let song = StandInSong([TextBox(slide: 0, element: "A", words: words)], key: "C")
        let sheet = ChordSheet(song: song)
        sheet.setSpot(0, 0)
        sheet.step(1, exact: true)
        #expect(sheet.spot.at == 1)
        sheet.step(1, exact: true)
        #expect(sheet.spot.at == 3)
        sheet.step(-1, exact: true)
        #expect(sheet.spot.at == 1)
        // (Every unit ten wide, as these letters are measured: 25 along is the second
        // half.)
        #expect(sheet.placeAt(0, along: 25, exact: true, FixedWidths()) == 1)
    }

    @Test("Mine, not a vector. The box a chord is typed in takes only what could be a chord, tidied as it is typed, sixteen at most (rule 30)")
    func whatTheBoxTakes() throws {
        let (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(1, 13)
        sheet.startTyping(seed: "F")
        #expect(sheet.entry.text == "F" && !sheet.entry.selected)
        #expect(sheet.type("f#") == "F#")
        #expect(sheet.type("F#m7/a") == "F#m7/A")
        // What could not be a chord is undone at once.
        #expect(sheet.type("F#m7/A!") == "F#m7/A")
        #expect(sheet.type("F#m7/A_") == "F#m7/A")
        #expect(sheet.type("x") == "F#m7/A")
        #expect(sheet.type("F#m7b5add9sus4/Ab") == "F#m7/A")
        #expect(sheet.type("F#m7b5add9sus/Ab") == "F#m7b5add9sus/Ab")
        #expect(sheet.type("") == "")
        #expect(song.asked.isEmpty)
    }

    @Test("Mine, not a vector. Down and Up go through what is offered, round and round; Right takes the one picked into the box (rule 31)")
    func throughWhatIsOffered() throws {
        let (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(1, 13)
        sheet.startTyping(seed: "E")
        let offered = sheet.offered
        #expect(offered == Array(Chords.completions(typed: "E", key: "G", used: sheet.used).prefix(14)))
        #expect(offered.count > 2 && offered.count <= 14)
        // Right with none picked is the box's own key.
        #expect(!sheet.press(.right))
        #expect(sheet.press(.down) && sheet.entry.pick == 0)
        #expect(sheet.press(.down) && sheet.entry.pick == 1)
        #expect(sheet.press(.up) && sheet.press(.up) && sheet.entry.pick == offered.count - 1)
        #expect(sheet.press(.down) && sheet.entry.pick == 0)
        #expect(sheet.taken() == offered[0])
        // Right with the caret not at the end is the box's own key too.
        #expect(!sheet.press(.right, caretAtEnd: false))
        #expect(sheet.press(.right) && sheet.entry.text == offered[0] && sheet.entry.pick == -1)
        // Up from none picked is the last.
        #expect(sheet.press(.up) && sheet.entry.pick == sheet.offered.count - 1)
        // Typing unpicks.
        sheet.type(sheet.entry.text)
        #expect(sheet.entry.pick == -1)
        #expect(song.asked.isEmpty && sheet.typing)
    }

    @Test("Mine, not a vector. Enter keeps the one picked; Tab takes the first offered if what is typed is no chord yet; Esc changes nothing (rules 32 to 34)")
    func keptAndThrownAway() throws {
        let (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(1, 13)
        sheet.startTyping(seed: "E")
        sheet.press(.down)
        let picked = sheet.offered[0]
        sheet.press(.enter)
        #expect(!sheet.typing && sheet.chordAt(1, 13) == picked)

        // Nothing typed is no chord: Tab takes the first that is offered, and moves on.
        sheet.setSpot(2, 5)
        sheet.startTyping()
        #expect(sheet.entry.text.isEmpty && sheet.entry.selected)
        let first = try #require(sheet.offered.first)
        sheet.press(.tab)
        #expect(sheet.chordAt(2, 5) == first)
        #expect(sheet.spot == Spot(row: 2, at: 11))

        // What is typed is a chord: Tab keeps that, not the first offered.
        sheet.startTyping(seed: "A")
        sheet.press(.tab)
        #expect(sheet.chordAt(2, 11) == "A")

        song.asked = []
        sheet.startTyping(seed: "B")
        sheet.type("Bm7")
        sheet.press(.escape)
        #expect(!sheet.typing && song.asked.isEmpty)
        // And with no chord being typed, a key is nobody's here.
        #expect(!sheet.press(.enter))
    }

    @Test("Mine, not a vector. Backspace in an empty box takes off the chord at that place (rule 35)")
    func emptiedBox() throws {
        let (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(1, 8)
        sheet.startTyping()
        #expect(sheet.entry.text == "G7" && sheet.entry.selected)
        // With something in it, Backspace is the box's own.
        #expect(!sheet.press(.delete))
        sheet.type("")
        #expect(sheet.press(.delete))
        #expect(!sheet.typing && sheet.chordAt(1, 8).isEmpty)
        #expect(song.asked.count == 1)
    }

    @Test("Mine, not a vector. Kept with nothing in the box, the chord that was there goes: by Enter, by a click on another place, by a click on the bare sheet; Esc changes nothing; and where there was no chord, nothing is asked (rule 35a)")
    func keptEmpty() throws {
        // Enter.
        var (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(1, 8)
        sheet.startTyping()
        sheet.type("")
        #expect(sheet.press(.enter))
        #expect(!sheet.typing && sheet.chordAt(1, 8).isEmpty && sheet.chordAt(1, 0) == "G")
        #expect(song.asked.count == 1)

        // A click on another place: the one goes, and the bubble opens on the other.
        (sheet, song) = try vectors.sheet("grace")
        sheet.pick(1, 8)
        sheet.type("")
        sheet.pick(2, 5)
        #expect(sheet.typing && sheet.typingAt == Spot(row: 2, at: 5) && sheet.chordAt(1, 8).isEmpty)
        #expect(song.asked.count == 1)

        // A click on the bare sheet.
        (sheet, song) = try vectors.sheet("grace")
        sheet.pick(1, 8)
        sheet.type("")
        sheet.settle()
        #expect(!sheet.typing && sheet.chordAt(1, 8).isEmpty && song.asked.count == 1)

        // Esc: the chord is as it was, and the song was asked nothing.
        (sheet, song) = try vectors.sheet("grace")
        sheet.pick(1, 8)
        sheet.type("")
        #expect(sheet.press(.escape))
        #expect(!sheet.typing && sheet.chordAt(1, 8) == "G7" && song.asked.isEmpty)

        // Opened and kept with its name untouched: nothing is taken off.
        (sheet, song) = try vectors.sheet("grace")
        sheet.pick(1, 8)
        #expect(sheet.press(.enter))
        #expect(sheet.chordAt(1, 8) == "G7")

        // Where there is no chord, an empty box kept asks the song nothing.
        (sheet, song) = try vectors.sheet("grace")
        sheet.pick(1, 13)
        #expect(sheet.entry.text.isEmpty)
        #expect(sheet.press(.enter))
        #expect(!sheet.typing && song.asked.isEmpty)

        // On a line of chords alone, the one cleared goes and the others close up.
        (sheet, song) = try vectors.sheet("intro")
        let alone = try #require(sheet.rows.firstIndex { $0.kind == .line && $0.alone && $0.chords.count >= 2 })
        let names = sheet.rows[alone].chords.map(\.name)
        sheet.pick(alone, 0)
        sheet.type("")
        #expect(sheet.press(.enter))
        #expect(sheet.rows[alone].chords.map(\.name) == Array(names.dropFirst()))
    }

    @Test("Mine, not a vector. 1 to 7 put the key's chord on the spot; while a chord is typed, where the typing was, in place of what was typed (rules 24 and 39)")
    func oneToSeven() throws {
        let (sheet, _) = try vectors.sheet("grace")
        sheet.setSpot(1, 13)
        sheet.putOfKey(6)
        #expect(sheet.chordAt(1, 13) == "Em")
        sheet.setSpot(2, 5)
        sheet.startTyping(seed: "B")
        sheet.type("Bm7")
        sheet.pointerOver(4, 0)
        sheet.putOfKey(5)
        #expect(!sheet.typing && sheet.chordAt(2, 5) == "D" && sheet.chordAt(4, 0).isEmpty)
        sheet.putOfKey(0)
        sheet.putOfKey(8)
        // A song that states no key is taken to be in C.
        let (odd, _) = try vectors.sheet("odd")
        #expect(odd.key == "C" && odd.inKey == ["C", "Dm", "Em", "F", "G", "Am", "Bdim"])
        odd.setSpot(0, 0)
        odd.putOfKey(4)
        #expect(odd.chordAt(0, 0) == "F")
    }

    @Test("Mine, not a vector. A chord carried: the spot goes with it and not with the pointer; let go, it moves, or is copied; where it started, nothing (rules 40 to 46)")
    func carried() throws {
        let (sheet, song) = try vectors.sheet("grace")
        sheet.lift(1, 8)
        #expect(sheet.dragged?.name == "G7")
        sheet.pointerOver(4, 0)
        #expect(sheet.spot.row == -1)
        sheet.carry(over: 6, 5)
        #expect(sheet.spot == Spot(row: 6, at: 5))
        sheet.drop(copy: false)
        #expect(sheet.dragged == nil && sheet.chordAt(6, 5) == "G7" && sheet.chordAt(1, 8).isEmpty)
        #expect(song.asked.count == 2 && song.asked[1].hasSuffix("joined true"))

        song.asked = []
        sheet.lift(6, 5)
        sheet.carry(over: 7, 4)
        sheet.drop(copy: true)
        #expect(sheet.chordAt(6, 5) == "G7" && sheet.chordAt(7, 4) == "G7" && song.asked.count == 1)

        song.asked = []
        sheet.lift(6, 5)
        sheet.carry(over: 6, 5)
        sheet.drop(copy: false)
        #expect(song.asked.isEmpty)
        // Not while another is being typed; and not where there is none.
        sheet.setSpot(1, 13)
        sheet.startTyping(seed: "E")
        sheet.lift(6, 5)
        #expect(sheet.dragged == nil)
        sheet.press(.escape)
        sheet.lift(1, 13)
        #expect(sheet.dragged == nil)
        sheet.lift(6, 5)
        sheet.dropNowhere()
        #expect(sheet.dragged == nil && song.asked.isEmpty)
    }

    @Test("Mine, not a vector. One of the key's chords carried in from the strip: over a place the spot goes with it; let go there it is put on as its key would put it, in place of what is there; off the places it is put nowhere; and picking it up keeps a chord that was being typed (rules 46a to 46g)")
    func carriedInFromTheStrip() throws {
        // (In G the key's chords are G, Am, Bm, C, D, Em and F#dim.)
        let (sheet, song) = try vectors.sheet("grace")
        sheet.setSpot(4, 0)
        sheet.carry("C", over: 2, 5)
        #expect(sheet.dragged?.name == "C" && sheet.dragged?.from.row == -1 && sheet.spot == Spot(row: 2, at: 5))
        // While it is carried the spot is its own, and not the pointer's.
        sheet.pointerOver(4, 0)
        #expect(sheet.spot == Spot(row: 2, at: 5))
        #expect(sheet.dropOfKey())
        #expect(sheet.dragged == nil && sheet.chordAt(2, 5) == "C" && sheet.spot == Spot(row: 2, at: 5))
        // One change, joined to nothing: and it is what the key 4 asks with the spot there.
        let (keyed, keyedSong) = try vectors.sheet("grace")
        keyed.setSpot(2, 5)
        keyed.putOfKey(4)
        #expect(song.asked.count == 1 && song.asked == keyedSong.asked && song.asked[0].hasSuffix("joined false"))

        // In place of the chord that is there (rule 43).
        sheet.carry("Em", over: 1, 8)
        #expect(sheet.dropOfKey() && sheet.rows[1].chords.map(\.name).sorted() == ["C", "Em", "G", "G"])

        // Off the places nothing shows it, and let go it is put nowhere.
        song.asked = []
        sheet.carry("D", over: 7, 4)
        sheet.carryOff()
        #expect(sheet.dragged == nil && !sheet.dropOfKey())
        // A row that is a group's name, a row there is not, and a name that is no chord.
        sheet.carry("D", over: 7, 4)
        sheet.carry("D", over: 5, 0)
        #expect(sheet.dragged == nil)
        sheet.carry("D", over: 99, 0)
        #expect(sheet.dragged == nil)
        sheet.carry("Hello", over: 7, 4)
        #expect(sheet.dragged == nil && !sheet.dropOfKey())
        #expect(song.asked.isEmpty && sheet.chordAt(7, 4).isEmpty)

        // The key that copies a chord as it is dragged changes nothing here (rule 46g).
        sheet.carry("D", over: 7, 4)
        sheet.drop(copy: true)
        #expect(sheet.dragged == nil && sheet.chordAt(7, 4) == "D" && song.asked.count == 1)

        // A chord lifted from the sheet is not let go of by what is for one from the
        // strip, nor put on by it.
        sheet.lift(7, 4)
        sheet.carryOff()
        #expect(sheet.dragged?.from == Spot(row: 7, at: 4))
        #expect(!sheet.dropOfKey() && sheet.dragged != nil)
        sheet.dropNowhere()

        // While another chord is being typed nothing is carried; but picking one up
        // keeps what was typed and closes the bubble, and then it is (rule 46e). The
        // two are two changes, neither joined to the other.
        song.asked = []
        sheet.setSpot(6, 5)
        sheet.startTyping(seed: "B")
        sheet.type("Bm7")
        sheet.carry("C", over: 9, 0)
        #expect(sheet.dragged == nil && sheet.typing)
        sheet.liftOfKey()
        #expect(!sheet.typing && sheet.chordAt(6, 5) == "Bm7" && song.asked.count == 1)
        sheet.carry("C", over: 9, 0)
        #expect(sheet.dropOfKey() && sheet.chordAt(9, 0) == "C")
        #expect(song.asked.count == 2 && song.asked[1].hasSuffix("joined false"))
        // Picked up with nothing typed: nothing is kept, and the bubble is closed.
        sheet.setSpot(8, 0)
        sheet.startTyping()
        sheet.liftOfKey()
        #expect(!sheet.typing && sheet.chordAt(8, 0).isEmpty && song.asked.count == 2)

        // On a line of chords alone it takes the place of the chord whose place it is
        // over, or past the last is one more (rule 22); on a blank slide, and on a
        // slide with no text box, it is the first.
        let (intro, introSong) = try vectors.sheet("intro")
        intro.carry("Bm", over: 1, 2)
        #expect(intro.dropOfKey() && intro.rows[1].chords.map(\.name) == ["D", "Bm", "A"])
        intro.carry("G", over: 1, 6)
        #expect(intro.dropOfKey() && intro.rows[1].chords == [Chord(at: 0, name: "D"), Chord(at: 2, name: "Bm"), Chord(at: 4, name: "A"), Chord(at: 6, name: "G")])
        #expect(introSong.asked.count == 2 && introSong.asked.allSatisfy { $0.hasPrefix("setChordsAlone") && $0.hasSuffix("joined false") })
        let (blank, _) = try vectors.sheet("blank")
        blank.carry("C", over: 1, 0)
        #expect(blank.dropOfKey() && blank.rows[1].chords == [Chord(at: 0, name: "C")] && !blank.rows[1].blank)
        let (bare, _) = try vectors.sheet("bare")
        bare.carry("C", over: 2, 0)
        #expect(bare.dropOfKey() && bare.rows[2].chords == [Chord(at: 0, name: "C")])

        // What the song refuses is said, and is answered as not put on.
        struct No: Error {}
        let (refusing, refusingSong) = try vectors.sheet("grace")
        var said = 0
        refusing.refused = { _ in said += 1 }
        refusingSong.refusing = No()
        refusing.carry("C", over: 2, 5)
        #expect(!refusing.dropOfKey() && said == 1 && refusing.chordAt(2, 5).isEmpty && refusing.dragged == nil)

        // The place it was over goes, the song having been changed under it: it is
        // carried no longer.
        let two = StandInSong([TextBox(slide: 0, element: "A", words: "Holy holy", chords: []), TextBox(slide: 1, element: "B", words: "Lord God", chords: [])], key: "C")
        let short = ChordSheet(song: two)
        short.carry("F", over: 1, 0)
        #expect(short.dragged != nil)
        two.textBoxes.removeLast()
        short.reload()
        #expect(short.dragged == nil && short.spot.row == -1)
    }

    @Test("Mine, not a vector. A name the song already has that is no chord is moved, copied and pasted as it is; only what is typed must be a chord")
    func notAChord() throws {
        let boxes = [TextBox(slide: 0, element: "A", words: "Holy holy holy\nLord God Almighty",
                             chords: [Chord(at: 0, name: "N.C."), Chord(at: 5, name: "C /// | Am ///")])]
        let song = StandInSong(boxes, key: "C")
        let sheet = ChordSheet(song: song)
        sheet.move(from: Spot(row: 0, at: 0), to: Spot(row: 1, at: 5), copy: false)
        #expect(sheet.chordAt(1, 5) == "N.C." && sheet.chordAt(0, 0).isEmpty)
        sheet.move(from: Spot(row: 0, at: 5), to: Spot(row: 0, at: 10), copy: true)
        #expect(sheet.chordAt(0, 10) == "C /// | Am ///" && sheet.chordAt(0, 5) == "C /// | Am ///")
        sheet.setSpot(0, 0)
        sheet.copy(whole: false)
        sheet.setSpot(1, 0)
        sheet.paste()
        #expect(sheet.rows[1].chords == [Chord(at: 5, name: "C /// | Am ///"), Chord(at: 9, name: "C /// | Am ///")])
        // Typed, it is refused, and nothing is asked.
        song.asked = []
        #expect(!sheet.put(0, 0, "N.C."))
        #expect(!sheet.put(0, 0, "hello"))
        #expect(song.asked.isEmpty)
    }

    @Test("Mine, not a vector. A paste answers with what it did: the lines given chords, the chords put, those left out for want of a word, and the lines passed over")
    func whatAPasteDid() throws {
        let (sheet, song) = try vectors.sheet("grace")
        // Nothing copied yet, and no spot: nothing to say.
        #expect(sheet.paste() == nil)
        sheet.setSpot(1, 0)
        sheet.copy(whole: false)
        // Four chords, of the first, second, third and fifth words, onto a line of
        // seven words: all four.
        sheet.setSpot(6, 0)
        #expect(sheet.paste() == ChordSheet.Pasted(lines: 1, chords: 4))
        // Onto a line of three ("Amen and amen"): the fifth word's has none to go on.
        sheet.setSpot(9, 0)
        #expect(sheet.paste() == ChordSheet.Pasted(lines: 1, chords: 3, leftOut: 1))
        // Onto a line of one word ("Amen"): one is put, three are left out.
        sheet.setSpot(8, 0)
        #expect(sheet.paste() == ChordSheet.Pasted(lines: 1, chords: 1, leftOut: 3))
        // A whole group of four lines from the song's last line but one: it runs out
        // of lines, and what there are take what they have words for.
        sheet.setSpot(1, 0)
        sheet.copy(whole: true)
        sheet.setSpot(8, 0)
        #expect(sheet.paste() == ChordSheet.Pasted(lines: 2, chords: 2, leftOut: 3))

        // The other kind of line is passed over, and says so.
        let (intro, _) = try vectors.sheet("intro")
        intro.setSpot(3, 0)
        intro.copy(whole: false)
        intro.setSpot(1, 0)
        #expect(intro.paste() == ChordSheet.Pasted(passedOver: 1))
        intro.setSpot(1, 0)
        intro.copy(whole: false)
        intro.setSpot(3, 0)
        #expect(intro.paste() == ChordSheet.Pasted(passedOver: 1))
        intro.setSpot(6, 0)
        #expect(intro.paste() == ChordSheet.Pasted(lines: 1, chords: 3))

        // What the song refuses is counted so.
        struct No: Error {}
        song.refusing = No()
        sheet.setSpot(1, 0)
        sheet.copy(whole: false)
        sheet.setSpot(6, 0)
        #expect(sheet.paste() == ChordSheet.Pasted(refused: 1))
    }

    @Test("Mine, not a vector. A paste is one step of undo whichever of its lines is the first it changes (rule 52)")
    func pasteIsOneStep() throws {
        // A group of a line of chords alone and a line of words, onto a group of two
        // lines of words: the first copied line is passed over (the kinds differ), and
        // the one change that is made is not joined to whatever was done before it.
        let standIns = Chords.placeholders(2)
        let boxes = [
            TextBox(slide: 0, group: "Verse 1", startsItsGroup: true, element: "A", words: standIns,
                    chords: [Chord(at: 0, name: "D"), Chord(at: 2, name: "G")]),
            TextBox(slide: 1, group: "Verse 1", element: "B", words: "Holy holy holy", chords: [Chord(at: 5, name: "A")]),
            TextBox(slide: 2, group: "Verse 2", startsItsGroup: true, element: "C", words: "Lord God Almighty"),
            TextBox(slide: 3, group: "Verse 2", element: "D", words: "Early in the morning"),
        ]
        let song = StandInSong(boxes, key: "D")
        let sheet = ChordSheet(song: song)
        sheet.setSpot(2, 0)
        sheet.copy(whole: true)
        #expect(sheet.copied == [.alone(["D", "G"]), .words([.init(word: 1, offset: 0, name: "A")])])
        sheet.setSpot(4, 0)
        sheet.paste()
        #expect(song.asked == ["setChords slide 3 box \"D\" [\"A\"@6] joined false"])
    }

    @Test("Mine, not a vector. Whether a paste would take chords off: only where a line that what was copied would go onto has chords of its own; a line that would be passed over loses nothing; and nothing is asked of the song to find out")
    func whetherAPasteWouldReplace() throws {
        let standIns = Chords.placeholders(2)
        let boxes = [
            TextBox(slide: 0, group: "Verse 1", startsItsGroup: true, element: "A", words: "Holy holy holy", chords: [Chord(at: 0, name: "D")]),
            TextBox(slide: 1, group: "Verse 1", element: "B", words: "Lord God Almighty", chords: [Chord(at: 5, name: "A")]),
            TextBox(slide: 2, group: "Verse 2", startsItsGroup: true, element: "C", words: "Early in the morning"),
            TextBox(slide: 3, group: "Verse 2", element: "D", words: "Our song shall rise", chords: [Chord(at: 4, name: "G")]),
            TextBox(slide: 4, group: "Ending", startsItsGroup: true, element: "E", words: standIns,
                    chords: [Chord(at: 0, name: "D"), Chord(at: 2, name: "G")]),
            TextBox(slide: 5, group: "Ending", element: "F", words: Chords.placeholders(1)),
        ]
        let song = StandInSong(boxes, key: "D")
        let sheet = ChordSheet(song: song)
        // (The rows: 0 the first verse's name, 1 and 2 its lines; 3, and 4 and 5; 6,
        // and 7 and 8, which are lines of chords alone, the second with none on it.)
        #expect(sheet.rows.map(\.kind) == [.group, .line, .line, .group, .line, .line, .group, .line, .line])
        func would(at row: Int) -> Bool {
            sheet.setSpot(row, 0)
            return sheet.pasteWouldReplace
        }

        // Nothing copied yet: nothing would be put on, and so nothing taken off.
        #expect(!would(at: 1) && !would(at: 4))

        // One line of words copied: onto a line that has chords, yes; onto one that
        // has none, no; onto a line of chords alone it is passed over, and that line
        // keeps its chords.
        sheet.setSpot(1, 0)
        sheet.copy(whole: false)
        #expect(would(at: 1) && would(at: 2) && would(at: 5))
        #expect(!would(at: 4))
        #expect(!would(at: 7) && !would(at: 8))

        // A whole verse of two lines: it goes onto the spot's line and the next, so
        // it is yes from a line that has none if the line after it has.
        sheet.setSpot(1, 0)
        sheet.copy(whole: true)
        #expect(sheet.copied?.count == 2)
        #expect(would(at: 4))
        // From the song's last line of words, its second line would go to a line of
        // chords alone and be passed over: only the first counts.
        #expect(would(at: 5))
        // From the last line of all there is one line to go onto, and it is of the
        // other kind.
        #expect(!would(at: 8))

        // A line of chords alone copied: onto one that has chords, yes; onto the one
        // that has none, no; and a line of words keeps its own.
        sheet.setSpot(7, 0)
        sheet.copy(whole: false)
        #expect(would(at: 7) && !would(at: 8))
        #expect(!would(at: 1) && !would(at: 5))

        // No spot: no.
        sheet.setSpot(-1, 0)
        #expect(!sheet.pasteWouldReplace)
        // And the song was asked nothing by any of it.
        #expect(song.asked.isEmpty)

        // What it says is what a paste then does: every line it changes that had
        // chords, it took them off.
        sheet.setSpot(1, 0)
        sheet.copy(whole: false)
        sheet.setSpot(5, 0)
        #expect(sheet.pasteWouldReplace)
        #expect(sheet.paste() == ChordSheet.Pasted(lines: 1, chords: 1))
        #expect(sheet.rows[5].chords == [Chord(at: 0, name: "D")])
    }

    @Test("Mine, not a vector. What the song refuses is said, changes nothing, and is not counted as done")
    func refused() throws {
        struct No: Error {}
        let (sheet, song) = try vectors.sheet("grace")
        var said = 0
        var changes = 0
        sheet.refused = { _ in said += 1 }
        sheet.changed = { changes += 1 }
        song.refusing = No()
        sheet.setSpot(1, 13)
        #expect(!sheet.put(1, 13, "Em"))
        #expect(said == 1 && changes == 0 && sheet.chordAt(1, 13).isEmpty)
        // A chord moved to another line that is refused there is not taken from here.
        sheet.move(from: Spot(row: 1, at: 8), to: Spot(row: 6, at: 0), copy: false)
        #expect(said == 2 && song.asked.count == 2 && sheet.chordAt(1, 8) == "G7")
        song.refusing = nil
        #expect(sheet.put(1, 13, "Em"))
        #expect(said == 2 && changes == 1)
    }

    @Test("Mine, not a vector. After a copy, the key that pastes is named as the platform names it")
    func namedAsThePlatformNamesIt() throws {
        let (sheet, _) = try vectors.sheet("grace")
        sheet.pasteKeys = "Command+V"
        sheet.setSpot(2, 0)
        #expect(sheet.copy(whole: false) == "1 chord copied from the line: Command+V puts them on the lines from the spot's on, word for word")
        #expect(sheet.copy(whole: true) == "8 chords copied from 4 lines: Command+V puts them on the lines from the spot's on, word for word")
    }
}
