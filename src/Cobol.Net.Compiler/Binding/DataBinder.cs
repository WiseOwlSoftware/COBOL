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
            // A level-88 entry is a condition-name on the immediately superior item — not a node in the tree.
            if (int.TryParse(entry.levelNumber().GetText(), out int lvl) && lvl == 88)
            {
                if (stack.Count > 0) BindCondition(entry, stack.Peek());
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

            if (item.CobolName is { } name)
            {
                if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
                list.Add(item);
            }
        }
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

        string? pictureText = null, usageText = null, rawValue = null;
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
}
