// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A LEVEL-66 ENTRY HAS A PLACE IN THE PLACE TABLE, SO EVERY CLAUSE THAT NAMES ONE BY ITEM FINDS ITS STORAGE (kb/Work
/// PB5801). A level-66 entry is a data item with the attributes of what it renames — "all of the data attributes of
/// data-name-2 become the data attributes for data-name-1" (§1985:VI.5.11.4 GR2; §2023:13.18.45.4 GR1) — so FILE STATUS,
/// RELATIVE KEY, LINAGE and its FOOTING/TOP/BOTTOM, RECORD VARYING DEPENDING ON, ASSIGN USING and a report's CONTROL may
/// name one. Those clauses ask <c>PlaceTable</c> by item, and the table's item set (<c>DataBinder.PlaceableItems</c>)
/// walked the storage tree, where a level-66 entry is not (it rides <see cref="DataItem.Renames66"/>): each such program
/// ended the compiler with an unhandled <see cref="InvalidOperationException"/> during C# emission.
/// <para>The first fact is the general one, so a clause operand kind added later is covered without a new line here:
/// every item a data-name can reference (<see cref="DataItem.SelfDescendantsAndAliasesOf"/>) has a table entry. The
/// second pins the by-item clause operands of today, THROUGH form included: each has a place, and a no-THROUGH alias's
/// place denotes the alias itself (kb/Work PB602).</para>
/// </summary>
public sealed class RenamesEntryPlaceDriftTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5801A.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN USING NM66
                       FILE STATUS IS FS66.
                   SELECT F2 ASSIGN TO "pb5801b.dat"
                       ORGANIZATION IS RELATIVE
                       ACCESS MODE IS RANDOM
                       RELATIVE KEY IS RK66
                       FILE STATUS IS FT66.
                   SELECT RPF ASSIGN TO "pb5801c.rpt".
               DATA DIVISION.
               FILE SECTION.
               FD F1 RECORD IS VARYING IN SIZE FROM 1 TO 20 DEPENDING ON LN66
                     LINAGE IS LG66 LINES WITH FOOTING AT FT-66
                     LINES AT TOP TP66 LINES AT BOTTOM BT66.
               01 R1 PIC X(20).
               FD F2.
               01 R2 PIC X(10).
               FD RPF REPORT IS RP.
               WORKING-STORAGE SECTION.
               01 G1.
                  05 G-NM PIC X(12) VALUE "pb5801a.dat".
                  05 G-FS PIC XX.
                  05 G-T1 PIC X.
                  05 G-T2 PIC X.
                  05 G-RK PIC 9(3) VALUE 1.
                  05 G-LN PIC 9(2) VALUE 5.
                  05 G-LG PIC 9(2) VALUE 10.
                  05 G-FT PIC 9(2) VALUE 8.
                  05 G-TP PIC 9(2) VALUE 1.
                  05 G-BT PIC 9(2) VALUE 1.
                  05 G-CX PIC X VALUE "A".
               66 NM66 RENAMES G-NM.
               66 FS66 RENAMES G-FS.
               66 FT66 RENAMES G-T1 THRU G-T2.
               66 RK66 RENAMES G-RK.
               66 LN66 RENAMES G-LN.
               66 LG66 RENAMES G-LG.
               66 FT-66 RENAMES G-FT.
               66 TP66 RENAMES G-TP.
               66 BT66 RENAMES G-BT.
               66 CX66 RENAMES G-CX.
               REPORT SECTION.
               RD RP CONTROL IS CX66.
               01 DET TYPE IS DETAIL.
                  02 LINE PLUS 1.
                     03 COLUMN 1 PIC X SOURCE CX66.
               PROCEDURE DIVISION.
                   OPEN OUTPUT F1 F2 RPF.
                   STOP RUN.
        """;

    private static BoundUnit Unit() => RepositoryCodecTests.Bind(Source, 2023).Units.Single(u => u.Name == "PB5801A");

    /// <summary>Every item a data-name can reference — the storage tree and each item's level-66 entries — has an entry in
    /// the place table: <see cref="PlaceTable.Of"/> never throws its "no Place was built" for one.</summary>
    [Fact]
    public void EveryReferenceableItem_HasAPlaceTableEntry_Level66Included()
    {
        var unit = Unit();
        var items = unit.Data.Forest.Roots.SelectMany(DataItem.SelfDescendantsAndAliasesOf).ToList();
        Assert.Equal(10, items.Count(i => i.Level == 66));
        var missing = new List<string>();
        foreach (var item in items)
            try { unit.Places.Of(item); }
            catch (InvalidOperationException) { missing.Add(item.CobolName ?? item.ClrName); }
        Assert.True(missing.Count == 0, "the place table has no entry for: " + string.Join(", ", missing));
    }

    /// <summary>Each by-item clause operand that names a level-66 entry — FILE STATUS (both forms), ASSIGN USING, RELATIVE
    /// KEY, RECORD VARYING DEPENDING ON, the four LINAGE operands and CONTROL — has a place, and a no-THROUGH alias's place
    /// denotes the alias while its attributes are the renamed item's (§2023:13.18.45.4 GR1).</summary>
    [Fact]
    public void EveryByItemClauseOperand_NamingALevel66Entry_HasItsPlace()
    {
        var unit = Unit();
        var files = unit.Data.Files.Declared;
        var f1 = files.Single(f => f.SelectName == "F1");
        var f2 = files.Single(f => f.SelectName == "F2");

        var operands = f1.ClauseOperandItems.Concat(f2.ClauseOperandItems).ToList();
        Assert.Equal(["NM66", "FS66", "LN66", "LG66", "FT-66", "TP66", "BT66", "FT66", "RK66"],
            operands.Select(i => i.CobolName));
        foreach (var operand in operands)
        {
            var place = unit.Places.Of(operand);
            Assert.True(place is not null, $"no place for the clause operand {operand.CobolName}");
            Assert.Same(operand, place!.DenotedItem);
            if (operand.Renames is { Thru: null, From: { } renamed }) Assert.Same(renamed, place.Item);
        }
        Assert.NotNull(unit.Places.FileStatusOf(f1));
        Assert.NotNull(unit.Places.FileStatusOf(f2));
        Assert.NotNull(unit.Places.RelativeKeyOf(f2));

        var control = Assert.Single(unit.Data.Reports.Declared.Single().Controls);
        Assert.Equal("CX66", control.Item?.CobolName);
        Assert.Same(control.Item, control.Place?.DenotedItem);
    }
}
