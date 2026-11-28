// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ A REFUSAL CITES THE RULE OF THE EDITION BEING COMPILED, NOT THE 2023 LOCATOR (kb/Work PB6072). Each program is refused at
/// every edition for the same reason, and the message names that edition's clause and syntax-rule number: the RENAMES operand and
/// window rules (§2002:13.16.43.2, §2014:13.18.45.2, §2023:13.18.45.3) and the RECORD clause's DEPENDING item that is a table element
/// (§2002:8.4.1.2.2 SR5, §2014:8.4.1.2.2 SR5, §2023:8.4.2.3.3 SR5).</summary>
public sealed class RefusalCitesItsEditionTests
{
    private const string Head =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB6072.\n";

    private const string Proc = "       PROCEDURE DIVISION.\n       MAIN.\n           STOP RUN.\n";

    /// <summary>RENAMES a level-01 record: <c>Neither data-name-2 nor data-name-3 shall refer to an entry that is described with
    /// level-number 1, 66, 77, or 88</c>, SR5 of the clause at each edition.</summary>
    [Theory]
    [InlineData(2002, "ISO §2002:13.16.43.2 SR5")]
    [InlineData(2014, "ISO §2014:13.18.45.2 SR5")]
    [InlineData(2023, "ISO §2023:13.18.45.3 SR5")]
    public void RenamesALevel1Entry_CitesTheEditionsSr5(int edition, string cite)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 REC.\n          05 A PIC X.\n"
            + "          66 AL RENAMES REC.\n" + Proc, edition);
        Assert.Contains(errors, e => e.Contains("COBOL") && e.Contains("(" + cite + ":"));
        Assert.DoesNotContain(edition < 2023 ? "§2023" : "§2002", string.Join("\n", errors));
    }

    /// <summary>RENAMES an item whose end does not follow data-name-2's end: the window rule is SR10 at 2002 (the clause has no
    /// CONSTANT RECORD rule), SR11 at 2014 and 2023.</summary>
    [Theory]
    [InlineData(2002, "ISO §2002:13.16.43.2 SR10")]
    [InlineData(2014, "ISO §2014:13.18.45.2 SR11")]
    [InlineData(2023, "ISO §2023:13.18.45.3 SR11")]
    public void RenamesWindowOrder_CitesTheEditionsRule(int edition, string cite)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 REC.\n          05 A PIC X.\n"
            + "          05 B PIC X.\n          66 AL RENAMES B THRU A.\n" + Proc, edition);
        Assert.Contains(errors, e => e.Contains("(" + cite + ":"));
        Assert.DoesNotContain(edition < 2023 ? "§2023" : "§2002", string.Join("\n", errors));
    }

    /// <summary>The RECORD clause's DEPENDING item is a table element: the unsubscripted table element reference is
    /// <c>Each table element reference shall be subscripted</c>, §2002:8.4.1.2.2 SR5, §2014:8.4.1.2.2 SR5, §2023:8.4.2.3.3 SR5.</summary>
    [Theory]
    [InlineData(2002, "ISO §2002:8.4.1.2.2 SR5")]
    [InlineData(2014, "ISO §2014:8.4.1.2.2 SR5")]
    [InlineData(2023, "ISO §2023:8.4.2.3.3 SR5")]
    public void RecordDependingOnATableElement_CitesTheEditionsTableElementRule(int edition, string cite)
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
            + "           SELECT F1 ASSIGN TO \"f.dat\".\n       DATA DIVISION.\n       FILE SECTION.\n"
            + "       FD F1 RECORD VARYING FROM 1 TO 10 DEPENDING ON LEN.\n       01 R1 PIC X(10).\n"
            + "       WORKING-STORAGE SECTION.\n       01 TBL.\n          05 LEN PIC 99 OCCURS 2.\n" + Proc, edition);
        Assert.Contains(errors, e => e.Contains("DEPENDING ON") && e.Contains("(" + cite + ")"));
        Assert.DoesNotContain(edition < 2023 ? "§2023" : "§2002", string.Join("\n", errors));
    }

    private static IReadOnlyList<string> Compile(string source, int edition)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB6072_");
        try
        {
            string src = Path.Combine(dir, "pb6072u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, EditionInfo.Of(edition), Path.Combine(dir, "pb6072u.dll"), CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.False(r.Success, "the source is non-conforming and must be rejected");
            return r.Errors;
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
