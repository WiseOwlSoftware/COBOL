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

/// <summary>The file-sharing mode of a connector (ISO/IEC 1989:2023 §9.1.15; the SHARING clause §12.4.5.15 / the
/// OPEN SHARING phrase §14.9.27). Governs whether OTHER connectors may open the same physical file (Table 19 →
/// status 61). The implementor default for a connector without any SHARING/LOCK-MODE clause is <b>outside</b> the
/// sharing subsystem entirely (legacy exclusive behavior — no physical-registry participation, so the existing
/// corpus is byte-invariant); <see cref="AllOther"/> is the neutral in-subsystem default.</summary>
public enum FileSharing
{
    /// <summary>SHARING WITH NO OTHER — exclusive; no other connector may open the file (record locks ignored, GR3).</summary>
    NoOther,
    /// <summary>SHARING WITH READ ONLY — other connectors may open only for INPUT.</summary>
    ReadOnly,
    /// <summary>SHARING WITH ALL OTHER — other connectors may open in any mode (record locking is then observable).</summary>
    AllOther,
}

/// <summary>The record-locking mode of a connector (ISO §12.4.5.9 LOCK MODE).</summary>
public enum FileLockMode
{
    /// <summary>No LOCK MODE clause — no record locking (the implementor default, GR1).</summary>
    None,
    /// <summary>LOCK MODE IS MANUAL — a lock is acquired only for a READ … WITH LOCK (GR5).</summary>
    Manual,
    /// <summary>LOCK MODE IS AUTOMATIC — a lock is acquired on every successful READ (GR4).</summary>
    Automatic,
}

/// <summary>The explicit record-lock phrase on a READ/WRITE/REWRITE (ISO §14.9.30 etc.).</summary>
public enum FileRecordLock
{
    /// <summary>No phrase — the connector's LOCK MODE governs (AUTOMATIC auto-locks; MANUAL/None do not).</summary>
    None,
    /// <summary>WITH LOCK — request a lock on the accessed record (MANUAL locking).</summary>
    WithLock,
    /// <summary>WITH NO LOCK — do not lock the accessed record.</summary>
    WithNoLock,
    /// <summary>IGNORING LOCK — access the record even if another connector holds its lock (§9.1.16).</summary>
    Ignoring,
}

/// <summary>The RETRY phrase kind (ISO §14.7.9).</summary>
public enum FileRetryKind
{
    /// <summary>No RETRY phrase — a single lock attempt (GR4a).</summary>
    None,
    /// <summary>RETRY n TIMES — the lock check is attempted n+1 times (GR1).</summary>
    Times,
    /// <summary>RETRY FOR n SECONDS — wall-clock retry; in one run unit no external releaser exists, so an
    /// unsatisfiable conflict deadlock-bails to status 52 rather than blocking (GR2 + §9.1.13.8 impl-license).</summary>
    Seconds,
    /// <summary>RETRY FOREVER — likewise deadlock-bails to 52 in one run unit (GR3).</summary>
    Forever,
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
