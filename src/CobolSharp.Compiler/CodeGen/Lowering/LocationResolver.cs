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
    public IrLocation? ResolveLocation(BoundIdentifierExpression id)
    {
        var baseLoc = _ctx.Semantic.GetStorageLocation(id.Symbol);
        if (!baseLoc.HasValue) return null;

        // Non-subscripted: static location
        if (!id.IsSubscripted)
            return new IrStaticLocation(baseLoc.Value);

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
    public IrLocation? ResolveLocation(DataSymbol sym)
    {
        var loc = _ctx.Semantic.GetStorageLocation(sym);
        if (!loc.HasValue) return null;
        return new IrStaticLocation(loc.Value);
    }

    /// <summary>
    /// Resolve any data-reference BoundExpression to an IrLocation.
    /// Handles BoundIdentifierExpression (with subscripts) and
    /// BoundReferenceModificationExpression (subscripts + substring).
    /// </summary>
    public IrLocation? ResolveExpressionLocation(BoundExpression expr)
    {
        return expr switch
        {
            BoundIdentifierExpression id => ResolveLocation(id),
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
