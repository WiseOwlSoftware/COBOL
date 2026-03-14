using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;

namespace CobolSharp.Compiler.Parsing;

public sealed partial class Parser
{
    // ── DISPLAY ──

    private DisplayStatement ParseDisplayStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DISPLAY

        var operands = new List<Expression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.EndDisplayKeyword))
        {
            // Issue 31 (§7.8): UPON clause
            if (Check(TokenKind.Identifier) && Current.Text.Equals("UPON", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // UPON
                if (Check(TokenKind.Identifier)) Advance(); // device-name
                continue;
            }
            // Skip optional WITH NO ADVANCING or NO ADVANCING
            if (Check(TokenKind.WithKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier) && Current.Text.Equals("NO", StringComparison.OrdinalIgnoreCase))
                    Advance();
                Match(TokenKind.AdvancingKeyword);
                continue;
            }
            if (Check(TokenKind.Identifier) && Current.Text.Equals("NO", StringComparison.OrdinalIgnoreCase) &&
                Peek().Kind == TokenKind.AdvancingKeyword)
            {
                Advance(); // NO
                Advance(); // ADVANCING
                continue;
            }
            operands.Add(ParseExpression());
        }
        Match(TokenKind.EndDisplayKeyword);

        return new DisplayStatement(operands, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── STOP RUN ──

    private StopRunStatement ParseStopStatement()
    {
        int start = Current.Span.Start;
        Advance(); // STOP

        // Issue 30 (§7.23): STOP literal (archaic format)
        if (Check(TokenKind.StringLiteral) || Check(TokenKind.IntegerLiteral) ||
            Check(TokenKind.DecimalLiteral))
        {
            Advance(); // consume the literal
            return new StopRunStatement(TextSpan.FromBounds(start, Current.Span.Start));
        }

        Expect(TokenKind.RunKeyword, "Expected RUN after STOP");

        // Issue 29 (§7.23): STOP RUN [WITH {ERROR|NORMAL} STATUS {identifier|literal}]
        if (Check(TokenKind.WithKeyword))
        {
            Advance(); // WITH
            // ERROR or NORMAL
            if (Check(TokenKind.ErrorKeyword))
                Advance();
            else if (Check(TokenKind.Identifier) &&
                     (Current.Text.Equals("NORMAL", StringComparison.OrdinalIgnoreCase) ||
                      Current.Text.Equals("ERROR", StringComparison.OrdinalIgnoreCase)))
                Advance();
            // STATUS
            if (Check(TokenKind.StatusKeyword))
                Advance();
            else if (Check(TokenKind.Identifier) && Current.Text.Equals("STATUS", StringComparison.OrdinalIgnoreCase))
                Advance();
            // identifier or literal
            if (Check(TokenKind.Identifier) || Check(TokenKind.IntegerLiteral) ||
                Check(TokenKind.StringLiteral))
                Advance();
        }

        return new StopRunStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── MOVE ──

    private MoveStatement ParseMoveStatement()
    {
        int start = Current.Span.Start;
        Advance(); // MOVE

        // Issue 27 (§7.16): MOVE CORRESPONDING/CORR — consume the keyword
        // CORRESPONDING flag is consumed but not modeled in AST (semantic difference)
        Match(TokenKind.CorrespondingKeyword);

        var source = ParseExpression();
        Expect(TokenKind.ToKeyword, "Expected TO after MOVE source");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword))
        {
            if (!Check(TokenKind.Identifier))
                break;
            var id = Advance();
            // Handle IN/OF qualification
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance(); // IN or OF
                if (Check(TokenKind.Identifier)) Advance(); // qualifier
            }
            // Handle subscripts: NAME(expr1, expr2, ...)
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                targets.Add(subId);
            }
            else
            {
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        return new MoveStatement(source, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ADD ──

    private AddStatement ParseAddStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ADD

        // Issue 25 (§7.2): ADD CORRESPONDING/CORR — consumed but not modeled
        Match(TokenKind.CorrespondingKeyword);

        var operands = new List<Expression>();
        while (!Check(TokenKind.ToKeyword) && !Check(TokenKind.GivingKeyword) &&
               !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        // Per §7.2: ADD Format 1 has TO, Format 3 (GIVING) may not have TO
        if (!Match(TokenKind.ToKeyword) && !Check(TokenKind.GivingKeyword))
        {
            ReportError("CS0100", "Expected TO or GIVING in ADD statement");
        }

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndAddKeyword))
        {
            if (!Check(TokenKind.Identifier))
                break;
            var id = Advance();
            // Handle IN/OF qualification
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance(); // IN or OF
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                ConsumeRoundedPhrase();
                targets.Add(subId);
            }
            else
            {
                ConsumeRoundedPhrase(); // optional ROUNDED
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        // Optional GIVING
        if (Match(TokenKind.GivingKeyword))
        {
            while (Check(TokenKind.Identifier) && !IsStatementStart(Current.Kind))
            {
                var id = Advance();
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(id);
                    ConsumeRoundedPhrase();
                    targets.Add(subId);
                }
                else
                {
                    ConsumeRoundedPhrase();
                    targets.Add(new IdentifierExpression(id.Text, id.Span));
                }
            }
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndAddKeyword);

        return new AddStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SUBTRACT ──

    private SubtractStatement ParseSubtractStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SUBTRACT

        // Issue 26 (§7.25): SUBTRACT CORRESPONDING/CORR — consumed but not modeled
        Match(TokenKind.CorrespondingKeyword);

        var operands = new List<Expression>();
        while (!Check(TokenKind.FromKeyword) && !Check(TokenKind.Period) && !Check(TokenKind.EndOfFile))
        {
            operands.Add(ParseExpression());
        }

        Expect(TokenKind.FromKeyword, "Expected FROM in SUBTRACT statement");

        var targets = new List<IdentifierExpression>();

        // §7.25 Format 3: SUBTRACT ... FROM literal GIVING identifier
        // The FROM operand can be a literal when GIVING follows
        if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.DecimalLiteral)) &&
            !Check(TokenKind.GivingKeyword))
        {
            operands.Add(ParseExpression());
        }

        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndSubtractKeyword))
        {
            if (!Check(TokenKind.Identifier))
            {
                break;
            }
            var id = Advance();
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(id);
                ConsumeRoundedPhrase();
                targets.Add(subId);
            }
            else
            {
                ConsumeRoundedPhrase();
                targets.Add(new IdentifierExpression(id.Text, id.Span));
            }
        }

        if (Match(TokenKind.GivingKeyword))
        {
            while (Check(TokenKind.Identifier) && !IsStatementStart(Current.Kind))
            {
                var id = Advance();
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(id);
                    ConsumeRoundedPhrase();
                    targets.Add(subId);
                }
                else
                {
                    ConsumeRoundedPhrase();
                    targets.Add(new IdentifierExpression(id.Text, id.Span));
                }
            }
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndSubtractKeyword);

        return new SubtractStatement(operands, targets, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── COMPUTE ──

    private ComputeStatement ParseComputeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // COMPUTE

        // Issue 28: COMPUTE accepts multiple targets: COMPUTE A B C = expr
        // Parse first target (required)
        var targetToken = Expect(TokenKind.Identifier, "Expected target identifier in COMPUTE");
        // Handle IN/OF qualification
        while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier)) Advance();
        }
        IdentifierExpression target;
        if (Check(TokenKind.LeftParen))
        {
            target = ParseSubscriptOrRefMod(targetToken);
        }
        else
        {
            target = new IdentifierExpression(targetToken.Text, targetToken.Span);
        }
        ConsumeRoundedPhrase(); // optional ROUNDED

        // Consume additional targets until = sign (Issue 28)
        while (Check(TokenKind.Identifier) && !Check(TokenKind.Equals))
        {
            Advance(); // additional target identifier
            // Handle IN/OF qualification on additional targets
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                // Skip subscript/refmod on additional targets
                Advance(); // (
                int depth = 1;
                while (depth > 0 && Current.Kind != TokenKind.EndOfFile)
                {
                    if (Check(TokenKind.LeftParen)) depth++;
                    else if (Check(TokenKind.RightParen)) depth--;
                    Advance();
                }
            }
            ConsumeRoundedPhrase(); // optional ROUNDED on additional target
        }

        Expect(TokenKind.Equals, "Expected = in COMPUTE statement");

        var value = ParseArithmeticExpression();

        SkipSizeErrorPhrases();
        Match(TokenKind.EndComputeKeyword);

        return new ComputeStatement(target, value, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── IF ──

    private IfStatement ParseIfStatement()
    {
        int start = Current.Span.Start;
        Advance(); // IF

        var condition = ParseConditionExpression();

        // Optional THEN keyword
        Match(TokenKind.ThenKeyword);

        // Parse then-statements until ELSE, END-IF, or period
        var thenStatements = ParseImperativeStatements(TokenKind.ElseKeyword, TokenKind.EndIfKeyword);

        var elseStatements = new List<Statement>();
        if (Match(TokenKind.ElseKeyword))
        {
            elseStatements = ParseImperativeStatements(TokenKind.EndIfKeyword);
        }

        if (Check(TokenKind.EndIfKeyword))
        {
            Advance();
        }
        // else: period-terminated IF — period consumed by caller

        return new IfStatement(condition, thenStatements, elseStatements,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── PERFORM ──

    private PerformStatement ParsePerformStatement()
    {
        int start = Current.Span.Start;
        Advance(); // PERFORM

        // Parse optional [WITH] TEST BEFORE/AFTER
        bool testAfter = false;
        if (Check(TokenKind.WithKeyword) && Peek().Kind == TokenKind.TestKeyword)
            Advance(); // consume WITH (only when followed by TEST)
        if (Check(TokenKind.TestKeyword))
        {
            Advance(); // TEST
            if (Check(TokenKind.AfterKeyword)) { Advance(); testAfter = true; }
            else Match(TokenKind.BeforeKeyword); // default
        }

        // PERFORM VARYING identifier FROM expr BY expr UNTIL condition
        if (Check(TokenKind.VaryingKeyword))
        {
            return ParsePerformVarying(start, testAfter, null, null);
        }

        // PERFORM UNTIL condition [inline body] END-PERFORM
        if (Check(TokenKind.UntilKeyword))
        {
            Advance();
            // Issue 60 (§7.19): PERFORM UNTIL EXIT — infinite loop
            Expression until;
            if (Check(TokenKind.Identifier) && Current.Text.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                // UNTIL EXIT = infinite loop; use TRUE as a never-true "until" condition
                var exitToken = Advance();
                until = new NumericLiteralExpression(0, exitToken.Span); // FALSE = never exit
            }
            else
            {
                until = ParseConditionExpression();
            }
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, null, until,
                TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
        }

        // PERFORM n TIMES [inline body] END-PERFORM
        if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier)) &&
            Peek().Kind == TokenKind.TimesKeyword)
        {
            var times = ParseExpression();
            Expect(TokenKind.TimesKeyword);
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, times, null,
                TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
        }

        // Out-of-line: PERFORM paragraph-name [THRU paragraph-name] [modifier]
        if (Check(TokenKind.Identifier))
        {
            string name = Advance().Text;
            string? thruName = null;
            if (Match(TokenKind.ThruKeyword))
            {
                var thruToken = Expect(TokenKind.Identifier, "Expected paragraph name after THRU");
                thruName = thruToken.Text;
            }

            // Out-of-line with TIMES
            if ((Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier)) &&
                Peek().Kind == TokenKind.TimesKeyword)
            {
                var times = ParseExpression();
                Expect(TokenKind.TimesKeyword);
                return new PerformStatement(name, new List<Statement>(), times, null,
                    TextSpan.FromBounds(start, Current.Span.Start),
                    thruParagraphName: thruName, testAfter: testAfter);
            }

            // Out-of-line with UNTIL
            if (Check(TokenKind.UntilKeyword))
            {
                Advance();
                var until = ParseConditionExpression();
                return new PerformStatement(name, new List<Statement>(), null, until,
                    TextSpan.FromBounds(start, Current.Span.Start),
                    thruParagraphName: thruName, testAfter: testAfter);
            }

            // Out-of-line with VARYING
            if (Check(TokenKind.VaryingKeyword))
            {
                return ParsePerformVarying(start, testAfter, name, thruName);
            }

            // Out-of-line with TEST BEFORE/AFTER
            if (Check(TokenKind.TestKeyword))
            {
                Advance();
                if (Check(TokenKind.AfterKeyword)) { Advance(); testAfter = true; }
                else Match(TokenKind.BeforeKeyword);

                if (Check(TokenKind.UntilKeyword))
                {
                    Advance();
                    var until = ParseConditionExpression();
                    return new PerformStatement(name, new List<Statement>(), null, until,
                        TextSpan.FromBounds(start, Current.Span.Start),
                        thruParagraphName: thruName, testAfter: testAfter);
                }
                if (Check(TokenKind.VaryingKeyword))
                {
                    return ParsePerformVarying(start, testAfter, name, thruName);
                }
            }

            return new PerformStatement(name, new List<Statement>(), null, null,
                TextSpan.FromBounds(start, Current.Span.Start),
                thruParagraphName: thruName, testAfter: testAfter);
        }

        // Bare inline PERFORM ... END-PERFORM
        var stmts = ParseImperativeStatements(TokenKind.EndPerformKeyword);
        Expect(TokenKind.EndPerformKeyword);
        return new PerformStatement(null, stmts, null, null,
            TextSpan.FromBounds(start, Current.Span.Start), testAfter: testAfter);
    }

    private PerformStatement ParsePerformVarying(int start, bool testAfter,
        string? paraName, string? thruName)
    {
        Advance(); // VARYING

        var varyToken = Expect(TokenKind.Identifier, "Expected identifier after VARYING");
        var varyId = new IdentifierExpression(varyToken.Text, varyToken.Span);

        Expect(TokenKind.FromKeyword, "Expected FROM in PERFORM VARYING");
        var from = ParseExpression();

        Expect(TokenKind.ByKeyword, "Expected BY in PERFORM VARYING");
        var by = ParseExpression();

        Expect(TokenKind.UntilKeyword, "Expected UNTIL in PERFORM VARYING");
        var until = ParseConditionExpression();

        // Per §7.19: consume optional AFTER clauses (nested varying)
        // AFTER id FROM expr BY expr UNTIL cond [AFTER ...]
        while (Check(TokenKind.AfterKeyword))
        {
            Advance(); // AFTER
            if (Check(TokenKind.Identifier)) Advance(); // varying identifier
            if (Check(TokenKind.FromKeyword)) { Advance(); ParseExpression(); } // FROM expr
            if (Check(TokenKind.ByKeyword)) { Advance(); ParseExpression(); } // BY expr
            if (Check(TokenKind.UntilKeyword)) { Advance(); ParseConditionExpression(); } // UNTIL cond
        }

        var varying = new PerformVarying(varyId, from, by,
            TextSpan.FromBounds(varyToken.Span.Start, Current.Span.Start));

        if (paraName != null)
        {
            // Out-of-line PERFORM para VARYING
            return new PerformStatement(paraName, new List<Statement>(), null, until,
                TextSpan.FromBounds(start, Current.Span.Start),
                thruParagraphName: thruName, varying: varying, testAfter: testAfter);
        }
        else
        {
            // Inline PERFORM VARYING ... END-PERFORM
            var body = ParseImperativeStatements(TokenKind.EndPerformKeyword);
            Expect(TokenKind.EndPerformKeyword);
            return new PerformStatement(null, body, null, until,
                TextSpan.FromBounds(start, Current.Span.Start),
                varying: varying, testAfter: testAfter);
        }
    }

    // ── GO TO ──

    private Statement ParseGoToStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GO
        Match(TokenKind.ToKeyword); // optional TO

        var names = new List<string>();
        while (Check(TokenKind.Identifier))
        {
            names.Add(Advance().Text);
        }

        // GO TO ... DEPENDING ON
        if (Check(TokenKind.DependingKeyword))
        {
            Advance();
            Match(TokenKind.OnKeyword);
            var expr = ParseExpression();
            return new GoToDependingStatement(names, expr,
                TextSpan.FromBounds(start, Current.Span.Start));
        }

        // Bare GO TO (no paragraph) is valid — target set by ALTER at runtime
        string? targetName = names.Count > 0 ? names[0] : null;
        return new GoToStatement(targetName, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ALTER ──

    private AlterStatement ParseAlterStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ALTER

        var alterations = new List<(string, string)>();
        while (Check(TokenKind.Identifier))
        {
            string fromPara = Advance().Text;
            Match(TokenKind.ToKeyword); // TO
            // Optional PROCEED TO
            if (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEED", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // PROCEED
                Match(TokenKind.ToKeyword); // TO
            }
            string toPara = Expect(TokenKind.Identifier, "Expected paragraph name after ALTER TO").Text;
            alterations.Add((fromPara, toPara));

            // Comma separator between multiple alterations
            Match(TokenKind.Comma);
        }

        return new AlterStatement(alterations, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CONTINUE ──

    private ContinueStatement ParseContinueStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CONTINUE

        // Issue 68 (§7.7): CONTINUE [AFTER arithmetic-expression SECONDS]
        if (Check(TokenKind.AfterKeyword))
        {
            Advance(); // AFTER
            ParseArithmeticExpression(); // seconds expression
            // SECONDS keyword
            if (Check(TokenKind.Identifier) && Current.Text.Equals("SECONDS", StringComparison.OrdinalIgnoreCase))
                Advance();
        }

        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── EXIT ──

    private ExitStatement ParseExitStatement()
    {
        int start = Current.Span.Start;
        Advance(); // EXIT

        ExitType kind = ExitType.Paragraph; // default
        if (Check(TokenKind.Identifier))
        {
            string word = Current.Text.ToUpperInvariant();
            if (word == "PARAGRAPH") { Advance(); kind = ExitType.Paragraph; }
            else if (word == "SECTION") { Advance(); kind = ExitType.Section; }
            else if (word == "PROGRAM") { Advance(); kind = ExitType.Program; }
            else if (word == "PERFORM")
            {
                Advance(); kind = ExitType.Perform;
                // Issue 34 (§7.11): EXIT PERFORM [CYCLE]
                if (Check(TokenKind.Identifier) && Current.Text.Equals("CYCLE", StringComparison.OrdinalIgnoreCase))
                    Advance();
            }
            // Issue 35 (§7.11): EXIT FUNCTION / EXIT METHOD
            else if (word == "FUNCTION") { Advance(); kind = ExitType.Program; }
            else if (word == "METHOD") { Advance(); kind = ExitType.Program; }
        }
        else if (Check(TokenKind.ProcedureKeyword))
        {
            // EXIT PROGRAM uses PROGRAM which isn't a generic identifier
        }

        return new ExitStatement(kind, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── ACCEPT ──

    private AcceptStatement ParseAcceptStatement()
    {
        int start = Current.Span.Start;
        Advance(); // ACCEPT

        var targetToken = Expect(TokenKind.Identifier, "Expected identifier after ACCEPT");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        string? fromSource = null;
        if (Check(TokenKind.FromKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier))
            {
                fromSource = Advance().Text.ToUpperInvariant();
                // Issue 23 (§7.1): Consume YYYYMMDD/YYYYDDD modifier after DATE/DAY
                if ((fromSource == "DATE" || fromSource == "DAY") && Check(TokenKind.Identifier))
                {
                    string modifier = Current.Text.ToUpperInvariant();
                    if (modifier == "YYYYMMDD" || modifier == "YYYYDDD")
                        Advance();
                }
            }
        }

        // Issue 24 (§7.1): ON EXCEPTION / NOT ON EXCEPTION / END-ACCEPT
        SkipExceptionPhrases(TokenKind.EndAcceptKeyword);
        Match(TokenKind.EndAcceptKeyword);

        return new AcceptStatement(target, fromSource,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INITIALIZE ──

    private InitializeStatement ParseInitializeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INITIALIZE

        var targets = new List<IdentifierExpression>();
        while (Check(TokenKind.Identifier))
        {
            var tok = Advance();
            targets.Add(new IdentifierExpression(tok.Text, tok.Span));
        }

        // Issue 38 (§7.14): [WITH FILLER] [THEN REPLACING {category DATA BY {id|lit}}...]
        //                    [THEN TO DEFAULT]
        if (Check(TokenKind.WithKeyword))
        {
            Advance(); // WITH
            if (Check(TokenKind.FillerKeyword)) Advance(); // FILLER
        }
        // REPLACING clause
        if (Check(TokenKind.Identifier) && Current.Text.Equals("REPLACING", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // REPLACING
            // Loop: category DATA BY value
            while (Check(TokenKind.Identifier) || Check(TokenKind.NumericKeyword) ||
                   Check(TokenKind.AlphabeticKeyword) || Check(TokenKind.NationalKeyword))
            {
                // Category: ALPHABETIC, ALPHANUMERIC, NATIONAL, NUMERIC, etc.
                Advance();
                // Optional DATA keyword
                if (Check(TokenKind.DataKeyword)) Advance();
                // BY
                if (Check(TokenKind.ByKeyword))
                {
                    Advance();
                    ParseExpression(); // replacement value
                }
                else break;
            }
        }
        // THEN TO DEFAULT
        if (Check(TokenKind.ThenKeyword))
        {
            Advance(); // THEN
            if (Check(TokenKind.ToKeyword)) Advance(); // TO
            if (Check(TokenKind.Identifier) && Current.Text.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
                Advance();
        }

        return new InitializeStatement(targets,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── File I/O Statements ──

    private OpenStatement ParseOpenStatement()
    {
        int start = Current.Span.Start;
        Advance(); // OPEN

        var clauses = new List<OpenClause>();
        while (!Check(TokenKind.Period) && Current.Kind != TokenKind.EndOfFile &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
        {
            OpenMode mode;
            if (Match(TokenKind.InputKeyword)) mode = OpenMode.Input;
            else if (Match(TokenKind.OutputKeyword)) mode = OpenMode.Output;
            else if (Check(TokenKind.I_OKeyword)) { Advance(); mode = OpenMode.InputOutput; }
            else if (Match(TokenKind.ExtendKeyword)) mode = OpenMode.Extend;
            else break;

            var fileNames = new List<string>();
            while (Check(TokenKind.Identifier))
            {
                // Issue 41 (§7.18): Skip SHARING clause
                if (Current.Text.Equals("SHARING", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // SHARING
                    if (Check(TokenKind.WithKeyword)) Advance();
                    // ALL OTHER or NO OTHER or ALL
                    while (Check(TokenKind.AllKeyword) || Check(TokenKind.Identifier))
                    {
                        string w = Current.Text.ToUpperInvariant();
                        if (w == "ALL" || w == "OTHER" || w == "NO" || w == "READ" || w == "ONLY")
                            Advance();
                        else break;
                    }
                    continue;
                }
                fileNames.Add(Advance().Text);
                // Issue 41 (§7.18): Skip WITH NO REWIND after file name
                if (Check(TokenKind.WithKeyword))
                {
                    int saved = _position;
                    Advance(); // WITH
                    if (Check(TokenKind.Identifier) && Current.Text.Equals("NO", StringComparison.OrdinalIgnoreCase))
                    {
                        Advance(); // NO
                        if (Check(TokenKind.Identifier) && Current.Text.Equals("REWIND", StringComparison.OrdinalIgnoreCase))
                            Advance(); // REWIND
                    }
                    else
                    {
                        _position = saved; // not WITH NO REWIND, restore
                    }
                }
            }
            clauses.Add(new OpenClause(mode, fileNames));
        }

        return new OpenStatement(clauses, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private CloseStatement ParseCloseStatement()
    {
        // Per §7.4 CLOSE: CLOSE file-name-1 [WITH {LOCK|NO REWIND}] ...
        int start = Current.Span.Start;
        Advance(); // CLOSE
        var fileNames = new List<string>();
        while (Check(TokenKind.Identifier))
        {
            fileNames.Add(Advance().Text);
            Match(TokenKind.Comma);
            // Skip optional WITH LOCK / WITH NO REWIND / REEL / UNIT phrases
            if (Check(TokenKind.WithKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance(); // LOCK or NO
                if (Check(TokenKind.Identifier)) Advance(); // REWIND
            }
            if (Check(TokenKind.Identifier) &&
                (Current.Text.Equals("REEL", StringComparison.OrdinalIgnoreCase) ||
                 Current.Text.Equals("UNIT", StringComparison.OrdinalIgnoreCase)))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
        }
        return new CloseStatement(fileNames, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private ReadStatement ParseReadStatement()
    {
        int start = Current.Span.Start;
        Advance(); // READ

        var fileNameToken = Expect(TokenKind.Identifier, "Expected file name after READ");
        string fileName = fileNameToken.Text;

        // Issue 43 (§7.20): Optional NEXT/PREVIOUS RECORD
        if (Match(TokenKind.NextKeyword))
        { /* consumed NEXT */ }
        else if (Check(TokenKind.Identifier) && Current.Text.Equals("PREVIOUS", StringComparison.OrdinalIgnoreCase))
            Advance();
        if (Check(TokenKind.RecordKeyword)) Advance();

        // INTO identifier
        IdentifierExpression? into = null;
        if (Match(TokenKind.IntoKeyword))
        {
            var intoToken = Expect(TokenKind.Identifier, "Expected identifier after INTO");
            into = new IdentifierExpression(intoToken.Text, intoToken.Span);
        }

        // KEY IS
        Expression? keyIs = null;
        if (Check(TokenKind.KeyKeyword))
        {
            Advance();
            Match(TokenKind.IsKeyword);
            keyIs = ParseExpression();
        }

        // §14.9.30: [AT END imperative-statement-1] [NOT AT END imperative-statement-2]
        //
        // COBOL-85 compatibility: In COBOL-85, END was a valid standalone imperative
        // statement (a no-op). NIST tests write "READ file RECORD END" which is
        // "AT END END" — the AT END clause with END as the minimal imperative.
        // Bare END after READ RECORD is accepted as a COBOL-85 compatibility extension.
        // COBOL-2023 removed standalone END as an imperative.
        var atEnd = new List<Statement>();
        var notAtEnd = new List<Statement>();

        if (Check(TokenKind.AtKeyword))
        {
            Advance(); // AT
            Match(TokenKind.EndKeyword);
            atEnd = ParseImperativeStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }
        else if (Check(TokenKind.EndKeyword) && !Check(TokenKind.EndReadKeyword))
        {
            // COBOL-85 compat: bare END = AT END with END as no-op imperative
            Advance(); // END
            atEnd = ParseImperativeStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }
        if (Check(TokenKind.NotKeyword) &&
            (Peek().Kind == TokenKind.AtKeyword || Peek().Kind == TokenKind.EndKeyword))
        {
            Advance(); // NOT
            if (Check(TokenKind.AtKeyword)) Advance(); // AT (required per 2023, often omitted in 85)
            Match(TokenKind.EndKeyword);
            notAtEnd = ParseImperativeStatements(TokenKind.EndReadKeyword);
        }
        // INVALID KEY / NOT INVALID KEY
        if (Check(TokenKind.InvalidKeyword))
        {
            Advance(); Match(TokenKind.KeyKeyword);
            atEnd = ParseImperativeStatements(TokenKind.NotKeyword, TokenKind.EndReadKeyword);
        }
        // Issue 44 (§7.20): NOT INVALID KEY handler
        if (Check(TokenKind.NotKeyword) && Peek().Kind == TokenKind.InvalidKeyword)
        {
            Advance(); Advance(); // NOT INVALID
            Match(TokenKind.KeyKeyword);
            notAtEnd = ParseImperativeStatements(TokenKind.EndReadKeyword);
        }

        Match(TokenKind.EndReadKeyword);

        return new ReadStatement(fileName, into, atEnd, notAtEnd, keyIs,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    private WriteStatement ParseWriteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // WRITE

        // Issue 45 (§7.27): WRITE {record-name | FILE file-name}
        if (Check(TokenKind.FileKeyword))
            Advance(); // consume FILE keyword prefix

        var recToken = Expect(TokenKind.Identifier, "Expected record name after WRITE");
        var recordName = new IdentifierExpression(recToken.Text, recToken.Span);

        Expression? from = null;
        if (Match(TokenKind.FromKeyword))
            from = ParseExpression();

        WriteAdvancing? advancing = null;
        if (Check(TokenKind.BeforeKeyword) || Check(TokenKind.AfterKeyword))
        {
            bool isBefore = Check(TokenKind.BeforeKeyword);
            Advance();
            Match(TokenKind.AdvancingKeyword);
            Expression? lines = null;
            if (Check(TokenKind.PageKeyword))
            {
                Advance();
            }
            else if (Check(TokenKind.IntegerLiteral) || Check(TokenKind.Identifier))
            {
                lines = ParseExpression();
                if (Check(TokenKind.LineKeyword) || Check(TokenKind.Identifier)) Advance(); // LINES
            }
            advancing = new WriteAdvancing(isBefore, lines);
        }

        // INVALID KEY / NOT INVALID KEY / AT END-OF-PAGE / NOT AT END-OF-PAGE
        SkipExceptionPhrases(TokenKind.EndWriteKeyword);
        Match(TokenKind.EndWriteKeyword);
        return new WriteStatement(recordName, from, advancing,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    private RewriteStatement ParseRewriteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // REWRITE
        // Issue 46 (§7.29): REWRITE {record-name | FILE file-name}
        if (Check(TokenKind.FileKeyword))
            Advance(); // consume FILE keyword prefix
        var recToken = Expect(TokenKind.Identifier, "Expected record name");
        var recordName = new IdentifierExpression(recToken.Text, recToken.Span);
        Expression? from = null;
        if (Match(TokenKind.FromKeyword))
            from = ParseExpression();
        SkipExceptionPhrases(TokenKind.EndRewriteKeyword);
        Match(TokenKind.EndRewriteKeyword);
        return new RewriteStatement(recordName, from, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private DeleteStatement ParseDeleteStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DELETE
        var fileToken = Expect(TokenKind.Identifier, "Expected file name");
        Match(TokenKind.RecordKeyword); // optional RECORD
        SkipExceptionPhrases(TokenKind.EndDeleteKeyword);
        Match(TokenKind.EndDeleteKeyword);
        return new DeleteStatement(fileToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    private StartStatement ParseStartStatement()
    {
        int start = Current.Span.Start;
        Advance(); // START
        var fileToken = Expect(TokenKind.Identifier, "Expected file name");
        BinaryOperator? keyCondition = null;
        Expression? keyIs = null;
        if (Check(TokenKind.KeyKeyword))
        {
            Advance();
            Match(TokenKind.IsKeyword);
            keyCondition = TryParseRelationalOperator();
            keyIs = ParseExpression();
        }
        SkipExceptionPhrases(TokenKind.EndStartKeyword);
        Match(TokenKind.EndStartKeyword);
        return new StartStatement(fileToken.Text, keyCondition, keyIs,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SORT ──

    private SortStatement ParseSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SORT

        var fileToken = Expect(TokenKind.Identifier, "Expected sort file name");
        var keys = new List<SortKey>();

        // ON ASCENDING/DESCENDING KEY
        while (Check(TokenKind.OnKeyword) || Check(TokenKind.AscendingKeyword) || Check(TokenKind.DescendingKeyword))
        {
            Match(TokenKind.OnKeyword);
            bool asc = true;
            if (Match(TokenKind.DescendingKeyword)) asc = false;
            else Match(TokenKind.AscendingKeyword);
            Match(TokenKind.KeyKeyword);
            Match(TokenKind.IsKeyword);
            while (Check(TokenKind.Identifier) &&
                   !Current.Text.Equals("INPUT", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("USING", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("GIVING", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("COLLATING", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("DUPLICATES", StringComparison.OrdinalIgnoreCase))
            {
                keys.Add(new SortKey(asc, Advance().Text));
            }
        }

        // Issue 49 (§7.33): WITH DUPLICATES IN ORDER
        if (Check(TokenKind.WithKeyword))
        {
            Advance(); // WITH
            if (Check(TokenKind.DuplicatesKeyword))
            {
                Advance(); // DUPLICATES
                if (Check(TokenKind.InKeyword)) Advance(); // IN
                if (Check(TokenKind.Identifier) && Current.Text.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
                    Advance(); // ORDER
            }
        }

        // Issue 50 (§7.33): COLLATING SEQUENCE IS alphabet-name
        if (Check(TokenKind.Identifier) && Current.Text.Equals("COLLATING", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // COLLATING
            if (Check(TokenKind.SequentialKeyword) || (Check(TokenKind.Identifier) &&
                Current.Text.Equals("SEQUENCE", StringComparison.OrdinalIgnoreCase)))
                Advance(); // SEQUENCE
            Match(TokenKind.IsKeyword); // IS
            if (Check(TokenKind.Identifier)) Advance(); // alphabet-name
        }

        string? inputProc = null, usingFile = null, outputProc = null, givingFile = null;

        // INPUT PROCEDURE / USING
        if (Check(TokenKind.InputKeyword))
        {
            Advance();
            if (Check(TokenKind.ProcedureKeyword) ||
                (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase)))
            {
                Advance(); // PROCEDURE
                Match(TokenKind.IsKeyword);
                inputProc = Expect(TokenKind.Identifier, "Expected procedure name").Text;
                // Issue 47 (§7.33): THRU in INPUT/OUTPUT PROCEDURE
                if (Match(TokenKind.ThruKeyword))
                {
                    if (Check(TokenKind.Identifier)) Advance(); // end procedure name
                }
            }
        }
        else if (Check(TokenKind.UsingKeyword))
        {
            Advance();
            usingFile = Expect(TokenKind.Identifier, "Expected file name after USING").Text;
            // Issue 48 (§7.33): Multiple USING files
            while (Check(TokenKind.Identifier) &&
                   !Current.Text.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase) &&
                   !Current.Text.Equals("GIVING", StringComparison.OrdinalIgnoreCase))
                Advance();
        }

        // OUTPUT PROCEDURE / GIVING
        if (Check(TokenKind.OutputKeyword))
        {
            Advance();
            if (Check(TokenKind.ProcedureKeyword) ||
                (Check(TokenKind.Identifier) && Current.Text.Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase)))
            {
                Advance(); // PROCEDURE
                Match(TokenKind.IsKeyword);
                outputProc = Expect(TokenKind.Identifier, "Expected procedure name").Text;
                // Issue 47 (§7.33): THRU in INPUT/OUTPUT PROCEDURE
                if (Match(TokenKind.ThruKeyword))
                {
                    if (Check(TokenKind.Identifier)) Advance(); // end procedure name
                }
            }
        }
        else if (Check(TokenKind.GivingKeyword))
        {
            Advance();
            givingFile = Expect(TokenKind.Identifier, "Expected file name after GIVING").Text;
            // Issue 48 (§7.33): Multiple GIVING files
            while (Check(TokenKind.Identifier) &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
                Advance();
        }

        return new SortStatement(fileToken.Text, keys, inputProc, usingFile, outputProc, givingFile,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CALL ──

    private CallStatement ParseCallStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CALL

        // Program name: literal or identifier
        var programName = ParseExpression();

        // USING clause
        var parameters = new List<CallParameter>();
        if (Match(TokenKind.UsingKeyword))
        {
            var convention = CallConvention.ByReference; // default
            while (!Check(TokenKind.Period) && !Check(TokenKind.ReturningKeyword) &&
                   !Check(TokenKind.OnKeyword) && Current.Kind != TokenKind.EndOfFile &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
            {
                // Check for BY REFERENCE/CONTENT/VALUE
                if (Match(TokenKind.ByKeyword))
                {
                    if (Match(TokenKind.ReferenceKeyword))
                        convention = CallConvention.ByReference;
                    else if (Match(TokenKind.ContentKeyword))
                        convention = CallConvention.ByContent;
                    else if (Check(TokenKind.ValueKeyword) || Check(TokenKind.ValuesKeyword))
                    {
                        Advance();
                        convention = CallConvention.ByValue;
                    }
                }

                // Issue 51 (§7.3): Handle OMITTED keyword
                if (Check(TokenKind.Identifier) && Current.Text.Equals("OMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    var omitToken = Advance();
                    var omitExpr = new StringLiteralExpression("OMITTED", omitToken.Span);
                    parameters.Add(new CallParameter(omitExpr, convention));
                    continue;
                }

                var paramExpr = ParseExpression();
                parameters.Add(new CallParameter(paramExpr, convention));
            }
        }

        // RETURNING clause
        IdentifierExpression? returning = null;
        if (Match(TokenKind.ReturningKeyword))
        {
            var retToken = Expect(TokenKind.Identifier, "Expected identifier after RETURNING");
            returning = new IdentifierExpression(retToken.Text, retToken.Span);
        }

        // ON EXCEPTION / NOT ON EXCEPTION
        SkipExceptionPhrases(TokenKind.EndCallKeyword);
        Match(TokenKind.EndCallKeyword);

        return new CallStatement(programName, parameters, returning,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── CANCEL ──

    private CancelStatement ParseCancelStatement()
    {
        int start = Current.Span.Start;
        Advance(); // CANCEL

        // Issue 62 (§7.4): CANCEL accepts multiple operands
        var programName = ParseExpression();
        while (Check(TokenKind.Identifier) || Check(TokenKind.StringLiteral))
        {
            ParseExpression(); // consume additional operands (not modeled in AST)
        }

        return new CancelStatement(programName,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── STRING ──

    private StringStatement ParseStringStatement()
    {
        int start = Current.Span.Start;
        Advance(); // STRING

        var sources = new List<StringSource>();
        while (!Check(TokenKind.IntoKeyword) && Current.Kind != TokenKind.EndOfFile &&
               Current.Kind != TokenKind.Period)
        {
            var value = ParseExpression();
            Expression? delim = null;

            // DELIMITED BY SIZE or DELIMITED BY literal/identifier
            if (Check(TokenKind.Identifier) && Current.Text.Equals("DELIMITED", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // DELIMITED
                Match(TokenKind.ByKeyword); // BY
                if (Check(TokenKind.SizeKeyword))
                {
                    Advance(); // SIZE
                    delim = null; // SIZE means use full value
                }
                else
                {
                    delim = ParseExpression();
                }
            }

            sources.Add(new StringSource(value, delim));
        }

        Expect(TokenKind.IntoKeyword, "Expected INTO in STRING statement");
        var targetToken = Expect(TokenKind.Identifier, "Expected target identifier");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        IdentifierExpression? pointer = null;
        // WITH POINTER
        if (Check(TokenKind.Identifier) && Current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // WITH
            if (Check(TokenKind.PointerKeyword))
            {
                Advance(); // POINTER
                var ptrToken = Expect(TokenKind.Identifier, "Expected pointer identifier");
                pointer = new IdentifierExpression(ptrToken.Text, ptrToken.Span);
            }
        }

        // ON OVERFLOW / NOT ON OVERFLOW
        SkipExceptionPhrases(TokenKind.EndStringKeyword);
        Match(TokenKind.EndStringKeyword);

        return new StringStatement(sources, target, pointer,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── UNSTRING ──

    private UnstringStatement ParseUnstringStatement()
    {
        int start = Current.Span.Start;
        Advance(); // UNSTRING

        var sourceToken = Expect(TokenKind.Identifier, "Expected source identifier");
        var source = new IdentifierExpression(sourceToken.Text, sourceToken.Span);

        Expression? delimiter = null;
        // DELIMITED BY [ALL] delimiter [OR [ALL] delimiter] ...
        if (Check(TokenKind.Identifier) && Current.Text.Equals("DELIMITED", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // DELIMITED
            Match(TokenKind.ByKeyword); // BY
            Match(TokenKind.AllKeyword); // Issue 54: optional ALL
            delimiter = ParseExpression();
            // Issue 54 (§7.26): OR [ALL] delimiter ...
            while (Check(TokenKind.OrKeyword))
            {
                Advance(); // OR
                Match(TokenKind.AllKeyword); // optional ALL
                ParseExpression(); // additional delimiter (consumed, not modeled)
            }
        }

        Expect(TokenKind.IntoKeyword, "Expected INTO in UNSTRING statement");

        var targets = new List<IdentifierExpression>();
        while (Check(TokenKind.Identifier) &&
               !Current.Text.Equals("TALLYING", StringComparison.OrdinalIgnoreCase) &&
               !Current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            var tok = Advance();
            targets.Add(new IdentifierExpression(tok.Text, tok.Span));
            // Issue 55 (§7.26): DELIMITER IN identifier / COUNT IN identifier
            if (Check(TokenKind.Identifier) && Current.Text.Equals("DELIMITER", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // DELIMITER
                Match(TokenKind.InKeyword); // IN
                if (Check(TokenKind.Identifier)) Advance(); // identifier
            }
            if (Check(TokenKind.Identifier) && Current.Text.Equals("COUNT", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // COUNT
                Match(TokenKind.InKeyword); // IN
                if (Check(TokenKind.Identifier)) Advance(); // identifier
            }
        }

        // Issue 56 (§7.26): WITH POINTER identifier
        if (Check(TokenKind.Identifier) && Current.Text.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // WITH
            if (Check(TokenKind.PointerKeyword))
            {
                Advance(); // POINTER
                if (Check(TokenKind.Identifier)) Advance(); // identifier
            }
        }

        IdentifierExpression? tallying = null;
        if (Check(TokenKind.Identifier) && Current.Text.Equals("TALLYING", StringComparison.OrdinalIgnoreCase))
        {
            Advance();
            Match(TokenKind.InKeyword); // optional IN (now a keyword)
            var tallyToken = Expect(TokenKind.Identifier, "Expected tally counter");
            tallying = new IdentifierExpression(tallyToken.Text, tallyToken.Span);
        }

        SkipExceptionPhrases(TokenKind.EndUnstringKeyword);
        Match(TokenKind.EndUnstringKeyword);

        return new UnstringStatement(source, delimiter, targets, tallying,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INSPECT ──

    private InspectStatement ParseInspectStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INSPECT

        var targetToken = Expect(TokenKind.Identifier, "Expected identifier after INSPECT");
        var target = new IdentifierExpression(targetToken.Text, targetToken.Span);

        InspectType inspectKind = InspectType.ReplacingAll;
        Expression? searchFor = null;
        Expression? replaceWith = null;
        IdentifierExpression? tallyCounter = null;

        // Issues 39-40 (§7.15): Full INSPECT parsing with multiple phrases and BEFORE/AFTER

        // TALLYING or REPLACING or CONVERTING
        if (Check(TokenKind.Identifier))
        {
            string verb = Current.Text.ToUpperInvariant();
            if (verb == "TALLYING")
            {
                Advance();
                var counterToken = Expect(TokenKind.Identifier, "Expected counter");
                tallyCounter = new IdentifierExpression(counterToken.Text, counterToken.Span);

                // Parse multiple FOR phrases
                while (Check(TokenKind.Identifier) && Current.Text.Equals("FOR", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // FOR

                    // CHARACTERS or ALL/LEADING search-pattern
                    if (Check(TokenKind.Identifier) && Current.Text.Equals("CHARACTERS", StringComparison.OrdinalIgnoreCase))
                    {
                        Advance();
                        inspectKind = InspectType.TallyingAll;
                    }
                    else
                    {
                        if (Check(TokenKind.AllKeyword)) { Advance(); inspectKind = InspectType.TallyingAll; }
                        else if (Check(TokenKind.Identifier) && Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase))
                        { Advance(); inspectKind = InspectType.TallyingLeading; }
                        searchFor = ParseExpression();
                    }

                    // BEFORE/AFTER INITIAL phrases
                    ConsumeBeforeAfterPhrases();
                }

                // Handle case where FOR is omitted (non-standard but common)
                if (searchFor == null && !Check(TokenKind.Period) && !IsStatementStart(Current.Kind) &&
                    !IsScopeTerminator(Current.Kind))
                {
                    if (Check(TokenKind.AllKeyword)) { Advance(); inspectKind = InspectType.TallyingAll; }
                    else if (Check(TokenKind.Identifier) && Current.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase))
                    { Advance(); inspectKind = InspectType.TallyingLeading; }
                    if (!Check(TokenKind.Period) && !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
                        searchFor = ParseExpression();
                    ConsumeBeforeAfterPhrases();
                }

                // TALLYING ... REPLACING is a combined form
                if (Check(TokenKind.Identifier) && Current.Text.Equals("REPLACING", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // REPLACING
                    ParseInspectReplacingPhrases(ref inspectKind, ref searchFor, ref replaceWith);
                }
            }
            else if (verb == "REPLACING")
            {
                Advance();
                ParseInspectReplacingPhrases(ref inspectKind, ref searchFor, ref replaceWith);
            }
            else if (verb == "CONVERTING")
            {
                Advance();
                inspectKind = InspectType.Converting;
                searchFor = ParseExpression();
                Match(TokenKind.ToKeyword);
                replaceWith = ParseExpression();
                ConsumeBeforeAfterPhrases();
            }
        }

        return new InspectStatement(target, inspectKind, searchFor, replaceWith, tallyCounter,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INITIATE (Phase 5.2) ──

    private InitiateStatement ParseInitiateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INITIATE
        var names = new List<string>();
        while (Check(TokenKind.Identifier))
            names.Add(Advance().Text);
        return new InitiateStatement(names, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── GENERATE (Phase 5.2) ──

    private GenerateStatement ParseGenerateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GENERATE
        var nameToken = Expect(TokenKind.Identifier, "Expected report-group name after GENERATE");
        return new GenerateStatement(nameToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── TERMINATE (Phase 5.2) ──

    private TerminateStatement ParseTerminateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // TERMINATE
        var names = new List<string>();
        while (Check(TokenKind.Identifier))
            names.Add(Advance().Text);
        return new TerminateStatement(names, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── INVOKE (Phase 5.4) ──

    private InvokeStatement ParseInvokeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // INVOKE

        // object-ref: identifier or SELF/SUPER
        var objectRef = ParseExpression();

        // method-name: literal or identifier
        var methodName = ParseExpression();

        // USING clause
        var args = new List<Expression>();
        if (Match(TokenKind.UsingKeyword))
        {
            while (!Check(TokenKind.Period) && !Check(TokenKind.ReturningKeyword) &&
                   Current.Kind != TokenKind.EndOfFile &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind))
            {
                // Issue 66 (§7.39): BY REFERENCE/CONTENT/VALUE
                if (Match(TokenKind.ByKeyword))
                {
                    if (!Match(TokenKind.ReferenceKeyword) && !Match(TokenKind.ContentKeyword))
                    {
                        // Issue 66: Also check for VALUE keyword
                        if (Check(TokenKind.ValueKeyword) || Check(TokenKind.ValuesKeyword))
                            Advance();
                    }
                }
                args.Add(ParseExpression());
            }
        }

        // RETURNING clause
        IdentifierExpression? returning = null;
        if (Match(TokenKind.ReturningKeyword))
        {
            var retToken = Expect(TokenKind.Identifier, "Expected identifier after RETURNING");
            returning = new IdentifierExpression(retToken.Text, retToken.Span);
        }

        return new InvokeStatement(objectRef, methodName, args, returning,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RAISE (Phase 5.5) ──

    private RaiseStatement ParseRaiseStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RAISE

        // Issue 63 (§7.37): RAISE [EXCEPTION] exception-name
        if (Check(TokenKind.Identifier) && Current.Text.Equals("EXCEPTION", StringComparison.OrdinalIgnoreCase))
            Advance(); // consume EXCEPTION keyword prefix

        var nameToken = Expect(TokenKind.Identifier, "Expected exception name after RAISE");
        return new RaiseStatement(nameToken.Text, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RESUME (Phase 5.5) ──

    private ResumeStatement ParseResumeStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RESUME

        // Issue 64 (§7.38): RESUME [AT] NEXT STATEMENT
        // The grammar only allows NEXT STATEMENT, not paragraph-name
        string? atLabel = null;
        Match(TokenKind.AtKeyword); // optional AT
        if (Check(TokenKind.NextKeyword))
        {
            Advance(); // NEXT
            // Consume optional STATEMENT
            if (Check(TokenKind.Identifier) &&
                Current.Text.Equals("STATEMENT", StringComparison.OrdinalIgnoreCase))
                Advance();
        }

        return new ResumeStatement(atLabel, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── >>SOURCE FORMAT (Phase 5.6-5.10) ──

    private SourceFormatDirective ParseCompilerDirective()
    {
        int start = Current.Span.Start;
        string directiveText = Current.Value is string s ? s : Current.Text;
        Advance(); // CompilerDirective token

        // Parse >>SOURCE FORMAT IS FREE | FIXED from the directive text
        bool isFree = directiveText.Contains("FREE", StringComparison.OrdinalIgnoreCase);

        return new SourceFormatDirective(isFree, TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── EVALUATE ──

    private EvaluateStatement ParseEvaluateStatement()
    {
        int start = Current.Span.Start;
        Advance(); // EVALUATE

        // Subject can be: identifier, literal, expression, TRUE, FALSE, condition
        var subject = ParseConditionExpression();
        // Issue 32 (§7.10): ALSO subject2 [ALSO subject3] ...
        while (Check(TokenKind.Identifier) && Current.Text.Equals("ALSO", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // ALSO
            ParseConditionExpression(); // additional subject (consumed, not modeled)
        }

        var whenClauses = new List<WhenClause>();
        var whenOtherStatements = new List<Statement>();

        while (Check(TokenKind.WhenKeyword))
        {
            int whenStart = Current.Span.Start;
            Advance(); // WHEN

            // WHEN OTHER
            if (Check(TokenKind.OtherKeyword))
            {
                Advance(); // OTHER
                whenOtherStatements = ParseImperativeStatements(TokenKind.EndEvaluateKeyword);
                break;
            }

            // Parse WHEN objects. Per §7.10, objects can be:
            // value-1 [THRU value-2] | TRUE | FALSE | ANY | condition | partial-expression

            var objects = new List<Expression>();

            // Issue 33 (§7.10): Check for partial-expression (relational operator + operand)
            var partialOp = TryParseRelationalOperator();
            if (partialOp.HasValue)
            {
                var partialRight = ParseArithmeticExpression();
                // Synthesize comparison with subject: subject op operand
                var whenObj = new BinaryExpression(subject, partialOp.Value, partialRight,
                    TextSpan.FromBounds(partialRight.Span.Start, partialRight.Span.End));
                objects.Add(whenObj);
            }
            else
            {
                // Check for ANY keyword
                if (Check(TokenKind.Identifier) && Current.Text.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    var anyToken = Advance();
                    objects.Add(new StringLiteralExpression("ANY", anyToken.Span));
                }
                else
                {
                    var whenObj = ParseExpression();
                    // Handle THRU/THROUGH for range: WHEN 1 THRU 10
                    if (Check(TokenKind.ThruKeyword))
                    {
                        Advance(); // THRU
                        ParseExpression(); // consume range end (not modeled yet)
                    }
                    objects.Add(whenObj);
                }
            }

            // Issue 32 (§7.10): ALSO selection-object per WHEN
            while (Check(TokenKind.Identifier) && Current.Text.Equals("ALSO", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // ALSO
                // Parse ALSO object (partial-expression or value)
                var alsoOp = TryParseRelationalOperator();
                if (alsoOp.HasValue)
                {
                    ParseArithmeticExpression(); // consume (not modeled)
                }
                else if (Check(TokenKind.Identifier) && Current.Text.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // ANY
                }
                else
                {
                    ParseExpression();
                    if (Check(TokenKind.ThruKeyword)) { Advance(); ParseExpression(); }
                }
            }

            // Handle additional WHEN clauses that share the same statement block
            // (WHEN value1 WHEN value2 ... statements)
            while (Check(TokenKind.WhenKeyword) && Peek().Kind != TokenKind.OtherKeyword)
            {
                Advance(); // WHEN
                // Issue 33: Check for partial-expression in subsequent WHENs
                var nextPartialOp = TryParseRelationalOperator();
                if (nextPartialOp.HasValue)
                {
                    var nextPartialRight = ParseArithmeticExpression();
                    var nextObj = new BinaryExpression(subject, nextPartialOp.Value, nextPartialRight,
                        TextSpan.FromBounds(nextPartialRight.Span.Start, nextPartialRight.Span.End));
                    objects.Add(nextObj);
                }
                else if (Check(TokenKind.Identifier) && Current.Text.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                {
                    var anyToken = Advance();
                    objects.Add(new StringLiteralExpression("ANY", anyToken.Span));
                }
                else
                {
                    var nextObj = ParseExpression();
                    if (Check(TokenKind.ThruKeyword))
                    {
                        Advance();
                        ParseExpression(); // consume range end
                    }
                    objects.Add(nextObj);
                }
                // ALSO in subsequent WHENs
                while (Check(TokenKind.Identifier) && Current.Text.Equals("ALSO", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // ALSO
                    var alsoOp = TryParseRelationalOperator();
                    if (alsoOp.HasValue)
                        ParseArithmeticExpression();
                    else if (Check(TokenKind.Identifier) && Current.Text.Equals("ANY", StringComparison.OrdinalIgnoreCase))
                        Advance();
                    else
                    {
                        ParseExpression();
                        if (Check(TokenKind.ThruKeyword)) { Advance(); ParseExpression(); }
                    }
                }
            }

            // Parse statements for this WHEN clause
            var stmts = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndEvaluateKeyword);
            whenClauses.Add(new WhenClause(objects, stmts,
                TextSpan.FromBounds(whenStart, Current.Span.Start)));
        }

        Match(TokenKind.EndEvaluateKeyword);

        return new EvaluateStatement(subject, whenClauses, whenOtherStatements,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── MULTIPLY ──

    private MultiplyStatement ParseMultiplyStatement()
    {
        int start = Current.Span.Start;
        Advance(); // MULTIPLY

        var operand = ParseExpression();
        Expect(TokenKind.ByKeyword, "Expected BY in MULTIPLY statement");

        var targets = new List<IdentifierExpression>();
        while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
               !Check(TokenKind.GivingKeyword) &&
               !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
               !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
               !Check(TokenKind.EndMultiplyKeyword))
        {
            if (!Check(TokenKind.Identifier))
            {
                ReportError("CS0100", "Expected identifier in MULTIPLY");
                Advance();
                break;
            }
            var tok = Advance();
            while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
            {
                Advance();
                if (Check(TokenKind.Identifier)) Advance();
            }
            if (Check(TokenKind.LeftParen))
            {
                var subId = ParseSubscriptOrRefMod(tok);
                ConsumeRoundedPhrase();
                targets.Add(subId);
            }
            else
            {
                ConsumeRoundedPhrase(); // optional ROUNDED
                targets.Add(new IdentifierExpression(tok.Text, tok.Span));
            }
        }

        Expression? giving = null;
        if (Match(TokenKind.GivingKeyword))
        {
            giving = ParseExpression();
            ConsumeRoundedPhrase();
        }

        // Skip ON SIZE ERROR / NOT ON SIZE ERROR
        SkipSizeErrorPhrases();
        Match(TokenKind.EndMultiplyKeyword);

        return new MultiplyStatement(operand, targets, giving,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── DIVIDE ──

    private DivideStatement ParseDivideStatement()
    {
        int start = Current.Span.Start;
        Advance(); // DIVIDE

        var operand = ParseExpression();

        // DIVIDE x INTO y  or  DIVIDE x BY y GIVING z
        bool hasInto = Match(TokenKind.IntoKeyword);
        bool hasBy = !hasInto && Match(TokenKind.ByKeyword);

        var targets = new List<IdentifierExpression>();
        Expression? giving = null;
        IdentifierExpression? remainder = null;

        if (hasBy)
        {
            // DIVIDE x BY y GIVING z [REMAINDER r]
            var divisor = ParseExpression();
            // For now, store the divisor as a target — semantic analysis resolves
            targets.Add(new IdentifierExpression(
                divisor is IdentifierExpression id ? id.Name : "?",
                divisor.Span));
            Expect(TokenKind.GivingKeyword, "Expected GIVING in DIVIDE ... BY");
            giving = ParseExpression();
            ConsumeRoundedPhrase();
        }
        else
        {
            // DIVIDE x INTO y [GIVING z] [REMAINDER r]
            while (!Check(TokenKind.Period) && !Check(TokenKind.EndOfFile) &&
                   !Check(TokenKind.GivingKeyword) && !Check(TokenKind.RemainderKeyword) &&
                   !IsStatementStart(Current.Kind) && !IsScopeTerminator(Current.Kind) &&
                   !Check(TokenKind.OnKeyword) && !Check(TokenKind.NotKeyword) &&
                   !Check(TokenKind.EndDivideKeyword))
            {
                if (!Check(TokenKind.Identifier))
                {
                    ReportError("CS0100", "Expected identifier in DIVIDE");
                    Advance();
                    break;
                }
                var tok = Advance();
                while (Check(TokenKind.InKeyword) || Check(TokenKind.OfKeyword))
                {
                    Advance();
                    if (Check(TokenKind.Identifier)) Advance();
                }
                if (Check(TokenKind.LeftParen))
                {
                    var subId = ParseSubscriptOrRefMod(tok);
                    ConsumeRoundedPhrase();
                    targets.Add(subId);
                }
                else
                {
                    ConsumeRoundedPhrase();
                    targets.Add(new IdentifierExpression(tok.Text, tok.Span));
                }
            }

            if (Match(TokenKind.GivingKeyword))
            {
                giving = ParseExpression();
                ConsumeRoundedPhrase();
            }
        }

        if (Match(TokenKind.RemainderKeyword))
        {
            var remToken = Expect(TokenKind.Identifier, "Expected identifier after REMAINDER");
            remainder = new IdentifierExpression(remToken.Text, remToken.Span);
        }

        SkipSizeErrorPhrases();
        Match(TokenKind.EndDivideKeyword);

        return new DivideStatement(operand, targets, giving, remainder,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SET ──

    private SetStatement ParseSetStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SET

        var targets = new List<IdentifierExpression>();

        // §7.22: SET {identifier|index-name|ADDRESS OF id}... {TO|UP BY|DOWN BY} value
        // Stop consuming targets at TO keyword, and at UP/DOWN when followed by BY
        // (per grammar Format 2: UP BY and DOWN BY are action phrases, not target names)
        while (Check(TokenKind.Identifier) && !Check(TokenKind.ToKeyword))
        {
            string upper = Current.Text.ToUpperInvariant();
            // UP BY / DOWN BY = Format 2 action (check lookahead for BY)
            if ((upper == "UP" || upper == "DOWN") && Peek().Kind == TokenKind.ByKeyword)
                break;
            // Check for ADDRESS OF construct
            if (upper == "ADDRESS")
            {
                Advance(); // ADDRESS
                Match(TokenKind.OfKeyword); // OF
                if (Check(TokenKind.Identifier))
                {
                    var tok = Advance();
                    targets.Add(new IdentifierExpression(tok.Text, tok.Span));
                }
            }
            else
            {
                var tok = Advance();
                targets.Add(new IdentifierExpression(tok.Text, tok.Span));
            }
        }

        SetAction action = SetAction.To;
        if (Match(TokenKind.ToKeyword))
        {
            action = SetAction.To;
        }
        else if (Check(TokenKind.Identifier) && Current.Text.Equals("UP", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // UP
            Match(TokenKind.ByKeyword);
            action = SetAction.UpBy;
        }
        else if (Check(TokenKind.Identifier) && Current.Text.Equals("DOWN", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // DOWN
            Match(TokenKind.ByKeyword);
            action = SetAction.DownBy;
        }

        var value = ParseExpression();

        return new SetStatement(targets, value, action,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── SEARCH ──

    private SearchStatement ParseSearchStatement()
    {
        int start = Current.Span.Start;
        Advance(); // SEARCH

        // SEARCH ALL identifier — binary search form
        bool isAll = Match(TokenKind.AllKeyword);

        var tableToken = Expect(TokenKind.Identifier, "Expected table name after SEARCH");
        var tableName = new IdentifierExpression(tableToken.Text, tableToken.Span);

        // Optional VARYING identifier
        if (Check(TokenKind.VaryingKeyword))
        {
            Advance();
            if (Check(TokenKind.Identifier)) Advance(); // varying identifier
        }

        // §14.9.37: [AT END imperative-statement-1]
        // COBOL-85 compat: bare END accepted (see READ comment for rationale)
        var atEnd = new List<Statement>();
        if (Check(TokenKind.AtKeyword))
        {
            Advance(); // AT
            Match(TokenKind.EndKeyword);
            atEnd = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndSearchKeyword);
        }
        else if (Check(TokenKind.EndKeyword))
        {
            // COBOL-85 compat: bare END = AT END with END as no-op imperative
            Advance();
            atEnd = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndSearchKeyword);
        }

        var whenClauses = new List<SearchWhenClause>();
        while (Check(TokenKind.WhenKeyword))
        {
            int whenStart = Current.Span.Start;
            Advance(); // WHEN
            var condition = ParseConditionExpression();
            var stmts = ParseImperativeStatements(TokenKind.WhenKeyword, TokenKind.EndSearchKeyword);
            whenClauses.Add(new SearchWhenClause(condition, stmts,
                TextSpan.FromBounds(whenStart, Current.Span.Start)));
        }

        Match(TokenKind.EndSearchKeyword);

        return new SearchStatement(tableName, whenClauses, atEnd,
            TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── GOBACK ──

    private GobackStatement ParseGobackStatement()
    {
        int start = Current.Span.Start;
        Advance(); // GOBACK

        // Issue 67 (§7.52): GOBACK [RAISING {EXCEPTION exception-name | identifier | LAST EXCEPTION}]
        if (Check(TokenKind.Identifier) && Current.Text.Equals("RAISING", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // RAISING
            if (Check(TokenKind.Identifier) && Current.Text.Equals("EXCEPTION", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // EXCEPTION
                if (Check(TokenKind.Identifier)) Advance(); // exception-name
            }
            else if (Check(TokenKind.Identifier) && Current.Text.Equals("LAST", StringComparison.OrdinalIgnoreCase))
            {
                Advance(); // LAST
                if (Check(TokenKind.Identifier) && Current.Text.Equals("EXCEPTION", StringComparison.OrdinalIgnoreCase))
                    Advance(); // EXCEPTION
            }
            else if (Check(TokenKind.Identifier))
            {
                Advance(); // identifier
            }
        }

        return new GobackStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── NEXT SENTENCE ──

    /// <summary>
    /// Issue 37 (§7.13): NEXT SENTENCE is NOT equivalent to CONTINUE.
    /// NEXT SENTENCE jumps past the next period, ignoring scope terminators.
    /// For now we emit it as ContinueStatement (no-op) which is semantically
    /// wrong but syntactically correct — fixing the runtime behavior requires
    /// emitter changes.
    /// </summary>
    private ContinueStatement ParseNextSentenceStatement()
    {
        int start = Current.Span.Start;
        Advance(); // NEXT
        // Consume SENTENCE
        if (Check(TokenKind.Identifier) && Current.Text.Equals("SENTENCE", StringComparison.OrdinalIgnoreCase))
            Advance();
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RETURN (sort output procedure) ──

    private Statement ParseReturnSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RETURN
        if (Check(TokenKind.Identifier)) Advance(); // file-name
        if (Check(TokenKind.RecordKeyword)) Advance(); // optional RECORD
        // INTO identifier
        IdentifierExpression? into = null;
        if (Match(TokenKind.IntoKeyword))
        {
            var intoToken = Expect(TokenKind.Identifier, "Expected identifier after INTO");
            into = new IdentifierExpression(intoToken.Text, intoToken.Span);
        }
        // AT END / NOT AT END
        SkipExceptionPhrases(TokenKind.EndReturnKeyword);
        Match(TokenKind.EndReturnKeyword);
        // Use ContinueStatement as placeholder — no AST node exists
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }

    // ── RELEASE (sort input procedure) ──

    private Statement ParseReleaseSortStatement()
    {
        int start = Current.Span.Start;
        Advance(); // RELEASE
        if (Check(TokenKind.Identifier)) Advance(); // record-name
        if (Match(TokenKind.FromKeyword))
            ParseExpression(); // discard
        return new ContinueStatement(TextSpan.FromBounds(start, Current.Span.Start));
    }
}
