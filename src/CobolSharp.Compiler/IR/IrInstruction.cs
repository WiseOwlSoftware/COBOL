// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// Base class for all IR instructions. CIL-friendly, COBOL-aware.
/// </summary>
public abstract class IrInstruction
{
    public IrValue? Result { get; protected set; }
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
public sealed class IrMoveStringToField : IrInstruction
{
    public CodeGen.StorageLocation Target { get; }
    public string Value { get; }  // embedded string — no IrValue dependency

    public IrMoveStringToField(CodeGen.StorageLocation target, string value)
    {
        Target = target;
        Value = value;
    }
}

/// <summary>
/// WRITE record — outputs record bytes from ProgramState to file.
/// </summary>
public sealed class IrWriteRecordFromStorage : IrInstruction
{
    public string FileName { get; }
    public CodeGen.StorageLocation Record { get; }

    public IrWriteRecordFromStorage(string fileName, CodeGen.StorageLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

// ── PIC-aware arithmetic ──

public sealed class IrPicMultiply : IrInstruction
{
    public CodeGen.StorageLocation Left { get; }
    public CodeGen.StorageLocation Right { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiply(CodeGen.StorageLocation left, CodeGen.StorageLocation right,
        CodeGen.StorageLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicMultiplyLiteral : IrInstruction
{
    public decimal Value { get; }
    public CodeGen.StorageLocation Other { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiplyLiteral(decimal value, CodeGen.StorageLocation other,
        CodeGen.StorageLocation dest, int rounding = 0)
    {
        Value = value; Other = other; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAdd : IrInstruction
{
    public CodeGen.StorageLocation Source { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAdd(CodeGen.StorageLocation src, CodeGen.StorageLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAddLiteral : IrInstruction
{
    public decimal Value { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAddLiteral(CodeGen.StorageLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

public sealed class IrPicSubtract : IrInstruction
{
    public CodeGen.StorageLocation Source { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtract(CodeGen.StorageLocation src, CodeGen.StorageLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicSubtractLiteral : IrInstruction
{
    public decimal Value { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtractLiteral(CodeGen.StorageLocation dest, decimal value, int rounding = 0)
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
    public CodeGen.StorageLocation Source { get; }

    public IrAccumulateField(IrValue accumulator, CodeGen.StorageLocation source)
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
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrAddAccumulatedToTarget(IrValue accumulator, CodeGen.StorageLocation dest, int rounding = 0)
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
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrSubtractAccumulatedFromTarget(IrValue accumulator, CodeGen.StorageLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

public sealed class IrPicDivide : IrInstruction
{
    public CodeGen.StorageLocation Left { get; }
    public CodeGen.StorageLocation Right { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivide(CodeGen.StorageLocation left, CodeGen.StorageLocation right,
        CodeGen.StorageLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicDivideLiteral : IrInstruction
{
    public decimal Value { get; }
    public CodeGen.StorageLocation Other { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivideLiteral(decimal value, CodeGen.StorageLocation other,
        CodeGen.StorageLocation dest, int rounding = 0)
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
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; }

    public IrComputeStore(Semantics.Bound.BoundExpression expression,
        CodeGen.StorageLocation dest, int rounding = 0)
    {
        Expression = expression;
        Destination = dest;
        Rounding = rounding;
    }
}

public sealed class IrPicCompare : IrInstruction
{
    public CodeGen.StorageLocation Left { get; }
    public CodeGen.StorageLocation Right { get; }
    public int OperatorKind { get; }

    public IrPicCompare(CodeGen.StorageLocation left, CodeGen.StorageLocation right,
        IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

public sealed class IrPicCompareLiteral : IrInstruction
{
    public CodeGen.StorageLocation Left { get; }
    public decimal Value { get; }
    public int OperatorKind { get; }

    public IrPicCompareLiteral(CodeGen.StorageLocation left, decimal value,
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
    public CodeGen.StorageLocation Left { get; }
    public string Value { get; }
    public int OperatorKind { get; }

    public IrStringCompareLiteral(CodeGen.StorageLocation left, string value,
        IrValue result, int operatorKind)
    {
        Left = left; Value = value; Result = result; OperatorKind = operatorKind;
    }
}

public sealed class IrPicMoveLiteralNumeric : IrInstruction
{
    public CodeGen.StorageLocation Destination { get; }
    public decimal Value { get; }
    public int Rounding { get; }

    public IrPicMoveLiteralNumeric(CodeGen.StorageLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

// ── PIC-aware data movement ──

/// <summary>
/// PIC-aware MOVE: the emitter calls the appropriate PicRuntime helper
/// based on source/destination PIC descriptors.
/// </summary>
public sealed class IrPicMove : IrInstruction
{
    public CodeGen.StorageLocation Source { get; }
    public CodeGen.StorageLocation Destination { get; }
    public int Rounding { get; } // 0=Truncate, 1=RoundHalfUp

    public IrPicMove(CodeGen.StorageLocation source, CodeGen.StorageLocation destination, int rounding = 0)
    {
        Source = source;
        Destination = destination;
        Rounding = rounding;
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
    public CodeGen.StorageLocation Location { get; }
    public DisplayFieldOperand(CodeGen.StorageLocation location) => Location = location;
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
