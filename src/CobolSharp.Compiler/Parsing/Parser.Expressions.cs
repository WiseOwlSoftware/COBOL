using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
    // ═══════════════════════════════════════════════════
    // Expression parsing
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Parse a general expression (used in DISPLAY operands, MOVE source, VALUE, etc.)
    /// </summary>
    private Expression ParseExpression()
    {
        // Handle unary +/- for signed literals (VALUE +123, VALUE -45.6)
        if ((Check(TokenKind.Plus) || Check(TokenKind.Minus)) &&
            (Peek().Kind == TokenKind.IntegerLiteral || Peek().Kind == TokenKind.DecimalLiteral))
        {
            bool negate = Check(TokenKind.Minus);
            Advance(); // consume +/-
            var lit = ParsePrimaryExpression();
            if (negate && lit is NumericLiteralExpression numLit)
                return new NumericLiteralExpression(-numLit.Value,
                    TextSpan.FromBounds(numLit.Span.Start - 1, numLit.Span.End));
            return lit;
        }
        return ParsePrimaryExpression();
    }

    /// <summary>
    /// Parse an arithmetic expression with operator precedence (for COMPUTE).
    /// </summary>
    private Expression ParseArithmeticExpression()
    {
        return ParseAddSubtract();
    }

    private Expression ParseAddSubtract()
    {
        var left = ParseMultiplyDivide();
        while (Check(TokenKind.Plus) || Check(TokenKind.Minus))
        {
            var op = Current.Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            Advance();
            var right = ParseMultiplyDivide();
            left = new BinaryExpression(left, op, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseMultiplyDivide()
    {
        var left = ParsePower();
        while (Check(TokenKind.Multiply) || Check(TokenKind.Divide))
        {
            var op = Current.Kind == TokenKind.Multiply ? BinaryOperator.Multiply : BinaryOperator.Divide;
            Advance();
            var right = ParsePower();
            left = new BinaryExpression(left, op, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParsePower()
    {
        var left = ParseUnary();
        if (Check(TokenKind.Power))
        {
            Advance();
            var right = ParsePower(); // right-associative
            return new BinaryExpression(left, BinaryOperator.Power, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private Expression ParseUnary()
    {
        if (Check(TokenKind.Minus))
        {
            int start = Current.Span.Start;
            Advance();
            var operand = ParsePrimaryExpression();
            return new UnaryExpression(UnaryOperator.Negate, operand,
                TextSpan.FromBounds(start, operand.Span.End));
        }
        if (Check(TokenKind.Plus))
        {
            // Unary plus — consume and return the operand as-is
            Advance();
            return ParsePrimaryExpression();
        }
        return ParsePrimaryExpression();
    }

    private Expression ParsePrimaryExpression()
    {
        var token = Current;

        switch (token.Kind)
        {
            case TokenKind.IntegerLiteral:
                Advance();
                return new NumericLiteralExpression(Convert.ToDecimal(token.Value), token.Span);

            case TokenKind.DecimalLiteral:
                Advance();
                return new NumericLiteralExpression((decimal)token.Value!, token.Span);

            case TokenKind.StringLiteral:
                Advance();
                return new StringLiteralExpression((string)token.Value!, token.Span);

            case TokenKind.Identifier:
                Advance();
                // Consume IN/OF qualification chain: NAME IN PARENT OF GRANDPARENT
                // Keep only the most specific (leftmost) name for now
                while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
                {
                    Advance(); // IN or OF
                    if (Check(TokenKind.Identifier))
                        Advance(); // qualifier name — consumed but not stored
                }
                // Check for subscripts or reference modification: NAME(...)
                if (Check(TokenKind.LeftParen))
                {
                    return ParseSubscriptOrRefMod(token);
                }
                return new IdentifierExpression(token.Text, token.Span);

            case TokenKind.ZeroKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Zero, token.Span);

            case TokenKind.SpaceKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Space, token.Span);

            case TokenKind.HighValueKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.HighValue, token.Span);

            case TokenKind.LowValueKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.LowValue, token.Span);

            case TokenKind.QuoteKeyword:
                Advance();
                return new FigurativeConstantExpression(FigurativeConstant.Quote, token.Span);

            case TokenKind.TrueKeyword:
                Advance();
                return new NumericLiteralExpression(1, token.Span); // TRUE → 1

            case TokenKind.FalseKeyword:
                Advance();
                return new NumericLiteralExpression(0, token.Span); // FALSE → 0

            case TokenKind.FunctionKeyword:
                return ParseFunctionCall();

            case TokenKind.HexLiteral:
            case TokenKind.BooleanLiteral:
            case TokenKind.NationalLiteral:
                Advance();
                return new StringLiteralExpression((string)token.Value!, token.Span);

            case TokenKind.AllKeyword:
                // ALL literal — figurative constant meaning "fill with literal"
                Advance(); // ALL
                if (Check(TokenKind.StringLiteral))
                {
                    var allLit = Advance();
                    return new StringLiteralExpression((string)allLit.Value!,
                        TextSpan.FromBounds(token.Span.Start, allLit.Span.End));
                }
                else if (Check(TokenKind.ZeroKeyword) || Check(TokenKind.SpaceKeyword) ||
                         Check(TokenKind.HighValueKeyword) || Check(TokenKind.LowValueKeyword) ||
                         Check(TokenKind.QuoteKeyword))
                {
                    // ALL ZEROS, ALL SPACES, etc. — parse the figurative constant
                    return ParsePrimaryExpression();
                }
                else if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.DecimalLiteral))
                {
                    var allNum = Advance();
                    return new NumericLiteralExpression(Convert.ToDecimal(allNum.Value),
                        TextSpan.FromBounds(token.Span.Start, allNum.Span.End));
                }
                // ALL by itself (e.g., in INSPECT TALLYING ALL)
                return new StringLiteralExpression("ALL", token.Span);

            case TokenKind.Comma:
                // Commas are separators in COBOL — skip and parse the next expression
                Advance();
                return ParsePrimaryExpression();

            case TokenKind.LeftParen:
                Advance();
                var expr = ParseArithmeticExpression();
                Expect(TokenKind.RightParen, "Expected closing parenthesis");
                return expr;

            default:
                ReportError("CS0300", $"Expected expression, found '{token.Text}'");
                Advance();
                return new NumericLiteralExpression(0, token.Span); // error recovery
        }
    }

    /// <summary>
    /// Parse subscripts or reference modification after an identifier.
    /// Subscripts: NAME(expr1, expr2, ...)
    /// Reference modification: NAME(start : length)
    /// Distinguishing: if we see a colon, it's ref-mod; otherwise subscripts.
    /// </summary>
    private FunctionCallExpression ParseFunctionCall()
    {
        int start = Current.Span.Start;
        Advance(); // FUNCTION

        string funcName = Expect(TokenKind.Identifier, "Expected function name after FUNCTION").Text;

        var args = new List<Expression>();
        if (Match(TokenKind.LeftParen))
        {
            while (!Check(TokenKind.RightParen) && Current.Kind != TokenKind.EndOfFile)
            {
                args.Add(ParseArithmeticExpression());
                if (!Check(TokenKind.RightParen))
                    Match(TokenKind.Comma); // optional comma separator
            }
            Expect(TokenKind.RightParen, "Expected ) after function arguments");
        }

        return new FunctionCallExpression(funcName.ToUpperInvariant(), args,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    private IdentifierExpression ParseSubscriptOrRefMod(Token nameToken)
    {
        Advance(); // consume (

        // Parse first expression
        var first = ParseArithmeticExpression();

        // Check for colon → reference modification: NAME(start : length)
        if (Check(TokenKind.Colon))
        {
            Advance(); // consume :
            Expression? length = null;
            if (!Check(TokenKind.RightParen))
            {
                length = ParseArithmeticExpression();
            }
            Expect(TokenKind.RightParen, "Expected ) after reference modification");
            var span = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
            return new IdentifierExpression(nameToken.Text, span,
                refModStart: first, refModLength: length);
        }

        // Subscripts: NAME(expr1, expr2, ...) or NAME(expr1 expr2 ...)
        // COBOL subscripts can be separated by commas OR spaces
        var subscripts = new List<Expression> { first };
        Match(TokenKind.Comma); // optional comma separator
        while (!Check(TokenKind.RightParen) && !Check(TokenKind.Colon) &&
               Current.Kind != TokenKind.EndOfFile && !Check(TokenKind.Period))
        {
            subscripts.Add(ParseArithmeticExpression());
            Match(TokenKind.Comma); // optional comma separator
        }

        // Check for reference modification after subscripts: NAME(sub1, sub2)(start:len)
        // or colon in the middle if it's actually ref-mod: NAME(start : length)
        if (Check(TokenKind.Colon))
        {
            // This was actually reference modification, not subscripts
            // The first subscript is the ref-mod start
            Advance(); // consume :
            Expression? length = null;
            if (!Check(TokenKind.RightParen))
            {
                length = ParseArithmeticExpression();
            }
            Expect(TokenKind.RightParen, "Expected ) after reference modification");
            var spanRefMod = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
            // If we had multiple "subscripts" before the colon, the last one is
            // actually the ref-mod start and the preceding ones are real subscripts
            if (subscripts.Count > 1)
            {
                var refStart = subscripts[^1];
                var realSubs = subscripts.GetRange(0, subscripts.Count - 1);
                return new IdentifierExpression(nameToken.Text, spanRefMod,
                    subscripts: realSubs, refModStart: refStart, refModLength: length);
            }
            return new IdentifierExpression(nameToken.Text, spanRefMod,
                refModStart: first, refModLength: length);
        }

        Expect(TokenKind.RightParen, "Expected ) after subscripts");

        var span2 = TextSpan.FromBounds(nameToken.Span.Start, Current.Span.Start);
        return new IdentifierExpression(nameToken.Text, span2, subscripts: subscripts);
    }

    /// <summary>
    /// Parse a condition expression (for IF, PERFORM UNTIL).
    /// Handles relational operators and simple comparisons.
    /// </summary>
    private Expression ParseConditionExpression()
    {
        return ParseOrExpression();
    }

    private Expression ParseOrExpression()
    {
        var left = ParseAndExpression();
        while (Check(TokenKind.OrKeyword))
        {
            Advance();

            // Check for abbreviated combined relation: A > B OR <= C
            if (left is BinaryExpression leftBin && IsRelationalOp(leftBin.Operator))
            {
                // Issue 5 (§4.2.3): Handle NOT in abbreviated OR context
                if (Check(TokenKind.NotKeyword))
                {
                    int saved = _position;
                    Advance(); // NOT
                    var negOp = TryParseRelationalOperator();
                    if (negOp.HasValue)
                    {
                        var finalOp = NegateRelationalOp(negOp.Value);
                        var abbrevRight = ParseArithmeticExpression();
                        var right = new BinaryExpression(leftBin.Left, finalOp, abbrevRight,
                            TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                        left = new BinaryExpression(left, BinaryOperator.Or, right,
                            TextSpan.FromBounds(left.Span.Start, right.Span.End));
                        continue;
                    }
                    // Bare NOT value: A > B OR NOT C → A > B OR A <= C
                    var notRight = ParseArithmeticExpression();
                    if (notRight is not BinaryExpression)
                    {
                        var negatedOp = NegateRelationalOp(leftBin.Operator);
                        var expandedRight = new BinaryExpression(leftBin.Left, negatedOp, notRight,
                            TextSpan.FromBounds(notRight.Span.Start, notRight.Span.End));
                        left = new BinaryExpression(left, BinaryOperator.Or, expandedRight,
                            TextSpan.FromBounds(left.Span.Start, expandedRight.Span.End));
                        continue;
                    }
                    _position = saved;
                }

                int saved2 = _position;
                var abbrevOp = TryParseRelationalOperator();
                if (abbrevOp.HasValue)
                {
                    var abbrevRight = ParseArithmeticExpression();
                    var right = new BinaryExpression(leftBin.Left, abbrevOp.Value, abbrevRight,
                        TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                    left = new BinaryExpression(left, BinaryOperator.Or, right,
                        TextSpan.FromBounds(left.Span.Start, right.Span.End));
                    continue;
                }
                _position = saved2;
            }

            // Check for abbreviated: A > B OR C → A > B OR A > C
            var rightExpr = ParseAndExpression();
            if (rightExpr is not BinaryExpression && left is BinaryExpression leftBin2 &&
                IsRelationalOp(leftBin2.Operator))
            {
                rightExpr = new BinaryExpression(leftBin2.Left, leftBin2.Operator, rightExpr,
                    TextSpan.FromBounds(rightExpr.Span.Start, rightExpr.Span.End));
            }

            left = new BinaryExpression(left, BinaryOperator.Or, rightExpr,
                TextSpan.FromBounds(left.Span.Start, rightExpr.Span.End));
        }
        return left;
    }

    private Expression ParseAndExpression()
    {
        var left = ParseNotExpression();
        while (Check(TokenKind.AndKeyword))
        {
            Advance();

            // Check for abbreviated combined relation: A > B AND <= C
            // If left is a relational expression and we see a relational operator,
            // carry the subject from the left side
            if (left is BinaryExpression leftBin && IsRelationalOp(leftBin.Operator))
            {
                // Issue 5 (§4.2.3): Handle NOT in abbreviated context
                // A > B AND NOT C → A > B AND A <= C (NOT negates carried-forward op)
                if (Check(TokenKind.NotKeyword))
                {
                    int saved = _position;
                    Advance(); // NOT

                    // Try relational operator after NOT: A > B AND NOT <= C
                    var negOp = TryParseRelationalOperator();
                    if (negOp.HasValue)
                    {
                        // Negate the operator
                        var finalOp = NegateRelationalOp(negOp.Value);
                        var abbrevRight = ParseArithmeticExpression();
                        var right = new BinaryExpression(leftBin.Left, finalOp, abbrevRight,
                            TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                        left = new BinaryExpression(left, BinaryOperator.And, right,
                            TextSpan.FromBounds(left.Span.Start, right.Span.End));
                        continue;
                    }

                    // No relational op after NOT — check for bare value abbreviation
                    // A > B AND NOT C → A > B AND NOT(A > C) = A > B AND A <= C
                    var notRight = ParseArithmeticExpression();
                    if (notRight is not BinaryExpression)
                    {
                        // Negate the carried-forward operator
                        var negatedOp = NegateRelationalOp(leftBin.Operator);
                        var expandedRight = new BinaryExpression(leftBin.Left, negatedOp, notRight,
                            TextSpan.FromBounds(notRight.Span.Start, notRight.Span.End));
                        left = new BinaryExpression(left, BinaryOperator.And, expandedRight,
                            TextSpan.FromBounds(left.Span.Start, expandedRight.Span.End));
                        continue;
                    }

                    // Was a complex expression — treat NOT as unary NOT (backtrack and reparse)
                    _position = saved;
                }

                // Try to parse a relational operator at the current position
                int saved2 = _position;
                var abbrevOp = TryParseRelationalOperator();
                if (abbrevOp.HasValue)
                {
                    // Abbreviated form: A > B AND <= C → A > B AND A <= C
                    var abbrevRight = ParseArithmeticExpression();
                    var right = new BinaryExpression(leftBin.Left, abbrevOp.Value, abbrevRight,
                        TextSpan.FromBounds(abbrevRight.Span.Start, abbrevRight.Span.End));
                    left = new BinaryExpression(left, BinaryOperator.And, right,
                        TextSpan.FromBounds(left.Span.Start, right.Span.End));
                    continue;
                }
                _position = saved2;
            }

            var rightExpr = ParseNotExpression();

            // If right is just a bare value (not a relational/logical expression),
            // and left is a relational expression, expand the abbreviation
            // A > B AND C → A > B AND A > C
            if (rightExpr is not BinaryExpression && left is BinaryExpression leftBin2 &&
                IsRelationalOp(leftBin2.Operator))
            {
                rightExpr = new BinaryExpression(leftBin2.Left, leftBin2.Operator, rightExpr,
                    TextSpan.FromBounds(rightExpr.Span.Start, rightExpr.Span.End));
            }

            left = new BinaryExpression(left, BinaryOperator.And, rightExpr,
                TextSpan.FromBounds(left.Span.Start, rightExpr.Span.End));
        }
        return left;
    }

    /// <summary>
    /// Issue 5 (§4.2.3): Negate a relational operator for abbreviated NOT context.
    /// </summary>
    private static BinaryOperator NegateRelationalOp(BinaryOperator op) => op switch
    {
        BinaryOperator.Equal => BinaryOperator.NotEqual,
        BinaryOperator.NotEqual => BinaryOperator.Equal,
        BinaryOperator.LessThan => BinaryOperator.GreaterThanOrEqual,
        BinaryOperator.GreaterThan => BinaryOperator.LessThanOrEqual,
        BinaryOperator.LessThanOrEqual => BinaryOperator.GreaterThan,
        BinaryOperator.GreaterThanOrEqual => BinaryOperator.LessThan,
        _ => op
    };

    private static bool IsRelationalOp(BinaryOperator op) => op is
        BinaryOperator.Equal or BinaryOperator.NotEqual or
        BinaryOperator.LessThan or BinaryOperator.GreaterThan or
        BinaryOperator.LessThanOrEqual or BinaryOperator.GreaterThanOrEqual;

    private Expression ParseNotExpression()
    {
        if (Check(TokenKind.NotKeyword))
        {
            int start = Current.Span.Start;
            Advance();
            var operand = ParseNotExpression(); // NOT can apply to parenthesized conditions
            return new UnaryExpression(UnaryOperator.Not, operand,
                TextSpan.FromBounds(start, operand.Span.End));
        }
        // Per §8.8.4.9: "Parentheses may override precedence" in conditions.
        // (condition) is valid wherever a simple condition appears.
        if (Check(TokenKind.LeftParen))
        {
            // Try parsing as a parenthesized condition.
            int saved = _position;
            Advance(); // consume (
            var inner = ParseConditionExpression();
            if (Check(TokenKind.RightParen))
            {
                Advance(); // consume )
                return inner;
            }
            // Not a parenthesized condition — backtrack
            _position = saved;
        }
        return ParseRelationalExpression();
    }

    private Expression ParseRelationalExpression()
    {
        var left = ParseArithmeticExpression();

        // Check for class condition: identifier IS [NOT] NUMERIC/ALPHABETIC
        if (Check(TokenKind.IsKeyword) || Check(TokenKind.NotKeyword))
        {
            int saved = _position;
            bool isNot = false;

            if (Check(TokenKind.IsKeyword)) Advance();
            if (Check(TokenKind.NotKeyword)) { isNot = true; Advance(); }

            if (Check(TokenKind.NumericKeyword) || Check(TokenKind.AlphabeticKeyword) ||
                Check(TokenKind.PositiveKeyword) || Check(TokenKind.NegativeKeyword) ||
                Check(TokenKind.ZeroKeyword))
            {
                // Class or sign condition — represent as a binary expression
                // using Equal/NotEqual with the class as a figurative constant
                var classToken = Advance();
                Expression classExpr;
                if (classToken.Kind == TokenKind.NumericKeyword)
                    classExpr = new StringLiteralExpression("NUMERIC", classToken.Span);
                else if (classToken.Kind == TokenKind.AlphabeticKeyword)
                    classExpr = new StringLiteralExpression("ALPHABETIC", classToken.Span);
                else if (classToken.Kind == TokenKind.PositiveKeyword)
                    classExpr = new StringLiteralExpression("POSITIVE", classToken.Span);
                else if (classToken.Kind == TokenKind.NegativeKeyword)
                    classExpr = new StringLiteralExpression("NEGATIVE", classToken.Span);
                else // ZeroKeyword
                    classExpr = new FigurativeConstantExpression(FigurativeConstant.Zero, classToken.Span);

                var op2 = isNot ? BinaryOperator.NotEqual : BinaryOperator.Equal;
                return new BinaryExpression(left, op2, classExpr,
                    TextSpan.FromBounds(left.Span.Start, classToken.Span.End));
            }

            // Not a class/sign condition — restore position and try relational
            _position = saved;
        }

        // Check for relational operator
        BinaryOperator? op = TryParseRelationalOperator();
        if (op.HasValue)
        {
            var right = ParseArithmeticExpression();
            var result = new BinaryExpression(left, op.Value, right,
                TextSpan.FromBounds(left.Span.Start, right.Span.End));

            // Check for abbreviated combined relations: A > B AND C → A > B AND A > C
            // If next token is AND/OR followed by something that's NOT a statement keyword,
            // and there's no relational operator after the AND/OR operand, it's abbreviated.
            return result;
        }

        return left;
    }

    private BinaryOperator? TryParseRelationalOperator()
    {
        // = , < , > , <= , >= , NOT = , NOT < , NOT >
        // EQUAL TO, GREATER THAN, LESS THAN, etc.
        // Use lookahead to avoid consuming IS/NOT when no operator follows
        int saved = _position;
        bool negated = false;

        if (Check(TokenKind.IsKeyword))
            Advance(); // skip optional IS

        if (Check(TokenKind.NotKeyword))
        {
            negated = true;
            Advance();
        }

        BinaryOperator? op = null;

        if (Match(TokenKind.Equals) || Match(TokenKind.EqualKeyword))
        {
            Match(TokenKind.ToKeyword); // optional TO
            op = BinaryOperator.Equal;
        }
        else if (Match(TokenKind.LessThan))
        {
            op = BinaryOperator.LessThan;
        }
        else if (Match(TokenKind.GreaterThan))
        {
            op = BinaryOperator.GreaterThan;
        }
        else if (Match(TokenKind.LessThanOrEqual))
        {
            op = BinaryOperator.LessThanOrEqual;
        }
        else if (Match(TokenKind.GreaterThanOrEqual))
        {
            op = BinaryOperator.GreaterThanOrEqual;
        }
        else if (Match(TokenKind.GreaterKeyword))
        {
            Match(TokenKind.ThanKeyword); // optional THAN
            // GREATER THAN OR EQUAL TO — only consume OR if followed by EQUAL
            if (Check(TokenKind.OrKeyword) && Peek().Kind == TokenKind.EqualKeyword)
            {
                Advance(); // OR
                Advance(); // EQUAL
                Match(TokenKind.ToKeyword);
                op = BinaryOperator.GreaterThanOrEqual;
            }
            else
            {
                op = BinaryOperator.GreaterThan;
            }
        }
        else if (Match(TokenKind.LessKeyword))
        {
            Match(TokenKind.ThanKeyword);
            if (Check(TokenKind.OrKeyword) && Peek().Kind == TokenKind.EqualKeyword)
            {
                Advance(); // OR
                Advance(); // EQUAL
                Match(TokenKind.ToKeyword);
                op = BinaryOperator.LessThanOrEqual;
            }
            else
            {
                op = BinaryOperator.LessThan;
            }
        }

        // If we consumed IS/NOT but found no operator, restore position
        if (!op.HasValue && _position != saved)
        {
            _position = saved;
            return null;
        }

        if (op.HasValue && negated)
        {
            op = op.Value switch
            {
                BinaryOperator.Equal => BinaryOperator.NotEqual,
                BinaryOperator.LessThan => BinaryOperator.GreaterThanOrEqual,
                BinaryOperator.GreaterThan => BinaryOperator.LessThanOrEqual,
                BinaryOperator.LessThanOrEqual => BinaryOperator.GreaterThan,
                BinaryOperator.GreaterThanOrEqual => BinaryOperator.LessThan,
                _ => op.Value
            };
        }

        return op;
    }
}
