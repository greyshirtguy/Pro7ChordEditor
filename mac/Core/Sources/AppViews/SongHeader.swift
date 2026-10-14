// The header of the song that is open: its name, the switch between its two editors
// (EditorSwitch.swift), the way to its earlier versions, and its two keys.
// spec/06-library.md, rules 12 and 14; spec/05-song-and-saving.md, rules 5 to 7.
//
// How it is arranged goes by the width there is (the maintainer's, of 2026-10-11 and
// 12: nothing is ever cut short to fit but the name):
//
//   one row      the name at the left, the switch in the middle of the pane, and at
//                the right the way to the earlier versions and the keys;
//   two rows     the name and the switch; under them the keys, and at the right the
//                earlier versions;
//   three rows   the name and the earlier versions; the switch; the keys.
//
// WHY IT IS A LAYOUT OF ITS OWN, and each part is in it once. It was the three
// arrangements written out one after another for SwiftUI to choose the first that fits
// (`ViewThatFits`). To choose, SwiftUI builds the arrangements it is not showing, each
// with its own two pickers of twenty-six keys, measures them, and throws them away;
// and it did so whenever anything in the song's part of the window was laid out again.
// Measured on a song of 56 slides (2026-10-13, after the maintainer found the pointer
// lagging and the edge slow to drag): 100 to 250 thousandths of a second each time, on
// every step of the edge being dragged, and every time the line of hints under the
// sheet changed what it said, which is as often as the pointer crosses a chord. Here
// each part is made once and measured once, and where it goes is arithmetic
// (`HeaderArrangement`, which the tests hold).
//
// The header is a view of its own for the same reason: what it shows changes seldom,
// and the song's part of the window is laid out often.

import SwiftUI

/// Where the parts of the header go in a width. No window in it: sizes in, places out.
enum HeaderArrangement {
    enum Part: Int, CaseIterable, Sendable {
        case name, editors, earlier, keys
    }

    enum Rows: Equatable, Sendable {
        case one, two, three
    }

    /// The room between two parts on a row, between two rows, and the least that is
    /// kept between the name and what is at the right of its row.
    static let gap = 12.0
    static let rowGap = 8.0
    static let least = 8.0

    /// How big each part is when it has all the room it wants; nothing for a part that
    /// is not there (a song that could not be read has no switch and no keys; one that
    /// was never changed here has no earlier versions).
    struct Sizes: Equatable, Sendable {
        var name: CGSize
        var editors: CGSize?
        var earlier: CGSize?
        var keys: CGSize?

        /// The earlier versions and the keys side by side, as they are at the right
        /// of the one row.
        var right: Double {
            switch (earlier, keys) {
            case (let earlier?, let keys?): earlier.width + gap + keys.width
            case (let one?, nil), (nil, let one?): one.width
            case (nil, nil): 0
            }
        }

        /// What the one row needs: the name whole, and every part with the room
        /// between them.
        var oneRow: Double {
            name.width + gap + (editors.map { $0.width + gap } ?? 0) + right
        }

        /// What two rows need: the name whole beside the switch, and the keys beside
        /// the earlier versions.
        var twoRows: Double {
            let first = name.width + gap + least + (editors.map { gap + $0.width } ?? 0)
            let second = (keys?.width ?? 0) + (earlier.map { 2 * gap + $0.width } ?? 0)
            return max(first, second)
        }
    }

    /// How many rows the header is, in a width: as few as hold everything with the
    /// name whole. In the narrowest it is three whatever the name's length, and the
    /// name is then cut short in its middle.
    static func rows(in width: Double, _ sizes: Sizes) -> Rows {
        if sizes.oneRow <= width { return .one }
        // (With no switch and no keys there is nothing to put on a second row.)
        if sizes.editors == nil && sizes.keys == nil { return .one }
        if sizes.twoRows <= width { return .two }
        return .three
    }

    /// Where each part goes, in a width, counted from the header's top left; and how
    /// tall the header then is. A part that is not there has no place.
    static func places(in width: Double, _ sizes: Sizes) -> (places: [Part: CGRect], height: Double) {
        var places: [Part: CGRect] = [:]
        /// Puts parts on a row at these lefts, each in the middle of the row's height.
        func row(_ parts: [(Part, CGSize, Double)], top: Double) -> Double {
            let height = parts.map(\.1.height).max() ?? 0
            for (part, size, left) in parts {
                places[part] = CGRect(x: left, y: top + (height - size.height) / 2, width: size.width, height: size.height)
            }
            return height
        }
        /// The name, cut short if it must be to leave this much of the row to others.
        func name(leaving others: Double) -> CGSize {
            CGSize(width: max(0, min(sizes.name.width, width - others)), height: sizes.name.height)
        }

        switch rows(in: width, sizes) {
        case .one:
            // The two sides are given the same width where both fit in it, so that
            // the switch is in the middle of the pane and not of what is left over.
            let between = width - (sizes.editors.map { $0.width + 2 * gap } ?? gap)
            let half = between / 2
            let left: Double
            if sizes.name.width <= half && sizes.right <= half {
                left = half
            } else if sizes.name.width > half {
                left = min(sizes.name.width, between - sizes.right)
            } else {
                left = between - sizes.right
            }
            // (The name is whole here, but for a song that has nothing to put on a
            // second row: then it is cut short to leave what is at its right its room.)
            let beside = (sizes.editors.map { $0.width + gap } ?? 0) + (sizes.right > 0 ? sizes.right + gap : 0)
            var parts: [(Part, CGSize, Double)] = [(.name, name(leaving: beside), 0)]
            if let editors = sizes.editors {
                parts.append((.editors, editors, left + gap))
            }
            var at = width
            if let keys = sizes.keys {
                at -= keys.width
                parts.append((.keys, keys, at))
                at -= gap
            }
            if let earlier = sizes.earlier {
                at -= earlier.width
                parts.append((.earlier, earlier, at))
            }
            // (The row is laid before its places are handed back: a row puts its
            // parts' places down as it is laid.)
            let height = row(parts, top: 0)
            return (places, height)

        case .two:
            var first: [(Part, CGSize, Double)] = []
            let beside = sizes.editors.map { $0.width + gap + least } ?? 0
            first.append((.name, name(leaving: beside), 0))
            if let editors = sizes.editors {
                first.append((.editors, editors, width - editors.width))
            }
            let top = row(first, top: 0) + rowGap
            var second: [(Part, CGSize, Double)] = []
            if let keys = sizes.keys {
                second.append((.keys, keys, 0))
            }
            if let earlier = sizes.earlier {
                second.append((.earlier, earlier, width - earlier.width))
            }
            let height = top + row(second, top: top)
            return (places, height)

        case .three:
            var first: [(Part, CGSize, Double)] = []
            let beside = sizes.earlier.map { $0.width + gap + least } ?? 0
            first.append((.name, name(leaving: beside), 0))
            if let earlier = sizes.earlier {
                first.append((.earlier, earlier, width - earlier.width))
            }
            var top = row(first, top: 0)
            if let editors = sizes.editors {
                top += rowGap
                top += row([(.editors, editors, max(0, (width - editors.width) / 2))], top: top)
            }
            if let keys = sizes.keys {
                top += rowGap
                top += row([(.keys, keys, 0)], top: top)
            }
            return (places, top)
        }
    }
}

/// The header's parts, each said which it is, placed as HeaderArrangement says.
struct HeaderLayout: Layout {
    struct PartKey: LayoutValueKey {
        static let defaultValue = HeaderArrangement.Part.name
    }

    private func sizes(of subviews: Subviews) -> HeaderArrangement.Sizes {
        var sizes = HeaderArrangement.Sizes(name: .zero)
        for subview in subviews {
            let size = subview.sizeThatFits(.unspecified)
            switch subview[PartKey.self] {
            case .name: sizes.name = size
            case .editors: sizes.editors = size
            case .earlier: sizes.earlier = size
            case .keys: sizes.keys = size
            }
        }
        return sizes
    }

    func sizeThatFits(proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) -> CGSize {
        let sizes = sizes(of: subviews)
        // (Asked how wide it would like to be, it is its one row; told a width, it is
        // that wide.)
        let width = proposal.width.map { $0.isFinite ? $0 : sizes.oneRow } ?? sizes.oneRow
        return CGSize(width: width, height: HeaderArrangement.places(in: width, sizes).height)
    }

    func placeSubviews(in bounds: CGRect, proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) {
        let places = HeaderArrangement.places(in: bounds.width, sizes(of: subviews)).places
        for subview in subviews {
            guard let place = places[subview[PartKey.self]] else { continue }
            subview.place(at: CGPoint(x: bounds.minX + place.minX, y: bounds.minY + place.minY), anchor: .topLeading,
                          proposal: ProposedViewSize(width: place.width, height: place.height))
        }
    }
}

struct SongHeader: View {
    @Bindable var model: LibraryModel
    var shown: LibraryModel.ShownSong

    var body: some View {
        HeaderLayout {
            Text(shown.name)
                .font(.title2)
                .lineLimit(1)
                .truncationMode(.middle)
                .layoutValue(key: HeaderLayout.PartKey.self, value: .name)
            if shown.problem == nil {
                EditorSwitch(model: model)
                    .layoutValue(key: HeaderLayout.PartKey.self, value: .editors)
            }
            if model.hasEarlierVersions {
                earlierVersions
                    .layoutValue(key: HeaderLayout.PartKey.self, value: .earlier)
            }
            if shown.problem == nil {
                HStack(alignment: .firstTextBaseline, spacing: HeaderArrangement.gap) {
                    keys
                }
                .fixedSize()
                .layoutValue(key: HeaderLayout.PartKey.self, value: .keys)
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
    }

    /// In a colour of its own, with the picture a song has in the list before it: it
    /// is the way back, and was passed over as one more grey button. (The
    /// maintainer's, 2026-10-13.)
    private var earlierVersions: some View {
        Button {
            model.showEarlierVersions()
        } label: {
            HStack(spacing: 5) {
                PresentationGlyph()
                    .fill(.white)
                    .aspectRatio(1, contentMode: .fit)
                    .frame(width: 13, height: 13)
                    .accessibilityHidden(true)
                Text("Earlier Versions")
            }
        }
        .buttonStyle(FilledButton(colour: SongLook.earlierVersions))
        .fixedSize()
        .help("See this song as it was before it was changed here, and put a version back.")
    }

    /// The song's two keys, each to be set: the key its chords are written in, and,
    /// quieter beside it, the key ProPresenter is to show them in. Neither changes any
    /// chord in the file.
    @ViewBuilder
    private var keys: some View {
        // (Each picker lists the song's own key before the twenty-six if it is not one
        // of them, and "None" before them if the song states none: `keysListed`. A
        // picker whose choice is not in its list shows nothing. A key is drawn with
        // signs while they are on, B♭ and F♯m, and chosen by its name: spec/06, rule
        // 12.)
        Picker("Key", selection: Binding(get: { shown.key }, set: { model.setKey($0) })) {
            ForEach(model.keysListed(for: shown.key), id: \.self) { Text($0.isEmpty ? "None" : model.drawn($0)).tag($0) }
        }
        .fixedSize()
        .disabled(!model.canChange)
        .help(shown.key.isEmpty ? "This song does not say what key it is in. Pick the key its chords are written in."
                                : "The key the song's chords are written in. Changing it changes no chord.")
        Picker("Shown in", selection: Binding(get: { shown.shownKey }, set: { model.setShownKey($0) })) {
            ForEach(model.keysListed(for: shown.shownKey), id: \.self) { Text($0.isEmpty ? "None" : model.drawn($0)).tag($0) }
        }
        .fixedSize()
        .foregroundStyle(.secondary)
        .disabled(!model.canChange || shown.key.isEmpty)
        .help(shown.key.isEmpty ? "Set the song's key first, and then the key ProPresenter is to show it in."
                                : "The key ProPresenter shows this song's chords in. It moves them there itself; the chords here stay as they are written.")
    }
}
