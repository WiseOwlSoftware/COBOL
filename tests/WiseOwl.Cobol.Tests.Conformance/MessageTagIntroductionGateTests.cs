// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ USAGE MESSAGE-TAG ANSWERS TO ONE ROW PER EDITION (kb/Work PB916): below COBOL-2023 the COBOL0900 introduction
/// gate (<c>usage-message-tag-2023</c>) is the whole edition answer; at 2023 the usage binds (kb/Work PB4740 retired
/// the Annex A.3 decline COBOL1943). The conformance harness's refusal files check that a code is REPORTED, not that
/// another is NOT, so the absence of the gate at 2023 is asserted here, over BOTH spellings of the clause (§13.18.60.2
/// prints <c>[ USAGE IS ]</c> as optional) and over the usage a group hands down (§13.18.60.4 GR1).
/// </summary>
public sealed class MessageTagIntroductionGateTests
{
    private const string Written = "01 M USAGE MESSAGE-TAG.";
    private const string Bare = "01 M MESSAGE-TAG.";
    private const string Group = "01 G USAGE MESSAGE-TAG.\n   05 M PIC X.";

    private static IReadOnlyList<string> ErrorsAt(string entry, int edition) =>
        EditionHarness.CompileFull($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTIG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {entry}
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """, edition).Errors;

    [Theory]
    [InlineData(Written, 85)]
    [InlineData(Written, 2002)]
    [InlineData(Written, 2014)]
    [InlineData(Bare, 85)]
    [InlineData(Bare, 2002)]
    [InlineData(Bare, 2014)]
    [InlineData(Group, 85)]
    [InlineData(Group, 2002)]
    [InlineData(Group, 2014)]
    public void BelowTheIntroduction_TheEntryDrawsTheIntroductionGate(string entry, int edition)
    {
        var errors = ErrorsAt(entry, edition);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
        EditionHarness.AssertHasDiagnostic(errors, "USAGE MESSAGE-TAG requires COBOL-2023");
        EditionHarness.AssertHasDiagnostic(errors, $"targeting COBOL-{edition}");
    }

    [Theory]
    [InlineData(Written)]
    [InlineData(Bare)]
    public void AtTheIntroduction_TheEntryBindsAndNeedsNoIntroductionGate(string entry)
    {
        var errors = ErrorsAt(entry, 2023);
        Assert.Empty(errors);
    }

    /// <summary>A group's message-tag usage is handed to its elementary items (§13.18.60.4 GR1), and an item that is not
    /// at level 1 or under a STRONG type declaration breaks §13.18.60.3 SR14 at every edition that has the usage.</summary>
    [Fact]
    public void AtTheIntroduction_AGroupsMessageTagUsageIsRefusedBySr14()
    {
        var errors = ErrorsAt(Group, 2023);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1724");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
    }

    /// <summary>The STATEMENT half (SET Format 17) follows its operand's usage: it binds at 2023 and, below it, the operands'
    /// own COBOL0900 is the answer.</summary>
    [Theory]
    [InlineData(2002, false)]
    [InlineData(2014, false)]
    [InlineData(2023, true)]
    public void SetFormat17_BindsOnlyWhereTheUsageExists(int edition, bool binds)
    {
        var errors = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTIS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MT1 USAGE MESSAGE-TAG.
            01 MT2 USAGE MESSAGE-TAG.
            PROCEDURE DIVISION.
            MAIN-P.
                SET MT1 TO MT2.
                STOP RUN.
            """, edition).Errors;
        if (binds) Assert.Empty(errors);
        else EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
    }

    /// <summary>kb/Work PB4771: below 2023 a SET Format 17 the SR35 rules refuse (a numeric receiver, a numeric sender, two
    /// receivers, a literal sender) draws ONLY the operands' COBOL0900 gate, because the usage the class refusals read
    /// does not exist there (DESIGN-message-control.md §7.1: "no second one may appear"); at 2023 each draws its
    /// COBOL0869 and no gate. The harness's refusal files check a code is reported, not that another is not.</summary>
    [Theory]
    [InlineData("SET N TO T", 85)]
    [InlineData("SET N TO T", 2002)]
    [InlineData("SET N TO T", 2014)]
    [InlineData("SET T TO N", 2014)]
    [InlineData("SET T U TO NULL", 2014)]
    [InlineData("SET T TO 5", 2014)]
    [InlineData("SET N TO T", 2023)]
    [InlineData("SET T TO N", 2023)]
    [InlineData("SET T U TO NULL", 2023)]
    [InlineData("SET T TO 5", 2023)]
    public void SetFormat17Refusals_AreTheGateAloneBelowTheIntroduction(string statement, int edition)
    {
        var errors = EditionHarness.CompileFull($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTISR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE MESSAGE-TAG.
            01 U USAGE MESSAGE-TAG.
            01 N PIC 9(4).
            PROCEDURE DIVISION.
            MAIN-P.
                {statement}.
                STOP RUN.
            """, edition).Errors;
        if (edition >= 2023)
        {
            EditionHarness.AssertHasDiagnostic(errors, "COBOL0869");
            EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
        }
        else
        {
            EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
            EditionHarness.AssertNoDiagnostic(errors, "COBOL0869");
        }
    }

    /// <summary>kb/Work PB4771 review R1-1: a message-tag SENDER that is an IMPORTED function's returned temporary has no
    /// declaration in this compilation, so no COBOL0900 gate fires on it; the SET must still be refused (§14.9.39.3 SR35,
    /// COBOL0869) at every edition, never dropped as a no-op that compiles clean below 2023.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void SetFormat17_ImportedFunctionSender_IsRefusedAtEveryEdition(int edition)
    {
        string dir = ScratchDirectory.Create("MtImported");
        try
        {
            string lib = CompiledProgramCache.StageSource(Path.Combine(dir, "MTF.cob"), """
                IDENTIFICATION DIVISION.
                FUNCTION-ID. MTF.
                DATA DIVISION.
                LINKAGE SECTION.
                01 R USAGE MESSAGE-TAG.
                PROCEDURE DIVISION RETURNING R.
                MAIN-P.
                    SET R TO NULL.
                    GOBACK.
                END FUNCTION MTF.
                """);
            Assert.True(CompiledProgramCache.Compile(new CompilerDriver.Options(lib, Path.Combine(dir, "MTF.dll"),
                DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free)).Success);
            string caller = CompiledProgramCache.StageSource(Path.Combine(dir, "MTFCALL.cob"), """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. MTFCALL.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    FUNCTION MTF.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 N PIC 9(4) VALUE 7.
                PROCEDURE DIVISION.
                MAIN-P.
                    SET N TO FUNCTION MTF().
                    STOP RUN.
                """);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(caller, Path.Combine(dir, "MTFCALL.dll"),
                DialectLevel: edition, SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [dir]));
            Assert.False(r.Success);
            EditionHarness.AssertHasDiagnostic(r.Errors, "COBOL0869");
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>kb/Work PB4771 review R2-1: an OBJECT PROPERTY of an imported class that is a message-tag item is a
    /// compiler temporary with no data description entry here, so no COBOL0900 gate fires on it. The SET naming it is
    /// bound at every edition: SR35 refuses a non-tag partner (COBOL0869), and <c>SET prop TO NULL</c> compiles as at 2023
    /// instead of being dropped as a no-op.</summary>
    [Theory]
    [InlineData("SET N TO TAGP OF O", 2002, false)]
    [InlineData("SET N TO TAGP OF O", 2014, false)]
    [InlineData("SET N TO TAGP OF O", 2023, false)]
    [InlineData("SET TAGP OF O TO N", 2002, false)]
    [InlineData("SET TAGP OF O TO N", 2014, false)]
    [InlineData("SET TAGP OF O TO N", 2023, false)]
    [InlineData("SET TAGP OF O TO NULL", 2002, true)]
    [InlineData("SET TAGP OF O TO NULL", 2014, true)]
    [InlineData("SET TAGP OF O TO NULL", 2023, true)]
    public void SetFormat17_ImportedPropertyOperand_IsBoundAtEveryEdition(string statement, int edition, bool succeeds)
    {
        string dir = ScratchDirectory.Create("MtProp");
        try
        {
            string lib = CompiledProgramCache.StageSource(Path.Combine(dir, "MTP.cob"), """
                IDENTIFICATION DIVISION.
                CLASS-ID. MTP.
                OBJECT.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 TAGP USAGE MESSAGE-TAG PROPERTY.
                END OBJECT.
                END CLASS MTP.
                """);
            Assert.True(CompiledProgramCache.Compile(new CompilerDriver.Options(lib, Path.Combine(dir, "MTP.dll"),
                DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free)).Success);
            string caller = CompiledProgramCache.StageSource(Path.Combine(dir, "MTPCALL.cob"), $"""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. MTPCALL.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    CLASS MTP PROPERTY TAGP.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 O USAGE OBJECT REFERENCE MTP.
                01 N PIC 9(4) VALUE 7.
                PROCEDURE DIVISION.
                MAIN-P.
                    {statement}.
                    STOP RUN.
                """);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(caller, Path.Combine(dir, "MTPCALL.dll"),
                DialectLevel: edition, SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [dir]));
            Assert.Equal(succeeds, r.Success);
            if (!succeeds) EditionHarness.AssertHasDiagnostic(r.Errors, "COBOL0869");
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The note's arm 2 from the other side: a group's message-tag usage handed to a leaf that wrote a
    /// PICTURE is refused as a MESSAGE-TAG usage at every edition, and never as a floating-point one.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AnInheritedMessageTagUsageWithAPicture_IsNeverReportedAsFloatingPoint(int edition)
    {
        var errors = ErrorsAt(Group, edition);
        EditionHarness.AssertHasDiagnostic(errors, "PICTURE may not be specified with USAGE MESSAGE-TAG");
        EditionHarness.AssertNoDiagnostic(errors, "floating-point usage");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1521");
    }
}
