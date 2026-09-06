// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE PLACE THE OPERATING ENVIRONMENT'S SHARE MODE IS DECIDED — ISO §9.1.15's <b>file lock</b>, as a
/// derivation from the arbitrated sharing mode and the set of file connectors the Table-19 arbiter has already
/// admitted on the physical file, rather than as a boolean at four stream sites (kb/Work PB740).
/// </summary>
/// <remarks>
/// <para>§9.1.15 names two audiences for one sharing mode, and they need two different mechanisms:</para>
/// <list type="number">
/// <item><description><b>Other file connectors of this run unit.</b> <i>"Multiple paths of access may exist in
/// the same runtime element, contained elements, separate runtime elements within the same run unit, or runtime
/// elements in different run units."</i> — and for them the gate is stated exactly: <i>"Before access to a
/// shared physical file is allowed through an OPEN statement, the sharing mode and the open mode of that OPEN
/// statement shall be allowed by all other file connectors that are currently associated with the physical
/// file, as described in 9.1.13, I-O status; 14.9.27, OPEN statement; and Table 19"</i>. <see cref="Table19"/>
/// IS that gate. Nothing may add a second, stricter one — which is precisely what the operating environment's
/// share mode had become: two clause-less connectors the arbiter PERMITS to share one file (OPEN INPUT, then
/// OPEN EXTEND) were refused by the host and answered '30', a status no row of Table 19 and no item of
/// §9.1.13.9 can produce.</description></item>
/// <item><description><b>Other run units.</b> <i>"The successful opening of a file establishes a file lock for
/// the applicable sharing rules, thereby preventing other run units from opening that file with incompatible
/// sharing rules."</i> — THAT is the OS share mode's job, and the only one it can do, because a host share mode
/// names no requester: it cannot admit this run unit's second connector while refusing a foreign process.
/// </description></item>
/// </list>
/// <para><b>So the posture is a union, not a choice.</b> A connector's handle carries the file lock its own
/// sharing mode establishes (<see cref="OfSharingMode"/>), widened by the access every OTHER connector the
/// arbiter has admitted on that physical file already holds (<see cref="For"/>). The widening is what makes
/// rule 1 hold; the base is what makes rule 2 hold. Because Windows checks a new handle against EVERY
/// outstanding one, the run unit's effective file lock against a foreign process stays the INTERSECTION of its
/// connectors' postures — so widening one connector to admit a sibling does not, by itself, admit an outsider
/// the other connector still refuses.</para>
/// <para>⛔ WHY THE UNDETERMINED DEFAULT IS NOT DECIDED HERE. §9.1.15: <i>"If no specification is made in either
/// location, the implementor defines the sharing mode in which the file is opened"</i>, and this compiler has
/// not made that determination (<see cref="FileRegistry.ImplementorDefaultSharing"/> is <c>null</c>, kb/Work
/// PB322). Its file lock is therefore an owner-facing question — should a clause-less connector protect its
/// physical file from other PROCESSES at all? — and until it is answered the value below is today's behaviour
/// kept byte for byte: <see cref="FileShare.Read"/>, the posture the .NET path constructors gave. Changing it
/// is a one-line change HERE and nowhere else, which is the point of the structure.</para>
/// </remarks>
public static class FileLockPosture
{
    /// <summary>The §9.1.15 file lock a sharing mode establishes against OTHER RUN UNITS — the share mode a
    /// connector's own handle carries when it is alone on the physical file.
    /// <para>The mapping is the three rules read literally. 1) <i>"The sharing with no other mode specifies
    /// exclusive access to a physical file"</i> ⇒ <see cref="FileShare.None"/>. 2) <i>"The sharing with read
    /// only mode restricts concurrent access to a physical file through file connectors other than this one, to
    /// input mode"</i> ⇒ <see cref="FileShare.Read"/>. 3) <i>"The sharing with all other mode allows concurrent
    /// access to a physical file through other file connectors specifying input, I-O, or extend mode"</i> ⇒
    /// <see cref="FileShare.ReadWrite"/>.</para>
    /// <para>⚠ Rule 1 is the half this compiler had INVERTED (kb/Work PB740): a connector that wrote
    /// <c>SHARING WITH NO OTHER</c> was a "sharing participant", and participants were given
    /// <see cref="FileShare.ReadWrite"/> — so writing the most restrictive sharing mode the standard has was
    /// what let a foreign process append to the file while the program held it open, measured across processes,
    /// while a connector that wrote no clause at all refused the same write.</para></summary>
    public static FileShare OfSharingMode(FileSharing? sharing) => sharing switch
    {
        FileSharing.NoOther => FileShare.None,        // §9.1.15 1) — exclusive access
        FileSharing.ReadOnly => FileShare.Read,       // §9.1.15 2) — others restricted to input mode
        FileSharing.AllOther => FileShare.ReadWrite,  // §9.1.15 3) — others may specify input, I-O or extend
        // The implementor default (§9.1.15) — UNDETERMINED, kb/Work PB322. Today's posture, unchanged.
        null => FileShare.Read,
        _ => throw new ArgumentOutOfRangeException(nameof(sharing), $"{sharing} is not an ISO §9.1.15 sharing mode"),
    };

    /// <summary>The access an open mode needs OF THE PHYSICAL FILE (ISO §9.1.4 open modes): INPUT reads, OUTPUT
    /// and EXTEND write, I-O does both (§14.9.35 GR3 — REWRITE replaces the record a READ retrieved, through the
    /// one connector).</summary>
    public static FileAccess AccessOf(FileOpenMode mode) => mode switch
    {
        FileOpenMode.Input => FileAccess.Read,
        FileOpenMode.Output or FileOpenMode.Extend => FileAccess.Write,
        FileOpenMode.IO => FileAccess.ReadWrite,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), $"{mode} is not an ISO §9.1.4 open mode"),
    };

    /// <summary>The share flags a handle shall carry to ADMIT <paramref name="access"/> on another handle. The
    /// host's rule is symmetric — a new open succeeds only if its access is admitted by every outstanding
    /// handle's share mode AND its own share mode admits every outstanding handle's access — so this is the
    /// one conversion both directions need.</summary>
    public static FileShare Admitting(FileAccess access) =>
        ((access & FileAccess.Read) != 0 ? FileShare.Read : FileShare.None)
        | ((access & FileAccess.Write) != 0 ? FileShare.Write : FileShare.None);

    /// <summary>The posture ONE connector's own handles shall carry: the §9.1.15 file lock of
    /// <paramref name="sharing"/>, widened to admit every mode in <paramref name="otherOpenModes"/> — the
    /// connectors of THIS run unit that the Table-19 arbiter has already admitted on the same physical file
    /// (<see cref="FileRegistry.Conflicts"/> is what put them there, so the widening never admits an open the
    /// standard refused).
    /// <para>A <c>SHARING WITH NO OTHER</c> connector is never widened in practice and the code needs no special
    /// case for it: every cell of Table 19's <c>NoOtherAnyMode</c> row and column is <i>Unsuccessful open</i>,
    /// so its <paramref name="otherOpenModes"/> is always empty and the union is <see cref="FileShare.None"/>.
    /// <c>FileLockPostureDriftTests</c> proves that rather than asserting it.</para></summary>
    public static FileShare For(FileSharing? sharing, IEnumerable<FileOpenMode> otherOpenModes)
    {
        var share = OfSharingMode(sharing);
        foreach (var mode in otherOpenModes) share |= Admitting(AccessOf(mode));
        return share;
    }

    /// <summary>Would a handle carrying <paramref name="share"/> admit a handle of this run unit — or of any
    /// other — WRITING to the same physical file? The predicate the sequential write path needs: when it is
    /// true the connector's release must land at the physical end as it stands at that moment and be flushed
    /// whole (§14.9.51.4 GR12/GR19, kb/Work PB739), and when it is false the connector holds the only writable
    /// handle there is and keeps the plain, buffered append it always had.
    /// <para>A bit test rather than <c>HasFlag</c>: <see cref="SequentialConnector"/> asks this once per
    /// released record, which is the sequential WRITE path.</para></summary>
    public static bool AdmitsAnotherWriter(FileShare share) => (share & FileShare.Write) != 0;
}
