// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO.Store;

namespace CobolNet.Tests.Unit.Store;

/// <summary>The pages of ONE physical file held in memory, the latest version of each and nothing else, so a case can
/// load tens of thousands of records through the real connectors without the host's per-page cost and without a write
/// log (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.11's witnesses count page I/O; they never time it).
/// <see cref="MemoryPageMedium"/> keeps every version of every page, which the crash cases need and a load of this size
/// cannot afford (gigabytes); this medium keeps only what a reader sees. Every run
/// unit's store on the file reads and writes it through its own <see cref="View"/>
/// (<c>KeyedStore.MediumObserver</c> hands each store one), so a second run unit's OPEN sees exactly the pages the first left: the
/// FILE is the truth across run units (2.2.1.7). The host file the connectors open still carries their handles and locks;
/// only the pages live here.</summary>
internal sealed class PageArrayMedium
{
    private readonly List<byte[]?> _pages = [];

    /// <summary>A new view for one run unit's store.</summary>
    public View Open() => new(this);

    /// <summary>One run unit's medium on the file. <see cref="Die"/> is a process death (2.2.1.12: <i>"a process death
    /// keeps every write handed to the operating environment, in order"</i>): every write handed over before it stays,
    /// and nothing after it reaches the file. <see cref="RefuseWrite"/>, when it answers an exception for a page, is the
    /// host refusing that write (a full medium): nothing changes.</summary>
    internal sealed class View(PageArrayMedium file) : IPageMedium
    {
        /// <summary>The host refuses the write of a page when this answers an exception for it.</summary>
        public Func<uint, Exception?>? RefuseWrite { get; set; }

        /// <summary>Whether the run unit is dead.</summary>
        public bool Dead { get; private set; }

        /// <summary>The run unit dies now.</summary>
        public void Die() => Dead = true;

        public long PageCount => file._pages.Count;

        public void Read(uint page, Span<byte> destination)
        {
            var buffer = destination[..PageHeader.PageSize];
            if (page < file._pages.Count && file._pages[(int)page] is { } bytes) bytes.CopyTo(buffer);
            else buffer.Clear();
        }

        public void Write(uint page, ReadOnlySpan<byte> source)
        {
            if (Dead) return;
            if (RefuseWrite?.Invoke(page) is { } refusal) throw refusal;
            while (file._pages.Count <= page) file._pages.Add(null);
            file._pages[(int)page] = source[..PageHeader.PageSize].ToArray();
        }

        public void Sync() { }

        public void SetPageCount(long pages)
        {
            if (Dead) return;
            if (pages < file._pages.Count) file._pages.RemoveRange((int)pages, file._pages.Count - (int)pages);
            while (file._pages.Count < pages) file._pages.Add(null);
        }
    }
}
