// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a <see cref="BoundIntrinsicCall"/> (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1), dispatching on the
/// catalog row. Three §15.2-type shapes (deep-dive D1):
/// <list type="bullet">
///   <item><b>floating-math</b> (Float rows, §15.4.1 native-arithmetic license) — compute in double, quantize
///         through the ONE <c>CobolIntrinsics.FromDouble</c> at working scale <c>max(TargetScale, 9)</c>: the
///         scale FLOOR is hazard H1 — <c>TargetScale</c> is stale in receiver-less contexts (IF conditions /
///         EVALUATE subjects), and 9 fraction digits leave 9 integer digits in the long, ample for every
///         trig/financial value;</item>
///   <item><b>exact numeric / integer</b> — unscaled Int128 values at a known scale, aligned with the same
///         <see cref="NumericRenderer.Align"/> machinery the arithmetic verbs use (§8.8.1);</item>
///   <item><b>alphanumeric</b> — STATIC string rendering (<see cref="RenderString"/>), reachable from the
///         context-free <see cref="OperandText"/> channel (MOVE-to-alphanumeric, string comparisons, group
///         moves). The static path renders the argument shapes the §15 string functions take (literals, fields,
///         nested calls, simple integer expressions); context-needing shapes (numeric-edited de-edit, computed
///         division) stay LOUD by name — hazard H3 / loud-failure §1.4.</item>
/// </list>
/// A <see cref="IntrinsicBind.Deferred"/> row (catalogued later-edition function with no runtime yet) renders a
/// loud not-implemented guard naming the function — never a wrong value.
/// </summary>
internal sealed class IntrinsicRenderer(EmissionContext ctx, NumericRenderer num)
{
    /// <summary>WHEN-COMPILED's compile-time constant (§15.99.3 r2 — the COMPILATION timestamp, baked into the
    /// generated source as a string literal; injectable via <see cref="StatementBinder.CompileClock"/>, D6).
    /// One capture per process: every unit of a compilation run shares one stamp (§15.99.3 r2's "associated
    /// with the compilation unit").</summary>
    private static readonly Lazy<string> WhenCompiledStamp =
        new(() => CobolDate.Format21(StatementBinder.CompileClock()));

    // ── The numeric channel (COMPUTE / arithmetic / numeric comparisons / MOVE-to-numeric) ──────────────────

    /// <summary>Render a numeric-result intrinsic as a scaled value.</summary>
    public NumX RenderNum(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);
        if (ic.ResultCategory == PicCategory.Alphanumeric)
            return new NumX(EmitText.LoudValue("long", $"alphanumeric FUNCTION {sig.Name} in a numeric context"), 0);

        if (sig.Float) return RenderFloat(ic);

        switch (sig.RuntimeMethod)
        {
            // Integer functions — scale-0 results (§15.2 type 5).
            case "Factorial":                                                   // §15.36 (Int128; 34! overflows → EC default 0)
                return new NumX($"CobolIntrinsics.Factorial({IntArg(ic, 0)})", 0);
            case "SignOf":                                                      // §15.81 — scale-independent sign
                return new NumX($"CobolIntrinsics.SignOf({Arg(ic, 0).Expr})", 0);
            case "Floor":                                                       // §15.44 INTEGER — floor to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX($"CobolIntrinsics.Floor({a.Expr}, {a.Scale})", 0);
            }
            case "Truncate":                                                    // §15.49 INTEGER-PART — truncate to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX($"CobolIntrinsics.Truncate({a.Expr}, {a.Scale})", 0);
            }
            case "AbsScaled":                                                   // §15.7 ABS — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX($"CobolIntrinsics.AbsScaled({a.Expr})", a.Scale);
            }
            case "FractionPart":                                                // §15.42 — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX($"CobolIntrinsics.FractionPart({a.Expr}, {a.Scale})", a.Scale);
            }
            case "ModScaled":                                                   // §15.64 — floored modulus (sign table)
            case "RemScaled":                                                   // §15.77 — truncated remainder
            {
                NumX a = Arg(ic, 0), b = Arg(ic, 1);
                int s = Math.Max(a.Scale, b.Scale);
                return new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({NumericRenderer.Align(a, s)}, {NumericRenderer.Align(b, s)})", s);
            }

            // Variadic statistics over scale-ALIGNED unscaled values (§8.8.1 alignment = value comparison).
            case "MaxScaled" or "MinScaled" or "SumScaled" or "RangeScaled":    // §15.59/63/88/76 — result at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({argList})", s);
            }
            case "MedianScaled" or "MidrangeScaled":                            // §15.61/62 — the /2 is exact at scale s+1 (×10/2)
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({argList})", s + 1);
            }
            case "MeanScaled":                                                  // §15.60 — Σ/n with the ÷ discipline of §8.8.1
            {
                var (argList, s) = AlignedArgs(ic);
                // Quotient quantized at ws = max(TargetScale, s+1, 6): the receiver's scale when known, never
                // below the sum's own resolution + 1, with a fraction floor against stale TargetScale (H1).
                int ws = Math.Max(Math.Max(ctx.TargetScale, s + 1), 6);
                // Same mode rule as NumericRenderer.Divide: AT the receiver scale the one exact RoundDiv applies
                // the receiver's mode; above it, truncate and let the receiver store round once (§14.7.4).
                CobolRounding mode = ws == ctx.TargetScale ? ctx.TargetRounding : CobolRounding.Truncation;
                return new NumX($"CobolNum.Divide(CobolIntrinsics.SumScaled({argList}), {s}, {ic.Args.Count}, 0, {ws}, CobolRounding.{mode})", ws);
            }
            case "OrdMax" or "OrdMin":                                          // §15.71/72 — 1-based ordinal, tie = first
            {
                var (argList, _) = AlignedArgs(ic);
                return new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({argList})", 0);
            }
            case "OrdMaxString" or "OrdMinString":                              // all-alphanumeric argument form
                return new NumX($"CobolIntrinsics.{sig.RuntimeMethod}({StrArgList(ic)})", 0);

            // NUMVAL / NUMVAL-C (§15.67/§15.68): parse to (unscaled, actual scale), rescaled to the compile-time
            // working scale ws = max(TargetScale, 6) — the ≥6 floor is hazard H1's NUMVAL rule (TargetScale is
            // stale in IF conditions; the suite's deepest literal fraction is 5 digits, so 6 is lossless).
            case "Numval":
            {
                int ws = Math.Max(ctx.TargetScale, 6);
                return new NumX($"CobolIntrinsics.Numval({Str(ic.Args[0])}, {ws}{CommaFlag})", ws);
            }
            case "NumvalC":
            {
                int ws = Math.Max(ctx.TargetScale, 6);
                return new NumX($"CobolIntrinsics.NumvalC({Str(ic.Args[0])}, {Str(ic.Args[1])}, {ws}{CommaFlag})", ws);
            }

            case "FindString":                                                  // §15.37 FIND-STRING (2023) — 1-based position of argument-2 in argument-1
                return new NumX($"CobolIntrinsics.FindString({Str(ic.Args[0])}, {Str(ic.Args[1])}, "
                    + $"{(ic.FindLast ? "true" : "false")}, {(ic.Args.Count > 2 ? IntArg(ic, 2) : "0")}, "
                    + $"{(ic.FindAnycase ? "true" : "false")})", 0);
            case "Ord":                                                         // §15.70 — PCS-relative ordinal (H5: weights only when flagged)
                return new NumX($"CobolIntrinsics.Ord({Str(ic.Args[0])}{Collate(ic)})", 0);
            case "Length":                                                      // §15.50 runtime residue (nested string-fn argument)
                return new NumX($"CobolIntrinsics.Length({Str(ic.Args[0])})", 0);

            // Date/time conversions (§15.22/24/46/47; integer date form §15.5.2).
            case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
                return new NumX($"CobolDate.{sig.RuntimeMethod}({IntArg(ic, 0)})", 0);

            default:
                return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (no numeric render recipe)"), 0);
        }
    }

    /// <summary>The §15.4.1 floating-math family: arguments as doubles (through the one scaled→double
    /// conversion), result quantized by the ONE FromDouble at <c>ws = max(TargetScale, 9)</c> (H1/H2).</summary>
    private NumX RenderFloat(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        int ws = Math.Max(ctx.TargetScale, 9);
        string call = sig.RuntimeMethod switch
        {
            // RANDOM (§15.75.3): the no-argument form continues the current sequence; the seeded form restarts it.
            "Random" when ic.Args.Count == 0 => "CobolIntrinsics.Random()",
            "Random" => $"CobolIntrinsics.Random({IntArg(ic, 0)})",
            _ => $"CobolIntrinsics.{sig.RuntimeMethod}({string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => Dbl(ic, i)))})",
        };
        return new NumX($"CobolIntrinsics.FromDouble({call}, {ws})", ws);
    }

    // ── Argument rendering (instance — full NumericRenderer fidelity for the numeric channel) ───────────────

    private NumX Arg(BoundIntrinsicCall ic, int i) => num.AsNum(ic.Args[i]);

    /// <summary>A numeric argument as a C# double (the float family's §15.4.1 carrier).</summary>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(Arg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §15.3; a fractional value is the program's EC latitude).</summary>
    private string IntArg(BoundIntrinsicCall ic, int i)
    {
        NumX a = Arg(ic, i);
        return a.Scale == 0
            ? $"(long)({a.Expr})"
            : $"(long)(CobolNum.Rescale({a.Expr}, {a.Scale}, 0, CobolRounding.Truncation))";
    }

    /// <summary>The variadic arguments aligned to their common scale (ISO §8.8.1 — alignment makes unscaled
    /// comparison/arithmetic equal value comparison/arithmetic), as a C# argument list + that scale.</summary>
    private (string ArgList, int Scale) AlignedArgs(BoundIntrinsicCall ic)
    {
        var xs = ic.Args.Select(num.AsNum).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        return (string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s))), s);
    }

    private string StrArgList(BoundIntrinsicCall ic) => string.Join(", ", ic.Args.Select(Str));

    /// <summary>A string-kind argument in the NUMERIC channel: literals, field images, and nested alphanumeric
    /// intrinsics — the §15.3 alphanumeric-argument shapes. (Same coverage as the static channel; the shared
    /// static helper keeps it ONE implementation.)</summary>
    private static string Str(BoundOperand op) => StrStatic(op);

    private string CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The trailing weights argument for a PCS-flagged CHAR/ORD (hazard H5: the binder set
    /// <see cref="BoundIntrinsicCall.Collate"/> ONLY when a non-identity PCS exists — exactly when the program
    /// class emitted its <c>__COLLATE</c> table).</summary>
    private static string Collate(BoundIntrinsicCall ic) => ic.Collate ? ", __COLLATE" : "";

    // ── The STRING channel (static — reachable from OperandText with no emission context) ───────────────────

    /// <summary>Render an alphanumeric-result intrinsic as a C# string expression. Static by design: the
    /// MOVE-to-alphanumeric / string-comparison / group-move channels reach intrinsics through the context-free
    /// <see cref="OperandText.AsString"/>; everything these functions need is either in the bound node or
    /// compile-time constant. Channels that would need instance context stay loud by name (H3).</summary>
    public static string RenderString(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return EmitText.LoudValue("string", $"FUNCTION {sig.Name} (catalogued, not yet implemented)");
        return sig.RuntimeMethod switch
        {
            "UpperCase" or "LowerCase" or "Reverse" =>                         // §15.97/57/78 — length-preserving
                $"CobolIntrinsics.{sig.RuntimeMethod}({StrStatic(ic.Args[0])})",
            "Char" =>                                                          // §15.15 — PCS-relative (H5 conditional weights)
                $"CobolIntrinsics.Char({IntStatic(ic.Args[0])}{Collate(ic)})",
            "CurrentDate" => "CobolDate.CurrentDate()",                        // §15.21 — the runtime clock
            // WHEN-COMPILED is the COMPILATION timestamp (§15.99.3 r2) — a constant in the generated source.
            // (The legacy's runtime-clock placeholder also passes IF142A's plausibility checks; the constant is
            // the spec-correct form — scout brief §4.4.)
            "WhenCompiled" => EmitText.CsLiteral(WhenCompiledStamp.Value),
            "MaxString" or "MinString" =>                                      // §15.59/63 all-alphanumeric form
                $"CobolIntrinsics.{sig.RuntimeMethod}({string.Join(", ", ic.Args.Select(StrStatic))})",
            "Concat" =>                                                        // §15.18 — concatenate all argument images (2023)
                $"CobolIntrinsics.Concat({string.Join(", ", ic.Args.Select(StrStatic))})",
            "BaseConvert" =>                                                   // §15.12 — unsigned-integer base conversion (2023)
                $"CobolIntrinsics.BaseConvert({StrStatic(ic.Args[0])}, {IntStatic(ic.Args[1])}, {IntStatic(ic.Args[2])})",
            "Trim" =>                                                          // §15.96 — delete leading/trailing/both of the char set (default: space)
                $"CobolIntrinsics.Trim({StrStatic(ic.Args[0])}, {ic.TrimMode}"
                    + string.Concat(ic.Args.Skip(1).Select(a => $", {StrStatic(a)}")) + ")",
            "Substitute" => RenderSubstitute(ic),                              // §15.87 — replace argument-2 pairs (2023)
            "Convert" =>                                                       // §15.19 — repertoire / hex / byte conversion (2023)
                $"CobolIntrinsics.Convert({StrStatic(ic.Args[0])}, {ic.ConvertSource}, {ic.ConvertDest}, "
                    + $"{(ic.ConvertDestHex ? "true" : "false")})",
            // The last-exception interrogation functions (§15.28/30/32/33 — the EC model): zero-argument reads
            // of the runtime register; the binder's EcNoteFunction flagged the group EC gate, so the generated
            // source carries the CobolNet.Runtime.Exceptions using.
            "EcStatus" => "EcFunctions.Status()",                              // §15.33
            "EcLocation" => "EcFunctions.Location()",                          // §15.30
            "EcStatement" => "EcFunctions.Statement()",                        // §15.32
            "EcFile" => ic.Args.Count == 0
                ? "EcFunctions.File()"                                         // §15.28.4 r1 — the no-argument form
                : EmitText.LoudValue("string", "FUNCTION EXCEPTION-FILE(file-connector-name) (the 2023 optional-argument form — VCR row 68)"),
            _ => EmitText.LoudValue("string", $"FUNCTION {sig.Name} in a string context"),
        };
    }

    /// <summary>SUBSTITUTE (§15.87): the source (Args[0]) plus parallel from/to/mode arrays over the pair operands
    /// (Args[1..] taken two at a time; one <see cref="BoundIntrinsicCall.SubstituteModes"/> entry per pair).</summary>
    private static string RenderSubstitute(BoundIntrinsicCall ic)
    {
        var froms = new List<string>();
        var tos = new List<string>();
        for (int i = 1; i + 1 < ic.Args.Count; i += 2)
        {
            froms.Add(StrStatic(ic.Args[i]));
            tos.Add(StrStatic(ic.Args[i + 1]));
        }
        return $"CobolIntrinsics.Substitute({StrStatic(ic.Args[0])}, "
            + $"new string[] {{ {string.Join(", ", froms)} }}, "
            + $"new string[] {{ {string.Join(", ", tos)} }}, "
            + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }})";
    }

    /// <summary>A string-kind argument, context-free: a literal, a field's display image (the static
    /// <see cref="OperandText.AsString"/> shapes), or a nested alphanumeric intrinsic. A NUMERIC-result operand
    /// in a string-argument position stays loud (H3 — the named uncovered channel).</summary>
    private static string StrStatic(BoundOperand op) => op switch
    {
        BoundStringLiteral s => EmitText.CsLiteral(s.Value),
        BoundFieldOperand => OperandText.AsString(op),
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric } nested } =>
            RenderString(nested),
        BoundOperandError e => EmitText.LoudValue("string", e.Feature),
        _ => EmitText.LoudValue("string", $"intrinsic string argument '{op.GetType().Name}'"),
    };

    /// <summary>An integer-kind argument, context-free (the static CHAR channel): literals, plain field reads
    /// (through the ONE <see cref="NumericRenderer.FieldNumCore"/>), index-names, and the +/−/× integer
    /// expression subset (scale alignment is static). Division and nested numeric intrinsics need the emission
    /// context — loud by name (H3).</summary>
    private static string IntStatic(BoundOperand op)
    {
        NumX a = NumStatic(op);
        return a.Scale == 0
            ? $"(long)({a.Expr})"
            : $"(long)(CobolNum.Rescale({a.Expr}, {a.Scale}, 0, CobolRounding.Truncation))";
    }

    private static NumX NumStatic(BoundOperand op) => op switch
    {
        BoundNumericLiteral n => EmitText.UnscaledLit(n.Text),
        BoundFieldOperand f => NumericRenderer.FieldNumCore(f.Place),
        BoundComputedOperand c => NumStaticExpr(c.Expr),
        BoundOperandError e => new NumX(EmitText.LoudValue("long", e.Feature), 0),
        _ => new NumX(EmitText.LoudValue("long", $"intrinsic numeric argument '{op.GetType().Name}'"), 0),
    };

    private static NumX NumStaticExpr(BoundExpr e) => e switch
    {
        BoundNumLiteral l => EmitText.UnscaledLit(l.Text),
        BoundNumRef r => NumericRenderer.FieldNumCore(r.Place),
        BoundIndexRef ix => new NumX(ix.IndexField, 0),
        BoundNegate n => NumStaticExpr(n.Operand) is var x ? new NumX($"(-({x.Expr}))", x.Scale) : default,
        BoundBinary { Op: '+' or '-' } b => StaticAdditive(NumStaticExpr(b.Left), b.Op, NumStaticExpr(b.Right)),
        BoundBinary { Op: '*' } b => StaticMul(NumStaticExpr(b.Left), NumStaticExpr(b.Right)),
        _ => new NumX(EmitText.LoudValue("long", $"computed intrinsic argument '{e.GetType().Name}' in a string-context call"), 0),
    };

    private static NumX StaticAdditive(NumX a, char op, NumX b)
    {
        int s = Math.Max(a.Scale, b.Scale);
        return new NumX($"((Int128)({NumericRenderer.Align(a, s)}) {op} ({NumericRenderer.Align(b, s)}))", s);
    }

    private static NumX StaticMul(NumX a, NumX b) =>
        new($"((Int128)({a.Expr}) * ({b.Expr}))", a.Scale + b.Scale);
}
