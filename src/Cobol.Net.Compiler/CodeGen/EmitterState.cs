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

/// <summary>The PC-dispatcher state the statement emitters cooperate over (COBOLNET_DESIGN §5): which paragraph
/// is being emitted, the NEXT SENTENCE label, the dispatch-method name, and the USE-declaratives hooks.</summary>
internal sealed class DispatchState
{
    /// <summary>The paragraph index being emitted (for EXIT PARAGRAPH / fall-through). Written per pc case by
    /// the dispatch-method emission.</summary>
    public int CurrentPc { get; set; }

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
