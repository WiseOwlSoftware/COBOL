// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB971 — the METHOD arm of the *-ARG-OMITTED rule, which the goldens cannot reach: ISO §14.9.23.4 GR10
/// ("If a parameter for which the omitted-argument condition is true is referenced in an invoked method, except as
/// an argument or in the omitted-argument condition, the EC-OO-ARG-OMITTED exception condition is set to exist")
/// is Table 13 Fatal, and a method's own DECLARATIVES are not implemented yet (COBOLNET0899), so the observable is
/// §14.6.13.1.3 item 7 — checking enabled, no applicable USE in the source unit: abnormal run-unit termination,
/// naming EC-OO-ARG-OMITTED. The program and function arms are the goldens
/// <c>2002/pb971_program_arg_omitted</c> and <c>2002/pb971_function_arg_omitted</c>.
/// </summary>
public sealed class ArgOmittedConditionTests
{
    private static string Method(string id, string turn, string body) => $$"""
        {{turn}}
               IDENTIFICATION DIVISION.
               CLASS-ID. C{{id}}.
               OBJECT.
               PROCEDURE DIVISION.
               METHOD-ID. SHOW.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC X(4).
               01 G.
                  05 G1 PIC X(2).
                  05 G2 PIC 9(2).
               PROCEDURE DIVISION USING OPTIONAL X OPTIONAL G.
        {{body}}
               END METHOD SHOW.
               END OBJECT.
               END CLASS C{{id}}.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. P{{id}}.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   CLASS C{{id}}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 B USAGE OBJECT REFERENCE C{{id}}.
               PROCEDURE DIVISION.
                   INVOKE C{{id}} "NEW" RETURNING B
                   INVOKE B "SHOW" USING OMITTED OMITTED
                   DISPLAY "AFTER SHOW"
                   STOP RUN.
               END PROGRAM P{{id}}.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. F{{id}}.
               DATA DIVISION.
               LINKAGE SECTION.
               01 FX PIC X(4).
               PROCEDURE DIVISION USING OPTIONAL FX.
                   IF FX IS OMITTED DISPLAY "F-OMITTED" END-IF
                   GOBACK.
               END PROGRAM F{{id}}.
        """;

    private const string Turn = "      >>TURN EC-OO-ARG-OMITTED CHECKING ON";

    /// <summary>The two exemptions raise nothing — the omitted-argument condition and the formal forwarded as an
    /// ARGUMENT (§8.8.4.8.4 GR1c keeps it omitted in the callee) — and the first REAL reference is the fatal
    /// EC-OO-ARG-OMITTED: the statement is interrupted (no "X=" line) and the run unit ends (no "AFTER SHOW").</summary>
    [Fact]
    public void ElementaryReference_RaisesEcOoArgOmitted_AfterTheExemptions()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Method("971A", Turn, """
                   IF X IS OMITTED DISPLAY "X-OMITTED" END-IF
                   CALL "F971A" USING X
                   DISPLAY "X=[" X "]".
        """), 2002);
        Assert.False(ok, "a fatal condition with checking enabled and no USE ends the run unit (§14.6.13.1.3 #7)");
        string all = stdout + detail;
        Assert.Contains("X-OMITTED", all);
        Assert.Contains("F-OMITTED", all);
        Assert.Contains("EC-OO-ARG-OMITTED", all);
        Assert.DoesNotContain("EC-PROGRAM-ARG-OMITTED", all);
        Assert.DoesNotContain("X=[", all);
        Assert.DoesNotContain("AFTER SHOW", all);
    }

    /// <summary>A subordinate of an omitted GROUP formal names the formal's storage: the store raises too (the
    /// method's group formal is a copy-in local, which is exactly where the pre-PB971 carrier raise never reached).</summary>
    [Fact]
    public void GroupSubordinateStore_RaisesEcOoArgOmitted()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Method("971B", Turn, """
                   DISPLAY "IN SHOW"
                   MOVE "QQ" TO G1
                   DISPLAY "STORED".
        """), 2002);
        Assert.False(ok);
        string all = stdout + detail;
        Assert.Contains("IN SHOW", all);
        Assert.Contains("EC-OO-ARG-OMITTED", all);
        Assert.DoesNotContain("STORED", all);
    }

    /// <summary>Checking OFF (§14.6.13.1.1): no condition is raised; the omitted formal reads its initial state and
    /// execution continues — GR10 leaves the content undefined, and this is the documented implementor choice.</summary>
    [Fact]
    public void CheckingOff_IsLenient()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Method("971C", "", """
                   DISPLAY "X=[" X "]"
                   MOVE "QQ" TO G1.
        """), 2002);
        Assert.True(ok, detail);
        Assert.Equal("X=[    ]\nAFTER SHOW", stdout.Replace("\r\n", "\n").TrimEnd());
    }
}
