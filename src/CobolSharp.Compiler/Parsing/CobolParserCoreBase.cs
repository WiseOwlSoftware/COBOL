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
}
