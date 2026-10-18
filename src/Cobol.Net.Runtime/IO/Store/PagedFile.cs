// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

/// <summary>What a page-layer operation answered: success, or §9.1.13.6 item 1's '30' (<i>"A permanent error exists and
/// no further information is available concerning the input-output operation"</i>).</summary>
internal enum StoreStatus
{
    /// <summary>The operation completed.</summary>
    Ok,

    /// <summary>The medium refused a write, a read or a sync, a page read through a stamped pointer was torn, or the
    /// store is damaged beyond repair (both committed slots): '30'.</summary>
    PermanentError,

    /// <summary>A changing statement would take the store past its BOUNDARY (2.2.1.9): its reservation would grow the
    /// file past the format's 2^32 pages, or the host refused one of its writes because the file cannot grow further
    /// (<see cref="MediumSizeLimitException"/>). Nothing the statement did is reachable (R1) and the file is unchanged.
    /// The page layer does not know the statement; the connector answers by it: a WRITE or REWRITE as §9.1.13.5 item 4's
    /// '24' with the invalid key condition (§9.1.14), a DELETE as §9.1.13.6 item 1's '30' with NO invalid key condition
    /// (item 4 is an attempt to write; §14.9.10.4 GR3/GR4 give a DELETE that condition only for a record the file does
    /// not contain). Only
    /// <see cref="PagedFile.Begin"/> and a statement's completion answer it: an OPEN, a CLOSE and a promotion have no
    /// '24' and answer <see cref="PermanentError"/>.</summary>
    Boundary,
}

/// <summary>How the adoption rule classified the WORK slot at OPEN (2.2.1.6 R6 step 2).</summary>
internal enum WorkSlotClass
{
    /// <summary>W validates and its base is the committed generation: the last completed statement of the history.</summary>
    Descendant,

    /// <summary>W validates, its base is below the committed generation and its generation equals it: the promotion
    /// landed and its retirement did not.</summary>
    PromotionSource,

    /// <summary>Anything else: torn, superseded by a fence, or damaged.</summary>
    Foreign,
}

/// <summary>What the OPEN's adoption rule decided (2.2.1.6 R6): the work slot's class, whether the work slot was
/// adopted (a verified DESCENDANT) or the committed meta, and whether the OPEN fenced.</summary>
internal readonly record struct Adoption(WorkSlotClass WorkSlot, bool WorkAdopted, bool Fenced);

/// <summary>The page I/O a store performed (2.2.1.11): pages read, pages written, syncs. Cumulative; a witness takes
/// the difference across a statement.</summary>
internal readonly record struct IoCounters(long PagesRead, long PagesWritten, long Syncs);

/// <summary>⛔ THE PAGE LAYER OF THE KEYED STORE ENGINE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4-2.2.1.7,
/// kb/Work PB3331): pages over an <see cref="IPageMedium"/>, the three meta slots, stamped pointers, the generation
/// ledger, the reservation, the CLAIM, promotion into the alternate committed slot with its retirement, the restart of a
/// promotion from its PROMOTING flag, and the OPEN's adoption rule with its fence. Every rule of 2.2.1.6 (R1-R6) is
/// enforced here and nowhere else; the trees of kb/Work PB3332 and the connectors of PB3333/PB3334 are its callers.
/// <para>A store is one run unit's view of one physical file. Two run units are two stores over one medium; the caller
/// serializes them with the store mutex (2.2.1.7) — a COHERENT store (every sharing mode but NO OTHER) runs every
/// statement and its OPEN inside it, and reads page 2 at each statement's entry.</para>
/// <para>⛔ EVERY GENERATION IS CLAIMED BEFORE ANYTHING CARRIES IT (2.2.1.6 R3's corollary): the store keeps the highest
/// generation it has claimed or seen (<c>_stampMint</c>, with the two other mints), the next claim is one above it and
/// above page 2's generation, and a coherent store writes the claim to page 2 before its first page write. A promotion
/// promotes the work meta AT A FRESH CLAIM P — the fence at P = G + 4 097 — so every claim of the history a base P begins
/// lies in (P, P + <see cref="GenerationCeiling"/>] (R2), whatever was refused or died before it.</para></summary>
internal sealed class PagedFile
{
    /// <summary>R2's GENERATION CEILING, a format constant: every claim of a history whose base is B lies in
    /// (B, B + <see cref="GenerationCeiling"/>]; a statement whose claim would pass it promotes first.</summary>
    public const ulong GenerationCeiling = 4096;

    /// <summary>R5's ledger checkpoint: a ledger past this many pages promotes at the next statement.</summary>
    public const int LedgerCheckpoint = 1024;

    /// <summary>The format's capacity: 32-bit page numbers address 2^32 pages of 4 096 bytes (2.2.1.3 decision 6,
    /// 2.2.1.9). A statement whose reservation would grow the file past it answers <see cref="StoreStatus.Boundary"/>.</summary>
    public const ulong FormatPages = 1ul << 32;

    private readonly IPageMedium _medium;
    private readonly bool _writable;
    private readonly bool _coherent;

    private MetaPage _meta;
    private Ledger _ledger;

    /// <summary>Page 2's generation as this store last saw or wrote it (2.2.1.7).</summary>
    private ulong _lastSeen;

    /// <summary>The highest generation claimed or seen — the own last claim and page 2's <c>stamp mint</c> alike.</summary>
    private ulong _stampMint;

    private ulong _releaseMint;
    private ulong _transactionMint;

    /// <summary>This store's own promotion was refused part-way, so page 2 may carry PROMOTING (a NO OTHER store reads
    /// page 2 at its next entry only for this).</summary>
    private bool _promotionPending;

    private PageStatement? _active;
    private readonly PageCache _cache;

    /// <summary>This store completed a statement since its last promotion (its OPEN's fence included): a CLOSE owes a
    /// promotion only then.</summary>
    private bool _changedSincePromotion;
    private long _reads, _writes, _syncs;
    private int _cacheEpoch;
    private Adoption _adoption;

    private PagedFile(IPageMedium medium, bool writable, bool coherent, MetaPage meta, int cachePages)
    {
        _cache = new PageCache(cachePages);
        _medium = medium;
        _writable = writable;
        _coherent = coherent;
        _meta = meta;
        _ledger = new Ledger(this);
    }

    /// <summary>The meta the store holds: the adopted meta, or page 2 from the moment its generation changes.</summary>
    public MetaPage Meta => _meta;

    /// <summary>The ledger the store holds.</summary>
    public IReadOnlyDictionary<uint, LedgerEntry> LedgerEntries => _ledger.Entries;

    /// <summary>Every page of the FREE tree the store's meta names, read by a walk of the tree (an audit; no statement
    /// reads more of the FREE tree than the batch its reservation takes, R2).</summary>
    public IReadOnlyCollection<uint> FreePages
    {
        get
        {
            var pages = new SortedSet<uint>();
            var free = new BTree(this, KeyOrders.Ordinal, _meta.FreeRoot);
            if (free.First(out var cursor) != StoreStatus.Ok) throw new IOException("the FREE tree reads torn");
            while (cursor.HasCurrent)
            {
                pages.Add(KeyOrders.PageOf(cursor.Key));
                if (cursor.Next() != StoreStatus.Ok) throw new IOException("the FREE tree reads torn");
            }
            return pages;
        }
    }

    /// <summary>The highest generation this store has claimed (or seen claimed).</summary>
    public ulong LastClaim => _stampMint;

    /// <summary>The release ordinal (§14.9.51.4 GR40) the next statement that mints one takes: the higher of the meta's
    /// <c>release mint</c> and what this store has claimed (<see cref="Begin"/> advances it by the statement's
    /// <c>ordinals</c>, before its first page write, and it never decreases: an ordinal a refused or dead statement drew
    /// is never issued again, 2.2.1.3 decision 4).</summary>
    public ulong ReleaseMint => Math.Max(_releaseMint, _meta.ReleaseMint);

    /// <summary>The page I/O performed so far.</summary>
    public IoCounters Counters => new(_reads, _writes, _syncs);

    /// <summary>One more each time the store adopted page 2 from another run unit and dropped its page cache.</summary>
    public int CacheEpoch => _cacheEpoch;

    /// <summary>What the OPEN's adoption rule decided.</summary>
    public Adoption Adoption => _adoption;

    // ── Creation and OPEN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>OPEN OUTPUT</c>'s creation (§14.9.27.4 GR18, <i>"After the creation of the file, the file contains no
    /// records"</i>), a promotion of the EMPTY store with ONE durable write that decides it (2.2.1.16's creation rows):
    /// the three meta pages are read (kept to undo a refusal) and the empty store's generation N is placed above every
    /// stamp the previous store can have issued (the fence's bound, R2); the empty meta at N is written into the slot that
    /// does NOT hold the previous store's committed meta and SYNCED — the commit point — then page 2, the other slot, the
    /// one TRUNCATION to three pages, and a sync. A death before the commit write was handed over leaves the previous file;
    /// after it, the empty store. A REFUSAL is §14.9.27.4 GR25's unsuccessful OPEN, <i>"the file is not affected"</i>: the
    /// pages written so far get their previous bytes back and the file its previous length IN BYTES — a previous file need
    /// not be a whole number of pages (a format-5 whole store, any foreign file) — then one more sync; only a refused final
    /// sync — after the truncation, nothing left to undo — leaves the created store, the medium's state unknown ('30'
    /// either way).</summary>
    public static StoreStatus Create(IPageMedium medium, FixedFileAttributes attributes, bool coherent, out PagedFile? store,
        int cachePages = PageCache.DefaultPages)
    {
        if (!MetaPage.Fits(attributes))
            throw new ArgumentException("the key descriptors exceed the meta page; the OPEN refuses the declaration first", nameof(attributes));
        store = null;
        var creator = new PagedFile(medium, writable: true, coherent, MetaPage.Empty(attributes), cachePages);
        long length;
        try { length = medium.Length; }
        catch (Exception e) when (IsRefusal(e)) { return StoreStatus.PermanentError; }
        var before = new byte[MetaPage.MetaPages][];
        var previous = new MetaPage?[MetaPage.MetaPages];
        for (uint p = 0; p < MetaPage.MetaPages; p++)
        {
            before[p] = new byte[PageHeader.PageSize];
            if (!creator.TryReadRaw(p, before[p])) return StoreStatus.PermanentError;
            previous[p] = MetaPage.Decode(before[p], p);
        }
        ulong n = previous.OfType<MetaPage>()
            .Select(m => Math.Max(m.Generation + GenerationCeiling + 1, m.StampMint + 1)).DefaultIfEmpty(0ul).Max();
        var meta = MetaPage.Empty(attributes) with { Generation = n, CommittedGeneration = n, StampMint = n };
        uint target = MetaPage.Committed(previous[MetaPage.SlotA], previous[MetaPage.SlotB]) is { Slot: MetaPage.SlotA }
            ? MetaPage.SlotB : MetaPage.SlotA;
        var written = new List<uint>((int)MetaPage.MetaPages);
        foreach (uint p in new[] { target, MetaPage.WorkSlot, target == MetaPage.SlotA ? MetaPage.SlotB : MetaPage.SlotA })
        {
            if (!creator.TryWrite(p, meta.Encode(p))) return creator.UndoCreation(written, before, length);
            written.Add(p);
            if (p == target && !creator.TrySync()) return creator.UndoCreation(written, before, length);
        }
        try { medium.SetLength(MetaPage.MetaPages * (long)PageHeader.PageSize); }
        catch (Exception e) when (IsRefusal(e)) { return creator.UndoCreation(written, before, length); }
        if (!creator.TrySync()) return StoreStatus.PermanentError;
        creator._meta = meta;
        creator._lastSeen = n;
        creator.SeeMints(meta);
        creator._adoption = new Adoption(WorkSlotClass.Descendant, WorkAdopted: true, Fenced: false);
        store = creator;
        return StoreStatus.Ok;
    }

    /// <summary>A refused creation (§14.9.27.4 GR25, <i>"the file is not affected"</i>): every meta page it wrote that held
    /// previous bytes gets them back, newest first — a page the previous file held only PART of too, its tail read as
    /// zeros and cut off again — then a file shorter than three pages its previous <paramref name="length"/> in BYTES, and
    /// one sync; '30' whatever the medium then does.</summary>
    private StoreStatus UndoCreation(List<uint> written, byte[][] before, long length)
    {
        if (written.Count == 0) return StoreStatus.PermanentError;
        for (int i = written.Count - 1; i >= 0; i--)
            if ((long)written[i] * PageHeader.PageSize < length && !TryWrite(written[i], before[written[i]]))
                return StoreStatus.PermanentError;
        if (length < MetaPage.MetaPages * (long)PageHeader.PageSize)
        {
            try { _medium.SetLength(length); }
            catch (Exception e) when (IsRefusal(e)) { return StoreStatus.PermanentError; }
        }
        TrySync();
        return StoreStatus.PermanentError;
    }

    /// <summary>OPEN an existing store: 2.2.1.6 R6's ADOPTION RULE, run by the caller under the store mutex, and through
    /// a writable handle its FENCE. Pages 0, 1 and 2 are read and validated; the COMMITTED meta C is the valid committed
    /// slot with the higher generation G ('30' when neither validates, nothing written); the WORK slot is a DESCENDANT
    /// (base G), the PROMOTION SOURCE (base below G, generation G) or FOREIGN; a DESCENDANT whose every <c>fresh</c> page
    /// verifies is adopted, anything else gives C. A writable store then promotes the adopted meta at
    /// F = G + 4 097 (above every stamp a history based on G can have issued, R2) with its retirement, which also reclaims
    /// a rejected DESCENDANT's readable ledger; an INPUT store adopts in memory and writes nothing.</summary>
    public static StoreStatus Open(IPageMedium medium, bool writable, bool coherent, out PagedFile? store,
        int cachePages = PageCache.DefaultPages)
    {
        store = null;
        var pages = new byte[MetaPage.MetaPages][];
        for (uint p = 0; p < MetaPage.MetaPages; p++)
        {
            pages[p] = new byte[PageHeader.PageSize];
            try { medium.Read(p, pages[p]); }
            catch (Exception e) when (IsRefusal(e)) { return StoreStatus.PermanentError; }
        }
        var work = MetaPage.Decode(pages[MetaPage.WorkSlot], MetaPage.WorkSlot);
        if (MetaPage.Committed(MetaPage.Decode(pages[MetaPage.SlotA], MetaPage.SlotA),
                MetaPage.Decode(pages[MetaPage.SlotB], MetaPage.SlotB)) is not { Meta: var committed })
            return StoreStatus.PermanentError;
        var opened = new PagedFile(medium, writable, coherent, committed, cachePages) { _reads = MetaPage.MetaPages };
        ulong g = committed.Generation;
        var workClass = work is null || work.Attributes.Conflicts(committed.Attributes) ? WorkSlotClass.Foreign
            : work.CommittedGeneration == g ? WorkSlotClass.Descendant
            : work.CommittedGeneration < g && work.Generation == g ? WorkSlotClass.PromotionSource
            : WorkSlotClass.Foreign;

        var adopted = committed;
        Ledger? adoptedLedger = null;
        Rejected? rejected = null;
        if (workClass == WorkSlotClass.Descendant)
        {
            // A REFUSED read is no verdict on the work slot (R6 step 3 rejects what fails to VERIFY): '30', nothing written.
            var load = LoadOf(Ledger.Load(opened, work!.LedgerRoot, out var ledger));
            if (load == Load.Loaded) load = opened.FreshPagesVerify(ledger!);
            if (load == Load.Refused) return StoreStatus.PermanentError;
            if (load == Load.Loaded) (adopted, adoptedLedger) = (work, ledger);
            else rejected = new Rejected(ledger);
        }
        if (adoptedLedger is null && Ledger.Load(opened, adopted.LedgerRoot, out adoptedLedger) != PageCheck.Whole)
            return StoreStatus.PermanentError;
        opened.SetState(adopted, adoptedLedger!);
        opened._lastSeen = work?.Generation ?? 0;
        opened.SeeMints(committed);
        if (work is not null) opened.SeeMints(work);
        bool workAdopted = ReferenceEquals(adopted, work);
        if (writable)
        {
            ulong fence = Math.Max(g + GenerationCeiling + 1, opened._stampMint + 1);
            ulong committing = workAdopted ? 0 : committed.Committing;
            if (opened.PromoteAt(adopted, committing, fence, rejected) != StoreStatus.Ok) return StoreStatus.PermanentError;
        }
        opened._adoption = new Adoption(workClass, workAdopted, Fenced: writable);
        store = opened;
        return StoreStatus.Ok;
    }

    /// <summary>A DESCENDANT the adoption rule rejected: its ledger when readable (its <c>fresh</c> and <c>released</c>
    /// pages and its own pages were never committed, and the fence's retirement returns them to the FREE tree).</summary>
    private sealed record Rejected(Ledger? Ledger);

    // ── The statement's entry ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>A statement's COHERENT ENTRY (2.2.1.7), the whole of a READ's: page 2 is read — ONE page — and compared
    /// with what the store last saw there. A changed <c>generation</c> is another run unit's completed statement,
    /// retirement or fence: the store ADOPTS page 2 (its roots, its ledger) and drops its page cache. A writer also takes
    /// page 2's mints (a <c>stamp mint</c> above the generation is a dead or refused attempt: the next claim goes above
    /// it, nothing is dropped) and resolves a PROMOTING flag first (R5's restart rule). A NO OTHER store has nobody to
    /// adopt from and reads page 2 only to resolve its own refused promotion.
    /// <para>⛔ ONLY AN EXCLUSIVE ENTRY WRITES. <paramref name="exclusive"/> is whether the caller's hold on the store mutex
    /// excludes every other run unit (or none can exist). A writable store entered by its run unit's INPUT connector — the
    /// store a writable sibling opened, entered under that reader's SHARED hold, because a read-only handle cannot take an
    /// exclusive lock (2.2.1.7) — adopts page 2 as a writer does (the ledger and the mints are reads) but leaves a
    /// PROMOTING flag to the next exclusive entry or the CLOSE (<see cref="_promotionPending"/>): two run units' readers
    /// each resolving it under shared holds would claim from the same mints and write the same slot and page 2
    /// interleaved, breaking R1. The tree page 2 names is a completed statement's either way, so the reader reads it.</para></summary>
    public StoreStatus Enter(bool exclusive = true)
    {
        if (!_coherent && !_promotionPending) return StoreStatus.Ok;
        var page = new byte[PageHeader.PageSize];
        if (!TryReadRaw(MetaPage.WorkSlot, page)) return StoreStatus.PermanentError;
        var work = MetaPage.Decode(page, MetaPage.WorkSlot);
        // A torn page 2 under a live store: a READER's entry — a read-only store's, or the shared entry of an INPUT
        // connector into the writable store its sibling opened — keeps the tree it holds (nothing it can follow moved; the
        // statement writes nothing), so its READ answers as any other run unit's reader would; an exclusive entry of a
        // writable store cannot know what another run unit completed before it writes, so it answers '30' (§9.1.13.6
        // item 1; 2.2.1.7).
        if (work is null) return _writable && exclusive ? StoreStatus.PermanentError : StoreStatus.Ok;
        if (work.Generation != _lastSeen)
        {
            // A reader adopts the roots; only a writer allocates, so only a writer reads the ledger (2.2.1.7).
            if (!_writable) _meta = work;
            else if (Ledger.Load(this, work.LedgerRoot, out var ledger) != PageCheck.Whole) return StoreStatus.PermanentError;
            else SetState(work, ledger!);
            _lastSeen = work.Generation;
            DropCache();
        }
        if (!_writable) return StoreStatus.Ok;
        SeeMints(work);
        if (!work.Promoting)
        {
            _promotionPending = false;
            return StoreStatus.Ok;
        }
        if (!exclusive)
        {
            _promotionPending = true;
            return StoreStatus.Ok;
        }
        return ResolvePromotion(work);
    }

    /// <summary>R5's RESTART RULE, for a PROMOTING flag left by a dead or a refused promoter: both slots are read. A slot
    /// naming page 2's generation means the promotion LANDED — its syncs run again and its retirement runs, claiming above
    /// the dead one's claim (or, were that claim to pass the ceiling, the promotion runs again); no slot names it and
    /// <c>committing</c> is 0 — the promotion runs again from its claim; no slot names it and <c>committing</c> is a
    /// transaction — a COMMIT that did not land, UNWOUND: page 2 rewritten with the flag clear and <c>committing</c> 0,
    /// nothing else written (the transaction stands as an ordinary active one, kb/Work PB3337).</summary>
    private StoreStatus ResolvePromotion(MetaPage work)
    {
        if (!TryReadSlots(out var a, out var b)) return StoreStatus.PermanentError;
        bool landed = a?.Generation == work.Generation || b?.Generation == work.Generation;
        if (landed)
        {
            if (!TrySync()) return StoreStatus.PermanentError;
            if (NextClaim() > work.Generation + GenerationCeiling)
                return PromoteAt(work with { Promoting = false }, work.Committing, Claim(), null);
            ulong claim = Claim();
            var reclaimed = WithMints(work);
            if (!TryWrite(MetaPage.WorkSlot, reclaimed.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
            return Retire(work, claim, null);
        }
        if (work.Committing == 0) return PromoteAt(work with { Promoting = false }, 0, Claim(), null);
        var unwound = WithMints(work with { Promoting = false, Committing = 0 });
        if (!TryWrite(MetaPage.WorkSlot, unwound.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
        _meta = unwound;
        _promotionPending = false;
        return StoreStatus.Ok;
    }

    // ── A changing statement ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Begin a statement that CHANGES the file, of <paramref name="bound"/>: the tree pages its edits will flush
    /// and the pages those edits stopped referencing (<see cref="StatementBound.Of"/> reads both from the trees), minting
    /// <paramref name="ordinals"/> release ordinals and <paramref name="transactionIds"/> transaction ids. Its entry
    /// (<see cref="Enter"/>, unless the caller already ran it for this statement — <paramref name="entered"/> — to read the
    /// trees its edits change); the ceiling and the ledger checkpoint (a promotion first, R2 and R5: it moves no data
    /// tree's root); its RESERVATION — the ledger's <c>released</c> pages, then one batch of the lowest FREE-tree pages,
    /// then growth, sized to the statement's pages plus the FREE-tree and LEDGER pages it rewrites (the ledger's own
    /// sized by every page the statement writes, releases or leaves over), so nothing is allocated once the ledger is
    /// being written (R2); and its CLAIM — in memory, and for a coherent store on page 2 with the roots, base and
    /// generation unchanged, before its first page write (2.2.1.7). The reservation is taken BEFORE the claim: one that
    /// would grow the file past the format's 2^32 pages is the BOUNDARY (2.2.1.9), and a torn FREE-tree page '30', both
    /// answered with nothing claimed or written.</summary>
    public StoreStatus Begin(StatementBound bound, out PageStatement? statement, int ordinals = 0, int transactionIds = 0, bool entered = false)
    {
        statement = null;
        if (!_writable) throw new InvalidOperationException("an INPUT store changes nothing");
        if (_active is not null) throw new InvalidOperationException("one statement at a time");
        if (!entered && Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        if ((NextClaim() > _meta.CommittedGeneration + GenerationCeiling || _ledger.Count > LedgerCheckpoint)
            && PromoteAt(_meta, 0, Claim(), null) != StoreStatus.Ok)
            return StoreStatus.PermanentError;
        var ledger = _ledger.Clone();
        var free = new BTree(this, KeyOrders.Ordinal, _meta.FreeRoot);
        if (Reserve(bound, ledger, free, _meta.PageCount, out var reservation) is var reserved && reserved != StoreStatus.Ok)
            return reserved;
        ulong claim = Claim();
        _releaseMint = Math.Max(_releaseMint, _meta.ReleaseMint) + (ulong)ordinals;
        _transactionMint = Math.Max(_transactionMint, _meta.TransactionMint) + (ulong)transactionIds;
        if (_coherent && !TryWrite(MetaPage.WorkSlot, WithMints(_meta).Encode(MetaPage.WorkSlot)))
            return _lastRefusal;
        _active = statement = new PageStatement(this, claim, ledger, free, reservation, _meta);
        return StoreStatus.Ok;
    }

    /// <summary>Complete <paramref name="s"/>: the FREE tree and then the LEDGER are written from the reservation, and
    /// page 2 LAST, with the claim as its generation (2.2.1.6 R1-R3). A refusal anywhere leaves page 2 naming the previous
    /// statement's tree, untouched (R1), and the store's memory as it was but for the claim, which stays raised (finding
    /// R4-3).</summary>
    internal StoreStatus Complete(PageStatement s)
    {
        End(s);
        if (s.Refused) return s.Refusal;
        if (!WriteBookkeeping(s, out var freeRoot, out var ledgerRoot)) return s.Refused ? s.Refusal : StoreStatus.PermanentError;
        var completed = WithMints(s.Meta with
        {
            Generation = s.Generation,
            FreeRoot = freeRoot,
            LedgerRoot = ledgerRoot,
            PageCount = Math.Max(s.Meta.PageCount, s.Reservation.GrownTo),
            Promoting = false,
        });
        if (!TryWrite(MetaPage.WorkSlot, completed.Encode(MetaPage.WorkSlot))) return _lastRefusal;
        SetState(completed, s.Ledger);
        _lastSeen = completed.Generation;
        _changedSincePromotion = true;
        return StoreStatus.Ok;
    }

    /// <summary>End <paramref name="s"/> without completing it.</summary>
    internal void End(PageStatement s)
    {
        if (!ReferenceEquals(_active, s)) throw new InvalidOperationException("not the active statement");
        _active = null;
    }

    /// <summary>Write one page of a statement: <see cref="StoreStatus.Ok"/>, or the refusal the medium answered.</summary>
    internal StoreStatus WritePage(uint page, byte[] bytes) => TryWrite(page, bytes) ? StoreStatus.Ok : _lastRefusal;

    // ── Reads ───────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Read a page through a stamped pointer (2.2.1.6 R3): <see cref="PageCheck.Whole"/> only when its checksum
    /// holds, its <c>self</c> is the page read and its generation is the stamp; a statement answers anything else as
    /// '30'.
    /// <para>A page the cache holds under the pointer's own <c>self</c> and generation is served from it (verified when
    /// it entered); anything else is read from the medium, and a page that checks whole enters the cache.</para></summary>
    public PageCheck Read(PagePointer pointer, Span<byte> page)
    {
        if (pointer.IsNone) throw new ArgumentException("no page", nameof(pointer));
        if (_cache.Get(pointer.Page) is { } cached && PageHeader.Decode(cached) is var head
            && head.Self == pointer.Page && head.Generation == pointer.Generation)
        {
            cached.CopyTo(page);
            return PageCheck.Whole;
        }
        if (!TryReadRaw(pointer.Page, page)) return PageCheck.Unreadable;
        var check = PageHeader.Check(page, pointer.Page, pointer.Generation);
        if (check == PageCheck.Whole) _cache.Put(pointer.Page, page[..PageHeader.PageSize].ToArray());
        return check;
    }

    /// <summary>Forget every cached page (2.2.1.7's drop when page 2 is adopted from another run unit; the cold-cache
    /// seam of 2.2.1.11's witnesses).</summary>
    public void DropCache()
    {
        _cache.Clear();
        _cacheEpoch++;
    }

    /// <summary>Raise the page cache's capacity to at least <paramref name="pages"/> — the input-output areas of a
    /// connector attached with a RESERVE clause (§12.4.5.14.3 GR1, 2.2.1.13).</summary>
    public void ReserveCache(int pages) => _cache.Reserve(pages);

    /// <summary>The pages the cache holds at most.</summary>
    public int CacheCapacity => _cache.Capacity;

    /// <summary>Whether the store was opened through a writable handle (it fenced, and it may change the file).</summary>
    public bool Writable => _writable;

    // ── Promotion ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A CHECKPOINT promotion (2.2.1.6 R5), after the statement's entry.</summary>
    public StoreStatus Promote()
    {
        if (!_writable) throw new InvalidOperationException("an INPUT store promotes nothing");
        if (Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        return PromoteAt(_meta, 0, Claim(), null);
    }

    /// <summary>CLOSE: through a writable handle a promotion, then one more sync, so a closed file's OPEN verifies the
    /// retirement's few pages and nothing else (R5); through an INPUT handle nothing. A store that completed no statement
    /// since its last promotion (its OPEN's fence, or an earlier CLOSE of the run unit's) owes none: page 2 is already
    /// the retirement of that promotion, and the sync alone makes it durable — so a connector that changed nothing never
    /// fails its CLOSE over a page 2 another run unit left torn (kb/Work PB3333; §9.1.13.1's permanent error of the
    /// statement that met it is that statement's, DOC-A.1-105).</summary>
    public StoreStatus Close()
    {
        if (!_writable) return StoreStatus.Ok;
        if ((_changedSincePromotion || _promotionPending) && Promote() != StoreStatus.Ok) return StoreStatus.PermanentError;
        return TrySync() ? StoreStatus.Ok : StoreStatus.PermanentError;
    }

    /// <summary>R5's PROMOTION of <paramref name="state"/> at the claimed generation <paramref name="p"/>: the CLAIM (page
    /// 2 := the state at generation P with PROMOTING set, <c>committing</c> and <c>stamp mint</c> = the retirement's
    /// claim P + 1) → sync → both committed slots read and the state written, with <c>committed generation</c> = P, into
    /// the slot that does NOT hold the committed meta (decided from the slots as they are now, never from memory) → sync
    /// → the RETIREMENT. For the fence, <paramref name="state"/> is the adopted meta and P = F.</summary>
    private StoreStatus PromoteAt(MetaPage state, ulong committing, ulong p, Rejected? rejected)
    {
        _stampMint = Math.Max(_stampMint, p);
        ulong retirement = Claim();
        var claim = WithMints(state with { Generation = p, Promoting = true, Committing = committing });
        _promotionPending = true;
        if (!TryWrite(MetaPage.WorkSlot, claim.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
        _lastSeen = p;
        if (!TrySync()) return StoreStatus.PermanentError;
        if (!TryReadSlots(out var a, out var b)) return StoreStatus.PermanentError;
        uint target = MetaPage.Committed(a, b) is { Slot: MetaPage.SlotA } ? MetaPage.SlotB : MetaPage.SlotA;
        var slot = claim with { Promoting = false, CommittedGeneration = p };
        if (!TryWrite(target, slot.Encode(target))) return StoreStatus.PermanentError;
        if (!TrySync()) return StoreStatus.PermanentError;
        var retired = Retire(claim, retirement, rejected);
        if (retired == StoreStatus.Ok) _changedSincePromotion = false;
        return retired;
    }

    /// <summary>The RETIREMENT statement (2.2.1.6 R5; finding R2-1, R3-2): an ordinary statement at
    /// <paramref name="claim"/> on the base <paramref name="promoted"/>'s generation. The promoted ledger's
    /// <c>released</c> and <c>freed</c> pages are inserted into the FREE tree (path-copied: the FREE-tree pages it
    /// replaces are committed pages, so they are <c>freed</c> in the new ledger), its <c>fresh</c> entries are dropped
    /// (committed pages now), and the promoted ledger's own pages — referenced by the committed slot until the next
    /// promotion — are <c>freed</c> in the new ledger (R4); at a fence after a rejection, the rejected history's
    /// never-committed pages join the FREE tree too (a page the committed FREE tree already holds is held once). A page
    /// past the promoted state's page count never enters the FREE tree: it is reached by nothing and the next growth
    /// reuses it.</summary>
    private StoreStatus Retire(MetaPage promoted, ulong claim, Rejected? rejected)
    {
        if (Ledger.Load(this, promoted.LedgerRoot, out var old) != PageCheck.Whole) return StoreStatus.PermanentError;
        var ownPages = old!.OwnPages;
        var own = ownPages.Select(p => p.Page).ToHashSet();
        var moved = new SortedSet<uint>(old.Pages(LedgerState.Released));
        moved.UnionWith(old.Pages(LedgerState.Freed));
        if (rejected?.Ledger is { } abandoned)
        {
            moved.UnionWith(abandoned.Pages(LedgerState.Fresh));
            moved.UnionWith(abandoned.Pages(LedgerState.Released));
            moved.UnionWith(abandoned.OwnPages.Select(p => p.Page));
        }
        moved.RemoveWhere(p => p < MetaPage.MetaPages || p >= promoted.PageCount || own.Contains(p));
        var ledger = new Ledger(this);
        foreach (var p in ownPages) ledger.Set(p.Page, p.Generation, LedgerState.Freed);
        var free = new BTree(this, KeyOrders.Ordinal, promoted.FreeRoot);
        foreach (uint p in moved)
            if (free.Put(KeyOrders.PageKey(p), []) != StoreStatus.Ok) return StoreStatus.PermanentError;
        // A promotion has no '24': a retirement that would grow past the format's last page is '30'.
        if (Reserve(default, ledger, free, promoted.PageCount, out var reservation) != StoreStatus.Ok) return StoreStatus.PermanentError;
        var s = new PageStatement(this, claim, ledger, free, reservation, promoted);
        if (!WriteBookkeeping(s, out var freeRoot, out var ledgerRoot)) return StoreStatus.PermanentError;
        var retired = WithMints(promoted with
        {
            Generation = claim,
            CommittedGeneration = promoted.Generation,
            Promoting = false,
            // The retirement clears `committing`; removing the TRANSACTIONS entry it names, and the holdings its UNDO
            // walk lists, is the transaction slice's part of this statement (kb/Work PB3337) — until it lands that tree
            // is always empty, as R6 step 5's dead-owner step reads it.
            Committing = 0,
            FreeRoot = freeRoot,
            LedgerRoot = ledgerRoot,
            PageCount = Math.Max(promoted.PageCount, s.Reservation.GrownTo),
        });
        if (!TryWrite(MetaPage.WorkSlot, retired.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
        SetState(retired, ledger);
        _lastSeen = retired.Generation;
        _promotionPending = false;
        return StoreStatus.Ok;
    }

    // ── The bookkeeping pages: the FREE tree and the LEDGER, written last from the reservation ──────────────────────

    /// <summary>Write statement <paramref name="s"/>'s FREE tree, when its reservation took FREE pages or a retirement
    /// moved pages into it (path-copied: its new pages are listed <c>fresh</c>, the pages it replaces <c>released</c> or
    /// <c>freed</c> by the ledger's rule), and then its LEDGER, settled and written LAST (R2). Nothing is allocated here:
    /// every page is the next of the reservation.</summary>
    private static bool WriteBookkeeping(PageStatement s, out PagePointer freeRoot, out PagePointer ledgerRoot)
    {
        freeRoot = s.Meta.FreeRoot;
        ledgerRoot = PagePointer.None;
        if (s.FreeTree.HasPendingChanges && s.FreeTree.Flush(s, out freeRoot) != StoreStatus.Ok) return false;
        return s.Ledger.Write(s, out ledgerRoot) == StoreStatus.Ok && !s.Refused;
    }

    /// <summary>A page the work tree stops referencing: <c>released</c> when it was allocated since the base (the ledger
    /// lists it <c>fresh</c>), <c>freed</c> when it is a committed page (R2, R4).</summary>
    internal static void Supersede(Ledger ledger, PagePointer page)
    {
        if (ledger.TryGet(page.Page, out var entry))
        {
            if (entry.State != LedgerState.Fresh)
                throw new InvalidOperationException($"page {page.Page} released twice");
            ledger.Set(page.Page, entry.Generation, LedgerState.Released);
        }
        else
            ledger.Set(page.Page, page.Generation, LedgerState.Freed);
    }

    /// <summary>R2's RESERVATION for a statement of <paramref name="bound"/>: the <c>released</c> pages of
    /// <paramref name="ledger"/> (reusable by a statement later than the one that released them), then one batch of the
    /// lowest pages of the FREE tree <paramref name="free"/> (deleted from it in memory: its pending edit is the
    /// statement's FREE-tree change), then growth from <paramref name="pageCount"/>. The size is settled with the pages
    /// the FREE tree's edit and the ledger's write will take — the ledger's sized by every change the statement makes to
    /// it: each page reserved, each page the statement's trees and the FREE tree's edit stop referencing ('30' for a torn
    /// FREE-tree page; the BOUNDARY, 2.2.1.9, when the growth would pass the format's 2^32 pages).</summary>
    private static StoreStatus Reserve(StatementBound bound, Ledger ledger, BTree free, ulong pageCount, out Reservation reservation)
    {
        reservation = null!;
        var released = ledger.Pages(LedgerState.Released).ToList();
        var fromFree = new List<uint>();
        bool exhausted = false;
        int need = 0;
        while (true)
        {
            int next = bound.Pages + free.PagesToWrite + ledger.WriteBound(need, bound.Releases + free.Superseded.Count);
            if (next <= need) break;
            need = next;
            int wanted = need - Math.Min(released.Count, need);
            if (!exhausted && fromFree.Count < wanted && TakeLowest(free, wanted - fromFree.Count, fromFree, ref exhausted) != StoreStatus.Ok)
                return StoreStatus.PermanentError;
        }
        var reserved = new List<(uint Page, PageOrigin Origin)>(need);
        foreach (uint p in released.Take(need)) reserved.Add((p, PageOrigin.Released));
        foreach (uint p in fromFree.Take(need - reserved.Count)) reserved.Add((p, PageOrigin.Free));
        if (pageCount + (ulong)(need - reserved.Count) > FormatPages) return StoreStatus.Boundary;
        for (ulong p = pageCount; reserved.Count < need; p++) reserved.Add(((uint)p, PageOrigin.Growth));
        reservation = new Reservation(reserved, pageCount);
        return StoreStatus.Ok;
    }

    /// <summary>The <paramref name="count"/> lowest pages of the FREE tree (fewer, and <paramref name="exhausted"/>, when
    /// it holds fewer), deleted from it in memory.</summary>
    private static StoreStatus TakeLowest(BTree free, int count, List<uint> into, ref bool exhausted)
    {
        if (free.First(out var cursor) != StoreStatus.Ok) return StoreStatus.PermanentError;
        var keys = new List<byte[]>(count);
        while (cursor.HasCurrent && keys.Count < count)
        {
            keys.Add(cursor.Key);
            if (cursor.Next() != StoreStatus.Ok) return StoreStatus.PermanentError;
        }
        exhausted |= keys.Count < count;
        foreach (var key in keys)
        {
            if (free.Delete(key, out _) != StoreStatus.Ok) return StoreStatus.PermanentError;
            into.Add(KeyOrders.PageOf(key));
        }
        return StoreStatus.Ok;
    }

    // ── Verifying and holding a meta's structures ───────────────────────────────────────────────────────────────

    /// <summary>What reading a meta's LEDGER tree or verifying its <c>fresh</c> pages found: whole; a page that did not
    /// verify or a body that did not decode (DAMAGED: R6 step 3 rejects it); or a read the medium REFUSED — no verdict on
    /// the page at all, so its OPEN answers '30' and decides nothing from it.</summary>
    private enum Load
    {
        Loaded,
        Damaged,
        Refused,
    }

    private static Load LoadOf(PageCheck check) => check switch
    {
        PageCheck.Whole => Load.Loaded,
        PageCheck.Unreadable => Load.Refused,
        _ => Load.Damaged,
    };

    /// <summary>R6 step 3: every <c>fresh</c> page of a DESCENDANT's ledger verifies against the ledger's generation —
    /// <see cref="Load.Damaged"/> at the first that does not, <see cref="Load.Refused"/> at a read the medium refused.</summary>
    private Load FreshPagesVerify(Ledger ledger)
    {
        var page = new byte[PageHeader.PageSize];
        foreach (var (p, entry) in ledger.Entries)
            if (entry.State == LedgerState.Fresh && Read(new PagePointer(p, entry.Generation), page) is var check
                && check != PageCheck.Whole)
                return LoadOf(check);
        return Load.Loaded;
    }

    private void SetState(MetaPage meta, Ledger ledger)
    {
        _meta = meta;
        _ledger = ledger;
    }

    private bool TryReadSlots(out MetaPage? a, out MetaPage? b)
    {
        a = b = null;
        var page = new byte[PageHeader.PageSize];
        if (!TryReadRaw(MetaPage.SlotA, page)) return false;
        a = MetaPage.Decode(page, MetaPage.SlotA);
        if (!TryReadRaw(MetaPage.SlotB, page)) return false;
        b = MetaPage.Decode(page, MetaPage.SlotB);
        return true;
    }

    // ── The mints ───────────────────────────────────────────────────────────────────────────────────────────────

    private void SeeMints(MetaPage m)
    {
        _stampMint = Math.Max(_stampMint, Math.Max(m.StampMint, m.Generation));
        _releaseMint = Math.Max(_releaseMint, m.ReleaseMint);
        _transactionMint = Math.Max(_transactionMint, m.TransactionMint);
    }

    /// <summary>The next claim: one above the highest of the generation last seen, page 2's mint as last read and this
    /// store's own last claim (2.2.1.7).</summary>
    private ulong NextClaim() => Math.Max(_lastSeen, _stampMint) + 1;

    private ulong Claim() => _stampMint = NextClaim();

    /// <summary><paramref name="m"/> carrying the store's mints: every page 2 the store writes is written from its
    /// memory, so no mint on page 2 ever decreases.</summary>
    private MetaPage WithMints(MetaPage m) => m with
    {
        StampMint = Math.Max(m.StampMint, _stampMint),
        ReleaseMint = Math.Max(m.ReleaseMint, _releaseMint),
        TransactionMint = Math.Max(m.TransactionMint, _transactionMint),
    };

    // ── The medium, every refusal answered ──────────────────────────────────────────────────────────────────────

    private static bool IsRefusal(Exception e) => e is IOException or UnauthorizedAccessException;

    /// <summary>What the last refused write answers at a changing statement: the boundary when the host said the file
    /// cannot grow further (2.2.1.9), else '30'.</summary>
    private StoreStatus _lastRefusal = StoreStatus.PermanentError;

    private bool TryReadRaw(uint page, Span<byte> into)
    {
        try
        {
            _medium.Read(page, into);
            _reads++;
            return true;
        }
        catch (Exception e) when (IsRefusal(e)) { return false; }
    }

    private bool TryWrite(uint page, byte[] bytes)
    {
        try
        {
            _medium.Write(page, bytes);
            _writes++;
            if (page >= MetaPage.MetaPages) _cache.Put(page, bytes);   // a page this store wrote is the page it reads
            return true;
        }
        catch (Exception e) when (IsRefusal(e))
        {
            _lastRefusal = e is MediumSizeLimitException ? StoreStatus.Boundary : StoreStatus.PermanentError;
            return false;
        }
    }

    private bool TrySync()
    {
        try
        {
            _medium.Sync();
            _syncs++;
            return true;
        }
        catch (Exception e) when (IsRefusal(e)) { return false; }
    }
}

/// <summary>Where a reserved page came from (R2's order).</summary>
internal enum PageOrigin
{
    /// <summary>A <c>released</c> page of the ledger.</summary>
    Released,

    /// <summary>A page of the FREE set.</summary>
    Free,

    /// <summary>A page at or past the page count: the file grows.</summary>
    Growth,
}

/// <summary>A statement's RESERVATION (R2): its pages, taken in order; what is left at the end is recorded, never lost.</summary>
internal sealed class Reservation(List<(uint Page, PageOrigin Origin)> pages, ulong pageCount)
{
    private int _next;

    /// <summary>How many reserved pages have been taken.</summary>
    public int Position => _next;

    /// <summary>The next reserved page; a statement past its reservation is a caller defect.</summary>
    public uint Take() => _next < pages.Count ? pages[_next++].Page
        : throw new InvalidOperationException("the statement wrote more pages than it reserved");

    /// <summary>The pages not taken yet, in the order they will be taken.</summary>
    public List<(uint Page, PageOrigin Origin)> Remaining() => pages.GetRange(_next, pages.Count - _next);

    /// <summary>The page count after the pages taken so far: one past the highest grown page used.</summary>
    public ulong GrownTo => pages.Take(_next).Where(p => p.Origin == PageOrigin.Growth)
        .Select(p => (ulong)p.Page + 1).DefaultIfEmpty(pageCount).Max();
}

/// <summary>What a changing statement's reservation is sized to (2.2.1.6 R2): <see cref="Pages"/>, the pages it writes
/// itself, and <see cref="Releases"/>, the pages it stops referencing — each a change to the LEDGER (<c>released</c> or
/// <c>freed</c>, R2, R4), so a statement that drops a long overflow chain needs ledger pages for every page of it.</summary>
internal readonly record struct StatementBound(int Pages, int Releases)
{
    /// <summary>The bound of the pending edits of <paramref name="trees"/>: every page their flushes will write and every
    /// page their edits superseded (both known before the statement begins, <see cref="BTree"/>).</summary>
    public static StatementBound Of(params ReadOnlySpan<BTree> trees)
    {
        int pages = 0, releases = 0;
        foreach (var tree in trees)
        {
            pages += tree.PagesToWrite;
            releases += tree.Superseded.Count;
        }
        return new StatementBound(pages, releases);
    }
}

/// <summary>One statement that changes the file (2.2.1.6): it writes its pages from its reservation, stamped with its
/// claimed generation, releases the pages its trees stop referencing, sets its roots, and completes with page 2 LAST.
/// A refused write makes every later write a no-op and the completion answer that refusal ('30', or the boundary when the
/// host said the file cannot grow further); nothing the statement did is reachable (R1). It is the
/// <see cref="IPageSink"/> its trees flush through.</summary>
internal sealed class PageStatement : IPageSink
{
    private readonly PagedFile _store;
    private MetaPage _meta;

    internal PageStatement(PagedFile store, ulong generation, Ledger ledger, BTree freeTree, Reservation reservation, MetaPage meta)
    {
        _store = store;
        Generation = generation;
        Ledger = ledger;
        FreeTree = freeTree;
        Reservation = reservation;
        _meta = meta;
    }

    /// <summary>The statement's claimed generation: the stamp of every page it writes.</summary>
    public ulong Generation { get; }

    /// <summary>Whether the medium refused one of the statement's writes.</summary>
    public bool Refused => Refusal != StoreStatus.Ok;

    /// <summary>What the first refused write answered (<see cref="StoreStatus.Ok"/> while none was).</summary>
    public StoreStatus Refusal { get; private set; } = StoreStatus.Ok;

    internal Ledger Ledger { get; }

    /// <summary>The FREE tree, with the reservation's batch deleted from it (a pending edit the completion flushes).</summary>
    internal BTree FreeTree { get; }

    internal Reservation Reservation { get; }

    internal MetaPage Meta => _meta;

    /// <summary>Write a new page from the reservation, stamped with the statement's generation and listed <c>fresh</c>;
    /// the POINTER to it (none once the statement has been refused). A body longer than <see cref="PageHeader.BodyBytes"/>
    /// is the caller's defect and is refused loudly, never cut to fit.</summary>
    public PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags = 0, ushort entries = 0, uint next = 0) =>
        Write(kind, body, flags, entries, next, listed: true);

    /// <summary>Write one of the LEDGER's own pages, which the ledger never lists (R2).</summary>
    internal PagePointer WriteUnlisted(PageKind kind, ReadOnlySpan<byte> body, byte flags, ushort entries, uint next) =>
        Write(kind, body, flags, entries, next, listed: false);

    private PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags, ushort entries, uint next, bool listed)
    {
        if (body.Length > PageHeader.BodyBytes)
            throw new ArgumentException($"a page body holds {PageHeader.BodyBytes} bytes, not {body.Length}", nameof(body));
        if (Refused) return PagePointer.None;
        uint page = Reservation.Take();
        var bytes = new byte[PageHeader.PageSize];
        new PageHeader(kind, flags, entries, Generation, page, next).Encode(bytes);
        body.CopyTo(bytes.AsSpan(PageHeader.Bytes));
        PageHeader.Seal(bytes);
        if (_store.WritePage(page, bytes) is var written && written != StoreStatus.Ok)
        {
            Refusal = written;
            return PagePointer.None;
        }
        if (listed) Ledger.Set(page, Generation, LedgerState.Fresh);
        return new PagePointer(page, Generation);
    }

    /// <summary>A page the work tree no longer references (R2: <c>released</c> or <c>freed</c>).</summary>
    public void Release(PagePointer pointer)
    {
        if (!Refused) PagedFile.Supersede(Ledger, pointer);
    }

    /// <summary>The prime (relative: record-number) tree's new root.</summary>
    public void SetPrimeRoot(PagePointer root) => _meta = _meta with { PrimeRoot = root };

    /// <summary>Alternate key <paramref name="alternate"/>'s tree's new root.</summary>
    public void SetAlternateRoot(int alternate, PagePointer root)
    {
        var roots = _meta.AlternateRoots.ToArray();
        roots[alternate] = root;
        _meta = _meta with { AlternateRoots = roots };
    }

    /// <summary>Write the statement's bookkeeping pages and page 2, last.</summary>
    public StoreStatus Complete() => _store.Complete(this);

    /// <summary>End the statement without completing it (its caller met a torn read): page 2 still names the previous
    /// statement, and the claim stays raised.</summary>
    public void Abandon() => _store.End(this);
}
