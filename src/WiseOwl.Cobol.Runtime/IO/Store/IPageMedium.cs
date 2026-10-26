// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO.Store;

/// <summary>The medium a keyed store's pages live on (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.12): read
/// page n, write page n, sync, and the length, over <see cref="PageHeader.PageSize"/>-byte pages. The production
/// medium is <see cref="HandlePageMedium"/> (positional I/O through the connector's own handle); the unit tests' medium
/// is an in-memory page array that logs every write and sync and can lose, keep, tear or age them, so every crash point
/// of 2.2.1.16 runs deterministically on every host.
/// <para>⛔ A HOST REFUSAL IS AN <see cref="IOException"/> (or <see cref="UnauthorizedAccessException"/>): a full medium,
/// a quota, a revoked handle, a failed read or sync. <see cref="PagedFile"/> answers it as §9.1.13.6 item 1's '30' at the
/// statement it refused; nothing else is caught, so a fault no medium can report is never mistaken for one. The one
/// refusal with another answer is a write the host refuses because the FILE cannot grow further: the medium reports it
/// as <see cref="MediumSizeLimitException"/>, and a changing statement it refuses answers the boundary ('24', 2.2.1.9).</para></summary>
internal interface IPageMedium
{
    /// <summary>The medium's length in BYTES. A store's file is always a whole number of pages; the file an
    /// <c>OPEN OUTPUT</c> creates over need not be (a format-5 whole store, any foreign file), and a refused creation
    /// gives it back exactly this length (§14.9.27.4 GR25).</summary>
    long Length { get; }

    /// <summary>Read page <paramref name="page"/> into <paramref name="destination"/> (one page); every byte at or past
    /// <see cref="Length"/> reads as zero, so a page wholly past the end fails every checksum.</summary>
    void Read(uint page, Span<byte> destination);

    /// <summary>Write one page at <paramref name="page"/>, extending the medium when the page lies past its end. The
    /// write is HANDED to the operating environment: a process death keeps it, a power loss before the next
    /// <see cref="Sync"/> may not (2.2.1.2).</summary>
    void Write(uint page, ReadOnlySpan<byte> source);

    /// <summary>Make every write handed over so far durable (<c>FlushFileBuffers</c> / <c>fsync</c>).</summary>
    void Sync();

    /// <summary>Set the medium's length to <paramref name="bytes"/> — the one truncation, <c>OPEN OUTPUT</c>'s: the
    /// created store's three pages after its commit point (§14.9.27.4 GR18), or a refused creation's previous length
    /// (GR25). The bytes cut off are gone: a later write past them leaves zeros in between.</summary>
    void SetLength(long bytes);
}

/// <summary>The host refused a page write because the file has reached the largest size the host allows
/// (<c>EFBIG</c>, <c>ERROR_FILE_TOO_LARGE</c>: <see cref="HostFile.IsFileSizeLimit"/>) — the boundary of a relative or
/// indexed file (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.9; §9.1.13.5 item 4's '24'), which a full medium is
/// not. An <see cref="IOException"/>, so every refusal arm still sees it as a refusal.</summary>
internal sealed class MediumSizeLimitException(string message, Exception? inner = null) : IOException(message, inner);
