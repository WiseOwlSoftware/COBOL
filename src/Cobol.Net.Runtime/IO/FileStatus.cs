// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The ISO/IEC 1989:2023 §9.1.13 I-O status codes the sequential connector reports (a focused subset of the
/// full table — the codes a sequential file can produce). The two-character code is stored into the FILE STATUS
/// data item after each I/O verb and steers the AT END / declarative branches.</summary>
public static class FileStatusCode
{
    /// <summary>00 — successful completion.</summary>
    public const string Success = "00";
    /// <summary>04 — a READ was successful but the physical record is shorter or longer than the min/max record
    /// length for the file's fixed attributes (ISO §9.1.13.2 item 3 / §14.9.30 GR14 — record-sequential only; the
    /// record is still delivered). Clarified in COBOL-2023 (Annex E.2 item 15), version-invariant behavior.</summary>
    public const string RecordLengthShortLong = "04";
    /// <summary>06 — a LINE-SEQUENTIAL READ found a record longer than the maximum record size; it is truncated on the
    /// right, the READ is SUCCESSFUL, and the file position indicator references the next unread character in the
    /// record so the following READ continues the remainder (ISO §14.9.30.4 GR15 + NOTE 3; §9.1.13.2 item 5).</summary>
    public const string LineRecordTooLong = "06";
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
    /// <summary>Classify a DELETE FILE IOException (ISO §14.9.10.4 GR17 vs §9.1.13.6 item 1): a storage medium
    /// that does not allow deletion — a write-protected volume (Windows ERROR_WRITE_PROTECT, HResult
    /// 0x80070013) or a read-only mount (Unix EROFS = errno 30, which .NET surfaces as the IOException's
    /// HResult) — is the '37' GR17 states; anything else is the generic permanent error '30'. Annex E.3.3
    /// item 35 lists '37' (never '30' from a medium refusal) among the statuses DELETE FILE sets. Public so
    /// the mapping is directly unit-testable (kb/Work PB140).</summary>
    public static string ForDeleteFileFailure(IOException ex) =>
        ex.HResult is unchecked((int)0x80070013) or 30 ? PermissionDenied : PermanentError;

    /// <summary>38 — OPEN of a file previously CLOSEd WITH LOCK (the ≤2014 CLOSE … WITH LOCK leg; NOT part of the
    /// 2002 5x/6x file-sharing family — that construct is COBOLNET0902-rejected at 2023 via the
    /// close-with-lock-removed-2023 gate, so 38 stays only for the still-legal ≤2014 path).</summary>
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
    /// <summary>44 — record-size boundary violation (§9.1.13.6 item 2): a WRITE/REWRITE outside the RECORD IS
    /// VARYING bounds (§13.18.43 GR14 / §14.9.35 GR20), or a record-sequential REWRITE whose size differs from
    /// the record being replaced (§14.9.35 GR16).</summary>
    public const string RecordSizeViolation = "44";

    /// <summary>71 — a line-sequential REWRITE whose new record contains a character outside the implementor-defined
    /// line character set (ISO §14.9.35.4 GR17d); here a record carrying a line delimiter (CR/LF) that would corrupt
    /// the line structure.</summary>
    public const string LineRecordInvalidChar = "71";

    /// <summary>02 — successful completion; a duplicate alternate record key was detected (ISO §9.1.13.2 item 2).</summary>
    public const string DuplicateAlternateKey = "02";
    /// <summary>14 — relative sequential READ: the RRN's significant digits exceed the RELATIVE KEY item (§9.1.13.4 item 2).</summary>
    public const string RelativeKeyOverflow = "14";
    /// <summary>21 — key sequence error on a sequentially-accessed indexed file (§9.1.13.5 item 1).</summary>
    public const string SequenceError = "21";
    /// <summary>22 — duplicate key: relative slot, prime key, or a no-DUPLICATES alternate (§9.1.13.5 item 2).</summary>
    public const string DuplicateKey = "22";
    /// <summary>23 — record not found on keyed access / keyed access to an absent optional file (§9.1.13.5 item 3).</summary>
    public const string RecordNotFound = "23";
    /// <summary>24 — invalid-key boundary violation on a relative/indexed WRITE (§9.1.13.5 item 4).</summary>
    public const string BoundaryViolation = "24";
    /// <summary>34 — permanent-error boundary violation (a relative random WRITE with a key &lt; 1, §14.9.51 GR29b / §9.1.13.6 item 4).</summary>
    public const string PermanentBoundary = "34";

    // ── The COBOL-2002 file-sharing / record-locking status family (ISO §9.1.13.8/9; Phase 4d M2-FILE-1). The
    //    '5' first digit maps to EC-I-O-RECORD-OPERATION, '6' to EC-I-O-FILE-SHARING (§9.1.13.1) — both
    //    continuable; the EC bridge (ExceptionCatalog) already routes them, so producing the code is the whole job.
    /// <summary>51 — a record READ/REWRITE/DELETE could not lock the record because another file connector holds
    /// its lock (§9.1.13.8 item 1).</summary>
    public const string RecordLocked = "51";
    /// <summary>52 — an implementor-detected deadlock: a record/file lock cannot be granted and a bounded RETRY
    /// (or SECONDS/FOREVER, which cannot block productively in one run unit) is exhausted (§9.1.13.8 item 2).</summary>
    public const string Deadlock = "52";
    /// <summary>53 — the maximum number of record locks for the run unit has been exceeded (§9.1.13.8 item 3).</summary>
    public const string RunUnitLockLimit = "53";
    /// <summary>54 — the maximum number of record locks for this file connector has been exceeded (§9.1.13.8 item 4).</summary>
    public const string ConnectorLockLimit = "54";
    /// <summary>61 — OPEN failed: a sharing conflict, based on the sharing mode of a previously-opened file
    /// connector or this OPEN's SHARING phrase, prevents the open (§9.1.13.9 item 1, sub-cases a–e).</summary>
    public const string FileSharingConflict = "61";
    /// <summary>62 — DELETE FILE failed: the file is currently open by another file connector (§9.1.13.9 item 2).</summary>
    public const string DeleteFileSharing = "62";
}
