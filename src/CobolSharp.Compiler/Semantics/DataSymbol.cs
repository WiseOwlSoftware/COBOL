// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.CodeGen;
namespace CobolSharp.Compiler.Semantics;

public sealed class DataSymbol : Symbol
{
    public int LevelNumber { get; }
    public string? PicString { get; }
    public UsageKind Usage { get; }
    public ITypeSymbol? ResolvedType { get; set; }
    public string? InitialValue { get; set; }
    public string? TypeName { get; }

    /// <summary>Original COBOL name (e.g., "FILLER"). Internal Name may differ for uniqueness.</summary>
    public string DisplayName { get; }
    public bool IsFiller { get; }
    public bool IsGroup => PicString == null;
    public bool IsElementary => PicString != null;
    public CodeGen.StorageAreaKind Area { get; set; }

    /// <summary>
    /// Sign storage from explicit SIGN clause. Null means no explicit clause
    /// (default is trailing overpunch for signed DISPLAY fields).
    /// </summary>
    public SignStorageKind? ExplicitSignStorage { get; set; }

    public DataSymbol? Redefines { get; set; }

    /// <summary>Unresolved REDEFINES target name (for deferred resolution).</summary>
    public string? RedefinesName { get; set; }
    public DataSymbol? Parent { get; private set; }
    public List<DataSymbol> Children { get; } = new();

    public DataSymbol(
        string internalName,
        string displayName,
        int levelNumber,
        string? picString,
        UsageKind usage,
        string? typeName,
        DataSymbol? redefines,
        int line)
        : base(internalName, SymbolKind.Data, line)
    {
        DisplayName = displayName;
        IsFiller = string.Equals(displayName, "FILLER", StringComparison.OrdinalIgnoreCase);
        LevelNumber = levelNumber;
        PicString = picString;
        Usage = usage;
        TypeName = typeName;
        Redefines = redefines;
    }

    internal void AddChild(DataSymbol child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
