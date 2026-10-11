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

    @Test("UNTIL M7. On a line of chords alone a chord can be renamed; one more, or one fewer, is refused in a sentence and changes nothing. So is the first chord of a blank slide")
    func untilM7() throws {
        let bench = try Workbench()
        var tried = 0
        for path in try TestData.presentationPaths() where path.hasPrefix("made-by-simple-presenter/") {
            let song = try bench.open(path)
            let sheet = ChordSheet(song: song)
            var said: [String] = []
            sheet.refused = { said.append("\($0)") }
            guard let row = sheet.rows.indices.first(where: { sheet.usable($0) && sheet.rows[$0].alone }) else { continue }
            let bytes = try Data(contentsOf: song.url)
            let chords = sheet.rows[row].chords
            // One more at its end (on a blank slide, its first).
            #expect(!sheet.put(row, chords.count * 2, "Em"))
            #expect(said.count == 1 && said[0].contains("cannot yet be added"), "\(path)")
            if let firstChord = chords.first {
                #expect(!sheet.take(row, firstChord.at))
                #expect(said.count == 2, "\(path)")
            }
            #expect(try Data(contentsOf: song.url) == bytes, "\(path)")
            #expect(sheet.rows[row].chords == chords, "\(path)")
            if !chords.isEmpty {
                #expect(sheet.put(row, 0, "Bm7"), "\(path)")
                #expect(sheet.rows[row].chords.map(\.name) == ["Bm7"] + chords.dropFirst().map(\.name), "\(path)")
                #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
            }
            tried += 1
        }
        #expect(tried >= 2, "the test files were to have a line of chords alone and a blank slide among them")
    }
}
