// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using System.Diagnostics;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ <b>THE INTRINSIC CARRIER RULES TWO LAYERS ASK</b> (kb/Work PB3268, review R1-2): which carrier a numeric function's
/// value is computed on is decided by <c>IntrinsicRenderer.RenderNum</c> (compiler.codegen) and PREDICTED at bind time
/// by <c>FunctionValueLane</c> (compiler.binding), which describes the §14.9.25.4 GR1 intermediate result item for it.
/// The facts both read are pure functions of a function's identity and of operand digit bounds, so they live in the
/// layer both may reach (DESIGN-architecture-review.md §8.1: compiler.codegen reaches compiler.bound, compiler.binding
/// reaches compiler.bound, and neither reaches the other) — written once, never restated by either side.
/// <para>⛔ <b>KEYED ON THE FUNCTION, NEVER ON A BACKEND'S SPELLING OF IT</b> (kb/Work PB4370;
/// DESIGN-backend-abstraction.md §1.2 bullet 2). Every key is an intrinsic-function-name as its catalog row spells it
/// (<c>IntrinsicSig.Name</c>, the key <c>IntrinsicCatalog.TryGet</c> looks up): <c>"MOD"</c>, <c>"NUMVAL-C"</c>. The
/// runtime body a backend calls for the function is that backend's spelling of the identity (the Roslyn renderer's is
/// the row's <c>RuntimeMethod</c>: <c>ModScaled</c>, <c>NumvalC</c>), so a second backend asks these rules of the same
/// name and spells its own call. <c>IntrinsicCarrierRuleKeyDriftTests</c> holds every key to a catalog row, so a misspelt
/// key cannot answer false in silence.</para>
/// </summary>
internal static class IntrinsicCarrierRules
{
    /// <summary>
    /// ⛔ THE FIRST HALF OF THE ALWAYS-SDIDI SET, AND THE ONLY HALF WITH A STRUCTURAL DEFINITION: the functions whose
    /// native-switch arm in <c>IntrinsicRenderer.RenderNum</c> CROSS-ALIGNS their arguments to one common scale before
    /// evaluating.
    /// </summary>
    /// <remarks>
    /// <para>Under a standard mode §15.4.1 r1 makes each of these equal its equivalent arithmetic expression
    /// (§15.61.4 MEDIAN, §15.62.4 MIDRANGE, §15.64.4 MOD, §15.76.4 RANGE, §15.77.4 REM, §15.88.4 SUM; MEAN, §15.60.4, is
    /// the SDIDI quotient in EVERY mode, so it belongs to <see cref="SdidiQuotientFunctions"/> and not to this set), and each argument converts to the SDIDI INDIVIDUALLY (§8.8.1.5.2 r1) — so no common scale is
    /// ever formed. The NATIVE arms instead align first, on the Int128 carrier, and alignment MULTIPLIES: a
    /// 31-digit integer beside a scale-18 item needs 49 digits, so <c>NumericRenderer.Align</c>'s per-argument
    /// escape raises EC-SIZE-OVERFLOW where the SDIDI holds the answer exactly. That makes "does this arm
    /// cross-align?" the exact test for "must this arm be on the Dec lane under a standard mode".</para>
    /// <para>⛔ It was built one arm at a time and each pass believed itself complete: PB62 added the summing
    /// family for MEAN's <c>MEAN(10³⁰, 2.0)</c>; PB252 found MOD and REM still missing — the SAME clause, the SAME
    /// mechanism, the arm nobody swept (feedback_two_arm_dispatch). It is a SET rather than an <c>is … or …</c>
    /// pattern so that <c>ExactCarrierBoundaryDriftTests</c> can hold it against the switch itself: the test reads
    /// <c>IntrinsicRenderer.cs</c>, collects the case labels of every arm whose body calls
    /// <c>IntrinsicRenderer.AlignedArgs</c> or <c>NumericRenderer.Align(x, s)</c> against a
    /// COMMON scale, and fails unless they are exactly the runtime methods the catalog rows of this set's functions
    /// name. A new aligning arm is therefore routed automatically or the build goes red — it can no longer be
    /// forgotten.</para>
    /// <para>MAX / MIN / ORD-MAX / ORD-MIN are deliberately ABSENT: <c>IntrinsicRenderer.RawArgPairs</c> aligns each argument
    /// to its OWN scale (an identity rescale), because §15.59.4 / §15.63.4 / §15.71.4 / §15.72.4 return the
    /// CONTENT or the ORDINAL of an argument — pure selection, no cross-argument arithmetic, no common scale to
    /// escape (kb/Work PB65).</para>
    /// </remarks>
    internal static readonly FrozenSet<string> CrossAligningFunctions = new[]
    {
        "MOD", "REM",                                               // §15.64.4 / §15.77.4 — Align(a, s), Align(b, s)
        "SUM", "RANGE",                                             // §15.88.4 / §15.76.4 — AlignedArgs
        "MEDIAN", "MIDRANGE",                                       // §15.61.4 / §15.62.4 — AlignedArgs
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// ⛔ THE FUNCTIONS WHOSE RETURNED VALUE IS THE SDIDI QUOTIENT OF THEIR EQUIVALENT ARITHMETIC EXPRESSION IN EVERY ARITHMETIC
    /// MODE, CHANNEL AND RECEIVER (kb/Work PB3860). Asked by <c>IntrinsicRenderer.RenderNum</c> (native, for an argument list with
    /// no binary64 operand), by <c>IntrinsicRenderer.RenderDec</c> and by <c>FunctionValueLane</c>.
    /// </summary>
    /// <remarks>
    /// <para>MEAN's equivalent arithmetic expression ends in a division, <c>((argument-11 + … + argument-1n) / n)</c>
    /// (§15.60.4 r1), and a quotient is the one operation whose digits are not finite: under a standard mode §15.4.1 r1 makes
    /// the value that expression's SDIDI value, and under native arithmetic §15.4.1 makes it "an implementor-defined
    /// approximation of the value of that expression". The native arm chose that approximation from the RECEIVER - the
    /// quotient was cut at max(receiver scale, sum scale + 1, 6) places and the receiver's own ROUNDED phrase then rounded
    /// the cut - so one reference held two values: <c>MEAN(1E-8 and nineteen zeros)</c> is 5E-10 and
    /// <c>COMPUTE R ROUNDED MODE AWAY-FROM-ZERO</c> into <c>PIC S9V99</c> stored 0.00 where the one rounding of 5E-10 is 0.01.
    /// The implementor's determination (owner, 2026-10-26, kb/Work PB3839 / PB3860: "always be consistent with ourself
    /// barring an explicit requirement differently") is the standard-mode value in every mode, so no receiver can change it.</para>
    /// <para>A function belongs here only when its equivalent expression DIVIDES by a count or a value that makes the quotient
    /// inexact: MEDIAN and MIDRANGE halve, and the halving is exact at the common argument scale plus one
    /// (<see cref="CrossAligningFunctions"/>), so they have no receiver-dependent digit to remove. A binary64 operand keeps the
    /// float lane, whose one value IS the double (<c>ReceiverShapeReaderDriftTests.Readers</c>, <c>FloatLanding</c>).</para>
    /// </remarks>
    internal static readonly FrozenSet<string> SdidiQuotientFunctions = new[] { "MEAN" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Is the value of the function <paramref name="functionName"/> the SDIDI quotient of its equivalent
    /// arithmetic expression whatever receives it (<see cref="SdidiQuotientFunctions"/>)?</summary>
    internal static bool ReturnsSdidiQuotient(string functionName) => SdidiQuotientFunctions.Contains(functionName);

    /// <summary>
    /// ⛔ THE FUNCTIONS WHOSE RETURNED VALUE IS THE EXACT PARSE OF THEIR STRING ARGUMENT IN EVERY ARITHMETIC MODE AND IN
    /// EVERY CHANNEL, so no working scale and no receiver shape may touch it (kb/Work PB251, PB3839). Asked by
    /// <c>IntrinsicRenderer.RenderNum</c> BEFORE the arithmetic-mode dispatch, and by <c>FunctionValueLane</c>.
    /// </summary>
    /// <remarks>
    /// <para>⛔ THE SET IS THREE: TWO BY DEFINITION AND ONE BY THE IMPLEMENTOR'S DETERMINATION. NUMVAL §15.67.4 r1 and
    /// NUMVAL-C §15.68.4 r1 are specifications "otherwise" in §15.4.1's closing conditional ("implementor-defined unless
    /// otherwise specified in the function definition"): "The returned value is the numeric value represented by
    /// argument-1", with no arithmetic-mode qualification, so the latitude is withdrawn. NUMVAL-F is the opposite
    /// case: §15.69.4 r2 makes the native and standard-binary value "an approximation of the numeric value represented
    /// by argument-1" and r3 gives standard-decimal arithmetic the value itself, so the approximation is the
    /// implementor's to choose. WiseOwl COBOL chooses the one approximation that is also r3's value, the EXACT parse
    /// (owner, 2026-10-26, kb/Work PB3839: "We should always be consistent with ourself barring an explicit requirement
    /// differently"). Every other approximation made the value depend on the receiver: a binary64 for a DISPLAY, a
    /// working-scale truncation for a fixed-point receiver that then rounded the truncation again.</para>
    /// <para>The sweep that derived the first two members screened clause 15 for a "Returned value rules" clause that
    /// mentions NEITHER an arithmetic mode, NOR "implementor", NOR "approximation", NOR an equivalent arithmetic
    /// expression. Over clause 15 that yields 38 functions, and every one but NUMVAL and NUMVAL-C returns an INTEGER, a
    /// STRING, or a selected argument's own content, shapes no fraction-digit working scale can damage. E (§15.27.3) and
    /// PI (§15.73.3) return, under native arithmetic, "an implementor-defined approximation of the arithmetic expression" and SQRT (§15.84.4) the absolute value of an approximation, all of values with no finite decimal
    /// spelling, which is why they keep the float lane; NUMVAL-F is the one approximating function whose exact value is
    /// a decimal its argument already spells. <c>IntrinsicRenderer.RenderDec</c> holds the SDIDI arms, so this predicate
    /// only decides WHEN that lane is taken, never what it does. If it ever returned <c>null</c> there the switch's
    /// <c>default</c> stages LOUD, never a quiet fallback to a working-scale render.</para>
    /// </remarks>
    internal static bool ReturnsExactParse(string functionName) => ExactParseFunctions.Contains(functionName);

    /// <summary>
    /// ⛔ IS A FUNCTION'S BINARY64 RETURNED VALUE AN ENGINE APPROXIMATION, OR A FLOAT ITEM'S CONTENT (kb/Work PB4622)? Asked
    /// by <c>FunctionValueLane.Of</c> (bind time) and <c>IntrinsicRenderer.RenderFloat</c> (the renderer) alike, so the two
    /// cannot disagree. It is an APPROXIMATION exactly when the function is in the §15.4.1 floating-math family
    /// (<paramref name="floatFamily"/>: SIN, SQRT, EXP, LOG, ...: no finite decimal spells its value, the implementor's
    /// approximation is the whole of §15.4.1's native latitude), or when no binary64 argument is a float ITEM's content
    /// (<paramref name="anyFloatItemArgument"/>: every binary64 argument is itself an approximation, so the value of a
    /// value-preserving function of it is one too: ABS (FUNCTION EXP10 (-6)) stays 0.000001). Otherwise the value is
    /// derived from a float ITEM's exact binary64 by the function's own definition: MAX, MIN and an odd MEDIAN return "the
    /// content of the argument" (§15.59.4 1), §15.63.4 1), §15.61.1), INTEGER the greatest integer not above it (§15.44.4
    /// 1)), and ABS, INTEGER-PART, FRACTION-PART, SIGN, REM, SUM, MEAN, RANGE and MIDRANGE have an equivalent arithmetic
    /// expression (§15.4.1) that a float item's binary64 evaluates exactly or as the written-out expression itself does
    /// (D16). It lands by its exact binary value, as <c>COMPUTE R = F</c> and <c>MOVE F</c> land the item.
    /// </summary>
    internal static bool Binary64ResultIsApproximation(bool floatFamily, bool anyFloatItemArgument) =>
        floatFamily || !anyFloatItemArgument;

    /// <summary>
    /// ⛔ DOES A SELECTION LIST (MAX, MIN, ORD-MAX, ORD-MIN, an odd MEDIAN) NEED THE EXACT LANE TO DELIVER THE SELECTED ARGUMENT FROM ITS OWN
    /// CARRIER (kb/Work PB65 RV-15.59.4-1 D2, PB4622)? Asked by <c>IntrinsicRenderer.RenderNum</c> and <c>FunctionValueLane.Of</c> alike.
    /// §15.59.4 1), §15.63.4 1) and §15.61.1 return "the content of the argument" selected, so the value is the selected argument's own:
    /// <list type="bullet">
    ///   <item>a list that mixes a binary64 with a fixed-point item (<paramref name="allBinary64"/> false), as the PB65 finding found;</item>
    ///   <item>a list whose binary64 arguments are of TWO KINDS (<paramref name="anyFloatItem"/> and <paramref name="anyApproximation"/>):
    ///         a float ITEM's content lands by its exact binary value, an engine's APPROXIMATION by its shortest decimal (D16), and
    ///         which argument is selected is a run-time fact, so the double the float lane would return cannot say which landing it is
    ///         owed (<c>MAX (ZF FUNCTION EXP10 (-6))</c> into <c>PIC 9V9(6)</c> stored 0.000000, the exact binary expansion of the
    ///         approximation, where its decimal is 0.000001). On the exact lane each argument enters as its own kind
    ///         (<c>CobolExact.FromBinary64</c>, <c>CobolExact.FromApproximation</c>) and the selected one lands as that kind.</item>
    /// </list>
    /// A list of one kind of binary64 stays on the float lane: its content IS the double. So does an even-count MEDIAN of two kinds
    /// (<paramref name="meanOfMiddlePair"/>, kb/Work PB4753): the mean of the two middle values is a NEW value, evaluated as its
    /// equivalent expression is (§15.4.1) in binary64 beside a float item, where the exact lane landed it one binary64 low. A MIXED
    /// even list keeps the exact lane because WHICH values are the middle pair is a run-time fact: its body
    /// (<c>CobolIntrinsics.MedianBinary64Exact</c>, kb/Work PB4859) forms the binary64 mean when a MIDDLE value is binary64 and the
    /// exact mean when both are fixed-point (a binary64 elsewhere in the list is not an operand of the mean).
    /// </summary>
    internal static bool SelectionNeedsExactLane(bool anyBinary64, bool allBinary64, bool anyFloatItem, bool anyApproximation,
        bool meanOfMiddlePair) =>
        anyBinary64 && (!allBinary64 || (!meanOfMiddlePair && anyFloatItem && anyApproximation));

    /// <summary>Is the value of the call of <paramref name="functionName"/> over <paramref name="arguments"/> the arithmetic MEAN of the two middle arguments and not a selected argument
    /// (kb/Work PB4753, PB4791)? True for MEDIAN over an even count of arguments (§15.61.4 r2). A table(ALL) argument stands for as many
    /// arguments as its ranges hold (<see cref="TableAllPlace.StaticCount"/>, the product of a fixed OCCURS's counts), so a list of
    /// fixed tables has a bind-time count and a bind-time parity. A table(ALL) over an OCCURS DEPENDING or dynamic-capacity range has a
    /// RUN-TIME count (<see cref="MedianParityIsRunTime"/>): the parity is not a bind-time fact, so this answers false and the list is
    /// not routed to the float lane here; the exact lane decides the parity at run time (<c>CobolIntrinsics.MedianBinary64Exact</c>,
    /// kb/Work PB4827). A mean is a NEW value that no argument's own kind describes, so the kind-preserving exact lane of
    /// <see cref="SelectionNeedsExactLane"/> has nothing to preserve for it.</summary>
    internal static bool ReturnsMeanOfMiddlePair(string functionName, IReadOnlyList<BoundOperand> arguments)
    {
        if (functionName != "MEDIAN" || MedianParityIsRunTime(functionName, arguments)) return false;
        long count = 0;
        foreach (var a in arguments)
            count += a is BoundFieldOperand { Place: TableAllPlace all } ? all.StaticCount!.Value : 1;
        return count % 2 == 0;
    }

    /// <summary>Is the count of arguments of a MEDIAN a RUN-TIME fact (kb/Work PB4827)? True when an argument is a table(ALL) with no
    /// bind-time element count (<see cref="TableAllPlace.StaticCount"/> null: an OCCURS DEPENDING or dynamic-capacity range), whose
    /// number of elements is "as if each table element associated with that subscript position were specified" (§15.3 14)) at the
    /// time of the call. Whether the value is a selection (§15.61.4 1)) or the mean of the middle pair (§15.61.4 2)) is then decided
    /// at run time, by the body, and not by the lane the binder or renderer picks.</summary>
    internal static bool MedianParityIsRunTime(string functionName, IReadOnlyList<BoundOperand> arguments) =>
        functionName == "MEDIAN"
        && arguments.Any(a => a is BoundFieldOperand { Place: TableAllPlace { StaticCount: null } });

    /// <summary>The functions <see cref="ReturnsExactParse"/> answers true for, as a set so that
    /// <c>ReceiverShapeReaderDriftTests</c> can hold each member's runtime body against the renderer (no <c>RenderNum</c>
    /// arm of its own, an SDIDI arm in <c>RenderDec</c>) instead of restating the list.</summary>
    internal static readonly FrozenSet<string> ExactParseFunctions = new[] { "NUMVAL", "NUMVAL-C", "NUMVAL-F" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>An exact scaled operand: its unscaled digits (<c>NumX.Digits</c>, 0 when it has no bound of its own: an
    /// intrinsic's value, a windowed or image-decoded read), its scale and whether it is a table(ALL) argument, which stands for an
    /// unknown number of elements. <paramref name="RunTimeScaled"/> marks a whole-value operand (<c>NumX.Exact</c>: a nested call's
    /// exact value, kb/Work PB4860) whose scale only the run time knows, so the alignment is taken to shift it.</summary>
    internal readonly record struct AlignedOperand(int Digits, int Scale, bool TableAll, bool RunTimeScaled = false)
    {
        /// <summary>Is the operand's digit bound unknown (<c>NumX.Digits</c> 0)? It is then bounded by its 16-byte carrier's
        /// <see cref="PicInfo.CarrierDigitBound"/> digits, as <c>NumericRenderer.DigitBound</c> bounds it (kb/Work PB4752).</summary>
        internal bool Unbounded => Digits == 0;
    }

    /// <summary>Does a cross-aligning call have an argument of unknown bound (<see cref="AlignedOperand.Unbounded"/>) while the
    /// alignment shifts any argument (kb/Work PB4792)? Then the aligned operand is bounded only by its 16-byte carrier's
    /// <see cref="PicInfo.CarrierDigitBound"/> = 39 digits, past the Int128's 38, whatever the shift is, and the native arm's alignment
    /// would end the run unit with EC-SIZE-OVERFLOW for a value its receiver may hold. Such a list is NOT taken by the SDIDI's
    /// 34-digit image (which would round a 36-digit sum the Int128 lane held whole, review R1-1): the renderer evaluates it on
    /// the exact family's whole value (<c>CobolIntrinsics.SumWhole</c> and siblings), the function arm of <c>CombineAdditive</c>'s
    /// exact additive. With NO shift the alignment is the identity and only the sum can leave the carrier, the documented size
    /// error (DOC-A.1-179 (1)): the native arm keeps that list. THE ONE READER of the question, asked by
    /// <see cref="AlignmentMayLeaveCarrier"/> and by <c>IntrinsicRenderer</c>.</summary>
    internal static bool AlignmentShiftsUnboundedOperand(string functionName, IReadOnlyList<AlignedOperand?> operands)
    {
        if (functionName is "MOD" or "REM" || operands.Count == 0 || operands.Any(x => x is null)) return false;
        var xs = operands.Select(x => x!.Value).ToList();
        int s = xs.Max(x => x.Scale);
        return xs.Any(x => x.Unbounded) && (xs.Any(x => x.Scale != s) || xs.Any(x => x.RunTimeScaled));
    }

    /// <summary>Can aligning a cross-aligning call's arguments to their common scale — and accumulating the aligned values —
    /// leave the <see cref="Int128"/> carrier? True only when EVERY argument is an exact scaled operand
    /// (<paramref name="operands"/>: <c>null</c> for an SDIDI or binary64 operand, which takes another lane and for which nothing is
    /// provable) and either one argument's aligned bound alone, or the sum of the magnitudes the function adds, each bounded by its
    /// aligned digits, exceeds <see cref="Int128.MaxValue"/>; or when an argument has no bound of its own
    /// (<see cref="AlignedOperand.Unbounded"/>) and the alignment shifts any argument (kb/Work PB4792). THE ONE READER of the question: <c>IntrinsicRenderer.RenderNum</c> asks it of the
    /// RAW operands it is about to render, and <c>FunctionValueLane</c>'s prediction asks it of the same facts read from the bound tree, so
    /// the carrier a nested SUM / RANGE / MEDIAN / MIDRANGE answers on is predicted by the rule that picks it.</summary>
    /// <remarks>
    /// <para>Which magnitudes are added is a property of the FUNCTION: SUM adds every argument (a table(ALL)
    /// argument, whose run-time count is unknown, is charged 9 more digits); RANGE subtracts two; MEDIAN and
    /// MIDRANGE add two and halve through ×10; MOD and REM align a pair and return one of them.</para>
    /// <para>The aligned value's own digits are an UPPER bound by construction: an argument's unscaled digits plus
    /// the scale gap it is shifted by.</para></remarks>
    internal static bool AlignmentMayLeaveCarrier(string functionName, IReadOnlyList<AlignedOperand?> operands)
    {
        // ⛔ MOD / REM ARE NOT ROUTED. Their value is a DIFFERENCE of nearly equal magnitudes: MOD(9E30, 0.12345678) is
        // 0.1194939, which needs 39 digits of the dividend, and the SDIDI body keeps 34 — it would answer an
        // approximation SILENTLY where the native arm refuses LOUDLY (EC-SIZE-OVERFLOW at the carrier). The summing
        // family's value, in contrast, is as large as its arguments, so every digit a receiver can store (≤ 31) lies
        // inside the SDIDI's 34. (PB621 names SUM, RANGE, MEAN, MEDIAN and MIDRANGE only; MEAN left this family with PB3860.)
        if (functionName is "MOD" or "REM") return false;
        if (operands.Count == 0 || operands.Any(x => x is null)) return false;
        var xs = operands.Select(x => x!.Value).ToList();
        int s = xs.Max(x => x.Scale);
        // ⛔ AN ARGUMENT OF UNKNOWN BOUND LEAVES THE CARRIER WHEN THE ALIGNMENT SHIFTS ANY ARGUMENT (kb/Work PB4792, the function arm of
        // PB4752's NumericRenderer.CombineAdditive). Such an argument (FUNCTION INTEGER (D), a windowed read) is bounded by its
        // 16-byte carrier's PicInfo.CarrierDigitBound = 39 digits, past the Int128's 38 whatever the shift is, and the native arm's
        // alignment ended the run unit (EC-SIZE-OVERFLOW) for a value its receiver holds: SUM (FUNCTION INTEGER (D) F) of
        // 1000000000000000001 and 1.01. With NO shift the alignment is the identity and only the sum can leave the carrier, the
        // documented size error (DOC-A.1-179 (1)); the native arm keeps that list, as it did.
        if (xs.Any(x => x.Unbounded)) return AlignmentShiftsUnboundedOperand(functionName, operands);
        // ⛔ THE BOUND IS ON THE VALUE, NOT ON THE WIDEST ARGUMENT PLUS A DIGIT COUNT. Argument i, aligned to the common
        // scale, is below 10^D_i (D_i its unscaled digits plus the scale gap — a table(ALL) argument stands for an
        // unknown number of elements, so it is charged nine more digits); the function's value is bounded by the sum of
        // the magnitudes it adds: every argument for SUM, the two largest for RANGE, and those two times ten
        // for MEDIAN and MIDRANGE (added, then halved through ×10). `9(29)` beside `V9(9)` is 38 + 9 digits of two
        // values whose sum is 1.0000000010e38 — the Int128 carrier holds it, and the old "widest + digits of n"
        // allowance sent it to the SDIDI, which keeps 34 digits (kb/Work PB621's review follow-up).
        var bounds = xs.Select(x => Pow10Bound(x.Digits + (s - x.Scale) + (x.TableAll ? 9 : 0))).ToList();
        // ⛔ AN ARGUMENT WHOSE ALIGNED BOUND IS PAST UInt128 MAY LEAVE THE CARRIER BY ITSELF, whatever the function adds: the
        // native arm aligns EVERY argument (AlignedArgs), so that one alignment already escapes. It is answered here, before
        // the largest terms are chosen, because an unbounded term is NOT the largest to an ordering: OrderByDescending puts
        // null LAST, so RANGE(BIG FR TH) with BIG a 31-digit item and FR scale 18 took the two BOUNDED terms, proved a fit
        // and kept the native arm, which raised EC-SIZE-OVERFLOW for a value its receiver holds (kb/Work PB4370, found on the
        // way; PB621's witness passed only because each of its legs had two such arguments).
        if (bounds.Any(t => t is null)) return true;
        var terms = bounds.Select(t => t!.Value).OrderByDescending(t => t).ToList();
        UInt128? bound = functionName switch
        {
            "SUM" => SumOf(terms),
            "RANGE" => SumOf(terms.Take(2)),
            "MEDIAN" or "MIDRANGE" => SumOf(terms.Take(2)) is { } two && two <= UInt128.MaxValue / 10 ? two * 10 : null,
            _ => throw new UnreachableException(
                $"AlignmentMayLeaveCarrier was asked of FUNCTION {functionName}, which is not in CrossAligningFunctions"),
        };
        return bound is not { } b || b > (UInt128)Int128.MaxValue;

        // 10^digits − 1 — the largest magnitude of `digits` digits; null once it is past UInt128 (never fits).
        static UInt128? Pow10Bound(int digits)
        {
            if (digits is < 0 or > 38) return null;
            UInt128 p = 1;
            for (int i = 0; i < digits; i++) p *= 10;
            return p - 1;
        }
        // The sum of the terms; null once it is past UInt128 (never fits).
        static UInt128? SumOf(IEnumerable<UInt128> terms)
        {
            UInt128 total = 0;
            foreach (var v in terms)
            {
                if (total > UInt128.MaxValue - v) return null;
                total += v;
            }
            return total;
        }
    }
}
