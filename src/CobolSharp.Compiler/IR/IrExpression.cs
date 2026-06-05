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
/// A COBOL-2002 user-defined function invocation (<c>FUNCTION user-name(args)</c>) in an expression context —
/// the function unit was compiled as a callable program with a PROCEDURE DIVISION RETURNING item. Emitted as a
/// call to <c>CobolProgramRegistry.InvokeNumericFunction</c>: the USING arguments are passed BY CONTENT and the
/// function's numeric result (decoded per <see cref="ReturnPic"/> from a scratch buffer of length
/// <see cref="ReturnLength"/>) is left on the IL stack like any other numeric expression. The whole-source
/// MOVE/COMPUTE form is handled separately (lowered directly to CALL … RETURNING the receiving item).
/// </summary>
public sealed class IrUserFunctionCall : IrExpression
{
    public string FunctionName { get; }
    public IReadOnlyList<IrUserFunctionArg> Arguments { get; }
    public int ReturnLength { get; }
    public Runtime.PicDescriptor ReturnPic { get; }

    public IrUserFunctionCall(string functionName, IReadOnlyList<IrUserFunctionArg> arguments,
        int returnLength, Runtime.PicDescriptor returnPic)
    {
        FunctionName = functionName;
        Arguments = arguments;
        ReturnLength = returnLength;
        ReturnPic = returnPic;
    }
}

/// <summary>One argument of an <see cref="IrUserFunctionCall"/>. Either a storage <see cref="Location"/> (passed
/// BY CONTENT), or a computed <see cref="Value"/> (a literal / arithmetic expression) which is encoded into the
/// target parameter's format (<see cref="ParamLength"/> + <see cref="ParamPic"/>) before being passed.</summary>
public sealed class IrUserFunctionArg
{
    public IrLocation? Location { get; }
    public IrExpression? Value { get; }
    public int ParamLength { get; }
    public Runtime.PicDescriptor? ParamPic { get; }

    public IrUserFunctionArg(IrLocation location) => Location = location;

    public IrUserFunctionArg(IrExpression value, int paramLength, Runtime.PicDescriptor paramPic)
    {
        Value = value;
        ParamLength = paramLength;
        ParamPic = paramPic;
    }
}

/// <summary>
/// The LINAGE-COUNTER special register value for a file: a runtime read of the file's current line
/// within the page body (ISO §8.4.3.14). Emitted as a call to FileRuntime.GetLinageCounter, leaving a
/// decimal on the stack like any other numeric expression.
/// </summary>
public sealed class IrLinageCounter : IrExpression
{
    public string FileName { get; }

    public IrLinageCounter(string fileName) => FileName = fileName;
}

/// <summary>The LINE-COUNTER special register (ISO §8.4.3.15): a runtime read of a report's current line,
/// emitted as a call to ReportWriterRuntime.GetLineCounter leaving a decimal on the IL stack.</summary>
public sealed class IrLineCounter : IrExpression
{
    public string ReportName { get; }
    public IrLineCounter(string reportName) => ReportName = reportName;
}

/// <summary>The PAGE-COUNTER special register (ISO §8.4.3.15): a runtime read of a report's current page,
/// emitted as a call to ReportWriterRuntime.GetPageCounter leaving a decimal on the IL stack.</summary>
public sealed class IrPageCounter : IrExpression
{
    public string ReportName { get; }
    public IrPageCounter(string reportName) => ReportName = reportName;
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
/// String-valued expression argument: an alphanumeric intrinsic-function call used as an
/// argument to another function (e.g. LOWER-CASE(FUNCTION LOWER-CASE("X"))). Evaluated to a
/// System.String at runtime — NOT a decimal — so it must not go through the numeric arg path.
/// </summary>
public sealed class IrStringExprArg : IrFunctionArg
{
    public IrExpression Expression { get; }

    public IrStringExprArg(IrExpression expression) => Expression = expression;
}

/// <summary>
/// Intrinsic function call: FUNCTION name(args).
/// Arguments are IrFunctionArg instances (numeric, alphanumeric, or string literal).
/// </summary>
public sealed class IrIntrinsicCall : IrExpression
{
    public string FunctionName { get; }
    public IReadOnlyList<IrFunctionArg> Arguments { get; }

    /// <summary>
    /// Program collating sequence (256-byte code→weight table) for collating-sensitive functions
    /// (CHAR, ORD); null = native ordinal order. Resolved at lowering time (ISO §15.15, §15.36).
    /// </summary>
    public byte[]? CollatingSequence { get; init; }

    public IrIntrinsicCall(string functionName, IReadOnlyList<IrFunctionArg> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
}
