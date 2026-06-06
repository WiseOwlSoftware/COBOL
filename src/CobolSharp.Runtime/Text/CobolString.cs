// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolSharp.Runtime.Text;

/// <summary>
/// The value-level character store for the .NET-native data model: COBOL alphanumeric / national
/// character data represented as a UTF-16 <see cref="string"/> (the typed default,
/// <c>docs/DATA_MODEL_ARCHITECTURE.md</c> §1.2). This is the character analogue of
/// <see cref="Numeric.CobolNum"/> — the typed-path counterpart of the byte-image string ops in
/// <c>StorageHelpers</c> (which becomes the <c>Bytes/</c> island engine, ADR §5).
///
/// <para><b>Two responsibilities.</b> (1) The COBOL whole-field alphanumeric MOVE / comparison semantics
/// over <see cref="string"/> — left/right justification, space fill/truncation (ISO §14.9.25), and the
/// alphanumeric relation condition with the shorter operand space-extended (ISO §8.8.4.1.2, default native
/// collating sequence). (2) The <c>IDataSlot</c> boundary codec (ADR §2.5) between a typed <see cref="string"/>
/// field and a byte window, under the <b>Latin-1</b> convention (byte <c>k</c> ↔ <c>U+00kk</c>, ADR R10) so
/// arbitrary content — incl. <c>LOW-VALUE</c>/<c>HIGH-VALUE</c>/binary — round-trips losslessly. This matches
/// the existing byte path exactly: <c>StorageHelpers.MoveStringToField</c>/<c>MoveFieldToField</c> lay one byte
/// per character position via the low byte, and <c>CompareFieldToField</c> decodes with
/// <see cref="Encoding.Latin1"/>.</para>
///
/// <para>Per the migration's "byte-identical to the legacy path first" discipline, every operation here is
/// proven bit-for-bit equivalent to the byte path by a differential oracle (<c>CobolStringDifferentialTests</c>)
/// before the typed character flip (ADR §10 Stage 3) wires it in. Purely additive — nothing in the pipeline
/// calls it yet, so the guard stays green by construction.</para>
///
/// <para><b>Always ordinal</b> (ADR §1.2.1 guardrail 1): every comparison is by character value, never the BCL's
/// culture-aware default, which would silently break COBOL semantics. <see cref="Compare"/> implements the COBOL
/// space-extension of the shorter operand directly (the <c>0x20</c> fill, ISO §8.8.4.1.2), which is the
/// COBOL-correct refinement of the legacy <c>TrimEnd()</c>-then-ordinal path; they agree on all data whose only
/// trailing whitespace is the COBOL space.</para>
/// </summary>
public static class CobolString
{
    /// <summary>The COBOL space character (ISO §8.5.1.2): the single fill/extension character, never any other
    /// Unicode whitespace.</summary>
    private const char Space = ' ';

    /// <summary>
    /// The receiving value of an alphanumeric MOVE (ISO §14.9.25): <paramref name="value"/> placed into a field
    /// of exactly <paramref name="width"/> character positions, space-filled or truncated. Left-justified by
    /// default (truncate / pad on the right); <paramref name="justifiedRight"/> (the JUSTIFIED RIGHT clause,
    /// ISO §13.18.36) truncates / pads on the left. Returns a string of exactly <paramref name="width"/> chars
    /// (empty when <paramref name="width"/> ≤ 0).
    /// </summary>
    public static string Store(string value, int width, bool justifiedRight = false)
    {
        value ??= "";
        if (width <= 0)
            return "";
        if (justifiedRight)
            return value.Length > width
                ? value[(value.Length - width)..]               // keep the rightmost `width` chars
                : new string(Space, width - value.Length) + value;
        return value.Length >= width
            ? value[..width]                                    // keep the leftmost `width` chars
            : value + new string(Space, width - value.Length);
    }

    /// <summary>
    /// Compares two alphanumeric operands by the default native collating sequence (ordinal character value),
    /// with the shorter operand space-extended to the longer (ISO §8.8.4.1.2). Returns a negative value, zero,
    /// or a positive value as <paramref name="left"/> is less than, equal to, or greater than
    /// <paramref name="right"/>. <b>Not</b> culture-aware (ADR §1.2.1 guardrail 1). When a
    /// <c>PROGRAM COLLATING SEQUENCE</c> is active the weight-table path is used instead (ADR §1.2.1 collating
    /// note) — that path is not this method.
    /// </summary>
    public static int Compare(string? left, string? right)
    {
        ReadOnlySpan<char> l = left ?? "";
        ReadOnlySpan<char> r = right ?? "";
        int n = Math.Max(l.Length, r.Length);
        for (int i = 0; i < n; i++)
        {
            char lc = i < l.Length ? l[i] : Space;
            char rc = i < r.Length ? r[i] : Space;
            if (lc != rc)
                return lc < rc ? -1 : 1;
        }
        return 0;
    }

    /// <summary>
    /// Decodes a byte window to its character image under the Latin-1 convention (byte <c>k</c> → <c>U+00kk</c>),
    /// the <c>IDataSlot</c> boundary read (ADR §2.5). No trimming — the raw field content, all
    /// <paramref name="window"/>.Length positions.
    /// </summary>
    public static string FromWindow(ReadOnlySpan<byte> window) => Encoding.Latin1.GetString(window);

    /// <summary>Decodes a <c>(area, offset, length)</c> byte window to its character image under the Latin-1
    /// convention — the typed↔byte boundary read from a backing array (the IL-friendly overload of
    /// <see cref="FromWindow(ReadOnlySpan{byte})"/>). No trimming.</summary>
    public static string FromWindow(byte[] area, int offset, int length) => Encoding.Latin1.GetString(area, offset, length);

    /// <summary>
    /// Encodes a character <paramref name="field"/> into a byte <paramref name="window"/> under the Latin-1
    /// convention (char low byte), the <c>IDataSlot</c> boundary write (ADR §2.5). Mirrors
    /// <c>StorageHelpers.MoveStringToField</c>: writes one byte per character position, then space-fills any
    /// remaining window positions (and truncates a longer field on the right). For a stored field of exactly
    /// <c>window.Length</c> positions this is a pure 1:1 byte copy.
    /// </summary>
    public static void ToWindow(string field, Span<byte> window)
    {
        field ??= "";
        int n = Math.Min(field.Length, window.Length);
        for (int i = 0; i < n; i++)
            window[i] = (byte)field[i];
        window[n..].Fill((byte)Space);
    }

    /// <summary>Allocates a fresh <paramref name="width"/>-byte window and encodes <paramref name="field"/> into
    /// it (Latin-1, left-justified, space-padded / right-truncated) — the IL-friendly materialization of a typed
    /// field to a scratch byte window for the byte engine (the §2.5 sender-materialize, e.g. a comparison operand).</summary>
    public static byte[] ToWindow(string field, int width)
    {
        var window = new byte[width < 0 ? 0 : width];
        ToWindow(field, window);
        return window;
    }
}
