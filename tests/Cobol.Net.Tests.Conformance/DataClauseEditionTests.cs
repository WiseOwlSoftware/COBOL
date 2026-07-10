// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.2 data-description-clause introduction gates (BASED / TYPE / PROPERTY /
/// TYPEDEF) — relocated from the bind-time <c>DataBinder.BindEntry</c> clause-loop Checks to the post-bind
/// <c>VersionConformancePass</c>. ALL FOUR go to the PARSE-arm on recognition: none needs a resolved fact, and a
/// bound-arm home would drop the 0900 on a declaration-error path (the DEVLOG-724 flaw) — the bound carriers of
/// BASED/TYPE/PROPERTY are cleared or nulled during bind, and (the DEVLOG-734 review correction) even the init-only
/// <c>DataItem.IsTypedef</c> is unreliable because the typedef ITEM is discarded from <c>ConformanceForest</c> when
/// <c>RegisterTypeDecl</c> rejects it (unnamed/FILLER, duplicate type-name) or it binds into method scope. The version
/// matrix + the contains-based conformance suite verify PRESENCE; these pin the FIRING COUNT — exactly ONE COBOLNET0900
/// per written clause — which a contains-based assertion cannot catch. The load-bearing case is a TYPEDEF referenced
/// twice: TYPEDEF gates ONCE (one <c>typedefClause</c> node) and TYPE TWICE (one <c>typeClause</c> node per written
/// reference; the ExpandTypes clones are DataItem objects, not parse nodes). The three DEVLOG-734 regressions
/// (FILLER typedef, duplicate type-name, level-88-mis-attached clause) pin the byte-neutral invariant the bound-arm
/// TYPEDEF gate violated.
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

    // ── DEVLOG-734 regressions: the byte-neutral 0900 invariant the bound-arm TYPEDEF gate violated ─────────────
    // A bound-arm gate over ConformanceForest dropped the 0900 whenever the typedef item failed to register; the
    // parse-arm (recognition) fires on the always-present parse node instead.

    /// <summary>A FILLER (nameless) level-01 TYPEDEF still names its edition at 85 — the parse node is present even
    /// though <c>RegisterTypeDecl</c> discards the item (unnamed → COBOLNET1529, never added to <c>TypeDecls</c>). A
    /// bound-arm gate saw nothing to fire (Defect 1).</summary>
    [Fact]
    public void FillerTypedef_At85_StillGatesOnce()
        => Assert.Equal(1, Count0900(Prog("01 TYPEDEF PIC X."), 85, "the TYPEDEF clause"));

    /// <summary>A DUPLICATE type-name gates TWICE at 85 — one 0900 per written TYPEDEF — even though the second
    /// <c>TypeDecls.TryAdd</c> fails (COBOLNET1529, item discarded). A bound-arm gate collapsed the pair to one
    /// (Defect 2).</summary>
    [Fact]
    public void DuplicateTypedefName_At85_GatesEach()
        => Assert.Equal(2, Count0900(Prog("""
            01 TDUP TYPEDEF.
               05 A PIC X.
            01 TDUP TYPEDEF.
               05 B PIC X.
            """), 85, "the TYPEDEF clause"));

    /// <summary>A data-description clause MIS-ATTACHED to a level-88 condition-name entry (which the permissive grammar
    /// admits) does NOT gate — <c>BindEntries</c> intercepts level-88 before the storage-clause loop, so the former
    /// gate never ran; the parse-arm's <c>InConditionOrRenamesEntry</c> guard reproduces that (Defect 3, over-fire).</summary>
    [Fact]
    public void ClauseUnderLevel88_At85_DoesNotGate()
    {
        string src = Prog("""
            01 W88 PIC X.
               88 C88 TYPE IS FOO.
            """);
        Assert.Equal(0, Count0900(src, 85, "the TYPE clause"));
    }
}
