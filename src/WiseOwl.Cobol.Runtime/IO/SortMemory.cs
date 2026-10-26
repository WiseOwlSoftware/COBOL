// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ THE SORT-MERGE STORE'S MEMORY LIMIT AND WORK DIRECTORY (kb/Work PB3339; docs/CONFORMANCE.md §3 D-SWK). The
/// standard says what a sort-merge file does, never where its records live (§13.4.6.1: the SD entry "furnishes
/// information concerning the physical structure pertaining to a sort or merge file"), so the storage is the
/// implementor's, and WiseOwl COBOL follows GnuCOBOL 3.2 (CLAUDE.md rule 1): a SORT or MERGE keeps at most
/// <see cref="LimitBytes"/> of records in memory and writes what exceeds it as sorted runs in
/// <see cref="Directory"/>, which <see cref="SortWork"/> merges. Read ONCE per run unit, when
/// <see cref="RunUnit.SortFiles"/> is built, so a run unit's answer cannot change under an executing statement.
/// </summary>
/// <param name="LimitBytes">The most bytes of records (by <see cref="SortWork"/>'s resident estimate) one statement keeps
/// in memory — never below <see cref="MinimumLimitBytes"/>.</param>
/// <param name="Directory">Where the statement's temporary run files are created.</param>
internal sealed record SortMemory(long LimitBytes, string Directory)
{
    /// <summary>The environment variable that sets <see cref="LimitBytes"/>: a byte count with an optional <c>K</c>,
    /// <c>M</c> or <c>G</c> suffix (powers of 1024, case-insensitive) — GnuCOBOL's <c>COB_SORT_MEMORY</c> vocabulary.
    /// ⛔ <c>WISEOWL_COBOL_</c>, never <c>COBOL_</c>: that prefix is the §12.3.7 external-switch family
    /// (<see cref="SwitchStore.Prefix"/>; the reason <see cref="FileRegistry.KeyCheckVariable"/> gives).</summary>
    public const string MemoryVariable = "WISEOWL_COBOL_SORT_MEMORY";

    /// <summary>The environment variable that sets <see cref="Directory"/>; unset or blank, the host's temporary
    /// directory (<see cref="Path.GetTempPath"/>: <c>TMPDIR</c> on Linux and macOS, <c>TMP</c> / <c>TEMP</c> on
    /// Windows).</summary>
    public const string DirectoryVariable = "WISEOWL_COBOL_SORT_TMPDIR";

    /// <summary>The limit when <see cref="MemoryVariable"/> is unset or not a size: 128 MiB, GnuCOBOL 3.2's
    /// <c>COB_SORT_MEMORY</c> default (<c>libcob/common.c</c>, <c>config/runtime.cfg</c>).</summary>
    public const long DefaultLimitBytes = 128L * 1024 * 1024;

    /// <summary>The smallest limit: a smaller request is raised to it, so a run holds more than a handful of records.</summary>
    public const long MinimumLimitBytes = 64L * 1024;

    /// <summary>The bytes of one run file's read buffer (<see cref="SortRunFile.BufferBytes"/>) — what the return
    /// phase spends per run it merges.</summary>
    private const long ReadBufferBytes = SortRunFile.BufferBytes;

    /// <summary>The most runs one merge reads at once: the limit over four read buffers, between 2 and 16 — so the
    /// return phase's buffers stay within a quarter of the limit and a statement holds a bounded number of
    /// temporary files open.</summary>
    public int FanIn => (int)Math.Clamp(LimitBytes / (4 * ReadBufferBytes), 2, 16);

    /// <summary>The run unit's configuration: <see cref="MemoryVariable"/> and <see cref="DirectoryVariable"/> over
    /// the defaults. Never throws: a value that is not a size keeps the default, as <c>CacheConfig</c> does.</summary>
    public static SortMemory FromEnvironment()
    {
        long limit = ParseSize(Environment.GetEnvironmentVariable(MemoryVariable)) ?? DefaultLimitBytes;
        string? dir = Environment.GetEnvironmentVariable(DirectoryVariable)?.Trim();
        return new SortMemory(Math.Max(limit, MinimumLimitBytes), string.IsNullOrEmpty(dir) ? Path.GetTempPath() : dir);
    }

    /// <summary>A size in bytes: digits with an optional <c>K</c>, <c>M</c> or <c>G</c> suffix (×1024, ×1024², ×1024³;
    /// case-insensitive); null for anything else, an overflow included.</summary>
    internal static long? ParseSize(string? text)
    {
        string s = text?.Trim() ?? "";
        if (s.Length == 0) return null;
        long unit = char.ToUpperInvariant(s[^1]) switch   // not a COBOL word: an environment variable value
        {
            'K' => 1024L,
            'M' => 1024L * 1024,
            'G' => 1024L * 1024 * 1024,
            _ => 1,
        };
        if (unit != 1) s = s[..^1];
        if (!long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long n)) return null;
        return n > long.MaxValue / unit ? null : n * unit;
    }
}
