// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The EC exception-condition slice of the Roslyn backend (ISO/IEC 1989:2023 §14.6.13;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12 as-built): renders the per-statement guards a
/// <see cref="BoundEcChecked"/> wrapper carries, the RAISE/RESUME statements, and the GENERATED dispatch
/// machinery — <c>__EcDispatch</c> (the §14.9.49.4 GR3c–g Format-3 selector over the program's USE AFTER
/// EXCEPTION CONDITION declaratives) and <c>__IoCheckEc</c> (the §9.1.13.1 status→EC bridge). EVERY artifact here
/// is gated: a compilation group with no enabling TURN, no F3, no RAISE/RESUME/RAISING and no EXCEPTION-*
/// function emits byte-identical source to a pre-EC build (the zero-scaffolding invariant, SSOT §18.16).
/// <para><b>The dispatch result protocol</b> (shared by <c>__RunUse</c>/<c>__EcDispatch</c>/<c>__IoCheckEc</c>):
/// <c>-1</c> = the declarative completed normally (§14.6.13.1.2) or no action; <c>-2</c> = RESUME AT NEXT
/// STATEMENT (fall through past the raising statement, §14.9.33.4 GR2 — suppresses a fatal termination,
/// §14.6.13.1.3 #5 NOTE 2); <c>-3</c> = no qualifying declarative; <c>≥0</c> = RESUME AT procedure-name's pc
/// (≡ GO TO, GR3).</para>
/// </summary>
internal sealed class EcEmitter(EmitContext ctx, EcState ecState, DispatchState dispatch)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/>: the EC↔statement
    /// cycle (<c>EmitChecked</c> re-enters <c>EmitStatement</c>; statements contain EC-checked children) is
    /// the edge the coupling census proved no ctor order can satisfy.</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The per-statement raise-site dispatch expression. When this unit has an exception-checking
    /// (Format-3) PERFORM (§14.9.28), every raise site routes through <c>__EcPerform</c> — which consults the
    /// ambient F3-frame stack FIRST (GR17: a matching WHEN preempts the USE declaratives) and falls to
    /// <c>__EcDispatch</c> only on no-match. Otherwise the historical funnel: <c>__EcDispatch</c> when the unit has
    /// F3 declaratives, else the no-declarative constant. A non-F3-PERFORM unit emits byte-identical text.</summary>
    public string EcDispatchExpr(string ecNameExpr, string fileExpr) =>
        ecState.UnitHasF3Perform ? $"__EcPerform({ecNameExpr}, {fileExpr})"
        : ecState.UnitHasF3       ? $"__EcDispatch({ecNameExpr}, {fileExpr})"
        :                           "-3";

    /// <summary>The <c>__EcObjDispatch</c> invocation (or the no-declarative constant when this unit has no
    /// Format-4 declaratives) — the §14.9.49.4 GR14 exception-OBJECT selector (the EC-OO wave).</summary>
    public string ObjDispatchExpr(string objExpr) =>
        ecState.UnitHasF4 ? $"__EcObjDispatch({objExpr})" : "-3";

    /// <summary>RAISE identifier-1 (ISO §14.9.29.4 GR2; §14.6.13.1.5): set EXCEPTION-OBJECT, run the F4
    /// declarative if one matches (GR14 — GR3: F4 REPLACES the F1/F3 tiers for object raises), and in EVERY
    /// no-match/complete case continue with the next statement — a RAISE of an object is NEVER fatal by
    /// itself.</summary>
    public bool EmitRaiseObject(BoundRaiseObject ro)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        w.Line($"ExceptionState.SetObject({(ro.Source is { } roSrc ? PlaceRenderer.Read(roSrc) : "this")});   // §14.6.13.1.5 (1)/(2) — EXCEPTION-OBJECT + the status sentinel");
        w.Line($"int __r{id} = {ObjDispatchExpr($"ExceptionState.ExceptionObject")};");
        w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
        w.Line($"// -1/-2/-3: declarative completed / RESUME NEXT / no match — continue after RAISE (§14.9.29.4 GR2)");
        return false;   // the continue-after-RAISE path IS the normal exit (GR2 — never fatal by itself)
    }

    public (string Stmt, string Loc) EcStmtLoc(EcStatementInfo info) =>
        info.WithLocation ? (CsLiteral(info.StatementName), CsLiteral(info.Location)) : ("null", "null");

    // ── The BoundEcChecked wrapper (the statement EC context + the EC-ARGUMENT-FUNCTION ambient gate) ────────

    /// <summary>The NONFATAL ambient per-statement EC gates — each rides a run-unit-scoped
    /// <c>ExceptionState.XxxChecking</c> flag its runtime raise site consults, set/reset around the statement (no
    /// catch, no throw — nonfatal ⇒ the raise only records the last exception status). Fixed order for
    /// byte-stability of the generated wrapper (a statement enabling one emits exactly the pre-generalization
    /// output). The fatal twins (EC-ARGUMENT-FUNCTION) stay in <see cref="EmitArgOrPlain"/> — they need a catch.</summary>
    private static readonly (string Ec, string Flag)[] NonfatalAmbientGates =
    [
        ("EC-DATA-CONVERSION", "DataConversionChecking"),   // §15.19.4 r1/r3 — CONVERT / DISPLAY-OF / NATIONAL-OF
        ("EC-BOUND-OVERFLOW", "BoundOverflowChecking"),     // §8.5.1.9.6 GR1 — OCCURS DYNAMIC implicit growth
    ];

    public bool EmitChecked(BoundEcChecked ec)
    {
        var prev = ecState.Info;
        ecState.Info = ec.Info;
        // The nonfatal ambient gates enabled at this statement ride a set/reset wrapper around whichever inner
        // dispatch (the fatal-gated or the plain) the statement needs.
        var gates = NonfatalAmbientGates.Where(g => ec.Info.Enabled.Any(p => p.Ec == g.Ec)).ToList();
        if (gates.Count > 0)
        {
            var w = ctx.Writer;
            foreach (var g in gates) w.Line($"ExceptionState.{g.Flag} = true;");
            using (w.Block("try"))
                EmitArgOrPlain(ec);
            w.Line("finally { " + string.Join(" ", gates.Select(g => $"ExceptionState.{g.Flag} = false;")) + " }");
            ecState.Info = prev;
            return false;   // conservative: the inner dispatch may itself resume past a transfer
        }
        bool terminated = EmitArgOrPlain(ec);
        ecState.Info = prev;
        return terminated;
    }

    /// <summary>The inner EC dispatch of a checked statement: the EC-ARGUMENT-FUNCTION fatal ambient gate (with
    /// USE F3 dispatch on the raise) or, when that condition is not enabled, a plain statement emission. Wrapped by
    /// <see cref="EcEmitChecked"/> with the nonfatal EC-DATA-CONVERSION gate when needed.</summary>
    /// <summary>The FATAL ambient per-statement EC gates — each rides an <c>ExceptionState.XxxChecking</c> flag its
    /// runtime raise site consults; a raise throws <see cref="Runtime.Exceptions.CobolFatalException"/> which the
    /// statement guard catches for USE F3 dispatch (RESUME) else re-throws to terminate. Fixed order for
    /// byte-stability. (Nonfatal twins live in <see cref="EmitChecked"/>'s set/reset wrapper — they need no catch.)</summary>
    private static readonly (string Ec, string Flag)[] FatalAmbientGates =
    [
        ("EC-ARGUMENT-FUNCTION", "ArgumentFunctionChecking"),   // §15.3 — intrinsic argument/domain error
        ("EC-BOUND-REF-MOD", "BoundRefModChecking"),            // §8.4.3.3.4 — ref-mod out of range / zero-length
        ("EC-DATA-NOT-FINITE", "FloatNotFiniteChecking"),       // §14.6.13.2 item 3 — NaN/±Inf float sending operand
        ("EC-DATA-OVERFLOW", "FloatOverflowChecking"),          // §14.9.25.4 GR4 step 4a — MOVE overflows a float receiver
        ("EC-RANGE-PERFORM-VARYING", "PerformVaryingChecking"), // §14.9.28.4 GR3 — index-name varied from a non-positive item
        ("EC-DATA-PTR-NULL", "DataPtrNullChecking"),            // §13.18.5.4 GR3 / §14.9.39 F10 GR18 — NULL data-address
        ("EC-BOUND-PTR", "BoundPtrChecking"),                   // §13.18.5.4 GR4 — address neither NULL nor valid
        ("EC-SIZE-ADDRESS", "SizeAddressChecking"),             // §14.9.39 F10 GR19 — non-integer SET UP/DOWN BY amount
    ];

    private bool EmitArgOrPlain(BoundEcChecked ec)
    {
        // The fatal ambient gates enabled at this statement: intrinsic calls / ref-mod render inline inside
        // arbitrary expressions, so the guard wraps the STATEMENT and the runtime error sites consult the flag(s).
        var gates = FatalAmbientGates.Where(g => ec.Info.Enabled.Any(p => p.Ec == g.Ec)).ToList();
        if (gates.Count == 0)
            return Statements.EmitStatement(ec.Inner);

        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        var (stmt, loc) = EcStmtLoc(ec.Info);
        // One gate ⇒ the raised name is the literal (byte-identical to the pre-generalization output); two or more
        // ⇒ the actual __af.EcName drives the status/dispatch.
        string ecExpr = gates.Count == 1 ? CsLiteral(gates[0].Ec) : $"__af{id}.EcName";
        string nameTest = string.Join(" || ", gates.Select(g => $"__af{id}.EcName == {CsLiteral(g.Ec)}"));
        foreach (var g in gates) w.Line($"ExceptionState.{g.Flag} = true;");
        using (w.Block("try"))
            Statements.EmitStatement(ec.Inner);
        using (w.Block($"catch (CobolFatalException __af{id}) when ({nameTest})"))
        {
            if (ec.Info.WithLocation)
                w.Line($"ExceptionState.Set({ecExpr}, true, {stmt}, {loc});");
            w.Line($"int __r{id} = {EcDispatchExpr(ecExpr, "\"\"")};");
            w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            w.Line($"if (__r{id} != -2) throw;   // fatal, unresumed → abnormal termination (§14.6.13.1.3 #5/#7)");
        }
        w.Line("finally { " + string.Join(" ", gates.Select(g => $"ExceptionState.{g.Flag} = false;")) + " }");
        return false;   // conservative: the catch can resume past an inner transfer
    }

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    public bool EmitRaise(BoundRaise r)
    {
        var w = ctx.Writer;
        if (!r.Enabled)
        {
            if (!r.Fatal)
            {
                // §14.6.13.1.4 first sentence + §14.6.13.1.1 (24485): checking off ⇒ the condition is not raised
                // and the last exception status is NOT set — the RAISE acts as CONTINUE (§14.9.29.4 GR1 NOTE).
                w.Line($"// RAISE {r.EcName}: checking not enabled — nonfatal, continues as if not raised (ISO §14.6.13.1.4)");
                return false;
            }
            // A FATAL exception-name raised with checking off is the §14.6.13.1.3 #8 implementor choice —
            // this implementation terminates loudly (the §1.4 doctrine; recorded in the deep-dive).
            w.Line($"throw new CobolFatalException({CsLiteral(r.EcName)}, \"raised by RAISE with checking not enabled "
                + "(ISO 14.6.13.1.3 #8 - implementor-defined; this implementation terminates)\");");
            return true;
        }
        int id = ctx.Names.NextEc();
        string stmt = r.WithLocation ? "\"RAISE\"" : "null";
        string loc = r.WithLocation ? CsLiteral(r.Location) : "null";
        w.Line($"ExceptionState.Set({CsLiteral(r.EcName)}, {(r.Fatal ? "true" : "false")}, {stmt}, {loc});   // §14.9.29.4 GR1 — raise + EXCEPTION-OBJECT null");
        w.Line($"int __r{id} = {EcDispatchExpr(CsLiteral(r.EcName), "\"\"")};");
        w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
        if (r.Fatal)
            w.Line($"if (__r{id} != -2) throw new CobolFatalException({CsLiteral(r.EcName)}, "
                + "\"raised by RAISE and not resumed (ISO 14.6.13.1.3 #5/#7)\");");
        // Nonfatal: handled-or-not, execution continues after the RAISE (§14.6.13.1.4 #3/#4).
        return false;
    }

    public void EmitResume(BoundResume r) =>
        ctx.Writer.Line(r.TargetPc == ResumeSignal.NextStatement
            ? "throw new ResumeSignal(ResumeSignal.NextStatement);   // RESUME AT NEXT STATEMENT (§14.9.33.4 GR2)"
            : $"throw new ResumeSignal({r.TargetPc});   // RESUME AT procedure-name ≡ GO TO (§14.9.33.4 GR3)");

    // ── The EC-SIZE family over the checked-arithmetic shape (§14.7.5 ↔ Table 13) ───────────────────────────

    /// <summary>The EC-SIZE-* names the current statement has enabled (empty list when none / no wrapper).</summary>
    public List<string> EnabledSizeNames() =>
        ecState.Info?.Enabled.Where(p => p.Ec.StartsWith("EC-SIZE-", StringComparison.Ordinal)).Select(p => p.Ec).ToList()
        ?? [];

    /// <summary>Emit the post-store EC-SIZE handling: when the latched size-error name is one of the ENABLED
    /// names, set the last exception status and — unless the statement's own ON SIZE ERROR phrase takes
    /// precedence (§14.6.13.1.3 #1 / §14.6.13.1.4 #1) — run the §14.9.49 F3 selection and the fatal default
    /// (every EC-SIZE-* is fatal, Table 13).</summary>
    public void EmitSizeHandling(string flag, string ecnVar, List<string> enabled, bool hasPhrase)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        string nameTest = string.Join(" || ", enabled.Select(n => $"{ecnVar} == {CsLiteral(n)}"));
        var (stmt, loc) = EcStmtLoc(ecState.Info!);
        using (w.Block($"if ({flag} && ({nameTest}))"))
        {
            w.Line($"ExceptionState.Set({ecnVar}, true, {stmt}, {loc});   // §14.6.13.1.1 — the last exception status");
            if (!hasPhrase)
            {
                w.Line($"int __r{id} = {EcDispatchExpr(ecnVar, "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                w.Line($"if (__r{id} != -2) throw new CobolFatalException({ecnVar}, "
                    + $"\"size error and not resumed (ISO 14.7.5; 14.6.13.1.3 #5/#7)\" + {(stmt == "null" ? "\"\"" : $"\" in \" + {stmt}")});");
            }
            // With an ON SIZE ERROR phrase the phrase handles it (§14.6.13.1.3 #1) — state is set, phrase runs below.
        }
    }

    // ── The EC-OVERFLOW family (STRING/UNSTRING, §14.9.43 GR8b / §14.9.48 GR16b) ─────────────────────────────

    /// <summary>Emit the EC-OVERFLOW-STRING/-UNSTRING raise after the kernel latched <paramref name="ovfFlag"/>:
    /// set the last exception status; without an ON OVERFLOW phrase run the F3 selection (nonfatal — execution
    /// continues either way, §14.6.13.1.4 #3/#4).</summary>
    public void EmitOverflow(string ovfFlag, string ecName, bool hasPhrase)
    {
        if (ecState.Info is null || !ecState.Info.Enabled.Any(p => p.Ec == ecName)) return;
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        var (stmt, loc) = EcStmtLoc(ecState.Info);
        using (w.Block($"if ({ovfFlag})"))
        {
            w.Line($"ExceptionState.Set({CsLiteral(ecName)}, false, {stmt}, {loc});");
            if (!hasPhrase)
            {
                w.Line($"int __r{id} = {EcDispatchExpr(CsLiteral(ecName), "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}");
            }
        }
    }

    // ── The EC-I-O bridge (the per-statement hook variant; §9.1.13.1) ────────────────────────────────────────

    /// <summary>The enabled EC-I-O (name → mask bit) pairs of the current statement for <paramref name="file"/>,
    /// or 0 when none (the caller then emits the plain F1 hook).</summary>
    public int IoMaskFor(FileModel file)
    {
        if (ecState.Info is null) return 0;
        int mask = 0;
        foreach (var (ec, f) in ecState.Info.Enabled)
            if (ReferenceEquals(f, file))
                mask |= ExceptionCatalog.IoBit(ec);
        return mask;
    }

    // ── The generated machinery (__EcDispatch / __IoCheckEc) ─────────────────────────────────────────────────

    /// <summary>Generate <c>__EcDispatch</c> — the Format-3 declarative selector (ISO §14.9.49.4 GR3c–g): the
    /// USE statements are analyzed in SOURCE order within each tier — file+level-3, file+level-2, level-3,
    /// level-2, level-1 (EC-ALL) — and the FIRST match runs (GR3: "no other declaratives are executed"). Level-2
    /// matching uses the catalog's longest-family-prefix predicate (so the open EC-USER-*/EC-IMP-* names select
    /// correctly). The GR3g outward-GLOBAL continuation is realized only on the I-O path (the existing F1
    /// <c>__RunGlobalUse</c> walk) — recorded in the deep-dive.</summary>
    public void EmitDispatchSelector(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        using (w.Block("private int __EcDispatch(string __ec, string __f)"))
        {
            void Tier(string comment, Func<string, Binding.Model.FileModel?, int, string?> condition)
            {
                bool any = false;
                for (int i = 0; i < decls.Count; i++)
                {
                    foreach (var (ec, file) in decls[i].EcEntries ?? [])
                        if (condition(ec, file, i) is { } cond)
                        {
                            if (!any) { w.Line(comment); any = true; }
                            w.Line($"if ({cond}) return __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc});");
                        }
                }
            }
            bool L3(string ec) => ExceptionCatalog.TryGet(ec, out var i) && i.Level == 3;
            bool L2(string ec) => ExceptionCatalog.TryGet(ec, out var i) && i.Level == 2;

            Tier("// GR3c — file-scoped level-3 entries", (ec, f, i) =>
                f is not null && L3(ec) ? $"__f == {FileKeyExpr(f)} && __ec == {CsLiteral(ec)}" : null);
            Tier("// GR3d — file-scoped level-2 entries", (ec, f, i) =>
                f is not null && L2(ec) ? $"__f == {FileKeyExpr(f)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(ec)})" : null);
            Tier("// GR3e — level-3 entries", (ec, f, i) =>
                f is null && L3(ec) ? $"__ec == {CsLiteral(ec)}" : null);
            Tier("// GR3f — level-2 entries", (ec, f, i) =>
                f is null && L2(ec) ? $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(ec)})" : null);
            Tier("// GR3g — the level-1 EC-ALL entry", (ec, f, i) =>
                f is null && ec.Equals(ExceptionCatalog.EcAll, StringComparison.OrdinalIgnoreCase) ? "true" : null);
            w.Line("return -3;   // no qualifying declarative (GR3g tail)");
        }
        w.Line();
    }

    /// <summary>Generate <c>__EcObjDispatch</c> — the Format-4 exception-OBJECT selector (ISO §14.9.49.4
    /// GR14): source-order scan; a class entry matches the object's class OR a subclass — exactly C#'s
    /// <c>is</c> (GR14a); GR15 (EXCEPTION-OBJECT references the object on declarative entry) already holds —
    /// the raise site set the register before dispatching. A null object matches nothing (spec-literal:
    /// no class describes it) → -3, the caller's §14.6.13.1.5 conversion.</summary>
    public void EmitObjDispatchSelector(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        using (w.Block("private int __EcObjDispatch(object? __obj)"))
        {
            for (int i = 0; i < decls.Count; i++)
                if (decls[i].EoClassCsName is { } cs)
                    w.Line($"if (__obj is {cs}) return __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc});");
            w.Line("return -3;   // no matching class entry (GR14 tail → 14.6.13.1.5)");
        }
        w.Line();
    }

    /// <summary>Generate <c>__IoCheckEc</c> — the EC-aware after-verb hook a statement with enabled EC-I-O
    /// checking calls INSTEAD of <c>__IoCheck</c>: the same F1 behavior (phrase short-circuits §9.1.13.1; GR3a/b
    /// file/mode selection; the GR4b outward-GLOBAL walk) plus the §9.1.13.1 status→EC raise (the per-statement
    /// <c>__mask</c> gates by name — checking is per-(name, file) at COMPILE time), the F3 GR3c–g selection
    /// behind the F1 tiers, and the fatal-status default (status 3x/4x/7x/9x + enabled checking → abnormal
    /// termination unless a RESUME redirected — §9.1.13.1 / §14.9.49.4 GR12c; checking off keeps today's
    /// continue-on-error behavior, scout hazard H6).</summary>
    public void EmitIoCheckEc(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        using (w.Block("private int __IoCheckEc(string __f, bool __atEnd, bool __invKey, int __mask, string? __stmt, string? __loc)"))
        {
            w.Line($"string __st = {RuntimeApi.FileStatus("__f")};");
            w.Line("string? __ec = ExceptionCatalog.IoEcOfStatus(__st);   // §9.1.13.1 status→EC correspondence");
            w.Line("bool __en = __ec is not null && (__mask & ExceptionCatalog.IoBit(__ec)) != 0;");
            w.Line("if (__en) ExceptionState.SetIo(__ec!, ExceptionCatalog.IsFatalIoStatus(__st), __f, __st, __stmt, __loc);");
            using (w.Block("if (__st.Length == 0 || __st[0] == '0')"))
            {
                // A successful completion: '00' raises nothing; '0x' (x≠0) is EC-I-O-WARNING — F3 may select it
                // (no F1: those fire on unsuccessful execution only, §14.9.49.4 GR6). Nonfatal — never terminates.
                // With an exception-checking PERFORM active, a matching WHEN preempts (and ignores) the USE (GR17).
                w.Line("if (!__en) return -1;");
                if (ecState.UnitHasF3Perform)
                {
                    w.Line("int __w = __EcPerform(__ec!, __f);   // GR17 — a matching WHEN preempts USE; warning is nonfatal");
                    w.Line("return __w == -3 ? -1 : __w;");
                }
                else
                {
                    w.Line($"int __w = {(decls.Any(d => d.EcEntries is not null) ? "__EcDispatch(__ec!, __f)" : "-3")};");
                    w.Line("return __w == -3 ? -1 : __w;");
                }
            }
            w.Line("if (__atEnd && __st[0] == '1') return -1;    // the statement's AT END phrase covers the family (§9.1.13.1)");
            w.Line("if (__invKey && __st[0] == '2') return -1;   // the statement's INVALID KEY phrase covers its family (§9.1.13.1)");
            w.Line("int __sel = -3;");
            // The F1 file/open-mode + F3 USE declarative tiers (§14.9.49.4 GR3a–g/GR4b) — byte-identical to a pre-F3
            // build. With an exception-checking PERFORM active they run ONLY when no WHEN matched (GR17: a matching
            // WHEN ignores the USE); the frame is consulted FIRST, above these tiers.
            void EmitUseTiers()
            {
                if (decls.Any(d => d.Files.Count > 0))
                    using (w.Block("switch (__f)"))   // F1 file-name scope first (GR3a/GR5)
                    {
                        for (int i = 0; i < decls.Count; i++)
                            foreach (var f in decls[i].Files)
                                w.Line($"case {FileKeyExpr(f)}: __sel = __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); break;");
                    }
                if (decls.Any(d => d.ModeIndex is not null))
                    using (w.Block($"if (__sel == -3) switch ({RuntimeApi.FileOpenModeOf("__f")})"))   // F1 open-mode scope (GR3b/GR6b–e)
                    {
                        for (int i = 0; i < decls.Count; i++)
                            if (decls[i].ModeIndex is { } m)
                                w.Line($"case {m}: __sel = __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); break;");
                    }
                if (decls.Any(d => d.EcEntries is not null))
                    w.Line("if (__sel == -3 && __en) __sel = __EcDispatch(__ec!, __f);   // F3 tiers behind F1 (GR3c–g)");
                if (dispatch.OuterGlobalUse)
                    w.Line("if (__sel == -3 && __outer.__RunGlobalUse(__f)) __sel = -1;   // outward GLOBAL walk (GR4b)");
            }
            if (ecState.UnitHasF3Perform)
            {
                w.Line("bool __wh = false;");
                w.Line("__sel = ExceptionState.RunTopFrame(__ec!, __f, out __wh);   // GR17 — a matching WHEN preempts the USE declaratives");
                w.Line("if (!__wh) __sel = -3;   // no WHEN matched → fall to the USE tiers below");
                using (w.Block("if (!__wh)")) EmitUseTiers();
            }
            else EmitUseTiers();
            w.Line("if (__sel >= 0 || __sel == -2) return __sel;   // RESUME redirected/suppressed (§14.9.33)");
            w.Line("if (__en && ExceptionCatalog.IsFatalIoStatus(__st))");
            w.Line("    throw new CobolFatalException(__ec!, \"I-O status \" + __st + \" on \" + __f"
                + " + (__stmt is null ? \"\" : \" (\" + __stmt + \")\"));   // §9.1.13.1 fatal classes; §14.6.13.1.3 #5/#7");
            w.Line("return -1;");
        }
        w.Line();
    }

    /// <summary>Generate the exception-checking (Format-3) PERFORM interceptor plumbing (ISO §14.9.28.4 GR17-20) —
    /// emitted ONLY for a unit that contains an F3 PERFORM (<see cref="EcState.UnitHasF3Perform"/>), so a non-F3
    /// unit's source is byte-identical. <c>__EcPerform</c> consults the ambient F3-frame stack first (GR17: a
    /// matching WHEN preempts — and ignores — the USE declaratives) and falls to <c>__EcDispatch</c> (or the
    /// no-declarative <c>-3</c>) only when no frame handled the condition. <c>__RunF3</c> composes a WHEN handler
    /// (imp-2/imp-3) with WHEN COMMON (imp-4, GR19): COMMON runs ONLY after the handler COMPLETES (falls off →
    /// <c>-1</c>); a RESUME NEXT STATEMENT (<c>-2</c>) is a transfer out of the handler and short-circuits COMMON
    /// (design SSOT §9.6 Q3). Both handler bodies are bounded pc-ranges run by the reused <c>__RunUse</c>.</summary>
    public void EmitPerformInterceptor(CodeWriter w)
    {
        EmitEcPerformMember(w);
        EmitRunF3(w, asLocal: false);
    }

    /// <summary>Emit the class-member <c>__EcPerform</c> raise-site funnel (ISO §14.9.28.4 GR17: a matching WHEN
    /// preempts the USE declaratives). ALWAYS a class member — it reaches a handler only through
    /// <see cref="ExceptionEngine.RunTopFrame"/> → the frame's Matcher (never <c>__RunF3</c> directly), so it is
    /// class-callable even when the F3 PERFORM (and hence <c>__RunF3</c>/<c>__RunUse</c>) is METHOD-LOCAL (an OO
    /// method's F3 PERFORM, design SSOT §9.10). Emitted once per program (the interceptor) and once per class that
    /// has any method-F3 (<see cref="OoEmitter"/>, gated on <c>bound.Ec.HasF3Perform</c>).</summary>
    public void EmitEcPerformMember(CodeWriter w)
    {
        using (w.Block("private int __EcPerform(string __ec, string __f)"))
        {
            w.Line("int __a = ExceptionState.RunTopFrame(__ec, __f.Length == 0 ? null : __f, out bool __h);");
            w.Line($"return __h ? __a : {(ecState.UnitHasF3 ? "__EcDispatch(__ec, __f)" : "-3")};   "
                + "// GR17/18 win over USE; else the USE tiers / -3");
        }
        w.Line();
    }

    /// <summary>Emit <c>__RunF3</c> (the WHEN handler + WHEN COMMON composer, ISO §14.9.28.4 GR19). SCOPE-PARAMETERIZED:
    /// a class MEMBER for a program's F3 PERFORM, a method-LOCAL function for an OO method's F3 PERFORM (design SSOT
    /// §9.10 — it calls <c>__RunUse</c>, which calls the method-local <c>__MDispatch</c>). Its sole caller is the frame
    /// Matcher, emitted inline where the F3 PERFORM statement is (so a method-local <c>__RunF3</c> is in scope).</summary>
    public void EmitRunF3(CodeWriter w, bool asLocal)
    {
        using (w.Block($"{(asLocal ? "" : "private ")}int __RunF3(int __u, int __pc, int __cu, int __cpc)"))
        {
            // §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of
            // imperative-statement-1" — so imp-2/3/4 run with NO exception checking enabled (§14.6.13.1.1: "if
            // checking for an exception that occurs is not enabled, no exception condition is raised"). It has to
            // be done HERE, at runtime, and not only by binding the handler bodies under a disabled TurnState:
            // the ambient gates are set by the guard around the RAISING statement, and this composer is called
            // from inside that guard, before its finally clears them.
            w.Line("var __ck = ExceptionState.PushAllCheckingOff();   // GR14 implicit PUSH ALL + TURN OFF ALL");
            using (w.Block("try"))
            {
                w.Line("int __a = __RunUse(__u, __pc, __pc);   // imp-2 / imp-3 (a single-pc synthetic handler range)");
                w.Line("if (__a == -1 && __cpc >= 0) __a = __RunUse(__cu, __cpc, __cpc);   // WHEN COMMON (imp-4, GR19); -2 short-circuits");
                w.Line("return __a;");
            }
            w.Line("finally { ExceptionState.PopAllChecking(__ck); }   // GR14 implicit POP ALL");
        }
        w.Line();
    }
}
