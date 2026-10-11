// The ChordPro editor in the window (spec/04-chordpro-editor.md): what the model does
// with what is typed, when it saves, and the real text view typed into, a key at a time.
//
// All on a made-up Mac, in a folder of its own (MadeUpMac.swift). Its songs are copies
// of the test files. The rules of what may be typed are proved without a window in
// SongTests/ChordProEditorTests; here it is that the window keeps to them.

import AppKit
@testable import AppViews
import ChordRules
import Foundation
import Song
import Testing

@MainActor
@Suite("The ChordPro editor in the window")
struct ChordProTypingTests {
    private func open(_ name: String, on mac: MadeUpMac) async -> LibraryModel {
        let model = mac.model()
        // (No waiting about in a test: a save follows the typing at once when asked.)
        model.pauseBeforeSaving = .milliseconds(1)
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(name))
        // (A song opens on the chord sheet; these are of the other editor.)
        model.show(editor: .chordPro)
        return model
    }

    /// The text with the first of something in it replaced: what typing there leaves.
    private func with(_ old: String, as new: String, in model: LibraryModel) throws -> String {
        let text = try #require(model.chordPro?.text)
        let range = try #require(text.range(of: old))
        return text.replacingCharacters(in: range, with: new)
    }

    /// A box of a song's file as "place:name" for each chord.
    private func chords(of box: Int, in name: String, on mac: MadeUpMac) throws -> [String] {
        try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/\(name).pro")).textBoxes[box].chords.map { "\($0.at):\($0.name)" }
    }

    // ---- The model

    @Test("A song opened is shown as ChordPro text, to be typed in")
    func opened() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.chordPro?.text.hasPrefix("{c: Verse 1}\n[G]One two three [D]four\n") == true)
        #expect(model.chordPro?.barred == nil && model.canType && model.canChange)
        #expect(model.notice == "4 slides, 11 chords. A change is saved a moment after it is typed.")
        // One that cannot be read has no text, and nothing can be done to it.
        model.open(song: mac.song("Nonsense"))
        #expect(model.chordPro == nil && !model.canType && !model.canChange)
        #expect(model.textMayBecome("anything") == false)
    }

    @Test("A chord typed is saved a moment after the typing stops, with a copy of the file kept first")
    func typedAndSaved() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let original = try mac.bytes(of: "Plain Song")
        #expect(model.songsWithCopies.isEmpty)

        #expect(model.textMayBecome(try with("One two", as: "One []two", in: model)))
        #expect(model.textMayBecome(try with("One []two", as: "One [A]two", in: model)))
        // Not yet: the typing has only just stopped.
        #expect(try mac.bytes(of: "Plain Song") == original)
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 0, in: "Plain Song", on: mac) == ["0:G", "4:A", "14:D", "28:Em"])

        // What is shown of the song follows: its count, its mark in the list, and that
        // it has an earlier version now, which is the file as it was.
        #expect(model.shown?.chords == 12)
        #expect(model.marks[mac.song("Plain Song")]?.chords == 12)
        #expect(model.songsWithCopies == ["Plain Song"] && model.hasEarlierVersions)
        let copies = mac.appData.copies().copies(of: KeptSong(place: "Sunday", library: "Songs", name: "Plain Song"))
        #expect(copies.count == 1 && copies.first?.why == .beforeAChange)
        #expect(try Data(contentsOf: try #require(copies.first).url) == original)
        // The text is left exactly as typed, and is not set afresh.
        #expect(model.chordPro?.text.contains("[G]One [A]two three [D]four") == true)
        #expect(model.chordPro?.version == 1)
        #expect(model.refusal == nil)
    }

    @Test("What is not the song's words is not let through, and nothing is saved")
    func notTheWords() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let original = try mac.bytes(of: "Plain Song")
        let before = model.chordPro
        #expect(model.textMayBecome(try with("One two", as: "One xtwo", in: model)) == false)
        #expect(model.textMayBecome(try with("{c: Chorus}", as: "{c: Chorus 2}", in: model)) == false)
        await model.whatWasTypedIsSaved()
        model.settle()
        #expect(model.chordPro == before && model.refusal == nil)
        #expect(try mac.bytes(of: "Plain Song") == original && model.songsWithCopies.isEmpty)
    }

    @Test("UNTIL M7: one more chord on a line with no words is not let through, and the line under the song says why")
    func notYet() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.textMayBecome(try with("[G] [D/F#] [Em]", as: "[G] [D/F#] [Em] []", in: model)) == false)
        #expect(model.refusal == "A chord cannot yet be added to a line that has no words, or taken from one. Its chords can be renamed. Nothing was changed.")
        #expect(model.notice == model.refusal)
        // The next thing that is let through takes the sentence away. A rename is.
        #expect(model.textMayBecome(try with("[G] [D/F#] [Em]", as: "[G] [D] [Em]", in: model)))
        #expect(model.refusal == nil)
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 3, in: "Plain Song", on: mac) == ["0:G", "2:D", "4:Em"])
    }

    @Test("Opening another song saves what was being typed in the one that is left")
    func leaving() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        model.pauseBeforeSaving = .seconds(60)
        #expect(model.textMayBecome(try with("Thirteen", as: "Thir[A]teen", in: model)))
        model.open(song: mac.song("Amazing Grace"))
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B", "27:A"])
        #expect(model.shown?.name == "Amazing Grace" && model.chordPro?.text.contains("A[G]mazing [G7]grace") == true)
        #expect(model.chordPro?.version == 2)
        // And so does leaving the app, or quitting it: `settle` is what those call.
        #expect(model.textMayBecome(try with("grace", as: "[F#m7]grace", in: model)))
        model.settle()
        #expect(try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/Amazing Grace.pro")).usedChords.contains("F#m7"))
    }

    @Test("While ProPresenter is running nothing can be typed; and if it was started unseen, the save is refused and the text is the song again")
    func proPresenterRunning() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let original = try mac.bytes(of: "Plain Song")
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        // ProPresenter is started before the pause is over, and the app has not been
        // told: only the save itself asks.
        mac.proPresenterIsRunning = true
        await model.whatWasTypedIsSaved()
        #expect(try mac.bytes(of: "Plain Song") == original && model.songsWithCopies.isEmpty)
        #expect(model.refusal == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
        #expect(model.proPresenterIsRunning && !model.canType && !model.canChange)
        #expect(model.chordPro?.text.contains("[A]") == false && model.chordPro?.version == 2)
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)) == false)
        // Quit, and the app come to the front again: it can be typed in.
        mac.proPresenterIsRunning = false
        await model.refresh()
        #expect(model.canType && model.chordPro?.version == 2)
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 0, in: "Plain Song", on: mac) == ["0:G", "4:A", "14:D", "28:Em"])
    }

    @Test("If what was typed cannot be saved when the song is left, the song is not left: the sentence is seen first")
    func notLeftUnsaid() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        model.pauseBeforeSaving = .seconds(60)
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        // ProPresenter is started unseen, and another song is clicked.
        mac.proPresenterIsRunning = true
        model.open(song: mac.song("Amazing Grace"))
        #expect(model.songID == mac.song("Plain Song") && model.shown?.name == "Plain Song")
        #expect(model.refusal == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
        #expect(model.chordPro?.text.contains("[A]") == false)
        // Nor is another library opened, nor the earlier versions, nor a key set.
        model.pauseBeforeSaving = .seconds(60)
        mac.proPresenterIsRunning = false
        await model.refresh()
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        mac.proPresenterIsRunning = true
        model.choose(library: mac.library("Kids"))
        #expect(model.library?.name == "Songs" && model.shown?.name == "Plain Song" && model.refusal != nil)
        // Clicked again, with nothing left unsaved, the other song opens.
        model.open(song: mac.song("Amazing Grace"))
        #expect(model.shown?.name == "Amazing Grace" && model.refusal == nil)
    }

    @Test("A file changed by something else is not saved over: the line says so and offers the song read again")
    func changedByAnother() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        // Something else changes the file meanwhile: here, its key.
        try mac.change("Plain Song", daysAgo: 0) { try $0.setKey("E") }
        let theirs = try mac.bytes(of: "Plain Song")
        await model.whatWasTypedIsSaved()
        #expect(try mac.bytes(of: "Plain Song") == theirs)
        #expect(model.refusal?.hasPrefix("This song's file has been changed by something else") == true)
        #expect(model.offersOpeningAgain)
        model.openAgain()
        #expect(model.shown?.key == "E" && model.refusal == nil && !model.offersOpeningAgain)
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 0, in: "Plain Song", on: mac) == ["0:G", "4:A", "14:D", "28:Em"])
    }

    @Test("When the app comes to the front the song stays as it is being typed, unless its file has changed underneath")
    func comingToTheFront() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.textMayBecome(try with("Five six", as: "Five []six", in: model)))
        await model.refresh()
        // Still the same sitting: the empty brackets are there, and the text was not set afresh.
        #expect(model.chordPro?.version == 1 && model.chordPro?.text.contains("Five []six") == true)
        try mac.change("Plain Song", daysAgo: 0) { try $0.setKey("E") }
        await model.refresh()
        #expect(model.chordPro?.version == 2 && model.chordPro?.text.contains("Five []six") == false)
        #expect(model.shown?.key == "E")
    }

    @Test("The two keys can be set from the song's header; neither changes a chord")
    func theKeys() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.shown?.key == "G" && model.shown?.shownKey == "G")
        #expect(model.keys.count == 26 && model.keys.contains("G") && model.keys.contains("Em"))
        let chordsBefore = try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/Plain Song.pro")).textBoxes.map(\.chords)

        model.setShownKey("A")
        #expect(model.shown?.key == "G" && model.shown?.shownKey == "A" && model.refusal == nil)
        // Someone chose to show it in another key, so setting the key leaves that alone.
        model.setKey("E")
        #expect(model.shown?.key == "E" && model.shown?.shownKey == "A")
        let now = try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/Plain Song.pro"))
        #expect(now.key == "E" && now.shownKey == "A" && now.textBoxes.map(\.chords) == chordsBefore)
        #expect(model.songsWithCopies == ["Plain Song"])
        // What was being typed is saved before the key is.
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        model.setKey("D")
        #expect(try chords(of: 0, in: "Plain Song", on: mac) == ["0:G", "4:A", "14:D", "28:Em"])
        // While ProPresenter runs, a key is refused like anything else.
        mac.proPresenterIsRunning = true
        model.setKey("C")
        #expect(model.shown?.key == "D" && model.refusal == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
    }

    @Test("The song that is open stays in the list when its first chord takes it out of what the list is narrowed to")
    func staysListed() async throws {
        let mac = try MadeUpMac()
        // A song with no chords: Plain Song with every chord taken off, through the song itself.
        try mac.change("Plain Song", daysAgo: 0) { song in
            for box in song.textBoxes where !Chords.isPlaceholders(box.words) {
                try song.setChords(slide: box.slide, element: box.element, [], joined: true)
            }
        }
        let model = await open("Plain Song", on: mac)
        model.filter = .withoutChords
        #expect(model.listedSongs.map(\.name) == ["Plain Song"] || model.listedSongs.map(\.name).contains("Plain Song"))
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        await model.whatWasTypedIsSaved()
        #expect(model.marks[mac.song("Plain Song")]?.hasChords == true)
        #expect(model.listedSongs.map(\.name).contains("Plain Song"))
        // Left for another song, it is no longer one without chords.
        model.open(song: nil)
        #expect(!model.listedSongs.map(\.name).contains("Plain Song"))
    }

    @Test("An earlier version put back is what the text then shows, as a new sitting")
    func putBack() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let original = try mac.bytes(of: "Plain Song")
        #expect(model.textMayBecome(try with("One two", as: "One [A]two", in: model)))
        // The panel is opened with the typing still unsaved: it is saved first, so the
        // copy listed is of the file as it was before it.
        model.showEarlierVersions()
        #expect(model.earlier?.copies.count == 1)
        model.putBackPicked()
        #expect(try mac.bytes(of: "Plain Song") == original)
        #expect(model.chordPro?.text.contains("[A]") == false && model.chordPro?.version == 2)
        #expect(model.shown?.chords == 11 && model.marks[mac.song("Plain Song")]?.chords == 11)
        model.takeBack()
        #expect(model.chordPro?.text.contains("[G]One [A]two") == true && model.chordPro?.version == 3)
    }

    @Test("The log names files and never has a chord or a word of a song in it")
    func theLog() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        #expect(model.textMayBecome(try with("Thirteen", as: "Thir[F#m7]teen", in: model)))
        await model.whatWasTypedIsSaved()
        mac.proPresenterIsRunning = true
        model.setKey("E")
        let log = try String(contentsOf: mac.appData.folder.appendingPathComponent("log.txt"), encoding: .utf8)
        #expect(log.contains("Saved Plain Song.pro.") && log.contains("A change to Plain Song.pro was refused."))
        #expect(!log.contains("F#m7") && !log.contains("Thirteen"))
    }

    // ---- The text view itself, typed into

    /// The real text view, wired to a model as the window wires it, in a window that is
    /// never shown.
    private func textView(for model: LibraryModel) throws -> (ChordProText, NSWindow) {
        let editor = ChordProText(asks: .init(typed: { model.textMayBecome($0) }, good: { model.chordPro?.text }))
        // (No sound in a test.)
        editor.saysNo = {}
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 600, height: 400), styleMask: [.borderless], backing: .buffered, defer: false)
        // (A window made in code lets go of itself when closed unless told not to, and
        // the test still holds it.)
        window.isReleasedWhenClosed = false
        window.contentView = editor.scroll
        editor.show(try #require(model.chordPro), canType: model.canType)
        window.makeFirstResponder(editor.text)
        return (editor, window)
    }

    @Test("The text can be made larger and smaller, with its buttons and with Command and the plus, minus and 0 keys: only its size changes, and the size is remembered apart from the sheet's")
    func theSizeOfTheText() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let editor = ChordProText(asks: .init(typed: { model.textMayBecome($0) }, good: { model.chordPro?.text }, zoom: { model.zoomText($0) }))
        editor.saysNo = {}
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 600, height: 400), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = editor.scroll
        defer { window.close() }
        editor.setScale(model.textZoom)
        editor.show(try #require(model.chordPro), canType: model.canType)
        window.makeFirstResponder(editor.text)
        func key(_ characters: String, _ flags: NSEvent.ModifierFlags) throws -> NSEvent {
            try #require(NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: flags, timestamp: 0, windowNumber: window.windowNumber,
                                          context: nil, characters: characters, charactersIgnoringModifiers: characters, isARepeat: false, keyCode: 0))
        }
        /// The size of the letters at a place of the text, and whether they are bold.
        func letters(at place: Int) -> (size: Double, bold: Bool) {
            let font = editor.text.textStorage?.attribute(.font, at: place, effectiveRange: nil) as? NSFont
            return (Double(font?.pointSize ?? 0), font?.fontDescriptor.symbolicTraits.contains(.bold) ?? false)
        }
        let before = editor.text.string
        let chord = try place(of: "[G]", in: editor)
        let word = try place(of: "One", in: editor)
        #expect(model.textZoom == 1 && letters(at: word).size == 14 && letters(at: chord) == (14, true))
        editor.text.setSelectedRange(NSRange(location: word + 2, length: 0))

        #expect(editor.text.performKeyEquivalent(with: try key("-", .command)))
        #expect(model.textZoom == 0.9)
        #expect(abs(letters(at: word).size - 12.6) < 0.01 && !letters(at: word).bold)
        #expect(abs(letters(at: chord).size - 12.6) < 0.01 && letters(at: chord).bold)
        // Not a character of the text, nor where the caret is, nor the song.
        #expect(editor.text.string == before && editor.text.selectedRange().location == word + 2)
        #expect(!model.canUndo && model.chordPro?.text == before)
        // What is typed next is the new size too.
        editor.text.setSelectedRange(NSRange(location: word, length: 0))
        editor.text.insertText("[", replacementRange: editor.text.selectedRange())
        #expect(abs(letters(at: word).size - 12.6) < 0.01)
        model.settle()

        #expect(editor.text.performKeyEquivalent(with: try key("=", .command)))
        #expect(editor.text.performKeyEquivalent(with: try key("+", [.command, .shift])))
        #expect(model.textZoom == 1.1 && abs(letters(at: chord).size - 15.4) < 0.01)
        #expect(editor.text.performKeyEquivalent(with: try key("0", .command)))
        #expect(model.textZoom == 1 && letters(at: chord).size == 14)
        // A key that is not one of these is not taken: Command with 1 is the header's.
        #expect(!editor.text.performKeyEquivalent(with: try key("1", .command)))

        // From its two buttons, to its ends; and the sheet's zoom is another thing.
        for _ in 0..<12 { model.zoomText(1) }
        #expect(model.textZoom == 1.5 && !model.canZoomTextIn && model.canZoomTextOut)
        for _ in 0..<12 { model.zoomText(-1) }
        #expect(model.textZoom == 0.5 && !model.canZoomTextOut)
        #expect(model.sheetZoom == LibraryModel.usualSheetZoom)
        model.zoomSheet(1)
        let again = mac.model()
        await again.start()
        #expect(again.textZoom == 0.5 && again.sheetZoom == 0.9)
    }

    private func place(of string: String, in editor: ChordProText) throws -> Int {
        let range = (editor.text.string as NSString).range(of: string)
        try #require(range.location != NSNotFound)
        return range.location
    }

    private func type(_ string: String, in editor: ChordProText) {
        editor.text.insertText(string, replacementRange: editor.text.selectedRange())
    }

    private func line(with string: String, in editor: ChordProText) -> String {
        editor.text.string.components(separatedBy: "\n").first { $0.contains(string) } ?? ""
    }

    @Test("Typed into: the words cannot be typed over; a bracket brings its other; a chord is typed between them and saved")
    func typingInTheView() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        let before = editor.text.string
        #expect(before == model.chordPro?.text)

        var saidNo = 0
        editor.saysNo = { saidNo += 1 }

        // In the middle of a word of the song: nothing happens, the caret stays, and
        // the sound for "no" is made.
        let inWord = try place(of: "Thirteen", in: editor) + 4
        editor.text.setSelectedRange(NSRange(location: inWord, length: 0))
        type("x", in: editor)
        #expect(editor.text.string == before && editor.text.selectedRange() == NSRange(location: inWord, length: 0))
        #expect(saidNo == 1)

        // A bracket brings its other with it, and the caret is between them.
        type("[", in: editor)
        #expect(line(with: "Thir", in: editor) == "Thir[]teen")
        #expect(editor.text.selectedRange() == NSRange(location: inWord + 1, length: 0))
        // A chord is typed between them, a character at a time.
        type("G", in: editor)
        type("/", in: editor)
        type("B", in: editor)
        #expect(line(with: "Thir", in: editor) == "Thir[G/B]teen")
        // What could not become a chord is not let in.
        type("*", in: editor)
        #expect(line(with: "Thir", in: editor) == "Thir[G/B]teen")
        // "]" typed at the "]" steps over it.
        type("]", in: editor)
        #expect(line(with: "Thir", in: editor) == "Thir[G/B]teen")
        #expect(editor.text.selectedRange() == NSRange(location: inWord + 5, length: 0))
        // And the model has every character of it. Only the "x" and the "*" were not taken.
        #expect(model.chordPro?.text == editor.text.string)
        #expect(saidNo == 2)

        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B", "27:G/B"])
        // The saving did not set the text afresh from under the person typing.
        #expect(line(with: "Thir", in: editor) == "Thir[G/B]teen")
        #expect(editor.text.selectedRange() == NSRange(location: inWord + 5, length: 0))
    }

    @Test("Typed into: Backspace just after a bracket, or Delete just before one, takes the whole chord; elsewhere they are the keys they always are")
    func deletingInTheView() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }

        // Backspace just after the "]" of "[G/B]" in "e[G/B]leven".
        let chord = try place(of: "[G/B]", in: editor)
        editor.text.setSelectedRange(NSRange(location: chord + 5, length: 0))
        editor.text.doCommand(by: #selector(NSResponder.deleteBackward(_:)))
        #expect(line(with: "Nine", in: editor) == "[C]Nine ten eleven twelve")
        #expect(editor.text.selectedRange() == NSRange(location: chord, length: 0))
        // Delete just before the "[" of "[C]".
        let first = try place(of: "[C]Nine", in: editor)
        editor.text.setSelectedRange(NSRange(location: first, length: 0))
        editor.text.doCommand(by: #selector(NSResponder.deleteForward(_:)))
        #expect(line(with: "Nine", in: editor) == "Nine ten eleven twelve")
        // Backspace among the words would take a letter, and is not let through.
        let before = editor.text.string
        editor.text.setSelectedRange(NSRange(location: try place(of: "Nine", in: editor) + 2, length: 0))
        editor.text.doCommand(by: #selector(NSResponder.deleteBackward(_:)))
        #expect(editor.text.string == before)
        // Inside a chord's name it takes one character of the name.
        let em = try place(of: "[Em]seven", in: editor)
        editor.text.setSelectedRange(NSRange(location: em + 3, length: 0))
        editor.text.doCommand(by: #selector(NSResponder.deleteBackward(_:)))
        #expect(line(with: "seven", in: editor) == "Five six [E]seven eight")

        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == [])
        #expect(try chords(of: 0, in: "Plain Song", on: mac) == ["0:G", "14:D", "28:E"])
    }

    @Test("Typed into: the text view's own undo takes back what was typed, and what it leaves is saved like anything typed")
    func undoInTheView() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        let before = editor.text.string
        let undo = try #require(editor.text.undoManager)

        editor.text.setSelectedRange(NSRange(location: try place(of: "Thirteen", in: editor), length: 0))
        type("[", in: editor)
        editor.text.breakUndoCoalescing()
        type("A", in: editor)
        editor.text.breakUndoCoalescing()
        #expect(line(with: "Thirteen", in: editor) == "[A]Thirteen")
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B", "23:A"])

        while undo.canUndo { undo.undo() }
        #expect(editor.text.string == before && model.chordPro?.text == before)
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B"])
        undo.redo()
        #expect(line(with: "Thirteen", in: editor).hasPrefix("[") && model.chordPro?.text == editor.text.string)
        // The Edit menu's Undo and Redo reach the text view as these two messages, and
        // it answers them with this same undo of its own.
        editor.text.undo(nil)
        #expect(editor.text.string == before && model.chordPro?.text == before)
        editor.text.redo(nil)
        #expect(line(with: "Thirteen", in: editor).hasPrefix("["))
        editor.text.undo(nil)
        #expect(editor.text.string == before && model.chordPro?.text == before)
        #expect(window.firstResponder === editor.text)
        #expect(editor.text.responds(to: #selector(ChordProText.View.undo(_:))) && editor.text.responds(to: #selector(ChordProText.View.redo(_:))))
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B"])
    }

    @Test("Typed into: what is pasted is held to the same rule, and a pasted bracket is not given its other")
    func pastingInTheView() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        // A pasteboard of the test's own: the person's is not touched.
        let board = NSPasteboard(name: NSPasteboard.Name("Pro7ChordEditorTests-" + UUID().uuidString))
        defer { board.releaseGlobally() }
        func paste(_ string: String, at place: Int) {
            board.clearContents()
            board.setString(string, forType: .string)
            editor.text.setSelectedRange(NSRange(location: place, length: 0))
            _ = editor.text.readSelection(from: board)
        }
        var saidNo = 0
        editor.saysNo = { saidNo += 1 }
        // (Something is typed first, so that a paste is seen to follow typing and still
        // not be taken for it: a bracket typed in a group's name, which is not let in.)
        editor.text.setSelectedRange(NSRange(location: 3, length: 0))
        type("[", in: editor)
        #expect(editor.text.string.hasPrefix("{c: Verse 1}\n") && saidNo == 1)
        let before = editor.text.string
        let here = try place(of: "Thirteen", in: editor)
        // Words pasted among the words, and a bracket by itself, are not let in.
        paste("more words ", at: here)
        #expect(editor.text.string == before)
        paste("[", at: here)
        #expect(editor.text.string == before && saidNo == 3)
        // A chord is, brackets and all; and two with the song's own words between.
        paste("[Am7]", at: here)
        #expect(line(with: "Thirteen", in: editor) == "[Am7]Thirteen")
        editor.text.setSelectedRange(NSRange(location: try place(of: "Sing it loud", in: editor), length: 12))
        board.clearContents()
        board.setString("Sing [E]it [A]loud", forType: .string)
        _ = editor.text.readSelection(from: board)
        #expect(line(with: "loud", in: editor) == "[D]Sing [E]it [A]loud")
        #expect(model.chordPro?.text == editor.text.string)
        await model.whatWasTypedIsSaved()
        #expect(try chords(of: 1, in: "Plain Song", on: mac) == ["0:C", "10:G/B", "23:Am7"])
        #expect(try chords(of: 2, in: "Plain Song", on: mac) == ["0:C", "8:G", "12:D", "17:E", "20:A"])
    }

    @Test("Typed into: chords are coloured as they are typed, and a group's name is greyed; the text is never changed by it")
    func coloursInTheView() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        let storage = try #require(editor.text.textStorage)
        func colour(at place: Int) -> NSColor? {
            storage.attribute(.foregroundColor, at: place, effectiveRange: nil) as? NSColor
        }
        #expect(colour(at: 0) == NSColor.secondaryLabelColor)                                  // {c: Verse 1}
        #expect(colour(at: try place(of: "[G]One", in: editor)) == NSColor.systemOrange)
        #expect(colour(at: try place(of: "[G]One", in: editor) + 2) == NSColor.systemOrange)   // its "]"
        #expect(colour(at: try place(of: "One two", in: editor)) == NSColor.textColor)
        // A chord typed is coloured as it goes in.
        let here = try place(of: "Thirteen", in: editor)
        editor.text.setSelectedRange(NSRange(location: here, length: 0))
        type("[", in: editor)
        type("D", in: editor)
        #expect(colour(at: here) == NSColor.systemOrange && colour(at: here + 1) == NSColor.systemOrange && colour(at: here + 2) == NSColor.systemOrange)
        #expect(colour(at: here + 3) == NSColor.textColor)
        #expect(editor.text.string == model.chordPro?.text)
        await model.whatWasTypedIsSaved()
    }

    @Test("Typed into: when the text is set from the song it takes the place of what was typed, and the undo of that is forgotten")
    func setFromTheSong() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        editor.text.setSelectedRange(NSRange(location: try place(of: "Thirteen", in: editor), length: 0))
        type("[", in: editor)
        type("A", in: editor)
        // ProPresenter is found to be running when the save comes: refused, and the
        // text is the song's again.
        mac.proPresenterIsRunning = true
        await model.whatWasTypedIsSaved()
        editor.show(try #require(model.chordPro), canType: model.canType)
        #expect(line(with: "Thirteen", in: editor) == "Thirteen")
        #expect(editor.text.undoManager?.canUndo == false)
        #expect(!editor.text.isEditable)
        // Nothing can be typed while it runs.
        type("[", in: editor)
        #expect(line(with: "Thirteen", in: editor) == "Thirteen")
        // The same song's text made again leaves the caret at the place it was (which
        // was after the "[A" that is gone, two along); another song opened starts at
        // its top.
        let thirteen = try place(of: "Thirteen", in: editor)
        #expect(editor.text.selectedRange() == NSRange(location: thirteen + 2, length: 0))
        mac.proPresenterIsRunning = false
        await model.refresh()
        model.open(song: mac.song("Amazing Grace"))
        editor.show(try #require(model.chordPro), canType: model.canType)
        #expect(editor.text.string.hasPrefix("{c: Verse 1}\nA[G]mazing") && editor.text.selectedRange() == NSRange(location: 0, length: 0))
        #expect(editor.text.isEditable)
    }

    @Test("Typed into: a song whose words have brackets of their own is shown and takes nothing")
    func barredInTheView() async throws {
        let mac = try MadeUpMac()
        // Plain Song with a bracket put among its words, in the file's RTF.
        let url = mac.place.appendingPathComponent("Libraries/Songs/Plain Song.pro")
        var bytes = try Data(contentsOf: url)
        let word = try #require(bytes.range(of: Data("Thirteen".utf8)))
        bytes.replaceSubrange(word, with: Data("Thir[een".utf8))
        try bytes.write(to: url)
        let model = await open("Plain Song", on: mac)
        #expect(model.chordPro?.barred?.contains("square brackets of their own") == true)
        #expect(!model.canType && model.canChange)
        let (editor, window) = try textView(for: model)
        defer { window.close() }
        #expect(!editor.text.isEditable && editor.text.string.contains("Thir[een"))
        // Its keys can still be set.
        model.setKey("E")
        #expect(model.shown?.key == "E" && model.refusal == nil)
    }
}
