// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Discriminates the kind of symbol in the COBOL semantic model.
/// </summary>
public enum SymbolKind
{
    /// <summary>A COBOL PROGRAM-ID declaration.</summary>
    Program,
    /// <summary>A PROCEDURE DIVISION SECTION header (groups paragraphs).</summary>
    Section,
    /// <summary>A PROCEDURE DIVISION paragraph (labeled statement block).</summary>
    Paragraph,
    /// <summary>A DATA DIVISION item declared at any level (01-49, 66, 77, 88).</summary>
    Data,
    /// <summary>A level-88 condition-name attached to a data item's VALUE clause.</summary>
    Condition88,
    /// <summary>An FD/SD file descriptor in the FILE SECTION.</summary>
    File,
    /// <summary>A resolved type derived from PIC/USAGE analysis.</summary>
    Type,
    /// <summary>A USING parameter in the PROCEDURE DIVISION header.</summary>
    Parameter,
    /// <summary>An INDEXED BY index-name on an OCCURS clause.</summary>
    Index,
    /// <summary>A CLASS-ID declaration (reserved for OO COBOL support).</summary>
    Class,
    /// <summary>A METHOD-ID declaration (reserved for OO COBOL support).</summary>
    Method,
    /// <summary>A compiler-generated local variable (e.g., temporaries for expressions).</summary>
    Local,
}

/// <summary>
/// Base class for all named entities in the COBOL semantic model.
/// Each symbol has a unique name within its declaring scope.
/// </summary>
public abstract class Symbol
{
    /// <summary>The canonical (possibly compiler-adjusted) name used for binding and code generation.</summary>
    public string Name { get; }
    /// <summary>Discriminates the symbol's role in the COBOL program structure.</summary>
    public SymbolKind Kind { get; }
    /// <summary>The scope in which this symbol was declared; set during declaration, null before.</summary>
    public Scope? DeclaringScope { get; internal set; }
    /// <summary>Source line number where this symbol was declared (1-based).</summary>
    public int Line { get; }
    /// <summary>
    /// False if this symbol was rejected during declaration (e.g., duplicate name).
    /// Invalid symbols remain in diagnostic tracking but are excluded from resolution.
    /// </summary>
    public bool IsValid { get; internal set; } = true;

    protected Symbol(string name, SymbolKind kind, int line)
    {
        Name = name;
        Kind = kind;
        Line = line;
    }
}
