// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Tests.Unit.Store;

/// <summary>A REAL medium (a connector's handle, through <c>KeyedStore.MediumObserver</c>) that refuses the writes and
/// syncs a case names — the host's answers a real file cannot be made to give on demand (a full medium, a file at its
/// maximum size, a refused flush): docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12's fault injection at the
/// connector's level. A refused write or sync changes nothing on the medium. <paramref name="read"/> observes every page
/// read before it is made (a case asks there what another run unit could see at that moment, kb/Work PB3335).</summary>
internal sealed class RefusingPageMedium(IPageMedium inner, Func<uint, Exception?>? write = null, Func<Exception?>? sync = null,
    Action<uint>? read = null)
    : IPageMedium
{
    /// <summary>The page writes handed to the inner medium so far.</summary>
    public int Writes { get; private set; }

    /// <summary>The page reads made through the inner medium so far.</summary>
    public int Reads { get; private set; }

    public long PageCount => inner.PageCount;

    public void Read(uint page, Span<byte> destination)
    {
        read?.Invoke(page);
        inner.Read(page, destination);
        Reads++;
    }

    public void Write(uint page, ReadOnlySpan<byte> source)
    {
        if (write?.Invoke(page) is { } refusal) throw refusal;
        inner.Write(page, source);
        Writes++;
    }

    public void Sync()
    {
        if (sync?.Invoke() is { } refusal) throw refusal;
        inner.Sync();
    }

    public void SetPageCount(long pages) => inner.SetPageCount(pages);
}
