// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolSharp.Compiler.IR;

// ── Enums ──

/// <summary>
/// Arithmetic operators for IR expression trees.
/// Replaces the use of BoundBinaryOperatorKind in the IR layer.
/// Named IrArithmeticOp to avoid collision with IrBinaryOp (register-level ops).
/// </summary>
public enum IrArithmeticOp { Add, Subtract, Multiply, Divide, Remainder, Power }

/// <summary>Unary operators for IR expressions.</summary>
public enum IrUnaryOp { Negate }

/// <summary>
/// Comparison operators for IR-level numeric and string comparisons.
/// Values match BoundBinaryOperatorKind so existing (int) casts in the Binder are stable.
/// </summary>
public enum IrCompareOp
{
    Equal = 5, NotEqual = 6,
    Less = 7, LessOrEqual = 8,
    Greater = 9, GreaterOrEqual = 10,
}

/// <summary>INSPECT TALLYING match kind (moved from Semantics.Bound).</summary>
public enum InspectTallyKind { All, Leading, Characters }

/// <summary>INSPECT REPLACING match kind (moved from Semantics.Bound).</summary>
public enum InspectReplaceKind { All, First, Leading, Characters }

/// <summary>IS NUMERIC / IS ALPHABETIC class condition kind (moved from Semantics.Bound).</summary>
public enum ClassConditionKind
{
    Numeric,
    Alphabetic,
    AlphabeticLower,
    AlphabeticUpper,
}

// ── Expression hierarchy ──

/// <summary>
/// Base type for IR-native expressions. Replaces BoundExpression in the IR layer.
/// All arithmetic evaluation, subscript computation, ref-mod boundaries, loop counts,
/// and function arguments will be expressed within this hierarchy.
/// </summary>
public abstract class IrExpression { }

/// <summary>
/// A compile-time numeric constant.
/// Covers numeric literals, figurative constants resolved to decimal,
/// and pre-negated literals.
/// </summary>
public sealed class IrLiteral : IrExpression
{
    public decimal Value { get; }

    public IrLiteral(decimal value) => Value = value;
}

/// <summary>
/// Load the numeric value of a COBOL data item at runtime.
/// The IrLocation is fully resolved (static, element, ref-mod, or cached).
/// This replaces the ResolvedLocations sidecar — location resolution
/// is embedded in the node itself.
/// </summary>
public sealed class IrLoadNumeric : IrExpression
{
    public IrLocation Source { get; }

    public IrLoadNumeric(IrLocation source) => Source = source;
}

/// <summary>
/// Binary arithmetic operation: left op right.
/// Covers Add, Subtract, Multiply, Divide, Remainder, Power.
/// </summary>
public sealed class IrBinaryExpr : IrExpression
{
    public IrArithmeticOp Op { get; }
    public IrExpression Left { get; }
    public IrExpression Right { get; }

    public IrBinaryExpr(IrArithmeticOp op, IrExpression left, IrExpression right)
    {
        Op = op;
        Left = left;
        Right = right;
    }
}

/// <summary>
/// Unary arithmetic operation (currently: negate only).
/// </summary>
public sealed class IrUnaryExpr : IrExpression
{
    public IrUnaryOp Op { get; }
    public IrExpression Operand { get; }

    public IrUnaryExpr(IrUnaryOp op, IrExpression operand)
    {
        Op = op;
        Operand = operand;
    }
}

// ── Intrinsic function arguments ──

/// <summary>
/// An argument to an intrinsic function call.
/// Distinguishes numeric expressions (evaluated to decimal) from
/// alphanumeric fields (read as strings) and string literals.
/// </summary>
public abstract class IrFunctionArg { }

/// <summary>Numeric argument: an IrExpression evaluated to decimal.</summary>
public sealed class IrNumericArg : IrFunctionArg
{
    public IrExpression Expression { get; }

    public IrNumericArg(IrExpression expression) => Expression = expression;
}

/// <summary>Alphanumeric field argument: read as a string at runtime.</summary>
public sealed class IrAlphanumericArg : IrFunctionArg
{
    public IrLocation Source { get; }

    public IrAlphanumericArg(IrLocation source) => Source = source;
}

/// <summary>String literal argument: a compile-time constant string.</summary>
public sealed class IrLiteralStringArg : IrFunctionArg
{
    public string Value { get; }

    public IrLiteralStringArg(string value) => Value = value;
}

/// <summary>
/// Intrinsic function call: FUNCTION name(args).
/// Arguments are IrFunctionArg instances (numeric, alphanumeric, or string literal).
/// </summary>
public sealed class IrIntrinsicCall : IrExpression
{
    public string FunctionName { get; }
    public IReadOnlyList<IrFunctionArg> Arguments { get; }

    public IrIntrinsicCall(string functionName, IReadOnlyList<IrFunctionArg> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
}
