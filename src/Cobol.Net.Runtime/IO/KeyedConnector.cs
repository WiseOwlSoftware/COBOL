// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The ACCESS MODE of a keyed file connector (ISO/IEC 1989:2023 §12.4.5.5, ACCESS MODE clause). The
/// ordinals mirror the compiler's <c>FileAccessMode</c> enum — registration passes the raw int.</summary>
public enum KeyedAccess
{
    /// <summary>ACCESS SEQUENTIAL — records in ascending RRN / key-of-reference order (§12.4.5.5.3 GR2).</summary>
    Sequential = 0,
    /// <summary>ACCESS RANDOM — records selected by key value (§9.1.8.3).</summary>
    Random = 1,
    /// <summary>ACCESS DYNAMIC — both, chosen per statement form (§9.1.8.4).</summary>
    Dynamic = 2,
}

/// <summary>
/// The shared base of the KEYED connectors — RELATIVE (§9.1.7.3) and INDEXED (§9.1.7.4) — carrying the one fact
/// every keyed verb branches on: the connector's ACCESS MODE. A sequential-organization connector has no access
/// mode to carry (§12.4.5.5.2 SR2 — <i>"The DYNAMIC and RANDOM phrases shall not be specified for a
/// sequential file"</i>), which is why this sits below <see cref="FileConnector"/> rather than on it.
/// That premise is now ENFORCED rather than assumed: until kb/Work PB692 the rule had no check anywhere and
/// <c>ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM</c> compiled and ran, so this class's reason for
/// existing was a comment about source the compiler accepted. <c>DataBinder.BindFileControl</c> screens it on
/// the file control entry (COBOLNET1858, every edition), which is what makes "a sequential-organization
/// connector has no access mode" a fact about every program that reaches the runtime.
///
/// ⛔ INVARIANT (kb/Work PB325) — <b>the OPEN MODE never SELECTS a keyed verb's branch; it is only ever
/// SCREENED.</b> Every branch the standard draws inside a keyed verb is drawn on the ACCESS MODE:
/// §14.9.51.4 GR29 a)/b) (relative WRITE: consecutive release vs. the staged RELATIVE KEY), GR38/GR39 (indexed
/// WRITE: the ascending-prime-key requirement vs. "in any order"), §14.9.35.4 GR5 vs. GR21/GR22/GR23 and
/// §14.9.10.4 GR2 vs. GR3/GR4 (REWRITE/DELETE target: the last-read record vs. the key item's). The OPEN MODE
/// enters only as the permission test that follows — §14.9.27.4 GR8's Table 20, whose unsuccessful cells
/// §9.1.13.7 items 8 and 9 name ('48' for WRITE, '49' for REWRITE/DELETE) — and item 8's own two arms are
/// themselves selected BY the access mode: a) sequential ⇒ extend or output, b) random or dynamic ⇒ I-O or
/// output.
///
/// Folding the open mode into a branch predicate ("sequential access <i>or</i> extend mode") therefore inverts
/// the dependency and makes the runtime's answer depend on a bind-time screen holding: §14.9.27.3 SR2 confines
/// EXTEND to sequential access, so a random- or dynamic-access connector open in the extend mode is a state the
/// SOURCE cannot legally reach — but the runtime must still answer '48' for it (Table 20 leaves the
/// Random/Extend and Dynamic/Extend WRITE cells blank), not divert into the sequential-release branch and
/// succeed. That divergence was PB325.
/// </summary>
public abstract class KeyedConnector : FileConnector
{
    protected KeyedConnector(string hostPath, int recordWidth, KeyedAccess access, int varyMin, int varyMax)
        : base(hostPath, recordWidth, varyMin, varyMax) => Access = access;

    /// <summary>The connector's ACCESS MODE (§12.4.5.5) — the sole discriminator of every keyed verb's branch;
    /// see the type remarks for why the open mode is never part of one.</summary>
    protected KeyedAccess Access { get; }

    /// <summary>Whether this run unit validates the INDEXED key table (<see cref="FileRegistry.KeyCheckVariable"/>
    /// — <c>COBOLNET_KEYCHECK=OFF</c> turns it off). Handed down by the registry at registration, exactly as
    /// <see cref="FileConnector.SharedStores"/> is, so a connector never has to ask an ambient run unit which
    /// registry it belongs to. A standalone connector (no registry) validates, the safe default.</summary>
    internal bool KeyCheck { get; set; } = true;

    /// <inheritdoc/>
    /// <remarks>⛔ THE KEYED ORGANIZATIONS' §14.9.27.4 GR10 ANSWER: the framed store's HEADER is the physical
    /// file's §9.1.6 fixed file attributes, so the comparison is that header against
    /// <see cref="FileConnector.DeclaredAttributes"/> (<see cref="FixedFileAttributes.Conflicts"/> — the one
    /// place the comparison is written). RELATIVE and INDEXED share this method because they share the format;
    /// what differs between them is only what <see cref="FileConnector.DeclaredKeys"/> supplies, which is where
    /// that difference belongs.
    /// <para>⛔ A STORE WITH NO READABLE HEADER IS A CONFLICT, NOT "attributes not recorded". Its layout is
    /// unknown, so its frames cannot be located either, and reading it anyway is the silent misread this rule
    /// exists to prevent — a plain sequential file opened through a RELATIVE description delivers rubbish with
    /// status '00' if nothing refuses it. A ZERO-BYTE file is the one exception
    /// (<see cref="StoreFormat.Empty"/>): it holds no records, so it can misread nothing and states no attribute
    /// to contradict.</para>
    /// <para>The header read is ONE bounded auxiliary open, and it replaces the catalog sidecar's open
    /// one-for-one — the file count went down by one and the open count did not go up (kb/Work PB802). It cannot
    /// ride the connector's own store load, because GR25 ("If the execution of the OPEN statement is
    /// unsuccessful, the file is not affected") puts this check BEFORE <c>OpenCore</c>, whose creation arms
    /// truncate.</para>
    /// <para>⛔ A HOST REFUSAL OF THAT READ CANNOT SILENTLY SKIP THE CHECK, and the reason is structural rather
    /// than lucky: <see cref="RecordFraming.ReadHeader"/> answers <see cref="StoreFormat.Empty"/> (no conflict)
    /// on an I/O failure, but it takes exactly the handle shape — <c>HostFile.OpenAuxiliary</c>, share
    /// <see cref="FileShare.ReadWrite"/> — that <see cref="RecordFraming.ReadStore"/> takes a few statements
    /// later inside <c>OpenCore</c>, where the failure PROPAGATES to <c>FileConnector.Open</c>'s catch and
    /// becomes '37' or '30'. So any state that refuses the header read refuses the store load too, and the OPEN
    /// is unsuccessful either way; the swallow can lose a '39' only in favour of another unsuccessful status,
    /// never in favour of a successful OPEN over a store this connector could not interpret. Answering the
    /// authority statuses here instead would be a SECOND place §9.1.13.6 item 1's '30' is decided, which
    /// <c>FileConnector.Open</c>'s catch is the one place for.</para></remarks>
    protected override bool FixedAttributeConflict() =>
        RecordFraming.ReadHeader(HostPath, out var recorded) switch
        {
            StoreFormat.Empty => false,
            StoreFormat.Described => recorded!.Conflicts(DeclaredAttributes, KeyCheck),
            _ => true,   // Foreign — not a store this build can interpret
        };
}
