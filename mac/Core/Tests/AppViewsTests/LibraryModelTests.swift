// What the window shows and what a click does, without drawing anything: LibraryModel
// on a made-up Mac. spec/06-library.md, rules 7 to 15 and 19.

import AppViews
import Foundation
import Song
import Testing
import TestData

@MainActor
@Suite("The window's model")
struct LibraryModelTests {
    @Test("At the start the place is found, its first library is open, and the marks are worked out and kept")
    func start() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()

        #expect(model.places.map(\.name) == ["Sunday"])
        #expect(model.place?.isInUse == true)
        #expect(model.libraries.map(\.name) == ["Kids", "Old copies", "Songs"])
        #expect(model.library?.name == "Kids")
        #expect(model.songs.isEmpty && model.shown == nil)

        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        #expect(model.songs.map(\.name) == ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Holy Holy Holy", "It Is Well", "Nonsense", "Plain Song", "Rock Of Ages (Toplady)"])
        #expect(model.marks.count == 8)
        #expect(model.marks[mac.song("Amazing Grace")]?.chords == 40)
        #expect(model.marks[mac.song("Rock Of Ages (Toplady)")]?.hasChords == false)
        #expect(model.marks[mac.song("Nonsense")]?.cannotBeRead == true)
        #expect(model.notice == "8 songs, 6 with chords.")

        // The library is remembered, and the marks kept: another start has them at once,
        // with no file read again.
        let again = mac.model()
        await again.start()
        #expect(again.library?.name == "Songs")
        #expect(again.marks.count == 8)
        #expect(mac.appData.marks.count == 8)
        #expect(mac.appData.settings.lastLibrary == mac.library("Songs"))
    }

    @Test("A click on a song opens it to be looked at; one that cannot be read says so")
    func openingASong() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))

        model.open(song: mac.song("Plain Song"))
        let shown = try #require(model.shown)
        #expect(shown.name == "Plain Song" && shown.key == "G" && shown.shownKey == "G" && shown.problem == nil)
        #expect(shown.slides == 4 && shown.chords == 11)
        #expect(shown.lines.first?.text == "{c: Verse 1}")
        #expect(shown.lines.last?.text == "[G] [D/F#] [Em]")
        #expect(model.notice == "4 slides, 11 chords. A change is saved a moment after it is typed.")

        model.open(song: mac.song("Nonsense"))
        let broken = try #require(model.shown)
        #expect(broken.lines.isEmpty)
        #expect(broken.problem == "Nonsense.pro could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.")
        #expect(model.notice == "Nothing was done to this file.")
        // Nothing was: it is the bytes it was.
        #expect(try Data(contentsOf: mac.place.appendingPathComponent("Libraries/Songs/Nonsense.pro")) == Data("This is not a song.".utf8))

        // Another library, and the song that was open is open no more.
        model.choose(library: mac.library("Old copies"))
        #expect(model.shown == nil && model.songID == nil)
        await model.marksAreWorkedOut()
        #expect(model.songs.count == 4)
        #expect(model.songs.allSatisfy { model.marks[$0.id]?.isOldEditorsCopy == true })
    }

    @Test("The list is narrowed as the search is typed, and by mark")
    func narrowing() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()

        model.typed = "ho"
        #expect(model.listedSongs.map(\.name) == ["Holy Holy Holy"])
        model.typed = "a"
        model.filter = .withoutChords
        #expect(model.listedSongs.map(\.name) == ["Rock Of Ages (Toplady)"])
        model.typed = ""
        model.filter = .withChords
        #expect(model.listedSongs.map(\.name) == ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Holy Holy Holy", "It Is Well", "Plain Song"])
        model.filter = .all
        #expect(model.listedSongs.count == 8)
    }

    @Test("When the app comes to the front the lists are read again, and ProPresenter is asked after")
    func comingToTheFront() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        model.open(song: mac.song("Plain Song"))
        await model.marksAreWorkedOut()
        #expect(!model.proPresenterIsRunning)

        // Meanwhile ProPresenter was started, and a song added.
        mac.proPresenterIsRunning = true
        try TestData.file("made-by-simple-presenter/It Is Well.pro").write(to: mac.place.appendingPathComponent("Libraries/Songs/Another Song.pro"))
        await model.refresh()
        #expect(model.proPresenterIsRunning)
        #expect(model.songs.count == 9 && model.songs.contains { $0.name == "Another Song" })
        #expect(model.marks[mac.song("Another Song")]?.hasChords == true)
        // What was open is open still.
        #expect(model.library?.name == "Songs" && model.shown?.name == "Plain Song")

        mac.proPresenterIsRunning = false
        await model.refresh()
        #expect(!model.proPresenterIsRunning)
    }

    @Test("With no place found there is nothing listed; a folder chosen by hand becomes one, and is remembered")
    func choosingAFolder() async throws {
        let mac = try MadeUpMac()
        mac.hasAWorkspace = false
        let model = mac.model()
        await model.start()
        #expect(model.places.isEmpty && model.libraries.isEmpty && model.songs.isEmpty && model.notice == "")

        // One library of songs, chosen by its own folder.
        model.add(folder: mac.place.appendingPathComponent("Libraries/Songs", isDirectory: true))
        await model.marksAreWorkedOut()
        #expect(model.places.map(\.kind) == [.chosen])
        #expect(model.libraries.map(\.name) == ["Songs"] && model.songs.count == 8)

        let again = mac.model()
        await again.start()
        #expect(again.places.map(\.name) == ["Songs"] && again.songs.count == 8)
    }

    @Test("The log names files and never has a word of a song in it")
    func theLogHasNoWords() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        model.open(song: mac.song("Amazing Grace"))
        model.open(song: mac.song("Nonsense"))
        let log = try String(contentsOf: mac.appData.folder.appendingPathComponent("log.txt"), encoding: .utf8)
        #expect(log.contains("Opened Amazing Grace.pro"))
        #expect(log.contains("Could not read Nonsense.pro"))
        // Not a line of the song, and not a chord of it.
        model.open(song: mac.song("Amazing Grace"))
        for line in try #require(model.shown).lines where line.words.length > 6 {
            #expect(!log.contains(line.words.string))
        }
        #expect(!log.contains("[G"))
    }
}
