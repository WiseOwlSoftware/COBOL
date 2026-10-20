// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A typed-native INDEXED-organization file connector (ISO/IEC 1989:2023 §9.1.7.4): records identified by a unique
/// PRIME record key plus optional ALTERNATE record keys (each a byte range of the record's character image,
/// §12.4.5.12/§12.4.5.6).
/// <para>⛔ THE RECORDS LIVE IN THE KEYED STORE ENGINE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.3 decision
/// 3 and 2.2.1.4; kb/Work PB3334): the PRIME tree (the prime key's channel bytes → the record's frame) and one COMPOSITE
/// tree per alternate key ((the key's bytes, the record's release ordinal under it) → the prime key), each under the key
/// order its collating sequence selects (<see cref="IndexedKeyOrders"/>), in <see cref="IndexedStore"/>. A record is n +
/// 1 entries; every statement reads the pages it touches and writes the pages it changes, then page 2, so a READ by key
/// is a seek, READ NEXT a cursor step and START a seek under the partial key — the per-call sorts and whole-file scans
/// of the record list this replaced are gone (kb/Work PB2129). The statuses and their order (docs/CONFORMANCE.md
/// DOC-A.1-104) are this connector's, and only the medium changed.</para>
/// <para>⛔ THE DUPLICATE ORDER IS EACH KEY'S RELEASE ORDINAL (§14.9.30.4 GR26, §14.9.51.4 GR40, §14.9.35.4 GR24; kb/Work
/// PB341, PB2693): a WRITE mints ONE ordinal from the file's release mint and stamps it under every key; a REWRITE
/// re-stamps only the keys it changed, each moved to the end of its duplicate set by a fresh ordinal; the ordinal is the
/// trailing part of the alternate tree's entry key, so a tree's order IS its duplicate order and nothing is ever
/// renumbered.</para>
/// <para>⛔ THE FILE POSITION INDICATOR IS A KEY VALUE AND NOTHING ELSE (kb/Work PB342). §14.9.41.4 GR17 e) 1. —
/// "The file position indicator is set to the value of the key of reference in the first logical record whose key
/// satisfies the comparison" — and §14.9.30.4 GR21 g)/GR32 say the same of a READ. The standard never puts a
/// duplicate POSITION in it, which is why §14.9.30.4 GR21 rules e) and f) have to name "the record that was made
/// available by that prior READ statement" instead of consulting it. This connector models exactly that: the FPI
/// (<c>_fpiKey</c>) is the key value, and the prior READ's position within its set of duplicates
/// (<c>_readOrdinal</c>) is separate state consulted ONLY under rules e)/f). Modelling the FPI as the pair
/// (key, ordinal) made a START into a duplicate set resume at the wrong end and lose the rest of the set.
/// Positions are held as VALUES, not record references, so they survive an interleaved REWRITE/DELETE of the
/// current record (§14.9.10.4 GR9) and another run unit's change: every sequential READ is a fresh seek from them.</para>
/// </summary>
public sealed class IndexedConnector : KeyedConnector
{
    /// <summary>The prime key's byte windows, in order (<see cref="KeySegment"/>; one for data-name-1, one per
    /// data-name-2 of the SOURCE phrase), and their total width — the key's length.</summary>
    private readonly KeySegment[] _prime;
    private readonly int _primeLen;
    private readonly CobolCollation? _primeCollation;   // §12.4.5.7 prime-key collating sequence AS DECLARED (the CobolCollation carrier); null = native ordinal
    private readonly List<(KeySegment[] Segments, int Len, bool Dups, CobolCollation? Collation, string? Suppress)> _alts = [];
    /// <summary>The key sequences IN EFFECT for the current OPEN — index 0 the prime key, 1.. the alternates — each the
    /// declared sequence's <see cref="CobolCollation.Snapshot"/> taken by <see cref="FreezeOpenSequences"/>. Every key
    /// comparison and the declared attributes read these (<see cref="SequenceOf"/>), never the declared carriers: a
    /// declared current-locale sequence (<see cref="LocaleCollation.Current"/>) resolves the run unit's locale at each
    /// use, and a SET LOCALE between two statements on the open file would otherwise reorder the keys it holds.</summary>
    private CobolCollation?[] _openSequences = [];
    /// <summary>True when ANY key's position varies with the record (<see cref="KeyOf"/>): such a key is located
    /// in the record AS WRITTEN — its length is what places it — so no key is ever sliced from a record image
    /// padded or truncated to the area width (<see cref="AreaKey"/>, and the WRITE / REWRITE key image).</summary>
    private bool _layoutKeys;

    /// <summary>The attached PER-PHYSICAL-FILE store (kb/Work PB143): every connector over one host path drives ONE set
    /// of trees and ONE release mint. A placeholder until OPEN attaches (and after CLOSE detaches).</summary>
    private IndexedStore _st = new();

    /// <summary>The key orders of this connector's trees and each alternate key's SUPPRESS WHEN value as channel bytes —
    /// chosen at every OPEN (2.2.1.4, 2.2.1.5), once the file's CODE-SET is known.</summary>
    private IndexedKeyOrders _orders = null!;
    private byte[]?[] _suppress = [];

    /// <summary>The key-index spelling of THE PRIME RECORD KEY: <c>_alts</c> is indexed from 0, so the prime key
    /// is the one index below it — the same value as <see cref="KeyedStore.TreeChange.PrimeRoot"/>, the prime tree's.
    /// Every key-index parameter on this connector reads it, and it is what §14.9.27.4 GR14 and §14.9.41.4 GR18/GR19
    /// mean by "the prime record key" / "the primary key".</summary>
    private const int PrimeKey = KeyedStore.TreeChange.PrimeRoot;

    private int _refKey = PrimeKey;      // key of reference: PrimeKey, or i = i-th alternate (§14.9.30.4 GR30/GR31)
    private byte[] _fpiKey = [];         // THE file position indicator — a KEY VALUE (§14.9.41.4 GR17e1; §9.1.11)
    private ulong _readOrdinal;          // NOT the FPI: the prior READ's record's release ordinal under the key of
                                         // reference — §14.9.30.4 GR21 e)/f)'s "the record that was made available
                                         // by that prior READ statement" and its "logical position within the set
                                         // of duplicates". Meaningful only while _positioner is 'R'.
    private char _positioner = 'O';      // 'O' OPEN / 'S' START / 'R' READ — selects GR21 rule d) vs. e)/f)
    private byte[]? _lastWrittenPrime;   // sequential-access ascending check (§14.9.51 GR38; EXTEND seeds highest)
    private byte[]? _lastReadPrime;      // sequential-access REWRITE/DELETE target (§14.9.35 GR22 / §14.9.10 GR2)
    private byte[]? _lastWrittenPrimeId; // the record released by the last successful Write (§14.9.51 GR11)

    // ── Record-lock identity (ISO §9.1.16) — an indexed record's identity is its PRIME record key ────────────

    /// <inheritdoc/>
    public override string LastReadRecordId => Identity(_lastReadPrime);

    /// <inheritdoc/>  (sequential access targets the last-read record, §14.9.35.4 GR22 / §14.9.10.4 GR2;
    /// random/dynamic the record whose prime key is the record area's key slice, §14.9.35.4 GR23 /
    /// §14.9.10.4 GR3 — the ACCESS MODE alone selects the target, see <see cref="KeyedConnector"/>)
    protected override string MutationTargetRecordIdCore(string recordImage, RecordExtents? recordExtents) =>
        Access == KeyedAccess.Sequential
        ? SequentialMutationTarget
        : AreaKey(recordImage, recordExtents, PrimeKey);

    /// <inheritdoc/>
    public override string LastWrittenRecordId => Identity(_lastWrittenPrimeId);

    // RECORD IS VARYING: each record's frame carries its exact length; key slices pad on demand (KeyOf), so a varying
    // record persists at its written length (§13.18.43 GR13) and reports it on READ (GR15). Out-of-bounds
    // WRITE/REWRITE is the GR14/§14.9.35 GR20 '44'.

    /// <param name="primeKey">The prime record key's byte windows in order (§12.4.5.12): one for data-name-1, one per
    /// data-name-2 of the SOURCE phrase (GR2), each with its record type's layout when a variable-length member
    /// precedes it (kb/Work PB1025).</param>
    public IndexedConnector(string hostPath, int recordWidth, KeyedAccess access, KeySegment[] primeKey,
        int varyMin = -1, int varyMax = -1, CobolCollation? primeCollation = null)
        : base(hostPath, recordWidth, access, varyMin, varyMax)
    {
        _prime = CheckedKey(primeKey);
        _primeLen = WidthOf(_prime);
        _primeCollation = primeCollation;
        _layoutKeys = _prime.Any(s => s.Layout is not null);
    }

    /// <summary>Register one ALTERNATE RECORD KEY's byte windows and WITH DUPLICATES phrase (§12.4.5.6), with its
    /// optional §12.4.5.7 collating-weight table (null = native ordinal) and §12.4.5.6.4 GR6 SUPPRESS WHEN value
    /// (null = no suppression). <paramref name="key"/> is shaped as the prime key's is (one segment per
    /// data-name-2 of the SOURCE phrase, §12.4.5.6.4 GR2).</summary>
    public void AddAlternateKey(KeySegment[] key, bool duplicates, CobolCollation? collation = null, string? suppress = null)
    {
        var segments = CheckedKey(key);
        _alts.Add((segments, WidthOf(segments), duplicates, collation, suppress));
        _layoutKeys |= segments.Any(s => s.Layout is not null);
    }

    /// <summary>A key has at least one window (the general formats print data-name-1, or one or more data-name-2),
    /// so an empty one is a generation defect and fails loudly here rather than as a zero-length key that every
    /// record would duplicate.</summary>
    private static KeySegment[] CheckedKey(KeySegment[] key) =>
        key is { Length: > 0 } ? key : throw new ArgumentException("a record key has at least one segment", nameof(key));

    private static int WidthOf(KeySegment[] key)
    {
        int width = 0;
        foreach (var s in key) width += s.Length;
        return width;
    }

    /// <inheritdoc/>
    protected override string DeclaredOrganization => FixedFileAttributes.Indexed;

    /// <summary>§14.9.6.4 GR2 d) — <i>"A file with organization other than sequential, that resides on a mass
    /// storage device."</i> The category is settled by the ORGANIZATION alone, so it needs no medium
    /// determination (kb/Work PB235).</summary>
    public override PhysicalFileCategory Category => PhysicalFileCategory.NonSequential;

    /// <inheritdoc/>
    /// <remarks>§9.1.6 names the prime record key, the alternate record keys, the SUPPRESS WHEN attribute and
    /// "the collating sequence of the keys for indexed files" as fixed attributes of the physical file — the
    /// index structure this connector builds is exactly those. Index 0 is the prime key; the alternates follow in
    /// declaration order, which is the order their key numbers are assigned in, and §12.4.5.6.4 GR6's key suppression
    /// value is an ALTERNATE key's attribute only.
    /// <para>⛔ FOR INDEXED FILES THIS IS NOT ONLY IMPLEMENTOR LATITUDE. §12.4.5.12.4 GR3: "The data description
    /// of data-name-1 or data-name-2 as well as their relative location within a record shall be the same as
    /// that used when the file was created", and §12.4.5.6.4 GR3 says the same of every alternate key AND that
    /// "The number of alternate record keys for the file shall also be the same as that used when the physical
    /// file was created". Those are normative requirements with no consequence stated where they are written;
    /// §14.9.27.4 GR10's file attribute conflict condition is the mechanism that detects a violation, so
    /// putting the key descriptors in the validated set is what gives those two rules an effect.</para>
    /// <para>The recorded descriptor is the key's BYTE WINDOW plus its collating sequence, and that is the whole
    /// of "the data description … as well as their relative location" this implementation can act on:
    /// §12.4.5.12.3 SR2 confines a record key to category alphanumeric or category national,
    /// §12.4.5.12.4 GR1 makes key equality a
    /// relation condition under the file's collating sequence (recorded, via
    /// <see cref="FixedFileAttributes.Fingerprint"/>), and both native sequences are one code-unit ordinal over
    /// the UTF-16 substrate (CONFORMANCE.md DOC-A.1-33/188) — so two descriptions with the same window and the
    /// same sequence order every key value identically, whatever their category.</para></remarks>
    protected override IReadOnlyList<FixedFileAttributes.KeyDescriptor> DeclaredKeys
    {
        get
        {
            var keys = new List<FixedFileAttributes.KeyDescriptor>(_alts.Count + 1)
            {
                new(Windows(_prime), false, null, FixedFileAttributes.Fingerprint(SequenceOf(PrimeKey))),
            };
            for (int i = 0; i < _alts.Count; i++)
            {
                var (segments, _, dups, _, suppress) = _alts[i];
                keys.Add(new(Windows(segments), dups, suppress, FixedFileAttributes.Fingerprint(SequenceOf(i))));
            }
            return keys;
            // The recorded descriptor is each window's offset and length; a layout is how a record is READ, not
            // an attribute of the physical file.
            static KeySegment[] Windows(KeySegment[] key) => [.. key.Select(s => s with { Layout = null })];
        }
    }

    /// <inheritdoc/>
    /// <remarks>⛔ THE FORMAT BOUNDS A KEY BY ITS LEAF (2.2.1.4; docs/CONFORMANCE.md DOC-A.1-129): a key's channel bytes
    /// are one per character position (2.2.1.5), and a tree key holds at most <see cref="BTree.MaxKeyBytes"/> bytes — the
    /// prime key's whole, an alternate key's with its 8-byte release ordinal after it — so a declaration with a longer key
    /// is one the physical format cannot hold, and the OPEN refuses it before any file is touched.</remarks>
    /// <para>Under commit control the prime key is also a HOLDINGS key after its kind byte (2.2.1.10,
    /// <see cref="TransactionKeys.MaxIdentityBytes"/>), so one byte less fits.</para>
    private protected override bool FormatHolds() =>
        base.FormatHolds()
        && _primeLen <= (CommitControlled ? TransactionKeys.MaxIdentityBytes : BTree.MaxKeyBytes)
        && _alts.All(a => a.Len <= BTree.MaxKeyBytes - CompositeKeyOrder.OrdinalBytes);

    // ── The engine (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1) ─────────────────────────────────────

    /// <summary>The collating sequence key <paramref name="keyIndex"/> (&lt; 0, <see cref="PrimeKey"/>: the prime key)
    /// is ordered by for the current OPEN; null = native ordinal. Before the first OPEN, the declared one.</summary>
    private CobolCollation? SequenceOf(int keyIndex)
    {
        int slot = keyIndex < 0 ? 0 : keyIndex + 1;
        if (slot < _openSequences.Length) return _openSequences[slot];
        return keyIndex < 0 ? _primeCollation : _alts[keyIndex].Collation;
    }

    /// <inheritdoc/>
    private protected override void FreezeOpenSequences()
    {
        var sequences = new CobolCollation?[_alts.Count + 1];
        sequences[0] = _primeCollation?.Snapshot();
        for (int i = 0; i < _alts.Count; i++) sequences[i + 1] = _alts[i].Collation?.Snapshot();
        _openSequences = sequences;
    }

    /// <inheritdoc/>
    private protected override KeyedStore AttachedStore => _st;

    /// <inheritdoc/>
    private protected override KeyedStore AttachStore(bool create) =>
        _st = SharedStores is { } table
            ? create ? table.AttachCreated<IndexedStore>(HostPath) : table.Attach<IndexedStore>(HostPath)
            : new IndexedStore();

    /// <inheritdoc/>
    private protected override void ForgetStore() => _st = new IndexedStore();

    /// <inheritdoc/>
    private protected override RestoringTreesFactory Restoring => IndexedStore.Restoring(_orders, AlternateEntries);

    /// <inheritdoc/>
    private protected override byte[]? IdentityOf(string recordId) => recordId.Length == 0 ? null : KeyBytes(recordId);

    /// <summary>Every alternate tree entry a record's frame has — the key value under each alternate key (suppression
    /// applied, §12.4.5.6.4 GR6) composed with the record's release ordinal under that key: what a DELETE removes and what
    /// a ROLLBACK rebuilds from a before-image's ordinal vector (2.2.1.10). Null for a frame this writer cannot have
    /// produced (a torn page, '30').</summary>
    private IReadOnlyList<(int Key, byte[] Entry)>? AlternateEntries(byte[] frame)
    {
        if (Decoded(frame) is not { } record) return null;
        var entries = new List<(int Key, byte[] Entry)>(_alts.Count);
        for (int i = 0; i < _alts.Count; i++)
            if (AlternateValue(record.Image, record.Extents, i) is { } value)
                entries.Add((i, CompositeKeyOrder.Compose(value, OrdinalOf(record, i))));
        return entries;
    }

    /// <summary>The HOLDINGS key of a unique alternate key's value (2.2.1.10, finding R2-4): a key declared WITH DUPLICATES
    /// needs no holding, so null for one.</summary>
    private byte[]? UniqueValueHolding(int keyIndex, byte[]? value) =>
        value is null || _alts[keyIndex].Dups ? null : TransactionKeys.Value(keyIndex, value);

    /// <summary>The key's channel bytes — the bytes its tree compares (2.2.1.4, 2.2.1.5): one per character position, the
    /// file's CODE-SET conversion applied, as the frame payload carries them. Null for a value holding a character with no
    /// byte image, which no stored key can carry (a WRITE refused it with '91').</summary>
    private byte[]? KeyBytes(string native) =>
        RecordHasCharacterWithoutByteImage(native) ? null
        : FileCharacterSet.Medium.GetBytes(FileCharacterSet.ToChannel(native, CodeSet));

    /// <summary>The §9.1.16 lock identity of a record: its prime key in the native character set (null: none).</summary>
    private string Identity(byte[]? prime) =>
        prime is null ? "" : FileCharacterSet.FromChannel(FileCharacterSet.Medium.GetString(prime), CodeSet);

    /// <summary>The order of key <paramref name="keyIndex"/>'s values (not its tree's entries: an alternate tree's
    /// composite order adds the ordinal).</summary>
    private IKeyOrder OrderOf(int keyIndex) => keyIndex < 0 ? _orders.Prime : _orders.Alternates[keyIndex];

    /// <summary>The key order a key's collating sequence selects (2.2.1.4): ORDINAL bytes for the native sequence over
    /// the native character set, WEIGHTED (decode, then the sequence — §12.4.5.7.4) for a declared sequence or a CODE-SET
    /// conversion, whose channel byte order is the coded set's. <paramref name="keyLength"/> is the key's length in
    /// character positions, the record key a START's partial key is a prefix of (§14.9.41.4 GR14).</summary>
    private IKeyOrder KeyOrder(CobolCollation? collation, int keyLength) =>
        collation is null && CodeSet is null ? KeyOrders.Ordinal : new WeightedKeyOrder(CodeSet, collation, keyLength);

    // ── OPEN (ISO §14.9.27; the arms are KeyedConnector.OpenCore's) ─────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>§14.9.27 GR14 — the PRIME key becomes the key of reference and the file position indicator is set to
    /// the lowest collating position; the key orders are chosen here, once the file's CODE-SET is known.</remarks>
    private protected override void ResetPosition()
    {
        _refKey = PrimeKey;
        _fpiKey = []; _readOrdinal = 0; _positioner = 'O';
        _lastWrittenPrime = null;
        _lastReadPrime = null;
        _lastWrittenPrimeId = null;
        var alternates = _alts.Select((a, i) => KeyOrder(SequenceOf(i), a.Len)).ToArray();
        _orders = new IndexedKeyOrders(KeyOrder(SequenceOf(PrimeKey), _primeLen), alternates,
            [.. alternates.Select(order => (IKeyOrder)new CompositeKeyOrder(order))]);
        _suppress = [.. _alts.Select(a => a.Suppress is { } value ? KeyBytes(value) : null)];
    }

    /// <inheritdoc/>
    /// <remarks>§14.9.27 GR15: EXTEND positions after the highest prime key, which seeds the GR38 ascending-sequence
    /// check — the prime tree's last key.</remarks>
    private protected override string? Opened(FileOpenMode mode)
    {
        if (mode is not FileOpenMode.Extend) return null;
        if (_st.Edge(PrimeKey, _orders, last: true, out var cursor) != StoreStatus.Ok) return FileStatusCode.PermanentError;
        if (cursor is { HasCurrent: true }) _lastWrittenPrime = cursor.Key;   // §14.9.51 GR38 — highest existing
        return null;
    }

    // ── The record an entry names ───────────────────────────────────────────────────────────────────────────────

    /// <summary>One record a tree entry names: its prime key, its frame (decoded), the key of reference's value in it and
    /// its release ordinal under that key.</summary>
    private readonly record struct Found(byte[] Prime, StoredFrame Record, byte[] KeyPart, ulong Ordinal);

    /// <summary>The record a frame value holds, its image normalized as the WRITE stored it — a varying file's frames keep
    /// their exact stored lengths (§13.18.43 GR15 reports them on READ), fixed frames normalize to the record width; null
    /// for a value this writer cannot have produced (the torn page it is, '30').</summary>
    private StoredFrame? Decoded(byte[] frame) =>
        RecordFraming.DecodeFrame(frame, _st.FrameSlots, CodeSet) is not { } stored ? null
        : IsVarying ? stored
        : stored with { Image = Fit(stored.Image), Extents = null };   // a fitted fixed record is not the image a table describes

    /// <summary>A record's release ordinal under <paramref name="keyIndex"/> (slot 0 the prime key's; a slot past the
    /// frame's — a key the file does not record, under the <c>COBOLNET_KEYCHECK</c> opt-out — reads 0).</summary>
    private static ulong OrdinalOf(StoredFrame record, int keyIndex)
    {
        int slot = keyIndex + 1;
        return record.Ordinals is { } o && (uint)slot < (uint)o.Length ? (ulong)o[slot] : 0;
    }

    /// <summary>⛔ THE RECORD THE CURSOR'S ENTRY NAMES, OR THE NEXT ONE IN <paramref name="forward"/>'s DIRECTION THAT
    /// <paramref name="satisfies"/> ACCEPTS. A prime tree entry IS the record. An alternate tree entry names it by its
    /// prime key, and is the record's only when the record still carries the entry's ordinal under that key — always, but
    /// under the <c>COBOLNET_KEYCHECK</c> opt-out, where a connector whose description omits a key the file records does
    /// not maintain that key's tree (IndexedStore): such an entry is not an access path to any record and is passed
    /// over. Null when the walk runs out.</summary>
    private StoreStatus Resolve(BTree.Cursor cursor, int keyIndex, bool forward, Func<byte[], bool>? satisfies, out Found? found)
    {
        found = null;
        while (cursor.HasCurrent)
        {
            byte[] entry = cursor.Key;
            byte[] keyPart = keyIndex < 0 ? entry : IndexedStore.Split(entry).KeyPart;
            if (satisfies is not null && !satisfies(keyPart)) return StoreStatus.Ok;
            var status = cursor.ReadValue(out var value);
            if (status != StoreStatus.Ok) return status;
            byte[] prime = keyIndex < 0 ? entry : value;
            byte[]? frame = value;
            if (keyIndex >= 0 && (status = _st.Get(_orders, prime, out frame)) != StoreStatus.Ok) return status;
            if (frame is not null)
            {
                if (Decoded(frame) is not { } record) return StoreStatus.PermanentError;
                ulong ordinal = keyIndex < 0 ? OrdinalOf(record, PrimeKey) : IndexedStore.Split(entry).Ordinal;
                if (keyIndex < 0 || OrdinalOf(record, keyIndex) == ordinal)
                {
                    found = new Found(prime, record, keyPart, ordinal);
                    return StoreStatus.Ok;
                }
            }
            if ((status = forward ? cursor.Next() : cursor.Previous()) != StoreStatus.Ok) return status;
        }
        return StoreStatus.Ok;
    }

    // ── READ (ISO §14.9.30) ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Sequential <c>READ [NEXT]</c> in (key-of-reference value, THAT key's release ordinal) order
    /// (§14.9.30.4 GR21 indexed rules d–g; GR26 — duplicates in release order). Sets '02' when the key of
    /// reference is an alternate and the FOLLOWING record duplicates the key just read (GR27 a).</summary>
    public string ReadNext(out string image) => ReadSequential(out image, previous: false);

    /// <summary>Sequential <c>READ PREVIOUS</c> (COBOL-2002+; compiler-gated) — the same walk in reverse
    /// (§14.9.30.4 GR21 d–g apply symmetrically, and GR26 makes a duplicate set's reverse order the reverse of
    /// its release order — the legacy ignored key-of-reference/arrival on PREVIOUS, brief §2.3 #5; this connector
    /// orders both directions identically). Immediately after OPEN → at end at 2023, the first existing record
    /// at 2002/2014 (<see cref="DialectBehavior.IndexedReadPreviousAfterOpenAtEnd"/>,
    /// VERSION_CHANGE_REFERENCE row 29 — kb/Work PB344).</summary>
    public string ReadPrevious(out string image) => ReadSequential(out image, previous: true);

    /// <summary>ISO §14.9.30.4 GR21's "When the file is an indexed file" SELECTION — rules d)/e)/f) over the key of
    /// reference's tree, ONE SEEK from the position held as values: the record to be made available (null is rule d) 3/e)
    /// 3/f) 3's at-end condition), and, with <paramref name="lookAhead"/>, whether the next record in the walk's direction
    /// duplicates its key (GR27's '02').
    /// <para>⛔ SELECTION ONLY — IT COMMITS NOTHING (kb/Work PB338). Splitting it out of the read is what lets
    /// <see cref="PeekSequentialRecordId"/> answer GR9's "the record identified for access" BEFORE the file
    /// position indicator and the duplicate-set ordinal move, without a second copy of these rules.</para>
    /// </summary>
    private StoreStatus SelectSequentialRecord(bool previous, bool lookAhead, out Found? found, out bool duplicateFollows)
    {
        found = null;
        duplicateFollows = false;
        // ⛔ WHICH RULE GOVERNS THIS WALK IS DECIDED BY THE PREVIOUS OPERATION, NOT BY A STORED POSITION
        // (kb/Work PB342). §14.9.30.4 GR21 rule d) applies when the previous operation was an OPEN or a START:
        // the FPI then holds a KEY VALUE and nothing else (§14.9.41.4 GR17 e) 1.), so the comparison is on the
        // key ALONE and a duplicate set is entered at the end §14.9.30.4 GR26 names for the direction —
        // first-released going forward, last-released going backward: a seek on the KEY PART of the entries. Rules
        // e)/f) apply when it was a READ: only then is there a "record that was made available by that prior READ
        // statement" whose "logical position within the set of duplicates" the walk resumes strictly after (or
        // before): a seek on the whole entry, the key and its ordinal.
        // ⛔ §14.9.30.4 GR21 d) 3 — "If no such record is found or PREVIOUS is specified and the previous
        // operation on the file was an OPEN statement, the at end condition exists". THAT SENTENCE IS THE 2023
        // RULE, and Annex E.2 item 22 is the informative record that 2023 AMENDED it: "READ PREVIOUS statement
        // following an OPEN statement. Ensure that an at end condition occurs", justified by "the rule itself
        // stated that the first record would be retrieved. The rule itself has been amended such that an at end
        // condition would occur." So at 2002/2014 the after-OPEN case is DIRECTION-BLIND — the first existing
        // record under the key of reference, exactly what NEXT yields and exactly what the relative block's own
        // rule b) still says at every edition. (E.2 also mentions a prior NOTE that said at end normally
        // exists; this document is drafted to the ISO/IEC Directives Part 2, under which a note carries no
        // requirement, so the prior standard's normative content is the rule. And were the prior behaviour
        // already at end, E.2 — the list of SUBSTANTIVE CHANGES potentially affecting existing programs —
        // would have no item 22 to carry.) The edition is the COMPILING program's, baked on the connector at
        // registration; kb/Work PB344, VERSION_CHANGE_REFERENCE row 29.
        if (previous && _positioner == 'O'
            && DialectBehaviors.IsActive(DialectBehavior.IndexedReadPreviousAfterOpenAtEnd, Edition))
            return StoreStatus.Ok;
        bool composite = _refKey >= 0;
        bool forward = !previous || _positioner == 'O';
        StoreStatus status;
        BTree.Cursor? cursor;
        if (_positioner == 'O')
            // §14.9.27 GR14 — OPEN INPUT/I-O positions the FPI at the LOWEST record in the key of reference's
            // collating sequence, so the first READ NEXT yields it — and, at 2002/2014, so does the first READ
            // PREVIOUS, which is why this arm is reached for BOTH directions there (the gate above). The tree's first
            // entry IS the lowest under its order, whatever weight the sequence gives the pad SPACE.
            status = _st.Edge(_refKey, _orders, last: false, out cursor);
        else if (_positioner != 'R')
            status = _st.Seek(_refKey, _orders, _fpiKey, previous ? SeekRelation.LessOrEqual : SeekRelation.GreaterOrEqual,
                prefix: composite, out cursor);
        else
            status = _st.Seek(_refKey, _orders, composite ? CompositeKeyOrder.Compose(_fpiKey, _readOrdinal) : _fpiKey,
                previous ? SeekRelation.LessThan : SeekRelation.GreaterThan, prefix: false, out cursor);
        if (status != StoreStatus.Ok || cursor is null) return status;
        if ((status = Resolve(cursor, _refKey, forward, null, out found)) != StoreStatus.Ok || found is not { } f
            || !lookAhead || !composite)
            return status;
        // §14.9.30 GR27 a) — '02': the key of reference is an alternate key and the next record in the walk duplicates
        // the key just read; the ordinal order makes "next" the next entry of the same key part.
        if ((status = forward ? cursor.Next() : cursor.Previous()) != StoreStatus.Ok) return status;
        var keyOrder = OrderOf(_refKey);
        status = Resolve(cursor, _refKey, forward, part => keyOrder.Compare(part, f.KeyPart) == 0, out var adjacent);
        duplicateFollows = adjacent is not null;
        return status;
    }

    /// <inheritdoc/>
    /// <remarks>The indexed organization's §14.9.30.4 GR9 pre-read conflict target: the PRIME key of the record
    /// <see cref="SelectSequentialRecord"/> names, which is this organization's lock identity (§9.1.16). The
    /// remaining precondition beyond <c>SequentialReadReachesRetrieval</c> is a valid file position indicator
    /// (a failed START leaves none — §14.9.41.4 GR7). A torn page names no record; the READ itself then answers '30'.</remarks>
    public override string PeekSequentialRecordId(bool previous) =>
        SequentialReadReachesRetrieval && FpiValid
        && SelectSequentialRecord(previous, lookAhead: false, out var found, out _) == StoreStatus.Ok && found is { } f
            ? Identity(f.Prime)
            : "";

    /// <inheritdoc/>
    /// <remarks>§14.9.30.4 GR32's lookup, run WITHOUT assigning the key of reference — GR10 d) requires it
    /// UNCHANGED when the record operation conflict condition arises, and <see cref="ReadRandom"/> assigns
    /// <c>_refKey</c> as its first act (kb/Work PB338).</remarks>
    public override string PeekRandomReadRecordId(int keyIndex, string recordImage, RecordExtents? recordExtents)
    {
        if (ReadOpenModeGuard() is not null || OptionalAbsent) return "";
        return FindRandom(keyIndex, KeyBytes(AreaKey(recordImage, recordExtents, keyIndex)), out var found) == StoreStatus.Ok
            && found is { } f ? Identity(f.Prime) : "";
    }

    /// <summary>ISO §14.9.30.4 GR32's record identification for a random READ — the record whose key of reference equals
    /// <paramref name="value"/>, and among duplicate alternates <i>"the first record in a sequence of duplicates that was
    /// released to the operating environment"</i>: the FIRST entry of the key's tree whose key part equals the value,
    /// because the smallest ordinal sorts first. §12.4.5.6.4 GR6's SUPPRESS WHEN records have no entry. ⛔ SELECTION
    /// ONLY — it establishes no key of reference and moves no position (kb/Work PB338).</summary>
    private StoreStatus FindRandom(int keyIndex, byte[]? value, out Found? found)
    {
        found = null;
        if (value is null) return StoreStatus.Ok;
        if (keyIndex < 0)
        {
            var status = _st.Get(_orders, value, out var frame);
            if (status != StoreStatus.Ok || frame is null) return status;
            if (Decoded(frame) is not { } record) return StoreStatus.PermanentError;
            found = new Found(value, record, value, OrdinalOf(record, PrimeKey));
            return StoreStatus.Ok;
        }
        var seek = _st.Seek(keyIndex, _orders, value, SeekRelation.Equal, prefix: true, out var cursor);
        if (seek != StoreStatus.Ok || cursor is null) return seek;
        var order = OrderOf(keyIndex);
        return Resolve(cursor, keyIndex, forward: true, part => order.Compare(part, value) == 0, out found);
    }

    private string ReadSequential(out string image, bool previous)
    {
        image = BlankRecordArea();
        if (SequentialReadGuard() is { } pre) return Unsuccessful(pre);   // '47'/'46'/'10' — FileConnector
        if (!FpiValid) return Unsuccessful(FileStatusCode.NoValidNextRecord);   // '46' §9.1.13.7 6a (failed START)
        if (SelectSequentialRecord(previous, lookAhead: true, out var selected, out bool duplicate) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);              // a torn page: '30' §9.1.13.6 item 1
        if (selected is not { } found) return Unsuccessful(FileStatusCode.AtEnd);   // §14.9.30 GR24b
        _fpiKey = found.KeyPart;                                           // GR21 rule g — a KEY VALUE
        _readOrdinal = found.Ordinal;                                      // GR21 e)/f) — where in its set of duplicates
        FpiValid = true; _positioner = 'R';
        _lastReadPrime = found.Prime;
        NoteRecordRead(found.Record.Image, found.Record.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Record.Image);
        return ReadSucceeded(duplicate ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success);
    }

    /// <summary>Format-2 random READ (§14.9.30 GR30–GR32): <paramref name="keyIndex"/> becomes the key of
    /// reference (persisting for subsequent dynamic sequential reads, GR30/GR31); the key VALUE is sliced from
    /// the record area image (<paramref name="keyedRecordImage"/> — GR32, the program stored it there). Among
    /// duplicate alternates the FIRST RELEASED record is made available (GR32 — the smallest release ordinal
    /// under THAT key); no record →
    /// invalid key '23'; an absent optional file → '23' (GR28).</summary>
    public string ReadRandom(int keyIndex, string keyedRecordImage, out string image, RecordExtents? areaExtents = null)
    {
        image = BlankRecordArea();
        if (ReadOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);            // '47' §14.9.30.4 GR2
        _refKey = keyIndex;                                                // GR30/GR31
        if (RandomReadAbsentOptionalGuard() is { } absent) return Unsuccessful(absent);  // '23' §9.1.13.5 3 b)
        byte[]? value = KeyBytes(AreaKey(keyedRecordImage, areaExtents, keyIndex));
        if (FindRandom(keyIndex, value, out var selected) != StoreStatus.Ok)   // §14.9.30.4 GR32 + §12.4.5.6.4 GR6 — the ONE copy
            return Unsuccessful(FileStatusCode.PermanentError);            // a torn page: '30' §9.1.13.6 item 1
        if (selected is not { } found) return Unsuccessful(FileStatusCode.RecordNotFound);   // '23' GR32
        // GR32 sets the FPI "to the value in the key of reference" — the key VALUE the program supplied; the
        // duplicate-set position of the record made available is separate state (GR21 e)/f), kb/Work PB342).
        _fpiKey = value!; _readOrdinal = found.Ordinal; FpiValid = true; _positioner = 'R';
        _lastReadPrime = found.Prime;
        NoteRecordRead(found.Record.Image, found.Record.Extents);   // §13.18.43 GR15 — the stored frame length
        image = Fit(found.Record.Image);
        return ReadSucceeded(FileStatusCode.Success);
    }

    // ── WRITE / REWRITE / DELETE (ISO §14.9.51 / §14.9.35 / §14.9.10) ───────────────────────────────────────

    /// <summary>Whether a record other than the one whose prime key is <paramref name="self"/> (null: any record) has
    /// <paramref name="value"/> under alternate key <paramref name="keyIndex"/> — the uniqueness test of §14.9.51.4
    /// GR40/GR42 c) and §14.9.35.4 GR25 c), a seek on the key part (§12.4.5.12.4 GR1: equality under the key's
    /// collating sequence). A key the file has no tree for has no access path to test.</summary>
    private StoreStatus Duplicates(int keyIndex, byte[] value, byte[]? self, out bool exists)
    {
        exists = false;
        var status = _st.Seek(keyIndex, _orders, value, SeekRelation.Equal, prefix: true, out var cursor);
        if (status != StoreStatus.Ok || cursor is null) return status;
        var order = OrderOf(keyIndex);
        while (true)
        {
            if ((status = Resolve(cursor, keyIndex, forward: true, part => order.Compare(part, value) == 0, out var found))
                != StoreStatus.Ok || found is not { } f)
                return status;
            if (self is null || _orders.Prime.Compare(f.Prime, self) != 0) { exists = true; return StoreStatus.Ok; }
            if ((status = cursor.Next()) != StoreStatus.Ok) return status;
        }
    }

    /// <summary>Alternate key <paramref name="keyIndex"/>'s value in a record, as its tree's key part, or null when it is
    /// the key's SUPPRESS WHEN value — §12.4.5.6.4 GR6: <i>"an alternate record key access path to a particular record
    /// shall not be provided when the value of data-name-1 or record-key-name-1 in that record is equal to literal-1"</i>,
    /// so the record has no entry in the key's tree and, under that key of reference, <i>"is not considered to exist"</i>
    /// (§14.9.30.4 GR21 c)) for READ and START. The comparison is the §14.9.51 GR35
    /// relation condition under this key's collating sequence; the registered value is already the key's own image (a
    /// figurative or ALL literal repeated to the key's size, a shorter literal space-extended, a national value in national
    /// spaces — <c>DataBinder.KeySuppressionOf</c>, kb/Work PB1072), because only the compiler knows the literal's form
    /// and the key's category. The prime key is NEVER suppressible (GR6 scopes suppression to alternate keys).</summary>
    private byte[]? AlternateValue(string image, RecordExtents? extents, int keyIndex)
    {
        byte[] value = KeyBytes(KeyOf(image, extents, keyIndex))!;   // a stored or storable record has a byte image ('91')
        return _suppress[keyIndex] is { } suppress && _orders.Alternates[keyIndex].Compare(value, suppress) == 0 ? null : value;
    }

    /// <summary>A record's frame bytes under the file's ordinal slots (<see cref="IndexedStore.FrameSlots"/>), the one
    /// codec (<see cref="RecordFraming.EncodeFrame"/>).</summary>
    private byte[] FrameOf(StoredFrame record, long[] ordinals) =>
        RecordFraming.EncodeFrame(record with { Ordinals = ordinals }, _st.FrameSlots, CodeSet);

    /// <summary>WRITE (§14.9.51 GR34–GR42): sequential access requires strictly ascending prime keys ('21',
    /// GR38/GR42a); a duplicate prime key → '22' (GR36/GR42b); a duplicate no-DUPLICATES alternate → '22'
    /// (GR40/GR42c); a permitted duplicate alternate succeeds with '02' (§9.1.13.2 2c). The released record takes
    /// the next release ordinal under EVERY key at once, so sequential retrieval order is the actual write order
    /// (GR40). A record whose pages would take the store past the boundary of the physical file → '24' (GR42 d),
    /// 2.2.1.9); a medium that refuses the statement's pages → '30' (§9.1.13.6 item 1), the file as the previous statement
    /// left it. Open-mode legality per §9.1.13.7 item 8.</summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is written
    /// (determination D-FRA (v); kb/Work PB1053): stored with it, and what locates a key the record's
    /// variable-length members precede.</param>
    public string Write(string image, int length = -1, RecordExtents? extents = null)
    {
        var (originalImage, originalExtents) = (image, extents);
        if (PermanentErrorReplay() is { } stuck) return Status = stuck;   // §9.1.13.1 — a permanent error in effect (kb/Work PB1541)
        // §14.9.51.4 GR38 "If the access mode of the write file connector is sequential, records shall be
        // released … in ascending order of prime record key values" vs. GR39 "If the access mode … is random
        // or dynamic, WRITE statements may release records … in any order": the ACCESS MODE alone selects the
        // ordering rule (the open mode only seeds GR38's first key — the highest existing one under extend).
        // ⛔ The open mode is NOT a disjunct here (kb/Work PB325): a random- or dynamic-access connector open
        // in the extend mode is illegal source (§14.9.27.3 SR2) but a REACHABLE runtime state, and Table 20
        // leaves its WRITE cell blank — item 8 b) is what it must answer, not GR38's append.
        bool sequentialRelease = Access == KeyedAccess.Sequential;
        if (sequentialRelease)
        {
            if (!IsOpen || Mode is not (FileOpenMode.Output or FileOpenMode.Extend))
                return Status = FileStatusCode.WriteNotOpenForOutput;      // '48' §9.1.13.7 8a
        }
        // §9.1.13.7 8 b) — "If the access mode is dynamic or random, the file connector is not open in the
        // I-O or output mode"; Table 20's Random/Extend and Dynamic/Extend WRITE cells are blank.
        else if (!IsOpen || Mode is not (FileOpenMode.IO or FileOpenMode.Output))
            return Status = FileStatusCode.WriteNotOpenForOutput;          // '48' §9.1.13.7 8b
        // ⛔ AFTER THE OPEN MODE, BEFORE THE KEYS (DOC-A.1-104; kb/Work PB2751): GR3's '48' is stated before GR14's
        // '44', which this statement used to test first — so a short record written through a connector open INPUT
        // answered '44' where its own REWRITE and DELETE answer the open mode.
        if (RecordRulesFailure(image, length, out string stored) is { } badRecord)
            return Status = badRecord;                                     // '44' §14.9.51.4 GR14, then '91'
        var framed = Framed(stored, image, extents);
        extents = framed.Extents;
        image = _layoutKeys ? stored : Fit(image);   // key slices come from the record-area image (KeyOf pads on demand)
        byte[] prime = KeyBytes(KeyOf(image, extents, PrimeKey))!;
        if (sequentialRelease && _lastWrittenPrime is { } lastPrime && _orders.Prime.Compare(prime, lastPrime) <= 0)
            return Status = FileStatusCode.SequenceError;                  // '21' GR38/GR42a
        if (_st.Get(_orders, prime, out var present) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
        if (present is not null) return Status = FileStatusCode.DuplicateKey;   // '22' GR36/GR42b
        bool duplicateAlt = false;
        var alternates = new byte[]?[_alts.Count];
        for (int i = 0; i < _alts.Count; i++)
        {
            // §14.9.51 GR41 — a suppressed alternate key provides no access path and never duplicates.
            if ((alternates[i] = AlternateValue(image, extents, i)) is not { } value) continue;
            if (Duplicates(i, value, self: null, out bool exists) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;   // '22' GR40/GR42c
            if (exists) duplicateAlt = true;
        }
        // The transactions' HOLDINGS (2.2.1.10): the identity and every unique value this WRITE would insert, after the
        // invalid key conditions (GR42, which ignore locks) and before the record is touched.
        var holdings = new List<byte[]> { TransactionKeys.Record(prime) };
        for (int i = 0; i < _alts.Count; i++)
            if (UniqueValueHolding(i, alternates[i]) is { } held) holdings.Add(held);
        if (HoldingsGate(holdings, out bool reevaluate) is { } locked) return Status = locked;   // '51' (RETRY: GR16)
        if (reevaluate) return Write(originalImage, length, originalExtents);   // against the file a rollback restored
        if (RefusedAtCommit() is { } lockRefused) return Status = lockRefused;   // '54' / '53': the lock request is the last check (kb/Work PB2848)
        // §14.9.51.4 GR40 — the WRITE RELEASES the record, so it is positioned last in the duplicate set of
        // EVERY key at once: one fresh ordinal stamped into every slot (the prime slot doubles as the record's
        // release order in the physical file).
        var released = _st.Release(Entered, _orders, prime,
            ordinal => FrameOf(framed, ReleaseOrdinals(ordinal)), alternates, out bool done,
            TransactionFor(prime, beforeImage: null));
        if (RecordWriteFailed(released) is { } refused) return Status = refused;   // '24' §14.9.51.4 GR42 d) / '30' §9.1.13.6 1)
        if (!done) return Status = FileStatusCode.DuplicateKey;            // '22' — the tree declined (GR42 b))
        if (sequentialRelease) _lastWrittenPrime = prime;   // GR38's running "highest … written" — sequential access only
        _lastWrittenPrimeId = prime;   // §9.1.16 lock identity of the record just released (§14.9.51 GR11)
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>A freshly released record's ordinal vector: ONE ordinal in every slot the file records — a WRITE releases
    /// the record under every key at the same instant (§14.9.51.4 GR40).</summary>
    private long[] ReleaseOrdinals(ulong ordinal)
    {
        var ordinals = new long[_st.FrameSlots];
        Array.Fill(ordinals, (long)ordinal);
        return ordinals;
    }

    /// <summary>REWRITE (§14.9.35): open mode I-O ('49'); sequential access requires the previous op to be a
    /// successful READ ('43', GR5) AND the prime key to equal the last-read prime ('21', GR22/GR25a);
    /// random/dynamic an existing prime ('23', GR23/GR25b); a no-DUPLICATES alternate conflict with ANOTHER
    /// record → '22' (GR25c); a CHANGED alternate key — and ONLY a changed one, GR24 a) — repositions the record
    /// LAST in that key's duplicate set (GR24 b: it takes a fresh release ordinal under that key alone); a
    /// duplicate key value CREATED under a permitted-duplicates alternate → '02' (§9.1.13.2 2 c). A replacing record whose
    /// pages would take the store past the boundary of the physical file → '24' (§9.1.13.5 item 4); a medium that refuses
    /// them → '30'.</summary>
    /// <param name="extents">The replacing record's EXTENT TABLE (D-FRA (v); kb/Work PB1053), replaced with it.</param>
    public string Rewrite(string image, int length = -1, RecordExtents? extents = null)
    {
        var (originalImage, originalExtents) = (image, extents);
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        // §14.9.35 GR18 — an indexed record's size MAY differ from the replaced record's; GR20 still bounds it.
        // ⛔ IN THE GENERAL RULES' ORDER (DOC-A.1-104; kb/Work PB2751): GR5's '43' is stated before GR20's '44'.
        bool sequential = Access == KeyedAccess.Sequential;   // §14.9.35.4 GR22 vs. GR23 — the ACCESS MODE alone
        if (sequential && !wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;   // '43' GR5
        if (RecordRulesFailure(image, length, out string stored) is { } badRecord)
            return Status = badRecord;                                                          // '44' GR20, then '91'
        var framed = Framed(stored, image, extents);
        extents = framed.Extents;
        image = _layoutKeys ? stored : Fit(image);   // the WRITE's key image rule (kb/Work PB1025)
        byte[] prime = KeyBytes(KeyOf(image, extents, PrimeKey))!;
        // '21' §14.9.35 GR22 — the prime key of the replaced record must EQUAL that of the last record read;
        // equality is collating-sequence-based per §12.4.5.12.4 GR1 (the prime key's order), not ordinal.
        if (sequential && (_lastReadPrime is not { } lastPrime || _orders.Prime.Compare(prime, lastPrime) != 0))
            return Status = FileStatusCode.SequenceError;
        if (_st.Get(_orders, prime, out var frame) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
        if (frame is null) return Status = FileStatusCode.RecordNotFound;                       // '23' GR23
        if (Decoded(frame) is not { } target) return Status = FileStatusCode.PermanentError;
        // ⛔ VALIDATE FIRST, THEN CHANGE. An early '22' leaves the REWRITE unsuccessful, so nothing may have been
        // repositioned by then; the statement below changes every tree at once or none.
        bool duplicateAlt = false;
        var moves = new List<(int Key, byte[]? Old, byte[]? New)>();
        var holdings = new List<byte[]> { TransactionKeys.Record(prime) };
        var freed = new List<byte[]>();
        for (int i = 0; i < _alts.Count; i++)
        {
            byte[]? now = AlternateValue(image, extents, i);
            byte[]? old = AlternateValue(target.Image, target.Extents, i);
            // The ONE definition of "this REWRITE changed alternate key i" that GR24 a)/b), GR24's SUPPRESS WHEN
            // sub-rules and §9.1.13.2 2 c) all key off. §14.9.35.4 GR24's closing sentence — "The comparison used for
            // determining changes to the key is based on the collating sequence for the file according to the rules
            // for a relation condition" — makes it the key's order, not a byte compare; entering OR leaving
            // suppression is a change of its own (GR24's two SUPPRESS WHEN sub-rules both reposition).
            bool changed = (now is null) != (old is null) || now is not null && _orders.Alternates[i].Compare(now, old!) != 0;
            if (changed)
            {
                moves.Add((i, old is null ? null : CompositeKeyOrder.Compose(old, OrdinalOf(target, i)), now));
                // 2.2.1.10 (finding R2-4): the unique value this REWRITE inserts is tested against the holdings, and the
                // one it frees becomes a holding of the transaction under commit control.
                if (UniqueValueHolding(i, now) is { } inserted) holdings.Add(inserted);
                if (UniqueValueHolding(i, old) is { } released) freed.Add(released);
            }
            // §12.4.5.6.4 GR6 — a key whose value equals its SUPPRESS WHEN value provides no access path, so it is
            // skipped for the uniqueness check (it can never duplicate, §14.9.51.4 GR41 by parity with WRITE).
            if (now is null) continue;
            if (Duplicates(i, now, self: prime, out bool exists) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
            if (exists && !_alts[i].Dups) return Status = FileStatusCode.DuplicateKey;          // '22' GR25c
            // §9.1.13.2 2 c) — '02' is "the record just written CREATED a duplicate key value": a key whose
            // value and suppression state this REWRITE did not change duplicated before the statement ran too,
            // so it created nothing (kb/Work PB341).
            if (exists && changed) duplicateAlt = true;
        }
        if (HoldingsGate(holdings, out bool reevaluate) is { } locked) return Status = locked;   // '51' (2.2.1.10)
        if (reevaluate) return Rewrite(originalImage, length, originalExtents);   // against the file a rollback restored
        if (RefusedAtCommit() is { } lockRefused) return Status = lockRefused;   // '54' / '53': the lock request is the last check (kb/Work PB2848)
        // §14.9.35.4 GR24 a) — "When the value of a specific alternate record key is not changed, the order of
        // retrieval when that key is the key of reference remains unchanged" — so ONLY the keys this REWRITE
        // actually changed are re-stamped and moved; b) puts the record "last within the set of duplicate records" of
        // each of those, which one ordinal off the monotone mint does for all of them at once.
        var replaced = _st.Replace(Entered, _orders, prime, ordinal =>
        {
            long[] ordinals = target.Ordinals is { } o ? (long[])o.Clone() : new long[_st.FrameSlots];
            foreach (var (key, _, _) in moves)
                if (key + 1 < ordinals.Length) ordinals[key + 1] = (long)ordinal;   // GR24 b)
            return FrameOf(framed, ordinals);
        }, ordinals: moves.Count > 0 ? 1 : 0, moves, out bool done, TransactionFor(prime, frame, freed));
        if (RecordWriteFailed(replaced) is { } refused) return Status = refused;   // '24' (§9.1.13.5 4) / '30' (§9.1.13.6 1)
        if (!done) return Status = FileStatusCode.RecordNotFound;
        return Status = duplicateAlt ? FileStatusCode.DuplicateAlternateKey : FileStatusCode.Success;
    }

    /// <summary>DELETE RECORD (§14.9.10): open mode I-O ('49', GR1); sequential access removes the prior READ's
    /// record ('43' without one, GR2); random/dynamic removes the record whose PRIME key equals the prime key
    /// item's content (sliced from the record area image — GR3; absent → invalid key '23'). The record's n + 1 entries
    /// go in one statement; a medium that refuses its pages → '30'. The FPI is unaffected (GR9) — the position survives
    /// because it is held as values (key + release ordinal) the next READ seeks from, not as a reference to the deleted
    /// record.</summary>
    public string Delete(string keyedRecordImage, RecordExtents? areaExtents = null)
    {
        bool wasRead = PrevOpWasSuccessfulRead;   // the terminal status assignment drops the gate (PB140)
        if (MutationOpenModeGuard() is { } notIO) return Status = notIO;   // '49' §14.9.35.4 GR3 / §14.9.10.4 GR1
        byte[]? prime;
        if (Access == KeyedAccess.Sequential)   // §14.9.10.4 GR2 vs. GR3 — the ACCESS MODE alone
        {
            if (!wasRead) return Status = FileStatusCode.NoSuccessfulReadBeforeDeleteRewrite;
            prime = _lastReadPrime;
        }
        else
            prime = KeyBytes(AreaKey(keyedRecordImage, areaExtents, PrimeKey));
        if (prime is null) return Status = FileStatusCode.RecordNotFound;
        if (_st.Get(_orders, prime, out var frame) != StoreStatus.Ok) return Status = FileStatusCode.PermanentError;
        if (frame is null) return Status = FileStatusCode.RecordNotFound;
        if (AlternateEntries(frame) is not { } entries) return Status = FileStatusCode.PermanentError;
        // 2.2.1.10: the record's identity is tested against the holdings after GR3's invalid key ('23'), and under commit
        // control the DELETE holds the identity (it stays held after the record is gone) and every unique value it frees.
        if (HoldingsGate([TransactionKeys.Record(prime)], out bool reevaluate) is { } locked) return Status = locked;   // '51'
        if (reevaluate) return Delete(keyedRecordImage, areaExtents);   // against the file a rollback restored
        var freed = new List<byte[]>();
        foreach (var (key, entry) in entries)
            if (UniqueValueHolding(key, IndexedStore.Split(entry).KeyPart) is { } released) freed.Add(released);
        var removed = _st.Remove(Entered, _orders, prime, entries, out bool done, TransactionFor(prime, frame, freed));
        if (Failed(removed) is { } refused) return Status = refused;       // '30' §9.1.13.6 item 1, the boundary too
        return Status = done ? FileStatusCode.Success : FileStatusCode.RecordNotFound;
    }

    // ── START (ISO §14.9.41) ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>START KEY rel-op (GR16/GR17): the comparison takes the leftmost <paramref name="length"/> characters
    /// of each stored key (the GR17 temporary-area partial-key model — a generic/shorter operand or the 2002+ WITH LENGTH
    /// count), ONE SEEK of the key's tree under its order with the partial key as a prefix of each entry's key part
    /// (2.2.1.4). An out-of-range length → '23' (GR14). Forward search (=, &gt;, &gt;=) takes the FIRST record in (key
    /// value, release ordinal) order satisfying the comparison; reverse (&lt;, &lt;=) the LAST — the file searched in
    /// reverse order (GR17 d)). Success establishes <paramref name="keyIndex"/> as the key of reference for subsequent
    /// sequential READs (GR16); failure invalidates the FPI and leaves the key of reference undefined (GR7).</summary>
    /// <param name="keyedRecordImage">⛔ THE RECORD AREA, NOT data-name-1's own content (kb/Work PB355).
    /// §14.9.41.4 GR17 a) — "The specified key is set up by moving the relevant parts of the record area into a
    /// temporary data area" — names the record area as the SOURCE and GR16's key of reference as the key, so the
    /// first temporary area is <see cref="KeyOf"/> the area under <paramref name="keyIndex"/>, truncated to the
    /// GR17 b) length the caller passes as <paramref name="length"/>. That is the SAME extraction the
    /// random READ (§14.9.30.4 GR32) and DELETE (§14.9.10.4 GR3) make from the same image; taking data-name-1's
    /// own bytes and PADDING them to the count instead made a LENGTH that reaches past the operand search for
    /// invented spaces where the record area holds the rest of the key.</param>
    /// <param name="length">The GR17 b) length EXACTLY as arithmetic-expression-1 produced it (kb/Work PB357) —
    /// GR14's positive-nonzero-integer-within-the-key predicate is asked of it HERE, once, and never of a value an
    /// emit-time narrowing has already forced into range.</param>
    public string Start(int keyIndex, string op, string keyedRecordImage, StartKeyLength length,
        RecordExtents? areaExtents = null)
    {
        if (StartOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();                           // '23' GR5
        int keyLength = keyIndex < 0 ? _primeLen : _alts[keyIndex].Len;
        if (!length.TryCompareLength(keyLength, out int compareLength)) return StartFail();   // '23' GR14
        // GR17 a)/b) — the first temporary area. KeyOf pads the area to the key's own span, so the slice is
        // always in range once GR14 above has bounded compareLength by that span.
        if (KeyBytes(AreaKey(keyedRecordImage, areaExtents, keyIndex)[..compareLength]) is not { } value) return StartFail();
        var relation = op switch
        {
            "==" => SeekRelation.Equal,
            ">" => SeekRelation.GreaterThan,
            ">=" => SeekRelation.GreaterOrEqual,
            "<" => SeekRelation.LessThan,
            "<=" => SeekRelation.LessOrEqual,
            _ => throw new ArgumentException($"'{op}' is not a START relational operator", nameof(op)),
        };
        // §14.9.41 GR17 e) — the comparison is the file's collating sequence over operands of equal length: the key's
        // order over the leading compareLength positions of each entry's key part.
        if (_st.Seek(keyIndex, _orders, value, relation, prefix: true, out var cursor) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);           // a torn page: '30' §9.1.13.6 item 1
        if (cursor is null) return StartFail();                           // '23' — no tree, no record satisfies it
        bool forward = relation is SeekRelation.Equal or SeekRelation.GreaterThan or SeekRelation.GreaterOrEqual;
        var order = OrderOf(keyIndex);
        Func<byte[], bool>? satisfies = relation == SeekRelation.Equal ? part => order.ComparePrefix(part, value) == 0 : null;
        if (Resolve(cursor, keyIndex, forward, satisfies, out var found) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);
        if (found is not { } f) return StartFail();                       // '23' — comparison not satisfied
        // GR16 — "The key specified in the KEY phrase … becomes the key of reference", and GR17 e) 1. sets the
        // file position indicator to ITS value in the record the comparison stopped on.
        return StartSucceeded(keyIndex, f.KeyPart);
    }

    /// <summary>START FIRST/LAST (COBOL-2002+) on an indexed file: §14.9.41.4 GR18 — "If FIRST is specified, the
    /// file position indicator is set to the value of the primary key of the first existing logical record in the
    /// physical file and the key of reference is set to the primary key" — and GR19, the same for LAST and the
    /// last existing record. An empty or absent-optional file → invalid key '23' (GR18/GR19's own second
    /// sentence).</summary>
    /// <remarks>⛔ THE PRIME KEY IS BOTH THE ORDERING AND THE OUTCOME (kb/Work PB356). GR18/GR19 name the primary
    /// key twice over: the record selected is the first/last one under the PRIME ordering — the prime tree's first or
    /// last entry — and the key of reference the statement leaves behind for every subsequent sequential READ (GR16's
    /// last sentence, §14.9.30.4 GR21 b)'s "otherwise") is the prime key too. Walking the INHERITED key of reference
    /// instead — which this method used to do, justified by §14.9.27.4 GR14, a real clause about what OPEN establishes
    /// and not about what START FIRST/LAST does — answered LAST with the alternate ordering's last record and left
    /// every following READ NEXT walking that alternate; and where the alternate carries SUPPRESS WHEN (§12.4.5.6.4
    /// GR6) it additionally hid records the primary-key view must include.</remarks>
    public string StartFirstLast(bool last)
    {
        if (StartOpenModeGuard() is { } notOpen) return Unsuccessful(notOpen);   // '47' §14.9.41.4 GR1 + GR7
        if (OptionalAbsent) return StartFail();
        // GR18/GR19 — "in the physical file": the PRIME tree, not the inherited key of reference's
        if (_st.Edge(PrimeKey, _orders, last, out var cursor) != StoreStatus.Ok)
            return Unsuccessful(FileStatusCode.PermanentError);           // a torn page: '30' §9.1.13.6 item 1
        return cursor is { HasCurrent: true } ? StartSucceeded(PrimeKey, cursor.Key) : StartFail();
    }

    /// <summary>The epilogue of EVERY successful START on an indexed file (§14.9.41.4): the format's own
    /// key-of-reference decision, then the file position indicator set to THAT key's value in the selected
    /// record.</summary>
    /// <remarks>⛔ <paramref name="keyOfReference"/> IS A REQUIRED ARGUMENT so that no START format can leave the
    /// previous one standing (kb/Work PB356). Each format decides it and the standard says so separately — GR16
    /// for the KEY phrase, GR18/GR19's "the key of reference is set to the primary key" for FIRST/LAST — so an
    /// epilogue that read <c>_refKey</c> instead would silently inherit whatever the last START or random READ
    /// established. The file position indicator is a KEY VALUE and nothing else (§14.9.41.4 GR17 e) 1.,
    /// kb/Work PB342): which record of a duplicate set the search stopped on is NOT recorded, so the following
    /// READ enters the set at the end §14.9.30.4 GR26 names for ITS direction — which is why
    /// <c>_readOrdinal</c> is deliberately untouched here and <c>_positioner</c> becomes 'S'.</remarks>
    private string StartSucceeded(int keyOfReference, byte[] keyValue)
    {
        _refKey = keyOfReference;
        _fpiKey = keyValue;
        FpiValid = true; _positioner = 'S';
        return StartPositioned();
    }

    // ── Internals ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The key value at <paramref name="keyIndex"/> (−1 = prime) — the concatenation of the key's
    /// <see cref="KeySegment"/> slices of the record's character image, in order (§12.4.5.12.4 GR2; a data-name-1
    /// key is one slice — the key IS its position range in the record). A key that
    /// follows a variable-length member is found in THIS record through its record type's layout (kb/Work PB1025;
    /// docs/CONFORMANCE.md §3 D-KWV): the image is contiguous (§8.5.1.11.2), so its position moves with the
    /// preceding members' current lengths — read from the record's own EXTENT TABLE when it carries one that
    /// describes it (<paramref name="extents"/>; D-FRA (v), kb/Work PB1053), by the take step otherwise, exactly as
    /// a READ of the record decomposes it.</summary>
    private string KeyOf(string image, RecordExtents? extents, int keyIndex)
    {
        var (segments, len) = keyIndex < 0 ? (_prime, _primeLen) : (_alts[keyIndex].Segments, _alts[keyIndex].Len);
        if (segments.Length == 1) return WindowOf(image, extents, segments[0]);
        // §12.4.5.12.4 GR2 / §12.4.5.6.4 GR2 — "the concatenation of all occurrences of data-name-2 in the order
        // specified": each window is taken by the same slice a one-window key takes, so a segment a
        // variable-length member precedes is located, and a short record padded, exactly as a whole key is.
        var key = new System.Text.StringBuilder(len);
        foreach (var segment in segments) key.Append(WindowOf(image, extents, segment));
        return key.ToString();
    }

    /// <summary>One <see cref="KeySegment"/> of the key — an (offset, length) slice of the record's character
    /// image.</summary>
    private string WindowOf(string image, RecordExtents? extents, KeySegment segment)
    {
        var (off, len) = (segment.Offset, segment.Length);
        if (segment.Layout is { } layout) off = layout.Position(image, off, extents);
        // A record shorter than the key's span (a RECORD VARYING record, or an area image taken as composed) is
        // extended with the RECORD AREA's own space — §14.9.30.4 GR15: a trailing space "is defined to be the
        // national space character" for a national record area — through the ONE pad, FitRecord. A byte-level
        // PadRight(' ') here manufactured U+2020 positions in a national key window, which no stored key equals
        // (kb/Work PB679). The two defensive pads that note named were this one site after PB325 rewrote the
        // keyed connectors' base; the START operand and the stored key both come through here, so they cannot
        // be padded differently.
        if (image.Length < off + len) image = FitRecord(image, off + len, NationalRecordArea);
        return image.Substring(off, len);
    }

    /// <summary>The key value in the RECORD AREA image a statement supplies (§14.9.30.4 GR32, §14.9.10.4 GR3,
    /// §14.9.41.4 GR17 a)): fitted to the area width, as the record area is — except on a connector whose key
    /// positions vary with the record, where the image is taken as composed, because truncating it would move
    /// the key (<see cref="_layoutKeys"/>).</summary>
    private string AreaKey(string areaImage, RecordExtents? areaExtents, int keyIndex)
    {
        // A file of FIXED-length records keys the record in its fixed form (D-FRA (vi)) — the shape it is stored in.
        var (image, extents) = FixedForm(areaImage, areaExtents);
        return KeyOf(_layoutKeys ? image : Fit(image), _layoutKeys ? extents : null, keyIndex);
    }
}
