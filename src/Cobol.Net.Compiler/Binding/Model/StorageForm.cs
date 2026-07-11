// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// The canonical storage representation of an elementary item or table element (rearchitecture PHASE 05;
/// DESIGN-data-model §2.1). A closed, pure value classification — no C# strings, no emit logic; the emitter
/// interprets it. Computed ONCE by <c>StorageFormPass</c> and (from Step 10) held init-only on <c>DataItem</c>,
/// replacing the late-mutated <c>StoreAsImage</c> flag and the recursive image-fact properties. A new representation
/// is a NEW case (an exhaustive switch makes a missing arm a compile error, not a runtime loud-stmt).
/// <para>The <c>Width</c> carried on the native cases is the item's CHARACTER-IMAGE width
/// (<c>ElementaryImageWidth</c> = digit count + a separate-sign position, ISO §13.18.52) — precomputed at
/// construction so <see cref="ImageWidth"/> reproduces <c>DataItem.ImageWidth</c> from the form alone (the D0
/// amendment, DEVLOG 749).</para>
/// </summary>
public abstract record StorageForm
{
    /// <summary>Does this leaf contribute character positions to an enclosing group's image codec
    /// (<c>AsImage()</c>/<c>FromImage()</c>, §14.4)? (An item-level query short-circuits on OCCURS DYNAMIC and
    /// recurses <c>Children.All</c> for a group — see <c>StorageFacts</c>.)</summary>
    public abstract bool IsCharacterImage { get; }

    /// <summary>This item's per-occurrence character-image width.</summary>
    public abstract int ImageWidth { get; }

    /// <summary>Native scaled integer — <c>long</c> (≤18 digits) or <c>Int128</c> (>18). Not character-imageable as
    /// itself, but IS image-CAPABLE (its zoned digit image is derived on demand). <paramref name="Width"/> = digits
    /// + separate-sign.</summary>
    public sealed record NativeInt(bool Wide, int Digits, int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }

    /// <summary>Native IEEE float (COMP-1/FLOAT-SHORT) or double (COMP-2). Never in a static record image.</summary>
    public sealed record NativeFloat(bool Single, int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }

    /// <summary>A C# <c>string</c> of exactly <paramref name="Width"/> characters — the ONE string-stored case:
    /// alphanumeric / numeric-edited / national / boolean, OR a numeric-DISPLAY (or Tier-B BINARY/PACKED) leaf
    /// promoted to its zoned image because it is used under a whole-group operand (§14.9 GR4). <paramref
    /// name="Category"/> is retained so the numeric pipeline decodes/encodes zoned images and so the promoted-numeric
    /// sub-case (<c>Category is Numeric</c>) equals the legacy <c>StoreAsImage</c>.</summary>
    public sealed record CharImage(int Width, PicCategory Category) : StorageForm
    {
        public override bool IsCharacterImage => true;
        public override int ImageWidth => Width;
    }

    /// <summary>A Tier-B REDEFINES view: a typed (offset, width) window over the class's ONE shared string backing.
    /// Assigned ONLY to a Tier-B member that is NOT image-promoted (a group backing / a non-numeric-display view); a
    /// Tier-B numeric-DISPLAY/BINARY/PACKED leaf is <see cref="CharImage"/>(Numeric) instead.</summary>
    public sealed record TierBWindow(RedefinesClass Class, int Offset, int Width) : StorageForm
    {
        public override bool IsCharacterImage => true;
        public override int ImageWidth => Width;
    }

    /// <summary>A Tier-C REDEFINES view over the class's ONE confined <c>byte[]</c> (§4.2 tier C). QUARANTINED —
    /// unreachable corpus-wide today (the classifier rejects float/COMP-5/BINARY-*/INDEX/National Tier-C classes), so
    /// its parity obligation is that <c>StorageFormPass</c> assigns it to ZERO leaves.</summary>
    public sealed record TierCWindow(RedefinesClass Class, int Offset, int Length, Usage Usage) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Length;
    }

    /// <summary>An out-of-line OCCURS DYNAMIC table (<c>CobolDynTable&lt;T&gt;</c>, D9). <paramref name="Element"/> is
    /// the per-occurrence element's form. Never character-image (an item-level query short-circuits on IsDynamicTable).</summary>
    public sealed record DynamicTable(StorageForm Element) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Element.ImageWidth;
    }

    /// <summary>A .NET object reference (a typed class, or universal <c>CobolObject?</c> when ClassName is null).
    /// Zero character positions.</summary>
    public sealed record ObjectRef(string? ClassName) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A data pointer (<c>ManagedPointer</c>). Zero character positions.</summary>
    public sealed record PointerRef : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A USAGE INDEX cell (a <c>long</c> occurrence number). Zero character positions.</summary>
    public sealed record IndexCell(int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }
}
