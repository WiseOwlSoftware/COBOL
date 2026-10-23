// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LEVEL-66 RENAMES ... THROUGH ALIAS IS A WHOLE-GROUP OPERAND AT EVERY OPERAND POSITION (kb/Work PB3563).
/// ISO §13.18.45.4 GR2 makes the alias "an alphanumeric group item that includes all elementary items" of its span,
/// and §14.9.25.4 GR4 makes a move that involves a group "an alphanumeric to alphanumeric elementary move, except that
/// there is no conversion of data from one form of internal representation to another". A numeric DISPLAY leaf the
/// alias spans therefore holds whatever characters arrive through the alias, which a native value carrier cannot —
/// so the leaf stores its character image, exactly as the leaves of a plain group used as a whole do. The alias is
/// not STRUCTURALLY a group (<see cref="DataItem.IsGroup"/> is false for a 66), and <c>UsageCollectionPass</c> asked
/// only that, so the leaves kept their carrier and <c>MOVE "XXXXXXX" TO R-AC</c> over <c>X(2) 9(3) X(2)</c> stored
/// <c>XX000XX</c>.
/// <para>These tests pin the operand positions one by one (the pass has a visitor method per statement, and a
/// position added without the alias arm is the sibling bug), and the two controls that must NOT promote: a
/// no-THROUGH alias, which forwards to the renamed item and is elementary exactly when that item is, and an alias no
/// statement names.</para>
/// </summary>
public sealed class RenamesAliasImageTests
{
    private const string Data = """
        01 R.
           05 R-A PIC X(2) VALUE "AB".
           05 R-B PIC 9(3) VALUE 123.
           05 R-C PIC X(2) VALUE "CD".
        66 R-AC RENAMES R-A THROUGH R-C.
        66 R-ONE RENAMES R-B.
        01 SAVE PIC X(7).
        """;

    /// <summary>Each statement names the THROUGH alias in one operand position of <c>UsageCollectionPass</c>; the
    /// numeric leaf <c>R-B</c> must then store its image.</summary>
    [Theory]
    [InlineData("MOVE \"XXXXXXX\" TO R-AC")]
    [InlineData("MOVE R-AC TO SAVE")]
    [InlineData("DISPLAY R-AC")]
    [InlineData("UNSTRING SAVE DELIMITED BY SPACE INTO R-AC")]
    [InlineData("ACCEPT R-AC")]
    [InlineData("CALL \"SUBPROG\" USING BY REFERENCE R-AC")]
    [InlineData("IF R-AC = SAVE CONTINUE END-IF")]
    [InlineData("INSPECT R-AC REPLACING ALL \"A\" BY \"Z\"")]
    [InlineData("STRING \"A\" DELIMITED BY SIZE INTO R-AC")]
    [InlineData("MOVE SPACES TO R-AC")]
    public void ThroughAlias_AtAnyOperandPosition_PromotesTheSpannedNumericLeaf(string statement)
    {
        var d = BindFull(statement);
        Assert.True(Item(d, "R-B").StoreAsImage, $"'{statement}' left R-B on a native carrier");
    }

    [Fact]
    public void ThroughAlias_PromotesNoLeafOutsideItsSpan()
    {
        var d = BindFull("MOVE \"XXXXXXX\" TO R-AC", extraData: "01 OUTSIDE PIC 9(3) VALUE 1.");
        Assert.False(Item(d, "OUTSIDE").StoreAsImage);
    }

    /// <summary>§13.18.45.4 GR1: a RENAMES without THROUGH gives data-name-1 the attributes of data-name-2 — here an
    /// elementary numeric item — so an elementary numeric move through it keeps the native carrier.</summary>
    [Fact]
    public void NoThroughAlias_OfAnElementaryNumeric_IsElementary_AndKeepsItsNativeCarrier()
    {
        var d = BindFull("MOVE 5 TO R-ONE");
        Assert.False(Item(d, "R-B").StoreAsImage);
    }

    /// <summary>The cost control: a program that never names the alias keeps the numeric leaf native.</summary>
    [Fact]
    public void ThroughAlias_NoStatementNames_KeepsTheNativeCarrier()
    {
        var d = BindFull("MOVE 5 TO R-B");
        Assert.False(Item(d, "R-B").StoreAsImage);
    }

    /// <summary>⛔ THE LEAF'S CLASS DOES NOT DECIDE (kb/Work PB3563, review round 1): a numeric leaf that is the
    /// canonical member of a REDEFINES class is promoted by the alias exactly as the leaf of a plain group is, because
    /// arm (4) of the promotion union (the elementary character channel) once skipped every class member while arm (3)
    /// (the whole group) never did. ISO 13.18.45.4 GR2 and 14.9.25.4 GR4 do not exempt a redefined item.</summary>
    [Theory]
    [InlineData("MOVE \"XXXXXXX\" TO R-AC")]
    [InlineData("DISPLAY R-AC")]
    public void ThroughAlias_OverARedefinedNumericLeaf_PromotesIt(string statement)
    {
        var d = BindFull(statement, data: RedefinedData);
        Assert.True(Item(d, "R-B").StoreAsImage, $"'{statement}' left the REDEFINES-class leaf R-B on a native carrier");
    }

    /// <summary>The elementary twin of the same rule, no alias involved: a group MOVE into a REDEFINES-class numeric
    /// receiver is a character channel (14.9.25.4 GR4) and promotes that receiver.</summary>
    [Fact]
    public void GroupMoveIntoARedefinedNumericReceiver_PromotesIt()
    {
        var d = BindFull("MOVE SAVE-GROUP TO R-B", data: RedefinedData);
        Assert.True(Item(d, "R-B").StoreAsImage);
    }

    private const string RedefinedData = """
        01 R.
           05 R-A PIC X(2) VALUE "AB".
           05 R-B PIC 9(3) VALUE 123.
           05 R-B2 REDEFINES R-B PIC 9V99.
           05 R-C PIC X(2) VALUE "CD".
        66 R-AC RENAMES R-A THROUGH R-C.
        01 SAVE-GROUP.
           05 SAVE-1 PIC X(3) VALUE "ABC".
        """;

    private static DataItem Item(DataBinder d, string name) => d.ByName[name][0];

    private static DataBinder BindFull(string statement, string extraData = "", string? data = null)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TRENAMES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {data ?? Data}
            {extraData}
            PROCEDURE DIVISION.
            MAIN-PARA.
                {statement}
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_renalias_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var bound = new BinderDriver().Bind(tree!, new EditionContext(2023));
            return bound.Units[0].Data;
        }
        finally { FileHarness.TryDelete(path); }
    }
}
