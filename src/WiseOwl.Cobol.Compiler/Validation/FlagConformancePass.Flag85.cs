// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding;              // IntrinsicCatalog
using WiseOwl.Cobol.Binding.Model;        // DataItem / PicInfo / Usage / OdoModel
using WiseOwl.Cobol.Binding.Procedure;    // ConditionBinder.QualifiersOf
using WiseOwl.Cobol.Common;               // CobolLiteral
using WiseOwl.Cobol.Editions;             // CobolKeywords
using WiseOwl.Cobol.Frontend.Generated;   // CobolParserCore
using WiseOwl.Cobol.Frontend.Preprocessor;// FlagOption

namespace WiseOwl.Cobol.Validation;

/// <summary>
/// The <c>&gt;&gt;FLAG-85</c> (§2002:7.2.11) and <c>&gt;&gt;FLAG-NATIVE-ARITHMETIC</c> (§2002:7.2.12) detectors — the
/// same warning mechanism, the same <see cref="FlagState"/> fold and the same parse-tree walk as the FLAG-02 / FLAG-14
/// detectors of <c>FlagConformancePass.cs</c> (kb/Work PB5600; <c>docs/rearchitecture/DESIGN-flag-directives.md</c>).
/// One detector per GR4 sub-rule, each a PARSE-NODE test (or, where §2002:7.2.11.3 names a property of a data item, a
/// RESOLVED-item test through the unit's own name scope — never a spelling match, kb/Work PB3288), each guarded by the
/// option's fold at the construct's line so that a source with no FLAG-85 directive pays nothing.
/// </summary>
internal sealed partial class FlagConformancePass
{
    // A statement's references to a data item that could be zero-length flag the STATEMENT once (§2002:7.2.11.3 GR4 k:
    // "any statement that references a data item that could be a zero-length item shall be flagged"), however many such
    // references it holds; the walk's nested statements each get their own latch.
    private bool _zeroLengthFlaggedInStatement;

    public override object? VisitStatement(CobolParserCore.StatementContext ctx)
    {
        bool saved = _zeroLengthFlaggedInStatement;
        _zeroLengthFlaggedInStatement = false;
        try { return base.VisitChildren(ctx); }
        finally { _zeroLengthFlaggedInStatement = saved; }
    }

    // ── FLAG-85 g NUMVAL and e FUNCTION-ARGUMENT (§2002:7.2.11.3 GR4 g, e). ──
    private void FlagFunctionCall85(CobolParserCore.FunctionCallContext ctx)
    {
        string? name = ctx.functionName()?.GetText() ?? ctx.RANDOM()?.GetText();
        if (name is null) return;

        if (CobolKeywords.Same(name, "NUMVAL") || CobolKeywords.Same(name, "NUMVAL-C"))
            Flag(FlagOption.Flag85Numval, ctx.Start.Line, $"the reference to FUNCTION {CobolKeywords.Spelling(name)}");

        // e: RANDOM followed IMMEDIATELY by a parenthesized arithmetic expression, written as an argument of a function that
        // allows several arithmetic-expression arguments. The 1985 text read that parenthesized expression as the next
        // argument of the enclosing function; the 2002 text makes the parenthesis RANDOM's own argument list.
        if (CobolKeywords.Same(name, "RANDOM") && RandomHasParenthesizedArgument(ctx)
            && EnclosingFunction(ctx) is { } outer
            && AllowsMultipleArithmeticArguments(outer))
            Flag(FlagOption.Flag85FunctionArgument, ctx.Start.Line,
                "FUNCTION RANDOM followed by a parenthesized arithmetic expression inside a function with several arithmetic arguments");
    }

    private static bool RandomHasParenthesizedArgument(CobolParserCore.FunctionCallContext ctx) =>
        ctx.FNARG_LPAREN() is not null
            ? ctx.functionArgList()?.functionArgument().Length > 0
            : ctx.subscriptPart()?.subscriptList()?.subscriptItem().Length > 0;

    /// <summary>The function call whose argument list holds <paramref name="ctx"/> (the nearest enclosing call), or null.</summary>
    private static CobolParserCore.FunctionCallContext? EnclosingFunction(ParserRuleContext ctx)
    {
        for (RuleContext? c = ctx.Parent; c is not null; c = c.Parent)
            if (c is CobolParserCore.FunctionCallContext outer) return outer;
        return null;
    }

    /// <summary>§2002:7.2.11.3 GR4 e: "a function that allows multiple arithmetic expressions as arguments" — an
    /// intrinsic function whose catalog row admits more than one argument and types its first two as numeric/integer
    /// (§2023:15.3 kinds <c>n</c>, <c>i</c>, or the category-polymorphic <c>p</c> of the MAX/MIN family).</summary>
    private static bool AllowsMultipleArithmeticArguments(CobolParserCore.FunctionCallContext call)
    {
        string? name = call.functionName()?.GetText() ?? call.reservedIntrinsicArgFn()?.GetText();
        if (name is null || !IntrinsicCatalog.TryGet(name, out var sig) || sig.MaxArgs < 2) return false;
        static bool Arithmetic(char kind) => kind is 'n' or 'i' or 'p';
        return Arithmetic(sig.ArgKind(0)) && Arithmetic(sig.ArgKind(1));
    }

    // ── FLAG-85 b CORRESPONDING (§2002:7.2.11.3 GR4 b): an ADD, MOVE or SUBTRACT with the CORRESPONDING phrase, when
    //    "subscripting with other than a constant is specified on any of the operands". A constant is a numeric literal. ──
    private void FlagCorresponding(int line, params CobolParserCore.DataReferenceContext?[] operands)
    {
        if (!_flag.IsOnAt(line, FlagOption.Flag85Corresponding)) return;
        if (operands.Any(o => o is not null && HasNonConstantSubscript(o)))
            Flag(FlagOption.Flag85Corresponding, line, "the CORRESPONDING statement with a non-constant subscript on an operand");
    }

    private static bool HasNonConstantSubscript(CobolParserCore.DataReferenceContext dref)
    {
        foreach (var part in Descendants<CobolParserCore.SubscriptPartContext>(dref))
            foreach (var item in part.subscriptList()?.subscriptItem() ?? [])
                if (!IsIntegerLiteralText(item.GetText())) return true;   // ALL, an identifier, an index-name, an expression
        return false;
    }

    private static bool IsIntegerLiteralText(string text)
    {
        int i = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (i == text.Length) return false;
        for (; i < text.Length; i++)
            if (!char.IsAsciiDigit(text[i])) return false;
        return true;
    }

    public override object? VisitAddStatement(CobolParserCore.AddStatementContext ctx)
    {
        if (ctx.CORRESPONDING() is not null || ctx.CORR() is not null)
            FlagCorresponding(ctx.Start.Line, ctx.dataReference(), ctx.receivingIdentifier()?.dataReference());
        FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitSubtractStatement(CobolParserCore.SubtractStatementContext ctx)
    {
        if (ctx.CORRESPONDING() is not null || ctx.CORR() is not null)
            FlagCorresponding(ctx.Start.Line, ctx.dataReference(), ctx.receivingIdentifier()?.dataReference());
        FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitMultiplyStatement(CobolParserCore.MultiplyStatementContext ctx)
    {
        FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    public override object? VisitComputeStatement(CobolParserCore.ComputeStatementContext ctx)
    {
        FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    // ── FLAG-85 d DIVIDE (§2002:7.2.11.3 GR4 d): a DIVIDE with the REMAINDER phrase, "if the quotient data item is not
    //    described with a sign … and either the divisor or dividend is described with a sign". The quotient is the GIVING
    //    item (REMAINDER is a GIVING-format phrase); the dividend and the divisor are the statement's other two operands,
    //    whichever of INTO / BY names them. ──
    public override object? VisitDivideStatement(CobolParserCore.DivideStatementContext ctx)
    {
        int line = ctx.Start.Line;
        if (ctx.divideRemainderPhrase() is not null && _flag.IsOnAt(line, FlagOption.Flag85Divide)
            && ctx.arithmeticGivingPhrase()?.receivingArithmeticOperand() is [{ } quotient, ..]
            && quotient.receivingIdentifier()?.dataReference() is { } quotientRef
            && ResolveItem(quotientRef)?.OperandPic is { } quotientPic && !quotientPic.HoldsSign)
        {
            var operands = new List<CobolParserCore.DataReferenceContext?> { ctx.sendingOperand()?.dataReference() };
            var phrase = ctx.divideIntoPhrase()?.arithmeticPhraseOperand() ?? ctx.divideByPhrase()?.arithmeticPhraseOperand() ?? [];
            operands.AddRange(phrase.Select(p => p.receivingIdentifier()?.dataReference()));
            if (operands.Any(o => o is not null && ResolveItem(o)?.OperandPic is { } pic && pic.HoldsSign))
                Flag(FlagOption.Flag85Divide, line, "the DIVIDE … REMAINDER with an unsigned quotient and a signed divisor or dividend");
        }
        FlagNativeArithmetic(ctx, line);
        return base.VisitChildren(ctx);
    }

    // ── FLAG-85 c DE-EDITING and f MOVE (§2002:7.2.11.3 GR4 c, f), and b CORRESPONDING for MOVE CORRESPONDING. ──
    private void FlagMove85(CobolParserCore.MoveStatementContext ctx)
    {
        int line = ctx.Start.Line;
        if (ctx.CORRESPONDING() is not null || ctx.CORR() is not null)
        {
            FlagCorresponding(line, ctx.dataReference(), ctx.receivingIdentifier()?.dataReference());
            return;
        }
        if (ctx.sendingOperand() is not { } send) return;
        var receivers = ctx.moveReceivingPhrase()?.receivingIdentifier()
            .Select(r => r.dataReference()).OfType<CobolParserCore.DataReferenceContext>().Select(ResolveItem)
            .OfType<DataItem>().Select(i => i.OperandPic).OfType<PicInfo>().ToList() ?? [];

        // c: §2002:14.8.24.3 GR3 — "De-editing takes place only when the sending operand is a numeric-edited data item and the
        // receiving item is a numeric or a numeric-edited data item."
        if (_flag.IsOnAt(line, FlagOption.Flag85DeEditing)
            && send.dataReference() is { } sendRef && ResolveItem(sendRef)?.OperandPic is { Category: PicCategory.NumericEdited }
            && receivers.Any(p => p.Category is PicCategory.Numeric or PicCategory.NumericEdited))
            Flag(FlagOption.Flag85DeEditing, line, "the de-editing MOVE");

        // f: an alphanumeric literal or data item of more than 31 characters moved to a numeric data item.
        if (_flag.IsOnAt(line, FlagOption.Flag85Move) && receivers.Any(p => p.Category == PicCategory.Numeric)
            && SendingCharacterCount(send) > 31)
            Flag(FlagOption.Flag85Move, line, "the MOVE of an alphanumeric operand longer than 31 characters to a numeric item");
    }

    /// <summary>The character count of an ALPHANUMERIC sending operand — an alphanumeric (plain or hexadecimal) literal or
    /// an alphanumeric / group data item — else 0 (numeric, national and every other operand are not §2002:7.2.11.3 GR4 f's).</summary>
    private int SendingCharacterCount(CobolParserCore.SendingOperandContext send)
    {
        if (send.literal()?.nonNumericLiteral() is { } literal)
            return (literal.STRINGLIT() ?? literal.HEXLIT())?.GetText() is { } raw
                && CobolLiteral.ClassOf(raw) == LiteralClass.Alphanumeric ? CobolLiteral.Decode(raw).Length : 0;
        if (send.dataReference() is { } dref && ResolveItem(dref) is { } item
            && (item.IsPlainGroup || item.OperandPic is { Category: PicCategory.Alphanumeric }))
            return item.ImageWidth;
        return 0;
    }

    // ── FLAG-85 h SET (§2002:7.2.11.3 GR4 h): "If a condition-setting format SET statement references a variable length
    //    group item". The condition-name's conditional variable is the item the statement references; a variable-length
    //    group is a group holding an OCCURS … DEPENDING ON table. ──
    public override object? VisitSetBooleanStatement(CobolParserCore.SetBooleanStatementContext ctx)
    {
        int line = ctx.Start.Line;
        if (_flag.IsOnAt(line, FlagOption.Flag85Set) && _currentData is { } data
            && ctx.setConditionPhrase().SelectMany(p => p.dataReference()).Any(dref =>
                dref.cobolWord()?.GetText() is { } name
                && data.QualifiedConditionCandidates(name, ConditionBinder.QualifiersOf(dref), data.ActiveScope) is [{ } condition]
                && IsVariableLengthGroup(condition.Parent)))
            Flag(FlagOption.Flag85Set, line, "the condition-setting SET of a variable-length group item");
        return base.VisitChildren(ctx);
    }

    private static bool IsVariableLengthGroup(DataItem item) => item.IsGroup && OdoModel.TableUnder(item) is not null;

    // ── FLAG-85 i STANDARD-1 and j STANDARD-2 (§2002:7.2.11.3 GR4 i, j): the ALPHABET clause that specifies it. ──
    public override object? VisitAlphabetClause(CobolParserCore.AlphabetClauseContext ctx)
    {
        if (ctx.alphabetDefinition() is { } definition)
        {
            if (definition.STANDARD_1() is not null)
                Flag(FlagOption.Flag85Standard1, ctx.Start.Line, "the ALPHABET clause that specifies STANDARD-1");
            else if (definition.STANDARD_2() is not null)
                Flag(FlagOption.Flag85Standard2, ctx.Start.Line, "the ALPHABET clause that specifies STANDARD-2");
        }
        return base.VisitChildren(ctx);
    }

    // ── FLAG-85 k ZERO-LENGTH (§2002:7.2.11.3 GR4 k): "A READ statement that could return a zero-length item or any statement
    //    that references a data item that could be a zero-length item". "Could be" is the DESCRIPTION's answer — a minimum
    //    length of zero (DataItem.MinimumLengthIsZero, the first half of §2023:8.5.4) — and a file whose smallest record
    //    could have no character positions (FileModel.VaryMin). ──
    private void FlagZeroLengthReference(CobolParserCore.DataReferenceContext ctx)
    {
        int line = ctx.Start.Line;
        if (_zeroLengthFlaggedInStatement || !_flag.IsOnAt(line, FlagOption.Flag85ZeroLength)) return;
        if (ResolveItem(ctx) is { MinimumLengthIsZero: true })
        {
            _zeroLengthFlaggedInStatement = true;
            Flag(FlagOption.Flag85ZeroLength, line, "the statement that references a data item that could be zero-length");
        }
    }

    private void FlagReadZeroLength(FileModel? file, int line)
    {
        if (file is { VaryMin: 0 } && !_zeroLengthFlaggedInStatement && _flag.IsOnAt(line, FlagOption.Flag85ZeroLength))
        {
            _zeroLengthFlaggedInStatement = true;
            Flag(FlagOption.Flag85ZeroLength, line, "the READ of a file whose record could be zero-length");
        }
    }

    // ── FLAG-NATIVE-ARITHMETIC (§2002:7.2.12.3 GR1–GR4). "If standard arithmetic has been specified or implied for the
    //    source unit and ON is explicitly or implicitly specified, flagging is enabled for any operation that uses or has the
    //    potential to use native arithmetic" (GR2); "If native arithmetic is in effect, no flagging … occurs" (GR4). Under
    //    standard arithmetic an operation uses native arithmetic when an operand is described with usage binary-char,
    //    binary-short, binary-long, binary-double, float-short, float-long or float-extended (§2002:8.8.1.3). The
    //    operations are the arithmetic statements (§2002:14.6.6), an arithmetic expression anywhere else, and the report
    //    SUM clause (§2002:8.8.1.1 lists the same three). ──
    private void FlagNativeArithmetic(ParserRuleContext operation, int line)
    {
        if (!_flag.IsOnAt(line, FlagOption.FlagNativeArithmeticOperation) || !StandardArithmeticInEffect()) return;
        if (OperandsOf(operation).Any(UsesNativeArithmetic))
            Flag(FlagOption.FlagNativeArithmeticOperation, line, "the arithmetic operation with an operand that uses native arithmetic");
    }

    /// <summary>Whether the unit — or the method — has standard arithmetic specified: its effective OPTIONS paragraph names
    /// ARITHMETIC IS STANDARD; absent or NATIVE is native arithmetic in effect (§2002:8.8.1.2), and the 2014 modes
    /// STANDARD-BINARY / STANDARD-DECIMAL use no native arithmetic (§2014:7.3.13.3 GR2).</summary>
    private bool StandardArithmeticInEffect() =>
        _currentData is { } data
        && data.Procedure.Options.Over(data.State.Configuration.Options).Arithmetic == ArithmeticMode.Standard;

    private static bool IsNativeUsage(Usage usage) =>
        UsageFamilies.IsFixedWidthBinary(usage)
        || usage is Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
            or Usage.Float or Usage.Double;   // COMP-1 / COMP-2: this implementation's float-short / float-long (§2023:13.16.3 comment)

    private bool UsesNativeArithmetic(CobolParserCore.DataReferenceContext dref) =>
        ResolveItem(dref)?.OperandPic is { } pic && IsNativeUsage(pic.Usage);

    /// <summary>The data references an operation's own operands are — every reference under it, except the ones inside a
    /// conditional phrase (ON SIZE ERROR …), which are the statements nested there and are flagged on their own.</summary>
    private static IEnumerable<CobolParserCore.DataReferenceContext> OperandsOf(IParseTree operation)
    {
        for (int i = 0; i < operation.ChildCount; i++)
        {
            var child = operation.GetChild(i);
            if (child is CobolParserCore.ConditionalPhrasesContext) continue;
            if (child is CobolParserCore.DataReferenceContext dref) yield return dref;
            foreach (var nested in OperandsOf(child)) yield return nested;
        }
    }

    /// <summary>An arithmetic expression that is not part of one of the flagged arithmetic statements (a condition's, a
    /// subscript's, a VALUE's …) and carries an operator is an arithmetic operation of its own; only the OUTERMOST is
    /// asked, its operands including every nested one.</summary>
    public override object? VisitArithmeticExpression(CobolParserCore.ArithmeticExpressionContext ctx)
    {
        if (_flag.IsOnAt(ctx.Start.Line, FlagOption.FlagNativeArithmeticOperation)
            && !IsCoveredArithmetic(ctx) && HasOperator(ctx))
            FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    /// <summary>True when the expression sits inside an enclosing arithmetic expression, or is an operand of one of the
    /// arithmetic statements the pass asks as a whole — either way its operation is asked by the outer node.</summary>
    private static bool IsCoveredArithmetic(CobolParserCore.ArithmeticExpressionContext ctx)
    {
        for (RuleContext? c = ctx.Parent; c is not null; c = c.Parent)
            switch (c)
            {
                case CobolParserCore.ArithmeticExpressionContext:
                case CobolParserCore.AddStatementContext:
                case CobolParserCore.SubtractStatementContext:
                case CobolParserCore.MultiplyStatementContext:
                case CobolParserCore.DivideStatementContext:
                case CobolParserCore.ComputeStatementContext:
                    return true;
                case CobolParserCore.ConditionalPhrasesContext:
                case CobolParserCore.StatementContext:
                    return false;
            }
        return false;
    }

    private static bool HasOperator(IParseTree expression)
    {
        for (int i = 0; i < expression.ChildCount; i++)
        {
            var child = expression.GetChild(i);
            if (child is CobolParserCore.AddOpContext or CobolParserCore.MulOpContext
                || child is ITerminalNode { Symbol.Type: CobolParserCore.POWER } || HasOperator(child)) return true;
        }
        return false;
    }

    public override object? VisitReportSumClause(CobolParserCore.ReportSumClauseContext ctx)
    {
        FlagNativeArithmetic(ctx, ctx.Start.Line);
        return base.VisitChildren(ctx);
    }

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : IParseTree
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
