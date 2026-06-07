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
public readonly record struct ManagedPointer(
    byte[] Buffer,
    int Offset,
    int Length,
    PicDescriptor Pic)
{
    /// <summary>True if this pointer has valid storage (non-null buffer).</summary>
    public bool IsValid => Buffer != null;

    /// <summary>Create a BY CONTENT copy of this pointer's data.</summary>
    public ManagedPointer CopyForByContent()
    {
        var copy = new byte[Length];
        Array.Copy(Buffer, Offset, copy, 0, Length);
        return new ManagedPointer(copy, 0, Length, Pic);
    }

    /// <summary>
    /// Create a ManagedPointer for BY REFERENCE passing.
    /// Used by CIL emitter to construct arguments for CALL USING.
    /// </summary>
    public static ManagedPointer CreateByReference(byte[] buffer, int offset, int length)
    {
        return new ManagedPointer(buffer, offset, length, default!);
    }

    /// <summary>
    /// Create a ManagedPointer for BY CONTENT passing (copies the data).
    /// </summary>
    public static ManagedPointer CreateByContent(byte[] buffer, int offset, int length)
    {
        var copy = new byte[length];
        Array.Copy(buffer, offset, copy, 0, length);
        return new ManagedPointer(copy, 0, length, default!);
    }

    /// <summary>
    /// Obtain dynamic storage for the ALLOCATE statement (ISO §14.9.3): a managed <c>byte[length]</c> wrapped in a
    /// ManagedPointer. Per GR2, a non-positive length yields the predefined null address (<c>default</c>). The
    /// backing array is GC-managed, so FREE simply drops the reference (sets the pointer to NULL) — there is no
    /// native heap and no manual deallocation. New <c>byte[]</c> is zero-initialized, satisfying ALLOCATE … INITIALIZED.
    /// </summary>
    public static ManagedPointer Allocate(int length) =>
        length <= 0 ? default : new ManagedPointer(new byte[length], 0, length, default!);
}
