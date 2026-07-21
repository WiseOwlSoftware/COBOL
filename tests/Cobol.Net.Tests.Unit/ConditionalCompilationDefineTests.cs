// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;DEFINE</c> directive (ISO §7.3.11) Wave-D behaviour in <see cref="ConditionalCompilationProcessor"/>:
/// the §7.3.11.3 SR2 no-OVERRIDE redefinition check (COBOLNET1618), the GR3 OVERRIDE phrase, and the GR4
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

    private static bool Has1618(DiagnosticBag b) => b.Diagnostics.Any(d => d.Code == "COBOLNET1618");

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

    [Fact] // GR4 — AS PARAMETER sources the value from the operating environment (numeric here).
    public void Parameter_SourcesFromEnvironment()
    {
        Environment.SetEnvironmentVariable("CN_CC_TEST_PARAM", "42");
        try
        {
            var (text, diags) = Run(">>DEFINE CN_CC_TEST_PARAM AS PARAMETER\n>>IF CN_CC_TEST_PARAM = 42\nKEEP\n>>END-IF\n");
            Assert.False(Has1618(diags));
            Assert.Contains("KEEP", text);   // the >>IF matched → the guarded line survives
        }
        finally { Environment.SetEnvironmentVariable("CN_CC_TEST_PARAM", null); }
    }

    [Fact] // GR4 — when the environment supplies no value, the variable is NOT defined.
    public void Parameter_Unavailable_NotDefined()
    {
        Environment.SetEnvironmentVariable("CN_CC_TEST_UNSET", null);
        var (text, _) = Run(">>DEFINE CN_CC_TEST_UNSET AS PARAMETER\n>>IF CN_CC_TEST_UNSET DEFINED\nKEEP\n>>END-IF\n");
        Assert.DoesNotContain("KEEP", text);   // undefined → the >>IF DEFINED is false → the line drops
    }

    [Fact] // >>PUSH / >>POP are recognized (consumed), not left as stray tokens.
    public void PushPop_Recognized_Consumed()
    {
        var (text, diags) = Run(">>PUSH\nLINE-A\n>>POP\n");
        Assert.False(diags.HasErrors);
        Assert.DoesNotContain(">>PUSH", text);
        Assert.DoesNotContain(">>POP", text);
        Assert.Contains("LINE-A", text);
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
