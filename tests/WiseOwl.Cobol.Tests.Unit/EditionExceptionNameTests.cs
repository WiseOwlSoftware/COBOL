// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ kb/Work PB6107 — A CONDITION THAT HAS ONE NAME IN 2002 AND ANOTHER FROM 2014 IS NAMED THROUGH ONE TABLE. §2002 states the
/// omitted-argument condition once, EC-PROGRAM-ARG-OMITTED, for a called program, an activated function and an invoked method
/// (§2002:14.8.4.3 GR12, §2002:8.4.2.2.3 GR7, §2002:14.8.22.3 GR10) and the locate miss of a function as EC-PROGRAM-NOT-FOUND
/// (§2002:8.4.2.2.3 GR5); §2014 and §2023 split each by element kind. <see cref="ExceptionNameRule.NameInEdition"/> is the one
/// table, and every consumer — the binder's per-statement declaration (<c>EcBinder.EcWrap</c>), the formal's guard
/// (<c>EcBinder.MarkFormals</c>) and the activation emitters — asks it, so the next edition-dependent name is one row.
/// </summary>
public sealed class EditionExceptionNameTests
{
    /// <summary>Every row renames a catalogued later name to a catalogued earlier one that its edition already lists, and the
    /// answer flips exactly at the later name's <see cref="EcInfo.IntroducedIn"/>.</summary>
    [Fact]
    public void EveryRename_PointsAtACataloguedEarlierName_AndFlipsAtTheLaterNamesEdition()
    {
        int rows = 0;
        foreach (var (later, earlier) in ExceptionNameRule.EditionRenames)
        {
            rows++;
            Assert.True(ExceptionCatalog.TryGet(later, out var laterInfo), later);
            Assert.True(ExceptionCatalog.TryGet(earlier, out var earlierInfo), earlier);
            Assert.True(earlierInfo.IntroducedIn < laterInfo.IntroducedIn, $"{earlier} must predate {later}");
            Assert.Equal(earlier, ExceptionNameRule.NameInEdition(later, laterInfo.IntroducedIn - 1));
            Assert.Equal(later, ExceptionNameRule.NameInEdition(later, laterInfo.IntroducedIn));
            Assert.Equal(later, ExceptionNameRule.NameInEdition(later, 2023));
        }
        Assert.True(rows >= 3, "the table lost a row");
    }

    /// <summary>A name that is the same in every edition, and a name no row renames, come back unchanged.</summary>
    [Theory]
    [InlineData("EC-PROGRAM-NOT-FOUND", 2002)]
    [InlineData("EC-PROGRAM-ARG-OMITTED", 2002)]
    [InlineData("EC-SIZE-TRUNCATION", 2002)]
    [InlineData("EC-FUNCTION-PTR-NULL", 2014)]
    public void AnUnrenamedName_IsItsOwnAnswer(string name, int edition) =>
        Assert.Equal(name, ExceptionNameRule.NameInEdition(name, edition));

    /// <summary>The kind a guard raises through: the element's own from 2014, the program's at 2002, whose one condition covers all
    /// three (so <c>OmittedFormal.Raise</c> reaches the helper and the flag of the condition the edition defines).</summary>
    [Theory]
    [InlineData(ActivatedElementKind.Program, 2002, ActivatedElementKind.Program)]
    [InlineData(ActivatedElementKind.Function, 2002, ActivatedElementKind.Program)]
    [InlineData(ActivatedElementKind.Method, 2002, ActivatedElementKind.Program)]
    [InlineData(ActivatedElementKind.Function, 2014, ActivatedElementKind.Function)]
    [InlineData(ActivatedElementKind.Method, 2014, ActivatedElementKind.Method)]
    [InlineData(ActivatedElementKind.Function, 2023, ActivatedElementKind.Function)]
    public void RaisedKind_FollowsTheEditionsConditionName(ActivatedElementKind element, int edition, ActivatedElementKind expected)
    {
        Assert.Equal(expected, ExceptionNameRule.OmittedArgumentRaisedKind(element, edition));
        Assert.Equal(OmittedFormal.ConditionName(expected), ExceptionNameRule.OmittedArgumentConditionName(element, edition));
    }

    private const string FunctionAndCaller = """
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. PB6107F.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9.
                   PROCEDURE DIVISION RETURNING R.
                       MOVE 1 TO R.
                       GOBACK.
                   END FUNCTION PB6107F.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB6107M.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   REPOSITORY.
                       FUNCTION PB6107F.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC 9.
                   PROCEDURE DIVISION.
                       MOVE FUNCTION PB6107F TO X.
                       STOP RUN.
                   END PROGRAM PB6107M.
            """;

    /// <summary>The activation site names the locate-miss condition of ITS edition and says it is a function activation
    /// (the runtime no longer reads function-ness off the name).</summary>
    [Theory]
    [InlineData(2002, "EC-PROGRAM-NOT-FOUND")]
    [InlineData(2014, "EC-FUNCTION-NOT-FOUND")]
    [InlineData(2023, "EC-FUNCTION-NOT-FOUND")]
    public void FunctionActivation_NamesTheEditionsLocateMissCondition(int edition, string expected)
    {
        string cs = Emit(FunctionAndCaller, edition);
        Assert.Contains($"notFoundEc: \"{expected}\", asFunction: true", cs);
    }

    private static string Emit(string source, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), $"edname_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = TestSourceFormat.Inline, DialectLevel = edition };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            return new CSharpEmitter().EmitBound(
                new BinderDriver().Bind(tree!, new EditionContext(edition), frontend.Directives), "EDNAMEPROBE");
        }
        finally { FileHarness.TryDelete(path); }
    }
}
