// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE TYPE 6 CLASSIFIER AND THE CODE GENERATOR EVALUATE AN EXPRESSION ON ONE LANE (kb/Work PB5891 review R1-3).
/// </summary>
/// <remarks>
/// <para>The classifier bounds an intermediate's exactness by the precision of the lane the expression is evaluated on: the SDIDI
/// rounds to 34 significant digits, binary64 to 53 bits, the Int128 and exact lanes keep every digit. The review found an argument
/// the classifier showed exact under native arithmetic while a cancelled numeric function had moved the generated code onto the
/// SDIDI, which rounded it: <c>MOD ((Y * 100 - (SQRT (N) - SQRT (N) + X * 10.25)) * 4, 1000)</c> printed 40 for 41. Both now ask
/// <see cref="NumericLanes.Combine"/>: <c>NumericRenderer.CombineCore</c> dispatches on it
/// (<see cref="TheRenderersDispatch_ReadsTheOneLaneRule"/>), and the classifier reads the leaves through the bind-time carrier
/// prediction <c>FunctionValueLane.LaneOf</c>.</para>
/// <para><see cref="EveryOperandKindBesideEveryPartner_TakesTheLaneTheClassifierPredicts"/> pins the two for every operand kind the
/// classifier admits as a term (an item, a fixed-point and a floating-point literal, a floating-point item, a numeric function, an
/// integer function, an exact power, an SDIDI-valued function) beside a fixed-point literal, a floating-point literal and a
/// floating-point item: it compiles <c>COMPUTE R = L + P</c> for each pair, reads the lane of the root operation from the generated
/// C#, and requires the classifier's <c>ArgumentLane</c> of the same bound expression to name it.</para>
/// </remarks>
public sealed class NumericLaneDriftTests : WiseOwlCobolTestBase
{
    private static DataItem Item(string name, PicInfo pic) => new() { Level = 1, CobolName = name, ClrName = name, Pic = pic };

    private static readonly DataItem N = Item("N", new PicInfo(PicCategory.Numeric, Usage.Display, Length: 3, Digits: 3, Scale: 0, Signed: false));
    private static readonly DataItem F = Item("F", PicInfo.FloatItem(Usage.Double));
    private static readonly DataItem A = Item("A", new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: 4, Digits: 0, Scale: 0, Signed: false));
    private static readonly DataItem B = Item("B", new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false));

    private static Place PlaceOf(DataItem item) => new MemberPlace(new AccessPath([new RootFieldSegment(item.ClrName)]), item);

    private static BoundIntrinsicCall Call(string name, params BoundOperand[] args)
    {
        Assert.True(IntrinsicCatalog.TryGet(name, out var sig), $"no catalog row {name}");
        return new BoundIntrinsicCall(sig, args, PicCategory.Numeric) { Arithmetic = ArithmeticMode.Native };
    }

    /// <summary>(COBOL text, the same expression as a bound tree) of each operand kind.</summary>
    private static readonly (string Cobol, Func<BoundExpr> Bound)[] Operands =
    [
        ("N", () => new BoundNumRef(PlaceOf(N))),
        ("1.5E0", () => new BoundNumLiteral("1.5E0")),
        ("F", () => new BoundNumRef(PlaceOf(F))),
        ("FUNCTION SQRT (N)", () => Call("SQRT", new BoundFieldOperand(PlaceOf(N)))),
        ("FUNCTION ORD (B)", () => Call("ORD", new BoundFieldOperand(PlaceOf(B)))),
        ("N ** 2", () => new BoundPower(new BoundNumRef(PlaceOf(N)), new BoundNumLiteral("2"), NegativeBaseRealRoots: false)),
        ("FUNCTION NUMVAL (A)", () => Call("NUMVAL", new BoundFieldOperand(PlaceOf(A)))),
    ];

    /// <summary>The partners: a fixed-point literal (Int128), a floating-point literal (SDIDI), a floating-point item (binary64).</summary>
    private static readonly (string Cobol, Func<BoundExpr> Bound)[] Partners =
    [
        ("1", () => new BoundNumLiteral("1")),
        ("1.5E0", () => new BoundNumLiteral("1.5E0")),
        ("F", () => new BoundNumRef(PlaceOf(F))),
    ];

    /// <summary>The lane of the root operation of a store's rendered expression: the carrier of the outermost value the store
    /// lands (<c>CobolNum.Store(&lt;value&gt;, ...)</c>): an SDIDI operation, a binary64 landing, a wide or exact operation, or an
    /// Int128 sum.</summary>
    private static NumericLane RenderedLane(string store)
    {
        const string Store = "CobolNum.Store(";
        int at = store.IndexOf(Store, StringComparison.Ordinal);
        Assert.True(at >= 0, "the rendered store lands no value through CobolNum.Store: " + store);
        string value = store[(at + Store.Length)..].TrimStart('(');
        var lanes = new (string Prefix, NumericLane Lane)[]
        {
            ("CobolDec.", NumericLane.Sdidi), ("CobolFloat.", NumericLane.Binary64), ("CobolWide.", NumericLane.Exact),
            ("CobolExact.", NumericLane.Exact), ("Int128)", NumericLane.Scaled),
        };
        foreach (var (prefix, lane) in lanes)
            if (value.StartsWith(prefix, StringComparison.Ordinal)) return lane;
        Assert.Fail("the rendered store's value is on no known lane: " + store);
        return default;
    }

    [Fact]
    public void EveryOperandKindBesideEveryPartner_TakesTheLaneTheClassifierPredicts()
    {
        var pairs = Operands.SelectMany(o => Partners.Select(p => (o, p))).ToList();
        var proc = new StringBuilder();
        foreach (var (o, p) in pairs) proc.AppendLine($"    COMPUTE R = {o.Cobol} + {p.Cobol}");
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LANEPIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC 9(3) VALUE 4.
            01 F COMP-2 VALUE 0.5.
            01 A PIC X(4) VALUE "12".
            01 B PIC X VALUE "B".
            01 R PIC 9(5)V9(5).
            PROCEDURE DIVISION.
            {proc}    STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "lanepin.cob");
        File.WriteAllText(srcPath, source);
        var compiled = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, Path.Combine(TempDir, "lanepin.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free));
        Assert.True(compiled.Errors.Count == 0, "the lane program drew a diagnostic:\n  " + string.Join("\n  ", compiled.Errors));
        var stores = File.ReadAllLines(compiled.GeneratedCsPath!)
            .Where(l => Regex.IsMatch(l, @"^\s*R = ")).ToList();
        Assert.Equal(pairs.Count, stores.Count);
        var wrong = new List<string>();
        for (int i = 0; i < pairs.Count; i++)
        {
            var (o, p) = pairs[i];
            var rendered = RenderedLane(stores[i]);
            var predicted = IntrinsicArgumentRules.ArgumentLane(new BoundBinary(o.Bound(), '+', p.Bound()), ArithmeticMode.Native);
            if (rendered != predicted) wrong.Add($"{o.Cobol} + {p.Cobol}: rendered on {rendered}, classified on {predicted}");
        }
        Assert.True(wrong.Count == 0, "the classifier's lane is not the code generator's (kb/Work PB5891 R1-3):\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>The renderer's operation dispatch reads its lane from <see cref="NumericLanes.Combine"/> alone: the per-flag lane
    /// predicates it used to spell out for itself are gone, so a lane rule changed in one place changes both readers.</summary>
    [Fact]
    public void TheRenderersDispatch_ReadsTheOneLaneRule()
    {
        string source = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));
        int start = source.IndexOf("private NumX CombineCore(", StringComparison.Ordinal);
        Assert.True(start >= 0, "NumericRenderer declares no CombineCore");
        int end = source.IndexOf("internal static NumericLane LaneOf(NumX x)", start, StringComparison.Ordinal);
        Assert.True(end > start, "NumericRenderer.LaneOf no longer follows CombineCore");
        string body = string.Join("\n", source[start..end].Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
        Assert.Contains("NumericLanes.Combine(", body, StringComparison.Ordinal);
        foreach (string retired in new[] { "a.Real || b.Real || _rcv.Real", "!a.Real && !b.Real && !_rcv.Real", "(a.Dec || b.Dec) && !a.Real" })
            Assert.DoesNotContain(retired, body, StringComparison.Ordinal);
    }

    /// <summary>The rule itself, read from its documentation: a float item or receiver owns binary64; an engine approximation beside
    /// none enters the SDIDI; a wide or exact operand keeps the operation exact; an SDIDI operand takes it onto the SDIDI; a decimal
    /// engine evaluates everything there.</summary>
    [Theory]
    [InlineData((int)NumericLane.Scaled, (int)NumericLane.Scaled, false, false, (int)NumericLane.Scaled)]
    [InlineData((int)NumericLane.Scaled, (int)NumericLane.Sdidi, false, false, (int)NumericLane.Sdidi)]
    [InlineData((int)NumericLane.Approximation, (int)NumericLane.Scaled, false, false, (int)NumericLane.Sdidi)]
    [InlineData((int)NumericLane.Approximation, (int)NumericLane.Binary64, false, false, (int)NumericLane.Binary64)]
    [InlineData((int)NumericLane.Exact, (int)NumericLane.Sdidi, false, false, (int)NumericLane.Exact)]
    [InlineData((int)NumericLane.Exact, (int)NumericLane.Binary64, false, false, (int)NumericLane.Binary64)]
    [InlineData((int)NumericLane.Scaled, (int)NumericLane.Scaled, true, false, (int)NumericLane.Binary64)]
    [InlineData((int)NumericLane.Scaled, (int)NumericLane.Binary64, false, true, (int)NumericLane.Sdidi)]
    public void Combine_FollowsTheRule(int left, int right, bool floatReceiver, bool decimalEngine, int expected) =>
        Assert.Equal((NumericLane)expected, NumericLanes.Combine((NumericLane)left, (NumericLane)right, floatReceiver, decimalEngine));
}
