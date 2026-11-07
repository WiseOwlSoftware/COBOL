// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Editions;

/// <summary>
/// One per-edition reserved-word row (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.4): the
/// four-edition reservation flags for a word, its confidence, and its provenance. Rows come from the generated
/// table (<c>ReservedWords.Table.cs</c>, emitted by <c>scripts/gen-reserved-words.ps1</c> from the in-repo ISO
/// 2023 §2023:8.9 list + VCR row 32 + the transcribed ISO 2002 §2002:8.9 list (the 2002 flags) + the 2009 draft's 8.9/8.10
/// lists (one 2014 correction) + the GnuCOBOL per-standard 85/2014 word lists — derived facts with provenance,
/// kb/Work PB4297); <c>tests/version-matrix/reserved-words.json</c> is the same data in its canonical form and a
/// drift test asserts they agree both directions.
/// </summary>
/// <param name="Word">The word, uppercase.</param>
/// <param name="R85">Reserved in ANSI X3.23-1985.</param>
/// <param name="R2002">Reserved in ISO/IEC 1989:2002.</param>
/// <param name="R2014">Reserved in ISO/IEC 1989:2014.</param>
/// <param name="R2023">Reserved in ISO/IEC 1989:2023 (§2023:8.9).</param>
/// <param name="Confidence">"high" = the row may REJECT; anything lower is present but inert (the conservative
/// policy — a wrong entry must never reject a valid program; VCR scope-limit rule).</param>
/// <param name="Provenance">Where the classification comes from (spec §, annex item, or the documented
/// presumption pending older-standard evidence — the decision-1 provisional policy).</param>
public sealed record ReservedWordEntry(
    string Word, bool R85, bool R2002, bool R2014, bool R2023, string Confidence, string Provenance)
{
    /// <summary>Whether the word is reserved at <paramref name="edition"/> (85/2002/2014/2023).</summary>
    public bool IsReservedAt(int edition) => edition switch
    {
        85 => R85,
        2002 => R2002,
        2014 => R2014,
        _ => R2023,
    };
}

/// <summary>The generated §2023:8.9 word table (the partial half is <c>ReservedWords.Table.cs</c>).</summary>
public static partial class ReservedWords
{
    private static Dictionary<string, ReservedWordEntry>? _byWord;

    /// <summary>Look up <paramref name="word"/> in the generated table, keyed by the Annex C fold
    /// (<see cref="CobolNames.Comparer"/>, kb/Work PB1965), so any spelling of the word finds its row.</summary>
    public static ReservedWordEntry? Find(string word) =>
        (_byWord ??= Entries.ToDictionary(e => e.Word, CobolNames.Comparer)).GetValueOrDefault(word);

    /// <summary>⛔ WHETHER A GENERAL FORMAT OF <paramref name="edition"/> CAN PRINT <paramref name="word"/> AS A KEYWORD
    /// (kb/Work PB4174) — the question a diagnostic asks before it tells the user which words are EXPECTED. The grammar
    /// parses the superset of all editions, so at COBOL-85 its expected set holds the 2002 boolean operators and at
    /// every edition below 2023 the 2023 shift operators (<c>B-SHIFT-L</c>…); naming them sends the user to a word the
    /// targeted edition does not have (ISO §2023:8.3.2.1 1: "Reserved words shall not be used as user-defined words or
    /// system-names", so a word §2023:8.9 has not reserved yet is a user-defined word there), and that edition-blind sentence
    /// was the whole of the defect.
    /// <para>False only for the two shapes the tables PROVE. A §2023:8.9 word the edition does not reserve, no OLDER edition
    /// reserved, and a LATER edition does: a word introduced after <paramref name="edition"/>. A §2023:8.10 context-sensitive
    /// word (a word §2023:8.9 does not reserve at or before the edition) whose first listing is a LATER edition
    /// (<see cref="ContextSensitiveWordEntry.Introduced"/>: <c>ROUNDED MODE</c>'s words are 2014's). Every other word is
    /// printable: one reserved at the edition, one an older edition reserved and this one dropped (the migration
    /// mode's removed clause, and the standard's own anomalies — COBOL and PHYSICAL are underlined in 2023 formats
    /// that §2023:8.9 does not list), one in neither table, and one whose §2023:8.9 row may not reject (anything but high
    /// confidence is inert, the conservative policy). A word wrongly named costs the user a sentence; a word wrongly
    /// left out of one costs them the right keyword, so the doubtful cases name.</para></summary>
    public static bool AdmitsAsKeyword(string word, int edition)
    {
        var row = Find(word) is { Confidence: "high" } high ? high : null;
        if (row is not null)
        {
            if (row.IsReservedAt(edition)) return true;
            foreach (int older in EditionInfo.Before(edition))
                if (row.IsReservedAt(older)) return true;
        }
        // §2023:8.10 and §2023:8.9 are disjoint at 2023 (ContextSensitiveWordsDriftTests); a context word an older edition
        // RESERVED outright (MULTIPLE, a 1985 reserved word) was answered above.
        if (ContextSensitiveWords.Find(word) is { } context) return edition >= context.Introduced;
        if (row is not null)
            foreach (int later in EditionInfo.All)
                if (later > edition && row.IsReservedAt(later)) return false;
        return true;
    }
}

/// <summary>
/// The per-compilation-unit EFFECTIVE reserved-word set — the seam the validator consults, never the raw table
/// (roadmap ISO-validation D9): the 2023 COBOL-WORDS directive (ISO Annex E.3.3 item 12) adds/removes/renames
/// words per compilation group, so the generated table is only the DEFAULT layer. Today the default is the only
/// layer; the COBOL-WORDS pass (roadmap Phase 7) composes modified instances over it.
/// </summary>
public sealed class ReservedWordSet
{
    /// <summary>The default set: exactly the generated table.</summary>
    public static ReservedWordSet Default { get; } = new();

    // The 2023 >>COBOL-WORDS overlay (ISO §2023:7.3.10.4): RESERVE adds a word (GR5), UNDEFINE / SUBSTITUTE remove one
    // (GR3/GR4). Null on the Default set (no directive) so the effective behavior is byte-identical.
    private readonly IReadOnlySet<string>? _reserved;    // RESERVE literal-6 — a NEW reserved word
    private readonly IReadOnlySet<string>? _suppressed;  // UNDEFINE literal-3 + SUBSTITUTE literal-4 — de-reserved
    // EQUATE literal-2 / SUBSTITUTE literal-5 whose original word §2023:8.9 reserves → that word (GR2/GR4: a synonym for a
    // reserved word is that reserved word; kb/Work PB4488).
    private readonly IReadOnlyDictionary<string, string>? _synonyms;

    private ReservedWordSet() { }

    private ReservedWordSet(IReadOnlySet<string> reserved, IReadOnlySet<string> suppressed,
        IReadOnlyDictionary<string, string> synonyms)
    {
        _reserved = reserved;
        _suppressed = suppressed;
        _synonyms = synonyms;
        _keywordToken = suppressed.Count == 0 ? null : new ReservedWordSet(reserved, new HashSet<string>(), synonyms);
    }

    // The view of this set without its de-reservations (see KeywordToken); null when there are none to drop.
    private readonly ReservedWordSet? _keywordToken;

    /// <summary>⛔ THE SET A KEYWORD TOKEN IS ASKED OF (kb/Work PB4589). The lexer's retype has already applied the
    /// de-reservations to the token stream: a word UNDEFINE'd or SUBSTITUTE'd away (GR3, GR4) is an <c>IDENTIFIER</c>, so
    /// the only keyword token whose spelling is in the suppression set is a SYNONYM that stands for it (EQUATE literal-2,
    /// SUBSTITUTE literal-5), carrying the original's text (kb/Work PB4573) and reserved with its original (GR2, GR4;
    /// §2023:8.3.2.1 rule 1). Asking such a token of the suppressing set answers for the word the source no longer writes. This
    /// view keeps everything else the directive and §2023:8.9 say, RESERVE (GR5) included: a RESERVE of a word the lexer has a
    /// token for stays a keyword token and stays rejected as a user-defined word. <see cref="Default"/> is its own view.</summary>
    public ReservedWordSet KeywordToken => _keywordToken ?? this;

    /// <summary>Compose the effective per-compilation-group set from a <see cref="CobolWordsMap"/> override (ISO
    /// §2023:7.3.10). An empty map yields <see cref="Default"/> (byte-identical). RESERVE words become reserved;
    /// UNDEFINE / SUBSTITUTE words are de-reserved (SR5 forbids a word being both, so no conflict); a synonym of a
    /// reserved word is reserved (<see cref="CobolWordsMap.ReservedSynonyms"/>: the lexer retypes only the synonyms of a
    /// word it has a token for, so this overlay is what keeps the others from being user-defined words).</summary>
    public static ReservedWordSet Compose(CobolWordsMap map) =>
        map.IsEmpty ? Default : new ReservedWordSet(map.Reserved, map.DeReserved, map.ReservedSynonyms);

    /// <summary>The program's own reservation of <paramref name="upperWord"/>: a RESERVE (GR5) or a synonym of a reserved
    /// word (GR2, GR4). The spelling was a user word until this compilation group's directive took it, which is the
    /// <see cref="ConstructAvailability.Removed"/> shape.</summary>
    private bool ReservedByDirective(string upperWord)
        => _reserved?.Contains(upperWord) == true || _synonyms?.ContainsKey(upperWord) == true;

    /// <summary>The entry for <paramref name="upperWord"/>, or null when the word is not in the effective set.</summary>
    public ReservedWordEntry? Find(string upperWord) => ReservedWords.Find(upperWord);

    /// <summary>True when <paramref name="upperWord"/> is reserved for the compilation group and may emit
    /// COBOL0901 when used as a user-defined word: a <c>&gt;&gt;COBOL-WORDS</c> UNDEFINE/SUBSTITUTE suppression
    /// wins (never rejects), then a RESERVE overlay or a synonym of a reserved word reserves unconditionally, else the generated §2023:8.9 table's
    /// HIGH-CONFIDENCE reserved-at-edition rule (the conservative policy).</summary>
    public bool RejectsAt(string upperWord, int edition)
    {
        if (_suppressed?.Contains(upperWord) == true) return false;   // UNDEFINE/SUBSTITUTE — a user word now
        if (ReservedByDirective(upperWord)) return true;              // RESERVE, or a synonym of a reserved word
        return Find(upperWord) is { Confidence: "high" } e && e.IsReservedAt(edition);
    }

    /// <summary>
    /// WHY <paramref name="upperWord"/> rejects at <paramref name="edition"/> — the availability of the construct
    /// "this spelling used as a user-defined word", so the ONE
    /// <see cref="EditionSeverityPolicy"/> decides the severity axis instead of the emit site asserting it.
    /// <para>⛔ THE DISTINCTION IS NOT COSMETIC, and hard-coding <see cref="ConstructAvailability.Removed"/> got
    /// it wrong for every RE-RESERVED word. A spelling an edition TOOK AWAY (COMMIT: user-definable until 2023
    /// reserved it) is the migration case <c>--permissive</c> exists for — an existing program legitimately
    /// contains it. A spelling that was reserved at the target edition AND at every edition before it
    /// (RECEIVE / END-RECEIVE at COBOL-85, where the '85 communication module owns them) was NEVER a user word
    /// there, so no conforming program of that vintage can contain one and there is nothing to migrate: it is
    /// <see cref="ConstructAvailability.NotYetIntroduced"/>, an error on BOTH axes (CA14's policy, swept).</para>
    /// </summary>
    public ConstructAvailability UserWordVerdictAt(string upperWord, int edition)
    {
        if (!RejectsAt(upperWord, edition)) return ConstructAvailability.Available;
        // A >>COBOL-WORDS RESERVE (ISO §2023:7.3.10.4 GR5) or a synonym of a reserved word (GR2, GR4) is the PROGRAM's own
        // reservation, not the edition's: the spelling was a user word until this compilation group's directive took it,
        // which is the removed shape.
        if (ReservedByDirective(upperWord)) return ConstructAvailability.Removed;
        if (Find(upperWord) is not { } e) return ConstructAvailability.Removed;
        foreach (int older in EditionInfo.Before(edition))
            if (!e.IsReservedAt(older)) return ConstructAvailability.Removed;
        return ConstructAvailability.NotYetIntroduced;
    }

    /// <summary>⛔ THE §2023:8.9 USER-DEFINED-WORD SLOT ADMISSION RULE (kb/Work PB693, PB792) — true when a compile at
    /// <paramref name="edition"/> may read <paramref name="upperWord"/> as a user-defined word. The frontend's
    /// token-level reservation gate (<c>CobolParserCoreBase.userWordHere</c> → <c>ReservationGateRewriter</c>,
    /// kb/Work PB655) IS this predicate, so the gate and the §2023:8.9 funnel cannot disagree about which occurrences are names (§2023:8.3.2.1 rule 1:
    /// "Reserved words shall not be used as user-defined words or system-names").
    /// <para>⛔ THE MIGRATION MODE IS NOT A BLANKET EXEMPTION, and writing it as one (<c>Permissive || …</c> in the
    /// parser) was kb/Work PB792. <c>--permissive</c> "accepts constructs the targeted edition REMOVED", so it
    /// restores the pre-removal reading of a spelling §2023:8.9 TOOK AWAY — <see cref="ConstructAvailability.Removed"/>,
    /// where an existing program legitimately contains the word as a name. A spelling reserved at this edition AND
    /// at every older edition this compiler targets (COLUMN and DESTINATION are reserved at all four) was never a
    /// user word anywhere, so migration mode has nothing to restore: the verdict is
    /// <see cref="ConstructAvailability.NotYetIntroduced"/>, an error on BOTH axes, and admitting the word let every
    /// greedy operand list absorb it — <c>03 VALUE "DETAIL LINE " COLUMN 20 PIC X(12).</c> (NIST RW104A card
    /// 024500, legal by §2023:13.15.3 SR2 "All other clauses may be written in any order") lost its §2023:13.18.14 COLUMN
    /// clause and printed in the wrong place, silently.</para>
    /// <para>The axis therefore routes through the ONE <see cref="EditionSeverityPolicy"/>: the slot admits the
    /// word exactly where a §2023:8.9 violation here would be DOWNGRADED to a warning, and never where the policy says
    /// error. Allocation-free (<see cref="EditionInfo.Before"/> is a span) — it runs inside ANTLR's speculative
    /// prediction.</para></summary>
    public bool AdmitsAsUserWord(string upperWord, EditionInfo edition)
        // Confidence-blind, exactly as the gate has always been: a reserved row that may not REJECT (the
        // conservative policy) still yields Available below, so the gate and the funnel agree by construction.
        => Find(upperWord) is not { } e
        || !e.IsReservedAt(edition.Year)
        || EditionSeverityPolicy.For(UserWordVerdictAt(upperWord, edition.Year), edition)
           is not EditionSeverity.Error;

    /// <summary>⛔ THE ONE §2023:8.9 user-word-violation SENTENCE (kb/Work PB693). TWO stages report it and they must
    /// say the same thing: the bound-tree funnel (<c>VersionConformancePass.FlagReservedUserWord</c>), and the
    /// PARSER's error listener for the occurrences the reservation gate makes unparseable — a REFERENCE to the
    /// word, where no name-slot alternative can match and a raw COBOL0001 would never name §2023:8.9. It lives here,
    /// in the assembly both stages already reference, so the wording, the code and the clause have one
    /// definition (<c>feedback_one_rule_one_place</c>).</summary>
    /// <param name="written">The word as the source writes it (<c>CobolKeywords.Spelling</c>).</param>
    /// <param name="words">The compilation group's directives. A word that is a synonym (EQUATE literal-2, SUBSTITUTE
    /// literal-5) for a reserved word is named beside that word, since the user wrote a word §2023:8.9 does not list and "reserved
    /// in COBOL-2023" would be false of it (ISO §2023:7.3.10.4 GR2, GR4; §2023:8.3.2.1 rule 1; kb/Work PB4488). The decision is the
    /// map's, <see cref="CobolWordsMap.SynonymOf"/>, so a synonym the lexer retyped to its original's token (whose text
    /// is the original's) and one it left an identifier are one case (kb/Work PB4573).</param>
    public static string UserWordViolationMessage(string written, CobolWordsMap words, int edition)
        => words.SynonymOf(written) is not { } original
            ? $"'{written}' is a reserved word in COBOL-{edition} and cannot be used as a "
              + "user-defined word (ISO §2023:8.9)"
            : $"'{written}' is a synonym for the reserved word {original} (>>COBOL-WORDS, ISO §2023:7.3.10.4 GR2, GR4) and cannot be "
              + "used as a user-defined word (ISO §2023:8.3.2.1 1)";
}
