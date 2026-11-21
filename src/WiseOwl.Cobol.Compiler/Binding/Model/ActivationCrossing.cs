// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The form in which a data item CROSSES an activation boundary (an INVOKE, a CALL, a host entry) — a pure
/// function of its description, read by the binder and the backend alike (kb/Work PB2403: it left the OO class table,
/// which the backend may not name).</summary>
public static class ActivationCrossing
{
    /// <summary>True when an item CROSSES the INVOKE boundary as a character string: groups (image crossing),
    /// image-stored numerics, alphanumeric / numeric-edited items. Native numerics and object references cross
    /// typed. The §2023:14.8.2 strict-conformance bind rules guarantee both sides agree on the crossing form's
    /// WIDTH/description — which is what keeps the marshaling free of cross-class numeric profiles.
    /// (THE one definition: the binder's storage-form harmonize and the emitter's signature and marshaling
    /// renders both consult it; model data since kb/Work PB2403, so the backend reads it without the class table.)</summary>
    public static bool StringCarried(DataItem item) =>
        item.IsGroup || item.StoreAsImage
        || item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;   // string-stored (D-N1/D-B1) — char crossing

    /// <summary>⛔ THE LEAF-VECTOR CROSSING FORM (kb/Work PB1116): a strongly-typed group with NO boundary character
    /// image — §2023:13.18.60.3 confines a subordinate object reference to a strong type declaration, so this is every
    /// strong group holding an object-reference (or class-pointer) leaf. It crosses an activation boundary as the
    /// <c>object?[]</c> of its record struct's physical fields, made by the sender's <c>AsLeaves</c> and stored by the
    /// RECEIVER's instance <c>WithLeaves</c> (both emitted by <c>RecordStructEmitter</c>; an instance store, because a
    /// dynamic-capacity table is recreated in the receiver's own description): the two sides are of the SAME type (§2023:14.8.2.2 / §2023:14.8.3.2 "both shall be of the same
    /// type", decided before any carrier is chosen), so their fields correspond one to one, while their C# record
    /// structs are distinct (every TYPE clone has its own, and two equivalent type declarations in two source elements
    /// are two types) — a field-wise copy through a neutral vector is the one carrier that works between any two of
    /// them. An object reference is copied as a reference.
    /// <para>⛔ A VARIABLE-LENGTH STRONG GROUP IS IN WHEN IT HAS NO OTHER FORM (kb/Work PB3139). One whose current extent
    /// composes crosses on its §2023:8.5.1.12 carrier (<see cref="DataItem.BoundaryImageCapable"/> excludes it here); one that
    /// holds a reference leaf has no such carrier — a pointer or object reference is no member of
    /// <c>DataItem.CurrentExtentComposes</c>'s law — and nothing in §2023:8.5.1.12.1 bars a strong group from being
    /// variable-length, so §2023:14.8.2.2 rule 2's "both shall be of the same type" makes this vector its crossing too: a
    /// dynamic-length member rides it as its current content and a dynamic-capacity table as its current occurrences, each
    /// in its element's form, recreated in the receiver's description (§2023:14.6.9.2). Excluding the shape refused legal
    /// source (COBOL1688 on the CALL, COBOL1513 on the prototype) and staged a group MOVE of it loud.</para>
    /// It is tested BEFORE <see cref="StringCarried"/>, which is true of every group.</summary>
    public static bool LeafCarried(DataItem item) =>
        item.IsGroup && !item.BoundaryImageCapable && StrongTypeModel.IsStrongGroup(item);
}
