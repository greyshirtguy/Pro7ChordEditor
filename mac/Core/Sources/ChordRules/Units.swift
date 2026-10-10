// What the rules do to text, done on UTF-16 units.
//
// The rules were written in C++ for Qt (reference/simple-presenter/src/chords.cpp),
// whose QString is an array of UTF-16 units: its `size`, `indexOf`, `mid`, `trimmed`,
// `startsWith` and `==` all go unit by unit. The answers in testdata/vectors were made
// by that code. Swift's String does every one of those by whole characters instead, and
// Foundation has its own idea of what white space is, so the port does not use them: it
// turns a String into its units on the way in, works on `[UInt16]` with the few
// operations below, and turns the units back on the way out. Each is named for the
// QString function it stands in for and does what that does.
//
// Two things here are definitions, not conveniences, and spec/02-chord-rules.md ("To
// the letter") has them for the Windows app to match:
//
//   White space is Qt's list (`isSpace`), written out below. It is NOT Foundation's
//   `CharacterSet.whitespaces`: that one holds U+200B, the zero-width space, which is
//   the very character a line of chords alone is made of (Chords.placeholders). Trimming
//   by Foundation's list would empty such a line. Checked on macOS 26.
//
//   A letter or a number is asked of each unit by itself (`isLetterOrNumber`), by its
//   Unicode category. So a letter from beyond the first 65,536 characters is not one:
//   it is two units, neither of which is a letter.

typealias Units = [UInt16]

/// One character of the first 65,536 as a unit: `u("/")`.
func u(_ scalar: Unicode.Scalar) -> UInt16 {
    precondition(scalar.value <= 0xFFFF, "not one UTF-16 unit")
    return UInt16(scalar.value)
}

func units(_ string: String) -> Units {
    Array(string.utf16)
}

func string(_ units: Units) -> String {
    String(decoding: units, as: UTF16.self)
}

/// QChar::isSpace: tab to carriage return, space, U+0085, U+00A0, and what Unicode calls
/// a space, line or paragraph separator.
func isSpace(_ unit: UInt16) -> Bool {
    switch unit {
    case 0x0009...0x000D, 0x0020, 0x0085, 0x00A0, 0x1680, 0x2000...0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000:
        return true
    default:
        return false
    }
}

/// QChar::isLetterOrNumber: the Unicode categories of letters and of numbers. Half of a
/// pair is neither.
func isLetterOrNumber(_ unit: UInt16) -> Bool {
    guard let scalar = Unicode.Scalar(unit) else { return false }
    switch scalar.properties.generalCategory {
    case .uppercaseLetter, .lowercaseLetter, .titlecaseLetter, .modifierLetter, .otherLetter,
         .decimalNumber, .letterNumber, .otherNumber:
        return true
    default:
        return false
    }
}

func isASCIILetter(_ unit: UInt16) -> Bool {
    (0x41...0x5A).contains(unit) || (0x61...0x7A).contains(unit)
}

func isASCIIDigit(_ unit: UInt16) -> Bool {
    (0x30...0x39).contains(unit)
}

/// The small letter of a capital A to Z; anything else as it is.
func asciiLowered(_ unit: UInt16) -> UInt16 {
    (0x41...0x5A).contains(unit) ? unit + 0x20 : unit
}

func asciiRaised(_ unit: UInt16) -> UInt16 {
    (0x61...0x7A).contains(unit) ? unit - 0x20 : unit
}

extension Array where Element == UInt16 {
    /// QString::trimmed: without the white space at its ends.
    func trimmed() -> Units {
        var start = 0
        var end = count
        while start < end && isSpace(self[start]) { start += 1 }
        while end > start && isSpace(self[end - 1]) { end -= 1 }
        return Array(self[start..<end])
    }

    /// QString::indexOf: where a unit first is, at or after `from`; -1 if it is not.
    func indexOf(_ unit: UInt16, from: Int = 0) -> Int {
        var i = Swift.max(0, from)
        while i < count {
            if self[i] == unit { return i }
            i += 1
        }
        return -1
    }

    /// QString::lastIndexOf: where a unit last is; -1 if it is not.
    func lastIndexOf(_ unit: UInt16) -> Int {
        lastIndex(of: unit) ?? -1
    }

    /// QString::mid: from a place to the end, or `length` units of it. What is asked
    /// for beyond the ends is not there.
    func mid(_ from: Int, _ length: Int? = nil) -> Units {
        let start = Swift.min(Swift.max(0, from), count)
        let end = length.map { Swift.min(count, Swift.max(start, start + $0)) } ?? count
        return Array(self[start..<end])
    }

    /// QString::left: the first `length` units.
    func left(_ length: Int) -> Units {
        mid(0, length)
    }

    func startsWith(_ other: Units) -> Bool {
        count >= other.count && Array(self[0..<other.count]) == other
    }

    func startsWith(_ text: String) -> Bool {
        startsWith(units(text))
    }

    func endsWith(_ unit: UInt16) -> Bool {
        last == unit
    }

    /// QString::split: the parts between each of a unit, the empty ones kept. Always at
    /// least one part.
    func split(at separator: UInt16) -> [Units] {
        var parts: [Units] = []
        var part: Units = []
        for unit in self {
            if unit == separator {
                parts.append(part)
                part = []
            } else {
                part.append(unit)
            }
        }
        parts.append(part)
        return parts
    }
}

extension Array {
    /// Sorted, with those that are equal left in the order they came in. Swift does not
    /// promise that of its own `sort`, and the rules depend on it (the reference uses
    /// std::stable_sort everywhere).
    func stablySorted(by areInIncreasingOrder: (Element, Element) -> Bool) -> [Element] {
        enumerated()
            .sorted { a, b in
                if areInIncreasingOrder(a.element, b.element) { return true }
                if areInIncreasingOrder(b.element, a.element) { return false }
                return a.offset < b.offset
            }
            .map(\.element)
    }
}
