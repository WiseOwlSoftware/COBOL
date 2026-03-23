// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Flow-sensitive validation pass for file I/O operations.
/// Tracks file open/close state across statement sequences within paragraphs.
/// - CBL0702: I/O operation on a file that has not been OPENed
/// - CBL3206: FILE STATUS variable not checked between I/O operations
/// Runs after binding, before IR lowering.
/// </summary>
public static class FileStateValidator
{
    public static void Validate(BoundProgram program, DiagnosticBag diagnostics)
    {
        // Track which files have been OPENed across the program
        // (simple forward-walk — no branch sensitivity)
        var openedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingStatusCheck = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // pendingStatusCheck maps: file name → FILE STATUS variable name (waiting for check)

        foreach (var para in program.Paragraphs)
        {
            foreach (var sentence in para.Sentences)
            {
                var statements = sentence.Statements;
                for (int i = 0; i < statements.Count; i++)
                {
                    var stmt = statements[i];
                    int line = para.Symbol.Line;
                    ProcessStatement(stmt, line, openedFiles, pendingStatusCheck, diagnostics);
                }
            }
        }
    }

    private static void ProcessStatement(
        BoundStatement stmt, int line,
        HashSet<string> openedFiles,
        Dictionary<string, string> pendingStatusCheck,
        DiagnosticBag diagnostics)
    {
        switch (stmt)
        {
            case BoundOpenStatement open:
                foreach (var file in open.Files)
                {
                    openedFiles.Add(file.Name);
                    CheckAndRecordStatus(file, pendingStatusCheck, line, diagnostics);
                }
                break;

            case BoundCloseStatement close:
                foreach (var file in close.Files)
                {
                    CheckFileOpen(file.Name, line, openedFiles, diagnostics);
                    openedFiles.Remove(file.Name);
                    CheckAndRecordStatus(file, pendingStatusCheck, line, diagnostics);
                }
                break;

            case BoundReadStatement read:
                CheckFileOpen(read.File.Name, line, openedFiles, diagnostics);
                CheckAndRecordStatus(read.File, pendingStatusCheck, line, diagnostics);
                WalkStatements(read.AtEnd, line, openedFiles, pendingStatusCheck, diagnostics);
                WalkStatements(read.NotAtEnd, line, openedFiles, pendingStatusCheck, diagnostics);
                break;

            case BoundWriteStatement write:
                if (write.File != null)
                {
                    CheckFileOpen(write.File.Name, line, openedFiles, diagnostics);
                    CheckAndRecordStatus(write.File, pendingStatusCheck, line, diagnostics);
                }
                break;

            case BoundRewriteStatement rewrite:
                CheckFileOpen(rewrite.File.Name, line, openedFiles, diagnostics);
                CheckAndRecordStatus(rewrite.File, pendingStatusCheck, line, diagnostics);
                break;

            case BoundDeleteStatement del:
                CheckFileOpen(del.File.Name, line, openedFiles, diagnostics);
                CheckAndRecordStatus(del.File, pendingStatusCheck, line, diagnostics);
                WalkStatements(del.InvalidKey, line, openedFiles, pendingStatusCheck, diagnostics);
                WalkStatements(del.NotInvalidKey, line, openedFiles, pendingStatusCheck, diagnostics);
                break;

            case BoundStartStatement start:
                CheckFileOpen(start.File.Name, line, openedFiles, diagnostics);
                CheckAndRecordStatus(start.File, pendingStatusCheck, line, diagnostics);
                WalkStatements(start.InvalidKey, line, openedFiles, pendingStatusCheck, diagnostics);
                WalkStatements(start.NotInvalidKey, line, openedFiles, pendingStatusCheck, diagnostics);
                break;

            // Statements that may reference FILE STATUS variables (clear pending check)
            case BoundIfStatement iff:
                ClearStatusIfReferenced(iff.Condition, pendingStatusCheck);
                WalkStatements(iff.ThenStatements, line, openedFiles, pendingStatusCheck, diagnostics);
                if (iff.ElseStatements != null)
                    WalkStatements(iff.ElseStatements, line, openedFiles, pendingStatusCheck, diagnostics);
                break;

            case BoundEvaluateStatement eval:
                foreach (var subject in eval.Subjects)
                    ClearStatusIfReferenced(subject, pendingStatusCheck);
                foreach (var when in eval.Whens)
                    WalkStatements(when.Statements, line, openedFiles, pendingStatusCheck, diagnostics);
                if (eval.WhenOther != null)
                    WalkStatements(eval.WhenOther, line, openedFiles, pendingStatusCheck, diagnostics);
                break;

            case BoundDisplayStatement display:
                foreach (var operand in display.Operands)
                    ClearStatusIfReferenced(operand, pendingStatusCheck);
                break;

            case BoundMoveStatement move:
                ClearStatusIfReferenced(move.Source, pendingStatusCheck);
                break;

            case BoundCompoundStatement compound:
                WalkStatements(compound.Statements, line, openedFiles, pendingStatusCheck, diagnostics);
                break;
        }
    }

    private static void WalkStatements(
        IReadOnlyList<BoundStatement> statements, int line,
        HashSet<string> openedFiles,
        Dictionary<string, string> pendingStatusCheck,
        DiagnosticBag diagnostics)
    {
        foreach (var stmt in statements)
            ProcessStatement(stmt, line, openedFiles, pendingStatusCheck, diagnostics);
    }

    /// <summary>CBL0702: I/O on file that hasn't been OPENed.</summary>
    private static void CheckFileOpen(
        string fileName, int line,
        HashSet<string> openedFiles,
        DiagnosticBag diagnostics)
    {
        if (!openedFiles.Contains(fileName))
        {
            diagnostics.Report(DiagnosticDescriptors.CBL0702,
                SourceLocation.None, TextSpan.Empty, fileName);
        }
    }

    /// <summary>
    /// CBL3206: Check if a previous I/O operation's FILE STATUS was left unchecked,
    /// then record the new pending status check for this operation.
    /// </summary>
    private static void CheckAndRecordStatus(
        FileSymbol file,
        Dictionary<string, string> pendingStatusCheck,
        int line,
        DiagnosticBag diagnostics)
    {
        // If there's already a pending status check for this file, it means
        // a previous I/O operation's status was never checked
        if (pendingStatusCheck.TryGetValue(file.Name, out var pendingVar))
        {
            diagnostics.Report(DiagnosticDescriptors.CBL3206,
                SourceLocation.None, TextSpan.Empty);
        }

        // Record new pending check (if file has FILE STATUS declared)
        if (file.FileStatus != null)
            pendingStatusCheck[file.Name] = file.FileStatus;
        else
            pendingStatusCheck.Remove(file.Name);
    }

    /// <summary>
    /// If an expression references a FILE STATUS variable, clear the pending check
    /// (the programmer is checking the status).
    /// </summary>
    private static void ClearStatusIfReferenced(
        BoundExpression? expr,
        Dictionary<string, string> pendingStatusCheck)
    {
        if (expr == null || pendingStatusCheck.Count == 0) return;

        // Collect all identifier names referenced in the expression
        var referencedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectIdentifiers(expr, referencedNames);

        // Clear any pending status check whose variable name is referenced
        var toClear = new List<string>();
        foreach (var (fileName, statusVar) in pendingStatusCheck)
        {
            if (referencedNames.Contains(statusVar))
                toClear.Add(fileName);
        }
        foreach (var name in toClear)
            pendingStatusCheck.Remove(name);
    }

    private static void CollectIdentifiers(BoundExpression expr, HashSet<string> names)
    {
        switch (expr)
        {
            case BoundIdentifierExpression id:
                names.Add(id.Symbol.Name);
                break;
            case BoundBinaryExpression bin:
                CollectIdentifiers(bin.Left, names);
                CollectIdentifiers(bin.Right, names);
                break;
            case BoundConditionNameExpression cn:
                names.Add(cn.Condition.Name);
                if (cn.Condition.ParentDataItem != null)
                    names.Add(cn.Condition.ParentDataItem.Name);
                break;
        }
    }
}
