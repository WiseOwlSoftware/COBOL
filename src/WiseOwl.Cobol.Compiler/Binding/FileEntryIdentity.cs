// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>The §2023:12.4.5.3 GR1 identity of an EXTERNAL file connector's file control entry, built at the end of the bind
/// (<c>BackendAnswers</c>) into <see cref="FileModel.SelectFingerprint"/>: it describes each record key with the binder's one
/// description builder (<see cref="ActivationDescriptions"/>), so the backend renders a value and asks no binder rule
/// (kb/Work PB2403).</summary>
internal static class FileEntryIdentity
{
    /// <summary>⛔ THE ONE §2023:12.4.5.3 GR1 IDENTITY of an external file connector's file control entry — the string two
    /// describers' <c>ExternalStore.Describe</c> registrations compare for EC-EXTERNAL-FILE-MISMATCH (§2023:14.8.4.4: "the
    /// rules specified in 12.4.5, File control entry General rule 1 apply"). One segment per GR1 item that the entry
    /// can make differ: a) OPTIONAL, b) the ASSIGN operands, c) the RECORD DELIMITER phrase, d) RESERVE integer-1,
    /// e) organization, f) access mode, g) the COLLATING SEQUENCE clauses, j) the prime key's description and relative
    /// location, k) each alternate key's description, relative location, DUPLICATES and SUPPRESS WHEN phrase (and so
    /// their number), l) sharing mode, m) lock mode. h) RELATIVE KEY and i) FILE STATUS are the §2023:14.8.4.2 external-item
    /// references the descriptor carries beside it (<c>RelativeKeyRef</c> / <c>FileStatusRef</c>). The split-key
    /// <c>SOURCE IS</c> operands of j)/k) ("each data-name-6" / "each data-name-3") enter as the record-key-name
    /// followed by each operand's own entry in order (kb/Work PB1099). Every segment is built from the bound <see cref="FileModel"/>; kb/Work
    /// PB1079 measured c), d), g), j)'s and k)'s descriptions and locations, and k)'s SUPPRESS WHEN missing from it,
    /// so a mismatch in any of them ran the activated program with no condition.
    /// <para>⛔ The SAME segments decide the rule at compile time for the describers of one compilation group
    /// (<see cref="ExternalFileConnectorRules"/>, COBOL6847, kb/Work PB5069), so the compile-time and the run-time checks
    /// cannot disagree on what "the same specification" is, and a segment added here is checked by both.</para></summary>
    public static IReadOnlyList<FileEntrySegment> Segments(FileModel f)
    {
        // §2023:12.4.5.3 GR1 j)/k) "The same data description entry for data-name-… as well as their relative location
        // within the associated record": the key's name as written (part of its data description entry), its
        // elementary/group shape and byte extent, its §2023:14.8 conformance description (category, usage, PICTURE clause
        // identity, sign) and its byte offset within the record area (RecordLayout.OffsetOf, the same offset the
        // connector registers the key window at).
        static string KeyEntry(string name, DataItem? item) => item is null ? CobolNames.UpperFold(name)
            : $"{CobolNames.UpperFold(name)}@{RecordLayout.OffsetOf(item)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?"}"
              + $":{(item.IsGroup ? "G" : "E")}{item.ByteWidth}:"
              + (ActivationDescriptions.Of(item, classes: null) is { } d ? $"{d.Category}/{d.Clauses}/{d.Positions}" : "-");
        // j)/k) "each data-name-6" / "each data-name-3": a SOURCE-form key is its record-key-name and every operand's
        // entry in the order specified, since that order IS the key (§2023:12.4.5.12.4 GR2).
        static string ClauseKey(string name, RecordKeySource? source, DataItem? item) => source is null
            ? KeyEntry(name, item)
            : $"{CobolNames.UpperFold(source.Name)}=SOURCE[{string.Join("+", source.Operands.Select(o => KeyEntry(o.Name, o.Item)))}]";
        // k) "the same SUPPRESS WHEN phrase" — the operand as the literal position read it (form, class, characters,
        // figurative kind), so `SUPPRESS WHEN "XX"` and `SUPPRESS WHEN SPACE` differ and an absent phrase is empty.
        static string Suppress(SuppressWhenOperand? s) => s is null ? ""
            : $"{s.Form}/{s.Class}/{s.FigurativeKind}/{s.Characters.Length}:{s.Characters}";
        // g) "The same specification of COLLATING SEQUENCE clauses": the file-level clause's alphabet-names, and each
        // key-level clause's alphabet-name-3 against the KEY POSITION it names (P = the prime key, A<i> = the i-th
        // alternate), so the rule compares what the clause says about the file's keys; sorted, because the clauses'
        // order in the entry is not part of their specification.
        // The operand is matched by the key ITEM the binder resolved it to (kb/Work PB1075 — a qualified operand).
        string KeyPosition(CollatingKeyOperand k)
        {
            if (k.Key is not null && ReferenceEquals(f.PrimeKey, k.Key)) return "P";
            int i = k.Key is null ? -1 : f.AlternateKeys.FindIndex(a => ReferenceEquals(a.Key, k.Key));
            return i >= 0 ? $"A{i}" : CobolNames.UpperFold(k.Name);
        }
        string fileColl = f.FileLevelCollating is { } fc ? $"{(fc.Alnum is { } an ? CobolNames.UpperFold(an) : null)}/{(fc.Nat is { } nn ? CobolNames.UpperFold(nn) : null)}" : "";
        string keyColl = string.Join(",", f.KeyLevelCollating
            .SelectMany(c => c.Keys.Select(k => $"{KeyPosition(k)}={CobolNames.UpperFold(c.Alphabet)}"))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        // b) "A consistent specification for data-name-1, device-name-1, and literal-1 in the ASSIGN clause" — all
        // THREE operands, so the USING data-name is part of the identity, not just the TO target. Consistency rule
        // (the implementor's, GR1 b's second sentence; docs/CONFORMANCE.md §7 DOC-A.1-72): the same data-name
        // spelling, qualifiers included.
        return
        [
            new("OPT", "the OPTIONAL phrase", $"{f.Optional}"),
            new("ASSIGN", "the ASSIGN clause", f.AssignTarget.ToUpperInvariant()),
            new("USING", "the ASSIGN clause's USING phrase",
                CobolNames.UpperFold(string.Join(" OF ", new[] { f.AssignUsingName ?? "" }.Concat(f.AssignUsingQualifiers)))),
            // c) "Either the STANDARD-1 phrase or a consistent value of feature-name-1" — consistency rule: the same
            // phrase (the clause is declined accept-inert, kb/Work PB292, so the written phrase is all there is).
            new("DELIM", "the RECORD DELIMITER clause", f.RecordDelimiter ?? ""),
            new("RESERVE", "the RESERVE clause", $"{f.ReserveAreas}"),
            new("ORG", "the organization", $"{f.Organization}"),
            new("ACC", "the access mode", $"{f.AccessMode}"),
            new("COLL", "the COLLATING SEQUENCE clauses", $"{fileColl};{keyColl}"),
            new("KEY", "the RECORD KEY clause", f.RecordKeyName is { } rk ? ClauseKey(rk, f.RecordKeySource, f.PrimeKey?.Item) : ""),
            new("ALT", "the ALTERNATE RECORD KEY clauses",
                string.Join(",", f.AlternateKeyNames.Select(a => $"{ClauseKey(a.Name, a.Source, a.Item)}:{a.Duplicates}:{Suppress(a.SuppressWhen)}"))),
            new("SHARE", "the sharing mode", $"{f.Sharing}"),
            new("LOCK", "the lock mode", f.LockMode is { } lm ? $"{lm.Kind},{lm.Multiple}" : ""),
            // §1985:VII.2.7.4 GR6 / §2002:12.3.4.3 GR1 h) "The same specification of the PADDING CHARACTER clause" (kb/Work
            // PB5069): literal-1's character, or data-name-1's corresponding external data item (its identity, the
            // RELATIVE KEY rule's reading of "the same"). Empty where no clause is written, so the editions that have no
            // such clause (2014 and later) compare equal.
            new("PAD", "the PADDING CHARACTER clause", f.Padding switch
            {
                null => "",
                { DataName: { } pn } p => "@" + (p.Item?.ExternalItemIdentity() ?? CobolNames.UpperFold(pn)),
                { Literal: { } lit } => "=" + lit,
                _ => "",
            }),
        ];
    }

    /// <summary>The identity as one string (<see cref="FileModel.SelectFingerprint"/>), the segments joined in order.</summary>
    public static string Of(FileModel f) => string.Join("|", Segments(f).Select(s => $"{s.Key}={s.Value}"));
}

/// <summary>One §2023:12.4.5.3 GR1 item of an external file control entry's identity: its key in the fingerprint, the
/// clause it is (for a diagnostic), and its value as the entry specifies it.</summary>
internal readonly record struct FileEntrySegment(string Key, string Clause, string Value);
