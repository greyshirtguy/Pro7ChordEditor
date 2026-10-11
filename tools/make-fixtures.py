#!/usr/bin/env python3
"""Makes the test files that are derived from others, and says what is in every one.

    tools/make-fixtures.py

It reads testdata/files/made-by-simple-presenter (presentations that Simple Presenter's
ChordPro import made of testdata/songs: testdata/README.md says how) and writes

  testdata/files/as-the-old-windows-app-left-them/   those songs as version 0.9.x of the
        Windows chord editor would have saved them: see OLD APP below
  testdata/files/expected/<file>.json                what is in each file, clean and
        damaged: its words, and every chord with its range as the file has it
  testdata/files/expected/repairs.json               for each damaged file, what is
        wrong with it and what its chords are once put right
  testdata/vectors/line-break-shift.json             the rule for one of the repairs,
        tried on made-up lines
  testdata/files/shaped-as-real-ones/                the song ProPresenter wrote
        (made-by-propresenter), with most of its slides made again in the shapes that
        real songs have for chords with no words: see SHAPES below

It is also the REFERENCE for the three checks of spec/08-repair.md: the functions below
are those rules, and both apps' own versions are held to what this writes.

OLD APP. What that editor did is read from its code (windows/old/Pro7ChordEditor/
MainWindow.xaml.cs as it was at v0.9.5) and, for the first of these, seen in a file it
saved:

  A. Ranges. It wrote each chord's range from the chord to the NEXT CHORD, wherever that
     was, and the last chord's to the end of the words. ProPresenter means a range to
     stop at the end of the chord's own line; one that runs on over a line break has
     the chord shown again at the start of the next line.
  B. Places. It counted the characters of the words with the line breaks left out, both
     when it read a file and when it saved one. ProPresenter counts a line break as one.
     So a chord it only read and saved again kept its number (and its place), while a
     chord put on by hand in that editor, on the second line or below, was saved one
     short for every line break above it. DEDUCED from the code; not yet seen in a file.
  C. Keys. A key with a flat or a sharp in its name could not be shown in its list, and
     a save with nothing chosen wrote the first key of the list, A flat major.
  D. Names. It saved beside the original as "<the name inside the file> (Chords).pro",
     leaving the name and the id inside as they were.

SHAPES. What the app does to chords that have no words under them (spec/01-file-format.
md, "Chords with no words") is the one thing it does to the RTF of a song, so it has to
be tried on RTF as real songs have it. Those are other people's songs and cannot be
here. What was read from them is only how such a slide is BUILT (mac/NOTES.md, "At
M7"), and that is made here, on the one file of ProPresenter's own that the test data
has: its slides, taken in the order they are shown, become

   1  a text box with no text, as ProPresenter leaves one: an RTF with an empty font
      table, and no settings that go by a stretch of text
   2, 3, 5  the song's own words, as they were
   4  four chords with no words as Multitracks writes them: each stand-in a zero-width
      space in the song's font, each em quad between two in another font
   6  three chords on plain spaces, with an em quad between each two
   7  one chord on one plain space
   8  a line of the song's words with two chords on it, over two chords on plain
      spaces
   9  three chords on plain spaces, each em quad in another font, over the slide's
      two lines of words, which have a chord each
  10  two lines of chords alone as Multitracks writes them, four chords and two
  11  a text box that holds one plain space, in a font: blank, as some are
  12  nothing at all: every element taken off the slide

The words are Toplady's, as in the file it is made from, and the chords are made up.
Made again it is the same file to the byte: nothing in it is random.

Needs `protoc` and the submodule in proto/.
"""
import json
import os
import re
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import keyfit
import look
import proread
import standins

ROOT = os.path.dirname(HERE)
CLEAN = os.path.join(ROOT, "testdata", "files", "made-by-simple-presenter")
OLD = os.path.join(ROOT, "testdata", "files", "as-the-old-windows-app-left-them")
# What ProPresenter itself wrote. It is only described: nothing is made of it, and it
# is not one of the files the checks are tried on (repairs.json).
BY_PROPRESENTER = os.path.join(ROOT, "testdata", "files", "made-by-propresenter")
# What is made here of that one, in the shapes real songs have: see SHAPES.
SHAPED = os.path.join(ROOT, "testdata", "files", "shaped-as-real-ones")
EXPECTED = os.path.join(ROOT, "testdata", "files", "expected")
VECTORS = os.path.join(ROOT, "testdata", "vectors")


# ---- What the old editor did (to make the damaged files)

def without_breaks(words):
    return len(words) - words.count("\n")


def old_ranges(words, starts_and_names):
    """A: ranges to the next chord, the last to the end; counted as that editor counted."""
    length = without_breaks(words)
    ordered = sorted(starts_and_names, key=lambda chord: chord[0])
    placed = [(min(start, length), name) for start, name in ordered]
    return [{"start": start, "end": placed[i + 1][0] if i + 1 < len(placed) else length, "name": name} for i, (start, name) in enumerate(placed)]


def as_placed_by_hand(words, start):
    """B: where that editor saved a chord that was put, in it, over the character at `start`."""
    return start - words.count("\n", 0, start)


# ---- The checks (the reference for spec/08-repair.md)

# White space as the rules have it (spec/02-chord-rules.md, "To the letter"), by number,
# since none of it can be seen: what a line of chords alone may hold beside U+200B.
_WHITE = set(chr(code) for code in list(range(0x09, 0x0E)) + [0x20, 0x85, 0xA0, 0x1680] + list(range(0x2000, 0x200B))
             + [0x2028, 0x2029, 0x202F, 0x205F, 0x3000])


def is_placeholders(line):
    """A line of chords alone: not empty, and nothing in it but U+200B and white space."""
    return line != "" and all(character == chr(0x200B) or character in _WHITE for character in line)


def right_ranges(words, starts_and_names):
    """What a text box's chords are written as. On a line of chords alone, the chord's
    own stand-in and no more: one long. On any other line, from the chord to the next
    chord or the end of its line, whichever comes first, and never empty.
    (chords::ranges of Simple Presenter; testdata/vectors/ranges.json has it from the
    C++, and main() below holds this to that file.)"""
    ordered = sorted(starts_and_names, key=lambda chord: chord[0])
    made = []
    for i, (start, name) in enumerate(ordered):
        line_start = words.rfind("\n", 0, start) + 1
        end = words.find("\n", start)
        if end < 0:
            end = len(words)
        if is_placeholders(words[line_start:end]):
            end = start + 1
        elif i + 1 < len(ordered):
            end = min(end, ordered[i + 1][0])
        made.append({"start": start, "end": max(end, start + 1), "name": name})
    return made


def ranges_over_line_breaks(words, chords):
    """Check 1. How many of a text box's chords have a range that runs past the end of
    the chord's own line. Any at all is the old editor's mark, and is put right by
    writing every range of the box again as right_ranges() has it: the chords stay
    where they are."""
    breaks = [i for i, c in enumerate(words) if c == "\n"]
    return sum(1 for chord in chords if any(chord["start"] < b < chord["end"] for b in breaks))


def unshifted(words, start):
    """The place a chord was meant for, if it was saved by the old editor at `start`:
    `start` counted along the words with their line breaks left out, turned back into a
    place in the words as they are."""
    left = start
    at = 0
    lines = words.split("\n")
    for n, line in enumerate(lines):
        if left < len(line) or n == len(lines) - 1:
            return at + min(left, max(0, len(line) - 1))
        left -= len(line)
        at += len(line) + 1
    return at


def starts_a_word(words, at):
    return 0 <= at < len(words) and not words[at].isspace() and (at == 0 or words[at - 1].isspace())


def line_break_shift(boxes):
    """Check 2. Whether a song's chords look as if they were put on by hand in the old
    editor: sitting a little to the left of the starts of words on every line but the
    first, by one character for each line break above.

    Only chords below the first line of their text box can show it. For those, count
    how many stand at the start of a word as they are, and how many would if they were
    moved to where unshifted() puts them. It is called likely when moving them puts at
    least four more of them, and at least a quarter more of all of them, on the starts
    of words. It is never certain, so it is only ever offered, with the song shown both
    ways."""
    below = as_is = moved = 0
    for box in boxes:
        words = box["words"]
        first_break = words.find("\n")
        if first_break < 0:
            continue
        for chord in box["chords"]:
            there = unshifted(words, chord["start"])
            if there <= first_break:
                continue
            below += 1
            as_is += starts_a_word(words, chord["start"])
            moved += starts_a_word(words, there)
    likely = below > 0 and moved - as_is >= 4 and moved - as_is >= below / 4
    return {"chordsBelowTheFirstLine": below, "atTheStartOfAWord": as_is, "ifMovedBack": moved, "likely": likely}


def key_check(about, own, offered):
    """Check 3. Whether the key a song states is to be doubted (tools/keyfit.py)."""
    names = [chord["name"] for box in about["textBoxes"] for chord in box["chords"]]
    answer = keyfit.likely(names, about["originalKey"], own, offered)
    return {"stated": about["originalKey"], "doubtful": answer["doubtful"], "suggested": answer["suggested"], "likeliest": answer["likeliest"][:4]}


# ---- Making the files

def damaged(source, target, ranges=False, by_hand=False, key_reset=False):
    decoded = proread.decode(source)
    # (One box at a time, from the last to the first, so that the places of the ones
    # still to do in the decoded text are not moved by the ones done.)
    for box in reversed(proread.text_boxes(decoded)):
        if not box["chords"]:
            continue
        words = box["words"]
        starts = [(as_placed_by_hand(words, chord["start"]) if by_hand else chord["start"], chord["name"]) for chord in box["chords"]]
        new = old_ranges(words, starts) if (ranges or by_hand) else [{"start": s, "end": c["end"], "name": n} for (s, n), c in zip(starts, box["chords"])]
        # The file's chords are in order of place in every file to hand; these are too.
        decoded = proread.with_chords(decoded, box, new)
    if key_reset:
        decoded, count = re.subn(r"^music \{\n.*?^\}\n", "music {\n  original {\n  }\n  user {\n  }\n}\n", decoded, flags=re.S | re.M)
        assert count == 1, "the song has no key to reset"
    proread.encode(decoded, target)


# ---- A song in the shapes real ones have (SHAPES, at the top)

# As Apple's text system writes a text box with no text in it.
EMPTY_DOCUMENT = (b"{\\rtf1\\ansi\\ansicpg1252\\cocoartf2761\n\\cocoatextscaling0\\cocoaplatform0{\\fonttbl}\n"
                  b"{\\colortbl;\\red255\\green255\\blue255;}\n{\\*\\expandedcolortbl;;}\n}")
_CUSTOM = re.compile(r"^( *)custom_attributes \{\n.*?^\1\}\n", re.S | re.M)


def quoted(data):
    """Bytes as protoc's text writes a string of them."""
    out = ""
    for byte in data:
        if byte == 0x0A:
            out += "\\n"
        elif byte in (0x22, 0x5C):
            out += "\\" + chr(byte)
        elif 0x20 <= byte < 0x7F:
            out += chr(byte)
        else:
            out += "\\%03o" % byte
    return out


def block_end(text, start):
    """Where the block that opens on the line at `start` closes: one past its last line."""
    indent = re.match(r" *", text[start:]).group(0)
    return re.compile(r"^" + indent + r"\}\n", re.M).search(text, start).end()


def remade(cue, rtf=None, chords=None, whole=None, bare=False):
    """One cue of protoc's text with its slide's text box changed: its RTF, if one is
    given; its chords (start, name), ranged as the rules range them; every setting it
    has that goes by a stretch of the text made to run over the whole of the new text
    (`whole`, true) or taken off (false). Or, `bare`, with every element taken off the
    slide."""
    if bare:
        while True:
            found = re.search(r"^ *elements \{\n", cue, re.M)
            if not found:
                return cue
            cue = cue[:found.start()] + cue[block_end(cue, found.start()):]
    if rtf is not None:
        cue, count = proread._RTF.subn(lambda _: 'rtf_data: "%s"' % quoted(rtf), cue)
        assert count == 1
    words = proread.plain_text(proread.unescape(proread._RTF.search(cue).group(1)))
    if whole is False:
        cue = _CUSTOM.sub("", cue)
    elif whole:
        cue, count = re.subn(r"(custom_attributes \{\n *range \{\n)(?: *start: \d+\n)?(?: *end: \d+\n)?", lambda m: m.group(1) + m.group(0).split("\n")[1].replace("range {", "  end: %d" % len(words)) + "\n", cue)
        assert count >= 1
    if chords:
        last = None
        for last in _CUSTOM.finditer(cue):
            pass
        indent = last.group(1) if last else re.search(r"^( *)stroke_color \{", cue, re.M).group(1)
        at = last.end() if last else block_end(cue, re.search(r"^ *stroke_color \{", cue, re.M).start())
        entries = ""
        for chord in right_ranges(words, chords):
            entries += indent + "custom_attributes {\n" + indent + "  range {\n"
            if chord["start"]:
                entries += indent + "    start: %d\n" % chord["start"]
            entries += indent + "    end: %d\n" % chord["end"] + indent + "  }\n"
            entries += indent + "  chord: \"%s\"\n" % chord["name"] + indent + "}\n"
        cue = cue[:at] + entries + cue[at:]
    return cue


def shaped_as_real_ones(source, target):
    decoded = proread.decode(source)
    # The cues, by their ids, and the order they are shown in: that of the groups.
    starts = [m.start() for m in re.finditer(r"^cues \{\n", decoded, re.M)]
    cues = {}
    for start in starts:
        end = block_end(decoded, start)
        cues[re.search(r'uuid \{\n *string: "([^"]+)"', decoded[start:end]).group(1)] = (start, end)
    shown = [m.group(1) for group in re.finditer(r"^cue_groups \{\n.*?^\}\n", decoded, re.S | re.M)
             for m in re.finditer(r'cue_identifiers \{\n *string: "([^"]+)"', group.group(0))]
    assert len(shown) == 12 and sorted(shown) == sorted(cues), "the song is not the one this was written for"

    Z, Q = standins.Z, standins.Q

    def rtf_of(n):
        return proread.unescape(proread._RTF.search(decoded[slice(*cues[shown[n]])]).group(1))

    def opening(n, fonts=1):
        """A slide's own RTF up to where its words begin; with a second font, if asked."""
        rtf = rtf_of(n)
        opening = rtf[:proread.written(rtf)[0][1]]
        if fonts == 2:
            assert opening.count(b"CenturyGothic;}") == 1
            opening = opening.replace(b"CenturyGothic;}", b"CenturyGothic;\\f1\\fnil\\fcharset0 LucidaGrande;}")
        return opening

    def lines_of(n):
        """A slide's own words, as their bytes: each line, as the RTF has it."""
        rtf = rtf_of(n)
        return rtf[proread.written(rtf)[0][1]:-1].split(b"\\\n")

    def multitracks(count):
        out = Z
        for _ in range(count - 1):
            out += b"\n\\f1 \\cf2 " + Q + b"\n\\f0 \\cf2 " + Z
        return out

    def spaces_in_two_fonts(count):
        out = b" "
        for _ in range(count - 1):
            out += b"\n\\f1 \\cf2 " + Q + b"\n\\f0 \\cf2  "
        return out

    def at_words(line, *which):
        """The places of some of a line's words, counted along it."""
        places = [0] + [n + 1 for n, c in enumerate(line) if c == " "]
        return [places[w] for w in which]

    changes = {
        0: dict(rtf=EMPTY_DOCUMENT, whole=False),
        3: dict(rtf=opening(3, fonts=2) + multitracks(4) + b"}", chords=[(0, "E"), (2, "B"), (4, "C#m"), (6, "A")], whole=True),
        5: dict(rtf=opening(5) + standins.on_spaces(3) + b"}", chords=[(0, "E"), (2, "A"), (4, "B7")], whole=True),
        6: dict(rtf=opening(6) + b" }", chords=[(0, "E")], whole=True),
        9: dict(rtf=opening(9, fonts=2) + multitracks(4) + b"\\\n" + multitracks(2) + b"}",
                chords=[(0, "E"), (2, "B"), (4, "C#m"), (6, "A"), (8, "E"), (10, "B")], whole=True),
        10: dict(rtf=opening(10) + b" }", whole=True),
        11: dict(bare=True),
    }
    first = lines_of(7)[0]
    words = first.decode("ascii")
    changes[7] = dict(rtf=opening(7) + first + b"\\\n" + standins.on_spaces(2) + b"}",
                      chords=[(p, name) for p, name in zip(at_words(words, 0, 2), ["E", "A"])] + [(len(words) + 1, "B"), (len(words) + 3, "E")], whole=True)
    both = lines_of(8)
    one, two = both[0].decode("ascii"), both[1].decode("ascii")
    changes[8] = dict(rtf=opening(8, fonts=2) + spaces_in_two_fonts(3) + b"\\\n" + both[0] + b"\\\n" + both[1] + b"}",
                      chords=[(0, "A"), (2, "B"), (4, "E"), (6 + at_words(one, 1)[0], "C#m"), (6 + len(one) + 1 + at_words(two, 0)[0], "A")], whole=True)

    out = decoded
    # (From the last cue in the file to the first, so that the places of those still to
    # do are not moved by those done.)
    for n in sorted(changes, key=lambda n: -cues[shown[n]][0]):
        start, end = cues[shown[n]]
        out = out[:start] + remade(out[start:end], **changes[n]) + out[end:]
    out, count = re.subn(r'^name: "Rock Of Ages \(Toplady\)"$', 'name: "Chords With No Words"', out, flags=re.M)
    assert count == 1
    out, count = re.subn(r'\A(application_info \{\n.*?^\}\nuuid \{\n  string: )"[^"]+"', r'\1"C0DA0000-0000-4000-8000-00000000C0DA"', out, flags=re.S | re.M)
    assert count == 1
    os.makedirs(os.path.dirname(target), exist_ok=True)
    proread.encode(out, target)

    # What was made is what was meant: read it back.
    boxes = proread.text_boxes(proread.decode(target))
    assert len(boxes) == 11, len(boxes)
    alone = [(n, [len(line) for line in box["words"].split("\n")], len(box["chords"])) for n, box in enumerate(boxes)]
    return alone


def main():
    wrong = proread.check_plain_text(VECTORS)
    if wrong:
        sys.exit("the reader of RTF here disagrees with the vectors made from Simple Presenter's: %s" % wrong)
    # The rule for ranges here is held to the C++ too: tidy.json and ranges.json are of
    # the same cases, and the second is the first's chords with their ranges.
    with open(os.path.join(VECTORS, "tidy.json"), encoding="utf-8") as tidied, open(os.path.join(VECTORS, "ranges.json"), encoding="utf-8") as ranged:
        for tidy_case, range_case in zip(json.load(tidied)["cases"], json.load(ranged)["cases"]):
            # (Counted as the file counts: a character from beyond the first 65,536 is two.)
            raw = tidy_case["text"].encode("utf-16-le", "surrogatepass")
            counted = "".join(chr(int.from_bytes(raw[i:i + 2], "little")) for i in range(0, len(raw), 2))
            mine = right_ranges(counted, [(chord["at"], chord["name"]) for chord in tidy_case["out"]])
            if tidy_case["text"] != range_case["text"] or mine != range_case["out"]:
                sys.exit("right_ranges() here disagrees with ranges.json, made from Simple Presenter's: %r gives %r, not %r"
                         % (tidy_case["text"], mine, range_case["out"]))
    for folder in (OLD, EXPECTED):
        os.makedirs(folder, exist_ok=True)

    made = [
        ("Plain Song.pro", "Plain Song (Chords).pro", {"ranges": True},
         "A: read and saved again by the old editor, no chord touched. Every range runs to the next chord."),
        ("It Is Well.pro", "It Is Well (Chords).pro", {"ranges": True},
         "A: the same, for a song of four lines to a slide, one of them a line of chords with no words."),
        ("Amazing Grace.pro", "Amazing Grace (Chords).pro", {"by_hand": True},
         "A and B: every chord put on by hand in the old editor. Those below a first line are short by one for each line break above."),
        ("A Song - In A Minor Key.pro", "A Song - In A Minor Key (Chords).pro", {"ranges": True, "key_reset": True},
         "A and C: read and saved again, with nothing chosen in the list of keys, the song's own (F sharp minor) being one it could not show."),
    ]
    for source, target, how, _ in made:
        damaged(os.path.join(CLEAN, source), os.path.join(OLD, target), **how)
    shaped_as_real_ones(os.path.join(BY_PROPRESENTER, "Rock Of Ages (Toplady).pro"), os.path.join(SHAPED, "Chords With No Words.pro"))

    own, offered = keyfit.tables(VECTORS)
    described = {}
    for folder in (CLEAN, OLD, BY_PROPRESENTER, SHAPED):
        for name in sorted(os.listdir(folder)) if os.path.isdir(folder) else []:
            if not name.endswith(".pro"):
                continue
            about = look.describe(os.path.join(folder, name))
            about["folder"] = os.path.basename(folder)
            described[name] = about
            with open(os.path.join(EXPECTED, name[:-4] + ".json"), "w", encoding="utf-8") as out:
                json.dump(about, out, indent=2, ensure_ascii=False)
                out.write("\n")

    repairs = []
    for source, target, how, why in made:
        about = described[target]
        clean = described[source]
        over = sum(ranges_over_line_breaks(box["words"], box["chords"]) for box in about["textBoxes"])
        shift = line_break_shift(about["textBoxes"])
        # Put right: check 1 always; check 2 only where it applies (it is offered, and
        # here it is taken).
        boxes = []
        for box in about["textBoxes"]:
            starts = [((unshifted(box["words"], chord["start"]) if how.get("by_hand") else chord["start"]), chord["name"]) for chord in box["chords"]]
            boxes.append({"chords": right_ranges(box["words"], starts)})
        same = all([(c["start"], c["end"], c["name"]) for c in fixed["chords"]] == [(c["start"], c["end"], c["name"]) for c in was["chords"]]
                   for fixed, was in zip(boxes, clean["textBoxes"]))
        repairs.append({
            "file": target, "madeFrom": source, "how": why,
            "rangesOverLineBreaks": over,
            "lineBreakShift": shift,
            "key": key_check(about, own, offered),
            "putRight": {"movedBack": bool(how.get("by_hand")), "textBoxes": boxes, "isThenTheFileItWasMadeFrom": same},
        })
        assert same, target
    clean_checks = []
    for name, about in sorted(described.items()):
        if about["folder"] != os.path.basename(CLEAN):
            continue
        clean_checks.append({
            "file": name,
            "rangesOverLineBreaks": sum(ranges_over_line_breaks(box["words"], box["chords"]) for box in about["textBoxes"]),
            "lineBreakShift": line_break_shift(about["textBoxes"]),
            "key": key_check(about, own, offered),
        })
    with open(os.path.join(EXPECTED, "repairs.json"), "w", encoding="utf-8") as out:
        json.dump({
            "about": "What the three checks of spec/08-repair.md find in each test file, and for the damaged ones what their chords are once "
                     "put right (`start` and `end` as the file then has them, for each text box that has words, in the file's order). "
                     "`damaged` are the files in as-the-old-windows-app-left-them; `clean` are those in made-by-simple-presenter, in which "
                     "nothing should be found.",
            "madeBy": "tools/make-fixtures.py, which is the reference for these checks. Do not edit by hand: run it again.",
            "damaged": repairs, "clean": clean_checks,
        }, out, indent=2, ensure_ascii=False)
        out.write("\n")

    # The rule of check 2 on made-up lines
    lines = "One two three four\nFive six seven eight\nNine ten eleven twelve"
    cases = []
    for start in range(0, without_breaks(lines) + 2):
        cases.append({"words": lines, "savedAt": start, "meantFor": unshifted(lines, start)})
    for words in ["One line only", "", "a\n\nb c", "ab\ncd"]:
        for start in range(0, without_breaks(words) + 2):
            cases.append({"words": words, "savedAt": start, "meantFor": unshifted(words, start)})
    songs = []
    by_word = [0, 4, 8, 14, 19, 24, 28, 34, 40, 45, 49, 56]

    def song(title, starts):
        box = {"words": lines, "chords": [{"start": s, "end": s + 1, "name": "G"} for s in starts]}
        songs.append({"about": title, "textBoxes": [box], "out": line_break_shift([box])})

    song("every chord on the start of a word, as ProPresenter counts", by_word)
    song("every chord put on the start of a word in the old editor", [as_placed_by_hand(lines, s) for s in by_word])
    song("chords in the middle of words, as ProPresenter counts", [1, 5, 10, 21, 26, 30, 42, 47, 51])
    song("only the first line has chords", [0, 4, 8, 14])
    song("too few below the first line to say", [as_placed_by_hand(lines, s) for s in [0, 4, 19, 40]])
    song("one line", [])
    with open(os.path.join(VECTORS, "line-break-shift.json"), "w", encoding="utf-8") as out:
        json.dump({
            "about": "Check 2 of spec/08-repair.md. `unshifted`: where a chord was meant for if the old Windows editor saved it at `savedAt` "
                     "(a place counted along the words with their line breaks left out). `songs`: whether a song's chords look as if they "
                     "were put on by hand in that editor, by line_break_shift() of tools/make-fixtures.py.",
            "madeBy": "tools/make-fixtures.py, which is the reference for this check. Do not edit by hand: run it again.",
            "unshifted": cases, "songs": songs,
        }, out, indent=2, ensure_ascii=False)
        out.write("\n")

    for entry in repairs:
        print("%-42s over line breaks %2d   shift likely %-5s (%d of %d at a word's start, %d if moved back)   key %s%s" % (
            entry["file"], entry["rangesOverLineBreaks"], entry["lineBreakShift"]["likely"], entry["lineBreakShift"]["atTheStartOfAWord"],
            entry["lineBreakShift"]["chordsBelowTheFirstLine"], entry["lineBreakShift"]["ifMovedBack"], entry["key"]["stated"],
            " doubtful, more likely " + entry["key"]["suggested"] if entry["key"]["doubtful"] else ""))
    for entry in clean_checks:
        print("%-42s over line breaks %2d   shift likely %-5s (%d of %d, %d if moved back)   key %s%s" % (
            entry["file"], entry["rangesOverLineBreaks"], entry["lineBreakShift"]["likely"], entry["lineBreakShift"]["atTheStartOfAWord"],
            entry["lineBreakShift"]["chordsBelowTheFirstLine"], entry["lineBreakShift"]["ifMovedBack"], entry["key"]["stated"] or "(none)",
            " doubtful, more likely " + entry["key"]["suggested"] if entry["key"]["doubtful"] else ""))


if __name__ == "__main__":
    main()
