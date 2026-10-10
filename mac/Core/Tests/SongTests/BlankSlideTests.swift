// Which boxes are handed over that Simple Presenter does not hand over, or that take
// some telling: a blank slide's box, and boxes with links. spec/01-file-format.md, "A
// blank slide", and "From the presentation down to a text box".
//
// No test file has a blank slide or a link in it, so they are made here, from Plain
// Song, in a temporary folder. NEW, and so held by these cases and by no vector.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("Blank slides, and boxes with links")
struct BlankSlideTests {
    /// The RTF of a text box with no text, as Apple's text system writes it: an empty
    /// document, whose font table has nothing in it. (As the blank boxes of real songs
    /// have it. There are no words in it to be anyone's.)
    static let emptyRTF = Data([
        #"{\rtf1\ansi\ansicpg1252\cocoartf2513"#,
        #"\cocoatextscaling0\cocoaplatform0{\fonttbl}"#,
        #"{\colortbl;\red255\green255\blue255;}"#,
        #"{\*\expandedcolortbl;;}"#,
        "}",
    ].joined(separator: "\n").utf8)

    /// Plain Song, its message, and what it reads as untouched: four slides, a box on each.
    private func plainSong() throws -> (RVData_Presentation, [TextBox]) {
        let bytes = try TestData.file("made-by-simple-presenter/Plain Song.pro")
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files/made-by-simple-presenter/Plain Song.pro")).textBoxes
        return (try RVData_Presentation(serializedBytes: bytes), boxes)
    }

    private func blanked(_ element: inout RVData_Slide.Element) {
        element.element.text.rtfData = Self.emptyRTF
        element.element.text.attributes.customAttributes.removeAll(where: isChord)
    }

    @Test("A slide whose only text box is blank is handed over, as blank")
    func aBlankSlide() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        #expect(boxes.count == 4)
        let second = try #require(place(of: boxes[1].element, in: made))
        blanked(&made.cues[second.cue].actions[second.action].slide.presentation.baseSlide.elements[second.element])
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Blank.pro"), safeguards: bench.safeguards)

        let now = song.textBoxes
        #expect(now.count == 4)
        #expect(now.map(\.blank) == [false, true, false, false])
        #expect(now[1].element == boxes[1].element && now[1].slide == boxes[1].slide && now[1].group == boxes[1].group)
        #expect(now[1].words.isEmpty && now[1].chords.isEmpty)
        // The others are what they were.
        #expect([now[0], now[2], now[3]] == [boxes[0], boxes[2], boxes[3]])
        #expect(song.usedChords == (boxes[0].chords + boxes[2].chords + boxes[3].chords).map(\.name))

        // It cannot be given chords yet: that writes the box's RTF, which is M7.
        let saved = try Data(contentsOf: song.url)
        #expect { try song.setChordsAlone(slide: now[1].slide, element: now[1].element, line: 0, names: ["G"]) } throws: { ($0 as? Refusal)?.reason == .notBuiltYet }
        // (No chords on it is what it has already.)
        try song.setChordsAlone(slide: now[1].slide, element: now[1].element, line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == saved)
        #expect(!song.canUndo)
    }

    @Test("A box with only spaces in it is blank too; one with a stand-in is a line of chords alone")
    func spacesAndStandIns() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        // Plain Song's last slide is a line of three stand-ins with a chord on each.
        // Without its chords it is still shown, as it always was: it has stand-ins.
        let last = try #require(place(of: boxes[3].element, in: made))
        made.cues[last.cue].actions[last.action].slide.presentation.baseSlide.elements[last.element].element.text.attributes.customAttributes.removeAll(where: isChord)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Stand-ins.pro"))
        #expect(song.textBoxes.count == 4)
        #expect(song.textBoxes[3].blank == false && song.textBoxes[3].chords.isEmpty)
        #expect(Chords.isPlaceholders(song.textBoxes[3].words))
    }

    @Test("A box with nothing in it but the wide space that parts stand-ins is not blank either")
    func onlyAnEmQuad() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        let second = try #require(place(of: boxes[1].element, in: made))
        // (The em quad is written straight into the RTF, as UTF-8, which the reader takes.)
        var rtf = Self.emptyRTF
        rtf.removeLast()
        rtf.append(contentsOf: Array("\u{2001}}".utf8))
        made.cues[second.cue].actions[second.action].slide.presentation.baseSlide.elements[second.element].element.text.rtfData = rtf
        made.cues[second.cue].actions[second.action].slide.presentation.baseSlide.elements[second.element].element.text.attributes.customAttributes.removeAll(where: isChord)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Quad.pro"))
        #expect(song.textBoxes.count == 4)
        #expect(song.textBoxes[1].words == Words(units: [0x2001]) && !song.textBoxes[1].blank)
    }

    @Test("Chords are not put on a thing that is no text box")
    func notATextBox() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        // The first slide's element is left with no text at all: a shape.
        let first = try #require(place(of: boxes[0].element, in: made))
        made.cues[first.cue].actions[first.action].slide.presentation.baseSlide.elements[first.element].element.clearText()
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Shape.pro"), safeguards: bench.safeguards)
        let saved = try Data(contentsOf: song.url)
        // The slide is a blank one now, with no text box to its name.
        #expect(song.textBoxes.map(\.element) == ["", boxes[1].element, boxes[2].element, boxes[3].element])
        #expect(song.textBoxes.map(\.blank) == [true, false, false, false])
        #expect { try song.setChords(slide: 0, element: boxes[0].element, [Chord(at: 0, name: "G")]) } throws: { ($0 as? Refusal)?.reason == .textBoxGone }
        #expect { try song.setChords(slide: 0, element: "", [Chord(at: 0, name: "G")]) } throws: { ($0 as? Refusal)?.reason == .textBoxGone }
        #expect(try Data(contentsOf: song.url) == saved)
    }

    @Test("A slide with no text box at all is a blank slide too, handed over with no id")
    func noTextBoxAtAll() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        // The third slide is left with nothing on it at all.
        let third = try #require(place(of: boxes[2].element, in: made))
        made.cues[third.cue].actions[third.action].slide.presentation.baseSlide.elements.removeAll()
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Bare.pro"), safeguards: bench.safeguards)
        let saved = try Data(contentsOf: song.url)

        let now = song.textBoxes
        #expect(now.map(\.element) == [boxes[0].element, boxes[1].element, "", boxes[3].element])
        #expect(now[2].blank && now[2].words.isEmpty && now[2].chords.isEmpty)
        #expect(now[2].slide == 2 && now[2].group == boxes[2].group && now[2].startsItsGroup == boxes[2].startsItsGroup)

        // It cannot be given chords yet: that gives the slide a text box, which is M7.
        #expect { try song.setChordsAlone(slide: 2, element: "", line: 0, names: ["G"]) } throws: { ($0 as? Refusal)?.reason == .notBuiltYet }
        // (No chords on it is what it has already: nothing is made for it.)
        try song.setChordsAlone(slide: 2, element: "", line: 0, names: [])
        #expect(try Data(contentsOf: song.url) == saved)
        #expect(!song.canUndo && bench.copiesKept.isEmpty)
    }

    @Test("A blank box beside a box with words is not handed over")
    func notEveryBlankBox() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        // The first slide is given a second box, blank, before its own.
        let first = try #require(place(of: boxes[0].element, in: made))
        var extra = made.cues[first.cue].actions[first.action].slide.presentation.baseSlide.elements[first.element]
        extra.element.uuid.string = "11111111-2222-3333-4444-555555555555"
        blanked(&extra)
        made.cues[first.cue].actions[first.action].slide.presentation.baseSlide.elements.insert(extra, at: 0)

        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Beside.pro"))
        let now = song.textBoxes
        #expect(now.map(\.element) == boxes.map(\.element))
        #expect(now.allSatisfy { !$0.blank })
        #expect(now.map(\.slide) == [0, 1, 2, 3])
    }

    @Test("A blank box that holds a stray space is handed over with no words")
    func aStraySpace() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        let second = try #require(place(of: boxes[1].element, in: made))
        // (An RTF whose only text is one space.)
        var rtf = String(decoding: Self.emptyRTF, as: UTF8.self)
        let end = try #require(rtf.lastIndex(of: "}"))
        rtf.insert(contentsOf: " ", at: end)
        made.cues[second.cue].actions[second.action].slide.presentation.baseSlide.elements[second.element].element.text.rtfData = Data(rtf.utf8)
        made.cues[second.cue].actions[second.action].slide.presentation.baseSlide.elements[second.element].element.text.attributes.customAttributes.removeAll(where: isChord)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Space.pro"))
        #expect(song.textBoxes[1].blank && song.textBoxes[1].words.isEmpty && song.textBoxes[1].element == boxes[1].element)
    }

    @Test("A box that shows a timer or another box's words is not handed over; one that is only sometimes seen is")
    func boxesWithLinks() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        func element(_ n: Int, _ change: (inout RVData_Slide.Element) -> Void) throws {
            let here = try #require(place(of: boxes[n].element, in: made))
            change(&made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element])
        }
        // The first box shows a timer; the second another element's words; the third is
        // its own words, seen only on some condition.
        try element(0) {
            var link = RVData_Slide.Element.DataLink()
            link.timerText = RVData_Slide.Element.DataLink.TimerText()
            $0.dataLinks = [link]
        }
        try element(1) {
            var link = RVData_Slide.Element.DataLink()
            link.alternateText = RVData_Slide.Element.DataLink.AlternateElementText()
            $0.dataLinks = [link]
        }
        try element(2) {
            var link = RVData_Slide.Element.DataLink()
            link.visibilityLink = RVData_Slide.Element.DataLink.VisibilityLink()
            $0.dataLinks = [link]
        }
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Links.pro"))
        // The first two slides have no box of their own words at all. A box with a link
        // is not offered for chords, so each is a blank slide with no text box to its
        // name: a chord put there would be given a box of its own.
        #expect(song.textBoxes.map(\.element) == ["", "", boxes[2].element, boxes[3].element])
        #expect(song.textBoxes.map(\.blank) == [true, true, false, false])
    }

    @Test("A slide that is switched off is not a slide of the song, and the rest are numbered without it")
    func switchedOff() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        let second = try #require(place(of: boxes[1].element, in: made))
        made.cues[second.cue].isEnabled = false
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Off.pro"))
        #expect(song.slideCount == 3)
        #expect(song.textBoxes.map(\.element) == [boxes[0].element, boxes[2].element, boxes[3].element])
        #expect(song.textBoxes.map(\.slide) == [0, 1, 2])
    }
}
