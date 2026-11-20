// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// What a stretch of source text IS to the compiler that read it (DESIGN-listing.md section 7.2; kb/Work PB5949): the classes the
/// source listing colours the text by. Each is decided where the compiler decides it, never by a pattern over the text: the
/// reference-format areas and the kind of each line by the reference format (<see cref="Preprocessor.ReferenceFormatMap"/>), a
/// word or literal by the lexer's token and the parse that read it, at the edition in force. The character-strings are the
/// standard's (§2023:8.3.1: "forms a COBOL word, a literal, or a picture character-string"; §1985:IV.4.2.2 adds the
/// comment-entry), and the COBOL words its categories (§2023:8.3.2.1).
/// </summary>
public enum SourceTextClass
{
    /// <summary>The sequence number area of a fixed-form line, positions 1 to 6 (§2023:6.3.1).</summary>
    SequenceArea,
    /// <summary>The indicator area of a fixed-form line, position 7, when it holds an indicator (§2023:6.3.1).</summary>
    Indicator,
    /// <summary>The text of a fixed-form line past margin R, which the compiler ignores (the identification area).</summary>
    IdentificationArea,
    /// <summary>A comment: the text of a comment line, an inline comment, or a debugging line read as a comment.</summary>
    Comment,
    /// <summary>A comment-entry of an identification division paragraph (§1985:IV.4.2.2).</summary>
    CommentEntry,
    /// <summary>A compiler directive line, which the text stages read and the lexer never sees.</summary>
    Directive,
    /// <summary>A reserved word of the edition in force (§2023:8.9, with the compilation group's COBOL-WORDS directives).</summary>
    ReservedWord,
    /// <summary>A word the compiler read as a keyword that the edition in force does not reserve: a context-sensitive word in its
    /// context (§2023:8.10), a word a general format recognizes by its spelling, or a later edition's keyword.</summary>
    ContextWord,
    /// <summary>An intrinsic-function-name or the function-name a FUNCTION reference names.</summary>
    FunctionName,
    /// <summary>A user-defined word, or a system-name: a word the compiler read as a name.</summary>
    UserWord,
    /// <summary>A level-number (§2023:8.3.2.2.13).</summary>
    LevelNumber,
    /// <summary>An alphanumeric literal, hexadecimal ones included.</summary>
    AlphanumericLiteral,
    /// <summary>A national literal.</summary>
    NationalLiteral,
    /// <summary>A boolean literal.</summary>
    BooleanLiteral,
    /// <summary>A numeric literal (a fixed-point or floating-point one, signed or not).</summary>
    NumericLiteral,
    /// <summary>A figurative constant (§2023:8.3.3.6): ZERO, SPACE, HIGH-VALUE, LOW-VALUE, QUOTE, and the ALL that prefixes one.</summary>
    FigurativeConstant,
    /// <summary>A picture character-string (§2023:8.3.4).</summary>
    Picture,
    /// <summary>A separator other than the space (§2023:8.3.5): the period, comma, semicolon, parentheses and colon.</summary>
    Separator,
    /// <summary>An arithmetic, relational, concatenation or invocation operator.</summary>
    Operator,
    /// <summary>The text of an embedded precompiler block (EXEC ... END-EXEC), another language.</summary>
    EmbeddedText,
    /// <summary>A character no lexical rule reads, which the compiler reports.</summary>
    Unrecognized,
}

/// <summary>One classified stretch of the RESULTANT text the lexer read (DESIGN-listing.md section 7.2): <paramref name="Length"/>
/// UTF-16 units from 0-based <paramref name="Column"/> of 1-based resultant <paramref name="Line"/> (a token's own coordinates),
/// which the source listing maps to the physical lines through the line map. A stretch may run past its line's end into the next
/// line of the text (a token the lexer read across a line end).</summary>
/// <param name="Line">The 1-based resultant line of its first character.</param>
/// <param name="Column">The 0-based resultant column of its first character.</param>
/// <param name="Length">Its length in UTF-16 units of the resultant text.</param>
/// <param name="Class">What it is.</param>
public readonly record struct SourceTextSpan(int Line, int Column, int Length, SourceTextClass Class);
