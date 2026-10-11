// The chord sheet's model on real files: what the vectors cannot hold, since their songs
// are lists and not files. Each thing done on the sheet is asked of an `OpenSong`, which
// saves it, and the FILE is then read again (on copies of testdata/files, in a folder
// of the test's own).

import ChordRules
import Foundation
import Song
import Testing
import TestData

@Suite("The chord sheet on a file")
struct ChordSheetOnAFileTests {
    static let grace = "made-by-simple-presenter/Amazing Grace.pro"

    /// The first row that is a line of words with at least this many of them.
    private func wordsRow(of sheet: ChordSheet, after: Int = -1, words: Int = 3) throws -> Int {
        try #require(sheet.rows.indices.first { $0 > after && sheet.usable($0) && !sheet.rows[$0].alone && sheet.stops($0).count >= words })
    }

    @Test("A chord put on the sheet is in the file, in that text box, and Delete takes it off again")
    func putAndTaken() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.grace)
        let sheet = ChordSheet(song: song)
        let before = song.textBoxes
        let row = try wordsRow(of: sheet)
        let line = sheet.rows[row]
        // A place of the line that has no chord yet.
        let at = try #require(sheet.stops(row).first { sheet.chordAt(row, $0).isEmpty } ?? (0..<line.text.length).first { sheet.chordAt(row, $0).isEmpty })
        sheet.setSpot(row, at)
        sheet.putOfKey(4)
        let name = try #require(sheet.inKey.dropFirst(3).first)
        #expect(sheet.chordAt(row, at) == name)

        let read = try OpenSong(opening: song.url)
        #expect(read.textBoxes[line.block].chords.contains(Chord(at: line.start + at, name: name)))
        for (index, box) in read.textBoxes.enumerated() where index != line.block {
            #expect(box == before[index])
        }
        sheet.takeAtSpot()
        #expect(try OpenSong(opening: song.url).textBoxes == before)
    }

    @Test("A chord moved to another slide is two changes that undo as one, and leave the file's bytes as they were")
    func movedAndUndone() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.grace)
        let bytes = try Data(contentsOf: song.url)
        let sheet = ChordSheet(song: song)
        let from = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone && !sheet.rows[$0].chords.isEmpty })
        let to = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone && sheet.rows[$0].slide != sheet.rows[from].slide })
        let chord = sheet.rows[from].chords[0]
        sheet.lift(from, chord.at)
        sheet.carry(over: to, 1)
        sheet.drop(copy: false)
        #expect(sheet.chordAt(to, 1) == chord.name && sheet.chordAt(from, chord.at).isEmpty)
        #expect(sheet.spot == Spot(row: to, at: 1))
        #expect(try Data(contentsOf: song.url) != bytes)
        try song.undo()
        sheet.reload()
        #expect(!song.canUndo)
        #expect(sheet.chordAt(from, chord.at) == chord.name && sheet.chordAt(to, 1).isEmpty)
        #expect(try Data(contentsOf: song.url) == bytes)
    }

    @Test("A group's chords pasted onto another group change each line's own text box, and undo as one")
    func pastedAndUndone() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.grace)
        let bytes = try Data(contentsOf: song.url)
        let sheet = ChordSheet(song: song)
        let first = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone && !sheet.rows[$0].chords.isEmpty })
        let nextGroup = try #require(sheet.rows.indices.first { $0 > first && sheet.rows[$0].kind == .group })
        let target = try wordsRow(of: sheet, after: nextGroup, words: 1)
        sheet.setSpot(first, 0)
        let said = try #require(sheet.copy(whole: true))
        #expect(said.contains("copied from"))
        sheet.setSpot(target, 0)
        sheet.paste()
        #expect(try Data(contentsOf: song.url) != bytes)
        #expect(sheet.rows[target].chords.map(\.name) == sheet.rows[first].chords.prefix(sheet.rows[target].chords.count).map(\.name))
        try song.undo()
        #expect(!song.canUndo)
        #expect(try Data(contentsOf: song.url) == bytes)
    }

    @Test("A chord dragged onto a slide that has no text box, and a line of chords alone pasted onto a blank slide: each is in the file, and each undoes as one, to the byte")
    func draggedAndPastedOntoBlankSlides() throws {
        let bench = try Workbench()
        let song = try bench.open(ChordsWithNoWordsTests.shaped)
        let bytes = try Data(contentsOf: song.url)
        let sheet = ChordSheet(song: song)
        var said: [String] = []
        sheet.refused = { said.append("\($0)") }
        let four = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 3 && sheet.rows[$0].alone })
        let first = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 0 && sheet.rows[$0].blank })
        let last = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 11 && sheet.rows[$0].blank })
        #expect(sheet.rows[four].chords.map(\.name) == ["E", "B", "C#m", "A"])

        // The second of the four, dragged to the last slide, which has nothing on it.
        sheet.lift(four, 2)
        sheet.carry(over: last, 0)
        sheet.drop(copy: false)
        #expect(sheet.rows[four].chords.map(\.name) == ["E", "C#m", "A"])
        #expect(sheet.rows[last].chords == [Chord(at: 0, name: "B")] && !sheet.rows[last].blank)
        #expect(!song.textBoxes[11].element.isEmpty && song.textBoxes[3].words == Chords.placeholders(3))
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
        try song.undo()
        #expect(!song.canUndo)
        #expect(try Data(contentsOf: song.url) == bytes)
        sheet.reload()

        // The line of four, copied, and pasted onto the first slide, which is blank.
        sheet.setSpot(four, 0)
        #expect(sheet.copy(whole: false) != nil)
        sheet.setSpot(first, 0)
        let pasted = try #require(sheet.paste())
        #expect(pasted.lines == 1 && pasted.chords == 4)
        #expect(sheet.rows[first].chords.map(\.name) == ["E", "B", "C#m", "A"] && sheet.rows[first].chords.map(\.at) == [0, 2, 4, 6])
        #expect(song.textBoxes[0].words == Chords.placeholders(4))
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
        try song.undo()
        #expect(!song.canUndo)
        #expect(try Data(contentsOf: song.url) == bytes)
        #expect(said.isEmpty, "\(said)")
    }

    @Test("The last chord taken off a line of chords alone that has words over it: the line is one zero-width space, its row is there still with the spot on it, and a chord is put on it again. So for one that is the whole of its slide")
    func theLastChordOfALineUnderWords() throws {
        let bench = try Workbench()
        let song = try bench.open(ChordsWithNoWordsTests.shaped)
        let bytes = try Data(contentsOf: song.url)
        let sheet = ChordSheet(song: song)
        var said: [String] = []
        sheet.refused = { said.append("\($0)") }
        let rows = sheet.rows.count
        let row = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 7 && sheet.rows[$0].alone })
        #expect(sheet.rows[row].chords.map(\.name) == ["B", "E"])
        sheet.setSpot(row, 0)
        sheet.takeAtSpot()
        #expect(sheet.rows[row].chords == [Chord(at: 0, name: "E")] && sheet.rows[row].text == Words(" "))
        sheet.setSpot(row, 0)
        sheet.takeAtSpot()
        // Its row is as it was but for its chords: a line of chords alone with none,
        // that takes the spot. It hung its chords on plain spaces; it is one
        // zero-width space now, which a stray space cannot be taken for.
        #expect(sheet.rows.count == rows)
        #expect(sheet.rows[row].alone && !sheet.rows[row].blank && sheet.usable(row))
        #expect(sheet.rows[row].chords.isEmpty && sheet.rows[row].text == Chords.placeholders(1))
        #expect(sheet.spot == Spot(row: row, at: 0) && sheet.stops(row) == [0])
        #expect(song.textBoxes[7].words.lines == [Words("Naked come to thee for dress"), Chords.placeholders(1)])
        #expect(song.textBoxes[7].chords.map(\.name) == ["E", "A"])
        // A chord put on it again, and a second after it.
        #expect(sheet.put(row, 0, "F#m"))
        #expect(sheet.put(row, 2, "B"))
        #expect(sheet.rows[row].chords == [Chord(at: 0, name: "F#m"), Chord(at: 2, name: "B")])
        #expect(song.textBoxes[7].chords == [Chord(at: 0, name: "E"), Chord(at: 11, name: "A"), Chord(at: 29, name: "F#m"), Chord(at: 31, name: "B")])
        #expect(song.textBoxes[7].words.lines[1] == Chords.placeholders(2))
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
        #expect(said.isEmpty)
        while song.canUndo { try song.undo() }
        sheet.reload()
        #expect(sheet.rows.count == rows && sheet.rows[row].chords.map(\.name) == ["B", "E"])
        #expect(try Data(contentsOf: song.url) == bytes)

        // The fourth slide is nothing but a line of four chords on zero-width spaces.
        // With all four taken off it is that line still, and not a blank slide.
        let whole = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 3 && sheet.rows[$0].alone })
        for _ in 0..<4 {
            sheet.setSpot(whole, 0)
            sheet.takeAtSpot()
        }
        #expect(sheet.rows[whole].alone && !sheet.rows[whole].blank && sheet.rows[whole].chords.isEmpty)
        #expect(song.textBoxes[3].words == Chords.placeholders(1) && !song.textBoxes[3].blank)
        #expect(sheet.put(whole, 0, "E"))
        #expect(song.textBoxes[3].chords == [Chord(at: 0, name: "E")] && song.textBoxes[3].words == Chords.placeholders(1))
        while song.canUndo { try song.undo() }
        #expect(try Data(contentsOf: song.url) == bytes)

        // The seventh hangs its one chord on one plain space, and is the whole of its
        // slide. With the chord taken off it is neither a blank slide nor gone, as a
        // box that held a stray space would be: it is the line still.
        sheet.reload()
        let one = try #require(sheet.rows.indices.first { sheet.rows[$0].slide == 6 && sheet.rows[$0].alone })
        #expect(sheet.rows[one].text == Words(" "))
        sheet.setSpot(one, 0)
        sheet.takeAtSpot()
        #expect(sheet.rows[one].alone && !sheet.rows[one].blank && sheet.rows[one].text == Chords.placeholders(1))
        #expect(!song.textBoxes[6].blank && song.textBoxes[6].words == Chords.placeholders(1))
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
        try song.undo()
        #expect(try Data(contentsOf: song.url) == bytes)
    }

    @Test("On a line of chords alone a chord is renamed, one more is put at its end and one is taken off, and each is in the file; so is the first chord of a blank slide, which Delete takes off again leaving the file's bytes as they were")
    func chordsWithNoWords() throws {
        let bench = try Workbench()
        var lines = 0, blanks = 0
        let paths = try TestData.presentationPaths().filter { $0.hasPrefix("made-by-simple-presenter/") } + [ChordsWithNoWordsTests.shaped]
        for path in paths {
            let song = try bench.open(path)
            let sheet = ChordSheet(song: song)
            var said: [String] = []
            sheet.refused = { said.append("\($0)") }
            for row in sheet.rows.indices where sheet.usable(row) && sheet.rows[row].alone {
                let bytes = try Data(contentsOf: song.url)
                let chords = sheet.rows[row].chords
                let names = chords.map(\.name)
                // One more at its end (on a blank slide, its first).
                #expect(sheet.put(row, chords.count * 2, "Em"), "\(path), row \(row)")
                #expect(sheet.rows[row].chords.map(\.name) == names + ["Em"], "\(path), row \(row)")
                #expect(sheet.rows[row].chords.map(\.at) == Array(0...chords.count).map { $0 * 2 }, "\(path), row \(row)")
                #expect(sheet.rows[row].alone && !sheet.rows[row].blank)
                #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes, "\(path), row \(row)")
                if chords.isEmpty {
                    // A blank slide's first chord, taken off again.
                    blanks += 1
                    #expect(sheet.take(row, 0), "\(path), row \(row)")
                    #expect(sheet.rows[row].blank && sheet.rows[row].chords.isEmpty, "\(path), row \(row)")
                    #expect(try Data(contentsOf: song.url) == bytes, "\(path), row \(row)")
                } else {
                    lines += 1
                    // Its first renamed, and then taken off: the rest move up.
                    #expect(sheet.put(row, 0, "Bm7"), "\(path), row \(row)")
                    #expect(sheet.rows[row].chords.map(\.name) == ["Bm7"] + names.dropFirst() + ["Em"], "\(path), row \(row)")
                    #expect(sheet.take(row, 0), "\(path), row \(row)")
                    #expect(sheet.rows[row].chords.map(\.name) == Array(names.dropFirst()) + ["Em"], "\(path), row \(row)")
                    #expect(sheet.rows[row].chords.map(\.at) == chords.indices.map { $0 * 2 }, "\(path), row \(row)")
                    #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes, "\(path), row \(row)")
                    // And every step of it taken back.
                    while song.canUndo { try song.undo() }
                    sheet.reload()
                    #expect(sheet.rows[row].chords == chords, "\(path), row \(row)")
                    #expect(try Data(contentsOf: song.url) == bytes, "\(path), row \(row)")
                }
            }
            #expect(said.isEmpty, "\(path): \(said)")
        }
        #expect(lines >= 9 && blanks >= 6, "\(lines) lines of chords alone and \(blanks) blank slides were tried")
    }
}
