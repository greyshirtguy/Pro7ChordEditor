#!/usr/bin/env python3
"""Says what chords a ProPresenter presentation has, and where: for looking at a file
that is behaving oddly, and for seeing what a program has written.

    tools/look.py <file.pro> [more files]          a summary of each
    tools/look.py --chords <file.pro>              every chord, with the words it stands over
    tools/look.py --json <file.pro>                the same as JSON (what testdata/files/expected holds)

It only reads. It needs `protoc` and the submodule in proto/. The words of songs are
printed by --chords and --json: mind where the output goes if the song is not yours to
pass on.
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proread

KEYS = ["Ab", "A", "A#", "Bb", "B", "B#", "Cb", "C", "C#", "Db", "D", "D#", "Eb", "E", "E#", "Fb", "F", "F#", "Gb", "G", "G#"]
_NUMBER = {"MUSIC_KEY_" + name.replace("b", "_FLAT").replace("#", "_SHARP"): number for number, name in enumerate(KEYS)}


def key_of(block, which):
    """"G", "F#m" ... for music.original or music.user; "" if the file has none."""
    found = re.search(r"^  " + which + r" \{(.*?)^  \}", block, re.S | re.M)
    if not found:
        return ""
    key = re.search(r"music_key: (\w+)", found.group(1))
    minor = "MUSIC_SCALE_MINOR" in found.group(1)
    return KEYS[_NUMBER[key.group(1)] if key else 0] + ("m" if minor else "")


def describe(path):
    decoded = proread.decode(path)
    name = re.search(r'^name: "((?:[^"\\]|\\.)*)"', decoded, re.M)
    uuid = re.search(r'^uuid \{\s*string: "([^"]+)"', decoded, re.M)
    music = re.search(r"^music \{\n(.*?)^\}", decoded, re.S | re.M)
    boxes = []
    for box in proread.text_boxes(decoded):
        words = box["words"]
        breaks = [i for i, c in enumerate(words) if c == "\n"]
        chords = []
        for chord in box["chords"]:
            line = sum(1 for b in breaks if b < chord["start"])
            chords.append({
                "start": chord["start"], "end": chord["end"], "name": chord["name"], "line": line,
                # Its range runs on past the end of its own line: ProPresenter then
                # shows the chord again at the start of the next.
                "overLineBreak": any(chord["start"] < b < chord["end"] for b in breaks),
                "pastTheEnd": chord["start"] > len(words) or chord["end"] > len(words),
            })
        boxes.append({"words": words, "length": len(words), "chords": chords})
    return {
        "file": os.path.basename(path),
        "nameInside": proread.unescape(name.group(1)).decode("utf-8") if name else "",
        "uuid": uuid.group(1) if uuid else "",
        "hasMusic": music is not None,
        "originalKey": key_of(music.group(1), "original") if music else "",
        "userKey": key_of(music.group(1), "user") if music else "",
        "multitracks": "multi_tracks_licensing" in decoded,
        "textBoxes": boxes,
    }


def main():
    paths = [a for a in sys.argv[1:] if not a.startswith("--")]
    if not paths:
        sys.exit(__doc__)
    for path in paths:
        about = describe(path)
        if "--json" in sys.argv:
            print(json.dumps(about, indent=2, ensure_ascii=False))
            continue
        chords = [c for box in about["textBoxes"] for c in box["chords"]]
        print("%s" % about["file"])
        print("  name inside %r, id %s%s" % (about["nameInside"], about["uuid"][:8], ", has Multitracks' block" if about["multitracks"] else ""))
        print("  key: %s" % ("written in %s, shown in %s" % (about["originalKey"], about["userKey"]) if about["hasMusic"] else "none given"))
        print("  %d text boxes, %d of them with chords; %d chords" % (len(about["textBoxes"]), sum(1 for b in about["textBoxes"] if b["chords"]), len(chords)))
        print("  ranges that run over a line break: %d; past the end of the words: %d"
              % (sum(c["overLineBreak"] for c in chords), sum(c["pastTheEnd"] for c in chords)))
        if "--chords" in sys.argv:
            for n, box in enumerate(about["textBoxes"]):
                for chord in box["chords"]:
                    print("    box %2d  line %d  %3d-%-3d  %-10s over %r" % (n, chord["line"], chord["start"], chord["end"], chord["name"],
                                                                             box["words"][chord["start"]:chord["start"] + 8]))


if __name__ == "__main__":
    main()
