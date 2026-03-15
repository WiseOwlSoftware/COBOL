// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Language-level type information for COBOL data items.
/// Separates "what kind of data" from "how it's stored."
/// </summary>
public interface ITypeSymbol
{
    string Name { get; }
    bool IsNumeric { get; }
    bool IsAlphanumeric { get; }
    bool IsBoolean { get; }
    PicLayout? Pic { get; }
    UsageKind Usage { get; }
}

// UsageKind is now in CobolSharp.Runtime (shared between compiler and runtime)

public enum PicCategory
{
    Numeric,
    Alphanumeric,
    National,
    Boolean,
    Edited,
    Unknown
}

/// <summary>
/// Decoded PIC string layout: category, length, scale, sign, editing.
/// </summary>
public sealed class PicLayout
{
    public PicCategory Category { get; }
    public int Length { get; }
    public int IntegerDigits { get; }
    public int FractionDigits { get; }
    public bool IsSigned { get; }
    public bool IsEdited { get; }

    public PicLayout(
        PicCategory category,
        int length,
        int integerDigits,
        int fractionDigits,
        bool isSigned,
        bool isEdited)
    {
        Category = category;
        Length = length;
        IntegerDigits = integerDigits;
        FractionDigits = fractionDigits;
        IsSigned = isSigned;
        IsEdited = isEdited;
    }
}

/// <summary>
/// Concrete type for a COBOL data item, carrying PIC layout and USAGE.
/// </summary>
public sealed class DataTypeSymbol : ITypeSymbol
{
    public string Name { get; }
    public bool IsNumeric { get; }
    public bool IsAlphanumeric { get; }
    public bool IsBoolean { get; }
    public PicLayout? Pic { get; }
    public UsageKind Usage { get; }

    public DataTypeSymbol(
        string name,
        bool isNumeric,
        bool isAlphanumeric,
        bool isBoolean,
        PicLayout? pic,
        UsageKind usage)
    {
        Name = name;
        IsNumeric = isNumeric;
        IsAlphanumeric = isAlphanumeric;
        IsBoolean = isBoolean;
        Pic = pic;
        Usage = usage;
    }
}

/// <summary>
/// Built-in primitive types for expressions and conditions.
/// </summary>
public static class BuiltinTypes
{
    public static readonly DataTypeSymbol Numeric =
        new("NUMERIC", isNumeric: true, isAlphanumeric: false, isBoolean: false, pic: null, UsageKind.Display);
    public static readonly DataTypeSymbol Boolean =
        new("BOOLEAN", isNumeric: false, isAlphanumeric: false, isBoolean: true, pic: null, UsageKind.Display);
    public static readonly DataTypeSymbol Alphanumeric =
        new("ALPHANUMERIC", isNumeric: false, isAlphanumeric: true, isBoolean: false, pic: null, UsageKind.Display);
    public static readonly DataTypeSymbol Unknown =
        new("UNKNOWN", isNumeric: false, isAlphanumeric: false, isBoolean: false, pic: null, UsageKind.Unknown);
}
