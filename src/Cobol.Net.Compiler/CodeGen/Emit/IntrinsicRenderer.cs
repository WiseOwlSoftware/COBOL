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
        // A string-class function result in a NUMERIC context reaches here only under --permissive (the binder's
        // §8.8.1.1 screen rejects it under strict conformance — kb/Work PB68): the DA6 leniency, the same
        // digit-character decode an alphanumeric DATA ITEM gets there (CobolNum.FromAlphanumeric over the
        // function's string image, an unsigned integer). It used to be a loud RUNTIME value on legal-shaped
        // source, i.e. an unhandled exception at the wrong stage.
        if (ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
            return new NumX(RuntimeApi.NumFromAlphanumeric(RenderString(ic)), 0);

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
        // ⛔ THE STANDARD-MODE ARM RUNS BEFORE THE FLOAT DISPATCH (kb/Work R18 — ledger F15/F19). Four float-family
        // rows have their standard-mode value FIXED by the spec, with no §15.4.1 r2 latitude: FUNCTION E and
        // FUNCTION PI are exact 34-digit constants (§15.27.3 r3 / §15.73.3 r3), and EXP / EXP10 carry equivalent
        // arithmetic expressions — `FUNCTION E ** argument-1` (§15.34.4 r1) and `10 ** argument-1` (§15.35.4 r1) —
        // that §15.4.1 r1 requires the returned value to EQUAL under a standard mode. Each renders through the
        // SAME CobolDec.Pow the hand-written `**` uses (§8.8.1.5.4), over the SAME exact constants, so the
        // function and its spelled-out EAE agree by construction — binary64 Math.Pow is not SDIDI arithmetic, and
        // routing these through RenderFloat gave receiver-DEPENDENT answers (§15.4.1's same-for-all-instances
        // rule) wrong from the third significant digit's ulp up. A float ARGUMENT lifts via DecOperand →
        // CobolDec.FromDouble (§8.8.1.5.1), exactly as the landed exact-family lane does.
        // (An if-chain, deliberately NOT a `switch (sig.RuntimeMethod)`: IntrinsicRealArgDriftTests anchors its
        // exact-arm scan on the FIRST such switch, and these arms are not exact-family arms — a Real argument
        // lifts through DecOperand, so no `…Real` body exists or is needed.)
        if (num.StandardDecimal)
        {
            // ⛔ THE EXPONENT ARGUMENT LIFTS FROM THE RAW OPERAND, NOT THE LANDED ONE (fix-queue PB56).
            // DecOperand(Arg(...)) round-tripped a Dec operand through the interim unscaled landing — truncated
            // at the working scale — and only then lifted it back, which is the very quantization this arm
            // exists to avoid (the RV-15.34.4-1 triage row measured it).
            if (sig.RuntimeMethod == "E") return new NumX(RuntimeApi.DecE, 0, Dec: true);
            if (sig.RuntimeMethod == "Pi") return new NumX(RuntimeApi.DecPi, 0, Dec: true);
            if (sig.RuntimeMethod == "Exp")
                return new NumX(RuntimeApi.DecPow(RuntimeApi.DecE, DecArg(ic, 0), num.IntermediateMode), 0, Dec: true);
            if (sig.RuntimeMethod == "Exp10")
                return new NumX(RuntimeApi.DecPow(RuntimeApi.DecFrom("10", "0"), DecArg(ic, 0), num.IntermediateMode), 0, Dec: true);
            if (RenderDec(ic) is { } dec)
                return dec;
        }
        // NATIVE arithmetic with an SDIDI-carried ARGUMENT (kb/Work PB69) — today the only native Dec producer is
        // integer exponentiation past (or within) the Int128 window: the function computes on the SDIDI body when
        // it has one (MOD/REM keep exact integers exact — CobolIntrinsics.ModDec's integer fast path), rather than
        // landing the argument into Int128 at a working scale that a 33-digit power already overflows.
        else if (AnyDecRaw(ic) && RenderDec(ic) is { } decNative)
            return decNative;
        if (sig.Float) return RenderFloat(ic, sig.RuntimeMethod);
        // ⛔ FACTORIAL IS ROUTED TO ITS EXACT ARM EVEN WITH A FLOAT ARGUMENT (PB21), and it is the only one.
        // RenderFloat wraps every result in FromDouble(double, ws), so a ...Real body must return something a
        // double can carry — and §15.36's result cannot be: 33! is ~8.7e36, which is why the exact body returns
        // Int128. The exact arm consumes its argument through IntArg, whose (long)(double) conversion handles a
        // float operand correctly, so the float case needs no separate body at all. Writing FactorialReal to
        // satisfy the pattern would have meant returning a double and silently losing exactness past 2^53 — a
        // function answering differently because of how its ARGUMENT was stored, which is the shape-dependence
        // defect PB13 closed. IntrinsicRealArgDriftTests carries the matching exemption with this reason.
        // MAX / MIN / ORD-MAX / ORD-MIN — and MEDIAN over an odd count — are pure SELECTION: §15.59.4 r1 /
        // §15.63.4 r1 "the returned value is the CONTENT of the argument-1 having the greatest [least] value",
        // §15.61.4 r1 "the content of the argument-1 that is the middle value", each compared "according to the
        // rules for simple conditions"; no equivalent arithmetic expression, so §15.4.1's native latitude over the
        // representation never reaches the VALUE. A MIXED list (a float beside a fixed-point item) must deliver
        // the selected argument from ITS OWN carrier, never re-rendered through binary64 (kb/Work PB65
        // RV-15.59.4-1 D2: MAX(F1 N1) with N1 = 999999999999999999 returned 13 — the 18-digit content went
        // double → FromDouble at scale 9 → modular store; MEDIAN(F1 N1 N2) returned 0). The SDIDI carrier holds
        // a 38-digit fixed exactly and the float through the §8.8.1.5.1 conversion, and its compare is exact — so
        // a mixed selection list evaluates on the SDIDI under NATIVE too, and lands once at the receiver. An
        // all-float list stays in the float lane below (its content IS the double), and the arithmetic
        // statistical family (SUM / MEAN / RANGE / MIDRANGE — equivalent arithmetic expressions) keeps the D16
        // native float lane a float operand selects.
        if (sig.RuntimeMethod is "MaxScaled" or "MinScaled" or "OrdMax" or "OrdMin" or "MedianScaled"
            && AnyRealArgument(ic) && !AllRealArguments(ic) && RenderDec(ic) is { } decSelection)
            return decSelection;
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

            // MAX/MIN are pure SELECTION (§15.59.4/§15.63.4: the returned value is the CONTENT of an argument)
            // — no alignment at all (fix-queue PB65): the §8.8.4.2 comparison is CobolNum.Compare's exact
            // non-widening compare over each argument AT ITS OWN SCALE, and only the ONE selected value
            // rescales (escape-checked) to the common scale. The aligned forms wrapped silently at 39 aligned
            // digits — MIN of two positive arguments returned a NEGATIVE value, the content of NO argument.
            case "MaxScaled" or "MinScaled":
            {
                var (vals, scales, s) = RawArgPairs(ic);
                // The result scale is the RECEIVER's when one is known (a store then rescales by identity and
                // the receiver's own §14.9.25.4 GR6 truncation semantics apply, via the store-cap); a
                // receiverless/float context keeps the common scale and stays LOUD past the escape boundary —
                // a capped value inside a comparison would silently compare the wrong number (PB13's
                // receiver-blind-scale lesson, applied in both directions).
                bool store = !num.Receiver.Receiverless && !num.Receiver.Real;
                int to = store ? num.Receiver.Scale : s;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod is "MaxScaled" ? "MaxAt" : "MinAt",
                    $"{to}, {(store ? "true" : "false")}, {vals}, {scales}"), to);
            }
            // SUM/RANGE genuinely ADD, so they keep aligned arguments — now escape-CHECKED (Align emits
            // RescaleEscape): an alignment past the Int128 intermediate is the size-error condition, never a wrap.
            case "SumScaled" or "RangeScaled":                                  // §15.88/76 — result at the common scale
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
                // (Under a STANDARD mode MEAN never reaches here — RenderDec's alwaysDec family evaluates the
                // §15.60.4 equivalent arithmetic expression on the SDIDI carrier, kb/Work PB62 / RV-15.60.4-1.)
                var (argList, s, _) = AlignedArgsEx(ic);
                // Quotient quantized at ws = max(Receiver.Scale, s+1, 6): the receiver's scale when known, never
                // below the sum's own resolution + 1, with a fraction floor for receiver-less (scale-0) contexts.
                int ws = Math.Max(Math.Max(num.Receiver.Scale, s + 1), 6);
                // Same mode rule as NumericRenderer.Divide: AT the receiver scale the one exact RoundDiv applies
                // the receiver's mode; above it, truncate and let the receiver store round once (§14.7.4).
                CobolRounding mode = ws == num.Receiver.Scale ? num.Receiver.Rounding : CobolRounding.Truncation;
                // The divisor is the number of ARGUMENTS — a compile-time count unless a table(ALL) argument
                // ranges over a runtime count (ISO §15.3; kb/Work PB62), in which case the enumerated list is
                // bound ONCE (CobolTable.With) and both the sum and the count read it.
                if (StaticArgCount(ic) is { } n)
                    return new NumX(RuntimeApi.NumDivide(false, RuntimeApi.Intrinsic("SumScaled", argList),
                        s.ToString(), n.ToString(), "0", ws.ToString(), mode), ws);
                string xs = NextWithVar();
                return new NumX(RuntimeApi.With(argList, xs,
                    RuntimeApi.NumDivide(false, RuntimeApi.Intrinsic("SumScaled", xs), s.ToString(), $"{xs}.Length", "0", ws.ToString(), mode)), ws);
            }
            case "OrdMax" or "OrdMin":                                          // §15.71/72 — 1-based ordinal, tie = first
            {
                // Pure selection with NO result rescale — every legal argument list has its defined ordinal
                // (fix-queue PB65; the aligned form wrapped at 39 aligned digits and picked the wrong argument).
                var (vals, scales, _) = RawArgPairs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod is "OrdMax" ? "OrdMaxAt" : "OrdMinAt",
                    $"{vals}, {scales}"), 0);
            }
            case "OrdMaxString" or "OrdMinString":                              // all-string form (PCS via CollatePrefix, CA23)
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, CollatePrefix(ic) + StrArgList(ic)), 0);

            // NUMVAL / NUMVAL-C (§15.67/§15.68) under NATIVE arithmetic (the standard-mode value is RenderDec's
            // exact SDIDI arm — §15.4.1; PB60): parse to (unscaled, actual scale), rescaled to the compile-time
            // working scale — the ≥6 floor is the NUMVAL rule (the receiver scale is stale in IF conditions; the
            // suite's deepest literal fraction is 5 digits, so 6 is lossless), CAPPED at the receiver's Int128
            // headroom by the same PB13 argument the float family uses (the CONFORMANCE.md item-92 native
            // working-scale determination). These carried the defect too: the runtime clamped the parsed value at
            // long.MaxValue, so a 20-digit NUMVAL string at ws = 6 needs 26 digits and silently became
            // 9223372036.854775807.
            case "Numval":
            {
                int ws = num.Receiver.WorkingScale(ReceiverContext.NumvalScaleFloor);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}{DigitCapFlag}{CheckedFlag}"), ws);
            }
            case "NumvalC":
            {
                int ws = num.Receiver.WorkingScale(ReceiverContext.NumvalScaleFloor);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {ws}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}{CheckedFlag}"), ws);
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
            case "Length":                                                      // §15.50 runtime shapes (kb/Work PB61)
                // (A group with a runtime length — an OCCURS DEPENDING table, dynamic-length items or
                // dynamic-capacity tables beneath it — never arrives here: the binder's VariableLengthGroupSum
                // builds the §15.50.4 r4b/r7 expression, whose ODO part is a BoundOdoExtent.)
                // r6 — a national DYNAMIC LENGTH item's CURRENT length in bytes (the storage image); every other
                // runtime shape (a ref-mod view, an ANY LENGTH / dynamic-length alphanumeric item, a nested
                // string-function result) is the character-position count over the string image.
                if (ic.LengthInBytes && ic.Args[0] is BoundFieldOperand { Place: { } lp })
                    return new NumX(RuntimeApi.Intrinsic("ByteLength", OperandText.AsStorageImage(lp)), 0);
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);
            case "ByteLength":                                                  // §15.14 runtime shapes (kb/Work PB61)
                if (ic.Args[0] is BoundFieldOperand { Place: { } bp })
                    return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, OperandText.AsStorageImage(bp)), 0);
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
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{IntArg(ic, 0)}, {t.Expr}, {t.Scale}{LeapSecondFlag}"), t.Scale + 5);
            }
            case "IntegerOfFormattedDate":                                       // §15.48 — analyze a2 per format a1 → integer date
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            case "SecondsFromFormattedTime":                                    // §15.79 — result scale = the format's fractional-second count
            {
                if (ic.Args[0] is not BoundStringLiteral fmt)
                    return new NumX(EmitText.LoudValue("long",
                        "FUNCTION SECONDS-FROM-FORMATTED-TIME requires a literal time format (§15.79.3 r1)"), 0);
                int fsc = RuntimeApi.DateFormatFractionDigits(fmt.Value);       // compile-time — the ONE format analyzer
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {fsc}{LeapSecondFlag}"), fsc);
            }
            case "TestFormattedDatetime":                                       // §15.92 — 0 (valid) or the 1-based error position
                return new NumX(RuntimeApi.DateFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            // §15.69 NUMVAL-F — the ws floor follows the float-family precedent, and so does its CAP: this is a
            // THIRD instance of the PB13 quantizer defect, and the one PB5 itself missed. The runtime still
            // clamped at long.MaxValue (PB5's original 9.2×10¹⁸ clamp, never swept to this sibling), so
            // FUNCTION NUMVAL-F("1E+20") into a PIC 9(31) returned 9223372036 — ten orders of magnitude out,
            // with NO SIZE ERROR, where §15.69.4 r2 requires "an approximation of the numeric value represented
            // by argument-1". Found by FloatQuantizeHeadroomDriftTests, not by eye.
            case "NumvalF":
            {
                // ⛔ THE FLOAT FAMILY'S RECEIVERLESS/REAL ARM, SWEPT HERE AT LAST (fix-queue PB60 / RV-15.69.4-2 —
                // the sweep RenderFloat's own comment asked for). §15.69.4 r2 makes NUMVAL-F's NATIVE value an
                // APPROXIMATION, and this compiler's documented determination for an approximated returned value
                // (CONFORMANCE.md item 92) is the float family's: binary64 unless a FIXED-POINT arithmetic receiver
                // quantizes it. With no receiver there is no scale to quantize TO, and the ws-9 stand-in was not a
                // rendering choice but a wrong answer: `IF FUNCTION NUMVAL-F("5E+30") = FUNCTION NUMVAL-F("9E+30")`
                // was TRUE (both saturating to the Int128 sentinel), `DISPLAY` printed that sentinel, a COMP-2
                // receiver got 1.7E+29, and NUMVAL-F("1.5E-12") was 0 in every receiver-less channel. Every consumer
                // of a receiver-less numeric already has its Real arm (relation, text, subscript, argument).
                // A fixed arithmetic receiver keeps the EXACT Int128 parse below — the receiver's scale is known and
                // the PB13 cap makes any saturation visible to its capacity check — and so does a MOVE SENDER
                // (ReceiverContext.MoveSender: the receiver's scale is known and MOVE lands the parsed decimal
                // digit-for-digit; the binary64 route would land NUMVAL-F("1.5E-8") into V9(9) as 14 through
                // ToScaled's multiply-then-truncate, and lose every digit past the 17th of a 20-digit argument).
                if (num.Receiver.Real || (num.Receiver.Receiverless && !num.Receiver.MoveSender))
                    return new NumX(RuntimeApi.Intrinsic("NumvalFDouble", $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0, Real: true);
                int ws = num.Receiver.FloatWorkingScale;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ws}{CommaFlag}{DigitCapFlag}{CheckedFlag}"), ws);
            }
            case "TestNumvalF":                                                 // §15.95 — 0 / first-error position / LENGTH+1;
                return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod,         //   the r1b digit-cap sub-note is MODE-dependent,
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);         //   exactly as its two TEST- siblings (PB60)

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
            // PRESENT-VALUE (§15.74.2 `argument-1 { argument-2 } …`): the rate leads, the amounts are the params tail.
            "PresentValue" => LeadThenTail(ic, method, "", "double", DblOf),
            // A table(ALL) argument enumerates at run time (ISO §15.3; kb/Work PB62) — the list becomes ONE array.
            _ => RuntimeApi.Intrinsic(method, ArgArray(ic, 0, "double", DblOf)
                ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => Dbl(ic, i)))),
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
        if (num.Receiver.Real || num.Receiver.Receiverless)
            return new NumX(RuntimeApi.Intrinsic("RealResult", call), 0, Real: true);
        // ⛔ UNDER A STANDARD MODE THE RESULT CONVERTS IN, IT DOES NOT QUANTIZE (fix-queue PB56). The prose
        // family's returned value is an implementor-defined binary64 approximation in every mode (§15.4.1
        // last ¶), but under STANDARD-DECIMAL that approximation enters the expression as an SDIDI operand
        // per §8.8.1.5.1 — CobolDec.FromDouble, the shortest round-trip identity — and lands at the receiver
        // ONCE, instead of truncating to unscaled at a receiver-derived working scale first (the triage rows
        // RV-15.8.4-1 / RV-15.10.4-1 / RV-15.11.4-1 / RV-15.35.4-1 measured the double quantization).
        // RealResult stays inside the conversion: it is the NaN → EC-ARGUMENT-FUNCTION raise site.
        if (num.StandardDecimal)
            return new NumX(RuntimeApi.DecFromDouble(RuntimeApi.Intrinsic("RealResult", call)), 0, Dec: true);
        // ⛔ A BOUNDED CODOMAIN CLAMPS THE QUANTIZED VALUE (fix-queue PB65 / RV-15.75.4-1): the catalog row
        // carries the §15.x.4 bound and the quantizer refuses to round out of it — RANDOM's [0,1) reached
        // exactly 1.000000000 in a 9V9(9) receiver, ASIN(1) exceeded its closed-but-irrational π/2. The
        // rounding itself stays (it recovers the SQRT(10) ** 2 binary64 artifact); only the exit is closed.
        // The Dec arm above needs no clamp: the un-quantized double never exits its codomain.
        // The quantizer's landing form past the carrier is the STATEMENT's (kb/Work PB77): saturate under ON SIZE
        // ERROR / EC-SIZE checking (the capacity check raises), the low-order digits for the no-phrase store.
        if (sig.Codomain != IntrinsicCodomain.None)
            return new NumX(RuntimeApi.Intrinsic("FromDoubleBounded",
                $"{call}, {ws}, {RuntimeApi.CodomainConst(sig.Codomain)}{CheckedFlag}"), ws);
        return new NumX(RuntimeApi.Intrinsic("FromDouble", $"{call}, {ws}{CheckedFlag}"), ws);
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
    /// the §15.67 fraction floor, capped at the receiver's <c>Int128</c> headroom by the PB13 argument.
    /// ⛔ THE §15.4.1 r1 FAMILY NO LONGER PASSES THROUGH HERE (fix-queue PB56): under a standard mode a
    /// Dec/float-bearing exact-family call routes to <see cref="RenderDec"/> BEFORE any landing, so this
    /// truncation now serves only the residual non-EAE consumers (NUMVAL's compile-scale materialization until
    /// PB60's parser rewrite, integer-argument intake via <c>AsInt</c>) — where scale-0/format semantics make
    /// the landing the defined behavior rather than an approximation.</para>
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
    private NumX Landed(NumX x) => num.Landed(x, num.Receiver);   // the ONE landing (NumericRenderer.Landed — kb/Work PB84)

    // ── The STANDARD-DECIMAL body dispatch (fix-queue PB56) ──────────────────────────────────────────────────

    /// <summary>An argument as rendered, WITHOUT the unscaled landing — the SDIDI lane's input.</summary>
    private NumX RawArg(BoundIntrinsicCall ic, int i) => num.AsNum(ic.Args[i], num.Receiver);

    /// <summary>An argument lifted to a <c>CobolDec</c> expression from its RAW carrier: a Dec operand passes
    /// through, a float converts per §8.8.1.5.1, a fixed-point operand lifts exactly — never quantized.</summary>
    private string DecArg(BoundIntrinsicCall ic, int i) => num.DecOperand(RawArg(ic, i));

    private string DecArgList(BoundIntrinsicCall ic) =>
        ArgArray(ic, 0, "CobolDec", DecOf) ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => DecArg(ic, i)));

    /// <summary>One operand lifted to a <c>CobolDec</c> from its RAW carrier — the per-operand form of <see cref="DecArg"/>
    /// (a table(ALL) element renders here inside its enumeration lambda).</summary>
    private string DecOf(BoundOperand a) => num.DecOperand(num.AsNum(a, num.Receiver));

    /// <summary>One operand as a C# double from its RAW carrier — the per-operand form of <see cref="Dbl"/>.</summary>
    private string DblOf(BoundOperand a) => NumericRenderer.Real(num.AsNum(a, num.Receiver));

    // ── table(ALL) arguments — the enumeration seam (ISO §15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>The C# expression enumerating a table(ALL) argument's elements as a <c>T[]</c>:
    /// <c>CobolTable.AllArgs</c> over the place's ranges — each a lambda over the index vector, so a nested
    /// dynamic-capacity table's capacity reads the OUTER occurrence's — with the element rendered by
    /// <paramref name="element"/> from the element operand (its subscripts are the index variable's slots).</summary>
    private static string AllArgsExpr(TableAllPlace all, string csType, Func<BoundOperand, string> element)
    {
        string v = all.IndexVar;
        var counts = all.Counts.Select(c => $"{v} => (long)({AllCountExpr(c)})");
        return RuntimeApi.TableAllArgs(csType, counts, $"{v} => {element(new BoundFieldOperand(all.Element))}");
    }

    /// <summary>One ALL level's range as a C# <c>long</c>-valued expression: the OCCURS count; data-name-1's value
    /// clamped to [integer-1, integer-2] with EC-BOUND-ODO outside (§13.18.38.4 GR7 — <c>CobolTable.OdoExtent</c>
    /// with a unit element, the same clamp the sending image applies); the dynamic table's current capacity.</summary>
    private static string AllCountExpr(AllCount c) => c switch
    {
        AllCount.Fixed f => f.Occurs.ToString(),
        AllCount.Odo o => RuntimeApi.TableOdoExtent(RuntimeApi.TableOcc(PlaceRenderer.Read(o.Depending)), o.MinOccurs, o.MaxOccurs, 0, 1),
        AllCount.Capacity cap => PlaceRenderer.Read(cap.Register),
        _ => throw new InvalidOperationException($"unknown ALL range {c.GetType().Name}"),
    };

    /// <summary>The argument list from position <paramref name="from"/> on as ONE <c>T[]</c> expression when a
    /// table(ALL) argument is among them (else null — the caller keeps its comma-list form, byte-identical to
    /// before): runs of written operands become array literals, each ALL an <see cref="AllArgsExpr"/>, joined
    /// in source order by <c>CobolTable.ArgConcat</c> — the ONE array a <c>params T[]</c> body binds to.</summary>
    private string? ArgArray(BoundIntrinsicCall ic, int from, string csType, Func<BoundOperand, string> render)
    {
        if (!ic.Args.Skip(from).Any(a => a is BoundFieldOperand { Place: TableAllPlace })) return null;
        var parts = new List<string>();
        var run = new List<string>();
        void Flush() { if (run.Count > 0) { parts.Add($"new {csType}[] {{ {string.Join(", ", run)} }}"); run.Clear(); } }
        foreach (var a in ic.Args.Skip(from))
        {
            if (a is BoundFieldOperand { Place: TableAllPlace all }) { Flush(); parts.Add(AllArgsExpr(all, csType, render)); }
            else run.Add(render(a));
        }
        Flush();
        return parts.Count == 1 ? parts[0] : RuntimeApi.TableArgConcat(csType, parts);
    }

    /// <summary>A body with a LEADING positional argument and a <c>params</c> tail (PRESENT-VALUE's rate, then the
    /// amounts): the tail may enumerate; a table(ALL) in the LEADING position itself is legal too (§15.3 puts no
    /// position on the ALL — "as if each table element … were specified"), so then the flat list is bound once
    /// and split at run time.</summary>
    private string LeadThenTail(BoundIntrinsicCall ic, string method, string prefix, string csType, Func<BoundOperand, string> render, string mid = "")
    {
        if (ic.Args[0] is BoundFieldOperand { Place: TableAllPlace })
        {
            string xs = NextWithVar();
            return RuntimeApi.With(ArgArray(ic, 0, csType, render)!, xs, RuntimeApi.Intrinsic(method, $"{prefix}{xs}[0], {mid}{xs}[1..]"));
        }
        string tail = ArgArray(ic, 1, csType, render) ?? string.Join(", ", ic.Args.Skip(1).Select(render));
        return RuntimeApi.Intrinsic(method, $"{prefix}{render(ic.Args[0])}, {mid}{tail}");
    }

    /// <summary>The number of arguments a call's list stands for when it is a compile-time fact — every table(ALL)
    /// argument with fixed ranges counted as its elements; null when an ALL ranges over a runtime count.</summary>
    private static long? StaticArgCount(BoundIntrinsicCall ic)
    {
        long n = 0;
        foreach (var a in ic.Args)
        {
            if (a is BoundFieldOperand { Place: TableAllPlace all }) { if (all.StaticCount is not { } k) return null; n += k; }
            else n++;
        }
        return n;
    }

    /// <summary>A fresh name for a <c>CobolTable.With</c> binding — nested bindings must not shadow.</summary>
    private string NextWithVar() => $"__xs{_withSerial++}";
    private int _withSerial;

    /// <summary>Does any argument arrive as a Dec (SDIDI) or float carrier, before any landing?</summary>
    private bool AnyDecOrRealRaw(BoundIntrinsicCall ic) =>
        ic.Args.Any(a => { var x = num.AsNum(a, num.Receiver); return x.Dec || x.Real; });

    /// <summary>Does any argument arrive on the SDIDI carrier (a native integer power — kb/Work PB69)?</summary>
    private bool AnyDecRaw(BoundIntrinsicCall ic) => ic.Args.Any(a => num.AsNum(a, num.Receiver).Dec);

    /// <summary>
    /// Under a standard arithmetic mode, evaluate a §15.4.1 r1 function's equivalent arithmetic expression ON
    /// the SDIDI carrier (<c>CobolIntrinsics.*Dec</c>) and return the result as a Dec-valued <see cref="NumX"/>
    /// that lands at the receiver ONCE — the PB56 Dec-carrier body, replacing the interim per-argument
    /// unscaled landing whose fixed working scale truncated sub-microscale operands to zero.
    /// </summary>
    /// <remarks>
    /// <para>Routing: the exact-EAE family routes here only when a RAW argument is Dec or float — an
    /// all-fixed-point list stays on the exact Int128 family, whose every EAE step is digit-identical to the
    /// SDIDI evaluation (documented equivalence, <c>COBOLNET_NUMERIC_DESIGN.md</c> D3; the >34-digit exact
    /// residue keeps MORE precision than per-op rounding would). The four inexact-EAE financial/statistical
    /// functions route here for EVERY argument shape — their EAEs divide, so even all-fixed lists must
    /// evaluate in SDIDI form; this is what removes their COBOLNET0899 stage. Returns null for functions
    /// with no Dec body (the prose family converts its binary64 result in <see cref="RenderFloat"/>).</para>
    /// <para>Result carriers: SIGN / ORD-MAX / ORD-MIN return INTEGERS (scale-0 exact); everything else
    /// returns the SDIDI itself (<c>Dec: true</c>), the same channel the R18 constant arms and MEAN's
    /// existing SDIDI division already prove end-to-end.</para>
    /// </remarks>
    private NumX? RenderDec(BoundIntrinsicCall ic)
    {
        string m = ic.Sig.RuntimeMethod;
        // Functions whose standard-mode value is FIXED for EVERY argument shape route here unconditionally: the
        // four inexact-EAE financial/statistical functions (their EAEs divide, so even all-fixed lists must
        // evaluate in SDIDI form), and the NUMVAL family — §15.67.4 r1 / §15.68.4 r1 / §15.69.4 r3 name the
        // returned value outright ("the numeric value represented by argument-1"; NUMVAL-F's r2/r3 pair grants
        // NATIVE arithmetic the approximation and STANDARD-DECIMAL none), and §15.4.1 places it in an SDIDI. Their
        // argument is a string, so the carrier question never arises; before this arm the standard-mode value
        // rode the native Int128 projection at the item-92 working scale (fix-queue PB60, RV-15.67.4-1a).
        // … and the SUMMING statistical family (kb/Work PB62 / RV-15.60.4-1): under a standard mode §15.4.1 r1
        // makes MEAN/SUM/RANGE/MEDIAN/MIDRANGE equal their §15.60.4 / §15.88.4 / … equivalent arithmetic
        // expressions evaluated in SDIDI form, and each argument converts to the SDIDI INDIVIDUALLY (§8.8.1.5.2 r1).
        // The native arms first ALIGN every argument to the list's maximum scale on the Int128 carrier, so a
        // 31-digit integer beside a scale-18 item needed 49 digits and RescaleEscape raised a size error where the
        // SDIDI holds the 30-digit answer exactly (MEAN(10³⁰, 2.0) = 500000000000000000000000000001).
        bool alwaysDec = m is "Annuity" or "PresentValue" or "Variance" or "StandardDeviation"
                           or "Numval" or "NumvalC" or "NumvalF"
                           or "SumScaled" or "RangeScaled" or "MeanScaled" or "MedianScaled" or "MidrangeScaled";
        if (!alwaysDec && !AnyDecOrRealRaw(ic)) return null;

        string mode = num.IntermediateMode;
        return m switch
        {
            // The NUMVAL family: the ONE positional scan's (sign, unscaled, frac[, exp]) lifted to the SDIDI exactly
            // at the parsed scale — no working scale, no receiver. The digit cap is 34 here by construction (this
            // arm runs only under the standard modes) and is still passed explicitly, exactly as the TEST- twins
            // receive it, so the scan's r1b sub-note-4 verdict and the value agree on the same number.
            "Numval" => Dec(RuntimeApi.Intrinsic("NumvalDec", $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            "NumvalC" => Dec(RuntimeApi.Intrinsic("NumvalCDec",
                $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}")),
            "NumvalF" => Dec(RuntimeApi.Intrinsic("NumvalFDec", $"{mode}, {Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            "SignOf" => new NumX(RuntimeApi.Intrinsic("SignDec", DecArg(ic, 0)), 0),
            "AbsScaled" => Dec(RuntimeApi.Intrinsic("AbsDec", DecArg(ic, 0))),
            "Floor" => Dec(RuntimeApi.Intrinsic("FloorDec", DecArg(ic, 0))),
            "Truncate" => Dec(RuntimeApi.Intrinsic("TruncDec", DecArg(ic, 0))),
            "FractionPart" => Dec(RuntimeApi.Intrinsic("FractionPartDec", $"{mode}, {DecArg(ic, 0)}")),
            "ModScaled" => Dec(RuntimeApi.Intrinsic("ModDec", $"{mode}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "RemScaled" => Dec(RuntimeApi.Intrinsic("RemDec", $"{mode}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "MaxScaled" => Dec(RuntimeApi.Intrinsic("MaxDec", DecArgList(ic))),
            "MinScaled" => Dec(RuntimeApi.Intrinsic("MinDec", DecArgList(ic))),
            "OrdMax" => new NumX(RuntimeApi.Intrinsic("OrdMaxDec", DecArgList(ic)), 0),
            "OrdMin" => new NumX(RuntimeApi.Intrinsic("OrdMinDec", DecArgList(ic)), 0),
            "SumScaled" => Dec(RuntimeApi.Intrinsic("SumDec", $"{mode}, {DecArgList(ic)}")),
            "RangeScaled" => Dec(RuntimeApi.Intrinsic("RangeDec", $"{mode}, {DecArgList(ic)}")),
            "MeanScaled" => Dec(RuntimeApi.Intrinsic("MeanDec", $"{mode}, {DecArgList(ic)}")),
            "MedianScaled" => Dec(RuntimeApi.Intrinsic("MedianDec", $"{mode}, {DecArgList(ic)}")),
            "MidrangeScaled" => Dec(RuntimeApi.Intrinsic("MidrangeDec", $"{mode}, {DecArgList(ic)}")),
            "Variance" => Dec(RuntimeApi.Intrinsic("VarianceDec", $"{mode}, {DecArgList(ic)}")),
            "StandardDeviation" => Dec(RuntimeApi.Intrinsic("StdDevDec", $"{mode}, {DecArgList(ic)}")),
            "Annuity" => Dec(RuntimeApi.Intrinsic("AnnuityDec", $"{mode}, {DecArg(ic, 0)}, {IntArg(ic, 1)}")),
            "PresentValue" => Dec(LeadThenTail(ic, "PresentValueDec", $"{mode}, ", "CobolDec", DecOf)),
            _ => null,
        };

        static NumX Dec(string expr) => new(expr, 0, Dec: true);
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

    /// <summary>Does EVERY argument render as floating? (An all-float selection list stays in the float lane —
    /// each argument's content is its double; a mixed list rides the SDIDI, kb/Work PB65 RV-15.59.4-1 D2.)</summary>
    private bool AllRealArguments(BoundIntrinsicCall ic) =>
        ic.Args.All(a => Landed(num.AsNum(a, num.Receiver)).Real);

    /// <summary>A numeric argument as a C# double (the float family's §15.4.1 carrier). ⛔ Converts the RAW
    /// operand (fix-queue PB56): routing through the <see cref="Landed"/> unscaled truncation first turned a
    /// sub-working-scale Dec operand to ZERO before the double conversion — SQRT(4e-18) probed as 0 where the
    /// approximation of 2e-9 is required — and <see cref="NumericRenderer.Real"/> carries its own exact arm
    /// for every carrier (a Dec operand converts via <c>ToDouble</c>, a float passes through).</summary>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(RawArg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §15.3; a fractional value is the program's EC latitude).</summary>
    private string IntArg(BoundIntrinsicCall ic, int i) => AsInt(RawArg(ic, i));   // an integer lands at scale 0 directly — never through a working scale that eats headroom (kb/Work PB69)

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
        // The Dec arm (kb/Work R24 — ledger F44): an SDIDI intermediate (a §15.3 type-6/type-10 expression
        // under a standard mode) lands at scale 0 through its own exact conversion. ⛔ BEFORE the Scale == 0
        // test — a Dec NumX carries Scale 0 BY CONVENTION (the PB14/PB32 lesson), so the scale test would pass
        // the CobolDec expression through raw and hand Roslyn the CS1503 this arm exists to close. This was
        // the one carrier-total dispatch in the renderer family still missing its Dec arm.
        : a.Dec ? RuntimeApi.IntegerArg(RuntimeApi.DecToUnscaledIntermediate(a.Expr, "0", CobolRounding.Truncation))
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
        // A table(ALL) operand renders as its ELEMENT here (the index variable inside) — right for the scale and
        // the Real flag; the list itself enumerates through ArgArray (kb/Work PB62).
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        string list = ArgArray(ic, 0, "Int128", a => NumericRenderer.Align(num.AsNum(a, num.Receiver), s))
                      ?? string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s)));
        return (list, s, xs.Any(x => x.Real));
    }

    /// <summary>The variadic arguments UNALIGNED — parallel value/scale arrays for the selection family
    /// (fix-queue PB65). Each argument renders at its OWN scale (<c>Align(x, x.Scale)</c> is the identity for
    /// an exact operand and the standard carrier conversion for Real/Dec/U), so no widening ever happens on
    /// intake; the common scale is reported for the result's compile-time <see cref="NumX"/>.</summary>
    private (string Vals, string Scales, int Scale) RawArgPairs(BoundIntrinsicCall ic)
    {
        var xs = ic.Args.Select(a => num.AsNum(a, num.Receiver)).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        // A table(ALL) argument enumerates (kb/Work PB62): its elements share one scale, so the scales array
        // enumerates the same ranges with a constant selector.
        string vals = ArgArray(ic, 0, "Int128", a => { var x = num.AsNum(a, num.Receiver); return NumericRenderer.Align(x, x.Scale); })
                      ?? $"new Int128[] {{ {string.Join(", ", xs.Select(x => NumericRenderer.Align(x, x.Scale)))} }}";
        string scales = ArgArray(ic, 0, "int", a => num.AsNum(a, num.Receiver).Scale.ToString())
                        ?? $"new int[] {{ {string.Join(", ", xs.Select(x => x.Scale))} }}";
        return (vals, scales, s);
    }

    /// <summary>The variadic string-argument list. <paramref name="admitNumeric"/> defaults FALSE and is passed
    /// true by exactly one caller (CONCAT — §15.18.3 r1 lists class numeric): four arms with three different
    /// §15.x.3 argument rules share this helper, so the admission is a parameter, never a global flip
    /// (§15.26.3 r1 / §15.66.3 r1 exclude class numeric outright — PB59).</summary>
    private string StrArgList(BoundIntrinsicCall ic, bool admitNumeric = false) =>
        ArgArray(ic, 0, "string", a => admitNumeric ? StrNum(a) : Str(a))   // a table(ALL) enumerates (kb/Work PB62)
        ?? string.Join(", ", ic.Args.Select(a => admitNumeric ? StrNum(a) : Str(a)));

    private string CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The ANYCASE named argument (§15.68.3 r4f / §15.37.4 r4 — the ONE <see cref="BoundIntrinsicCall.Anycase"/>
    /// flag), rendered only when present so the default-free call stays byte-stable.</summary>
    private static string AnycaseFlag(BoundIntrinsicCall ic) => ic.Anycase ? ", anycase: true" : "";

    /// <summary>The §15.93.4/§15.94.4 r1b digit-cap named argument: 34 under the SDIDI standard-arithmetic
    /// modes (sub-note 4), omitted for the native default 31 (sub-note 2).</summary>
    private string DigitCapFlag => num.StandardDecimal ? ", digitCap: 34" : "";

    /// <summary>The landing form past the Int128 carrier for a quantizer / exact-family parse (kb/Work PB77) — the
    /// ONE <c>NumericRenderer.CheckedFlag</c>, read from the receiver context this render runs under.</summary>
    private string CheckedFlag => num.CheckedFlag;

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
        // The MAX/MIN family takes the program's CobolCollation carrier, exactly as CHAR/ORD do (PB101 — the
        // ushort[] raw-table overload is gone; the carrier's Compare is the order-equivalent tail, PB59).
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
            // UPPER-CASE / LOWER-CASE (§15.97 / §15.57; kb/Work PB64 T5): with a LOCALE phrase, the named locale's LC_CTYPE
            // (r2); else, when this module has a CHARACTER CLASSIFICATION, the classification's LC_CTYPE for the
            // operand's class (r3 — __CLASSIFY, resolved at activation); else the implementor's correspondence (r4 —
            // the invariant map the function always used).
            "UpperCase" or "LowerCase" when ic.Locale.Tag is { } caseTag =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {EmitText.CsLiteral(caseTag)}"),
            "UpperCase" or "LowerCase" when ctx.Data.Classification is not null =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, __CLASSIFY.For({(IsNationalArg(ic.Args[0]) ? "true" : "false")})"),
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
            "Concat" =>                                                        // §15.18 — concatenate all argument images (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   r1 admits class NUMERIC → the admitting list (PB59)
                    StrArgList(ic, admitNumeric: true)),
            // BOOLEAN-OF-INTEGER (§15.13.4 r1) — a boolean-result function on the D-B1 '0'/'1' substrate:
            // rightmost position = low-order digit, left zero-fill/truncate to argument-2 positions.
            // Argument-1 crosses the WIDE bridge (Int128 — §15.13.3 r1 admits any positive integer, PB65);
            // argument-2 stays on the long bridge (§15.4 caps the returned length at 8 191 positions).
            "BooleanOfInteger" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgIntWide(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            // DISPLAY-OF (§15.26) / NATIONAL-OF (§15.66) — the national↔alphanumeric repertoire pair (2002);
            // the optional argument-2 is the one-character substitution string (§15.26.3 r2 / §15.66.3 r2).
            "DisplayOf" or "NationalOf" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, StrArgList(ic)),
            "BaseConvert" =>                                                   // §15.12 — unsigned-integer base conversion (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   r1 admits an unsigned integer LITERAL argument-1 below
                    $"{StrNum(ic.Args[0])}, {ArgInt(ic.Args[1])}, {ArgInt(ic.Args[2])}"),   // base 11 (PB59); args 2/3 stay integers
            "Trim" =>                                                          // §15.96 — delete leading/trailing/both of the char set (default: space)
                ic.Args.Any(a => a is BoundFieldOperand { Place: TableAllPlace })   //   a table(ALL) argument-2 list enumerates (kb/Work PB62)
                    ? LeadThenTail(ic, sig.RuntimeMethod, "", "string", Str, mid: $"{ic.TrimMode}, ")
                    : RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ic.TrimMode}"
                        + string.Concat(ic.Args.Skip(1).Select(a => $", {Str(a)}"))),
            // STANDARD-COMPARE (§15.85) — the cultural-ordering comparison over the derived CLDR/UCA engine
            // (kb/Work PB101 T7). The ordering TABLE travels as the bind-time-resolved literal-9 (null ⇒ §15.85.3
            // r5's default table); the ordering LEVEL travels as 0 when argument-4 is omitted, which §15.85.4 r1
            // defines as "the highest level defined in the ordering table". Both are complete on the bound node,
            // so the backend never consults the SPECIAL-NAMES model.
            "StandardCompare" =>
                RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, "
                    + $"{(ic.OrderingTable is { } ot ? EmitText.CsLiteral(ot) : "null")}, "
                    + $"{(ic.Args.Count > 2 ? ArgInt(ic.Args[2]) : "0")}"),
            // The LOCALE functions (§15.51–§15.54; kb/Work PB64 T4): the bound LocaleRef travels as the named locale's
            // L1-normalized tag (null ⇒ the locale current for the category at use — §14.6.6 r7/r8); the runtime
            // resolves availability (EC-LOCALE-MISSING) and content (EC-LOCALE-INVALID) at use.
            "Compare" when sig.Name == "LOCALE-COMPARE" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {LocaleTagArg(ic)}"),
            "Date" when sig.Name == "LOCALE-DATE" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "Time" when sig.Name == "LOCALE-TIME" =>
                RuntimeApi.LocaleFn(sig.RuntimeMethod, $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "TimeFromSeconds" => RenderLocaleTimeFromSeconds(ic),               // §15.54 — standard numeric time form per t_fmt
            "Substitute" => RenderSubstitute(ic),                              // §15.87 — replace argument-2 pairs (2023)
            "Convert" =>                                                       // §15.19 — repertoire / hex / byte conversion (2023);
                RuntimeApi.Intrinsic(sig.RuntimeMethod,                        //   an ANY source takes the RAW STORAGE image (r7, PB59 5b)
                    $"{(ic.ConvertSource == 0 ? StorageArg(ic.Args[0]) : Str(ic.Args[0]))}, "
                    + $"{ic.ConvertSource}, {ic.ConvertDest}, {(ic.ConvertDestHex ? "true" : "false")}"),
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

    /// <summary>FORMATTED-TIME (§15.41): seconds as (unscaled, scale) via <see cref="SecondsArg"/> — NOT the
    /// truncating ArgInt, so the fractional seconds survive — plus the optional offset-minutes (a3).</summary>
    private string RenderFormattedTime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[1]));
        bool hasOff = ic.Args.Count > 2;
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, {secExpr}, {secScale}, "
             + $"{(hasOff ? ArgInt(ic.Args[2]) : "0")}, {(hasOff ? "true" : "false")}{LeapSecondFlag}");
    }

    /// <summary>Is the operand of class national (its category National) — the CHARACTER CLASSIFICATION's national
    /// phrase governs it (ISO §12.3.6.4 GR5 f–j), the alphanumeric phrase everything else.</summary>
    private static bool IsNationalArg(BoundOperand op) => op switch
    {
        BoundStringLiteral { Category: PicCategory.National } => true,
        BoundFieldOperand { Place.Item.Pic.Category: PicCategory.National } => true,
        _ => false,
    };

    /// <summary>The bound <see cref="BoundIntrinsicCall.Locale"/> as the runtime's <c>localeTag</c> argument: the named
    /// locale's L1-normalized tag literal, or <c>null</c> for the current-locale form.</summary>
    private static string LocaleTagArg(BoundIntrinsicCall ic) =>
        ic.Locale.Tag is { } tag ? EmitText.CsLiteral(tag) : "null";

    /// <summary>LOCALE-TIME-FROM-SECONDS (§15.54): the seconds argument through the ONE <see cref="SecondsArg"/> pair
    /// (fraction-preserving — the nanosecond note of Annex D.31.4.5), the locale tag, the LEAP-SECOND flag.</summary>
    private string RenderLocaleTimeFromSeconds(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[0]));
        return RuntimeApi.LocaleFn(ic.Sig.RuntimeMethod, $"{secExpr}, {secScale}, {LocaleTagArg(ic)}{LeapSecondFlag}");
    }

    /// <summary>FORMATTED-DATETIME (§15.40): integer date a2 + seconds a3 (via <see cref="SecondsArg"/>) + the
    /// optional offset-minutes a4.</summary>
    private string RenderFormattedDatetime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[2]));
        bool hasOff = ic.Args.Count > 3;
        return RuntimeApi.DateFn(ic.Sig.RuntimeMethod, $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}, "
             + $"{secExpr}, {secScale}, {(hasOff ? ArgInt(ic.Args[3]) : "0")}, {(hasOff ? "true" : "false")}{LeapSecondFlag}");
    }

    /// <summary>The <c>&gt;&gt;LEAP-SECOND ON</c> argument every §15.3 date/time function that reads a seconds subfield or
    /// a standard numeric time form takes (ISO §7.3.17 / §15.3.3.3 — kb/Work PB65): the compilation group's ONE
    /// directive fact (<c>DataBinder.LeapSecond</c>), passed as a trailing named argument only when ON, so every
    /// OFF emission is byte-identical to before (the CommaFlag discipline).</summary>
    private string LeapSecondFlag => ctx.Data.LeapSecond ? ", leapSecond: true" : "";

    /// <summary>The SECONDS argument of the formatted time family as an (unscaled, scale) pair, TOTAL over the
    /// four value carriers (kb/Work R24 — ledger F44/F46/F57; §15.41.4 r1 / §15.40.4 make the returned value a
    /// representation of the VALUE "contained in" the argument, fraction included):
    /// <list type="bullet">
    ///   <item>a FIXED-POINT operand passes through at its own scale with NO narrowing cast — the former
    ///         <c>(long)(expr)</c> silently WRAPPED a wide picture's unscaled form (a 9(5)V9(15) item holding
    ///         the in-range 45296.5 fabricated 02:20:03), and the runtime now takes <see cref="Int128"/>;</item>
    ///   <item>a FLOAT operand lands through the checked <c>CobolFloat.ToScaled</c> at scale 9 — the
    ///         FORMATTED-CURRENT-DATE nanosecond convention, and every reliable binary64 fraction digit under a
    ///         5-integer-digit bound — where the former cast truncated to WHOLE seconds;</item>
    ///   <item>an SDIDI operand (a legal §15.3 type-10 expression under a standard mode) lands exactly at scale
    ///         18, the documented §15.3.3.2 maximum fraction width (CONFORMANCE.md item 202) — it used to reach
    ///         the cast as a raw CS1503, the PB2 shape on the Dec axis;</item>
    ///   <item>an unsigned-wide operand narrows through the R10 Widen funnel (a valid time always fits).</item>
    /// </list></summary>
    private static (string Expr, int Scale) SecondsArg(NumX x)
    {
        x = NumericRenderer.DeU(x);
        if (x.Real) return (RuntimeApi.FloatToScaled(x.Expr, "9", CobolRounding.Truncation, checkedLanding: true), 9);
        if (x.Dec) return (RuntimeApi.DecToUnscaledIntermediate(x.Expr, "18", CobolRounding.Truncation), 18);
        return (x.Expr, x.Scale);
    }

    /// <summary>SUBSTITUTE (§15.87): the source (Args[0]) plus parallel from/to/mode arrays over the pair operands
    /// (Args[1..] taken two at a time; one <see cref="BoundIntrinsicCall.SubstituteModes"/> entry per pair).</summary>
    private string RenderSubstitute(BoundIntrinsicCall ic)
    {
        // The FLAT form (kb/Work PB81 — a table(ALL) among the pairs): every part after argument-1 is ONE string[]
        // (a written operand's singleton, or the enumeration), paired at run time by SubstituteFlat with the
        // per-part flags.
        if (ic.SubstituteFlat)
        {
            var parts = ic.Args.Skip(1).Select(a => a is BoundFieldOperand { Place: TableAllPlace all }
                ? AllArgsExpr(all, "string", Str)
                : $"new string[] {{ {Str(a)} }}");
            return RuntimeApi.Intrinsic("SubstituteFlat", $"{Str(ic.Args[0])}, "
                + $"new string[][] {{ {string.Join(", ", parts)} }}, "
                + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}");
        }
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

    /// <summary>The WIDE integer-argument bridge — the Int128-carrier twin of <see cref="ArgInt"/>, for the ONE
    /// §15 integer argument whose domain exceeds <c>long</c>: BOOLEAN-OF-INTEGER's argument-1 (§15.13.3 r1 —
    /// any positive integer; §15.13.4 r1's bit configuration is mathematical, so 2⁶³ and up are legal values
    /// the <see cref="AsInt"/> narrowing EC'd to the checking-off default 0 — fix-queue PB65 / RV-15.13.4-1 D1).
    /// Every legal fixed-point value rides Int128 exactly, so a scale-0 operand passes through RAW — there is
    /// no narrowing and therefore no PB22 raise point; the carrier arms mirror <see cref="AsInt"/> exactly
    /// (Dec BEFORE the scale test — a Dec NumX carries Scale 0 by convention, the PB14/PB32 lesson; the
    /// unsigned-wide lane narrows through the R10 <c>CobolNum.Widen</c> funnel, loud past the intermediate).</summary>
    private string ArgIntWide(BoundOperand op) => AsIntWide(ArgNum(op));

    private static string AsIntWide(NumX a) =>
        a.Real ? RuntimeApi.IntegerArgWide(a.Expr)
        : a.Dec ? RuntimeApi.DecToUnscaledIntermediate(a.Expr, "0", CobolRounding.Truncation)
        : a.U ? RuntimeApi.NumWiden(a.Expr)
        : a.Scale == 0 ? a.Expr
        : RuntimeApi.NumRescale(a.Expr, a.Scale.ToString(), "0", CobolRounding.Truncation);

    /// <summary>A string-kind argument (the §15.3 alphanumeric-argument shapes): literals, field display
    /// images, and nested alphanumeric intrinsics. A numeric-category operand in a string-argument position
    /// stays loud — §15's string functions take alphanumeric/national/boolean arguments (the named channel).
    /// ⛔ THE DEFAULT IS NON-ADMITTING AND MUST STAY SO: NUMVAL/NUMVAL-F carry open rows (AR-15.67.3-1 /
    /// AR-15.69.3-1) DEMANDING a compile-time rejection of a numeric literal — widening this default would
    /// turn their wrong-stage defect into a silently-wrong one.</summary>
    private string Str(BoundOperand op) => op.Accept(_strArg ??= new StrArgVisitor(this, admitNumeric: false));
    private StrArgVisitor? _strArg;

    /// <summary>CONVERT's ANY-source argument — the RAW STORAGE byte image (§15.19.3 r7: any usage, contents
    /// need not be valid for it — the storage BITS, never the display image; PB59 family 5b). A FIELD renders
    /// through the ONE storage channel (<see cref="OperandText.AsStorageImage"/>); a literal renders its
    /// declared storage (an N"…" literal its UTF-16BE bytes, a B"…" literal its packed bits, an alphanumeric
    /// literal its characters under the item-209 1-byte serialization). Every other shape (computed values,
    /// figuratives, ALL literals) HAS no storage — those keep the display-image channel they always rode, which
    /// for a numeric shape stays the deliberately-loud <see cref="Str"/> default.</summary>
    private string StorageArg(BoundOperand op) => op switch
    {
        BoundFieldOperand f => OperandText.AsStorageImage(f.Place),
        BoundStringLiteral { Category: PicCategory.National } sl => RuntimeApi.NatBytes(EmitText.CsLiteral(sl.Value)),
        BoundStringLiteral { Category: PicCategory.Boolean } sl =>
            RuntimeApi.BitsPack(EmitText.CsLiteral(sl.Value), sl.Value.Length.ToString()),
        _ => Str(op),
    };

    /// <summary>The NUMERIC-ADMITTING string-argument entry (fix-queue PB59 / RV-15.12.4-1, RV-15.18.4-1) —
    /// for EXACTLY the functions whose §15.x.3 argument rules admit a numeric literal: BASECONVERT argument-1
    /// (§15.12.3 r1 — "a usage display or national data item or literal, and, if the base … is less than 11,
    /// … an unsigned integer data item or literal") and CONCAT (§15.18.3 r1 — class numeric is in its list).
    /// The literal renders through the ONE <c>OperandText.AsString</c> image (raw source text — correct for
    /// §15.18.4 r1; r3's value conditions are NOT the renderer's to enforce and stay recorded on the
    /// DeliberatelyUnscreened rows). Never a blanket route — the drift test pins the caller set.</summary>
    private string StrNum(BoundOperand op) => op.Accept(_strArgNum ??= new StrArgVisitor(this, admitNumeric: true));
    private StrArgVisitor? _strArgNum;

    /// <summary>The string-argument dispatch — an exhaustive generated-visitor implementation (a new
    /// <see cref="BoundOperand"/> leaf is a compile error), INSTANCE-bound so the nested-intrinsic arm
    /// re-enters the instance <see cref="RenderString"/>. ONE class, two cached flagged instances (the
    /// OperandText pattern): <paramref name="admitNumeric"/>'s ONLY difference is the
    /// <see cref="BoundNumericLiteral"/> arm — a second visitor CLASS would silently join the drift test's
    /// end-of-file body slice.</summary>
    private sealed class StrArgVisitor(IntrinsicRenderer owner, bool admitNumeric) : IBoundOperandVisitor<string>
    {
        private static string Loud(BoundOperand n) => EmitText.LoudValue("string", $"intrinsic string argument '{n.GetType().Name}'");
        public string Visit(BoundStringLiteral n) => EmitText.CsLiteral(n.Value);
        public string Visit(BoundFieldOperand n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } nested
                ? owner.RenderString(nested) : Loud(n);   // string-class results incl. national (§15.66) + boolean (§15.13 — the '0'/'1' substrate)
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        // Admitted PER-FUNCTION (PB59): the raw source-text image via the ONE OperandText channel where the
        // function's §15.x.3 rule admits a numeric literal (see StrNum); Loud everywhere else — see Str's ⛔.
        public string Visit(BoundNumericLiteral n) => admitNumeric ? OperandText.AsString(n, owner.Num) : Loud(n);
        // ⛔ A FIGURATIVE CONSTANT IS A LEGAL INTRINSIC ARGUMENT, AND ITS IMAGE IS ALREADY WRITTEN DOWN ONCE
        // (fix-queue PB25). §8.3.3.6.3 SR1 admits a figurative constant "whenever 'literal' appears in a format",
        // and §8.4.3.2.3 SR8 makes a literal a valid argument-1 — so `FUNCTION LOWER-CASE(SPACE)` is legal source.
        // Both arms used to render EmitText.LoudValue, so it compiled CLEAN and aborted at RUN TIME with
        // "intrinsic string argument 'BoundFigurative'": the wrong-stage family, on legal input.
        // The image is §8.3.3.6.4 GR3's, for the case where "the length of the string is NOT specified in the
        // rules for the context" — which is exactly a bare function argument: (b) a figurative other than
        // ALL literal-1 is ONE character, (c) otherwise the length of literal-1. OperandText.AsString already
        // implements precisely that, PCS-aware (its BoundFigurative interception is collating-context sensitive,
        // which a local copy here could not be, since this visitor cannot reach the renderer's collating state).
        // So these delegate rather than re-deriving it — the same delegation the BoundFieldOperand arm above
        // already makes.
        public string Visit(BoundFigurative n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundAllLiteral n) => OperandText.AsString(n, owner.Num);
        // A boolean EXPRESSION argument (§8.4.3.2.3 SR8; kb/Work PB65): its '0'/'1' image through the ONE boolean
        // renderer — INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B) reads the combined bit string.
        public string Visit(BoundBoolOperand n) => BooleanRenderer.Render(n.Expr, owner.Num);
    }
}
