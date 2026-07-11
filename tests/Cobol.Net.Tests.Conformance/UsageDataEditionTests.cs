// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.1 data-attribute USAGE / PICTURE-category introduction gates — relocated
/// from the bind-time <c>PictureAnalyzer.ParseUsage</c>/<c>Analyze</c> Checks to the post-bind <c>VersionConformancePass</c>
/// <c>GateData</c> enumerator (keyed on the RESOLVED item). The version matrix + the contains-based conformance suite
/// verify PRESENCE; these pin the FIRING COUNT — exactly ONE COBOLNET0900 per SOURCE declaration — which a
/// contains-based assertion cannot catch. They guard the three dedup hazards the recon flagged: (1) the dual
/// USAGE-clause vs PICTURE analysis paths must not double-fire; (2) a TYPE-clause CLONE + a compiler temp share the
/// source item's <c>PicInfo</c> by reference and must NOT be gated (the once-per-source gate fired on the template);
/// (3) two DISTINCT pointer items share the ONE <c>PicInfo.PointerItem</c> singleton — so the enumerator must NOT
/// dedup by <c>PicInfo</c> identity (which would collapse them to one).
/// </summary>
public sealed class UsageDataEditionTests
{
    private static int Count0900(string source, int edition, string whereFragment)
    {
        var (_, errors, _) = EditionHarness.CompileFull(source, edition);
        return errors.Count(e => e.Contains("COBOLNET0900", StringComparison.OrdinalIgnoreCase)
            && e.Contains(whereFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string Prog(string wsLines) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. UDE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsLines}
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    /// <summary>One data item of a 2002-introduced USAGE / PICTURE category, at COBOL-85, produces EXACTLY ONE
    /// COBOLNET0900 naming that item — never two (the dual USAGE-clause + PICTURE analysis paths that both fired
    /// before 14g.1 now resolve to one item, gated once).</summary>
    [Theory]
    [InlineData("01 WITM PIC N(4).")]                  // national (PIC N)
    [InlineData("01 WITM PIC 1(4).")]                  // boolean (PIC 1)
    [InlineData("01 WITM USAGE POINTER.")]             // pointer (picture-less)
    [InlineData("01 WITM USAGE OBJECT REFERENCE.")]    // object reference (universal, picture-less)
    [InlineData("01 WITM USAGE BINARY-LONG.")]         // binary-char family (picture-less)
    [InlineData("01 WITM USAGE FLOAT-LONG.")]          // float trio (picture-less)
    public void SingleUsageItem_At85_ExactlyOne0900(string wsLine)
        => Assert.Equal(1, Count0900(Prog(wsLine), 85, "data item 'WITM'"));

    /// <summary>Two DISTINCT pointer items each produce their own 0900 (the <c>PicInfo.PointerItem</c> singleton is
    /// shared BY REFERENCE across both, so a naive identity dedup would collapse them to one — the enumerator keys
    /// on the item, not the <c>PicInfo</c>).</summary>
    [Fact]
    public void TwoDistinctPointerItems_At85_GateIndependently()
    {
        string src = Prog("01 WPA USAGE POINTER.\n        01 WPB USAGE POINTER.");
        Assert.Equal(1, Count0900(src, 85, "data item 'WPA'"));
        Assert.Equal(1, Count0900(src, 85, "data item 'WPB'"));
    }

    /// <summary>A TYPEDEF whose member carries a national PICTURE, referenced by TWO <c>TYPE IS</c> items, gates the
    /// national introduction EXACTLY ONCE — on the TEMPLATE member (in <c>TypeDecls</c>), never on the two post-bind
    /// clone subtrees (which the enumerator excludes via <c>DataItem.TypeAnchor</c>). Without the clone exclusion the
    /// count would be three (template + both clones, all named MNAT).</summary>
    [Fact]
    public void TypedefNationalMember_ReferencedTwice_GatesNationalOnce()
    {
        string src = Prog("""
            01 TNAT TYPEDEF.
               05 MNAT PIC N(4).
            01 X1 TYPE TNAT.
            01 X2 TYPE TNAT.
        """);
        Assert.Equal(1, Count0900(src, 85, "data item 'MNAT'"));
    }
}
