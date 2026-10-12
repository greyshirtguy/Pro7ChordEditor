// The guide, as far as it can be told without a hand: that its words are the spec's, to
// the byte, and say the Mac's own names for the keys; that the page of the spec says the
// same words; and that the app shows it once by itself and no more. spec/13-guide.md.
//
// What it looks like is in the pictures (PictureTests, "the guide"), and its own
// pictures are GuidePictureTests'. That its window opens, at the first start and from
// Help and from the ? in the strip, with the app still to be used behind it, is the
// maintainer's to try: no test here has a scene of the app to open a window in.

import AppKit
@testable import AppViews
import Song
import SwiftUI
import Testing
import TestData

@MainActor
@Suite("The guide")
struct GuideTests {
    private func asTheSpecHasIt() throws -> Guide {
        try #require(Guide.read(try TestData.spec("guide.json")), "spec/guide.json is not the guide")
    }

    /// The short names that stand in curly brackets in a sentence.
    private func names(in sentence: String) -> [String] {
        var found: [String] = []
        var rest = Substring(sentence)
        while let open = rest.firstIndex(of: "{"), let close = rest[open...].firstIndex(of: "}") {
            found.append(String(rest[rest.index(after: open)..<close]))
            rest = rest[rest.index(after: close)...]
        }
        return found
    }

    @Test("The words the app carries are spec/guide.json to the byte, and are what the app finds and shows, in the Mac's words")
    func theWordsAreTheSpecs() throws {
        let original = try TestData.spec("guide.json")
        let copy = try Data(contentsOf: GuidePictureTests.kept.appendingPathComponent(GuideFiles.wordsName))
        #expect(copy == original, "mac/Core/Sources/AppViews/Guide/guide.json is no longer spec/guide.json: copy the one over the other")
        // And what was built from it, which is what the app reads.
        let folder = try #require(GuideFiles.folder, "the guide's folder was not found beside the tests")
        #expect(try Data(contentsOf: folder.appendingPathComponent(GuideFiles.wordsName)) == original)
        let guide = try asTheSpecHasIt()
        #expect(GuideFiles.guide == guide.said(on: "mac"))
        // In the Mac's words and not Windows': Command, and never Ctrl.
        let shown = try #require(GuideFiles.guide).everySentence.joined(separator: "\n")
        #expect(shown.contains("Command+C") && !shown.contains("Ctrl"))
    }

    @Test("A key's name is put in wherever it stands, in every part of the guide; and every part is among what there is to read, in the order it is read")
    func everyPartOfIt() throws {
        // A guide made up for this, with a name in each of its parts.
        let made = #"""
            {"title": "T {k}", "first": {"heading": "H {k}", "points": ["P1 {k}", "P2 {k}"]},
             "sections": [
               {"heading": "S1 {k}", "lead": "L {k}", "picture": "one", "paragraphs": ["A {k}", "B {k}"],
                "items": [{"lead": "I1 {k}", "text": "X1 {k}", "picture": "two"}, {"lead": "I2 {k}", "text": "X2 {k}"}], "sample": "[C]sample"},
               {"heading": "S2 {k}", "items": [{"lead": "I3 {k}", "text": "X3 {k}", "picture": "three"}]}],
             "foot": "F {k}", "pictures": {"one": "one {k}", "two": "two {k}", "three": "three {k}"},
             "names": {"here": {"k": "KEY"}, "there": {"k": "OTHER"}}}
            """#
        let guide = try #require(Guide.read(Data(made.utf8)))
        #expect(guide.everySentence == ["T {k}", "H {k}", "P1 {k}", "P2 {k}", "S1 {k}", "L {k}", "A {k}", "B {k}", "I1 {k}", "X1 {k}", "I2 {k}", "X2 {k}",
                                        "[C]sample", "S2 {k}", "I3 {k}", "X3 {k}", "F {k}"])
        #expect(guide.picturesNamed == ["one", "two", "three"])
        let here = guide.said(on: "here")
        #expect(here.everySentence == ["T KEY", "H KEY", "P1 KEY", "P2 KEY", "S1 KEY", "L KEY", "A KEY", "B KEY", "I1 KEY", "X1 KEY", "I2 KEY", "X2 KEY",
                                       "[C]sample", "S2 KEY", "I3 KEY", "X3 KEY", "F KEY"])
        #expect(here.pictures == ["one": "one KEY", "two": "two KEY", "three": "three KEY"])
        #expect(here.picturesNamed == ["one", "two", "three"])
        #expect(here.sections[0].items?.map(\.picture) == ["two", nil])
        // Each platform has its own, and the other's is not put in.
        #expect(guide.said(on: "there").title == "T OTHER")
        #expect(guide.said(on: "nowhere") == guide)
    }

    @Test("The guide's folder is found in the app as Xcode builds it and beside the tests as the package manager builds them; and where there is none, nothing is found")
    func whereTheGuideIs() throws {
        let temporary = FileManager.default.temporaryDirectory.appendingPathComponent(MadeUpMac.prefix + "guide-" + UUID().uuidString, isDirectory: true)
        defer {
            // Only ever the folder made just above, and only if it is named as such.
            if temporary.lastPathComponent.hasPrefix(MadeUpMac.prefix) { try? FileManager.default.removeItem(at: temporary) }
        }
        let words = try TestData.spec("guide.json")
        func make(_ path: String) throws -> URL {
            let folder = temporary.appendingPathComponent(path, isDirectory: true)
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            return folder
        }
        // As in the app: Contents/Resources of the app, and in that the package's
        // bundle with resources of its own.
        let app = try make("An.app/Contents/Resources")
        let inApp = try make("An.app/Contents/Resources/\(GuideFiles.bundleName)/Contents/Resources/Guide")
        try words.write(to: inApp.appendingPathComponent(GuideFiles.wordsName))
        #expect(GuideFiles.folder(in: [app])?.standardizedFileURL == inApp.standardizedFileURL)
        // As beside the tests: the bundle with the guide's folder directly in it.
        let built = try make("debug")
        let beside = try make("debug/\(GuideFiles.bundleName)/Guide")
        try words.write(to: beside.appendingPathComponent(GuideFiles.wordsName))
        #expect(GuideFiles.folder(in: [built])?.standardizedFileURL == beside.standardizedFileURL)
        // The first place that has it is the one; a place without it is passed over.
        let empty = try make("nothing here")
        #expect(GuideFiles.folder(in: [empty, built, app])?.standardizedFileURL == beside.standardizedFileURL)
        // A bundle with no words in it is not the guide's, and nowhere is nowhere.
        _ = try make("half/\(GuideFiles.bundleName)/Guide")
        #expect(GuideFiles.folder(in: [temporary.appendingPathComponent("half")]) == nil)
        #expect(GuideFiles.folder(in: [empty]) == nil)
        #expect(GuideFiles.folder(in: []) == nil)
    }

    @Test("What is not the guide is read as nothing, and a guide with a part left out likewise")
    func whatIsNotTheGuide() throws {
        #expect(Guide.read(Data()) == nil)
        #expect(Guide.read(Data("This is not a guide.".utf8)) == nil)
        #expect(Guide.read(Data(#"{"title": "Only a title"}"#.utf8)) == nil)
        // The least there can be: the parts that are always there, and a section with
        // nothing but its heading.
        let least = #"{"title":"T","first":{"heading":"H","points":[]},"sections":[{"heading":"S"}],"foot":"F","pictures":{},"names":{}}"#
        let guide = try #require(Guide.read(Data(least.utf8)))
        #expect(guide.sections.count == 1 && guide.sections[0].items == nil && guide.sections[0].picture == nil)
        #expect(guide.everySentence == ["T", "H", "S", "F"])
        #expect(guide.picturesNamed.isEmpty)
    }

    @Test("It has what it is laid out by: a title, what to know first, the quick way with its one large sentence and a picture, and the ways after it")
    func whatItHas() throws {
        let guide = try asTheSpecHasIt()
        #expect(!guide.title.isEmpty && !guide.foot.isEmpty)
        #expect(guide.first.points.count >= 3)
        // The first section, and only the first, has the sentence that is written large.
        #expect(guide.sections.first?.lead?.isEmpty == false)
        #expect(guide.sections.dropFirst().allSatisfy { $0.lead == nil })
        #expect(guide.sections.first?.picture != nil)
        // No section is a heading and nothing else, and nothing to read is empty.
        for section in guide.sections {
            #expect(!(section.paragraphs ?? []).isEmpty || !(section.items ?? []).isEmpty, "\(section.heading) has nothing under it")
        }
        #expect(guide.everySentence.allSatisfy { !$0.trimmingCharacters(in: .whitespaces).isEmpty })
        // Every picture the words name is described, once each, and nothing is
        // described that they do not name.
        #expect(Set(guide.picturesNamed) == Set(guide.pictures.keys))
        #expect(Set(guide.picturesNamed).count == guide.picturesNamed.count)
        #expect(guide.pictures.values.allSatisfy { !$0.isEmpty })
        // The sample is a line of ChordPro: it has a chord in brackets.
        let sample = try #require(guide.sections.compactMap(\.sample).first)
        #expect(sample.contains("[") && sample.contains("]"))
    }

    @Test("Every short name in curly brackets has a word on each platform, none is left in what is said, and each platform says its own keys")
    func theNamesOfTheKeys() throws {
        let guide = try asTheSpecHasIt()
        let all = guide.everySentence + Array(guide.pictures.values)
        let used = Set(all.flatMap(names))
        #expect(!used.isEmpty)
        #expect(Set(guide.names.keys) == ["mac", "windows"])
        for (platform, own) in guide.names {
            // A word for every name used, and no name that nothing uses.
            #expect(Set(own.keys) == used, "\(platform)")
            #expect(own.values.allSatisfy { !$0.isEmpty && !$0.contains("{") })
            let said = guide.said(on: platform)
            for sentence in said.everySentence + Array(said.pictures.values) {
                #expect(!sentence.contains("{") && !sentence.contains("}"), "\(platform): \(sentence)")
            }
            // What is not a name is as it was: the same sentences, in the same order.
            #expect(said.everySentence.count == guide.everySentence.count)
            #expect(said.sections.map(\.heading) == guide.sections.map(\.heading))
            #expect(said.picturesNamed == guide.picturesNamed)
            #expect(said.names == guide.names)
        }
        // The Mac's keys, and none of Windows'.
        let mac = guide.said(on: "mac").everySentence.joined(separator: "\n")
        for word in ["Command+C", "Command+V", "Command+Z", "Shift+Command+Z", "Shift+Command+C", "Option", "Return", "Command+1", "Command+2"] {
            #expect(mac.contains(word), "the Mac's guide does not say \(word)")
        }
        #expect(!mac.contains("Ctrl") && !mac.contains("Enter"))
        // And Windows', and none of the Mac's.
        let windows = guide.said(on: "windows").everySentence.joined(separator: "\n")
        for word in ["Ctrl+C", "Ctrl+V", "Ctrl+Z", "Ctrl+Y", "Ctrl+Shift+C", "Enter", "Ctrl+1", "Ctrl+2"] {
            #expect(windows.contains(word), "the guide for Windows does not say \(word)")
        }
        #expect(!windows.contains("Command") && !windows.contains("Option") && !windows.contains("Return"))
        // A platform the file does not know is said with the names left standing.
        #expect(guide.said(on: "linux").everySentence == guide.everySentence)
    }

    @Test("A name is put in wherever it stands in curly brackets; one that is not known is left standing, and so is a bracket with no other")
    func puttingNamesIn() {
        let names = ["ctrl": "Command", "enter": "Return"]
        #expect(Guide.put(names, into: "{ctrl}+C copies") == "Command+C copies")
        #expect(Guide.put(names, into: "press {enter}, or {ctrl}+V; {ctrl}") == "press Return, or Command+V; Command")
        #expect(Guide.put(names, into: "nothing to put in") == "nothing to put in")
        #expect(Guide.put(names, into: "") == "")
        #expect(Guide.put(names, into: "{unknown} stays, and {enter} goes in") == "{unknown} stays, and Return goes in")
        #expect(Guide.put(names, into: "a bracket { with no other") == "a bracket { with no other")
        #expect(Guide.put(names, into: "one closed } and then {ctrl}") == "one closed } and then Command")
        #expect(Guide.put(names, into: "{}") == "{}")
        #expect(Guide.put([:], into: "{ctrl}") == "{ctrl}")
        // Square brackets, which the sample line has, are nothing to it.
        #expect(Guide.put(names, into: "[G]Amazing [C]grace") == "[G]Amazing [C]grace")
    }

    /// Words as they are held against a page of the spec: where a line ends, the stars
    /// that make a word bold, and which kind of quotation mark, are the page's own
    /// business.
    private static func plain(_ text: String) -> String {
        var text = text.replacingOccurrences(of: "**", with: "")
        for (curly, straight) in [("\u{2019}", "'"), ("\u{2018}", "'"), ("\u{201C}", "\""), ("\u{201D}", "\"")] {
            text = text.replacingOccurrences(of: curly, with: straight)
        }
        return text.split(whereSeparator: \.isWhitespace).joined(separator: " ")
    }

    private func thePage() throws -> String {
        Self.plain(String(decoding: try TestData.spec("13-guide.md"), as: UTF8.self))
    }

    @Test("The page of the spec says the guide's words, every sentence of them, as Windows says them")
    func thePageSaysTheWords() throws {
        let page = try thePage()
        let windows = try asTheSpecHasIt().said(on: "windows")
        for sentence in windows.everySentence {
            #expect(page.contains(Self.plain(sentence)), "spec/13-guide.md does not say: \(sentence)")
        }
        // And what each picture shows, which the page has under the picture.
        for says in windows.pictures.values {
            #expect(page.contains(Self.plain(says)), "spec/13-guide.md does not say of a picture: \(says)")
        }
    }

    @Test("The app shows the guide once by itself: at its first start on a Mac, and not at the next; asked for, it is shown whenever")
    func shownOnce() async throws {
        let mac = try MadeUpMac()
        let first = mac.model()
        #expect(first.guideAsked == 0)
        #expect(mac.appData.settings.guideShown == nil)
        await first.start()
        #expect(first.guideAsked == 1)
        // It is written down at once that it was shown, not when it is closed.
        #expect(mac.appData.settings.guideShown == true)
        // The window shown again in the same run of the app starts nothing again.
        await first.start()
        #expect(first.guideAsked == 1)
        // The ? in the strip asks for it, as often as it is clicked.
        first.showGuide()
        first.showGuide()
        #expect(first.guideAsked == 3)
        // What the app remembers afterwards does not forget that the guide was shown.
        first.zoomSheet(1)
        first.choose(library: mac.library("Kids"))
        #expect(mac.appData.settings.guideShown == true)
        #expect(mac.appData.settings.sheetZoom != nil)

        // The next start of the app.
        let second = mac.model()
        await second.start()
        #expect(second.guideAsked == 0)
        second.showGuide()
        #expect(second.guideAsked == 1)
        #expect(mac.appData.settings.guideShown == true)
    }

    @Test("Whoever had the app before it had a guide is shown it once too, and what the app kept for them is as it was")
    func shownOnceToWhoeverHadTheApp() async throws {
        let mac = try MadeUpMac()
        // What an earlier version left: a folder chosen that is not there today, the
        // place and library last open, which are not to be found either, and how the
        // window was left.
        var kept = AppData.Settings()
        kept.chosenFolders = ["/Volumes/Not Plugged In/Songs"]
        kept.lastPlace = "a place that is not found today"
        kept.lastLibrary = "a library of it"
        kept.sheetZoom = 1.25
        kept.textZoom = 0.9
        kept.listsSplit = 0.5
        kept.sheetChordColour = "#7fe3ff"
        kept.textChordColour = "#c95fff"
        mac.appData.settings = kept
        #expect(mac.appData.settings == kept)

        let model = mac.model()
        await model.start()
        #expect(model.guideAsked == 1)
        var now = kept
        now.guideShown = true
        #expect(mac.appData.settings == now)
        // And it was read as it was: nothing of it was lost on the way.
        #expect(model.sheetZoom == 1.25 && model.sheetChordColour == "#7fe3ff")

        let again = mac.model()
        await again.start()
        #expect(again.guideAsked == 0)
        #expect(mac.appData.settings == now)
    }

    @Test("Settings that cannot be read are taken as not there: the guide is shown, as at a first start, and the app goes on")
    func settingsThatCannotBeRead() async throws {
        let mac = try MadeUpMac()
        try FileManager.default.createDirectory(at: mac.appData.folder, withIntermediateDirectories: true)
        try Data("{ this is not settings".utf8).write(to: mac.appData.folder.appendingPathComponent("settings.json"))
        let model = mac.model()
        await model.start()
        #expect(model.guideAsked == 1)
        #expect(mac.appData.settings.guideShown == true)
        #expect(!model.libraries.isEmpty)
    }

    @Test("Settings written before there was a guide, and settings with things in them this version does not know, are read all the same")
    func settingsOfOtherVersions() throws {
        let mac = try MadeUpMac()
        try FileManager.default.createDirectory(at: mac.appData.folder, withIntermediateDirectories: true)
        let file = mac.appData.folder.appendingPathComponent("settings.json")
        try Data(#"{"chosenFolders": ["/a"], "sheetZoom": 1.1}"#.utf8).write(to: file)
        #expect(mac.appData.settings.guideShown == nil)
        #expect(mac.appData.settings.chosenFolders == ["/a"] && mac.appData.settings.sheetZoom == 1.1)
        try Data(#"{"chosenFolders": [], "guideShown": true, "somethingLater": 3}"#.utf8).write(to: file)
        #expect(mac.appData.settings.guideShown == true)
    }

    @Test("Asked for in a window that no app stands behind, the guide does no harm; and the window's own words for it are what the spec says")
    func askedForWhereThereIsNoApp() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        model.open(song: mac.song("Amazing Grace"))
        let host = NSHostingView(rootView: LibraryWindow(model: model).frame(width: 900, height: 560))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 900, height: 560), styleMask: [.borderless], backing: .buffered, defer: false)
        window.contentView = host
        host.layoutSubtreeIfNeeded()
        Self.turn()
        let before = model.guideAsked
        model.showGuide()
        Self.turn()
        #expect(model.guideAsked == before + 1)
        // Nothing of the song was touched by it.
        #expect(model.shown?.name == "Amazing Grace" && model.refusal == nil)

        #expect(GuideWindow.title == "Pro7 Chord Editor Guide")
        #expect(GuideWindow.tip == "The guide: the ways to put a chord on, on one page. It is in the Help menu too.")
        let page = try thePage()
        #expect(page.contains(GuideWindow.title))
        #expect(page.contains(GuideWindow.tip))
    }

    /// A few turns of the run loop, for a view to hear what its model said. (A plain
    /// function: the run loop cannot be turned from one that waits.)
    private static func turn() {
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.2))
    }

    @Test("The guide's window is drawn with its words and its pictures; and where the guide cannot be found it says so, and is still a window")
    func theWindow() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        func drawn(_ view: some View, tall: Double) throws -> NSBitmapImageRep {
            let size = NSSize(width: 780, height: tall)
            let host = NSHostingView(rootView: view.frame(width: size.width, height: size.height))
            let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
            window.contentView = host
            host.frame = NSRect(origin: .zero, size: size)
            host.layoutSubtreeIfNeeded()
            Self.turn()
            let picture = try #require(host.bitmapImageRepForCachingDisplay(in: host.bounds))
            host.cacheDisplay(in: host.bounds, to: picture)
            return picture
        }
        /// How many different colours a picture has at a grid of its dots.
        func colours(_ picture: NSBitmapImageRep) -> Int {
            var seen: Set<String> = []
            for x in stride(from: 5, to: picture.pixelsWide, by: 7) {
                for y in stride(from: 5, to: picture.pixelsHigh, by: 7) {
                    if let colour = picture.colorAt(x: x, y: y) {
                        seen.insert("\(Int(colour.redComponent * 255)),\(Int(colour.greenComponent * 255)),\(Int(colour.blueComponent * 255))")
                    }
                }
            }
            return seen.count
        }
        // With the guide: words, the box of what to know first, and a picture of the
        // sheet, so a good many colours.
        let whole = colours(try drawn(GuideWindow(model: model), tall: 820))
        #expect(whole > 40, "the guide's window has only \(whole) colours in it")
        // With no guide to be found: one sentence, and so very few.
        let none = colours(try drawn(GuideWindow(model: model, guide: nil), tall: 820))
        #expect(none >= 2 && none < whole / 2, "\(none) colours with no guide, \(whole) with")
        // Every picture the guide names is one the window can show.
        let guide = try #require(GuideFiles.guide)
        for name in guide.picturesNamed {
            let image = try #require(GuidePicture.image(name), "the window has no picture \(name)")
            #expect(abs(image.size.width - GuideLook.width) < 0.5, "\(name) would be shown \(image.size.width) wide")
        }
        #expect(GuidePicture.image("no such picture") == nil)
    }
}
