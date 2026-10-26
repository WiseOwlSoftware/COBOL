// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>The procedure phase of an executing SORT/MERGE (see <see cref="SortStore.Phase"/>). <c>None</c> covers
/// every part of the statement that runs no program procedure: the USING/GIVING transfers and the sequence
/// phase — a USE declarative that runs from an implicit transfer is not in the range of either procedure.</summary>
internal enum ProcedurePhase { None, Input, Output }

/// <summary>The per-SD store of one executing SORT/MERGE statement: its records — <see cref="Work"/>, the bounded
/// external merge sort that keeps release order as the stability anchor ISO §14.9.40.4 GR3 requires (kb/Work PB3339) —
/// and the statement's procedure phase. ⛔ It holds no record collection of its own (<c>SortWorkBoundDriftTests</c>).</summary>
internal sealed class SortStore(SortMemory memory)
{
    /// <summary>The statement's records, keys and GR5 sequences (the §14.6.6 r5 snapshots), from
    /// <see cref="CobolSort.Init(string, CobolSort.Key[], CobolCollation?, CobolCollation?)"/> — a keyless, empty work
    /// until then.</summary>
    public SortWork Work { get; private set; } = new(memory, [], null, null);

    /// <summary>Begin the statement's records: <paramref name="work"/> replaces the previous statement's, whose
    /// temporary files close (and delete themselves) here.</summary>
    public void Begin(SortWork work)
    {
        Work.Dispose();
        Work = work;
    }

    public int LastReturnedLength;
    /// <summary>The extent table of the most recently RETURNed record — null when it carries none.</summary>
    public RecordExtents? LastReturnedExtents;
    /// <summary>Which procedure of the executing SORT/MERGE statement is running — the state §14.9.32.4 GR1
    /// ("within the range of an input procedure being executed by a SORT statement that references the
    /// file-name") and §14.9.34.4 GR1 ("within the range of an output procedure being executed by a MERGE or
    /// SORT statement that references file-name-1") test. The store exists only between
    /// <see cref="CobolSort.Init(string, CobolSort.Key[], CobolCollation?, CobolCollation?)"/> and <see cref="CobolSort.Close"/>, so
    /// "no store" is "no statement executing".</summary>
    public ProcedurePhase Phase;
    /// <summary>The statement's §14.9.40.4 GR17 registration (kb/Work PB1177) — set by
    /// <c>CobolSort.RegisterFormat1Sort</c> for EVERY format 1 SORT, null for a MERGE, whose rules (§14.9.24.4) carry no
    /// such sentence. Its presence is what makes the store an ACTIVE FORMAT 1 SORT to <see cref="SortFileTable.ActiveFormat1Sort"/>.</summary>
    public Format1SortRegistration? Format1;
    /// <summary>§14.9.34.4 GR3's latch: the at end condition has occurred for this file in the current output
    /// procedure, so a further RETURN is EC-SORT-MERGE-RETURN.</summary>
    public bool AtEndReached;
}

/// <summary>A format 1 SORT statement's §14.9.40.4 GR17 registration (kb/Work PB1177): "If a USE procedure invoked while
/// a format 1 SORT statement is active does not complete normally, the SORT statement is terminated."
/// <paramref name="UseScope"/> is the declarative scope (the emitted <c>__useActive</c>) of the program instance or method
/// invocation that executes the statement — null when that scope has no EC-model declaratives — and tells the statement's
/// OWN USE procedures from every other one the rule reaches (a CALLed or INVOKEd element's, a containing program's
/// GLOBAL one): only an own procedure's RESUME AT procedure-name names a procedure the statement's landing can transfer
/// to. <paramref name="Ordinal"/> orders the registrations of the run unit, so the INNERMOST active SORT is the one
/// terminated when a SORT runs inside another's procedure (§14.9.40.4 GR10/GR13 make that EC-SORT-MERGE-ACTIVE, whose
/// results are undefined when it is not checked).</summary>
internal sealed record Format1SortRegistration(object? UseScope, long Ordinal);

/// <summary>⛔ THE RUN UNIT'S SORT-MERGE FILE STORES, keyed by COBOL file-name (kb/Work PB1570; the owner is
/// <see cref="RunUnit.SortFiles"/>). A sort-merge file's store exists only while a SORT/MERGE statement of THIS run
/// unit executes (ISO §14.9.40 GR9 phases; §9 sort-merge file model), and the -ACTIVE rules (§14.9.40.4 GR10 / GR13,
/// §14.9.24.4 GR8) name the procedures of "an executing SORT/MERGE statement" — a statement of the same run unit:
/// "A run unit is an independent entity that may be executed without communicating with, or being coordinated with,
/// any other run unit except that it may communicate via messages with other run units, process files, and set and
/// test switches" (§14.6.1), and a sort-merge file is none of those. The table used to be one process-wide static
/// <c>Dictionary</c> on <see cref="CobolSort"/>, so two run units in one process (the <see cref="RunUnit.Run"/> /
/// <see cref="RunUnit.Begin"/> host shape, or two concurrent hosts) shared and mutated each other's sort files, an
/// abandoned statement's store outlived its run unit, and one run unit's procedure phase could raise
/// EC-SORT-MERGE-ACTIVE in another. A run unit executes on one logical thread (<see cref="RunUnit"/>), so the
/// table needs no lock; a new run unit is a new object, so it starts with no store.</summary>
public sealed class SortFileTable
{
    private readonly Dictionary<string, SortStore> _stores = new(CobolNames.Comparer);
    private long _ordinals;
    /// <summary>The file-name of the active format 1 SORT that a USE procedure of ANOTHER element terminated by
    /// executing EXIT PROGRAM or GOBACK (<see cref="CobolSort.UseProcedureExited"/>) — landed when that element's
    /// activation returns (<see cref="AfterActivationReturn"/>), else null.</summary>
    private string? _exitTerminated;

    /// <summary>The run unit's sort memory limit and work directory (kb/Work PB3339; docs/CONFORMANCE.md §3 D-SWK) —
    /// read from the environment ONCE, when the run unit is built, so an executing statement's limit cannot change
    /// under it. A host (or a test) may set it before the run unit's first SORT or MERGE.</summary>
    internal SortMemory Memory { get; set; } = SortMemory.FromEnvironment();

    /// <summary>The store of <paramref name="name"/>, created empty when no statement has begun on it.</summary>
    internal SortStore Get(string name)
    {
        if (!_stores.TryGetValue(name, out var s)) _stores[name] = s = new SortStore(Memory);
        return s;
    }

    /// <summary>The store of <paramref name="name"/> when a statement has begun on it, else null.</summary>
    internal SortStore? Find(string name) => _stores.GetValueOrDefault(name);

    /// <summary>Drop the store of <paramref name="name"/> — the statement ended (<see cref="CobolSort.Close"/>) — and close
    /// its temporary run files, each of which deletes itself as it closes.</summary>
    internal void Remove(string name)
    {
        if (_stores.Remove(name, out var store)) store.Work.Dispose();
        if (_exitTerminated is { } t && CobolNames.Comparer.Equals(t, name)) _exitTerminated = null;
    }

    /// <summary>Is a procedure of an executing SORT/MERGE statement of this run unit — in one of
    /// <paramref name="phases"/> — running, whatever file it names?</summary>
    internal bool AnyInPhase(ReadOnlySpan<ProcedurePhase> phases)
    {
        foreach (var store in _stores.Values)
            if (phases.Contains(store.Phase))
                return true;
        return false;
    }

    /// <summary>The next registration ordinal of this run unit (<see cref="Format1SortRegistration.Ordinal"/>).</summary>
    internal long NextOrdinal() => ++_ordinals;

    /// <summary>The run unit's ACTIVE FORMAT 1 SORT — the innermost executing statement that registered as one
    /// (<see cref="SortStore.Format1"/>), with its file-name — else null: the statement §14.9.40.4 GR17 terminates when a
    /// USE procedure invoked while it is active does not complete normally. A store exists only while its statement
    /// executes, so a registered store IS an active SORT.</summary>
    internal (string Name, Format1SortRegistration Registration)? ActiveFormat1Sort()
    {
        (string, Format1SortRegistration)? innermost = null;
        foreach (var (name, store) in _stores)
            if (store.Format1 is { } r && (innermost is not { } i || r.Ordinal > i.Item2.Ordinal))
                innermost = (name, r);
        return innermost;
    }

    /// <summary>Record that a USE procedure of an element other than the one executing the active format 1 SORT
    /// (<paramref name="name"/>) has ended in EXIT PROGRAM or GOBACK — see <see cref="CobolSort.UseProcedureExited"/>.</summary>
    internal void TerminateOnActivationReturn(string name) => _exitTerminated = name;

    /// <summary>⛔ §14.9.40.4 GR17 AT AN ACTIVATION'S RETURN (kb/Work PB1177): when a USE procedure of the returning
    /// element ended in EXIT PROGRAM or GOBACK — a completion that is not normal (§14.6.13.1.2 1)) — while a format 1
    /// SORT whose range activated the element was active, that statement is terminated now, in the activator: the
    /// return has done what a return does (the BY REFERENCE copy-out, the RETURNING delivery), and the
    /// <see cref="SortTerminationSignal"/> unwinds the rest of the range to the statement's landing as a completion
    /// that is not normal. Asked by every emitted activating site AFTER the return has been picked up completely - the
    /// RETURNING delivery and the propagation pickup of a <c>GOBACK ... RAISING</c> the same activation staged
    /// (§14.9.18.4 GR1 b); kb/Work PB3246): a CALL or function activation and an INVOKE (both
    /// <c>CallEmitter.EmitActivationReturned</c>), and the host boundary (<c>ProgramTable.CallFromHost</c>).</summary>
    internal void AfterActivationReturn() => TerminateAfterActivation(TakeActivationReturnTermination());

    /// <summary>⛔ THE REQUEST BELONGS TO THE ACTIVATION THAT LEFT IT (kb/Work PB3246, review R1-1): the file-name of the SORT
    /// the activation just returned from asked to have terminated, taken OUT of the run unit's one pending slot. An activating
    /// site that emits a propagation pickup takes it before the pickup runs, because the pickup can run the activator's own USE
    /// procedure and that procedure can activate again: left in the slot, the nested activation's return would consume this
    /// activation's request and terminate the SORT in the middle of the declarative. Null when the activation left none.</summary>
    internal string? TakeActivationReturnTermination()
    {
        var name = _exitTerminated;
        _exitTerminated = null;
        return name;
    }

    /// <summary>Terminate the SORT named by a request <see cref="TakeActivationReturnTermination"/> took (nothing for null):
    /// the <see cref="SortTerminationSignal"/> unwinds the rest of the range to the statement's landing as a completion that is
    /// not normal.</summary>
    internal static void TerminateAfterActivation(string? name)
    {
        if (name is not null) throw new SortTerminationSignal(name, DispatchResult.NotNormal);
    }
}
