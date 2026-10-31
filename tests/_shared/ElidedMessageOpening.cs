// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⛔ THE ONE DEFINITION OF "A DIAGNOSTIC MESSAGE THAT OPENS WITH AN ELIDED STATEMENT" (kb/Work PB388, PB4121, PB4160).
/// <para>A message that begins <c>CALL … USING BY VALUE</c> or <c>READ … KEY IS X</c> echoes a statement the programmer
/// never wrote: the general format's ellipsis stands for operands the binder HAS in hand, and the console renders the
/// U+2026 as <c>.</c> (<c>CALL . USING BY VALUE</c>). The cure is to name the phrase the message is about, as the
/// programmer wrote it. A mid-sentence <c>…</c> is the general format's own notation (<c>"the receiving operand of
/// SET … TO ENTRY"</c>) and is deliberately not matched: both patterns key on the message's HEAD.</para>
/// <para>The two halves share one head shape so they cannot drift apart: <see cref="Source"/> finds the head in the
/// C# that builds a message (<c>DiagnosticOpeningElisionDriftTests</c>, which sees every call form, reached or not),
/// and <see cref="Rendered"/> finds it in a diagnostic a compile really produced (the conformance suite's refusal
/// test, which sees whatever string-building a message passed through).</para>
/// </summary>
internal static class ElidedMessageOpening
{
    /// <summary>The head: one to four upper-case words (or a <c>{value}</c> hole standing for one), then an ellipsis in
    /// its U+2026 or three-dot spelling. <c>VERB …</c>, <c>INITIALIZE REPLACING …</c>, <c>PERFORM VARYING …</c>,
    /// <c>{verb} …</c>.</summary>
    private const string Head = @"(?:[A-Z][A-Z-]*|\{[\w.]+\})(?: (?:[A-Z][A-Z-]*|\{[\w.]+\})){0,3} (?:…|\.\.\.)";

    /// <summary>A leading call argument that is not the message, ended by its comma: a <c>"COBOLnnnn"</c> code literal,
    /// any non-delimiter, or a parenthesized group. ONE alternation under ONE star, so the match is linear — two
    /// adjacent unbounded classes would backtrack catastrophically on the many calls that do not match.</summary>
    private const string LeadingArgument = @"(?:""COBOL\d{4}""|[^,()""]|\([^()]*\))*,";

    /// <summary>The message-reporting call forms of the compiler, followed by up to four leading arguments (the
    /// edition, the code, the severity, the id) and then the FIRST string literal: <c>Error(</c>, <c>Report(</c>,
    /// <c>BoundRejected.Report(</c>, <c>BoundOperandError.Refused(</c>, <c>InitializeErrorAction(</c>,
    /// <c>EditionDiagnostic(</c> and the warning forms.</summary>
    public static readonly Regex Source = new(
        @"\b(?:Error|Report|Refused|Warning|InitializeErrorAction|EditionDiagnostic)\((?:" + LeadingArgument
        + @"){0,4}\s*\$?""" + Head,
        RegexOptions.Compiled);

    /// <summary>A text that BEGINS with the head — for a string that becomes the head of a message when a registry
    /// splices it in front of one (a construct's <c>display</c> opens <c>"{display} requires COBOL-2002"</c>).</summary>
    public static readonly Regex Opening = new("^" + Head, RegexOptions.Compiled);

    /// <summary>A rendered diagnostic (<c>file(l,c): error COBOLnnnn: message</c>) whose message has the head.</summary>
    public static readonly Regex Rendered = new(
        @"(?:error|warning|info) COBOL\d{4}: " + Head, RegexOptions.Compiled);
}
