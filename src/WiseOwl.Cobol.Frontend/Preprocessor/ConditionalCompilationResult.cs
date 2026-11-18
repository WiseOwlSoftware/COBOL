// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>One point at which the conditional-compilation driver READS the state it holds, in encounter order (kb/Work
/// PB1066): every <c>&gt;&gt;</c> line it meets and, while a source listing is produced, the start and the end of each
/// library text it incorporates — §2023:7.2.3.4 GR5 reads the LISTING state "in effect for the COPY statement itself" at
/// the start and puts it back at the end, so an implicit op standing before either is applied there, not at the next
/// directive (kb/Work PB5925 review R1-1). Encounter order is resultant-text order: the driver flushes every COPY before
/// the directive that follows it.</summary>
/// <param name="Line">The line the encounter stands on — 1-based in the RESULTANT frame when it comes from
/// <see cref="ConditionalCompilationResult"/>; 0-based in the driver's own output frame while the driver runs. A library
/// text's start is its first line, its end the line after its last (the COPY expansion begins and ends a line).</param>
/// <param name="ChangesState">True when the directive, in an emitting branch, changed state a PUSH saves and this
/// driver holds (§2023:7.3.22.4 GR2): a DEFINE, a FLAG-02 / FLAG-14 toggle, a LISTING directive while a listing is
/// produced, or a written PUSH / POP. Never true for a library text's start or end.</param>
public readonly record struct StateEncounter(int Line, bool ChangesState);

/// <summary>A PUSH/POP op the driver applies immediately before its encounter
/// <paramref name="BeforeEncounter"/> (<see cref="StateEncounter"/> index; the encounter count when no encounter
/// follows it).</summary>
public readonly record struct KeyedDirectiveOp(int BeforeEncounter, DirectiveStackOp Op);

/// <summary>
/// The conditional-compilation driver's product (<see cref="ConditionalCompilationProcessor.Manipulate"/>): the
/// resultant text and every state encounter, placed in the resultant frame — what the front end needs to carry
/// §2023:14.9.28.4 GR14's implicit PUSH ALL / POP ALL, which only the parse of this text can place, back into the state
/// the driver holds (kb/Work PB1066) — and every change a DEFINE made to the compilation-variable table, placed in
/// the same frame: the events the front end replays into the table's <see cref="DirectiveTimeline{T}"/> for the uses
/// that read a compilation variable outside conditional compilation (§2023:7.3.11.4 GR1; kb/Work PB1368).
/// </summary>
public sealed record ConditionalCompilationResult(MappedText Text, IReadOnlyList<StateEncounter> Encounters,
    IReadOnlyList<CompilationVariableEvent> CompilationVariables)
{
    /// <summary>
    /// <paramref name="ops"/> — resultant-line PUSH/POP ops in token order
    /// (<see cref="ExceptionPerformDirectiveScope.ImplicitOps"/>) — as the driver's implicit-op program: each op keyed
    /// to the first directive written on a LATER line (a directive occupies its own line, so no directive shares an
    /// op's line), keeping only the brackets that matter. A PUSH ALL / POP ALL pair enclosing no directive that
    /// changed the driver's state restores exactly the state it saved, so it is dropped; the result is therefore
    /// empty for every source whose handlers write no DEFINE, FLAG-02 / FLAG-14 or PUSH / POP, which is what lets
    /// the front end skip re-running the driver. Equal programs mean equal driver behaviour — the front end's
    /// fixed-point test.
    /// </summary>
    public IReadOnlyList<KeyedDirectiveOp> KeyImplicitOps(IReadOnlyList<DirectiveStackOp> ops)
    {
        if (ops.Count == 0 || Encounters.Count == 0) return [];
        var keyed = new KeyedDirectiveOp[ops.Count];
        for (int i = 0; i < ops.Count; i++) keyed[i] = new KeyedDirectiveOp(FirstEncounterAfter(ops[i].Line), ops[i]);

        // Pair each POP with its PUSH (the ops nest: every exception-checking PERFORM contributes a PUSH then a POP,
        // in token order) and keep the pair only when a state-changing directive lies between them.
        // changesBefore[k] = how many of the first k encounters changed the driver's state (one pass, so every
        // pair's test below is O(1)).
        var changesBefore = new int[Encounters.Count + 1];
        for (int k = 0; k < Encounters.Count; k++) changesBefore[k + 1] = changesBefore[k] + (Encounters[k].ChangesState ? 1 : 0);
        var keep = new bool[keyed.Length];
        var open = new Stack<int>();
        for (int i = 0; i < keyed.Length; i++)
        {
            if (keyed[i].Op.Kind == DirectiveStackKind.Push) { open.Push(i); continue; }
            if (open.Count == 0)
                throw new InvalidOperationException(
                    "§2023:14.9.28.4 GR14 implicit POP ALL without its PUSH ALL — ExceptionPerformDirectiveScope emits them in pairs");
            int push = open.Pop();
            if (changesBefore[keyed[i].BeforeEncounter] > changesBefore[keyed[push].BeforeEncounter])
                keep[push] = keep[i] = true;
        }
        if (open.Count > 0)
            throw new InvalidOperationException(
                "§2023:14.9.28.4 GR14 implicit PUSH ALL without its POP ALL — ExceptionPerformDirectiveScope emits them in pairs");
        return [.. keyed.Where((_, i) => keep[i])];
    }

    /// <summary>The index of the first encounter on a line after <paramref name="line"/>.</summary>
    private int FirstEncounterAfter(int line)
    {
        int lo = 0, hi = Encounters.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (Encounters[mid].Line <= line) lo = mid + 1; else hi = mid;
        }
        return lo;
    }
}
