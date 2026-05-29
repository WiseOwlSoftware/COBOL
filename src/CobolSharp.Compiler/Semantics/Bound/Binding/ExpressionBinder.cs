// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics.Bound.Binding;

/// <summary>
/// Expression binding: arithmetic expressions, literals, figurative constants,
/// function calls, data references with subscripts/ref-mod, qualified names.
/// </summary>
internal sealed class ExpressionBinder
{
    private readonly BindingContext _ctx;

    internal ExpressionBinder(BindingContext ctx) => _ctx = ctx;

    // ── Arithmetic expression tree walk ──

    /// <summary>
    /// Recursively bind an arithmetic expression tree for COMPUTE.
    /// Walks the parse tree: additiveExpression → multiplicativeExpression →
    /// powerExpression → unaryExpression → primaryExpression.
    /// </summary>
    internal BoundExpression BindAdditiveExpression(CobolParserCore.AdditiveExpressionContext ctx)
    {
        var terms = ctx.multiplicativeExpression();
        var ops = ctx.addOp();

        var left = BindMultiplicativeExpression(terms[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindMultiplicativeExpression(terms[i + 1]);
            var opKind = ops[i].GetText() == "+"
                ? BoundBinaryOperatorKind.Add
                : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindMultiplicativeExpression(CobolParserCore.MultiplicativeExpressionContext ctx)
    {
        var factors = ctx.powerExpression();
        var ops = ctx.mulOp();

        var left = BindPowerExpression(factors[0]);
        for (int i = 0; i < ops.Length; i++)
        {
            var right = BindPowerExpression(factors[i + 1]);
            var opKind = ops[i].GetText() == "*"
                ? BoundBinaryOperatorKind.Multiply
                : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, opKind, right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindPowerExpression(CobolParserCore.PowerExpressionContext ctx)
    {
        var unaries = ctx.unaryExpression();
        var left = BindUnaryExpression(unaries[0]);
        if (unaries.Length > 1)
        {
            // a ** b
            var right = BindUnaryExpression(unaries[1]);
            // Power is not a standard BoundBinaryOperatorKind; use Multiply as placeholder
            // and handle at emit time. For now, use a dedicated representation.
            // Simple approach: emit as Math.Pow at runtime
            left = new BoundBinaryExpression(left,
                BoundBinaryOperatorKind.Power,
                right, CobolCategory.Numeric);
        }
        return left;
    }

    internal BoundExpression BindUnaryExpression(CobolParserCore.UnaryExpressionContext ctx)
    {
        var addOp = ctx.addOp();
        if (addOp != null)
        {
            var inner = BindUnaryExpression(ctx.unaryExpression());
            if (addOp.GetText() == "-")
            {
                // Negate: 0 - inner
                return new BoundBinaryExpression(
                    new BoundLiteralExpression(0m, CobolCategory.Numeric),
                    BoundBinaryOperatorKind.Subtract,
                    inner, CobolCategory.Numeric);
            }
            return inner; // unary + is identity
        }
        return BindPrimaryExpression(ctx.primaryExpression());
    }

    internal BoundExpression BindPrimaryExpression(CobolParserCore.PrimaryExpressionContext ctx)
    {
        if (ctx.numericLiteral() != null)
            return BindNumericLiteral(ctx.numericLiteral());

        // ZERO_ARITH: figurative ZERO rewritten by token rewriter in arithmetic context
        if (ctx.ZERO_ARITH() != null)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        if (ctx.dataReference() != null)
        {
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        }

        if (ctx.arithmeticExpression() != null)
            return BindAdditiveExpression(ctx.arithmeticExpression().additiveExpression());

        // Intrinsic function call (1989 Amendment)
        if (ctx.functionCall() != null)
        {
            return BindFunctionCall(ctx.functionCall());
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── FUNCTION CALL ──

    internal BoundExpression BindFunctionCall(CobolParserCore.FunctionCallContext ctx)
    {
        // FUNCTION functionName subscriptPart? — the function name comes from the
        // functionName rule (IDENTIFIER or a reserved-word alternative like SIGN/SUM/RANDOM).
        // Arguments (if any) are captured as SUBSCRIPT-mode tokens; InterpretSubscriptTokens
        // splits them on the COBOL comma/space separators and binds each as an arithmetic
        // expression (ISO §15).
        var funcName = ctx.functionName()?.GetText() ?? "UNKNOWN";

        var args = new List<BoundExpression>();
        var subPart = ctx.subscriptPart();
        if (subPart != null)
        {
            var subOrRefMod = subPart.subscriptOrRefMod();
            if (subOrRefMod != null)
            {
                var (subExprs, _) = InterpretSubscriptTokens(subOrRefMod);
                args.AddRange(subExprs);
            }
        }

        // Expand the ALL subscript (ISO §15.4): FUNCTION f(table(ALL)) passes every occurrence
        // of the table as a separate argument. Replace each table(ALL) reference in-place with
        // one element reference per occurrence (table(1), table(2), …, table(n)).
        if (args.Any(IsAllSubscriptedRef))
        {
            var expanded = new List<BoundExpression>(args.Count);
            foreach (var a in args)
                expanded.AddRange(ExpandAllSubscript(a));
            args = expanded;
        }

        // FUNCTION LENGTH returns the defined size of the operand, not its content length.
        // Per ISO §15.24: "the value returned is the number of character positions
        // in argument-1". Resolved at bind time — no runtime call needed.
        if (funcName.Equals("LENGTH", StringComparison.OrdinalIgnoreCase) && args.Count == 1)
            return new BoundLiteralExpression(StaticLength(args[0]), CobolCategory.Numeric);

        var category = BindingContext.AlphanumericFunctions.Contains(funcName)
            ? CobolCategory.Alphanumeric
            : CobolCategory.Numeric;

        // MAX and MIN are category-polymorphic (ISO §15.x): their result category follows the
        // arguments. With all-alphanumeric arguments they return the selected string, so the
        // result must be treated as alphanumeric (otherwise the string result is unboxed to
        // decimal at the call site). ORD-MAX/ORD-MIN always return a numeric ordinal.
        if ((funcName.Equals("MAX", StringComparison.OrdinalIgnoreCase)
                || funcName.Equals("MIN", StringComparison.OrdinalIgnoreCase))
            && args.Count > 0 && args.All(a => !a.Category.IsNumericLike()))
        {
            category = CobolCategory.Alphanumeric;
        }

        return new BoundFunctionCallExpression(funcName, args.AsReadOnly(), category);
    }

    /// <summary>
    /// Compute FUNCTION LENGTH at bind time (ISO §15.24: number of character positions in
    /// argument-1). The length-preserving string functions (REVERSE/UPPER-CASE/LOWER-CASE)
    /// report the length of their own single argument, so LENGTH(REVERSE(x)) == LENGTH(x) and
    /// nested forms recurse. A numeric literal that survived from a folded nested function is
    /// already its own value.
    /// </summary>
    private static decimal StaticLength(BoundExpression arg) => arg switch
    {
        BoundIdentifierExpression idExpr => idExpr.Symbol.ElementSize,
        BoundLiteralExpression { Value: string s } => s.Length,
        BoundLiteralExpression { Value: decimal d } => d,
        BoundFunctionCallExpression fn when fn.Arguments.Count == 1
            && (fn.FunctionName.Equals("REVERSE", StringComparison.OrdinalIgnoreCase)
                || fn.FunctionName.Equals("UPPER-CASE", StringComparison.OrdinalIgnoreCase)
                || fn.FunctionName.Equals("LOWER-CASE", StringComparison.OrdinalIgnoreCase))
            => StaticLength(fn.Arguments[0]),
        _ => 0
    };

    /// <summary>True if the expression is a table reference whose subscript is the ALL keyword.</summary>
    private static bool IsAllSubscriptedRef(BoundExpression e) =>
        e is BoundIdentifierExpression { Subscripts: { } subs }
        && subs.Any(s => s is BoundLiteralExpression { Value: string sv }
            && string.Equals(sv, "ALL", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Expand a table(ALL) reference (ISO §15.4) into one reference per occurrence. ALL may appear
    /// in any/all subscript positions of a multi-dimensional table, so the expansion is the
    /// cartesian product over every ALL position: each position's range comes from the OCCURS
    /// bound of the table level corresponding to that subscript position (outermost subscript ↔
    /// outermost OCCURS). Fixed subscripts are kept; the references are produced in row-major
    /// (leftmost-varies-slowest) order.
    /// </summary>
    private static IEnumerable<BoundExpression> ExpandAllSubscript(BoundExpression e)
    {
        if (e is not BoundIdentifierExpression id || id.Subscripts is null)
            return new[] { e };

        // OCCURS bounds for this item's nesting, outermost-first to align with subscript order.
        var bounds = new List<int>();
        for (var sym = id.Symbol; sym != null; sym = sym.Parent)
            if (sym.Occurs != null) bounds.Add(sym.Occurs.MaxOccurs);
        bounds.Reverse();

        // Start with the original subscripts; expand each ALL position by its OCCURS bound.
        var combos = new List<List<BoundExpression>> { new(id.Subscripts) };
        for (int pos = 0; pos < id.Subscripts.Count; pos++)
        {
            if (id.Subscripts[pos] is not BoundLiteralExpression { Value: string sv }
                || !string.Equals(sv, "ALL", StringComparison.OrdinalIgnoreCase))
                continue;

            int count = pos < bounds.Count ? bounds[pos] : id.Symbol.Occurs?.MaxOccurs ?? 0;
            if (count <= 0) return new[] { e };

            var expanded = new List<List<BoundExpression>>(combos.Count * count);
            foreach (var combo in combos)
                for (int idx = 1; idx <= count; idx++)
                {
                    var next = new List<BoundExpression>(combo)
                    {
                        [pos] = new BoundLiteralExpression((decimal)idx, CobolCategory.Numeric)
                    };
                    expanded.Add(next);
                }
            combos = expanded;
        }

        return combos.Select(subs =>
            (BoundExpression)new BoundIdentifierExpression(id.Symbol, id.Category, subs));
    }

    // ── LITERALS ──

    internal BoundExpression BindLiteral(CobolParserCore.LiteralContext lit)
    {
        // literal: numericLiteral | nonNumericLiteral
        var numLit = lit.numericLiteral();
        if (numLit != null)
            return BindNumericLiteral(numLit);

        var nonNumLit = lit.nonNumericLiteral();
        if (nonNumLit != null)
            return BindNonNumericLiteral(nonNumLit);

        // Fallback
        return new BoundLiteralExpression(lit.GetText(), CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindNumericLiteral(CobolParserCore.NumericLiteralContext numLit)
    {
        var normalized = SemanticBuilder.NormalizeNumericLiteralText(numLit);
        var originalText = numLit.GetText();
        if (decimal.TryParse(normalized, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: originalText);
        return new BoundLiteralExpression(originalText, CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindNonNumericLiteral(CobolParserCore.NonNumericLiteralContext nonNum)
    {
        var s = nonNum.STRINGLIT();
        if (s != null)
        {
            var text = s.GetText();
            if (text.Length >= 2 &&
                ((text[0] == '"' && text[^1] == '"') ||
                 (text[0] == '\'' && text[^1] == '\'')))
            {
                char quoteChar = text[0];
                text = text[1..^1];
                // Un-escape doubled quotes: "" → " (ISO §8.3.1.2)
                text = text.Replace(new string(quoteChar, 2), new string(quoteChar, 1));
            }
            return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
        }

        var figCtx = nonNum.figurativeConstant();
        if (figCtx != null)
            return BindFigurativeConstantExpression(figCtx);

        // HEXLIT, etc.
        return new BoundLiteralExpression(nonNum.GetText(), CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindFigurativeConstantExpression(CobolParserCore.FigurativeConstantContext figCtx)
    {
        if (figCtx.ALL() != null)
        {
            // ALL STRINGLIT / ALL HEXLIT: repeating literal pattern
            var allStr = figCtx.STRINGLIT();
            if (allStr != null)
            {
                var raw = allStr.GetText();
                string allText = raw.Length >= 2 ? raw[1..^1] : "";
                return new BoundFigurativeExpression(FigurativeKind.None, allText);
            }
            var allHex = figCtx.HEXLIT();
            if (allHex != null)
            {
                var raw = allHex.GetText();
                if (raw.Length >= 3)
                {
                    var hexBody = raw[2..^1];
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i + 1 < hexBody.Length; i += 2)
                        sb.Append((char)Convert.ToByte(hexBody[i..(i + 2)], 16));
                    return new BoundFigurativeExpression(FigurativeKind.None, sb.ToString());
                }
            }

            // ALL ZERO / ALL SPACE / ALL HIGH-VALUE / ALL LOW-VALUE / ALL QUOTE
            // Per COBOL-85 §4.3.3, ALL applied to a figurative constant is
            // semantically identical to the figurative constant alone.
            if (figCtx.ZERO() != null) return new BoundFigurativeExpression(FigurativeKind.Zero);
            if (figCtx.SPACE() != null) return new BoundFigurativeExpression(FigurativeKind.Space);
            if (figCtx.HIGH_VALUE() != null) return new BoundFigurativeExpression(FigurativeKind.HighValue);
            if (figCtx.LOW_VALUE() != null) return new BoundFigurativeExpression(FigurativeKind.LowValue);
            if (figCtx.QUOTE_() != null) return new BoundFigurativeExpression(FigurativeKind.Quote);

            // Fallback: should not reach here with valid grammar
            return new BoundFigurativeExpression(FigurativeKind.None, "");
        }

        string figText = figCtx.GetText().ToUpperInvariant();
        return figText switch
        {
            "SPACE" or "SPACES" => new BoundFigurativeExpression(FigurativeKind.Space),
            "ZERO" or "ZEROS" or "ZEROES" => new BoundFigurativeExpression(FigurativeKind.Zero),
            "HIGH-VALUE" or "HIGH-VALUES" => new BoundFigurativeExpression(FigurativeKind.HighValue),
            "LOW-VALUE" or "LOW-VALUES" => new BoundFigurativeExpression(FigurativeKind.LowValue),
            "QUOTE" or "QUOTES" => new BoundFigurativeExpression(FigurativeKind.Quote),
            _ => new BoundLiteralExpression(figText, CobolCategory.Alphanumeric)
        };
    }

    // ── OPERAND BINDING ──

    /// <summary>
    /// Bind a givingReceiver (identifier | literal) — unified GIVING-form operand.
    /// </summary>
    internal BoundExpression BindReceivingOperand(CobolParserCore.ReceivingOperandContext ctx)
    {
        if (ctx.dataReference() != null)
            return BindDataReferenceWithSubscripts(ctx.dataReference());
        if (ctx.literal() != null)
            return BindLiteral(ctx.literal());
        throw new InvalidOperationException("givingReceiver has neither identifier nor literal");
    }

    /// <summary>
    /// Bind a simple operand (identifier or literal) from ADD/SUBTRACT/MULTIPLY/DIVIDE.
    /// These statements accept only simple operands, not full expressions.
    /// </summary>
    internal BoundExpression BindSimpleOperand(ParserRuleContext ctx)
    {
        // The rule is: identifier | literal
        // Check for identifier child first
        if (ctx is CobolParserCore.AddOperandContext addOp)
        {
            if (addOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(addOp.dataReference());
            if (addOp.literal() != null)
                return BindLiteral(addOp.literal());
        }
        else if (ctx is CobolParserCore.SubtractOperandContext subOp)
        {
            if (subOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(subOp.dataReference());
            if (subOp.literal() != null)
                return BindLiteral(subOp.literal());
        }
        else if (ctx is CobolParserCore.MultiplyOperandContext mulOp)
        {
            if (mulOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(mulOp.dataReference());
            if (mulOp.literal() != null)
                return BindLiteral(mulOp.literal());
        }
        else if (ctx is CobolParserCore.DivideOperandContext divOp)
        {
            if (divOp.dataReference() != null)
                return BindDataReferenceWithSubscripts(divOp.dataReference());
            if (divOp.literal() != null)
                return BindLiteral(divOp.literal());
        }

        // Fallback: try to parse the text
        string text = ctx.GetText();
        return BindDataReferenceOrLiteral(text);
    }

    internal BoundExpression BindDataReferenceOrLiteral(string text)
    {
        if (decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return new BoundLiteralExpression(val, CobolCategory.Numeric, originalText: text);

        var sym = _ctx.Semantic.ResolveData(text);
        if (sym != null)
            return new BoundIdentifierExpression(sym, CobolCategory.Alphanumeric);

        _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0110,
            SourceLocation.None, TextSpan.Empty,
            $"Unresolved identifier '{text}'");
        return new BoundLiteralExpression(text, CobolCategory.Alphanumeric);
    }

    internal BoundExpression BindArithmeticExpr(CobolParserCore.ArithmeticExpressionContext? ctx)
        => ctx != null ? BindAdditiveExpression(ctx.additiveExpression()) : new BoundLiteralExpression(0m, CobolCategory.Numeric);

    // ── DATA REFERENCE WITH SUBSCRIPTS ──

    /// Bind a data reference: IDENTIFIER with optional qualification (OF/IN),
    /// subscripts, and reference modification.
    /// Qualified names are resolved right-to-left: A OF B OF C → resolve C, then B in C, then A in B.
    /// </summary>
    internal BoundExpression BindDataReferenceWithSubscripts(CobolParserCore.DataReferenceContext idCtx)
    {
        string name = idCtx.cobolWord().GetText();
        var tails = idCtx.dataReferenceSuffix();

        // Extract qualifications, subscripts, and refmod from dataNameTail*
        var qualifiers = new List<string>();
        CobolParserCore.SubscriptOrRefModContext? subOrRefMod = null;
        CobolParserCore.RefModSpecContext? refModCtx = null;

        foreach (var tail in tails)
        {
            if (tail.qualification() != null)
            {
                var qual = tail.qualification();
                qualifiers.Add(qual.cobolWord().GetText());
                // Extract subscripts/refmods attached to the qualifier (e.g., AX-2 IN AX(I))
                var qualSubs = qual.subscriptPart();
                if (qualSubs.Length > 0 && subOrRefMod == null)
                    subOrRefMod = qualSubs[0].subscriptOrRefMod();
                var qualRefMods = qual.refModPart();
                if (qualRefMods.Length > 0 && refModCtx == null)
                    refModCtx = qualRefMods[0].refModSpec();
            }
            else if (tail.subscriptPart() != null && subOrRefMod == null)
            {
                subOrRefMod = tail.subscriptPart().subscriptOrRefMod();
            }
            else if (tail.refModPart() != null && refModCtx == null)
            {
                refModCtx = tail.refModPart().refModSpec();
            }
        }

        // Resolve the data symbol — qualified or unqualified
        DataSymbol? sym;
        if (qualifiers.Count > 0)
        {
            // Right-to-left narrowing: resolve outermost qualifier first,
            // then walk inward to the leftmost identifier.
            sym = ResolveQualifiedName(name, qualifiers);
        }
        else
        {
            sym = _ctx.Semantic.ResolveData(name);
        }

        if (sym == null)
        {
            // Check for SYMBOLIC CHARACTER from SPECIAL-NAMES
            var symChar = _ctx.Semantic.ResolveSymbolicCharacter(name);
            if (symChar.HasValue)
            {
                // Symbolic character: produce a 1-byte string literal
                string charValue = ((char)symChar.Value).ToString();
                return _ctx.Typed(new BoundLiteralExpression(charValue, CobolCategory.Alphanumeric));
            }

            // Check if this is a condition name (level 88) — possibly qualified and/or subscripted
            var condSym = qualifiers.Count > 0
                ? _ctx.Semantic.ResolveQualifiedConditionName(name, qualifiers)
                : _ctx.Semantic.ResolveConditionName(name);
            if (condSym != null)
            {
                BoundExpression? parentExpr = null;
                if (condSym.ParentDataItem != null && subOrRefMod != null)
                {
                    var (condSubExprs, condIsRefMod) = InterpretSubscriptTokens(subOrRefMod);
                    if (!condIsRefMod && condSubExprs.Count > 0)
                    {
                        var parentCat = condSym.ParentDataItem.ResolvedType?.Category
                                        ?? CobolCategory.Alphanumeric;
                        parentExpr = new BoundIdentifierExpression(
                            condSym.ParentDataItem, parentCat, condSubExprs);
                    }
                }
                return new BoundConditionNameExpression(condSym, parentExpression: parentExpr);
            }

            return new BoundLiteralExpression(name, CobolCategory.Alphanumeric);
        }

        var cat = sym.ResolvedType?.Category ?? CobolCategory.Alphanumeric;

        if (subOrRefMod == null)
        {
            var plainId = new BoundIdentifierExpression(sym, cat);
            _ctx.Typed(plainId);
            if (refModCtx != null)
                return _ctx.Typed(BindReferenceModification(plainId, refModCtx));
            return plainId;
        }

        // Interpret the flat SUBSCRIPT-mode token sequence
        var (subExprs, isRefMod) = InterpretSubscriptTokens(subOrRefMod);

        if (isRefMod)
        {
            var startExpr = subExprs.Count > 0 ? subExprs[0] : new BoundLiteralExpression(1m, CobolCategory.Numeric);
            BoundExpression? lengthExpr = subExprs.Count > 1 ? subExprs[1] : null;
            var refModBase = new BoundIdentifierExpression(sym, cat);
            _ctx.Typed(refModBase);
            return _ctx.Typed(new BoundReferenceModificationExpression(refModBase, startExpr, lengthExpr));
        }

        var subs = subExprs;

        // ── Subscript validation (COBOL-85 semantic rules) ──

        int occursDepth = 0;
        var current = sym;
        while (current != null)
        {
            if (current.Occurs != null)
                occursDepth++;
            current = current.Parent;
        }

        int subscriptCount = subs.Count;
        int line = idCtx.Start?.Line ?? 0;
        var loc = new SourceLocation("<source>", 0, line, 0);
        var span = TextSpan.Empty;

        if (subscriptCount > 0 && occursDepth == 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0405, loc, span, sym.Name);

        if (subscriptCount > occursDepth && occursDepth > 0)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0406, loc, span, sym.Name, occursDepth, subscriptCount);

        // COBOL-85 standard specifies 3 OCCURS levels; we support up to 7 (NIST suite exercises 7).
        // Emit a warning (not error) beyond 3 levels to note departure from strict COBOL-85.
        if (occursDepth > 7)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0407, loc, span, sym.Name, occursDepth);

        if (subscriptCount > 7)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0408, loc, span, subscriptCount);

        if (sym.IsElementary && occursDepth > 0 && subscriptCount > 0 && subscriptCount < occursDepth)
            _ctx.Diagnostics.Report(DiagnosticDescriptors.COBOL0409, loc, span, sym.Name, occursDepth, subscriptCount);

        var baseId = new BoundIdentifierExpression(sym, cat, subs);
        _ctx.Typed(baseId);

        if (refModCtx != null)
            return _ctx.Typed(BindReferenceModification(baseId, refModCtx));

        return baseId;
    }

    // ── SUBSCRIPT TOKEN INTERPRETATION ──

    /// <summary>
    /// Interpret the flat SUBSCRIPT-mode token sequence into expressions.
    /// Returns (expressions, isRefMod). If SUB_COLON is present, it's ref-mod
    /// and expressions[0] = start, expressions[1] = length. Otherwise it's subscripts.
    /// </summary>
    internal (List<BoundExpression> Exprs, bool IsRefMod) InterpretSubscriptTokens(
        CobolParserCore.SubscriptOrRefModContext ctx)
    {
        // Collect all leaf tokens from the subToken+ tree
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        // Check for colon → ref-mod
        int colonIdx = tokens.FindIndex(t => t.Type == CobolParserCore.SUB_COLON);
        if (colonIdx >= 0)
        {
            // Ref-mod: split on colon, parse each half as arithmetic expression
            var startTokens = tokens.GetRange(0, colonIdx);
            var lengthTokens = colonIdx + 1 < tokens.Count
                ? tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1)
                : new List<IToken>();
            var exprs = new List<BoundExpression>();
            exprs.Add(BindSubscriptTokensAsArithmetic(startTokens));
            if (lengthTokens.Any(t => t.Type != CobolParserCore.SUB_WS))
                exprs.Add(BindSubscriptTokensAsArithmetic(lengthTokens));
            return (exprs, true);
        }

        // Subscripts: split on multi-space (SUB_WS with 2+ chars) or SUB_COMMA boundaries
        var segments = SplitSubscriptTokens(tokens);
        var subs = new List<BoundExpression>();
        foreach (var seg in segments)
            subs.Add(BindSubscriptSegment(seg));
        return (subs, false);
    }

    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term)
        {
            tokens.Add(term.Symbol);
            return;
        }
        for (int i = 0; i < node.ChildCount; i++)
            CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split token list into subscript segments on WS/COMMA boundaries.</summary>
    internal static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;   // nested-paren depth: only split at depth 0

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_LPAREN) { depth++; current.Add(t); continue; }
            if (t.Type == CobolParserCore.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == CobolParserCore.SUB_COMMA || t.Type == CobolParserCore.SUB_SEMICOLON))
            {
                if (current.Count > 0) segments.Add(current);
                current = new List<IToken>();
                continue;
            }
            if (depth == 0 && t.Type == CobolParserCore.SUB_WS)
            {
                // Multi-space is a subscript separator. Single space could be part of
                // relative subscripting (IDENT + N). Check: if next non-WS is a sign
                // token (SUB_PLUS/SUB_MINUS) and current ends with an identifier,
                // it MIGHT be relative. But SIGNED_INTEGERLIT already handled the
                // adjacent-sign case. If we see SUB_PLUS/SUB_MINUS after WS, it's
                // part of relative subscripting (operator separated by space).
                // Split only when what follows starts a new subscript:
                //   SIGNED_INTEGERLIT, SUB_IDENTIFIER, SUB_INTEGERLIT, SUB_ALL
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == CobolParserCore.SUB_WS)
                    next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    int nextType = tokens[next].Type;
                    // Only split if next token starts a new subscript AND current
                    // segment doesn't end with an operator (which would mean the
                    // WS is inside a relative subscript: IDENT + N)
                    var lastNonWs = current.FindLast(x => x.Type != CobolParserCore.SUB_WS);
                    // A trailing arithmetic operator means the WS is inside an expression
                    // (e.g. "9 * A", "B / 2", "X + N") — do not split the argument here.
                    bool endsWithOperator = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_PLUS || lastNonWs.Type == CobolParserCore.SUB_MINUS
                         || lastNonWs.Type == CobolParserCore.SUB_STAR || lastNonWs.Type == CobolParserCore.SUB_SLASH
                         || lastNonWs.Type == CobolParserCore.SUB_POWER);
                    // Don't split after OF/IN — these are qualification keywords
                    bool endsWithQualifier = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_OF || lastNonWs.Type == CobolParserCore.SUB_IN);
                    // Don't split after the FUNCTION keyword — the following name is part of a
                    // nested intrinsic-function call (e.g. ACOS(FUNCTION ACOS(D / D))).
                    bool endsWithFunction = lastNonWs != null
                        && lastNonWs.Type == CobolParserCore.SUB_IDENTIFIER
                        && string.Equals(lastNonWs.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase);

                    if (!endsWithOperator && !endsWithQualifier && !endsWithFunction &&
                        (nextType == CobolParserCore.SIGNED_INTEGERLIT
                         || nextType == CobolParserCore.SIGNED_DECIMALLIT
                         || nextType == CobolParserCore.SUB_IDENTIFIER
                         || nextType == CobolParserCore.SUB_INTEGERLIT
                         || nextType == CobolParserCore.SUB_ALL))
                    {
                        segments.Add(current);
                        current = new List<IToken>();
                        i = next - 1; // skip consumed WS
                        continue;
                    }
                }
                // Part of relative subscripting — keep in current segment
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Bind a single subscript segment (list of SUBSCRIPT-mode tokens).</summary>
    internal BoundExpression BindSubscriptSegment(List<IToken> tokens)
    {
        // Remove leading/trailing WS
        while (tokens.Count > 0 && tokens[0].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(tokens.Count - 1);

        if (tokens.Count == 0)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // A segment that is a full arithmetic expression or a nested intrinsic-function call
        // (intrinsic-function arguments such as "9 * A", "B / 2", "E + .001", "B - 2",
        // "FUNCTION INTEGER(1.6)") is parsed by the arithmetic parser. This includes additive
        // operators: a relative subscript "IDENT ± integer" yields the same bound tree through
        // the arithmetic parser, while expressions like "E + .001" or "1 - .1" are ONLY handled
        // correctly there — the simpler relative-offset path below recognises just IDENT ± INT
        // and silently dropped a decimal or second operand. A single SIGNED literal (+8, -3) has
        // no separate SUB_PLUS/SUB_MINUS token, so it still takes the simple path.
        if (tokens.Any(t => t.Type is CobolParserCore.SUB_STAR or CobolParserCore.SUB_SLASH
                or CobolParserCore.SUB_POWER or CobolParserCore.SUB_PLUS or CobolParserCore.SUB_MINUS
                || (t.Type == CobolParserCore.SUB_IDENTIFIER
                    && string.Equals(t.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase))))
            return BindSubscriptTokensAsArithmetic(tokens);

        // Single SIGNED_INTEGERLIT: +8, -3
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SIGNED_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Single SUB_INTEGERLIT: 1, 10
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_INTEGERLIT)
        {
            decimal value = decimal.Parse(tokens[0].Text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL
        if (tokens.Count == 1 && tokens[0].Type == CobolParserCore.SUB_ALL)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Identifier with optional qualification (OF/IN) and relative offset
        // Extract identifier and qualifiers first
        string? baseName = null;
        var qualNames = new List<string>();
        bool expectingQualifier = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_WS) continue;
            if (t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN)
            {
                expectingQualifier = true;
                continue;
            }
            if (t.Type == CobolParserCore.SUB_IDENTIFIER)
            {
                if (baseName == null) baseName = t.Text;
                else if (expectingQualifier) { qualNames.Add(t.Text); expectingQualifier = false; }
                continue;
            }
            // Remaining tokens are operator + offset (relative subscript)
            break;
        }

        if (baseName == null)
            return BindSubscriptTokensAsArithmetic(tokens);

        DataSymbol? sym2;
        if (qualNames.Count > 0)
            sym2 = ResolveQualifiedName(baseName, qualNames);
        else
            sym2 = _ctx.Semantic.ResolveData(baseName);

        // Nested subscript or reference-modification on the base reference, e.g. an
        // intrinsic-function argument like MEDIAN(IND(1), IND(2)) or REVERSE(WS(1:3)).
        // The base identifier is followed by a balanced SUB_LPAREN … SUB_RPAREN group;
        // inside SUBSCRIPT mode a nested '(' pushes another SUBSCRIPT mode (CobolLexer.g4),
        // so the inner subscripts/colon arrive as ordinary SUBSCRIPT tokens. Without this,
        // the subscript was silently dropped and the whole base table was read.
        int lparenIdx = tokens.FindIndex(t => t.Type == CobolParserCore.SUB_LPAREN);
        if (sym2 != null && lparenIdx >= 0)
        {
            var inner = new List<IToken>();
            int pdepth = 0;
            for (int i = lparenIdx; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Type == CobolParserCore.SUB_LPAREN) { pdepth++; if (pdepth == 1) continue; }
                else if (t.Type == CobolParserCore.SUB_RPAREN) { pdepth--; if (pdepth == 0) break; }
                inner.Add(t);
            }
            var cat2 = sym2.ResolvedType?.Category ?? CobolCategory.Numeric;

            int innerColon = inner.FindIndex(t => t.Type == CobolParserCore.SUB_COLON);
            if (innerColon >= 0)
            {
                var startToks = inner.GetRange(0, innerColon);
                var lenToks = innerColon + 1 < inner.Count
                    ? inner.GetRange(innerColon + 1, inner.Count - innerColon - 1)
                    : new List<IToken>();
                var startE = BindSubscriptTokensAsArithmetic(startToks);
                BoundExpression? lenE = lenToks.Any(t => t.Type != CobolParserCore.SUB_WS)
                    ? BindSubscriptTokensAsArithmetic(lenToks) : null;
                var rmBase = new BoundIdentifierExpression(sym2, cat2);
                _ctx.Typed(rmBase);
                return _ctx.Typed(new BoundReferenceModificationExpression(rmBase, startE, lenE));
            }

            var innerSubs = new List<BoundExpression>();
            foreach (var seg in SplitSubscriptTokens(inner))
                innerSubs.Add(BindSubscriptSegment(seg));
            var subscriptedId = new BoundIdentifierExpression(sym2, cat2, innerSubs);
            _ctx.Typed(subscriptedId);
            return subscriptedId;
        }

        BoundExpression baseExpr2 = sym2 != null
            ? new BoundIdentifierExpression(sym2, sym2.ResolvedType?.Category ?? CobolCategory.Numeric)
            : new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);

        // Check for relative offset (+/- integer) in remaining tokens
        var remaining = tokens.SkipWhile(t =>
            t.Type == CobolParserCore.SUB_WS || t.Type == CobolParserCore.SUB_IDENTIFIER
            || t.Type == CobolParserCore.SUB_OF || t.Type == CobolParserCore.SUB_IN).ToList();
        if (remaining.Count >= 2)
        {
            var opTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_PLUS || t.Type == CobolParserCore.SUB_MINUS);
            var numTok = remaining.FirstOrDefault(t => t.Type == CobolParserCore.SUB_INTEGERLIT);
            if (opTok != null && numTok != null)
            {
                var offset = decimal.Parse(numTok.Text, System.Globalization.CultureInfo.InvariantCulture);
                var op = opTok.Type == CobolParserCore.SUB_MINUS
                    ? BoundBinaryOperatorKind.Subtract : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr2, op,
                    new BoundLiteralExpression(offset, CobolCategory.Numeric), CobolCategory.Numeric);
            }
        }

        return baseExpr2;
    }

    /// <summary>
    /// Bind a flat SUBSCRIPT-mode token list as a full arithmetic expression. Used for
    /// reference-modification operands, relative subscripts, and intrinsic-function arguments
    /// (ISO §15). Recursive descent with COBOL precedence: additive → multiplicative → power
    /// → unary → primary, where a primary is a parenthesized expression, a numeric literal, or
    /// a (possibly qualified and/or subscripted) data reference. Whitespace tokens are skipped.
    /// </summary>
    internal BoundExpression BindSubscriptTokensAsArithmetic(List<IToken> tokens)
    {
        int pos = 0;
        var expr = ParseSubAdditive(tokens, ref pos);
        return expr ?? new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    private static int SkipSubWs(List<IToken> toks, int pos)
    {
        while (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_WS) pos++;
        return pos;
    }

    private static int PeekSubType(List<IToken> toks, int pos)
    {
        pos = SkipSubWs(toks, pos);
        return pos < toks.Count ? toks[pos].Type : -1;
    }

    private BoundExpression ParseSubAdditive(List<IToken> toks, ref int pos)
    {
        var left = ParseSubMultiplicative(toks, ref pos);
        while (true)
        {
            int t = PeekSubType(toks, pos);
            if (t != CobolParserCore.SUB_PLUS && t != CobolParserCore.SUB_MINUS) break;
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubMultiplicative(toks, ref pos);
            var op = t == CobolParserCore.SUB_PLUS ? BoundBinaryOperatorKind.Add : BoundBinaryOperatorKind.Subtract;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubMultiplicative(List<IToken> toks, ref int pos)
    {
        var left = ParseSubPower(toks, ref pos);
        while (true)
        {
            int t = PeekSubType(toks, pos);
            if (t != CobolParserCore.SUB_STAR && t != CobolParserCore.SUB_SLASH) break;
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubPower(toks, ref pos);
            var op = t == CobolParserCore.SUB_STAR ? BoundBinaryOperatorKind.Multiply : BoundBinaryOperatorKind.Divide;
            left = new BoundBinaryExpression(left, op, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubPower(List<IToken> toks, ref int pos)
    {
        var left = ParseSubUnary(toks, ref pos);
        if (PeekSubType(toks, pos) == CobolParserCore.SUB_POWER)
        {
            pos = SkipSubWs(toks, pos) + 1;
            var right = ParseSubPower(toks, ref pos); // right-associative
            return new BoundBinaryExpression(left, BoundBinaryOperatorKind.Power, right, CobolCategory.Numeric);
        }
        return left;
    }

    private BoundExpression ParseSubUnary(List<IToken> toks, ref int pos)
    {
        int t = PeekSubType(toks, pos);
        if (t == CobolParserCore.SUB_PLUS || t == CobolParserCore.SUB_MINUS)
        {
            pos = SkipSubWs(toks, pos) + 1;
            var operand = ParseSubUnary(toks, ref pos);
            if (t == CobolParserCore.SUB_PLUS) return operand;
            return new BoundBinaryExpression(
                new BoundLiteralExpression(0m, CobolCategory.Numeric),
                BoundBinaryOperatorKind.Subtract, operand, CobolCategory.Numeric);
        }
        return ParseSubPrimary(toks, ref pos);
    }

    private BoundExpression ParseSubPrimary(List<IToken> toks, ref int pos)
    {
        pos = SkipSubWs(toks, pos);
        if (pos >= toks.Count) return new BoundLiteralExpression(0m, CobolCategory.Numeric);
        var tok = toks[pos];

        // Parenthesized sub-expression
        if (tok.Type == CobolParserCore.SUB_LPAREN)
        {
            pos++;
            var inner = ParseSubAdditive(toks, ref pos);
            pos = SkipSubWs(toks, pos);
            if (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_RPAREN) pos++;
            return inner;
        }

        if (tok.Type == CobolParserCore.SUB_INTEGERLIT || tok.Type == CobolParserCore.SUB_DECIMALLIT
            || tok.Type == CobolParserCore.SIGNED_INTEGERLIT || tok.Type == CobolParserCore.SIGNED_DECIMALLIT)
        {
            pos++;
            return new BoundLiteralExpression(
                decimal.Parse(tok.Text, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture),
                CobolCategory.Numeric);
        }

        // Alphanumeric literal argument (e.g. FUNCTION LOWER-CASE("ABC")).
        if (tok.Type == CobolParserCore.SUB_STRINGLIT)
        {
            pos++;
            string text = tok.Text;
            string value = text.Length >= 2
                ? text[1..^1].Replace(new string(text[0], 2), new string(text[0], 1))
                : text;
            return new BoundLiteralExpression(value, CobolCategory.Alphanumeric);
        }

        if (tok.Type == CobolParserCore.SUB_IDENTIFIER)
        {
            // Nested intrinsic-function call as an argument, e.g. ACOS(FUNCTION ACOS(D / D)).
            // In SUBSCRIPT mode the FUNCTION keyword and the function name are plain
            // SUB_IDENTIFIERs, so recognize "FUNCTION name (args)" here.
            if (string.Equals(tok.Text, "FUNCTION", StringComparison.OrdinalIgnoreCase))
            {
                pos++;
                pos = SkipSubWs(toks, pos);
                string fname = (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_IDENTIFIER)
                    ? toks[pos++].Text : "UNKNOWN";
                var fargs = new List<BoundExpression>();
                if (PeekSubType(toks, pos) == CobolParserCore.SUB_LPAREN)
                {
                    pos = SkipSubWs(toks, pos);
                    var innerToks = CollectBalancedSubTokens(toks, ref pos);
                    var (innerExprs, _) = InterpretCollectedSubscriptTokens(innerToks);
                    fargs.AddRange(innerExprs);
                }
                var fcat = BindingContext.AlphanumericFunctions.Contains(fname)
                    ? CobolCategory.Alphanumeric : CobolCategory.Numeric;
                return new BoundFunctionCallExpression(fname, fargs.AsReadOnly(), fcat);
            }

            pos++;
            string baseName = tok.Text;
            var qualNames = new List<string>();
            // Qualification: OF/IN data-name ...
            while (PeekSubType(toks, pos) is CobolParserCore.SUB_OF or CobolParserCore.SUB_IN)
            {
                pos = SkipSubWs(toks, pos) + 1;
                pos = SkipSubWs(toks, pos);
                if (pos < toks.Count && toks[pos].Type == CobolParserCore.SUB_IDENTIFIER)
                    qualNames.Add(toks[pos++].Text);
            }
            // Nested subscript: name(subscripts)
            List<BoundExpression>? subs = null;
            if (PeekSubType(toks, pos) == CobolParserCore.SUB_LPAREN)
            {
                pos = SkipSubWs(toks, pos);
                var inner = CollectBalancedSubTokens(toks, ref pos);
                var (innerExprs, _) = InterpretCollectedSubscriptTokens(inner);
                subs = innerExprs;
            }

            var sym = qualNames.Count > 0 ? ResolveQualifiedName(baseName, qualNames) : _ctx.Semantic.ResolveData(baseName);
            if (sym == null)
                return new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);
            var cat = sym.ResolvedType?.Category ?? CobolCategory.Numeric;
            return _ctx.Typed(new BoundIdentifierExpression(sym, cat, subs));
        }

        // Unknown token — consume and yield 0 so parsing can continue.
        pos++;
        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    /// <summary>Collect the tokens inside a balanced SUB_LPAREN…SUB_RPAREN (excluding the outer parens).</summary>
    private static List<IToken> CollectBalancedSubTokens(List<IToken> toks, ref int pos)
    {
        var inner = new List<IToken>();
        if (pos >= toks.Count || toks[pos].Type != CobolParserCore.SUB_LPAREN) return inner;
        pos++; // consume '('
        int depth = 1;
        while (pos < toks.Count && depth > 0)
        {
            int tt = toks[pos].Type;
            if (tt == CobolParserCore.SUB_LPAREN) depth++;
            else if (tt == CobolParserCore.SUB_RPAREN) { depth--; if (depth == 0) { pos++; break; } }
            inner.Add(toks[pos]);
            pos++;
        }
        return inner;
    }

    /// <summary>Interpret a collected inner token list as a subscript list (split on commas/spaces, bind each).</summary>
    private (List<BoundExpression> Exprs, bool IsRefMod) InterpretCollectedSubscriptTokens(List<IToken> tokens)
    {
        var segments = SplitSubscriptTokens(tokens);
        var subs = new List<BoundExpression>(segments.Count);
        foreach (var seg in segments)
            subs.Add(BindSubscriptSegment(seg));
        return (subs, false);
    }

    /// <summary>
    /// Bind a subscript entry per COBOL-85 §5.3. SUBSCRIPT lexer mode provides
    /// sign-adjacency disambiguation: SIGNED_INTEGERLIT (+N) vs SUB_PLUS SUB_WS SUB_INTEGERLIT (+ N).
    /// </summary>
    internal BoundExpression BindSubscriptEntry(CobolParserCore.SubscriptEntryContext ctx)
    {
        // Signed integer literal: +8, -3, +1 (sign adjacent to digits)
        if (ctx.SIGNED_INTEGERLIT() is { } signedLit)
        {
            string text = signedLit.GetText();
            decimal value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // Unsigned integer literal: 1, 10, 300
        if (ctx.SUB_INTEGERLIT() is { } intLit)
        {
            decimal value = decimal.Parse(intLit.GetText(), System.Globalization.CultureInfo.InvariantCulture);
            return new BoundLiteralExpression(value, CobolCategory.Numeric);
        }

        // ALL (for SEARCH ALL)
        if (ctx.SUB_ALL() != null)
            return new BoundLiteralExpression("ALL", CobolCategory.Alphanumeric);

        // Data-name / index-name with optional qualification and relative offset
        if (ctx.SUB_IDENTIFIER() is { } idToken)
        {
            string baseName = idToken.GetText();

            // Handle qualifications
            var quals = ctx.subscriptQualification();
            DataSymbol? baseSym;
            if (quals.Length > 0)
            {
                var qualNames = new List<string>();
                foreach (var q in quals)
                    qualNames.Add(q.SUB_IDENTIFIER().GetText());
                baseSym = ResolveQualifiedName(baseName, qualNames);
            }
            else
            {
                baseSym = _ctx.Semantic.ResolveData(baseName);
            }

            BoundExpression baseExpr;
            if (baseSym != null)
            {
                var baseCat = baseSym.ResolvedType?.Category ?? CobolCategory.Numeric;
                baseExpr = new BoundIdentifierExpression(baseSym, baseCat);
            }
            else
            {
                baseExpr = new BoundLiteralExpression(baseName, CobolCategory.Alphanumeric);
            }

            // Relative subscript offset: data-name + N or data-name - N
            if (ctx.relativeOffset() is { } relOff)
            {
                decimal offset = decimal.Parse(relOff.SUB_INTEGERLIT().GetText(),
                    System.Globalization.CultureInfo.InvariantCulture);
                var offsetLit = new BoundLiteralExpression(offset, CobolCategory.Numeric);
                var op = relOff.SUB_MINUS() != null
                    ? BoundBinaryOperatorKind.Subtract
                    : BoundBinaryOperatorKind.Add;
                return new BoundBinaryExpression(baseExpr, op, offsetLit, CobolCategory.Numeric);
            }

            return baseExpr;
        }

        return new BoundLiteralExpression(0m, CobolCategory.Numeric);
    }

    // ── QUALIFIED NAME RESOLUTION ──

    /// <summary>
    /// Resolve a qualified name using right-to-left narrowing.
    /// A OF B OF C → resolve C (outermost), then B within C, then A within B.
    /// </summary>
    internal DataSymbol? ResolveQualifiedName(string name, List<string> qualifiers)
    {
        // Start from the rightmost (outermost) qualifier
        DataSymbol? context = _ctx.Semantic.ResolveData(qualifiers[^1]);
        if (context == null) return null;

        // Walk qualifiers right-to-left (skip the last one, already resolved)
        for (int i = qualifiers.Count - 2; i >= 0; i--)
        {
            context = FindChild(context, qualifiers[i]);
            if (context == null) return null;
        }

        // Resolve the target name within the final context
        return FindChild(context, name);
    }

    /// <summary>
    /// Find a child data symbol by name within a group item.
    /// Searches recursively through the group's children.
    /// </summary>
    internal static DataSymbol? FindChild(DataSymbol parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                return child;
            // Search deeper (intermediate groups)
            var deep = FindChild(child, name);
            if (deep != null) return deep;
        }
        return null;
    }

    // ── REFERENCE MODIFICATION ──

    internal BoundExpression BindReferenceModification(
        BoundIdentifierExpression baseId,
        CobolParserCore.RefModSpecContext ctx)
    {
        var arithExprs = ctx.arithmeticExpression();
        var startExpr = BindArithmeticExpr(arithExprs[0]);

        BoundExpression? lengthExpr = null;
        if (arithExprs.Length > 1)
            lengthExpr = BindArithmeticExpr(arithExprs[1]);

        return new BoundReferenceModificationExpression(baseId, startExpr, lengthExpr);
    }
}
