// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.InteropServices;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>The host's cumulative processor times at one moment, in the operating system's own ticks: the ticks its
/// processors spent idle and the ticks they spent in all.</summary>
internal readonly record struct HostCpuTimes(ulong Idle, ulong Total);

/// <summary>
/// What the host was doing while an observation ran, so a non-observation tells LOAD from a HANG (kb/Work PB3108): a
/// process killed at its budget on a host whose processors were busy throughout was starved; one killed on an idle host
/// that had used its own processor time throughout was spinning; one that used none was blocked.
/// </summary>
/// <remarks>
/// Linux reads <c>/proc/stat</c> (the aggregate <c>cpu</c> line) and <c>/proc/loadavg</c>; Windows reads
/// <c>GetSystemTimes</c>. Another host reports that it has no reading, which is said in the line, never guessed. Reading
/// the host is an instrument: it never fails an observation, it only describes one.
/// </remarks>
internal static class HostLoad
{
    /// <summary>The host's processor times now, or null where this host offers no reading.</summary>
    public static HostCpuTimes? Sample()
    {
        try
        {
            if (OperatingSystem.IsLinux()) return SampleProcStat();
            if (OperatingSystem.IsWindows()) return SampleWindows();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or OverflowException)
        {
            // An instrument that cannot read the host says so in the line below; it never fails the observation.
        }
        return null;
    }

    /// <summary>The host's load across an attempt that began at <paramref name="start"/> and ended at
    /// <paramref name="end"/>, as one line for a non-observation.</summary>
    public static string Describe(HostCpuTimes? start, HostCpuTimes? end)
    {
        string busy = start is { } s && end is { } e && e.Total > s.Total
            ? string.Create(CultureInfo.InvariantCulture,
                $"{100.0 * (1.0 - (double)(e.Idle - s.Idle) / (e.Total - s.Total)):0} % busy across the attempt")
            : "processor use not readable on this host";
        string queue = LoadAverage() is { } avg ? $"; load average {avg}" : "";
        return $"host: {Environment.ProcessorCount} logical processor(s), {busy}{queue}";
    }

    /// <summary>How many durable flushes <see cref="DurableFlushLine"/> times.</summary>
    internal const int FlushProbes = 5;

    /// <summary>The time a durable flush (<c>FlushFileBuffers</c> / <c>fsync</c>) of a small file takes on the volume of
    /// <paramref name="directory"/>, as one line for a non-observation (kb/Work PB5147). A process that spent its budget
    /// off the processor on a host that was not busy was blocked, and a program whose every statement flushes a file
    /// (the MCS device of a SEND flushes it before the store commits it) is blocked in exactly that call when the volume
    /// flushes slowly; the line says how slowly, so the next reader reads the stall off the line instead of guessing it.
    /// The probe file is created in, and removed from, that directory; an instrument that cannot probe says so.</summary>
    public static string DurableFlushLine(string? directory)
    {
        string dir = string.IsNullOrEmpty(directory) ? Path.GetTempPath() : directory;
        string probe = Path.Combine(dir, ".flush-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            var times = new List<TimeSpan>(FlushProbes);
            using (var handle = File.OpenHandle(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.DeleteOnClose))
            {
                byte[] one = [1];
                for (int i = 0; i < FlushProbes; i++)
                {
                    RandomAccess.Write(handle, one, 0);
                    long start = System.Diagnostics.Stopwatch.GetTimestamp();
                    RandomAccess.FlushToDisk(handle);
                    times.Add(System.Diagnostics.Stopwatch.GetElapsedTime(start));
                }
            }
            return DescribeFlushes(times);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return "durable flush on the run volume: not probed (" + e.GetType().Name + ")";
        }
    }

    /// <summary>The line for the flush times of the probes: their median and their largest, in milliseconds.</summary>
    internal static string DescribeFlushes(IReadOnlyList<TimeSpan> times)
    {
        if (times.Count == 0) return "durable flush on the run volume: not probed";
        var ordered = times.Order().ToArray();
        double median = ordered[ordered.Length / 2].TotalMilliseconds;
        double worst = ordered[^1].TotalMilliseconds;
        return string.Create(CultureInfo.InvariantCulture,
            $"durable flush on the run volume: median {median:0.0} ms, worst {worst:0.0} ms of {ordered.Length} probes");
    }

    /// <summary>Linux's 1-, 5- and 15-minute run-queue averages (<c>/proc/loadavg</c>): how many processes wanted a
    /// processor, which the busy share cannot show once every processor is busy.</summary>
    private static string? LoadAverage()
    {
        if (!OperatingSystem.IsLinux()) return null;
        try
        {
            string[] fields = File.ReadAllText("/proc/loadavg").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return fields.Length >= 3 ? $"{fields[0]} {fields[1]} {fields[2]}" : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }
    }

    /// <summary>The aggregate <c>cpu</c> line of <c>/proc/stat</c>: user nice system idle iowait irq softirq steal
    /// (guest time is already inside user). Idle is idle plus iowait.</summary>
    private static HostCpuTimes? SampleProcStat()
    {
        using var reader = new StreamReader("/proc/stat");
        string? line = reader.ReadLine();
        if (line is null || !line.StartsWith("cpu ", StringComparison.Ordinal)) return null;
        ulong[] ticks = [.. line[4..].Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(8)
            .Select(f => ulong.Parse(f, CultureInfo.InvariantCulture))];
        if (ticks.Length < 5) return null;
        ulong total = 0;
        foreach (ulong t in ticks) total += t;
        return new HostCpuTimes(ticks[3] + ticks[4], total);
    }

    /// <summary><c>GetSystemTimes</c>: kernel time includes idle time, so the total is kernel plus user.</summary>
    private static HostCpuTimes? SampleWindows() =>
        GetSystemTimes(out ulong idle, out ulong kernel, out ulong user) ? new HostCpuTimes(idle, kernel + user) : null;

    // DllImport, not LibraryImport: the generated stub needs AllowUnsafeBlocks in every test project that links
    // tests/_shared, and three blittable out parameters marshal the same either way.
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);
}
