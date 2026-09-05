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
    /// <param name="sending">The §14.6.13.2 exempt context of the reads in this expression
    /// (<see cref="SendingRef"/>). Rule 1's checked boolean read is emitted unless the context is one of that
    /// rule's TWO exemptions — a class condition or VALIDATE.</param>
    public static string Render(BoundBoolExpr e, NumericRenderer num, SendingRef sending = SendingRef.Normal) =>
        e.Accept(new RenderVisitor(num, sending));

    // Dispatch through the generated exhaustive IBoundBoolExprVisitor (PHASE-07 Step 6): a new BoundBoolExpr leaf
    // is a COMPILE error here (the loud `_ =>` is gone). The visitor carries the NumericRenderer for shift counts.
    private sealed class RenderVisitor(NumericRenderer num, SendingRef sending) : IBoundBoolExprVisitor<string>
    {
        public string Visit(BoundBoolLiteral n) => EmitText.CsLiteral(n.Bits);
        // ⛔ THE BOOLEAN SENDING-READ CHOKEPOINT — the third of §14.6.13.2's three, beside the fixed-point one in
        // NumericRenderer.FieldNumCore and the float one beside it (kb/Work PB230). A category-boolean item IS a
        // '0'/'1' string (D-B1 — USAGE BIT takes the same character storage, §13.18.40.4 GR14's representation
        // license), and a REDEFINES window over it can therefore deposit a character that is not a boolean value:
        // `MOVE "1Q01" TO <X(4) window>` then `COMPUTE R = B` propagated the Q straight into the boolean RESULT.
        // Rule 1 makes that EC-DATA-INCOMPATIBLE under checking; with checking off CobolBool's operators keep
        // treating a foreign position as boolean zero, which is what "the result of the reference is undefined"
        // licenses. BOTH arms are wrapped — the bit-GROUP arm derives its string from bits and so can only ever
        // pass the test, but arm-specific cleverness here is exactly the shape that left rule 2 unwired.
        public string Visit(BoundBoolRef n) => sending.FixedPointChecked()
            ? RuntimeApi.BoolSending(n.Place.Item.IsAsIfElementary ? OperandText.FieldImage(n.Place) : PlaceRenderer.Read(n.Place))
            : n.Place.Item.IsAsIfElementary ? OperandText.FieldImage(n.Place) : PlaceRenderer.Read(n.Place);
        // A boolean FUNCTION's returned value lives in a fresh temporary (§15.4), not a stored item a window could
        // have corrupted, so it carries no rule-1 wrap — the same reasoning OperandText applies to a numeric
        // intrinsic's text.
        public string Visit(BoundBoolCall n) => OperandText.AsString(new BoundComputedOperand(n.Call), num, sending: sending);   // the boolean function's '0'/'1' image (kb/Work PB68)
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
                $"(long)({NumericRenderer.Align(num.Render(s.Count, ReceiverContext.None, sending), 0)})");
    }

    /// <summary>A boolean value expression as read for a relation operand — the same '0'/'1' string form.</summary>
    public static string BoolRead(BoundBoolExpr e, NumericRenderer num, SendingRef sending = SendingRef.Normal) =>
        Render(e, num, sending);
}
