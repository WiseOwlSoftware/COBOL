// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Represents a COBOL PROGRAM-ID and owns the three top-level scopes
/// (global, data division, procedure division) that structure all symbol declarations.
/// </summary>
public sealed class ProgramSymbol : Symbol
{
    /// <summary>Root scope for program-wide declarations (e.g., the program name itself).</summary>
    public Scope GlobalScope { get; }
    /// <summary>Scope for all DATA DIVISION items (WORKING-STORAGE, FILE SECTION, etc.).</summary>
    public Scope DataDivisionScope { get; }
    /// <summary>Scope for PROCEDURE DIVISION sections and paragraphs.</summary>
    public Scope ProcedureDivisionScope { get; }

    public ProgramSymbol(string name, int line)
        : base(name, SymbolKind.Program, line)
    {
        GlobalScope = new Scope(ScopeKind.GlobalProgram, parent: null);
        DataDivisionScope = new Scope(ScopeKind.DataDivision, GlobalScope);
        ProcedureDivisionScope = new Scope(ScopeKind.ProcedureDivision, GlobalScope);
    }
}

/// <summary>
/// A named SECTION in the PROCEDURE DIVISION. Sections group paragraphs and
/// serve as PERFORM targets (PERFORM section-name THRU ...).
/// </summary>
public sealed class SectionSymbol : Symbol
{
    /// <summary>The scope owned by this section, containing its paragraph symbols.</summary>
    public Scope Scope { get; }

    public SectionSymbol(string name, Scope parentScope, int line)
        : base(name, SymbolKind.Section, line)
    {
        Scope = new Scope(ScopeKind.Section, parentScope);
    }
}

/// <summary>
/// A named paragraph in the PROCEDURE DIVISION. Paragraphs are the basic
/// unit of control flow and the target of PERFORM and GO TO statements.
/// </summary>
public sealed class ParagraphSymbol : Symbol
{
    /// <summary>The scope owned by this paragraph (currently unused but available for nested declarations).</summary>
    public Scope Scope { get; }

    public ParagraphSymbol(string name, Scope parentScope, int line)
        : base(name, SymbolKind.Paragraph, line)
    {
        Scope = new Scope(ScopeKind.Paragraph, parentScope);
    }
}

/// <summary>
/// Represents an FD (file descriptor) from the FILE SECTION.
/// Captures SELECT/ASSIGN metadata and the associated record layout.
/// </summary>
public sealed class FileSymbol : Symbol
{
    /// <summary>External file name from ASSIGN TO clause.</summary>
    public string? AssignTarget { get; set; }

    /// <summary>True if ASSIGN target was a string literal (explicit path), false if identifier.</summary>
    public bool AssignIsLiteral { get; set; }

    /// <summary>SEQUENTIAL, RELATIVE, or INDEXED.</summary>
    public string? Organization { get; set; }

    /// <summary>SEQUENTIAL, RANDOM, or DYNAMIC.</summary>
    public string? AccessMode { get; set; }

    /// <summary>RECORD KEY identifier name (for INDEXED).</summary>
    public string? RecordKey { get; set; }

    /// <summary>FILE STATUS identifier name (PIC XX variable).</summary>
    public string? FileStatus { get; set; }

    /// <summary>The 01-level record DataSymbol under this FD.</summary>
    public DataSymbol? Record { get; set; }

    /// <summary>Record length in bytes (computed from PIC layout).</summary>
    public int RecordLength { get; set; }

    public FileSymbol(string name, int line)
        : base(name, SymbolKind.File, line) { }
}

/// <summary>
/// A level-88 condition-name. Bound to a parent data item and carries one or more
/// VALUE ranges (e.g., <c>88 IS-VALID VALUE 1 THRU 9.</c>). Used for boolean tests
/// that compare the parent item against the declared values.
/// </summary>
public sealed class ConditionSymbol : Symbol
{
    /// <summary>The data item this condition tests against.</summary>
    public DataSymbol ParentDataItem { get; }

    /// <summary>
    /// Value ranges declared in the level-88 VALUE clause.
    /// Each entry is a single value (To is null) or an inclusive THRU range.
    /// </summary>
    public IReadOnlyList<ConditionValueRange> ValueRanges => _ranges;
    private readonly List<ConditionValueRange> _ranges = [];

    public ConditionSymbol(string name, DataSymbol parent, int line)
        : base(name, SymbolKind.Condition88, line)
    {
        ParentDataItem = parent;
    }

    /// <summary>Adds a single value or inclusive THRU range to this condition.</summary>
    public void AddRange(ConditionValue from, ConditionValue? to = null)
        => _ranges.Add(new ConditionValueRange(from, to));
}

/// <summary>
/// A typed condition value: either a decimal (numeric) or a string (alphanumeric).
/// Replaces untyped object in level-88 VALUE clause processing.
/// </summary>
public sealed class ConditionValue
{
    public decimal? NumericValue { get; }
    public string? StringValue { get; }

    public bool IsNumeric => NumericValue.HasValue;
    public bool IsString => StringValue != null;

    private ConditionValue(decimal? numeric, string? str)
    {
        NumericValue = numeric;
        StringValue = str;
    }

    public static ConditionValue FromNumeric(decimal value) => new(value, null);
    public static ConditionValue FromString(string value) => new(null, value);

    /// <summary>Convert from the legacy untyped value.</summary>
    public static ConditionValue FromObject(object value) => value switch
    {
        decimal d => FromNumeric(d),
        string s => FromString(s),
        _ => throw new ArgumentException($"Unsupported condition value type: {value.GetType()}")
    };
}

/// <summary>
/// A single value or inclusive THRU range in a level-88 VALUE clause.
/// </summary>
public sealed record ConditionValueRange(ConditionValue From, ConditionValue? To);
