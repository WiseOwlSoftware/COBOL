// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>Why <see cref="PlaceForItem(DataItem, IReadOnlyList{string}, out PlaceGap)"/> built no place:
/// the <see cref="DeferredShape"/> it has not built, or — when <paramref name="Deferred"/> is null — the
/// item whose DECLARATION was refused. That item is carried rather than assumed because it need not be the
/// item asked about: a level-66 RENAMES alias builds no place when data-name-2 (its no-THROUGH target) or one
/// of its spanned leaves was refused, and the report must name that entry (kb/Work PB1380).</summary>
internal readonly record struct PlaceGap(DeferredShape? Deferred, DataItem Refused);

/// <summary>
/// ⛔ <b>THE ONE ITEM-TO-PLACE BUILDER</b> (DESIGN-architecture-review §8.3 (4), DESIGN-backend-abstraction §1.3 L6,
/// kb/Work PB2375), in the model layer and free of the binder: given a <see cref="DataItem"/>, the typed
/// <see cref="Position"/> of each OCCURS level on its path and the facts of the finished binding
/// (<see cref="IPlaceFacts"/>), it answers the <see cref="Place"/> every consumer reads and writes through — a plain
/// <see cref="MemberPlace"/>, a Tier-A alias forwarded to its canonical, a Tier-B/C view window, a level-66 RENAMES
/// composition (<see cref="PlaceForRenames"/>, ISO §2023:13.18.45.4 GR1/GR2), an OCCURS DEPENDING group's wrap
/// (<see cref="WrapIfOdoGroup"/>, §2023:13.18.38.4 GR8) and the dynamic-capacity element place (§2023:13.18.38.4 GR14). Both the
/// syntactic resolution (<see cref="ReferenceResolver"/>, which reads the subscripts the program wrote) and the by-item
/// resolution (<see cref="PlaceTable"/>, which the binder fills for every item once binding is done) go through it, so
/// every consumer sees identical view resolution (<c>RenamesPlaceBuilderDriftTests</c>); the code generator is handed
/// the finished <see cref="Place"/>s on the bound unit and asks no builder for the ones without a written subscript.
/// </summary>
internal sealed class PlaceBuilder(IPlaceFacts facts)
{
    /// <summary>
    /// Build the typed <see cref="Place"/> for an already-resolved <paramref name="item"/> with its subscript index
    /// expressions, honoring the REDEFINES machinery (DESIGN §3.4 / §4.2):
    /// <list type="bullet">
    ///   <item>a Tier-B (string-canonical) view → a <see cref="RedefViewPlace"/> (offset,width) window over the
    ///         class's ONE string backing (the canonical too, so exactly one stored member);</item>
    ///   <item>a Tier-A (alias) view → the canonical's ONE stored field, reinterpreted through the view's own
    ///         Pic/scale/profile (the place carries the VIEW's <see cref="DataItem"/>);</item>
    ///   <item>a level-66 RENAMES entry → data-name-2's own place (no THROUGH) or a <see cref="RenamesPlace"/>
    ///         composed over the spanned leaves (THROUGH) — <see cref="PlaceForRenames"/>;</item>
    ///   <item>any other item → a plain <see cref="MemberPlace"/>.</item>
    /// </list>
    /// Returns <see langword="null"/> for a form not handled in this slice — a subscripted Tier-B view, a whole-OCCURS
    /// reference, a not-yet-wired Tier-C / Rejected view, or a subscript-count mismatch — so the caller fails loud.
    /// This is the ONE item→<see cref="Place"/> builder: both the syntactic <see cref="Resolve"/> path and the by-item
    /// <see cref="ResolveItem"/> path go through it, so EVERY consumer (verb operands, level-88 / SET conditional
    /// variables, FD record areas) sees identical view resolution.
    /// </summary>
    internal Place? PlaceForItem(DataItem item, IReadOnlyList<Position> indexExprs) => PlaceForItem(item, indexExprs, out _);

    /// <param name="gap">On a null return, the <see cref="DeferredShape"/> this builder has not built — or none,
    /// with the entry whose declaration was refused: a REJECTED (Tier D) REDEFINES view (kb/Work PB1030), or a
    /// level-66 RENAMES entry or the refused entry behind it (kb/Work PB1380).</param>
    internal Place? PlaceForItem(DataItem item, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        gap = new(null, item);
        // A level-66 RENAMES entry (ISO §2023:13.18.45) is a data item of its own that owns no storage — its place is
        // composed from the places of the items it renames (kb/Work PB1380: it is built HERE, in the one builder,
        // so every caller — a reference-modified alias above all — receives it the same way).
        if (item.Renames is { } ren) return PlaceForRenames(item, ren, indexExprs, out gap);
        // A WHOLE (unsubscripted) OCCURS DYNAMIC table reference has no element access — it would otherwise fold to a
        // MemberPlace wrapping the bare CobolDynTable<T> object, which is uncompilable in any value context. Fail
        // LOUD here (data-model D9); FUNCTION LENGTH and other whole-table operations route to a dedicated
        // DynWholeTablePlace in a later increment. A SUBSCRIPTED dynamic element (indexExprs non-empty) is inc 3's
        // access path and is NOT caught by this guard.
        if (item.IsDynamicTable && indexExprs.Count == 0) { gap = new(DeferredShape.DynamicWholeTable, item); return null; }
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class.
            // ⛔ A CLASS WHOSE CANONICAL LIES WITHIN AN OCCURS IS REACHED THROUGH THE SUBSCRIPTED PARENT PATH (kb/Work
            // PB1279). ISO §2023:13.18.44.3 SR5 sentence 2 — "However, data-name-2 may be subordinate to an item whose
            // data description entry contains an OCCURS clause" — and §2023:13.18.44.4 GR1 associates the redefining
            // item's storage with the first bit of the redefined item IN EACH OCCURRENCE, so every occurrence of the
            // enclosing table holds its own backing, in that element's struct. §2023:8.4.2.3.3 SR3 gives the reference one
            // subscript per OCCURS clause: the LEADING ones select the enclosing element (they are the canonical
            // parent's own dimensions), the trailing ones are the in-class levels below.
            int outerCount = sc.Canonical.Parent?.SubscriptArity ?? 0;
            // A SUBSCRIPTED view: each OCCURS level on the item's path WITHIN the class displaces the window by
            // (occurrence − 1) × that level's per-occurrence width — the redefined table lays its occurrences
            // end-to-end in the ONE backing (ISO §2023:13.18.44). ClassOffset is the occurrence-1 position; subscripts
            // map to the in-class OCCURS levels outer→inner, exactly as in AccessPath.
            var levels = SubscriptLevelsWithin(item, sc);
            if (indexExprs.Count != outerCount + levels.Count) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }   // an item-path caller's count
            if (BuildBackingPath(sc, [.. indexExprs.Take(outerCount)]) is not { } classBacking) { gap = new(DeferredShape.NestedClassBacking, item); return null; }
            indexExprs = [.. indexExprs.Skip(outerCount)];
            if (WindowScopeOf(classBacking, BuildCellPath(sc), levels, indexExprs, CellOrdinalBase(sc)) is not { } scope) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
            // A dynamic-capacity table referenced by its subscript IS its element, at offset zero of the element cell.
            Position origin = new PositionConstant(item.IsDynamicTable ? 0 : item.ClassOffset);
            // A BASED class's window is displaced by the data-address pointer's runtime offset (ISO §2023:13.18.5
            // — the view addresses wherever the pointer currently points; Phase-4b increment 2). The backing
            // property renders FIRST in both Read and Write, so the Deref null/bounds traps (GR3/GR4) fire
            // before the null-lenient OffsetOf. Only the record SCOPE is displaced: a dynamic-capacity table's element
            // cell (an area formal may hold one — kb/Work PB2094) has offsets of its own; its component ordinals are
            // displaced the same way (CellOrdinalBase, inside the scope walk).
            Position? based = null;
            if (sc.BasedPointerField is { } addr && !scope.Nested)
            {
                based = new PositionPointerOffset(addr);
                origin = new PositionBinary(based, PositionOperator.Add, origin);
            }
            var offset = new PositionOffset(origin, scope.Terms);
            // (whole-group image analysis moved OUT of resolve to the post-bind UsageCollectionPass, PHASE-05 Step 5)
            // A class-tier GROUP holding an occurs-depending table is an ODO operand exactly like a struct group
            // (kb/Work PB80: a BASED record — string-canonical — sent its MAXIMUM image; §2023:13.18.38.4 GR8 does not
            // care how the group is stored). ONE wrap rule for both storage shapes.
            Position? dynOrdinal = scope.Cell is null ? null : scope.OrdinalAt(item.IsDynamicTable ? 0 : item.ClassDynOrdinal);
            return WrapIfOdoGroup(RedefViewPlace.For(scope.Backing, item, offset, based, scope.BitTerms, scope.Cell, dynOrdinal), item);
        }
        // A Tier-A view forwards to the canonical (a numeric view reinterprets the shared unscaled value via its own
        // scale, for free). A not-yet-wired (Tier-C) / Rejected view is loud.
        if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
            return null;   // Tier D (Rejected) — refused at the declaration; the gap names the item itself
        DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
            ? ac.Canonical : item;
        // A subscripted element whose access path crosses an OCCURS DYNAMIC level (data-model D9): the sending and
        // receiving accessors differ (RefSending vs RefReceiving — the latter grows-and-seeds), so build BOTH paths
        // and return a direction-carrying DynTablePlace. (A dynamic element is never an ODO subject, and a group
        // containing a dynamic table is not image-capable, so the ODO-wrap / whole-group paths below do not apply.)
        for (DataItem? n = accessItem; n is not null; n = n.Parent)
            if (n.IsDynamicTable)
            {
                if (BuildAccessPath(accessItem, indexExprs, OdoReferenceCheckFor) is not { } dynPath) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
                return new DynTablePlace(dynPath, item);
            }
        // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
        if (BuildAccessPath(accessItem, indexExprs, OdoReferenceCheckFor) is not { } path) { gap = new(DeferredShape.UnbuiltAccessPath, item); return null; }
        // (Resolving a group no longer mutates WholeGroupReferenced — the "which groups are whole-image operands"
        // analysis is the post-bind UsageCollectionPass, which walks the BOUND tree and collects ONLY true
        // whole-group operands, not every RESOLVED group. PHASE-05 Step 5, §2023:14.9.25.4 MOVE GR4.)
        return WrapIfOdoGroup(new MemberPlace(path, item), item);
    }

    /// <summary>The place of a level-66 RENAMES entry (ISO §2023:13.18.45) — the <see cref="PlaceForItem"/> arm for an
    /// item that owns no storage of its own. The no-THROUGH form forwards to data-name-2's place (GR1); the
    /// THROUGH form composes a <see cref="RenamesPlace"/> over the spanned storage parts (GR2): string-valued leaves
    /// as they are and every numeric leaf through its storage image (<see cref="SpanLeafPlace"/>).</summary>
    private Place? PlaceForRenames(DataItem alias, RenamesInfo ren, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        gap = new(null, alias);
        // A level-66 entry is never a table element, so a written subscript was refused by §2023:8.4.2.3.3 SR2 before
        // the syntactic path got here (ScreenSubscriptArity); an item-path caller's count is the same mismatch
        // every other item reports.
        if (indexExprs.Count > 0) { gap = new(DeferredShape.UnbuiltAccessPath, alias); return null; }
        // A RENAMES entry whose operands did not resolve was refused at its declaration (COBOL1655,
        // §2023:13.18.45.3), and the data binder leaves it with no From and an empty Span.
        if (ren.Thru is null ? ren.From is null : ren.Span.Count == 0) return null;
        // The no-THRU form is an ALIAS: §2023:13.18.45.4 GR1 — "all of the data attributes of data-name-2 become the
        // data attributes of data-name-1 and the storage area occupied by data-name-2 becomes the storage area
        // occupied by data-name-1". Attributes AND storage forward to the renamed item's place (numeric stays
        // numeric: NC252A's ADD 3500 TO RENAME-12 over a PIC 9(4); a group forwards as the group), and only the
        // THRU form composes an alphanumeric span (GR2).
        // ⛔ BUT THE IDENTITY DOES NOT FORWARD (kb/Work PB602). GR1 shares the attributes and the storage, not the
        // NAME: data-name-1 is a data item of its OWN, and a bare forward made a reference to the 66
        // indistinguishable from a reference to the renamed item — so `START RLF KEY IS = RK-ALIAS` passed
        // §2023:14.9.41.3 SR5's "shall be the data item specified in the RELATIVE KEY clause" that its THROUGH sibling
        // (a RenamesPlace, which keeps its own Item) was correctly refused by. `DenotesAs` records the alias on the
        // renamed item's own place, so nothing about the ACCESS changes and only the identity question answers
        // differently.
        if (ren.Thru is null)
            return PlaceForItem(ren.From!, [], out gap) is { } fwdPlace ? fwdPlace with { DenotesAs = alias } : null;
        var leafPlaces = new List<Place>(ren.Span.Count);
        var widths = new List<int>(ren.Span.Count);
        foreach (var part in ren.Span)
        {
            var leaf = part.Leaf;
            if (part.Occurrence is { } occIdx)
            {
                // ONE occurrence (or the one-and-only cell) of the leaf, possibly a partial slice of it (kb/Work
                // PB96): the cell's storage image, then a BYTE slice of it when the part does not cover the whole cell
                // — bytes, because the cell's image is its storage bytes (SpanLeafPlace), so a slice that splits a
                // national character is as addressable as any other (kb/Work PB2466).
                if (SpanLeafPlace(leaf, part.SubscriptsFor(leaf.Occurs is null ? null : new PositionConstant(occIdx)), out gap) is not { } cell)
                    return null;
                if (part.IsPartial) cell = new RefModPlace(cell, new PositionConstant(part.StartByte), new PositionConstant(part.LengthBytes));
                leafPlaces.Add(cell);
                widths.Add(part.LengthBytes);   // STORAGE bytes — the unit the alias's image is in (kb/Work PB1665)
                continue;
            }
            // An OCCURS leaf inside the span contributes EVERY occurrence in order (§2023:13.18.45 — the alias covers
            // the whole fixed-size area; NC252A's RENAME-7 over TABLE-ITEM-2 OCCURS 5). A leaf under an OCCURS GROUP is
            // one part per occurrence of the group (RenamesSpanPart.Outer, kb/Work PB986), so its cell is addressed by
            // those subscripts first.
            int occ = leaf.Occurs ?? 1;
            for (int k = 1; k <= occ; k++)
            {
                if (SpanLeafPlace(leaf, part.SubscriptsFor(leaf.Occurs is null ? null : new PositionConstant(k)), out gap) is not { } lp) return null;
                widths.Add(leaf.ByteWidth);   // a whole part: every occurrence, at the leaf's storage width (kb/Work PB96, PB1665)
                leafPlaces.Add(lp);
            }
        }
        return new RenamesPlace(leafPlaces, alias, widths);
    }

    /// <summary>One spanned leaf cell of a RENAMES THROUGH alias, as the STORAGE image the composed alias
    /// concatenates: a string-valued leaf as it is, a typed NUMERIC leaf through its <see cref="NumericImagePlace"/>
    /// — the bytes it occupies, zoned digits for usage DISPLAY, radix-2 / BCD bytes for BINARY / PACKED, the IEEE
    /// window for a float (V59; the alias is an alphanumeric group item over the storage, §2023:13.18.45.4 GR2 — NC252A's
    /// PIC 999 leaves under RENAMES-TEST-1). Null with the gap when the cell has no place.
    /// <para>⛔ EVERY NUMERIC USAGE, NOT ONLY THE CHARACTER FORMS (kb/Work PB1054). GR2 defines data-name-1 as "an
    /// alphanumeric group item that includes all elementary items" of the range, so a COMP member contributes its
    /// storage bytes exactly as it does to the record's own group image. This used to admit only
    /// <see cref="PicInfo.IsCharacterFormNumeric"/> leaves and DEFER the rest (a COBOL1756 warning and a run-time
    /// abort for <c>66 RN RENAMES RA THRU RC</c> over a <c>PIC 9(4) COMP</c> member), although the image place it
    /// declined renders every numeric usage through the ONE storage codec (<c>NumFormatImage</c> /
    /// <c>NumStoreImage</c>, and the float lane). Pointer, object and message-tag members never reach here: §2023:13.18.45.3
    /// SR8 refuses them in the range at the declaration (<c>ResolveRedefinesPass.RenamesRangeFault</c>).</para>
    /// <para>⛔ A USAGE NATIONAL CELL IS ITS BYTES TOO (kb/Work PB1665, PB2466): its value carrier holds one character
    /// per national position, and the position occupies two storage bytes (D-N1), so the cell is composed through
    /// <see cref="NationalBytesPlace"/> — after the numeric image, for a usage-national numeric leaf whose image is
    /// national characters. The image is then in the unit every <see cref="RenamesSpanPart"/> is kept in, and a part
    /// that splits a national character is a byte slice of it like any other.</para></summary>
    private Place? SpanLeafPlace(DataItem leaf, IReadOnlyList<Position> indexExprs, out PlaceGap gap)
    {
        if (PlaceForItem(leaf, indexExprs, out gap) is not { } place) return null;
        bool stringValued = facts.IsImageBackedEarly(leaf) || place is RedefViewPlace
            || leaf.Pic?.Category is not PicCategory.Numeric;
        var image = stringValued ? place : new NumericImagePlace(place);
        return leaf.Pic is { Usage: Usage.National } ? new NationalBytesPlace(image) : image;
    }

    /// <summary>A group whose subtree contains an occurs-depending table is an ODO operand (ISO §2023:13.18.38 GR8): wrap
    /// it so the sending slice / receiving direction-split applies — whatever the group's storage shape (a record
    /// struct member, or a Tier-B / BASED class-tier window; kb/Work PB80). data-name-1 is resolved post-build,
    /// declared anywhere outside the table (SR20), and read at the operation site via CobolTable.Occ
    /// (storage-form-agnostic). Any other place passes through unchanged.
    /// <para>The recursion through <see cref="ResolveItem"/> ends because <c>OdoResolvePass</c> records only an
    /// ADMITTED data-name-1 (<see cref="OccursSpec.Depending"/>: an integer, so an elementary item, kb/Work PB3510);
    /// a group data-name-1 — the record holding the table, or a record whose own table depends on this one — is
    /// refused there with its SR17/SR20 diagnostics and never reaches this wrap.</para></summary>
    private Place WrapIfOdoGroup(Place place, DataItem item) =>
        item.IsGroup && OdoModel.TableUnder(item) is { OccursSpec.Depending: { } dep } table
            && ResolveItem(dep) is { } depPlace
            ? OdoModel.WrapGroup(place, depPlace, item, table)
            : place;

    /// <summary>A <see cref="Place"/> for an already-resolved item with no subscripts (e.g. a level-88's conditional
    /// variable, a SET condition's variable, or an FD record area) — view-aware via <see cref="PlaceForItem"/>, so a
    /// REDEFINES view resolves to its window / canonical exactly as a verb operand does. <see langword="null"/> if the
    /// item is within an OCCURS table (a subscripted reference is then required) or is an unhandled view form.</summary>
    public Place? ResolveItem(DataItem item) => PlaceForItem(item, []);

    /// <summary>The <see cref="Place"/> of ONE OCCURRENCE of an already-resolved <paramref name="item"/> — one
    /// typed occurrence position per OCCURS level of its path, outermost first —
    /// through the same view-aware builder every verb operand uses. The caller states the occurrence; nothing is
    /// read from source. The Format-2 table SORT of a table in a shared-storage (REDEFINES) class sorts through
    /// these (kb/Work PB1175): each element and each key is a window the class's own offset law positions.
    /// <see langword="null"/> when the count does not match the item's <see cref="DataItem.SubscriptArity"/> or the
    /// item has no built place.</summary>
    internal Place? ResolveItemAt(DataItem item, IReadOnlyList<Position> indexExprs) => PlaceForItem(item, indexExprs);

    /// <summary>⛔ THE ONE RESOLUTION OF AN FD/SD's RECORD AREA (kb/Work PB355). ISO §2023:13.18.33.4 GR3 — "Multiple
    /// level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area" — so
    /// the area is ONE place, and it is the LARGEST description's view (<see cref="FileModel.AreaRecord"/>): a
    /// shorter <c>Records[0]</c> window truncates the splice (RL106A's 56/102-char pair, ST111A's 50/75/100 SD).
    /// Null only for a file whose record area could not be resolved at all — §2023:14.9.30.4 GR6's implied entry is
    /// materialized at bind time (kb/Work PB345), so no LEGAL file reaches a consumer with none.
    /// <para>It lives here because five consumers each wrote the same two-step —
    /// <c>file.AreaRecord is { } ar ? ResolveItem(ar) : null</c> — and START's own key comparand was built from
    /// data-name-1 instead of the area precisely because the area was not a thing a verb could simply ask
    /// for.</para></summary>
    public Place? RecordArea(FileModel file) => file.AreaRecord is { } area ? ResolveItem(area) : null;

    /// <summary>The STRUCTURAL access path to a Tier-B/Tier-C class's single stored backing field (the
    /// <see cref="RedefViewPlace"/> twin of the old string <c>BackingPath</c>). The backing is emitted in the
    /// canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable. A canonical within a FIXED table is reached
    /// through the enclosing element — <paramref name="outerIndexExprs"/> are the subscripts of the canonical
    /// parent's own dimensions, outermost first (ISO §2023:13.18.44.3 SR5, kb/Work PB1279). A canonical within an OCCURS
    /// DYNAMIC table is reached the same way (kb/Work PB1933): the path's <see cref="DynTableSegment"/> renders its
    /// accessor from the direction <c>PlaceRenderer</c> renders the backing in — <c>RefSending</c> on a read,
    /// <c>RefReceiving</c> (grow-and-seed, §2023:8.5.1.9.3) on a store into the window. A METHOD's top-level class backing is a
    /// local of the method (<see cref="RedefinesClass.BackingLocal"/>; kb/Work PB4416), the root of its path by identity.</summary>
    private AccessPath? BuildBackingPath(RedefinesClass cls, IReadOnlyList<Position> outerIndexExprs)
    {
        if (cls.Canonical.Parent is not { } parent)
            return new AccessPath([StorageRoot(cls.BackingLocal, cls.BackingClrName, OmittedFormalGuard.Of(cls.Canonical))]);
        // The enclosing element carries the same §2023:13.18.38.4 GR7 OCCURS DEPENDING check every other element path does.
        return BuildAccessPath(parent, outerIndexExprs, OdoReferenceCheckFor) is { } parentPath
            ? parentPath.Add(new MemberSegment(cls.BackingClrName)) : null;
    }

    /// <summary>The STRUCTURAL access path to the <c>StorageCell</c> behind a CELL-BACKED class's backing —
    /// the second half of the one storage area, holding the MANAGED SLOTS a pointer-class member rides
    /// (kb/Work PB231; <see cref="SlotWindow"/>). Null for a plain REDEFINES class, which has no cell.
    /// <para>Always a bare root field: the three cell surfaces (EXTERNAL, ADDRESS OF, BASED) all force a
    /// LEVEL-1 record — §2023:13.18.22.3 SR1 puts EXTERNAL only on "level 1 data description entries", the
    /// ADDRESS-OF forcer walks to the root before forcing, and a BASED entry is a 01/77 — so there is no
    /// containing struct to reach through, and a canonical with a parent would mean the cell property was
    /// never emitted.</para></summary>
    internal static AccessPath? BuildCellPath(RedefinesClass? cls) =>
        cls is { IsCellBacked: true, Canonical.Parent: null }
            // The cell is a reference-type StorageCell held in a READONLY field, so its guard passes it through
            // rather than taking it by ref (kb/Work PB971).
            ? new AccessPath([new RootFieldSegment(cls.BackingCellClrName,
                OmittedFormalGuard.Of(cls.Canonical) is { } g ? g with { PassesCarrier = true } : null)])
            : null;

    /// <summary>⛔ THE COMPONENT-ORDINAL BASE OF A CELL-BACKED CLASS'S RECORD SCOPE (kb/Work PB2094), as the leading term of
    /// its ordinal displacement: a class laid over storage through its data-address pointer — a BASED item, an AREA formal
    /// (§2023:14.2.3 GR8) — numbers its components from the pointer's <c>CellPointer.DynBase</c>, the ordinal twin of the
    /// <c>CobolPtr.OffsetOf</c> its character window is displaced by (an area formal over a subordinate variable-length
    /// group begins at that group's first component). Empty for a class that owns its cell.</summary>
    internal static Position? CellOrdinalBase(RedefinesClass cls) =>
        cls.BasedPointerField is { } addr ? new PositionPointerDynBase(addr) : null;

    /// <summary>The origin of a component ordinal: the item's static ordinal <paramref name="staticOrdinal"/> in its
    /// scope, displaced by the scope's run-time ordinal base (<see cref="CellOrdinalBase"/>) when it has one.</summary>
    internal static Position OrdinalOrigin(int staticOrdinal, Position? ordinalBase) =>
        ordinalBase is null ? new PositionConstant(staticOrdinal)
                            : new PositionBinary(new PositionConstant(staticOrdinal), PositionOperator.Add, ordinalBase);

    /// <summary>The subscript levels of a reference to <paramref name="item"/> WITHIN its Tier-B class
    /// <paramref name="cls"/>, outermost first: every fixed OCCURS level and every dynamic-capacity table level on
    /// its path (§2023:8.4.2.3.3 SR3 — one subscript per OCCURS clause, a dynamic table's included).</summary>
    private static List<DataItem> SubscriptLevelsWithin(DataItem item, RedefinesClass cls)
    {
        var levels = new List<DataItem>();
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, cls); n = n.Parent)
            if (n.Occurs is not null || n.IsDynamicTable) levels.Add(n);
        levels.Reverse();
        return levels;
    }

    /// <summary>Where a Tier-B class reference's window lives, and its displacements there.</summary>
    /// <param name="Cell">The scope's <c>StorageCell</c> path (null for a REDEFINES class, which has none).</param>
    /// <param name="Backing">The scope's character backing — the class backing, or an element cell's <c>Ref</c>.</param>
    /// <param name="Terms">The byte displacement terms of the fixed levels crossed in the scope.</param>
    /// <param name="BitTerms">The same displacement in bits, for a USAGE BIT member.</param>
    /// <param name="OrdinalTerms">The component-ordinal displacement of the same levels (cell only).</param>
    /// <param name="Nested">True when a dynamic-capacity table level opened an element cell's scope.</param>
    private readonly record struct WindowScope(AccessPath? Cell, AccessPath Backing, IReadOnlyList<OffsetTerm> Terms,
                                               IReadOnlyList<OffsetTerm> BitTerms, Position? OrdinalBase,
                                               IReadOnlyList<OffsetTerm> OrdinalTerms, bool Nested)
    {
        /// <summary>The component ordinal of an item whose static ordinal in the scope is <paramref name="staticOrdinal"/>,
        /// displaced by the scope's run-time ordinal base and its levels.</summary>
        public PositionOffset OrdinalAt(int staticOrdinal) => new(OrdinalOrigin(staticOrdinal, OrdinalBase), OrdinalTerms);
    }

    /// <summary>⛔ THE ONE DISPLACEMENT TERM OF A FIXED OCCURS LEVEL (kb/Work PB231, PB3287): <c>(index − 1) ×</c> the
    /// level's per-occurrence STORAGE extent, <see cref="DataItem.ByteWidth"/> — never its character-position count,
    /// because the class backing is byte-addressed and a NATIONAL element strides two bytes per position (§2023:13.18.60.4
    /// GR8 / D-N1). A Tier-B view window (<see cref="WindowScopeOf"/>) and the address of an occurrence
    /// (<see cref="ResolveForAddressOf"/>) lay a table's occurrences end-to-end in the one cell image and take their
    /// displacement from here, so a stride fix cannot be made in one of them only.</summary>
    internal static OffsetTerm FixedLevelTerm(DataItem level, Position index) => new(index, level.ByteWidth);

    /// <summary>⛔ THE ONE WALK OF A TIER-B REFERENCE'S SUBSCRIPT LEVELS (kb/Work PB1042), outermost first. A fixed
    /// level displaces the window by <c>(index − 1) ×</c> its per-occurrence STORAGE extent — never its
    /// character-position count (kb/Work PB231: a NATIONAL element occupies two bytes per position, §2023:13.18.60.4 GR8 /
    /// D-N1) — its BIT twin by the level's bit stride (kb/Work PB203: `PIC 1(4) USAGE BIT OCCURS 6` strides 4 bits;
    /// ALIGNED strides whole bytes, §2023:13.18.1.4 GR2), and, in a cell, the component ordinal by the level's components
    /// per occurrence (<see cref="CellComponents.PerOccurrence"/>). A dynamic-capacity table level — ISO §2023:8.5.1.9.1
    /// 3): it "may be defined in any place, other than the file section, in which a fixed-capacity table may be
    /// defined" — opens the SCOPE of its element: the occurrence's element cell (<see cref="CellTableSegment"/> then
    /// <see cref="DynTableSegment"/>, whose accessor is the reference's direction, §2023:8.5.1.9.2 / §2023:8.5.1.9.3), whose
    /// <c>Ref</c> is the backing and whose own offsets and ordinals start again at zero. Null when such a level lies in
    /// a class with no cell.</summary>
    private static WindowScope? WindowScopeOf(AccessPath backing, AccessPath? cell, IReadOnlyList<DataItem> levels,
                                              IReadOnlyList<Position> indexExprs, Position? ordinalBase)
    {
        List<OffsetTerm> terms = [], bitTerms = [], ordinalTerms = [];
        bool nested = false;
        for (int k = 0; k < levels.Count; k++)
        {
            var level = levels[k];
            if (level.IsDynamicTable)
            {
                if (cell is null) return null;
                cell = cell.Add(CellTableSegment.Of(level, new PositionOffset(OrdinalOrigin(level.ClassDynOrdinal, ordinalBase), [.. ordinalTerms])))
                           .Add(new DynTableSegment(indexExprs[k]));
                backing = cell.Add(CellBackingSegment.Instance);
                (terms, bitTerms, ordinalBase, ordinalTerms, nested) = ([], [], null, [], true);
                continue;
            }
            terms.Add(FixedLevelTerm(level, indexExprs[k]));
            bitTerms.Add(new OffsetTerm(indexExprs[k], BitLayout.StrideBits(level)));
            if (cell is not null) ordinalTerms.Add(new OffsetTerm(indexExprs[k], CellComponents.PerOccurrence(level)));
        }
        return new WindowScope(cell, backing, terms, bitTerms, ordinalBase, ordinalTerms, nested);
    }

    /// <summary>⛔ THE ONE ROOT OF AN ITEM'S ACCESS PATH (kb/Work PB971, PB4320): the item's storage, carrying the
    /// *-ARG-OMITTED guard when the item is a formal parameter (<see cref="OmittedFormalGuard.Of"/>), so every
    /// path builder — element, whole table, Tier-B backing — checks a reference to an omitted formal the same
    /// way, and a new builder that roots through here cannot forget it. A CARRIER-RESIDENT formal has no storage of its
    /// own: its root is its carrier's (<see cref="DataItem.ResidentCarrier"/>: a program's carrier member, a method's
    /// carrier local, kb/Work PB4380) and the next step is the storage the carrier designates,
    /// <see cref="CarrierValueSegment"/> (ISO §2023:14.2.3 GR8). A METHOD's root is a local of the method, named by its
    /// identity (<see cref="DataItem.ActivationLocal"/>, ISO §2023:8.6.4; kb/Work PB4416); every other root is a member of the
    /// program class, named by its CLR name.</summary>
    private static void AddRoot(List<AccessSegment> segs, DataItem root)
    {
        var guard = OmittedFormalGuard.Of(root);
        if (root.ResidentCarrier is { } carrier)
        {
            segs.Add(carrier.Root(guard));
            segs.Add(CarrierValueSegment.Instance);
        }
        else
            segs.Add(StorageRoot(root.ActivationLocal, root.ClrName, guard));
    }

    /// <summary>⛔ THE ONE CHOICE OF A STORAGE ROOT'S KIND (kb/Work PB4416): a local of the generated method when the storage
    /// has a local's identity (<paramref name="local"/>: a method's data root, <see cref="DataItem.ActivationLocal"/>, or its
    /// Tier-B class backing, <see cref="RedefinesClass.BackingLocal"/>), else the program class's member named
    /// <paramref name="memberClrName"/> (DESIGN-backend-abstraction §1.2: a local is referenced by its identity, never its
    /// name).</summary>
    private static RootSegment StorageRoot(MethodLocal? local, string memberClrName, OmittedFormalGuard? guard) =>
        local is not null ? new LocalRootSegment(local, guard) : new RootFieldSegment(memberClrName, guard);

    /// <summary>The STRUCTURAL access path for an item — the <see cref="MemberPlace"/>/<see cref="DynTablePlace"/>
    /// twin of the string <see cref="AccessPath"/>: each chain node is a field segment, each OCCURS level a fixed or
    /// dynamic table segment carrying its typed index position. Null on a subscript-count mismatch.</summary>
    internal static AccessPath? BuildAccessPath(DataItem item, IReadOnlyList<Position> indexExprs,
        Func<DataItem, OdoReferenceCheck?>? odo = null)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        // §2023:8.4.2.3.3 SR3's count, from THE one place it is written down — the string twin below reads the same
        // property, so the two access-path builders cannot disagree about arity (kb/Work PB877).
        if (item.SubscriptArity != indexExprs.Count) return null;   // wrong number of subscripts
        var segs = new List<AccessSegment>();
        int si = 0;
        bool first = true;
        foreach (var seg in chain)
        {
            if (first) AddRoot(segs, seg);
            else segs.Add(new MemberSegment(seg.ClrName));
            first = false;
            if (seg.Occurs is not null) segs.Add(new FixedTableSegment(indexExprs[si++], odo?.Invoke(seg)));   // fixed OCCURS → CobolTable.At
            else if (seg.IsDynamicTable) segs.Add(new DynTableSegment(indexExprs[si++]));         // dynamic OCCURS → RefSending/RefReceiving
        }
        return new AccessPath(segs);
    }

    /// <summary>The STRUCTURAL whole-table path (no subscript wraps) — the ONE whole-table path: the
    /// <see cref="CapacityRegisterPlace"/>'s table, the SEARCH bound and EC-FLOW-SEARCH bracket, and the base of a
    /// whole-dynamic-table INITIALIZE element path (its string twin was deleted with kb/Work PB1042, which found it
    /// blind to a cell-backed table). Null when an ancestor is itself a table (an ambiguous whole-table
    /// reference).</summary>
    internal static AccessPath? BuildTablePath(DataItem table) => BuildTablePath(table, []);

    /// <summary>⛔ THE ONE MODEL OF A TABLE LEVEL'S CURRENT OCCURRENCE COUNT (the <see cref="AllCount"/> the backend
    /// renders through <c>PlaceRenderer.OccurrenceCount</c>): a fixed table's OCCURS integer (§2023:13.18.38.4 GR4); an
    /// occurs-depending table's data-name-1, which "represents the current number of occurrences of the subject of
    /// the entry" (GR7 — clamped to [integer-1, integer-2] with EC-BOUND-ODO outside); a dynamic-capacity table's
    /// current capacity (§2023:8.5.1.9.1), whose register view carries the OUTER index expressions of a nested table.
    /// Every statement that ranges over the CURRENT occurrences of a table it sends asks HERE — a table(ALL)
    /// argument (§2023:15.3) and the Format-2 table SORT (§2023:14.9.40.4 GR20, kb/Work PB1174, whose sort used to reorder
    /// the whole PHYSICAL array). INITIALIZE keeps its own <c>InitializeBinder.TableCount</c> because its operand is
    /// a RECEIVING one: §2023:13.18.38.4 GR8b gives a receiving group holding its own data-name-1 the MAXIMUM, a question
    /// this sending-count model does not ask. <see langword="null"/> when the count
    /// cannot be addressed (an unresolvable data-name-1, or a dynamic table with no reachable register) — the caller
    /// reports it in its own words.
    /// <para>The three-way switch itself is <see cref="Procedure.OccurrenceCounts.Current"/> — the ONE reading INITIALIZE and the
    /// §2023:14.6.9.2 element moves also take; this entry only supplies the whole-table path a dynamic level's capacity
    /// register needs (the two used to be two copies of the switch, each documented as the one).</para></summary>
    internal AllCount? CurrentOccurrenceCount(DataItem table, IReadOnlyList<Position> outerIndexExprs) =>
        OccurrenceCounts.Current(table, table.IsDynamicTable ? BuildTablePath(table, outerIndexExprs) : null, this);

    /// <summary>The §2023:13.18.38.4 GR7 check a subscripted reference through <paramref name="level"/> carries
    /// (<see cref="OdoReferenceCheck"/>, kb/Work PB1268): data-name-1's place and integer-1/integer-2 for an OCCURS
    /// DEPENDING level, when the compilation group can enable EC-BOUND-ODO at all; <see langword="null"/> otherwise
    /// (a fixed level, or no enabling >>TURN — the zero-scaffolding invariant). The bounds are the SAME the extent
    /// model uses (<see cref="CurrentOccurrenceCount"/>), so the superordinate and the element checks agree.</summary>
    private OdoReferenceCheck? OdoReferenceCheckFor(DataItem level) =>
        facts.OdoReferenceChecking && CurrentOccurrenceCount(level, []) is AllCount.Odo o
            ? new OdoReferenceCheck(o.Depending, o.MinOccurs, o.MaxOccurs) : null;

    /// <summary>The STRUCTURAL whole-table path to a table that may itself lie under OTHER tables — one index
    /// position per enclosing table level, outermost first: the
    /// <see cref="CapacityRegisterPlace"/> of a NESTED dynamic-capacity table for a table(ALL) enumeration (ISO §2023:15.3
    /// — each outer occurrence has its own capacity; kb/Work PB62). Null when fewer indices are supplied than there
    /// are enclosing tables — the zero-index form is exactly the whole-table ambiguity the one-argument overload
    /// reports.</summary>
    internal static AccessPath? BuildTablePath(DataItem table, IReadOnlyList<Position> outerIndexExprs)
    {
        // A dynamic-capacity table of a CELL-BACKED class is a component of its scope's cell (kb/Work PB1042): the
        // enclosing levels are walked by THE ONE scope walk, and the table is that cell's component.
        if (table is { IsDynamicTable: true, Class: { IsCellBacked: true } cc } && BuildCellPath(cc) is { } rootCell)
        {
            var outer = SubscriptLevelsWithin(table, cc);
            outer.RemoveAt(outer.Count - 1);   // the table itself
            if (outerIndexExprs.Count < outer.Count) return null;   // an enclosing table with no index — ambiguous
            return WindowScopeOf(rootCell, rootCell, outer, outerIndexExprs, CellOrdinalBase(cc)) is { Cell: { } scopeCell } s
                ? scopeCell.Add(CellTableSegment.Of(table, s.OrdinalAt(table.ClassDynOrdinal)))
                : null;
        }
        var chain = new List<DataItem>();
        for (DataItem? n = table; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        var segs = new List<AccessSegment>();
        bool first = true;
        int oi = 0;
        foreach (var n in chain)
        {
            if (first) AddRoot(segs, n);
            else segs.Add(new MemberSegment(n.ClrName));
            first = false;
            if (ReferenceEquals(n, table)) break;
            if (!n.IsTable) continue;
            if (oi >= outerIndexExprs.Count) return null;   // an enclosing table with no index — ambiguous
            segs.Add(n.Occurs is not null
                ? new FixedTableSegment(outerIndexExprs[oi++])
                : new DynTableSegment(outerIndexExprs[oi++]));
        }
        return new AccessPath(segs);
    }
}
