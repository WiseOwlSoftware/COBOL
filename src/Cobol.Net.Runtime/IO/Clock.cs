// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.IO;

/// <summary>The run unit's clock (DESIGN-runtime-library §2.7 — replaces the process-global mutable
/// <c>AcceptSource.Now</c> seam): every ACCEPT temporal source reads <c>RunUnit.Current.Clock.Now()</c>
/// (ISO §14.9.1.4 GR7 — "the hardware clock provides the current date and time"). Injectable per run unit
/// (<c>RunUnit.Current.Clock = new FixedClock(...)</c> in an in-process test).</summary>
public interface IClock
{
    /// <summary>The current local date-time reading.</summary>
    DateTime Now();
}

/// <summary>The default clock: consults the <c>COBOLNET_CLOCK</c> environment variable (an invariant-culture
/// date-time, e.g. <c>2026-06-10T14:30:45.67</c>) so a test run can pin the clock ACROSS PROCESSES — the
/// deterministic-clock path the temporal conformance goldens use — falling back to the local system clock.</summary>
public sealed class SystemClock : IClock
{
    /// <summary>The shared instance (stateless — legitimately static).</summary>
    public static readonly SystemClock Instance = new();

    /// <inheritdoc/>
    public DateTime Now() =>
        Environment.GetEnvironmentVariable("COBOLNET_CLOCK") is { Length: > 0 } pin
        && DateTime.TryParse(pin, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime pinned)
            ? pinned
            : DateTime.Now;
}
