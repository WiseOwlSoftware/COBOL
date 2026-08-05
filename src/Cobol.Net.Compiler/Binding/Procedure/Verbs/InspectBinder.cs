// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The INSPECT verb binder (P7 Step 10c — the FIRST real binder collaborator over
/// <see cref="BinderContext"/>, extracted from the <c>StatementBinder.Inspect</c> partial; the census's
/// cleanest file proves the Step-9-style pattern: ctx + transitional host edges for the shared operand
/// spine). The edition-invariant SR error-halves live in <c>StatementValidation</c> (pure checks — the
/// SR6/SR9 figurative-expansion operand REWRITE is bind logic and stays here); the 0845 BACKWARD gate moved
/// VERBATIM (the pass-folding is Exec Step E's scope). The <c>BoundInspect*</c> records/enums stayed in
/// <c>Binding/Bound/BoundInspect.cs</c> — the Tally/Replace enum ordinals are runtime ABI.</summary>
internal sealed class InspectBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind INSPECT (ISO §14.9.22): resolve identifier-1 (SR1 — an alphanumeric group or an elementary
    /// usage-DISPLAY item), flatten the TALLYING/REPLACING operands across all counters in SOURCE order (the GR8a
    /// shared-cycle order), and bind CONVERTING to its from/to maps (GR20). BACKWARD is 2023-only
    /// (VERSION_CHANGE_REFERENCE row 77 / E.3.3 item 34); TRAILING and tallying FIRST are not in any ISO format —
    /// both fail loud rather than silently aliasing to ALL.</summary>
    public BoundStatement Bind(Core.InspectStatementContext ins)
    {
        // ── identifier-1 may be a FUNCTION-IDENTIFIER, but only where the statement does not MODIFY it (PB10).
        // §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so this position admits one; §8.4.3.2.3
        // SR1 bars it from a RECEIVING operand. INSPECT splits BY FORMAT, and the split is derived, not assumed:
        // §14.9.22.4 GR1 concedes only that "for purposes of determining its length, identifier-1 is treated as a
        // sending data item" (a scoped concession that would be pointless if it were generally sending); GR7 has
        // each match "tallied (format 1) or replaced by literal-3 (format 2)"; and GR20 makes format 4 execute AS
        // a format 2 over the same identifier-1. So Format 1 (TALLYING alone) SENDS and Formats 2/3/4 RECEIVE.
        // ⚠ The screen keys on the PHRASES PRESENT rather than on a format number, because that is what the
        // grammar gives us and it is the same predicate the emitter already computes as `mutated` — one fact,
        // not two representations of it.
        bool modifies = ins.inspectReplacingPhrase() is not null || ins.inspectConvertingPhrase() is not null;
        if (ins.functionCall() is { } fnTarget)
        {
            if (modifies)
            {
                ctx.Edition.Error(DiagnosticCatalog.FunctionIdentifierReceiving,
                    $"INSPECT identifier-1 is the function-identifier '{fnTarget.GetText()}', but this INSPECT "
                    + (ins.inspectConvertingPhrase() is not null
                        ? "CONVERTS it (ISO §14.9.22.2 Format 4, which §14.9.22.4 GR20 executes as a Format 2 "
                          + "over the same identifier-1)"
                        : "REPLACES characters in it (ISO §14.9.22.2 Format 2/3, §14.9.22.4 GR7)")
                    + " — a receiving operand, which ISO §8.4.3.2.3 SR1 bars a function-identifier from. A "
                    + "function-identifier IS legal as INSPECT identifier-1 in Format 1 (TALLYING only)");
                return new BoundUnsupported(
                    $"INSPECT REPLACING/CONVERTING over the function-identifier '{fnTarget.GetText()}' (ISO §8.4.3.2.3 SR1)");
            }
            // Format 1: identifier-1 is only READ. It binds as an ordinary sending operand and flows to the
            // emitter's AsString read; nothing stores back, so no Place is needed. SR1's usage constraint is
            // satisfied by construction — a function's returned value is a temporary elementary item of the
            // function's own category (§15.4), never a binary/packed/float/index storage form.
            return BindPhrases(host.Intrinsic.IntrinsicOperand(fnTarget), ins);
        }
        if (ctx.Refs.Resolve(ins.dataReference()) is not { } target)
            return new BoundUnsupported($"INSPECT of unresolvable item '{ins.dataReference().GetText()}'");
        // SR1: identifier-1 is an alphanumeric/national group or an elementary usage DISPLAY/NATIONAL item — a
        // binary/packed/float/index elementary item has no character image to inspect. USAGE NATIONAL joined
        // the admitted set at Phase 4a (M2-DATA-3): a national item is a plain string under D-N1, so the
        // character-based INSPECT machinery applies unchanged (the cross-class operand-MIX validation across
        // the whole operand set is residue #12). Display-form boolean items pass the Display arm.
        // ⛔ DA7 — A COMPILE-TIME DIAGNOSTIC, not a run-time stage. The verdict was always right (SR1 genuinely bars
        // an elementary binary/packed/float/index identifier-1), but returning BoundUnsupported meant the illegal
        // program COMPILED CLEAN and threw only when control reached the INSPECT. The standard promises a syntax
        // error, so a user was getting a crash where they were owed a diagnostic. Edition-invariant — SR1 is
        // unchanged at 85/2002/2014/2023, so this is deliberately NOT gated. Note the rule constrains only an
        // ELEMENTARY operand: `target.Item.Pic is { }` is false for a GROUP, so an alphanumeric group — including
        // one holding a BINARY/PACKED leaf, which SR1 admits outright as "an alphanumeric or national group item" —
        // never reaches this check.
        if (target.Item.Pic is { } tp && tp.Usage is not (Usage.Display or Usage.National))
        {
            ctx.Edition.Error(DiagnosticCatalog.CharacterOperandUsage,
                $"INSPECT identifier-1 '{target.Item.CobolName}' is an elementary item of USAGE {tp.Usage}, which "
                + "has no character image; SR1 admits an alphanumeric/national GROUP item or an ELEMENTARY item of "
                + "usage display or national (ISO §14.9.22.3 SR1)");
            return new BoundUnsupported(
                $"INSPECT identifier-1 '{target.Item.CobolName}' of USAGE {tp.Usage} (ISO §14.9.22.3 SR1 — usage display or national only)");
        }

        return BindPhrases(new BoundFieldOperand(target), ins);
    }

    /// <summary>Bind the TALLYING / REPLACING / CONVERTING phrases over an already-bound identifier-1. Split out
    /// from <see cref="Bind"/> so the DATA-REFERENCE and FUNCTION-IDENTIFIER targets share one body rather than
    /// two copies of the phrase walk (PB10) — the target's SHAPE is the only thing that differs, and it differs
    /// only in whether a Place exists to store back into.</summary>
    private BoundStatement BindPhrases(BoundOperand targetOperand, Core.InspectStatementContext ins)
    {
        bool backward = ins.BACKWARD() is not null;   // inspect-backward-2023: the pass owns the edition gate (Exec Step E)

        var tallying = new List<BoundInspectTally>();
        if (ins.inspectTallyingPhrase() is { } tallyPhrase)
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                if (ctx.Refs.Resolve(item.dataReference()) is not { } counter)
                    return new BoundUnsupported($"INSPECT TALLYING counter '{item.dataReference().GetText()}'");
                ctx.Validation.CheckInspectTallyCounter(counter);   // SR5 — pure check; binding continues
                foreach (var fc in item.inspectForClause())
                {
                    // GR10: ALL and LEADING are transitive across the bare operands that follow them until the
                    // next adjective. The format requires an adjective on the first operand, so the All seed is
                    // only a lenient default for that (ungrammatical) case.
                    InspectTallyKind last = InspectTallyKind.All;
                    foreach (var cp in fc.inspectCountPhrase())
                    {
                        var (before, after) = InspectDelimiters(cp.inspectDelimiters());
                        if (cp.CHARACTERS() is not null)
                        {
                            tallying.Add(new BoundInspectTally(counter, InspectTallyKind.Characters, null, before, after));
                            continue;
                        }
                        if (cp.FIRST() is not null || cp.TRAILING() is not null)
                            return new BoundUnsupported("INSPECT TALLYING FOR "
                                + (cp.FIRST() is not null ? "FIRST" : "TRAILING (non-ISO extension)")
                                + " — ISO §14.9.22.2 Format 1 admits ALL / LEADING / CHARACTERS only");
                        InspectTallyKind kind = cp.ALL() is not null ? InspectTallyKind.All
                            : cp.LEADING() is not null ? InspectTallyKind.Leading
                            : last;   // a bare operand inherits the governing adjective (GR10)
                        if (cp.ALL() is not null || cp.LEADING() is not null) last = kind;
                        tallying.Add(new BoundInspectTally(counter, kind, InspectCharOperand(cp.inspectChar()).Op, before, after));
                    }
                }
            }

        var replacing = new List<BoundInspectReplace>();
        if (ins.inspectReplacingPhrase() is { } replPhrase)
        {
            // GR16: ALL, FIRST, and LEADING are transitive across following bare operands until the next adjective.
            InspectReplaceKind? last = null;
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                var (before, after) = InspectDelimiters(item.inspectDelimiters());
                if (item.CHARACTERS() is not null)
                {
                    var (rep, _) = InspectCharOperand(item.inspectChar(0));
                    ctx.Validation.CheckInspectCharactersReplacement(rep);   // SR7 — pure check
                    replacing.Add(new BoundInspectReplace(InspectReplaceKind.Characters, null, rep, before, after));
                    continue;
                }
                if (item.TRAILING() is not null)
                    return new BoundUnsupported("INSPECT REPLACING TRAILING (non-ISO extension — not in ISO §14.9.22.2 Format 2)");
                InspectReplaceKind kind = item.ALL() is not null ? InspectReplaceKind.All
                    : item.FIRST() is not null ? InspectReplaceKind.First
                    : item.LEADING() is not null ? InspectReplaceKind.Leading
                    : last ?? InspectReplaceKind.All;   // bare operand pair — GR16 (All only when ungrammatically first)
                if (item.ALL() is not null || item.FIRST() is not null || item.LEADING() is not null) last = kind;

                var (pat, _) = InspectCharOperand(item.inspectChar(0));
                var (rep2, figurative) = InspectCharOperand(item.inspectChar(1));
                if (figurative && rep2 is BoundStringLiteral f && InspectStaticWidth(pat) is { } wp && wp != f.Value.Length)
                    // SR6 / GR14: a figurative literal-3 is expanded (or contracted) to the size of literal-1 /
                    // identifier-3 — e.g. ALL "AB" BY SPACES replaces with "  ". (The legacy skipped the operand.)
                    rep2 = new BoundStringLiteral(new string(f.Value[0], wp));
                else
                    ctx.Validation.CheckInspectReplacingSize(pat, rep2, figurative);   // SR6 — pure check
                replacing.Add(new BoundInspectReplace(kind, pat, rep2, before, after));
            }
        }

        BoundInspectConvert? converting = null;
        if (ins.inspectConvertingPhrase() is { } conv)
        {
            var (from, _) = InspectCharOperand(conv.inspectChar(0));
            var (to, figurative) = InspectCharOperand(conv.inspectChar(1));
            if (figurative && to is BoundStringLiteral f && InspectStaticWidth(from) is { } wf && wf != f.Value.Length)
                to = new BoundStringLiteral(new string(f.Value[0], wf));   // SR9/GR22 — figurative literal-5 takes literal-4's size
            else
                ctx.Validation.CheckInspectConvertingSize(from, to, figurative);   // SR9 — pure check
            BoundOperand? before = null, after = null;
            foreach (var ba in conv.inspectBeforeAfterPhrase())
            {
                var (op, _) = InspectCharOperand(ba.inspectChar());
                if (ba.BEFORE() is not null) before = op;
                else after = op;
            }
            converting = new BoundInspectConvert(from, to, before, after);
        }

        return new BoundInspect(targetOperand, tallying, replacing, converting, backward);
    }

    /// <summary>Bind a per-operand BEFORE/AFTER delimiter pair (ISO §14.9.22.2 after-before-phrase; both may
    /// appear on one operand, in either order — disambiguated by token index, since the grammar's two alternatives
    /// share the merged BEFORE/AFTER accessors). INITIAL is a noise word.</summary>
    private (BoundOperand? Before, BoundOperand? After) InspectDelimiters(Core.InspectDelimitersContext? c)
    {
        if (c is null) return (null, null);
        var chars = c.inspectChar();
        if (c.BEFORE() is { } b && c.AFTER() is { } a)
        {
            var first = InspectCharOperand(chars[0]).Op;
            var second = InspectCharOperand(chars[1]).Op;
            return b.Symbol.TokenIndex < a.Symbol.TokenIndex ? (first, second) : (second, first);
        }
        if (c.BEFORE() is not null) return (InspectCharOperand(chars[0]).Op, null);
        if (c.AFTER() is not null) return (null, InspectCharOperand(chars[0]).Op);
        return (null, null);
    }

    /// <summary>Bind an INSPECT operand (identifier-3..7 / literal-1..5 / figurative): a figurative constant is an
    /// implicit ONE-character item (ISO §14.9.22.3 SR3 — and a figurative beginning with ALL is forbidden there);
    /// an identifier reads its FULL raw image at run time (GR5/GR6 — no trimming; GR4d de-signs a signed numeric
    /// operand at the read). <c>Figurative</c> reports the figurative origin so SR6/SR9 can expand a replacement
    /// to the pattern size.</summary>
    private (BoundOperand Op, bool Figurative) InspectCharOperand(Core.InspectCharContext c)
    {
        var fig = c.figurativeConstant() ?? c.literal()?.nonNumericLiteral()?.figurativeConstant();
        if (fig is not null)
        {
            if (fig.STRINGLIT() is not null)
                return (new BoundOperandError(
                    "INSPECT operand ALL \"literal\" (ISO §14.9.22.3 SR3 — a figurative constant beginning with ALL is not permitted)"), false);
            return (new BoundStringLiteral(InspectFigurativeChar(fig).ToString()), true);
        }
        // ⛔ A FUNCTION-IDENTIFIER OPERAND (ISO §8.4.3.1.2 Format 1; fix-queue PB45). §14.9.22.2 writes these as
        // `identifier-n | literal-n` and every inspectChar use is SENDING, so `INSPECT S TALLYING N FOR ALL
        // FUNCTION UPPER-CASE(P)` is conforming. It used to be a PARSE error; once the grammar admitted it, this
        // arm was still missing and the operand fell through to the not-implemented tail, which reported a
        // MISLEADING reason — "a numeric literal is not a valid INSPECT literal" — for something that is neither
        // numeric nor a literal. Routed through the ONE intrinsic-operand entry the other verbs use.
        if (c.functionCall() is { } fc) return (host.Intrinsic.IntrinsicOperand(fc), false);
        // §8.8.3.3 GR3: a concatenation expression is the equivalent single literal — fold and use it as the
        // INSPECT literal operand (not Figurative: the fold result is a plain literal value).
        if (c.literal()?.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return (host.Expr.ConcatOperand(ce), false);
        if (c.literal()?.nonNumericLiteral()?.STRINGLIT() is { } s)
            return (new BoundStringLiteral(CobolLiteral.Decode(s.GetText())), false);
        // National/boolean literal operands decode char-correct (the class-mix SR validation across the
        // INSPECT operand set — §14.9.22.3 SR2/SR3's per-class forms — is named Phase-4a residue #12).
        if (c.literal()?.nonNumericLiteral()?.NATLIT() is { } nlit)
            return (host.Expr.NationalLiteralOperand(nlit.GetText()), false);
        if (c.literal()?.nonNumericLiteral()?.BOOLLIT() is { } blit)
            return (host.Expr.BooleanLiteralOperand(blit.GetText()), false);
        if (c.dataReference() is { } dref)
        {
            if (ctx.Refs.Resolve(dref) is not { } p)
                return (new BoundOperandError($"INSPECT operand '{dref.GetText()}'"), false);
            ctx.Validation.CheckInspectOperandUsage(p, dref.GetText());   // SR2 — pure check
            return (new BoundFieldOperand(p), false);
        }
        // The grammar admits a numeric literal here; SR3 does not (alphanumeric/boolean/national literals only).
        return (new BoundOperandError(
            $"INSPECT operand '{c.GetText()}' (ISO §14.9.22.3 SR3 — a numeric literal is not a valid INSPECT literal)"), false);
    }

    /// <summary>The single character a figurative INSPECT operand denotes (ISO §14.9.22.3 SR3 — an implicit
    /// one-character item; HIGH/LOW-VALUE are U+00FF/U+0000 per COBOLNET_DESIGN §14.9, matching the emitter's
    /// figurative fills).</summary>
    private static char InspectFigurativeChar(Core.FigurativeConstantContext fig) =>
        fig.ZERO() is not null ? '0'
        : fig.SPACE() is not null ? ' '
        : fig.HIGH_VALUE() is not null ? '\u00ff'
        : fig.LOW_VALUE() is not null || fig.NULL_() is not null ? '\u0000'
        : fig.QUOTE_() is not null ? '"'
        : ' ';

    /// <summary>The compile-time-known character width of an INSPECT operand's run-time image, or null. A literal
    /// is its own length; an identifier's raw image width is static — alphanumeric/edited items their PIC length,
    /// a numeric item its digit count (the GR4d de-signed image excludes any separate sign position), a group its
    /// image width. Sizes both SR6/SR9 figurative expansion and the literal/literal equal-size checks rest on.</summary>
    private static int? InspectStaticWidth(BoundOperand op) => op switch
    {
        BoundStringLiteral s => s.Value.Length,
        BoundFieldOperand f when f.Place.Item.IsGroup => f.Place.Item.ImageWidth,
        BoundFieldOperand { Place.Item.Pic: { Category: PicCategory.Numeric } pic } => pic.Digits,
        BoundFieldOperand { Place.Item.Pic: { } pic } => pic.Length,
        _ => null,
    };
}
