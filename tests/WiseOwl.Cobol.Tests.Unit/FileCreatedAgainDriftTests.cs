// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE FILE A WRITE CREATES AGAIN AFTER ITS RUN UNIT'S ROLLBACK REMOVED IT IS ONE DECISION IN EVERY ORGANIZATION: A PRESENCE PROBE
/// UNDER THE ENTRY MUTEX, THEN A HANDLE TAKEN <c>FileMode.CreateNew</c> (kb/Work PB3635; DESIGN-runtime-library.md 2.2.2.14). A file
/// another run unit made at the path since the ROLLBACK is that run unit's (§9.1.17), so the WRITE answers '30' (§9.1.13.6 item 1)
/// rather than appending to it, replacing it or journaling it as its own creation. The probe alone leaves a window — another run unit's
/// OPEN OUTPUT takes no entry mutex — which only a creating handle that refuses an existing file closes, so a creation spelled
/// <c>Append</c>, <c>OpenOrCreate</c> or <c>Create</c> is the defect again. The behavioural witnesses (SequentialCommitControlTests,
/// CommitRollbackRuntimeTests) cover the probe's half; this guards the half no deterministic test can reach.
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
}
