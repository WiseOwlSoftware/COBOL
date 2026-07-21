// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The COBOL.NET <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> migration-flagging directive stage (ISO §7.3.14 /
/// §7.3.15; greenfield-only — the legacy pipeline keeps consuming the words via
/// <see cref="ConditionalCompilationProcessor"/>'s <c>KnownIgnoredDirectives</c>): parses each surviving
/// <c>&gt;&gt;FLAG-nn { ALL | option… } { ON | OFF }</c> line of the FINAL preprocessed text into a
/// <see cref="FlagEvent"/> (via the ONE <see cref="FlagDirectiveLine"/> parser), reports a malformed operand, and
/// blanks the line — line-count preserving, so every later token line number is stable (the <c>&gt;&gt;TURN</c> H3
/// discipline). The events build the compile-time <see cref="Binding.FlagState"/> that
/// <see cref="Validation.FlagConformancePass"/> folds per source line to decide whether a construct is flagged.
/// Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
/// <remarks>The directive-WORD edition gate (>>FLAG-14 = a 2023 introduction; >>FLAG-02 = a 2014 introduction,
/// obsolete at 2023) is Increment 0b — added here through the ONE <see cref="Editions.ConstructRegistry"/> like
/// <see cref="RefModZeroLengthDirectiveProcessor"/>.</remarks>
public static class FlagDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: edition-gate each directive word, collect the FLAG-02/FLAG-14
    /// toggle events, syntax-check each operand, and blank the directive lines. Line-count preserving.</summary>
    public static (string Text, IReadOnlyList<FlagEvent> Events) Process(
        string text, int dialectLevel, bool permissive, DiagnosticBag diagnostics, string sourcePath)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, []);
        var lines = text.Split('\n');
        List<FlagEvent>? events = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal)) continue;
            string body = trimmed[2..].TrimStart();

            FlagDirective directive;
            if (Matches(body, "FLAG-02")) directive = FlagDirective.Flag02;
            else if (Matches(body, "FLAG-14")) directive = FlagDirective.Flag14;
            else continue;

            string keyword = FlagDirectiveLine.DirectiveWord(directive);   // "FLAG-02" / "FLAG-14" (7 chars)
            string operand = body.Length > keyword.Length ? body[keyword.Length..].Trim() : "";
            var loc = new SourceLocation(sourcePath, 0, i, 0);

            // The directive-WORD edition gate, routed through the ONE ConstructRegistry (the >>REF-MOD-ZERO-LENGTH
            // precedent — one funnel for every construct, uniform across the four editions): >>FLAG-14 is a 2023
            // introduction (COBOLNET0900 below 2023); >>FLAG-02 is a 2014 introduction that is OBSOLETE at 2023
            // (COBOLNET0900 below 2014, then the COBOLNET0903 obsolete WARNING at 2023 — §7.3.14.1 NOTE / §4.2.13:
            // obsolete elements are still SUPPORTED and merely flagged, never rejected/removed).
            ConstructRegistry.Check(EditionInfo.Of(dialectLevel, permissive), new BagSink(diagnostics, loc),
                directive == FlagDirective.Flag14 ? Constructs.Flag14Directive2023 : Constructs.Flag02Directive2014,
                $">>{keyword} directive");

            if (FlagDirectiveLine.TryParse(directive, operand, out var options, out bool on, out string? error))
                (events ??= []).Add(new FlagEvent(i + 1, directive, on, options));
            else
                diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.FlagDirectiveMalformed.Code,
                    $">>{keyword} is malformed: {error} (ISO §7.3.{(directive == FlagDirective.Flag02 ? "14" : "15")}.2)",
                    loc, default);

            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), (IReadOnlyList<FlagEvent>?)events ?? []);
    }

    /// <summary>Does the directive body begin with <paramref name="keyword"/> as a whole word (the next char is
    /// whitespace or end-of-line)?</summary>
    private static bool Matches(string body, string keyword) =>
        body.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)
        && (body.Length == keyword.Length || char.IsWhiteSpace(body[keyword.Length]));

    /// <summary>Bridges the ONE <see cref="ConstructRegistry"/> edition funnel's <see cref="EditionDiagnostic"/>
    /// onto this stage's <see cref="DiagnosticBag"/> at the directive's line (the same bridge
    /// <see cref="RefModZeroLengthDirectiveProcessor"/> uses — Error fails the compile, Warning [the 0903 obsolete
    /// flag] rides through).</summary>
    private sealed class BagSink(DiagnosticBag bag, SourceLocation loc) : IDiagnosticSink
    {
        public void Report(in EditionDiagnostic d) => bag.Report(d.Code,
            d.Severity == EditionSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
            d.Message, loc, default);
    }
}
