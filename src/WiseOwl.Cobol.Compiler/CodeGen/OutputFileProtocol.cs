// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// ⛔ THE ONE WAY THE COMPILER REPLACES A FILE IT PRODUCES (DESIGN-external-repository §7.5; kb/Work PB2103). An output
/// directory is a repository directory (§7.4, §2023:9.3): another compilation may be reading an entry there while this one
/// writes it, and the run-time probe of a running program may be reading it too. So a produced file is never written in
/// place: it is written to <c>{target}.{Guid:N}.tmp</c> beside the target and then renamed over it
/// (<see cref="File.Move(string, string, bool)"/> with overwrite), which a reader never sees half-written — every reader
/// holds a prefetched copy and no handle (<c>RepositoryIndex.Open</c>, §2023:15.1). The rename is retried a bounded number of
/// times (<see cref="Attempts"/> × <see cref="RetryDelay"/>) while another process holds the target open (a
/// <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>, which Windows raises for a file being read), then
/// fails LOUDLY with an <see cref="IOException"/> naming the file. A write that fails leaves no temporary behind.
/// </summary>
internal static class OutputFileProtocol
{
    /// <summary>How many times the rename is attempted before the failure is reported.</summary>
    internal const int Attempts = 10;

    /// <summary>The pause between two attempts.</summary>
    internal static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>Produce <paramref name="target"/> through <paramref name="write"/>, which writes the content to the path it
    /// is given and returns whether it succeeded. On success the content replaces <paramref name="target"/>; on failure the
    /// temporary is deleted and <paramref name="target"/> is untouched. Returns what <paramref name="write"/> returned.</summary>
    public static bool Replace(string target, Func<string, bool> write)
    {
        string temporary = $"{target}.{Guid.NewGuid():N}.tmp";
        bool written = false;
        try
        {
            written = write(temporary);
            if (written) Rename(temporary, target);
            return written;
        }
        finally
        {
            if (!written || File.Exists(temporary)) TryDelete(temporary);   // not a compilation input: this write's own temporary file
        }
    }

    private static void Rename(string temporary, string target)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temporary, target, overwrite: true);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (attempt == Attempts)
                    throw new IOException(
                        $"cannot replace '{target}': it stayed in use by another process through {Attempts} attempts "
                        + $"{RetryDelay.TotalMilliseconds} ms apart ({e.Message}); the new build is not written", e);
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }   // not a compilation input: this compile's own temporary
    }
}
