// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Lowers COBOL control flow to IR: PERFORM (all variants), IF/ELSE,
/// EVALUATE (WHEN), GO TO, ALTER, EXIT PERFORM/PARAGRAPH/SECTION,
/// NEXT SENTENCE, SEARCH, and SEARCH ALL.
/// M002 Stage 4: methods move here from Binder.
/// </summary>
internal sealed class ControlFlowLowerer
{
    private readonly LoweringContext _ctx;

    public ControlFlowLowerer(LoweringContext ctx) => _ctx = ctx;

    public IrBasicBlock LowerPerform(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // PERFORM VARYING: recursive loop structure with optional AFTER nesting
        if (perf.Varying != null)
        {
            // The outermost loop's end block is the EXIT PERFORM target
            var varyEnd = method.CreateBlock("vary.exit");
            _ctx.PerformExitStack.Push(varyEnd);
            var result = LowerPerformVarying(perf.Varying, perf, method, block);
            // The recursive lowering returns the outermost loopEnd block,
            // but EXIT PERFORM jumps to varyEnd. Wire them together.
            result.Instructions.Add(new IrJump(varyEnd));
            method.Blocks.Add(varyEnd);
            _ctx.PerformExitStack.Pop();
            return varyEnd;
        }

        // PERFORM para N TIMES: expression-based loop count
        if (perf.TimesExpression != null)
        {
            return LowerPerformTimes(perf, method, block);
        }

        // PERFORM UNTIL (no VARYING): simple condition loop
        if (perf.UntilCondition != null)
        {
            var loopStart = method.CreateBlock("perf.until.start");
            var loopBody = method.CreateBlock("perf.until.body");
            var loopEnd = method.CreateBlock("perf.until.end");

            _ctx.PerformExitStack.Push(loopEnd);
            // EXIT PERFORM CYCLE jumps to loopStart (re-tests condition)
            _ctx.PerformContinueStack.Push(loopStart);

            if (perf.IsTestAfter)
            {
                // TEST AFTER: do-while — execute body first, then test condition
                block.Instructions.Add(new IrJump(loopBody));

                method.Blocks.Add(loopBody);
                var bodyCurrent = LowerPerformBody(perf, method, loopBody);
                bodyCurrent.Instructions.Add(new IrJump(loopStart));

                method.Blocks.Add(loopStart);
                var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                _ctx.Condition.LowerCondition(perf.UntilCondition, condVal, loopStart);
                loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
                loopStart.Instructions.Add(new IrJump(loopEnd));
            }
            else
            {
                // TEST BEFORE (default): while — test condition first
                block.Instructions.Add(new IrJump(loopStart));

                method.Blocks.Add(loopStart);
                var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                _ctx.Condition.LowerCondition(perf.UntilCondition, condVal, loopStart);
                loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
                loopStart.Instructions.Add(new IrJump(loopEnd));

                method.Blocks.Add(loopBody);
                var bodyCurrent = LowerPerformBody(perf, method, loopBody);
                bodyCurrent.Instructions.Add(new IrJump(loopStart));
            }

            method.Blocks.Add(loopEnd);
            _ctx.PerformContinueStack.Pop();
            _ctx.PerformExitStack.Pop();
            return loopEnd;
        }

        // Inline PERFORM (no options, no target): execute block once
        if (perf.InlineStatements is { Count: > 0 })
        {
            var current = block;
            foreach (var stmt in perf.InlineStatements)
                current = _ctx.LowerStatement(stmt, method, current);
            return current;
        }

        // Simple PERFORM (no UNTIL/VARYING): just call paragraphs
        LowerPerformSimple(perf, block);
        return block;
    }

    /// <summary>
    /// Recursive VARYING lowering. For each level:
    ///   1. Initialize index from FROM value
    ///   2. Loop: test UNTIL; if true exit; execute body (or inner VARYING); increment; repeat
    /// AFTER clauses become nested inner levels via v.Next.
    /// </summary>
    public IrBasicBlock LowerPerformVarying(BoundPerformVarying v, BoundPerformStatement perf,
        IrMethod method, IrBasicBlock block)
    {
        // Use full IndexExpression (preserves subscripts) when available
        var indexLoc = v.IndexExpression != null
            ? _ctx.Location.ResolveExpressionLocation(v.IndexExpression)
            : _ctx.Location.ResolveLocation(v.Index);
        if (indexLoc == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0500, SourceLocation.None, TextSpan.Empty, v.Index.Name);
            return block;
        }

        // 1. Initialize: MOVE initial TO index
        EmitVaryingMove(v.Initial, indexLoc, block);

        // 2. Loop structure
        var loopStart = method.CreateBlock("vary.start");
        var loopBody = method.CreateBlock("vary.body");
        var loopEnd = method.CreateBlock("vary.end");

        // Create a separate increment block so EXIT PERFORM CYCLE can target it
        var loopIncr = method.CreateBlock("vary.incr");

        // Push continue target: EXIT PERFORM CYCLE jumps to increment block
        // Only the outermost VARYING level manages the continue stack (inner AFTER
        // levels do not — CYCLE always targets the outermost loop).
        bool isOutermost = v == perf.Varying;
        if (isOutermost)
            _ctx.PerformContinueStack.Push(loopIncr);

        if (perf.IsTestAfter)
        {
            // TEST AFTER: do-while — execute body first, then test + increment
            block.Instructions.Add(new IrJump(loopBody));

            // Body
            method.Blocks.Add(loopBody);
            IrBasicBlock bodyCurrent;

            if (v.Next != null)
            {
                bodyCurrent = LowerPerformVarying(v.Next, perf, method, loopBody);
            }
            else
            {
                bodyCurrent = LowerPerformBody(perf, method, loopBody);
            }

            // Test UNTIL (bottom-tested): condition checked after body executes
            bodyCurrent.Instructions.Add(new IrJump(loopStart));
            method.Blocks.Add(loopStart);
            var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            _ctx.Condition.LowerCondition(v.UntilCondition, condVal, loopStart);
            // If condition true → exit; if false → increment and loop
            loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopIncr));
            loopStart.Instructions.Add(new IrJump(loopEnd));

            // Increment: ADD step TO index
            method.Blocks.Add(loopIncr);
            EmitVaryingAdd(v.Step, indexLoc, loopIncr);
            loopIncr.Instructions.Add(new IrJump(loopBody));
        }
        else
        {
            // TEST BEFORE (default): while — test condition first
            block.Instructions.Add(new IrJump(loopStart));

            // Test UNTIL (top-tested)
            method.Blocks.Add(loopStart);
            var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            _ctx.Condition.LowerCondition(v.UntilCondition, condVal, loopStart);
            loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
            loopStart.Instructions.Add(new IrJump(loopEnd));

            // Body
            method.Blocks.Add(loopBody);
            IrBasicBlock bodyCurrent;

            if (v.Next != null)
            {
                bodyCurrent = LowerPerformVarying(v.Next, perf, method, loopBody);
            }
            else
            {
                bodyCurrent = LowerPerformBody(perf, method, loopBody);
            }

            // Jump to increment block (separate so EXIT PERFORM CYCLE can target it)
            bodyCurrent.Instructions.Add(new IrJump(loopIncr));

            // Increment: ADD step TO index, then re-test
            method.Blocks.Add(loopIncr);
            EmitVaryingAdd(v.Step, indexLoc, loopIncr);
            loopIncr.Instructions.Add(new IrJump(loopStart));
        }

        if (isOutermost)
            _ctx.PerformContinueStack.Pop();

        method.Blocks.Add(loopEnd);
        return loopEnd;
    }

    public void EmitVaryingMove(BoundExpression source, IrLocation dest, IrBasicBlock block)
    {
        if (source is BoundLiteralExpression lit && lit.Value is decimal d)
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d, 0));
        }
        else if (ExpressionLowerer.TryExtractNegativeLiteral(source, out var negVal))
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, negVal, 0));
        }
        else if (source is BoundIdentifierExpression id)
        {
            var srcLoc = _ctx.Location.ResolveExpressionLocation(id);
            if (srcLoc != null)
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dest,
                    srcLoc.GetPic(), dest.GetPic()));
        }
        else
        {
            // General expression (arithmetic, etc.): evaluate via COMPUTE and store
            var irExpr = _ctx.Expression.LowerExpression(source);
            if (irExpr != null)
                block.Instructions.Add(new IrComputeStore(irExpr, dest, 0));
        }
    }

    public void EmitVaryingAdd(BoundExpression step, IrLocation indexLoc, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());
        if (step is BoundLiteralExpression litStep && litStep.Value is decimal dStep)
        {
            block.Instructions.Add(new IrPicAddLiteral(indexLoc, dStep, 0));
        }
        else if (ExpressionLowerer.TryExtractNegativeLiteral(step, out var negVal))
        {
            block.Instructions.Add(new IrPicAddLiteral(indexLoc, negVal, 0));
        }
        else if (step is BoundIdentifierExpression idStep)
        {
            var stepLoc = _ctx.Location.ResolveExpressionLocation(idStep);
            if (stepLoc != null)
                block.Instructions.Add(new IrPicAdd(stepLoc, indexLoc, 0));
        }
        else
        {
            // General expression: evaluate step into accumulator, then add to index
            var accumulator = _ctx.ValueFactory.Next(IrPrimitiveType.Decimal);
            var irExpr = _ctx.Expression.LowerExpression(step);
            if (irExpr != null)
            {
                block.Instructions.Add(new IrComputeIntoAccumulator(accumulator, irExpr));
                block.Instructions.Add(new IrAddAccumulatedToTarget(accumulator, indexLoc, 0));
            }
        }
    }

    /// <summary>
    public IrBasicBlock LowerPerformBody(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Inline statements
        if (perf.InlineStatements is { Count: > 0 })
        {
            var current = block;
            foreach (var stmt in perf.InlineStatements)
                current = _ctx.LowerStatement(stmt, method, current);
            return current;
        }

        // Target paragraph(s)
        if (perf.Target != null)
        {
            LowerPerformSimple(perf, block);
        }
        return block;
    }

    public void LowerPerformSimple(BoundPerformStatement perf, IrBasicBlock block)
    {
        if (perf.Target == null) return;
        if (!_ctx.ParagraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0501, SourceLocation.None, TextSpan.Empty, perf.Target.Name);
            return;
        }

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _ctx.ParagraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        // Ensure valid range (section THRU may produce reversed indices)
        if (endIdx < startIdx)
            (startIdx, endIdx) = (endIdx, startIdx);

        if (startIdx == endIdx)
        {
            var paraName = _ctx.ParagraphsByIndex[startIdx];
            if (_ctx.ParagraphMethods.TryGetValue(paraName, out var paraMethod))
                block.Instructions.Add(new IrPerform(paraMethod));
        }
        else
        {
            var methods = new List<IrMethod>();
            for (int i = startIdx; i <= endIdx; i++)
            {
                var paraName = _ctx.ParagraphsByIndex[i];
                if (_ctx.ParagraphMethods.TryGetValue(paraName, out var paraMethod))
                    methods.Add(paraMethod);
                else
                {
                    _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0501, SourceLocation.None, TextSpan.Empty, paraName);
                    continue;
                }
            }
            block.Instructions.Add(new IrPerformThru(startIdx, endIdx, methods));
        }
    }

    /// <summary>
    /// PERFORM para N TIMES: runtime expression-based loop.
    /// Evaluates the TIMES expression, loops that many iterations.
    /// </summary>
    public IrBasicBlock LowerPerformTimes(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Inline PERFORM N TIMES: loop over inline statements
        if (perf.Target == null && perf.InlineStatements is { Count: > 0 })
        {
            return LowerInlinePerformTimes(perf, method, block);
        }

        if (perf.Target == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0502, SourceLocation.None, TextSpan.Empty);
            return block;
        }
        if (!_ctx.ParagraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
            return block;

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _ctx.ParagraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        // Evaluate TIMES expression to a counter and loop
        // Lower as: counter = TIMES expr; while counter > 0: body; counter--
        var loopStart = method.CreateBlock("perf.times.start");
        var loopBody = method.CreateBlock("perf.times.body");
        var loopEnd = method.CreateBlock("perf.times.end");

        _ctx.PerformExitStack.Push(loopEnd);

        // Emit a single IrPerformTimes instruction. The emitter manages the
        // CIL loop counter as a local int, evaluates CountExpression once at entry,
        // and calls the paragraph method(s) in a loop.
        var methods = new List<IrMethod>();
        IrMethod? singleMethod = null;
        if (startIdx == endIdx)
        {
            var paraName = _ctx.ParagraphsByIndex[startIdx];
            _ctx.ParagraphMethods.TryGetValue(paraName, out singleMethod);
        }
        else
        {
            for (int i = startIdx; i <= endIdx; i++)
            {
                var pn = _ctx.ParagraphsByIndex[i];
                _ctx.ParagraphMethods.TryGetValue(pn, out var pm);
                methods.Add(pm!);
            }
        }

        var irCount = _ctx.Expression.LowerExpression(perf.TimesExpression!) ?? new IrLiteral(0m);
        block.Instructions.Add(new IrPerformTimes(
            singleMethod ?? methods[0], startIdx, endIdx, methods, irCount));

        _ctx.PerformExitStack.Pop();
        return block;
    }

    /// <summary>
    /// Inline PERFORM N TIMES: loop over inline statements N times.
    /// Uses PERFORM UNTIL pattern with a synthetic counter condition.
    /// </summary>
    /// <summary>
    /// Inline PERFORM N TIMES: emits IrPerformInlineTimes.
    /// The emitter manages a CIL-local int counter, evaluates CountExpression
    /// once at entry, then loops over the lowered body instructions.
    /// Works for both literal and identifier count expressions.
    /// </summary>
    public IrBasicBlock LowerInlinePerformTimes(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Lower inline body into a temporary block to collect its IR instructions
        var tempBlock = new IrBasicBlock("perf.inline.times.temp");
        var bodyCurrent = tempBlock;
        foreach (var stmt in perf.InlineStatements!)
            bodyCurrent = _ctx.LowerStatement(stmt, method, bodyCurrent);

        // Collect body instructions from tempBlock (single block for simple bodies)
        var bodyInstructions = new List<IrInstruction>(tempBlock.Instructions);

        // Lower the count expression to IR
        var irCount = _ctx.Expression.LowerExpression(perf.TimesExpression!) ?? new IrLiteral(0m);

        // Emit IrPerformInlineTimes — the emitter:
        //   1. Evaluates CountExpression → CIL local int counter
        //   2. Loops: while counter > 0, execute body, counter--
        block.Instructions.Add(new IrPerformInlineTimes(irCount, bodyInstructions));

        return block;
    }

    // ── IF ──

    public IrBasicBlock LowerIf(BoundIfStatement iff, IrMethod method, IrBasicBlock current)
    {
        var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        _ctx.Condition.LowerCondition(iff.Condition, condVal, current);

        var thenBlock = method.CreateBlock("if.then");
        var elseBlock = iff.ElseStatements is { Count: > 0 }
            ? method.CreateBlock("if.else")
            : null;
        var joinBlock = method.CreateBlock("if.join");

        // Branch: if condition false → else (or join if no else)
        current.Instructions.Add(new IrBranchIfFalse(condVal,
            elseBlock ?? joinBlock));

        // THEN block
        method.Blocks.Add(thenBlock);
        var thenCurrent = thenBlock;
        foreach (var stmt in iff.ThenStatements)
            thenCurrent = _ctx.LowerStatement(stmt, method, thenCurrent);
        thenCurrent.Instructions.Add(new IrJump(joinBlock));

        // ELSE block (optional)
        if (elseBlock != null)
        {
            method.Blocks.Add(elseBlock);
            var elseCurrent = elseBlock;
            foreach (var stmt in iff.ElseStatements!)
                elseCurrent = _ctx.LowerStatement(stmt, method, elseCurrent);
            elseCurrent.Instructions.Add(new IrJump(joinBlock));
        }

        // Subsequent statements go to join block
        method.Blocks.Add(joinBlock);
        return joinBlock;
    }

    // ── EVALUATE ──

    public IrBasicBlock LowerEvaluate(BoundEvaluateStatement eval, IrMethod method, IrBasicBlock block)
    {
        var doneBlock = method.CreateBlock("eval.done");

        for (int i = 0; i < eval.Whens.Count; i++)
        {
            var when = eval.Whens[i];
            var whenBody = method.CreateBlock($"eval.when.{i}");
            var nextWhen = method.CreateBlock($"eval.next.{i}");

            // Build match condition: AND across subjects, OR within each subject's values/ranges
            LowerEvaluateWhenMatch(eval, when, whenBody, nextWhen, method, ref block);

            // WHEN body
            method.Blocks.Add(whenBody);
            var bodyCurrent = whenBody;
            foreach (var stmt in when.Statements)
                bodyCurrent = _ctx.LowerStatement(stmt, method, bodyCurrent);
            bodyCurrent.Instructions.Add(new IrJump(doneBlock));

            // Next WHEN
            method.Blocks.Add(nextWhen);
            block = nextWhen;
        }

        // WHEN OTHER
        if (eval.WhenOther is { Count: > 0 })
        {
            var otherCurrent = block;
            foreach (var stmt in eval.WhenOther)
                otherCurrent = _ctx.LowerStatement(stmt, method, otherCurrent);
            otherCurrent.Instructions.Add(new IrJump(doneBlock));
        }
        else
        {
            block.Instructions.Add(new IrJump(doneBlock));
        }

        method.Blocks.Add(doneBlock);
        return doneBlock;
    }

    /// <summary>
    /// For a single WHEN clause, emit the match logic.
    /// Multi-subject: AND across subjects. Within each subject: OR over values/ranges.
    /// EVALUATE TRUE: each SubjectCondition is a BoundEvaluateConditionWhen (standalone condition).
    /// </summary>
    public void LowerEvaluateWhenMatch(
        BoundEvaluateStatement eval,
        BoundEvaluateWhen when,
        IrBasicBlock whenBody,
        IrBasicBlock nextWhen,
        IrMethod method,
        ref IrBasicBlock block)
    {
        int subjectCount = when.SubjectConditions.Count;

        if (subjectCount == 0)
        {
            block.Instructions.Add(new IrJump(nextWhen));
            return;
        }

        // For single subject (common case), simplify: no AND needed
        if (subjectCount == 1)
        {
            LowerEvaluateSubjectMatch(eval, 0, when.SubjectConditions[0],
                whenBody, nextWhen, method, ref block);
            return;
        }

        // Multi-subject: all subjects must match (AND).
        // Strategy: for each subject, if it fails → jump to nextWhen.
        // If all pass, fall through to whenBody.
        for (int k = 0; k < subjectCount; k++)
        {
            var cond = when.SubjectConditions[k];
            if (cond is BoundEvaluateValueCondition vc && vc.IsAny)
                continue; // ANY always matches — skip

            if (k < subjectCount - 1)
            {
                // Not the last subject: if this fails, go to nextWhen
                var nextSubject = method.CreateBlock($"eval.subj.{k + 1}");
                LowerEvaluateSubjectMatch(eval, k, cond, nextSubject, nextWhen, method, ref block);
                method.Blocks.Add(nextSubject);
                block = nextSubject;
            }
            else
            {
                // Last subject: if this passes, go to whenBody; if fails, nextWhen
                LowerEvaluateSubjectMatch(eval, k, cond, whenBody, nextWhen, method, ref block);
            }
        }
    }

    /// <summary>
    /// Match a single subject against its condition.
    /// On match: jump to successBlock. On fail: jump to failBlock.
    /// </summary>
    public void LowerEvaluateSubjectMatch(
        BoundEvaluateStatement eval,
        int subjectIndex,
        BoundEvaluateCondition cond,
        IrBasicBlock successBlock,
        IrBasicBlock failBlock,
        IrMethod method,
        ref IrBasicBlock block)
    {
        if (cond is BoundEvaluateConditionWhen condWhen)
        {
            // EVALUATE TRUE/FALSE: condition is a standalone boolean
            var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            _ctx.Condition.LowerCondition(condWhen.Condition, condVal, block);
            if (eval.IsEvaluateFalse)
            {
                // EVALUATE FALSE: match when condition is FALSE
                block.Instructions.Add(new IrBranchIfFalse(condVal, successBlock));
                block.Instructions.Add(new IrJump(failBlock));
            }
            else
            {
                block.Instructions.Add(new IrBranchIfFalse(condVal, failBlock));
                block.Instructions.Add(new IrJump(successBlock));
            }
            return;
        }

        if (cond is BoundEvaluateValueCondition vc)
        {
            if (vc.IsAny)
            {
                block.Instructions.Add(new IrJump(successBlock));
                return;
            }

            // WHEN NOT: invert match by swapping success/fail targets
            var matchSuccess = vc.IsNot ? failBlock : successBlock;
            var matchFail = vc.IsNot ? successBlock : failBlock;

            var subject = eval.Subjects[subjectIndex];

            // OR over values: if any value matches, success
            foreach (var value in vc.Values)
            {
                var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, value, condVal, block);
                // If true → match success
                var tryNext = method.CreateBlock("eval.val.next");
                block.Instructions.Add(new IrBranchIfFalse(condVal, tryNext));
                block.Instructions.Add(new IrJump(matchSuccess));
                method.Blocks.Add(tryNext);
                block = tryNext;
            }

            // OR over ranges: if any range matches, success
            foreach (var range in vc.Ranges)
            {
                // subject >= from AND subject <= to
                var geVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                var leVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, range.From, geVal, block,
                    BoundBinaryOperatorKind.GreaterOrEqual);
                LowerEvaluateComparison(subject, range.To, leVal, block,
                    BoundBinaryOperatorKind.LessOrEqual);
                // Both must be true → AND
                var tryNextRange = method.CreateBlock("eval.range.next");
                block.Instructions.Add(new IrBranchIfFalse(geVal, tryNextRange));
                block.Instructions.Add(new IrBranchIfFalse(leVal, tryNextRange));
                block.Instructions.Add(new IrJump(matchSuccess));
                method.Blocks.Add(tryNextRange);
                block = tryNextRange;
            }

            // None matched → match fail
            block.Instructions.Add(new IrJump(matchFail));
            return;
        }

        // Unknown condition type — skip to fail
        block.Instructions.Add(new IrJump(failBlock));
    }

    public void LowerEvaluateComparison(BoundExpression subject, BoundExpression obj,
        IrValue result, IrBasicBlock block,
        BoundBinaryOperatorKind op = BoundBinaryOperatorKind.Equal)
    {
        var cmpExpr = new BoundBinaryExpression(subject, op, obj, CobolCategory.Unknown);
        _ctx.Condition.LowerCondition(cmpExpr, result, block);
    }

    // ── ALTER ──

    public void LowerAlter(BoundAlterStatement alter, IrBasicBlock block)
    {
        foreach (var entry in alter.Entries)
        {
            if (!_ctx.AlterSlots.TryGetValue(entry.TargetParagraph.Name, out int slot))
                continue;
            if (!_ctx.ParagraphIndices.TryGetValue(entry.NewDestination.Name, out int newIndex))
                continue;
            block.Instructions.Add(new IrAlter(slot, newIndex));
        }
    }

    // ── GO TO ──

    public void LowerGoTo(BoundGoToStatement gt, IrBasicBlock block)
    {
        // Bare GO TO (no target) — target set by ALTER at runtime
        if (gt.IsBare)
        {
            if (_ctx.CurrentParagraphName != null && _ctx.AlterSlots.TryGetValue(_ctx.CurrentParagraphName, out int bareSlot))
            {
                // Default remains -1 (STOP RUN) — ALTER will set the real target
                block.Instructions.Add(new IrReturnAlterable(bareSlot));
            }
            else
            {
                // Bare GO TO not referenced by ALTER — undefined behavior, emit STOP
                block.Instructions.Add(new IrReturnConst(-1));
            }
            return;
        }

        if (gt.IsSimple)
        {
            // Check if this paragraph is an ALTER target — use indirection table
            if (_ctx.CurrentParagraphName != null && _ctx.AlterSlots.TryGetValue(_ctx.CurrentParagraphName, out int slot))
            {
                // Record the default GO TO target for this alter slot
                if (_ctx.ParagraphIndices.TryGetValue(gt.Target.Name, out int defaultTarget))
                    _ctx.AlterDefaults[slot] = defaultTarget;
                block.Instructions.Add(new IrReturnAlterable(slot));
                return;
            }

            // Simple GO TO: unconditional branch to paragraph (non-alterable)
            if (_ctx.ParagraphIndices.TryGetValue(gt.Target.Name, out int targetIndex))
                block.Instructions.Add(new IrReturnConst(targetIndex));
            else
                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0506, SourceLocation.None, TextSpan.Empty, gt.Target.Name);
            return;
        }

        // GO TO para1 para2 ... DEPENDING ON selector
        if (gt.DependingOn == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0507, SourceLocation.None, TextSpan.Empty);
            return;
        }

        var selectorLoc = _ctx.Location.ResolveExpressionLocation(gt.DependingOn);
        if (selectorLoc == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0508, SourceLocation.None, TextSpan.Empty, gt.DependingOn);
            return;
        }

        // Build list of target paragraph indices
        var targetIndices = new List<int>();
        foreach (var target in gt.Targets)
        {
            if (_ctx.ParagraphIndices.TryGetValue(target.Name, out int idx))
                targetIndices.Add(idx);
            else
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0506, SourceLocation.None, TextSpan.Empty, target.Name);
                targetIndices.Add(-1);
            }
        }

        block.Instructions.Add(new IrGoToDepending(selectorLoc, targetIndices));
    }

    // ── NEXT SENTENCE ──

    public IrBasicBlock LowerNextSentence(IrMethod method, IrBasicBlock block)
    {
        if (_ctx.CurrentSentenceEnd is null)
            throw new InvalidOperationException("NEXT SENTENCE used outside a sentence context.");

        // Jump to the end of the current sentence
        block.Instructions.Add(new IrJump(_ctx.CurrentSentenceEnd));

        // Any statements after NEXT SENTENCE in this sentence are unreachable.
        // Create a dead block so subsequent lowering has somewhere to emit into.
        var dead = new IrBasicBlock("dead_after_next_sentence");
        method.Blocks.Add(dead);
        return dead;
    }

    // ── EXIT PERFORM / EXIT PARAGRAPH / EXIT SECTION ──

    public IrBasicBlock LowerExitPerform(BoundExitPerformStatement exitPerf, IrMethod method, IrBasicBlock block)
    {
        if (_ctx.PerformExitStack.Count == 0)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0509, SourceLocation.None, TextSpan.Empty, "PERFORM", "PERFORM");
            return block;
        }

        if (exitPerf.IsCycle)
        {
            // EXIT PERFORM CYCLE: jump to the loop's continue target
            // (condition re-test for UNTIL; increment block for VARYING)
            if (_ctx.PerformContinueStack.Count == 0)
            {
                _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0509, SourceLocation.None, TextSpan.Empty, "PERFORM CYCLE", "PERFORM");
                return block;
            }
            var continueBlock = _ctx.PerformContinueStack.Peek();
            block.Instructions.Add(new IrJump(continueBlock));
        }
        else
        {
            // EXIT PERFORM: jump to the loop's exit target (break)
            var exitBlock = _ctx.PerformExitStack.Peek();
            block.Instructions.Add(new IrJump(exitBlock));
        }

        var dead = new IrBasicBlock("dead_after_exit_perform");
        method.Blocks.Add(dead);
        return dead;
    }

    public IrBasicBlock LowerExitParagraph(IrMethod method, IrBasicBlock block)
    {
        if (_ctx.ParagraphEndBlock == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0509, SourceLocation.None, TextSpan.Empty, "PARAGRAPH", "paragraph");
            return block;
        }

        block.Instructions.Add(new IrJump(_ctx.ParagraphEndBlock));

        var dead = new IrBasicBlock("dead_after_exit_paragraph");
        method.Blocks.Add(dead);
        return dead;
    }

    public IrBasicBlock LowerExitSection(IrMethod method, IrBasicBlock block)
    {
        if (_ctx.SectionExitReturnIndex == null)
        {
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0509, SourceLocation.None, TextSpan.Empty, "SECTION", "section");
            return block;
        }

        // Return the index of the first paragraph after the section,
        // causing the dispatcher to skip remaining paragraphs in this section.
        block.Instructions.Add(new IrReturnConst(_ctx.SectionExitReturnIndex.Value));

        var dead = new IrBasicBlock("dead_after_exit_section");
        method.Blocks.Add(dead);
        return dead;
    }

    // ── SEARCH (linear) ──

    public IrBasicBlock LowerSearch(BoundSearchStatement search, IrMethod method, IrBasicBlock block)
    {
        if (search.Index == null) return block;

        var indexLoc = _ctx.Location.ResolveLocation(search.Index);
        if (indexLoc == null) return block;

        var occurs = search.Table.Symbol.Occurs;
        int staticUpperBound = occurs?.MaxOccurs ?? 1;

        // Resolve ODO DEPENDING ON variable for runtime upper bound
        IrLocation? odoLoc = null;
        if (occurs?.DependingOnSymbol != null)
            odoLoc = _ctx.Location.ResolveLocation(occurs.DependingOnSymbol);

        // COBOL-85 §14.9.38: SEARCH uses the CURRENT index value.
        // If the index already exceeds the table, AT END is triggered immediately.
        // The index is NOT reset — the programmer must SET it before SEARCH.

        // Loop structure
        var loopHeader = method.CreateBlock("search.loop");
        var atEndBlock = method.CreateBlock("search.atend");
        var exitBlock = method.CreateBlock("search.exit");

        block.Instructions.Add(new IrJump(loopHeader));
        method.Blocks.Add(loopHeader);

        // Bounds check: if index > upperBound → AT END
        // When ODO is active, compare against the DEPENDING ON variable at runtime
        var boundsResult = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        if (odoLoc != null)
        {
            loopHeader.Instructions.Add(new IrPicCompare(
                indexLoc, odoLoc, boundsResult,
                (int)BoundBinaryOperatorKind.Greater));
        }
        else
        {
            loopHeader.Instructions.Add(new IrPicCompareLiteral(
                indexLoc, (decimal)staticUpperBound, boundsResult,
                (int)BoundBinaryOperatorKind.Greater));
        }
        var whenChain = method.CreateBlock("search.whens");
        loopHeader.Instructions.Add(new IrBranchIfFalse(boundsResult, whenChain));
        loopHeader.Instructions.Add(new IrJump(atEndBlock));

        // WHEN chain: evaluate each condition, execute body on match
        method.Blocks.Add(whenChain);
        var current = whenChain;

        foreach (var when in search.Whens)
        {
            var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            _ctx.Condition.LowerCondition(when.Condition, condVal, current);

            var bodyBlock = method.CreateBlock("search.when.body");
            var nextWhen = method.CreateBlock("search.when.next");

            current.Instructions.Add(new IrBranchIfFalse(condVal, nextWhen));

            // WHEN body
            method.Blocks.Add(bodyBlock);
            var bodyCurrent = bodyBlock;
            foreach (var stmt in when.Statements)
                bodyCurrent = _ctx.LowerStatement(stmt, method, bodyCurrent);
            bodyCurrent.Instructions.Add(new IrJump(exitBlock));

            method.Blocks.Add(nextWhen);
            current = nextWhen;
        }

        // No WHEN matched: increment index (and varying, if present) and loop
        block.Instructions.Add(new IrInitArithmeticStatus());
        current.Instructions.Add(new IrPicAddLiteral(indexLoc, 1m));
        if (search.VaryingSymbol != null
            && search.VaryingSymbol.Symbol != search.Index.Symbol)
        {
            var varyLoc = _ctx.Location.ResolveLocation(search.VaryingSymbol);
            if (varyLoc != null)
                current.Instructions.Add(new IrPicAddLiteral(varyLoc, 1m));
        }
        current.Instructions.Add(new IrJump(loopHeader));

        // AT END
        method.Blocks.Add(atEndBlock);
        var atEndCurrent = atEndBlock;
        foreach (var stmt in search.AtEnd)
            atEndCurrent = _ctx.LowerStatement(stmt, method, atEndCurrent);
        atEndCurrent.Instructions.Add(new IrJump(exitBlock));

        method.Blocks.Add(exitBlock);
        return exitBlock;
    }

    // ── SEARCH ALL (binary) ──

    public IrBasicBlock LowerSearchAll(BoundSearchAllStatement searchAll, IrMethod method, IrBasicBlock block)
    {
        if (searchAll.Index == null || searchAll.Whens.Count == 0) return block;

        var indexLoc = _ctx.Location.ResolveLocation(searchAll.Index);
        if (indexLoc == null) return block;

        var occurs = searchAll.Table.Symbol.Occurs;
        int upperBound = occurs?.MaxOccurs ?? 1;
        var when = searchAll.Whens[0]; // SEARCH ALL allows exactly one WHEN

        // Resolve ODO DEPENDING ON variable for runtime upper bound
        IrLocation? odoLoc = null;
        if (occurs?.DependingOnSymbol != null)
            odoLoc = _ctx.Location.ResolveLocation(occurs.DependingOnSymbol);

        // Determine sort direction from table's KEY clause.
        // ASCENDING KEY → key < target means search right (higher indices).
        // DESCENDING KEY → key < target means search left (lower indices).
        bool isAscending = occurs == null || occurs.AscendingKeys.Count > 0 || occurs.DescendingKeys.Count == 0;

        // Extract the first relational comparison from the WHEN condition
        // to build a less-than test for binary search direction.
        var directionComparison = ExtractFirstRelationalComparison(when.Condition);

        // Compile-time unrolled binary search tree.
        // For a table of size N, we generate ceil(log2(N))+1 levels of
        // if-else blocks. Each node: set index=mid, test equality, if no match
        // test direction comparison and branch to left or right subtree.
        var atEndBlock = method.CreateBlock("searchall.atend");
        var exitBlock = method.CreateBlock("searchall.exit");

        // Initialize arithmetic status once before the tree
        block.Instructions.Add(new IrInitArithmeticStatus());

        // Generate the unrolled binary search tree
        EmitBinarySearchNode(searchAll, when, directionComparison, isAscending,
            indexLoc, 1, upperBound, method, block, atEndBlock, exitBlock, odoLoc);

        // AT END
        method.Blocks.Add(atEndBlock);
        var atEndCurrent = atEndBlock;
        foreach (var stmt in searchAll.AtEnd)
            atEndCurrent = _ctx.LowerStatement(stmt, method, atEndCurrent);
        atEndCurrent.Instructions.Add(new IrJump(exitBlock));

        method.Blocks.Add(exitBlock);
        return exitBlock;
    }

    /// <summary>
    /// Recursively emit a binary search tree node for SEARCH ALL.
    /// Sets index=mid, tests the WHEN condition for equality, and if no match
    /// uses a less-than comparison to branch into left or right subtree.
    /// </summary>
    public void EmitBinarySearchNode(
        BoundSearchAllStatement searchAll,
        BoundSearchWhenClause when,
        BoundBinaryExpression? directionComparison,
        bool isAscending,
        IrLocation indexLoc,
        int lo, int hi,
        IrMethod method,
        IrBasicBlock block,
        IrBasicBlock atEndBlock,
        IrBasicBlock exitBlock,
        IrLocation? odoLoc = null)
    {
        if (lo > hi)
        {
            // Empty range → AT END
            block.Instructions.Add(new IrJump(atEndBlock));
            return;
        }

        int mid = lo + (hi - lo) / 2;

        // ODO bounds check: if mid > active ODO count, this element is inactive → AT END
        if (odoLoc != null)
        {
            var odoCheck = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrPicCompareLiteral(
                odoLoc, (decimal)mid, odoCheck,
                (int)BoundBinaryOperatorKind.Less));
            var inBoundsBlock = method.CreateBlock("searchall.inbounds");
            // If odoCount < mid → out of bounds, search left half or AT END
            if (lo < mid)
            {
                var odoLeftBlock = method.CreateBlock("searchall.odoleft");
                block.Instructions.Add(new IrBranchIfFalse(odoCheck, inBoundsBlock));
                block.Instructions.Add(new IrJump(odoLeftBlock));
                method.Blocks.Add(odoLeftBlock);
                EmitBinarySearchNode(searchAll, when, directionComparison, isAscending,
                    indexLoc, lo, mid - 1, method, odoLeftBlock, atEndBlock, exitBlock, odoLoc);
            }
            else
            {
                block.Instructions.Add(new IrBranchIfFalse(odoCheck, inBoundsBlock));
                block.Instructions.Add(new IrJump(atEndBlock));
            }
            method.Blocks.Add(inBoundsBlock);
            block = inBoundsBlock;
        }

        // Set index = mid
        block.Instructions.Add(new IrPicMoveLiteralNumeric(indexLoc, (decimal)mid));

        // Test WHEN condition (equality)
        var condVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
        _ctx.Condition.LowerCondition(when.Condition, condVal, block);

        var bodyBlock = method.CreateBlock("searchall.body");
        var noMatchBlock = method.CreateBlock("searchall.nomatch");

        block.Instructions.Add(new IrBranchIfFalse(condVal, noMatchBlock));

        // Match: execute body
        method.Blocks.Add(bodyBlock);
        var bodyCurrent = bodyBlock;
        foreach (var stmt in when.Statements)
            bodyCurrent = _ctx.LowerStatement(stmt, method, bodyCurrent);
        bodyCurrent.Instructions.Add(new IrJump(exitBlock));

        // No match: determine direction
        method.Blocks.Add(noMatchBlock);

        if (lo == hi)
        {
            // Leaf node, no children to recurse into → AT END
            noMatchBlock.Instructions.Add(new IrJump(atEndBlock));
            return;
        }

        if (directionComparison != null)
        {
            // Build a less-than comparison from the WHEN's equality comparison
            var lessComparison = new BoundBinaryExpression(
                directionComparison.Left,
                BoundBinaryOperatorKind.Less,
                directionComparison.Right,
                directionComparison.Category);

            var lessVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
            _ctx.Condition.LowerCondition(lessComparison, lessVal, noMatchBlock);

            var leftBlock = method.CreateBlock("searchall.left");
            var rightBlock = method.CreateBlock("searchall.right");

            // For ASCENDING: key < target → search right half (higher indices)
            // For DESCENDING: key < target → search left half (lower indices)
            if (isAscending)
            {
                noMatchBlock.Instructions.Add(new IrBranchIfFalse(lessVal, leftBlock));
                noMatchBlock.Instructions.Add(new IrJump(rightBlock));
            }
            else
            {
                noMatchBlock.Instructions.Add(new IrBranchIfFalse(lessVal, rightBlock));
                noMatchBlock.Instructions.Add(new IrJump(leftBlock));
            }

            // Left subtree: lo..mid-1
            method.Blocks.Add(leftBlock);
            EmitBinarySearchNode(searchAll, when, directionComparison, isAscending,
                indexLoc, lo, mid - 1, method, leftBlock, atEndBlock, exitBlock, odoLoc);

            // Right subtree: mid+1..hi
            method.Blocks.Add(rightBlock);
            EmitBinarySearchNode(searchAll, when, directionComparison, isAscending,
                indexLoc, mid + 1, hi, method, rightBlock, atEndBlock, exitBlock, odoLoc);
        }
        else
        {
            // No relational comparison extracted — fall back to linear scan of remaining range.
            // This handles compound conditions where direction can't be determined.
            var scanBlock = noMatchBlock;

            // Try each remaining index in order
            for (int i = lo; i <= hi; i++)
            {
                if (i == mid) continue; // Already tested

                scanBlock.Instructions.Add(new IrPicMoveLiteralNumeric(indexLoc, (decimal)i));
                var tryCondVal = _ctx.ValueFactory.Next(IrPrimitiveType.Bool);
                _ctx.Condition.LowerCondition(when.Condition, tryCondVal, scanBlock);

                var tryBodyBlock = method.CreateBlock("searchall.trybody");
                var nextTryBlock = method.CreateBlock("searchall.trynext");
                scanBlock.Instructions.Add(new IrBranchIfFalse(tryCondVal, nextTryBlock));

                method.Blocks.Add(tryBodyBlock);
                var tryBodyCurrent = tryBodyBlock;
                foreach (var stmt in when.Statements)
                    tryBodyCurrent = _ctx.LowerStatement(stmt, method, tryBodyCurrent);
                tryBodyCurrent.Instructions.Add(new IrJump(exitBlock));

                method.Blocks.Add(nextTryBlock);
                scanBlock = nextTryBlock;
            }

            scanBlock.Instructions.Add(new IrJump(atEndBlock));
        }
    }

    /// <summary>
    /// Extract the first relational (Equal) comparison from a SEARCH ALL WHEN condition.
    /// The condition may be a single equality or ANDed equalities. We extract the first
    /// one to use for binary search direction testing.
    /// </summary>
    public static BoundBinaryExpression? ExtractFirstRelationalComparison(BoundExpression condition)
    {
        if (condition is BoundBinaryExpression bin)
        {
            if (bin.OperatorKind == BoundBinaryOperatorKind.Equal)
                return bin;

            if (bin.OperatorKind == BoundBinaryOperatorKind.And)
            {
                // Try left side first, then right
                return ExtractFirstRelationalComparison(bin.Left)
                    ?? ExtractFirstRelationalComparison(bin.Right);
            }
        }
        return null;
    }
}
