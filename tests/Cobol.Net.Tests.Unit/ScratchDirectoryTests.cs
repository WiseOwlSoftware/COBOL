// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <c>tests/_shared/ScratchDirectory.cs</c> does what its summary says (kb/Work PB3194, review R1-2): a directory goes
/// whole, read-only entries included; one it cannot remove is REPORTED and the call returns; a test host's directory is
/// reaped only once its host has ended (its lock is free, or gone), never while the host runs; and a program loaded
/// through <see cref="ScratchDirectory.LoadAssembly"/> leaves its file deletable.
/// </summary>
public sealed class ScratchDirectoryTests
{
    [Fact]
    public void Remove_TakesReadOnlyEntries_AndAPathAlreadyGoneIsNoError()
    {
        string dir = ScratchDirectory.Create("scratch-ro-");
        string sub = Path.Combine(dir, "objects", "pack");
        Directory.CreateDirectory(sub);
        string file = Path.Combine(sub, "pack-1.idx");
        File.WriteAllText(file, "x");
        new FileInfo(file).IsReadOnly = true;                       // git writes its objects so
        new DirectoryInfo(sub).Attributes |= FileAttributes.ReadOnly;

        ScratchDirectory.Remove(dir);
        Assert.False(Directory.Exists(dir), $"{dir} survived Remove");
        ScratchDirectory.Remove(dir);                                // gone: no error
    }

    /// <summary>A directory that cannot be removed (Windows: a file another handle holds; Linux: a directory this user
    /// may not search) is reported on the harness log and Remove returns: it never throws from a finally or a Dispose.</summary>
    [Fact]
    public void Remove_ReportsWhatItCannotRemove_AndReturns()
    {
        string dir = ScratchDirectory.Create("scratch-held-");
        string inner = Path.Combine(dir, "inner");
        Directory.CreateDirectory(inner);
        string file = Path.Combine(inner, "held.dll");
        File.WriteAllText(file, "x");
        FileStream? held = null;
        try
        {
            if (OperatingSystem.IsWindows())
                held = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            else
                File.SetUnixFileMode(inner, UnixFileMode.None);

            ScratchDirectory.Remove(dir);

            Assert.True(Directory.Exists(dir), "the refusal did not happen, so this case measured nothing");
            string log = File.Exists(HarnessLog.Path) ? File.ReadAllText(HarnessLog.Path) : "";
            Assert.Contains($"SCRATCH-LEFT  {dir} :: ", log, StringComparison.Ordinal);
        }
        finally
        {
            held?.Dispose();
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(inner, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            ScratchDirectory.Remove(dir);
        }
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>Reaping, on a root of the test's own: an ended host (its lock free) and a leftover (no lock file) go with
    /// their lock files; a host that runs (its lock held) keeps its directory and its lock.</summary>
    [Fact]
    public void Reap_TakesEndedHostsAndLeftovers_AndKeepsARunningHost()
    {
        string root = ScratchDirectory.Create("scratch-reap-");
        try
        {
            foreach (string host in new[] { "101-ended", "102-running", "103-leftover" })
                File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(root, host)).FullName, "prog.dll"), "x");
            File.WriteAllText(Path.Combine(root, "101-ended.lock"), "");
            File.WriteAllText(Path.Combine(root, "102-running.lock"), "");
            using (new FileStream(Path.Combine(root, "102-running.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var reaped = ScratchDirectory.Reap(root);
                Assert.Equal(new[] { "101-ended", "103-leftover" }, reaped.Order(StringComparer.Ordinal));
                Assert.Equal(new[] { "102-running", "102-running.lock" },
                    Directory.EnumerateFileSystemEntries(root).Select(p => Path.GetFileName(p)).Order(StringComparer.Ordinal));
            }
        }
        finally { ScratchDirectory.Remove(root); }
    }

    /// <summary>This host's own directory holds its lock: a reap of the real root, as every other host's first
    /// <see cref="ScratchDirectory.Create"/> runs it, leaves it in place.</summary>
    [Fact]
    public void Reap_OfTheRealRoot_NeverTakesThisRunningHost()
    {
        string mine = ScratchDirectory.Create("scratch-live-");
        string host = Path.GetDirectoryName(mine)!;
        Assert.Equal(ScratchDirectory.HostsRoot, Path.GetDirectoryName(host));
        Assert.True(File.Exists(host + ".lock"), $"{host} has no lock file");

        Assert.DoesNotContain(Path.GetFileName(host), ScratchDirectory.Reap(ScratchDirectory.HostsRoot));
        Assert.True(Directory.Exists(mine), "a reap took this running host's directory");
        ScratchDirectory.Remove(mine);
    }

    [Fact]
    public void LoadAssembly_LeavesTheFileDeletable()
    {
        string dir = ScratchDirectory.Create("scratch-load-");
        string name = "ScratchLoad" + Guid.NewGuid().ToString("N");
        string dll = Path.Combine(dir, name + ".dll");
        var compilation = CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText("public static class Probe { public static int Answer() => 42; }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var emitted = compilation.Emit(dll);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));

        var probe = ScratchDirectory.LoadAssembly(dll).GetType("Probe")!;
        Assert.Equal(42, probe.GetMethod("Answer")!.Invoke(null, null));

        File.Delete(dll);                                            // a path load maps the file and Windows refuses this
        Assert.False(File.Exists(dll));
        ScratchDirectory.Remove(dir);
        Assert.False(Directory.Exists(dir));
    }
}
