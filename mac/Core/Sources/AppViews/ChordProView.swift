// The ChordPro editor's text: the system's own text view, in a fixed-width font, that
// never wraps (spec/04-chordpro-editor.md, rules 1, 4 and 6 to 12; spec/10-platforms.md:
// "the ChordPro editor is NSTextView wrapped: SwiftUI's own text editor cannot colour as
// it is typed in or refuse a change").
//
// Nothing here is a rule. The text view asks before it lets any change through
// (`shouldChangeTextIn`), and what it is told comes from Song/ChordProEditor.swift by
// way of the model: the one rule of what may be typed, and the help with brackets. So
// typing in the wrong place, pasting, cutting, dropping and the text view's own undo
// are all held the same way, and a change that is not let through never happens: the
// caret stays where it was, which is where the eye expects it.
//
// What the Mac does its own way (spec/10-platforms.md):
//
//   - Undo is the text view's own (rule 23), kept apart from the window's, and is
//     forgotten whenever the text is set from the song and not typed.
//   - The help with brackets is given to what is TYPED, whatever keys typed it. The
//     page has it withheld while Control, Alt or Command is down, which on the Mac the
//     system sees to already: such a key press never arrives as typed text, and Option
//     with Delete is another command than Delete. And several keyboards type "[" and
//     "]" with Option held, which must be helped like any other.
//   - Everything the system would do to text as it is typed is switched off: quotes
//     and dashes made curly, spelling put right, spaces added round a paste. Each would
//     be a change to the words, and so refused, one keystroke after another.

import AppKit
import Song
import SwiftUI

/// The text view, and what stands between it and the model. Made apart from SwiftUI so
/// that a test can type into it.
@MainActor
final class ChordProText: NSObject, NSTextViewDelegate {
    /// What the text view asks of the model.
    struct Asks {
        /// May the text become this? If it may, it has been taken.
        var typed: (String) -> Bool
        /// The text as it last was when it might: what to go back to.
        var good: () -> String?
        /// The text a step larger (1) or smaller (-1), or its usual size (0): answers
        /// with how many times its usual size it then is.
        var zoom: (Int) -> Double? = { _ in nil }
    }

    final class View: NSTextView {
        /// Whether what is arriving was typed. What is pasted does not come by this
        /// door (seen: read from a pasteboard, it is asked about and put in without
        /// `insertText` being called), so a pasted "[" is not given its other.
        private(set) var isTyping = false

        override func insertText(_ string: Any, replacementRange: NSRange) {
            isTyping = true
            defer { isTyping = false }
            super.insertText(string, replacementRange: replacementRange)
        }

        // (What is pasted or dropped is always plain text: the song's words have their
        // format in the song, not here.)
        override func pasteAsRichText(_ sender: Any?) {
            pasteAsPlainText(sender)
        }

        /// Told when a key that makes the text larger or smaller is pressed.
        var zoomed: (Int) -> Void = { _ in }

        // Command with plus (or equals, which is plus without Shift), minus and 0: the
        // text larger, smaller, and its usual size. A window's views are asked before
        // its menus are, and this one answers while it has the keyboard.
        override func performKeyEquivalent(with event: NSEvent) -> Bool {
            let flags = event.modifierFlags.intersection([.command, .control, .option, .shift])
            if window?.firstResponder === self, event.type == .keyDown, flags.contains(.command), flags.isSubset(of: [.command, .shift]) {
                switch event.charactersIgnoringModifiers ?? "" {
                case "=", "+":
                    zoomed(1)
                    return true
                case "-":
                    zoomed(-1)
                    return true
                case "0" where !flags.contains(.shift):
                    zoomed(0)
                    return true
                default:
                    break
                }
            }
            return super.performKeyEquivalent(with: event)
        }

        // Undo and Redo in the Edit menu, and their keys, arrive as these two messages
        // at whatever has the keyboard. A text view does not answer them itself: it
        // leaves them to its window, and this one's undo is its own, kept apart from the
        // window's (rule 23). So it answers them here, with that.
        @objc func undo(_ sender: Any?) {
            if undoManager?.canUndo == true { undoManager?.undo() }
        }

        @objc func redo(_ sender: Any?) {
            if undoManager?.canRedo == true { undoManager?.redo() }
        }

        override func validateUserInterfaceItem(_ item: NSValidatedUserInterfaceItem) -> Bool {
            if item.action == #selector(undo(_:)) { return isEditable && undoManager?.canUndo == true }
            if item.action == #selector(redo(_:)) { return isEditable && undoManager?.canRedo == true }
            return super.validateUserInterfaceItem(item)
        }
    }

    let scroll = NSScrollView()
    let text: View
    var asks: Asks
    /// What is done when a key is not taken: the system's sound for "no".
    var saysNo: () -> Void = { NSSound.beep() }
    private let ownUndo = UndoManager()
    /// The change in hand is one made from here, already asked about.
    private var applying = false
    private var shownVersion: Int?
    private var shownSitting: Int?

    /// The colour of a chord in the text: the system's orange, or the one chosen in
    /// the settings.
    private(set) var chordColour = NSColor.systemOrange

    /// Colours the text's chords another colour.
    func setChordColour(_ wanted: NSColor) {
        guard wanted != chordColour else { return }
        chordColour = wanted
        colour()
    }

    /// How many times its usual size the text is, and the size it usually is.
    private(set) var scale = 1.0
    static let usualSize = 14.0
    var font: NSFont { NSFont.monospacedSystemFont(ofSize: Self.usualSize * scale, weight: .regular) }
    var chordFont: NSFont { NSFont.monospacedSystemFont(ofSize: Self.usualSize * scale, weight: .bold) }

    init(asks: Asks) {
        self.asks = asks
        // Laid out the older way (a layout manager), which is the one that lets a line
        // be as long as it likes without wrapping, in a view that grows to hold it.
        let storage = NSTextStorage()
        let layout = NSLayoutManager()
        storage.addLayoutManager(layout)
        let container = NSTextContainer(size: NSSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude))
        container.widthTracksTextView = false
        layout.addTextContainer(container)
        text = View(frame: NSRect(x: 0, y: 0, width: 400, height: 300), textContainer: container)
        super.init()

        text.minSize = NSSize(width: 0, height: 0)
        text.maxSize = NSSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude)
        text.isVerticallyResizable = true
        text.isHorizontallyResizable = true
        text.autoresizingMask = []
        text.textContainerInset = NSSize(width: 12, height: 12)
        text.isRichText = false
        text.importsGraphics = false
        text.allowsUndo = true
        text.usesFindBar = true
        text.isIncrementalSearchingEnabled = true
        text.font = font
        text.textColor = .textColor
        text.backgroundColor = .textBackgroundColor
        text.insertionPointColor = .textColor
        // Nothing is to change what is typed but the person typing it.
        text.isAutomaticQuoteSubstitutionEnabled = false
        text.isAutomaticDashSubstitutionEnabled = false
        text.isAutomaticTextReplacementEnabled = false
        text.isAutomaticSpellingCorrectionEnabled = false
        text.isAutomaticTextCompletionEnabled = false
        text.isAutomaticDataDetectionEnabled = false
        text.isAutomaticLinkDetectionEnabled = false
        text.isContinuousSpellCheckingEnabled = false
        text.isGrammarCheckingEnabled = false
        text.smartInsertDeleteEnabled = false
        text.delegate = self
        text.setAccessibilityLabel("The song as ChordPro text")
        text.zoomed = { [weak self] direction in
            guard let self, let scale = self.asks.zoom(direction) else { return }
            self.setScale(scale)
        }

        scroll.documentView = text
        scroll.hasVerticalScroller = true
        scroll.hasHorizontalScroller = true
        scroll.autohidesScrollers = true
        scroll.borderType = .noBorder
        scroll.drawsBackground = true
        scroll.backgroundColor = .textBackgroundColor
    }

    /// Shows what the model has. The text is put in place of what is there only when it
    /// was set from the song (a new `version`), never because of what was typed here.
    func show(_ shown: LibraryModel.ChordProShown, canType: Bool) {
        if shown.version != shownVersion {
            shownVersion = shown.version
            // (A song just opened starts at its top; the same song's text made again
            // keeps the caret where it was.)
            let caret = shown.sitting == shownSitting ? text.selectedRange().location : 0
            shownSitting = shown.sitting
            text.string = shown.text
            ownUndo.removeAllActions()
            colour()
            text.setSelectedRange(NSRange(location: min(caret, (shown.text as NSString).length), length: 0))
            // The caret is kept in view; at the start of the text, the text's own top
            // left corner is, margin and all. (Asked of a view that has no size yet,
            // "in view" would scroll the margin away.)
            if text.selectedRange().location == 0 || scroll.contentSize.width < 1 {
                scroll.contentView.scroll(to: .zero)
                scroll.reflectScrolledClipView(scroll.contentView)
            } else {
                text.scrollRangeToVisible(text.selectedRange())
            }
        }
        if text.isEditable != canType {
            text.isEditable = canType
        }
    }

    /// Shows the text so many times its usual size. Only its size changes: not a
    /// character of it, nor where the caret is, nor what can be undone.
    func setScale(_ wanted: Double) {
        guard wanted != scale else { return }
        scale = wanted
        text.font = font
        colour()
    }

    /// Chords are coloured, brackets and all; the lines in curly brackets are greyed.
    /// Only how the text looks is touched: no character of it.
    private func colour() {
        guard let storage = text.textStorage else { return }
        let colouring = ChordProEditor.colouring(of: text.string)
        storage.beginEditing()
        storage.setAttributes([.font: font, .foregroundColor: NSColor.textColor], range: NSRange(location: 0, length: storage.length))
        for range in colouring.greyed {
            storage.addAttribute(.foregroundColor, value: NSColor.secondaryLabelColor, range: NSRange(location: range.lowerBound, length: range.count))
        }
        for range in colouring.chords {
            storage.addAttributes([.foregroundColor: chordColour, .font: chordFont], range: NSRange(location: range.lowerBound, length: range.count))
        }
        storage.endEditing()
        // (What is typed next starts plain, whatever it is typed beside.)
        text.typingAttributes = [.font: font, .foregroundColor: NSColor.textColor]
    }

    /// Makes a change from here, as if it had been typed: asked about, and one step of
    /// the text view's undo.
    private func make(_ edit: ChordProEditor.Edit) {
        let range = NSRange(location: edit.range.lowerBound, length: edit.range.count)
        if !(range.length == 0 && edit.with.isEmpty) {
            let proposed = (text.string as NSString).replacingCharacters(in: range, with: edit.with)
            guard asks.typed(proposed) else {
                saysNo()
                return
            }
            applying = true
            defer { applying = false }
            guard text.shouldChangeText(in: range, replacementString: edit.with) else { return }
            text.textStorage?.replaceCharacters(in: range, with: edit.with)
            text.didChangeText()
        }
        text.setSelectedRange(NSRange(location: edit.caret, length: 0))
    }

    // ---- What the text view asks

    func undoManager(for view: NSTextView) -> UndoManager? {
        ownUndo
    }

    func textView(_ textView: NSTextView, shouldChangeTextIn affectedCharRange: NSRange, replacementString: String?) -> Bool {
        // (No string: only how the text looks is to change, which is ours to do.)
        guard !applying, let replacement = replacementString else { return true }
        if text.isTyping, let edit = ChordProEditor.helpWithTyping(replacement, in: textView.string,
                                                                   selection: affectedCharRange.location..<(affectedCharRange.location + affectedCharRange.length)) {
            make(edit)
            return false
        }
        let proposed = (textView.string as NSString).replacingCharacters(in: affectedCharRange, with: replacement)
        if asks.typed(proposed) { return true }
        saysNo()
        return false
    }

    func textDidChange(_ notification: Notification) {
        // A change that got here without being asked about, by some road of the text
        // system's that does not ask first, is asked about now; and if it is not the
        // song's words, the text is put back as it was, as the reference does for every
        // change. (None is known. Every change should have been asked about above.)
        if !asks.typed(text.string), let good = asks.good(), good != text.string {
            text.string = good
            ownUndo.removeAllActions()
            saysNo()
        }
        colour()
    }

    func textView(_ textView: NSTextView, doCommandBy commandSelector: Selector) -> Bool {
        let selected = textView.selectedRange()
        let selection = selected.location..<(selected.location + selected.length)
        var edit: ChordProEditor.Edit?
        if commandSelector == #selector(NSResponder.deleteBackward(_:)) {
            edit = ChordProEditor.helpWithBackspace(in: textView.string, selection: selection)
        } else if commandSelector == #selector(NSResponder.deleteForward(_:)) {
            edit = ChordProEditor.helpWithDelete(in: textView.string, selection: selection)
        }
        guard let edit, textView.isEditable else { return false }
        make(edit)
        return true
    }
}

/// The text view in the window.
struct ChordProView: NSViewRepresentable {
    var model: LibraryModel
    var shown: LibraryModel.ChordProShown
    var canType: Bool
    /// How many times its usual size the text is.
    var scale: Double
    /// The colour of a chord, if another than the usual was chosen: "#rrggbb".
    var chordColour: String?

    private var asks: ChordProText.Asks {
        .init(typed: { [model] in model.textMayBecome($0) }, good: { [model] in model.chordPro?.text }, zoom: { [model] in model.zoomText($0) })
    }

    func makeCoordinator() -> ChordProText {
        ChordProText(asks: asks)
    }

    func makeNSView(context: Context) -> NSScrollView {
        context.coordinator.setChordColour(ChordColour.colour(chordColour) ?? .systemOrange)
        context.coordinator.setScale(scale)
        context.coordinator.show(shown, canType: canType)
        return context.coordinator.scroll
    }

    func updateNSView(_ view: NSScrollView, context: Context) {
        context.coordinator.asks = asks
        context.coordinator.setChordColour(ChordColour.colour(chordColour) ?? .systemOrange)
        context.coordinator.setScale(scale)
        context.coordinator.show(shown, canType: canType)
    }
}
