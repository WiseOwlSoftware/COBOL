// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Tests for the Stage-2 <see cref="RecordClassificationPass"/> (data-model migration, ADR §3) — the Phase-A
/// data-division triggers plus the REDEFINES-class / downward-transitivity fixpoint. The pass is additive and
/// not yet consumed by codegen; these tests pin the classification logic before any Stage-3 typed flip.
/// </summary>
public sealed class RecordClassificationPassTests
{
    private static readonly RecordClassificationPass Pass = new();

    private static DataSymbol Item(string name, string? pic, int level = 5, UsageKind usage = UsageKind.Display)
        => new(name, name, level, pic, usage, null, null, 0) { Area = StorageAreaKind.WorkingStorage };

    private static RecordClassification Classify(
        IReadOnlyList<DataSymbol> items, Func<DataSymbol, CobolCategory>? categoryOf = null)
        => Pass.Classify(items, categoryOf ?? (_ => CobolCategory.Alphanumeric));

    // ── default: a plain elementary item with no trigger is typed ──

    [Fact]
    public void PlainAlphanumericItems_AreTyped()
    {
        var x = Item("WS-X", "X(5)");
        var y = Item("WS-Y", "X(10)");
        var c = Classify([x, y]);
        Assert.True(c.IsTyped(x));
        Assert.True(c.IsTyped(y));
        Assert.Equal(0, c.ByteIslandCount);
    }

    // ── trigger 1: REDEFINES — redefiner and target are one island (both directions) ──

    [Fact]
    public void Redefines_DemotesBothRedefinerAndTarget()
    {
        var num = Item("PACKED-DATE", "9(8)");
        var alt = Item("ALT-DATE", "X(8)");
        alt.Redefines = num;
        var c = Classify([num, alt]);
        Assert.True(c.IsByteIsland(num));
        Assert.True(c.IsByteIsland(alt));
    }

    [Fact]
    public void Redefines_PropagatesToSubordinatesOfBoth()
    {
        var baseGrp = Item("BASE", null, level: 5);
        var baseChild = Item("BASE-FLD", "9(4)");
        baseGrp.AddChild(baseChild);
        var view = Item("VIEW", null, level: 5);
        var viewChild = Item("VIEW-FLD", "X(4)");
        view.AddChild(viewChild);
        view.Redefines = baseGrp;

        var c = Classify([baseGrp, baseChild, view, viewChild]);
        Assert.True(c.IsByteIsland(baseGrp));
        Assert.True(c.IsByteIsland(baseChild));   // downward-transitive
        Assert.True(c.IsByteIsland(view));
        Assert.True(c.IsByteIsland(viewChild));   // downward-transitive
    }

    [Fact]
    public void Redefines_Chain_DemotesWholeClass()
    {
        // B is the base; A redefines B; C redefines A — the whole equivalence class is one island.
        var b = Item("B", "9(8)");
        var a = Item("A", "X(8)");
        a.Redefines = b;
        var cc = Item("C", "X(8)");
        cc.Redefines = a;
        var c = Classify([b, a, cc]);
        Assert.True(c.IsByteIsland(b));
        Assert.True(c.IsByteIsland(a));
        Assert.True(c.IsByteIsland(cc));
    }

    // ── trigger 2: RENAMES (level 66) — the renaming item and its spanned range ──

    [Fact]
    public void Renames_WithoutThru_DemotesOnlyRenamingItemAndFrom()
    {
        var f1 = Item("F1", "X(3)");
        var f2 = Item("F2", "X(3)");
        var r = new DataSymbol("R66", "R66", 66, null, UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage, Renames = new RenamesInfo("F1") { FromSymbol = f1 } };

        var c = Classify([f1, f2, r]);
        Assert.True(c.IsByteIsland(r));
        Assert.True(c.IsByteIsland(f1));
        Assert.True(c.IsTyped(f2));   // no THRU → only the FROM item is in the renamed view
    }

    [Fact]
    public void Renames_DemotesRenamingItemAndSpannedRange()
    {
        var f1 = Item("F1", "X(3)");
        var f2 = Item("F2", "9(3)");
        var f3 = Item("F3", "X(3)");
        var outside = Item("F4", "X(3)");
        var r = new DataSymbol("R66", "R66", 66, null, UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage, Renames = new RenamesInfo("F1", "F3") { FromSymbol = f1, ThruSymbol = f3 } };

        var c = Classify([f1, f2, f3, outside, r]);
        Assert.True(c.IsByteIsland(r));
        Assert.True(c.IsByteIsland(f1));
        Assert.True(c.IsByteIsland(f2));   // inside the F1..F3 span
        Assert.True(c.IsByteIsland(f3));
        Assert.True(c.IsTyped(outside));   // F4 is outside the renamed range
    }

    // ── trigger 5: FD/SD file record storage ──

    [Fact]
    public void FileSectionRecord_AndChildren_AreByteIslands()
    {
        var rec = Item("FILE-REC", null, level: 1);
        rec.Area = StorageAreaKind.FileSection;
        var fld = Item("FILE-FLD", "X(20)");
        fld.Area = StorageAreaKind.FileSection;
        rec.AddChild(fld);

        var c = Classify([rec, fld]);
        Assert.True(c.IsByteIsland(rec));
        Assert.True(c.IsByteIsland(fld));
    }

    // ── trigger 12: LINKAGE-section items ──

    [Fact]
    public void LinkageSectionItem_IsByteIsland()
    {
        var l = Item("LK-PARM", "X(8)");
        l.Area = StorageAreaKind.LinkageSection;
        Assert.True(Classify([l]).IsByteIsland(l));
    }

    // ── trigger 8: IS EXTERNAL / IS GLOBAL ──

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ExternalOrGlobal_IsByteIsland(bool external, bool global)
    {
        var s = Item("SHARED", "X(4)");
        s.IsExternal = external;
        s.IsGlobal = global;
        Assert.True(Classify([s]).IsByteIsland(s));
    }

    // ── trigger 13: edited items ──

    [Theory]
    [InlineData(CobolCategory.NumericEdited)]
    [InlineData(CobolCategory.AlphanumericEdited)]
    [InlineData(CobolCategory.NationalEdited)]
    public void EditedItem_IsByteIsland(CobolCategory editedCategory)
    {
        var e = Item("WS-EDIT", "ZZ9.99");
        var c = Classify([e], s => s == e ? editedCategory : CobolCategory.Alphanumeric);
        Assert.True(c.IsByteIsland(e));
    }

    // ── mixed model: a typed group keeps typed children; only the triggered child is an island ──

    [Fact]
    public void TypedGroup_WithOneEditedChild_KeepsOtherChildrenTyped()
    {
        var grp = Item("REPORT-LINE", null, level: 1);
        var plain = Item("R-NAME", "X(20)");
        var edited = Item("R-AMOUNT", "ZZ9.99");
        grp.AddChild(plain);
        grp.AddChild(edited);

        var c = Classify([grp, plain, edited], s => s == edited ? CobolCategory.NumericEdited : CobolCategory.Alphanumeric);
        Assert.True(c.IsTyped(grp));     // group itself has no trigger
        Assert.True(c.IsTyped(plain));   // plain alphanumeric stays typed
        Assert.True(c.IsByteIsland(edited));
    }

    // ── conservative default: an unseen item is byte (any doubt → byte) ──

    [Fact]
    public void UnseenItem_DefaultsToByteIsland()
    {
        var unseen = Item("GHOST", "X(1)");
        Assert.True(Classify([]).IsByteIsland(unseen));
    }
}
