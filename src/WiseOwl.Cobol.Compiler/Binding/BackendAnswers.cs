// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE ANSWERS THE BACKEND READS OF THE BINDER'S RULE FAMILIES, WRITTEN ONCE OVER THE FINISHED BIND (kb/Work PB2403;
/// DESIGN-architecture-review §8; DESIGN-backend-abstraction §1.1). The activation descriptions (<see cref="ActivationDescriptions"/>,
/// whose item half reads the binder's MOVE, Table 16 and prototype-signature rules), the §2023:9.3.8.2.3 pair-mode relation
/// (<see cref="OoConformance.SignatureDifference"/>), the class table's RAISING resolution and an external file connector's
/// §2023:12.4.5.3 GR1 identity (<see cref="FileEntryIdentity"/>) are binder questions. A backend renders their answers and never
/// asks them, so a second backend (direct CIL) needs neither the class table nor the binder's rule families: each answer is a
/// value on the node the backend already reads.
/// <para>It runs at the end of <c>BinderDriver.Bind</c>, after the group tail, beside the repository records
/// (<c>RepositoryPublication</c>), so every class fact a description carries (§2023:9.3.6 match rule 6's class lineage) and every
/// record layout an entry identity measures is final. Each answer is written where the backend asked for it, and only for
/// the nodes it asked about: a unit's formal and RETURNING descriptions for a unit that registers them (one that checks
/// EC-PROGRAM-ARG-MISMATCH) or publishes a host entry, and a method's for a method with a universal dispatch case.</para>
/// </summary>
internal static class BackendAnswers
{
    /// <summary>Write every answer into the bound group: <paramref name="units"/>, the class units <paramref name="classes"/>,
    /// the files each of their source elements declares, resolved through the group's <paramref name="table"/>;
    /// <paramref name="entryUnits"/> are the units that publish a host entry (<see cref="BoundCompilation.EntryUnits"/>).</summary>
    public static void Write(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes, OoClassTable table, IReadOnlyList<BoundUnit> entryUnits)
    {
        var entries = new HashSet<BoundUnit>(entryUnits, ReferenceEqualityComparer.Instance);
        foreach (var unit in units)
        {
            unit.RaisingObjects = Resolve(unit.Bound.Raising ?? [], table);
            bool entry = entries.Contains(unit);
            // The unit's registration states its descriptions only when it checks EC-PROGRAM-ARG-MISMATCH itself (§2023:14.9.4.4
            // GR3 d)'s activated half; kb/Work PB165); a host entry's arguments always state them (DESIGN-external-repository §4.3).
            if (entry || !unit.IsPrototype && unit.Data.UnitChecking.ArgMismatchChecking)
            {
                unit.FormalDescriptions = [.. unit.Data.Linkage.Formals.Select(f => ActivationDescriptions.Of(f.Item, table))];
                unit.ReturningDescription = unit.Data.Linkage.Returning is { } returning ? ActivationDescriptions.Of(returning, table) : null;
            }
            if (entry) unit.FormalsOfOneDescription = OneDescriptionPairs(unit.Data.Linkage.Formals);
        }
        foreach (var cls in classes)
            foreach (var method in cls.Symbol.Methods.Concat(cls.Symbol.FactoryMethods))
            {
                if (method.Binding is not { } binding) continue;
                binding.RaisingObjects = Resolve(method.Raising, table);
                // A universal dispatch case exists for a method that overrides nothing and has no BY VALUE formal (§2023:9.3.6 match
                // rule 3 a): a universal invocation passes every argument BY REFERENCE, §2023:14.9.23.3 SR6).
                if (method.OverrideOf is not null || binding.Formals.Any(f => f.ByValue)) continue;
                binding.FormalDescriptions = [.. binding.Formals.Select(f => ActivationDescriptions.OfFormal(f, table))];
                binding.ReturningDescription = binding.Returning is { } returning ? ActivationDescriptions.Of(returning, table) : null;
            }
        foreach (var files in units.Select(static u => u.Data.Files).Concat(classes.SelectMany(static c => new[] { c.Data.Files, c.FactoryData.Files })))
            foreach (var file in files.Declared)
                if (file.IsExternal) file.SelectFingerprint = FileEntryIdentity.Of(file);
    }

    /// <summary>The classes and interfaces of a RAISING phrase, each resolved by its name in the group's class table: an entry
    /// the table does not resolve (and an exception-name entry) has no <see cref="RaisingObject"/>.</summary>
    private static List<RaisingObject> Resolve(IEnumerable<RaisingTarget> raising, OoClassTable table)
    {
        var resolved = new List<RaisingObject>();
        foreach (var target in raising)
        {
            if (target.Kind is RaisingTargetKind.ObjectClass && table.Find(target.Name) is { } cls)
                resolved.Add(new RaisingObject(cls, target.Factory, null));
            else if (target.Kind is RaisingTargetKind.Interface && table.FindInterface(target.Name) is { } iface)
                resolved.Add(new RaisingObject(null, false, iface));
        }
        return resolved;
    }

    /// <summary>The pairs of BY REFERENCE formals (earlier position, later position) whose descriptions are the same in both
    /// directions of the ONE pair-mode relation (<see cref="OoConformance.SignatureDifference"/>): a host entry gives one variable
    /// passed to both one storage (DESIGN-external-repository §4.3).</summary>
    private static HashSet<(int Earlier, int Later)> OneDescriptionPairs(IReadOnlyList<LinkageFormal> formals)
    {
        var pairs = new HashSet<(int, int)>();
        for (int later = 0; later < formals.Count; later++)
        {
            if (formals[later].ByValue) continue;
            for (int earlier = 0; earlier < later; earlier++)
                if (!formals[earlier].ByValue
                    && OoConformance.SignatureDifference(formals[earlier].Item, formals[later].Item, "formal", "other formal") is null
                    && OoConformance.SignatureDifference(formals[later].Item, formals[earlier].Item, "formal", "other formal") is null)
                    pairs.Add((earlier, later));
        }
        return pairs;
    }
}
