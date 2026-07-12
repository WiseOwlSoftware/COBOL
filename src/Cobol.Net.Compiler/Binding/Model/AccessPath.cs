// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;

namespace CobolNet.Binding.Model;

/// <summary>The access direction of a subscripted OCCURS DYNAMIC element (data-model D9): a SENDING read (benign
/// scratch on out-of-range) vs a RECEIVING write (grows-and-seeds the table past its current capacity). A fixed
/// table ignores this — its <c>CobolTable.At</c> <c>ref T</c> serves both directions. It is resolved at RENDER time
/// (the backend picks the accessor from whether it is reading or writing), NOT baked into the segment.</summary>
public enum AccessDir { Sending, Receiving }

/// <summary>
/// A structural access path to a storage location (P7 Step 11 — structural <see cref="Place"/>): an ordered chain of
/// <see cref="AccessSegment"/>s the BACKEND renders to a C# lvalue expression (a field chain, table accessors), so a
/// <see cref="MemberPlace"/>/<see cref="RedefViewPlace"/>/<see cref="CapacityRegisterPlace"/> carries STRUCTURE, not
/// C# text. A table segment's subscript INDEX is the D10 TRANSITIONAL string carrier (a rendered index expression) —
/// it becomes a <c>BoundExpr</c> when PHASE 15 removes the SUBSCRIPT lexer mode (see the PHASE-07 Step 11 plan +
/// <c>project_phase04_d10_deferral</c>). The rendering lives in <c>CodeGen.PlaceRenderer.RenderPath</c>.
/// </summary>
public sealed record AccessPath(IReadOnlyList<AccessSegment> Segments)
{
    /// <summary>Extend the path with one trailing segment (used by the cursor walks in the binder).</summary>
    public AccessPath Add(AccessSegment seg) => new([.. Segments, seg]);

    /// <summary>Textually re-anchor the path behind a contained-program <c>__outer</c> chain (ISO §12.4.5.8.4 —
    /// a FILE STATUS item is never subscripted, so the root is a plain field): prepend <paramref name="prefix"/> to
    /// the ROOT field expression, byte-identical to the old <c>prefix + pathString</c>.</summary>
    public AccessPath Reroot(string prefix)
    {
        if (Segments.Count == 0 || Segments[0] is not RootFieldSegment root) return this;
        var segs = new List<AccessSegment>(Segments.Count) { new RootFieldSegment(prefix + root.CsField) };
        for (int i = 1; i < Segments.Count; i++) segs.Add(Segments[i]);
        return new AccessPath(segs);
    }

    /// <summary>True when any segment evaluates a subscript (a table access) — the structural replacement for the
    /// former <c>path.Contains("CobolTable.At(")</c> string-sniff (CORRESPONDING ref-vs-value anchoring).</summary>
    public bool HasIndex => Segments.Any(s => s is FixedTableSegment or DynTableSegment);
}

/// <summary>One step of an <see cref="AccessPath"/>.</summary>
public abstract record AccessSegment;

/// <summary>The root of the path — a static or instance C# field (or, after <see cref="AccessPath.Reroot"/>, a
/// contained-program <c>__outer</c>-prefixed root expression, or a CORRESPONDING anchor local).</summary>
public sealed record RootFieldSegment(string CsField) : AccessSegment;

/// <summary>A <c>.Member</c> access on the accumulated path (a nested <c>record struct</c> member).</summary>
public sealed record MemberSegment(string CsMember) : AccessSegment;

/// <summary>A FIXED OCCURS subscript — the accumulated path is wrapped in <c>CobolTable.At(path, index)</c>
/// (ISO §8.4.2.3.4 GR2, benign out-of-range). <paramref name="OneBasedIndex"/> is the D10 transitional index string.</summary>
public sealed record FixedTableSegment(string OneBasedIndex) : AccessSegment;

/// <summary>An OCCURS DYNAMIC subscript (§8.5.1.9.2/.9.3, D9) — the accessor is direction-specific
/// (<c>RefSending</c> on a read, <c>RefReceiving</c> on a write), chosen at RENDER time from the operation.
/// <paramref name="OneBasedIndex"/> is the D10 transitional index string.</summary>
public sealed record DynTableSegment(string OneBasedIndex) : AccessSegment;
