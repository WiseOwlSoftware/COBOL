// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// A diagnosable internal runtime error: a runtime support routine was invoked with malformed arguments
/// (a null buffer, a negative or out-of-range offset/length, or an uninitialized SORT/MERGE file). This
/// indicates a CobolSharp code-generation defect, not a defect in the COBOL program — so it carries the
/// failing operation and target for a clear message, instead of surfacing as an opaque framework
/// <see cref="ArgumentException"/> / <see cref="NullReferenceException"/> / <see cref="IndexOutOfRangeException"/>.
///
/// Distinct from <see cref="StopRunException"/> (normal STOP RUN unwinding) and from legitimate COBOL
/// conditions (FILE STATUS, INVALID KEY, AT END, ON SIZE ERROR), which are modeled as data and are never
/// exceptions.
/// </summary>
public sealed class CobolRuntimeException : Exception
{
    /// <summary>Short runtime diagnostic code (RT####), parallel to the compiler's CBL####/COBOL#### codes.</summary>
    public string Code { get; }

    /// <summary>The COBOL operation in progress (e.g. "WRITE", "READ", "numeric-decode", "RELEASE").</summary>
    public string Operation { get; }

    /// <summary>The file- or field-name involved, when known.</summary>
    public string? Target { get; }

    public CobolRuntimeException(string code, string operation, string? target, string reason)
        : base($"{code}: internal runtime error during {operation}"
               + (target is null ? "" : $" on '{target}'") + $": {reason}")
    {
        Code = code;
        Operation = operation;
        Target = target;
    }
}
