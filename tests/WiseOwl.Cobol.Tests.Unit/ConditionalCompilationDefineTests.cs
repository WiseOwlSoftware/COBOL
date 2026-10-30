// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Linq;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;DEFINE</c> directive (ISO §7.3.11) Wave-D behaviour in <see cref="ConditionalCompilationProcessor"/>:
/// the §7.3.11.3 SR2 no-OVERRIDE redefinition check (COBOL1618), the GR3 OVERRIDE phrase, and the GR4
/// <c>AS PARAMETER</c> operating-environment source.
/// </summary>
public sealed class ConditionalCompilationDefineTests
{
    private static (string Text, DiagnosticBag Diags) Run(string src)
    {
        var bag = new DiagnosticBag();
        string outp = ConditionalCompilationProcessor.Process(src, diagnostics: bag, sourcePath: "t.cob");
        return (outp, bag);
    }

    private static bool Has1618(DiagnosticBag b) => b.Diagnostics.Any(d => d.Code == "COBOL1618");

    [Fact] // SR2 (§7.3.11.3 #2) — a no-OVERRIDE redefinition to a DIFFERENT value is rejected.
    public void Redefine_DifferentValue_NoOverride_Rejected1618()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 2\n");
        Assert.True(Has1618(diags));
    }

    [Fact] // SR2 third bullet — redefinition to the SAME value is allowed.
    public void Redefine_SameValue_NoOverride_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 1\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // GR3 — OVERRIDE unconditionally redefines (SR2 bypassed).
    public void Redefine_WithOverride_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 2 OVERRIDE\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // SR2 second bullet — a previous OFF clears the variable, so a later redefine is not a violation.
    public void Redefine_AfterOff_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X OFF\n>>DEFINE X AS 2\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // GR4 — AS PARAMETER sources the value from the operating environment (numeric here). The compilation-
           // variable-name is a COBOL user-defined word (§8.3.2.1 — letters, digits, hyphen and underscore;
           // ⚠ the lexer does not yet accept the underscore §8.3.2.1 permits — kb/Work PB159 reports it).
    public void Parameter_SourcesFromEnvironment()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PARAM", "42");
        try
        {
            var (text, diags) = Run(">>DEFINE CN-CC-TEST-PARAM AS PARAMETER\n>>IF CN-CC-TEST-PARAM = 42\nKEEP\n>>END-IF\n");
            Assert.False(Has1618(diags));
            Assert.Contains("KEEP", text);   // the >>IF matched → the guarded line survives
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-PARAM", null); }
    }

    [Fact] // GR4 — when the environment supplies no value, the variable is NOT defined.
    public void Parameter_Unavailable_NotDefined()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-UNSET", null);
        var (text, _) = Run(">>DEFINE CN-CC-TEST-UNSET AS PARAMETER\n>>IF CN-CC-TEST-UNSET DEFINED\nKEEP\n>>END-IF\n");
        Assert.DoesNotContain("KEEP", text);   // undefined → the >>IF DEFINED is false → the line drops
    }

    [Fact] // kb/Work PB1533 — a COBOL word is case-insensitive (§8.3.1), so two spellings of ONE compilation-variable-name
           // read ONE environment variable: the implementor-defined method (DOC-A.1-49) names the variable by the
           // name's canonical UPPER-CASE spelling, whatever the spelling in the directive.
    public void Parameter_TwoSpellingsOfOneName_ReadOneVariable()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-SPELLING", "7");
        try
        {
            foreach (string spelling in (string[])["cn-cc-test-spelling", "Cn-Cc-Test-Spelling", "CN-CC-TEST-SPELLING"])
            {
                var (text, diags) = Run($">>DEFINE {spelling} AS PARAMETER\n>>IF {spelling} = 7\nKEEP\n>>END-IF\n");
                Assert.False(Has1618(diags));
                Assert.Contains("KEEP", text);   // defined from the ONE variable, whichever way the name is written
            }
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-SPELLING", null); }
    }

    [Theory] // DOC-A.1-49 (kb/Work PB1533, PB1626): the value's CATEGORY is SYNTACTIC, as GnuCOBOL 3.2 classifies it — NUMERIC
             // exactly when the text is the spelling of a fixed-point numeric literal (§8.3.3.3.2: an optional leading sign,
             // digits, at most one decimal point that is not the rightmost character), a quoted value is that ONE literal,
             // and every other text is ALPHANUMERIC exactly as given. §7.3.8.2 SR1 a): a relation's operands are of the same
             // category, so the literal the relation compares against names the category the value has.
    [InlineData("7", "7")]
    [InlineData("-2.5", "-2.5")]
    [InlineData("+.5", "0.5")]
    [InlineData("007", "7")]
    [InlineData("abc", "\"abc\"")]
    [InlineData("7x", "\"7x\"")]
    [InlineData("1,000", "\"1,000\"")]   // the comma is no part of a numeric literal's spelling
    [InlineData("1,5", "\"1,5\"")]
    [InlineData("5.", "\"5.\"")]         // §8.3.3.3.2 3): the decimal point is never the rightmost character
    [InlineData("1E5", "\"1E5\"")]       // no floating-point spelling: the classification is a fixed-point literal's
    [InlineData("7 ", "\"7 \"")]         // trailing white space is content, not ignored
    [InlineData("\"ab\"\"c\"", "\"ab\"\"c\"")]   // a quoted value is the literal as written: the doubled quotation symbol is content
    [InlineData("'x y'", "\"x y\"")]
    [InlineData("\"7\"", "\"7\"")]       // quoted, so alphanumeric although its content spells a number
    public void Parameter_ValueCategory_FollowsTheDocumentedTyping(string value, string literal)
    {
        const string name = "CN-CC-TEST-TYPING";
        Environment.SetEnvironmentVariable(name, value);
        try
        {
            var (text, diags) = Run($">>DEFINE {name} AS PARAMETER\n>>IF {name} = {literal}\nKEEP\n>>END-IF\n");
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.Contains("KEEP", text);
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Theory] // §7.3.11.4 GR4 (kb/Work PB1626): "If no value is made available from the operating environment, compilation-variable-name-1
             // is not defined." GnuCOBOL 3.2 reads an empty value, one that begins with a space, and an improperly quoted one as
             // no value — here a quoted value is held to ONE literal, so a quoted value followed by more text is no value too.
    [InlineData("")]
    [InlineData(" 7")]
    [InlineData(" -2.5 ")]
    [InlineData("\"abc")]
    [InlineData("\"")]
    [InlineData("\"a\"b\"")]
    [InlineData("\"a\" & \"b\"")]
    [InlineData("'a\"")]
    public void Parameter_NoValue_LeavesTheNameUndefined(string value)
    {
        const string name = "CN-CC-TEST-NOVALUE";
        Environment.SetEnvironmentVariable(name, value);
        try
        {
            var (text, diags) = Run($">>DEFINE {name} AS PARAMETER\n>>IF {name} IS NOT DEFINED\nKEEP\n>>END-IF\n");
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.Contains("KEEP", text);
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Theory] // kb/Work PB1626 — the classification is the SAME at every edition and is not System.Decimal's: a 31-digit value (the
             // fixed-point literal capacity, §8.3.3.3.2) is NUMERIC, so it compares against the numeric literal it spells and is
             // the SAME value as that literal written in a DEFINE (§7.3.11.3 SR2 raises no COBOL1618). Before PB1626 a 29-31
             // digit value was alphanumeric and ">>IF X > 0" was refused (COBOL1619, 'Alphanumeric' vs 'Numeric').
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Parameter_ThirtyOneDigitValue_IsNumericAtEveryEdition(int edition)
    {
        const string name = "CN-CC-TEST-LONG";
        const string digits = "1234567890123456789012345678901";
        Environment.SetEnvironmentVariable(name, digits);
        try
        {
            var bag = new DiagnosticBag();
            string text = ConditionalCompilationProcessor.Process(
                $">>DEFINE {name} AS PARAMETER\n>>IF {name} > 0\nPOSITIVE\n>>END-IF\n>>IF {name} = {digits}\nEQUAL\n>>END-IF\n>>DEFINE {name} AS {digits}\n",
                diagnostics: bag, sourcePath: "t.cob", dialectLevel: edition);
            Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
            Assert.Contains("POSITIVE", text);
            Assert.Contains("EQUAL", text);
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact] // kb/Work PB1626 — a numeric spelling longer than the fixed-point literal capacity (§8.3.3.3.2: 1 through 31 digits) is
           // refused where the literal is evaluated, exactly as `>>DEFINE X AS <32 digits>` is: at the DEFINITION, naming the
           // directive, and the name stays undefined (GnuCOBOL reports it at the use; the diagnostic's position is the only difference).
    public void Parameter_ThirtyTwoDigitValue_RefusedAtTheDefinition()
    {
        const string name = "CN-CC-TEST-TOOLONG";
        Environment.SetEnvironmentVariable(name, "12345678901234567890123456789012");
        try
        {
            var (_, diags) = Run($">>DEFINE {name} AS PARAMETER\n");
            var d = Assert.Single(diags.Diagnostics, x => x.Code == "COBOL1619");
            Assert.Contains("32 digit positions", d.Message);
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact] // a directive in an omitted branch changes nothing, so the over-long value is never evaluated there
    public void Parameter_ThirtyTwoDigitValue_InAnOmittedBranch_IsNotEvaluated()
    {
        const string name = "CN-CC-TEST-TOOLONG2";
        Environment.SetEnvironmentVariable(name, "12345678901234567890123456789012");
        try
        {
            var (_, diags) = Run($">>IF 1 = 2\n>>DEFINE {name} AS PARAMETER\n>>END-IF\n");
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact] // kb/Work PB1367 — §7.3.11.3 SR2 reaches the PARAMETER alternative: without OVERRIDE, a name already defined
           // may be redefined only to the SAME value, and "no value from the environment" is not the same value.
    public void Parameter_NoValue_RedefiningADefinedName_WithoutOverride_Rejected1618()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PX", null);
        var (_, diags) = Run(">>DEFINE CN-CC-TEST-PX AS 1\n>>DEFINE CN-CC-TEST-PX AS PARAMETER\n");
        Assert.True(Has1618(diags));

        var (text, over) = Run(">>DEFINE CN-CC-TEST-PX AS 1\n>>DEFINE CN-CC-TEST-PX AS PARAMETER OVERRIDE\n>>IF CN-CC-TEST-PX DEFINED\nKEEP\n>>END-IF\n");
        Assert.False(Has1618(over));   // OVERRIDE is unconditional (GR3); no value ⇒ not defined (GR4)
        Assert.DoesNotContain("KEEP", text);
    }

    [Fact] // kb/Work PB1367 — the PARAMETER alternative with a DIFFERENT environment value is the same SR2 violation.
    public void Parameter_DifferentValue_RedefiningADefinedName_WithoutOverride_Rejected1618()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PY", "2");
        try
        {
            Assert.True(Has1618(Run(">>DEFINE CN-CC-TEST-PY AS 1\n>>DEFINE CN-CC-TEST-PY AS PARAMETER\n").Diags));
            Assert.False(Has1618(Run(">>DEFINE CN-CC-TEST-PY AS 2\n>>DEFINE CN-CC-TEST-PY AS PARAMETER\n").Diags));   // the SAME value
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-PY", null); }
    }

    private static bool Has1619(DiagnosticBag b) => b.Diagnostics.Any(d => d.Code == "COBOL1619");

    [Fact] // ⛔ Ledger C2 — the CLOSED DEFECT: a MULTI-TOKEN arithmetic operand now EVALUATES (§7.3.6) instead of
           // silently binding to its first token. >>DEFINE X AS 2 * 3 + 1 defines 7 (was 2), so >>IF X = 7 matches.
    public void Define_ArithmeticExpressionOperand_Evaluated()
    {
        var (text, _) = Run(">>DEFINE X AS 2 * 3 + 1\n>>IF X = 7\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.11.4 GR5 — a single numeric literal keeps its fractional value (not INTEGER-PART truncated).
    public void Define_SingleFractionLiteral_KeepsValue()
    {
        var (text, _) = Run(">>DEFINE Q AS 0.25\n>>IF Q = 0.25\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.7 — a boolean-EXPRESSION operand now EVALUATES: FLG = B"1100" B-OR B"0011" = B"1111".
    public void Define_BooleanExpressionOperand_Evaluated()
    {
        var (text, _) = Run(">>DEFINE FLG AS B\"1100\" B-OR B\"0011\"\n>>IF FLG = B\"1111\"\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.3 SR10 — a floating-point literal in a directive operand is a loud COBOL1619, not a value.
    public void Define_FloatLiteral_Rejected1619()
    {
        var (_, diags) = Run(">>DEFINE X AS 1.5E3\n");
        Assert.True(Has1619(diags));
    }

    [Fact] // §7.3.6.2 SR1c — a division by zero in a compile-time arithmetic operand is COBOL1619.
    public void Define_DivByZero_Rejected1619()
    {
        var (_, diags) = Run(">>DEFINE X AS 5 / 0\n");
        Assert.True(Has1619(diags));
    }

    [Fact] // §7.3.13.3 SR11 — an EVALUATE selection object of a different category than the subject is COBOL1619.
    public void Evaluate_ObjectCategoryMismatch_Rejected1619()
    {
        var (_, diags) = Run(">>EVALUATE 5\n>>WHEN \"A\"\nX\n>>END-EVALUATE\n");
        Assert.True(Has1619(diags));
    }

    /// <summary>&gt;&gt;PUSH / &gt;&gt;POP are recognized (consumed), not left as stray tokens.
    /// ⛔ The operand is REQUIRED: §7.3.22.2 and §7.3.20.2 print <c>{ directive-name | ALL }</c> in plain braces
    /// — verified against the printed page, folio 82 — so exactly one alternative shall be written, and
    /// §7.3.22.4 GR1/GR2 define an effect only for each of the two. This test wrote the BARE forms until
    /// kb/Work PB794 and passed, which made it a green test pinning an under-rejection
    /// (feedback_green_test_can_hold_a_gap_open); the bare form's own diagnostic is asserted below.</summary>
    [Fact]
    public void PushPop_Recognized_Consumed()
    {
        var (text, diags) = Run(">>PUSH ALL\nLINE-A\n>>POP ALL\n");
        Assert.False(diags.HasErrors);
        Assert.DoesNotContain(">>PUSH", text);
        Assert.DoesNotContain(">>POP", text);
        Assert.Contains("LINE-A", text);

        var (_, bare) = Run(">>PUSH\nLINE-A\n>>POP\n");
        Assert.Equal(2, bare.Diagnostics.Count(d => d.Code == "COBOL1911"));
    }

    private static string[] ErrorMessages(DiagnosticBag b) =>
        b.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message).ToArray();

    private const string Undefined = "is not a previously-defined";

    /// <summary>kb/Work PB3119 — a refused <c>&gt;&gt;DEFINE</c> is reported ONCE, at the directive. A later operand that
    /// names the variable fails silently in every arm that reads a name: a comparison operand (§7.3.8), an arithmetic
    /// operand (§7.3.6.2 SR1b) and a boolean operand (§7.3.7.2 SR1). The control is the same operand over a name nobody
    /// wrote, which still says it was never defined — the cascade is cut, the diagnostic is not.</summary>
    [Theory]
    [InlineData(">>DEFINE FOO AS 1 +\n")]          // operand that is no expression
    [InlineData(">>DEFINE FOO AS\n")]              // no operand (§7.3.11.2)
    [InlineData(">>DEFINE FOO AS 'A' OFF\n")]      // a word after the operand
    public void RefusedDefine_ReportsOnce_LaterUsesAreSilent(string define)
    {
        const string uses = ">>IF FOO = 2\n>>END-IF\n>>IF FOO + 1 = 2\n>>END-IF\n>>IF B\"1\" AND FOO\n>>END-IF\n";
        var (_, diags) = Run(define + uses);
        string[] errors = ErrorMessages(diags);
        Assert.Single(errors);
        Assert.Contains(">>DEFINE", errors[0]);

        var (_, control) = Run(define + uses.Replace("FOO", "BAR"));
        Assert.Equal(3, ErrorMessages(control).Count(m => m.Contains(Undefined) || m.Contains("'BAR'")));
    }

    [Fact] // a PARAMETER value that is a numeric literal but does not evaluate (past the digit capacity) is a refusal too
    public void RefusedDefine_ParameterLiteralThatDoesNotEvaluate_LaterUseIsSilent()
    {
        Environment.SetEnvironmentVariable("CN-PB3119-NUM", new string('9', 60));
        try
        {
            var (_, diags) = Run(">>DEFINE CN-PB3119-NUM AS PARAMETER\n>>IF CN-PB3119-NUM = 1\n>>END-IF\n");
            string[] errors = ErrorMessages(diags);
            Assert.Single(errors);
            Assert.DoesNotContain(Undefined, errors[0]);
        }
        finally { Environment.SetEnvironmentVariable("CN-PB3119-NUM", null); }
    }

    [Fact] // the refusal belongs to the LAST directive that wrote the name: a good redefinition ends it
    public void RefusedDefine_ThenGoodDefine_NameResolvesAgain()
    {
        var (text, diags) = Run(">>DEFINE FOO AS 1 +\n>>DEFINE FOO AS 2\n>>IF FOO = 2\nKEPT\n>>END-IF\n");
        Assert.Single(ErrorMessages(diags));
        Assert.Contains("KEPT", text);

        // …and a name that does resolve, to the WRONG category, is still a genuine complaint, not a silent one.
        var (_, wrong) = Run(">>DEFINE FOO AS 1 +\n>>DEFINE FOO AS 'A'\n>>IF FOO + 1 = 2\n>>END-IF\n");
        Assert.Equal(2, ErrorMessages(wrong).Length);
    }

    [Fact] // a DEFINE in an OMITTED branch has no effect: it refuses nothing, so a later use is a genuine undefined name
    public void RefusedDefine_InOmittedBranch_DoesNotSilenceLaterUse()
    {
        var (_, diags) = Run(">>IF 1 = 2\n>>DEFINE FOO AS 1 +\n>>END-IF\n>>IF FOO = 2\n>>END-IF\n");
        Assert.Contains(ErrorMessages(diags), m => m.Contains(Undefined));
    }

    [Fact] // PUSH/POP restore the refusals with the table: after POP the refused definition is gone, so FOO is undefined
    public void RefusedDefine_PopRestoresTheRefusals()
    {
        var (_, diags) = Run(">>PUSH ALL\n>>DEFINE FOO AS 1 +\n>>IF FOO = 2\n>>END-IF\n>>POP ALL\n>>IF FOO = 2\n>>END-IF\n");
        string[] errors = ErrorMessages(diags);
        Assert.Equal(2, errors.Length);   // the refusal, then the use AFTER the POP; the use inside the PUSH was silent
        Assert.Contains(Undefined, errors[1]);
    }

    /// <summary>kb/Work PB3429 — a diagnostic that quotes an operand quotes it AS WRITTEN (<c>FOO OF X</c>), never the
    /// tokens run together (<c>FOOOFX</c>), in every arm of the evaluator that quotes a multi-token operand.</summary>
    [Theory]
    [InlineData(">>IF FOO OF X + 1 = 2\n>>END-IF\n", "'FOO OF X'")]                       // arithmetic operand
    [InlineData(">>IF B\"1\" AND FOO OF X\n>>END-IF\n", "'FOO OF X'")]                    // boolean operand
    [InlineData(">>IF B\"1\" B-SHIFT-L FOO OF X\n>>END-IF\n", "'FOO OF X'")]              // boolean shift count
    public void QuotedOperand_KeepsItsWrittenSpacing(string directive, string quoted)
    {
        var (_, diags) = Run(directive);
        string[] errors = ErrorMessages(diags);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, m => m.Contains(quoted));
        Assert.DoesNotContain(errors, m => m.Contains("FOOOFX"));
    }

    [Theory] // every standard §7.3 directive is RECOGNIZED (consumed with its operand), never a stray token.
    [InlineData(">>DISPLAY \"hi\"")]
    [InlineData(">>FLAG-02 ON")]
    [InlineData(">>FLAG-14 OFF")]
    [InlineData(">>CALL-CONVENTION COBOL")]
    [InlineData(">>LEAP-SECOND ON")]
    [InlineData(">>LISTING ON")]
    public void StandardDirective_Recognized_Consumed(string directive)
    {
        var (text, _) = Run(directive + "\nLINE-B\n");
        Assert.DoesNotContain(">>", text);      // the whole directive line is consumed (operand and all)
        Assert.Contains("LINE-B", text);
    }
}
