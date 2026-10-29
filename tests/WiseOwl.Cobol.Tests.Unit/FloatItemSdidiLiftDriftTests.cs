// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FLOATING-POINT DATA ITEM ENTERS THE STANDARD-DECIMAL INTERMEDIATE THROUGH ONE CONVERSION, IN EVERY POSITION (kb/Work
/// PB1606). ISO §8.8.1.5.1 leaves "the mechanisms for conversion of data items described with a usage of float-short,
/// float-long, or float-extended into standard-decimal intermediate data items" to the implementor, and §8.8.1.5.2 r1
/// converts "any operand of an arithmetic expression". WiseOwl COBOL's conversion is <c>CobolDec.FromBinary64</c> (the
/// exact binary value, rounded once under INTERMEDIATE ROUNDING). It used to be two: an operand beside an operator went
/// through the shortest round-trip decimal and a float item that was the WHOLE expression (<c>COMPUTE R = F</c>) landed
/// natively, so <c>F</c> and <c>F + 0</c> stored different values into one receiver.
/// <para>The shortest round-trip decimal survives for ONE kind of value, an implementor-defined APPROXIMATION (a §15.4.1
/// function's returned value, a non-integer power): <c>CobolDec.FromApproximation</c>. This test keeps both spellings
/// closed lists, so a new float lift states which kind of value it carries.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FloatItemSdidiLiftDriftTests
{
    private static IEnumerable<(string File, string Line)> CodeLines(string project)
    {
        foreach (string file in Directory.GetFiles(TestRepo.Src(project), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            foreach (string line in File.ReadLines(file))
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                    yield return (Path.GetFileName(file), line);
        }
    }

    private static List<string> Callers(string project, string needle, params string[] allowedFiles) =>
        CodeLines(project)
            .Where(l => l.Line.Contains(needle, StringComparison.Ordinal) && !allowedFiles.Contains(l.File))
            .Select(l => $"{l.File}: {l.Line.Trim()}")
            .ToList();

    [Fact]
    public void TheFloatItemLift_IsEmittedByDecOperandExactAlone()
    {
        // RuntimeApi.DecFromBinary64 is the spelling; one renderer arm calls it.
        var offenders = Callers("WiseOwl.Cobol.Compiler", "DecFromBinary64(", "RuntimeApi.cs", "NumericRenderer.cs");
        Assert.True(offenders.Count == 0, "A float item is lifted into the SDIDI only by NumericRenderer.DecOperandExact (kb/Work PB1606): "
            + string.Join("; ", offenders));
        var calls = CodeLines("WiseOwl.Cobol.Compiler")
            .Where(l => l.File == "NumericRenderer.cs" && l.Line.Contains("DecFromBinary64(", StringComparison.Ordinal))
            .ToList();
        Assert.Single(calls);
        Assert.Contains("{ Real: true } r =>", calls[0].Line);
    }

    [Fact]
    public void TheApproximationLift_HasOnlyTheEngineProducedValuesAsCallers()
    {
        // The renderer's three: a prose-family function's returned value (IntrinsicRenderer), an engine approximation
        // beside non-float operands (LiftApproximation) and the same value reaching DecOperandExact (NumericRenderer).
        var offenders = Callers("WiseOwl.Cobol.Compiler", "DecFromApproximation(", "RuntimeApi.cs", "NumericRenderer.cs", "IntrinsicRenderer.cs");
        Assert.True(offenders.Count == 0, "A shortest-round-trip lift is for an implementor-defined approximation only (kb/Work PB1606): "
            + string.Join("; ", offenders));
        // In the runtime: the non-integer power of the r2e development, and the sending operand of a CALL's BY CONTENT function
        // value (MoveSending.ExactValue: its Approximate flag is stated by the activating element, never inferred), and the
        // temporary of a function's binary64 RESULT that DISPLAY orders and shows (FunctionValueField.OfFloat, kb/Work PB3514:
        // a floating-point ITEM argument arrives with its exact lift, OfFloatItem, and never reaches this one).
        var runtime = Callers("WiseOwl.Cobol.Runtime", "CobolDec.FromApproximation(", "CobolIntrinsics.RealArgs.cs", "CallAbi.cs", "FunctionValueField.cs");
        Assert.True(runtime.Count == 0, "CobolDec.FromApproximation lifts a function's or a power's binary64 approximation, never a data item: "
            + string.Join("; ", runtime));
    }

    [Fact]
    public void TheRuntime_HasNoShortestDecimalConversionOfADataItem()
    {
        // The data-item conversions of the runtime: DATE seconds (a range check) lifts by the exact value; the
        // floating-point numeric-edited sender by the exact value as a round-to-odd significand.
        var exact = Callers("WiseOwl.Cobol.Runtime", "CobolDec.FromBinary64(", "CobolDate.cs");
        Assert.True(exact.Count == 0, "A new runtime caller of CobolDec.FromBinary64 states its INTERMEDIATE ROUNDING mode itself: " + string.Join("; ", exact));
        var odd = Callers("WiseOwl.Cobol.Runtime", "CobolDec.FromBinary64ToOdd(", "CobolEdit.Float.cs", "CallAbi.cs");
        Assert.True(odd.Count == 0, "FromBinary64ToOdd is the floating-point numeric-edited sender's form only: " + string.Join("; ", odd));
        // "R" formatting of a double is FromApproximation's alone inside CobolDec.
        var shortest = CodeLines("WiseOwl.Cobol.Runtime")
            .Where(l => l.File == "CobolDec.cs" && l.Line.Contains("ToString(\"R\"", StringComparison.Ordinal)).ToList();
        Assert.Single(shortest);
    }

    [Fact]
    public void TheFloatingPointEditedSender_IsNeverAFixedPointValueThroughBinary64_NorAnApproximationByItsBinaryExpansion()
    {
        // The float-edited store (CobolEdit.FormatFloatStore(double, ...)) reads a binary64 by its EXACT binary value, which is
        // right for a floating-point data item and wrong for any other value that reaches it as a double: a fixed-point
        // sender at a CALL's activation boundary (0.3 would show as +2.9999999999999998E-01) and an engine approximation
        // (a function's value). Each states its own exact form instead (kb/Work PB1606 review round 1, finding R1-1).
        string move = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "MoveEmitter.cs"));
        Assert.Contains("MoveSendingExactValue(sending)", move);
        Assert.DoesNotContain("MoveSendingBinary64", move);
        Assert.Null(typeof(WiseOwl.Cobol.Runtime.MoveSending).GetProperty("Binary64"));
        Assert.True(typeof(WiseOwl.Cobol.Runtime.MoveSending).GetProperty("ExactValue") is not null,
            "MoveSending hands a floating-point edited receiver its ExactValue, never a binary64 of a fixed-point sender");
        string api = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Roslyn", "RuntimeApi.cs")).Replace("\r\n", "\n");
        int entry = api.IndexOf("FloatEditEntry(Emit.NumX value) =>", StringComparison.Ordinal);
        Assert.True(entry >= 0, "FloatEditEntry not found");
        int approx = api.IndexOf("Emit.NumXCarrier.Binary64 when value.Approximate", entry, StringComparison.Ordinal);
        int exact = api.IndexOf("Emit.NumXCarrier.Binary64 or Emit.NumXCarrier.Sdidi", entry, StringComparison.Ordinal);
        Assert.True(approx > entry && approx < exact,
            "FloatEditEntry must send an Approximate binary64 through DecFromApproximation BEFORE its exact-binary64 arm");
        // The same approximation crossing a CALL's activation boundary BY CONTENT (review round 2, finding R2-1): the cell is a
        // bare double, so the activating element states that it is an approximation, and the sending operand reads it so.
        string call = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "CallEmitter.cs"));
        Assert.Contains("approximateReal = x.Approximate;", call);
        Assert.Contains("RuntimeApi.ArgWithValueSending(built, OperandText.StorageImageConverter(computed), approximateReal)", call);
    }

    [Fact]
    public void AFloatItemThatIsTheWholeExpression_IsConvertedAtTheArithmeticStore()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "ArithmeticEmitter.cs")).Replace("\r\n", "\n");
        int start = src.IndexOf("public void StoreArith(Place target, NumX value, CobolRounding mode)", StringComparison.Ordinal);
        Assert.True(start >= 0, "StoreArith not found");
        string head = src[start..Math.Min(src.Length, start + 8000)];
        int lift = head.IndexOf("if (num.StandardDecimal && value.Real)", StringComparison.Ordinal);
        // The first receiver-category arm (the §14.7.4.3 rule 7 gate that once read the value first is gone, kb/Work PB3990).
        int firstUse = head.IndexOf("if (target.Item.Pic is", StringComparison.Ordinal);
        Assert.True(lift >= 0 && firstUse > lift,
            "StoreArith must convert a lone float operand into the SDIDI (num.DecOperand) BEFORE any arm reads the value: under a "
            + "standard-decimal mode `COMPUTE R = F` and `COMPUTE R = F + 0` store one value (ISO §8.8.1.5.2 r1, kb/Work PB1606).");
        Assert.Contains("num.DecOperand(value)", head[lift..]);
    }
}
