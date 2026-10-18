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

    /// <summary>A WRITE, a REWRITE and a DELETE whose page write the host refuses because the file cannot grow further:
    /// the boundary, the records as they were, and — the condition gone — the same run unit's next statement succeeds;
    /// the same refusal for any other reason (a full medium) is '30'.</summary>
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
