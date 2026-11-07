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
/// ⛔ A METHOD NAME IS LOOKED UP ON A RECEIVER'S ROSTER IN ONE PLACE, <c>MethodRoster.Find</c> (kb/Work PB4893). The choice between a
/// class's factory and instance interface (§2023:9.3.6; §2023:14.9.23.3 SR4) and the search of an interface's prototype closure were
/// written at seven sites, and the interface copy in <c>OoBinder.OoBindInstanceInvoke</c> compared method-name-1 where every other
/// compared the externalized name (§2023:14.9.23.4 GR2a; §2023:11.7.4 GR1b), so <c>INVOKE I "Y"</c> of a prototype
/// <c>METHOD-ID. X AS "Y"</c> was refused (COBOL0825) while the same call through the implementing class bound. This scan fails
/// when a compiler file writes the selection again: a <c>FindFactoryMethod</c> and a <c>FindMethod</c> chosen by one conditional, or a
/// search of <c>AllPrototypes()</c> by name.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class MethodRosterLookupDriftTests
{
    /// <summary>The factory-or-instance selection written as one conditional expression, in either order.</summary>
    private static readonly Regex FactoryOrInstance = new(
        @"\?\s*[\w.!?]*\.Find(?:Factory)?Method\([^;]*?:\s*[\w.!?]*\.Find(?:Factory)?Method\(", RegexOptions.Compiled);

    /// <summary>A search of an interface's prototype closure for ONE prototype (a predicate over all of them, such as
    /// <c>OoClassTable</c>'s accessor test, selects no method and is not a lookup).</summary>
    private static readonly Regex PrototypeSearch = new(@"AllPrototypes\(\)\s*\.\s*(?:FirstOrDefault|First|SingleOrDefault|Single)\(",
        RegexOptions.Compiled);

    private static readonly string Home = Path.Combine("Binding", "Model", "MethodRoster.cs");

    [Fact]
    public void EveryRosterLookup_IsMethodRosterFind()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        var offenders = new List<string>();
        bool homeSeen = false;
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            if (rel.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || rel.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue;
            string text = File.ReadAllText(file);
            if (rel == Home)
            {
                homeSeen = FactoryOrInstance.IsMatch(text) && PrototypeSearch.IsMatch(text);
                continue;
            }
            int n = FactoryOrInstance.Matches(text).Count + PrototypeSearch.Matches(text).Count;
            if (n > 0) offenders.Add($"{rel} ({n})");
        }
        // The population: the home still writes both selections, so a rename fails here instead of passing vacuously.
        Assert.True(homeSeen, $"{Home} no longer writes the roster selection this scan looks for — the guard must follow it");
        Assert.True(offenders.Count == 0,
            "a method name is selected on a roster outside MethodRoster.Find — ask MethodRoster.Of(...).Find(name), the one lookup keyed "
            + "by the externalized name (kb/Work PB4893; §2023:14.9.23.4 GR2a):\n  " + string.Join("\n  ", offenders));
    }
}
