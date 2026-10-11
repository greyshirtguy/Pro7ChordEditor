// The two lists at the left of the window, as far as they can be told without a hand:
// how the bar between them shares out their height, and the small mark that is drawn
// before each name (spec/06-library.md, rules 7 and 8). What they look like is in the
// pictures (PictureTests); that the bar drags is the maintainer's to try.

@testable import AppViews
import CoreGraphics
import SwiftUI
import Testing

@MainActor
@Suite("The lists at the left of the window")
struct WindowListsTests {
    @Test("How tall the libraries' list is: its share of the height, but never so much or so little that either list is left with less than the least")
    func howTallTheLibrariesAre() {
        #expect(ListsLook.top(share: 0.3, of: 1000) == 300)
        #expect(ListsLook.top(share: 0.5, of: 600) == 300)
        // Too little, and too much.
        #expect(ListsLook.top(share: 0.01, of: 600) == ListsLook.leastLibraries)
        #expect(ListsLook.top(share: 0.99, of: 600) == 600 - ListsLook.barHeight - ListsLook.leastSongs)
        // In a window too short for both to have their least, the libraries have theirs.
        #expect(ListsLook.top(share: 0.5, of: 250) == ListsLook.leastLibraries)
    }

    @Test("The bar says how many songs the library has, and how many of them are listed where the list is narrowed")
    func whatTheBarSays() {
        #expect(ListsLook.count(shown: 8, of: 8) == "8 SONGS")
        #expect(ListsLook.count(shown: 1, of: 1) == "1 SONG")
        #expect(ListsLook.count(shown: 0, of: 0) == "0 SONGS")
        #expect(ListsLook.count(shown: 3, of: 8) == "3 OF 8 SONGS")
        #expect(ListsLook.count(shown: 1, of: 8) == "1 OF 8 SONGS")
    }

    @Test("The mark of a presentation is drawn from its outline: a bar and a screen, inside the square it is given, at any size")
    func theMarkOfAPresentation() {
        let whole = PresentationGlyph.outline.boundingRect
        // The outline fills its square of 200 from side to side, and nearly from top
        // to bottom.
        #expect(abs(whole.minX) < 0.01 && abs(whole.maxX - 200) < 0.01)
        #expect(whole.minY > 4 && whole.minY < 8 && whole.maxY > 192 && whole.maxY < 196)
        // Its two parts do not touch: the bar ends before the screen begins.
        #expect(!PresentationGlyph.outline.contains(CGPoint(x: 66, y: 100)))
        #expect(PresentationGlyph.outline.contains(CGPoint(x: 30, y: 100)))
        // The screen is an outline: its middle is clear, and its edge is not.
        #expect(!PresentationGlyph.outline.contains(CGPoint(x: 110, y: 100)))
        #expect(PresentationGlyph.outline.contains(CGPoint(x: 170, y: 100)))
        // Given a rectangle, it is drawn in the middle of it, as big as fits, square.
        let small = PresentationGlyph().path(in: CGRect(x: 10, y: 20, width: 40, height: 16)).boundingRect
        #expect(abs(small.width - 16) < 0.01 && abs(small.midX - 30) < 0.01)
        #expect(small.minY >= 20 && small.maxY <= 36)
        // (A path of the four things a picture file writes one with, and nothing else.)
        let made = PresentationGlyph.path(of: "M0,0 L10,0 10,10 C10,20 0,20 0,10 Z M-5,-5 L-1,-1")
        #expect(made.boundingRect.minX == -5 && made.boundingRect.maxX == 10)
        #expect(made.contains(CGPoint(x: 5, y: 5)) && made.contains(CGPoint(x: 5, y: 14)))
    }
}
