// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The COMPUTED set of kb/Work PB5852 (DESIGN-data-model §2.1.1): at 1985 (and 2002) a MOVE places "the sign of the sending
/// operand" (§1985:VI.6.19.4 GR4b.1), so a signed zoned receiver a store can hand a NEGATIVE zero is promoted to its image by
/// <c>StorageFormPass</c>, and every other item keeps its native carrier. Each test binds a whole program through the full
/// pipeline and reads the storage form the pass computed: an item in the set, items out of it, the closure, and the empty
/// set at 2023, whose rule ties the sign to the value (§2023:14.9.25.4 GR6d.2.a).
/// </summary>
public sealed class NegativeZeroPromotionTests
{
    private const string Data = """
        01 S3 PIC S999 VALUE -100.
        01 S1 PIC S99 VALUE -1.
        01 L  PIC S99 SIGN LEADING SEPARATE.
        01 M  PIC S99.
        01 M2 PIC S99 SIGN TRAILING SEPARATE.
        01 N  PIC S99.
        01 U  PIC 99.
        01 B  PIC S99 COMP.
        01 K  PIC S9V9 PACKED-DECIMAL.
        01 L3 PIC S99 SIGN LEADING SEPARATE.
        01 L6 PIC S99 SIGN LEADING SEPARATE.
        """;

    /// <summary>Bind the program at <paramref name="edition"/> and return its items by name.</summary>
    private static Func<string, DataItem> Bind(int edition, string procedure)
    {
        string body = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NZ{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {Data}
            PROCEDURE DIVISION.
            P0.
            {procedure}
                STOP RUN.
            """;
        // Fixed reference format, so the 1985 front end reads it: every line in area A or B.
        string src = string.Join("\n", body.Split('\n').Select(l => "       " + l.TrimEnd('\r')));
        string path = Path.Combine(Path.GetTempPath(), "cn_nzero_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Fixed, DialectLevel = edition };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var bound = new BinderDriver().Bind(tree!, new EditionContext(edition, permissive: false), frontend.Directives);
            var data = bound.Units.Single().Data;
            return name => data.Forest.ItemNamed(name);
        }
        finally { FileHarness.TryDelete(path); }
    }

    private static bool Image(DataItem item) => item.Storage is StorageForm.CharImage { Category: PicCategory.Numeric };

    /// <summary>IN THE SET: -100 into S99 loses its digits (high-order truncation), so the receiver can hold -00 at 85 and is
    /// image-carried; the sender itself is only a sender and stays native.</summary>
    [Fact]
    public void TruncatingNegativeStore_PromotesItsReceiver_At1985()
    {
        var item = Bind(85, "    MOVE S3 TO L. MOVE S3 TO M.");
        Assert.True(Image(item("L")));
        Assert.True(Image(item("M")));
        Assert.IsType<StorageForm.NativeInt>(item("S3").Storage);
    }

    /// <summary>OUT OF THE SET: a store that keeps every digit of a native sender (S99 into S99), a non-negative literal, an
    /// unsigned receiver, and BINARY and PACKED-DECIMAL receivers (no sign holder: their sign is the value's) stay native.</summary>
    [Fact]
    public void StoresThatCannotHandANegativeZero_LeaveTheReceiverNative_At1985()
    {
        var item = Bind(85, "    MOVE S1 TO N. MOVE 100 TO L. MOVE S3 TO U.\n    MOVE S3 TO B. MOVE S3 TO K.");
        foreach (string name in new[] { "N", "L", "U", "B", "K" })
            Assert.False(Image(item(name)), name);
    }

    /// <summary>A literal is decided on its own non-zero digits: -0.4 loses its digit on the right of S99 (in the set), -1
    /// keeps it (out of the set).</summary>
    [Fact]
    public void NegativeLiteral_IsInTheSetOnlyWhenItsDigitsTruncate()
    {
        var item = Bind(85, "    MOVE -0.4 TO L3. MOVE -1 TO L6.");
        Assert.True(Image(item("L3")));
        Assert.False(Image(item("L6")));
    }

    /// <summary>THE CLOSURE: S99 into S99 TRAILING SEPARATE keeps every digit, yet when the sender L can itself hold a negative
    /// zero (it receives -100) the receiver is handed one too ("the sign of the sending operand" is negative), so M2 joins
    /// the set; with L only ever receiving -1, nothing promotes L and M2 stays native.</summary>
    [Fact]
    public void ANegativeZeroMovedOn_PromotesTheNextReceiver()
    {
        var closed = Bind(85, "    MOVE S3 TO L. MOVE L TO M2.");
        Assert.True(Image(closed("L")));
        Assert.True(Image(closed("M2")));

        var open = Bind(85, "    MOVE S1 TO L. MOVE L TO M2.");
        Assert.False(Image(open("L")));
        Assert.False(Image(open("M2")));
    }

    /// <summary>THE SET IS EMPTY AT 2023: the sign is the value's, so no store hands a negative zero and every receiver of the
    /// same program keeps its native carrier.</summary>
    [Fact]
    public void TheSetIsEmpty_At2023()
    {
        var item = Bind(2023, "    MOVE S3 TO L. MOVE S3 TO M. MOVE L TO M2. MOVE -0.4 TO L3.");
        foreach (string name in new[] { "L", "M", "M2", "L3" })
            Assert.False(Image(item(name)), name);
    }
}
