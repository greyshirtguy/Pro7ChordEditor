// What lies over each of the strip's seven buttons and takes the mouse there: a press
// let go is the button's click, as it always was, and a press pulled 6 away picks the
// chord up, to be carried to the sheet and let go on a word (spec/03-chord-sheet.md, "A
// chord dragged out of the strip": the maintainer's, 2026-10-12, whose hand had tried
// it unasked).
//
// The carrying is the Mac's own dragging (spec/10-platforms.md). A button of SwiftUI
// cannot begin that and a view of AppKit can: hence this view, laid over the button
// and seen through. The system then draws the button's picture under the pointer,
// puts its green plus beside the pointer wherever the sheet says the chord can be put,
// slides the picture back when it is let go anywhere else, and ends it all at Esc.
// None of that is written here.
//
// The sheet's side of it is in ChordSheetView.swift (`carriedOfKey`, `droppedOfKey`,
// and what the system calls on a view that a thing is dragged over). What is done to
// the song is Song/ChordSheet.swift's, with no window in it.
//
// What is carried is the chord's name, under a type of this app's own. It is offered
// to this app only, and the sheet takes nothing else: so a chord cannot be let go into
// another app, and nothing dragged from another app lands on a song.

import AppKit
import SwiftUI

extension NSPasteboard.PasteboardType {
    /// One of the key's chords, picked up from the strip: its name.
    static let chordOfTheKey = NSPasteboard.PasteboardType("com.greyshirtguy.Pro7ChordEditor.chord-of-the-key")
}

@MainActor
final class KeyChordGrip: NSView, NSDraggingSource {
    /// How far the pointer goes with the button down before the chord is picked up:
    /// what a chord of the sheet's own is dragged at (spec/03, rule 41).
    static let pull = 6.0

    /// The chord's name.
    var name = ""
    /// The press was let go where it was made: the button's click.
    var clicked: () -> Void = {}
    /// The chord is picked up.
    var lifted: () -> Void = {}
    /// The carrying is over, wherever it ended.
    var ended: () -> Void = {}
    /// The button's picture, to go with the pointer.
    var picture: () -> NSImage? = { nil }
    /// Begins the system's dragging of what was picked up. (The tests put another
    /// here: they have no pointer for the system to follow.)
    var begin: (KeyChordGrip, NSDraggingItem, NSEvent) -> Void = { grip, item, event in
        let session = grip.beginDraggingSession(with: [item], event: event, source: grip)
        session.animatesToStartingPositionsOnCancelOrFail = true
        session.draggingFormation = .none
    }

    /// Where the button went down, while it is down; and whether the chord has been
    /// picked up since.
    private(set) var pressedAt: NSPoint?
    private(set) var carrying = false

    override init(frame: NSRect) {
        super.init(frame: frame)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("not made from a file")
    }

    // (A view that is seen through would otherwise be one the window can be moved by,
    // where a window is moved by its background.)
    override var mouseDownCanMoveWindow: Bool { false }

    // ---- The mouse

    func pressed(at point: NSPoint) {
        pressedAt = point
        carrying = false
        needsDisplay = true
    }

    /// The pointer has moved with the button down. Answers true the once: when it has
    /// gone far enough from where the button went down, and the chord is picked up.
    func pulled(to point: NSPoint) -> Bool {
        guard let from = pressedAt, !carrying, abs(point.x - from.x) + abs(point.y - from.y) >= Self.pull else { return false }
        carrying = true
        lifted()
        needsDisplay = true
        return true
    }

    /// The button is let go with nothing picked up: a click, if it is let go on the
    /// button. (Let go of a chord that is being carried, it is the system that hears
    /// of it, and `carryingEnded` that is told.)
    func released(at point: NSPoint) {
        guard pressedAt != nil, !carrying else { return }
        pressedAt = nil
        needsDisplay = true
        if bounds.contains(point) {
            clicked()
        }
    }

    /// What the system carries: the chord's name under this app's own type, and the
    /// button's picture where the button is.
    func carried() -> NSDraggingItem {
        let written = NSPasteboardItem()
        written.setString(name, forType: .chordOfTheKey)
        let item = NSDraggingItem(pasteboardWriter: written)
        item.setDraggingFrame(bounds, contents: picture())
        return item
    }

    /// The carrying is over: the chord was put on, or let go somewhere else, or Esc
    /// was pressed.
    func carryingEnded() {
        pressedAt = nil
        carrying = false
        needsDisplay = true
        ended()
    }

    override func mouseDown(with event: NSEvent) {
        pressed(at: convert(event.locationInWindow, from: nil))
    }

    override func mouseDragged(with event: NSEvent) {
        if pulled(to: convert(event.locationInWindow, from: nil)) {
            begin(self, carried(), event)
        }
    }

    override func mouseUp(with event: NSEvent) {
        released(at: convert(event.locationInWindow, from: nil))
    }

    // ---- What the system asks of where a thing is dragged from

    /// Within this app the chord may be put somewhere (the system's word for that is
    /// "copy", and its sign a green plus). Outside it, nothing may be done with it.
    func draggingSession(_ session: NSDraggingSession, sourceOperationMaskFor context: NSDraggingContext) -> NSDragOperation {
        Self.mayBeDone(context)
    }

    static func mayBeDone(_ context: NSDraggingContext) -> NSDragOperation {
        context == .withinApplication ? .copy : []
    }

    /// Option, Command and Control held as it is carried change nothing: there is one
    /// thing to be done with it (spec/03, rule 46g).
    func ignoreModifierKeys(for session: NSDraggingSession) -> Bool {
        true
    }

    func draggingSession(_ session: NSDraggingSession, endedAt screenPoint: NSPoint, operation: NSDragOperation) {
        carryingEnded()
    }

    // ---- Drawing

    override func draw(_ dirtyRect: NSRect) {
        // Seen through; but for a shade on the button while it is held down, and the
        // button made faint while its chord is out being carried, as the bubble that a
        // chord of the sheet's own leaves behind is.
        guard pressedAt != nil || carrying else { return }
        (carrying ? SheetLook.strip.withAlphaComponent(0.6) : NSColor.black.withAlphaComponent(0.22)).setFill()
        NSBezierPath(roundedRect: bounds, xRadius: 6, yRadius: 6).fill()
    }
}

/// The grip, over one of the strip's buttons.
struct KeyChordHandle: NSViewRepresentable {
    var name: String
    /// What the button says of itself when it is pointed at. (Said here as well as by
    /// the button under this: this is what the pointer is over.)
    var says: String
    var picture: () -> NSImage?
    var clicked: () -> Void
    var lifted: () -> Void
    var ended: () -> Void

    func makeNSView(context: Context) -> KeyChordGrip {
        let grip = KeyChordGrip(frame: .zero)
        bring(grip)
        return grip
    }

    func updateNSView(_ grip: KeyChordGrip, context: Context) {
        bring(grip)
    }

    private func bring(_ grip: KeyChordGrip) {
        grip.name = name
        grip.toolTip = says
        grip.picture = picture
        grip.clicked = clicked
        grip.lifted = lifted
        grip.ended = ended
    }
}
