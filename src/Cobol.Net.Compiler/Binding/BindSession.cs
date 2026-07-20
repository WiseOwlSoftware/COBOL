// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

/// <summary>The shared state of ONE group bind (rearch PHASE-06 Step 2): built by <see cref="BinderDriver.Bind"/>
/// and handed to the <see cref="Compiler.Oo.OoDriver"/> + the per-unit binds so both sides of the driver's per-unit binds draw
/// from the SAME uid-band sequence and fold the SAME compile-time TurnState — exactly as the fused
/// <c>CallBindRunUnit</c> did with instance fields.</summary>
internal sealed class BindSession
{
    public required TurnState Turn { get; init; }
    public required OoClassTable OoClasses { get; init; }
    public required EditionContext Edition { get; init; }

    /// <summary>The group's compile-time <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> resolution (ISO §7.3.23) — the per-line
    /// zero-length allowance fold every unit's <see cref="ReferenceResolver"/> queries when building a ref-mod
    /// Place. Defaults to <see cref="RefModZeroLengthState.Empty"/> (the OFF default, no directive).</summary>
    public RefModZeroLengthState RefModZeroLength { get; init; } = RefModZeroLengthState.Empty;

    private int _uidBand;

    /// <summary>Take the next disjoint 100k uid band (one per DataBinder, so nested-class struct/profile names
    /// never shadow a container's — the band discipline of the fused pipeline's <c>_callUidBand</c>).</summary>
    public int TakeUidBand()
    {
        int band = _uidBand;
        _uidBand += 100_000;
        return band;
    }
}
