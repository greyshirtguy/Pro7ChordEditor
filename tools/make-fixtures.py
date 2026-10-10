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

It is also the REFERENCE for the three checks of spec/08-repair.md: the functions below
are those rules, and both apps' own versions are held to what this writes.

OLD APP. What that editor did is read from its code (windows/Pro7ChordEditor/
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

ROOT = os.path.dirname(HERE)
CLEAN = os.path.join(ROOT, "testdata", "files", "made-by-simple-presenter")
OLD = os.path.join(ROOT, "testdata", "files", "as-the-old-windows-app-left-them")
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

def right_ranges(words, starts_and_names):
    """What a text box's chords are written as: from the chord to the next chord or the
    end of its line, whichever comes first, and never empty. (chords::ranges of Simple
    Presenter; testdata/vectors/ranges.json has it from the C++.)"""
    ordered = sorted(starts_and_names, key=lambda chord: chord[0])
    made = []
    for i, (start, name) in enumerate(ordered):
        end = words.find("\n", start)
        if end < 0:
            end = len(words)
        if i + 1 < len(ordered):
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


def main():
    wrong = proread.check_plain_text(VECTORS)
    if wrong:
        sys.exit("the reader of RTF here disagrees with the vectors made from Simple Presenter's: %s" % wrong)
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

    own, offered = keyfit.tables(VECTORS)
    described = {}
    for folder in (CLEAN, OLD):
        for name in sorted(os.listdir(folder)):
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
