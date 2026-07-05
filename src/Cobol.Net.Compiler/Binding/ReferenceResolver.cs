// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, and the subscript / reference-modification token group (a flat SUBSCRIPT-mode stream the
///         binding layer interprets — the same grammar shape the legacy compiler proved over 364 NIST tests).</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/>, interpret
///         the subscripts to C# index expressions, and build the member-access path with each subscript attached to
///         its OCCURS level (outer→inner).</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, a reference-modified reference (<c>(s:l)</c> — G2-1c), or a subscript form not yet handled — so the
/// caller emits a loud not-implemented guard rather than silently mis-binding.
/// </summary>
public sealed class ReferenceResolver(DataBinder data)
{
    /// <summary>The object-property reference BINDER (ISO §8.4.3.9; deep-dive D-P2): when normal
    /// qualification fails and the single qualifier is a class-name (factory form) or a TYPED object
    /// reference (instance form) whose roster carries an accessor for the head word under the PINNED
    /// §11.7.4 GR1a names, synthesize the compiler temp, register the pending op (drained into the
    /// GR1/GR2/GR3 BoundSequence by StatementBinder once the carrying statement's polarity is known), and
    /// return the temp so the statement binds over its Place. Returns null when the shape is NOT a property
    /// reference (the caller keeps its generic unknown-name diagnosis). SR checks here: SR1 (:7376) the
    /// REPOSITORY property-specifier, SR2 (:7378) no universal receiver — both COBOLNET0843; SR3/SR4
    /// (accessor existence) belong to the drain, where the sending/receiving polarity is known.</summary>
    private DataItem? OoTryBindPropertyReference(string name, List<string> qualifiers)
    {
        if (qualifiers.Count != 1 || data.OoClasses is not { } table) return null;
        string recv = qualifiers[0];

        OoClassSymbol? cls = table.Find(recv);
        bool factory = cls is not null;                      // prop OF Class-name → the FACTORY accessors (SR3/SR4 "or in the factory object")
        DataItem? recvItem = null;
        if (cls is null)
        {
            recvItem = data.LookupData(recv)?.FirstOrDefault();
            if (recvItem?.Pic is not { Category: PicCategory.ObjectReference } rp) return null;
            if (rp.ObjectClassName is not { } cn)
            {
                // The shape IS a property reference on a universal receiver — SR2 rejects it by name.
                data.Edition.Error("COBOLNET0843",
                    $"the object-property reference '{name}' OF '{recv}': the receiving identifier shall "
                    + "not be a universal object reference (ISO §8.4.3.9.3 SR2)");
                return null;
            }
            cls = table.Find(cn);
            if (cls is null) return null;                    // interface-typed receivers: property prototypes are a later refinement (0899 at the interface)
        }

        string pinned = DataItem.Sanitize(name).ToUpperInvariant();
        var get = factory ? cls.FindFactoryMethod("__GET_" + pinned) : cls.FindMethod("__GET_" + pinned);
        var set = factory ? cls.FindFactoryMethod("__SET_" + pinned) : cls.FindMethod("__SET_" + pinned);
        if (get is null && set is null) return null;         // not a property of the roster → generic diagnosis

        if (!data.OoRepositoryProperties.Contains(name))
            data.Edition.Error("COBOLNET0843",
                $"the object-property reference '{name}' OF '{recv}' requires a PROPERTY specifier in the "
                + "REPOSITORY paragraph (ISO §8.4.3.9.3 SR1; §12.3.8)");

        var model = get?.Returning ?? set!.Formals[0].Item;
        if (model.IsGroup)
        {
            data.Edition.Error("COBOLNET0899",
                $"the object-property reference '{name}' OF '{recv}': a GROUP-valued property reference "
                + "(the §8.4.3.9.4 temps over a group description) is a later refinement of the OO wave");
            return null;
        }

        var temp = data.OoCreatePropertyTemp(model, name);
        data.OoPendingPropertyOps.Add(new DataBinder.OoPendingPropertyOp(
            temp,
            recvItem is null ? null : PlaceForItem(recvItem, []),
            cls.CsName, factory, get, set, name, recv));
        return temp;
    }

    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported here.</summary>
    public Place? Resolve(Core.DataReferenceContext dref)
    {
        // LINAGE-COUNTER is the I-O control system's register (ISO §8.4.3.14) — runtime-sourced, never a storage
        // Place; the binder routes it to BoundLinageCounterRef. The early return also covers the QUALIFIED form
        // (`LINAGE-COUNTER OF file`), where dref.cobolWord() is the FILE-NAME qualifier and would otherwise
        // mis-resolve here as a base data-name.
        if (dref.LINAGE_COUNTER() is not null) return null;
        // LINE-COUNTER / PAGE-COUNTER are the Report Writer Control System's registers (ISO §8.4.3.15) —
        // runtime-sourced from the report engine, never a storage Place; the binder routes them to
        // BoundReportCounterRef (StatementBinder.ReportWriter.cs). The early return is LOAD-BEARING for the
        // QUALIFIED form (`LINE-COUNTER OF report`): there dref.cobolWord() is the REPORT-NAME qualifier and
        // would otherwise mis-resolve as a base data-name (the LINAGE-COUNTER lesson above).
        if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null) return null;
        if (dref.cobolWord() is not { } baseWord) return null;
        string name = baseWord.GetText();

        var qualifiers = new List<string>();
        Core.SubscriptOrRefModContext? subCtx = null;    // the subscript group (no depth-0 colon)
        Core.SubscriptOrRefModContext? refCtx = null;    // a reference-modification group (start : length)
        Core.RefModPartContext? cleanRef = null;         // the refModPart form (parsed arithmeticExpression : ...)

        void Classify(Core.SubscriptOrRefModContext s) { if (HasDepth0Colon(s)) refCtx ??= s; else subCtx ??= s; }

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                qualifiers.Add(q.cobolWord().GetText());
                foreach (var sp in q.subscriptPart()) if (sp.subscriptOrRefMod() is { } qs) Classify(qs);
                if (q.refModPart().Length > 0) cleanRef ??= q.refModPart()[0];
            }
            else if (suffix.refModPart() is { } rmp) cleanRef ??= rmp;
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s) Classify(s);
        }

        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        // The object-property fallback (§8.4.3.9.2 — `prop OF {class-name | identifier}` is textually a
        // qualified data reference, so it legitimately FAILS normal qualification): the hook synthesizes the
        // GR1–GR3 temp and the rest of THIS method gives the temp the full normal tail (subscript rejection —
        // a temp has no OCCURS — and reference-modification, which SR5/SR6 permit on the property value).
        item ??= OoTryBindPropertyReference(name, qualifiers);
        if (item is null) return null;

        List<string> indexExprs = [];
        if (subCtx is not null)
        {
            var (e, isRefMod) = InterpretSubscripts(subCtx);
            if (isRefMod || e is null) return null;   // unsupported subscript form → loud
            indexExprs = e;
        }

        // A level-66 RENAMES alias (ISO §13.18.45): one elementary-alphanumeric view COMPOSED over the spanned
        // leaves — reads concatenate their images, writes distribute slices back. This slice covers STRING-VALUED
        // leaves (X / edited / StoreAsImage); a typed-numeric leaf in the span fails loud (the image codecs for a
        // composed numeric leaf are a later slice). No subscripts: a RENAMES operand cannot have/live under OCCURS.
        if (item.Renames is { } ren)
        {
            if (indexExprs.Count > 0) return null;
            // The no-THRU form is an ALIAS: the 66 has the SAME description as the renamed item (§13.18.45 GR1)
            // — forward to its place outright (numeric stays numeric: NC252A's ADD 3500 TO RENAME-12 over a
            // PIC 9(4); a group forwards as the group). Only the THRU form composes an alphanumeric span (GR2).
            if (ren.Thru is null) return ren.From is { } fwd ? PlaceForItem(fwd, []) : null;
            if (ren.SpanLeaves.Count == 0) return null;
            var leafPlaces = new List<Place>(ren.SpanLeaves.Count);
            foreach (var leaf in ren.SpanLeaves)
            {
                // An OCCURS leaf inside the span contributes EVERY occurrence in order (§13.18.45 — the alias
                // covers the whole fixed-size area; NC252A's RENAME-7 over TABLE-ITEM-2 OCCURS 5).
                int occ = leaf.Occurs ?? 1;
                for (int k = 1; k <= occ; k++)
                {
                    if (PlaceForItem(leaf, leaf.Occurs is null ? [] : [k.ToString()]) is not { } lpRaw) return null;
                    Place lp = lpRaw;
                    bool stringValued = leaf.StoreAsImage || lp is RedefViewPlace
                        || leaf.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited;
                    // A typed NUMERIC-DISPLAY leaf participates through its character image (the alias is an
                    // alphanumeric view of the span, §13.18.45 — NC252A's PIC 999 leaves under RENAMES-TEST-1).
                    if (!stringValued)
                    {
                        if (leaf.Pic is not { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false })
                            return null;
                        lp = new NumericImagePlace(lp);
                    }
                    leafPlaces.Add(lp);
                }
            }
            return new RenamesPlace(leafPlaces, item);
        }

        if (PlaceForItem(item, indexExprs) is not { } inner) return null;

        if (refCtx is null && cleanRef is null) return inner;
        // Reference modification is over a character string — alphanumeric / numeric-edited items (incl. a Tier-B
        // view), or a NUMERIC USAGE-DISPLAY item viewed through its character image (ISO §8.4.2.4 — the unique
        // result is alphanumeric; NC224A's TEST-1-DATA(3:) over PIC 9(6)). Binary/packed usage stays loud.
        if (item.Pic?.Category is PicCategory.Numeric)
        {
            if (item.Pic is not { Usage: Usage.Display, IsFloat: false }) return null;
            if (!item.StoreAsImage && inner is not RedefViewPlace) inner = new NumericImagePlace(inner);
        }
        else if (item.Pic?.Category is not (PicCategory.Alphanumeric or PicCategory.NumericEdited)) return null;
        if (cleanRef is not null)
        {
            // The PARSED refModSpec form `(arithmetic-expression : [arithmetic-expression])` (ISO §8.4.2.4 —
            // the lexer stayed in DEFAULT mode): render each expression's leaf tokens through the same segment
            // renderer the SUBSCRIPT-mode form uses (NC224A's TEST-1-DATA(3:)).
            var rmExprs = cleanRef.refModSpec().arithmeticExpression();
            if (rmExprs.Length == 0) return null;
            var startToks = new List<IToken>();
            CollectLeafTokens(rmExprs[0], startToks);
            if (RenderSegment(startToks) is not { } rmStart) return null;
            string? rmLen = null;
            if (rmExprs.Length > 1)
            {
                var lenToks = new List<IToken>();
                CollectLeafTokens(rmExprs[1], lenToks);
                if (RenderSegment(lenToks) is not { } l) return null;
                rmLen = l;
            }
            return new RefModPlace(inner, rmStart, rmLen);
        }
        var (rm, _) = InterpretSubscripts(refCtx!);
        return rm is { Count: > 0 } ? new RefModPlace(inner, rm[0], rm.Count > 1 ? rm[1] : null) : null;
    }

    /// <summary>
    /// Build the typed <see cref="Place"/> for an already-resolved <paramref name="item"/> with its subscript index
    /// expressions, honoring the REDEFINES machinery (COBOLNET_DESIGN §3.4 / §4.2):
    /// <list type="bullet">
    ///   <item>a Tier-B (string-canonical) view → a <see cref="RedefViewPlace"/> (offset,width) window over the
    ///         class's ONE string backing (the canonical too, so exactly one stored member);</item>
    ///   <item>a Tier-A (alias) view → the canonical's ONE stored field, reinterpreted through the view's own
    ///         Pic/scale/profile (the place carries the VIEW's <see cref="DataItem"/>);</item>
    ///   <item>any other item → a plain <see cref="MemberPlace"/>.</item>
    /// </list>
    /// Returns <see langword="null"/> for a form not handled in this slice — a subscripted Tier-B view, a whole-OCCURS
    /// reference, a not-yet-wired Tier-C / Rejected view, or a subscript-count mismatch — so the caller fails loud.
    /// This is the ONE item→<see cref="Place"/> builder: both the syntactic <see cref="Resolve"/> path and the by-item
    /// <see cref="ResolveItem"/> path go through it, so EVERY consumer (verb operands, level-88 / SET conditional
    /// variables, FD record areas) sees identical view resolution.
    /// </summary>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<string> indexExprs)
    {
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class. Fail loud if the parent path is unavailable (e.g. it is itself within an
            // OCCURS), rather than emit an unqualified reference that does not exist in scope.
            if (BackingPath(sc) is not { } backing) return null;
            // A SUBSCRIPTED view: each OCCURS level on the item's path WITHIN the class displaces the window by
            // (occurrence − 1) × that level's per-occurrence width — the redefined table lays its occurrences
            // end-to-end in the ONE backing (ISO §13.18.44). ClassOffset is the occurrence-1 position; subscripts
            // map to the in-class OCCURS levels outer→inner, exactly as in AccessPath.
            var occursLevels = new List<DataItem>();
            for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, sc); n = n.Parent)
                if (n.Occurs is not null) occursLevels.Add(n);
            occursLevels.Reverse();
            if (occursLevels.Count != indexExprs.Count) return null;   // wrong subscript count → loud
            string offset = item.ClassOffset.ToString();
            for (int k = 0; k < occursLevels.Count; k++)
                offset += $" + ({indexExprs[k]} - 1) * {occursLevels[k].ImageWidth}";
            // A BASED class's window is displaced by the data-address pointer's runtime offset (ISO §13.18.5
            // — the view addresses wherever the pointer currently points; Phase-4b increment 2). The backing
            // property renders FIRST in both Read and Write, so the Deref null/bounds traps (GR3/GR4) fire
            // before the null-lenient OffsetOf.
            if (sc.BasedPointerField is { } addr)
                offset = $"CobolPtr.OffsetOf({addr}) + {offset}";
            if (item.IsGroup) data.WholeGroupReferenced.Add(item);
            return new RedefViewPlace(backing, offset, item.ImageWidth, item);
        }
        // A Tier-A view forwards to the canonical (a numeric view reinterprets the shared unscaled value via its own
        // scale, for free). A not-yet-wired (Tier-C) / Rejected view is loud.
        if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
            return null;
        DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
            ? ac.Canonical : item;
        // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
        if (AccessPath(accessItem, indexExprs) is not { } path) return null;
        // A group name can only be used as a whole operand (MOVE/DISPLAY/compare) — record it so the whole-group
        // analysis can decide which numeric-DISPLAY leaves must store their character image (§14.9 MOVE GR4).
        if (item.IsGroup) data.WholeGroupReferenced.Add(item);
        var member = new MemberPlace(path, item);
        // A group whose subtree contains an occurs-depending table is an ODO operand (ISO §13.18.38 GR8): wrap it so
        // the sending slice / receiving direction-split applies. data-name-1 is resolved post-build, declared anywhere
        // outside the table (SR20), and read at the operation site via CobolTable.Occ (storage-form-agnostic).
        if (item.IsGroup && OdoModel.TableUnder(item) is { OccursSpec.Depending: { } dep } table
            && ResolveItem(dep) is { } depPlace)
            return OdoModel.WrapGroup(member, depPlace, item, table);
        return member;
    }

    /// <summary>A <see cref="Place"/> for an already-resolved item with no subscripts (e.g. a level-88's conditional
    /// variable, a SET condition's variable, or an FD record area) — view-aware via <see cref="PlaceForItem"/>, so a
    /// REDEFINES view resolves to its window / canonical exactly as a verb operand does. <see langword="null"/> if the
    /// item is within an OCCURS table (a subscripted reference is then required) or is an unhandled view form.</summary>
    public Place? ResolveItem(DataItem item) => PlaceForItem(item, []);

    /// <summary>The place for an already-resolved <paramref name="item"/> using the SUBSCRIPTS of
    /// <paramref name="dref"/> — the condition-name-with-subscripts form (ISO §8.4.2.3 Format 2): a level-88
    /// reference's subscripts identify the occurrence of its CONDITIONAL VARIABLE. Null for an unhandled subscript
    /// form (the caller fails loud).</summary>
    public Place? ResolveForItem(Core.DataReferenceContext dref, DataItem item)
    {
        Core.SubscriptOrRefModContext? subCtx = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s && !HasDepth0Colon(s)) subCtx ??= s;
            else if (suffix.qualification() is { } q)
                foreach (var sp in q.subscriptPart())
                    if (sp.subscriptOrRefMod() is { } qs && !HasDepth0Colon(qs)) subCtx ??= qs;
        }
        List<string> indexExprs = [];
        if (subCtx is not null)
        {
            var (e, isRefMod) = InterpretSubscripts(subCtx);
            if (isRefMod || e is null) return null;
            indexExprs = e;
        }
        return PlaceForItem(item, indexExprs);
    }

    // ── Intrinsic-argument entries (ISO §15.3; consumed by StatementBinder.Intrinsics.cs) ─────────────────
    // The function-argument mini-parser resolves identifiers from flat SUBSCRIPT-mode tokens, where no
    // dataReference parse context exists — these thin internal entries expose the SAME private resolution
    // (ResolveUnqualified/ResolveQualified → PlaceForItem → RenderSegment) so argument references see identical
    // view/qualification/subscript semantics as every verb operand (singular-pattern rule).

    /// <summary>Resolve a (possibly OF/IN-qualified) data-name to its <see cref="DataItem"/>, or null. Used by
    /// the table(ALL) expansion (§15.3) to read the OCCURS counts before building per-occurrence places.</summary>
    internal DataItem? FindItem(string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count > 0 ? ResolveQualified(name, [.. qualifiers]) : ResolveUnqualified(name);

    /// <summary>Build the <see cref="Place"/> for a by-name reference with ALREADY-RENDERED C# index expressions
    /// (one per OCCURS level, outermost first). A level-66 RENAMES alias stays null (loud) — its composition
    /// lives only on the parse-context path.</summary>
    internal Place? ResolveByName(string name, IReadOnlyList<string> qualifiers, IReadOnlyList<string> indexExprs) =>
        FindItem(name, qualifiers) is { Renames: null } item ? PlaceForItem(item, indexExprs) : null;

    /// <summary>Render one subscript token segment to a C# index expression (the private
    /// <see cref="RenderSegment"/>), or null when the segment uses an unhandled form (caller fails loud).</summary>
    internal string? RenderIndexSegment(List<IToken> tokens) => RenderSegment(tokens);

    /// <summary>The qualified C# access path to a Tier-B/Tier-C class's single stored backing field. The backing is
    /// emitted in the canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable (the canonical is within an OCCURS table).</summary>
    private static string? BackingPath(RedefinesClass cls) =>
        cls.Canonical.Parent is not { } parent ? cls.BackingCsName
        : AccessPath(parent, []) is { } parentPath ? parentPath + "." + cls.BackingCsName
        : null;

    // ── Name resolution ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to disambiguate).</summary>
    private DataItem? ResolveUnqualified(string name)
    {
        // §11.7 GR5 — inside a METHOD body a method-local name (LINKAGE / LOCAL-STORAGE / method WS) SHADOWS
        // the same name in object data; sibling methods' names are invisible (each method has its own scope).
        if (data.ActiveMethodScope is { } m && m.ByName.TryGetValue(name, out var mlist) && mlist.Count > 0)
            return mlist[0];
        return data.ByName.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> by right-to-left narrowing: resolve the
    /// outermost qualifier, walk inward through each qualifier, then find <paramref name="name"/> within the
    /// innermost qualifier's subtree (ISO §8.4.3.3 qualification).
    /// </summary>
    private DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        // §8.4.2.2 — the FD/SD's FILE-NAME is the highest permissible qualifier of its record names and their
        // subordinates (SQ207M's `WRITE PRINT-REC IN PRINT-FILE`): scope the remaining reference to each of the
        // file's record descriptions — the named item may BE a record (not only a descendant of one).
        if (data.FilesByName.TryGetValue(qualifiers[^1], out var file))
        {
            foreach (var record in file.Records)
            {
                DataItem? s = qualifiers.Count >= 2
                    ? string.Equals(record.CobolName, qualifiers[^2], StringComparison.OrdinalIgnoreCase)
                        ? record : FindDescendant(record, qualifiers[^2])
                    : record;
                for (int k = qualifiers.Count - 3; k >= 0 && s is not null; k--)
                    s = FindDescendant(s, qualifiers[k]);
                if (s is null) continue;
                if (string.Equals(s.CobolName, name, StringComparison.OrdinalIgnoreCase)) return s;
                if (FindDescendant(s, name) is { } found) return found;
            }
            return null;
        }
        DataItem? scope = ResolveUnqualified(qualifiers[^1]);            // outermost qualifier
        for (int k = qualifiers.Count - 2; k >= 0 && scope is not null; k--)
            scope = FindDescendant(scope, qualifiers[k]);
        return scope is null ? null : FindDescendant(scope, name);
    }

    /// <summary>Find a descendant (direct or nested) of <paramref name="scope"/> with COBOL name <paramref name="name"/>.
    /// A record's level-66 RENAMES aliases live OFF the children (no storage) but ARE qualifiable by the record
    /// name (ISO §8.4.3.3 — <c>HARRY OF A-GLOB</c> where HARRY is a 66 of A-GLOB, NC209A), so they search too.</summary>
    private static DataItem? FindDescendant(DataItem scope, string name)
    {
        foreach (var child in scope.Children)
        {
            if (string.Equals(child.CobolName, name, StringComparison.OrdinalIgnoreCase)) return child;
            if (FindDescendant(child, name) is { } found) return found;
        }
        foreach (var ren in scope.Renames66)
            if (string.Equals(ren.CobolName, name, StringComparison.OrdinalIgnoreCase)) return ren;
        return null;
    }

    // ── Access-path construction (subscripts attach to OCCURS levels, outer→inner) ───────────────────────

    /// <summary>
    /// The C# member-access path for an item: a static field at the root, else <c>Parent.Child</c> chained, with
    /// each <paramref name="indexExprs"/> entry inserted as <c>[expr - 1]</c> at its OCCURS level (outermost first).
    /// Returns <see langword="null"/> if the subscript count does not match the table's OCCURS dimension.
    /// </summary>
    private static string? AccessPath(DataItem item, IReadOnlyList<string> indexExprs)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();   // root-first

        int occursLevels = chain.Count(n => n.Occurs is not null);
        if (occursLevels != indexExprs.Count) return null;   // wrong number of subscripts

        string path = "";
        int si = 0;
        foreach (var seg in chain)
        {
            path += path.Length == 0 ? seg.CsName : "." + seg.CsName;
            // Every subscripted access routes through the ref-returning CobolTable.At (ISO §8.4.2.3.4 GR2):
            // an out-of-range occurrence number continues benignly with subscript checking off (the COBOL-85
            // semantics — conditions and FAIL paths legally evaluate one-past-the-end references, e.g. an
            // induction variable after its UNTIL went true), instead of a raw CLR IndexOutOfRangeException.
            if (seg.Occurs is not null) path = $"CobolTable.At({path}, {indexExprs[si++]})";
        }
        return path;
    }

    // ── Subscript interpretation (the flat SUBSCRIPT-mode token stream) ──────────────────────────────────

    /// <summary>
    /// Interpret the flat subscript/ref-mod token sequence. Returns (index expressions, isRefMod): a depth-0
    /// <c>SUB_COLON</c> marks reference modification (handled in a later slice, so the C# list is null). Otherwise
    /// each comma- or multi-space-separated segment is rendered to a C# <c>long</c> index expression; a segment that
    /// cannot be rendered yields a null list (→ the caller fails loud).
    /// </summary>
    /// <summary>True if the flat token stream has a depth-0 <c>SUB_COLON</c> — i.e. it is a reference modification
    /// (<c>start:length</c>) rather than a subscript list.</summary>
    private static bool HasDepth0Colon(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) return true;
        }
        return false;
    }

    private (List<string>? Exprs, bool IsRefMod) InterpretSubscripts(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        int colonIdx = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) { colonIdx = i; break; }
        }
        if (colonIdx >= 0)   // reference modification: start [: length]
        {
            if (RenderSegment(tokens.GetRange(0, colonIdx)) is not { } start) return (null, true);
            var result = new List<string> { start };
            var lengthTokens = tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1);
            if (lengthTokens.Any(t => t.Type != Core.SUB_WS))
            {
                if (RenderSegment(lengthTokens) is not { } len) return (null, true);
                result.Add(len);
            }
            return (result, true);
        }

        var exprs = new List<string>();
        foreach (var seg in SplitSubscriptTokens(tokens))
        {
            if (RenderSegment(seg) is not { } e) return (null, false);
            exprs.Add(e);
        }
        return (exprs, false);
    }

    // Internal (not private): the intrinsic-argument binder (StatementBinder.Intrinsics.cs) flattens and splits
    // the SAME SUBSCRIPT-mode token streams for FUNCTION argument lists (ISO §15.3) — one splitter, not two.
    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split a flat token list into subscript segments on depth-0 comma / multi-space boundaries (a faithful
    /// reduction of the legacy <c>ExpressionBinder.SplitSubscriptTokens</c>: a single space inside a relative
    /// subscript such as <c>I + 1</c> does not split; a separator space before a new operand does).</summary>
    internal static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == Core.SUB_LPAREN) { depth++; current.Add(t); continue; }
            if (t.Type == Core.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == Core.SUB_COMMA || t.Type == Core.SUB_SEMICOLON))
            {
                if (current.Count > 0) { segments.Add(current); current = []; }
                continue;
            }
            if (depth == 0 && t.Type == Core.SUB_WS)
            {
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == Core.SUB_WS) next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
                    // A trailing operator OR a trailing OF/IN continues the SAME segment (`I + 1` relative
                    // subscripts; `SUB1 OF GRP` qualified subscripts, ISO §8.4.2.3.2) — and a pending OF/IN also
                    // continues into its qualifier identifier. The FUNCTION keyword (a plain SUB_IDENTIFIER in
                    // SUBSCRIPT mode) also continues: the following name belongs to a nested intrinsic call in a
                    // FUNCTION argument list — `SQRT(FUNCTION SQRT(F))` is ONE argument (ISO §15.3; the legacy
                    // splitter's endsWithFunction rule, dropped in the original subscript-only reduction).
                    bool continues = lastNonWs is not null &&
                        (lastNonWs.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR or Core.SUB_SLASH
                            or Core.SUB_POWER or Core.SUB_OF or Core.SUB_IN
                         || (lastNonWs.Type == Core.SUB_IDENTIFIER
                             && lastNonWs.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)));
                    int nextType = tokens[next].Type;
                    // The new-segment starters: every token that can BEGIN an operand — identifiers, all four
                    // numeric-literal shapes, string literals (intrinsic arguments may be space-separated,
                    // ISO §15's general formats), and the ALL subscript word (§15.3 table(ALL) arguments).
                    if (!continues && nextType is Core.SIGNED_INTEGERLIT or Core.SIGNED_DECIMALLIT
                            or Core.SUB_IDENTIFIER or Core.SUB_INTEGERLIT or Core.SUB_DECIMALLIT
                            or Core.SUB_STRINGLIT or Core.SUB_ALL)
                    {
                        segments.Add(current);
                        current = [];
                        i = next - 1;   // skip consumed WS
                        continue;
                    }
                    // A following OF/IN never splits — `name OF qualifier` stays one segment.
                }
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Render one subscript segment to a C# <c>long</c> index expression, or <see langword="null"/> if it
    /// uses a form not yet handled (so the caller fails loud). Handles integer literals, data-name / index-name
    /// references, the arithmetic operators, and parentheses — the relative-subscript and simple-index forms.</summary>
    private string? RenderSegment(List<IToken> tokens)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            switch (t.Type)
            {
                case Core.SUB_WS: sb.Append(' '); break;
                case Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.INTEGERLIT: sb.Append(t.Text); break;
                case Core.SUB_PLUS or Core.PLUS: sb.Append(" + "); break;
                case Core.SUB_MINUS or Core.MINUS: sb.Append(" - "); break;
                case Core.SUB_STAR or Core.STAR: sb.Append(" * "); break;
                case Core.SUB_SLASH or Core.SLASH: sb.Append(" / "); break;
                case Core.SUB_LPAREN or Core.LPAREN: sb.Append('('); break;
                case Core.SUB_RPAREN or Core.RPAREN: sb.Append(')'); break;
                case Core.SUB_IDENTIFIER or Core.IDENTIFIER:
                {
                    // Gather `name (OF|IN qualifier)*` — a QUALIFIED data-name subscript (ISO §8.4.2.3.2).
                    string name = t.Text;
                    var qualifiers = new List<string>();
                    int j = i;
                    while (true)
                    {
                        int k = j + 1;
                        while (k < tokens.Count && tokens[k].Type == Core.SUB_WS) k++;
                        if (k >= tokens.Count || tokens[k].Type is not (Core.SUB_OF or Core.SUB_IN or Core.OF or Core.IN)) break;
                        int m = k + 1;
                        while (m < tokens.Count && tokens[m].Type == Core.SUB_WS) m++;
                        if (m >= tokens.Count || tokens[m].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) break;
                        qualifiers.Add(tokens[m].Text);
                        j = m;
                    }
                    i = j;
                    if (ResolveSubscriptName(name, qualifiers) is not { } readExpr) return null;
                    sb.Append(readExpr);
                    break;
                }
                default: return null;   // SUB_STRINGLIT / SUB_DECIMALLIT / SUB_ALL / FUNCTION etc.
            }
        }
        string expr = sb.ToString().Trim();
        return expr.Length == 0 ? null : expr;
    }

    /// <summary>A subscript data-name → its C# read expression: an INDEXED BY index-name (a <c>long</c> field), or
    /// a (possibly OF/IN-qualified, ISO §8.4.2.3.2) numeric data item read; <see langword="null"/> if unresolvable.
    /// A data-item read is wrapped in <c>CobolTable.Occ(…)</c> — overload resolution converts a STRING-stored
    /// occurrence number (a leaf the post-bind whole-group analysis flags <see cref="DataItem.StoreAsImage"/>, a
    /// decision NOT yet made when this bind-time text is produced) exactly as a native <c>long</c>.</summary>
    private string? ResolveSubscriptName(string name, List<string> qualifiers)
    {
        if (qualifiers.Count == 0 && data.TryGetVisibleIndexField(name, out var field)) return field;
        DataItem? item = qualifiers.Count == 0 ? ResolveUnqualified(name) : ResolveQualified(name, qualifiers);
        return item is not null && AccessPath(item, []) is { } path ? $"CobolTable.Occ({path})" : null;
    }
}
