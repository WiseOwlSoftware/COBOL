// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The views of <see cref="DataBindState"/> one pass declares (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1),
/// kb/Work PB2352): which append-only fact sets it appends to (<see cref="IAppends{T}"/>), reads as "recorded so far"
/// (<see cref="IReadsEarly{T}"/>) and reads whole (<see cref="IReads{T}"/>). <c>BindPipeline.ValidateDag</c> checks the
/// declaration against the phase order and <see cref="FactSetContract"/>; <see cref="AppendGate"/> refuses, while a
/// per-unit resolve pass runs, an append the pass did not declare. A pass that touches no fact set declares
/// <see cref="None"/>.
/// </summary>
public readonly record struct PassViews
{
    /// <summary>No fact set.</summary>
    public static PassViews None => default;

    /// <summary>The fact sets the pass appends to.</summary>
    public ImmutableArray<FactSetKind> AppendsTo { get => field.IsDefault ? [] : field; init; }

    /// <summary>The fact sets the pass reads as "recorded so far".</summary>
    public ImmutableArray<FactSetKind> ReadsEarly { get => field.IsDefault ? [] : field; init; }

    /// <summary>The fact sets the pass reads whole.</summary>
    public ImmutableArray<FactSetKind> ReadsWhole { get => field.IsDefault ? [] : field; init; }

    /// <summary>Views of a pass that only appends to <paramref name="kinds"/>.</summary>
    public static PassViews Appends(params ReadOnlySpan<FactSetKind> kinds) => new() { AppendsTo = [.. kinds] };

    /// <summary>Views of a pass that appends to, reads early and reads whole the named sets.</summary>
    public static PassViews Of(ReadOnlySpan<FactSetKind> appends, ReadOnlySpan<FactSetKind> readsEarly,
        ReadOnlySpan<FactSetKind> readsWhole) =>
        new() { AppendsTo = [.. appends], ReadsEarly = [.. readsEarly], ReadsWhole = [.. readsWhole] };
}
