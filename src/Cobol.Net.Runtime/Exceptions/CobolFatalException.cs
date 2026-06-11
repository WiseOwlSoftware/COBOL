// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The FATAL exception-condition termination signal (ISO/IEC 1989:2023 §14.6.13.1.3 #7: with checking enabled and
/// no handler that resumes, "execution of the run unit is terminated abnormally as specified in 14.6.12"). Thrown
/// by generated raise sites and runtime raise points; caught at the generated run-unit entry (<c>Main</c>) which
/// writes the diagnostic to stderr and exits NONZERO — the settled §18.16 implementor choice (COBOLNET_DESIGN).
/// The <c>finally CobolFile.CloseAll()</c> already performs the §14.6.11 attempt at the implicit-CLOSE part of
/// termination. Also thrown — as the documented implementor choice of §14.6.13.1.3 #8 (checking NOT enabled,
/// loud-failure doctrine §1.4) — for a RAISE of a fatal exception-name whose checking is off.
/// </summary>
public sealed class CobolFatalException(string ecName, string detail)
    : Exception($"{ecName} (fatal): {detail}")
{
    /// <summary>The level-3 exception-name (uppercase) that terminated the run unit.</summary>
    public string EcName { get; } = ecName.ToUpperInvariant();
}
