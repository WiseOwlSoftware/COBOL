// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO.Store;

/// <summary>The medium a keyed store's pages live on (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12): read
/// page n, write page n, sync, and the page count, over <see cref="PageHeader.PageSize"/>-byte pages. The production
/// medium is <see cref="HandlePageMedium"/> (positional I/O through the connector's own handle); the unit tests' medium
/// is an in-memory page array that logs every write and sync and can lose, keep, tear or age them, so every crash point
/// of §2.2.1.16 runs deterministically on every host.
/// <para>⛔ A HOST REFUSAL IS AN <see cref="IOException"/> (or <see cref="UnauthorizedAccessException"/>): a full medium,
/// a quota, a revoked handle, a failed sync. <see cref="PagedFile"/> answers it as §9.1.13.6 item 1's '30' at the
/// statement it refused; nothing else is caught, so a fault no medium can report is never mistaken for one.</para></summary>
internal interface IPageMedium
{
    /// <summary>The pages the medium holds: its length over the page size.</summary>
    long PageCount { get; }

    /// <summary>Read page <paramref name="page"/> into <paramref name="destination"/> (one page); a page at or past
    /// <see cref="PageCount"/> reads as zeros, which no checksum accepts.</summary>
    void Read(uint page, Span<byte> destination);

    /// <summary>Write one page at <paramref name="page"/>, extending the medium when the page lies past its end. The
    /// write is HANDED to the operating environment: a process death keeps it, a power loss before the next
    /// <see cref="Sync"/> may not (§2.2.1.2).</summary>
    void Write(uint page, ReadOnlySpan<byte> source);

    /// <summary>Make every write handed over so far durable (<c>FlushFileBuffers</c> / <c>fsync</c>).</summary>
    void Sync();

    /// <summary>Set the medium's length to <paramref name="pages"/> pages — the one truncation: <c>OPEN OUTPUT</c>'s
    /// creation empties the medium before it writes the three meta pages of the new store (§14.9.27.4 GR18).</summary>
    void SetPageCount(long pages);
}
