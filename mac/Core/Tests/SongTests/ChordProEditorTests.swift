// The ChordPro editor with no window in it (spec/04-chordpro-editor.md): what may be
// typed, the help with brackets, and what a pause in the typing saves.
//
// There are no vectors for these: the rules live in the reference's QML. The cases are
// those of its scripted test (reference/simple-presenter/tests/chords-scripted-test.qml,
// "ChordPro": typing over a word, into a group's name, a chord in each kind of line, a
// paste), and the page's rules one by one. All are MINE, NOT A VECTOR.
//
// Plain Song, as the editor has it, line for line:
//
//    0  {c: Verse 1}
//    1  [G]One two three [D]four
//    2  Five six [Em]seven eight
//    3
//    4  [C]Nine ten e[G/B]leven twelve
//    5  Thirteen
//    6
//    7  {c: Chorus}
//    8  [C]Sing it [G]out
//    9  [D]Sing it loud
//   10
//   11  {c: Outro}
//   12  [G] [D/F#] [Em]

import ChordRules
import Foundation
import ProFormat
@testable import Song
import Testing
import TestData

@Suite("The ChordPro editor, with no window")
struct ChordProEditorTests {
    static let plain = "made-by-simple-presenter/Plain Song.pro"
    static let blank = "made-by-simple-presenter/Blank Slides.pro"

    /// The text with the first of something in it replaced: what typing there leaves.
    private func with(_ old: String, as new: String, in text: String) throws -> String {
        let range = try #require(text.range(of: old))
        return text.replacingCharacters(in: range, with: new)
    }

    /// Types: asks the editor to take its text with the first `old` in it made `new`.
    @discardableResult
    private func type(_ editor: ChordProEditor, _ old: String, as new: String) throws -> ChordProEditor.Verdict {
        editor.take(try with(old, as: new, in: editor.text))
    }

    /// A box of a file on disk as "place:name" for each chord.
    private func chords(of box: Int, at url: URL) throws -> [String] {
        try OpenSong(opening: url).textBoxes[box].chords.map { "\($0.at):\($0.name)" }
    }

    /// Plain Song with one thing in its message changed, opened to be changed.
    private func plainSong(_ bench: Workbench, named name: String, _ change: (inout RVData_Presentation, [TextBox]) throws -> Void) throws -> OpenSong {
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.plain))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.plain)).textBoxes
        try change(&made, boxes)
        return try OpenSong(opening: bench.put(made.serializedData(), named: name), safeguards: bench.safeguards)
    }

    /// Gives the n-th chord of a box another name, in the message.
    private func rename(chord n: Int, of element: String, to name: String, in made: inout RVData_Presentation) throws {
        let here = try #require(place(of: element, in: made))
        var all = entries(at: here, in: made)
        let index = try #require(all.indices.filter { isChord(all[$0]) }.dropFirst(n).first)
        all[index].chord = name
        setEntries(all, at: here, in: &made)
    }

    // ---- The text

    @Test("The text is the song's lines, and passes its own rule", arguments: (try? TestData.presentationPaths()) ?? [])
    func theTextAsMade(path: String) throws {
        let song = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(path))
        let editor = ChordProEditor(song: song)
        #expect(editor.text == SongText.text(of: song.textBoxes))
        #expect(editor.lines == SongText.lines(of: song.textBoxes))
        #expect(editor.barred == nil)
        #expect(editor.take(editor.text) == .taken)
        // Every bracket in it is a chord `chordsOf` reads, at the same place.
        for line in editor.lines where line.kind == .words || line.kind == .chordsAlone {
            let units = Array(line.text.utf16)
            let pairs = ChordProEditor.pairs(in: units)
            let read = Chords.chordsOf(Words(units: units))
            #expect(pairs.map(\.at) == read.map(\.at))
            #expect(pairs.map { Chords.tidied(String(decoding: units[($0.open + 1)..<$0.close], as: UTF16.self)) } == read.map(\.name))
        }
    }

    // ---- What may be typed (rule 6)

    @Test("The words cannot be typed over, added to or taken from")
    func theWords() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let before = editor.text
        #expect(try type(editor, "Thirteen", as: "Thirxteen") == .notTheWords)
        #expect(try type(editor, "Thirteen", as: "Thirten") == .notTheWords)
        #expect(try type(editor, "Thirteen", as: "thirteen") == .notTheWords)
        #expect(try type(editor, "six ", as: "six  ") == .notTheWords)
        #expect(try type(editor, "Sing it loud", as: "") == .notTheWords)
        #expect(editor.text == before)
    }

    @Test("A group's name and the gap between slides take nothing")
    func groupsAndGaps() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let before = editor.text
        #expect(try type(editor, "{c: Chorus}", as: "{c: Chorus 2}") == .notTheWords)
        #expect(try type(editor, "{c: Chorus}", as: "{c: Chorus}[G]") == .notTheWords)
        // The gap before the chorus: the empty line after "Thirteen".
        #expect(try type(editor, "Thirteen\n\n", as: "Thirteen\n[G]\n") == .notTheWords)
        #expect(try type(editor, "Thirteen\n\n", as: "Thirteen\n \n") == .notTheWords)
        #expect(editor.text == before)
    }

    @Test("The number of lines cannot change")
    func theLines() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        #expect(try type(editor, "Thirteen", as: "Thirteen\n") == .notTheWords)
        #expect(try type(editor, "four\nFive", as: "fourFive") == .notTheWords)
        #expect(try type(editor, "four\nFive", as: "four\n\nFive") == .notTheWords)
        #expect(editor.take("") == .notTheWords)
        // Not at the end either, where every line before is still what it was: a line
        // more, with or without anything on it, or the last line gone.
        #expect(editor.take(editor.text + "\n") == .notTheWords)
        #expect(editor.take(editor.text + "\n[G]") == .notTheWords)
        #expect(editor.take(editor.text.components(separatedBy: "\n").dropLast().joined(separator: "\n")) == .notTheWords)
    }

    @Test("A chord may be typed among the words, a character at a time")
    func aChordAmongTheWords() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        // "[" brings its "]" with it (the help below); then the name, a character at a
        // time, each of which is let through.
        #expect(try type(editor, "Five six", as: "Five []six") == .taken)
        #expect(try type(editor, "Five []six", as: "Five [F]six") == .taken)
        #expect(try type(editor, "Five [F]six", as: "Five [F#]six") == .taken)
        #expect(try type(editor, "Five [F#]six", as: "Five [F#m]six") == .taken)
        #expect(try type(editor, "Five [F#m]six", as: "Five [F#m7]six") == .taken)
        #expect(try type(editor, "Five [F#m7]six", as: "Five [F#m7/]six") == .taken)
        #expect(try type(editor, "Five [F#m7/]six", as: "Five [F#m7/A]six") == .taken)
        #expect(editor.text.contains("Five [F#m7/A]six [Em]seven eight"))
        // And taken out again, whole.
        #expect(try type(editor, "[F#m7/A]", as: "") == .taken)
        // One after the last word, and one before the first.
        #expect(try type(editor, "Thirteen", as: "[D]Thirteen[G]") == .taken)
        // Spaces round a name are let through (it is trimmed), and a small letter.
        #expect(try type(editor, "Sing it loud", as: "Sing [ a ]it loud") == .taken)
    }

    @Test("What is in brackets must be able to become a chord")
    func notAChord() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let before = editor.text
        #expect(try type(editor, "Five six", as: "Five [x]six") == .notTheWords)
        #expect(try type(editor, "Five six", as: "Five [H]six") == .notTheWords)
        #expect(try type(editor, "Five six", as: "Five [Coda]six") == .taken)   // "Coda" starts as a C chord would
        #expect(try type(editor, "[Coda]", as: "[*Coda]") == .notTheWords)
        #expect(try type(editor, "[Coda]", as: "") == .taken)
        #expect(try type(editor, "[G/B]", as: "[G/B*]") == .notTheWords)
        #expect(editor.text == before)
    }

    @Test("A bracket with no other of its pair is not let through")
    func strayBrackets() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let before = editor.text
        #expect(try type(editor, "Five six", as: "Five [six") == .notTheWords)
        #expect(try type(editor, "Five six", as: "Five ]six") == .notTheWords)
        #expect(try type(editor, "Five six", as: "Five [[G]six") == .notTheWords)
        #expect(try type(editor, "Five six", as: "Five [G]]six") == .notTheWords)
        // Brackets that would reach over a line break are two strays.
        #expect(try type(editor, "four\nFive", as: "four[G\n]Five") == .notTheWords)
        #expect(editor.text == before)
    }

    @Test("A paste is held to the same rule: the whole text at once, with chords or with other words")
    func aPaste() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let pasted = """
            {c: Verse 1}
            [Em]One [C]two [G]three [D]four
            [Em]Five [C]six seven [D]eight

            Nine ten eleven twelve
            Thir[A]teen

            {c: Chorus}
            Sing it out
            Sing it [D]loud[G]

            {c: Outro}
            [C] [G] [D]
            """
        #expect(editor.take(pasted) == .taken)
        #expect(editor.text == pasted)
        #expect(editor.take(pasted.replacingOccurrences(of: "Nine ten", with: "Ten nine")) == .notTheWords)
        #expect(editor.text == pasted)
    }

    @Test("What is typed is held against the words unit for unit, not as text that looks the same")
    func unitForUnit() {
        // One "e" with an acute accent written as one character, and as two.
        let one = "caf\u{E9}"
        let two = "cafe\u{301}"
        #expect(one == two && Array(one.utf16) != Array(two.utf16))
        let line = SongLine(kind: .words, text: one, slide: 0, box: 0, element: "x", words: Words(one))
        #expect(ChordProEditor.allows(Array(one.utf16), lines: [line], names: []))
        #expect(ChordProEditor.allows(Array("[G]\(one)".utf16), lines: [line], names: []))
        #expect(!ChordProEditor.allows(Array(two.utf16), lines: [line], names: []))
    }

    @Test("A line of chords alone: its chords may be renamed, with spaces between and nothing else")
    func chordsAlone() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        #expect(try type(editor, "[G] [D/F#] [Em]", as: "[G] [] [Em]") == .taken)
        #expect(try type(editor, "[G] [] [Em]", as: "[G] [D] [Em]") == .taken)
        #expect(try type(editor, "[G] [D] [Em]", as: "[G]   [D]\t[Em] ") == .taken)
        #expect(try type(editor, "[G]   [D]", as: "[G] x [D]") == .notTheWords)
        #expect(try type(editor, "[G]   [D]", as: "[G] [x] [D]") == .notTheWords)
    }

    @Test("UNTIL M7: a line of chords alone keeps as many chords as it has, and a blank slide takes none; each says why")
    func notYet() throws {
        let sentence = "A chord cannot yet be added to a line that has no words, or taken from one. Its chords can be renamed. Nothing was changed."
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let before = editor.text
        #expect(try type(editor, "[G] [D/F#] [Em]", as: "[G] [D/F#] [Em] [C]") == .notYet(sentence))
        #expect(try type(editor, "[G] [D/F#] [Em]", as: "[G] [Em]") == .notYet(sentence))
        #expect(try type(editor, "[G] [D/F#] [Em]", as: "[G] [D/F#] [Em] []") == .notYet(sentence))
        #expect(editor.text == before)

        // Blank Slides: line 1 is its intro, a blank slide; line 2 the gap after it.
        let blank = ChordProEditor(song: try Workbench().open(Self.blank))
        #expect(blank.lines[1].kind == .blankSlide && blank.lines[2].kind == .gap)
        var lines = blank.text.components(separatedBy: "\n")
        lines[1] = "[A]"
        #expect(blank.take(lines.joined(separator: "\n")) == .notYet(sentence))
        lines[1] = "A"
        #expect(blank.take(lines.joined(separator: "\n")) == .notTheWords)
        // The line after it is the gap, and takes nothing whatever is built.
        lines[1] = ""
        lines[2] = "[A]"
        #expect(blank.take(lines.joined(separator: "\n")) == .notTheWords)
        // The last slide has no text box at all, and is a line of the text all the same.
        #expect(blank.lines.last?.kind == .blankSlide && blank.lines.last?.element == "")
    }

    // ---- Different from the reference, on purpose (rule 22)

    @Test("A name the song already had that is no chord is let through as it is, and saved back")
    func aNameThatIsNoChord() throws {
        let bench = try Workbench()
        let song = try plainSong(bench, named: "Bars.pro") { made, boxes in
            try rename(chord: 0, of: boxes[0].element, to: "C /// | Am ///", in: &made)
        }
        let editor = ChordProEditor(song: song)
        #expect(editor.barred == nil)
        #expect(editor.text.contains("[C /// | Am ///]One two three [D]four"))
        // Something else in its box is changed, and it is saved back as it was.
        #expect(try type(editor, "Five six", as: "Five [A]six") == .taken)
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:C /// | Am ///", "14:D", "24:A", "28:Em"])
        // Only that name, exactly: another that is no chord is still not let through,
        // and it can be taken out and put back.
        #expect(try type(editor, "[C /// | Am ///]", as: "[C /// | Am //]") == .notTheWords)
        #expect(try type(editor, "[C /// | Am ///]", as: "") == .taken)
        #expect(try type(editor, "One two", as: "[C /// | Am ///]One two") == .taken)
    }

    // ---- Words that have brackets of their own (rule 20)

    @Test("A song whose words have square brackets is shown and cannot be changed here, and says so")
    func wordsWithBrackets() throws {
        let bench = try Workbench()
        let song = try plainSong(bench, named: "Brackets.pro") { made, boxes in
            let here = try #require(place(of: boxes[1].element, in: made))
            let rtf = String(decoding: made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData, as: UTF8.self)
            #expect(rtf.contains("Thirteen"))
            made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData =
                Data(rtf.replacingOccurrences(of: "Thirteen", with: "Thir[teen]").utf8)
        }
        let editor = ChordProEditor(song: song)
        #expect(editor.text.contains("Thir[teen]"))
        #expect(editor.barred == "This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text.")
        let before = try Data(contentsOf: song.url)
        #expect(try type(editor, "Five six", as: "Five [A]six") == .notTheWords)
        #expect(try editor.save() == false)
        #expect(try Data(contentsOf: song.url) == before)
    }

    @Test("A song with a chord whose name has a bracket in it cannot be changed here either, and says so")
    func aNameWithABracket() throws {
        let bench = try Workbench()
        let song = try plainSong(bench, named: "Name.pro") { made, boxes in
            try rename(chord: 0, of: boxes[2].element, to: "A]", in: &made)
        }
        let editor = ChordProEditor(song: song)
        #expect(editor.barred?.hasPrefix("This song cannot be changed as ChordPro text") == true)
        #expect(try type(editor, "Five six", as: "Five [A]six") == .notTheWords)
    }

    // ---- Saving (rules 13 to 18)

    @Test("A chord typed is in the file after a save, at its place among the words; and nothing typed saves nothing")
    func saving() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        let original = try Data(contentsOf: song.url)

        #expect(try editor.save() == false)
        #expect(try Data(contentsOf: song.url) == original && bench.copiesKept.isEmpty && !song.canUndo)

        try type(editor, "One two", as: "One [A]two")
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:G", "4:A", "14:D", "28:Em"])
        // A copy of the file as it was is kept first, and the text is left as typed.
        #expect(bench.copiesKept == [original])
        #expect(editor.text.contains("[G]One [A]two three [D]four"))
        // Saved, there is nothing more to save.
        let saved = try Data(contentsOf: song.url)
        #expect(try editor.save() == false)
        #expect(try Data(contentsOf: song.url) == saved)
        // The other boxes are as they were.
        for box in 1...3 {
            #expect(try chords(of: box, at: song.url) == song.textBoxes[box].chords.map { "\($0.at):\($0.name)" })
        }
        #expect(try chords(of: 1, at: song.url) == ["0:C", "10:G/B"])
    }

    @Test("A chord typed after the last word of a line hangs on the line's last character")
    func afterTheLastWord() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        try type(editor, "[D]four", as: "[D]four[C]")
        try type(editor, "Thirteen", as: "Thirteen[A]")
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:G", "14:D", "17:C", "28:Em"])
        #expect(try chords(of: 1, at: song.url) == ["0:C", "10:G/B", "30:A"])
    }

    @Test("Empty brackets are left out of what is saved and left in the text")
    func emptyBrackets() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        let original = try Data(contentsOf: song.url)
        try type(editor, "Five six", as: "Five []six")
        try type(editor, "Thirteen", as: "Thir[ ]teen")
        #expect(try editor.save() == false)
        #expect(try Data(contentsOf: song.url) == original && bench.copiesKept.isEmpty)
        #expect(editor.text.contains("Five []six") && editor.text.contains("Thir[ ]teen"))
        // Filled in, it is a chord, and is saved; the other is still left in the text.
        try type(editor, "Five []six", as: "Five [Am]six")
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:G", "14:D", "24:Am", "28:Em"])
        #expect(editor.text.contains("Thir[ ]teen"))
    }

    @Test("The same chords put on here and asked of the song directly give the same file")
    func theSameFileBothWays() throws {
        let typed = try Workbench()
        let asked = try Workbench()
        let here = try typed.open(Self.plain)
        let there = try asked.open(Self.plain)
        let editor = ChordProEditor(song: here)
        #expect(editor.take("""
            {c: Verse 1}
            [Em]One [C]two [G]three [D]four
            Five six seven eight[f#m7/a]

            [C]Nine ten e[G/B]leven twelve
            Thir[A]teen

            {c: Chorus}
            Sing it out
            [D]Sing it [  Bb ]loud

            {c: Outro}
            [C] [G] [D]
            """) == .taken)
        #expect(try editor.save())

        let boxes = there.textBoxes
        // (As the sheet would ask: a line of chords alone first, then each box's list.)
        try there.setChordsAlone(slide: 3, element: boxes[3].element, line: 0, names: ["C", "G", "D"])
        try there.setChords(slide: 0, element: boxes[0].element, [Chord(at: 0, name: "Em"), Chord(at: 4, name: "C"), Chord(at: 8, name: "G"), Chord(at: 14, name: "D"), Chord(at: 38, name: "F#m7/A")], joined: true)
        try there.setChords(slide: 1, element: boxes[1].element, [Chord(at: 0, name: "C"), Chord(at: 10, name: "G/B"), Chord(at: 27, name: "A")], joined: true)
        try there.setChords(slide: 2, element: boxes[2].element, [Chord(at: 12, name: "D"), Chord(at: 20, name: "Bb")], joined: true)
        #expect(try Data(contentsOf: here.url) == Data(contentsOf: there.url))
    }

    @Test("A line of chords alone is saved as its names in order; one with a bracket still empty waits")
    func savingChordsAlone() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        try type(editor, "[G] [D/F#] [Em]", as: "[G] [] [Em]")
        try type(editor, "Thirteen", as: "[A]Thirteen")
        #expect(try editor.save())
        // The other box is saved; the line waits, as it was in the file.
        #expect(try chords(of: 1, at: song.url) == ["0:C", "10:G/B", "23:A"])
        #expect(try chords(of: 3, at: song.url) == ["0:G", "2:D/F#", "4:Em"])
        #expect(editor.text.contains("[G] [] [Em]"))
        try type(editor, "[G] [] [Em]", as: "[G] [d] [Em]")
        #expect(try editor.save())
        #expect(try chords(of: 3, at: song.url) == ["0:G", "2:D", "4:Em"])
        #expect(try OpenSong(opening: song.url).textBoxes[3].words == song.textBoxes[3].words)
        #expect(editor.text.contains("[G] [d] [Em]"))
    }

    @Test("A box with a line of chords alone and a line of words: each keeps its chords when the other is typed in")
    func bothKindsInOneBox() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/It Is Well.pro")
        let editor = ChordProEditor(song: song)
        // Its fourth box: a line of four chords alone, then a line of words with two.
        let box = song.textBoxes[3]
        #expect(box.chords.map { "\($0.at):\($0.name)" } == ["0:Am", "2:F", "4:C", "6:G", "8:Am", "22:F"])
        let own = editor.lines.filter { $0.box == 3 }
        #expect(own.map(\.kind) == [.chordsAlone, .words] && own[0].text == "[Am] [F] [C] [G]")
        // A chord typed at the end of its words: the line of chords alone keeps its four.
        #expect(try type(editor, own[1].text, as: own[1].text + "[E]") == .taken)
        #expect(try editor.save())
        let last = box.words.length - 1
        #expect(try chords(of: 3, at: song.url) == ["0:Am", "2:F", "4:C", "6:G", "8:Am", "22:F", "\(last):E"])
        // One of the four renamed: the words keep their three.
        #expect(try type(editor, "[Am] [F] [C] [G]", as: "[Am] [Dm] [C] [G]") == .taken)
        #expect(try editor.save())
        #expect(try chords(of: 3, at: song.url) == ["0:Am", "2:Dm", "4:C", "6:G", "8:Am", "22:F", "\(last):E"])
        #expect(try OpenSong(opening: song.url).textBoxes[3].words == box.words)
    }

    @Test("A song with blank slides can be typed in like any other, on its lines of words")
    func amongBlankSlides() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.blank)
        let editor = ChordProEditor(song: song)
        #expect(try type(editor, "That saved", as: "That [Em]saved") == .taken)
        #expect(try editor.save())
        #expect(try chords(of: 2, at: song.url) == ["5:Em"])
        // The blank slides are as they were, the one with no text box among them.
        let now = try OpenSong(opening: song.url).textBoxes
        #expect(now.map(\.blank) == song.textBoxes.map(\.blank) && now.filter(\.blank).count == 3)
        #expect(now.last?.element == "")
    }

    @Test("All the changes of one saving undo as one step")
    func oneStep() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        let original = try Data(contentsOf: song.url)
        try type(editor, "One two", as: "One [A]two")
        try type(editor, "Sing it loud", as: "Sing it [E]loud")
        try type(editor, "[G] [D/F#] [Em]", as: "[Em] [D/F#] [G]")
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:G", "4:A", "14:D", "28:Em"])
        #expect(try chords(of: 2, at: song.url) == ["0:C", "8:G", "12:D", "20:E"])
        #expect(try chords(of: 3, at: song.url) == ["0:Em", "2:D/F#", "4:G"])
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
        #expect(!song.canUndo)
        // When the song changes from elsewhere, the text is made again from it.
        editor.reload(keepingWhatIsTyped: false)
        #expect(editor.text == SongText.text(of: song.textBoxes) && !editor.text.contains("[A]"))
    }

    @Test("A box is saved only when what the text says of it has changed: one with an untidy name, or a chord past its words, is left alone")
    func onlyWhatWasTypedIn() throws {
        let bench = try Workbench()
        var untouched: [Entry] = []
        var second = ""
        let song = try plainSong(bench, named: "Untidy.pro") { made, boxes in
            // The second box: a name that is not tidy, and a chord past the end.
            try rename(chord: 0, of: boxes[1].element, to: "c", in: &made)
            let here = try #require(place(of: boxes[1].element, in: made))
            var all = entries(at: here, in: made)
            var stray = try #require(all.first(where: isChord))
            stray.chord = "E"
            stray.range.start = 90
            stray.range.end = 91
            all.append(stray)
            setEntries(all, at: here, in: &made)
            untouched = all
            second = boxes[1].element
        }
        let editor = ChordProEditor(song: song)
        #expect(editor.text.contains("[c]Nine ten e[G/B]leven twelve"))
        // A chord is typed in the FIRST box.
        try type(editor, "One two", as: "One [A]two")
        #expect(try editor.save())
        #expect(try chords(of: 0, at: song.url) == ["0:G", "4:A", "14:D", "28:Em"])
        // The second box's entries are, to the last field, what they were.
        let now = try message(at: song.url)
        #expect(entries(at: try #require(place(of: second, in: now)), in: now) == untouched)
    }

    @Test("A save that is refused changes nothing, and says why")
    func refused() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plain)
        let editor = ChordProEditor(song: song)
        let original = try Data(contentsOf: song.url)
        try type(editor, "One two", as: "One [A]two")
        bench.proPresenterIsRunning = true
        #expect { try editor.save() } throws: { ($0 as? Refusal)?.reason == .proPresenterIsRunning }
        #expect(try Data(contentsOf: song.url) == original && bench.copiesKept.isEmpty)
        // "Nothing was changed": the window then makes the text again from the song.
        editor.reload(keepingWhatIsTyped: false)
        #expect(!editor.text.contains("[A]"))
        // Once ProPresenter has quit, the same thing typed is saved.
        bench.proPresenterIsRunning = false
        try type(editor, "One two", as: "One [A]two")
        #expect(try editor.save())
    }

    // ---- Help with the brackets (rules 9 to 11)

    @Test("A bracket brings its other with it, and the caret is left between them")
    func openBracket() {
        let text = "Born to raise"
        #expect(ChordProEditor.helpWithTyping("[", in: text, selection: 5..<5) == .init(range: 5..<5, with: "[]", caret: 6))
        // Not over a selection, and not for any other character.
        #expect(ChordProEditor.helpWithTyping("[", in: text, selection: 5..<7) == nil)
        #expect(ChordProEditor.helpWithTyping("G", in: text, selection: 5..<5) == nil)
        #expect(ChordProEditor.helpWithTyping("[G]", in: text, selection: 5..<5) == nil)
    }

    @Test("A closing bracket typed at a closing bracket steps over it")
    func closeBracket() {
        let text = "Born [G]to raise"
        #expect(ChordProEditor.helpWithTyping("]", in: text, selection: 7..<7) == .init(range: 7..<7, with: "", caret: 8))
        #expect(ChordProEditor.helpWithTyping("]", in: text, selection: 6..<6) == nil)
        #expect(ChordProEditor.helpWithTyping("]", in: text, selection: 16..<16) == nil)
        #expect(ChordProEditor.helpWithTyping("]", in: text, selection: 6..<7) == nil)
    }

    @Test("Backspace just after a bracket, or Delete just before one, takes the whole chord")
    func wholeChord() {
        //          0123456789012345678
        let text = "Born [G/B]to raise"
        let whole = ChordProEditor.Edit(range: 5..<10, with: "", caret: 5)
        // Backspace after "[" and after "]".
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 6..<6) == whole)
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 10..<10) == whole)
        // Delete before "[" and before "]".
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 5..<5) == whole)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 9..<9) == whole)
        // Elsewhere the key does what it always does: inside the name, among the words,
        // at the ends of the text, and over a selection.
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 8..<8) == nil)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 7..<7) == nil)
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 3..<3) == nil)
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 0..<0) == nil)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 18..<18) == nil)
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 6..<8) == nil)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 5..<6) == nil)
    }

    @Test("Only a chord on its own line: a bracket whose other is on another line is left to the key")
    func notOverALineBreak() {
        //          01234 5678
        let text = "ab[G\ncd]e"
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 3..<3) == nil)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 2..<2) == nil)
        #expect(ChordProEditor.helpWithBackspace(in: text, selection: 8..<8) == nil)
        #expect(ChordProEditor.helpWithDelete(in: text, selection: 7..<7) == nil)
        // The second of two chords on a line, and the first of the next line.
        //         0123456 789012
        let two = "[C]a[G]\n[D]b"
        #expect(ChordProEditor.helpWithBackspace(in: two, selection: 7..<7) == .init(range: 4..<7, with: "", caret: 4))
        #expect(ChordProEditor.helpWithDelete(in: two, selection: 8..<8) == .init(range: 8..<11, with: "", caret: 8))
        #expect(ChordProEditor.helpWithBackspace(in: two, selection: 9..<9) == .init(range: 8..<11, with: "", caret: 8))
        // A bracket that closes nothing, after a chord that is whole, does not take that
        // chord with it. (Text that passes the rule has no such bracket; this is the
        // reference's own care, kept.)
        #expect(ChordProEditor.helpWithBackspace(in: "[C]ab]", selection: 6..<6) == nil)
        #expect(ChordProEditor.helpWithDelete(in: "[C]ab]", selection: 5..<5) == nil)
    }

    @Test("What the help does is then held to the rule like anything typed")
    func helpIsHeldToTheRule() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        func apply(_ edit: ChordProEditor.Edit) -> ChordProEditor.Verdict {
            var units = Array(editor.text.utf16)
            units.replaceSubrange(edit.range, with: Array(edit.with.utf16))
            return editor.take(String(decoding: units, as: UTF16.self))
        }
        // "[" among the words is let through; in a group's name it is not.
        let among = try #require(editor.text.range(of: "to raise") ?? editor.text.range(of: "six"))
        let caret = editor.text.utf16.distance(from: editor.text.utf16.startIndex, to: among.lowerBound.samePosition(in: editor.text.utf16)!)
        #expect(apply(try #require(ChordProEditor.helpWithTyping("[", in: editor.text, selection: caret..<caret))) == .taken)
        #expect(apply(try #require(ChordProEditor.helpWithTyping("[", in: editor.text, selection: 3..<3))) == .notTheWords)
        // Backspace on the bracket of a chord among the words takes it; on one of a
        // line of chords alone it would take a chord from the line, which is M7's.
        let d = try #require(editor.text.range(of: "[D]four"))
        let afterOpen = editor.text.utf16.distance(from: editor.text.utf16.startIndex, to: d.lowerBound.samePosition(in: editor.text.utf16)!) + 1
        #expect(apply(try #require(ChordProEditor.helpWithBackspace(in: editor.text, selection: afterOpen..<afterOpen))) == .taken)
        #expect(editor.text.contains("three four"))
        let end = editor.text.utf16.count
        if case .notYet = apply(try #require(ChordProEditor.helpWithBackspace(in: editor.text, selection: end..<end))) {} else {
            Issue.record("Taking a chord from a line of chords alone should be refused until M7.")
        }
    }

    // ---- Which slide, and the colours

    @Test("The caret's line says which slide is picked, and a slide picked puts the caret at the start of its first line")
    func whichSlide() throws {
        let editor = ChordProEditor(song: try Workbench().open(Self.plain))
        let lines = editor.text.components(separatedBy: "\n")
        var start = 0
        var slides: [Int?] = []
        for line in lines {
            slides.append(editor.slide(at: start))
            #expect(editor.slide(at: start + line.utf16.count) == editor.slide(at: start))
            start += line.utf16.count + 1
        }
        #expect(slides == [nil, 0, 0, nil, 1, 1, nil, nil, 2, 2, nil, nil, 3])
        // "{c: Verse 1}\n" is 13 units: the first slide's first line starts after it.
        #expect(editor.start(ofSlide: 0) == 13)
        #expect(editor.start(ofSlide: 3) == editor.text.utf16.count - "[G] [D/F#] [Em]".utf16.count)
        #expect(editor.start(ofSlide: 4) == nil)
        for slide in 0...3 {
            #expect(editor.slide(at: try #require(editor.start(ofSlide: slide))) == slide)
        }
    }

    @Test("Chords are coloured brackets and all, and the lines in curly brackets greyed")
    func colours() {
        //          0123456789012 3456789012345 6 78901
        let text = "{c: Verse 1}\n[G]One [D/F#]two\n\n[] x"
        let found = ChordProEditor.colouring(of: text)
        #expect(found.greyed == [0..<12])
        #expect(found.chords == [13..<16, 20..<26, 31..<33])
        #expect(ChordProEditor.colouring(of: "") == .init())
        // A bracket with no other is no chord, and is not coloured.
        #expect(ChordProEditor.colouring(of: "a[G b c[D]").chords == [7..<10])
    }
}
