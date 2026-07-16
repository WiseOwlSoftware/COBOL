// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;

namespace CobolNet.Runtime;

/// <summary>
/// The single owner of all run-unit-lifetime state (ISO §14.6.1 run unit; DESIGN-runtime-library §2.1).
/// Replaces the five independent process-global static stores: one instance per run unit owning the program
/// table, the EC engine, the EXTERNAL store, the MODULE-NAME stack, the file registry, and the clock. The
/// ambient current run unit is an <see cref="AsyncLocal{T}"/> so a host that thread-hops or runs run units
/// concurrently each see their own (uniform threading model — the former <c>[ThreadStatic]</c>-vs-plain-static
/// split is gone). The pre-existing static facades (<see cref="ProgramRegistry"/>, <see cref="ExceptionState"/>,
/// <see cref="CobolFile"/>, <see cref="CobolModule"/>, <see cref="ExternalStore"/>) remain the emitted surface
/// as thin delegators over <see cref="Current"/>, so generated code is byte-stable pre-G8; <see cref="Current"/>
/// lazily establishes an ambient run unit, which is what makes the emitted
/// <c>ProgramRegistry.Reset(); CobolFile.Init(); … CobolFile.CloseAll()</c> run-unit driver work unchanged.
/// </summary>
public sealed class RunUnit
{
    private static readonly AsyncLocal<RunUnit?> _current = new();

    /// <summary>The ambient current run unit — lazily established if none (the delegating static facades reach
    /// state through this, so a plain generated <c>Main</c> works without ever naming <see cref="RunUnit"/>).</summary>
    public static RunUnit Current => _current.Value ??= new RunUnit();

    /// <summary>The ambient current run unit, or null when none has been established (diagnostic/host use).</summary>
    internal static RunUnit? TryCurrent => _current.Value;

    public RunUnit()
    {
        Programs = new ProgramTable(this);
    }

    /// <summary>The run-unit program registry (name resolution, §14.6.2.3 state model, CANCEL).</summary>
    public ProgramTable Programs { get; }

    /// <summary>The run-unit-wide LAST EXCEPTION STATUS register + propagation slots (§14.6.13.1.1).</summary>
    public ExceptionEngine Exceptions { get; } = new();

    /// <summary>The run-unit EXTERNAL data store (§8.6.7 / §13.18.22).</summary>
    public ExternalTable External { get; } = new();

    /// <summary>The FUNCTION MODULE-NAME call-name stack (§15.65).</summary>
    public ModuleStack Modules { get; } = new();

    /// <summary>The run-unit file-connector registry (§9.1; owns the physical-file sharing table).</summary>
    public FileRegistry Files { get; } = new();

    /// <summary>The run unit's clock (ISO §14.9.1.4 GR7; injectable — a test may set a fixed clock).</summary>
    public IClock Clock { get; set; } = SystemClock.Instance;

    /// <summary>Establish a FRESH ambient run unit for the duration of <paramref name="body"/> — the one
    /// lifecycle boundary (begin = a clean run unit; end = the §14.6 implicit CloseAll + ambient restore).
    /// The DEFAULT emitted run-unit driver does not call this (it stays on the lazy-ambient
    /// <c>ProgramRegistry.Reset()</c> path for byte-stability); hosts embedding multiple run units use it.</summary>
    public static void Run(Action<RunUnit> body)
    {
        var ru = new RunUnit();
        var prior = _current.Value;
        _current.Value = ru;
        try { body(ru); }
        finally { ru.Files.CloseAll(); _current.Value = prior; }
    }

    /// <summary>Reset the AMBIENT run unit's program/external/module state — the exact semantics of the
    /// pre-P8 <c>ProgramRegistry.Reset()</c> (clear registrations + EXTERNAL store + MODULE-NAME stack; files
    /// and the last-exception status reset through their own emitted entry points, exactly as before). Called
    /// by the <see cref="ProgramRegistry"/> shim from the emitted run-unit driver.</summary>
    public static void ResetCurrent() => Current.Programs.Reset();
}
