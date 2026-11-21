// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE ONE DEFINITION OF WHAT A SOURCE ELEMENT SAYS OF AN EXTERNAL RECORD, as the run unit compares it (kb/Work PB5996): the
/// run time's own <see cref="ExternalDescriptor"/> of kind <c>record</c>, composed here and nowhere else. Two checks read it:
/// the compile-time agreement of the descriptions in one compilation group (<see cref="ExternalRecordDescriptionRules"/>,
/// COBOL7735) and the run-time registration each describer makes at its activation entry
/// (<see cref="CallExternalBacking.Descriptor"/>, rendered by <c>OoEmitter.EmitExternalDescribes</c> and compared by
/// <c>ExternalTable.Describe</c>, EC-EXTERNAL-FORMAT-CONFLICT), so "the same description" means one thing at compile time and at
/// run time.
/// <para>The facts are the rule's: the records "must define the same number of standard data format characters"
/// (§1985:X.4.5.4 GR2), "the VALUE clause specification, if any, for each record name of the associated record description
/// entries shall be identical; and the records shall define the same number of bytes" (§2002:13.16.20.3 GR4,
/// §2014:13.18.22.3 GR4, §2023:13.18.22.4 GR6), and for a strongly typed record "the same corresponding external strong type
/// declarations and the same presence or absence of the CONSTANT RECORD clause" (§2023:14.8.4.3). The record-name half of
/// each rule is the key the descriptions are grouped by, the externalized name (§2023:13.18.22.4 GR5). A complete
/// REDEFINES of the record contributes nothing: each text exempts it ("this complete redefinition need not occur
/// identically"), so the description is built from the base record only.</para>
/// </summary>
internal static class ExternalRecordDescription
{
    /// <summary>The description <paramref name="backing"/>'s source element gives of its external record: its size in the
    /// characters (bytes) of its one shared image, the record name's OWN VALUE clause specification as a canonical key
    /// (<see cref="ValueSpecificationKey"/>), the external strong TYPE name (upper-folded), and the CONSTANT RECORD presence.</summary>
    public static ExternalDescriptor Of(CallExternalBacking backing) => new(
        "record",
        ByteCount: backing.Width,
        ValueImage: backing.Record.RawValue is { } rv ? ValueSpecificationKey(rv) : null,
        StrongTypeKey: backing.Record.StrongType && backing.Record.TypeName is { } tn ? CobolNames.UpperFold(tn) : null,
        ConstantRecord: backing.Record.IsConstantRecord);

    /// <summary>Whether the record's description defines its size. A variable-length record, one holding a dynamic-length item or
    /// a dynamic-capacity table (<see cref="FileModel.IsVariableLengthRecord"/>, §2014:8.5.1.9), has its number of bytes set at
    /// run time, so its <see cref="ExternalDescriptor.ByteCount"/> is not a size its description defines, and a description of
    /// its table that does not agree with the table's internal format is the run time's EC-DATA-INCOMPATIBLE (§2014:14.6.12.2,
    /// its rule 6).</summary>
    public static bool DefinesItsSize(CallExternalBacking backing) => !FileModel.IsVariableLengthRecord(backing.Record);

    /// <summary>⛔ THE VALUE-IDENTITY KEY OF A RECORD NAME'S VALUE CLAUSE (kb/Work PB1236). §2023:13.18.22.4 GR6 compares the
    /// specification, and a literal's CHARACTERS are its content: "ABCD" and "abcd" are two different VALUE clauses. Only the
    /// WORDS of the clause are case-insensitive — a figurative constant's keyword (SPACE / space, ALL, the N / B / X literal
    /// prefix) and a hexadecimal literal's digits (X"4a" is X"4A") — so those are folded and a quoted literal's text is kept
    /// exactly.</summary>
    internal static string ValueSpecificationKey(string raw)
    {
        int q = raw.IndexOfAny(['"', (char)39]);
        if (q < 0) return raw.ToUpperInvariant();                    // no literal text at all: keywords and numbers
        string head = raw[..q].ToUpperInvariant();
        string body = raw[q..];
        return head.TrimEnd().EndsWith('X') ? head + body.ToUpperInvariant() : head + body;   // X"…" hex digits: any case
    }
}
