// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

public enum SymbolKind
{
    Program,
    Section,
    Paragraph,
    Data,
    Condition88,
    File,
    Type,
    Parameter,
    Index,
    Class,
    Method,
    Local,
}

public abstract class Symbol
{
    public string Name { get; }
    public SymbolKind Kind { get; }
    public Scope? DeclaringScope { get; internal set; }
    public int Line { get; }

    protected Symbol(string name, SymbolKind kind, int line)
    {
        Name = name;
        Kind = kind;
        Line = line;
    }
}
