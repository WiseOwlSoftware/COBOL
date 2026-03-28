// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Compiler.IR;

/// <summary>
/// Base class for all types in the intermediate representation.
/// Each IR type corresponds to a COBOL data description and carries
/// enough layout information to emit CIL with explicit byte offsets.
/// </summary>
public abstract class IrType(string name)
{
    /// <summary>The CIL type name used during code generation.</summary>
    public string Name { get; } = name;
}

/// <summary>
/// A COBOL 01-level record mapped to a .NET struct with StructLayout.Explicit.
/// Fields carry byte offsets so the CIL emitter can overlay them exactly
/// as COBOL's contiguous-storage model requires.
/// </summary>
public sealed class IrRecordType(string name) : IrType(name)
{
    /// <summary>All elementary fields in declaration order, each with a byte offset.</summary>
    public List<IrField> Fields { get; } = [];

    /// <summary>Total byte size of the record, including REDEFINES overlaps and OCCURS expansion.</summary>
    public int TotalSize { get; set; }
}

/// <summary>
/// Flyweight singletons for the primitive .NET types used in CIL emission.
/// Elementary COBOL items resolve to one of these based on PIC category and USAGE.
/// </summary>
public sealed class IrPrimitiveType : IrType
{
    public static readonly IrPrimitiveType Int32 = new("int32");
    public static readonly IrPrimitiveType Int64 = new("int64");
    public static readonly IrPrimitiveType Float32 = new("float32");
    public static readonly IrPrimitiveType Float64 = new("float64");
    public static readonly IrPrimitiveType Decimal = new("decimal");
    public static readonly IrPrimitiveType String = new("string");
    public static readonly IrPrimitiveType Bool = new("bool");
    public static readonly IrPrimitiveType Void = new("void");
    /// <summary>Used for alphanumeric fields and any field stored as raw bytes in the record buffer.</summary>
    public static readonly IrPrimitiveType ByteArray = new("byte[]");

    private IrPrimitiveType(string name) : base(name) { }
}

/// <summary>
/// A field in an <see cref="IrRecordType"/>. Carries the byte offset and size
/// needed to emit [FieldOffset] attributes in the explicit-layout CIL struct.
/// </summary>
/// <param name="Name">COBOL data-name (used as the CIL field name).</param>
/// <param name="FieldType">The IR type this field maps to (primitive or nested record).</param>
/// <param name="Offset">Byte offset from the start of the containing record.</param>
/// <param name="Size">Total byte size, including OCCURS expansion.</param>
public sealed record IrField(string Name, IrType FieldType, int Offset, int Size);
