// Chords with no words: adding to a line of chords alone and taking from one, and the
// first chords of a blank slide. spec/01-file-format.md, "Chords with no words" and
// what follows it; spec/11-plan.md, M7, whose proofs these are:
//
//   - the RTF of every other line of a changed box is byte for byte what it was;
//   - a blank slide whose chords are taken off again in the same sitting leaves the
//     file byte for byte what it was.
//
// (The third, that a song so changed opens in ProPresenter with its chords in place,
// is not a thing a test can tell: it is asked of the maintainer.)
//
// They run on copies of two kinds of file. Those Simple Presenter made, which write a
// line of chords alone their own way; and Chords With No Words, which
// tools/make-fixtures.py made from the one song of ProPresenter's own, in the shapes
// real songs were found to have (the head of that tool lists them). This is the one
// place the app writes into a song's words, and the one place it adds anything to a
// slide, so each test also holds that NOTHING ELSE in the file changed.

@testable import ChordRules
import Foundation
import ProFormat
@testable import Song
import Testing
import TestData

@Suite("Chords with no words")
struct ChordsWithNoWordsTests {
    static let shaped = "shaped-as-real-ones/Chords With No Words.pro"
    static let files = ((try? TestData.presentationPaths()) ?? []).filter { $0.hasPrefix("made-by-simple-presenter/") } + [shaped]

    // ---- Looking inside a file

    /// A text box's element, as the file has it.
    private func element(_ id: String, at url: URL) throws -> RVData_Slide.Element {
        let made = try message(at: url)
        let here = try #require(place(of: id, in: made), "no text box \(id)")
        return made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element]
    }

    private func rtf(of id: String, at url: URL) throws -> [UInt8] {
        Array(try element(id, at: url).element.text.rtfData)
    }

    /// A message with one text box's words and chords made another's: what a change of
    /// chords with no words may change, and nothing else.
    private func with(wordsAndChordsOf id: String, from other: RVData_Presentation, in made: RVData_Presentation) throws -> RVData_Presentation {
        var made = made
        let here = try #require(place(of: id, in: made))
        let there = try #require(place(of: id, in: other))
        let theirs = other.cues[there.cue].actions[there.action].slide.presentation.baseSlide.elements[there.element].element.text
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData = theirs.rtfData
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.attributes.customAttributes
            = entries(at: here, in: made).filter { !isChord($0) } + theirs.attributes.customAttributes.filter(isChord)
        return made
    }

    /// The bytes these stand-ins are written with.
    private func written(_ standIns: Words) -> [UInt8] {
        standIns.units.flatMap { $0 == 0x20 ? [UInt8(0x20)] : Array("\\uc0\\u\($0) ".utf8) }
    }

    /// The stand-in a line hangs its chords on.
    private func standIn(of line: Words) -> UInt16 {
        !line.contains(0x200B) && line.contains(0x20) ? 0x20 : 0x200B
    }

    private func shown(_ rtf: [UInt8]) -> String {
        String(decoding: rtf, as: UTF8.self).replacingOccurrences(of: "\n", with: "\u{23CE}")
    }

    // ---- The file that was made for this

    @Test("Chords With No Words is the shapes it was made to be")
    func theShapes() throws {
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        #expect(boxes.map(\.slide) == Array(0..<12))
        #expect(boxes.map(\.blank) == [true, false, false, false, false, false, false, false, false, false, true, true])
        #expect(boxes[11].element.isEmpty && boxes.dropLast().allSatisfy { !$0.element.isEmpty })
        func alone(_ box: Int) -> [[UInt16]] {
            boxes[box].words.lines.filter { Chords.isPlaceholders($0) }.map(\.units)
        }
        let z: UInt16 = 0x200B, q: UInt16 = 0x2001, s: UInt16 = 0x20
        #expect(alone(3) == [[z, q, z, q, z, q, z]] && boxes[3].chords.map(\.name) == ["E", "B", "C#m", "A"])
        #expect(alone(5) == [[s, q, s, q, s]] && boxes[5].chords.map(\.at) == [0, 2, 4])
        #expect(alone(6) == [[s]] && boxes[6].chords == [Chord(at: 0, name: "E")])
        #expect(alone(7) == [[s, q, s]] && boxes[7].words.lines.count == 2 && boxes[7].chords.map(\.at) == [0, 11, 29, 31])
        #expect(alone(8) == [[s, q, s, q, s]] && boxes[8].words.lines.count == 3 && boxes[8].chords.map(\.at) == [0, 2, 4, 11, 33])
        #expect(alone(9) == [[z, q, z, q, z, q, z], [z, q, z]] && boxes[9].chords.count == 6)
        for words in [1, 2, 4] {
            #expect(alone(words).isEmpty && boxes[words].words.lines.count == 2 && boxes[words].chords.isEmpty)
        }
    }

    // ---- A line of chords alone

    @Test("Every line of chords alone, given one more chord, one fewer, three more, and none: the line is a stand-in for each chord (and one zero-width space where it has none), the chords are on them, and those of the lines after it have moved with their words",
          arguments: files)
    func everyLine(path: String) throws {
        let bench = try Workbench()
        var tried = 0
        let untouched = try bench.open(path).textBoxes
        for box in untouched where !box.blank {
            let lines = box.words.lines
            for (line, old) in lines.enumerated() where Chords.isPlaceholders(old) {
                let start = lines[..<line].reduce(0) { $0 + $1.length + 1 }
                let had = box.chords.filter { $0.at >= start && $0.at <= start + old.length }
                for count in Set([had.count + 1, max(0, had.count - 1), had.count + 3, 0]).sorted() {
                    tried += 1
                    let song = try bench.open(path)
                    let names = (0..<count).map { ["Em", "C", "G/B", "D", "Am7", "F#m", "Bb"][$0 % 7] }
                    try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: names)

                    // (A line with no chord is one zero-width space, whatever it
                    // hung its chords on, and is a line of chords alone still.)
                    let new = count == 0 ? Chords.placeholders(1) : Chords.placeholders(count, standIn: standIn(of: old))
                    var meant = lines
                    meant[line] = new
                    var chords: [Chord] = box.chords.filter { $0.at < start }
                    chords += names.enumerated().map { Chord(at: start + $0.offset * 2, name: $0.element) }
                    chords += box.chords.filter { $0.at > start + old.length }.map { Chord(at: $0.at + new.length - old.length, name: $0.name) }

                    // What was asked is what the song now says, and what the file says.
                    // (No slide is left a blank one, nor any box left out: a line with
                    // no chord has its zero-width space, which no stray space is.)
                    let read = try OpenSong(opening: song.url).textBoxes
                    #expect(read == song.textBoxes, "\(path), slide \(box.slide), line \(line), to \(count)")
                    let now = try #require(read.first { $0.slide == box.slide && $0.element == box.element })
                    #expect(!now.blank && now.words.lines == meant, "\(path), slide \(box.slide), line \(line), to \(count)")
                    #expect(now.chords == chords, "\(path), slide \(box.slide), line \(line), to \(count)")
                    #expect(read.filter { $0.element != box.element } == untouched.filter { $0.element != box.element })

                    // Each chord of the line is written one long: its own stand-in.
                    let after = try message(at: song.url)
                    let here = try #require(place(of: box.element, in: after))
                    let ranges = entries(at: here, in: after).filter(isChord).filter { $0.range.start >= start && Int($0.range.start) < start + max(1, new.length) }
                    #expect(ranges.map { Int($0.range.end - $0.range.start) } == Array(repeating: 1, count: count))

                    // THE FIRST PROOF. Outside the line's own bytes, the box's RTF is
                    // what it was; and nothing else in the whole file has changed.
                    let before = try RVData_Presentation(serializedBytes: TestData.file(path))
                    let was = try rtf(of: box.element, at: TestData.root.appendingPathComponent("files").appendingPathComponent(path))
                    let made = try rtf(of: box.element, at: song.url)
                    let span = try #require(RTFWords.span(ofLine: line, of: was))
                    #expect(Array(made.prefix(span.lowerBound)) == Array(was.prefix(span.lowerBound)), "\(path), slide \(box.slide), line \(line), to \(count): before the line")
                    #expect(Array(made.suffix(was.count - span.upperBound)) == Array(was.suffix(was.count - span.upperBound)), "\(path), slide \(box.slide), line \(line), to \(count): after the line")
                    #expect(try with(wordsAndChordsOf: box.element, from: after, in: before) == after, "\(path), slide \(box.slide), line \(line), to \(count): something else changed")

                    // And it is one step, that undo takes back to the byte.
                    try song.undo()
                    #expect(!song.canUndo)
                    #expect(try Data(contentsOf: song.url) == TestData.file(path), "\(path), slide \(box.slide), line \(line), to \(count)")
                }
            }
        }
        if path == Self.shaped {
            #expect(tried >= 24)
        }
    }

    @Test("The bytes, as they are written: a line as Multitracks writes it grows after its last stand-in and shrinks leaving the fonts between; one on plain spaces is given plain spaces")
    func theBytes() throws {
        let bench = try Workbench()
        let source = TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)
        let boxes = try OpenSong(opening: source).textBoxes
        func changed(_ box: Int, line: Int = 0, to count: Int) throws -> [UInt8] {
            let song = try bench.open(Self.shaped)
            try song.setChordsAlone(slide: boxes[box].slide, element: boxes[box].element, line: line, names: Array(repeating: "G", count: count))
            return try rtf(of: boxes[box].element, at: song.url)
        }
        let z = written(Words(units: [0x200B])), q = written(Words(units: [0x2001]))
        let fonts = Array("\n\\f1 \\cf2 ".utf8), back = Array("\n\\f0 \\cf2 ".utf8)
        let uc = Array("\\uc0".utf8)

        // Slide 4: four chords, each em quad in another font. (Its first stand-in
        // has a `\uc0` before it like the rest, which is not the stand-in's own.)
        let four = try rtf(of: boxes[3].element, at: source)
        let opening = Array(four.prefix(try #require(RTFWords.span(ofLine: 0, of: four)).lowerBound - uc.count))
        #expect(shown(four) == shown(opening + z + fonts + q + back + z + fonts + q + back + z + fonts + q + back + z + Array("}".utf8)))
        #expect(shown(try changed(3, to: 5)) == shown(opening + z + fonts + q + back + z + fonts + q + back + z + fonts + q + back + z + q + z + Array("}".utf8)))
        // (What is left where a character was taken out is its `\uc0`, which is not
        // the character's own, and the fonts, which say how what follows is set.)
        #expect(shown(try changed(3, to: 2)) == shown(opening + z + fonts + q + back + z + fonts + uc + back + uc + fonts + uc + back + uc + Array("}".utf8)))

        // Slide 6: three chords on plain spaces.
        let three = try rtf(of: boxes[5].element, at: source)
        let before = Array(three.prefix(try #require(RTFWords.span(ofLine: 0, of: three)).lowerBound))
        #expect(shown(three) == shown(before + Array(" ".utf8) + q + Array(" ".utf8) + q + Array(" }".utf8)))
        #expect(shown(try changed(5, to: 4)) == shown(before + Array(" ".utf8) + q + Array(" ".utf8) + q + Array(" ".utf8) + q + Array(" }".utf8)))
        #expect(shown(try changed(5, to: 1)) == shown(before + Array(" ".utf8) + uc + uc + Array("}".utf8)))

        // Slide 9: three chords over two lines of words. One more: the words after
        // the line are the bytes they were.
        let over = try rtf(of: boxes[8].element, at: source)
        let span = try #require(RTFWords.span(ofLine: 0, of: over))
        #expect(shown(try changed(8, to: 4)) == shown(Array(over.prefix(span.upperBound)) + q + Array(" ".utf8) + Array(over.suffix(from: span.upperBound))))
    }

    @Test("As many chords as the line has stand-ins, with other names or in another order: only the chord entries change, and not a byte of the RTF")
    func renamed() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let source = TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)
        for box in song.textBoxes where !box.blank {
            for (line, old) in box.words.lines.enumerated() where Chords.isPlaceholders(old) {
                let count = (old.length + 1) / 2
                try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: (0..<count).map { "F\($0 + 2)" })
                #expect(try rtf(of: box.element, at: song.url) == rtf(of: box.element, at: source))
            }
        }
        #expect(song.usedChords.filter { $0.hasPrefix("F") }.count == 4 + 3 + 1 + 2 + 3 + 4 + 2)
    }

    @Test("A line that has both kinds of stand-in is given zero-width spaces: plain spaces are for a line that has them and no zero-width space")
    func bothKindsOfStandIn() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let source = TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)
        let boxes = try OpenSong(opening: source).textBoxes
        // The fourth slide's line: a zero-width space, a plain space, a zero-width
        // space, with a chord on each of the two.
        let four = try rtf(of: boxes[3].element, at: source)
        let opening = Array(four.prefix(try #require(RTFWords.span(ofLine: 0, of: four)).lowerBound))
        let z = written(Words(units: [0x200B]))
        let here = try #require(place(of: boxes[3].element, in: made))
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData = Data(opening + z.dropFirst(4) + [0x20] + z + Array("}".utf8))
        var entries = entries(at: here, in: made).filter { !isChord($0) }
        for (at, name) in [(0, "E"), (2, "B")] {
            var chord = Entry()
            chord.range.start = Int32(at)
            chord.range.end = Int32(at + 1)
            chord.chord = name
            entries.append(chord)
        }
        setEntries(entries, at: here, in: &made)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Both.pro"), safeguards: bench.safeguards)
        #expect(song.textBoxes[3].words == Words(units: [0x200B, 0x20, 0x200B]))
        try song.setChordsAlone(slide: 3, element: boxes[3].element, line: 0, names: ["E", "B", "A"])
        #expect(song.textBoxes[3].words == Chords.placeholders(3))
        #expect(song.textBoxes[3].chords.map(\.at) == [0, 2, 4])
    }

    @Test("A chord that stands on the line break that ends a line of chords alone is that line's, and goes with the line's chords")
    func onTheLinesOwnBreak() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        // The ninth slide: three chords on five places, a line break at 5, then two
        // lines of words with a chord each. One more chord is put on the break.
        let here = try #require(place(of: boxes[8].element, in: made))
        var all = entries(at: here, in: made)
        var chord = Entry()
        chord.range.start = 5
        chord.range.end = 6
        chord.chord = "D"
        all.insert(chord, at: try #require(all.firstIndex { isChord($0) && $0.range.start > 5 }))
        setEntries(all, at: here, in: &made)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "On The Break.pro"), safeguards: bench.safeguards)
        #expect(song.textBoxes[8].chords.map(\.at) == [0, 2, 4, 5, 11, 33])

        // Every chord taken off the line: the one on its break goes too, and is not
        // left standing on the one zero-width space the line then is; the words' two
        // move up by the four places the line is shorter.
        try song.setChordsAlone(slide: 8, element: boxes[8].element, line: 0, names: [])
        #expect(song.textBoxes[8].words.lines.map(\.length) == [1, 26, 23] && song.textBoxes[8].words.lines[0] == Chords.placeholders(1))
        #expect(song.textBoxes[8].chords == [Chord(at: 7, name: "C#m"), Chord(at: 29, name: "A")])
    }

    @Test("Names are tidied, and one that is nothing is left out and gets no stand-in")
    func namesAreTidied() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let box = song.textBoxes[5]
        try song.setChordsAlone(slide: box.slide, element: box.element, line: 0, names: [" g ", "", "  ", "bb/d"])
        let now = song.textBoxes[5]
        #expect(now.words == Chords.placeholders(2, standIn: 0x20))
        #expect(now.chords == [Chord(at: 0, name: "G"), Chord(at: 2, name: "Bb/D")])
    }

    @Test("A line whose stand-ins cannot be changed cleanly is refused in a sentence and nothing is changed; its chords can still be renamed")
    func cannotBeWritten() throws {
        let bench = try Workbench()
        // The fourth slide's stand-ins, written as an RTF that has a character follow
        // each to stand in for it (which Apple's text system never writes).
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        let here = try #require(place(of: boxes[3].element, in: made))
        let u = "\\u"
        let odd = "{\\rtf1\\ansi " + u + "8203?" + u + "8193?" + u + "8203?" + u + "8193?" + u + "8203?" + u + "8193?" + u + "8203?}"
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData = Data(odd.utf8)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Odd.pro"), safeguards: bench.safeguards)
        #expect(song.textBoxes[3].words == Chords.placeholders(4) && song.textBoxes[3].chords.count == 4)
        let saved = try Data(contentsOf: song.url)

        for names in [["E", "B", "C#m", "A", "E"], ["E", "B"], []] {
            let refused = #expect(throws: Refusal.self) { try song.setChordsAlone(slide: 3, element: boxes[3].element, line: 0, names: names) }
            #expect(refused?.reason == .cannotBeWritten)
            #expect(refused?.sentence == "A chord could not be added to this line, or taken from it: its text box is written in a way that cannot be changed here safely. Nothing was changed.")
        }
        #expect(try Data(contentsOf: song.url) == saved)
        #expect(!song.canUndo && bench.copiesKept.isEmpty)
        try song.setChordsAlone(slide: 3, element: boxes[3].element, line: 0, names: ["E", "B", "C#m", "F#"])
        #expect(song.textBoxes[3].chords.map(\.name) == ["E", "B", "C#m", "F#"])
    }

    @Test("The song's own rule and the sheet's stand-in for it give the same words and the same chords, for every line of chords alone and every blank slide", arguments: files)
    func asTheStandInSong(path: String) throws {
        let bench = try Workbench()
        var tried = 0
        let boxes = try bench.open(path).textBoxes
        for box in boxes {
            let lines = box.blank ? [0] : box.words.lines.indices.filter { Chords.isPlaceholders(box.words.lines[$0]) }
            for line in lines {
                for names in [["G"], ["G", "D", "Em", "C", "G"], []] {
                    tried += 1
                    let song = try bench.open(path)
                    let double = StandInSong(song.textBoxes, key: song.key)
                    for asked in [names, ["A", "E"], []] {
                        // (A slide that has been given a box is asked by that box's id.)
                        let mine = try #require(song.textBoxes.first { $0.slide == box.slide && ($0.element == box.element || box.element.isEmpty) })
                        let theirs = try #require(double.textBoxes.first { $0.slide == box.slide && ($0.element == box.element || box.element.isEmpty) })
                        try song.setChordsAlone(slide: box.slide, element: mine.element, line: line, names: asked)
                        try double.setChordsAlone(slide: box.slide, element: theirs.element, line: line, names: asked, joined: false)
                        let expected = double.textBoxes.map { one in
                            var one = one
                            if one.element == "(a text box added for the chords)" {
                                one.element = song.textBoxes.first { $0.slide == one.slide }?.element ?? ""
                            }
                            return one
                        }
                        #expect(song.textBoxes == expected, "\(path), slide \(box.slide), line \(line), \(asked)")
                    }
                }
            }
        }
        #expect(tried > 0 || !path.contains("Blank") && !path.contains("Words"))
    }

    // ---- A blank slide that has a text box

    @Test("The first chords of a blank slide give its box words: the opening of the nearest box that has words, and a stand-in for each chord. Only its RTF and its chords change")
    func aBlankBox() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let original = try Data(contentsOf: song.url)
        let before = try message(at: song.url)
        let blank = song.textBoxes[0]
        #expect(blank.blank && !blank.element.isEmpty)

        try song.setChordsAlone(slide: 0, element: blank.element, line: 0, names: ["e", "A/C#"])
        let now = song.textBoxes[0]
        #expect(now == TextBox(slide: 0, group: blank.group, groupColour: blank.groupColour, startsItsGroup: blank.startsItsGroup, element: blank.element,
                               words: Chords.placeholders(2), chords: [Chord(at: 0, name: "E"), Chord(at: 2, name: "A/C#")]))
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)

        // It is the first slide, so the nearest box with words is the one after it:
        // its RTF up to its first word, then the stand-ins, then the closing bracket.
        let model = try rtf(of: song.textBoxes[1].element, at: song.url)
        let words = try #require(String(decoding: model, as: UTF8.self).range(of: "Let the water"))
        let opening = Array(String(decoding: model, as: UTF8.self)[..<words.lowerBound].utf8)
        #expect(shown(try rtf(of: blank.element, at: song.url)) == shown(opening + written(Chords.placeholders(2)) + Array("}".utf8)))
        // The box had no settings that go by a stretch of text; it has its two chords.
        let after = try message(at: song.url)
        #expect(spell(entries(at: try #require(place(of: blank.element, in: after)), in: after)) == ["E 0-1", "A/C# 2-3"])
        #expect(entries(at: try #require(place(of: blank.element, in: after)), in: after).count == 2)
        // Nothing else in the file has changed: not the box's place, its size or how
        // its text is set, nor any other slide.
        #expect(try with(wordsAndChordsOf: blank.element, from: after, in: before) == after)

        // THE SECOND PROOF. Its chords taken off again in the same sitting: the file
        // is byte for byte what it was.
        try song.setChordsAlone(slide: 0, element: blank.element, line: 0, names: ["E"])
        try song.setChordsAlone(slide: 0, element: blank.element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
        #expect(song.textBoxes[0] == blank)
        // And again, the second time round; and by undo, each step of it.
        try song.setChordsAlone(slide: 0, element: blank.element, line: 0, names: ["G", "C", "D"])
        try song.setChordsAlone(slide: 0, element: blank.element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
        while song.canUndo {
            try song.undo()
        }
        #expect(try Data(contentsOf: song.url) == original)
        try song.redo()
        #expect(song.textBoxes[0].chords.map(\.name) == ["E", "A/C#"])
    }

    @Test("A blank box that holds a stray space is given words the same way; the box before it being blank too, its model is the nearest that is not, and what that box keeps for stretches of its text stays")
    func aBlankBoxWithASpace() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let original = try Data(contentsOf: song.url)
        let before = try message(at: song.url)
        let blank = song.textBoxes[10]
        let held = RTFWords.words(ofRTF: try rtf(of: blank.element, at: song.url))
        #expect(blank.blank && !blank.element.isEmpty && held == Words(" "))

        try song.setChordsAlone(slide: 10, element: blank.element, line: 0, names: ["B"])
        #expect(song.textBoxes[10].words == Chords.placeholders(1) && song.textBoxes[10].chords == [Chord(at: 0, name: "B")])
        // The nearest before it is the tenth slide, whose own words are stand-ins. Its
        // opening is all that is before its first stand-in's own bytes, the `\uc0`
        // among it.
        let model = try rtf(of: song.textBoxes[9].element, at: song.url)
        let opening = Array(model.prefix(try #require(RTFWords.span(ofLine: 0, of: model)).lowerBound))
        #expect(shown(try rtf(of: blank.element, at: song.url)) == shown(opening + written(Chords.placeholders(1)) + Array("}".utf8)))
        #expect(String(decoding: opening, as: UTF8.self).hasSuffix("\\strokec3 \\uc0"))
        let after = try message(at: song.url)
        let all = entries(at: try #require(place(of: blank.element, in: after)), in: after)
        let had = entries(at: try #require(place(of: blank.element, in: before)), in: before)
        #expect(had.count == 1 && all.count == 2 && !isChord(all[0]) && all[0] == had[0])
        #expect(spell(all) == ["B 0-1"])
        #expect(try with(wordsAndChordsOf: blank.element, from: after, in: before) == after)

        try song.setChordsAlone(slide: 10, element: blank.element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
    }

    // ---- A blank slide that has no text box

    @Test("The first chords of a slide with no text box give it one: a copy of the nearest box that has words, with a new id, nothing tying it to the other slide, no chords but its own, and stand-ins for words")
    func noBox() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let original = try Data(contentsOf: song.url)
        let before = try message(at: song.url)
        #expect(song.textBoxes[11].blank && song.textBoxes[11].element.isEmpty)

        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["E", "B", "A"])
        let now = song.textBoxes[11]
        #expect(!now.blank && now.words == Chords.placeholders(3) && now.chords.map(\.name) == ["E", "B", "A"] && now.chords.map(\.at) == [0, 2, 4])
        #expect(now.group == "Verse 4" && now.slide == 11)
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)

        // Its id is a new one, written as the file's others are: in capitals.
        let ids = Set(before.cues.flatMap { $0.actions.flatMap { $0.slide.presentation.baseSlide.elements.map(\.element.uuid.string) } })
        #expect(!ids.contains(now.element) && UUID(uuidString: now.element) != nil && now.element == now.element.uppercased())

        // The slide before it is blank, and is passed over: the model is the tenth
        // slide's box. The new one is that, but for what is said above.
        let after = try message(at: song.url)
        let here = try #require(place(of: now.element, in: after))
        let added = after.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element]
        let model = try element(song.textBoxes[9].element, at: song.url)
        #expect(after.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements.count == 1)
        #expect(added.element.name == model.element.name && added.element.name == "Default")
        #expect(!added.hasBuildIn && !added.hasBuildOut && added.childBuilds.isEmpty && added.dataLinks.isEmpty)
        #expect(!model.dataLinks.isEmpty, "the model was to have a link, to see that the copy has none")
        var same = model
        same.element.uuid = added.element.uuid
        same.element.text = added.element.text
        same.dataLinks = []
        #expect(same == added, "the copy is placed, sized and set otherwise than its model")
        #expect(added.element.bounds == model.element.bounds && added.element.fill == model.element.fill && added.info == model.info)
        var text = model.element.text
        text.rtfData = added.element.text.rtfData
        text.attributes.customAttributes = added.element.text.attributes.customAttributes
        #expect(text == added.element.text)
        // Its words: the model's opening and three stand-ins. Its settings by stretch
        // of text: the model's that are not chords, and its own three chords.
        let modelRTF = Array(model.element.text.rtfData)
        let opening = Array(modelRTF.prefix(try #require(RTFWords.span(ofLine: 0, of: modelRTF)).lowerBound))
        #expect(shown(Array(added.element.text.rtfData)) == shown(opening + written(Chords.placeholders(3)) + Array("}".utf8)))
        let all = added.element.text.attributes.customAttributes
        #expect(all.filter { !isChord($0) } == model.element.text.attributes.customAttributes.filter { !isChord($0) })
        #expect(spell(all) == ["E 0-1", "B 2-3", "A 4-5"])

        // Nothing else in the file has changed: with the box taken off, the message is
        // what it was.
        var without = after
        without.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements.remove(at: here.element)
        #expect(without == before)

        // THE SECOND PROOF, for a box that was added: the last chord taken off in the
        // same sitting takes the box off with it.
        try song.setChordsAlone(slide: 11, element: now.element, line: 0, names: ["E"])
        try song.setChordsAlone(slide: 11, element: now.element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
        #expect(song.textBoxes[11].blank && song.textBoxes[11].element.isEmpty)
        // Given chords again it is given another box, with another id; undone, none.
        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["D"])
        #expect(!song.textBoxes[11].element.isEmpty && song.textBoxes[11].element != now.element)
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
        // Redone, and its chord taken off: the box that came back goes too.
        try song.redo()
        try song.setChordsAlone(slide: 11, element: song.textBoxes[11].element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
    }

    @Test("A slide that has a shape and no text box: the box goes after the shape, and is named apart from it if the names would be one")
    func afterWhatIsThere() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        // The third slide's box is left a shape, with no text, called what the song's
        // text boxes are called; and a second shape beside it, called that with a 2.
        let here = try #require(place(of: boxes[2].element, in: made))
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.clearText()
        var second = made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element]
        second.element.uuid.string = "0000AAAA-0000-4000-8000-000000000002"
        second.element.name = "Default 2"
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements.append(second)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Shapes.pro"), safeguards: bench.safeguards)
        let original = try Data(contentsOf: song.url)
        #expect(song.textBoxes[2].blank && song.textBoxes[2].element.isEmpty)

        try song.setChordsAlone(slide: 2, element: "", line: 0, names: ["G"])
        let after = try message(at: song.url)
        let elements = after.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements
        #expect(elements.map(\.element.name) == ["Default", "Default 2", "Default 3"])
        #expect(elements.map(\.element.hasText) == [false, false, true])
        #expect(elements[2].element.uuid.string == song.textBoxes[2].element)
        // (The nearest box with words before it is the second slide's.)
        #expect(RTFWords.words(ofRTF: elements[2].element.text.rtfData) == Chords.placeholders(1))
        try song.setChordsAlone(slide: 2, element: song.textBoxes[2].element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
    }

    @Test("What tied the copied box to things on its own slide is cleared: how it comes on, how it goes off, and the steps its lines come in")
    func whatTiedTheModelToItsSlide() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        // The tenth slide's box, which the last slide's is modelled on, is given a
        // way on, a way off, and a step.
        let here = try #require(place(of: boxes[9].element, in: made))
        var build = RVData_Slide.Element.Build()
        build.delayTime = 2
        var step = RVData_Slide.Element.ChildBuild()
        step.delayTime = 1
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].buildIn = build
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].buildOut = build
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].childBuilds = [step]
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Builds.pro"), safeguards: bench.safeguards)
        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["G"])
        let added = try element(song.textBoxes[11].element, at: song.url)
        let model = try element(boxes[9].element, at: song.url)
        #expect(model.hasBuildIn && model.hasBuildOut && model.childBuilds.count == 1)
        #expect(!added.hasBuildIn && !added.hasBuildOut && added.childBuilds.isEmpty && added.dataLinks.isEmpty)
        // And nothing else of it differs from its model but its id, its words and its chords.
        var same = model
        same.clearBuildIn()
        same.clearBuildOut()
        same.childBuilds = []
        same.dataLinks = []
        same.element.uuid = added.element.uuid
        same.element.text = added.element.text
        #expect(same == added)
    }

    @Test("A box that is shown for its chords and has not a character in it has no opening to give, and is passed over for the next nearest")
    func aModelWithNoCharacter() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        // The second slide's box is left with ProPresenter's empty document for words,
        // and a chord all the same: shown, not blank, and with nowhere for words to begin.
        let empty = try rtf(of: boxes[0].element, at: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped))
        let here = try #require(place(of: boxes[1].element, in: made))
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData = Data(empty)
        var chord = Entry()
        chord.range.end = 1
        chord.chord = "G"
        made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.attributes.customAttributes.append(chord)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "No Character.pro"), safeguards: bench.safeguards)
        #expect(!song.textBoxes[1].blank && song.textBoxes[1].words.isEmpty && song.textBoxes[1].chords.count == 1)

        // The first slide is blank; the nearest box after it is that one; the next is
        // the third slide's, whose words are words.
        try song.setChordsAlone(slide: 0, element: boxes[0].element, line: 0, names: ["E"])
        let model = try rtf(of: boxes[2].element, at: song.url)
        let words = try #require(String(decoding: model, as: UTF8.self).range(of: "Be of sin"))
        let opening = Array(String(decoding: model, as: UTF8.self)[..<words.lowerBound].utf8)
        #expect(shown(try rtf(of: boxes[0].element, at: song.url)) == shown(opening + written(Chords.placeholders(1)) + Array("}".utf8)))
    }

    @Test("A slide whose file has not so much as an empty slide written for it: given a text box and then left without, it is not written one")
    func downToTheEmptyParts() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        // The last cue shown has a slide with nothing on it. Here it has not even
        // that: the part of the file that would hold its elements is left out.
        let rows = made.cueGroups.flatMap(\.cueIdentifiers).map(\.string)
        let cue = try #require(made.cues.firstIndex { $0.uuid.string == rows.last })
        let action = try #require(made.cues[cue].actions.firstIndex { $0.type == .presentationSlide })
        made.cues[cue].actions[action].slide.presentation.clearBaseSlide()
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Left Out.pro"), safeguards: bench.safeguards)
        let original = try Data(contentsOf: song.url)
        #expect(song.textBoxes[11].blank && song.textBoxes[11].element.isEmpty)
        #expect(try !message(at: song.url).cues[cue].actions[action].slide.presentation.hasBaseSlide)

        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["G", "D"])
        #expect(try message(at: song.url).cues[cue].actions[action].slide.presentation.baseSlide.elements.count == 1)
        try song.setChordsAlone(slide: 11, element: song.textBoxes[11].element, line: 0, names: ["D"])
        try song.setChordsAlone(slide: 11, element: song.textBoxes[11].element, line: 0, names: [])
        // Not "an empty slide" where there was none: the bytes it was.
        #expect(try Data(contentsOf: song.url) == original)
        #expect(try !message(at: song.url).cues[cue].actions[action].slide.presentation.hasBaseSlide)
    }

    @Test("A name for the box that no other element of the slide has, and an id written as the file's others are")
    func namesAndIDs() {
        #expect(OpenSong.name("Lyrics", notAmong: []) == "Lyrics")
        #expect(OpenSong.name("Lyrics", notAmong: ["Title", "lyrics"]) == "Lyrics")
        #expect(OpenSong.name("Lyrics", notAmong: ["Lyrics"]) == "Lyrics 2")
        #expect(OpenSong.name("Lyrics", notAmong: ["Lyrics 2", "Lyrics", "Lyrics 3"]) == "Lyrics 4")
        #expect(OpenSong.name("", notAmong: ["", " 2"]) == " 3")
        let capitals = OpenSong.newID(writtenAs: "51C0AF50-4E4D-43EF-B6E0-87B3A2BC1DA6")
        #expect(capitals == capitals.uppercased() && UUID(uuidString: capitals) != nil)
        let small = OpenSong.newID(writtenAs: "51c0af50-4e4d-43ef-b6e0-87b3a2bc1da6")
        #expect(small == small.lowercased() && UUID(uuidString: small) != nil)
        #expect(OpenSong.newID(writtenAs: "") != OpenSong.newID(writtenAs: ""))
    }

    // ---- What is refused

    @Test("A song with no text box that has words: a blank slide cannot be given chords, and the sentence says why. Nothing is changed")
    func noBoxToModelOn() throws {
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file(Self.shaped))
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.shaped)).textBoxes
        // Every box but the first slide's (which is blank) and the last slide's (which
        // has none) goes.
        for box in boxes.dropFirst() where !box.element.isEmpty {
            let here = try #require(place(of: box.element, in: made))
            made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements.remove(at: here.element)
        }
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Bare.pro"), safeguards: bench.safeguards)
        let saved = try Data(contentsOf: song.url)
        #expect(song.textBoxes.allSatisfy(\.blank) && song.textBoxes.count == 12)
        for (slide, element) in [(0, boxes[0].element), (11, ""), (5, "")] {
            let refused = #expect(throws: Refusal.self) { try song.setChordsAlone(slide: slide, element: element, line: 0, names: ["G"]) }
            #expect(refused?.reason == .noBoxToModelOn)
            #expect(refused?.sentence == "This song has no text box with words in it to model one on, so chords cannot be put on a slide that has none. Nothing was changed.")
        }
        #expect(try Data(contentsOf: song.url) == saved && !song.canUndo && bench.copiesKept.isEmpty)
    }

    @Test("A slide that is asked to be given a text box and has one with words by now is refused, and so is a line asked of a slide that is not there")
    func noLongerBlank() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let saved = try Data(contentsOf: song.url)
        for slide in [1, 3, 0] {
            // (The first slide is blank, and has a text box: it is asked by that box.)
            let refused = #expect(throws: Refusal.self) { try song.setChordsAlone(slide: slide, element: "", line: 0, names: ["G"]) }
            #expect(refused?.reason == .noLongerBlank)
            #expect(refused?.sentence == "That slide is no longer blank. Nothing was changed.")
        }
        #expect { try song.setChordsAlone(slide: 12, element: "", line: 0, names: ["G"]) } throws: { ($0 as? Refusal)?.reason == .slideGone }
        #expect { try song.setChordsAlone(slide: 1, element: "no such box", line: 0, names: ["G"]) } throws: { ($0 as? Refusal)?.reason == .textBoxGone }
        // No chords for a slide with no box is nothing to do, whatever the slide.
        try song.setChordsAlone(slide: 1, element: "", line: 0, names: [])
        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["", " "])
        #expect(try Data(contentsOf: song.url) == saved && !song.canUndo && bench.copiesKept.isEmpty)
    }

    @Test("While ProPresenter is running a blank slide is given nothing, and nothing is remembered of it")
    func whileProPresenterIsRunning() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.shaped)
        let original = try Data(contentsOf: song.url)
        bench.proPresenterIsRunning = true
        for (slide, element) in [(0, song.textBoxes[0].element), (11, ""), (3, song.textBoxes[3].element)] {
            #expect { try song.setChordsAlone(slide: slide, element: element, line: 0, names: ["G"]) } throws: { ($0 as? Refusal)?.reason == .proPresenterIsRunning }
        }
        #expect(try Data(contentsOf: song.url) == original && song.textBoxes[11].element.isEmpty && !song.canUndo)
        bench.proPresenterIsRunning = false
        try song.setChordsAlone(slide: 11, element: "", line: 0, names: ["G"])
        try song.setChordsAlone(slide: 11, element: song.textBoxes[11].element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == original)
    }

    // ---- What is remembered, and for how long

    @Test("Another day: chords taken off a slide that was given them in an earlier sitting leave its box with the opening it was given and one stand-in, as any line of chords alone is left, and a box that was added stays. Each takes chords again, the first without a byte of its RTF changing")
    func anotherDay() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.shaped)
        let original = try Data(contentsOf: url)
        var first: OpenSong? = try OpenSong(opening: url, safeguards: bench.safeguards)
        let own = try #require(first?.textBoxes[0].element)
        try first?.setChordsAlone(slide: 0, element: own, line: 0, names: ["E", "A"])
        try first?.setChordsAlone(slide: 11, element: "", line: 0, names: ["B"])
        let added = try #require(first?.textBoxes[11].element)
        first = nil

        // Closed, and opened again.
        let song = try OpenSong(opening: url, safeguards: bench.safeguards)
        let given = try rtf(of: own, at: url)
        try song.setChordsAlone(slide: 0, element: own, line: 0, names: [])
        try song.setChordsAlone(slide: 11, element: added, line: 0, names: [])
        #expect(try Data(contentsOf: url) != original)
        // Neither is a blank slide now: each is a line of chords alone with none on it.
        #expect(song.textBoxes[0] == TextBox(slide: 0, group: "Verse 1", groupColour: song.textBoxes[0].groupColour, startsItsGroup: true, element: own, words: Chords.placeholders(1)))
        #expect(!song.textBoxes[11].blank && song.textBoxes[11].element == added && song.textBoxes[11].words == Chords.placeholders(1) && song.textBoxes[11].chords.isEmpty)
        // The box keeps its opening and its first stand-in: the RTF it was given, less
        // the bytes of the stand-ins after the first. It names a font still, which the
        // empty document it was did not.
        let left = try rtf(of: own, at: url)
        let span = try #require(RTFWords.span(ofLine: 0, of: given))
        #expect(Array(left.prefix(span.lowerBound)) == Array(given.prefix(span.lowerBound)))
        #expect(RTFWords.words(ofRTF: left) == Chords.placeholders(1) && String(decoding: left, as: UTF8.self).contains("CenturyGothic"))
        #expect(try message(at: url).cues.flatMap { $0.actions.flatMap { $0.slide.presentation.baseSlide.elements } }.count == 12)

        // And they are given chords again: one, on the stand-in that was kept, with
        // not a byte of the RTF changed; and two, with a stand-in more.
        try song.setChordsAlone(slide: 0, element: own, line: 0, names: ["G"])
        #expect(try rtf(of: own, at: url) == left)
        try song.setChordsAlone(slide: 11, element: added, line: 0, names: ["D", "G"])
        #expect(song.textBoxes[0].chords == [Chord(at: 0, name: "G")] && song.textBoxes[11].chords.map(\.name) == ["D", "G"])
        #expect(song.textBoxes[11].element == added && song.textBoxes[11].words == Chords.placeholders(2))
    }

    @Test("A copy put back is another file: what was made for a blank slide before it is not remembered after it")
    func afterACopyIsPutBack() throws {
        let bench = try Workbench()
        // The file as it would be on another day, with chords on its first slide.
        let other = try bench.open(Self.shaped)
        let own = other.textBoxes[0].element
        try other.setChordsAlone(slide: 0, element: own, line: 0, names: ["E", "A", "B"])
        let anotherDay = try Data(contentsOf: other.url)

        let url = try bench.put(TestData.file(Self.shaped), named: "Put Back.pro")
        let original = try Data(contentsOf: url)
        let song = try OpenSong(opening: url, safeguards: bench.safeguards)
        try song.setChordsAlone(slide: 0, element: own, line: 0, names: ["G"])
        try song.putBack(anotherDay)
        #expect(song.textBoxes[0].chords.map(\.name) == ["E", "A", "B"])
        // Its chords taken off now are chords taken off another day: the box is not
        // made what it was when this sitting began, which the copy knows nothing of.
        // It keeps one stand-in, as any line of chords alone does.
        try song.setChordsAlone(slide: 0, element: own, line: 0, names: [])
        #expect(!song.textBoxes[0].blank && song.textBoxes[0].words == Chords.placeholders(1) && song.textBoxes[0].chords.isEmpty)
        #expect(try Data(contentsOf: url) != original)
        #expect(String(decoding: try rtf(of: own, at: url), as: UTF8.self).contains("CenturyGothic"))
    }

    @Test("Blank Slides, which Simple Presenter made: each of its three blank slides is given chords and has them taken off again, leaving the file as it was")
    func blankSlidesAsSimplePresenterMadeThem() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Blank Slides.pro")
        let original = try Data(contentsOf: song.url)
        let blanks = song.textBoxes.filter(\.blank)
        #expect(blanks.count == 3 && blanks.filter(\.element.isEmpty).count == 1)
        for blank in blanks {
            try song.setChordsAlone(slide: blank.slide, element: blank.element, line: 0, names: ["G", "D"])
        }
        #expect(song.textBoxes.allSatisfy { !$0.blank })
        #expect(try OpenSong(opening: song.url).textBoxes == song.textBoxes)
        for blank in blanks {
            let now = try #require(song.textBoxes.first { $0.slide == blank.slide })
            #expect(now.words == Chords.placeholders(2) && now.chords.map(\.name) == ["G", "D"])
            try song.setChordsAlone(slide: blank.slide, element: now.element, line: 0, names: [])
        }
        #expect(try Data(contentsOf: song.url) == original)
        #expect(song.textBoxes.filter(\.blank) == blanks)
    }
}
