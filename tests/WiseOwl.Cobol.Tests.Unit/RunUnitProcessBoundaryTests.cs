// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ §9.1.15's FILE LOCK ACROSS A REAL PROCESS BOUNDARY (kb/Work PB2485): two compiled COBOL programs in two operating-system
/// processes, one holding a file and the other asking for it. <see cref="RunUnitFileLockDriftTests"/> proves the
/// lock's arbitration over every sharing and open mode with a second <see cref="FileRegistry"/> as the second run unit;
/// that reaches the same host primitive, but it cannot see what only a process boundary has: a lock that dies with
/// its HOLDER (an implicit CLOSE, an abnormal termination, a kill that runs nothing), a descendant process that
/// inherits what its parent holds, and the path a compiled program really takes (the generated code's
/// <c>FileConnector.Open</c>, not the registry's API).
/// <para>ISO §9.1.15 3): <i>"The successful opening of a file establishes a file lock for the applicable sharing rules,
/// thereby preventing other run units from opening that file with incompatible sharing rules. The file lock is
/// removed by an explicit or implicit CLOSE statement executed for that file connector"</i>. The refusal is §9.1.13.9
/// 1)'s '61' for an OPEN and 2)'s '62' for a DELETE FILE; an unsuccessful OPEN leaves the file as it was (§14.9.27.4
/// GR25); a run unit's end is an implicit CLOSE of every open file (§14.6.11 2)), also after an abnormal end
/// (§14.6.12), and what a killed process loses with its descriptors is the implementor's documented host fact
/// (docs/CONFORMANCE.md DOC-A.1-75).</para>
/// <para><b>The exception paths are the subject, not a corner of it</b> (MANDATORY-PRACTICES I11): every refused request
/// is run with a FILE STATUS item alone, with a USE declarative alone, with both and with neither (§9.1.12; §9.1.13.1:
/// a '6x' status is not a fatal class, so a request with no handler continues), and with a RETRY phrase that the
/// holder outlasts (§14.7.9.3 4)).</para>
/// <para><b>Why <c>Host=ReFS</c> (tier 0h):</b> this class is the process-boundary member of the family whose other two,
/// <see cref="RunUnitFileLockDriftTests"/> and <c>SidecarColumnLockHostDriftTests</c>, carry the same trait: the lock files a
/// holder publishes beside the physical file are removed by a holder's CLOSE, end or kill, and removal differs between this
/// host's ReFS and CI's NTFS (the PB2917 unlink race); only a REAL holder's end, in another process, reaches that removal
/// the way a compiled program does.</para>
/// </summary>
[Trait("Host", "ReFS")]
public sealed class RunUnitProcessBoundaryTests : IClassFixture<RunUnitProcessBoundaryTests.CompiledPrograms>, IDisposable
{
    /// <summary>Compiles each COBOL program once per class and hands every test the same assembly.</summary>
    public sealed class CompiledPrograms : IDisposable
    {
        private readonly string _root = ScratchDirectory.Create("pb2485-");
        private readonly Dictionary<string, string> _built = [];

        internal string Build(string name, string source)
        {
            lock (_built)
            {
                if (_built.TryGetValue(name, out string? dll)) return dll;
                string dir = Path.Combine(_root, name);
                Directory.CreateDirectory(dir);
                string src = Path.Combine(dir, name + ".cob");
                dll = Path.Combine(dir, name + ".dll");
                File.WriteAllText(src, source);
                var result = CompilerDriver.Compile(new CompilerDriver.Options(
                    src, dll, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
                Assert.True(result.Success, $"{name} did not compile: {result.Status}: {string.Join("\n", result.Errors)}");
                _built[name] = dll;
                return dll;
            }
        }

        public void Dispose()
        {
            ScratchDirectory.Remove(_root);
        }
    }

    private readonly CompiledPrograms _programs;
    private readonly string _work = ScratchDirectory.Create("pb2485w-");

    public RunUnitProcessBoundaryTests(CompiledPrograms programs)
    {
        _programs = programs;
    }

    public void Dispose()
    {
        ScratchDirectory.Remove(_work);
    }

    private const string Seed = "SEEDSEED";
    private static readonly string[] EveryOpenMode = ["INPUT", "I-O", "EXTEND", "OUTPUT"];

    // ── The programs ────────────────────────────────────────────────────────────────────────────────────────────────

    private static string Cobol(params string[] lines) => string.Join("\n", lines) + "\n";

    /// <summary>The file's SELECT entry, its sentence ended by the period after its last clause. A SHARING WITH ALL OTHER
    /// entry carries LOCK MODE IS MANUAL beside it, which §14.9.27.3 SR8 asks for when the OPEN has no sharing phrase.</summary>
    private static string[] SelectEntry(string file, string? sharing, bool fileStatus)
    {
        var entry = new List<string> { $"           SELECT F ASSIGN TO \"{file}\"", "               ORGANIZATION IS SEQUENTIAL" };
        if (sharing is not null) entry.Add($"               SHARING WITH {sharing}");
        if (sharing == "ALL OTHER") entry.Add("               LOCK MODE IS MANUAL");
        if (fileStatus) entry.Add("               FILE STATUS IS ST");
        entry[^1] += ".";
        return [.. entry];
    }

    /// <summary>The HOLDER: opens <paramref name="file"/> in <paramref name="sharing"/> and <paramref name="mode"/>, prints
    /// <c>HELD &lt;status&gt;</c>, then obeys one line from stdin — <c>CLOSE</c> gives the lock back with an explicit
    /// CLOSE and waits for a second line; <c>ABORT</c> ends the run unit abnormally (§14.9.4.4 GR3: a call of a
    /// program that does not exist); anything else ends it normally with the file still open.</summary>
    private static string Holder(string id, string file, string sharing, string mode) => Cobol([
        "       IDENTIFICATION DIVISION.",
        $"       PROGRAM-ID. {id}.",
        "       ENVIRONMENT DIVISION.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        .. SelectEntry(file, sharing, fileStatus: true),
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD F.",
        "       01 REC PIC X(8).",
        "       WORKING-STORAGE SECTION.",
        "       01 ST PIC XX.",
        "       01 CMD PIC X(8).",
        "       PROCEDURE DIVISION.",
        "       MAIN.",
        $"           OPEN {mode} F.",
        "           DISPLAY \"HELD \" ST.",
        "           ACCEPT CMD.",
        "           IF CMD = \"CLOSE\"",
        "               CLOSE F",
        "               DISPLAY \"CLOSED \" ST",
        "               ACCEPT CMD",
        "           END-IF.",
        "           IF CMD = \"ABORT\"",
        "               CALL \"NOSUCHPG\"",
        "           END-IF.",
        "           DISPLAY \"EXITING\".",
        "           STOP RUN."]);

    /// <summary>The second run unit. Its file connector carries a FILE STATUS item (<paramref name="fileStatus"/>) and
    /// a USE declarative with the file-sharing exception checked (<paramref name="declarative"/>) as asked, and its
    /// procedure division is <paramref name="body"/>.</summary>
    private static string Prober(string id, bool fileStatus, bool declarative, string[] body, string? sharing = null)
    {
        var lines = new List<string>();
        if (declarative) lines.Add("       >>TURN EC-I-O-FILE-SHARING CHECKING ON");
        lines.AddRange(
        [
            "       IDENTIFICATION DIVISION.",
            $"       PROGRAM-ID. {id}.",
            "       ENVIRONMENT DIVISION.",
            "       INPUT-OUTPUT SECTION.",
            "       FILE-CONTROL.",
            .. SelectEntry("shared.dat", sharing, fileStatus),
        ]);
        lines.AddRange(
        [
            "       DATA DIVISION.",
            "       FILE SECTION.",
            "       FD F.",
            "       01 REC PIC X(8).",
            "       WORKING-STORAGE SECTION.",
            "       01 ST PIC XX.",
            "       PROCEDURE DIVISION.",
        ]);
        if (declarative)
            lines.AddRange(
            [
                "       DECLARATIVES.",
                "       D SECTION.",
                "           USE AFTER STANDARD EXCEPTION PROCEDURE ON F.",
                "       D1.",
                "           DISPLAY \"DECL [\"",
                "               FUNCTION TRIM(FUNCTION EXCEPTION-STATUS) \"]\".",
                "           DISPLAY \"FILE [\" FUNCTION EXCEPTION-FILE \"]\".",
                "       END DECLARATIVES.",
                "       MAIN SECTION.",
            ]);
        lines.Add("       M1.");
        lines.AddRange(body);
        lines.Add("           STOP RUN.");
        return Cobol([.. lines]);
    }

    /// <summary>One OPEN per mode: <c>&lt;mode&gt; &lt;status&gt;</c> is printed for each, and a file that opened is closed.</summary>
    private static string[] OpenEach(IEnumerable<string> modes, string retry = "") =>
        [.. modes.SelectMany(mode => new[]
        {
            $"           OPEN {mode} {retry}F.",
            $"           DISPLAY \"{mode} \" ST.",
            "           IF ST = \"00\" CLOSE F END-IF.",
        })];

    /// <summary>A PROGRAM-ID for a program named like its assembly: upper case, a hyphen where the name has an underscore.</summary>
    private static string Id(string name) => name.Replace('_', '-').ToUpperInvariant();

    private string Open(string name, IEnumerable<string>? modes = null, string retry = "") =>
        _programs.Build(name, Prober(Id(name), fileStatus: true, declarative: false, OpenEach(modes ?? EveryOpenMode, retry)));

    // ── The harness ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The holder most tests ask against: <c>SHARING WITH NO OTHER</c>, open for I-O.</summary>
    private string NoOtherHolder() =>
        _programs.Build("pb2485_holder_noother", Holder("PB2485HNO", "shared.dat", "NO OTHER", "I-O"));

    private string SharedFile =>Path.Combine(_work, "shared.dat");

    /// <summary>The held file's size, from its metadata: the holder's share mode refuses this process a read, but not
    /// this question, and an OPEN OUTPUT that got through would have truncated it to nothing.</summary>
    private long SharedFileLength() => new FileInfo(SharedFile).Length;

    private void SeedFile(string name) => File.WriteAllText(Path.Combine(_work, name), Seed);

    private ProcessStartInfo Dotnet(string dll) => new("dotnet", $"\"{dll}\"") { WorkingDirectory = _work };

    private LiveProcess StartHolder(string dll, string expectedHeld = "HELD 00")
    {
        var holder = LiveProcess.Start(Dotnet(dll));
        Assert.Equal(expectedHeld, holder.ExpectLine());
        return holder;
    }

    /// <summary>Run <paramref name="dll"/> as a second run unit to completion and return its stdout lines.</summary>
    private string[] Run(string dll)
    {
        var observed = ProcessObserver.ObserveOrThrow(Dotnet(dll));
        Assert.True(observed.ExitCode == 0, $"{Path.GetFileName(dll)} ended with exit code {observed.ExitCode}:\n{observed.Stderr}");
        return observed.Stdout.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] Every(string status, IEnumerable<string>? modes = null) =>
        [.. (modes ?? EveryOpenMode).Select(mode => $"{mode} {status}")];

    // ── 1. The lock is held across the boundary and is given back by the holder's CLOSE ───────────────────────────────

    /// <summary>§9.1.13.9 1) a): another run unit holds the file in the sharing with no other mode, so EVERY open mode a
    /// second run unit asks for is '61' — and the file lock is removed by the holder's explicit CLOSE (§9.1.15 3)), after
    /// which the same four OPENs succeed while the holder is still alive: it is the CLOSE that gave the lock back, not
    /// the holder's end.</summary>
    [Fact]
    public void ARefusedOpenIsStatus61_AndTheHoldersCloseGivesTheLockBack_WhileTheHolderLives()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = Open("pb2485_open_each");
        using var holder = StartHolder(holderDll);

        Assert.Equal(Every("61"), Run(prober));
        Assert.Equal(Seed.Length, SharedFileLength());          // §14.9.27.4 GR25: a refused OPEN OUTPUT truncated nothing

        holder.Send("CLOSE");
        Assert.Equal("CLOSED 00", holder.ExpectLine());
        Assert.False(holder.HasExited);
        Assert.Equal(Every("00"), Run(prober));
        Assert.Equal(string.Empty, File.ReadAllText(SharedFile));  // the permitted OPEN OUTPUT did truncate: the file really was open to it

        holder.Send("EXIT");
        Assert.Equal("EXITING", holder.ExpectLine());
        Assert.Equal(0, holder.WaitForExit());
    }

    /// <summary>The lock must not over-refuse: where Table 19 (§14.9.27.4) prints <i>"Normal open"</i> a second process's OPEN
    /// is a success BESIDE a live holder, and where it prints <i>"Unsuccessful open"</i> it is '61' — read from the printed
    /// table for a holder in SHARING WITH ALL OTHER: an ALL OTHER request is normal for INPUT, I-O and EXTEND and
    /// unsuccessful for OUTPUT; a READ ONLY or NO OTHER request is unsuccessful for every mode against an I-O
    /// holder, and a READ ONLY request is normal for INPUT, I-O and EXTEND against an INPUT holder.</summary>
    [Fact]
    public void AnOpenThatTable19Permits_SucceedsBesideALiveHolderInAnotherProcess_AndOneItRefusesDoesNot()
    {
        SeedFile("shared.dat");
        string[] readModes = ["INPUT", "I-O", "EXTEND"];
        string Sharing(string name, string sharing, string[] modes) =>
            _programs.Build(name, Prober(Id(name), true, false, OpenEach(modes), sharing));

        string allOther = Sharing("pb2485_open_allother", "ALL OTHER", EveryOpenMode);
        string readOnly = Sharing("pb2485_open_readonly", "READ ONLY", readModes);
        string noOther = Sharing("pb2485_open_noother", "NO OTHER", readModes);

        using (var ioHolder = StartHolder(_programs.Build("pb2485_holder_allother_io",
                   Holder("PB2485HAO", "shared.dat", "ALL OTHER", "I-O"))))
        {
            Assert.Equal(["INPUT 00", "I-O 00", "EXTEND 00", "OUTPUT 61"], Run(allOther));
            Assert.Equal(Every("61", readModes), Run(readOnly));
            Assert.Equal(Every("61", readModes), Run(noOther));
            Assert.Equal(Seed.Length, SharedFileLength());
        }

        using var inputHolder = StartHolder(_programs.Build("pb2485_holder_allother_input",
            Holder("PB2485HAI", "shared.dat", "ALL OTHER", "INPUT")));
        Assert.Equal(Every("00", readModes), Run(readOnly));
    }

    // ── 2. The lock dies with its holder, however the holder ends ────────────────────────────────────────────────────

    /// <summary>§14.6.11 2): the holder's run unit ends with the file still open, and the implicit CLOSE it runs gives the
    /// lock back — the very next OPEN of a second process is a success in every mode.</summary>
    [Fact]
    public void TheImplicitCloseAtTheEndOfTheHoldersRunUnitGivesTheLockBack()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = Open("pb2485_open_each");
        using var holder = StartHolder(holderDll);
        Assert.Equal(Every("61"), Run(prober));

        holder.Send("EXIT");
        Assert.Equal("EXITING", holder.ExpectLine());
        Assert.Equal(0, holder.WaitForExit());
        Assert.Equal(Every("00"), Run(prober));
    }

    /// <summary>§14.6.12: the holder's run unit ends ABNORMALLY — a call of a program that does not exist, with no
    /// handler, is a fatal exception (§14.9.4.4 GR3) — and the runtime performs the termination operations it can;
    /// the lock does not outlive the run unit.</summary>
    [Fact]
    public void AnAbnormalEndOfTheHoldersRunUnitGivesTheLockBack()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = Open("pb2485_open_each");
        using var holder = StartHolder(holderDll);
        Assert.Equal(Every("61"), Run(prober));

        holder.Send("ABORT");
        Assert.NotEqual(0, holder.WaitForExit());
        Assert.Contains("NOSUCHPG", holder.Stderr, StringComparison.Ordinal);
        Assert.Equal(Every("00"), Run(prober));
    }

    /// <summary>A holder that is KILLED runs no CLOSE and no run-unit termination at all, so only the operating system
    /// can give the lock back — the host fact DOC-A.1-75 documents (<i>"the lock ... dies with a run unit that
    /// terminates"</i>): an open-file-description lock or a handle goes with its process, and the macOS lock file's
    /// <c>flock</c> with its descriptor.</summary>
    [Fact]
    public void AKilledHoldersLockDiesWithItsProcess()
    {
        SeedFile("shared.dat");
        string holderDll = _programs.Build("pb2485_holder_readonly", Holder("PB2485HRO", "shared.dat", "READ ONLY", "EXTEND"));
        string prober = Open("pb2485_open_ro_each", modes: ["EXTEND", "I-O", "OUTPUT"]);
        using var holder = StartHolder(holderDll);

        // §9.1.13.9 1) c) and e): the file is open in the sharing with read only mode by another file connector.
        Assert.Equal(Every("61", ["EXTEND", "I-O", "OUTPUT"]), Run(prober));
        Assert.Equal(Seed.Length, SharedFileLength());

        holder.Kill();
        Assert.Equal(Every("00", ["EXTEND", "I-O", "OUTPUT"]), Run(prober));
    }

    // ── 3. A descendant of a holder does not keep the lock alive ──────────────────────────────────────────────────────

    /// <summary>⛔ A CHILD PROCESS DOES NOT INHERIT THE LOCK (the <c>O_CLOEXEC</c> guard of <c>OfdRegionLocks.Open</c>, and
    /// a handle that is not inheritable on Windows). A run unit that starts a program — <c>CALL "SYSTEM"</c> in
    /// COBOL — does it with the file open; were the descriptor inherited, the child would hold the lock through the
    /// parent's CLOSE and §9.1.15 3)'s <i>"removed by an explicit or implicit CLOSE"</i> would be false for as long as
    /// the child lived. The holder here is this test process, a run unit in its own right through
    /// <see cref="FileRegistry"/>; the child is started FROM it while it holds, and outlives the CLOSE.</summary>
    [Fact]
    public void AChildStartedWhileTheLockIsHeld_DoesNotKeepItAfterTheClose()
    {
        SeedFile("shared.dat");
        SeedFile("child.dat");
        string childDll = _programs.Build("pb2485_holder_child", Holder("PB2485CHD", "child.dat", "NO OTHER", "INPUT"));
        string prober = Open("pb2485_open_each");

        var parent = new FileRegistry();
        parent.Register("A", SharedFile, recordWidth: 8, lineSequential: false, optional: false, varyMin: -1, varyMax: -1);
        parent.RegisterSharing("A", FileSharing.NoOther, FileLockMode.Manual, multiple: false);
        try
        {
            parent.OpenStatic("A", FileOpenMode.IO);
            Assert.Equal(FileStatusCode.Success, parent.Status("A"));

            using var child = StartHolder(childDll);            // started with the lock held, and alive until the end of the test
            Assert.Equal(Every("61"), Run(prober));             // the lock held by THIS process binds the other one

            parent.Close("A");
            Assert.False(child.HasExited);
            Assert.Equal(Every("00"), Run(prober));             // ... and the child, still alive, is not holding it
        }
        finally { parent.CloseAll(); }
    }

    // ── 4. The refusal reaches the program's handlers as the standard says ───────────────────────────────────────────

    /// <summary>§9.1.12 and §9.1.13.1: the '61' is a file-sharing exception condition (EC-I-O-FILE-SHARING, §9.1.13's
    /// '6'); a FILE STATUS item receives it, an applicable USE declarative runs, a '6x' status is not a fatal class — so
    /// with NEITHER handler the statement is simply unsuccessful and execution continues — and the declarative runs
    /// only for the refusal, never after the holder is gone.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ARefusedOpen_ReachesTheProgramsHandlers_WithAndWithoutAFileStatusAndADeclarative(bool fileStatus, bool declarative)
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string name = $"pb2485_handlers_{(fileStatus ? "fs" : "nofs")}_{(declarative ? "use" : "nouse")}";
        string prober = _programs.Build(name, Prober(Id(name), fileStatus, declarative,
        [
            "           OPEN INPUT F.",
            fileStatus ? "           DISPLAY \"OPEN \" ST." : "           DISPLAY \"OPEN CONTINUES\".",
            "           DISPLAY \"AFTER\".",
        ]));
        using var holder = StartHolder(holderDll);

        var expectedRefused = new List<string>();
        if (declarative) expectedRefused.AddRange(["DECL [EC-I-O-FILE-SHARING]", "FILE [61F]"]);
        expectedRefused.Add(fileStatus ? "OPEN 61" : "OPEN CONTINUES");
        expectedRefused.Add("AFTER");
        Assert.Equal(expectedRefused.ToArray(), Run(prober));

        holder.Kill();
        Assert.Equal(new[] { fileStatus ? "OPEN 00" : "OPEN CONTINUES", "AFTER" }, Run(prober));
    }

    // ── 5. DELETE FILE is refused the same way (§9.1.13.9 2) ────────────────────────────────────────────────────────

    /// <summary>§9.1.13.9 2): a DELETE FILE of a physical file another run unit holds is '62', through the ON EXCEPTION
    /// phrase, through the FILE STATUS item alone and through a USE declarative alone (the declarative runs only when
    /// the statement has no ON EXCEPTION phrase, §9.1.13.1's EC-I-O paragraph); the file is still there; and once the
    /// holder is gone the same DELETE FILE is successful and the file is deleted.</summary>
    [Fact]
    public void ARefusedDeleteFileIsStatus62_ThroughEveryHandler_AndSucceedsOnceTheHolderIsGone()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string onException = _programs.Build("pb2485_delete_onexception", Prober("PB2485DEL1", true, false,
        [
            "           DELETE FILE F",
            "               ON EXCEPTION DISPLAY \"DELEXC \" ST",
            "               NOT ON EXCEPTION DISPLAY \"DELOK \" ST",
            "           END-DELETE.",
        ]));
        string statusOnly = _programs.Build("pb2485_delete_status", Prober("PB2485DEL2", true, false,
        [
            "           DELETE FILE F.",
            "           DISPLAY \"DEL \" ST.",
        ]));
        string declarative = _programs.Build("pb2485_delete_declarative", Prober("PB2485DEL3", false, true,
        [
            "           DELETE FILE F.",
            "           DISPLAY \"AFTER\".",
        ]));
        using var holder = StartHolder(holderDll);

        Assert.Equal(["DELEXC 62"], Run(onException));
        Assert.Equal(["DEL 62"], Run(statusOnly));
        Assert.Equal(["DECL [EC-I-O-FILE-SHARING]", "FILE [62F]", "AFTER"], Run(declarative));
        Assert.Equal(Seed.Length, SharedFileLength());          // §9.1.13.9 2): refused, so the file is still there, untouched

        holder.Kill();
        Assert.Equal(["DELOK 00"], Run(onException));
        Assert.False(File.Exists(SharedFile));
        SeedFile("shared.dat");
        Assert.Equal(["DEL 00"], Run(statusOnly));
        Assert.False(File.Exists(SharedFile));
    }

    /// <summary>§9.1.12 and §9.1.13.1 for the '62' (§9.1.13.9 2)) as for the '61' above: a DELETE FILE with NO ON EXCEPTION
    /// phrase reaches a FILE STATUS item alone, a USE declarative alone, both (the declarative runs and the item receives
    /// the status) and NEITHER — a '6x' status is not a fatal class, so with no handler at all the run unit continues
    /// with the file untouched; and once the holder is gone the same statement deletes the file.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ARefusedDeleteFile_ReachesTheProgramsHandlers_WithAndWithoutAFileStatusAndADeclarative(bool fileStatus, bool declarative)
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string name = $"pb2485_delhandlers_{(fileStatus ? "fs" : "nofs")}_{(declarative ? "use" : "nouse")}";
        string prober = _programs.Build(name, Prober(Id(name), fileStatus, declarative,
        [
            "           DELETE FILE F.",
            fileStatus ? "           DISPLAY \"DEL \" ST." : "           DISPLAY \"DEL CONTINUES\".",
            "           DISPLAY \"AFTER\".",
        ]));
        using var holder = StartHolder(holderDll);

        var expectedRefused = new List<string>();
        if (declarative) expectedRefused.AddRange(["DECL [EC-I-O-FILE-SHARING]", "FILE [62F]"]);
        expectedRefused.Add(fileStatus ? "DEL 62" : "DEL CONTINUES");
        expectedRefused.Add("AFTER");
        Assert.Equal(expectedRefused.ToArray(), Run(prober));
        Assert.Equal(Seed.Length, SharedFileLength());          // §9.1.13.9 2): the file is still there, untouched

        holder.Kill();
        Assert.Equal(new[] { fileStatus ? "DEL 00" : "DEL CONTINUES", "AFTER" }, Run(prober));
        Assert.False(File.Exists(SharedFile));
    }

    // ── 6. RETRY (§14.7.9.3) ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>§14.7.9.3 4) b): a RETRY phrase attempts the OPEN again, and when the holder outlasts every attempt
    /// <i>"the statement is unsuccessful, the appropriate value is placed in the I-O status"</i>: '61' for each mode, the
    /// file untouched — and after the holder's end the same RETRY OPENs succeed at once.</summary>
    [Fact]
    public void ARetryThatTheHolderOutlasts_IsStatus61_AndSucceedsOnceTheHolderIsGone()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = Open("pb2485_open_retry_each", retry: "RETRY 2 TIMES ");
        using var holder = StartHolder(holderDll);

        Assert.Equal(Every("61"), Run(prober));
        Assert.Equal(Seed.Length, SharedFileLength());

        holder.Kill();
        Assert.Equal(Every("00"), Run(prober));
    }

    /// <summary>§14.7.9.3 4) b) for DELETE FILE: a RETRY phrase attempts the statement again, and a holder that outlasts
    /// every attempt leaves it unsuccessful with '62' (§9.1.13.9 2)) and the file untouched; after the holder's end the
    /// same RETRY statement deletes the file at once.</summary>
    [Fact]
    public void ADeleteFileRetryThatTheHolderOutlasts_IsStatus62_AndSucceedsOnceTheHolderIsGone()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = _programs.Build("pb2485_delete_retry", Prober("PB2485DELR", true, false,
        [
            "           DELETE FILE F RETRY 2 TIMES.",
            "           DISPLAY \"DELR \" ST.",
        ]));
        using var holder = StartHolder(holderDll);

        Assert.Equal(["DELR 62"], Run(prober));
        Assert.Equal(Seed.Length, SharedFileLength());

        holder.Kill();
        Assert.Equal(["DELR 00"], Run(prober));
        Assert.False(File.Exists(SharedFile));
    }

    /// <summary>§14.7.9.3 3) and 4): <i>"If the FOREVER phrase is specified, the mass storage control system shall attempt
    /// to gain access to a locked resource until the input-output operation has been completed"</i>, and a permitted
    /// attempt makes the statement successful <i>"as if the file sharing ... conflict had never occurred"</i>. The second
    /// run unit asks while the holder holds and is released by the holder's CLOSE. ⚠ The prober prints <c>TRYING</c>
    /// before its OPEN and the holder is released only after that line, so the OPEN's first attempt is expected to
    /// meet the lock; a first attempt that came after the CLOSE would succeed at once, so this witness can pass for
    /// the wrong reason but can never fail for one — and §14.7.9.3 4) is what makes '61' the wrong answer here.</summary>
    [Fact]
    public void ARetryForeverSucceedsOnceTheHolderClosesTheFile()
    {
        SeedFile("shared.dat");
        string holderDll = NoOtherHolder();
        string prober = _programs.Build("pb2485_open_forever", Prober("PB2485FOR", true, false,
        [
            "           DISPLAY \"TRYING\".",
            "           OPEN I-O RETRY FOREVER F.",
            "           DISPLAY \"OPEN \" ST.",
            "           IF ST = \"00\" CLOSE F END-IF.",
        ]));
        using var holder = StartHolder(holderDll);
        using var retrying = LiveProcess.Start(Dotnet(prober));
        Assert.Equal("TRYING", retrying.ExpectLine());

        holder.Send("CLOSE");
        Assert.Equal("CLOSED 00", holder.ExpectLine());
        Assert.Equal("OPEN 00", retrying.ExpectLine());
        Assert.Equal(0, retrying.WaitForExit());
    }
}
