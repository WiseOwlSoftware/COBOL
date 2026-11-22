// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE §2023:15.5 DATE FORMS ARE SPELLED IN ONE TYPE (kb/Work PB2634): the integer-date-form ceiling 3,067,671, the
/// 1601-01-01 epoch and the Gregorian calendar predicates live in <c>IntegerDateForm</c>, and every date intrinsic and
/// LOCALE-* body asks it. Before it the ceiling was a bare literal at six sites and the day-number conversion was written
/// out at five, and a forgotten copy had already cost two defects (kb/Work R25, PB23). A runtime or compiler source that
/// spells one of them again fails here.
/// </summary>
public sealed class IntegerDateFormDriftTests
{
    private static readonly Regex Spelling = new(
        @"\b3_?067_?671\b|new(?: DateTime)?\(1601, 1, 1\)|DateTime\.(?:IsLeapYear|DaysInMonth)\(",
        RegexOptions.Compiled);

    [Fact]
    public void NoSourceButIntegerDateForm_SpellsTheDateFormsRules()
    {
        string owner = Path.GetFullPath(TestRepo.Src("WiseOwl.Cobol.Runtime", "Intrinsics", "IntegerDateForm.cs"));
        Assert.Matches(Spelling, File.ReadAllText(owner));                  // non-vacuity: the owner spells them
        var offenders = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !string.Equals(Path.GetFullPath(f), owner, StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal) && Spelling.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(TestRepo.Root, x.f)}:{x.i + 1}: {x.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "the §2023:15.5 date forms are IntegerDateForm's (kb/Work PB2634) — ask IntegerDateForm.Contains / Of / ToDate / "
            + "IsStandardDate / DaysInMonth / DaysInYear instead of spelling the rule:\n" + string.Join("\n", offenders));
    }
}
