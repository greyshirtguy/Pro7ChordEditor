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
//   with Option held,     the copying of a line's chords onto another: Command+C,
//   or Command            Shift+Command+C and Command+V, which no picture on the sheet
//                         shows;
//   the pointer elsewhere one thing worth knowing, and after some seconds another.
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
        public var keys: [String]
        public var says: String

        public init(_ keys: [String], _ says: String) {
            self.keys = keys
            self.says = says
        }

        /// A hint that is words alone.
        public init(_ says: String) {
            self.init([], says)
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

    /// How many chords have been copied, if any have been.
    public static func copied(in sheet: ChordSheet) -> Int? {
        sheet.copied.map { lines in
            lines.reduce(0) { sum, line in
                switch line {
                case .words(let chords): sum + chords.count
                case .alone(let names): sum + names.count
                }
            }
        }
    }

    /// What to say: the first is the one most worth reading, and where there is not
    /// room for all of them the last are left off. `held`: Option or Command is down.
    /// `copied`: how many chords have been copied, if any. `tip`: which of the tips
    /// it is the turn of, where there is nothing in particular to say.
    public static func hints(_ situation: Situation, held: Bool = false, copied: Int? = nil, tip: Int = 0) -> [Hint] {
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
        case .nothing, .onChord, .onPlace:
            if held { return copying(copied) }
            if situation == .onChord {
                return [Hint([delete], "takes this chord off"),
                        Hint([numbers], "change it"),
                        Hint("drag it to another word"),
                        Hint("click to type over it")]
            }
            if situation == .onPlace {
                return [Hint([numbers], "put one of the key\u{2019}s chords here"),
                        Hint([letters], "or a click: type one"),
                        Hint([option], "hold, to copy a line\u{2019}s chords onto another")]
            }
            return tips[((tip % tips.count) + tips.count) % tips.count]
        }
    }

    /// The copying of a line's chords onto another, which is done with keys alone
    /// and so is seen nowhere on the sheet. "This line" is the spot's. The two that
    /// go together come first, so that where there is not room for all three it is
    /// the whole verse that is left off.
    static func copying(_ copied: Int?) -> [Hint] {
        let pastes = copied.map { "pastes the \($0) copied from this line on, word for word" } ?? "pastes them from this line on, word for word"
        return [Hint([command + "C"], "copies this line\u{2019}s chords"),
                Hint([command + "V"], pastes),
                Hint([shift + command + "C"], "copies the whole verse\u{2019}s or chorus\u{2019}s")]
    }

    /// Things worth knowing, said one at a time while nothing else is to be said.
    public static let tips: [[Hint]] = [
        [Hint("Point at a word and press"), Hint([numbers], "the key\u{2019}s chord of that number goes on")],
        [Hint("Point at a chord and press"), Hint([delete], "it is taken off")],
        [Hint([option], "hold it down to see how a line\u{2019}s chords are copied onto another")],
        [Hint("Drag a chord to another word to move it"), Hint([option], "held, copies it")],
        [Hint("Drag one of the seven chords out of the strip onto a word")],
        [Hint([command + "Z"], "takes the last change back"), Hint([shift + command + "Z"], "puts it back")],
        [Hint([shift], "held, keeps the spot to the starts of words")],
        [Hint(["\u{2190}", "\u{2192}", "\u{2191}", "\u{2193}"], "move the spot"), Hint([enter], "types a chord there")],
        [Hint([command + "1"], "shows the sheet"), Hint([command + "2"], "the song as ChordPro text")],
        [Hint([command + "\u{2212}", command + "+"], "make the words smaller and larger")],
    ]

    /// How long one tip stays before the next.
    public static let tipLasts = 9.0

    /// What is said while ProPresenter is running and nothing can be put on.
    public static let cannotChange = [Hint("Nothing can be changed here while ProPresenter is running.")]
}
