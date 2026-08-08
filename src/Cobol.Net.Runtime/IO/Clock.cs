// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.IO;

/// <summary>The run unit's clock (DESIGN-runtime-library §2.7 — replaces the process-global mutable
/// <c>AcceptSource.Now</c> seam): every ACCEPT temporal source reads <c>RunUnit.Current.Clock.Now()</c>
/// (ISO §14.9.1.4 GR7 — "the hardware clock provides the current date and time"), and every now-intrinsic
/// (CURRENT-DATE, FORMATTED-CURRENT-DATE, SECONDS-PAST-MIDNIGHT, the windowing execution-time defaults)
/// reads the SAME seam, so one run unit observes one clock (§15.21.1 — the "local time differential factor
/// provided by the system on which the function is evaluated"; §15.38.4 r1). Injectable per run unit
/// (<c>RunUnit.Current.Clock = new FixedClock(...)</c> in an in-process test). <c>ClockSeamDriftTests</c>
/// is the source-form guard: no direct <c>DateTime[Offset].Now</c> read survives outside
/// <see cref="SystemClock"/> and the WHEN-COMPILED compile-time capture.</summary>
public interface IClock
{
    /// <summary>The current local wall-clock reading WITH its offset from UTC — the local time differential
    /// factor CURRENT-DATE renders in positions 17–21 (§15.21.3 r1) and the §15.3.3 offset format fields emit.</summary>
    DateTimeOffset Now();
}

/// <summary>The default clock: consults the <c>COBOLNET_CLOCK</c> environment variable (an invariant-culture
/// date-time, e.g. <c>2026-06-10T14:30:45.67</c>, optionally carrying an explicit UTC offset,
/// <c>2026-06-10T14:30:45.67+02:30</c> — without one the machine-local offset is assumed) so a test run can
/// pin the clock ACROSS PROCESSES — the deterministic-clock path the temporal conformance goldens use —
/// falling back to the local system clock.</summary>
public sealed class SystemClock : IClock
{
    /// <summary>The shared instance (stateless — legitimately static).</summary>
    public static readonly SystemClock Instance = new();

    /// <inheritdoc/>
    public DateTimeOffset Now() =>
        Environment.GetEnvironmentVariable("COBOLNET_CLOCK") is { Length: > 0 } pin
        && DateTimeOffset.TryParse(pin, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset pinned)
            ? pinned
            : DateTimeOffset.Now;
}
