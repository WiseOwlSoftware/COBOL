// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A STRING READS AN OPERAND IN PLACE WHEN A TRANSFER OF THE STATEMENT CAN REACH IT, AND ONLY THEN, AND THE QUESTION
/// "CAN A TRANSFER REACH IT?" IS ANSWERED IN ONE PLACE (kb/Work PB1908). ISO §14.9.43.4 GR10 (Annex A.2 item 60)
/// leaves a STRING whose sending or delimiter operand shares storage with the receiving item undefined;
/// WiseOwl COBOL runs it in place, as GnuCOBOL's <c>cob_string_append</c> does (docs/CONFORMANCE.md D-STR1 to D-STR4),
/// so the emitter stores the receiver image back and reads the operand again when its transfer begins.
/// <para>Three properties keep that true. (1) The decision is <c>StorageOverlap.MayShareStorage</c> (the finer of the two
/// predicates, kb/Work PB1911 keeps it) and nothing in the STRING emitter walks a record. (2) The cost is paid only
/// where the predicate says so: a STRING over disjoint operands, or whose only reachable operand is its first, renders
/// ONE store of the image, the code it always had, which is also why the Characterization emit snapshots did not move.
/// This class reads the EMITTED C# over each arm of the predicate (the same record, REDEFINES, LINKAGE, a level-66
/// alias, a function argument) and over the operands that are never in reach (a literal, a sibling field, the first
/// sending), so a new arm that forgets the re-read fails here even if no golden crosses it. (3) A function
/// operand of a later sending that may share identifier-3's storage is evaluated ONCE, inline, when its transfer
/// begins (D-STR4), never twice; every other function, the first sending's included, is identified up front in
/// statement order (§14.6.4 7, kb/Work PB3261), so its range tests precede identifier-3's.</para>
/// </summary>
public sealed class StringLiveReadDriftTests
{
    private static string EmitCSharp(string source)
    {
        string dir = ScratchDirectory.Create("pb1908drift");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string Program(string data, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB1908DRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{data}}
        PROCEDURE DIVISION.
        MAIN.
            {{procedure}}
            STOP RUN.
        """;

    /// <summary>The receiver image stored back: an assignment whose right side is the working image local. A STRING
    /// stores once at its end; the in-place read adds one store before each transfer whose operand is reachable.
    /// Every receiver shape stores the local its own way (<c>R = img;</c>, <c>R.FromImage(img);</c>,
    /// <c>R = WindowInto(R, ..., img);</c>), so a store is any statement that ends in the image local and does not itself
    /// assign it (the working image's declaration and its transfers start with the local or with <c>string</c>).</summary>
    private static readonly Regex ImageStore = new(@"^\s*(?!string |__strInto)\S[^\r\n]*[ (,]__strInto\d+\)*;[ \t]*\r?$", RegexOptions.Multiline);

    /// <summary>A sender or delimiter local read again: an assignment to the local (the declaration has a type in front).</summary>
    private static readonly Regex Reread = new(@"^\s*__str(Src|Dlm)\d+_\d+ = ", RegexOptions.Multiline);

    private const string Disjoint = """
        01 R1   PIC X(12) VALUE "ABCDEFGHIJKL".
        01 S1   PIC X(4)  VALUE "abcd".
        01 D1   PIC X     VALUE "c".
        """;

    private static int Stores(string cs) => ImageStore.Matches(cs).Count;

    [Fact]
    public void DisjointOperands_StoreTheImageOnceAndReadNothingAgain()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING S1(1:2) DELIMITED BY D1 "x" DELIMITED BY SIZE S1 DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.DoesNotMatch(Reread, cs);
    }

    [Fact]
    public void SiblingFieldsOfOneRecord_AreProvedDisjoint()
    {
        const string data = """
            01 REC.
               05 F1 PIC X(4) VALUE "abcd".
               05 F2 PIC X(4) VALUE "efgh".
               05 F3 PIC X(4) VALUE "ijkl".
            """;
        string cs = EmitCSharp(Program(data, "STRING F1(1:2) DELIMITED BY SIZE F3(1:2) DELIMITED BY SIZE INTO F2 END-STRING"));
        Assert.Equal(1, Stores(cs));
        Assert.DoesNotMatch(Reread, cs);
    }

    [Fact]
    public void OnlyTheFirstSendingReachesTheReceiver_NeedsNoStore()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING R1(1:2) DELIMITED BY SIZE "x" DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.DoesNotMatch(Reread, cs);
    }

    [Fact]
    public void ALaterSenderInTheReceiver_StoresTheImageAndReadsTheSenderAgain()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING "XY" DELIMITED BY SIZE R1(1:2) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(2, Stores(cs));
        Assert.Matches(new Regex(@"^\s*__strSrc\d+_1 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void ALaterIdentifierDelimiterInTheReceiver_ReadsTheDelimiterAgain()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING "x" DELIMITED BY SIZE "12x34" DELIMITED BY R1(1:1) INTO R1 END-STRING"""));
        Assert.Equal(2, Stores(cs));
        Assert.Matches(new Regex(@"^\s*__strDlm\d+_1 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void ASenderThatStaysInlineIsNotAssignedAgain_ButTheImageIsStoredFirst()
    {
        // A plain field renders inline into the transfer call, so it is read when the call runs: the store must precede it.
        string cs = EmitCSharp(Program(Disjoint, """STRING "XY" DELIMITED BY SIZE R1 DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(2, Stores(cs));
        Assert.DoesNotMatch(Reread, cs);
    }

    [Fact]
    public void RedefinesViewOfTheReceiver_Reads()
    {
        const string data = """
            01 R1   PIC X(12) VALUE "ABCDEFGHIJKL".
            01 VW REDEFINES R1.
               05 V1 PIC X(3).
               05 V2 PIC X(9).
            """;
        string cs = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE V2(1:2) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(2, Stores(cs));
    }

    [Fact]
    public void Level66AliasOfTheReceivedField_Reads()
    {
        const string data = """
            01 REC.
               05 FA PIC X(4) VALUE "AAA,".
               05 FB PIC X(5) VALUE "BBB,C".
               66 AL RENAMES FA THRU FB.
            """;
        string cs = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE AL(1:2) DELIMITED BY SIZE INTO FB END-STRING"""));
        Assert.Equal(2, Stores(cs));
    }

    [Fact]
    public void LinkageSender_ReadsBecauseItsStorageIsDecidedAtRunTime()
    {
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB1908DRIFTL.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-R PIC X(9).
            LINKAGE SECTION.
            01 LK-S PIC X(9).
            PROCEDURE DIVISION USING LK-S.
            MAIN.
                STRING "XY" DELIMITED BY SIZE LK-S DELIMITED BY SIZE INTO WS-R END-STRING
                GOBACK.
            """;
        Assert.Equal(2, Stores(EmitCSharp(program)));
    }

    [Fact]
    public void FunctionOverTheReceiver_IsEvaluatedOnce_InlineAtItsTransfer()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING "XY" DELIMITED BY SIZE FUNCTION UPPER-CASE(R1) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(2, Stores(cs));
        // no up-front local for it, and exactly one evaluation of the function
        Assert.DoesNotMatch(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), cs);
        Assert.Single(Regex.Matches(cs, @"CobolIntrinsics\.UpperCase\("));
    }

    [Fact]
    public void FunctionOverADisjointField_IsEvaluatedUpFrontAndNeverStoresBetween()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING "XY" DELIMITED BY SIZE FUNCTION UPPER-CASE(S1) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.Matches(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void AFunctionInTheFirstSending_IsEvaluatedUpFront_EvenOverTheReceiver()
    {
        // Review R1-1: no store precedes the first transfer, so §14.9.43.4 GR10 cannot reach it and its function is
        // identified in statement order (§14.6.4 7), before identifier-3 is read - not inline in the transfer after that read.
        string cs = EmitCSharp(Program(Disjoint, """STRING FUNCTION UPPER-CASE(R1) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.Matches(new Regex(@"string __strSrc\d+_0 = ", RegexOptions.Multiline), cs);
        Assert.Single(Regex.Matches(cs, @"CobolIntrinsics\.UpperCase\("));
    }

    [Fact]
    public void AFunctionDelimiterInTheFirstSending_IsEvaluatedUpFront_EvenOverTheReceiver()
    {
        string cs = EmitCSharp(Program(Disjoint, """STRING S1 DELIMITED BY FUNCTION UPPER-CASE(R1(1:1)) INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.Matches(new Regex(@"string __strDlm\d+_0 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void ARefModifiedFunctionResultOverADisjointItem_IsEvaluatedUpFront()
    {
        // Review R1-2: the arm that cannot be walked answered "may share" for a shape it CAN walk; every storage the
        // function reads (its argument, the positions of its result's reference modification) is disjoint here.
        const string data = Disjoint + """

            01 N1   PIC 9 VALUE 2.
            """;
        string cs = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE FUNCTION UPPER-CASE(S1)(1:N1) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.Matches(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void AnArithmeticFunctionArgumentOverADisjointItem_IsEvaluatedUpFront()
    {
        const string data = Disjoint + """

            01 N1   PIC 99 VALUE 2.
            """;
        string cs = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE FUNCTION CHAR(N1 + 64) DELIMITED BY SIZE INTO R1 END-STRING"""));
        Assert.Equal(1, Stores(cs));
        Assert.Matches(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), cs);
    }

    [Fact]
    public void AReferenceModifierPositionOrArithmeticArgumentInTheReceiver_StillReadsInPlace()
    {
        // The walk answers "may share" when the storage a position or an arithmetic argument reads can be identifier-3's.
        const string data = """
            01 REC.
               05 CNT PIC 9 VALUE 2.
               05 BUF PIC X(8) VALUE "ABCDEFGH".
            01 S1   PIC X(4)  VALUE "abcd".
            """;
        string position = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE FUNCTION UPPER-CASE(S1)(1:CNT) DELIMITED BY SIZE INTO REC END-STRING"""));
        Assert.Equal(2, Stores(position));
        Assert.DoesNotMatch(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), position);
        string arithmetic = EmitCSharp(Program(data, """STRING "XY" DELIMITED BY SIZE FUNCTION CHAR(CNT + 64) DELIMITED BY SIZE INTO REC END-STRING"""));
        Assert.Equal(2, Stores(arithmetic));
        Assert.DoesNotMatch(new Regex(@"string __strSrc\d+_1 = ", RegexOptions.Multiline), arithmetic);
    }

    [Fact]
    public void TheEmitterAsksTheOnePredicate_AndWalksNoRecords()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "StringEmitter.cs"));
        Assert.Contains("StorageOverlap.MayShareStorage(", src);
        Assert.DoesNotContain(".Parent", src);
        Assert.DoesNotContain(".RedefinesTarget", src);
    }
}
