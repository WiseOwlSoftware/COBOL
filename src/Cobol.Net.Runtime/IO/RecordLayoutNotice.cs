// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// The RUNTIME notice channel for a file whose bytes are PROVABLY inconsistent with the record description that
/// is about to read them — the support half of the V59 representation change (COBOLNET_DESIGN §14.4).
/// <para>
/// Why this exists: a fixed-length record-sequential file carries NO self-description. If the record description
/// says 5 bytes and the file holds 8-byte records — the shape a data file written by a build before V59 has, when
/// a BINARY or PACKED field was stored as its zoned digits — every read after the first is misaligned and the
/// program computes on rubbish. The standard's own detection (§14.9.30.4 GR14: a record whose byte count is
/// outside the description's min/max is a SUCCESSFUL read with I-O status '04') fires only on the trailing
/// partial record, only if the total length is not a multiple, and only if the program inspects FILE STATUS.
/// That is too late and too quiet for a layout migration, so the arithmetic fact — a file length that is not a
/// whole multiple of the fixed record length — is reported at OPEN, before a single record is misread.
/// </para>
/// <para>
/// It is a NOTICE, not a status change: OPEN still succeeds and the I-O status is untouched, because the standard
/// defines no OPEN condition for this and inventing one would be non-conforming. It goes to STANDARD ERROR so a
/// program's own output (which goldens compare) is never disturbed.
/// </para>
/// </summary>
public static class RecordLayoutNotice
{
    /// <summary>Emitted once per (file, host path) pair — a program that OPENs the same file in a loop should not
    /// produce a wall of identical notices.</summary>
    private static readonly HashSet<string> Reported = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Report a fixed-length record-sequential file whose byte length is not a whole multiple of its
    /// declared record length. <paramref name="hostPath"/> is the resolved host file — the name the ASSIGN clause
    /// produced, which is what the user can act on. No-op when the lengths agree or the file is empty.</summary>
    public static void CheckFixedLengthFile(string hostPath, long byteLength, int recordLength)
    {
        if (recordLength <= 0 || byteLength <= 0 || byteLength % recordLength == 0) return;
        lock (Reported)
            if (!Reported.Add(hostPath)) return;
        long whole = byteLength / recordLength;
        Console.Error.WriteLine(
            $"COBOL.NET notice: file '{hostPath}' is {byteLength} bytes, which is not a whole multiple "
            + $"of the {recordLength}-byte record described for it ({whole} whole records + {byteLength % recordLength} "
            + "trailing bytes). The record description and the file's layout disagree; reads after the first record "
            + "will be misaligned. If this file was written by an earlier build, note that a BINARY or PACKED field "
            + "now occupies its true bytes (COMP 1-2-4-8-16, COMP-3 packed BCD) rather than one byte per digit.");
    }

    /// <summary>Test seam: forget which paths have been reported (the set is process-wide).</summary>
    public static void ResetForTests()
    {
        lock (Reported) Reported.Clear();
    }
}
