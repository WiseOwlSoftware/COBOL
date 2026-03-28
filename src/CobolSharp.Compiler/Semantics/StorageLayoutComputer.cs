// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Assigns byte offsets to all data items in working-storage and file section.
/// Populates SemanticModel.StorageLocations for the binder and codegen to use.
///
/// REDEFINES family handling:
/// Each REDEFINES group gets its OWN declared size (computed from children).
/// A RedefinesFamily tracks the max extent across the original + all REDEFINES.
/// The NEXT sibling starts at the family's max extent, not the original's end.
/// This ensures REDEFINES larger than the original don't overlap with subsequent items.
/// </summary>
public static class StorageLayoutComputer
{
    /// <summary>Minimum storage area size when no data items are declared.</summary>
    private const int MinimumAreaSize = 256;

    /// <summary>
    /// Tracks a REDEFINES family during layout: the original item and all items
    /// that REDEFINE it. Computes the max extent so the next sibling starts
    /// after the largest member.
    /// </summary>
    private sealed class RedefinesFamily
    {
        public DataSymbol Original { get; }
        public int BaseOffset { get; }
        private int _maxEnd;

        public RedefinesFamily(DataSymbol original, int baseOffset, int originalLength)
        {
            Original = original;
            BaseOffset = baseOffset;
            _maxEnd = baseOffset + originalLength;
        }

        public void AddMember(int declaredLength)
        {
            int end = BaseOffset + declaredLength;
            if (end > _maxEnd)
                _maxEnd = end;
        }

        /// <summary>Offset where the next sibling after this family must start.</summary>
        public int NextSiblingOffset => _maxEnd;
    }

    public static void ComputeLayout(SemanticModel model)
    {
        // Working storage: layout 01/77-level items with REDEFINES family tracking
        int wsOffset = 0;
        RedefinesFamily? currentFamily = null;

        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber is not (1 or 77) || data.Area != StorageAreaKind.WorkingStorage)
                continue;

            if (data.Redefines != null)
            {
                // This item REDEFINES another — add to current family
                LayoutRedefines(data, StorageAreaKind.WorkingStorage, ref wsOffset, model);
                var selfLoc = model.GetStorageLocation(data);
                if (selfLoc.HasValue)
                    currentFamily?.AddMember(selfLoc.Value.Length);
            }
            else
            {
                // Not a REDEFINES — close any open family first
                if (currentFamily != null)
                {
                    wsOffset = currentFamily.NextSiblingOffset;
                    currentFamily = null;
                }

                int itemStart = wsOffset;
                LayoutItem(data, StorageAreaKind.WorkingStorage, ref wsOffset, model);
                int itemSize = wsOffset - itemStart;

                // Check if subsequent items might REDEFINE this one — start a family
                // We always start a family for 01-level groups; if no REDEFINES follow,
                // CloseFamily just returns the same offset.
                if (data.LevelNumber == 1)
                    currentFamily = new RedefinesFamily(data, itemStart, itemSize);
            }
        }

        // Close any trailing family
        if (currentFamily != null)
            wsOffset = currentFamily.NextSiblingOffset;

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

        // Level-66 RENAMES: alias an existing contiguous byte range.
        // Must run after all records are laid out so FROM/THRU have storage locations.
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber != 66 || data.Renames == null) continue;
            LayoutRenames(data, model);
        }

        // Local storage: separate offset namespace from working storage (§13.8).
        // LOCAL-STORAGE items are re-initialized on each program invocation.
        int lsOffset = 0;
        RedefinesFamily? currentLsFamily = null;

        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber is not (1 or 77) || data.Area != StorageAreaKind.LocalStorage)
                continue;

            if (data.Redefines != null)
            {
                LayoutRedefines(data, StorageAreaKind.LocalStorage, ref lsOffset, model);
                var selfLoc = model.GetStorageLocation(data);
                if (selfLoc.HasValue)
                    currentLsFamily?.AddMember(selfLoc.Value.Length);
            }
            else
            {
                if (currentLsFamily != null)
                {
                    lsOffset = currentLsFamily.NextSiblingOffset;
                    currentLsFamily = null;
                }

                int itemStart = lsOffset;
                LayoutItem(data, StorageAreaKind.LocalStorage, ref lsOffset, model);
                int itemSize = lsOffset - itemStart;

                if (data.LevelNumber == 1)
                    currentLsFamily = new RedefinesFamily(data, itemStart, itemSize);
            }
        }

        if (currentLsFamily != null)
            lsOffset = currentLsFamily.NextSiblingOffset;

        // Linkage section: each 01-level item gets its own layout starting at offset 0.
        // LINKAGE items don't share a contiguous buffer — each is backed by a separate
        // CobolDataPointer passed via CALL USING. The offset is relative to the parameter.
        int linkageSize = 0;
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber is not (1 or 77) || data.Area != StorageAreaKind.LinkageSection)
                continue;
            int itemOffset = 0;
            LayoutItem(data, StorageAreaKind.LinkageSection, ref itemOffset, model);
            linkageSize = Math.Max(linkageSize, itemOffset);
        }

        model.WorkingStorageSize = wsOffset > 0 ? wsOffset : MinimumAreaSize;
        model.FileSectionSize = maxRecordSize > 0 ? maxRecordSize : MinimumAreaSize;
        model.LinkageSectionSize = linkageSize;
        model.LocalStorageSize = lsOffset;
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

        if (item.IsElementary)
        {
            int size = FieldSizeCalculator.ComputeElementSize(item);
            var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, size, model.PicEnvironment);
            model.RegisterStorageLocation(item,
                new StorageLocation(targetLoc.Value.Area, targetLoc.Value.Offset, size, pic));
        }
        else
        {
            // Group REDEFINES: compute OWN declared size from children.
            int childOffset = targetLoc.Value.Offset;
            foreach (var child in item.Children)
                LayoutItem(child, area, ref childOffset, model);

            int groupSize = Math.Max(childOffset - targetLoc.Value.Offset, 1);
            item.ElementSize = groupSize;
            var pic = CompilerPicDescriptorFactory.FromDataSymbol(item, groupSize, model.PicEnvironment);
            model.RegisterStorageLocation(item,
                new StorageLocation(targetLoc.Value.Area, targetLoc.Value.Offset, groupSize, pic));
        }

        RegisterValue(model, item);
    }

    private static void LayoutElementary(
        DataSymbol item,
        StorageAreaKind area,
        ref int offset,
        SemanticModel model)
    {
        int elementSize = FieldSizeCalculator.ComputeElementSize(item);
        item.ElementSize = elementSize;

        // SYNCHRONIZED (§13.18.55): align to natural boundary.
        // Slack bytes are inserted before the item to reach the alignment boundary.
        // 2-byte items → half-word (2), 4-byte items → word (4), 8-byte items → doubleword (8).
        if (item.IsSynchronized && elementSize >= 2)
        {
            int alignment = elementSize switch
            {
                <= 2 => 2,
                <= 4 => 4,
                _ => 8
            };
            int remainder = offset % alignment;
            if (remainder != 0)
                offset += alignment - remainder;
        }

        int totalSize = elementSize * (item.Occurs?.MaxOccurs ?? 1);
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
            int maxOccurs = item.Occurs?.MaxOccurs ?? 1;
            int groupSize = childrenSize * maxOccurs;
            if (maxOccurs > 1)
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

    /// <summary>
    /// Level-66 RENAMES: creates an alias for a contiguous byte range from the
    /// FROM item through the THRU item (or just FROM if no THRU). No additional
    /// storage is consumed — the RENAMES item overlays existing fields.
    /// </summary>
    private static void LayoutRenames(DataSymbol data, SemanticModel model)
    {
        var renames = data.Renames!;
        if (renames.FromSymbol == null) return;

        var fromLoc = model.GetStorageLocation(renames.FromSymbol);
        if (!fromLoc.HasValue) return;

        int startOffset = fromLoc.Value.Offset;
        int endOffset;

        if (renames.ThruSymbol != null)
        {
            var thruLoc = model.GetStorageLocation(renames.ThruSymbol);
            if (!thruLoc.HasValue) return;
            endOffset = thruLoc.Value.Offset + thruLoc.Value.Length;
        }
        else
        {
            endOffset = fromLoc.Value.Offset + fromLoc.Value.Length;
        }

        int totalLength = endOffset - startOffset;
        if (totalLength <= 0) return;

        data.ElementSize = totalLength;
        data.Area = fromLoc.Value.Area;

        Runtime.PicDescriptor pic;
        if (renames.ThruSymbol == null && renames.FromSymbol.IsElementary && renames.FromSymbol.ResolvedType != null)
        {
            // Single-field RENAMES: inherit the source field's PIC and category
            data.ResolvedType = renames.FromSymbol.ResolvedType;
            pic = fromLoc.Value.Pic;
        }
        else
        {
            // THRU range or group source: alphanumeric group-like byte range
            pic = CompilerPicDescriptorFactory.FromDataSymbol(data, totalLength, model.PicEnvironment);
        }
        model.RegisterStorageLocation(data,
            new StorageLocation(fromLoc.Value.Area, startOffset, totalLength, pic));
    }

    private static void RegisterValue(SemanticModel model, DataSymbol data)
    {
        if (data.FigurativeInit.HasValue)
        {
            model.RegisterFigurativeInit(data, data.FigurativeInit.Value);
            if (data.InitialValue == null) return;
        }

        if (data.InitialValue == null) return;

        // Expand ALL literal pattern to fill the field width by repeating the pattern
        string initVal = data.InitialValue;
        if (data.AllLiteralPattern != null && data.ElementSize > 0)
        {
            int fieldWidth = data.ElementSize;
            string pattern = data.AllLiteralPattern;
            if (pattern.Length > 0 && fieldWidth > pattern.Length)
            {
                var sb = new System.Text.StringBuilder(fieldWidth);
                while (sb.Length < fieldWidth)
                    sb.Append(pattern);
                initVal = sb.ToString(0, fieldWidth);
            }
        }

        if (decimal.TryParse(initVal,
            System.Globalization.CultureInfo.InvariantCulture, out var numVal)
            && data.ResolvedType?.IsNumeric == true)
        {
            model.RegisterInitialValue(data, numVal, CobolCategory.Numeric);
        }
        else
        {
            model.RegisterInitialValue(data, initVal, CobolCategory.Alphanumeric);
        }
    }
}
