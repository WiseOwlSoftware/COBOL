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

    /// <summary>
    /// Create a CobolDataPointer for BY REFERENCE passing.
    /// Used by CIL emitter to construct arguments for CALL USING.
    /// </summary>
    public static CobolDataPointer CreateByReference(byte[] buffer, int offset, int length)
    {
        return new CobolDataPointer(buffer, offset, length, default!);
    }

    /// <summary>
    /// Create a CobolDataPointer for BY CONTENT passing (copies the data).
    /// </summary>
    public static CobolDataPointer CreateByContent(byte[] buffer, int offset, int length)
    {
        var copy = new byte[length];
        Array.Copy(buffer, offset, copy, 0, length);
        return new CobolDataPointer(copy, 0, length, default!);
    }
}
