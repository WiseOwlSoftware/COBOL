// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;

namespace CobolNet.CodeGen;

/// <summary>
/// The typed, <c>nameof</c>-anchored façade over the emitted runtime surface (P7 Step 4b;
/// DESIGN-codegen-backend §3): every C# fragment that names a runtime member routes through here, so a runtime
/// rename breaks THIS file at compile time instead of silently mis-emitting text. Migration is INCREMENTAL by
/// design (the doc's shrinking-whitelist plan): the ratchet guard test
/// (<c>tests/Cobol.Net.Tests.Characterization/RuntimeApiGuardTests.cs</c>) pins each CodeGen file's bare
/// <c>Cobol*.</c> count and fails on any INCREASE — Step 9's per-verb rewrites drive the counts to zero, at
/// which point the whitelist empties and the guard flips to forbid-all. Type-name anchors land first (a runtime
/// TYPE rename already breaks here); member anchors accrete per migrated file.
/// </summary>
internal static class RuntimeApi
{
    // ── Type-name anchors (each is a compile-time reference to the runtime type). ──
    public static string Bool => nameof(CobolBool);

    // ── The CALL-site exception carrier (ISO §14.9.4.4 GR3h/GR3i; kb/Work PB233). Its two classification
    //    predicates are asked in BOTH directions, and both route here so the runtime member is named once:
    //    at COMPILE time to split the statement's enabled name set into catch arms, and as EMITTED TEXT in the
    //    arm that must test the raised name at run time. ──

    /// <summary>ISO §14.9.4.4 GR3h item 1's family partition — "if the exception condition is any of the
    /// EC-PROGRAM or EC-EXTERNAL exception conditions" — asked at COMPILE time over one enabled name.</summary>
    public static bool CallEcIsProgramOrExternal(string ec) => CobolCallException.IsProgramOrExternal(ec);

    /// <summary>The EMITTED form of the same predicate, over a caught exception's <c>EcName</c> — the arm that
    /// takes item 1's families when checking for the raised name is NOT enabled.</summary>
    public static string CallEcIsProgramOrExternalText(string ecNameExpr) =>
        $"{nameof(CobolCallException)}.{nameof(CobolCallException.IsProgramOrExternal)}({ecNameExpr})";

    /// <summary>Can a <see cref="CobolCallException"/> actually raise <paramref name="ec"/>? A COMPILE-time
    /// question: an enabled name with no raise site on this carrier would contribute a catch-filter disjunct
    /// that can never be true (<see cref="CobolCallException.CarriedNames"/>).</summary>
    public static bool CallEcIsCarried(string ec) => CobolCallException.CanCarry(ec);

    /// <summary>Boolean NOT — ISO §8.8.4.5 boolean expressions (the D-B1 '0'/'1' string substrate).</summary>
    public static string BoolNot(string operand) => $"{nameof(CobolBool)}.{nameof(CobolBool.Not)}({operand})";

    /// <summary>The alphabet-name class condition's membership test (ISO §8.8.4.4.4 GR3 a; kb/Work PB109):
    /// <c>CobolClass.IsInCodedSet(arg, CobolClass.CodedSetKind.&lt;kind&gt;)</c>.</summary>
    public static string ClassInCodedSet(string arg, string kind) =>
        $"{nameof(CobolClass)}.{nameof(CobolClass.IsInCodedSet)}({arg}, {nameof(CobolClass)}.{nameof(CobolClass.CodedSetKind)}.{kind})";

    /// <summary>A boolean dyadic op (AND/OR/XOR/EXCLUSIVE-OR family) — <c>CobolBool.{method}(l, r)</c>.
    /// <paramref name="method"/> is the runtime method NAME (validated by the anchors below at compile time
    /// via <see cref="BoolOpName"/>).</summary>
    public static string BoolOp(string method, string l, string r) => $"{nameof(CobolBool)}.{method}({l}, {r})";

    /// <summary>The literal-optimized variant <c>CobolBool.{method}All(operand, bits)</c>.</summary>
    public static string BoolOpAll(string method, string operand, string bitsLiteral) =>
        $"{nameof(CobolBool)}.{method}All({operand}, {bitsLiteral})";

    /// <summary>Compile-time anchor for the boolean dyadic method names the binder selects: renaming
    /// <c>CobolBool.And/Or/Xor</c> breaks this member, not the emitted text.</summary>
    public static string BoolOpName(char op) => op switch
    {
        '|' => nameof(CobolBool.Or),
        '^' => nameof(CobolBool.Xor),
        _ => nameof(CobolBool.And),   // '&' and the (unreachable) default — the pre-4b table's shape
    };

    /// <summary>A boolean shift/rotate (ISO §8.8.2 rule 8, 2023) — <c>CobolBool.Shift{Left|Right}[Circular](v, k)</c>.</summary>
    public static string BoolShift(CobolNet.Binding.Bound.BoolShiftKind kind, string operand, string count) => kind switch
    {
        CobolNet.Binding.Bound.BoolShiftKind.Left => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeft)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.Right => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRight)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.LeftCircular => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeftCircular)}({operand}, {count})",
        _ => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRightCircular)}({operand}, {count})",
    };

    // ── Numeric (CobolNum) ──

    /// <summary>Decode a zoned/separate-sign DISPLAY image per the receiver's profile — <c>CobolNum.ParseDisplay</c>.</summary>
    public static string NumParseDisplay(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseDisplay)}({image}, {profile})";

    /// <summary>An unsigned integer's DISPLAY image zero-padded to a fixed digit width —
    /// <c>CobolNum.FormatUnsignedDisplay</c> (the ACCEPT temporal conceptual-item image, ISO §14.9.1.4 GR7–GR12).</summary>
    public static string NumFormatUnsignedDisplay(string value, int digits) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatUnsignedDisplay)}({value}, {digits})";

    /// <summary>The text image of an intrinsic FUNCTION's returned value (ISO §15.4 temporary item;
    /// characteristics implementor-defined under native arithmetic, §15.4.1) — <c>CobolNum.FormatFunctionText</c>.
    /// The literal form, so a folded intrinsic and a computed one are indistinguishable (DA2).</summary>
    /// <paramref name="deSign"/> carries §14.9.25.4 GR6a (the operational sign is not moved to an alphanumeric
    /// receiver / text comparison) — the same flag <c>FieldAsString</c> honours for a signed FIELD operand.
    public static string NumFormatFunctionText(string value, int scale, bool deSign = false) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatFunctionText)}({value}, {scale}{(deSign ? ", true" : "")})";

    /// <summary>The same text image for a STANDARD-DECIMAL intermediate — <c>CobolDec.ToFunctionText</c>.</summary>
    public static string DecFunctionText(string value, bool deSign = false) =>
        $"({value}).{nameof(CobolDec.ToFunctionText)}({(deSign ? "true" : "")})";

    /// <summary>The numeric MOVE-rules store (decimal alignment, truncation/zero-fill) — <c>CobolNum.Store</c>,
    /// or <c>CobolNum.StoreU</c> when the VALUE expression is on the unsigned-wide lane (<c>NumX.U</c> — a
    /// 16-byte unsigned COMP-5 read or the HIGHEST-ALGEBRAIC fold literal, kb/Work R10). The lane is picked by
    /// NAME, never by overload: an int constant converts implicitly to both Int128 and UInt128, so a same-name
    /// pair makes every <c>Store(0, …)</c>-shaped emission a CS0121 ambiguity.</summary>
    public static string NumStore(string value, string scale, string profile, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.StoreU) : nameof(CobolNum.Store))}({value}, {scale}, {profile})";

    /// <summary>Render an unscaled value as the receiver's DISPLAY image — <c>CobolNum.FormatDisplay</c>
    /// (<c>FormatDisplayU</c> for a UInt128-carrier read — see <see cref="NumStore"/> on lane-by-name).
    /// ⛔ This is the CHARACTER rendering (what a DISPLAY statement shows). For the bytes an item occupies in a
    /// record, a file, a SORT key or a REDEFINES backing, use <see cref="NumFormatImage"/> — for a BINARY or
    /// PACKED item the two differ, and that difference is V59.</summary>
    public static string NumFormatDisplay(string value, string profile, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.FormatDisplayU) : nameof(CobolNum.FormatDisplay))}({value}, {profile})";

    /// <summary>Encode an unscaled value as the BYTES the item occupies at a byte boundary — the record/group
    /// image, a file record, a SORT key window, a Tier-B REDEFINES backing (<c>CobolNum.FormatImage</c>,
    /// COBOLNET_DESIGN §14.4). Zoned items render exactly as <see cref="NumFormatDisplay"/>; BINARY and PACKED
    /// render their radix-2 / BCD bytes.</summary>
    public static string NumFormatImage(string value, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatImage)}({value}, {profile})";

    /// <summary>Decode an item's record-image bytes back to its unscaled value — <c>CobolNum.ParseImage</c>, the
    /// inverse of <see cref="NumFormatImage"/>.
    /// <para>⛔ <paramref name="sending"/> HAS NO DEFAULT, deliberately (the "caller names the landing" shape
    /// <see cref="FloatToScaled"/> uses): a windowed decode is either a SENDING reference of the item's content,
    /// which ISO §14.6.13.2 rule 2 makes EC-DATA-INCOMPATIBLE-checkable, or it is not — and a new call site that
    /// silently inherited "not" is precisely how rule 2 came to have no raise site at all (kb/Work PB230).
    /// <c>true</c> emits <c>CobolNum.ParseImageSending</c>, which tests the content against the numeric class
    /// condition under checking and is otherwise identical.</para></summary>
    public static string NumParseImage(string image, string profile, bool sending) =>
        $"{nameof(CobolNum)}.{(sending ? nameof(CobolNum.ParseImageSending) : nameof(CobolNum.ParseImage))}({image}, {profile})";

    /// <summary>The table SORT (ISO §14.9.40 Format 2) — <c>CobolTable.Sorted(elements, comparison)</c>: the
    /// stable <c>OrderBy</c> §14.9.40.4 GR19c/GR3c want, with the framework array sort's comparer-exception
    /// wrapper undone so a key comparison's fatal COBOL exception condition still reaches the statement guard
    /// (kb/Work PB230).</summary>
    public static string TableSorted(string elements, string comparison) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.Sorted)}({elements}, {comparison})";

    /// <summary>The checked read of a BOOLEAN sending operand — <c>CobolBool.Sending(value)</c>: raises the fatal
    /// EC-DATA-INCOMPATIBLE for content that is not all <c>'0'</c>/<c>'1'</c> under checking (ISO §14.6.13.2
    /// rule 1), else returns the value. The boolean twin of <see cref="FloatSending"/>.</summary>
    public static string BoolSending(string value) =>
        $"{nameof(CobolBool)}.{nameof(CobolBool.Sending)}({value})";

    /// <summary>⛔ THE ONE NUMERIC CLASS CONDITION over a numeric item's stored image —
    /// <c>CobolNum.IsNumericImage</c> (ISO §8.8.4.4.4 GR3 n)1, keyed on the item's byte form). Emitted by the class
    /// condition itself, and called from inside <see cref="NumParseImage"/>'s checked lane, because §14.6.13.2
    /// rule 2 defines its own test BY REFERENCE to this one — one rule, one place (kb/Work PB230).</summary>
    public static string NumIsNumericImage(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.IsNumericImage)}({image}, {profile})";

    /// <summary>The rule-2 checked sending read on the STRING channel — <c>CobolNum.SendingImage</c>: a ZONED
    /// window is handed on VERBATIM (its stored image is its text), having first been tested against the numeric
    /// class condition under checking. <paramref name="sending"/> false is the raw read, for an exempt context
    /// (§14.6.13.2 rule 2's class-condition and VALIDATE dashes — <see cref="Emit.SendingRef"/>).</summary>
    public static string NumSendingImage(string image, string profile, bool sending) =>
        sending ? $"{nameof(CobolNum)}.{nameof(CobolNum.SendingImage)}({image}, {profile})" : image;

    /// <summary>The FLOAT decode lane (kb/Work PB164 wave 2) — <c>CobolNum.ParseImageFloat</c>, the IEEE bit
    /// reinterpretation (the Int128 lane would numerically CONVERT).</summary>
    public static string NumParseImageFloat(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseImageFloat)}({image}, {profile})";

    /// <summary>The UNSIGNED decode twins (the Step D arm-1 dissolution) — a ulong/UInt128-carried window
    /// decodes to its container value, bit-identically through the signed lane.
    /// <para>⚠ The 8-byte (<c>ulong</c>) half has NO emitter caller: <c>ParseBinaryImage</c> already returns an
    /// unsigned item's full width as a non-negative <c>Int128</c>, so <c>NumericRenderer</c>'s generic
    /// StoreAsImage arm decodes a <c>PIC 9(10..18) COMP-5</c> window identically and the extra arm was pure
    /// duplication (kb/Work PB164, the Step D review). Kept as the named half of the runtime's unsigned lane
    /// pair, NOT as live drift — the 16-byte twin below is the one an emitter picks.</para></summary>
    public static string NumParseImageU(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseImageU)}({image}, {profile})";

    /// <inheritdoc cref="NumParseImageU"/>
    /// <param name="sending">As <see cref="NumParseImage"/> — no default, so a new site states whether it is a
    /// §14.6.13.2 rule 2 sending reference.</param>
    public static string NumParseImageU128(string image, string profile, bool sending) =>
        $"{nameof(CobolNum)}.{(sending ? nameof(CobolNum.ParseImageU128Sending) : nameof(CobolNum.ParseImageU128))}({image}, {profile})";

    /// <summary>The FLOAT encode lane (kb/Work PB164 wave 2) — <c>CobolNum.FormatImageFloat</c>, distinctly
    /// named because FormatImage overloads on a float would make integer call sites ambiguous.</summary>
    public static string NumFormatImageFloat(string value, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatImageFloat)}({value}, {profile})";

    // ── Strings (CobolString) ──

    /// <summary>The alphanumeric MOVE-rules store (left-justify, right space-fill/truncate) —
    /// <c>CobolString.Store</c>.</summary>
    public static string StrStore(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width})";

    /// <summary>The DYNAMIC LENGTH receiving store (ISO §8.5.1.10.4 — replace, truncate on the right to the LIMIT,
    /// NO padding) — <c>CobolDynString.Store</c>. <paramref name="limit"/> is the LIMIT character count, or "-1"
    /// for the implementor-defined maximum (no explicit LIMIT phrase).</summary>
    public static string DynStore(string value, string limit) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.Store)}({value}, {limit})";

    /// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16) — set the current length of a dynamic-length
    /// item, space-filling grown positions (GR39); the negative→0 (GR37) and clamp-to-maximum (GR38) legs set the
    /// nonfatal EC-STORAGE-NOT-AVAIL when <paramref name="checkStorage"/> is <c>true</c>. <paramref name="newLen"/> is
    /// the arithmetic-expression-5 value at FULL precision (a <c>double</c>) so the GR37 sign test precedes the
    /// toward-zero truncation — <c>CobolDynString.SetSize(current, newLen, limit, check)</c>.</summary>
    public static string DynSetSize(string current, string newLen, string limit, string checkStorage) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.SetSize)}({current}, {newLen}, {limit}, {checkStorage})";

    /// <summary>CONTINUE AFTER n SECONDS (ISO §14.9.9) — the timed pause; a negative interval sets the nonfatal
    /// EC-CONTINUE-LESS-THAN-ZERO when checking is enabled — <c>CobolTiming.ContinueAfter(seconds, check)</c>.</summary>
    public static string ContinueAfter(string seconds, string checkLessThanZero) =>
        $"{nameof(CobolTiming)}.{nameof(CobolTiming.ContinueAfter)}({seconds}, {checkLessThanZero})";

    /// <summary>The exact-lane suspension (kb/Work PB138) — the sign value at full precision beside the
    /// exactly-truncated seconds: <c>CobolTiming.ContinueAfterExact(full, truncated, check)</c>.</summary>
    public static string ContinueAfterExact(string fullForSign, string truncatedSeconds, string checkLessThanZero) =>
        $"{nameof(CobolTiming)}.{nameof(CobolTiming.ContinueAfterExact)}({fullForSign}, {truncatedSeconds}, {checkLessThanZero})";

    /// <summary>Set the run-unit termination status passed to the OS as the process exit code (ISO §14.9.42.4 GR5 /
    /// §14.9.18.4 GR10) — <c>RunUnit.SetExitStatus(status)</c>.</summary>
    public static string SetExitStatus(string status) =>
        $"{nameof(RunUnit)}.{nameof(RunUnit.SetExitStatus)}({status})";

    // ── Editing (CobolEdit) ──

    /// <summary>Edit a numeric value into a PICTURE mask — <c>CobolEdit.Format</c>. <paramref name="cfgArgs"/> is
    /// the per-item editing-config suffix (<see cref="Emit.EmitContext.EditCfg"/>), possibly empty.</summary>
    public static string EditFormat(string value, string scale, string maskLiteral, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.Format)}({value}, {scale}, {maskLiteral}{cfgArgs})";

    /// <summary>THE edited MOVE-semantics store keyed on the receiver's picture (data-model design D21 / kb/Work PB66 —
    /// the form dispatch lives HERE, never at a call site): a floating-point numeric-edited receiver takes
    /// <c>CobolEdit.FormatFloatMove</c> over the sender's exact form (an unscaled Int128 + scale, a CobolDec, or a
    /// binary64 — §14.9.25.4 GR6 item 4: overflow → EC-DATA-OVERFLOW + the pinned saturated image, underflow → zero);
    /// a fixed-point one the classic <c>CobolEdit.Format</c> over the value ALIGNED at the mask's scale
    /// (<paramref name="alignedFixed"/> — the caller's rescale, which a floating-point form never needs).</summary>
    public static string EditFormatFor(PicInfo pic, Emit.NumX value, string alignedFixed, string alignedScale, string cfgArgs)
    {
        // A format-2 (LOCALE) receiver edits through CobolLocaleEdit (§13.18.40.5 r9–r15; PB64 T6) — the arm
        // sits FIRST because a locale item has NO EditMask and the deref below is reachable-null for it. Its
        // cfgArgs carry at most the blankWhenZero: flag — EmitContext.EditCfg produces "" for a locale item
        // (no currencyString:, no commaMode: — §13.18.40.5 r9 / §12.3.7.4 GR14) and EditsArg "" (no EDITING).
        if (pic.LocaleEdit is { } le)
            return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.Format)}({alignedFixed}, {alignedScale}, "
                + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}{cfgArgs})";
        if (!pic.IsFloatEdited) return EditFormat(alignedFixed, alignedScale, Emit.EmitText.CsLiteral(pic.EditMask!), cfgArgs);
        string mask = Emit.EmitText.CsLiteral(pic.EditMask!);
        return value.Real || value.Dec
            ? $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatFloatMove)}({value.Expr}, {mask}{cfgArgs})"
            : $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatFloatMove)}({value.Expr}, {value.Scale}, {mask}{cfgArgs})";
    }

    /// <summary>A locale-name reference rendered for a runtime call: the L1-normalized tag as a string literal,
    /// or <c>null</c> for the current-locale form (the runtime resolves the category's current locale at use —
    /// §13.18.40.5 r11 / §14.6.6 r6). The ONE renderer of a <see cref="Binding.Model.LocaleRef"/> argument.</summary>
    public static string LocaleTagArg(Binding.Model.LocaleRef locale) =>
        locale.Tag is { } t ? Emit.EmitText.CsLiteral(t) : "null";

    /// <summary>The format-2 (LOCALE) receiver's ARITHMETIC store (§14.7.5 — false = the size error condition,
    /// receiver unchanged; the capacity is the picture's integer digit positions, DISTINCT from EC-LOCALE-SIZE,
    /// which is §13.18.40.5 r14 b's character-truncation condition inside the edit itself):
    /// <c>CobolLocaleEdit.TryFormat</c>. The EditTryFormatFloat shape.</summary>
    public static string EditTryFormatLocale(PicInfo pic, string alignedFixed, string alignedScale, string imgVar, string cfgArgs)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.TryFormat)}({alignedFixed}, {alignedScale}, "
            + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}, out var {imgVar}{cfgArgs})";
    }

    /// <summary>The ONE PicInfo-keyed receiver-scale rule (kb/Work PB64 T6 — it was written twice, in
    /// <c>MoveEmitter.SenderContext</c> and <c>ArithmeticEmitter.ScaleOf</c>, and both copies silently fell to
    /// <c>pic.Scale</c> = 0 for a locale item, truncating a fractional sender): a floating-point edited receiver
    /// has no fixed scale (0 — the caller's form dispatch never uses it); a format-2 (LOCALE) receiver's scale is
    /// the picture's digits right of '.' (<see cref="PicInfo.Scale"/> — the analyzer set it; there is no mask);
    /// a masked numeric-edited receiver's is the MASK's; everything else <see cref="PicInfo.Scale"/>.</summary>
    public static int ReceiverScaleOf(PicInfo pic, bool commaMode) =>
        pic.LocaleEdit is not null ? pic.Scale
        // A float-edited receiver rides the mask arm too — its significand scale drives the working scale of
        // an intermediate landing (measured: returning 0 here flipped a DIVIDE quotient golden to 0.00000E+00).
        : pic is { Category: PicCategory.NumericEdited, EditMask: { } m } ? MaskScale(m, '$', commaMode)
        : pic.Scale;

    /// <summary>A format-2 (LOCALE) sender's DE-EDIT read (§14.9.25.4 GR5/GR6 d over §14.6.13.2 r4) — the
    /// <c>CobolLocaleEdit.DeEdit</c> call under the locale current NOW; the scale is the picture's.</summary>
    public static string LocaleDeEdit(PicInfo pic, string read, bool blankWhenZero)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.DeEdit)}({read}, {Emit.EmitText.CsLiteral(le.Picture)}, "
            + $"{LocaleTagArg(le.Locale)}{(blankWhenZero ? ", blankWhenZero: true" : "")})";
    }

    /// <summary>A format-2 (LOCALE) item's VALUE-clause initializer — a RUNTIME <c>CobolLocaleEdit.Format</c>
    /// call, never a baked image: §13.18.40.5 r11 + §14.6.6 r6 make the locale the one current AT THE TIME of
    /// editing, so no compile-time image exists. The ONE producer for the field initializer, the group-image
    /// composer and the level-88 membership value.</summary>
    public static string LocaleEditCompose(PicInfo pic, Int128 unscaled, int scale, bool blankWhenZero)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.Format)}({Emit.EmitText.IntLiteral(unscaled.ToString())}, {scale}, "
            + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}{(blankWhenZero ? ", blankWhenZero: true" : "")})";
    }

    /// <summary>The CORRECTLY-ROUNDED scaled-value→double conversion — <c>CobolFloat.ScaledToDouble</c> (kb/Work
    /// PB115; the ONE conversion <c>NumericRenderer.Real</c> emits for a scaled float-lane argument).</summary>
    public static string ScaledToDouble(string unscaled, int scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.ScaledToDouble)}({unscaled}, {scale})";

    /// <summary>The floating-point form's ARITHMETIC store (§14.7.5 cases 3/4 — false = the size error condition,
    /// receiver unchanged): <c>CobolEdit.TryFormatFloat</c> over the result's exact form.</summary>
    public static string EditTryFormatFloat(PicInfo pic, Emit.NumX value, string imgVar, string cfgArgs)
    {
        string mask = Emit.EmitText.CsLiteral(pic.EditMask!);
        return value.Real || value.Dec
            ? $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormatFloat)}({value.Expr}, {mask}, out var {imgVar}{cfgArgs})"
            : $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormatFloat)}({value.Expr}, {value.Scale}, {mask}, out var {imgVar}{cfgArgs})";
    }

    /// <summary>A floating-point literal as the EXACT standard-decimal operand (ISO §8.8.1.5.2 r1 — the literal's
    /// value, significand × 10^exponent, lifted through the ONE range-checking funnel <c>CobolDec.FromParsed</c>; a
    /// 35/36-digit significand rounds to decimal128's 34 under the intermediate rounding mode). Under STANDARD-DECIMAL
    /// arithmetic this replaces the binary64 form a floating literal takes natively (D16), so a 20-digit significand or
    /// a 4-digit exponent reaches the intermediate exactly (kb/Work PB99).</summary>
    public static string DecFromParsedLiteral(Int128 sig, int exp10, string modeExpr) =>
        $"CobolDec.FromParsed({Emit.EmitText.IntLiteral(sig.ToString())}, {exp10}, {modeExpr})";

    /// <summary>The compile-time floating-point edited image of a VALUE literal (<c>CobolEdit.FormatFloatMove</c> at
    /// compile time — the same runtime, so the baked initial content is what a MOVE of the literal would store).</summary>
    public static string EditComposeFloat(Int128 sig, int exp10, string picture, bool blankWhenZero, bool commaMode) =>
        CobolEdit.FormatFloatMove(new CobolDec(sig, exp10), picture, blankWhenZero, commaMode);

    /// <summary>The trailing <c>edits:</c> named argument for a numeric-edited store carrying PICTURE EDITING
    /// phrases (ISO §13.18.40.2 Format 1) — the resolved single-character render rules serialized as a
    /// <c>CobolEdit.EditRule[]</c>. Empty for every non-editing item, so the generated code of an ordinary program
    /// is byte-identical. Appended AFTER <c>BwzFlag</c>/<c>EditCfg</c> (all named args) at each edited store.</summary>
    public static string EditsArg(IReadOnlyList<CobolEdit.EditRule>? rules)
    {
        if (rules is null || rules.Count == 0) return "";
        static string Ch(char c) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote: true);
        string items = string.Join(", ", rules.Select(r =>
            $"new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}({Ch(r.Char1)}, {Ch(r.Neg)}, {Ch(r.Pos)})"));
        return $", edits: new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}[] {{ {items} }}";
    }

    /// <summary>Place sending characters into an alphanumeric-edited mask's positions (ISO §13.18.40 insertion) —
    /// <c>CobolEdit.FormatAlphanumeric</c>.</summary>
    public static string EditFormatAlphanumeric(string value, string maskLiteral) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatAlphanumeric)}({value}, {maskLiteral})";

    /// <summary>Decode a digit image's magnitude (non-digits contribute no digit) — <c>CobolNum.FromAlphanumeric</c>.</summary>
    public static string NumFromAlphanumeric(string image) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FromAlphanumeric)}({image})";

    /// <summary>Rescale an unscaled value between fraction scales under a rounding mode — <c>CobolNum.Rescale</c>,
    /// or the size-error-latching <c>CobolNum.RescaleChecked</c> when <paramref name="checkedPath"/>.</summary>
    /// <summary>The §15.3 integer-argument landing (PB22) — <c>CobolIntrinsics.IntegerArg</c>, which RAISES on a
    /// value outside the <c>long</c> range instead of letting an unchecked cast wrap it past the function's own
    /// range guard. <paramref name="real"/> selects the double-typed twin (distinct name, not an overload — an
    /// integer literal converts to both carriers and would be a CS0121 ambiguity).</summary>
    public static string IntegerArg(string value, bool real = false) =>
        $"{nameof(CobolIntrinsics)}.{(real ? nameof(CobolIntrinsics.IntegerArgReal) : nameof(CobolIntrinsics.IntegerArg))}({value})";

    /// <summary>The WIDE (Int128) floating-point integer-argument intake — <c>CobolIntrinsics.IntegerArgWideReal</c>,
    /// for the one §15 integer argument whose domain exceeds <c>long</c> (BOOLEAN-OF-INTEGER argument-1, PB65).
    /// A non-float wide operand needs no intake at all — Int128 is the lane's own carrier.</summary>
    public static string IntegerArgWide(string value) =>
        $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.IntegerArgWideReal)}({value})";

    /// <summary>The runtime scale-37 codomain-maximum constant for a bounded float-family function (PB65 /
    /// RV-15.75.4-1) — consumed by <c>CobolIntrinsics.FromDoubleBounded</c>'s clamp.</summary>
    public static string CodomainConst(CobolNet.Binding.IntrinsicCodomain c) => c switch
    {
        CobolNet.Binding.IntrinsicCodomain.UnitOpen => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainBelowOne37)}",
        CobolNet.Binding.IntrinsicCodomain.HalfPi => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainHalfPi37)}",
        _ => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainPi37)}",
    };

    public static string NumRescale(string value, string fromScale, string toScale, CobolRounding mode, bool checkedPath = false) =>
        $"{nameof(CobolNum)}.{(checkedPath ? nameof(CobolNum.RescaleChecked) : nameof(CobolNum.Rescale))}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>The §14.9.12 GR6c/GR7 scaled division — <c>CobolNum.Divide</c>, or the size-error-throwing
    /// <c>CobolNum.DivideOrThrow</c> under a checked context.</summary>
    public static string NumDivide(bool orThrow, string a, string aScale, string b, string bScale, string resultScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{(orThrow ? nameof(CobolNum.DivideOrThrow) : nameof(CobolNum.Divide))}({a}, {aScale}, {b}, {bScale}, {resultScale}, {RoundingText(mode)})";

    /// <summary>The checked numeric store — <c>CobolNum.TryStore</c> (false = capacity/PROHIBITED failure; the
    /// receiver stays unchanged, §14.7.5). <paramref name="argsFragment"/> is the pre-shaped value/scale/profile
    /// argument run (fixed, Real-landed, or SDIDI overload); <paramref name="u"/> picks the unsigned-wide
    /// <c>TryStoreU</c> lane by NAME (see <see cref="NumStore"/>).</summary>
    public static string NumTryStore(string argsFragment, CobolRounding mode, string outVar, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.TryStoreU) : nameof(CobolNum.TryStore))}({argsFragment}, {RoundingText(mode)}, out var {outVar})";

    /// <summary>The unchecked rounded store — the <c>CobolNum.Store</c> overload taking a rounding mode
    /// (<c>StoreU</c> on the unsigned-wide lane — see <see cref="NumStore"/>).</summary>
    public static string NumStoreRounded(string argsFragment, CobolRounding mode, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.StoreU) : nameof(CobolNum.Store))}({argsFragment}, {RoundingText(mode)})";

    /// <summary>The unsigned-wide → Int128 funnel — <c>CobolNum.Widen</c> (kb/Work R10: loud beyond the
    /// documented native intermediate, never a silent wrap).</summary>
    public static string NumWiden(string value) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Widen)}({value})";

    /// <summary>An algebraic-value comparison with an unsigned-wide side — <c>CobolNum.CompareU</c> (kb/Work
    /// R10). The overload set covers U-vs-U and either mixed order; operands are passed with their own scales.</summary>
    public static string NumCompareU(string a, string aScale, string b, string bScale) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.CompareU)}({a}, {aScale}, {b}, {bScale})";

    /// <summary>The Int128-lane exact non-widening comparison at each operand's own scale —
    /// <c>CobolNum.Compare</c> (fix-queue PB65: the common-scale alignment wrapped at 39 aligned digits).</summary>
    public static string NumCompareScaled(string a, string aScale, string b, string bScale) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Compare)}({a}, {aScale}, {b}, {bScale})";

    /// <summary>The escape-CHECKED widening rescale — <c>CobolNum.RescaleEscape</c> (fix-queue PB65: a
    /// value-semantics alignment past the Int128 intermediate is the size-error condition, never a wrap).</summary>
    public static string NumRescaleEscape(string value, string fromScale, string toScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.RescaleEscape)}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>An SDIDI intermediate landed to an unscaled value — the instance <c>CobolDec.ToUnscaled</c> (the
    /// UNCHECKED §14.7 final transfer: MOVE, alignment, an arithmetic store with no size-error checking).</summary>
    public static string DecToUnscaled(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaled)}({scale}, {RoundingText(mode)})";

    /// <summary>The SIZE-ERROR-CHECKED sibling — <c>CobolDec.ToUnscaledChecked</c> (kb/Work PB74): a magnitude past
    /// the Int128 carrier raises <c>CobolSizeError</c> EC-SIZE-TRUNCATION for the statement's ON SIZE ERROR /
    /// EC-SIZE machinery instead of returning the low-order digits. The checked numeric-EDITED transfer in
    /// <c>ArithmeticEmitter.StoreArith</c> rides this; the numeric receiver's <c>TryStore(CobolDec)</c> calls it directly.</summary>
    public static string DecToUnscaledChecked(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaledChecked)}({scale}, {RoundingText(mode)})";

    /// <summary>The INTERMEDIATE landing — <c>CobolDec.ToUnscaledIntermediate</c> (kb/Work PB69): an SDIDI value
    /// entering the Int128 carrier as an argument, an arithmetic operand, a subscript … A magnitude the carrier
    /// cannot hold at the scale raises EC-SIZE-OVERFLOW (§14.7.5 case 5 — the implementor-defined intermediate
    /// range IS checked, A.1 item 179), never the modular low-order digits.</summary>
    /// <summary>The algebraic sign of an SDIDI intermediate as an <c>int</c> (−1/0/+1) — <c>Int128.Sign</c> over the
    /// significand, which carries the value's sign exactly at every exponent (a sign condition over a
    /// STANDARD-DECIMAL expression, or over a native integer power — kb/Work PB84, NIST NC250A).</summary>
    /// <summary>Is a data-address / pointer expression the NULL pointer? The runtime's null is the
    /// <c>ManagedPointer.Null</c> SINGLETON, never a C# null — a bare <c>is null</c> test is always false on it (kb/Work
    /// PB80: the LENGTH r4a association guard read an unallocated BASED entry as associated).</summary>
    public static string PtrIsNull(string ptrExpr) => $"({ptrExpr} is null || {ptrExpr}.{nameof(ManagedPointer.IsNull)})";

    public static string DecSign(string decExpr) =>
        $"{nameof(Int128)}.{nameof(Int128.Sign)}(({decExpr}).{nameof(CobolDec.Sig)})";

    public static string DecToUnscaledIntermediate(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaledIntermediate)}({scale}, {RoundingText(mode)})";

    /// <summary>SDIDI exponentiation (ISO §8.8.1.5.4; P10 Step 12) — <c>CobolDec.Pow</c>. <paramref name="mode"/>
    /// is the pre-rendered INTERMEDIATE ROUNDING fragment (<c>CobolRounding.X</c>).</summary>
    public static string DecPow(string baseOperand, string expOperand, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Pow)}({baseOperand}, {expOperand}, {mode})";

    /// <summary>The exact §15.27.3 r3 FUNCTION E constant under a standard mode — <c>CobolDec.E</c> (kb/Work R18).</summary>
    public static string DecE => $"{nameof(CobolDec)}.{nameof(CobolDec.E)}";

    /// <summary>The exact §15.73.3 r3 FUNCTION PI constant under a standard mode — <c>CobolDec.Pi</c> (kb/Work R18).</summary>
    public static string DecPi => $"{nameof(CobolDec)}.{nameof(CobolDec.Pi)}";

    /// <summary>An exact fixed-point value lifted into SDIDI form — <c>CobolDec.From(unscaled, scale)</c>.</summary>
    public static string DecFrom(string unscaled, string scale) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.From)}({unscaled}, {scale})";

    /// <summary>The §8.8.1.5.1 implementor-defined float→SDIDI operand conversion — <c>CobolDec.FromDouble</c>
    /// (the shortest round-trip decimal identity of the IEEE value; P10 Step 12).</summary>
    public static string DecFromDouble(string doubleExpr) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.FromDouble)}({doubleExpr})";

    /// <summary>One SDIDI division of two exactly-lifted fixed-point values — MEAN's §15.60.4 equivalent-expression
    /// division under standard-decimal arithmetic (§15.4.1 r1; P10 Step 12).</summary>
    public static string DecDivLifted(string numerator, string numeratorScale, string denominator, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Div)}({nameof(CobolDec)}.{nameof(CobolDec.From)}({numerator}, "
        + $"{numeratorScale}), {nameof(CobolDec)}.{nameof(CobolDec.From)}({denominator}, 0), {mode})";

    /// <summary>A float value's inexactness probe at a fraction scale (the ROUNDED PROHIBITED gate, §14.7.5 r7)
    /// — <c>CobolFloat.InexactAtScale</c>.</summary>
    public static string FloatInexactAtScale(string value, string scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.InexactAtScale)}({value}, {scale})";

    /// <summary>The capacity-checked edited format — <c>CobolEdit.TryFormat</c> (false = the aligned value's
    /// significant digits exceed the mask, §14.7.5 case 3).</summary>
    public static string EditTryFormat(string value, string scale, string maskLiteral, string imgVar, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormat)}({value}, {scale}, {maskLiteral}, out var {imgVar}{cfgArgs})";

    /// <summary>Resize a boolean value to the receiver's GR3 width — <c>CobolBool.Resize</c>.</summary>
    public static string BoolResize(string value, string width) =>
        $"{nameof(CobolBool)}.{nameof(CobolBool.Resize)}({value}, {width})";

    /// <summary>Alphanumeric/national THROUGH-range membership under the effective collating sequence —
    /// <c>CobolString.ThruMember(read, lo, hi{collate})</c>: sets the nonfatal EC-RANGE-INVALID and returns false when
    /// <c>lo</c> collates after <c>hi</c> (§14.7.8 rule 2), else the inclusive bound test. <paramref name="collate"/>
    /// is the trailing collating-arg fragment (empty for the default, <c>, __COLLATE</c> / <c>, __COLLATE_NAT</c>
    /// otherwise) selecting the matching <c>Compare</c> overload.</summary>
    public static string ThruMember(string read, string lo, string hi, string collate) =>
        $"{nameof(CobolString)}.{nameof(CobolString.ThruMember)}({read}, {lo}, {hi}{collate})";

    /// <summary>The emitted-text reference to a <see cref="CobolRounding"/> value — <c>nameof</c>-anchored so a
    /// member rename breaks HERE, never the generated text.</summary>
    public static string RoundingText(CobolRounding mode) => $"{nameof(CobolRounding)}.{mode}";

    /// <summary>A floating-point intermediate landed to a scaled integer at a target fraction scale — the CHECKED
    /// <c>CobolFloat.ToScaled</c> (saturates past the carrier so a capacity check raises — an ON SIZE ERROR / EC-SIZE
    /// store, an intermediate consumer) or the UNCHECKED <c>CobolFloat.ToScaledUnchecked</c> (the low-order digits —
    /// a MOVE, §14.6.8.2 r4; the no-phrase store; INVOKE BY CONTENT). The caller names the LANDING (kb/Work PB77) —
    /// there is no default, so a new site has to say which store it is.</summary>
    public static string FloatToScaled(string value, string scale, CobolRounding mode, bool checkedLanding) =>
        $"{nameof(CobolFloat)}.{(checkedLanding ? nameof(CobolFloat.ToScaled) : nameof(CobolFloat.ToScaledUnchecked))}({value}, {scale}, {RoundingText(mode)})";

    /// <summary>The checked read of a standard-float SENDING operand — <c>CobolFloat.Sending(value)</c>: raises the
    /// fatal EC-DATA-NOT-FINITE for a NaN/±Infinity content under checking (ISO §14.6.13.2 item 3), else returns the
    /// value. Wrapped at both float read chokepoints (the numeric-value read and the string-image read); the exempt
    /// sites (class/sign condition, same-usage MOVE) emit the raw read instead.</summary>
    public static string FloatSending(string value) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.Sending)}({value})";

    /// <summary>A float value's DISPLAY image — <c>CobolFloat.Display(value)</c> (invariant-culture shortest
    /// round-trip, §14.9.11 GR1 implementor-defined).</summary>
    public static string FloatDisplay(string value) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.Display)}({value})";

    /// <summary>The checked store of a MOVE algebraic value into a SINGLE-precision float receiver —
    /// <c>CobolFloat.StoreSingleChecked(src)</c>: raises the fatal EC-DATA-OVERFLOW when a finite source overflows to
    /// ±Infinity under checking (ISO §14.9.25.4 GR6 d)4.a), else returns the cast value.</summary>
    public static string FloatStoreSingleChecked(string src) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.StoreSingleChecked)}({src})";

    /// <summary>The checked store of a STANDARD-DECIMAL MOVE algebraic value into a float receiver —
    /// <c>CobolFloat.StoreChecked(dec, single)</c>. The SDIDI is handed over WHOLE, because the range test is on
    /// the algebraic value and a <c>ToDouble</c> would have already collapsed it to ±Infinity (ISO §14.9.25.4
    /// GR6 d)4.a; kb/Work PB271).</summary>
    public static string FloatStoreDecChecked(string dec, bool single) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.StoreChecked)}({dec}, {(single ? "true" : "false")})";

    /// <summary>The BOOLEAN-receiver store (§14.6.8.6 — boolean-ZERO pad, explicit justification) —
    /// <c>CobolString.Store</c> with <c>pad: '0'</c>.</summary>
    public static string StrStoreBoolean(string value, string width, bool justifiedRight) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, " +
        $"justifiedRight: {(justifiedRight ? "true" : "false")}, pad: '0')";

    // ── INSPECT (CobolInspect; ISO §14.9.22) ──

    /// <summary>The tallying pass — <c>CobolInspect.Tally</c>. Array-literal fragments are pre-rendered by the caller.</summary>
    public static string InspectTally(string image, string kinds, string pats, string befs, string afts, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Tally)}({image}, new int[] {{ {kinds} }}, " +
        $"new string?[] {{ {pats} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward})";

    /// <summary>The replacing pass — <c>CobolInspect.Replace</c>.</summary>
    public static string InspectReplace(string image, string kinds, string pats, string reps, string befs, string afts, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Replace)}({image}, new int[] {{ {kinds} }}, new string?[] {{ {pats} }}, " +
        $"new string?[] {{ {reps} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward})";

    /// <summary>CONVERTING — <c>CobolInspect.Convert</c>.</summary>
    public static string InspectConvert(string image, string from, string to, string before, string after, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Convert)}({image}, {from}, {to}, {before}, {after}, {backward})";

    /// <summary>Compile-time anchor for the tally-kind discriminators the emitter selects.</summary>
    public static string InspectTallyKindText(Binding.Bound.InspectTallyKind k) => k switch
    {
        Binding.Bound.InspectTallyKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyAll)}",
        Binding.Bound.InspectTallyKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyCharacters)}",
    };

    /// <summary>Compile-time anchor for the replace-kind discriminators the emitter selects.</summary>
    public static string InspectReplaceKindText(Binding.Bound.InspectReplaceKind k) => k switch
    {
        Binding.Bound.InspectReplaceKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceAll)}",
        Binding.Bound.InspectReplaceKind.First => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceFirst)}",
        Binding.Bound.InspectReplaceKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceCharacters)}",
    };

    // ── STRING / UNSTRING (CobolStringOps; ISO §14.9.43 / §14.9.48) ──

    /// <summary>One sending operand's transfer into the STRING working image — <c>CobolStringOps.StringTransfer</c>
    /// (advances the pointer, latches the overflow flag by ref).</summary>
    public static string StrTransfer(string acc, string src, string delim, string ptrVar, string ovfVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.StringTransfer)}({acc}, {src}, {delim}, ref {ptrVar}, ref {ovfVar})";

    /// <summary>One receiving area's extraction — <c>CobolStringOps.UnstringExtract</c> (out-vars for the examined
    /// field and the matched delimiter; −1 = not acted upon).</summary>
    public static string UnstringExtract(string src, string dels, string alls, string noDelimSize,
        string ptrVar, string fldVar, string dlmVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.UnstringExtract)}({src}, {dels}, {alls}, {noDelimSize}, " +
        $"ref {ptrVar}, out var {fldVar}, out var {dlmVar})";

    /// <summary>The JUSTIFIED-right alphanumeric store (§14.9.25.4 GR6c) — <c>CobolString.Store</c> with
    /// <c>justifiedRight: true</c>.</summary>
    public static string StrStoreJustified(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, justifiedRight: true)";

    /// <summary>The ONE alignment dispatch for a character store (§14.9.25.4 GR6/GR6c via §13.18.32): a
    /// JUSTIFIED receiver right-justifies (left space-fill / left truncation), otherwise left-justified with
    /// right space-fill / right truncation. Every emitter storing a character image into an
    /// alphanumeric/national receiver routes here — MOVE, STRING, the INVOKE formal copy-back, ACCEPT
    /// temporal (kb/Work PB139's one-rule-one-place extraction of six hand-rolled ternaries).</summary>
    public static string StrStoreAligned(string value, string width, bool justified) =>
        justified ? StrStoreJustified(value, width) : StrStore(value, width);

    // ── Pointers (CobolPtr; ISO §14.9.39 F7/F10, §14.9.3, §14.9.15) ──

    /// <summary>Displace a pointer by n character positions — <c>CobolPtr.UpBy</c> (GR18 null trap inside).</summary>
    public static string PtrUpBy(string ptr, string amount) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpBy)}({ptr}, {amount})";

    /// <summary>Displace by a SCALED amount — <c>CobolPtr.UpByScaled</c> (the GR19 divisibility test).</summary>
    public static string PtrUpByScaled(string ptr, string amount, string scale) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpByScaled)}({ptr}, {amount}, {scale})";

    /// <summary>ALLOCATE a fresh cell — <c>CobolPtr.Allocate</c> (GR1/GR2; GR6 zero fill).</summary>
    /// <summary>ALLOCATE — <c>CobolPtr.Allocate</c> over the FULL Int128 size (no emitter-side narrowing —
    /// the PB22 wrap family), with the GR6/GR8 fill character and the GR5 not-available out-flag.</summary>
    public static string PtrAllocate(string sizeInt128, string fillCharLiteral, string notAvailVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Allocate)}({sizeInt128}, {fillCharLiteral}, out {notAvailVar})";

    /// <summary>ALLOCATE with a native-float expression — <c>CobolPtr.AllocateReal</c> (GR1's round-UP on
    /// the double; kb/Work PB151).</summary>
    public static string PtrAllocateReal(string sizeDouble, string fillCharLiteral, string notAvailVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.AllocateReal)}({sizeDouble}, {fillCharLiteral}, out {notAvailVar})";

    /// <summary>SET pointer UP/DOWN BY a native-float amount — <c>CobolPtr.UpByReal</c> (GR19's integrality
    /// test on the double; kb/Work PB151).</summary>
    public static string PtrUpByReal(string ptr, string amount) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpByReal)}({ptr}, {amount})";

    /// <summary>FREE a pointer's cell — <c>CobolPtr.Free</c> (three-way per GR1; not-alloc out-flag).</summary>
    public static string PtrFree(string ptr, string notAllocVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Free)}({ptr}, out {notAllocVar})";

    // ── More strings / tables ──

    /// <summary>A reference-modification slice — <c>CobolString.RefMod</c> (1-based start, length).
    /// <paramref name="allowZeroLength"/> (the REF-MOD-ZERO-LENGTH directive, §7.3.23) emits the named argument only
    /// when true, so every existing site stays byte-identical.</summary>
    public static string StrRefMod(string s, string start, string len, bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.RefMod)}({s}, {start}, {len}{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>The OMITTED-length ref-mod sentinel (<c>identifier(start:)</c> "to the end") as an emit expression —
    /// routed through the façade (the P7 Step 4b ratchet) so a rename of the runtime const breaks HERE at compile time.
    /// Distinct from −1 so a specified negative length raises EC-BOUND-REF-MOD (review C14).</summary>
    public static string OmittedRefModLength => $"{nameof(CobolString)}.{nameof(CobolString.OmittedRefModLength)}";

    // The rendered ref-mod positions are `long`-valued COBOL expressions and the runtime takes `int`, so each is
    // cast at the call site; an omitted length renders the distinct sentinel above rather than −1. Both rules live
    // HERE and nowhere else: a ref-mod attaches to a storage PLACE (PlaceRenderer, readable and writable) and to a
    // ref-modified FUNCTION RESULT (IntrinsicRenderer, read-only — §8.4.3.3.3 SR2), and the two must agree.

    /// <summary>The runtime <c>int</c> leftmost-position from a rendered start expression (§8.4.3.3.4 item 5b).</summary>
    public static string RefModStart(string renderedStart) => $"(int)({renderedStart})";

    /// <summary>The runtime <c>int</c> length from a rendered length expression, or the OMITTED sentinel when the
    /// "to the end" form was written (§8.4.3.3.4 item 5c).</summary>
    public static string RefModLength(string? renderedLength) =>
        renderedLength is null ? OmittedRefModLength : $"(int)({renderedLength})";

    /// <summary>A reference-modification slice over an already-rendered VALUE, from the model's
    /// <see cref="RefModSpec"/>. The value form has no splice counterpart: §8.4.3.2.3 SR1 makes a
    /// function-identifier a non-receiving operand, so a ref-modified function result is read-only.</summary>
    public static string StrRefMod(string s, RefModSpec rm) =>
        StrRefMod(s, RefModStart(rm.Start), RefModLength(rm.Length), rm.AllowZeroLength);

    /// <summary>Splice <paramref name="rhs"/> into <paramref name="s"/> at a 1-based start/length, preserving the
    /// rest of the width — <c>CobolString.SpliceInto</c>. <paramref name="pad"/> is the optional fill-char argument
    /// (a C# <c>char</c> literal, e.g. boolean-zero <c>'0'</c>); null emits the default space fill.</summary>
    public static string StrSpliceInto(string s, string start, string len, string rhs, string? pad = null,
        bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.SpliceInto)}({s}, {start}, {len}, {rhs}"
        + $"{(pad is null ? "" : $", pad: {pad}")}{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>The three-way alphanumeric comparison — <c>CobolString.Compare</c>. <paramref name="weightsArg"/>
    /// is the trailing collation argument (", __COLLATE" — the program's CobolCollation carrier), possibly empty.</summary>
    public static string StrCompare(string a, string b, string weightsArg) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Compare)}({a}, {b}{weightsArg})";

    /// <summary>An OCCURS-DEPENDING current count read — <c>CobolTable.Occ</c>.</summary>
    public static string TableOcc(string expr) => $"{nameof(CobolTable)}.{nameof(CobolTable.Occ)}({expr})";

    /// <summary>A FIXED OCCURS element access — the ref-returning <c>CobolTable.At(path, oneBasedIndex)</c>
    /// (ISO §8.4.2.3.4 GR2 — a benign out-of-range occurrence, subscript-checking off in COBOL-85).</summary>
    public static string TableAt(string path, string oneBasedIndex) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.At)}({path}, {oneBasedIndex})";

    /// <summary>The current POSITION extent of an occurs-depending GROUP operand (ISO §13.18.38 GR8) — the fixed
    /// prefix plus data-name-1's clamped value × the element width — <c>CobolTable.OdoExtent</c>. The unit is the
    /// group's own (bit positions for a subtree holding USAGE BIT leaves, character positions otherwise; kb/Work
    /// PB173) — the CHARACTER channel uses <see cref="TableOdoExtentChars"/>.</summary>
    public static string TableOdoExtent(string occ, int minOccurs, int maxOccurs, int fixedUnits, int elemUnits) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.OdoExtent)}({occ}, {minOccurs}, {maxOccurs}, {fixedUnits}, {elemUnits})";

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand — <see cref="TableOdoExtent"/>
    /// rounded up to whole characters (<c>CobolTable.OdoExtentChars</c>). Identity when the positions ARE characters,
    /// so the image channel has ONE arm rather than a units branch (kb/Work PB173).</summary>
    public static string TableOdoExtentChars(
        string occ, int minOccurs, int maxOccurs, int fixedUnits, int elemUnits, int positionsPerChar) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.OdoExtentChars)}"
        + $"({occ}, {minOccurs}, {maxOccurs}, {fixedUnits}, {elemUnits}, {positionsPerChar})";

    /// <summary>A table(ALL) intrinsic argument's enumeration (ISO §15.3; kb/Work PB62) — <c>CobolTable.AllArgs&lt;T&gt;</c>
    /// over one range lambda per ALL level (each <c>Func&lt;long[], long&gt;</c>, the index vector in) and the element
    /// lambda; yields the <c>T[]</c> a <c>params T[]</c> body binds to.</summary>
    public static string TableAllArgs(string csType, IEnumerable<string> countLambdas, string elementLambda) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.AllArgs)}<{csType}>(new Func<long[], long>[] {{ {string.Join(", ", countLambdas)} }}, {elementLambda})";

    /// <summary>The intrinsic argument list assembled from written operands and enumerations, in source order —
    /// <c>CobolTable.ArgConcat&lt;T&gt;</c>.</summary>
    public static string TableArgConcat(string csType, IEnumerable<string> parts) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.ArgConcat)}<{csType}>({string.Join(", ", parts)})";

    /// <summary>Bind an evaluated value to a name inside an expression — <c>CobolTable.With(value, name => body)</c>
    /// (an enumerated argument list read twice: MEAN's sum and count, a leading positional argument and its tail).</summary>
    public static string With(string value, string name, string body) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.With)}({value}, {name} => {body})";

    // ── Keyed file I/O (CobolFile; ISO §14.9.10/.30/.35/.41/.51) ──

    /// <summary>Register a RELATIVE connector — <c>CobolFile.RegisterRelative</c>. <paramref name="varyArgs"/> is
    /// the optional trailing ", min, max" record-bounds fragment (§13.18.43 GR9/GR10), possibly empty.</summary>
    public static string FileRegisterRelative(string name, string assign, int width, string optional, int access, int keyDigits, string varyArgs, string? selectName = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterRelative)}({name}, {assign}, {width}, {optional}, {access}, {keyDigits}{varyArgs}{SelectNameArg(selectName)})";

    /// <summary>The SELECT-spelled file-name (ISO §15.28.4 r1c/r2b — kb/Work PB63) as the registration's trailing
    /// named argument; empty when the caller has none.</summary>
    private static string SelectNameArg(string? selectName) => selectName is null ? "" : $", selectName: {selectName}";

    /// <summary>Register an INDEXED connector — <c>CobolFile.RegisterIndexed</c> (prime-key window per §12.4.5.12,
    /// plus the optional §12.4.5.7 prime-key collating sequence — a CobolCollation expression; <paramref name="weights"/>
    /// is "null" for native, emitted as a named argument so a no-clause file's registration is byte-identical to the
    /// pre-clause engine).</summary>
    public static string FileRegisterIndexed(string name, string assign, int width, string optional, int access, string pkOffset, int pkWidth, string varyArgs, string weights = "null", string? selectName = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterIndexed)}({name}, {assign}, {width}, {optional}, {access}, {pkOffset}, {pkWidth}{varyArgs}{(weights == "null" ? "" : $", primeCollation: {weights}")}{SelectNameArg(selectName)})";

    /// <summary>Register one ALTERNATE RECORD KEY window (§12.4.5.6) — <c>CobolFile.AddAlternateKey</c>, with its
    /// optional §12.4.5.7 collating weights and §12.4.5.6.4 GR6 SUPPRESS WHEN value ("null" = absent, each emitted
    /// as a named argument so a plain alternate key's registration is unchanged).</summary>
    public static string FileAddAlternateKey(string name, string offset, int width, string dups, string weights = "null", string suppress = "null") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.AddAlternateKey)}({name}, {offset}, {width}, {dups}{(weights == "null" ? "" : $", collation: {weights}")}{(suppress == "null" ? "" : $", suppress: {suppress}")})";

    /// <summary>Position a relative connector to the RELATIVE KEY item's RRN — <c>CobolFile.SetRelativeKey</c>.</summary>
    public static string FileSetRelativeKey(string name, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.SetRelativeKey)}({name}, {rrn})";

    /// <summary>Sequential-forward keyed READ — <c>CobolFile.ReadKeyedNext</c> (status result, out image).</summary>
    public static string FileReadKeyedNext(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyedNext)}({name}, out var {imgVar})";

    /// <summary>READ PREVIOUS — <c>CobolFile.ReadKeyedPrevious</c>.</summary>
    public static string FileReadKeyedPrevious(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyedPrevious)}({name}, out var {imgVar})";

    /// <summary>Random keyed READ by key-of-reference — <c>CobolFile.ReadKeyed</c>.</summary>
    public static string FileReadKeyed(string name, int keyIndex, string keyImage, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyed)}({name}, {keyIndex}, {keyImage}, out var {imgVar})";

    /// <summary>The §9.1.16 record-lock governance adjustment of a just-read status — <c>CobolFile.ReadLockGovern</c>.</summary>
    public static string FileReadLockGovern(string name, string status, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadLockGovern)}({name}, {status}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Sequential-organization governed READ (§9.1.16 / §14.9.30 GR9–GR12/GR22) — <c>CobolFile.ReadShared</c>
    /// (bool result, out image — the same contract as the plain <c>FileRead</c>).</summary>
    public static string FileReadShared(string name, string lockRef, string advancingOnLock, string retryKind,
        string retryAmount, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadShared)}({name}, {lockRef}, {advancingOnLock}, {retryKind}, {retryAmount}, out var {imgVar})";

    /// <summary>Governed WRITE for a sharing-active file, any organization (§14.9.51 GR10/GR11) — <c>CobolFile.WriteShared</c>.</summary>
    public static string FileWriteShared(string name, string image, string lenArg, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Governed REWRITE for a sharing-active file, any organization (§14.9.35 GR11/GR12) — <c>CobolFile.RewriteShared</c>.</summary>
    public static string FileRewriteShared(string name, string image, string lenArg, string lockRef, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RewriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount})";

    /// <summary>Governed DELETE RECORD for a sharing-active file (§14.9.10 GR6/GR7) — <c>CobolFile.DeleteShared</c>.</summary>
    public static string FileDeleteShared(string name, string areaImage, string retryKind, string retryAmount) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteShared)}({name}, {areaImage}, {retryKind}, {retryAmount})";

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 — the '62' re-attempt) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFileRetry(string name, string retryKind, string retryAmount, string overridden) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name}, {retryKind}, {retryAmount}, {overridden})";

    /// <summary>The connector's current relative slot number — <c>CobolFile.RelativeSlot</c>.</summary>
    public static string FileRelativeSlot(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RelativeSlot)}({name})";

    /// <summary>Keyed WRITE — <c>CobolFile.WriteKeyed</c>; <paramref name="lenArg"/> = the optional §13.18.43
    /// GR13a varying-length argument (null when fixed).</summary>
    public static string FileWriteKeyed(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteKeyed)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>Keyed REWRITE — <c>CobolFile.RewriteKeyed</c>.</summary>
    public static string FileRewriteKeyed(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RewriteKeyed)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>DELETE RECORD — <c>CobolFile.DeleteRecord</c> (key sliced from the record-area image, GR3/GR8).</summary>
    public static string FileDeleteRecord(string name, string areaImage) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteRecord)}({name}, {areaImage})";

    /// <summary>DELETE FILE (Format 2, every organization) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFile(string name, string overridden) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name}, {overridden})";

    /// <summary>START FIRST/LAST — <c>CobolFile.StartFirstLast</c>.</summary>
    public static string FileStartFirstLast(string name, string last) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartFirstLast)}({name}, {last})";

    /// <summary>START on a relative file (§14.9.41 GR9/GR10 — numeric RRN comparison) — <c>CobolFile.StartRelative</c>.</summary>
    public static string FileStartRelative(string name, string opLiteral, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartRelative)}({name}, {opLiteral}, {rrn})";

    /// <summary>START on an indexed file (§14.9.41 GR17 — leftmost-LENGTH key comparison) — <c>CobolFile.StartIndexed</c>.</summary>
    public static string FileStartIndexed(string name, int keyIndex, string opLiteral, string operandImage, string len) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartIndexed)}({name}, {keyIndex}, {opLiteral}, {operandImage}, {len})";

    /// <summary>Implicit OPEN INPUT (SORT GR12a / MERGE GR7a) — <c>CobolFile.OpenInput</c>.</summary>
    public static string FileOpenInput(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenInput)}({name})";

    /// <summary>Implicit OPEN OUTPUT (SORT GR15a) — <c>CobolFile.OpenOutput</c>.</summary>
    public static string FileOpenOutput(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenOutput)}({name})";

    /// <summary>CLOSE a connector — <c>CobolFile.Close</c>.</summary>
    public static string FileClose(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Close)}({name})";

    /// <summary>The IMPLICIT close (§14.9.5 GR9 — only a connector "that is open") — <c>CobolFile.CloseIfOpen</c>.</summary>
    public static string FileCloseIfOpen(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.CloseIfOpen)}({name})";

    /// <summary>Sequential READ into an out-image (the implicit USING loop shape) — <c>CobolFile.Read</c>.</summary>
    public static string FileRead(string name, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Read)}({name}, out var {imgVar})";

    /// <summary>Sequential WRITE without optional phrases (the implicit GIVING loop shape) — <c>CobolFile.Write</c>.</summary>
    public static string FileWrite(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Write)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>Register a SEQUENTIAL/LINE-SEQUENTIAL connector — <c>CobolFile.Register</c>.
    /// <paramref name="varyArgs"/> is the optional trailing ", min, max" bounds fragment.</summary>
    public static string FileRegister(string name, string assign, string width, string lineSeq, string optional, string varyArgs = "", string? selectName = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Register)}({name}, {assign}, {width}, {lineSeq}, {optional}{varyArgs}{SelectNameArg(selectName)})";

    /// <summary>Register a LINAGE file's logical-page evaluator closure (§13.18.34 GR6) — <c>CobolFile.SetLinage</c>.</summary>
    public static string FileSetLinage(string name, string closure) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.SetLinage)}({name}, {closure})";

    /// <summary>Mark a connector sharing-active (Phase 4d M2-FILE-1) — <c>CobolFile.RegisterSharing</c>.</summary>
    public static string FileRegisterSharing(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterSharing)}({name}, {argsFragment})";

    /// <summary>The sharing-governed OPEN (Table 19 → status 61) — <c>CobolFile.OpenShared</c>.</summary>
    public static string FileOpenShared(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenShared)}({name}, {argsFragment})";

    /// <summary>The mode-specific plain OPEN — anchored over <c>CobolFile.Open{Input,Output,Extend,IO}</c>.</summary>
    public static string FileOpen(string name, Binding.Bound.BoundOpenMode mode) =>
        $"{nameof(CobolFile)}.{mode switch
        {
            Binding.Bound.BoundOpenMode.Output => nameof(CobolFile.OpenOutput),
            Binding.Bound.BoundOpenMode.Extend => nameof(CobolFile.OpenExtend),
            Binding.Bound.BoundOpenMode.IO => nameof(CobolFile.OpenIO),
            _ => nameof(CobolFile.OpenInput),
        }}({name})";

    /// <summary>The written-form CLOSE entry — one per Table 14 row (§14.9.6.4 GR3) plus WITH LOCK, anchored
    /// over <c>CobolFile.Close{,WithLock,ReelUnit,ReelUnitForRemoval,NoRewind}</c>. The runtime resolves the
    /// row against the file's §14.9.6.4 GR2 category; the emitter names the FORM the program wrote.</summary>
    public static string FileClose(string name, Binding.Bound.BoundCloseKind kind) =>
        $"{nameof(CobolFile)}.{kind switch
        {
            Binding.Bound.BoundCloseKind.WithLock => nameof(CobolFile.CloseWithLock),
            Binding.Bound.BoundCloseKind.ReelUnit => nameof(CobolFile.CloseReelUnit),
            Binding.Bound.BoundCloseKind.ReelUnitForRemoval => nameof(CobolFile.CloseReelUnitForRemoval),
            Binding.Bound.BoundCloseKind.NoRewind => nameof(CobolFile.CloseNoRewind),
            Binding.Bound.BoundCloseKind.Normal => nameof(CobolFile.Close),
            // ⛔ NOT a silent fall-through to the plain CLOSE. A written form with no entry here would emit a
            // Table 14 row the program did not write — the exact shape that let REEL/UNIT FOR REMOVAL ride
            // REEL/UNIT's entry for two waves (kb/Work PB235). Only an out-of-range cast reaches this.
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no CobolFile CLOSE entry for this written form"),
        }}({name})";

    /// <summary>UNLOCK — <c>CobolFile.Unlock</c> (records flag = UNLOCK RECORDS).</summary>
    public static string FileUnlock(string name, string records) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Unlock)}({name}, {records})";

    /// <summary>WRITE … ADVANCING — <c>CobolFile.WriteAdvancing</c>.</summary>
    public static string FileWriteAdvancing(string name, string image, string lines, string before) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteAdvancing)}({name}, {image}, {lines}, {before})";

    /// <summary>WRITE … BEFORE ADVANCING n AFTER ADVANCING m (ISO §14.9.51, 2023) — <c>CobolFile.WriteBeforeAndAfter</c>.</summary>
    public static string FileWriteBeforeAndAfter(string name, string image, string beforeLines, string afterLines) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteBeforeAndAfter)}({name}, {image}, {beforeLines}, {afterLines})";

    /// <summary>The LINAGE end-of-page probe (§13.18.34) — <c>CobolFile.EndOfPage</c>.</summary>
    public static string FileEndOfPage(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.EndOfPage)}({name})";

    /// <summary>The connector's two-character I-O status — <c>CobolFile.Status</c>.</summary>
    public static string FileStatus(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Status)}({name})";

    /// <summary>The open-mode ordinal of a connector (the USE mode-scope switch) — <c>CobolFile.OpenModeOf</c>.</summary>
    public static string FileOpenModeOf(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenModeOf)}({name})";

    /// <summary>Sequential REWRITE — <c>CobolFile.Rewrite</c> (optional varying-length argument).</summary>
    public static string FileRewrite(string name, string image, string? lenArg = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Rewrite)}({name}, {image}{(lenArg is null ? "" : $", {lenArg}")})";

    /// <summary>The just-read record's frame length — <c>CobolFile.LastReadLength</c>.</summary>
    public static string FileLastReadLength(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.LastReadLength)}({name})";

    // ── SORT / MERGE (CobolSort; ISO §14.9.40 / §14.9.24) ──

    /// <summary>Initialize the per-SD image store — <c>CobolSort.Init</c> — with the statement's collating sequence,
    /// snapshotted at statement start (ISO §14.6.6 r5: a locale switch during the SORT/MERGE has no effect on it).</summary>
    public static string SortInit(string sd, string weights) => $"{nameof(CobolSort)}.{nameof(CobolSort.Init)}({sd}, {weights})";

    /// <summary>RELEASE one record image — <c>CobolSort.Release</c>.</summary>
    public static string SortRelease(string sd, string image) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Release)}({sd}, {image})";

    /// <summary>The sequence phase — <c>CobolSort.Sort</c> (stable; GR8).</summary>
    public static string SortSort(string sd, string keys, string weights, string dupsInOrder) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Sort)}({sd}, {keys}, {weights}, {dupsInOrder})";

    /// <summary>The k-way merge — <c>CobolSort.Merge</c> (GR4 — file order breaks ties).</summary>
    public static string SortMerge(string sd, string keys, string weights) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Merge)}({sd}, {keys}, {weights})";

    /// <summary>Open a new pre-sorted USING stream — <c>CobolSort.NextInput</c>.</summary>
    public static string SortNextInput(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.NextInput)}({sd})";

    /// <summary>Close the SD store — <c>CobolSort.Close</c>.</summary>
    public static string SortClose(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Close)}({sd})";

    /// <summary>Rewind the return cursor (each GIVING file gets the FULL result, GR15) — <c>CobolSort.Rewind</c>.</summary>
    public static string SortRewind(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Rewind)}({sd})";

    /// <summary>Pull the next record in key order — <c>CobolSort.Return</c> (bool, out image).</summary>
    public static string SortReturn(string sd, string imgVar) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Return)}({sd}, out var {imgVar})";

    /// <summary>The just-returned record's length (§13.18.43 GR15) — <c>CobolSort.LastReturnedLength</c>.</summary>
    public static string SortLastReturnedLength(string sd) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.LastReturnedLength)}({sd})";

    /// <summary>The <c>CobolSort.Key[]</c> array literal over per-key "new(…)" element fragments.</summary>
    public static string SortKeyArray(IEnumerable<string> keyElements) =>
        $"new {nameof(CobolSort)}.{nameof(CobolSort.Key)}[] {{ {string.Join(", ", keyElements)} }}";

    // ── Report Writer (CobolReport; ISO §13.14–§13.18) ──

    /// <summary>A space-filled report-line buffer — <c>CobolReport.NewLine</c>.</summary>
    public static string ReportNewLine(int width) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.NewLine)}({width})";

    /// <summary>Place a printable item's image at its COLUMN (§13.18.14) — <c>CobolReport.Place</c>.</summary>
    public static string ReportPlace(string lineVar, int column, string image) =>
        ReportPlace(lineVar, column.ToString(), image);

    /// <summary>The variable-column form of <see cref="ReportPlace(string,int,string)"/> — a relative (PLUS)
    /// COLUMN operand places against the line's horizontal counter (§13.18.14.4 GR8).</summary>
    public static string ReportPlace(string lineVar, string columnExpr, string image) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.Place)}({lineVar}, {columnExpr}, {image})";

    /// <summary>Decode a DISPLAY image back into a native numeric leaf, preserving unset positions from the
    /// current value — <c>CobolNum.StoreDisplay</c>.</summary>
    public static string NumStoreDisplay(string image, string profile, string current) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.StoreDisplay)}({image}, {profile}, {current})";

    /// <summary>Decode a spliced RECORD IMAGE back into a numeric leaf — <c>CobolNum.StoreImage</c>, the write
    /// half of <see cref="NumFormatImage"/> (the <paramref name="current"/> dummy selects the storage form's
    /// conversion, exactly as <see cref="NumStoreDisplay"/> does).</summary>
    public static string NumStoreImage(string image, string profile, string current) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.StoreImage)}({image}, {profile}, {current})";

    // ── Inter-program ABI (CobolArgAdapt / CobolPassMode; interprogram design D1/D2) — Step 9-final sweep ──

    /// <summary>A LINKAGE formal's numeric carrier adoption — <c>CobolArgAdapt.Num&lt;T&gt;</c> over the
    /// formal's OWN carrier type (kb/Work R12 — the cell type is the field type, so a wide or unsigned formal's
    /// carrier-typed reads compile and carry the full container range).</summary>
    public static string ArgAdaptNum(string args, int position, string profile, string scale, string carrier = "long") =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Num)}<{carrier}>({args}, {position}, {profile}, {scale})";

    /// <summary>A LINKAGE formal's text carrier adoption — <c>CobolArgAdapt.Text</c>.</summary>
    public static string ArgAdaptText(string args, int position, string width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Text)}({args}, {position}, {width})";

    // ── The VARIABLE-LENGTH GROUP boundary carrier (ISO §8.5.1.12; kb/Work PB204). A third crossing form
    //    beside the native cell and the character image, because a variable-length group has neither a fixed
    //    record window nor an invertible flat image — see CobolVarGroup.

    /// <summary>The C# type of the variable-length-group boundary carrier.</summary>
    public static string VarGroupType => nameof(CobolVarGroup);

    /// <summary>The empty carrier value (an unbound formal's seed).</summary>
    public static string VarGroupEmpty => $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Empty)}";

    /// <summary>A DETACHED variable-length carrier cell (BY CONTENT / BY VALUE, §14.2.3 GR9/GR10).</summary>
    public static string VarGroupCell(string value) => $"ManagedPointer<{VarGroupType}>.Cell({value})";

    /// <summary>An ALIASING variable-length carrier over the caller's storage (BY REFERENCE, §14.2.3 GR8).</summary>
    public static string VarGroupOverField(string get, string set) =>
        $"ManagedPointer<{VarGroupType}>.OverField(() => {get}, __v => {{ {set} }})";

    /// <summary>A LINKAGE formal's variable-length carrier adoption — <c>CobolArgAdapt.VarGroup</c>.</summary>
    public static string ArgAdaptVarGroup(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.VarGroup)}({args}, {position})";

    /// <summary>The BY VALUE / BY CONTENT twin — <c>CobolArgAdapt.VarGroupValue</c> (§14.2.3 GR9/GR10).</summary>
    public static string ArgAdaptVarGroupValue(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.VarGroupValue)}({args}, {position})";

    /// <summary>A BY VALUE numeric formal's DETACHED value-copy cell (ISO §14.2.3 GR10 — stores never reach
    /// the caller) — <c>CobolArgAdapt.NumValue&lt;T&gt;</c> over the formal's carrier (see
    /// <see cref="ArgAdaptNum"/>).</summary>
    public static string ArgAdaptNumValue(string args, int position, string profile, string scale, string carrier = "long") =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.NumValue)}<{carrier}>({args}, {position}, {profile}, {scale})";

    /// <summary>A BY VALUE image-carried formal's DETACHED value-copy cell (§14.2.3 GR10, image form) —
    /// <c>CobolArgAdapt.TextValue</c>.</summary>
    public static string ArgAdaptTextValue(string args, int position, string width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.TextValue)}({args}, {position}, {width})";

    /// <summary>The argument-present probe (OMITTED handling, §14.2.3) — <c>CobolArgAdapt.Present</c>.</summary>
    public static string ArgAdaptPresent(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Present)}({args}, {position})";

    /// <summary>RETURNING delivery into the caller's cell (§14.2.3 GR7) — <c>CobolArgAdapt.StoreReturn</c>.</summary>
    public static string ArgAdaptStoreReturn(string ret, string value) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.StoreReturn)}({ret}, {value})";

    /// <summary>The emitted-text reference to a <see cref="CobolPassMode"/> value — <c>nameof</c>-anchored like
    /// <see cref="RoundingText"/>, so a member rename breaks HERE, never the generated text.</summary>
    public static string PassModeText(CobolPassMode mode) => $"{nameof(CobolPassMode)}.{mode}";

    // ── Run-unit lifecycle (CobolFile) ──

    /// <summary>Run-unit file-subsystem init (the entry wrapper's Main) — <c>CobolFile.Init</c>. (The matching
    /// §14.6.11 run-unit-termination implicit CLOSE is runtime-side — <see cref="Runtime.ProgramTable.RunMain"/>'s
    /// finally — so a separately-compiled module's open files are closed even when this main group declares none.)</summary>
    public static string FileInit() => $"{nameof(CobolFile)}.{nameof(CobolFile.Init)}()";

    /// <summary>Mint a per-object instance-file connector key (§9.1.4) — <c>CobolFile.MintInstanceKey</c>.</summary>
    public static string FileMintInstanceKey(string baseKeyLiteral) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.MintInstanceKey)}({baseKeyLiteral})";

    // ── Pointers / objects ──

    /// <summary>Dereference a data-address pointer to its storage cell (GR3/GR4 loud) — <c>CobolPtr.Deref</c>.</summary>
    public static string PtrDeref(string ptr, string classWidth) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Deref)}({ptr}, {classWidth})";

    /// <summary>The INVOKE null-receiver guard (EC-OO-NULL, §14.9.23.4 GR5) — <c>CobolObject.RequireNonNull</c>.</summary>
    public static string ObjRequireNonNull(string receiver) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.RequireNonNull)}({receiver})";

    /// <summary>Normalize a runtime method-name value for universal dispatch (D-U6) —
    /// <c>CobolObject.NormalizeMethodName</c>.</summary>
    public static string ObjNormalizeMethodName(string nameExpr) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.NormalizeMethodName)}({nameExpr})";

    // ── USAGE BIT record images (ISO §13.18.60.4 GR5 / §8.5.1.6.3; design D19, fix-queue PB43) ──
    // The VALUE carrier stays a '0'/'1' string for both boolean usages; only USAGE BIT's IMAGE is packed.

    /// <summary>Pack a bit run's carrier into its packed record image — <c>CobolBits.Pack</c>.</summary>
    public static string BitsPack(string bitsExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Pack)}({bitsExpr}, {countExpr})";

    /// <summary>Pack every boolean position the carrier holds — <c>CobolBits.Pack(bits)</c>. For an operand
    /// whose position count is the carrier's own length (a reference-modified usage-bit slice, §8.4.3.3.4 GR5c;
    /// kb/Work PB173), so the slice expression is evaluated once and no count is re-derived at the call site.</summary>
    public static string BitsPackAll(string bitsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Pack)}({bitsExpr})";

    /// <summary>Unpack a packed image slice back to the run's carrier — <c>CobolBits.Unpack</c>.</summary>
    public static string BitsUnpack(string imageExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Unpack)}({imageExpr}, {countExpr})";

    /// <summary>The UTF-16BE byte serialization of a national string — <c>CobolBits.NatBytes</c> (D-N1; the ONE
    /// national→bytes reduction, shared with the runtime CONVERT NAT arm — PB59 family 5b's ANY storage channel).</summary>
    public static string NatBytes(string expr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatBytes)}({expr})";

    /// <summary>Read a Tier-B REDEFINES member's BIT window out of the class's ONE byte backing —
    /// <c>CobolBits.ReadWindow</c>. The §13.18.44.4 GR1 storage association is stated in BITS, and
    /// §13.18.29.4 GR1c sends a bit group's members through §8.5.1.6.3, so a bit member of a redefines class
    /// is located at a BIT offset in the shared area, not a byte one (kb/Work PB203).</summary>
    public static string BitsReadWindow(string imageExpr, string startBitExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.ReadWindow)}({imageExpr}, {startBitExpr}, {countExpr})";

    /// <summary>Splice a Tier-B REDEFINES member's BIT window back into the class's ONE byte backing, leaving
    /// every other bit of it untouched — <c>CobolBits.WriteWindow</c>, the receiving twin of
    /// <see cref="BitsReadWindow"/> (kb/Work PB203). Leaving the neighbouring bits alone is what makes two
    /// same-level bit members that SHARE a byte (§8.5.1.6.3) independent receivers.</summary>
    public static string BitsWriteWindow(string imageExpr, string startBitExpr, string bitsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.WriteWindow)}({imageExpr}, {startBitExpr}, {bitsExpr})";

    /// <summary>The national read of a Tier-B window over the class's one byte backing —
    /// <c>CobolBits.NatReadWindow</c> (kb/Work PB231). A national character position occupies TWO bytes of the
    /// byte-addressed backing (ISO §13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two), so the
    /// window's bytes are transcoded back to the member's value carrier — the inverse of
    /// <see cref="NatBytes"/>, and the national counterpart of <see cref="BitsReadWindow"/>. The offset is
    /// 0-BASED (the window helpers' convention, unlike the 1-based <see cref="StrRefMod"/>).</summary>
    public static string NatReadWindow(string imageExpr, string startByteExpr, string positionsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatReadWindow)}({imageExpr}, {startByteExpr}, {positionsExpr})";

    /// <summary>Splice a value into a national window of the class backing, leaving every other byte untouched —
    /// <c>CobolBits.NatWriteWindow</c>, the receiving twin of <see cref="NatReadWindow"/> (kb/Work PB231). The
    /// fit to exactly the member's position count is the helper's, done in POSITIONS before serialization.</summary>
    public static string NatWriteWindow(string imageExpr, string startByteExpr, string positionsExpr, string valueExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatWriteWindow)}({imageExpr}, {startByteExpr}, {positionsExpr}, {valueExpr})";

    /// <summary>One member's slice of an unpacked run carrier — <c>CobolBits.Slice</c>.</summary>
    public static string BitsSlice(string carrierExpr, string offsetExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Slice)}({carrierExpr}, {offsetExpr}, {countExpr})";

    /// <summary>Repeat an element image for a table initializer — <c>CobolString.Repeat</c>.</summary>
    public static string StrRepeat(string s, string n) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Repeat)}({s}, {n})";

    // ── Intrinsic functions (CobolIntrinsics / CobolDate / EcFunctions / CobolModule; ISO §15 — P7 Step 12) ──

    /// <summary>A <c>CobolIntrinsics</c> call. <paramref name="method"/> is normally the catalog row's
    /// <c>RuntimeMethod</c> name — <c>IntrinsicCatalog</c> is the single name source, exercised end-to-end by
    /// the intrinsic conformance suite; the TYPE anchor breaks here on a rename.</summary>
    public static string Intrinsic(string method, string args) =>
        $"{nameof(CobolIntrinsics)}.{method}({args})";

    /// <summary>A <c>CobolLocale</c> call (the §15.51–§15.54 LOCALE functions; kb/Work PB64 T4 — same catalog-name discipline).</summary>
    public static string LocaleFn(string method, string args) =>
        $"{nameof(CobolLocale)}.{method}({args})";

    /// <summary>A <c>CobolDate</c> call (the §15 date/time family — same catalog-name discipline).</summary>
    public static string DateFn(string method, string args) =>
        $"{nameof(CobolDate)}.{method}({args})";

    /// <summary>A last-exception interrogation read (§15.28–15.33) — <c>EcFunctions.{method}(args)</c>.</summary>
    public static string EcFn(string method, string args = "") =>
        $"{nameof(Runtime.Exceptions.EcFunctions)}.{method}({args})";

    /// <summary>Push a METHOD activation frame (ISO §15.65.4 r5 — "This may be by a CALL statement, an INVOKE
    /// statement, a function reference, or an inline invocation"; fix-queue PB36). Emitted INSIDE the method body
    /// rather than at the INVOKE site, because a method is reached by several paths — a typed direct call, the
    /// universal <c>__CobolInvoke</c> switch, an inline invocation — and a per-site push would be the same
    /// two-arm dispatch this compiler keeps re-learning.</summary>
    public static string ModulePushMethod(string nameLit, string classLit) =>
        $"{nameof(CobolModule)}.{nameof(CobolModule.Push)}({nameLit}, {classLit}, false)";

    /// <summary>The run unit's module stack, resolved once per activation — <c>CobolModule.Stack</c>.</summary>
    public static string ModuleStack() => $"{nameof(CobolModule)}.{nameof(CobolModule.Stack)}";

    /// <summary>Pop the activation frame pushed by <see cref="ModulePushMethod"/> — always in a finally.</summary>
    public static string ModulePop() => $"{nameof(CobolModule)}.{nameof(CobolModule.Pop)}()";

    /// <summary>FUNCTION MODULE-NAME's runtime read (§15.65) — <c>CobolModule.Name(kind)</c>.</summary>
    public static string ModuleNameFn(int kind) =>
        $"{nameof(CobolModule)}.{nameof(CobolModule.Name)}({kind})";

    /// <summary>§14.9.12.4 GR6c's subsidiary-quotient digit cap (kb/Work PB129) — the low-order digits
    /// at the GIVING receiver's digit capacity, the §14.7.5 no-phrase store's own disposition.</summary>
    public static string NumCapDigits(string expr, int digits) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.CapDigits)}({expr}, {digits})";

    /// <summary>The COMPILE-TIME WHEN-COMPILED stamp format (a typed passthrough like <see cref="MaskScale"/>):
    /// the §15.99.3 r2 compilation timestamp is baked as a constant with the SAME runtime formatter the
    /// generated CURRENT-DATE call uses.</summary>
    public static string DateFormat21(DateTimeOffset t) => CobolDate.Format21(t);

    /// <summary>The COMPILE-TIME fractional-second count of a literal time format (§15.79 — the result scale
    /// is format-derived at compile time), through the ONE runtime format analyzer.</summary>
    public static int DateFormatFractionDigits(string format) => CobolDate.FormatFractionDigits(format);

    /// <summary>The COMPILE-TIME mask-scale computation (a typed passthrough, not a fragment): the emitters
    /// compute a numeric-edited receiver's fraction scale from its edit mask at compile time with the SAME
    /// runtime routine the generated code uses — one definition, anchored here.</summary>
    public static int MaskScale(string picture, char currency, bool commaMode) =>
        CobolEdit.MaskScale(picture, currency, commaMode);

    /// <summary>The COMPILE-TIME edited-image composition (a typed passthrough): a numeric literal VALUE on a
    /// numeric-edited item bakes its edited image as a constant (ISO §13.18.63 GR6) with the SAME runtime
    /// editor the generated code calls.</summary>
    public static string EditCompose(Int128 value, int valueScale, string picture, bool blankWhenZero, string? currencyString,
        bool commaMode, IReadOnlyList<CobolEdit.EditRule>? edits = null) =>
        CobolEdit.Format(value, valueScale, picture, blankWhenZero, '$', commaMode, edits?.ToArray(), currencyString);
}
