// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A WRITTEN object-class-name OR interface-name RESOLVES THROUGH ONE FUNNEL (<c>SymbolTable.ClassNames.Resolve</c> and its
/// non-diagnosing half <c>Lookup</c> — kb/Work PB365), AND THIS KEEPS IT TRUE. ISO §2023:8.4.6.4 scopes both to "the name of the
/// containing object class definition or declared in the REPOSITORY paragraph of that or a containing source element"; the
/// compilation group's tier holds the GROUP's names, which is a strictly wider set, so a lookup that skipped the scope would answer
/// a different question than the one the standard asks — silently, because widening a set cannot fail a lookup.
///
/// <para>Since listing step 6b (the OO names, docs/rearchitecture/DESIGN-listing.md section 6.7.1) the tier's name tables are
/// private and keyed by <c>CobolName</c>, and no member takes a name as a string and answers a class
/// (<c>NameTableOwnershipDriftTests</c>): a name a SYMBOL carries resolves from the carrier (<c>ClassOf</c>, <c>InterfaceOf</c>,
/// re-lookups of names scope-checked where they were written), and a written word reaches a class either through the funnel or
/// through the two GROUP-WIDE readers, <c>GroupClassNamed</c> and <c>GroupInterfaceNamed</c>, which ignore the scope. This guard
/// adjudicates those two: each caller with its reason and its call count. A new group-wide reading of a written name fails here
/// until it goes through the funnel or is adjudicated.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OoNameResolutionDriftTests
{
    /// <summary>Adjudicated group-wide readers: file → (expected call count, reason). Adding or moving a call is an adjudication.</summary>
    private static readonly Dictionary<string, (int Count, string Reason)> Adjudicated =
        new(StringComparer.Ordinal)
    {
        [Path.Combine("Oo", "OoClassTable.cs")] =
            (4, "a REPOSITORY specifier's own question of the external repository's information, which is the group's whatever "
                + "scope the specifier is written in: §2023:12.3.8.3 SR6 (UnansweredClassSpecifier) and SR9 (UnansweredInterfaceSpecifier) "
                + "ask whether a class or interface of the group answers the word, and SR16 (CheckPropertySpecifiers) whether a class "
                + "or interface of the paragraph has the property — each word a specifier, never a reference"),
    };

    /// <summary>A group-wide reading of a written word.</summary>
    private static readonly Regex GroupReader = new(@"\.\s*Group(?:Class|Interface)Named\(", RegexOptions.Compiled);

    /// <summary>A call of the funnel.</summary>
    private static readonly Regex Funnel = new(@"\.Names\??\.\s*(?:Resolve|Lookup)\(", RegexOptions.Compiled);

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)) continue;
            yield return (Path.GetRelativePath(root, f), File.ReadAllText(f));
        }
    }

    [Fact]
    public void EveryGroupWideReadingOfAWrittenName_IsAdjudicated()
    {
        var found = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerSources())
        {
            int n = GroupReader.Matches(text).Count;
            if (n > 0) found[rel] = n;
        }

        // Population: the readers exist and this scan saw their callers. A rename must fail loudly, not vacuously.
        Assert.True(found.ContainsKey(Path.Combine("Oo", "OoClassTable.cs")),
            "OoClassTable.cs no longer calls GroupClassNamed / GroupInterfaceNamed — this guard must follow the group-wide readers");

        var unadjudicated = found.Where(kv =>
            !Adjudicated.TryGetValue(kv.Key, out var a) || a.Count != kv.Value).ToList();
        Assert.True(unadjudicated.Count == 0,
            "SymbolTable.ClassNames.GroupClassNamed / GroupInterfaceNamed call site(s) outside the adjudicated set (or an adjudicated "
            + "file whose call count changed) — resolve a WRITTEN class-name / interface-name through SymbolTable.ClassNames.Resolve "
            + "or Lookup so the ISO §2023:8.4.6.4 REPOSITORY scope applies, or adjudicate the group-wide reading here WITH its reason "
            + "(kb/Work PB365):\n  "
            + string.Join("\n  ", unadjudicated.Select(kv => $"{kv.Key} ({kv.Value} call(s))")));
    }

    [Fact]   // The complement of the scan above, which can only prove that nobody ELSE reads a name group-wide: the sites the
             // funnel's doc comment names really do call it (feedback_measure_the_selectors_complement).
    public void TheNamedReferenceSites_CallTheFunnel()
    {
        foreach (string rel in new[]
                 {
                     Path.Combine("Oo", "OoClassTable.cs"),                          // INHERITS, IMPLEMENTS
                     Path.Combine("Binding", "Procedure", "UseStatementBinder.cs"),   // USE Format 4
                     // USAGE OBJECT REFERENCE and the object-view's AS phrase: OoDataBinder.DescribeNamedReference (kb/Work PB389).
                     Path.Combine("Binding", "OoDataBinder.cs"),
                     Path.Combine("Binding", "RaisingPhrase.cs"),                     // PD-header + METHOD-ID RAISING (PB815)
                     Path.Combine("Binding", "Procedure", "Verbs", "OoBinder.cs"),    // INVOKE / SET class-name
                     Path.Combine("Binding", "ObjectPropertyResolver.cs"),  // property-ref class-name object (PB1425)
                 })
        {
            string text = File.ReadAllText(Path.Combine(TestRepo.Src("WiseOwl.Cobol.Compiler"), rel));
            Assert.True(Funnel.IsMatch(text),
                $"{rel} no longer resolves its written class-name / interface-name through SymbolTable.ClassNames.Resolve or Lookup "
                + "(kb/Work PB365 — ISO §2023:8.4.6.4 scopes the name to the referring source element)");
        }
    }
}
