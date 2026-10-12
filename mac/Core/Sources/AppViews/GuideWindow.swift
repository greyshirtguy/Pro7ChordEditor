// The guide's window: one page that says how chords are put on a song, with pictures of
// this app's own sheet. spec/13-guide.md.
//
// It is a window of its own, and not a panel that must be put away before the app is
// used: what it says is to be tried as it is read, on the song behind it. It opens by
// itself once, at the first start of the app on this Mac (LibraryModel says when), and
// is under Help, and behind the ? at the right of the sheet's strip, ever after.
//
// Nothing here is a rule, and nothing in the window does anything to a song. The words
// are the guide's own (Guide.swift), laid out in the order they come:
//
//   the title;
//   what to know before starting, in a box that is not to be missed;
//   then each section: its heading, the one sentence that is written large (the first
//   section's: the quick way), its picture, its paragraphs, and its ways of doing a
//   thing, each a few bold words and what they mean, some with a picture under them;
//   a line of ChordPro text, where a section has one, shown as the ChordPro editor
//   shows one;
//   and at the foot, where the page is found again.
//
// The page follows the system, light or dark. The pictures are of the sheet, which is
// dark in both. Each is shown at the size it was drawn, or smaller where the window is
// narrower, and never larger.

import AppKit
import SwiftUI

/// The sizes and colours of the page.
enum GuideLook {
    /// As wide as a line of the words is let grow, which is as wide as the pictures
    /// were drawn.
    static let width = 720.0
    static let margin = 30.0
    static let body = Font.system(size: 14)
    static let accent = Color(nsColor: SheetLook.accent)
}

public struct GuideWindow: View {
    /// What the app's scene for this window is known by.
    public static let id = "guide"
    /// What the window is called, in its title and in the Help menu.
    public static let title = "Pro7 Chord Editor Guide"
    /// What the ? at the right of the strip says when it is pointed at.
    public static let tip = "The guide: the ways to put a chord on, on one page. It is in the Help menu too."

    private var model: LibraryModel
    private var guide: Guide?

    public init(model: LibraryModel, guide: Guide? = GuideFiles.guide) {
        self.model = model
        self.guide = guide
    }

    public var body: some View {
        ScrollView {
            Group {
                if let guide {
                    GuidePage(guide: guide, chordColour: ChordColour.colour(model.textChordColour) ?? .systemOrange)
                } else {
                    // (It is the app's own, and is always there. But if it is not, that
                    // is said here and the app goes on.)
                    Text("The guide could not be found in this copy of the app.")
                        .foregroundStyle(.secondary)
                }
            }
            .frame(maxWidth: GuideLook.width, alignment: .leading)
            .padding(GuideLook.margin)
            .frame(maxWidth: .infinity)
        }
        .background(Color(nsColor: .textBackgroundColor))
        .frame(minWidth: 460, idealWidth: GuideLook.width + 2 * GuideLook.margin, minHeight: 320, idealHeight: 820)
    }
}

/// The page itself, as tall as its words and pictures make it.
struct GuidePage: View {
    var guide: Guide
    /// The colour of a chord in ChordPro text, as the settings have it.
    var chordColour: NSColor

    var body: some View {
        VStack(alignment: .leading, spacing: 26) {
            Text(guide.title)
                .font(.system(size: 28, weight: .bold))
                .fixedSize(horizontal: false, vertical: true)
            first
            ForEach(Array(guide.sections.enumerated()), id: \.offset) { _, section in
                self.section(section)
            }
            Divider()
            Text(guide.foot)
                .font(.callout)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    /// What to know before starting: in a box of the sheet's orange, faint, so that it
    /// is seen before anything else and read as apart from the ways that follow.
    private var first: some View {
        VStack(alignment: .leading, spacing: 9) {
            Text(guide.first.heading)
                .font(.system(size: 15, weight: .bold))
            ForEach(Array(guide.first.points.enumerated()), id: \.offset) { _, point in
                pointed(Text(point))
            }
        }
        .padding(16)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(GuideLook.accent.opacity(0.11)))
        .overlay(RoundedRectangle(cornerRadius: 10, style: .continuous).stroke(GuideLook.accent.opacity(0.4)))
    }

    private func section(_ section: Guide.Section) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text(section.heading)
                .font(.system(size: 19, weight: .bold))
                .fixedSize(horizontal: false, vertical: true)
            if let lead = section.lead {
                // The one thing to take away, if only one is.
                Text(lead)
                    .font(.system(size: 27, weight: .semibold))
                    .foregroundStyle(GuideLook.accent)
                    .fixedSize(horizontal: false, vertical: true)
            }
            if let picture = section.picture {
                GuidePicture(name: picture, says: guide.pictures[picture] ?? "")
            }
            ForEach(Array((section.paragraphs ?? []).enumerated()), id: \.offset) { _, paragraph in
                words(Text(paragraph))
            }
            ForEach(Array((section.items ?? []).enumerated()), id: \.offset) { _, item in
                VStack(alignment: .leading, spacing: 10) {
                    pointed(Text(item.lead).bold() + Text(" ") + Text(item.text))
                    if let picture = item.picture {
                        GuidePicture(name: picture, says: guide.pictures[picture] ?? "")
                            .padding(.leading, 16)
                    }
                }
            }
            if let sample = section.sample {
                self.sample(sample)
            }
        }
    }

    private func words(_ text: Text) -> some View {
        text
            .font(GuideLook.body)
            .lineSpacing(3)
            .fixedSize(horizontal: false, vertical: true)
            .textSelection(.enabled)
    }

    /// Words with a small orange point before their first line.
    private func pointed(_ text: Text) -> some View {
        HStack(alignment: .firstTextBaseline, spacing: 9) {
            Circle()
                .fill(GuideLook.accent)
                .frame(width: 7, height: 7)
                // (Lifted to the middle of the small letters of the line beside it.)
                .alignmentGuide(.firstTextBaseline) { $0[.bottom] + 3 }
                .accessibilityHidden(true)
            words(text)
        }
    }

    /// A line of ChordPro text as the ChordPro editor shows one: in letters of one
    /// width, its chords in their colour, brackets and all.
    private func sample(_ line: String) -> some View {
        var text = Text("")
        var rest = Substring(line)
        while let open = rest.firstIndex(of: "["), let close = rest[open...].firstIndex(of: "]") {
            text = text + Text(rest[..<open]) + Text(rest[open...close]).bold().foregroundColor(Color(nsColor: chordColour))
            rest = rest[rest.index(after: close)...]
        }
        return (text + Text(rest))
            .font(.system(size: 14, design: .monospaced))
            .padding(.horizontal, 12)
            .padding(.vertical, 9)
            .background(RoundedRectangle(cornerRadius: 6).fill(Color.primary.opacity(0.06)))
            .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color.primary.opacity(0.14)))
            .accessibilityLabel("A line of ChordPro text: \(line)")
    }
}

/// One of the guide's pictures, at the size it was drawn or smaller, with what it shows
/// said in words for whoever cannot see it. Where the picture is not there, the words
/// stand in its place.
struct GuidePicture: View {
    var name: String
    var says: String

    var body: some View {
        if let image = Self.image(name) {
            Image(nsImage: image)
                .resizable()
                .interpolation(.high)
                .aspectRatio(image.size, contentMode: .fit)
                .frame(maxWidth: image.size.width)
                // (As tall as its width makes it, and never squeezed shorter by what
                // is around it: it would then stand narrow in a frame too wide for it.)
                .fixedSize(horizontal: false, vertical: true)
                .clipShape(RoundedRectangle(cornerRadius: 8, style: .continuous))
                .overlay(RoundedRectangle(cornerRadius: 8, style: .continuous).stroke(Color.primary.opacity(0.18)))
                .accessibilityLabel(says)
        } else {
            Text(says)
                .font(.callout)
                .italic()
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
    }

    /// The picture of that name, read once. It says its own size, which is half its
    /// dots each way: it was drawn at twice the size, for a screen with dots to spare.
    @MainActor
    static func image(_ name: String) -> NSImage? {
        if let read = images[name] { return read }
        guard let file = GuideFiles.picture(name), let image = NSImage(contentsOf: file) else { return nil }
        images[name] = image
        return image
    }

    @MainActor private static var images: [String: NSImage] = [:]
}

/// Help, in the menu bar: the guide, where the system would otherwise say that this
/// app has no help. Command with the question mark is the Mac's own key for it.
public struct GuideCommands: Commands {
    @Environment(\.openWindow) private var openWindow

    public init() {}

    public var body: some Commands {
        CommandGroup(replacing: .help) {
            Button(GuideWindow.title) { openWindow(id: GuideWindow.id) }
                .keyboardShortcut("?", modifiers: .command)
        }
    }
}
