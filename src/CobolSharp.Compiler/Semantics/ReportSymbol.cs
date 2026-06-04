// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;

namespace CobolSharp.Compiler.Semantics;

/// <summary>The TYPE of a report group (ISO §13.18.57).</summary>
public enum ReportGroupKind
{
    None,
    ReportHeading,
    PageHeading,
    ControlHeading,
    Detail,
    ControlFooting,
    PageFooting,
    ReportFooting,
}

/// <summary>
/// An RD (report description) from the REPORT SECTION (ISO §13.14). Holds the page geometry, the control
/// hierarchy, and the tree of report group entries, plus the host file named in the FD's REPORT IS clause.
/// Analogous to <see cref="FileSymbol"/>. Declared in the program GlobalScope so a report-name resolves.
/// </summary>
public sealed class ReportSymbol : Symbol
{
    /// <summary>The FD whose REPORT IS clause names this report — the file its lines are written to.</summary>
    public string? HostFileName { get; set; }

    /// <summary>PAGE LIMIT IS n LINES (§13.18.37): physical page height. 0 = unbounded.</summary>
    public int PageLimitLines { get; set; }

    /// <summary>HEADING integer (§13.18.37): first line a heading group may occupy. Defaults to 1.</summary>
    public int HeadingLine { get; set; } = 1;

    /// <summary>FIRST DETAIL integer: first line a body group may occupy. Defaults to 1.</summary>
    public int FirstDetailLine { get; set; } = 1;

    /// <summary>LAST DETAIL integer: last line a body group may occupy. Defaults to PAGE LIMIT.</summary>
    public int LastDetailLine { get; set; }

    /// <summary>FOOTING integer: last line a CONTROL/PAGE FOOTING may occupy. Defaults to PAGE LIMIT.</summary>
    public int FootingLine { get; set; }

    /// <summary>RD ... IS GLOBAL (§13.18.23).</summary>
    public bool IsGlobal { get; set; }

    /// <summary>CODE literal prefix (§13.18.12), or null.</summary>
    public string? CodeValue { get; set; }

    /// <summary>CONTROL(S) hierarchy, major→minor; "FINAL" for the FINAL control (§13.18.16).</summary>
    public List<string> ControlFields { get; } = [];

    /// <summary>The 01-level report group entries under this RD, in source order.</summary>
    public List<ReportGroupSymbol> TopGroups { get; } = [];

    /// <summary>Width (bytes) of the composed report line buffer — the max column-end over all fields,
    /// or a printer default. Used to size the runtime line buffer.</summary>
    public int LineWidth { get; set; } = 132;

    public ReportSymbol(string name, int line)
        : base(name, SymbolKind.Report, line) { }

    /// <summary>All report group entries in this report (depth-first), 01-levels and subordinates.</summary>
    public IEnumerable<ReportGroupSymbol> AllGroups()
    {
        foreach (var top in TopGroups)
            foreach (var g in top.SelfAndDescendants())
                yield return g;
    }

    /// <summary>Find a report group entry by name (used to resolve a GENERATE report-group-name).</summary>
    public ReportGroupSymbol? FindGroup(string name)
    {
        foreach (var g in AllGroups())
            if (string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase))
                return g;
        return null;
    }
}

/// <summary>
/// A report group description entry (ISO §13.15): a 01-level group (carrying a TYPE) or a subordinate
/// item (carrying LINE/COLUMN/SOURCE/SUM/PIC). The level-number tree mirrors the data-description tree.
/// </summary>
public sealed class ReportGroupSymbol : Symbol
{
    /// <summary>Level number (01 for a group, 02-49 for a subordinate item).</summary>
    public int Level { get; }

    /// <summary>TYPE IS … (only on the entry that carries it, usually the 01).</summary>
    public ReportGroupKind GroupKind { get; set; } = ReportGroupKind.None;

    /// <summary>For TYPE CONTROL HEADING/FOOTING: the control data-name, or "FINAL".</summary>
    public string? ControlField { get; set; }

    /// <summary>LINE NUMBER clause present on this entry.</summary>
    public bool HasLine { get; set; }
    /// <summary>The LINE value (the integer; for relative it is the PLUS amount).</summary>
    public int LineValue { get; set; }
    /// <summary>LINE NUMBER IS PLUS n (relative to the current line).</summary>
    public bool LineRelative { get; set; }
    /// <summary>LINE NUMBER IS NEXT PAGE.</summary>
    public bool LineNextPage { get; set; }

    /// <summary>COLUMN NUMBER clause present.</summary>
    public bool HasColumn { get; set; }
    /// <summary>COLUMN value (1-based).</summary>
    public int ColumnValue { get; set; }

    /// <summary>SOURCE IS data-name (§13.18.53), or null.</summary>
    public string? SourceName { get; set; }

    /// <summary>SUM data-name… (§13.18.54), or empty.</summary>
    public List<string> SumFields { get; } = [];

    /// <summary>PIC string (§13.18.40) if this entry is an elementary printable field, else null.</summary>
    public string? PicString { get; set; }

    /// <summary>Receiving width (bytes) of this field, computed from PIC. 0 if not an elementary field.</summary>
    public int FieldWidth { get; set; }

    /// <summary>VALUE literal (§13.18.63) for a constant printable field, or null.</summary>
    public string? ValueLiteral { get; set; }

    /// <summary>The RD this entry belongs to.</summary>
    public ReportSymbol? OwningReport { get; set; }

    /// <summary>Parent group entry in the level tree (null for a 01-level group).</summary>
    public ReportGroupSymbol? Parent { get; set; }

    /// <summary>Subordinate report group entries.</summary>
    public List<ReportGroupSymbol> Children { get; } = [];

    public ReportGroupSymbol(string name, int level, int line)
        : base(name, SymbolKind.ReportGroup, line) => Level = level;

    /// <summary>This entry and all its descendants, depth-first in source order.</summary>
    public IEnumerable<ReportGroupSymbol> SelfAndDescendants()
    {
        yield return this;
        foreach (var c in Children)
            foreach (var d in c.SelfAndDescendants())
                yield return d;
    }
}
