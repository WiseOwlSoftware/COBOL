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
/// another is NOT, so the absence of the gate at 2023 is asserted here, over BOTH spellings of the clause (§2023:13.18.60.2
/// prints <c>[ USAGE IS ]</c> as optional) and over the usage a group hands down (§2023:13.18.60.4 GR1).
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

    /// <summary>A group's message-tag usage is handed to its elementary items (§2023:13.18.60.4 GR1), and an item that is not
    /// at level 1 or under a STRONG type declaration breaks §2023:13.18.60.3 SR14 at every edition that has the usage.</summary>
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

    /// <summary>kb/Work PB4836: the statement is the gate's whatever SET format its message-tag operand reaches, not Format 17
    /// alone. Below 2023 each of these draws ONLY the operand's COBOL0900 (DESIGN-message-control.md §7.1); at 2023 each is
    /// refused by its own format's rule and the gate is absent. The pointer (Format 7), object-reference (Format 5), UP/DOWN
    /// (Format 10 / Format 2) and message-tag-receiver (Format 17) arms are each reached by a tag operand, as sender or as
    /// receiver.</summary>
    [Theory]
    [InlineData("SET P TO T", 2002)]
    [InlineData("SET P TO T", 2014)]
    [InlineData("SET O TO T", 2014)]
    [InlineData("SET T UP BY 1", 2014)]
    [InlineData("SET T DOWN BY 1", 2014)]
    [InlineData("SET T TO P", 2014)]
    [InlineData("SET T TO O", 2014)]
    [InlineData("SET P TO T", 2023)]
    [InlineData("SET O TO T", 2023)]
    [InlineData("SET T UP BY 1", 2023)]
    [InlineData("SET T DOWN BY 1", 2023)]
    [InlineData("SET T TO P", 2023)]
    [InlineData("SET T TO O", 2023)]
    public void EverySetFormatAMessageTagOperandReaches_IsTheGateAloneBelowTheIntroduction(string statement, int edition)
    {
        var errors = EditionHarness.CompileFull($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTISF.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE MESSAGE-TAG.
            01 P USAGE POINTER.
            01 O USAGE OBJECT REFERENCE.
            PROCEDURE DIVISION.
            MAIN-P.
                {statement}.
                STOP RUN.
            """, edition).Errors;
        if (edition >= 2023)
        {
            Assert.NotEmpty(errors);
            EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
        }
        else
        {
            EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
            Assert.All(errors, e => Assert.Contains("COBOL0900", e));   // the gate alone: no second message
        }
    }

    /// <summary>kb/Work PB4837: a SAME-FILE function's returned temporary, a SAME-FILE class's object property and a same-file
    /// method's inline result are message-tag items whose own entries THIS compilation walks, so the COBOL0900 gate refuses
    /// the compile below 2023 and the SET naming them draws no second message. At 2023 each draws its SR35 refusal and no
    /// gate. (The imported counterparts, which have no entry here and so no gate, are
    /// <see cref="SetFormat17_ImportedFunctionSender_IsRefusedAtEveryEdition"/> and
    /// <see cref="SetFormat17_ImportedPropertyOperand_IsBoundAtEveryEdition"/>.)</summary>
    [Theory]
    [InlineData("SET N TO FUNCTION MTF()", 2014)]
    [InlineData("SET P TO FUNCTION MTF()", 2014)]
    [InlineData("SET N TO TAGP OF O", 2014)]
    [InlineData("SET TAGP OF O TO N", 2014)]
    [InlineData("SET N TO O :: \"MGET\" (N)", 2014)]
    [InlineData("SET N TO FUNCTION MTF()", 2023)]
    [InlineData("SET P TO FUNCTION MTF()", 2023)]
    [InlineData("SET N TO TAGP OF O", 2023)]
    [InlineData("SET TAGP OF O TO N", 2023)]
    [InlineData("SET N TO O :: \"MGET\" (N)", 2023)]
    public void SameFileFunctionPropertyAndMethodOperands_AreTheGateAloneBelowTheIntroduction(string statement, int edition)
    {
        var errors = EditionHarness.CompileFull($"""
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

            IDENTIFICATION DIVISION.
            CLASS-ID. MTP.
            OBJECT.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TAGP USAGE MESSAGE-TAG PROPERTY.
            PROCEDURE DIVISION.
            METHOD-ID. MGET.
            DATA DIVISION.
            LINKAGE SECTION.
            01 MA PIC 9(4).
            01 MR USAGE MESSAGE-TAG.
            PROCEDURE DIVISION USING MA RETURNING MR.
                SET MR TO NULL.
                GOBACK.
            END METHOD MGET.
            END OBJECT.
            END CLASS MTP.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTISAME.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION MTF
                CLASS MTP PROPERTY TAGP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC 9(4) VALUE 7.
            01 P USAGE POINTER.
            01 O USAGE OBJECT REFERENCE MTP.
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
            Assert.All(errors, e => Assert.Contains("COBOL0900", e));   // the gate alone: no COBOL0869 beside it
        }
    }

    /// <summary>kb/Work PB4771 review R1-1: a message-tag SENDER that is an IMPORTED function's returned temporary has no
    /// declaration in this compilation, so no COBOL0900 gate fires on it; the SET must still be refused (§2023:14.9.39.3 SR35,
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

    /// <summary>kb/Work PB4868: the gate is the whole answer for a statement of ANY verb that names a gated message tag, a same-file function
    /// returning one, or a statement nested in a statement that does, and not only for SET. The statement is asked once, at the funnel every
    /// statement passes through (<c>StatementBinder.NamesGatedMessageTag</c>), over its own operands: a statement NESTED in one that names the
    /// tag is bound on its own, so an unrelated refusal inside an IF whose condition names no tag still speaks. At 2023 the same statements are
    /// bound, so the class refusals the gate suppressed speak and no COBOL0900 does.</summary>
    [Theory]
    [InlineData("MOVE T TO N", 2014, true)]
    [InlineData("MOVE N TO T", 2014, true)]
    [InlineData("DISPLAY T", 2014, true)]
    [InlineData("ADD 1 TO T", 2014, true)]
    [InlineData("MOVE FUNCTION MTF() TO N", 2014, true)]
    [InlineData("DISPLAY FUNCTION MTF()", 2014, true)]
    [InlineData("SET N UP BY FUNCTION MTF()", 2014, true)]
    [InlineData("SET N DOWN BY FUNCTION MTF()", 2014, true)]
    [InlineData("SET ADDRESS OF L TO FUNCTION MTF()", 2014, true)]
    [InlineData("IF N = 7 MOVE T TO N END-IF", 2014, true)]
    [InlineData("MOVE T TO N", 2002, true)]
    [InlineData("DISPLAY T", 85, true)]
    [InlineData("MOVE T TO N", 2023, true)]
    [InlineData("MOVE N TO T", 2023, true)]
    [InlineData("DISPLAY T", 2023, true)]
    [InlineData("ADD 1 TO T", 2023, true)]
    [InlineData("MOVE FUNCTION MTF() TO N", 2023, true)]
    [InlineData("DISPLAY FUNCTION MTF()", 2023, true)]
    public void EveryVerbAMessageTagOperandReaches_IsTheGateAloneBelowTheIntroduction(string statement, int edition, bool refusedWhereTheUsageExists)
    {
        // The function and its REPOSITORY entry exist only where the statement names it: a function-id is itself an introduction below 2002.
        bool usesFunction = statement.Contains("FUNCTION", StringComparison.Ordinal);
        string function = usesFunction ? """
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

            """ : "";
        string repository = usesFunction ? """
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION MTF.
            """ : "";
        var errors = EditionHarness.CompileFull($"""
            {function}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTIVERB.
            {repository}
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE MESSAGE-TAG.
            01 N PIC 9(4) VALUE 7.
            LINKAGE SECTION.
            01 L PIC X.
            PROCEDURE DIVISION.
            MAIN-P.
                {statement}.
                STOP RUN.
            """, edition).Errors;
        if (edition >= 2023)
        {
            if (refusedWhereTheUsageExists) Assert.NotEmpty(errors);
            EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
        }
        else
        {
            EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
            Assert.All(errors, e => Assert.Contains("COBOL0900", e));   // the gate alone: no class diagnostic beside it
        }
    }

    /// <summary>kb/Work PB4868: a statement nested in one that names a gated tag is asked on its own, so a refusal that has nothing to do with the
    /// tag (here a MOVE of a name nothing declares, inside an IF whose condition names none) is not lost because a LATER statement is the gate's.</summary>
    [Fact]
    public void ANestedStatementThatNamesNoTagKeepsItsOwnRefusalBelowTheIntroduction()
    {
        var errors = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MTINEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T USAGE MESSAGE-TAG.
            01 N PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
            MAIN-P.
                IF N = 7
                    MOVE UNDEFINED-NAME TO N
                END-IF.
                MOVE T TO N.
                STOP RUN.
            """, 2014).Errors;
        EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
        EditionHarness.AssertHasDiagnostic(errors, "UNDEFINED-NAME");
        Assert.DoesNotContain(errors, e => e.Contains("COBOL0809"));
    }

    /// <summary>kb/Work PB4893: a statement that ACTIVATES a same-file method or function whose signature has a message-tag item — its
    /// RETURNING item, which the operand's temporary takes the description of (§2023:8.4.3.4.4 GR1b, §2023:8.4.3.2.4 GR1), or a formal its
    /// argument conforms to (§2023:14.8.2) — is the gate's below 2023, through EVERY receiver the invocation can name: a typed data item, a
    /// FACTORY OF reference, a class-name, an interface-typed reference, SELF, SUPER, an object view of SELF or of a universal reference,
    /// a chained segment, and the INVOKE statement with or without RETURNING. The funnel's diagnostic-free probe
    /// (<c>OoBinder.InvokedMethodsOf</c> / <c>InvokedMethodOf</c>) selects the method as the binding does, so at 2023 the same statements
    /// draw the class refusal the gate suppressed (COBOL0869, COBOL0809, COBOL0828, COBOL2470) and no COBOL0900. A statement in the
    /// method of the class (<c>inMethod</c>) is written in MTQ's method MUSE, whose SUPER is MTP.</summary>
    [Theory]
    [InlineData("SET N TO SELF :: \"MGET\" (N)", true, "COBOL0869")]
    [InlineData("MOVE SELF :: \"MGET\" (N) TO N", true, "COBOL0809")]
    [InlineData("MOVE SUPER :: \"MGET\" (N) TO N", true, "COBOL0809")]
    [InlineData("MOVE SELF AS MTP :: \"MGET\" (N) TO N", true, "COBOL0809")]
    [InlineData("INVOKE SELF \"MGET\" USING N RETURNING N", true, "COBOL0828")]
    [InlineData("INVOKE SUPER \"MGET\" USING N RETURNING N", true, "COBOL0828")]
    [InlineData("SET N TO U AS MTP :: \"MGET\" (N)", false, "COBOL0869")]
    [InlineData("MOVE MTP :: \"FGET\" TO N", false, "COBOL0809")]
    [InlineData("MOVE I :: \"MGET\" (N) TO N", false, "COBOL0809")]
    [InlineData("MOVE O :: \"ME\" :: \"MGET\" (N) TO N", false, "COBOL0809")]
    [InlineData("INVOKE O \"MGET\" USING N RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE U AS MTP \"MGET\" USING N RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE MTP \"FGET\" RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE F \"FGET\" RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE I \"MGET\" USING N RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE O :: \"ME\" \"MGET\" USING N RETURNING N", false, "COBOL0828")]
    [InlineData("INVOKE O \"MSET\" USING N", false, "COBOL0828")]
    [InlineData("MOVE O :: \"MSET\" (N) TO N", false, "COBOL0828")]
    [InlineData("MOVE FUNCTION MTF2(N) TO N", false, "COBOL2470")]
    public void AStatementActivatingASignatureWithATag_IsTheGateAloneBelowTheIntroduction(string statement, bool inMethod, string refusalAt2023)
    {
        foreach (int edition in (ReadOnlySpan<int>)[2014, 2023])
        {
            var errors = EditionHarness.CompileFull($"""
                IDENTIFICATION DIVISION.
                FUNCTION-ID. MTF2.
                DATA DIVISION.
                LINKAGE SECTION.
                01 A USAGE MESSAGE-TAG.
                01 R PIC 9(4).
                PROCEDURE DIVISION USING A RETURNING R.
                    MOVE 1 TO R.
                    GOBACK.
                END FUNCTION MTF2.

                IDENTIFICATION DIVISION.
                INTERFACE-ID. MTI.
                PROCEDURE DIVISION.
                METHOD-ID. MGET.
                DATA DIVISION.
                LINKAGE SECTION.
                01 MA PIC 9(4).
                01 MR USAGE MESSAGE-TAG.
                PROCEDURE DIVISION USING MA RETURNING MR.
                END METHOD MGET.
                END INTERFACE MTI.

                IDENTIFICATION DIVISION.
                CLASS-ID. MTP.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    INTERFACE MTI.
                FACTORY.
                PROCEDURE DIVISION.
                METHOD-ID. FGET.
                DATA DIVISION.
                LINKAGE SECTION.
                01 FR USAGE MESSAGE-TAG.
                PROCEDURE DIVISION RETURNING FR.
                    SET FR TO NULL.
                    GOBACK.
                END METHOD FGET.
                END FACTORY.
                OBJECT. IMPLEMENTS MTI.
                PROCEDURE DIVISION.
                METHOD-ID. MGET.
                DATA DIVISION.
                LINKAGE SECTION.
                01 MA PIC 9(4).
                01 MR USAGE MESSAGE-TAG.
                PROCEDURE DIVISION USING MA RETURNING MR.
                    SET MR TO NULL.
                    GOBACK.
                END METHOD MGET.
                METHOD-ID. MSET.
                DATA DIVISION.
                LINKAGE SECTION.
                01 MS USAGE MESSAGE-TAG.
                PROCEDURE DIVISION USING MS.
                    GOBACK.
                END METHOD MSET.
                METHOD-ID. ME.
                DATA DIVISION.
                LINKAGE SECTION.
                01 MO USAGE OBJECT REFERENCE MTP.
                PROCEDURE DIVISION RETURNING MO.
                    SET MO TO SELF.
                    GOBACK.
                END METHOD ME.
                END OBJECT.
                END CLASS MTP.

                IDENTIFICATION DIVISION.
                CLASS-ID. MTQ INHERITS MTP.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    CLASS MTP.
                OBJECT.
                PROCEDURE DIVISION.
                METHOD-ID. MUSE.
                DATA DIVISION.
                LOCAL-STORAGE SECTION.
                01 N PIC 9(4) VALUE 7.
                PROCEDURE DIVISION.
                    {(inMethod ? statement : "CONTINUE")}.
                    GOBACK.
                END METHOD MUSE.
                END OBJECT.
                END CLASS MTQ.

                IDENTIFICATION DIVISION.
                PROGRAM-ID. MTISIG.
                ENVIRONMENT DIVISION.
                CONFIGURATION SECTION.
                REPOSITORY.
                    FUNCTION MTF2
                    CLASS MTP
                    INTERFACE MTI.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 N PIC 9(4) VALUE 7.
                01 O USAGE OBJECT REFERENCE MTP.
                01 U USAGE OBJECT REFERENCE.
                01 I USAGE OBJECT REFERENCE MTI.
                01 F USAGE OBJECT REFERENCE FACTORY OF MTP.
                PROCEDURE DIVISION.
                MAIN-P.
                    {(inMethod ? "CONTINUE" : statement)}.
                    STOP RUN.
                """, edition).Errors;
            if (edition >= 2023)
            {
                EditionHarness.AssertHasDiagnostic(errors, refusalAt2023);
                EditionHarness.AssertNoDiagnostic(errors, "COBOL0900");
            }
            else
            {
                EditionHarness.AssertHasDiagnostic(errors, "COBOL0900");
                Assert.All(errors, e => Assert.Contains("COBOL0900", e));   // the gate alone: no class diagnostic beside it
            }
        }
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
