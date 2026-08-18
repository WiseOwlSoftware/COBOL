// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound boolean expression (<see cref="BoundBoolExpr"/>, ISO §8.8.2) to a side-effect-free C#
/// <see cref="string"/> expression over the runtime <c>CobolBool</c> — a '0'/'1' bit-string value (D-B1).
/// Rule 4 (§8.8.2 :9364) guarantees at most one <see cref="BoundBoolAll"/> operand of a binary op, so the
/// <c>…All</c> runtime forms take the concrete side ONCE (never double-rendered) and the positionless pattern
/// as a literal. This is the boolean-operator half of Phase-4 track (a) increment 2. A boolean SHIFT (§8.8.2
/// rule 8, 2023) carries an integer count, so the render is parameterized by a <see cref="NumericRenderer"/>
/// (the count is a numeric operand, not a boolean one).
/// </summary>
internal static class BooleanRenderer
{
    public static string Render(BoundBoolExpr e, NumericRenderer num) => e.Accept(new RenderVisitor(num));

    // Dispatch through the generated exhaustive IBoundBoolExprVisitor (PHASE-07 Step 6): a new BoundBoolExpr leaf
    // is a COMPILE error here (the loud `_ =>` is gone). The visitor carries the NumericRenderer for shift counts.
    private sealed class RenderVisitor(NumericRenderer num) : IBoundBoolExprVisitor<string>
    {
        public string Visit(BoundBoolLiteral n) => EmitText.CsLiteral(n.Bits);
        public string Visit(BoundBoolRef n) => PlaceRenderer.Read(n.Place);     // a category-boolean item IS a '0'/'1' string
        public string Visit(BoundBoolCall n) => OperandText.AsString(new BoundComputedOperand(n.Call), num);   // the boolean function's '0'/'1' image (kb/Work PB68)
        public string Visit(BoundBoolAll n) => EmitText.CsLiteral(n.Bits);     // materialized at the combine site (…All forms)
        public string Visit(BoundBoolNot n) => RenderNot(n.Operand);
        public string Visit(BoundBoolBinary n) => RenderBinary(n);
        public string Visit(BoundBoolShift n) => RenderShift(n);
        public string Visit(BoundBoolError n) => EmitText.LoudValue("string", n.Feature);

        private string RenderNot(BoundBoolExpr op) =>
            // A B-NOT ALL … already constant-folded at bind (BoundBoolAll); any other operand flips at runtime.
            op is BoundBoolAll a ? EmitText.CsLiteral(a.Bits) : RuntimeApi.BoolNot(op.Accept(this));

        private string RenderBinary(BoundBoolBinary b)
        {
            string method = RuntimeApi.BoolOpName(b.Op);   // nameof-anchored (P7 Step 4b)
            // Rule 4: at most one side is ALL. When one side is the positionless pattern, use the …All form so the
            // concrete side evaluates exactly once (an intrinsic/UDF operand must not double-render — future-proof).
            if (b.Left is BoundBoolAll la)
                return RuntimeApi.BoolOpAll(method, b.Right.Accept(this), EmitText.CsLiteral(la.Bits));
            if (b.Right is BoundBoolAll ra)
                return RuntimeApi.BoolOpAll(method, b.Left.Accept(this), EmitText.CsLiteral(ra.Bits));
            return RuntimeApi.BoolOp(method, b.Left.Accept(this), b.Right.Accept(this));
        }

        private string RenderShift(BoundBoolShift s) =>
            // The count is a numeric operand rendered as an integer (m=0 implementor choice — a fractional count
            // truncates, ISO §8.8.2 rule 8 "repeat until iterations == K"); the runtime kernel guards k ≤ 0 / k ≥ N.
            RuntimeApi.BoolShift(s.Kind, s.Operand.Accept(this),
                $"(long)({NumericRenderer.Align(num.Render(s.Count, ReceiverContext.None), 0)})");
    }

    /// <summary>A boolean value expression as read for a relation operand — the same '0'/'1' string form.</summary>
    public static string BoolRead(BoundBoolExpr e, NumericRenderer num) => Render(e, num);
}
