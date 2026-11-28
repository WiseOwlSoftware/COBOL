// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding;

public sealed partial class SymbolTable
{
    /// <summary>The SOURCE-form keys of <paramref name="file"/> that the written reference <paramref name="written"/> names — the ONE
    /// answer to ISO §2023:14.9.30.3 SR11 / §2023:14.9.41.3 SR7 / §2023:12.4.5.7.3 SR5's "specified as a record-key-name … in the
    /// file control entry", as a COUNTED set (prime key first, then the alternates in declaration order). A record-key-name is its
    /// own name class (§2023:8.3.2.2.24), scoped by its file control entry (§2023:8.4.6.2.4), and its declarations are the entry's
    /// key clauses, so the table reads them off the file that carries them (section 6.7.1, "A name a SYMBOL carries is resolved
    /// by the table from the symbol"; DESIGN-listing.md section 6.7.1, "Step 6b, remainder slice 2"). The reference's IN/OF list:
    /// §2023:8.4.2.2.2 Format 6 (qualified-record-key-name) qualifies a record-key-name by ONE file-name, so a reference qualified
    /// by anything but this file's own name names no key of it.
    /// <para>⛔ A SET, NEVER "THE FIRST MATCH" (kb/Work PB1099 review R1-2). Nothing forbids two of this entry's clauses declaring
    /// the same record-key-name (§2023:8.3.2.2), and Format 6's only qualifier is the file-name both share, so no reference can
    /// single either out: §2023:8.4.2.2.3 SR1's error, reported by the caller's one ambiguity verdict. Returning the first match
    /// bound such a reference silently to the prime key.</para></summary>
    internal NameCandidates<KeyNameReading> RecordKeyNamesOf(FileModel file, WrittenDataName written)
    {
        List<KeyNameReading> named = [];
        if (!RecordKeyNameQualifiersAdmit(file, written.Qualifiers)) return new(named);
        var key = Key(written.Name);
        if (file.PrimeKey is { IsSourceForm: true } pk && Key(pk.RecordKeyName!) == key)
            named.Add(new KeyNameReading.RecordKeyName(file, -1));
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (file.AlternateKeys[i].Key is { IsSourceForm: true } alt && Key(alt.RecordKeyName!) == key)
                named.Add(new KeyNameReading.RecordKeyName(file, i));
        return new(named);
    }

    /// <summary>Whether a key clause of <paramref name="file"/>'s entry that did NOT resolve (an operand referencing nothing
    /// describable, already reported by that clause's own SR2 row) is the one a key-level COLLATING SEQUENCE operand
    /// <paramref name="written"/> was written to name — so the operand is not reported a second time as "not a key of this file"
    /// (one fault, one verdict). A data-name-1 clause matches by spelling (its operand names nothing, so no qualification can be
    /// judged); a SOURCE-form clause matches its record-key-name under Format 6's qualifier rule. ⛔ A SOURCE-form clause is
    /// unresolved when one of ITS OPERANDS is — never because <see cref="AlternateKeyClause.Item"/> is null, which it always is for
    /// that form (kb/Work PB1099 review R1-1: every SOURCE alternate read as unresolved, so <c>COLLATING SEQUENCE OF AK IN
    /// another-file</c> was dropped unreported). A declaration question: no symbol answers.</summary>
    internal bool DeclaresUnresolvedKeyNamed(FileModel file, WrittenDataName written)
    {
        var key = Key(written.Name);
        bool Matches(WrittenDataName clauseName, RecordKeySource? source) =>
            Key(clauseName.Name) == key && (source is null || RecordKeyNameQualifiersAdmit(file, written.Qualifiers));
        static bool Unresolved(DataItem? item, RecordKeySource? source) =>
            source is null ? item is null : source.Operands.Any(o => o.Item is null);
        return file.RecordKeyOperand is { } rk && file.PrimeKey is null && Matches(rk, file.RecordKeySource)
            || file.AlternateKeyNames.Any(a => Unresolved(a.Item, a.Source) && Matches(a.Operand, a.Source));
    }

    /// <summary>Whether a record-key-name reference's IN/OF list can name a key of <paramref name="file"/>: none, or §2023:8.4.2.2.2
    /// Format 6's one file-name qualifier naming this file.</summary>
    private static bool RecordKeyNameQualifiersAdmit(FileModel file, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count == 0
        || qualifiers.Count == 1 && (Key(qualifiers[0]) == Key(file.SelectName) || Key(qualifiers[0]) == Key(file.CobolName));
}
