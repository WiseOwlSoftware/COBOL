// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The SET-family emitter (P7 Step 9i — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): SET … TO / UP-DOWN BY / pointer F4 senders / OCCURS-DYNAMIC capacity /
/// condition-names TO TRUE, plus the ONE SET-target store/augment pair PERFORM VARYING and SEARCH ride.</summary>
internal sealed class SetEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith, PtrEmitter ptr, MoveEmitter move)
{
    /// <summary><c>SET … TO value</c> (ISO §14.9.39 Format 1): the sender is evaluated ONCE through THE ONE SET
    /// amount landing (GR2 — "the value of the sending operand is determined once"), then each receiver takes it
    /// by kind: an index-name or index data item receives it unchanged (GR2a/GR2b — in the §3.5 model an index IS
    /// its 1-based occurrence number, so cross-table conversion is the identity); a numeric data item receives the
    /// occurrence number through its own PICTURE store (GR2c).
    /// <para>⛔ THE LANDING IS THE GUARD (kb/Work PB459). GR2 a) 1. a — "If the value of arithmetic-expression-1
    /// does not result in an integer" — and GR2 a) 1. b — "outside the limit specified in General rule 2 of
    /// 13.18.38" — each state the same three consequents (the condition set to exist · the SET unsuccessful · the
    /// receiving operand unchanged), and the former <c>long __set = (long)(Align(…, 0))</c> could satisfy NEITHER:
    /// the cast TRUNCATED the fraction the first tests for and WRAPPED the magnitude the second rejects. The whole
    /// receiver loop now sits inside the landing's success leg, so "unsuccessful" leaves EVERY receiver unchanged
    /// — the rule is stated over the statement, not over the receiver that happened to be reached first.</para>
    /// <para>The guard rides every receiver kind and that is exactly right: §14.9.39.3 SR3 forbids
    /// arithmetic-expression-1 with an index-data-item receiver and SR4 requires index-name-2 with a numeric
    /// receiver, so an arithmetic-expression sender implies EVERY receiver is an index-name-1 — GR2 a)'s own
    /// population. With an index / index-data-item sender the value is a <c>long</c> occurrence number, integral
    /// and inside the implementor range by construction, so the landing is a no-op on the GR2 b) / GR2 c) arms
    /// rather than a rule applied where the standard states none.</para></summary>
    public void EmitSetTo(BoundSetTo s)
    {
        var w = ctx.Writer;
        string guard = LandAmount(s.Value, SetAmountRule.IndexTo, "SET … TO", out string tmp, "set");
        using (w.Block($"if ({guard})"))
            foreach (var t in s.Targets) StoreSetTarget(t, new NumX(tmp, 0));
    }

    /// <summary>⛔ THE ONE SET-FAMILY AMOUNT LANDING at the emitter — the two-lane render that keeps an amount's
    /// FRACTION and its FULL MAGNITUDE alive all the way into <see cref="CobolIndex"/>, where §14.9.39.4's
    /// integrality / range / sign guards are written once (kb/Work PB459). Renders the amount temp and returns the
    /// C# condition text that is TRUE when the landing succeeded; the caller puts the statement's effect inside
    /// <c>if (…)</c> so the unsuccessful leg leaves every receiving operand unchanged.
    /// <para>The shape is <see cref="PtrEmitter.EmitSetPointerUpDown"/>'s, by construction: a NATIVE-FLOAT amount
    /// keeps its <c>double</c> so the integrality test runs on the double (kb/Work PB151 — an emitter-side
    /// <c>(long)(double)</c> truncation bypasses the raise entirely), and every other amount arrives as its EXACT
    /// scaled <c>Int128</c> with its scale, so the divisibility test IS the integrality test and a magnitude past
    /// the 64-bit index carrier is a value the runtime can see rather than a wrap it cannot.</para>
    /// <para><paramref name="valueVar"/> is declared by the <c>out long</c> in the returned condition, so it is in
    /// scope for the whole enclosing block — the landed, guaranteed-integral occurrence number.</para></summary>
    /// <param name="x">The rendered amount, BEFORE any narrowing. Funnelled through <c>Landed</c>/<c>DeU</c> here,
    /// so an SDIDI intermediate (STANDARD-DECIMAL, or a native <c>**</c> — kb/Work PB84) and an unsigned-wide read
    /// (kb/Work R10) each reach the exact <c>Int128</c> lane in ONE place rather than at every caller.</param>
    /// <param name="prefix">The generated temp's name stem, so the emitted C# still reads as the statement it came
    /// from (<c>__set…</c> / <c>__cap…</c> / <c>__pv…</c>).</param>
    public string LandAmount(NumX x, SetAmountRule rule, string detail, out string valueVar, string prefix)
    {
        x = num.Landed(NumericRenderer.DeU(x), ReceiverContext.None);
        int id = ctx.Names.NextSet();
        valueVar = $"__{prefix}{id}";
        string raw = $"__amt{id}";
        if (x.Real)
        {
            ctx.Writer.Line($"double {raw} = ({x.Expr});");
            return RuntimeApi.IndexTryAmountReal(raw, rule, detail, valueVar);
        }
        ctx.Writer.Line($"Int128 {raw} = (Int128)({x.Expr});");
        return RuntimeApi.IndexTryAmount(raw, $"{x.Scale}", rule, detail, valueVar);
    }

    /// <inheritdoc cref="LandAmount(NumX,SetAmountRule,string,out string,string)"/>
    private string LandAmount(BoundExpr amount, SetAmountRule rule, string detail, out string valueVar, string prefix) =>
        LandAmount(num.Render(amount, ReceiverContext.None), rule, detail, out valueVar, prefix);

    /// <summary><c>SET pointer… TO {NULL | pointer}</c> (ISO §14.9.39 Format 4; Phase-4b increment 1): copy
    /// the NULL singleton or the source pointer's carrier into each target in order (GR — a straight handle
    /// copy; a data pointer carries no PICTURE store).</summary>
    public void EmitSetPointer(BoundSetPointer s)
    {
        string src = s.ToNull ? "ManagedPointer.Null"
            : s.Address is { } a ? ptr.AddressOfText(a)   // ADDRESS OF sender (F7; Phase-4b inc 2)
            : PlaceRenderer.Read(s.Source!);
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET pointer (ISO §14.9.39 Format 4/7)");
    }

    /// <summary><c>SET LOCALE … TO …</c> (ISO §14.9.39 Format 11; kb/Work PB64 T1): one call on the run unit's ONE
    /// <c>LocaleState</c> per (first operand, source) pair — GR22 (the user default), GR23a/b/c (the categories from a
    /// locale-name / a saved locale / the defaults); GR24 / GR21 are the runtime's EC-LOCALE-MISSING / -INVALID-PTR.</summary>
    public void EmitSetLocale(BoundSetLocale s)
    {
        string state = "RunUnit.Current.Locale";
        string cats = s.Categories == LocaleCategorySet.All ? "LocaleCategorySet.All"
            : string.Join(" | ", Enum.GetValues<LocaleCategorySet>().Where(c => c is not (LocaleCategorySet.None or LocaleCategorySet.All) && s.Categories.HasFlag(c)).Select(c => $"LocaleCategorySet.{c}"));
        string call = (s.SetsUserDefault, s.Source) switch
        {
            (true, LocaleSetSource.LocaleName) => $"{state}.SetUserDefaultFromLocale({CsLiteral(s.Locale!.External)})",
            (true, _) => $"{state}.SetUserDefaultFromSaved({PlaceRenderer.Read(s.SavedPointer!)})",
            (false, LocaleSetSource.LocaleName) => $"{state}.SetFromLocale({cats}, {CsLiteral(s.Locale!.External)})",
            (false, LocaleSetSource.SavedPointer) => $"{state}.SetFromSaved({cats}, {PlaceRenderer.Read(s.SavedPointer!)})",
            (false, LocaleSetSource.UserDefault) => $"{state}.SetFromUserDefault({cats})",
            _ => $"{state}.SetFromSystemDefault({cats})",
        };
        ctx.Writer.Line($"{call};   // SET LOCALE (ISO §14.9.39 Format 11{(s.SetsUserDefault ? ", GR22" : ", GR23")})");
    }

    /// <summary><c>SET identifier TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §14.9.39 Format 12; GR26/GR27): a saved-locale
    /// handle into the data-pointer.</summary>
    public void EmitSaveLocale(BoundSaveLocale s) =>
        ctx.Writer.Line(PlaceRenderer.Write(s.Target, $"RunUnit.Current.Locale.Save(userDefault: {(s.UserDefault ? "true" : "false")})")
            + $"   // SET … TO LOCALE {(s.UserDefault ? "USER-DEFAULT" : "LC_ALL")} (ISO §14.9.39 Format 12)");

    private static string CsLiteral(string s) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, quote: true);

    /// <summary><c>SET program-pointer… TO {NULL | program-pointer}</c> (ISO §14.9.39 Format 9; P10 Step 7):
    /// a straight carrier copy — the Format-4 data-pointer twin over <c>ProgramPointer</c>.</summary>
    public void EmitSetProgramPointer(BoundSetProgramPointer s)
    {
        string src = s.ToNull ? "ProgramPointer.Null" : PlaceRenderer.Read(s.Source!);
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET program-pointer (ISO §14.9.39 Format 9)");
    }

    /// <summary><c>SET function-pointer… TO {NULL | function-pointer}</c> (ISO §14.9.39.2 Format 8; GR14 — the
    /// address is stored in each receiver in the order specified): the Format-9 twin, a carrier copy. The GR14
    /// EC-FUNCTION-PTR-INVALID screen belongs to the ADDRESS OF FUNCTION sender, not here — §14.9.39.3 SR20 has
    /// already proven the two prototypes' signatures equal at bind and §13.18.60.4 GR26 makes the sender's
    /// content NULL-or-same-signature by construction. kb/Work PB452.</summary>
    public void EmitSetFunctionPointer(BoundSetFunctionPointer s)
    {
        string src = s.ToNull ? "FunctionPointer.Null" : PlaceRenderer.Read(s.Source!);
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET function-pointer (ISO §14.9.39.2 Format 8)");
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is evaluated ONCE
    /// through THE ONE landing (GR3 — "If arithmetic-expression-2 does not evaluate to an integer" ⇒
    /// EC-BOUND-SUBSCRIPT, the SET unsuccessful, the receiving operand unchanged), then each index is adjusted by
    /// it through the GUARDED augment (GR4 a) — a result outside the §13.18.38.4 GR2 implementor range is
    /// EC-RANGE-INDEX with the index unchanged). kb/Work PB459: both guards were absent, so `SET IX UP BY 1.5`
    /// moved IX by 1 and `UP BY 9223372036854775800` twice left IX holding a NEGATIVE occurrence number.</summary>
    public void EmitSetUpDown(BoundSetUpDown s)
    {
        var w = ctx.Writer;
        string verb = s.Down ? "SET … DOWN BY" : "SET … UP BY";
        string guard = LandAmount(s.Amount, SetAmountRule.IndexBy, verb, out string tmp, "set");
        using (w.Block($"if ({guard})"))
            foreach (var t in s.Targets) AugmentSetTarget(t, s.Down, new NumX(tmp, 0), verb);
    }

    /// <summary>SET Format 14 (ISO §14.9.39 GR29; OCCURS DYNAMIC, data-model D9): the amount is evaluated ONCE,
    /// then the owning table's current capacity is set / raised / lowered through the runtime — new occurrences
    /// seeded (§8.5.1.9.5), clamped to the minimum, and EC-FLOW-SEARCH raised if a SEARCH of the same table is
    /// active (GR31). The register carries no storage; the operation is on the <c>CobolDynTable&lt;T&gt;</c> itself.</summary>
    public void EmitSetCapacity(BoundSetCapacity s)
    {
        var w = ctx.Writer;
        string call = s.Kind switch
        {
            SetCapacityKind.To => "SetCapacity",
            SetCapacityKind.UpBy => "CapacityUpBy",
            _ => "CapacityDownBy",
        };
        // GR29 — "If arithmetic-expression-4 does not evaluate to a NONNEGATIVE integer, the EC-BOUND-SUBSCRIPT
        // exception condition is set to exist and the execution of the SET statement is unsuccessful." The test is
        // on the AMOUNT, in all three kinds (GR30 forms the new capacity from it afterwards), and GR30's
        // minimum-capacity clamp CANNOT stand in for it: the clamp is the rule for a legal new capacity below the
        // OCCURS minimum, and GR29 rejects the operand BEFORE any new capacity is computed. kb/Work PB459
        // measured both holes — `SET CAP TO A` (A = −3) DESTROYED five live occurrences, `SET CAP TO B` (B = 3.5)
        // shrank five to three.
        string guard = LandAmount(s.Amount, SetAmountRule.Capacity,
            $"SET capacity-register {SetCapacityKinds.Text(s.Kind)}", out string tmp, "cap");
        using (w.Block($"if ({guard})"))
            w.Line($"{PlaceRenderer.RenderPath(s.Table, AccessDir.Sending)}.{call}({tmp});");
    }

    /// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16, COBOL-2023): evaluate the amount ONCE, then
    /// resize the dynamic-length item's native string in place — <c>CobolDynString.SetSize</c> space-fills grown
    /// positions (GR39), drops trailing ones on shrink, clamps above the LIMIT and floors a negative to 0
    /// (GR37/GR38), and sets the nonfatal EC-STORAGE-NOT-AVAIL on the clamp/negative legs when checking was enabled
    /// at this statement (<see cref="BoundSetSize.CheckStorage"/>).</summary>
    public void EmitSetSize(BoundSetSize s)
    {
        // Evaluate arithmetic-expression-5 at FULL precision (a double) — the GR37 sign test must precede the
        // GR38 clamp and the toward-zero truncation, so a fractional negative in (−1,0) still raises. Mirrors the
        // CONTINUE AFTER interval render (StatementEmitter.Visit(BoundContinueAfter)); the runtime does the
        // truncation, not a (long) cast here that would lose the sign of a (−1,0) value.
        string amt = $"__sz{ctx.Names.NextSet()}";
        ctx.Writer.Line($"double {amt} = {NumericRenderer.Real(num.Render(s.Amount, ReceiverContext.None))};");
        ctx.Writer.Line(PlaceRenderer.Write(s.Target,
            RuntimeApi.DynSetSize(PlaceRenderer.Read(s.Target), amt, s.Limit.ToString(), s.CheckStorage ? "true" : "false")));
    }

    /// <summary>SET CONTENT OF identifier-14 … TO … (ISO §14.9.39.2 Format 15, numeric-content; kb/Work PB452) —
    /// one store per receiver, each with the content the binder computed for THAT receiver's data description.
    /// <list type="bullet">
    /// <item>GR32/GR36 (FARTHEST-FROM-ZERO / NEAREST-TO-ZERO): the extreme is a numeric literal, stored through
    /// the ONE MOVE path — so conversion, truncation and an unsigned receiver's sign drop are the ordinary store
    /// rules and not a second copy of them. The <see cref="BoundMove"/> was BUILT BY THE BINDER (kb/Work PB348);
    /// this only renders it.</item>
    /// <item>GR33/GR34/GR35 (FLOAT-INFINITY / FLOAT-NOT-A-NUMBER[-SIGNALING]): a bit-exact write of the carrier.
    /// No numeric store path can carry an infinity or a NaN — CobolNum's store rules are stated over algebraic
    /// values — and §14.9.39.3 SR32 has already confined the receiver to a STANDARD floating-point usage, so the
    /// carrier IS the ISO/IEC 60559:2020 basic interchange format GR33-GR35 name. A WINDOWED receiver (Tier-B /
    /// image-stored) takes its IEEE window bytes, the same shape MoveEmitter's and InitializeEmitter's
    /// float-receiver arms use, so all three deposit identical bytes into the same window.</item></list></summary>
    public void EmitSetContent(BoundSetContent s)
    {
        foreach (var st in s.Stores)
        {
            if (st.Ieee is { } which)
            {
                string ieee = IeeeSpecials.Text(which,
                    single: st.Target.Item.Pic!.Usage is Usage.FloatBinary32, negative: st.NegativeSign);
                ctx.Writer.Line(PlaceRenderer.Write(st.Target, st.Target.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImageFloat(ieee, st.Target.Item.ProfileName)
                    : ieee));
                continue;
            }
            move.Emit(st.Store!);
        }
    }

    /// <summary>THE store into a SET-style target (shared by SET TO and PERFORM VARYING initialization): an
    /// index-name field or index data item takes the integer value UNCHANGED (§14.9.39 GR2a/2b — an index IS its
    /// occurrence number); a numeric data item takes it through its own PICTURE store (GR2c).
    /// <para>⛔ PRECONDITION — <paramref name="value"/> SHALL ALREADY HAVE COME THROUGH <see cref="LandAmount"/>
    /// when the target is an index (kb/Work PB459): the integrality (§14.9.39.4 GR2 a) 1. a) and implementor-range
    /// (GR2 a) 1. b / §13.18.38.4 GR2) guards are stated over the value BEFORE it reaches the <c>long</c> carrier,
    /// so they cannot be applied here — by the time this renders, a fraction and a 64-bit overflow are already
    /// gone. Both of this method's callers land first (<see cref="EmitSetTo"/>, and
    /// <c>ControlFlowEmitter.InitVaryingTarget</c> for PERFORM VARYING's FROM, which §13.18.38.4 GR2 names
    /// alongside SET); <c>SetIndexStoreLandingTests</c> is the drift test that keeps that true for the next
    /// caller.</para></summary>
    public void StoreSetTarget(BoundSetTarget t, NumX value)
    {
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} = (long)({NumericRenderer.Align(value, 0)});");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                // A WINDOWED index data item (Tier-B / image-stored — the Step D arm-1 dissolution) stores
                // its 8 occurrence-number bytes (the R40 pin); a native one takes the raw long unchanged
                // (§14.9.39 GR2b). The raw arm against a window was CS1503 in generated code.
                ctx.Writer.Line(PlaceRenderer.Write(p, p.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImage($"(long)({NumericRenderer.Align(value, 0)})", p.Item.ProfileName)
                    : $"(long)({NumericRenderer.Align(value, 0)})"));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, value, CobolRounding.Truncation);
                break;
        }
    }

    /// <summary>THE augment of a SET-style target by ±amount (shared by SET UP/DOWN BY, PERFORM VARYING and
    /// SEARCH's GR8b varied index): index-name / index data item → THE GUARDED occurrence-number augment; a
    /// numeric data item → an in-place add through its PICTURE store (legal as a VARYING induction variable,
    /// §14.9.28 GR13; a plain SET UP/DOWN on a numeric item is invalid COBOL — the edition validator will diagnose
    /// it, the behavior is the natural add).
    /// <para>⛔ THE INDEX ARMS ARE GUARDED (kb/Work PB459). §14.9.39.4 GR4 a) and §13.18.38.4 GR2 — which names
    /// PERFORM VARYING and SEARCH beside SET as the three statements that may modify an index — make a result
    /// "outside the range of the values allowed by the implementor" the EC-RANGE-INDEX case with the receiving
    /// operand unchanged. The former <c>{ix} += (long)(…)</c> formed the sum IN the carrier, so the boundary was a
    /// silent wrap: measured, <c>SET IDX UP BY 9223372036854775800</c> then <c>UP BY 100</c> left IDX holding a
    /// NEGATIVE occurrence number and the program carried on. <see cref="CobolIndex.Augment"/> forms it in
    /// <c>Int128</c>, where the boundary is a value the runtime can see.</para>
    /// <para>The amount stays <c>Int128</c> into the runtime for the same reason: a PERFORM VARYING BY operand is
    /// an integer data item (§14.9.28.3 SR4 a/c — so no integrality test belongs here) but may be up to 31 digits,
    /// which a <c>long</c> narrowing would wrap before the guard could see it.</para>
    /// <para>The numeric-data-item arm is deliberately NOT guarded: §14.9.28.4 GR13's induction variable is a
    /// PICTURE store with its own §14.7 size rules, and no index exists to be out of range.</para></summary>
    /// <param name="detail">The statement this augment belongs to, for the EC-RANGE-INDEX detail — every caller
    /// names its own verb so the diagnostic reads as the user's statement, not as "SET".</param>
    public void AugmentSetTarget(BoundSetTarget t, bool down, NumX amount, string detail)
    {
        string op = down ? "-" : "+";
        // Int128, not long: see the remarks — a 31-digit BY operand must reach the guard unwrapped.
        string by = $"(Int128)({NumericRenderer.Align(amount, 0)})";
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} = {RuntimeApi.IndexAugment(ix.IndexField, by, down, detail)};");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                // The windowed twin of the StoreSetTarget arm (Step D): decode → augment → re-encode.
                // sending: false — an INDEX data item's class and category are INDEX, not numeric
                // (§13.18.60.4 GR10), so §14.6.13.2 rule 2 ("a numeric sending item") is not about it.
                string cur = p.Item.StoreAsImage
                    ? RuntimeApi.NumParseImage(PlaceRenderer.Read(p), p.Item.ProfileName, sending: false)
                    : PlaceRenderer.Read(p);
                string augmented = RuntimeApi.IndexAugment($"(long)({cur})", by, down, detail);
                ctx.Writer.Line(PlaceRenderer.Write(p, p.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImage(augmented, p.Item.ProfileName)
                    : augmented));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, num.Combine(num.FieldNum(p), op, amount, ReceiverContext.None), CobolRounding.Truncation);
                break;
        }
    }

    public void EmitSet(BoundSetConditions set)
    {
        foreach (var (parent, cond) in set.Sets)
        {
            var (low, _) = cond.Values[0];   // SET TO TRUE stores the first VALUE (ISO §14.9.39 Format 5)
            // ⛔ THE ONE CATEGORY READER (DataItem.OperandPic — an elementary item's own PICTURE, a bit /
            // national GROUP's §13.18.29.4 GR1b/GR2b as-if PICTURE 1(m) / N(m)), never raw `Pic`, which is NULL
            // for every group. §14.9.39.4 GR6 names the population by name — "when the conditional variable is an
            // alphanumeric group item, bit group item, or national group item …" — so all three group shapes are
            // contemplated Format-4 subjects, and reading `Pic` sent every one of them to the loud default:
            // MEASURED, `SET` over a national / bit group emitted `FromNat(NotImplemented.Value<string>(…))`
            // (a runtime throw) and over an ORDINARY group emitted a bare `GA = <string>` that would not even
            // compile (CS0029). kb/Work PB728.
            var pic = parent.Item.OperandPic;
            // An ORDINARY (alphanumeric) group has neither a PICTURE nor an as-if one: §8.8.4.2.1 treats it as an
            // elementary ALPHANUMERIC data item, and its character-position count is its image width.
            bool imageGroup = pic is null && parent.Item.IsGroup;
            PicCategory? cat = pic?.Category ?? (imageGroup ? PicCategory.Alphanumeric : null);
            int width = pic?.Length ?? parent.Item.ImageWidth;
            // A FIGURATIVE-word VALUE (SPACE/ZERO/QUOTE/HIGH-VALUE/LOW-VALUE, incl. ALL forms) fills the
            // conditional variable to its width (§8.3.3.6.4 GR2), not the WORD stored as characters — the
            // fill char is category-aware (national/boolean HIGH/LOW-VALUE = the D-N3 pin). '0' for boolean/
            // numeric ZERO. Only reaches the string categories here (numeric SET handles ZERO natively).
            string? figFill = cat is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean ? FigurativeWordFill(low, cat.Value) : null;
            string rhs = figFill is not null
                ? $"new string({figFill}, {width})"
                : cat switch
            {
                // National joins the character store (its 88-VALUE is the prefix-stripped N"…" text);
                // a boolean parent stores its B"…" bits with the §14.6.8.6 zero pad.
                PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                    RuntimeApi.StrStore(CsLiteral(CobolLiteral.Decode(low)), $"{width}"),
                PicCategory.Boolean =>
                    RuntimeApi.StrStoreBoolean(CsLiteral(CobolLiteral.Decode(low)), $"{width}", false),
                PicCategory.Numeric =>
                    ArithmeticEmitter.Narrow(RuntimeApi.NumStore(UnscaledAtScale(low, pic!.Scale), $"{pic.Scale}", parent.Item.ProfileName), parent.Item),
                _ => LoudValue("string", $"SET condition '{cond.Name}' over a '{parent.Item.CobolName}' of no category"),
            };
            // ⛔ An ORDINARY group receiver takes the value through THE ONE GROUP-IMAGE STORE, not a bare
            // assignment: PlaceRenderer.Write's MemberPlace arm assigns the record struct, so handing it a string
            // is the CS0029 above. §14.9.25.4 GR4 — a group receiver is "filled without consideration for the
            // individual elementary or group items" — and §14.9.39.4 GR6's OCCURS-dependent length is exactly the
            // §13.18.38 GR8 splice WriteGroupImage already carries. A bit / national group is NOT an image group:
            // its as-if value is boolean positions / national positions, which Write routes to FromBits / FromNat.
            ctx.Writer.Line(imageGroup
                ? PlaceRenderer.WriteGroupImage(parent, rhs, $"SET condition '{cond.Name}' TO TRUE")
                : PlaceRenderer.Write(parent, rhs));
        }
    }

    /// <summary>The category-aware C# <c>char</c>-literal a level-88 figurative-word VALUE fills with (SET TO
    /// TRUE, ISO §14.9.39 Format 5 + §8.3.3.6.4 GR2), or null when the operand is not a bare figurative word
    /// (a quoted / N"…" / B"…" / numeric literal takes the store path). Tolerates the ALL-prefixed spelling.</summary>
    private string? FigurativeWordFill(string raw, PicCategory cat)
    {
        string w = raw.Trim();
        if (w.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && w.Length > 3
            && (char.IsWhiteSpace(w[3]) || char.IsLetter(w[3])))
            w = w[3..].TrimStart();
        return FigurativeConstants.KindOf(w, includeNull: true) is { } k
            ? FigurativeConstants.Fill(k, ctx.Data.Collating, cat, ctx.Data.NationalCollating) : null;   // the ONE service (P7 Step 4)
    }

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ─────────────────────────────────────────────────────────────

}
