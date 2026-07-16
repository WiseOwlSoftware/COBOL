// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The keyed (RELATIVE/INDEXED) I/O bound nodes (P7 Step 10h: the binder half moved to
// Binding/Procedure/Verbs/KeyedIoBinder.cs; these types STAY here — the VersionConformancePass edition
// gates fire on their SHAPE, and the source-generated visitor keys on this namespace).

/// <summary>The <c>INVALID KEY</c> / <c>NOT INVALID KEY</c> phrase pair of a keyed I/O statement (ISO §9.1.14):
/// the INVALID imperative runs exactly when the invalid key condition exists (an I-O status in the <c>2x</c>
/// family, §9.1.13.5); the NOT INVALID imperative runs ONLY on successful completion (§9.1.14 final rule item 2 —
/// a non-invalid-key UNSUCCESSFUL completion takes NEITHER branch, it goes to exception processing).</summary>
public sealed record KeyedInvalidKey(
    IReadOnlyList<BoundStatement>? Invalid, IReadOnlyList<BoundStatement>? NotInvalid);

/// <summary>How a keyed READ retrieves its record (ISO §14.9.30 GR19): a sequential Format-1 NEXT/PREVIOUS walk,
/// or the Format-2 random retrieval by key value.</summary>
public enum KeyedReadKind { Next, Previous, Random }

/// <summary>The positioning basis of a START (ISO §14.9.41 general format): FIRST / LAST (2002+), or the KEY
/// relational phrase (the COBOL-85 form; KEY omitted ⇒ EQUAL on the relative key / prime key, GR8/GR15).</summary>
public enum KeyedStartMode { First, Last, Key }

/// <summary><c>READ file [NEXT|PREVIOUS] [INTO x] [KEY IS k] [AT END …|INVALID KEY …]</c> on a RELATIVE or INDEXED
/// file (ISO §14.9.30). <paramref name="KeyIndex"/> is the key of reference for a Format-2 read: −1 = the prime
/// record key (GR31) / the relative key (GR29), ≥0 = the i-th ALTERNATE RECORD KEY (GR30). The emitter renders the
/// SSOT status-first shape (COBOLNET_DESIGN §8.3): store the status, branch AT END on <c>'1'</c> (10/14,
/// §9.1.13.4), INVALID KEY on <c>'2'</c> (§9.1.13.5), success phrases on <c>'0'</c>.</summary>
public sealed record BoundKeyedRead(
    FileModel File, KeyedReadKind Kind, int KeyIndex, Place? Into,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd,
    KeyedInvalidKey? InvalidKey) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.30 — WITH LOCK / WITH NO LOCK / IGNORING LOCK), or
    /// None; combined at runtime with the file's declared LOCK MODE (AUTOMATIC auto-locks on any READ).</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9): n TIMES loops the registry lock-check; SECONDS/FOREVER deadlock-bail
    /// to status 52 in one run unit (GR4a — no external releaser).</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>ADVANCING ON LOCK (§14.9.30 GR22, sequential NEXT/PREVIOUS only): skip-scan locked records.</summary>
    public bool AdvancingOnLock { get; init; }
}

/// <summary><c>WRITE record [FROM x] [INVALID KEY …]</c> on a RELATIVE or INDEXED file (ISO §14.9.51 GR29–GR33
/// relative / GR34–GR42 indexed). For a sequential-access relative file the released RRN is MOVEd back into the
/// RELATIVE KEY item on success (GR29a/GR30); for random/dynamic access the key item is read first (GR29b).</summary>
public sealed record BoundKeyedWrite(
    FileModel File, Place Record, BoundOperand? From, KeyedInvalidKey? InvalidKey) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.51 — WITH LOCK / WITH NO LOCK), or None; WITH LOCK
    /// locks the record written on a sharing-active file (GR11), single locking releases the prior lock (GR10).</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.51 GR16), or null.</summary>
    public RetrySpec? Retry { get; init; }
}

/// <summary><c>REWRITE record [FROM x] [INVALID KEY …]</c> on a RELATIVE or INDEXED file (ISO §14.9.35 GR18–GR25):
/// relative random/dynamic replaces the slot named by the relative key (absent → '23', GR21); indexed sequential
/// requires the prime key to equal the last-read key ('21', GR22), random/dynamic an existing prime key ('23',
/// GR23).</summary>
public sealed record BoundKeyedRewrite(
    FileModel File, Place Record, BoundOperand? From, KeyedInvalidKey? InvalidKey) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.35 — WITH LOCK / WITH NO LOCK), or None; a record
    /// locked by another connector blocks the rewrite (GR11 → status 51), the GR12 discipline follows.</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.35 GR11), or null.</summary>
    public RetrySpec? Retry { get; init; }
}

/// <summary><c>DELETE file RECORD [INVALID KEY …]</c> (ISO §14.9.10 Format 1): sequential access removes the
/// record of the prior successful READ (GR2, else '43'); indexed random/dynamic deletes by the prime record key
/// (GR3), relative by the relative key item (GR4) — absent record → invalid key '23'. The FPI is unaffected (GR9).</summary>
public sealed record BoundKeyedDelete(FileModel File, KeyedInvalidKey? InvalidKey) : BoundStatement
{
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.10 GR6 — a record locked by another connector blocks the
    /// deletion, status 51), or null. (DELETE RECORD carries no record-lock phrase — §14.9.10 general format.)</summary>
    public RetrySpec? Retry { get; init; }
}

/// <summary><c>DELETE FILE file [ON EXCEPTION …]</c> (ISO §14.9.10 Format 2 — COBOL-2023, grammar-gated
/// <c>{is2023()}?</c>): removes the physical file. An open connector → '41' (GR13); an ABSENT file is a
/// SUCCESSFUL completion with status '05' (GR14 — the legacy returned '35'; the spec wins); insufficient
/// authority → '37' (GR16).</summary>
public sealed record BoundKeyedDeleteFile(
    FileModel File, IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement
{
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.10 GR15 — the physical file open by another connector is the
    /// '62' file-sharing conflict, re-attempted under RETRY), or null.</summary>
    public RetrySpec? Retry { get; init; }
}

/// <summary><c>START file [FIRST|LAST|KEY rel-op k [WITH LENGTH n]] [INVALID KEY …]</c> (ISO §14.9.41).
/// <paramref name="Op"/> is the mapped C# relational operator (EQUAL when the KEY phrase or its operator is
/// omitted, GR8/GR15; NOT EQUAL is rejected, SR3). <paramref name="KeyIndex"/> = −1 prime/relative, ≥0 alternate.
/// <paramref name="Operand"/> is the comparison data item — the RELATIVE KEY item (SR5/GR10) or the (possibly
/// generic, leftmost-coincident shorter) indexed key item (SR6); <paramref name="Length"/> is the 2002+ WITH
/// LENGTH partial-key character count (SR8/GR13–GR14, indexed only).</summary>
public sealed record BoundKeyedStart(
    FileModel File, KeyedStartMode Mode, string Op, int KeyIndex, Place? Operand, BoundExpr? Length,
    KeyedInvalidKey? InvalidKey) : BoundStatement;
