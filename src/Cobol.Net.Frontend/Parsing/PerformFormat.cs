// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE FORMAT-3 PERFORM DECISION, WRITTEN ONCE. An inline PERFORM is Format 3 (exception-checking) iff it carries
/// any WHEN phrase (ordinary / OTHER / COMMON), a FINALLY phrase, or a [WITH] LOCATION head (ISO §14.9.28.2
/// Format 3). Every reader asks THIS predicate — the binder's format dispatch, the COBOLNET0900 introduction gate
/// (<c>VersionConformancePass.VisitPerformStatement</c>) and the front end's §14.9.28.4 GR14 implicit PUSH ALL /
/// POP ALL placement (<see cref="Preprocessor.ExceptionPerformDirectiveScope"/>) — so the three cannot drift. It
/// lives in the front end because the last of them runs before binding: the implicit POP ALL must reach the
/// conditional-compilation driver's state, which only the front end holds (kb/Work PB1066).
/// </summary>
public static class PerformFormat
{
    /// <summary>True when <paramref name="p"/> is an exception-checking (Format-3) PERFORM.</summary>
    public static bool IsFormat3(CobolParserCore.PerformStatementContext p) =>
        p.performWhenPhrase().Length > 0 || p.performWhenOther() is not null
        || p.performWhenCommon() is not null || p.performFinally() is not null
        || p.performInlineHead()?.performLocationPhrase() is not null;
}
