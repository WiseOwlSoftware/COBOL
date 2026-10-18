// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Tests.Unit.Store;

/// <summary>A REAL medium (a connector's handle, through <c>RelativeStore.MediumObserver</c>) whose run unit DIES after a
/// named write: every write handed over before the death is on the medium, in order, and nothing after it is — what a
/// process death leaves (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12: <i>"a process death keeps every write
/// handed to the operating environment, in order"</i>). The connector above it runs on in the test process, but none of
/// its later writes or syncs reach the file, so the file is exactly what the death left; the next run unit's OPEN reads
/// it through its own medium.</summary>
internal sealed class DyingPageMedium(IPageMedium inner) : IPageMedium
{
    /// <summary>The writes handed over so far (the dead run unit's dropped ones excluded).</summary>
    public int Writes { get; private set; }

    /// <summary>The run unit dies once this many writes have been handed over (never, by default).</summary>
    public int DieAfter { get; set; } = int.MaxValue;

    /// <summary>Whether the run unit is dead.</summary>
    public bool Dead => Writes >= DieAfter;

    public long PageCount => inner.PageCount;

    public void Read(uint page, Span<byte> destination) => inner.Read(page, destination);

    public void Write(uint page, ReadOnlySpan<byte> source)
    {
        if (Dead) return;
        inner.Write(page, source);
        Writes++;
    }

    public void Sync()
    {
        if (!Dead) inner.Sync();
    }

    public void SetPageCount(long pages)
    {
        if (!Dead) inner.SetPageCount(pages);
    }
}
