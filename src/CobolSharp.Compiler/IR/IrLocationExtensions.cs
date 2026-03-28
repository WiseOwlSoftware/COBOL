// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.IR;

/// <summary>
/// Extension methods for <see cref="IrLocation"/>.
/// </summary>
public static class IrLocationExtensions
{
    /// <summary>
    /// Get the <see cref="PicDescriptor"/> for an IrLocation (static, element, or ref-mod).
    /// For ref-mod locations the PIC of the underlying base is returned.
    /// </summary>
    public static PicDescriptor GetPic(this IrLocation loc)
    {
        return loc switch
        {
            IrCachedLocation c => c.Inner.GetPic(),
            IrStaticLocation s => s.Location.Pic,
            IrElementRef e => e.ElementPic,
            IrRefModLocation r => r.Base.GetPic(),
            _ => throw new InvalidOperationException($"Unknown IrLocation type: {loc.GetType().Name}")
        };
    }
}
