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
}
