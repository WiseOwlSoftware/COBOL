// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Common;

/// <summary>
/// ⛔ THE §8.3.2.1 WORD-LENGTH CEILING, IN ONE PLACE — because it was in one place (the VersionConformancePass
/// <c>VisitCobolWord</c> funnel, which walks the MAIN parse tree) and that place cannot see DIRECTIVE-carried
/// words: a 44-character exception-name in <c>&gt;&gt;TURN</c> and a 44-character compilation-variable-name in
/// <c>&gt;&gt;DEFINE</c> both compiled clean at <c>--std 2002</c> while the same word in a RAISE statement was
/// correctly rejected (found by kb/Work R05's own conformance fact — the evidence ledger's "correctly rejected
/// with COBOLNET1567" was measured on the statement spelling only). §8.3.2.1 covers every COBOL word — "a
/// compiler-directive word, a context-sensitive word, an intrinsic-function-name, a reserved word, a
/// system-name, or a user-defined word" — so the directive stages enforce the SAME rule through the SAME text,
/// reporting on their own channels. (&gt;&gt;COBOL-WORDS needs no site of its own: the words its literals
/// introduce reach the main tree — and the funnel — wherever they are actually used.)
///
/// <para>The ceiling: 63 at COBOL-2023 (Annex E.3.3 item 11 — a RELAXATION, so firing below 2023 for a 32..63
/// word is a length error, not an introduction gate), 31 at 2002/2014, 30 at 1985. Above 63 is a hard cap at
/// every edition. The underscore/charset half of §8.3.2.1 stays in the tree funnel and the lexer: every
/// <c>&gt;&gt;</c> directive is itself rejected below 2002, so a below-2002 underscore cannot reach a directive
/// word.</para>
/// </summary>
public static class CobolWordRule
{
    /// <summary>The §8.3.2.1 maximum COBOL-word length for a targeted edition.</summary>
    public static int MaxLength(int dialectLevel) =>
        dialectLevel >= 2023 ? 63 : dialectLevel >= 2002 ? 31 : 30;

    /// <summary>The COBOLNET1567 message when <paramref name="word"/> exceeds the ceiling; null when legal.
    /// One text for every reporting channel — the tree funnel and the directive stages.</summary>
    public static string? LengthViolation(string word, int dialectLevel) =>
        word.Length > MaxLength(dialectLevel)
            ? $"the COBOL word '{word}' is {word.Length} characters, exceeding the "
              + $"{MaxLength(dialectLevel)}-character maximum for COBOL-{dialectLevel} "
              + "(ISO §8.3.2.1 — COBOL-2023 raised the limit to 63)"
            : null;
}
