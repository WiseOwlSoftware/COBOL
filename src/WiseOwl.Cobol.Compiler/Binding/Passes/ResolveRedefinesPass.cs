using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Passes;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The ResolveRedefines pass: row 13 of the per-unit resolve pipeline (SignResolved to SignResolved).
/// </summary>
/// <remarks>
/// <para>
/// The REDEFINES clause's ENTRY-level syntax rules (ISO §13.18.44.3; kb/Work PB1280) — the ones that ask about the
/// subject and data-name-2 as WRITTEN, before the storage-class machinery (<c>ClassifyRedefinesClasses</c>) lays the
/// overlay out: SR1 (position in the entry — <c>ScreenLeadingClausePosition</c>, DataBinder.ClausePlacement.cs, beside
/// its §13.16.3 SR4 TYPEDEF twin), SR2 (identical level-numbers), SR3 (not a level-1 file-section entry),
/// SR8 (the size screen, in BITS, with its level-1-without-EXTERNAL exception), SR10 (no entry that defines new
/// storage between data-name-2 and the subject), SR13 (data-name-2 is no CONSTANT RECORD) and SR15 (alignment). The clause-placement table had named <c>ResolveRedefines</c> as the home of the
/// "level and position rules" and nothing was there: data-name-2 was found by name alone, so a 77 and an 01 were
/// interchangeable and the SR8 comment ASSUMED SR3.
/// </para>
/// </remarks>
internal sealed class ResolveRedefinesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "ResolveRedefines";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Resolve each item's REDEFINES target name to its <see cref="DataItem"/>, and each level-66 RENAMES
    /// FROM/THRU operand to its item. A REDEFINES target is an unqualified prior entry in the same scope (SR1/SR6); a
    /// RENAMES range names items within the owning record (SR3). Target resolution does not chase chains — the
    /// classification pass walks <see cref="DataItem.RedefinesTarget"/> transitively to the anchor (SR11).
    /// <para>⛔ A REDEFINES clause written in a TYPEDEF template is resolved and screened TOO (kb/Work PB2071): the
    /// §13.18.44.3 syntax rules hold of the source as written (§4.3), and a type declaration nothing instantiates is
    /// still source. Walking <see cref="DataBinder.Roots"/> alone, an unused template's `REDEFINES NOSUCH`, its oversized
    /// redefinition (SR8) or its redefinition of a table (SR5) compiled clean, and a used one was reported only through
    /// the copies its TYPE subjects hold. The walk is the <see cref="DataBinder.CompositionForest"/> (the template members are on it;
    /// kb/Work PB2518) and only WRITTEN entries are screened, so a type is diagnosed once, at its declaration; a template
    /// gets no storage class (<see cref="ClassifyRedefinesClassesPass.Run"/> asks it only the clause rules).</para></summary>
    public void Run(DataBinder binder)
    {
        // ⛔ THE COMPOSED FOREST, BUT ONLY WRITTEN ENTRIES ARE ASKED (kb/Work PB2518). A TYPEDEF template is off Roots, so
        // the roots-only walk never reached a redefiner INSIDE a type declaration: `01 T TYPEDEF. 05 A PIC X.
        // 05 B REDEFINES A PIC X(2).` compiled clean unless some entry said TYPE T — and then the screens ran on the
        // CLONE. Every clone is resolved (its target feeds the layout), but the rules are about what the programmer
        // WROTE, so only an entry that is no clone (ClonedFrom null) is screened: a type is diagnosed once, at its
        // declaration, used or not. A template's own level-01 entry stays out (it has no siblings to redefine).
        foreach (var item in binder.CompositionForest())
            if (item.RedefinesTargetName is { } tname && !(item.Parent is null && item.IsTypedef))
            {
                bool written = item.ClonedFrom is null;
                using var _ = binder.Edition.At(item);
                // A subordinate (02+) redefiner scopes to its own siblings (correct in every scope). A top-level
                // 01/77 method redefiner must scope to the OWNING METHOD's own roots (§13.18.44.3 SR — the target
                // is a prior sibling in the same data description; a method may NOT redefine object/program data,
                // §11.7.4), NEVER the cross-scope global `Roots` pool (M2-OO-1h step 3).
                IReadOnlyList<DataItem> scope;
                if (item.Parent is { } par)
                    scope = par.Children;
                else if (binder.OoRootOwner.TryGetValue(item.Root, out var mm))
                    // A top-level method redefiner scopes to its OWN section only (§13.18.44.3 SR — the target is a
                    // preceding item in the SAME data description; cross-section LOCAL↔LINKAGE aliasing is illegal,
                    // and their storage classes differ — review B). item.Root
                    // == item here (Parent is null).
                    scope = mm.Binding!.LocalRoots.Contains(item) ? mm.Binding!.LocalRoots
                          : mm.Binding!.LinkageRoots;
                else
                    scope = binder.Roots;
                // §13.18.44.3 SR4/SR7 + NOTE 1: data-name-2 is a PRECEDING entry at the same level, and when the name is
                // not unique "no ambiguity of reference exists because of the required placement" — the NEAREST preceding
                // same-named sibling. The former whole-scope FirstOrDefault admitted a LATER sibling (illegal source, kb/Work
                // PB93) and picked the FIRST of duplicates.
                // ⛔ THE ORIGINAL DEFINITION, NOT THE NEAREST SAME-NAMED ENTRY (kb/Work PB1280; §13.18.44.3 SR7, SR10): a
                // redefinition defines no storage of its own, so "the entry that originally defined the area" of
                // `05 A. 05 A REDEFINES A. 05 C REDEFINES A.` is the FIRST A, and that is what NOTE 1's "required placement"
                // makes unambiguous — the nearest same-named entry that is not itself a redefinition. Only when every
                // candidate is a redefinition is the nearest one named (SR7's own violation, reported below). A top-level
                // entry's candidates are the roots of ITS OWN section: SR10's "entries defining the area of data-name-2"
                // do not cross a section boundary.
                var preceding = scope.TakeWhile(s => !ReferenceEquals(s, item))
                    .Where(s => CobolNames.Same(s.CobolName, tname)
                        && (item.Parent is not null || s.Section == item.Section)).ToList();
                item.SetRedefinition(DataItem.RedefinedEntry(preceding), RedefinitionKind.Clause);
                // A method 01 REDEFINES whose target isn't in the method's own roots is a scope error (never a
                // silent cross-scope bind to an object/program item) — §13.18.44.3 SR.
                if (written && item.RedefinesTarget is null && item.Parent is null && binder.OoRootOwner.ContainsKey(item.Root))
                    // 1577, renumbered from a bare "COBOL1518" that collided with the locale-module
                    // non-support meaning (review V11 — the code comes from the catalog descriptor, never a literal).
                    binder.Edition.Error(DiagnosticCatalog.MethodRedefinesScope,
                        $"REDEFINES target '{tname}' of method data item "
                        + $"'{item.CobolName ?? "FILLER"}' is not a preceding item in the same method scope "
                        + "(ISO §13.18.44.3 — a method item may not redefine object or program data)");
                else if (written && item.RedefinesTarget is null)
                    // kb/Work PB93: the PROGRAM-scope miss used to be silent — the item kept RedefinesTargetName (so
                    // BitLayout / ImageWidth / ByteWidth skipped it as an overlay) against a null RedefinesTarget (so
                    // the emitter gave it its own field): a storage shape no edition defines. ONE diagnostic; the
                    // item then binds as an ORDINARY entry (the name cleared below) so no consumer sees the half-state.
                    binder.Edition.Error(DiagnosticCatalog.RedefinesTargetUnresolved,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{tname}': data-name-2 does not name a preceding "
                        + "entry in the same scope (ISO §13.18.44.3 SR4/SR7/SR10; §8.4.2.1)");
                if (item.RedefinesTarget is null)
                    item.RedefinesTargetName = null;   // no half-state: an unresolved redefiner is an ordinary entry
                // §13.18.44.3 SR7 (kb/Work PB93 sweep): data-name-2 shall be the entry that ORIGINALLY defined the area
                // — a redefiner naming a redefiner is a chain ISO forbids and the field's vendors accept: error strict,
                // warning under --permissive with the chain semantics (ClassifyRedefinesClasses chases the anchor).
                if (written && item.RedefinesTarget is { RedefinesTargetName: not null } viaRedefiner)
                    binder.Edition.Removed(DiagnosticCatalog.RedefinesOfRedefinition.Code,
                        $"'{item.CobolName ?? "FILLER"}' REDEFINES '{viaRedefiner.CobolName ?? "FILLER"}', which is itself "
                        + "a redefinition — data-name-2 shall be the entry that originally defined the storage area "
                        + "(ISO §13.18.44.3 SR7)");
                // §13.18.44.3 SR16: data-name-2 (the redefined item) shall not be described with the ANY
                // LENGTH clause — a runtime-length item has no fixed storage area a redefiner could overlay.
                if (written && item.RedefinesTarget is { IsAnyLength: true })
                    binder.Edition.Error("COBOL1542", $"'{item.CobolName ?? "FILLER"}' REDEFINES "
                        + $"'{tname}': the redefined item is described with the ANY LENGTH clause "
                        + "(ISO §13.18.44.3 SR16 — data-name-2 shall not be ANY LENGTH)");
                // §13.18.44.3 SR1-SR3, SR8, SR10, SR13, SR15 — the entry-level rules over the resolved pair
                // (Passes/ResolveRedefinesPass.cs; kb/Work PB1280). SR8, which used to be written out here, is one of them,
                // and SR10 (no entry defining new storage between the pair, kb/Work PB2518) is asked over the same scope.
                if (written && item.RedefinesTarget is { } entryTarget) ScreenRedefinesEntry(binder, item, entryTarget, scope);
            }

        // Each alias is resolved in the scope of its OWNER: the record whose level-66 entries it is, or — for an alias a
        // TYPEDEF brought (CloneRenamesOnto, §13.18.58.4 GR1) — the item the type was applied to, which may be a group
        // inside a record. Storage offsets are always measured from the record's own root (Offset below).
        foreach (var root in binder.RenamesOwners())
            foreach (var ren in root.Renames66)
            {
                var info = ren.Renames!;
                // SR4 confines both operands to THIS record, so the candidate set is the record's; it is COUNTED
                // (kb/Work PB978) — two same-named items there are §8.4.2.2.3 SR1's ambiguity, not the first found.
                // An operand the capture REFUSED (§13.18.45.3 SR7 / §8.4.2.2.2) was reported there — one fault, one verdict.
                if (binder._refusedClauseOperands.Contains(info.FromName)
                    || (info.ThruName is { } rt && binder._refusedClauseOperands.Contains(rt))) continue;
                using var __q = binder.Edition.At(ren);
                string face = $"'{ren.CobolName ?? "FILLER"}' RENAMES";
                info.From = binder.UniqueOrReportAmbiguous(binder.SubtreeCandidates(root, info.FromName, info.FromQualifiers), face,
                    DataBinder.WrittenQualified(info.FromName, info.FromQualifiers), out bool fromAmbiguous);
                bool thruAmbiguous = false;
                info.Thru = info.ThruName is { } t
                    ? binder.UniqueOrReportAmbiguous(binder.SubtreeCandidates(root, t, info.ThruQualifiers), face,
                        DataBinder.WrittenQualified(t, info.ThruQualifiers), out thruAmbiguous)
                    : null;
                if (fromAmbiguous || thruAmbiguous) continue;
                if (info.From is null || (info.ThruName is not null && info.Thru is null))
                {
                    // kb/Work PB93: an operand naming nothing in the record was skipped silently — the alias then had
                    // no picture and no span, and every reference to it failed downstream with an unrelated message.
                    using var __r = binder.Edition.At(ren);
                    string missing = info.From is null ? info.FromName : info.ThruName!;
                    binder.Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName}{(info.ThruName is { } tn ? " THRU " + tn : "")}: "
                        + $"'{missing}' does not name an item of the record (ISO §13.18.45.3 SR4; §8.4.2.1)");
                    continue;
                }
                // §13.18.45.3 SR5 / SR3 / SR6 — WHAT THE OPERANDS MAY BE (kb/Work PB1283, DataBinder.RenamesEntry.cs): the
                // candidate set includes the record itself, so an operand is asked its level, its OCCURS and its record.
                bool operandsOk = ScreenRenamesOperand(binder, root, ren, info.From, "data-name-2");
                if (info.Thru is { } thruOperand) operandsOk &= ScreenRenamesOperand(binder, root, ren, thruOperand, "data-name-3");
                if (!operandsOk) continue;
                // §13.18.45.3 SR8 — WHAT THE RANGE MAY HOLD (kb/Work PB1284), asked before either form binds: a
                // refused range never reaches the no-THRU forwarding or the tiler, which would otherwise model the
                // alias as a fixed window over storage that has no fixed extent.
                if (RenamesRangeFault(root, info.From, info.Thru) is { } rangeFault)
                {
                    using var __s = binder.Edition.At(ren);
                    binder.Edition.Error(DiagnosticCatalog.RenamesRangeContent,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName}{(info.ThruName is { } tn8 ? " THRU " + tn8 : "")}: "
                        + $"{rangeFault}; none of the items within the range, including data-name-2 and data-name-3, "
                        + "shall be of class object, message-tag, or pointer, a variable-length data item, or an "
                        + "occurs-depending table (ISO §13.18.45.3 SR8)");
                    continue;
                }
                // The no-THRU alias inherits the renamed item's description (§13.18.45 GR1) — the resolver
                // forwards to the FROM item's place; no span, no synthetic alphanumeric picture.
                if (info.Thru is null) { ren.Pic = info.From.Pic; continue; }

                // The alias is the record's STORAGE WINDOW from data-name-2's first character to data-name-3's last
                // (§13.18.45.4 GR1/GR2 — a re-grouping of the record's characters; the alias reads/writes as one
                // elementary ALPHANUMERIC item of the window's width). kb/Work PB96: the former leaf-run walk listed
                // every leaf between FROM and THRU — a REDEFINES view included, as if it occupied its own characters —
                // so `RENAMES A THRU C` over A / B REDEFINES A / C was 6 characters, not 4. Offsets now come from the
                // ONE recursive storage function (a redefiner sits at its target's offset, §13.18.44), the parts are
                // the record's NON-redefining leaves that intersect the window, and a boundary inside a leaf (a FROM /
                // THRU that partially redefines it) is a partial part.
                int Width(DataItem d) => d.ByteWidth * (d.Occurs ?? 1);   // STORAGE bytes: a national position is two (D-N1; kb/Work PB1665)
                int Offset(DataItem d)
                {
                    if (d.RedefinesTarget is { } tgt) return Offset(tgt);          // an overlay starts where its target does
                    if (d.Parent is not { } par) return 0;                           // the record itself
                    int off = Offset(par);
                    foreach (var sib in par.Children)
                    {
                        if (ReferenceEquals(sib, d)) break;
                        if (sib.RedefinesTargetName is null) off += Width(sib);       // a redefining sibling adds no storage
                    }
                    return off;
                }
                int startOff = Offset(info.From);
                int endOff = Offset(info.Thru) + Width(info.Thru) - 1;
                // §13.18.45.3 SR11 — data-name-3 neither begins before data-name-2 nor ends at or before its end.
                (long Start, long End) ExtentOf(DataItem n)
                {
                    if (root.HasBitDescendant && BitLayout.StartBitOf(root, n) is var bitStart and >= 0)
                        return (bitStart, bitStart + BitLayout.RunBits(n) - 1);
                    int at = Offset(n);
                    return (at, at + Width(n) - 1);
                }
                if (!ScreenRenamesWindowOrder(binder, ren, info, ExtentOf)) continue;
                // §13.18.45.3 SR10 — "The area described by data-name-2 THROUGH data-name-3 shall define an integral
                // number of bytes" (kb/Work PB1284). Only a USAGE BIT item can make it fractional: §8.5.1.6.3 packs
                // same-level bit items into shared bytes, so `A PIC 1(3) USAGE BIT` THRU `B PIC 1(2) USAGE BIT` is a
                // 5-bit area the character offsets above cannot even see. Measured on THE ONE bit layout
                // (BitLayout), and only for a record that holds a bit leaf — without one every extent is whole
                // characters by construction.
                if (root.HasBitDescendant
                    && BitLayout.StartBitOf(root, info.From) is var fromBit and >= 0
                    && BitLayout.StartBitOf(root, info.Thru) is var thruBit and >= 0
                    && (thruBit + BitLayout.RunBits(info.Thru) - fromBit) is var areaBits
                    && areaBits % BitLayout.BitsPerCharacter != 0)
                {
                    using var __b = binder.Edition.At(ren);
                    binder.Edition.Error(DiagnosticCatalog.RenamesAreaNotWholeBytes,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: the area is "
                        + $"{areaBits} bits, which is not a whole number of bytes; the area described by data-name-2 "
                        + "THROUGH data-name-3 shall define an integral number of bytes (ISO §13.18.45.3 SR10)");
                    continue;
                }
                // Every leaf of the record with its storage extent — REDEFINES views INCLUDED: a view reads the
                // storage it overlays exactly as the redefined entry does (NC252A's `RDF8-5 THRU RDF8-6` lies inside a
                // double redefinition of an OCCURS 36 table, and only those two views tile that window). Views are
                // never counted twice because the tiling below advances by what it covered.
                // ⛔ A LEAF UNDER AN OCCURS GROUP LIVES ONCE PER OCCURRENCE OF THAT GROUP (kb/Work PB986). §13.18.45.3 SR3
                // bars an OCCURS only on the three NAMED operands, so a table may lie INSIDE the window, and its
                // elementary items repeat at the GROUP's stride: S4 of `05 ST OCCURS 2. 10 S4 PIC X.` is two cells, one
                // per ST. The walk therefore carries the enclosing table groups, and each leaf contributes one RUN per
                // combination of their occurrences that the window reaches (a run = the leaf's own contiguous
                // occurrences within one outer occurrence); only the occurrences the window intersects are visited, so a
                // large table elsewhere in the record costs nothing. Counting the leaf `Occurs ?? 1` and ignoring the
                // group — the former walk — left the second occurrence uncovered and refused the legal alias as one that
                // "does not tile". A table whose extent is not fixed (dynamic-capacity) lies outside every legal window
                // (SR8, RenamesRangeFault) and is not entered.
                var leaves = new List<(DataItem Leaf, int Off, int W, int Occ, int[] Outer)>();
                void AddRuns(DataItem leaf, List<DataItem> tables, int level, int delta, List<int> outer)
                {
                    if (level == tables.Count)
                    {
                        int off = Offset(leaf) + delta, total = Width(leaf);
                        if (off + total - 1 >= startOff && off <= endOff)
                            leaves.Add((leaf, off, leaf.ByteWidth, leaf.Occurs ?? 1, [.. outer]));
                        return;
                    }
                    var table = tables[level];
                    int stride = table.ByteWidth, groupOff = Offset(table) + delta;
                    int first = startOff <= groupOff ? 1 : (startOff - groupOff) / stride + 1;
                    int last = endOff < groupOff ? 0 : Math.Min(table.Occurs!.Value, (endOff - groupOff) / stride + 1);
                    for (int k = first; k <= last; k++)
                    {
                        outer.Add(k);
                        AddRuns(leaf, tables, level + 1, delta + (k - 1) * stride, outer);
                        outer.RemoveAt(outer.Count - 1);
                    }
                }
                void Walk(DataItem n, List<DataItem> tables)
                {
                    if (n.IsElementary) { if (Width(n) > 0) AddRuns(n, tables, 0, 0, []); return; }
                    if (n.IsDynamicTable) return;
                    bool isTable = n.Occurs is not null;
                    if (isTable) tables.Add(n);
                    foreach (var c in n.Children) Walk(c, tables);
                    if (isTable) tables.RemoveAt(tables.Count - 1);
                }
                Walk(root, []);
                // GREEDY TILING of [startOff, endOff]: at each position take the LONGEST whole leaf (or whole table
                // cell) that starts exactly there and fits inside the window; else the most specific (narrowest) leaf
                // cell containing the position, as a partial slice up to the window's end or the cell's end.
                int pos = startOff;
                bool stuck = false;
                while (pos <= endOff)
                {
                    // ⛔ EVERY CANDIDATE IS ADDRESSABLE, because a part is a range of STORAGE BYTES (RenamesSpanPart;
                    // kb/Work PB2466, PB1902): a slice whose boundary falls on the odd byte of a national character, and
                    // an interior byte only half a national character covers (an alphanumeric REDEFINES view shorter than
                    // the national item it redefines), are bytes of the record's storage like any other, and the alias
                    // is "an alphanumeric group item" over that storage (§13.18.45.4 GR2). Parts used to be kept in their
                    // leaf's positions, so such a slice was chosen and then refused — COBOL1655 on legal source.
                    RenamesSpanPart? best = null;
                    foreach (var (leaf, off, w, occ, outer) in leaves)
                    {
                        int total = w * occ;
                        if (pos < off || pos > off + total - 1) continue;          // the leaf does not contain pos
                        // the whole leaf (every occurrence) starting exactly here and fitting the window
                        if (off == pos && off + total - 1 <= endOff)
                            Consider(new RenamesSpanPart(leaf, null, 1, total, outer));
                        int k = (pos - off) / w + 1;                                // the occurrence containing pos
                        int cellLo = off + (k - 1) * w, cellHi = cellLo + w - 1;
                        if (cellLo == pos && cellHi <= endOff)                      // a whole cell (or the whole 1-occurrence leaf)
                            Consider(new RenamesSpanPart(leaf, occ == 1 ? null : k, 1, w, outer));
                        else                                                        // a partial slice of the containing cell
                            Consider(new RenamesSpanPart(leaf, k, pos - cellLo + 1, Math.Min(endOff, cellHi) - pos + 1, outer));
                    }
                    void Consider(RenamesSpanPart c)
                    {
                        // prefer: whole (non-partial) over partial; then the longest cover; then the narrowest leaf
                        if (best is not { } b
                            || (b.IsPartial && !c.IsPartial)
                            || (b.IsPartial == c.IsPartial && c.LengthBytes > b.LengthBytes)
                            || (b.IsPartial == c.IsPartial && c.LengthBytes == b.LengthBytes && c.Leaf.ByteWidth < b.Leaf.ByteWidth))
                            best = c;
                    }
                    if (best is not { LengthBytes: > 0 } chosen) { stuck = true; break; }
                    info.Span.Add(chosen);
                    pos += chosen.LengthBytes;
                }
                if (stuck || info.Span.Count == 0)
                {
                    using var __u = binder.Edition.At(ren);
                    binder.Edition.Error(DiagnosticCatalog.RenamesOperandUnresolved,
                        $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: the record's leaves do "
                        + "not tile the alias's storage window (kb/Work PB96)");
                    info.Span.Clear();
                    continue;
                }
                ren.Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: endOff - startOff + 1, Digits: 0, Scale: 0, Signed: false);
            }
    }

    /// <summary>The first §13.18.45.3 SR8 violation among the items WITHIN a RENAMES range, as the clause of the
    /// diagnostic naming the item and its shape, or null (kb/Work PB1284). SR8: "None of the items within the range,
    /// including data-name-2 and data-name-3, if specified, shall be of class object, message-tag, or pointer, a
    /// strongly-typed group item, an item subordinate to a strongly-typed group item, a variable-length data item, or
    /// an occurs-depending table."
    /// <para>THE RANGE is §13.18.45.4 GR2's: every entry from data-name-2 through the last entry of data-name-3's
    /// subtree, in declaration order — groups and elementary items alike, a REDEFINES view among them — or, without
    /// THROUGH, data-name-2 and its subordinates (GR1). It is enumerated over the DECLARATION tree, not over storage
    /// offsets, because the offending shapes are exactly the ones storage offsets cannot see: a dynamic-length item
    /// has zero fixed width, and a dynamic-capacity or occurs-depending table's extent is not its declared one.</para>
    /// <para>The strongly-typed arms are enforced with the rest of the strong-type rules they share a sentence
    /// with, <c>CheckStrongTypeDeclarations</c> (§13.18.57.3 SR3, COBOL1532); this screen owns the other three.
    /// The class arm reads §13.18.60.3 SR14's phrase reader, whose five phrases are exactly the usages of classes
    /// object (OBJECT REFERENCE), message-tag (MESSAGE-TAG) and pointer (POINTER, PROGRAM-POINTER,
    /// FUNCTION-POINTER).</para></summary>
    private static string? RenamesRangeFault(DataItem record, DataItem from, DataItem? thru)
    {
        var ordered = DataItem.SelfAndDescendantsOf(record).ToList();
        int first = ordered.IndexOf(from);
        var last = thru ?? from;
        int lastAt = ordered.IndexOf(last);
        if (first < 0 || lastAt < 0) return null;   // not in this record — reported by the operand resolution
        int end = lastAt;
        while (end + 1 < ordered.Count && OdoModel.IsWithin(ordered[end + 1], last)) end++;   // last's subtree
        for (int i = first; i <= end; i++)
        {
            var d = ordered[i];
            string name = $"'{d.CobolName ?? "FILLER"}'";
            if (VariableLengthCompatibility.IsVariableLengthDataItem(d))
                return $"{name} is a variable-length data item — "
                    + (d.IsDynamicTable ? "a dynamic-capacity table" : "a dynamic-length elementary item")
                    + " (ISO §8.5.1.11.1)";
            if (d.OccursSpec is { DependingName: not null })
                return $"{name} is an occurs-depending table";
            if (!d.IsGroup
                && (ItemCategory.Sr14PhraseOf(d.Pic?.Usage) ?? ItemCategory.Sr14PhraseOf(d.OwnUsage)) is { } phrase)
                return $"{name} is described with USAGE {phrase} — of class " + phrase switch
                {
                    "OBJECT REFERENCE" => "object",
                    "MESSAGE-TAG" => "message-tag",
                    "POINTER" or "PROGRAM-POINTER" or "FUNCTION-POINTER" => "pointer",
                    _ => throw new System.Diagnostics.UnreachableException(
                        $"§13.18.60.3 SR14 phrase '{phrase}' has no §8.5.2 class in the RENAMES SR8 screen"),
                };
        }
        return null;
    }

    /// <summary>The entry-level rules over the RESOLVED pair (subject, data-name-2), asked once per REDEFINES clause by
    /// <see cref="Run"/> — the one place both entries are known. <paramref name="scope"/> is the entry
    /// sequence the pair was resolved in (the subject's siblings, or the roots of its own section), in source order.</summary>
    private static void ScreenRedefinesEntry(DataBinder binder, DataItem item, DataItem target, IReadOnlyList<DataItem> scope)
    {
        string name = item.CobolName ?? "FILLER";
        string tname = target.CobolName ?? "FILLER";

        // SR10 — "The entries giving the new descriptions of the storage area shall follow the entries defining the
        // area of data-name-2, without intervening entries that define new storage areas." Every entry between
        // data-name-2 and the subject of the entry is a REDEFINITION (it gives another description of the same area and
        // defines none of its own) or breaks the rule: `05 A PIC X(2). 05 C PIC X. 05 B REDEFINES A` reaches PAST C. The
        // overlay layout (ClassifyRedefinesClasses) tolerates the gap, which is why nothing else could see it. A
        // compiler temporary is no entry of the program, and at top level only the roots of the subject's own section
        // lie between the pair (ResolveRedefines picked data-name-2 among exactly those).
        if (FirstInterveningStorage(item, target, scope) is { } between)
            binder.Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}' REDEFINES '{tname}': the entry '{between.CobolName ?? "FILLER"}' between them defines new "
                + "storage — the entries giving the new descriptions of the storage area shall follow the entries "
                + "defining the area of data-name-2, without intervening entries that define new storage areas "
                + "(ISO §13.18.44.3 SR10)");

        // SR2 — a 77 and an 01 are both roots, so name resolution alone cannot tell them apart; the written
        // level-numbers can.
        if (item.Level != target.Level)
            binder.Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}' REDEFINES '{tname}': the level-numbers of data-name-2 ({target.Level}) and the subject of "
                + $"the entry ({item.Level}) shall be identical (ISO §13.18.44.3 SR2)");

        // SR3 — first half: no REDEFINES in a level-1 FILE SECTION entry (the records of one FD already share their
        // area, §13.18.33.4 GR3, and that implicit redefinition is not this clause). The second half, an FD with a
        // FORMAT clause, belongs to the declined A.4.8 facility.
        if (item.Parent is null && item.Level == 1 && item.Section == EntrySection.File)
            binder.Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}': the REDEFINES clause shall not be specified in a level-1 entry in the file section "
                + "(ISO §13.18.44.3 SR3)");

        // SR13 — a constant record cannot be redefined and then modified through the redefining name.
        if (target.IsConstantRecord)
            binder.Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                $"'{name}' REDEFINES '{tname}': the data description entry for data-name-2 shall not contain the "
                + "CONSTANT RECORD clause (ISO §13.18.44.3 SR13)");

        // SR8 — "The storage area required for the subject of the entry shall not be larger than the storage area
        // required for the data item referenced by data-name-2, unless the data item referenced by data-name-2 has been
        // specified with level number 1 and without the EXTERNAL clause." §13.18.44.4 GR1 measures the association in
        // BITS ("the number of bits required by the data item referenced by the subject of the entry"), so a bit item
        // is measured in bits and everything else in bytes of eight (kb/Work PB231: a national position is two
        // bytes). An EXTERNAL level-1 target is NOT exempt: its area is the run-unit's one shared area, and a larger
        // redefiner would run past it.
        bool exempt = target.Level == 1 && !target.HasExternalClause;
        if (!exempt && StorageBitsOf(item) is var itemBits && StorageBitsOf(target) is var targetBits && itemBits > targetBits)
            binder.Edition.Error("COBOL1539", $"'{name}' REDEFINES '{tname}': the redefining storage area "
                + $"({itemBits} bits) is larger than the redefined ({targetBits}) — permitted only when the redefined "
                + "item is level 1 and not EXTERNAL (ISO §13.18.44.3 SR8)");

        // SR15 — "The description of the subject of the entry shall be such that its required alignment is the same
        // as the alignment of the data item referenced by data-name-2." §8.5.1.6.3 puts an elementary bit item that
        // follows a same-level bit item at the NEXT BIT position and every other data item on a byte boundary (an
        // ALIGNED clause, §13.18.1.4 GR1, makes a bit item the latter too). A subject needing a byte boundary over a
        // data-name-2 that begins mid-byte has no storage association §13.18.44.4 GR1 can make: the area "starts at
        // the first bit of the data item referenced by data-name-2". A bit-aligned subject fits any starting bit. The
        // "requires a byte boundary" is the layout's own placement predicate, so a bit group whose first item is
        // ALIGNED requires one too: it begins where that item does (§8.5.1.6.3; kb/Work PB3167).
        if (!BitLayout.KeepsPosition(item))
        {
            int start = TargetStartBit(target);
            if (start > 0 && start % BitLayout.BitsPerCharacter != 0)
                binder.Edition.Error(DiagnosticCatalog.RedefinesEntryRule,
                    $"'{name}' REDEFINES '{tname}': '{name}' requires a byte boundary, but '{tname}' begins at bit "
                    + $"{start % BitLayout.BitsPerCharacter} of its byte — the required alignment of the subject shall be "
                    + "the same as the alignment of the data item referenced by data-name-2 (ISO §13.18.44.3 SR15; "
                    + "§8.5.1.6.3)");
        }
    }

    /// <summary>The first entry of <paramref name="scope"/> after <paramref name="target"/> and before the subject that
    /// DEFINES storage (§13.18.44.3 SR10) — an entry with no REDEFINES clause of its own; null when the pair is adjacent
    /// or only redefinitions lie between them (several redefinitions of one area are the clause's whole purpose, SR7).</summary>
    private static DataItem? FirstInterveningStorage(DataItem item, DataItem target, IReadOnlyList<DataItem> scope)
    {
        bool afterTarget = false;
        foreach (var entry in scope)
        {
            if (ReferenceEquals(entry, item)) return null;
            if (ReferenceEquals(entry, target)) { afterTarget = true; continue; }
            if (afterTarget && entry.RedefinesTargetName is null && !entry.IsCompilerTemp
                && (item.Parent is not null || entry.Section == item.Section))
                return entry;
        }
        return null;
    }

    /// <summary>The storage a REDEFINES overlay measures (§13.18.44.3 SR8 / §13.18.44.4 GR1), in bits: a bit item
    /// exactly (<see cref="BitLayout.RunBits"/>, occurrences included), anything else its byte width times eight.</summary>
    private static long StorageBitsOf(DataItem d) =>
        BitLayout.IsBitItem(d) ? BitLayout.RunBits(d)
        : (long)d.ByteWidth * (d.Occurs ?? 1) * BitLayout.BitsPerCharacter;

    /// <summary>The bit offset of <paramref name="target"/> within its record (§8.5.1.6.3 placement), or -1 when an
    /// overlay chain below it cannot be resolved — the callers must not reject on -1.</summary>
    private static int TargetStartBit(DataItem target)
    {
        var root = target;
        while (root.Parent is { } p) root = p;
        return ReferenceEquals(root, target) ? 0 : BitLayout.StartBitOf(root, target);
    }

    /// <summary>§13.18.45.3 SR5, SR3 and SR6 over one resolved operand, reported against the alias
    /// <paramref name="ren"/>; false when any was violated (the alias is then left unbound, like any refused
    /// range). <paramref name="owner"/> is the record — or the group a TYPEDEF's RENAMES was cloned onto — whose scope
    /// resolved the operand; the OCCURS walk climbs past it to the true record root, because an alias inside a table is
    /// itself subject to that table's OCCURS (data-name-1 is named by SR3 as well).
    /// <para>The OCCURS test is the operand and its ANCESTORS only — never the items BETWEEN the endpoints: a table may
    /// lie inside the window (PB986 tiles it), and testing the span would re-create that over-rejection.</para></summary>
    private static bool ScreenRenamesOperand(DataBinder binder, DataItem owner, DataItem ren, DataItem operand, string role)
    {
        string alias = ren.CobolName ?? "FILLER";
        string op = operand.CobolName ?? "FILLER";
        bool ok = true;

        // SR5 — "Neither data-name-2 nor data-name-3 shall refer to an entry that is described with level-number 1, 66,
        // 77, or 88." Levels 66 and 88 are off the storage tree and already unreachable (SR4); a level-1 or level-77
        // entry is on it, and the owner itself (a record, or the group a type was applied to) is such an entry.
        if (operand.Level is 1 or 77 || ReferenceEquals(operand, owner))
        {
            binder.Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                $"'{alias}' RENAMES: {role} '{op}' refers to an entry described with level-number "
                + $"{(operand.Level is 77 ? "77" : "1")} (ISO §13.18.45.3 SR5: \"Neither data-name-2 nor data-name-3 "
                + "shall refer to an entry that is described with level-number 1, 66, 77, or 88.\")");
            ok = false;
        }

        // SR3 — "Data-name-1, data-name-2 and data-name-3 shall not be subject to any OCCURS clauses": the operand's
        // own OCCURS or any ancestor's.
        for (var n = operand; n is not null; n = n.Parent)
            if (n.IsTable)
            {
                binder.Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                    $"'{alias}' RENAMES: {role} '{op}' is subject to an OCCURS clause"
                    + (ReferenceEquals(n, operand) ? "" : $" (of '{n.CobolName ?? "FILLER"}')")
                    + " (ISO §13.18.45.3 SR3: \"Data-name-1, data-name-2 and data-name-3 shall not be subject to any "
                    + "OCCURS clauses.\")");
                ok = false;
                break;
            }

        // SR6 — no operand within a CONSTANT RECORD: the alias would be a modifiable name for constant content.
        if (binder.IsConstantRecordItem(operand))
        {
            binder.Edition.Error(DiagnosticCatalog.RenamesEntryRule,
                $"'{alias}' RENAMES: {role} '{op}' is an entry within a record whose data description entry includes "
                + "the CONSTANT RECORD clause (ISO §13.18.45.3 SR6)");
            ok = false;
        }
        return ok;
    }

    /// <summary>§13.18.45.3 SR11 — "The beginning of the storage area described by data-name-3 shall not precede the
    /// beginning of the storage area described by data-name-2. The end of the storage area described by data-name-3
    /// shall follow the end of the storage area described by data-name-2. NOTE Data-name-3, therefore, cannot be
    /// subordinate to data-name-2." Both ends are asked (the screen used to test only that data-name-3 did not END before
    /// data-name-2 BEGAN, which is a strict subset). Extents are in BITS for a record that holds a bit item (§8.5.1.6.3
    /// packs same-level bit items into shared bytes), else in bytes — the measure the tiler below uses.</summary>
    private static bool ScreenRenamesWindowOrder(DataBinder binder, DataItem ren, RenamesInfo info, Func<DataItem, (long Start, long End)> extentOf)
    {
        var (start2, end2) = extentOf(info.From!);
        var (start3, end3) = extentOf(info.Thru!);
        if (start3 >= start2 && end3 > end2) return true;
        binder.Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{ren.CobolName ?? "FILLER"}' RENAMES {info.FromName} THRU {info.ThruName}: "
            + (start3 < start2
                ? "the beginning of data-name-3 precedes the beginning of data-name-2"
                : "the end of data-name-3 does not follow the end of data-name-2 (data-name-3 cannot be subordinate to, "
                  + "or coincide with, data-name-2)")
            + " (ISO §13.18.45.3 SR11: \"The beginning of the storage area described by data-name-3 shall not precede the "
            + "beginning of the storage area described by data-name-2. The end of the storage area described by "
            + "data-name-3 shall follow the end of the storage area described by data-name-2.\")");
        return false;
    }
}
