// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ AN EDITION GATE KEYED ON A BOUND NODE'S SHAPE IS SILENTLY UN-GATED ON EVERY PATH THAT BAILS OUT BEFORE THE
/// NODE IS BUILT (kb/Work PB353). <c>VersionConformancePass</c> is TWO arms — a parse-tree arm that fires on the
/// construct's RECOGNITION and a bound-tree arm that fires on a RESOLVED fact — and the arm a gate lives in is
/// the whole question: a recognition gate cannot be dropped by any binder, while a bound-arm gate is dropped by
/// every <c>BoundUnsupported</c> / <c>BoundNop</c> return that precedes its node. Five gates were on the wrong
/// arm, and the compiler measured this way at <c>--std 85</c> before the correction:
/// <list type="bullet">
/// <item><c>START NOSUCHF FIRST</c> → COBOLNET1639 alone. No edition named.</item>
/// <item><c>START SDF FIRST</c> (an SD) → COBOLNET1692 alone.</item>
/// <item><c>START IXF KEY = NOSUCHK WITH LENGTH 3</c> → COBOLNET1639 alone.</item>
/// <item><c>READ NOSUCHF PREVIOUS ADVANCING ON LOCK</c> → COBOLNET1639 alone; TWO 2002 phrases dropped.</item>
/// <item><c>INVOKE NOSUCHO "M1"</c> → COBOLNET0823 alone.</item>
/// </list>
/// Each of those five keyed on a bound member (<c>Mode</c>, <c>Length</c>, <c>Kind</c>, <c>AdvancingOnLock</c>,
/// the node type) that the binder assigns from a PARSE fact and nothing else — so the bound-tree home bought no
/// precision and cost the diagnostic. A per-edition compiler that accepts at COBOL-85 a construct it rejects at
/// COBOL-85 one undeclared name away is not four compilers; it is one with a hole.
/// <para><b>Two tests, one adjudicated table, and they have to agree.</b> The STATIC half derives both arms'
/// construct ids from the pass's own source and requires every bound-arm id to name the RESOLVED FACT that makes
/// a parse-arm home impossible — so a future gate cannot be classified by habit. The BEHAVIOURAL half proves the
/// property the classification is FOR: it takes every gated construct's own <c>constructs.json</c> program,
/// breaks every PROCEDURE-DIVISION reference to a declared name so the bind must bail, and requires the edition
/// diagnostic to survive. The exempt rows are exactly the ones whose gate legitimately depends on a fact the
/// broken program no longer supplies.</para>
/// <para>⛔ WHY THIS IS A GATE AND NOT A GREEN TICK (feedback_green_gates_arent_evidence). The behavioural half
/// asserts its own POPULATION — a mutation that stopped biting, or a catalogue whose sources lost their
/// operands, would otherwise turn the whole theory into a green that measured nothing. It also asserts that the
/// mutant actually reached the BINDER (an undefined-name error is present), so a mutant that merely failed to
/// PARSE cannot be mistaken for a surviving gate.</para>
/// </summary>
public sealed class EditionGateArmDriftTests
{
    // ── The adjudicated bound-arm set ─────────────────────────────────────────────────────────────────────────
    // Every construct id the BOUND arm Checks, with the resolved fact that keeps it there. An id here is a claim
    // that no parse rule identifies the construct — adding one is an adjudication, not a formality, and the two
    // tests below read the SAME table so a wrong entry cannot pass the static half and hide from the behavioural
    // one. The five ids that used to be here (Invoke2002, ReadPrevious2002, RecordLockPhrase2002,
    // StartFirstLast2002, StartWithLength2002) are gone because none of them had an answer to fill in.
    private static readonly Dictionary<string, string> BoundArmResolvedFact = new(StringComparer.Ordinal)
    {
        ["FunctionPrototype2002"] =
            "BoundUnit.IsPrototype — a FUNCTION-ID paragraph's IS PROTOTYPE marking is settled while binding the "
            + "unit's identification division, and the gate runs per bound unit, not per parse node (Step 14g.5)",
        ["SetObjectReference2002"] =
            "the Format-5 RE-ROUTE: `SET a TO b` reaches OoBinder as an object-reference SET only when both "
            + "operands RESOLVE to PicCategory.ObjectReference — setObjectReferenceStatement is one spelling of "
            + "several, and the generic setToValueStatement carries the rest (ISO §14.9.39 Format 5)",
        ["PointerArithmetic2002"] =
            "SET x UP/DOWN BY n is ONE printed shape for TWO constructs — the version-invariant index form "
            + "(§14.9.39 Format 8) and the 2002 pointer form (Format 10). Only the operand's resolved USAGE "
            + "separates them, so recognition would gate every SET of an index",
        ["SetDynLengthSize2023"] =
            "the bare `SET item TO n` re-routes to BoundSetSize only when the target resolves to a DYNAMIC LENGTH "
            + "item (§14.9.39 Format 16); the explicit SIZE OF spelling and the re-routed one share one node and "
            + "must share one gate",
        // The USAGE / PICTURE-category family (Step 14g.1): one gate per RESOLVED DataItem, keyed on
        // (OwnUsage, Pic.Category, Pic.Usage). A parse-arm home cannot work for any of them — a group header
        // sheds its PICTURE, TYPE / SAME AS / TYPEDEF copy a usage in from another entry entirely, and the
        // recovered category of an unimplemented skeleton erases the written keyword.
        ["NationalData2002"] = UsageFact("PIC N / USAGE NATIONAL"),
        ["BooleanData2002"] = UsageFact("PIC 1 / USAGE BIT"),
        ["UsagePointer2002"] = UsageFact("USAGE POINTER"),
        ["UsageProgramPointer2002"] = UsageFact("USAGE PROGRAM-POINTER"),
        ["UsageFunctionPointer2014"] = UsageFact("USAGE FUNCTION-POINTER"),
        ["UsageObjectReference2002"] = UsageFact("USAGE OBJECT REFERENCE"),
        ["UsageBinaryCharFamily2002"] = UsageFact("USAGE BINARY-CHAR/-SHORT/-LONG/-DOUBLE"),
        ["UsageFloatShort2002"] = UsageFact("USAGE FLOAT-SHORT"),
        ["UsageFloatLong2002"] = UsageFact("USAGE FLOAT-LONG"),
        ["UsageFloatExtended2002"] = UsageFact("USAGE FLOAT-EXTENDED"),
        ["UsageFloatBinary322014"] = UsageFact("USAGE FLOAT-BINARY-32"),
        ["UsageFloatBinary642014"] = UsageFact("USAGE FLOAT-BINARY-64"),
        // The MOVE figurative-constant category rows (ISO §14.9.25.3 SR5): which of the three edition rows a
        // MOVE falls under depends on the source figurative crossed with EACH receiver's resolved PICTURE, and a
        // MOVE's receiver list is a parse fact only in its spelling.
        ["MoveAllDigitIntegerObsolete2023"] = MoveFact("ALL <digit-literal> into an integer receiver"),
        ["MoveQuoteNumericObsolete2014"] = MoveFact("QUOTE into a numeric receiver"),
        ["MoveAlphanumericFigurativeRemoved2023"] = MoveFact("an alphanumeric figurative into a numeric receiver"),
        ["PicExternalFloat2002"] =
            "PicInfo.IsFloatEdited — the external-floating-point PICTURE is identified by the ANALYSED character "
            + "string (the E and its exponent), not by any token the parse tree distinguishes from an ordinary "
            + "picture-string (Step 14g.5)",
    };

    private static string MoveFact(string shape) =>
        $"the bound MOVE's source figurative crossed with EACH receiver's RESOLVED picture — {shape} is a "
        + "category question, and the same written statement falls under a different edition row (or none) "
        + "depending on what its receivers turn out to be";

    private static string UsageFact(string spelling) =>
        $"the RESOLVED DataItem's (OwnUsage, Pic.Category, Pic.Usage) — {spelling} reaches an item through TYPE / "
        + "SAME AS / TYPEDEF as readily as through its own written clause, and a group header sheds its PICTURE, "
        + "so the written keyword is not where the construct lives";

    /// <summary>Construct ids Checked from BOTH arms, with the reason the two occurrences are DIFFERENT
    /// constructs sharing one edition row. Anything else appearing in both arms is the duplicate the pass's
    /// own "a Check for any one construct fires from EXACTLY one arm" invariant forbids — which is how the READ
    /// … ADVANCING ON LOCK gate sat on the bound arm while its two printed siblings gated on recognition.</summary>
    private static readonly Dictionary<string, string> AdjudicatedDualArm = new(StringComparer.Ordinal)
    {
        ["NationalData2002"] =
            "two occurrences: a national LITERAL as a procedure-division statement operand (parse arm) and a "
            + "national DATA ITEM's resolved usage (bound arm). Gating the literal from the data arm would miss "
            + "it; gating the item from the parse arm would double the diagnostic on its own VALUE clause",
        ["BooleanData2002"] = "the boolean twin of NationalData2002 — same literal/item split, same reason",
    };

    // ── The static half ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The pass source split into (bound arm, parse arm) — the ParseArm nested class is the parse arm,
    /// everything else is the bound arm. Brace-matched rather than line-numbered so the split cannot rot.</summary>
    private static (string Bound, string Parse) Arms()
    {
        string src = File.ReadAllText(TestRepo.Src(Path.Combine(
            "Cobol.Net.Compiler", "Validation", "VersionConformancePass.cs")));
        int start = src.IndexOf("private sealed class ParseArm", StringComparison.Ordinal);
        Assert.True(start >= 0, "VersionConformancePass no longer declares a ParseArm — the two-arm split this "
            + "test measures has been restructured; re-derive the arms before trusting either assertion.");
        int i = src.IndexOf('{', start);
        int depth = 0, j = i;
        for (; j < src.Length; j++)
        {
            if (src[j] == '{') depth++;
            else if (src[j] == '}' && --depth == 0) break;
        }
        int end = j + 1;
        return (src[..start] + src[end..], src[start..end]);
    }

    private static HashSet<string> ConstructIds(string region) =>
        [.. Regex.Matches(region, @"Constructs\.([A-Za-z0-9_]+)").Select(m => m.Groups[1].Value)];

    /// <summary>Every construct the BOUND arm gates names the resolved fact that keeps it off the parse arm, and
    /// the table carries nothing else — an entry whose gate has moved is deleted with it.</summary>
    [Fact]
    public void EveryBoundArmGate_NamesTheResolvedFactItNeeds()
    {
        var (bound, parse) = Arms();
        var boundIds = ConstructIds(bound);
        Assert.NotEmpty(boundIds);
        Assert.NotEmpty(ConstructIds(parse));

        var unadjudicated = boundIds.Where(id => !BoundArmResolvedFact.ContainsKey(id))
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.True(unadjudicated.Count == 0,
            "bound-arm edition gate(s) with no resolved fact named: " + string.Join(", ", unadjudicated)
            + ". A gate belongs on the bound arm only when a parse rule cannot identify the construct — otherwise "
            + "every binder bail-out that precedes the node drops the diagnostic (kb/Work PB353). Move it to "
            + "VersionConformancePass.ParseArm, or add it to BoundArmResolvedFact with the fact it needs.");

        var orphans = BoundArmResolvedFact.Keys.Where(id => !boundIds.Contains(id))
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.True(orphans.Count == 0,
            "BoundArmResolvedFact entr(ies) for gate(s) the bound arm no longer Checks: " + string.Join(", ", orphans)
            + " — delete them, or the table becomes a record of what USED to be true (a dead lookup is also an "
            + "unverified one).");
    }

    /// <summary>One construct id fires from ONE arm, unless the two occurrences are adjudicated as different
    /// constructs. The pass documents this invariant; until kb/Work PB353 it was broken by
    /// RecordLockPhrase2002, whose three printed spellings were split two-parse / one-bound.</summary>
    [Fact]
    public void NoConstruct_FiresFromBothArms_ExceptByAdjudication()
    {
        var (bound, parse) = Arms();
        var both = ConstructIds(bound).Intersect(ConstructIds(parse))
            .OrderBy(x => x, StringComparer.Ordinal).ToList();

        var undeclared = both.Where(id => !AdjudicatedDualArm.ContainsKey(id)).ToList();
        Assert.True(undeclared.Count == 0,
            "construct(s) gated from BOTH arms with no adjudication: " + string.Join(", ", undeclared)
            + ". Two arms for one construct means one of them is dead on some path — pick the arm, or record why "
            + "the occurrences are different constructs in AdjudicatedDualArm.");

        var stale = AdjudicatedDualArm.Keys.Where(id => !both.Contains(id))
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0,
            "AdjudicatedDualArm entr(ies) that are no longer dual-arm: " + string.Join(", ", stale));
    }

    // ── The behavioural half ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>An introduction gate whose construct sits in the PROCEDURE DIVISION, paired with the highest
    /// edition that must still reject it, for every catalogue row whose program can be broken at all. Rows whose
    /// gate is adjudicated bound-arm are EXCLUDED rather than asserted-and-skipped: their gate legitimately
    /// depends on a resolved fact the broken program no longer supplies, and a theory row that asserts nothing
    /// is a green that measured nothing.</summary>
    public static IEnumerable<object[]> BreakableGatedRows()
    {
        foreach (var c in VersionMatrixCatalogue.Active)
        {
            if (c.IntroducedIn <= 85 || (c.ExpectDiagnosticBelow is null && c.ExpectDiagnostic is null)) continue;
            if (IsAdjudicatedBoundArm(c.Id)) continue;
            if (BreakOperands(c.Source) is null) continue;
            int below = EditionHarness.Editions.Where(v => v < c.IntroducedIn).Max();
            yield return [c.Id, below];
        }
    }

    /// <summary>⛔ THE GATE MUST SURVIVE THE BIND. Every gated construct's own catalogue program, with every
    /// PROCEDURE-DIVISION reference to a declared name broken so the binder cannot resolve it, still names its
    /// edition at the edition below its introduction — because that is the difference between four per-edition
    /// compilers and one compiler with a hole. The exempt rows are the bound-arm ones whose gate depends on a
    /// resolved fact the broken program genuinely no longer supplies.</summary>
    [Theory]
    [MemberData(nameof(BreakableGatedRows))]
    public void GatedConstruct_NamesItsEdition_EvenWhenItsOperandsDoNotResolve(string constructId, int edition)
    {
        var c = VersionMatrixCatalogue.ById[constructId];
        string mutant = BreakOperands(c.Source)!;

        var (ok, diagnostics) = EditionHarness.Compile(mutant, edition);
        Assert.False(ok, $"[{constructId}] every name it references is undefined and it compiled at COBOL-{edition}");

        string code = (c.ExpectDiagnosticBelow ?? c.ExpectDiagnostic)!;
        Assert.True(diagnostics.Any(d => d.Contains(code, StringComparison.Ordinal)),
            $"[{constructId}] is a COBOL-{c.IntroducedIn} introduction and its own catalogue program named "
            + $"{code} at COBOL-{edition} — but with its operands broken so the bind bails, the edition went "
            + $"unnamed. That is a per-edition hole, not a diagnostic-quality nicety: the SAME construct is "
            + $"gated one resolvable name away (kb/Work PB353).\n{string.Join("\n", diagnostics)}"
            + $"\n--- source ---\n{mutant}");
    }

    /// <summary>⛔ THE PROBE'S OWN LIVENESS, ASSERTED ONCE OVER THE POPULATION (feedback_prove_the_watchdog_fails).
    /// The theory above is only evidence about bail-out paths if the broken programs actually REACH the binder —
    /// a few halt earlier by their nature (a below-edition <c>&gt;&gt;</c>directive is refused in the
    /// preprocessor; a PROCEDURE DIVISION USING operand is screened in the header), and if that became true of
    /// ALL of them the theory would still be green while measuring nothing. So: most broken programs must draw
    /// the undefined-name error that proves a binder bail-out was taken.</summary>
    [Fact]
    public void TheBrokenPrograms_ReachTheBinder()
    {
        var reached = new List<string>();
        var missed = new List<string>();
        foreach (object[] row in BreakableGatedRows())
        {
            string id = (string)row[0];
            var (_, diagnostics) = EditionHarness.Compile(
                BreakOperands(VersionMatrixCatalogue.ById[id].Source)!, (int)row[1]);
            (diagnostics.Any(d => d.Contains("COBOLNET1639", StringComparison.Ordinal)) ? reached : missed).Add(id);
        }

        Assert.True(reached.Count >= 80,
            $"only {reached.Count} of {reached.Count + missed.Count} broken programs drew an undefined-name error, "
            + "so the mutation has stopped exercising binder bail-out paths and the survival theory is measuring "
            + "nothing. Not reached: " + string.Join(", ", missed));
    }

    /// <summary>Whether a catalogue row's gate is one the static half adjudicated onto the BOUND arm — the
    /// construct is identified by a RESOLVED fact, so a broken program stops carrying it. Derived from
    /// <see cref="BoundArmResolvedFact"/>, the same table the static half enforces, through the kebab→Pascal
    /// spelling <c>scripts/gen-constructs.ps1</c> uses, so the two halves cannot drift apart.</summary>
    private static bool IsAdjudicatedBoundArm(string constructId) =>
        BoundArmResolvedFact.ContainsKey(string.Concat(constructId.Split('-')
            .Select(s => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..])));

    /// <summary>The POPULATION assertion. A mutation that stopped biting turns the theory above into a green
    /// that measured nothing, and the theory itself cannot say so — an empty MemberData is a PASS.</summary>
    [Fact]
    public void TheSurvivalTheory_CoversTheGatedProcedureDivisionPopulation()
    {
        var rows = BreakableGatedRows().Select(r => (string)r[0]).ToList();
        Assert.True(rows.Count >= 90,
            $"only {rows.Count} catalogue rows could be broken — the mutation has stopped reaching the binder, or "
            + "the catalogue's sources have lost their declared-name operands. Re-derive BreakOperands before "
            + "trusting a green run.");
        // The five gates kb/Work PB353 moved are IN the population, not merely adjacent to it.
        foreach (string id in new[] { "start-first-last-2002", "start-with-length-2002", "read-previous-2002",
                                      "record-lock-phrase-2002", "invoke-2002" })
        {
            Assert.True(VersionMatrixCatalogue.ById.ContainsKey(id), $"catalogue row '{id}' has disappeared");
            Assert.Contains(id, rows);
        }
    }

    // ── The mutation ──────────────────────────────────────────────────────────────────────────────────────────

    private const string BreakSuffix = "Z9";
    private static readonly Regex SelectName =
        new(@"\bSELECT\s+(?:OPTIONAL\s+)?([A-Za-z][A-Za-z0-9-]*)", RegexOptions.IgnoreCase);
    private static readonly Regex LevelName =
        new(@"(?m)^\s*(?:0?[1-9]|[1-4][0-9]|66|77|88)\s+([A-Za-z][A-Za-z0-9-]*)");

    /// <summary>Rename every PROCEDURE-DIVISION reference to a name the ENVIRONMENT/DATA divisions declare, so
    /// the binder must bail on it, leaving the declarations and every keyword untouched — the parse tree keeps
    /// its exact SHAPE, which is the point: a recognition gate reads the shape and a bound-arm gate reads what
    /// the shape resolves to. Returns null when the program has no procedure division or no such reference (a
    /// data-division construct, an OO class), which is a SKIP and not a pass.</summary>
    internal static string? BreakOperands(string source)
    {
        int pd = source.IndexOf("PROCEDURE DIVISION", StringComparison.OrdinalIgnoreCase);
        if (pd < 0) return null;
        string head = source[..pd], body = source[pd..];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in SelectName.Matches(head)) names.Add(m.Groups[1].Value);
        foreach (Match m in LevelName.Matches(head))
        {
            string n = m.Groups[1].Value;
            if (!n.Equals("FILLER", StringComparison.OrdinalIgnoreCase)) names.Add(n);
        }
        // §8.3.2.1 caps a COBOL word; a rename that would breach the cap would draw a WORD-LENGTH error instead
        // of the undefined-name one this mutation is for, so those names are left alone.
        names.RemoveWhere(n => n.Length + BreakSuffix.Length > 31);
        if (names.Count == 0) return null;

        var pattern = new Regex(@"(?<![A-Za-z0-9-])(" + string.Join("|", names.Select(Regex.Escape))
            + @")(?![A-Za-z0-9-])", RegexOptions.IgnoreCase);

        bool changed = false;
        var sb = new StringBuilder(head);
        foreach (string line in body.Split('\n'))
        {
            string trimmed = line.TrimStart();
            // Comment lines carry no references; a fixed-form indicator '*' or the free-form '*>' both start one.
            if (trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                sb.Append(line).Append('\n');
                continue;
            }
            // Rewrite only OUTSIDE literals — a name inside a quoted literal is character data, and renaming it
            // would change what the program PRINTS rather than what it references.
            var outLine = new StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                char q = line[i];
                if (q is '"' or '\'')
                {
                    int close = line.IndexOf(q, i + 1);
                    if (close < 0) { outLine.Append(line[i..]); i = line.Length; break; }
                    outLine.Append(line[i..(close + 1)]);
                    i = close + 1;
                    continue;
                }
                int next = line.IndexOfAny(['"', '\''], i);
                string chunk = next < 0 ? line[i..] : line[i..next];
                string rewritten = pattern.Replace(chunk, m => m.Groups[1].Value + BreakSuffix);
                if (rewritten != chunk) changed = true;
                outLine.Append(rewritten);
                i = next < 0 ? line.Length : next;
            }
            sb.Append(outLine).Append('\n');
        }

        // The trailing '\n' Split/Append round-trip adds one newline for a source that did not end in one.
        string result = sb.ToString();
        if (!source.EndsWith('\n') && result.EndsWith('\n')) result = result[..^1];
        return changed ? result : null;
    }
}
