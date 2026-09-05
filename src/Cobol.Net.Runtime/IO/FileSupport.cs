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
/// OPEN SHARING phrase §14.9.27). Governs whether OTHER connectors may open the same physical file (§14.9.27.4
/// <see cref="Table19"/> → status '61'). These three members are the whole of what §9.1.15 specifies — <i>"The
/// sharing with no other mode …"</i>, <i>"The sharing with read only mode …"</i>, <i>"The sharing with all other
/// mode …"</i> — so there is deliberately no fourth "default" member: a connector that writes NEITHER a SHARING
/// clause nor an OPEN SHARING phrase carries the UNDETERMINED implementor default, represented as a null
/// <c>FileSharing?</c> (see <see cref="FileRegistry.ImplementorDefaultSharing"/>, kb/Work PB322). Every connector
/// takes part in the Table-19 arbitration whatever it declared (kb/Work PB321).</summary>
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

/// <summary>The explicit lock-RETENTION phrase on a READ/WRITE/REWRITE — the printed
/// <c>[ WITH LOCK | WITH NO LOCK ]</c> bracket of ISO §14.9.30.2 / §14.9.51.2 / §14.9.35.2.
/// <para>⛔ IGNORING LOCK IS DELIBERATELY ABSENT (kb/Work PB331). It is an alternative of READ's OTHER printed
/// bracket, which §5.2.6.1 makes an independent selection, so <c>READ … IGNORING LOCK WITH NO LOCK</c> is one
/// legal statement that has to say BOTH things at once — a single enum member cannot. It travels beside this
/// one as the <c>ignoringLock</c> argument of <see cref="CobolFile.ReadShared"/> /
/// <see cref="CobolFile.ReadLockGovern"/>.</para></summary>
public enum FileRecordLock
{
    /// <summary>No phrase — the connector's LOCK MODE governs (AUTOMATIC auto-locks; MANUAL/None do not).</summary>
    None,
    /// <summary>WITH LOCK — request a lock on the accessed record (MANUAL locking).</summary>
    WithLock,
    /// <summary>WITH NO LOCK — do not lock the accessed record.</summary>
    WithNoLock,
}

/// <summary>The RETRY phrase kind (ISO §14.7.9).</summary>
public enum FileRetryKind
{
    /// <summary>No RETRY phrase — a single lock attempt (GR4a).</summary>
    None,
    /// <summary>RETRY n TIMES — the lock check is attempted n+1 times (GR1); a zero or negative n makes no
    /// further attempt (GR4a).</summary>
    Times,
    /// <summary>RETRY FOR n SECONDS — a timeout period clamped by GR2 to the implementor's MAXIMUM MEANINGFUL
    /// VALUE, which COBOL.NET defines as ZERO (Annex A.1 item 166, recorded in docs/CONFORMANCE.md §7): a lock
    /// here is held only by a file connector of the executing run unit, which cannot release it while this
    /// statement runs, so no positive timeout could change the outcome. The period is therefore zero-length,
    /// no further attempt is made, and the conflict's OWN §9.1.13 status stands — never a sleep.</summary>
    Seconds,
    /// <summary>RETRY FOREVER — GR3's unbounded wait. The conflict's own §9.1.13 status stands, except that a
    /// wait on a record locked by another file connector is the deadlock §9.1.13.8 item 2 defines and this
    /// implementation detects ('52'; A.1 item 109). See <c>FileRegistry.ExhaustionStatus</c>.</summary>
    Forever,
}

/// <summary>What an honest probe of a physical file's presence can answer — <b>three</b> states, not two.
/// <para>ISO/IEC 1989:2023 draws the distinction and prices it in two different I-O status values: §9.1.13.6
/// item 5 sets '35' because <i>"an OPEN statement with the INPUT, I-O, or EXTEND phrase is attempted on a file
/// that is not described as optional and the physical file is not present"</i>, while §14.9.27.4 GR3 sets '37'
/// because <i>"the file associated with file-name-1 is present and insufficient authority exists to open the
/// file"</i> — restated as §9.1.13.6 item 6 b), which adds <i>"The ability to detect this is processor
/// dependent"</i>. A BOOLEAN probe cannot express the difference, so it has to invent one of the two answers,
/// and <c>File.Exists</c> invents the wrong one: it swallows every access error and returns false. A present
/// file the process may not observe is then reported to the COBOL program as ABSENT — '35' where the standard
/// requires '37', and for an OPTIONAL file a <i>successful</i> open over an invented empty file (kb/Work PB323;
/// the DELETE FILE twin of the same defect was kb/Work PB140).</para></summary>
public enum FilePresence
{
    /// <summary>The physical file is not present — §9.1.13.6 item 5's '35' for a non-optional INPUT/I-O/EXTEND,
    /// and the condition §14.9.27.4 GR13 and GR17 both open with (<i>"If the file is not present"</i>) for the
    /// OPTIONAL at-end and creation arms.</summary>
    Absent,
    /// <summary>The physical file is present and observable.</summary>
    Present,
    /// <summary>The process was refused the authority even to observe whether the file is there. §14.9.27.4 GR3
    /// and §9.1.13.6 item 6 b) make this '37' — never '35', and never an OPTIONAL file's invented empty one:
    /// a refusal is not evidence of absence.</summary>
    Unauthorized,
}

/// <summary>Host-filesystem probes shared by every file organization. ⛔ <b>THIS IS THE ONE PLACE THE RUNTIME
/// ASKS WHETHER A PHYSICAL FILE IS PRESENT</b> — <see cref="FileConnector.Open"/> takes the answer once per OPEN
/// and hands it to the organization's <c>OpenCore</c>, and <c>FileRegistry.DeleteFile</c> takes it for
/// §14.9.10.4 GR14/GR16. <c>FileExistenceProbeDriftTests</c> keeps it that way: <c>File.Exists</c>,
/// <c>FileInfo.Exists</c> and <c>File.GetAttributes</c> are forbidden everywhere else under
/// <c>Cobol.Net.Runtime/IO</c>, because every hand-written probe is another chance to answer "absent" to a
/// question the operating environment refused to answer at all.</summary>
public static class HostFile
{
    /// <summary>Probe the physical file at <paramref name="hostPath"/>, distinguishing §9.1.13.6 item 5's
    /// ABSENT from §14.9.27.4 GR3's PRESENT-but-unauthorized. Never throws for either of those two.
    /// <para>The probe is <c>File.GetAttributes</c> rather than <c>File.Exists</c> precisely because its
    /// FAILURES carry the distinction: a missing file or directory raises
    /// <see cref="FileNotFoundException"/>/<see cref="DirectoryNotFoundException"/>, while a refused one raises
    /// <see cref="UnauthorizedAccessException"/>. <c>File.Exists</c> collapses both to <c>false</c>.</para>
    /// <para>A DIRECTORY at the path answers <see cref="FilePresence.Absent"/>: §9.1.6's fixed file attributes
    /// describe a <i>physical file</i>, and a directory of that name is not one — so "the physical file is not
    /// present" (§9.1.13.6 item 5) is the true statement about it. That is also the answer <c>File.Exists</c>
    /// gave, so the OPEN path is unchanged; DELETE FILE on a directory moves from a factually wrong '37'
    /// ("insufficient authority") to §14.9.10.4 GR14's '05', the successful completion for a file that is not
    /// there.</para>
    /// <para>A path the host will not accept as a file name at all — too long for it
    /// (<see cref="PathTooLongException"/>), or malformed (<see cref="ArgumentException"/>,
    /// <see cref="NotSupportedException"/>) — likewise names no present file. That is a statement about the
    /// PATH, not an input-output failure, so it belongs with Absent rather than with '30'; it is also the
    /// reading <c>FixedFileAttributes.Load</c> already takes, and the answer <c>File.Exists</c> gave.</para>
    /// <para>Every OTHER <see cref="IOException"/> PROPAGATES on purpose: §9.1.13.6 item 1's '30' (<i>"A
    /// permanent error exists and no further information is available concerning the input-output
    /// operation"</i>) is mapped in exactly ONE place — <see cref="FileConnector.Open"/>'s catch — and a probe
    /// that swallowed it here would have to guess between '35' and '37' for a file it never reached.</para></summary>
    public static FilePresence Probe(string hostPath)
    {
        try
        {
            return File.GetAttributes(hostPath).HasFlag(FileAttributes.Directory)
                ? FilePresence.Absent : FilePresence.Present;
        }
        catch (FileNotFoundException) { return FilePresence.Absent; }              // §9.1.13.6 item 5
        catch (DirectoryNotFoundException) { return FilePresence.Absent; }         // §9.1.13.6 item 5
        catch (UnauthorizedAccessException) { return FilePresence.Unauthorized; }  // §14.9.27.4 GR3 / item 6 b)
        catch (PathTooLongException) { return FilePresence.Absent; }               // a path shape, not an I/O failure
        catch (ArgumentException) { return FilePresence.Absent; }                  // not a file name the host takes
        catch (NotSupportedException) { return FilePresence.Absent; }
    }
}
