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
