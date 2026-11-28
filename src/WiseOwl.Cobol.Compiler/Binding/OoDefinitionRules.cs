// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Binding;

using Core = CobolParserCore;

/// <summary>The sections of a DATA DIVISION (ISO §2023:13.2), the columns of <see cref="OoDefinitionRules"/>'s table.</summary>
internal enum OoDataSection { File, WorkingStorage, LocalStorage, Linkage, Report, Screen, Communication }

/// <summary>
/// ⛔ THE ONE HOME OF THE OO DATA-DIVISION PLACEMENT RULES (kb/Work PB1251; the DATA-division twin of
/// <see cref="OoEnvironmentRules"/>). Which sections of a data division each kind of class-body definition may
/// carry is one table, keyed by (definition kind × section), judged once per parsed definition — before this, only
/// the METHOD arm existed (an ad-hoc block in <c>OoDataBinder.BindMethodData</c>) and a LINKAGE or LOCAL-STORAGE
/// SECTION written in a FACTORY or OBJECT paragraph bound silently as persistent data:
/// <list type="bullet">
/// <item><b>§2023:13.4.3 SR1 / §2023:13.5.3 SR1 / §2023:13.8.3 SR1 / §2023:13.9.3 SR1</b> — within a class definition the file,
/// working-storage, report and screen sections are admitted "only in a factory definition or an instance
/// definition, but not in a method definition".</item>
/// <item><b>§2023:13.6.3 SR1</b> — the local-storage section is admitted in "a program definition or function
/// definition or in a method definition contained in a class definition" — so not in a factory or instance
/// definition.</item>
/// <item><b>§2023:13.7.3 SR1</b> — the linkage section is admitted in "a program definition, function definition,
/// method definition, program prototype definition, or function prototype definition" — so not in a factory or
/// instance definition.</item>
/// </list>
/// A method PROTOTYPE of an interface is a method definition (§2023:10.6.1 NOTE), so it is asked as a METHOD; its own
/// further restriction (the data division "may contain only a linkage section", §2023:10.6.2 SR4e) is
/// <see cref="PrototypeUnitRules"/>. The class definition and the interface definition have no data division of
/// their own in the grammar, so they carry no row. Every (kind × section) cell has a verdict — admitted, or the
/// clause that refuses it — and <c>OoDefinitionRulesDriftTests</c> holds the table total.
/// <para>The same walk answers the one CLAUSE rule whose subject is the definition kind rather than the section:
/// §2023:13.18.27.3 SR4 bars the GLOBAL clause in all three data-bearing kinds (<see cref="GlobalClauseSites"/>; kb/Work
/// PB1045).</para>
/// </summary>
internal static class OoDefinitionRules
{
    /// <summary>The definition kinds that carry a data division of their own in a class body.</summary>
    internal static readonly IReadOnlyList<OoDefinition> DataBearingKinds =
        [OoDefinition.Factory, OoDefinition.Instance, OoDefinition.Method];

    /// <summary>The §13 clause that refuses <paramref name="section"/> in <paramref name="kind"/>, or null when the
    /// standard admits it there. Total over <see cref="DataBearingKinds"/> × <see cref="OoDataSection"/>.</summary>
    internal static string? RefusedBy(OoDefinition kind, OoDataSection section) => (kind, section) switch
    {
        // ISO/IEC 1989:2002 Annex A: "The communication facility may be used only in a program definition"; 10.5 SR4 for a method.
        (OoDefinition.Factory or OoDefinition.Instance or OoDefinition.Method, OoDataSection.Communication) => "ISO/IEC 1989:2002 Annex A",

        // Factory and instance definitions: the "only in a factory or instance" sections are theirs; the two
        // sections that belong to a function, program or method are not.
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.File or OoDataSection.WorkingStorage
            or OoDataSection.Report or OoDataSection.Screen) => null,
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.LocalStorage) => "§2023:13.6.3 SR1",
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.Linkage) => "§2023:13.7.3 SR1",

        // A method definition: LOCAL-STORAGE and LINKAGE only.
        (OoDefinition.Method, OoDataSection.LocalStorage or OoDataSection.Linkage) => null,
        (OoDefinition.Method, OoDataSection.File) => "§2023:13.4.3 SR1",
        (OoDefinition.Method, OoDataSection.WorkingStorage) => "§2023:13.5.3 SR1",
        (OoDefinition.Method, OoDataSection.Report) => "§2023:13.8.3 SR1",
        (OoDefinition.Method, OoDataSection.Screen) => "§2023:13.9.3 SR1",

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"{kind} carries no data division of its own; add it to {nameof(DataBearingKinds)} with a verdict for every section"),
    };

    private static string SectionName(OoDataSection s) => s switch
    {
        OoDataSection.File => "FILE SECTION",
        OoDataSection.WorkingStorage => "WORKING-STORAGE SECTION",
        OoDataSection.LocalStorage => "LOCAL-STORAGE SECTION",
        OoDataSection.Linkage => "LINKAGE SECTION",
        OoDataSection.Report => "REPORT SECTION",
        _ => "SCREEN SECTION",
    };

    /// <summary>Where the standard DOES admit the section the kind may not carry, for the message.</summary>
    private static string AdmittedIn(OoDataSection s) => s switch
    {
        OoDataSection.LocalStorage => "it may appear only in a program, function or method definition",
        OoDataSection.Linkage => "it may appear only in a program, function, method or prototype definition",
        _ => "it may appear only in a factory or instance definition",
    };

    private static string KindName(OoDefinition kind) => kind switch
    {
        OoDefinition.Method => "a method definition",
        OoDefinition.Factory => "a factory definition",
        _ => "an instance definition",
    };

    /// <summary>Screen <paramref name="data"/> (null = none written) of a <paramref name="kind"/> definition: its
    /// sections against the placement table, then every GLOBAL clause written anywhere in it (§2023:13.18.27.3 SR4).
    /// <paramref name="what"/> names it in the message (<c>method 'M'</c>, <c>class 'C' OBJECT paragraph</c>).</summary>
    internal static void Screen(OoDefinition kind, string what, Core.DataDivisionContext? data, EditionContext edition)
    {
        if (data is null) return;
        foreach (var (section, context) in SectionsOf(data))
        {
            if (RefusedBy(kind, section) is not { } clause) continue;
            using var _ = edition.At(context);
            edition.Error(DiagnosticCatalog.OoDataDivisionPlacement,
                $"{what}: {KindName(kind)} shall not contain a {SectionName(section)} — {AdmittedIn(section)} "
                + $"({EditionInfo.Designated(clause)})");
        }
        ScreenGlobalClauses(kind, what, data, edition);
    }

    /// <summary>One host of the GLOBAL clause: the grammar rule whose context carries the ONE <c>globalClause</c> as a
    /// child, the §2023:13.18.27.3 SR1 item that admits the clause there, and the entry kind for the message.</summary>
    internal sealed record GlobalClauseSite(Type Host, string Sr1Item, string Entry);

    /// <summary>⛔ THE ENTRIES THE GLOBAL CLAUSE CAN BE WRITTEN IN, by the grammar rule that HOSTS the one
    /// <c>globalClause</c> — §2023:13.18.27.3 SR1's closed list: "The GLOBAL clause may be specified only in the following
    /// entries: a) A constant entry. b) A data description entry whose level-number is 1 … c) A screen-description entry
    /// whose level-number is 1. d) A file description entry. e) A report description entry." The grammar spells the clause
    /// ONCE (kb/Work PB2724) and references it from exactly these entries' rules, so the list a)–e) is the grammar's; b)'s
    /// level and section residence is the clause-placement screen's (<c>ClausePlacementRules</c>, §2023:13.16.3 SR6), and
    /// c)'s entry is refused whole by the screen facility (COBOL1560). <c>OoDefinitionRulesDriftTests</c> holds this list
    /// equal to the rules that reference <c>globalClause</c>, so a new host joins SR4 here.</summary>
    internal static readonly IReadOnlyList<GlobalClauseSite> GlobalClauseSites =
    [
        new(typeof(Core.ConstantEntryBodyContext), "a)", "constant entry"),
        new(typeof(Core.DataDescriptionClauseContext), "b)", "data description entry"),
        new(typeof(Core.ScreenClauseContext), "c)", "screen description entry"),
        new(typeof(Core.FileDescriptionClauseContext), "d)", "file description entry"),
        new(typeof(Core.ReportDescriptionClauseContext), "e)", "report description entry"),
    ];

    /// <summary><see cref="GlobalClauseSites"/> by host rule.</summary>
    private static readonly FrozenDictionary<Type, GlobalClauseSite> GlobalClauseSiteByHost =
        GlobalClauseSites.ToFrozenDictionary(site => site.Host);

    /// <summary>§2023:13.18.27.3 SR4 — "The GLOBAL clause shall not be specified in a factory definition, an instance
    /// definition, or a method definition." Every data-bearing kind of <see cref="DataBearingKinds"/> is one of the
    /// three, and every entry SR1 lets carry the clause lives in a data division, so this one walk over the written
    /// data division answers SR4 for all of them — constant entries included (kb/Work PB1045: the former class arm
    /// asked the bound files, reports and level-1 roots only, and the method arm only its data description entries).
    /// A method PROTOTYPE is a method definition (§2023:10.6.1 NOTE), so an interface's prototypes are asked too.
    /// Syntactic by design: the rule is about where the clause is WRITTEN, which the parse tree answers before any
    /// binding decides what the clause would have meant.</summary>
    private static void ScreenGlobalClauses(OoDefinition kind, string what, Core.DataDivisionContext data,
                                            EditionContext edition)
    {
        var pending = new Stack<ParserRuleContext>();
        pending.Push(data);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is Core.GlobalClauseContext && GlobalClauseSiteByHost.GetValueOrDefault(node.Parent.GetType()) is { } site)
            {
                using var _ = edition.At(node);
                edition.Error(DiagnosticCatalog.GlobalInOoDefinition,
                    $"{what}: the {site.Entry} '{EntryName(node) ?? "FILLER"}' specifies the GLOBAL clause in "
                    + $"{KindName(kind)} — \"The GLOBAL clause shall not be specified in a factory "
                    + "definition, an instance definition, or a method definition.\" (ISO §2023:13.18.27.3 SR4)");
            }
            if (node.children is { } children)
                for (int i = children.Count - 1; i >= 0; i--)   // reversed, so the walk reports in source order
                    if (children[i] is ParserRuleContext child) pending.Push(child);
        }
    }

    /// <summary>The name the entry carrying <paramref name="clause"/> declares: its data-name, constant-name,
    /// file-name, report-name or screen-name (null for an entry written without one).</summary>
    private static string? EntryName(ParserRuleContext clause)
    {
        for (var p = clause.Parent; p is not null; p = p.Parent)
            switch (p)
            {
                case Core.DataDescriptionEntryContext d: return d.dataName()?.GetText();
                case Core.ConstantEntryContext k: return k.dataName()?.GetText();
                case Core.FileDescriptionEntryContext f: return f.fileName()?.GetText();
                case Core.ReportDescriptionEntryContext r: return r.reportName()?.GetText();
                case Core.ScreenDescriptionEntryContext sc: return sc.dataName()?.GetText();
            }
        return null;
    }

    private static IEnumerable<(OoDataSection Section, ParserRuleContext Context)> SectionsOf(Core.DataDivisionContext data)
    {
        if (data.fileSection() is { } f) yield return (OoDataSection.File, f);
        if (data.workingStorageSection() is { } w) yield return (OoDataSection.WorkingStorage, w);
        if (data.localStorageSection() is { } l) yield return (OoDataSection.LocalStorage, l);
        if (data.linkageSection() is { } k) yield return (OoDataSection.Linkage, k);
        if (data.communicationSection() is { } c) yield return (OoDataSection.Communication, c);
        if (data.reportSection() is { } r) yield return (OoDataSection.Report, r);
        if (data.screenSection() is { } s) yield return (OoDataSection.Screen, s);
    }
}
