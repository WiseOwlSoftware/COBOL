// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// A reference to a contiguous byte range in COBOL storage.
/// Used for CALL parameter passing:
/// - BY REFERENCE: points directly into the caller's WorkingStorage byte[]
/// - BY CONTENT: points into a private copy of the argument bytes
/// - BY VALUE: points into a temporary holding the encoded value
/// </summary>
public readonly record struct CobolDataPointer(
    byte[] Buffer,
    int Offset,
    int Length,
    PicDescriptor Pic)
{
    /// <summary>True if this pointer has valid storage (non-null buffer).</summary>
    public bool IsValid => Buffer != null;

    /// <summary>Create a BY CONTENT copy of this pointer's data.</summary>
    public CobolDataPointer CopyForByContent()
    {
        var copy = new byte[Length];
        Array.Copy(Buffer, Offset, copy, 0, Length);
        return new CobolDataPointer(copy, 0, Length, Pic);
    }
}
