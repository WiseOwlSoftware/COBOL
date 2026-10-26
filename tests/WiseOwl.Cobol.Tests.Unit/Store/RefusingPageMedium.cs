// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO.Store;

namespace WiseOwl.Cobol.Tests.Unit.Store;

/// <summary>A REAL medium (a connector's handle, through <c>KeyedStore.MediumObserver</c>) that refuses the reads, writes
/// and syncs a case names — the host's answers a real file cannot be made to give on demand (a full medium, a file at its
/// maximum size, a refused flush, a read the device fails): docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.12's fault
/// injection at the connector's level. A refused write or sync changes nothing on the medium, and a refused read hands
/// nothing back. Each seam is called before its operation is made, so it also observes it (a case asks there what another
/// run unit could see at that moment, kb/Work PB3335), and answers the exception the host would throw, or null.</summary>
internal sealed class RefusingPageMedium(IPageMedium inner, Func<uint, Exception?>? write = null, Func<Exception?>? sync = null,
    Func<uint, Exception?>? read = null, Func<Exception?>? truncate = null)
    : IPageMedium
{
    /// <summary>The page writes handed to the inner medium so far.</summary>
    public int Writes { get; private set; }

    /// <summary>The page reads made through the inner medium so far.</summary>
    public int Reads { get; private set; }

    /// <summary>A medium that refuses its <paramref name="refuseAt"/>-th write or sync (counted together, from the
    /// medium's creation, as <paramref name="seen"/> reads the count) with a host <see cref="IOException"/>, and calls
    /// <paramref name="count"/> for every write and sync handed to it: the seam of 2.2.1.16's refused OPEN and CLOSE rows
    /// through a connector (both organizations' engine tests).</summary>
    public static RefusingPageMedium AtOperation(IPageMedium inner, Func<int> refuseAt, Action count, Func<int> seen)
    {
        IOException? Next()
        {
            bool refuse = seen() == refuseAt();
            count();
            return refuse ? new IOException("refused by the host") : null;
        }
        return new(inner, write: _ => Next(), sync: Next);
    }

    public long Length => inner.Length;

    public void Read(uint page, Span<byte> destination)
    {
        if (read?.Invoke(page) is { } refusal) throw refusal;
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

    public void SetLength(long bytes)
    {
        if (truncate?.Invoke() is { } refusal) throw refusal;
        inner.SetLength(bytes);
    }
}
