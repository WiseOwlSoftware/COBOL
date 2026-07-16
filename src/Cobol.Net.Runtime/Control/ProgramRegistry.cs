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
/// A CALL/CANCEL-machinery failure (ISO §14.9.4.4 GR3b–h): program not found (EC-PROGRAM-NOT-FOUND), a re-entry
/// of an active non-RECURSIVE program (GR3f, EC-PROGRAM-RECURSIVE-CALL), CANCEL of an active program (§14.9.5
/// GR5, EC-PROGRAM-CANCEL-ACTIVE), or a reference to an omitted argument (GR12, EC-PROGRAM-ARG-OMITTED). At
/// <c>--std 85</c> there is no EC machinery — a CALL site with an ON OVERFLOW phrase converts this to the
/// overflow branch (the 85 surface); without one, the run unit terminates loudly (abnormal termination).
/// <paramref name="ecName"/> carries the Table 13 level-3 EC-PROGRAM-* name so a CALL site compiled with
/// EC-PROGRAM checking enabled (>>TURN, §7.3.25) can set the last exception status and run the §14.9.49 F3
/// declarative selection over the precise condition.
/// </summary>
public sealed class CobolCallException(string message, string ecName = "EC-PROGRAM-IMP") : Exception(message)
{
    /// <summary>The Table 13 level-3 exception-name of this failure (uppercase).</summary>
    public string EcName { get; } = ecName;
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
        Func<ICobolProgram?, ICobolProgram> factory)
        => RunUnit.Current.Programs.Register(path, name, parentPath, initial, common, recursive, factory);

    /// <inheritdoc cref="ProgramTable.RunMain"/>
    public static void RunMain(string path) => RunUnit.Current.Programs.RunMain(path);

    /// <inheritdoc cref="ProgramTable.CallProgram"/>
    public static void CallProgram(string name, string callerPath, CobolArg[] args, ManagedPointer? returning,
        bool siteHandlesPropagation = false, string notFoundEc = "EC-PROGRAM-NOT-FOUND")
        => RunUnit.Current.Programs.CallProgram(name, callerPath, args, returning, siteHandlesPropagation, notFoundEc);

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
