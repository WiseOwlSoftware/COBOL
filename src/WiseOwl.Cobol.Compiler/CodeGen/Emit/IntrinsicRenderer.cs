// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.CodeGen.Emit;

/// <summary>
/// Renders a <see cref="BoundIntrinsicCall"/> (ISO §15; INTRINSICS_DESIGN spine 1), dispatching on the
/// catalog row — the SINGLE render channel (P7 Step 12: the former parallel static evaluator is deleted;
/// DESIGN-codegen-backend §2.5). Three §2023:15.2-type shapes (deep-dive D1):
/// <list type="bullet">
///   <item><b>floating-math</b> (Float rows, §2023:15.4.1 native-arithmetic license) — compute in double; a
///         FIXED-POINT receiver then quantizes through the ONE <c>CobolIntrinsics.FromDouble</c> at the landing
///         <see cref="ReceiverContext.FloatLanding"/> chooses (the resultant identifier's scale + the statement's
///         ROUNDED mode when this value IS the transfer — so the arithmetic channel and the MOVE channel land one
///         returned value identically, §2023:15.4.1 / kb/Work PB647; otherwise the ≥9 float floor CAPPED at the
///         receiver's Int128 headroom, with TRUNCATION, so a saturation can never store silently — PB13), while a
///         FLOAT receiver and a
///         RECEIVER-LESS context both keep the binary64 value: §2023:15.4.1 leaves the returned value's
///         representation to the implementor, and with no receiver there is no scale to quantize TO
///         (P7.3 <c>ReceiverContext.None</c> — IF conditions / EVALUATE subjects / DISPLAY operands);</item>
///   <item><b>exact numeric / integer</b> — unscaled Int128 values at a known scale, aligned with the same
///         <see cref="NumericRenderer.Align"/> machinery the arithmetic verbs use (§2023:8.8.1);</item>
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

    // ── The numeric channel (COMPUTE / arithmetic / numeric comparisons / MOVE-to-numeric) ──────────────────

    /// <summary>Render a numeric-result intrinsic as a scaled value.</summary>
    public NumX RenderNum(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred)
            return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);
        // ⛔ LENGTH / BYTE-LENGTH OF A NUMERIC OR INTEGER FUNCTION MEASURE ITS RETURNED-VALUE TEMPORARY, BEFORE ANY CARRIER ROUTING (kb/Work PB3951).
        // §2023:8.4.3.2.4 GR1: a function-identifier references a temporary data item (§2023:15.4); §2023:15.14.4 RV1 measures it in bytes (ByteLength) and
        // §2023:15.50.4 RV2 / r3 in national or alphanumeric character positions (Length). It is the item DISPLAY shows (FunctionValueField, DOC-A.1-92), so its length is read from that field
        // and never from a width of ours; the argument's value only fills the field, so a binary64 argument (SQRT) is no reason to
        // leave this arm for the float lane below. The binder sends every such argument here (IntrinsicBinder.FunctionResultLength).
        if (sig.Name is "LENGTH" or "BYTE-LENGTH" && FunctionDeviceText.IsNumericFunctionResult(ic.Args[0]))
            return new NumX(RuntimeApi.FunctionFieldLength(new FunctionDeviceText(num).Field(ic.Args[0]), bytes: sig.Name is "BYTE-LENGTH"), 0);
        // A string-class function result in a NUMERIC context reaches here only under --permissive (the binder's
        // §2023:8.8.1.1 screen rejects it under strict conformance — kb/Work PB68): the DA6 leniency, the same
        // digit-character decode an alphanumeric DATA ITEM gets there (CobolNum.FromAlphanumeric over the
        // function's string image, an unsigned integer). It used to be a loud RUNTIME value on legal-shaped
        // source, i.e. an unhandled exception at the wrong stage.
        if (ic.ResultCategory is PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
            // sending: false — the §2023:8.8.1.1 screen already rejected this shape under STRICT conformance, so
            // this arm IS the DA6 permissive leniency and not a MOVE; §2023:14.9.25.4 GR6d 1's condition has no
            // scope over an arithmetic operand (kb/Work PB844).
            return new NumX(RuntimeApi.NumFromAlphanumeric(RenderString(ic), sending: false), 0);

        // ⛔ THE §2023:15.4.1 NATIVE LATITUDE IS WITHDRAWN WHERE THE FUNCTION DEFINITION FIXES THE VALUE (NUMVAL, NUMVAL-C) AND
        // IS RESOLVED TO THE EXACT PARSE WHERE IT LEAVES AN APPROXIMATION (NUMVAL-F, kb/Work PB3839, below) — SO THIS ARM
        // RUNS BEFORE THE ARITHMETIC-MODE DISPATCH, NOT INSIDE ITS STANDARD BRANCH (kb/Work PB251, rows
        // RV-15.4.1-2 / RV-15.4.1-L2.2). §2023:15.4.1's closing sentence is a CONDITIONAL — "When a numeric or integer
        // function does not have an equivalent arithmetic expression, its returned value is implementor-defined
        // unless otherwise specified in the function definition" — and §2023:15.67.4 r1 / §2023:15.68.4 r1 are exactly such
        // a specification, stated once for every arithmetic mode with no qualification: "The returned value is the
        // numeric value represented by argument-1". NUMVAL and NUMVAL-C have no equivalent arithmetic expression,
        // so the latitude the sentence grants is REMOVED for them and their value is the parsed value EXACTLY —
        // under NATIVE arithmetic too. Only the REPRESENTATION stays the implementor's ("When native arithmetic is
        // in effect, the characteristics and representation of the returned value are defined by the implementor"),
        // and the SDIDI is the ONE representation that holds every conforming argument: §2023:15.67.3 AR3 admits 31
        // total digits under native and r4 admits 34 under a standard mode, both of which the SDIDI's runtime
        // exponent carries without a working scale at all.
        // ⛔ NO COMPILE-TIME WORKING SCALE CAN DO THIS, WHICH IS WHY THE FIX IS THE CARRIER AND NOT A BIGGER FLOOR.
        // A scaled Int128 temporary holds i + ws digits for an argument with i integer digits, and §2023:15.67.3 AR3
        // permits i = 31, so the only ws safe for EVERY conforming argument is 7 — while `NUMVAL("0.123456789")`
        // needs 9. The former arms took ws = max(receiver scale, 6), which made a function's value depend on the
        // SHAPE of its receiver and truncated in every channel with no receiver scale to inherit: measured at HEAD
        // before this landed, `DISPLAY FUNCTION NUMVAL("0.1234567")` printed 0.123456, and — a real receiver being
        // no protection, because the receiver bounds the STATEMENT's result and not an OPERAND's precision —
        // `COMPUTE R = FUNCTION NUMVAL("0.1234567") * 10000000` stored 1234560 into PIC 9(9) where r1 owes
        // 1234567.
        // ⛔ NUMVAL-F IS HERE TOO, AND FOR A DIFFERENT REASON (kb/Work PB3839). §2023:15.69.4 RV2 grants native arithmetic "an
        // approximation of the numeric value represented by argument-1" and r3 gives standard-decimal arithmetic the value
        // itself, so the native digits are the IMPLEMENTOR's (§2023:15.4.1) and the only question is which approximation. The
        // float family's (a binary64 wherever no fixed-point scale governs, an Int128 truncation at the receiver-capped
        // working scale where one does) made ONE reference two values: `DISPLAY FUNCTION NUMVAL-F("0.12340000000001")`
        // printed the full value while `COMPUTE R ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.12340000000001")` into
        // `9V9(4)` truncated at the working scale first and stored 0.1234 where NUMVAL and GnuCOBOL store 0.1235: the
        // receiver-shape dependence CONFORMANCE.md item 92's NUMVAL paragraph rejects, and a double rounding
        // (§2023:14.7.4.3's rounding happens ONCE, at the receiver). The determination is the exact parse, r3's value, in every
        // mode and channel; a floating-point receiver is the SDIDI landing's own one conversion of that exact value.
        if (IntrinsicCarrierRules.ReturnsExactParse(sig.Name) && RenderDec(ic) is { } fixedByDefinition)
            return fixedByDefinition;

        // ⛔ ROUTE ON THE ARGUMENT'S TYPE, NOT ONLY THE FUNCTION'S FAMILY (fix-queue PB2). The exact family
        // computes over scale-aligned Int128, which is right (deep-dive D1) — but a FLOATING-POINT argument is
        // legal for all of it (§2023:15.7.3 AR1 and siblings require class NUMERIC, and a COMP-2 item is class
        // numeric), and dispatching on sig.Float alone handed a double expression to an Int128 parameter. The
        // user saw the generated C# fail: "CS1503: cannot convert from 'double' to 'System.Int128'" — an
        // internal failure escaping as a backend error on legal source. Ten of eleven functions probed did it.
        // Under native arithmetic §2023:15.4.1 makes the returned value implementor-defined and each of these
        // functions is an equivalent arithmetic expression over its own operands, so once an argument arrives as
        // binary64 the EAE *is* a binary64 evaluation — nothing exact is surrendered, because there was nothing
        // exact left to keep. The real-argument bodies are CobolIntrinsics.RealArgs.cs, deliberately sharing
        // these method names so this one line is the whole dispatch.
        // ⛔ THE STANDARD-MODE ARM RUNS BEFORE THE FLOAT DISPATCH (kb/Work R18 — ledger F15/F19). Four float-family
        // rows have their standard-mode value FIXED by the spec, with no §2023:15.4.1 r2 latitude: FUNCTION E and
        // FUNCTION PI are exact 34-digit constants (§2023:15.27.3 RV3 / §2023:15.73.3 RV3), and EXP / EXP10 carry equivalent
        // arithmetic expressions — `FUNCTION E ** argument-1` (§2023:15.34.4 RV1) and `10 ** argument-1` (§2023:15.35.4 RV1) —
        // that §2023:15.4.1 r1 requires the returned value to EQUAL under a standard mode. Each renders through the
        // SAME CobolDec.Pow the hand-written `**` uses (§2023:8.8.1.5.4), over the SAME exact constants, so the
        // function and its spelled-out EAE agree by construction — binary64 Math.Pow is not SDIDI arithmetic, and
        // routing these through RenderFloat gave receiver-DEPENDENT answers (§2023:15.4.1's same-for-all-instances
        // rule) wrong from the third significant digit's ulp up. A float ARGUMENT lifts via DecOperand →
        // CobolDec.FromBinary64 (§2023:8.8.1.5.1), exactly as the landed exact-family lane does.
        // (An if-chain, deliberately NOT a switch on the function name: IntrinsicRealArgDriftTests anchors its
        // exact-arm scan on the FIRST such switch, and these arms are not exact-family arms — a Real argument
        // lifts through DecOperand, so no `…Real` body exists or is needed.)
        if (num.StandardDecimal)
        {
            // ⛔ THE EXPONENT ARGUMENT LIFTS FROM THE RAW OPERAND, NOT THE LANDED ONE (fix-queue PB56).
            // DecOperand(Arg(...)) round-tripped a Dec operand through the interim unscaled landing — truncated
            // at the working scale — and only then lifted it back, which is the very quantization this arm
            // exists to avoid (the RV-15.34.4-1 triage row measured it).
            if (sig.Name == "E") return new NumX(RuntimeApi.DecE, 0, Dec: true);
            if (sig.Name == "PI") return new NumX(RuntimeApi.DecPi, 0, Dec: true);
            // Under ARITHMETIC IS STANDARD the expression's FUNCTION E is the 32-digit standard intermediate data item, and an
            // integer power develops one standard intermediate per multiplication (§2002:15.29.3 RV1, §2002:8.8.1.3.6; kb/Work PB5575).
            string e = num.StandardIntermediate ? RuntimeApi.DecToSidi(RuntimeApi.DecE, num.IntermediateMode) : RuntimeApi.DecE;
            if (sig.Name == "EXP")
                return new NumX(RuntimeApi.DecPow(e, DecArg(ic, 0), num.IntermediateMode, num.StandardIntermediate), 0, Dec: true);
            if (sig.Name == "EXP10")
                return new NumX(RuntimeApi.DecPow(RuntimeApi.DecFrom("10", "0"), DecArg(ic, 0), num.IntermediateMode, num.StandardIntermediate), 0, Dec: true);
            if (RenderDec(ic) is { } dec)
                return dec;
        }
        // NATIVE arithmetic with an SDIDI-carried ARGUMENT (kb/Work PB69): the function returns its value on the SDIDI
        // when it has an SDIDI body, rather than landing the argument into Int128 at a working scale that a 33-digit power
        // already overflows; RenderDec's native arms take every argument there as its EXACT value and round the value once
        // (CobolIntrinsics.ExactValue.cs, kb/Work PB4568: MOD (BIG * BIG * K3, 2 ** 10) is 999 with every dividend digit).
        // ⚠ The native Dec producers are THREE, not one — an integer power (past or within the Int128 window), a
        // floating-point numeric-EDITED sender, and a floating-point LITERAL operand, which owner decision D-B
        // made much the most common of them. This comment named only the first, and so did CombineCore's
        // (kb/Work PB273); the census is written out there.
        // ⛔ AN ARGUMENT EXPRESSION PAST THE Int128 CARRIER IS TAKEN EXACT BY THE EXACT FAMILY, BEFORE THE SDIDI ARM (kb/Work
        // PB3953). Such an argument (a nested product of 39 to 77 digits, NumX.Wide) would otherwise reach the SDIDI arm below
        // lowered to its 34-digit round-to-odd image, and ABS (BIG * BIG * K3) answered from the image (…100000 for …000999);
        // RenderWide evaluates the call on the same exact wide form the arithmetic statements keep (§2023:8.8.1.3, §2023:15.4.1, GnuCOBOL's
        // arbitrary-precision argument temporary by CLAUDE.md rule 1).
        // ⛔ THE FAMILY OF THE CALL IS THE PREDICTION'S (kb/Work PB5893 review R3-1, the coordinator's ruling): FunctionValueLane.IsBinary64 is the
        // one decider of whether the call is a binary64, in the context this render runs in, and NumericLanes.LaneOf reads the same answer.
        // DOC-A.1-92b lives there: a binary64 argument makes the function binary64 even beside an argument that is on the SDIDI only because a
        // floating-point receiver's argument keeps significance, which the SDIDI-argument arm below would otherwise take.
        else if (FunctionValueLane.IsBinary64(ic, num.CurrentLaneContext))
            return RenderFloat(ic, Binary64Body(sig));
        else if (RenderWide(ic) is { } wideExact)
            return wideExact;
        // ⛔ A CROSS-ALIGNING ARM WHOSE ALIGNED ARGUMENTS PROVABLY CANNOT STAY IN THE Int128 CARRIER TAKES THE SDIDI
        // (kb/Work PB621; the Int128 half of the boundary PB252 made honest). SUM / RANGE / MEDIAN / MIDRANGE (MEAN left them with PB3860, below)
        // align every argument to the LIST's maximum scale, and alignment multiplies: ±1.7e30 beside a
        // PIC SV9(8) item needs 39 digits aligned, so the native arm answered EC-SIZE-OVERFLOW for RANGE = 3.4e30 —
        // a value its PIC S9(31) receiver holds. The value is representable; only the carrier's width was the
        // problem, and the SDIDI carries its exponent at run time (SumDec / RangeDec …: each argument converts
        // exactly, §2023:8.8.1.5.2 GR1, and the equivalent arithmetic expression evaluates on 34 digits — more than any
        // receiver can store, §2023:13.18.40.3 SR14). ⚠ NOT "ALIGN TO THE RECEIVER'S SCALE", which the note proposed:
        // RANGE(0.6, −0.5) into a scale-0 receiver is 1.1 → 1, but each argument cut to scale 0 first is 0 − 0 = 0 —
        // no guard-digit count survives a carry (0.95 + 0.05), so the arguments must keep their own digits and the
        // wider carrier must be the one that changes. The question is asked of the arguments' digit BOUNDS
        // (NumX.Digits) so an ordinary list keeps its Int128 body unchanged and only a list that may leave the
        // carrier moves; an argument whose bound is unknown (an intrinsic's value) moves it when the alignment shifts any
        // argument (kb/Work PB4792), as CombineAdditive's exact additive does (PB4752).
        // ⛔ UNDER NATIVE ARITHMETIC THE LIST IS EVALUATED ON THE EXACT FAMILY'S WHOLE VALUE, BOUNDED OR NOT (kb/Work PB4826): the
        // SDIDI body's 34 digits (SumDec …) round a 35-to-38-digit value the Int128 body, the arithmetic expression and the receiver's
        // single rounding (§2023:14.7.4.3) would have kept whole, so SUM (A B) ROUNDED stored .01 where A + B stored .00. A standard mode
        // keeps the SDIDI body: there the 34-digit SDIDI value IS the returned value (§2023:15.4.1 1)).
        // ⛔ THIS ARM PRECEDES THE SDIDI-ARGUMENT ARM BELOW IT (kb/Work PB4860): a whole-value argument (NumX.Exact, a nested call of
        // this family) lowers to its 34-digit image and reads as an SDIDI operand, and SUM (SUM (A B) Z) took SumExact, whose 34 digits rounded
        // the 35-digit value this arm keeps whole. AlignedOperands reads that argument unlowered. A genuine SDIDI or binary64 operand still
        // answers null there, so those lists keep the SDIDI and float bodies. Under a standard mode RenderDec has already returned (the
        // family is always-SDIDI there), so this arm is the native one.
        else if (IntrinsicCarrierRules.CrossAligningFunctions.Contains(sig.Name) && AlignmentMayLeaveCarrier(ic)
            && RenderWholeAligned(ic) is { } wideAligned)
            return wideAligned;
        else if (AnyDecRaw(ic) && RenderDec(ic) is { } decNative)
            return decNative;
        // ⛔ MEAN'S VALUE IS THE SDIDI QUOTIENT UNDER NATIVE ARITHMETIC TOO (kb/Work PB3860). §2023:15.60.4 RV1's equivalent arithmetic
        // expression ends in a division; under a standard mode the value is that expression's SDIDI value (§2023:15.4.1 r1) and
        // under native arithmetic §2023:15.4.1 leaves "an implementor-defined approximation". The approximation this arm used to
        // choose came from the RECEIVER: the quotient was cut at max(receiver scale, sum scale + 1, 6) places and the
        // receiver's ROUNDED phrase then rounded the cut, so COMPUTE R2 ROUNDED MODE AWAY-FROM-ZERO = MEAN(1E-8 and nineteen
        // zeros) stored 0.00 where the one rounding of 5E-10 is 0.01, and DISPLAY showed the cut value. The determination is
        // the standard-mode value in every mode and channel (the PB3839 owner determination: one reference, one value), which
        // the receiver then rounds ONCE (§2023:14.7.4.3). A list with a binary64 operand keeps the float lane below: its one value
        // IS a double, and the landing is the float family's own (ReceiverContext.FloatLanding).
        if (IntrinsicCarrierRules.ReturnsSdidiQuotient(sig.Name) && !AnyRealArgument(ic)
            && RenderDec(ic) is { } sdidiQuotient)
            return sdidiQuotient;
        if (sig.Float) return RenderFloat(ic, Binary64Body(sig));
        // (The float lane's precondition is IntrinsicArgumentRules.ArgumentRunIsAllNumeric — an argument run binary64
        // can carry — and its trigger is a binary64 operand at a §2023:15.3 TYPE-10 position: AnyRealArgument reads the
        // operands through RawArg, where a type-6 operand is already its integer (kb/Work PB651). That is also why no
        // integer-only function needs a by-name exemption: FACTORIAL, whose result binary64 cannot carry (33! ≈ 8.7e36),
        // never reaches the lane, because its one position is type 6.)
        // MAX / MIN / ORD-MAX / ORD-MIN — and MEDIAN over an odd count — are pure SELECTION: §2023:15.59.4 RV1 /
        // §2023:15.63.4 RV1 "the returned value is the CONTENT of the argument-1 having the greatest [least] value",
        // §2023:15.71.4 RV1 / §2023:15.72.4 RV1 "the ordinal number that corresponds to the position of the argument-1 having
        // the greatest [least] value", and §2023:15.61.1 MEDIAN "returns the content of the argument whose value is the
        // middle value" (an even count is §2023:15.61.4 RV2's arithmetic mean, not a selection), each compared "according
        // to the rules for simple conditions"; no equivalent arithmetic expression, so §2023:15.4.1's native latitude over the
        // representation never reaches the VALUE. A MIXED list (a float beside a fixed-point item) must deliver
        // the selected argument from ITS OWN carrier, never re-rendered through binary64 (kb/Work PB65
        // RV-15.59.4-1 D2: MAX(F1 N1) with N1 = 999999999999999999 returned 13 — the 18-digit content went
        // double → FromDouble at scale 9 → modular store; MEDIAN(F1 N1 N2) returned 0). The SDIDI carrier holds
        // a 38-digit fixed exactly and the float through the §2023:8.8.1.5.1 conversion, and its compare is exact — so
        // a mixed selection list evaluates on the SDIDI under NATIVE too, and lands once at the receiver. An
        // all-float list stays in the float lane below (its content IS the double), and the arithmetic
        // statistical family (SUM / MEAN / RANGE / MIDRANGE — equivalent arithmetic expressions) keeps the D16
        // native float lane a float operand selects.
        if (sig.Name is "MAX" or "MIN" or "ORD-MAX" or "ORD-MIN" or "MEDIAN" && !ic.ComparesStrings
            && IntrinsicCarrierRules.SelectionNeedsExactLane(AnyRealArgument(ic), AllRealArguments(ic),
                AnyFloatItemArgument(ic), AnyApproximateArgument(ic), IntrinsicCarrierRules.ReturnsMeanOfMiddlePair(sig.Name, ic.Args))
            && RenderDec(ic) is { } decSelection)
            return decSelection;
        // ⛔ A SELECTION THE RECEIVER'S INT128 HEADROOM WOULD CUT RIDES THE SDIDI TOO (kb/Work PB3839, review R1-1). MAX/MIN
        // land the selected value at the receiver-bound working scale, which ReceiverContext.WorkingScale caps at
        // 38 - receiver integer digits; a receiver wide enough to push that cap below the arguments' common scale
        // truncated the selection BEFORE the receiver's ROUNDED phrase saw it (MAX(0.000000001 0) into PIC 9(30)V9
        // ROUNDED MODE AWAY-FROM-ZERO stored 0.0 where NUMVAL-F of the same text stores 0.1) - the double rounding and
        // the receiver-dependent value this note removes, left standing at the cap. The SDIDI holds the selected
        // argument exactly at its own digits whatever the receiver, and its store rounds once.
        if (sig.Name is "MAX" or "MIN" && SelectionPastReceiverHeadroom(ic)
            && RenderDec(ic) is { } decHeadroom)
            return decHeadroom;
        // ⛔ AND THE LANE IS A FACT ABOUT THE ARGUMENT RUN, NOT ABOUT ONE ARGUMENT (kb/Work PB635).
        // `AnyRealArgument` answers "is any argument floating"; the question this dispatch has to answer is "does
        // the WHOLE argument run render in binary64", because FloatBody converts EVERY argument through Dbl. For
        // a function whose §2023:15.3 argument types are all numeric (type 6 "an integer data item", type 10 "a
        // numeric data item") those are the same question. For one that MIXES them they are not, and the gap was
        // a crash on conforming-shaped source: FIND-STRING takes two string operands (§2023:15.37.3 AR1 — "class
        // alphabetic, alphanumeric, or national") and an integer argument-3 (r3), so
        // `FIND-STRING(HAY NDL START AFTER <COMP-2>)` moved the whole call to the float lane — the two strings
        // became `(double)CobolNum.FromAlphanumeric(…)`, LAST/ANYCASE were dropped, and the emitted call named
        // `FindStringReal`, which the runtime does not contain (Roslyn CS0117). It should not: §2023:15.37.4's
        // returned value is a character POSITION with no equivalent arithmetic expression, so §2023:15.4.1's
        // "implementor-defined approximation of the value of that expression" — the whole licence the binary64
        // lane rests on — is not granted for it at all, and there is no binary64 carrier anywhere in the call.
        // IntrinsicArgumentRules.ArgumentRunIsAllNumeric derives the precondition from the SPEC-VERIFIED
        // argument table and fails open, so it can only ever take a call OUT of the lane on a cited rule.
        if (AnyRealArgument(ic) && IntrinsicArgumentRules.ArgumentRunIsAllNumeric(sig.Name, ic.Args.Count))
            return RenderFloat(ic, Binary64Body(sig));

        switch (sig.Name)
        {
            // Integer functions — scale-0 results (§2023:15.2 type 5).
            case "FACTORIAL":                                                   // §2023:15.36 (Int128; 34! overflows → EC default 0)
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Factorial), IntArg(ic, 0)), 0);
            // ⛔ THE FIVE UNARY EXACT ARMS TAKE AN UNSIGNED-WIDE ARGUMENT ON ITS OWN LANE (kb/Work PB2698). `Arg` passes a
            // `NumX.U` operand (a 16-byte unsigned COMP-5 item above Int128.MaxValue) through unlanded, and these arms
            // handed its UInt128 expression to Int128 parameters — Roslyn CS1503 on conforming source for ABS, SIGN,
            // INTEGER, INTEGER-PART and FRACTION-PART alike. Each now has a U-named runtime twin (named, never
            // overloaded: an int literal converts to both Int128 and UInt128, CS0121 — see CobolNum.FormatDisplayU)
            // whose result is correct WITHOUT narrowing (ABS of an unsigned operand is the operand itself), so the
            // five agree on the one carrier question.
            case "SIGN":                                                        // §2023:15.81 — scale-independent sign
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(a.U ? nameof(CobolIntrinsics.SignOfU) : nameof(CobolIntrinsics.SignOf), a.Expr), 0);
            }
            case "INTEGER":                                                     // §2023:15.44 INTEGER — floor to scale 0
            case "INTEGER-PART":                                                // §2023:15.49 INTEGER-PART — truncate to scale 0
            {
                NumX a = Arg(ic, 0);
                return a.U
                    ? new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.IntegerPartU), $"{a.Expr}, {a.Scale}"), 0, U: true)
                    : new NumX(RuntimeApi.Intrinsic(sig.Name is "INTEGER" ? nameof(CobolIntrinsics.Floor) : nameof(CobolIntrinsics.Truncate), $"{a.Expr}, {a.Scale}"), 0);
            }
            case "ABS":                                                         // §2023:15.7 ABS — argument's own scale
            {
                NumX a = Arg(ic, 0);
                // |U| is U itself (an unsigned operand is never negative): no call, the operand passes on its own lane.
                return a.U ? a : new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.AbsScaled), a.Expr), a.Scale);
            }
            case "FRACTION-PART":                                               // §2023:15.42 — argument's own scale
            {
                NumX a = Arg(ic, 0);
                return new NumX(RuntimeApi.Intrinsic(a.U ? nameof(CobolIntrinsics.FractionPartU) : nameof(CobolIntrinsics.FractionPart), $"{a.Expr}, {a.Scale}"), a.Scale);
            }
            case "MOD":                                                         // §2023:15.64 — floored modulus (sign table)
            case "REM":                                                         // §2023:15.77 — truncated remainder
            {
                NumX a = Arg(ic, 0), b = Arg(ic, 1);
                int s = Math.Max(a.Scale, b.Scale);
                return new NumX(RuntimeApi.Intrinsic(sig.Name is "MOD" ? nameof(CobolIntrinsics.ModScaled) : nameof(CobolIntrinsics.RemScaled),
                    $"{NumericRenderer.Align(a, s)}, {NumericRenderer.Align(b, s)}"), s);
            }

            // MAX/MIN are pure SELECTION (§2023:15.59.4/§2023:15.63.4: the returned value is the CONTENT of an argument)
            // — no alignment at all (fix-queue PB65): the §2023:8.8.4.2 comparison is CobolNum.Compare's exact
            // non-widening compare over each argument AT ITS OWN SCALE, and only the ONE selected value
            // rescales (escape-checked) to the common scale. The aligned forms wrapped silently at 39 aligned
            // digits — MIN of two positive arguments returned a NEGATIVE value, the content of NO argument.
            case "MAX" or "MIN":
            {
                var (vals, scales, s) = RawArgPairs(ic);
                // ⛔ THE SELECTED VALUE REACHES A RECEIVER WITH EVERY DIGIT IT HAS (kb/Work PB3839). The result scale
                // is the common argument scale s, widened to the receiver's own scale and capped at the receiver's
                // Int128 headroom (ReceiverContext.WorkingScale), NEVER the receiver's scale alone: landing the
                // selected value at a coarser receiver scale truncated it there first and the receiver's ROUNDED
                // phrase then rounded the truncation, so MAX(0.12345 0.1) into PIC 9V99 ROUNDED MODE
                // AWAY-FROM-ZERO stored 0.12 where §2023:14.7.4.3 GR3 owes the one rounding of 0.12345, 0.13 - the same
                // reference giving a different value to a different receiver, which §2023:15.59.4 RV1 ("the content of the
                // argument-1 having the greatest value") does not allow. The receiver-bound result keeps the
                // store-cap (§2023:14.9.25.4 GR6 high-order truncation semantics, the receiver's own capacity finishing
                // the job). A selection whose common scale s EXCEEDS the receiver's headroom never reaches this arm:
                // SelectionPastReceiverHeadroom routes it to the SDIDI above, so the cap below never cuts a digit of
                // the selected value (review R1-1: the cap truncated it before the receiver's ROUNDED phrase). A
                // receiverless/float context keeps the common scale and stays LOUD past the escape boundary — a
                // capped value inside a comparison would silently compare the wrong number (PB13's
                // receiver-blind-scale lesson, applied in both directions). A FLOATING-POINT edited receiver is the
                // float context too: its Scale is only the mask's hint (ReceiverContext.RoundsAtItsScale).
                bool store = num.Receiver.RoundsAtItsScale;
                int to = store ? num.Receiver.WorkingScale(s) : s;
                return new NumX(RuntimeApi.Intrinsic(sig.Name is "MAX" ? nameof(CobolIntrinsics.MaxAt) : nameof(CobolIntrinsics.MinAt),
                    $"{to}, {(store ? "true" : "false")}, {vals}, {scales}"), to);
            }
            // SUM/RANGE genuinely ADD, so they keep aligned arguments — escape-CHECKED both at the per-argument
            // alignment (Align emits RescaleEscape) AND across the accumulation itself (CobolIntrinsics.SizeEscape,
            // kb/Work PB252): every step past the Int128 intermediate is the size-error condition, never a wrap.
            // ⛔ Both of these reach here ONLY under NATIVE arithmetic — CrossAligningFunctions routes them to
            // SumDec/RangeDec under a standard mode, where §2023:15.4.1 r1 admits no approximation at all.
            case "SUM":                                                         // §2023:15.88 — Σ at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SumScaled), argList), s);
            }
            case "RANGE":                                                       // §2023:15.76 — MAX − MIN at the common scale
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RangeScaled), argList), s);
            }
            case "MEDIAN" or "MIDRANGE":                                        // §2023:15.61/62 — the /2 is exact at scale s+1 (×10/2)
            {
                var (argList, s) = AlignedArgs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.Name is "MEDIAN" ? nameof(CobolIntrinsics.MedianScaled) : nameof(CobolIntrinsics.MidrangeScaled), argList), s + 1);
            }
            // (MEAN, §2023:15.60, has NO arm in this switch: its value is the SDIDI quotient of its equivalent arithmetic expression in
            // every arithmetic mode and for every receiver, IntrinsicCarrierRules.SdidiQuotientFunctions, so RenderNum hands it to
            // RenderDec's MeanDec before this switch - kb/Work PB3860. An arm here is where a receiver-scale read lives.)
            case "ORD-MAX" or "ORD-MIN" when ic.ComparesStrings:                // all-string form (PCS via CollatePrefix, CA23)
                return new NumX(RuntimeApi.Intrinsic(sig.Name is "ORD-MAX" ? nameof(CobolIntrinsics.OrdMaxString) : nameof(CobolIntrinsics.OrdMinString),
                    CollatePrefix(ic) + StrArgList(ic)), 0);
            case "ORD-MAX" or "ORD-MIN":                                        // §2023:15.71/72 — 1-based ordinal, tie = first
            {
                // Pure selection with NO result rescale — every legal argument list has its defined ordinal
                // (fix-queue PB65; the aligned form wrapped at 39 aligned digits and picked the wrong argument).
                var (vals, scales, _) = RawArgPairs(ic);
                return new NumX(RuntimeApi.Intrinsic(sig.Name is "ORD-MAX" ? nameof(CobolIntrinsics.OrdMaxAt) : nameof(CobolIntrinsics.OrdMinAt),
                    $"{vals}, {scales}"), 0);
            }

            // (NUMVAL / NUMVAL-C have NO arm here in ANY arithmetic mode — §2023:15.67.4 RV1 / §2023:15.68.4 RV1 fix their
            // value, so RenderNum's §2023:15.4.1 arm routes both to the SDIDI projection before the mode dispatch
            // and no compile-time working scale exists for them to be wrong about; kb/Work PB251.)

            // The §2023:15.93/§2023:15.94 TEST validators — 0 / first-error position / LENGTH+1, scale 0. The digit-cap
            // sub-notes are ARITHMETIC-MODE dependent (§2023:15.93.4 RV1b notes 2/4): 31 native, 34 under the SDIDI
            // standard modes (standard-binary's 35 is in ArithmeticModes.NumvalDigitCap and unreachable — the
            // mode is declined at bind on §2023:4.2.6 processor-dependence, kb/Work PB198).
            case "TEST-NUMVAL":
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.TestNumval),
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);
            case "TEST-NUMVAL-C":
                // The LOCALE arm mirrors NumvalC's (§2023:15.94.3 AR1 imports §2023:15.68.3 whole; PB64 T6).
                if (ic.LocaleWritten)
                    return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.TestNumvalCLocale),
                        $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}{AnycaseFlag(ic)}{DigitCapFlag}"), 0);
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.TestNumvalC),
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}{AnycaseClassification(ic)}"), 0);

            // The §2023:15.90/§2023:15.91 date validators — integer verdict chains (year → month → day), scale 0.
            // ⛔ THE WIDE INTAKE, because the two functions are TOTAL (kb/Work PB254 — see IntArgWide):
            // §2023:15.90.3 AR1 / §2023:15.91.3 AR1 constrain nothing but integer-ness and r1a is a CATCH-ALL — which is also
            // why a binary64 past the Int128 carrier may be clamped here: the verdict past the window never varies.
            case "TEST-DATE-YYYYMMDD" or "TEST-DAY-YYYYDDD":
                return new NumX(RuntimeApi.DateFn(sig.Name is "TEST-DATE-YYYYMMDD" ? nameof(CobolDate.TestDateYyyymmdd) : nameof(CobolDate.TestDayYyyyddd),
                    IntArgWideSaturating(ic, 0)), 0);

            case "FIND-STRING":                                                 // §2023:15.37 FIND-STRING (2023) — 1-based position of argument-2 in argument-1
                // Argument-3 also takes the WIDE intake: §2023:15.37.3 AR3 places no value constraint and
                // §2023:15.37.4 RV2/r3 answer for every integer — ignore that many matches, else 0 (PB254). Past every
                // match the answer is 0 however large the count, so a binary64 past the carrier is clamped.
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.FindString),
                    $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {(ic.FindLast ? "true" : "false")}, "
                    + $"{(ic.Args.Count > 2 ? IntArgWideSaturating(ic, 2) : "0")}, {(ic.Anycase ? "true" : "false")}{AnycaseClassification(ic)}"), 0);
            case "ORD":                                                         // §2023:15.70 — PCS-relative ordinal (H5: weights only when flagged)
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Ord), $"{Str(ic.Args[0])}{Collate(ic)}"), 0);
            case "LENGTH" or "BYTE-LENGTH" when ic.OverCurrentImage && ic.Args[0] is BoundFieldOperand { Place: { } vg }:
                // §2023:15.50.4 RV7 / §2023:15.14.4 RV6 over a variable-length group's CURRENT-EXTENT IMAGE (kb/Work PB244):
                // the same string DISPLAY shows (A.1 item 57), whose storage characters are bytes.
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.ByteLength),
                    PlaceRenderer.VarGroupCurrentImage(vg, $"FUNCTION {sig.Name} of", transfer: true)), 0);
            case "LENGTH":                                                      // §2023:15.50 runtime shapes (kb/Work PB61)
                // (A group with a runtime length — an OCCURS DEPENDING table, dynamic-length items or
                // dynamic-capacity tables beneath it — never arrives here: the binder's VariableLengthGroupSum
                // builds the §2023:15.50.4 RV4b/r7 expression, whose ODO part is a BoundOdoExtent.)
                // r6 — a national DYNAMIC LENGTH item's CURRENT length in bytes (the storage image); every other
                // runtime shape (a ref-mod view, an ANY LENGTH / dynamic-length alphanumeric item, a nested
                // string-function result) is the character-position count over the string image.
                if (ic.LengthInBytes && ic.Args[0] is BoundFieldOperand { Place: { } lp })
                    return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.ByteLength), OperandText.AsStorageImage(lp)), 0);
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Length), Str(ic.Args[0])), 0);
            case "BYTE-LENGTH":                                                 // §2023:15.14 runtime shapes (kb/Work PB61)
                if (ic.Args[0] is BoundFieldOperand { Place: { } bp })
                    return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.ByteLength), OperandText.AsStorageImage(bp)), 0);
                // A nested function result is not storage the renderer can slice; its string image is the value's
                // characters, and the BYTES those occupy follow its §2023:15.2 category (kb/Work PB1398): a national
                // result is 2 bytes per position (D-N1), a boolean result (implicit usage bit, §2023:15.2 GR2) packs 8
                // positions per byte and rounds up (§2023:15.14.4 RV4), an alphanumeric one is 1 byte per position.
                return new NumX(RuntimeApi.Intrinsic(
                    IntrinsicResultType.OperandCategory(ic.Args[0]) switch
                    {
                        PicCategory.National => nameof(CobolIntrinsics.ByteLengthNational),
                        PicCategory.Boolean => nameof(CobolIntrinsics.ByteLengthBoolean),
                        _ => nameof(CobolIntrinsics.ByteLength),
                    }, Str(ic.Args[0])), 0);

            // Date/time conversions (§2023:15.22/24/46/47; integer date form §2023:15.5.2). These four keep the NARROW
            // §2023:15.3 landing: §2023:15.5.2 BOUNDS the integer date form (1601-01-01 onward), so an argument the body
            // cannot represent really is an incorrect value and EC-ARGUMENT-FUNCTION is the standard's answer —
            // the exact opposite of the TEST- validators above (kb/Work PB22 / PB254).
            // The 1989 module's form has no upper bound on the year (kb/Work PB5130, BoundIntrinsicCall.IntegerDateFormUnbounded).
            case "DATE-OF-INTEGER" or "DAY-OF-INTEGER" or "INTEGER-OF-DATE" or "INTEGER-OF-DAY":
                return new NumX(RuntimeApi.DateFn(sig.Name switch
                    {
                        "DATE-OF-INTEGER" => nameof(CobolDate.DateOfInteger),
                        "DAY-OF-INTEGER" => nameof(CobolDate.DayOfInteger),
                        "INTEGER-OF-DATE" => nameof(CobolDate.IntegerOfDate),
                        _ => nameof(CobolDate.IntegerOfDay),
                    },
                    $"{IntArg(ic, 0)}{(ic.IntegerDateFormUnbounded ? ", unbounded: true" : "")}"), 0);

            // The Y2K windowing trio (§2023:15.23/§2023:15.25/§2023:15.100) — integer results; the optional trailing
            // argument-2 (a SIGNED offset) / argument-3 ride the runtime's C#-optional-parameter defaults
            // (50 / the execution-time year via the argument-3 = 0 sentinel), so only the present arguments
            // render.
            case "DATE-TO-YYYYMMDD" or "DAY-TO-YYYYDDD" or "YEAR-TO-YYYY":
                return new NumX(RuntimeApi.DateFn(sig.Name switch
                    {
                        "DATE-TO-YYYYMMDD" => nameof(CobolDate.DateToYyyymmdd),
                        "DAY-TO-YYYYDDD" => nameof(CobolDate.DayToYyyyddd),
                        _ => nameof(CobolDate.YearToYyyy),
                    },
                    string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => IntArg(ic, i)))), 0);

            // SECONDS-PAST-MIDNIGHT (§2023:15.80) — type NUMERIC in standard numeric time form: the runtime
            // returns the local time-of-day TICK count = the unscaled value at SCALE 7 (the documented
            // 100 ns WiseOwl COBOL precision, §2023:15.80.3 RV3).
            case "SECONDS-PAST-MIDNIGHT":
                return new NumX(RuntimeApi.DateFn(nameof(CobolDate.SecondsPastMidnight), ""), 7);

            // ── The COBOL-2014 date/time + number family (§2023:15.17/48/79/92; §2023:15.69/95) ───────────────────────────
            case "COMBINED-DATETIME":                                           // §2023:15.17 — a1 + a2/100000
            {
                // ⛔ ARGUMENT-2 IS TAKEN EXACTLY, NEVER LANDED (kb/Work PB620). §2023:15.17.3 AR2 requires only
                // "standard numeric time form", which §2023:15.5.5 defines by MAGNITUDE — no fraction-digit limit —
                // so a receiver-derived working scale has nothing to round to and §2023:15.4.1 r1 makes the returned
                // value EQUAL the §2023:15.17.4 RV1 expression. A Dec/float argument-2 never reaches here at all: it
                // is claimed by RenderDec's own arm under either arithmetic mode, which is what lets this arm
                // consume the operand at its OWN scale.
                NumX t = RawArg(ic, 1);
                // A trailing-P argument (negative scale) is whole seconds first (the runtime's WholeSecondsAtScale), so
                // the exact result is read at max(scale, 0) + 5 (kb/Work PB2633).
                return new NumX(RuntimeApi.DateFn(nameof(CobolDate.CombinedDatetime), $"{IntArg(ic, 0)}, {t.Expr}, {t.Scale}{LeapSecondFlag}"), Math.Max(0, t.Scale) + 5);
            }
            case "INTEGER-OF-FORMATTED-DATE":                                   // §2023:15.48 — analyze a2 per format a1 → integer date
                return new NumX(RuntimeApi.DateFn(nameof(CobolDate.IntegerOfFormattedDate), $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            case "SECONDS-FROM-FORMATTED-TIME":                                 // §2023:15.79 — result scale = the format's fractional-second count
            {
                if (ic.Args[0] is not BoundStringLiteral fmt)
                    return new NumX(EmitText.LoudValue("long",
                        "FUNCTION SECONDS-FROM-FORMATTED-TIME requires a literal time format (§2023:15.79.3 r1)"), 0);
                int fsc = RuntimeApi.DateFormatFractionDigits(fmt.Value);       // compile-time — the ONE format analyzer
                return new NumX(RuntimeApi.DateFn(nameof(CobolDate.SecondsFromFormattedTime), $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {fsc}{LeapSecondFlag}"), fsc);
            }
            case "TEST-FORMATTED-DATETIME":                                     // §2023:15.92 — 0 (valid) or the 1-based error position
                return new NumX(RuntimeApi.DateFn(nameof(CobolDate.TestFormattedDatetime), $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{LeapSecondFlag}"), 0);
            // (NUMVAL-F has NO arm here in ANY arithmetic mode or channel: its value is the exact parse, so RenderNum's first
            // arm routes it to the SDIDI projection before the mode dispatch, exactly as NUMVAL and NUMVAL-C; kb/Work PB3839.)
            case "TEST-NUMVAL-F":                                               // §2023:15.95 — 0 / first-error position / LENGTH+1;
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.TestNumvalF),    //   the r1b digit-cap sub-note is MODE-dependent,
                    $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}"), 0);         //   exactly as its two TEST- siblings (PB60)

            case "INTEGER-OF-BOOLEAN":                                          // §2023:15.45.4 RV1 — the unsigned MSB-first value of the bit configuration
                return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.IntegerOfBoolean), Str(ic.Args[0])), 0);

            default:
                return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (no numeric render recipe)"), 0);
        }
    }

    /// <summary>⛔ THE BINARY64 BODY OF EACH FUNCTION THE FLOAT LANE RENDERS, keyed on the function (its catalog name) and named
    /// through <c>nameof</c> (kb/Work PB2627): the body a call emits is the compiler's fact, so a renamed or deleted body fails the build
    /// and the reachability census sees exactly the bodies a program can reach.</summary>
    /// <remarks>
    /// <para>Two families. The §2023:15.4.1 floating-math rows (<see cref="IntrinsicSig.Float"/>) compute in binary64 always. An exact-family
    /// function computes in binary64 when a binary64 argument reaches a numeric position (PB2), through a <c>…Real</c> twin: a
    /// same-named <c>double</c> overload does not compile, because an integer literal converts implicitly to both <c>Int128</c> and
    /// <c>double</c>, so <c>FUNCTION MAX(5 7)</c> became a CS0121 ambiguity and broke six corpus programs that never touched a float.</para>
    /// <para><c>IntrinsicRealArgDriftTests</c> holds the table to every function whose argument run can carry a binary64
    /// (<c>IntrinsicArgumentRules.ArgumentRunIsAllNumeric</c>, the precondition the lane dispatches on) and to no other.</para>
    /// </remarks>
    internal static readonly FrozenDictionary<string, string> Binary64Bodies = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // The §2023:15.4.1 floating-math family.
        ["ACOS"] = nameof(CobolIntrinsics.Acos),
        ["ASIN"] = nameof(CobolIntrinsics.Asin),
        ["ATAN"] = nameof(CobolIntrinsics.Atan),
        ["COS"] = nameof(CobolIntrinsics.Cos),
        ["SIN"] = nameof(CobolIntrinsics.Sin),
        ["TAN"] = nameof(CobolIntrinsics.Tan),
        ["SQRT"] = nameof(CobolIntrinsics.Sqrt),
        ["LOG"] = nameof(CobolIntrinsics.Log),
        ["LOG10"] = nameof(CobolIntrinsics.Log10),
        ["ANNUITY"] = nameof(CobolIntrinsics.Annuity),
        ["PRESENT-VALUE"] = nameof(CobolIntrinsics.PresentValue),
        ["RANDOM"] = nameof(CobolIntrinsics.Random),
        ["STANDARD-DEVIATION"] = nameof(CobolIntrinsics.StandardDeviation),
        ["VARIANCE"] = nameof(CobolIntrinsics.Variance),
        ["E"] = nameof(CobolIntrinsics.E),
        ["PI"] = nameof(CobolIntrinsics.Pi),
        ["EXP"] = nameof(CobolIntrinsics.Exp),
        ["EXP10"] = nameof(CobolIntrinsics.Exp10),
        // The exact family's binary64 twins (PB2).
        ["ABS"] = nameof(CobolIntrinsics.AbsReal),
        ["COMBINED-DATETIME"] = nameof(CobolIntrinsics.CombinedDatetimeReal),
        ["FRACTION-PART"] = nameof(CobolIntrinsics.FractionPartReal),
        ["INTEGER"] = nameof(CobolIntrinsics.FloorReal),
        ["INTEGER-PART"] = nameof(CobolIntrinsics.TruncateReal),
        ["MAX"] = nameof(CobolIntrinsics.MaxReal),
        ["MEAN"] = nameof(CobolIntrinsics.MeanReal),
        ["MEDIAN"] = nameof(CobolIntrinsics.MedianReal),
        ["MIDRANGE"] = nameof(CobolIntrinsics.MidrangeReal),
        ["MIN"] = nameof(CobolIntrinsics.MinReal),
        ["ORD-MAX"] = nameof(CobolIntrinsics.OrdMaxReal),
        ["ORD-MIN"] = nameof(CobolIntrinsics.OrdMinReal),
        ["RANGE"] = nameof(CobolIntrinsics.RangeReal),
        ["REM"] = nameof(CobolIntrinsics.RemReal),
        ["SIGN"] = nameof(CobolIntrinsics.SignOfReal),
        ["SUM"] = nameof(CobolIntrinsics.SumReal),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The binary64 body of <paramref name="sig"/>'s function (<see cref="Binary64Bodies"/>).</summary>
    private static string Binary64Body(in IntrinsicSig sig) =>
        Binary64Bodies.TryGetValue(sig.Name, out string? body)
            ? body
            : throw new InvalidOperationException($"FUNCTION {sig.Name} reached the binary64 lane, which has no body for it (IntrinsicRenderer.Binary64Bodies)");

    /// <summary>The binary64 CALL this family computes in — the argument run only, with no container or receiver
    /// decision in it (kb/Work PB253: those belong to <see cref="RenderFloat"/> and
    /// <see cref="RenderFloatNative"/> respectively, and mixing them is what let an arm order go wrong).</summary>
    private string FloatBody(BoundIntrinsicCall ic, string method) => method switch
    {
        // RANDOM (§2023:15.75.3): the no-argument form continues the current sequence; the seeded form restarts it.
        // ⛔ THE SEED TAKES THE WIDE INTAKE, because the argument is TOTAL (kb/Work PB636 — see IntArgWide):
        // §2023:15.75.3 AR2 constrains the SIGN alone and §2023:15.75.4 RV3 makes the distinct-sequence subset a FLOOR
        // ("from 0 through at least 32767"), not a domain, so §2023:15.3 has no incorrect value to raise on.
        nameof(CobolIntrinsics.Random) when ic.Args.Count == 0 => RuntimeApi.Intrinsic(method, ""),
        nameof(CobolIntrinsics.Random) => RuntimeApi.Intrinsic(method, IntArgWide(ic, 0)),
        // COMBINED-DATETIME's binary64 carrier takes the compilation group's LEAP-SECOND state, exactly as its
        // two exact-carrier twins do (kb/Work PB620): §2023:15.17.3 AR2's bound is §2023:7.3.17.4 GR4/GR5's, so a screen
        // that could not see the directive would admit 86 400.5 on one carrier and refuse it on another.
        nameof(CobolIntrinsics.CombinedDatetimeReal) => RuntimeApi.Intrinsic(method, $"{Dbl(ic, 0)}, {Dbl(ic, 1)}{LeapSecondFlag}"),
        // A variadic body whose EAE CANCELS across its list takes an all-exact list as its EXACT values (kb/Work PB1565, PB4111 —
        // see ExactListBodies): the expression formed exactly and divided once in the native intermediate rounding mode, narrowed
        // once. PRESENT-VALUE's rate leads unscreened, exactly as on RenderDec's arm: the exact body decides §2023:15.74.3 AR2 on the
        // exact rate itself (PresentValueExact), so a leading table(ALL) stays one CobolExact[] enumeration.
        nameof(CobolIntrinsics.PresentValue) when !AnyRealArgument(ic) =>
            LeadThenTail(ic, method, $"{num.IntermediateMode}, ", RuntimeApi.ExactType, a => ExactValueOf(ic, a)),
        _ when ExactListBodies.Contains(method) && !AnyRealArgument(ic) =>
            RuntimeApi.Intrinsic(method, $"{num.IntermediateMode}, {ExactValueArgList(ic)}"),
        // PRESENT-VALUE (§2023:15.74.2 `argument-1 { argument-2 } …`): the rate leads, the amounts are the params tail.
        // The lead is the DISCOUNT BASE 1 + rate, formed on the rate's exact carrier after the ONE screen
        // (kb/Work PB1000 — see PresentValueBase).
        nameof(CobolIntrinsics.PresentValue) => LeadThenTail(ic, method, "", "double", DblOf, lead: a => PresentValueBase(ic, a)),
        // An ill-conditioned body handed an argument expression past the Int128 carrier takes the EXACT WIDE value, not its
        // 34-digit image (kb/Work PB4075 — see WideLeadArg).
        _ when WideLeadArg(ic, method) is { } wide => RuntimeApi.Intrinsic(method + "Wide", wide),
        // A table(ALL) argument enumerates at run time (ISO §2023:15.3; kb/Work PB62) — the list becomes ONE array.
        _ => RuntimeApi.Intrinsic(method, ArgArray(ic, 0, "double", DblOf)
            ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => i == 0 ? LeadArg(ic, method) : Dbl(ic, i)))),
    };

    /// <summary>
    /// The binary64-family bodies that take argument-1 on its EXACT carrier instead of its binary64 — a
    /// <c>CobolDec</c> overload in <c>CobolIntrinsics.Float.cs</c> — and which carriers each takes that way
    /// (kb/Work PB999).
    /// </summary>
    /// <remarks>
    /// <para>The §2023:15.4.1 licence makes the RETURNED value an implementor-defined approximation; it does not make
    /// the ARGUMENT one. Each member is a body whose result for the narrowed substitute is not an approximation of
    /// its result for the real argument:</para>
    /// <list type="bullet">
    ///   <item><see cref="ExactIntake.Sdidi"/> — the substitute is wrong only past binary64's RANGE, which only an
    ///         SDIDI reaches (10^±6144, §2023:8.8.1.5.2: a floating-point literal, a product past the Int128 window);
    ///         a scaled operand is inside it, where the body's bounded condition number keeps binary64's RELATIVE
    ///         error the ordinary approximation. <c>Sqrt</c> (the native arm; the standard modes are
    ///         <c>SqrtDec</c>'s) — §2023:15.84.4 RV4: the root of 10^−400 is the representable 10^−200.</item>
    ///   <item><see cref="ExactIntake.EveryExact"/> — the body is ILL-CONDITIONED somewhere in its domain (its
    ///         condition number |x·f′(x)/f(x)| is unbounded, or reaches hundreds), so even the RELATIVE error of a
    ///         scaled operand's binary64 is amplified into the result, and every exact operand, scaled or SDIDI, is
    ///         taken exactly (kb/Work PB1041). <c>Sin</c> / <c>Cos</c> / <c>Tan</c> (§2023:15.82.4 RV1 / §2023:15.20.4 RV1 /
    ///         §2023:15.89.4 RV1) at every multiple of π/2 and at magnitude — reduced to a quadrant and residue on the
    ///         exact carrier: SIN(3.14159265358979323) answered 1.22·10^−16 where the value is 8.46·10^−18,
    ///         sin(1.0E40) answered +0.6468 where sin(10^40) is −0.5696, and sin(10^400) computed sin(+∞) = NaN;
    ///         <c>Log</c> / <c>Log10</c> (§2023:15.55.4 RV1 / §2023:15.56.4 RV1) at 1 (LOG(1 + 10^−26) answered 0) and past the
    ///         range (log10(10^−400) is −400, not log10(+0.0) = −∞); <c>Acos</c> / <c>Asin</c> (§2023:15.8.4 RV1 /
    ///         §2023:15.10.4 RV1) at ±1 (ACOS(1 − 10^−26) answered 0, not 1.414·10^−13); <c>Exp</c> / <c>Exp10</c>
    ///         (§2023:15.34.1 / §2023:15.35.1 — condition number |x|: 150 ulps at EXP(700.12…)).</item>
    /// </list>
    /// <para>Not members, each because its condition number is bounded by a constant on the whole domain, so the
    /// substitute's result IS the approximation: ATAN (≤ 1), and SQRT for a scaled operand (½). A float operand IS
    /// its binary64 and keeps the double body. <c>WholeRangeBodiesDriftTests</c> pins the runtime overload each
    /// member needs and the values each arm owes.</para>
    /// <para>⛔ A domain member still passes the ONE argument-domain screen (§2023:15.3 AR14; kb/Work PB952): its
    /// operand is <c>CobolIntrinsics.DomainDecAdmitted</c>, <c>DomainDec</c>'s predicate returning the admitted
    /// carrier instead of its binary64, so the rule is decided in one place on either lane.</para>
    /// </remarks>
    internal static readonly FrozenDictionary<string, ExactIntake> WholeRangeBodies = new Dictionary<string, ExactIntake>
    {
        [nameof(CobolIntrinsics.Sqrt)] = ExactIntake.Sdidi,
        [nameof(CobolIntrinsics.Sin)] = ExactIntake.Wide, [nameof(CobolIntrinsics.Cos)] = ExactIntake.Wide, [nameof(CobolIntrinsics.Tan)] = ExactIntake.Wide,
        [nameof(CobolIntrinsics.Log)] = ExactIntake.Wide, [nameof(CobolIntrinsics.Log10)] = ExactIntake.Wide,
        [nameof(CobolIntrinsics.Acos)] = ExactIntake.Wide, [nameof(CobolIntrinsics.Asin)] = ExactIntake.Wide,
        [nameof(CobolIntrinsics.Exp)] = ExactIntake.EveryExact, [nameof(CobolIntrinsics.Exp10)] = ExactIntake.EveryExact,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The VARIADIC binary64-family bodies that take an all-exact argument LIST as its exact values — a runtime overload
    /// <c>(CobolRounding mode, …CobolExact…)</c> in <c>CobolIntrinsics.Float.cs</c> that forms the function's equivalent
    /// arithmetic expression exactly, divides once and narrows only the result (kb/Work PB1565, PB4111): the list arm of
    /// <see cref="WholeRangeBodies"/>.
    /// </summary>
    /// <remarks>
    /// <para>Each member's EAE SUBTRACTS its arguments from one another, so narrowing each exact argument first is
    /// not an approximation of the returned value — its low digits cancel away: <c>Variance</c> (§2023:15.98.4 RV1, the
    /// deviations from FUNCTION MEAN: VARIANCE(100000000000000001 100000000000000002) answered 0, not 0.25),
    /// <c>StandardDeviation</c> (§2023:15.86.4 RV1, SQRT of that: 0, not 0.5), <c>PresentValue</c> (§2023:15.74.4 RV1, a Σ of
    /// signed amounts: PRESENT-VALUE(0 100000000000000001 −100000000000000000) answered 0, not 1). Nor is a 34-digit
    /// evaluation of it (kb/Work PB4111): a mean off by δ adds exactly δ² to the variance, so the SDIDI body's mean made
    /// VARIANCE of the 31-digit items 10^30, 10^30 + 1, 10^30 + 1 0.2222223333 for 2/9, and 34-digit term quotients made
    /// PRESENT-VALUE (0, P, 1 − P) of a 36-digit P 0 for 1. Under native arithmetic the value is the expression's exact
    /// value rounded once (CONFORMANCE.md DOC-A.1-123, GnuCOBOL's by CLAUDE.md rule 1), on every lane: here for a fixed-point
    /// list, through <see cref="RenderDec"/>'s native arms for an SDIDI or wide argument.</para>
    /// <para>A list with a FLOATING argument keeps the binary64 body — its EAE is evaluated in binary64, the
    /// determination CONFORMANCE.md DOC-A.1-92b states for the exact family's floating arm. An SDIDI argument is
    /// already claimed by <c>RenderNum</c>'s native Dec arm before the float family is reached.
    /// <c>WholeRangeBodiesDriftTests</c> pins the overload each member needs and that EVERY variadic float-family
    /// catalog row is a member, so a new variadic float function cannot silently take the narrowing arm.</para>
    /// </remarks>
    internal static readonly FrozenSet<string> ExactListBodies = new[]
    {
        nameof(CobolIntrinsics.Variance), nameof(CobolIntrinsics.StandardDeviation), nameof(CobolIntrinsics.PresentValue),
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Which exact carriers a <see cref="WholeRangeBodies"/> member takes unnarrowed.</summary>
    internal enum ExactIntake
    {
        /// <summary>An SDIDI operand only — the narrowing is wrong only past binary64's range.</summary>
        Sdidi,
        /// <summary>A scaled operand too (lifted exactly by <c>CobolDec.From</c>) — the body is ill-conditioned
        /// somewhere, so the narrowing's relative error is amplified into the result.</summary>
        EveryExact,
        /// <summary><see cref="EveryExact"/>, and the exact WIDE intermediate too (<c>NumX.Wide</c>, an argument expression of 39
        /// to 77 digits; kb/Work PB4075): the body is ill-conditioned at a place the SDIDI's 34-digit round-to-odd image cannot
        /// name — every multiple of π/2 for the periodic trio (a 39-digit angle's image is hundreds of radians away), 1 and ±1 for
        /// LOG, LOG10, ACOS and ASIN (a value 10^−25 from 1 has an image that is the whole answer) — so a <c>…Wide</c> overload
        /// in <c>CobolIntrinsics.FloatWide.cs</c> takes the exact value. <c>Exp</c> / <c>Exp10</c> stay <see cref="EveryExact"/>:
        /// their condition number |x| is at most 709 on the range binary64 holds, so the image's 10^−34 relative error never
        /// reaches the result's last bit; <c>Sqrt</c> is ½.</summary>
        Wide,
    }

    /// <summary>Argument-1 of the default binary64-family arm: an operand a <see cref="WholeRangeBodies"/> member
    /// takes exactly stays (or is lifted exactly into) a <c>CobolDec</c>, screened by
    /// <see cref="RuntimeApi.DomainArgAdmitted"/> when the row has a domain; every other operand is
    /// <see cref="DomainArg"/>'s screened binary64.</summary>
    /// <remarks>INTAKE(EXACT) — a whole-range body's operand on an exact carrier, unnarrowed (kb/Work PB999); any
    /// other operand falls through to <see cref="DomainArg"/>, whose contract is APPROXIMATED.</remarks>
    private string LeadArg(BoundIntrinsicCall ic, string method)
    {
        NumX x = RawArg(ic, 0);
        bool exact = WholeRangeBodies.TryGetValue(method, out var intake)
            && (x.Dec || intake is ExactIntake.EveryExact or ExactIntake.Wide && !x.Real);
        return exact ? ExactArg(ic, x) : DomainArg(ic, x);
    }

    /// <summary>Argument-1 on an EXACT carrier as a <c>CobolDec</c> expression — an SDIDI as it is, a scaled or
    /// unsigned-wide operand lifted exactly (<c>CobolDec.From</c> holds any Int128 at any scale) — and, when the
    /// row has a §15.x.3 value domain, through the ONE screen's admitted form
    /// (<see cref="RuntimeApi.DomainArgAdmitted"/>: a <c>CobolDec?</c>, null = rejected). A float operand has no
    /// exact carrier beyond its binary64 and never reaches here.</summary>
    /// <remarks>INTAKE(EXACT) — the carrier unnarrowed; the body narrows only what it computes from it.</remarks>
    private string ExactArg(BoundIntrinsicCall ic, NumX x)
    {
        NumX dec = x.Dec ? x : new NumX(num.DecOperand(x), 0, Dec: true);
        return ic.Sig.Domain is IntrinsicDomain.None
            ? dec.Expr
            : RuntimeApi.DomainArgAdmitted(dec, ic.Sig.Domain, ic.Sig.Name, DomainRuleCited(ic));
    }

    /// <summary>⛔ Argument-1 of a <see cref="ExactIntake.Wide"/> member as the EXACT WIDE value (kb/Work PB4075): the argument
    /// expression's <see cref="CobolWide"/> and its scale — through the row's ONE domain screen
    /// (<see cref="RuntimeApi.DomainWideAdmitted"/>, a <c>CobolWide?</c>, null = rejected) when it has a §15.x.3 value domain —
    /// for the <c>…Wide</c> body the call names; <c>null</c> when the member takes no wide form or the argument is not wide
    /// (a call over data items and literals is answered without rendering its argument a further time).</summary>
    /// <remarks>INTAKE(EXACT) — the operand's exact wide form, unnarrowed; the body narrows only what it computes from it.</remarks>
    private string? WideLeadArg(BoundIntrinsicCall ic, string method)
    {
        if (!WholeRangeBodies.TryGetValue(method, out var intake) || intake is not ExactIntake.Wide
            || ic.Args.Count != 1 || ic.Args[0] is not BoundComputedOperand) return null;
        NumX x = RawWideArg(ic, 0);
        if (!x.Wide) return null;
        string value = ic.Sig.Domain is IntrinsicDomain.None
            ? x.Expr
            : RuntimeApi.DomainWideAdmitted(x.Expr, x.Scale, ic.Sig.Domain, ic.Sig.Name, DomainRuleCited(ic));
        return $"{value}, {x.Scale}";
    }

    /// <summary>PRESENT-VALUE's leading value: the discount base <c>1 + argument-1</c>
    /// (<c>CobolIntrinsics.PresentValueBase</c>), from the rate's exact carrier after the ONE §2023:15.74.3 AR2 screen
    /// (<see cref="ExactArg"/>), or from a float rate's own binary64 after <see cref="DomainArg"/>.</summary>
    /// <remarks>⛔ The base, not the rate (kb/Work PB1000): <c>1 + rate</c> cancels at the domain's bound, so a body
    /// forming it from the NARROWED rate turned the legal −0.99999999999999999999 into a base of 0 and answered ∞.
    /// INTAKE(EXACT) for an exact carrier; INTAKE(APPROXIMATED) for a float one, whose binary64 IS the argument.</remarks>
    private string PresentValueBase(BoundIntrinsicCall ic, BoundOperand a)
    {
        NumX x = num.AsNum(a, NumericArgument);
        return RuntimeApi.Intrinsic(nameof(CobolIntrinsics.PresentValueBase), x.Real ? DomainArg(ic, x) : ExactArg(ic, x));
    }

    /// <summary>Argument-1 of a binary64-family call, SCREENED against the row's §15.x.3 value domain on its exact
    /// carrier before it becomes the body's double (kb/Work PB952 — <see cref="RuntimeApi.DomainArg"/>); a row with
    /// no domain takes the plain <see cref="DblOf"/>. ⛔ The screen is the ONLY place such a rule is decided: the
    /// runtime bodies carry no second test on the double (a legal argument rounded onto a bound would trip it).</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier, after an EXACT domain test on that carrier.</remarks>
    private string DomainArg(BoundIntrinsicCall ic, NumX x) =>
        ic.Sig.Domain is IntrinsicDomain.None
            ? NumericRenderer.Real(x)
            : RuntimeApi.DomainArg(x, ic.Sig.Domain, ic.Sig.Name, DomainRuleCited(ic));

    /// <summary>The row's domain rule as the run-time screen's message cites it: the compilation's edition's (the 1989 amendment's
    /// §1989:A.2.x.3 rule at COBOL-85, the COBOL-2023 rule after it), designated by <see cref="EditionInfo.CiteRule"/> (kb/Work
    /// PB6023), so a COBOL-85 run is never sent to a 2023 clause.</summary>
    private string DomainRuleCited(BoundIntrinsicCall ic) =>
        EditionInfo.Of(ctx.Data.DialectLevel).CiteRule(ic.Sig.DomainRule1985!, ic.Sig.DomainRule!);

    /// <summary>The §2023:15.4.1 floating-math family: arguments as doubles (through the one scaled→double
    /// conversion), the result landed by <see cref="RenderFloatNative"/> through the ONE FromDouble at the landing
    /// <see cref="ReceiverContext.FloatLanding"/> chooses (a final transfer into a fixed-point resultant: its scale and
    /// mode; any other shape: the binary64 stays), or converted into an SDIDI under a standard mode.</summary>
    private NumX RenderFloat(BoundIntrinsicCall ic, string method)
    {
        string call = FloatBody(ic, method);
        // ⛔ THE KIND OF THE BINARY64 IS ASKED ONCE, BY THE RULE THE BIND-TIME PREDICTION ASKS TOO (kb/Work PB4622): an engine
        // APPROXIMATION (the floating-math family, or a function of approximations only) lands by its shortest round-trip decimal;
        // a value derived from a float ITEM's content (ABS, MAX, FRACTION-PART, SUM ... of it) lands by its exact binary value, as
        // the item itself does (§2023:15.59.4 RV1 "the content of the argument", §2023:15.4.1).
        bool approximation = IntrinsicCarrierRules.Binary64ResultIsApproximation(ic.Sig.Float, AnyFloatItemArgument(ic));
        // ⛔ THE ARITHMETIC MODE IS TESTED BEFORE THE RECEIVER SHAPE, AND THE COMMENT BELOW SAYS WHY EACH ARM IS
        // WHERE IT IS (kb/Work PB253). §2023:15.4.1: "When standard-decimal arithmetic or standard-binary arithmetic is
        // in effect, the returned value for numeric and integer functions is contained in a temporary standard data
        // item in the intermediate form defined for the arithmetic mode in effect" — UNCONDITIONAL on whether the
        // function has an equivalent arithmetic expression AND on the shape of whatever consumes the value, and
        // §2023:8.8.1.5.1 makes the mode "a method of evaluating an arithmetic expression, an arithmetic statement, the
        // SUM clause, AND CERTAIN INTEGER AND NUMERIC FUNCTIONS as specified in 15.4.1" — the function itself, not
        // only its use as an operand. The last-¶ exemption ("When a numeric or integer function does not have an
        // equivalent arithmetic expression, its returned value is implementor-defined unless otherwise specified in
        // the function definition") exempts the VALUE — the binary64 approximation this family computes — never the
        // CONTAINER. This is the ordering NUMERIC_DESIGN.md D3 states for the whole engine ("the mode
        // branch runs BEFORE the D16 float branch") and that NumericRenderer.CombineCore, NumericRenderer.Power and
        // NumericRenderer.Landed already keep; this renderer was the one place that read the receiver first, so
        // under ARITHMETIC IS STANDARD-DECIMAL the SDIDI arm was UNREACHABLE for every receiver-less or
        // float-receiver reference and the raw binary64 escaped. Measured before the fix, with
        // A = 1.570796326794896619231321691639: `MOVE FUNCTION TAN(A)` landed 16331239353195368.96 (the binary64
        // ×10^scale artifact of CobolFloat.ToScaledUnchecked) while `COMPUTE R = FUNCTION TAN(A)` landed
        // 16331239353195370.00 — the SAME function, the SAME argument, TWO returned values in one run, which
        // §2023:15.4.1 forbids outright ("the returned value is the same for all instances of a given function within a
        // single execution of the runtime element so long as the value and order of the arguments, the collating
        // sequence, and the locale are unchanged"). The text channel diverged the same way: `DISPLAY FUNCTION
        // SIN(0.00000000000000000001)` printed `1E-20`, CobolFloat.Display's binary64 E-notation, where the
        // SDIDI's own item-92 text (CobolDec.ToFunctionText) is `0.00000000000000000001`.
        // The residue this reaches is exactly the prose family with no §2023:15.4.1 r1 equivalent arithmetic expression
        // and no RenderDec body — ACOS, ASIN, ATAN, COS, SIN, TAN, LOG, LOG10, RANDOM: everything else with
        // Float: true is already claimed above by RenderNum's standard-mode arm (E / PI / EXP / EXP10 by name,
        // SQRT / FACTORIAL / ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION by RenderDec's alwaysDec).
        //
        // ⛔ UNDER A STANDARD MODE THE RESULT CONVERTS IN, IT DOES NOT QUANTIZE (fix-queue PB56). The prose
        // family's returned value is an implementor-defined binary64 approximation in every mode (§2023:15.4.1 last ¶),
        // but under STANDARD-DECIMAL that approximation is CONTAINED IN an SDIDI (§2023:15.4.1) — CobolDec.FromApproximation,
        // its shortest round-trip decimal (an approximation is not a float data item, whose §2023:8.8.1.5.1 conversion is
        // the exact CobolDec.FromBinary64, kb/Work PB1606) — and lands at the receiver ONCE, instead of truncating to unscaled at a receiver-derived
        // working scale first (the triage rows RV-15.8.4-1 / RV-15.10.4-1 / RV-15.11.4-1 / RV-15.35.4-1 measured
        // the double quantization). RealResult stays inside the conversion: it is the NaN → EC-ARGUMENT-FUNCTION
        // raise site, so the screen fires identically in every mode and every receiver shape.
        //
        // ⛔ AND THE ORDER IS STRUCTURAL, NOT POSITIONAL. The native arms live in <see cref="RenderFloatNative"/>,
        // which is not given the mode and cannot test it, so an arm added there CANNOT pre-empt the container rule
        // — the defect this shape closes was exactly an arm sitting one line too high. `num.StandardDecimal` is
        // read HERE and nowhere below (StandardModeReturnedValueContainerDriftTests proves the property over every
        // Float: true catalog row, so a new float function inherits it without anyone remembering to).
        // The standard-mode lift is the ONE operand lift (NumericRenderer.DecOperand): an approximation enters by its shortest
        // round-trip decimal, a value derived from a float ITEM by §2023:8.8.1.5.1's conversion of the item (CobolDec.FromBinary64).
        return num.StandardDecimal
            ? new NumX(num.DecOperand(new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RealResult), call), 0, Real: true, Approximate: approximation)), 0, Dec: true)
            : RenderFloatNative(call, approximation);
    }

    /// <summary>The §2023:15.4.1 float family under NATIVE arithmetic — the receiver-shape arms, and ONLY those.
    /// <para>⛔ It is a separate method so the arithmetic mode is settled before any of this runs: the container
    /// question belongs to <see cref="RenderFloat"/>, this one answers the D16 quantization question. Deliberately
    /// mode-blind (kb/Work PB253).</para></summary>
    private NumX RenderFloatNative(string call, bool approximation)
    {
        // ⛔ WHETHER THIS VALUE LANDS AT ALL, AND AT WHAT SCALE AND MODE, IS ONE DECISION TAKEN ONCE ON
        // ReceiverContext (kb/Work PB647, then PB653 — ReceiverContext.FloatLanding carries the derivation):
        // the FINAL TRANSFER into a fixed-point resultant lands at THAT identifier's scale with the statement's
        // ROUNDED mode (§2023:14.7.4.1 — "truncation is relative to the size provided for the resultant identifier";
        // §2023:14.7.4.3 GR2 — no ROUNDED phrase IS ROUNDED MODE IS TRUNCATION), which is exactly the landing
        // `MOVE FUNCTION SQRT(3) TO S` performs (§2023:14.6.8.2 GR4), so §2023:15.4.1's one-returned-value identity
        // holds across the two channels. EVERY OTHER SHAPE — a nested intermediate, a float receiver, a
        // receiver-less render — keeps the binary64, because there is no transfer here to quantize FOR.
        var landing = num.Receiver.FloatLanding(num.Outermost);
        // A float RECEIVER keeps the transcendental result in the binary64 pipeline (full precision — SQRT(2) into a
        // COMP-2 is 1.4142135623730951, not the scale-9 1.414213562); a fixed receiver quantizes through FromDouble
        // at the landing chosen just above. (D16 review finding.)
        //
        // ⛔ AND SO DOES A NESTED OPERAND — THE THIRD SHAPE, AND THE ONE THAT WAS MISSING (kb/Work PB653).
        // A returned value feeding a larger expression is not transferred anywhere yet, so quantizing it at a
        // ≥9 working scale merely truncates it one digit too early and every operation above it propagates the
        // loss: `COMPUTE R = FUNCTION SQRT(3) * 2` into PIC 9V9(9) gave 3.464101614 where a COMP-2 item holding
        // the IDENTICAL binary64 gave 3.464101615, and `FUNCTION SQRT(10) ** 2` gave 9.999999998 against that
        // arm's 10.000000000. §2023:15.4.1 forbids the split — "the returned value is the same for all instances of a
        // given function within a single execution of the runtime element so long as the value and order of the
        // arguments, the collating sequence, and the locale are unchanged" — and the receiver-less RELATION
        // channel, which has always kept the binary64, already agreed with the COMP-2 item. Keeping it here makes
        // the four channels one value by CONSTRUCTION, and lets CombineCore's Real lane evaluate the expression
        // entirely in IEEE binary64 as numeric design D16 says it does. (Guard digits are NOT the alternative:
        // a landing cannot know how many multiplications sit above it, and two guarded scale-23 operands
        // multiply into a scale-46 product the Int128 carrier wraps — ReceiverContext.FloatLanding derives it.)
        //
        // ⛔ AND SO DOES A RECEIVER-LESS CONTEXT, UNDER NATIVE ARITHMETIC (fix-queue PB13, the half no
        // working-scale choice can reach — the standard-mode arm above now owns the other modes, which is the
        // scope this citation always had).
        // §2023:15.4.1: "When native arithmetic is in effect, the characteristics and representation of the returned
        // value are defined by the implementor" — WiseOwl COBOL's determination is that the §2023:15.4.1 float family's
        // returned value IS a binary64, and the quantization exists ONLY to land it in a fixed-point receiver,
        // whose scale is what defines the quantization. With no receiver there is no scale to quantize TO, and the
        // arbitrary ws = 9 stand-in was not merely a different rendering — it was WRONG, because FromDouble
        // saturates: `IF FUNCTION EXP10(30) = FUNCTION EXP10(31)` evaluated TRUE (both sides saturating to the
        // same Int128.MaxValue) and `DISPLAY FUNCTION EXP10(31)` printed 170141183460469231731687303715.884105727.
        // Every consumer of a receiver-less numeric already has a Real arm and is MORE correct on it: a relation
        // compares natively (§2023:8.8.4.2.4 — "comparison proceeds by the rules of native arithmetic", the same arm a
        // COMP-2 operand takes), the text channel renders through the one CobolFloat.Display a float ITEM uses
        // (§2023:14.9.11.4 GR1), and a MOVE source lands through CobolFloat.ToScaledUnchecked AT THE RECEIVER'S SCALE — which is
        // the form that cannot hide a size error: the checked landing RAISES past the carrier (kb/Work PB2967) and
        // the unchecked one keeps the low-order digits, so no saturation sentinel is ever stored as a value.
        // This is what makes FUNCTION SQRT(2) and a COMP-2 item holding that value the SAME value everywhere,
        // which OperandText.NumericIntrinsicText already documented as required and the ws = 9 form silently broke.
        // ⛔ THE UNQUANTIZED ARMS KEEP THE RAISE SITE. FromDouble is where an out-of-domain NaN became the §2023:15.3
        // EC-ARGUMENT-FUNCTION default (or the raise, under checking); leaving the value in binary64 skips it, so
        // `COMPUTE R = FUNCTION ACOS(2)` gave the default 0 while `IF FUNCTION ACOS(2) = 0` propagated a raw NaN
        // and compared FALSE. RealResult restores the screen without re-quantizing — a function's returned value
        // must not depend on the SHAPE of its receiver (§2023:15.4), and under EC-ARGUMENT-FUNCTION checking
        // §2023:14.6.13.1 requires the condition be raised at all. (Found by the Phase-B §2023:15.55 refuter.)
        // ⛔ THE BINARY64 THAT STAYS IS AN APPROXIMATION THE ENGINE PRODUCED, NOT A FLOAT ITEM'S CONTENT (kb/Work
        // PB1641). It is the SAME value a COMP-2 item holding it would carry, so every receiver-less channel (a
        // relation, the text channel, a MOVE source) still reads one binary64; but as an OPERAND it is flagged
        // Approximate, and NumericRenderer.CombineCore — the one place the lane is chosen — lifts it to the SDIDI
        // (its shortest-round-trip decimal) beside fixed-point operands, so `13.2 + FUNCTION SQRT(16)` is 17.2 and not
        // the 17.1 a truncating store of 13.199999999999999 + 4.0 gives.
        var value = new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RealResult), call), 0, Real: true, Approximate: approximation);
        if (!landing.Quantize)
            return value;
        // ⛔ ONE LANDING FOR EVERY FUNCTION OF THE FAMILY, WITH NO CODOMAIN CLAMP (kb/Work PB3829, overturning PB65's
        // clamp). A §15.x.4 bound (ACOS's [0, π], ASIN's and ATAN's ±π/2, RANDOM's [0, 1)) is a rule on the RETURNED
        // value, which the binary64 already satisfies; the value the resultant identifier receives is the returned
        // value ROUNDED per §2023:14.7.4.3 and checked per §2023:14.7.5 (§2023:14.9.8.4 GR2), so ROUNDED may legitimately land past
        // the bound (ACOS(-1) ROUNDED into 9V9999 is 3.1416) and a rounded value the receiver cannot hold is a size
        // error (RANDOM ROUNDED into V9(4)). The clamp cut those back to 3.1415 and 0.9999 for a bare reference only:
        // `FUNCTION ACOS(-1) + 0` already landed 3.1416, the nested arm above, and so did standard-decimal.
        // The quantizer's landing form past the carrier is the STATEMENT's (kb/Work PB77): saturate under ON SIZE
        // ERROR / EC-SIZE checking (the capacity check raises), the low-order digits for the no-phrase store.
        // ⛔ THE VALUE IT LANDS IS THE APPROXIMATION'S, THE SAME ONE THE OPERAND LIFT AND THE RECEIVER-LESS LANDINGS READ
        // (kb/Work PB4516): FromDouble takes the binary64's shortest round-trip decimal (CobolDec.FromApproximation),
        // as the `Approximate: true` NumX of the keep-the-binary64 arm above does when a store or an alignment lands it
        // (NumericRenderer.StoreArgs / EditedLanding / Align read NumX.Approximate). Landing the binary EXPANSION stored
        // 0.000000 for `COMPUTE R = FUNCTION EXP10(-6)` and 0.000001 for `COMPUTE R = 0 + FUNCTION EXP10(-6)`.
        // ⛔ A VALUE DERIVED FROM A FLOAT ITEM LANDS BY THE ITEM'S EXACT BINARY VALUE (kb/Work PB4622), through the landing `COMPUTE R = F`
        // and `MOVE F` take: ABS (DN) of -2.5E+40 stores 73792 in its low five digits, not the 00000 of its 17-digit shortest decimal.
        // RealResult keeps the NaN screen FromDouble owns.
        if (!value.Approximate)
            return new NumX(RuntimeApi.FloatToScaled(value.Expr, value.Approximate, $"{landing.Scale}", landing.Mode, num.CheckedLanding), landing.Scale);
        return new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.FromDouble), $"{call}, {landing.Scale}, {RuntimeApi.RoundingText(landing.Mode)}{CheckedFlag}"), landing.Scale);
    }

    // ── Argument rendering (the ONE NumericRenderer for every numeric-kind argument) ────────────────────────
    //
    // ⛔ THERE IS ONE ENTRY — `num.AsNum(operand, receiver)` — AND SEVERAL INTAKES OVER IT, WITH DIFFERENT VALUE
    // SEMANTICS. Every intake below therefore DECLARES its contract as `INTAKE(<class>)` in its doc comment, and
    // `IntrinsicArgumentIntakeContractDriftTests` fails if a member that calls `num.AsNum(` does not declare one.
    // The classes are exhaustive and mean exactly this — what the intake does to the argument's VALUE:
    //
    //   EXACT        the operand on its own carrier at its own scale; nothing is rescaled or converted.
    //   LIFTED       lifted to the SDIDI; the VALUE is preserved (a float converts in per §2023:8.8.1.5.1).
    //   ALIGNED      rescaled UP to the argument list's common maximum scale; no digit lost, headroom spent.
    //   LANDED       landed into the exact Int128 lane at the RECEIVER's working scale — TRUNCATES past it.
    //   INTEGRAL     rescaled to scale 0 with truncation (the §2023:15.3 integer positions).
    //   APPROXIMATED converted to binary64; exactness past 2^53 is surrendered.
    //   PREDICATE    rendered only to ask a question; the text is discarded, so no value semantics apply.
    //
    // ⛔ THE VOCABULARY EXISTS BECAUSE A ROW ABOUT "WHAT HAPPENS TO AN ARGUMENT" WAS VERIFIED FROM ONE OF THEM
    // (kb/Work PB251, row RV-15.4.1-2). The adjudication rested on "Arg/RawArg/DecArg is the ONE argument intake
    // for the numeric channel", and the file carried several with different semantics — so a claim true of
    // `RawArg` (it renders an argument without redefining its value) was recorded against `Arg`, which is the one
    // intake that CAN redefine it. Reading a class name off the member is now the whole check.

    /// <remarks>INTAKE(LANDED) — an SDIDI operand (and, under a standard mode, a float) is LANDED into the exact <c>Int128</c> lane at the receiver's working scale, truncating past it — the one intake that can change the argument's VALUE, which is why no function whose definition FIXES its value may consume its argument through it (kb/Work PB251).</remarks>
    private NumX Arg(BoundIntrinsicCall ic, int i) => Landed(RawArg(ic, i));

    /// <summary>
    /// ⛔ THE ONE SDIDI → EXACT-CARRIER LANDING FOR AN INTRINSIC ARGUMENT (fix-queue PB32/PB14).
    /// </summary>
    /// <remarks>
    /// <para><see cref="NumX"/> has THREE carriers — exact scaled <see cref="Int128"/>, the <c>CobolDec</c> SDIDI,
    /// and binary64 — and this renderer's arms were written for two. Under <c>ARITHMETIC IS STANDARD-DECIMAL</c>
    /// every arithmetic expression renders as a <c>Dec</c> carrier (<c>NumericRenderer.CombineCore</c> /
    /// <c>Power</c>), so a §2023:15.3 type-10 arithmetic-expression argument — legal at 2014 and 2023 — was handed raw
    /// to a body expecting <c>Int128</c> and the user saw a Roslyn <c>CS1503</c> on conforming COBOL.</para>
    /// <para>⛔ IT IS LANDED HERE, AT <see cref="Arg"/>, AND NOT IN EACH ARM. Placing the arm in
    /// <c>NumericRenderer.Align</c> alone fixed only the variadic family that happens to route through it —
    /// measured: MAX / MIN / MOD / MEDIAN recovered while ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still
    /// failed to compile, because those arms consume <c>Arg(...).Expr</c> (or <see cref="AsInt"/>) directly. Every
    /// numeric argument in this renderer originates HERE, so one landing covers all of them and the NEXT arm
    /// added is covered without anyone remembering to (feedback_change_the_dispatch_not_the_callers). Landing
    /// early also gives the SDIDI a compile-time <c>Scale</c>, so it participates in
    /// <see cref="AlignedArgs"/>'s common-scale maximum instead of contributing the placeholder 0 and dragging
    /// the whole argument list down to integer alignment.</para>
    /// <para>⚠ THE SCALE IS A CHOICE, AND IT IS AN IMPLEMENTOR'S CHOICE — NO CLAUSE PRESCRIBES IT. An SDIDI carries
    /// its exponent at RUN time, so there is no compile-time scale to preserve; the value lands through the same
    /// <c>WorkingScale(floor)</c> discipline the float family uses — the receiver's scale, never below
    /// <see cref="ReceiverContext.SdidiLandingScaleFloor"/>, capped at the receiver's <c>Int128</c> headroom by the
    /// PB13 argument. ⛔ THAT FLOOR IS NOT A NUMVAL RULE AND MUST NOT BE DESCRIBED AS ONE (kb/Work PB251): the
    /// sentence here used to read "never below the §2023:15.67 fraction floor", and §2023:15.67 prescribes no working scale
    /// at all — the family's value is fixed by §2023:15.67.4 RV1 and now renders on the SDIDI carrier in every mode.
    /// ⛔ THE §2023:15.4.1 r1 FAMILY NO LONGER PASSES THROUGH HERE (fix-queue PB56): under a standard mode a
    /// Dec/float-bearing exact-family call routes to <see cref="RenderDec"/> BEFORE any landing, so this
    /// truncation now serves only the residual non-EAE consumers (the integer-argument intake via
    /// <c>AsInt</c>, and the arms with no Dec body) — where scale-0/format semantics make the landing the
    /// defined behavior rather than an approximation.</para>
    /// ⛔ <b>AND A FLOAT OPERAND LANDS THE SAME WAY UNDER A STANDARD ARITHMETIC MODE (fix-queue PB38).</b> This is
    /// where the ARITHMETIC MODE beats the float branch, which is the ordering `NUMERIC_DESIGN.md` D3
    /// states in words ("the mode branch runs BEFORE the D16 float branch") and which
    /// <c>NumericRenderer.CombineCore</c>, <c>NumericRenderer.Power</c> and <c>ConditionRenderer</c> all obey —
    /// <c>RenderNum</c> was the ONE renderer that did not, so a single COMP-1/COMP-2 argument demoted the whole
    /// list to binary64 even under <c>ARITHMETIC IS STANDARD-DECIMAL</c>, where §2023:15.4.1 r1 is unconditional (the
    /// returned value <i>shall equal</i> the equivalent arithmetic expression) and §2023:8.8.1.5.2 GR1 converts every
    /// fixed-point operand into an SDIDI EXACTLY. MEASURED before the fix, with three 18-digit items and a COMP-2
    /// pair: <c>FUNCTION MEDIAN(H1 H2 H3 F1 F1)</c> returned 100000000000000004.76 where the SDIDI-exact answer
    /// is 100000000000000001, and <c>FUNCTION MAX(H1 H2 H3 F1)</c> returned the same 100000000000000004.76
    /// against 100000000000000003 — the three distinct 18-digit operands all collapse to ONE binary64 (the ulp
    /// at 1e17 is 16) and compare EQUAL, so the §2023:8.8.4.2.4 comparison the clause mandates never happens. The same
    /// operands without the float are exact in the same program.
    /// <para>The float converts through <see cref="NumericRenderer.DecOperand"/> — the compiler's own §2023:8.8.1.5.1
    /// conversion, the one <c>CombineCore</c> and <c>Power</c> already use — and then lands by the identical
    /// route a <c>Dec</c> operand takes. So there is ONE landing here, not a second mechanism beside the first,
    /// and the exact Int128 family evaluates the EAE as §2023:15.4.1 r1 requires.</para>
    /// <para>The landing of an argument already rendered in the argument context (<c>NumericRenderer.ArgumentReceiver</c>): it reads
    /// only the receiver's scale and integer-digit headroom (<see cref="ReceiverContext.WorkingScale"/>), which the argument context
    /// shares with the statement's receiver, and never a lane fact, so it reads the statement's receiver.</para>
    /// </remarks>
    private NumX Landed(NumX x) => num.Landed(x, num.Receiver);   // the ONE landing (NumericRenderer.Landed — kb/Work PB84)

    // ── The STANDARD-DECIMAL body dispatch (fix-queue PB56) ──────────────────────────────────────────────────

    /// <summary>⛔ THE CONTEXT ARGUMENT <paramref name="i"/> IS RENDERED IN (kb/Work PB5891 review R3-1, kb/Work PB5893):
    /// <see cref="NumericRenderer.ArgumentReceiver"/> at the argument's position, a §2023:15.3 type 6 (integer) position or not. Every
    /// position renders without the receiver's floating-point facts; under a floating-point receiver a numeric position also keeps
    /// significance (<c>NumericLanes.ArgumentKeepsSignificance</c>).</summary>
    private ReceiverContext ReceiverFor(BoundIntrinsicCall ic, int i) =>
        num.ArgumentReceiver(ic.CarrierFacts.IsIntegerPosition(i));

    /// <summary>The context of an operand the intake reads without its index: an element of a table(ALL) argument or an argument of a
    /// variadic numeric list, and the leading rate of PRESENT-VALUE and a binary64-family argument-1, each at a §2023:15.3 type 10
    /// (numeric) position.</summary>
    private ReceiverContext NumericArgument => num.ArgumentReceiver(integerPosition: false);

    /// <summary>
    /// ⛔ AN ARGUMENT AS ITS POSITION TAKES IT, WITHOUT THE UNSCALED LANDING — the one indexed entry every numeric-channel
    /// intake and lane predicate reads (<see cref="Arg"/>, <see cref="Dbl"/>, <see cref="DecArg"/>, <see cref="IntArg"/>,
    /// <see cref="Operands"/>). At a §2023:15.3 type-6 position it is the operand's INTEGER (kb/Work PB651).
    /// </summary>
    /// <remarks>
    /// <para>§2023:15.3 type 6: "An arithmetic expression that will always result in an integer value or an integer data item
    /// shall be specified." Strict conformance refuses a data item or literal that is not one (COBOL1627, kb/Work
    /// PB248) and fails open on an arithmetic expression it cannot prove non-integral, so an expression (<c>A9 * 7</c>,
    /// <c>A ** 3</c>) brings a fractional or SDIDI-carried value to such a position in every mode, and
    /// <c>--permissive</c> also a floating-point item or a fixed-point item with fraction digits — and
    /// docs/CONFORMANCE.md DOC-A.1-124 fixes what it then means: an integer when its value has no nonzero fraction
    /// digit, otherwise EC-ARGUMENT-FUNCTION with checking on and the TRUNCATED integer with it off. That is a
    /// property of the POSITION, so it is taken here, ONCE, before any arm or lane predicate sees the operand, on every
    /// carrier, keeping the operand's VALUE or saying it cannot. An SDIDI operand STAYS on the SDIDI
    /// (<c>CobolIntrinsics.IntegerArgUnboundedDec</c>): it holds 34 significant digits at ANY exponent, so its integer
    /// reaches far past <c>Int128</c> — MOD(10 ** 40, 3) (kb/Work PB2630) and MOD(A ** 3, B) of a 15-digit A
    /// (kb/Work PB69) are integers the SDIDI body evaluates, and landing them on <c>Int128</c> was EC-SIZE-OVERFLOW
    /// (the PB651 finisher's Linux-gate red). A fixed-point operand's integer fits the <c>Int128</c> carrier its
    /// unscaled value already rides, and a binary64 one takes the ONE type-6 intake at the total width
    /// (<see cref="IntegerIntake"/>), past ±1.7e38 raising EC-ARGUMENT-FUNCTION as outside what the implementation
    /// can represent (<c>CobolIntrinsics.IntegerArgWideReal</c>). A bounded body narrows the integer again through
    /// <see cref="IntArg"/> and keeps its own range raise.</para>
    /// <para>⛔ IT NEVER SATURATES (kb/Work PB651 review R1-2). Clamping a binary64 past the carrier to
    /// <see cref="Int128.MaxValue"/> answers for a DIFFERENT integer, so every function whose value depends on its
    /// argument's value answered silently wrong — MOD(1.0E39, 7) = 1 where the integer 10³⁹'s binary64 value answers
    /// 5, ANNUITY(0, 1.0E39) = 1 / 1.7e38. The clamp is right only where the function's answer is CONSTANT past a
    /// bound, and that is the arm's knowledge, not the position's: such an arm takes <see cref="IntArgWideSaturating"/>
    /// instead.</para>
    /// <para>⛔ BEFORE THIS, THE ARMS EACH DECIDED, AND THREE DECIDED WRONG — measured by
    /// <c>IntrinsicIntegerPositionPermissiveDriftTests</c> over every verified type-6 position: MOD's scaled arm aligned
    /// 7.5 and answered MOD(7.5, 3) = 1.5 where the integer 7 answers 1; a float at COMBINED-DATETIME's argument-1
    /// moved the call to the binary64 lane, whose body added 143951.5 as it stood; ANNUITY's binary64 body took 3.5
    /// periods. A float argument made a function's value depend on how its argument was stored — the invariant kb/Work
    /// PB21 was opened for. And because an integer has no binary64-ness, a float at a type-6 position no longer moves a
    /// call to the binary64 lane at all: <see cref="AnyRealArgument"/> reads this, so the lane is entered only through a
    /// type-10 (numeric) position, where §2023:15.4.1's licence is an approximation of the equivalent arithmetic expression's
    /// own binary64 operand.</para>
    /// <para>INTAKE(EXACT) — the operand on its OWN carrier at its OWN scale, nothing rescaled, converted or truncated —
    /// EXCEPT at a §2023:15.3 type-6 position, where it is INTEGRAL: an SDIDI operand becomes its integer on the SDIDI, a
    /// float or a fractional fixed-point operand lands at scale 0 through the type-6 intake (a scale-0 fixed-point
    /// integer passes as it stands), a float past the Int128 carrier raising rather than changing its value.</para>
    /// <para>⛔ AN ARITHMETIC EXPRESSION PAST THE Int128 CARRIER (a nested product of 39 to 77 digits, <c>NumX.Wide</c>) IS
    /// LOWERED HERE, and only here, for the consumers that have no wide form: <see cref="RawWideArg"/> keeps it exact and this
    /// settles it into the SDIDI by round-to-odd, as <c>NumericRenderer.Settle</c> does for every receiver-less operand
    /// (kb/Work PB1900). The exact family never reads it lowered: <see cref="RenderWide"/> evaluates such a call on the wide form
    /// first (kb/Work PB3953), so <c>ABS (BIG * BIG * K3)</c> of 18-, 18- and 3-digit items is the exact 39-digit product, as
    /// GnuCOBOL's arbitrary-precision argument temporary is. The bounded integer positions still take the image, and rightly:
    /// a value past 34 significant digits is outside the function's domain on either form. MEAN divides the exact wide sum ONCE
    /// (<see cref="RenderWide"/>'s <c>MeanScaled</c> arm, <c>CobolIntrinsics.MeanWide</c>; kb/Work PB4075), and the binary64 family's
    /// ill-conditioned bodies take the exact wide value (<see cref="WideLeadArg"/>, <see cref="ExactIntake.Wide"/>): SIN of a
    /// 39-digit product reduces that product, not an image some 999 radians away. The consumers that still read the image, each
    /// for the reason its value cannot depend on the lost digits: EXP, EXP10 and SQRT (condition numbers of at most 709, 709 and ½,
    /// against an image error of 10^−34 relative), the bounded integer positions, COMBINED-DATETIME's argument-2, and every
    /// STANDARD-DECIMAL body (§2023:8.8.1.5.2 GR1 rounds the operand there). Under native arithmetic the SDIDI lane itself takes the exact
    /// value (<see cref="ExactValueArg"/>, kb/Work PB4111, PB4568): VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE, and the exact
    /// family beside an SDIDI or binary64 argument, form their value from <c>CobolExact</c> arguments and round it once.</para>
    /// </remarks>
    private NumX RawArg(BoundIntrinsicCall ic, int i) => NumericRenderer.LowerWide(RawWideArg(ic, i));

    /// <summary>
    /// ⛔ AN ARGUMENT AS ITS POSITION TAKES IT, KEEPING THE EXACT WIDE FORM (kb/Work PB3953) — <see cref="RawArg"/>'s intake before
    /// the lowering: the operand rendered with <c>keepWide</c>, so an arithmetic expression past the <see cref="Int128"/> carrier
    /// arrives as the exact 256-bit intermediate (<c>NumX.Wide</c>) the arithmetic statements and the relation already keep.
    /// </summary>
    /// <remarks>
    /// <para>INTAKE(EXACT) — the operand on its OWN carrier at its OWN scale, the exact wide form included, nothing rescaled,
    /// converted or truncated — EXCEPT at a §2023:15.3 type-6 position, where it is INTEGRAL by the one intake of every carrier: an
    /// SDIDI operand becomes its integer on the SDIDI, a wide one its integer on the wide form
    /// (<c>CobolIntrinsics.IntegerArgWide</c>, so MOD (BIG * BIG * K3, 1000) sees every digit of its dividend and the
    /// integrality test reads the exact fraction), a run-time-scaled exact one (<c>NumX.Exact</c>, kb/Work PB4575) its integer on
    /// its own carrier (<c>CobolIntrinsics.IntegerArgExact</c>), a float or a fractional fixed-point operand lands at scale 0
    /// through the type-6 intake. Under ARITHMETIC IS STANDARD a standard intermediate data item argument of a function with no
    /// equivalent arithmetic expression is first rounded to 31 digits, the rule's own value of the argument (<see cref="StandardArgument"/>,
    /// §2002:8.8.1.3.1.3 situation 2).</para>
    /// <para>⛔ AN EXACT VALUE IT RETURNS IS OWED A SETTLEMENT (<c>ExactWideSettlementDriftTests</c>): its readers are
    /// <see cref="RawArg"/>, which lowers it, <see cref="RenderWide"/>, whose result is a <c>NumX.Wide</c> that
    /// <c>NumericRenderer</c> settles at its public entry like any nested product (and which declines a <c>NumX.Exact</c>), and
    /// <see cref="ExactValueArg"/>, which takes it as the <c>CobolExact</c> it is.</para>
    /// </remarks>
    private NumX RawWideArg(BoundIntrinsicCall ic, int i)
    {
        NumX x = num.AsNum(ic.Args[i], ReceiverFor(ic, i), keepWide: true);
        x = StandardArgument(ic, x);
        if (!(x.Real || x.Dec || x.Exact || x.Scale != 0) || !IntrinsicArgumentRules.IsIntegerPosition(ic.Sig.Name, i)) return x;
        var site = UndecidedIntegerSite(ic, i);
        if (x.Wide)
        {
            string wide = site is { } w ? RuntimeApi.CheckedIntegerArgumentWide(x.Expr, x.Scale, w) : x.Expr;
            return new NumX(RuntimeApi.IntegerArgWide(wide, x.Scale), 0, Wide: true,
                Digits: x.Scale > 0 ? Math.Max(1, x.Digits - x.Scale) : x.Digits - x.Scale);
        }
        if (x.Exact)
            return new NumX(RuntimeApi.IntegerArgExact(site is { } e ? RuntimeApi.CheckedIntegerArgumentExact(x.Expr, e) : x.Expr), 0,
                Exact: true, Precision: 0);
        return x.Dec
            ? x with { Expr = RuntimeApi.IntegerArgUnboundedDec(site is { } d ? RuntimeApi.CheckedIntegerArgument(RuntimeApi.IntegerArgCarrier.Dec, x.Expr, 0, d) : x.Expr), WithPrecision = null }
            : new NumX(IntegerIntake(x, wide: true, site), 0);
    }

    /// <summary>⛔ THE SITE OF THE RUN-TIME INTEGER CHECK (kb/Work PB5889): non-null exactly when the argument at the integer position
    /// <paramref name="i"/> is one the compile-time screen neither proved nor refuted (<see cref="IntrinsicArgumentRules.NeedsRunTimeIntegerCheck"/>),
    /// so the value is tested where it is evaluated and a failure names the function and the argument. The failure takes the one
    /// path of every broken argument rule (kb/Work PB6019): a COBOL-85 element terminates the run unit (the 1985 text has no
    /// exception conditions; §1993:p.B-46 lets the run terminate), a later one raises EC-ARGUMENT-FUNCTION (§2023:15.3 AR14).</summary>
    private static RuntimeApi.IntegerArgumentSite? UndecidedIntegerSite(BoundIntrinsicCall ic, int i) =>
        IntrinsicArgumentRules.NeedsRunTimeIntegerCheck(ic.Args[i], ic.Arithmetic)
            ? new RuntimeApi.IntegerArgumentSite(ic.Sig.Name, i + 1)
            : null;

    /// <summary>A written argument as its function receives it under ARITHMETIC IS STANDARD: a standard intermediate data item (an
    /// arithmetic expression's result, <c>NumX.Dec</c>) that "is the argument of a function and there is no equivalent arithmetic
    /// expression defined for the rules of the function" is rounded to 31 digits (§2002:8.8.1.3.1.3 situation 2; kb/Work PB5575),
    /// unless the function is one whose argument keeps all 32 (<see cref="IntrinsicCarrierRules.RoundsStandardArgument"/>). So
    /// FUNCTION INTEGER-OF-DATE (20000100 + 1 / K * K), K = 3 - the 32-digit 20000100.999999999999999999999999 - receives the date 20000101. Every
    /// other mode and argument is unchanged (<see cref="NumericRenderer.SidiRounded"/>). An element of a table(ALL) argument is a data
    /// item, never a standard intermediate data item, so this written-argument intake is the whole of the rule.</summary>
    private NumX StandardArgument(BoundIntrinsicCall ic, NumX x) =>
        num.StandardIntermediate && x.Dec && IntrinsicCarrierRules.RoundsStandardArgument(ic.Sig.Name, ctx.Data.DialectLevel)
            ? x with { Expr = num.SidiRounded(x, x.Expr), WithPrecision = null }
            : x;

    /// <summary>
    /// ⛔ THE WIDE INTEGER INTAKE FOR AN ARGUMENT WHOSE FUNCTION ANSWERS THE SAME PAST A BOUND — the one place a binary64
    /// operand past the <c>Int128</c> carrier is CLAMPED to the carrier's extreme of its sign
    /// (<c>CobolIntrinsics.IntegerArgSaturatingReal</c>) instead of raising (kb/Work PB254, PB651 review R1-2).
    /// </summary>
    /// <remarks>
    /// <para>The clamp substitutes one integer for another, so it is correct exactly where the returned value cannot tell
    /// them apart. Members, each with its runtime witness in
    /// <c>IntrinsicCarrierAgreementDriftTests.TheTotalArguments_AnswerPastLong_AndRaiseNothingUnderChecking</c>:
    /// TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD argument-1 (§2023:15.90.4 RV1a / §2023:15.91.4 RV1a — a CATCH-ALL verdict for every
    /// integer outside the window, however far) and FIND-STRING argument-3 (§2023:15.37.4 RV2/r3 — ignoring at least as many
    /// matches as exist answers 0, and a negative count ignores none). Both functions are TOTAL, so a raise there would be
    /// manufactured (§2023:15.3 has no incorrect argument to key on). A function whose value DEPENDS on the integer — MOD,
    /// RANDOM's seed, ANNUITY's periods, BOOLEAN-OF-INTEGER's bit configuration — never takes it:
    /// <c>IntrinsicCarrierAgreementDriftTests.OnlyAnArgumentWhoseAnswerIsConstantPastABound_Saturates</c> holds the arm
    /// set closed.</para>
    /// <para>INTAKE(INTEGRAL) — the operand as an integer on the <c>Int128</c> carrier: a fixed-point carrier through
    /// <see cref="AsIntWide"/>, a binary64 or an SDIDI one integrality-tested (DOC-A.1-124) and clamped past the
    /// carrier. The SDIDI needs the clamp as much as binary64 does: its integer reaches far past <c>Int128</c>, and
    /// <c>TEST-DATE-YYYYMMDD(10 ** 40)</c> under STANDARD-DECIMAL was a fatal EC-SIZE-OVERFLOW through the wide landing,
    /// where §2023:15.90.4 RV1a answers 1.</para>
    /// </remarks>
    private string IntArgWideSaturating(BoundIntrinsicCall ic, int i)
    {
        NumX x = num.AsNum(ic.Args[i], ReceiverFor(ic, i));
        var site = UndecidedIntegerSite(ic, i);
        return x.Real ? RuntimeApi.IntegerArgSaturatingReal(site is { } r ? RuntimeApi.CheckedIntegerArgument(RuntimeApi.IntegerArgCarrier.Real, x.Expr, 0, r) : x.Expr)
            : x.Dec ? RuntimeApi.IntegerArgSaturatingDec(site is { } d ? RuntimeApi.CheckedIntegerArgument(RuntimeApi.IntegerArgCarrier.Dec, x.Expr, 0, d) : x.Expr)
            : IntegerIntake(x, wide: true, site);
    }

    /// <summary>Every argument as its position takes it (<see cref="RawArg"/>), in order — what a lane predicate asks.</summary>
    private IEnumerable<NumX> Operands(BoundIntrinsicCall ic) => Enumerable.Range(0, ic.Args.Count).Select(i => RawArg(ic, i));

    /// <summary>An argument lifted to a <c>CobolDec</c> expression from its RAW carrier: a Dec operand passes
    /// through, a float converts per §2023:8.8.1.5.1, a fixed-point operand lifts through <see cref="NumericRenderer.DecOperand"/>
    /// (exactly within the SDIDI's 34 digits, rounded once past them — §2023:8.8.1.5.2 GR1) — never quantized to a working scale.</summary>
    /// <remarks>INTAKE(LIFTED) — lifted to the SDIDI from the RAW carrier — exact for a fixed-point operand within 34 digits and rounded once past them (§2023:8.8.1.5.2 GR1), §2023:8.8.1.5.1 for a float; the value is preserved up to the SDIDI's own precision, the carrier is not.</remarks>
    private string DecArg(BoundIntrinsicCall ic, int i) => num.DecOperand(RawArg(ic, i));

    /// <remarks>INTAKE(LIFTED) — the variadic form of <see cref="DecArg"/> — same contract, per element.</remarks>
    private string DecArgList(BoundIntrinsicCall ic) =>
        ArgArray(ic, 0, "CobolDec", DecOf) ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => DecArg(ic, i)));

    /// <summary>One operand lifted to a <c>CobolDec</c> from its RAW carrier — the per-operand form of <see cref="DecArg"/>
    /// (a table(ALL) element renders here inside its enumeration lambda).</summary>
    /// <remarks>INTAKE(LIFTED) — the per-operand form of <see cref="DecArg"/> — same contract.</remarks>
    private string DecOf(BoundOperand a) => num.DecOperand(num.AsNum(a, NumericArgument));

    /// <summary>⛔ AN ARGUMENT AS ITS EXACT VALUE (<c>CobolExact</c>; kb/Work PB4111, PB4568): the position intake keeping the wide form
    /// (<see cref="RawWideArg"/>, so a §2023:15.3 type-6 position is still its integer), then <see cref="NumericRenderer.ExactOperand"/> —
    /// never <see cref="DecArg"/>'s 34-digit image of a wide argument. The intake of the native SDIDI lane's <c>…Exact</c> bodies.</summary>
    /// <remarks>INTAKE(EXACT) — the operand on its own carrier with every digit; a binary64 as its one SDIDI conversion.</remarks>
    private string ExactValueArg(BoundIntrinsicCall ic, int i) => num.ExactOperand(RawWideArg(ic, i));

    /// <remarks>INTAKE(EXACT) — the variadic form of <see cref="ExactValueArg"/> — same contract, per element (a table(ALL) element
    /// is a data item, never wide).</remarks>
    private string ExactValueArgList(BoundIntrinsicCall ic) =>
        ArgArray(ic, 0, RuntimeApi.ExactType, a => ExactValueOf(ic, a))
        ?? string.Join(", ", Enumerable.Range(0, ic.Args.Count).Select(i => ExactValueArg(ic, i)));

    /// <summary>One operand as its exact value: a written argument through <see cref="ExactValueArg"/> (its own position's intake),
    /// an element of a table(ALL) argument directly.</summary>
    /// <remarks>INTAKE(EXACT) — the per-operand form of <see cref="ExactValueArg"/> — same contract.</remarks>
    private string ExactValueOf(BoundIntrinsicCall ic, BoundOperand a)
    {
        for (int i = 0; i < ic.Args.Count; i++)
            if (ReferenceEquals(ic.Args[i], a)) return ExactValueArg(ic, i);
        return num.ExactOperand(num.AsNum(a, NumericArgument));
    }

    /// <summary>One operand as a C# double from its RAW carrier — the per-operand form of <see cref="Dbl"/>.</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier — exactness past 2^53 is surrendered. §2023:15.4.1's native latitude permits that for an equivalent-arithmetic-expression function; a standard mode and a value-fixing definition do not.</remarks>
    private string DblOf(BoundOperand a) => NumericRenderer.Real(num.AsNum(a, NumericArgument));

    // ── table(ALL) arguments — the enumeration seam (ISO §2023:15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>The C# expression enumerating a table(ALL) argument's elements as a <c>T[]</c>:
    /// <c>CobolTable.AllArgs</c> over the place's ranges — each a lambda over the index vector, so a nested
    /// dynamic-capacity table's capacity reads the OUTER occurrence's — with the element rendered by
    /// <paramref name="element"/> from the element operand (its subscripts are the index variable's slots).</summary>
    /// <param name="lead">When given, renders the FIRST enumerated element instead (<c>CobolTable.AllArgs</c>'s
    /// lead lambda) — the table(ALL) is the leading argument and its first element is argument-1.</param>
    private static string AllArgsExpr(TableAllPlace all, string csType, Func<BoundOperand, string> element,
        Func<BoundOperand, string>? lead = null)
    {
        string v = LocalNames.Of(all.Indices.Local);
        var e = new BoundFieldOperand(all.Element);
        return RuntimeApi.TableAllArgs(csType, PlaceRenderer.AllCountLambdas(all), $"{v} => {element(e)}", lead is null ? null : $"{v} => {lead(e)}");
    }

    /// <summary>The argument list from position <paramref name="from"/> on as ONE <c>T[]</c> expression when a
    /// table(ALL) argument is among them (else null — the caller keeps its comma-list form, byte-identical to
    /// before): runs of written operands become array literals, each ALL an <see cref="AllArgsExpr"/>, joined
    /// in source order by <c>CobolTable.ArgConcat</c> — the ONE array a <c>params ReadOnlySpan&lt;T&gt;</c> body takes (kb/Work PB2636).</summary>
    /// <param name="lead">When given, renders the list's FIRST argument (the operand at <paramref name="from"/>, or
    /// the first element that operand's table(ALL) enumerates) — a leading argument with its own intake.</param>
    internal string? ArgArray(BoundIntrinsicCall ic, int from, string csType, Func<BoundOperand, string> render,
        Func<BoundOperand, string>? lead = null)
    {
        if (!ic.Args.Skip(from).Any(a => a is BoundFieldOperand { Place: TableAllPlace })) return null;
        var parts = new List<string>();
        var run = new List<string>();
        void Flush() { if (run.Count > 0) { parts.Add($"new {csType}[] {{ {string.Join(", ", run)} }}"); run.Clear(); } }
        Func<BoundOperand, string>? firstLead = lead;   // consumed by the first argument only
        foreach (var a in ic.Args.Skip(from))
        {
            if (a is BoundFieldOperand { Place: TableAllPlace all }) { Flush(); parts.Add(AllArgsExpr(all, csType, render, firstLead)); }
            else run.Add((firstLead ?? render)(a));
            firstLead = null;
        }
        Flush();
        return parts.Count == 1 ? parts[0] : RuntimeApi.TableArgConcat(csType, parts);
    }

    /// <summary>A body with a LEADING positional argument and a <c>params</c> tail (PRESENT-VALUE's rate, then the
    /// amounts): the tail may enumerate; a table(ALL) in the LEADING position itself is legal too (§2023:15.3 puts no
    /// position on the ALL — "as if each table element … were specified"), so then the flat list is bound once
    /// and split at run time.</summary>
    /// <param name="lead">The LEADING argument's own intake (PRESENT-VALUE's screened discount base), in place of
    /// <paramref name="render"/>. ⛔ Also when that argument is itself a table(ALL) (kb/Work PB1000): the lead then
    /// renders the enumeration's FIRST element — argument-1 (ISO §2023:15.3 AR14's left-to-right order) — from the
    /// element operand, inside the walk (<c>CobolTable.AllArgs</c>'s lead lambda), before it joins the list.
    /// Screening the list's <c>[0]</c> instead read a binary64 image: a legal rate −0.99999999999999999999 had
    /// already become −1.0 there and was refused under §2023:15.74.3 r2.</param>
    private string LeadThenTail(BoundIntrinsicCall ic, string method, string prefix, string csType, Func<BoundOperand, string> render, string mid = "", Func<BoundOperand, string>? lead = null)
    {
        if (ic.Args[0] is BoundFieldOperand { Place: TableAllPlace })
        {
            string xs = NextWithVar();
            return RuntimeApi.With(ArgArray(ic, 0, csType, render, lead)!, xs, RuntimeApi.Intrinsic(method, $"{prefix}{xs}[0], {mid}{xs}.AsSpan(1)"));
        }
        string tail = ArgArray(ic, 1, csType, render) ?? string.Join(", ", ic.Args.Skip(1).Select(render));
        return RuntimeApi.Intrinsic(method, $"{prefix}{(lead ?? render)(ic.Args[0])}, {mid}{tail}");
    }

    /// <summary>A fresh name for a <c>CobolTable.With</c> binding — nested bindings must not shadow.</summary>
    private string NextWithVar() => $"__xs{_withSerial++}";
    private int _withSerial;

    /// <summary>Does any argument arrive as a Dec (SDIDI) or float carrier, before any landing?</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question; the rendered text is discarded, so no value semantics apply.</remarks>
    private bool AnyDecOrRealRaw(BoundIntrinsicCall ic) =>
        Operands(ic).Any(x => x.Dec || x.Real);

    /// <summary>Can aligning this call's arguments to their common scale leave the <see cref="Int128"/> carrier? The rule is
    /// <see cref="IntrinsicCarrierRules.AlignmentMayLeaveCarrier"/> (the ONE reader, which the bind-time lane prediction asks of
    /// the same facts read from the bound tree); this reads them off the RAW operands: an SDIDI or binary64 operand is not
    /// provable and answers <c>null</c>; one of unknown digit bound (<see cref="NumX.Digits"/> 0) is stated with that bound, which
    /// the rule reads as <see cref="IntrinsicCarrierRules.AlignedOperand.Unbounded"/> (kb/Work PB4792).</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the RAW operand: its carrier flags and its digit bound.</remarks>
    private bool AlignmentMayLeaveCarrier(BoundIntrinsicCall ic) =>
        IntrinsicCarrierRules.AlignmentMayLeaveCarrier(ic.Sig.Name, AlignedOperands(ic));

    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the RAW operand: its carrier flags and its digit bound.</remarks>
    private List<IntrinsicCarrierRules.AlignedOperand?> AlignedOperands(BoundIntrinsicCall ic) =>
        Enumerable.Range(0, ic.Args.Count)
            .Select(i => RawWideArg(ic, i))
            .Select((w, i) => w.Exact
                // ⛔ A WHOLE-VALUE ARGUMENT (a nested SUM / RANGE / MEDIAN / MIDRANGE over a list that left the carrier, an expression in which a
                // wide value met an SDIDI operand) IS NOT AN SDIDI OPERAND (kb/Work PB4860): RawArg lowers it to its 34-digit image, which
                // read here as the SDIDI's and sent SUM (SUM (A B) Z) to SumExact, whose 34 digits rounded the 35-digit value the whole
                // lane keeps. It is an operand of unknown bound and run-time scale, which the rule reads as shifting.
                ? new IntrinsicCarrierRules.AlignedOperand(0, 0, false, RunTimeScaled: true)
                : NumericRenderer.LowerWide(w) is { Dec: false, Real: false } x
                    ? new IntrinsicCarrierRules.AlignedOperand(x.Digits, x.Scale, ic.Args[i] is BoundFieldOperand { Place: TableAllPlace })
                    : (IntrinsicCarrierRules.AlignedOperand?)null)
            .ToList();

    /// <summary>⛔ A NATIVE CROSS-ALIGNING LIST THAT LEAVES THE Int128 CARRIER IS EVALUATED ON THE EXACT FAMILY'S WHOLE VALUE (kb/Work PB4792,
    /// review R1-1, and PB4826 for the list whose arguments all have a bound): the arguments enter as their exact values and the sum,
    /// difference or halved sum is returned unrounded, for the receiver to round once (§2023:14.7.4.3). The SDIDI bodies round the value to
    /// 34 digits first, so SUM (FUNCTION INTEGER (A) F) of 10000 and .9999999999999999999999999999999 would store 10001.00000 where the
    /// Int128 body, the arithmetic expression and SUM (A F) give 10000.99999, and SUM (A B) ROUNDED of a 26-digit A and .004999999
    /// would store .01 where A + B stores .00 (§2023:15.4.1 leaves the approximation to the implementor; CLAUDE.md rule 1 sends it to
    /// GnuCOBOL, whose decimal temporary is exact). Null for a function this family does not cover.</summary>
    private NumX? RenderWholeAligned(BoundIntrinsicCall ic) => ic.Sig.Name switch
    {
        "SUM" => Exact(nameof(CobolIntrinsics.SumWhole), ExactValueArgList(ic), null),
        "RANGE" => Exact(nameof(CobolIntrinsics.RangeWhole), ExactValueArgList(ic), null),
        "MEDIAN" => Exact(nameof(CobolIntrinsics.MedianWhole), ExactValueArgList(ic), null),
        "MIDRANGE" => Exact(nameof(CobolIntrinsics.MidrangeWhole), ExactValueArgList(ic), null),
        _ => null,
    };

    /// <summary>Would landing this MAX/MIN selection at its arguments' common scale overrun the receiver-bound Int128 headroom
    /// (<see cref="ReceiverContext.WorkingScale"/>)? Then the landing would truncate the selected value before the receiver's
    /// ROUNDED phrase, so the selection takes the SDIDI instead (kb/Work PB3839, review R1-1). Asked only of a fixed-point
    /// receiver (<see cref="ReceiverContext.RoundsAtItsScale"/>, the same test the landing uses) and an all-exact list; a
    /// float argument has its own lanes.</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the RAW operands' scales.</remarks>
    private bool SelectionPastReceiverHeadroom(BoundIntrinsicCall ic) =>
        ic.Sig.Name is "MAX" or "MIN" && !ic.ComparesStrings && num.Receiver.RoundsAtItsScale && !AnyRealArgument(ic)
        && Operands(ic).Select(x => x.Scale).DefaultIfEmpty(0).Max() is var s && num.Receiver.WorkingScale(s) < s;

    /// <summary>Does any argument arrive on the SDIDI carrier (a native integer power — kb/Work PB69)?</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question; the rendered text is discarded.</remarks>
    private bool AnyDecRaw(BoundIntrinsicCall ic) => Operands(ic).Any(x => x.Dec);

    /// <summary>
    /// The second half of the always-SDIDI set: functions whose standard-mode returned value a RULE fixes
    /// outright, so the argument carrier cannot matter. Unlike <see cref="IntrinsicCarrierRules.CrossAligningFunctions"/> this half has
    /// no structural derivation — each member is here for its own cited reason and is listed one by one.
    /// The NUMVAL family is not listed: <see cref="IntrinsicCarrierRules.ReturnsExactParse"/> (every mode, every
    /// channel) is the third member of the always-SDIDI union <see cref="RenderDec"/> forms.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><c>Sqrt</c> — §2023:15.84.4 RV2 fixes the 34-digit correctly-rounded root; without this the early-out
    ///         returned null for a plain-operand call and SQRT fell to binary64 (kb/Work PB116). The arm's own
    ///         <c>when num.StandardDecimal</c> guard keeps a NATIVE call on the float lane.</item>
    ///   <item><c>Factorial</c> — §2023:15.36.4 RV1c on the SDIDI, where 34! is exact against the native lane's
    ///         documented 33 cap (kb/Work PB125); same <c>when</c> guard, same reason.</item>
    ///   <item><c>Annuity</c> / <c>PresentValue</c> / <c>Variance</c> / <c>StandardDeviation</c> — the four
    ///         INEXACT-EAE functions: their equivalent arithmetic expressions DIVIDE, so even an all-fixed-point
    ///         argument list must evaluate in SDIDI form. This is what removed their COBOL0899 stage.</item>
    /// </list>
    /// </remarks>
    private static readonly FrozenSet<string> StandardValueFixedByRule = new[]
    {
        "SQRT", "FACTORIAL",
        "ANNUITY", "PRESENT-VALUE", "VARIANCE", "STANDARD-DEVIATION",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// ⛔ THE THIRD ROUTE TO THE SDIDI BODY, AND THE ONLY ONE DECIDED FROM THE ARGUMENT'S LIFT (kb/Work PB2965): the
    /// unary exact-family arms whose returned value has an EQUIVALENT ARITHMETIC EXPRESSION over their argument —
    /// ABS (§2023:15.7.4 RV1), SIGN (§2023:15.81.4 RV1), INTEGER-PART (§2023:15.49.4 RV1) and FRACTION-PART (§2023:15.42.4 RV1) — and so,
    /// under a standard arithmetic mode, equal that expression evaluated in the SDIDI (§2023:15.4.1 r1).
    /// </summary>
    /// <remarks>
    /// <para>The expression's operand <c>argument-1</c> is converted into the SDIDI and "if the operand value cannot be
    /// expressed exactly in an SDIDI, the value is rounded according to the rules in 11.9.11" (§2023:8.8.1.5.2 GR1). The
    /// exact Int128 body is digit-identical to that evaluation ONLY while the lift is exact (D3's documented
    /// equivalence), so these arms keep their Int128 body for an argument whose digit bound is within the SDIDI's 34
    /// digits and take <see cref="RenderDec"/> when the lift would round (<see cref="ArgumentRoundsAtLift"/>, which
    /// asks <see cref="NumericRenderer.LiftRounds"/> — the SAME predicate the operand lift acts on): a 16-byte binary
    /// container holds 39 digits, and <c>FUNCTION ABS(S)</c> of S = -2^127 is the rounded
    /// 170141183460469231731687303715884100000, a value its receiver holds, where the Int128 body answered the
    /// size error of an |Int128.MinValue| the SDIDI never forms.</para>
    /// <para>⛔ INTEGER (<c>Floor</c>) IS DELIBERATELY ABSENT. §2023:15.44.4 RV1 gives it no equivalent arithmetic expression
    /// — "the greatest integer less than or equal to the value of argument-1" — so §2023:15.4.1 r1 does not make its
    /// argument an operand of an arithmetic expression, and the value of a data item argument is the item's own
    /// (INTEGER-PART's EAE reaches INTEGER with an argument that is ALREADY an SDIDI, the ABS term). MAX / MIN /
    /// ORD-MAX / ORD-MIN are absent for the same reason (§2023:15.59.4 / §2023:15.63.4 return the CONTENT of an argument).
    /// <c>ExactCarrierBoundaryDriftTests</c> pins both facts.</para>
    /// </remarks>
    internal static readonly FrozenSet<string> EquivalentExpressionUnaryArms = new[]
    {
        "ABS", "SIGN", "INTEGER-PART", "FRACTION-PART",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Does lifting this call's argument into the SDIDI round it (§2023:8.8.1.5.2 GR1) under the statement's
    /// arithmetic mode? Asked only of an <see cref="EquivalentExpressionUnaryArms"/> member under a standard mode.</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question; the rendered text is discarded, so no value semantics apply.</remarks>
    private bool ArgumentRoundsAtLift(BoundIntrinsicCall ic) =>
        num.StandardDecimal && EquivalentExpressionUnaryArms.Contains(ic.Sig.Name)
        && Operands(ic).Any(num.LiftRounds);

    /// <summary>
    /// Under a standard arithmetic mode, evaluate a §2023:15.4.1 r1 function's equivalent arithmetic expression ON
    /// the SDIDI carrier (<c>CobolIntrinsics.*Dec</c>) and return the result as a Dec-valued <see cref="NumX"/>
    /// that lands at the receiver ONCE — the PB56 Dec-carrier body, replacing the interim per-argument
    /// unscaled landing whose fixed working scale truncated sub-microscale operands to zero.
    /// </summary>
    /// <remarks>
    /// <para>Routing, in ONE line: <c>CrossAligningFunctions ∪ StandardValueFixedByRule ∪ ReturnsExactParse ∪ ReturnsSdidiQuotient</c> route here for EVERY
    /// argument shape, and everything else routes here only when a RAW argument is Dec or float — or, for an
    /// <see cref="EquivalentExpressionUnaryArms"/> member, when the argument's lift would ROUND
    /// (<see cref="ArgumentRoundsAtLift"/>, kb/Work PB2965). The remaining NON-aligning exact-EAE functions with an
    /// all-fixed-point list stay on the exact Int128 family, whose every EAE step is
    /// then digit-identical to the SDIDI evaluation (documented equivalence, <c>NUMERIC_DESIGN.md</c> D3;
    /// the &gt;34-digit exact residue keeps MORE precision than per-op rounding would) — that equivalence is what
    /// ALIGNMENT breaks, which is exactly why the aligning arms are unconditional (see
    /// <see cref="IntrinsicCarrierRules.CrossAligningFunctions"/>). Returns null for functions with no Dec body (the prose family
    /// converts its binary64 result in <see cref="RenderFloat"/>).</para>
    /// <para>Result carriers: SIGN / ORD-MAX / ORD-MIN return INTEGERS (scale-0 exact); everything else
    /// returns the SDIDI itself (<c>Dec: true</c>), the same channel the R18 constant arms and MEAN's
    /// existing SDIDI division already prove end-to-end.</para>
    /// </remarks>
    private NumX? RenderDec(BoundIntrinsicCall ic)
    {
        string m = ic.Sig.Name;
        // ⛔ FOUR SETS, ONE DECISION (kb/Work PB252, PB3860): CrossAligningFunctions is STRUCTURAL (the native arm
        // cross-aligns, so alignment can need more digits than the Int128 carrier holds),
        // StandardValueFixedByRule is the cited-exception half, ReturnsExactParse is the NUMVAL family and
        // ReturnsSdidiQuotient is MEAN, whose inexact quotient may not depend on its receiver.
        // Between them they replace the inline `alwaysDec` name chain, and ExactCarrierBoundaryDriftTests derives
        // the first from this switch. The three IntrinsicCarrierRules sets are asked of the FUNCTION (its catalog
        // name, kb/Work PB4370); StandardValueFixedByRule is this renderer's own table and keys on its runtime body.
        // ⛔ THE NUMVAL FAMILY REACHES HERE IN EVERY MODE AND CHANNEL (kb/Work PB251, PB3839): RenderNum consults
        // RenderDec for it through ReturnsExactParse BEFORE the arithmetic-mode dispatch, because §2023:15.67.4 RV1 /
        // §2023:15.68.4 RV1 name the returned value with no mode qualification, §2023:15.4.1's implementor latitude reaches only
        // a function whose definition does NOT otherwise specify it, and NUMVAL-F's §2023:15.69.4 RV2 approximation is
        // resolved to r3's exact value. Their argument is a STRING, so AnyDecOrRealRaw is false and the set is the
        // only thing that keeps this early-out from returning null under native.
        string fn = ic.Sig.Name;
        bool alwaysSdidi = IntrinsicCarrierRules.CrossAligningFunctions.Contains(fn) || StandardValueFixedByRule.Contains(m)
            || IntrinsicCarrierRules.ReturnsExactParse(fn) || IntrinsicCarrierRules.ReturnsSdidiQuotient(fn);
        // ⛔ UNDER ARITHMETIC IS STANDARD EVERY BODY HERE IS THE VALUE (kb/Work PB5575): the exact family's documented equivalence with
        // the SDIDI evaluation is a 34-digit one, and the standard intermediate data item truncates each operation to 32 digits
        // (§2002:8.8.1.3.2), so SUM (10 ** 30, .5, .25) is 10 ** 30 + .7 there and 10 ** 30 + .75 exactly.
        if (!num.StandardIntermediate && !alwaysSdidi && !AnyDecOrRealRaw(ic) && !ArgumentRoundsAtLift(ic) && !SelectionPastReceiverHeadroom(ic))
            return null;
        // An ORD-MAX or ORD-MIN whose arguments are compared as strings has no SDIDI body: its arm is the string one in RenderNum's
        // switch (kb/Work PB2627; the arms below are keyed on the function, so the string form is excluded by its fact, not its key).
        if (ic.ComparesStrings) return null;

        string mode = num.IntermediateMode, arithmetic = num.DecArithmetic;
        return m switch
        {
            // The NUMVAL family: the ONE positional scan's (sign, unscaled, frac[, exp]) lifted to the SDIDI exactly
            // at the parsed scale — no working scale, no receiver. ⛔ NUMVAL and NUMVAL-C reach these arms in EVERY
            // arithmetic mode (kb/Work PB251 — §2023:15.67.4 RV1 / §2023:15.68.4 RV1 fix the value with no mode qualification,
            // and §2023:15.4.1's "unless otherwise specified in the function definition" is what makes that binding
            // under native too), so the digit cap is 31 or 34 by the MODE and is passed explicitly whenever it is
            // not the family's default — exactly as the TEST- twins receive it, so the scan's r1b sub-note-4
            // verdict and the value agree on the same number. NUMVAL-F is the third member (kb/Work PB3839): its
            // §2023:15.69.4 RV2 grants native arithmetic an approximation, and the implementor's determination is r3's exact
            // value, so it cannot depend on the receiver that lands it or on the channel that reads it.
            "NUMVAL" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.NumvalDec), $"{Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            // ReturnsExactParse makes this the path for EVERY NUMVAL-C in every arithmetic mode: RenderNum's
            // first arm reaches it under NATIVE too (PB251) — the
            // LOCALE arm is needed HERE too or --arithmetic standard is silently wrong (indexing the absent
            // ic.Args[1] — no argument-2 is injected under LOCALE; PB64 T6).
            "NUMVAL-C" when ic.LocaleWritten => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.NumvalCLocaleDec),
                $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}{AnycaseFlag(ic)}{DigitCapFlag}")),
            // At COBOL-85 argument-2's content is not screened (kb/Work PB5161, BoundIntrinsicCall.CurrencyContentUnrestricted).
            "NUMVAL-C" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.NumvalCDec),
                $"{Str(ic.Args[0])}, {Str(ic.Args[1])}{CommaFlag}{AnycaseFlag(ic)}{DigitCapFlag}{AnycaseClassification(ic)}"
                + (ic.CurrencyContentUnrestricted ? ", contentUnrestricted: true" : ""))),
            "NUMVAL-F" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.NumvalFDec), $"{mode}, {Str(ic.Args[0])}{CommaFlag}{DigitCapFlag}")),
            // SQRT (§2023:15.84.4 RV2; kb/Work PB116): the one function whose standard-mode value is FIXED — the
            // 34-digit correctly-rounded root; without this arm it fell to RenderFloat's binary64 Math.Sqrt.
            // ⛔ Guarded to STANDARD-DECIMAL: RenderDec is also consulted under NATIVE when a float-typed
            // argument is present (AnyDecOrRealRaw), and an unguarded arm would flip native float SQRT onto
            // the Dec lane — a behavior change r4's approximation licence does not ask for.
            "SQRT" when num.StandardDecimal => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SqrtDec), $"{arithmetic}, {DecArg(ic, 0)}")),
            // FACTORIAL (§2023:15.36.4 RV1c; kb/Work PB125): the SDIDI product loop — FACTORIAL(34) is exact where
            // the native Int128 lane's 33 cap returned the §2023:15.3 default 0 on a conforming argument.
            "FACTORIAL" when num.StandardDecimal =>
                Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.FactorialDec), $"{arithmetic}, {IntArg(ic, 0)}")),
            // COMBINED-DATETIME (§2023:15.17.4 RV1's EAE `argument-1 + (argument-2 / 100000)`; kb/Work PB620): the one
            // §2023:15.4.1 r1 function PB56 left on the landing intake. Argument-2 carries no fraction-digit limit
            // (§2023:15.17.3 AR2 → §2023:15.5.5's magnitude-only "standard numeric time form"), so a landing at
            // max(receiver scale, 6) truncated it and the function stopped equalling its own EAE; argument-1
            // keeps the NARROW intake because §2023:15.5.2 genuinely bounds the integer date form.
            "COMBINED-DATETIME" => Dec(RuntimeApi.DateFn(nameof(CobolDate.CombinedDatetimeDec),
                $"{mode}, {IntArg(ic, 0)}, {DecArg(ic, 1)}{LeapSecondFlag}")),
            "SIGN" => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SignDec), DecArg(ic, 0)), 0),
            "ABS" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.AbsDec), DecArg(ic, 0))),
            "INTEGER" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.FloorDec), DecArg(ic, 0))),
            "INTEGER-PART" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.TruncDec), DecArg(ic, 0))),
            "FRACTION-PART" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.FractionPartDec), $"{arithmetic}, {DecArg(ic, 0)}")),
            // ⛔ UNDER NATIVE ARITHMETIC THE ARGUMENTS ENTER AS THEIR EXACT VALUES (kb/Work PB4111, PB4568; DOC-A.1-123). The arms below
            // are reached under native when an argument is SDIDI-carried (a floating-point literal, a power), an argument
            // expression the exact wide lane could not take (beside an SDIDI or binary64, or past 77 digits), a 39-digit container,
            // or a list whose alignment may leave the carrier. There §2023:8.8.1.3 leaves the method to the implementor and §2023:15.4.1 the
            // value ("an implementor-defined approximation of the value of that expression"), and CLAUDE.md rule 1 sends that
            // latitude to GnuCOBOL, which evaluates from the exact arguments: each body forms the value from CobolExact intakes and
            // rounds it ONCE (CobolIntrinsics.ExactValue.cs). The SDIDI bodies took each wide argument as its 34-digit image and
            // rounded every step: ORD-MAX (P - 1, P, 2.0E0) of a 39-digit P answered 1 although IF P - 1 < P is true, MOD (P, 2 ** 10)
            // answered 0 for 999, and VARIANCE (P, P - 1) 10^10 for 0.25. Under a standard mode the SDIDI evaluation IS the value
            // (§2023:8.8.1.5.2 GR1 rounds each operand, §2023:15.4.1 1) equates the expression), so those keep the Dec bodies.
            "MOD" when !num.StandardDecimal => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.ModExact), $"{mode}, {ExactValueArg(ic, 0)}, {ExactValueArg(ic, 1)}")),
            "REM" when !num.StandardDecimal => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RemExact), $"{mode}, {ExactValueArg(ic, 0)}, {ExactValueArg(ic, 1)}")),
            "ORD-MAX" when !num.StandardDecimal => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMaxExact), ExactValueArgList(ic)), 0),
            "ORD-MIN" when !num.StandardDecimal => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMinExact), ExactValueArgList(ic)), 0),
            // ⛔ A SELECTION RETURNS THE SELECTED ARGUMENT'S CONTENT, WHOLE (kb/Work PB4730): §2023:15.59.4 RV1 "the content of the argument-1
            // having the greatest value", §2023:15.63.4 RV1, §2023:15.61.1. The selected value is an EXACT value (NumX.Exact), not an SDIDI
            // content: a float item's expansion (DP = 2.5E+40 is 25000000000000000155002161260194579873792) keeps all 41 digits to its
            // receiver, where the SDIDI's 34 stored 00000 for 73792.
            "MAX" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.MaxExact), ExactValueArgList(ic), null),
            "MIN" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.MinExact), ExactValueArgList(ic), null),
            "SUM" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.SumExact), $"{mode}, {ExactValueArgList(ic)}", null),
            "RANGE" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.RangeExact), $"{mode}, {ExactValueArgList(ic)}", null),
            "MEAN" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.MeanExact), $"{mode}, {ExactValueArgList(ic)}", null),
            // ⛔ A MEDIAN DECIDES ITS PARITY, ITS MIDDLE PAIR AND THEIR KINDS AT RUN TIME (kb/Work PB4827, PB4859, PB4891). An odd count selects
            // (§2023:15.61.4 RV1, the exact lane keeps the selected argument's own kind); an even count is the mean of the two middle values
            // (§2023:15.61.4 RV2), a new value that is the binary64 mean when a MIDDLE value is binary64 (DOC-A.1-92b), as the float lane forms it for
            // a count the binder knows (IntrinsicCarrierRules.ReturnsMeanOfMiddlePair), and the exact mean when both are fixed-point. Which values
            // are the middle pair is a run-time fact, and so is the kind of a nested selection's returned content (MAX (F M2) returns F's or
            // M2's), so the body reads each middle's binary64-ness off its exact value (CobolExact.TryBinary64), never off the written operands.
            "MEDIAN" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.MedianExact), $"{mode}, {ExactValueArgList(ic)}", null),
            "MIDRANGE" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.MidrangeExact), $"{mode}, {ExactValueArgList(ic)}", null),
            // ⛔ VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE AND ANNUITY DECIDE THEIR KIND AT RUN TIME TOO (kb/Work PB4979; DOC-A.1-92b): an argument
            // that is a nested selection may have returned a float item's content, so the body reads the arguments' binary64-ness off their
            // exact values (CobolExact.TryBinary64) and returns the binary64 of `VARIANCE (F M)` as the SUM family does (PB4923), else the
            // exact value rounded once. The value is a run-time-scaled exact one that its consumer reads as the SDIDI image it lowers to.
            "VARIANCE" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.VarianceExact), $"{mode}, {ExactValueArgList(ic)}", null),
            "STANDARD-DEVIATION" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.StdDevExact), $"{mode}, {ExactValueArgList(ic)}", null),
            "PRESENT-VALUE" when !num.StandardDecimal =>
                Exact(LeadThenTail(ic, nameof(CobolIntrinsics.PresentValueExact), $"{mode}, ", RuntimeApi.ExactType, a => ExactValueOf(ic, a))),
            "ANNUITY" when !num.StandardDecimal => Exact(nameof(CobolIntrinsics.AnnuityExact), $"{mode}, {ExactValueArg(ic, 0)}, {DecArg(ic, 1)}", null),
            "MOD" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.ModDec), $"{arithmetic}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "REM" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RemDec), $"{arithmetic}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "MAX" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MaxDec), DecArgList(ic))),
            "MIN" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MinDec), DecArgList(ic))),
            "ORD-MAX" => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMaxDec), DecArgList(ic)), 0),
            "ORD-MIN" => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMinDec), DecArgList(ic)), 0),
            "SUM" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SumDec), $"{arithmetic}, {DecArgList(ic)}")),
            "RANGE" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.RangeDec), $"{arithmetic}, {DecArgList(ic)}")),
            "MEAN" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MeanDec), $"{arithmetic}, {DecArgList(ic)}")),
            "MEDIAN" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MedianDec), $"{arithmetic}, {DecArgList(ic)}")),
            "MIDRANGE" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MidrangeDec), $"{arithmetic}, {DecArgList(ic)}")),
            "VARIANCE" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.VarianceDec), $"{arithmetic}, {DecArgList(ic)}")),
            "STANDARD-DEVIATION" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.StdDevDec), $"{arithmetic}, {DecArgList(ic)}")),
            // ANNUITY (kb/Work PB637): §2023:15.9.3 AR3's "positive integer" is a SIGN rule and says nothing about magnitude,
            // so argument-2 is a TOTAL argument and rides the SDIDI as the position intake left it (RawArg: the operand's
            // integer on the SDIDI, unbounded — an SDIDI integer reaches past Int128), never on a native integer
            // carrier. At a positive rate an enormous n is the equivalent expression's own size condition, never
            // EC-ARGUMENT-FUNCTION (see CobolIntrinsics.AnnuityDec). IntrinsicCarrierAgreementDriftTests
            // .IntegerArgumentsThatRideTheSdidi_TakeNoNativeIntake pins this arm.
            "ANNUITY" => Dec(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.AnnuityDec), $"{arithmetic}, {DecArg(ic, 0)}, {DecArg(ic, 1)}")),
            "PRESENT-VALUE" => Dec(LeadThenTail(ic, nameof(CobolIntrinsics.PresentValueDec), $"{arithmetic}, ", "CobolDec", DecOf)),
            _ => null,
        };

        static NumX Dec(string expr) => new(expr, 0, Dec: true);
    }

    /// <summary>
    /// ⛔ THE UNARY EXACT FAMILY ON THE RUN-TIME-SCALED EXACT VALUE (kb/Work PB4575 review finding R1-2): ABS, SIGN, INTEGER,
    /// INTEGER-PART and FRACTION-PART of an argument expression in which a wide value met an SDIDI operand (<c>NumX.Exact</c>) return
    /// their value on the same carrier (<c>CobolIntrinsics.AbsExact</c> and its siblings), which <c>NumericRenderer</c> settles at its
    /// public entry like a nested product; SIGN's −1/0/+1 is an integer. Their value is the argument's own digits, and the expression
    /// around the call keeps its other operands exact (<c>NumericRenderer.CombineCore</c>), so a body over the argument's 34-digit
    /// image made <c>FUNCTION INTEGER (P + 0.0E0) - P</c> of a 39-digit P the image's residue (99001 for 0). Every other function
    /// with such an argument returns null here: a type-6 position took its integer already (<see cref="RawWideArg"/>), and the
    /// selection, MOD/REM and statistics bodies take it exact on the SDIDI lane (<see cref="RenderDec"/>'s native arms over
    /// <see cref="ExactValueArg"/>). <c>ExactWideSettlementDriftTests.EveryWideBody_TakesTheExactValueToo</c> holds that closed.
    /// </summary>
    private NumX? RenderExactUnary(string function, NumX x) => function switch
    {
        "ABS" => Exact(nameof(CobolIntrinsics.AbsExact), x.Expr, x.Precision),
        "SIGN" => new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SignExact), x.Expr), 0),
        "INTEGER" => Exact(nameof(CobolIntrinsics.FloorExact), x.Expr, 0),
        "INTEGER-PART" => Exact(nameof(CobolIntrinsics.TruncateExact), x.Expr, 0),
        "FRACTION-PART" => Exact(nameof(CobolIntrinsics.FractionPartExact), $"{num.IntermediateMode}, {x.Expr}", null),
        _ => null,
    };

    private static NumX Exact(string body, string args, int? precision) =>
        new(RuntimeApi.Intrinsic(body, args), 0, Exact: true, Precision: precision);

    /// <summary>A run-time-scaled exact value whose call text the arm built itself (PRESENT-VALUE's lead-then-tail enumeration).</summary>
    private static NumX Exact(string call) => new(call, 0, Exact: true);

    /// <summary>
    /// ⛔ THE EXACT FAMILY ON THE EXACT WIDE INTERMEDIATE (kb/Work PB3953; docs/CONFORMANCE.md DOC-A.1-123): under native arithmetic,
    /// a call with an argument expression past the <see cref="Int128"/> carrier (<c>NumX.Wide</c>, kept by <see cref="RawWideArg"/>)
    /// is evaluated on <c>CobolWide</c> (<c>CobolIntrinsics.Wide.cs</c>) and returns a <c>NumX.Wide</c> where its value can be
    /// as wide as an argument, which <c>NumericRenderer</c> settles at its public entry like a nested product: rounded once into a
    /// fixed-point resultant, compared exactly by a relation, kept by a function above it, and lowered to the SDIDI otherwise.
    /// </summary>
    /// <remarks>
    /// <para>WHY. §2023:8.8.1.3 makes the method of evaluating "all integer and numeric functions" under native arithmetic the
    /// implementor's, and §2023:15.4.1 the returned value's characteristics; for that latitude CLAUDE.md rule 1 makes GnuCOBOL the
    /// answer, and its argument temporary is arbitrary-precision. The argument used to reach the function as its 34-digit
    /// round-to-odd image: <c>ABS (BIG * BIG * K3)</c> showed <c>…100000</c> for <c>…000999</c>, MOD of it by 1000 was 0 for 999.</para>
    /// <para>WHEN. Some argument is wide and none is an SDIDI, a binary64 or a run-time-scaled exact value (<c>NumX.Exact</c>, an
    /// expression in which a wide value met an SDIDI operand, kb/Work PB4575, whose scale no aligned body can know; a call with one
    /// returns its value on the SDIDI, and
    /// takes the wide argument there EXACTLY: <see cref="RenderDec"/>'s native arms over <see cref="ExactValueArgList"/>, kb/Work
    /// PB4568, so ORD-MAX (P - 1, P, 2.0E0) is 2 and MOD (P, 2 ** 10) 999), and the arguments' digit bounds prove the WORK fits the wide
    /// form's 77 digits: each argument aligned to the common scale (its digits plus the scale gap), plus what the function adds to
    /// it (a digit per tenfold argument count for SUM, nine more for a table(ALL) argument, one for RANGE's difference and
    /// INTEGER's floor, two for MEDIAN's and MIDRANGE's exact halving one place further). A call past that bound returns null and
    /// keeps the SDIDI arm, which under native arithmetic still forms the value from the exact arguments wherever the run-time
    /// values fit the wide form, and from the SDIDI images past it, the determination for an intermediate past 77 digits
    /// (DOC-A.1-123).</para>
    /// <para>WHAT. Every exact-family arm of <see cref="RenderNum"/>'s switch that takes a numeric argument has a body here
    /// (<c>ExactWideSettlementDriftTests</c> holds that closed): ABS, SIGN, INTEGER, INTEGER-PART and FRACTION-PART at the argument's
    /// own scale; MOD and REM over their type-6 integers; MAX, MIN, ORD-MAX, ORD-MIN, SUM, RANGE, MEDIAN and MIDRANGE over the
    /// arguments aligned to the common scale, exactly as the <c>…Scaled</c> bodies take them. A selection needs no
    /// receiver-headroom arm: its value reaches the receiver whole, rounded once by the settlement.</para>
    /// <para>INTAKE(ALIGNED) — each argument through <see cref="RawWideArg"/> (a table(ALL) element directly: an element is a data
    /// item, never wide), aligned UP to the common scale on the wide form, which is exact: nothing is truncated or rounded.</para>
    /// </remarks>
    private NumX? RenderWide(BoundIntrinsicCall ic)
    {
        // Only a computed operand (an arithmetic expression, a nested call) can be wide; a call over data items and literals is
        // answered here without rendering its arguments a further time.
        if (num.StandardDecimal || !ic.Args.Any(a => a is BoundComputedOperand)) return null;
        var xs = Enumerable.Range(0, ic.Args.Count).Select(i => RawWideArg(ic, i)).ToList();
        if (xs.Count == 1 && xs[0].Exact) return RenderExactUnary(ic.Sig.Name, xs[0]);
        if (!xs.Any(x => x.Wide) || xs.Any(x => x.Dec || x.Real || x.Exact)) return null;

        int s = xs.Max(x => x.Scale);
        int widest = xs.Max(x => NumericRenderer.DigitBound(x) + (s - x.Scale));
        int one = xs[0].Scale;
        string Aligned(int i) => NumericRenderer.WideAligned(xs[i], s);
        // A written argument is the one RawWideArg kept; an element of a table(ALL) argument is a data item, never wide.
        string Element(BoundOperand a)
        {
            for (int i = 0; i < ic.Args.Count; i++)
                if (ReferenceEquals(ic.Args[i], a)) return Aligned(i);
            return NumericRenderer.WideAligned(num.AsNum(a, NumericArgument), s);
        }
        string List() => ArgArray(ic, 0, "CobolWide", Element) ?? string.Join(", ", xs.Select((_, i) => Aligned(i)));
        // A table(ALL) argument stands for an unknown number of elements, so SUM charges it nine more digits (as
        // IntrinsicCarrierRules.AlignmentMayLeaveCarrier does), and every argument counts toward the digits of the count.
        int sumDigits = xs.Select((x, i) => NumericRenderer.DigitBound(x) + (s - x.Scale)
                                            + (ic.Args[i] is BoundFieldOperand { Place: TableAllPlace } ? 9 : 0)).Max()
                        + xs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;

        (NumX Value, int Work)? call = ic.ComparesStrings ? null : ic.Sig.Name switch
        {
            "ABS" => (Wide(nameof(CobolIntrinsics.AbsWide), Aligned(0), one, widest), widest),
            "SIGN" => (new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SignWide), Aligned(0)), 0), widest),
            // A negative scale (trailing P positions) is an integer already: its zeros are multiplied in.
            "INTEGER" => (Wide(nameof(CobolIntrinsics.FloorWide), $"{Aligned(0)}, {one}", 0, widest + Math.Max(0, -one) + 1), widest + Math.Max(0, -one) + 1),
            "INTEGER-PART" => (Wide(nameof(CobolIntrinsics.TruncateWide), $"{Aligned(0)}, {one}", 0, widest + Math.Max(0, -one)), widest + Math.Max(0, -one)),
            "FRACTION-PART" => (Wide(nameof(CobolIntrinsics.FractionPartWide), $"{Aligned(0)}, {one}", one, widest), widest),
            "MOD" => (Wide(nameof(CobolIntrinsics.ModWide), $"{Aligned(0)}, {Aligned(1)}", s, widest), widest),
            "REM" => (Wide(nameof(CobolIntrinsics.RemWide), $"{Aligned(0)}, {Aligned(1)}", s, widest), widest),
            "MAX" => (Wide(nameof(CobolIntrinsics.MaxWide), List(), s, widest), widest),
            "MIN" => (Wide(nameof(CobolIntrinsics.MinWide), List(), s, widest), widest),
            "ORD-MAX" => (new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMaxWide), List()), 0), widest),
            "ORD-MIN" => (new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.OrdMinWide), List()), 0), widest),
            "SUM" => (Wide(nameof(CobolIntrinsics.SumWide), List(), s, sumDigits), sumDigits),
            "RANGE" => (Wide(nameof(CobolIntrinsics.RangeWide), List(), s, widest + 1), widest + 1),
            // MEAN (§2023:15.60.4 RV1) is the one arm whose value is not wide: the exact sum divided ONCE on the SDIDI (kb/Work PB4075,
            // PB3860), the value every other lane forms, returned as the SDIDI the receiver then rounds once.
            "MEAN" => (new NumX(RuntimeApi.Intrinsic(nameof(CobolIntrinsics.MeanWide), $"{num.IntermediateMode}, {s}, {List()}"), 0, Dec: true), sumDigits),
            "MEDIAN" => (Wide(nameof(CobolIntrinsics.MedianWide), List(), s + 1, widest + 2), widest + 2),
            "MIDRANGE" => (Wide(nameof(CobolIntrinsics.MidrangeWide), List(), s + 1, widest + 2), widest + 2),
            _ => null,
        };
        return call is { } c && c.Work <= RuntimeApi.WideMaxDigits ? c.Value : null;

        static NumX Wide(string body, string args, int scale, int digits) =>
            new(RuntimeApi.Intrinsic(body, args), scale, Digits: digits, Wide: true);
    }

    /// <summary>Does any argument render as FLOATING (binary64) rather than as a scaled integer? (PB2.)</summary>
    /// <remarks><para>Asked of the ARGUMENTS, not the receiver: a float argument into a fixed-point receiver still has
    /// to be computed in binary64 and only then quantized, which is exactly what <see cref="RenderFloat"/>'s
    /// <c>FromDouble</c> tail does.</para>
    /// <para>⛔ ASKED OF THE <b>LANDED</b> OPERAND, NOT THE RAW ONE (fix-queue PB38). Under a standard
    /// arithmetic mode <see cref="Landed"/> converts a float in per §2023:8.8.1.5.1, so it is no longer floating and
    /// this dispatch must not route the call to the binary64 body. Reading the RAW operand here would reinstate
    /// the exact defect the landing exists to remove, with the landing silently doing nothing — the two must ask
    /// the same question of the same value.</para>
    /// <para>INTAKE(PREDICATE) — renders only to ASK a question — of the LANDED operand, deliberately (see the remark above).</para></remarks>
    private bool AnyRealArgument(BoundIntrinsicCall ic) =>
        Operands(ic).Any(x => Landed(x).Real);

    /// <summary>Does EVERY argument render as floating? (An all-float selection list stays in the float lane —
    /// each argument's content is its double; a mixed list rides the SDIDI, kb/Work PB65 RV-15.59.4-1 D2.)</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the LANDED operand, as its sibling does.</remarks>
    private bool AllRealArguments(BoundIntrinsicCall ic) =>
        Operands(ic).All(x => Landed(x).Real);

    /// <summary>Is any binary64 argument a float ITEM's content (<c>Real</c> and not <see cref="NumX.Approximate"/>), the fact
    /// <see cref="IntrinsicCarrierRules.Binary64ResultIsApproximation"/> keys the kind of a function's binary64 value on
    /// (kb/Work PB4622)? The bind-time prediction reads the same fact off the bound tree (<c>FunctionValueLane.Of</c>).</summary>
    /// <remarks>INTAKE(PREDICATE) — renders only to ASK a question — of the LANDED operand, as its siblings do.</remarks>
    private bool AnyFloatItemArgument(BoundIntrinsicCall ic) =>
        Operands(ic).Any(x => Landed(x) is { Real: true, Approximate: false });

    /// <summary>Is any binary64 argument an engine's APPROXIMATION (<see cref="NumX.Approximate"/>), the other kind
    /// <see cref="IntrinsicCarrierRules.SelectionNeedsExactLane"/> tells from a float item's content (kb/Work PB4622)?</summary>
    /// <remarks>INTAKE(PREDICATE) - renders only to ASK a question - of the LANDED operand, as its siblings do.</remarks>
    private bool AnyApproximateArgument(BoundIntrinsicCall ic) =>
        Operands(ic).Any(x => Landed(x) is { Real: true, Approximate: true });

    /// <summary>A numeric argument as a C# double (the float family's §2023:15.4.1 carrier). ⛔ Converts the RAW
    /// operand (fix-queue PB56): routing through the <see cref="Landed"/> unscaled truncation first turned a
    /// sub-working-scale Dec operand to ZERO before the double conversion — SQRT(4e-18) probed as 0 where the
    /// approximation of 2e-9 is required — and <see cref="NumericRenderer.Real"/> carries its own exact arm
    /// for every carrier (a Dec operand converts via <c>ToDouble</c>, a float passes through).</summary>
    /// <remarks>INTAKE(APPROXIMATED) — binary64 from the RAW carrier — the indexed form of <see cref="DblOf"/>.</remarks>
    private string Dbl(BoundIntrinsicCall ic, int i) => NumericRenderer.Real(RawArg(ic, i));

    /// <summary>An integer-kind argument as a C# <c>long</c> (truncated to scale 0 when the operand carries a
    /// fraction — integer arguments "shall be integers", §2023:15.3; a fractional value is the program's EC latitude).</summary>
    /// <remarks>INTAKE(INTEGRAL) — rescaled to scale 0 with TRUNCATION — the §2023:15.3 integer positions, where the argument is required to be an integer already.
    /// ⛔ FOR A <b>PARTIAL</b> FUNCTION ONLY — see <see cref="AsInt"/>, and use
    /// <see cref="IntArgWide"/> when the argument is total.</remarks>
    private string IntArg(BoundIntrinsicCall ic, int i) => AsInt(RawArg(ic, i));   // an integer lands at scale 0 directly — never through a working scale that eats headroom (kb/Work PB69)

    /// <summary>The WIDE twin of <see cref="IntArg"/> on the NUMERIC channel — the intake for a §15 integer
    /// argument that is <b>TOTAL</b>. <see cref="AsIntWide"/> states the rule and names the drift test that
    /// holds the renderer's intake to the runtime body's declared carrier.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the §2023:15.3 type-6 argument class, on the Int128 carrier — the same scale-0 truncation <see cref="IntArg"/> applies, on a carrier wide enough for a TOTAL argument.</remarks>
    private string IntArgWide(BoundIntrinsicCall ic, int i) => AsIntWide(RawArg(ic, i));

    /// <summary>
    /// ⛔ THE ONE NARROWING, AND IT RAISES RATHER THAN WRAPS (fix-queue PB22). This emitted a bare
    /// <c>(long)(…)</c> over an <c>Int128</c>-typed expression, and <c>RoslynBackend</c> sets no
    /// <c>checkOverflow</c> — so the cast wrapped MODULO 2⁶⁴, silently, and BEFORE the function's own range
    /// guard could see the value. <c>FUNCTION INTEGER-OF-DAY(P * 100 + 62)</c> with
    /// <c>P PIC 9(18) VALUE 184467440737115466</c> is 2⁶⁴ + 1995046, so §2023:15.5.2's correct 1601..9999 / 1..366
    /// check received 1995046 and returned a plausible 143951 — from an argument nineteen orders of magnitude
    /// away, with no EC-ARGUMENT-FUNCTION even under enabled checking.
    /// <para>Both entries (<see cref="IntArg"/> for the numeric channel, <see cref="ArgInt"/> for the string one)
    /// funnel here, so ONE change covers seven renderer arms over eleven functions — which is the point: a value
    /// the receiving body cannot represent is an incorrect argument (§2023:15.3), and the place to say so is where
    /// the narrowing happens, not eleven times downstream.</para>
    /// <para>⛔ IT IS THE LANDING FOR A <b>PARTIAL</b> FUNCTION AND ONLY FOR ONE (kb/Work PB254). PB22 swept
    /// EVERY integer arm into it, including arms whose function is TOTAL — §2023:15.90/§2023:15.91's date validators and
    /// FIND-STRING's argument-3 — and for those the raise is manufactured: their argument rules constrain
    /// nothing but integer-ness, so §2023:15.3 has no "incorrect value … according to the rules specified in the
    /// function definition" to key on. A total argument takes <see cref="AsIntWide"/>, which states the rule
    /// and names the drift test that keeps the two in step.</para>
    /// <para>The <c>Real</c> arm takes the double-typed twin by NAME rather than by overload: an integer literal
    /// converts implicitly to both <c>Int128</c> and <c>double</c>, and an overload pair would turn
    /// <c>FUNCTION FACTORIAL(5)</c> into a CS0121 ambiguity — the collision that broke six corpus programs when
    /// the <c>…Real</c> bodies were first written.</para></summary>
    private static string AsInt(NumX a) => IntegerIntake(a, wide: false);

    /// <summary>
    /// ⛔ THE ONE §2023:15.3 TYPE-6 INTAKE, TOTAL OVER EVERY CARRIER A <see cref="NumX"/> CAN BE, and the bounded
    /// (<see cref="AsInt"/>) and total (<see cref="AsIntWide"/>) arms are this method's two widths — they were two
    /// hand-copied four-arm dispatches, and the copies had drifted: the wide one had an unsigned-wide arm the
    /// narrow one lacked (<c>USAGE COMP-5</c> over a 19+-digit unsigned PICTURE at INTEGER-OF-DATE / CHAR /
    /// BASE-CONVERT reached Roslyn as a CS1503 — kb/Work PB638) and ignored the scale of such an operand; neither
    /// asked the integrality question (kb/Work PB1526), which <see cref="RuntimeApi.IntegerArgOf"/> now asks on
    /// the exact value on every carrier.
    /// <list type="bullet">
    ///   <item>the unsigned-wide lane funnels through <see cref="NumericRenderer.DeU"/> FIRST — the R10 Widen
    ///         funnel, loud past the Int128 intermediate — and then rides the ordinary scaled arm, scale included;</item>
    ///   <item>the Dec arm is BEFORE the scale test (kb/Work R24 — ledger F44): a Dec NumX carries Scale 0 BY
    ///         CONVENTION (the PB14/PB32 lesson), so the scale test would pass the CobolDec expression through raw
    ///         and hand Roslyn the CS1503 this arm exists to close; the float arm is the binary64 carrier.</item>
    /// </list>
    /// <c>IntrinsicCarrierAgreementDriftTests.EveryIntegerIntake_IsTotalOverTheNumXCarriers</c> derives the carrier
    /// list from <see cref="NumX"/>'s own flags and proves both widths accept every one.</summary>
    internal static string IntegerIntake(NumX a, bool wide, RuntimeApi.IntegerArgumentSite? site = null)
    {
        var (carrier, value) = NumericRenderer.IntakeCarrier(a);
        // An undecided argument (kb/Work PB5889) is checked on its own carrier first, naming its site; a scale-0 fixed-point one is an
        // integer already and takes no check.
        string expr = site is { } s && !(carrier is RuntimeApi.IntegerArgCarrier.Scaled && value.Scale == 0)
            ? RuntimeApi.CheckedIntegerArgument(carrier, value.Expr, value.Scale, s)
            : value.Expr;
        return RuntimeApi.IntegerArgOf(carrier, expr, value.Scale, wide);
    }

    /// <summary>The variadic arguments aligned to their common scale (ISO §2023:8.8.1 — alignment makes unscaled
    /// comparison/arithmetic equal value comparison/arithmetic), as a C# argument list + that scale.</summary>
    /// <remarks>INTAKE(ALIGNED) — every argument rescaled UP to the argument list's common maximum scale on the <c>Int128</c> carrier — no digit is lost, but the list's headroom is.</remarks>
    private (string ArgList, int Scale) AlignedArgs(BoundIntrinsicCall ic)
    {
        // A table(ALL) operand renders as its ELEMENT here (the index variable inside) — right for the scale;
        // the list itself enumerates through ArgArray (kb/Work PB62).
        var xs = Operands(ic).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        string list = ArgArray(ic, 0, "Int128", a => NumericRenderer.Align(num.AsNum(a, NumericArgument), s))
                      ?? string.Join(", ", xs.Select(x => NumericRenderer.Align(x, s)));
        return (list, s);
    }

    /// <summary>The variadic arguments UNALIGNED — parallel value/scale arrays for the selection family
    /// (fix-queue PB65). Each argument renders at its OWN scale (<c>Align(x, x.Scale)</c> is the identity for
    /// an exact operand and the standard carrier conversion for Real/Dec/U), so no widening ever happens on
    /// intake; the common scale is reported for the result's compile-time <see cref="NumX"/>.</summary>
    /// <remarks>INTAKE(EXACT) — each argument at its OWN scale with that scale carried beside it — the selection family's intake, where no common alignment may be imposed (kb/Work PB65).</remarks>
    private (string Vals, string Scales, int Scale) RawArgPairs(BoundIntrinsicCall ic)
    {
        var xs = Operands(ic).ToList();
        int s = xs.Count == 0 ? 0 : xs.Max(x => x.Scale);
        // A table(ALL) argument enumerates (kb/Work PB62): its elements share one scale, so the scales array
        // enumerates the same ranges with a constant selector. A written list is a collection expression the runtime
        // takes as a ReadOnlySpan, so the values ride the stack and the constant scales static data: no array per
        // evaluation (kb/Work PB2636).
        string vals = ArgArray(ic, 0, "Int128", a => { var x = num.AsNum(a, NumericArgument); return NumericRenderer.Align(x, x.Scale); })
                      ?? $"[{string.Join(", ", xs.Select(x => NumericRenderer.Align(x, x.Scale)))}]";
        string scales = ArgArray(ic, 0, "int", a => num.AsNum(a, NumericArgument).Scale.ToString())
                        ?? $"[{string.Join(", ", xs.Select(x => x.Scale))}]";
        return (vals, scales, s);
    }

    /// <summary>The variadic string-argument list. <paramref name="admitNumeric"/> defaults FALSE and is passed
    /// true by exactly one caller (CONCAT — §2023:15.18.3 AR1 lists class numeric): four arms with three different
    /// §15.x.3 argument rules share this helper, so the admission is a parameter, never a global flip
    /// (§2023:15.26.3 AR1 / §2023:15.66.3 AR1 exclude class numeric outright — PB59).</summary>
    private string StrArgList(BoundIntrinsicCall ic, bool admitNumeric = false) =>
        ArgArray(ic, 0, "string", a => admitNumeric ? StrNum(a) : Str(a))   // a table(ALL) enumerates (kb/Work PB62)
        ?? string.Join(", ", ic.Args.Select(a => admitNumeric ? StrNum(a) : Str(a)));

    private string CommaFlag => ctx.Data.Configuration.DecimalPointIsComma ? ", commaMode: true" : "";

    /// <summary>The ANYCASE named argument (§2023:15.68.3 AR4f / §2023:15.37.4 RV4 — the ONE <see cref="BoundIntrinsicCall.Anycase"/>
    /// flag), rendered only when present so the default-free call stays byte-stable.</summary>
    private static string AnycaseFlag(BoundIntrinsicCall ic) => ic.Anycase ? ", anycase: true" : "";

    /// <summary>⛔ THE CLASSIFICATION AN ANYCASE FOLD READS (§2023:15.57.4 RV3; kb/Work PB2631). ANYCASE is DEFINED as "the rules
    /// for the LOWER-CASE function without the LOCALE argument" (§2023:15.37.4 RV4, §2023:15.87.4 RV5, §2023:15.68.3 AR4f), and that
    /// function maps case through the module's CHARACTER CLASSIFICATION when it has one - so the matchers receive the
    /// same selector the UPPER-CASE / LOWER-CASE arms do (<see cref="ObjectComputerEmit.ClassificationExpr"/>, keyed on
    /// argument-1's class), only when the module HAS a classification and an ANYCASE comparison is in play, which keeps
    /// every other emission byte-stable.</summary>
    private string AnycaseClassification(BoundIntrinsicCall ic) =>
        ctx.Data.Configuration.Classification is not null
        && (ic.Anycase || (ic.SubstituteModes?.Any(m => (m & 4) != 0) ?? false))
            ? $", classification: {ObjectComputerEmit.ClassificationExpr(ic.Args[0])}" : "";

    /// <summary>The §2023:15.93.4/§2023:15.94.4 RV1b digit-cap named argument, DERIVED FROM THE MODE
    /// (<see cref="ArithmeticModes.NumvalDigitCap"/> — the one table, keyed on the mode the standard keys its
    /// three sub-notes on). Omitted when the cap is the runtime's own default, so the generated call stays
    /// byte-stable for every native compilation.
    ///
    /// <para>This was a two-state ternary on <c>num.StandardDecimal</c>, which gave STANDARD-BINARY the NATIVE
    /// cap through its else-branch. Unreachable today — the mode is declined at bind — but a lane that answers
    /// a question it was never asked is the half of a double defence that rots first.</para></summary>
    private string DigitCapFlag =>
        ArithmeticModes.NumvalDigitCap(ctx.CurrentOptions.Arithmetic) is var cap && cap != ArithmeticModes.DefaultDigitCap
            ? $", digitCap: {cap}" : "";

    /// <summary>The landing form past the Int128 carrier for a quantizer / exact-family parse (kb/Work PB77) — the
    /// ONE <c>NumericRenderer.CheckedFlag</c>, read from the receiver context this render runs under.</summary>
    private string CheckedFlag => num.CheckedFlag;

    /// <summary>The trailing weights argument for a PCS-flagged CHAR/ORD/CHAR-NATIONAL (hazard H5: the binder
    /// set <see cref="BoundIntrinsicCall.Collate"/>/<see cref="BoundIntrinsicCall.CollateNat"/> ONLY when the
    /// matching non-identity PCS exists — exactly when the program class emitted its <c>__COLLATE</c> /
    /// <c>__COLLATE_NAT</c> table; the two never coexist on one call — CHAR/alphanumeric-ORD read the
    /// alphanumeric sequence, CHAR-NATIONAL/national-ORD the national one, §2023:15.15.4/§2023:15.16.4/§2023:15.70.4).</summary>
    private static string Collate(BoundIntrinsicCall ic) =>
        ic.Collate ? $", {ObjectComputerEmit.ProgramCollation}" : ic.CollateNat ? $", {ObjectComputerEmit.NationalProgramCollation}" : "";

    /// <summary>The LEADING weights argument for a PCS-flagged MAX/MIN/ORD-MAX/ORD-MIN string form — collate goes
    /// FIRST (a <c>params</c> list can take no trailing param), selecting the <c>MaxString(ushort[]|NationalCollation,
    /// params ReadOnlySpan&lt;string&gt;)</c> overload. The mirror of <see cref="Collate"/>'s trailing form for the single-arg CHAR/ORD.</summary>
    private static string CollatePrefix(BoundIntrinsicCall ic) =>
        // The MAX/MIN family takes the program's CobolCollation carrier, exactly as CHAR/ORD do (PB101 — the
        // ushort[] raw-table overload is gone; the carrier's Compare is the order-equivalent tail, PB59).
        ic.Collate ? $"{ObjectComputerEmit.ProgramCollation}, " : ic.CollateNat ? $"{ObjectComputerEmit.NationalProgramCollation}, " : "";

    // ── The STRING channel (instance — reached through OperandText.AsString with the per-unit renderer) ─────

    /// <summary>
    /// Reference-modify the function RESULT when the reference carried one (ISO §2023:8.4.3.3.3 SR2, fix-queue PB8),
    /// through the SAME <c>CobolString.RefMod</c> — and therefore the same §2023:8.4.3.3.4 item-5c bounds check and
    /// EC-BOUND-REF-MOD raise — that a reference-modified data item uses. No second slicer exists.
    /// <para>⛔ THIS IS THE ONLY EMIT SITE THAT HAS TO HONOUR IT, and that is provable rather than surveyed: SR2
    /// admits a ref-mod only on an alphanumeric, boolean or national function, and every such result renders
    /// through this one method (reached from <c>OperandText.AsString</c> and from the nested-argument visitor).
    /// A numeric-result call cannot carry one — the binder rejects it with COBOL1629 — so the numeric and
    /// folded channels need no arm. Wrapping HERE rather than at each caller is what keeps that true.</para>
    /// </summary>
    public string RenderString(BoundIntrinsicCall ic)
    {
        string value = RenderStringValue(ic);
        return ic.RefMod is { } rm ? RuntimeApi.StrRefMod(value, rm) : value;
    }

    /// <summary>Render an alphanumeric-result intrinsic as a C# string expression. An INSTANCE channel (P7
    /// Step 12 — the context-free static twin is deleted): numeric-kind arguments render through the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> (a string-channel call has no
    /// numeric receiver), so division, float items, nested numeric intrinsics, and numeric-edited de-edits
    /// all render where the static channel stayed loud (H3 closed).</summary>
    private string RenderStringValue(BoundIntrinsicCall ic)
    {
        var sig = ic.Sig;
        if (sig.Bind == IntrinsicBind.Deferred)
            return EmitText.LoudValue("string", $"FUNCTION {sig.Name} (catalogued, not yet implemented)");
        return sig.Name switch
        {
            // UPPER-CASE / LOWER-CASE (§2023:15.97 / §2023:15.57; kb/Work PB64 T5): with a LOCALE phrase, the named locale's LC_CTYPE
            // (r2); else, when this module has a CHARACTER CLASSIFICATION, the classification's LC_CTYPE for the
            // operand's class (r3 — __CLASSIFY, resolved at activation); else the implementor's correspondence (r4 —
            // the invariant map the function always used).
            "UPPER-CASE" or "LOWER-CASE" when ic.Locale.Tag is { } caseTag =>
                RuntimeApi.LocaleFn(sig.Name is "UPPER-CASE" ? nameof(CobolLocale.UpperCase) : nameof(CobolLocale.LowerCase),
                    $"{Str(ic.Args[0])}, {EmitText.CsLiteral(caseTag)}"),
            "UPPER-CASE" or "LOWER-CASE" when ctx.Data.Configuration.Classification is not null =>
                RuntimeApi.LocaleFn(sig.Name is "UPPER-CASE" ? nameof(CobolLocale.UpperCase) : nameof(CobolLocale.LowerCase),
                    $"{Str(ic.Args[0])}{ObjectComputerEmit.ClassificationArg(ic.Args[0])}"),
            "UPPER-CASE" or "LOWER-CASE" or "REVERSE" =>                       // §2023:15.97/57/78 — length-preserving
                RuntimeApi.Intrinsic(sig.Name switch
                {
                    "UPPER-CASE" => nameof(CobolIntrinsics.UpperCase),
                    "LOWER-CASE" => nameof(CobolIntrinsics.LowerCase),
                    _ => nameof(CobolIntrinsics.Reverse),
                }, Str(ic.Args[0])),
            "CHAR" =>                                                          // §2023:15.15 — PCS-relative (H5 conditional weights)
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Char), $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CHAR-NATIONAL" =>                                                 // §2023:15.16 — the NATIONAL PCS ordinal (native UTF-16 order,
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.CharNational),                 //   or __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL)
                    $"{ArgInt(ic.Args[0])}{Collate(ic)}"),
            "CURRENT-DATE" => RuntimeApi.DateFn(nameof(CobolDate.CurrentDate), ""),   // §2023:15.21 — the runtime clock
            // WHEN-COMPILED is the COMPILATION timestamp (§2023:15.99.3 RV2) — a constant in the generated source,
            // captured ONCE PER COMPILATION on ProgramEmitter (kb/Work PB120), not per process. (The legacy's
            // runtime-clock placeholder also passes IF142A's plausibility checks; the constant is the
            // spec-correct form — scout brief §4.4.)
            "WHEN-COMPILED" => EmitText.CsLiteral(ctx.WhenCompiledStamp),
            "MAX" or "MIN" when ic.ComparesStrings =>                          // §2023:15.59/63 all-string form (PCS via CollatePrefix, CA23)
                RuntimeApi.Intrinsic(sig.Name is "MAX" ? nameof(CobolIntrinsics.MaxString) : nameof(CobolIntrinsics.MinString), CollatePrefix(ic) + StrArgList(ic)),
            "CONCAT" =>                                                        // §2023:15.18 — concatenate all argument images (2023);
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Concat),                       //   r1 admits class NUMERIC → the admitting list (PB59)
                    StrArgList(ic, admitNumeric: true)),
            // BOOLEAN-OF-INTEGER (§2023:15.13.4 RV1) — a boolean-result function on the D-B1 '0'/'1' substrate:
            // rightmost position = low-order digit, left zero-fill/truncate to argument-2 positions.
            // Argument-1 crosses the WIDE bridge (Int128 — §2023:15.13.3 AR1 admits any positive integer, PB65);
            // argument-2 stays on the long bridge (§2023:15.4 caps the returned length at CobolIntrinsics.ReturnedValueMaximum).
            "BOOLEAN-OF-INTEGER" =>
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.BooleanOfInteger), $"{ArgIntWide(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            // DISPLAY-OF (§2023:15.26) / NATIONAL-OF (§2023:15.66) — the national↔alphanumeric repertoire pair (2002);
            // the optional argument-2 is the one-character substitution string (§2023:15.26.3 AR2 / §2023:15.66.3 AR2).
            "DISPLAY-OF" or "NATIONAL-OF" =>
                RuntimeApi.Intrinsic(sig.Name is "DISPLAY-OF" ? nameof(CobolIntrinsics.DisplayOf) : nameof(CobolIntrinsics.NationalOf), StrArgList(ic)),
            "BASECONVERT" =>                                                   // §2023:15.12 — unsigned-integer base conversion (2023);
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.BaseConvert),                  //   r1 admits an unsigned integer LITERAL argument-1 below
                    $"{StrNum(ic.Args[0])}, {ArgInt(ic.Args[1])}, {ArgInt(ic.Args[2])}"),   // base 11 (PB59); args 2/3 stay integers
            "TRIM" =>                                                          // §2023:15.96 — delete leading/trailing/both of the char set (default: space)
                ic.Args.Any(a => a is BoundFieldOperand { Place: TableAllPlace })   //   a table(ALL) argument-2 list enumerates (kb/Work PB62)
                    ? LeadThenTail(ic, nameof(CobolIntrinsics.Trim), "", "string", Str, mid: $"{ic.TrimMode}, ")
                    : RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Trim), $"{Str(ic.Args[0])}, {ic.TrimMode}"
                        + string.Concat(ic.Args.Skip(1).Select(a => $", {Str(a)}"))),
            // STANDARD-COMPARE (§2023:15.85) — the cultural-ordering comparison over the derived CLDR/UCA engine
            // (kb/Work PB101 T7). The ordering TABLE travels as the bind-time-resolved literal-9 (null ⇒ §2023:15.85.3
            // r5's default table); the ordering LEVEL travels as 0 when argument-4 is omitted, which §2023:15.85.4 RV1
            // defines as "the highest level defined in the ordering table". Both are complete on the bound node,
            // so the backend never consults the SPECIAL-NAMES model.
            "STANDARD-COMPARE" =>
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.StandardCompare), $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, "
                    + $"{(ic.OrderingTable is { } ot ? EmitText.CsLiteral(ot) : "null")}, "
                    + $"{(ic.Args.Count > 2 ? ArgInt(ic.Args[2]) : "null")}"),   // null = argument-4 unspecified (§2023:15.85.4 RV1); a specified 0 is §2023:15.85.3 AR6's violation
            // The LOCALE functions (§2023:15.51–§2023:15.54; kb/Work PB64 T4): the bound LocaleRef travels as the named locale's
            // L1-normalized tag (null ⇒ the locale current for the category at use — §2023:14.6.6 GR7/r8); the runtime
            // resolves availability (EC-LOCALE-MISSING) and content (EC-LOCALE-INVALID) at use.
            "LOCALE-COMPARE" =>
                RuntimeApi.LocaleFn(nameof(CobolLocale.Compare), $"{Str(ic.Args[0])}, {Str(ic.Args[1])}, {LocaleTagArg(ic)}"),
            "LOCALE-DATE" =>
                RuntimeApi.LocaleFn(nameof(CobolLocale.Date), $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "LOCALE-TIME" =>
                RuntimeApi.LocaleFn(nameof(CobolLocale.Time), $"{Str(ic.Args[0])}, {LocaleTagArg(ic)}"),
            "LOCALE-TIME-FROM-SECONDS" => RenderLocaleTimeFromSeconds(ic),      // §2023:15.54 — standard numeric time form per t_fmt
            "SUBSTITUTE" => RenderSubstitute(ic),                              // §2023:15.87 — replace argument-2 pairs (2023)
            "CONVERT" =>                                                       // §2023:15.19 — repertoire / hex / byte conversion (2023);
                RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Convert),                      //   an ANY source takes the RAW STORAGE image (r7, PB59 5b)
                    $"{(ic.ConvertSource == 0 ? StorageArg(ic.Args[0]) : Str(ic.Args[0]))}, "
                    + $"{ic.ConvertSource}, {ic.ConvertDest}, {(ic.ConvertDestHex ? "true" : "false")}"),
            "MODULE-NAME" =>                                                   // §2023:15.65 — the runtime module call-name stack (2023)
                RuntimeApi.ModuleNameFn(ic.ModuleNameKeyword
                    ?? throw new InvalidOperationException("a MODULE-NAME call bound with no keyword (IntrinsicBinder.BindModuleName binds one)")),
            "FORMATTED-DATE" =>                                                // §2023:15.39 — integer date per a date format (2014)
                RuntimeApi.DateFn(nameof(CobolDate.FormattedDate), $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}"),
            "FORMATTED-TIME" => RenderFormattedTime(ic),                       // §2023:15.41
            "FORMATTED-DATETIME" => RenderFormattedDatetime(ic),               // §2023:15.40
            "FORMATTED-CURRENT-DATE" =>                                        // §2023:15.38 — the runtime clock per a combined format
                RuntimeApi.DateFn(nameof(CobolDate.FormattedCurrentDate), Str(ic.Args[0])),
            // The last-exception interrogation functions (§2023:15.28–15.33 — the EC model, incl. the national
            // twins EXCEPTION-FILE-N/EXCEPTION-LOCATION-N §2023:15.29/§2023:15.31): zero-argument reads of the runtime
            // register; the binder's EcNoteFunction flagged the group EC gate, so the generated source
            // carries the WiseOwl.Cobol.Runtime.Exceptions using.
            "EXCEPTION-STATUS" => RuntimeApi.EcFn(nameof(EcFunctions.Status)),                           // §2023:15.33
            "EXCEPTION-LOCATION" => RuntimeApi.EcFn(nameof(EcFunctions.Location)),                       // §2023:15.30
            "EXCEPTION-LOCATION-N" => RuntimeApi.EcFn(nameof(EcFunctions.LocationN)),                     // §2023:15.31 — the national twin
            "EXCEPTION-STATEMENT" => RuntimeApi.EcFn(nameof(EcFunctions.Statement),                     // §2023:15.32.3 RV1 and RV2 (63), §2002:15.27.2 RV1 and RV2 (31)
                EcFunctions.StatementLength(ctx.Data.DialectLevel).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            // §2023:15.28.4 / §2023:15.29.4: the no-argument form (r1) reads the last-exception register; the 2023
            // file-connector-argument form (r2) reads the NAMED connector — the SAME FileKeyExpr the file verbs use.
            "EXCEPTION-FILE" => ic.FileArg is { } eff
                ? RuntimeApi.EcFn(nameof(EcFunctions.File), EmitText.FileKeyExpr(eff))          // §2023:15.28.4 RV2 — the named connector's status
                : RuntimeApi.EcFn(nameof(EcFunctions.File)),                                    // §2023:15.28.4 RV1 — the no-argument form
            "EXCEPTION-FILE-N" => ic.FileArg is { } effn
                ? RuntimeApi.EcFn(nameof(EcFunctions.FileN), EmitText.FileKeyExpr(effn))        // §2023:15.29.4 RV2 — the national twin
                : RuntimeApi.EcFn(nameof(EcFunctions.FileN)),                                   // §2023:15.29.4 RV1 — the no-argument form
            _ => EmitText.LoudValue("string", $"FUNCTION {sig.Name} in a string context"),
        };
    }

    /// <summary>FORMATTED-TIME (§2023:15.41): seconds as (unscaled, scale) via <see cref="SecondsArg"/> — NOT the
    /// truncating ArgInt, so the fractional seconds survive — plus the optional offset-minutes (a3).</summary>
    private string RenderFormattedTime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[1]));
        return RuntimeApi.DateFn(nameof(CobolDate.FormattedTime), $"{Str(ic.Args[0])}, {secExpr}, {secScale}, "
             + $"{OffsetMinutesArg(ic, 2)}{LeapSecondFlag}");
    }

    /// <summary>The bound <see cref="BoundIntrinsicCall.Locale"/> as the runtime's <c>localeTag</c> argument: the named
    /// locale's L1-normalized tag literal, or <c>null</c> for the current-locale form.</summary>
    private static string LocaleTagArg(BoundIntrinsicCall ic) =>
        ic.Locale.Tag is { } tag ? EmitText.CsLiteral(tag) : "null";

    /// <summary>LOCALE-TIME-FROM-SECONDS (§2023:15.54): the seconds argument through the ONE <see cref="SecondsArg"/> pair
    /// (fraction-preserving — the nanosecond note of Annex D.31.4.5), the locale tag, the LEAP-SECOND flag.</summary>
    private string RenderLocaleTimeFromSeconds(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[0]));
        return RuntimeApi.LocaleFn(nameof(CobolLocale.TimeFromSeconds), $"{secExpr}, {secScale}, {LocaleTagArg(ic)}{LeapSecondFlag}");
    }

    /// <summary>FORMATTED-DATETIME (§2023:15.40): integer date a2 + seconds a3 (via <see cref="SecondsArg"/>) + the
    /// optional offset-minutes a4.</summary>
    private string RenderFormattedDatetime(BoundIntrinsicCall ic)
    {
        var (secExpr, secScale) = SecondsArg(ArgNum(ic.Args[2]));
        return RuntimeApi.DateFn(nameof(CobolDate.FormattedDatetime), $"{Str(ic.Args[0])}, {ArgInt(ic.Args[1])}, "
             + $"{secExpr}, {secScale}, {OffsetMinutesArg(ic, 3)}{LeapSecondFlag}");
    }

    /// <summary>The UTC offset in minutes of FORMATTED-TIME (argument-3) and FORMATTED-DATETIME (argument-4). An
    /// omitted offset IS 0: §2023:15.41.3 AR6 / §2023:15.40.3 AR7 evaluate the function "as though 0 were specified", so the
    /// runtime gets one value on one path whether the program wrote it or not (kb/Work PB2629: a separate "has
    /// offset" flag skipped the UTC adjustment for the omitted form only, and the two forms disagreed on the leap
    /// second).</summary>
    private string OffsetMinutesArg(BoundIntrinsicCall ic, int index) =>
        ic.Args.Count > index ? ArgInt(ic.Args[index]) : "0";

    /// <summary>The <c>&gt;&gt;LEAP-SECOND ON</c> argument every §2023:15.3 date/time function that reads a seconds subfield or
    /// a standard numeric time form takes (ISO §2023:7.3.17 / §2023:15.3.3.3 — kb/Work PB65): the compilation group's ONE
    /// directive fact (<c>DataBinder.LeapSecond</c>), passed as a trailing named argument only when ON, so every
    /// OFF emission is byte-identical to before (the CommaFlag discipline).</summary>
    private string LeapSecondFlag => ctx.Data.LeapSecond ? ", leapSecond: true" : "";

    /// <summary>The SECONDS argument of the formatted time family as an (unscaled, scale) pair, TOTAL over the
    /// four value carriers (kb/Work R24 — ledger F44/F46/F57; §2023:15.41.4 RV1 / §2023:15.40.4 make the returned value a
    /// representation of the VALUE "contained in" the argument, fraction included):
    /// <list type="bullet">
    ///   <item>a FIXED-POINT operand passes through at its own scale with NO narrowing cast — the former
    ///         <c>(long)(expr)</c> silently WRAPPED a wide picture's unscaled form (a 9(5)V9(15) item holding
    ///         the in-range 45296.5 fabricated 02:20:03), and the runtime now takes <see cref="Int128"/>;</item>
    ///   <item>a FLOAT operand lands through <c>CobolDate.SecondsOfReal</c> at scale 9 — the
    ///         FORMATTED-CURRENT-DATE nanosecond convention, and every reliable binary64 fraction digit under a
    ///         5-integer-digit bound — where the former cast truncated to WHOLE seconds;</item>
    ///   <item>an SDIDI operand (a legal §2023:15.3 type-10 expression under a standard mode) lands exactly at scale
    ///         18, the documented §2023:15.3.3.2 maximum fraction width (CONFORMANCE.md item 202) — it used to reach
    ///         the cast as a raw CS1503, the PB2 shape on the Dec axis;</item>
    ///   <item>an unsigned-wide operand narrows through the R10 Widen funnel (a valid time always fits).</item>
    /// </list></summary>
    private static (string Expr, int Scale) SecondsArg(NumX x)
    {
        x = NumericRenderer.DeU(x);
        // ⛔ BOTH LANDINGS ROUND TOWARD NEGATIVE INFINITY AND NEVER RAISE (kb/Work PB1379): the consumers screen the
        // landed value against §2023:7.3.17.4's [0, 86 400) / [0, 86 401), and a landing that truncated a tiny negative to
        // zero (or raised a size error for a huge one) answered that screen before it could. CobolDate.SecondsOfReal
        // / SecondsOfDec preserve membership in the form; the fixed-point arm below is exact at its own scale.
        if (x.Real) return (RuntimeApi.SecondsLanding(x.Expr, dec: false), 9);
        if (x.Dec) return (RuntimeApi.SecondsLanding(x.Expr, dec: true), 18);
        return (x.Expr, x.Scale);
    }

    /// <summary>SUBSTITUTE (§2023:15.87): the source (Args[0]) plus parallel from/to/mode arrays over the pair operands
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
            return RuntimeApi.Intrinsic(nameof(CobolIntrinsics.SubstituteFlat), $"{Str(ic.Args[0])}, "
                + $"new string[][] {{ {string.Join(", ", parts)} }}, "
                + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}{AnycaseClassification(ic)}");
        }
        var froms = new List<string>();
        var tos = new List<string>();
        for (int i = 1; i + 1 < ic.Args.Count; i += 2)
        {
            froms.Add(Str(ic.Args[i]));
            tos.Add(Str(ic.Args[i + 1]));
        }
        return RuntimeApi.Intrinsic(nameof(CobolIntrinsics.Substitute), $"{Str(ic.Args[0])}, "
            + $"new string[] {{ {string.Join(", ", froms)} }}, "
            + $"new string[] {{ {string.Join(", ", tos)} }}, "
            + $"new int[] {{ {string.Join(", ", ic.SubstituteModes ?? [])} }}{AnycaseClassification(ic)}");
    }

    // ── String-channel argument rendering (P7 Step 12 — the ONE NumericRenderer under the default receiver) ──

    /// <summary>A numeric-kind argument inside the STRING channel, rendered by the ONE
    /// <see cref="NumericRenderer"/> under <see cref="ReceiverContext.None"/> — a string-channel render has no
    /// numeric receiver, exactly the Step-3 default-receiver convention (DESIGN-codegen-backend §2.5). The
    /// renderer's public entries save/restore the ambient receiver, so this re-entrant call cannot go stale.</summary>
    /// <remarks>INTAKE(EXACT) — the STRING channel's numeric intake, under <see cref="ReceiverContext.None"/> — a text context has no numeric receiver to derive a landing from.</remarks>
    private NumX ArgNum(BoundOperand op) => num.AsNum(op, ReceiverContext.None);

    /// <summary>An integer-kind argument inside the STRING channel (CHAR / BASE-CONVERT / the formatted
    /// date-time offsets) — the same (long) truncation the numeric channel's <see cref="IntArg"/> applies.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the STRING channel's integer intake — the same scale-0 truncation <see cref="IntArg"/> applies.</remarks>
    private string ArgInt(BoundOperand op) => AsInt(ArgNum(op));

    /// <summary>The WIDE integer-argument bridge on the STRING channel — the Int128-carrier twin of
    /// <see cref="ArgInt"/>. <see cref="AsIntWide"/> carries the rule.</summary>
    /// <remarks>INTAKE(INTEGRAL) — the STRING channel's WIDE integer intake — scale-0 truncation on the <c>Int128</c> carrier.</remarks>
    private string ArgIntWide(BoundOperand op) => AsIntWide(ArgNum(op));

    /// <summary>
    /// ⛔ THE INTAKE FOR A <b>TOTAL</b> §15 INTEGER ARGUMENT — no narrowing, therefore no raise point.
    /// </summary>
    /// <remarks>
    /// <para>THE RULE, and it is a property of the ARGUMENT, never of the arm that happens to render it: a §15
    /// integer argument whose function definition places <b>no constraint on its VALUE</b> is total — the
    /// returned-value rule answers for every integer — so §2023:15.3's closing paragraph has nothing to fire on
    /// ("the evaluation of an argument results in an incorrect value for that argument or for the returned
    /// value <i>according to the rules specified in the function definition</i>"), and putting
    /// <see cref="AsInt"/>'s §2023:15.3 landing in front of it manufactures an exception condition the standard
    /// does not define. Members today:</para>
    /// <list type="bullet">
    /// <item>BOOLEAN-OF-INTEGER argument-1 — §2023:15.13.3 AR1 constrains the SIGN only; §2023:15.13.4 RV1's bit
    /// configuration is mathematical, so 2⁶³ and up are legal (fix-queue PB65 / RV-15.13.4-1 D1).</item>
    /// <item>TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD argument-1 — §2023:15.90.3 AR1 / §2023:15.91.3 AR1 say only "shall be
    /// an integer" and r1a is a CATCH-ALL (kb/Work PB254 / RV-15.90.4-1 / RV-15.91.4-1).</item>
    /// <item>FIND-STRING argument-3 — §2023:15.37.3 AR3 places no value constraint; §2023:15.37.4 RV2/r3 answer for every
    /// integer (kb/Work PB254).</item>
    /// <item>RANDOM argument-1 — §2023:15.75.3 AR2 constrains the SIGN only ("it shall be zero or a positive
    /// integer") and §2023:15.75.4 RV3 makes the distinct-sequence subset a FLOOR ("shall include the values from 0
    /// through at least 32767"), not a domain, so a wider seed is legal and its sequence is defined
    /// (kb/Work PB636 — the narrow intake aborted the run unit on a 19-digit seed).</item>
    /// <item>ANNUITY argument-2 — §2023:15.9.3 AR3 constrains the SIGN only ("a positive integer"; kb/Work PB637). It is
    /// TOTAL too, but its value rides the SDIDI, whose integer reaches past <c>Int128</c>, so the SDIDI arm does NOT
    /// take this intake: it keeps the position's unbounded SDIDI integer (<see cref="DecArg"/> over
    /// <see cref="RawArg"/>), and the binary64 arm takes it as a binary64 (<see cref="Dbl"/>).
    /// <c>IntrinsicCarrierAgreementDriftTests.IntegerArgumentsThatRideTheSdidi_TakeNoNativeIntake</c> holds that.</item>
    /// </list>
    /// <para>⚠ THE SET IS NOT MAINTAINED HERE. The runtime body's declared parameter carrier IS the totality
    /// claim — <c>Int128</c> for a total argument, <c>long</c> for one the argument rules bound — and
    /// <c>IntrinsicCarrierAgreementDriftTests.EveryTotalIntegerArgument_TakesTheWideIntake</c> re-derives the
    /// pairing from the runtime signatures and fails when an arm and its body disagree. Widening a body is
    /// therefore enough; forgetting the arm is red, not silent.</para>
    /// <para>Every legal fixed-point value rides Int128 exactly, so a scale-0 operand passes through RAW; the
    /// carrier arms mirror <see cref="AsInt"/> exactly (Dec BEFORE the scale test — a Dec NumX carries Scale 0
    /// by convention, the PB14/PB32 lesson; the unsigned-wide lane narrows through the R10
    /// <c>CobolNum.Widen</c> funnel, loud past the intermediate).</para>
    /// </remarks>
    private static string AsIntWide(NumX a) => IntegerIntake(a, wide: true);

    /// <summary>A string-kind argument (the §2023:15.3 alphanumeric-argument shapes): literals, field display
    /// images, and nested alphanumeric intrinsics. A numeric-category operand in a string-argument position
    /// stays loud — §15's string functions take alphanumeric/national/boolean arguments (the named channel).
    /// ⛔ THE DEFAULT IS NON-ADMITTING AND MUST STAY SO: NUMVAL/NUMVAL-F carry open rows (AR-15.67.3-1 /
    /// AR-15.69.3-1) DEMANDING a compile-time rejection of a numeric literal — widening this default would
    /// turn their wrong-stage defect into a silently-wrong one.</summary>
    private string Str(BoundOperand op) => op.Accept(_strArg ??= new StrArgVisitor(this, admitNumeric: false));
    private StrArgVisitor? _strArg;

    /// <summary>CONVERT's ANY-source argument — the RAW STORAGE byte image (§2023:15.19.3 AR7: any usage, contents
    /// need not be valid for it — the storage BITS, never the display image; PB59 family 5b). A FIELD renders
    /// through the ONE storage channel (<see cref="OperandText.AsStorageImage"/>); a literal renders its
    /// declared storage (an N"…" literal its UTF-16BE bytes, a B"…" literal its packed bits, an alphanumeric
    /// literal its characters under the item-209 1-byte serialization); a counter register the bytes of its description
    /// (<see cref="OperandText.CounterRegisterStorageImage"/>). Every other shape (computed values,
    /// figuratives, ALL literals) HAS no storage — those keep the display-image channel they always rode, which
    /// for a numeric shape stays the deliberately-loud <see cref="Str"/> default.</summary>
    private string StorageArg(BoundOperand op) => op switch
    {
        // ⚠ transfer: true — CONVERT only READS the storage image (kb/Work PB1901, the Visit(BoundFieldOperand) twin):
        // a pointer-leaf strongly-typed group's image is its D-SLOT placeholder, the one image every reader gets.
        BoundFieldOperand f => OperandText.AsStorageImage(f.Place, transfer: true),
        // A class-boolean item in grouping parentheses is a boolean EXPRESSION operand (§2023:8.8.2, "a boolean expression enclosed in
        // parentheses"; §2023:8.4.3.2.3 SR8 admits it as an argument), and the parentheses leave the item's own bits or characters as
        // they are (IntrinsicArgumentRules.StaticUsageOf reads the same item for the r7 usage screen): its storage image is the
        // item's, not the '0'/'1' text the boolean renderer would send (kb/Work PB4635 review R2-1). A COMPOUND boolean
        // expression has no storage and keeps its text.
        BoundBoolOperand { Expr: BoundBoolRef { Place: var p } } => OperandText.AsStorageImage(p, transfer: true),
        BoundStringLiteral { Category: PicCategory.National } sl => RuntimeApi.NatBytes(EmitText.CsLiteral(sl.Value)),
        BoundStringLiteral { Category: PicCategory.Boolean } sl =>
            RuntimeApi.BitsPack(EmitText.CsLiteral(sl.Value), sl.Value.Length.ToString()),
        // A COUNTER REGISTER is a data item with a description (§2023:8.4.3.14.4 GR1, §2023:8.4.3.15.4 GR1), so its storage is that
        // description's bytes, not the display image Str gives a character argument (kb/Work PB4766 review R1-2).
        BoundComputedOperand c when OperandText.IsCounterRegister(c) => OperandText.CounterRegisterStorageImage(c, Num),
        _ => Str(op),
    };

    /// <summary>The NUMERIC-ADMITTING string-argument entry (fix-queue PB59 / RV-15.12.4-1, RV-15.18.4-1) —
    /// for EXACTLY the functions whose §15.x.3 argument rules admit a numeric literal: BASECONVERT argument-1
    /// (§2023:15.12.3 AR1 — "a usage display or national data item or literal, and, if the base … is less than 11,
    /// … an unsigned integer data item or literal") and CONCAT (§2023:15.18.3 AR1 — class numeric is in its list).
    /// The literal renders through the ONE <c>OperandText.AsString</c> image (raw source text — correct for
    /// §2023:15.18.4 RV1; r3's value conditions are NOT the renderer's to enforce and stay recorded on the
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
        // An intrinsic function READS its argument's characters and never reads them back, so a strongly-typed group
        // holding a pointer/object leaf (§2023:15.3 type 2: "treated as though they were of class and category
        // alphanumeric") takes the one-way transfer image, not the two-way Tier-C refusal (kb/Work PB1901).
        public string Visit(BoundFieldOperand n) =>
            OperandText.AsTransferString(n, owner.Num, "intrinsic function argument");
        // THE CURRENT RECORD (kb/Work PB339) is built only for a READ/RETURN INTO implicit MOVE, so no intrinsic
        // argument list can hold one today — but its image is alphanumeric and already written down once, so this
        // DELEGATES rather than going loud (the same delegation the field arm above makes; a loud arm here would
        // be the wrong-stage failure PB25 fixed for figuratives).
        public string Visit(BoundCurrentRecord n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundComputedOperand n) =>
            n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } nested
                ? owner.RenderString(nested)   // string-class results incl. national (§2023:15.66) + boolean (§2023:15.13 — the '0'/'1' substrate)
            // ⛔ THE --permissive COERCION OF A PARENTHESIZED ITEM OR LITERAL AT A CHARACTER POSITION (kb/Work PB4635 review R2-3): the
            // binder makes `(X9)` an arithmetic expression, so strict mode refuses it at a string function (COBOL0844 / COBOL1627) and
            // --permissive warns and coerces — the coercion offered every other operand that holds characters is its own image, and the
            // enclosed item or literal is that operand (it ran as the bare spelling before the parentheses were kept). The two
            // numeric-admitting positions (CONCAT, BASECONVERT argument-1) are no coercion site: their "data item or literal" rule
            // refuses the expression's SHAPE in every mode (IntrinsicArgumentRules.IsCoercible, review R3-1), so this arm never serves them.
            : n.EnclosedOperand is { } enclosed
                ? enclosed.Accept(this)
            // ⛔ A NUMERIC FUNCTION'S RETURNED VALUE AT A NUMERIC-ADMITTING POSITION (kb/Work PB1420). CONCAT §2023:15.18.3 AR1
            // lists class numeric and BASECONVERT §2023:15.12.3 AR1 a numeric argument-1 below base 11, and §2023:8.4.3.2.3 SR12
            // exempts "the integer form of the ABS function" from the bar on integer functions at an unsigned-integer
            // position — so `FUNCTION CONCAT(FUNCTION ABS(3) "A")` is LEGAL and used to compile clean and abort at run
            // time on this arm (the wrong-stage family). Its text is the ONE numeric-function image,
            // OperandText.AsString's (DOC-A.1-92: the significant digits, no padding), exactly what a numeric LITERAL
            // argument renders beside it. The binder bars every other numeric/integer function here (SR11/SR12), so a
            // non-admitting position still reaches Loud only through a --permissive coercion.
            : admitNumeric && n.Expr is BoundIntrinsicCall { ResultCategory: PicCategory.Numeric }
                ? OperandText.AsString(n, owner.Num)
            // ⛔ A COUNTER REGISTER AS A CHARACTER ARGUMENT (kb/Work PB4766). LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER each a
            // "temporary unsigned integer data item" of class and category numeric (§2023:8.4.3.14.4 GR1, §2023:8.4.3.15.4 GR1), a DATA ITEM
            // like the numeric field the field arm above images without asking which position it is at, so CONCAT (§2023:15.18.3 AR1) and
            // BASECONVERT argument-1 (§2023:15.12.3 AR1) admit it bare and it compiled clean and died on the loud arm below (the
            // wrong-stage family). Its image is the unsigned integer item's, the ONE form OperandText.AsString gives a register in a
            // string context (its digits zero-filled to the register's implicit digit count, AlgebraicRanges.CounterRegisterDigits).
            // The usage screens see the register through IntrinsicArgumentRules.StaticUsageOf (its description's usage), so CONCAT
            // r2/r3 and BASECONVERT r1 refuse LINE-/PAGE-COUNTER (binary) as they refuse a declared BINARY item, and only
            // LINAGE-COUNTER (a DISPLAY item) reaches this arm; it renders.
            : OperandText.IsCounterRegister(n)
                ? OperandText.AsString(n, owner.Num)
            // ⛔ THE --permissive COERCION OF A COMPOUND ARITHMETIC EXPRESSION AT A CHARACTER POSITION (kb/Work PB4710): `TRIM (I2 + 1)`
            // is refused in every strict mode (an arithmetic expression is class numeric where TRIM requires a character class,
            // COBOL1627), and --permissive demotes the refusal to a warning and coerces. An expression holds no characters, so its
            // image is its VALUE's text, the one form a numeric function's returned value has at a character position
            // (OperandText.AsString, DOC-A.1-92): TRIM (I2 + 1) of I2 = 2 is "3". It used to reach the loud arm below and die at run
            // time with NotImplementedCobolFeatureException on legal-shaped, warned source (the wrong-stage family). A parenthesized
            // item or literal is the enclosed operand's own image (above); the positions that read characters keep the error
            // (IntrinsicArgumentRules.IsCoercible), so this arm never serves them.
            : n.Expr is BoundBinary or BoundNegate or BoundPower
                ? OperandText.AsString(n, owner.Num)
                : Loud(n);
        public string Visit(BoundOperandError n) => EmitText.LoudValue("string", n.Feature);
        // An address-identifier (kb/Work PB1021) is class pointer: the binder admits it only as a relation operand, which
        // ConditionRenderer's pointer arms render, and an INVOKE argument, which OoEmitter renders — never a
        // character or numeric value. A class-screened intrinsic position refuses it even under --permissive, whose
        // coercion is offered only for an operand that holds characters (IntrinsicBinder.ReportClass, kb/Work PB2079;
        // PB2074 pins UPPER-CASE (ADDRESS OF X)). Reaching this is a binder hole, so it is LOUD, never a guessed value.
        public string Visit(BoundAddressOperand n) => EmitText.LoudValue("string", "address-identifier as a character argument");
        // Admitted PER-FUNCTION (PB59): the raw source-text image via the ONE OperandText channel where the
        // function's §15.x.3 rule admits a numeric literal (see StrNum); Loud everywhere else — see Str's ⛔.
        public string Visit(BoundNumericLiteral n) => admitNumeric ? OperandText.AsString(n, owner.Num) : Loud(n);
        // ⛔ A FIGURATIVE CONSTANT IS A LEGAL INTRINSIC ARGUMENT, AND ITS IMAGE IS ALREADY WRITTEN DOWN ONCE
        // (fix-queue PB25). §2023:8.3.3.6.3 SR1 admits a figurative constant "whenever 'literal' appears in a format",
        // and §2023:8.4.3.2.3 SR8 makes a literal a valid argument-1 — so `FUNCTION LOWER-CASE(SPACE)` is legal source.
        // Both arms used to render EmitText.LoudValue, so it compiled CLEAN and aborted at RUN TIME with
        // "intrinsic string argument 'BoundFigurative'": the wrong-stage family, on legal input.
        // The image is §2023:8.3.3.6.4 GR3's, for the case where "the length of the string is NOT specified in the
        // rules for the context" — which is exactly a bare function argument: (b) a figurative other than
        // ALL literal-1 is ONE character, (c) otherwise the length of literal-1. OperandText.AsString already
        // implements precisely that, PCS-aware (its BoundFigurative interception is collating-context sensitive,
        // which a local copy here could not be, since this visitor cannot reach the renderer's collating state).
        // So these delegate rather than re-deriving it — the same delegation the BoundFieldOperand arm above
        // already makes.
        public string Visit(BoundFigurative n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundAllLiteral n) => OperandText.AsString(n, owner.Num);
        public string Visit(BoundPredefinedNull n) => OperandText.AsString(n, owner.Num);   // throws — never an intrinsic argument
        // A boolean EXPRESSION argument (§2023:8.4.3.2.3 SR8; kb/Work PB65): its '0'/'1' image through the ONE boolean
        // renderer — INTEGER-OF-BOOLEAN(BIT-A B-AND BIT-B) reads the combined bit string.
        public string Visit(BoundBoolOperand n) => BooleanRenderer.Render(n.Expr, owner.Num);
    }
}
