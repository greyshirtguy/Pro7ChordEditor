#!/bin/sh
# Makes a disk image of the Mac app to give to other people:
#
#     mac/make-dmg.sh              writes mac/build/Pro7ChordEditor-mac-<version>.dmg
#
# What it does, in order, stopping at the first thing that is not as it should be:
#
#   1. Builds the app as a Release, for both kinds of Mac (Apple's own chips and
#      Intel's), in a build folder of its own under mac/build, so that it never
#      disturbs a build Xcode has open. And without the leave to be debugged that
#      Xcode gives every build it makes for its own use (the entitlement
#      get-task-allow), which lets any other program on the Mac look inside the app
#      as it runs: right for a build one is working on, wrong for one given out.
#      And with what a debugger goes by taken out of the program (stripped), which
#      Xcode does only when it installs or archives, not when it builds: the tables
#      it leaves in name every file the program was made from by its whole path, and
#      so the folders of the Mac it was built on, the account's name among them.
#   2. Looks at what it built: its version, that it runs on both kinds of Mac, how it
#      is signed, that its seal holds, that it has its guide (the words and every
#      picture they name), and that nothing in it names a folder of this Mac.
#   3. Lays out a folder: the app, a way to Applications to drag it onto, and the
#      read-me that is kept beside this script (mac/dmg/Read Me First.txt).
#   4. Makes a compressed image of that folder, mounts the image, and holds the app in
#      it against the one that was built.
#
# The version is the app's own (MARKETING_VERSION in the Xcode project): change it
# there, not here.
#
# HOW IT IS SIGNED, and what that means for whoever is given it. The project signs
# "to run locally" (an ad hoc signature, the identity "-"), which needs no certificate
# and no account. That is not nothing: a Mac with Apple's own chip will not run code
# that has no signature at all. But it names no one, and Apple has not checked the app
# (notarized it), so on another Mac, once the image has come through a browser, macOS
# refuses to open the app at a double-click, and the person has to allow it in System
# Settings, Privacy & Security. The read-me in the image says how.
#
# WHEN THERE IS A DEVELOPER ID (spec/10-platforms.md, "Getting it to other people";
# milestone M10): the app is signed with it and with a secure timestamp, the image is
# sent to Apple to be notarized (xcrun notarytool submit ... --wait) and the ticket
# stapled to it (xcrun stapler staple), and the section of the read-me about opening
# it the first time goes. None of that is here yet.
#
# Needs Xcode and nothing else: hdiutil, codesign, lipo and PlistBuddy come with macOS.
set -eu

HERE=$(cd "$(dirname "$0")" && pwd)
BUILD="$HERE/build"
DERIVED="$BUILD/DerivedData"
NAME="Pro7 Chord Editor"
APP="$DERIVED/Build/Products/Release/$NAME.app"
README="$HERE/dmg/Read Me First.txt"

[ -f "$README" ] || { echo "there is no $README to put in the image" >&2; exit 1; }
mkdir -p "$BUILD"

echo "Building the app..."
xcodebuild \
    -project "$HERE/Pro7ChordEditor.xcodeproj" \
    -scheme Pro7ChordEditor \
    -configuration Release \
    -destination "generic/platform=macOS" \
    -derivedDataPath "$DERIVED" \
    ONLY_ACTIVE_ARCH=NO \
    CODE_SIGN_INJECT_BASE_ENTITLEMENTS=NO \
    DEPLOYMENT_POSTPROCESSING=YES \
    STRIP_INSTALLED_PRODUCT=YES \
    build > "$BUILD/build.log" 2>&1 || { tail -40 "$BUILD/build.log" >&2; echo "the build failed: all of what it said is in $BUILD/build.log" >&2; exit 1; }
[ -d "$APP" ] || { echo "the build did not leave $APP" >&2; exit 1; }

# ---- What was built
PLIST="$APP/Contents/Info.plist"
VERSION=$(/usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$PLIST")
NUMBER=$(/usr/libexec/PlistBuddy -c "Print :CFBundleVersion" "$PLIST")
NEEDS=$(/usr/libexec/PlistBuddy -c "Print :LSMinimumSystemVersion" "$PLIST")
ARCHS=$(lipo -archs "$APP/Contents/MacOS/$NAME")
case "$ARCHS" in *arm64*) ;; *) echo "the app was not built for Apple's own chips: $ARCHS" >&2; exit 1 ;; esac
case "$ARCHS" in *x86_64*) ;; *) echo "the app was not built for Intel's chips: $ARCHS" >&2; exit 1 ;; esac
codesign --verify --deep --strict "$APP" || { echo "the app's signature does not hold" >&2; exit 1; }
if codesign -d --entitlements - "$APP" 2>/dev/null | grep -q "get-task-allow"; then
    echo "the app was built with leave to be debugged (get-task-allow), which a build to give out must not have" >&2
    exit 1
fi
# The guide, which the app opens by itself at its first start: its words, which are
# the spec's to the byte, and every picture those words name (spec/13-guide.md).
GUIDE="$APP/Contents/Resources/Core_AppViews.bundle/Contents/Resources/Guide"
[ -f "$GUIDE/guide.json" ] || { echo "the app has not got its guide: there is no $GUIDE/guide.json" >&2; exit 1; }
cmp -s "$GUIDE/guide.json" "$HERE/../spec/guide.json" || { echo "the guide's words in the app are not spec/guide.json" >&2; exit 1; }
PICTURES=$(sed -n 's/.*"picture": "\([^"]*\)".*/\1/p' "$GUIDE/guide.json" | sort -u)
[ -n "$PICTURES" ] || { echo "the guide's words name no picture, which cannot be right" >&2; exit 1; }
for PICTURE in $PICTURES; do
    [ -s "$GUIDE/$PICTURE.png" ] || { echo "the app has not got the guide's picture $PICTURE.png" >&2; exit 1; }
done
SIGNED=$(codesign -dv "$APP" 2>&1 | sed -n 's/^Signature=//p; s/^Authority=//p' | head -1)
[ -n "$SIGNED" ] || SIGNED="in a way this script does not know"
# Nothing in the app is to say where on this Mac it was made: not the home folder of
# whoever built it, nor the folder this script is in. (Looked for as bytes, in every
# file: `strings` does not show what is in a program's tables for a debugger.)
for WHERE in "$HOME" "$HERE"; do
    NAMED=$(LC_ALL=C grep -r -a -l -F "$WHERE" "$APP" | head -3 || true)
    if [ -n "$NAMED" ]; then
        echo "the app names a folder of this Mac ($WHERE), in:" >&2
        echo "$NAMED" >&2
        exit 1
    fi
done

# ---- The folder the image is made of
STAGE=$(mktemp -d "${TMPDIR:-/tmp}/Pro7ChordEditor-dmg.XXXXXX")
MOUNT=$(mktemp -d "${TMPDIR:-/tmp}/Pro7ChordEditor-dmg-mounted.XXXXXX")
tidy() {
    # Only ever the two folders made just above, and only if they are named as such.
    hdiutil detach "$MOUNT" -quiet 2>/dev/null || true
    case "$STAGE" in */Pro7ChordEditor-dmg.*) rm -rf "$STAGE" ;; esac
    case "$MOUNT" in */Pro7ChordEditor-dmg-mounted.*) rmdir "$MOUNT" 2>/dev/null || true ;; esac
}
trap tidy EXIT

# (ditto, and not cp: it keeps the app as it was signed, to the last attribute.)
ditto "$APP" "$STAGE/$NAME.app"
ln -s /Applications "$STAGE/Applications"
cp "$README" "$STAGE/Read Me First.txt"

# ---- The image
OUT="$BUILD/Pro7ChordEditor-mac-$VERSION.dmg"
echo "Making the image..."
hdiutil create -volname "$NAME $VERSION" -srcfolder "$STAGE" -fs HFS+ -format UDZO -ov -quiet "$OUT"
hdiutil verify -quiet "$OUT"

# Mounted, and looked into: the app in it is the app that was built, and is still whole.
hdiutil attach "$OUT" -nobrowse -readonly -mountpoint "$MOUNT" -quiet
codesign --verify --deep --strict "$MOUNT/$NAME.app" || { echo "the app in the image has lost its signature" >&2; exit 1; }
diff -r "$APP" "$MOUNT/$NAME.app" > /dev/null || { echo "the app in the image is not the app that was built" >&2; exit 1; }
[ -L "$MOUNT/Applications" ] && [ -f "$MOUNT/Read Me First.txt" ] || { echo "the image has not got all it should have in it" >&2; exit 1; }
hdiutil detach "$MOUNT" -quiet

echo
echo "$OUT"
echo "  $NAME $VERSION ($NUMBER), for macOS $NEEDS and later, on $ARCHS"
echo "  signed: $SIGNED"
echo "  $(du -h "$OUT" | cut -f1 | tr -d ' '), sha256 $(shasum -a 256 "$OUT" | cut -d' ' -f1)"
case "$SIGNED" in
    adhoc) echo "  Not notarized: on another Mac it has to be allowed in System Settings before it opens. The read-me in the image says how." ;;
esac
