// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>
/// One bit per clause slot of the ISO §13.16.2 Format-1 data description entry — the vocabulary the §13.16.3
/// "…shall not be specified in the same data description entry with…" rules are written in.
///
/// <para>⛔ <b>The general format is a CLOSED list</b> (rendered from the printed page, PDF p393 / printed folio
/// 363): level-number, entry-name, REDEFINES, IS TYPEDEF [STRONG], ALIGNED, ANY LENGTH, BASED, BLANK WHEN ZERO,
/// CONSTANT RECORD, DYNAMIC LENGTH, IS EXTERNAL [AS], IS GLOBAL, GROUP-USAGE, JUSTIFIED, occurs-clause,
/// picture-clause, PROPERTY, SAME AS, select-when-clause, SIGN, SYNCHRONIZED, TYPE, usage-clause,
/// validation-clauses, value-clause. Level-number and entry-name are not <c>dataDescriptionClause</c>
/// alternatives (they are separate elements of <c>dataDescriptionEntry</c>) and so have no bit here; every other
/// slot does, plus <see cref="Unrecognized"/> for the error production that replaced the vendor catch-all
/// (kb/Work PB487).</para>
///
/// <para>⛔ <b>Do not add a member without adding its row to <see cref="DataClauseKinds.ByContextType"/></b> —
/// and do not add a <c>dataDescriptionClause</c> alternative without adding both. <c>DataClauseKindDriftTests</c>
/// reflects over the generated parser context and fails when the grammar and this enum disagree in EITHER
/// direction, which is what keeps the permitted-set rules complete as the grammar grows.</para>
/// </summary>
[Flags]
public enum DataClauseKind : uint
{
    None = 0,

    /// <summary>PICTURE (§13.18.40).</summary>
    Picture = 1u << 0,
    /// <summary>USAGE (§13.18.60).</summary>
    Usage = 1u << 1,
    /// <summary>OCCURS (§13.18.38).</summary>
    Occurs = 1u << 2,
    /// <summary>REDEFINES (§13.18.44).</summary>
    Redefines = 1u << 3,
    /// <summary>VALUE (§13.18.63).</summary>
    Value = 1u << 4,
    /// <summary>SIGN (§13.18.52).</summary>
    Sign = 1u << 5,
    /// <summary>SYNCHRONIZED / SYNC (§13.18.55).</summary>
    Synchronized = 1u << 6,
    /// <summary>JUSTIFIED / JUST (§13.18.32).</summary>
    Justified = 1u << 7,
    /// <summary>BLANK WHEN ZERO (§13.18.9).</summary>
    BlankWhenZero = 1u << 8,
    /// <summary>ALIGNED (§13.18.1).</summary>
    Aligned = 1u << 9,
    /// <summary>CONSTANT RECORD (§13.18.15).</summary>
    ConstantRecord = 1u << 10,
    /// <summary>PROPERTY (§13.18.42).</summary>
    Property = 1u << 11,
    /// <summary>IS EXTERNAL [AS literal-1] (§13.18.22).</summary>
    External = 1u << 12,
    /// <summary>IS GLOBAL (§13.18.27).</summary>
    Global = 1u << 13,
    /// <summary>TYPE type-name-1 (§13.18.57).</summary>
    Type = 1u << 14,
    /// <summary>IS TYPEDEF [STRONG] (§13.18.58).</summary>
    Typedef = 1u << 15,
    /// <summary>SAME AS data-name-2 (§13.18.49).</summary>
    SameAs = 1u << 16,
    /// <summary>BASED (§13.18.5).</summary>
    Based = 1u << 17,
    /// <summary>ANY LENGTH (§13.18.2).</summary>
    AnyLength = 1u << 18,
    /// <summary>DYNAMIC LENGTH (§13.18.19).</summary>
    DynamicLength = 1u << 19,
    /// <summary>GROUP-USAGE (§13.18.29).</summary>
    GroupUsage = 1u << 20,
    /// <summary>select-when-clause (§13.18.51).</summary>
    SelectWhen = 1u << 21,
    /// <summary>validation-clauses — CLASS, DEFAULT, DESTINATION, INVALID WHEN, PRESENT WHEN, VARYING and
    /// validate-status, which §13.16.2 names as ONE meta-language term and §13.16.3 SR13/SR14 treat as one
    /// group.</summary>
    Validation = 1u << 22,
    /// <summary>⛔ NOT A CLAUSE — the <c>unrecognizedDataClause</c> error production (kb/Work PB487). It carries a
    /// bit so the permitted-set rules SEE it: an entry whose only defect is an unrecognized word draws
    /// COBOLNET1941 alone, and one that ALSO violates SR17 draws both, rather than the unknown word masking the
    /// composition rule or the reverse.</summary>
    Unrecognized = 1u << 23,
}

/// <summary>The grammar-alternative → <see cref="DataClauseKind"/> map, and the §13.16.3 permitted-set constants
/// derived from it. ⛔ ONE declaration: <see cref="DataDescriptionClauseCst.Kind"/> reads it and
/// <c>DataClauseKindDriftTests</c> checks it against the generated parser, so there is no second copy to drift.</summary>
public static class DataClauseKinds
{
    /// <summary>Every alternative of the <c>dataDescriptionClause</c> grammar rule, by the context type ANTLR
    /// generates for it. A <see cref="FrozenDictionary{TKey,TValue}"/> rather than a type-pattern <c>switch</c>
    /// because it must be ENUMERABLE — the drift test compares its key set against the generated context's
    /// sub-rule accessors, which a switch body cannot expose. Data-division binding walks a handful of clauses
    /// per entry, so the lookup is not on any hot path.</summary>
    public static readonly FrozenDictionary<Type, DataClauseKind> ByContextType =
        new Dictionary<Type, DataClauseKind>
        {
            [typeof(Core.PictureClauseContext)] = DataClauseKind.Picture,
            [typeof(Core.UsageClauseContext)] = DataClauseKind.Usage,
            [typeof(Core.OccursClauseContext)] = DataClauseKind.Occurs,
            [typeof(Core.RedefinesClauseContext)] = DataClauseKind.Redefines,
            [typeof(Core.ValueClauseContext)] = DataClauseKind.Value,
            [typeof(Core.SignClauseContext)] = DataClauseKind.Sign,
            [typeof(Core.SyncClauseContext)] = DataClauseKind.Synchronized,
            [typeof(Core.JustifiedClauseContext)] = DataClauseKind.Justified,
            [typeof(Core.BlankWhenZeroClauseContext)] = DataClauseKind.BlankWhenZero,
            [typeof(Core.AlignedClauseContext)] = DataClauseKind.Aligned,
            [typeof(Core.ConstantRecordClauseContext)] = DataClauseKind.ConstantRecord,
            [typeof(Core.PropertyClauseContext)] = DataClauseKind.Property,
            [typeof(Core.ExternalClauseContext)] = DataClauseKind.External,
            [typeof(Core.GlobalClauseContext)] = DataClauseKind.Global,
            [typeof(Core.TypeClauseContext)] = DataClauseKind.Type,
            [typeof(Core.TypedefClauseContext)] = DataClauseKind.Typedef,
            [typeof(Core.SameAsClauseContext)] = DataClauseKind.SameAs,
            [typeof(Core.BasedClauseContext)] = DataClauseKind.Based,
            [typeof(Core.AnyLengthClauseContext)] = DataClauseKind.AnyLength,
            [typeof(Core.DynamicLengthClauseContext)] = DataClauseKind.DynamicLength,
            [typeof(Core.GroupUsageClauseContext)] = DataClauseKind.GroupUsage,
            [typeof(Core.SelectWhenClauseContext)] = DataClauseKind.SelectWhen,
            [typeof(Core.ValidationClauseContext)] = DataClauseKind.Validation,
            [typeof(Core.UnrecognizedDataClauseContext)] = DataClauseKind.Unrecognized,
        }.ToFrozenDictionary();

    /// <summary>The clause words as the STANDARD spells them, for a diagnostic that must NAME the offending
    /// clause rather than say "some other clause". Keyed by the single bit; <see cref="Name"/> is the accessor.</summary>
    private static readonly (DataClauseKind Bit, string Word)[] Words =
    [
        (DataClauseKind.Picture, "PICTURE"),
        (DataClauseKind.Usage, "USAGE"),
        (DataClauseKind.Occurs, "OCCURS"),
        (DataClauseKind.Redefines, "REDEFINES"),
        (DataClauseKind.Value, "VALUE"),
        (DataClauseKind.Sign, "SIGN"),
        (DataClauseKind.Synchronized, "SYNCHRONIZED"),
        (DataClauseKind.Justified, "JUSTIFIED"),
        (DataClauseKind.BlankWhenZero, "BLANK WHEN ZERO"),
        (DataClauseKind.Aligned, "ALIGNED"),
        (DataClauseKind.ConstantRecord, "CONSTANT RECORD"),
        (DataClauseKind.Property, "PROPERTY"),
        (DataClauseKind.External, "EXTERNAL"),
        (DataClauseKind.Global, "GLOBAL"),
        (DataClauseKind.Type, "TYPE"),
        (DataClauseKind.Typedef, "TYPEDEF"),
        (DataClauseKind.SameAs, "SAME AS"),
        (DataClauseKind.Based, "BASED"),
        (DataClauseKind.AnyLength, "ANY LENGTH"),
        (DataClauseKind.DynamicLength, "DYNAMIC LENGTH"),
        (DataClauseKind.GroupUsage, "GROUP-USAGE"),
        (DataClauseKind.SelectWhen, "SELECT WHEN"),
        (DataClauseKind.Validation, "a validation clause"),
        (DataClauseKind.Unrecognized, "an unrecognized word"),
    ];

    /// <summary>The clause words present in <paramref name="set"/>, comma-separated in §13.16.2 format order —
    /// the operand of "…shall not be specified…" made concrete. Empty string for <see cref="DataClauseKind.None"/>.</summary>
    public static string Name(DataClauseKind set)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (bit, word) in Words)
            if ((set & bit) != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(word);
            }
        return sb.ToString();
    }

    // ── The §13.16.3 permitted / excluded sets, one constant per rule ──────────────────────────────────────
    // Each is transcribed from the rule's own sentence; level-number and entry-name are always permitted and
    // carry no bit. `Unrecognized` is in NO permitted set — an unrecognized word is never a permitted co-clause,
    // so an entry carrying one draws its own COBOLNET1941 and does not silently satisfy a permitted-set rule.

    /// <summary>§13.16.3 SR12: "The SAME AS clause shall not be specified in the same data description entry
    /// with any clauses except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS."
    /// CO-permitted: the SAME AS bit itself is the subject and is OR-ed in at the check site.</summary>
    public const DataClauseKind SameAsCoPermitted =
        DataClauseKind.ConstantRecord | DataClauseKind.External
        | DataClauseKind.Global | DataClauseKind.Occurs;

    /// <summary>§13.16.3 SR13 ¶1: "The ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when,
    /// SYNCHRONIZED, and TYPEDEF clauses and validation-clauses shall not be specified in the same data
    /// description entry with the CONSTANT RECORD clause…"</summary>
    public const DataClauseKind ConstantRecordExcluded =
        DataClauseKind.AnyLength | DataClauseKind.Based | DataClauseKind.BlankWhenZero
        | DataClauseKind.DynamicLength | DataClauseKind.SelectWhen | DataClauseKind.Synchronized
        | DataClauseKind.Typedef | DataClauseKind.Validation;

    /// <summary>§13.16.3 SR17: "If the ANY LENGTH clause is specified, the only other clauses permitted are
    /// level-number, entry-name, PICTURE, USAGE, and VALUE." SR18 says the same of DYNAMIC LENGTH, so ONE
    /// constant serves both rules — they are the same set, and writing it twice is how the two drifted apart
    /// before (kb/Work PB487).
    /// <para>⛔ CO-permitted, so neither length clause is in it: the subject's OWN bit is OR-ed in at the check
    /// site, which is exactly what makes <c>ANY LENGTH DYNAMIC LENGTH</c> on one entry a violation of BOTH rules
    /// rather than of neither.</para></summary>
    public const DataClauseKind LengthClauseCoPermitted =
        DataClauseKind.Picture | DataClauseKind.Usage | DataClauseKind.Value;
}
