// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5702 review R1-1 — at --std 2002 the lexer writes a character 2002's Annex D does not fold but 2023's table does
/// (U+10A0, §2002:8.1.2.1 GR4c) as a private-use stand-in (<c>CobolNames.EditionSpelling</c>). The ASSIGN operand names a host
/// file (§2023:12.4.5.2 SR5, implementor-defined), so the emitted program must carry the text the user wrote, never a
/// stand-in: the name is read from the input (<c>CobolWordRule.WrittenText</c>), at every edition alike.
/// </summary>
public sealed class AssignNameWrittenTextTests
{
    private const string Program = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. ASGN.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO OUTႠF.
               DATA DIVISION.
               FILE SECTION.
               FD F.
               01 R PIC X.
               PROCEDURE DIVISION.
                   OPEN OUTPUT F
                   CLOSE F
                   STOP RUN.
        """;

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AssignWord_ReachesTheEmittedProgramAsWritten(int edition)
    {
        string dir = ScratchDirectory.Create("pb5702assign");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, Program.Replace("\\u10A0", "Ⴀ"));
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, EditionInfo.Of(edition), Path.Combine(dir, "p.dll"), CheckOnly: false, SourceFormat: TestSourceFormat.Inline));
            Assert.True(r.Success, "the program must compile: " + string.Join("; ", r.Errors));
            string cs = File.ReadAllText(r.GeneratedCsPath!);
            Assert.Contains("OUTႠF", cs);
            Assert.False(CobolNames.HasReservedSpelling(cs), "a stand-in character reached the emitted C#");
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
