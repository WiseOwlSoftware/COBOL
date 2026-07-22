// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The EXIT PERFORM control-unwind signal for a Format-3 (exception-checking) PERFORM (ISO/IEC 1989:2023
/// §14.9.14.4 GR4 / §14.9.28.4 GR16): an <c>EXIT PERFORM</c> inside a WHEN / WHEN OTHER / WHEN COMMON handler
/// (imp-2/3/4) — which the emitter runs as a synthetic pc-range via a nested <c>__Dispatch</c> reached across the
/// C# call boundary of <c>__RunUse</c> → matcher → <c>__EcPerform</c> — cannot <c>goto</c> the inline PERFORM-end
/// label. It instead throws this signal, which unwinds the nested dispatcher frames back to the owning PERFORM
/// boundary, where <c>catch (ExitPerformSignal … when (Id == n))</c> lands control at the implicit CONTINUE
/// preceding FINALLY. <see cref="Id"/> disambiguates nested Format-3 PERFORMs. It is the sanctioned sibling of
/// <see cref="ResumeSignal"/>/<c>StopRun</c>/<c>ProgramReturn</c> — the exception-as-control family — NOT a second
/// dispatch mechanism. An <c>EXIT PERFORM</c> inside imperative-statement-1 or FINALLY (imp-5) is a plain
/// <c>goto</c>, never this signal.
/// </summary>
public sealed class ExitPerformSignal(int id) : Exception
{
    /// <summary>The <c>PerformId</c> of the owning Format-3 PERFORM whose boundary catches this signal.</summary>
    public int Id { get; } = id;
}
