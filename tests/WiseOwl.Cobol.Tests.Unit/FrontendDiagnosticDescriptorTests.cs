// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The frontend's diagnostic descriptors (<see cref="DiagnosticDescriptors"/>) are the ONE text of each parser hint and COPY
/// error: <c>CobolErrorStrategy</c> and <see cref="DiagnosticBag"/>'s descriptor report compose the message through <see cref="DiagnosticDescriptor.Format"/>, and no site types the sentence again (kb/Work
/// PB2624: seventeen hint rows declared a severity no path reported and kept a second, hand-copied message).
/// </summary>
public sealed partial class FrontendDiagnosticDescriptorTests
{
    private static IEnumerable<DiagnosticDescriptor> All()
        => typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
            .Select(f => (DiagnosticDescriptor)f.GetValue(null)!);

    [GeneratedRegex(@"\{(\d+)\}")]
    private static partial Regex Placeholder();

    /// <summary>Every template is a composite format whose placeholders are numbered 0..n-1 with no gap, so the composition
    /// a parse error runs cannot throw (a malformed template would surface only when the parse failed at that hint).</summary>
    [Fact]
    public void EveryTemplate_ComposesWithItsOwnPlaceholderCount()
    {
        var descriptors = All().ToArray();
        Assert.True(descriptors.Length >= 20, $"DiagnosticDescriptors enumerated to {descriptors.Length}; the reflection is broken");
        foreach (var d in descriptors)
        {
            var numbers = Placeholder().Matches(d.MessageTemplate).Select(m => int.Parse(m.Groups[1].Value)).Distinct().Order().ToArray();
            Assert.True(numbers.SequenceEqual(Enumerable.Range(0, numbers.Length)),
                $"{d.Code}: template placeholders {string.Join(",", numbers)} are not 0..{numbers.Length - 1}");
            var args = Enumerable.Range(0, numbers.Length).Select(i => (object?)$"<arg{i}>").ToArray();
            string composed = d.Format(args);
            for (int i = 0; i < numbers.Length; i++) Assert.Contains($"<arg{i}>", composed);
        }
    }

    /// <summary>The hint with two arguments (the unowned scope terminator, ISO §2023:14.5.3.2) composes the sentence
    /// <c>CobolErrorStrategy</c> used to type by hand.</summary>
    [Fact]
    public void ScopeTerminatorHint_ComposesFromItsTemplate()
        => Assert.Equal("END-IF appears without a matching IF statement.",
            DiagnosticDescriptors.COBOL0306.Format("END-IF", "IF"));

    /// <summary>A template with no placeholder is its own message.</summary>
    [Fact]
    public void ArgumentFreeHint_IsItsTemplate()
        => Assert.Equal("A period may be missing at the end of the previous sentence.", DiagnosticDescriptors.COBOL0307.Format());

    /// <summary>A COPY error reported through the bag is an error, with its message composed from its template.</summary>
    [Fact]
    public void BagReport_IsAnErrorComposedFromTheTemplate()
    {
        var bag = new DiagnosticBag();
        bag.Report(DiagnosticDescriptors.CBL3621, default, default, "BOOK");
        var only = Assert.Single(bag.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, only.Severity);
        Assert.Equal("CBL3621", only.Code);
        Assert.Equal("Circular COPY: copybook 'BOOK' is already being included", only.Message);
    }
}
