// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.2 data-description-clause introduction gates (BASED / TYPE / PROPERTY /
/// TYPEDEF) — relocated from the bind-time <c>DataBinder.BindEntry</c> clause-loop Checks to the post-bind
/// <c>VersionConformancePass</c>. Three go to the PARSE-arm on recognition (BASED / TYPE / PROPERTY — their bound
/// carriers are cleared or nulled during bind, so a bound-arm gate would drop the 0900, the DEVLOG-724 flaw); TYPEDEF
/// goes to the BOUND-arm <c>GateData</c> (the init-only <c>DataItem.IsTypedef</c> survives declaration errors). The
/// version matrix + the contains-based conformance suite verify PRESENCE; these pin the FIRING COUNT — exactly ONE
/// COBOLNET0900 per written clause — which a contains-based assertion cannot catch. The load-bearing case is a TYPEDEF
/// referenced twice: the TYPEDEF intro must gate ONCE (on the template in <c>TypeDecls</c>, never on the two post-bind
/// <c>TYPE</c> clone subtrees, which <c>ConformanceForest</c> excludes via <c>DataItem.TypeAnchor</c>) while the TYPE
/// clause gates TWICE (one parse node per written reference).
/// </summary>
public sealed class DataClauseEditionTests
{
    private static int Count0900(string source, int edition, string whereFragment)
    {
        var (_, errors, _) = EditionHarness.CompileFull(source, edition);
        return errors.Count(e => e.Contains("COBOLNET0900", StringComparison.OrdinalIgnoreCase)
            && e.Contains(whereFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string Prog(string wsLines) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DCE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsLines}
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    /// <summary>BASED (parse-arm): a below-2002 BASED clause produces EXACTLY ONE COBOLNET0900 naming the clause.</summary>
    [Fact]
    public void BasedClause_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WB PIC X(5) BASED."), 85, "the BASED clause"));

    /// <summary>BASED at its introduction edition (2002) is a no-op — the recognition gate names an edition only when
    /// the construct is below it.</summary>
    [Fact]
    public void BasedClause_At2002_NoGate()
        => Assert.Equal(0, Count0900(Prog("01 WB PIC X(5) BASED."), 2002, "the BASED clause"));

    /// <summary>PROPERTY (parse-arm): a below-2002 PROPERTY clause produces EXACTLY ONE COBOLNET0900. (PROPERTY as a
    /// data-NAME stays a legal user word at 85 — only the CLAUSE is gated; see property_below_2002.cob.)</summary>
    [Fact]
    public void PropertyClause_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("01 WP PIC X PROPERTY."), 85, "the PROPERTY clause"));

    /// <summary>TYPEDEF (bound-arm): a level-01 TYPEDEF declaration produces EXACTLY ONE COBOLNET0900 — the subordinate
    /// member is not itself a typedef, so <c>IsTypedef</c> is set only on the template root.</summary>
    [Fact]
    public void TypedefDeclaration_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("""
            01 WT TYPEDEF.
               05 WF PIC X.
            """), 85, "the TYPEDEF clause"));

    /// <summary>A MISPLACED subordinate TYPEDEF (illegal per §13.18.58 → COBOLNET1529) still keeps its init-only
    /// <c>IsTypedef</c> in the Roots subtree, so the edition gate fires ONCE there too (recognition-equivalent, matching
    /// the former BindEntry Check which fired for every entry regardless of placement).</summary>
    [Fact]
    public void SubordinateTypedef_At85_StillGatedOnce()
        => Assert.Equal(1, Count0900(Prog("""
            01 WREC.
               05 WSUB TYPEDEF.
                  10 WF PIC X.
            """), 85, "the TYPEDEF clause"));

    /// <summary>The load-bearing dedup witness: a TYPEDEF referenced by TWO <c>TYPE IS</c> items gates the TYPEDEF
    /// intro EXACTLY ONCE (on the template — the two ExpandTypes clone subtrees are excluded via <c>TypeAnchor</c>) and
    /// the TYPE clause EXACTLY TWICE (one parse node per written reference; the clones are DataItem objects, not parse
    /// nodes, so they add no TYPE occurrences).</summary>
    [Fact]
    public void TypedefReferencedTwice_GatesTypedefOnce_TypeTwice()
    {
        string src = Prog("""
            01 WTT TYPEDEF.
               05 WF PIC X.
            01 WA TYPE WTT.
            01 WB TYPE WTT.
            """);
        Assert.Equal(1, Count0900(src, 85, "the TYPEDEF clause"));
        Assert.Equal(2, Count0900(src, 85, "the TYPE clause"));
    }
}
