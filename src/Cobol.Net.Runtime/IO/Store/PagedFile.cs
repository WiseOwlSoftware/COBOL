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

/// <summary>How the adoption rule classified the WORK slot at OPEN (§2.2.1.6 R6 step 2).</summary>
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

/// <summary>What the OPEN's adoption rule decided (§2.2.1.6 R6): the work slot's class, whether the work slot was
/// adopted (a verified DESCENDANT) or the committed meta, and whether the OPEN fenced.</summary>
internal readonly record struct Adoption(WorkSlotClass WorkSlot, bool WorkAdopted, bool Fenced);

/// <summary>The page I/O a store performed (§2.2.1.11): pages read, pages written, syncs. Cumulative; a witness takes
/// the difference across a statement.</summary>
internal readonly record struct IoCounters(long PagesRead, long PagesWritten, long Syncs);

/// <summary>⛔ THE PAGE LAYER OF THE KEYED STORE ENGINE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4-§2.2.1.7,
/// kb/Work PB3331): pages over an <see cref="IPageMedium"/>, the three meta slots, stamped pointers, the generation
/// ledger, the reservation, the CLAIM, promotion into the alternate committed slot with its retirement, the restart of a
/// promotion from its PROMOTING flag, and the OPEN's adoption rule with its fence. Every rule of §2.2.1.6 (R1-R6) is
/// enforced here and nowhere else; the trees of kb/Work PB3332 and the connectors of PB3333/PB3334 are its callers.
/// <para>A store is one run unit's view of one physical file. Two run units are two stores over one medium; the caller
/// serializes them with the store mutex (§2.2.1.7) — a COHERENT store (every sharing mode but NO OTHER) runs every
/// statement and its OPEN inside it, and reads page 2 at each statement's entry.</para></summary>
internal sealed class PagedFile
{
    /// <summary>R2's GENERATION CEILING, a format constant: every claim of a history whose base is B lies in
    /// (B, B + <see cref="GenerationCeiling"/>]; a statement whose claim would pass it promotes first.</summary>
    public const ulong GenerationCeiling = 4096;

    /// <summary>R5's ledger checkpoint: a ledger past this many pages promotes at the next statement.</summary>
    public const int LedgerCheckpoint = 1024;

    private PagedFile() { }

    /// <summary>Create a new empty store on <paramref name="medium"/> (§14.9.27.4 GR18).</summary>
    public static StoreStatus Create(IPageMedium medium, FixedFileAttributes attributes, bool coherent, out PagedFile? store) =>
        throw new NotImplementedException();

    /// <summary>OPEN an existing store: §2.2.1.6 R6's adoption rule and, through a writable handle, its fence.</summary>
    public static StoreStatus Open(IPageMedium medium, bool writable, bool coherent, out PagedFile? store) =>
        throw new NotImplementedException();

    /// <summary>The meta the store holds: the adopted meta, or page 2 from the moment its generation changes.</summary>
    public MetaPage Meta => throw new NotImplementedException();

    /// <summary>The ledger the store holds.</summary>
    public IReadOnlyDictionary<uint, LedgerEntry> LedgerEntries => throw new NotImplementedException();

    /// <summary>The FREE set the store holds.</summary>
    public IReadOnlyCollection<uint> FreePages => throw new NotImplementedException();

    /// <summary>The highest generation this store has claimed.</summary>
    public ulong LastClaim => throw new NotImplementedException();

    /// <summary>The page I/O performed so far.</summary>
    public IoCounters Counters => throw new NotImplementedException();

    /// <summary>One more each time the store adopted page 2 from another run unit and dropped its page cache.</summary>
    public int CacheEpoch => throw new NotImplementedException();

    /// <summary>What the OPEN's adoption rule decided.</summary>
    public Adoption Adoption => throw new NotImplementedException();

    /// <summary>A statement's coherent entry that claims nothing (a READ's, §2.2.1.7).</summary>
    public StoreStatus Enter() => throw new NotImplementedException();

    /// <summary>Begin a statement that changes the file.</summary>
    public StoreStatus Begin(int pageBound, out PageStatement? statement, int ordinals = 0, int transactionIds = 0) =>
        throw new NotImplementedException();

    /// <summary>Read a page through a stamped pointer (§2.2.1.6 R3).</summary>
    public PageCheck Read(PagePointer pointer, Span<byte> page) => throw new NotImplementedException();

    /// <summary>A checkpoint promotion (§2.2.1.6 R5).</summary>
    public StoreStatus Promote() => throw new NotImplementedException();

    /// <summary>CLOSE: a promotion, then one more sync.</summary>
    public StoreStatus Close() => throw new NotImplementedException();
}

/// <summary>One statement that changes the file.</summary>
internal sealed class PageStatement
{
    private PageStatement() { }

    /// <summary>The statement's claimed generation: the stamp of every page it writes.</summary>
    public ulong Generation => throw new NotImplementedException();

    /// <summary>Whether the medium refused one of the statement's writes.</summary>
    public bool Refused => throw new NotImplementedException();

    /// <summary>Write a new page from the reservation.</summary>
    public PagePointer Write(PageKind kind, ReadOnlySpan<byte> body, byte flags = 0, ushort entries = 0, uint next = 0) =>
        throw new NotImplementedException();

    /// <summary>A page the work tree no longer references.</summary>
    public void Release(PagePointer pointer) => throw new NotImplementedException();

    /// <summary>The prime (relative: record-number) tree's new root.</summary>
    public void SetPrimeRoot(PagePointer root) => throw new NotImplementedException();

    /// <summary>Alternate key <paramref name="alternate"/>'s tree's new root.</summary>
    public void SetAlternateRoot(int alternate, PagePointer root) => throw new NotImplementedException();

    /// <summary>Write the statement's bookkeeping pages and page 2, last.</summary>
    public StoreStatus Complete() => throw new NotImplementedException();

    /// <summary>End the statement without completing it (its caller met a torn read).</summary>
    public void Abandon() => throw new NotImplementedException();
}
