// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.Loader;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⛔ THE ONE WAY A TEST MAKES AND REMOVES A SCRATCH DIRECTORY (kb/Work PB3194, review R1-2): <see cref="Create"/>,
/// <see cref="Remove"/>, and <see cref="LoadAssembly"/> for a compiled program a test runs
/// in its own process.
///
/// <para><b>What happened.</b> Every test class made its own directory in the machine-wide temp directory and removed it
/// with <c>try { Directory.Delete(dir, recursive: true); } catch { }</c>, about a hundred copies. A delete that failed
/// said nothing, and three things made it fail: a test that loaded the program it compiled into the test host
/// (<c>AssemblyLoadContext.Default.LoadFromAssemblyPath</c>, <c>Assembly.LoadFrom</c>), which maps the file until the
/// process ends, so Windows refuses its delete; a read-only entry; and a test host the gate ends at its first red, which
/// runs no cleanup at all. 16,096 <c>CobolNet_Tests_*</c> directories had piled up in one host's temp directory by
/// 2026-10-23, nearly every one holding the one program a witness test had loaded. It is the C# twin of the Python
/// self-tests' <c>rmtree(ignore_errors=True)</c> that <c>scripts/scratch_repo.py</c>'s <c>remove_tree</c> and
/// <c>scratch_dir</c> replaced, and this class follows the same rules.</para>
///
/// <para><b>The mechanism.</b>
/// <list type="bullet">
///   <item>Every scratch directory lives under ONE directory per test host, <c>&lt;temp&gt;/cobolnet-tests/&lt;pid&gt;-&lt;id&gt;</c>,
///     beside a lock file of the same name, <c>&lt;pid&gt;-&lt;id&gt;.lock</c>, which the host holds open exclusively
///     for as long as it runs. The operating system releases that lock when the process ends, however it ends, so
///     "this host has ended" is the lock being free: no process id or start time is compared, and a reused pid cannot
///     be mistaken for the host.</item>
///   <item><see cref="Remove"/> deletes a directory WHOLE, read-only entries included, and REPORTS what it cannot delete
///     (standard error and the harness observation log, <see cref="HarnessLog"/>): never an empty catch.</item>
///   <item>The host's directory is removed when the host exits, and the first <see cref="Create"/> of every host first
///     REAPS the directory of every host whose lock is free: a killed host's leftovers go with the next run, the rule
///     <c>scripts/linux-gate.sh</c>'s <c>reap_gates</c> applies to the Linux gate's clones.</item>
///   <item><see cref="LoadAssembly"/> loads a compiled program from its bytes, so the test host holds no mapping of the
///     file and its directory can go while the host runs.</item>
/// </list>
/// <c>ScratchDirectoryDriftTests</c> holds every test file to it: no recursive <c>Directory.Delete</c>, no
/// <c>Directory.CreateTempSubdirectory</c> and no assembly load from a path outside this class, and no delete whose
/// failure an empty catch swallows anywhere in the test tree.</para>
/// </summary>
public static class ScratchDirectory
{
    /// <summary>The directory that holds every test host's directory and lock file.</summary>
    public static string HostsRoot { get; } = Path.Combine(Path.GetTempPath(), "cobolnet-tests");

    private const string LockSuffix = ".lock";

    private static readonly Lazy<string> Host = new(() => StartHost(HostsRoot), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>A new, empty directory of this test host's own, its name starting <paramref name="prefix"/>. Remove it
    /// with <see cref="Remove"/>; whatever is left goes when the host ends, or with the next host's reaping.</summary>
    public static string Create(string prefix)
    {
        string path = Path.Combine(Host.Value, prefix + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Delete <paramref name="path"/> and everything under it, read-only entries included, and report (never
    /// swallow) what cannot go: a file another process holds open, a directory this user may not change. A path that does
    /// not exist, or that another host's reaping removed first, is no error. It never throws, so it can run in a
    /// <c>finally</c> or a <c>Dispose</c> without replacing the test's own failure.</summary>
    public static void Remove(string path)
    {
        if (TryRemove(path) is { } failure)
            HarnessLog.ReportLeft(path, failure, "the next test host reaps it once this one has ended");
    }

    /// <summary>Load the assembly at <paramref name="path"/> into the default load context FROM ITS BYTES: the host keeps
    /// no mapping of the file, so the scratch directory holding it can be removed while the host still runs.</summary>
    public static Assembly LoadAssembly(string path)
    {
        using var image = new MemoryStream(File.ReadAllBytes(path));
        return AssemblyLoadContext.Default.LoadFromStream(image);
    }

    /// <summary>Under <paramref name="root"/>, remove the directory of every test host that has ended, and return their
    /// names. A host that has ended has released its lock, or has no lock file; one that runs holds its lock, so its
    /// directory is never taken from under it. The reaper HOLDS the lock while it removes the directory, and the lock
    /// file goes when it lets go (<see cref="FileOptions.DeleteOnClose"/>): no starting host can take the lock in
    /// between and lose its directory.</summary>
    internal static IReadOnlyList<string> Reap(string root)
    {
        var reaped = new List<string>();
        List<string> entries;
        try { entries = Directory.EnumerateFileSystemEntries(root).ToList(); }
        catch (DirectoryNotFoundException) { return reaped; }
        var names = entries.Select(e => Path.GetFileName(e))
            .Select(n => n.EndsWith(LockSuffix, StringComparison.Ordinal) ? n[..^LockSuffix.Length] : n)
            .Distinct(StringComparer.Ordinal);
        foreach (string name in names)
        {
            string dir = Path.Combine(root, name);
            FileStream? held = null;
            try { held = Hold(dir + LockSuffix, FileMode.Open); }
            catch (FileNotFoundException) { /* no lock file: a leftover, since a host makes its lock before its directory */ }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { continue; }  // held: its host runs
            using (held)
                Remove(dir);
            if (!Directory.Exists(dir)) reaped.Add(name);
        }
        return reaped;
    }

    /// <summary>Make this host's directory under <paramref name="root"/> (after reaping every ended host's) and hold its
    /// lock until the process ends; at a normal exit the directory goes, then the lock.</summary>
    internal static string StartHost(string root)
    {
        foreach (string name in Reap(root))
            HarnessLog.Record($"SCRATCH-REAPED  {Path.Combine(root, name)} :: its test host has ended");
        Directory.CreateDirectory(root);
        for (int attempt = 1; ; attempt++)
        {
            string host = Path.Combine(root, $"{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..12]}");
            // The lock first, then the directory: a host's directory never exists without its lock file while the host
            // runs, so a directory with no lock file is a leftover. On Linux .NET creates the file and THEN locks it
            // (flock), and a reaper that took it in that instant deletes it when it lets go: then the lock fails, or the
            // file is gone once the directory exists, and this host starts again under a new name.
            FileStream held;
            try { held = Hold(host + LockSuffix, FileMode.CreateNew); }
            catch (IOException) when (attempt < 3) { continue; }
            Directory.CreateDirectory(host);
            if (!File.Exists(host + LockSuffix))
            {
                held.Dispose();
                Remove(host);
                if (attempt < 3) continue;
                throw new IOException($"the test host's scratch lock {host}{LockSuffix} was taken three times while starting");
            }
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                Remove(host);
                held.Dispose();
            };
            return host;
        }
    }

    /// <summary>Open <paramref name="lockPath"/> exclusively, deleted when closed. The operating system releases it when
    /// the holding process ends, however it ends (on Linux .NET takes it as an flock).</summary>
    private static FileStream Hold(string lockPath, FileMode mode) =>
        new(lockPath, mode, FileAccess.ReadWrite, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose);

    /// <summary>The deletion and its one retry: the exception that kept the directory <paramref name="path"/> from going,
    /// or null.</summary>
    internal static Exception? TryRemove(string path)
    {
        if (!Directory.Exists(path)) return null;
        try
        {
            Directory.Delete(path, recursive: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!Directory.Exists(path)) return null;
        }
        // A read-only entry refuses its delete on Windows, and a directory without write permission refuses the delete of
        // what it holds on Linux: make every entry writable, the directory itself included, and delete once more.
        try
        {
            var all = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = 0 };
            var top = new DirectoryInfo(path);
            foreach (FileSystemInfo entry in top.EnumerateFileSystemInfos("*", all).Prepend(top))
                if (entry.Attributes.HasFlag(FileAttributes.ReadOnly))
                    entry.Attributes &= ~FileAttributes.ReadOnly;
            Directory.Delete(path, recursive: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Directory.Exists(path) ? e : null;
        }
    }
}
