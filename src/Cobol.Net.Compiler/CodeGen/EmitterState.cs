// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The emitter's mutable state model (P7 Step 9b; the phase doc's AS-BUILT PLAN). The cross-partial mutable
//  fields formerly scattered over the CSharpEmitter partials become three cohesive per-scope objects so the
//  Step-9 collaborator emitters receive their shared state EXPLICITLY (ctor-threaded), never through a god
//  class's private fields. All three are RUN-UNIT lifetime (fields of ProgramEmitter since Step 9n, like
//  NameAllocator); the "per unit" / "per statement" designations below document the MUTATION discipline —
//  which emitter writes the field, and when — preserved exactly from the pre-split partials (byte-exactness
//  gate: the 32 characterization snapshots).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The lexical region governing what an <c>EXIT PERFORM</c> compiles to (ISO §14.9.14.4 GR4/GR5 /
/// §14.9.28.4 GR16). For a Format-3 (exception-checking) PERFORM: a plain <c>goto</c> to the PERFORM's
/// implicit-CONTINUE-before-FINALLY label in imperative-statement-1 (<see cref="Imp1"/>), a thrown
/// <see cref="Runtime.Exceptions.ExitPerformSignal"/> from a handler pc-range (<see cref="Handler"/> — imp-2/3/4,
/// which runs in a nested dispatcher a goto cannot leave), or a <c>goto</c> to the end label in FINALLY
/// (<see cref="Finally"/> — imp-5). For an ORDINARY inline PERFORM (<see cref="Inline"/>): a <c>goto</c> to the
/// per-PERFORM <c>__pexit</c> label just past the loop (EXIT PERFORM) or the <c>__pcont</c> label at the
/// loop-control boundary (EXIT PERFORM CYCLE) — a bare C# <c>break</c>/<c>continue</c> cannot express GR5a/GR6
/// when a multi-level VARYING is emitted as nested loops (it would leave/cycle only the innermost). Every inline
/// PERFORM sets its OWN <see cref="Inline"/> region with a fresh id, so a nested inline PERFORM's EXIT PERFORM
/// targets the innermost loop (§14.9.14.4 GR5a "the most closely preceding, unterminated inline PERFORM").
/// <see cref="None"/> is a defensive fallback the binder never reaches for a valid EXIT PERFORM (SR8 permits it
/// only inside an inline/F3 PERFORM).</summary>
internal enum F3Region { None, Imp1, Handler, Finally, Inline }

/// <summary>The PC-dispatcher state the statement emitters cooperate over (COBOLNET_DESIGN §5): which paragraph
/// is being emitted, the NEXT SENTENCE label, the dispatch-method name, and the USE-declaratives hooks.</summary>
internal sealed class DispatchState
{
    /// <summary>The paragraph index being emitted (for EXIT PARAGRAPH / fall-through). Written per pc case by
    /// the dispatch-method emission.</summary>
    public int CurrentPc { get; set; }

    /// <summary>The current Format-3 PERFORM region + its <c>PerformId</c> (see <see cref="F3Region"/>) — read by
    /// <c>BoundExitPerform</c>'s emit. Default <see cref="F3Region.None"/> (an ordinary loop EXIT PERFORM). Set by
    /// <c>EmitExceptionPerform</c> (Imp1/Finally), by the dispatcher around a handler <c>case</c> (Handler), and
    /// reset to None around a nested inline/out-of-line PERFORM body.</summary>
    public (F3Region Region, int Id) F3Cur { get; private set; } = (F3Region.None, 0);

    /// <summary>Set <see cref="F3Cur"/>, returning the previous value for a later <see cref="RestoreF3Region"/>
    /// (the save/restore idiom around nested statement bodies).</summary>
    public (F3Region Region, int Id) SetF3Region(F3Region region, int id)
    {
        var saved = F3Cur;
        F3Cur = (region, id);
        return saved;
    }

    /// <summary>Restore <see cref="F3Cur"/> to a value captured by <see cref="SetF3Region"/>.</summary>
    public void RestoreF3Region((F3Region Region, int Id) saved) => F3Cur = saved;

    /// <summary>The unit's declarative count and the first appended Format-3 handler pc — the emitter derives a
    /// handler's <c>__useActive</c> id as <c>DeclCount + (pc − F3HandlerBasePc)</c> (the handler pc-ranges reuse
    /// <c>__RunUse</c>'s re-entrancy array above the declarative slots). Set per unit by the dispatcher emission;
    /// <see cref="F3HandlerBasePc"/> is null for a non-F3 unit.</summary>
    public int DeclCount { get; set; }
    public int? F3HandlerBasePc { get; set; }

    /// <summary>The goto target NEXT SENTENCE jumps to (null in the last sentence). Written per sentence by the
    /// paragraph-body emission.</summary>
    public string? SentenceEndLabel { get; set; }

    /// <summary>The dispatch-method NAME the statement emitters call for a bounded range (out-of-line PERFORM,
    /// SORT/MERGE procedures): <c>__Dispatch</c> for a program's instance method; <c>__MDispatch</c> while a
    /// COBOL-class METHOD body emits — its dispatcher is a LOCAL FUNCTION of the emitted method, so the
    /// method's LINKAGE/LOCAL-STORAGE locals are capturable (OO deep-dive D3/D6, slice 2). Saved/swapped/
    /// restored around each OO method body.</summary>
    public string DispatchName { get; set; } = "__Dispatch";

    /// <summary>The program being emitted declares USE procedures (drives the <c>__IoCheck</c> hooks). Set per
    /// unit by the dispatcher emission; cleared by the OO class-unit emission (a class owns no USE
    /// declaratives).</summary>
    public bool UseDecls { get; set; }

    /// <summary>A CONTAINING program has USE … GLOBAL declaratives (ISO §14.9.49.4 GR4b — the child's
    /// <c>__IoCheck</c> walks outward). Set per unit by the program-class emission.</summary>
    public bool OuterGlobalUse { get; set; }

    /// <summary>The program being emitted has an ACTIVE X3.23-1985 USE FOR DEBUGGING procedure-trigger facility
    /// (WITH DEBUGGING MODE + a procedure-subject debugging declarative; VCR Table 7 row 7.17). Gates the debug
    /// scaffolding — the <c>__dbgItem</c>/<c>__dbgCause</c> fields, the <c>__RunDebug</c> helper, the per-subject
    /// entry triggers, and the DEBUG-CONTENTS cause assignments threaded through every transfer of control — so a
    /// non-debug program's generated source is byte-identical (the zero-scaffolding invariant). Set per unit by the
    /// dispatcher emission; a class unit owns no debug facility.</summary>
    public bool DebugActive { get; set; }

    /// <summary>The debug trigger subjects keyed by their nondeclarative pc (empty unless <see cref="DebugActive"/>):
    /// the dispatch-method emission injects a <c>__RunDebug(...)</c> at each subject case's entry.</summary>
    public IReadOnlyDictionary<int, BoundDebugSubject> DebugByPc { get; set; } =
        new Dictionary<int, BoundDebugSubject>();

    /// <summary>Emit a DEBUG-CONTENTS cause + DEBUG-LINE assignment at a transfer of control, but ONLY when the
    /// debug facility is active (else nothing — the zero-scaffolding gate). <paramref name="cause"/> is a
    /// <c>DebugCause</c> enumerand name (the emitted file has <c>using CobolNet.Runtime;</c>);
    /// <paramref name="causingLine"/> is the source line of the CAUSING (transferring) statement — the X3.23-1985
    /// DEBUG-LINE (VCR 7.17). A non-positive line is omitted (e.g. START PROGRAM, whose DEBUG-LINE is the subject's
    /// own first statement, applied in __RunDebug).</summary>
    public void EmitDebugCause(CodeWriter w, string cause, int causingLine = 0)
    {
        if (!DebugActive) return;
        w.Line(causingLine > 0
            ? $"__dbgCause = DebugCause.{cause}; __dbgLine = {causingLine};"
            : $"__dbgCause = DebugCause.{cause};");
    }
}

/// <summary>The exception-condition emission state (ISO §14.6.13 / §7.3.25) shared by the EC wrappers, the
/// arithmetic size-error emission, the I/O hooks, CALL propagation, and pointer checks.</summary>
internal sealed class EcState
{
    /// <summary>Group-level: ANY EC feature in use (gates every machinery emission). Restored once per run unit
    /// from the immutable <c>BoundCompilation</c>.</summary>
    public bool Active { get; set; }

    /// <summary>The program class being emitted has F3 declaratives (→ <c>__EcDispatch</c> exists). Set per
    /// unit.</summary>
    public bool UnitHasF3 { get; set; }

    /// <summary>The program class being emitted has an exception-checking (Format-3) PERFORM (§14.9.28) → the funnel
    /// emits <c>__EcPerform</c> (consult the ambient F3-frame stack before the USE dispatch) and the pc-range
    /// handler machinery is installed, EVEN when the unit declares no F3 USE declaratives (the <c>UnitHasF3</c>
    /// gate alone is insufficient — §9.5). Set per unit from <c>EcFeatures.HasF3Perform</c>; false for OO methods
    /// (F3-in-method is loud-rejected). A non-F3 unit emits byte-identical source.</summary>
    public bool UnitHasF3Perform { get; set; }

    /// <summary>… has F4 (EXCEPTION OBJECT) declaratives (→ <c>__EcObjDispatch</c> exists). Set per unit.</summary>
    public bool UnitHasF4 { get; set; }

    /// <summary>The wrapper context of the statement being emitted (else null) — statement-scoped, saved/restored
    /// around each <c>BoundEcChecked</c> body.</summary>
    public EcStatementInfo? Info { get; set; }

    /// <summary>The current <c>__sizeErr</c> flag while emitting a checked arithmetic body (else null) —
    /// statement-scoped scratch set/cleared by the ON SIZE ERROR two-phase wrapper and read by the checked
    /// arithmetic stores (the EC↔arithmetic interlock).</summary>
    public string? SizeErrVar { get; set; }

    /// <summary>The current EC-SIZE name local while emitting a checked arithmetic body (else null) — the
    /// <c>&gt;&gt;TURN EC-SIZE</c> half of the interlock.</summary>
    public string? SizeErrEcVar { get; set; }
}

/// <summary>The inter-program emission state of the unit being emitted (COBOLNET_INTERPROGRAM_DESIGN D1–D5).</summary>
internal sealed class CallUnitState
{
    /// <summary>The emitted unit's qualified program path — the CALL/CANCEL calling-path argument (§8.4.6.3).
    /// Set per unit (a method body names its class).</summary>
    public string SelfPath { get; set; } = "";

    /// <summary>The LINKAGE RETURNING item's place (null when none) — the EXIT PROGRAM / GOBACK result store.
    /// Set per unit (methods deliver results via slice-2 RETURNING, never the program ABI).</summary>
    public Place? ReturningPlace { get; set; }

    /// <summary>For each GLOBAL file INHERITED from a container (ISO §13.18.30), the place of the OWNER's FILE
    /// STATUS item reached through the <c>__outer</c> instance chain. §12.4.5.8.4 GR1 NOTE 1: "In the case where
    /// a file-name is global and data-name-1 is not, data-name-1 is updated by references to file-name in
    /// contained programs even though data-name-1 is a local name" — the contained program's after-verb status
    /// store must write the OWNER's storage although the NAME is not visible to it. Rebuilt per emitted unit
    /// (nearest container first); consumed by the after-verb FILE STATUS store.</summary>
    public Dictionary<FileModel, Place> InheritedStatusPlace { get; } = [];
}
