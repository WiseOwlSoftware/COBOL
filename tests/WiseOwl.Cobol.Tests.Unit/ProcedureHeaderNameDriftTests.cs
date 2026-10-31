// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A PROCEDURE HEADER'S NAME IS READ BY ONE FUNCTION, AND THE NODE'S TEXT IS NEVER THAT FUNCTION (kb/Work PB4238).
/// The grammar's <c>paragraphName</c> and <c>sectionName</c> are <c>procedureName</c>, which is <c>word ((OF | IN)
/// word)?</c> because the references share it, so <c>P1 OF S1.</c> and <c>S1 OF X SECTION.</c> parse. A reader that
/// asked the header for <c>GetText()</c> got the words glued — <c>P1OFS1</c> — and declared a procedure no source
/// element wrote, which <c>PERFORM P1OFS1</c> then ran. ISO §14.2.1 prints the headers bare (<c>section-name-1
/// SECTION.</c>, <c>paragraph-name-1.</c>) and §14.4.1 gives qualification to the procedure-name that refers to a
/// paragraph, so <c>ProcedureTableBuilder.HeaderName</c> is the one place that reads a header's name and refuses a
/// qualifier (COBOL5716). A header slot read any other way — <c>.paragraphName().GetText()</c>, a child of it — is the
/// regression this test names; the goldens prove today's readers, this proves the next one goes through
/// <c>.procedureName()</c> and <c>ProcedureNameShape</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProcedureHeaderNameDriftTests
{
    private static readonly Regex HeaderSlotRead =
        new(@"\b(paragraphName|sectionName)\(\)(?!\s*\.\s*procedureName\(\))", RegexOptions.Compiled);

    [Fact]
    public void AHeaderSlotIsReadOnlyThroughItsProcedureName()
    {
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal) && HeaderSlotRead.IsMatch(lines[i]))
                    found.Add($"{Path.GetFileName(file)}:{i + 1}");
        }
        Assert.True(found.Count == 0,
            "A paragraph-name / section-name header slot is read without .procedureName() (" + string.Join(", ", found)
            + "): its GetText() glues a qualified header into one word (P1 OF S1 -> P1OFS1). Read the head word with "
            + "ProcedureNameShape.Head, through ProcedureTableBuilder.HeaderName where the header is declared (kb/Work PB4238).");
    }

    private const string QualifiedParagraphHeader = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4238A.
        PROCEDURE DIVISION.
        S1 SECTION.
        MAIN-P.
            STOP RUN.
        P1 OF S1.
            DISPLAY "X".
        """;

    private const string QualifiedSectionHeader = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4238B.
        PROCEDURE DIVISION.
        S0 SECTION.
        MAIN-P.
            STOP RUN.
        S1 IN X SECTION.
        P2.
            DISPLAY "X".
        """;

    private const string QualifiedNumericParagraphHeader = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4238C.
        PROCEDURE DIVISION.
        MAIN-P.
            STOP RUN.
        12 OF Q.
            DISPLAY "X".
        """;

    private const string QualifiedDeclarativeHeaders = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4238D.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D1 OF X SECTION.
            USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
        P9 OF D1.
            DISPLAY "X".
        END DECLARATIVES.
        S1 SECTION.
        MAIN-P.
            STOP RUN.
        """;

    private const string BareHeadersQualifiedReferences = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB4238E.
        PROCEDURE DIVISION.
        S0 SECTION.
        MAIN-P.
            PERFORM P1 OF S1.
            PERFORM P1 IN S2.
            STOP RUN.
        S1 SECTION.
        P1.
            DISPLAY "A".
        S2 SECTION.
        P1.
            DISPLAY "B".
        """;

    /// <summary>⛔ AT EVERY EDITION and in every header slot the grammar has: the paragraph header, the section header,
    /// an all-digits paragraph-name, and the two declarative-section arms.</summary>
    [Theory]
    [InlineData(QualifiedParagraphHeader, 1, 85)]
    [InlineData(QualifiedParagraphHeader, 1, 2023)]
    [InlineData(QualifiedSectionHeader, 1, 85)]
    [InlineData(QualifiedSectionHeader, 1, 2023)]
    [InlineData(QualifiedNumericParagraphHeader, 1, 2002)]
    [InlineData(QualifiedDeclarativeHeaders, 2, 85)]
    [InlineData(QualifiedDeclarativeHeaders, 2, 2014)]
    public void AQualifiedHeaderIsRefusedOncePerHeader(string source, int expected, int edition)
    {
        var errors = Compile(source, edition);
        Assert.Equal(expected, errors.Count(e => e.Contains("COBOL5716", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void BareHeadersWithQualifiedReferencesStayLegal(int edition) =>
        Assert.Empty(Compile(BareHeadersQualifiedReferences, edition));

    private static List<string> Compile(string source, int edition)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4238_");
        try
        {
            string src = Path.Combine(dir, $"hdr_{edition}.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return [.. r.Errors];
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
