// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Runtime;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a <see cref="BoundIntrinsicCall"/> (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1), dispatching on the
/// catalog row — the SINGLE render channel (P7 Step 12: the former parallel static evaluator is deleted;
/// DESIGN-codegen-backend §2.5). Three §15.2-type shapes (deep-dive D1):
/// <list type="bullet">
///   <item><b>floating-math</b> (Float rows, §15.4.1 native-arithmetic license) — compute in double, quantize
///         through the ONE <c>CobolIntrinsics.FromDouble</c> at working scale <c>max(Receiver.Scale, 9)</c>: the
///         scale FLOOR covers receiver-less contexts, which render at scale 0 (P7.3 <c>ReceiverContext.None</c> — IF conditions /
///         EVALUATE subjects), and 9 fraction digits leave 9 integer digits in the long, ample for every
///         trig/financial value;</item>
///   <item><b>exact numeric / integer</b> — unscaled Int128 values at a known scale, aligned with the same
///         <see cref="NumericRenderer.Align"/> machinery the arithmetic verbs use (§8.8.1);</item>
///   <item><b>alphanumeric</b> — the INSTANCE string channel (<see cref="RenderString"/>, reached from
///         <see cref="OperandText.AsString"/> with the per-unit renderer): argument shapes render through the
///         ONE <see cref="NumericRenderer"/> under the default receiver (<see cref="ReceiverContext.None"/> —
///         a string-channel call has no numeric receiver), so compound arithmetic, division, float items, and
///         numeric-edited de-edits all render — the former static channel's H3 loud channels are gone.</item>
/// </list>
/// Every runtime-member fragment routes through <see cref="RuntimeApi"/> (the Step-4b façade; the ratchet's
/// IntrinsicRenderer whitelist entry is deleted). A <see cref="IntrinsicBind.Deferred"/> row (catalogued
/// later-edition function with no runtime yet) renders a loud not-implemented guard naming the function —
/// never a wrong value.
/// </summary>
internal sealed class IntrinsicRenderer(EmitContext ctx, NumericRenderer num)
{
    /// <summary>The per-unit expression renderer, surfaced for the nested argument visitor (a primary-ctor
    /// capture is not reachable through an instance reference).</summary>
    private NumericRenderer Num => num;

    /// <summary>WHEN-COMPILED's compile-time constant (§15.99.3 r2 — the COMPILATION timestamp, baked into the
    /// generated source as a string literal; injectable via <see cref="Binding.Procedure.IntrinsicBinder.CompileClock"/>, D6).
    /// One capture per process: every unit of a compilation run shares one stamp (§15.99.3 r2's "associated
    /// with the compilation unit").</summary>
    private static readonly Lazy<string> WhenCompiledStamp =
        new(() => RuntimeApi.DateFormat21(Binding.Procedure.IntrinsicBinder.CompileClock()));

    // ── The numeric channel (COMPUTE / arithmetic / numeric comparisons / MOVE-to-numeric) ──────────────────

    /// <summary>Render a numeric-result intrinsic as a scaled value.</summary>
    public NumX RenderNum(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);
        if (ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National)
            return new NumX(EmitText.LoudValue("long", $"string-class FUNCTION {sig.Name} in a numeric context"), 0);

        if (sig.Float) return RenderFloat(ic);

        switch (sig.RuntimeMethod)
        {
            // Integer functions — scale-0 results (§15.2 type 5).
            case "Factorial":                                                   // §15.36 (Int128; 34! overflows → EC default 0)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, IntArg(ic, 0)), 0);
            case "SignOf":                                                      // §15.81 — scale-independent sign
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Arg(ic, 0).Expr), 0);
            case "Floor":                                                       // §15.44 INTEGER — floor to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), 0);
            }
            case "Truncate":                                                    // §15.49 INTEGER-PART — truncate to scale 0
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), 0);
            }
            case "AbsScaled":                                                   // §15.7 ABS — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, a.Expr), a.Scale);
            }
            case "FractionPart":                                                // §15.42 — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{a.Expr}, {a.Scale}"), a.Scale);
            }
            case "ModScaled":                                                   // §15.64 — floored modulus (sign table)
            case "RemScaled":                                                   // §15.77 — truncated remainder
            {
                NumX a = Arg(ic, 0), b = Arg(ic, 1);
                int s = Math.Max(a.Scale, b.Scale);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{NumericRenderer.Align(a, s)}, {NumericRenderer.Align(b, s)}"), s);
            }

            // Variadic statistics over scale-ALIGNED unscaled values (§8.8.1 alignment = value comparison).
            case "MaxScaled" or "MinScaled" or "SumScaled" or "RangeScaled":    // §15.59/63/88/76 — result at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, argList), s);
            }
            case "MedianScaled" or "MidrangeScaled":                            // §15.61/62 — the /2 is exact at scale s+1 (×10/2)
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, argList), s + 1);
            }
            case "MeanScaled":                                                  // §15.60 — Σ/n with the ÷ discipline of §8.8.1
            {
                var (argList, s) = AlignedArgs(ic);
                // Quotient quantized at ws = max(Receiver.Scale, s+1, 6): the receiver's scale when known, never
                // below the sum's own resolution + 1, with a fraction floor for receiver-less (scale-0) contexts.
                int ws = Math.Max(Math.Max(num.Receiver.Scale, s + 1), 6);
                // Same mode rule as NumericRenderer.Divide: AT the receiver scale the one exact RoundDiv applies
                // the receiver's mode; above it, truncate and let the receiver store round once (§14.7.4).
                CobolRounding mode = ws == num.Receiver.Scale ? num.Receiver.Rounding : CobolRounding.Truncation;
                return new NumX(RuntimeApi.NumDivide(false, RuntimeApi.Intrinsic("SumScaled", argList),
                    s.ToString(), ic.Args.Count.ToString(), "0", ws.ToString(), mode), ws);
            }
            case "OrdMax" or "OrdMin":                                          // §15.71/72 — 1-based ordinal, tie = first
            {
                var (argList, _) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, argList), 0);
            }
            case "OrdMaxString" or "OrdMinString":                              // all-alphanumeric argument form
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)), 0);

            // NUMVAL / NUMVAL-C (§15.67/§15.68): parse to (unscaled, actual scale), rescaled to the compile-time
            // working scale ws = max(Receiver.Scale, 6) — the ≥6 floor is the NUMVAL rule (the receiver scale is
            // stale in IF conditions; the suite's deepest literal fraction is 5 digits, so 6 is lossless).
            case "Numval":
            {
                int ws = Math.Max(num.Receiver.Scale, 6);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}"), ws);
            }
            case "NumvalC":
            {
                int ws = Math.Max(num.Receiver.Scale, 6);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {ws}{CommaFlag}"), ws);
            }

            case "FindString":                                                  // §15.37 FIND-STRING (2023) — 1-based position of argument-2 in argument-1
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {(ic.FindLast ? "true" : "false")}, "
                    + $"{(ic.Args.Count > 2 ? IntArg(ic, 2) : "0")}, {(ic.FindAnycase ? "true" : "false")}"), 0);
            case "Ord":                                                         // §15.70 — PCS-relative ordinal (H5: weights only when flagged)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{Collate(ic)}"), 0);
            case "Length":                                                      // §15.50 runtime residue (nested string-fn argument)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);

            // Date/time conversions (§15.22/24/46/47; integer date form §15.5.2).
            case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, IntArg(ic, 0)), 0);

            // ── The COBOL-2014 date/time + number family (§15.17/48/79/92; §15.69/95) ───────────────────────────
            case "CombinedDatetime":                                            // §15.17 — a1 + a2/100000
            {
                NumX t = Arg(ic, 1);
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{IntArg(ic, 0)}, {t.Expr}, {t.Scale}"), t.Scale + 5);
            }
            case "IntegerOfFormattedDate":                                       // §15.48 — analyze a2 per format a1 → integer date
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}"), 0);
            case "SecondsFromFormattedTime":                                    // §15.79 — result scale = the format's fractional-second count
            {
                if (ic.Args[0] is not BoundStringLiteral fmt)
                    return new NumX(EmitText.LoudValue("long",
                        "FUNCTION SECONDS-FROM-FORMATTED-TIME requires a literal time format (§15.79.3 r1)"), 0);
                int fsc = RuntimeApi.DateFormatFractionDigits(fmt.Value);       // compile-time — the ONE format analyzer
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {fsc}"), fsc);
            }
            case "TestFormattedDatetime":                                       // §15.92 — 0 (valid) or the 1-based error position
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}"), 0);
            case "NumvalF":                                                      // §15.69 — floating NUMVAL; ws floor 9 (the float-family precedent)
            {
                int ws = Math.Max(num.Receiver.Scale, 9);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}"), ws);
            }
            case "TestNumvalF":                                                 // §15.95 — 0 / first-error position / LENGTH+1
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{CommaFlag}"), 0);

            default:
                return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (no numeric render recipe)"), 0);
        }
    }

    /// <summary>The §15.4.1 floating-math family: arguments as doubles (through the one scaled→double
    /// conversion), result quantized by the ONE FromDouble at <c>ws = max(Receiver.Scale, 9)</c> (the ≥9 float floor).</summary>
    private NumX RenderFloat(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        int ws = Math.Max(num.Receiver.Scale, 9);
        string call = sig.RuntimeMethod switch
        {
            // RANDOM (§15.75.3): the no-argument form continues the current sequence; the seeded form restarts it.
            "Random" when ic.Args.Count == 0 => RuntimeApi.Intrinsic(sig.RuntimeMethod, ""),
            "Random" => RuntimeApi.Intrinsic(sig.RuntimeMethod, IntArg(ic, 0)),
            _ => RuntimeApi.Intrinsic(sig.RuntimeMethod,
                string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => Dbl(ic, i)))),
        };
        // A float RECEIVER keeps the transcendental result in the binary64 pipeline (full precision — SQRT(2) into a
        // COMP-2 is 1.4142135623730951, not the scale-9 1.414213562); a fixed receiver quantizes through FromDouble
        // at the working scale (the established behavior the intrinsic goldens encode). (D16 review finding.)
        return num.Receiver.Real
            ? new NumX(call, 0, Real: true)
            : new NumX(RuntimeApi.Intrinsic("FromDouble", $"{call}, {ws}"), ws);
    }

    // ── Argument rendering (the ONE NumericRenderer for every numeric-kind argument) ────────────────────────

    private NumX Arg(BoundIntrinsicCall ic, int i) => num.AsNum(ic.Args[i], num.Receiver);

    /// <summary>A numeric argument as a C# double (the float family's §15.4.1 carrier).</summary>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(Arg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §15.3; a fractional value is the program's EC latitude).</summary>
    private string IntArg(BoundIntrinsicCall ic, int i) => AsInt(Arg(ic, i));

    private static string AsInt(NumX a) => a.Scale == 0
        ? $"(long)({a.Expr})"
        : $"(long)({RuntimeApi.NumRescale(a.Expr, a.Scale.ToString(), "0", CobolRounding.Truncation)})";

    /// <summary>The variadic arguments aligned to their common scale (ISO §8.8.1 — alignment makes unscaled
    /// comparison/arithmetic equal value comparison/arithmetic), as a C# argument list + that scale.</summary>
    private (string ArgList, int Scale) AlignedArgs(BoundIntrinsicCall ic)
    {
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        return (string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s))), s);
    }

    private string StrArgList(BoundIntrinsicCall ic) => string.Join(", ", ic.Args.Select(Str));

    private string CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The trailing weights argument for a PCS-flagged CHAR/ORD (hazard H5: the binder set
    /// <see cref="BoundIntrinsicCall.Collate"/> ONLY when a non-identity PCS exists — exactly when the program
    /// class emitted its <c>__COLLATE</c> table).</summary>
    private static string Collate(BoundIntrinsicCall ic) => ic.Collate ? ", __COLLATE" : "";

    // ── The STRING channel (instance — reached through OperandText.AsString with the per-unit renderer) ─────

    /// <summary>Render an alphanumeric-result intrinsic as a C# string expression. An INSTANCE channel (P7
    /// Step 12 — the context-free static twin is deleted): numeric-kind arguments render through the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> (a string-channel call has no
    /// numeric receiver), so division, float items, nested numeric intrinsics, and numeric-edited de-edits
    /// all render where the static channel stayed loud (H3 closed).</summary>
    public string RenderString(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0)
            return EmitText.LoudValue("string", $"FUNCTION {sig.Name} (catalogued, not yet implemented)");
        return sig.RuntimeMethod switch
        {
            "UpperCase" or "LowerCase" or "Reverse" =>                         // §15.97/57/78 — length-preserving
                RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])),
            "Char" =>                                                          // §15.15 — PCS-relative (H5 conditional weights)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CurrentDate" => RuntimeApi.DateFn(sig.RuntimeMethod, ""),         // §15.21 — the runtime clock
            // WHEN-COMPILED is the COMPILATION timestamp (§15.99.3 r2) — a constant in the generated source.
            // (The legacy's runtime-clock placeholder also passes IF142A's plausibility checks; the constant is
            // the spec-correct form — scout brief §4.4.)
            "WhenCompiled" => EmitText.CsLiteral(WhenCompiledStamp.Value),
            "MaxString" or "MinString" =>                                      // §15.59/63 all-alphanumeric form
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            "Concat" =>                                                        // §15.18 — concatenate all argument images (2023)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            // DISPLAY-OF (§15.26) / NATIONAL-OF (§15.66) — the national↔alphanumeric repertoire pair (2002);
            // the optional argument-2 is the one-character substitution string (§15.26.3 r2 / §15.66.3 r2).
            "DisplayOf" or "NationalOf" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            "BaseConvert" =>                                                   // §15.12 — unsigned-integer base conversion (2023)
                RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}, {ArgInt(ic.Args[2])}"),
            "Trim" =>                                                          // §15.96 — delete leading/trailing/both of the char set (default: space)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ic.TrimMode}"
                    + string.Concat(ic.Args.Skip(1).Select(a => $", {Str(a)}"))),
            "Substitute" => RenderSubstitute(ic),                              // §15.87 — replace argument-2 pairs (2023)
            "Convert" =>                                                       // §15.19 — repertoire / hex / byte conversion (2023)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ic.ConvertSource}, {ic.ConvertDest}, "
                    + $"{(ic.ConvertDestHex ? "true" : "false")}"),
            "ModuleName" =>                                                    // §15.65 — the runtime module call-name stack (2023)
                RuntimeApi.ModuleNameFn(ic.ModuleNameKind),
            "FormattedDate" =>                                                 // §15.39 — integer date per a date format (2014)
                RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            "FormattedTime" => RenderFormattedTime(ic),                        // §15.41
            "FormattedDatetime" => RenderFormattedDatetime(ic),               // §15.40
            "FormattedCurrentDate" =>                                          // §15.38 — the runtime clock per a combined format
                RuntimeApi.DateFn(sig.RuntimeMethod, Str(ic.Args[0])),
            // The last-exception interrogation functions (§15.28/30/32/33 — the EC model): zero-argument reads
            // of the runtime register; the binder's EcNoteFunction flagged the group EC gate, so the generated
            // source carries the CobolNet.Runtime.Exceptions using.
            "EcStatus" => RuntimeApi.EcFn("Status"),                           // §15.33
            "EcLocation" => RuntimeApi.EcFn("Location"),                       // §15.30
            "EcStatement" => RuntimeApi.EcFn("Statement"),                     // §15.32
            "EcFile" => ic.Args.Count == 0
                ? RuntimeApi.EcFn("File")                                      // §15.28.4 r1 — the no-argument form
                : EmitText.LoudValue("string", "FUNCTION EXCEPTION-FILE(file-connector-name) (the 2023 optional-argument form — VCR row 68)"),
            _ => EmitText.LoudValue("string", $"FUNCTION {sig.Name} in a string context"),
        };
    }

    /// <summary>FORMATTED-TIME (§15.41): seconds as (unscaled long, scale) — NOT the truncating ArgInt, so the
    /// fractional seconds survive — plus the optional offset-minutes (a3).</summary>
    private string RenderFormattedTime(BoundIntrinsicCall ic)
    {
        NumX sec = ArgNum(ic.Args[1]);
        bool hasOff = ic.Args.Count > 2;
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, (long)({sec.Expr}), {sec.Scale}, "
             + $"{(hasOff ? ArgInt(ic.Args[2]) : "0")}, {(hasOff ? "true" : "false")}");
    }

    /// <summary>FORMATTED-DATETIME (§15.40): integer date a2 + seconds a3 (unscaled/scale) + the optional
    /// offset-minutes a4.</summary>
    private string RenderFormattedDatetime(BoundIntrinsicCall ic)
    {
        NumX sec = ArgNum(ic.Args[2]);
        bool hasOff = ic.Args.Count > 3;
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}, "
             + $"(long)({sec.Expr}), {sec.Scale}, {(hasOff ? ArgInt(ic.Args[3]) : "0")}, {(hasOff ? "true" : "false")}");
    }

    /// <summary>SUBSTITUTE (§15.87): the source (Args[0]) plus parallel from/to/mode arrays over the pair operands
    /// (Args[1..] taken two at a time; one <see cref="BoundIntrinsicCall.SubstituteModes"/> entry per pair).</summary>
    private string RenderSubstitute(BoundIntrinsicCall ic)
    {
        var froms = new List<string>();
        var tos = new List<string>();
        for (int i = 1; i + 1 < ic.Args.Count; i += 2)
        {
            froms.Add(Str(ic.Args[i]));
            tos.Add(Str(ic.Args[i + 1]));
        }
        return RuntimeApi.Intrinsic(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, "
            + $"new string[] {{ {string.Join(", ", froms)} }}, "
            + $"new string[] {{ {string.Join(", ", tos)} }}, "
            + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}");
    }

    // ── String-channel argument rendering (P7 Step 12 — the ONE NumericRenderer under the default receiver) ──

    /// <summary>A numeric-kind argument inside the STRING channel, rendered by the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> — a string-channel render has no
    /// numeric receiver, exactly the Step-3 default-receiver convention (DESIGN-codegen-backend §2.5). The
    /// renderer's public entries save/restore the ambient receiver, so this re-entrant call cannot go stale.</summary>
    private NumX ArgNum(BoundOperand op) => num.AsNum(op, ReceiverContext.None);

    /// <summary>An integer-kind argument inside the STRING channel (CHAR / BASE-CONVERT / the formatted
    /// date-time offsets) — the same (long) truncation the numeric channel's <see cref="IntArg"/> applies.</summary>
    private string ArgInt(BoundOperand op) => AsInt(ArgNum(op));

    /// <summary>A string-kind argument (the §15.3 alphanumeric-argument shapes): literals, field display
    /// images, and nested alphanumeric intrinsics. A numeric-category operand in a string-argument position
    /// stays loud — §15's string functions take alphanumeric/national/boolean arguments (the named channel).</summary>
    private string Str(BoundOperand op) => op.Accept(_strArg ??= new StrArgVisitor(this));
    private StrArgVisitor? _strArg;

    /// <summary>The string-argument dispatch — an exhaustive generated-visitor implementation (a new
    /// <see cref="BoundOperand"/> leaf is a compile error), INSTANCE-bound so the nested-intrinsic arm
    /// re-enters the instance <see cref="RenderString"/>.</summary>
    private sealed class StrArgVisitor(IntrinsicRenderer owner) : IBoundOperandVisitor<string>
    {
        private static string Loud(BoundOperand n) => EmitText.LoudValue("string", $"intrinsic string argument '{n.GetType().Name}'");
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        public string Visit(BoundFieldOperand n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } nested
                ? owner.RenderString(nested) : Loud(n);   // string-class results incl. national (NATIONAL-OF §15.66)
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        public string Visit(BoundNumericLiteral n) => Loud(n);
        public string Visit(BoundFigurative n) => Loud(n);
        public string Visit(BoundAllLiteral n) => Loud(n);
        public string Visit(BoundBoolOperand n) => Loud(n);
    }
}
