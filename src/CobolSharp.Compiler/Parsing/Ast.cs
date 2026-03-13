using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Parsing;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract class AstNode
{
    public TextSpan Span { get; }
    protected AstNode(TextSpan span) => Span = span;
}

// ═══════════════════════════════════════════════════════════════
// Top-level structure
// ═══════════════════════════════════════════════════════════════

public sealed class CompilationUnit : AstNode
{
    public List<ProgramNode> Programs { get; }
    public CompilationUnit(List<ProgramNode> programs, TextSpan span) : base(span)
    {
        Programs = programs;
    }
}

public sealed class ProgramNode : AstNode
{
    public IdentificationDivision Identification { get; }
    public DataDivision? Data { get; }
    public ProcedureDivision? Procedure { get; }

    public ProgramNode(IdentificationDivision identification,
        DataDivision? data, ProcedureDivision? procedure, TextSpan span) : base(span)
    {
        Identification = identification;
        Data = data;
        Procedure = procedure;
    }
}

// ═══════════════════════════════════════════════════════════════
// Identification Division
// ═══════════════════════════════════════════════════════════════

public sealed class IdentificationDivision : AstNode
{
    public string ProgramId { get; }
    public IdentificationDivision(string programId, TextSpan span) : base(span)
    {
        ProgramId = programId;
    }
}

// ═══════════════════════════════════════════════════════════════
// Data Division
// ═══════════════════════════════════════════════════════════════

public sealed class DataDivision : AstNode
{
    public WorkingStorageSection? WorkingStorage { get; }
    public DataDivision(WorkingStorageSection? workingStorage, TextSpan span) : base(span)
    {
        WorkingStorage = workingStorage;
    }
}

public sealed class WorkingStorageSection : AstNode
{
    public List<DataDescriptionEntry> Entries { get; }
    public WorkingStorageSection(List<DataDescriptionEntry> entries, TextSpan span) : base(span)
    {
        Entries = entries;
    }
}

public sealed class DataDescriptionEntry : AstNode
{
    public int LevelNumber { get; }
    public string? Name { get; }  // null for FILLER
    public string? PictureString { get; }
    public UsageType Usage { get; }
    public Expression? InitialValue { get; }
    public string? RedefinesName { get; }   // REDEFINES data-name
    public int OccursCount { get; }          // OCCURS n TIMES (0 = no OCCURS)
    public string? OccursDependingOn { get; } // OCCURS ... DEPENDING ON
    public string? RenamesStartName { get; }  // RENAMES start (level 66)
    public string? RenamesEndName { get; }    // RENAMES THRU end (level 66)
    public bool IsBlankWhenZero { get; }
    public bool IsJustifiedRight { get; }
    public List<ConditionValueClause>? ConditionValues { get; }  // level 88

    public DataDescriptionEntry(int levelNumber, string? name, string? pictureString,
        UsageType usage, Expression? initialValue, TextSpan span,
        string? redefinesName = null, int occursCount = 0,
        string? occursDependingOn = null, string? renamesStartName = null,
        string? renamesEndName = null, bool isBlankWhenZero = false,
        bool isJustifiedRight = false,
        List<ConditionValueClause>? conditionValues = null) : base(span)
    {
        LevelNumber = levelNumber;
        Name = name;
        PictureString = pictureString;
        Usage = usage;
        InitialValue = initialValue;
        RedefinesName = redefinesName;
        OccursCount = occursCount;
        OccursDependingOn = occursDependingOn;
        RenamesStartName = renamesStartName;
        RenamesEndName = renamesEndName;
        IsBlankWhenZero = isBlankWhenZero;
        IsJustifiedRight = isJustifiedRight;
        ConditionValues = conditionValues;
    }
}

/// <summary>
/// A value or range for a level-88 condition-name.
/// </summary>
public sealed class ConditionValueClause : AstNode
{
    public Expression Value { get; }
    public Expression? ThruValue { get; }  // for VALUE 1 THRU 5

    public ConditionValueClause(Expression value, Expression? thruValue, TextSpan span) : base(span)
    {
        Value = value;
        ThruValue = thruValue;
    }
}

public enum UsageType
{
    Display,        // default
    Binary,         // COMP / BINARY / COMP-4 / COMP-5
    PackedDecimal,  // COMP-3 / PACKED-DECIMAL
    Index,          // INDEX
    Pointer,        // POINTER
    FunctionPointer, // FUNCTION-POINTER
    ProcedurePointer, // PROCEDURE-POINTER
}

// ═══════════════════════════════════════════════════════════════
// Procedure Division
// ═══════════════════════════════════════════════════════════════

public sealed class ProcedureDivision : AstNode
{
    /// <summary>Statements before the first paragraph (the "initial" paragraph).</summary>
    public List<Statement> Statements { get; }
    /// <summary>Named paragraphs in the procedure division (across all sections).</summary>
    public List<Paragraph> Paragraphs { get; }
    /// <summary>Named sections in the procedure division.</summary>
    public List<Section> Sections { get; }

    public ProcedureDivision(List<Statement> statements, List<Paragraph> paragraphs,
        List<Section> sections, TextSpan span) : base(span)
    {
        Statements = statements;
        Paragraphs = paragraphs;
        Sections = sections;
    }
}

public sealed class Section : AstNode
{
    public string Name { get; }
    public List<Paragraph> Paragraphs { get; }

    public Section(string name, List<Paragraph> paragraphs, TextSpan span) : base(span)
    {
        Name = name;
        Paragraphs = paragraphs;
    }
}

public sealed class Paragraph : AstNode
{
    public string Name { get; }
    public List<Statement> Statements { get; }
    public string? SectionName { get; }  // which section this paragraph belongs to

    public Paragraph(string name, List<Statement> statements, TextSpan span,
        string? sectionName = null) : base(span)
    {
        Name = name;
        Statements = statements;
        SectionName = sectionName;
    }
}

// ═══════════════════════════════════════════════════════════════
// Statements
// ═══════════════════════════════════════════════════════════════

public abstract class Statement : AstNode
{
    protected Statement(TextSpan span) : base(span) { }
}

public sealed class DisplayStatement : Statement
{
    public List<Expression> Operands { get; }
    public DisplayStatement(List<Expression> operands, TextSpan span) : base(span)
    {
        Operands = operands;
    }
}

public sealed class StopRunStatement : Statement
{
    public StopRunStatement(TextSpan span) : base(span) { }
}

public sealed class GoToStatement : Statement
{
    public string ParagraphName { get; }
    public GoToStatement(string paragraphName, TextSpan span) : base(span)
    {
        ParagraphName = paragraphName;
    }
}

public sealed class GoToDependingStatement : Statement
{
    public List<string> ParagraphNames { get; }
    public Expression DependingOn { get; }
    public GoToDependingStatement(List<string> paragraphNames, Expression dependingOn, TextSpan span) : base(span)
    {
        ParagraphNames = paragraphNames;
        DependingOn = dependingOn;
    }
}

public sealed class ContinueStatement : Statement
{
    public ContinueStatement(TextSpan span) : base(span) { }
}

public sealed class ExitStatement : Statement
{
    public ExitType ExitKind { get; }
    public ExitStatement(ExitType exitKind, TextSpan span) : base(span) { ExitKind = exitKind; }
}

public enum ExitType
{
    Paragraph,
    Section,
    Program,
    Perform,
}

public sealed class AcceptStatement : Statement
{
    public IdentifierExpression Target { get; }
    public string? FromSource { get; }  // DATE, DAY, TIME, etc. null = from console

    public AcceptStatement(IdentifierExpression target, string? fromSource, TextSpan span) : base(span)
    {
        Target = target;
        FromSource = fromSource;
    }
}

public sealed class InitializeStatement : Statement
{
    public List<IdentifierExpression> Targets { get; }
    public InitializeStatement(List<IdentifierExpression> targets, TextSpan span) : base(span)
    {
        Targets = targets;
    }
}

/// <summary>
/// STRING source-1 DELIMITED BY delim-1 ... INTO target [WITH POINTER ptr] [ON OVERFLOW stmts]
/// </summary>
public sealed class StringStatement : Statement
{
    public List<StringSource> Sources { get; }
    public IdentifierExpression Target { get; }
    public IdentifierExpression? Pointer { get; }

    public StringStatement(List<StringSource> sources, IdentifierExpression target,
        IdentifierExpression? pointer, TextSpan span) : base(span)
    {
        Sources = sources;
        Target = target;
        Pointer = pointer;
    }
}

public sealed class StringSource
{
    public Expression Value { get; }
    public Expression? Delimiter { get; }  // null = SIZE

    public StringSource(Expression value, Expression? delimiter)
    {
        Value = value;
        Delimiter = delimiter;
    }
}

/// <summary>
/// UNSTRING source DELIMITED BY delim INTO target-1 [, target-2...] [TALLYING count]
/// </summary>
public sealed class UnstringStatement : Statement
{
    public IdentifierExpression Source { get; }
    public Expression? Delimiter { get; }
    public List<IdentifierExpression> Targets { get; }
    public IdentifierExpression? Tallying { get; }

    public UnstringStatement(IdentifierExpression source, Expression? delimiter,
        List<IdentifierExpression> targets, IdentifierExpression? tallying,
        TextSpan span) : base(span)
    {
        Source = source;
        Delimiter = delimiter;
        Targets = targets;
        Tallying = tallying;
    }
}

/// <summary>
/// INSPECT data-name TALLYING/REPLACING/CONVERTING
/// Simplified for Phase 3: supports INSPECT REPLACING ALL/LEADING/FIRST
/// </summary>
public sealed class InspectStatement : Statement
{
    public IdentifierExpression Target { get; }
    public InspectType InspectKind { get; }
    public Expression? SearchFor { get; }
    public Expression? ReplaceWith { get; }
    public IdentifierExpression? TallyCounter { get; }

    public InspectStatement(IdentifierExpression target, InspectType inspectKind,
        Expression? searchFor, Expression? replaceWith,
        IdentifierExpression? tallyCounter, TextSpan span) : base(span)
    {
        Target = target;
        InspectKind = inspectKind;
        SearchFor = searchFor;
        ReplaceWith = replaceWith;
        TallyCounter = tallyCounter;
    }
}

public enum InspectType
{
    TallyingAll,
    TallyingLeading,
    ReplacingAll,
    ReplacingLeading,
    ReplacingFirst,
    Converting,
}

public sealed class MoveStatement : Statement
{
    public Expression Source { get; }
    public List<IdentifierExpression> Targets { get; }

    public MoveStatement(Expression source, List<IdentifierExpression> targets, TextSpan span) : base(span)
    {
        Source = source;
        Targets = targets;
    }
}

public sealed class AddStatement : Statement
{
    /// <summary>Values being added (the operands before TO).</summary>
    public List<Expression> Operands { get; }
    /// <summary>Target identifiers (after TO).</summary>
    public List<IdentifierExpression> Targets { get; }

    public AddStatement(List<Expression> operands, List<IdentifierExpression> targets, TextSpan span) : base(span)
    {
        Operands = operands;
        Targets = targets;
    }
}

public sealed class SubtractStatement : Statement
{
    public List<Expression> Operands { get; }
    public List<IdentifierExpression> Targets { get; }

    public SubtractStatement(List<Expression> operands, List<IdentifierExpression> targets, TextSpan span) : base(span)
    {
        Operands = operands;
        Targets = targets;
    }
}

public sealed class ComputeStatement : Statement
{
    public IdentifierExpression Target { get; }
    public Expression Value { get; }

    public ComputeStatement(IdentifierExpression target, Expression value, TextSpan span) : base(span)
    {
        Target = target;
        Value = value;
    }
}

public sealed class IfStatement : Statement
{
    public Expression Condition { get; }
    public List<Statement> ThenStatements { get; }
    public List<Statement> ElseStatements { get; }

    public IfStatement(Expression condition, List<Statement> thenStatements,
        List<Statement> elseStatements, TextSpan span) : base(span)
    {
        Condition = condition;
        ThenStatements = thenStatements;
        ElseStatements = elseStatements;
    }
}

public sealed class PerformStatement : Statement
{
    /// <summary>Paragraph name for out-of-line PERFORM, null for inline.</summary>
    public string? ParagraphName { get; }
    /// <summary>End paragraph for PERFORM THRU.</summary>
    public string? ThruParagraphName { get; }
    /// <summary>Inline body statements (for inline PERFORM).</summary>
    public List<Statement> Body { get; }
    /// <summary>Number of times (PERFORM n TIMES).</summary>
    public Expression? Times { get; }
    /// <summary>UNTIL condition.</summary>
    public Expression? Until { get; }

    public PerformStatement(string? paragraphName, List<Statement> body,
        Expression? times, Expression? until, TextSpan span,
        string? thruParagraphName = null) : base(span)
    {
        ParagraphName = paragraphName;
        ThruParagraphName = thruParagraphName;
        Body = body;
        Times = times;
        Until = until;
    }
}

// ═══════════════════════════════════════════════════════════════
// Expressions
// ═══════════════════════════════════════════════════════════════

public abstract class Expression : AstNode
{
    protected Expression(TextSpan span) : base(span) { }
}

public sealed class NumericLiteralExpression : Expression
{
    public decimal Value { get; }
    public NumericLiteralExpression(decimal value, TextSpan span) : base(span) { Value = value; }
}

public sealed class StringLiteralExpression : Expression
{
    public string Value { get; }
    public StringLiteralExpression(string value, TextSpan span) : base(span) { Value = value; }
}

public sealed class IdentifierExpression : Expression
{
    public string Name { get; }
    /// <summary>Subscripts for table access: ITEM(1, 2) → [1, 2]</summary>
    public List<Expression>? Subscripts { get; }
    /// <summary>Reference modification start position: ITEM(3:5) → 3</summary>
    public Expression? RefModStart { get; }
    /// <summary>Reference modification length: ITEM(3:5) → 5</summary>
    public Expression? RefModLength { get; }

    public IdentifierExpression(string name, TextSpan span,
        List<Expression>? subscripts = null,
        Expression? refModStart = null, Expression? refModLength = null) : base(span)
    {
        Name = name;
        Subscripts = subscripts;
        RefModStart = refModStart;
        RefModLength = refModLength;
    }

    public bool HasSubscripts => Subscripts != null && Subscripts.Count > 0;
    public bool HasRefMod => RefModStart != null;
}

public sealed class FigurativeConstantExpression : Expression
{
    public FigurativeConstant Constant { get; }
    public FigurativeConstantExpression(FigurativeConstant constant, TextSpan span) : base(span)
    {
        Constant = constant;
    }
}

public enum FigurativeConstant
{
    Zero,
    Space,
    HighValue,
    LowValue,
    Quote,
}

public sealed class BinaryExpression : Expression
{
    public Expression Left { get; }
    public BinaryOperator Operator { get; }
    public Expression Right { get; }

    public BinaryExpression(Expression left, BinaryOperator op, Expression right, TextSpan span) : base(span)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Power,
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual,
    And,
    Or,
}

public sealed class UnaryExpression : Expression
{
    public UnaryOperator Operator { get; }
    public Expression Operand { get; }

    public UnaryExpression(UnaryOperator op, Expression operand, TextSpan span) : base(span)
    {
        Operator = op;
        Operand = operand;
    }
}

public enum UnaryOperator
{
    Negate,
    Not,
}
