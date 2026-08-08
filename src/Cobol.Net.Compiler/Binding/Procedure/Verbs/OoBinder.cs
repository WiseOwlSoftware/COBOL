// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The OO binder (P7 Step 10s — the OO half converts LAST, behind the OO goldens + method-scope tests):
/// INVOKE §14.9.23 in every form (class/NEW · instance/interface · SELF/SUPER §8.4.3.8 · factory §11.4 ·
/// the D10 UNIVERSAL dynamic path with conformance descriptors) with the D6 USING/RETURNING marshaling
/// (§14.8.2/§14.8.3 conformance), SET Format 5 object-reference assignment (§14.9.39, D-U7), the
/// §8.4.3.9.4 GR1–GR3 object-property desugar (<see cref="OoWrapPropertyOps"/> — invoked between the UDF
/// wrap and the EC wrap at the host BindStatement exit), and the D8 method-context returns
/// (GOBACK/EXIT METHOD → <c>BoundMethodReturn</c>). The ride-along bound records moved to
/// <c>Binding/Bound/BoundOo.cs</c> (records-only, the established rule). The OO HOST state
/// (OoClasses/OoCurrentClass/OoInFactory — set by the emitter's OO bind half) stays on
/// <see cref="StatementBinder"/> (set by <c>Oo/OoDriver</c> since P9 Step 4), read here via host edges that flip
/// at 10t; BindMethodRoster (the class-roster entry-point twin of Bind()) stays on the host with the
/// procedure table until the 10t ProcedureTableBuilder hoist.
/// </summary>
internal sealed class OoBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Drain THIS statement's pending object-property ops (registered by the ReferenceResolver
    /// fallback while the statement bound) into the §8.4.3.9.4 GR1–GR3 desugar: classify each temp's store
    /// polarity over the BOUND statement (BoundStores — the emitter-verified taxonomy), then
    /// GR1 (pure sending) = prepend the get-invoke; GR2 (write-only receiving) = append the set-invoke, get
    /// NOT invoked; GR3 (read-modify-write) = both around ONE temp. SR3/SR4 (:7380/:7382 — the needed
    /// accessor must exist, on the instance or factory roster per the reference form) check HERE, against
    /// the classified need, both COBOLNET0843. An unclassifiable statement (a taxonomy hole) stages LOUD —
    /// never a silent guess about whether a side-effecting accessor runs.</summary>
    public BoundStatement OoWrapPropertyOps(BoundStatement core, int mark)
    {
        var ops = ctx.Data.OoPendingPropertyOps;
        if (ops.Count <= mark) return core;
        var taken = ops.GetRange(mark, ops.Count - mark);
        ops.RemoveRange(mark, ops.Count - mark);

        List<BoundStatement> pre = [], post = [];
        foreach (var op in taken)
        {
            var kind = BoundStores.StoreKindOf(core, op.Temp);
            if (kind is null)
            {
                ctx.Edition.Error("COBOLNET0843",
                    $"the object-property reference '{op.PropName}' OF '{op.ReceiverName}' occurs in a "
                    + $"statement ({core.GetType().Name}) outside the classified store taxonomy — the "
                    + "sending/receiving polarity (ISO §8.4.3.9.4 GR1–GR3) cannot be established; extend "
                    + "BoundStores before accepting this shape");
                continue;
            }
            bool needGet = kind == StoreKind.None || kind == StoreKind.ReadWrite;
            bool needSet = kind == StoreKind.Write || kind == StoreKind.ReadWrite;
            string where = $"'{op.PropName}' OF '{op.ReceiverName}'";
            var form = op.Factory ? InvokeForm.Factory : InvokeForm.Instance;
            var tempPlace = ctx.Refs.ResolveItem(op.Temp)!;

            if (needGet)
            {
                if (op.Get is null)
                    ctx.Edition.Error("COBOLNET0843",
                        $"the object-property reference {where} is a SENDING operand but the class has no "
                        + "GET property method (ISO §8.4.3.9.3 SR3 — WITH NO GET, or no accessor defined)");
                else
                    pre.Add(new BoundInvoke(form, op.ClassCsName, op.Receiver, op.Get.CsName, tempPlace,
                        null, op.Get.Binding!.Returning, op.Get.Owner?.CsName));
            }
            if (needSet)
            {
                if (op.Set is null)
                    ctx.Edition.Error("COBOLNET0843",
                        $"the object-property reference {where} is a RECEIVING operand but the class has no "
                        + "SET property method (ISO §8.4.3.9.3 SR4 — WITH NO SET, or no accessor defined)");
                else
                    post.Add(new BoundInvoke(form, op.ClassCsName, op.Receiver, op.Set.CsName, null,
                        [new BoundInvokeArg(op.Set.Binding!.Formals[0].Item, tempPlace, null, null, WriteBack: false)],
                        null, op.Set.Owner?.CsName));
            }
        }
        return pre.Count + post.Count == 0 ? core : new BoundSequence([.. pre, core, .. post]);
    }


    // ── INVOKE (ISO §14.9.23; deep-dive D5) ─────────────────────────────────────────────────────────────────

    /// <summary>Bind one INVOKE: resolve the receiver (identifier-1 first, class-name-1 second — a data-name
    /// shadows a class-name at reference resolution), the LITERAL method name, and the call form against the
    /// pass-1 symbol table. Part-2 spine scope: <c>Class "NEW" RETURNING obj</c> and the no-arg instance call
    /// are LIVE; SELF/SUPER (slice 3b), factory calls (§11.4 slice), USING/RETURNING marshaling (slice 2),
    /// universal/dynamic dispatch (D10 wave) stage loud.</summary>
    public BoundStatement OoBindInvoke(Core.InvokeStatementContext inv)
    {
        // INVOKE (§14.9.23, OO) is a COBOL-2002 introduction; the edition gate moved to the post-bind
        // VersionConformancePass (Step 14e), firing on the BoundInvoke / BoundInvokeUniversal node this explicit
        // statement produces. (A synthesized property-op BoundInvoke also gates, but property use is itself 2002+,
        // so a below-2002 program using it is already rejected — the over-fire is on an already-errored unit.)
        var target = inv.invokeTarget().objectReference();

        // The method selector: an alphanumeric/national literal binds statically (§14.9.23.3 SR2);
        // identifier-2 (a method name held in a data item) is legal ONLY through a UNIVERSAL receiver
        // (§14.9.23.3 SR7) — the D10 dynamic path, live as of the universal wave.
        if (inv.invokeMethodName().dataReference() is { } mref)
        {
            if (target.dataReference() is not { } uref || ctx.Refs.Resolve(uref) is not { } urecv
                || urecv.Item.Pic is not { Category: PicCategory.ObjectReference, ObjectClassName: null })
            {
                ctx.Edition.Error("COBOLNET0866",
                    "INVOKE: identifier-2 (a method name held in a data item) is permitted only when "
                    + "identifier-1 is a UNIVERSAL object reference (ISO §14.9.23.3 SR7)");
                return new BoundNop();
            }
            if (ctx.Refs.Resolve(mref) is not { } msrc)
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE: the method-name identifier '{mref.GetText()}' is not resolvable to storage");
                return new BoundNop();
            }
            if (msrc.Item.Pic?.Category is not PicCategory.Alphanumeric && !msrc.Item.IsGroup)
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE: identifier-2 ('{mref.GetText()}') shall be of class alphanumeric "
                    + "(ISO §14.9.23.3 SR8; national identifier-2 is a later refinement)");
                return new BoundNop();
            }
            return OoBindUniversalInvoke(inv, urecv, methodLiteral: null, methodSource: msrc);
        }
        // §8.8.3.3 GR3: an alphanumeric/national concatenation expression stands anywhere a literal of that
        // class may — including INVOKE literal-1 (§14.9.23.3 SR2); a boolean-class concat stays null → 0823.
        var mnLit = inv.invokeMethodName().literal();
        string? methodName = mnLit?.nonNumericLiteral()?.concatenationExpression() is { } mce
            ? ConcatFolder.ClassOf(mce) is not PicCategory.Boolean
                ? ConcatFolder.Fold(mce, ctx.Edition, ctx.Data.Collating).Value : null
            : OoDecodeMethodNameLiteral(mnLit);
        if (methodName is null)
        {
            ctx.Edition.Error("COBOLNET0823",
                "INVOKE: literal-1 (the method name) shall be of class alphanumeric or national "
                + "(ISO §14.9.23.3 SR2)");
            return new BoundNop();
        }
        if (methodName.Length == 0)
        {
            ctx.Edition.Error("COBOLNET0823",
                "INVOKE: literal-1 shall not be a zero-length literal (ISO §14.9.23.3 SR2)");
            return new BoundNop();
        }

        if (target.SELF() is not null || target.SUPER() is not null)
        {
            // Slice 3b — §8.4.3.8: SELF/SUPER are the predefined object references of the CURRENT method's
            // object; legal only within a method body.
            bool isSuper = target.SUPER() is not null;
            if (!host.InMethod || host.OoCurrentClass is not { } cur)
            {
                ctx.Edition.Error("COBOLNET0827",
                    $"INVOKE {(isSuper ? "SUPER" : "SELF")} may be specified only within a method definition "
                    + "(ISO §8.4.3.8 — the predefined object references of the current object)");
                return new BoundNop();
            }
            // In a FACTORY method, SELF|SUPER "NEW" is the ACTIVE-CLASS creation (§16.2.1 GR1 — the
            // BaseFactoryInterface's New): bind InvokeForm.NewSelf → `this.__New()` (covariant per class;
            // SUPER restricts the METHOD SEARCH, GR3, but the found method IS the predefined New whose
            // behavior is active-class creation on the SAME runtime factory — the equivalence is deliberate).
            if (host.OoInFactory && string.Equals(methodName, "NEW", StringComparison.OrdinalIgnoreCase))
            {
                if (inv.invokeUsing() is not null)
                {
                    ctx.Edition.Error("COBOLNET0826",
                        "INVOKE SELF/SUPER \"NEW\": the predefined NEW method takes no USING arguments "
                        + "(ISO §16.2.1)");
                    return new BoundNop();
                }
                if (inv.invokeReturning()?.dataReference() is not { } nrRef)
                {
                    ctx.Edition.Error("COBOLNET0826",
                        "INVOKE SELF/SUPER \"NEW\" without RETURNING — the created object would be lost "
                        + "(ISO §16.2.1/§14.9.23.4 GR8)");
                    return new BoundNop();
                }
                if (ctx.Refs.Resolve(nrRef) is not { } nret)
                    return new BoundUnsupported($"INVOKE … RETURNING '{nrRef.GetText()}' (unresolvable receiver)");
                if (nret.Item.Pic is not { Category: PicCategory.ObjectReference } nrp)
                {
                    ctx.Edition.Error("COBOLNET0826",
                        $"INVOKE SELF/SUPER \"NEW\" RETURNING '{nrRef.GetText()}': the receiving item shall "
                        + "be a USAGE OBJECT REFERENCE data item (ISO §14.9.23.4 GR8)");
                    return new BoundNop();
                }
                // The runtime class is the CONTAINING class or a subclass — the containing class's
                // conformance is the strongest compile-time guarantee (§14.8 — a subclass instance still
                // conforms downstream of anything the containing class conforms to).
                if (OoConformance.ObjectRefWideningMismatch(host.OoClasses, PicInfo.ObjectReferenceItem(cur.Name), nrp) is { } nwerr)
                {
                    ctx.Edition.Error("COBOLNET0826",
                        $"INVOKE SELF/SUPER \"NEW\" RETURNING '{nrRef.GetText()}': {nwerr} (ISO §14.8)");
                    return new BoundNop();
                }
                return new BoundInvoke(InvokeForm.NewSelf, cur.CsName, null, null, nret);
            }
            OoClassSymbol searchRoot;
            if (!isSuper)
                searchRoot = cur;   // GR2 — resolve on the current class's chain; dispatch on the RUNTIME class
            else if (cur.Base is { } b)
                searchRoot = b;     // GR3 — the restricted search STARTS at the base class
            else
            {
                // Trap #7 — SUPER in a root class is a clean compile diagnostic, never an internal error
                // (applies identically to the FACTORY flavor).
                ctx.Edition.Error("COBOLNET0827",
                    $"INVOKE SUPER in class '{cur.Name}', which INHERITS from no class (ISO §8.4.3.8 — SUPER "
                    + "references the inherited class's methods)");
                return new BoundNop();
            }
            // Roster selection by CONTEXT (§14.9.23.3 SR4f/g/h/i): a factory method's SELF/SUPER resolve
            // over the FACTORY interface; an instance method's over the instance interface.
            var sm = host.OoInFactory ? searchRoot.FindFactoryMethod(methodName) : searchRoot.FindMethod(methodName);
            if (sm is null)
            {
                ctx.Edition.Error("COBOLNET0825",
                    $"INVOKE {(isSuper ? "SUPER" : "SELF")} \"{methodName}\": class '{searchRoot.Name}' (and "
                    + $"its inheritance chain) does not define a{(host.OoInFactory ? " factory" : "n instance")} "
                    + "method named '" + methodName + "' "
                    + "(ISO §14.9.23.3 SR4f–SR4i — the SELF/SUPER method-name placement rules)");
                return new BoundNop();
            }
            return OoBindResolvedInvoke(inv, sm, isSuper ? InvokeForm.Super : InvokeForm.Self, null);
        }
        if (target.dataReference() is not { } dref)
        {
            ctx.Edition.Error("COBOLNET0823",
                "INVOKE NULL: the receiver shall be an object-reference identifier or a class-name "
                + "(ISO §14.9.23.3 — the predefined NULL object reference cannot be a receiver)");
            return new BoundNop();
        }

        // identifier-1 vs class-name-1 (§14.9.23.2): resolve as a data item first (a data-name shadows);
        // an unresolved SIMPLE name is then a class-name candidate in the pass-1 table — a LEGAL alternative,
        // so this is a Probe; the else-tail below reports when NEITHER reading holds (R30).
        if (ctx.Refs.Probe(dref) is { } receiver)
            return OoBindInstanceInvoke(inv, receiver, methodName);
        if (host.OoClasses?.Find(dref.GetText()) is { } cls)
            return OoBindClassInvoke(inv, cls, methodName);
        ctx.Edition.Error("COBOLNET0823",
            $"INVOKE: '{dref.GetText()}' is neither a resolvable data item nor a class of the compilation "
            + "group (ISO §14.9.23.2 — identifier-1 or class-name-1)");
        return new BoundNop();
    }

    /// <summary><c>INVOKE class-name-1 …</c>: the predefined NEW (§16.2.1) → the generated ctor; any other
    /// method through a class-name is a FACTORY invocation (§11.4) — a later slice.</summary>
    private BoundStatement OoBindClassInvoke(Core.InvokeStatementContext inv, OoClassSymbol cls, string method)
    {
        if (!string.Equals(method, "NEW", StringComparison.OrdinalIgnoreCase))
        {
            // §14.9.23.3 SR3: literal-1 names a method of the FACTORY interface of class-name-1 — resolution
            // walks the INHERITS chain over the factory rosters (§9.3.6); the lookup failure is the
            // compile-time analog of EC-OO-METHOD (GR7b).
            if (cls.FindFactoryMethod(method) is { } fm)
            {
                var bound = OoBindResolvedInvoke(inv, fm, InvokeForm.Factory, null);
                return bound is BoundInvoke bi ? bi with { ClassCsName = cls.CsName } : bound;
            }
            ctx.Edition.Error("COBOLNET0825",
                $"INVOKE {cls.Name} \"{method}\": class '{cls.Name}' (and its inheritance chain) does not "
                + "define a FACTORY method named '" + method + "' (ISO §14.9.23.3 SR3 — literal-1 shall name "
                + "a method of the factory interface; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b)");
            return new BoundNop();
        }
        if (inv.invokeUsing() is not null)
        {
            ctx.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\": the predefined NEW method takes no USING arguments "
                + "(ISO §16.2.1 — its only result is the new object reference)");
            return new BoundNop();
        }
        if (inv.invokeReturning()?.dataReference() is not { } retRef)
        {
            ctx.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" without RETURNING — the created object would be lost; NEW's "
                + "result is delivered only through the RETURNING identifier (ISO §16.2.1/§14.9.23.4 GR8)");
            return new BoundNop();
        }
        if (ctx.Refs.Resolve(retRef) is not { } ret)
            return new BoundUnsupported($"INVOKE … RETURNING '{retRef.GetText()}' (unresolvable receiver)");
        if (ret.Item.Pic is not { Category: PicCategory.ObjectReference } retPic)
        {
            ctx.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" RETURNING '{retRef.GetText()}': the receiving item shall be a "
                + "USAGE OBJECT REFERENCE data item (ISO §14.9.23.4 GR8 / §14.8 conformance)");
            return new BoundNop();
        }
        // Receiver conformance (§14.8 via the SET/widening direction): universal accepts anything; a typed
        // receiver accepts the class, a subclass, or — for an INTERFACE-typed receiver — any class whose
        // §11.8.4 closure implements it (the ONE OoConformance.ObjectRefWideningMismatch rule).
        if (OoConformance.ObjectRefWideningMismatch(host.OoClasses, PicInfo.ObjectReferenceItem(cls.Name), retPic) is { } werr)
        {
            ctx.Edition.Error("COBOLNET0826",
                $"INVOKE {cls.Name} \"NEW\" RETURNING '{retRef.GetText()}': {werr} (ISO §14.8)");
            return new BoundNop();
        }
        return new BoundInvoke(InvokeForm.New, cls.CsName, null, null, ret);
    }

    /// <summary><c>INVOKE identifier-1 "method" …</c>: virtual dispatch through a TYPED object reference; the
    /// method resolves over the declared class's hierarchy at COMPILE time (§14.9.23.3 SR4d — for the typed
    /// path a lookup failure is a compile-time diagnostic, the static analog of EC-OO-METHOD, GR7b).</summary>
    private BoundStatement OoBindInstanceInvoke(Core.InvokeStatementContext inv, Place receiver, string method)
    {
        if (receiver.Item.Pic is not { Category: PicCategory.ObjectReference } pic)
        {
            ctx.Edition.Error("COBOLNET0824",
                $"INVOKE '{receiver.Item.CobolName}': identifier-1 shall be a USAGE OBJECT REFERENCE data "
                + "item (ISO §14.9.23.3 SR3)");
            return new BoundNop();
        }
        if (pic.ObjectClassName is not { } className)
            // A UNIVERSAL receiver with a literal selector (SR4 permits literal-1; it still cannot bind
            // statically — no roster exists at compile time): the D10 dynamic path.
            return OoBindUniversalInvoke(inv, receiver, methodLiteral: method, methodSource: null);
        // An INTERFACE-typed receiver: resolution over the interface's prototype closure (§14.9.23.3 SR4e);
        // the emitted call is static C# interface dispatch behind the same GR5 null guard.
        if (host.OoClasses?.FindInterface(className) is { } recvIface)
        {
            var proto = recvIface.AllPrototypes()
                .FirstOrDefault(pm => string.Equals(pm.Name, method, StringComparison.OrdinalIgnoreCase));
            if (proto is null)
            {
                ctx.Edition.Error("COBOLNET0825",
                    $"INVOKE '{receiver.Item.CobolName}' \"{method}\": interface '{recvIface.Name}' (and "
                    + "its INHERITS closure) does not declare a method named '" + method + "' "
                    + "(ISO §14.9.23.3 SR4e)");
                return new BoundNop();
            }
            var ibound = OoBindResolvedInvoke(inv, proto, InvokeForm.Instance, receiver);
            return ibound is BoundInvoke ibi ? ibi with { OwnerCsName = recvIface.CsName } : ibound;
        }
        if (host.OoClasses?.Find(className) is not { } cls)
        {
            // Unreachable when DataBinder validated the declared class (COBOLNET0813) — defensive, loud.
            ctx.Edition.Error("COBOLNET0813",
                $"INVOKE '{receiver.Item.CobolName}': its declared class '{className}' is not a class of the "
                + "compilation group (ISO §13.18.60.4)");
            return new BoundNop();
        }
        if (cls.FindMethod(method) is not { } m)
        {
            string hint = cls.FindFactoryMethod(method) is not null
                ? $" ('{method}' IS a FACTORY method of class '{cls.Name}' — invoke it through the "
                  + "class-name: an instance receiver resolves the INSTANCE interface, §14.9.23.3 SR4b)"
                : "";
            ctx.Edition.Error("COBOLNET0825",
                $"INVOKE '{receiver.Item.CobolName}' \"{method}\": class '{cls.Name}' (and its inheritance "
                + "chain) does not define a method named '" + method + "' (ISO §14.9.23.3 SR4d — compile-time "
                + $"for a typed receiver; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b){hint}");
            return new BoundNop();
        }
        return OoBindResolvedInvoke(inv, m, InvokeForm.Instance, receiver);
    }

    /// <summary>The shared USING + RETURNING binding tail for a RESOLVED method — the Instance / SELF / SUPER
    /// forms differ only in receiver resolution and dispatch rendering (§8.4.3.8), never in marshaling.</summary>
    private BoundStatement OoBindResolvedInvoke(
        Core.InvokeStatementContext inv, OoMethodSymbol m, InvokeForm form, Place? receiver)
    {
        // ── USING marshaling (slice 2 — D6; §14.9.23.4 GR3: positional correspondence) ──
        var argCtxs = inv.invokeUsing()?.invokeArgument() ?? [];
        if (argCtxs.Length != m.Binding!.Formals.Count)
        {
            // The trap-#3 rule: an arity mismatch is LOUD — a silently dropped/extra argument would shift
            // every following slot (the legacy DEVLOG-449 blocker: the first USING bound to the RETURNING).
            ctx.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\": {argCtxs.Length} USING argument(s) for {m.Binding!.Formals.Count} formal "
                + $"parameter(s) of the method (ISO §14.9.23.4 GR3 — correspondence is positional; "
                + "trailing-OMITTED support is a later slice)");
            return new BoundNop();
        }
        var args = new List<BoundInvokeArg>(argCtxs.Length);
        for (int i = 0; i < argCtxs.Length; i++)
        {
            if (OoBindInvokeArg(argCtxs[i], m.Binding!.Formals[i].Item, m.Name) is not { } a) return new BoundNop();
            args.Add(a);
        }

        // ── RETURNING pairing + conformance (GR8; §14.8.3; the deep-dive signature-check edge case:
        // BOTH mismatch directions are compile-time diagnostics) ──
        var retRef = inv.invokeReturning()?.dataReference();
        Place? retPlace = null;
        if (retRef is not null && m.Binding!.Returning is null)
        {
            ctx.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\" RETURNING: the method declares no RETURNING item (ISO §14.9.23.4 GR8 / "
                + "§14.8.3 — nothing to deliver)");
            return new BoundNop();
        }
        if (retRef is null && m.Binding!.Returning is not null)
        {
            ctx.Edition.Error("COBOLNET0828",
                $"INVOKE \"{m.Name}\": the method declares a RETURNING item ('{m.Binding!.Returning.CobolName}') — "
                + "the INVOKE must specify RETURNING to receive it (the binder's signature check, deep-dive "
                + "D1; ISO §14.9.23.4 GR8)");
            return new BoundNop();
        }
        if (retRef is not null)
        {
            if (ctx.Refs.Resolve(retRef) is not { } rp)
            {
                ctx.Edition.Error("COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': the receiving identifier is not "
                    + "resolvable to storage");
                return new BoundNop();
            }
            // §14.8.3.3 rule 1: the RETURNING delivery conforms "as if a SET statement were performed" —
            // for object references that is the WIDENING direction (universal receiver accepts anything; a
            // typed receiver accepts the same class or a subclass — SET SR12a2), NOT the §14.8.2.3.2
            // identity rule. Everything else keeps the strict description check.
            string? rerr = m.Binding!.Returning!.Pic is { Category: PicCategory.ObjectReference } sendPic
                    && rp.Item.Pic is { Category: PicCategory.ObjectReference } recvPic
                ? OoConformance.ObjectRefWideningMismatch(host.OoClasses, sendPic, recvPic)
                : OoConformanceError(m.Binding!.Returning!, rp.Item);
            if (rerr is not null)
            {
                ctx.Edition.Error("COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': {rerr} (ISO §14.8.3.3 "
                    + "returning-item conformance)");
                return new BoundNop();
            }
            retPlace = rp;
        }
        return new BoundInvoke(form, null, receiver, m.CsName, retPlace, args, m.Binding!.Returning, m.Owner?.CsName);
    }

    /// <summary>Bind ONE INVOKE argument against its positional formal — the conformance RULE is selected
    /// by the EFFECTIVE passing mode (§14.9.23.4 GR6): BY REFERENCE takes §14.8.2.3.2 strict identity (with
    /// the §14.8.2.2 rule-1 group-prefix allowance); BY CONTENT — explicit, the §14.9.23.3 SR 10 object-data
    /// auto-CONTENT, and every literal — takes §14.8.2.3.3: COMPUTE rules for a numeric formal (any numeric
    /// argument), SET rules for an object-reference formal (widening), MOVE rules otherwise. A
    /// reference-modified argument conforms by its EFFECTIVE description (a unique elementary alphanumeric
    /// item of the window length, §8.4.3.3.4 GR6). Null on a diagnostic.</summary>
    private BoundInvokeArg? OoBindInvokeArg(Core.InvokeArgumentContext arg, DataItem formal, string methodName)
    {
        void Err(string msg) => ctx.Edition.Error("COBOLNET0828", $"INVOKE \"{methodName}\": {msg}");

        if (arg.VALUE() is not null)
        {
            // SR5b: a BY VALUE argument requires a BY VALUE formal; every formal is BY REFERENCE today (the
            // procedure-division-header BY phrases are an unparsed grammar extension — added with them).
            Err($"BY VALUE argument for formal '{formal.CobolName}': the corresponding formal parameter is "
                + "BY REFERENCE (ISO §14.9.23.3 SR5b; header BY VALUE formals are a later slice)");
            return null;
        }

        bool explicitReference = arg.REFERENCE() is not null;
        bool explicitContent = arg.CONTENT() is not null;

        // ── ONE OPERAND, FOUR CHANNELS — resolved ONCE, here (ISO §14.9.23.2 BY CONTENT: `arithmetic-
        // expression-1 | boolean-expression-1 | identifier-5 | literal-2`) ──────────────────────────────────
        // ⛔ THE PARSE NODE AN OPERAND LANDS IN IS NOT ITS MEANING, and both of PB46's halves learned that the
        // hard way. `arithmeticExpression` SUBSUMES `dataReference` and every numeric literal, and the
        // `{boolExprAhead()}?`-gated `booleanExpression` alternative subsumes BOTH of those in turn — its leaf
        // is `valueOperand`, and the predicate's scan runs to the statement's period, so in
        // `USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2` the FIRST argument reaches the boolean node on the
        // strength of the SECOND argument's B-AND. Normalizing here is what makes that harmless: a boolean node
        // carrying NO boolean operator reduces to its bare `valueOperand` (ConditionBinder.UnwrapBareBool — the
        // same reduction BindPrimaryBoolean uses) and rides exactly the arm it would have without the predicate.
        var boolCtx = arg.booleanExpression();
        var arithCtx = arg.arithmeticExpression();
        var nonNumCtx = arg.literal()?.nonNumericLiteral();
        string? numLitRaw = arg.literal()?.numericLiteral()?.GetText();
        if (boolCtx is not null && ConditionBinder.UnwrapBareBool(boolCtx) is { } bare)
        {
            boolCtx = null;
            arithCtx = bare.arithmeticExpression();
            nonNumCtx = bare.nonNumericLiteral();
        }
        // A SOLE numeric literal is a literal wherever it parsed. The grammar's own `literal` alternative wins
        // it when the boolean/arithmetic arms are not taken, and the two paths must agree: the literal arm
        // admits an unsigned integer into an ALPHANUMERIC formal by the MOVE rules, which the expression arm
        // (§14.8.2.3.3 rule 2a, category-numeric formals only) correctly does not.
        if (numLitRaw is null && arithCtx is not null && ConditionBinder.SoleNumLiteral(arithCtx) is { } soleNum)
        {
            numLitRaw = soleNum;
            arithCtx = null;
        }

        // ⛔ THE IDENTIFIER CASE IS RECOVERED HERE, NOT IN THE GRAMMAR (fix-queue PB46). BY CONTENT's operand
        // list admits an arithmetic expression, and `arithmeticExpression` SUBSUMES `dataReference` — so a bare
        // `BY CONTENT A` now arrives as an expression, and routing it to the expression arm would silently drop
        // the §14.9.23.3 SR9/SR10 object-data rules, the §14.8.2.3.2 conformance check and the ref-mod handling
        // that only the identifier arm performs. The grammar cannot express "a reference, unless it is part of
        // an expression"; the binder can, through the SAME sole-reference reduction ConditionBinder and
        // IntrinsicBinder already use (feedback_one_rule_one_place — that helper is now shared, not re-copied).
        var dref = arg.dataReference() ?? ConditionBinder.SoleDataReference(arithCtx);
        if (dref is not null)
        {
            if (ctx.Refs.Resolve(dref) is not { } place)
            {
                Err($"USING argument '{dref.GetText()}' is not resolvable to storage (or uses a reference "
                    + "form not yet carried across INVOKE)");
                return null;
            }
            // §14.9.23.3 SR 10: object data (factory/instance WS) cannot cross BY REFERENCE — explicit
            // BY REFERENCE violates the rule; a BARE object-data identifier is assumed BY CONTENT (GR6a2).
            bool objectData = ctx.Data.OoIsObjectData(place.Item);
            if (explicitReference && objectData)
            {
                Err($"BY REFERENCE argument '{dref.GetText()}' references OBJECT data — factory/instance "
                    + "working-storage may not cross an INVOKE by reference (ISO §14.9.23.3 SR 10); pass it "
                    + "BY CONTENT");
                return null;
            }
            bool byReference = !explicitContent && !objectData;   // GR6a — REFERENCE assumed when SR9/10 hold

            // A reference-modified operand is a unique ELEMENTARY ALPHANUMERIC item of the window length
            // (§8.4.3.3.4 GR6): conformance goes against that effective description, never the whole inner item.
            if (place is RefModPlace rmp)
            {
                if (formal.IsGroup || formal.Pic?.Category is not PicCategory.Alphanumeric)
                {
                    Err($"reference-modified argument '{dref.GetText()}': the operand is elementary "
                        + $"alphanumeric (§8.4.3.3.4 GR6) and does not conform to formal '{formal.CobolName}'");
                    return null;
                }
                if (byReference)
                {
                    // Strict identity needs a PROVABLE window length equal to the formal's.
                    if (!int.TryParse(rmp.Start, out _) || rmp.Length is null
                        || !int.TryParse(rmp.Length, out int rlen))
                    {
                        Err($"BY REFERENCE reference-modified argument '{dref.GetText()}' needs a "
                            + "compile-time (start:length) to prove §14.8.2.3.2 conformance — pass it "
                            + "BY CONTENT or use literal subscripts");
                        return null;
                    }
                    if (!formal.IsAnyLength && rlen != formal.Pic.Length)   // ANY LENGTH: any window length matches (§14.8.2.3.2 rule d)
                    {
                        Err($"reference-modified argument window ({rlen}) does not match formal "
                            + $"'{formal.CobolName}' X({formal.Pic.Length}) (ISO §14.8.2.3.2)");
                        return null;
                    }
                }
                return new BoundInvokeArg(formal, place, null, null,
                    WriteBack: byReference, ByContent: !byReference);
            }

            if (byReference)
            {
                if (OoConformance.DescriptionMismatch(formal, place.Item, byRefGroupPrefix: true,
                        anyLengthActivationRelax: true) is { } err1)   // §14.8.2.3.2 rules d/e (ANY LENGTH)
                {
                    Err($"USING argument '{dref.GetText()}' does not conform to formal parameter "
                        + $"'{formal.CobolName}': {err1} (ISO §14.8.2.3.2 — BY REFERENCE requires the "
                        + "identical description)");
                    return null;
                }
                return new BoundInvokeArg(formal, place, null, null, WriteBack: true);
            }

            // Effective BY CONTENT (§14.8.2.3.3): rule-per-formal-category.
            if (OoContentMismatch(formal, place.Item) is { } cerr)
            {
                Err($"BY CONTENT argument '{dref.GetText()}' does not conform to formal "
                    + $"'{formal.CobolName}': {cerr} (ISO §14.8.2.3.3)");
                return null;
            }
            return new BoundInvokeArg(formal, place, null, null, WriteBack: false, ByContent: true);
        }

        // ── BY CONTENT arithmetic-expression-1 (ISO §14.9.23.2; fix-queue PB46) ─────────────────────────────
        // The general format's BY CONTENT branch admits an arithmetic expression, and this arm is what makes
        // that true end to end. It is BY CONTENT by construction: §14.9.23.3 SR9 confines BY REFERENCE to an
        // identifier, and an expression has no storage to write back to.
        // §14.8.2.3.3 rule 2a governs the crossing — "the value is transferred according to the rules of the
        // COMPUTE statement" — which is exactly a numeric formal. A NON-numeric formal is not a gap here but a
        // CONFORMANCE failure the standard requires be reported: §14.9.25.3 Table 16 admits a numeric sender to
        // an alphanumeric receiver only for an INTEGER sender, and an arithmetic expression carries no
        // compile-time guarantee of that, so the honest verdict is a cited diagnostic rather than silent
        // truncation.
        // ── BY CONTENT boolean-expression-1 (ISO §14.9.23.2; fix-queue PB46) ────────────────────────────────
        // The third operand shape the BY CONTENT branch admits, and the ONE the BY VALUE branch does not — the
        // two phrases genuinely differ in the printed general format. It is its own VALUE channel (D-B1: a
        // '0'/'1' bit string, §8.8.2), never the numeric one, which is why it needs a slot of its own rather
        // than a second spelling of ContentExpr.
        // §14.8.2.3.3 rule 2d governs the crossing: the formal is not numeric, not an index item and not
        // ANY LENGTH, so "the conformance rules are the same as for a MOVE statement with the argument as the
        // sending operand" — §14.9.25.3 Table 16's BOOLEAN row, which admits alphanumeric and boolean
        // receivers and refuses alphabetic, numeric and numeric-edited ones.
        // ⚠ TABLE 16 ALSO ADMITS A NATIONAL RECEIVER, AND THIS ARM REFUSES IT ON PURPOSE — the IDENTIFIER
        // CONTENT arm above refuses the same pairing through OoContentMismatch's conservative strict gate, and
        // two arms of one rule disagreeing is worse than one named residue. Both are recorded together.
        if (boolCtx is { } bx && explicitContent)
        {
            if (formal.IsGroup || formal.Pic is not { Category: PicCategory.Alphanumeric or PicCategory.Boolean }
                || formal.Pic is { Category: PicCategory.Alphanumeric, IsAlphabetic: true })
            {
                Err($"BY CONTENT boolean-expression argument '{bx.GetText()}' for formal "
                    + $"'{formal.CobolName}': §14.8.2.3.3 rule 2d transfers it by the MOVE rules, and "
                    + "§14.9.25.3 Table 16 admits a boolean sending operand only to a boolean or alphanumeric "
                    + "receiver");
                return null;
            }
            var bound = host.Cond.BindBoolExpr(bx);
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                { ContentBool = bound, ContentBoolWidth = ConditionBinder.Gr3Width(bound) };
        }

        if (arithCtx is { } ax && explicitContent)   // a SOLE reference / numeric literal was taken above
        {
            if (formal.IsGroup || formal.Pic is not { Category: PicCategory.Numeric })
            {
                Err($"BY CONTENT arithmetic-expression argument '{ax.GetText()}' for formal "
                    + $"'{formal.CobolName}': §14.8.2.3.3 rule 2a transfers an expression by the COMPUTE rules, "
                    + "which requires a category-numeric formal parameter");
                return null;
            }
            if (formal.Pic is { IsFloat: true })
            {
                Err($"BY CONTENT arithmetic-expression argument '{ax.GetText()}' for the floating-point formal "
                    + $"'{formal.CobolName}': the fixed-point→float CONTENT conversion is the same documented "
                    + "refinement the identifier arm defers (ISO §14.8.2.3.3)");
                return null;
            }
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                { ContentExpr = host.Expr.BindExpr(ax) };
        }

        // A literal argument — BY CONTENT (GR6a2; a literal never meets SR9). Per §9.3.6 resolution rule 5
        // a literal that would TRUNCATE still conforms (the SET/MOVE no-truncation requirements are ignored
        // for literal arguments), so length/digit overflow converts per MOVE rules rather than erroring.
        // §8.8.3.3 GR3: an alphanumeric concatenation expression is the equivalent alphanumeric literal —
        // fold it and ride the STRINGLIT leg's conformance shape (a non-alphanumeric concat falls through
        // to the trailing unsupported-argument diagnostic like any other non-alphanumeric literal).
        string? alnumTxt =
            nonNumCtx?.STRINGLIT() is { } sl ? CobolLiteral.Decode(sl.GetText())
            : nonNumCtx?.concatenationExpression() is { } ice
              && ConcatFolder.ClassOf(ice) is PicCategory.Alphanumeric
                ? ConcatFolder.Fold(ice, ctx.Edition, ctx.Data.Collating).Value
            : null;
        if (alnumTxt is not null)
        {
            if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
                return new BoundInvokeArg(formal, null, null, alnumTxt, WriteBack: false, ByContent: true);
            Err($"nonnumeric literal argument {nonNumCtx!.GetText()} for the non-alphanumeric formal "
                + $"'{formal.CobolName}' (ISO §14.8.2.3.3 MOVE-rule conformance)");
            return null;
        }
        // A BOOLEAN literal (or a boolean concatenation expression, §8.8.3.3 GR3) is literal-2 of the same
        // BY CONTENT branch, and it is a boolean VALUE with no storage — so it rides the boolean channel this
        // fix built rather than a fourth one. Without it, `INVOKE O "M" USING BY CONTENT B"1010"` fell all the
        // way to the trailing "argument form … not yet carried" diagnostic: legal source (§14.9.23.3 SR17 bars
        // only a ZERO-LENGTH literal-2), refused.
        string? boolTxt =
            nonNumCtx?.BOOLLIT() is { } bl ? CobolLiteral.Decode(bl.GetText())
            : nonNumCtx?.concatenationExpression() is { } bce && ConcatFolder.ClassOf(bce) is PicCategory.Boolean
                ? ConcatFolder.Fold(bce, ctx.Edition, ctx.Data.Collating).Value
            : null;
        if (boolTxt is not null)
        {
            // Table 16's BOOLEAN row again (§14.8.2.3.3 rule 2d) — the same receivers the expression arm takes.
            if (formal.IsGroup || formal.Pic is not { Category: PicCategory.Alphanumeric or PicCategory.Boolean }
                || formal.Pic is { Category: PicCategory.Alphanumeric, IsAlphabetic: true })
            {
                Err($"boolean literal argument {nonNumCtx!.GetText()} for formal '{formal.CobolName}': "
                    + "§14.9.25.3 Table 16 admits a boolean sending operand only to a boolean or alphanumeric "
                    + "receiver (ISO §14.8.2.3.3 rule 2d MOVE-rule conformance)");
                return null;
            }
            // A LITERAL contributes no item width to §8.8.2 rule 10, so the value crosses at the formal's
            // width — width 0, exactly as ConditionBinder.Gr3Width scores a literal-only expression.
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                { ContentBool = new BoundBoolLiteral(boolTxt), ContentBoolWidth = 0 };
        }
        if (numLitRaw is { } raw)
        {
            if (formal.Pic is { Category: PicCategory.Numeric, IsFloat: false })
                return new BoundInvokeArg(formal, null, raw, null, WriteBack: false, ByContent: true);
            if (!formal.IsGroup && formal.Pic?.Category is PicCategory.Alphanumeric
                && !raw.Contains('.') && !raw.StartsWith('-') && !raw.StartsWith('+'))
                // MOVE rules: an unsigned integer numeric literal moves to an alphanumeric receiver as its
                // digit characters (§14.9.25).
                return new BoundInvokeArg(formal, null, null, raw, WriteBack: false, ByContent: true);
            Err($"numeric literal argument {raw} for formal '{formal.CobolName}' "
                + "(ISO §14.8.2.3.3 — no conforming COMPUTE/MOVE rule applies)");
            return null;
        }
        Err($"USING argument form for formal '{formal.CobolName}' is not yet carried across INVOKE");
        return null;
    }

    /// <summary>§14.8.2.3.3 — the BY CONTENT conformance rules per formal category: COMPUTE for numeric
    /// (any fixed-point numeric argument; float formals require the identical float usage — the cross-float
    /// CONTENT conversion is a documented later refinement), SET for object references (widening — the
    /// argument's class shall be the receiver's class or a subclass), MOVE otherwise (alphanumeric/group
    /// formals take any alphanumeric/group/integer-display argument, pad/truncate per MOVE).</summary>
    private string? OoContentMismatch(DataItem formal, DataItem arg)
    {
        if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
        {
            if (arg.IsGroup)
                return arg.IsImageCapable ? null : "the argument group has no character image (Tier-C)";
            return arg.Pic?.Category switch
            {
                PicCategory.Alphanumeric or PicCategory.NumericEdited => null,
                // Table 16: boolean→alphanumeric is a conforming MOVE; national→alphanumeric is NOT
                // (§14.9.25.3 — DISPLAY-OF is the sanctioned narrowing), so National keeps the mismatch arm.
                PicCategory.Boolean => null,
                PicCategory.Numeric when arg.Pic is { IsFloat: false, Scale: 0 } => null,   // MOVE integer→alnum
                _ => "no conforming MOVE rule applies (ISO §14.8.2.2 rule 2 / §14.9.25)",
            };
        }
        var f = formal.Pic!;
        return f.Category switch
        {
            PicCategory.Numeric when f.IsFloat =>
                arg.Pic is { IsFloat: true } a2 && a2.Usage == f.Usage
                    ? null
                    : "a float formal takes the identical float usage BY CONTENT (cross-float COMPUTE "
                      + "conversion is a later refinement)",
            PicCategory.Numeric =>
                arg.IsGroup ? "a group argument does not conform to a numeric formal (§14.8.2.3.3)"
                : arg.Pic is { Category: PicCategory.Numeric, IsFloat: false } ? null
                : "COMPUTE-rule conformance needs a numeric argument (ISO §14.8.2.3.3 rule 2a)",
            PicCategory.ObjectReference =>
                arg.Pic is { Category: PicCategory.ObjectReference } ap
                    ? OoConformance.ObjectRefWideningMismatch(host.OoClasses, ap, f)
                    : "an object-reference formal takes an object-reference argument (SET rules, §14.8.2.3.3)",
            // ⭐ BOOLEAN / NATIONAL / NUMERIC-EDITED FORMALS ASK TABLE 16, NOT STRICT IDENTITY (fix-queue PB53).
            // This arm used to call DescriptionMismatch — which is §14.8.2.3.2, the BY **REFERENCE** rule —
            // described in its own comment as a "conservative strict gate". It was not conservative, it was the
            // WRONG CLAUSE: §14.8.2.3.3 rule 2d says a BY CONTENT crossing whose formal is not numeric, not an
            // index item and not ANY LENGTH conforms "as for a MOVE statement", i.e. by §14.9.25.3 Table 16.
            // Identity is far narrower, so three pairings the standard admits were refused with a "category
            // mismatch" naming a rule that does not govern the crossing:
            //     boolean → national · alphanumeric → boolean · national → boolean
            // ⚠ ANY LENGTH keeps its own answer FIRST: §14.8.2.3.3 rule 2c makes such a formal's length
            // "considered to match", which is a statement about LENGTH and leaves the category pair to 2d.
            _ => formal.IsAnyLength && !arg.IsAnyLength ? null
                : MoveTable16.Refusal(Table16Operand.Of(arg), Table16Operand.Of(formal)),
        };
    }

    /// <summary>Decode INVOKE's literal-1 (§14.9.23.3 SR2 — class alphanumeric or national): an alphanumeric
    /// STRINGLIT, a national N"…" literal (the method NAME is its character value — §8.3.2.2 comparison), or
    /// a hex X"…" literal (byte pairs decoded through the alphanumeric runtime encoding). Null for a literal
    /// class SR2 excludes (boolean B"…", figurative constants) — the caller diagnoses.</summary>
    /// <summary>Bind an INVOKE through a UNIVERSAL receiver (D10/D-U5; §14.9.23.4 GR7c): no compile-time
    /// conformance — each argument and the RETURNING item carry their CONFORMANCE DESCRIPTOR for the
    /// callee's runtime check (§9.3.8.2.1 NOTE). Argument rules, all COBOLNET0866 with citations: explicit
    /// BY CONTENT/BY VALUE are forbidden (SR6 :28435 — BY REFERENCE is assumed implicitly); a literal or
    /// arithmetic-expression argument cannot cross by reference (SR6 + GR6's non-universal-only scope);
    /// OBJECT data may not cross at all (SR10 bans by-reference and SR6 removes the typed path's GR6a2
    /// auto-CONTENT fallback); a Tier-C group (no character image) has no crossing form.</summary>
    private BoundStatement OoBindUniversalInvoke(
        Core.InvokeStatementContext inv, Place receiver, string? methodLiteral, Place? methodSource)
    {
        var argCtxs = inv.invokeUsing()?.invokeArgument() ?? [];
        var args = new List<BoundUniversalArg>(argCtxs.Length);
        foreach (var a in argCtxs)
        {
            if (a.VALUE() is not null || a.CONTENT() is not null)
            {
                ctx.Edition.Error("COBOLNET0866",
                    "INVOKE through a universal object reference: neither BY CONTENT nor BY VALUE may be "
                    + "specified — BY REFERENCE is assumed implicitly (ISO §14.9.23.3 SR6)");
                return new BoundNop();
            }
            if (a.dataReference() is not { } dref)
            {
                ctx.Edition.Error("COBOLNET0866",
                    "INVOKE through a universal object reference: a literal or arithmetic-expression "
                    + "argument cannot cross BY REFERENCE (ISO §14.9.23.3 SR6 — every universal argument "
                    + "is implicitly BY REFERENCE)");
                return new BoundNop();
            }
            if (ctx.Refs.Resolve(dref) is not { } p)
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE: the argument '{dref.GetText()}' is not resolvable to storage");
                return new BoundNop();
            }
            if (ctx.Data.OoIsObjectData(p.Item))
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE through a universal object reference: '{p.Item.CobolName}' is OBJECT "
                    + "(factory/instance) data — it may not cross BY REFERENCE (ISO §14.9.23.3 SR10), and "
                    + "the universal path has no BY CONTENT fallback (SR6)");
                return new BoundNop();
            }
            string d = OoConformance.ConformanceDescriptor(p.Item);
            if (d == "T:!")
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE: the argument '{p.Item.CobolName}' has no crossing form (a Tier-C group or a "
                    + "not-yet-carried category — mirrors the typed path's rejection)");
                return new BoundNop();
            }
            args.Add(new BoundUniversalArg(p, d));
        }

        Place? retPlace = null;
        string? retDesc = null;
        if (inv.invokeReturning()?.dataReference() is { } retRef)
        {
            if (ctx.Refs.Resolve(retRef) is not { } rp)
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE RETURNING '{retRef.GetText()}': the receiving identifier is not resolvable "
                    + "to storage");
                return new BoundNop();
            }
            retDesc = OoConformance.ConformanceDescriptor(rp.Item);
            if (retDesc == "T:!")
            {
                ctx.Edition.Error("COBOLNET0866",
                    $"INVOKE RETURNING '{rp.Item.CobolName}': no crossing form (Tier-C / not-carried)");
                return new BoundNop();
            }
            retPlace = rp;
        }
        // GR2a/§8.3.2.2: the selector is a user-defined word — normalize the LITERAL at bind time (the
        // identifier-2 value normalizes at runtime via CobolObject.NormalizeMethodName).
        return new BoundInvokeUniversal(receiver, methodLiteral?.TrimEnd().ToUpperInvariant(), methodSource,
            args, retPlace, retDesc);
    }

    /// <summary>SET Format 5 core (§14.9.39; D-U7) — shared by the grammar's NULL/SELF/SUPER-sender rule
    /// and BindSetTo's SEMANTIC re-route (a dataReference sender parses as the Format-1 shape). Rules, all
    /// COBOLNET0867: every target an object-reference item (SR8 :31298); SUPER sender rejected (SR9
    /// :31300); SELF only inside a method, and a TYPED target requires the current class to conform
    /// (SR12c :31353); a dataReference sender must be an object-reference item, and a TYPED target
    /// requires a TYPED, conforming sender (SR12a2 :31341 — universal-into-typed is OUTSIDE SR12's closed
    /// list: the narrowing tool is an object view, the EC-OO wave); a UNIVERSAL target is unconstrained
    /// (SET universal TO typed is unconditionally legal). An unresolvable sender that names a CLASS of the
    /// group is the SR13 factory-object form — the factory singleton reference (D11 makes it directly
    /// emittable).</summary>
    public BoundStatement OoBindSetObjectRef(
        IReadOnlyList<Core.DataReferenceContext> targetRefs,
        Core.DataReferenceContext? senderRef, bool senderNull, bool senderSelf, bool senderSuper)
    {
        // SET … TO object-reference (§14.9.39 Format 5) is a COBOL-2002 introduction; the edition gate moved to the
        // post-bind VersionConformancePass (PHASE-03 Step 14b) — it fires on the self-identifying BoundSetObjectRef
        // node this convergence point (NULL/SELF/SUPER route + the data-sender re-route) produces.
        if (senderSuper)
        {
            ctx.Edition.Error("COBOLNET0867",
                "SET … TO SUPER: SUPER shall not be the sending operand of an object-reference SET "
                + "(ISO §14.9.39.3 SR9)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (string.Equals(t.GetText(), "EXCEPTION-OBJECT", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Edition.Error("COBOLNET0848",
                    "SET EXCEPTION-OBJECT: the predefined object reference shall not be a receiving "
                    + "operand (ISO §8.4.3.6 SR1)");
                return new BoundNop();
            }
            if (ctx.Refs.Resolve(t) is not { } tp || tp.Item.Pic is not { Category: PicCategory.ObjectReference })
            {
                ctx.Edition.Error("COBOLNET0867",
                    $"SET '{t.GetText()}': the receiving operand of an object-reference SET shall be a "
                    + "USAGE OBJECT REFERENCE data item (ISO §14.9.39.3 SR8)");
                return new BoundNop();
            }
            targets.Add(tp);
        }

        Place? src = null;
        string? srcFactoryClassCs = null;
        if (senderSelf)
        {
            if (host.OoCurrentClass is not { } cur)
            {
                ctx.Edition.Error("COBOLNET0867",
                    "SET … TO SELF: SELF is defined only within a method of a class (ISO §14.9.39.3 SR12c)");
                return new BoundNop();
            }
            // The receiver decides WHICH rule governs a SELF sender: SR12c when it is described with an
            // object-class-name, SR10d when it is described with an interface-name. A UNIVERSAL receiver
            // (ObjectClassName null) constrains nothing — SR8.
            foreach (var tp in targets)
            {
                if (tp.Item.Pic!.ObjectClassName is not { } tcn) continue;
                if (host.OoClasses?.Find(tcn) is { } tcls)
                {
                    if (!cur.ConformsTo(tcls))
                        ctx.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO SELF: class '{cur.Name}' is not '{tcls.Name}' or a "
                            + "subclass of it (ISO §14.9.39.3 SR12c2)");
                }
                // §14.9.39.3 SR10d — "the predefined object reference SELF, subject to the following rules:
                // 1. if the SET statement is contained in a method within the FACTORY definition of the class,
                // that factory definition shall be described with an IMPLEMENTS clause that references int-1,
                // 2. if … within the INSTANCE definition …, that instance definition shall be described with an
                // IMPLEMENTS clause that references int-1". `Find` is class-only, so before this an
                // interface-typed receiver fell through both arms unchecked and the emitter rendered a raw
                // `(I)(this)` cast — a runtime InvalidCastException, or a Roslyn CS error on generated user
                // source for a sealed class, which the G4 no-CS-on-user-source rule forbids.
                else if (host.OoClasses?.FindInterface(tcn) is { } tiface
                         && !host.OoClasses.ImplementsClosure(cur, host.OoInFactory).Contains(tiface))
                    ctx.Edition.Error("COBOLNET0867",
                        $"SET '{tp.Item.CobolName}' TO SELF: the {(host.OoInFactory ? "factory" : "instance")} "
                        + $"definition of class '{cur.Name}' does not IMPLEMENT interface '{tiface.Name}' "
                        + $"(ISO §14.9.39.3 SR10d{(host.OoInFactory ? 1 : 2)})");
            }
        }
        else if (!senderNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET object-reference sender shape");
            var sp = ctx.Refs.Probe(senderRef);   // Probe — EXCEPTION-OBJECT below is a legal alternative (R30)
            if (sp is not null && sp.Item.Pic is { Category: PicCategory.ObjectReference } spic)
            {
                foreach (var tp in targets)
                    if (tp.Item.Pic!.ObjectClassName is not null
                        && OoConformance.ObjectRefWideningMismatch(host.OoClasses, spic, tp.Item.Pic!) is { } werr)
                        ctx.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO '{sp.Item.CobolName}': {werr} "
                            + "(ISO §14.9.39.3 SR12 — a universal sender needs an object view to narrow)");
                src = sp;
            }
            else if (string.Equals(senderRef.GetText(), "EXCEPTION-OBJECT", StringComparison.OrdinalIgnoreCase))
                // §8.4.3.6 — the predefined register (ONE per run unit, GR2; implicitly universal SR2):
                // a universal target copies the reference; a TYPED target gets the RUNTIME narrow check
                // in the emitter (§9.3.8.2 :12291 — EC-OO-UNIVERSAL on failure; the SR12 closed list is
                // satisfied through the object-view-equivalent runtime conformance this register carries).
                return new BoundSetObjectRef(targets, null, false, false) { FromExceptionObject = true };
            else if (senderRef.cobolWord()?.GetText() is { } sname && host.OoClasses?.Find(sname) is { } scls)
            {
                // SR13 (:31371): the sender names a CLASS → the factory object of that class. D11's
                // singleton makes it a direct reference; conformance into a TYPED target is the FACTORY
                // conformance question — v1 permits only a UNIVERSAL target (factory-class hierarchies
                // widen via FACTORY OF phrases the USAGE grammar does not carry yet — 0899-noted).
                foreach (var tp in targets)
                    if (tp.Item.Pic!.ObjectClassName is not null)
                    {
                        ctx.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO {sname}: a factory-object sender (SR13) into a "
                            + "TYPED receiver needs the FACTORY OF usage phrase — not yet carried "
                            + "(universal receivers accept it)");
                        return new BoundNop();
                    }
                srcFactoryClassCs = scls.FactoryCsName;
            }
            else
            {
                ctx.Edition.Error("COBOLNET0867",
                    $"SET … TO '{senderRef.GetText()}': the sending operand shall be an object-reference "
                    + "data item, NULL, SELF, or a class-name (ISO §14.9.39.3 SR9/SR12/SR13)");
                return new BoundNop();
            }
        }
        return new BoundSetObjectRef(targets, src, senderNull, senderSelf) { SourceFactoryCs = srcFactoryClassCs };
    }

    /// <summary>True when an arithmetic expression is EXACTLY one bare data reference (the Format-5
    /// re-route's sender shape) — its single dataReference descendant spans the whole expression text.</summary>
    public static Core.DataReferenceContext? OoExtractBareReference(Core.ArithmeticExpressionContext e)
    {
        Core.DataReferenceContext? only = null;
        var stack = new Stack<Antlr4.Runtime.Tree.IParseTree>();
        stack.Push(e);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur is Core.DataReferenceContext d)
            {
                if (only is not null) return null;
                only = d;
                continue;
            }
            for (int i = 0; i < cur.ChildCount; i++) stack.Push(cur.GetChild(i));
        }
        return only is not null && only.GetText() == e.GetText() ? only : null;
    }

    private static string? OoDecodeMethodNameLiteral(Core.LiteralContext? lit)
    {
        var nn = lit?.nonNumericLiteral();
        if (nn is null) return null;
        if (nn.STRINGLIT() is { } sl) return CobolLiteral.Decode(sl.GetText());
        if (nn.NATLIT() is { } nat)
        {
            string t = nat.GetText();
            return t.Length >= 3 ? CobolLiteral.Decode(t[1..]) : "";   // strip the N prefix, decode the body
        }
        if (nn.HEXLIT() is { } hex) return CobolLiteral.DecodeHex(hex.GetText());   // §8.3.3.2 — the ONE hex codec
        return null;
    }

    /// <summary>The significant-digit count of a numeric literal rescaled to <paramref name="scale"/> (the
    /// same string math as the emitter's <c>EmitText.UnscaledAtScale</c>, counting only — the bind-time
    /// fits-the-formal check for literal arguments, §14.8.2).</summary>
    private static int OoUnscaledDigitCount(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+').TrimStart('-');
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        string digits = scale >= 0
            ? intPart + (fracPart.Length < scale ? fracPart.PadRight(scale, '0') : fracPart[..scale])
            : (intPart + fracPart) is var all && all.Length > -scale ? all[..^(-scale)] : "0";
        return digits.TrimStart('0').Length;
    }

    /// <summary>The §14.8.2/§14.8.3 STRICT conformance check between a formal/returning item and an
    /// argument/receiver item — delegates to the ONE shared description-equality rule
    /// (<see cref="OoConformance.DescriptionMismatch"/>, also the §9.3.8.2 override-signature check) that
    /// makes the emitted marshaling TYPE-PRESERVING. Null when conformant, else the mismatch.</summary>
    private static string? OoConformanceError(DataItem formal, DataItem arg)
        // Activation mode: §14.8.2.3.2 rules d/e for arguments; for the INVOKE RETURNING delivery pair the
        // sender (parameter 1 = the method's returning item) being ANY LENGTH matches any receiver length
        // (§14.8.3.3 rule 5) while an ANY LENGTH receiver demands an ANY LENGTH sender (rule 4).
        => OoConformance.DescriptionMismatch(formal, arg, anyLengthActivationRelax: true);

    // ── Method-context control flow (deep-dive D8) ──────────────────────────────────────────────────────────

    /// <summary>GOBACK inside a METHOD (§14.9.18.4 GR4): terminate the METHOD, control back to the INVOKE
    /// site. The RETURNING-item delivery is the method entry's job (slice 2 — no formals yet); GOBACK's own
    /// phrases in a method context stage loud (RAISING → the EC-OO slice; the RETURNING/GIVING and 2023
    /// status phrases are activation-result forms that do not apply to a method return).</summary>
    public BoundStatement OoBindMethodGoback(Core.GobackStatementContext g)
    {
        if (g.dataReference() is not null)
            return new BoundUnsupported("GOBACK with a RETURNING/GIVING phrase inside a method "
                + "(ISO §14.9.18.4 GR4 returns the METHOD's RETURNING item — an activation-result form)");
        return new BoundMethodReturn(OoBindMethodRaising(g.raisingPhrase(), "GOBACK"));
    }

    /// <summary>Bind a method-context RAISING phrase (§14.9.18.4 GR1b — staged before the MethodReturn
    /// throw; the INVOKE site picks up). RAISING LAST inside a method needs method DECLARATIVES (SR5: only
    /// in a declarative/WHEN) — staged with the method-declaratives refinement.</summary>
    private BoundRaising? OoBindMethodRaising(Core.RaisingPhraseContext? raising, string verb)
    {
        if (raising is null) return null;
        if (raising.LAST() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.OoMethodRaisingLast,
                $"{verb} RAISING LAST EXCEPTION inside a method: LAST is legal only within a declarative "
                + "or a PERFORM WHEN (ISO §14.9.18.3 SR5) — method declaratives are a later refinement of "
                + "the EC-OO wave");
            return null;
        }
        return host.Ec.EcBindRaising(raising, raising.Start.Line, verb);
    }

    /// <summary>EXIT METHOD (pre-2023 editions — REMOVED by 2023, Annex E.2; the <c>exit-method-window</c>
    /// registry row already flags 0900/0902 at the window edges): inside a method it is the method-return
    /// synonym (≡ the §14.9.18.4 GR4 GOBACK); outside one it violates its placement rule.</summary>
    public BoundStatement OoBindExitMethod(Core.ExitStatementContext e)
    {
        if (!host.InMethod)
        {
            ctx.Edition.Error("COBOLNET0827",
                "EXIT METHOD may be specified only in a method definition (ISO §14.9.14 — the method form "
                + "of the EXIT statement; this is not a method procedure division)");
            return new BoundNop();
        }
        return new BoundMethodReturn(OoBindMethodRaising(e.raisingPhrase(), "EXIT METHOD"));
    }
}
