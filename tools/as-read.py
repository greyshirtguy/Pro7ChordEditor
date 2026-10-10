#!/usr/bin/env python3
"""Writes testdata/files/expected/as-read-by-simple-presenter.json from what
tools/simple-presenter-scripts/dump.qml printed while it ran inside Simple Presenter.

    python3 tools/as-read.py <the output of that run, as a file>

dump.qml opens every presentation of a library in Simple Presenter's editor and prints,
for each, one line that starts "SONG " and then has, as JSON, what the chord editors
are handed (PresentationEditor::song). This gathers those lines, in the order of the
files' names, and writes them as the one file both apps' tests read. It checks nothing
and changes nothing in what was printed: the answers are Simple Presenter's.
tools/README.md has the run itself.
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "testdata", "files", "expected", "as-read-by-simple-presenter.json")

ABOUT = ("Each test file as Simple Presenter's own editor reads it: what its chord editors are handed (PresentationEditor::song). "
         "One entry for each text box that has words of its own, in the order of the slides: `slide` counted from 0, the name of its "
         "group and whether it is the first slide of that group, the id of the text box, its words, and its chords, each `at` a place "
         "along those words (a line break being one). A blank slide, one with no words or chords on it anywhere, has one entry too, "
         "with no words and `blank` true: its first text box that is not linked to anything. Where a blank slide has no text box at "
         "all, `element` is empty: that entry is Simple Presenter's own, which gives such a slide a text box when a chord is put "
         "there, and an app that makes no text boxes hands nothing over for it. `key` is the key the chords are written in, as a "
         "musician writes it, or nothing. This is the reference for reading a file: a port that reads these files should find the "
         "same. Some strings hold characters that cannot be seen: U+200B and U+2001.")


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    files = []
    with open(sys.argv[1], encoding="utf-8") as printed:
        for line in printed:
            if line.startswith("SONG "):
                files.append(json.loads(line[5:]))
    if not files:
        sys.exit("no line of that file starts with SONG: was it the output of dump.qml?")
    files.sort(key=lambda entry: entry["file"])
    whole = {"about": ABOUT,
             "madeBy": "tools/simple-presenter-scripts/dump.qml, run in Simple Presenter, and tools/as-read.py (tools/README.md says how). "
                       "Do not edit by hand.",
             "files": files}
    with open(OUT, "w", encoding="utf-8") as out:
        json.dump(whole, out, indent=2, ensure_ascii=False)
        out.write("\n")
    print("%d files, %d text boxes, %d of them blank slides" % (len(files), sum(len(entry["textBoxes"]) for entry in files),
                                                               sum(1 for entry in files for box in entry["textBoxes"] if box.get("blank"))))


if __name__ == "__main__":
    main()
