// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>What <see cref="SymbolTable.Identify(Antlr4.Runtime.ParserRuleContext, Symbols.NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
/// answers for a named OCCURS DYNAMIC CAPACITY register (§2023:13.18.38.3 SR30): the dynamic tables whose register carries the
/// name and that the reference's scope reaches, nearest declaring source element first (§2023:8.4.6.2.1 3)). A TYPE or SAME AS copy
/// of one written entry reproduces its register (kb/Work PB2988), so the reference's qualifiers tell the copies apart; its funnel
/// (<c>ReferenceResolver.CapacityRegisterFor</c>) applies them and words the ambiguity, so it is no identification until the funnel
/// has one table, and listing step 7b records an occurrence only then (as for <see cref="ReportCandidates{T}"/>).</summary>
internal sealed record CapacityRegisterCandidates(IReadOnlyList<DataItem> Tables);

public sealed partial class SymbolTable
{
    /// <summary>The named OCCURS DYNAMIC <c>CAPACITY IN data-name-3</c> registers this element sees, by name: a list of the owning
    /// dynamic tables per name, nearest declaring source element first (kb/Work PB1674). The register is implicitly defined at the
    /// OCCURS entry (§2023:13.18.38.3 SR30) and is no data item of the forest; it is reached only through this table. The register of
    /// a dynamic table in a GLOBAL record is subordinate to a global name, so it is a global name too (§2023:8.4.6.2.2), and a
    /// contained program inherits it with the rest of the record (<see cref="InheritGlobalItem"/>). This element's own registers are
    /// the tables at depth 0. A METHOD's own registers are its <see cref="MethodNames.CapacityRegisters"/> (kb/Work PB3165).</summary>
    private readonly Dictionary<CobolName, List<DataItem>> _capacityRegisters = [];

    /// <summary>⛔ THE ONE INSERT OF A CAPACITY REGISTER NAME: this element's own register (from <c>DynamicResolvePass</c>) and each
    /// container's GLOBAL one (from <see cref="InheritGlobalItem"/>, nearest container first), into <paramref name="method"/>'s own
    /// table when the dynamic table is a method's. ⛔ THE LIST IS KEPT IN NEAREST-DECLARING-ELEMENT ORDER BY THE INSERT, NOT BY THE
    /// ORDER OF THE CALLS (kb/Work PB1674; PB3766 review R2-1): the containers' names are inherited before this element's registers
    /// are minted, so a register goes after the last one at its own depth or nearer, the order <see cref="CapacityRegisterNamed"/>'s
    /// funnel reads (its first table is the nearest).</summary>
    internal void DeclareCapacityRegister(OoMethodDataScope? method, string name, DataItem table)
    {
        var registers = method is null ? _capacityRegisters : MethodNamesOf(method).CapacityRegisters;
        if (!registers.TryGetValue(Key(name), out var tables)) registers[Key(name)] = tables = [];
        int depth = _data.State.Forest.DeclaringDepth(table);
        int at = tables.Count;
        while (at > 0 && _data.State.Forest.DeclaringDepth(tables[at - 1]) > depth) at--;
        tables.Insert(at, table);
    }

    /// <summary>§2023:13.18.38.3 SR30, "Data-name-3 shall not be defined elsewhere in the source element": whether a register named
    /// <paramref name="name"/> on <paramref name="table"/> duplicates a data-name of its source element (this element's own, or the
    /// method's when the table is a method's, kb/Work PB3165) or the register of another written entry. A container's GLOBAL name
    /// is defined in ANOTHER source element, which this element's declaration hides (§2023:8.4.6.2.1 3); kb/Work PB3766 review
    /// R2-1), and a TYPE or SAME AS copy of the same written entry reproduces the one register (kb/Work PB2988). Asked while the
    /// registers are still being minted, so it reads them with no watermark gate.</summary>
    internal bool DuplicatesCapacityRegister(OoMethodDataScope? method, string name, DataItem table)
    {
        var registers = method is null ? _capacityRegisters : _methodNames.GetValueOrDefault(method)?.CapacityRegisters;
        return (method is null ? _data.State.Forest.DeclaresOwnDataName(name) : method.ByName.ContainsKey(name))
            || (registers is not null && registers.TryGetValue(Key(name), out var sameName)
                && !sameName.TrueForAll(t => _data.State.Forest.DeclaringDepth(t) != 0
                                             || ReferenceEquals(t.WrittenDefinition, table.WrittenDefinition)));
    }

    /// <summary>The dynamic tables this element and its methods declare a named CAPACITY register for (this element's own, then each
    /// method's): the registers the record emitter declares.</summary>
    internal IEnumerable<DataItem> DeclaredCapacityRegisterTables =>
        _capacityRegisters.Values.SelectMany(static t => t).Where(t => _data.State.Forest.DeclaringDepth(t) == 0)
            .Concat(_data.Oo.BoundMethods.SelectMany(m => _methodNames.GetValueOrDefault(m.DataScope)?.CapacityRegisters.Values
                ?? Enumerable.Empty<List<DataItem>>()).SelectMany(static t => t));

    /// <summary>⛔ THE ONE CAPACITY-REGISTER-NAME LOOKUP (kb/Work PB3165): the dynamic tables whose named OCCURS DYNAMIC
    /// <c>CAPACITY IN</c> register the written word reaches in <paramref name="scope"/>, or null when it reaches none. The register
    /// is a data-name of its entry's source element (§2023:13.18.38.3 SR30), so it has the data-name's visibility (§2023:11.7.4
    /// GR5): a method sees its OWN registers first, and they REPLACE the object's; a method-local name of the spelling in any
    /// other class (<see cref="DataBinder.MethodDeclares"/>) hides an object-level register (the object's declaration is
    /// "inaccessible to this method"); a sibling method's registers are not in the scope at all. Else this element's, nearest
    /// declaring element first, read only once <c>DynamicResolvePass</c> has minted every register (the
    /// <see cref="Passes.PassPhase.OccursResolved"/> watermark, the P6 Step-6 gate): a read before it would see a null register.</summary>
    private CapacityRegisterCandidates? CapacityRegisterNamed(CobolName key, Scope scope)
    {
        if (scope.Method is { } m)
        {
            if (_methodNames.TryGetValue(m, out var own) && own.CapacityRegisters.TryGetValue(key, out var tables)) return new(tables);
            if (_data.MethodDeclares(m, key.Word)) return null;   // any other method-local name of the spelling wins (GR5 covers every name class)
        }
        _data.State.Require(Passes.PassPhase.OccursResolved, "CapacityRegisters");
        return _capacityRegisters.TryGetValue(key, out var registers) ? new(registers) : null;
    }

    /// <summary>Whether a CAPACITY register of <paramref name="name"/>, minted so far, is declared in a source element nearer than
    /// <paramref name="depth"/> (<see cref="Declarations.DeclaredNearerThan"/>, which runs while they are still being minted).</summary>
    private bool CapacityRegisterDeclaredNearerThan(string name, int depth) =>
        _capacityRegisters.TryGetValue(Key(name), out var tables) && tables.Any(t => _data.State.Forest.DeclaringDepth(t) < depth);

    /// <summary>Makes ONE container GLOBAL item (no recursion: <c>DataBinder.InheritGlobalSubtree</c> walks the subtree) visible here,
    /// <paramref name="depth"/> containment levels out (ISO §2023:13.18.27.4 GR2, "may reference that name without describing it
    /// again"): its data-name, the condition-names associated with it and its index-names (<see cref="ForestSlice.Inherit"/>), and
    /// its CAPACITY register. A register the container refused (SR30's COBOL1523) failed that compile and has none to inherit.</summary>
    internal void InheritGlobalItem(DataItem item, int depth)
    {
        _data.State.Forest.Inherit(item, depth);
        if (item.OccursSpec is { CapacityName: { } cap, CapacityRegister: not null }) DeclareCapacityRegister(null, cap, item);
    }
}
