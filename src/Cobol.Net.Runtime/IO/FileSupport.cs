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
