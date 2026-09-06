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
/// <see cref="CobolFile.ReadKeyedShared"/>.</para></summary>
public enum FileRecordLock
{
    /// <summary>No phrase — the connector's LOCK MODE governs (AUTOMATIC auto-locks; MANUAL/None do not).</summary>
    None,
    /// <summary>WITH LOCK — request a lock on the accessed record (MANUAL locking).</summary>
    WithLock,
    /// <summary>WITH NO LOCK — do not lock the accessed record.</summary>
    WithNoLock,
}

/// <summary>Which ADVANCING phrases ONE WRITE statement carries — the print-control bracket of ISO §14.9.51.2
/// Format 1. It is the WRITE's <i>presentation</i> shape, and nothing else: it does NOT decide whether the
/// statement is lock-governed.
/// <para>⛔ A WRITE WITH AN ADVANCING PHRASE IS STILL A WRITE STATEMENT. §14.9.51.4 GR10 and GR11 are ALL FILES
/// rules — <i>"If the locking mode of the write file connector is single record locking, any record lock
/// associated with that file connector is released by the execution of the WRITE statement"</i> and <i>"If
/// record locks have an effect for the write file connector and the WITH LOCK phrase is specified or implied,
/// the record lock associated with the record written is set …"</i> — and Format 1's printed general format
/// carries the <c>retry-phrase</c> and the <c>WITH LOCK / WITH NO LOCK</c> bracket ALONGSIDE the ADVANCING
/// phrase, so `WRITE R AFTER ADVANCING 1 LINE WITH LOCK RETRY 5 TIMES` is one legal statement. This descriptor
/// therefore RIDES WITH the record into the ONE governed entry (<see cref="CobolFile.WriteShared"/>); it never
/// selects a separate ungoverned one. Three separate emitter arms used to, and the two advancing arms dropped
/// the lock and RETRY phrases on the floor (kb/Work PB683).</para></summary>
public enum WriteAdvanceKind : byte
{
    /// <summary>No ADVANCING phrase — the plain record write.</summary>
    None,
    /// <summary>BEFORE ADVANCING — the record is presented, then the medium advances.</summary>
    Before,
    /// <summary>AFTER ADVANCING — the medium advances, then the record is presented.</summary>
    After,
    /// <summary>COBOL-2023's combined <c>BEFORE ADVANCING n AFTER ADVANCING m</c> (§14.9.51.4 GR25 e/f): the
    /// record is presented once at the current line and the medium then advances by both amounts.</summary>
    BeforeAndAfter,
}

/// <summary>The ADVANCING phrases of ONE WRITE statement, as data — see <see cref="WriteAdvanceKind"/> for why
/// this travels as an argument of the governed WRITE rather than as a choice of runtime entry.</summary>
/// <param name="Kind">Which phrases the statement wrote.</param>
/// <param name="Lines">The single phrase's line count, or the BEFORE amount of the combined form; <c>-1</c> is
/// ADVANCING PAGE, which §14.9.51.3 SR17 permits only in the single-phrase form.</param>
/// <param name="AfterLines">The AFTER amount of the combined form; unused otherwise.</param>
public readonly record struct WriteAdvance(WriteAdvanceKind Kind, int Lines, int AfterLines)
{
    /// <summary>A WRITE with no ADVANCING phrase.</summary>
    public static WriteAdvance None => default;
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
/// ASKS THE OPERATING ENVIRONMENT ABOUT A PHYSICAL FILE</b> — two questions, each asked once per OPEN by
/// <see cref="FileConnector.Open"/> and never by an organization body: <see cref="Probe"/> ("is it there, and
/// may this process observe it?", handed down to <c>OpenCore</c>; <c>FileRegistry.DeleteFile</c> takes the same
/// answer for §14.9.10.4 GR14/GR16) and <see cref="PermitsWrite"/> ("may this process WRITE it?", §14.9.27.4
/// GR16 / §9.1.13.6 item 6 a)). <c>HostFileProbeDriftTests</c> keeps it that way: <c>File.Exists</c>,
/// <c>FileInfo.Exists</c> and <c>File.GetAttributes</c> are forbidden everywhere else under
/// <c>Cobol.Net.Runtime/IO</c>, because every hand-written probe is another chance to answer "absent" to a
/// question the operating environment refused to answer at all — and <see cref="PermitsWrite"/> may be CALLED
/// only from <see cref="FileConnector"/>, because a capability asked inside one organization's arm is a
/// capability the other organizations do not ask (kb/Work PB328).</summary>
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

    /// <summary>Whether the operating environment permits WRITE operations on the PRESENT physical file at
    /// <paramref name="hostPath"/> — the question §14.9.27.4 GR16 and §9.1.13.6 item 6 a) price at '37'.
    /// <para>⛔ THE PROBE IS A REAL WRITE OPEN, because nothing weaker answers the question the standard asks.
    /// A read-only ATTRIBUTE, a mode bit and a deny-write ACE are three different mechanisms with the same
    /// consequence, and a host may add a fourth; asking the host to hand over a writable handle asks about the
    /// consequence rather than about any one mechanism. <c>FileMode.Open</c> (never <c>Create</c>/<c>Truncate</c>)
    /// with no bytes written leaves the file byte-for-byte and timestamp-for-timestamp as it was, which
    /// §14.9.27.4 GR25 requires of an OPEN that is about to be unsuccessful (<i>"If the execution of the OPEN
    /// statement is unsuccessful, the file is not affected"</i>) and which the SUCCESSFUL path needs just as
    /// much — the probe closes before the organization's own stream opens. <c>FileShare.ReadWrite</c> is the
    /// most permissive request there is, so the probe itself never manufactures a sharing failure that the
    /// organization's own open would not have hit.</para>
    /// <para>ONLY <see cref="UnauthorizedAccessException"/> is an answer. Every other
    /// <see cref="IOException"/> PROPAGATES on purpose, exactly as <see cref="Probe"/>'s do: §9.1.13.6 item 1's
    /// '30' (<i>"A permanent error exists and no further information is available concerning the input-output
    /// operation"</i>) is mapped in ONE place — <see cref="FileConnector.Open"/>'s catch — and a probe that
    /// swallowed a host refusal here would have to invent "writable" for a file it never reached. That is also
    /// what the sequential organization has always done, since its I-O/EXTEND streams are themselves write
    /// opens; the probe makes the keyed organizations answer the same way (kb/Work PB328).</para></summary>
    public static bool PermitsWrite(string hostPath)
    {
        try
        {
            using var _ = new FileStream(hostPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }   // §9.1.13.6 item 6 a) — '37'
    }
}
