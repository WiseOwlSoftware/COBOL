// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Unit tests for the REDEFINES / RENAMES classification pass (ISO/IEC 1989:2023 §13.18.44/§13.18.45;
/// COBOLNET_DESIGN §4). They bind a WORKING-STORAGE fragment and assert the shared-storage class membership, the
/// canonical/view split, the tier verdict, the class width, and the SR9 subordinate-suppression propagation —
/// catching silent mis-classification a behavioral test could miss (the dispatch-site / mis-classification bug class).
/// </summary>
public sealed class RedefinesClassificationTests
{
    /// <summary>Bind a WORKING-STORAGE fragment and return the populated <see cref="DataBinder"/>.</summary>
    private static DataBinder Bind(string ws)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. T.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {ws}
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_redef_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { DialectLevel = 85 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder();
            data.Bind(program);
            return data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    private static DataItem Item(DataBinder d, string name) => d.ByName[name][0];

    [Fact]
    public void TierA_IdenticalPic_OneClassOneCanonical()
    {
        var d = Bind("01 WS-A PIC 9(4).\n01 WS-B REDEFINES WS-A PIC 9(4).");
        var a = Item(d, "WS-A");
        var b = Item(d, "WS-B");
        Assert.NotNull(a.Class);
        Assert.Same(a.Class, b.Class);
        Assert.True(a.IsCanonical);
        Assert.False(b.IsCanonical);
        Assert.Equal(RedefinesTier.Alias, a.Class!.Tier);
        Assert.Equal([a, b], a.Class.Members);
        Assert.Same(a, a.Class.Canonical);
    }

    [Fact]
    public void TierA_NumericOverNumeric_SameDigitsDifferentScale()
    {
        // S9(6)V9(6) and S9(12) are both a 12-digit DISPLAY long — one shared storage, different scale metadata.
        var d = Bind("01 WS-A PIC S9(6)V9(6).\n01 WS-B REDEFINES WS-A PIC S9(12).");
        Assert.Equal(RedefinesTier.Alias, Item(d, "WS-A").Class!.Tier);
        Assert.Equal(12, Item(d, "WS-A").Class!.Width);
    }

    [Fact]
    public void TierB_GroupView_StringCanonical()
    {
        // A group redefiner over an alphanumeric original is not a same-type alias → a string canonical with windows.
        var d = Bind("""
            01 WS-A PIC X(6).
            01 WS-B REDEFINES WS-A.
               05 WS-B1 PIC X(2).
               05 WS-B2 PIC 9(4).
            """);
        var a = Item(d, "WS-A");
        Assert.Equal(RedefinesTier.StringCanonical, a.Class!.Tier);
        Assert.Equal(6, a.Class.Width);
        // SR9: the view's subordinates are non-canonical too (their stored fields + VALUE are suppressed).
        Assert.False(Item(d, "WS-B").IsCanonical);
        Assert.False(Item(d, "WS-B1").IsCanonical);
        Assert.False(Item(d, "WS-B2").IsCanonical);
    }

    [Fact]
    public void TierB_DifferentWidthsViaPartialRedefine()
    {
        // 9(4) over 9(6) is a partial overlap (different widths) → not a whole-area alias → string canonical.
        var d = Bind("01 WS-A PIC 9(6).\n01 WS-B REDEFINES WS-A PIC 9(4).");
        Assert.Equal(RedefinesTier.StringCanonical, Item(d, "WS-A").Class!.Tier);
        Assert.Equal(6, Item(d, "WS-A").Class!.Width);   // class-max width
    }

    [Fact]
    public void TierC_MixedUsage_RejectedInInterim()
    {
        var d = Bind("01 WS-A PIC X(4).\n01 WS-B REDEFINES WS-A PIC 9(8) COMP.");
        var cls = Item(d, "WS-A").Class!;
        Assert.Equal(RedefinesTier.Rejected, cls.Tier);
        Assert.NotNull(cls.RejectReason);
    }

    [Fact]
    public void NestedRedefinesChain_AllAnchorToOriginal_SR11()
    {
        var d = Bind("01 WS-A PIC X(4).\n01 WS-B REDEFINES WS-A PIC X(4).\n01 WS-C REDEFINES WS-B PIC X(4).");
        var cls = Item(d, "WS-A").Class!;
        Assert.Equal([Item(d, "WS-A"), Item(d, "WS-B"), Item(d, "WS-C")], cls.Members);
        Assert.Same(Item(d, "WS-A"), cls.Canonical);
    }

    [Fact]
    public void MultipleRedefinitionsOfOneArea_OneClass_SR7()
    {
        var d = Bind("01 WS-A PIC X(4).\n01 WS-B REDEFINES WS-A PIC X(4).\n01 WS-C REDEFINES WS-A PIC X(4).");
        var cls = Item(d, "WS-A").Class!;
        Assert.Equal(3, cls.Members.Count);
        Assert.Contains(Item(d, "WS-B"), cls.Members);
        Assert.Contains(Item(d, "WS-C"), cls.Members);
    }

    [Fact]
    public void Renames_NoThru_IsAlias()
    {
        var d = Bind("01 WS-R.\n   05 WS-X PIC X(3).\n   05 WS-Y PIC X(3).\n66 WS-Z RENAMES WS-X.");
        var z = Item(d, "WS-Z");
        Assert.NotNull(z.Renames);
        Assert.True(z.Renames!.IsAlias);
        Assert.Same(Item(d, "WS-X"), z.Renames.From);
        Assert.Null(z.Renames.Thru);
    }

    [Fact]
    public void Renames_Thru_SpansFromTo()
    {
        var d = Bind("01 WS-R.\n   05 WS-X PIC X(3).\n   05 WS-Y PIC X(3).\n66 WS-Z RENAMES WS-X THRU WS-Y.");
        var z = Item(d, "WS-Z");
        Assert.False(z.Renames!.IsAlias);
        Assert.Same(Item(d, "WS-X"), z.Renames.From);
        Assert.Same(Item(d, "WS-Y"), z.Renames.Thru);
    }

    [Fact]
    public void NonRedefiningItem_StandsAlone_NoClass()
    {
        var d = Bind("01 WS-A PIC X(4).\n01 WS-B PIC 9(4).");
        Assert.Null(Item(d, "WS-A").Class);
        Assert.Null(Item(d, "WS-B").Class);
        Assert.True(Item(d, "WS-A").IsCanonical);   // standalone items are trivially canonical (emit normally)
    }
}
