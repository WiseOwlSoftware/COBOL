// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A PARENTHESIZED ITEM AT A CHARACTER POSITION OF A STRING FUNCTION RUNS UNDER <c>--permissive</c> AS THE ITEM DOES (kb/Work PB4635,
/// review R2-3). Strict conformance refuses it: <c>(X9)</c> is "an arithmetic expression enclosed in parentheses" (§8.8.1.1), whose operand
/// must reference a numeric data item (COBOL0844), and an arithmetic expression is class numeric where TRIM and UPPER-CASE require a
/// character class (§15.96.3 r1, §15.97.3 r1; COBOL1627). <c>--permissive</c> demotes both to warnings and coerces, and the coercion every
/// other operand that holds characters gets is its own image, so the enclosed item is rendered. The binder wraps the item as a computed
/// operand to make it an expression; before the string-argument visitor read the item back out of the wrapper, the coercion reached the
/// visitor's loud arm and the program died at run time with <c>NotImplementedCobolFeatureException</c>.
/// <para>It is not a conformance golden because the shape is a refusal in every strict edition, so the only mode in which it runs is
/// <c>--permissive</c> (as <c>FunctionValueLanePermissiveTests</c> is not one). The expected text is the bare spelling's: the same call with the
/// parentheses removed, compiled in the same program.</para>
/// </summary>
public sealed class ParenthesizedIntrinsicArgumentPermissiveTests : WiseOwlCobolTestBase
{
    [Fact]
    public void AParenthesizedItemAtACharacterPosition_RendersAsTheBareItemDoes()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4635PERM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X9 PIC X(9) VALUE "123456789".
            01 XL PIC X(5) VALUE " abc ".
            01 I2 PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "TRIM=" FUNCTION TRIM(X9) "|" FUNCTION TRIM((X9)) "|"
                DISPLAY "TRIML=" FUNCTION TRIM(XL) "|" FUNCTION TRIM((XL)) "|"
                DISPLAY "UPPER=" FUNCTION UPPER-CASE(XL) "|" FUNCTION UPPER-CASE((XL)) "|"
                DISPLAY "NUMERIC=" FUNCTION TRIM(I2) "|" FUNCTION TRIM((I2)) "|"
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb4635perm.cob");
        string dllPath = Path.Combine(TempDir, "pb4635perm.dll");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, dllPath, DialectLevel: 2023, Permissive: true, SourceFormat: InitialReferenceFormat.Free));
        Assert.True(compiled.Success, $"({compiled.Status}):\n  {string.Join("\n  ", compiled.Errors)}");

        var (ran, stdout, detail) = RunAssembly(dllPath);
        Assert.True(ran, detail);
        Assert.Equal(
            ["TRIM=123456789|123456789|", "TRIML=abc|abc|", "UPPER= ABC | ABC |", "NUMERIC=2|2|"],
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>
    /// ⛔ THE SHAPE RULE OF A CHARACTER-READING POSITION HAS NO <c>--permissive</c> COERCION (kb/Work PB4635, review R3-1 and R3-3).
    /// CONCAT's arguments are "data items or literals" (§15.18.3 r1) and BASECONVERT's argument-1 "a usage display or national data item
    /// or literal" (§15.12.3 r1); a parenthesized item is an arithmetic expression (§8.8.1.1) and a parenthesized boolean item "a boolean
    /// expression enclosed in parentheses" (§8.8.2), neither of them either. The coercion <c>--permissive</c> offers an operand that holds
    /// characters is its image, and an expression has none, so the violation stays an error in the mode that demotes the class rules
    /// (FIND-STRING argument-3, an INTEGER position, reads the expression's value and keeps its coercion:
    /// <see cref="IntrinsicArgumentRules.IsCoercible"/>).
    /// </summary>
    [Theory]
    [InlineData("FUNCTION CONCAT((U2) \"A\")")]
    [InlineData("FUNCTION CONCAT(\"A\" (U2))")]
    [InlineData("FUNCTION BASECONVERT((U2) 10 16)")]
    [InlineData("FUNCTION CONCAT((BB) \"A\")")]
    [InlineData("FUNCTION LENGTH((BB))")]
    public void AParenthesizedItemAtACharacterReadingPosition_IsAnErrorEvenUnderPermissive(string call)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4635SHAPE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U2 PIC 99 VALUE 12.
            01 BB PIC 1(4) VALUE B"1010".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY {call}
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb4635shape.cob");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, Path.Combine(TempDir, "pb4635shape.dll"), DialectLevel: 2023, Permissive: true, SourceFormat: InitialReferenceFormat.Free));
        Assert.False(compiled.Success, $"{call} compiled under --permissive");
        Assert.Contains("COBOL1627", string.Join("\n", compiled.Errors));
    }
}
