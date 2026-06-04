// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;

namespace CobolSharp.Runtime;

/// <summary>
/// Cheap argument-validity guards for the runtime support routines the emitted CIL calls. On a valid run
/// the check is the same comparison the CLR's own bounds check performs immediately afterwards, so it never
/// trips and the cost is a few predictably-not-taken integer comparisons; on a malformed
/// (buffer, offset, length) triple — a code-generation defect — it throws a diagnosable
/// <see cref="CobolRuntimeException"/> carrying the operation/target, instead of an opaque framework
/// exception with no COBOL context.
/// </summary>
public static class RuntimeGuard
{
    /// <summary>
    /// Validate that <paramref name="area"/> is non-null and that <c>[offset, offset+length)</c> lies
    /// within it. Throws <see cref="CobolRuntimeException"/> (RT0001) otherwise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Buffer(byte[] area, int offset, int length, string operation, string? target = null)
    {
        if (area is null || offset < 0 || length < 0 || (long)offset + length > area.Length)
            throw new CobolRuntimeException("RT0001", operation, target,
                $"buffer access out of range (offset={offset}, length={length}, bufferLength={area?.Length ?? 0})");
    }
}
