// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The mode a file connector is opened in (ISO/IEC 1989:2023 §14.9.25 OPEN).</summary>
public enum FileOpenMode
{
    /// <summary>OPEN INPUT — read-only sequential access from the start.</summary>
    Input,
    /// <summary>OPEN OUTPUT — write a new file from the start (existing content discarded).</summary>
    Output,
    /// <summary>OPEN EXTEND — append after the existing records.</summary>
    Extend,
    /// <summary>OPEN I-O — read and rewrite an existing file.</summary>
    IO,
}

/// <summary>The ISO/IEC 1989:2023 §9.1.13 I-O status codes the sequential connector reports (a focused subset of the
/// full table — the codes a sequential file can produce). The two-character code is stored into the FILE STATUS
/// data item after each I/O verb and steers the AT END / declarative branches.</summary>
public static class FileStatusCode
{
    /// <summary>00 — successful completion.</summary>
    public const string Success = "00";
    /// <summary>05 — OPEN of an OPTIONAL file that is not present (created on OUTPUT/EXTEND/I-O, EOF on INPUT).</summary>
    public const string OptionalFileNotFound = "05";
    /// <summary>10 — end-of-file reached on a sequential READ (the AT END condition).</summary>
    public const string AtEnd = "10";
    /// <summary>30 — permanent I/O error with no more specific code.</summary>
    public const string PermanentError = "30";
    /// <summary>35 — OPEN INPUT/I-O/EXTEND on a non-optional file that is not present.</summary>
    public const string FileNotFound = "35";
    /// <summary>37 — OPEN failed: insufficient access permission.</summary>
    public const string PermissionDenied = "37";
    /// <summary>38 — OPEN of a file previously CLOSEd WITH LOCK.</summary>
    public const string FileLocked = "38";
    /// <summary>41 — OPEN attempted on an already-open file.</summary>
    public const string FileAlreadyOpen = "41";
    /// <summary>42 — CLOSE attempted on a file that is not open.</summary>
    public const string FileNotOpen = "42";
    /// <summary>43 — no successful READ immediately preceded a sequential REWRITE.</summary>
    public const string NoSuccessfulReadBeforeDeleteRewrite = "43";
    /// <summary>46 — no valid next record for a sequential READ (a prior READ was unsuccessful, no reposition).</summary>
    public const string NoValidNextRecord = "46";
    /// <summary>47 — READ/START on a file not open for INPUT or I-O.</summary>
    public const string ReadNotOpenForInput = "47";
    /// <summary>48 — WRITE on a file not open for OUTPUT, EXTEND, or I-O.</summary>
    public const string WriteNotOpenForOutput = "48";
    /// <summary>49 — DELETE/REWRITE on a file not open for I-O.</summary>
    public const string DeleteRewriteNotOpenForIO = "49";
}
