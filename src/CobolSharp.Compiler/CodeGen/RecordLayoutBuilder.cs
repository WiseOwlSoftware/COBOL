// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// The product of <see cref="RecordLayoutBuilder"/>: an <see cref="IrRecordType"/>
/// whose fields have been assigned concrete byte offsets following COBOL storage rules.
/// </summary>
/// <param name="RecordType">The fully laid-out IR record type.</param>
public sealed record RecordLayout(IrRecordType RecordType)
{
    /// <summary>Total byte size of the record (convenience wrapper).</summary>
    public int Size => RecordType.TotalSize;
}

/// <summary>
/// Walks a <see cref="DataSymbol"/> tree (01-level record) and assigns byte offsets
/// to every elementary item, producing an <see cref="IrRecordType"/> suitable for
/// explicit-layout CIL emission.
///
/// Storage rules follow the COBOL standard:
///   DISPLAY numeric:   1 byte per digit/char
///   Alphanumeric:      1 byte per char
///   COMP / BINARY:     1-4 digits = 2 bytes, 5-9 = 4 bytes, 10-18 = 8 bytes
///   COMP-3 / PACKED:   (digits + 2) / 2 bytes (sign nibble)
///   COMP-1:            4 bytes (float)
///   COMP-2:            8 bytes (double)
///   Groups:            sum of children
///   REDEFINES:         shares offset with target, record size = max
///   OCCURS:            elementSize x count
/// </summary>
public sealed class RecordLayoutBuilder
{
    /// <summary>
    /// Builds a <see cref="RecordLayout"/> from a 01-level record symbol,
    /// recursively assigning byte offsets to all children.
    /// </summary>
    public RecordLayout Build(DataSymbol recordSymbol)
    {
        var irRecord = new IrRecordType(recordSymbol.Name);
        int size = LayoutChildren(recordSymbol, irRecord, baseOffset: 0);
        irRecord.TotalSize = size;
        return new RecordLayout(irRecord);
    }

    /// <summary>
    /// Lays out all children of a group item sequentially, handling REDEFINES
    /// by reusing the target field's offset. Returns the total byte span consumed.
    /// </summary>
    private int LayoutChildren(DataSymbol parent, IrRecordType irRecord, int baseOffset)
    {
        int current = baseOffset;
        // maxEnd tracks the highest byte touched, which may exceed current due to REDEFINES
        int maxEnd = baseOffset;

        foreach (var child in parent.Children)
        {
            if (child.Redefines != null)
            {
                int redefOffset = FindFieldOffset(irRecord, child.Redefines.Name);
                int redefSize = LayoutOne(child, irRecord, redefOffset);
                maxEnd = Math.Max(maxEnd, redefOffset + redefSize);
            }
            else
            {
                int fieldSize = LayoutOne(child, irRecord, current);
                current += fieldSize;
                maxEnd = Math.Max(maxEnd, current);
            }
        }

        return maxEnd - baseOffset;
    }

    /// <summary>Lays out a single data item (group or elementary) and returns its total byte size.</summary>
    private int LayoutOne(DataSymbol symbol, IrRecordType irRecord, int offset)
    {
        bool isGroup = symbol.Children.Count > 0 && symbol.PicString == null;

        if (isGroup)
        {
            int groupSize = LayoutChildren(symbol, irRecord, offset);
            symbol.ElementSize = groupSize;
            if (symbol.OccursCount > 1)
                groupSize *= symbol.OccursCount;
            return groupSize;
        }

        // Elementary item
        int elemSize = FieldSizeCalculator.ComputeElementSize(symbol);
        symbol.ElementSize = elemSize;
        int totalSize = elemSize * Math.Max(1, symbol.OccursCount);

        var fieldType = MapToIrType(symbol, elemSize);
        var field = new IrField(symbol.Name, fieldType, offset, totalSize);
        irRecord.Fields.Add(field);

        return totalSize;
    }

    /// <summary>Looks up a previously laid-out field's offset by name (case-insensitive, per COBOL rules).</summary>
    private static int FindFieldOffset(IrRecordType record, string name)
    {
        var f = record.Fields.FirstOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        return f?.Offset ?? 0;
    }

    /// <summary>
    /// Maps a COBOL elementary item to its IR primitive type based on PIC category,
    /// USAGE clause, and storage size. This determines what CIL type the field
    /// will be declared as in the explicit-layout struct.
    /// </summary>
    private static IrType MapToIrType(DataSymbol s, int elemSize)
    {
        var type = s.ResolvedType;
        var pic = type?.Pic;

        if (pic == null)
            return IrPrimitiveType.ByteArray;

        if (pic.Category == CobolCategory.Alphanumeric || pic.Category == CobolCategory.National)
            return IrPrimitiveType.ByteArray;

        if (s.Usage == UsageKind.Display && pic.Category == CobolCategory.Numeric)
        {
            if (pic.FractionDigits > 0)
                return IrPrimitiveType.Decimal;
            return elemSize switch
            {
                <= 2 => IrPrimitiveType.Int32, // Int16 mapped to Int32 for CIL simplicity
                <= 4 => IrPrimitiveType.Int32,
                _ => IrPrimitiveType.Int64
            };
        }

        return s.Usage switch
        {
            UsageKind.Comp or UsageKind.Binary => elemSize switch
            {
                <= 2 => IrPrimitiveType.Int32,
                <= 4 => IrPrimitiveType.Int32,
                _ => IrPrimitiveType.Int64
            },
            UsageKind.Comp3 or UsageKind.PackedDecimal => IrPrimitiveType.Decimal,
            UsageKind.Comp1 => IrPrimitiveType.Int32, // float mapped via Int32 for now
            UsageKind.Comp2 => IrPrimitiveType.Int64, // double mapped via Int64 for now
            _ => IrPrimitiveType.ByteArray
        };
    }
}
