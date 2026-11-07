// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>A fixed-width window over character storage (<see cref="TextViews.Window"/>): <see cref="Base"/> is the
/// UNDERLYING carrier (never itself a window), <see cref="Trunc"/> the number of leading characters of it the
/// window can see (<see cref="int.MaxValue"/> for a window straight over the base), <see cref="Width"/> its width.</summary>
internal sealed record TextWindow(ManagedPointer<string> Base, int Trunc, int Width);

/// <summary>An ANY LENGTH view (<see cref="TextViews.AnyLength"/>): the full string, stores re-fitted to its length.</summary>
internal sealed record AnyLengthView(ManagedPointer<string> Base);

/// <summary>A dynamic-length view (<see cref="TextViews.Dynamic"/>): the full string, stores truncated at <see cref="Limit"/>.</summary>
internal sealed record DynamicView(ManagedPointer<string> Base, int Limit);

/// <summary>
/// The ONE builder of the character views a BY REFERENCE formal reads its argument through (ISO §2023:14.2.3 GR8: the
/// formal "operates as if" it occupies the argument's storage area; kb/Work PB4768; docs/INTERPROGRAM_DESIGN.md).
/// A view over a view never collapses on its own: a formal passed on BY REFERENCE through N activations would be a
/// chain of N closures, each access costing N delegate calls and a deep recursion exhausting the .NET stack outside
/// the activation-stack check (§2023:14.9.4.4 GR3 c) and h)). Every view here is built over the UNDERLYING storage and
/// composes the windows of the views it would have wrapped, so the depth is one however many activations pass the
/// formal on.
/// <para>The composition: a window of width w over a window (Trunc a, Width w1) reads
/// <c>Store(Store(B, w1), w)</c> = <c>Store(Trunc(B, min(a, w1)), w)</c>, and a store splices
/// <c>min(w, a, w1, length of B)</c> characters into B; a window of a window is one window over B with
/// <c>Trunc = min(a, w1)</c>.</para>
/// </summary>
internal static class TextViews
{
    /// <summary>The first <paramref name="width"/> characters of <paramref name="argument"/>'s storage (space padded) and
    /// a store that splices into them, collapsed over any window <paramref name="argument"/> already is.</summary>
    public static ManagedPointer<string> Window(ManagedPointer<string> argument, int width)
    {
        var (b, a) = argument.ViewTag is TextWindow t ? (t.Base, Math.Min(t.Trunc, t.Width)) : (argument, int.MaxValue);
        return ManagedPointer<string>.OverView(
            () =>
            {
                string? s = b.Value;
                return CobolString.Store(s is not null && a < s.Length ? s[..a] : s, width);
            },
            v =>
            {
                string? s = b.Value;
                b.Value = CobolString.WindowInto(s, 1, Math.Min(Math.Min(width, a), s?.Length ?? width), v);
            },
            new TextWindow(b, a, width));
    }

    /// <summary>The ANY LENGTH view (§2023:13.18.2 GR1): the full string, every store re-fitted to its length. A window and
    /// an ANY LENGTH view already show a fixed-length string and re-fit stores to it, so the view over either is
    /// that carrier itself.</summary>
    public static ManagedPointer<string> AnyLength(ManagedPointer<string> argument) =>
        argument.ViewTag is TextWindow or AnyLengthView
            ? argument
            : ManagedPointer<string>.OverView(
                () => argument.Value ?? "",
                v => argument.Value = CobolString.Store(v, argument.Value?.Length ?? 0),
                new AnyLengthView(argument));

    /// <summary>The dynamic-length view (§2023:13.18.19; §2023:8.5.1.10.4): the full string, stores truncated at
    /// <paramref name="limit"/>; over another dynamic view the limits compose by their minimum.</summary>
    public static ManagedPointer<string> Dynamic(ManagedPointer<string> argument, int limit)
    {
        var (b, l) = argument.ViewTag is DynamicView d ? (d.Base, Math.Min(d.Limit, limit)) : (argument, limit);
        return ManagedPointer<string>.OverView(
            () => b.Value ?? "",
            v => b.Value = CobolDynString.Store(v, l),
            new DynamicView(b, l));
    }
}
