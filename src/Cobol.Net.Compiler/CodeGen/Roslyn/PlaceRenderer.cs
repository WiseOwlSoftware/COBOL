// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

/// <summary>
/// The Roslyn backend's sole renderer of a <see cref="Place"/> lvalue to C# read/write text
/// (DESIGN-codegen-backend §2.3). Keeping ALL C# text on the backend side of the bind→emit seam is what makes the
/// bound tree backend-neutral (the G4 invariant): a <see cref="Place"/> carries STRUCTURE (an access path, subscript
/// <c>BoundExpr</c>s, a ref-mod span, resolved <see cref="DataItem"/>s), never call text, so a future CIL backend can
/// consume the same tree. Every runtime call this renderer emits routes through <see cref="RuntimeApi"/>.
/// <para>Rendering is context-free (it reads only the <see cref="Place"/>'s own structure + <see cref="RuntimeApi"/>),
/// so this is a <c>static</c> class like <see cref="RuntimeApi"/> — no <c>EmitContext</c> is threaded. It lives in
/// <c>CodeGen</c>, never <c>Binding</c>: the binder produces the structural <see cref="Place"/>, the backend renders
/// it (the binder never references this type — that would invert the layering the seam exists to enforce).</para>
/// <para><b>Migration (P7 Step 11, subtype-at-a-time).</b> Consumers are routed through <see cref="Read"/>/
/// <see cref="Write"/> FIRST (each routing is byte-identical — the shim below forwards to the legacy
/// <see cref="Place.Read"/>/<see cref="Place.Write"/>); THEN each <see cref="Place"/> subtype is converted to
/// structure one at a time, its rendering moving into an explicit arm here while the still-string subtypes keep
/// delegating. When every subtype and consumer is migrated, <see cref="Place.Read"/>/<see cref="Place.Write"/> are
/// deleted and the neutrality reflection test (DESIGN-codegen-backend §6 R5) locks the invariant.</para>
/// </summary>
internal static class PlaceRenderer
{
    /// <summary>A C# expression that reads <paramref name="p"/>'s current value.</summary>
    public static string Read(Place p) => p switch
    {
        // A level-66 RENAMES alias (ISO §13.18.45): concatenate the spanned leaves' character images.
        RenamesPlace n => n.Leaves.Count == 1
            ? Read(n.Leaves[0])
            : "(" + string.Join(" + ", n.Leaves.Select(Read)) + ")",
        // An ODO group operand read plainly (not as a GR8 slice) is the struct lvalue — the inner member place.
        OdoGroupPlace o => Read(o.Inner),
        // (more structural arms accrete here as subtypes migrate; the default is the temporary legacy shim)
        _ => p.Read(),
    };

    /// <summary>A C# statement (with trailing <c>;</c>) that stores <paramref name="rhs"/> into <paramref name="p"/>.</summary>
    public static string Write(Place p, string rhs) => p switch
    {
        RenamesPlace n => WriteRenames(n, rhs),
        OdoGroupPlace o => Write(o.Inner, rhs),
        _ => p.Write(rhs),
    };

    /// <summary>Store into a multi-leaf RENAMES alias (ISO §13.18.45): store the value at the span width, then
    /// distribute the slices back into the leaves left to right (a write through the alias shows through every
    /// renamed item and vice versa — no second storage).</summary>
    private static string WriteRenames(RenamesPlace n, string rhs)
    {
        if (n.Leaves.Count == 1) return Write(n.Leaves[0], rhs);
        int width = n.Leaves.Sum(l => l.Item.ImageWidth);
        var sb = new System.Text.StringBuilder();
        sb.Append($"{{ string __ren = {RuntimeApi.StrStore(rhs, width.ToString())};");
        int off = 0;
        foreach (var l in n.Leaves)
        {
            int w = l.Item.ImageWidth;
            sb.Append(' ').Append(Write(l, $"__ren.Substring({off}, {w})"));
            off += w;
        }
        return sb.Append(" }").ToString();
    }

    /// <summary>A figurative-constant store into a reference-modified slice (every targeted position takes the fill).</summary>
    public static string WriteFill(RefModPlace p, string fillChar) => p.WriteFill(fillChar);

    /// <summary>The SENDING character image of an occurs-depending GROUP operand (ISO §13.18.38 GR8 — only the
    /// current-count part: the maximum image truncated to the current extent, a prefix by SR22).</summary>
    public static string SendingImage(OdoGroupPlace p) => $"{Read(p.Inner)}.AsImage().Substring(0, {LengthExpr(p)})";

    /// <summary>A receiving store over an occurs-depending GROUP operand's CURRENT extent (GR8a — depending-outside):
    /// splice the stored prefix over the live image, leaving positions past the count unmodified.</summary>
    public static string ReceiveInto(OdoGroupPlace p, string imageExpr) =>
        $"{Read(p.Inner)}.FromImage({RuntimeApi.StrSpliceInto($"{Read(p.Inner)}.AsImage()", "1", LengthExpr(p), imageExpr)});";

    /// <summary>The C# <c>int</c> expression for an occurs-depending group operand's current character extent (GR8):
    /// the fixed prefix plus data-name-1's clamped value × the element width, read at the operation site.</summary>
    public static string LengthExpr(OdoGroupPlace p) =>
        RuntimeApi.TableOdoExtent(RuntimeApi.TableOcc(Read(p.Depending)), p.MaxOccurs, p.FixedChars, p.ElemChars);
}
