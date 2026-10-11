"""Reading a ProPresenter presentation without any library for its format: through
`protoc`, which turns the file into text and text back into a file, given the
descriptions of the format in proto/autogen-proto (proto/ is a submodule:
`git submodule update --init`). That folder is the set taken from ProPresenter itself; see
spec/01-file-format.md for why it and no other.

This is for the tools beside it (look.py, make-fixtures.py), which make and describe the
test files. It is NOT how either app reads a file: they use the code their own language
generates from the same descriptions. It is slow and it is simple, which is what a tool
that makes test data wants to be.

What it knows of the format is in spec/01-file-format.md; the short of it:
  - a presentation has cues, a cue has actions, one action of a cue is its slide, a
    slide has elements, an element may have text;
  - the text is RTF, and beside it is a list of "custom attributes", some of which are
    chords: a range of characters (start, end) and the chord's name;
  - places are counted along the plain text of the RTF, a line break being one.
"""
import os
import re
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
PROTO = os.path.join(os.path.dirname(HERE), "proto", "autogen-proto")
MESSAGE = "rv.data.Presentation"


def _protoc(how, data):
    if not os.path.exists(os.path.join(PROTO, "presentation.proto")):
        raise SystemExit("proto/ is empty: run `git submodule update --init` in the repository")
    done = subprocess.run(["protoc", "-I", PROTO, how, MESSAGE, os.path.join(PROTO, "presentation.proto")],
                          input=data, capture_output=True)
    if done.returncode != 0:
        raise SystemExit("protoc: " + done.stderr.decode(errors="replace")[:400])
    return done.stdout


def decode(path):
    """The file as protoc's text, its lines ended by a line feed alone.

    On Windows protoc ends each line of its text with a carriage return and a line
    feed, and everything below finds its way about that text by line feeds: left as
    they came, a file's name was still found and not one of its text boxes. (Found on
    Windows, 2026-10-12, where look.py said of a song with chords that it had none.)
    A carriage return inside a string is written by protoc as a backslash and an "r",
    so none that is taken out here was ever part of a file."""
    with open(path, "rb") as file:
        return _protoc("--decode", file.read()).decode("utf-8").replace("\r\n", "\n")


def encode(text, path):
    """protoc's text as a file."""
    with open(path, "wb") as file:
        file.write(_protoc("--encode", text.encode("utf-8")))


def unescape(quoted):
    """What is between the quotes of a string in protoc's text, as bytes."""
    out = bytearray()
    i = 0
    while i < len(quoted):
        c = quoted[i]
        if c != "\\":
            out += c.encode("utf-8")
            i += 1
            continue
        n = quoted[i + 1]
        if n in "01234567":
            j = i + 1
            while j < len(quoted) and j < i + 4 and quoted[j] in "01234567":
                j += 1
            out.append(int(quoted[i + 1:j], 8))
            i = j
        else:
            out += {"n": b"\n", "r": b"\r", "t": b"\t"}.get(n, n.encode())
            i += 2
    return bytes(out)


# ---- The words of an RTF

_SKIPPED = ("fonttbl", "colortbl", "info", "stylesheet", "pict", "header", "footer", "listtable", "listoverridetable")
_CP1252 = {i: bytes([i]).decode("cp1252", "replace") for i in range(256)}


def plain_text(rtf):
    """The plain text of the RTF ProPresenter keeps for a text box: what a chord's place
    is a place in. A paragraph break and a line break are one "\\n" each. The string
    given back has one character for each UTF-16 unit, which is how the file counts: a
    character beyond the first sixty-five thousand comes back as its two halves, so
    that len() and an index mean here what they mean in the file.

    Written to the same rules as Simple Presenter's reader (reference/simple-presenter/
    src/rtf.cpp), and held to testdata/vectors/rtf-text.json by check_plain_text()."""
    return "".join(unit[0] for unit in written(rtf))


def written(rtf):
    """The plain text of an RTF, one entry for each of its units, each with where in
    the RTF it is written:

        (character, start, end, open_before, open_after, depth)

    `start` and `end` are the unit's own bytes, from the first to one past the last: a
    control word's are its backslash, its letters and number, and the space that ends
    it if it has one. `open_before` says a control word with no space after it ends
    exactly where the unit begins, and `open_after` that the unit is itself such a
    word: a plain space put in at either place would be taken for the word's end and
    would not be text. `depth` is how many groups are open there.

    It is what standins.py goes by to change one line of an RTF and leave the rest."""
    units = []
    i = 0
    skipping = []      # for each open group: whether what is in it is not text
    starred = False
    pending = 0        # characters still to pass over after a \u
    skip_count = [1]   # \uc, for each open group
    open_end = -1      # where the last control word that had no space after it ended

    def note(character, start, end, open_before):
        units.append((character, start, end, open_before, end == open_end, len(skipping)))

    def put(character, start, end, open_before=None):
        nonlocal pending
        if pending > 0:
            pending -= 1
            return
        if not (skipping and skipping[-1]):
            note(character, start, end, start == open_end if open_before is None else open_before)

    def new_paragraph(start, end, open_before):
        if not (skipping and skipping[-1]):
            note("\n", start, end, open_before)

    while i < len(rtf):
        c = rtf[i]
        i += 1
        if c == 0x7B:  # {
            skipping.append(skipping[-1] if skipping else False)
            skip_count.append(skip_count[-1])
            starred = False
            pending = 0
        elif c == 0x7D:  # }
            if skipping:
                skipping.pop()
            if len(skip_count) > 1:
                skip_count.pop()
            pending = 0
        elif c == 0x5C:  # backslash
            if i >= len(rtf):
                break
            at = i - 1
            # (Asked now: this may itself be a control word with no space after it,
            # and would then be the last such.)
            was_open = at == open_end
            n = rtf[i]
            if not (65 <= n <= 90 or 97 <= n <= 122):
                i += 1
                if n == 0x27:  # \'hh
                    i += 2
                    put(_CP1252[int(rtf[i - 2:i], 16)], at, i, was_open)
                elif n == 0x2A:  # \*
                    starred = True
                elif n in (0x0A, 0x0D):
                    new_paragraph(at, i, was_open)
                elif n == 0x7E:
                    put(chr(0x00A0), at, i, was_open)
                elif n == 0x5F:
                    put(chr(0x2011), at, i, was_open)
                elif n == 0x2D:
                    pass
                else:
                    put(chr(n), at, i, was_open)
                continue
            start = i
            while i < len(rtf) and (65 <= rtf[i] <= 90 or 97 <= rtf[i] <= 122):
                i += 1
            word = rtf[start:i].decode("ascii")
            start = i
            if i < len(rtf) and rtf[i] == 0x2D:
                i += 1
            while i < len(rtf) and 48 <= rtf[i] <= 57:
                i += 1
            number = int(rtf[start:i]) if i > start and rtf[start:i] != b"-" else None
            if i < len(rtf) and rtf[i] == 0x20:
                i += 1
            else:
                open_end = i
            if starred:
                # The first word after "{\*" names something that is not text.
                starred = False
                if skipping:
                    skipping[-1] = True
                continue
            if skipping and skipping[-1]:
                continue
            if word in _SKIPPED:
                if skipping:
                    skipping[-1] = True
            elif word == "par":
                new_paragraph(at, i, was_open)
            elif word == "line":
                put("\n", at, i, was_open)
            elif word == "tab":
                put("\t", at, i, was_open)
            elif word == "u" and number is not None:
                put(chr(number + 65536 if number < 0 else number), at, i, was_open)
                pending = skip_count[-1]
            elif word == "uc" and number is not None:
                skip_count[-1] = number
        elif c in (0x0A, 0x0D):
            pass
        elif c < 0x80:
            put(chr(c), i - 1, i)
        else:
            # Bytes above ASCII written straight in: UTF-8 if they are, else the code page.
            start = i - 1
            while i < len(rtf) and rtf[i] >= 0x80:
                i += 1
            run = rtf[start:i]
            try:
                decoded = run.decode("utf-8")
            except UnicodeDecodeError:
                for n, b in enumerate(run):
                    put(_CP1252[b], start + n, start + n + 1)
                continue
            at = start
            for character in decoded:
                after = at + len(character.encode("utf-8"))
                if ord(character) > 0xFFFF:
                    high, low = divmod(ord(character) - 0x10000, 0x400)
                    put(chr(0xD800 + high), at, after)
                    put(chr(0xDC00 + low), at, after)
                else:
                    put(character, at, after)
                at = after
    return units


# ---- The text boxes of a presentation, and their chords

_TEXT = re.compile(r"^( *)text \{$", re.M)
_CHORD = re.compile(r"custom_attributes \{\s*(?:range \{\s*(?:start: (\d+)\s*)?(?:end: (\d+)\s*)?\}\s*)?chord: \"((?:[^\"\\]|\\.)*)\"\s*\}")
_RTF = re.compile(r"rtf_data: \"((?:[^\"\\]|\\.)*)\"")


def text_boxes(decoded):
    """For each text box that has RTF, in the file's order: a dict of
       span (where its `text { ... }` is in the decoded text), words (its plain text),
       chords ([{start, end, name}] as the file has them, in the file's order)."""
    found = []
    for opening in _TEXT.finditer(decoded):
        indent = opening.group(1)
        closing = re.compile(r"^" + indent + r"\}$", re.M).search(decoded, opening.end())
        if not closing:
            continue
        block = decoded[opening.start():closing.end()]
        rtf = _RTF.search(block)
        if not rtf:
            continue
        chords = [{"start": int(m.group(1) or 0), "end": int(m.group(2) or 0), "name": unescape(m.group(3)).decode("utf-8")}
                  for m in _CHORD.finditer(block)]
        found.append({"span": (opening.start(), closing.end()), "words": plain_text(unescape(rtf.group(1))), "chords": chords})
    return found


def with_chords(decoded, box, chords):
    """The decoded text with one text box's chords given new ranges and names: `chords`
    is one for each chord the box has, in the file's order. Nothing else is touched."""
    start, end = box["span"]
    block = decoded[start:end]
    given = iter(chords)

    def rewritten(match):
        chord = next(given)
        indent = re.match(r" *", block[block.rfind("\n", 0, match.start()) + 1:match.start()]).group(0)
        inner = indent + "  "
        lines = ["custom_attributes {", inner + "range {"]
        if chord["start"]:
            lines.append(inner + "  start: %d" % chord["start"])
        if chord["end"]:
            lines.append(inner + "  end: %d" % chord["end"])
        lines += [inner + "}", inner + "chord: \"%s\"" % chord["name"].replace("\\", "\\\\").replace('"', '\\"'), indent + "}"]
        return "\n".join(lines)

    return decoded[:start] + _CHORD.sub(rewritten, block) + decoded[end:]


def check_plain_text(vectors):
    """Holds plain_text() to the vectors made from Simple Presenter's own reader."""
    import base64
    import json
    wrong = []
    with open(os.path.join(vectors, "rtf-text.json"), encoding="utf-8") as file:
        for case in json.load(file)["cases"]:
            if [ord(c) for c in plain_text(base64.b64decode(case["rtfBase64"]))] != case["units"]:
                wrong.append(case["name"])
    return wrong
