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

    private readonly IPageMedium _medium;
    private readonly bool _writable;
    private readonly bool _coherent;

    private MetaPage _meta;
    private Ledger _ledger = new();
    private SortedSet<uint> _free = [];
    private List<PagePointer> _ledgerChain = [];
    private List<PagePointer> _freeChain = [];

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
    private long _reads, _writes, _syncs;
    private int _cacheEpoch;
    private Adoption _adoption;

    private PagedFile(IPageMedium medium, bool writable, bool coherent, MetaPage meta)
    {
        _medium = medium;
        _writable = writable;
        _coherent = coherent;
        _meta = meta;
    }

    /// <summary>The meta the store holds: the adopted meta, or page 2 from the moment its generation changes.</summary>
    public MetaPage Meta => _meta;

    /// <summary>The ledger the store holds.</summary>
    public IReadOnlyDictionary<uint, LedgerEntry> LedgerEntries => _ledger.Entries;

    /// <summary>The FREE set the store holds.</summary>
    public IReadOnlyCollection<uint> FreePages => _free;

    /// <summary>The highest generation this store has claimed (or seen claimed).</summary>
    public ulong LastClaim => _stampMint;

    /// <summary>The page I/O performed so far.</summary>
    public IoCounters Counters => new(_reads, _writes, _syncs);

    /// <summary>One more each time the store adopted page 2 from another run unit and dropped its page cache.</summary>
    public int CacheEpoch => _cacheEpoch;

    /// <summary>What the OPEN's adoption rule decided.</summary>
    public Adoption Adoption => _adoption;

    // ── Creation and OPEN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>OPEN OUTPUT</c>'s creation (§14.9.27.4 GR18, <i>"After the creation of the file, the file contains no
    /// records"</i>): the medium is EMPTIED first — so a death part-way leaves no page of a previous store to adopt — then
    /// the three meta pages of an empty store are written and synced. A death before page 0 or page 1 landed leaves no
    /// valid committed slot (the next OPEN answers '30'); after either, an empty store.</summary>
    public static StoreStatus Create(IPageMedium medium, FixedFileAttributes attributes, bool coherent, out PagedFile? store)
    {
        if (!MetaPage.Fits(attributes))
            throw new ArgumentException("the key descriptors exceed the meta page; the OPEN refuses the declaration first", nameof(attributes));
        store = null;
        var meta = MetaPage.Empty(attributes);
        var created = new PagedFile(medium, writable: true, coherent, meta);
        try { medium.SetPageCount(0); }
        catch (Exception e) when (IsRefusal(e)) { return StoreStatus.PermanentError; }
        for (uint slot = 0; slot < MetaPage.MetaPages; slot++)
            if (!created.TryWrite(slot, meta.Encode(slot))) return StoreStatus.PermanentError;
        if (!created.TrySync()) return StoreStatus.PermanentError;
        created._adoption = new Adoption(WorkSlotClass.Descendant, WorkAdopted: true, Fenced: false);
        store = created;
        return StoreStatus.Ok;
    }

    /// <summary>OPEN an existing store: 2.2.1.6 R6's ADOPTION RULE, run by the caller under the store mutex, and through
    /// a writable handle its FENCE. Pages 0, 1 and 2 are read and validated; the COMMITTED meta C is the valid committed
    /// slot with the higher generation G ('30' when neither validates, nothing written); the WORK slot is a DESCENDANT
    /// (base G), the PROMOTION SOURCE (base below G, generation G) or FOREIGN; a DESCENDANT whose every <c>fresh</c> page
    /// verifies is adopted, anything else gives C. A writable store then promotes the adopted meta at
    /// F = G + 4 097 (above every stamp a history based on G can have issued, R2) with its retirement, which also reclaims
    /// a rejected DESCENDANT's readable ledger; an INPUT store adopts in memory and writes nothing.</summary>
    public static StoreStatus Open(IPageMedium medium, bool writable, bool coherent, out PagedFile? store)
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
        var opened = new PagedFile(medium, writable, coherent, committed) { _reads = MetaPage.MetaPages };
        ulong g = committed.Generation;
        var workClass = work is null || work.Attributes.Conflicts(committed.Attributes) ? WorkSlotClass.Foreign
            : work.CommittedGeneration == g ? WorkSlotClass.Descendant
            : work.CommittedGeneration < g && work.Generation == g ? WorkSlotClass.PromotionSource
            : WorkSlotClass.Foreign;

        var adopted = committed;
        Rejected? rejected = null;
        if (workClass == WorkSlotClass.Descendant)
        {
            bool readable = opened.TryLoad(work!, out var ledger, out _, out var ledgerChain, out _);
            if (readable && opened.FreshPagesVerify(ledger)) adopted = work!;
            else rejected = new Rejected(readable ? ledger : null, readable ? ledgerChain : []);
        }
        if (!opened.TryLoad(adopted, out var l, out var f, out var lc, out var fc)) return StoreStatus.PermanentError;
        opened.SetState(adopted, l, f, lc, fc);
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
    /// pages and its own pages were never committed, and the fence's retirement returns them to the FREE set).</summary>
    private sealed record Rejected(Ledger? Ledger, List<PagePointer> LedgerChain);

    // ── The statement's entry ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>A statement's COHERENT ENTRY (2.2.1.7), the whole of a READ's: page 2 is read — ONE page — and compared
    /// with what the store last saw there. A changed <c>generation</c> is another run unit's completed statement,
    /// retirement or fence: the store ADOPTS page 2 (its roots, its ledger) and drops its page cache. A writer also takes
    /// page 2's mints (a <c>stamp mint</c> above the generation is a dead or refused attempt: the next claim goes above
    /// it, nothing is dropped) and resolves a PROMOTING flag first (R5's restart rule). A NO OTHER store has nobody to
    /// adopt from and reads page 2 only to resolve its own refused promotion.</summary>
    public StoreStatus Enter()
    {
        if (!_coherent && !_promotionPending) return StoreStatus.Ok;
        var page = new byte[PageHeader.PageSize];
        if (!TryReadRaw(MetaPage.WorkSlot, page)) return StoreStatus.PermanentError;
        var work = MetaPage.Decode(page, MetaPage.WorkSlot);
        // A torn page 2 under a live store: a reader keeps the tree it holds (nothing it can follow moved); a writer
        // cannot know what another run unit completed, so it answers '30' (§9.1.13.6 item 1).
        if (work is null) return _writable ? StoreStatus.PermanentError : StoreStatus.Ok;
        if (work.Generation != _lastSeen)
        {
            if (!TryLoad(work, out var l, out var f, out var lc, out var fc)) return StoreStatus.PermanentError;
            SetState(work, l, f, lc, fc);
            _lastSeen = work.Generation;
            _cacheEpoch++;
        }
        if (!_writable) return StoreStatus.Ok;
        SeeMints(work);
        if (!work.Promoting)
        {
            _promotionPending = false;
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

    /// <summary>Begin a statement that CHANGES the file, bounded to <paramref name="pageBound"/> pages of its own (the
    /// tree pages it writes), minting <paramref name="ordinals"/> release ordinals and <paramref name="transactionIds"/>
    /// transaction ids. Its entry (<see cref="Enter"/>); the ceiling and the ledger checkpoint (a promotion first, R2 and
    /// R5); its CLAIM — in memory, and for a coherent store on page 2 with the roots, base and generation unchanged,
    /// before its first page write (2.2.1.7); and its RESERVATION — the ledger's <c>released</c> pages, then one batch of
    /// the lowest FREE pages, then growth, sized to the bound plus the FREE and LEDGER pages the statement rewrites, so
    /// nothing is allocated once the ledger is being written (R2).</summary>
    public StoreStatus Begin(int pageBound, out PageStatement? statement, int ordinals = 0, int transactionIds = 0)
    {
        statement = null;
        if (!_writable) throw new InvalidOperationException("an INPUT store changes nothing");
        if (_active is not null) throw new InvalidOperationException("one statement at a time");
        if (Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        if ((NextClaim() > _meta.CommittedGeneration + GenerationCeiling || _ledger.Count > LedgerCheckpoint)
            && PromoteAt(_meta, 0, Claim(), null) != StoreStatus.Ok)
            return StoreStatus.PermanentError;
        ulong claim = Claim();
        _releaseMint = Math.Max(_releaseMint, _meta.ReleaseMint) + (ulong)ordinals;
        _transactionMint = Math.Max(_transactionMint, _meta.TransactionMint) + (ulong)transactionIds;
        if (_coherent && !TryWrite(MetaPage.WorkSlot, WithMints(_meta).Encode(MetaPage.WorkSlot)))
            return StoreStatus.PermanentError;
        var ledger = _ledger.Clone();
        var free = new SortedSet<uint>(_free);
        int bound = ReservationBound(pageBound, ledger.Count, free.Count);
        var reservation = Reserve(bound, ledger, free, _meta.PageCount);
        _active = statement = new PageStatement(this, claim, ledger, free, reservation, _meta);
        return StoreStatus.Ok;
    }

    /// <summary>Complete <paramref name="s"/>: the superseded ledger pages become <c>released</c>, the FREE and LEDGER
    /// pages are written from the reservation, and page 2 LAST, with the claim as its generation (2.2.1.6 R1-R3). A
    /// refusal anywhere leaves page 2 naming the previous statement's tree, untouched (R1), and the store's memory as it
    /// was but for the claim, which stays raised (finding R4-3).</summary>
    internal StoreStatus Complete(PageStatement s)
    {
        End(s);
        if (s.Refused) return StoreStatus.PermanentError;
        foreach (var p in _ledgerChain) s.Ledger.Set(p.Page, p.Generation, LedgerState.Released);
        if (!WriteBookkeeping(s.Generation, s.Ledger, s.Free, s.Reservation, s.Reservation.TookFromFree, _freeChain,
                _freeChain.Count > 0 ? _meta.FreeRoot : PagePointer.None,
                out var freeRoot, out var freeChain, out var ledgerRoot, out var ledgerChain, out ulong pageCount))
            return StoreStatus.PermanentError;
        var completed = WithMints(s.Meta with
        {
            Generation = s.Generation,
            FreeRoot = freeRoot,
            LedgerRoot = ledgerRoot,
            PageCount = Math.Max(s.Meta.PageCount, pageCount),
            Promoting = false,
        });
        if (!TryWrite(MetaPage.WorkSlot, completed.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
        SetState(completed, s.Ledger, s.Free, ledgerChain, freeChain);
        _lastSeen = completed.Generation;
        return StoreStatus.Ok;
    }

    /// <summary>End <paramref name="s"/> without completing it.</summary>
    internal void End(PageStatement s)
    {
        if (!ReferenceEquals(_active, s)) throw new InvalidOperationException("not the active statement");
        _active = null;
    }

    /// <summary>Write one page of statement <paramref name="s"/>.</summary>
    internal bool WritePage(uint page, byte[] bytes) => TryWrite(page, bytes);

    // ── Reads ───────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Read a page through a stamped pointer (2.2.1.6 R3): <see cref="PageCheck.Whole"/> only when its checksum
    /// holds, its <c>self</c> is the page read and its generation is the stamp; a statement answers anything else as
    /// '30'.</summary>
    public PageCheck Read(PagePointer pointer, Span<byte> page)
    {
        if (pointer.IsNone) throw new ArgumentException("no page", nameof(pointer));
        return TryReadRaw(pointer.Page, page) ? PageHeader.Check(page, pointer.Page, pointer.Generation) : PageCheck.Unreadable;
    }

    // ── Promotion ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A CHECKPOINT promotion (2.2.1.6 R5), after the statement's entry.</summary>
    public StoreStatus Promote()
    {
        if (!_writable) throw new InvalidOperationException("an INPUT store promotes nothing");
        if (Enter() != StoreStatus.Ok) return StoreStatus.PermanentError;
        return PromoteAt(_meta, 0, Claim(), null);
    }

    /// <summary>CLOSE: through a writable handle a promotion, then one more sync, so a closed file's OPEN verifies the
    /// retirement's few pages and nothing else (R5); through an INPUT handle nothing.</summary>
    public StoreStatus Close()
    {
        if (!_writable) return StoreStatus.Ok;
        if (Promote() != StoreStatus.Ok) return StoreStatus.PermanentError;
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
        return Retire(claim, retirement, rejected);
    }

    /// <summary>The RETIREMENT statement (2.2.1.6 R5; finding R2-1, R3-2): an ordinary statement at
    /// <paramref name="claim"/> on the base <paramref name="promoted"/>'s generation. The promoted ledger's
    /// <c>released</c> and <c>freed</c> pages move into the FREE set, its <c>fresh</c> entries are dropped (committed
    /// pages now), and the promoted ledger's own pages and the promoted FREE chain — referenced by the committed slot until
    /// the next promotion — are <c>freed</c> in the new ledger (R4); at a fence after a rejection, the rejected history's
    /// never-committed pages join the FREE set too. A page past the promoted state's page count never enters the FREE set:
    /// it is reached by nothing and the next growth reuses it.</summary>
    private StoreStatus Retire(MetaPage promoted, ulong claim, Rejected? rejected)
    {
        if (!TryLoad(promoted, out var old, out var oldFree, out var oldLedgerChain, out var oldFreeChain))
            return StoreStatus.PermanentError;
        var committedPages = new HashSet<uint>(old.Pages(LedgerState.Fresh));
        committedPages.UnionWith(oldLedgerChain.Select(c => c.Page));
        committedPages.UnionWith(oldFreeChain.Select(c => c.Page));
        var free = new SortedSet<uint>(oldFree);
        free.UnionWith(old.Pages(LedgerState.Released));
        free.UnionWith(old.Pages(LedgerState.Freed));
        if (rejected?.Ledger is { } abandoned)
        {
            free.UnionWith(abandoned.Pages(LedgerState.Fresh));
            free.UnionWith(abandoned.Pages(LedgerState.Released));
            free.UnionWith(rejected.LedgerChain.Select(c => c.Page));
        }
        free.RemoveWhere(p => p < MetaPage.MetaPages || p >= promoted.PageCount || committedPages.Contains(p));
        var ledger = new Ledger();
        foreach (var c in oldLedgerChain.Concat(oldFreeChain)) ledger.Set(c.Page, c.Generation, LedgerState.Freed);
        var reservation = Reserve(ReservationBound(0, ledger.Count, free.Count), ledger, free, promoted.PageCount);
        if (!WriteBookkeeping(claim, ledger, free, reservation, freeChanged: true, oldFreeChain: [], currentFreeRoot: PagePointer.None,
                out var freeRoot, out var freeChain, out var ledgerRoot, out var ledgerChain, out ulong pageCount))
            return StoreStatus.PermanentError;
        var retired = WithMints(promoted with
        {
            Generation = claim,
            CommittedGeneration = promoted.Generation,
            Promoting = false,
            Committing = 0,
            FreeRoot = freeRoot,
            LedgerRoot = ledgerRoot,
            PageCount = Math.Max(promoted.PageCount, pageCount),
        });
        if (!TryWrite(MetaPage.WorkSlot, retired.Encode(MetaPage.WorkSlot))) return StoreStatus.PermanentError;
        SetState(retired, ledger, free, ledgerChain, freeChain);
        _lastSeen = retired.Generation;
        _promotionPending = false;
        return StoreStatus.Ok;
    }

    // ── The bookkeeping pages: the FREE set and the LEDGER, written last from the reservation ───────────────────────

    /// <summary>Write a statement's FREE chain (when the set changed: the superseded chain's pages become <c>released</c>
    /// or <c>freed</c> by the ledger's rule, the new chain's pages <c>fresh</c>) and then its LEDGER chain, both from the
    /// reservation; the reserved pages left over are <c>released</c> (a FREE page) or stay as they were (a released page)
    /// or are simply not grown into. Nothing is allocated here: every page comes from the reservation (R2).</summary>
    private bool WriteBookkeeping(ulong claim, Ledger ledger, SortedSet<uint> free, Reservation reservation, bool freeChanged,
        List<PagePointer> oldFreeChain, PagePointer currentFreeRoot,
        out PagePointer freeRoot, out List<PagePointer> freeChain, out PagePointer ledgerRoot, out List<PagePointer> ledgerChain,
        out ulong pageCount)
    {
        freeRoot = currentFreeRoot;
        freeChain = oldFreeChain;
        ledgerRoot = PagePointer.None;
        ledgerChain = [];
        pageCount = 0;
        if (freeChanged)
        {
            foreach (var p in oldFreeChain) Supersede(ledger, p);
            var payload = PageChain.EncodePageSet(free);
            var pages = reservation.Take(PageChain.PagesFor(payload.Length));
            if (!PageChain.Compose(payload, pages, claim, TryWrite, out freeRoot)) return false;
            freeChain = pages.Select(p => new PagePointer(p, claim)).ToList();
            foreach (var p in pages) ledger.Set(p, claim, LedgerState.Fresh);
        }
        int k = 0;
        while (PageChain.PagesFor(Ledger.BytesFor(ledger.Count + reservation.UnusedFreeAfter(k))) > k) k++;
        var ledgerPages = reservation.Take(k);
        // The ledger does not list its own pages (R2): a released page reused for its chain leaves the list.
        foreach (var p in ledgerPages) ledger.Remove(p);
        foreach (var p in reservation.UnusedFree()) ledger.Set(p, 0, LedgerState.Released);
        if (!PageChain.Compose(ledger.Encode(), ledgerPages, claim, TryWrite, out ledgerRoot)) return false;
        ledgerChain = ledgerPages.Select(p => new PagePointer(p, claim)).ToList();
        pageCount = reservation.GrownTo;
        return true;
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

    /// <summary>The pages a statement of <paramref name="pages"/> tree pages reserves: its own, the FREE chain it may
    /// rewrite and the LEDGER chain it writes (which lists every reserved page and the superseded chains' pages).</summary>
    private int ReservationBound(int pages, int ledgerEntries, int freeCount)
    {
        int freePages = PageChain.PagesFor(4L * freeCount);
        int superseded = _freeChain.Count + _ledgerChain.Count;
        int total = pages + freePages;
        while (true)
        {
            int ledgerPages = PageChain.PagesFor(Ledger.BytesFor((long)ledgerEntries + total + superseded + freePages));
            if (pages + freePages + ledgerPages <= total) return total;
            total = pages + freePages + ledgerPages;
        }
    }

    /// <summary>R2's RESERVATION of <paramref name="count"/> pages: the <c>released</c> pages of
    /// <paramref name="ledger"/> (reusable by a statement later than the one that released them), then one batch of the
    /// lowest pages of <paramref name="free"/> (taken out of it), then growth from <paramref name="pageCount"/>.</summary>
    private static Reservation Reserve(int count, Ledger ledger, SortedSet<uint> free, ulong pageCount)
    {
        var pages = new List<(uint Page, PageOrigin Origin)>(count);
        foreach (uint p in ledger.Pages(LedgerState.Released))
        {
            if (pages.Count == count) break;
            pages.Add((p, PageOrigin.Released));
        }
        foreach (uint p in free)
        {
            if (pages.Count == count) break;
            pages.Add((p, PageOrigin.Free));
        }
        foreach (var (p, origin) in pages)
            if (origin == PageOrigin.Free) free.Remove(p);
        for (ulong next = pageCount; pages.Count < count; next++)
        {
            if (next > uint.MaxValue) throw new IOException("the store has reached the format's 2^32 pages");
            pages.Add(((uint)next, PageOrigin.Growth));
        }
        return new Reservation(pages, pageCount);
    }

    // ── Loading, verifying and holding a meta's structures ──────────────────────────────────────────────────────

    private bool TryLoad(MetaPage meta, out Ledger ledger, out SortedSet<uint> free,
        out List<PagePointer> ledgerChain, out List<PagePointer> freeChain)
    {
        ledger = new Ledger();
        free = [];
        freeChain = [];
        if (PageChain.Read(meta.LedgerRoot, Verify, out var ledgerBytes, out ledgerChain) != PageCheck.Whole
            || Ledger.Decode(ledgerBytes) is not { } decoded)
            return false;
        ledger = decoded;
        if (PageChain.Read(meta.FreeRoot, Verify, out var freeBytes, out freeChain) != PageCheck.Whole
            || PageChain.DecodePageSet(freeBytes) is not { } set)
            return false;
        free = set;
        return true;
    }

    private PageCheck Verify(PagePointer pointer, byte[] page) => Read(pointer, page);

    /// <summary>R6 step 3: every <c>fresh</c> page of a DESCENDANT's ledger verifies against the ledger's generation.</summary>
    private bool FreshPagesVerify(Ledger ledger)
    {
        var page = new byte[PageHeader.PageSize];
        foreach (var (p, entry) in ledger.Entries)
            if (entry.State == LedgerState.Fresh && Read(new PagePointer(p, entry.Generation), page) != PageCheck.Whole)
                return false;
        return true;
    }

    private void SetState(MetaPage meta, Ledger ledger, SortedSet<uint> free, List<PagePointer> ledgerChain, List<PagePointer> freeChain)
    {
        _meta = meta;
        _ledger = ledger;
        _free = free;
        _ledgerChain = ledgerChain;
        _freeChain = freeChain;
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
            return true;
        }
        catch (Exception e) when (IsRefusal(e)) { return false; }
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

    /// <summary>Whether a FREE page was reserved (the FREE set then changed).</summary>
    public bool TookFromFree { get; } = pages.Any(p => p.Origin == PageOrigin.Free);

    /// <summary>The next reserved page; a statement past its bound is a caller defect.</summary>
    public uint Take() => _next < pages.Count ? pages[_next++].Page
        : throw new InvalidOperationException("the statement wrote more pages than it reserved");

    /// <summary>The next <paramref name="count"/> reserved pages.</summary>
    public List<uint> Take(int count)
    {
        var taken = new List<uint>(count);
        for (int i = 0; i < count; i++) taken.Add(Take());
        return taken;
    }

    /// <summary>The FREE pages that would be left over after <paramref name="more"/> further takes.</summary>
    public int UnusedFreeAfter(int more) => pages.Skip(_next + more).Count(p => p.Origin == PageOrigin.Free);

    /// <summary>The FREE pages left over.</summary>
    public IEnumerable<uint> UnusedFree() => pages.Skip(_next).Where(p => p.Origin == PageOrigin.Free).Select(p => p.Page);

    /// <summary>The page count after the pages taken so far: one past the highest grown page used.</summary>
    public ulong GrownTo => pages.Take(_next).Where(p => p.Origin == PageOrigin.Growth)
        .Select(p => (ulong)p.Page + 1).DefaultIfEmpty(pageCount).Max();
}

/// <summary>One statement that changes the file (2.2.1.6): it writes its pages from its reservation, stamped with its
/// claimed generation, releases the pages its tree stops referencing, sets its roots, and completes with page 2 LAST.
/// A refused write makes every later write a no-op and the completion '30'; nothing the statement did is reachable
/// (R1).</summary>
internal sealed class PageStatement
{
    private readonly PagedFile _store;
    private MetaPage _meta;

    internal PageStatement(PagedFile store, ulong generation, Ledger ledger, SortedSet<uint> free, Reservation reservation, MetaPage meta)
    {
        _store = store;
        Generation = generation;
        Ledger = ledger;
        Free = free;
        Reservation = reservation;
        _meta = meta;
    }

    /// <summary>The statement's claimed generation: the stamp of every page it writes.</summary>
    public ulong Generation { get; }

    /// <summary>Whether the medium refused one of the statement's writes.</summary>
    public bool Refused { get; private set; }

    internal Ledger Ledger { get; }

    internal SortedSet<uint> Free { get; }

    internal Reservation Reservation { get; }

    internal MetaPage Meta => _meta;

    /// <summary>Write a new page from the reservation, stamped with the statement's generation; the POINTER to it (none
    /// once the statement has been refused).</summary>
    public PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags = 0, ushort entries = 0, uint next = 0)
    {
        if (Refused) return PagePointer.None;
        uint page = Reservation.Take();
        var bytes = new byte[PageHeader.PageSize];
        new PageHeader(kind, flags, entries, Generation, page, next).Encode(bytes);
        body[..Math.Min(body.Length, PageHeader.BodyBytes)].CopyTo(bytes.AsSpan(PageHeader.Bytes));
        PageHeader.Seal(bytes);
        if (!_store.WritePage(page, bytes))
        {
            Refused = true;
            return PagePointer.None;
        }
        Ledger.Set(page, Generation, LedgerState.Fresh);
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
