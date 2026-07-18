// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The boolean-expression / boolean-operator binder (Phase-4 track (a) increment 2; ISO §8.8.2 the operators
/// B-AND/B-OR/B-XOR/B-NOT, §14.9.8 Format-2 boolean COMPUTE, §8.8.4.2.2 the boolean relation, §8.8.4.3 the
/// simple boolean condition). A boolean expression binds into the <see cref="BoundBoolExpr"/> value channel
/// (a '0'/'1' string world, D-B1) — never the numeric or DISPLAY operand channels. Operand SHAPES are validated
/// here (the COBOLNET1511 constraint band); the {is2002()}?-gated grammar tiers enforce the §8.8.2 formation
/// rules structurally, and the operators are unreachable below 2002 (so no binder-side introduction gate — the
/// XOR precedent: the grammar predicate + the parse-layer hint ARE the gate).
/// P7 Step 10o: the WHOLE condition/relation/boolean channel as ONE collaborator over
/// <see cref="BinderContext"/> — the .Boolean partial + the core channel (AbbrevCarry / BindCondition family
/// / BindComparison / BindSoleOperandCondition / <see cref="CheckedRelational"/> [THE one BoundRelational
/// checkpoint] / ConditionOf / MapOperator / SoleDataRef / SoleNumLiteral) merged, because the two halves are
/// bidirectionally coupled (BindPrimary→BindPrimaryBoolean; the boolean alt→CheckedRelational/
/// ComparisonOperandOf). AbbrevCarry is hoisted out of the god class WITH its source-order threading intact.
/// RECORDED deviations from the plan text: (a) the collaborator host edges did NOT flip here — the host keeps
/// forwarders and everything flips at once at 10t (less churn, identical behavior); (b) the 1511/relational
/// SR bodies stay INSIDE this binder (no longer god-class inline — the pure StatementValidation lift is
/// deferred to the 10t sweep). The VersionConformancePass HasBoolOp duplicate is DELIBERATE — do not
/// unify.</summary>
internal sealed class ConditionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind a <c>booleanExpression</c> (ISO §8.8.2, precedence B-NOT &gt; B-AND &gt; B-XOR &gt; B-OR):
    /// left-to-right within each tier (rule 7c). Rule 4 — both operands of a binary op shall not both be
    /// <c>ALL "literal"</c> — is checked at each combination.</summary>
    internal BoundBoolExpr BindBoolExpr(Core.BooleanExpressionContext ctx)
    {
        var terms = ctx.booleanXorTerm();
        BoundBoolExpr acc = BindBoolXor(terms[0]);
        for (int i = 1; i < terms.Length; i++)
            acc = MakeBoolBinary(acc, '|', BindBoolXor(terms[i]));
        return acc;
    }

    private BoundBoolExpr BindBoolXor(Core.BooleanXorTermContext ctx)
    {
        var terms = ctx.booleanAndTerm();
        BoundBoolExpr acc = BindBoolAnd(terms[0]);
        for (int i = 1; i < terms.Length; i++)
            acc = MakeBoolBinary(acc, '^', BindBoolAnd(terms[i]));
        return acc;
    }

    private BoundBoolExpr BindBoolAnd(Core.BooleanAndTermContext ctx)
    {
        var terms = ctx.booleanShiftTerm();
        BoundBoolExpr acc = BindBoolShift(terms[0]);
        for (int i = 1; i < terms.Length; i++)
            acc = MakeBoolBinary(acc, '&', BindBoolShift(terms[i]));
        return acc;
    }

    /// <summary>Bind a boolean shift term (ISO §8.8.2 rule 8, COBOL-2023): a boolean factor followed by zero or more
    /// <c>(B-SHIFT-L|R|LC|RC) integer</c> suffixes, left-associative. Rule 5 — the first operand of a shift shall not
    /// be the figurative ALL literal (COBOLNET1511). The 2023 introduction is gated in the VersionConformancePass
    /// parse arm (HasShiftOp), so no binder-side gate here (the boolean-operators precedent).</summary>
    private BoundBoolExpr BindBoolShift(Core.BooleanShiftTermContext shift)
    {
        BoundBoolExpr acc = BindBoolFactor(shift.booleanFactor());
        // Rule 7b (§8.8.2): a shift's precedence is that of the operator IMMEDIATELY TO ITS LEFT — context-sensitive,
        // not a fixed tier. The fixed booleanShiftTerm tier realizes the UNMIXED default correctly, but a shift at the
        // SAME level as a binary boolean operator (B-AND/B-OR/B-XOR — i.e. its enclosing chain has >1 operand) would
        // be silently mis-grouped. Reject that loudly (LOUD-not-silently-wrong); the user parenthesizes to
        // disambiguate. A parenthesized shift starts a fresh booleanExpression, so its chain is single-operand here.
        if (shift.booleanShiftSuffix().Length > 0 && ShiftMixedWithBinary(shift))
            ctx.Edition.Error("COBOLNET1569", "a boolean shift operator (B-SHIFT-L/R/LC/RC) combined with a binary "
                + "boolean operator (B-AND/B-OR/B-XOR) in the same expression is not supported — its precedence is "
                + "context-sensitive (ISO §8.8.2 rule 7b); parenthesize the shift operand to disambiguate");
        foreach (var suf in shift.booleanShiftSuffix())
        {
            var kind = suf.B_SHIFT_LC() is not null ? BoolShiftKind.LeftCircular
                     : suf.B_SHIFT_RC() is not null ? BoolShiftKind.RightCircular
                     : suf.B_SHIFT_L() is not null ? BoolShiftKind.Left
                     : BoolShiftKind.Right;
            if (acc is BoundBoolAll)
                ctx.Edition.Error("COBOLNET1511", "the first operand of a boolean shift operation shall not be the "
                    + "figurative constant ALL literal (ISO §8.8.2 rule 5)");
            acc = new BoundBoolShift(acc, kind, host.Expr.BindExpr(suf.arithmeticExpression()));
        }
        return acc;
    }

    private BoundBoolExpr BindBoolFactor(Core.BooleanFactorContext ctx)
    {
        if (ctx.B_NOT() is not null)
        {
            var inner = BindBoolFactor(ctx.booleanFactor());
            // B-NOT ALL … constant-folds — ALL is positionless (§8.3.3.6.4), so flip the pattern.
            return inner is BoundBoolAll all ? new BoundBoolAll(FlipBits(all.Bits)) : new BoundBoolNot(inner);
        }
        if (ctx.booleanExpression() is { } paren) return BindBoolExpr(paren);
        return BindBoolOperandValue(ctx.valueOperand());
    }

    /// <summary>True when this shift term sits at the same precedence level as a binary boolean operator — its
    /// enclosing B-AND chain has &gt;1 shift term, or the XOR chain &gt;1 AND term, or the OR chain &gt;1 XOR term.
    /// Parentheses start a fresh <c>booleanExpression</c>, so a parenthesized shift's chains are single-operand here
    /// and it is (correctly) not flagged (§8.8.2 rule 7b context-sensitivity).</summary>
    private static bool ShiftMixedWithBinary(Core.BooleanShiftTermContext shift) =>
        (shift.Parent is Core.BooleanAndTermContext a && a.booleanShiftTerm().Length > 1)
        || (shift.Parent?.Parent is Core.BooleanXorTermContext x && x.booleanAndTerm().Length > 1)
        || (shift.Parent?.Parent?.Parent is Core.BooleanExpressionContext e && e.booleanXorTerm().Length > 1);

    /// <summary>Rule 4 (§8.8.2 :9364): both operands of a binary boolean op shall not both be ALL "literal".</summary>
    private BoundBoolExpr MakeBoolBinary(BoundBoolExpr left, char op, BoundBoolExpr right)
    {
        if (left is BoundBoolAll && right is BoundBoolAll)
            ctx.Edition.Error("COBOLNET1511", "both operands of a boolean operator shall not be ALL literals "
                + "(ISO §8.8.2 rule 4)");
        return new BoundBoolBinary(left, op, right);
    }

    /// <summary>Resolve a boolean-expression leaf operand (ISO §8.8.2 operand list): a boolean literal, the
    /// figurative ZERO / <c>ALL B"…"</c>, or a category-boolean data item. Anything else — a non-boolean item,
    /// a plain string, an arithmetic expression, another figurative — is COBOLNET1511.</summary>
    private BoundBoolExpr BindBoolOperandValue(Core.ValueOperandContext vo)
    {
        var nn = vo.nonNumericLiteral();
        // A boolean concatenation expression (B"01" & B"10", ISO §8.8.3) folds to its equivalent single
        // boolean literal at compile time (§8.8.3.3 GR3) — the §8.8.2 operand list admits a boolean literal,
        // and the folded result IS one. A non-boolean concat is not a boolean operand (1511, below).
        if (nn?.concatenationExpression() is { } ce && ConcatFolder.ClassOf(ce) is PicCategory.Boolean)
            return new BoundBoolLiteral(ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating).Value);
        if (nn?.BOOLLIT() is { } bl)
            // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
            // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
            return new BoundBoolLiteral(CobolLiteral.Decode(bl.GetText()));
        if (nn?.figurativeConstant() is { } fig)
        {
            if (fig.ZERO() is not null) return new BoundBoolAll("0");   // figurative ZERO — boolean zeros by context (§8.3.3.6.4 GR4)
            if (fig.BOOLLIT() is { } allBl) return new BoundBoolAll(CobolLiteral.Decode(allBl.GetText()));   // ALL B"…"
            return new BoundBoolError($"figurative constant '{fig.GetText()}' in a boolean expression "
                + "(ISO §8.8.2 — only ZERO and ALL B\"…\" are boolean figuratives)");
        }
        // A sole data reference to a category-boolean item.
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref && ctx.Refs.Resolve(dref) is { } p)
        {
            var cat = p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category;
            if (cat is PicCategory.Boolean) return new BoundBoolRef(p);
            ctx.Edition.Error("COBOLNET1511", $"operand '{dref.GetText()}' in a boolean expression is not a "
                + "boolean data item (ISO §8.8.2 — boolean operands only)");
            return new BoundBoolError($"non-boolean operand '{dref.GetText()}'");
        }
        ctx.Edition.Error("COBOLNET1511", $"'{vo.GetText()}' is not a valid boolean operand — a boolean "
            + "expression admits boolean items, boolean literals, and the figurative ZERO / ALL B\"…\" only "
            + "(ISO §8.8.2)");
        return new BoundBoolError($"boolean operand '{vo.GetText()}'");
    }

    /// <summary>True when a <c>comparisonOperand</c> / <c>valueOperand</c> is a BOOLEAN-valued operand — a
    /// boolean expression (B-op tier) OR a sole category-boolean item / boolean literal — so the relation
    /// binder routes it through the boolean channel. (A bare category-boolean item parses as a valueOperand,
    /// not booleanExpression, so this inspects both.)</summary>
    private bool IsBooleanValueOperand(Core.ValueOperandContext vo)
    {
        var nn = vo.nonNumericLiteral();
        if (nn?.BOOLLIT() is not null) return true;
        if (nn?.figurativeConstant()?.BOOLLIT() is not null) return true;
        // A concatenation expression whose class is boolean (§8.8.3.3 GR1) routes through the boolean channel
        // like the equivalent single B"…" literal it folds to (GR3). ClassOf is diagnostic-free — the fold
        // (and its SR diagnostics) happens exactly once, on the bind path this predicate selects.
        if (nn?.concatenationExpression() is { } ce) return ConcatFolder.ClassOf(ce) is PicCategory.Boolean;
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref
            && ctx.Refs.Resolve(dref) is { } p)
            return (p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category) is PicCategory.Boolean;
        return false;
    }

    /// <summary>The set of category-boolean ITEMS referenced in a bound boolean expression, for the §14.9.8 GR3
    /// COMPUTE-store width (the max static boolean positions; literal/ALL operands do not count — GR3).</summary>
    internal static int Gr3Width(BoundBoolExpr e) => e switch
    {
        BoundBoolRef r => r.Place is RefModPlace ? RefModLen(r.Place) : r.Place.Item.Pic?.Length ?? 0,
        BoundBoolBinary b => System.Math.Max(Gr3Width(b.Left), Gr3Width(b.Right)),
        BoundBoolNot n => Gr3Width(n.Operand),
        BoundBoolShift s => Gr3Width(s.Operand),   // rule 9 — result length = the FIRST operand (the count adds none)
        _ => 0,   // literals / ALL / error contribute no ITEM width
    };

    /// <summary>The static length of a ref-mod boolean operand (its own §8.4.3.3 unique data item at the
    /// ref-mod length); a dynamic/computed length is not statically known — GR3 stages that leg (returns the
    /// inner item's full length as a conservative width; the dynamic case is named residue).</summary>
    private static int RefModLen(Place p) =>
        p is RefModPlace { Length: { } lit } && int.TryParse(lit, out int n) ? n
        : p.Item.Pic?.Length ?? 0;

    private static string FlipBits(string bits)
    {
        var arr = bits.ToCharArray();
        for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] == '1' ? '0' : '1';
        return new string(arr);
    }

    /// <summary>Bind a SIMPLE boolean condition (ISO §8.8.4.3): a bare boolean expression used as a condition,
    /// true iff its value is boolean 1 (GR1). SR1 (:9810) — every referenced boolean item AND literal shall be
    /// of length 1; a wider operand is COBOLNET1511. Called from the sole-operand comparison path.</summary>
    private BoundCondition BindSimpleBooleanCondition(BoundBoolExpr expr)
    {
        if (!BoolExprAllLengthOne(expr))
            ctx.Edition.Error("COBOLNET1511", "a simple boolean condition shall reference only boolean items "
                + "and literals of length 1 (ISO §8.8.4.3 SR1)");
        return new BoundBooleanCondition(expr);
    }

    /// <summary>SR1 check: every ITEM and LITERAL in the boolean expression has length 1 (§8.8.4.3 SR1). ALL
    /// figuratives are positionless (they materialize to length 1 against a length-1 sibling) — they pass.</summary>
    private static bool BoolExprAllLengthOne(BoundBoolExpr e) => e switch
    {
        BoundBoolLiteral l => l.Bits.Length == 1,
        BoundBoolRef r => (r.Place is RefModPlace ? RefModLen(r.Place) : r.Place.Item.Pic?.Length ?? 0) == 1,
        BoundBoolBinary b => BoolExprAllLengthOne(b.Left) && BoolExprAllLengthOne(b.Right),
        BoundBoolNot n => BoolExprAllLengthOne(n.Operand),
        BoundBoolShift s => BoolExprAllLengthOne(s.Operand),   // shift preserves length (rule 9)
        BoundBoolAll => true,   // positionless — materializes to the sibling's length
        _ => true,              // error nodes already reported
    };

    /// <summary>Bind the boolExprAhead()-gated <c>primaryCondition</c> boolean alternative (ISO §8.8.4.2.2
    /// relation / §8.8.4.3 simple condition): <c>booleanExpression (comparisonOperator booleanExpression)?</c>.
    /// Only an expression that ACTUALLY contains a B-operator uses the boolean channel — the predicate
    /// guarantees at least one B-op somewhere, but an INDIVIDUAL relation operand may still be B-op-free (e.g.
    /// the RHS literal in `(a B-AND b) = c`), so each side unwraps to a normal operand when it has no B-op.</summary>
    private BoundCondition BindPrimaryBoolean(Core.BooleanExpressionContext[] be, Core.ComparisonOperatorContext? opCtx, AbbrevCarry carry)
    {
        carry.Reset();
        // The COBOL-2002 boolean-operator introduction gate (BooleanOperators2002) fires on RECOGNITION in the
        // VersionConformancePass parse-arm (VisitPrimaryCondition, be.Any(HasBoolOp) at the same altitude); Step
        // 14h.4b. A B-op-free relation operand rides its own channel here regardless.
        if (opCtx is not null && be.Length >= 2)
        {
            BoundOperand left = BindBoolOrValueOperand(be[0]);
            string op = MapOperator(opCtx.GetText());
            BoundOperand right = BindBoolOrValueOperand(be[1]);
            // A boolean relation (§8.8.4.2.2 Format 2): equality-only, both operands boolean-valued.
            if (left is BoundBoolOperand || right is BoundBoolOperand)
            {
                if (op is not ("==" or "!="))
                    ctx.Edition.Error("COBOLNET1511", "a boolean relation admits only [NOT] EQUAL / '=' / '<>' "
                        + "(ISO §8.8.4.2.2 Format 2 — no ordering is defined for boolean values)");
                else if (!BoolValued(left) || !BoolValued(right))
                    ctx.Edition.Error("COBOLNET1511", "both operands of a boolean relation shall be "
                        + "boolean-valued (ISO §8.8.4.2.2)");
            }
            return CheckedRelational(left, op, right);
        }
        // A bare boolean expression ⇒ a simple boolean condition (§8.8.4.3).
        if (HasBoolOp(be[0])) return BindSimpleBooleanCondition(BindBoolExpr(be[0]));
        // Defensive: a B-op-free bare operand (the predicate should have excluded it) unwraps to the normal
        // sole-operand resolution.
        var vo = UnwrapBareBool(be[0]);
        return BindSoleOperandCondition(vo, () => vo is not null ? ComparisonOperandOf(vo) : new BoundOperandError("operand"), carry);
    }

    /// <summary>Bind a boolean-expression RELATION operand: a real boolean expression (contains a B-op) →
    /// <see cref="BoundBoolOperand"/>; a B-op-free operand → its normal comparison-operand binding (so a plain
    /// boolean-item / literal / numeric operand rides its existing channel).</summary>
    private BoundOperand BindBoolOrValueOperand(Core.BooleanExpressionContext ctx) =>
        HasBoolOp(ctx) ? new BoundBoolOperand(BindBoolExpr(ctx)) : ComparisonOperandOf(UnwrapBareBool(ctx));

    /// <summary>True when a bound relation operand is BOOLEAN-VALUED (a boolean expression, a category-boolean
    /// item incl. ref-mod, a boolean literal, or figurative ZERO — boolean by context, §8.3.3.6.4 GR4).</summary>
    private static bool BoolValued(BoundOperand o) => o switch
    {
        BoundBoolOperand => true,
        BoundStringLiteral { Category: PicCategory.Boolean } => true,
        BoundFigurative { Kind: 'Z' } => true,
        BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category is PicCategory.Boolean,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Boolean,
        _ => false,
    };

    /// <summary>True when a boolean-expression subtree contains any B-operator token — the discriminator
    /// between a genuine boolean expression and a bare operand parsed through the booleanExpression rule.</summary>
    private static bool HasBoolOp(Antlr4.Runtime.Tree.IParseTree t)
    {
        if (t is Antlr4.Runtime.Tree.ITerminalNode term)
            return term.Symbol.Type is Core.B_AND or Core.B_OR or Core.B_XOR or Core.B_NOT;
        for (int i = 0; i < t.ChildCount; i++)
            if (HasBoolOp(t.GetChild(i))) return true;
        return false;
    }

    /// <summary>The single leaf <c>valueOperand</c> of a B-op-FREE boolean expression (walking single-child
    /// tiers and paren groups), for re-binding as a normal operand; null if the shape is not a bare operand.</summary>
    private static Core.ValueOperandContext? UnwrapBareBool(Core.BooleanExpressionContext ctx)
    {
        var xor = ctx.booleanXorTerm();
        if (xor.Length != 1) return null;
        var and = xor[0].booleanAndTerm();
        if (and.Length != 1) return null;
        var shift = and[0].booleanShiftTerm();
        // A bare operand has a single shift term with NO shift suffix (a shift op means it is a real expression).
        if (shift.Length != 1 || shift[0].booleanShiftSuffix().Length != 0) return null;
        return UnwrapFactor(shift[0].booleanFactor());
    }

    private static Core.ValueOperandContext? UnwrapFactor(Core.BooleanFactorContext f)
    {
        if (f.valueOperand() is { } vo) return vo;
        if (f.booleanExpression() is { } inner) return UnwrapBareBool(inner);   // ( … ) group
        return null;   // B-NOT factor — has a B-op, never reaches here
    }

    /// <summary>The carried subject + relational operator for ABBREVIATED COMBINED RELATION CONDITIONS (ISO §8.8.4.12).
    /// In a paren-free sequence of relations joined by AND/OR/XOR, a succeeding relation may omit the subject (operator
    /// stated, e.g. the <c>&lt; C</c> in <c>A &gt; B OR &lt; C</c>) or the subject AND operator (a bare operand, e.g.
    /// <c>A = B AND C</c> ≡ <c>A = C</c>). GR1 inserts the last STATED subject and the last STATED operator.
    /// <see cref="Subject"/> is set only by a fully-stated relation; <see cref="Op"/> by a full OR an abbreviated
    /// relation. A complete non-relational simple condition (class / sign / condition-name / parenthesized) terminates
    /// the insertion. Threaded left-to-right (source order) as a mutable holder.</summary>
    private sealed class AbbrevCarry
    {
        public BoundOperand? Subject;
        public string? Op;
        public void Reset() { Subject = null; Op = null; }
    }

    public BoundCondition BindCondition(IParseTree node) => BindCondition(node, new AbbrevCarry());

    private BoundCondition BindCondition(IParseTree node, AbbrevCarry carry) => node switch
    {
        Core.ConditionContext c => BindCondition(c.GetChild(0), carry),
        Core.LogicalOrExpressionContext orExpr => BindFlatSequence(orExpr, "||", carry),
        Core.LogicalXorExpressionContext xorExpr => BindXorSequence(xorExpr, carry),
        Core.LogicalAndExpressionContext andExpr => BindFlatSequence(andExpr, "&&", carry),
        Core.AbbreviatedAndChainContext chain => BindFlatSequence(chain, "&&", carry),
        Core.UnaryLogicalExpressionContext u => u.NOT() is not null
            ? new BoundNot(BindCondition(u.primaryCondition(), carry)) : BindCondition(u.primaryCondition(), carry),
        Core.AbbreviatedRelationContext ar => BindAbbreviatedRelation(ar, carry),
        Core.PrimaryConditionContext p => BindPrimary(p, carry),
        _ => new BoundConditionError("unsupported condition form"),
    };

    /// <summary>Bind a left-to-right logical sequence (an OR / XOR / AND chain, or an abbreviated-AND chain), threading
    /// the abbreviation <paramref name="carry"/> through every operand in SOURCE ORDER so a later abbreviated relation
    /// sees the subject / operator an earlier one established. A lone operand returns its own condition (no wrapper).
    /// A user-function reference in a NON-FIRST operand of an AND/OR chain is CONDITIONALLY evaluated
    /// (§8.8.4.13 r1 — evaluation stops when the hierarchical level's truth value is determined; r2 — functions
    /// are evaluated "if and when the conditions containing them are evaluated"), so that operand's activations
    /// attach to the operand itself (<c>UdfAttachPerEvaluation</c>) and run only when C#'s matching
    /// short-circuit reaches it. The FIRST operand always evaluates — its activations stay statement-hoisted
    /// (exact) unless an ENCLOSING repeated window drains them. XOR is exempt: both operands are always
    /// required (§8.8.4.9), so a hoist is exact for every XOR operand.</summary>
    private BoundCondition BindFlatSequence(IParseTree ctx, string op, AbbrevCarry carry)
    {
        var parts = new List<BoundCondition>();
        for (int i = 0; i < ctx.ChildCount; i++)
        {
            var ch = ctx.GetChild(i);
            if (ch is ITerminalNode) continue;   // the AND / OR / XOR / EXCLUSIVE-OR connective tokens
            int udfMark = host.Udf.PendingCount;
            parts.Add(BindCondition(ch, carry));
            if (parts.Count > 1 && op != "^")
                parts[^1] = host.Udf.UdfAttachPerEvaluation(parts[^1], udfMark);
        }
        return parts.Count == 1 ? parts[0] : new BoundLogical(op, parts);
    }

    /// <summary>The logical XOR / EXCLUSIVE-OR operator (ISO §8.8.4.9) is a COBOL-2023 introduction. It parses at all
    /// editions (superset — the <c>{is2023()}?</c> predicate is gone); the introduction gate fires HERE, only when the
    /// operator is genuinely present (<c>ChildCount &gt; 1</c> ⇒ an <c>XOR</c>/<c>EXCLUSIVE_OR</c> terminal was matched
    /// between two operands), so a bare below-2023 <c>logicalAndExpression</c> is untouched. Residue migration #1
    /// (DESIGN-version-conformance-pipeline.md) — the reverse-signature arm is deleted.</summary>
    private BoundCondition BindXorSequence(Core.LogicalXorExpressionContext xorExpr, AbbrevCarry carry)
        // The XOR-operator introduction gate (LogicalXorOperator2023) fires on RECOGNITION in the
        // VersionConformancePass parse-arm (VisitLogicalXorExpression, ChildCount>1); Step 14h.4a.
        => BindFlatSequence(xorExpr, "^", carry);

    private BoundCondition BindPrimary(Core.PrimaryConditionContext p, AbbrevCarry carry)
    {
        // COBOL-2002 boolean forms (the boolExprAhead()-gated primaryCondition alt) — a boolean relation
        // (§8.8.4.2.2) or a simple boolean condition (§8.8.4.3).
        if (p.booleanExpression() is { Length: > 0 } be) return BindPrimaryBoolean(be, p.comparisonOperator(), carry);
        if (p.comparisonExpression() is { } cmp) return BindComparison(cmp, carry);
        if (p.condition() is { } inner)
        {
            // A parenthesized condition is a complete simple condition: a FRESH abbreviation scope inside, and the
            // insertion terminates for the enclosing sequence (ISO §8.8.4.12.4 GR1).
            var bound = BindCondition(inner, new AbbrevCarry());
            carry.Reset();
            return bound;
        }
        return new BoundConditionError("boolean-literal condition");
    }

    /// <summary>An abbreviated relation with the subject omitted (<c>comparisonOperator comparisonOperand</c>): the
    /// carried subject is inserted and the newly-stated operator becomes the carried operator (ISO §8.8.4.12.4 GR1).</summary>
    private BoundCondition BindAbbreviatedRelation(Core.AbbreviatedRelationContext ar, AbbrevCarry carry)
    {
        if (carry.Subject is not { } subject)
            return new BoundConditionError("abbreviated relation with no preceding relation subject");
        string op = MapOperator(ar.comparisonOperator().GetText());
        carry.Op = op;
        return CheckedRelational(subject, op, ComparisonOperand(ar.comparisonOperand()));
    }

    /// <summary>The §8.8.4.4.3 class-condition operand rules for the boolean category (the one class the
    /// data increment newly introduces): SR8 — NUMERIC requires an operand whose usage is display or national
    /// or whose category is numeric, so a USAGE BIT boolean is rejected (a DISPLAY-form boolean is admitted);
    /// SR4 — ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER / class-name (<paramref name="kind"/> 'A'/'U'/
    /// 'L'/'C') shall not be specified for a boolean operand at all. Both → COBOLNET0844.</summary>
    public void CheckClassConditionOperand(BoundOperand op, char kind)
    {
        // §8.8.4.4.3 SR1 (data-model D17): a strongly-typed group item may not appear in a class condition — it has
        // its own unique class and category (the type-name), not one of the general classes a class condition tests.
        if (op is BoundFieldOperand fg && StrongTypeModel.IsStrongGroup(fg.Place.Item))
        {
            ctx.Edition.Error(DiagnosticCatalog.StrongClassCondition, "a strongly-typed group item may not appear in a class condition — "
                + "it has its own unique class and category (ISO §8.8.4.4.3 SR1)");
            return;
        }
        PicInfo? pic = op switch
        {
            BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic,
            BoundFieldOperand f => f.Place.Item.Pic,
            _ => null,
        };
        if (pic is not { Category: PicCategory.Boolean }) return;
        if (kind is 'N' && pic.Usage is Usage.Bit)
            ctx.Edition.Error("COBOLNET0844", "the NUMERIC class condition requires an operand whose usage is "
                + "display or national or whose category is numeric — a USAGE BIT boolean item is none of these "
                + "(ISO §8.8.4.4.3 SR8)");
        else if (kind is 'A' or 'U' or 'L' or 'C')
            ctx.Edition.Error("COBOLNET0844", "ALPHABETIC / ALPHABETIC-LOWER / ALPHABETIC-UPPER / a class-name "
                + "shall not be specified for a boolean operand (ISO §8.8.4.4.3 SR4)");
    }

    private BoundCondition BindComparison(Core.ComparisonExpressionContext cmp, AbbrevCarry carry)
    {
        var operands = cmp.comparisonOperand();
        bool not = cmp.NOT() is not null;

        if (cmp.className() is { } cls)
        {
            carry.Reset();   // a class condition is a complete simple condition — terminates the abbreviation
            char? kind = cls.NUMERIC() is not null ? 'N'
                : cls.ALPHABETIC() is not null ? 'A'
                : cls.ALPHABETIC_UPPER() is not null ? 'U'
                : cls.ALPHABETIC_LOWER() is not null ? 'L'
                : null;
            if (kind is { } k && operands.Length >= 1)
            {
                var opnd = ComparisonOperand(operands[0]);
                CheckClassConditionOperand(opnd, k);
                return new BoundClassCondition(opnd, k, not);
            }
            // A SPECIAL-NAMES user-defined class (§12.3.7): membership over the expanded character set.
            if (cls.cobolWord() is { } ucls && operands.Length >= 1
                && ctx.Data.UserClasses.TryGetValue(ucls.GetText(), out string? members))
            {
                var opnd = ComparisonOperand(operands[0]);
                CheckClassConditionOperand(opnd, 'C');   // SR4 also forbids a class-name for a boolean operand
                return new BoundUserClassCondition(opnd, members, not);
            }
            return new BoundConditionError($"class condition '{cls.GetText()}'");
        }

        if (cmp.POSITIVE() is not null || cmp.NEGATIVE() is not null || cmp.ZERO() is not null)
        {
            carry.Reset();
            char kind = cmp.POSITIVE() is not null ? 'P' : cmp.NEGATIVE() is not null ? 'N' : 'Z';
            return new BoundSignCondition(host.Expr.BindOperandExpr(operands[0]), kind, not);
        }


        if (cmp.comparisonOperator() is { } opCtx && operands.Length >= 2)
        {
            // A fully-stated relation establishes the subject + operator for any following abbreviated relation in the
            // sequence (ISO §8.8.4.12.4 GR1 — "the last preceding stated subject … and the last stated operator").
            BoundOperand subject = ComparisonOperand(operands[0]);
            string op = MapOperator(opCtx.GetText());
            carry.Subject = subject;
            carry.Op = op;
            BoundOperand right = ComparisonOperand(operands[1]);
            // Object relations (ISO §8.8.4.2.1 Format 3 :9591 — D-U8): class-object operands admit ONLY
            // [NOT] EQUAL, and SR5 (:9614) requires BOTH operands of class object (figurative NULL rides —
            // it is a class-object sender). Reference IDENTITY (§8.8.4.2.15 :9769) renders in the
            // ConditionRenderer's object branch. Typed-vs-typed of UNRELATED classes is LEGAL (identity is
            // simply false); ordering operators and object-vs-non-object mixes are COBOLNET0868.
            static bool IsObjOperand(BoundOperand o) =>
                o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.ObjectReference;
            if (IsObjOperand(subject) || IsObjOperand(right))
            {
                if (op is not ("==" or "!="))
                    ctx.Edition.Error("COBOLNET0868",
                        "an object-reference relation admits only [NOT] EQUAL / '=' / '<>' "
                        + "(ISO §8.8.4.2.1 Format 3 — ordering is undefined for references)");
                else if (!(IsObjOperand(subject) || subject is BoundFigurative { Kind: 'N' })
                         || !(IsObjOperand(right) || right is BoundFigurative { Kind: 'N' }))
                    ctx.Edition.Error("COBOLNET0868",
                        "both operands of an object-reference relation shall be of class object — an "
                        + "object reference or the NULL figurative (ISO §8.8.4.2.1 SR5)");
            }
            // Data-pointer relations (ISO §8.8.4.1.3 / §8.8.4.2 — pointers admit ONLY [NOT] EQUAL, against
            // another pointer or the NULL figurative; the renderer's pointer branch does SameTarget identity).
            static bool IsPtrOperand(BoundOperand o) =>
                o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.Pointer;
            if (IsPtrOperand(subject) || IsPtrOperand(right))
            {
                if (op is not ("==" or "!="))
                    ctx.Edition.Error("COBOLNET0869",
                        "a data-pointer relation admits only [NOT] EQUAL (ISO §8.8.4.1.3 — pointers are "
                        + "not ordered)");
                else if (!(IsPtrOperand(subject) || subject is BoundFigurative { Kind: 'N' })
                         || !(IsPtrOperand(right) || right is BoundFigurative { Kind: 'N' }))
                    ctx.Edition.Error("COBOLNET0869",
                        "both operands of a data-pointer relation shall be a data pointer or NULL "
                        + "(ISO §8.8.4.1.3)");
            }
            // (A boolean-EXPRESSION relation — `IF (a B-AND b) = c` — is staged residue this increment; the
            // item↔item boolean compares of the data increment ride CheckedRelational's 0844 guard below.)
            return CheckedRelational(subject, op, right);
        }

        // A bare single operand — resolve as a sole-operand condition (88 / switch / simple-boolean / abbreviated).
        if (operands.Length == 1)
            return BindSoleOperandCondition(operands[0].valueOperand(), () => ComparisonOperand(operands[0]), carry);

        return new BoundConditionError($"condition '{cmp.GetText()}'");
    }

    /// <summary>A bare single operand is either a level-88 condition-name (a complete simple condition —
    /// terminates the abbreviation), a switch-status condition-name (§8.8.4.6), a SIMPLE BOOLEAN CONDITION over
    /// a length-1 boolean item/literal (§8.8.4.3), or — within an abbreviated sequence — a relation with BOTH
    /// subject and operator omitted (§8.8.4.12; the trailing C in `A = B AND C` ≡ `A = C`). A condition-name
    /// takes precedence. Shared by the generic path and the boolean-alt unwrap (a B-op-free bare operand).</summary>
    private BoundCondition BindSoleOperandCondition(Core.ValueOperandContext? vo, System.Func<BoundOperand> bindOperand, AbbrevCarry carry)
    {
        if (vo?.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref && ConditionOf(dref) is { } cond)
        {
            carry.Reset();
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            return ctx.Refs.ResolveForItem(dref, cond.Parent) is { } parent
                ? new BoundCondition88(parent, cond)
                : new BoundConditionError($"condition-name '{cond.Name}' (unresolvable conditional variable)");
        }
        // A switch-status condition-name — resolved AFTER level-88 (NC211A: a name defined as both → the 88
        // wins), BEFORE the abbreviated-carry fallback.
        if (vo?.arithmeticExpression() is { } swx && SoleDataRef(swx) is { } swr && host.Alter.SwitchCondOf(swr) is { } swCond)
        {
            carry.Reset();
            return swCond;
        }
        // A SIMPLE BOOLEAN CONDITION over a bare length-1 boolean item/literal (§8.8.4.3).
        if (vo is not null && IsBooleanValueOperand(vo))
        {
            carry.Reset();
            return BindSimpleBooleanCondition(BindBoolOperandValue(vo));
        }
        if (carry is { Subject: { } subject, Op: { } op })
            return CheckedRelational(subject, op, bindOperand());
        return new BoundConditionError($"condition '{vo?.GetText() ?? "operand"}'");
    }

    /// <summary>The ONE <see cref="BoundRelational"/> construction checkpoint — the §8.8.4.2.2 boolean
    /// relation rules ride every site (IF / EVALUATE pairing + ranges / PERFORM UNTIL / SEARCH): a boolean
    /// operand compares only with another boolean operand (§8.8.4.2.1 F1 SR2/SR3 exclude class boolean from
    /// the general relation — a class mix is 0844) and only for [in]equality (Format 2 — an ordering operator
    /// on boolean operands is 0844; an EVALUATE THRU range over a boolean subject trips the same check,
    /// §14.9.13.3 SR4). Figurative ZERO is boolean zeros by context (§8.3.3.6.4 GR4); every other figurative
    /// against a boolean operand is non-boolean.</summary>
    public BoundRelational CheckedRelational(BoundOperand left, string op, BoundOperand right)
    {
        // The edition-invariant §8.8.4.2.2 / §8.8.4.2.3 SR band (class-boolean comparability + the
        // strongly-typed-group rules) is the ONE StatementValidation home (10t/3 — the 10o deviation-(b)
        // pure-lift discharged); the node is always built (a PURE emission check, no verdict).
        ctx.Validation.CheckRelationalOperands(left, op, right);
        return new BoundRelational(left, op, right);
    }

    /// <summary>Bind a comparison operand: a non-numeric literal, a sole data reference, or a numeric expression.</summary>
    private BoundOperand ComparisonOperand(Core.ComparisonOperandContext operand) =>
        ComparisonOperandOf(operand.valueOperand());

    /// <summary>Bind a <c>valueOperand</c> as a comparison operand (the shared body of <see cref="ComparisonOperand"/>
    /// and the boolean-alt unwrap path — feedback_singular_pattern).</summary>
    private BoundOperand ComparisonOperandOf(Core.ValueOperandContext? vo)
    {
        // §8.8.3.3 GR3: a concatenation expression folds to (and compares as) the equivalent single literal.
        if (vo?.nonNumericLiteral()?.concatenationExpression() is { } ce) return host.Expr.ConcatOperand(ce);
        if (vo?.nonNumericLiteral()?.figurativeConstant() is { } fig) return ExpressionBinder.FigurativeOperand(fig);
        if (vo?.nonNumericLiteral()?.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        if (vo?.nonNumericLiteral()?.NATLIT() is { } nat) return host.Expr.NationalLiteralOperand(nat.GetText());
        if (vo?.nonNumericLiteral()?.BOOLLIT() is { } bl) return host.Expr.BooleanLiteralOperand(bl.GetText());
        if (vo?.arithmeticExpression() is { } expr)
            return SoleDataRef(expr) is { } dref ? host.Expr.FieldOperand(dref)
                // A sole numeric LITERAL stays a literal operand — against an alphanumeric/group operand it
                // participates as its WRITTEN character form, leading zeros intact (ISO §8.8.4.2.1), which a
                // computed wrapper would lose.
                : SoleNumLiteral(expr) is { } lit ? new BoundNumericLiteral(host.Expr.CheckLiteral(lit))
                // The ONE expression→operand mapping (IntrinsicBinder.OperandOf): a user-function reference
                // binds to a BoundNumRef over its result temp, which MUST surface as a FIELD operand here so
                // the temp's cloned category (§8.4.3.2.4 GR1) drives the relation's class dispatch — a raw
                // computed wrapper would compare an alphanumeric/national result NUMERICALLY. Numeric
                // renderings are identical either way (AsNum unwraps both to the same FieldNum read).
                : IntrinsicBinder.OperandOf(host.Expr.BindExpr(expr));
        return new BoundOperandError("comparison operand");
    }

    /// <summary>Resolve a condition-name reference, honoring OF/IN qualifiers (ISO §8.4.2.2 Format 2: a
    /// condition-name qualifies by its conditional variable and/or the variable's containing groups, innermost
    /// first) — duplicate 88 names across tables select by the qualifier chain.</summary>
    public Condition88? ConditionOf(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        // §11.7 GR5 — a method-local 88 shadows object data; the overlay-first precedence lives in the ONE
        // scope-aware SymbolTable (P7 Step 10a, the DEVLOG-773 pickup), no longer duplicated inline here.
        if (!ctx.Symbols.TryResolveCondition(name, ctx.ActiveScope, out var list)) return null;
        var qualifiers = dref.dataReferenceSuffix()
            .Select(sfx => sfx.qualification()?.cobolWord().GetText())
            .OfType<string>().ToList();
        if (qualifiers.Count == 0) return list[0];
        return list.FirstOrDefault(c => MatchesQualifiers(c.Parent, qualifiers));
    }

    /// <summary>True when each qualifier (innermost→outermost) names the conditional variable itself or one of
    /// its containing groups, in nesting order.</summary>
    private static bool MatchesQualifiers(DataItem parent, List<string> qualifiers)
    {
        DataItem? n = parent;
        foreach (string q in qualifiers)
        {
            while (n is not null && !string.Equals(n.CobolName, q, StringComparison.OrdinalIgnoreCase)) n = n.Parent;
            if (n is null) return false;
            n = n.Parent;
        }
        return true;
    }

    // ── Operator mapping + helpers (ported from the former emitter) ──────────────────────────────────────────

    public static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        if (!not) return baseOp;
        return baseOp switch { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
    }

    public static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.PrimaryExpressionContext)n).dataReference();
    }

    /// <summary>The raw text of an arithmetic expression that is a SOLE numeric literal, else null.</summary>
    public static string? SoleNumLiteral(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        var pe = (Core.PrimaryExpressionContext)n;
        return pe.numericLiteral()?.GetText() ?? (pe.ZERO_ARITH() is not null ? "0" : null);
    }
}
