// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Runtime.InteropServices;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.27.4 GR3 — <i>"If the file associated with file-name-1 is present and insufficient authority
/// exists to open the file, the execution of the OPEN statement is unsuccessful, and the I-O status value in
/// the file connector referenced by file-name-1 is set to '37'."</i> (§9.1.13.6 item 6 b) restates it for OPEN
/// and DELETE FILE together and adds <i>"The ability to detect this is processor dependent"</i>.)
/// <para>These are the arms no conformance golden can reach: the corpus runner compiles a <c>.cob</c> and
/// compares its output, with nowhere to express "and first make this file unreadable", so the ONLY place the
/// authority precondition can be established is a test that builds it. kb/Work PB323 is what the gap cost —
/// every <c>OpenCore</c> asked <c>File.Exists</c>, which swallows an access error and answers <c>false</c>, so
/// a present file the process may not read was reported as '35' (not present) and, for an OPTIONAL file, as a
/// SUCCESSFUL '05' open over an invented empty file whose first READ hit at end.</para>
/// <para>The denial itself is real, not simulated: a DACL deny ACE on Windows, mode 000 on Unix. The helper
/// PROVES the denial took — with a raw <c>File.OpenRead</c>, never with <see cref="HostFile.Probe"/>, because
/// checking a precondition with the subject under test lets a broken subject certify its own premise.</para>
/// <para>⛔ THE ONE ESCAPE HATCH, AND WHY IT CANNOT ROT INTO A SILENT GREEN. A process running as root cannot
/// be denied by mode bits at all, so on such a host these assertions have nothing to assert. That case is not
/// waved through: when the denial fails, the helper asserts POSITIVELY that this process is permission-
/// bypassing, so a deny helper that simply stopped working goes RED on every ordinary account instead of
/// quietly passing (feedback: green_gates_arent_evidence). <see cref="TheDenyHelperWorksOnThisHost"/> reports
/// the classification as a test of its own, so the hatch is visible in the run rather than buried in a
/// branch.</para>
/// </summary>
public sealed class FileAuthorityPresenceTests : IDisposable
{
    private const int SeqAccess = 0;   // KeyedAccess.Sequential

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"pb323-{Guid.NewGuid():N}");

    public FileAuthorityPresenceTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            Restore(_root);
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // ── The presence probe itself (HostFile.Probe) ───────────────────────────────────────────────────────────

    /// <summary>§9.1.13.6 item 5's "the physical file is not present" — the answer that earns '35'.</summary>
    [Fact]
    public void Probe_MissingFile_IsAbsent() =>
        Assert.Equal(FilePresence.Absent, HostFile.Probe(Path.Combine(_root, "nothing.dat")));

    /// <summary>A path the host will not accept as a file name is a statement about the PATH, not an
    /// input-output failure: it names no present file, so it is Absent (§9.1.13.6 item 5) and not §9.1.13.6
    /// item 1's '30'. This is also the answer <c>File.Exists</c> gave, so no OPEN changed behaviour when the
    /// probe replaced it — the pin is here because <c>File.GetAttributes</c>, unlike <c>File.Exists</c>,
    /// THROWS for these and the mapping had to be chosen rather than inherited.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Probe_APathTheHostWillNotAccept_IsAbsent(string path) =>
        Assert.Equal(FilePresence.Absent, HostFile.Probe(path));

    [Fact]
    public void Probe_APathLongerThanTheHostAllows_IsAbsent() =>
        Assert.Equal(FilePresence.Absent,
            HostFile.Probe(Path.Combine(_root, new string('x', 400), new string('y', 400))));

    [Fact]
    public void Probe_ExistingFile_IsPresent()
    {
        string p = Path.Combine(_root, "there.dat");
        File.WriteAllText(p, "X");
        Assert.Equal(FilePresence.Present, HostFile.Probe(p));
    }

    /// <summary>A DIRECTORY at the ASSIGN path is not the §9.1.6 <i>physical file</i>, so "the physical file is
    /// not present" is the true statement about it — the same answer <c>File.Exists</c> gave, which is why the
    /// PB323 refactor left every OPEN of such a path at '35'.</summary>
    [Fact]
    public void Probe_DirectoryAtThePath_IsAbsent()
    {
        string p = Path.Combine(_root, "adir");
        Directory.CreateDirectory(p);
        Assert.Equal(FilePresence.Absent, HostFile.Probe(p));
    }

    /// <summary>⛔ THE WHOLE DEFECT IN ONE ASSERTION. <c>File.Exists</c> answers <c>false</c> here — asserted
    /// alongside, so the test states what it is discriminating against and cannot quietly stop discriminating.
    /// A refusal is not evidence of absence.</summary>
    [Fact]
    public void Probe_PresentButRefused_IsUnauthorized_WhereFileExistsSaysAbsent()
    {
        if (Refused() is not { } witness) return;
        Assert.False(File.Exists(witness));                                  // the two-valued probe's lie
        Assert.Equal(FilePresence.Unauthorized, HostFile.Probe(witness));    // the three-valued truth
    }

    /// <summary>The escape hatch, made visible. On an ordinary account this asserts that the deny helper
    /// really can establish the GR3 precondition — so a helper that silently stopped denying turns THIS test
    /// red rather than turning every GR3 test into a vacuous pass.</summary>
    [Fact]
    public void TheDenyHelperWorksOnThisHost()
    {
        bool refused = Refused() is not null;
        Assert.True(refused || BypassesFilePermissions(),
            "The deny helper could not make a file unreadable, and this process is NOT running with a "
            + "permission bypass — so the helper is broken, not the environment, and every §14.9.27.4 GR3 "
            + "assertion in this class is currently proving nothing.");
    }

    // ── GR3 on the OPEN statement, every organization, OPTIONAL and not ──────────────────────────────────────

    /// <summary>GR3 is not conditioned on the OPTIONAL clause, and that is the silent half of the defect:
    /// GR13's at-end arm opens with <i>"If the file is NOT present"</i>, so an OPTIONAL file that IS present
    /// and merely refused must never take it. Before PB323 the non-optional legs answered '35' and the OPTIONAL
    /// legs answered a SUCCESSFUL '05'.</summary>
    [Theory]
    [InlineData("seq", false)]
    [InlineData("seq", true)]
    [InlineData("rel", false)]
    [InlineData("rel", true)]
    [InlineData("idx", false)]
    [InlineData("idx", true)]
    public void OpenInput_PresentButRefused_Is37(string organization, bool optional)
    {
        if (Refused() is not { } witness) return;
        var reg = Register(organization, witness, optional);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>GR17's creation arm carries the SAME "If the file is not present" precondition as GR13, so a
    /// refused I-O or EXTEND target is '37' and is NOT created — an unsuccessful OPEN leaves the file
    /// unaffected (GR25).</summary>
    [Theory]
    [InlineData(FileOpenMode.IO, false)]
    [InlineData(FileOpenMode.IO, true)]
    [InlineData(FileOpenMode.Extend, false)]
    [InlineData(FileOpenMode.Extend, true)]
    public void OpenIoOrExtend_PresentButRefused_Is37(FileOpenMode mode, bool optional)
    {
        if (Refused() is not { } witness) return;
        var reg = Register("seq", witness, optional);
        reg.OpenStatic("F", mode);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ THE OVER-FIRE GUARD, and the reason GR3's short-circuit deliberately excludes OUTPUT.
    /// §14.9.27.4 GR18 makes OUTPUT a CREATION that never consults presence, and §9.1.13.6 item 5's '35' is
    /// defined only <i>"for an OPEN statement with the INPUT, I-O, or EXTEND phrase"</i> — so a directory this
    /// process may WRITE but not LIST legitimately accepts a new file. The probe answers Unauthorized for that
    /// path; a blanket '37' would reject an OPEN OUTPUT the operating environment carries out perfectly.</summary>
    [Fact]
    public void OpenOutput_NewFileUnderARefusedDirectory_StillSucceeds()
    {
        if (Refused() is null) return;
        string fresh = Path.Combine(_root, "locked", "brand-new.dat");
        if (!RawCreateSucceeds(fresh)) return;   // a host that refuses creation too has no case to guard here
        var reg = Register("seq", fresh, optional: false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>An OUTPUT over a file that IS present and refused is still '37' — GR3's answer, reached through
    /// the creating stream's own <c>UnauthorizedAccessException</c> and <c>FileConnector.Open</c>'s catch rather
    /// than through the presence short-circuit. Both routes, one status.</summary>
    [Fact]
    public void OpenOutput_OverAPresentRefusedFile_Is37()
    {
        if (Refused() is not { } witness) return;
        var reg = Register("seq", witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("37", reg.Status("F"));
    }

    // ── GR16's write capability: the SAME file, the SAME answer, every organization (kb/Work PB328) ──────────

    /// <summary>⛔ THE NINE CELLS. §14.9.27.4 GR16 — <i>"If the I-O phrase is specified, the file shall support
    /// the input and output statements that are permitted for the organization of that file when opened in the
    /// I-O mode. If the file does not support those statements, the I-O status value for file-name-1 is set to
    /// '37' and the execution of the OPEN statement is unsuccessful."</i> §9.1.13.6 item 6 a) prices the other
    /// two write modes identically: 1. <i>"the EXTEND or OUTPUT phrase is specified but the file will not
    /// support write operations"</i>, 2. the I-O restatement of GR16.
    /// <para>Neither rule names an organization, and Table 20 is why they cannot: REWRITE sits under the I-O
    /// column for sequential, random AND dynamic access, so "supports the statements permitted in the I-O mode"
    /// entails write capability whatever the organization. kb/Work PB328 was the three organizations
    /// disagreeing about that on ONE file — the sequential arm answered '37' because its I-O and EXTEND streams
    /// happen to be write opens, while the relative and indexed arms only read their store and answered '00',
    /// after which READ and REWRITE both reported '00' and the loss surfaced as a '30' at CLOSE on a file that
    /// was byte-for-byte unchanged.</para></summary>
    [Theory]
    [InlineData("seq", FileOpenMode.IO)]
    [InlineData("seq", FileOpenMode.Extend)]
    [InlineData("seq", FileOpenMode.Output)]
    [InlineData("rel", FileOpenMode.IO)]
    [InlineData("rel", FileOpenMode.Extend)]
    [InlineData("rel", FileOpenMode.Output)]
    [InlineData("idx", FileOpenMode.IO)]
    [InlineData("idx", FileOpenMode.Extend)]
    [InlineData("idx", FileOpenMode.Output)]
    public void OpenAWriteMode_PresentButNotWritable_Is37(string organization, FileOpenMode mode)
    {
        if (NotWritable() is not { } witness) return;
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", mode);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>The OPTIONAL clause does not enter GR16 or §9.1.13.6 item 6 a) at all, and that is the silent
    /// half: GR17's creation arm opens with <i>"If the file is NOT present"</i>, so a file that IS present and
    /// merely unwritable must never be treated as one to create.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentButNotWritable_OptionalFile_IsStill37(string organization)
    {
        if (NotWritable() is not { } witness) return;
        var reg = Register(organization, witness, optional: true);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>§14.9.27.4 GR25 — <i>"If the execution of the OPEN statement is unsuccessful, the file is not
    /// affected"</i>. The capability answer is obtained by really asking the host for a writable handle, so the
    /// probe itself has to be provably inert: <c>FileMode.Open</c> with no bytes written leaves content AND
    /// last-write time exactly as they were.</summary>
    [Fact]
    public void TheRefusedOpenLeavesTheFileUnaffected()
    {
        if (NotWritable() is not { } witness) return;
        byte[] before = File.ReadAllBytes(witness);
        DateTime stamp = File.GetLastWriteTimeUtc(witness);
        var reg = Register("idx", witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
        Assert.Equal(before, File.ReadAllBytes(witness));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(witness));
    }

    /// <summary>⛔ THE OVER-FIRE GUARD. A read-only file supports every statement Table 20 permits in the INPUT
    /// mode, and §9.1.13.6 item 6 a) 3. asks only whether <i>"the file will not support read operations"</i>.
    /// Probing WRITE capability for an INPUT open would refuse an OPEN the operating environment carries out
    /// perfectly — and reading a write-protected master file is one of the oldest idioms there is.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenInput_PresentButNotWritable_StillOpens(string organization)
    {
        if (NotWritable() is not { } witness) return;
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>The complement that keeps the '37' above from being unconditional: the same organization, the
    /// same mode, a file this process CAN write, still opens normally (Table 18, "file is available").</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentAndWritable_StillOpens(string organization)
    {
        string p = Path.Combine(_root, $"writable-{organization}.dat");
        var make = Register(organization, p, optional: false);
        make.OpenStatic("F", FileOpenMode.Output);
        Assert.Equal("00", make.Status("F"));
        make.Close("F");

        var reg = Register(organization, p, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("00", reg.Status("F"));
        reg.Close("F");
    }

    /// <summary>The write-capability probe against the PRESENCE probe, on one file: they are different
    /// questions and the file answers them differently. This is the assertion that states what the fix is
    /// discriminating against — before kb/Work PB328 the keyed OPEN arms had only the presence answer, which
    /// says "Present" here, and read it as permission to proceed.</summary>
    [Fact]
    public void PermitsWrite_SeesWhatThePresenceProbeCannot()
    {
        if (NotWritable() is not { } witness) return;
        Assert.Equal(FilePresence.Present, HostFile.Probe(witness));   // present, and observable
        Assert.False(HostFile.PermitsWrite(witness));                  // and still not writable
    }

    [Fact]
    public void PermitsWrite_AnOrdinaryFile_IsTrue()
    {
        string p = Path.Combine(_root, "ordinary.dat");
        File.WriteAllText(p, "HELLO-RECORD-0001");
        Assert.True(HostFile.PermitsWrite(p));
    }

    /// <summary>§9.1.13.6 item 6 a) 3. — <i>"the INPUT phrase is specified but the file will not support read
    /// operations"</i>, the third face of the same '37'. Its answer is NOT the write probe (which excludes
    /// INPUT on purpose) and NOT GR3's presence short-circuit (the file here is fully observable: the
    /// DIRECTORY is readable and only the FILE is refused, so <c>HostFile.Probe</c> says Present). It is the
    /// organization's own eager read — the sequential <c>StreamReader</c>, the keyed <c>Attach()</c> — whose
    /// <c>UnauthorizedAccessException</c> reaches the same '37' through <c>FileConnector.Open</c>'s catch.
    /// <para>⛔ ASSERTED BECAUSE IT WAS ASSUMED. <c>FileConnector.Open</c>'s GR16 comment justifies excluding
    /// INPUT from the write probe by claiming item 6 a) 3. is already answered; an exclusion resting on an
    /// unmeasured claim is how a gap ships. All three organizations were measured before the claim was
    /// written (feedback: reachability_is_measured_not_deduced).</para></summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenInput_PresentAndObservableButNotReadable_Is37(string organization)
    {
        if (NotReadable() is not { } witness) return;
        Assert.Equal(FilePresence.Present, HostFile.Probe(witness));   // not GR3's arm: the file IS observable
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.Input);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>⛔ GR16'S OTHER HALF, AND THE ANSWER TO "WHICH ARM DID YOU FIX?". §14.9.27.4 GR16 requires the
    /// file to support <i>"the input AND output statements that are permitted for the organization of that
    /// file when opened in the I-O mode"</i> — Table 20 lists READ under the I-O column alongside REWRITE — so
    /// a file this process may WRITE but not READ is '37' on OPEN I-O just as surely as a read-only one is.
    /// The write probe passes on this file by construction; the '37' therefore comes from the organization's
    /// own eager read, on every organization, and the two halves together cover the rule rather than the half
    /// that happened to be the defect.</summary>
    [Theory]
    [InlineData("seq")]
    [InlineData("rel")]
    [InlineData("idx")]
    public void OpenIo_PresentAndWritableButNotReadable_Is37(string organization)
    {
        if (NotReadable() is not { } witness) return;
        Assert.True(HostFile.PermitsWrite(witness),
            "The premise of this test is a file the WRITE probe accepts; if it does not, the '37' below could "
            + "be the write half answering and GR16's read half would still be unmeasured.");
        var reg = Register(organization, witness, optional: false);
        reg.OpenStatic("F", FileOpenMode.IO);
        Assert.Equal("37", reg.Status("F"));
    }

    /// <summary>The escape hatch for the read-only helper, made visible exactly as
    /// <see cref="TheDenyHelperWorksOnThisHost"/> is: on a host where this process cannot be denied write, the
    /// nine cells above assert nothing, and that has to go RED on an ordinary account rather than pass
    /// quietly.</summary>
    [Fact]
    public void TheCapabilityHelpersWorkOnThisHost()
    {
        bool bypass = BypassesFilePermissions();
        Assert.True(NotWritable() is not null || bypass,
            "The read-only helper could not make a file unwritable, and this process is NOT running with a "
            + "permission bypass — so the helper is broken, not the environment, and every §14.9.27.4 GR16 "
            + "assertion in this class is currently proving nothing.");
        Assert.True(NotReadable() is not null || bypass,
            "The unreadable-file helper could not refuse this process a read, and this process is NOT running "
            + "with a permission bypass — so §9.1.13.6 item 6 a) 3.'s assertions are currently proving nothing.");
    }

    // ── The DELETE FILE twin, now on the same probe (§14.9.10.4 GR14/GR16) ───────────────────────────────────

    /// <summary>§14.9.10.4 GR16's '37'. Correct since kb/Work PB140; pinned here because PB323 moved it onto
    /// the shared <see cref="HostFile.Probe"/> — the refactor has to preserve it.</summary>
    [Fact]
    public void DeleteFile_PresentButRefused_Is37()
    {
        if (Refused() is not { } witness) return;
        var reg = Register("seq", witness, optional: false);
        Assert.Equal("37", reg.DeleteFile("F"));
    }

    /// <summary>§14.9.10.4 GR14's '05' — a SUCCESSFUL completion for a file that is not present. A DIRECTORY at
    /// the path is not the §9.1.6 physical file, so it takes the absent arm; before PB323 unified the probe,
    /// <c>File.Delete</c>'s refusal made this a factually wrong '37' ("insufficient authority").</summary>
    [Fact]
    public void DeleteFile_DirectoryAtThePath_Is05()
    {
        string p = Path.Combine(_root, "notafile");
        Directory.CreateDirectory(p);
        var reg = Register("seq", p, optional: false);
        Assert.Equal("05", reg.DeleteFile("F"));
        Assert.True(Directory.Exists(p), "GR14's '05' means the FILE was not there; the directory is untouched.");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────────────────────────────────

    private static FileRegistry Register(string organization, string host, bool optional)
    {
        var reg = new FileRegistry();
        switch (organization)
        {
            case "seq": reg.Register("F", host, 17, false, optional, -1, -1); break;
            case "rel": reg.RegisterRelative("F", host, 17, optional, SeqAccess, 4, -1, -1); break;
            case "idx": reg.RegisterIndexed("F", host, 17, optional, SeqAccess, 0, 4, -1, -1); break;
            default: throw new ArgumentOutOfRangeException(nameof(organization), organization, "seq|rel|idx");
        }
        return reg;
    }

    /// <summary>Create <c>locked/secret.dat</c>, make its directory unreadable to this process, and return the
    /// file's path — the "present and insufficient authority" precondition of GR3, actually established — or
    /// <c>null</c> when this host cannot deny anything to this process.
    /// <para>The denial is verified with a RAW <c>File.OpenRead</c>, never with <see cref="HostFile.Probe"/>:
    /// checking the precondition with the subject under test would let a broken subject certify its own
    /// premise. A <c>null</c> return is asserted about, not assumed — see
    /// <see cref="TheDenyHelperWorksOnThisHost"/>.</para></summary>
    private string? Refused()
    {
        string dir = Path.Combine(_root, "locked");
        Directory.CreateDirectory(dir);
        string witness = Path.Combine(dir, "secret.dat");
        if (!File.Exists(witness)) File.WriteAllText(witness, "HELLO-RECORD-0001");
        Deny(dir);

        try { using (File.OpenRead(witness)) { } }
        catch (UnauthorizedAccessException) { return witness; }
        catch (IOException) { }
        return null;
    }

    /// <summary>Create <c>readonly.dat</c>, make it unwritable to this process while leaving it PRESENT and
    /// fully observable, and return its path — the precondition of §14.9.27.4 GR16 and §9.1.13.6 item 6 a),
    /// actually established — or <c>null</c> when this host cannot deny writing to this process.
    /// <para>This is deliberately NOT <see cref="Refused"/>'s directory denial: that one hides the file from
    /// the presence probe, which is GR3's precondition and a different rule. GR16's file is one the process can
    /// see, stat and read perfectly — the '37' has to come from the write capability alone, so the two probes
    /// must give different answers on it. The denial is verified with a RAW write open, never with
    /// <c>HostFile.PermitsWrite</c>: checking the precondition with the subject under test would let a broken
    /// subject certify its own premise. A <c>null</c> return is asserted about, not assumed — see
    /// <see cref="TheReadOnlyHelperWorksOnThisHost"/>.</para></summary>
    private string? NotWritable()
    {
        string witness = Path.Combine(_root, "readonly.dat");
        if (!File.Exists(witness)) File.WriteAllText(witness, "HELLO-RECORD-0001");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) File.SetAttributes(witness, FileAttributes.ReadOnly);
        else File.SetUnixFileMode(witness, UnixFileMode.UserRead);

        try { using (new FileStream(witness, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { } }
        catch (UnauthorizedAccessException) { return witness; }
        catch (IOException) { }
        return null;
    }

    /// <summary>Create <c>unreadable.dat</c> as a WRITE-ONLY file — refused read, still writable, and with its
    /// directory listable so the presence probe answers <c>Present</c>. It is the exact complement of
    /// <see cref="NotWritable"/>, and having both is what keeps §14.9.27.4 GR16's two halves separable: GR16
    /// requires the file to support <i>"the input AND output statements"</i> the I-O mode permits, so a file
    /// that fails EITHER half is '37' and a probe that only asked about writing would answer half a rule.
    /// <para>Deliberately not <see cref="Refused"/>'s shape: there the DIRECTORY is denied, so the file is
    /// invisible and the answer comes from GR3's presence short-circuit. Here <c>File.GetAttributes</c>
    /// succeeds, so the '37' can only come from the organization's own read. On Windows the deny ACE is
    /// <c>(RD)</c> — read DATA only; a blanket <c>(R)</c> takes the synchronize/read-control rights a write
    /// open also needs and would silently make the file unwritable too, collapsing the complement. Verified
    /// with a raw <c>File.OpenRead</c> and a raw write open, never with the subject under test. Returns
    /// <c>null</c> when this host cannot deny reading to this process.</para></summary>
    private string? NotReadable()
    {
        string witness = Path.Combine(_root, "unreadable.dat");
        if (!File.Exists(witness)) File.WriteAllText(witness, "HELLO-RECORD-0001");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Icacls(witness, "/deny", $"{Environment.UserDomainName}\\{Environment.UserName}:(RD)");
        else
            File.SetUnixFileMode(witness, UnixFileMode.UserWrite);

        try { using (File.OpenRead(witness)) { } }
        catch (UnauthorizedAccessException) { return witness; }
        catch (IOException) { }
        return null;
    }

    /// <summary>Whether this process bypasses file permissions outright — root on Unix, where mode bits do not
    /// apply at all. (A merely ELEVATED Windows token does not bypass an explicit deny ACE for its own SID, so
    /// Windows has no bypass case here.)</summary>
    private static bool BypassesFilePermissions() =>
        !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        && string.Equals(Environment.UserName, "root", StringComparison.Ordinal);

    /// <summary>Whether the host really does allow creating a NEW file under the refused directory — the
    /// premise of the OPEN OUTPUT guard. Unix mode 000 removes write along with read, so only the Windows
    /// deny-RX shape has an unreadable-but-writable directory for GR18's creation arm to be guarded in.</summary>
    private static bool RawCreateSucceeds(string path)
    {
        try
        {
            using (File.Create(path)) { }
            File.Delete(path);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (IOException) { return false; }
    }

    private static void Deny(string dir)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Icacls(dir, "/inheritance:r", "/grant:r", $"{who}:(F)");
            Icacls(dir, "/deny", $"{who}:(RX)");
        }
        else
        {
            File.SetUnixFileMode(dir, UnixFileMode.None);
        }
    }

    private static void Restore(string root)
    {
        // The GR16 witness is read-only, and a read-only file defeats the recursive Directory.Delete below on
        // Windows — the temp tree would then leak one directory per run.
        string ro = Path.Combine(root, "readonly.dat");
        if (File.Exists(ro))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) File.SetAttributes(ro, FileAttributes.Normal);
            else File.SetUnixFileMode(ro, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        string nr = Path.Combine(root, "unreadable.dat");
        if (File.Exists(nr))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Icacls(nr, "/remove:d", $"{Environment.UserDomainName}\\{Environment.UserName}");
            else
                File.SetUnixFileMode(nr, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        string dir = Path.Combine(root, "locked");
        if (!Directory.Exists(dir)) return;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Icacls(dir, "/remove:d", who);
            Icacls(dir, "/inheritance:e");
        }
        else
        {
            File.SetUnixFileMode(dir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>Run <c>icacls</c> through <see cref="ProcessObserver"/> — the ONE child-process seam
    /// (<c>ProcessObservationDriftTests</c>). A private <c>WaitForExit(n)</c> here would report a contention
    /// timeout as a silently-unchanged DACL, and the tests above would then read the missing denial as a
    /// conforming '35'. The observer's budget applies; the exit code is deliberately not asserted, because
    /// whether the denial actually took is settled by a raw <c>File.OpenRead</c> in <see cref="Refused"/> and
    /// not by what icacls said about it.</summary>
    private static void Icacls(string target, params string[] args)
    {
        var psi = new ProcessStartInfo("icacls")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(target);
        foreach (string a in args) psi.ArgumentList.Add(a);
        ProcessObserver.ObserveOrThrow(psi);
    }
}
