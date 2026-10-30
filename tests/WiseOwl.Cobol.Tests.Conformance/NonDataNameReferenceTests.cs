// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A NAME DECLARED AS SOMETHING OTHER THAN A DATA ITEM IS NEVER "NOT DEFINED" (kb/Work PB1906). ISO §8.4.2.1: "a
/// statement shall contain a reference that uniquely identifies that resource", so a reference whose word names no data
/// item is refused — but the word IS declared, and "no declaration in this source element gives the name … Check the
/// spelling" sends the user hunting a typo. <c>DataBinder.NonDataDeclarationOf</c> is the one answer to what the word
/// is declared as, and every site that refuses such a reference asks it: the procedure division's unidentified
/// reference (<c>ReferenceResolver.ReportUnidentified</c>), a file clause's data-name operand
/// (<c>DataBinder.ResolveClauseOperand</c>), an OCCURS DEPENDING ON object and SET Format 4's "not a condition-name"
/// arm (<c>StatementValidation.RejectSetConditionName</c>). A type-name drew "not defined" at all
/// three (§13.18.58.4 GR2: "A type declaration has no storage associated with it"), a type declaration's member at all
/// three (§13.18.58.4 GR1: such data-names "may be referenced only as subordinate items of groups defined using the
/// type-name"), and every other kind at the two clause sites. One case per kind per site.
/// </summary>
public sealed class NonDataNameReferenceTests
{
    private const string Declarations =
        "       01 T IS TYPEDEF.\n"
        + "          05 M PIC XX.\n"
        + "             88 M-ON VALUE \"YY\".\n"
        + "       01 KC CONSTANT AS 5.\n"
        + "       01 TB.\n"
        + "          05 E PIC X OCCURS 3 INDEXED BY IX.\n"
        + "       01 C PIC X.\n"
        + "          88 C-ON VALUE \"Y\".\n";

    private const string SpecialNames =
        "       ENVIRONMENT DIVISION.\n"
        + "       CONFIGURATION SECTION.\n"
        + "       SPECIAL-NAMES.\n"
        + "           ALPHABET AL IS NATIVE.\n";

    private static string StatementProgram(string body) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1906S.\n" + SpecialNames
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n" + Declarations
        + "       PROCEDURE DIVISION.\n" + body + "\n           STOP RUN.\n";

    private static string FileStatusProgram(string operand) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1906F.\n" + SpecialNames
        + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + $"           SELECT F1 ASSIGN TO \"F1\" FILE STATUS IS {operand}.\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD F1.\n       01 R1 PIC X.\n"
        + "       WORKING-STORAGE SECTION.\n" + Declarations
        + "       PROCEDURE DIVISION.\n           STOP RUN.\n";

    private static string DependingProgram(string operand) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1906D.\n" + SpecialNames
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n" + Declarations
        + $"       01 G.\n          05 GE PIC X OCCURS 1 TO 5 DEPENDING ON {operand}.\n"
        + "       PROCEDURE DIVISION.\n           STOP RUN.\n";

    public static TheoryData<string, string> Kinds => new()
    {
        { "T", "is declared as a type-name" },
        { "M", "is declared only by a subordinate entry of the type declaration 'T'" },
        { "M OF T", "names 'M', which is declared only by a subordinate entry of the type declaration 'T'" },
        { "AL", "is declared as an alphabet-name" },
        { "KC", "is declared as a constant-name" },
        { "C-ON", "is declared as a condition-name" },
    };

    /// <summary>The statement site's kinds: a condition-name keeps its one wording (PB567's misqualified-condition text),
    /// and a constant-name in a RECEIVING position breaks a rule of its own first (COBOL1548, §13.10.4 GR1) — its
    /// data-item-position arm is the conformance negative of kb/Work PB3287.</summary>
    public static TheoryData<string, string> StatementKinds => new()
    {
        { "T", "is declared as a type-name" },
        { "M", "is declared only by a subordinate entry of the type declaration 'T'" },
        { "M OF T", "names 'M', which is declared only by a subordinate entry of the type declaration 'T'" },
        { "AL", "is declared as an alphabet-name" },
        { "C-ON", "is a condition-name (a level-88 entry)" },
        { "M-ON", "is declared only by a level-88 entry of the type declaration 'T'" },
    };

    [Theory]
    [MemberData(nameof(StatementKinds))]
    public void AStatementOperand_NamesWhatTheWordIsDeclaredAs(string operand, string expected)
    {
        var (ok, errors) = EditionHarness.Compile(StatementProgram($"           MOVE \"A\" TO {operand}"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertHasDiagnostic(errors, expected);
        EditionHarness.AssertNoDiagnostic(errors, "is not defined");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    [InlineData("IX", "is declared as an index-name")]
    public void AFileStatusOperand_NamesWhatTheWordIsDeclaredAs(string operand, string expected)
    {
        var (ok, errors) = EditionHarness.Compile(FileStatusProgram(operand), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertHasDiagnostic(errors, $"FILE STATUS '{operand}'");
        EditionHarness.AssertHasDiagnostic(errors, expected);
        EditionHarness.AssertNoDiagnostic(errors, "no declaration in this source element gives the name");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    [InlineData("IX", "is declared as an index-name")]
    public void ADependingOnObject_NamesWhatTheWordIsDeclaredAs(string operand, string expected)
    {
        var (ok, errors) = EditionHarness.Compile(DependingProgram(operand), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL0851");
        EditionHarness.AssertHasDiagnostic(errors, expected);
        EditionHarness.AssertNoDiagnostic(errors, "is not defined");
    }

    /// <summary>SET Format 4's own "not a condition-name" arm (§14.9.39.3 SR6) asks the same query: a level-88 entry of a
    /// type declaration DOES declare the word, so "no level-88 entry declares it" would be false.</summary>
    [Fact]
    public void ASetOperandNamingATypeDeclarationCondition_NamesWhatTheWordIsDeclaredAs()
    {
        var (ok, errors) = EditionHarness.Compile(StatementProgram("           SET M-ON TO TRUE"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertHasDiagnostic(errors, "is declared only by a level-88 entry of the type declaration 'T'");
        EditionHarness.AssertNoDiagnostic(errors, "no level-88 entry");
    }

    /// <summary>An index-name in a statement operand keeps its category refusal (§13.18.38.3 SR7, COBOL1637, kb/Work
    /// PB1266) — the query names its kind; the statement site words the rule it breaks.</summary>
    [Fact]
    public void AnIndexNameStatementOperand_IsTheSr7CategoryRefusal()
    {
        var (ok, errors) = EditionHarness.Compile(StatementProgram("           ADD 1 TO IX"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1637");
        EditionHarness.AssertNoDiagnostic(errors, "is not defined");
    }

    /// <summary>A reference to a constant-name whose entry was refused is that one fault's consequence (COBOL2364), never
    /// a constant-name in a data-item position, and never a refusal without a diagnostic (COBOL2362); a clause operand
    /// naming it adds nothing to the entry's own error.</summary>
    [Fact]
    public void AReferenceToARefusedConstant_IsTheRefusedDeclarationConsequence()
    {
        string source =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1906K.\n"
            + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
            + "           SELECT F1 ASSIGN TO \"F1\" FILE STATUS IS K.\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD F1.\n       01 R1 PIC X.\n"
            + "       WORKING-STORAGE SECTION.\n       01 K CONSTANT AS LENGTH OF NOPE.\n"
            + "       PROCEDURE DIVISION.\n           DISPLAY K\n           STOP RUN.\n";
        var (ok, errors) = EditionHarness.Compile(source, 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1547");
        EditionHarness.AssertHasDiagnostic(errors, "COBOL2364");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL2362");
        EditionHarness.AssertNoDiagnostic(errors, "FILE STATUS 'K'");
        Assert.Equal(2, errors.Count);
    }
}
