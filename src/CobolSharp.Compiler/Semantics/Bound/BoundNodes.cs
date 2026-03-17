// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
namespace CobolSharp.Compiler.Semantics.Bound;

public enum BoundNodeKind
{
    Program,
    Paragraph,
    Sentence,
    MoveStatement,
    PerformStatement,
    WriteStatement,
    IfStatement,
    DisplayStatement,
    StopStatement,
    GoToStatement,
    OpenStatement,
    CloseStatement,
    ReadStatement,
    ExitStatement,
    NextSentenceStatement,
    AddStatement,
    SubtractStatement,
    MultiplyStatement,
    DivideStatement,
    ComputeStatement,
    EvaluateStatement,
    LiteralExpression,
    IdentifierExpression,
    BinaryExpression,
    ConditionNameExpression,
}

public abstract class BoundNode
{
    public abstract BoundNodeKind Kind { get; }
}

// ═══════════════════════════════════
// Expressions
// ═══════════════════════════════════

public abstract class BoundExpression : BoundNode
{
    public CobolCategory Category { get; }
    protected BoundExpression(CobolCategory category) => Category = category;
}

public sealed class BoundLiteralExpression : BoundExpression
{
    public object Value { get; }

    public BoundLiteralExpression(object value, CobolCategory category)
        : base(category) => Value = value;

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

/// <summary>
/// A COBOL figurative constant (SPACE, ZERO, HIGH-VALUE, LOW-VALUE, QUOTE, ALL "X").
/// Unlike BoundLiteralExpression, this carries FigurativeKind so that MOVE can fill
/// the entire destination field with the figurative's byte value.
/// </summary>
public sealed class BoundFigurativeExpression : BoundExpression
{
    public int FigurativeKind { get; }   // Runtime.FigurativeKind enum value
    public string? AllLiteral { get; }   // non-null for ALL "X"

    public BoundFigurativeExpression(int figurativeKind, string? allLiteral = null)
        : base(CobolCategory.Alphanumeric)
    {
        FigurativeKind = figurativeKind;
        AllLiteral = allLiteral;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

public sealed class BoundIdentifierExpression : BoundExpression
{
    public DataSymbol Symbol { get; }

    public BoundIdentifierExpression(DataSymbol symbol, CobolCategory category)
        : base(category) => Symbol = symbol;

    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
}

public enum BoundBinaryOperatorKind
{
    // Arithmetic
    Add, Subtract, Multiply, Divide, Remainder,
    // Comparison
    Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual,
    // Logical
    Or, And, Not,
    // Exponentiation
    Power,
}

public sealed class BoundBinaryExpression : BoundExpression
{
    public BoundExpression Left { get; }
    public BoundExpression Right { get; }
    public BoundBinaryOperatorKind OperatorKind { get; }

    public BoundBinaryExpression(
        BoundExpression left, BoundBinaryOperatorKind op,
        BoundExpression right, CobolCategory category)
        : base(category)
    {
        Left = left;
        OperatorKind = op;
        Right = right;
    }

    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
}

// ── Class conditions (IS NUMERIC, IS ALPHABETIC, etc.) ──

public enum ClassConditionKind
{
    Numeric,
    Alphabetic,
    AlphabeticLower,
    AlphabeticUpper,
}

public sealed class BoundClassConditionExpression : BoundExpression
{
    public BoundExpression Subject { get; }
    public ClassConditionKind ClassKind { get; }
    public bool IsNegated { get; }

    public BoundClassConditionExpression(BoundExpression subject, ClassConditionKind classKind, bool isNegated)
        : base(CobolCategory.Unknown)
    {
        Subject = subject;
        ClassKind = classKind;
        IsNegated = isNegated;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression; // reuse
}

/// <summary>
/// A level-88 condition name test: expands to parent = value1 OR parent = value2 ...
/// </summary>
public sealed class BoundConditionNameExpression : BoundExpression
{
    public ConditionSymbol Condition { get; }
    public bool IsNegated { get; }

    public BoundConditionNameExpression(ConditionSymbol condition, bool isNegated = false)
        : base(CobolCategory.Unknown)
    {
        Condition = condition;
        IsNegated = isNegated;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ConditionNameExpression;
}

// ═══════════════════════════════════
// Statements
// ═══════════════════════════════════

public abstract class BoundStatement : BoundNode { }

public sealed class BoundDisplayStatement : BoundStatement
{
    public IReadOnlyList<BoundExpression> Operands { get; }

    public BoundDisplayStatement(IEnumerable<BoundExpression> operands)
        => Operands = operands.ToList().AsReadOnly();

    public override BoundNodeKind Kind => BoundNodeKind.DisplayStatement;
}

public sealed class BoundMoveStatement : BoundStatement
{
    public BoundExpression Source { get; }
    public IReadOnlyList<BoundExpression> Targets { get; }
    public bool IsRounded { get; }

    public BoundMoveStatement(BoundExpression source, IEnumerable<BoundExpression> targets, bool isRounded)
    {
        Source = source;
        Targets = targets.ToList().AsReadOnly();
        IsRounded = isRounded;
    }

    public override BoundNodeKind Kind => BoundNodeKind.MoveStatement;
}

public sealed class BoundPerformStatement : BoundStatement
{
    public ParagraphSymbol? Target { get; }
    public ParagraphSymbol? ThruTarget { get; }
    public int Times { get; } // 0 = once (no TIMES phrase)
    public BoundExpression? UntilCondition { get; }
    public BoundPerformVarying? Varying { get; }
    public IReadOnlyList<BoundStatement>? InlineStatements { get; }

    public BoundPerformStatement(ParagraphSymbol? target, ParagraphSymbol? thruTarget = null,
        int times = 0, BoundExpression? untilCondition = null,
        BoundPerformVarying? varying = null,
        IReadOnlyList<BoundStatement>? inlineStatements = null)
    {
        Target = target;
        ThruTarget = thruTarget;
        Times = times;
        UntilCondition = untilCondition;
        Varying = varying;
        InlineStatements = inlineStatements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PerformStatement;
}

public sealed class BoundPerformVarying
{
    public DataSymbol Index { get; }
    public BoundExpression Initial { get; }
    public BoundExpression Step { get; }
    public BoundExpression UntilCondition { get; }
    public BoundPerformVarying? Next { get; }  // AFTER clause → nested inner level

    public BoundPerformVarying(DataSymbol index, BoundExpression initial,
        BoundExpression step, BoundExpression untilCondition,
        BoundPerformVarying? next = null)
    {
        Index = index;
        Initial = initial;
        Step = step;
        UntilCondition = untilCondition;
        Next = next;
    }
}

public sealed class BoundWriteStatement : BoundStatement
{
    public FileSymbol? File { get; }
    public DataSymbol Record { get; }
    public BoundExpression? From { get; }

    public BoundWriteStatement(FileSymbol? file, DataSymbol record, BoundExpression? from)
    {
        File = file;
        Record = record;
        From = from;
    }

    public override BoundNodeKind Kind => BoundNodeKind.WriteStatement;
}

public sealed class BoundIfStatement : BoundStatement
{
    public BoundExpression Condition { get; }
    public IReadOnlyList<BoundStatement> ThenStatements { get; }
    public IReadOnlyList<BoundStatement>? ElseStatements { get; }

    public BoundIfStatement(
        BoundExpression condition,
        IEnumerable<BoundStatement> thenStatements,
        IEnumerable<BoundStatement>? elseStatements)
    {
        Condition = condition;
        ThenStatements = thenStatements.ToList().AsReadOnly();
        ElseStatements = elseStatements?.ToList().AsReadOnly();
    }

    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
}

public sealed class BoundStopStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.StopStatement;
}

public sealed class BoundGoToStatement : BoundStatement
{
    public ParagraphSymbol Target { get; }

    public BoundGoToStatement(ParagraphSymbol target) => Target = target;

    public override BoundNodeKind Kind => BoundNodeKind.GoToStatement;
}

public sealed class BoundExitStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ExitStatement;
}

public sealed class BoundNextSentenceStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.NextSentenceStatement;
}

public enum OpenMode { Input, Output, IO, Extend }

public sealed class BoundOpenStatement : BoundStatement
{
    public OpenMode Mode { get; }
    public IReadOnlyList<FileSymbol> Files { get; }

    public BoundOpenStatement(OpenMode mode, IReadOnlyList<FileSymbol> files)
    {
        Mode = mode;
        Files = files;
    }

    public override BoundNodeKind Kind => BoundNodeKind.OpenStatement;
}

public sealed class BoundCloseStatement : BoundStatement
{
    public IReadOnlyList<FileSymbol> Files { get; }

    public BoundCloseStatement(IReadOnlyList<FileSymbol> files)
        => Files = files;

    public override BoundNodeKind Kind => BoundNodeKind.CloseStatement;
}

public sealed class BoundReadStatement : BoundStatement
{
    public FileSymbol File { get; }
    public DataSymbol? Into { get; }
    public IReadOnlyList<BoundStatement> AtEnd { get; }
    public IReadOnlyList<BoundStatement> NotAtEnd { get; }

    public BoundReadStatement(
        FileSymbol file,
        DataSymbol? into,
        IReadOnlyList<BoundStatement> atEnd,
        IReadOnlyList<BoundStatement> notAtEnd)
    {
        File = file;
        Into = into;
        AtEnd = atEnd;
        NotAtEnd = notAtEnd;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReadStatement;
}

public sealed class BoundAddStatement : BoundStatement
{
    public IReadOnlyList<BoundExpression> Operands { get; }
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public BoundSizeErrorClause? SizeError { get; }
    public bool IsGiving { get; }  // true for ADD ... GIVING (target = sum, not target += sum)

    public BoundAddStatement(IReadOnlyList<BoundExpression> operands,
        IReadOnlyList<BoundArithmeticTarget> targets,
        BoundSizeErrorClause? sizeError = null,
        bool isGiving = false)
    {
        Operands = operands;
        Targets = targets;
        SizeError = sizeError;
        IsGiving = isGiving;
    }

    public override BoundNodeKind Kind => BoundNodeKind.AddStatement;
}

/// <summary>
/// ON SIZE ERROR / NOT ON SIZE ERROR clause, shared by all arithmetic statements.
/// </summary>
public sealed class BoundSizeErrorClause
{
    public IReadOnlyList<BoundStatement> OnSizeError { get; }
    public IReadOnlyList<BoundStatement> NotOnSizeError { get; }

    public BoundSizeErrorClause(
        IReadOnlyList<BoundStatement>? onSizeError = null,
        IReadOnlyList<BoundStatement>? notOnSizeError = null)
    {
        OnSizeError = onSizeError ?? Array.Empty<BoundStatement>();
        NotOnSizeError = notOnSizeError ?? Array.Empty<BoundStatement>();
    }

    public bool HasClauses => OnSizeError.Count > 0 || NotOnSizeError.Count > 0;
}

/// <summary>
/// A single receiving item in an arithmetic statement, with per-item ROUNDED flag.
/// Used by MULTIPLY, SUBTRACT, DIVIDE, ADD.
/// </summary>
public sealed class BoundArithmeticTarget
{
    public DataSymbol Symbol { get; }
    public bool IsRounded { get; }

    public BoundArithmeticTarget(DataSymbol symbol, bool isRounded)
    {
        Symbol = symbol;
        IsRounded = isRounded;
    }
}

public sealed class BoundMultiplyStatement : BoundStatement
{
    public BoundExpression Operand { get; }
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public DataSymbol? GivingTarget { get; }
    public BoundSizeErrorClause? SizeError { get; }

    public BoundMultiplyStatement(BoundExpression operand,
        IReadOnlyList<BoundArithmeticTarget> targets,
        DataSymbol? givingTarget = null,
        BoundSizeErrorClause? sizeError = null)
    {
        Operand = operand;
        Targets = targets;
        GivingTarget = givingTarget;
        SizeError = sizeError;
    }

    public override BoundNodeKind Kind => BoundNodeKind.MultiplyStatement;
}

public sealed class BoundSubtractStatement : BoundStatement
{
    public IReadOnlyList<BoundExpression> Operands { get; }
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public BoundSizeErrorClause? SizeError { get; }
    public bool IsGiving { get; }
    public BoundExpression? GivingMinuend { get; }  // the FROM operand for GIVING form

    public BoundSubtractStatement(IReadOnlyList<BoundExpression> operands,
        IReadOnlyList<BoundArithmeticTarget> targets,
        BoundSizeErrorClause? sizeError = null,
        bool isGiving = false,
        BoundExpression? givingMinuend = null)
    {
        Operands = operands;
        Targets = targets;
        SizeError = sizeError;
        IsGiving = isGiving;
        GivingMinuend = givingMinuend;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SubtractStatement;
}

public sealed class BoundDivideStatement : BoundStatement
{
    public BoundExpression Divisor { get; }
    public BoundExpression? Dividend { get; }
    public bool IsByForm { get; }
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public DataSymbol? RemainderTarget { get; }
    public BoundSizeErrorClause? SizeError { get; }

    public BoundDivideStatement(
        BoundExpression divisor,
        BoundExpression? dividend,
        bool isByForm,
        IReadOnlyList<BoundArithmeticTarget> targets,
        DataSymbol? remainderTarget = null,
        BoundSizeErrorClause? sizeError = null)
    {
        Divisor = divisor;
        Dividend = dividend;
        IsByForm = isByForm;
        Targets = targets;
        RemainderTarget = remainderTarget;
        SizeError = sizeError;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DivideStatement;
}

public sealed class BoundComputeStatement : BoundStatement
{
    public BoundExpression Expression { get; }
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public BoundSizeErrorClause? SizeError { get; }

    public BoundComputeStatement(BoundExpression expression,
        IReadOnlyList<BoundArithmeticTarget> targets,
        BoundSizeErrorClause? sizeError = null)
    {
        Expression = expression;
        Targets = targets;
        SizeError = sizeError;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ComputeStatement;
}

// BoundArithmeticStatement DELETED — it was a silent-drop pattern that produced NO code.
// All arithmetic binders now throw InvalidOperationException if they can't produce a
// proper bound statement. A compiler must never silently skip a statement.

// ═══════════════════════════════════
// EVALUATE
// ═══════════════════════════════════

public sealed class BoundEvaluateStatement : BoundStatement
{
    public IReadOnlyList<BoundExpression> Subjects { get; }  // empty for EVALUATE TRUE
    public IReadOnlyList<BoundEvaluateWhen> Whens { get; }
    public IReadOnlyList<BoundStatement>? WhenOther { get; }

    public bool IsEvaluateTrue => Subjects.Count == 0;

    public BoundEvaluateStatement(
        IReadOnlyList<BoundExpression> subjects,
        IReadOnlyList<BoundEvaluateWhen> whens,
        IReadOnlyList<BoundStatement>? whenOther)
    {
        Subjects = subjects;
        Whens = whens;
        WhenOther = whenOther;
    }

    public override BoundNodeKind Kind => BoundNodeKind.EvaluateStatement;
}

/// <summary>
/// One WHEN clause in an EVALUATE. SubjectConditions has one entry per subject
/// (positional: SubjectConditions[i] matches Subjects[i]).
/// For EVALUATE TRUE, SubjectConditions[0] is a BoundEvaluateConditionWhen.
/// </summary>
public sealed class BoundEvaluateWhen
{
    public IReadOnlyList<BoundEvaluateCondition> SubjectConditions { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundEvaluateWhen(
        IReadOnlyList<BoundEvaluateCondition> subjectConditions,
        IReadOnlyList<BoundStatement> statements)
    {
        SubjectConditions = subjectConditions;
        Statements = statements;
    }
}

/// <summary>
/// Base class for a single subject's match condition in an EVALUATE WHEN.
/// </summary>
public abstract class BoundEvaluateCondition { }

/// <summary>
/// Match subject against values and/or ranges: WHEN 1, WHEN 4 THRU 6.
/// </summary>
public sealed class BoundEvaluateValueCondition : BoundEvaluateCondition
{
    public IReadOnlyList<BoundExpression> Values { get; }
    public IReadOnlyList<BoundEvaluateRange> Ranges { get; }
    public bool IsAny { get; }  // WHEN ANY — matches unconditionally

    public BoundEvaluateValueCondition(
        IReadOnlyList<BoundExpression> values,
        IReadOnlyList<BoundEvaluateRange> ranges,
        bool isAny = false)
    {
        Values = values;
        Ranges = ranges;
        IsAny = isAny;
    }
}

/// <summary>
/// A range in an EVALUATE WHEN: value THRU value.
/// </summary>
public sealed class BoundEvaluateRange
{
    public BoundExpression From { get; }
    public BoundExpression To { get; }

    public BoundEvaluateRange(BoundExpression from, BoundExpression to)
    {
        From = from;
        To = to;
    }
}

/// <summary>
/// For EVALUATE TRUE: the WHEN's object is a standalone condition.
/// </summary>
public sealed class BoundEvaluateConditionWhen : BoundEvaluateCondition
{
    public BoundExpression Condition { get; }

    public BoundEvaluateConditionWhen(BoundExpression condition)
    {
        Condition = condition;
    }
}

// ═══════════════════════════════════
// Paragraph / Program
// ═══════════════════════════════════

public sealed class BoundSentence : BoundNode
{
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundSentence(IEnumerable<BoundStatement> statements)
        => Statements = statements.ToList().AsReadOnly();

    public override BoundNodeKind Kind => BoundNodeKind.Sentence;
}

public sealed class BoundParagraph : BoundNode
{
    public ParagraphSymbol Symbol { get; }
    public IReadOnlyList<BoundSentence> Sentences { get; }

    public BoundParagraph(ParagraphSymbol symbol, IEnumerable<BoundSentence> sentences)
    {
        Symbol = symbol;
        Sentences = sentences.ToList().AsReadOnly();
    }

    public override BoundNodeKind Kind => BoundNodeKind.Paragraph;
}

public sealed class BoundProgram : BoundNode
{
    public ProgramSymbol Program { get; }
    public IReadOnlyList<BoundParagraph> Paragraphs { get; }

    public BoundProgram(ProgramSymbol program, IEnumerable<BoundParagraph> paragraphs)
    {
        Program = program;
        Paragraphs = paragraphs.ToList().AsReadOnly();
    }

    public override BoundNodeKind Kind => BoundNodeKind.Program;
}

