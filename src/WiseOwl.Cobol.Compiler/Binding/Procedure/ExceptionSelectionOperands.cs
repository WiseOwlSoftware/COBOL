// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure;

/// <summary>The one resolver of the exception-selection operand <c>exception-name-1 | exception-name-2 { FILE file-name-2 }…</c>,
/// the grammar rule <c>exceptionSelectionOperand</c> that a Format-3 USE statement (§2023:14.9.49.2) and a Format-3 PERFORM's
/// WHEN phrase (§2023:14.9.28.2) both reference. The two are one rule because §2023:14.9.28.4 GR17 makes a WHEN match the USE
/// statement's: "The rules for determining a match are specified in General rules 3a to 3g of the USE statement" (kb/Work
/// PB6139; PB2723 unified the bound operand, <see cref="BoundExceptionSelector"/>, and its rendering).
/// <para>What differs between the two statements is passed in, never decided here: the context the name funnel's diagnostics
/// name, the refusal of a FILE paired with a name that does not begin EC-I-O (the USE statement's §2023:14.9.49.3 SR13 and the
/// PERFORM statement's §2023:14.9.28.3 SR16, the same sentence under two numbers and two diagnostic codes), and how a written
/// file-name resolves (a USE statement also refuses a sort or merge file, §2023:14.9.49.3 SR2; a WHEN phrase prints no such
/// rule).</para></summary>
internal static class ExceptionSelectionOperands
{
    /// <summary>Resolve one written operand and hand each selector it yields to <paramref name="select"/>, in written order,
    /// as it is resolved (so a statement's own rule on a pair reports in source order with the file refusals): the bare
    /// exception-name as one selector, or one selector per FILE-paired file-name that resolves, each with the token a
    /// per-statement rule reports at (the operand for a bare name, the file-name for a pair). Nothing is added when the name does not resolve (the funnel
    /// <see cref="EcNameResolution.TryResolve"/> has reported it) or when FILE pairs a non-EC-I-O name
    /// (<paramref name="refuseFilePairing"/> reports it); a file-name <paramref name="resolveFile"/> answers null for is
    /// skipped, its refusal being that delegate's.</summary>
    public static void Resolve(BinderContext ctx, CobolParserCore.ExceptionSelectionOperandContext operand, string where,
        Action<CobolParserCore.ExceptionSelectionOperandContext, EcInfo> refuseFilePairing,
        Func<CobolParserCore.FileNameContext, FileModel?> resolveFile,
        Action<BoundExceptionSelector, IToken> select)
    {
        string raw = operand.cobolWord().GetText();
        // Resolution and the 0711/0878/1636 diagnostics live in the ONE funnel (kb/Work R05).
        if (!EcNameResolution.TryResolve(ctx.Edition, raw, where, out var info)) return;
        var files = operand.fileName();
        if (files.Length == 0)
        {
            select(BoundExceptionSelector.ForName(info, null), operand.Start);
            return;
        }
        if (!ExceptionCatalog.IsIoName(info.Name))
        {
            refuseFilePairing(operand, info);
            return;
        }
        foreach (var fn in files)
            if (resolveFile(fn) is { } file)
                select(BoundExceptionSelector.ForName(info, file), fn.Start);
    }
}
