// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The MOVE figurative-constant edition gates (ISO/IEC 1989:2023 §14.9.25.3 SR5; roadmap Phase 2 W2 track A —
/// VCR rows 1 / 92 / 128): SR5 permits "an ALL "literal" figurative constant (containing only digits) or an ALL
/// symbolic-character (representing a digit)" to move "to an integer numeric item"; "in all other cases, the move
/// of an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL
/// symbolic-character) to either a numeric item or a numeric-edited item is prohibited". The prohibition is NEW in
/// 2023 — these moves were permitted through ISO 2014 (Annex E.2 item 1 bullet 1, the removals list) — so the
/// registry row "move-alphanumeric-figurative-removed-2023" routes it: silent below 2023, error at 2023 strict,
/// warning at 2023 --permissive (the pre-removal semantics preserved). The surviving digit-only-ALL-to-integer
/// case is itself obsolete at 2023 (the SR5 NOTE; Annex F.2 item 2) — row "move-all-digit-integer-obsolete-2023",
/// a 0903 warning at ≥2023 and silent below.
/// </summary>
/// <summary>The MOVE verb binder (P7 Step 10e — a real collaborator over <see cref="BinderContext"/>;
/// absorbs the former <c>StatementBinder.MoveFigurative</c> partial WHOLE: the SR1 class-index errors, the
/// SR5 pre-removal <c>MarkImageForced</c> storage marking, the Table-16 legality arms, and the W2 ref-mod
/// image marking fire at the SAME per-statement points — the collected-fact choreography the
/// <c>StorageFormPass</c> and <c>VersionConformancePass.GateMove</c> depend on is byte-preserved).</summary>
/// <summary>
/// The syntax rules ONE verb's <c>… FROM {identifier-1 | literal-1}</c> phrase carries IN ADDITION to the MOVE
/// statement's own — one row per verb, read by <see cref="MoveBinder.BindFromPhrase"/>.
///
/// <para>⛔ The columns are SETS and NULLABLE clauses, not flags, because the three verbs' rules genuinely
/// differ and a scalar "the function must be alphanumeric" column would reject legal source: RELEASE
/// (§14.9.32.3 SR2) and WRITE (§14.9.51.3 SR4) admit an alphanumeric or national function, REWRITE
/// (§14.9.35.3 SR9) also admits a BOOLEAN one, and only RELEASE (§14.9.32.3 SR4) forbids a zero-length
/// literal-1. Adding the next FROM phrase is a row here, not another screen
/// (feedback_model_the_rule_shape_not_one_case).</para>
/// </summary>
/// <param name="Statement">How the statement names itself in a diagnostic — <c>RELEASE … FROM</c>.</param>
/// <param name="MoveRuleCite">The clause making the phrase an implicit MOVE, plus the clause subjecting
/// identifier-1/literal-1 to the MOVE statement's own syntax rules.</param>
/// <param name="FunctionCategories">The result categories a function-identifier in this position may have, or
/// <see langword="null"/> where the verb states no such rule.</param>
/// <param name="FunctionRuleCite">The quoted rule behind <paramref name="FunctionCategories"/>.</param>
/// <param name="ZeroLengthLiteralRuleCite">The quoted rule forbidding a zero-length literal-1, or
/// <see langword="null"/> where the verb states none.</param>
internal sealed record FromPhraseRules(
    string Statement,
    string MoveRuleCite,
    PicCategory[]? FunctionCategories,
    string? FunctionRuleCite,
    string? ZeroLengthLiteralRuleCite)
{
    /// <summary>The <see cref="ImplicitMovePhrase"/> every move of THIS verb's FROM phrase carries — built once
    /// per verb, beside the three static rows, rather than once per bound statement. It is the same object the
    /// two INTO phrases keep as statics (<c>ImplicitMovePhrase.ReadInto</c> / <c>ReturnInto</c>); a phrase is a
    /// property of the VERB, not of an occurrence of it, so all five are singletons.</summary>
    public ImplicitMovePhrase Phrase { get; } = new(Statement, MoveRuleCite);

    /// <summary>RELEASE (ISO §14.9.32) — the only FROM phrase carrying the zero-length-literal rule.</summary>
    public static readonly FromPhraseRules Release = new(
        "RELEASE … FROM",
        "ISO §14.9.32.4 GR4 a); §14.9.32.3 SR3",
        [PicCategory.Alphanumeric, PicCategory.National],
        "ISO §14.9.32.3 SR2 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or "
        + "national function.\"",
        "ISO §14.9.32.3 SR4 — \"Literal-1 shall not be a zero-length literal.\"");

    /// <summary>WRITE (ISO §14.9.51). §14.9.51.3 SR4 is UNCONDITIONAL and stricter than the FILE-less SR11
    /// ("alphanumeric, boolean, or national"), so a conforming program satisfies both only inside SR4's set —
    /// which is what is enforced. The FILE-phrase arm (SR10) is unreachable: Annex A.4.13 item 2 is declined.</summary>
    public static readonly FromPhraseRules Write = new(
        "WRITE … FROM",
        "ISO §14.9.51.4 GR5 a); §14.9.51.3 SR6",
        [PicCategory.Alphanumeric, PicCategory.National],
        "ISO §14.9.51.3 SR4 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or "
        + "national function.\"",
        null);

    /// <summary>REWRITE (ISO §14.9.35). §14.9.35.3 SR9's set is the WIDER one — it admits a boolean function —
    /// and it is the applicable rule because the FILE phrase (SR8's condition) is declined, A.4.13 item 2.</summary>
    public static readonly FromPhraseRules Rewrite = new(
        "REWRITE … FROM",
        "ISO §14.9.35.4 GR7 a); §14.9.35.3 SR6",
        [PicCategory.Alphanumeric, PicCategory.Boolean, PicCategory.National],
        "ISO §14.9.35.3 SR9 — \"If identifier-1 references a function and the FILE phrase is not specified, "
        + "identifier-1 shall reference an alphanumeric, boolean, or national function.\"",
        null);
}

internal sealed class MoveBinder(BinderContext ctx, StatementBinder host, CorrespondingBinder corr)
{
    public BoundStatement Bind(Core.MoveStatementContext move)
    {
        if (move.CORRESPONDING() is not null || move.CORR() is not null)   // Format 2 — BOTH tokens (§14.9.25.3 SR11)
            return corr.Bind(CorrVerb.Move, move.dataReference(), CobolRounding.Truncation, null);
        if (move.moveSendingOperand() is not { } send || move.moveReceivingPhrase()?.dataReferenceList() is not { } targets)
            return new BoundUnsupported("MOVE CORRESPONDING / unsupported MOVE form");
        BoundOperand source = send.literal() is { } lit ? host.Expr.LiteralOperand(lit)
            : send.dataReference() is { } dref ? host.Expr.FieldOperand(dref)
            // MOVE FUNCTION … TO targets (ISO §14.9.25 + §15.2 — a function is a sending item of its category).
            : send.functionCall() is { } sfc ? host.Intrinsic.IntrinsicOperand(sfc)
            : new BoundOperandError("MOVE source");
        // An INDEX-NAME sending operand (kb/Work R16): MOVE is not among §13.18.38.3 r7's five index-name
        // contexts — the same judgment the SR1 arm below applies to class-index DATA ITEMS (COBOLNET0809).
        // Before this, a string-category receiver aborted at RUN time and a numeric one silently computed.
        if (send.dataReference() is { } sdref
            && host.Expr.ScreenIndexNameOperand(source, sdref.GetText(), "a MOVE sending operand"))
            source = new BoundOperandError($"MOVE of the index-name '{sdref.GetText()}' (ISO §13.18.38.3 r7)");
        var resolved = host.Expr.ResolveTargets(targets.dataReference());
        return BindMoveOf(source, resolved);
    }

    /// <summary>
    /// ⛔ <b>THE ONE APPLICATION OF THE MOVE STATEMENT'S OWN RULES TO A SENDER/RECEIVER PAIR</b> — the explicit
    /// MOVE statement above, and EVERY implicit move a phrase's rules define (<c>READ … INTO</c>,
    /// <c>RETURN … INTO</c>, <c>WRITE/REWRITE/RELEASE … FROM</c>), which reach it through
    /// <see cref="BindFromPhrase"/> / <see cref="BindIntoPhrase"/>.
    ///
    /// <para><b>Why it exists (kb/Work PB348).</b> The implicit moves used to be constructed in the EMITTER —
    /// <c>move.Emit(new BoundMove(from, [rl.Record]))</c> — downstream of every screen below AND of the storage
    /// facts the emitter's own output depends on. Two consequences, both measured: <c>RELEASE SRT-REC FROM
    /// WS-NUM</c> (a PIC 9(3) sender into a PIC A(8) record) compiled clean where the identical explicit
    /// <c>MOVE</c> drew COBOLNET0819; and <c>RELEASE SRT-NUM FROM QUOTE</c> ABORTED THE RUN with an unhandled
    /// <c>NotImplementedCobolFeatureException</c>, because <see cref="MarkImageForced"/> — a fact
    /// <c>StorageFormPass</c> consumes — is collected HERE and the emitter-built move never passed through.
    /// §14.9.32.4 GR4 makes <c>RELEASE record-name-1 FROM x</c> exactly <c>MOVE x TO record-name-1</c> followed
    /// by the same RELEASE, so "exactly" has to include the bind.</para>
    ///
    /// <para>Everything a MOVE's own rules impose lives in this one call, so a phrase added tomorrow inherits it
    /// by construction rather than by an edit. <paramref name="implicitOf"/> is null for the explicit statement
    /// and names the phrase otherwise, so each screen's diagnostic reports the statement that was written.</para>
    /// </summary>
    public BoundMove BindMoveOf(BoundOperand source, IReadOnlyList<Place> targets,
                                ImplicitMovePhrase? implicitOf = null)
    {
        // The §14.9.25.3 SR5 edition gates (VCR rows 1 / 92 / 128) + the SR1 class-index check.
        MoveFigurativeEditionGates(source, targets, implicitOf);
        // The Table 16 boolean/national legality arms + SR7 (Phase 4a — StatementBinder.MoveFigurative.cs).
        MoveCategoryLegality(source, targets, implicitOf);
        // A ref-mod slice store on a numeric-DISPLAY receiver needs image backing for ANY sender (§8.4.3.3.4 GR6;
        // the W2 adversarial-review round-trip-loss fix — see MarkRefModStoreImage).
        MarkRefModStoreImage(targets);
        ctx.Validation.CheckStrongMove(source, targets, implicitOf);   // §14.9.25.3 SR2 — pure check (D17 inc 2)
        return new BoundMove(source, targets) { ImplicitOf = implicitOf };
    }

    // ── The … FROM and … INTO phrases (kb/Work PB348) ───────────────────────────────────────────────────────

    /// <summary>
    /// Bind a <c>… FROM {identifier-1 | literal-1}</c> phrase into its implicit MOVE — WRITE (ISO §14.9.51),
    /// REWRITE (§14.9.35) and RELEASE (§14.9.32) alike. Null when no FROM phrase is written.
    ///
    /// <para>The operand is bound, the verb's OWN phrase syntax rules are applied (<paramref name="rules"/>),
    /// and the move is then screened by <see cref="BindMoveOf"/> under the MOVE statement's rules — which is
    /// what each verb's "shall be valid as a sending operand in a MOVE statement specifying record-name-1 as
    /// the receiving operand" rule says to do (§14.9.32.3 SR3 · §14.9.35.3 SR6 · §14.9.51.3 SR6).</para>
    ///
    /// <para>⛔ This is the successor to the former <c>SequentialIoBinder.WriteSource</c>, which bound the
    /// operand and inspected NOTHING — a three-way hand-off from the three verbs, each of which carries its own
    /// syntax rules that were therefore applied by no one, and the move itself was then built in the emitter.
    /// The home moved here because this class is where a MOVE's rules live; the sort binder no longer reaches
    /// across into the sequential I-O binder for it.</para>
    /// </summary>
    public BoundMove? BindFromPhrase(FromPhraseRules rules, Place record, Core.DataReferenceContext? dref,
                                     Core.LiteralContext? lit, Core.FunctionCallContext? fc)
    {
        if (dref is null && lit is null && fc is null) return null;   // no FROM phrase
        BoundOperand source =
            fc is not null ? host.Intrinsic.IntrinsicOperand(fc)
            : lit is not null ? host.Expr.LiteralOperand(lit)
            : host.Expr.FieldOperand(dref!);

        // ── The FUNCTION-IDENTIFIER class restriction, a SET per verb because the rules name different sets:
        // RELEASE §14.9.32.3 SR2 and WRITE §14.9.51.3 SR4 admit "an alphanumeric or national function";
        // REWRITE §14.9.35.3 SR9 admits "an alphanumeric, boolean, or national function" when the FILE phrase is
        // not specified (and the FILE arm, §14.9.35.3 SR8 / §14.9.51.3 SR10, is Annex A.4.13 item 2 — DECLINED
        // COBOLNET1706 before this point, so only the no-FILE readings are reachable). A scalar "must be
        // alphanumeric" column here would reject legal source on the REWRITE arm.
        // The class is read off the BOUND operand through the same Table-16 sender position the category screen
        // uses, so a folded integer function (FUNCTION LENGTH constant-folds to a numeric literal, which is how
        // it escaped every earlier reading of this position) still answers "numeric", not "unknown". ──
        if (fc is not null && rules.FunctionCategories is { } admitted
            && SenderPosition(source).Category is var cat && !admitted.Contains(cat))
            ctx.Edition.Error(DiagnosticCatalog.FromPhraseFunctionClass,
                $"{rules.Statement} {fc.GetText()}: {rules.FunctionRuleCite} — this function's result is of "
                + $"category {cat.ToString().ToLowerInvariant()}");

        // ── §14.9.32.3 SR4, "Literal-1 shall not be a zero-length literal." RELEASE's alone: neither
        // §14.9.51.3 nor §14.9.35.3 states it for the FROM phrase of WRITE / REWRITE, so it is carried on the
        // rules row rather than applied to every verb. The compiler already owns the predicate — it is the same
        // BoundStringLiteral test ControlFlowBinder applies to the STOP RUN / GOBACK status literal. ──
        if (rules.ZeroLengthLiteralRuleCite is { } zlCite && source is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error(DiagnosticCatalog.FromPhraseZeroLengthLiteral,
                $"{rules.Statement}: {zlCite}");

        return BindMoveOf(source, [record], rules.Phrase);
    }

    /// <summary>
    /// Bind a <c>… INTO identifier-1</c> phrase into its implicit MOVE — the sequential READ, the keyed READ and
    /// the sort RETURN (ISO §14.9.30.4 GR4 b) and §14.9.34.4 GR5 b), which are the same sentence: "The current
    /// record is moved from the record area to the area specified by identifier-1 according to the rules for the
    /// MOVE statement without the CORRESPONDING phrase").
    ///
    /// <para>The SENDER is the record area sliced to its §13.18.43.4 GR16 byte count, and — for a FORMAT 2
    /// <c>RECORD IS VARYING</c> file only — designated an alphanumeric group move by those same rules. That
    /// choice is <see cref="BoundCurrentRecord"/>'s and was built in ONE place already (kb/Work PB339); it moved
    /// here from the emitter with the move it belongs to, because an operand is a bind-time object.</para>
    /// </summary>
    public BoundMove BindIntoPhrase(FileModel file, Place area, Place receiver, ImplicitMovePhrase phrase)
    {
        // §13.18.43.4 GR16 a) reads the DEPENDING item; GR16 b) applies when the phrase is absent. Resolved
        // HERE, once, for all three INTO arms — the sequential READ, the keyed READ and the sort RETURN.
        Place? depending = file is { Varying.DependingName: not null, VaryingDependingItem: { } d }
            ? ctx.Refs.ResolveItem(d) : null;
        // A FIXED-length file keeps the plain record-area operand: its current record IS the whole area
        // (§13.18.43.4 GR6 — integer-1 bytes in every record), including the short-final-record '04' case of
        // §14.9.30.4 GR14, where the area right of the last valid character is undefined rather than short.
        return BindMoveOf(
            file.Varying is { } v ? new BoundCurrentRecord(area, file, depending, v.VaryingClause)
                                  : new BoundFieldOperand(area),
            [receiver], phrase);
    }

    /// <summary>
    /// Apply the §14.9.25.3 SR5 gates to one bound MOVE (Format 1) and flag the pre-removal receivers'
    /// storage. Exemptions honored:
    /// <list type="bullet">
    /// <item>ZERO/ZEROS/ZEROES — the NUMERIC figurative (§8.3.3.6.4 GR4 "the numeric value '0'"); SR5's
    /// prohibition names only the alphanumeric figuratives, and Table 17 gives ZERO category numeric against a
    /// numeric receiver.</item>
    /// <item>GROUP receivers — a group move is a character copy without conversion (§14.9.25.4 GR4), not a
    /// numeric elementary move, so SR5 does not reach it.</item>
    /// <item>Reference-modified receivers — the unique result of reference modification is an elementary
    /// ALPHANUMERIC item whatever the underlying item (§8.4.3.3.4 GR6), so the receiver is not numeric.</item>
    /// <item>The digit-only single-character ALL "literal" into an INTEGER numeric item — SR5's surviving
    /// exception, valid at every edition; obsolete-flagged 0903 at ≥2023 (SR5 NOTE; Annex F.2 item 2).</item>
    /// </list>
    /// A digit-only ALL longer than one character is NOT the exception even though its characters are digits:
    /// §8.3.3.6.3 SR3 forbids associating an ALL literal whose length is greater than one with a numeric or
    /// numeric-edited item (the '85 obsolete element the legacy oracle still accepts — MOVE ALL "57" TO PIC 9(3)
    /// stores 575), so at 2023 it falls under the 0902 removal row with the other prohibited shapes.
    /// A digit-only ALL to a NON-integer numeric receiver (PIC 9V9) is likewise outside SR5's exception —
    /// 0902 at 2023; pre-2023 it fills every digit position (legacy-oracle adjudicated, provisional).
    /// </summary>
    private void MoveFigurativeEditionGates(BoundOperand source, IReadOnlyList<Place> targets,
                                            ImplicitMovePhrase? implicitOf)
    {
        // The §14.9.25.3 SR1 class check FIRST — version-invariant, every sender kind: "The class of
        // identifier-1 or identifier-2 shall not be index, message-tag, object, or pointer." An index data
        // item may be referenced only by SET, SEARCH, relation conditions, and as a function/USING argument
        // (§13.18.60.3 SR10) — a MOVE operand of class index is invalid at EVERY edition, never an Annex-E
        // removal (the W2 adversarial review caught the 0902 row mislabeling it "permitted through 2014").
        // Message-tag/object/pointer classes cannot reach a bound MOVE yet (their usages are compile-gated
        // skeletons, W2 track B) — this check gains those arms when their phases land.
        if (source is BoundFieldOperand { Place.Item.Pic.Usage: Usage.Index } sIdx)
            ctx.Edition.Error("COBOLNET0809",
                $"a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60.3 SR10 — only a "
                + $"SEARCH or SET statement, a relation condition, an intrinsic-function or inline-method "
                + $"argument, or a procedure-division / CALL / INVOKE USING phrase may reference an index "
                + $"data item) — MOVE {sIdx.Place.Item.CobolName}{ImplicitMovePhrase.Via(implicitOf)}");
        // SR1 reaches a FUNCTION sender through §15.2 item 6 (kb/Work PB124 wave 5b): "Index functions.
        // These are of the class and category index." — MAX/MIN over index arguments IS one, and its result's
        // storage category (Numeric) made it indistinguishable from a numeric sender here, so
        // MOVE FUNCTION MAX(IX1 IX2) TO 9(n) silently stored an occurrence-number image (GR-15.2-6).
        // Typed by the SAME IntrinsicResultType.Resolve the binder resolves results with.
        if (source is BoundComputedOperand { Expr: BoundIntrinsicCall sic }
            && IntrinsicResultType.Resolve(sic.Sig, sic.Args) is IntrinsicType.Index)
            ctx.Edition.Error("COBOLNET0809",
                $"a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §15.2 item 6 — FUNCTION "
                + $"{sic.Sig.Name} over index arguments is an INDEX function, of the class and category index)"
                + ImplicitMovePhrase.Via(implicitOf));
        foreach (var t in targets)
            if (t.Item.Pic is { Usage: Usage.Index })
                ctx.Edition.Error("COBOLNET0809",
                    $"a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60.3 SR10) — "
                    + ImplicitMovePhrase.WhereOf(implicitOf, t.Item.CobolName));

        // Classify the SENDER (only the SR5 alphanumeric figuratives / ALL "literal" participate). The §14.9.25.3
        // SR5 EDITION gates (MoveAllDigitIntegerObsolete2023 / MoveQuoteNumericObsolete2014 /
        // MoveAlphanumericFigurativeRemoved2023) moved to the post-bind VersionConformancePass (Step 14f), which
        // re-derives the SAME classification from the bound MOVE. The binder keeps ONLY the pre-removal STORAGE
        // marking below (needed at 85/2002/2014 + 2023 --permissive regardless of the gate), with the same eligibility.
        var all = source as BoundAllLiteral;
        if (source is not (BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } or BoundAllLiteral)) return;
        foreach (var t in targets)
        {
            if (t is RefModPlace || t.Item.OperandPic is not { } pic) continue;   // SR5 exemptions (an alphanumeric group; D20)
            if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)) continue;
            if (pic.Usage is Usage.Index) continue;   // class index — SR1 errored above

            // Pre-removal storage (reachable at 85/2002/2014 + 2023 --permissive): a NON-digit fill
            // (SPACE/QUOTE/HIGH-VALUE/LOW-VALUE, or an ALL literal with a non-digit) deposits the fill CHARACTERS as
            // the receiver's character image (provisional; the legacy oracle's byte fill — MOVE QUOTE TO PIC 9(3)
            // leaves three quotation marks, IS NUMERIC then false, a later read decodes deterministically per
            // §14.6.13.2). Flag an eligible elementary numeric-DISPLAY receiver StoreAsImage — the SAME §14.9 MOVE
            // GR4 whole-group image substrate; a digit-only ALL stores its numeric value natively (no image), a
            // numeric-edited receiver is string-backed by nature, a Tier-B REDEFINES window / NumericImagePlace
            // already writes its image, and a REDEFINES shared-storage alias keeps its (already-run) tier flag.
            if (all is not { IsDigitOnly: true } && t is not (RedefViewPlace or NumericImagePlace)
                && t.Item.Class is null
                && pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                ctx.Data.MarkImageForced(t.Item);   // the collected image fact
        }
    }

    /// <summary>The receiver-view data category of a MOVE target for the §14.9.25.3 Table 16 legality check.
    /// A reference-modified receiver is the unique data item of ISO §8.4.3.3.4 GR6, whose category is computed by
    /// the ONE rule on <see cref="RefModPlace.CategoryOf"/> (PB20). Groups return null (a group move is a
    /// conversion-free character copy, GR4 — Table 16 does not reach it; bit/national GROUP-USAGE groups are
    /// grammar residue).
    /// <para>⛔ THIS CARRIED ITS OWN PARTIAL COPY of GR6 and got two of the three lettered exceptions wrong:
    /// national and boolean were preserved correctly, but national-EDITED flattened to alphanumeric (GR6b makes
    /// it national) and so did a numeric item of usage NATIONAL (GR6c makes it national). Three copies of one
    /// rule, none complete — the copies are gone.</para></summary>
    private static PicCategory? MoveReceiverCategory(Place t) => t switch
    {
        // The view FIRST: a ref-mod over a GROUP is the elementary ALPHANUMERIC unique item of GR6 (kb/Work PB70) —
        // Table 16 applies to it, where the whole group would be a conversion-free GR4 copy.
        RefModPlace rm => rm.Category,
        // D20/PB79: a bit / national group receiver IS a category — its as-if picture's (§13.18.29.4 GR1b/GR2b);
        // only an alphanumeric group is the GR4 conversion-free copy Table 16 does not reach.
        _ when t.Item.OperandPic is null => null,
        _ => t.Item.OperandPic!.Category,
    };

    /// <summary>
    /// The §14.9.25.3 Table 16 category-legality arms for the 2002 categories (boolean / national), plus SR7
    /// (a figurative whose characters are not boolean shall not move to a boolean item) — all COBOLNET0819,
    /// version-invariant at ≥2002 (below 2002 the operands themselves are already 0900-introduction-gated).
    /// Only the arms Table 16 marks "No" around the NEW categories are checked here — the classic
    /// alphanumeric/numeric rows keep their existing paths (VCR rows 1/92/128 above). A GROUP sender or
    /// receiver is exempt (GR4 group moves copy characters without conversion).
    /// </summary>
    private void MoveCategoryLegality(BoundOperand source, IReadOnlyList<Place> targets,
                                      ImplicitMovePhrase? implicitOf)
    {
        // The SENDER's Table-16 position — the ONE reader (fix-queue PB72; see SenderPosition).
        Table16Operand senderPos = SenderPosition(source);
        // §14.9.25.3 SR8: a fixed-width binary sender (BINARY-CHAR/-SHORT/-LONG/-DOUBLE) shall reference
        // only a numeric or numeric-edited receiver — SR10 (Table 16) applies only to cases NOT covered by
        // SR8, so this precedes the Table-16 arms. The family is 2002+ (absent from the '85 corpus), so
        // the check is corpus-safe at every receiver category.
        bool senderBinaryFamily = source is BoundFieldOperand fb
            && fb.Place.Item.Pic is { Usage: Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble };

        foreach (var t in targets)
        {
            if (MoveReceiverCategory(t) is not { } recvCat) continue;   // group receiver — GR4 exempt
            string where = ImplicitMovePhrase.WhereOf(implicitOf, t.Item.CobolName);

            if (senderBinaryFamily && recvCat is not (PicCategory.Numeric or PicCategory.NumericEdited))
            {
                ctx.Edition.Error("COBOLNET0819", $"{where}: a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sending "
                    + "operand shall reference only a numeric or numeric-edited receiver (ISO §14.9.25.3 SR8)");
                continue;
            }

            // §14.9.25.3 SR7 — a SOURCE-SHAPE rule, not a Table-16 one: a figurative constant whose characters
            // are not boolean characters (and the ALL-literal form of the same) never moves to a boolean item.
            // ZERO is boolean zeros by context (§8.3.3.6.4 GR4). This keys on the bound operand's shape, which a
            // category table cannot see, so it stays here.
            if (recvCat is PicCategory.Boolean && source is BoundFigurative { Kind: not 'Z' })
                ctx.Edition.Error("COBOLNET0819", $"{where}: a figurative constant whose characters are "
                    + "not boolean characters shall not be moved to a boolean data item "
                    + "(ISO §14.9.25.3 SR7)");
            else if (recvCat is PicCategory.Boolean && source is BoundAllLiteral bal
                     && (bal.Literal.Length == 0 || !bal.Literal.All(c => c is '0' or '1')))
                ctx.Edition.Error("COBOLNET0819", $"{where}: ALL \"{bal.Literal}\" contains non-boolean "
                    + "characters and shall not be moved to a boolean data item (ISO §14.9.25.3 SR7)");
            // ⭐ AND THE CATEGORY-PAIR RULE ITSELF IS NOW ASKED OF THE ONE TABLE (fix-queue PB53). It used to be
            // four inline arms here and a §14.8.2.3.2 STRICT-IDENTITY fallback in the INVOKE argument screen —
            // two answers to one question, and §14.8.2.3.3 rule 2d says the INVOKE crossing asks THIS one.
            // The RECEIVER position likewise builds through Table16Operand.Of(Place) (PB72): a ref-mod receiver is
            // plain alphanumeric (GR2/GR6), never the inner item's alphabetic/edited row.
            else if (MoveTable16.Refusal(senderPos, Table16Operand.Of(t)) is { } refusal)
            {
                // The two leniencies (kb/Work PB73): a NUMERIC-typed function into a character receiver (Table 16's
                // Noninteger row; every earlier release admitted it as the CONFORMANCE.md item-92 text form) and a
                // reference-modified ALPHABETIC view read as plain alphanumeric (GnuCOBOL's reading; PB72's
                // 2026-08-09 erasure) — accepted under --permissive with a warning when the lenient reading admits
                // the move; every other refusal is an error on both axes.
                bool senderIsFunction = source is BoundComputedOperand { Expr: BoundIntrinsicCall };
                bool senderIsView = source is BoundFieldOperand { Place: RefModPlace };
                if (ctx.Edition.Permissive
                    && MoveTable16.Refusal(Table16Operand.Lenient(senderPos, senderIsFunction, senderIsView),
                                           Table16Operand.Lenient(Table16Operand.Of(t), false, t is RefModPlace)) is null)
                    ctx.Edition.Warning("COBOLNET0819", $"{where}: {refusal}; accepted under --permissive "
                        + (senderIsFunction ? "as the function's literal text (a NUMERIC-typed function is the Noninteger sender, ISO §15.2 item 4)"
                                            : "reading the reference-modified view as plain alphanumeric (ISO §8.4.3.3.4 GR6 keeps it alphabetic)"));
                else
                    ctx.Edition.Error("COBOLNET0819", $"{where}: MOVE is invalid — {refusal}"
                        + (senderIsFunction && senderPos is { Category: PicCategory.Numeric, IsNonInteger: true }
                            ? " (a NUMERIC-typed function is the Noninteger sender, §15.2 item 4 / §8.4.3.2.3 SR11; an INTEGER function moves to a character receiver; --permissive accepts this as the function's literal text)"
                            : ""));
            }
        }
    }


    /// <summary>The SENDER's position in the §14.9.25.3 Table 16 category matrix (fix-queue PB72: built in ONE
    /// place, and a FIELD builds through <c>Table16Operand.Of(Place)</c> so a ref-mod view takes §8.4.3.3.4
    /// GR2/GR6's rewrites — category via the one GR6 reader, and the finer alphabetic/edited/noninteger flags
    /// erased, because the unique data item a view creates is plain class-and-category alphanumeric). An
    /// INTRINSIC sender reports the §15.18.4 r3 ALPHABETIC rider alongside its result category — the finer row
    /// Table 16 keys on and <c>PicCategory</c> deliberately cannot express (the PIC A fold).
    /// <para>It is a NAMED reader rather than an inline switch because the FROM-phrase function-class rules
    /// (§14.9.32.3 SR2 · §14.9.51.3 SR4 · §14.9.35.3 SR9) ask the same question of the same operand, and a
    /// second reading of "what category is this sender" is exactly how a folded <c>FUNCTION LENGTH</c> — which
    /// reaches the binder as a plain numeric literal — would have been read as "unknown" and waved through
    /// (kb/Work PB348).</para></summary>
    private static Table16Operand SenderPosition(BoundOperand source) => source switch
    {
            BoundStringLiteral sl => new Table16Operand(sl.Category),
            BoundAllLiteral al => new Table16Operand(al.Category),
            BoundFieldOperand f when f.Place is not RefModPlace && f.Place.Item.OperandPic is null =>
                new Table16Operand(PicCategory.Group),   // GR4 — an ALPHANUMERIC group moves without conversion (D20)
            BoundFieldOperand f => Table16Operand.Of(f.Place),
            BoundNumericLiteral nl => new Table16Operand(PicCategory.Numeric, IsNonInteger: nl.Text.Contains('.')),
            // An INTRINSIC sender's Table-16 row is its §15.2 TYPE (kb/Work PB73, adjudicated 2026-08-18): an
            // INTEGER function ("no digits to the right of the decimal point", §15.2 item 5 — resolved per call by
            // the ONE IntrinsicResultType reader, so MAX over integers is integer) is the Integer row; a NUMERIC
            // function (item 4) is the NONINTEGER row whatever a particular reference's value — §8.4.3.2.3 SR11's
            // principle for the integer-operand positions applies to the table's split too. The former admission
            // (IsNonInteger: false for every function) survives under --permissive as a warning, below.
            BoundComputedOperand { Expr: BoundIntrinsicCall ic } =>
                new Table16Operand(ic.ResultCategory, ic.ResultIsAlphabetic,
                    IsNonInteger: ic.ResultCategory is PicCategory.Numeric && !IntrinsicResultType.IsIntegerOperand(source)),
            BoundComputedOperand => new Table16Operand(PicCategory.Numeric),
            _ => new Table16Operand(PicCategory.Group),   // figuratives (SR7's own arm) / errors — category-exempt
    };

    /// <summary>
    /// Ref-mod STORE backing (the W2 adversarial-review fix, DEVLOG 595): a MOVE into a reference-modified
    /// slice of a numeric USAGE-DISPLAY item writes CHARACTERS into the item's character positions
    /// (§8.4.3.3.4 GR6 — the unique result is an elementary alphanumeric item). Without image backing the resolver
    /// wraps <c>NumericImagePlace(long)</c> and the spliced image ROUND-TRIPS through the <c>long</c> on
    /// store, silently losing any non-digit deposit (<c>MOVE SPACE TO N(1:2)</c> left N's digits — and the
    /// observable result flipped with whether UNRELATED code elsewhere image-backed the item). Mark the
    /// underlying item <c>StoreAsImage</c> at bind time for EVERY sender kind — digits round-trip either way,
    /// so the flag is safe, and the byte-semantics model (a ref-mod store is a character-cell write) is what
    /// §14.6.8's fixed-width transfer implies. Same substrate + eligibility as the figurative pass; a Tier-B
    /// window already writes character images; a REDEFINES-class member keeps its (already-run) tier
    /// classification — the emitter's narrow loud guard covers that residue.
    /// </summary>
    private void MarkRefModStoreImage(IReadOnlyList<Place> targets)
    {
        foreach (var t in targets)
            if (t is RefModPlace rm
                && rm.Item is { Class: null, Pic: { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } } item)
                ctx.Data.MarkImageForced(item);   // the collected image fact
    }
}
