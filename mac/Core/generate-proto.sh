#!/bin/sh
# Makes the Swift code that reads and writes a ProPresenter presentation
# (Sources/ProFormat/Generated), from the descriptions of the format in the proto/
# submodule. This is "the command that made it", which spec/10-platforms.md asks to be
# kept beside generated code that is in the repository.
#
#     mac/Core/generate-proto.sh
#
# Why the code is kept and not made at each build: swift-protobuf's build plug-in only
# takes .proto files that are inside the package, and these are in a submodule outside
# it, shared with the Windows app. Kept, the package builds from a clone with no
# submodule and no protoc, on GitHub's machine too. The price is that this must be run
# again by hand when the submodule moves on, which MadeFrom.swift is there to show: it
# says what the code beside it was made from.
#
# Only what a presentation needs is generated: presentation.proto and everything it
# imports, worked out by protoc itself so that the list cannot go stale. The whole set
# is five times the size and is mostly about things this app never opens. When
# something else is to be read (a workspace's settings, say), add its file to ROOTS.
#
# protoc will warn, once for nearly every file, that an import of customOptions.proto
# is unused. It is, and it does no harm: the descriptions are taken out of ProPresenter
# by a program, which leaves those lines in.
#
# Needs protoc and protoc-gen-swift (`brew install protobuf swift-protobuf`), and the
# submodule (`git submodule update --init`). The library the code is built against
# (Package.swift) must not be older than the protoc-gen-swift that made it.
set -eu

ROOTS="presentation.proto"

HERE=$(cd "$(dirname "$0")" && pwd)
PROTO="$HERE/../../proto/autogen-proto"
OUT="$HERE/Sources/ProFormat/Generated"

command -v protoc >/dev/null || { echo "protoc is not installed: brew install protobuf" >&2; exit 1; }
command -v protoc-gen-swift >/dev/null || { echo "protoc-gen-swift is not installed: brew install swift-protobuf" >&2; exit 1; }
[ -f "$PROTO/presentation.proto" ] || { echo "proto/ is empty: run git submodule update --init" >&2; exit 1; }
[ -d "$OUT" ] || { echo "there is no $OUT to write into" >&2; exit 1; }

# Every file the roots import, by way of protoc's own account of them. The names of the
# files are the only lines of that account indented by exactly two spaces and starting
# `name:`. Google's own files are left out: the library has those already.
# shellcheck disable=SC2086
FILES=$(protoc -I "$PROTO" --include_imports --descriptor_set_out=/dev/stdout $ROOTS \
    | protoc -I "$PROTO" --decode google.protobuf.FileDescriptorSet google/protobuf/descriptor.proto \
    | sed -n 's/^  name: "\(.*\)"$/\1/p' \
    | grep -v '^google/protobuf/' \
    | sort)
[ -n "$FILES" ] || { echo "protoc named no files" >&2; exit 1; }

# Made in a folder of its own first, so that a failure leaves what was there alone.
NEW=$(mktemp -d)
trap 'rm -rf "$NEW"' EXIT
# shellcheck disable=SC2086
protoc -I "$PROTO" --swift_out="$NEW" --swift_opt=Visibility=Public $FILES

rm -f "$OUT"/*.pb.swift
cp "$NEW"/*.pb.swift "$OUT"/

cat > "$OUT/MadeFrom.swift" <<EOF
// What the code in this folder was made from. Written by generate-proto.sh, with the
// rest of the folder: do not edit it, run that again.

/// What the generated reader of ProPresenter's files was made from, and with what.
public enum MadeFrom {
    /// The version of ProPresenter the descriptions were taken from (proto/autogen-proto/version.txt).
    public static let proPresenterVersion = "$(tr -d '[:space:]' < "$PROTO/version.txt")"
    /// The commit of the proto/ submodule.
    public static let protoCommit = "$(git -C "$HERE/../../proto" rev-parse --short HEAD)"
    /// The generator.
    public static let generator = "$(protoc-gen-swift --version), $(protoc --version)"
    /// How many .proto files a presentation takes in.
    public static let fileCount = $(echo "$FILES" | wc -l | tr -d ' ')
}
EOF

echo "$(echo "$FILES" | wc -l | tr -d ' ') files, from ProPresenter $(tr -d '[:space:]' < "$PROTO/version.txt"), into $OUT"
