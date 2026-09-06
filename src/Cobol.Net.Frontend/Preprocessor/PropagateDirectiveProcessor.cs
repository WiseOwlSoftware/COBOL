// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The COBOL.NET <c>&gt;&gt;PROPAGATE</c> directive stage (ISO/IEC 1989:2023 §7.3.21): the directive controls
/// AUTOMATIC propagation of an unhandled exception condition to the activating runtime element (GR1/GR2 — as though
/// a <c>GOBACK RAISING LAST</c> were executed), scoped over the functions/methods/programs that follow in the
/// compilation group; the default is <c>PROPAGATE OFF</c> (GR4). <b>§7.3.21.3 SR1 (the directive shall not be
/// specified WITHIN a compilation unit) is NOT enforced</b> — this pre-parse, line-based stage has no
/// compilation-unit-boundary awareness, and blanking the line before parsing means no downstream stage can catch a
/// misplaced directive, so a <c>&gt;&gt;PROPAGATE</c> inside a unit is recognized and consumed. A documented
/// limitation (a placement-diagnostic follow-up), not a silent mis-compile of well-placed source.
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
    /// <summary>Process <paramref name="text"/>: a <c>&gt;&gt;PROPAGATE</c> is recognized (syntax-checked:
    /// <c>ON</c> | <c>OFF</c>, OFF default) and blanked, its runtime semantics deferred to PHASE-13. Line-count
    /// preserving. The EDITION question — the directive is a COBOL-2002 introduction — is not asked here: it was
    /// answered once, at the directive-recognition point, by the <c>propagate-directive-2002</c> registry row
    /// (kb/Work PB725), so this stage needs no dialect at all.</summary>
    public static string Process(string text, DiagnosticBag diagnostics, string sourcePath,
        SourceLineMap? lineMap = null)
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

            var loc = lineMap?.Locate(i + 1, sourcePath) ?? new SourceLocation(sourcePath, 0, i, 0);   // the SOURCE origin of resultant line i (kb/Work PB82)
            // The introduction gate fired at the ONE directive-recognition point (CompilerDirectiveCatalog,
            // from the propagate-directive-2002 row) — this stage ran its own `if (dialectLevel < 2002)` with a
            // BESPOKE COBOLNET0883 until kb/Work PB725. COBOLNET0883 now owns ONLY the malformed-operand rule
            // below; the edition question is the registry's COBOLNET0900.

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
