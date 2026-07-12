// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The DATA DIVISION emission facade (P7 Step 9l — was <c>Emit/FieldEmitter.cs</c>, split per
/// DESIGN-codegen-backend §2.5 into <see cref="RecordStructEmitter"/> · <see cref="GroupImageCodec"/> ·
/// <see cref="GroupValueSlicer"/> · <see cref="ValueInitializer"/> over the shared memoized
/// <see cref="PhysicalModel"/>): constructs and WIRES the five (they are mutually recursive by design —
/// a field's Init is a VALUE initializer, a Tier-B backing's seed is an image init, and both walk the
/// physical model), and forwards the public surface the per-unit emitters consume.</summary>
internal sealed class DataEmitter
{
    private readonly RecordStructEmitter _structs;
    private readonly GroupImageCodec _codec;
    private readonly ValueInitializer _values;

    public DataEmitter(EmitContext ctx)
    {
        var phys = new PhysicalModel(ctx);
        _values = new ValueInitializer(ctx);
        var slicer = new GroupValueSlicer(phys);
        _codec = new GroupImageCodec(ctx, phys, _values);
        phys.Values = _values;
        phys.Codec = _codec;
        _values.Slicer = slicer;
        _structs = new RecordStructEmitter(ctx, phys, _codec, _values);
    }

    /// <summary>Emit every WORKING-STORAGE / FILE-SECTION type, profile, index field, and root field.</summary>
    public void Emit() => _structs.Emit();

    /// <summary>See <see cref="RecordStructEmitter.RootDecl"/>.</summary>
    public (string Type, string Init) RootDecl(DataItem item) => _structs.RootDecl(item);

    /// <summary>See <see cref="RecordStructEmitter.MethodRedefinesBackingDecl"/>.</summary>
    public (string Name, string Init)? MethodRedefinesBackingDecl(DataItem root) => _structs.MethodRedefinesBackingDecl(root);

    /// <summary>See <see cref="GroupImageCodec.ImageInitOf"/>.</summary>
    public string ImageInitOf(DataItem item) => _codec.ImageInitOf(item);
}
