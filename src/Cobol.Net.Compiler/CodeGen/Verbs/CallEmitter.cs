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
            invocation = $"ProgramRegistry.CallProgram({nameExpr}, {CsLiteral(callState.SelfPath)}, {args}, {ret}"
                + $"{(ecState.Active ? ", siteHandlesPropagation: true" : "")}"
                + $"{(c.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "")});";   // §8.4.3.2.4 GR6b — a UDF locate miss is EC-FUNCTION-NOT-FOUND
        }

        var ecProg = EnabledProgramNames();
        // The ACTIVATING half of §14.8.4.1's both-elements rule: this CALL statement's enabled EC-EXTERNAL-*
        // set becomes the pending site mask the activation boundary latches for the activated element's
        // Describe gate (§14.9.4.4 GR3e — "enabled ... in both the activated program and activating runtime
        // element"). Zero-scaffolding: an EC-free site emits nothing (the boundary re-zeroes after every call).
        int siteExternalMask = ecProg.Sum(ExternalBit);
        if (siteExternalMask != 0)
            w.Line($"ExceptionState.ExternalCheckMask = {siteExternalMask};   // §14.8.4.1 — this CALL's EC-EXTERNAL enablement (the activating element)");
        bool hasPhrase = c.OnException is not null || c.NotOnException is not null;
        if (!hasPhrase && ecProg.Count == 0)
        {
            w.Line(invocation);
            EmitPropagationPickup();
            return false;
        }
        int id = ctx.Names.NextCall();
        if (hasPhrase) w.Line($"bool __callErr{id} = false;");
        using (w.Block("try"))
            w.Line(invocation);
        if (ecProg.Count > 0)
            EmitProgramEcCatch(ecProg, hasPhrase, hasPhrase ? $"__callErr{id}" : null);
        if (hasPhrase)
            w.Line($"catch (CobolCallException) {{ __callErr{id} = true; }}   // CALL exception condition → the ON phrase (ISO §14.9.4.4 GR3h)");
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

    /// <summary>The enabled EC-PROGRAM-* / EC-EXTERNAL-* names of the current statement (empty when none / no
    /// wrapper) — the two families a CALL raises through <see cref="CobolCallException"/> (ISO §14.9.4.4
    /// GR3b–f: locate/recursion/argument failures; GR3e: the §14.8.4 external-conformance trio). Both take the
    /// same catch arm: all are Table 13 Fatal and GR3h #1 gives the ON EXCEPTION phrase both families.</summary>
    private List<string> EnabledProgramNames() =>
        ecState.Info?.Enabled.Where(p => p.Ec.StartsWith("EC-PROGRAM-", StringComparison.Ordinal)
            || p.Ec.StartsWith("EC-EXTERNAL-", StringComparison.Ordinal)).Select(p => p.Ec).ToList()
        ?? [];

    /// <summary>The <see cref="ExternalChecks"/> bit of one EC-EXTERNAL level-3 name (0 for any other name) —
    /// the emitted CALL-site mask is the OR over the statement's enabled set.</summary>
    private static int ExternalBit(string ec) => ec switch
    {
        "EC-EXTERNAL-FORMAT-CONFLICT" => (int)ExternalChecks.FormatConflict,
        "EC-EXTERNAL-DATA-MISMATCH" => (int)ExternalChecks.DataMismatch,
        "EC-EXTERNAL-FILE-MISMATCH" => (int)ExternalChecks.FileMismatch,
        _ => 0,
    };

    /// <summary>Emit the name-filtered <c>catch (CobolCallException)</c> arm of a CALL/CANCEL under enabled
    /// EC-PROGRAM checking (§9.1.13-style bridge for the inter-program family: the runtime latched the Table 13
    /// level-3 name in <see cref="CobolCallException.EcName"/>): set the last exception status (§14.6.13.1.1),
    /// then either flag the statement's own ON EXCEPTION phrase (it wins — §14.6.13.1.3 #1 / §14.9.4.4 GR3h) or
    /// run the §14.9.49 F3 selection with the fatal default (every EC-PROGRAM-* is fatal, Table 13). A
    /// CobolCallException whose name is NOT enabled falls through to the next catch arm / propagates — the
    /// checking-off behavior unchanged.</summary>
    private void EmitProgramEcCatch(List<string> ecProg, bool hasPhrase, string? phraseFlag)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        string nameTest = string.Join(" || ", ecProg.Select(n => $"__ce{id}.EcName == {CsLiteral(n)}"));
        using (w.Block($"catch (CobolCallException __ce{id}) when ({nameTest})"))
        {
            // The §15.32.3 r2 pair rides the CALL statement's ambient context (kb/Work R14) — the callee's own
            // contexts were restored on unwind, so this Set attributes the CALL, not the callee's last statement.
            w.Line($"ExceptionState.Set(__ce{id}.EcName, true);   // §14.6.13.1.1 — all EC-PROGRAM-* are fatal (Table 13)");
            if (hasPhrase)
                w.Line($"{phraseFlag} = true;   // the statement's ON EXCEPTION phrase handles it (§14.6.13.1.3 #1; §14.9.4.4 GR3h)");
            else
            {
                w.Line($"int __r{id} = {ec.EcDispatchExpr($"__ce{id}.EcName", "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                w.Line($"if (__r{id} != -2) throw new CobolFatalException(__ce{id}.EcName, __ce{id}.Message);   // §14.6.13.1.3 #5/#7");
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
                    + "\"an exception object was not handled (ISO 14.6.13.1.5; Table 13 - fatal)\");");
            }
            w.Line("// -1/-2: declarative completed / RESUME NEXT — normal continuation (:24604)");
        }
        using (w.Block($"if (ExceptionState.TakePropagated(out var __pn{id}, out var __pf{id}))   // §14.9.18 GR — raised at the end of the CALL"))
        {
            w.Line($"int __pr{id} = {ec.EcDispatchExpr($"__pn{id}", "\"\"")};");
            w.Line($"if (__pr{id} >= 0) {{ __pc = __pr{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            w.Line($"if (__pr{id} != -2 && __pf{id}) throw new CobolFatalException(__pn{id}, "
                + "\"exception condition propagated by GOBACK/EXIT PROGRAM RAISING and not resumed "
                + "(ISO 14.9.18; 14.6.13.1.3 #6/#7)\");");
        }
    }

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument (caller side; design D1/D2).
    /// BY REFERENCE builds an accessor carrier over the caller's storage (§14.2.3 GR8); BY CONTENT/BY VALUE
    /// snapshot the value into a cell AT CALL INITIATION — which also realizes the §14.9.4.4 GR3a once-only
    /// evaluation for those modes. (A BY REFERENCE accessor over a SUBSCRIPTED operand re-evaluates the
    /// subscript inside the closure — the GR3a capture-into-locals refinement is a known follow-up.)</summary>
    public string ArgText(BoundCallArg a)
    {
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
            // exists. A float / COMP-5 / INDEX leaf is still genuinely imageless and stays loud — hence the
            // leaf-kind wording now matches the predicate actually being tested.
            if (p.Item.IsGroup && !p.Item.IsImageCapable && p is not RedefViewPlace)
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", TierCIsland.Reason(p.Item, "CALL USING group"))
                    + "), 0, 0)";
            if (a.Mode == CobolPassMode.Reference)
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, {RefCarrier(p)}, {digits}, {scale})";
            // BY CONTENT — "a record … allocated by the activating element" (§14.2.3 GR9) — and BY VALUE with
            // an identifier argument (a UDF BY VALUE formal, §8.4.3.2.4 GR5c): both are value snapshots at
            // call initiation; the mode rides the wire so the arg is honest about which rule produced it
            // (the BY VALUE callee re-conforms through its own NumValue cell, GR10).
            return CallPlaceIsString(p)
                ? $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CallStringRead(p)}), {digits}, {scale})"
                : $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<long>.Cell({PlaceRenderer.Read(p)}), {digits}, {scale})";
        }
        switch (a.Value)
        {
            case BoundStringLiteral s:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CsLiteral(s.Value)}), 0, 0)";
            case BoundNumericLiteral n:
            {
                var lit = UnscaledLit(n.Text);
                int digits = n.Text.Count(char.IsAsciiDigit);
                if (digits > 18)
                    return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell("
                        + LoudValue("string", $"CALL USING wide numeric literal '{n.Text}' (19+ digits — the Int128 carrier tier)") + "), 0, 0)";
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<long>.Cell({lit.Expr}), {digits}, {lit.Scale})";
            }
            case BoundComputedOperand expr:
            {
                // An expression argument snapshots its computed value (§14.2.3 GR9/GR10 — the CALL BY VALUE
                // grammar leg binds Mode=Value; a UDF expression argument to a BY REFERENCE formal binds
                // Mode=Content per §8.4.3.2.4 GR5b — the mode is bound, not assumed here).
                // An unsigned-wide result (a HIGHEST-ALGEBRAIC fold literal — kb/Work R10) funnels through the
                // same DeU rule as every arithmetic consumer: loud beyond the Int128 intermediate, never a wrap.
                NumX x = NumericRenderer.DeU(num.Render(expr.Expr, ReceiverContext.None));
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<long>.Cell((long)({x.Expr})), 18, {x.Scale})";
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

    /// <summary>An accessor carrier over a caller place — the BY REFERENCE / RETURNING aliasing form (design D1:
    /// <c>OverField</c> over the native field; a whole group crosses as its character image, distributed back
    /// through <c>FromImage</c> — the deep-dive group round-trip).</summary>
    public string RefCarrier(Place p) => CallPlaceIsString(p)
        ? $"ManagedPointer<string>.OverField(() => {CallStringRead(p)}, __v => {{ {CallStringWrite(p, "__v")} }})"
        : $"ManagedPointer<long>.OverField(() => {PlaceRenderer.Read(p)}, __v => {{ {PlaceRenderer.Write(p, "__v")} }})";

    /// <summary>True when a place's storage crosses the CALL boundary as a character image (string carrier):
    /// groups, Tier-B windows, zoned-image leaves, alphanumeric / numeric-edited items. A native fixed-point
    /// leaf crosses as its <c>long</c> (fully typed — the common conforming case).</summary>
    internal static bool CallPlaceIsString(Place p) =>
        p is RedefViewPlace || p.Item.IsGroup || p.Item.StoreAsImage
        || p.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean   // string-stored (D-N1/D-B1): both ABI sides are C# strings, char-correct
        || p.Item.Pic is { IsFloat: true } || p.Item.Pic is { Digits: > 18 }
        // The ulong carrier tier (kb/Work R10): an unsigned 8-byte BinaryCapacity leaf cannot ride the
        // ManagedPointer<long> cell (C# has no ulong↔long assignment), so it crosses as its digit image exactly
        // like the wide (>18-digit) tier. ⚠ Both tiers share the pre-existing picture-digit-image ABI, which
        // truncates a value beyond the PICTURE's digit count — registered as its own kb/Work item (R12).
        || p.Item.Pic is { IsUnsignedLongBinary: true };

    /// <summary>The string image a place contributes ACROSS THE CALL BOUNDARY. An occurs-depending group reads
    /// its FULL maximum-allocation image here, never the ODO window: BY REFERENCE "operates as if the [formal]
    /// occupies the same storage area as the argument" (ISO §14.2.3 GR8 — the STORAGE is the maximum allocation)
    /// and a BY CONTENT copy is of the whole record (GR9); the current-extent window of §13.18.38 GR8 is a
    /// SENDING-OPERAND rule for MOVE/compare/INSPECT, not a storage-aliasing rule (IC207A: CALL … USING TABLE-01
    /// with DN3=3 must still carry all 15 character positions in, and carry the callee's full table back out).
    /// Every call site of this helper (the BY REFERENCE carrier, BY CONTENT snapshot, callee copy-out, and
    /// RETURNING delivery) is such a boundary.</summary>
    internal static string CallStringRead(Place p) => p is OdoGroupPlace odo
        ? $"{PlaceRenderer.Read(odo)}.AsImage()"
        : OperandText.FieldImage(p);

    internal static string CallStringWrite(Place p, string value) =>
        // The boundary WRITE half of the §14.2.3 GR8/GR9 full-allocation rule above: a group (including an
        // occurs-depending group — OdoGroupPlace.Write delegates to the full-width struct) distributes the whole
        // image through FromImage, never the GR8a current-extent splice.
        // IsImageCapable, matching the ArgText guard above — the two are the READ and WRITE halves of ONE
        // round-trip, so a predicate that differed between them would let a group cross IN through FromImage and
        // back OUT through a raw write (or refuse the write for a group the guard had just admitted). Kept in
        // lockstep deliberately; `V59ImagePredicateDriftTests` fails if they diverge again.
        p.Item.IsGroup && p is not RedefViewPlace && p.Item.IsImageCapable
            ? $"{PlaceRenderer.Read(p)}.FromImage({value});"
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
            EmitProgramEcCatch(ecProg, hasPhrase: false, phraseFlag: null);
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
