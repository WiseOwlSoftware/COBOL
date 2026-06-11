// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The RESUME control-unwind signal (ISO/IEC 1989:2023 §14.9.33): a RESUME statement inside a USE declarative
/// throws it; the generated <c>__RunUse</c> catches it and RETURNS the resume action to the raise site — the
/// exception-as-control pattern of <c>StopRun</c>/<c>ProgramReturn</c> (the as-built mapping of the deep-dive's
/// original "declarative returns a ResumeAction" sketch: declaratives are pc ranges run by the bounded
/// dispatcher, not methods — recorded in COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN). The unwind crosses any nested
/// PERFORM <c>__Dispatch</c> frames inside the declarative; the §14.9.33.4 GR3 NOTE 2 caveat (abandoned PERFORM
/// flow is undefined) is inherited deliberately.
/// </summary>
public sealed class ResumeSignal(int targetPc) : Exception
{
    /// <summary><c>RESUME AT NEXT STATEMENT</c> (§14.9.33.4 GR2): control transfers to the implicit CONTINUE
    /// after the statement that raised the condition — the raise site falls through (suppressing a fatal
    /// termination, §14.6.13.1.3 #5 NOTE 2).</summary>
    public const int NextStatement = -2;

    /// <summary>The resume target: a nondeclarative pc (<c>RESUME AT procedure-name</c> ≡ GO TO, GR3), or
    /// <see cref="NextStatement"/>.</summary>
    public int TargetPc { get; } = targetPc;
}
