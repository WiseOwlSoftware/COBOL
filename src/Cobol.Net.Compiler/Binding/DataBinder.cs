// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Builds the bound DATA DIVISION model (a forest of <see cref="DataItem"/> trees, one per 01/77 item) from the
/// parse tree, and indexes every named item for reference resolution. Pure syntactic/semantic analysis — no byte
/// layout; the .NET type IS the storage. (Slice scope: WORKING-STORAGE groups + elementary items with fixed
/// OCCURS recorded; FILE/LINKAGE/LOCAL-STORAGE, level-66/88, and REDEFINES follow in later slices.)
/// </summary>
public sealed class DataBinder
{
    private int _fillerCounter;
    private int _uidCounter;

    /// <summary>The top-level (01/77) items of WORKING-STORAGE, in source order.</summary>
    public List<DataItem> Roots { get; } = [];

    /// <summary>
    /// Every named item, keyed by COBOL name (case-insensitive) → the list of items with that name. COBOL permits
    /// duplicate data-names disambiguated only by qualification (OF/IN), so this is a MULTIMAP — a single-valued
    /// dictionary would silently drop all but the last (a latent wrong-item bug; COBOLNET_DESIGN §3.5).
    /// </summary>
    public Dictionary<string, List<DataItem>> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>INDEXED BY index-names (case-insensitive) → the C# <c>long</c> field that holds the 1-based
    /// occurrence number (COBOLNET_DESIGN §3.5). A subscript may name an index, so the resolver consults this.</summary>
    public Dictionary<string, string> IndexFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Level-88 condition-names (case-insensitive) → the conditions with that name (a list, since names
    /// may be duplicated under different parents and disambiguated by qualification).</summary>
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Group items referenced as a whole (non-elementary) operand anywhere in the PROCEDURE DIVISION — recorded by
    /// <see cref="ReferenceResolver"/> as it resolves each reference. A group name can only be used as a whole (MOVE
    /// to/from it, DISPLAY it, compare it), so any resolved group reference is a whole-group operand. The bind-time
    /// <c>MarkStoreAsImage</c> pass consults this to decide which numeric-DISPLAY leaves must store their character
    /// image (ISO §14.9 MOVE GR4 — a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).
    /// </summary>
    public HashSet<DataItem> WholeGroupReferenced { get; } = [];

    /// <summary>Bind the WORKING-STORAGE section of a program unit (if present).</summary>
    public void Bind(Core.ProgramUnitContext program)
    {
        var ws = program.dataDivision()?.workingStorageSection();
        if (ws is null) return;

        // A level-number stack builds the tree: an entry attaches under the nearest open item whose level is lower.
        var stack = new Stack<DataItem>();
        var rootNames = new HashSet<string>(StringComparer.Ordinal);   // C#-field-name scope at the Program level
        foreach (var entry in ws.dataDescriptionEntry())
        {
            int.TryParse(entry.levelNumber().GetText(), out int lvl);
            // A level-88 entry is a condition-name on the immediately superior item — not a node in the tree.
            if (lvl == 88)
            {
                if (stack.Count > 0) BindCondition(entry, stack.Peek());
                continue;
            }
            // A level-66 RENAMES entry is a re-grouping alias on the owning record — not a node in the storage tree.
            if (lvl == 66)
            {
                BindRenames(entry);
                continue;
            }

            if (BindEntry(entry) is not { } item) continue;
            item.Uid = _uidCounter++;

            while (stack.Count > 0 && stack.Peek().Level >= item.Level)
                stack.Pop();

            if (stack.Count == 0)
            {
                item.CsName = Unique(item.CsName, rootNames);
                Roots.Add(item);
                _lastRoot = item;
            }
            else
            {
                var parent = stack.Peek();
                // A member name need only be unique within its containing struct (the parent's children).
                item.CsName = Unique(item.CsName, parent.Children.Select(c => c.CsName));
                item.Parent = parent;
                parent.Children.Add(item);
            }
            stack.Push(item);
            RegisterName(item);
        }

        // Post-build (the forest is complete): resolve REDEFINES/RENAMES targets, then group overlaid items into
        // shared-storage classes and assign each a tier (ISO §13.18.44/§13.18.45; COBOLNET_DESIGN §4).
        ResolveRedefines();
        ClassifyRedefinesClasses();
    }

    /// <summary>The most-recently-opened 01/77 record, so a following level-66 RENAMES attaches to its owner.</summary>
    private DataItem? _lastRoot;

    /// <summary>Index a named item in the <see cref="ByName"/> multimap (COBOL allows duplicate names disambiguated
    /// only by qualification).</summary>
    private void RegisterName(DataItem item)
    {
        if (item.CobolName is not { } name) return;
        if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
        list.Add(item);
    }

    /// <summary>Bind a level-66 RENAMES entry (ISO §13.18.45): a re-grouping alias <c>RENAMES from [THRU thru]</c>
    /// over a contiguous sibling run of the owning record. It adds no storage (SR2/SR3) — it is attached to the
    /// owning record's <see cref="DataItem.Renames66"/> list (not <see cref="DataItem.Children"/>) and registered for
    /// reference resolution; the FROM/THRU operands are resolved by the post-build pass.</summary>
    private void BindRenames(Core.DataDescriptionEntryContext entry)
    {
        var rc = entry.dataDescriptionBody().renamesClause();
        if (rc is null || entry.dataName()?.GetText() is not { } name || _lastRoot is null) return;
        bool thru = rc.THRU() is not null || rc.THROUGH() is not null;
        var item = new DataItem
        {
            Level = 66,
            CobolName = name,
            CsName = DataItem.Sanitize(name),
            Renames = new RenamesInfo
            {
                FromName = rc.dataReference(0).GetText(),
                ThruName = thru && rc.dataReference().Length > 1 ? rc.dataReference(1).GetText() : null,
            },
        };
        item.Uid = _uidCounter++;
        item.Parent = _lastRoot;        // owning record — an alias sibling, NOT a storage child
        _lastRoot.Renames66.Add(item);
        RegisterName(item);
    }

    /// <summary>Bind a level-88 condition-name on its conditional variable <paramref name="parent"/>, capturing the
    /// VALUE set (singletons + THRU ranges) as raw operand text (decoded at emit time).</summary>
    private void BindCondition(Core.DataDescriptionEntryContext entry, DataItem parent)
    {
        if (entry.dataName()?.GetText() is not { } name) return;
        var cond = new Condition88 { Name = name, Parent = parent };

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
                if (clause.valueClause() is { } value)
                    foreach (var vi in value.valueItem())
                    {
                        if (vi.valueClauseRange() is { } range)
                            cond.Values.Add((range.valueClauseOperand(0).GetText(), range.valueClauseOperand(1).GetText()));
                        else
                            foreach (var op in vi.valueClauseOperand())
                                cond.Values.Add((op.GetText(), null));
                    }

        if (!Conditions.TryGetValue(name, out var list)) Conditions[name] = list = [];
        list.Add(cond);
    }

    /// <summary>Make <paramref name="name"/> unique within a C# name scope, appending <c>_2</c>, <c>_3</c>, … on collision.</summary>
    private static string Unique(string name, IEnumerable<string> used)
    {
        var set = used as ICollection<string> ?? used.ToList();
        if (!set.Contains(name)) return name;
        for (int n = 2; ; n++)
        {
            string candidate = $"{name}_{n}";
            if (!set.Contains(candidate)) return candidate;
        }
    }

    /// <summary>Bind one data-description entry (skips level-66 RENAMES and level-88 condition names for now).</summary>
    private DataItem? BindEntry(Core.DataDescriptionEntryContext entry)
    {
        if (!int.TryParse(entry.levelNumber().GetText(), out int level)) return null;
        if (level is 66 or 88) return null; // RENAMES / condition-names: later slice.

        string? cobolName = entry.dataName()?.GetText();
        bool isFiller = cobolName is null || cobolName.Equals("FILLER", StringComparison.OrdinalIgnoreCase);
        string csName = isFiller ? $"_filler{_fillerCounter++}" : DataItem.Sanitize(cobolName!);

        string? pictureText = null, usageText = null, rawValue = null, redefinesTargetName = null;
        int? occurs = null;
        var indexNames = new List<string>();
        bool hasSign = false, signLeading = false, signSeparate = false;

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
            {
                if (clause.pictureClause()?.PIC_STRING() is { } picTok)
                    pictureText = picTok.GetText();
                else if (clause.usageClause() is { } usage)
                    usageText = UsageKeyword(usage);
                else if (clause.redefinesClause() is { } redef)
                    // Capture the target name only; resolution waits until the forest is built (the target is a
                    // prior sibling, but a chain A REDEFINES B REDEFINES C resolves in the post-build pass).
                    redefinesTargetName = redef.dataReference().GetText();
                else if (clause.valueClause() is { } value)
                    rawValue = ExtractValue(value);
                else if (clause.signClause() is { } sign)
                {
                    hasSign = true;
                    signLeading = sign.LEADING() is not null;
                    signSeparate = sign.SEPARATE() is not null;
                }
                else if (clause.occursClause() is { } occ)
                {
                    if (occ.integerLiteral() is { Length: > 0 } lits && int.TryParse(lits[0].GetText(), out int n))
                        occurs = n;
                    if (occ.INDEXED() is not null && occ.dataReferenceList() is { } idxList)
                        foreach (var idx in idxList.dataReference())
                            indexNames.Add(idx.GetText());
                }
            }

        var pic = pictureText is null
            ? null
            : PicInfo.Analyze(pictureText, PicInfo.ParseUsage(usageText), hasSign, signLeading, signSeparate);
        var item = new DataItem
        {
            Level = level,
            CobolName = isFiller ? null : cobolName,
            CsName = csName,
            Pic = pic,
            RawValue = rawValue,
            Occurs = occurs,
            RedefinesTargetName = redefinesTargetName,
        };

        // Register each INDEXED BY index-name as a distinct C# long field (1-based occurrence number, §3.5).
        foreach (var idxName in indexNames)
        {
            item.IndexNames.Add(idxName);
            if (!IndexFields.ContainsKey(idxName))
                IndexFields[idxName] = "_IX_" + IndexFields.Count;
        }
        return item;
    }

    /// <summary>Extract a usage keyword's text (the form after USAGE IS, or the bare keyword).</summary>
    private static string UsageKeyword(Core.UsageClauseContext usage)
    {
        // The keyword is the last child for the bare forms and the usageKeyword child for "USAGE IS <kw>".
        var kw = usage.usageKeyword();
        return kw is not null ? kw.GetText() : usage.GetText().Replace("USAGE", "").Replace("IS", "");
    }

    /// <summary>Extract the first VALUE operand's raw source text (literal or figurative constant). THRU ranges /
    /// 88-levels are later. The emitter (<c>FieldEmitter</c>) interprets the text — including figurative constants
    /// such as ZERO/SPACE — against the item's category and width.</summary>
    private static string? ExtractValue(Core.ValueClauseContext value)
    {
        var item = value.valueItem().FirstOrDefault();
        return item?.GetText();
    }

    // ── REDEFINES / RENAMES resolution + classification (post-build, ISO §13.18.44/45) ───────────────────────

    /// <summary>Resolve each item's REDEFINES target name to its <see cref="DataItem"/>, and each level-66 RENAMES
    /// FROM/THRU operand to its item. A REDEFINES target is an unqualified prior entry in the same scope (SR1/SR6); a
    /// RENAMES range names items within the owning record (SR3). Target resolution does not chase chains — the
    /// classification pass walks <see cref="DataItem.RedefinesTarget"/> transitively to the anchor (SR11).</summary>
    private void ResolveRedefines()
    {
        foreach (var item in AllItems())
            if (item.RedefinesTargetName is { } tname)
            {
                IReadOnlyList<DataItem> scope = item.Parent?.Children ?? Roots;
                item.RedefinesTarget = scope.FirstOrDefault(s =>
                    !ReferenceEquals(s, item) && string.Equals(s.CobolName, tname, StringComparison.OrdinalIgnoreCase));
            }

        foreach (var root in Roots)
            foreach (var ren in root.Renames66)
            {
                var info = ren.Renames!;
                info.From = FindDescendantOrSelf(root, info.FromName);
                info.Thru = info.ThruName is { } t ? FindDescendantOrSelf(root, t) : null;
            }
    }

    /// <summary>Group every redefining entry with the non-redefining anchor it ultimately overlays (SR7/SR11) into a
    /// <see cref="RedefinesClass"/>, mark the anchor canonical and every other member a view, then assign the class a
    /// tier (D &gt; C &gt; B &gt; A) and its class-max width, and propagate view-suppression to each view's
    /// subordinates (SR9 — no VALUE on a subordinate of a redefiner). (COBOLNET_DESIGN §4.2.)</summary>
    private void ClassifyRedefinesClasses()
    {
        var byAnchor = new Dictionary<DataItem, RedefinesClass>();
        foreach (var item in AllItems())
        {
            if (item.RedefinesTarget is null) continue;
            DataItem anchor = item;
            while (anchor.RedefinesTarget is { } t) anchor = t;     // chase the chain to the original (SR11)
            if (!byAnchor.TryGetValue(anchor, out var cls))
            {
                cls = new RedefinesClass { Canonical = anchor };
                cls.Members.Add(anchor);
                anchor.Class = cls;
                byAnchor[anchor] = cls;
            }
            cls.Members.Add(item);
            item.Class = cls;
            item.IsCanonical = false;
        }

        foreach (var cls in byAnchor.Values)
        {
            cls.Tier = ComputeTier(cls, out string? reject);
            cls.RejectReason = reject;
            cls.Width = cls.Members.Max(m => m.ImageWidth);
            foreach (var view in cls.Members)
                if (!view.IsCanonical)
                    foreach (var d in DescendantsOf(view)) { d.IsCanonical = false; d.Class = cls; }
        }
    }

    /// <summary>Assign a redefines class its tier (COBOLNET_DESIGN §4.2 cascade D &gt; C &gt; B &gt; A). Tier C (the
    /// confined byte[] codec for a genuine mixed-USAGE pun) is not yet implemented, so a class that would be Tier C is
    /// loudly rejected in the interim — a conformant diagnostic on a legal-but-unimplemented construct.</summary>
    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(LeavesOf).ToList();

        // Tier C → Rejected (interim): any leaf is COMP/COMP-1/2/3/5 or float — a binary representation no character
        // image can carry. (No pointer/object/strongly-typed items exist in the bound model yet → no Tier-D check.)
        if (leaves.Any(l => l.Pic is { } p && (p.IsFloat || p.Usage is not Usage.Display)))
        {
            reject = $"mixed-USAGE REDEFINES of '{cls.Canonical.CobolName}' (Tier-C byte path) not yet implemented";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        DataItem canon = cls.Canonical;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ImageWidth == canon.ImageWidth);
        if (allAlias) return RedefinesTier.Alias;

        // Tier B — DISPLAY-homogeneous: one string canonical of class-max width, each view an (offset,width) accessor.
        return RedefinesTier.StringCanonical;
    }

    /// <summary>Every item in the WORKING-STORAGE forest, in declaration (pre-order DFS) order.</summary>
    private IEnumerable<DataItem> AllItems()
    {
        static IEnumerable<DataItem> Walk(DataItem d)
        {
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        return Roots.SelectMany(Walk);
    }

    /// <summary>The elementary leaves of an item (itself if elementary), in source order.</summary>
    private static IEnumerable<DataItem> LeavesOf(DataItem d)
    {
        if (d.IsElementary) { yield return d; yield break; }
        foreach (var c in d.Children)
            foreach (var l in LeavesOf(c)) yield return l;
    }

    /// <summary>Every descendant of an item (children, recursively).</summary>
    private static IEnumerable<DataItem> DescendantsOf(DataItem d)
    {
        foreach (var c in d.Children)
        {
            yield return c;
            foreach (var x in DescendantsOf(c)) yield return x;
        }
    }

    /// <summary>Find an item by COBOL name within a record subtree (the item itself or any descendant).</summary>
    private static DataItem? FindDescendantOrSelf(DataItem root, string name)
    {
        if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase)) return root;
        foreach (var c in root.Children)
            if (FindDescendantOrSelf(c, name) is { } f) return f;
        return null;
    }
}
