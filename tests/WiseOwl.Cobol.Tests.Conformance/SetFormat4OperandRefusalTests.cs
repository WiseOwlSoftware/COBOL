// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ SET Format 4's two operand refusals name the mistake the program made, in the rule of the edition it is compiled at (kb/Work
/// PB6355, PB6356; the PB4950 rule).
/// <para>PB6355: a Format-4 operand that names a resource of another category (a data item, a procedure-name, a mnemonic-name, an
/// alphabet-name, a class-name, an index-name) used to draw COBOL1639 "the reference identifies no resource", which is false: the
/// name identifies one. The broken rule is SR6, "Condition-name-1 shall be associated with a conditional variable"
/// (§2023:14.9.39.3 SR6; §2014:14.9.35.2 SR6; §2002:14.8.35.2 SR6; §1985:VI.6.23.3 SR6), so the refusal is COBOL1757 saying what the
/// operand is. COBOL1639 stays for a name that resolves to nothing.</para>
/// <para>PB6356: <c>SET condition-name TO FALSE</c> is a COBOL-2002 construct, so below 2002 the edition gate (COBOL0900) is the one
/// refusal and SR7's COBOL2049 stands down; from 2002 on COBOL2049 cites that edition's own SR7.</para>
/// </summary>
public sealed class SetFormat4OperandRefusalTests
{
    private static string Sr6(int edition) => edition switch
    {
        85 => "§1985:VI.6.23.3 SR6",
        2002 => "§2002:14.8.35.2 SR6",
        2014 => "§2014:14.9.35.2 SR6",
        _ => "§2023:14.9.39.3 SR6",
    };

    private static string Sr7(int edition) => edition switch
    {
        2002 => "§2002:14.8.35.2 SR7",
        2014 => "§2014:14.9.35.2 SR7",
        _ => "§2023:14.9.39.3 SR7",
    };

    private static readonly string[] AllRules =
        ["§1985:VI.6.23.3 SR6", "§2002:14.8.35.2 SR6", "§2014:14.9.35.2 SR6", "§2023:14.9.39.3 SR6"];

    private static string Program(string statement) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB6355S.\n"
        + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
        + "           SWITCH-1 IS SW1 ON STATUS IS SW1-ON\n"
        + "           ALPHABET AL IS NATIVE\n"
        + "           CLASS CL IS \"A\" THRU \"Z\".\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 W PIC 9.\n"
        + "       01 TBL.\n          05 E PIC 9 OCCURS 3 INDEXED BY IX.\n"
        + "       PROCEDURE DIVISION.\n       PARA-1.\n"
        + $"           {statement}\n           STOP RUN.\n";

    public static TheoryData<int, string, string> WrongCategories()
    {
        var data = new TheoryData<int, string, string>();
        foreach (int edition in EditionHarness.Editions)
        {
            data.Add(edition, "W", "'W' is a data item");
            data.Add(edition, "PARA-1", "'PARA-1' is a procedure-name (a paragraph-name or section-name)");
            data.Add(edition, "SW1", "'SW1' is a mnemonic-name (a SPECIAL-NAMES implementor-name entry)");
            data.Add(edition, "AL", "'AL' is an alphabet-name");
            data.Add(edition, "CL", "'CL' is a class-name");
            data.Add(edition, "IX", "'IX' is an index-name");
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(WrongCategories))]
    public void AFormat4OperandOfAnotherCategory_NamesItsCategoryAndTheEditionsSr6(int edition, string operand, string expected)
    {
        var (ok, errors) = EditionHarness.Compile(Program($"SET {operand} TO TRUE."), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1757");
        EditionHarness.AssertHasDiagnostic(errors, $"SET '{operand}' TO TRUE — {expected}");
        EditionHarness.AssertHasDiagnostic(errors, "not a condition-name associated with a conditional variable");
        EditionHarness.AssertHasDiagnostic(errors, Sr6(edition));
        foreach (string other in AllRules.Where(r => r != Sr6(edition)))
            EditionHarness.AssertNoDiagnostic(errors, other);
        EditionHarness.AssertNoDiagnostic(errors, "identifies no resource");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
    }

    /// <summary>A name that resolves to nothing keeps COBOL1639, with the edition's own SR6.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AFormat4OperandThatNamesNothing_KeepsCobol1639(int edition)
    {
        var (ok, errors) = EditionHarness.Compile(Program("SET NOWHERE TO TRUE."), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertHasDiagnostic(errors, "SET 'NOWHERE' TO TRUE — 'NOWHERE' is not a condition-name");
        EditionHarness.AssertHasDiagnostic(errors, "identifies no resource");
        EditionHarness.AssertHasDiagnostic(errors, Sr6(edition));
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1757");
    }

    /// <summary>The switch-status condition-name arm quotes the SR6 text and its clauses from the edition compiled at.</summary>
    [Theory]
    [InlineData(85, "§1985:VI.4.5.4 GR2")]
    [InlineData(2002, "§2002:12.2.6")]
    [InlineData(2014, "§2014:12.3.6")]
    [InlineData(2023, "§2023:12.3.7")]
    public void ASwitchStatusConditionName_CitesTheEditionsOwnClauses(int edition, string switchClause)
    {
        var (ok, errors) = EditionHarness.Compile(Program("SET SW1-ON TO TRUE."), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1757");
        EditionHarness.AssertHasDiagnostic(errors, switchClause);
        EditionHarness.AssertHasDiagnostic(errors, Sr6(edition));
        foreach (string other in AllRules.Where(r => r != Sr6(edition)))
            EditionHarness.AssertNoDiagnostic(errors, other);
    }

    private const string FalseWithoutPhrase =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB6356S.\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 W PIC 9.\n          88 W-ON VALUE 1.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-PARA.\n"
        + "           SET W-ON TO FALSE.\n           STOP RUN.\n";

    /// <summary>Below COBOL-2002 the edition gate is the one refusal of SET condition-name TO FALSE: COBOL2049 is no second report.</summary>
    [Fact]
    public void AtCobol85_OnlyTheEditionGateRefusesSetToFalse()
    {
        var (ok, errors) = EditionHarness.Compile(FalseWithoutPhrase, 85);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL0900: SET condition-name TO FALSE requires COBOL-2002");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL2049");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL2362");
        EditionHarness.AssertNoDiagnostic(errors, "ISO §2023:14.9.39.3 SR7");
    }

    /// <summary>From COBOL-2002 on SR7 is the rule broken, and each edition's refusal cites its own.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void FromCobol2002_Cobol2049CitesTheEditionsOwnSr7(int edition)
    {
        var (ok, errors) = EditionHarness.Compile(FalseWithoutPhrase, edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL2049");
        EditionHarness.AssertHasDiagnostic(errors, Sr7(edition));
        foreach (int other in new[] { 2002, 2014, 2023 }.Where(e => e != edition))
            if (Sr7(other) != Sr7(edition)) EditionHarness.AssertNoDiagnostic(errors, Sr7(other));
        EditionHarness.AssertNoDiagnostic(errors, "COBOL0900: SET condition-name TO FALSE");
    }
}
