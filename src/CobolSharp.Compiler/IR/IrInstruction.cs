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

public sealed class IrReturn : IrInstruction
{
    public IrValue? Value { get; }
    public IrReturn(IrValue? value) => Value = value;
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
