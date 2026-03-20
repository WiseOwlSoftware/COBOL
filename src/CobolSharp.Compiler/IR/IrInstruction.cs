// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.IR;

/// <summary>
/// Base class for all IR instructions. CIL-friendly, COBOL-aware.
/// </summary>
public abstract class IrInstruction
{
    public IrValue? Result { get; protected set; }
}

// ── Compiler temporaries ──

/// <summary>
/// A compiler-generated temporary variable. Not addressable from COBOL.
/// Scoped to the containing method. Lowered to a CIL local by the emitter.
/// </summary>
public sealed record IrTemp(string Name, IrPrimitiveType Type, int Id);

/// <summary>
/// Inline PERFORM N TIMES: execute BodyStatements exactly CountExpression times.
/// CountExpression is evaluated once at entry into a compiler temp (IrTemp).
/// The emitter manages the CIL local counter. EXIT PERFORM exits this scope.
/// </summary>
public sealed class IrPerformInlineTimes : IrInstruction
{
    public Semantics.Bound.BoundExpression CountExpression { get; }
    public IReadOnlyList<IrInstruction> BodyInstructions { get; }
    public IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>? ResolvedLocations { get; }

    public IrPerformInlineTimes(
        Semantics.Bound.BoundExpression countExpression,
        IReadOnlyList<IrInstruction> bodyInstructions,
        IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>? resolvedLocations = null)
    {
        CountExpression = countExpression;
        BodyInstructions = bodyInstructions;
        ResolvedLocations = resolvedLocations;
    }
}

// ── Data movement ──

public sealed class IrLoadField : IrInstruction
{
    public IrValue Record { get; }
    public IrField Field { get; }

    public IrLoadField(IrValue result, IrValue record, IrField field)
    {
        Result = result;
        Record = record;
        Field = field;
    }
}

public sealed class IrStoreField : IrInstruction
{
    public IrValue Record { get; }
    public IrField Field { get; }
    public IrValue Value { get; }

    public IrStoreField(IrValue record, IrField field, IrValue value)
    {
        Record = record;
        Field = field;
        Value = value;
    }
}

public sealed class IrMove : IrInstruction
{
    public IrValue Source { get; }
    public IrValue Target { get; }

    public IrMove(IrValue source, IrValue target)
    {
        Source = source;
        Target = target;
    }
}

public sealed class IrLoadConst : IrInstruction
{
    public object Value { get; }

    public IrLoadConst(IrValue result, object value)
    {
        Result = result;
        Value = value;
    }
}

// ── Arithmetic and comparisons ──

public enum IrBinaryOp
{
    Add, Sub, Mul, Div,
    And, Or,
    Eq, Ne, Lt, Le, Gt, Ge
}

public sealed class IrBinary : IrInstruction
{
    public IrBinaryOp Op { get; }
    public IrValue Left { get; }
    public IrValue Right { get; }

    public IrBinary(IrValue result, IrBinaryOp op, IrValue left, IrValue right)
    {
        Result = result;
        Op = op;
        Left = left;
        Right = right;
    }
}

// ── Control flow ──

public sealed class IrBranch : IrInstruction
{
    public IrValue Condition { get; }
    public IrBasicBlock TrueTarget { get; }
    public IrBasicBlock FalseTarget { get; }

    public IrBranch(IrValue condition, IrBasicBlock trueTarget, IrBasicBlock falseTarget)
    {
        Condition = condition;
        TrueTarget = trueTarget;
        FalseTarget = falseTarget;
    }
}

public sealed class IrJump : IrInstruction
{
    public IrBasicBlock Target { get; }
    public IrJump(IrBasicBlock target) => Target = target;
}

/// <summary>
/// Branch to Target if Condition is false; otherwise fall through.
/// </summary>
public sealed class IrBranchIfFalse : IrInstruction
{
    public IrValue Condition { get; }
    public IrBasicBlock Target { get; }

    public IrBranchIfFalse(IrValue condition, IrBasicBlock target)
    {
        Condition = condition;
        Target = target;
    }
}

/// <summary>
/// Store a boolean constant into an IrValue (used as fallback condition).
/// </summary>
public sealed class IrSetBool : IrInstruction
{
    public bool Value { get; }

    public IrSetBool(IrValue result, bool value)
    {
        Result = result;
        Value = value;
    }
}

public enum IrLogicalOp { And, Or, Not }

/// <summary>
/// Logical AND/OR/NOT on boolean values.
/// For NOT, Left and Right are the same value (only Left is used).
/// </summary>
public sealed class IrBinaryLogical : IrInstruction
{
    public IrValue Left { get; }
    public IrValue Right { get; }
    public IrLogicalOp Op { get; }

    public IrBinaryLogical(IrValue result, IrValue left, IrValue right, IrLogicalOp op)
    {
        Result = result;
        Left = left;
        Right = right;
        Op = op;
    }
}

/// <summary>
/// Initialize (clear) the method's ArithmeticStatus local.
/// Emitted once per arithmetic statement, before any operations.
/// </summary>
public sealed class IrInitArithmeticStatus : IrInstruction { }

/// <summary>
/// Load the SizeError flag from the method's ArithmeticStatus local into a bool.
/// </summary>
public sealed class IrLoadSizeError : IrInstruction
{
    public IrLoadSizeError(IrValue result) => Result = result;
}

public sealed class IrReturn : IrInstruction
{
    public IrValue? Value { get; }
    public IrReturn(IrValue? value) => Value = value;
}

/// <summary>
/// Return a constant int from a paragraph method.
/// Fall-through: myIndex+1, GO TO: targetIndex, STOP RUN: -1.
/// </summary>
public sealed class IrReturnConst : IrInstruction
{
    public int Value { get; }
    public IrReturnConst(int value) => Value = value;
}

/// <summary>
/// PC-driven dispatch loop over paragraph methods (emitted in Main).
/// while (pc >= 0 && pc &lt; N) pc = paragraphs[pc]();
/// </summary>
public sealed class IrParagraphDispatch : IrInstruction
{
    public IReadOnlyList<IrMethod> Paragraphs { get; }
    public IrParagraphDispatch(IReadOnlyList<IrMethod> paragraphs) => Paragraphs = paragraphs;
}

// ── Calls and PERFORM ──

public sealed class IrCall : IrInstruction
{
    public IrMethod Target { get; }
    public IReadOnlyList<IrValue> Arguments { get; }

    public IrCall(IrValue? result, IrMethod target, IReadOnlyList<IrValue> args)
    {
        Result = result;
        Target = target;
        Arguments = args;
    }
}

/// <summary>
/// PERFORM paragraph → call to generated method.
/// Each COBOL paragraph becomes its own IrMethod.
/// </summary>
public sealed class IrPerform : IrInstruction
{
    public IrMethod Target { get; }
    public IrPerform(IrMethod target) => Target = target;
}

/// <summary>
/// PERFORM para N TIMES: calls Target method Count times using a CIL local counter.
/// Count is a BoundExpression (literal or identifier) evaluated once at entry.
/// The emitter manages the loop counter as a CIL local int.
/// </summary>
public sealed class IrPerformTimes : IrInstruction
{
    public IrMethod Target { get; }
    public int StartIdx { get; }
    public int EndIdx { get; }
    public IReadOnlyList<IrMethod> ThruMethods { get; }
    public Semantics.Bound.BoundExpression CountExpression { get; }
    public IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>? ResolvedLocations { get; }

    public IrPerformTimes(IrMethod target, int startIdx, int endIdx,
        IReadOnlyList<IrMethod> thruMethods,
        Semantics.Bound.BoundExpression countExpression,
        IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>? resolvedLocations = null)
    {
        Target = target;
        StartIdx = startIdx;
        EndIdx = endIdx;
        ThruMethods = thruMethods;
        CountExpression = countExpression;
        ResolvedLocations = resolvedLocations;
    }
}

/// <summary>
/// PERFORM para-a THRU para-b: dynamic dispatch loop that respects GO TO returns.
/// Calls paragraphs startIdx..endIdx, but if a paragraph returns a PC within the
/// range, skips forward to that PC. If it returns outside the range or negative, exits.
/// </summary>
public sealed class IrPerformThru : IrInstruction
{
    public int StartIndex { get; }
    public int EndIndex { get; }
    public IReadOnlyList<IrMethod> Paragraphs { get; }

    public IrPerformThru(int startIndex, int endIndex, IReadOnlyList<IrMethod> paragraphs)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        Paragraphs = paragraphs;
    }
}

// ── Storage-backed data movement ──

/// <summary>
/// MOVE "literal" TO field — writes string bytes into ProgramState backing array.
/// </summary>
/// <summary>
/// MOVE string literal TO field. Uses PIC-aware MOVE semantics:
/// plain alphanumeric fields get left-justified space-padded copy,
/// alphanumeric-edited fields get edit pattern applied (B→space, 0→zero, etc.).
/// The emitter passes the destination PIC to the runtime so the correct
/// MOVE method is selected.
/// </summary>
public sealed class IrMoveStringToField : IrInstruction
{
    public IrLocation Target { get; }
    public string Value { get; }

    public IrMoveStringToField(IrLocation target, string value)
    {
        Target = target;
        Value = value;
    }
}

/// <summary>
/// MOVE figurative-constant TO field — fills entire field with figurative byte value.
/// </summary>
public sealed class IrMoveFigurative : IrInstruction
{
    public IrLocation Destination { get; }
    public FigurativeKind FigurativeKind { get; }

    public IrMoveFigurative(IrLocation dest, FigurativeKind figurativeKind)
    {
        Destination = dest;
        FigurativeKind = figurativeKind;
    }
}

/// <summary>
/// MOVE ALL "pattern" TO field — repeats pattern to fill entire field.
/// </summary>
public sealed class IrMoveAllLiteral : IrInstruction
{
    public IrLocation Destination { get; }
    public string Pattern { get; }

    public IrMoveAllLiteral(IrLocation dest, string pattern)
    {
        Destination = dest;
        Pattern = pattern;
    }
}

/// <summary>
/// WRITE record — outputs record bytes from ProgramState to file.
/// </summary>
public sealed class IrWriteRecordFromStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrWriteRecordFromStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// REWRITE record — replaces the last-read record in a file.
/// </summary>
public sealed class IrRewriteRecordFromStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrRewriteRecordFromStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// WRITE AFTER ADVANCING: print-control write with line advance.
/// </summary>
public sealed class IrWriteAfterAdvancing : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public int AdvanceLines { get; }

    public IrWriteAfterAdvancing(string fileName, IrLocation record, int advanceLines)
    {
        FileName = fileName;
        Record = record;
        AdvanceLines = advanceLines;
    }
}

/// <summary>
/// READ: read next record from file into storage location.
/// </summary>
public sealed class IrReadRecordToStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrReadRecordToStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// DELETE: delete the current record from an indexed/relative file.
/// </summary>
public sealed class IrDeleteRecord : IrInstruction
{
    public string FileName { get; }
    public IrDeleteRecord(string fileName) { FileName = fileName; }
}

/// <summary>
/// START: position an indexed file for subsequent READ NEXT.
/// Condition maps to Runtime.IO.StartCondition enum.
/// </summary>
public sealed class IrStartFile : IrInstruction
{
    public string FileName { get; }
    public IrLocation KeyLocation { get; }
    public int Condition { get; }

    public IrStartFile(string fileName, IrLocation keyLocation, int condition)
    {
        FileName = fileName;
        KeyLocation = keyLocation;
        Condition = condition;
    }
}

/// <summary>
/// Check if the last file operation was successful (status == "00").
/// Sets result bool to true if the operation failed (invalid key / error).
/// </summary>
public sealed class IrCheckFileInvalidKey : IrInstruction
{
    public string FileName { get; }

    public IrCheckFileInvalidKey(string fileName, IrValue result)
    {
        FileName = fileName;
        Result = result;
    }
}

/// <summary>
/// Check if a file is at EOF after a READ. Sets result bool to true if at end.
/// </summary>
public sealed class IrCheckFileAtEnd : IrInstruction
{
    public string FileName { get; }
    public new IrValue Result { get; }

    public IrCheckFileAtEnd(string fileName, IrValue result)
    {
        FileName = fileName;
        Result = result;
    }
}

/// <summary>
/// Store the most recent file status code into a FILE STATUS variable.
/// </summary>
public sealed class IrStoreFileStatus : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation StatusVariable { get; }

    public IrStoreFileStatus(string cobolFileName, IrLocation statusVariable)
    {
        CobolFileName = cobolFileName;
        StatusVariable = statusVariable;
    }
}

// ── Location abstraction ──

/// <summary>
/// Base type for "where a value lives": either a compile-time-known static
/// location or a runtime-computed element within an OCCURS array.
/// All IR instructions that operate on data items use IrLocation instead of
/// raw StorageLocation, making subscript handling uniform.
/// </summary>
public abstract class IrLocation { }

/// <summary>
/// A compile-time-known storage location (non-subscripted, or constant-subscript
/// already folded to a fixed offset).
/// </summary>
public sealed class IrStaticLocation : IrLocation
{
    public CodeGen.StorageLocation Location { get; }

    public IrStaticLocation(CodeGen.StorageLocation location)
    {
        Location = location;
    }
}

/// <summary>
/// A reference to an element within an OCCURS array (1D, 2D, or 3D).
/// The effective offset is computed at runtime using the general formula:
///   offset = base + sum_i((subscript_i - 1) * multiplier_i)
/// where multiplier_i is the product of all inner dimension sizes * element size.
/// Subscripts are carried as BoundExpressions — the emitter evaluates each one
/// via EmitExpression (handles identifiers, arithmetic, and any expression).
/// </summary>
public sealed class IrElementRef : IrLocation
{
    public CodeGen.StorageLocation BaseLocation { get; }
    public IReadOnlyList<Semantics.Bound.BoundExpression> Subscripts { get; }
    public IReadOnlyList<int> Multipliers { get; }
    public int ElementSize { get; }
    public Runtime.PicDescriptor ElementPic { get; }

    public IrElementRef(CodeGen.StorageLocation baseLocation,
        IReadOnlyList<Semantics.Bound.BoundExpression> subscripts,
        IReadOnlyList<int> multipliers,
        int elementSize, Runtime.PicDescriptor elementPic)
    {
        BaseLocation = baseLocation;
        Subscripts = subscripts;
        Multipliers = multipliers;
        ElementSize = elementSize;
        ElementPic = elementPic;
    }
}

/// <summary>
/// A reference modification: base location + runtime start:length substring.
/// Composes with IrStaticLocation or IrElementRef as the base.
/// The effective storage is: (base_area, base_offset + start - 1, length).
/// </summary>
public sealed class IrRefModLocation : IrLocation
{
    public IrLocation Base { get; }
    public Semantics.Bound.BoundExpression Start { get; }
    public Semantics.Bound.BoundExpression? Length { get; }
    public int BaseFieldLength { get; }

    public IrRefModLocation(IrLocation @base, Semantics.Bound.BoundExpression start,
        Semantics.Bound.BoundExpression? length, int baseFieldLength)
    {
        Base = @base;
        Start = start;
        Length = length;
        BaseFieldLength = baseFieldLength;
    }
}

// ── GO TO DEPENDING ──

/// <summary>
/// GO TO para1 para2 ... DEPENDING ON selector.
/// Evaluates selector as integer N (1-based). If 1 ≤ N ≤ targets.Count,
/// returns targets[N-1] as the next PC. Otherwise falls through.
/// </summary>
public sealed class IrGoToDepending : IrInstruction
{
    public IrLocation Selector { get; }
    public IReadOnlyList<int> TargetParagraphIndices { get; }

    public IrGoToDepending(IrLocation selector, IReadOnlyList<int> targetParagraphIndices)
    {
        Selector = selector;
        TargetParagraphIndices = targetParagraphIndices;
    }
}

// ── ACCEPT ──

public sealed class IrAccept : IrInstruction
{
    public IrLocation Target { get; }
    public AcceptSourceKind Source { get; }

    public IrAccept(IrLocation target, AcceptSourceKind source)
    {
        Target = target;
        Source = source;
    }
}

// ── INSPECT ──

/// <summary>
/// IR-level INSPECT pattern: either a compile-time literal string or a pre-resolved
/// runtime location. The Binder resolves BoundInspectPatternValue → IrInspectPatternValue
/// during lowering, converting data-ref patterns to IrLocations.
/// </summary>
public sealed class IrInspectPatternValue
{
    public string? Literal { get; }
    public IrLocation? Location { get; }

    public bool IsLiteral => Literal != null;
    public bool IsLocation => Location != null;

    private IrInspectPatternValue(string? literal, IrLocation? location)
    {
        Literal = literal;
        Location = location;
    }

    public static IrInspectPatternValue FromLiteral(string value) => new(value, null);
    public static IrInspectPatternValue FromLocation(IrLocation loc) => new(null, loc);
}

public sealed class IrInspectTally : IrInstruction
{
    public IrLocation Target { get; }
    public IrLocation Counter { get; }
    public Semantics.Bound.InspectTallyKind Kind { get; }
    public IrInspectPatternValue? Pattern { get; }
    public IrInspectPatternValue? BeforePattern { get; }
    public bool BeforeInitial { get; }
    public IrInspectPatternValue? AfterPattern { get; }
    public bool AfterInitial { get; }

    public IrInspectTally(IrLocation target, IrLocation counter,
        Semantics.Bound.InspectTallyKind kind, IrInspectPatternValue? pattern,
        IrInspectPatternValue? beforePattern, bool beforeInitial,
        IrInspectPatternValue? afterPattern, bool afterInitial)
    {
        Target = target; Counter = counter; Kind = kind; Pattern = pattern;
        BeforePattern = beforePattern; BeforeInitial = beforeInitial;
        AfterPattern = afterPattern; AfterInitial = afterInitial;
    }
}

public sealed class IrInspectReplace : IrInstruction
{
    public IrLocation Target { get; }
    public Semantics.Bound.InspectReplaceKind Kind { get; }
    public IrInspectPatternValue Pattern { get; }
    public IrInspectPatternValue Replacement { get; }
    public IrInspectPatternValue? BeforePattern { get; }
    public bool BeforeInitial { get; }
    public IrInspectPatternValue? AfterPattern { get; }
    public bool AfterInitial { get; }

    public IrInspectReplace(IrLocation target,
        Semantics.Bound.InspectReplaceKind kind,
        IrInspectPatternValue pattern, IrInspectPatternValue replacement,
        IrInspectPatternValue? beforePattern, bool beforeInitial,
        IrInspectPatternValue? afterPattern, bool afterInitial)
    {
        Target = target; Kind = kind; Pattern = pattern; Replacement = replacement;
        BeforePattern = beforePattern; BeforeInitial = beforeInitial;
        AfterPattern = afterPattern; AfterInitial = afterInitial;
    }
}

public sealed class IrInspectConvert : IrInstruction
{
    public IrLocation Target { get; }
    public IrInspectPatternValue FromSet { get; }
    public IrInspectPatternValue ToSet { get; }
    public IrInspectPatternValue? BeforePattern { get; }
    public bool BeforeInitial { get; }
    public IrInspectPatternValue? AfterPattern { get; }
    public bool AfterInitial { get; }

    public IrInspectConvert(IrLocation target,
        IrInspectPatternValue fromSet, IrInspectPatternValue toSet,
        IrInspectPatternValue? beforePattern, bool beforeInitial,
        IrInspectPatternValue? afterPattern, bool afterInitial)
    {
        Target = target; FromSet = fromSet; ToSet = toSet;
        BeforePattern = beforePattern; BeforeInitial = beforeInitial;
        AfterPattern = afterPattern; AfterInitial = afterInitial;
    }
}

// ── PIC-aware arithmetic ──

public sealed class IrPicMultiply : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiply(IrLocation left, IrLocation right,
        IrLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicMultiplyLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Other { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiplyLiteral(decimal value, IrLocation other,
        IrLocation dest, int rounding = 0)
    {
        Value = value; Other = other; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAdd : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAdd(IrLocation src, IrLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAddLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAddLiteral(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

public sealed class IrPicSubtract : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtract(IrLocation src, IrLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicSubtractLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtractLiteral(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

// ── Accumulator pattern for multi-operand ADD/SUBTRACT ──
// COBOL spec: "All operands preceding TO/FROM are summed, then the sum is applied to each target."

/// <summary>
/// Initialize a decimal accumulator local to zero.
/// </summary>
public sealed class IrInitAccumulator : IrInstruction
{
    public IrInitAccumulator(IrValue result)
    {
        Result = result;
    }
}

/// <summary>
/// Decode a field to decimal and add it to the accumulator.
/// </summary>
public sealed class IrAccumulateField : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Source { get; }

    public IrAccumulateField(IrValue accumulator, IrLocation source)
    {
        Accumulator = accumulator;
        Source = source;
    }
}

/// <summary>
/// Add a literal decimal to the accumulator.
/// </summary>
public sealed class IrAccumulateLiteral : IrInstruction
{
    public IrValue Accumulator { get; }
    public decimal Value { get; }

    public IrAccumulateLiteral(IrValue accumulator, decimal value)
    {
        Accumulator = accumulator;
        Value = value;
    }
}

/// <summary>
/// target = target + accumulator, with rounding and overflow detection.
/// </summary>
public sealed class IrAddAccumulatedToTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrAddAccumulatedToTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

/// <summary>
/// target = accumulator (GIVING form: store sum directly, don't add to current value).
/// </summary>
public sealed class IrMoveAccumulatedToTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrMoveAccumulatedToTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

/// <summary>
/// target = target - accumulator, with rounding and overflow detection.
/// </summary>
public sealed class IrSubtractAccumulatedFromTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrSubtractAccumulatedFromTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

public sealed class IrPicDivide : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivide(IrLocation left, IrLocation right,
        IrLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicDivideLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Other { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivideLiteral(decimal value, IrLocation other,
        IrLocation dest, int rounding = 0)
    {
        Value = value; Other = other; Destination = dest; Rounding = rounding;
    }
}

/// <summary>
/// COMPUTE: evaluate a bound expression tree and store the decimal result
/// into a target field with optional rounding and overflow detection.
/// </summary>
public sealed class IrComputeStore : IrInstruction
{
    public Semantics.Bound.BoundExpression Expression { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    /// <summary>
    /// Pre-resolved IrLocations for all data-reference leaf nodes in the Expression tree.
    /// Populated by the Binder so the emitter never needs to resolve locations itself.
    /// </summary>
    public IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation> ResolvedLocations { get; }

    public IrComputeStore(Semantics.Bound.BoundExpression expression,
        IrLocation dest, int rounding = 0,
        IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>? resolvedLocations = null)
    {
        Expression = expression;
        Destination = dest;
        Rounding = rounding;
        ResolvedLocations = resolvedLocations
            ?? (IReadOnlyDictionary<Semantics.Bound.BoundExpression, IrLocation>)
               new Dictionary<Semantics.Bound.BoundExpression, IrLocation>();
    }
}

/// <summary>
/// Class condition: IS NUMERIC, IS ALPHABETIC, etc.
/// Calls PicRuntime.IsNumericClass / IsAlphabeticClass / etc.
/// </summary>
public sealed class IrClassCondition : IrInstruction
{
    public IrLocation Subject { get; }
    public int ClassKind { get; }  // ClassConditionKind enum value

    public IrClassCondition(IrLocation subject, int classKind, IrValue result)
    {
        Subject = subject;
        ClassKind = classKind;
        Result = result;
    }
}

public sealed class IrPicCompare : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public int OperatorKind { get; }

    public IrPicCompare(IrLocation left, IrLocation right,
        IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

public sealed class IrPicCompareLiteral : IrInstruction
{
    public IrLocation Left { get; }
    public decimal Value { get; }
    public int OperatorKind { get; }

    public IrPicCompareLiteral(IrLocation left, decimal value,
        IrValue result, int operatorKind)
    {
        Left = left; Value = value; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare an alphanumeric field to a string literal. Result is bool.
/// </summary>
public sealed class IrStringCompareLiteral : IrInstruction
{
    public IrLocation Left { get; }
    public string Value { get; }
    public int OperatorKind { get; }

    public IrStringCompareLiteral(IrLocation left, string value,
        IrValue result, int operatorKind)
    {
        Left = left; Value = value; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Alphanumeric field-to-field comparison. Uses StorageHelpers.CompareFieldToField.
/// </summary>
public sealed class IrStringCompare : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public int OperatorKind { get; }

    public IrStringCompare(IrLocation left, IrLocation right,
        IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// One sending item in a STRING statement.
/// </summary>
public sealed class IrStringSending
{
    /// <summary>Literal value (non-null for literal sendings).</summary>
    public string? LiteralValue { get; }
    /// <summary>Field location (non-null for field sendings).</summary>
    public IrLocation? SourceLocation { get; }
    public string? Delimiter { get; }
    public bool DelimitedBySize { get; }

    public IrStringSending(string? literalValue, IrLocation? sourceLocation,
        string? delimiter, bool delimitedBySize)
    {
        LiteralValue = literalValue;
        SourceLocation = sourceLocation;
        Delimiter = delimiter;
        DelimitedBySize = delimitedBySize;
    }
}

/// <summary>
/// STRING statement: concatenate multiple sending items into a destination.
/// The emitter manages a single pointer local, initializes from PointerLocation
/// (or 1 if null), calls StringConcatLiteral/StringConcat per sending, and
/// writes the pointer back to PointerLocation (if non-null).
/// </summary>
public sealed class IrStringStatement : IrInstruction
{
    public IrLocation Destination { get; }
    public IReadOnlyList<IrStringSending> Sendings { get; }
    /// <summary>Null if no WITH POINTER clause.</summary>
    public IrLocation? PointerLocation { get; }

    public IrStringStatement(IrLocation dest, IReadOnlyList<IrStringSending> sendings,
        IrLocation? pointerLocation, IrValue overflowResult)
    {
        Destination = dest;
        Sendings = sendings;
        PointerLocation = pointerLocation;
        Result = overflowResult;
    }
}

/// <summary>
/// One INTO target in an UNSTRING statement.
/// </summary>
public sealed class IrUnstringInto
{
    public IrLocation Target { get; }
    public IrLocation? CountIn { get; }
    public IrLocation? DelimiterIn { get; }

    public IrUnstringInto(IrLocation target, IrLocation? countIn, IrLocation? delimiterIn)
    {
        Target = target;
        CountIn = countIn;
        DelimiterIn = delimiterIn;
    }
}

/// <summary>
/// UNSTRING statement: split a source string into multiple destination fields.
/// The emitter manages a shared pointer local, calls UnstringExtract per INTO,
/// handles COUNT IN / DELIMITER IN write-back, and writes pointer/tallying back.
/// </summary>
public sealed class IrUnstringStatement : IrInstruction
{
    public IrLocation Source { get; }
    public string? LiteralDelimiter { get; }
    public bool DelimitedByAll { get; }
    public IReadOnlyList<IrUnstringInto> Intos { get; }
    public IrLocation? PointerLocation { get; }
    public IrLocation? TallyingLocation { get; }

    public IrUnstringStatement(IrLocation source, string? literalDelimiter, bool delimitedByAll,
        IReadOnlyList<IrUnstringInto> intos, IrLocation? pointerLocation, IrLocation? tallyingLocation,
        IrValue overflowResult)
    {
        Source = source;
        LiteralDelimiter = literalDelimiter;
        DelimitedByAll = delimitedByAll;
        Intos = intos;
        PointerLocation = pointerLocation;
        TallyingLocation = tallyingLocation;
        Result = overflowResult;
    }
}

public sealed class IrPicMoveLiteralNumeric : IrInstruction
{
    public IrLocation Destination { get; }
    public decimal Value { get; }
    public int Rounding { get; }

    public IrPicMoveLiteralNumeric(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

// ── PIC-aware data movement ──

/// <summary>
/// PIC-aware field-to-field MOVE. Canonical primitive for all identifier→identifier
/// moves: regular MOVE, MOVE CORRESPONDING pairs, and SET TRUE/FALSE.
/// Carries resolved PIC descriptors — the emitter dispatches to the appropriate
/// PicRuntime helper based on source/destination categories.
/// </summary>
public sealed class IrMoveFieldToField : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public Runtime.PicDescriptor SourcePic { get; }
    public Runtime.PicDescriptor DestinationPic { get; }
    public bool IsRounded { get; }

    public IrMoveFieldToField(
        IrLocation source, IrLocation destination,
        Runtime.PicDescriptor sourcePic, Runtime.PicDescriptor destinationPic,
        bool isRounded = false)
    {
        Source = source;
        Destination = destination;
        SourcePic = sourcePic;
        DestinationPic = destinationPic;
        IsRounded = isRounded;
    }
}

// ── DISPLAY ──

/// <summary>
/// Represents a single DISPLAY operand: either a string literal or a field reference.
/// </summary>
public abstract class DisplayOperand { }

public sealed class DisplayLiteralOperand : DisplayOperand
{
    public string Value { get; }
    public DisplayLiteralOperand(string value) => Value = value;
}

public sealed class DisplayFieldOperand : DisplayOperand
{
    public IrLocation Location { get; }
    public DisplayFieldOperand(IrLocation location) => Location = location;
}

/// <summary>
/// DISPLAY statement: outputs concatenated operands (literals + field values) to console.
/// </summary>
public sealed class IrPicDisplay : IrInstruction
{
    public IReadOnlyList<DisplayOperand> Operands { get; }

    public IrPicDisplay(IReadOnlyList<DisplayOperand> operands)
    {
        Operands = operands;
    }
}

// ── I/O and runtime calls ──

public sealed class IrRuntimeCall : IrInstruction
{
    public string MethodName { get; }
    public IReadOnlyList<IrValue> Arguments { get; }

    public IrRuntimeCall(IrValue? result, string methodName, IReadOnlyList<IrValue> args)
    {
        Result = result;
        MethodName = methodName;
        Arguments = args;
    }
}
