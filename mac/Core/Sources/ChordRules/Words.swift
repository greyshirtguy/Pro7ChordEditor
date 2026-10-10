// Words: a text box's words as the file counts them.
//
// A chord's place in a song's file is a number: how many UTF-16 units into the plain
// text of its text box the chord stands, a line break being one (spec/01-file-format.md,
// "The words, and counting along them"). Swift's String does not count that way. Its
// `count`, `prefix(n)` and the distances between its indexes go by whole characters, so
// that an emoji, or a letter with an accent laid over it, is one where the file counts
// two or more; and it calls two strings equal that are written with different units
// ("é" as one unit, and as "e" with an accent after it). A place worked out with any of
// those is wrong for some songs and right for most, which is the worst kind of wrong.
//
// So the words of a text box are kept here as the units themselves, and this is the one
// small type the rest of the code goes through (spec/10-platforms.md, "Counting
// characters"). A place is an `Int` that indexes `units`. There is no way to ask this
// type how many characters it has, on purpose.
//
// It can also hold what a String cannot: a unit that is half of a pair with its other
// half missing. An RTF can say such a thing, and ProPresenter counts it as one place, so
// it has to be kept to keep the count. `string` turns each of those into U+FFFD; nothing
// that is written to a file is ever made from `string`.

/// A text box's words (or one line of them) as UTF-16 units. A place in them is an index
/// into `units`.
public struct Words: Hashable, Sendable {
    /// The line break, as it is in the words: one unit, whatever the RTF wrote it as.
    public static let lineBreak: UInt16 = 0x000A

    public var units: [UInt16]

    public init(units: [UInt16] = []) {
        self.units = units
    }

    public init(_ string: String) {
        self.units = Array(string.utf16)
    }

    /// The words as a String, to show or to compare by eye. Not for counting along.
    public var string: String {
        String(decoding: units, as: UTF16.self)
    }

    /// How many places there are: the number of units.
    public var length: Int { units.count }

    public var isEmpty: Bool { units.isEmpty }

    /// The unit at a place.
    public subscript(place: Int) -> UInt16 { units[place] }

    /// The lines, parted at each line break. There is always at least one; words that
    /// end with a line break end with an empty line.
    public var lines: [Words] {
        units.split(at: Words.lineBreak).map(Words.init(units:))
    }

    /// Whether there is nothing here to read: no units, or none but white space, as the
    /// rules count white space (Units.swift). A zero-width space is not white space, so
    /// a line of stand-ins is not blank.
    public var isBlank: Bool {
        units.allSatisfy(isSpace)
    }

    /// Whether a unit is anywhere in the words.
    public func contains(_ unit: UInt16) -> Bool {
        units.contains(unit)
    }
}

extension Words: ExpressibleByStringLiteral {
    public init(stringLiteral value: String) {
        self.init(value)
    }
}

extension Words: CustomStringConvertible, CustomDebugStringConvertible {
    public var description: String { string }
    public var debugDescription: String { string.debugDescription }
}
