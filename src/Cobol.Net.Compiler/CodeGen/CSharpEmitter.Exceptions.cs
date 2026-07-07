// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
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
public sealed partial class CSharpEmitter
{
    private TurnState _turnState = TurnState.Empty;
    private bool _ecActive;            // group-level: ANY EC feature in use (gates every machinery emission)
    private bool _ecUnitHasF3;         // the program class being emitted has F3 declaratives (→ __EcDispatch exists)
    private bool _ecUnitHasF4;         // … has F4 (EXCEPTION OBJECT) declaratives (→ __EcObjDispatch exists)
    private EcStatementInfo? _ecInfo;  // the wrapper context of the statement being emitted (else null)
    private int _ecCounter;            // unique-name counter for EC locals
    private string? _sizeErrEcVar;     // the current EC-SIZE name local while emitting a checked arithmetic body

    /// <summary>The <c>__EcDispatch</c> invocation (or the no-declarative constant when this program has no F3
    /// declaratives — same protocol, zero machinery).</summary>
    private string EcDispatchExpr(string ecNameExpr, string fileExpr) =>
        _ecUnitHasF3 ? $"__EcDispatch({ecNameExpr}, {fileExpr})" : "-3";

    /// <summary>The <c>__EcObjDispatch</c> invocation (or the no-declarative constant when this unit has no
    /// Format-4 declaratives) — the §14.9.49.4 GR14 exception-OBJECT selector (the EC-OO wave).</summary>
    private string EcObjDispatchExpr(string objExpr) =>
        _ecUnitHasF4 ? $"__EcObjDispatch({objExpr})" : "-3";

    /// <summary>RAISE identifier-1 (ISO §14.9.29.4 GR2; §14.6.13.1.5): set EXCEPTION-OBJECT, run the F4
    /// declarative if one matches (GR14 — GR3: F4 REPLACES the F1/F3 tiers for object raises), and in EVERY
    /// no-match/complete case continue with the next statement — a RAISE of an object is NEVER fatal by
    /// itself.</summary>
    private bool EcEmitRaiseObject(BoundRaiseObject ro)
    {
        var w = _ctx.Writer;
        int id = _ecCounter++;
        w.Line($"ExceptionState.SetObject({ro.Source?.Read() ?? "this"});   // §14.6.13.1.5 (1)/(2) — EXCEPTION-OBJECT + the status sentinel");
        w.Line($"int __r{id} = {EcObjDispatchExpr($"ExceptionState.ExceptionObject")};");
        w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
        w.Line($"// -1/-2/-3: declarative completed / RESUME NEXT / no match — continue after RAISE (§14.9.29.4 GR2)");
        return false;   // the continue-after-RAISE path IS the normal exit (GR2 — never fatal by itself)
    }

    private (string Stmt, string Loc) EcStmtLoc(EcStatementInfo info) =>
        info.WithLocation ? (CsLiteral(info.StatementName), CsLiteral(info.Location)) : ("null", "null");

    // ── The BoundEcChecked wrapper (the statement EC context + the EC-ARGUMENT-FUNCTION ambient gate) ────────

    private bool EcEmitChecked(BoundEcChecked ec)
    {
        var prev = _ecInfo;
        _ecInfo = ec.Info;
        bool terminated;
        // EC-DATA-CONVERSION (nonfatal, §15.19.4 r1/r3) rides an ambient per-statement gate — the nonfatal twin of
        // the EC-ARGUMENT-FUNCTION gate below: FUNCTION CONVERT's substitution site records the last exception
        // status while checking is enabled. Nonfatal ⇒ set/reset only (no catch, no throw); it wraps whichever
        // inner dispatch (the fatal-gated or the plain) the statement needs.
        if (ec.Info.Enabled.Any(p => p.Ec == "EC-DATA-CONVERSION"))
        {
            var w = _ctx.Writer;
            w.Line("ExceptionState.DataConversionChecking = true;");
            using (w.Block("try"))
                EcEmitArgOrPlain(ec);
            w.Line("finally { ExceptionState.DataConversionChecking = false; }");
            _ecInfo = prev;
            return false;   // conservative: the inner dispatch may itself resume past a transfer
        }
        terminated = EcEmitArgOrPlain(ec);
        _ecInfo = prev;
        return terminated;
    }

    /// <summary>The inner EC dispatch of a checked statement: the EC-ARGUMENT-FUNCTION fatal ambient gate (with
    /// USE F3 dispatch on the raise) or, when that condition is not enabled, a plain statement emission. Wrapped by
    /// <see cref="EcEmitChecked"/> with the nonfatal EC-DATA-CONVERSION gate when needed.</summary>
    private bool EcEmitArgOrPlain(BoundEcChecked ec)
    {
        bool terminated;
        if (ec.Info.Enabled.Any(p => p.Ec == "EC-ARGUMENT-FUNCTION"))
        {
            // EC-ARGUMENT-FUNCTION rides an ambient per-statement gate (ExceptionState.ArgumentFunctionChecking):
            // intrinsic calls render inline inside arbitrary expressions, so the guard wraps the STATEMENT and
            // the runtime domain-error sites consult the flag (ExceptionState.ArgumentError — §15.3's default
            // result 0 becomes the raise when checking is on; Table 13: Fatal).
            var w = _ctx.Writer;
            int id = _ecCounter++;
            var (stmt, loc) = EcStmtLoc(ec.Info);
            w.Line("ExceptionState.ArgumentFunctionChecking = true;");
            using (w.Block("try"))
                EmitStatement(ec.Inner);
            using (w.Block($"catch (CobolFatalException __af{id}) when (__af{id}.EcName == \"EC-ARGUMENT-FUNCTION\")"))
            {
                if (ec.Info.WithLocation)
                    w.Line($"ExceptionState.Set(\"EC-ARGUMENT-FUNCTION\", true, {stmt}, {loc});");
                w.Line($"int __r{id} = {EcDispatchExpr("\"EC-ARGUMENT-FUNCTION\"", "\"\"")};");
                w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                w.Line($"if (__r{id} != -2) throw;   // fatal, unresumed → abnormal termination (§14.6.13.1.3 #5/#7)");
            }
            w.Line("finally { ExceptionState.ArgumentFunctionChecking = false; }");
            terminated = false;   // conservative: the catch can resume past an inner transfer
        }
        else
            terminated = EmitStatement(ec.Inner);
        return terminated;
    }

    // ── RAISE (§14.9.29) ─────────────────────────────────────────────────────────────────────────────────────

    private bool EcEmitRaise(BoundRaise r)
    {
        var w = _ctx.Writer;
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
        int id = _ecCounter++;
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

    private void EcEmitResume(BoundResume r) =>
        _ctx.Writer.Line(r.TargetPc == ResumeSignal.NextStatement
            ? "throw new ResumeSignal(ResumeSignal.NextStatement);   // RESUME AT NEXT STATEMENT (§14.9.33.4 GR2)"
            : $"throw new ResumeSignal({r.TargetPc});   // RESUME AT procedure-name ≡ GO TO (§14.9.33.4 GR3)");

    // ── The EC-SIZE family over the checked-arithmetic shape (§14.7.5 ↔ Table 13) ───────────────────────────

    /// <summary>The EC-SIZE-* names the current statement has enabled (empty list when none / no wrapper).</summary>
    private List<string> EcEnabledSizeNames() =>
        _ecInfo?.Enabled.Where(p => p.Ec.StartsWith("EC-SIZE-", StringComparison.Ordinal)).Select(p => p.Ec).ToList()
        ?? [];

    /// <summary>Emit the post-store EC-SIZE handling: when the latched size-error name is one of the ENABLED
    /// names, set the last exception status and — unless the statement's own ON SIZE ERROR phrase takes
    /// precedence (§14.6.13.1.3 #1 / §14.6.13.1.4 #1) — run the §14.9.49 F3 selection and the fatal default
    /// (every EC-SIZE-* is fatal, Table 13).</summary>
    private void EcEmitSizeHandling(string flag, string ecnVar, List<string> enabled, bool hasPhrase)
    {
        var w = _ctx.Writer;
        int id = _ecCounter++;
        string nameTest = string.Join(" || ", enabled.Select(n => $"{ecnVar} == {CsLiteral(n)}"));
        var (stmt, loc) = EcStmtLoc(_ecInfo!);
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
    private void EcEmitOverflow(string ovfFlag, string ecName, bool hasPhrase)
    {
        if (_ecInfo is null || !_ecInfo.Enabled.Any(p => p.Ec == ecName)) return;
        var w = _ctx.Writer;
        int id = _ecCounter++;
        var (stmt, loc) = EcStmtLoc(_ecInfo);
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
    private int EcIoMaskFor(FileModel file)
    {
        if (_ecInfo is null) return 0;
        int mask = 0;
        foreach (var (ec, f) in _ecInfo.Enabled)
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
    private void EcEmitDispatchSelector(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        using (w.Block("private int __EcDispatch(string __ec, string __f)"))
        {
            void Tier(string comment, Func<string, Binding.FileModel?, int, string?> condition)
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
    private void EcEmitObjDispatchSelector(BoundProgram bound, CodeWriter w)
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
    private void EcEmitIoCheckEc(BoundProgram bound, CodeWriter w)
    {
        var decls = bound.Declaratives ?? [];
        using (w.Block("private int __IoCheckEc(string __f, bool __atEnd, bool __invKey, int __mask, string? __stmt, string? __loc)"))
        {
            w.Line("string __st = CobolFile.Status(__f);");
            w.Line("string? __ec = ExceptionCatalog.IoEcOfStatus(__st);   // §9.1.13.1 status→EC correspondence");
            w.Line("bool __en = __ec is not null && (__mask & ExceptionCatalog.IoBit(__ec)) != 0;");
            w.Line("if (__en) ExceptionState.SetIo(__ec!, ExceptionCatalog.IsFatalIoStatus(__st), __f, __st, __stmt, __loc);");
            using (w.Block("if (__st.Length == 0 || __st[0] == '0')"))
            {
                // A successful completion: '00' raises nothing; '0x' (x≠0) is EC-I-O-WARNING — F3 may select it
                // (no F1: those fire on unsuccessful execution only, §14.9.49.4 GR6). Nonfatal — never terminates.
                w.Line("if (!__en) return -1;");
                w.Line($"int __w = {(decls.Any(d => d.EcEntries is not null) ? "__EcDispatch(__ec!, __f)" : "-3")};");
                w.Line("return __w == -3 ? -1 : __w;");
            }
            w.Line("if (__atEnd && __st[0] == '1') return -1;    // the statement's AT END phrase covers the family (§9.1.13.1)");
            w.Line("if (__invKey && __st[0] == '2') return -1;   // the statement's INVALID KEY phrase covers its family (§9.1.13.1)");
            w.Line("int __sel = -3;");
            if (decls.Any(d => d.Files.Count > 0))
                using (w.Block("switch (__f)"))   // F1 file-name scope first (GR3a/GR5)
                {
                    for (int i = 0; i < decls.Count; i++)
                        foreach (var f in decls[i].Files)
                            w.Line($"case {FileKeyExpr(f)}: __sel = __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); break;");
                }
            if (decls.Any(d => d.ModeIndex is not null))
                using (w.Block("if (__sel == -3) switch (CobolFile.OpenModeOf(__f))"))   // F1 open-mode scope (GR3b/GR6b–e)
                {
                    for (int i = 0; i < decls.Count; i++)
                        if (decls[i].ModeIndex is { } m)
                            w.Line($"case {m}: __sel = __RunUse({i}, {decls[i].StartPc}, {decls[i].HandlerEndPc}); break;");
                }
            if (decls.Any(d => d.EcEntries is not null))
                w.Line("if (__sel == -3 && __en) __sel = __EcDispatch(__ec!, __f);   // F3 tiers behind F1 (GR3c–g)");
            if (_callOuterGlobalUse)
                w.Line("if (__sel == -3 && __outer.__RunGlobalUse(__f)) __sel = -1;   // outward GLOBAL walk (GR4b)");
            w.Line("if (__sel >= 0 || __sel == -2) return __sel;   // RESUME redirected/suppressed (§14.9.33)");
            w.Line("if (__en && ExceptionCatalog.IsFatalIoStatus(__st))");
            w.Line("    throw new CobolFatalException(__ec!, \"I-O status \" + __st + \" on \" + __f"
                + " + (__stmt is null ? \"\" : \" (\" + __stmt + \")\"));   // §9.1.13.1 fatal classes; §14.6.13.1.3 #5/#7");
            w.Line("return -1;");
        }
        w.Line();
    }
}
