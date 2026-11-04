// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Repository;

/// <summary>
/// ⛔ WHAT A COMPILATION PUBLISHES TO THE EXTERNAL REPOSITORY, DECIDED AT BIND TIME (ISO §8.13; DESIGN-external-repository §7.2,
/// §9.1; kb/Work PB4319). Which units publish a host entry (<see cref="EntryUnitsOf"/>), and, when the compilation updates the
/// repository, each published unit's record, written by the one encoder (<see cref="RepositoryCodec"/>) over the bound data
/// once <c>BinderDriver.Bind</c> has finished everything else. The bound compilation carries the records as model data
/// (<see cref="BoundUnit.Record"/>, <see cref="OoClassUnit.Record"/>, <see cref="BoundCompilation.InterfaceRecords"/>), so a
/// backend transcribes a value and never runs the encoder (DESIGN-codegen-backend §2.2; DESIGN-backend-abstraction §1.3).
/// </summary>
internal static class RepositoryPublication
{
    /// <summary>The units that publish a host entry: every outermost unit, except a prototype whose definition the same
    /// group compiles (that definition's entry is the one class for the name — DESIGN-external-repository §7.2). The pairing
    /// is by the Annex C fold (<see cref="ExternalizedNames.Same"/>, the one mapping rule, ISO §8.3.2.2 2)): a prototype
    /// <c>psub</c> beside a definition <c>PSUB</c> is one externalized name (kb/Work PB3872). Nothing else is withheld: every
    /// outermost unit publishes its entry, and so its repository record (§7.2); a group item the published-record emitter
    /// refuses is an internal compiler error in the backend, never a missing entry (the binder admits no such shape since
    /// kb/Work PB3273: COBOL0853, COBOL0854).</summary>
    public static IReadOnlyList<BoundUnit> EntryUnitsOf(IReadOnlyList<BoundUnit> units) =>
        [.. units.Where(u => u.Parent is null && !(u.IsPrototype && units.Any(d => d is { Parent: null, IsPrototype: false }
            && d.IsFunction == u.IsFunction && ExternalizedNames.Same(d.ExternalizedName, u.ExternalizedName))))];

    /// <summary>Write the record of every unit the module publishes: each entry unit's (<see cref="BoundUnit.Record"/>), each
    /// class's and each interface's the module emits (<see cref="ExpansionPlacements.EmittedHere"/>), an expansion's under its
    /// placement identity (§5.2). Returns the interfaces' records, which the bound compilation carries beside its interface
    /// forests. Run only under <c>--repository-update on</c>; with it off no unit carries a record.</summary>
    public static IReadOnlyDictionary<OoInterfaceSymbol, RepositoryRecord> Publish(IReadOnlyList<BoundUnit> entryUnits,
        IReadOnlyList<OoClassUnit> classes, OoClassTable table, ExpansionPlacements expansions,
        ElementBinders binders)
    {
        foreach (var unit in entryUnits) unit.Record = RepositoryCodec.Of(unit, binders.Of(unit).Data);
        foreach (var cls in classes.Where(c => expansions.EmittedHere(c.Symbol.ClrName)))
            cls.Record = RepositoryCodec.Of(cls, binders.ObjectOf(cls).Data, expansions.Of(cls.Symbol.ClrName)?.Identity);
        return table.Interfaces.Where(i => expansions.EmittedHere(i.ClrName))
            .ToDictionary(i => i, i => RepositoryCodec.Of(i, binders.Interfaces[i], expansions.Of(i.ClrName)?.Identity));
    }
}
