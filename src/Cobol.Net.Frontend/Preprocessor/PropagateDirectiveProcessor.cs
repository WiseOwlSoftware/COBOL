// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The COBOL.NET <c>&gt;&gt;PROPAGATE</c> directive stage (ISO/IEC 1989:2023 §7.3.21): the directive controls
/// AUTOMATIC propagation of an unhandled exception condition to the activating runtime element (GR1/GR2 — as though
/// a <c>GOBACK RAISING LAST</c> were executed), scoped over the functions/methods/programs that follow in the
/// compilation group; the default is <c>PROPAGATE OFF</c> (GR4). SR1 requires it OUTSIDE a compilation unit
/// (between units).
/// <para>This stage RECOGNIZES the directive and EDITION-GATES it (the introduction gate). Its INTRODUCTION edition
/// is PROVISIONAL COBOL-2002 (the roadmap decision-1 policy, as for TYPEDEF / the FLOAT trio): §7.3.21 is live in
/// the 2023 spec — Annex E lists no removal — and it belongs to the 2002-era EC / compiler-directive facility (the
/// same era as <c>&gt;&gt;TURN</c>, gated at 2002), but the exact 2002-vs-2014 edge cannot be pinned from the 2023
/// text or the VCR; refine against the 1989:2002 standard when available. The RUNTIME semantics (actually driving
/// EC propagation) are the deferred PHASE-13 EC-remnant work — this stage does NOT implement them, so a
/// well-formed <c>&gt;&gt;PROPAGATE ON</c> is recognized-and-edition-gated but does not yet change EC behavior; it
/// is never a silent stray token nor a parse error. The line is blanked (line-count preserving, so every later
/// token line number is stable — the <c>&gt;&gt;TURN</c> H3 discipline).</para>
/// </summary>
public static class PropagateDirectiveProcessor
{
    /// <summary>Process <paramref name="text"/>: at <paramref name="dialectLevel"/> &lt; 2002 a
    /// <c>&gt;&gt;PROPAGATE</c> is the four-compilers introduction diagnostic (COBOLNET0883), never silently
    /// ignored; at 2002+ it is recognized (syntax-checked: <c>ON</c> | <c>OFF</c>, OFF default) and blanked, its
    /// runtime semantics deferred to PHASE-13. Line-count preserving.</summary>
    public static string Process(string text, int dialectLevel, DiagnosticBag diagnostics, string sourcePath)
    {
        if (!text.Contains(">>", StringComparison.Ordinal)) return text;
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').TrimStart();
            if (!trimmed.StartsWith(">>", StringComparison.Ordinal)) continue;
            string body = trimmed[2..].TrimStart();
            if (!body.StartsWith("PROPAGATE", StringComparison.OrdinalIgnoreCase)
                || (body.Length > 9 && !char.IsWhiteSpace(body[9]))) continue;

            var loc = new SourceLocation(sourcePath, 0, i, 0);
            if (dialectLevel < 2002)
            {
                diagnostics.ReportError("COBOLNET0883",
                    ">>PROPAGATE is the COBOL-2002+ exception-condition propagation directive (ISO §7.3.21) — it "
                    + $"requires --std 2002 or later (targeting COBOL-{dialectLevel})", loc, default);
                lines[i] = "";
                continue;
            }

            // §7.3.21.2: >> PROPAGATE { ON | OFF }, OFF the underlined default. Recognize the phrase; a malformed
            // operand is the syntax diagnostic (never a silent accept). The runtime propagation effect is PHASE-13.
            string operand = body.Length > 9 ? body[9..].Trim().ToUpperInvariant() : "";
            if (operand is not ("" or "ON" or "OFF"))
                diagnostics.ReportError("COBOLNET0883",
                    $">>PROPAGATE expects the ON or OFF phrase (ISO §7.3.21.2), not '{operand}'", loc, default);
            lines[i] = "";   // blank, never delete — line-count preserving (the >>TURN H3 discipline)
        }
        return string.Join('\n', lines);
    }
}
