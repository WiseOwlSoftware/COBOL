// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ <c>--repository-check</c>: ISO §8.13's "mechanism that allows the user to specify whether to flag differences in
/// prototypes and definitions in the compilation group from the information in the external repository"
/// (DESIGN-external-repository §9.2; kb/Work PB2103). Every outermost program or function — definition or prototype — of
/// the group is compared by the ONE signature comparison (<see cref="PrototypeSignatures.FirstDifference"/>, over the
/// decoded record) against
/// <list type="number">
/// <item>(a) the PREVIOUS BUILD OF THIS OUTPUT — the file callers were compiled against, read once before the new build
///   replaces it (§7.5); one of another schema, runtime major or call ABI is REPO-12 and is not compared; and</item>
/// <item>(b) every entry the resolver already opened that publishes the same externalized name as a program or function
///   (a definition that differs from a published prototype, a prototype that differs from a published definition).</item>
/// </list>
/// A difference is REPO-9, a WARNING: a signature change is a normal event, this compilation is the update, and §10.6.2
/// SR2 / SR3's "shall" is scoped to one compilation group. The cost is one prefetched read of the previous output when it
/// exists (§16); the entries of (b) are already open.
/// </summary>
internal static class RepositoryCheck
{
    /// <summary>Compare <paramref name="units"/> against the information the external repository holds for them.</summary>
    public static void Run(IReadOnlyList<BoundUnit> units, BindSession session)
    {
        var space = session.SearchSpace;
        if (!space.Check) return;
        var outermost = units.Where(u => u.Parent is null).ToList();
        if (outermost.Count == 0) return;
        var records = new List<(string Source, RepositoryRecord Record, bool Previous)>();
        // The previous build is the compilation's OWN output path, which the compilation then replaces. A finding against it
        // marks the input record as a repository read, so a result that reports one is never replayed; a result that reports
        // none is replayed only over no previous build or over its own output byte for byte (CompiledProgramCache.TryHit),
        // where a cold compile reports none either.
        if (space.OutputPath is { } output && File.Exists(output))   // not a compilation input: the compilation's own output, see above
            records.AddRange(PreviousBuild(Path.GetFullPath(output), session).Select(r => ($"the previous build '{Path.GetFullPath(output)}'", r, true)));
        records.AddRange(session.Repository.External.OpenedRecords().Select(e => ($"the repository entry '{e.Path}'", e.Record, false)));
        if (records.Count == 0) return;

        // The records are decoded into a partition of their own: what they describe is compared, never admitted into the
        // group's types (the identities are canonical, so a decoded pointer and a group pointer of one signature compare
        // equal across the two instances, §6.4).
        var classes = new PrototypeSignatureClasses();
        foreach (var unit in outermost)
            foreach (var (source, record, previous) in records)
            {
                if (IsFunction(record.Kind) != unit.IsFunction || !ExternalizedNames.Same(record.ExternalizedName, unit.ExternalizedName))
                    continue;
                DecodedRecord decoded;
                try { decoded = RepositoryCodec.Decode(record, classes, RepositoryCodec.Names(record, session.OoClasses, [], import: false)); }
                catch (RepositoryRecordException) { continue; }   // the resolver's REPO-5 names an entry it uses; this one is only compared
                if (PrototypeSignatures.FirstDifference(new CalleeSignature(unit.Data.LinkageFormals, unit.Data.LinkageReturning),
                        decoded.Signature) is not { } difference)
                    continue;
                string kind = unit.IsFunction ? "function" : "program";
                if (previous) space.Inputs.RecordRepositoryRead();
                using var _ = session.Edition.At(BinderDriver.NameCtx(unit));
                session.Edition.Warning(DiagnosticCatalog.RepositorySignatureDiffers,
                    $"the {kind}{(unit.IsPrototype ? " prototype" : "")} {unit.Name} AS \"{unit.ExternalizedName}\" differs from "
                    + $"{(record.Kind is RepositoryUnitKind.ProgramPrototype or RepositoryUnitKind.FunctionPrototype ? "the prototype" : "the definition")} "
                    + $"{source} records, first in {difference}: callers compiled against that entry are checked at run time "
                    + "by ISO §14.9.4.4 GR3 d) when checking is enabled in both, and adopt the arguments positionally otherwise; "
                    + "recompile them (ISO §8.13; --repository-check off does not compare)");
            }
    }

    private static bool IsFunction(RepositoryUnitKind kind) => kind is RepositoryUnitKind.Function or RepositoryUnitKind.FunctionPrototype;

    /// <summary>The previous build's program and function records, or none: a file that is no WiseOwl COBOL module or carries
    /// no signatures has nothing to compare; one of another schema, runtime major or call ABI is REPO-12.</summary>
    private static IReadOnlyList<RepositoryRecord> PreviousBuild(string output, BindSession session)
    {
        RepositoryIndexOutcome opened;
        try { opened = RepositoryIndex.Open(output); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return []; }
        if (opened.Index is not { } index) return [];
        using (index)
        {
            string? skew = index.SchemaVersion != CobolRepositoryAttribute.CurrentSchemaVersion
                ? $"it carries repository schema {index.SchemaVersion} and this compiler reads {CobolRepositoryAttribute.CurrentSchemaVersion}"
                : RuntimeAbi.Skew(index.RuntimeVersion, index.CallAbi);
            if (skew is not null)
            {
                session.SearchSpace.Inputs.RecordRepositoryRead();
                session.Edition.Warning(DiagnosticCatalog.RepositoryPreviousBuildSkew,
                    $"the previous build '{output}' is not compared with this compilation: {skew} (ISO §8.13; DESIGN-external-repository §9.2)");
                return [];
            }
            var outcome = RepositoryReader.Read(index);
            return outcome.Failure is RepositoryReadFailure.None ? [.. outcome.Records.Where(r => r.Class is null)] : [];
        }
    }

}
