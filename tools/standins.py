#!/usr/bin/env python3
"""Stand-ins written into an RTF: the reference for that one rule, and the maker of
testdata/vectors/stand-ins-in-rtf.json.

    tools/standins.py testdata/vectors        writes stand-ins-in-rtf.json there

Why there is such a rule. A chord with no words under it (an intro, a turnaround) hangs
in the file on an invisible character, a stand-in. Putting one more chord on such a
line, or taking one off, changes how many stand-ins the line needs, and so the RTF the
words are kept in: the one case in which either app writes words
(spec/01-file-format.md, "Chords with no words"). It is new. Simple Presenter reads the
whole RTF into a model and writes it out again in its own words, which suits an app
that edits text and would here rewrite the RTF of someone's library for the sake of
one line. So unlike most of the vectors these do not record what that app does. They
record what THIS does, so that the two new apps write the same bytes.

The rule, for a line of a text box that is to become a given run of stand-ins.
  1. Read the RTF (proread.written), keeping for every unit of its plain text the
     bytes it was written with: a plain character's one byte; a control word's
     backslash, letters, number and the space that ends it if it has one; `\\'hh`'s
     four; a character written straight in as UTF-8, its bytes.
  2. The line is the units between two line breaks. It must have at least one, or
     there is nothing to say where it is.
  3. What the line has already of what it is to become, counted from its start, is
     kept as it is written.
  4. The bytes of every unit of the line after that are taken out: the units' own, and
     none of the bytes that lie between them (control words, which say how what
     follows is set; in a song from Multitracks every other stand-in is in another
     font).
  5. What is still to come of the stand-ins is put where the first unit taken out
     began, or, if none was taken out, just after the line's last unit. A plain space
     is written as a space; U+200B and U+2001 as a backslash, "uc0", a backslash, "u",
     the unit's number and a space, which is how Apple's text system writes any
     character above ASCII.
  6. If what is put in starts with a plain space, and a control word with no space of
     its own ends exactly there, a space is put in first to end that word.
  7. The result is read again. If its plain text is not the old text with that line
     changed and nothing else, there is no result: the change is not made.

And for a text box that has no words at all, which is given another's opening:
  8. The opening is the other's RTF up to the first byte of its first unit. (If it has
     no unit there is no result.) After it go the stand-ins as in 5, and then as many
     closing curly brackets as there are groups open at that place. (They are always
     zero-width spaces here, which begin with a backslash, so 6 has nothing to do.)
  9. The result is read again, and must be the stand-ins and nothing else.
"""
import base64
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import proread

ZERO_WIDTH = chr(0x200B)
EM_QUAD = chr(0x2001)


def stand_ins(count, stand_in=ZERO_WIDTH):
    """What a line of that many chords alone is: a stand-in for each, an em quad between."""
    return EM_QUAD.join(stand_in for _ in range(count))


def spelt(units):
    """The bytes a run of stand-ins is written with, or None if it holds anything else."""
    out = b""
    for unit in units:
        if unit == " ":
            out += b" "
        elif unit in (ZERO_WIDTH, EM_QUAD):
            out += b"\\uc0\\u" + str(ord(unit)).encode("ascii") + b" "
        else:
            return None
    return out


def line_of(units, line):
    """Where a line is among the units of a plain text: (start, end), or None."""
    if line < 0:
        return None
    start = 0
    for _ in range(line):
        breaks = [n for n in range(start, len(units)) if units[n][0] == "\n"]
        if not breaks:
            return None
        start = breaks[0] + 1
    end = start
    while end < len(units) and units[end][0] != "\n":
        end += 1
    return start, end


def with_line(rtf, line, wanted):
    """The RTF with one line of its plain text made `wanted`, a run of stand-ins, and
    every byte that is not that line's characters left as it was. None if it cannot be
    done cleanly."""
    units = proread.written(rtf)
    found = line_of(units, line)
    if not found:
        return None
    start, end = found
    if end == start:
        return None
    same = 0
    while start + same < end and same < len(wanted) and units[start + same][0] == wanted[same]:
        same += 1
    put = spelt(wanted[same:])
    if put is None:
        return None
    going = units[start + same:end]
    for one, following in zip(going, going[1:]):
        if following[1] < one[2]:
            return None
    if going:
        at, lost = going[0][1], going[0][3]
    else:
        at, lost = units[end - 1][2], units[end - 1][4]
    if lost and put[:1] == b" ":
        put = b" " + put
    made = rtf[:at] + put
    after = at
    for one in going:
        made += rtf[after:one[1]]
        after = one[2]
    made += rtf[after:]
    text = "".join(unit[0] for unit in units)
    if proread.plain_text(made) != text[:start + same] + wanted[same:] + text[end:]:
        return None
    return made


def from_opening(model, wanted):
    """A new RTF: the opening of `model`, up to where its words begin, then `wanted`, a
    run of stand-ins, then the brackets that opening left open. None if the model has
    no character, or what is made does not read back as the stand-ins alone."""
    units = proread.written(model)
    put = spelt(wanted)
    if not units or put is None:
        return None
    _, start, _, _, _, depth = units[0]
    made = model[:start] + put + b"}" * depth
    if proread.plain_text(made) != wanted:
        return None
    return made


# ---- The cases
#
# RTF as Apple's text system writes it, in the shapes real songs were found to have
# (mac/NOTES.md, "At M7"), with made-up words. None of it is anyone's song.

def apple(body):
    return (b"{\\rtf1\\ansi\\ansicpg1252\\cocoartf2761\n"
            b"\\cocoatextscaling0\\cocoaplatform0{\\fonttbl\\f0\\fswiss\\fcharset0 Helvetica;\\f1\\fnil\\fcharset0 LucidaGrande;}\n"
            b"{\\colortbl;\\red255\\green255\\blue255;\\red255\\green255\\blue255;}\n"
            b"{\\*\\expandedcolortbl;;\\csgenericrgb\\c100000\\c100000\\c100000;}\n"
            b"\\pard\\pardirnatural\\qc\\partightenfactor0\n\n" + body)


Z = spelt(ZERO_WIDTH)
Q = spelt(EM_QUAD)
# The same two with no `\uc0` before them and no space after.
BARE_Z = Z[len(b"\\uc0"):-1]
BARE_Q = Q[len(b"\\uc0"):-1]


def multitracks(count, first=b"\\f0\\fs160 \\cf2 "):
    """That many chords with no words as Multitracks writes them: each stand-in in the
    song's font, each em quad between two in another."""
    out = first + Z
    for _ in range(count - 1):
        out += b"\n\\f1 \\cf2 " + Q + b"\n\\f0 \\cf2 " + Z
    return out


def on_spaces(count):
    """The same hung on plain spaces."""
    return (b"\\uc0\\u" + str(ord(EM_QUAD)).encode() + b" ").join(b" " for _ in range(count))


SHAPES = [
    ("two chords, as Multitracks writes them", apple(multitracks(2) + b"}")),
    ("four chords, as Multitracks writes them", apple(multitracks(4) + b"}")),
    ("one chord on a zero-width space", apple(b"\\f0\\fs160 \\cf2 " + Z + b"}")),
    ("three chords on plain spaces", apple(b"\\f0\\fs84 \\cf2 " + on_spaces(3) + b"}")),
    ("one chord on one plain space", apple(b"\\f0\\fs84 \\cf2  }")),
    ("one chord on one plain space, outlined", apple(b"\\f0\\fs134 \\cf2 \\outl0\\strokewidth-60 \\strokec3  }")),
    ("words over two chords on plain spaces", apple(b"\\f0\\fs84 \\cf2 Made up words\\\n" + on_spaces(2) + b"}")),
    ("three chords on plain spaces over words", apple(b"\\f0\\fs84 \\cf2 " + on_spaces(3) + b"\\\nMade up words}")),
    ("words between two lines of chords alone", apple(multitracks(2) + b"\\\nMade up words\\\n" + multitracks(3, b"") + b"}")),
    ("two lines of chords alone", apple(multitracks(4) + b"\\\n" + multitracks(2, b"") + b"}")),
    ("three plain spaces, which is no run of stand-ins", apple(b"\\f0\\fs84 \\cf2    }")),
    ("a space and an em quad whose word runs up to the bracket", apple(b"\\cf2  \\uc0" + BARE_Q + b"}")),
    ("an em quad straight after a word with no space, and a space", apple(b"\\cf2\\uc0" + BARE_Q + b"  }")),
    ("an em quad written straight in, straight after a word with no space, and a space",
     b"{\\rtf1\\ansi\\cf2" + EM_QUAD.encode("utf-8") + b" }"),
    ("stand-ins with no space after their words", apple(b"\\f0 \\uc0" + BARE_Z + b"\\uc0" + BARE_Q + b"\\uc0" + BARE_Z + b"}")),
    ("stand-ins each in a group of its own", apple(b"{\\f0 " + Z + b"}{\\f1 " + Q + b"}{\\f0 " + Z + b"}}")),
    ("stand-ins written straight in as UTF-8", b"{\\rtf1\\ansi " + (ZERO_WIDTH + EM_QUAD + ZERO_WIDTH).encode("utf-8") + b"}"),
    ("a line break written as a word, and chords alone under it", b"{\\rtf1\\ansi\\uc0 Made up words\\par " + BARE_Z + b" " + BARE_Q + b" " + BARE_Z + b" }"),
    ("a break inside a paragraph, and chords alone after it", b"{\\rtf1\\ansi\\uc0 Made up\\line " + BARE_Z + b" " + BARE_Q + b" " + BARE_Z + b" \\par words}"),
    ("each stand-in with a character to stand in for it, which cannot be changed cleanly",
     b"{\\rtf1\\ansi " + BARE_Z + b"?" + BARE_Q + b"?" + BARE_Z + b"?\\par Words " + b"\\u" + b"8217?}"),
    ("an empty document, as ProPresenter leaves a text box with no text",
     b"{\\rtf1\\ansi\\ansicpg1252\\cocoartf2761\n\\cocoatextscaling0\\cocoaplatform0{\\fonttbl}\n{\\colortbl;\\red255\\green255\\blue255;}\n{\\*\\expandedcolortbl;;}\n}"),
    ("an empty line between two lines of words", apple(b"\\f0\\fs84 \\cf2 Made up\\\n\\\nwords}")),
]

MODELS = [
    ("words, outlined", apple(b"\\f0\\fs134 \\cf2 \\outl0\\strokewidth-60 \\strokec3 Made up words\\\nAnd more}")),
    ("words that begin inside groups", b"{\\rtf1\\ansi{\\fonttbl{\\f0 Arial;}}{\\pard{\\f0\\fs40 Words}\\par}}"),
    ("words that begin with an empty line", apple(b"\\f0\\fs84 \\cf2 \\\nWords}")),
    ("a word with no space of its own straight before the words", apple(b"\\f0\\fs84\\cf2\\'e9t\\'e9}")),
]


def b64(data):
    return None if data is None else base64.b64encode(data).decode("ascii")


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    vectors = sys.argv[1]
    wrong = proread.check_plain_text(vectors)
    if wrong:
        sys.exit("the reader of RTF here disagrees with the vectors made from Simple Presenter's: %s" % wrong)

    in_place = []
    made = refused = 0

    def try_line(name, rtf, line, count, stand_in):
        nonlocal made, refused
        wanted = stand_ins(count, stand_in)
        out = with_line(rtf, line, wanted)
        if out is None:
            refused += 1
        else:
            made += 1
            # What the rule promises, held here too, apart from the function itself:
            # outside the line's own bytes nothing changes, and the text is as meant.
            units = proread.written(rtf)
            start, end = line_of(units, line)
            before, after = units[start][1], len(rtf) - units[end - 1][2]
            assert out[:before] == rtf[:before] and out[len(out) - after:] == rtf[len(rtf) - after:], name
            lines = proread.plain_text(rtf).split("\n")
            lines[line] = wanted
            assert proread.plain_text(out).split("\n") == lines, name
        in_place.append({"name": name, "rtfBase64": b64(rtf), "line": line, "standIn": ord(stand_in), "count": count, "outBase64": b64(out)})

    for name, rtf in SHAPES:
        lines = proread.plain_text(rtf).split("\n")
        for line in range(len(lines) + 1):
            # What the line hangs its chords on now, as the apps tell it: a plain space
            # if it has spaces and no zero-width space. Every number of chords from
            # none to four on that, and two on the other, which the apps never ask for.
            if line == len(lines):
                # (A line the text has not got: once is enough.)
                try_line("%s: line %d, which it has not got" % (name, line), rtf, line, 1, ZERO_WIDTH)
                continue
            text = lines[line]
            own = " " if " " in text and ZERO_WIDTH not in text else ZERO_WIDTH
            other = ZERO_WIDTH if own == " " else " "
            for count, stand_in in [(count, own) for count in range(0, 5)] + [(2, other)]:
                try_line("%s: line %d to %d on %s" % (name, line, count, "spaces" if stand_in == " " else "zero-width spaces"),
                         rtf, line, count, stand_in)
    with open(os.path.join(vectors, "rtf-text.json"), encoding="utf-8") as file:
        samples = json.load(file)["cases"]
    for sample in samples:
        rtf = base64.b64decode(sample["rtfBase64"])
        for line, text in enumerate(sample["text"].split("\n")):
            if not text:
                continue
            for count, stand_in in ((0, ZERO_WIDTH), (2, ZERO_WIDTH), (3, " ")):
                try_line("rtf-text.json, %s: line %d to %d on %s" % (sample["name"], line, count, "spaces" if stand_in == " " else "zero-width spaces"),
                         rtf, line, count, stand_in)

    openings = []
    for name, rtf in MODELS + SHAPES + [("rtf-text.json, " + s["name"], base64.b64decode(s["rtfBase64"])) for s in samples]:
        for count in (1, 3):
            out = from_opening(rtf, stand_ins(count))
            if out is not None:
                start = proread.written(rtf)[0][1]
                assert out[:start] == rtf[:start] and proread.plain_text(out) == stand_ins(count), name
            openings.append({"name": "%s: %d" % (name, count), "modelBase64": b64(rtf), "count": count, "outBase64": b64(out)})

    with open(os.path.join(vectors, "stand-ins-in-rtf.json"), "w", encoding="utf-8") as out:
        json.dump({
            "about": "Writing the characters that chords with no words hang on into a text box's RTF (spec/01-file-format.md, \"Chords with "
                     "no words\" and \"Chords onto a blank slide\"). `inPlace`: the RTF with line `line` of its plain text made `count` "
                     "stand-ins (`standIn` is the unit each chord hangs on, 8203 or 32, with U+2001 between two), every other byte left as "
                     "it was; `outBase64` is null where that cannot be done cleanly, and the change is then refused. `fromAnOpening`: a new "
                     "RTF for a box with no words, made of the opening of `modelBase64` and `count` stand-ins on U+200B; null where the "
                     "model has no opening to give. The RTF is in base64 because some of it is not text.",
            "madeBy": "tools/standins.py, which is the reference for this rule. Do not edit by hand: run it again.",
            "inPlace": in_place, "fromAnOpening": openings,
        }, out, indent=1, ensure_ascii=False)
        out.write("\n")
    print("stand-ins-in-rtf.json: %d changes of a line, %d made and %d that cannot be; %d openings, %d made"
          % (len(in_place), made, refused, len(openings), sum(1 for o in openings if o["outBase64"])))


if __name__ == "__main__":
    main()
