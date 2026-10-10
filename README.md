# Pro7 Chord Editor

Puts chords on the songs of a ProPresenter 7 library, so that ProPresenter shows them
on a stage screen, as it does for songs from Multitracks.

ProPresenter shows the chords a song came with and has no way to put one on. This
does: it opens the library where it lies, shows a song with its chords over its
words, and lets them be put on, moved and changed.

> [!WARNING]
> **A personal hobby project, not a product.** Nobody supports it and nothing is
> promised. It changes the files of your ProPresenter library: keep a backup of the
> library, and quit ProPresenter before using it. It has nothing to do with Renewed
> Vision, the makers of ProPresenter.

## Where things stand

| | |
| --- | --- |
| **Windows** | The first editor, version 0.9.5, is in [`windows/`](windows/README.md) and on the [Releases](https://github.com/greyshirtguy/Pro7ChordEditor/releases) page. It works by typing chords in square brackets among the words. It has known faults and is to be replaced |
| **Mac** | Being built first. Nothing to download yet |
| **The new editor, for both** | Planned in full in [`spec/`](spec/00-overview.md) and not yet built |

## What the new one is to be

The chord editor that was built for
[Simple Presenter](https://github.com/greyshirtguy/Simple-Presenter), a presenter for
Linux, lifted out and made again natively for the Mac and for Windows:

![The chord sheet: a song laid out as one sheet, each slide's lines in a card, with chords in bubbles over the words and the key's own chords numbered 1 to 7 along the top](spec/pictures/chord-editor.png)

- **The whole song on one sheet**, each slide's lines in a card, with a spot that
  follows the pointer to show where the next chord will go.
- **Most chords are one key press**: the song's key is at the top with its seven
  chords numbered, and 1 to 7 put them on.
- **Typing is helped along**: a bubble that takes only what could be a chord and
  offers what it might be going to be.
- **Chords are things to pick up**: drag one to another word; copy a verse's chords
  onto the next verse in one go.
- **Or type the lot as ChordPro**, `[G]Amazing [C]grace`, as the first editor did.
- **Every change saved at once into the song's own file**, with a copy of the file
  kept first, out of sight, and a way back to any of them.
- **Songs the first editor left wrong are found and mended.**
- **A ChordPro file, from SongSelect say, made into a song.**

## This repository

| Folder | What it is |
| --- | --- |
| [`spec/`](spec/00-overview.md) | What the apps are to do, page by page |
| [`testdata/`](testdata/README.md) | What the rules must answer, and sample songs, for both apps' tests |
| [`reference/`](reference/README.md) | The source of the editor being copied, to read |
| [`tools/`](tools/README.md) | What made the test data, and a tool for looking inside a `.pro` file |
| `proto/` | The descriptions of ProPresenter's file formats ([ProPresenter7-Proto](https://github.com/greyshirtguy/ProPresenter7-Proto), a submodule) |
| [`windows/`](windows/README.md) | The Windows app |
| [`mac/`](mac/README.md) | The Mac app |

It is being built by describing it to an AI model. [`AGENTS.md`](AGENTS.md) is what
such a model is told before it starts, and [`spec/11-plan.md`](spec/11-plan.md) is the
plan it works to.

## Licence

MIT: see [`LICENSE`](LICENSE). The test songs are hymns long out of copyright and
made-up words. `reference/` is from Simple Presenter, under the same licence by the
same person. `proto/` has its own.

ProPresenter is a trademark of Renewed Vision. This project is not affiliated with or
endorsed by them.
