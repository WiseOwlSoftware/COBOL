// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.Loader;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Runtime.IO.Store;
using WiseOwl.Cobol.Tests.Shared;
using WiseOwl.Cobol.Tests.Unit.Store;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COMMIT OR ROLLBACK THE MEDIUM REFUSES, AND §14.6.13.1.3 ITEM 8, THROUGH A COMPILED PROGRAM (kb/Work PB3338, review
/// findings w1154a R1-1 and R1-3; the owner's standing rule, MANDATORY-PRACTICES I11: the exception paths of file I/O are
/// tested as fully as its success paths). <see cref="Store.TransactionControlTests"/> drives the run unit's half with a
/// hand-written clause; these compile the program, so the EMITTED half is checked: <c>TransactionEmitter.EmitStatement</c>'s
/// own-file arm (the FILE STATUS store, the file's exception processing, item 8's ROLLBACK, in that order) and its
/// foreign-file arm (another element's file), and the item 8 lines <c>SequentialIoEmitter.EmitUseHook</c> writes around
/// every input-output statement's exception processing on a file under commit control (<c>Item8Applies</c>, the one
/// rule; kb/Work PB3542). Each runs in both keyed organizations, with every combination
/// of a USE procedure and a FILE STATUS clause, and with EC-I-O checking enabled with no declarative and with a
/// declarative for the condition. The foreign-file arm also runs the executing element's format 1 declaratives, which
/// §14.9.49.4 GR4 and GR6 a)-d) select by the file's I-O status and open mode with or without checking (kb/Work PB3554):
/// the open-mode tier and the file-name tier (the containing element's GLOBAL file), with and without a FILE STATUS item.
///
/// <para>The refusal comes from 2.2.1.12's fault-injecting medium (<see cref="RefusingPageMedium"/> through
/// <c>KeyedStore.MediumObserver</c>, per async flow), so the program is activated in THIS process through its generated
/// .NET host entry (the module's <c>Call</c>), as <see cref="KeyedClosePermanentErrorWitnessTests"/> does. The medium
/// refuses the first page write the statement makes: <see cref="Probe"/> counts, through the registry on a copy of the
/// same file, the writes the OPEN I-O and the WRITE make, and asserts that refusing the next one makes the COMMIT (or the
/// ROLLBACK) answer the file with '30', before the program runs.</para>
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> COMMIT and ROLLBACK have no I-O status
/// (§9.1.13.1 names the statements that set it), so what a refused one raises is the implementor's, and
/// docs/CONFORMANCE.md §5, A.4.3 determination (a) makes it: <i>"a COMMIT or ROLLBACK the medium refuses raises the fatal
/// EC-I-O-PERMANENT-ERROR for the refused file, with '30' in that file's FILE STATUS item, and with checking off item 8's
/// ROLLBACK follows"</i>. Item 8 (§14.6.13.1.3): <i>"if there is a fatal exception condition for a file subject to an
/// active APPLY COMMIT clause, then a ROLLBACK statement is executed unconditionally"</i>; that ROLLBACK restores the
/// files and the data items to the last COMMIT, here the activation (§14.9.36.4 GR3: <i>"the state they were in at the
/// execution of the last COMMIT statement or the start of the run unit"</i>), and the data items include the FILE STATUS
/// item (§12.4.6.3.3 SR9; §14.9.36.4 GR3: <i>"It also includes the file status data items"</i>), so after item 8 the
/// FILE STATUS item holds its value at activation, '--', and W its value 'INIT'. The USE procedure runs before item 8 (it
/// is the condition's exception processing; item 8's ROLLBACK follows it), so it sees '30' and 'CHGD'. With checking not
/// enabled the run unit continues when a FILE STATUS clause or a USE procedure covers the statement and terminates
/// abnormally when neither does (DOC-A.1-103); with checking enabled the run unit terminates abnormally, with no
/// declarative by §14.6.13.1.3 rule 7, after a declarative naming the condition (format 3) by rule 5, and after a format 1
/// declarative (selected by the I-O status, rule 3 and §9.1.13.1) by rule 7, whose trace entry is then the
/// last. An abnormal termination's rollback restores the files (§14.6.12, §9.1.18.5), so in every case the record the
/// program wrote is absent from the file afterwards: the refused COMMIT committed nothing.</para>
/// </summary>
public sealed class CommitRollbackRefusalWitnessTests : WiseOwlCobolTestBase
{
    /// <summary>What covers the statement: as <see cref="KeyedClosePermanentErrorWitnessTests.Cover"/>, with EC-I-O checking
    /// enabled either with a FILE STATUS clause alone or with a declarative for the condition
    /// (<c>USE AFTER EXCEPTION CONDITION EC-I-O</c>), which notes FUNCTION EXCEPTION-STATUS.</summary>
    public enum Cover { UseAndFileStatus, UseOnly, FileStatusOnly, Neither, CheckingWithFileStatusOnly, CheckingWithExceptionUse }

    private static bool HasFileStatus(Cover c) => c is not (Cover.UseOnly or Cover.Neither);
    private static bool HasFileUse(Cover c) => c is Cover.UseAndFileStatus or Cover.UseOnly;
    private static bool Checking(Cover c) => c is Cover.CheckingWithFileStatusOnly or Cover.CheckingWithExceptionUse;
    private static bool Continues(Cover c) => !Checking(c) && (HasFileUse(c) || HasFileStatus(c));

    private delegate void Entry(ref string trace);

    /// <summary>The writes every store the run unit opens hands to its medium, counted together; the
    /// <paramref name="refuse"/>-th is refused (0: none).</summary>
    private static Func<IPageMedium, IPageMedium> RefusingTheNthWrite(int refuse, Action? counted = null)
    {
        int seen = 0;
        return inner => new RefusingPageMedium(inner, write: _ =>
        {
            counted?.Invoke();
            return ++seen == refuse ? new IOException("refused by the host (test)") : null;
        });
    }

    private string Seeded(string tag, FileOrg org)
    {
        string host = Path.Combine(TempDir, $"{tag}.dat");
        FileHarness.Seed(host, org, ["AAAA", "BBBB"]);
        return host;
    }

    /// <summary>The precondition, measured through the registry on a copy of the file, never assumed: the writes of the
    /// activation's OPEN I-O and the WRITE of record 3, and that refusing the next one refuses the statement with '30'.
    /// Returns the write to refuse.</summary>
    private int Probe(string host, FileOrg org, bool commit)
    {
        int Writes(int refuse, out string? refused, out string status)
        {
            string copy = Path.Combine(TempDir, $"{Path.GetFileNameWithoutExtension(host)}-probe{refuse}.dat");
            File.Copy(host, copy);
            var unit = new RunUnit();
            FileHarness.Register(unit.Files, "F", copy, org, sharing: null);
            Assert.True(unit.Transactions.Activate(this, ["F"], () => { }, () => { }, _ => false));
            int writes = 0;
            KeyedStore.MediumObserver.Value = RefusingTheNthWrite(refuse, () => writes++);
            try
            {
                FileHarness.Open(unit.Files, "F", FileOpenMode.IO);
                Assert.Equal("00", unit.Files.Status("F"));
                Assert.Equal("00", FileHarness.Write(unit.Files, "F", org, 3, "CCCC"));
                int before = writes;
                refused = commit ? unit.Transactions.Commit() : unit.Transactions.Rollback();
                status = unit.Files.Status("F");
                return before;
            }
            finally
            {
                KeyedStore.MediumObserver.Value = null;
                unit.Files.CloseAll();
            }
        }
        int nth = Writes(0, out var none, out _) + 1;
        Assert.Null(none);
        Writes(nth, out var refusedFile, out var refusedStatus);
        Assert.Equal("F", refusedFile);
        Assert.Equal("30", refusedStatus);
        return nth;
    }

    private static string Select(string host, FileOrg org, bool fileStatus) =>
        $"""
        SELECT KF ASSIGN TO "{host}"
            ORGANIZATION {(org == FileOrg.Relative ? "RELATIVE" : "INDEXED")} ACCESS DYNAMIC
            {(org == FileOrg.Relative ? "RELATIVE KEY K" : "RECORD KEY KFK")}{(fileStatus ? " FILE STATUS FS" : "")}.
        """;

    private const string Fd = """
        FD KF.
        01 KFR.
           05 KFK PIC X(4).
           05 KFD PIC X(4).
        """;

    private static string Note(string what, params string[] items) =>
        $"STRING \"{what}\" {string.Concat(items.Select(i => $"\" \" {i} "))}\";\" DELIMITED BY SIZE INTO LG WITH POINTER P.";

    /// <summary>OPEN I-O, WRITE record 3 and change W: the transaction the refused statement leaves uncommitted.</summary>
    private static string Changes(FileOrg org) =>
        $"""
        OPEN I-O KF.
        {Note("OPEN", "FS")}
        MOVE "0003CCCC" TO KFR.
        {(org == FileOrg.Relative ? "MOVE 3 TO K." : "CONTINUE.")}
        WRITE KFR.
        {Note("WRITE", "FS")}
        MOVE "CHGD" TO W.
        """;

    /// <summary>OPEN INPUT, READ record 3 (absent after a rollback) and CLOSE: a fresh connector, because the refused
    /// statement left the file in permanent error until its CLOSE (DESIGN-runtime-library.md 2.2.1.10, "What the program is
    /// told").</summary>
    private static string Reread(FileOrg org) =>
        $"""
        OPEN INPUT KF.
        {Note("REOPEN", "FS")}
        {(org == FileOrg.Relative ? "MOVE 3 TO K." : "MOVE \"0003\" TO KFK.")}
        READ KF INVALID KEY {Note("READ", "FS", "\"ABSENT\"").TrimEnd('.')} NOT INVALID KEY {Note("READ", "FS", "KFR").TrimEnd('.')} END-READ.
        CLOSE KF.
        {Note("CLOSE2", "FS")}
        """;

    private const string Storage = """
        01 FS PIC XX VALUE "--".
        01 W PIC X(4) VALUE "INIT".
        01 K PIC 9(4).
        01 XS PIC X(31).
        01 P PIC 999 VALUE 1.
        """;

    /// <summary>The note of a USE procedure: the FILE STATUS item and W as it ran; a declarative for the condition also
    /// notes FUNCTION EXCEPTION-STATUS.</summary>
    private static string UseNote(string tag, bool exceptionStatus, bool items = true) =>
        exceptionStatus
            ? $"MOVE FUNCTION EXCEPTION-STATUS TO XS.\nSTRING \"{tag} \" {(items ? "FS \" \" W \" \" " : "")}DELIMITED BY SIZE XS DELIMITED BY SPACE \";\" DELIMITED BY SIZE INTO LG WITH POINTER P."
            : Note(tag, "FS", "W");

    private static string Declaratives(string use, string note) =>
        $"DECLARATIVES.\nERR-S SECTION.\n    {use}\nERR-P.\n{note}\nEND DECLARATIVES.\nMAIN-S SECTION.\n";

    /// <summary>One element with APPLY COMMIT ON KF W executing the refused statement itself (the own-file arm).</summary>
    private static string OwnFileProgram(string id, string host, FileOrg org, string verb, Cover cover)
    {
        string declaratives = HasFileUse(cover) ? Declaratives("USE AFTER STANDARD ERROR PROCEDURE ON KF.", UseNote("USE", false))
            : cover == Cover.CheckingWithExceptionUse ? Declaratives("USE AFTER EXCEPTION CONDITION EC-I-O.", UseNote("USE", true))
            : "";
        return $"""
            {(Checking(cover) ? ">>TURN EC-I-O CHECKING ON\n" : "")}IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
            {Select(host, org, HasFileStatus(cover))}
            I-O-CONTROL.
                APPLY COMMIT ON KF W.
            DATA DIVISION.
            FILE SECTION.
            {Fd}
            WORKING-STORAGE SECTION.
            {Storage}
            LINKAGE SECTION.
            01 LG PIC X(240).
            PROCEDURE DIVISION USING LG.
            {declaratives}MAIN-P.
            {Changes(org)}
            {verb}.
            {Note(verb, "FS", "W")}
            CLOSE KF.
            {Note("CLOSE", "FS")}
            {Reread(org)}
            GOBACK.
            END PROGRAM {id}.
            """;
    }

    /// <summary>The trace the rules give (see the class summary).</summary>
    private static string[] OwnFileExpected(string verb, Cover cover)
    {
        bool fs = HasFileStatus(cover);
        string ok = fs ? "00" : "--";
        string[] before = [$"OPEN {ok}", $"WRITE {ok}"];
        // The declarative runs before item 8's ROLLBACK and before the run unit's fate is decided. A refused COMMIT saved
        // nothing, so W is still the program's CHGD; a refused ROLLBACK has restored the items already (§14.9.36.4 GR3:
        // every file but the refused one was rolled back, and the program is told of that one by its status).
        string w = verb == "COMMIT" ? "CHGD" : "INIT";
        string[] use = HasFileUse(cover) ? [$"USE {(fs ? "30" : "--")} {w}"]
            : cover == Cover.CheckingWithExceptionUse ? [$"USE 30 {w} EC-I-O-PERMANENT-ERROR"]
            : [];
        if (!Continues(cover)) return [.. before, .. use];
        return [.. before, .. use, $"{verb} -- INIT", .. Reopened(fs)];
    }

    /// <summary>The CLOSE of the connector the refusal left in permanent error, then a fresh OPEN INPUT that finds record 3
    /// absent: item 8's ROLLBACK (or the run unit's) removed it.</summary>
    private static string[] Reopened(bool fileStatus)
    {
        string ok = fileStatus ? "00" : "--";
        return [$"CLOSE {ok}", $"REOPEN {ok}", $"READ {(fileStatus ? "23" : "--")} ABSENT", $"CLOSE2 {ok}"];
    }

    public static TheoryData<FileOrg, string, Cover> OwnFileCases()
    {
        var data = new TheoryData<FileOrg, string, Cover>();
        foreach (var org in FileHarness.KeyedOrgs)
            foreach (var verb in new[] { "COMMIT", "ROLLBACK" })
                foreach (var cover in Enum.GetValues<Cover>())
                    data.Add(org, verb, cover);
        return data;
    }

    [Theory]
    [MemberData(nameof(OwnFileCases))]
    public void A_refused_commit_or_rollback_of_the_elements_own_file_is_told_raised_and_rolled_back_by_item_8(
        FileOrg org, string verb, Cover cover)
    {
        string id = $"CR{(org == FileOrg.Relative ? "R" : "I")}{verb[0]}{(int)cover}";
        string host = Seeded(id, org);
        int refuse = Probe(host, org, verb == "COMMIT");
        var (trace, ended) = Run(id, OwnFileProgram(id, host, org, verb, cover), refuse);
        Assert.Equal(OwnFileExpected(verb, cover), trace);
        AssertEnding(ended, Continues(cover), cover == Cover.Neither ? $"({verb})" : null);
        AssertRecordAbsent(host, org);
    }

    // ── The foreign-file arm: the refused file is another element's ──────────────────────────────────────────────────

    /// <summary>What covers a refused file of ANOTHER element's clause, in the element executing the statement: the
    /// describing element's FILE STATUS clause (no checking), nothing, EC-I-O checking by a directive naming no file in the
    /// executing element with the FILE STATUS clause, or with a declarative for the condition there; and (kb/Work PB3554)
    /// the executing element's FORMAT 1 declaratives, which apply to another element's refused file as to any input-output
    /// statement's file, with or without checking (§14.9.49.4 GR4 a), GR6 a)-d)): the open-mode tier (<c>USE ... ON
    /// I-O</c>, by the file's open mode), the file-name tier (<c>USE ... ON KF</c> for the containing element's GLOBAL
    /// file), each with and without the describing element's FILE STATUS clause, the open-mode tier with checking
    /// enabled (a format 1 declarative names no exception-name, so rule 5 does not select it: the I-O status selects it under
    /// rule 3 and §9.1.13.1, then the fatal condition ends the run unit under rule 7, §14.6.13.1.3), and the open-mode tier ending
    /// in <c>RESUME AT</c> (item 8's ROLLBACK is before the transfer), the file-name tier with checking enabled by a
    /// directive that NAMES the file (<c>&gt;&gt;TURN EC-I-O KF CHECKING ON</c>, §7.3.25.4 GR6: "if file-name-1 is
    /// specified, checking is enabled only for exception conditions associated with that file-name") and with checking
    /// enabled for every file and disabled for the file by a directive naming it (GR8), and the containing element's own
    /// <c>USE GLOBAL</c> declarative (§14.9.49.4 GR4 b)), which the executing element's refused COMMIT or ROLLBACK
    /// reaches when the executing element has none.</summary>
    public enum ForeignCover
    {
        StatusItem, Nothing, CheckingWithStatusItem, CheckingWithExceptionUse,
        OpenModeUseWithStatusItem, OpenModeUseNoStatusItem, GlobalFileUseWithStatusItem, GlobalFileUseNoStatusItem,
        CheckingWithOpenModeUse, OpenModeUseResumeAt,
        NamedFileCheckingWithStatusItem, NamedFileCheckingNoStatusItem, NamedFileCheckingOffUnderGeneralOn,
        ContainerGlobalUseNoStatusItem, ContainerGlobalUseWithStatusItem,
    }

    /// <summary>Checking for the EC-I-O condition is enabled at the statement for the refused file, by a directive naming
    /// no file or naming the file (§7.3.25.4 GR6).</summary>
    private static bool ForeignChecking(ForeignCover c) =>
        c is ForeignCover.CheckingWithStatusItem or ForeignCover.CheckingWithExceptionUse or ForeignCover.CheckingWithOpenModeUse
            or ForeignCover.NamedFileCheckingWithStatusItem or ForeignCover.NamedFileCheckingNoStatusItem;

    /// <summary>The directives that precede the contained program: they govern only its statements (§7.3.25.4 GR5/GR6).</summary>
    private static string ForeignDirectives(ForeignCover c) => c switch
    {
        ForeignCover.CheckingWithStatusItem or ForeignCover.CheckingWithExceptionUse or ForeignCover.CheckingWithOpenModeUse =>
            ">>TURN EC-I-O CHECKING ON\n",
        ForeignCover.NamedFileCheckingWithStatusItem or ForeignCover.NamedFileCheckingNoStatusItem =>
            ">>TURN EC-I-O KF CHECKING ON\n",
        ForeignCover.NamedFileCheckingOffUnderGeneralOn => ">>TURN EC-I-O CHECKING ON\n>>TURN EC-I-O KF CHECKING OFF\n",
        _ => "",
    };

    /// <summary>The describing (containing) element gives the file a FILE STATUS clause.</summary>
    private static bool ForeignStatusItem(ForeignCover c) =>
        c is not (ForeignCover.Nothing or ForeignCover.OpenModeUseNoStatusItem or ForeignCover.GlobalFileUseNoStatusItem
            or ForeignCover.NamedFileCheckingNoStatusItem or ForeignCover.ContainerGlobalUseNoStatusItem);

    private static bool ForeignGlobalFile(ForeignCover c) =>
        c is ForeignCover.GlobalFileUseWithStatusItem or ForeignCover.GlobalFileUseNoStatusItem
            or ForeignCover.NamedFileCheckingWithStatusItem or ForeignCover.NamedFileCheckingNoStatusItem
            or ForeignCover.NamedFileCheckingOffUnderGeneralOn;

    /// <summary>The run unit continues: the describing element stored the status (<see cref="ForeignCover.StatusItem"/>) or
    /// a format 1 declarative ran with checking off (DOC-A.1-103 terminates only when neither covers the status).</summary>
    private static bool ForeignContinues(ForeignCover c) =>
        c is ForeignCover.StatusItem or ForeignCover.OpenModeUseWithStatusItem or ForeignCover.OpenModeUseNoStatusItem
            or ForeignCover.GlobalFileUseWithStatusItem or ForeignCover.GlobalFileUseNoStatusItem or ForeignCover.OpenModeUseResumeAt
            or ForeignCover.NamedFileCheckingOffUnderGeneralOn
            or ForeignCover.ContainerGlobalUseNoStatusItem or ForeignCover.ContainerGlobalUseWithStatusItem;

    /// <summary>The element <c>id</c> with APPLY COMMIT ON KF W calls its contained <c>id</c>S, which has no clause and
    /// executes the statement; the TURN directive, when present, precedes the contained program, so it governs only that
    /// program's statements (§7.3.25.4 GR5/GR6).</summary>
    private static string ForeignProgram(string id, string host, FileOrg org, string verb, ForeignCover cover)
    {
        string suse = UseNote("SUSE", true, items: false);
        string declaratives = cover switch
        {
            ForeignCover.CheckingWithExceptionUse => Declaratives("USE AFTER EXCEPTION CONDITION EC-I-O.", suse),
            ForeignCover.OpenModeUseWithStatusItem or ForeignCover.OpenModeUseNoStatusItem or ForeignCover.CheckingWithOpenModeUse =>
                Declaratives("USE AFTER STANDARD ERROR PROCEDURE ON I-O.", suse),
            ForeignCover.GlobalFileUseWithStatusItem or ForeignCover.GlobalFileUseNoStatusItem
                or ForeignCover.NamedFileCheckingWithStatusItem or ForeignCover.NamedFileCheckingNoStatusItem
                or ForeignCover.NamedFileCheckingOffUnderGeneralOn =>
                Declaratives("USE AFTER STANDARD ERROR PROCEDURE ON KF.", suse),
            ForeignCover.OpenModeUseResumeAt =>
                Declaratives("USE AFTER STANDARD ERROR PROCEDURE ON I-O.", suse + "\nRESUME AT S-END."),
            _ => "",
        };
        // The containing element's own GLOBAL declarative; the executing element has none (§14.9.49.4 GR4 b)).
        string containerDeclaratives = cover is ForeignCover.ContainerGlobalUseNoStatusItem or ForeignCover.ContainerGlobalUseWithStatusItem
            ? Declaratives("USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON I-O.", suse)
            : "";
        return $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
            {Select(host, org, ForeignStatusItem(cover))}
            I-O-CONTROL.
                APPLY COMMIT ON KF W.
            DATA DIVISION.
            FILE SECTION.
            {(ForeignGlobalFile(cover) ? Fd.Replace("FD KF.", "FD KF IS GLOBAL.") : Fd)}
            WORKING-STORAGE SECTION.
            {Storage}
            LINKAGE SECTION.
            01 LG PIC X(240).
            PROCEDURE DIVISION USING LG.
            {containerDeclaratives}MAIN-P.
            {Changes(org)}
            CALL "{id}S" USING LG P.
            {Note("CALL", "FS", "W")}
            CLOSE KF.
            {Note("CLOSE", "FS")}
            {Reread(org)}
            GOBACK.
            {ForeignDirectives(cover)}IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}S.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 XS PIC X(31).
            LINKAGE SECTION.
            01 LG PIC X(240).
            01 P PIC 999.
            PROCEDURE DIVISION USING LG P.
            {declaratives}S-P.
            {verb}.
            {Note(verb)}
            S-END.
            GOBACK.
            END PROGRAM {id}S.
            END PROGRAM {id}.
            """;
    }

    private static string[] ForeignExpected(string verb, ForeignCover cover)
    {
        bool fs = ForeignStatusItem(cover);
        string ok = fs ? "00" : "--";
        string[] before = [$"OPEN {ok}", $"WRITE {ok}"];
        return cover switch
        {
            // The describing element stored '30' (TransactionControl.Refused), so DOC-A.1-103 continues; item 8 restores.
            ForeignCover.StatusItem => [.. before, verb, "CALL -- INIT", .. Reopened(fileStatus: true)],
            ForeignCover.CheckingWithExceptionUse or ForeignCover.CheckingWithOpenModeUse
                or ForeignCover.NamedFileCheckingWithStatusItem or ForeignCover.NamedFileCheckingNoStatusItem =>
                [.. before, "SUSE EC-I-O-PERMANENT-ERROR"],
            // With checking off no condition is raised (EXCEPTION-STATUS is blank, §14.6.13.1.1) and the format 1 declarative
            // runs by the file's status (§14.9.49.4 GR6); a declarative ran, so the run unit continues with or without a
            // FILE STATUS clause (DOC-A.1-103) and item 8's ROLLBACK follows it, before any transfer.
            ForeignCover.OpenModeUseWithStatusItem or ForeignCover.OpenModeUseNoStatusItem
                or ForeignCover.GlobalFileUseWithStatusItem or ForeignCover.GlobalFileUseNoStatusItem
                or ForeignCover.NamedFileCheckingOffUnderGeneralOn
                or ForeignCover.ContainerGlobalUseNoStatusItem or ForeignCover.ContainerGlobalUseWithStatusItem =>
                [.. before, "SUSE ", verb, "CALL -- INIT", .. Reopened(fs)],
            ForeignCover.OpenModeUseResumeAt => [.. before, "SUSE ", "CALL -- INIT", .. Reopened(fs)],
            _ => before,
        };
    }

    public static TheoryData<FileOrg, string, ForeignCover> ForeignCases()
    {
        var data = new TheoryData<FileOrg, string, ForeignCover>();
        foreach (var org in FileHarness.KeyedOrgs)
            foreach (var verb in new[] { "COMMIT", "ROLLBACK" })
                foreach (var cover in Enum.GetValues<ForeignCover>())
                    data.Add(org, verb, cover);
        return data;
    }

    [Theory]
    [MemberData(nameof(ForeignCases))]
    public void A_refused_commit_or_rollback_of_another_elements_file_reaches_the_executing_elements_declaratives_with_or_without_checking(
        FileOrg org, string verb, ForeignCover cover)
    {
        string id = $"CF{(org == FileOrg.Relative ? "R" : "I")}{verb[0]}{(int)cover}";
        string host = Seeded(id, org);
        int refuse = Probe(host, org, verb == "COMMIT");
        var (trace, ended) = Run(id, ForeignProgram(id, host, org, verb, cover), refuse);
        Assert.Equal(ForeignExpected(verb, cover), trace);
        AssertEnding(ended, ForeignContinues(cover), cover == ForeignCover.Nothing ? $"({verb})" : null);
        AssertRecordAbsent(host, org);
    }

    // ── Item 8 after an ordinary input-output statement ──────────────────────────────────────────────────────────────

    /// <summary>§14.6.13.1.3 item 8 after a READ of the closed file ('47', EC-I-O-LOGIC-ERROR: fatal, §9.1.13.1) — the line
    /// <c>EmitUseHook</c> appends for a file under commit control: the CLOSE kept the transaction (kb/Work PB3480), and the
    /// ROLLBACK removes record 3 and restores W and the FILE STATUS item to the activation's 'INIT' and '--'.</summary>
    [Theory]
    [MemberData(nameof(Orgs))]
    public void A_fatal_status_of_an_ordinary_statement_on_a_file_under_commit_control_rolls_back_by_item_8(FileOrg org)
    {
        string id = $"CO{(org == FileOrg.Relative ? "R" : "I")}";
        string host = Seeded(id, org);
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
            {Select(host, org, fileStatus: true)}
            I-O-CONTROL.
                APPLY COMMIT ON KF W.
            DATA DIVISION.
            FILE SECTION.
            {Fd}
            WORKING-STORAGE SECTION.
            {Storage}
            LINKAGE SECTION.
            01 LG PIC X(240).
            PROCEDURE DIVISION USING LG.
            MAIN-P.
            {Changes(org)}
            CLOSE KF.
            {Note("CLOSE", "FS", "W")}
            READ KF NEXT RECORD END-READ.
            {Note("READ47", "FS", "W")}
            {Reread(org)}
            GOBACK.
            END PROGRAM {id}.
            """;
        var (trace, ended) = Run(id, source, refuse: 0);
        Assert.Null(ended);
        Assert.Equal(["OPEN 00", "WRITE 00", "CLOSE 00 CHGD", "READ47 -- INIT", "REOPEN 00", "READ 23 ABSENT", "CLOSE2 00"], trace);
        AssertRecordAbsent(host, org);
    }

    public static TheoryData<FileOrg> Orgs() => [.. FileHarness.KeyedOrgs];

    // ── The SEQUENTIAL operand (kb/Work PB3340; review finding w1154a R1-4) ────────────────────────────────────────────

    /// <summary>A SEQUENTIAL operand puts the file under commit control (§9.1.18.2; kb/Work PB3340), and with it its FILE STATUS
    /// item (§12.4.6.3.3 SR9: <i>"the file status data items ... are automatically subject to the APPLY COMMIT clause"</i>), so
    /// the ROLLBACK restores the item to its value at the last COMMIT (§14.9.36.4 GR3: <i>"It also includes the file status data
    /// items"</i>): the AT END's '10' gives way to the '00' the COMMIT saved after the OPEN, and W to 'CHGD'.</summary>
    [Fact]
    public void A_sequential_operand_puts_the_file_and_its_status_item_under_commit_control()
    {
        const string id = "CSQ";
        string host = Path.Combine(TempDir, "csq.dat");
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT SQ ASSIGN TO "{host}" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            I-O-CONTROL.
                APPLY COMMIT ON SQ W.
            DATA DIVISION.
            FILE SECTION.
            FD SQ.
            01 SQR PIC X(4).
            WORKING-STORAGE SECTION.
            {Storage}
            LINKAGE SECTION.
            01 LG PIC X(240).
            PROCEDURE DIVISION USING LG.
            MAIN-P.
            OPEN OUTPUT SQ.
            CLOSE SQ.
            OPEN INPUT SQ.
            MOVE "CHGD" TO W.
            COMMIT.
            READ SQ AT END CONTINUE END-READ.
            {Note("READ", "FS")}
            MOVE "LATE" TO W.
            ROLLBACK.
            {Note("ROLLBACK", "FS", "W")}
            CLOSE SQ.
            GOBACK.
            END PROGRAM {id}.
            """;
        var (trace, ended) = Run(id, source, refuse: 0);
        Assert.Null(ended);
        Assert.Equal(["READ 10", "ROLLBACK 00 CHGD"], trace);
    }

    // ── The harness ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Compile the program and activate it through its host entry in a flow of its own, over a medium that
    /// refuses the <paramref name="refuse"/>-th write (0: none): its trace (one entry per note) and the run unit's ending.</summary>
    private (string[] trace, RunUnitEndedException? ended) Run(string id, string source, int refuse)
    {
        string src = Path.Combine(TempDir, $"{id}.cob"), dll = Path.Combine(TempDir, $"{id}.dll");
        File.WriteAllText(src, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: 2023,
            SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(result.Success, $"{result.Status}: {string.Join("\n", result.Errors)}");
        var call = ScratchDirectory.LoadAssembly(dll).GetTypes()
            .Single(t => t.Name == id && t.GetMethod("Call") is not null).GetMethod("Call")!;
        var entry = call.CreateDelegate<Entry>();
        string trace = new(' ', 240);
        RunUnitEndedException? ended = null;
        Task.Run(() =>
        {
            KeyedStore.MediumObserver.Value = RefusingTheNthWrite(refuse);
            try { entry(ref trace); }
            catch (RunUnitEndedException e) { ended = e; }
            finally { KeyedStore.MediumObserver.Value = null; }
        }).GetAwaiter().GetResult();
        return (trace.TrimEnd().TrimEnd(';').Split(';'), ended);
    }

    /// <summary>The run unit continued, or ended abnormally on EC-I-O-PERMANENT-ERROR (with <paramref name="statement"/>
    /// in DOC-A.1-103's message when nothing covered the status).</summary>
    private static void AssertEnding(RunUnitEndedException? ended, bool continues, string? statement)
    {
        if (continues)
        {
            Assert.Null(ended);
            return;
        }
        Assert.NotNull(ended);
        Assert.True(ended.Abnormal, ended.Message);
        Assert.Contains("EC-I-O-PERMANENT-ERROR", ended.Message);
        if (statement is not null) Assert.Contains(statement, ended.Message);
    }

    /// <summary>Record 3, which the program wrote and never committed, is absent from the file.</summary>
    private static void AssertRecordAbsent(string host, FileOrg org)
    {
        var reader = new FileRegistry();
        FileHarness.Register(reader, "R", host, org);
        FileHarness.Open(reader, "R", FileOpenMode.Input);
        try
        {
            Assert.Equal("00", reader.Status("R"));
            Assert.NotEqual("00", FileHarness.Read(reader, "R", org, 3, out _));
            Assert.Equal("00", FileHarness.Read(reader, "R", org, 2, out var image));
            Assert.Equal("0002BBBB", image);
        }
        finally { reader.CloseAll(); }
    }
}
