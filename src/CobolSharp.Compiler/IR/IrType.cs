// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// IR type — maps COBOL data descriptions to layout-aware types.
/// </summary>
public abstract class IrType
{
    public string Name { get; }
    protected IrType(string name) => Name = name;
}

/// <summary>
/// COBOL record (01-level group) → explicit-layout .NET type.
/// </summary>
public sealed class IrRecordType : IrType
{
    public List<IrField> Fields { get; } = new();
    public int TotalSize { get; set; }

    public IrRecordType(string name) : base(name) { }
}

/// <summary>
/// Primitive IR types that map to .NET built-in types.
/// </summary>
public sealed class IrPrimitiveType : IrType
{
    public static readonly IrPrimitiveType Int32 = new("int32");
    public static readonly IrPrimitiveType Int64 = new("int64");
    public static readonly IrPrimitiveType Decimal = new("decimal");
    public static readonly IrPrimitiveType String = new("string");
    public static readonly IrPrimitiveType Bool = new("bool");
    public static readonly IrPrimitiveType Void = new("void");
    public static readonly IrPrimitiveType ByteArray = new("byte[]");

    private IrPrimitiveType(string name) : base(name) { }
}

/// <summary>
/// A field in an IR record type. Carries byte offset and size
/// for explicit layout mapping to CIL.
/// </summary>
public sealed class IrField
{
    public string Name { get; }
    public IrType FieldType { get; }
    public int Offset { get; }
    public int Size { get; }

    public IrField(string name, IrType fieldType, int offset, int size)
    {
        Name = name;
        FieldType = fieldType;
        Offset = offset;
        Size = size;
    }
}
