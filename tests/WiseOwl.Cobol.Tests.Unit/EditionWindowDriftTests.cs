// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE EDITION WINDOW OF A CATALOGUED ELEMENT IS ONE TYPE, AND ITS VERDICT AND SEVERITY ARE ONE METHOD (kb/Work PB2620).
/// <para>The construct registry, the intrinsic-function catalog, the exception-name catalog and the file description clause
/// table each carry an element's editions. Each used to compare those columns with the edition itself, and the intrinsic arm
/// decided its own severity: a function given a <c>RemovedIn</c> would have been an error under <c>--permissive</c>, where every
/// removed construct is a warning. <see cref="EditionWindow"/> is now the only reader of the columns: this class holds its
/// verdicts to <see cref="EditionSeverityPolicy"/> and refuses a source line that compares a window column again.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EditionWindowDriftTests
{
    private const string Home = "src/WiseOwl.Cobol.Editions/EditionWindow.cs";

    private static readonly TreeScope ProductSources = new(Under: ["src/"], Kinds: [".cs"]);

    private static readonly EditionGateCodes Codes = new("NEW", "GONE");

    [Fact]
    public void ARemovedElement_IsAnErrorStrict_AndAWarningPermissive_WhicheverCatalogHoldsIt()
    {
        var window = new EditionWindow(85, RemovedIn: 2014);
        var strict = window.VerdictAt(EditionInfo.Of(2023), Codes, "x", "the x element");
        var permissive = window.VerdictAt(EditionInfo.Of(2023, permissive: true), Codes, "x", "the x element");
        Assert.NotNull(strict);
        Assert.NotNull(permissive);
        Assert.Equal((ConstructAvailability.Removed, EditionSeverity.Error, "GONE"),
            (strict.Value.Availability, strict.Value.Severity, strict.Value.Code));
        Assert.Equal((ConstructAvailability.Removed, EditionSeverity.Warning, "GONE"),
            (permissive.Value.Availability, permissive.Value.Severity, permissive.Value.Code));
        Assert.Equal(new EditionFlag(EditionFlagKind.Removed, "x", "the x element", 2014), strict.Value.Flag);
    }

    [Fact]
    public void ANotYetIntroducedElement_IsAnErrorOnBothAxes_AndNoFlag()
    {
        var window = new EditionWindow(2014);
        foreach (bool permissive in new[] { false, true })
        {
            var verdict = window.VerdictAt(EditionInfo.Of(2002, permissive), Codes, "x", "the x element");
            Assert.NotNull(verdict);
            Assert.Equal((ConstructAvailability.NotYetIntroduced, EditionSeverity.Error, "NEW", (EditionFlag?)null),
                (verdict.Value.Availability, verdict.Value.Severity, verdict.Value.Code, verdict.Value.Flag));
        }
        Assert.Null(window.VerdictAt(EditionInfo.Of(2014), Codes, "x", "the x element"));
    }

    [Fact]
    public void AnObsoleteElement_IsInForce_AndAWarningWithItsFlag()
    {
        var window = new EditionWindow(85, RemovedIn: 2002, ObsoleteIn: 85);
        Assert.True(window.IsInForceAt(85));
        Assert.False(window.IsInForceAt(2002));
        var verdict = window.VerdictAt(EditionInfo.Of(85), Codes, "x", "the x element");
        Assert.NotNull(verdict);
        Assert.Equal((ConstructAvailability.Obsolete, EditionSeverity.Warning, EditionCodes.ObsoleteFlag),
            (verdict.Value.Availability, verdict.Value.Severity, verdict.Value.Code));
        Assert.Equal(new EditionFlag(EditionFlagKind.Obsolete, "x", "the x element", 85), verdict.Value.Flag);
    }

    /// <summary>A window column compared with anything: <c>x.IntroducedIn &gt; level</c>, <c>level &gt;= r.RemovedIn</c>, or the
    /// pattern <c>RemovedIn is { } r &amp;&amp; …</c> that reads the column to compare it.</summary>
    private static readonly Regex ColumnComparison = new(
        @"\b(?:IntroducedIn|RemovedIn|ObsoleteIn)\s*(?:<=|>=|<|>)(?!>)|(?<![=-])(?:<=|>=|<|>)\s*[\w.!]*\b(?:IntroducedIn|RemovedIn|ObsoleteIn)\b"
        + @"|\b(?:RemovedIn|ObsoleteIn)\s+is\s+\{\s*\}\s*\w+\s*&&",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void NoSourceOutsideTheWindow_ComparesAWindowColumn()
    {
        var offenders = new List<string>();
        foreach (string path in TrackedTree.Files(ProductSources))
        {
            if (path == Home) continue;
            string[] lines = File.ReadAllText(TestRepo.Within("src/", path)).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal)) continue;
                if (ColumnComparison.IsMatch(line)) offenders.Add($"{path}:{i + 1}: {line.Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "an edition-window column is compared outside EditionWindow (ask the element's Window instead; kb/Work PB2620):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void TheSweep_SeesTheWindowsOwnComparisons()
    {
        // A detector that stopped matching would pass the sweep above vacuously: the window's own body must match it.
        string home = File.ReadAllText(TestRepo.Within("src/", Home));
        Assert.Contains(home.Split('\n'), line => ColumnComparison.IsMatch(line.TrimStart()));
    }
}
