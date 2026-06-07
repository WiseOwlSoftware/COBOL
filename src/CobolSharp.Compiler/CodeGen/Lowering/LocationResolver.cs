// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen.Lowering;

/// <summary>
/// Resolves DataSymbol and BoundIdentifierExpression to IrLocation,
/// handling subscripts (1D-3D OCCURS), reference modification, and
/// compile-time constant folding of subscript offsets.
/// </summary>
internal sealed class LocationResolver
{
    private readonly LoweringContext _ctx;

    public LocationResolver(LoweringContext ctx) => _ctx = ctx;

    /// <summary>
    /// Resolve a BoundIdentifierExpression to an IrLocation.
    /// - Non-subscripted: returns IrStaticLocation (compile-time offset).
    /// - All-constant subscripts: folds to IrStaticLocation (compile-time offset).
    /// - Any variable subscript: returns IrElementRef (runtime offset computation).
    /// Supports 1D, 2D, and 3D OCCURS (COBOL-85 max 3 dimensions).
    /// Returns null only if the symbol has no registered storage location.
    /// </summary>
    public IrLocation? ResolveLocation(BoundIdentifierExpression id, bool receiving = false)
    {
        var baseLoc = _ctx.Semantic.GetStorageLocation(id.Symbol);
        if (!baseLoc.HasValue) return null;

        // Non-subscripted: whole-item reference (static, or ODO-variable-length group).
        if (!id.IsSubscripted)
            return ResolveWholeItem(id.Symbol, baseLoc.Value, receiving);

        // Collect OCCURS dimension info by walking the symbol tree
        // from the item upward, collecting each OCCURS level
        var occursLevels = new List<(DataSymbol sym, int count)>();
        var current = id.Symbol;
        while (current != null)
        {
            if (current.Occurs != null)
                occursLevels.Insert(0, (current, current.Occurs.MaxOccurs));
            current = current.Parent;
        }

        // Two sizes to distinguish:
        // - stepSize: size of one occurrence of the innermost OCCURS level (for subscript arithmetic)
        // - leafSize: size of the leaf element being addressed (for PIC descriptor and IrElementRef)
        //
        // For direct OCCURS items (ITEM PIC X OCCURS 5), stepSize == leafSize.
        // For children of OCCURS groups (VAL PIC 9 within ROW OCCURS 3),
        // stepSize = ROW's element size, leafSize = VAL's size.
        int leafSize = id.Symbol.ElementSize;
        if (leafSize == 0)
            leafSize = baseLoc.Value.Length;

        int stepSize = occursLevels.Count > 0
            ? occursLevels[^1].sym.ElementSize
            : leafSize;
        if (stepSize == 0)
            stepSize = leafSize;

        // Build element PicDescriptor with the leaf element's storage length
        var arrayPic = baseLoc.Value.Pic;
        var elementPic = new Runtime.PicDescriptor(
            arrayPic.TotalDigits, arrayPic.FractionDigits,
            arrayPic.IsSigned, arrayPic.IsNumeric, arrayPic.IsAlphanumeric,
            arrayPic.HasEditing, leafSize, arrayPic.Usage,
            arrayPic.Category, arrayPic.SignStorage, arrayPic.Editing,
            arrayPic.BlankWhenZero, arrayPic.LeadingScaleDigits,
            arrayPic.TrailingScaleDigits, arrayPic.EditPattern);

        // Compute multipliers using stepSize (OCCURS group element size for subscript arithmetic)
        var multipliers = ComputeMultipliers(occursLevels, stepSize);

        // Data-model migration S4: a flipped fixed-OCCURS table — the element lives in a typed .NET array, not a
        // byte window. Produce a typed element location (array[subscript-1]) instead of a byte offset. Only
        // exclusively-element-accessed tables are flipped (a whole-table operand demotes to byte), and slice 1 is
        // single-dimension, so the first subscript is the index. (docs/RECORD_STRUCT_STORAGE_DESIGN.md §9.)
        if (_ctx.TypedArrayRefs.TryGetValue(id.Symbol, out var arr))
        {
            var indexExpr = _ctx.Expression.LowerExpression(id.Subscripts![0]) ?? new IrLiteral(1m);
            return new IrTypedElementLocation(arr.Name, indexExpr, leafSize, elementPic);
        }

        // Try all-constant fold: if every subscript is a literal, compute offset at compile time
        var subs = id.Subscripts!;
        bool allConstant = true;
        int effectiveOffset = baseLoc.Value.Offset;
        for (int i = 0; i < subs.Count && i < multipliers.Count; i++)
        {
            if (subs[i] is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                int val = (int)d;
                if (val < 1) return null;
                effectiveOffset += (val - 1) * multipliers[i];
            }
            else
            {
                allConstant = false;
                break;
            }
        }

        if (allConstant)
        {
            return new IrStaticLocation(
                new StorageLocation(baseLoc.Value.Area, effectiveOffset, leafSize, elementPic));
        }

        // Variable/expression subscripts → IrElementRef for runtime offset computation.
        // Lower each subscript BoundExpression to an IrExpression.
        var irSubscripts = new List<IrExpression>(subs.Count);
        foreach (var sub in subs)
            irSubscripts.Add(_ctx.Expression.LowerExpression(sub) ?? new IrLiteral(1m));
        return new IrElementRef(baseLoc.Value, irSubscripts, multipliers, leafSize, elementPic);
    }

    /// <summary>
    /// Compute per-dimension multipliers for multi-dimensional OCCURS.
    /// Each multiplier is the ElementSize of the OCCURS group at that level.
    /// </summary>
    internal static List<int> ComputeMultipliers(
        List<(DataSymbol sym, int count)> occursLevels, int elementSize)
    {
        var multipliers = new List<int>(occursLevels.Count);
        for (int i = 0; i < occursLevels.Count; i++)
            multipliers.Add(occursLevels[i].sym.ElementSize);
        return multipliers;
    }

    /// <summary>
    /// Resolve a DataSymbol (non-subscriptable reference) to an IrLocation.
    /// Used for record buffers, file status variables, INITIALIZE items,
    /// PERFORM VARYING index, and condition parents.
    /// </summary>
    public IrLocation? ResolveLocation(DataSymbol sym, bool receiving = false)
    {
        var loc = _ctx.Semantic.GetStorageLocation(sym);
        if (!loc.HasValue) return null;
        return ResolveWholeItem(sym, loc.Value, receiving);
    }

    /// <summary>
    /// Resolve a whole-item (non-subscripted) reference. If the item contains a trailing
    /// OCCURS DEPENDING ON table, return an IrOdoGroupLocation so the effective byte length
    /// is computed at runtime from the DEPENDING ON value; otherwise a plain IrStaticLocation.
    ///
    /// ISO 1989:1985 OCCURS clause GR 7: when the DEPENDING ON object is within the group,
    /// a SENDING operand uses the current value but a RECEIVING operand uses the MAXIMUM
    /// length (so all occurrences are written). When the DEPENDING ON object is outside the
    /// group, the current value is used regardless of direction.
    /// </summary>
    private IrLocation ResolveWholeItem(DataSymbol sym, StorageLocation loc, bool receiving)
    {
        // Data-model migration S3 (docs/RECORD_STRUCT_STORAGE_DESIGN.md): a flipped item is a typed-native
        // .NET field, not a byte window. The flip is gated by EnableTypedFields + the classifier (collected in
        // Binder.CollectTypedFields), so when the flag is off TypedFieldRefs is empty and this never fires.
        if (_ctx.TypedFieldRefs.TryGetValue(sym, out var typed))
            return new IrTypedFieldLocation(typed.Name, typed.Width, loc.Pic, typed.Instance);

        // S4 belt-and-suspenders: a whole-operand reference to a flipped OCCURS table — or to a group containing one
        // — has no byte home. The classifier's whole-table/whole-group demotion (§9.3) should have kept such a table
        // byte-backed (so it never reaches TypedArrayRefs); if one slips through, fail loudly rather than silently
        // read stale bytes.
        if (_ctx.TypedArrayRefs.ContainsKey(sym) || HasFlippedTableDescendant(sym))
            throw new System.NotSupportedException(
                $"Typed OCCURS table at/under '{sym.Name}' was referenced as a whole operand but flipped to a typed " +
                "array; it should have been classifier-demoted to byte (RECORD_STRUCT_STORAGE_DESIGN.md §9.3).");

        // Runtime length is only computed for areas backed by a contiguous byte[] we can
        // re-slice (WORKING-STORAGE, LOCAL-STORAGE, FILE SECTION). LINKAGE keeps the
        // compile-time layout length.
        bool sliceable = loc.Area is StorageAreaKind.WorkingStorage
            or StorageAreaKind.LocalStorage or StorageAreaKind.FileSection;

        if (sliceable && FindDependingOnArray(sym) is { } odo
            && odo.Occurs?.DependingOnSymbol is { } dependOn
            && odo.ElementSize > 0)
        {
            bool dependOnInside = IsDescendant(sym, dependOn);
            if (!(receiving && dependOnInside))
            {
                var dependOnLoc = ResolveLocation(dependOn);
                if (dependOnLoc != null)
                    return new IrOdoGroupLocation(loc, odo.Occurs.MaxOccurs, odo.ElementSize, dependOnLoc);
            }
        }

        return new IrStaticLocation(loc);
    }

    /// <summary>True if <paramref name="node"/> is subordinate to <paramref name="ancestor"/>.</summary>
    private static bool IsDescendant(DataSymbol ancestor, DataSymbol node)
    {
        for (var p = node.Parent; p != null; p = p.Parent)
            if (ReferenceEquals(p, ancestor)) return true;
        return false;
    }

    /// <summary>S4: true if any descendant of <paramref name="sym"/> is a flipped typed OCCURS table — used by the
    /// whole-item loud guard to refuse a whole-group operand over a group that contains a typed array.</summary>
    private bool HasFlippedTableDescendant(DataSymbol sym)
    {
        if (!sym.IsGroup) return false;
        foreach (var child in sym.Children)
            if (_ctx.TypedArrayRefs.ContainsKey(child) || HasFlippedTableDescendant(child))
                return true;
        return false;
    }

    /// <summary>
    /// Find the trailing OCCURS DEPENDING ON table at or beneath <paramref name="sym"/>.
    /// Children are scanned last-first so the trailing (variable-length) table is found.
    /// Returns null if there is no DEPENDING ON table in the subtree.
    /// </summary>
    private static DataSymbol? FindDependingOnArray(DataSymbol sym)
    {
        if (sym.Occurs?.DependingOnSymbol != null)
            return sym;

        for (int i = sym.Children.Count - 1; i >= 0; i--)
        {
            var child = sym.Children[i];
            if (child.LevelNumber == 66 || child.Redefines != null) continue;
            if (FindDependingOnArray(child) is { } found)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Resolve any data-reference BoundExpression to an IrLocation.
    /// Handles BoundIdentifierExpression (with subscripts) and
    /// BoundReferenceModificationExpression (subscripts + substring).
    /// </summary>
    public IrLocation? ResolveExpressionLocation(BoundExpression expr, bool receiving = false)
    {
        return expr switch
        {
            BoundIdentifierExpression id => ResolveLocation(id, receiving),
            BoundReferenceModificationExpression refMod => ResolveRefModLocation(refMod),
            _ => null
        };
    }

    public IrLocation? ResolveRefModLocation(BoundReferenceModificationExpression refMod)
    {
        var baseLoc = ResolveLocation(refMod.Base);
        if (baseLoc == null) return null;

        int baseLen = baseLoc switch
        {
            IrStaticLocation s => s.Location.Length,
            IrElementRef e => e.ElementSize,
            _ => 0
        };

        var irStart = _ctx.Expression.LowerExpression(refMod.Start);
        if (irStart == null) return null;
        var irLength = refMod.Length != null ? _ctx.Expression.LowerExpression(refMod.Length) : null;

        return new IrRefModLocation(baseLoc, irStart, irLength, baseLen);
    }
}
