// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The PROCEDURE-DIVISION statement dispatch (PHASE-07 Step 6b). This partial makes CSharpEmitter the ONE
//  IBoundStatementVisitor<bool> — the exhaustive generated interface (Cobol.Net.Compiler.SourceGen). Every bound
//  statement leaf has a Visit below (bool = "unconditionally transfers control out of the paragraph case"); the
//  hand-maintained 79-arm switch + its loud `default` are GONE, so a NEW BoundStatement leaf is a COMPILE error
//  here (a missing Visit), never a silent runtime LoudStmt. Each Visit is the former switch arm verbatim.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

public sealed partial class CSharpEmitter : IBoundStatementVisitor<bool>
{
    // ── Control flow / no-op ─────────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundStop n) { _ctx.Writer.Line("throw new StopRun();"); return true; }

    public bool Visit(BoundStopLiteral n)
    {
        // X3.23-1985 STOP literal: communicate to the operator (stderr), then continue (BoundTree doc).
        _ctx.Writer.Line($"Console.Error.WriteLine({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(n.Text, quote: true)});");
        return false;
    }

    public bool Visit(BoundGoTo n) { var w = _ctx.Writer; w.Line($"__pc = {n.TargetPc};"); w.Line("break;"); return true; }
    public bool Visit(BoundExitParagraph n) { var w = _ctx.Writer; w.Line($"__pc = {_currentPc + 1};"); w.Line("break;"); return true; }
    public bool Visit(BoundExitPerform n) { _ctx.Writer.Line(n.Cycle ? "continue;" : "break;"); return false; }   // inline-PERFORM loop
    public bool Visit(BoundGoToDepending n) { EmitGoToDepending(n); return false; }
    public bool Visit(BoundNop n) => false;

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
        if (_sentenceEndLabel is { } lbl) { w.Line($"goto {lbl};"); return true; }
        w.Line($"__pc = {_currentPc + 1};");
        w.Line("break;");
        return true;
    }

    public bool Visit(BoundUnsupported n) { _ctx.Writer.Line(LoudStmt(n.Feature)); return false; }

    // ── DISPLAY / MOVE / arithmetic ──────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundDisplay n) { EmitDisplay(n); return false; }
    public bool Visit(BoundMove n) { EmitMove(n); return false; }
    public bool Visit(BoundAddTo n) { EmitInPlace(n.Targets, "+", n.Addends, n.SizeError); return false; }
    public bool Visit(BoundAddGiving n) { EmitGiving(n.Targets, () => _num.Fold(n.Addends), n.SizeError); return false; }
    public bool Visit(BoundSubtractFrom n) { EmitInPlace(n.Targets, "-", n.Minuends, n.SizeError); return false; }
    public bool Visit(BoundSubtractGiving n) { EmitGiving(n.Targets, () => _num.Combine(_num.Render(n.From), "-", _num.Fold(n.Minuends)), n.SizeError); return false; }
    public bool Visit(BoundMultiplyBy n) { EmitInPlace(n.Targets, "*", [n.A], n.SizeError); return false; }
    public bool Visit(BoundMultiplyGiving n) { EmitGiving(n.Targets, () => _num.Combine(_num.Render(n.A), "*", _num.Render(n.B)), n.SizeError); return false; }
    public bool Visit(BoundDivideInto n) { EmitDivide(n.Targets, null, n.Divisor, n.SizeError); return false; }
    public bool Visit(BoundDivideGiving n) { EmitDivide(n.Targets, n.Dividend, n.Divisor, n.SizeError); return false; }
    public bool Visit(BoundDivideRemainder n) { EmitDivideRemainder(n); return false; }
    public bool Visit(BoundCompute n) { EmitCompute(n); return false; }
    public bool Visit(BoundComputeBoolean n) { EmitComputeBoolean(n); return false; }

    // ── Conditionals / loops / SEARCH / EVALUATE ─────────────────────────────────────────────────────────────
    public bool Visit(BoundIf n) { EmitIf(n); return false; }
    public bool Visit(BoundInlinePerform n) { EmitInlinePerform(n); return false; }
    public bool Visit(BoundOutOfLinePerform n) { EmitOutOfLinePerform(n); return false; }
    public bool Visit(BoundSetConditions n) { EmitSet(n); return false; }
    public bool Visit(BoundSetSwitches n) { SwitchEmitSet(n); return false; }
    public bool Visit(BoundAlter n) { AlterEmitAlter(n); return false; }
    public bool Visit(BoundGoToAlterable n) { AlterEmitGoTo(n); return true; }
    public bool Visit(BoundSetTo n) { EmitSetTo(n); return false; }
    public bool Visit(BoundSetUpDown n) { EmitSetUpDown(n); return false; }
    public bool Visit(BoundSetCapacity n) { EmitSetCapacity(n); return false; }
    public bool Visit(BoundSearch n) { EmitSearch(n); return false; }
    public bool Visit(BoundEvaluate n) { EmitEvaluate(n); return false; }
    public bool Visit(BoundInspect n) { EmitInspect(n); return false; }
    public bool Visit(BoundCorresponding n) { EmitCorresponding(n); return false; }

    // ── Sequential file I/O ──────────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundOpen n) { EmitOpen(n); return false; }
    public bool Visit(BoundClose n) { EmitClose(n); return false; }
    public bool Visit(BoundUnlock n) { EmitUnlock(n); return false; }
    public bool Visit(BoundWrite n) { EmitWrite(n); return false; }
    public bool Visit(BoundRead n) { EmitRead(n); return false; }
    public bool Visit(BoundRewrite n) { EmitRewrite(n); return false; }

    // ── Keyed (relative/indexed) file I/O ────────────────────────────────────────────────────────────────────
    public bool Visit(BoundKeyedRead n) { KeyedEmitRead(n); return false; }
    public bool Visit(BoundKeyedWrite n) { KeyedEmitWrite(n); return false; }
    public bool Visit(BoundKeyedRewrite n) { KeyedEmitRewrite(n); return false; }
    public bool Visit(BoundKeyedDelete n) { KeyedEmitDelete(n); return false; }
    public bool Visit(BoundKeyedStart n) { KeyedEmitStart(n); return false; }
    public bool Visit(BoundKeyedDeleteFile n) { KeyedEmitDeleteFile(n); return false; }

    // ── SORT / MERGE / RELEASE / RETURN ──────────────────────────────────────────────────────────────────────
    public bool Visit(BoundSort n) { EmitSort(n); return false; }
    public bool Visit(BoundTableSort n) { EmitTableSort(n); return false; }
    public bool Visit(BoundMerge n) { EmitMerge(n); return false; }
    public bool Visit(BoundRelease n) { EmitRelease(n); return false; }
    public bool Visit(BoundReturn n) { EmitReturn(n); return false; }

    // ── STRING / UNSTRING / ACCEPT / INITIALIZE ──────────────────────────────────────────────────────────────
    public bool Visit(BoundStringStmt n) { EmitString(n); return false; }
    public bool Visit(BoundUnstringStmt n) { EmitUnstring(n); return false; }
    public bool Visit(BoundAccept n) { EmitAccept(n); return false; }
    public bool Visit(BoundInitialize n) { EmitInitialize(n); return false; }

    // ── Report Writer (ISO §14.9) ────────────────────────────────────────────────────────────────────────────
    public bool Visit(BoundInitiate n) { RwEmitInitiate(n); return false; }     // §14.9.21
    public bool Visit(BoundGenerate n) { RwEmitGenerate(n); return false; }     // §14.9.16
    public bool Visit(BoundTerminate n) { RwEmitTerminate(n); return false; }   // §14.9.46

    // ── Interprogram: CALL / CANCEL / EXIT PROGRAM / GOBACK ──────────────────────────────────────────────────
    public bool Visit(BoundCallProgram n) => CallEmitCall(n);
    public bool Visit(BoundCancel n) { CallEmitCancel(n); return false; }
    public bool Visit(BoundExitProgram n) { CallEmitExitProgram(n); return false; }
    public bool Visit(BoundGoback n) => CallEmitGoback(n);

    // ── OO: INVOKE / SET object ref (ISO §14.9.23/§14.9.39; deep-dive D5/D8/D10) ─────────────────────────────
    public bool Visit(BoundInvoke n) { OoEmitInvoke(n); return false; }                 // §14.9.23
    public bool Visit(BoundInvokeUniversal n) { OoEmitUniversalInvoke(n); return false; }   // D10 universal dispatch (GR7c)
    public bool Visit(BoundSetObjectRef n) { OoEmitSetObjectRef(n); return false; }      // SET F5 (§14.9.39; D-U7)

    // ── Pointers: SET / ALLOCATE / FREE (ISO §14.9.39/§14.9.3/§14.9.15; Phase-4b) ────────────────────────────
    public bool Visit(BoundSetPointer n) { EmitSetPointer(n); return false; }               // SET pointer F4
    public bool Visit(BoundSetAddressOfBased n) { PtrEmitSetAddressOfBased(n); return false; }   // SET F7
    public bool Visit(BoundSetPointerUpDown n) { PtrEmitSetPointerUpDown(n); return false; }     // SET F10
    public bool Visit(BoundAllocate n) { PtrEmitAllocate(n); return false; }                     // ALLOCATE §14.9.3
    public bool Visit(BoundFree n) { PtrEmitFree(n); return false; }                             // FREE §14.9.15

    public bool Visit(BoundMethodReturn n)
    {
        // method GOBACK/EXIT METHOD (D8). GR1b (§14.9.18.4): the RAISING stages BEFORE the throw; the entry's
        // catch(MethodReturn) still delivers the RETURNING local + copy-outs, so the INVOKE-site pickup sees the
        // exception only AFTER the result — the result-before-exception ordering for free (D-EO6).
        if (n.Raising is { } mrr) CallEmitRaisingStage(mrr, "GOBACK");
        _ctx.Writer.Line("throw new MethodReturn();   // terminate the METHOD only — caught at the method entry (ISO §14.9.18.4 GR4)");
        return true;
    }

    // ── Exception-condition model (ISO §7.3.25 / §14.9.29 / §14.9.33 / §14.9.39) ─────────────────────────────
    public bool Visit(BoundRaiseObject n) => EcEmitRaiseObject(n);   // EC-OO (§14.9.29; §14.6.13.1.5)
    public bool Visit(BoundEcChecked n) => EcEmitChecked(n);         // TURN scope (§7.3.25)
    public bool Visit(BoundRaise n) => EcEmitRaise(n);               // RAISE (§14.9.29)
    public bool Visit(BoundResume n) { EcEmitResume(n); return true; }   // RESUME (§14.9.33) — unwinds
    public bool Visit(BoundSetLastException n) { _ctx.Writer.Line("ExceptionState.Clear();   // SET LAST EXCEPTION TO OFF (ISO §14.9.39 F13)"); return false; }
}
