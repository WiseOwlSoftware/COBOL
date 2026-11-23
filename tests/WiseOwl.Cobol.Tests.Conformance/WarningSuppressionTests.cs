// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// <c>--suppress</c>, end to end (kb/Work PB2652; owner decision D20). ISO §2023:4.2.2 makes the warning mechanism one "that optionally
/// may be invoked by the user at compile time", and D20 settles that COBOL2187 (a TURN directive inside an exception-checking PERFORM,
/// §2023:7.3.25.3 SR5) is a SUPPRESSIBLE warning. Each leg can fail on its own: the warning is reported without the option; a name
/// mutes it whether the name is its code or its descriptor's id, in the printed lines and the listing's records alike; a binder warning
/// and a front-end warning are each muted at their own sink; a name mutes only what it names; and an error is never muted.
/// </summary>
public sealed class WarningSuppressionTests
{
    /// <summary>The PB595 program: a >>TURN directive written inside imperative-statement-1 of a Format-3 PERFORM.</summary>
    private const string TurnInsidePerform =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB2652A.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 WS-D PIC X(3) VALUE SPACES.\n       PROCEDURE DIVISION.\n       MAIN-P.\n           PERFORM\n"
        + "       >>TURN EC-OVERFLOW-STRING CHECKING ON\n"
        + "               STRING \"ABCDEFG\" DELIMITED BY SIZE INTO WS-D\n           WHEN EC-OVERFLOW-STRING\n"
        + "               DISPLAY \"L1=WHEN\"\n           END-PERFORM.\n           STOP RUN.\n";

    private const string TurnCode = "COBOL2187";

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, IReadOnlyList<Diagnostic> Records) Compile(
        string source, int edition, IReadOnlyList<string>? suppress, InitialReferenceFormat format = TestSourceFormat.Inline) =>
        EditionHarness.CompileStaged(source, edition, permissive: false, copybooks: null, flagExtensions: false,
            static (_, r) => (r.Success, r.Errors, r.Warnings, r.Diagnostics), format, suppress);

    [Fact]
    public void TheTurnWarning_IsReportedWithoutTheOption()
    {
        var (ok, errors, warnings, records) = Compile(TurnInsidePerform, 2023, suppress: null);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains(TurnCode, StringComparison.Ordinal));
        // The record carries the descriptor that reported it: the identity a name other than the code is matched against.
        Assert.Contains(records, d => d.Code == TurnCode && d.Descriptor?.Id == "directive-in-exception-checking-perform");
    }

    [Theory]
    [InlineData(TurnCode)]
    [InlineData("cobol2187")]                                     // a name is matched without regard to case
    [InlineData("directive-in-exception-checking-perform")]       // the descriptor's stable id
    public void TheTurnWarning_IsMutedByItsCodeOrItsId_InTheLinesAndTheRecords(string name)
    {
        var (ok, errors, warnings, records) = Compile(TurnInsidePerform, 2023, [name]);
        Assert.True(ok, string.Join("\n", errors));
        Assert.DoesNotContain(warnings, w => w.Contains(TurnCode, StringComparison.Ordinal));
        Assert.DoesNotContain(records, d => d.Code == TurnCode);
    }

    /// <summary>A name mutes only what it names: suppressing another warning's code leaves COBOL2187 reported.</summary>
    [Fact]
    public void AnotherName_LeavesTheTurnWarningReported()
    {
        var (ok, _, warnings, _) = Compile(TurnInsidePerform, 2023, ["COBOL0903"]);
        Assert.True(ok);
        Assert.Contains(warnings, w => w.Contains(TurnCode, StringComparison.Ordinal));
    }

    /// <summary>The front end's sink: the fixed-form continuation indicator is obsolete at 2023 (COBOL0903, a warning the
    /// preprocessor reports into the front end's bag), and suppressing its code mutes it there.</summary>
    [Fact]
    public void AFrontEndWarning_IsMutedAtTheFrontEndSink()
    {
        const string continued =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB2652B.\n       PROCEDURE DIVISION.\n"
            + "           DISPLAY \"ABC\n      -    \"DEF\".\n           STOP RUN.\n";
        var (ok, errors, warnings, _) = Compile(continued, 2023, suppress: null, InitialReferenceFormat.Fixed);
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOL0903", StringComparison.Ordinal));
        var (mutedOk, _, muted, records) = Compile(continued, 2023, ["COBOL0903"], InitialReferenceFormat.Fixed);
        Assert.True(mutedOk);
        Assert.DoesNotContain(muted, w => w.Contains("COBOL0903", StringComparison.Ordinal));
        Assert.DoesNotContain(records, d => d.Code == "COBOL0903");
    }

    /// <summary>⛔ An error is never muted: naming the code of the error that refuses the program leaves the compile failing with it.</summary>
    [Fact]
    public void AnError_IsNeverMuted()
    {
        const string undefined =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB2652C.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
            + "       01 X PIC X.\n       PROCEDURE DIVISION.\n           MOVE NOSUCH TO X.\n           STOP RUN.\n";
        var (ok, errors, _, _) = Compile(undefined, 2023, suppress: null);
        Assert.False(ok);
        string code = errors.Select(e => System.Text.RegularExpressions.Regex.Match(e, @"COBOL\d{4}")).First(m => m.Success).Value;
        var (stillOk, stillErrors, _, records) = Compile(undefined, 2023, [code]);
        Assert.False(stillOk);
        Assert.Contains(stillErrors, e => e.Contains(code, StringComparison.Ordinal));
        Assert.Contains(records, d => d.Code == code && d.IsError);
    }

    /// <summary>A name no diagnostic answers to is refused, never ignored: a misspelling that muted nothing would pass for a warning
    /// the compiler failed to mute.</summary>
    [Fact]
    public void AnUnknownName_IsRefused()
    {
        Assert.NotNull(WarningSuppression.Problem("COBOL21877"));
        Assert.Throws<ArgumentException>(() => WarningSuppression.Of(["no-such-diagnostic"]));
        Assert.Null(WarningSuppression.Problem("recognized-not-implemented"));   // a family key
    }
}
