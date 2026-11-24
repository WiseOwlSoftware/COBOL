// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace WiseOwl.Cobol.Analyzers;

/// <summary>⛔ A CITATION WRITTEN FOR AN EDITION IS THE ONE <c>EditionInfo.Cite</c> CHOOSES AT THAT EDITION (WOCA0004; kb/Work PB5922
/// review round 1, R1-1). <c>Cite(y1985, later, y2002, y2014)</c> gives COBOL-2002 <c>y2002 ?? later</c> and COBOL-2014
/// <c>y2014 ?? y2002 ?? later</c>, so a COBOL-2002 rule passed only as <c>y1985</c> (the first edition that states it, for a construct
/// COBOL-85 lacks) is printed at <c>--std 85</c> and never at <c>--std 2002</c>, where the 2023 rule prints instead: eleven calls of one
/// change did so, and the 2002 net could not see it (a code held open by the sweep, or a message that also names a §2002 rule). The
/// rule: when any argument of a <c>Cite</c> or <c>CiteRule</c> call cites a rule of COBOL-2002 or COBOL-2014 (<c>§2002:</c>,
/// <c>§2014:</c> in a constant string of the argument), the argument the call chooses at that edition cites that edition too. Only
/// the constant text of an argument is read; a citation built at run time is the message's own business.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EditionCiteSlotAnalyzer : DiagnosticAnalyzer
{
    /// <summary>An edition's citation that the edition never prints.</summary>
    public const string Id = "WOCA0004";

    private static readonly DiagnosticDescriptor Unprinted = new(Id,
        "A citation written for an edition is not the one EditionInfo.Cite chooses at that edition",
        "This {0} call cites a §{1} rule that --std {1} never prints: at COBOL-{1} Cite chooses its {2} argument; pass the §{1} text as "
        + "y{1} (kb/Work PB5922, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
        "WiseOwl.Editions", DiagnosticSeverity.Error, isEnabledByDefault: true, customTags: [WellKnownDiagnosticTags.NotConfigurable]);

    /// <summary>A citation of a COBOL-2002 or COBOL-2014 rule, in its edition form (kb/Work PB4887).</summary>
    private static readonly Regex EditionCitation = new(@"§(2002|2014):", RegexOptions.CultureInvariant);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Unprinted];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var call = (IInvocationOperation)context.Operation;
        var method = call.TargetMethod;
        if (method.Name is not ("Cite" or "CiteRule")
            || method.ContainingType?.ToDisplayString() != EditionCiteAnalyzer.EditionInfoName)
            return;
        // The arguments the call WROTE, by parameter name (a defaulted one is absent, as Cite reads it: null).
        var written = new Dictionary<string, IArgumentOperation>(StringComparer.Ordinal);
        foreach (var argument in call.Arguments)
            if (argument.Parameter is { } parameter && argument.ArgumentKind != ArgumentKind.DefaultValue)
                written[parameter.Name] = argument;
        if (!written.ContainsKey("later")) return;
        var cited = new HashSet<string>(written.Values.SelectMany(CitedEditions), StringComparer.Ordinal);
        foreach (string edition in cited.OrderBy(e => e, StringComparer.Ordinal))
        {
            string chosen = Chosen(edition, written);
            if (!CitedEditions(written[chosen]).Contains(edition))
                context.ReportDiagnostic(Diagnostic.Create(Unprinted, call.Syntax.GetLocation(), method.Name, edition, chosen));
        }
    }

    /// <summary>The parameter <c>EditionInfo.Cite</c> reads at <paramref name="edition"/> (2002 or 2014), of those written.</summary>
    private static string Chosen(string edition, Dictionary<string, IArgumentOperation> written) => edition switch
    {
        "2002" => written.ContainsKey("y2002") ? "y2002" : "later",
        _ => written.ContainsKey("y2014") ? "y2014" : written.ContainsKey("y2002") ? "y2002" : "later",
    };

    /// <summary>The editions (2002, 2014) whose rules the constant strings of an argument cite.</summary>
    private static IEnumerable<string> CitedEditions(IArgumentOperation argument) =>
        argument.Value.DescendantsAndSelf()
            .Select(o => o.ConstantValue)
            .Where(c => c.HasValue && c.Value is string)
            .SelectMany(c => EditionCitation.Matches((string)c.Value!).Cast<Match>().Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal);
}
