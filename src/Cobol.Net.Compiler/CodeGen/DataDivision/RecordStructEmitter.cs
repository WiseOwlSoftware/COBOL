// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The record-struct / profile / root-field emitter of the DATA DIVISION (P7 Step 9l): a nested
/// <c>record struct</c> per group, a static <c>NumProfile</c> per numeric leaf, the INDEXED BY cells, and one
/// field per top-level item — a COBOL record IS a .NET record; no byte substrate, no flattening. Wired by
/// <see cref="DataEmitter"/>.</summary>
internal sealed class RecordStructEmitter(EmitContext ctx, PhysicalModel phys, GroupImageCodec codec, ValueInitializer vals)
{
    /// <summary>Emit every WORKING-STORAGE / FILE-SECTION type, profile, index field, and root field.</summary>
    public void Emit()
    {
        var w = ctx.Writer;
        foreach (var root in ctx.Data.Roots) EmitStructTypeDecls(root, w);
        foreach (var root in ctx.Data.Roots) EmitProfiles(root, w);
        // An OCCURS DYNAMIC CAPACITY register is an implicit VIEW over the table's Capacity — it has NO storage
        // field, but DISPLAY / MOVE-to-alphanumeric of the register formats through its own NumProfile (ISO
        // §13.18.38 GR15 / §8.5.1.9.1; data-model D9), so emit those profiles (not fields) too.
        foreach (var tbl in ctx.Data.CapacityRegisters.Values)
            if (tbl.OccursSpec?.CapacityRegister is { } reg) EmitProfiles(reg, w);
        // Programs are instantiable classes (interprogram design D3): root + index fields are INSTANCE fields —
        // a fresh instance IS the §14.6.2.3.2 initial state; the registry's cached singleton IS last-used state.
        // The suppression filter skips members another mechanism provides: a carrier-resident LINKAGE formal
        // (its field is `__lnkpN.Value` — caller storage, ISO §13.7.1) and an inherited GLOBAL table's index
        // field (a ref-bridge to the container, §13.18.27 GR2).
        foreach (var (name, field) in ctx.Data.IndexFields)
            if (!ctx.Data.CallSuppressedRootFields.Contains(field))
                w.Line($"private long {field} = 1;   // INDEX-NAME {name}");
        // A method WORKING-STORAGE table's index cell is a class STATIC (persistent across activations, §11.7;
        // M2-OO-1h step 4). LOCAL/LINKAGE table cells are per-activation method locals, emitted in OoEmitMethod.
        foreach (var cell in ctx.Data.OoStaticIndexCells)
            w.Line($"private static long {cell} = 1;   // method-WS INDEX-NAME cell (M2-OO-1h)");
        foreach (var f in phys.RootPhysicals())
            if (!ctx.Data.CallSuppressedRootFields.Contains(f.Name))
                // A method-WS root is a STATIC field (OO deep-dive D3 — one copy per class, shared across
                // instances, persistent across activations, ISO §11.7; pre-2023 editions only, §13.5.3 SR 1).
                w.Line($"private {(ctx.Data.OoStaticRootFields.Contains(f.Name) ? "static " : "")}{f.Type} {f.Name} = {f.Init};   // {f.Comment}");
    }

    /// <summary>The C# (type, initializer) pair for declaring one root as a METHOD LOCAL (the OO slice-2
    /// LINKAGE/LOCAL-STORAGE mapping) — the same composed initializer a field declaration gets, so a group
    /// local's OCCURS arrays and VALUE seeds are identical to field semantics (§14.5.3: LOCAL-STORAGE
    /// re-initializes on every activation — a C# local declaration does exactly that).</summary>
    public (string Type, string Init) RootDecl(DataItem item) => (item.FieldType, vals.FieldInit(item));

    /// <summary>The (name, init) of a method-scoped Tier-B REDEFINES class's ONE string backing when
    /// <paramref name="root"/> is that class's canonical (M2-OO-1h step 3). The class-level field loop suppresses
    /// this backing (it is method-scoped), so <c>OoEmitMethod</c> emits it as a method LOCAL of type
    /// <c>string</c> — the members are windows over it (via <see cref="RedefViewPlace"/>). Null otherwise.</summary>
    public (string Name, string Init)? MethodRedefinesBackingDecl(DataItem root) =>
        root.Class is { Tier: RedefinesTier.StringCanonical } cls && ReferenceEquals(cls.Canonical, root)
            ? (cls.BackingCsName, RuntimeApi.StrStore(codec.ImageInitOf(root), $"{cls.Width}"))
            : null;

    private void EmitStructTypeDecls(DataItem item, CodeWriter w)
    {
        if (!item.IsGroup) return;
        foreach (var child in item.Children) EmitStructTypeDecls(child, w);   // nested types first (any order is fine)
        using (w.Block($"private record struct {item.StructName}"))
        {
            foreach (var f in phys.PhysicalChildrenOf(item))
                w.Line($"public {f.Type} {f.Name};   // {f.Comment}");

            // An image-capable group gets the whole-group image facility (COBOLNET_DESIGN §14.4): AsImage
            // concatenates the leaves' character images — a string-stored leaf its characters, a NATIVE
            // fixed-point leaf (DISPLAY/BINARY/PACKED) its zoned digit image (trailing-overpunch sign, the
            // §13.18.60 USAGE GR4 implementor representation) — and FromImage distributes a character image back
            // into them. Used by whole-group MOVE / DISPLAY / compare / WRITE / RELEASE and the READ / RETURN
            // record-area distribution; the SD/FD record codec IS this pair (§8.2). Only a group with a float /
            // COMP-5 / INDEX leaf stays the loud Tier-C island (DataItem.IsImageCapable).
            if (item.IsImageCapable) codec.EmitImageMethods(item, w);
        }
    }

    private static void EmitProfiles(DataItem item, CodeWriter w)
    {
        if (item.IsElementary && item.Pic is { Category: PicCategory.Numeric, IsFloat: false })
            // INTERNAL (not private): an INVOKE BY CONTENT conversion composes the FORMAL's value/image at
            // the CALL SITE, qualifying the profile by its owner class ({OWNER}._P_n) — same assembly, one
            // generated file (the OO slice-2 review's cross-class-profile rule).
            w.Line($"internal static readonly NumProfile {item.ProfileName} = {item.Pic.ProfileInitializer};");
        foreach (var child in item.Children) EmitProfiles(child, w);
    }

}
