// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The <c>&gt;&gt;LEAP-SECOND [ON | OFF]</c> compiler directive (ISO/IEC 1989:2023 §7.3.17) — the ONE compilation-group
/// fact every §15.3 date/time consumer reads (kb/Work PB65, AR-15.79.3-4): with ON in effect a formatted-time
/// argument's seconds subfield may be 60 (§15.3.3.3 — "less than 61 when the LEAP-SECOND directive with the ON
/// phrase is in effect") and a standard numeric time form value is bounded at 86,401 (§7.3.17.4 GR4) instead of
/// 86,400 (GR5). The directive used to be CONSUMED and DISCARDED (<c>ConditionalCompilationProcessor</c>'s
/// known-ignored set), so <c>SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960")</c> under <c>&gt;&gt;LEAP-SECOND ON</c>
/// answered 0 (and killed the run unit under EC-ARGUMENT-FUNCTION checking) where §15.79.4 requires 86,400.
/// <para>The reported side of the directive — whether a value greater than 59 is REPORTED in the seconds position
/// of ACCEPT … FROM TIME / CURRENT-DATE / FORMATTED-CURRENT-DATE / WHEN-COMPILED, and whether SECONDS-PAST-MIDNIGHT
/// may return ≥ 86,400 (GR2, GR4) — is implementor-defined and answered "never" (docs/CONFORMANCE.md A.1 item 112:
/// the .NET clock has no leap seconds), so ON changes only what the program may PRESENT as an argument.</para>
/// <para>Line-count preserving (the &gt;&gt;TURN H3 discipline): the directive line is blanked, never deleted.
/// §7.3.17.3 SR1 — the directive "shall not be specified within a compilation unit": one after the first unit's
/// header is COBOLNET1650. §7.3.17.4 GR1 — absent, OFF is implied; the LAST directive before the first unit wins.
/// The word ON is optional in the printed format (only OFF is underlined — the figure note at §7.3.17.2), so a
/// bare <c>&gt;&gt;LEAP-SECOND</c> selects ON.</para>
/// </summary>
public static class LeapSecondDirectiveProcessor
{
    private const string Keyword = "LEAP-SECOND";

    /// <summary>Process <paramref name="text"/>: edition-gate + syntax-check each directive, resolve the group's
    /// ON/OFF state, blank the directive lines. Returns the text and whether ON is in effect for the group.</summary>
    public static (string Text, bool LeapSecondOn) Process(
        string text, int dialectLevel, bool permissive, DiagnosticBag diagnostics, string sourcePath)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, false);
        var lines = text.Split('\n');
        bool on = false, insideUnit = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            if (!insideUnit && StartsUnit(trimmed)) insideUnit = true;
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal)) continue;
            string body = trimmed[2..].TrimStart();
            if (!body.StartsWith(Keyword, StringComparison.OrdinalIgnoreCase)
                || (body.Length > Keyword.Length && !char.IsWhiteSpace(body[Keyword.Length]))) continue;

            var loc = new SourceLocation(sourcePath, 0, i, 0);
            // Introduction gate (§7.3 compiler directives are COBOL-2002 additions): reject below 2002 via the ONE
            // ConstructRegistry (COBOLNET0900); a no-op at 2002+.
            ConstructRegistry.Check(EditionInfo.Of(dialectLevel, permissive), new BagSink(diagnostics, loc),
                Constructs.LeapSecondDirective2002, ">>LEAP-SECOND directive");

            string operand = body.Length > Keyword.Length ? body[Keyword.Length..].Trim().ToUpperInvariant() : "";
            if (insideUnit)
                diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.LeapSecondDirectiveSyntax.Code,
                    ">>LEAP-SECOND shall not be specified within a compilation unit (ISO §7.3.17.3 SR1) — write it "
                    + "before the first IDENTIFICATION DIVISION of the compilation group", loc, default);
            else if (operand is "" or "ON") on = true;
            else if (operand == "OFF") on = false;
            else
                diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.LeapSecondDirectiveSyntax.Code,
                    $">>LEAP-SECOND expects the ON or OFF phrase (ISO §7.3.17.2), not '{operand}'", loc, default);
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return (string.Join('\n', lines), on);
    }

    /// <summary>The first line of a compilation unit — an IDENTIFICATION DIVISION header, or the header-less
    /// unit forms (§8.1.1: PROGRAM-ID / CLASS-ID / FUNCTION-ID / INTERFACE-ID may open a unit without the division
    /// header at 2002+).</summary>
    private static bool StartsUnit(string trimmed)
    {
        string t = trimmed.ToUpperInvariant();
        return t.StartsWith("IDENTIFICATION DIVISION", StringComparison.Ordinal)
            || t.StartsWith("ID DIVISION", StringComparison.Ordinal)
            || t.StartsWith("PROGRAM-ID", StringComparison.Ordinal)
            || t.StartsWith("CLASS-ID", StringComparison.Ordinal)
            || t.StartsWith("FUNCTION-ID", StringComparison.Ordinal)
            || t.StartsWith("INTERFACE-ID", StringComparison.Ordinal);
    }

    private sealed class BagSink(DiagnosticBag bag, SourceLocation loc) : IDiagnosticSink
    {
        public void Report(in EditionDiagnostic d) => bag.Report(d.Code,
            d.Severity == EditionSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
            d.Message, loc, default);
    }
}
