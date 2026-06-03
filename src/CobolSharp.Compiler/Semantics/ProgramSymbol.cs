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

    /// <summary>True if this program is declared IS INITIAL (WORKING-STORAGE re-initialized per CALL).</summary>
    public bool IsInitial { get; set; }

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

    /// <summary>ALTERNATE RECORD KEY entries (for INDEXED files).</summary>
    public List<AlternateKeyInfo> AlternateKeys { get; } = [];

    /// <summary>FILE STATUS identifier name (PIC XX variable).</summary>
    public string? FileStatus { get; set; }

    /// <summary>SAME [RECORD] AREA clause (ISO §12.4.6.4): the representative file name of the group of
    /// files that share one record storage area. All files in a group carry the same leader name, and their
    /// 01 records are laid out at one shared base offset (so reading one file's record overwrites the
    /// others'). Null when the file is not named in any SAME [RECORD] AREA clause.</summary>
    public string? SameRecordAreaLeader { get; set; }

    /// <summary>RELATIVE KEY identifier name (for RELATIVE organization).</summary>
    public string? RelativeKey { get; set; }

    /// <summary>True if SELECT OPTIONAL was specified.</summary>
    public bool IsOptional { get; set; }

    /// <summary>True when this file is written anywhere with the WRITE … ADVANCING phrase — the spec's
    /// vertical page-positioning feature (ISO §14.9.51), which is meaningful only for a printer/report
    /// file. Together with a LINAGE clause it marks the file as a printed-page (listing) file whose
    /// records are page lines, so its host representation is line-rendered text rather than binary
    /// record-sequential. Set during WRITE binding.</summary>
    public bool WrittenWithAdvancing { get; set; }

    /// <summary>True when the FD has a LINAGE clause (the file is a logical-page/report file with a
    /// LINAGE-COUNTER, ISO §13.18.34). Set even when the LINAGE phrases use data-names (whose runtime
    /// values are read at OPEN), unlike LinageBody which only holds an integer-literal page size.</summary>
    public bool HasLinage { get; set; }

    /// <summary>LINAGE body line count (0 = no LINAGE clause). May be a data-name or integer.</summary>
    public int LinageBody { get; set; }
    /// <summary>LINAGE FOOTING line number (0 = no footing).</summary>
    public int LinageFooting { get; set; }
    /// <summary>LINAGE LINES AT TOP (default 0).</summary>
    public int LinageTop { get; set; }
    /// <summary>LINAGE LINES AT BOTTOM (default 0).</summary>
    public int LinageBottom { get; set; }

    // Data-name forms of the LINAGE phrases (ISO §13.18.34): when a phrase is a data-name rather than an
    // integer literal, its runtime value is read at OPEN OUTPUT (GR6b). Null = phrase is a literal/absent.
    public string? LinageBodyName { get; set; }
    public string? LinageFootingName { get; set; }
    public string? LinageTopName { get; set; }
    public string? LinageBottomName { get; set; }

    /// <summary>True when any LINAGE phrase uses a data-name (so its value must be evaluated at OPEN).</summary>
    public bool HasLinageDataNames =>
        LinageBodyName != null || LinageFootingName != null || LinageTopName != null || LinageBottomName != null;

    /// <summary>The 01-level record DataSymbol under this FD.</summary>
    public DataSymbol? Record { get; set; }

    /// <summary>
    /// True when the FD has a RECORD IS VARYING clause (with or without DEPENDING ON). Variable-length
    /// records are written for their exact length without trailing-space trimming and read into the
    /// largest record area, so a maximum-length record round-trips.
    /// </summary>
    public bool IsRecordVarying { get; set; }

    /// <summary>
    /// RECORD IS VARYING … DEPENDING ON data-name (ISO §13.18.43): the data-name that, on READ,
    /// receives the actual record length and, on WRITE/REWRITE, supplies the record length.
    /// Null when the FD has no VARYING…DEPENDING ON clause (fixed-length record or VARYING w/o DEPENDING).
    /// </summary>
    public string? RecordVaryingDependingOn { get; set; }

    /// <summary>VARYING IN SIZE FROM minimum (0 if unspecified).</summary>
    public int RecordVaryingMin { get; set; }

    /// <summary>VARYING IN SIZE TO maximum (0 if unspecified).</summary>
    public int RecordVaryingMax { get; set; }

    /// <summary>Record length in bytes (computed from PIC layout).</summary>
    public int RecordLength { get; set; }

    /// <summary>True if this file is described by an SD entry (sort-merge file).</summary>
    public bool IsSortMerge { get; set; }

    public FileSymbol(string name, int line)
        : base(name, SymbolKind.File, line) { }
}

/// <summary>
/// ALTERNATE RECORD KEY descriptor: data-name and whether duplicates are allowed.
/// </summary>
public sealed record AlternateKeyInfo(string DataName, bool AllowDuplicates);

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
/// An implementor switch defined in SPECIAL-NAMES.
/// Maps an implementor-defined name to a mnemonic with optional ON/OFF conditions.
/// </summary>
public sealed class ImplementorSwitch(
    string name, string implementorName, string? onValueName, string? offValueName)
{
    public string Name { get; } = name;
    public string ImplementorName { get; } = implementorName;
    public string? OnValueName { get; } = onValueName;
    public string? OffValueName { get; } = offValueName;
}

/// <summary>
/// A typed condition value: either a decimal (numeric) or a string (alphanumeric).
/// Replaces untyped object in level-88 VALUE clause processing.
/// </summary>
public sealed class ConditionValue
{
    public decimal? NumericValue { get; }
    public string? StringValue { get; }

    /// <summary>
    /// True when this value was declared with ALL (e.g., VALUE ALL "BAC").
    /// The StringValue holds the base pattern; at comparison time the pattern
    /// must be repeated to fill the parent field length.
    /// </summary>
    public bool IsAllLiteral { get; }

    public bool IsNumeric => NumericValue.HasValue;
    public bool IsString => StringValue != null;

    private ConditionValue(decimal? numeric, string? str, bool isAllLiteral = false)
    {
        NumericValue = numeric;
        StringValue = str;
        IsAllLiteral = isAllLiteral;
    }

    public static ConditionValue FromNumeric(decimal value) => new(value, null);
    public static ConditionValue FromString(string value) => new(null, value);
    public static ConditionValue FromAllString(string value) => new(null, value, isAllLiteral: true);

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

/// <summary>
/// Parameter passing mode for CALL USING arguments.
/// </summary>
public enum ParameterMode
{
    ByReference,
    ByContent,
    ByValue,
}

/// <summary>
/// Represents a callable procedure prototype for static CALL validation.
/// </summary>
public sealed class ProcedureSymbol : Symbol
{
    public IReadOnlyList<ProcedureParameter> Parameters { get; }
    public DataSymbol? Returning { get; }

    public ProcedureSymbol(string name, int line,
        IReadOnlyList<ProcedureParameter>? parameters = null,
        DataSymbol? returning = null)
        : base(name, SymbolKind.Program, line)
    {
        Parameters = parameters ?? [];
        Returning = returning;
    }
}

/// <summary>
/// One parameter in a ProcedureSymbol's USING list.
/// </summary>
public sealed class ProcedureParameter
{
    public string Name { get; }
    public ParameterMode Mode { get; }
    public DataSymbol? DataItem { get; }

    public ProcedureParameter(string name, ParameterMode mode, DataSymbol? dataItem = null)
    {
        Name = name;
        Mode = mode;
        DataItem = dataItem;
    }
}
