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

    public DataDescriptionEntry(int levelNumber, string? name, string? pictureString,
        UsageType usage, Expression? initialValue, TextSpan span) : base(span)
    {
        LevelNumber = levelNumber;
        Name = name;
        PictureString = pictureString;
        Usage = usage;
        InitialValue = initialValue;
    }
}

public enum UsageType
{
    Display,     // default
    Binary,      // COMP / BINARY
    PackedDecimal, // COMP-3 / PACKED-DECIMAL
}

// ═══════════════════════════════════════════════════════════════
// Procedure Division
// ═══════════════════════════════════════════════════════════════

public sealed class ProcedureDivision : AstNode
{
    public List<Statement> Statements { get; }
    public ProcedureDivision(List<Statement> statements, TextSpan span) : base(span)
    {
        Statements = statements;
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
    /// <summary>Inline body statements (for inline PERFORM).</summary>
    public List<Statement> Body { get; }
    /// <summary>Number of times (PERFORM n TIMES).</summary>
    public Expression? Times { get; }
    /// <summary>UNTIL condition.</summary>
    public Expression? Until { get; }

    public PerformStatement(string? paragraphName, List<Statement> body,
        Expression? times, Expression? until, TextSpan span) : base(span)
    {
        ParagraphName = paragraphName;
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
    public IdentifierExpression(string name, TextSpan span) : base(span) { Name = name; }
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
