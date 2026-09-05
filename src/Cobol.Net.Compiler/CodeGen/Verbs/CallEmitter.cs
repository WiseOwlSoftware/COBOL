// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The CALL / CANCEL / GOBACK / EXIT PROGRAM verb emitter (P7 Step 9m, BATCH-3a — a real collaborator
/// over the per-unit <see cref="EmitContext"/>; ISO §14.9.4 / §14.9.5 / §14.9.14 / §14.9.18): the activation
/// call with its BY REFERENCE/CONTENT/VALUE carriers, the EC-PROGRAM catch + RAISING propagation pickup, and
/// the ONE CALL-boundary string-carrier trio (<see cref="CallPlaceIsString"/>/<see cref="CallStringRead"/>/
/// <see cref="CallStringWrite"/>) Report Writer and the program-class emission reuse.</summary>
internal sealed class CallEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, CallUnitState callState,
    EcEmitter ec, MoveEmitter move)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the ON/NOT-ON
    /// EXCEPTION phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    internal static string CallBool(bool b) => b ? "true" : "false";

    // ── Statement emitters: CALL / CANCEL / GOBACK ──────────────────────────────────────────────────────────

    /// <summary>Emit one CALL (ISO §14.9.4.4). With no exception phrase, a CALL failure (not found / recursive
    /// re-entry) propagates and terminates the run unit loudly (the 85 abnormal-termination surface; the
    /// EC-PROGRAM model is the §11 subsystem). With a phrase, the failure runs the ON imperative and control
    /// falls to the end of the CALL (GR3h); NOT ON runs only on a successful return (GR3i).</summary>
    public bool EmitCall(BoundCallProgram c)
    {
        var w = ctx.Writer;
        // §14.9.4.4 GR3a (kb/Work PB133 wave B): "item identification is done for identifier-3 at the
        // beginning of the execution of the CALL statement" — and 14.2.3 GR8 fixes each BY REFERENCE
        // argument's STORAGE AREA at the same point. The aliasing carriers re-render their subscript and
        // ref-mod expressions on every access, so a callee that reaches the caller's index item through
        // another BY REFERENCE argument could re-aim them mid-call; each variable index is hoisted into a
        // statement-local evaluated here, once.
        c = HoistOnceOnlyIdentification(c, w);
        string args = ArgsArrayText(c);
        string ret = c.Returning is { } rp ? RefCarrier(rp) : "null";
        // An EC-active group's CALL site consumes a callee-staged RAISING propagation itself (the pickup below
        // runs the §14.9.49 F3 selection and honors RESUME); the registry's boundary default stands down.
        string invocation;
        if (c.IsPointerTarget && c.DynamicName is BoundFieldOperand ppf)
            // CALL through a PROGRAM-POINTER (§14.9.4.3 SR1 / GR :26177; P10 Step 7): activate the HELD
            // program — the pointer's carrier goes straight to the registry, never a name-string read.
            invocation = $"ProgramRegistry.CallPointer({PlaceRenderer.Read(ppf.Place)}, {CsLiteral(callState.SelfPath)}, {args}, {ret}"
                + $"{(ecState.Active ? ", siteHandlesPropagation: true" : "")});";
        else
        {
            string nameExpr = c.LiteralName is { } literal
                ? CsLiteral(literal)
                : $"({OperandText.AsString(c.DynamicName!, num)}).Trim()";   // GR3b — the identifier's value at CALL time (GR3a: read once)
            // §14.9.4.4 GR3d's ACTIVATING half (kb/Work PB133 wave C2b): this statement's TURN state.
            bool argChk = EnabledProgramNames().Contains("EC-PROGRAM-ARG-MISMATCH");
            invocation = $"ProgramRegistry.CallProgram({nameExpr}, {CsLiteral(callState.SelfPath)}, {args}, {ret}"
                + $"{(ecState.Active ? ", siteHandlesPropagation: true" : "")}"
                + $"{(c.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "")}"
                + $"{(argChk ? ", siteArgMismatchChecking: true" : "")});";
        }

        var ecProg = EnabledProgramNames();
        // The ACTIVATING half of §14.8.4.1's both-elements rule: this CALL statement's enabled EC-EXTERNAL-*
        // set becomes the pending site mask the activation boundary latches for the activated element's
        // Describe gate (§14.9.4.4 GR3e — "enabled ... in both the activated program and activating runtime
        // element"). Zero-scaffolding: an EC-free site emits nothing (the boundary re-zeroes after every call).
        int siteExternalMask = ecProg.Sum(ExternalBit);
        if (siteExternalMask != 0)
            w.Line($"ExceptionState.ExternalCheckMask = {siteExternalMask};   // §14.8.4.1 — this CALL's EC-EXTERNAL enablement (the activating element)");
        // ── §14.9.4.4 GR3h/GR3i: the CALL statement's exception partition (kb/Work PB233) ────────────────────
        // ON EXCEPTION is the ONLY phrase that diverts a failed activation. GR3h item 1 names it explicitly,
        // and §14.6.13.1.3 #1 admits only "a conditional phrase WITHOUT the NOT phrase" — so a CALL written
        // with only NOT ON EXCEPTION behaves exactly like a CALL with no phrase at all (item 2 or item 3
        // governs). Keying the catch on "either phrase" let a NOT-ON-only CALL SWALLOW a failed activation.
        bool hasOn = c.OnException is not null;
        bool hasPhrase = hasOn || c.NotOnException is not null;
        var ecOther = EnabledOtherCallNames();
        if (!hasOn && ecProg.Count == 0 && ecOther.Count == 0)
        {
            // Nothing catches: the condition leaves the statement and takes §14.6.13.1 (item 3 → #8, this
            // implementation's loud abnormal termination). A NOT ON phrase can only be reached by a normal
            // return, so it needs no guard here — GR3i.
            w.Line(invocation);
            if (c.NotOnException is { } notBare) Statements.EmitStatementList(notBare);   // GR3i — a non-exception return
            EmitPropagationPickup();
            return false;
        }
        int id = ctx.Names.NextCall();
        if (hasPhrase) w.Line($"bool __callErr{id} = false;");
        using (w.Block("try"))
            w.Line(invocation);
        // The arms, in the ONE order that keeps each reachable (a narrower filter must precede a broader one):
        //   1. enabled EC-PROGRAM-*/EC-EXTERNAL-*  → status set, then the phrase (item 1) or the declaratives (item 2)
        //   2. enabled non-EC-PROGRAM carriers     → status set, then the declaratives ALWAYS (item 2, 2nd disjunct)
        //   3. UNenabled EC-PROGRAM-*/EC-EXTERNAL-* → the phrase only (item 1 carries no checking-enabled qualifier),
        //      with NO status set (§14.6.13.1.1 sets an indicator only when checking is enabled).
        // Anything else — a name no arm claims, or ANY condition that escaped the CALLED program's execution —
        // falls through to §14.6.13.1, because §14.9.4.4 GR3i says that once the program "was successfully
        // called" the ON EXCEPTION phrase is ignored.
        string? flag = hasPhrase ? $"__callErr{id}" : null;
        if (ecProg.Count > 0) EmitCallEcCatch(ecProg, byPhrase: hasOn, flag);
        if (ecOther.Count > 0) EmitCallEcCatch(ecOther, byPhrase: false, flag);
        if (hasOn)
        {
            int pid = ctx.Names.NextEc();
            w.Line($"catch (CobolCallException __cp{pid}) when (!__cp{pid}.ControlTransferred "
                + $"&& {RuntimeApi.CallEcIsProgramOrExternalText($"__cp{pid}.EcName")}) {{ {flag} = true; }}"
                + "   // §14.9.4.4 GR3h item 1 (checking not enabled → no status is set) / GR3i");
        }
        if (c.OnException is { } on)
        {
            using (w.Block($"if (__callErr{id})")) Statements.EmitStatementList(on);
            if (c.NotOnException is { } notAlso)
                using (w.Block("else")) Statements.EmitStatementList(notAlso);
        }
        else if (c.NotOnException is { } not)
            using (w.Block($"if (!__callErr{id})")) Statements.EmitStatementList(not);   // GR3i — only on a non-exception return
        EmitPropagationPickup();
        return false;
    }

    /// <summary>§14.9.4.4 GR3a's once-only identification (kb/Work PB133 wave B): rewrite the ALIASING
    /// operands' places so every non-constant table subscript and ref-mod position is a hoisted local. The
    /// value operands (BY CONTENT / BY VALUE snapshots) already read once when the args array is built.</summary>
    private BoundCallProgram HoistOnceOnlyIdentification(BoundCallProgram c, CodeWriter w)
    {
        bool needed = (c.Returning is { } r0 && HasVariableIndex(r0))
            || c.Args.Any(a => a.Mode == CobolPassMode.Reference && a.Place is { } p0 && HasVariableIndex(p0));
        if (!needed) return c;
        var args = c.Args
            .Select(a => a.Mode == CobolPassMode.Reference && a.Place is { } p ? a with { Place = HoistPlace(p, w) } : a)
            .ToList();
        var ret = c.Returning is { } rp && HasVariableIndex(rp) ? HoistPlace(rp, w) : c.Returning;
        return c with { Args = args, Returning = ret };
    }

    private static bool HasVariableIndex(Place p) => p switch
    {
        RefModPlace rm => !IsConstIndex(rm.Start) || (rm.Length is { } l && !IsConstIndex(l)) || HasVariableIndex(rm.Inner),
        PlaceDecorator d => HasVariableIndex(d.Inner),
        MemberPlace mp => PathHasVariableIndex(mp.Path),
        DynTablePlace dp => PathHasVariableIndex(dp.Path),
        _ => false,
    };

    private static bool PathHasVariableIndex(AccessPath path) => path.Segments.Any(s => s switch
    {
        FixedTableSegment ft => !IsConstIndex(ft.OneBasedIndex),
        DynTableSegment dt => !IsConstIndex(dt.OneBasedIndex),
        _ => false,
    });

    private static bool IsConstIndex(string rendered) => long.TryParse(rendered.Trim(), out _);

    private int _gr3aSeq;

    private Place HoistPlace(Place p, CodeWriter w)
    {
        switch (p)
        {
            case RefModPlace rm:
                return rm with
                {
                    Inner = HoistPlace(rm.Inner, w),
                    Start = HoistIndex(rm.Start, w),
                    Length = rm.Length is { } l ? HoistIndex(l, w) : null,
                };
            case MemberPlace mp when PathHasVariableIndex(mp.Path):
                return mp with { Path = HoistPath(mp.Path, w) };
            case DynTablePlace dp when PathHasVariableIndex(dp.Path):
                return dp with { Path = HoistPath(dp.Path, w) };
            default:
                return p;   // constant or index-free — nothing to pin
        }
    }

    private AccessPath HoistPath(AccessPath path, CodeWriter w)
    {
        var segs = new List<AccessSegment>(path.Segments.Count);
        foreach (var s in path.Segments)
            segs.Add(s switch
            {
                FixedTableSegment ft when !IsConstIndex(ft.OneBasedIndex) => new FixedTableSegment(HoistIndex(ft.OneBasedIndex, w)),
                DynTableSegment dt when !IsConstIndex(dt.OneBasedIndex) => new DynTableSegment(HoistIndex(dt.OneBasedIndex, w)),
                _ => s,
            });
        return new AccessPath(segs);
    }

    private string HoistIndex(string rendered, CodeWriter w)
    {
        if (IsConstIndex(rendered)) return rendered;
        string local = $"__ci{_gr3aSeq++}";
        w.Line($"var {local} = {rendered};   // §14.9.4.4 GR3a — identified once, at the CALL's start");
        return local;
    }

    /// <summary>The <c>CobolArg[]</c> expression of one bound call's arguments — the ONE argument-array text
    /// both the statement-position <see cref="EmitCall"/> and the per-evaluation
    /// <see cref="FunctionActivationText"/> render (singular-pattern rule).</summary>
    private string ArgsArrayText(BoundCallProgram c) => c.Args.Count == 0
        ? "System.Array.Empty<CobolArg>()"
        : $"new CobolArg[] {{ {string.Join(", ", c.Args.Select(ArgText))} }}";

    /// <summary>The single-statement activation text of one user-defined-function call for an EXPRESSION-POSITION
    /// per-evaluation window (<c>BoundUdfEvaluated</c> — ISO §8.4.3.2.4 GR1/GR6a: the activation runs when the
    /// containing condition text evaluates). Function references carry no ON EXCEPTION phrases (§8.4.3.2), and a
    /// declarative RESUME pickup is a statement-position surface (<c>__pc</c>-anchored) that cannot run inside an
    /// expression — so the invocation goes out WITHOUT <c>siteHandlesPropagation</c>: a callee-staged RAISING
    /// condition takes the registry's activation-boundary default (fatal → loud termination, nonfatal → stands in
    /// the last-exception status; ISO §14.6.13.1.3 #8 / §14.6.13.1.4 — the same posture as an EC-free caller).</summary>
    internal string FunctionActivationText(BoundCallProgram c) =>
        $"ProgramRegistry.CallProgram({CsLiteral(c.LiteralName!)}, {CsLiteral(callState.SelfPath)}, "
        + $"{ArgsArrayText(c)}, {(c.Returning is { } rp ? RefCarrier(rp) : "null")}, "
        + "notFoundEc: \"EC-FUNCTION-NOT-FOUND\");";   // §8.4.3.2.4 GR6b — a UDF locate miss is EC-FUNCTION-NOT-FOUND

    /// <summary>The current statement's enabled level-3 names that a <see cref="CobolCallException"/> can
    /// actually carry (empty when none / no wrapper). ONE filter, asked once and split two ways below: an
    /// enabled name outside <see cref="CobolCallException.CarriedNames"/> — EC-PROGRAM-RESOURCES and
    /// EC-PROGRAM-ARG-OMITTED are the live examples, the latter having left this carrier at kb/Work PB133 —
    /// has no raise site to match, so naming it in a catch filter emits a disjunct that can never be true, and
    /// a <c>&gt;&gt;TURN EC-ALL CHECKING ON</c> unit would emit a two-hundred-way one on every CALL.</summary>
    private List<string> EnabledCallNames() =>
        ecState.Info?.Enabled.Select(p => p.Ec).Where(RuntimeApi.CallEcIsCarried).ToList() ?? [];

    /// <summary>The enabled EC-PROGRAM-* / EC-EXTERNAL-* names of the current statement — the two families a
    /// CALL raises through <see cref="CobolCallException"/> (ISO §14.9.4.4 GR3b–f: locate/recursion/argument
    /// failures; GR3e: the §14.8.4 external-conformance trio). All are Table 13 Fatal and GR3h item 1 gives the
    /// ON EXCEPTION phrase both families, so they share one catch arm. The partition itself is
    /// <see cref="CobolCallException.IsProgramOrExternal"/> — written down ONCE, next to the carrier, so this
    /// compile-time split and the emitted runtime filter cannot drift apart. Also the source of this CALL's
    /// §14.8.4.1 EC-EXTERNAL site mask and of GR3d's ACTIVATING-half argument-checking flag.</summary>
    private List<string> EnabledProgramNames() =>
        EnabledCallNames().Where(RuntimeApi.CallEcIsProgramOrExternal).ToList();

    /// <summary>The complement: enabled carriable names NOT in GR3h item 1's two families (today only
    /// EC-FUNCTION-NOT-FOUND — §8.4.3.2.4 GR6b, a user-defined-function locate miss). These take ISO §14.9.4.4
    /// GR3h item 2's SECOND disjunct — "or if the exception condition is not one of the EC-PROGRAM exception
    /// conditions, any applicable exception processing statements are executed" — with NO ON EXCEPTION escape,
    /// which is why they need an arm of their own rather than a share of the family arm.</summary>
    private List<string> EnabledOtherCallNames() =>
        EnabledCallNames().Where(n => !RuntimeApi.CallEcIsProgramOrExternal(n)).ToList();

    /// <summary>The <see cref="ExternalChecks"/> bit of one EC-EXTERNAL level-3 name (0 for any other name) —
    /// the emitted CALL-site mask is the OR over the statement's enabled set.</summary>
    private static int ExternalBit(string ec) => ec switch
    {
        "EC-EXTERNAL-FORMAT-CONFLICT" => (int)ExternalChecks.FormatConflict,
        "EC-EXTERNAL-DATA-MISMATCH" => (int)ExternalChecks.DataMismatch,
        "EC-EXTERNAL-FILE-MISMATCH" => (int)ExternalChecks.FileMismatch,
        _ => 0,
    };

    /// <summary>Emit ONE name-filtered <c>catch (CobolCallException)</c> arm of a CALL under enabled checking
    /// (§9.1.13-style bridge for the inter-program family: the runtime latched the Table 13 level-3 name in
    /// <see cref="CobolCallException.EcName"/>): set the last exception status (§14.6.13.1.1), flag the
    /// statement as failed (so GR3i's NOT ON phrase cannot run over a failed activation), then either leave it
    /// to the statement's own ON EXCEPTION phrase — <paramref name="byPhrase"/>, §14.6.13.1.3 #1 / §14.9.4.4
    /// GR3h item 1 — or run the §14.9.49 F3 selection with the fatal default (every name reachable here is
    /// Table 13 Fatal: the EC-PROGRAM-*/EC-EXTERNAL-* families and EC-FUNCTION-NOT-FOUND).
    /// <para><c>!ControlTransferred</c> is the GR3h/GR3i boundary: GR3h speaks only of a program that "was not
    /// successfully called", so an exception raised INSIDE the called program's execution is none of this
    /// statement's business (GR3i) and must fall through to §14.6.13.1. A CobolCallException whose name is not
    /// enabled likewise falls through to the next arm / propagates — the checking-off behavior unchanged.</para>
    /// </summary>
    private void EmitCallEcCatch(List<string> ecNames, bool byPhrase, string? phraseFlag)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        string nameTest = string.Join(" || ", ecNames.Select(n => $"__ce{id}.EcName == {CsLiteral(n)}"));
        using (w.Block($"catch (CobolCallException __ce{id}) when (!__ce{id}.ControlTransferred && ({nameTest}))"))
        {
            // The §15.32.3 r2 pair rides the CALL statement's ambient context (kb/Work R14) — the callee's own
            // contexts were restored on unwind, so this Set attributes the CALL, not the callee's last statement.
            w.Line($"ExceptionState.Set(__ce{id}.EcName, true);   // §14.6.13.1.1 — every name reachable here is fatal (Table 13)");
            if (phraseFlag is not null)
                w.Line($"{phraseFlag} = true;   // the activation failed — GR3i's NOT ON phrase shall not run");
            if (byPhrase)
                w.Line("// the statement's ON EXCEPTION phrase handles it (§14.6.13.1.3 #1; §14.9.4.4 GR3h item 1)");
            else
            {
                w.Line($"int __r{id} = {ec.EcDispatchExpr($"__ce{id}.EcName", "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                w.Line($"if (__r{id} != -2) throw new CobolFatalException(__ce{id}.EcName, __ce{id}.Message) {{ Dispatched = true }};   // §14.6.13.1.3 #5/#7 (dispatched here)");
            }
        }
    }

    /// <summary>Emit the activator-side pickup of a callee-staged <c>GOBACK / EXIT PROGRAM … RAISING</c>
    /// exception condition (ISO §14.9.18 GR — raised "as if a RAISE statement" at the end of the activating
    /// statement; §14.6.13.1.3 #6): run the §14.9.49 F3 selection over the DYNAMIC name, honor RESUME, and apply
    /// the fatal default. Emitted only when the group uses the EC model (<c>EcState.Active</c>) — the propagated name
    /// is dynamic (RAISING LAST EXCEPTION), so the gate is the group's EC participation, not a per-name TURN
    /// fold (the documented refinement, recorded in the deep-dive; an EC-free caller gets the registry's
    /// boundary default instead).</summary>
    public void EmitPropagationPickup()
    {
        if (!ecState.Active) return;
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        using (w.Block($"if (ExceptionState.TakePropagatedObject(out var __po{id}))   // §14.6.13.1.5 — an exception OBJECT propagated"))
        {
            w.Line($"ExceptionState.SetObject(__po{id});   // GR1b2 — the current exception object HERE (the activator)");
            w.Line($"int __or{id} = {ec.ObjDispatchExpr($"__po{id}")};   // rule 2 — USE AFTER EXCEPTION OBJECT (GR14)");
            w.Line($"if (__or{id} >= 0) {{ __pc = __or{id}; break; }}   // RESUME AT procedure-name");
            using (w.Block($"if (__or{id} == -3)   // rule 3 PROPAGATE ON: directive not implemented (residue); rule 4 —"))
            {
                w.Line("ExceptionState.Set(\"EC-OO-EXCEPTION\", true);   // as if EXCEPTION EC-OO-EXCEPTION (:24608)");
                w.Line($"int __oq{id} = {ec.EcDispatchExpr("\"EC-OO-EXCEPTION\"", "\"\"")};   // the name enters the F3 tiers");
                w.Line($"if (__oq{id} >= 0) {{ __pc = __oq{id}; break; }}");
                w.Line($"if (__oq{id} != -2) throw new CobolFatalException(\"EC-OO-EXCEPTION\", "
                    + "\"an exception object was not handled (ISO 14.6.13.1.5; Table 13 - fatal)\") { Dispatched = true };");
            }
            w.Line("// -1/-2: declarative completed / RESUME NEXT — normal continuation (:24604)");
        }
        using (w.Block($"if (ExceptionState.TakePropagated(out var __pn{id}, out var __pf{id}))   // §14.9.18 GR — raised at the end of the CALL"))
        {
            w.Line($"int __pr{id} = {ec.EcDispatchExpr($"__pn{id}", "\"\"")};");
            w.Line($"if (__pr{id} >= 0) {{ __pc = __pr{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            w.Line($"if (__pr{id} != -2 && __pf{id}) throw new CobolFatalException(__pn{id}, "
                + "\"exception condition propagated by GOBACK/EXIT PROGRAM RAISING and not resumed "
                + "(ISO 14.9.18; 14.6.13.1.3 #6/#7)\") { Dispatched = true };");
        }
    }

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument (caller side; design D1/D2).
    /// BY REFERENCE builds an accessor carrier over the caller's storage (§14.2.3 GR8); BY CONTENT/BY VALUE
    /// snapshot the value into a cell AT CALL INITIATION — which also realizes the §14.9.4.4 GR3a once-only
    /// evaluation for those modes. (A BY REFERENCE accessor over a SUBSCRIPTED operand re-evaluates the
    /// subscript inside the closure — the GR3a capture-into-locals refinement is a known follow-up.)</summary>
    public string ArgText(BoundCallArg a)
    {
        // §14.9.4.4 GR11 (kb/Work PB133 wave C): the omitted argument crosses as the NULL carrier —
        // CobolArgAdapt.Present answers false, the formal's adapters hand out the GR12 checked-raise carrier,
        // and a forwarded omitted formal stays omitted (GR1c) because IsNull rides the carrier itself.
        if (a.Omitted)
            return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, ManagedPointer.Null, 0, 0)";
        if (a.Place is { } p)
        {
            string digits = (p.Pic?.Digits ?? 0).ToString();
            string scale = (p.Pic?.Scale ?? 0).ToString();
            // ⛔ V59 RESIDUE FIX: the predicate is IsImageCapable, not the pre-V59 IsCharacterImage. A group whose
            // only non-character leaf is BINARY/PACKED now HAS a whole-group image — V59 gave those leaves their
            // pinned bytes — and `RecordStructEmitter` emits AsImage()/FromImage() for exactly IsImageCapable
            // items. Guarding on the stricter predicate therefore loud-staged a CALL whose codec had actually been
            // generated: `01 G. 05 N PIC S9(4) COMP. 05 A PIC X(3).` answered BYTE-LENGTH(G) = 5 and then threw
            // "no whole-group character image" on `CALL "SUB" USING G`. That claim was false, and refusing the
            // CALL rejected conforming source — §14.2.3 GR8 (`cite.py`-verified): "If the argument is passed by
            // reference, the activated runtime element operates as if the formal parameter occupies the same
            // storage area as the argument", which COBOL.NET realizes through the very image round-trip that
            // exists. Only a variable-length group or a group with a pointer/object-class leaf is still
            // genuinely imageless and stays loud (every NUMERIC leaf kind joined the image across kb/Work
            // PB164 waves 1–2 + the R40 INDEX pin) — the wording matches the predicate actually tested.
            // ⛔ THE PREDICATE IS BoundaryImageCapable, NOT IsImageCapable (kb/Work PB204). §14.8.2.2 admits a
            // VARIABLE-LENGTH group across a Format-2 boundary "subject to compatibility as described in
            // 8.5.1.12" — an admission, checked at bind by OoConformance.DescriptionMismatch — so staging it
            // loud here refused conforming source. Only a group with NO boundary image at all (a
            // pointer/object-class leaf, or a variable-length shape outside the current-extent gate) is loud.
            if (p.Item.IsGroup && !p.Item.BoundaryImageCapable && p is not RedefViewPlace)
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", TierCIsland.Reason(p.Item, "CALL USING group"))
                    + "), 0, 0)";
            if (a.Mode == CobolPassMode.Reference)
            {
                // §14.9.4.4 GR1c/GR12 (kb/Work PB133 wave C): forwarding a CARRIER-RESIDENT formal passes the
                // carrier ITSELF — presence (the omitted state) rides with it, GR1c's transitive omission
                // reaches the next callee, and the sanctioned as-an-argument reference form never touches the
                // accessors (a re-wrapped OverField over `__lnkpN.Value` would read — and GR12-raise — on an
                // omitted formal). A resident formal's CsName IS its carrier accessor (DataBinder sets
                // `__lnkpN.Value`); a SUBITEM or subscripted reference keeps the ordinary wrap, so referencing
                // inside an omitted formal still raises, as GR12 requires.
                if (p is MemberPlace { Path.Segments: [RootFieldSegment fr] }
                    && fr.CsField.StartsWith("__lnkp", StringComparison.Ordinal)
                    && fr.CsField.EndsWith(".Value", StringComparison.Ordinal))
                    return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, "
                        + $"{fr.CsField[..^".Value".Length]}, {digits}, {scale})";
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, {RefCarrier(p)}, {digits}, {scale})";
            }
            // BY CONTENT — "a record … allocated by the activating element" (§14.2.3 GR9) — and BY VALUE with
            // an identifier argument (a UDF BY VALUE formal, §8.4.3.2.4 GR5c): both are value snapshots at
            // call initiation; the mode rides the wire so the arg is honest about which rule produced it
            // (the BY VALUE callee re-conforms through its own NumValue cell, GR10).
            return CallPlaceIsVarGroup(p)
                ? $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, "
                  + $"{RuntimeApi.VarGroupCell(PlaceRenderer.VarGroupImage(p, "CALL argument"))}, {digits}, {scale})"
                : CallPlaceIsString(p)
                ? $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CallStringRead(p)}), {digits}, {scale})"
                : $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<{CallNumCarrier(p)}>.Cell({PlaceRenderer.Read(p)}), {digits}, {scale})";
        }
        switch (a.Value)
        {
            case BoundStringLiteral s:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CsLiteral(s.Value)}), 0, 0)";
            // ⛔ ONE NUMERIC-ARGUMENT FUNNEL for both non-place arms (kb/Work PB263 + PB264). A numeric literal
            // reaches this switch in EITHER bound shape — BY CONTENT and a bare Format-2 argument bind it as
            // BoundNumericLiteral, while BY VALUE binds it as a BoundComputedOperand wrapping a BoundNumLiteral
            // (CallBinder's byValue arm goes through BindByValueExpr) — and the two arms used to derive a
            // carrier and a scale EACH. They disagreed, so ONE rule ("a numeric literal argument crosses with
            // its exact value") produced three different wrong answers depending on how it was spelled.
            case BoundNumericLiteral n:
                return NumericArgText(a.Mode, n.Text);
            case BoundComputedOperand ce when ConstLiteralText(ce.Expr) is { } ct:
                return NumericArgText(a.Mode, ct);
            case BoundComputedOperand expr:
            {
                // A GENUINE runtime expression snapshots its computed value (§14.2.3 GR9/GR10 — the CALL BY
                // VALUE grammar leg binds Mode=Value; a UDF expression argument to a BY REFERENCE formal binds
                // Mode=Content per §8.4.3.2.4 GR5b — the mode is bound, not assumed here).
                // An unsigned-wide result (a HIGHEST-ALGEBRAIC fold literal — kb/Work R10) funnels through the
                // same DeU rule as every arithmetic consumer: loud beyond the Int128 intermediate, never a wrap.
                // An SDIDI intermediate (a STANDARD-DECIMAL expression; a native integer power — kb/Work PB69) lands
                // through the ONE landing at the receiver-less working scale (kb/Work PB84 — `(long)(CobolDec)` was
                // a Roslyn error on `CALL … BY VALUE A ** 2`).
                NumX x = num.Landed(NumericRenderer.DeU(num.Render(expr.Expr, ReceiverContext.None)), ReceiverContext.None);
                // ⛔ THE CELL IS Int128, NOT long, AND THE CONVERSION IS WIDENING (kb/Work PB264). This used to
                // be `ManagedPointer<long>.Cell((long)(x.Expr))` — an UNCHECKED narrowing of a value that the
                // DeU/Landed funnel above delivers on the Int128 lane, so an argument beyond 18 digits crossed
                // as its MODULAR LOW-ORDER BITS: a silent wrong value, in the one direction the callee cannot
                // detect. Widening to the lane's own carrier removes the narrowing rather than checking it —
                // there is no value on the Int128 lane that an Int128 cell cannot hold — and every carrier the
                // ABI accepts is read back through CobolArgAdapt's ReadNumericCell (kb/Work R12).
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<Int128>.Cell((Int128)({x.Expr})), 38, {x.Scale})";
            }
            case BoundAllLiteral all:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell({CsLiteral(all.Literal)}), 0, 0)";
            case BoundFigurative fig:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell(new string({FigurativeConstants.Fill(fig.Kind, ctx.Data.Collating)}, 1)), 0, 0)";
            default:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", "CALL USING argument form") + "), 0, 0)";
        }
    }

    /// <summary>The COMPILE-TIME numeric-literal text of an argument expression — the literal itself, with any
    /// leading sign folded in — or null when the expression is not a literal constant.
    /// <para>⛔ WHY THE NEGATE ARM EXISTS, and it is the two-arm shape again. At a LITERAL position the sign is
    /// part of the token (<c>numericLiteral : signedNumericLiteral</c>), but inside an ARITHMETIC EXPRESSION —
    /// which is what a BY VALUE argument binds as — a leading '−' before the FLOATING-POINT form is taken by
    /// <c>unaryExpression</c> first, so <c>BY VALUE -1.234E-5</c> arrives as BoundNegate(BoundNumLiteral) while
    /// <c>BY VALUE -0.00001234</c> arrives as a bare BoundNumLiteral. Matching only the bare shape fixed the
    /// unsigned spelling and left the signed one falling through to the runtime-expression arm, where the
    /// receiver-less working scale (6 fraction digits) truncated it: −0.00001234 crossed as −0.000012, a silent
    /// wrong value on conforming source. One rule, two spellings, and only one of them fixed is exactly the
    /// defect being repaired here — so the fold is part of the fix, not a refinement of it.</para></summary>
    private static string? ConstLiteralText(BoundExpr e) => e switch
    {
        BoundNumLiteral n => n.Text,
        BoundNegate g => ConstLiteralText(g.Operand) is { } t ? Negated(t) : null,
        _ => null,
    };

    /// <summary>The literal text of the algebraic negation of <paramref name="text"/> (ISO §8.3.3.3.2 rule 2 —
    /// a sign, if used, is the leftmost character; §8.3.3.3.3 rule 2 makes a signed significand sign the whole
    /// floating-point literal).</summary>
    private static string Negated(string text)
    {
        string t = text.Trim().TrimStart('+');
        return t.StartsWith('-') ? t[1..] : "-" + t;
    }

    /// <summary>⛔ THE ONE numeric-LITERAL argument carrier build, for every notation and every pass mode
    /// (kb/Work PB263 + PB264). A numeric literal argument crosses as the <c>(unscaled value, scale)</c> pair
    /// that <see cref="EmitText.TryUnscaledParts"/> derives EXACTLY from the literal — its §8.3.3.3.2 rule-4
    /// value for the fixed-point form, its §8.3.3.3.3 rule-5 value ("the algebraic product of the value of its
    /// significand and the quantity derived by raising ten to the power of the exponent") for the
    /// floating-point form — so the two notations of one value cross identically, and the callee's own
    /// conformance (<c>CobolArgAdapt.Num</c> for §14.2.3 GR9, <c>NumValue</c> for GR10's "COMPUTE statement
    /// without the ROUNDED phrase") receives the value the program actually wrote.
    /// <para>THE CARRIER AND THE DIGIT META COME FROM THE RENDERED VALUE, never from the source text. The digit
    /// count used to be <c>Text.Count(char.IsAsciiDigit)</c>, which counts a floating-point literal's EXPONENT
    /// digits as significand digits, and the cell type was re-derived from that miscount — so
    /// <c>BY CONTENT 1.5E+3</c> asked for a <c>long</c> cell and got a <c>double</c> expression, a raw Roslyn
    /// CS1503 on conforming source with no COBOL diagnostic at all (PB263). <c>IntLiteralCore</c> now decides
    /// the rendering and its carrier together, so they cannot disagree.</para></summary>
    private static string NumericArgText(CobolPassMode mode, string literalText)
    {
        // ONE decomposition, then ONE carrier decision over it. ⛔ Deliberately NOT `UnscaledLit` here: that
        // would decompose the literal a SECOND time and take only half of the result, leaving the rendered
        // expression and the cell type derived independently again — which is the precise shape of the defect
        // this method exists to remove.
        if (!TryUnscaledParts(literalText, out string unscaled, out int scale))
            // Not a canonical numeric literal — the binder has already diagnosed it (COBOLNET1661 for an
            // out-of-range exponent, the §8.3.3.3.3 SR2/SR3 form checks otherwise). Stage loud rather than
            // emit a cell whose type cannot be derived; never a silent value.
            return $"new CobolArg({RuntimeApi.PassModeText(mode)}, ManagedPointer<string>.Cell("
                + LoudValue("string", $"CALL USING numeric literal '{literalText}'") + "), 0, 0)";
        var (cell, _, carrier) = IntLiteralCore(unscaled);
        int digits = unscaled.Count(char.IsAsciiDigit);
        return $"new CobolArg({RuntimeApi.PassModeText(mode)}, "
            + $"ManagedPointer<{carrier}>.Cell({cell}), {digits}, {scale})";
    }

    /// <summary>An accessor carrier over a caller place — the BY REFERENCE / RETURNING aliasing form (design D1:
    /// <c>OverField</c> over the native field; a whole group crosses as its character image, distributed back
    /// through <c>FromImage</c> — the deep-dive group round-trip).</summary>
    public string RefCarrier(Place p) =>
        // §14.8.2.2's variable-length sentence, realized (kb/Work PB204): the carrier is the group's
        // current-extent components, aliased through the SAME OverField shape every other form uses.
        CallPlaceIsVarGroup(p)
            ? RuntimeApi.VarGroupOverField(
                PlaceRenderer.VarGroupImage(p, "CALL argument"),
                PlaceRenderer.WriteVarGroupImage(p, "__v", "CALL boundary copy into"))
        : CallPlaceIsString(p)
        ? $"ManagedPointer<string>.OverField(() => {CallStringRead(p)}, __v => {{ {CallStringWrite(p, "__v")} }})"
        : $"ManagedPointer<{CallNumCarrier(p)}>.OverField(() => {PlaceRenderer.Read(p)}, __v => {{ {PlaceRenderer.Write(p, "__v")} }})";

    /// <summary>True when a place's storage crosses the CALL boundary as a character image (string carrier):
    /// groups, Tier-B windows, zoned-image leaves, alphanumeric / numeric-edited items. EVERY native
    /// fixed-point leaf crosses as its own CARRIER (<c>long</c> / <c>ulong</c> / <c>Int128</c> /
    /// <c>UInt128</c> — kb/Work R12): the former <c>Digits &gt; 18</c> leg routed the wide tiers onto a string
    /// crossing whose write half was NEVER implemented (the generated C# assigned a string to the native field
    /// and did not compile) and whose read half was the picture-digit image (lossy for a BinaryCapacity item's
    /// beyond-picture container values), while the CALLEE side built a <c>ManagedPointer&lt;long&gt;</c> cell
    /// its own carrier-typed reads could not use. One predicate, both sides, native and value-exact.</summary>
    internal static bool CallPlaceIsString(Place p) =>
        p is RedefViewPlace || p.Item.IsGroup || p.Item.StoreAsImage
        || p.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean   // string-stored (D-N1/D-B1): both ABI sides are C# strings, char-correct
        || p.Item.Pic is { IsFloat: true };

    /// <summary>True when a place crosses the activation boundary as the §8.5.1.12 VARIABLE-LENGTH carrier
    /// (kb/Work PB204) — the THIRD crossing form beside the native cell and the flat character image. A
    /// variable-length group has no fixed record window, so <see cref="CallStringRead"/>'s flat image is not
    /// invertible for it; <c>CobolVarGroup</c> carries the fixed run and the ordered variable-length components
    /// instead. Deliberately NARROWER than "is a variable-length group": it also demands
    /// <see cref="DataItem.CurrentExtentImageCapable"/>, so a shape outside that gate (an OCCURS DEPENDING
    /// member, an in-element runtime length) still takes the ordinary arms and still stages the documented
    /// Tier-C loud — a residue keeps its loud rather than acquiring a half-built crossing.
    /// <para>⛔ <see cref="CallPlaceIsString"/> deliberately still answers TRUE for such a place. Its consumers
    /// that this mechanism did NOT convert (ReportWriterEmitter's CONTROL restore) therefore keep routing
    /// through <see cref="CallStringRead"/>, whose group arm stages the Tier-C loud — the SAFE fallback. Making
    /// it answer false would have sent those sites down the NATIVE arm instead, which is the wrong answer
    /// rather than a loud one.</para></summary>
    internal static bool CallPlaceIsVarGroup(Place p) =>
        // CurrentExtentImageCapable ALREADY implies both `IsGroup` and `!IsImageCapable` — a variable-length
        // group has a dynamic child whose own IsImageCapable is false, so the group's is too. The conjuncts
        // are left out rather than restated: a redundant conjunct is a claim that can rot.
        p is not RedefViewPlace and not RefModPlace && p.Item.CurrentExtentImageCapable;

    /// <summary>The C# carrier type of a native fixed-point leaf at the CALL boundary — the item's OWN
    /// <c>ElementType</c> (kb/Work R12: the cell type IS the field type, so the aliasing lambdas and the
    /// callee's carrier-typed reads compile and carry the full container range by construction).</summary>
    internal static string CallNumCarrier(Place p) => p.Item.ElementType;

    /// <summary>The string image a place contributes ACROSS THE CALL BOUNDARY. An occurs-depending group reads
    /// its FULL maximum-allocation image here, never the ODO window: BY REFERENCE "operates as if the [formal]
    /// occupies the same storage area as the argument" (ISO §14.2.3 GR8 — the STORAGE is the maximum allocation)
    /// and a BY CONTENT copy is of the whole record (GR9); the current-extent window of §13.18.38 GR8 is a
    /// SENDING-OPERAND rule for MOVE/compare/INSPECT, not a storage-aliasing rule (IC207A: CALL … USING TABLE-01
    /// with DN3=3 must still carry all 15 character positions in, and carry the callee's full table back out).
    /// Every call site of this helper (the BY REFERENCE carrier, BY CONTENT snapshot, callee copy-out, and
    /// RETURNING delivery) is such a boundary.
    /// <para>⛔ A GROUP CROSSES AS ITS STORAGE IMAGE, NOT AS ITS OPERAND VALUE (kb/Work PB173 — measured, and
    /// PRE-EXISTING at 876d8ab0: `01 G GROUP-USAGE BIT. 05 B1 PIC 1(4). 05 B2 PIC 1(4).` holding 11001010 and
    /// passed BY REFERENCE arrived in the callee as 00110001 and came home as 00110000). §14.2.3 GR8 makes the
    /// formal occupy "the same storage area as the argument", so the carrier is the group's character IMAGE —
    /// the exact inverse of the write half's <c>FromImage</c>. Routing through <c>OperandText.FieldImage</c>
    /// instead delivered a BIT group's OPERAND value (§13.18.29.4 GR1b's m boolean positions, <c>AsBits</c>)
    /// into a <c>FromImage</c> that reads ceil(m/8) PACKED characters — two alphabets, one carrier, silent
    /// argument corruption on legal source that §14.9.4.3 SR6 explicitly admits ("If the BY REFERENCE phrase is
    /// specified or implied for an identifier-2 that is a bit data item, identifier-2 shall be described such
    /// that it is aligned on a byte boundary …", which a level-01 bit group satisfies by construction).
    /// <c>PlaceRenderer.GroupImage</c> is THE ONE reader and already owns all four arms — the Tier-B window, the
    /// <c>OdoGroupPlace</c> unwrap to the FULL allocation, the capability guard and the struct image — so this
    /// is now the exact mirror of <see cref="CallStringWrite"/>'s group arm, arm for arm.</para></summary>
    internal static string CallStringRead(Place p) =>
        // ⛔ A REFERENCE-MODIFIED OPERAND IS AN ELEMENTARY ALPHANUMERIC ITEM OVER THE SLICE (§8.4.3.3.4 GR6),
        // whatever the inner item is — it must NOT take the group arm below. Its own substrate wrap
        // (GroupImagePlace / BitImagePlace / NumericImagePlace) is already inside the RefModPlace, so
        // `PlaceRenderer.Read` gives the slice and `PlaceRenderer.Write` splices it back: an exact pair. This
        // arm is stated FIRST and explicitly on BOTH halves because the alternative is invisible — widening the
        // read half's group test to `p.Item.IsGroup` (RefModPlace.Item forwards to the INNER item, so a
        // ref-modded group answers true) rendered `CobolStr.RefMod(G.AsImage(),1,3).AsImage()`, a backend
        // CS1061 on `string`, and the write half had been emitting the `.FromImage(` half of exactly that pair
        // since before this change (measured at 876d8ab0: `CALL "S" USING G(1:3)` = one CS1061; with the group
        // test widened, two).
        p is RefModPlace ? OperandText.FieldImage(p)
        : p.Item.IsGroup
            ? PlaceRenderer.GroupImage(p)   // the FULL image (GR8 is a sending-operand rule, not a boundary one) — window or struct (kb/Work PB80)
            : OperandText.FieldImage(p);

    internal static string CallStringWrite(Place p, string value) =>
        // The boundary WRITE half of the §14.2.3 GR8/GR9 full-allocation rule above: a group (including an
        // occurs-depending group — OdoGroupPlace.Write delegates to the full-width struct) distributes the whole
        // image through FromImage, never the GR8a current-extent splice.
        // ⛔ NO `&& p.Item.IsImageCapable` HERE — THE ONE WRITER OWNS THE GUARD (kb/Work PB177 arm B, the EIGHTH
        // two-arm-dispatch instance in this repo). This arm used to carry the capability test itself and an
        // imageless group therefore FELL THROUGH to the raw `PlaceRenderer.Write(p, value)` at the bottom, which
        // for a group MemberPlace renders `_G = <string>;` — a backend CS0029 (measured: a sub-program whose
        // `PROCEDURE DIVISION USING G` names a group with a USAGE POINTER leaf, compiled ALONE, so the
        // caller-side ArgText screen above gives it no cover). Its READ twin `CallStringRead` correctly staged
        // the Tier-C loud through `OperandText.FieldImage`, and the comment right here claimed the two were
        // "kept in lockstep deliberately" while they were not. Routing EVERY non-RedefViewPlace group to
        // `WriteFullGroupImage` makes the lockstep a STRUCTURAL fact — `WriteGroupImage`'s own arm order stages
        // the same loud — instead of something a drift test has to assert. All FIVE live callers inherit it with
        // no edit: ProgramEmitter's callee formal copy-in (ProgramEmitter.cs), the CALL BY REFERENCE cell
        // (CallEmitter.cs), the two INVOKE argument write-backs (OoEmitter.cs — the BY REFERENCE copy-out and
        // the RETURNING delivery), and ReportWriterEmitter's `CONTROL IS <group>` restore. (⚠ this sentence used
        // to say "three", which under-counted the two OO sites — the enumeration is now the grep.)
        // The RECEIVING twin of CallStringRead's first arm: a ref-modded operand splices its slice back
        // (§8.4.3.3.4 GR6 — an elementary alphanumeric item over the slice), and takes NEITHER the group image
        // store NOR the numeric decode/re-encode below, whose predicates both read through to the INNER item.
        p is RefModPlace ? PlaceRenderer.Write(p, value)
        : p.Item.IsGroup && p is not RedefViewPlace
            ? PlaceRenderer.WriteFullGroupImage(p, value, "CALL boundary copy")   // the FULL image — an ODO wrapper is unwrapped (kb/Work PB80)
        // kb/Work PB181 (measured — 1234 crossed BY REFERENCE, ADD 1, came home as 2594): the elementary
        // boundary convention carries the DISPLAY image, and a byte-form windowed / image-stored NUMERIC
        // receiver must DECODE it and re-encode through the ONE byte-form recipe (the same MOVE/ACCEPT
        // store shape) — the raw splice put the returned CHARACTERS into a StorageWidth window.
        : (p.Item.StoreAsImage || p is RedefViewPlace)
            && p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false }
            ? PlaceRenderer.Write(p, RuntimeApi.NumFormatImage(
                ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(value, p.Item.ProfileName), p.Item),
                p.Item.ProfileName))
            : PlaceRenderer.Write(p, value);

    /// <summary>Emit CANCEL (ISO §14.9.5): one registry call per target, left to right (GR2). Under enabled
    /// EC-PROGRAM checking (>>TURN, §7.3.25) each target's <see cref="CobolCallException"/> runs the
    /// §14.6.13.1.3 sequence (status, F3 selection, fatal default) instead of crashing raw.</summary>
    public void EmitCancel(BoundCancel c)
    {
        var w = ctx.Writer;
        var ecProg = EnabledProgramNames();
        foreach (var (literal, dynamic) in c.Targets)
        {
            string nameExpr = literal is { } l ? CsLiteral(l) : $"({OperandText.AsString(dynamic!, num)}).Trim()";
            string call = $"ProgramRegistry.Cancel({nameExpr}, {CsLiteral(callState.SelfPath)});";
            if (ecProg.Count == 0)
            {
                w.Line(call);
                continue;
            }
            using (w.Block("try"))
                w.Line(call);
            // §14.9.5.2 gives CANCEL no conditional phrase at all, so the arm is always the §14.6.13.1.3
            // sequence; and every name it can raise (EC-PROGRAM-CANCEL-ACTIVE) is raised OUTSIDE any
            // activation, so the shared arm's !ControlTransferred filter is vacuously true here.
            EmitCallEcCatch(ecProg, byPhrase: false, phraseFlag: null);
        }
    }

    /// <summary>Emit GOBACK (ISO §14.9.18): move the RETURNING source into the header RETURNING item (GR2 — the
    /// activation result), stage a RAISING exception condition for the activator (the EC model — picked up at
    /// the activating CALL site or by the registry's boundary default), then raise <see cref="ProgramReturn"/> —
    /// caught at THIS program's activation entry, returning control to the activator (called program) or ending
    /// the run unit (main program, GR3).</summary>
    public bool EmitGoback(BoundGoback g)
    {
        var w = ctx.Writer;
        if (g.ReturningSource is { } src)
        {
            if (callState.ReturningPlace is { } ret)
                move.Emit(new BoundMove(new BoundFieldOperand(src), [ret]));
            else
                w.Line(LoudStmt("GOBACK RETURNING without a PROCEDURE DIVISION RETURNING item (ISO §14.9.18 SR)"));
        }
        if (g.Raising is { } r)
            // §14.9.18.4 GR3 (the P13 review C3 fix): in a program NOT under the control of a calling runtime
            // element, GOBACK operates as STOP and "a RAISING phrase, if specified, is ignored" — so the staging
            // (including the checking-off fatal termination arm) is __asCalled-gated, exactly like EmitExitProgram.
            using (w.Block("if (__asCalled)   // §14.9.18.4 GR1b/GR3 — a main-program GOBACK ignores RAISING"))
                EmitRaisingStage(r, "GOBACK");
        // GOBACK … WITH {NORMAL|ERROR} STATUS [value] (§14.9.18.4 GR10): the status reaches the OS ONLY in a main
        // program (GR3 — a called-program GOBACK returns to the activator, GR2, so its status phrase is inert);
        // guard on __asCalled, the same activation flag EmitExitProgram uses.
        if (g.Status is { } st)
            w.Line($"if (!__asCalled) {RuntimeApi.SetExitStatus(num.ExitStatus(st))};   // §14.9.18.4 GR3/GR10 — a main program passes the status");
        w.Line("throw new ProgramReturn();   // return to the activator; in a main program ≡ STOP (ISO §14.9.18 GR2/GR3)");
        return true;
    }

    /// <summary>Emit EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2): GR2 — in a program NOT under the control
    /// of a calling runtime element the statement is CONTINUE and "no exception condition is raised even if the
    /// RAISING phrase is specified", so BOTH the staging and the return are <c>__asCalled</c>-gated; GR3 — in a
    /// called program it returns per the GOBACK rules, staging the RAISING condition for the activator.</summary>
    public void EmitExitProgram(BoundExitProgram ep)
    {
        var w = ctx.Writer;
        if (ep.Raising is null)
        {
            w.Line("if (__asCalled) throw new ProgramReturn();   // ISO §14.9.14 GR2: CONTINUE in a non-called program; GR3: return in a called one");
            return;
        }
        using (w.Block("if (__asCalled)   // GR2 — a non-called program raises nothing, even with RAISING"))
        {
            EmitRaisingStage(ep.Raising, "EXIT PROGRAM");
            w.Line("throw new ProgramReturn();   // return to the activator (ISO §14.9.14 GR3)");
        }
    }

    /// <summary>Stage a <c>RAISING</c> phrase's exception condition for re-raise in the ACTIVATOR
    /// (ISO §14.9.18 GR / §14.6.13.1.3 #6 — consumed by the activating CALL site's pickup, or by
    /// <c>ProgramRegistry</c>'s boundary default when the caller is EC-free). The TURN decision was baked in at
    /// bind time (§14.6.13.1.1: a condition is raised only when checking for it is enabled): disabled + nonfatal
    /// stages nothing (§14.6.13.1.4 first sentence); disabled + fatal is the §14.6.13.1.3 #8 implementor
    /// choice — this implementation terminates loudly (mirrors <see cref="EcEmitRaise"/>).</summary>
    public void EmitRaisingStage(BoundRaising r, string verb)
    {
        var w = ctx.Writer;
        if (r.ObjectSource is { } os)
        {
            // The exception-OBJECT leg (§14.9.18.4 GR1b; the EC-OO wave): no Enabled/Fatal logic — objects
            // are not TURN-gated (§7.3.25 takes names only); the activator's §14.6.13.1.5 rules decide.
            w.Line($"ExceptionState.SetPropagatingObject({PlaceRenderer.Read(os)});   // {verb} RAISING identifier-1 — staged for the activator");
            return;
        }
        if (r.IsLast)
        {
            w.Line("ExceptionState.SetPropagatingLast();   // RAISING LAST EXCEPTION (§14.9.18.2 — nothing staged when the status is clear)");
            return;
        }
        if (!r.Enabled)
        {
            if (!r.Fatal)
            {
                w.Line($"// {verb} RAISING {r.EcName}: checking not enabled — nonfatal, not raised (ISO §14.6.13.1.4)");
                return;
            }
            w.Line($"throw new CobolFatalException({CsLiteral(r.EcName!)}, \"raised by {verb} RAISING with checking "
                + "not enabled (ISO 14.6.13.1.3 #8 - implementor-defined; this implementation terminates)\");");
            return;
        }
        // kb/Work R07: the returning element's own last-exception status carries the §15.32.3 r2 operands when
        // THIS name's TURN said WITH LOCATION (SetPropagating Sets before staging); null-null keeps the two-arg
        // call byte-identical for the without-LOCATION case.
        w.Line(r.WithLocation
            ? $"ExceptionState.SetPropagating({CsLiteral(r.EcName!)}, {(r.Fatal ? "true" : "false")}, "
              + $"{CsLiteral(r.StatementName!)}, {CsLiteral(r.Location!)});   // staged for the activator (§14.9.18 GR)"
            : $"ExceptionState.SetPropagating({CsLiteral(r.EcName!)}, {(r.Fatal ? "true" : "false")});   // staged for the activator (§14.9.18 GR)");
    }
}
