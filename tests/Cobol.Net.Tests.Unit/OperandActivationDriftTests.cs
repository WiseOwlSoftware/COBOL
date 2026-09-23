// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB892 — <b>an activation written as an OPERAND has the statement it was written in, and one landing
/// gives it that statement.</b> ISO §14.9.18.4 GR1 b) raises a condition an activated element propagates "in the
/// activating runtime element if checking for that exception condition is enabled in the activating runtime
/// element", and §14.9.33.4 GR2 a) 2. makes the applicable statement of a RESUME AT NEXT STATEMENT for it, "for an
/// inline invocation or a function invocation, … the statement in which the inline invocation or function
/// invocation was specified".
///
/// <para>The defect these pins keep closed had two arms on one surface. A per-evaluation function reference (a
/// PERFORM UNTIL, a SEARCH WHEN, an EVALUATE object, a short-circuited operand) was rendered by a SECOND
/// activation text that emitted no propagation pickup, so the registry discarded every condition it
/// propagated; and a HOISTED one's pickup resumed by falling through, back INTO the statement GR2 says to leave
/// (a COMPUTE completed with the function's result). The repair is ONE mechanism: the activation is marked
/// <c>InExpression</c>, its pickup throws <c>RaiseResumeSignal</c>, and the carrying statement's
/// <c>BoundActivationSite</c> lands it.</para>
/// </summary>
public sealed class OperandActivationDriftTests
{
    private static string CodeGenFile(params string[] parts) =>
        File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "CodeGen", .. parts]));

    private static string MethodBody(string file, string signaturePrefix)
    {
        var m = Regex.Match(file, Regex.Escape(signaturePrefix) + @"[^\n]*\n    \{(?<b>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(m.Success, $"'{signaturePrefix}' not found");
        return m.Groups["b"].Value;
    }

    /// <summary>The activation-site RESUME landings in <c>CallEmitter</c> all go through the ONE helper that
    /// parts the two kinds of activation — a direct <c>dispatch.ResumeTransfer</c> in either method would land an
    /// operand activation's RESUME inside the statement it belongs to (or a <c>goto</c> inside a lambda).</summary>
    [Fact]
    public void ActivationSiteResumes_GoThroughTheOperandAwareHelper()
    {
        string call = CodeGenFile("Verbs", "CallEmitter.cs");
        foreach (var method in new[] { "public void EmitPropagationPickup(", "private void EmitCallEcCatch(" })
        {
            string body = MethodBody(call, method);
            Assert.DoesNotContain("dispatch.ResumeTransfer(", body);
            Assert.Contains("Resume(", body);
        }
    }

    /// <summary>A per-evaluation window's activation is the statement emitter's own output — there is no second
    /// activation text for expression position any more.</summary>
    [Fact]
    public void ConditionWindow_RendersActivationsThroughTheStatementEmitter()
    {
        string cond = CodeGenFile("Emit", "ConditionRenderer.cs");
        Assert.DoesNotContain("ProgramRegistry.CallProgram", cond);
        Assert.DoesNotContain("FunctionActivationText", CodeGenFile("Verbs", "CallEmitter.cs"));
        Assert.Matches(@"private string PreOpText\(BoundStatement s\) => ctx\.Writer\.CaptureText\(\(\) => Statements\.EmitStatement\(s\)\);", cond);
    }

    /// <summary>The emitted shape: a function referenced in a PERFORM UNTIL condition activates inside the
    /// condition's lambda WITH the site-handled pickup and the operand-activation throw, the PERFORM carries the
    /// landing, and no <c>goto</c> sits inside the lambda. A hoisted reference in a COMPUTE takes the same landing.
    /// </summary>
    [Fact]
    public void GeneratedCode_OperandActivationsPickUpAndLandAtTheirStatement()
    {
        string cs = Emit("""
                   >>TURN EC-USER-OAZ CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OADRIFT1.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   REPOSITORY.
                       FUNCTION OADRIFTF.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC 99 VALUE 5.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-OAZ.
                   HZ-P.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       PERFORM UNTIL FUNCTION OADRIFTF = 7
                           DISPLAY "BODY"
                       END-PERFORM.
                       COMPUTE X = FUNCTION OADRIFTF + 1.
                       STOP RUN.
                   END PROGRAM OADRIFT1.
                   IDENTIFICATION DIVISION.
                   FUNCTION-ID. OADRIFTF.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 R PIC 9.
                   PROCEDURE DIVISION RETURNING R RAISING EC-USER-OAZ.
                   F-P.
                       MOVE 7 TO R.
                       GOBACK RAISING EXCEPTION EC-USER-OAZ.
                   END FUNCTION OADRIFTF.
            """);
        var lambda = Regex.Match(cs, @"new Func<bool>\(\(\) => \{(?<b>.*?)return ", RegexOptions.Singleline);
        Assert.True(lambda.Success, cs);
        string body = lambda.Groups["b"].Value;
        Assert.Contains("ProgramRegistry.CallProgram(", body);
        Assert.Contains("ExceptionState.TakeRaisedPropagation(", body);
        Assert.Contains("throw new RaiseResumeSignal(", body);
        Assert.DoesNotContain("goto ", body);
        // Two landings — the PERFORM's and the COMPUTE's — and every operand pickup throws rather than falls through.
        Assert.Equal(2, Regex.Matches(cs, @"catch \(RaiseResumeSignal __as\d+\)").Count);
        Assert.Equal(2, Regex.Matches(cs, @"== ResumeSignal\.NextStatement\) throw new RaiseResumeSignal\(__pr\d+\);").Count);
    }

    /// <summary>The control: a statement with no operand activation binds no landing.</summary>
    [Fact]
    public void AStatementWithoutAnOperandActivation_BindsNoLanding()
    {
        string cs = Emit("""
                   >>TURN EC-USER-OAN CHECKING ON
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. OADRIFT2.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 X PIC 99 VALUE 5.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-OAN.
                   HZ-P.
                       RESUME AT NEXT STATEMENT.
                   END DECLARATIVES.
                   MAIN SECTION.
                   MAIN-P.
                       CALL "OADRIFT3".
                       COMPUTE X = X + 1.
                       STOP RUN.
            """);
        Assert.DoesNotContain("catch (RaiseResumeSignal __as", cs);
        Assert.DoesNotContain("throw new RaiseResumeSignal(__pr", cs);
    }

    private static string Emit(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"oadrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            // The >>TURN events ride frontend.Directives — without them every assertion passes for the wrong reason.
            return emitter.EmitBound(emitter.Bind(tree!, new EditionContext(2023), frontend.Directives));
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
