// swift-tools-version: 6.1
//
// Everything of the Mac app that has no window in it, as one Swift package, so that it
// can be built and proved from the command line without Xcode ever being opened:
//
//     swift test --package-path mac/Core
//
// Three libraries, each knowing only of those above it in this list
// (spec/10-platforms.md, "The Mac app"):
//
//   ChordRules   What a chord is, keys, moving to another key, where a chord stands
//                in the words, the words of an RTF. Plain Swift and Foundation: no
//                protobuf, no AppKit, no SwiftUI. Held to testdata/vectors.
//   ProFormat    The reader and writer of a .pro file, generated from the descriptions
//                of ProPresenter's format. Nothing in it is written by hand except the
//                file that says so.
//   Song         A song as the editors see it: opening a file, its text boxes, setting
//                chords and the key, saving safely, undo, and later the copies and the
//                checks. On top of the other two. Held to testdata/files.
//
// The app (../App, built by ../Pro7ChordEditor.xcodeproj) is a thin fourth thing on
// top of Song. It is not in this package, because an app with an Info.plist and a
// signature is Xcode's to build, not the package manager's.
//
// One package and not three: one `swift test`, one Package.resolved, and the layers
// are still kept apart, because a target can only import what it names below.
//
// swift-protobuf is the one dependency, named by the spec. Add no other without the
// maintainer's yes (AGENTS.md).

import PackageDescription

let package = Package(
    name: "Core",
    // The newest macOS and the one before it.
    platforms: [.macOS(.v15)],
    products: [
        .library(name: "ChordRules", targets: ["ChordRules"]),
        .library(name: "ProFormat", targets: ["ProFormat"]),
        .library(name: "Song", targets: ["Song"]),
    ],
    dependencies: [
        // Not older than the protoc-gen-swift that made Sources/ProFormat/Generated
        // (Generated/MadeFrom.swift says which that was).
        .package(url: "https://github.com/apple/swift-protobuf.git", from: "1.38.1"),
    ],
    targets: [
        .target(name: "ChordRules"),
        .target(
            name: "ProFormat",
            dependencies: [.product(name: "SwiftProtobuf", package: "swift-protobuf")]
        ),
        .target(
            name: "Song",
            dependencies: [
                "ChordRules",
                "ProFormat",
                .product(name: "SwiftProtobuf", package: "swift-protobuf"),
            ]
        ),

        // How the tests find testdata/ at the top of the repository. Not a test target
        // itself: both of those use it.
        .target(name: "TestData", path: "Tests/TestData"),
        .testTarget(name: "ChordRulesTests", dependencies: ["ChordRules", "TestData"]),
        .testTarget(
            name: "SongTests",
            dependencies: [
                "Song",
                "ChordRules",
                "ProFormat",
                "TestData",
                .product(name: "SwiftProtobuf", package: "swift-protobuf"),
            ]
        ),
    ]
)
