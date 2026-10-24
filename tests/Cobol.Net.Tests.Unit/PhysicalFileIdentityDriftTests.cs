// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PHYSICAL FILE'S IDENTITY TO THE RUN UNIT IS ITS ABSOLUTE HOST PATH UNDER THE HOST'S CASE RULE (kb/Work PB2748).
/// Table 19's arbitration (§9.1.15: <i>"the sharing mode and the open mode of that OPEN statement shall be allowed by
/// all other file connectors that are currently associated with the physical file"</i>) and the keyed record stores
/// are keyed on the physical file. They were keyed on the verbatim ASSIGN text compared case-insensitively, so two
/// spellings of one file (<c>data.dat</c>, <c>./data.dat</c>, the full path) were two physical files, and on a
/// case-sensitive host two files (<c>Cust.dat</c>, <c>cust.dat</c>) were one: the second OPEN OUTPUT answered '61'
/// against a file it never shared, and a keyed one killed the run unit in the shared store table.
/// <see cref="CobolFile.ResolveHostPath"/> now makes the path absolute and <see cref="HostFile.PhysicalFileComparer"/>
/// carries the case rule; the structural half keeps every table keyed on the physical file on that comparer.
/// <para>A path names a physical file only at the instant it is asked (kb/Work PB3533): a file removed or renamed away and
/// another made at its path outside the run unit is another physical file under the same path, which a host that unlinks a file
/// under an open handle (Linux) permits. The host's own identity (<see cref="HostFile.IdentityOf"/>, <see cref="HostFile.IdentityAt"/>)
/// tells them apart where a handle holds the first one: the run unit's store table and the state a CLOSE under commit control
/// kept compare it beside the path.</para>
/// </summary>
public sealed class PhysicalFileIdentityDriftTests
{
    /// <summary>The host identity answers the question the path cannot (kb/Work PB3533): a handle and the path it was opened through
    /// name one file, two files are two, a path that names nothing has no identity — and, where the host lets a file be taken
    /// away under an open handle, the file made at the path since is another file than the one the handle still holds.</summary>
    [Fact]
    public void TheHostIdentity_TellsTheFileAHandleHoldsFromTheOneMadeAtItsPathSince()
    {
        string dir = ScratchDirectory.Create("pb3533-identity-");
        try
        {
            string first = Path.Combine(dir, "first.dat"), second = Path.Combine(dir, "second.dat");
            File.WriteAllText(first, "1");
            File.WriteAllText(second, "2");
            using var held = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var identity = HostFile.IdentityOf(held.SafeFileHandle);
            Assert.NotNull(identity);
            Assert.Equal(identity, HostFile.IdentityAt(first));
            Assert.NotEqual(identity, HostFile.IdentityAt(second));
            Assert.Null(HostFile.IdentityAt(Path.Combine(dir, "absent.dat")));
            File.Move(first, Path.Combine(dir, "moved.dat"));
            File.WriteAllText(first, "made since");
            Assert.NotEqual(identity, HostFile.IdentityAt(first));
            Assert.Equal(identity, HostFile.IdentityOf(held.SafeFileHandle));
            Assert.Equal(identity, HostFile.IdentityAt(Path.Combine(dir, "moved.dat")));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void TwoSpellingsOfOneFile_ResolveToOneHostPath()
    {
        string dir = Path.GetTempPath();
        string plain = CobolFile.ResolveHostPath(Path.Combine(dir, "pb2748-id.dat"));
        Assert.True(Path.IsPathFullyQualified(plain), plain);
        Assert.Equal(plain, CobolFile.ResolveHostPath(Path.Combine(dir, "pb2748-sub", "..", "pb2748-id.dat")));
        Assert.Equal(plain, CobolFile.ResolveHostPath(Path.Combine(dir, ".", "pb2748-id.dat")));
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "pb2748-id.dat"), CobolFile.ResolveHostPath("pb2748-id.dat"));
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "custmast.txt"), CobolFile.ResolveHostPath("CUSTMAST"));
        Assert.Equal("", CobolFile.ResolveHostPath(""));
    }

    [Fact]
    public void TheCaseRule_IsTheHostFileSystems()
    {
        bool sameFile = HostFile.PhysicalFileComparer.Equals("/d/Cust.dat", "/d/cust.dat");
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), sameFile);
    }

    /// <summary>On a case-sensitive host, two files whose names differ only in case are two physical files: each
    /// OPEN OUTPUT succeeds, and each keyed file keeps its own records.</summary>
    [Fact]
    public void OnACaseSensitiveHost_TwoFilesDifferingInCase_AreTwoPhysicalFiles()
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) return;
        string dir = ScratchDirectory.Create("pb2748-case-");
        try
        {
            var reg = new FileRegistry();
            foreach (var (name, file) in new[] { ("U", "Cust.dat"), ("L", "cust.dat") })
                reg.RegisterIndexed(name, Path.Combine(dir, file), recordWidth: 8, optional: false,
                    accessMode: (int)KeyedAccess.Dynamic, primeKey: [new(0, 4)], varyMin: -1, varyMax: -1);
            reg.OpenStatic("U", FileOpenMode.Output);
            reg.OpenStatic("L", FileOpenMode.Output);
            Assert.Equal("00", reg.Status("U"));
            Assert.Equal("00", reg.Status("L"));
            Assert.Equal("00", reg.WriteShared("U", "0001UUUU", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            Assert.Equal("00", reg.WriteShared("L", "0001LLLL", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("U");
            reg.Close("L");
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ THE STRUCTURAL HALF: every table keyed on the physical file uses the one comparer, and no file under
    /// <c>Cobol.Net.Runtime/IO</c> compares a host path with a comparer of its own.</summary>
    [Fact]
    public void EveryPhysicalFileTable_UsesTheOneComparer()
    {
        foreach (var file in new[] { TestRepo.Src("Cobol.Net.Runtime", "IO", "StoreTable.cs"),
                     TestRepo.Src("Cobol.Net.Runtime", "IO", "Sharing", "PhysicalFileTable.cs") })
        {
            string byHost = File.ReadAllLines(file).Single(l => l.Contains("_byHost = new(", StringComparison.Ordinal));
            Assert.Contains("HostFile.PhysicalFileComparer", byHost, StringComparison.Ordinal);
        }
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        var offenders = Directory.EnumerateFiles(io, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
            .Where(t => t.l.Contains("HostPath", StringComparison.Ordinal) && !t.l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                && (t.l.Contains("StringComparison.", StringComparison.Ordinal) || t.l.Contains("StringComparer.", StringComparison.Ordinal)))
            .Select(t => $"{Path.GetRelativePath(io, t.f)}:{t.i + 1}: {t.l.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "A host path compared outside HostFile.PhysicalFileComparer:\n  " + string.Join("\n  ", offenders));
    }
}
