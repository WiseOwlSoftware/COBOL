// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The method-return signal (ISO §14.9.18.4 GR4 — GOBACK within a method terminates the executing METHOD and
/// returns control to the INVOKE site; OO deep-dive D8): a method-context GOBACK (and, in pre-2023 editions,
/// EXIT METHOD — removed by 2023, Annex E.2) raises it; the emitted method's public entry catches it and
/// returns normally, delivering the method's RETURNING item (if any) as the invocation result. The exception
/// form (rather than a plain <c>return</c> out of the PC loop) is required for the same reason as
/// <see cref="ProgramReturn"/>: a GOBACK inside an out-of-line PERFORM executes in a NESTED bounded
/// <c>__Dispatch</c> frame, which a <c>return</c> would exit one frame at a time — the signal unwinds all of
/// them to the method boundary at once. Distinct from <see cref="ProgramReturn"/> (a CALLed program's return —
/// caught at <c>__Activate</c>, never at a method entry) and from <see cref="StopRun"/> (STOP RUN inside a
/// method still terminates the whole run unit, §14.9.43 — it must NOT be caught at the method boundary).
/// </summary>
public sealed class MethodReturn : Exception;
