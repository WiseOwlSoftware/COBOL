// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The CONTINUE AFTER timed-pause facility (ISO/IEC 1989:2023 §14.9.9 — a COBOL-2023 addition).
/// </summary>
public static class CobolTiming
{
    /// <summary>The implementor-defined maximum meaningful interval (§14.9.9.4 GR1 — "the implementor shall
    /// specify the maximum meaningful value"): a value above it suspends for the maximum. One day is a generous,
    /// non-blocking-forever cap.</summary>
    public const long MaxSeconds = 86_400;

    /// <summary>
    /// CONTINUE AFTER n SECONDS (ISO §14.9.9.4 GR1): suspend execution for <paramref name="seconds"/> seconds,
    /// then continue with the next statement. The implementor value of m (the fractional digit count of the
    /// temporary 9(n)V9(m) item) is 0 — fractional seconds truncate toward zero (the implicit COMPUTE without
    /// ROUNDED), which the integer-typed caller already applied. GR1a — a value below zero is forced to 0; GR1b —
    /// when <paramref name="checkLessThanZero"/> (EC-CONTINUE-LESS-THAN-ZERO checking was enabled at the statement),
    /// the nonfatal EC-CONTINUE-LESS-THAN-ZERO is set to exist (observable via FUNCTION EXCEPTION-STATUS), then
    /// execution continues (§14.6.13.1.4). A value above <see cref="MaxSeconds"/> suspends for the maximum.
    /// (The §14.6.13.1.4 selection of a matching USE declarative for this nonfatal condition is a scheduled
    /// follow-on — the last-exception status this sets is the observable behavior.)
    /// </summary>
    public static void ContinueAfter(double seconds, bool checkLessThanZero)
    {
        // GR1a/GR1b operate on arithmetic-expression-1's EVALUATED value (the sign test precedes the m=0
        // truncation), so a negative FRACTIONAL interval in (-1, 0) must still set the exception — test the sign of
        // the full-precision value, not a pre-truncated integer.
        if (seconds < 0.0)
        {
            if (checkLessThanZero) ExceptionState.Set("EC-CONTINUE-LESS-THAN-ZERO", fatal: false);
            return;                                                 // GR1a — value set to 0 → no suspension
        }
        long secs = (long)seconds;                                  // m = 0: truncate toward zero (GR1, no ROUNDED)
        if (secs == 0) return;                                      // GR1 — no suspension for a zero interval
        System.Threading.Thread.Sleep((int)System.Math.Min(secs, MaxSeconds) * 1000);
    }
}
