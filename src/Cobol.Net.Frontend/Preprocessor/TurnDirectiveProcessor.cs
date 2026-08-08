// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>One <c>&gt;&gt;TURN</c> directive event (ISO/IEC 1989:2023 §7.3.25), anchored to its 1-based line in
/// the FINAL preprocessed text — directly comparable to an ANTLR token's <c>Start.Line</c>. The compile-time
/// TurnState folds these events to decide WHETHER an EC guard is emitted at each statement (deep-dive D10).</summary>
/// <param name="Line">The directive's 1-based line in the final preprocessed text. A directive applies to
/// SUCCEEDING statements only (§7.3.25.4 GR5); strict <c>eventLine &lt; statementLine</c> realizes that because a
/// directive occupies its own whole line after free-form normalization.</param>
/// <param name="Names">The (exception-name, file-name-or-null) pairs the directive toggles, pre-expansion —
/// EC-ALL / level-2 expansion (GR2/GR3) is the TurnState's matching predicate, not stored pairs.</param>
/// <param name="On">CHECKING ON vs OFF.</param>
/// <param name="WithLocation">ON WITH LOCATION (GR7 — saves EXCEPTION-LOCATION/-STATEMENT information).</param>
public sealed record TurnEvent(int Line, IReadOnlyList<(string Ec, string? File)> Names, bool On, bool WithLocation);

/// <summary>
/// The COBOL.NET <c>&gt;&gt;TURN</c> directive stage (ISO §7.3.25; greenfield-only — the legacy pipeline keeps
/// consuming TURN in <see cref="ConditionalCompilationProcessor"/>): parses each surviving <c>&gt;&gt;TURN</c>
/// line of the FINAL preprocessed text into a <see cref="TurnEvent"/>, enforces the directive's syntax rules
/// (SR3 — no duplicate (exception-name, file-name) pair per directive; SR4 — a file-name only with an
/// <c>EC-I-O…</c> name), and blanks the line (line-count preserving, so every later token line number is stable
/// — scout hazard H3: the stage asserts the line count is unchanged).
/// </summary>
public static class TurnDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: collect the TURN events, blank the directive lines. At
    /// <paramref name="dialectLevel"/> &lt; 2002 a <c>&gt;&gt;TURN</c> is the four-compilers diagnostic (the EC
    /// model is 2002+ — COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN per-edition gating), never silently ignored.</summary>
    public static (string Text, IReadOnlyList<TurnEvent> Events) Process(
        string text, int dialectLevel, DiagnosticBag diagnostics, string sourcePath)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return (text, []);
        var lines = text.Split('\n');
        List<TurnEvent>? events = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal)) continue;
            string body = trimmed[2..].TrimStart();
            if (!body.StartsWith("TURN", StringComparison.OrdinalIgnoreCase)
                || (body.Length > 4 && !char.IsWhiteSpace(body[4]))) continue;

            var loc = new SourceLocation(sourcePath, 0, i, 0);
            if (dialectLevel < 2002)
            {
                diagnostics.ReportError("COBOLNET0875",
                    ">>TURN is the COBOL-2002+ exception-condition checking directive (ISO §7.3.25) — it requires "
                    + $"--std 2002 or later (targeting COBOL-{dialectLevel})", loc, default);
                lines[i] = "";
                continue;
            }
            if (ParseTurn(body[4..], i + 1, dialectLevel, diagnostics, loc) is { } ev)
                (events ??= []).Add(ev);
            lines[i] = "";   // blank, never delete — line-count preserving (H3)
        }
        return (string.Join('\n', lines), (IReadOnlyList<TurnEvent>?)events ?? []);
    }

    /// <summary>Parse one directive body: <c>{ec-name [file-name]…}… CHECKING {ON [WITH LOCATION] | OFF}</c>
    /// (§7.3.25.2). SR1: any word starting <c>EC-</c> is an exception-name, not a file-name. Null on a malformed
    /// directive (reported).</summary>
    private static TurnEvent? ParseTurn(string body, int line, int dialectLevel,
        DiagnosticBag diagnostics, SourceLocation loc)
    {
        var words = body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var names = new List<(string Ec, string? File)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool? on = null;
        bool withLocation = false;

        // §8.3.2.1 applies to DIRECTIVE-carried words too — the tree-walk funnel never sees them, so this was
        // the hole through which a 44-character exception-name compiled at --std 2002 (CobolWordRule; the R05
        // fact that found it). One check per operand word, exception-names and file-names alike.
        void CheckLength(string w)
        {
            if (Common.CobolWordRule.LengthViolation(w, dialectLevel) is { } violation)
                diagnostics.ReportError(Editions.Diagnostics.DiagnosticCatalog.WordLengthExceeded.Code,
                    violation, loc, default);
        }

        int k = 0;
        string? currentEc = null;
        bool currentEcHasFiles = false;
        void Flush()
        {
            // An EC name with no trailing file-names is one (ec, null) pair.
            if (currentEc is not null && !currentEcHasFiles) AddPair(currentEc, null);
            currentEc = null;
            currentEcHasFiles = false;
        }
        void AddPair(string ec, string? file)
        {
            if (!seen.Add(ec + "|" + (file ?? "")))
                diagnostics.ReportError("COBOLNET0718",
                    $">>TURN: the exception-name/file-name combination '{ec}{(file is null ? "" : " " + file)}' is "
                    + "specified more than once in this directive (ISO §7.3.25.3 SR3)", loc, default);
            else
                names.Add((ec.ToUpperInvariant(), file?.ToUpperInvariant()));
        }

        for (; k < words.Length; k++)
        {
            string w = words[k];
            if (w.Equals("CHECKING", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                if (k + 1 < words.Length && words[k + 1].Equals("ON", StringComparison.OrdinalIgnoreCase))
                {
                    on = true;
                    // ON [WITH LOCATION] (§7.3.25.2; GR7)
                    if (k + 2 < words.Length && words[k + 2].Equals("WITH", StringComparison.OrdinalIgnoreCase)
                        && k + 3 < words.Length && words[k + 3].Equals("LOCATION", StringComparison.OrdinalIgnoreCase))
                        withLocation = true;
                    else if (k + 2 < words.Length && words[k + 2].Equals("LOCATION", StringComparison.OrdinalIgnoreCase))
                        withLocation = true;   // WITH is a noise word in this implementation's leniency
                }
                else if (k + 1 < words.Length && words[k + 1].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                    on = false;
                break;
            }
            if (w.StartsWith("EC-", StringComparison.OrdinalIgnoreCase))   // SR1 — an EC- word is an exception-name
            {
                Flush();
                CheckLength(w);
                currentEc = w;
            }
            else if (currentEc is not null)
            {
                // A non-EC word after an exception-name is a file-name (SR1); SR4 — only with an EC-I-O… name.
                if (!currentEc.StartsWith("EC-I-O", StringComparison.OrdinalIgnoreCase))
                    diagnostics.ReportError("COBOLNET0719",
                        $">>TURN: file-name '{w}' specified with exception-name '{currentEc}', which does not begin "
                        + "with 'EC-I-O' (ISO §7.3.25.3 SR4)", loc, default);
                else
                {
                    CheckLength(w);
                    AddPair(currentEc, w);
                    currentEcHasFiles = true;
                }
            }
            else
            {
                diagnostics.ReportError("COBOLNET0718",
                    $">>TURN: unexpected word '{w}' — expected an exception-name (a word beginning 'EC-', "
                    + "ISO §7.3.25.3 SR1)", loc, default);
                return null;
            }
        }

        if (on is null || names.Count == 0)
        {
            diagnostics.ReportError("COBOLNET0718",
                ">>TURN: malformed directive — the format is '>>TURN {exception-name [file-name]…}… CHECKING "
                + "{ON [WITH LOCATION] | OFF}' (ISO §7.3.25.2)", loc, default);
            return null;
        }
        return new TurnEvent(line, names, on.Value, withLocation);
    }
}
