// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime.IO;

/// <summary>
/// Standard COBOL file status codes per ISO/IEC 1989:2023.
/// </summary>
public static class FileStatus
{
    /// <summary>Status 00 — operation completed successfully.</summary>
    public const string Success = "00";
    /// <summary>Status 10 — sequential READ reached end-of-file (AT END condition).</summary>
    public const string AtEnd = "10";
    /// <summary>Status 21 — key value not in ascending sequence for sequential WRITE to indexed file.</summary>
    public const string KeyOutOfSequence = "21";
    /// <summary>Status 22 — WRITE attempted with a duplicate key on a file that disallows duplicates.</summary>
    public const string DuplicateKey = "22";
    /// <summary>Status 23 — READ/START found no record matching the specified key.</summary>
    public const string RecordNotFound = "23";
    /// <summary>Status 24 — record boundary violation (e.g., relative key exceeds file boundary).</summary>
    public const string BoundaryViolation = "24";
    /// <summary>Status 30 — permanent I/O error with no more specific code.</summary>
    public const string PermanentError = "30";
    /// <summary>Status 35 — OPEN failed because the file does not exist (INPUT/I-O mode).</summary>
    public const string FileNotFound = "35";
    /// <summary>Status 37 — OPEN failed due to insufficient access permissions.</summary>
    public const string PermissionDenied = "37";
    /// <summary>Status 41 — OPEN attempted on a file that is already open.</summary>
    public const string FileAlreadyOpen = "41";
    /// <summary>Status 42 — CLOSE attempted on a file that is not open.</summary>
    public const string FileNotOpen = "42";
    /// <summary>Status 43 — READ attempted on a file not opened for input or I-O.</summary>
    public const string NoReadPermission = "43";
    /// <summary>Status 44 — WRITE attempted on a file not opened for output or I-O.</summary>
    public const string NoWritePermission = "44";
    /// <summary>Status 47 — READ/WRITE record length does not match file's record description.</summary>
    public const string RecordLengthError = "47";
}
