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
        // Arguments (if any) are captured as subscriptPart tokens by the SUBSCRIPT lexer mode.
        var funcName = ctx.functionName()?.GetText() ?? "UNKNOWN";

        var args = new List<BoundExpression>();
        var subPart = ctx.subscriptPart();
        if (subPart != null)
        {
            var subOrRefMod = subPart.subscriptOrRefMod();
            if (subOrRefMod != null)
            {
                // Reuse the subscript token interpreter — it splits comma-separated
                // expressions which is exactly what function arguments are.
                var (subExprs, _) = InterpretSubscriptTokens(subOrRefMod);
                args.AddRange(subExprs);
            }
        }

        // FUNCTION LENGTH returns the defined size of the operand, not its content length.
        // Per ISO §15.24: "the value returned is the number of character positions
        // in argument-1". Resolved at bind time — no runtime call needed.
        if (funcName.Equals("LENGTH", StringComparison.OrdinalIgnoreCase) && args.Count == 1)
        {
            decimal lengthValue = 0;
            if (args[0] is BoundIdentifierExpression idExpr)
                lengthValue = idExpr.Symbol.ElementSize;
            else if (args[0] is BoundLiteralExpression litExpr && litExpr.Value is string s)
                lengthValue = s.Length;
            else if (args[0] is BoundLiteralExpression numLit && numLit.Value is decimal d)
                lengthValue = d; // already a number (e.g., from nested function)
            return new BoundLiteralExpression(lengthValue, CobolCategory.Numeric);
        }

        var category = BindingContext.AlphanumericFunctions.Contains(funcName)
            ? CobolCategory.Alphanumeric
            : CobolCategory.Numeric;

        return new BoundFunctionCallExpression(funcName, args.AsReadOnly(), category);
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

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == CobolParserCore.SUB_COMMA || t.Type == CobolParserCore.SUB_SEMICOLON)
            {
                if (current.Count > 0) segments.Add(current);
                current = new List<IToken>();
                continue;
            }
            if (t.Type == CobolParserCore.SUB_WS)
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
                    bool endsWithOperator = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_PLUS || lastNonWs.Type == CobolParserCore.SUB_MINUS);
                    // Don't split after OF/IN — these are qualification keywords
                    bool endsWithQualifier = lastNonWs != null &&
                        (lastNonWs.Type == CobolParserCore.SUB_OF || lastNonWs.Type == CobolParserCore.SUB_IN);

                    if (!endsWithOperator && !endsWithQualifier &&
                        (nextType == CobolParserCore.SIGNED_INTEGERLIT
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

    /// <summary>Bind a token list as an arithmetic expression (for ref-mod or relative subscript).</summary>
    internal BoundExpression BindSubscriptTokensAsArithmetic(List<IToken> tokens)
    {
        // Remove leading/trailing WS
        while (tokens.Count > 0 && tokens[0].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(0);
        while (tokens.Count > 0 && tokens[^1].Type == CobolParserCore.SUB_WS)
            tokens.RemoveAt(tokens.Count - 1);

        if (tokens.Count == 0)
            return new BoundLiteralExpression(0m, CobolCategory.Numeric);

        // Build expression from tokens: handle identifiers, integers, +/- operators
        BoundExpression? result = null;
        BoundBinaryOperatorKind pendingOp = default;
        bool hasPendingOp = false;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];

            BoundExpression? term = null;
            if (t.Type == CobolParserCore.SUB_IDENTIFIER)
            {
                var sym = _ctx.Semantic.ResolveData(t.Text);
                term = sym != null
                    ? new BoundIdentifierExpression(sym, sym.ResolvedType?.Category ?? CobolCategory.Numeric)
                    : (BoundExpression)new BoundLiteralExpression(t.Text, CobolCategory.Alphanumeric);
            }
            else if (t.Type == CobolParserCore.SUB_INTEGERLIT || t.Type == CobolParserCore.SIGNED_INTEGERLIT)
            {
                term = new BoundLiteralExpression(
                    decimal.Parse(t.Text, System.Globalization.CultureInfo.InvariantCulture),
                    CobolCategory.Numeric);
            }
            else if (t.Type == CobolParserCore.SUB_PLUS)
            {
                pendingOp = BoundBinaryOperatorKind.Add;
                hasPendingOp = true;
                continue;
            }
            else if (t.Type == CobolParserCore.SUB_MINUS)
            {
                pendingOp = BoundBinaryOperatorKind.Subtract;
                hasPendingOp = true;
                continue;
            }
            else continue; // skip OF, IN, etc. for now

            if (term != null)
            {
                if (result == null)
                    result = term;
                else if (hasPendingOp)
                {
                    result = new BoundBinaryExpression(result, pendingOp, term, CobolCategory.Numeric);
                    hasPendingOp = false;
                }
            }
        }

        return result ?? new BoundLiteralExpression(0m, CobolCategory.Numeric);
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
