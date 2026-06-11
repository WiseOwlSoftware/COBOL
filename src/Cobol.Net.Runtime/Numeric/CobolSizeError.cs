// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Signals a size error raised <i>during expression evaluation</i> (ISO/IEC 1989:2023 §14.7.5 phase-a — e.g. a
/// divide-by-zero, §14.7.5 case 2). It is thrown ONLY from the checked arithmetic helpers a statement opts into by
/// carrying an ON SIZE ERROR phrase (e.g. <see cref="CobolNum.DivideOrThrow"/>); generated code that has a SIZE
/// ERROR phrase wraps its evaluation+store in a <c>try/catch (CobolSizeError)</c> and runs the imperative. A
/// statement without the phrase never invokes the checked helpers, so its behavior is unchanged.
/// <para><paramref name="ecName"/> identifies the precise EC-SIZE-* condition (ISO §14.6.13.1.6 Table 13) for a
/// statement compiled with EC-SIZE checking enabled (&gt;&gt;TURN, §7.3.25): a zero divisor is
/// EC-SIZE-ZERO-DIVIDE; an exponentiation-rule violation EC-SIZE-EXPONENTIATION; the PROHIBITED-inexact and
/// generic evaluation overflows default to EC-SIZE-OVERFLOW ("arithmetic overflow in calculation").</para>
/// </summary>
public sealed class CobolSizeError(string detail, string ecName = "EC-SIZE-OVERFLOW") : Exception(detail)
{
    /// <summary>The Table 13 level-3 EC-SIZE-* exception-name of this size-error condition.</summary>
    public string EcName { get; } = ecName;
}
