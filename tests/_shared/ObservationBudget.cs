// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⛔ THE ONE BUDGET RULE for every child process a test observes (kb/Work PB3108; DESIGN-test-build-ci.md §3.10): a
/// process is bounded by the WORK it was started to do, at one harness unit (<see cref="UnitMs"/>) per serial step of
/// that work, never by one fixed number of seconds whatever it does.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS TYPE EXISTS. The observer had one budget, 120 s, sized for ONE program: compile it, or run it. The
/// conformance suite's two-runner drift test hands that budget to ONE <c>run.py</c> process that compiles and runs
/// every test of the suite (78 processes, 8 at a time, 30 s on a quiet host), so a host loaded four-fold killed it,
/// and it went RED on load alone at least eight times between 2026-10-18 and 2026-10-22 while the same test passed in
/// 38 s to 13 min elsewhere (kb/Work PB3108). A limit that a correct process meets on a quiet host and misses on a busy
/// one is a measurement of the host, not of the code (kb/Work PB3022).
/// </para>
/// <para>
/// The rule: one unit for the observed process itself (start-up, reading its inputs, writing its results), plus one
/// unit for each serial round of the child processes it runs, <c>ceil(children / parallel)</c>. A program compiled or
/// run by the observer is one process with no children: one unit. Each child is thereby given the same unit an
/// observed program gets, whichever runner starts it, and the budget grows with the work as the suite does.
/// </para>
/// <para>
/// The unit stays the design's floor for one program: 120 s, against a recorded cost of at most 9.3 s for one suite
/// test's compile AND run (run.py's <c>seconds</c>, 2026-10-22, 32 logical processors) and 7 s for the costliest
/// single-program tests the shared gate timings name in PB3108 (RelationPairShapeTests, ActivationResourcesTests),
/// so it carries a multiple of 13 to 30 for load. <c>COBOLNET_RUN_TIMEOUT_MS</c> sets it.
/// </para>
/// </remarks>
internal readonly record struct ObservationBudget
{
    /// <summary>The harness unit: the budget of one process doing one job (compile one program, run one program, run
    /// one script). Default 120 000 ms; <c>COBOLNET_RUN_TIMEOUT_MS</c> overrides it.</summary>
    public static int UnitMs { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("COBOLNET_RUN_TIMEOUT_MS"), out int ms) && ms > 0
            ? ms : 120_000;

    /// <summary>One process that starts no child it waits for: one unit.</summary>
    public static ObservationBudget OneProcess { get; } = For(UnitMs, children: 0, parallel: 1, "one process");

    /// <summary>A process that runs <paramref name="children"/> child processes, at most <paramref name="parallel"/> at a
    /// time, at the harness unit.</summary>
    public static ObservationBudget ForChildren(int children, int parallel, string work) =>
        For(UnitMs, children, parallel, work);

    /// <summary>The rule itself, at an explicit unit: <c>1 + ceil(children / parallel)</c> units of
    /// <paramref name="unitMs"/>. Only the observer's own tests choose the unit; every other caller takes
    /// <see cref="UnitMs"/> through <see cref="OneProcess"/> or <see cref="ForChildren"/>.</summary>
    internal static ObservationBudget For(int unitMs, int children, int parallel, string work)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(unitMs, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(children);
        ArgumentOutOfRangeException.ThrowIfLessThan(parallel, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(work);
        long units = 1 + ((long)children + parallel - 1) / parallel;
        return new ObservationBudget(unitMs, units, work);
    }

    private ObservationBudget(int unitMs, long units, string work)
    {
        PerUnitMs = unitMs;
        Units = units;
        Work = work;
        // Process.WaitForExit takes an int: a budget past int.MaxValue ms (24.8 days) is clamped, never wrapped.
        Ms = (int)Math.Min(int.MaxValue, units * unitMs);
    }

    /// <summary>The unit this budget was computed at.</summary>
    public int PerUnitMs { get; }

    /// <summary>The serial units of work: one for the process, one per round of its children.</summary>
    public long Units { get; }

    /// <summary>What the process was started to do, in words, for the non-observation report.</summary>
    public string Work { get; }

    /// <summary>The wall-clock budget of one attempt, in milliseconds.</summary>
    public int Ms { get; }

    /// <summary>The budget as a non-observation reports it: the total, the work, and how it was computed.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"{Ms} ms for {Work} ({Units} unit(s) of {PerUnitMs} ms; COBOLNET_RUN_TIMEOUT_MS sets the unit)");
}
