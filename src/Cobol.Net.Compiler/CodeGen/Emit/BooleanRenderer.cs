// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound boolean expression (<see cref="BoundBoolExpr"/>, ISO §8.8.2) to a side-effect-free C#
/// <see cref="string"/> expression over the runtime <c>CobolBool</c> — a '0'/'1' bit-string value (D-B1).
/// Rule 4 (§8.8.2 :9364) guarantees at most one <see cref="BoundBoolAll"/> operand of a binary op, so the
/// <c>…All</c> runtime forms take the concrete side ONCE (never double-rendered) and the positionless pattern
/// as a literal. This is the boolean-operator half of Phase-4 track (a) increment 2.
/// </summary>
internal static class BooleanRenderer
{
    // Dispatch through the generated exhaustive IBoundBoolExprVisitor (PHASE-07 Step 6): a cached instance renders
    // with no per-call allocation, and a new BoundBoolExpr leaf is a COMPILE error here (the loud `_ =>` is gone).
    private static readonly RenderVisitor _visitor = new();

    public static string Render(BoundBoolExpr e) => e.Accept(_visitor);

    private sealed class RenderVisitor : IBoundBoolExprVisitor<string>
    {
        public string Visit(BoundBoolLiteral n) => EmitText.CsLiteral(n.Bits);
        public string Visit(BoundBoolRef n) => n.Place.Read();                 // a category-boolean item IS a '0'/'1' string
        public string Visit(BoundBoolAll n) => EmitText.CsLiteral(n.Bits);     // materialized at the combine site (…All forms)
        public string Visit(BoundBoolNot n) => RenderNot(n.Operand);
        public string Visit(BoundBoolBinary n) => RenderBinary(n);
        public string Visit(BoundBoolError n) => EmitText.LoudValue("string", n.Feature);
    }

    private static string RenderNot(BoundBoolExpr op) =>
        // A B-NOT ALL … already constant-folded at bind (BoundBoolAll); any other operand flips at runtime.
        op is BoundBoolAll a ? EmitText.CsLiteral(a.Bits) : RuntimeApi.BoolNot(Render(op));

    private static string RenderBinary(BoundBoolBinary b)
    {
        string method = RuntimeApi.BoolOpName(b.Op);   // nameof-anchored (P7 Step 4b)
        // Rule 4: at most one side is ALL. When one side is the positionless pattern, use the …All form so the
        // concrete side evaluates exactly once (an intrinsic/UDF operand must not double-render — future-proof).
        if (b.Left is BoundBoolAll la)
            return RuntimeApi.BoolOpAll(method, Render(b.Right), EmitText.CsLiteral(la.Bits));
        if (b.Right is BoundBoolAll ra)
            return RuntimeApi.BoolOpAll(method, Render(b.Left), EmitText.CsLiteral(ra.Bits));
        return RuntimeApi.BoolOp(method, Render(b.Left), Render(b.Right));
    }

    /// <summary>A boolean value expression as read for a relation operand — the same '0'/'1' string form.</summary>
    public static string BoolRead(BoundBoolExpr e) => Render(e);
}
