// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    public CobolType Type { get; }
    protected BoundExpression(CobolType type) => Type = type;
}

public sealed class BoundLiteralExpression : BoundExpression
{
    public object Value { get; }

    public BoundLiteralExpression(object value, CobolType type)
        : base(type) => Value = value;

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

public sealed class BoundIdentifierExpression : BoundExpression
{
    public DataSymbol Symbol { get; }

    public BoundIdentifierExpression(DataSymbol symbol, CobolType type)
        : base(type) => Symbol = symbol;

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
        BoundExpression right, CobolType type)
        : base(type)
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

// ═══════════════════════════════════
// COBOL type (minimal for now)
// ═══════════════════════════════════

public sealed class CobolType
{
    public static readonly CobolType Numeric = new("Numeric");
    public static readonly CobolType Alphanumeric = new("Alphanumeric");
    public static readonly CobolType Boolean = new("Boolean");
    public static readonly CobolType String = new("String");
    public static readonly CobolType Unknown = new("Unknown");

    public string Name { get; }
    private CobolType(string name) => Name = name;
}
