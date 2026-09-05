// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The inter-program runtime (COBOLNET_INTERPROGRAM_DESIGN D1–D5; ISO/IEC 1989:2023 §14.9.4 CALL / §14.9.5 CANCEL /
//  §14.2 procedure-division parameter passing / §8.4.6.3 program-name scope / §8.6.7 EXTERNAL sharing):
//    • ManagedPointer / ManagedPointer<T> — the ONE managed-reference carrier (design D1; the typed-native
//      re-implementation of the legacy ManagedPointer — internally the typed carrier, public name kept per the
//      settled SSOT §18 #12). Serves BY REFERENCE arguments and LINKAGE formals today; USAGE POINTER / ADDRESS OF /
//      BASED / ALLOCATE reuse the same carrier (singular-pattern rule).
//    • ICobolProgram / CobolArg — the uniform opaque calling ABI (design D2): an ordered (mode, carrier, meta)
//      argument list every compiled program-class accepts, so dynamic CALL (identifier) and cross-assembly CALL
//      need no knowledge of the callee's LINKAGE.
//    • ProgramReturn — the called-program return signal (settled SSOT §18 #10; Control/Signals/).
//    • ProgramTable (instance, on RunUnit) — program-name resolution honoring §8.4.6.3, the §14.6.2.3 state model,
//      and CANCEL semantics; ProgramRegistry (this file) is its static emitted-surface shim.
//    • ExternalTable (instance, on RunUnit) — one storage copy per external name per run unit (§8.6.7 /
//      §13.18.22); NOT reset by CANCEL (§14.9.5 GR8); ExternalStore is its static shim.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// An ACTIVATION-ATTEMPT failure of CALL / CANCEL / a user-function reference — ISO §14.9.4.4 GR3b–f's "the
/// program call is not successful" set: program not found (GR3b, EC-PROGRAM-NOT-FOUND), a NULL program-pointer
/// (GR3b, EC-PROGRAM-PTR-NULL), an argument-count violation (GR3d, EC-PROGRAM-ARG-MISMATCH), an external
/// non-conformance (GR3e, the EC-EXTERNAL trio), a re-entry of an active non-RECURSIVE program (GR3f,
/// EC-PROGRAM-RECURSIVE-CALL), CANCEL of an active program (§14.9.5 GR5, EC-PROGRAM-CANCEL-ACTIVE), or a
/// user-defined-function locate miss (§8.4.3.2.4 GR6b, EC-FUNCTION-NOT-FOUND). <see cref="CarriedNames"/> is
/// the whole set. At <c>--std 85</c> there is no EC machinery — a CALL site with an ON OVERFLOW phrase converts
/// this to the overflow branch (the 85 surface); without one, the run unit terminates loudly (abnormal
/// termination). <paramref name="ecName"/> carries the Table 13 level-3 name so a CALL site compiled with that
/// name's checking enabled (>>TURN, §7.3.25) can set the last exception status and run the §14.9.49 F3
/// declarative selection over the precise condition.
/// <para>⛔ NOT every failure inside a called program: a reference to an OMITTED argument raises
/// EC-PROGRAM-ARG-OMITTED (§14.9.4.4 GR12) through the CALLEE's own checked-raise gate, precisely because it is
/// an in-execution raise and not an activation failure — the old carrier threw this exception for it and every
/// enclosing CALL site read it as its own GR3h failure (kb/Work PB133). <see cref="ControlTransferred"/> is the
/// general form of that distinction.</para>
/// </summary>
public sealed class CobolCallException(string message, string ecName = "EC-PROGRAM-IMP") : Exception(message)
{
    /// <summary>The Table 13 level-3 exception-name of this failure (uppercase).</summary>
    public string EcName { get; } = ecName;

    /// <summary>ISO §14.9.4.4 GR3i — "If the program was successfully called, after control is returned from
    /// the called program the ON EXCEPTION phrase, if specified, is ignored." False while the condition is
    /// still THIS activation attempt's own GR3b–f/GR3e failure ("the program was not successfully called",
    /// GR3h); set to true by the activation boundary (<c>ProgramTable.CallProgram</c>) as the condition
    /// crosses OUT of the called program's execution. The mark is monotone: for every CALL site above the one
    /// that actually failed, control HAD been transferred, so those sites leave the condition to §14.6.13.1
    /// instead of running their own imperative-statement-1. Without it a nested failure ran — and was
    /// swallowed by — every enclosing CALL's ON EXCEPTION phrase (kb/Work PB233).</summary>
    public bool ControlTransferred { get; set; }

    /// <summary>ISO §14.9.4.4 GR3h item 1's family partition — "if the exception condition is any of the
    /// EC-PROGRAM or EC-EXTERNAL exception conditions". THE one place that partition is written down: the CALL
    /// emitter's enabled-name split and the emitted phrase arm's runtime filter both ask this. Every other
    /// condition a <see cref="CobolCallException"/> can carry (today EC-FUNCTION-NOT-FOUND, §8.4.3.2.4 GR6b)
    /// takes GR3h item 2's second disjunct instead — the applicable exception processing statements, never
    /// imperative-statement-1.</summary>
    public static bool IsProgramOrExternal(string ec) =>
        ec.StartsWith("EC-PROGRAM-", StringComparison.Ordinal)
        || ec.StartsWith("EC-EXTERNAL-", StringComparison.Ordinal);

    /// <summary>Every Table 13 level-3 exception-name this carrier can actually raise — the ONE declaration of
    /// what a CALL site's catch arms may usefully filter on. A CALL emitter that filtered on the statement's
    /// whole enabled set instead would emit a two-hundred-way dead name test under
    /// <c>&gt;&gt;TURN EC-ALL CHECKING ON</c>. Kept honest by <c>CallExceptionCarrierDriftTests</c>, which
    /// re-derives this set from the <c>new CobolCallException(...)</c> raise sites in the runtime sources —
    /// add a raise site with a new name and the test names this list, so nothing is hand-maintained blind.
    /// <para>EC-PROGRAM-IMP is the constructor default (§14.6.13.1.1's implementor <c>*-IMP</c> level-3 row):
    /// an internal invariant failure at the activation boundary raises it with no explicit name.</para></summary>
    public static readonly string[] CarriedNames =
    [
        "EC-EXTERNAL-DATA-MISMATCH",      // §14.8.4.2 via §14.9.4.4 GR3e
        "EC-EXTERNAL-FILE-MISMATCH",      // §14.8.4.4 via §14.9.4.4 GR3e
        "EC-EXTERNAL-FORMAT-CONFLICT",    // §14.8.4.3 via §14.9.4.4 GR3e
        "EC-FUNCTION-NOT-FOUND",          // §8.4.3.2.4 GR6b — a user-defined-function locate miss
        "EC-PROGRAM-ARG-MISMATCH",        // §14.8.2.1 via §14.9.4.4 GR3d
        "EC-PROGRAM-CANCEL-ACTIVE",       // §14.9.5 GR5
        "EC-PROGRAM-IMP",                 // the ctor default (see above)
        "EC-PROGRAM-NOT-FOUND",           // §14.9.4.4 GR3b
        "EC-PROGRAM-PTR-NULL",            // §14.9.4.4 GR3b, first sentence
        "EC-PROGRAM-RECURSIVE-CALL",      // §14.9.4.4 GR3f
    ];

    /// <summary>Is <paramref name="ec"/> a name this carrier can raise (<see cref="CarriedNames"/>)?</summary>
    public static bool CanCarry(string ec) => Array.IndexOf(CarriedNames, ec) >= 0;
}

/// <summary>
/// The static facade over the run unit's <see cref="ProgramTable"/> (the emitted surface — generated run-unit
/// drivers call <c>ProgramRegistry.Reset()/Register(...)/RunMain(...)</c> and call sites emit
/// <c>CallProgram/Cancel</c>; kept name-stable pre-G8, DESIGN-runtime-library §2.1). Every member forwards to
/// <c>RunUnit.Current.Programs</c>; <see cref="Reset"/> maps to the run-unit lifecycle
/// (<see cref="RunUnit.ResetCurrent"/> — the ambient run unit is established lazily, which keeps the emitted
/// driver byte-stable).
/// </summary>
public static class ProgramRegistry
{
    /// <summary>Run-unit start: reset the ambient run unit's program/external/module state
    /// (see <see cref="RunUnit.ResetCurrent"/> — the exact pre-P8 semantics).</summary>
    public static void Reset() => RunUnit.ResetCurrent();

    /// <inheritdoc cref="ProgramTable.Register"/>
    public static void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory,
        Action? staticReset = null,
        int formalCount = -1, int requiredCount = 0, bool argMismatchChecking = false,
        bool isFunction = false)
        => RunUnit.Current.Programs.Register(path, name, parentPath, initial, common, recursive, factory,
            staticReset, formalCount, requiredCount, argMismatchChecking, isFunction);

    /// <inheritdoc cref="ProgramTable.RunMain"/>
    public static void RunMain(string path) => RunUnit.Current.Programs.RunMain(path);

    /// <inheritdoc cref="ProgramTable.CallProgram"/>
    public static void CallProgram(string name, string callerPath, CobolArg[] args, ManagedPointer? returning,
        bool siteHandlesPropagation = false, string notFoundEc = "EC-PROGRAM-NOT-FOUND",
        bool siteArgMismatchChecking = false)
        => RunUnit.Current.Programs.CallProgram(name, callerPath, args, returning, siteHandlesPropagation,
            notFoundEc, siteArgMismatchChecking);

    /// <inheritdoc cref="ProgramTable.Cancel"/>
    public static void Cancel(string name, string callerPath) => RunUnit.Current.Programs.Cancel(name, callerPath);

    /// <inheritdoc cref="ProgramTable.EntryOf"/>
    public static ProgramPointer EntryOf(string name, out bool notFound)
        => RunUnit.Current.Programs.EntryOf(name, out notFound);

    /// <inheritdoc cref="ProgramTable.CallPointer"/>
    public static void CallPointer(ProgramPointer target, string callerPath, CobolArg[] args,
        ManagedPointer? returning, bool siteHandlesPropagation = false)
        => RunUnit.Current.Programs.CallPointer(target, callerPath, args, returning, siteHandlesPropagation);
}
