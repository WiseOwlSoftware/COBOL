// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// Result of laying out a COBOL record: an IrRecordType with concrete
/// byte offsets for every field.
/// </summary>
public sealed class RecordLayout
{
    public IrRecordType RecordType { get; }
    public int Size => RecordType.TotalSize;

    public RecordLayout(IrRecordType recordType)
    {
        RecordType = recordType;
    }
}

/// <summary>
/// Takes DataSymbol trees (01-level records from WORKING-STORAGE, etc.)
/// and produces IrRecordType with byte-accurate field offsets.
///
/// Storage rules (COBOL-standard):
///   DISPLAY numeric:   1 byte per digit/char
///   Alphanumeric:      1 byte per char
///   COMP / BINARY:     1-4 digits → 2 bytes, 5-9 → 4 bytes, 10-18 → 8 bytes
///   COMP-3 / PACKED:   (digits + 2) / 2 bytes (sign nibble)
///   COMP-1:            4 bytes (float)
///   COMP-2:            8 bytes (double)
///   Groups:            sum of children
///   REDEFINES:         shares offset with target, record size = max
///   OCCURS:            elementSize × count
/// </summary>
public sealed class RecordLayoutBuilder
{
    public RecordLayout Build(DataSymbol recordSymbol)
    {
        var irRecord = new IrRecordType(recordSymbol.Name);
        int size = LayoutChildren(recordSymbol, irRecord, baseOffset: 0);
        irRecord.TotalSize = size;
        return new RecordLayout(irRecord);
    }

    private int LayoutChildren(DataSymbol parent, IrRecordType irRecord, int baseOffset)
    {
        int current = baseOffset;
        int maxEnd = baseOffset;

        foreach (var child in parent.Children)
        {
            if (child.Redefines != null)
            {
                // REDEFINES: same offset as target
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

    private int LayoutOne(DataSymbol symbol, IrRecordType irRecord, int offset)
    {
        bool isGroup = symbol.Children.Count > 0 && symbol.PicString == null;

        if (isGroup)
        {
            int groupSize = LayoutChildren(symbol, irRecord, offset);
            int occurs = GetOccursCount(symbol);
            if (occurs > 1)
                groupSize *= occurs;
            return groupSize;
        }

        // Elementary item
        int elemSize = ComputeStorageSize(symbol);
        int occurs2 = GetOccursCount(symbol);
        int totalSize = elemSize * Math.Max(1, occurs2);

        var fieldType = MapToIrType(symbol, elemSize);
        var field = new IrField(symbol.Name, fieldType, offset, totalSize);
        irRecord.Fields.Add(field);

        return totalSize;
    }

    private static int FindFieldOffset(IrRecordType record, string name)
    {
        var f = record.Fields.FirstOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        return f?.Offset ?? 0;
    }

    private static int GetOccursCount(DataSymbol s)
    {
        // TODO: extract OCCURS count from data description
        // For now, return 1 (no OCCURS)
        return 1;
    }

    private static int ComputeStorageSize(DataSymbol s)
    {
        var type = s.ResolvedType;
        var pic = type?.Pic;

        if (pic == null)
            return 1; // unknown — minimum 1 byte

        if (pic.Category == CobolCategory.Alphanumeric || pic.Category == CobolCategory.National)
            return pic.Length;

        if (s.Usage == UsageKind.Display && pic.Category == CobolCategory.Numeric)
            return pic.Length + (pic.IsSigned ? 1 : 0);

        return s.Usage switch
        {
            UsageKind.Comp or UsageKind.Binary => ComputeBinarySize(pic.IntegerDigits + pic.FractionDigits),
            UsageKind.Comp3 or UsageKind.PackedDecimal => (pic.IntegerDigits + pic.FractionDigits + 2) / 2,
            UsageKind.Comp1 => 4,
            UsageKind.Comp2 => 8,
            _ => pic.Length > 0 ? pic.Length : 1
        };
    }

    private static int ComputeBinarySize(int digits)
    {
        if (digits <= 4) return 2;
        if (digits <= 9) return 4;
        return 8;
    }

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
