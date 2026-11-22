// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A FILE CONTROL OR FILE DESCRIPTION CLAUSE OPERAND REFUSED WHERE IT IS WRITTEN IS NOT JUDGED AGAIN BY ITS USERS (kb/Work PB6041).
/// <para>The code read of 1985-SR-VIII.2.4.3-1 found that every refused RELATIVE KEY (an ambiguous name, a qualifier off the
/// item's hierarchy, a subscript, an item of the file's own record) also drew COBOL0862 on the file's START, "data-name-1 shall
/// be the RELATIVE KEY item '(none)'" (§1985:VIII.4.7.3 SR4, §2023:14.9.41.3 SR5): a second verdict about an operand already
/// refused, which tells the programmer to change a statement that is right. The sweep found the same mechanism at every user of
/// a refused clause operand: READ KEY IS (COBOL0864) and START KEY IS (COBOL0862) against a refused RECORD KEY or ALTERNATE RECORD
/// KEY, START with no KEY phrase (a COBOL1756 "not implemented" warning, or GR8's "no RELATIVE KEY clause"), and the external
/// file's "shall be an external data item" (COBOL1624) for a refused RELATIVE KEY, FILE STATUS or LINAGE operand.</para>
/// <para>Each program below breaks exactly ONE rule, so its error list is exactly that one refusal, at every edition the program
/// is otherwise legal at. The programs are fixed form: 85 defines no free form (kb/Work PB5754).</para>
/// </summary>
public sealed class RefusedFileClauseOperandTests
{
    private static void AssertOnlyRefusal(string source, int edition, string code)
    {
        var (ok, diags) = EditionHarness.Compile(source, edition, format: InitialReferenceFormat.Fixed);
        Assert.False(ok, $"--std {edition}: accepted a program that breaks a rule");
        Assert.True(diags.Count == 1 && diags[0].Contains(code, StringComparison.Ordinal),
            $"--std {edition}: expected exactly one diagnostic, {code}; got:\n  " + string.Join("\n  ", diags));
    }

    /// <summary>A relative file whose RELATIVE KEY clause writes <paramref name="relativeKey"/>, with every statement that uses
    /// the key: START with and without the KEY phrase, READ, WRITE, REWRITE and DELETE (§1985:VIII.4).</summary>
    private static string RelativeProgram(string relativeKey, string record = "       01  REL-REC PIC X(5).") => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB6041R"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS {relativeKey}
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
{record}
       WORKING-STORAGE SECTION.
       01  TOP-A.
           05  MID-A.
               10  RK PIC 9(3).
       01  TOP-B.
           05  MID-B.
               10  RK PIC 9(3).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O REL-FILE.
           READ REL-FILE INVALID KEY DISPLAY "R".
           START REL-FILE INVALID KEY DISPLAY "S1".
           START REL-FILE KEY IS GREATER THAN RK OF TOP-A
               INVALID KEY DISPLAY "S2".
           WRITE REL-REC INVALID KEY DISPLAY "W".
           REWRITE REL-REC INVALID KEY DISPLAY "RW".
           DELETE REL-FILE INVALID KEY DISPLAY "D".
           CLOSE REL-FILE.
           STOP RUN.
""";

    /// <summary>§1985:IV.4.3.8.1 SR3 (§2023:8.4.2.2.3 SR1): RK names two items and no qualifier makes it unique (COBOL1639).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RelativeKey_Ambiguous_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(RelativeProgram("RK"), edition, "COBOL1639");

    /// <summary>A qualifier that is not on the item's hierarchy references nothing (COBOL1639).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2023)]
    public void RelativeKey_WrongQualifier_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(RelativeProgram("RK OF NOPE"), edition, "COBOL1639");

    /// <summary>Qualifiers written out of order, from the innermost outward reversed, reference nothing (COBOL1639).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void RelativeKey_QualifiersOutOfOrder_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(RelativeProgram("RK OF TOP-A OF MID-A"), edition, "COBOL1639");

    /// <summary>A subscript on data-name-1 at 85 and 2023 (COBOL2024); 2002 admits the subscript and refuses it because RK is
    /// no table element (COBOL2096).</summary>
    [Theory]
    [InlineData(85, "COBOL2024")]
    [InlineData(2002, "COBOL2096")]
    [InlineData(2023, "COBOL2024")]
    public void RelativeKey_Subscripted_IsTheOneRefusal(int edition, string code) =>
        AssertOnlyRefusal(RelativeProgram("RK OF TOP-A (1)"), edition, code);

    /// <summary>§1985:VIII.2.4.3 SR3 (§2023:12.4.5.13.3 SR3): qualification by its own record resolves the name, and the item lies in
    /// the file's record (COBOL0863). START's KEY names RK OF TOP-A, the item the program means to be the key; the clause is the
    /// one fault, so SR4/SR5 is not asked against the refused item.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2023)]
    public void RelativeKey_InItsOwnRecord_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(RelativeProgram("RK OF REL-REC IN REL-FILE",
            "       01  REL-REC.\n           05  RK PIC 9(3).\n           05  FILLER PIC XX."), edition, "COBOL0863");

    /// <summary>An indexed file whose RECORD KEY clause names RK ambiguously, with READ KEY IS, START KEY IS and START with no
    /// KEY phrase (GR15's prime key).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void RecordKey_Ambiguous_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "PB6041I"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  G1.
               10  IK PIC X(3).
           05  G2.
               10  IK PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O IX-FILE.
           READ IX-FILE KEY IS IK OF G1 INVALID KEY DISPLAY "R".
           START IX-FILE KEY IS = IK OF G1 INVALID KEY DISPLAY "S".
           START IX-FILE INVALID KEY DISPLAY "S2".
           WRITE IX-REC INVALID KEY DISPLAY "W".
           REWRITE IX-REC INVALID KEY DISPLAY "RW".
           DELETE IX-FILE INVALID KEY DISPLAY "D".
           CLOSE IX-FILE.
           STOP RUN.
""", edition, "COBOL1639");

    /// <summary>An ALTERNATE RECORD KEY named ambiguously, then named by READ KEY IS and START KEY IS.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2023)]
    public void AlternateRecordKey_Ambiguous_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "PB6041A"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  PK PIC X(3).
           05  G1.
               10  AK PIC X(3).
           05  G2.
               10  AK PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O IX-FILE.
           READ IX-FILE KEY IS AK OF G1 INVALID KEY DISPLAY "R".
           START IX-FILE KEY IS = AK OF G1 INVALID KEY DISPLAY "S".
           CLOSE IX-FILE.
           STOP RUN.
""", edition, "COBOL1639");

    /// <summary>An external relative file whose RELATIVE KEY names RK ambiguously: "shall be an external data item"
    /// (§1985:VIII.2.4.4 GR8) is not asked of a name that identifies no item. FILE STATUS is external, so no other rule breaks.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalRelativeKey_Ambiguous_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB6041E"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  RK PIC 9(3).
       01  TOP-B IS EXTERNAL.
           05  RK PIC 9(3).
       01  STATUS-AREA IS EXTERNAL.
           05  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O REL-FILE.
           READ REL-FILE INVALID KEY DISPLAY "R".
           CLOSE REL-FILE.
           STOP RUN.
""", edition, "COBOL1639");

    /// <summary>Two describers of one external relative file, the first naming RK ambiguously: the RELATIVE KEY agreement of
    /// the run unit's describers (§1985:VIII.2.4.4 GR8) has no item of the first to compare.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalRelativeKey_AmbiguousInOneDescriber_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB6041P"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  RK PIC 9(3).
       01  TOP-B IS EXTERNAL.
           05  RK PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O REL-FILE.
           READ REL-FILE INVALID KEY DISPLAY "R".
           CLOSE REL-FILE.
           CALL "PB6041Q".
           STOP RUN.
       END PROGRAM PB6041P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB6041P"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK OF TOP-A.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  RK PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O REL-FILE.
           READ REL-FILE INVALID KEY DISPLAY "R".
           CLOSE REL-FILE.
           EXIT PROGRAM.
       END PROGRAM PB6041Q.
""", edition, "COBOL1639");

    /// <summary>An external file whose LINAGE names LN ambiguously (§1985:VII.3.7.4 GR11c is not asked of it).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalLinage_Ambiguous_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PR-FILE ASSIGN TO "PB6041L".
       DATA DIVISION.
       FILE SECTION.
       FD  PR-FILE IS EXTERNAL
           LINAGE IS LN LINES.
       01  PR-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  LN PIC 99.
       01  TOP-B IS EXTERNAL.
           05  LN PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PR-FILE.
           WRITE PR-REC AFTER ADVANCING 1 LINE.
           CLOSE PR-FILE.
           STOP RUN.
""", edition, "COBOL1639");

    /// <summary>An external file whose FILE STATUS names FS ambiguously: 2023's "shall be an external data item" (§2023:14.8.4.2) is
    /// not asked of it; 85 has no such rule, so the program is refused once there too.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalFileStatus_Ambiguous_IsTheOneRefusal(int edition) => AssertOnlyRefusal("""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "PB6041S"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE IS EXTERNAL.
       01  SQ-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  FS PIC XX.
       01  TOP-B IS EXTERNAL.
           05  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SQ-FILE.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
           STOP RUN.
""", edition, "COBOL1639");

    // ── The refused clause hides no OTHER fault (kb/Work PB6041 review R1-1) ─────────────────────────────────────────────────────────
    // A refused key clause leaves out only what could be the refused key: a KEY operand outside the file's records can never be a key
    // (§1985:IX.2.7.3 SR2, §1985:IX.2.5.3 SR2), so READ's SR11 and START's SR6 are asked of it as if no clause were refused; and an
    // external describer's refused key is undecided in the agreement of the entries, its other keys still compared.

    /// <summary>Exactly the diagnostics <paramref name="codes"/>, in source order.</summary>
    private static void AssertRefusals(string source, int edition, params string[] codes)
    {
        var (ok, diags) = EditionHarness.Compile(source, edition, format: InitialReferenceFormat.Fixed);
        Assert.False(ok, $"--std {edition}: accepted a program that breaks a rule");
        Assert.True(diags.Count == codes.Length && diags.Zip(codes).All(p => p.First.Contains(p.Second, StringComparison.Ordinal)),
            $"--std {edition}: expected exactly {string.Join(", ", codes)}; got:\n  " + string.Join("\n  ", diags));
    }

    /// <summary>An indexed file whose RECORD KEY (<paramref name="recordKey"/>) or ALTERNATE RECORD KEY (<paramref name="alternateKey"/>)
    /// may be refused, with READ KEY IS and START KEY IS naming <paramref name="keyOperand"/>, and a second file whose record holds
    /// OTHER-K.</summary>
    private static string IndexedProgram(string recordKey, string alternateKey, string keyOperand) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "PB6041K"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS {recordKey}
               ALTERNATE RECORD KEY IS {alternateKey}
               FILE STATUS IS FS.
           SELECT SQ-FILE ASSIGN TO "PB6041Z".
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  G1.
               10  PK PIC X(3).
               10  AK PIC X(3).
           05  G2.
               10  PK PIC X(3).
               10  AK PIC X(3).
       FD  SQ-FILE.
       01  SQ-REC.
           05  OTHER-K PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  WS-N PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O IX-FILE.
           READ IX-FILE KEY IS {keyOperand} INVALID KEY DISPLAY "R".
           START IX-FILE KEY IS = {keyOperand} INVALID KEY DISPLAY "S".
           CLOSE IX-FILE.
           STOP RUN.
""";

    /// <summary>The review's probe: the ALTERNATE RECORD KEY is ambiguous (COBOL1639), and READ and START name a WORKING-STORAGE item,
    /// which no repair of the clause makes a key: READ's SR11 (COBOL0864) and START's SR6 (COBOL0862) are reported too.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2023)]
    public void AlternateKeyRefused_KeyOperandOutsideTheRecord_IsStillJudged(int edition) =>
        AssertRefusals(IndexedProgram("PK OF G1", "AK", "WS-N"), edition, "COBOL1639", "COBOL0864", "COBOL0862");

    /// <summary>The RECORD KEY is the refused clause, and the operand lies in ANOTHER file's record: still no key of this file.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void RecordKeyRefused_KeyOperandInAnotherFilesRecord_IsStillJudged(int edition) =>
        AssertRefusals(IndexedProgram("PK", "AK OF G1", "OTHER-K"), edition, "COBOL1639", "COBOL0864", "COBOL0862");

    /// <summary>The control: the operand lies in the file's own record, so it could be the refused key once the clause is repaired,
    /// and the clause's refusal is the one diagnostic.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void RecordKeyRefused_KeyOperandInTheFilesRecord_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(IndexedProgram("PK", "AK OF G1", "PK OF G2"), edition, "COBOL1639");

    /// <summary>Two describers of one external indexed file; the first's ALTERNATE RECORD KEY clause <paramref name="firstAlternates"/>
    /// is refused or not, the second writes <paramref name="secondAlternates"/>.</summary>
    private static string ExternalIndexedPair(string firstAlternates, string secondAlternates) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "PB6041X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
{firstAlternates}.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE IS EXTERNAL.
       01  IX-REC.
           05  PK PIC X(3).
           05  G1.
               10  AK PIC X(3).
           05  G2.
               10  AK PIC X(3).
           05  X3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB6041Y".
           STOP RUN.
       END PROGRAM PB6041X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6041Y.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "PB6041X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
{secondAlternates}.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE IS EXTERNAL.
       01  IX-REC.
           05  PK PIC X(3).
           05  G1.
               10  AK PIC X(3).
           05  G2.
               10  AK PIC X(3).
           05  X3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           EXIT PROGRAM.
       END PROGRAM PB6041Y.
""";

    private const string AltAmbiguous = "               ALTERNATE RECORD KEY IS AK";
    private const string AltG1 = "               ALTERNATE RECORD KEY IS AK OF G1";
    private const string AltG2 = "               ALTERNATE RECORD KEY IS AK OF G2";

    /// <summary>The first describer's ALTERNATE RECORD KEY is ambiguous (COBOL1639) and the second names AK OF G1: the refused clause
    /// specifies no key to disagree with, so the entries' agreement (§1985:IX.2.3.4 GR1) reports nothing more.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalAlternateKey_AmbiguousInOneDescriber_IsTheOneRefusal(int edition) =>
        AssertOnlyRefusal(ExternalIndexedPair(AltAmbiguous, AltG1), edition, "COBOL1639");

    /// <summary>The same, but the second describer writes TWO alternate keys against the first's one: the number of alternate keys
    /// differs whatever the refused clause would name, a fault of its own (COBOL6847).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalAlternateKey_RefusedInOneDescriber_CountStillCompared(int edition) =>
        AssertRefusals(ExternalIndexedPair(AltAmbiguous, AltG1 + "\n" + AltG2), edition, "COBOL1639", "COBOL6847");

    /// <summary>The first describer writes AK (ambiguous) and X3, the second AK OF G1 and AK OF G2: the first's decided X3 has no equal
    /// in the second, and the second's two keys are more than the first's one undecided clause can be, so the entries disagree
    /// whatever the refused clause would name (COBOL6847).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalAlternateKey_RefusedInOneDescriber_DecidedKeysStillCompared(int edition) =>
        AssertRefusals(ExternalIndexedPair(AltAmbiguous + "\n               ALTERNATE RECORD KEY IS X3", AltG1 + "\n" + AltG2),
            edition, "COBOL1639", "COBOL6847");

    /// <summary>Three describers of one external relative file: the first's RELATIVE KEY is ambiguous (COBOL1639), and the other two name
    /// different external items, which disagree whatever the first would name (§1985:VIII.2.4.4 GR8).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ExternalRelativeKey_RefusedInOneDescriber_OthersStillCompared(int edition)
    {
        static string Describer(string id, string key, string call) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {id}.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB6041T"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS {key}.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  TOP-A IS EXTERNAL.
           05  RK PIC 9(3).
       01  TOP-B IS EXTERNAL.
           05  RK PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           {call}
           EXIT PROGRAM.
       END PROGRAM {id}.

""";
        string source = Describer("PB6041T", "RK", "CALL \"PB6041U\".") + Describer("PB6041U", "RK OF TOP-A", "CALL \"PB6041V\".")
            + Describer("PB6041V", "RK OF TOP-B", "CONTINUE.");
        var (ok, diags) = EditionHarness.Compile(source, edition, format: InitialReferenceFormat.Fixed);
        Assert.False(ok);
        Assert.True(diags.Count == 2 && diags[0].Contains("COBOL1639", StringComparison.Ordinal) && !diags[1].Contains("COBOL1639", StringComparison.Ordinal),
            $"--std {edition}: expected COBOL1639 and the RELATIVE KEY agreement; got:\n  " + string.Join("\n  ", diags));
    }
}
