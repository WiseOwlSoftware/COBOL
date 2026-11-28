// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A DIAGNOSTIC ABOUT AN INTRINSIC FUNCTION CITES THE RULE OF THE EDITION IT RUNS AT (kb/Work PB6118, PB4950): every §2023:15
/// citation the binder spells goes through <see cref="IntrinsicCitations"/>, whose 2002 and 2014 counterparts are
/// <see cref="IntrinsicCitationConcordance"/>, written by <c>scripts/spec/gen_intrinsic_citation_concordance.py</c> from the three
/// transcriptions (a rule found by its wording). This class holds the three together: the table is what the generator prints now, no
/// message of <c>IntrinsicBinder.cs</c> spells a §2023:15 rule outside the one call, and a row's editions agree with the function
/// catalog's own introduction edition.
/// </summary>
public sealed class IntrinsicCitationConcordanceDriftTests
{
    [Fact]
    public void TheTable_IsWhatTheGeneratorPrints()
    {
        var r = PythonInstrument.Run(ObservationBudget.OneProcess, TestRepo.Scripts("spec", "gen_intrinsic_citation_concordance.py"), "--check");
        Assert.True(r.ExitCode == 0,
            $"IntrinsicCitationConcordance.cs is not what the generator prints: run python scripts/spec/gen_intrinsic_citation_concordance.py\n{r.Stdout}{r.Stderr}");
    }

    /// <summary>A row's function, and the editions it exists in, agree with <see cref="IntrinsicCatalog"/>: a function of 2002 has
    /// a 2002 citation, one of 2014 has none at 2002, one of 2023 has none at 2002 or 2014; and each citation names its own edition.</summary>
    [Fact]
    public void EveryRow_AgreesWithTheCatalogsIntroductionEdition_AndNamesItsOwnEdition()
    {
        Assert.True(IntrinsicCitationConcordance.Rows.Count > 100, "the concordance is nearly empty");
        foreach (var (key, row) in IntrinsicCitationConcordance.Rows)
        {
            var m = IntrinsicCitations.Cite2023.Match("§2023:" + key);
            Assert.True(m.Success && m.Length == key.Length + "§2023:".Length && IntrinsicCitations.CanonicalKey(m) == key,
                $"'{key}' is not a canonical §2023:15 citation");
            if (row.In2002 is not null) Assert.StartsWith("§2002:15.", row.In2002, StringComparison.Ordinal);
            if (row.In2014 is not null) Assert.StartsWith("§2014:15.", row.In2014, StringComparison.Ordinal);
            if (row.Function is not { } function) continue;
            Assert.True(IntrinsicCatalog.TryGet(function, out var sig), $"{key}: {function} is no catalog function");
            Assert.Equal(sig.IntroducedIn > 2002, row.In2002 is null);
            Assert.Equal(sig.IntroducedIn > 2014, row.In2014 is null);
        }
    }

    [Fact]
    public void ARuleIsPrintedAtEachEditionsOwnNumber_OrWithTheEditionThatBeginsIt()
    {
        var e85 = EditionInfo.Of(85);
        var e2002 = EditionInfo.Of(2002);
        var e2014 = EditionInfo.Of(2014);
        var e2023 = EditionInfo.Of(2023);

        // ABS: in every edition, at its own paragraph and rule number.
        Assert.Equal("ISO §2023:15.7.3 AR1", IntrinsicCitations.Rule(e2023, "§2023:15.7.3 AR1"));
        Assert.Equal("ISO §2014:15.7.2 AR1", IntrinsicCitations.Rule(e2014, "§2023:15.7.3 AR1"));
        Assert.Equal("ISO §2002:15.6.2 AR1", IntrinsicCitations.Rule(e2002, "§2023:15.7.3 AR1"));
        Assert.Equal("§1989:A.2.5.3", IntrinsicCitations.BareRule(e85, "§2023:15.8.3 AR1"));   // ACOS, the first function of the 1989 module
        Assert.Contains("FUNCTION ABS begins in COBOL-2002 (targeting COBOL-85)", IntrinsicCitations.BareRule(e85, "§2023:15.7.3 AR1"), StringComparison.Ordinal);
        // the legacy spellings read as the rule
        Assert.Equal(IntrinsicCitations.Rule(e2014, "§2023:15.7.3 AR1"), IntrinsicCitations.Rule(e2014, "§2023:15.7.3 rule 1"));   // citation-audit: example
        // a chain of rules is cited whole
        Assert.Equal("§2014:15.9.2 AR1/AR3", IntrinsicCitations.BareRule(e2014, "§2023:15.9.3 AR1/AR3"));
        // CONVERT is COBOL-2023's: no older edition has a rule to cite, and the message says which edition begins it
        Assert.Equal("ISO §2023:15.19.3 AR3; FUNCTION CONVERT begins in COBOL-2023 (targeting COBOL-2002)", IntrinsicCitations.Rule(e2002, "§2023:15.19.3 AR3"));
        Assert.Equal("ISO §2023:15.19.3 AR3; FUNCTION CONVERT begins in COBOL-2023 (targeting COBOL-2014)", IntrinsicCitations.Rule(e2014, "§2023:15.19.3 AR3"));
        // TRIM is COBOL-2014's
        Assert.Contains("FUNCTION TRIM begins in COBOL-2014 (targeting COBOL-2002)", IntrinsicCitations.BareRule(e2002, "§2023:15.96.2"), StringComparison.Ordinal);
        Assert.StartsWith("§2014:15.89.1", IntrinsicCitations.BareRule(e2014, "§2023:15.96.2"), StringComparison.Ordinal);
        // a citation the table does not hold fails loudly, naming the generator
        var ex = Assert.Throws<InvalidOperationException>(() => IntrinsicCitations.Rule(e2002, "§2023:15.7.3 AR99"));   // citation-audit: example
        Assert.Contains("gen_intrinsic_citation_concordance.py", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>A string literal that is nothing but a §2023:15 citation: an input to <c>IntrinsicCitations.Rule</c>, <c>BareRule</c> or
    /// <c>Clause</c> (a switch arm, a conditional, a method's return), not message text.</summary>
    private static readonly Regex CitationLiteral = new(
        @"""§2023:15\.[\d.]+(?: [A-Za-z]+ ?\d+[a-z]?\d?(?:/[A-Z]+\d+)*)?(?: \(via [^""]*\))?""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Cite = new(@"§2023:15\.\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The binder writes a §2023:15 rule in one way: a literal that is the citation alone, handed to
    /// <c>IntrinsicCitations</c> (or a <c>Cite</c> of its own that names every edition). A message that spells one in its text prints
    /// COBOL-2023's number at COBOL-2002 and COBOL-2014. The one exception is the TRIM gate, a message of COBOL-2023's own rule that
    /// names the edition that introduced it.</summary>
    [Fact]
    public void IntrinsicBinder_SpellsNoFunctionRuleInAMessage()
    {
        string path = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs");
        var offences = new List<string>();
        int n = 0;
        foreach (string line in File.ReadLines(path))
        {
            n++;
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            string code = CitationLiteral.Replace(line, "\"\"");
            int comment = code.IndexOf("//", StringComparison.Ordinal);
            if (comment >= 0 && code.IndexOf('"', comment) < 0) code = code[..comment];
            if (!Cite.IsMatch(code)) continue;
            if (code.Contains(".Cite(", StringComparison.Ordinal) || code.Contains("CiteRule(", StringComparison.Ordinal)) continue;
            if (code.Contains("introduced by ISO/IEC 1989:2023 (§2023:15.96;", StringComparison.Ordinal)) continue;
            offences.Add($"IntrinsicBinder.cs:{n}: {trimmed}");
        }
        Assert.True(offences.Count == 0,
            "a message of IntrinsicBinder.cs spells a §2023:15 rule in its text, so COBOL-2002 and COBOL-2014 print COBOL-2023's number "
            + "(kb/Work PB6118): pass the citation through IntrinsicCitations.Rule or BareRule\n  " + string.Join("\n  ", offences));
    }
}
