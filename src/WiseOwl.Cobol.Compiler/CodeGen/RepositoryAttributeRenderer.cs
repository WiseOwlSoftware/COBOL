// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// ⛔ THE ONE PLACE REPOSITORY ATTRIBUTE SYNTAX IS WRITTEN (DESIGN-external-repository §3.9, §6.2, §12; kb/Work PB2100): the
/// assembly-level <see cref="CobolRepositoryAttribute"/> and a unit's <see cref="CobolUnitAttribute"/>, as C# attribute text
/// the Roslyn backend compiles (a direct-CIL backend would write the same facts through its own metadata builder, §17.4's
/// backend-independence fixture). <c>RuntimeApi.ModuleRecordAttribute</c>, which wrote the assembly record when it carried a
/// registrar and nothing else, moved here. The reader of what this writes is the runtime's <see cref="RepositoryIndex"/>:
/// <see cref="RepositoryIndex.EncodeUnit"/> is the one encoding of an index row, so the writer calls it rather than
/// restating it.
/// <para>Every type is <c>global::</c>-qualified like every foreign name in emitted code (§4.5), and a named argument is
/// written only when it has a value (§6.2 "Encoding" rule 2: nullability is presence), so the record of a build with
/// nothing to say about a fact does not mention it.</para>
/// </summary>
internal static class RepositoryAttributeRenderer
{
    /// <summary>The rows of the assembly's <c>Units</c> index: one per host entry <paramref name="entryUnits"/> publishes, in
    /// the order the entries are emitted (§6.5). The <c>[CobolUnit]</c> of each entry is written from the same list by
    /// <see cref="UnitRecord"/>, so the index and the records cannot name different units.</summary>
    internal static IReadOnlyList<RepositoryUnitRow> IndexOf(IEnumerable<BoundUnit> entryUnits) =>
        [.. entryUnits.Select(u => new RepositoryUnitRow(KindOf(u), u.ExternalizedName, u.Name))];

    /// <summary>What a program, function or prototype unit is, as the repository records it (§5.1).</summary>
    internal static RepositoryUnitKind KindOf(BoundUnit unit) => (unit.IsFunction, unit.IsPrototype) switch
    {
        (false, false) => RepositoryUnitKind.Program,
        (true, false) => RepositoryUnitKind.Function,
        (false, true) => RepositoryUnitKind.ProgramPrototype,
        (true, true) => RepositoryUnitKind.FunctionPrototype,
    };

    /// <summary>The module's assembly-level record, <c>[assembly: CobolRepository(schema, runtimeVersion, callAbi, …)]</c>
    /// (§6.2, §7.1): the versions it was compiled against (so a run unit can refuse a module whose runtime major or call ABI
    /// differs, <see cref="RuntimeAbi.Skew"/>), the compiler and edition that wrote it (diagnostics only), whether it carries
    /// signatures, its registrar and its unit index. Written before the module's namespace.</summary>
    /// <param name="registrarClrName">The module's registrar type, as metadata names it.</param>
    /// <param name="units">The index rows (<see cref="IndexOf"/>).</param>
    /// <param name="edition">The ISO edition the group was bound under (<c>EditionContext.DialectLevel</c>).</param>
    /// <param name="hasSignatures">Whether the module carries the signature attributes (<c>--repository-update on</c>).</param>
    internal static string ModuleRecord(string registrarClrName, IReadOnlyList<RepositoryUnitRow> units, int edition, bool hasSignatures)
    {
        var named = new List<string>
        {
            Named(nameof(CobolRepositoryAttribute.Registrar), CsLiteral(registrarClrName)),
            Named(nameof(CobolRepositoryAttribute.CompilerVersion), CsLiteral(CompilerVersion)),
            Named(nameof(CobolRepositoryAttribute.Edition), edition.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        if (hasSignatures) named.Add(Named(nameof(CobolRepositoryAttribute.HasSignatures), "true"));
        if (units.Count > 0)
            named.Add(Named(nameof(CobolRepositoryAttribute.Units),
                "new string[] { " + string.Join(", ", units.Select(u => CsLiteral(RepositoryIndex.EncodeUnit(u.Kind, u.ExternalizedName, u.Word)))) + " }"));
        return $"[assembly: global::{typeof(CobolRepositoryAttribute).FullName}("
            + $"{CobolRepositoryAttribute.CurrentSchemaVersion}, {CsLiteral(RuntimeAbi.Version.ToString())}, {RuntimeAbi.CallAbi}, "
            + string.Join(", ", named) + ")]";
    }

    /// <summary>A unit's <c>[CobolUnit(kind, name, externalizedName)]</c> (§6.2), written on its host-entry class.</summary>
    internal static string UnitRecord(BoundUnit unit) =>
        $"[global::{typeof(CobolUnitAttribute).FullName}(global::{typeof(RepositoryUnitKind).FullName}.{KindOf(unit)}, "
        + $"{CsLiteral(unit.Name)}, {CsLiteral(unit.ExternalizedName)})]";

    /// <summary>The version of this compiler, as a record states it. The compiler and the runtime ship from one package
    /// version (<c>Directory.Build.props</c>), so it is the compiler assembly's own, never the runtime's.</summary>
    internal static string CompilerVersion { get; } = typeof(RepositoryAttributeRenderer).Assembly.GetName().Version?.ToString() ?? "";

    private static string Named(string name, string value) => $"{name} = {value}";
}
