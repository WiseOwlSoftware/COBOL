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
    public static string Render(BoundBoolExpr e) => e switch
    {
        BoundBoolLiteral l => EmitText.CsLiteral(l.Bits),
        BoundBoolRef r => r.Place.Read(),                       // a category-boolean item IS a '0'/'1' string
        BoundBoolAll a => EmitText.CsLiteral(a.Bits),           // materialized at the combine site (…All forms)
        BoundBoolNot n => RenderNot(n.Operand),
        BoundBoolBinary b => RenderBinary(b),
        BoundBoolError err => EmitText.LoudValue("string", err.Feature),
        _ => EmitText.LoudValue("string", $"boolean expression '{e.GetType().Name}'"),
    };

    private static string RenderNot(BoundBoolExpr op) =>
        // A B-NOT ALL … already constant-folded at bind (BoundBoolAll); any other operand flips at runtime.
        op is BoundBoolAll a ? EmitText.CsLiteral(a.Bits) : $"CobolBool.Not({Render(op)})";

    private static string RenderBinary(BoundBoolBinary b)
    {
        string method = b.Op switch { '&' => "And", '|' => "Or", '^' => "Xor", _ => "And" };
        // Rule 4: at most one side is ALL. When one side is the positionless pattern, use the …All form so the
        // concrete side evaluates exactly once (an intrinsic/UDF operand must not double-render — future-proof).
        if (b.Left is BoundBoolAll la)
            return $"CobolBool.{method}All({Render(b.Right)}, {EmitText.CsLiteral(la.Bits)})";
        if (b.Right is BoundBoolAll ra)
            return $"CobolBool.{method}All({Render(b.Left)}, {EmitText.CsLiteral(ra.Bits)})";
        return $"CobolBool.{method}({Render(b.Left)}, {Render(b.Right)})";
    }

    /// <summary>A boolean value expression as read for a relation operand — the same '0'/'1' string form.</summary>
    public static string BoolRead(BoundBoolExpr e) => Render(e);
}
