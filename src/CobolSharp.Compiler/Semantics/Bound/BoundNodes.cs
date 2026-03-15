// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
namespace CobolSharp.Compiler.Semantics.Bound;

public enum BoundNodeKind
{
    Program,
    Paragraph,
    MoveStatement,
    PerformStatement,
    WriteStatement,
    IfStatement,
    DisplayStatement,
    StopStatement,
    GoToStatement,
    OpenStatement,
    CloseStatement,
    ExitStatement,
    AddStatement,
    SubtractStatement,
    MultiplyStatement,
    DivideStatement,
    ComputeStatement,
    LiteralExpression,
    IdentifierExpression,
    BinaryExpression,
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

public sealed class BoundIdentifierExpression : BoundExpression
{
    public DataSymbol Symbol { get; }

    public BoundIdentifierExpression(DataSymbol symbol, CobolCategory category)
        : base(category) => Symbol = symbol;

    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
}

public enum BoundBinaryOperatorKind
{
    Add, Subtract, Multiply, Divide,
    Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual,
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
    public ParagraphSymbol Target { get; }
    public ParagraphSymbol? ThruTarget { get; }
    public int Times { get; } // 0 = once (no TIMES phrase)

    public BoundPerformStatement(ParagraphSymbol target, ParagraphSymbol? thruTarget = null, int times = 0)
    {
        Target = target;
        ThruTarget = thruTarget;
        Times = times;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PerformStatement;
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

public sealed class BoundOpenStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.OpenStatement;
}

public sealed class BoundCloseStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.CloseStatement;
}

public sealed class BoundAddStatement : BoundStatement
{
    public BoundExpression Operand { get; }
    public BoundExpression Target { get; }
    public bool IsRounded { get; }
    public IReadOnlyList<BoundStatement> OnSizeError { get; }
    public IReadOnlyList<BoundStatement> NotOnSizeError { get; }

    public BoundAddStatement(BoundExpression operand, BoundExpression target,
        bool isRounded = false,
        IReadOnlyList<BoundStatement>? onSizeError = null,
        IReadOnlyList<BoundStatement>? notOnSizeError = null)
    {
        Operand = operand;
        Target = target;
        IsRounded = isRounded;
        OnSizeError = onSizeError ?? Array.Empty<BoundStatement>();
        NotOnSizeError = notOnSizeError ?? Array.Empty<BoundStatement>();
    }

    public override BoundNodeKind Kind => BoundNodeKind.AddStatement;
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
    public IReadOnlyList<BoundStatement> OnSizeError { get; }
    public IReadOnlyList<BoundStatement> NotOnSizeError { get; }

    public BoundMultiplyStatement(BoundExpression operand,
        IReadOnlyList<BoundArithmeticTarget> targets,
        DataSymbol? givingTarget = null,
        IReadOnlyList<BoundStatement>? onSizeError = null,
        IReadOnlyList<BoundStatement>? notOnSizeError = null)
    {
        Operand = operand;
        Targets = targets;
        GivingTarget = givingTarget;
        OnSizeError = onSizeError ?? Array.Empty<BoundStatement>();
        NotOnSizeError = notOnSizeError ?? Array.Empty<BoundStatement>();
    }

    public override BoundNodeKind Kind => BoundNodeKind.MultiplyStatement;
}

public sealed class BoundSubtractStatement : BoundStatement
{
    /// <summary>The operands being subtracted (SUBTRACT A B C FROM ...: operands = [A, B, C]).</summary>
    public IReadOnlyList<BoundExpression> Operands { get; }
    /// <summary>The FROM targets: each gets target = target - sum(operands).</summary>
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    public IReadOnlyList<BoundStatement> OnSizeError { get; }
    public IReadOnlyList<BoundStatement> NotOnSizeError { get; }

    public BoundSubtractStatement(IReadOnlyList<BoundExpression> operands,
        IReadOnlyList<BoundArithmeticTarget> targets,
        IReadOnlyList<BoundStatement>? onSizeError = null,
        IReadOnlyList<BoundStatement>? notOnSizeError = null)
    {
        Operands = operands;
        Targets = targets;
        OnSizeError = onSizeError ?? Array.Empty<BoundStatement>();
        NotOnSizeError = notOnSizeError ?? Array.Empty<BoundStatement>();
    }

    public override BoundNodeKind Kind => BoundNodeKind.SubtractStatement;
}

public sealed class BoundDivideStatement : BoundStatement
{
    /// <summary>The divisor (DIVIDE a INTO b → a is divisor, b is dividend/target).</summary>
    public BoundExpression Divisor { get; }
    /// <summary>The dividend (for BY form: DIVIDE a BY b → b is dividend).</summary>
    public BoundExpression? Dividend { get; }
    /// <summary>True if DIVIDE a BY b form (vs INTO form).</summary>
    public bool IsByForm { get; }
    /// <summary>INTO targets (Format 1) or GIVING targets (Formats 2-5).</summary>
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }
    /// <summary>REMAINDER target (Formats 4-5).</summary>
    public DataSymbol? RemainderTarget { get; }
    public IReadOnlyList<BoundStatement> OnSizeError { get; }
    public IReadOnlyList<BoundStatement> NotOnSizeError { get; }

    public BoundDivideStatement(
        BoundExpression divisor,
        BoundExpression? dividend,
        bool isByForm,
        IReadOnlyList<BoundArithmeticTarget> targets,
        DataSymbol? remainderTarget = null,
        IReadOnlyList<BoundStatement>? onSizeError = null,
        IReadOnlyList<BoundStatement>? notOnSizeError = null)
    {
        Divisor = divisor;
        Dividend = dividend;
        IsByForm = isByForm;
        Targets = targets;
        RemainderTarget = remainderTarget;
        OnSizeError = onSizeError ?? Array.Empty<BoundStatement>();
        NotOnSizeError = notOnSizeError ?? Array.Empty<BoundStatement>();
    }

    public override BoundNodeKind Kind => BoundNodeKind.DivideStatement;
}

public sealed class BoundArithmeticStatement : BoundStatement
{
    public BoundNodeKind StatementKind { get; }

    public BoundArithmeticStatement(BoundNodeKind kind) => StatementKind = kind;

    public override BoundNodeKind Kind => StatementKind;
}

// ═══════════════════════════════════
// Paragraph / Program
// ═══════════════════════════════════

public sealed class BoundParagraph : BoundNode
{
    public ParagraphSymbol Symbol { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundParagraph(ParagraphSymbol symbol, IEnumerable<BoundStatement> statements)
    {
        Symbol = symbol;
        Statements = statements.ToList().AsReadOnly();
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

