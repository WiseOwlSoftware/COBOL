// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE SET STATEMENT'S FORMAT-SELECTION TABLE (kb/Work PB449 + PB456).
///
/// <para><b>Why it exists.</b> §2023:14.9.39.2 prints seventeen general formats, several of which share one token
/// shape, so which format a statement IS has to be decided semantically. That decision used to be spread across
/// the candidate formats as independent peeks at <c>receivers[0]</c>, and the cost was measurable: the SAME two
/// operands gave a correct SR23 diagnostic when the pointer was written first and a run-time crash when it was
/// written second. <see cref="SetFormatSelection"/> replaced the peeks with ONE table, and these facts are what
/// keep "one table" true — a new amount- or carrier-taking format must be a row, and a row that overlaps an
/// existing one within a direction would restore exactly the ambiguity the peeks had.</para>
///
/// <para><b>What is pinned.</b> (1) Within one written direction — <c>TO</c> or <c>UP/DOWN BY</c> — no receiving
/// kind is admitted by two rows, so selection is deterministic and independent of row order for an exact match.
/// (2) Every kind a receiving operand can classify as is admitted by at least one row in at least one direction,
/// or is deliberately admitted by none; a kind no row names anywhere would be a receiving operand the selector
/// can see and no format can hold. (3) The order-independence property itself: a receiving list and its reverse
/// select the same format. (4) Format 1 is the last row, because the nearest-row fallback reads the table in
/// order and Format 1's <c>identifier-1</c> brace is the wide one. (5) The <c>SendsOnly</c> column — the sender
/// tie-break over Format 1's catch-all brace — names each kind at most once and names NO kind that Format 1's
/// own sending brace can hold, or a legal Format-1 statement would be hijacked into another format's syntax
/// rule.</para>
/// </summary>
public sealed class SetFormatSelectionDriftTests : WiseOwlCobolTestBase
{
    /// <summary>The kinds no printed receiving brace admits: a resolvable data item that is neither of SR1's two
    /// alternatives belongs to Format 1's BRACE (SR1 screens its CATEGORY, which is kb/Work PB212's own item),
    /// and <see cref="SetOperandKind.Unclassified"/> is the absence of evidence, never a category.</summary>
    private static readonly HashSet<SetOperandKind> NotAFormatOfItsOwn =
        [SetOperandKind.Unclassified];

    [Fact]
    public void WithinOneDirection_NoReceivingKindIsAdmittedByTwoFormats()
    {
        foreach (var dir in new[] { SetDirections.To, SetDirections.UpDown })
        {
            var seen = new Dictionary<SetOperandKind, SetFormat>();
            foreach (var row in SetFormatSelection.Rows.Where(r => (r.Dir & dir) != 0))
                foreach (var kind in row.Admits)
                {
                    Assert.False(seen.TryGetValue(kind, out var other),
                        $"{dir}: {kind} is admitted by both {other} and {row.Format}; ISO §2023:14.9.39.2's receiving "
                        + "braces are disjoint and an overlap makes the selection order-dependent again");
                    seen[kind] = row.Format;
                }
        }
    }

    [Fact]
    public void EveryClassifiableReceivingKind_IsAdmittedBySomeFormat()
    {
        var admitted = SetFormatSelection.Rows.SelectMany(r => r.Admits).ToHashSet();
        foreach (var kind in Enum.GetValues<SetOperandKind>())
        {
            if (NotAFormatOfItsOwn.Contains(kind)) continue;
            Assert.True(admitted.Contains(kind),
                $"{kind} is a receiving kind the classifier can produce and no general format admits it — a "
                + "statement written with it can only reach the COBOL2112 residual arm");
        }
    }

    /// <summary>ISO §2023:14.9.39.2 Format 1's sending brace is
    /// <c>{ arithmetic-expression-1 | index-name-2 | identifier-2 }</c>: §2023:8.8.1.1 admits numeric operands in an
    /// arithmetic expression and §2023:14.9.39.3 SR2 makes identifier-2 "a data item of class index". Those are the
    /// kinds a Format-1 statement may legally SEND, and the sender tie-break exists precisely for the kinds it
    /// may NOT — so a kind in both sets would take a legal <c>SET IX TO IDX</c> away from Format 1 and hand it
    /// to another format's receiving rule.</summary>
    [Fact]
    public void NoSenderKindTheFormatOneSendingBraceAdmits_AppearsInSendsOnly()
    {
        SetOperandKind[] formatOneCanSend =
        [
            SetOperandKind.IndexName, SetOperandKind.IndexDataItem,     // index-name-2 / identifier-2 (SR2)
            SetOperandKind.IntegerItem, SetOperandKind.OtherDataItem,   // arithmetic-expression-1 (§2023:8.8.1.1)
            SetOperandKind.CapacityRegister, SetOperandKind.DynamicLength,   // numeric / alphanumeric items
            SetOperandKind.Unclassified,                                // no evidence is never evidence
        ];
        foreach (var row in SetFormatSelection.Rows)
            foreach (var kind in row.SendsOnly)
                Assert.False(formatOneCanSend.Contains(kind),
                    $"{row.Format}'s SendsOnly names {kind}, which ISO §2023:14.9.39.2 Format 1's own sending brace "
                    + "admits — the tie-break would take a legal Format-1 statement to another format");
    }

    /// <summary>Each sender kind names at most ONE format, in one direction: the tie-break asks "which printed
    /// format's sending brace holds this category", and two answers would make it order-dependent — the very
    /// defect the receiving table was built to end.</summary>
    [Fact]
    public void WithinOneDirection_NoSenderKindNamesTwoFormats()
    {
        foreach (var dir in new[] { SetDirections.To, SetDirections.UpDown })
        {
            var seen = new Dictionary<SetOperandKind, SetFormat>();
            foreach (var row in SetFormatSelection.Rows.Where(r => (r.Dir & dir) != 0))
                foreach (var kind in row.SendsOnly)
                {
                    Assert.False(seen.TryGetValue(kind, out var other),
                        $"{dir}: a {kind} sender names both {other} and {row.Format}");
                    seen[kind] = row.Format;
                }
        }
    }

    /// <summary>kb/Work PB4954 — a row's introducing edition and its 1985 wording say the same thing: a row the 1985
    /// standard prints (Formats 1 and 2 of §1985:VI.6.23.2) carries 1985 wording, and a row introduced later has none,
    /// so a new row with no form for an edition is absent from that edition's list and cannot print other-edition
    /// wording.</summary>
    [Fact]
    public void ARowHas1985WordingExactlyWhenItIsIntroducedAt1985()
    {
        foreach (var (format, introducedIn, hasWording1985) in SetFormatSelection.RowEditions)
            Assert.True(hasWording1985 == (introducedIn <= 85),
                $"{format}: introduced in {introducedIn} but {(hasWording1985 ? "has" : "lacks")} 1985 wording");
    }

    /// <summary>The introducing edition of every row that the construct registry also gates is the registry's, not a
    /// second hand-kept year (one rule, one place).</summary>
    [Theory]
    [InlineData((int)SetFormat.F5, "set-object-reference-2002")]
    [InlineData((int)SetFormat.F8, "set-function-pointer-2014")]
    [InlineData((int)SetFormat.F9, "set-program-pointer-2002")]
    [InlineData((int)SetFormat.F10, "pointer-arithmetic-2002")]
    public void ARowsIntroducingEditionAgreesWithTheConstructRegistry(int format, string constructId)
    {
        var row = SetFormatSelection.RowEditions.Single(r => r.Format == (SetFormat)format);
        var status = WiseOwl.Cobol.Editions.ConstructRegistry.Find(constructId);
        Assert.NotNull(status);
        Assert.Equal(status.IntroducedIn, row.IntroducedIn);
    }

    [Fact]
    public void FormatOne_IsTheLastRow_SoTheNearestMatchPrefersACarrierFormat()
    {
        var rows = SetFormatSelection.Rows.ToList();
        Assert.Equal(SetFormat.F1, rows[^1].Format);
    }

    [Theory]
    // Every pair of receiving kinds that can be written in one statement, in BOTH orders — the property the
    // whole mechanism exists for (kb/Work PB449: "compile each mixed list in both orders and assert the two
    // diagnostics are the same").
    [InlineData((int)SetOperandKind.DataPointer, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.ProgramPointer, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.FunctionPointer, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.ObjectReference, (int)SetOperandKind.IntegerItem)]
    // Format 17 (message-tag) — the DECLINED facility that had no row at all and was therefore selected by
    // nothing, so a Format-17 statement fell to the NEAREST row and drew another format's syntax rule
    // (kb/Work PB453). Its kind is pinned here for the same reason the carriers are: the whole point of the
    // table is that a mixed list answers the same however it is written.
    [InlineData((int)SetOperandKind.MessageTag, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.CapacityRegister, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.DynamicLength, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.IndexName, (int)SetOperandKind.DataPointer)]
    [InlineData((int)SetOperandKind.IndexName, (int)SetOperandKind.IntegerItem)]
    [InlineData((int)SetOperandKind.Unclassified, (int)SetOperandKind.DataPointer)]
    public void SelectionDoesNotDependOnTheOrderTheOperandsAreWritten(int aKind, int bKind)
    {
        var a = (SetOperandKind)aKind;
        var b = (SetOperandKind)bKind;
        foreach (var dir in new[] { SetDirections.To, SetDirections.UpDown })
        {
            var forward = SetFormatSelection.Select([a, b], dir, out bool exactForward);
            var reversed = SetFormatSelection.Select([b, a], dir, out bool exactReversed);
            Assert.Equal(forward, reversed);
            Assert.Equal(exactForward, exactReversed);
        }
    }

    [Fact]
    public void UpDownBy_AdmitsNoIntegerOrIndexDataItemReceiver()
    {
        // ISO §2023:14.9.39.2 Format 2 is `SET { index-name-3 } … {UP BY|DOWN BY} arithmetic-expression-2` and
        // §2023:14.9.39.4 GR4 is "For each occurrence of index-name-3": there is no identifier alternative, and
        // Formats 10 and 14 take a data-pointer and a capacity register. Nothing else has a format.
        Assert.Null(SetFormatSelection.Select([SetOperandKind.IntegerItem], SetDirections.UpDown, out _));
        Assert.Null(SetFormatSelection.Select([SetOperandKind.IndexDataItem], SetDirections.UpDown, out _));
        Assert.Null(SetFormatSelection.Select([SetOperandKind.OtherDataItem], SetDirections.UpDown, out _));
        Assert.Null(SetFormatSelection.Select([SetOperandKind.ObjectReference], SetDirections.UpDown, out _));
    }

    [Fact]
    public void AnEntirelyUnclassifiableList_StillReachesABinderThatReportsTheName()
    {
        // R30: a probe never diagnoses, so a statement of undefined names must still reach a format whose own
        // receiver resolution reports COBOL1639 — never the COBOL2112 residual, which would blame the
        // FORMAT for what is a typo.
        Assert.Equal(SetFormat.F1,
            SetFormatSelection.Select([SetOperandKind.Unclassified], SetDirections.To, out _));
        Assert.Equal(SetFormat.F2,
            SetFormatSelection.Select([SetOperandKind.Unclassified], SetDirections.UpDown, out _));
    }

    /// <summary>kb/Work PB4954 — COBOL2112's list of formats names only the formats the COMPILED edition prints.
    /// <c>SET WS-N UP BY 1</c> over an integer item is admitted by no UP/DOWN format at any edition; the message
    /// lists Format 2 (all), Format 10 (from 2002) and Format 14 (from 2014) as the edition has them.</summary>
    [Theory]
    [InlineData(85, new[] { "Format 2's" }, new[] { "Format 10's", "Format 14's" })]
    [InlineData(2002, new[] { "Format 2's", "Format 10's" }, new[] { "Format 14's" })]
    [InlineData(2014, new[] { "Format 2's", "Format 10's", "Format 14's" }, new string[0])]
    [InlineData(2023, new[] { "Format 2's", "Format 10's", "Format 14's" }, new string[0])]
    public void TheNoFormatListNamesOnlyTheFormatsTheEditionPrints(int dialect, string[] named, string[] absent)
    {
        string id = $"PB4954-{dialect}";
        string src = Path.Combine(TempDir, $"{id}.cob");
        File.WriteAllLines(src,
        [
            "       IDENTIFICATION DIVISION.", $"       PROGRAM-ID. {id}.", "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.", "       01 WS-N PIC 9(4) VALUE 1.", "       PROCEDURE DIVISION.",
            "           SET WS-N UP BY 1", "           STOP RUN.",
        ]);
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: dialect,
            SourceFormat: WiseOwl.Cobol.Frontend.Preprocessor.InitialReferenceFormat.Auto));
        string message = Assert.Single(r.Errors, e => e.Contains("COBOL2112", StringComparison.Ordinal));
        foreach (var n in named) Assert.Contains(n, message, StringComparison.Ordinal);
        foreach (var a in absent) Assert.DoesNotContain(a, message, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB5015 — a statement can SELECT a row its edition does not print, because selection reads
    /// the operands' kinds: a dynamic-length item declares from 2014 while Format 16 begins at 2023, and a CAPACITY
    /// register declared at 2002 (refused COBOL0900, still bound) selects Format 14, which begins at 2014. The
    /// COBOL2112 that names the selected row labels it with its introducing edition below that edition and prints it
    /// plain from it, on both arms that name a selected row (an operand the brace does not admit, and a second
    /// receiver of a one-operand format).</summary>
    [Theory]
    [InlineData("D-ITEM PIC X DYNAMIC LENGTH", "SET D-ITEM N TO 5", 2014, "Format 16's", "COBOL-2023")]
    [InlineData("D-ITEM PIC X DYNAMIC LENGTH", "SET D-ITEM N TO 5", 2023, "Format 16's", null)]
    [InlineData("D-ITEM PIC X DYNAMIC LENGTH", "SET D-ITEM D-ITEM TO 5", 2014, "Format 16's", "COBOL-2023")]
    [InlineData("D-ITEM PIC X DYNAMIC LENGTH", "SET D-ITEM D-ITEM TO 5", 2023, "Format 16's", null)]
    [InlineData("T. 05 TE PIC X OCCURS DYNAMIC CAPACITY IN TC", "SET TC N TO 5", 2002, "Format 14's", "COBOL-2014")]
    [InlineData("T. 05 TE PIC X OCCURS DYNAMIC CAPACITY IN TC", "SET TC N TO 5", 2014, "Format 14's", null)]
    public void ASelectedRowTheEditionLacks_IsLabelledWithItsIntroducingEdition(
        string entry, string statement, int dialect, string format, string? introducedIn)
    {
        string id = $"PB5015-{dialect}";
        string src = Path.Combine(TempDir, $"{id}-{entry.Length}-{statement.Length}.cob");
        File.WriteAllLines(src,
        [
            "       IDENTIFICATION DIVISION.", $"       PROGRAM-ID. {id}.", "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.", $"       01 {entry}.", "       01 N PIC 9(4).",
            "       PROCEDURE DIVISION.", "       P1.", $"           {statement}.", "           STOP RUN.",
        ]);
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: dialect,
            SourceFormat: WiseOwl.Cobol.Frontend.Preprocessor.InitialReferenceFormat.Auto));
        string message = Assert.Single(r.Errors, e => e.Contains("COBOL2112", StringComparison.Ordinal));
        Assert.Contains(format, message, StringComparison.Ordinal);
        const string Label = "absent from this edition";
        if (introducedIn is null)
            Assert.DoesNotContain(Label, message, StringComparison.Ordinal);
        else
            Assert.Contains($"(a format first printed in {introducedIn}, {Label})", message, StringComparison.Ordinal);
    }
}
