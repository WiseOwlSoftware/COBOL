// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics.Bound;

/// <summary>
/// Post-binding validation pass that enforces:
/// - Expression type rules on PERFORM, IF, EVALUATE (CBL23xx/24xx/25xx)
/// - File I/O organization constraints on OPEN, READ, START, REWRITE, DELETE (CBL0701/16xx/17xx/1901/2001)
/// - Sort/merge constraints on RETURN (CBL2101)
/// - Dynamic CALL warnings (CBL3310)
/// Runs after BoundTreeBuilder and ProcedureGraph, before IR lowering.
/// </summary>
public static class BoundTreeValidator
{
    public static void Validate(BoundProgram program, DiagnosticBag diagnostics)
    {
        foreach (var para in program.Paragraphs)
        {
            int line = para.Symbol.Line;
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                    WalkStatement(stmt, line, diagnostics);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Recursive statement walker
    // ═══════════════════════════════════════════════════════════════

    private static void WalkStatement(BoundStatement stmt, int line, DiagnosticBag diagnostics)
    {
        switch (stmt)
        {
            case BoundPerformStatement perf:
                ValidatePerform(perf, line, diagnostics);
                if (perf.InlineStatements != null)
                    WalkStatements(perf.InlineStatements, line, diagnostics);
                break;

            case BoundIfStatement ifStmt:
                ValidateIf(ifStmt, line, diagnostics);
                WalkStatements(ifStmt.ThenStatements, line, diagnostics);
                if (ifStmt.ElseStatements != null)
                    WalkStatements(ifStmt.ElseStatements, line, diagnostics);
                break;

            case BoundEvaluateStatement eval:
                ValidateEvaluate(eval, line, diagnostics);
                foreach (var when in eval.Whens)
                    WalkStatements(when.Statements, line, diagnostics);
                if (eval.WhenOther != null)
                    WalkStatements(eval.WhenOther, line, diagnostics);
                break;

            case BoundSearchStatement search:
                foreach (var when in search.Whens)
                    WalkStatements(when.Statements, line, diagnostics);
                WalkStatements(search.AtEnd, line, diagnostics);
                break;

            case BoundCompoundStatement compound:
                WalkStatements(compound.Statements, line, diagnostics);
                break;

            case BoundReadStatement read:
                ValidateRead(read, line, diagnostics);
                WalkStatements(read.AtEnd, line, diagnostics);
                WalkStatements(read.NotAtEnd, line, diagnostics);
                break;

            case BoundArithmeticStatement arith:
                if (arith.SizeError != null)
                {
                    WalkStatements(arith.SizeError.OnSizeError, line, diagnostics);
                    WalkStatements(arith.SizeError.NotOnSizeError, line, diagnostics);
                }
                break;

            case BoundStringStatement str:
                WalkStatements(str.OnOverflow, line, diagnostics);
                WalkStatements(str.NotOnOverflow, line, diagnostics);
                break;

            case BoundUnstringStatement unstr:
                WalkStatements(unstr.OnOverflow, line, diagnostics);
                WalkStatements(unstr.NotOnOverflow, line, diagnostics);
                break;

            case BoundDeleteStatement del:
                ValidateDelete(del, line, diagnostics);
                WalkStatements(del.InvalidKey, line, diagnostics);
                WalkStatements(del.NotInvalidKey, line, diagnostics);
                break;

            case BoundStartStatement start:
                ValidateStart(start, line, diagnostics);
                WalkStatements(start.InvalidKey, line, diagnostics);
                WalkStatements(start.NotInvalidKey, line, diagnostics);
                break;

            case BoundRewriteStatement rewrite:
                ValidateRewrite(rewrite, line, diagnostics);
                break;

            case BoundWriteStatement write:
                ValidateWrite(write, line, diagnostics);
                break;

            case BoundOpenStatement open:
                ValidateOpen(open, line, diagnostics);
                break;

            case BoundReturnStatement ret:
                ValidateReturn(ret, line, diagnostics);
                WalkStatements(ret.AtEnd, line, diagnostics);
                WalkStatements(ret.NotAtEnd, line, diagnostics);
                break;

            case BoundCallStatement call:
                ValidateCall(call, line, diagnostics);
                WalkStatements(call.OnException, line, diagnostics);
                WalkStatements(call.NotOnException, line, diagnostics);
                break;
        }
    }

    private static void WalkStatements(IReadOnlyList<BoundStatement> statements, int line, DiagnosticBag diagnostics)
    {
        foreach (var stmt in statements)
            WalkStatement(stmt, line, diagnostics);
    }

    // ═══════════════════════════════════════════════════════════════
    // PERFORM validation (CBL2303–CBL2308)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidatePerform(BoundPerformStatement perf, int line, DiagnosticBag diagnostics)
    {
        // CBL2302: PERFORM THRU out of order
        if (perf.Target != null && perf.ThruTarget != null)
        {
            if (perf.ThruTarget.Line < perf.Target.Line)
                Report(diagnostics, line, DiagnosticDescriptors.CBL2302,
                    perf.Target.Name, perf.ThruTarget.Name);
        }

        // CBL2303: PERFORM TIMES must be integer numeric
        if (perf.TimesExpression != null)
        {
            var type = perf.TimesExpression.ResultType;
            if (type != null && !type.IsNumeric)
                Report(diagnostics, line, DiagnosticDescriptors.CBL2303);
        }

        // CBL2304: PERFORM UNTIL condition must be boolean
        if (perf.UntilCondition != null)
        {
            var type = perf.UntilCondition.ResultType;
            if (type != null && !type.IsBoolean)
                Report(diagnostics, line, DiagnosticDescriptors.CBL2304);
        }

        // Validate VARYING chain
        var varying = perf.Varying;
        while (varying != null)
        {
            ValidateVarying(varying, line, diagnostics);
            varying = varying.Next;
        }
    }

    private static void ValidateVarying(BoundPerformVarying varying, int line, DiagnosticBag diagnostics)
    {
        // CBL2305: VARYING control must be integer/index
        var indexType = ExpressionType.FromDataSymbol(varying.Index);
        if (!indexType.IsNumeric)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2305);

        // CBL2306: VARYING FROM must be numeric
        var fromType = varying.Initial.ResultType;
        if (fromType != null && !fromType.IsNumeric)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2306);

        // CBL2307: VARYING BY must be numeric
        var byType = varying.Step.ResultType;
        if (byType != null && !byType.IsNumeric)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2307);

        // CBL2308: VARYING UNTIL must be boolean
        var untilType = varying.UntilCondition.ResultType;
        if (untilType != null && !untilType.IsBoolean)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2308);
    }

    // ═══════════════════════════════════════════════════════════════
    // IF validation (CBL2401)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateIf(BoundIfStatement ifStmt, int line, DiagnosticBag diagnostics)
    {
        // CBL2401: IF condition must be boolean
        var type = ifStmt.Condition.ResultType;
        if (type != null && !type.IsBoolean)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2401);
    }

    // ═══════════════════════════════════════════════════════════════
    // EVALUATE validation (CBL2501–CBL2503)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateEvaluate(BoundEvaluateStatement eval, int line, DiagnosticBag diagnostics)
    {
        if (eval.IsEvaluateTrue)
        {
            ValidateEvaluateTrue(eval, line, diagnostics);
        }
        else
        {
            ValidateEvaluateSubjects(eval, line, diagnostics);
        }

        // CBL2502: EVALUATE missing WHEN OTHER (warning)
        if (eval.WhenOther == null)
            Report(diagnostics, line, DiagnosticDescriptors.CBL2502);
    }

    private static void ValidateEvaluateTrue(BoundEvaluateStatement eval, int line, DiagnosticBag diagnostics)
    {
        // CBL2503: EVALUATE TRUE WHEN must be boolean
        foreach (var when in eval.Whens)
        {
            foreach (var cond in when.SubjectConditions)
            {
                if (cond is BoundEvaluateConditionWhen condWhen)
                {
                    var type = condWhen.Condition.ResultType;
                    if (type != null && !type.IsBoolean)
                        Report(diagnostics, line, DiagnosticDescriptors.CBL2503);
                }
            }
        }
    }

    private static void ValidateEvaluateSubjects(BoundEvaluateStatement eval, int line, DiagnosticBag diagnostics)
    {
        // CBL2501: EVALUATE WHEN type incompatible with subject
        for (int si = 0; si < eval.Subjects.Count; si++)
        {
            var subjectType = eval.Subjects[si].ResultType;
            if (subjectType == null) continue;

            foreach (var when in eval.Whens)
            {
                if (si >= when.SubjectConditions.Count) continue;

                if (when.SubjectConditions[si] is BoundEvaluateValueCondition valueCond)
                {
                    if (valueCond.IsAny) continue;

                    foreach (var val in valueCond.Values)
                    {
                        if (!IsTypeCompatible(subjectType, val.ResultType))
                            Report(diagnostics, line, DiagnosticDescriptors.CBL2501);
                    }

                    foreach (var range in valueCond.Ranges)
                    {
                        if (!IsTypeCompatible(subjectType, range.From.ResultType))
                            Report(diagnostics, line, DiagnosticDescriptors.CBL2501);
                        if (!IsTypeCompatible(subjectType, range.To.ResultType))
                            Report(diagnostics, line, DiagnosticDescriptors.CBL2501);
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // File I/O organization validation (CBL1601, CBL1901, CBL2001)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>CBL1601: START not allowed on sequential files. CBL1603: KEY operand check.</summary>
    private static void ValidateStart(BoundStartStatement start, int line, DiagnosticBag diagnostics)
    {
        if (IsSequentialOrganization(start.File))
            Report(diagnostics, line, DiagnosticDescriptors.CBL1601);

        // CBL1603: START KEY operand not a record key of file
        if (start.KeyCondition is BoundBinaryExpression binExpr
            && binExpr.Left is BoundIdentifierExpression keyId
            && start.File.RecordKey != null
            && !string.Equals(keyId.Symbol.Name, start.File.RecordKey, StringComparison.OrdinalIgnoreCase))
        {
            Report(diagnostics, line, DiagnosticDescriptors.CBL1603);
        }
    }

    /// <summary>CBL1801: WRITE FROM source incompatible with record.</summary>
    private static void ValidateWrite(BoundWriteStatement write, int line, DiagnosticBag diagnostics)
    {
        // CBL1801: WRITE FROM source must be compatible with record
        if (write.From != null && write.File?.Record != null)
        {
            var sourceType = write.From.ResultType;
            var recordType = ExpressionType.FromDataSymbol(write.File.Record);
            // Group records accept any source (group move). For elementary records,
            // check that source is not boolean (conditions can't be MOVE sources).
            if (sourceType != null && recordType.Kind != ExpressionTypeKind.Group
                && sourceType.IsBoolean)
                Report(diagnostics, line, DiagnosticDescriptors.CBL1801);
        }
    }

    /// <summary>CBL1901: REWRITE not allowed on sequential files. CBL1902: FROM incompatible.</summary>
    private static void ValidateRewrite(BoundRewriteStatement rewrite, int line, DiagnosticBag diagnostics)
    {
        if (IsSequentialOrganization(rewrite.File))
            Report(diagnostics, line, DiagnosticDescriptors.CBL1901);

        // CBL1902: REWRITE FROM source must be compatible with record
        if (rewrite.From != null)
        {
            var sourceType = rewrite.From.ResultType;
            var recordType = ExpressionType.FromDataSymbol(rewrite.Record);
            if (sourceType != null && recordType.Kind != ExpressionTypeKind.Group
                && sourceType.IsBoolean)
                Report(diagnostics, line, DiagnosticDescriptors.CBL1902);
        }
    }

    /// <summary>CBL2001: DELETE not allowed on sequential files.</summary>
    private static void ValidateDelete(BoundDeleteStatement del, int line, DiagnosticBag diagnostics)
    {
        if (IsSequentialOrganization(del.File))
            Report(diagnostics, line, DiagnosticDescriptors.CBL2001);
    }

    // ═══════════════════════════════════════════════════════════════
    // RETURN validation (CBL2101)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateReturn(BoundReturnStatement ret, int line, DiagnosticBag diagnostics)
    {
        // CBL2101: RETURN on non-sort/merge file — no sort/merge support yet
        Report(diagnostics, line, DiagnosticDescriptors.CBL2101);
    }

    // ═══════════════════════════════════════════════════════════════
    // CALL validation (CBL3310)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateCall(BoundCallStatement call, int line, DiagnosticBag diagnostics)
    {
        // CBL3310: Dynamic CALL — parameter list cannot be validated at compile time
        if (call.IsDynamic)
            Report(diagnostics, line, DiagnosticDescriptors.CBL3310);

        // CBL3304: RETURNING item must be in LINKAGE SECTION
        if (call.ReturningTarget is BoundIdentifierExpression returning
            && returning.Symbol.Area != StorageAreaKind.LinkageSection)
            Report(diagnostics, line, DiagnosticDescriptors.CBL3304);
    }

    // ═══════════════════════════════════════════════════════════════
    // READ validation (CBL1701–CBL1703)
    // ═══════════════════════════════════════════════════════════════

    private static void ValidateRead(BoundReadStatement read, int line, DiagnosticBag diagnostics)
    {
        // CBL1701: READ NEXT/PREVIOUS invalid for random-access-only
        if (read.IsNext
            && string.Equals(read.File.AccessMode, "RANDOM", StringComparison.OrdinalIgnoreCase))
            Report(diagnostics, line, DiagnosticDescriptors.CBL1701);

        // CBL1702: READ KEY not allowed on non-indexed file
        if (read.KeyDataName != null && !IsIndexedOrganization(read.File))
            Report(diagnostics, line, DiagnosticDescriptors.CBL1702);

        // CBL1703: READ KEY not a record/alternate key of file
        if (read.KeyDataName != null && read.File.RecordKey != null
            && !string.Equals(read.KeyDataName, read.File.RecordKey, StringComparison.OrdinalIgnoreCase))
            Report(diagnostics, line, DiagnosticDescriptors.CBL1703);
    }

    // ═══════════════════════════════════════════════════════════════
    // OPEN validation (CBL0701)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>CBL0701: OPEN EXTEND only allowed on sequential files.</summary>
    private static void ValidateOpen(BoundOpenStatement open, int line, DiagnosticBag diagnostics)
    {
        foreach (var file in open.Files)
        {
            if (open.Mode == OpenMode.Extend && !IsSequentialOrganization(file))
                Report(diagnostics, line, DiagnosticDescriptors.CBL0701, file.Name);
        }
    }

    private static bool IsSequentialOrganization(FileSymbol file)
        => file.Organization == null
           || string.Equals(file.Organization, "SEQUENTIAL", StringComparison.OrdinalIgnoreCase);

    private static bool IsIndexedOrganization(FileSymbol file)
        => string.Equals(file.Organization, "INDEXED", StringComparison.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if two expression types are compatible for EVALUATE matching.
    /// Numeric/numeric and alphanumeric/alphanumeric are compatible.
    /// Group is treated as alphanumeric. Unknown or null types are permissive (no error).
    /// </summary>
    private static bool IsTypeCompatible(ExpressionType subject, ExpressionType? value)
    {
        if (value == null) return true;
        if (subject.Kind == ExpressionTypeKind.Unknown || value.Kind == ExpressionTypeKind.Unknown)
            return true;

        // Numeric vs numeric → OK
        if (subject.IsNumeric && value.IsNumeric) return true;

        // Alphanumeric/group vs alphanumeric/group → OK
        bool subjectIsAlpha = subject.IsAlphanumeric || subject.Kind == ExpressionTypeKind.Group;
        bool valueIsAlpha = value.IsAlphanumeric || value.Kind == ExpressionTypeKind.Group;
        if (subjectIsAlpha && valueIsAlpha) return true;

        // Boolean vs boolean → OK (EVALUATE TRUE handled separately, but just in case)
        if (subject.IsBoolean && value.IsBoolean) return true;

        return false;
    }

    private static void Report(DiagnosticBag diagnostics, int line, DiagnosticDescriptor descriptor,
        params object[] args)
    {
        diagnostics.Report(descriptor,
            new SourceLocation("<source>", 0, line, 0),
            TextSpan.Empty,
            args);
    }
}
