// Which key a song is likely in, held to testdata/vectors/key-fit.json.

import ChordRules
import Testing

@Suite("Which key is likely")
struct KeyFitTests {
    struct File: Decodable {
        struct Case: Decodable {
            let about: String
            let stated: String
            let chords: [String]
            let likeliest: [String]
            let doubtful: Bool
            let suggested: String
            let scores: [String: Double]
        }
        let cases: [Case]
    }

    /// Every key's score, in order of the keys' names. A score is spelt as the shortest
    /// number that reads back as itself, so two are spelt alike only if they are the
    /// same number exactly.
    private func spellScores(_ scores: [String: Double]) -> String {
        scores.sorted { $0.key < $1.key }.map { "\($0.key)=\($0.value)" }.joined(separator: " ")
    }

    @Test("key-fit.json: every case")
    func everyCase() throws {
        let file: File = try vector("key-fit.json")
        var tally = Tally("key-fit.json")
        for c in file.cases {
            let answer = KeyFit.likely(chords: c.chords, stated: c.stated)
            let which = spell(c.about)
            tally.check(spell(answer.likeliest), spell(c.likeliest), "\(which): likeliest")
            tally.check(spell(answer.doubtful), spell(c.doubtful), "\(which): doubtful")
            tally.check(spell(answer.suggested), spell(c.suggested), "\(which): suggested")
            tally.check(spellScores(answer.scores), spellScores(c.scores), "\(which): scores")
        }
        tally.settle()
    }
}
