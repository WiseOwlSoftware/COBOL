// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Passes;

/// <summary>
/// The LAST data-model pass (rearchitecture PHASE 05; DESIGN-data-model §2.5 step 10): computes the canonical
/// <see cref="StorageForm"/> for every elementary item, ONCE, after all facts are known. It replaces — in
/// prove-then-delete stages — the late-mutated <c>StoreAsImage</c> flag + the recursive image-fact properties.
/// <para><b>D0 (this step) is the PROVE half:</b> <see cref="Compute"/> runs AFTER every <c>StoreAsImage</c> write
/// has fired, so the numeric-image promotion (<c>NativeInt → CharImage(Numeric)</c>) is read from the FINAL
/// <c>StoreAsImage</c> — byte-exact by construction. The from-scratch whole-group union walk (reproducing
/// <c>MarkStoreAsImage</c> + the 8 other write sites WITHOUT the flag) is the delete-phase work (Step 7). The value
/// of D0 is <see cref="Verify"/>: the facts DERIVED from <see cref="StorageForm"/> (IsCharacterImage / ImageWidth /
/// ElementType) equal the legacy recursive computations, corpus-wide — proving the model before any deletion.</para>
/// </summary>
internal static class StorageFormPass
{
    /// <summary>Assign <see cref="DataItem.Storage"/> to every ELEMENTARY item reachable from the binder's roots +
    /// report print items (a group carries no Storage — it emits as a record struct and answers its image facts
    /// recursively over children).</summary>
    public static void Compute(DataBinder data)
    {
        var visited = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var root in data.Roots) Walk(root, visited);
        // Report print items are synthetic DataItems OFF Roots (they carry StoreAsImage too — a Roots-only walk
        // would miss them, a parity trap).
        foreach (var report in data.Reports)
            foreach (var group in report.Groups)
                foreach (var line in group.Lines)
                    foreach (var field in line.Fields)
                    Walk(field.PrintItem, visited);
    }

    private static void Walk(DataItem item, HashSet<DataItem> visited)
    {
        if (!visited.Add(item)) return;          // reference dedup — LINKAGE roots are already in Roots; temps share a Pic
        if (item.IsElementary) item.Storage = Classify(item);
        foreach (var c in item.Children) Walk(c, visited);
    }

    /// <summary>The base + image-promoted <see cref="StorageForm"/> of one ELEMENTARY item.</summary>
    private static StorageForm Classify(DataItem item)
    {
        // (1) OCCURS DYNAMIC — the out-of-line table wraps its element's own form (highest priority; §8.5.1.9.1, D9).
        if (item.IsDynamicTable) return new StorageForm.DynamicTable(BaseElementary(item));

        // (2) A REDEFINES view member (non-canonical member of a class).
        if (item.Class is { } cls && !item.IsCanonical)
        {
            if (cls.Tier == RedefinesTier.StringCanonical)
                // A Tier-B numeric-DISPLAY / BINARY / PACKED leaf is image-stored (StoreAsImage) → CharImage(Numeric);
                // a Tier-B group backing or non-numeric-display view → the typed (offset, width) window.
                return item.StoreAsImage
                    ? new StorageForm.CharImage(item.ImageWidth, PicCategory.Numeric)
                    : new StorageForm.TierBWindow(cls, item.ClassOffset, item.ImageWidth);
            if (cls.Tier == RedefinesTier.Alias)
                return Classify(cls.Canonical);   // Tier-A: forward to the canonical's form
            // ByteCanonical (Tier-C) / Rejected: unreachable corpus-wide today — fall through to base classify.
        }

        // (3) Base classification, then the numeric-image promotion for a whole-group / figurative / OO-harmonize leaf.
        var form = BaseElementary(item);
        if (item.StoreAsImage && form is StorageForm.NativeInt)
            return new StorageForm.CharImage(item.ImageWidth, PicCategory.Numeric);
        return form;
    }

    /// <summary>The pre-promotion base form from (Pic.Category, Usage, tier, dynamic). <c>item.ImageWidth</c> is the
    /// character-image width (= <c>ElementaryImageWidth</c>: digits + separate-sign, or PIC length) — the source of
    /// truth this pass reproduces.</summary>
    private static StorageForm BaseElementary(DataItem item)
    {
        if (item.Pic is not { } pic) return new StorageForm.CharImage(0, PicCategory.Alphanumeric);   // Pic-null recovery leaf
        return pic.Category switch
        {
            PicCategory.ObjectReference => new StorageForm.ObjectRef(pic.ObjectClassName),
            PicCategory.Pointer => new StorageForm.PointerRef(),
            PicCategory.Numeric when pic.Usage == Usage.Index => new StorageForm.IndexCell(item.ImageWidth),
            PicCategory.Numeric when pic.IsFloat => new StorageForm.NativeFloat(pic.IsSingle, item.ImageWidth),
            PicCategory.Numeric => new StorageForm.NativeInt(pic.IsWide, pic.Digits, item.ImageWidth),
            // Alphanumeric / NumericEdited / National / Boolean — the base string categories.
            _ => new StorageForm.CharImage(item.ImageWidth, pic.Category),
        };
    }

    // ── The item-level facts DERIVED from Storage (what Verify compares against the legacy recursive props) ──

    /// <summary>Reproduces <c>DataItem.IsCharacterImage</c> off <see cref="StorageForm"/> instead of the string-category
    /// test + <c>StoreAsImage</c>. Leaf = its form's flag; group = every child is character-image; dynamic = no.</summary>
    public static bool IsCharacterImageOf(DataItem item) =>
        !item.IsDynamicTable && (item.IsElementary
            ? item.Storage!.IsCharacterImage
            : item.IsGroup && item.Children.All(IsCharacterImageOf));

    /// <summary>Reproduces <c>DataItem.ImageWidth</c> off <see cref="StorageForm"/> — single-sourced through the
    /// PHASE-05 §2.6 width authority <see cref="RecordLayout.ImageWidth"/> (leaf = its form's width; group = the sum
    /// over NON-redefining children of child image width × the child's own OCCURS count). The Step-2 corpus assert
    /// (<see cref="Verify"/> identity #3) thus also proves <c>RecordLayout.ImageWidth == DataItem.ImageWidth</c>.</summary>
    public static int ImageWidthOf(DataItem item) => RecordLayout.ImageWidth(item);

    /// <summary>Reproduces <c>DataItem.ElementType</c> off <see cref="StorageForm"/> (a group is its record-struct
    /// name; an elementary is its form's CLR type — a promoted numeric leaf is "string", not its Pic.ClrType).</summary>
    public static string ElementTypeOf(DataItem item) =>
        item.IsGroup ? item.StructName : StorageElementType(item.Storage!);

    private static string StorageElementType(StorageForm f) => f switch
    {
        StorageForm.CharImage => "string",
        StorageForm.NativeInt ni => ni.Wide ? "Int128" : "long",
        StorageForm.NativeFloat nf => nf.Single ? "float" : "double",
        StorageForm.IndexCell => "long",
        StorageForm.ObjectRef o => o.ClassName is { } cls ? DataItem.Sanitize(cls).ToUpperInvariant() + "?" : "CobolObject?",
        StorageForm.PointerRef => "ManagedPointer",
        StorageForm.TierBWindow => "string",       // a Tier-B window is a string slice (a numeric Tier-B leaf is CharImage)
        StorageForm.DynamicTable dt => StorageElementType(dt.Element),
        StorageForm.TierCWindow => "string",       // unreachable today
        _ => "object",
    };

    /// <summary>The corpus-wide PROVE assert (DESIGN Phase D0 / exit criterion 3): for every item under a binder, the
    /// StorageForm identities (#1–#4) between the new <see cref="StorageForm"/> and the legacy computation, PLUS the
    /// PHASE-05 §2.6 <see cref="RecordLayout"/> physical-width identity (#5). Returns the list of divergences (empty =
    /// proven equal). #1 is tautological in D0 (Storage is promoted FROM StoreAsImage) — it is the guard for the later
    /// delete phase; the value here is #2/#3/#4 (the derived recursive facts still hold once Storage, not the scattered
    /// flag, is the source) and #5 (the RecordLayout width authority reproduces the legacy tier-aware physical extent —
    /// the Step-9 drift guard).</summary>
    public static List<string> Verify(DataBinder data)
    {
        var d = new List<string>();
        var visited = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        void Check(DataItem item)
        {
            if (!visited.Add(item)) return;
            if (item.IsElementary)
            {
                if (item.Storage is null) { d.Add($"NO-STORAGE: {Desc(item)}"); return; }
                bool promoted = item.Storage is StorageForm.CharImage { Category: PicCategory.Numeric };
                if (promoted != item.StoreAsImage)
                    d.Add($"#1 StoreAsImage: {Desc(item)} form-numeric-image={promoted} flag={item.StoreAsImage}");
                if (ElementTypeOf(item) != item.ElementType)
                    d.Add($"#4 ElementType: {Desc(item)} derived={ElementTypeOf(item)} legacy={item.ElementType}");
            }
            if (IsCharacterImageOf(item) != item.IsCharacterImage)
                d.Add($"#2 IsCharacterImage: {Desc(item)} derived={IsCharacterImageOf(item)} legacy={item.IsCharacterImage}");
            if (ImageWidthOf(item) != item.ImageWidth)
                d.Add($"#3 ImageWidth: {Desc(item)} derived={ImageWidthOf(item)} legacy={item.ImageWidth}");
            // #5 (PHASE-05 Step 4): the RecordLayout §2.6 width authority reproduces the legacy tier-aware physical
            // extent (OdoModel.PhysicalWidth) for every group — the DESIGN §5.4 drift guard, held green until Step 9
            // deletes the duplicate width copies.
            if (item.IsGroup && RecordLayout.PhysicalWidth(item) != OdoModel.PhysicalWidth(item))
                d.Add($"#5 PhysicalWidth: {Desc(item)} recordlayout={RecordLayout.PhysicalWidth(item)} legacy={OdoModel.PhysicalWidth(item)}");
            foreach (var c in item.Children) Check(c);
        }
        foreach (var root in data.Roots) Check(root);
        foreach (var report in data.Reports)
            foreach (var group in report.Groups)
                foreach (var line in group.Lines)
                    foreach (var field in line.Fields)
                    Check(field.PrintItem);
        return d;
    }

    private static string Desc(DataItem item) =>
        $"{item.CobolName ?? "FILLER"}#{item.Uid} (pic={item.Pic?.Category.ToString() ?? "group"}/{item.Pic?.Usage.ToString() ?? "-"})";
}
