#!/usr/bin/env python3
"""Sharps and flats drawn as the music signs: the reference for that one rule, and the
maker of testdata/vectors/signs.json.

    tools/signs.py testdata/vectors        writes signs.json there

Why there is such a rule. A chord's name is written with "#" for a sharp and "b" for a
flat, in the song's file and wherever it is typed. The chord sheet draws the names, and
the maintainer asked (2026-10-14) for those that are clearly a sharp or a flat to be
drawn as the signs a musician writes, ♯ (U+266F) and ♭ (U+266D): "just a visual skin -
not changing the underlying data", with the ChordPro text left as it is, and a setting
to turn it off. It is new: Simple Presenter has no such thing. So these vectors record
what THIS does, so that the apps draw the same.

The rule, `withSigns(name)`:
  1. Only a name that the rules take for a chord (`isChord`: it starts with a note, and
     the rest of it is written only with what a chord's name is written with, so no
     space), and whose root's letter is a capital A to G. Anything else is given back
     as it is: "N.C.", "Bb riff", "C /// | Am ///". So is a name whose root is a small
     letter, which may be no note at all: in "b3/b5", a chord written in numbers, both
     are flats, and the rules would read the first as the note B.
  2. The root's sharps and flats, every one: "F#" is F♯, "Bbm" B♭m, "F##" F♯♯.
  3. The bass's, if there is a bass (`parts`: the note after the last slash, when that
     is a note and nothing more) and its letter is a capital: "Eb/G" is E♭/G and "C/Bb"
     C/B♭; "C/bb" is left.
  4. Between them, a "#" or a "b" that has a digit, 0 to 9, straight after it: "C7b9" is
     C7♭9, "Dm7(b5)" Dm7(♭5), "Cmaj7#11" Cmaj7♯11. No other: "Bm7b", half typed, keeps
     its "b" until the 5 comes. (No kind of chord has a "b" between its notes for
     anything but a flat, but a word a writer put in a chord's name may.)
  5. A sign that is in the name already is left as it is; and nothing else is touched.
     A "#" and its sign are one UTF-16 unit each, and so are a "b" and its: the name
     drawn is as long as the name, and a place in one is the same place in the other.

And `withoutSigns(text)`, its other way: every ♯ as "#" and every ♭ as "b", and nothing
else changed. What the box a chord is typed in holds is read so before anything is done
with it, the box showing the signs as they are typed (spec/03-chord-sheet.md, rule 30a).

What is done is done on UTF-16 units, as everywhere in the rules (spec/02-chord-rules.md,
"To the letter"), and "a letter or a number" is asked of each unit by itself.
"""
import json
import os
import sys
import unicodedata

SHARP = "\u266f"
FLAT = "\u266d"
NOTE_LETTERS = [ord(c) for c in "ABCDEFGabcdefg"]
CAPITALS = [ord(c) for c in "ABCDEFG"]


def units(text):
    data = text.encode("utf-16-le")
    return [int.from_bytes(data[i:i + 2], "little") for i in range(0, len(data), 2)]


def string(us):
    return b"".join(u.to_bytes(2, "little") for u in us).decode("utf-16-le")


def read_note(us, start=0):
    """How many units a note takes from `start`: a letter A to G of either case, then
    sharps ("#" or the sign, any number) or else flats ("b" or the sign, any number).
    Nought if there is no note there."""
    if start >= len(us) or us[start] not in NOTE_LETTERS:
        return 0
    i = start + 1
    while i < len(us) and us[i] in (ord("#"), ord(SHARP)):
        i += 1
    if i == start + 1:
        while i < len(us) and us[i] in (ord("b"), ord(FLAT)):
            i += 1
    return i - start


def letter_or_number(unit):
    # (A unit that is half of a character beyond the first 65,536 is neither.)
    if 0xD800 <= unit <= 0xDFFF:
        return False
    return unicodedata.category(chr(unit)) in ("Lu", "Ll", "Lt", "Lm", "Lo", "Nd", "Nl", "No")


def chord_character(unit):
    return letter_or_number(unit) or chr(unit) in "#/+-()\u00b0\u00f8^.," + SHARP + FLAT + "\u0394"


def is_chord(us):
    length = read_note(us)
    return length > 0 and all(chord_character(u) for u in us[length:])


def bass_at(us):
    """Where the bass note starts, or -1: after the last slash after the root, if what
    follows that slash is a note and nothing more."""
    root = read_note(us)
    rest = us[root:]
    if ord("/") not in rest:
        return -1
    slash = root + len(rest) - 1 - rest[::-1].index(ord("/"))
    after = slash + 1
    length = read_note(us, after)
    return after if length > 0 and after + length == len(us) else -1


def with_signs(name):
    us = units(name)
    if not is_chord(us) or us[0] not in CAPITALS:
        return name
    out = list(us)

    def signs(start, length):
        for i in range(start + 1, start + length):
            out[i] = {ord("#"): ord(SHARP), ord("b"): ord(FLAT)}.get(us[i], us[i])

    root = read_note(us)
    signs(0, root)
    end = len(us)
    bass = bass_at(us)
    if bass >= 0:
        end = bass - 1
        if us[bass] in CAPITALS:
            signs(bass, len(us) - bass)
    for i in range(root, end):
        if us[i] in (ord("#"), ord("b")) and i + 1 < len(us) and ord("0") <= us[i + 1] <= ord("9"):
            out[i] = ord(SHARP) if us[i] == ord("#") else ord(FLAT)
    return string(out)


def without_signs(text):
    return text.replace(SHARP, "#").replace(FLAT, "b")


# Each with what it is there to show.
NAMES = [
    ("C", "nothing to draw"),
    ("Am7", "nothing to draw"),
    ("F#", "the root's sharp"),
    ("Bb", "the root's flat"),
    ("Bbm7", "the root's flat, and nothing in the quality"),
    ("Ebmaj7", "the root's flat"),
    ("Abadd9", "the root's flat; the quality's own letters are left"),
    ("Bbsus4", "the root's flat, and the s of sus left"),
    ("C#m7b5", "the root's sharp, and a flat before a digit"),
    ("Gb", "the root's flat"),
    ("Cb", "C flat: a root's flat however rare"),
    ("Cb5", "the rules read this as C flat with the quality 5"),
    ("E#m", "E sharp minor"),
    ("F##", "every one of the root's sharps"),
    ("Bbb", "every one of the root's flats"),
    ("Eb/G", "the root's flat, and a bass with none"),
    ("C/Bb", "the bass's flat"),
    ("D/F#", "the bass's sharp"),
    ("F#maj7/A#", "both notes' sharps"),
    ("Ab/Gb", "both notes' flats"),
    ("C7b9", "a flat before a digit"),
    ("C7#9", "a sharp before a digit"),
    ("Cmaj7#11", "a sharp before two digits"),
    ("Dm7(b5)", "a flat in brackets, before a digit"),
    ("G7(#5b9)", "two, each before a digit"),
    ("A7b13", "a flat before 13"),
    ("Bm7b", "half typed: no digit after the b yet"),
    ("C7#", "half typed: no digit after the # yet"),
    ("Fa#m", "Do Re Mi: the # follows a letter, not a digit, and the a is no flat"),
    ("Dob", "Do Re Mi: a b at the end"),
    ("C6/9", "a slash and no bass: nothing to draw"),
    ("C/Bb7", "no bass, as more than a note follows the slash: the b is before a digit"),
    ("C/bb", "a bass whose letter is small is left"),
    ("bb", "a root whose letter is small: left"),
    ("f#m", "a root whose letter is small: left"),
    ("b3/b5", "a chord in numbers: left"),
    ("bb7", "a chord in numbers: left"),
    ("N.C.", "no chord"),
    ("(x2)", "no chord"),
    ("Bb riff", "a space: not a chord, and left"),
    ("C /// | Am ///", "not a chord, and left"),
    ("Bb ", "a space after it: not a chord, and left"),
    ("", "nothing"),
    ("B\u266d", "a sign already there"),
    ("C7\u266d9", "a sign already there, before a digit"),
    ("F\u266f#", "a sign and a #: both the root's"),
    ("H7", "H is no note here"),
    ("Bb\u00b0", "the root's flat, and the little circle"),
    ("C#\u00f87", "the root's sharp, and the half-diminished sign"),
    ("E\u0394b9", "a triangle for major seventh, and a flat before a digit"),
    ("C7b\u0663", "a digit of another script is no digit 0 to 9"),
    ("Bb\U0001D12B", "a character beyond the first 65,536 is two units, neither a letter: not a chord"),
]

TEXTS = [
    ("F#m7", "nothing to undo"),
    ("F\u266fm7", "a sharp"),
    ("B\u266d", "a flat"),
    ("C\u266fm7\u266d5/G\u266f", "all three"),
    ("C7\u266d", "half typed"),
    ("\u266d\u266f", "signs alone are read so too"),
    ("", "nothing"),
]


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    vectors = sys.argv[1]
    whole = {
        "about": "A chord's name as the chord sheet draws it, with the music signs U+266F and U+266D for the # and b that are "
                 "clearly a sharp or a flat, and the other way: the rule at the top of tools/signs.py and in "
                 "spec/02-chord-rules.md, \"Sharps and flats drawn as signs\". `withSigns`: for each `in`, what withSigns(in) "
                 "is. `withoutSigns`: for each `in`, what withoutSigns(in) is. The `about` of each case says what it is there "
                 "to show. Only drawing: a name with signs is never written anywhere.",
        "madeBy": "tools/signs.py, which is itself the reference for this rule. Do not edit by hand: run it again.",
        "withSigns": [{"about": about, "in": name, "out": with_signs(name)} for name, about in NAMES],
        "withoutSigns": [{"about": about, "in": text, "out": without_signs(text)} for text, about in TEXTS],
    }
    for case in whole["withSigns"]:
        assert len(units(case["out"])) == len(units(case["in"])), case
        assert without_signs(case["out"]) == without_signs(case["in"]), case
    with open(os.path.join(vectors, "signs.json"), "w", encoding="utf-8") as file:
        json.dump(whole, file, indent=4, ensure_ascii=False)
        file.write("\n")
    print("signs.json")
    for case in whole["withSigns"]:
        print("  %-18s %-18s %s" % (repr(case["in"]), repr(case["out"]), case["about"]))


if __name__ == "__main__":
    main()
