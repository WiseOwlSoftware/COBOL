// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE RUN UNIT'S COMMIT AND ROLLBACK (ISO §9.1.18; DESIGN-runtime-library.md 2.2.1.10, "The compiler half"; kb/Work
/// PB3338) — the one place the COMMIT and ROLLBACK statements, the activation of an APPLY COMMIT clause and §14.6.13.1.3
/// item 8's unconditional ROLLBACK meet. The FILES' half is the keyed store engine's, reached through
/// <see cref="FileRegistry.Commit"/> and <see cref="FileRegistry.Rollback"/> (kb/Work PB3337); the DATA ITEMS' half is
/// here: every activated APPLY COMMIT clause is a <see cref="Participant"/> whose emitted <c>save</c> and <c>restore</c>
/// copy the clause's data items (its identifier-1 operands and §12.4.6.3.3 SR9's file status, LINAGE and RECORD items)
/// to and from an image the runtime element keeps.
/// <para><b>Active, and deactivated.</b> A clause becomes active when its runtime element is activated (§12.4.6.3.4
/// GR5) and is a participant from then on. A CANCEL of the element, or the return of an INITIAL program (whose
/// implicit CANCEL ends the instance), RETIRES it (<see cref="Retire"/>): its data items are no longer restored
/// (§14.9.36.4 GR4/GR5) and the next COMMIT or ROLLBACK deactivates it (§14.9.7.4 GR3, §14.9.36.4 GR6). An EXTERNAL item
/// still described by another active clause is restored through that clause, which is GR4/GR5's exception.</para>
/// <para><b>Run-unit scope.</b> A COMMIT or ROLLBACK acts on every active clause of the run unit whichever element
/// executes it (§14.9.7.1, §14.9.36.1), so the statement emits a call here and nothing of its own element's clause.</para>
/// </summary>
internal sealed class TransactionControl(FileRegistry files, ProgramTable programs, SortFileTable sorts, ExceptionEngine exceptions)
{
    /// <summary>One activated APPLY COMMIT clause: the element instance that activated it, the emitted image copies,
    /// and the FILE STATUS store of the element's own files (<see cref="StoreStatus"/>).</summary>
    private sealed class Participant(object owner, Action save, Action restore, Func<string, bool> storeStatus)
    {
        public object Owner { get; } = owner;
        public Action Save { get; } = save;
        public Action Restore { get; } = restore;
        public Func<string, bool> StoreStatus { get; } = storeStatus;
        public bool Retired { get; set; }
    }

    private readonly List<Participant> _participants = [];

    /// <summary>Whether a clause is active in the run unit — §14.9.7.4 GR1 / §14.9.36.4 GR1's "no active APPLY COMMIT
    /// clause" is its negation.</summary>
    public bool AnyActive => _participants.Count > 0;

    /// <summary>§9.1.18.7's "under the control of a recursive runtime element, or … the MERGE or SORT file statements".</summary>
    private bool FlowForbidden(out string why)
    {
        if (programs.AnyRecursiveActive()) { why = "under the control of a recursive runtime element"; return true; }
        if (sorts.AnyInPhase([ProcedurePhase.Input, ProcedurePhase.Output]))
        {
            why = "under the control of a file SORT or MERGE statement";
            return true;
        }
        why = "";
        return false;
    }

    /// <summary>⛔ ACTIVATE an APPLY COMMIT clause (§12.4.6.3.4 GR5) — emitted at the activation of a runtime element that
    /// contains one, once per element instance. <paramref name="files"/> are the keyed file connectors the clause names,
    /// put under commit control here (<see cref="FileRegistry.RegisterCommitControl"/>); <paramref name="save"/> runs at
    /// once, because a ROLLBACK before the first COMMIT restores the state "at the start of the run unit" (§14.9.36.4 GR3),
    /// which for an element activated later is its state at activation. Returns false — and activates nothing — when
    /// the activation is one §12.4.6.3.4 GR3/GR4 forbids: EC-FLOW-APPLY-COMMIT is raised when checking is enabled
    /// (Table 13's row for "Invocation of a runtime element with an APPLY COMMIT clause in effect by a runtime element for
    /// which commit and rollback is not permitted"), and with checking off the clause stays inactive.</summary>
    public bool Activate(object owner, string[] keyedFiles, Action save, Action restore, Func<string, bool> storeStatus)
    {
        if (FlowForbidden(out string why))
        {
            exceptions.FlowApplyCommitError($"an APPLY COMMIT clause activated {why} (ISO §12.4.6.3.4 GR3/GR4)");
            return false;
        }
        foreach (var name in keyedFiles) files.RegisterCommitControl(name);
        save();
        _participants.Add(new Participant(owner, save, restore, storeStatus));
        return true;
    }

    /// <summary>The element instance <paramref name="owner"/> has been canceled or, an INITIAL program, has returned
    /// (§14.9.7.4 GR3, §14.9.36.4 GR4-GR6): its data items are no longer restored, and the next COMMIT or ROLLBACK
    /// deactivates its clause. Its files stay under commit control until then (a CLOSE under commit control keeps the
    /// transaction, kb/Work PB3480).</summary>
    public void Retire(object owner)
    {
        foreach (var p in _participants)
            if (ReferenceEquals(p.Owner, owner)) p.Retired = true;
    }

    /// <summary>⛔ THE COMMIT STATEMENT (§14.9.7.4). Returns the connector name of the file whose COMMIT the medium
    /// refused, with '30' already in its I-O status and in its FILE STATUS item (<see cref="Refused"/>), or null.
    /// A COMMIT §9.1.18.7 forbids raises EC-FLOW-COMMIT (GR2) or, with checking off, does nothing.
    /// GR1: with no active clause the statement is CONTINUE — the registry has no transaction and there is nothing to
    /// save. GR3: the files' changes are made permanent and their record locks released (the registry's half), and the
    /// clauses of exited initial programs and canceled elements are deactivated; GR5: the remaining clauses' data items
    /// are saved. A refused COMMIT saves nothing and deactivates nothing: §14.6.13.1.3 item 8's ROLLBACK follows it.</summary>
    public string? Commit()
    {
        if (FlowForbidden(out string why))
        {
            exceptions.FlowCommitError($"COMMIT executed {why} (ISO §14.9.7.4 GR2)");
            return null;
        }
        if (files.Commit() is { } refused) return Refused(refused);
        _participants.RemoveAll(p => p.Retired);
        foreach (var p in _participants) p.Save();
        return null;
    }

    /// <summary>⛔ THE ROLLBACK STATEMENT (§14.9.36.4). The files first (the registry's half, GR3: every change of the run
    /// unit since the last COMMIT reversed, its record locks released), then the data items of every clause that is not
    /// retired (GR3's second sentence; GR4/GR5 exclude the canceled and exited ones), then the retired clauses are
    /// deactivated (GR6). Returns the refused file's connector name as <see cref="Commit"/> does, or null; a ROLLBACK
    /// §9.1.18.7 forbids raises EC-FLOW-ROLLBACK (GR2) or, with checking off, does nothing. The data items are restored
    /// even when a file's ROLLBACK is refused: every other file was rolled back, and the program is told of the one that
    /// was not by its '30'.</summary>
    public string? Rollback()
    {
        if (FlowForbidden(out string why))
        {
            exceptions.FlowRollbackError($"ROLLBACK executed {why} (ISO §14.9.36.4 GR2)");
            return null;
        }
        string? refused = files.Rollback();
        RestoreData();
        return refused is null ? null : Refused(refused);
    }

    /// <summary>⛔ §14.6.13.1.3 ITEM 8: <i>"if there is a fatal exception condition for a file subject to an active APPLY
    /// COMMIT clause, then a ROLLBACK statement is executed unconditionally"</i> — emitted after an input-output
    /// statement's (or a refused COMMIT's or ROLLBACK's) exception processing, where checking for the condition is not
    /// enabled (an enabled one has terminated the run unit, whose §14.6.12 rollback is the termination's). Does nothing
    /// unless <paramref name="name"/>'s I-O status is fatal and the file is under commit control. The ROLLBACK it executes
    /// is the statement's, files and data items alike; its own refusal is left to the run unit's termination and the
    /// dead-owner step (a second exception processing would repeat this one without end).</summary>
    public void RollbackOnFatal(string name)
    {
        if (!files.IsCommitControlled(name) || !ExceptionCatalog.IsFatalIoStatus(files.Status(name))) return;
        files.Rollback();
        RestoreData();
    }

    /// <summary>The refused file of a COMMIT or ROLLBACK, told to the program (DESIGN-runtime-library.md 2.2.1.10, "What the
    /// program is told"; docs/CONFORMANCE.md §5, A.4.3): the registry has set '30', and the element that describes the
    /// file stores it into its FILE STATUS item. Returns the name for the statement's exception processing.</summary>
    private string Refused(string name)
    {
        foreach (var p in _participants) p.StoreStatus(name);
        return name;
    }

    /// <summary>Whether an element's FILE STATUS item received <paramref name="name"/>'s status — false when no element
    /// describing the file writes a FILE STATUS clause for it, which is DOC-A.1-103's "nothing covers this status".</summary>
    public bool HasStatusItem(string name)
    {
        foreach (var p in _participants)
            if (p.StoreStatus(name)) return true;
        return false;
    }

    private void RestoreData()
    {
        foreach (var p in _participants)
            if (!p.Retired) p.Restore();
        _participants.RemoveAll(p => p.Retired);
    }
}
