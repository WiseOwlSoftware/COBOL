// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET verb binder (P7 Step 10m — a real collaborator over <see cref="BinderContext"/>): the
/// 13-format dispatch with the CONTRACT-ORDER semantic re-routes preserved verbatim — the F10 pointer peek
/// FIRST (<c>host.Ptr.TryBindSetUpDown</c>), the F14 CAPACITY-register peek upstream of ResolveReceiving,
/// switches via the .AlterSwitches host edge (10n), objects via the OO host edge (10s).
/// <see cref="SetTargetOf"/> lives HERE (the host keeps a forwarder for ControlFlowBinder until 10t).</summary>
internal sealed class SetBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind a SET statement, dispatching by format (ISO §14.9.39; COBOLNET_DESIGN §12.3). The COBOL-85
    /// surface — Format 1 index/value assignment, Format 2 UP/DOWN BY, Format 4 condition-name TO TRUE — binds here;
    /// the later-edition formats (switches need SPECIAL-NAMES, pointers/objects their 2002 subsystems, TO FALSE the
    /// 2002 FALSE phrase) fail loud by NAME until their subsystem lands.</summary>
    public BoundStatement BindSet(Core.SetStatementContext set)
    {
        if (set.setLastExceptionStatement() is not null) return host.Ec.BindSetLastException();   // F13 (ISO §14.9.39; 2002+)
        if (set.setToValueStatement() is { } tv) return BindSetTo(tv);
        if (set.setIndexStatement() is { } ud) return BindSetUpDown(ud);
        if (set.setBooleanStatement() is { } b) return BindSetCondition(b);
        if (set.setSwitchStatement() is { } sw) return host.Alter.SwitchBindSet(sw);   // Format 3 — external switches (ISO §14.9.39)
        if (set.setAddressStatement() is { } sa)
            return host.Ptr.BindSetAddress(sa);   // F7 both directions + ADDRESS OF senders (Phase-4b inc 2)
        if (set.setObjectReferenceStatement() is { } sor)
        {
            // A POINTER target (§14.9.39 Format 4 — SET pointer TO NULL/pointer) is bound BEFORE the
            // object-reference Format 5: both share the `SET dataRef+ TO objectReference` shape.
            if (sor.dataReference().Length > 0 && ctx.Refs.Resolve(sor.dataReference(0))?.Item.Pic?.Category
                    is PicCategory.Pointer)
                return BindSetPointer(sor.dataReference(),
                    sor.objectReference().dataReference(), sor.objectReference().NULL_() is not null,
                    sor.objectReference().SELF() is not null || sor.objectReference().SUPER() is not null);
            return host.Oo.OoBindSetObjectRef(sor.dataReference(),
                senderRef: sor.objectReference().dataReference(),
                senderNull: sor.objectReference().NULL_() is not null,
                senderSelf: sor.objectReference().SELF() is not null,
                senderSuper: sor.objectReference().SUPER() is not null);
        }
        return new BoundUnsupported($"SET form '{set.GetText()}'");
    }

    /// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1). Receivers may mix index-names and data
    /// items; the sender is any integer-valued operand (an index-name sender reads its occurrence number, §3.5).</summary>
    /// <summary>SET data-pointer assignment (§14.9.39 Format 4; Phase-4b increment 1): every target shall
    /// be USAGE POINTER (COBOLNET0869 otherwise); the sender is the NULL figurative or another data pointer
    /// (SELF/SUPER are object-only — 0869). ADDRESS OF senders/receivers are increment 2 (staged loud).</summary>
    private BoundStatement BindSetPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, Core.DataReferenceContext? senderRef,
        bool toNull, bool senderIsSelfSuper)
    {
        if (senderIsSelfSuper)
        {
            ctx.Edition.Error("COBOLNET0869",
                "SET … TO SELF/SUPER: SELF and SUPER are object references, not data pointers "
                + "(ISO §14.9.39 Format 4/5 — the sender of a pointer SET is NULL or another pointer)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (ctx.Refs.Resolve(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                ctx.Edition.Error("COBOLNET0869",
                    $"SET '{t.GetText()}': the receiving operand of a data-pointer SET shall be USAGE POINTER "
                    + "(ISO §14.9.39 Format 4)");
                return new BoundNop();
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET pointer — sender shape");
            if (ctx.Refs.Resolve(senderRef) is not { } sp || sp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                ctx.Edition.Error("COBOLNET0869",
                    $"SET … TO '{senderRef?.GetText()}': a data-pointer sender shall be NULL or another "
                    + "USAGE POINTER item (ISO §14.9.39 Format 4; ADDRESS OF senders are a later increment)");
                return new BoundNop();
            }
            source = sp;
        }
        return new BoundSetPointer(targets, source, toNull);
    }

    public BoundStatement BindSetTo(Core.SetToValueStatementContext tv)
    {
        // SET Format 14 (ISO §14.9.39; the OCCURS DYNAMIC feature, data-model D9): a CAPACITY-register target
        // reroutes to a capacity change. It runs BEFORE the F4/F5 pointer/object reroutes — a register is numeric,
        // so it would otherwise fall through to the Format-1 store and throw at CapacityRegisterPlace.Write.
        if (DynTryBindSetCapacity(tv.dataReference(), tv.arithmeticExpression(), SetCapacityKind.To) is { } dcap)
            return dcap;
        // The Format-5 SEMANTIC re-route (D-U7): `SET U TO A` parses HERE (alternative order — a
        // dataReference sender is an arithmeticExpression prefix), but an object-reference TARGET selects
        // §14.9.39 Format 5. Detect on the FIRST target; mixed target categories then fail SR8 inside.
        if (tv.dataReference() is { Length: > 0 } tds
            && OoBinder.OoExtractBareReference(tv.arithmeticExpression()) is { } senderDref)
        {
            var t0 = ctx.Refs.Resolve(tds[0])?.Item.Pic?.Category;
            var s0 = ctx.Refs.Resolve(senderDref)?.Item.Pic?.Category;
            // A POINTER on either side selects Format 4 (SET pointer TO pointer) — the Format-1 numeric
            // path cannot carry a ManagedPointer.
            if (t0 is PicCategory.Pointer || s0 is PicCategory.Pointer)
                return BindSetPointer(tds, senderDref, toNull: false, senderIsSelfSuper: false);
            // Either side being an object reference selects Format 5 (§14.9.39 F5; D-U7).
            if (t0 is PicCategory.ObjectReference || s0 is PicCategory.ObjectReference)
                return host.Oo.OoBindSetObjectRef(tds, senderDref, senderNull: false, senderSelf: false, senderSuper: false);
        }
        var targets = new List<BoundSetTarget>();
        foreach (var dref in tv.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetTo(targets, host.Expr.BindExpr(tv.arithmeticExpression()));
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2) — with the Format-10
    /// data-pointer re-route on the FIRST target's category (the D-U7 semantic-re-route pattern; the two
    /// formats share one grammar shape).</summary>
    public BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        if (host.Ptr.TryBindSetUpDown(ud) is { } ptr) return ptr;   // F10 — pointer arithmetic (Phase-4b inc 2)
        if (DynTryBindSetCapacity(ud.dataReference(), ud.arithmeticExpression(),
                ud.DOWN() is not null ? SetCapacityKind.DownBy : SetCapacityKind.UpBy) is { } dcap)
            return dcap;   // F14 — dynamic-capacity change (OCCURS DYNAMIC, D9)
        var targets = new List<BoundSetTarget>();
        foreach (var dref in ud.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetUpDown(targets, host.Expr.BindExpr(ud.arithmeticExpression()), ud.DOWN() is not null);
    }

    /// <summary>SET Format 14 (ISO §14.9.39; OCCURS DYNAMIC, data-model D9): reroute when the FIRST target resolves
    /// to a dynamic-table CAPACITY register — <c>SET reg {TO | UP BY | DOWN BY} n</c> changes the table's current
    /// capacity. A non-register first target returns <see langword="null"/> so the normal Format-1/2 path continues
    /// (the non-consuming peek idiom, mirroring <c>PtrTryBindSetUpDown</c>). The register is the SOLE receiver of a
    /// capacity SET (one capacity per statement); a second/mixed target is COBOLNET1524.</summary>
    private BoundStatement? DynTryBindSetCapacity(
        IReadOnlyList<Core.DataReferenceContext> targets, Core.ArithmeticExpressionContext amount, SetCapacityKind kind)
    {
        // A PURE capacity-register peek (NOT refs.Resolve, which would route an OO `prop OF obj` first target through
        // the property hook and enqueue a spurious pending op — OCCURS DYNAMIC review #7).
        if (targets.Count == 0 || ctx.Refs.CapacityRegisterFor(targets[0]) is not { } cap) return null;
        if (targets.Count > 1)
        {
            ctx.Edition.Error("COBOLNET1524",
                $"SET '{cap.RegisterItem.CobolName}' {SetCapacityKindText(kind)}: a dynamic-table CAPACITY register "
                + "is the sole receiver of a SET Format 14 statement (ISO §14.9.39; §13.18.38 Format 4)");
            return new BoundNop();
        }
        return new BoundSetCapacity(cap.Table, host.Expr.BindExpr(amount), kind);
    }

    private static string SetCapacityKindText(SetCapacityKind kind) =>
        kind switch { SetCapacityKind.To => "TO", SetCapacityKind.UpBy => "UP BY", _ => "DOWN BY" };

    /// <summary>A SET receiving operand: an INDEXED BY index-name (its <c>long</c> field) or a resolvable data item
    /// (an index data item or an integer item — the emitter dispatches on its usage).</summary>
    public BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) =>
        host.Expr.IndexFieldOf(dref) is { } ix ? new SetIndexTarget(ix)
        : host.Expr.ResolveReceiving(dref) is { } p ? new SetPlaceTarget(p)   // a SET receiver IS a receiving operand
        : null;

    /// <summary><c>SET condition-name+ TO TRUE</c> (ISO §14.9.39 Format 4). TO FALSE needs the 2002 <c>WHEN SET TO
    /// FALSE</c> VALUE phrase (SR7) — loud until the 88 model captures it.</summary>
    public BoundStatement BindSetCondition(Core.SetBooleanStatementContext b)
    {
        if (b.TRUE_() is null)
            return new BoundUnsupported("SET condition-name TO FALSE (the VALUE … WHEN SET TO FALSE phrase, COBOL-2002+, ISO §14.9.39 SR7)");
        var sets = new List<(Place, Condition88)>();
        foreach (var dref in b.dataReference())
        {
            if (host.Cond.ConditionOf(dref) is not { } cond) return new BoundUnsupported($"SET '{dref.GetText()}' TO TRUE (not a condition-name)");
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            if (ctx.Refs.ResolveForItem(dref, cond.Parent) is not { } parent)
                return new BoundUnsupported($"SET condition '{cond.Name}' (unresolvable conditional variable)");
            sets.Add((parent, cond));
        }
        return new BoundSetConditions(sets);
    }
}
