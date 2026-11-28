// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE 1985 AND 2002 SIGN OF THE SENDING OPERAND IS READ FROM THE ONE READ OF THE SENDER (kb/Work PB5852;
/// DESIGN-data-model §2.1.1). §1985:VI.6.19.4 GR2: a subscript, reference modifier or function-identifier of the sender
/// "is evaluated only once". <c>MoveEmitter.SendersSign</c> captures that one read and asks both the sign and the value of
/// the capture, so the emitted C# spells the sender once per source reference: a second spelling is a second evaluation,
/// and for <c>FUNCTION RANDOM</c> a different value. At 1985 no sender read can have a side effect: a subscript is "an
/// integer, a data-name optionally followed by an integer with the operator + or -" or an index-name likewise (§1985:III.2),
/// and a numeric function is no identifier operand (§1985:IV.4.3.8.3.3 SR4). Both shapes are pinned at 2002, where the same
/// arm runs, a subscript may be an arithmetic expression and a function-identifier may be the sender.
/// </summary>
public sealed class SendersSignOneReadTests
{
    private static string EmitCSharp(string source, int edition)
    {
        string dir = ScratchDirectory.Create("pb5852read");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, EditionInfo.Of(edition), Path.Combine(dir, "p.dll"), CheckOnly: false, SourceFormat: TestSourceFormat.Inline));
            Assert.True(r.Success, "the program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static int Randoms(string cs) => Regex.Matches(cs, @"CobolIntrinsics\.Random\(\)").Count;

    /// <summary>An IMAGE-carried sign holder sender (A, under the whole-group MOVE), subscripted by an expression holding
    /// FUNCTION RANDOM and by a data-name, moved into a receiver the arm writes (L, a truncating store): each MOVE reads the
    /// sender's element ONCE (one <c>CobolTable.At(T.A, …)</c> per MOVE; the sign and the value both read the capture), and
    /// RANDOM is spelled once. A second read of the element was the shape before the capture (measured: four reads).</summary>
    [Fact]
    public void ImageSender_WithAFunctionInItsSubscript_IsReadOnce()
    {
        string cs = EmitCSharp("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB5852RA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T.
               05 A PIC S999 SIGN LEADING SEPARATE OCCURS 5.
            01 I PIC 9 VALUE 2.
            01 L PIC S99 SIGN LEADING SEPARATE.
            PROCEDURE DIVISION.
            MAIN.
                MOVE ALL "-100" TO T.
                MOVE A (FUNCTION RANDOM * 4 + 1) TO L.
                MOVE A (I) TO L.
                STOP RUN.
            """, 2002);
        Assert.Contains("CobolNum.FormatImageSendersSign(", cs);
        Assert.Contains("CobolNum.ImageIsNegative(", cs);
        Assert.Equal(2, Regex.Matches(cs, @"CobolTable\.At\(T\.A,").Count);
        Assert.Equal(1, Randoms(cs));
    }

    /// <summary>A function-identifier sender (an identifier operand from 2002: at 1985 a numeric function stands only in an
    /// arithmetic expression, §1985:IV.4.3.8.3.3 SR4): its value is captured and both the sign test (CobolNum.IsNegative)
    /// and the store read the capture, so RANDOM is spelled once.</summary>
    [Fact]
    public void FunctionSender_IsEvaluatedOnce()
    {
        string cs = EmitCSharp("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB5852RF.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 L PIC S99 SIGN LEADING SEPARATE.
                   PROCEDURE DIVISION.
                   MAIN.
                       MOVE FUNCTION RANDOM TO L.
                       STOP RUN.
            """, 2002);
        Assert.Contains("CobolNum.FormatImageSendersSign(", cs);
        Assert.Contains("CobolNum.IsNegative(", cs);
        Assert.Equal(1, Randoms(cs));
    }
}
