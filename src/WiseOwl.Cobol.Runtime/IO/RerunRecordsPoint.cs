// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// ⛔ ONE X3.23-1985 RERUN CLAUSE OF THE INTEGER-1 RECORDS FORM, as a file connector holds it (§1985:VII.2.12.4 GR2 d:
/// "the rerun information is written on the device specified by implementor-name-1, which must be specified in the ON
/// phrase, whenever integer-1 records of file-name-2 have been processed. File-name-2 may be either an input or output file
/// with any organization or access"; kb/Work PB5198). It is the one form of the clause this processor provides
/// (GR2: "The implementor must provide at least one of the specified forms"; docs/CONFORMANCE.md §7).
/// <para>The implementor defines the device and the information (GR1 b: "written as a separate file on a device specified
/// by the implementor"). The device of implementor-name-1 is the host file the name spells exactly as an ASSIGN target
/// spells one (<see cref="CobolFile.ResolveHostPath"/>), and each rerun point appends one text line: the word RERUN, the
/// file-name-2, and the number of records processed since the file's OPEN, ended by a line feed on every host.</para>
/// </summary>
/// <param name="Every">integer-1, at least 1 (the compiler registers no clause whose integer-1 is zero: it never fires).</param>
/// <param name="Device">implementor-name-1 as written.</param>
/// <param name="File">file-name-2 as written.</param>
internal readonly record struct RerunRecordsPoint(long Every, string Device, string File)
{
    /// <summary>The line appended to the device at a rerun point, after <paramref name="processed"/> records.</summary>
    internal string Information(long processed) => $"RERUN {File} {processed}\n";
}
