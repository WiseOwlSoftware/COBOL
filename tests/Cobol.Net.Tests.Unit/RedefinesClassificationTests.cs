// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
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

    /// <summary>§13.18.44.3 SR12 (kb/Work PB179 — the Step D Tier-D bind half): a POINTER item as the
    /// SUBJECT of a REDEFINES entry is a conformance rejection — before the screen it classified Tier B
    /// with a ZERO-WIDTH window (the pointer categories occupy no character positions).</summary>
    [Fact]
    public void TierD_PointerSubject_RejectedSr12()
    {
        var d = Bind("01 WS-A PIC X(8).\n01 WS-P REDEFINES WS-A USAGE POINTER.");
        var p = Item(d, "WS-P");
        Assert.Equal(RedefinesTier.Rejected, p.Class!.Tier);
        Assert.Contains("SR12", p.Class.RejectReason);
    }

    /// <summary>§13.18.44.3 SR14: data-name-2 (the redefined item) of class pointer.</summary>
    [Fact]
    public void TierD_PointerTarget_RejectedSr14()
    {
        var d = Bind("01 WS-P USAGE POINTER.\n01 WS-A REDEFINES WS-P PIC X(8).");
        var a = Item(d, "WS-A");
        Assert.Equal(RedefinesTier.Rejected, a.Class!.Tier);
        Assert.Contains("SR14", a.Class.RejectReason);
    }

    /// <summary>A pointer leaf NESTED inside a redefining group is NOT SR12/SR14's letter (those bar the
    /// entry-level items) — it takes the staged-loud Tier-D arm: Rejected with the byte-window-overlay
    /// mechanism named, never a silent zero-width Tier-B alias.</summary>
    [Fact]
    public void TierD_NestedPointerLeaf_StagedLoud()
    {
        var d = Bind("01 WS-A PIC X(8).\n01 WS-G REDEFINES WS-A.\n   05 WS-G-X PIC X(3).\n   05 WS-G-P USAGE POINTER.");
        var g = Item(d, "WS-G");
        Assert.Equal(RedefinesTier.Rejected, g.Class!.Tier);
        Assert.Contains("byte-window overlay", g.Class.RejectReason);
    }

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
    public void TierB_BinaryLeafPun_StringCanonicalOverItsTrueBytes()
    {
        // Phase 1E (supersedes the interim Tier-C rejection): a DISPLAY + fixed-point BINARY pun is Tier B —
        // ONE string backing, the COMP leaf an image-stored window over it. V59 re-based that window on the
        // leaf's TRUE BYTES: a PIC S9(8) COMP occupies FOUR bytes (radix 2, §13.18.60.4 GR4), not eight zoned
        // digit characters, so the pun sees the first four positions of the X(8) area and the class width comes
        // from the WIDER member. The former zoned window also had to rewrite the leaf's SignKind to a trailing
        // overpunch, because a variable-width BinaryMinus image could not sit in a fixed digit window; with the
        // sign in the bytes there is nothing to rewrite, and DISPLAY of the leaf still renders '-100'.
        var d = Bind("01 WS-A PIC X(8).\n01 WS-B REDEFINES WS-A PIC S9(8) COMP.");
        var cls = Item(d, "WS-A").Class!;
        Assert.Equal(RedefinesTier.StringCanonical, cls.Tier);
        Assert.Equal(8, cls.Width);   // class-max: the X(8) member, now strictly wider than the 4-byte COMP view
        var b = Item(d, "WS-B");
        Assert.Equal(4, b.ImageWidth);   // 8 digits → the pinned 4-byte binary tier
        Assert.Equal(b.ByteWidth, b.ImageWidth);   // the one-width invariant, at a REDEFINES window
        // P5.7: the classifier RECORDS the image fact (ImageForcedItems) instead of mutating a flag; the final
        // StoreAsImage/Storage is computed once by the group-tail StorageFormPass, which this bare resolve-only
        // harness never runs — so assert the resolve-time COLLECTED FACT, the exact thing the classifier owns.
        Assert.Contains(b, d.ImageForcedItems);
        Assert.Equal("BinaryMinus", b.Pic!.SignKind);   // untouched: the sign lives in the two's-complement bytes
    }

    /// <summary>The FLIPPED lock (the Step D arm-1 dissolution — this fact previously pinned the Tier-C
    /// rejection): a float or COMP-5 pun is an ordinary Tier-B byte-window class now — the leaf's window is
    /// its pinned byte form at StorageWidth (IEEE 4 bytes for COMP-1; the full-container 2 bytes for a
    /// 9(4) COMP-5), image-forced onto the one string backing. The float fixture is re-expressed as the
    /// LEGAL picture-less <c>USAGE COMP-1</c> (the old <c>PIC 9(4) COMP-1</c> was COBOLNET1521 illegal
    /// source binding through error recovery — it proved nothing).</summary>
    [Fact]
    public void TierB_FloatAndComp5Puns_ByteWindows()
    {
        var d = Bind("01 WS-A PIC X(4).\n01 WS-B REDEFINES WS-A USAGE COMP-1.");
        var b = Item(d, "WS-B");
        Assert.Equal(RedefinesTier.StringCanonical, b.Class!.Tier);
        Assert.Equal(4, b.ImageWidth);            // the IEEE binary32 window
        Assert.Contains(b, d.ImageForcedItems);   // image-forced onto the one backing

        var d5 = Bind("01 WS-C PIC X(4).\n01 WS-D REDEFINES WS-C PIC 9(4) COMP-5.");
        var w5 = Item(d5, "WS-D");
        Assert.Equal(RedefinesTier.StringCanonical, w5.Class!.Tier);
        Assert.Equal(2, w5.ImageWidth);           // the full-container radix-2 window
        Assert.Contains(w5, d5.ImageForcedItems);
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
