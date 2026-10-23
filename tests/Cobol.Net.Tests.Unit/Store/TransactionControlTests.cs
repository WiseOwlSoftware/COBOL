// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using CobolNet.Tests.Shared;
using Xunit;
using static CobolNet.Tests.Shared.FileHarness;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE RUN UNIT'S COMMIT AND ROLLBACK OVER THE DATA ITEMS AND THE FILES TOGETHER (<see cref="TransactionControl"/>;
/// DESIGN-runtime-library.md 2.2.1.10, "The compiler half", in docs/rearchitecture; kb/Work PB3338), driven as the
/// emitted code drives it: a real <see cref="RunUnit"/>, real keyed connectors on real host files, and each store's pages
/// through a <see cref="RefusingPageMedium"/> (2.2.1.12's fault injection at the connector's level) whose seam logs and
/// refuses the operation a case names. The files' own regions are <see cref="TransactionFaultMatrixTests"/>' (kb/Work
/// PB3337); these cases pin what the statements add over them: the data items' image, what the program is told of a
/// refusal, §14.6.13.1.3 item 8, the EC-FLOW conditions and the retirement of a clause.
/// <para>Expected values are the standard's:
/// §14.9.7.4 GR1 / §14.9.36.4 GR1 (<i>"If this statement is executed when there is no active APPLY COMMIT clause, then it
/// has the same effect as a CONTINUE statement with no additional phrases."</i>);
/// §14.9.7.4 GR5 (<i>"The contents of all data-items referenced in the remaining active APPLY COMMIT clauses are saved
/// for potential use in a subsequent rollback."</i>);
/// §14.9.36.4 GR3 (<i>"All files and data-items referenced in active APPLY COMMIT clauses are restored to the state they
/// were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had
/// previously been executed."</i>);
/// §14.6.13.1.3 item 8 (<i>"if there is a fatal exception condition for a file subject to an active APPLY COMMIT clause,
/// then a ROLLBACK statement is executed unconditionally"</i>);
/// §9.1.13.6 item 1 ('30': <i>"A permanent error exists and no further information is available concerning the
/// input-output operation."</i>); and §9.1.18.7's EC-FLOW-COMMIT and EC-FLOW-ROLLBACK, raised for a statement executed
/// while a SORT or MERGE file statement is active (Table 13: fatal).</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class TransactionControlTests
{
    public static TheoryData<FileOrg> Orgs() => [.. KeyedOrgs];

    private static readonly IOException Refusal = new("refused by the host");

    /// <summary>One activated APPLY COMMIT clause as the emitted program class holds it: its data item, the image of it
    /// <c>__CommitSave</c> keeps, and the FILE STATUS item <c>__CommitStatus</c> stores a refused file's status into.</summary>
    private sealed class Clause(FileRegistry reg, bool hasStatusItem = true)
    {
        public string Item { get; set; } = "INITIAL";
        public string Image { get; private set; } = "";
        public int Saves { get; private set; }
        public Dictionary<string, string> StatusItems { get; } = [];

        public void Save() { Image = Item; Saves++; }
        public void Restore() => Item = Image;

        public bool StoreStatus(string file)
        {
            if (!hasStatusItem) return false;
            StatusItems[file] = reg.Status(file);
            return true;
        }
    }

    /// <summary>One run unit with its files, each a host file seeded with records 1 to 5 (<c>nnnn</c> + <c>Annn</c>), every
    /// page operation logged on its file's seam as <c>R&lt;page&gt;</c>, <c>W&lt;page&gt;</c> or <c>S</c>.</summary>
    private sealed class Run(FileOrg org) : IDisposable
    {
        public RunUnit Unit { get; } = new();
        public FileRegistry Reg => Unit.Files;
        public TransactionControl Txn => Unit.Transactions;
        public FileOrg Org => org;
        public Dictionary<string, string> Hosts { get; } = [];
        public Dictionary<string, List<string>> Log { get; } = [];

        /// <summary>The refusal the host answers on file f's n-th operation (an index into its log), or null.</summary>
        public Func<string, int, string, Exception?>? Refuse { get; set; }

        /// <summary>Declare connector <paramref name="name"/> over a new seeded host file (the per-file registration of
        /// <c>__Activate</c>), sharing ALL OTHER, LOCK MODE MANUAL.</summary>
        public void Declare(string name)
        {
            string host = Tmp($"pb3338-txn-{name}-{org}");
            Hosts[name] = host;
            Log[name] = [];
            Seed(host, org, ["A001", "A002", "A003", "A004", "A005"]);
            Register(Reg, name, host, org);
        }

        /// <summary>OPEN I-O <paramref name="name"/>, its store's pages through the refusing seam.</summary>
        public void OpenIO(string name)
        {
            var log = Log[name];
            KeyedStore.MediumObserver.Value = m => new RefusingPageMedium(m,
                write: p => Seam(name, log, $"W{p}"), sync: () => Seam(name, log, "S"), read: p => Seam(name, log, $"R{p}"));
            try { FileHarness.Open(Reg, name, FileOpenMode.IO); }
            finally { KeyedStore.MediumObserver.Value = null; }
            Assert.Equal("00", Reg.Status(name));
        }

        private Exception? Seam(string file, List<string> log, string op)
        {
            int n = log.Count;
            log.Add(op);
            return Refuse?.Invoke(file, n, op);
        }

        /// <summary>The emitted activation: <c>__CommitSave</c>, <c>__CommitRestore</c> and <c>__CommitStatus</c> of
        /// <paramref name="clause"/>, for the keyed files the clause names.</summary>
        public bool Activate(object owner, Clause clause, params string[] files) =>
            Txn.Activate(owner, files, clause.Save, clause.Restore, clause.StoreStatus);

        public string Write(int n, string tail, string name = "F") => FileHarness.Write(Reg, name, org, n, tail);
        public string Rewrite(int n, string tail, string name = "F") => FileHarness.Rewrite(Reg, name, org, n, tail);
        public string Delete(int n, string name = "F") => FileHarness.Delete(Reg, name, org, n);

        /// <summary>Records 1 to 9 as a fresh run unit's OPEN INPUT reads them (absent: null).</summary>
        public string?[] Contents(string name = "F")
        {
            var reader = new FileRegistry();
            Register(reader, "R", Hosts[name], org);
            FileHarness.Open(reader, "R", FileOpenMode.Input);
            var records = new string?[10];
            for (int n = 1; n <= 9; n++)
                records[n] = Read(reader, "R", org, n, out var image) == "00" ? image : null;
            reader.CloseAll();
            return records;
        }

        public void Dispose()
        {
            try { Reg.CloseAll(); }
            finally
            {
                foreach (var host in Hosts.Values) TryDelete(host);
            }
        }
    }

    private static string?[] Seeded()
    {
        var expected = new string?[10];
        for (int n = 1; n <= 5; n++) expected[n] = Rec(n, $"A{n:000}");
        return expected;
    }

    private static string?[] With(string?[] records, int n, string? tail)
    {
        var copy = (string?[])records.Clone();
        copy[n] = tail is null ? null : Rec(n, tail);
        return copy;
    }

    /// <summary>The run unit's transaction on <paramref name="name"/>: it DELETEs record 5, REWRITEs record 1 and WRITEs
    /// record 7.</summary>
    private static void Change(Run r, string name = "F")
    {
        Assert.Equal("00", r.Delete(5, name));
        Assert.Equal("00", r.Rewrite(1, "X001", name));
        Assert.Equal("00", r.Write(7, "A007", name));
    }

    private static string?[] Changed() => With(With(With(Seeded(), 5, null), 1, "X001"), 7, "A007");

    /// <summary>§14.6.13.1.3 item 8 as the emitted hook drives it (<c>SequentialIoEmitter.Item8Applies</c>): the question
    /// asked at the raise, under the statement's checking for the raised condition, and the ROLLBACK when it is due.</summary>
    private static void Item8(Run r, string name, bool checkingEnabled = false)
    {
        if (r.Txn.RollbackDueOnFatal(name, checkingEnabled)) r.Txn.RollbackOnFatal();
    }

    // ── The data items' image (§14.9.7.4 GR5, §14.9.36.4 GR3) ─────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_rollback_before_any_commit_restores_the_items_as_at_activation_and_the_files_as_at_the_start(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        Assert.True(r.Activate(this, clause, "F"));
        Assert.Equal(1, clause.Saves);              // saved at activation: the state "at the start of the run unit"
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        Assert.Null(r.Txn.Rollback());
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Seeded(), r.Contents());
    }

    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_commit_saves_the_items_and_a_later_rollback_restores_them_with_the_files(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "ONE";
        Assert.Null(r.Txn.Commit());
        clause.Item = "TWO";
        Assert.Equal("00", r.Write(8, "A008"));
        Assert.Null(r.Txn.Rollback());
        Assert.Equal("ONE", clause.Item);
        Assert.Equal(Changed(), r.Contents());
    }

    /// <summary>GR1 of both statements: with no active clause each is CONTINUE — even where §9.1.18.7 would forbid it,
    /// because GR1 is asked before GR2 (EC-FLOW-COMMIT is not raised with checking on).</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void With_no_active_clause_commit_and_rollback_are_continue(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        r.OpenIO("F");
        Assert.Equal("00", r.Write(7, "A007"));
        r.Unit.SortFiles.Get("SD1").Phase = ProcedurePhase.Input;
        r.Unit.Exceptions.FlowCommitChecking = true;
        r.Unit.Exceptions.FlowRollbackChecking = true;
        Assert.False(r.Txn.AnyActive);
        Assert.Null(r.Txn.Rollback());
        Assert.Null(r.Txn.Commit());
        Assert.Equal(With(Seeded(), 7, "A007"), r.Contents());   // the WRITE stands: there was nothing to roll back
    }

    // ── A refused COMMIT at every operation: 2.2.1.10's three regions, as the program sees them ────────────────────

    /// <summary>The COMMIT refused at every page operation it makes. Before the slot write's sync (2.2.1.10's first two
    /// regions) it answers the refused file, '30' in its I-O status and in the FILE STATUS item, and saves NOTHING, so
    /// item 8's ROLLBACK restores the items as at the activation and the file as at its start; refused after it, the
    /// COMMIT stands and answers success. The case is run with a FILE STATUS item and without one
    /// (<see cref="TransactionControl.HasStatusItem"/> false: DOC-A.1-103's "nothing covers this status").</summary>
    [Theory]
    [MemberData(nameof(OrgsWithAndWithoutStatus))]
    public void A_commit_refused_at_every_operation_is_told_and_rolled_back_by_item_8(FileOrg org, bool statusItem)
    {
        List<string> ops;
        using (var m = new Run(org))
        {
            m.Declare("F");
            m.Activate(this, new Clause(m.Reg), "F");
            m.OpenIO("F");
            Change(m);
            int before = m.Log["F"].Count;
            Assert.Null(m.Txn.Commit());
            ops = m.Log["F"].Skip(before).ToList();
        }
        int slot = ops.FindIndex(op => op is "W0" or "W1"), sync2 = ops.FindIndex(Math.Max(slot, 0), op => op == "S");
        Assert.True(slot >= 0 && sync2 > slot, string.Join(' ', ops));
        for (int k = 0; k < ops.Count; k++)
        {
            using var r = new Run(org);
            r.Declare("F");
            var clause = new Clause(r.Reg, statusItem);
            r.Activate(this, clause, "F");
            r.OpenIO("F");
            Change(r);
            clause.Item = "AT-COMMIT";
            int at = r.Log["F"].Count + k;
            r.Refuse = (_, n, _) => n == at ? Refusal : null;
            string? refused = r.Txn.Commit();
            r.Refuse = null;
            if (k > sync2)
            {
                Assert.Null(refused);
                clause.Item = "LATER";
                Assert.Null(r.Txn.Rollback());
                Assert.Equal("AT-COMMIT", clause.Item);     // GR5: the COMMIT saved the items
                Assert.Equal(Changed(), r.Contents());
                continue;
            }
            Assert.Equal("F", refused);
            Assert.Equal("30", r.Reg.Status("F"));
            Assert.Equal(statusItem, r.Txn.HasStatusItem("F"));
            Assert.Equal(statusItem ? "30" : null, clause.StatusItems.GetValueOrDefault("F"));
            Item8(r, "F");                                   // §14.6.13.1.3 item 8
            Assert.Equal("INITIAL", clause.Item);            // the refused COMMIT saved nothing
            Assert.Equal(Seeded(), r.Contents());
        }
    }

    public static TheoryData<FileOrg, bool> OrgsWithAndWithoutStatus()
    {
        var data = new TheoryData<FileOrg, bool>();
        foreach (var org in KeyedOrgs)
        {
            data.Add(org, true);
            data.Add(org, false);
        }
        return data;
    }

    /// <summary>The controls §14.9.36.4 GR2 forbids a ROLLBACK under, as the run unit records each one.</summary>
    public enum Item8Control { InputProcedure, OutputProcedure, RecursiveElement }

    public static TheoryData<FileOrg, Item8Control, bool> Item8ControlsWithAndWithoutChecking()
    {
        var data = new TheoryData<FileOrg, Item8Control, bool>();
        foreach (var org in KeyedOrgs)
            foreach (var control in Enum.GetValues<Item8Control>())
            {
                data.Add(org, control, true);
                data.Add(org, control, false);
            }
        return data;
    }

    /// <summary>A registered runtime element whose activation runs <paramref name="body"/>.</summary>
    private sealed class Body(Action body) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) => body();
        public void Activate() { }
        public void CloseFiles() { }
    }

    /// <summary>A COMMIT over two files is refused at the SECOND (files commit one at a time, in the order of the run
    /// unit's open stores: the owner's per-file decision of 2026-10-19, kb/Work PB3454, docs/CONFORMANCE.md A.4.3): the
    /// first file stays committed, the second is at the previous COMMIT once item 8's ROLLBACK has run, and the items,
    /// which the refused COMMIT never saved, are restored.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_two_file_commit_refused_at_the_second_file_leaves_the_first_committed(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        r.Declare("G");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F", "G");
        r.OpenIO("F");
        r.OpenIO("G");
        Change(r, "F");
        Change(r, "G");
        clause.Item = "AT-COMMIT";
        r.Refuse = (file, _, op) => file == "G" && op[0] == 'W' ? Refusal : null;
        Assert.Equal("G", r.Txn.Commit());
        r.Refuse = null;
        Assert.Equal("00", r.Reg.Status("F"));
        Assert.Equal("30", r.Reg.Status("G"));
        Assert.Equal("30", clause.StatusItems["G"]);
        Item8(r, "G");
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Changed(), r.Contents("F"));
        Assert.Equal(Seeded(), r.Contents("G"));
    }

    /// <summary>A ROLLBACK the medium refuses answers the refused file with '30', and the items are restored anyway: every
    /// other file was rolled back, and the program is told of the one that was not by its status. A ROLLBACK that is not
    /// refused then finishes the file's.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_refused_rollback_is_told_and_still_restores_the_items(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        r.Refuse = (_, _, op) => op[0] == 'W' ? Refusal : null;
        Assert.Equal("F", r.Txn.Rollback());
        r.Refuse = null;
        Assert.Equal("30", r.Reg.Status("F"));
        Assert.Equal("30", clause.StatusItems["F"]);
        Assert.Equal("INITIAL", clause.Item);
        Assert.Null(r.Txn.Rollback());
        Assert.Equal(Seeded(), r.Contents());
    }

    // ── §14.6.13.1.3 item 8 after an input-output statement ──────────────────────────────────────────────────────

    /// <summary>A WRITE refused by the medium ('30', a fatal I-O status) on a file under commit control: item 8's
    /// unconditional ROLLBACK undoes the transaction's earlier changes and restores the items. On a file no active clause
    /// names, the same refusal rolls nothing back.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Item_8_rolls_back_after_a_refused_write_only_on_a_file_under_commit_control(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        r.Declare("H");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        r.OpenIO("H");
        Change(r);
        clause.Item = "CHANGED";
        r.Refuse = (_, _, op) => op[0] == 'W' ? Refusal : null;
        Assert.Equal("30", r.Write(9, "A009", "H"));
        r.Refuse = null;
        Item8(r, "H");                                       // H is not under commit control: nothing happens
        Assert.Equal("CHANGED", clause.Item);
        r.Refuse = (_, _, op) => op[0] == 'W' ? Refusal : null;
        Assert.Equal("30", r.Write(9, "A009"));
        r.Refuse = null;
        Item8(r, "F");
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Seeded(), r.Contents());
    }

    /// <summary>A non-fatal status ('23' on a READ of an absent record, '22' on a duplicate WRITE) runs no ROLLBACK.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Item_8_does_nothing_after_a_status_that_is_not_fatal(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        Assert.Equal("22", r.Write(2, "A002"));
        Item8(r, "F");
        Assert.Equal("CHANGED", clause.Item);
        Assert.Null(r.Txn.Commit());
        Assert.Equal(Changed(), r.Contents());
    }

    /// <summary>kb/Work PB3543: item 8 opens <i>"If checking for the exception condition is not enabled"</i>; with checking
    /// enabled items 4-7 decide (a RESUME continues, item 5 NOTE 2, with no ROLLBACK). A refused WRITE ('30', fatal) under
    /// enabled checking is therefore not due, and the transaction stands for the program's own ROLLBACK.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void Item_8_is_not_due_when_checking_for_the_condition_is_enabled(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        r.Refuse = (_, _, op) => op[0] == 'W' ? Refusal : null;
        Assert.Equal("30", r.Write(9, "A009"));
        r.Refuse = null;
        Assert.False(r.Txn.RollbackDueOnFatal("F", checkingEnabled: true));
        Assert.True(r.Txn.RollbackDueOnFatal("F", checkingEnabled: false));   // the same status, checking off: due
        Item8(r, "F", checkingEnabled: true);
        Assert.Equal("CHANGED", clause.Item);
        Assert.Null(r.Txn.Rollback());
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Seeded(), r.Contents());
    }

    /// <summary>kb/Work PB3544: item 8's ROLLBACK is <i>"a ROLLBACK statement"</i>, so §14.9.36.4 GR2 holds for it: <i>"If
    /// this statement is attempted to be executed under the control of a recursive runtime element or a file SORT or MERGE
    /// statement, then the exception condition EC-FLOW-ROLLBACK is set to exist."</i> A fatal status on a file under commit
    /// control under each control GR2 names raises EC-FLOW-ROLLBACK with checking on and, with it off, rolls nothing
    /// back; the transaction stands for the program's ROLLBACK once the control has ended. The controls are a SORT's input
    /// procedure, an output procedure (the run unit records the procedure's phase, which a SORT's and a MERGE's output
    /// procedure share; the MERGE end to end is the golden <c>pb3544_item8_rollback_in_merge_output_procedure_ec_flow</c>)
    /// and a recursive runtime element in the call chain (end to end:
    /// <c>pb3544_item8_rollback_under_recursive_element_unchecked</c>). Checking is set inside the control, because an
    /// activation starts its element's checking from all off.</summary>
    [Theory]
    [MemberData(nameof(Item8ControlsWithAndWithoutChecking))]
    public void Item_8_under_a_forbidding_control_raises_ec_flow_rollback_or_rolls_nothing_back(
        FileOrg org, Item8Control control, bool checking)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        r.Refuse = (_, _, op) => op[0] == 'W' ? Refusal : null;
        Assert.Equal("30", r.Write(9, "A009"));
        r.Refuse = null;
        Exception? raised = null;
        bool ran = false;
        void Item8UnderTheControl()
        {
            r.Unit.Exceptions.FlowRollbackChecking = checking;
            Assert.True(r.Txn.RollbackDueOnFatal("F", checkingEnabled: false));
            raised = Record.Exception(r.Txn.RollbackOnFatal);
            Assert.Equal("CHANGED", clause.Item);            // nothing was rolled back under the control
            ran = true;
        }
        switch (control)
        {
            case Item8Control.InputProcedure:
            case Item8Control.OutputProcedure:
                r.Unit.SortFiles.Get("SD1").Phase =
                    control == Item8Control.InputProcedure ? ProcedurePhase.Input : ProcedurePhase.Output;
                Item8UnderTheControl();
                r.Unit.SortFiles.Remove("SD1");
                break;
            case Item8Control.RecursiveElement:
                r.Unit.Programs.RegisterModule("Cobol.R.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi,
                    () => r.Unit.Programs.Register("R", "R", null, initial: false, common: false, recursive: true,
                        _ => new Body(Item8UnderTheControl)));
                r.Unit.Programs.CallProgram("R", "", [], null, "EC-PROGRAM-NOT-FOUND");
                break;
        }
        Assert.True(ran);
        if (checking)
            Assert.Equal("EC-FLOW-ROLLBACK", Assert.IsType<CobolFatalException>(raised).EcName);
        else
            Assert.Null(raised);
        Assert.Equal("CHANGED", clause.Item);
        Assert.Null(r.Txn.Rollback());
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Seeded(), r.Contents());
    }

    // ── §9.1.18.7: EC-FLOW-COMMIT, EC-FLOW-ROLLBACK and EC-FLOW-APPLY-COMMIT under an active SORT or MERGE ──────────

    /// <summary>With checking enabled the statement raises its fatal condition and changes nothing; with checking off it
    /// is not executed (docs/CONFORMANCE.md A.4.3): the transaction and the items stay as they were.</summary>
    [Theory]
    [MemberData(nameof(OrgsWithAndWithoutStatus))]
    public void Commit_and_rollback_under_an_active_sort_raise_ec_flow_or_change_nothing(FileOrg org, bool checking)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Activate(this, clause, "F");
        r.OpenIO("F");
        Change(r);
        clause.Item = "CHANGED";
        r.Unit.SortFiles.Get("SD1").Phase = ProcedurePhase.Output;
        r.Unit.Exceptions.FlowCommitChecking = checking;
        r.Unit.Exceptions.FlowRollbackChecking = checking;
        if (checking)
        {
            Assert.Equal("EC-FLOW-COMMIT", Assert.Throws<CobolFatalException>(() => r.Txn.Commit()).EcName);
            Assert.Equal("EC-FLOW-ROLLBACK", Assert.Throws<CobolFatalException>(() => r.Txn.Rollback()).EcName);
        }
        else
        {
            Assert.Null(r.Txn.Commit());
            Assert.Null(r.Txn.Rollback());
        }
        Assert.Equal(1, clause.Saves);                       // neither statement ran
        Assert.Equal("CHANGED", clause.Item);
        r.Unit.SortFiles.Remove("SD1");
        Assert.Null(r.Txn.Rollback());                       // the transaction was still there to roll back
        Assert.Equal("INITIAL", clause.Item);
        Assert.Equal(Seeded(), r.Contents());
    }

    /// <summary>§12.4.6.3.4 GR3/GR4: a clause activated under an active SORT or MERGE raises EC-FLOW-APPLY-COMMIT with
    /// the ACTIVATING element's checking on; with it off the clause stays inactive and its file is not put under commit
    /// control. The activated element's own scope is handed back either way.</summary>
    [Theory]
    [MemberData(nameof(OrgsWithAndWithoutStatus))]
    public void An_activation_under_an_active_sort_raises_ec_flow_apply_commit_or_stays_inactive(FileOrg org, bool checking)
    {
        using var r = new Run(org);
        r.Declare("F");
        var clause = new Clause(r.Reg);
        r.Unit.SortFiles.Get("SD1").Phase = ProcedurePhase.Input;
        r.Unit.Exceptions.ActivatorFlowApplyCommit = checking;   // the activating CALL's checking, latched
        if (checking)
            Assert.Equal("EC-FLOW-APPLY-COMMIT",
                Assert.Throws<CobolFatalException>(() => r.Activate(this, clause, "F")).EcName);
        else
            Assert.False(r.Activate(this, clause, "F"));
        Assert.False(r.Txn.AnyActive);
        Assert.False(r.Reg.IsCommitControlled("F"));
        Assert.Equal(0, clause.Saves);
        Assert.False(r.Unit.Exceptions.FlowApplyCommitChecking);
    }

    // ── Retirement: a canceled element or a returned INITIAL program (§14.9.7.4 GR3, §14.9.36.4 GR4-GR6) ──────────

    /// <summary>A retired clause's items are no longer restored, and the next COMMIT or ROLLBACK deactivates it, while
    /// another element's active clause is restored and saved as before.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_retired_clause_is_not_restored_and_the_next_statement_deactivates_it(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        object canceled = new(), live = new();
        var gone = new Clause(r.Reg);
        var kept = new Clause(r.Reg);
        r.Activate(canceled, gone, "F");
        r.Activate(live, kept, "F");
        r.OpenIO("F");
        gone.Item = "GONE-CHANGED";
        kept.Item = "KEPT-CHANGED";
        r.Txn.Retire(canceled);
        Assert.Null(r.Txn.Rollback());
        Assert.Equal("GONE-CHANGED", gone.Item);             // GR4/GR5: the canceled element's items are not restored
        Assert.Equal("INITIAL", kept.Item);
        Assert.True(r.Reg.IsCommitControlled("F"));          // the live clause still names F
        Assert.Null(r.Txn.Commit());
        Assert.Equal(1, gone.Saves);                         // GR6: deactivated by the ROLLBACK, never saved again
        Assert.Equal(2, kept.Saves);
        Assert.True(r.Txn.AnyActive);
        r.Txn.Retire(live);
        Assert.Null(r.Txn.Commit());                         // §14.9.7.4 GR3: the COMMIT deactivates the last clause
        Assert.False(r.Txn.AnyActive);
        Assert.Equal(2, kept.Saves);
        Assert.False(r.Reg.IsCommitControlled("F"));         // kb/Work PB3541: no active clause names F any more
    }

    /// <summary>⛔ kb/Work PB3541 — A FILE WHOSE LAST CLAUSE IS DEACTIVATED LEAVES COMMIT CONTROL. §14.9.7.4 GR3 (<i>"The COMMIT
    /// statement also deactivates any APPLY COMMIT clauses in exited initial programs and canceled runtime elements"</i>) and
    /// §14.9.36.4 GR6 for the ROLLBACK end the clause, and §12.4.6.3.4 GR5 makes it active only until then; so F's changes
    /// after the deactivating statement are no ROLLBACK's (§14.9.36.4 GR3 restores <i>"All files and data-items referenced
    /// in active APPLY COMMIT clauses"</i>) and a fatal status on F runs no §14.6.13.1.3 item 8 ROLLBACK, while another
    /// element's active clause, on file G, keeps the run unit's commit control alive. Both deactivating statements are
    /// cases: the COMMIT (<paramref name="byCommit"/>) and the ROLLBACK.</summary>
    [Theory]
    [MemberData(nameof(OrgsByStatement))]
    public void A_file_whose_last_clause_is_deactivated_is_no_rollbacks_and_no_item_8s(FileOrg org, bool byCommit)
    {
        using var r = new Run(org);
        r.Declare("F");
        r.Declare("G");
        object canceled = new(), live = new();
        var gone = new Clause(r.Reg);
        var kept = new Clause(r.Reg);
        Assert.True(r.Activate(canceled, gone, "F"));
        Assert.True(r.Activate(live, kept, "G"));
        r.OpenIO("F");
        r.OpenIO("G");
        r.Txn.Retire(canceled);
        Assert.Null(byCommit ? r.Txn.Commit() : r.Txn.Rollback());
        Assert.False(r.Reg.IsCommitControlled("F"));
        Assert.True(r.Reg.IsCommitControlled("G"));
        Assert.True(r.Txn.AnyActive);
        Change(r);                                           // F's changes, made after the deactivation
        Assert.Equal("00", r.Write(8, "A008", "G"));         // G's change, under its active clause
        kept.Item = "CHANGED";
        r.Refuse = (file, _, op) => file == "F" && op[0] == 'W' ? Refusal : null;
        Assert.Equal("30", r.Write(9, "A009"));              // a fatal status on F
        r.Refuse = null;
        Item8(r, "F");                                       // F is subject to no active clause: no ROLLBACK
        Assert.Equal("CHANGED", kept.Item);
        Assert.Null(r.Txn.Rollback());
        Assert.Equal(Changed(), r.Contents("F"));            // F is no ROLLBACK's: every change of it survives
        Assert.Equal(Seeded(), r.Contents("G"));             // G is rolled back
        Assert.Equal("INITIAL", kept.Item);
    }

    /// <summary>⛔ kb/Work PB3541 — THE DEACTIVATION PUTS THE FILE CONTROL ENTRY'S OWN LOCK POSTURE BACK. Under commit control
    /// §9.1.16 applies <i>"an implicit LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS"</i>, so a READ locks its record
    /// (§12.4.5.9.4 GR4) and another connector's READ of it answers '51'. Once the clause naming F is deactivated F is no
    /// longer a file subject to commit and rollback, and its entry's LOCK MODE IS MANUAL holds again: a READ without the
    /// WITH LOCK phrase locks nothing, and the other connector reads the record '00'.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_deactivated_clause_gives_the_file_its_declared_lock_posture_back(FileOrg org)
    {
        using var r = new Run(org);
        r.Declare("F");
        Register(r.Reg, "F2", r.Hosts["F"], org);
        object canceled = new();
        var gone = new Clause(r.Reg);
        Assert.True(r.Activate(canceled, gone, "F"));
        r.OpenIO("F");
        FileHarness.Open(r.Reg, "F2", FileOpenMode.IO);
        Assert.Equal("00", Read(r.Reg, "F", org, 2, out _));
        Assert.Equal("51", Read(r.Reg, "F2", org, 2, out _));   // AUTOMATIC under commit control: F's READ locked record 2
        r.Txn.Retire(canceled);
        r.Reg.Close("F");
        Assert.Null(r.Txn.Commit());                            // deactivates the clause; F's kept locks released
        r.OpenIO("F");
        Assert.Equal("00", Read(r.Reg, "F", org, 2, out _));
        Assert.Equal("00", Read(r.Reg, "F2", org, 2, out _));   // MANUAL again: a READ without WITH LOCK locks nothing
    }

    public static TheoryData<FileOrg, bool> OrgsByStatement()
    {
        var data = new TheoryData<FileOrg, bool>();
        foreach (var org in KeyedOrgs)
        {
            data.Add(org, true);
            data.Add(org, false);
        }
        return data;
    }
}
