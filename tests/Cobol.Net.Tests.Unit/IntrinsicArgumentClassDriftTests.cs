// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the ISO §15.3 argument-class table WIRED IN (fix-queue PB1).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE DEFECT THIS GUARDS AGAINST ALREADY HAPPENED ONCE, AND WAS INVISIBLE FOR THE WHOLE LIFE OF THE FEATURE.
/// <c>IntrinsicCatalog</c> declared an <c>ArgKinds</c> class code on all 79 of its rows, and
/// <c>IntrinsicSig.ArgKind(int)</c> existed to read it — with ZERO callers. The table looked complete, every row
/// looked maintained, and no §15 argument rule was enforced from it: <c>FUNCTION REVERSE</c> over a numeric item
/// and <c>FUNCTION ABS</c> over an alphanumeric one both compiled clean and produced garbage. Nothing failed,
/// because a declaration nobody reads cannot fail.
/// </para>
/// <para>
/// A dead lookup is the hardest kind of defect to notice — it presents as thorough, well-maintained data. So the
/// wiring itself is asserted here rather than assumed, which is the half of CLAUDE.md rule 5 that makes the
/// restructuring stick: "pair it with a drift test so 'automatic' stays true".
/// </para>
/// </remarks>
public sealed class IntrinsicArgumentClassDriftTests
{
    private static string CatalogSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));

    private static string BinderSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));

    private static string RulesSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicArgumentRules.cs"));

    /// <summary>Every <c>Add(new("NAME", …, "&lt;argkinds&gt;", …))</c> row, as (function, declared kinds).</summary>
    private static List<(string Name, string Kinds)> CatalogRows()
    {
        // ⚠ The arity bounds are NOT always literals — a variadic row writes `inf` for MaxArgs. An earlier
        // version of this pattern required digits and silently skipped every variadic function, which is how a
        // guard against a dead table becomes a dead guard. Caught by
        // EveryVerifiedRule_NamesARealFunction_WithAHandledCode reporting ORD-MAX as absent from the catalog.
        var rx = new Regex(
            "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,"
            + "\\s*[-\\w]+,\\s*[-\\w]+,\\s*\"(?<k>[a-z ]*)\"",
            RegexOptions.Compiled);
        return [.. rx.Matches(CatalogSource()).Select(m => (m.Groups["n"].Value, m.Groups["k"].Value))];
    }

    [Fact]
    public void TheCatalog_StillDeclaresArgumentKinds()
    {
        var rows = CatalogRows();
        // A floor, not the exact count: adding functions must not fail this, but a parser that silently stops
        // matching (a formatting change to the rows) must.
        Assert.True(rows.Count >= 79,
            $"only {rows.Count} catalog rows parsed — the Add(new(...)) shape changed and this guard has gone "
            + "blind; fix the regex, do not lower the floor.");
    }

    /// <summary>
    /// ⛔ THE CENTRAL ASSERTION: <c>ArgKind</c> is CALLED. This is the exact fact that was false for the whole
    /// life of the catalog, and the one that cannot be inferred from the data looking well-maintained.
    /// </summary>
    [Fact]
    public void TheArgumentClassScreen_IsActuallyWiredIn()
    {
        string binder = BinderSource();
        Assert.True(binder.Contains("CheckArgumentClasses", StringComparison.Ordinal),
            "the ISO §15.3 argument-class screen is gone from IntrinsicBinder — every catalogued function's "
            + "argument rule is unenforced again (fix-queue PB1).");
        Assert.True(binder.Contains("IntrinsicArgumentRules.Verified", StringComparison.Ordinal),
            "IntrinsicBinder no longer consults IntrinsicArgumentRules.Verified. The `ArgKinds == \"p\"` "
            + "polymorphism test does NOT count as enforcement: it reads the whole string for one function "
            + "family and screens nothing — that is precisely the state PB1 found.");
    }

    /// <summary>
    /// ⛔ EVERY SCREENED FUNCTION CITES THE CLAUSE ITS RULE COMES FROM. This is the guard that keeps the table
    /// spec-derived rather than guessed, which is the distinction that cost 12 legal corpus programs to learn:
    /// the catalog's own <c>ArgKinds</c> hint column is UNAUDITED (BYTE-LENGTH declares "s" where §15.14.3 admits
    /// any class), so screening from it rejected valid COBOL.
    /// </summary>
    [Fact]
    public void EveryVerifiedRule_CitesItsClause()
    {
        string rules = RulesSource();
        int at = rules.IndexOf("Verified =", StringComparison.Ordinal);
        Assert.True(at > 0, "IntrinsicArgumentRules.Verified is gone");
        string table = rules[at..rules.IndexOf("};", at, StringComparison.Ordinal)];

        var entries = Regex.Matches(table, """\["(?<f>[A-Z0-9-]+)"\]\s*=\s*\('(?<k>.)',\s*"(?<c>[^"]*)"\)""");
        Assert.True(entries.Count >= 11, $"only {entries.Count} verified rules parsed — the table shape changed "
            + "and this guard has gone blind; fix the regex, do not lower the floor.");

        var uncited = entries.Where(m => !m.Groups["c"].Value.Contains('§'))
            .Select(m => m.Groups["f"].Value).ToList();
        Assert.True(uncited.Count == 0,
            $"verified argument rule(s) with no ISO clause: [{string.Join(", ", uncited)}]. An entry here is a "
            + "spec-derived fact and must carry the § it was read from — an uncited one is a guess that rejects "
            + "legal source.");
    }

    /// <summary>A screened function is one the catalog actually has, and its code is one the screen handles.</summary>
    [Fact]
    public void EveryVerifiedRule_NamesARealFunction_WithAHandledCode()
    {
        string rules = RulesSource();
        int at = rules.IndexOf("Verified =", StringComparison.Ordinal);
        string table = rules[at..rules.IndexOf("};", at, StringComparison.Ordinal)];
        var catalogNames = CatalogRows().Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var handled = new HashSet<char>(Regex.Matches(RulesSource(), @"'(?<c>[a-z])' =>")
            .Select(m => m.Groups["c"].Value[0])) { 'p', ' ' };

        foreach (Match m in Regex.Matches(table, """\["(?<f>[A-Z0-9-]+)"\]\s*=\s*\('(?<k>.)'"""))
        {
            string fn = m.Groups["f"].Value;
            char code = m.Groups["k"].Value[0];
            Assert.True(catalogNames.Contains(fn),
                $"verified rule names FUNCTION {fn}, which is not in IntrinsicCatalog — the screen can never fire");
            Assert.True(handled.Contains(code),
                $"FUNCTION {fn} is verified with code '{code}', which Admissible does not handle — silently "
                + "unscreened, the dead-table failure in miniature");
        }
    }

    /// <summary>The three PB1 negative fixtures exist and are REGISTERED — an unregistered golden never runs.</summary>
    [Fact]
    public void ThePb1NegativeFixtures_ExistAndAreRegistered()
    {
        string manifest = File.ReadAllText(
            TestRepo.Tests("conformance", "negative", "manifest.json"));
        foreach (string name in new[]
                 {
                     "pb1-numeric-arg-alphanumeric",       // 's'-shaped rule violated by a numeric operand
                     "pb1-string-arg-numeric",             // 'n'-shaped rule violated by an alphanumeric operand
                     "pb1-numeric-arg-numeric-edited",     // the §8.5.2.1 Table-2 row that reads the other way
                 })
        {
            Assert.True(File.Exists(TestRepo.Tests("conformance", "negative", name + ".cob")), $"{name}.cob missing");
            Assert.True(File.Exists(TestRepo.Tests("conformance", "negative", name + ".err")), $"{name}.err missing");
            Assert.True(manifest.Contains(name, StringComparison.Ordinal),
                $"{name} is not in tests/conformance/negative/manifest.json — it would never run");
        }
    }

    /// <summary>
    /// The diagnostic cites the clauses the screen actually implements, and says CLASS — because the whole
    /// defect turns on §8.5.2.1 Table 2 being a CLASS table, not a category one.
    /// </summary>
    [Fact]
    public void TheDiagnostic_CitesTheGoverningClauses()
    {
        string catalog = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Editions", "Diagnostics", "DiagnosticCatalog.cs"));
        int at = catalog.IndexOf("COBOLNET1627", StringComparison.Ordinal);
        Assert.True(at > 0, "COBOLNET1627 (intrinsic-argument-class) is not in the catalog");
        string block = catalog[at..Math.Min(catalog.Length, at + 2400)];
        foreach (string cite in new[] { "§15.3", "§8.5.2.1", "§4.2.2" })
            Assert.True(block.Contains(cite, StringComparison.Ordinal), $"COBOLNET1627 no longer cites {cite}");
    }
}
