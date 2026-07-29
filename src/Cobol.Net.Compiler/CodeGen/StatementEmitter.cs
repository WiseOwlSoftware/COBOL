// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The PROCEDURE-DIVISION statement dispatch (PHASE-07 Step 6b; a real collaborator since Step 9n). This class
//  is the ONE IBoundStatementVisitor<bool> — the exhaustive generated interface (Cobol.Net.Compiler.SourceGen).
//  Every bound statement leaf has a Visit below (bool = "unconditionally transfers control out of the paragraph
//  case"); the hand-maintained 79-arm switch + its loud `default` are GONE, so a NEW BoundStatement leaf is a
//  COMPILE error here (a missing Visit), never a silent runtime LoudStmt. Constructed per unit by UnitEmitters
//  with DIRECT collaborator references (the former CSharpEmitter host shims are deleted); the ctor copies the
//  refs out of the root — every referenced collaborator is already constructed when the root news this class.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

internal sealed class StatementEmitter : IBoundStatementVisitor<bool>
{
    private readonly EmitContext _ctx;
    private readonly NumericRenderer _num;
    private readonly DispatchState _dispatchState;
    private readonly MoveEmitter _move;
    private readonly ArithmeticEmitter _arith;
    private readonly AlterSwitchEmitter _alterSwitch;
    private readonly AcceptDisplayEmitter _acceptDisplay;
    private readonly EvaluateEmitter _evaluate;
    private readonly InitializeEmitter _initialize;
    private readonly CorrespondingEmitter _corresponding;
    private readonly InspectEmitter _inspect;
    private readonly StringEmitter _strings;
    private readonly PtrEmitter _ptr;
    private readonly SetEmitter _set;
    private readonly KeyedIoEmitter _keyedIo;
    private readonly SequentialIoEmitter _seqIo;
    private readonly SortEmitter _sort;
    private readonly ReportWriterEmitter _reportWriter;
    private readonly ControlFlowEmitter _controlFlow;
    private readonly CallEmitter _call;
    private readonly EcEmitter _ecEmit;
    private readonly OoEmitter _oo;

    public StatementEmitter(UnitEmitters u, OoEmitter oo, DispatchState dispatchState)
    {
        _ctx = u.Ctx;
        _num = u.Num;
        _dispatchState = dispatchState;
        _move = u.Move;
        _arith = u.Arith;
        _alterSwitch = u.AlterSwitch;
        _acceptDisplay = u.AcceptDisplay;
        _evaluate = u.Evaluate;
        _initialize = u.Initialize;
        _corresponding = u.Corresponding;
        _inspect = u.Inspect;
        _strings = u.Strings;
        _ptr = u.Ptr;
        _set = u.Set;
        _keyedIo = u.KeyedIo;
        _seqIo = u.SeqIo;
        _sort = u.Sort;
        _reportWriter = u.ReportWriter;
        _controlFlow = u.ControlFlow;
        _call = u.Call;
        _ecEmit = u.Ec;
        _oo = oo;
    }

    /// <summary>Emit a statement list (a paragraph case, an IF branch, or an inline-PERFORM body), suppressing dead
    /// code after an unconditional transfer; returns whether the list ends by transferring control out of the case.</summary>
    internal bool EmitStatementList(IReadOnlyList<BoundStatement> stmts)
    {
        bool terminated = false;
        foreach (var st in stmts)
        {
            if (terminated) break;   // unreachable after an unconditional GO TO / STOP / EXIT PARAGRAPH
            terminated = EmitStatement(st);
        }
        return terminated;
    }

    /// <summary>Emit one statement; returns true if it unconditionally transfers control out of the paragraph case.
    /// Dispatch is the generated exhaustive <see cref="IBoundStatementVisitor{T}"/> (PHASE-07 Step 6b): every bound
    /// statement leaf has a <c>Visit</c> below, so a missing arm is a COMPILE error — the former 79-arm switch and
    /// its loud <c>default</c> are gone.</summary>
    internal bool EmitStatement(BoundStatement s) => s.Accept(this);

    // ── Control flow / no-op ─────────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundStop n)
    {
        // STOP RUN … WITH {NORMAL|ERROR} STATUS [value] (§14.9.42.4 GR5): pass the status to the OS (the process
        // exit code) before unwinding. No status phrase ⇒ no SetExitStatus (exit stays 0 — byte-identical default).
        if (n.Status is { } st) _ctx.Writer.Line(RuntimeApi.SetExitStatus(_num.ExitStatus(st)) + ";");
        _ctx.Writer.Line("throw new StopRun();");
        return true;
    }

    public bool Visit(BoundStopLiteral n)
    {
        // X3.23-1985 STOP literal: communicate to the operator (stderr), then continue (BoundTree doc).
        _ctx.Writer.Line($"Console.Error.WriteLine({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(n.Text, quote: true)});");
        return false;
    }

    // X3.23-1985 USE FOR DEBUGGING (VCR 7.17): a GO TO transfer is DEBUG-CONTENTS SPACES (Transfer); EXIT PARAGRAPH
    // returns to the paragraph end (a controlled fall-through into pc+1) — DEBUG-CONTENTS "FALL THROUGH". DEBUG-LINE
    // is the transferring statement's own source line.
    public bool Visit(BoundGoTo n) { var w = _ctx.Writer; _dispatchState.EmitDebugCause(w, "Transfer", n.SourceLine); w.Line($"__pc = {n.TargetPc};"); w.Line("break;"); return true; }
    public bool Visit(BoundExitParagraph n) { var w = _ctx.Writer; _dispatchState.EmitDebugCause(w, "FallThrough", n.SourceLine); w.Line($"__pc = {_dispatchState.CurrentPc + 1};"); w.Line("break;"); return true; }

    // EXIT SECTION (§14.9.14.4 GR7): transfer to the unnamed empty paragraph after the section's last paragraph
    // (SectionEndPc+1) from ANY paragraph of the section. When the enclosing bounded dispatch was entered with its
    // exit AT the section end (PERFORM SECTION / PERFORM … THRU the section end / SORT-or-USE / the top-level end
    // wall), the section's return mechanism must fire — an explicit `return __pc` that mirrors the bounded loop's
    // `__atExit` tail-check (which a MID-section EXIT SECTION cannot reach, since __atExit was captured for the
    // current pc, not the section end). Otherwise (exit ≠ section end) the `break` falls through to SectionEndPc+1
    // exactly as EXIT PARAGRAPH does at a paragraph boundary.
    public bool Visit(BoundExitSection n)
    {
        var w = _ctx.Writer;
        _dispatchState.EmitDebugCause(w, "FallThrough", n.SourceLine);
        w.Line($"__pc = {n.SectionEndPc + 1};");
        w.Line($"if (__exitPc == {n.SectionEndPc}) return __pc;   // §14.9.14.4 GR7 — the section's PERFORM/SORT/USE return");
        w.Line("break;");
        return true;
    }
    public bool Visit(BoundExitPerform n) => _dispatchState.F3Cur.Region switch   // §14.9.14.4 GR4/GR5/GR6; §14.9.28.4 GR16
    {
        // Inside a Format-3 PERFORM: imp-1 → goto the implicit-CONTINUE-before-FINALLY label; a handler pc-range →
        // throw ExitPerformSignal (crosses the nested __Dispatch a goto cannot leave); FINALLY (imp-5) → goto the
        // end label. Each transfers control (returns true — the statement sequence terminates).
        F3Region.Imp1 => Emit($"goto __f3fin{_dispatchState.F3Cur.Id};", terminated: true),
        F3Region.Handler => Emit($"throw new ExitPerformSignal({_dispatchState.F3Cur.Id});", terminated: true),
        F3Region.Finally => Emit($"goto __f3end{_dispatchState.F3Cur.Id};", terminated: true),
        // Ordinary inline PERFORM: EXIT PERFORM → goto __pexit (past the loop, leaving EVERY nested VARYING level,
        // §14.9.14.4 GR5a); EXIT PERFORM CYCLE → goto __pcont (the loop-control boundary, so the VARYING augment +
        // re-test still run, §14.9.14.4 GR6 / §14.9.28.4 GR13). A bare break/continue exits/cycles only the innermost
        // C# loop, wrong for a multi-level VARYING (CA31/CA32). The __pexit/__pcont labels are emitted by EmitPerform.
        F3Region.Inline => Emit(n.Cycle ? $"goto __pcont{_dispatchState.F3Cur.Id};" : $"goto __pexit{_dispatchState.F3Cur.Id};", terminated: true),
        _ => Emit(n.Cycle ? "continue;" : "break;", terminated: false),   // defensive fallback (SR8: never reached for a valid bind)
    };

    private bool Emit(string line, bool terminated) { _ctx.Writer.Line(line); return terminated; }
    public bool Visit(BoundGoToDepending n) { _controlFlow.EmitGoToDepending(n); return false; }
    public bool Visit(BoundNop n) => false;
    public bool Visit(BoundContinueAfter n)
    {
        // CONTINUE AFTER n SECONDS (§14.9.9): evaluate the interval at FULL precision (the GR1a/GR1b sign test
        // precedes the m=0 truncation), then suspend via the runtime, which sets the nonfatal
        // EC-CONTINUE-LESS-THAN-ZERO under CHECKING ON for a negative value (incl. a fractional (-1,0)) and truncates
        // toward zero (m=0) for the positive-value sleep.
        string secs = NumericRenderer.Real(_num.Render(n.Seconds, ReceiverContext.None));
        _ctx.Writer.Line(RuntimeApi.ContinueAfter(secs, n.CheckLessThanZero ? "true" : "false") + ";");
        return false;
    }

    public bool Visit(BoundSequence n)
    {
        // Render children consecutively (D-P2); "terminates" iff the last child does (a GET/SET
        // wrapper never changes the wrapped statement's fall-through shape).
        bool terminated = false;
        foreach (var step in n.Steps) terminated = EmitStatement(step);
        return terminated;
    }

    public bool Visit(BoundNextSentence n)
    {
        // §14.9.19 GR6: to the implicit CONTINUE after the current sentence; in the LAST sentence that is
        // the paragraph fall-through (pc+1 — the dispatcher's at-exit check then handles a PERFORM return).
        var w = _ctx.Writer;
        if (_dispatchState.SentenceEndLabel is { } lbl) { w.Line($"goto {lbl};"); return true; }
        _dispatchState.EmitDebugCause(w, "FallThrough", n.SourceLine);   // to pc+1 (X3.23-1985 "FALL THROUGH", VCR 7.17)
        w.Line($"__pc = {_dispatchState.CurrentPc + 1};");
        w.Line("break;");
        return true;
    }

    public bool Visit(BoundUnsupported n) { _ctx.Writer.Line(LoudStmt(n.Feature)); return false; }

    // ── DISPLAY / MOVE / arithmetic ──────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundDisplay n) { _acceptDisplay.EmitDisplay(n); return false; }
    public bool Visit(BoundMove n) { _move.Emit(n); return false; }
    public bool Visit(BoundAddTo n) { _arith.EmitInPlace(n.Targets, "+", n.Addends, n.SizeError); return false; }
    public bool Visit(BoundAddGiving n) { _arith.EmitGiving(n.Targets, rcv => _num.Fold(n.Addends, rcv), n.SizeError); return false; }
    public bool Visit(BoundSubtractFrom n) { _arith.EmitInPlace(n.Targets, "-", n.Minuends, n.SizeError); return false; }
    public bool Visit(BoundSubtractGiving n) { _arith.EmitGiving(n.Targets, rcv => _num.Combine(_num.Render(n.From, rcv), "-", _num.Fold(n.Minuends, rcv), rcv), n.SizeError); return false; }
    public bool Visit(BoundMultiplyBy n) { _arith.EmitInPlace(n.Targets, "*", [n.A], n.SizeError); return false; }
    public bool Visit(BoundMultiplyGiving n) { _arith.EmitGiving(n.Targets, rcv => _num.Combine(_num.Render(n.A, rcv), "*", _num.Render(n.B, rcv), rcv), n.SizeError); return false; }
    public bool Visit(BoundDivideInto n) { _arith.EmitDivide(n.Targets, null, n.Divisor, n.SizeError); return false; }
    public bool Visit(BoundDivideGiving n) { _arith.EmitDivide(n.Targets, n.Dividend, n.Divisor, n.SizeError); return false; }
    public bool Visit(BoundDivideRemainder n) { _arith.EmitDivideRemainder(n); return false; }
    public bool Visit(BoundCompute n) { _arith.EmitCompute(n); return false; }
    public bool Visit(BoundComputeBoolean n) { _arith.EmitComputeBoolean(n); return false; }

    // ── Conditionals / loops / SEARCH / EVALUATE ─────────────────────────────────────────────────────────────
    public bool Visit(BoundIf n) { _controlFlow.EmitIf(n); return false; }
    public bool Visit(BoundInlinePerform n) { _controlFlow.EmitInlinePerform(n); return false; }
    public bool Visit(BoundExceptionPerform n) { _controlFlow.EmitExceptionPerform(n); return false; }
    public bool Visit(BoundOutOfLinePerform n) { _controlFlow.EmitOutOfLinePerform(n); return false; }
    public bool Visit(BoundSetConditions n) { _set.EmitSet(n); return false; }
    public bool Visit(BoundSetSwitches n) { _alterSwitch.EmitSetSwitches(n); return false; }
    public bool Visit(BoundAlter n) { _alterSwitch.EmitAlter(n); return false; }
    public bool Visit(BoundGoToAlterable n) { _alterSwitch.EmitGoTo(n); return true; }
    public bool Visit(BoundSetTo n) { _set.EmitSetTo(n); return false; }
    public bool Visit(BoundSetUpDown n) { _set.EmitSetUpDown(n); return false; }
    public bool Visit(BoundSetCapacity n) { _set.EmitSetCapacity(n); return false; }
    public bool Visit(BoundSetSize n) { _set.EmitSetSize(n); return false; }
    public bool Visit(BoundSearch n) { _controlFlow.EmitSearch(n); return false; }
    public bool Visit(BoundEvaluate n) { _evaluate.Emit(n); return false; }
    public bool Visit(BoundInspect n) { _inspect.Emit(n); return false; }
    public bool Visit(BoundCorresponding n) { _corresponding.Emit(n); return false; }

    // ── Sequential file I/O ──────────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundOpen n) { _seqIo.EmitOpen(n); return false; }
    public bool Visit(BoundClose n) { _seqIo.EmitClose(n); return false; }
    public bool Visit(BoundUnlock n) { _seqIo.EmitUnlock(n); return false; }
    public bool Visit(BoundWrite n) { _seqIo.EmitWrite(n); return false; }
    public bool Visit(BoundRead n) { _seqIo.EmitRead(n); return false; }
    public bool Visit(BoundRewrite n) { _seqIo.EmitRewrite(n); return false; }

    // ── Keyed (relative/indexed) file I/O ────────────────────────────────────────────────────────────────────
    public bool Visit(BoundKeyedRead n) { _keyedIo.EmitRead(n); return false; }
    public bool Visit(BoundKeyedWrite n) { _keyedIo.EmitWrite(n); return false; }
    public bool Visit(BoundKeyedRewrite n) { _keyedIo.EmitRewrite(n); return false; }
    public bool Visit(BoundKeyedDelete n) { _keyedIo.EmitDelete(n); return false; }
    public bool Visit(BoundKeyedStart n) { _keyedIo.EmitStart(n); return false; }
    public bool Visit(BoundKeyedDeleteFile n) { _keyedIo.EmitDeleteFile(n); return false; }

    // ── SORT / MERGE / RELEASE / RETURN ──────────────────────────────────────────────────────────────────────
    public bool Visit(BoundSort n) { _sort.EmitSort(n); return false; }
    public bool Visit(BoundTableSort n) { _sort.EmitTableSort(n); return false; }
    public bool Visit(BoundMerge n) { _sort.EmitMerge(n); return false; }
    public bool Visit(BoundRelease n) { _sort.EmitRelease(n); return false; }
    public bool Visit(BoundReturn n) { _sort.EmitReturn(n); return false; }

    // ── STRING / UNSTRING / ACCEPT / INITIALIZE ──────────────────────────────────────────────────────────────
    public bool Visit(BoundStringStmt n) { _strings.EmitString(n); return false; }
    public bool Visit(BoundUnstringStmt n) { _strings.EmitUnstring(n); return false; }
    public bool Visit(BoundAccept n) { _acceptDisplay.EmitAccept(n); return false; }
    public bool Visit(BoundInitialize n) { _initialize.Emit(n); return false; }

    // ── Report Writer (ISO §14.9) ────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundInitiate n) { _reportWriter.EmitInitiate(n); return false; }     // §14.9.21
    public bool Visit(BoundGenerate n) { _reportWriter.EmitGenerate(n); return false; }     // §14.9.16
    public bool Visit(BoundTerminate n) { _reportWriter.EmitTerminate(n); return false; }   // §14.9.46
    public bool Visit(BoundSuppress n) { _reportWriter.EmitSuppress(n); return false; }     // §14.9.45

    // ── Interprogram: CALL / CANCEL / EXIT PROGRAM / GOBACK ──────────────────────────────────────────────────
    public bool Visit(BoundCallProgram n) => _call.EmitCall(n);
    public bool Visit(BoundCancel n) { _call.EmitCancel(n); return false; }
    public bool Visit(BoundExitProgram n) { _call.EmitExitProgram(n); return false; }
    public bool Visit(BoundGoback n) => _call.EmitGoback(n);

    // ── OO: INVOKE / SET object ref (ISO §14.9.23/§14.9.39; deep-dive D5/D8/D10) ─────────────────────────────
    public bool Visit(BoundInvoke n) { _oo.EmitInvoke(n); return false; }                 // §14.9.23
    public bool Visit(BoundInvokeUniversal n) { _oo.EmitUniversalInvoke(n); return false; }   // D10 universal dispatch (GR7c)
    public bool Visit(BoundSetObjectRef n) { _oo.EmitSetObjectRef(n); return false; }      // SET F5 (§14.9.39; D-U7)

    // ── Pointers: SET / ALLOCATE / FREE (ISO §14.9.39/§14.9.3/§14.9.15; Phase-4b) ────────────────────────────
    public bool Visit(BoundSetPointer n) { _set.EmitSetPointer(n); return false; }               // SET pointer F4
    public bool Visit(BoundSetProgramPointer n) { _set.EmitSetProgramPointer(n); return false; } // SET program-pointer F9 (P10 Step 7)
    public bool Visit(BoundSetEntry n) { _ptr.EmitSetEntry(n); return false; }                   // SET … TO ENTRY (§8.4.3.13)
    public bool Visit(BoundSetAddressOfBased n) { _ptr.EmitSetAddressOfBased(n); return false; }   // SET F7
    public bool Visit(BoundSetPointerUpDown n) { _ptr.EmitSetPointerUpDown(n); return false; }     // SET F10
    public bool Visit(BoundAllocate n) { _ptr.EmitAllocate(n); return false; }                     // ALLOCATE §14.9.3
    public bool Visit(BoundFree n) { _ptr.EmitFree(n); return false; }                             // FREE §14.9.15

    public bool Visit(BoundMethodReturn n)
    {
        // method GOBACK/EXIT METHOD (D8). GR1b (§14.9.18.4): the RAISING stages BEFORE the throw; the entry's
        // catch(MethodReturn) still delivers the RETURNING local + copy-outs, so the INVOKE-site pickup sees the
        // exception only AFTER the result — the result-before-exception ordering for free (D-EO6).
        if (n.Raising is { } mrr) _call.EmitRaisingStage(mrr, "GOBACK");
        _ctx.Writer.Line("throw new MethodReturn();   // terminate the METHOD only — caught at the method entry (ISO §14.9.18.4 GR4)");
        return true;
    }

    // ── Exception-condition model (ISO §7.3.25 / §14.9.29 / §14.9.33 / §14.9.39) ─────────────────────────────
    public bool Visit(BoundRaiseObject n) => _ecEmit.EmitRaiseObject(n);   // EC-OO (§14.9.29; §14.6.13.1.5)
    public bool Visit(BoundEcChecked n) => _ecEmit.EmitChecked(n);         // TURN scope (§7.3.25)
    public bool Visit(BoundRaise n) => _ecEmit.EmitRaise(n);               // RAISE (§14.9.29)
    public bool Visit(BoundResume n) { _ecEmit.EmitResume(n); return true; }   // RESUME (§14.9.33) — unwinds
    public bool Visit(BoundSetLastException n) { _ctx.Writer.Line("ExceptionState.Clear();   // SET LAST EXCEPTION TO OFF (ISO §14.9.39 F13)"); return false; }
}
