// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY CATALOGUED FUNCTION STATES WHAT FIXES ITS RETURNED VALUE (kb/Work PB5891).
/// </summary>
/// <remarks>
/// The type 6 classifier reads two references of a function as one term only when the function's arguments fix its value
/// (<c>IntrinsicArgumentRules.SameTerm</c>, docs/INTRINSICS_DESIGN.md, the type-6 intake, rule 1): <c>FUNCTION SQRT (N) - FUNCTION
/// SQRT (N) + 5</c> is the constant 5, while <c>FUNCTION RANDOM - FUNCTION RANDOM + 1</c> is not shown to be one. A function whose
/// value the run unit's state fixes and that the catalog called <see cref="IntrinsicValueSource.Arguments"/> would cancel against
/// itself silently. So the catalog's column is held to the source reviewed here for EVERY function, and a function added later fails
/// this test until its value source is reviewed and recorded.
/// </remarks>
public sealed class IntrinsicValueSourceDriftTests
{
    /// <summary>The functions whose value their arguments do NOT fix, and why; every other catalogued function is
    /// <see cref="IntrinsicValueSource.Arguments"/>.</summary>
    private static readonly Dictionary<string, (IntrinsicValueSource Source, string Why)> NotByArguments = new(StringComparer.Ordinal)
    {
        ["RANDOM"] = (IntrinsicValueSource.RunUnitState, "§2023:15.75: each reference without a seed returns the next value of the sequence"),
        ["CURRENT-DATE"] = (IntrinsicValueSource.RunUnitState, "the date and time at the reference"),
        ["FORMATTED-CURRENT-DATE"] = (IntrinsicValueSource.RunUnitState, "the date and time at the reference"),
        ["SECONDS-PAST-MIDNIGHT"] = (IntrinsicValueSource.RunUnitState, "the time at the reference"),
        ["WHEN-COMPILED"] = (IntrinsicValueSource.RunUnitState, "the time of compilation, which no argument gives"),
        ["EXCEPTION-FILE"] = (IntrinsicValueSource.RunUnitState, "the last exception raised, which a term evaluated between two references can raise"),
        ["EXCEPTION-FILE-N"] = (IntrinsicValueSource.RunUnitState, "the last exception raised"),
        ["EXCEPTION-LOCATION"] = (IntrinsicValueSource.RunUnitState, "the last exception raised"),
        ["EXCEPTION-LOCATION-N"] = (IntrinsicValueSource.RunUnitState, "the last exception raised"),
        ["EXCEPTION-STATEMENT"] = (IntrinsicValueSource.RunUnitState, "the last exception raised"),
        ["EXCEPTION-STATUS"] = (IntrinsicValueSource.RunUnitState, "the last exception raised"),
        ["YEAR-TO-YYYY"] = (IntrinsicValueSource.ArgumentsOrCurrentYear, "§2023:15.100.3 AR5: an omitted argument-3 is FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4))"),
        ["DATE-TO-YYYYMMDD"] = (IntrinsicValueSource.ArgumentsOrCurrentYear, "windows the year as YEAR-TO-YYYY does"),
        ["DAY-TO-YYYYDDD"] = (IntrinsicValueSource.ArgumentsOrCurrentYear, "windows the year as YEAR-TO-YYYY does"),
    };

    /// <summary>Every function of the catalog whose value source was reviewed for kb/Work PB5891 (the catalog at that landing). A
    /// new function fails <see cref="EveryCataloguedFunction_HasTheReviewedValueSource"/> until it is added here, and to
    /// <see cref="NotByArguments"/> when its arguments do not fix its value.</summary>
    private static readonly string[] Reviewed =
    [
        "ABS", "ACOS", "ANNUITY", "ASIN", "ATAN", "BASECONVERT", "BOOLEAN-OF-INTEGER", "BYTE-LENGTH", "CHAR", "CHAR-NATIONAL",
        "COMBINED-DATETIME", "CONCAT", "CONVERT", "COS", "CURRENT-DATE", "DATE-OF-INTEGER", "DATE-TO-YYYYMMDD", "DAY-OF-INTEGER",
        "DAY-TO-YYYYDDD", "DISPLAY-OF", "E", "EXCEPTION-FILE", "EXCEPTION-FILE-N", "EXCEPTION-LOCATION", "EXCEPTION-LOCATION-N",
        "EXCEPTION-STATEMENT", "EXCEPTION-STATUS", "EXP", "EXP10", "FACTORIAL", "FIND-STRING", "FORMATTED-CURRENT-DATE",
        "FORMATTED-DATE", "FORMATTED-DATETIME", "FORMATTED-TIME", "FRACTION-PART", "HIGHEST-ALGEBRAIC", "INTEGER",
        "INTEGER-OF-BOOLEAN", "INTEGER-OF-DATE", "INTEGER-OF-DAY", "INTEGER-OF-FORMATTED-DATE", "INTEGER-PART", "LENGTH",
        "LOCALE-COMPARE", "LOCALE-DATE", "LOCALE-TIME", "LOCALE-TIME-FROM-SECONDS", "LOG", "LOG10", "LOWER-CASE",
        "LOWEST-ALGEBRAIC", "MAX", "MEAN", "MEDIAN", "MIDRANGE", "MIN", "MOD", "MODULE-NAME", "NATIONAL-OF", "NUMVAL", "NUMVAL-C",
        "NUMVAL-F", "ORD", "ORD-MAX", "ORD-MIN", "PI", "PRESENT-VALUE", "RANDOM", "RANGE", "REM", "REVERSE", "SECONDS-FROM-FORMATTED-TIME",
        "SECONDS-PAST-MIDNIGHT", "SIGN", "SIN", "SMALLEST-ALGEBRAIC", "SQRT", "STANDARD-COMPARE", "STANDARD-DEVIATION", "SUBSTITUTE",
        "SUM", "TAN", "TEST-DATE-YYYYMMDD", "TEST-DAY-YYYYDDD", "TEST-FORMATTED-DATETIME", "TEST-NUMVAL", "TEST-NUMVAL-C",
        "TEST-NUMVAL-F", "TRIM", "UPPER-CASE", "VARIANCE", "WHEN-COMPILED", "YEAR-TO-YYYY",
    ];

    [Fact]
    public void EveryCataloguedFunction_HasTheReviewedValueSource()
    {
        var catalog = ((System.Collections.IDictionary)typeof(IntrinsicCatalog)
            .GetField("Table", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!)
            .Values.Cast<IntrinsicSig>().OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
        Assert.True(catalog.Count > 0, "the intrinsic catalog enumerated no function");
        var reviewed = Reviewed.ToHashSet(StringComparer.Ordinal);
        var wrong = new List<string>();
        foreach (var sig in catalog)
        {
            if (!reviewed.Contains(sig.Name))
            {
                wrong.Add($"{sig.Name}: not reviewed - decide whether its arguments fix its value, record it in Reviewed (and in "
                    + "NotByArguments when they do not), and give the catalog row its ValueSource");
                continue;
            }
            var expected = NotByArguments.TryGetValue(sig.Name, out var row) ? row.Source : IntrinsicValueSource.Arguments;
            if (sig.ValueSource != expected) wrong.Add($"{sig.Name}: the catalog says {sig.ValueSource}, the review says {expected}");
        }
        var gone = reviewed.Where(n => catalog.All(s => s.Name != n)).Order(StringComparer.Ordinal).ToList();
        Assert.True(wrong.Count == 0 && gone.Count == 0,
            "the catalog's value sources disagree with the review (kb/Work PB5891):\n  " + string.Join("\n  ", wrong)
            + (gone.Count == 0 ? "" : "\nreviewed names the catalog no longer has:\n  " + string.Join("\n  ", gone)));
    }
}
