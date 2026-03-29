// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Structured OCCURS clause information: min/max, DEPENDING ON, KEY, INDEXED BY.
/// </summary>
public sealed class OccursInfo
{
    /// <summary>Minimum occurrence count (for OCCURS m TO n DEPENDING ON).</summary>
    public int MinOccurs { get; }

    /// <summary>Maximum occurrence count (fixed OCCURS n, or max in DEPENDING ON).</summary>
    public int MaxOccurs { get; }

    /// <summary>DEPENDING ON data-name (unresolved text; resolved via ResolveDeferred).</summary>
    public string? DependingOnName { get; }

    /// <summary>Resolved DEPENDING ON symbol (set during deferred resolution).</summary>
    public DataSymbol? DependingOnSymbol { get; set; }

    /// <summary>ASCENDING KEY data-names from the OCCURS clause.</summary>
    public IReadOnlyList<string> AscendingKeys { get; }

    /// <summary>DESCENDING KEY data-names from the OCCURS clause.</summary>
    public IReadOnlyList<string> DescendingKeys { get; }

    /// <summary>INDEXED BY index-names from the OCCURS clause.</summary>
    public IReadOnlyList<string> IndexNames { get; }

    public OccursInfo(
        int minOccurs,
        int maxOccurs,
        string? dependingOnName = null,
        IReadOnlyList<string>? ascendingKeys = null,
        IReadOnlyList<string>? descendingKeys = null,
        IReadOnlyList<string>? indexNames = null)
    {
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        DependingOnName = dependingOnName;
        AscendingKeys = ascendingKeys ?? [];
        DescendingKeys = descendingKeys ?? [];
        IndexNames = indexNames ?? [];
    }
}

/// <summary>
/// Level-66 RENAMES clause information: FROM [THRU] data-name references.
/// </summary>
public sealed class RenamesInfo
{
    /// <summary>RENAMES source data-name (FROM).</summary>
    public string FromName { get; }

    /// <summary>RENAMES THRU target data-name (null if no THRU).</summary>
    public string? ThruName { get; }

    /// <summary>OF/IN qualifier for FROM (e.g., "RECORD-A" in NAME-1 OF RECORD-A).</summary>
    public string? FromQualifier { get; }

    /// <summary>OF/IN qualifier for THRU (e.g., "RECORD-A" in NAME-2 OF RECORD-A).</summary>
    public string? ThruQualifier { get; }

    /// <summary>Resolved FROM symbol (set during deferred resolution).</summary>
    public DataSymbol? FromSymbol { get; set; }

    /// <summary>Resolved THRU symbol (set during deferred resolution).</summary>
    public DataSymbol? ThruSymbol { get; set; }

    public RenamesInfo(string fromName, string? thruName = null,
        string? fromQualifier = null, string? thruQualifier = null)
    {
        FromName = fromName;
        ThruName = thruName;
        FromQualifier = fromQualifier;
        ThruQualifier = thruQualifier;
    }
}

/// <summary>
/// Represents a DATA DIVISION item at any level (01-49, 66, 77, 88).
/// Carries the raw PIC string, USAGE, VALUE, and structural parent/child
/// relationships that mirror the COBOL level-number hierarchy.
/// </summary>
public sealed class DataSymbol : Symbol
{
    /// <summary>COBOL level number (01-49 for records, 77 for independent items, 88 for conditions).</summary>
    public int LevelNumber { get; }

    /// <summary>Raw PIC clause string (e.g., "9(5)V99"). Null for group items (no PIC).</summary>
    public string? PicString { get; }

    /// <summary>USAGE clause: DISPLAY, COMP, COMP-3, INDEX, etc.</summary>
    public UsageKind Usage { get; set; }

    /// <summary>Type resolved from PIC/USAGE analysis; set during type resolution, null before.</summary>
    public ITypeSymbol? ResolvedType { get; set; }

    /// <summary>Literal value from the VALUE clause, or null if no VALUE was specified.</summary>
    public string? InitialValue { get; set; }

    /// <summary>Type name string for USAGE TYPE declarations (reserved for future use).</summary>
    public string? TypeName { get; }

    /// <summary>Original COBOL name (e.g., "FILLER"). Internal Name may differ for uniqueness.</summary>
    public string DisplayName { get; }

    /// <summary>True if this item was declared as FILLER (unnamed placeholder for record layout).</summary>
    public bool IsFiller { get; }

    /// <summary>True if this is a group item (has subordinate items, no PIC clause).
    /// USAGE INDEX/COMP-1/COMP-2 items without children are elementary, not groups.</summary>
    public bool IsGroup => PicString == null
        && !(Usage == Runtime.UsageKind.Index && Children.Count == 0)
        && !(Usage is Runtime.UsageKind.Comp1 or Runtime.UsageKind.Comp2 && Children.Count == 0);

    /// <summary>True if this is an elementary item (has a PIC clause, or USAGE INDEX/COMP-1/COMP-2 without children).</summary>
    public bool IsElementary => PicString != null
        || (Usage == Runtime.UsageKind.Index && Children.Count == 0)
        || (Usage is Runtime.UsageKind.Comp1 or Runtime.UsageKind.Comp2 && Children.Count == 0);

    /// <summary>Which DATA DIVISION storage area this item belongs to (WORKING-STORAGE, FILE SECTION, etc.).</summary>
    public StorageAreaKind Area { get; set; }

    /// <summary>
    /// Sign storage from explicit SIGN clause. Null means no explicit clause
    /// (default is trailing overpunch for signed DISPLAY fields).
    /// </summary>
    public SignStorageKind? ExplicitSignStorage { get; set; }

    /// <summary>
    /// If non-null, the VALUE clause specified a figurative constant.
    /// Used for field-filling initialization instead of InitialValue string.
    /// </summary>
    public FigurativeKind? FigurativeInit { get; set; }

    /// <summary>
    /// ALL literal pattern (e.g., "ABC" for VALUE ALL "ABC").
    /// Must be repeated to fill the field's PIC width during initialization.
    /// </summary>
    public string? AllLiteralPattern { get; set; }

    /// <summary>JUSTIFIED RIGHT clause present on this data item.</summary>
    public bool IsJustifiedRight { get; set; }

    /// <summary>SYNCHRONIZED clause present — align to natural boundary (§13.18.55).</summary>
    public bool IsSynchronized { get; set; }

    /// <summary>IS EXTERNAL clause (§13.18.22): shared storage across run unit.</summary>
    public bool IsExternal { get; set; }

    /// <summary>IS GLOBAL clause (§13.18.27): visible to contained programs.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>True if USAGE was explicitly specified on this item (not inherited from parent).</summary>
    public bool HasExplicitUsage { get; set; }

    /// <summary>Structured OCCURS clause information; null if no OCCURS clause.</summary>
    public OccursInfo? Occurs { get; set; }

    /// <summary>Level-66 RENAMES clause information; null if not a RENAMES item.</summary>
    public RenamesInfo? Renames { get; set; }

    /// <summary>Size of one element in bytes (set by RecordLayoutBuilder).</summary>
    public int ElementSize { get; set; }

    /// <summary>Resolved target of a REDEFINES clause; null if this item does not REDEFINE another.</summary>
    public DataSymbol? Redefines { get; set; }

    /// <summary>Unresolved REDEFINES target name (for deferred resolution).</summary>
    public string? RedefinesName { get; set; }

    /// <summary>Enclosing group item in the level-number hierarchy; null for 01/77-level items.</summary>
    public DataSymbol? Parent { get; private set; }

    /// <summary>Subordinate items in the level-number hierarchy (empty for elementary items).</summary>
    public List<DataSymbol> Children { get; } = [];

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

        // ISO §13.16.56: USAGE on a group applies to all subordinate items
        // that do not have their own explicit USAGE clause.
        if (!child.HasExplicitUsage && Usage != UsageKind.Display)
            child.Usage = Usage;

        Children.Add(child);
    }
}
