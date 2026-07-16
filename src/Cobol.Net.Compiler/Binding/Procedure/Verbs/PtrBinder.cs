// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The data-pointer statement slice (Phase-4b increment 2 — ISO §14.9.39 Formats 7/10, §14.9.3 ALLOCATE,
/// §14.9.15 FREE; the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). All semantic
/// resolution happens here once: receiver/sender category checks (the 0869 pointer band), the BASED-receiver
/// rule (SR18), edition gates via the registry (binder-side — a grammar predicate would fall through to the
/// OTHER SET alternatives and mis-diagnose), and the staged-loud residue (qualified/subscripted ADDRESS OF
/// operands).
/// P7 Step 10g: a real collaborator over <see cref="BinderContext"/> — the tri-state
/// <see cref="TryBindSetUpDown"/> contract (null = fall through to the index path · BoundNop = error
/// consumed · node = bound) and the non-consuming first-target peek move VERBATIM, as does the raw
/// <c>ctx.Data.ByName</c> lookup in <c>PtrResolveBased</c> (the documented SymbolTable bypass — the
/// convergence is a flagged behavior-sensitive follow-up, per the §Step 10 plan block).</summary>
internal sealed class PtrBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind SET Format 7 — both grammar alternatives of <c>setAddressStatement</c>:
    /// <c>SET ADDRESS OF based TO pointer</c> (receiver form; SR18 :31399 — the receiver SHALL be BASED, the
    /// IBM non-BASED-LINKAGE idiom is a rejected non-ISO extension) and <c>SET pointer TO ADDRESS OF x</c>
    /// (sender form — routes into the ONE pointer-SET node with the ADDRESS OF source leg).</summary>
    public BoundStatement BindSetAddress(Core.SetAddressStatementContext sa)
    {
        // SET ADDRESS OF (§14.9.39 Format 7) is a COBOL-2002 introduction; the edition gate moved to the post-bind
        // VersionConformancePass (PHASE-03 Step 14b) — it fires on BoundSetAddressOfBased (receiver form) and
        // BoundSetPointer{Address} (sender form).
        bool receiverForm = sa.GetChild(1) is Antlr4.Runtime.Tree.ITerminalNode { Symbol.Type: Core.ADDRESS };

        if (receiverForm)
        {
            // SET ADDRESS OF based-item TO pointer (GR12–13 — the address VALUE is assigned; a snapshot).
            var basedRef = sa.dataReference(0);
            var senderRef = sa.dataReference(1);
            if (PtrResolveBased(basedRef) is not { } based) return new BoundNop();   // 0869 reported
            if (PtrResolvePointer(senderRef, "the sender of SET ADDRESS OF (ISO §14.9.39 SR17)") is not { } src)
                return new BoundNop();
            return new BoundSetAddressOfBased(based, src);
        }

        // SET pointer TO ADDRESS OF identifier (§8.4.3.11 — the sender form).
        var targetRef = sa.dataReference(0);
        var addrRef = sa.dataReference(1);
        if (PtrResolvePointer(targetRef, "the receiver of SET … TO ADDRESS OF (ISO §14.9.39 Format 7)") is not { } tp)
            return new BoundNop();
        if (PtrBindAddressOf(addrRef) is not { } addr) return new BoundNop();
        return new BoundSetPointer([tp], null, ToNull: false, Address: addr);
    }

    /// <summary>Resolve an <c>ADDRESS OF identifier</c> operand (ISO §8.4.3.11): a BASED item's value is its
    /// implicit pointer (§8.6.5); an ordinary record must have been storage-forced onto a cell by the data
    /// pass (or be an EXTERNAL record — already cell-backed). Qualified/subscripted operands and un-forcible
    /// shapes (a rejected COMP-leaf class, an OCCURS-resident anchor, a carrier-resident LINKAGE formal)
    /// stage LOUD — never a pointer to the wrong storage.</summary>
    private BoundAddressOf? PtrBindAddressOf(Core.DataReferenceContext addrRef)
    {
        if (addrRef.ChildCount != 1)
        {
            ctx.Edition.Error("COBOLNET0869",
                $"ADDRESS OF '{addrRef.GetText()}': qualified or subscripted ADDRESS OF operands are a named "
                + "increment residue (ISO §8.4.3.11) — take the address of the containing record");
            return null;
        }
        if (ctx.Refs.Resolve(addrRef) is not { } place)
        {
            ctx.Edition.Error("COBOLNET0869", $"ADDRESS OF '{addrRef.GetText()}': unresolvable operand");
            return null;
        }
        var item = place.Item;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        bool cellBacked = root.Class is { Tier: RedefinesTier.StringCanonical } cls
            && (cls.BasedPointerField is not null
                || ctx.Data.PtrAddressableCellOf.ContainsKey(cls)
                || ctx.Data.CallExternalBackings.Any(b => b.BackingCsName == cls.BackingCsName));
        if (!cellBacked)
        {
            ctx.Edition.Error("COBOLNET0869",
                $"ADDRESS OF '{addrRef.GetText()}': the operand's record could not be placed on addressable "
                + "cell storage (a COMP/float/index leaf, an OCCURS-resident anchor, or a carrier-resident "
                + "LINKAGE formal — named increment residue; ISO §8.4.3.11)");
            return null;
        }
        return new BoundAddressOf(item);
    }

    /// <summary>Bind ALLOCATE (ISO §14.9.3, both formats). The INITIALIZED based form lowers per GR7 to the
    /// allocation followed by EXACTLY the spec's <c>INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO
    /// DEFAULT</c> expansion (the ONE INITIALIZE mechanism, <see cref="InitializeBinder.BindAllocateInitialized"/>),
    /// carried as a <see cref="BoundSequence"/>.</summary>
    public BoundStatement BindAllocate(Core.AllocateStatementContext al)
    {
        // ALLOCATE (§14.9.3) is a COBOL-2002 INTRODUCTION gate, now gated on RECOGNITION by the
        // VersionConformancePass parse-arm (VisitAllocateStatement, Step 14h.2) — so a below-2002 ALLOCATE is an
        // edition violation even when its RETURNING fails to resolve (SR3/0869), which a bound-arm gate lost when
        // binding errored to a BoundNop before a BoundAllocate was produced (the DEVLOG-724 CI-red finding).
        var drefs = al.dataReference();
        Place? returning = null;
        if (al.RETURNING() is not null)
        {
            if (PtrResolvePointer(drefs[^1], "ALLOCATE RETURNING (ISO §14.9.3 SR3 — category data-pointer)") is not { } rp)
                return new BoundNop();
            returning = rp;
        }

        if (al.CHARACTERS() is not null)
        {
            // Form 1: ALLOCATE arithmetic-expression CHARACTERS [INITIALIZED] RETURNING pointer.
            if (returning is null)
            {
                ctx.Edition.Error("COBOLNET0869",
                    "ALLOCATE … CHARACTERS requires the RETURNING phrase (ISO §14.9.3 SR2 — without a based "
                    + "item there is no other way to address the storage)");
                return new BoundNop();
            }
            return new BoundAllocate(null, host.Expr.BindExpr(al.arithmeticExpression()), al.INITIALIZED() is not null, returning);
        }

        // Form 2: ALLOCATE based-item [INITIALIZED] [RETURNING pointer].
        var basedRef = drefs[0];
        if (PtrResolveBased(basedRef) is not { } based) return new BoundNop();
        var alloc = new BoundAllocate(based, null, al.INITIALIZED() is not null, returning);
        if (al.INITIALIZED() is null) return alloc;
        // GR7: "the allocated storage is initialized as if an INITIALIZE data-name-1 WITH FILLER ALL TO VALUE
        // THEN TO DEFAULT statement were executed" — the lowering IS that statement's bind-time expansion,
        // sequenced AFTER the allocation so each store windows the cell the implicit pointer now addresses
        // (GR4a). The GR5 not-available leg (no storage to initialize) is unreachable in this managed model —
        // a form-2 request is the template width (> 0), and CobolPtr.Allocate always satisfies a positive size.
        return new BoundSequence([alloc, host.Init.BindAllocateInitialized(basedRef)]);
    }

    /// <summary>Bind FREE (ISO §14.9.15 SR1 — every operand shall be category data-pointer; the vendor
    /// <c>FREE based-item</c> form is rejected, never silently mis-freed).</summary>
    public BoundStatement BindFree(Core.FreeStatementContext fr)
    {
        // FREE (§14.9.15) is a COBOL-2002 introduction; edition gate moved to VersionConformancePass (Step 14b),
        // firing on the self-identifying BoundFree node.
        var operands = new List<Place>();
        foreach (var dref in fr.dataReference())
        {
            if (PtrResolvePointer(dref, "a FREE operand (ISO §14.9.15 SR1 — data-pointers only)") is not { } p)
                return new BoundNop();
            operands.Add(p);
        }
        return new BoundFree(operands);
    }

    /// <summary>The SET UP/DOWN BY pointer arm (ISO §14.9.39 Format 10; the D-U7 category re-route pattern —
    /// the 85 index grammar shape, re-dispatched by the FIRST target's category): all targets must be
    /// data-pointers (SR23). GR19's non-integer-amount rule is a VALUE rule, realized EXACTLY at runtime
    /// (<c>CobolPtr.UpByScaled</c> → EC-SIZE-ADDRESS fatal; 2.0 moves by 2). Returns null when the first
    /// target is NOT a pointer — the caller proceeds with the index binding.</summary>
    public BoundStatement? TryBindSetUpDown(Core.SetIndexStatementContext ud)
    {
        var drefs = ud.dataReference();
        if (drefs.Length == 0) return null;
        // Peek the FIRST target's category without consuming diagnostics: an index-name or non-pointer item
        // belongs to the Format-2 index path.
        if (host.Expr.IndexFieldOf(drefs[0]) is not null) return null;
        if (ctx.Refs.Resolve(drefs[0]) is not { } first || first.Item.Pic?.Category is not PicCategory.Pointer)
            return null;

        // SET pointer UP/DOWN BY (§14.9.39 Format 10) is a COBOL-2002 introduction; edition gate moved to
        // VersionConformancePass (Step 14b), firing on the self-identifying BoundSetPointerUpDown node.
        var targets = new List<Place> { first };
        foreach (var dref in drefs.Skip(1))
        {
            if (PtrResolvePointer(dref, "a SET UP/DOWN BY receiver mixed with data-pointers (ISO §14.9.39 SR23)") is not { } p)
                return new BoundNop();
            targets.Add(p);
        }
        var amount = host.Expr.BindExpr(ud.arithmeticExpression());
        return new BoundSetPointerUpDown(targets, amount, ud.DOWN() is not null);
    }

    /// <summary>Resolve a reference that must be a USAGE POINTER item (the 0869 pointer band).</summary>
    private Place? PtrResolvePointer(Core.DataReferenceContext dref, string what)
    {
        if (ctx.Refs.Resolve(dref) is { } p && p.Item.Pic?.Category is PicCategory.Pointer) return p;
        ctx.Edition.Error("COBOLNET0869",
            $"'{dref.GetText()}': {what} shall be a USAGE POINTER data item");
        return null;
    }

    /// <summary>Resolve a reference that must be a BASED 01/77 item (SR18 / §14.9.3 SR1).</summary>
    private DataItem? PtrResolveBased(Core.DataReferenceContext dref)
    {
        DataItem? item = dref.ChildCount == 1 && ctx.Data.ByName.TryGetValue(dref.GetText(), out var list) && list.Count > 0
            ? list[0] : null;
        if (item is { IsBased: true }) return item;
        ctx.Edition.Error("COBOLNET0869",
            $"'{dref.GetText()}': the operand shall be a BASED level-01/77 item (ISO §14.9.39 SR18 / "
            + "§14.9.3 SR1 — rebasing or allocating a non-BASED item is not ISO COBOL)");
        return null;
    }
}
