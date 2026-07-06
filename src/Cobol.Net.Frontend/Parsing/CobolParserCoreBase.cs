// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

namespace CobolSharp.Compiler.Generated;

/// <summary>
/// Base class for the ANTLR-generated CobolParserCore.
/// Provides semantic predicates for paragraph detection.
/// </summary>
public abstract class CobolParserCoreBase : Parser
{
    /// <summary>
    /// Dialect level for gating non-COBOL-85 features.
    /// Default is COBOL-85 (strict). Set higher to enable later standards.
    /// </summary>
    public int DialectLevel { get; set; } = 85;

    protected bool is85()   => DialectLevel >= 85;
    protected bool is2002() => DialectLevel >= 2002;
    protected bool is2014() => DialectLevel >= 2014;
    protected bool is2023() => DialectLevel >= 2023;

    protected CobolParserCoreBase(ITokenStream input) : base(input) { }
    protected CobolParserCoreBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
        : base(input, output, errorOutput) { }

    /// <summary>
    /// Returns true if the current token is the first non-whitespace token on its line.
    /// Used to prevent stray identifiers (like LINES after WRITE ADVANCING)
    /// from being misinterpreted as paragraph names.
    /// </summary>
    protected bool IsAtLineStart()
    {
        var token = CurrentToken;
        if (token == null) return false;

        // Check if this token's column is 0, or if the previous token
        // is on a different line
        int tokenLine = token.Line;
        int tokenIndex = token.TokenIndex;

        if (tokenIndex <= 0) return true;

        var prevToken = TokenStream.Get(tokenIndex - 1);
        return prevToken.Line < tokenLine;
    }

    /// <summary>
    /// Predicate for the bare (adjective-less) INSPECT TALLYING count phrase. An ALL or
    /// LEADING adjective is transitive across the operands that follow it (ISO 1989:1985
    /// 14.9.22 GR 10), so "FOR LEADING ""S"" ""S"" ""T""" lists three operands under one
    /// counter. But a data-name immediately followed by FOR is the NEXT tallying counter,
    /// not a transitive operand. Returning false there stops the count-phrase repetition so
    /// the data-name begins a new inspectTallyingItem instead of being swallowed as a pattern.
    /// </summary>
    protected bool IsBareInspectOperand() => TokenStream.LA(2) != CobolLexer.FOR;

    /// <summary>
    /// COBOL-2002 boolean-condition discriminator (ISO §8.8.4.2.2 / §8.8.4.3): true when a boolean OPERATOR
    /// (B-AND / B-OR / B-XOR / B-NOT) appears in the CURRENT condition ahead of the parse position, before any
    /// condition boundary. This gates a dedicated <c>primaryCondition</c> alternative WITHOUT touching the
    /// shared <c>comparisonExpression</c> rule (whose modification regressed subscript/ref-mod comparisons at
    /// 2002+, DEVLOG 621) — a normal comparison (no B-op ahead) returns false and falls to comparisonExpression
    /// unchanged. The scan stops at the condition's end: a period, the logical connectives (AND/OR/THEN/ELSE),
    /// a WHEN / END-* / UNTIL / VARYING, or any statement-starting keyword (so it never crosses into an IF body),
    /// and is window-capped. Read-only over the token stream — safe for ANTLR's repeated prediction calls.
    /// </summary>
    protected bool boolExprAhead()
    {
        if (DialectLevel < 2002) return false;
        for (int i = 1; i <= 96; i++)
        {
            switch (TokenStream.LA(i))
            {
                case CobolLexer.B_AND:
                case CobolLexer.B_OR:
                case CobolLexer.B_XOR:
                case CobolLexer.B_NOT:
                    return true;
                // ── Condition boundaries: no B-operator can belong to THIS condition past here ──
                case CobolLexer.DOT:
                case CobolLexer.AND:
                case CobolLexer.OR:
                case CobolLexer.THEN:
                case CobolLexer.ELSE:
                case CobolLexer.WHEN:
                case CobolLexer.END_IF:
                case CobolLexer.END_PERFORM:
                case CobolLexer.END_EVALUATE:
                case CobolLexer.END_SEARCH:
                case CobolLexer.UNTIL:
                case CobolLexer.VARYING:
                case CobolLexer.TIMES:
                case TokenConstants.EOF:
                // Statement-starting keywords — the condition ends where the IF/WHEN body begins, so the scan
                // must never cross into a body that might itself contain a boolean COMPUTE.
                case CobolLexer.ACCEPT: case CobolLexer.ADD: case CobolLexer.ALLOCATE: case CobolLexer.CALL:
                case CobolLexer.CANCEL: case CobolLexer.CLOSE: case CobolLexer.COMPUTE: case CobolLexer.CONTINUE:
                case CobolLexer.DELETE: case CobolLexer.DISPLAY: case CobolLexer.DIVIDE: case CobolLexer.EVALUATE:
                case CobolLexer.EXIT: case CobolLexer.FREE: case CobolLexer.GOBACK: case CobolLexer.GO:
                case CobolLexer.INITIALIZE: case CobolLexer.INSPECT: case CobolLexer.INVOKE: case CobolLexer.MERGE:
                case CobolLexer.MOVE: case CobolLexer.MULTIPLY: case CobolLexer.NEXT: case CobolLexer.OPEN:
                case CobolLexer.PERFORM: case CobolLexer.RAISE: case CobolLexer.READ: case CobolLexer.RELEASE:
                case CobolLexer.RESUME: case CobolLexer.RETURN: case CobolLexer.REWRITE: case CobolLexer.SEARCH:
                case CobolLexer.SET: case CobolLexer.SORT: case CobolLexer.START: case CobolLexer.STOP:
                case CobolLexer.STRING: case CobolLexer.SUBTRACT: case CobolLexer.UNSTRING: case CobolLexer.WRITE:
                case CobolLexer.IF:
                    return false;
            }
        }
        return false;
    }
}
