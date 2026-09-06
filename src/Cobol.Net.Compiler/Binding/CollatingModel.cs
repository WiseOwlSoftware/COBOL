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
    /// <summary>The identity (native-order) alphabet: NATIVE, STANDARD-1, STANDARD-2 (their <see cref="Phrase"/>
    /// differs — the coded character SETS differ per Table 6 even though the sequences are all the native order).</summary>
    public static AlphabetDef Native { get; } = new(null, null, "NATIVE");

    /// <summary>True when the alphabet IS the native order — no runtime carrier is emitted for it.</summary>
    public bool IsIdentity => Table is null && Locale is null;

    /// <summary>The sequence's HIGH-VALUE character (§12.3.7.4 GR8 / §8.3.3.6.4 GR6).</summary>
    public char HighValue => Table?.HighValue ?? (Locale is not null ? (char)0xFFFF : (char)0xFF);

    /// <summary>The sequence's LOW-VALUE character (§12.3.7.4 GR9 / §8.3.3.6.4 GR7).</summary>
    public char LowValue => Table?.LowValue ?? (char)0;

    /// <summary>The CODED CHARACTER SET this alphabet defines (ISO §12.3.7.4 GR7 Table 6; kb/Work PB110), or null
    /// for a LOCALE alphabet — Table 6's one blank coded-character-set row (the reference sites raise COBOLNET1669
    /// through <c>DataBinder.CodedCharacterSetOf</c>, the ONE resolver).</summary>
    public CodedCharacterSet? CodedSet => Locale is not null ? null : new CodedCharacterSet(Phrase, National: false, Table, null);
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

    /// <summary>The CODED CHARACTER SET this alphabet defines (Table 6; null for LOCALE — see the alphanumeric twin).</summary>
    public CodedCharacterSet? CodedSet => Locale is not null ? null : new CodedCharacterSet(Phrase, National: true, null, Table);
}

/// <summary>
/// The CODED CHARACTER SET an alphabet-name references (ISO §12.3.7.4 GR7 + Table 6; kb/Work PB110 — the model
/// the four coded-set reference sites share: the class condition's alphabet-name-1, §8.8.4.4.4 GR3 a; SYMBOLIC
/// CHARACTERS … IN, GR11 b/c; CLASS … IN, GR12 a; CODE-SET, §13.18.13). A set associates each of its characters
/// with an ORDINAL POSITION (GR6), and for a literal-phrase alphabet the ordinals ARE the collating positions + 1
/// (GR7 k4's implementor determination — the unspecified characters follow the highest specified position in
/// native relative order, exactly as the collating sequence places them; documented in CONFORMANCE.md).
/// <b>Determinations:</b> the native alphanumeric set is the UTF-16 repertoire in code-unit order (ordinal n =
/// code unit n−1 — the same correspondence FUNCTION CHAR / ORD use, §15.15.4 r1 / §15.70.4 r1); STANDARD-1 /
/// STANDARD-2 are ISO/IEC 646 IRV, 128 characters, the identity correspondence on U+0000–U+007F (GR7 c);
/// NATIVE / UTF-16 national sets are the UTF-16 code units (GR7 d2/h); UCS-4 / UTF-8 are the ISO/IEC 10646
/// scalar values (GR7 f/g — an unpaired surrogate is not a character of these sets).
/// </summary>
public sealed record CodedCharacterSet(string Phrase, bool National, CollatingTable? Table, NationalCollatingTable? NatTable)
{
    /// <summary>The number of ordinal positions in the set (the §12.3.7.3 SR16 e/f and SR17 b2 range bound):
    /// 128 for STANDARD-1/2; 65 536 for the native / UTF-16 / literal-phrase sets (a literal phrase's set contains
    /// every native character — GR7 k4); 0x110000 scalar values for UCS-4 / UTF-8.</summary>
    public int OrdinalCount => Phrase switch
    {
        "STANDARD-1" or "STANDARD-2" => 128,
        "UCS-4" or "UTF-8" => 0x110000 - 0x800,   // the scalar values (surrogate code points are not characters)
        _ => 65536,
    };

    /// <summary>The character at 1-based <paramref name="ordinal"/> (GR11 b/c — SYMBOLIC CHARACTERS; GR12 a —
    /// a numeric CLASS literal under IN), as a native STRING (a UCS-4/UTF-8 supplementary character is its UTF-16
    /// surrogate pair — one character, two code units). Null when the ordinal is outside the set (the caller's
    /// range diagnostic).</summary>
    public string? CharAt(int ordinal)
    {
        if (ordinal < 1 || ordinal > OrdinalCount) return null;
        if (Table is { } t)
            return ordinal <= t.NextFree ? ((char)t.RepByPos[ordinal - 1]).ToString()
                : ((char)(256 + (ordinal - 1) - t.NextFree)).ToString();          // the GR7 k3 tail: position = NextFree + (c − 256)
        if (NatTable is { } n)
        {
            if (ordinal <= n.NextFree) return ((char)n.RepByPos[ordinal - 1]).ToString();
            // position p ≥ NextFree belongs to the unspecified code unit c with c − |specified < c| = p − NextFree
            int want = ordinal - 1 - n.NextFree;
            int specifiedBelow = 0;
            for (int c = 0; c < 65536; c++)
            {
                if (specifiedBelow < n.Codes.Length && n.Codes[specifiedBelow] == c) { specifiedBelow++; continue; }
                if (c - specifiedBelow == want) return ((char)c).ToString();
                if (c - specifiedBelow > want) break;
            }
            return null;
        }
        if (Phrase is "UCS-4" or "UTF-8")
        {
            int scalar = ordinal - 1;
            if (scalar >= 0xD800) scalar += 0x800;                                 // skip the surrogate block — not scalar values
            return char.ConvertFromUtf32(scalar);
        }
        return ((char)(ordinal - 1)).ToString();                                   // the native / STANDARD / UTF-16 identity
    }
}

/// <summary>
/// ⛔ THE COMPARISON CLASS that selects WHICH collating sequence applies to an operand — the ONE place the rule is
/// written down. ISO §8.8.4.2 gives one comparison clause per class and only two of them consult a sequence:
/// §8.8.4.2.7 (alphanumeric — the alphanumeric sequence), §8.8.4.2.9 (national — "the collating sequence of
/// characters specified for the current national program collating sequence"), §8.8.4.2.8 (boolean — "a comparison
/// of their boolean value, regardless of their usage", never collated) and §8.8.4.2.4 (numeric — algebraic, never
/// collated). §14.9.40.4 GR5 / §14.9.24.4 GR5 say the same thing for SORT/MERGE keys in one sentence: "The
/// alphanumeric collating sequence that applies to the comparison of key data items of class alphabetic and class
/// alphanumeric, and the national collating sequence that applies to the comparison of key data items of class
/// national, are each separately determined …".
/// <para>Every consumer picks its own RENDERING from this one classification — the relation-condition renderer's
/// trailing <c>__COLLATE</c>/<c>__COLLATE_NAT</c> argument, the table sort's comparer argument, the file sort's
/// runtime key descriptor — but none of them re-decides which class an operand IS (kb/Work PB678: the SORT key
/// comparator had no national arm at all, and its boolean keys took the alphanumeric weights).</para>
/// </summary>
public enum CollatingClass
{
    /// <summary>Class alphabetic / alphanumeric (and the edited categories, which compare as alphanumeric), plus
    /// every ordinary GROUP operand — §8.8.4.2.3 SR2 makes a group item class alphanumeric.</summary>
    Alphanumeric,

    /// <summary>Class national (§8.8.4.2.9) — including a national GROUP, which operates as an elementary item of
    /// PICTURE N(m) (§13.18.29.4 GR2b).</summary>
    National,

    /// <summary>Class boolean (§8.8.4.2.8) — including a bit GROUP (§13.18.29.4 GR1b).</summary>
    Boolean,

    /// <summary>Class numeric (§8.8.4.2.4) — algebraic comparison by value, regardless of usage.</summary>
    Numeric,
}

/// <summary>The classifier for <see cref="CollatingClass"/>: "which class is this operand" asked once, so no
/// consumer re-decides it.</summary>
public static class CollatingSelection
{
    /// <summary>The comparison class of an operand described by <paramref name="operandPic"/> — which shall be the
    /// item's OPERAND picture (<c>DataItem.OperandPic</c>: its own PICTURE for an elementary item, the
    /// §13.18.29.4 GR1b/GR2b as-if PICTURE for a bit / national group), never <c>Pic</c> guarded by
    /// <c>IsGroup</c>. A null picture is an ordinary (alphanumeric) group — §8.8.4.2.3 SR2.</summary>
    public static CollatingClass Of(PicInfo? operandPic) => Of(operandPic?.Category);

    /// <summary>The same rule keyed directly on a category — the form the relation-condition renderer needs, whose
    /// anchor category is derived (a figurative constant takes the OTHER operand's category, §8.3.3.6.4 GR1) and so
    /// never arrives as a <see cref="PicInfo"/>.</summary>
    public static CollatingClass Of(PicCategory? category) => category switch
    {
        PicCategory.National => CollatingClass.National,
        PicCategory.Boolean => CollatingClass.Boolean,
        PicCategory.Numeric => CollatingClass.Numeric,
        _ => CollatingClass.Alphanumeric,
    };
}

/// <summary>
/// The PAIR of collating sequences one SORT/MERGE statement resolves (ISO §14.9.40.4 GR5 / §14.9.24.4 GR5 — the two
/// are "each separately determined", in this order of precedence: a) the statement's COLLATING SEQUENCE phrase,
/// alphabet-name-1 for keys of class alphabetic and alphanumeric and alphabet-name-2 for keys of class national;
/// b) the program collating sequences). Either half is null for the native order of its class, and the two are
/// independent — a statement may name one, both or neither.
/// </summary>
/// <param name="Alphanumeric">The GR5-resolved sequence for keys of class alphabetic and alphanumeric (and for
/// ordinary group keys, §8.8.4.2.3 SR2).</param>
/// <param name="National">The GR5-resolved sequence for keys of class national.</param>
public sealed record SortCollation(AlphabetDef? Alphanumeric, NationalAlphabetDef? National)
{
    /// <summary>Both halves native — no carrier is emitted for either.</summary>
    public static SortCollation Native { get; } = new(null, null);
}
