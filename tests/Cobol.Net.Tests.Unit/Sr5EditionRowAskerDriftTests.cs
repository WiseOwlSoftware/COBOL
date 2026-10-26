// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ §14.9.25.3 SR5's EDITION ROWS ARE ONE BODY, ASKED OF EVERY ASKER OF THE MOVE RULES (kb/Work PB879, PB3755).
/// HIGH-VALUE, LOW-VALUE, QUOTE and a non-digit ALL literal into a numeric or numeric-edited item were valid through 2014
/// and are removed at 2023 (the rows <c>move-alphanumeric-figurative-removed-2023</c>, <c>move-quote-numeric-obsolete-2014</c>
/// and <c>move-all-digit-integer-obsolete-2023</c>), and the rows are asked wherever "the same as for a MOVE statement" is
/// the rule: the written MOVE and every implicit FROM / INTO move, INITIALIZE REPLACING's hypothetical MOVE (§14.9.20.3
/// SR4) and a figurative argument BY CONTENT into the formal parameter of a CALL, an INVOKE or a user-defined function
/// (§14.8.2.3.3 rule 2 d)). The rows lived in <c>VersionConformancePass</c> with two askers, a bound MOVE and a bound
/// INITIALIZE, and a third asker (the activation arm) compiled a 2023-removed form clean until PB3755. Two facts keep a
/// fourth from doing the same: the rows are spelled in ONE place, and every file that asks the BY CONTENT activation
/// verdict also asks the rows.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class Sr5EditionRowAskerDriftTests
{
    private const string BodyFile = "src/Cobol.Net.Compiler/Binding/FigurativeAssociation.cs";

    private static readonly string[] Rows =
        ["MoveAllDigitIntegerObsolete2023", "MoveQuoteNumericObsolete2014", "MoveAlphanumericFigurativeRemoved2023"];

    /// <summary>The generated registry files DEFINE the ids; the body file is the rows' owner and is set aside by name.</summary>
    private static readonly TreeScope Sources = new(Under: ["src/"],
        Except: ["src/Cobol.Net.Editions/Constructs.g.cs", "src/Cobol.Net.Editions/ConstructRegistry.g.cs",
                 "src/Cobol.Net.Compiler/Binding/FigurativeAssociation.cs"],
        Kinds: [".cs"]);

    private static string Read(string repoRelative) => File.ReadAllText(TestRepo.At(repoRelative.Split('/')));

    /// <summary>The three rows are read by <c>FigurativeAssociation.GateSr5</c> and by nothing else.</summary>
    [Fact]
    public void TheThreeRows_AreReadOnlyByTheOneBody()
    {
        var files = TrackedTree.Files(Sources).ToList();
        var offenders = new List<string>();
        foreach (string row in Rows)
        {
            var rx = new Regex($@"\bConstructs\s*\.\s*{row}\b");
            offenders.AddRange(files.Where(f => rx.IsMatch(Read(f))).Select(f => $"{row} in {f}"));
        }
        Assert.True(offenders.Count == 0,
            "§14.9.25.3 SR5's edition rows are asked through FigurativeAssociation.GateSr5 (kb/Work PB3755); a second reader "
            + "is a second copy of the rule, which is how the activation arm came to have none: "
            + string.Join(", ", offenders));
        string body = Read(BodyFile);
        foreach (string row in Rows) Assert.Matches($@"\bConstructs\s*\.\s*{row}\b", body);
    }

    /// <summary>The three askers the standard's MOVE rules reach today — the bound MOVE, the bound INITIALIZE and the
    /// BY CONTENT activation arm — each call the body.</summary>
    [Theory]
    [InlineData("src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs")]
    [InlineData("src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ParameterConformance.cs")]
    public void EachAsker_CallsTheBody(string asker)
        => Assert.Matches(@"\bFigurativeAssociation\s*\.\s*GateSr5\s*\(", Read(asker));

    /// <summary>EVERY file that asks the BY CONTENT / BY VALUE activation verdict
    /// (<c>ParameterConformance.ContentConformanceReason</c>: the CALL and function lanes through <c>CheckArgument</c>, the
    /// INVOKE lane directly) also asks the SR5 rows (<c>GateFigurativeEdition</c>). The verdict is edition-blind, so a new
    /// activation lane that asks only it accepts at 2023 what the written MOVE refuses.</summary>
    [Fact]
    public void EveryFileThatAsksTheContentVerdict_AlsoAsksTheSr5Rows()
    {
        var verdict = new Regex(@"\bContentConformanceReason\s*\(");
        var rows = new Regex(@"\bGateFigurativeEdition\s*\(");
        var askers = new List<string>();
        var missing = new List<string>();
        foreach (string f in TrackedTree.Files(Sources))
        {
            string text = Read(f);
            if (!verdict.IsMatch(text)) continue;
            askers.Add(f);
            if (!rows.IsMatch(text)) missing.Add(f);
        }
        Assert.Contains(askers, f => f.EndsWith("/ParameterConformance.cs", StringComparison.Ordinal));
        Assert.Contains(askers, f => f.EndsWith("/OoBinder.cs", StringComparison.Ordinal));
        Assert.True(missing.Count == 0,
            "these files ask ParameterConformance.ContentConformanceReason (a BY CONTENT argument against its formal) but never "
            + "ParameterConformance.GateFigurativeEdition, so a figurative argument they bind skips §14.9.25.3 SR5's edition "
            + "rows (kb/Work PB3755): " + string.Join(", ", missing));
    }
}
