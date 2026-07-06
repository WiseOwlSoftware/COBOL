// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Validation;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// The boolean-expression / boolean-operator binder (Phase-4 track (a) increment 2; ISO §8.8.2 the operators
/// B-AND/B-OR/B-XOR/B-NOT, §14.9.8 Format-2 boolean COMPUTE, §8.8.4.2.2 the boolean relation, §8.8.4.3 the
/// simple boolean condition). A boolean expression binds into the <see cref="BoundBoolExpr"/> value channel
/// (a '0'/'1' string world, D-B1) — never the numeric or DISPLAY operand channels. Operand SHAPES are validated
/// here (the COBOLNET1511 constraint band); the {is2002()}?-gated grammar tiers enforce the §8.8.2 formation
/// rules structurally, and the operators are unreachable below 2002 (so no binder-side introduction gate — the
/// XOR precedent: the grammar predicate + the parse-layer hint ARE the gate).
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>Bind a <c>booleanExpression</c> (ISO §8.8.2, precedence B-NOT &gt; B-AND &gt; B-XOR &gt; B-OR):
    /// left-to-right within each tier (rule 7c). Rule 4 — both operands of a binary op shall not both be
    /// <c>ALL "literal"</c> — is checked at each combination.</summary>
    private BoundBoolExpr BindBoolExpr(Core.BooleanExpressionContext ctx)
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
        var factors = ctx.booleanFactor();
        BoundBoolExpr acc = BindBoolFactor(factors[0]);
        for (int i = 1; i < factors.Length; i++)
            acc = MakeBoolBinary(acc, '&', BindBoolFactor(factors[i]));
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

    /// <summary>Rule 4 (§8.8.2 :9364): both operands of a binary boolean op shall not both be ALL "literal".</summary>
    private BoundBoolExpr MakeBoolBinary(BoundBoolExpr left, char op, BoundBoolExpr right)
    {
        if (left is BoundBoolAll && right is BoundBoolAll)
            data.Edition.Error("COBOLNET1511", "both operands of a boolean operator shall not be ALL literals "
                + "(ISO §8.8.2 rule 4)");
        return new BoundBoolBinary(left, op, right);
    }

    /// <summary>Resolve a boolean-expression leaf operand (ISO §8.8.2 operand list): a boolean literal, the
    /// figurative ZERO / <c>ALL B"…"</c>, or a category-boolean data item. Anything else — a non-boolean item,
    /// a plain string, an arithmetic expression, another figurative — is COBOLNET1511.</summary>
    private BoundBoolExpr BindBoolOperandValue(Core.ValueOperandContext vo)
    {
        var nn = vo.nonNumericLiteral();
        if (nn?.BOOLLIT() is { } bl)
        {
            ConstructRegistry.Check(data.Edition, "boolean-data-2002", "boolean literal B\"…\"");
            return new BoundBoolLiteral(DecodeCobolString(bl.GetText()));
        }
        if (nn?.figurativeConstant() is { } fig)
        {
            if (fig.ZERO() is not null) return new BoundBoolAll("0");   // figurative ZERO — boolean zeros by context (§8.3.3.6.4 GR4)
            if (fig.BOOLLIT() is { } allBl) return new BoundBoolAll(DecodeCobolString(allBl.GetText()));   // ALL B"…"
            return new BoundBoolError($"figurative constant '{fig.GetText()}' in a boolean expression "
                + "(ISO §8.8.2 — only ZERO and ALL B\"…\" are boolean figuratives)");
        }
        // A sole data reference to a category-boolean item.
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref && refs.Resolve(dref) is { } p)
        {
            var cat = p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category;
            if (cat is PicCategory.Boolean) return new BoundBoolRef(p);
            data.Edition.Error("COBOLNET1511", $"operand '{dref.GetText()}' in a boolean expression is not a "
                + "boolean data item (ISO §8.8.2 — boolean operands only)");
            return new BoundBoolError($"non-boolean operand '{dref.GetText()}'");
        }
        data.Edition.Error("COBOLNET1511", $"'{vo.GetText()}' is not a valid boolean operand — a boolean "
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
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref
            && refs.Resolve(dref) is { } p)
            return (p is RefModPlace rm ? rm.Inner.Item.Pic?.Category : p.Item.Pic?.Category) is PicCategory.Boolean;
        return false;
    }

    /// <summary>The set of category-boolean ITEMS referenced in a bound boolean expression, for the §14.9.8 GR3
    /// COMPUTE-store width (the max static boolean positions; literal/ALL operands do not count — GR3).</summary>
    private static int Gr3Width(BoundBoolExpr e) => e switch
    {
        BoundBoolRef r => r.Place is RefModPlace ? RefModLen(r.Place) : r.Place.Item.Pic?.Length ?? 0,
        BoundBoolBinary b => System.Math.Max(Gr3Width(b.Left), Gr3Width(b.Right)),
        BoundBoolNot n => Gr3Width(n.Operand),
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
            data.Edition.Error("COBOLNET1511", "a simple boolean condition shall reference only boolean items "
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
        BoundBoolAll => true,   // positionless — materializes to the sibling's length
        _ => true,              // error nodes already reported
    };
}
