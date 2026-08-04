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
///   <item><b>floating-math</b> (Float rows, §15.4.1 native-arithmetic license) — compute in double; a
///         FIXED-POINT receiver then quantizes through the ONE <c>CobolIntrinsics.FromDouble</c> at
///         <see cref="ReceiverContext.FloatWorkingScale"/> (the ≥9 float floor CAPPED at the receiver's Int128
///         headroom, so a saturation can never store silently — PB13), while a FLOAT receiver and a
///         RECEIVER-LESS context both keep the binary64 value: §15.4.1 leaves the returned value's
///         representation to the implementor, and with no receiver there is no scale to quantize TO
///         (P7.3 <c>ReceiverContext.None</c> — IF conditions / EVALUATE subjects / DISPLAY operands);</item>
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
        if (ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
            return new NumX(EmitText.LoudValue("long", $"string-class FUNCTION {sig.Name} in a numeric context"), 0);

        // ⛔ ROUTE ON THE ARGUMENT'S TYPE, NOT ONLY THE FUNCTION'S FAMILY (fix-queue PB2). The exact family
        // computes over scale-aligned Int128, which is right (deep-dive D1) — but a FLOATING-POINT argument is
        // legal for all of it (§15.7.3 r1 and siblings require class NUMERIC, and a COMP-2 item is class
        // numeric), and dispatching on sig.Float alone handed a double expression to an Int128 parameter. The
        // user saw the generated C# fail: "CS1503: cannot convert from 'double' to 'System.Int128'" — an
        // internal failure escaping as a backend error on legal source. Ten of eleven functions probed did it.
        // Under native arithmetic §15.4.1 makes the returned value implementor-defined and each of these
        // functions is an equivalent arithmetic expression over its own operands, so once an argument arrives as
        // binary64 the EAE *is* a binary64 evaluation — nothing exact is surrendered, because there was nothing
        // exact left to keep. The real-argument bodies are CobolIntrinsics.RealArgs.cs, deliberately sharing
        // these method names so this one line is the whole dispatch.
        if (sig.Float) return RenderFloat(ic, sig.RuntimeMethod);
        // ⛔ FACTORIAL IS ROUTED TO ITS EXACT ARM EVEN WITH A FLOAT ARGUMENT (PB21), and it is the only one.
        // RenderFloat wraps every result in FromDouble(double, ws), so a ...Real body must return something a
        // double can carry — and §15.36's result cannot be: 33! is ~8.7e36, which is why the exact body returns
        // Int128. The exact arm consumes its argument through IntArg, whose (long)(double) conversion handles a
        // float operand correctly, so the float case needs no separate body at all. Writing FactorialReal to
        // satisfy the pattern would have meant returning a double and silently losing exactness past 2^53 — a
        // function answering differently because of how its ARGUMENT was stored, which is the shape-dependence
        // defect PB13 closed. IntrinsicRealArgDriftTests carries the matching exemption with this reason.
        if (AnyRealArgument(ic) && sig.RuntimeMethod != "Factorial")
            return RenderFloat(ic, RealMethod(sig.RuntimeMethod));

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
                var (argList, s, anyReal) = AlignedArgsEx(ic);
                // STANDARD / STANDARD-DECIMAL (§15.4.1 r1 + §8.8.1.5.1 — MEAN's returned value shall EQUAL its
                // §15.60.4 equivalent arithmetic expression evaluated in SDIDI form, so the spec's own NOTE-2
                // relation `FUNCTION MEAN(a b c) = (a + b + c) / 3` holds): the exact Int128 sum (identical to
                // the SDIDI addition chain — ≤32-digit sums are exact in both engines) divides ONCE in SDIDI
                // form under the INTERMEDIATE ROUNDING mode. Fixed-point arguments only — a float argument
                // falls to the native rendering below exactly as before (its Align shape is the pre-existing
                // native-path behavior for float statistics arguments).
                if (num.StandardDecimal && !anyReal)
                    return new NumX(RuntimeApi.DecDivLifted(RuntimeApi.Intrinsic("SumScaled", argList),
                        s.ToString(), ic.Args.Count.ToString(), num.IntermediateMode), 0, Dec: true);
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
            case "OrdMaxString" or "OrdMinString":                              // all-string form (PCS via CollatePrefix, CA23)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, CollatePrefix(ic) + StrArgList(ic)), 0);

            // NUMVAL / NUMVAL-C (§15.67/§15.68): parse to (unscaled, actual scale), rescaled to the compile-time
            // working scale — the ≥6 floor is the NUMVAL rule (the receiver scale is stale in IF conditions; the
            // suite's deepest literal fraction is 5 digits, so 6 is lossless), CAPPED at the receiver's Int128
            // headroom by the same PB13 argument the float family uses. These carried the defect too: the runtime
            // clamped the parsed value at long.MaxValue, so a 20-digit NUMVAL string at ws = 6 needs 26 digits and
            // silently became 9223372036.854775807.
            case "Numval":
            {
                int ws = num.Receiver.WorkingScale(ReceiverContext.NumvalScaleFloor);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}"), ws);
            }
            case "NumvalC":
            {
                int ws = num.Receiver.WorkingScale(ReceiverContext.NumvalScaleFloor);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {ws}{CommaFlag}{AnycaseFlag(ic)}"), ws);
            }

            // The §15.93/§15.94 TEST validators — 0 / first-error position / LENGTH+1, scale 0. The digit-cap
            // sub-notes are ARITHMETIC-MODE dependent (§15.93.4 r1b notes 2/4): 31 native, 34 under the SDIDI
            // standard modes (standard-binary's 35 rides the P12/P13 STANDARD-BINARY wave).
            case "TestNumval":
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);
            case "TestNumvalC":
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}"), 0);

            // The §15.90/§15.91 date validators — integer verdict chains (year → month → day), scale 0.
            case "TestDateYyyymmdd" or "TestDayYyyyddd":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, IntArg(ic, 0)), 0);

            case "FindString":                                                  // §15.37 FIND-STRING (2023) — 1-based position of argument-2 in argument-1
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {(ic.FindLast ? "true" : "false")}, "
                    + $"{(ic.Args.Count > 2 ? IntArg(ic, 2) : "0")}, {(ic.Anycase ? "true" : "false")}"), 0);
            case "Ord":                                                         // §15.70 — PCS-relative ordinal (H5: weights only when flagged)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{Collate(ic)}"), 0);
            case "Length":                                                      // §15.50 runtime residue (nested string-fn argument)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);

            // Date/time conversions (§15.22/24/46/47; integer date form §15.5.2).
            case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, IntArg(ic, 0)), 0);

            // The Y2K windowing trio (§15.23/§15.25/§15.100) — integer results; the optional trailing
            // argument-2 (a SIGNED offset) / argument-3 ride the runtime's C#-optional-parameter defaults
            // (50 / the execution-time year via the argument-3 = 0 sentinel), so only the present arguments
            // render.
            case "DateToYyyymmdd" or "DayToYyyyddd" or "YearToYyyy":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod,
                    string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => IntArg(ic, i)))), 0);

            // SECONDS-PAST-MIDNIGHT (§15.80) — type NUMERIC in standard numeric time form: the runtime
            // returns the local time-of-day TICK count = the unscaled value at SCALE 7 (the documented
            // 100 ns COBOL.NET precision, §15.80.3 r3).
            case "SecondsPastMidnight":
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, ""), 7);

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
            // §15.69 NUMVAL-F — the ws floor follows the float-family precedent, and so does its CAP: this is a
            // THIRD instance of the PB13 quantizer defect, and the one PB5 itself missed. The runtime still
            // clamped at long.MaxValue (PB5's original 9.2×10¹⁸ clamp, never swept to this sibling), so
            // FUNCTION NUMVAL-F("1E+20") into a PIC 9(31) returned 9223372036 — ten orders of magnitude out,
            // with NO SIZE ERROR, where §15.69.4 r2 requires "an approximation of the numeric value represented
            // by argument-1". Found by FloatQuantizeHeadroomDriftTests, not by eye.
            case "NumvalF":
            {
                int ws = num.Receiver.FloatWorkingScale;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}"), ws);
            }
            case "TestNumvalF":                                                 // §15.95 — 0 / first-error position / LENGTH+1
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{CommaFlag}"), 0);

            case "IntegerOfBoolean":                                            // §15.45.4 r1 — the unsigned MSB-first value of the bit configuration
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);

            default:
                return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (no numeric render recipe)"), 0);
        }
    }

    /// <summary>The §15.4.1 floating-math family: arguments as doubles (through the one scaled→double
    /// conversion), result quantized by the ONE FromDouble at <c>ws = max(Receiver.Scale, 9)</c> (the ≥9 float floor).</summary>
    /// <summary>The FLOATING-POINT body's name for an exact-family runtime method (PB2).</summary>
    /// <remarks>
    /// A CONVENTION, not a table: <c>XxxScaled</c> → <c>XxxReal</c>, anything else gains a <c>Real</c> suffix.
    /// The obvious alternative — a same-named <c>double</c> overload — does not compile: an integer literal
    /// converts implicitly to both <c>Int128</c> and <c>double</c>, so <c>FUNCTION MAX(5 7)</c> became a CS0121
    /// ambiguity and broke six corpus programs that never touched a float. <c>IntrinsicRealArgDriftTests</c>
    /// asserts the counterpart exists for every exact method reachable with a real argument.
    /// </remarks>
    internal static string RealMethod(string exact) =>
        exact.EndsWith("Scaled", StringComparison.Ordinal)
            ? string.Concat(exact.AsSpan(0, exact.Length - "Scaled".Length), "Real")
            : exact + "Real";

    private NumX RenderFloat(BoundIntrinsicCall ic, string method)
    {
        var sig = ic.Sig;
        // The ≥9 float floor CAPPED at the receiver's Int128 headroom — the one rule, on ReceiverContext (PB13).
        int ws = num.Receiver.FloatWorkingScale;
        string call = method switch
        {
            // RANDOM (§15.75.3): the no-argument form continues the current sequence; the seeded form restarts it.
            "Random" when ic.Args.Count == 0 => RuntimeApi.Intrinsic(method, ""),
            "Random" => RuntimeApi.Intrinsic(method, IntArg(ic, 0)),
            _ => RuntimeApi.Intrinsic(method,
                string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => Dbl(ic, i)))),
        };
        // A float RECEIVER keeps the transcendental result in the binary64 pipeline (full precision — SQRT(2) into a
        // COMP-2 is 1.4142135623730951, not the scale-9 1.414213562); a fixed receiver quantizes through FromDouble
        // at the working scale (the established behavior the intrinsic goldens encode). (D16 review finding.)
        //
        // ⛔ AND SO DOES A RECEIVER-LESS CONTEXT (fix-queue PB13, the half no working-scale choice can reach).
        // §15.4.1: "When native arithmetic is in effect, the characteristics and representation of the returned
        // value are defined by the implementor" — COBOL.NET's determination is that the §15.4.1 float family's
        // returned value IS a binary64, and the quantization exists ONLY to land it in a fixed-point receiver,
        // whose scale is what defines the quantization. With no receiver there is no scale to quantize TO, and the
        // arbitrary ws = 9 stand-in was not merely a different rendering — it was WRONG, because FromDouble
        // saturates: `IF FUNCTION EXP10(30) = FUNCTION EXP10(31)` evaluated TRUE (both sides saturating to the
        // same Int128.MaxValue) and `DISPLAY FUNCTION EXP10(31)` printed 170141183460469231731687303715.884105727.
        // Every consumer of a receiver-less numeric already has a Real arm and is MORE correct on it: a relation
        // compares natively (§8.8.4.2.4 — "comparison proceeds by the rules of native arithmetic", the same arm a
        // COMP-2 operand takes), the text channel renders through the one CobolFloat.Display a float ITEM uses
        // (§14.9.11.4 GR1), and a MOVE source lands through CobolFloat.ToScaled AT THE RECEIVER'S SCALE — which is
        // the saturation-safe form, since the store's capacity check then sees the sentinel undivided.
        // This is what makes FUNCTION SQRT(2) and a COMP-2 item holding that value the SAME value everywhere,
        // which OperandText.NumericIntrinsicText already documented as required and the ws = 9 form silently broke.
        // ⛔ THE UNQUANTIZED ARMS KEEP THE RAISE SITE. FromDouble is where an out-of-domain NaN became the §15.3
        // EC-ARGUMENT-FUNCTION default (or the raise, under checking); leaving the value in binary64 skips it, so
        // `COMPUTE R = FUNCTION ACOS(2)` gave the default 0 while `IF FUNCTION ACOS(2) = 0` propagated a raw NaN
        // and compared FALSE. RealResult restores the screen without re-quantizing — a function's returned value
        // must not depend on the SHAPE of its receiver (§15.4), and under EC-ARGUMENT-FUNCTION checking
        // §14.6.13.1 requires the condition be raised at all. (Found by the Phase-B §15.55 refuter.)
        return num.Receiver.Real || num.Receiver.Receiverless
            ? new NumX(RuntimeApi.Intrinsic("RealResult", call), 0, Real: true)
            : new NumX(RuntimeApi.Intrinsic("FromDouble", $"{call}, {ws}"), ws);
    }

    // ── Argument rendering (the ONE NumericRenderer for every numeric-kind argument) ────────────────────────

    private NumX Arg(BoundIntrinsicCall ic, int i) => Landed(num.AsNum(ic.Args[i], num.Receiver));

    /// <summary>
    /// ⛔ THE ONE SDIDI → EXACT-CARRIER LANDING FOR AN INTRINSIC ARGUMENT (fix-queue PB32/PB14).
    /// </summary>
    /// <remarks>
    /// <para><see cref="NumX"/> has THREE carriers — exact scaled <see cref="Int128"/>, the <c>CobolDec</c> SDIDI,
    /// and binary64 — and this renderer's arms were written for two. Under <c>ARITHMETIC IS STANDARD-DECIMAL</c>
    /// every arithmetic expression renders as a <c>Dec</c> carrier (<c>NumericRenderer.CombineCore</c> /
    /// <c>Power</c>), so a §15.3 type-10 arithmetic-expression argument — legal at 2014 and 2023 — was handed raw
    /// to a body expecting <c>Int128</c> and the user saw a Roslyn <c>CS1503</c> on conforming COBOL.</para>
    /// <para>⛔ IT IS LANDED HERE, AT <see cref="Arg"/>, AND NOT IN EACH ARM. Placing the arm in
    /// <c>NumericRenderer.Align</c> alone fixed only the variadic family that happens to route through it —
    /// measured: MAX / MIN / MOD / MEDIAN recovered while ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still
    /// failed to compile, because those arms consume <c>Arg(...).Expr</c> (or <see cref="AsInt"/>) directly. Every
    /// numeric argument in this renderer originates HERE, so one landing covers all of them and the NEXT arm
    /// added is covered without anyone remembering to (feedback_change_the_dispatch_not_the_callers). Landing
    /// early also gives the SDIDI a compile-time <c>Scale</c>, so it participates in
    /// <see cref="AlignedArgsEx"/>'s common-scale maximum instead of contributing the placeholder 0 and dragging
    /// the whole argument list down to integer alignment.</para>
    /// <para>⚠ THE SCALE IS A CHOICE, AND IT IS THE FAMILY-FLOOR RULE, NOT A NEW ONE. An SDIDI carries its
    /// exponent at RUN time, so there is no compile-time scale to preserve; the value lands through the same
    /// <c>WorkingScale(floor)</c> discipline the NUMVAL and float families use — the receiver's scale, never below
    /// the §15.67 fraction floor, capped at the receiver's <c>Int128</c> headroom by the PB13 argument. A quotient
    /// with more fraction digits than that is truncated before the function sees it, which is a strictly smaller
    /// wrong than "does not compile" and is NOT the end state: the §15.4.1 r1 answer is a Dec-carrier body, and it
    /// is ledgered as PB38 rather than left as an unrecorded approximation.</para>
    /// </remarks>
    /// <remarks>
    /// ⛔ <b>AND A FLOAT OPERAND LANDS THE SAME WAY UNDER A STANDARD ARITHMETIC MODE (fix-queue PB38).</b> This is
    /// where the ARITHMETIC MODE beats the float branch, which is the ordering `COBOLNET_NUMERIC_DESIGN.md` D3
    /// states in words ("the mode branch runs BEFORE the D16 float branch") and which
    /// <c>NumericRenderer.CombineCore</c>, <c>NumericRenderer.Power</c> and <c>ConditionRenderer</c> all obey —
    /// <c>RenderNum</c> was the ONE renderer that did not, so a single COMP-1/COMP-2 argument demoted the whole
    /// list to binary64 even under <c>ARITHMETIC IS STANDARD-DECIMAL</c>, where §15.4.1 r1 is unconditional (the
    /// returned value <i>shall equal</i> the equivalent arithmetic expression) and §8.8.1.5.2 r1 converts every
    /// fixed-point operand into an SDIDI EXACTLY. MEASURED before the fix, with three 18-digit items and a COMP-2
    /// pair: <c>FUNCTION MEDIAN(H1 H2 H3 F1 F1)</c> returned 100000000000000004.76 where the SDIDI-exact answer
    /// is 100000000000000001, and <c>FUNCTION MAX(H1 H2 H3 F1)</c> returned the same 100000000000000004.76
    /// against 100000000000000003 — the three distinct 18-digit operands all collapse to ONE binary64 (the ulp
    /// at 1e17 is 16) and compare EQUAL, so the §8.8.4.2.4 comparison the clause mandates never happens. The same
    /// operands without the float are exact in the same program.
    /// <para>The float converts through <see cref="NumericRenderer.DecOperand"/> — the compiler's own §8.8.1.5.1
    /// conversion, the one <c>CombineCore</c> and <c>Power</c> already use — and then lands by the identical
    /// route a <c>Dec</c> operand takes. So there is ONE landing here, not a second mechanism beside the first,
    /// and the exact Int128 family evaluates the EAE as §15.4.1 r1 requires.</para>
    /// </remarks>
    private NumX Landed(NumX x)
    {
        // The MODE first: under a standard mode a float operand is converted in (§8.8.1.5.1), not computed in.
        if (x.Real && num.StandardDecimal)
            x = new NumX(num.DecOperand(x), 0, Dec: true);
        if (!x.Dec) return x;
        int ws = num.Receiver.WorkingScale(ReceiverContext.NumvalScaleFloor);
        return new NumX(RuntimeApi.DecToUnscaled(x.Expr, ws.ToString(), CobolRounding.Truncation), ws);
    }

    /// <summary>Does any argument render as FLOATING (binary64) rather than as a scaled integer? (PB2.)</summary>
    /// <remarks>Asked of the ARGUMENTS, not the receiver: a float argument into a fixed-point receiver still has
    /// to be computed in binary64 and only then quantized, which is exactly what <see cref="RenderFloat"/>'s
    /// <c>FromDouble</c> tail does.</remarks>
    /// <remarks>⛔ ASKED OF THE <b>LANDED</b> OPERAND, NOT THE RAW ONE (fix-queue PB38). Under a standard
    /// arithmetic mode <see cref="Landed"/> converts a float in per §8.8.1.5.1, so it is no longer floating and
    /// this dispatch must not route the call to the binary64 body. Reading the RAW operand here would reinstate
    /// the exact defect the landing exists to remove, with the landing silently doing nothing — the two must ask
    /// the same question of the same value.</remarks>
    private bool AnyRealArgument(BoundIntrinsicCall ic) =>
        ic.Args.Any(a => Landed(num.AsNum(a, num.Receiver)).Real);

    /// <summary>A numeric argument as a C# double (the float family's §15.4.1 carrier).</summary>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(Arg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §15.3; a fractional value is the program's EC latitude).</summary>
    private string IntArg(BoundIntrinsicCall ic, int i) => AsInt(Arg(ic, i));

    /// <summary>
    /// ⛔ THE ONE NARROWING, AND IT RAISES RATHER THAN WRAPS (fix-queue PB22). This emitted a bare
    /// <c>(long)(…)</c> over an <c>Int128</c>-typed expression, and <c>RoslynBackend</c> sets no
    /// <c>checkOverflow</c> — so the cast wrapped MODULO 2⁶⁴, silently, and BEFORE the function's own range
    /// guard could see the value. <c>FUNCTION INTEGER-OF-DAY(P * 100 + 62)</c> with
    /// <c>P PIC 9(18) VALUE 184467440737115466</c> is 2⁶⁴ + 1995046, so §15.5.2's correct 1601..9999 / 1..366
    /// check received 1995046 and returned a plausible 143951 — from an argument nineteen orders of magnitude
    /// away, with no EC-ARGUMENT-FUNCTION even under enabled checking.
    /// <para>Both entries (<see cref="IntArg"/> for the numeric channel, <see cref="ArgInt"/> for the string one)
    /// funnel here, so ONE change covers seven renderer arms over eleven functions — which is the point: a value
    /// the receiving body cannot represent is an incorrect argument (§15.3), and the place to say so is where
    /// the narrowing happens, not eleven times downstream.</para>
    /// <para>The <c>Real</c> arm takes the double-typed twin by NAME rather than by overload: an integer literal
    /// converts implicitly to both <c>Int128</c> and <c>double</c>, and an overload pair would turn
    /// <c>FUNCTION FACTORIAL(5)</c> into a CS0121 ambiguity — the collision that broke six corpus programs when
    /// the <c>…Real</c> bodies were first written.</para></summary>
    private static string AsInt(NumX a) =>
        a.Real ? RuntimeApi.IntegerArg(a.Expr, real: true)
        : a.Scale == 0 ? RuntimeApi.IntegerArg(a.Expr)
        : RuntimeApi.IntegerArg(RuntimeApi.NumRescale(a.Expr, a.Scale.ToString(), "0", CobolRounding.Truncation));

    /// <summary>The variadic arguments aligned to their common scale (ISO §8.8.1 — alignment makes unscaled
    /// comparison/arithmetic equal value comparison/arithmetic), as a C# argument list + that scale.</summary>
    private (string ArgList, int Scale) AlignedArgs(BoundIntrinsicCall ic)
    {
        var (argList, s, _) = AlignedArgsEx(ic);
        return (argList, s);
    }

    /// <summary>As <see cref="AlignedArgs"/>, also reporting whether any argument rendered FLOATING (Real) —
    /// the MEAN standard-arithmetic branch keys on it (a float statistics argument keeps the pre-existing
    /// native rendering).</summary>
    private (string ArgList, int Scale, bool AnyReal) AlignedArgsEx(BoundIntrinsicCall ic)
    {
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        return (string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s))), s, xs.Any(x => x.Real));
    }

    private string StrArgList(BoundIntrinsicCall ic) => string.Join(", ", ic.Args.Select(Str));

    private string CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The ANYCASE named argument (§15.68.3 r4f / §15.37.4 r4 — the ONE <see cref="BoundIntrinsicCall.Anycase"/>
    /// flag), rendered only when present so the default-free call stays byte-stable.</summary>
    private static string AnycaseFlag(BoundIntrinsicCall ic) => ic.Anycase ? ", anycase: true" : "";

    /// <summary>The §15.93.4/§15.94.4 r1b digit-cap named argument: 34 under the SDIDI standard-arithmetic
    /// modes (sub-note 4), omitted for the native default 31 (sub-note 2).</summary>
    private string DigitCapFlag => num.StandardDecimal ? ", digitCap: 34" : "";

    /// <summary>The trailing weights argument for a PCS-flagged CHAR/ORD/CHAR-NATIONAL (hazard H5: the binder
    /// set <see cref="BoundIntrinsicCall.Collate"/>/<see cref="BoundIntrinsicCall.CollateNat"/> ONLY when the
    /// matching non-identity PCS exists — exactly when the program class emitted its <c>__COLLATE</c> /
    /// <c>__COLLATE_NAT</c> table; the two never coexist on one call — CHAR/alphanumeric-ORD read the
    /// alphanumeric sequence, CHAR-NATIONAL/national-ORD the national one, §15.15.4/§15.16.4/§15.70.4).</summary>
    private static string Collate(BoundIntrinsicCall ic) =>
        ic.Collate ? ", __COLLATE" : ic.CollateNat ? ", __COLLATE_NAT" : "";

    /// <summary>The LEADING weights argument for a PCS-flagged MAX/MIN/ORD-MAX/ORD-MIN string form — collate goes
    /// FIRST (a <c>params string[]</c> can take no trailing param), selecting the <c>MaxString(ushort[]|NationalCollation,
    /// params string[])</c> overload. The mirror of <see cref="Collate"/>'s trailing form for the single-arg CHAR/ORD.</summary>
    private static string CollatePrefix(BoundIntrinsicCall ic) =>
        ic.Collate ? "__COLLATE, " : ic.CollateNat ? "__COLLATE_NAT, " : "";

    // ── The STRING channel (instance — reached through OperandText.AsString with the per-unit renderer) ─────

    /// <summary>Render an alphanumeric-result intrinsic as a C# string expression. An INSTANCE channel (P7
    /// Step 12 — the context-free static twin is deleted): numeric-kind arguments render through the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> (a string-channel call has no
    /// numeric receiver), so division, float items, nested numeric intrinsics, and numeric-edited de-edits
    /// all render where the static channel stayed loud (H3 closed).</summary>
    /// <summary>
    /// Reference-modify the function RESULT when the reference carried one (ISO §8.4.3.3.3 SR2, fix-queue PB8),
    /// through the SAME <c>CobolString.RefMod</c> — and therefore the same §8.4.3.3.4 item-5c bounds check and
    /// EC-BOUND-REF-MOD raise — that a reference-modified data item uses. No second slicer exists.
    /// <para>⛔ THIS IS THE ONLY EMIT SITE THAT HAS TO HONOUR IT, and that is provable rather than surveyed: SR2
    /// admits a ref-mod only on an alphanumeric, boolean or national function, and every such result renders
    /// through this one method (reached from <c>OperandText.AsString</c> and from the nested-argument visitor).
    /// A numeric-result call cannot carry one — the binder rejects it with COBOLNET1629 — so the numeric and
    /// folded channels need no arm. Wrapping HERE rather than at each caller is what keeps that true.</para>
    /// </summary>
    public string RenderString(BoundIntrinsicCall ic)
    {
        string value = RenderStringValue(ic);
        return ic.RefMod is { } rm ? RuntimeApi.StrRefMod(value, rm) : value;
    }

    private string RenderStringValue(BoundIntrinsicCall ic)
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
            "CharNational" =>                                                  // §15.16 — the NATIONAL PCS ordinal (native UTF-16 order,
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   or __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL)
                    $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CurrentDate" => RuntimeApi.DateFn(sig.RuntimeMethod, ""),         // §15.21 — the runtime clock
            // WHEN-COMPILED is the COMPILATION timestamp (§15.99.3 r2) — a constant in the generated source.
            // (The legacy's runtime-clock placeholder also passes IF142A's plausibility checks; the constant is
            // the spec-correct form — scout brief §4.4.)
            "WhenCompiled" => EmitText.CsLiteral(WhenCompiledStamp.Value),
            "MaxString" or "MinString" =>                                      // §15.59/63 all-string form (PCS via CollatePrefix, CA23)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, CollatePrefix(ic) + StrArgList(ic)),
            "Concat" =>                                                        // §15.18 — concatenate all argument images (2023)
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            // BOOLEAN-OF-INTEGER (§15.13.4 r1) — a boolean-result function on the D-B1 '0'/'1' substrate:
            // rightmost position = low-order digit, left zero-fill/truncate to argument-2 positions.
            "BooleanOfInteger" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgInt(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
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
            // The last-exception interrogation functions (§15.28–15.33 — the EC model, incl. the national
            // twins EXCEPTION-FILE-N/EXCEPTION-LOCATION-N §15.29/§15.31): zero-argument reads of the runtime
            // register; the binder's EcNoteFunction flagged the group EC gate, so the generated source
            // carries the CobolNet.Runtime.Exceptions using.
            "EcStatus" => RuntimeApi.EcFn("Status"),                           // §15.33
            "EcLocation" => RuntimeApi.EcFn("Location"),                       // §15.30
            "EcLocationN" => RuntimeApi.EcFn("LocationN"),                     // §15.31 — the national twin
            "EcStatement" => RuntimeApi.EcFn("Statement"),                     // §15.32
            // §15.28.4 / §15.29.4: the no-argument form (r1) reads the last-exception register; the 2023
            // file-connector-argument form (r2) reads the NAMED connector — the SAME FileKeyExpr the file verbs use.
            "EcFile" => ic.FileArg is { } eff
                ? RuntimeApi.EcFn("File", EmitText.FileKeyExpr(eff))          // §15.28.4 r2 — the named connector's status
                : RuntimeApi.EcFn("File"),                                    // §15.28.4 r1 — the no-argument form
            "EcFileN" => ic.FileArg is { } effn
                ? RuntimeApi.EcFn("FileN", EmitText.FileKeyExpr(effn))        // §15.29.4 r2 — the national twin
                : RuntimeApi.EcFn("FileN"),                                   // §15.29.4 r1 — the no-argument form
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
            n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } nested
                ? owner.RenderString(nested) : Loud(n);   // string-class results incl. national (§15.66) + boolean (§15.13 — the '0'/'1' substrate)
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        public string Visit(BoundNumericLiteral n) => Loud(n);
        public string Visit(BoundFigurative n) => Loud(n);
        public string Visit(BoundAllLiteral n) => Loud(n);
        public string Visit(BoundBoolOperand n) => Loud(n);
    }
}
