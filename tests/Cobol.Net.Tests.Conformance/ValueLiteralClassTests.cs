// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB94 — the VALUE clause's literal-class rules for numeric (ISO §13.18.63.3 SR2) and alphabetic /
/// alphanumeric / alphanumeric-edited (SR4) subjects: an error strict at every edition; under <c>--permissive</c>
/// the REPRESENTABLE vendor leniency is a COBOLNET1657 warning plus the value (a digits-only alphanumeric literal on a
/// numeric item is that number, ALL "digits" repeated to the digit width; a numeric literal on an alphanumeric item
/// is its characters as MOVE would store them; a character figurative on a numeric item is ZERO); a literal no
/// numeric item can hold (`PIC 9 VALUE "abc"` — which used to reach the C# backend) is an error on both axes.
/// </summary>
public sealed class ValueLiteralClassTests
{
    private const string Head = "IDENTIFICATION DIVISION.\nPROGRAM-ID. PB94T.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n";

    private static string Program(string decl) =>
        Head + decl + "\nPROCEDURE DIVISION.\n    DISPLAY \"[\" A \"]\".\n    STOP RUN.\n";

    [Theory]
    [InlineData("01 A PIC 99 VALUE \"7\".", "[07]")]
    [InlineData("01 A PIC 9(3) COMP VALUE \"12\".", "[012]")]
    [InlineData("01 A PIC 9V9 VALUE \"1.5\".", "[15]")]
    [InlineData("01 A PIC 9(3) VALUE ALL \"1\".", "[111]")]
    [InlineData("01 A PIC 9(3) VALUE SPACES.", "[000]")]
    [InlineData("01 A PIC X(3) VALUE 12.", "[12 ]")]
    [InlineData("01 A PIC A(3) VALUE 12.", "[12 ]")]
    public void ClassMismatch_IsAnErrorStrict_AndTheRepresentableValueWithAWarningPermissive(string decl, string expected)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(decl), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1657");
        var (pok, stdout, detail) = EditionHarness.CompileAndRun(Program(decl), 2023, permissive: true);
        Assert.True(pok, detail);
        Assert.Equal(expected, stdout.Trim());
        var (_, _, warnings) = EditionHarness.CompileFull(Program(decl), 2023, permissive: true);
        Assert.Contains(warnings, w => w.Contains("COBOLNET1657"));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void NonNumericContentOnNumeric_IsAnErrorOnBothAxes(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("01 A PIC 9 VALUE \"abc\"."), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1657");
        var (pok, perrors, _) = EditionHarness.CompileFull(Program("01 A PIC 9 VALUE \"abc\"."), edition, permissive: true);
        Assert.False(pok);
        EditionHarness.AssertHasDiagnostic(perrors, "COBOLNET1657");
    }

    [Theory]
    [InlineData("01 A PIC 9(3) VALUE ZERO.", "[000]")]                        // figurative ZERO is numeric (SR2)
    [InlineData("01 A PIC X(3) VALUE ZERO.", "[000]")]                        // a figurative permitted for the category
    [InlineData("01 A PIC 999 VALUE \"000\" BLANK WHEN ZERO.", "[000]")]      // BLANK WHEN ZERO makes it numeric-EDITED (SR8: no blanking on an alphanumeric VALUE)
    [InlineData("01 A PIC X(3) VALUE \"12\".", "[12 ]")]
    [InlineData("01 A PIC 9(3) VALUE 12.", "[012]")]
    public void LegalShapes_StillCompileStrict(string decl, string expected)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Program(decl), 2023);
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.Trim());
    }

    [Fact]   // the condition-name (Format 3) VALUEs ride the same funnel (SR2 is an ALL FORMATS rule)
    public void ConditionName_OnNumericVariable_TakesNumericLiteralsOnly()
    {
        string src = Head + "01 N PIC 9 VALUE 1.\n   88 N-BLANK VALUE \" \".\nPROCEDURE DIVISION.\n    IF N-BLANK DISPLAY \"B\" ELSE DISPLAY \"N\" END-IF.\n    STOP RUN.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1657");
    }
}
