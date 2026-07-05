// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// One argument (or the RETURNING slot) crossing a UNIVERSAL object-reference dispatch
/// (<see cref="CobolObject.__CobolInvoke"/>; OO deep-dive D10/D-U2). The <see cref="Descriptor"/> is the
/// caller-computed conformance descriptor (ONE encoding shared with the compile-time strict-conformance
/// rule — <c>OoClassTable.ConformanceDescriptor</c>, the D-U3 no-drift invariant): the generated callee
/// switch compares it for string equality and raises EC-OO-UNIVERSAL on any mismatch (ISO §14.9.23.4 GR7c —
/// conformance through a universal receiver is checked AT RUNTIME, §9.3.8.2.1 NOTE). The mutable
/// <see cref="Value"/> is the BY REFERENCE write-back channel — everything crossing a universal dispatch is
/// implicitly BY REFERENCE (§14.9.23.3 SR6): the caller boxes per its own crossing form, the callee unboxes
/// (descriptor equality makes the cast total), runs, and re-boxes; the caller copies out.
/// </summary>
public sealed class CobolInvokeArg(string descriptor, object? value = null)
{
    public string Descriptor { get; } = descriptor;
    public object? Value { get; set; } = value;
}
