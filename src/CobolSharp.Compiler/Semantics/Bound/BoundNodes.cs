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
    ArithmeticStatement,
    EvaluateStatement,
    RewriteStatement,
    InitializeStatement,
    SetStatement,
    InspectStatement,
    AcceptStatement,
    LiteralExpression,
    IdentifierExpression,
    BinaryExpression,
    ConditionNameExpression,
    ReferenceModificationExpression,
    SearchStatement,
    SearchAllStatement,
    StringStatement,
    UnstringStatement,
    DeleteStatement,
    StartStatement,
    CorrespondingStatement,
    CompoundStatement,
    ReturnStatement,
    CallStatement,
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
    public ExpressionType? ResultType { get; set; }
    protected BoundExpression(CobolCategory category) => Category = category;
}

public sealed class BoundLiteralExpression : BoundExpression
{
    public object Value { get; }

    /// <summary>
    /// Original source text of the literal (e.g., "00000" for numeric literal 00000).
    /// Used for MOVE to alphanumeric fields where display digit count matters.
    /// Null for string literals (Value is already the original text).
    /// </summary>
    public string? OriginalText { get; }

    public BoundLiteralExpression(object value, CobolCategory category, string? originalText = null)
        : base(category) => (Value, OriginalText) = (value, originalText);

    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
}

/// <summary>
/// A COBOL figurative constant (SPACE, ZERO, HIGH-VALUE, LOW-VALUE, QUOTE, ALL "X").
/// Unlike BoundLiteralExpression, this carries FigurativeKind so that MOVE can fill
/// the entire destination field with the figurative's byte value.
/// </summary>
public sealed class BoundFigurativeExpression : BoundExpression
{
    public Runtime.FigurativeKind FigurativeKind { get; }
    public string? AllLiteral { get; }   // non-null for ALL "X"

    public BoundFigurativeExpression(Runtime.FigurativeKind figurativeKind, string? allLiteral = null)
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
    public IReadOnlyList<BoundExpression>? Subscripts { get; }

    public bool IsSubscripted => Subscripts != null && Subscripts.Count > 0;

    public BoundIdentifierExpression(DataSymbol symbol, CobolCategory category,
        IReadOnlyList<BoundExpression>? subscripts = null)
        : base(category)
    {
        Symbol = symbol;
        Subscripts = subscripts;
    }

    public override BoundNodeKind Kind => BoundNodeKind.IdentifierExpression;
}

/// <summary>
/// Reference modification: identifier(start:length) — extracts a substring.
/// Base is a BoundIdentifierExpression (possibly with subscripts).
/// Start is 1-based. Length is optional (null = rest of field).
/// </summary>
public sealed class BoundReferenceModificationExpression : BoundExpression
{
    public BoundIdentifierExpression Base { get; }
    public BoundExpression Start { get; }
    public BoundExpression? Length { get; }

    public BoundReferenceModificationExpression(
        BoundIdentifierExpression @base,
        BoundExpression start,
        BoundExpression? length)
        : base(CobolCategory.Alphanumeric)
    {
        Base = @base;
        Start = start;
        Length = length;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReferenceModificationExpression;
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

/// <summary>
/// A sequence of statements produced by multi-target SET desugaring.
/// The binder lowers each target into a separate statement; the lowering
/// phase flattens BoundCompoundStatement into sequential IR.
/// </summary>
public sealed class BoundCompoundStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.CompoundStatement;
    public IReadOnlyList<BoundStatement> Statements { get; }
    public BoundCompoundStatement(IReadOnlyList<BoundStatement> statements) => Statements = statements;
}

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

/// <summary>
/// Unified CORRESPONDING statement: MOVE, ADD, or SUBTRACT CORRESPONDING.
/// CorrespondingKind discriminates the operation. Pairs computed at bind time
/// via CorrespondingMatcher.
/// </summary>
public enum CorrespondingKind { Move, Add, Subtract }

public sealed class BoundCorrespondingStatement : BoundStatement
{
    public CorrespondingKind CorrespondingKind { get; }
    public DataSymbol SourceGroup { get; }
    public DataSymbol TargetGroup { get; }
    public IReadOnlyList<(DataSymbol Source, DataSymbol Target)> Pairs { get; }
    public bool IsRounded { get; }
    public BoundSizeErrorClause? SizeError { get; }

    public BoundCorrespondingStatement(
        CorrespondingKind kind,
        DataSymbol sourceGroup, DataSymbol targetGroup,
        IReadOnlyList<(DataSymbol Source, DataSymbol Target)> pairs,
        bool isRounded = false,
        BoundSizeErrorClause? sizeError = null)
    {
        CorrespondingKind = kind;
        SourceGroup = sourceGroup;
        TargetGroup = targetGroup;
        Pairs = pairs;
        IsRounded = isRounded;
        SizeError = sizeError;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CorrespondingStatement;
}

public sealed class BoundPerformStatement : BoundStatement
{
    public ParagraphSymbol? Target { get; }
    public ParagraphSymbol? ThruTarget { get; }
    public BoundExpression? TimesExpression { get; } // null = no TIMES phrase
    public BoundExpression? UntilCondition { get; }
    public BoundPerformVarying? Varying { get; }
    public IReadOnlyList<BoundStatement>? InlineStatements { get; }

    public BoundPerformStatement(ParagraphSymbol? target, ParagraphSymbol? thruTarget = null,
        BoundExpression? timesExpression = null, BoundExpression? untilCondition = null,
        BoundPerformVarying? varying = null,
        IReadOnlyList<BoundStatement>? inlineStatements = null)
    {
        Target = target;
        ThruTarget = thruTarget;
        TimesExpression = timesExpression;
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
    /// <summary>If non-null, this is WRITE AFTER/BEFORE ADVANCING n LINES.</summary>
    public int? AdvancingLines { get; }
    /// <summary>True for AFTER advancing, false for BEFORE advancing.</summary>
    public bool IsAfterAdvancing { get; }

    public BoundWriteStatement(FileSymbol? file, DataSymbol record, BoundExpression? from,
        int? advancingLines = null, bool isAfterAdvancing = true)
    {
        File = file;
        Record = record;
        From = from;
        AdvancingLines = advancingLines;
        IsAfterAdvancing = isAfterAdvancing;
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
    public IReadOnlyList<ParagraphSymbol> Targets { get; }
    public BoundIdentifierExpression? DependingOn { get; }

    public bool IsSimple => DependingOn == null && Targets.Count == 1;

    public BoundGoToStatement(IReadOnlyList<ParagraphSymbol> targets, BoundIdentifierExpression? dependingOn = null)
    {
        Targets = targets;
        DependingOn = dependingOn;
    }

    /// <summary>Convenience: single-target GO TO.</summary>
    public ParagraphSymbol Target => Targets[0];

    public override BoundNodeKind Kind => BoundNodeKind.GoToStatement;
}

public sealed class BoundExitStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ExitStatement;
}

public sealed class BoundExitPerformStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ExitStatement; // reuse — lowering distinguishes
}

public sealed class BoundExitParagraphStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.ExitStatement;
}

public sealed class BoundExitSectionStatement : BoundStatement
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
    public BoundIdentifierExpression? Into { get; }
    /// <summary>True if READ NEXT or READ PREVIOUS was specified.</summary>
    public bool IsNext { get; }
    /// <summary>The KEY IS data-name, if specified (for indexed files).</summary>
    public string? KeyDataName { get; }
    public IReadOnlyList<BoundStatement> AtEnd { get; }
    public IReadOnlyList<BoundStatement> NotAtEnd { get; }

    public BoundReadStatement(
        FileSymbol file,
        BoundIdentifierExpression? into,
        bool isNext,
        string? keyDataName,
        IReadOnlyList<BoundStatement> atEnd,
        IReadOnlyList<BoundStatement> notAtEnd)
    {
        File = file;
        Into = into;
        IsNext = isNext;
        KeyDataName = keyDataName;
        AtEnd = atEnd;
        NotAtEnd = notAtEnd;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReadStatement;
}

public sealed class BoundRewriteStatement : BoundStatement
{
    public FileSymbol File { get; }
    public DataSymbol Record { get; }
    public BoundExpression? From { get; }

    public BoundRewriteStatement(FileSymbol file, DataSymbol record, BoundExpression? from = null)
    {
        File = file;
        Record = record;
        From = from;
    }

    public override BoundNodeKind Kind => BoundNodeKind.RewriteStatement;
}

// ── INITIALIZE ──

public enum InitializeCategory
{
    Numeric,
    Alphanumeric,
    NumericEdited,
    AlphanumericEdited
}

public sealed class BoundInitializeCategoryReplacement
{
    public InitializeCategory Category { get; }
    public BoundExpression Value { get; }

    public BoundInitializeCategoryReplacement(InitializeCategory category, BoundExpression value)
    {
        Category = category;
        Value = value;
    }
}

public sealed class BoundInitializeStatement : BoundStatement
{
    public IReadOnlyList<DataSymbol> Targets { get; }
    public IReadOnlyList<BoundInitializeCategoryReplacement> CategoryReplacements { get; }

    public BoundInitializeStatement(
        IReadOnlyList<DataSymbol> targets,
        IReadOnlyList<BoundInitializeCategoryReplacement> categoryReplacements)
    {
        Targets = targets;
        CategoryReplacements = categoryReplacements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.InitializeStatement;
}

// ── SET ──

public enum SetOperation { Assign, UpBy, DownBy }

public sealed class BoundSetConditionStatement : BoundStatement
{
    public ConditionSymbol Condition { get; }
    public bool SetToTrue { get; }

    public BoundSetConditionStatement(ConditionSymbol condition, bool setToTrue)
    {
        Condition = condition;
        SetToTrue = setToTrue;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SetStatement;
}

public sealed class BoundSetIndexStatement : BoundStatement
{
    public BoundIdentifierExpression Target { get; }
    public SetOperation Operation { get; }
    public BoundExpression Value { get; }

    public BoundSetIndexStatement(BoundIdentifierExpression target, SetOperation operation, BoundExpression value)
    {
        Target = target;
        Operation = operation;
        Value = value;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SetStatement;
}

// ── ACCEPT ──

public sealed class BoundAcceptStatement : BoundStatement
{
    public BoundIdentifierExpression Target { get; }
    public AcceptSourceKind Source { get; }

    public BoundAcceptStatement(BoundIdentifierExpression target, AcceptSourceKind source)
    {
        Target = target;
        Source = source;
    }

    public override BoundNodeKind Kind => BoundNodeKind.AcceptStatement;
}

// ── INSPECT ──

public sealed class BoundInspectRegion
{
    public InspectPatternValue? BeforePattern { get; }
    public bool BeforeInitial { get; }
    public InspectPatternValue? AfterPattern { get; }
    public bool AfterInitial { get; }

    public BoundInspectRegion(InspectPatternValue? beforePattern, bool beforeInitial,
        InspectPatternValue? afterPattern, bool afterInitial)
    {
        BeforePattern = beforePattern;
        BeforeInitial = beforeInitial;
        AfterPattern = afterPattern;
        AfterInitial = afterInitial;
    }

    public static BoundInspectRegion Empty { get; } = new(null, false, null, false);
}

public enum InspectTallyKind { All, Leading, Characters }
public enum InspectReplaceKind { All, First, Leading, Characters }

/// <summary>
/// An INSPECT pattern value: either a compile-time literal string or a runtime data reference.
/// For data references, the pattern bytes are read from the field at runtime.
/// </summary>
public sealed class InspectPatternValue
{
    public string? Literal { get; }
    public BoundIdentifierExpression? DataRef { get; }

    public bool IsLiteral => Literal != null;
    public bool IsDataRef => DataRef != null;

    private InspectPatternValue(string? literal, BoundIdentifierExpression? dataRef)
    {
        Literal = literal;
        DataRef = dataRef;
    }

    public static InspectPatternValue FromLiteral(string value) => new(value, null);
    public static InspectPatternValue FromDataRef(BoundIdentifierExpression expr) => new(null, expr);
}

public sealed class BoundInspectTallyingItem
{
    public BoundIdentifierExpression Counter { get; }
    public InspectTallyKind Kind { get; }
    public InspectPatternValue? Pattern { get; }
    public BoundInspectRegion Region { get; }

    public BoundInspectTallyingItem(BoundIdentifierExpression counter, InspectTallyKind kind,
        InspectPatternValue? pattern, BoundInspectRegion region)
    {
        Counter = counter;
        Kind = kind;
        Pattern = pattern;
        Region = region;
    }
}

public sealed class BoundInspectReplacingItem
{
    public InspectReplaceKind Kind { get; }
    public InspectPatternValue Pattern { get; }
    public InspectPatternValue Replacement { get; }
    public BoundInspectRegion Region { get; }

    public BoundInspectReplacingItem(InspectReplaceKind kind, InspectPatternValue pattern,
        InspectPatternValue replacement, BoundInspectRegion region)
    {
        Kind = kind;
        Pattern = pattern;
        Replacement = replacement;
        Region = region;
    }
}

public sealed class BoundInspectConverting
{
    public InspectPatternValue FromSet { get; }
    public InspectPatternValue ToSet { get; }
    public BoundInspectRegion Region { get; }

    public BoundInspectConverting(InspectPatternValue fromSet, InspectPatternValue toSet,
        BoundInspectRegion region)
    {
        FromSet = fromSet;
        ToSet = toSet;
        Region = region;
    }
}

public sealed class BoundInspectStatement : BoundStatement
{
    public BoundIdentifierExpression Target { get; }
    public IReadOnlyList<BoundInspectTallyingItem> Tallying { get; }
    public IReadOnlyList<BoundInspectReplacingItem> Replacing { get; }
    public BoundInspectConverting? Converting { get; }

    public BoundInspectStatement(BoundIdentifierExpression target,
        IReadOnlyList<BoundInspectTallyingItem> tallying,
        IReadOnlyList<BoundInspectReplacingItem> replacing,
        BoundInspectConverting? converting)
    {
        Target = target;
        Tallying = tallying;
        Replacing = replacing;
        Converting = converting;
    }

    public override BoundNodeKind Kind => BoundNodeKind.InspectStatement;
}

/// <summary>
/// Unified arithmetic statement. All five COBOL arithmetic statements
/// (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE) converge to this single type.
/// ArithmeticKind discriminates the operation. No per-statement subclasses.
/// </summary>
public sealed class BoundArithmeticStatement : BoundStatement
{
    /// <summary>ADD, SUBTRACT, MULTIPLY, DIVIDE, or COMPUTE.</summary>
    public ArithmeticKind ArithmeticKind { get; }

    /// <summary>Source operands: the values being added/subtracted/multiplied/divided/computed.</summary>
    public IReadOnlyList<BoundExpression> Operands { get; }

    /// <summary>
    /// The TO/FROM/BY/INTO receiving operand (identifier or literal in GIVING forms).
    /// - SUBTRACT A FROM B: Receiver = null (B is in Targets); SUBTRACT A FROM 100 GIVING C: Receiver = literal(100)
    /// - MULTIPLY A BY B: Receiver = B; MULTIPLY A BY 2 GIVING C: Receiver = literal(2)
    /// - DIVIDE A INTO B: Receiver = null (B in Targets); DIVIDE A INTO 864 GIVING C: Receiver = literal(864)
    /// - COMPUTE X = expr: Receiver = null (expr is in Operands)
    /// - ADD: Receiver = null (TO targets are in Targets)
    /// </summary>
    public BoundExpression? Receiver { get; }

    /// <summary>Receiving targets (always identifiers). In non-GIVING forms, also accumulators.</summary>
    public IReadOnlyList<BoundArithmeticTarget> Targets { get; }

    /// <summary>True when GIVING keyword was used (target = result, not target op= result).</summary>
    public bool IsGiving { get; }

    /// <summary>DIVIDE-specific: true for BY form, false for INTO form.</summary>
    public bool IsByForm { get; }

    /// <summary>DIVIDE-specific: REMAINDER target identifier.</summary>
    public BoundIdentifierExpression? RemainderTarget { get; }

    /// <summary>ON SIZE ERROR / NOT ON SIZE ERROR clause.</summary>
    public BoundSizeErrorClause? SizeError { get; }

    public BoundArithmeticStatement(
        ArithmeticKind arithmeticKind,
        IReadOnlyList<BoundExpression> operands,
        BoundExpression? receiver,
        IReadOnlyList<BoundArithmeticTarget> targets,
        bool isGiving = false,
        bool isByForm = false,
        BoundIdentifierExpression? remainderTarget = null,
        BoundSizeErrorClause? sizeError = null)
    {
        ArithmeticKind = arithmeticKind;
        Operands = operands;
        Receiver = receiver;
        Targets = targets;
        IsGiving = isGiving;
        IsByForm = isByForm;
        RemainderTarget = remainderTarget;
        SizeError = sizeError;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ArithmeticStatement;
}

public enum ArithmeticKind { Add, Subtract, Multiply, Divide, Compute }

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
/// </summary>
public sealed class BoundArithmeticTarget
{
    public BoundIdentifierExpression Target { get; }
    public bool IsRounded { get; }

    public BoundArithmeticTarget(BoundIdentifierExpression target, bool isRounded)
    {
        Target = target;
        IsRounded = isRounded;
    }
}

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

// ═══════════════════════════════════
// SEARCH / SEARCH ALL
// ═══════════════════════════════════

/// <summary>
/// A single WHEN clause in a SEARCH or SEARCH ALL statement.
/// Condition is the match expression; Statements is the imperative body.
/// </summary>
public sealed class BoundSearchWhenClause
{
    public BoundExpression Condition { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundSearchWhenClause(BoundExpression condition, IReadOnlyList<BoundStatement> statements)
    {
        Condition = condition;
        Statements = statements;
    }
}

/// <summary>
/// SEARCH table — linear search through OCCURS table.
/// Index is the subscript variable used in WHEN conditions, extracted by the binder.
/// The lowering initializes Index to 1, increments it per iteration, and
/// terminates when Index exceeds OccursCount.
/// </summary>
public sealed class BoundSearchStatement : BoundStatement
{
    public BoundIdentifierExpression Table { get; }
    public BoundIdentifierExpression? Index { get; }
    public IReadOnlyList<BoundSearchWhenClause> Whens { get; }
    public IReadOnlyList<BoundStatement> AtEnd { get; }

    public BoundSearchStatement(
        BoundIdentifierExpression table,
        BoundIdentifierExpression? index,
        IReadOnlyList<BoundSearchWhenClause> whens,
        IReadOnlyList<BoundStatement> atEnd)
    {
        Table = table;
        Index = index;
        Whens = whens;
        AtEnd = atEnd;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SearchStatement;
}

/// <summary>
/// SEARCH ALL table — binary search through sorted OCCURS table.
/// Semantic restrictions (single WHEN, relational equality, sorted table)
/// are enforced in the binder. The lowering implements a binary search loop
/// using low/high/mid temporaries.
/// </summary>
public sealed class BoundSearchAllStatement : BoundStatement
{
    public BoundIdentifierExpression Table { get; }
    public BoundIdentifierExpression? Index { get; }
    public IReadOnlyList<BoundSearchWhenClause> Whens { get; }
    public IReadOnlyList<BoundStatement> AtEnd { get; }

    public BoundSearchAllStatement(
        BoundIdentifierExpression table,
        BoundIdentifierExpression? index,
        IReadOnlyList<BoundSearchWhenClause> whens,
        IReadOnlyList<BoundStatement> atEnd)
    {
        Table = table;
        Index = index;
        Whens = whens;
        AtEnd = atEnd;
    }

    public override BoundNodeKind Kind => BoundNodeKind.SearchAllStatement;
}

// ═══════════════════════════════════
// STRING
// ═══════════════════════════════════

/// <summary>
/// One sending phrase in a STRING statement: value + optional delimiter.
/// </summary>
public sealed class BoundStringSending
{
    public BoundExpression Value { get; }
    public BoundExpression? Delimiter { get; } // null if DELIMITED BY SIZE or no DELIMITED clause
    public bool DelimitedBySize { get; }

    public BoundStringSending(BoundExpression value, BoundExpression? delimiter, bool delimitedBySize)
    {
        Value = value;
        Delimiter = delimiter;
        DelimitedBySize = delimitedBySize;
    }
}

/// <summary>
/// STRING statement: concatenate sending items into a destination field.
/// </summary>
public sealed class BoundStringStatement : BoundStatement
{
    public IReadOnlyList<BoundStringSending> Sendings { get; }
    public BoundExpression Into { get; }
    public BoundExpression? Pointer { get; }
    public IReadOnlyList<BoundStatement> OnOverflow { get; }
    public IReadOnlyList<BoundStatement> NotOnOverflow { get; }

    public BoundStringStatement(
        IReadOnlyList<BoundStringSending> sendings,
        BoundExpression into,
        BoundExpression? pointer,
        IReadOnlyList<BoundStatement> onOverflow,
        IReadOnlyList<BoundStatement> notOnOverflow)
    {
        Sendings = sendings;
        Into = into;
        Pointer = pointer;
        OnOverflow = onOverflow;
        NotOnOverflow = notOnOverflow;
    }

    public override BoundNodeKind Kind => BoundNodeKind.StringStatement;
}

// ═══════════════════════════════════
// UNSTRING
// ═══════════════════════════════════

/// <summary>
/// One INTO phrase in an UNSTRING statement: target + optional COUNT IN + optional DELIMITER IN.
/// </summary>
public sealed class BoundUnstringInto
{
    public BoundExpression Target { get; }
    public BoundExpression? CountIn { get; }
    public BoundExpression? DelimiterIn { get; }

    public BoundUnstringInto(BoundExpression target, BoundExpression? countIn, BoundExpression? delimiterIn)
    {
        Target = target;
        CountIn = countIn;
        DelimiterIn = delimiterIn;
    }
}

/// <summary>
/// UNSTRING statement: split a source string into multiple destination fields.
/// </summary>
public sealed class BoundUnstringStatement : BoundStatement
{
    public BoundExpression Source { get; }
    public BoundExpression? Delimiter { get; }
    public bool DelimitedByAll { get; }
    public IReadOnlyList<BoundUnstringInto> Intos { get; }
    public BoundExpression? Pointer { get; }
    public BoundExpression? Tallying { get; }
    public IReadOnlyList<BoundStatement> OnOverflow { get; }
    public IReadOnlyList<BoundStatement> NotOnOverflow { get; }

    public BoundUnstringStatement(
        BoundExpression source, BoundExpression? delimiter, bool delimitedByAll,
        IReadOnlyList<BoundUnstringInto> intos, BoundExpression? pointer,
        BoundExpression? tallying,
        IReadOnlyList<BoundStatement> onOverflow, IReadOnlyList<BoundStatement> notOnOverflow)
    {
        Source = source;
        Delimiter = delimiter;
        DelimitedByAll = delimitedByAll;
        Intos = intos;
        Pointer = pointer;
        Tallying = tallying;
        OnOverflow = onOverflow;
        NotOnOverflow = notOnOverflow;
    }

    public override BoundNodeKind Kind => BoundNodeKind.UnstringStatement;
}

// ═══════════════════════════════════
// DELETE / START
// ═══════════════════════════════════

public sealed class BoundDeleteStatement : BoundStatement
{
    public FileSymbol File { get; }
    public IReadOnlyList<BoundStatement> InvalidKey { get; }
    public IReadOnlyList<BoundStatement> NotInvalidKey { get; }

    public BoundDeleteStatement(FileSymbol file,
        IReadOnlyList<BoundStatement> invalidKey, IReadOnlyList<BoundStatement> notInvalidKey)
    {
        File = file;
        InvalidKey = invalidKey;
        NotInvalidKey = notInvalidKey;
    }

    public override BoundNodeKind Kind => BoundNodeKind.DeleteStatement;
}

public sealed class BoundStartStatement : BoundStatement
{
    public FileSymbol File { get; }
    public BoundExpression? KeyCondition { get; }
    public IReadOnlyList<BoundStatement> InvalidKey { get; }
    public IReadOnlyList<BoundStatement> NotInvalidKey { get; }

    public BoundStartStatement(FileSymbol file, BoundExpression? keyCondition,
        IReadOnlyList<BoundStatement> invalidKey, IReadOnlyList<BoundStatement> notInvalidKey)
    {
        File = file;
        KeyCondition = keyCondition;
        InvalidKey = invalidKey;
        NotInvalidKey = notInvalidKey;
    }

    public override BoundNodeKind Kind => BoundNodeKind.StartStatement;
}

// ═══════════════════════════════════
// RETURN (sort/merge)
// ═══════════════════════════════════

public sealed class BoundReturnStatement : BoundStatement
{
    public FileSymbol File { get; }
    public BoundIdentifierExpression? Into { get; }
    public IReadOnlyList<BoundStatement> AtEnd { get; }
    public IReadOnlyList<BoundStatement> NotAtEnd { get; }

    public BoundReturnStatement(FileSymbol file, BoundIdentifierExpression? into,
        IReadOnlyList<BoundStatement> atEnd, IReadOnlyList<BoundStatement> notAtEnd)
    {
        File = file;
        Into = into;
        AtEnd = atEnd;
        NotAtEnd = notAtEnd;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
}

// ═══════════════════════════════════
// CALL
// ═══════════════════════════════════

public sealed class BoundCallArgument
{
    public ParameterMode Mode { get; }
    public BoundExpression Expression { get; }

    public BoundCallArgument(ParameterMode mode, BoundExpression expression)
    {
        Mode = mode;
        Expression = expression;
    }
}

public sealed class BoundCallStatement : BoundStatement
{
    public string TargetName { get; }
    public bool IsDynamic { get; }
    public IReadOnlyList<BoundCallArgument> Arguments { get; }
    public BoundIdentifierExpression? ReturningTarget { get; }
    public IReadOnlyList<BoundStatement> OnException { get; }
    public IReadOnlyList<BoundStatement> NotOnException { get; }

    public BoundCallStatement(
        string targetName, bool isDynamic,
        IReadOnlyList<BoundCallArgument> arguments,
        BoundIdentifierExpression? returningTarget,
        IReadOnlyList<BoundStatement> onException,
        IReadOnlyList<BoundStatement> notOnException)
    {
        TargetName = targetName;
        IsDynamic = isDynamic;
        Arguments = arguments;
        ReturningTarget = returningTarget;
        OnException = onException;
        NotOnException = notOnException;
    }

    public override BoundNodeKind Kind => BoundNodeKind.CallStatement;
}

