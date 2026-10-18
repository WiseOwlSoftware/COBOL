// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Buffers.Binary;

namespace CobolNet.Runtime.IO.Store;

/// <summary>A POINTER (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.4): a page number and the generation of the
/// write it names (§2.2.1.6 R3), 12 bytes on the page. Page 0 is a committed meta slot and is never a tree page, so the
/// pointer to page 0 is <see cref="None"/>.</summary>
internal readonly record struct PagePointer(uint Page, ulong Generation)
{
    /// <summary>The pointer's size on the page.</summary>
    public const int Bytes = 12;

    /// <summary>No page: an empty tree's root.</summary>
    public static PagePointer None => default;

    /// <summary>Whether this is <see cref="None"/>.</summary>
    public bool IsNone => Page == 0;

    /// <summary>Encode at <paramref name="at"/>.</summary>
    public void Write(Span<byte> at)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(at, Page);
        BinaryPrimitives.WriteUInt64LittleEndian(at[4..], Generation);
    }

    /// <summary>Decode from <paramref name="at"/>.</summary>
    public static PagePointer Read(ReadOnlySpan<byte> at) =>
        new(BinaryPrimitives.ReadUInt32LittleEndian(at), BinaryPrimitives.ReadUInt64LittleEndian(at[4..]));
}
