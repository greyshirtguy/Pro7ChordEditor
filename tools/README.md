# The tools

What made the test data, and one tool for looking inside a song. They are for the
maintainer and for an agent that needs to see what is in a file; neither app uses
them.

| | |
| --- | --- |
| `look.py` | Says what chords a `.pro` file has and where. `tools/look.py --chords "Some Song.pro"`. The first thing to reach for when a song behaves oddly. It only reads |
| `proread.py` | What `look.py` and `make-fixtures.py` read a file with: through `protoc`, as text. Not how either app reads one |
| `make-vectors/` | Writes most of `testdata/vectors` from Simple Presenter's source: `main.cpp` (its C++ rules) and `sheet.js` (its chord sheet, run under Node) |
| `keyfit.py` | The rule for which key a song is likely in, and its vectors |
| `make-fixtures.py` | Makes the damaged test files from the clean ones, describes every test file, and is the reference for the mending checks |
| `simple-presenter-scripts/` | Two scripts that Simple Presenter itself runs, to make the clean test files and to read every test file back |

They need Python 3, `protoc`, and the submodule (`git submodule update --init`).
`make-vectors` needs Qt 6, CMake, Node and a checkout of Simple Presenter.
`testdata/README.md` has the commands in order.

## The clean test files, made with Simple Presenter

`simple-presenter-scripts/import.qml` brings the songs of `testdata/songs` into a
library, and `dump.qml` opens every presentation of a library in its editor and
prints what the chord editors are handed. Both run inside a build of Simple Presenter
that has its test hook (`-DSIMPLEPRESENTER_TEST_HOOK=ON`), without a screen, on an
empty workspace, with settings and documents folders of their own so that nothing of
anyone's is touched:

```
T=$(mktemp -d)
mkdir -p $T/ws/Libraries/Songs $T/config $T/data $T/cache $T/docs $T/frames
printf 'XDG_DOCUMENTS_DIR="%s"\n' "$T/docs" > $T/config/user-dirs.dirs
sed "s|@SONGS@|$PWD/testdata/songs|" tools/simple-presenter-scripts/import.qml > $T/import.qml

QT_QPA_PLATFORM=offscreen QT_QUICK_BACKEND=software \
XDG_CONFIG_HOME=$T/config XDG_DATA_HOME=$T/data XDG_CACHE_HOME=$T/cache \
    /path/to/Simple-Presenter/build-tests/SimplePresenter --workspace $T/ws --script $T/import.qml --frames $T/frames

cp $T/ws/Libraries/Songs/*.pro testdata/files/made-by-simple-presenter/
```

and the same with `dump.qml`, on a library holding every file of `testdata/files`,
taking the lines it prints that start `SONG ` as the entries of
`testdata/files/expected/as-read-by-simple-presenter.json`.
