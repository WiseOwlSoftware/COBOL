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
/// ⛔ A KEYED CLOSE THAT ANSWERS '30', THROUGH A COMPILED PROGRAM (kb/Work PB3335, review w1118-a5 lead R2-L1; the owner's
/// standing rule, MANDATORY-PRACTICES I11: the exception paths of file I/O are tested as fully as its success paths). The
/// keyed store engine answers a CLOSE §9.1.13.6 item 1's '30' — <i>"A permanent error exists and no further information
/// is available concerning the input-output operation"</i> — in two ways (docs/rearchitecture/DESIGN-runtime-library.md
/// 2.2.1.16): the PROMOTION of a writable connector that changed the file is refused (2.2.1.6 R5), or the last writable
/// connector's CLOSE re-adopts the store for the INPUT sibling it leaves open and the host refuses that re-adoption's read
/// (2.2.1.7; <c>KeyedConnector.CloseCore</c>). The unit cases (<see cref="StoreCoherenceTests"/>,
/// <see cref="KeyedStoreReloadFailureDriftTests"/>) see only the connector's status; these run the program, so the
/// emitted transfer of control — the USE procedure, the FILE STATUS item and whether the run unit continues — is checked
/// for each refusal, in both organizations, in every combination of a USE procedure and a FILE STATUS clause, and with
/// EC-I-O checking enabled, with no declarative, a format 1 USE procedure on the file, or a declarative for the condition.
///
/// <para>The refusal is the host's answer a real file cannot be made to give on demand, so it comes from 2.2.1.12's
/// fault-injecting medium (<see cref="RefusingPageMedium"/> through <c>KeyedStore.MediumObserver</c>), which is per
/// async flow: the program is therefore activated in THIS process, through its generated .NET host entry (the module's
/// <c>Call</c>, DESIGN-external-repository §4.3), whose run unit is the flow's own (<see cref="RunUnit.Current"/>). The
/// program writes its trace into its one BY REFERENCE formal, which the entry decodes in a <c>finally</c>, so the trace
/// up to an abnormal termination reaches the test too. The seam refuses one read of committed slot A, the first that
/// follows the OPEN I-O's two (its adoption and its fence's promotion): the CLOSE's promotion reads both slots, and a
/// reader's adoption reads all three meta pages, so that read is the CLOSE's (<see cref="Probe"/> measures it through the
/// registry, on a copy of the same file, before the program runs).</para>
///
/// <para><b>Every expected value is derived from the rules, not observed.</b> §14.9.6.4 GR4: <i>"The execution of the
/// CLOSE statement causes the value of the I-O status associated with file-name-1 to be updated as specified in 9.1.13,
/// I-O status"</i>, so the FILE STATUS item holds '30'. A status beginning with 3 is fatal (§9.1.13.1), and <i>"the
/// implementor determines what action is taken after the execution of any applicable exception processing statement"</i>:
/// the USE procedure runs, and then <i>"The implementor may either continue or terminate the execution of the run unit"</i>,
/// which docs/CONFORMANCE.md DOC-A.1-103 makes: with EC-I-O checking not enabled the run unit continues when a FILE STATUS
/// clause or a USE procedure covers the statement and terminates abnormally when neither does. With checking enabled the
/// fatal-exception rules follow the §9.1.13 processing (§14.6.13.1.3 rule 3), and the run unit terminates abnormally even
/// with a FILE STATUS clause: with no USE procedure by rule 7, <i>"If checking for the exception condition is enabled,
/// execution of the run unit is terminated abnormally"</i>; after a format 1 USE procedure on the file has run (rule 3's
/// §9.1.13 processing) by the same rule 7; and after a declarative for the condition by rule 5, <i>"If execution of the
/// declarative completes normally the execution of the run unit is terminated abnormally"</i>; so the declarative's trace
/// entry is the last (DOC-A.1-218). §14.9.6.4 GR8 takes the connector OUT of its open mode after a SUCCESSFUL CLOSE —
/// <i>"Following the successful execution of a CLOSE statement without the UNIT phrase, … the open mode of that file
/// connector is set such that it is no longer in an open mode"</i> — and says nothing of an unsuccessful one;
/// docs/CONFORMANCE.md DOC-A.1-105 documents that an unsuccessful CLOSE leaves the connector closed too, so the next OPEN
/// of the connector succeeds rather than answering '41'. GR9 releases the file lock whatever the outcome: <i>"the file
/// lock and any record locks associated with the file connector referenced by file-name-1 are released by the execution
/// of the CLOSE statement"</i>. The file keeps every record a completed statement wrote: page 2 names the last
/// completed statement's trees (2.2.1.6), so the next OPEN adopts them; and the INPUT sibling of a refused re-adoption
/// keeps reading the trees it holds, while the run unit's next OPEN I-O fences (2.2.1.7, 2.2.1.16's re-adoption row).</para>
/// </summary>
public sealed class KeyedClosePermanentErrorWitnessTests : WiseOwlCobolTestBase
{
    /// <summary>Which read of the CLOSE the host refuses.</summary>
    public enum Refusal
    {
        /// <summary>The writer changed the file, so its CLOSE promotes (2.2.1.6 R5), and the promotion's slot read is refused.</summary>
        Promotion,
        /// <summary>The writer leaves an INPUT sibling open, so its CLOSE re-adopts the store for it (2.2.1.7), and the
        /// re-adoption's first read is refused.</summary>
        ReAdoption,
    }

    /// <summary>What covers the writer's statements: a USE procedure, a FILE STATUS clause, both, neither; or, with EC-I-O
    /// checking enabled, a FILE STATUS clause alone, a format 1 USE procedure on the file (<c>USE AFTER STANDARD ERROR
    /// PROCEDURE ON KW</c>, §14.6.13.1.3 rule 3, then rule 7) with and without a FILE STATUS clause, or a declarative for
    /// the condition (<c>USE AFTER EXCEPTION CONDITION EC-I-O</c>, rule 5) with one.</summary>
    public enum Cover
    {
        UseAndFileStatus, UseOnly, FileStatusOnly, Neither,
        CheckingWithFileStatusOnly, CheckingWithUseAndFileStatus, CheckingWithUseOnly, CheckingWithExceptionUseAndFileStatus,
    }

    private static bool HasFileStatus(Cover cover) =>
        cover is not (Cover.UseOnly or Cover.Neither or Cover.CheckingWithUseOnly);

    /// <summary>A format 1 USE procedure on the writer's file.</summary>
    private static bool HasFileUse(Cover cover) =>
        cover is Cover.UseAndFileStatus or Cover.UseOnly or Cover.CheckingWithUseAndFileStatus or Cover.CheckingWithUseOnly;

    private static bool Checking(Cover cover) =>
        cover is Cover.CheckingWithFileStatusOnly or Cover.CheckingWithUseAndFileStatus or Cover.CheckingWithUseOnly
            or Cover.CheckingWithExceptionUseAndFileStatus;

    /// <summary>A declarative runs for the CLOSE's '30': the file's USE procedure, or the condition's.</summary>
    private static bool DeclarativeRuns(Cover cover) =>
        HasFileUse(cover) || cover == Cover.CheckingWithExceptionUseAndFileStatus;

    /// <summary>The run unit continues past the CLOSE's '30' (DOC-A.1-103): checking not enabled, and a FILE STATUS
    /// clause or a USE procedure covers the statement.</summary>
    private static bool Continues(Cover cover) => !Checking(cover) && (HasFileUse(cover) || HasFileStatus(cover));

    /// <summary>The program's one formal: its trace, an entry per statement ending in <c>;</c>.</summary>
    private delegate void Entry(ref string trace);

    /// <summary>The OPEN I-O reads committed slot A twice — its adoption (<see cref="PagedFile.Open"/>) and its fence's
    /// promotion — so the third read is the CLOSE's.</summary>
    private const int RefusedSlotARead = 3;

    /// <summary>A medium that refuses the <paramref name="nth"/> read of committed slot A that the run unit makes, and
    /// no other operation (the count spans every store the observer makes, one per OPEN of the file).</summary>
    private static Func<IPageMedium, IPageMedium> RefusingTheNthSlotARead(int nth)
    {
        int seen = 0;
        return inner => new RefusingPageMedium(inner, read: page =>
            page == MetaPage.SlotA && ++seen == nth ? new IOException("refused by the host (test)") : null);
    }

    private string NewFile(string tag) => Path.Combine(TempDir, $"{tag}.dat");

    /// <summary>The file with the records 0001AAAA and 0002BBBB (relative record numbers 1 and 2), written and CLOSEd
    /// through the registry.</summary>
    private string Seeded(string tag, FileOrg org)
    {
        string host = NewFile(tag);
        FileHarness.Seed(host, org, ["AAAA", "BBBB"]);
        return host;
    }

    /// <summary>The precondition, measured through the registry on a copy of the file, never assumed: the program's
    /// statements before the CLOSE answer '00' over the refusing medium, and the CLOSE answers '30'.</summary>
    private void Probe(string host, FileOrg org, Refusal refusal)
    {
        string copy = NewFile(Path.GetFileNameWithoutExtension(host) + "-probe");
        File.Copy(host, copy);
        var reg = new FileRegistry();
        FileHarness.Register(reg, "W", copy, org);
        FileHarness.Register(reg, "R", copy, org);
        KeyedStore.MediumObserver.Value = RefusingTheNthSlotARead(RefusedSlotARead);
        try
        {
            reg.OpenStatic("W", FileOpenMode.IO);
            Assert.Equal("00", reg.Status("W"));
            if (refusal == Refusal.Promotion) Assert.Equal("00", FileHarness.Write(reg, "W", org, 3, "CCCC"));
            else
            {
                reg.OpenStatic("R", FileOpenMode.Input);
                Assert.Equal("00", reg.Status("R"));
            }
            reg.Close("W");
            Assert.Equal("30", reg.Status("W"));
            if (refusal == Refusal.ReAdoption) reg.Close("R");
        }
        finally { KeyedStore.MediumObserver.Value = null; }
    }

    private static string Program(string id, string host, FileOrg org, Refusal refusal, Cover cover)
    {
        bool fileStatus = HasFileStatus(cover);
        string organization = org == FileOrg.Relative ? "RELATIVE" : "INDEXED";
        string writerKey = org == FileOrg.Relative ? "RELATIVE KEY WK" : "RECORD KEY KWK";
        string readerKey = org == FileOrg.Relative ? "RELATIVE KEY RK" : "RECORD KEY KRK";
        // The relative key is the record number; an indexed record's key is its first four characters.
        string WriterAt(int n) => org == FileOrg.Relative ? $"MOVE {n} TO WK." : "CONTINUE.";
        string ReaderAt(int n) => org == FileOrg.Relative ? $"MOVE {n} TO RK." : $"MOVE \"000{n}\" TO KRK.";
        string Note(string what, string status, string? record = null) =>
            $"STRING \"{what} \" {status}{(record is null ? "" : $" \" \" {record}")} \";\" DELIMITED BY SIZE INTO LG WITH POINTER P.";
        string body = refusal == Refusal.Promotion
            ? $"""
                OPEN I-O KW.
                {Note("OPEN", "FS")}
                MOVE "0003CCCC" TO KWR.
                {WriterAt(3)}
                WRITE KWR.
                {Note("WRITE", "FS")}
                CLOSE KW.
                {Note("CLOSE", "FS")}
                OPEN INPUT KW.
                {Note("REOPEN", "FS")}
                MOVE "0003" TO KWK.
                {WriterAt(3)}
                READ KW.
                {Note("READ", "FS", "KWR")}
                CLOSE KW.
                {Note("CLOSE2", "FS")}
                GOBACK.
            """
            : $"""
                OPEN I-O KW.
                {Note("OPEN", "FS")}
                OPEN INPUT KR.
                {Note("ROPEN", "RS")}
                CLOSE KW.
                {Note("CLOSE", "FS")}
                {ReaderAt(1)}
                READ KR.
                {Note("RREAD", "RS", "KRR")}
                OPEN I-O KW.
                {Note("REOPEN", "FS")}
                MOVE "0003CCCC" TO KWR.
                {WriterAt(3)}
                WRITE KWR.
                {Note("WRITE", "FS")}
                CLOSE KW.
                {Note("CLOSE2", "FS")}
                {ReaderAt(3)}
                READ KR.
                {Note("RREAD3", "RS", "KRR")}
                CLOSE KR.
                {Note("RCLOSE", "RS")}
                GOBACK.
            """;
        string? use = HasFileUse(cover) ? "USE AFTER STANDARD ERROR PROCEDURE ON KW."
            : cover == Cover.CheckingWithExceptionUseAndFileStatus ? "USE AFTER EXCEPTION CONDITION EC-I-O."
            : null;
        string declaratives = use is not null
            ? $"DECLARATIVES.\nERR-S SECTION.\n    {use}\nERR-P.\n    "
              + Note("USE", "FS") + "\nEND DECLARATIVES.\nMAIN-S SECTION.\n"
            : "";
        return $"""
            {(Checking(cover) ? ">>TURN EC-I-O CHECKING ON\n" : "")}IDENTIFICATION DIVISION.
            PROGRAM-ID. {id}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KW ASSIGN TO "{host}"
                    ORGANIZATION {organization} ACCESS DYNAMIC LOCK MODE IS MANUAL SHARING WITH ALL OTHER
                    {writerKey}{(fileStatus ? " FILE STATUS FS" : "")}.
                SELECT KR ASSIGN TO "{host}"
                    ORGANIZATION {organization} ACCESS DYNAMIC LOCK MODE IS MANUAL SHARING WITH ALL OTHER
                    {readerKey} FILE STATUS RS.
            DATA DIVISION.
            FILE SECTION.
            FD KW.
            01 KWR.
               05 KWK PIC X(4).
               05 KWD PIC X(4).
            FD KR.
            01 KRR.
               05 KRK PIC X(4).
               05 KRD PIC X(4).
            WORKING-STORAGE SECTION.
            01 FS PIC XX VALUE "--".
            01 RS PIC XX VALUE "--".
            01 WK PIC 9(4).
            01 RK PIC 9(4).
            01 P PIC 999 VALUE 1.
            LINKAGE SECTION.
            01 LG PIC X(240).
            PROCEDURE DIVISION USING LG.
            {declaratives}MAIN-P.
            {body}
            """;
    }

    /// <summary>Compile the program and activate it through its host entry in a flow of its own, over the refusing
    /// medium: its trace (one entry per statement) and the run unit's ending, if it ended.</summary>
    private (string[] trace, RunUnitEndedException? ended) Run(string id, string source)
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
        // The run unit and the medium seam are both per async flow (RunUnit.Current, KeyedStore.MediumObserver).
        Task.Run(() =>
        {
            KeyedStore.MediumObserver.Value = RefusingTheNthSlotARead(RefusedSlotARead);
            try { entry(ref trace); }
            catch (RunUnitEndedException e) { ended = e; }
            finally { KeyedStore.MediumObserver.Value = null; }
        }).GetAwaiter().GetResult();
        return (trace.TrimEnd().TrimEnd(';').Split(';'), ended);
    }

    public static TheoryData<FileOrg, Refusal, Cover> Cases()
    {
        var data = new TheoryData<FileOrg, Refusal, Cover>();
        foreach (var org in FileHarness.KeyedOrgs)
            foreach (var refusal in Enum.GetValues<Refusal>())
                foreach (var cover in Enum.GetValues<Cover>())
                    data.Add(org, refusal, cover);
        return data;
    }

    /// <summary>The trace the rules give (see the class summary). FS reads '--' when no FILE STATUS clause names it: it
    /// is then an ordinary item the I-O never stores into. The reader's RS is always its own FILE STATUS item.</summary>
    private static string[] Expected(Refusal refusal, Cover cover)
    {
        bool fileStatus = HasFileStatus(cover);
        string ok = fileStatus ? "00" : "--", failed = fileStatus ? "30" : "--";
        var before = refusal == Refusal.Promotion ? new[] { $"OPEN {ok}", $"WRITE {ok}" } : [$"OPEN {ok}", "ROPEN 00"];
        // The declarative runs before the run unit's fate is decided (§9.1.13.1; §14.6.13.1.3 rules 3 and 5).
        string[] use = DeclarativeRuns(cover) ? [$"USE {failed}"] : [];
        if (!Continues(cover)) return [.. before, .. use];
        string[] close = [.. use, $"CLOSE {failed}"];
        string[] after = refusal == Refusal.Promotion
            ? [$"REOPEN {ok}", $"READ {ok} 0003CCCC", $"CLOSE2 {ok}"]
            : ["RREAD 00 0001AAAA", $"REOPEN {ok}", $"WRITE {ok}", $"CLOSE2 {ok}", "RREAD3 00 0003CCCC", "RCLOSE 00"];
        return [.. before, .. close, .. after];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void A_keyed_close_that_answers_30_runs_the_use_procedure_stores_the_status_and_continues_or_terminates_by_the_rule(
        FileOrg org, Refusal refusal, Cover cover)
    {
        string id = $"KC{(org == FileOrg.Relative ? "R" : "I")}{(refusal == Refusal.Promotion ? "P" : "A")}{(int)cover}";
        string host = Seeded(id, org);
        Probe(host, org, refusal);
        var (trace, ended) = Run(id, Program(id, host, org, refusal, cover));
        Assert.Equal(Expected(refusal, cover), trace);
        if (Continues(cover))
        {
            Assert.Null(ended);
            return;
        }
        Assert.NotNull(ended);
        Assert.True(ended.Abnormal, ended.Message);
        Assert.Contains("EC-I-O-PERMANENT-ERROR", ended.Message);
        if (cover == Cover.Neither) Assert.Contains("(CLOSE)", ended.Message);
    }
}
