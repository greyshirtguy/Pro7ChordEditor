// What the editors are handed: one entry for each text box that holds the song's words,
// in the order of the slides and, on a slide, of its boxes (spec/05-song-and-saving.md,
// "What the editors are handed"; spec/01-file-format.md says which boxes those are).

import ChordRules

public struct TextBox: Hashable, Sendable {
    /// Which slide it is on, counted from 0 in the order the slides are shown.
    public var slide: Int
    /// The name of the slide's group, or "" for none.
    public var group: String
    /// The group's colour, if the file gives it one.
    public var groupColour: Colour?
    /// Whether this slide is the first of its group.
    public var startsItsGroup: Bool
    /// The text box's id, by which a change names it. Empty for a blank slide that has
    /// no text box yet: one is made when a chord is put there.
    public var element: String
    /// Its plain text.
    public var words: Words
    /// Its chords in order, each a place along the words and a name. As the file has
    /// them: a name is not tidied on the way in, nor held to being a chord.
    public var chords: [Chord]
    /// Whether it is a blank slide's box: no words, no chords, and handed over only so
    /// that chords can be put on it.
    public var blank: Bool

    public init(slide: Int, group: String = "", groupColour: Colour? = nil, startsItsGroup: Bool = false,
                element: String, words: Words, chords: [Chord] = [], blank: Bool = false) {
        self.slide = slide
        self.group = group
        self.groupColour = groupColour
        self.startsItsGroup = startsItsGroup
        self.element = element
        self.words = words
        self.chords = chords
        self.blank = blank
    }
}

/// A colour as the file has one: each part from nought to one.
public struct Colour: Hashable, Sendable {
    public var red: Double
    public var green: Double
    public var blue: Double
    public var alpha: Double

    public init(red: Double, green: Double, blue: Double, alpha: Double = 1) {
        self.red = red
        self.green = green
        self.blue = blue
        self.alpha = alpha
    }
}
