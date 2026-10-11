// Earlier versions in the window, without drawing anything: the mark, the panel's list,
// looking at a copy, putting one back and taking that back, and All Copies.
// spec/07-backups.md, rules 14 to 23.

import AppViews
import ChordRules
import Foundation
import Song
import Testing
import TestData

@MainActor
@Suite("Earlier versions, in the window")
struct EarlierVersionsTests {
    /// A made-up Mac on which Plain Song has been changed twice: nine days ago its key
    /// was set to E, and two days ago the three chords of its first slide were taken
    /// off, leaving eight of its eleven.
    private func macWithHistory() throws -> (MadeUpMac, original: Data, inE: Data, now: Data) {
        let mac = try MadeUpMac()
        let original = try mac.bytes(of: "Plain Song")
        try mac.change("Plain Song", daysAgo: 9) { try $0.setKey("E") }
        let inE = try mac.bytes(of: "Plain Song")
        try mac.change("Plain Song", daysAgo: 2) { song in
            let box = song.textBoxes[0]
            try song.setChords(slide: box.slide, element: box.element, [])
        }
        return (mac, original, inE, try mac.bytes(of: "Plain Song"))
    }

    private func modelOn(_ mac: MadeUpMac, song: String = "Plain Song") async -> LibraryModel {
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(song))
        return model
    }

    @Test("A song that has copies has a mark; its earlier versions are listed, the newest first and picked")
    func listed() async throws {
        let (mac, _, _, _) = try macWithHistory()
        let model = await modelOn(mac)
        #expect(model.songsWithCopies == ["Plain Song"])
        #expect(model.hasEarlierVersions)
        #expect(model.shown?.key == "E" && model.shown?.chords == 8)

        model.showEarlierVersions()
        let panel = try #require(model.earlier)
        #expect(panel.copies.count == 2)
        #expect(panel.copies.map(\.why) == [.beforeAChange, .beforeAChange])
        #expect(panel.copies.map(\.isTheFirst) == [false, true])
        // The newest is picked and shown as it was: in E, with all its chords still.
        #expect(panel.pickedID == panel.copies[0].id)
        #expect(panel.picked?.key == "E" && panel.picked?.chords == 11)
        #expect(panel.comparison == "11 chords, in E.  Now: 8 chords, in E.")

        model.pick(copy: panel.copies[1].id)
        #expect(model.earlier?.picked?.key == "G")
        #expect(model.earlier?.comparison == "11 chords, in G.  Now: 8 chords, in E.")
        #expect(model.earlier?.picked?.lines.first?.text == "{c: Verse 1}")

        model.closeEarlierVersions()
        #expect(model.earlier == nil)
        // A song with no copies has no earlier versions to show.
        model.open(song: mac.song("Amazing Grace"))
        #expect(!model.hasEarlierVersions)
        model.showEarlierVersions()
        #expect(model.earlier == nil)
    }

    @Test("Put back, the song's file is the copy; taken back, it is what it was")
    func putBackAndTakenBack() async throws {
        let (mac, original, _, now) = try macWithHistory()
        let model = await modelOn(mac)
        model.showEarlierVersions()
        let first = try #require(model.earlier?.copies.last)
        model.pick(copy: first.id)

        model.putBackPicked()
        #expect(try mac.bytes(of: "Plain Song") == original)
        #expect(model.earlier == nil && model.refusal == nil)
        #expect(model.shown?.key == "G" && model.shown?.chords == 11)
        #expect(model.justPutBack?.sentence == "Put back the version of \(model.when(first)).")
        #expect(model.notice == model.justPutBack?.sentence)

        // The file as it was just before is the newest copy now, and says why.
        model.showEarlierVersions()
        let newest = try #require(model.earlier?.copies.first)
        #expect(model.earlier?.copies.count == 3 && newest.why == .beforePuttingBack)
        #expect(try Data(contentsOf: newest.url) == now)
        model.closeEarlierVersions()

        // Undo: only putting that copy back.
        model.takeBack()
        #expect(try mac.bytes(of: "Plain Song") == now)
        #expect(model.shown?.key == "E" && model.shown?.chords == 8)
        #expect(model.justPutBack == nil)
        model.show(editor: .chordPro)
        #expect(model.notice == "4 slides, 8 chords. A change is saved a moment after it is typed.")
    }

    @Test("Nothing is put back while ProPresenter is running, and the panel says why")
    func whileProPresenterIsRunning() async throws {
        let (mac, _, _, now) = try macWithHistory()
        let model = await modelOn(mac)
        model.showEarlierVersions()
        model.pick(copy: model.earlier?.copies.last?.id)
        mac.proPresenterIsRunning = true

        model.putBackPicked()
        #expect(model.refusal == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
        #expect(try mac.bytes(of: "Plain Song") == now)
        #expect(model.earlier != nil && model.justPutBack == nil)
        #expect(model.earlier?.copies.count == 2)

        mac.proPresenterIsRunning = false
        model.putBackPicked()
        #expect(model.refusal == nil && model.earlier == nil && model.shown?.key == "G")
    }

    @Test("A copy of another song than the one now there asks first, with both names")
    func aCopyOfAnotherSong() async throws {
        let (mac, _, _, now) = try macWithHistory()
        // Among Plain Song's copies is one of a file that was Amazing Grace: another
        // song once stood at that place under that name.
        let grace = try mac.bytes(of: "Amazing Grace")
        let elsewhere = mac.folder.appendingPathComponent("Was there once.pro")
        try grace.write(to: elsewhere)
        try mac.appData.copies().keep(elsewhere, of: KeptSong(place: "Sunday", library: "Songs", name: "Plain Song"), why: .beforeAChange,
                                      at: Date().addingTimeInterval(-86_400))
        let model = await modelOn(mac)
        model.showEarlierVersions()
        let stranger = try #require(model.earlier?.copies.first { $0.songName == "Amazing Grace" })
        model.pick(copy: stranger.id)

        model.putBackPicked()
        #expect(model.earlier?.question == "This copy is of \u{201C}Amazing Grace\u{201D}, and the song here now is \u{201C}Plain Song\u{201D}: another song under the same name. Put the copy in its place?")
        #expect(try mac.bytes(of: "Plain Song") == now)
        // Picking again is leaving it.
        model.pick(copy: stranger.id)
        #expect(model.earlier?.question == nil)
        #expect(try mac.bytes(of: "Plain Song") == now)

        model.putBackPicked()
        model.putBackPicked(sure: true)
        #expect(try mac.bytes(of: "Plain Song") == grace)
        #expect(model.shown?.chords == 40 && model.earlier == nil)
    }

    @Test("When the app starts, copies that are kept no longer are thrown away, and never the first")
    func thinnedAtTheStart() async throws {
        let mac = try MadeUpMac()
        // Twenty-five changes of Plain Song, each a copy, from 300 days ago to 60.
        let keys = Chords.majorKeys() + Chords.minorKeys()
        for n in 0..<25 {
            try mac.change("Plain Song", daysAgo: 300 - 10 * Double(n)) { try $0.setKey(keys[n]) }
        }
        let copies = mac.appData.copies()
        let kept = KeptSong(place: "Sunday", library: "Songs", name: "Plain Song")
        let first = try #require(copies.copies(of: kept).last)
        #expect(copies.copies(of: kept).count == 25)

        let model = mac.model()
        await model.start()
        let left = copies.copies(of: kept)
        #expect(left.count == 21 && left.last?.id == first.id)
        #expect(try String(contentsOf: mac.appData.folder.appendingPathComponent("log.txt"), encoding: .utf8).contains("4 old copies were thrown away."))
    }

    @Test("All Copies: every song that has copies, whether its song is still there, a copy looked at and saved as a file")
    func allCopies() async throws {
        let (mac, original, _, _) = try macWithHistory()
        try mac.change("Holy Holy Holy", daysAgo: 1) { try $0.setKey("D") }
        try mac.change("Amazing Grace (Chords)", in: "Old copies", daysAgo: 3) { try $0.setKey("A") }
        try FileManager.default.removeItem(at: mac.place.appendingPathComponent("Libraries/Songs/Holy Holy Holy.pro"))

        let model = AllCopiesModel(copies: mac.appData.copies())
        #expect(model.rows.isEmpty)
        model.read()
        #expect(model.rows.map(\.song.id) == ["Sunday/Old copies/Amazing Grace (Chords)", "Sunday/Songs/Holy Holy Holy", "Sunday/Songs/Plain Song"])
        #expect(model.rows.map(\.songIsStillThere) == [true, false, true])
        #expect(model.rows.map(\.copies.count) == [1, 1, 2])
        #expect(!model.size.isEmpty)
        // The first song is picked, and its newest copy shown.
        #expect(model.row?.song.name == "Amazing Grace (Chords)" && model.looked?.key == "G")

        model.pick(song: "Sunday/Songs/Plain Song")
        model.pick(copy: model.row?.copies.last?.id)
        #expect(model.looked?.key == "G" && model.looked?.chords == 11)
        let saved = mac.folder.appendingPathComponent("Saved.pro")
        #expect(model.savePicked(to: saved) == nil)
        #expect(try Data(contentsOf: saved) == original)
        #expect(model.savePicked(to: mac.folder.appendingPathComponent("No such folder/Saved.pro"))?.hasPrefix("The copy could not be saved there") == true)
    }
}
