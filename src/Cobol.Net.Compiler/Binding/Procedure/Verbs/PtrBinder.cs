// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
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
            if (PtrResolveBased(basedRef) is not { } based) return new BoundNop();   // 0869 reported
            // SR19 — identifier-6 "shall be the predefined address NULL or shall reference a data-pointer": TO NULL
            // disassociates the based item (its implicit pointer becomes NULL — §13.18.5 GR2's initial state; kb/Work
            // PB89 — the form was a parse error).
            if (sa.NULL_() is not null) return new BoundSetAddressOfBased(based, null);
            var senderRef = sa.dataReference(1);
            if (PtrResolvePointer(senderRef, "the sender of SET ADDRESS OF (ISO §14.9.39 SR17)") is not { } src)
                return new BoundNop();
            // §14.9.39.3 SR19, second sentence — the RECEIVER arm: "If data-name-1 is a strongly-typed group item
            // or a restricted pointer, identifier-6 shall reference a data-pointer restricted to the type of
            // data-name-1." Here data-name-1 is the BASED receiver and identifier-6 the pointer sender.
            if ((StrongTypeModel.StrongGroupType(based) ?? StrongTypeModel.PointerRestriction(based)) is { } needed
                && !StrongTypeModel.SameRestriction(needed, StrongTypeModel.PointerRestriction(src.Item)))
            {
                RejectRestriction(senderRef.GetText(),
                    $"the receiver of SET ADDRESS OF is restricted to type '{needed}', so the sender shall "
                    + "reference a data-pointer restricted to the same type (ISO §14.9.39.3 SR19)");
                return new BoundNop();
            }
            return new BoundSetAddressOfBased(based, src);
        }

        // SET pointer TO ADDRESS OF identifier (§8.4.3.11 — the sender form).
        var targetRef = sa.dataReference(0);
        var addrRef = sa.dataReference(1);
        if (PtrResolvePointer(targetRef, "the receiver of SET … TO ADDRESS OF (ISO §14.9.39 Format 7)") is not { } tp)
            return new BoundNop();
        if (PtrBindAddressOf(addrRef) is not { } addr) return new BoundNop();
        // ⛔ THE SECOND ARM, AND IT IS A SEPARATE CODE PATH — the two-arm question asked and answered. §14.9.39.3
        // SR19's first sentence governs the RECEIVING pointer: "If identifier-5 references a restricted
        // data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
        // restricted to the same type." SR20 is its converse: "If identifier-6 references a restricted
        // data-pointer, either identifier-5 shall reference a data-pointer restricted to the same type or
        // data-name-1 shall be a typed item of the type to which identifier-6 is restricted." Here identifier-5 is
        // the pointer receiver and the ADDRESS OF operand supplies identifier-6's restriction (§8.4.3.11.4 GR2).
        string? receiverRestriction = StrongTypeModel.PointerRestriction(tp.Item);
        string? sourceRestriction = StrongTypeModel.AddressOfRestriction(addr.Item);
        if (receiverRestriction is not null && !StrongTypeModel.SameRestriction(receiverRestriction, sourceRestriction))
        {
            RejectRestriction(addrRef.GetText(),
                $"the receiving data-pointer is restricted to type '{receiverRestriction}', so the sender shall "
                + "be NULL or a data-pointer restricted to the same type (ISO §14.9.39.3 SR19)");
            return new BoundNop();
        }
        if (sourceRestriction is not null && receiverRestriction is null)
        {
            RejectRestriction(targetRef.GetText(),
                $"ADDRESS OF '{addrRef.GetText()}' is a RESTRICTED data-pointer of type '{sourceRestriction}' "
                + "(ISO §8.4.3.11.4 GR2 — the operand is a strongly-typed group item or a restricted pointer), so "
                + "the receiver shall be a data-pointer restricted to the same type (ISO §14.9.39.3 SR20)");
            return new BoundNop();
        }
        return new BoundSetPointer([tp], null, ToNull: false, Address: addr);
    }

    /// <summary>Report a restricted-data-pointer type-safety violation. The 0869 pointer band, where PtrBinder
    /// already reports every other §14.9.39 / §14.9.3 operand rule (SR1, SR2, SR3, SR17, SR18, SR23) — these are
    /// operand rules of the same statements, so they belong to the same band rather than to new codes.</summary>
    private void RejectRestriction(string operandText, string what) =>
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape, $"'{operandText}': {what}. Annex D.9.2.2: a restricted data-pointer "
            + "\"shall contain only the predefined address NULL or the address of a data item of the specified "
            + "type\"");

    /// <summary>Resolve an <c>ADDRESS OF identifier</c> operand (ISO §8.4.3.11): a BASED item's value is its
    /// implicit pointer (§8.6.5); an ordinary record must have been storage-forced onto a cell by the data
    /// pass (or be an EXTERNAL record — already cell-backed). A qualified operand resolves through the ONE
    /// §8.4.2.2 qualification machinery; a subscripted operand addresses THE OCCURRENCE (GR1) — the resolver
    /// returns its in-class occurrence displacement (<c>ReferenceResolver.ResolveForAddressOf</c>). Un-forcible
    /// shapes (a class rejected for a national/bit/pointer-class leaf, an OCCURS-resident anchor, a
    /// carrier-resident LINKAGE formal) and reference-modified operands stage LOUD — never a pointer to the
    /// wrong storage. A COMP/float/INDEX leaf is NOT un-forcible any more: every numeric byte form rides the
    /// cell (kb/Work PB164).</summary>
    private BoundAddressOf? PtrBindAddressOf(Core.DataReferenceContext addrRef)
    {
        if (ctx.Refs.ResolveForAddressOf(addrRef) is not { } r)
        {
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                $"ADDRESS OF '{addrRef.GetText()}': the operand is unresolvable, reference-modified, or "
                + "mis-subscripted — ADDRESS OF takes a (possibly qualified/subscripted) data item "
                + "(ISO §8.4.3.11)");
            return null;
        }
        var (item, occursDisp) = r;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        bool cellBacked = root.Class is { Tier: RedefinesTier.StringCanonical } cls
            && (cls.BasedPointerField is not null
                || ctx.Data.PtrAddressableCellOf.ContainsKey(cls)
                || ctx.Data.CallExternalBackings.Any(b => b.BackingCsName == cls.BackingCsName));
        if (!cellBacked)
        {
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                $"ADDRESS OF '{addrRef.GetText()}': the operand's record could not be placed on addressable "
                + "cell storage (a national/bit/pointer-class leaf, an OCCURS-resident anchor, or a "
                + "carrier-resident LINKAGE formal — named increment residue; ISO §8.4.3.11)");
            return null;
        }
        return new BoundAddressOf(item, occursDisp);
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
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    "ALLOCATE … CHARACTERS requires the RETURNING phrase (ISO §14.9.3 SR2 — without a based "
                    + "item there is no other way to address the storage)");
                return new BoundNop();
            }
            // §14.9.3.3 SR4, the CHARACTERS arm: "If data-name-2 references a restricted data-pointer,
            // data-name-1 shall be specified …" — and in Format 1 there IS no data-name-1, so a restricted
            // RETURNING can never be satisfied here. Screening it in this branch too is what keeps the rule from
            // being half-enforced: Form 1 and Form 2 are separate code paths.
            if (StrongTypeModel.PointerRestriction(returning.Item) is { } charsRestriction)
            {
                RejectRestriction(drefs[^1].GetText(),
                    $"the RETURNING data item is a data-pointer restricted to type '{charsRestriction}', which "
                    + "ALLOCATE … CHARACTERS cannot satisfy — it specifies no data-name-1 to supply that type "
                    + "(ISO §14.9.3.3 SR4)");
                return new BoundNop();
            }
            return new BoundAllocate(null, host.Expr.BindExpr(al.arithmeticExpression()), al.INITIALIZED() is not null, returning);
        }

        // Form 2: ALLOCATE based-item [INITIALIZED] [RETURNING pointer].
        var basedRef = drefs[0];
        if (PtrResolveBased(basedRef) is not { } based) return new BoundNop();
        // ⛔ §14.9.3.3 SR4 AND SR5 — the restricted-data-pointer type-safety pair, BOTH directions, and neither
        // existed before kb/Work PB153. Measured on this tree beforehand: `01 T TYPEDEF STRONG. 02 F PIC 9(4).
        // 01 V TYPE T BASED. 01 P USAGE POINTER. ALLOCATE V RETURNING P.` bound clean, silently defeating the
        // Annex D.9.2.2 type-safety guarantee this whole model exists to provide.
        //   SR5: "If both data-name-1 and data-name-2 are specified and data-name-1 references a strongly-typed
        //         group item, the data item referenced by data-name-2 shall be restricted to the type of
        //         data-name-1."
        //   SR4: "If data-name-2 references a restricted data-pointer, data-name-1 shall be specified and shall
        //         reference a typed data item, and the data item referenced by data-name-2 shall be restricted to
        //         the type of data-name-1."  — the converse, which catches a restricted RETURNING over an
        //         untyped or absent based item.
        if (returning is { } ret)
        {
            string? returningRestriction = StrongTypeModel.PointerRestriction(ret.Item);
            // ⛔ SR4 AND SR5 ASK ABOUT DIFFERENT THINGS, and conflating them rejects legal source. SR5's
            // antecedent is "data-name-1 references a STRONGLY-TYPED GROUP ITEM"; SR4's is "data-name-2
            // references a restricted data-pointer", and its requirement on data-name-1 is only that it
            // "reference a TYPED DATA ITEM" — which a WEAK typedef satisfies. So the two tests take different
            // type accessors: StrongGroupType for SR5, the plain TYPE anchor for SR4.
            string? strongType = StrongTypeModel.StrongGroupType(based);
            string? basedType = StrongTypeModel.TypeAnchor(based)?.TypeName;
            if (strongType is not null && !StrongTypeModel.SameRestriction(strongType, returningRestriction))
            {
                RejectRestriction(drefs[^1].GetText(),
                    $"'{basedRef.GetText()}' is a strongly-typed group item of type '{strongType}', so the "
                    + "RETURNING data item shall be a data-pointer restricted to that type (ISO §14.9.3.3 SR5)");
                return new BoundNop();
            }
            if (returningRestriction is not null
                && !StrongTypeModel.SameRestriction(returningRestriction, basedType))
            {
                RejectRestriction(drefs[^1].GetText(),
                    $"the RETURNING data item is a data-pointer restricted to type '{returningRestriction}', so "
                    + $"'{basedRef.GetText()}' shall reference a typed data item of that type — it is "
                    + $"{(basedType is null ? "untyped" : $"of type '{basedType}'")} (ISO §14.9.3.3 SR4)");
                return new BoundNop();
            }
        }
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
        if (ctx.Refs.Probe(drefs[0]) is not { } sniff || sniff.Item.Pic?.Category is not PicCategory.Pointer)
            return null;

        // SET pointer UP/DOWN BY (§14.9.39 Format 10) is a COBOL-2002 introduction; edition gate moved to
        // VersionConformancePass (Step 14b), firing on the self-identifying BoundSetPointerUpDown node.
        // ⛔ RESOLVE to commit — the probe above only DISCRIMINATED the format (kb/Work PB221). Committing the
        // probe's Place made `SET P(XE) Q(XE) UP BY 4` diagnose COBOLNET0844 for Q and not for P: one statement,
        // one rule, two verdicts, decided by which operand the format sniff happened to read.
        if (ctx.Refs.Resolve(drefs[0]) is not { } first) return new BoundNop();
        var targets = new List<Place> { first };
        foreach (var dref in drefs.Skip(1))
        {
            if (PtrResolvePointer(dref, "a SET UP/DOWN BY receiver mixed with data-pointers (ISO §14.9.39 SR23)") is not { } p)
                return new BoundNop();
            targets.Add(p);
        }
        var amount = host.Expr.BindIndexWindowExpr(ud.arithmeticExpression());   // SET (pointer form) is an r7 window (kb/Work R29)
        return new BoundSetPointerUpDown(targets, amount, ud.DOWN() is not null);
    }

    /// <summary>Resolve a reference that must be a USAGE POINTER item (the 0869 pointer band).</summary>
    private Place? PtrResolvePointer(Core.DataReferenceContext dref, string what)
    {
        if (ctx.Refs.Resolve(dref) is { } p && p.Item.Pic?.Category is PicCategory.Pointer) return p;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"'{dref.GetText()}': {what} shall be a USAGE POINTER data item");
        return null;
    }

    /// <summary>Resolve a reference that must be a BASED 01/77 item (SR18 / §14.9.3 SR1).</summary>
    private DataItem? PtrResolveBased(Core.DataReferenceContext dref)
    {
        DataItem? item = dref.ChildCount == 1 && ctx.Data.ByName.TryGetValue(dref.GetText(), out var list) && list.Count > 0
            ? list[0] : null;
        if (item is { IsBased: true }) return item;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"'{dref.GetText()}': the operand shall be a BASED level-01/77 item (ISO §14.9.39 SR18 / "
            + "§14.9.3 SR1 — rebasing or allocating a non-BASED item is not ISO COBOL)");
        return null;
    }
}
