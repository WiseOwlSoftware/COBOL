// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The declaratives' STRUCTURAL syntax rules — ISO §2023:14.9.49.3 SR1 (placement of USE), SR3/SR4 (the reference
/// boundary between the declaratives portion and the rest), SR10/SR11 (the two restrictions on a USE BEFORE
/// REPORTING procedure). kb/Work PB361, PB362, PB363: each compiled clean at every <c>--std</c> before, the
/// misplaced USE then aborting the run unit as a "not implemented" feature.
///
/// <para>⛔ SR3 and SR4 are a TWO-ARM rule whose arms point in OPPOSITE directions (SR3 restricts references OUT of
/// a declarative, exempting RESUME; SR4 restricts references INTO a declarative section, exempting PERFORM), so
/// every arm is pinned from both sides, with the exempt verb as the control.</para>
/// </summary>
public sealed class DeclarativesStructuralRulesTests
{
    // ── SR1 — a USE statement anywhere but the first sentence of a declarative section (PB361) ────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void AUseStatementInANondeclarativeParagraph_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR1A", "", """
                   M1.
                       STOP RUN.
                   M2.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
            """), level);
        Assert.Contains(d, x => x.Contains("COBOL2377") && x.Contains("nondeclarative portion"));
        Assert.DoesNotContain(d, x => x.Contains("COBOL1756"));   // never again "not implemented"
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ASecondUseStatementInsideADeclarativeParagraph_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR1B", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
            """, """
                   M1.
                       STOP RUN.
            """), level);
        Assert.Single(d, x => x.Contains("COBOL2377") && x.Contains("inside a declarative section"));
    }

    // ── SR4 — INTO a declarative section, PERFORM only (PB362) ──────────────────────────────────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AGoToIntoADeclarativeFromTheNondeclarativePortion_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR4A", Decl1, """
                   M1.
                       GO TO DP1.
            """), level);
        Assert.Contains(d, x => x.Contains("COBOL2376") && x.Contains("nondeclarative portion"));
    }

    [Fact]
    public void AGoToIntoADifferentDeclarativeSection_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4B", Decl1 + Environment.NewLine + """
                   D2 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
                   DP2.
                       GO TO DP1.
            """, """
                   M1.
                       STOP RUN.
            """), 2023);
        Assert.Contains(d, x => x.Contains("COBOL2376") && x.Contains("different declarative section, 'D2'"));
    }

    [Fact]
    public void AnAlterOfAndToADeclarativeParagraph_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4C", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       GO TO DP2.
                   DP2.
                       EXIT.
            """, """
                   M1.
                       ALTER DP1 TO PROCEED TO DP2.
                       STOP RUN.
            """), 85);
        Assert.Contains(d, x => x.Contains("COBOL2376") && x.Contains("ALTER 'DP1'"));
    }

    [Fact]
    public void AnAlterToProceedToADeclarativeParagraph_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4E", Decl1, """
                   M1.
                       ALTER M2 TO PROCEED TO DP1.
                       STOP RUN.
                   M2.
                       GO TO M1.
            """), 85);
        Assert.Contains(d, x => x.Contains("COBOL2376") && x.Contains("ALTER TO PROCEED TO 'DP1'"));
    }

    [Fact]
    public void ASortInputProcedureNamingADeclarativeSection_IsRefused()
    {
        var d = BindDiagnostics($$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. SR4D.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "sr4d-f1.dat".
                       SELECT SW ASSIGN TO "sr4d-sw.dat".
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1.
                   01  F1-REC PIC X(10).
                   SD  SW.
                   01  SW-REC PIC X(10).
                   PROCEDURE DIVISION.
                   DECLARATIVES.
            {{Decl1}}
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       SORT SW ON ASCENDING KEY SW-REC
                           INPUT PROCEDURE IS D1
                           GIVING F1.
                       STOP RUN.
            """, 2023);
        Assert.Contains(d, x => x.Contains("COBOL2376") && x.Contains("SORT"));
    }

    /// <summary>The SR4 control: PERFORM is the exemption, from the nondeclarative portion AND from a different
    /// declarative section; and within its OWN section any statement may name a declarative paragraph.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void PerformIntoADeclarative_AndAnyReferenceWithinTheSameSection_AreAccepted(int level)
    {
        var d = BindDiagnostics(FileProgram("SR4OK", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       GO TO DP2.
                   DP2.
                       ADD 1 TO N.
                   D2 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
                   DP3.
                       PERFORM DP2.
            """, """
                   M1.
                       PERFORM DP1 THRU DP2.
                       PERFORM D1.
                       STOP RUN.
            """), level);
        Assert.True(d.Count == 0, string.Join("\n", d));
    }

    // ── SR3 — OUT of a declarative, RESUME only (PB362) — a WARNING by determination ─────────────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void APerformOrGoToOutOfADeclarative_IsWarned_AndCompiles(int level)
    {
        var d = BindDiagnostics(FileProgram("SR3A", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       PERFORM NONDECL-P.
                       GO TO NONDECL-P.
            """, """
                   M1.
                       STOP RUN.
                   NONDECL-P.
                       ADD 1 TO N.
            """), level);
        Assert.Equal(2, d.Count(x => x.Contains("COBOL2375") && x.Contains("nondeclarative procedures")));
        Assert.DoesNotContain(d, x => x.Contains("COBOL2376"));
    }

    /// <summary>The SR3 control: RESUME AT is the exemption (a COBOL-2002 statement).</summary>
    [Fact]
    public void AResumeAtANondeclarativeProcedure_IsNotWarned()
    {
        var d = BindDiagnostics(FileProgram("SR3OK", Decl1Resume, """
                   M1.
                       STOP RUN.
                   NONDECL-P.
                       ADD 1 TO N.
            """), 2002);
        Assert.DoesNotContain(d, x => x.Contains("COBOL2375") || x.Contains("COBOL2376"));
    }

    // ── SR10 / SR11 — USE BEFORE REPORTING (PB363) ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("GENERATE DET-A")]
    [InlineData("INITIATE R-1")]
    [InlineData("TERMINATE R-1")]
    public void AReportWriterVerbInABeforeReportingProcedure_IsRefused(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains("§2023:14.9.49.3 SR10"));
    }

    [Theory]
    [InlineData("MOVE 9 TO WS-GRP")]              // the control data item itself
    [InlineData("ADD 1 TO WS-GRP-LO")]            // read-modify-write of a subordinate item
    [InlineData("MOVE SPACES TO WS-CTL-REC")]     // a group containing it
    [InlineData("MOVE 1 TO WS-GRP-LO")]           // an item subordinate to it
    [InlineData("INITIALIZE WS-CTL-REC")]
    [InlineData("IF WS-N = 0 MOVE 9 TO WS-GRP END-IF")]   // nested: one diagnostic, not two
    [InlineData("MOVE \"22\" TO WS-CTL-RED")]            // a REDEFINES of the record holding it: shared bytes (kb/Work PB5808)
    [InlineData("MOVE \"2\" TO WS-CR (1)")]              // one occurrence of the redefining table, a byte of the control item
    [InlineData("MOVE \"2\" TO WS-CR (WS-IX)")]          // a run-time subscript may reach it
    [InlineData("MOVE \"2\" TO WS-CTL-REC (1:1)")]       // a window over the record that covers its byte
    [InlineData("MOVE \"2\" TO WS-CTL-RED (1:2)")]       // a window of the redefinition that covers it
    [InlineData("MOVE \"22\" TO WS-GRP-REN")]            // a RENAMES covering it
    public void AStoreIntoAControlDataItem_IsRefused(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains("§2023:14.9.49.3 SR11"));
    }

    /// <summary>SR11 is in every edition, each under its own citation (kb/Work PB5808): a store through a REDEFINES of the record
    /// holding the control data item is refused at 85, 2002, 2014 and 2023.</summary>
    [Theory]
    [InlineData(85, "§1985:XIII.4.9.3 SR4")]
    [InlineData(2002, "§2002:14.8.45.2 SR11")]
    [InlineData(2014, "§2014:14.9.45.2 SR11")]
    [InlineData(2023, "§2023:14.9.49.3 SR11")]
    public void AStoreThroughARedefinesOfTheControlItemsRecord_IsRefusedAtEveryEdition(int level, string citation)
    {
        var d = BindDiagnostics(ReportProgram("MOVE \"22\" TO WS-CTL-RED"), level);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains(citation));
    }

    /// <summary>The SR11 control: stores into other items conform, and a BY REFERENCE CALL argument is not an
    /// alteration the source can be convicted of (the callee may not store).</summary>
    [Theory]
    [InlineData("MOVE 7 TO WS-N")]
    [InlineData("ADD 1 TO WS-N")]
    [InlineData("CALL \"SOMEPROG\" USING BY REFERENCE WS-GRP")]
    [InlineData("DISPLAY WS-GRP")]
    [InlineData("MOVE \"7\" TO WS-N-RED")]                // a redefinition of an item that shares no byte with a control item
    [InlineData("MOVE \"2\" TO WS-CR (3)")]              // the occurrence over WS-TAIL: no byte of the control item (kb/Work PB5808, review R1-1)
    [InlineData("MOVE \"2\" TO WS-CTL-RED (3:1)")]       // a window of the redefinition over WS-TAIL
    [InlineData("MOVE \"2\" TO WS-CTL-REC (3:1)")]       // a window of the record over WS-TAIL
    [InlineData("MOVE 2 TO WS-TAIL")]
    public void OtherStatementsInABeforeReportingProcedure_AreAccepted(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.DoesNotContain(d, x => x.Contains("COBOL2378"));
    }

    /// <summary>A statement that names only a FILE stores through its connector (kb/Work PB5885): a READ makes the record
    /// available in the record area (§2023:14.9.30.4 GR13) of every record description of the file and of a SAME RECORD AREA
    /// file, moves the relative record number into the RELATIVE KEY item (GR25) and the record's length into the DEPENDING ON
    /// item (§2023:13.18.43.4 GR15); CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and WRITE set the FILE STATUS item
    /// (§2023:9.1.13.1, §2023:12.4.5.8.4 GR1); a sequential-access WRITE of a relative file moves the record number into the
    /// RELATIVE KEY item (§2023:14.9.51.4 GR29). Each control data item of <see cref="FileReportProgram"/> is one of those.</summary>
    [Theory]
    [InlineData("READ INF AT END MOVE 1 TO WS-N")]                // the record area holds IN-KEY
    [InlineData("READ INF INTO WS-X AT END MOVE 1 TO WS-N")]      // the record area is stored before the INTO move
    [InlineData("READ INF2 AT END MOVE 1 TO WS-N")]               // SAME RECORD AREA: INF2's record area is INF's
    [InlineData("READ STF AT END MOVE 1 TO WS-N")]                // the FILE STATUS item WS-FS
    [InlineData("OPEN INPUT STF")]
    [InlineData("CLOSE STF")]
    [InlineData("READ RELF NEXT RECORD AT END MOVE 1 TO WS-N")]   // the RELATIVE KEY item WS-RK
    [InlineData("WRITE RELF-REC INVALID KEY MOVE 1 TO WS-N")]     // sequential access: the RELATIVE KEY item WS-RK
    [InlineData("READ VARF AT END MOVE 1 TO WS-N")]               // the DEPENDING ON item WS-LEN
    [InlineData("READ RELD NEXT RECORD AT END MOVE 1 TO WS-N")]   // dynamic access, NEXT: a format 1 READ stores WS-RK3
    public void AStoreThroughAFileConnectorIntoAControlDataItem_IsRefused(string statement)
    {
        var d = BindDiagnostics(FileReportProgram(statement), 2023);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains("§2023:14.9.49.3 SR11"));
    }

    /// <summary>The 1985 rule is the same (§1985:XIII.4.9.3 SR4), for the program kb/Work PB5885 measured.</summary>
    [Fact]
    public void AReadIntoTheRecordAreaHoldingAControlDataItem_IsRefusedAt85()
    {
        var d = BindDiagnostics(FileReportProgram("READ INF AT END MOVE 1 TO WS-N"), 85);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains("§1985:XIII.4.9.3 SR4"));
    }

    /// <summary>A CORRESPONDING statement stores only the receiving item of each corresponding pair (§1985:VI.6.19.4 GR1), and
    /// INITIALIZE ... REPLACING only the elementary items of the category it names (§1985:VI.6.17.4 GR2): neither alters WS-CCTL,
    /// the alphanumeric control data item of the group they name, which has no corresponding item in WS-CSRC (kb/Work PB5890).
    /// The same at 85 and 2023.</summary>
    [Theory]
    [InlineData("MOVE CORRESPONDING WS-CSRC TO WS-CGRP", 85)]
    [InlineData("MOVE CORRESPONDING WS-CSRC TO WS-CGRP", 2023)]
    [InlineData("ADD CORRESPONDING WS-CSRC TO WS-CGRP", 85)]
    [InlineData("ADD CORRESPONDING WS-CSRC TO WS-CGRP", 2023)]
    [InlineData("SUBTRACT CORRESPONDING WS-CSRC FROM WS-CGRP", 85)]
    [InlineData("SUBTRACT CORRESPONDING WS-CSRC FROM WS-CGRP", 2023)]
    [InlineData("INITIALIZE WS-CGRP REPLACING NUMERIC DATA BY 7", 85)]
    [InlineData("INITIALIZE WS-CGRP REPLACING NUMERIC DATA BY 7", 2023)]
    [InlineData("MOVE CORRESPONDING WS-CGRP TO WS-CSRC2", 85)]      // the control data item is a SENDING item of a pair
    [InlineData("MOVE CORRESPONDING WS-CGRP TO WS-CSRC2", 2023)]
    public void ACorrespondingOrInitializeThatStoresNoControlItemOfItsGroup_IsAccepted(string statement, int level)
    {
        var d = BindDiagnostics(CorrespondingReportProgram(statement), level);
        Assert.DoesNotContain(d, x => x.Contains("COBOL2378"));
    }

    /// <summary>The refusals that stay (kb/Work PB5890): a corresponding pair whose receiving item IS the control data item; an
    /// INITIALIZE whose expansion stores it (no REPLACING phrase sets an alphanumeric item to spaces, §1985:VI.6.17.4 GR5; REPLACING
    /// ALPHANUMERIC selects it, GR2); a MOVE to the group that holds it.</summary>
    [Theory]
    [InlineData("MOVE CORRESPONDING WS-CSRC2 TO WS-CGRP", 85, "§1985:XIII.4.9.3 SR4")]
    [InlineData("MOVE CORRESPONDING WS-CSRC2 TO WS-CGRP", 2023, "§2023:14.9.49.3 SR11")]
    [InlineData("INITIALIZE WS-CGRP", 85, "§1985:XIII.4.9.3 SR4")]
    [InlineData("INITIALIZE WS-CGRP", 2023, "§2023:14.9.49.3 SR11")]
    [InlineData("INITIALIZE WS-CGRP REPLACING ALPHANUMERIC DATA BY \"X\"", 85, "§1985:XIII.4.9.3 SR4")]
    [InlineData("INITIALIZE WS-CGRP REPLACING ALPHANUMERIC DATA BY \"X\"", 2023, "§2023:14.9.49.3 SR11")]
    [InlineData("MOVE SPACES TO WS-CGRP", 85, "§1985:XIII.4.9.3 SR4")]
    [InlineData("MOVE SPACES TO WS-CGRP", 2023, "§2023:14.9.49.3 SR11")]
    public void ACorrespondingOrInitializeThatStoresTheControlItem_IsRefused(string statement, int level, string citation)
    {
        var d = BindDiagnostics(CorrespondingReportProgram(statement), level);
        Assert.Single(d, x => x.Contains("COBOL2378") && x.Contains(citation));
    }

    /// <summary>The control: an I-O statement that stores none of its file's connector items that is a control data item alters
    /// none.</summary>
    [Theory]
    [InlineData("READ OTF AT END MOVE 1 TO WS-N")]
    [InlineData("OPEN INPUT OTF")]
    [InlineData("CLOSE OTF")]
    [InlineData("READ OTF INTO WS-X AT END MOVE 1 TO WS-N")]
    // A random READ takes the RELATIVE KEY item as its input and stores nothing into it (§1985:VIII.4.5.4 GR14 and
    // §2023:14.9.30.4 GR25 move the record number only on a format 1, sequential, READ; kb/Work PB5885 review R1-1).
    [InlineData("READ RELR INVALID KEY MOVE 1 TO WS-N")]
    [InlineData("READ RELD INVALID KEY MOVE 1 TO WS-N")]         // dynamic access without NEXT: a random read
    // A random or dynamic WRITE reads the key (§2023:14.9.51.4 GR29b); START, DELETE and REWRITE store no key; RELR and
    // RELD have no FILE STATUS item.
    [InlineData("WRITE RELR-REC INVALID KEY MOVE 1 TO WS-N")]
    [InlineData("START RELD KEY IS EQUAL TO WS-RK3 INVALID KEY MOVE 1 TO WS-N")]
    [InlineData("DELETE RELD RECORD INVALID KEY MOVE 1 TO WS-N")]
    [InlineData("REWRITE RELD-REC INVALID KEY MOVE 1 TO WS-N")]
    public void AnIoStatementOnAFileWithNoControlItem_IsAccepted(string statement)
    {
        var d = BindDiagnostics(FileReportProgram(statement), 2023);
        Assert.DoesNotContain(d, x => x.Contains("COBOL2378"));
    }

    /// <summary>SR11 places a store in the 01 record's own coordinates (kb/Work PB5808, review R2-1): a REDEFINES BELOW level 01
    /// whose original is not at the record's first byte. WS-NB1 overlays the control data item WS-NGRP; WS-NB2 overlays only WS-NOTH.</summary>
    [Theory]
    [InlineData("MOVE \"2\" TO WS-NB1", true)]
    [InlineData("MOVE \"2\" TO WS-NB2", false)]
    public void AStoreThroughANestedRedefines_IsPlacedInTheRecordNotTheClass(string statement, bool refused)
    {
        string source = ReportProgram(statement)
            .Replace("CONTROL IS WS-GRP", "CONTROL IS WS-NGRP")
            .Replace("01 WS-IX PIC 9 VALUE 1.",
                """
                01 WS-NREC.
                          02 WS-NPAD PIC X.
                          02 WS-NA.
                             03 WS-NGRP PIC 9.
                             03 WS-NOTH PIC 9.
                          02 WS-NB REDEFINES WS-NA.
                             03 WS-NB1 PIC X.
                             03 WS-NB2 PIC X.
                       01 WS-IX PIC 9 VALUE 1.
                """);
        var d = BindDiagnostics(source, 2023);
        Assert.Equal(refused, d.Any(x => x.Contains("COBOL2378")));
    }

    // ── The structure that keeps the next case automatic ──────────────────────────────────────────────────

    /// <summary>Only PERFORM and RESUME name an exemption; every other caller of the ONE procedure-name funnel
    /// takes the restricted default, so a new verb inherits SR3 and SR4 without being taught them.</summary>
    [Fact]
    public void OnlyPerformAndResume_ClaimAnExemptionFromTheDeclarativesBoundary()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding");
        var claims = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"(?:,|kind:)\s*ProcedureReferenceKind\.(Perform|Resume)\)"))
                claims.Add($"{Path.GetFileName(file)}:{m.Groups[1].Value}");
        Assert.Equal(new[] { "ControlFlowBinder.cs:Perform", "ControlFlowBinder.cs:Perform", "EcBinder.cs:Resume" },
            claims.Order().ToArray());
    }

    /// <summary>The three SR10 verbs read the ONE guard.</summary>
    [Fact]
    public void GenerateInitiateAndTerminate_ReadTheOneSr10Guard()
    {
        string rw = File.ReadAllText(
            TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "ReportWriterBinder.cs"));
        Assert.Contains("RejectInBeforeReporting(\"GENERATE\")", rw);
        // INITIATE and TERMINATE bind through the one report-name series, which asks the guard with its verb's word (kb/Work PB2158).
        Assert.Contains("RejectInBeforeReporting(verb.Word)", rw);
        foreach (string verb in new[] { "INITIATE", "TERMINATE" })
            Assert.Contains($"new(\"{verb}\", ", rw);
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────────────────────────────────

    private const string Decl1 = """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       ADD 1 TO N.
            """;

    private const string Decl1Resume = """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       RESUME AT NONDECL-P.
            """;

    /// <summary>A program with one sequential file F1 and an item N; <paramref name="declaratives"/> is empty for
    /// a program with no DECLARATIVES.</summary>
    private static string FileProgram(string id, string declaratives, string body) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {{id}}.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "declstruct-f1.dat"
                           ORGANIZATION IS SEQUENTIAL.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1.
                   01  F1-REC PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  N PIC 9(4) VALUE 0.
                   PROCEDURE DIVISION.
            {{(declaratives.Length == 0 ? "" : "       DECLARATIVES.\n" + declaratives + "\n       END DECLARATIVES.\n       MAIN SECTION.")}}
            {{body}}
            """;

    /// <summary>A report whose control data item WS-CCTL (alphanumeric) is the first item of the group WS-CGRP (kb/Work PB5890).
    /// WS-CSRC corresponds to WS-CGRP in WS-OTH and WS-OTA only; WS-CSRC2 corresponds in WS-CCTL and WS-OTH.</summary>
    private static string CorrespondingReportProgram(string statement) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. BRCORR.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "brcorr.rpt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01 WS-CGRP.
                      02 WS-CCTL PIC X(2) VALUE "AA".
                      02 WS-OTH PIC 9(4) VALUE 0.
                      02 WS-OTA PIC X(3) VALUE "ZZZ".
                   01 WS-CSRC.
                      02 WS-OTH PIC 9(4) VALUE 5.
                      02 WS-OTA PIC X(3) VALUE "QQQ".
                   01 WS-CSRC2.
                      02 WS-CCTL PIC X(2) VALUE "BB".
                      02 WS-OTH PIC 9(4) VALUE 6.
                   REPORT SECTION.
                   RD R-1 CONTROL IS WS-CCTL OF WS-CGRP
                       PAGE LIMIT IS 20 LINES.
                   01 DET-A TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC X(2) SOURCE IS WS-CCTL OF WS-CGRP.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   BR-SEC SECTION.
                       USE BEFORE REPORTING DET-A.
                   BR-P.
                       {{statement}}.
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       OPEN OUTPUT RPT.
                       INITIATE R-1.
                       GENERATE DET-A.
                       TERMINATE R-1.
                       CLOSE RPT.
                       STOP RUN.
            """;

    /// <summary>A report whose CONTROL is WS-GRP (subordinate to WS-CTL-REC, with WS-GRP-LO subordinate to it) and
    /// a USE BEFORE REPORTING procedure whose one paragraph holds <paramref name="statement"/>.</summary>
    private static string ReportProgram(string statement) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. BRSTRUCT.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "brstruct.rpt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01 WS-CTL-REC.
                      02 WS-GRP.
                         03 WS-GRP-HI PIC 9.
                         03 WS-GRP-LO PIC 9.
                      02 WS-TAIL PIC 9.
                   66 WS-GRP-REN RENAMES WS-GRP-HI THRU WS-GRP-LO.
                   01 WS-CTL-RED REDEFINES WS-CTL-REC.
                      02 WS-CR PIC X OCCURS 3.
                   01 WS-IX PIC 9 VALUE 1.
                   01 WS-N PIC 9 VALUE 0.
                   01 WS-N-RED REDEFINES WS-N PIC X.
                   REPORT SECTION.
                   RD R-1 CONTROL IS WS-GRP
                       PAGE LIMIT IS 20 LINES.
                   01 DET-A TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC 9 SOURCE IS WS-N.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   BR-SEC SECTION.
                       USE BEFORE REPORTING DET-A.
                   BR-P.
                       {{statement}}.
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       OPEN OUTPUT RPT.
                       INITIATE R-1.
                       GENERATE DET-A.
                       TERMINATE R-1.
                       CLOSE RPT.
                       STOP RUN.
            """;

    /// <summary>A report whose control data items are each an item a file connector stores into (kb/Work PB5885): IN-KEY in the
    /// record of INF (whose record area INF2 shares), STF's FILE STATUS item WS-FS, RELF's RELATIVE KEY item WS-RK and VARF's
    /// DEPENDING ON item WS-LEN. OTF has none of them. The USE BEFORE REPORTING procedure's one paragraph holds
    /// <paramref name="statement"/>.</summary>
    private static string FileReportProgram(string statement) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. BRFILES.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "brfiles.rpt".
                       SELECT INF ASSIGN TO "brfiles-inf.dat".
                       SELECT INF2 ASSIGN TO "brfiles-inf2.dat".
                       SELECT STF ASSIGN TO "brfiles-stf.dat" FILE STATUS IS WS-FS.
                       SELECT RELF ASSIGN TO "brfiles-relf.dat" ORGANIZATION IS RELATIVE
                           ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS WS-RK.
                       SELECT RELR ASSIGN TO "brfiles-relr.dat" ORGANIZATION IS RELATIVE
                           ACCESS MODE IS RANDOM RELATIVE KEY IS WS-RK2.
                       SELECT RELD ASSIGN TO "brfiles-reld.dat" ORGANIZATION IS RELATIVE
                           ACCESS MODE IS DYNAMIC RELATIVE KEY IS WS-RK3.
                       SELECT VARF ASSIGN TO "brfiles-varf.dat".
                       SELECT OTF ASSIGN TO "brfiles-otf.dat".
                   I-O-CONTROL.
                       SAME RECORD AREA FOR INF INF2.
                   DATA DIVISION.
                   FILE SECTION.
                   FD INF.
                   01 IN-REC.
                      02 IN-KEY PIC X(2).
                      02 IN-AMT PIC 9(3).
                   FD INF2.
                   01 IN2-REC PIC X(9).
                   FD STF.
                   01 ST-REC PIC X(5).
                   FD RELF.
                   01 RELF-REC PIC X(5).
                   FD RELR.
                   01 RELR-REC PIC X(5).
                   FD RELD.
                   01 RELD-REC PIC X(5).
                   FD VARF RECORD IS VARYING IN SIZE FROM 1 TO 9 CHARACTERS DEPENDING ON WS-LEN.
                   01 VAR-REC PIC X(9).
                   FD OTF.
                   01 OT-REC PIC X(5).
                   FD RPT REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01 WS-FS PIC XX.
                   01 WS-RK PIC 9(4).
                   01 WS-LEN PIC 9(4).
                   01 WS-RK2 PIC 9(4).
                   01 WS-RK3 PIC 9(4).
                   01 WS-X PIC X(5).
                   01 WS-N PIC 9 VALUE 0.
                   REPORT SECTION.
                   RD R-1 CONTROLS ARE IN-KEY WS-FS WS-RK WS-LEN WS-RK2 WS-RK3
                       PAGE LIMIT IS 20 LINES.
                   01 DET-A TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC 9 SOURCE IS WS-N.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   BR-SEC SECTION.
                       USE BEFORE REPORTING DET-A.
                   BR-P.
                       {{statement}}.
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       OPEN OUTPUT RPT.
                       INITIATE R-1.
                       GENERATE DET-A.
                       TERMINATE R-1.
                       CLOSE RPT.
                       STOP RUN.
            """;

    /// <summary>Bind one fixture at <paramref name="level"/> and return the BIND errors AND warnings — every rule here is a
    /// syntax rule decided in the binder, so the parse must succeed for the test to mean anything.</summary>
    private static IReadOnlyList<string> BindDiagnostics(string source, int level)
    {
        string path = Path.Combine(Path.GetTempPath(), $"declstruct_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = TestSourceFormat.Inline, DialectLevel = level }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(level);
            new BinderDriver().Bind(tree!, edition);
            return [.. edition.Diagnostics, .. edition.Warnings];   // SR3 is a WARNING (D-DECLREF)
        }
        finally { FileHarness.TryDelete(path); }
    }
}
