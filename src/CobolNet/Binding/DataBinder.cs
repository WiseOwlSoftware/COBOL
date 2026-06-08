// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Builds the bound DATA DIVISION model (a forest of <see cref="DataItem"/> trees, one per 01/77 item) from the
/// parse tree, and indexes every named item for reference resolution. Pure syntactic/semantic analysis — no byte
/// layout. (Slice scope: WORKING-STORAGE; FILE/LINKAGE/LOCAL-STORAGE follow in later tasks.)
/// </summary>
public sealed class DataBinder
{
    private int _fillerCounter;

    /// <summary>The top-level (01/77) items of WORKING-STORAGE, in source order.</summary>
    public List<DataItem> Roots { get; } = [];

    /// <summary>Every named elementary/group item, keyed by COBOL name (case-insensitive) for reference resolution.</summary>
    public Dictionary<string, DataItem> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bind the WORKING-STORAGE section of a program unit (if present).</summary>
    public void Bind(Core.ProgramUnitContext program)
    {
        var ws = program.dataDivision()?.workingStorageSection();
        if (ws is null) return;

        // A level-number stack builds the tree: an entry attaches under the nearest open item whose level is lower.
        var stack = new Stack<DataItem>();
        foreach (var entry in ws.dataDescriptionEntry())
        {
            if (BindEntry(entry) is not { } item) continue;

            while (stack.Count > 0 && stack.Peek().Level >= item.Level)
                stack.Pop();

            if (stack.Count == 0)
                Roots.Add(item);
            else
            {
                item.Parent = stack.Peek();
                stack.Peek().Children.Add(item);
            }
            stack.Push(item);

            if (item.CobolName is { } name)
                ByName[name] = item;
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

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
            {
                if (clause.pictureClause()?.PIC_STRING() is { } picTok)
                    pictureText = picTok.GetText();
                else if (clause.usageClause() is { } usage)
                    usageText = UsageKeyword(usage);
                else if (clause.valueClause() is { } value)
                    rawValue = ExtractValue(value);
                else if (clause.occursClause()?.integerLiteral() is { Length: > 0 } occ)
                    occurs = int.TryParse(occ[0].GetText(), out int n) ? n : null;
            }

        var pic = pictureText is null ? null : PicInfo.Analyze(pictureText, PicInfo.ParseUsage(usageText));
        return new DataItem
        {
            Level = level,
            CobolName = isFiller ? null : cobolName,
            CsName = csName,
            Pic = pic,
            RawValue = rawValue,
            Occurs = occurs,
        };
    }

    /// <summary>Extract a usage keyword's text (the form after USAGE IS, or the bare keyword).</summary>
    private static string UsageKeyword(Core.UsageClauseContext usage)
    {
        // The keyword is the last child for the bare forms and the usageKeyword child for "USAGE IS <kw>".
        var kw = usage.usageKeyword();
        return kw is not null ? kw.GetText() : usage.GetText().Replace("USAGE", "").Replace("IS", "");
    }

    /// <summary>Extract the first VALUE operand's raw source text (literal). THRU ranges / 88-levels are later.</summary>
    private static string? ExtractValue(Core.ValueClauseContext value)
    {
        var item = value.valueItem().FirstOrDefault();
        // Descend to the literal text; for the common single-literal VALUE this is the operand's source text.
        return item?.GetText();
    }
}
