#!/usr/bin/env python3
"""Which key a song's chords are most likely in: the reference for that one rule, and
the maker of testdata/vectors/key-fit.json.

    tools/keyfit.py testdata/vectors        writes key-fit.json there

Why there is such a rule. The old Windows editor could save a song with its key set to
A flat major when the key was really something else (spec/08-repair.md), and a song
typed by hand often names no key at all. Neither app can know the right key, but both
can say "these chords look like G" and let the person decide. This is how that is said.
It is new: Simple Presenter has nothing like it, so unlike the other vectors these do
not record what that app does. They record what THIS does, so that the two new apps say
the same thing.

The rule.
  1. Each chord of the song is a note and a kind: minor if what follows its root starts
     with "m" (but not "maj"), diminished if it starts with "dim" or "°", otherwise
     major. Sevenths, suspensions and the rest are not looked at, nor is a bass note.
     What does not start with a note is left out.
  2. A key has seven chords of its own (testdata/vectors/diatonic.json). A chord fits a
     key if its note and kind are those of one of the seven. A major key and its
     relative minor have the same seven, so they always fit equally.
  3. A key's score is the share of the song's chords that fit it, each counted as often
     as it is used. Twenty-six keys are scored: those a picker offers
     (testdata/vectors/keys.json, majorKeys and minorKeys).
  4. The likeliest keys are those with the best score. Among them the one whose own
     first chord (G in G, Em in E minor) is used most comes first; if that is a tie, the
     major one; and two names for the same notes (F# and Gb) are told apart by which
     way the song's own chords are written, sharps or flats.
  5. A stated key is called doubtful only when the song has at least four chords, the
     best score is at least 0.75, and the stated key's score is more than 0.25 below it.
     Then the likeliest is suggested. A song in an unusual key, or with borrowed chords,
     is thus left alone.
"""
import json
import os
import re
import sys

PITCH = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def note_and_kind(chord):
    found = re.match(r"^([A-Ga-g])(#+|b+|♯+|♭+)?(.*)$", chord.strip())
    if not found:
        return None
    letter, marks, rest = found.group(1).upper(), found.group(2) or "", found.group(3)
    sharps = marks.count("#") + marks.count("♯")
    flats = marks.count("b") + marks.count("♭")
    pitch = (PITCH[letter] + sharps - flats) % 12
    slash = rest.rfind("/")
    if slash >= 0 and re.fullmatch(r"[A-Ga-g](#+|b+)?", rest[slash + 1:]):
        rest = rest[:slash]
    if rest.startswith("dim") or rest.startswith("°"):
        kind = "dim"
    elif rest.startswith("m") and not rest.startswith("maj"):
        kind = "m"
    else:
        kind = ""
    return pitch, kind


def likely(chords, stated, own_chords, offered):
    """{scores, likeliest (best first), doubtful, suggested}"""
    read = [nk for nk in (note_and_kind(c) for c in chords) if nk]
    scores = {}
    for key in offered:
        own = {note_and_kind(c) for c in own_chords[key]}
        scores[key] = round(sum(1 for nk in read if nk in own) / len(read), 4) if read else 0.0
    best = max(scores.values()) if scores else 0.0
    flats_written = sum(1 for c in chords if re.match(r"^[A-Ga-g]b", c.strip()))
    sharps_written = sum(1 for c in chords if re.match(r"^[A-Ga-g]#", c.strip()))

    def order(key):
        tonic = note_and_kind(own_chords[key][0])
        uses = sum(1 for nk in read if nk == tonic)
        # (A key spelt against the way the song's chords are spelt goes after its twin.)
        against = ("b" in key[1:2] and sharps_written > flats_written) or ("#" in key[1:2] and flats_written > sharps_written)
        return (-uses, key.endswith("m"), against, offered.index(key))

    likeliest = sorted((k for k in offered if scores[k] == best and best > 0), key=order)
    stated_score = scores.get(stated, 0.0) if stated else 0.0
    doubtful = bool(stated) and len(read) >= 4 and best >= 0.75 and stated_score < best - 0.25
    return {"scores": scores, "likeliest": likeliest, "doubtful": doubtful, "suggested": likeliest[0] if likeliest and (doubtful or not stated) and len(read) >= 4 and best >= 0.75 else ""}


def tables(vectors):
    with open(os.path.join(vectors, "diatonic.json"), encoding="utf-8") as file:
        own = {case["key"]: case["out"] for case in json.load(file)["cases"] if case["out"]}
    with open(os.path.join(vectors, "keys.json"), encoding="utf-8") as file:
        keys = json.load(file)
    return own, keys["majorKeys"] + keys["minorKeys"]


SONGS = [
    ("in G, and says so", "G", "G D Em C G D/F# G Em7 C D G"),
    ("in G, saved as A flat by the old editor", "Ab", "G D Em C G D/F# G Em7 C D G"),
    ("in G, with no key given", "", "G D Em C G D/F# G Em7 C D G"),
    ("in E minor: the same chords, resting on Em", "Em", "Em C G D Em C D Em Em"),
    ("in E minor, saved as A flat", "Ab", "Em C G D Em C D Em Em"),
    ("in B flat", "Bb", "Bb Eb F Gm Bb/D Eb Bb F Bb"),
    ("in F sharp, written with sharps", "", "F# B C# D#m F# B F#"),
    ("the same notes written with flats", "", "Gb Cb Db Ebm Gb Cb Gb"),
    ("in A flat, and truly so", "Ab", "Ab Db Eb Fm Ab Eb Ab"),
    ("one borrowed chord: still its own key", "G", "G C D F G C D G"),
    ("stated a fourth away: C for a song in G, which shares most chords", "C", "G D Em C G D G"),
    ("stated wrongly by a tone", "A", "G D Em C G D Em C G"),
    ("too few chords to say", "Ab", "G D"),
    ("no chords", "Ab", ""),
    ("things that are not chords among them", "Ab", "G N.C. D x Em C /// G"),
    ("all sevenths and suspensions", "", "Gmaj7 Dsus4 Em7 Cadd9 G/B D7 Gsus2"),
    ("jazz that fits no key well", "C", "Cmaj7 Eb7 Abmaj7 B7 Emaj7 G7"),
    ("a diminished chord of the key", "C", "C Dm G Bdim C F G C"),
]


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    vectors = sys.argv[1]
    own, offered = tables(vectors)
    cases = []
    for title, stated, chords in SONGS:
        chord_list = chords.split()
        answer = likely(chord_list, stated, own, offered)
        cases.append({"about": title, "stated": stated, "chords": chord_list, "likeliest": answer["likeliest"], "doubtful": answer["doubtful"],
                      "suggested": answer["suggested"], "scores": answer["scores"]})
    whole = {
        "about": "Which key a song's chords are most likely in, and whether the key it states is to be doubted: the rule at the top of "
                 "tools/keyfit.py and in spec/08-repair.md. `chords` is every chord of the song, one entry for each use. `likeliest` are the "
                 "keys with the best score, the likeliest first; `doubtful` whether `stated` is far enough from them to say so; `suggested` "
                 "the key to offer (when the stated one is doubtful, or none is stated), or nothing. `scores` is every offered key's share "
                 "of the chords that fit it.",
        "madeBy": "tools/keyfit.py, which is itself the reference for this rule. Do not edit by hand: run it again.",
        "cases": cases,
    }
    with open(os.path.join(vectors, "key-fit.json"), "w", encoding="utf-8") as file:
        json.dump(whole, file, indent=4, ensure_ascii=False)
        file.write("\n")
    print("key-fit.json")
    for case in cases:
        print("  %-62s stated %-3s likeliest %-14s doubtful %-5s suggest %s" % (case["about"][:62], case["stated"] or "-", ",".join(case["likeliest"][:3]), case["doubtful"], case["suggested"] or "-"))


if __name__ == "__main__":
    main()
