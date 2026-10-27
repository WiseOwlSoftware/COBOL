// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY CREATION DECIDED ON AN ABSENT PRESENCE PROBE TAKES A HANDLE OPENED <c>FileMode.CreateNew</c>, IN EVERY ORGANIZATION (kb/Work
/// PB3635; DESIGN-runtime-library.md 2.2.2.14). Two creations are decided that way: the WRITE that creates the file again after its run
/// unit's ROLLBACK removed it, and the OPEN that creates it (OUTPUT of an absent file, I-O or EXTEND of an absent OPTIONAL one,
/// §14.9.27.4 GR17, GR18). A file another run unit made at the path since the ROLLBACK is that run unit's (§9.1.17), so the WRITE answers
/// '30' (§9.1.13.6 item 1) rather than appending to it, replacing it or journaling it as its own creation; a file made between an OPEN's
/// probe and its handle is met by an OPEN that is made again over it (<c>FileConnector.Open</c>). The probe alone leaves a window —
/// another run unit's OPEN OUTPUT takes no entry mutex — which only a creating handle that refuses an existing file closes, so a creation
/// spelled <c>Append</c>, <c>OpenOrCreate</c> or <c>Create</c> is the defect again. The behavioural witnesses (SequentialCommitControlTests,
/// CommitRollbackRuntimeTests) interleave the second run unit through the fault-injecting medium's <c>BeforeOperation</c> seam wherever the
/// medium opens the handle; this guards the arms no seam reaches (an uncovered keyed OPEN opens through the host directly).
/// </summary>
public sealed class FileCreatedAgainDriftTests
{
    private static string BodyOf(string file, string signature)
    {
        string text = File.ReadAllText(file);
        int start = text.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{Path.GetFileName(file)}: the member '{signature}' moved or was renamed; update this guard with it");
        int end = text.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        Assert.True(end > start, $"{Path.GetFileName(file)}: the end of '{signature}' was not found");
        return text[start..end];
    }

    [Fact]
    public void TheSequentialCreationProbesThenTakesACreateNewHandle()
    {
        string body = BodyOf(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "SequentialConnector.CommitControl.cs"), "private string? CreateTheFileAgain()");
        Assert.Contains("HostFile.Probe(HostPath) is not FilePresence.Absent", body, StringComparison.Ordinal);
        Assert.Contains("TakeHeld(FileMode.CreateNew,", body, StringComparison.Ordinal);
        Assert.DoesNotContain("FileMode.OpenOrCreate", body, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenWriter(FileMode.Create)", body, StringComparison.Ordinal);
    }

    [Fact]
    public void TheKeyedCreationProbesThenTakesACreateNewHandle()
    {
        string file = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "KeyedConnector.cs");
        string again = BodyOf(file, "private string? CreateTheFileAgain()");
        Assert.Contains("HostFile.Probe(HostPath) is FilePresence.Absent", again, StringComparison.Ordinal);
        Assert.Contains("LockCreating(newFileOnly: true)", again, StringComparison.Ordinal);
        string handle = BodyOf(file, "private void TakeFileLock(bool create, FileAccess access, bool newFileOnly)");
        Assert.Contains("newFileOnly ? FileMode.CreateNew : FileMode.OpenOrCreate", handle, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSequentialOpenCreatesThroughACreateNewHandleWhenTheProbeSaidAbsent()
    {
        string body = BodyOf(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "SequentialConnector.cs"), "protected override string OpenCore(FileOpenMode mode, FilePresence presence)");
        Assert.Contains("bool absent = presence is FilePresence.Absent;", body, StringComparison.Ordinal);
        Assert.Contains("OpenWriter(absent ? FileMode.CreateNew : FileMode.Create)", body, StringComparison.Ordinal);
        Assert.Contains("OpenWriter(absent ? FileMode.CreateNew : FileMode.Append)", body, StringComparison.Ordinal);
        Assert.Contains("OpenAuxiliary(HostPath, FileMode.CreateNew, FileAccess.Write)", body, StringComparison.Ordinal);
        Assert.Contains("OpenReader(creates: !exists)", body, StringComparison.Ordinal);
    }

    [Fact]
    public void TheKeyedOpenCreatesThroughACreateNewHandleWhenTheProbeSaidAbsent()
    {
        string file = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "KeyedConnector.cs");
        string body = BodyOf(file, "protected sealed override string OpenCore(FileOpenMode mode, FilePresence presence)");
        Assert.Contains("LockCreating(newFileOnly: presence is FilePresence.Absent)", body, StringComparison.Ordinal);
        Assert.Contains("LockCreating(newFileOnly: true)", body, StringComparison.Ordinal);
        string creating = BodyOf(file, "private string? LockCreating(bool newFileOnly)");
        Assert.Contains("Lock(create: true, newFileOnly)", creating, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOpenWhoseCreatingHandleMeetsAFileIsMadeAgainOverIt()
    {
        string file = TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "FileConnector.cs");
        string entered = BodyOf(file, "private string OpenOnceEntered(FileOpenMode mode, bool ownedWritesAdmitted, out OpenAgain again)");
        Assert.Contains("catch (IOException e) when (HostFile.IsAlreadyExists(e, HostPath))", entered, StringComparison.Ordinal);
        Assert.Contains("OpenAgain.FileMadeMeanwhile", entered, StringComparison.Ordinal);
        // A directory at the path (the probe answers Absent for one) is Unix's EEXIST too, and is '37' as on Windows (review R2-1): it
        // is classified before it can read as a file made meanwhile.
        Assert.Contains("catch (IOException e) when (HostFile.IsDirectoryInTheWay(e, HostPath)) { s = FileStatusCode.PermissionDenied; }",
            entered, StringComparison.Ordinal);
        string open = BodyOf(file, "public string Open(FileOpenMode mode)");
        Assert.Contains("OpenAgain.FileMadeMeanwhile when filesMadeMeanwhile < MaxFileMadeRetries", open, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnsuccessfulOpenWithdrawsTheJournalItBeganInBothOrganizations()
    {
        string sequential = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "SequentialConnector.cs"));
        string keyed = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Runtime", "IO", "KeyedConnector.cs"));
        Assert.Contains("WithdrawWhatTheOpenBegan();", sequential, StringComparison.Ordinal);
        Assert.Contains("WithdrawWhatTheOpenBegan();", keyed, StringComparison.Ordinal);
        Assert.DoesNotContain("Transactions.Withdraw(begun)", keyed, StringComparison.Ordinal);   // the one rule is FileConnector's
    }
}
