// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using BenchmarkDotNet.Attributes;
using WiseOwl.Cobol.Benchmarks.Collation;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Benchmarks.Numeric;

/// <summary>
/// The SLOW PATH of the scaled→binary conversions (<see cref="CobolFloat.ScaledToDouble"/>,
/// <see cref="CobolFloat.ScaledToSingle"/>; kb/Work PB2658): a value whose unscaled magnitude passes the exact-operand
/// bound (2^53 / 2^24) or whose scale passes the exact power of ten (22 / 10) is converted by one correctly rounded
/// decimal parse of <c>&lt;unscaled&gt;E&lt;−scale&gt;</c>. The comparison axis is the text's construction: the
/// baseline is the form PB2658 replaced (two <c>ToString</c>s and a concatenation, three strings per conversion), the
/// candidate is the runtime's stack-formatted span. <see cref="Setup"/> is the witness that both compute the same
/// thing: every corpus value converts to the SAME bits through both, or the run refuses to start.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ScaledFloatBenchmarks
{
    private const string Double = "ScaledToDouble-SlowPath";
    private const string Single = "ScaledToSingle-SlowPath";
    private const int Ops = 64;

    private Int128[] _unscaled = null!;
    private int[] _scale = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 64 slow-path operands, four shapes of sixteen: a 31-digit fixed-point value at scale 25 (the PB115 ASIN
        // case), an 18-digit value past 2^53 at scale 10, a small value at scale 30 (the exponent bound), and a
        // 38-digit value near the Int128 limit, negative, at scale 2.
        var rng = new Random(2658);
        _unscaled = new Int128[Ops];
        _scale = new int[Ops];
        for (int i = 0; i < Ops; i++)
        {
            (_unscaled[i], _scale[i]) = (i % 4) switch
            {
                0 => (Digits(rng, 31), 25),
                1 => (Digits(rng, 18), 10),
                2 => ((Int128)rng.Next(1, 1000), 30),
                _ => (-Digits(rng, 38), 2),
            };
        }
        for (int i = 0; i < Ops; i++)
        {
            double before = BeforeDouble(_unscaled[i], _scale[i]), after = CobolFloat.ScaledToDouble(_unscaled[i], _scale[i]);
            float beforeSingle = BeforeSingle(_unscaled[i], _scale[i]), afterSingle = CobolFloat.ScaledToSingle(_unscaled[i], _scale[i]);
            if (BitConverter.DoubleToInt64Bits(before) != BitConverter.DoubleToInt64Bits(after)
                || BitConverter.SingleToInt32Bits(beforeSingle) != BitConverter.SingleToInt32Bits(afterSingle))
                throw new InvalidOperationException(
                    $"corpus[{i}] = {_unscaled[i]}E{-_scale[i]}: the two constructions disagree ({before:R}/{after:R}, {beforeSingle:R}/{afterSingle:R})");
        }
    }

    private static Int128 Digits(Random rng, int count)
    {
        Int128 v = rng.Next(1, 10);
        for (int d = 1; d < count; d++) v = v * 10 + rng.Next(0, 10);
        return v;
    }

    /// <summary>The construction PB2658 replaced: three strings, then the parse.</summary>
    private static double BeforeDouble(Int128 unscaled, int scale) => double.Parse(
        unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale).ToString(CultureInfo.InvariantCulture),
        NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="BeforeDouble"/>
    private static float BeforeSingle(Int128 unscaled, int scale) => float.Parse(
        unscaled.ToString(CultureInfo.InvariantCulture) + "E" + (-scale).ToString(CultureInfo.InvariantCulture),
        NumberStyles.Float, CultureInfo.InvariantCulture);

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops), BenchmarkCategory(Double)]
    public double Double_StringConcat()
    {
        double acc = 0;
        for (int i = 0; i < Ops; i++) acc += BeforeDouble(_unscaled[i], _scale[i]);
        return acc;
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Double)]
    public double Double_StackSpan()
    {
        double acc = 0;
        for (int i = 0; i < Ops; i++) acc += CobolFloat.ScaledToDouble(_unscaled[i], _scale[i]);
        return acc;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops), BenchmarkCategory(Single)]
    public float Single_StringConcat()
    {
        float acc = 0;
        for (int i = 0; i < Ops; i++) acc += BeforeSingle(_unscaled[i], _scale[i]);
        return acc;
    }

    [Benchmark(OperationsPerInvoke = Ops), BenchmarkCategory(Single)]
    public float Single_StackSpan()
    {
        float acc = 0;
        for (int i = 0; i < Ops; i++) acc += CobolFloat.ScaledToSingle(_unscaled[i], _scale[i]);
        return acc;
    }
}
