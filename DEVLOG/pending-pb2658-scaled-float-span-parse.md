## PB2658: the scaled-to-binary conversions format their slow-path text on the stack

kb/Work PB2658 (R2 runtime-values-2 performance lead, cluster PB1754) said `CobolFloat.ScaledToDouble` and
`ScaledToSingle` build three strings before the parse on their slow path. The note asked for span-based parsing, kept
only if a BenchmarkDotNet witness showed the change mattered.

**Re-probe.** On base 316d0ea82e both slow paths still built `unscaled.ToString() + "E" + (-scale).ToString()`. The
file now lives at `src/WiseOwl.Cobol.Runtime/Values/Numeric/CobolFloat.cs`.

**Witness.** The new `tests/WiseOwl.Cobol.Benchmarks/Numeric/ScaledFloatBenchmarks.cs` uses 64 slow-path operands
(31-digit at scale 25, 18-digit at scale 10, small at scale 30, 38-digit negative at scale 2). It compares the old
construction against the runtime, and its `GlobalSetup` refuses to run unless both give the same bits for every operand.
Release, net11.0, per conversion: binary64 went from 194.3 ns and 182 B to 179.3 ns and 0 B (1.08x faster). Binary32
went from 185.1 ns and 182 B to 157.1 ns and 0 B (1.18x faster). The numbers are in the benchmark README. The run used
BenchmarkDotNet's in-process toolchain. BenchmarkDotNet 0.15.8's out-of-process toolchain stops on net11.0 with
`GetRuntimeVersion not implemented for NotRecognized`. That is reported as a lead and not fixed here.

**Fix.** One private `CobolFloat.ScientificText<T>` writes `<mantissa>E<-scale>` into a 64-character stack buffer for
either 128-bit carrier, and both slow paths parse that span. The sibling sweep (a grep for `+ "E" +` and for
`double|float .Parse(` under `src/WiseOwl.Cobol.Runtime`) found two more copies of the conversion:
- `FloatResultant.ExactMagnitude.NearestBinary64` had its own string twin past the signed carrier. It now calls the new
  `CobolFloat.ScaledMagnitudeToDouble(UInt128, int)`.
- `CobolIntrinsics.NumvalFDouble` parsed an interpolated `[-]digitsEexp` string. It now calls `ScaledToDouble`, the same
  way `NumvalFSingle` already calls `ScaledToSingle`. Both paths of `ScaledToDouble` round once, so the value does not
  change.

`CobolBig.Binary64Magnitude.NearestBinary64` keeps its string, because its BigInteger has no length bound that a stack
buffer could hold.

**Tests.** `ScaledToDoubleTests.TheLongestScientificText_FitsTheBuffer` covers the longest text each carrier can
produce. It was seen to fail with a 50-character buffer (`UnreachableException`). `NumvalFDoubleTests.NumvalFDouble_IsTheParseOfItsText_OnEveryPathOfTheConversion`
compares NUMVAL-F's binary64 bit for bit against one parse of the text, over 6,696 significand/exponent pairs that cross
the fast path, scale 0 and the parse path.

**Neutrality.** The change touches only the runtime. No emitted C# changes, and no inventory row closes
(`closes_rows: []`, with its reason).
