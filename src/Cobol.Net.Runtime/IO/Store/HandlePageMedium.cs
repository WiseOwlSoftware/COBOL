// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO.Store;

/// <summary>The production <see cref="IPageMedium"/>: positional reads and writes of one page per call through the
/// connector's OWN handle (<see cref="RandomAccess"/>), so the store travels through the §9.1.15 file lock and no second
/// handle is ever taken (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.8). It owns nothing: the connector opened
/// the handle and closes it.</summary>
internal sealed class HandlePageMedium(SafeFileHandle handle) : IPageMedium
{
    /// <inheritdoc/>
    public long PageCount => RandomAccess.GetLength(handle) / PageHeader.PageSize;

    /// <inheritdoc/>
    public void Read(uint page, Span<byte> destination)
    {
        var buffer = destination[..PageHeader.PageSize];
        int read = 0;
        while (read < buffer.Length)
        {
            int n = RandomAccess.Read(handle, buffer[read..], Offset(page) + read);
            if (n == 0) break;
            read += n;
        }
        // A page past the end of the medium (or the tail of a short one) reads as zeros, which no checksum accepts.
        buffer[read..].Clear();
    }

    /// <inheritdoc/>
    /// <remarks>The host's "the file cannot grow further" is the medium contract's <see cref="MediumSizeLimitException"/>
    /// (asked of <see cref="HostFile.IsFileSizeLimit"/>, the one place the host's codes are read); every other refusal
    /// travels as the host raised it.</remarks>
    public void Write(uint page, ReadOnlySpan<byte> source)
    {
        try { RandomAccess.Write(handle, source[..PageHeader.PageSize], Offset(page)); }
        catch (IOException e) when (HostFile.IsFileSizeLimit(e)) { throw new MediumSizeLimitException(e.Message, e); }
    }

    /// <inheritdoc/>
    public void Sync() => RandomAccess.FlushToDisk(handle);

    /// <inheritdoc/>
    public void SetPageCount(long pages) => RandomAccess.SetLength(handle, pages * PageHeader.PageSize);

    private static long Offset(uint page) => (long)page * PageHeader.PageSize;
}
