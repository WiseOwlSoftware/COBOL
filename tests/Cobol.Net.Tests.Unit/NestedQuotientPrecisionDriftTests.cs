// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PRECISION OF A NESTED NATIVE INTERMEDIATE DOES NOT DEPEND ON THE EXPRESSION'S DEPTH (kb/Work PB3538; numeric
/// design D2; CONFORMANCE.md DOC-A.1-123). <c>NumericRenderer.Divide</c> gave a nested quotient
/// <c>max(receiver, a.Scale, b.Scale) + 14</c> guard digits, and the next division's <c>a.Scale</c> IS that quotient's
/// scale, so the working scale compounded 14, 28, 42 with depth until `COMPUTE R = A / B / B / B / B` (0.5) and
/// `(A / B) * (A / B) * (A / B)` (64) left the Int128 carrier and ended the run unit with EC-SIZE-OVERFLOW. A nested
/// quotient's working scale now derives from its operands' PRECISION (<c>NumX.FractionPrecision</c>: the fraction digits
/// their operands carry, never the guard digits a quotient added), and every exact arm above a quotient (sum, difference,
/// product, negation, power) carries that precision as it carries the scale.
/// <para>This class reads the EMITTED C#: for each expression shape that crosses a quotient (a division chain, a product,
/// a sum and a negation of quotients, a quotient over a scaled leaf, a receiver-less relation), compiled at depth 2 and
/// at depth 16 into receivers of scale 0, 4 and 18, the set of result scales the truncating division kernels are asked for
/// (the fifth argument of <c>CobolNum.Divide</c> / <c>DivideOrThrow</c> and of the wide form's <c>CobolWide.DivTruncated</c>)
/// is the same at both depths, and none passes the carrier's 38 digits. A new arm that derives a scale from an intermediate's scale makes the depth-16 set grow and
/// turns this red even where no golden crosses it; the goldens
/// <c>conformance-suite/cases/8.8.1.2-Native-standard-binary-and-standard-decimal/pb3538_*</c> pin the values.</para>
/// <para>⛔ AND THE LANE NEVER CHANGES THE RULE (review finding R1-1): a dividend of unknown digit bound (an intrinsic's
/// value) or a wide product passes the Int128 lane's bound, and its nested quotient is the same truncated quotient on
/// the exact wide form (<c>CobolWide.DivTruncated</c>), never the SDIDI's round-to-odd <c>CobolDec.DivToOdd</c>, which
/// made `FUNCTION INTEGER(X) / 7 * 7` 2.000000 where `X / 7 * 7` is 1.999999.</para>
/// </summary>
public sealed class NestedQuotientPrecisionDriftTests
{
    private static string Program(int depth)
    {
        string Chain(string head, string op, string tail) =>
            head + string.Concat(Enumerable.Repeat($" {op} {tail}", depth - 1));
        var sb = new StringBuilder();
        sb.AppendLine("IDENTIFICATION DIVISION.");
        sb.AppendLine($"PROGRAM-ID. PB3538D{depth}.");
        sb.AppendLine("DATA DIVISION.");
        sb.AppendLine("WORKING-STORAGE SECTION.");
        sb.AppendLine("01 A   PIC 9 VALUE 8.");
        sb.AppendLine("01 B   PIC 9 VALUE 2.");
        sb.AppendLine("01 F   PIC 9V9(18) VALUE 1.5.");
        sb.AppendLine("01 R0  PIC 9(4).");
        sb.AppendLine("01 R4  PIC 9V9(4).");
        sb.AppendLine("01 R18 PIC 9V9(18).");
        sb.AppendLine("PROCEDURE DIVISION.");
        sb.AppendLine("MAIN.");
        foreach (string r in new[] { "R0", "R4", "R18" })
        {
            sb.AppendLine($"    COMPUTE {r} = {Chain("A / B", "/", "B")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("(A / B)", "*", "(A / B)")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("(A / B)", "+", "(A / B)")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("(- (A / B))", "/", "B")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("F / B", "/", "B")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("(F / B)", "*", "(F / B)")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("FUNCTION INTEGER(A) / B", "/", "B")}.");
            sb.AppendLine($"    COMPUTE {r} = {Chain("(FUNCTION MOD(A 9) * A / B)", "*", "(A / B)")}.");
        }
        sb.AppendLine($"    IF {Chain("A / B", "/", "B")} > 0 DISPLAY \"GT\" END-IF.");
        sb.AppendLine("    STOP RUN.");
        return sb.ToString();
    }

    private static string EmitCSharp(string source)
    {
        string dir = Directory.CreateTempSubdirectory("pb3538drift").FullName;
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Free));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }

    /// <summary>The fifth argument (the result scale) of every truncating division kernel call in <paramref name="cs"/>,
    /// each kernel with its argument count.</summary>
    private static SortedSet<int> DivisionScales(string cs)
    {
        var scales = new SortedSet<int>();
        foreach (var (call, arity) in new[] { ("CobolNum.Divide(", 6), ("CobolNum.DivideOrThrow(", 6), ("CobolWide.DivTruncated(", 5) })
            for (int at = cs.IndexOf(call, StringComparison.Ordinal); at >= 0; at = cs.IndexOf(call, at + 1, StringComparison.Ordinal))
            {
                var args = new List<string>();
                int depth = 0, start = at + call.Length;
                for (int i = start; i < cs.Length; i++)
                {
                    char c = cs[i];
                    if (c is '(' or '[' or '{') depth++;
                    else if (c is ')' or ']' or '}' && depth > 0) depth--;
                    else if ((c == ',' && depth == 0) || (c == ')' && depth == 0))
                    {
                        args.Add(cs[start..i].Trim());
                        start = i + 1;
                        if (c == ')') break;
                    }
                }
                Assert.True(args.Count == arity, $"a {call} call with {args.Count} arguments: the kernel's shape changed");
                scales.Add(int.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture));
            }
        return scales;
    }

    [Fact]
    public void ANestedQuotientsScale_IsTheSameAtDepth2AndDepth16()
    {
        SortedSet<int> shallow = DivisionScales(EmitCSharp(Program(2)));
        SortedSet<int> deep = DivisionScales(EmitCSharp(Program(16)));
        Assert.NotEmpty(shallow);
        Assert.True(shallow.SetEquals(deep),
            $"the scaled division kernel is asked for scales {{{string.Join(", ", deep)}}} at depth 16 but "
            + $"{{{string.Join(", ", shallow)}}} at depth 2: a nested intermediate's scale is being derived from an "
            + "intermediate's scale (kb/Work PB3538) instead of from its operands' precision (NumX.FractionPrecision)");
        Assert.True(deep.Max <= 38, $"a division result scale {deep.Max} passes the Int128 carrier's 38 digits");
    }

    [Fact]
    public void ANestedQuotientPastTheInt128Lane_IsTheTruncatedQuotientOnTheWideForm()
    {
        // Every quotient here is NESTED (the outermost operation is a product or a relation), so none is the final
        // transfer whose one rounding may take the SDIDI's round-to-odd form.
        string cs = EmitCSharp("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3538W.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A   PIC 9 VALUE 2.
            01 B   PIC 9 VALUE 7.
            01 R4  PIC S9(9)V9(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE R4 = FUNCTION INTEGER(A) / B * B.
                COMPUTE R4 = FUNCTION MOD(A 9) / B / B * B * B.
                COMPUTE R4 = (FUNCTION ORD-MAX(A B) * A) / B * B.
                IF FUNCTION INTEGER(A) / B * B < 2 DISPLAY "LT" END-IF.
                STOP RUN.
            """);
        Assert.Contains("CobolWide.DivTruncated(", cs);
        Assert.DoesNotContain("CobolDec.DivToOdd(", cs);
    }
}
