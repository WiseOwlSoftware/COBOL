// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
        // (structural arms accrete here as subtypes migrate; the default is the temporary legacy shim)
        _ => p.Read(),
    };

    /// <summary>A C# statement (with trailing <c>;</c>) that stores <paramref name="rhs"/> into <paramref name="p"/>.</summary>
    public static string Write(Place p, string rhs) => p switch
    {
        _ => p.Write(rhs),
    };

    /// <summary>A figurative-constant store into a reference-modified slice (every targeted position takes the fill).</summary>
    public static string WriteFill(RefModPlace p, string fillChar) => p.WriteFill(fillChar);

    /// <summary>The SENDING character image of an occurs-depending GROUP operand (ISO §13.18.38 GR8 — the current-count part).</summary>
    public static string SendingImage(OdoGroupPlace p) => p.SendingImage();

    /// <summary>A receiving store over an occurs-depending GROUP operand's CURRENT extent (GR8a — depending-outside).</summary>
    public static string ReceiveInto(OdoGroupPlace p, string imageExpr) => p.ReceiveInto(imageExpr);

    /// <summary>The C# <c>int</c> expression for an occurs-depending group operand's current character extent (GR8).</summary>
    public static string LengthExpr(OdoGroupPlace p) => p.LengthExpr;
}
