// The line of hints under the chord sheet: what the keys and the mouse do just now,
// where the spot is. spec/03-chord-sheet.md, "The line of hints".
//
// It was one sentence that never changed, and said everything at once: ten things, of
// which the one wanted was seldom the one read. (The maintainer, 2026-10-13, after
// using the sheet for two days: "I did not realise I could hover and press delete".)
// So it says less, and says what goes with what is under the pointer:
//
//   on a chord            Delete takes it off; 1 to 7 change it; it can be dragged; a
//                         click opens it to be typed over;
//   on a place with none  1 to 7 put one on; a letter or a click types one;
//   while one is typed    what Return, Tab and Esc do with it. With the box empty on
//                         a chord, that Return takes the chord off;
//   while one is carried  that letting go moves it, and Option leaves a copy;
//   with Option held,     the copying of a line's chords onto another: Command+C and
//   or Command            Command+V, which no picture on the sheet shows;
//   the pointer elsewhere where to point to be told.
//
// It says them when they are of use, and at no other time. (For a day it also went
// through ten tips, one every nine seconds, while the pointer was elsewhere. The
// maintainer, having tried it: "love how they appear at the right time - lets drop the
// randomly timed tips".)
//
// THE HIGHLIGHTED LINE. The copying is of a line and onto a line, and the hint for it
// has to say which. So the line the person is at is drawn in a colour of its own on
// the sheet (`line`, below, says which line that is; the sheet's view draws it), and
// the hint calls it "the highlighted line".
//
// The keys are drawn as keys, by the signs the Mac's own keyboard and menus have for
// them (the Windows app says them in words: spec/10-platforms.md).
//
// No window in this file, and no rule of the song: what a key DOES is ChordSheet's.
// This only says it, and is held to what ChordSheet does by reading the same state.

import Song

public enum SheetHints {
    /// What is going on in the sheet, as far as what is worth saying goes.
    public enum Situation: Equatable, Sendable {
        /// Nothing in particular: the pointer is not over the sheet, and nothing is
        /// being typed or carried.
        case nothing
        /// A chord is being typed: whether there is a chord at that place already,
        /// and whether the box has nothing in it.
        case typing(onChord: Bool, empty: Bool)
        /// A chord is being carried, from the sheet or out of the strip.
        case carrying
        /// The spot is on a chord.
        case onChord
        /// The spot is on a place that has none.
        case onPlace
    }

    /// One hint: the keys it is about, drawn as keys, and what they do.
    public struct Hint: Equatable, Sendable {
        /// Words that stand before the keys, where the hint is a sentence: "Press".
        public var lead: String
        public var keys: [String]
        public var says: String
        /// Whether it is to be said whatever the room: where the line has not room
        /// for all its hints the last are left off, but not one of these, for which
        /// the line is drawn smaller first.
        public var needed: Bool

        public init(lead: String = "", _ keys: [String], _ says: String, needed: Bool = false) {
            self.lead = lead
            self.keys = keys
            self.says = says
            self.needed = needed
        }

        /// A hint that is words alone.
        public init(_ says: String) {
            self.init([], says)
        }

        /// The hint as it would be read out.
        public var read: String {
            ([lead] + keys + [says]).filter { !$0.isEmpty }.joined(separator: " ")
        }
    }

    /// What a paste would do just now, which the hint for it says before the key is
    /// pressed.
    public struct Paste: Equatable, Sendable {
        /// How many lines have been copied, if any have.
        public var lines: Int?
        /// Whether it would take chords off to put the copied ones on: whether the
        /// lines it would go onto have chords of their own. (With nothing copied yet,
        /// whether the spot's line has: that is what a paste there will do.)
        public var replaces: Bool

        public init(lines: Int? = nil, replaces: Bool = false) {
            self.lines = lines
            self.replaces = replaces
        }
    }

    // The signs of the Mac's keys, as its menus and its keyboard have them.
    public static let delete = "\u{232B}"
    public static let enter = "\u{21A9}"
    public static let tab = "\u{21E5}"
    public static let escape = "esc"
    public static let option = "\u{2325}"
    public static let command = "\u{2318}"
    public static let shift = "\u{21E7}"
    static let numbers = "1 \u{2013} 7"
    static let letters = "A \u{2013} G"

    /// What is going on in a sheet. `attending`: the pointer is over the sheet, or
    /// the keys have just moved the spot; without that the spot is only where it was
    /// left, and says nothing of what the person is about.
    public static func situation(of sheet: ChordSheet, attending: Bool) -> Situation {
        if sheet.typing {
            let at = sheet.typingAt
            return .typing(onChord: !sheet.chordAt(at.row, at.at).isEmpty, empty: sheet.entry.text.isEmpty)
        }
        if sheet.dragged != nil { return .carrying }
        guard attending, sheet.usable(sheet.spot.row) else { return .nothing }
        return sheet.chordAt(sheet.spot.row, sheet.spot.at).isEmpty ? .onPlace : .onChord
    }

    /// The highlighted line: which row of the sheet is the line the person is at, if
    /// they are at one. The line a chord is being typed on, wherever the pointer has
    /// gone on to since; else the spot's line, while the person is at the sheet or
    /// carries a chord over it. It is where Command+C copies from and Command+V
    /// pastes to, which is why it is shown.
    public static func line(of sheet: ChordSheet, attending: Bool) -> Int? {
        let row: Int
        switch situation(of: sheet, attending: attending) {
        case .nothing: return nil
        case .typing: row = sheet.typingAt.row
        case .carrying, .onChord, .onPlace: row = sheet.spot.row
        }
        return sheet.usable(row) ? row : nil
    }

    /// What a paste would do in a sheet just now.
    public static func paste(in sheet: ChordSheet) -> Paste {
        guard let copied = sheet.copied else {
            let row = sheet.spot.row
            return Paste(lines: nil, replaces: sheet.usable(row) && !sheet.rows[row].chords.isEmpty)
        }
        return Paste(lines: copied.count, replaces: sheet.pasteWouldReplace)
    }

    /// What to say: the first is the one most worth reading, and where there is not
    /// room for all of them the last are left off. `held`: Option or Command is down.
    /// `paste`: what a paste would do just now.
    public static func hints(_ situation: Situation, held: Bool = false, paste: Paste = Paste()) -> [Hint] {
        switch situation {
        case .typing(onChord: true, empty: true):
            return [Hint([enter], "takes this chord off"),
                    Hint([escape], "leaves it as it was"),
                    Hint("or type the chord that is to be here")]
        case .typing:
            return [Hint([enter], "keeps it"),
                    Hint([tab], "keeps it and goes on to the next word"),
                    Hint(["\u{2191}", "\u{2193}"], "choose from what is offered"),
                    Hint([escape], "throws it away"),
                    Hint("a click on another word keeps it too")]
        case .carrying:
            return held
                ? [Hint("Let go on a word to put a copy there")]
                : [Hint("Let go on a word to move it there"),
                   Hint([option], "held as you let go, leaves a copy behind")]
        case .onChord:
            if held { return copying(paste) }
            return [Hint([delete], "takes this chord off"),
                    Hint([numbers], "change it"),
                    Hint("drag it to another word"),
                    Hint("click to type over it")]
        case .onPlace:
            if held { return copying(paste) }
            return [Hint([numbers], "put one of the key\u{2019}s chords here"),
                    Hint([letters], "or a click: type one"),
                    Hint([option], "hold, to copy a line\u{2019}s chords onto another")]
        case .nothing:
            // (Whatever is held: there is no highlighted line to speak of.)
            return idle
        }
    }

    /// The copying of a line's chords onto another, which is done with keys alone and
    /// so is seen nowhere on the sheet. The words are the maintainer's (2026-10-13):
    /// "Press Command-C to copy all chords on the highlighted line. Press Command-V to
    /// paste previously copied chords to the highlighted line", and after it, where
    /// the line it would go onto has chords, " (Replaces existing chords)".
    ///
    /// One thing is added to them. A whole verse or chorus that was copied
    /// (Shift+Command+C) goes onto as many lines as it has, from the highlighted one
    /// on; so for that, and only then, the hint says "and the lines after it".
    static func copying(_ paste: Paste) -> [Hint] {
        let onto = (paste.lines ?? 1) > 1 ? "to the highlighted line and the lines after it" : "to the highlighted line"
        return [Hint(lead: "Press", [command + "C"], "to copy all chords on the highlighted line.", needed: true),
                Hint(lead: "Press", [command + "V"], "to paste previously copied chords \(onto)" + (paste.replaces ? " (Replaces existing chords)" : ""), needed: true)]
    }

    /// What is said while the person is not at the sheet: where to point to be told.
    public static let idle = [Hint("Point at a word or a chord, and this line says what the keys do there.")]

    /// What is said while ProPresenter is running and nothing can be put on.
    public static let cannotChange = [Hint("Nothing can be changed here while ProPresenter is running.")]
}
