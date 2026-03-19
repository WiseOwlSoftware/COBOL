// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Assigns byte offsets to all data items in working-storage and file section.
/// Populates SemanticModel.StorageLocations for the binder and codegen to use.
/// </summary>
public static class StorageLayoutComputer
{
    /// <summary>Minimum storage area size when no data items are declared.</summary>
    private const int MinimumAreaSize = 256;

    /// <summary>
    /// Walks all data items in declaration order and assigns each a
    /// <see cref="StorageLocation"/> (area, offset, length, PIC descriptor).
    /// Also records initial values and figurative constants for the initializer.
    /// File-section records share offset 0 (implicit REDEFINES per COBOL spec).
    /// </summary>
    public static void ComputeLayout(SemanticModel model)
    {
        int wsOffset = 0;
        int fsOffset = 0;

        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber is 1 or 77 && data.Area == StorageAreaKind.WorkingStorage)
                LayoutItem(data, StorageAreaKind.WorkingStorage, ref wsOffset, model);
        }

        // File section: all 01-level records under the same FD share the same record buffer
        // (implicit REDEFINES). Layout each at offset 0 and take the max size.
        int maxRecordSize = 0;
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber == 1 && data.Area == StorageAreaKind.FileSection)
            {
                int recOffset = 0;
                LayoutItem(data, StorageAreaKind.FileSection, ref recOffset, model);
                maxRecordSize = Math.Max(maxRecordSize, recOffset);
            }
        }
        fsOffset = maxRecordSize;

        model.WorkingStorageSize = wsOffset > 0 ? wsOffset : MinimumAreaSize;
        model.FileSectionSize = fsOffset > 0 ? fsOffset : MinimumAreaSize;
    }

    private static void LayoutItem(
        DataSymbol item,
        StorageAreaKind area,
        ref int offset,
        SemanticModel model)
    {
        if (item.Redefines != null)
        {
            LayoutRedefines(item, area, ref offset, model);
            return;
        }

        if (item.IsElementary)
            LayoutElementary(item, area, ref offset, model);
        else
            LayoutGroup(item, area, ref offset, model);
    }

    private static void LayoutRedefines(
        DataSymbol item,
        StorageAreaKind area,
        ref int offset,
        SemanticModel model)
    {
        var targetLoc = model.GetStorageLocation(item.Redefines!);
        if (!targetLoc.HasValue) return;

        int size = item.IsElementary ? FieldSizeCalculator.ComputeElementSize(item) : targetLoc.Value.Length;
        var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, size, model.PicEnvironment);
        model.RegisterStorageLocation(item, new StorageLocation(targetLoc.Value.Area, targetLoc.Value.Offset, size, pic));
        RegisterValue(model, item);

        // For group REDEFINES, recurse so nested items get storage locations
        if (item.Children.Count > 0)
        {
            int childOffset = targetLoc.Value.Offset;
            foreach (var child in item.Children)
                LayoutItem(child, area, ref childOffset, model);
        }
    }

    private static void LayoutElementary(
        DataSymbol item,
        StorageAreaKind area,
        ref int offset,
        SemanticModel model)
    {
        int elementSize = FieldSizeCalculator.ComputeElementSize(item);
        item.ElementSize = elementSize;
        int totalSize = elementSize * item.OccursCount;
        var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, totalSize, model.PicEnvironment);
        model.RegisterStorageLocation(item, new StorageLocation(area, offset, totalSize, pic));
        RegisterValue(model, item);
        offset += totalSize;
    }

    private static void LayoutGroup(
        DataSymbol item,
        StorageAreaKind area,
        ref int offset,
        SemanticModel model)
    {
        int groupStart = offset;

        if (item.Children.Count > 0)
        {
            foreach (var child in item.Children)
                LayoutItem(child, area, ref offset, model);

            int childrenSize = Math.Max(offset - groupStart, 1);
            item.ElementSize = childrenSize;
            int groupSize = childrenSize * item.OccursCount;
            if (item.OccursCount > 1)
                offset = groupStart + groupSize;
            var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, groupSize, model.PicEnvironment);
            model.RegisterStorageLocation(item, new StorageLocation(area, groupStart, groupSize, pic));
        }
        else
        {
            var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, 1, model.PicEnvironment);
            model.RegisterStorageLocation(item, new StorageLocation(area, offset, 1, pic));
            offset += 1;
        }

        RegisterValue(model, item);
    }

    private static void RegisterValue(SemanticModel model, DataSymbol data)
    {
        if (data.FigurativeInit.HasValue)
        {
            model.RegisterFigurativeInit(data, data.FigurativeInit.Value);
            if (data.InitialValue == null) return;
        }

        if (data.InitialValue == null) return;

        if (decimal.TryParse(data.InitialValue,
            System.Globalization.CultureInfo.InvariantCulture, out var numVal)
            && data.ResolvedType?.IsNumeric == true)
        {
            model.RegisterInitialValue(data, numVal, CobolCategory.Numeric);
        }
        else
        {
            model.RegisterInitialValue(data, data.InitialValue, CobolCategory.Alphanumeric);
        }
    }
}
