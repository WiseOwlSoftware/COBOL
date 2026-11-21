// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN REPORT-GROUP REFERENCE (kb/Work PB365). THREE sites name a
/// report group by name — <c>GENERATE data-name-1</c> (ISO §2023:14.9.16.3 SR1), <c>USE BEFORE REPORTING
/// identifier-1</c> (§2023:14.9.49.3 SR9) and, since kb/Work PB482, the SUM clause's <c>UPON data-name-2</c>
/// (§2023:13.18.54.3 SR7: "Data-name-2 shall be the name of a detail. It may be qualified only by a report-name" —
/// the DATA-division site, resolved in <c>ReportSectionBinder.ResolveReports</c> once every group is described) — and
/// before this funnel the two statement sites were written as
/// <c>foreach (report) if (report.Groups.FirstOrDefault(name-matches) is {} g) return g;</c>: a loop shape that
/// encodes <i>find one</i> where §2023:8.4.2.2 requires <i>find exactly one</i>. With the same 01-level group name in
/// two report description entries, the reference bound to whichever RD was written first, silently — so a
/// GENERATE of the second report's group ran the OTHER report's declarative and nothing downstream noticed
/// (feedback_two_arm_dispatch: two arms, and neither one was right).
///
/// <para><b>The rule.</b> §2023:8.4.2.2.1: "Identical user-defined names may be specified in a source unit; however,
/// uniqueness shall be established through qualification for each user-defined name explicitly referenced,
/// except as specified in rules 2 through 6" — none of exceptions 2–6 covers a report-group name. §2023:8.4.2.2.3
/// SR1 restates it as the syntax rule: "For each non unique user-defined name that is explicitly referenced,
/// uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference."
/// The available qualifier is the report-name (§2023:8.4.2.2.2 Format 1's file-report-qualifier; §2023:14.9.16.3 SR1 says
/// so in words: "It may be qualified by a report-name"), which is why the qualified spelling exists at all.
/// §2023:8.4.2.2.3 SR3 makes IN and OF equivalent — the grammar's <c>reportGroupReference</c> accepts both.</para>
///
/// <para><b>Why the ambiguity is diagnosed HERE and not at the call sites.</b> One rule, one place. The callers
/// differ only in what a MISS means (GENERATE falls back to the report-name form for summary reporting, SR2;
/// USE and the SUM clause's UPON operand have no fallback), so <see cref="Resolve"/> owns Found/Ambiguous and
/// hands <c>None</c> back for the caller's own SR. On Ambiguous NO candidate is returned (<c>report</c> and <c>group</c> are null):
/// the diagnostic has already fired, and a caller can only stop, never go on with a first candidate by omission (kb/Work PB5542).</para>
/// </summary>
internal static class ReportGroupResolution
{
    /// <summary>The outcome of a report-group lookup. <c>Ambiguous</c> is ALREADY DIAGNOSED (COBOL1920) and
    /// carries no group — the caller stops; <c>None</c> carries nothing and leaves the "this is not a
    /// report group" diagnostic to the caller, whose syntax rule differs per statement.</summary>
    public enum Match { Found, Ambiguous, None }

    /// <summary>Split a <c>reportGroupReference</c> parse context into (head, qualifier). The qualifier is the
    /// <c>IN/OF report-name-1</c> tail (§2023:8.4.2.2.2 Format 1's file-report-qualifier), null when unqualified.
    /// ⚠ Read from the context's CHILDREN, never <c>GetText()</c> of the whole context — that concatenates
    /// <c>DET-A OF R-B</c> into an unmatchable key (the same trap <c>ResolveProcedure</c> documents).</summary>
    public static (string Head, string? Qualifier) Parts(Core.ReportGroupReferenceContext r) =>
        (r.cobolWord().GetText(), r.reportName()?.GetText());

    /// <summary>⛔ THE ONE DECOMPOSITION OF A WRITTEN REPORT-GROUP REFERENCE into its head and its IN/OF report-name qualifier: a
    /// <c>reportGroupReference</c> (<see cref="Parts"/>), or the <c>dataReference</c> of a SUM clause's UPON operand, which
    /// <c>ReportSectionBinder.UponDetailRef</c> has screened to one qualifier at most (read through the one decomposition of a written
    /// reference, <see cref="ReferenceResolver.ReadWritten"/>). The symbol table's lookup (<c>SymbolTable.ReportGroupsNamed</c>) and
    /// this funnel's diagnostic and data-item count read the SAME words from here, so the candidates a qualifier restricts and the
    /// reference the funnel treats as qualified cannot drift apart. Null for any other context, or a <c>dataReference</c> with no word.</summary>
    internal static (string Head, string? Qualifier)? Words(ParserRuleContext written)
    {
        switch (written)
        {
            case Core.ReportGroupReferenceContext r:
                return Parts(r);
            case Core.DataReferenceContext d when d.cobolWord() is { } word:
                var quals = ReferenceResolver.ReadWritten(d).Qualifiers;
                return (word.GetText(), quals.Count == 1 ? quals[0] : null);
            default:
                return null;
        }
    }

    /// <summary>The reference's head word with no qualifier, as the data-name lookup takes it: the count of the data items an
    /// UNQUALIFIED report-group name shares its spelling with (§2023:8.4.2.2.3 SR1). <paramref name="written"/> has words (the table
    /// answered it).</summary>
    private static WrittenQualifiedName HeadOnly(ParserRuleContext written) => new(written switch
    {
        Core.ReportGroupReferenceContext r => r.cobolWord().Start,
        Core.DataReferenceContext d => d.cobolWord().Start,
        _ => throw new InvalidOperationException($"a report-group reference is a reportGroupReference or a dataReference, not {written.GetType().Name}"),
    }, []);

    /// <summary>Resolve a possibly-qualified report-group reference: the symbol table answers EVERY candidate
    /// (<see cref="SymbolTable.Identify(ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>,
    /// <c>NameClass.ReportGroup</c>: a qualified reference restricts the candidate reports to the named one), and this funnel reports
    /// COBOL1920 when more than one survives — §2023:8.4.2.2.1 / §2023:8.4.2.2.3 SR1.</summary>
    /// <param name="written">The reference as written: a <c>reportGroupReference</c> or an UPON operand's <c>dataReference</c>.</param>
    /// <param name="where">The reference site for the diagnostic, e.g. <c>"GENERATE 'DET-A'"</c>.</param>
    /// <param name="reach"><see cref="NameReach.Visible"/> for a procedure division reference, whose reports include the GLOBAL
    /// ones inherited from containers: candidates from more than one source element are narrowed to the NEAREST one before
    /// ambiguity is asked (§2023:8.4.6.2.1 rule 3 — <see cref="ReportScope.NearestInScope"/>). <see cref="NameReach.Own"/> for the
    /// data-division site, whose reports are all the unit's own. The reach selects the reports only: at both, a data item of the
    /// head's spelling counts against an unqualified reference (kb/Work PB5775).</param>
    /// <param name="preferred">The report whose name the hint's example qualifier should use when it is one of the
    /// candidates, else the first candidate's. The SUM clause's UPON operand passes the control footing's own report: below
    /// COBOL-2002 data-name-1 must be a detail "described in the same report as the control footing report group"
    /// (§1985:XIII.3.19.3 SR2), so the qualifier that resolves the ambiguity is that report's (kb/Work PB5524).</param>
    public static Match Resolve(DataBinder binder, ParserRuleContext written, string where, out ReportModel? report,
        out ReportGroupModel? group, NameReach reach, ReportModel? preferred = null)
    {
        report = null;
        group = null;
        var edition = binder.Edition;
        IReadOnlyList<(ReportModel Report, ReportGroupModel Group)> found =
            binder.Symbols.Identify(written, NameClass.ReportGroup, report: true, reach)?.Symbol
                is ReportCandidates<(ReportModel Report, ReportGroupModel Group)> { Hits: var hits } ? hits : [];
        if (found.Count == 0) return Match.None;
        var (head, qualifier) = Words(written)!.Value;   // the table answered, so the reference has words
        // ⛔ A DATA ITEM OF THE SAME SPELLING IS ANOTHER DECLARATION OF THE NAME (kb/Work PB5775). A report group's name and a
        // Working-Storage item's live in one namespace of user-defined names: "For each nonunique user-defined name that is explicitly
        // referenced, uniqueness must be established through a sequence of qualifiers which precludes any ambiguity of reference"
        // (§1985:IV.4.3.8.1 SRL2.1; §2023:8.4.2.2.3 SR1), and a data item has no qualifier that tells it from the report group, so an
        // UNQUALIFIED reference to the shared name is the ambiguity and the report-name qualifier (DA OF R-1) is the way out. Counted
        // in the source element the reports are (the data lookup leaves no item a nearer report entry hides, kb/Work PB4196). At
        // EVERY site: the reach chooses which reports are searched, never whether a data item of the spelling is a declaration of the
        // name, so the SUM clause's UPON operand (NameReach.Own) counts them as GENERATE and USE BEFORE REPORTING do.
        int dataItems = qualifier is null
            ? binder.Symbols.IdentifyDataName(HeadOnly(written), report: false).ItemCount : 0;
        if (found.Count == 1 && dataItems == 0)
        {
            (report, group) = found[0];
            return Match.Found;
        }
        var first = found[0].Report;
        var extra = found.Skip(1);

        // §2023:8.4.2.2.3 SR1 — the reference does not preclude ambiguity. Name every report that carries the group
        // so the fix (which qualifier to write) is in the message.
        string owners = string.Join(", ", new[] { first.Name }
            .Concat(extra.Select(e => e.Report.Name)).Select(n => $"'{n}'"));
        string example = preferred is not null && found.Any(f => ReferenceEquals(f.Report, preferred)) ? preferred.Name : first.Name;
        string rule = edition.Edition.Cite("§1985:IV.4.3.8.1 SRL2.1", "ISO §2023:8.4.2.2.1 / §2023:8.4.2.2.3 SR1");
        string homonyms = (found.Count > 1 ? $"described in more than one report description entry ({owners})" : $"described in report {owners}")
            + (dataItems == 0 ? "" : $"{(found.Count > 1 ? " and" : ",")} also the name of {(dataItems == 1 ? "a data item" : $"{dataItems} data items")}");
        edition.Error(DiagnosticCatalog.ReportGroupReferenceAmbiguous, $"{where}: report group '{head}' is "
            + $"{homonyms} — qualify the reference "
            + $"(e.g. '{head} OF {example}'), since uniqueness shall be established through qualification "
            + $"({rule})");
        return Match.Ambiguous;
    }
}
