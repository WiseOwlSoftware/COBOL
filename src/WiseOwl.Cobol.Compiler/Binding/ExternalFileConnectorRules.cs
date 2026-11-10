// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding;

using WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE RUN UNIT'S RULES FOR AN EXTERNAL FILE CONNECTOR'S FILE CONTROL ENTRIES, ASKED OF THE COMPILATION GROUP (kb/Work
/// PB5069). Every edition says that the entries of the run unit that describe one external file connector agree, and that
/// the data items some of their clauses name are external data items:
/// <list type="bullet">
/// <item><b>Agreement</b> (COBOL6847, every edition): §1985:VII.2.3.4 GR1, §1985:VIII.2.3.4 GR1 and §1985:IX.2.3.4 GR1 ("all
/// file control entries in the run unit which reference this file connector must have" the same OPTIONAL phrase, ASSIGN,
/// RECORD DELIMITER and RESERVE, and for a relative or indexed file the same organization and access mode),
/// §1985:VII.2.4.3 GR3 (a sequential file's access mode), §1985:VII.2.7.4 GR6 (PADDING CHARACTER), §1985:IX.2.5.4 GR5 (the
/// alternate keys); §2002:12.3.4.3 GR1 a) to k); §2023:12.4.5.3 GR1 a) to m). What is compared is
/// <see cref="FileEntryIdentity.Segments"/>, the identity the run time compares between separately compiled runtime
/// elements (EC-EXTERNAL-FILE-MISMATCH), so the two checks read one definition of "the same specification".</item>
/// <item><b>The RELATIVE KEY item</b> (COBOL1624, COBOL1575, every edition): §1985:VIII.2.4.4 GR8 "data-name-1 must
/// reference an external data item and the RELATIVE KEY phrase in each associated file control entry must reference
/// that same external data item in each case"; §2002:12.3.4.3 GR1 i); §2023:12.4.5.3 GR1 h).</item>
/// <item><b>The PADDING CHARACTER item</b> (COBOL1624, COBOL-85 and COBOL-2002, where the clause exists): §1985:VII.2.7.4
/// GR6 "If data-name-1 is specified, it must reference an external data item"; §2002:12.3.4.3 GR1 h).</item>
/// <item><b>The FILE STATUS and LINAGE items</b> (COBOL1624, COBOL1573, COBOL-2023): §2023:14.8.4.2 and §2023:12.4.5.3 GR1
/// i), the requirement Annex E.2 items 9 and 12 name as new in 2023. Its severity follows the removal policy (an error
/// under strict, a warning under --permissive), the contract the continuity witnesses need: an 85 NIST program that
/// violates the new rule (IC227A, two differently-named non-external FILE STATUS items) still compiles under permissive.</item>
/// </list>
/// The compile-time reach is the group's units; cross-compilation sameness is the run time's
/// (<c>ExternalTable</c>, EC-EXTERNAL-DATA-MISMATCH and EC-EXTERNAL-FILE-MISMATCH).
/// <para>⛔ EVERY REPORT HERE IS POSITIONED (kb/Work PB2034): this is a group-level pass, so no walker has set the
/// diagnostic cursor, and each report is made at the clause or entry it is about.</para>
/// </summary>
internal static class ExternalFileConnectorRules
{
    public static void Check(IReadOnlyList<BoundUnit> units, EditionContext edition)
    {
        var byExternalName = units.SelectMany(u => u.Data.Files.Declared)
            .Where(f => f is { IsExternal: true, ExternalName: not null })
            .GroupBy(f => f.ExternalName!, CobolNames.Comparer);
        bool is2023 = edition.DialectLevel >= 2023;
        var newIn2023 = EditionSeverityPolicy.For(ConstructAvailability.Removed, edition.Edition);
        foreach (var group in byExternalName)
        {
            string externalName = group.Key.ToUpperInvariant();   // the name as the diagnostics show it; equality was the fold's above
            var conns = group.ToList();
            foreach (var f in conns)
            {
                if (is2023 && f.FileStatusName is not null && f.FileStatusItem?.ExternalItemIdentity() is null)
                    NotExternal(edition, externalName, f.ClauseOrEntryAt(f.FileStatusAt), "FILE STATUS", f.FileStatusName,
                        newIn2023, "ISO §2023:14.8.4.2; Annex E.2 item 9");
                if (f.Organization == FileOrganization.Relative && f.RelativeKeyName is not null
                    && f.RelativeKeyItem?.ExternalItemIdentity() is null)
                    NotExternal(edition, externalName, f.ClauseOrEntryAt(f.RelativeKeyAt), "RELATIVE KEY", f.RelativeKeyName,
                        EditionSeverity.Error, "ISO " + Cite(edition, "§1985:VIII.2.4.4 GR8", "§2002:12.3.4.3 GR1 i)", "§2023:12.4.5.3 GR1 h)"));
                if (f.Padding is { DataName: { } pad, Item: not null } p && p.Item.ExternalItemIdentity() is null)
                    NotExternal(edition, externalName, p.At, "PADDING CHARACTER", pad,
                        EditionSeverity.Error, "ISO " + Cite(edition, "§1985:VII.2.7.4 GR6", "§2002:12.3.4.3 GR1 h)", "§2002:12.3.4.3 GR1 h)"));
                if (is2023 && f.Linage is { } lin)
                    foreach (var op in lin.Operands)
                        if (op.DataName is not null && op.Item?.ExternalItemIdentity() is null)   // literal operands are exempt
                            NotExternal(edition, externalName, f.ClauseOrEntryAt(op.At), "LINAGE", op.DataName,
                                newIn2023, "ISO §2023:14.8.4.2; Annex E.2 item 9");
            }
            // The agreement rules relate two describers; one has nothing to agree with (a lone program's external file
            // is checked against the other runtime elements at run time).
            if (conns.Count < 2) continue;

            // FILE STATUS (VCR 18, new in 2023): if ANY corresponding SELECT specifies FILE STATUS, ALL shall, each naming
            // the same corresponding external data item (§2023:12.4.5.3 GR1 i)).
            if (is2023 && conns.Any(f => f.FileStatusName is not null))
                SameItem(edition, conns, f => f.FileStatusItem, f => f.ClauseOrEntryAt(f.FileStatusAt),
                    DiagnosticCatalog.ExternalFileStatusConsistency, newIn2023,
                    $"external file '{externalName}': all corresponding SELECT statements in the run unit shall "
                    + "specify FILE STATUS naming the same corresponding external data item "
                    + "(ISO §2023:12.4.5.3 GR1(i); Annex E.2 item 12)",
                    "ISO §2023:12.4.5.3 GR1(i) / §2023:14.8.4.2 / Annex E.2 item 12", externalName);

            // RELATIVE KEY: for an external RELATIVE file, every corresponding SELECT names the same external data item.
            if (conns.Any(f => f.Organization == FileOrganization.Relative) && conns.Any(f => f.RelativeKeyName is not null))
                SameItem(edition, conns, f => f.RelativeKeyItem, f => f.ClauseOrEntryAt(f.RelativeKeyAt),
                    DiagnosticCatalog.ExternalRelativeKeyConsistency, EditionSeverity.Error,
                    $"external relative file '{externalName}': all corresponding SELECT statements in the run unit "
                    + "shall specify RELATIVE KEY naming the same external data item (ISO "
                    + Cite(edition, "§1985:VIII.2.4.4 GR8", "§2002:12.3.4.3 GR1 i)", "§2023:12.4.5.3 GR1 h)") + ")",
                    Cite(edition, "§1985:VIII.2.4.4 GR8", "§2002:12.3.4.3 GR1 i)", "§2023:12.4.5.3 GR1 h)"), externalName);

            // Every other specification: the entry identity's segments, compared with the first describer's.
            var first = FileEntryIdentity.Segments(conns[0]);
            foreach (var f in conns.Skip(1))
            {
                var mine = FileEntryIdentity.Segments(f);
                for (int i = 0; i < mine.Count; i++)
                {
                    if (string.Equals(mine[i].Value, first[i].Value, StringComparison.Ordinal)) continue;
                    using (edition.At(f.EntryAt))
                        edition.Error(DiagnosticCatalog.ExternalFileEntryDisagreement,
                            $"external file '{externalName}': this file control entry and the first one in the run unit "
                            + $"that describes the file connector differ in {mine[i].Clause} — every entry of an external "
                            + "file connector specifies it the same way (ISO "
                            + Cite(edition, "§1985:VII.2.3.4 GR1, §1985:VII.2.7.4 GR6, §1985:VIII.2.3.4 GR1, §1985:IX.2.3.4 GR1",
                                "§2002:12.3.4.3 GR1", "§2023:12.4.5.3 GR1") + ")");
                    break;   // one report per entry: the first clause that differs
                }
            }
        }
    }

    /// <summary>The rule of the edition being compiled, by the edition's own citation.</summary>
    private static string Cite(EditionContext edition, string y1985, string y2002, string y2023) =>
        edition.DialectLevel < 2002 ? y1985 : edition.DialectLevel < 2023 ? y2002 : y2023;

    private static void NotExternal(EditionContext edition, string ext, Editions.DiagnosticCursor at, string clause, string name,
        EditionSeverity severity, string cite)
    {
        using (edition.At(at))
            edition.Report(new EditionDiagnostic(DiagnosticCatalog.ExternalFileItemNotExternal.Code, severity,
                DiagnosticCatalog.ExternalFileItemNotExternal.Id,
                $"external file '{ext}': the {clause} data item '{name}' shall be an external data item ({cite})",
                $"external file '{ext}'", cite));
    }

    /// <summary>"If any specifies it, all shall, naming the same item": report the first describer that names no external
    /// item, else the first whose item differs from the first describer's.</summary>
    private static void SameItem(EditionContext edition, List<FileModel> conns, Func<FileModel, DataItem?> item,
        Func<FileModel, Editions.DiagnosticCursor> at, DiagnosticDescriptor code, EditionSeverity severity, string message,
        string cite, string externalName)
    {
        var ids = conns.Select(f => item(f)?.ExternalItemIdentity()).ToList();
        if (!ids.Any(id => id is null) && ids.Distinct(StringComparer.Ordinal).Count() <= 1) return;
        int odd = ids.FindIndex(id => id is null);
        if (odd < 0) odd = ids.FindIndex(id => !string.Equals(id, ids[0], StringComparison.Ordinal));
        using (edition.At(at(conns[odd])))
            edition.Report(new EditionDiagnostic(code.Code, severity, code.Id, message, $"external file '{externalName}'", cite));
    }
}
