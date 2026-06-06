// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
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

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Phase B (procedure-division scan) + Phase C (cross-edge fixpoint) — ADR §3 triggers 3/4a/11/15
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static BoundIdentifierExpression Id(DataSymbol s, CobolCategory cat = CobolCategory.Alphanumeric,
        IReadOnlyList<BoundExpression>? subscripts = null)
        => new(s, cat, subscripts);

    private static BoundLiteralExpression Lit(long n)
        => new(n, CobolCategory.Numeric, n.ToString());

    private static BoundReferenceModificationExpression RefMod(DataSymbol s, CobolCategory cat,
        BoundExpression? start = null, BoundExpression? length = null)
        => new(Id(s, cat), start ?? Lit(1), length ?? Lit(2));

    private static BoundMoveStatement Move(BoundExpression source, BoundExpression target)
        => new(source, [target], isRounded: false);

    private static RecordClassification ClassifyProc(
        IReadOnlyList<DataSymbol> items, IEnumerable<BoundStatement> statements,
        Func<DataSymbol, CobolCategory>? categoryOf = null,
        Func<DataSymbol, (int Offset, int Length)?>? layoutOf = null)
        => Pass.Classify(items, categoryOf ?? (_ => CobolCategory.Alphanumeric), statements, layoutOf);

    // ── trigger 3: reference modification ──

    [Fact]
    public void RefMod_OfNumericDisplayItem_DemotesBase()
    {
        // MOVE WS-N(1:4) TO WS-X — the everyday refmod of a PIC 9(8); a typed long has no character image.
        var n = Item("WS-N", "9(8)");
        var x = Item("WS-X", "X(10)");
        var c = ClassifyProc([n, x], [Move(RefMod(n, CobolCategory.Numeric, Lit(1), Lit(4)), Id(x))],
            s => s == n ? CobolCategory.Numeric : CobolCategory.Alphanumeric);
        Assert.True(c.IsByteIsland(n));
        Assert.True(c.IsTyped(x));   // the elementary alphanumeric MOVE target is untouched
    }

    [Theory]
    [InlineData(CobolCategory.Alphanumeric)]
    [InlineData(CobolCategory.Alphabetic)]
    [InlineData(CobolCategory.National)]
    public void RefMod_OfHomogeneousStringField_StaysTyped(CobolCategory stringCategory)
    {
        var a = Item("WS-A", "X(10)");
        var x = Item("WS-X", "X(10)");
        var c = ClassifyProc([a, x], [Move(RefMod(a, stringCategory, Lit(1), Lit(3)), Id(x))],
            s => s == a ? stringCategory : CobolCategory.Alphanumeric);
        Assert.True(c.IsTyped(a));
    }

    [Fact]
    public void RefMod_OfGroup_DemotesGroupAndChildren()
    {
        var grp = Item("WS-GRP", null, level: 1);
        var f = Item("WS-F", "X(4)");
        grp.AddChild(f);
        var x = Item("WS-X", "X(10)");
        var c = ClassifyProc([grp, f, x], [Move(RefMod(grp, CobolCategory.Alphanumeric), Id(x))]);
        Assert.True(c.IsByteIsland(grp));
        Assert.True(c.IsByteIsland(f));   // downward-transitive
        Assert.True(c.IsTyped(x));
    }

    [Fact]
    public void RefMod_NestedInsideIfBranch_IsStillFound()
    {
        // the walker must recurse into IF/THEN; the refmod of a numeric DISPLAY item still demotes it.
        var n = Item("WS-N", "9(6)");
        var x = Item("WS-X", "X(6)");
        var move = Move(RefMod(n, CobolCategory.Numeric, Lit(1), Lit(3)), Id(x));
        var iff = new BoundIfStatement(Lit(1), [move], elseStatements: null);
        var c = ClassifyProc([n, x], [iff], s => s == n ? CobolCategory.Numeric : CobolCategory.Alphanumeric);
        Assert.True(c.IsByteIsland(n));
    }

    // ── trigger 11: CALL … USING … BY REFERENCE ──

    [Fact]
    public void CallByReference_DemotesArgument()
    {
        var p = Item("WS-P", "X(4)");
        var call = new BoundCallStatement("SUB", isDynamic: false,
            [new BoundCallArgument(ParameterMode.ByReference, Id(p))],
            returningTarget: null, onException: [], notOnException: []);
        Assert.True(ClassifyProc([p], [call]).IsByteIsland(p));
    }

    [Theory]
    [InlineData(ParameterMode.ByContent)]
    [InlineData(ParameterMode.ByValue)]
    public void CallByContentOrValue_KeepsArgumentTyped(ParameterMode mode)
    {
        var p = Item("WS-P", "X(4)");
        var call = new BoundCallStatement("SUB", isDynamic: false,
            [new BoundCallArgument(mode, Id(p))],
            returningTarget: null, onException: [], notOnException: []);
        Assert.True(ClassifyProc([p], [call]).IsTyped(p));
    }

    // ── trigger 15: ODO whole-group operand ──

    private static (DataSymbol Group, DataSymbol Counter, DataSymbol Table) OdoGroup()
    {
        var grp = new DataSymbol("WS-GRP", "WS-GRP", 1, null, UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage };
        var cnt = new DataSymbol("WS-CNT", "WS-CNT", 5, "9(2)", UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage };
        var tbl = new DataSymbol("WS-TBL", "WS-TBL", 5, "X(4)", UsageKind.Display, null, null, 0)
        {
            Area = StorageAreaKind.WorkingStorage,
            Occurs = new OccursInfo(1, 5, dependingOnName: "WS-CNT") { DependingOnSymbol = cnt },
        };
        grp.AddChild(cnt);
        grp.AddChild(tbl);
        return (grp, cnt, tbl);
    }

    [Fact]
    public void OdoGroup_UsedAsWholeOperand_IsDemoted()
    {
        var (grp, cnt, tbl) = OdoGroup();
        var x = Item("WS-X", "X(30)");
        // MOVE WS-GRP TO WS-X — the ODO group is the whole sender (uses current count).
        var c = ClassifyProc([grp, cnt, tbl, x], [Move(Id(grp), Id(x))]);
        Assert.True(c.IsByteIsland(grp));
        Assert.True(c.IsByteIsland(cnt));   // downward-transitive
        Assert.True(c.IsByteIsland(tbl));
    }

    [Fact]
    public void OdoGroup_OnlyElementAccessed_StaysTyped()
    {
        var (grp, cnt, tbl) = OdoGroup();
        var x = Item("WS-X", "X(4)");
        // MOVE WS-TBL(1) TO WS-X — only an element is referenced; the whole group is never an operand.
        var c = ClassifyProc([grp, cnt, tbl, x], [Move(Id(tbl, CobolCategory.Alphanumeric, [Lit(1)]), Id(x))]);
        Assert.True(c.IsTyped(grp));
        Assert.True(c.IsTyped(tbl));
    }

    // ── trigger 4a: group MOVE — dissimilar layout demotes the destination, identical stays typed ──

    private static (DataSymbol Group, DataSymbol A, DataSymbol B) TwoFieldGroup(string name, int baseOffset)
    {
        var grp = new DataSymbol(name, name, 1, null, UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage };
        var a = new DataSymbol(name + "-A", name + "-A", 5, "X(4)", UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage };
        var b = new DataSymbol(name + "-B", name + "-B", 5, "X(4)", UsageKind.Display, null, null, 0)
        { Area = StorageAreaKind.WorkingStorage };
        grp.AddChild(a);
        grp.AddChild(b);
        return (grp, a, b);
    }

    [Fact]
    public void GroupMove_NoLayoutAccessor_DemotesDestinationOnly()
    {
        var (g1, g1a, g1b) = TwoFieldGroup("G1", 0);
        var (g2, g2a, g2b) = TwoFieldGroup("G2", 100);
        // MOVE G1 TO G2 — without layout info the groups are treated as dissimilar (any doubt → byte).
        var c = ClassifyProc([g1, g1a, g1b, g2, g2a, g2b], [Move(Id(g1), Id(g2))]);
        Assert.True(c.IsByteIsland(g2));   // destination holds the raw moved image
        Assert.True(c.IsByteIsland(g2a));
        Assert.True(c.IsTyped(g1));        // source materializes its byte image on demand — stays typed
    }

    [Fact]
    public void GroupMove_IdenticalLayout_StaysTyped()
    {
        var (g1, g1a, g1b) = TwoFieldGroup("G1", 0);
        var (g2, g2a, g2b) = TwoFieldGroup("G2", 100);
        var layout = Layout((g1, 0, 8), (g1a, 0, 4), (g1b, 4, 4), (g2, 100, 8), (g2a, 100, 4), (g2b, 104, 4));
        // MOVE G1 TO G2 — identical layouts → a value-type struct copy; both stay typed.
        var c = ClassifyProc([g1, g1a, g1b, g2, g2a, g2b], [Move(Id(g1), Id(g2))], layoutOf: layout);
        Assert.True(c.IsTyped(g1));
        Assert.True(c.IsTyped(g2));
    }

    [Fact]
    public void GroupMove_DiffersInOffsetOnly_DemotesDestination()
    {
        // identical declared fields but a SYNC gap shifts G2-B by one byte → dissimilar (ADR §3.4).
        var (g1, g1a, g1b) = TwoFieldGroup("G1", 0);
        var (g2, g2a, g2b) = TwoFieldGroup("G2", 100);
        var layout = Layout((g1, 0, 8), (g1a, 0, 4), (g1b, 4, 4), (g2, 100, 9), (g2a, 100, 4), (g2b, 105, 4));
        var c = ClassifyProc([g1, g1a, g1b, g2, g2a, g2b], [Move(Id(g1), Id(g2))], layoutOf: layout);
        Assert.True(c.IsByteIsland(g2));
        Assert.True(c.IsTyped(g1));
    }

    [Fact]
    public void GroupMove_IdenticalLayout_ByteSource_PropagatesToDestination_PhaseC()
    {
        // G1 is a file record (byte by Phase A trigger 5); an identical-layout MOVE makes G2 byte too, because a
        // struct copy is byte-exact only when both ends share the byte representation (ADR §3.4 / §2.4 cross-edge).
        var (g1, g1a, g1b) = TwoFieldGroup("G1", 0);
        foreach (var s in new[] { g1, g1a, g1b }) s.Area = StorageAreaKind.FileSection;
        var (g2, g2a, g2b) = TwoFieldGroup("G2", 100);
        var layout = Layout((g1, 0, 8), (g1a, 0, 4), (g1b, 4, 4), (g2, 100, 8), (g2a, 100, 4), (g2b, 104, 4));
        var c = ClassifyProc([g1, g1a, g1b, g2, g2a, g2b], [Move(Id(g1), Id(g2))], layoutOf: layout);
        Assert.True(c.IsByteIsland(g1));   // Phase A: file record
        Assert.True(c.IsByteIsland(g2));   // Phase C: propagated across the struct-copy edge
        Assert.True(c.IsByteIsland(g2a));  // and downward
    }

    // ── group COMPARE / class-condition do NOT demote (materialize on demand, ADR §3.4) ──

    [Fact]
    public void GroupComparison_DoesNotDemote()
    {
        var (g1, g1a, g1b) = TwoFieldGroup("G1", 0);
        var (g2, g2a, g2b) = TwoFieldGroup("G2", 100);
        // IF G1 = G2 — a group relational comparison materializes byte images on demand; neither is demoted.
        var cmp = new BoundBinaryExpression(Id(g1), BoundBinaryOperatorKind.Equal, Id(g2), CobolCategory.Unknown);
        var iff = new BoundIfStatement(cmp, [new BoundStopStatement()], elseStatements: null);
        var c = ClassifyProc([g1, g1a, g1b, g2, g2a, g2b], [iff]);
        Assert.True(c.IsTyped(g1));
        Assert.True(c.IsTyped(g2));
    }

    private static Func<DataSymbol, (int, int)?> Layout(
        params (DataSymbol Sym, int Offset, int Length)[] entries)
    {
        var map = new Dictionary<DataSymbol, (int, int)>(ReferenceEqualityComparer.Instance);
        foreach (var (sym, off, len) in entries)
            map[sym] = (off, len);
        return s => map.TryGetValue(s, out var v) ? v : null;
    }
}
