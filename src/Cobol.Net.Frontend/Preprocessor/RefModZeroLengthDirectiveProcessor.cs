// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive toggle (ISO/IEC 1989:2023 §7.3.23), anchored to its
/// 1-based line in the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The
/// compile-time <see cref="Binding.RefModZeroLengthState"/> folds these to decide whether a reference modification
/// on a given line ALLOWS a zero-length result (§8.4.3.3.4 item 5c).</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text. A directive applies to the
/// text that FOLLOWS it (the <c>&gt;&gt;TURN</c> GR5 discipline); a directive occupies its own whole line after
/// free-form normalization, so the fold is strict <c>Line &lt; siteLine</c>.</param>
/// <param name="On">ON (zero-length allowed) vs OFF (the §7.3.23.3 GR1 default — a zero-length ref-mod raises
/// EC-BOUND-REF-MOD).</param>
public sealed record RefModZeroLengthEvent(int Line, bool On);

/// <summary>
/// The COBOL.NET <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive stage (ISO §7.3.23; greenfield-only — the legacy
/// pipeline keeps consuming the directive via <see cref="ConditionalCompilationProcessor"/>'s
/// <c>KnownIgnoredDirectives</c>): parses each surviving <c>&gt;&gt;REF-MOD-ZERO-LENGTH {ON | OFF}</c> line of the
/// FINAL preprocessed text into a <see cref="RefModZeroLengthEvent"/>, edition-gates the directive (the
/// introduction gate — §7.3.23 is a COBOL-2023 addition, Annex E.3.3 item 23 — routed through the ONE
/// <see cref="ConstructRegistry"/> so the message is single-sourced and the construct enters the version matrix),
/// and blanks the line (line-count preserving, so every later token line number is stable — the <c>&gt;&gt;TURN</c>
/// H3 discipline).
/// </summary>
public static class RefModZeroLengthDirectiveProcessor
{
    private const string Keyword = "REF-MOD-ZERO-LENGTH";   // 19 characters

    /// <summary>Process <paramref name="text"/>: collect the toggle events, edition-gate + syntax-check each
    /// directive, blank the directive lines. At <paramref name="dialectLevel"/> &lt; 2023 the directive is the
    /// four-compilers introduction diagnostic (COBOLNET0900), never silently ignored. Line-count preserving.</summary>
    public static (string Text, IReadOnlyList<RefModZeroLengthEvent> Events) Process(
        string text, DiagnosticBag diagnostics, string sourcePath, SourceLineMap? lineMap = null)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, []);
        var lines = text.Split('\n');
        List<RefModZeroLengthEvent>? events = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal)) continue;
            string body = trimmed[2..].TrimStart();
            if (!body.StartsWith(Keyword, StringComparison.OrdinalIgnoreCase)
                || (body.Length > Keyword.Length && !char.IsWhiteSpace(body[Keyword.Length]))) continue;

            var loc = lineMap?.Locate(i + 1, sourcePath) ?? new SourceLocation(sourcePath, 0, i, 0);   // the SOURCE origin of resultant line i (kb/Work PB82)

            // The introduction gate (§7.3.23 is a COBOL-2023 addition) already fired at the ONE
            // directive-recognition point — CompilerDirectiveCatalog, from the ref-mod-zero-length-2023 row's
            // directiveWords (kb/Work PB725). This stage collects the toggles; it does not re-decide the edition.

            // §7.3.23.2: >> REF-MOD-ZERO-LENGTH { ON | OFF }, OFF the underlined default. Record a well-formed toggle;
            // a malformed operand is the syntax diagnostic (never a silent accept).
            string operand = body.Length > Keyword.Length ? body[Keyword.Length..].Trim().ToUpperInvariant() : "";
            if (operand is "ON" or "OFF")
                (events ??= []).Add(new RefModZeroLengthEvent(i + 1, operand == "ON"));
            else
                // The Id comes from the catalog descriptor, never a bare literal — a bare "COBOLNET1573" here
                // collided with the later catalog-allocated external-file-status-consistency (review finding C1).
                diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.RefModZeroLengthMalformedOperand.Code,
                    $">>REF-MOD-ZERO-LENGTH expects the ON or OFF phrase (ISO §7.3.23.2), not '{operand}'", loc, default);
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), (IReadOnlyList<RefModZeroLengthEvent>?)events ?? []);
    }
}
