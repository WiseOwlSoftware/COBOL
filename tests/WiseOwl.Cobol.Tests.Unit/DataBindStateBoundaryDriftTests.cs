// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH SLICE OF THE BINDER'S STATE HAS ONE PRODUCER, AND EACH PROCEDURE-BIND SHAPE CHANGES ONLY THROUGH ITS OWN API
/// (kb/Work PB2296; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1)).
/// <para>Step 1 of the <c>DataBinder</c> extract regrouped the binder's state: <c>DataBindState</c> holds the value slices
/// and the four append-only fact sets, <c>ProcedureBindState</c> the scoped override, the drain queues and the cursors.
/// The C# types hold most of it (a fact set admits no removal; a scope cursor and a scoped override change through a
/// restoring scope; a drain queue gives up entries only as a mark's suffix). What the types cannot say is WHO writes a
/// value slice, so this test says it, by a scan of the compiler's source:</para>
/// <list type="bullet">
/// <item>the checking slices are written only with <c>CheckingFold</c>'s answer (<c>State.UnitChecking = CheckingFold.…</c>
/// and <c>.Checking = CheckingFold.Method(…)</c>), so no second fold of a <c>&gt;&gt;TURN</c> query can appear;</item>
/// <item>the inherited-globals slice is written only by <c>InheritStep</c>, so no second inheritance path can
/// appear;</item>
/// <item>the data binder's own slices (the type forest and its name tables, the files, the clause facts, the storage tier,
/// the implicit registers; PB2296 M6) are written only by the files that own their writers
/// (<see cref="TheDataBindersSlices_AreWrittenOnlyByTheirWriters"/>), and <c>DataBinder</c> keeps no table of its own: every
/// collection a reader sees is a read-only view of a slice
/// (<see cref="TheDataBinder_DeclaresNoCollectionOtherReadersSee_EveryTableIsASliceView"/>).</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DataBindStateBoundaryDriftTests
{
    private static IEnumerable<(string File, string Text)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Fact]
    public void TheCheckingSlices_AreWrittenOnlyWithCheckingFoldsAnswer()
    {
        var writes = new Regex(@"(\.UnitChecking|\.Checking)\s*=(?!=)\s*(?<rhs>[^;]*);");
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
            foreach (Match m in writes.Matches(text))
            {
                found++;
                if (!m.Groups["rhs"].Value.TrimStart().StartsWith("CheckingFold.", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        Assert.True(found >= 4, $"the scan found only {found} checking-slice writes — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "A checking slice is written with something other than CheckingFold's answer — fold it in CheckingFold "
            + "(codegen owns no TurnState, and one fold per element is the rule):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>The configuration slice is SEEDED in one place (kb/Work PB2296 M5; ISO §12.3.4 GR1, §11.9.4 GR1): what a
    /// container hands a contained unit or a class hands its halves (<c>ConfigurationNames.InheritFrom</c>, the OPTIONS
    /// baseline and skeleton paragraph, the mnemonic container) is written only by <c>ConfigurationStep</c>, and the step is
    /// run only by the two drivers that bind units (<c>BinderDriver</c>, <c>OoDriver</c>).</summary>
    [Fact]
    public void TheConfigurationSlice_IsSeededOnlyByConfigurationStep()
    {
        var seeding = new Regex(@"\.(InheritFrom|InheritMnemonicsFrom)\(|\.(InheritedOptions|OptionsParagraph)\s*=(?!=)");
        var producers = new Regex(@"ConfigurationStep\.Produce\(");
        var offenders = new List<string>();
        int seeds = 0, produced = 0;
        foreach (var (file, text) in CompilerSources())
        {
            if (file.EndsWith("/Binding/ConfigurationNames.cs", StringComparison.Ordinal)
                || file.EndsWith("/Binding/ConfigurationSlice.cs", StringComparison.Ordinal)) continue;
            foreach (Match m in seeding.Matches(text))
            {
                seeds++;
                if (!file.EndsWith("/Binding/ConfigurationStep.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
            foreach (Match m in producers.Matches(text))
            {
                if (file.EndsWith("/Binding/ConfigurationStep.cs", StringComparison.Ordinal)) continue;
                produced++;
                if (!file.EndsWith("/Binding/BinderDriver.cs", StringComparison.Ordinal)
                    && !file.EndsWith("/Oo/OoDriver.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        }
        Assert.True(seeds >= 10 && produced >= 5,
            $"the scan found {seeds} seeding writes and {produced} step runs — the patterns no longer match the code");
        Assert.True(offenders.Count == 0,
            "A unit's configuration slice is seeded outside ConfigurationStep — a contained program's and a class half's "
            + "configuration has ONE producer (ISO §12.3.4 GR1):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheInheritedGlobalsSlice_IsWrittenOnlyByInheritStep()
    {
        var writers = new Regex(@"\.(AddContainerFiles|AddContainerTypeDecls|AddContainerConstants|OfferContainerGlobals)\(|Inherited\.DropShadowed\(");
        var offenders = new List<string>();
        int found = 0;
        foreach (var (file, text) in CompilerSources())
        {
            if (file.EndsWith("/Binding/InheritedGlobals.cs", StringComparison.Ordinal)) continue;
            foreach (Match m in writers.Matches(text))
            {
                found++;
                if (!file.EndsWith("/Binding/InheritStep.cs", StringComparison.Ordinal))
                    offenders.Add($"{file}: {m.Value}");
            }
        }
        Assert.True(found >= 3, $"the scan found only {found} inherited-globals writes — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "The inherited-globals slice is written outside InheritStep — a contained program's inheritance has ONE "
            + "producer (ISO §13.18.27.4 GR1–GR2):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>⛔ A FACT THE BINDER'S PHASES PRODUCE IS A SLICE OF <c>DataBindState</c>, NEVER A TABLE ON THE BINDER (kb/Work
    /// PB2296 M6; design §8.3 (1)): <c>DataBinder</c> declares no public or internal collection-typed field or property (an
    /// init-only one is a unit-kind input the driver hands in, not a fact). A private collection is the walk's own working
    /// memory, which no other phase can read. So the next table a pass wants to publish has no place to go but a slice, whose
    /// views are read-only and whose writer is pinned below.</summary>
    [Fact]
    public void TheDataBinder_DeclaresNoCollectionOtherReadersSee_EveryTableIsASliceView()
    {
        static bool IsCollection(Type t) =>
            t != typeof(string) && new[] { t }.Concat(t.GetInterfaces()).Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() is var d
                && (d == typeof(ICollection<>) || d == typeof(IReadOnlyCollection<>) || d == typeof(IDictionary<,>)
                    || d == typeof(IReadOnlyDictionary<,>) || d == typeof(ISet<>) || d == typeof(IReadOnlySet<>)));
        static bool IsInit(PropertyInfo p) =>
            p.SetMethod is { } set && set.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var offenders = new List<string>();
        foreach (var f in typeof(DataBinder).GetFields(all))
            if (!f.IsPrivate && !f.Name.Contains('<') && IsCollection(f.FieldType)) offenders.Add($"field {f.Name} : {f.FieldType.Name}");
        foreach (var p in typeof(DataBinder).GetProperties(all))
        {
            var get = p.GetMethod;
            if (get is null || get.IsPrivate || IsInit(p) || !IsCollection(p.PropertyType)) continue;
            offenders.Add($"property {p.Name} : {p.PropertyType.Name}");
        }
        Assert.True(offenders.Count == 0,
            "DataBinder declares a collection other code can read — put the fact in the DataBindState slice its producing "
            + "phase owns (a read-only view, one writer), not on the binder:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>Each value slice of the data binder's own phases has the writers its summary names (kb/Work PB2296 M6): the
    /// scan finds every call of a slice's mutator in the compiler's source and fails on a call from any other file. The
    /// views are read-only, so a reader cannot write; this says WHO may.</summary>
    [Fact]
    public void TheDataBindersSlices_AreWrittenOnlyByTheirWriters()
    {
        // Writer files are matched by repo-relative path suffix; '*' stands for any file-name characters.
        var slices = new (string Name, Regex Mutator, string[] Writers)[]
        {
            ("ForestSlice (the type forest and its name tables)",
                new(@"\.Forest\.(AddRoot|AddWorkingStorageRoots|AddLocalStorageRoots|DeclareName|DeclareCondition|DeclareIndex|TryDeclareType|AddScreenNames|Inherit|AddCapacityRegister)\("),
                ["/Binding/DataBinder*.cs", "/Binding/FileControlBinder.FileSection.cs", "/Binding/Passes/DynamicResolvePass.cs"]),
            // The LATE doors (design §8.3 (1)): the PROCEDURE binders mint compiler temporaries after every data phase, so a slice
            // that takes them is frozen only when those binders are done. Each door has the one caller that names its phase.
            ("ForestSlice's late door (a compiler temporary's root, procedure phase)",
                new(@"\.Forest\.AddTemporaryRoot\("),
                ["/Binding/CompilerTemps.cs"]),
            ("StorageTierSlice's late door (a function result temporary's cell, procedure phase)",
                new(@"\.StorageTier\.ClaimTemporaryCell\("),
                ["/Binding/DataBinder.Ptr.cs"]),
            ("DataBinder.ClaimTemporarySlotCarriedCell (the late cell claim, procedure phase)",
                new(@"\.ClaimTemporarySlotCarriedCell\("),
                ["/Binding/Procedure/Verbs/UdfBinder.cs"]),
            ("DataBinder.ClaimSlotCarriedCell (the data-phase cell claim of a program's RETURNING item)",
                new(@"\.ClaimSlotCarriedCell\("),
                ["/Binding/Passes/PtrBindBasedAndAddressablesPass.cs"]),
            ("FileSlice (the files the unit declares)",
                new(@"\.Files\.Declare\("),
                ["/Binding/FileControlBinder.cs", "/Binding/FileControlBinder.FileSection.cs"]),
            ("StorageTierSlice (the static channels, cell backings and bridges)",
                new(@"\.StorageTier\.(AddStatic\w+|AddBasedBridge|ClaimAddressableCell)\("),
                ["/Binding/DataBinder.Ptr.cs", "/Binding/Passes/PtrBindBasedAndAddressablesPass.cs", "/Binding/Passes/RouteStaticUnitStoragePass.cs"]),
            ("ClauseFacts (what the entry walk recorded for the screening passes)",
                new(@"\.Clauses\.(Record\w+\(|LapsedTypeMembers\s*=(?!=))"),
                ["/Binding/DataBinder*.cs", "/Binding/Passes/ExpandTypesPass.cs"]),
            ("ImplicitRegisters (the DEBUG-ITEM family)",
                new(@"\.Implicit\.ActivateDebugRegisters\("),
                ["/Binding/Procedure/ProcedureTableBuilder.cs"]),
        };
        var offenders = new List<string>();
        var found = new int[slices.Length];
        foreach (var (file, text) in CompilerSources())
            for (int i = 0; i < slices.Length; i++)
            {
                var (name, mutator, writers) = slices[i];
                foreach (Match m in mutator.Matches(text))
                {
                    found[i]++;
                    bool allowed = writers.Any(w => Regex.IsMatch(file, "^.*" + Regex.Escape(w).Replace(@"\*", "[^/]*") + "$"));
                    if (!allowed) offenders.Add($"{file}: {m.Value}  ({name})");
                }
            }
        for (int i = 0; i < slices.Length; i++)
            Assert.True(found[i] > 0, $"the scan found no write of {slices[i].Name} — the pattern no longer matches the code");
        Assert.True(offenders.Count == 0,
            "A data-binder slice is written outside the files that own its writers — a slice has the one producer its summary "
            + "names (design §8.3 (1)):\n  " + string.Join("\n  ", offenders));
    }
}
