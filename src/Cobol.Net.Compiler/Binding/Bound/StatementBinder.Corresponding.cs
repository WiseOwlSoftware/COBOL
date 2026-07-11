// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>The verb a CORRESPONDING statement expands into per-pair implied statements
/// (ISO §14.9.25.2 MOVE Format 2 / §14.9.2.2 ADD Format 3 / §14.9.44.2 SUBTRACT Format 3).</summary>
public enum CorrVerb { Move, Add, Subtract }

/// <summary>One hoisted group-operand anchor, emitted ONCE before the first implied statement — §14.7.6: all item
/// identification for the pairs (including any subscript on the group operands) is done at the START of the
/// statement, never per implied statement. <paramref name="IsRef"/> selects the C# form: a <c>ref var</c> local
/// aliasing a member-path group (its <c>CobolTable.At</c> subscripts evaluate exactly once), or a <c>long</c> local
/// pinning a Tier-B REDEFINES view group's computed window offset.</summary>
public sealed record CorrespondingHoist(string Local, string Init, bool IsRef);

/// <summary>One corresponding pair (§14.7.6): the resolved sending and receiving <see cref="Place"/>s of an
/// implied per-pair statement. Both are anchored on the statement's hoisted group locals where applicable.</summary>
public sealed record CorrespondingPair(Place Source, Place Target);

/// <summary>A MOVE/ADD/SUBTRACT CORRESPONDING statement, expanded at BIND time into its corresponding pairs in D1
/// declaration order (§14.7.6 — "the order in which the elements in the group data item immediately following
/// CORRESPONDING are specified"). <paramref name="Rounding"/> is the statement's ONE rounded-phrase mode, applied
/// to EVERY pair store (§14.9.2.2/§14.9.44.2 — a single rounded-phrase after the receiving group); MOVE carries
/// <see cref="CobolRounding.Truncation"/>. <paramref name="SizeError"/> is STATEMENT-level (§14.7.6): one latching
/// flag across all checked pair stores, ONE phrase dispatch after every pair completes, the NOT branch suppressed
/// when any pair erred — never a per-pair phrase.</summary>
public sealed record BoundCorresponding(
    CorrVerb Verb,
    IReadOnlyList<CorrespondingHoist> Hoists,
    IReadOnlyList<CorrespondingPair> Pairs,
    CobolRounding Rounding,
    SizeErrorPhrase? SizeError) : BoundStatement;

public sealed partial class StatementBinder
{
    private int _corrCounter;   // unique hoist-local ids (__corrNs / __corrNt) across the program unit

    /// <summary>Bind <c>ADD {CORRESPONDING|CORR} id-4 TO id-5 [ROUNDED] [ON SIZE ERROR …] [END-ADD]</c>
    /// (ISO §14.9.2.2 Format 3; SR5 — CORR and CORRESPONDING are equivalent, BOTH tokens tested). The one
    /// rounded-phrase follows the receiving group and applies to every implied statement; <c>ROUNDED MODE IS</c>
    /// is edition-gated inside <see cref="RoundingOf"/> (2014+, §14.7.4). Reached from <see cref="BindAdd"/> when
    /// the operand-list alternative is absent — i.e. exactly the Format-3 parse.</summary>
    private BoundStatement BindAddCorresponding(Core.AddStatementContext add) =>
        add.CORRESPONDING() is not null || add.CORR() is not null
            ? BindCorresponding(CorrVerb.Add, add.dataReference(), RoundingOf(add.roundedPhrase()),
                BindSizeError(add.arithmeticOnSizeError()))
            : new BoundUnsupported("ADD statement form");

    /// <summary>Bind <c>SUBTRACT {CORRESPONDING|CORR} id-4 FROM id-5 [ROUNDED] [ON SIZE ERROR …] [END-SUBTRACT]</c>
    /// (ISO §14.9.44.2 Format 3; SR5 — CORR ≡ CORRESPONDING).</summary>
    private BoundStatement BindSubtractCorresponding(Core.SubtractStatementContext sub) =>
        sub.CORRESPONDING() is not null || sub.CORR() is not null
            ? BindCorresponding(CorrVerb.Subtract, sub.dataReference(), RoundingOf(sub.roundedPhrase()),
                BindSizeError(sub.arithmeticOnSizeError()))
            : new BoundUnsupported("SUBTRACT statement form");

    /// <summary>
    /// Bind a CORRESPONDING statement (ISO §14.7.6): resolve both group operands ONCE, hoist their anchors (item
    /// identification at statement start), compute the corresponding pairs at BIND time, and expand to a
    /// <see cref="BoundCorresponding"/> the backend renders as the per-pair implied statements (MOVE GR11 §14.9.25.4 /
    /// ADD GR5 §14.9.2.4 / SUBTRACT GR5 §14.9.44.4 — "the same as if the user had referred to each pair … in
    /// separate statements"). The entire Format-2/Format-3 surface is COBOL-85 (no edition gate on the forms
    /// themselves); the SR6/SR12 2002+/2014+ operand categories (national / bit / strongly-typed / variable-length
    /// groups) and the rule-4 object/pointer/message-tag exclusion classes have no representation in this data
    /// model yet — this binder is the seam that gates them when those usages bind.
    /// </summary>
    private BoundStatement BindCorresponding(
        CorrVerb verb, Core.DataReferenceContext[] groups, CobolRounding rounding, SizeErrorPhrase? sizeErr)
    {
        string verbName = verb switch { CorrVerb.Move => "MOVE", CorrVerb.Add => "ADD", _ => "SUBTRACT" };
        if (groups.Length < 2)
            return new BoundUnsupported($"{verbName} CORRESPONDING operand shape");
        if (refs.Resolve(groups[0]) is not { } src)
            return new BoundUnsupported($"{verbName} CORRESPONDING source group '{groups[0].GetText()}'");
        if (refs.Resolve(groups[1]) is not { } dst)
            return new BoundUnsupported($"{verbName} CORRESPONDING receiving group '{groups[1].GetText()}'");
        // Both operands shall be GROUP items (MOVE §14.9.25.3 SR12; ADD §14.9.2.3 SR6; SUBTRACT §14.9.44.3 SR6).
        // SR12's "not reference-modified" and SR6's "not level-66" hold structurally here: reference modification
        // resolves only over elementary character items (never a group), and RENAMES entries are not data-tree
        // members — both shapes already failed the resolves above.
        if (!src.Item.IsGroup || !dst.Item.IsGroup)
            return new BoundUnsupported($"{verbName} CORRESPONDING over an elementary operand "
                + $"'{(src.Item.IsGroup ? dst.Item.CobolName : src.Item.CobolName)}' "
                + "(group items required — ISO §14.9.25.3 SR12 / §14.9.2.3 SR6 / §14.9.44.3 SR6)");

        int id = _corrCounter++;
        var hoists = new List<CorrespondingHoist>();
        if (CorrAccess.Create(src, $"__corr{id}s", hoists, refs) is not { } srcAcc
            || CorrAccess.Create(dst, $"__corr{id}t", hoists, refs) is not { } dstAcc)
            return new BoundUnsupported($"{verbName} CORRESPONDING group operand storage shape "
                + $"('{src.Item.CobolName}' / '{dst.Item.CobolName}' is neither a member path nor a REDEFINES view)");

        var pairs = new List<CorrespondingPair>();
        if (CorrMatch(verb, verbName, src.Item, dst.Item, srcAcc, dstAcc, [], [], pairs, rounding) is { } fail)
            return new BoundUnsupported(fail);
        // Zero pairs is a legal EMPTY implied set (§14.7.6 selects none silently): MOVE does nothing; an arithmetic
        // statement raises no size error, so a NOT ON SIZE ERROR phrase still runs (§14.7.5 rule 3).
        return new BoundCorresponding(verb, hoists, pairs, rounding, sizeErr);
    }

    /// <summary>
    /// The pair matcher (ISO §14.7.6, one source level per call): index D2's ELIGIBLE children by name, iterate
    /// D1's children in DECLARATION order (the implied-statement execution order), and for each name present and
    /// unique on BOTH sides descend matched group×group levels (rule 1 — identical relative qualification paths)
    /// or yield the pair, filtered per verb (rule 2 move-validity / rule 3 both-numeric). Rule 5 holds for free:
    /// the recursion never descends into an ineligible (OCCURS/REDEFINES) child. Returns a loud-failure reason for
    /// an unreachable storage shape, else <see langword="null"/>.
    /// </summary>
    private string? CorrMatch(CorrVerb verb, string verbName, DataItem src, DataItem dst,
        CorrAccess srcAcc, CorrAccess dstAcc, List<DataItem> sChain, List<DataItem> dChain,
        List<CorrespondingPair> pairs, CobolRounding rounding)
    {
        var dstByName = new Dictionary<string, List<DataItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dst.Children)
        {
            if (!CorrEligible(d)) continue;
            if (!dstByName.TryGetValue(d.CobolName!, out var list)) dstByName[d.CobolName!] = list = [];
            list.Add(d);
        }
        // Rule 6 is SYMMETRIC ("the name … is unique after application of the implied qualifiers"): a duplicated
        // eligible name on EITHER side makes the implied qualified reference ambiguous — excluded, not an error.
        // (The legacy matcher checked only the target side; the spec governs.)
        var srcCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in src.Children)
            if (CorrEligible(s)) srcCount[s.CobolName!] = srcCount.GetValueOrDefault(s.CobolName!) + 1;

        foreach (var s in src.Children)
        {
            if (!CorrEligible(s)) continue;
            if (srcCount[s.CobolName!] > 1) continue;                          // rule 6, source side
            if (!dstByName.TryGetValue(s.CobolName!, out var cands)) continue; // rule 1: no same-named target
            if (cands.Count > 1) continue;                                     // rule 6, target side
            DataItem d = cands[0];

            if (s.IsGroup && d.IsGroup)
            {
                sChain.Add(s); dChain.Add(d);
                string? fail = CorrMatch(verb, verbName, s, d, srcAcc, dstAcc, sChain, dChain, pairs, rounding);
                sChain.RemoveAt(sChain.Count - 1); dChain.RemoveAt(dChain.Count - 1);
                if (fail is not null) return fail;
                continue;
            }
            if (verb is CorrVerb.Move)
            {
                // Rule 2: at least one elementary (guaranteed — not both groups) AND the move valid per Table 16;
                // an invalid combination means the pair simply does NOT correspond (silent skip, not an error).
                if (!CorrMoveValid(s, d)) continue;
            }
            // Rule 3: BOTH shall be numeric data items — a group is class alphanumeric and numeric-edited is
            // category numeric-edited (§8.4.2), so only elementary-numeric × elementary-numeric qualifies; every
            // other namesake silently does not correspond. (The legacy matcher lacked this filter; spec governs.)
            else if (s.Pic?.Category is not PicCategory.Numeric || d.Pic?.Category is not PicCategory.Numeric)
                continue;

            sChain.Add(s); dChain.Add(d);
            Place? sp = srcAcc.ChildPlace(sChain);
            Place? dp = dstAcc.ChildPlace(dChain);
            sChain.RemoveAt(sChain.Count - 1); dChain.RemoveAt(dChain.Count - 1);
            if (sp is null || dp is null)
                return $"{verbName} CORRESPONDING pair '{s.CobolName}' (subscripted group operand over an "
                    + "interior REDEFINES class — member storage unreachable in this slice)";
            // The per-edition composite of operands is per PAIR — §14.9.2.3 SR1c / §14.9.44.3 SR1c: "the two
            // corresponding operands for each separate pair".
            if (verb is not CorrVerb.Move)
                CheckComposite(verbName, [new BoundNumRef(sp)], [new Receiver(dp, rounding)]);
            pairs.Add(new CorrespondingPair(sp, dp));
        }
        return null;
    }

    /// <summary>A child participates in correspondence matching unless excluded (ISO §14.7.6): rule 1 — FILLER
    /// (and a nameless group's whole subtree, which can never satisfy rule 1's name-qualified identity: a FILLER
    /// level contributes no qualifier — the legacy/NIST-proven reading); rule 4 — an OCCURS, REDEFINES, or RENAMES
    /// clause, or class index (USAGE INDEX, §13.18.60). The 2002+/2014 exclusion classes (object, pointer,
    /// message-tag) have no representation in this data model yet — nothing to test until those usages bind.
    /// Level-66 entries never join <see cref="DataItem.Children"/>, so rule 4's RENAMES leg is structural; the
    /// <see cref="DataItem.Renames"/> test is defensive.</summary>
    private static bool CorrEligible(DataItem item) =>
        item.CobolName is not null
        && !item.IsTable   // rule 4 — ANY OCCURS: fixed OR Format-4 DYNAMIC (Occurs is null for a dynamic table, D9)
        && item.RedefinesTargetName is null
        && item.Renames is null
        && item.Pic?.Usage is not Usage.Index;

    /// <summary>
    /// Rule 2's move-validity filter (ISO §14.7.6 r2 → §14.9.25.3 SR10, Table 16) over the modeled categories. A
    /// group operand is class/category alphanumeric (§8.8.4.1.1). Table 16 rows: alphanumeric → every modeled
    /// receiver; alphanumeric-EDITED → alphanumeric only (numeric / numeric-edited: No); numeric INTEGER (a
    /// fixed-point item with no fraction digits — a P-scaled ×10ⁿ item included) → every modeled receiver; numeric
    /// NONINTEGER (fraction digits, or a float usage) → numeric / numeric-edited only; numeric-edited → alphanumeric
    /// always, numeric / numeric-edited only with DE-EDITING — an ISO-2002 introduction, so gated ≥ 2002 (at
    /// COBOL-85 the pair does not correspond). The model folds ALPHABETIC (PIC A) into alphanumeric
    /// (<see cref="PicInfo.Analyze"/>), so Table 16's alphabetic-only prohibitions (numeric / numeric-edited /
    /// boolean → alphabetic: No) are not separable here — those pairs are admitted under the alphanumeric column.
    /// </summary>
    private bool CorrMoveValid(DataItem src, DataItem dst)
    {
        if (!src.IsGroup && src.Pic is null) return false;   // a childless PIC-less entry corresponds to nothing
        if (!dst.IsGroup && dst.Pic is null) return false;
        // The receiving side folds to Table 16's "Alphanumeric-edited, Alphanumeric" column (a group receiver is
        // an alphanumeric receiver) vs its "Numeric, Numeric-edited" column.
        bool dstIsAlphanumeric = dst.IsGroup || dst.Pic!.Category is PicCategory.Alphanumeric;
        if (src.IsGroup) return true;                                        // alphanumeric sending row: all Yes
        PicInfo sp = src.Pic!;
        return sp.Category switch
        {
            PicCategory.Alphanumeric when sp.EditMask is null => true,       // alphanumeric row: all Yes
            PicCategory.Alphanumeric => dstIsAlphanumeric,                   // AN-edited row: numeric/NE are No
            PicCategory.Numeric when !sp.IsFloat && sp.Scale <= 0 => true,   // integer row: all modeled Yes
            PicCategory.Numeric => !dstIsAlphanumeric,                       // noninteger row: AN is No
            PicCategory.NumericEdited =>                                     // NE row: AN Yes; N/NE = de-editing
                dstIsAlphanumeric || data.Edition.DialectLevel >= 2002,      // (ISO-2002 introduction)
            _ => false,
        };
    }

    /// <summary>
    /// The per-statement child-place factory over ONE resolved group operand: §14.7.6 requires all item
    /// identification (including the group's subscripts) at statement START, so the group is anchored exactly once
    /// — a member-path group by a <c>ref var</c> local (added lazily on first use, so no unused local is emitted),
    /// a Tier-B REDEFINES view group by a <c>long</c> local pinning its window offset. The relative chain from the
    /// group to a pair item never crosses an OCCURS level (rules 4/5 exclude them), so a member child is a plain
    /// dotted path off the anchor; a view child is a sibling window at the class-offset delta (every descendant of
    /// a view shares the class and carries its own <see cref="DataItem.ClassOffset"/>).
    /// </summary>
    private sealed class CorrAccess
    {
        private readonly List<CorrespondingHoist> _hoists;
        private readonly string _local;
        private readonly string _init;        // the member path, or the view's window-offset expression
        private readonly bool _isMember;      // MemberPlace group vs Tier-B RedefViewPlace group
        private readonly bool _subscripted;   // the member path evaluates a subscript (a CobolTable.At call)
        private readonly string _backing;
        private readonly DataItem _groupItem;
        private readonly ReferenceResolver _refs;
        private bool _hoisted;

        private CorrAccess(List<CorrespondingHoist> hoists, string local, string init, bool isMember,
            bool subscripted, string backing, DataItem groupItem, ReferenceResolver refs)
        {
            _hoists = hoists; _local = local; _init = init; _isMember = isMember;
            _subscripted = subscripted; _backing = backing; _groupItem = groupItem; _refs = refs;
        }

        /// <summary>Create the access over a resolved group place, or <see langword="null"/> for a storage shape
        /// no CORRESPONDING child can be built from (the caller fails loud). A <see cref="RefModPlace"/> group is
        /// impossible — reference modification resolves only over elementary character items (§14.9.25.3 SR12).</summary>
        public static CorrAccess? Create(Place group, string local, List<CorrespondingHoist> hoists, ReferenceResolver refs)
            => group switch
            {
                MemberPlace m => new CorrAccess(hoists, local, m.Path, isMember: true,
                    subscripted: m.Path.Contains("CobolTable.At(", StringComparison.Ordinal), backing: "", m.Item, refs),
                RedefViewPlace v => new CorrAccess(hoists, local, v.OffsetExpr, isMember: false,
                    subscripted: false, v.Backing, v.Item, refs),
                _ => null,
            };

        /// <summary>The pair item's <see cref="Place"/> for the relative <paramref name="chain"/> (group-exclusive,
        /// pair item last), or <see langword="null"/> for an unreachable shape (the caller fails loud).</summary>
        public Place? ChildPlace(IReadOnlyList<DataItem> chain)
        {
            DataItem leaf = chain[^1];
            if (_isMember)
            {
                if (chain.All(CorrPlainMember))
                    return new MemberPlace($"{Hoist(isRef: true)}.{string.Join('.', chain.Select(c => c.CsName))}", leaf);
                // A chain member inside a non-alias REDEFINES class stores in the class BACKING, not as a struct
                // member — reachable absolutely (from the root) only when the group reference has no subscript.
                return _subscripted ? null : _refs.ResolveItem(leaf);
            }
            // A Tier-B view group: the child window sits at the class-offset delta from the group's hoisted
            // offset; the group's subscript displacement (already inside the hoisted value) applies to both
            // identically (ISO §13.18.44 — a redefined table lays its occurrences end-to-end in the one backing).
            if (!ReferenceEquals(leaf.Class, _groupItem.Class)) return null;
            return new RedefViewPlace(_backing,
                $"{Hoist(isRef: false)} - {_groupItem.ClassOffset} + {leaf.ClassOffset}", leaf.ImageWidth, leaf);
        }

        /// <summary>Record the anchor hoist on first use and return its local name.</summary>
        private string Hoist(bool isRef)
        {
            if (!_hoisted)
            {
                _hoists.Add(new CorrespondingHoist(_local, isRef ? _init : $"(long)({_init})", isRef));
                _hoisted = true;
            }
            return _local;
        }

        /// <summary>True when a chain member is an ordinary struct member: no REDEFINES class, or the stored
        /// canonical of a Tier-A alias class (the alias tier keeps the canonical's typed field).</summary>
        private static bool CorrPlainMember(DataItem c) =>
            c.Class is null || (c.Class.Tier is RedefinesTier.Alias && c.IsCanonical);
    }
}
