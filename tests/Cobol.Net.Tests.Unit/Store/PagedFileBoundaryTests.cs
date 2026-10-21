// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Runtime.IO.Store;
using Xunit;
using Fault = CobolNet.Tests.Unit.Store.MemoryPageMedium.Fault;

namespace CobolNet.Tests.Unit.Store;

/// <summary>
/// ⛔ THE BOUNDARY OF A RELATIVE OR INDEXED FILE IS THE FORMAT'S 2^32 PAGES AND THE HOST'S MAXIMUM FILE SIZE, AND A
/// STATEMENT THAT MEETS IT LEAVES THE FILE UNCHANGED (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.3 decision 6,
/// 2.2.1.9; kb/Work PB3331). §9.1.13.5 item 4: <i>"I-O status = 24. An attempt is made to write outside the
/// externally-defined boundaries of a physical relative or indexed file. The implementor specifies the manner in which
/// these boundaries are defined."</i> The page layer answers <see cref="StoreStatus.Boundary"/> — never an escaping
/// exception, never '30' — from the changing statement's reservation (the format) and from a write the host refuses
/// because the file cannot grow further (<c>EFBIG</c>, <c>ERROR_FILE_TOO_LARGE</c>); a full medium is '30'. The
/// statement it refuses claims nothing or leaves its pages unreachable (R1), so the file is the previous statement's and
/// the same run unit's next statement that fits succeeds (§9.1.14, <i>"the file is not affected"</i>).
/// </summary>
public sealed class PagedFileBoundaryTests
{
    /// <summary>Every meta page of <paramref name="medium"/> rewritten to record <paramref name="pageCount"/> pages (the
    /// pages in between were never written and are reached by nothing): a store a few pages short of the format's end.</summary>
    private static void SetRecordedPageCount(MemoryPageMedium medium, ulong pageCount)
    {
        for (uint p = 0; p < MetaPage.MetaPages; p++)
            if (MetaPage.Decode(medium.Snapshot(p), p) is { } meta)
                medium.Poke(p, (meta with { PageCount = pageCount }).Encode(p));
    }

    /// <summary>Statements that grow the file, ever closer to the format's 2^32 pages: each one that fits succeeds and
    /// the first that does not answers the boundary with NOTHING written and nothing claimed — the claim is taken after
    /// the reservation — so the next OPEN finds the records of the statements before it; a statement that fits in the
    /// pages a DELETE released still succeeds afterwards. Coherent and NO OTHER alike.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_statement_whose_reservation_would_pass_the_formats_2_32_pages_answers_the_boundary_and_writes_nothing(bool coherent)
    {
        var medium = StoreKit.ClosedStore("1=one");
        SetRecordedPageCount(medium, PagedFile.FormatPages - 64);
        var (h, opened, a) = StoreKit.OpenAs(medium, "A", coherent: coherent);
        Assert.Equal(StoreStatus.Ok, opened);
        string expected = "1=one;";
        int k = 1;
        for (; ; k++)
        {
            Assert.True(k < 64, "the boundary was never met");
            long mark = medium.LastSeq;
            ulong claim = a!.LastClaim;
            var status = StoreKit.Put(a, 100 + k, $"r{k}", blobPages: 6);
            if (status == StoreStatus.Boundary)
            {
                Assert.Equal(mark, medium.LastSeq);   // not one page — not the claim — was written
                Assert.Equal(claim, a.LastClaim);
                break;
            }
            Assert.Equal(StoreStatus.Ok, status);
            expected += $"{100 + k}=r{k};";
        }
        Assert.True(k > 1, "no statement fitted below the boundary");
        Assert.True(a.Meta.PageCount <= PagedFile.FormatPages);
        Assert.True(a.Meta.PageCount > PagedFile.FormatPages - 16, $"the store stopped at {a.Meta.PageCount}");
        Assert.Equal(expected, StoreKit.Show(a));

        // The branch not taken: a statement that fits in what a DELETE released succeeds beside the boundary.
        Assert.Equal(StoreStatus.Ok, StoreKit.Delete(a, 101));
        expected = expected.Replace("101=r1;", "");
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(a, 300, "small"));
        expected += "300=small;";
        Assert.Equal(expected, StoreKit.Show(a));
        var (_, again, c) = StoreKit.OpenAs(medium, "C", writable: false);
        Assert.Equal(StoreStatus.Ok, again);
        Assert.Equal(expected, StoreKit.Show(c!));
        StoreKit.AssertR1(medium);
        StoreKit.AssertNoStampReused(medium);
        _ = h;
    }

    /// <summary>A store that WROTE one record and was never closed (its process died after the statement), over a fresh
    /// medium: the fence of the next writable OPEN retires its adopted state, and — the ledger listing the statement's
    /// page <c>fresh</c> and its own page, with nothing released and nothing in the FREE tree — that retirement writes
    /// ONE new page, the ledger's, from growth. (A second statement would release the first's leaf, which the retirement
    /// moves into the FREE tree and then takes: it needs no growth, and so is never refused — measured for 2 to 50
    /// records. The statements' own released pages always cover a retirement; the pure-growth case is the one a fence
    /// can be refused in.)</summary>
    private static MemoryPageMedium UnclosedStore()
    {
        var (medium, _, store) = StoreKit.NewStore();
        Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, 1, "r1"));
        return medium;
    }

    /// <summary>A writable OPEN's fence at each headroom below the format's last page (every meta page records
    /// <c>FormatPages - headroom</c> pages): the fence retires the adopted state with ONE reservation (2.2.1.6 R5), and
    /// a promotion has no '24' (2.2.1.9) — it answers <see cref="StoreStatus.PermanentError"/> or succeeds. It takes the
    /// pages it WRITES, never the bound its reservation was sized to (R2): it is refused only while the format has
    /// fewer pages than it writes, so the first headroom that lets it complete is exactly the number of pages it wrote
    /// (the store then fills the format's last page) and every smaller headroom answers '30' with the committed records
    /// unchanged and readable (R1) — the refusal arm and the success arm of one rule (kb/Work PB3396).</summary>
    [Fact]
    public void An_open_fence_is_refused_only_for_the_pages_it_writes_not_for_the_bound_of_its_reservation()
    {
        byte[] content = UnclosedStore().Contents();
        const string shown = "1=r1;";
        int? minimal = null;
        for (ulong headroom = 0; headroom <= 64; headroom++)
        {
            var medium = MemoryPageMedium.Holding(content);
            SetRecordedPageCount(medium, PagedFile.FormatPages - headroom);
            var (_, status, store) = StoreKit.OpenAs(medium, "A");
            if (status == StoreStatus.Ok)
            {
                minimal = (int)headroom;
                Assert.Equal(PagedFile.FormatPages, store!.Meta.PageCount);   // it took what it wrote: the last pages, no more
                Assert.Equal(shown, StoreKit.Show(store));
                Assert.Equal(StoreStatus.Ok, store.Close());
                break;
            }
            Assert.Equal(StoreStatus.PermanentError, status);   // a promotion has no '24' (2.2.1.9)
            var (_, again, reader) = StoreKit.OpenAs(medium, "R", writable: false);
            Assert.Equal(StoreStatus.Ok, again);
            Assert.Equal(shown, StoreKit.Show(reader!));
            StoreKit.AssertR1(medium);
            StoreKit.AssertNoStampReused(medium);
        }
        Assert.True(minimal is >= 1, "a fence that writes a page needs at least one page below the format's end");
    }

    /// <summary>The same rule's other end: a fence that needs no page past the page count — the CLOSEd store's retirement
    /// is served from the pages its ledger and FREE tree hold — completes at the format's very last page, where a
    /// reservation sized to a bound would have refused it. Coherent and NO OTHER alike.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_fence_that_writes_no_page_past_the_page_count_completes_at_the_formats_last_page(bool coherent)
    {
        var medium = StoreKit.ClosedStore("1=one", "2=two");
        SetRecordedPageCount(medium, PagedFile.FormatPages);
        var (_, status, store) = StoreKit.OpenAs(medium, "A", coherent: coherent);
        Assert.Equal(StoreStatus.Ok, status);
        Assert.Equal(PagedFile.FormatPages, store!.Meta.PageCount);
        Assert.Equal("1=one;2=two;", StoreKit.Show(store));
        Assert.Equal(StoreStatus.Ok, store.Close());
        StoreKit.AssertR1(medium);
    }

    /// <summary>A store that holds <paramref name="records"/> three-page records 1.. and has DELETEd the first
    /// <paramref name="deleted"/> of them before its CLOSE: the closed file's FREE tree then holds the pages the deletes
    /// freed, and its page count is rewritten to the format's very last page, so only those pages can serve a statement.</summary>
    private static MemoryPageMedium StoreWithFreePagesAtTheFormatsEnd(int records, int deleted)
    {
        var (medium, _, store) = StoreKit.NewStore();
        for (int id = 1; id <= records; id++)
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(store, id, $"r{id}", blobPages: 2));
        for (int id = 1; id <= deleted; id++)
            Assert.Equal(StoreStatus.Ok, StoreKit.Delete(store, id));
        Assert.Equal(StoreStatus.Ok, store.Close());
        SetRecordedPageCount(medium, PagedFile.FormatPages);
        return medium;
    }

    /// <summary>A CLOSE's promotion right behind the one statement that fits beside the format's last page, at every
    /// headroom: a store that has released and freed nothing (its one WRITE created the file's first leaf), so the
    /// retirement has no page to move into the FREE tree and writes its ledger from growth — ONE page — while the bound
    /// its reservation is sized to is several. The CLOSE completes whenever the page exists (the statement left at least
    /// one: its own reservation was the larger bound) and the store reopens holding the record; a bound-sized
    /// reservation refused it for headrooms the statement fitted in (kb/Work PB3396). Coherent and NO OTHER alike.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_close_promotion_behind_the_one_statement_that_fits_completes_in_the_pages_it_writes(bool coherent)
    {
        int fitted = 0;
        for (ulong headroom = 0; headroom <= 30; headroom++)
        {
            var (medium, _, fresh) = StoreKit.NewStore(coherent);
            _ = fresh;
            SetRecordedPageCount(medium, PagedFile.FormatPages - headroom);
            var (_, opened, a) = StoreKit.OpenAs(medium, "A", coherent: coherent);
            if (opened != StoreStatus.Ok)
            {
                Assert.Equal(StoreStatus.PermanentError, opened);   // the fence's retirement found too few pages
                continue;
            }
            var put = StoreKit.Put(a!, 1, "one");
            if (put == StoreStatus.Boundary) continue;   // the statement's own reservation does not fit: nothing to close behind
            Assert.Equal(StoreStatus.Ok, put);
            fitted++;
            Assert.Equal(StoreStatus.Ok, a!.Close());
            var (_, reopened, b) = StoreKit.OpenAs(medium, "B");
            Assert.Equal(StoreStatus.Ok, reopened);
            Assert.True(b!.Meta.PageCount <= PagedFile.FormatPages);
            Assert.Equal("1=one;", StoreKit.Show(b));
            StoreKit.AssertR1(medium);
            StoreKit.AssertNoStampReused(medium);
        }
        Assert.True(fitted > 0, "no headroom fitted the statement: nothing was measured");
    }

    /// <summary>A CLOSE's promotion at the format's last page, its retirement built from every kind of reserved page (the
    /// ledger lists pages RELEASED by the statements' path copies and FREED by a DELETE, and the FREE tree holds pages):
    /// statements spend the FREE pages until the next one's reservation would grow the file (the boundary, nothing
    /// written), a DELETE then frees pages, and the CLOSE retires them. The CLOSE completes — a retirement is refused
    /// only for a page it WRITES past the format's last, and the pages the statements released and freed are the ones
    /// its own reservation takes (the bound-sized reservation refused it here before kb/Work PB3396) — and the store
    /// reopens as the statements left it, still at the format's last page. A CLOSE that cannot write its pages is not
    /// reachable through statements (each leaves its retirement the pages it released: measured over this sweep and over
    /// single-record stores at every headroom of 0 to 30, where the CLOSE never answered '30'); the refusal of a
    /// retirement for a page past the end is witnessed at the fence, the same <c>Retire</c>, above, and a CLOSE's '30'
    /// by a refused page write is KeyedClosePermanentErrorWitnessTests'.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_close_promotion_at_the_formats_last_page_completes_from_the_pages_the_statements_released(bool coherent)
    {
        for (int deleted = 4; deleted <= 40; deleted += 4)
        {
            var medium = StoreWithFreePagesAtTheFormatsEnd(records: 42, deleted);
            var (_, opened, a) = StoreKit.OpenAs(medium, "A", coherent: coherent);
            Assert.Equal(StoreStatus.Ok, opened);
            string expected = StoreKit.Show(a!);
            int record = 100;
            foreach (int blobPages in new[] { 2, 0 })   // then single-page statements, whose smaller reservation fits closer to the end
                for (; ; record++)
                {
                    Assert.True(record < 400, "the boundary was never met");
                    var put = StoreKit.Put(a!, record, $"r{record}", blobPages);
                    if (put == StoreStatus.Boundary) break;
                    Assert.Equal(StoreStatus.Ok, put);
                    expected += $"{record}=r{record};";
                }
            Assert.Equal(PagedFile.FormatPages, a!.Meta.PageCount);   // not one page grew: the FREE pages served every statement
            int victim = record > 100 ? 100 : 42;   // a record the statements wrote, else one the store began with
            var removed = StoreKit.Delete(a, victim);   // its reservation may itself meet the boundary: it then changes nothing
            Assert.True(removed is StoreStatus.Ok or StoreStatus.Boundary, $"DELETE answered {removed}");
            if (removed == StoreStatus.Ok) expected = expected.Replace($"{victim}=r{victim};", "");
            Assert.Equal(expected, StoreKit.Show(a));

            Assert.Equal(StoreStatus.Ok, a.Close());
            var (_, reopened, b) = StoreKit.OpenAs(medium, "B");
            Assert.Equal(StoreStatus.Ok, reopened);
            Assert.Equal(PagedFile.FormatPages, b!.Meta.PageCount);
            Assert.Equal(expected, StoreKit.Show(b));
            Assert.Equal(StoreStatus.Ok, b.Close());
            StoreKit.AssertR1(medium);
            StoreKit.AssertNoStampReused(medium);
        }
    }

    /// <summary>A WRITE, a REWRITE and a DELETE whose page write the host refuses because the file cannot grow further:
    /// the boundary, the records as they were, and — the condition gone — the same run unit's next statement succeeds;
    /// the same refusal for any other reason (a full medium) is '30'. (The connector answers the boundary by the
    /// statement: '24' with INVALID KEY at the WRITE and REWRITE, '30' with no invalid key condition at the DELETE —
    /// FileBoundaryTests, through the RELATIVE and INDEXED connectors.)</summary>
    [Fact]
    public void The_hosts_file_size_refusal_is_the_boundary_and_a_full_medium_is_30_with_the_file_unchanged()
    {
        foreach (var fault in new[] { Fault.SizeLimit, Fault.Refuse })
        {
            var expectedStatus = fault == Fault.SizeLimit ? StoreStatus.Boundary : StoreStatus.PermanentError;
            var medium = StoreKit.ClosedStore("1=one", "2=two");
            var (h, _, a) = StoreKit.OpenAs(medium, "A");
            foreach (var statement in new Func<PagedFile, StoreStatus>[]
                     {
                         s => StoreKit.Put(s, 3, "three", blobPages: 2),   // a WRITE that grows the file
                         s => StoreKit.Put(s, 1, "ONE"),                   // a REWRITE
                         s => StoreKit.Delete(s, 2),                       // a DELETE
                     })
            {
                h.FaultAtWrite(1, fault);
                Assert.Equal(expectedStatus, statement(a!));
                h.Heal();
                Assert.Equal("1=one;2=two;", StoreKit.Show(a!));
            }
            Assert.Equal(StoreStatus.Ok, StoreKit.Put(a!, 4, "four"));
            Assert.Equal(StoreStatus.Ok, a!.Close());
            var (_, opened, c) = StoreKit.OpenAs(medium, "C");
            Assert.Equal(StoreStatus.Ok, opened);
            Assert.Equal("1=one;2=two;4=four;", StoreKit.Show(c!));
            StoreKit.AssertR1(medium);
            StoreKit.AssertNoStampReused(medium);
        }
    }

    /// <summary>The host's own words for "the file cannot grow further" — <c>ERROR_FILE_TOO_LARGE</c> (223, the HRESULT
    /// 0x800700DF, negative and so classified on every host) and <c>EFBIG</c> (27, a raw errno only where the host reports
    /// errnos) — are the keyed boundary; the full-medium codes the sequential boundary also reads (<c>ERROR_DISK_FULL</c>,
    /// <c>ENOSPC</c>, the quotas) are not: a keyed store on a full medium answers '30' (2.2.1.9).</summary>
    [Fact]
    public void The_file_size_limit_is_the_narrower_half_of_the_medium_boundary()
    {
        Assert.True(HostFile.IsFileSizeLimit(new IOException("too large", unchecked((int)0x800700DF))));
        Assert.Equal(!OperatingSystem.IsWindows(), HostFile.IsFileSizeLimit(new IOException("errno", 27)));
        int quota = OperatingSystem.IsLinux() ? 122 : 69;
        foreach (int hr in new[] { unchecked((int)0x80070027), unchecked((int)0x80070070), unchecked((int)0x8007050F), 28, quota })
            Assert.False(HostFile.IsFileSizeLimit(new IOException("full", hr)), $"0x{hr:X8}");
        foreach (int hr in new[] { unchecked((int)0x800700DF), unchecked((int)0x80070070) })
            Assert.True(HostFile.IsMediumBoundary(new IOException("boundary", hr)), $"0x{hr:X8}");
    }
}
