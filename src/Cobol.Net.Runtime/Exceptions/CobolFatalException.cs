// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The FATAL exception-condition termination signal (ISO/IEC 1989:2023 §14.6.13.1.3 #7: with checking enabled and
/// no handler that resumes, "execution of the run unit is terminated abnormally as specified in 14.6.12"). Thrown
/// by generated raise sites and runtime raise points; caught at the runtime run-unit boundary
/// (<see cref="ProgramTable.RunMain"/>) which writes the diagnostic to stderr and exits NONZERO — the settled §18.16
/// implementor choice (COBOLNET_DESIGN). Runtime-side (not the generated <c>Main</c>) so a fatal from ANY runtime
/// element reaches the surface, incl. a separately-compiled CALLed module. RunMain's finally then performs the
/// §14.6.11 attempt at the implicit-CLOSE part of termination. Also thrown — as the documented implementor choice
/// of §14.6.13.1.3 #8 (checking NOT enabled,
/// loud-failure doctrine §1.4) — for a RAISE of a fatal exception-name whose checking is off.
/// <para>NOT sealed (kb/Work PB75): <see cref="CobolSizeError"/> — the size error condition of §14.7.5, whose no-phrase
/// disposition is "processing proceeds as specified in 14.6.13.1.3" — is a fatal exception condition too, so it derives
/// from this type: an arithmetic statement's own <c>catch (CobolSizeError)</c> takes it first (the SIZE ERROR phrase /
/// the statement's EC-SIZE handling), and one that ESCAPES — from a condition, a DISPLAY operand, an argument, a
/// no-phrase statement — is dispatched by the statement guard like every other fatal EC, or reaches this boundary.</para>
/// </summary>
public class CobolFatalException(string ecName, string detail)
    : Exception($"{ecName} (fatal): {detail}")
{
    /// <summary>The level-3 exception-name (uppercase) that terminated the run unit.</summary>
    public string EcName { get; } = ecName.ToUpperInvariant();

    /// <summary>Set by the statement guard that PROCESSED this condition — the §14.9.49 F3 selection ran (a USE
    /// declarative / the enclosing PERFORM's WHEN, §14.6.13.1.3 #4/#5) and did not RESUME — before it rethrows for
    /// the abnormal termination (#7). Every ENCLOSING statement's guard lets a dispatched condition pass
    /// (<c>when (!Dispatched && …)</c>): the same raise must not be dispatched once per nesting level. Measured
    /// (kb/Work PB75): a fatal raise inside a PERFORM ran the USE declarative twice — once from the raising
    /// statement's guard, once from the PERFORM's — before terminating.</summary>
    public bool Dispatched { get; set; }
}
