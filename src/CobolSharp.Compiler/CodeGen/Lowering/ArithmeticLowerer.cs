// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers COBOL arithmetic statements to IR: ADD, SUBTRACT, MULTIPLY,
/// DIVIDE (with REMAINDER), COMPUTE, and ON SIZE ERROR handling.
/// </summary>
internal sealed class ArithmeticLowerer
{
    private readonly LoweringContext _ctx;

    public ArithmeticLowerer(LoweringContext ctx) => _ctx = ctx;

    public IrBasicBlock LowerArithmetic(BoundArithmeticStatement arith, IrMethod method, IrBasicBlock block)
        => arith.ArithmeticKind switch
        {
            ArithmeticKind.Add => LowerAdd(arith, method, block),
            ArithmeticKind.Subtract => LowerSubtract(arith, method, block),
            ArithmeticKind.Multiply => LowerMultiply(arith, method, block),
            ArithmeticKind.Divide => LowerDivide(arith, method, block),
            ArithmeticKind.Compute => LowerCompute(arith, method, block),
            _ => throw new InvalidOperationException($"Unknown arithmetic kind: {arith.ArithmeticKind}")
        };

    private IrBasicBlock LowerMultiply(BoundArithmeticStatement mult, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        foreach (var target in mult.Targets)
        {
            var destLoc = _ctx.Location.ResolveExpressionLocation(target.Target);
            if (destLoc == null) continue;
            int roundingMode = target.IsRounded ? 1 : 0;

            if (mult.IsGiving)
            {
                var irLeft = _ctx.Expression.LowerExpression(mult.Operands[0]);
                var irRight = _ctx.Expression.LowerExpression(mult.Receiver!);
                if (irLeft != null && irRight != null)
                {
                    var irMul = new IrBinaryExpr(IrArithmeticOp.Multiply, irLeft, irRight);
                    block.Instructions.Add(new IrComputeStore(irMul, destLoc, roundingMode));
                }
            }
            else
            {
                if (mult.Operands[0] is BoundLiteralExpression lit && lit.Value is decimal d)
                {
                    block.Instructions.Add(new IrPicMultiplyLiteral(
                        d, destLoc, destLoc, roundingMode));
                }
                else
                {
                    var opLoc = _ctx.Location.ResolveExpressionLocation(mult.Operands[0]);
                    if (opLoc != null)
                    {
                        block.Instructions.Add(new IrPicMultiply(
                            opLoc, destLoc, destLoc, roundingMode));
                    }
                }
            }
        }

        return LowerSizeError(mult.SizeError, method, block);
    }

    private IrBasicBlock LowerSubtract(BoundArithmeticStatement sub, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        var accum = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in sub.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression opId)
            {
                var opLoc = _ctx.Location.ResolveLocation(opId);
                if (opLoc != null)
                    block.Instructions.Add(new IrAccumulateField(accum, opLoc));
            }
        }

        foreach (var target in sub.Targets)
        {
            var destLoc = _ctx.Location.ResolveLocation(target.Target);
            if (destLoc == null) continue;
            int roundingMode = target.IsRounded ? 1 : 0;
            if (sub.IsGiving && sub.Receiver != null)
            {
                IrExpression irSum = _ctx.Expression.LowerExpression(sub.Operands[0]) ?? new IrLiteral(0m);
                for (int i = 1; i < sub.Operands.Count; i++)
                {
                    var irNext = _ctx.Expression.LowerExpression(sub.Operands[i]) ?? new IrLiteral(0m);
                    irSum = new IrBinaryExpr(IrArithmeticOp.Add, irSum, irNext);
                }
                var irMinuend = _ctx.Expression.LowerExpression(sub.Receiver) ?? new IrLiteral(0m);
                var irSub = new IrBinaryExpr(IrArithmeticOp.Subtract, irMinuend, irSum);
                block.Instructions.Add(new IrComputeStore(irSub, destLoc, roundingMode));
            }
            else
            {
                block.Instructions.Add(new IrSubtractAccumulatedFromTarget(accum, destLoc, roundingMode));
            }
        }

        return LowerSizeError(sub.SizeError, method, block);
    }

    private IrBasicBlock LowerDivide(BoundArithmeticStatement div, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());
        IrValue? accum = null;

        if (div.Receiver != null)
        {
            var irDividend = _ctx.Expression.LowerExpression(div.Receiver) ?? new IrLiteral(0m);
            var irDivisor0 = _ctx.Expression.LowerExpression(div.Operands[0]) ?? new IrLiteral(1m);
            var irDiv = new IrBinaryExpr(IrArithmeticOp.Divide, irDividend, irDivisor0);
            accum = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
            block.Instructions.Add(new IrComputeIntoAccumulator(accum.Value, irDiv));

            foreach (var target in div.Targets)
            {
                var destLoc = _ctx.Location.ResolveLocation(target.Target);
                if (destLoc == null) continue;
                int roundingMode = target.IsRounded ? 1 : 0;
                block.Instructions.Add(new IrMoveAccumulatedToTarget(accum.Value, destLoc, roundingMode));
            }
        }
        else
        {
            bool needRemainder = div.RemainderTarget != null && div.Targets.Count > 0;

            if (needRemainder)
            {
                var target0 = div.Targets[0];
                var irTarget0 = _ctx.Expression.LowerExpression(target0.Target) ?? new IrLiteral(0m);
                var irDivisor1 = _ctx.Expression.LowerExpression(div.Operands[0]) ?? new IrLiteral(1m);
                var irDiv2 = new IrBinaryExpr(IrArithmeticOp.Divide, irTarget0, irDivisor1);
                accum = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
                block.Instructions.Add(new IrComputeIntoAccumulator(accum.Value, irDiv2));

                var destLoc = _ctx.Location.ResolveLocation(target0.Target);
                if (destLoc != null)
                {
                    int roundingMode = target0.IsRounded ? 1 : 0;
                    block.Instructions.Add(new IrMoveAccumulatedToTarget(accum.Value, destLoc, roundingMode));

                    int givingFracDigits = destLoc.GetPic().FractionDigits;
                    var remLoc = _ctx.Location.ResolveLocation(div.RemainderTarget!);
                    if (remLoc != null)
                    {
                        var irRemDividend = _ctx.Expression.LowerExpression(target0.Target) ?? new IrLiteral(0m);
                        var irRemDivisor = _ctx.Expression.LowerExpression(div.Operands[0]) ?? new IrLiteral(1m);
                        block.Instructions.Add(new IrCobolRemainder(
                            irRemDividend, irRemDivisor, accum.Value, givingFracDigits, remLoc));
                    }
                }
            }
            else
            {
                foreach (var target in div.Targets)
                {
                    var destLoc = _ctx.Location.ResolveLocation(target.Target);
                    if (destLoc == null) continue;
                    int roundingMode = target.IsRounded ? 1 : 0;

                    if (div.Operands[0] is BoundLiteralExpression litDiv && litDiv.Value is decimal d)
                    {
                        block.Instructions.Add(new IrPicDivideLiteral(d, destLoc, destLoc, roundingMode));
                    }
                    else if (div.Operands[0] is BoundIdentifierExpression divisorId)
                    {
                        var divisorLoc = _ctx.Location.ResolveLocation(divisorId);
                        if (divisorLoc != null)
                            block.Instructions.Add(new IrPicDivide(destLoc, divisorLoc, destLoc, roundingMode));
                    }
                }
            }
        }

        if (div.RemainderTarget != null && div.Receiver != null && div.Targets.Count > 0 && accum != null)
        {
            var remLoc = _ctx.Location.ResolveLocation(div.RemainderTarget);
            if (remLoc != null)
            {
                var givingLoc = _ctx.Location.ResolveLocation(div.Targets[0].Target);
                int givingFracDigits = givingLoc != null
                    ? givingLoc.GetPic().FractionDigits : 0;

                var irRemGivDividend = _ctx.Expression.LowerExpression(div.Receiver) ?? new IrLiteral(0m);
                var irRemGivDivisor = _ctx.Expression.LowerExpression(div.Operands[0]) ?? new IrLiteral(1m);
                block.Instructions.Add(new IrCobolRemainder(
                    irRemGivDividend, irRemGivDivisor, accum.Value, givingFracDigits, remLoc));
            }
        }

        return LowerSizeError(div.SizeError, method, block);
    }

    private IrBasicBlock LowerCompute(BoundArithmeticStatement comp, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        foreach (var target in comp.Targets)
        {
            var destLoc = _ctx.Location.ResolveLocation(target.Target);
            if (destLoc == null) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            var irCompExpr = _ctx.Expression.LowerExpression(comp.Operands[0]);
            if (irCompExpr != null)
                block.Instructions.Add(new IrComputeStore(irCompExpr, destLoc, roundingMode));
        }

        return LowerSizeError(comp.SizeError, method, block);
    }

    private IrBasicBlock LowerAdd(BoundArithmeticStatement add, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        var accum = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in add.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression srcId)
            {
                var srcLoc = _ctx.Location.ResolveLocation(srcId);
                if (srcLoc != null)
                    block.Instructions.Add(new IrAccumulateField(accum, srcLoc));
            }
        }

        foreach (var target in add.Targets)
        {
            var destLoc = _ctx.Location.ResolveLocation(target.Target);
            if (destLoc == null) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            if (add.IsGiving)
                block.Instructions.Add(new IrMoveAccumulatedToTarget(accum, destLoc, roundingMode));
            else
                block.Instructions.Add(new IrAddAccumulatedToTarget(accum, destLoc, roundingMode));
        }

        return LowerSizeError(add.SizeError, method, block);
    }

    public IrBasicBlock LowerSizeError(BoundSizeErrorClause? clause, IrMethod method, IrBasicBlock block)
    {
        if (clause == null || !clause.HasClauses)
            return block;

        var sizeErrorBlock = method.CreateBlock("size.error");
        var notSizeErrorBlock = method.CreateBlock("not.size.error");
        var doneBlock = method.CreateBlock("size.done");

        var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrLoadSizeError(condVal));
        block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

        method.Blocks.Add(sizeErrorBlock);
        var seCurrent = sizeErrorBlock;
        foreach (var stmt in clause.OnSizeError)
            seCurrent = _ctx.LowerStatement(stmt, method, seCurrent);
        seCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(notSizeErrorBlock);
        var nseCurrent = notSizeErrorBlock;
        foreach (var stmt in clause.NotOnSizeError)
            nseCurrent = _ctx.LowerStatement(stmt, method, nseCurrent);
        nseCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(doneBlock);
        return doneBlock;
    }
}
