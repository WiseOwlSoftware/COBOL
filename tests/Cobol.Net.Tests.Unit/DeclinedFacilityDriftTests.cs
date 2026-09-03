// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard for the DECLINED-OPTIONAL-ELEMENT surface (ISO Annex A.4.1). The whole point of putting the
/// declined constructs in ONE grammar fragment behind ONE pass was to make the next one automatic — but
/// "automatic" is a claim, and an unguarded claim rots. These facts derive the obligation FROM the grammar file
/// so a new declined construct cannot parse into silence.
///
/// <para>⛔ WHY THIS SHAPE AND NOT A LIST. A hand-maintained "declined constructs" list would be a sixth work
/// register (CLAUDE.md rule 8) AND would drift the moment someone added a grammar rule without touching it. The
/// grammar file IS the list; this test reads it.</para>
/// </summary>
public sealed class DeclinedFacilityDriftTests
{
    private static string GrammarPath => TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolDeclined.g4");
    private static string PassPath => TestRepo.Src("Cobol.Net.Compiler", "Validation", "DeclinedFacilityPass.cs");

    /// <summary>Every top-level parser rule defined in <c>CobolDeclined.g4</c>, in file order.</summary>
    private static List<string> GrammarRules()
    {
        string text = File.ReadAllText(GrammarPath);
        // A rule definition is a name at the START of a line followed (possibly on the next line) by ':'.
        // Comment lines start with '//' and never begin at column 0 with an identifier followed by a colon.
        var rules = new List<string>();
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"^([a-z][A-Za-z0-9_]*)\s*$");
            if (!m.Success) continue;
            // The next non-blank line must open the rule body with ':'.
            for (int j = i + 1; j < lines.Length; j++)
            {
                string next = lines[j].Trim();
                if (next.Length == 0) continue;
                if (next.StartsWith(':')) rules.Add(m.Groups[1].Value);
                break;
            }
        }
        return rules;
    }

    /// <summary>The ENTRY POINTS: rules no OTHER rule in the same file references. A rule reached from a
    /// diagnosed parent needs no override of its own (the parent already refused the whole construct and does
    /// not descend); a rule reachable only from OUTSIDE the file is a construct the compiler will parse and
    /// must therefore refuse by name.</summary>
    private static List<string> EntryPointRules()
    {
        string text = File.ReadAllText(GrammarPath);
        var rules = GrammarRules();
        // Strip line comments so a rule NAMED in prose does not count as a reference.
        string body = Regex.Replace(text, @"//[^\n]*", "");
        var entries = new List<string>();
        foreach (string r in rules)
        {
            // References = occurrences of the name that are not its own definition line.
            int uses = Regex.Matches(body, $@"\b{Regex.Escape(r)}\b").Count;
            int defs = Regex.Matches(body, $@"(?m)^{Regex.Escape(r)}\s*$").Count;
            if (uses - defs == 0) entries.Add(r);
        }
        return entries;
    }

    [Fact]
    public void GrammarFile_DefinesRules_AndSomeAreEntryPoints()
    {
        var rules = GrammarRules();
        var entries = EntryPointRules();
        // The population assertion (feedback_verdict_evidence_invariant): a parse that found nothing would make
        // every other fact below vacuously green.
        Assert.True(rules.Count >= 8, $"CobolDeclined.g4 parsed to only {rules.Count} rules — the rule scanner "
            + "is broken, and every obligation derived from it would be vacuously satisfied");
        Assert.True(entries.Count >= 3, $"only {entries.Count} entry-point rule(s) found — expected at least "
            + "validationClause, validateValidPhrase and applyCommitClause");
    }

    /// <summary>⛔ THE OBLIGATION. Every entry-point rule of the declined-facility grammar has a
    /// <c>VisitXxx</c> override in <see cref="CobolNet.Validation.DeclinedFacilityPass"/>. Adding a declined
    /// construct's SYNTAX without adding its REFUSAL would make the compiler PARSE the construct and then
    /// silently ignore it — strictly worse than the generic parse error it replaced, because the program would
    /// compile and run with the declined semantics simply absent (the new-construct skill's "parsing something
    /// and emitting a no-op is worse than a compile error").</summary>
    [Fact]
    public void EveryEntryPointRule_HasARefusalOverride()
    {
        string pass = File.ReadAllText(PassPath);
        var missing = EntryPointRules()
            .Where(r => !pass.Contains($"Visit{char.ToUpperInvariant(r[0])}{r[1..]}(", StringComparison.Ordinal))
            .ToList();
        Assert.True(missing.Count == 0,
            "declined-facility grammar rule(s) with NO refusal override in DeclinedFacilityPass — the compiler "
            + "would parse the construct and ignore it: " + string.Join(", ", missing));
    }

    /// <summary>Prove the guard above can FAIL (feedback_green_gates_arent_evidence): a rule name that is NOT
    /// overridden must be reported. Run against a fabricated name rather than by mutating the real file.</summary>
    [Fact]
    public void TheRefusalObligation_CanFail()
    {
        string pass = File.ReadAllText(PassPath);
        Assert.DoesNotContain("VisitNoSuchDeclinedClause(", pass, StringComparison.Ordinal);
    }

    /// <summary>The three declined-facility diagnostics are ERRORS, not warnings — the distinction that makes
    /// the whole band different from the §4.2.6 processor-dependent band (COBOLNET1578/1579/1580, Warning).
    /// Annex A.4.1: an implementation "shall accept the syntax … for an optional element ONLY when support for
    /// that language element is claimed", so accepting a declined element's syntax is itself the
    /// non-conformance. It is also what makes the rows WITNESSABLE: the negative corpus asserts a failing
    /// compile, and the whole 1560/1578/1579/1580 warning band has no assertion mechanism at all — the reason
    /// every A.4.2 screen row in kb/Work PB260 is still open.</summary>
    [Fact]
    public void TheDeclinedBand_IsErrorSeverity_AndSharesOneSuppressKey()
    {
        foreach (var d in new[]
                 {
                     DiagnosticCatalog.ValidateDataDivisionClauseUnsupported,
                     DiagnosticCatalog.ApplyCommitClauseUnsupported,
                     DiagnosticCatalog.DeclinedModuleExceptionName,
                 })
        {
            Assert.Equal(Editions.EditionSeverity.Error, d.Severity);
            Assert.Equal(DiagnosticCatalog.DeclinedOptionalElement, d.ResolvedSuppressKey);
        }
    }

    /// <summary>The three codes are 1708/1709/1710 and nothing else carries them — the allocation this cluster
    /// claimed. A collision is how the shipped COBOLNET1573 two-meanings defect happened.</summary>
    [Fact]
    public void TheDeclinedBand_OwnsItsCodesExclusively()
    {
        foreach (string code in new[] { "COBOLNET1708", "COBOLNET1709", "COBOLNET1710" })
            Assert.Single(DiagnosticCatalog.All, d => d.Code == code);
    }
}
