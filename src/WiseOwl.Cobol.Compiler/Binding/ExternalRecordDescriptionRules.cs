// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// ⛔ THE RUN UNIT'S AGREEMENT RULE FOR THE DESCRIPTIONS OF ONE EXTERNAL RECORD, ASKED OF THE COMPILATION GROUP (kb/Work
/// PB5996; COBOL7735). Every edition requires that the source elements of a run unit which describe one external data record
/// describe it alike: §1985:X.4.5.4 GR2 "each record-name of the associated record description entries must be the same and
/// the records must define the same number of standard data format characters"; §2002:13.16.20.3 GR4 and §2014:13.18.22.3
/// GR4 add "the VALUE clause specification, if any, for each record name of the associated record description entries shall
/// be identical", and §2023:13.18.22.4 GR6 states the same. The descriptions compared are
/// <see cref="ExternalRecordDescription"/>, the facts the run time compares too, so the two checks read one definition.
/// <para>⛔ WHERE THE RULE IS ASKED, BY EDITION. No edition states a consequence for a run unit that breaks it below 2023
/// (§1985:I.1.6: "The results of violating the formats or rules of Standard COBOL are undefined unless otherwise specified"),
/// so the compiler refuses a violation wherever it sees both descriptions, in one compilation group, at COBOL-85, COBOL-2002
/// and COBOL-2014, as the agreement of an external file connector's entries is refused (<see cref="ExternalFileConnectorRules"/>,
/// COBOL6847). COBOL-2023 SPECIFIES the result: "For external data items, the rules specified in 13.18.22, EXTERNAL clause,
/// General rule 6 apply" (§2023:14.8.4.3), checked at the activation of the describing element, where a violation is
/// EC-EXTERNAL-FORMAT-CONFLICT when the condition is enabled (§2023:14.8.4.1, §2023:14.9.4.4 GR3e), so at 2023 the rule stays the
/// run time's (<c>ExternalTable.Describe</c>), where a program in one compilation group meets its defined outcome.
/// Separately compiled programs are always the run time's: at COBOL-85 none compares them (docs/conformance/3/
/// D-85-EXTERNAL-FD-AGREEMENT.md).</para>
/// <para>⛔ THE RECORDS OF AN EXTERNAL FILE DESCRIPTION. From COBOL-2002 they are external records "subject to the following
/// rules" (§2002:13.16.20.3 GR2b, §2014:13.18.22.3 GR2b), and the name externalized for them is the FILE's (GR3), so their one
/// shared record area (the <c>FD::</c> backings, <see cref="LinkageBinder.ExternalBackings"/>) is one external data record
/// compared like any other, as the run time compares it at 2023. At COBOL-85 the file description entry's own rule governs
/// the size of an external file's records, "all record description entries associated with this file connector must be the
/// same length" (§1985:VII.3.8.4 GR2, asked by <see cref="ExternalFileConnectorRules"/>, COBOL6847), so they are not
/// compared again here.</para>
/// <para>⛔ EVERY REPORT IS POSITIONED (kb/Work PB2034): this is a group-level pass, so each report is made at the record
/// description entry that disagrees.</para>
/// </summary>
internal static class ExternalRecordDescriptionRules
{
    public static void Check(IReadOnlyList<ExternalRecordNameRules.Element> elements, EditionContext edition)
    {
        if (edition.DialectLevel >= 2023) return;   // the run time's EC-EXTERNAL-FORMAT-CONFLICT (§2023:14.8.4.3), above
        // Each external record of every element, by its externalized name, in the run-unit store's own comparison.
        Dictionary<string, (string Describer, CallExternalBacking Backing)>? first = null;
        foreach (var e in elements)
            foreach (var backing in e.Data.Linkage.ExternalBackings)
            {
                bool fileArea = backing.ExternalName.StartsWith(Model.FileModel.ExternalAreaPrefix, StringComparison.Ordinal);
                if (fileArea && edition.DialectLevel < 2002) continue;   // §1985:VII.3.8.4 GR2's, above
                string describer = e.Unit is { } u ? $"program '{u.Name}'" : "a class";
                first ??= new Dictionary<string, (string, CallExternalBacking)>(ExternalizedNames.Comparer);
                if (!first.TryGetValue(backing.ExternalName, out var earlier))
                {
                    first[backing.ExternalName] = (describer, backing);
                    continue;
                }
                string? differs = Difference(earlier.Backing, backing, edition);
                if (differs is null) continue;
                using var _ = edition.At(backing.Record.DeclaredAt);
                edition.Error(DiagnosticCatalog.ExternalRecordDescriptionDisagreement,
                    (fileArea ? $"the records of external file '{backing.ExternalName[Model.FileModel.ExternalAreaPrefix.Length..]}'" : $"external record '{backing.Record.CobolName}'")
                    + $": this description and the one in {earlier.Describer} describe the same "
                    + $"external data record and differ in {differs} — "
                    + edition.Edition.Cite(
                        "the records must define the same number of standard data format characters",
                        "the VALUE clause specification, if any, for each record name shall be identical and the records shall define "
                        + "the same number of bytes")
                    + $" ({edition.Edition.CiteRule("§1985:X.4.5.4 GR2", "§2023:13.18.22.4 GR6", y2002: "§2002:13.16.20.3 GR4", y2014: "§2014:13.18.22.3 GR4")})");
            }
    }

    /// <summary>What the edition's rule compares and <paramref name="b"/> does not share with <paramref name="a"/>, or null when
    /// they agree. COBOL-85 compares the size alone: its EXTERNAL record writes no VALUE clause (§1985:X.4.5.3 SR3). The size of a
    /// variable-length record is not its description's (<see cref="ExternalRecordDescription.DefinesItsSize"/>), so it is not
    /// compared: a description of its dynamic-capacity table that disagrees is EC-DATA-INCOMPATIBLE at run time (§2014:14.6.12.2
    /// rule 6, the suite's pb1042_dyn_table_agreement).</summary>
    private static string? Difference(CallExternalBacking first, CallExternalBacking backing, EditionContext edition)
    {
        ExternalDescriptor a = ExternalRecordDescription.Of(first), b = ExternalRecordDescription.Of(backing);
        if (ExternalRecordDescription.DefinesItsSize(first) && ExternalRecordDescription.DefinesItsSize(backing) && a.ByteCount != b.ByteCount)
            return $"their size ({b.ByteCount} {edition.Edition.Cite("character positions", "bytes")} against {a.ByteCount})";   // each text's own unit
        if (edition.DialectLevel >= 2002 && !string.Equals(a.ValueImage, b.ValueImage, StringComparison.Ordinal))
            return $"the VALUE clause of the record name ({Shown(b.ValueImage)} against {Shown(a.ValueImage)})";
        return null;
        static string Shown(string? v) => v is null ? "none" : "VALUE " + v;
    }
}
