// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>The role a key operand plays in a file control entry — the axis the ORGANIZATION formats of
/// ISO/IEC 1989:2023 §12.4.5.1 are cut on: a prime RECORD KEY and an ALTERNATE RECORD KEY belong to Format 1
/// (indexed), a RELATIVE KEY to Format 2 (relative).</summary>
internal enum FileKeyRole
{
    /// <summary>RECORD KEY IS data-name-1 (ISO §12.4.5.12) — Format 1, exactly one per entry.</summary>
    PrimeRecordKey,

    /// <summary>ALTERNATE RECORD KEY IS data-name-1 (ISO §12.4.5.6) — Format 1, zero or more per entry.</summary>
    AlternateRecordKey,

    /// <summary>RELATIVE KEY IS data-name-1 (ISO §12.4.5.13) — Format 2, at most one per entry.</summary>
    RelativeKey,
}

/// <summary>ONE key operand of a file control entry, as the screen sees it: the clause's data-name-1 as WRITTEN
/// (<paramref name="Name"/>, null when the clause is absent), what it resolved to (<paramref name="Item"/>, null
/// when it referenced nothing describable), and the cursor its rules report at.
/// <para>An ABSENT clause is an operand too — with a null name — because §12.4.5.1's required members and
/// §12.4.5.2 SR10 are violated by the absence itself. Modelling absence as a missing operand instead would need a
/// second kind of rule and a second arm to dispatch on, which is the shape this file exists to avoid.</para>
/// <para><paramref name="ClauseFace"/> is the clause's name as the standard prints it, and it is the ONLY
/// spelling of it: every message below renders it rather than repeating a literal, so a role and its
/// diagnostics cannot drift apart.</para></summary>
internal readonly record struct FileKeyOperand(
    FileKeyRole Role, string ClauseFace, string? Name, DataItem? Item, DiagnosticCursor At);

/// <summary>ONE syntax rule of the file control entry stated about a key clause.</summary>
/// <param name="RuleId">The traceability-inventory row this rule closes (<c>SR-12.4.5.12.3-1</c>), or null for a
/// requirement carried by a GENERAL FORMAT rather than by a numbered rule (§12.4.5.1 Format 1's unbracketed
/// RECORD KEY clause). The drift test keys on this.</param>
/// <param name="Clause">The clause number, exactly as <c>scripts/spec/cite.py --check</c> takes it.</param>
/// <param name="Citation">The citation as it appears in the shipped diagnostic — clause plus printed ordinal.</param>
/// <param name="RuleText">A verbatim span of the PRINTED rule, long enough to identify it. The drift test asserts
/// this span is inside <paramref name="Clause"/>'s own region of <c>specs/ISO_COBOL.md</c>, so an inherited or
/// drifted clause number turns a test red instead of shipping (CLAUDE.md rule 1).</param>
/// <param name="Organization">The organization whose §12.4.5.1 format carries the clause. A rule is screened only
/// on a file of that organization — this column IS the "keyed by organization" table.</param>
/// <param name="Role">Which key clause of that format the rule is stated about.</param>
/// <param name="Applies">The entry-level precondition beyond the organization (an access mode, a present FD).</param>
/// <param name="Violated">Whether this operand breaks the rule.</param>
/// <param name="Message">The diagnostic body — what the operand IS, then the rule, then the citation.</param>
internal sealed record FileControlKeyRule(
    string? RuleId,
    string Clause,
    string Citation,
    string RuleText,
    FileOrganization Organization,
    FileKeyRole Role,
    Func<FileModel, bool> Applies,
    Func<FileModel, FileKeyOperand, bool> Violated,
    Func<FileModel, FileKeyOperand, string> Message);

/// <summary>
/// ⛔ THE ONE SCREEN FOR THE FILE CONTROL ENTRY'S KEY SYNTAX RULES (ISO/IEC 1989:2023 §12.4.5), run from
/// <c>DataBinder.ResolveFiles</c> over EVERY declared file once the data forest is indexed.
/// <para>WHY IT EXISTS. These rules were enforced by <c>KeyedIoBinder.KeyedValidateFile</c>, which ran on the
/// FIRST KEYED VERB that named the file. An entry whose RECORD KEY and ALTERNATE RECORD KEY both sat under an
/// OCCURS clause therefore compiled with zero diagnostics when the procedure division only OPENed and CLOSEd the
/// file, and grew two errors the moment a READ was added (kb/Work PB699, measured). They are syntax rules of the
/// ENTRY: the entry violates them whether or not a statement references the file, and §4.2.2's mechanism has to
/// be able to indicate it. The screen therefore lives with the entry, and the verbs keep only the rules that are
/// genuinely stated about a STATEMENT.</para>
/// <para>WHY A TABLE. The three key clauses state overlapping rule SETS keyed by organization — §12.4.5.12.3 and
/// §12.4.5.6.3 state the identical OCCURS ban word for word, §12.4.5.13.3 states it a third time for the relative
/// key — and the shape where one member of such a set is written down is exactly the shape in which the missing
/// members hide (kb/Work PB354 found two that way, PB699 a third). One row per rule, one loop, one report site:
/// adding the next key rule is a row, and <c>FileControlKeyRuleDriftTests</c> re-derives every row's RULE TEXT
/// from <c>specs/ISO_COBOL.md</c> so a row can neither carry an inherited clause number nor go stale.</para>
/// <para>EDITIONS. Every rule here is present in all four supported editions (85 · 2002 · 2014 · 2023) — the
/// RECORD KEY, ALTERNATE RECORD KEY and RELATIVE KEY clauses and their syntax rules all predate COBOL-85 — so
/// the table needs no edition column and the negatives reject at every edition. A rule that arrived with a later
/// edition would add one, and its gate would ride <c>EditionContext</c> exactly as every other introduction does.</para>
/// <para>⚖ THE SIBLING SITE. Entry rules that are decidable from the CLAUSES ALONE — §12.4.5.5.2 SR2 (no DYNAMIC
/// or RANDOM on a sequential file, kb/Work PB692) and the §12.4.5.9 SR2 screen beside it — stay inline in
/// <c>DataBinder.BindFileControl</c>, because they need nothing resolved and can report as the entry is read.
/// This table holds the rules that need a RESOLVED DATA ITEM: a key clause names a data-name whose OCCURS
/// ancestry, PICTURE and owning record description are unknown until the data forest is indexed. That is the
/// whole criterion for which of the two a new entry rule joins — `COBOLNET_FILES_DESIGN.md` D19.</para>
/// <para>NOT HERE, deliberately: the SEMANTIC rules of the same clauses — §12.4.5.12.3 SR3/SR4/SR5,
/// §12.4.5.6.3 SR3..SR7 and the category half of both SR2s — which need machinery this screen does not have
/// (variable-length record geometry, the record-key-name-1 SOURCE phrase, which has no grammar carrier: Annex A.3
/// item 40). Each is one row when it is implemented; that is the point of the table.</para>
/// </summary>
internal static class FileControlKeyRules
{
    /// <summary>Every file-control-entry key syntax rule this compiler screens, keyed by organization and role.
    /// ⛔ ORDER IS SIGNIFICANT ONLY WITHIN A ROLE, and only so a program hears the most specific true sentence
    /// first: the "clause absent" and "operand references nothing in a record of this file" rows are mutually
    /// exclusive by construction, and every operand rule below them tests <c>Item</c>, which both of those leave
    /// null.</summary>
    private static readonly FileControlKeyRule[] Rules =
    [
        // ── Format 1 (indexed) — the prime RECORD KEY ────────────────────────────────────────────────────────
        // ⚠ THE ONLY ROW WHOSE RULE IS A GENERAL FORMAT, so it has no numbered ordinal and no sentence of its
        // own: §12.4.5.1's Format 1 diagram prints `RECORD KEY IS …` with NO bracket around it, and the figure's
        // own note says every stacked group in it is a plain brace or a plain bracket — so an unbracketed clause
        // is a required member of the indexed entry. The quoted span is that note (a sentence the normalizer can
        // match); the DIAGRAM half is asserted separately by
        // FileControlKeyRuleDriftTests.IndexedFormat_PrintsTheRecordKeyClauseUnbracketed, because a markup-laden
        // syntax diagram is not a sentence and pretending otherwise would make this row's guard vacuous.
        new(null, "12.4.5.1", "ISO §12.4.5.1 Format 1",
            "No choice indicators appear anywhere in this figure",
            FileOrganization.Indexed, FileKeyRole.PrimeRecordKey,
            f => f.HasFd,
            (_, op) => op.Name is null,
            (f, op) => $"indexed file '{f.CobolName}' has no {op.ClauseFace} clause (ISO §12.4.5.1 Format 1 — the "
                + "RECORD KEY clause is unbracketed in the indexed format, so it is required for ORGANIZATION "
                + "INDEXED)"),

        new("SR-12.4.5.12.3-2", "12.4.5.12.3", "ISO §12.4.5.12.3 SR2",
            "within a record description entry associated with the file-name specified in this file control entry",
            FileOrganization.Indexed, FileKeyRole.PrimeRecordKey,
            f => f.HasFd && f.Records.Count > 0,
            (f, op) => op.Name is not null && !RecordLayout.IsInRecordOfFile(f, op.Item),
            (f, op) => $"{op.ClauseFace} '{op.Name}' {(op.Item is null ? "references nothing described in this program" : "references an item outside this file's record descriptions")}; "
                + $"data-name-1 shall reference a data item within a record description entry associated with "
                + $"'{f.SelectName}' (ISO §12.4.5.12.3 SR2)"),

        new("SR-12.4.5.12.3-1", "12.4.5.12.3", "ISO §12.4.5.12.3 SR1",
            "Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses",
            FileOrganization.Indexed, FileKeyRole.PrimeRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not be "
                + "(ISO §12.4.5.12.3 SR1)"),

        // ── Format 1 (indexed) — each ALTERNATE RECORD KEY clause ────────────────────────────────────────────
        new("SR-12.4.5.6.3-2", "12.4.5.6.3", "ISO §12.4.5.6.3 SR2",
            "within a record description entry associated with the file-name to which the ALTERNATE RECORD KEY clause is subordinate",
            FileOrganization.Indexed, FileKeyRole.AlternateRecordKey,
            f => f.HasFd && f.Records.Count > 0,
            (f, op) => !RecordLayout.IsInRecordOfFile(f, op.Item),
            (f, op) => $"{op.ClauseFace} '{op.Name}' {(op.Item is null ? "references nothing described in this program" : "references an item outside this file's record descriptions")}; "
                + $"data-name-1 shall be defined within a record description entry associated with "
                + $"'{f.SelectName}' (ISO §12.4.5.6.3 SR2)"),

        new("SR-12.4.5.6.3-1", "12.4.5.6.3", "ISO §12.4.5.6.3 SR1",
            "Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses",
            FileOrganization.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not "
                + "be (ISO §12.4.5.6.3 SR1)"),

        // ── Format 2 (relative) — the RELATIVE KEY clause ────────────────────────────────────────────────────
        // ⚠ THE REQUIREMENT IS §12.4.5.2 SR10, NOT §12.4.5.13. The old site cited "§12.4.5.13 — required for
        // random/dynamic access"; §12.4.5.13 has no syntax rules at all (they are in §12.4.5.13.3, and none of
        // the three requires the clause), and the RELATIVE KEY clause is BRACKETED in the §12.4.5.1 Format 2
        // diagram, so the format does not require it either. SR10 is the one sentence that does (kb/Work PB699).
        new("SR-12.4.5.2-10", "12.4.5.2", "ISO §12.4.5.2 SR10",
            "The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of the ACCESS clause is specified",
            FileOrganization.Relative, FileKeyRole.RelativeKey,
            f => f.AccessMode != FileAccessMode.Sequential,
            (_, op) => op.Name is null,
            (f, op) => $"relative file '{f.CobolName}' is ACCESS {f.AccessMode.ToString().ToUpperInvariant()} but "
                + $"has no {op.ClauseFace} clause; the RELATIVE clause shall be specified if the DYNAMIC or RANDOM "
                + "phrase of the ACCESS clause is specified (ISO §12.4.5.2 SR10)"),

        new("SR-12.4.5.13.3-1", "12.4.5.13.3", "ISO §12.4.5.13.3 SR1",
            "Data-name-1 shall not be subject to any OCCURS clauses",
            FileOrganization.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not be "
                + "(ISO §12.4.5.13.3 SR1)"),

        new("SR-12.4.5.13.3-2", "12.4.5.13.3", "ISO §12.4.5.13.3 SR2",
            "shall reference an unsigned integer data item whose description does not contain the picture symbol",
            FileOrganization.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (_, op) => op.Item is { } i && i.Pic is not { Category: PicCategory.Numeric, Scale: 0, Signed: false },
            (_, op) => $"{op.ClauseFace} '{op.Name}' shall be an unsigned integer without the symbol 'P' "
                + "(ISO §12.4.5.13.3 SR2)"),

        new("SR-12.4.5.13.3-3", "12.4.5.13.3", "ISO §12.4.5.13.3 SR3",
            "shall not be defined in a record description entry subordinate to the associated file-name",
            FileOrganization.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (f, op) => op.Item is { } i && RecordLayout.IsInRecordOfFile(f, i),
            (f, op) => $"{op.ClauseFace} '{op.Name}' shall not be defined within a record description of file "
                + $"'{f.CobolName}' (ISO §12.4.5.13.3 SR3)"),
    ];

    /// <summary>The table, for <c>FileControlKeyRuleDriftTests</c> — the only reason it is not private.</summary>
    internal static IReadOnlyList<FileControlKeyRule> Catalog => Rules;

    /// <summary>Screen one file control entry against every rule of its organization. Called once per file from
    /// <c>DataBinder.ResolveFiles</c>, so a file is reported once however many statements name it — the
    /// one-report-per-file property the old per-verb memo (<c>_keyedCheckedFiles</c>) existed to provide, now a
    /// consequence of WHERE the screen runs rather than a set the screen has to carry.</summary>
    public static void Screen(FileModel file, EditionContext edition)
    {
        // A sort-merge file is described by an SD; §12.4.5.1 Format 4 carries no key clause at all. An SD whose
        // SELECT nonetheless writes ORGANIZATION INDEXED breaks §12.4.5.2 SR8/SR13, NOT a key clause's own rule,
        // so this screen has nothing true to say about it and stays silent rather than saying the wrong thing.
        if (file.IsSortMerge) return;
        // Role-outer: each role's operands are enumerated ONCE for all of its rules, so a file control entry
        // costs one iterator per role rather than one per rule.
        foreach (var role in Roles)
        {
            if (!HasApplicableRule(file, role)) continue;   // a relative entry never enumerates a RECORD KEY
            foreach (var op in Operands(file, role))
                foreach (var rule in Rules)
                {
                    if (rule.Role != role || rule.Organization != file.Organization
                        || !rule.Applies(file) || !rule.Violated(file, op)) continue;
                    using var _ = edition.At(op.At.IsSet ? op.At : file.EntryAt);
                    edition.Error(DiagnosticCatalog.FileKeyClauseRule, rule.Message(file, op));
                }
        }
    }

    /// <summary>Whether any rule of <paramref name="role"/> is in force for this entry — the organization and the
    /// entry-level precondition, both cheap, tested before an operand is materialized.</summary>
    private static bool HasApplicableRule(FileModel file, FileKeyRole role)
    {
        foreach (var rule in Rules)
            if (rule.Role == role && rule.Organization == file.Organization && rule.Applies(file)) return true;
        return false;
    }

    /// <summary>The roles, in the order a program hears about them — the §12.4.5.1 format order (prime key,
    /// alternate keys, relative key).</summary>
    private static readonly FileKeyRole[] Roles =
        [FileKeyRole.PrimeRecordKey, FileKeyRole.AlternateRecordKey, FileKeyRole.RelativeKey];

    /// <summary>The operands a role contributes to the entry. The prime and relative roles always contribute
    /// EXACTLY ONE — with a null name when the clause is absent, which is what lets an absence rule and an
    /// operand rule be the same kind of row.</summary>
    private static IEnumerable<FileKeyOperand> Operands(FileModel file, FileKeyRole role)
    {
        switch (role)
        {
            case FileKeyRole.PrimeRecordKey:
                yield return new FileKeyOperand(role, "RECORD KEY", file.RecordKeyName, file.RecordKeyItem,
                    file.RecordKeyAt);
                break;
            case FileKeyRole.AlternateRecordKey:
                // The clauses AS WRITTEN, not FileModel.AlternateKeys: a clause whose data-name-1 resolved to
                // nothing is absent from the resolved list, and that is precisely the case SR2 speaks about.
                foreach (var alt in file.AlternateKeyNames)
                    yield return new FileKeyOperand(role, "ALTERNATE RECORD KEY", alt.Name, alt.Item, alt.At);
                break;
            case FileKeyRole.RelativeKey:
                yield return new FileKeyOperand(role, "RELATIVE KEY", file.RelativeKeyName, file.RelativeKeyItem,
                    file.RelativeKeyAt);
                break;
        }
    }
}
