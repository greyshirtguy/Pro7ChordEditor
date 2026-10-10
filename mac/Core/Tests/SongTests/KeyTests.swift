// The song's two keys: the one its chords are written in, and the one ProPresenter is
// to show them in. spec/05-song-and-saving.md, rules 4 to 8.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("The key")
struct KeyTests {
    @Test("Setting the key sets both keys, where they were the same; and it is a step of undo")
    func bothWhereTheyWereTheSame() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        let original = try Data(contentsOf: song.url)
        #expect(song.key == "G" && song.shownKey == "G")

        try song.setKey("F#m")
        #expect(song.key == "F#m" && song.shownKey == "F#m")
        let saved = try message(at: song.url)
        #expect(saved.music.original.musicKey == .fSharp && saved.music.original.musicScale == .minor)
        #expect(saved.music.user == saved.music.original)
        // It changes no chord, and nothing else: the file before, given the new key, is the file after.
        var expected = try RVData_Presentation(serializedBytes: original)
        expected.music = saved.music
        #expect(expected == saved)
        #expect(bench.copiesKept == [original])

        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
        #expect(song.key == "G")
        try song.redo()
        #expect(song.key == "F#m")
    }

    @Test("Setting the key leaves the key it is shown in alone, where someone had chosen another")
    func leavesAChosenKeyAlone() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        try song.setShownKey("E")
        #expect(song.key == "G" && song.shownKey == "E")
        #expect(try message(at: song.url).music.original.musicKey == .g)

        try song.setKey("A")
        #expect(song.key == "A" && song.shownKey == "E")
        // Set to the same again, the two go together from then on.
        try song.setShownKey("A")
        try song.setKey("Bb")
        #expect(song.key == "Bb" && song.shownKey == "Bb")
    }

    @Test("A song that states no key is given one, in both; taking that back leaves it with none")
    func aSongWithNoKey() throws {
        let bench = try Workbench()
        // Holy Holy Holy states no key: it has no `music` at all.
        let song = try bench.open("made-by-simple-presenter/Holy Holy Holy.pro")
        let original = try Data(contentsOf: song.url)
        #expect(song.key == "" && song.shownKey == "")
        #expect(try !message(at: song.url).hasMusic)

        // It cannot be shown in another key than one it does not have.
        #expect { try song.setShownKey("D") } throws: { ($0 as? Refusal)?.reason == .noKeyToShowAnother }
        #expect(try Data(contentsOf: song.url) == original)

        try song.setKey("D")
        #expect(song.key == "D" && song.shownKey == "D")
        try song.undo()
        #expect(try !message(at: song.url).hasMusic)
        #expect(try Data(contentsOf: song.url) == original)
    }

    @Test("What is no key is refused; the key it has already is nothing to do")
    func whatIsNoKey() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        let original = try Data(contentsOf: song.url)
        for nonsense in ["", "H", "Gmaj7", "g", "C##"] {
            #expect { try song.setKey(nonsense) } throws: { ($0 as? Refusal)?.reason == .notAKey }
            #expect { try song.setShownKey(nonsense) } throws: { ($0 as? Refusal)?.reason == .notAKey }
        }
        try song.setKey("G")
        try song.setShownKey("G")
        #expect(try Data(contentsOf: song.url) == original)
        #expect(!song.canUndo && bench.copiesKept.isEmpty)
    }

    @Test("Every key the file can hold is read and written by its own number")
    func everyKey() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        for number in 0...20 {
            for minor in [false, true] {
                let key = Chords.keyName(number, minor: minor)
                try song.setKey(key)
                let saved = try message(at: song.url).music.original
                #expect(saved.musicKey.rawValue == number && (saved.musicScale == .minor) == minor)
                #expect(try OpenSong(opening: song.url).key == key)
            }
        }
    }
}
