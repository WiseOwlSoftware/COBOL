// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Collation;

namespace CobolNet.Runtime.IO.Store;

/// <summary>⛔ A TREE'S HIGH-WATER MARKS (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.4, the PAGE layout's bytes
/// 24..27 of a tree's ROOT page; kb/Work PB3332): the largest prefix <see cref="Deficit"/> g and the largest
/// <see cref="Overhang"/> c − 1 of any key the tree was given (<see cref="PrefixShape"/>, under the tree's
/// <see cref="IKeyOrder"/>), raised by every INSERT and never lowered by a DELETE, so they hold for every key the tree
/// holds. A partial START key of k positions under an order whose prefixes do not narrow is bounded by its first
/// <see cref="Margin"/> = k − g − (c − 1) level-1 weights. Both are 0 for an order whose prefixes narrow and for ordinary
/// text — every page but a root carries zeros — so the marks cost nothing where they are not asked.</summary>
internal readonly record struct TreeMarks(ushort Deficit, ushort Overhang)
{
    /// <summary>The marks of one key's <paramref name="shape"/>, saturated at the field's width (a larger mark only
    /// narrows the bound, so saturation stays sound).</summary>
    public static TreeMarks Of(PrefixShape shape) => new(Saturate(shape.Deficit), Saturate(shape.Overhang));

    /// <summary>The marks of a tree holding the keys of both.</summary>
    public TreeMarks Max(TreeMarks other) => new(Math.Max(Deficit, other.Deficit), Math.Max(Overhang, other.Overhang));

    /// <summary>How many of a <paramref name="prefixLength"/>-position partial key's level-1 weights bound the walk:
    /// k − g − (c − 1), never below 0.</summary>
    public int Margin(int prefixLength) => Math.Max(0, prefixLength - Deficit - Overhang);

    private static ushort Saturate(int value) => (ushort)Math.Clamp(value, 0, ushort.MaxValue);
}
