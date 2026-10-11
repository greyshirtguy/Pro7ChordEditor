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

    @Test("The last chord taken off a line of chords alone that has words over it: the line is then empty, has no row and takes the spot no more; and undo brings it back")
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
        // (An empty line a text box ends with has no row: spec/03, rule 2a.)
        #expect(sheet.rows.count == rows - 1)
        #expect(!sheet.rows.contains { $0.slide == 7 && $0.alone })
        #expect(sheet.usable(sheet.spot.row))
        #expect(song.textBoxes[7].words.lines == [Words("Naked come to thee for dress"), Words()])
        #expect(song.textBoxes[7].chords.map(\.name) == ["E", "A"])
        #expect(said.isEmpty)
        try song.undo()
        try song.undo()
        sheet.reload()
        #expect(sheet.rows.count == rows && sheet.rows[row].chords.map(\.name) == ["B", "E"])
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
