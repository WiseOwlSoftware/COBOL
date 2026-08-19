// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The PROGRAM COLLATING SEQUENCE model (ISO §12.3.6 / §12.3.7 ALPHABET, GR7 k): a 256-entry POSITION table over
/// the Latin-1 native set (index = native char code, value = 0-based ordinal position in the user sequence), plus
/// the PCS-derived figurative extremes. Built once per alphabet by the SPECIAL-NAMES binder; an identity table
/// (NATIVE / STANDARD-1 / STANDARD-2 — ISO/IEC 646 order IS the native order here) normalizes to "no table"
/// at the PCS resolution so the native fast path costs nothing.
/// </summary>
/// <param name="Positions">Native char code → 0-based collating position. ALSO members share one position
/// (§12.3.7 GR7 k6); unspecified characters take DISTINCT ascending positions above the highest specified, in
/// native relative order (§12.3.7.4 GR7 1.3 — never a shared bucket; ORD over them must stay distinct).</param>
/// <param name="RepByPos">Indexed by position 0..<paramref name="NextFree"/>−1: the FIRST character DEFINED at
/// that position, in SOURCE order — literal-1 of an ALSO group (§15.15.4 r2's "first character defined" /
/// GR7 1.6, the CHAR representative; fix-queue PB59 — <c>AlphabetBind</c> computed this into
/// <c>specOrder</c> and discarded it, so CHAR scanned for the LOWEST-coded member instead).</param>
/// <param name="NextFree">The first position AFTER the positioned 256-block (= the number of DISTINCT
/// positions the block occupies; ALSO groups make it smaller than 256). Code unit <c>c ≥ 256</c> takes
/// position <c>NextFree + (c − 256)</c> (GR7 1.3), giving the sequence
/// <c>NextFree + (0x10000 − 256)</c> positions — the §15.15.3 r2 domain bound.</param>
/// <param name="HighValue">The runtime HIGH-VALUE character under this sequence (§12.3.7 GR8 + §8.3.3.6 GR6/7):
/// the character at the HIGHEST position; a tie (an ALSO group at the top) takes the LAST character specified.
/// ⚠ Deliberately still computed over the Latin-1 block — the documented byte-stability pin (the flagged
/// §8.3.3.6 divergence recorded in PHASE4_RECONCILIATION), not an oversight of GR7 1.3's tail.</param>
/// <param name="LowValue">The runtime LOW-VALUE character (§12.3.7 GR9): lowest position; tie takes the FIRST
/// character specified.</param>
public sealed record CollatingTable(ushort[] Positions, ushort[] RepByPos, int NextFree,
    char HighValue, char LowValue);

/// <summary>
/// A LOCALE-based collating sequence (ISO §12.3.7.2 <c>ALPHABET … IS LOCALE [locale-name-2]</c>; §8.8.4.2.11;
/// DESIGN-locale-facility §4.4.2 / kb/Work PB101): the runtime <c>LocaleCollation</c> arm of the ONE
/// <c>CobolCollation</c> carrier — the derived CLDR/UCA engine for the locale NAMED here or, when
/// <paramref name="LocaleName"/> is null, for the locale CURRENT at each use (§12.3.7.4 GR7e: "*otherwise by the
/// locale that is current at the time the collating sequence is used at runtime*").
/// </summary>
/// <param name="Locale">The ONE "which locale" operand (<see cref="LocaleRef"/>): <see cref="LocaleRef.Current"/> for the
/// phrase without a locale-name; the SPECIAL-NAMES locale-name (§12.3.7.3 SR24) whose external identification binds
/// the sequence for <c>IS LOCALE locale-name-2</c> — its normalized tag is what the emitted carrier holds, and its
/// availability is decided at use (EC-LOCALE-MISSING; L1 item 4).</param>
public sealed record LocaleCollatingSpec(LocaleRef Locale)
{
    /// <summary>The phrase without a locale-name — the locale current at each use.</summary>
    public static LocaleCollatingSpec CurrentLocale { get; } = new(LocaleRef.Current);
}

/// <summary>
/// One SPECIAL-NAMES ALPHANUMERIC alphabet (an <c>ALPHABET</c> clause without FOR NATIONAL, ISO §12.3.7): what the
/// name references — the compile-time twin of the runtime's ONE <c>CobolCollation</c> carrier. Exactly one of the
/// arms is set for a NON-identity sequence: <paramref name="Table"/> for a literal phrase, <paramref name="Locale"/>
/// for the LOCALE phrase; both null for the identity sequences NATIVE / STANDARD-1 / STANDARD-2 (ISO/IEC 646
/// order IS the native order here — no carrier is emitted and the native fast path costs nothing).
/// <see cref="HighValue"/>/<see cref="LowValue"/> are the sequence's §12.3.7.4 GR8/GR9 extremes: a table's are
/// computed by the binder; a LOCALE sequence's are U+FFFF / U+0000 under EVERY CLDR/UCA table (U+FFFF carries the
/// maximum primary, U+0000 is completely ignorable — the runtime's <c>LocaleCollation</c> materializes the same
/// answer, DESIGN-locale-facility L7), so the figurative constants can still fold at compile time.
/// </summary>
public sealed record AlphabetDef(CollatingTable? Table, LocaleCollatingSpec? Locale, string Phrase)
{
    /// <summary>The identity (native-order) alphabet: NATIVE, STANDARD-1, STANDARD-2.</summary>
    public static AlphabetDef Native { get; } = new(null, null, "NATIVE");

    /// <summary>True when the alphabet IS the native order — no runtime carrier is emitted for it.</summary>
    public bool IsIdentity => Table is null && Locale is null;

    /// <summary>The sequence's HIGH-VALUE character (§12.3.7.4 GR8 / §8.3.3.6.4 GR6).</summary>
    public char HighValue => Table?.HighValue ?? (Locale is not null ? (char)0xFFFF : (char)0xFF);

    /// <summary>The sequence's LOW-VALUE character (§12.3.7.4 GR9 / §8.3.3.6.4 GR7).</summary>
    public char LowValue => Table?.LowValue ?? (char)0;
}

/// <summary>
/// A NATIONAL collating sequence built from an <c>ALPHABET … FOR NATIONAL</c> literal phrase (ISO §12.3.7 GR7 k,
/// applied over the native NATIONAL character set — the 65,536 UTF-16 code units, one per national position,
/// D-N1). SPARSE by design: only the SPECIFIED characters are tabulated; every unspecified code unit takes a
/// DISTINCT ascending position above the highest specified one, in native (code-unit) relative order (§12.3.7.4 GR7 1.3),
/// which the runtime computes arithmetically (<c>CobolNet.Runtime.NationalCollation.Weight</c>) — a dense
/// 65,536-entry table would bloat every generated program for a handful of remapped characters.
/// </summary>
/// <param name="Codes">The specified code units, sorted ASCENDING BY CODE (the runtime's binary-search key).</param>
/// <param name="Positions">Parallel to <paramref name="Codes"/>: each specified code's 0-based collating
/// position. ALSO members share one position (GR7 k6).</param>
/// <param name="RepByPos">Indexed by specified position 0..<paramref name="NextFree"/>−1: the FIRST character
/// SPECIFIED at that position (source order — §15.15.4 r2's "first character defined", the CHAR-NATIONAL
/// representative; also the GR9 LOW-VALUE tie rule).</param>
/// <param name="NextFree">The first position AFTER the specified block (= the number of DISTINCT specified
/// positions; ALSO groups make it smaller than <c>Codes.Length</c>). Unspecified code unit <c>c</c> takes
/// position <c>NextFree + (c − |specified codes &lt; c|)</c>.</param>
/// <param name="HighValue">The national HIGH-VALUE character under this sequence (§12.3.7 GR8): the character
/// at the highest position — the LARGEST unspecified code unit (unspecified characters sit above all specified
/// ones, §12.3.7.4 GR7 1.3), or, when every code unit is specified, the GR8 tie rule over the specified block.</param>
/// <param name="LowValue">The national LOW-VALUE character (§12.3.7 GR9): the character at position 0 — the
/// first character specified (a position-0 ALSO tie takes the FIRST specified, which is the same character).</param>
public sealed record NationalCollatingTable(ushort[] Codes, ushort[] Positions, ushort[] RepByPos, int NextFree,
    char HighValue, char LowValue);

/// <summary>
/// One SPECIAL-NAMES national alphabet (an <c>ALPHABET … FOR NATIONAL</c> clause, ISO §12.3.7): what the name
/// references per Table 6 — a coded character set, a collating sequence, or both.
/// <list type="bullet">
/// <item><c>NATIVE</c> — the native national coded character set AND collating sequence (GR7 d2): identity,
/// no table.</item>
/// <item><c>UCS-4</c> — the ISO/IEC 10646 UTF-32 coded character set AND the ISO/IEC 10646 appearance-order
/// collating sequence (GR7 f; Table 6 row UCS-4: both). On the D-N1 substrate the collating sequence is the
/// IDENTITY: the implementor correspondence (implementor item 188) maps UTF-32 character U+0000..U+FFFF to the
/// SAME native code unit, and §8.5.1.4 makes each UTF-16 code element its OWN character position with "no
/// special handling or recognition of surrogate pairs" — so the codepoint-vs-code-unit divergence above
/// U+FFFF (a surrogate PAIR weighed as one supplementary codepoint) is UNREACHABLE in COBOL's per-position
/// comparison model, and ISO 10646 order over the 65,536 single-position characters IS code-unit order (D-N3).</item>
/// <item><c>UTF-16</c> / <c>UTF-8</c> — coded character sets ONLY (Table 6: their collating-sequence column is
/// EMPTY): legal where an alphabet references a coded character set (CODE-SET, SYMBOLIC … IN, CLASS … IN), an
/// SR violation where a collating sequence is required (§12.3.6 SR2, §12.4.5.7, SORT/MERGE). UTF-16 as a coded
/// set is the D-N1 native identity; UTF-8's external form matters only at a codec boundary (none exists yet —
/// the CODE-SET clause has no compiler surface, so declaring it is well-formed but inert).</item>
/// <item>literal-phrase — a user collating sequence AND coded character set (Table 6 last row): the sparse
/// <see cref="NationalCollatingTable"/>.</item>
/// <item><c>LOCALE [locale-name-2]</c> — a locale-based national collating sequence (Table 6 row LOCALE: a
/// collating sequence, NOT a coded character set — §12.3.7.3 SR16g/SR17d forbid it in CODE-SET / SYMBOLIC / CLASS):
/// the <see cref="LocaleCollatingSpec"/> arm; DETERMINATION L5 — one locale sequence serves both classes on the
/// D-N1 substrate, so the same runtime <c>LocaleCollation</c> is emitted.</item>
/// </list>
/// </summary>
/// <param name="Table">The non-identity collating table (literal phrase), or null (NATIVE/UCS-4/UTF-16/UTF-8/LOCALE
/// — identity, not-a-collating-sequence, or the locale arm).</param>
/// <param name="Locale">The LOCALE phrase's arm, or null.</param>
/// <param name="HasCollatingSequence">Table 6's collating-sequence column: false for UTF-8/UTF-16 (coded
/// character set only) — referencing such an alphabet as a collating sequence is the SR violation.</param>
/// <param name="Phrase">The defining phrase, for diagnostics ("NATIVE", "UCS-4", "UTF-8", "UTF-16",
/// "literal-phrase", "LOCALE").</param>
public sealed record NationalAlphabetDef(NationalCollatingTable? Table, LocaleCollatingSpec? Locale, bool HasCollatingSequence, string Phrase)
{
    /// <summary>A national alphabet without a locale arm (the pre-PB101 constructor shape).</summary>
    public NationalAlphabetDef(NationalCollatingTable? Table, bool HasCollatingSequence, string Phrase)
        : this(Table, null, HasCollatingSequence, Phrase) { }

    /// <summary>True when the alphabet is an identity sequence or no sequence at all — no runtime carrier.</summary>
    public bool IsIdentity => Table is null && Locale is null;

    /// <summary>The national HIGH-VALUE character (§12.3.7.4 GR8): the table's, else U+FFFF (a LOCALE sequence's
    /// maximum and the native national pin alike).</summary>
    public char HighValue => Table?.HighValue ?? (char)0xFFFF;

    /// <summary>The national LOW-VALUE character (§12.3.7.4 GR9): the table's, else U+0000.</summary>
    public char LowValue => Table?.LowValue ?? (char)0;
}
