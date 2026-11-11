// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The X3.23-1985 STOP literal statement communicates a keyword figurative constant as the value it represents
/// (kb/Work PB5074). §1985:VI.6.24.4 GR4: "If STOP literal-1 is specified, the execution of the run unit is suspended
/// and literal-1 is communicated to the operator", and a figurative constant represents a value, not its spelling
/// (§1985:IV.4.2.2.2.3: ZERO "Represents the numeric value '0', or one or more of the character '0' from the computer's
/// character set", SPACE "Represents one or more of the character space from the computer's character set"). STOP ZERO
/// used to communicate the word ZERO. The operator channel
/// is standard error (<c>StatementEmitter.Visit(BoundStopLiteral)</c>), which the conformance-suite <c>.out</c>
/// files do not read, so the witness is here. The run continues after each STOP literal (the statement suspends,
/// GR4), which the trailing DISPLAY proves.
/// </summary>
public sealed class StopLiteralFigurativeTests
{
    [Fact]
    public void AKeywordFigurative_IsCommunicatedAsItsValue()
    {
        const string source = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB5074SF.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       STOP ZERO.
                       STOP QUOTE.
                       STOP SPACE.
                       STOP "X".
                       DISPLAY "END".
                       STOP RUN.
            """;
        var (exit, stdout, stderr) = new WiseOwlCobolCompiler(85).CompileAndRunExit(source);
        Assert.True(exit == 0, stderr);
        Assert.Equal("END", stdout);
        // One operator line per STOP; the SPACE line is a single blank, which the harness's per-line trim empties.
        Assert.Equal(["0", "\"", "", "X"], stderr.Split('\n'));
    }
}
