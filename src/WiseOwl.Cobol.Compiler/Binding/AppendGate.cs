// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Passes;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The check that a per-unit resolve pass appends only to the fact sets it declared
/// (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2352). <c>BindPipeline.ValidateDag</c> checks a pass's declared <see cref="PassViews"/> against the
/// phase order; this gate checks the declaration against what the pass DOES: <see cref="DataBinder.BindResolve"/> opens
/// the gate around each pass's <see cref="IBindPass.Run"/>, and a <see cref="FactSet{T}"/> appended to while a gate is open
/// for a pass that did not declare it throws. Outside a pass (the entry walk, the procedure division, the group tail)
/// nothing is open and nothing is checked: those appenders are declared by the group passes of
/// <c>BindPipeline.GroupTail</c> and checked by <c>ValidateDag</c> alone.
/// </summary>
public sealed class AppendGate
{
    private IPassInfo? _running;

    /// <summary>Open the gate for <paramref name="pass"/>; disposing the result closes it.</summary>
    internal Scope Running(IPassInfo pass)
    {
        _running = pass;
        return new Scope(this);
    }

    /// <summary>Called by <see cref="FactSet{T}.Add"/>: refuse an append the running pass did not declare.</summary>
    internal void Require(FactSetKind kind)
    {
        if (_running is { } pass && !pass.Views.AppendsTo.Contains(kind))
            throw new InvalidOperationException(
                $"BindPipeline views violation: pass '{pass.Name}' appended to {kind} but does not declare it in Views "
                + "(kb/Work PB2352)");
    }

    /// <summary>The open gate; disposing it closes the gate.</summary>
    internal readonly struct Scope(AppendGate gate) : IDisposable
    {
        public void Dispose() => gate._running = null;
    }
}
