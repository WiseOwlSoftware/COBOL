// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ PARAMETERIZED CLASSES ACROSS ASSEMBLIES: SKELETONS, EXPANSIONS, THEIR KEY, THEIR PLACEMENT AND STALE EXPANSIONS
/// (ISO §9.3.12, §9.3.13, §12.3.8.3 SR4/SR6, §12.3.8.4 GR5/GR8; DESIGN-external-repository §10.5, §17.1; kb/Work PB2104). Each Fact
/// compiles a library that defines a class BOX and the parameterized class HOLD USING ELEM into a directory, and one or more
/// callers against it, as separate compilations, and reads what each wrote with the same reader a later compilation uses. The
/// multi-unit goldens (<c>conformance-suite/wiseowl/cases/12.3.8-REPOSITORY/pb2104_expand_skeleton_from_repository</c> and
/// <c>pb2104_expansion_is_one_class_across_groups</c>) run the result; these are the cases that need a directory layout, a second
/// build of a changed library or the bytes and units of what was written.
/// <para>§9.3.12: "Within a run unit, two classes with the same externalized object-class-name that are created by expanding the same
/// parameterized class with the same actual parameters are the same class instance"; §12.3.8.4 GR5: "The class object-class-name-1 is
/// created from the parameterized class object-class-name-2 by replacing each specification of the formal parameter by the
/// corresponding actual parameter".</para>
/// </summary>
public sealed class ExternalRepositoryExpansionTests
{
    private const string Box = """
        IDENTIFICATION DIVISION.
        CLASS-ID. BOX INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 V PIC 9(3) VALUE 0.
        PROCEDURE DIVISION.
        METHOD-ID. SETV.
        DATA DIVISION.
        LINKAGE SECTION.
        01 NUM PIC 9(3).
        PROCEDURE DIVISION USING NUM.
            MOVE NUM TO V.
        END METHOD SETV.
        METHOD-ID. SHOW.
        PROCEDURE DIVISION.
            DISPLAY "BOX " V.
        END METHOD SHOW.
        END OBJECT.
        END CLASS BOX.

        """;

    private static string Hold(string show = "INVOKE ITEM \"SHOW\".") => $"""
        IDENTIFICATION DIVISION.
        CLASS-ID. HOLD INHERITS FROM BASE USING ELEM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE
            CLASS ELEM.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 ITEM USAGE OBJECT REFERENCE ELEM.
        PROCEDURE DIVISION.
        METHOD-ID. PUT.
        DATA DIVISION.
        LINKAGE SECTION.
        01 X USAGE OBJECT REFERENCE ELEM.
        PROCEDURE DIVISION USING X.
            SET ITEM TO X.
        END METHOD PUT.
        METHOD-ID. SHOW-IT.
        PROCEDURE DIVISION.
            {show}
        END METHOD SHOW-IT.
        END OBJECT.
        END CLASS HOLD.
        """;

    private static string Client(string id, int number = 42) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BOX
            CLASS HOLD
            CLASS HOLD-BOX EXPANDS HOLD USING BOX.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 B  USAGE OBJECT REFERENCE BOX.
        01 HB USAGE OBJECT REFERENCE HOLD-BOX.
        PROCEDURE DIVISION.
            INVOKE BOX "NEW" RETURNING B
            INVOKE B "SETV" USING {number}
            INVOKE HOLD-BOX "NEW" RETURNING HB
            INVOKE HB "PUT" USING B
            INVOKE HB "SHOW-IT"
            STOP RUN.
        END PROGRAM {id}.
        """;

    private static CompilerDriver.Result Compile(string dir, string name, string source, int level = 2014)
    {
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, name + ".cob");
        File.WriteAllText(src, source);
        return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: level,
            SourceFormat: InitialReferenceFormat.Free));
    }

    /// <summary>Run the compiled program <paramref name="dll"/> in <paramref name="dir"/> (the repository directory is the run unit's
    /// directory, §11.4) and return its exit code, stdout and stderr.</summary>
    private static (int Exit, string Stdout, string Stderr) Run(string dll, string dir)
    {
        var observed = ProcessObserver.ObserveOrThrow(new ProcessStartInfo("dotnet", $"\"{dll}\"") { WorkingDirectory = dir }, null, runDirectory: dir);
        return (observed.ExitCode, observed.Stdout.ReplaceLineEndings("\n").TrimEnd('\n'), observed.Stderr);
    }

    private static void WithDirectory(string tag, Action<string> body)
    {
        string dir = ScratchDirectory.Create(tag);
        try { body(dir); }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string Codes(CompilerDriver.Result r) => string.Join("\n", r.Errors.Concat(r.Warnings));

    /// <summary>The records the assembly at <paramref name="path"/> publishes, read as a later compilation reads them.</summary>
    private static IReadOnlyList<RepositoryRecord> Records(string path)
    {
        var opened = RepositoryIndex.Open(path);
        Assert.NotNull(opened.Index);
        using var index = opened.Index!;
        var outcome = RepositoryReader.Read(index);
        Assert.True(outcome.Failure is RepositoryReadFailure.None, outcome.Detail);
        return outcome.Records;
    }

    [Fact]   // §7.2: a parameterized definition emits a static class with [CobolUnit(ParameterizedClass)] + [CobolSkeleton].
    public void AParameterizedClass_IsPublishedAsASkeleton_WithItsFormalsResolutionsAndCompileState()
    {
        WithDirectory("skeleton-record", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            var records = Records(Path.Combine(dir, "LIBX.dll"));
            Assert.Equal([RepositoryUnitKind.Class, RepositoryUnitKind.ParameterizedClass], records.Select(r => r.Kind).ToList());
            var skeleton = records.Single(r => r.Kind is RepositoryUnitKind.ParameterizedClass);
            Assert.Equal("HOLD", skeleton.ExternalizedName);
            var s = skeleton.Skeleton!;
            Assert.Equal(["ELEM"], s.Formals.ToList());
            Assert.Equal([false], s.FormalIsInterface.ToList());
            Assert.Equal([new ResolutionRecord("BASE", false, "BASE")], s.Resolutions.ToList());
            Assert.StartsWith("IDENTIFICATION DIVISION. CLASS-ID. HOLD INHERITS FROM BASE USING ELEM.", s.Source);
            Assert.EndsWith("END CLASS HOLD.", s.Source);
            Assert.Equal(2014, s.State.Edition);
            Assert.False(s.State.Permissive);
            Assert.Equal("", s.State.DirectiveFolds);
        });
    }

    [Fact]   // §10.5 (4a): an expansion that names no class of the expanding group is its own assembly, published as an expansion.
    public void AnExpansionThatNamesNoClassOfTheGroup_IsWrittenToAnAssemblyOfItsOwn_AndRuns()
    {
        WithDirectory("expansion-own", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"));
            Assert.True(r.Success, Codes(r));
            Assert.DoesNotContain(r.Warnings, w => w.Contains("COBOL5969"));
            var own = Records(Path.Combine(dir, "HOLD-BOX.dll")).Single();
            Assert.Equal(RepositoryUnitKind.Expansion, own.Kind);
            Assert.Equal("HOLD-BOX", own.ExternalizedName);
            Assert.True(own.Expansion!.OwnAssembly);
            Assert.Equal("HOLD", own.Expansion.DefinitionIdentity);
            Assert.Equal(["BOX"], own.Expansion.ActualIdentities.ToList());
            Assert.Matches("^[0-9a-f]{64}$", own.Expansion.Key);
            Assert.Equal(["definition", "skeleton source", "compile state", "resolution BASE", "actual BOX"],
                own.Expansion.Parts.Select(p => p[..p.IndexOf('=')]).ToList());
            Assert.DoesNotContain(Records(Path.Combine(dir, "CLIENT1.dll")), x => x.Kind is RepositoryUnitKind.Expansion);   // the group's module holds no copy
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 042", stdout);
        });
    }

    [Fact]   // §9.3.12: a second group expanding the same triple imports the first's class — one file, written once, one class.
    public void ASecondGroupExpandingTheSameTriple_ImportsTheExistingExpansion_AndDoesNotRewriteIt()
    {
        WithDirectory("expansion-import", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            Assert.True(Compile(dir, "CLIENT1", Client("CLIENT1")).Success);
            string file = Path.Combine(dir, "HOLD-BOX.dll");
            var written = File.GetLastWriteTimeUtc(file);
            var mvid = Records(file).Single().Expansion!.Key;
            Thread.Sleep(50);
            var r = Compile(dir, "CLIENT2", Client("CLIENT2", 7));
            Assert.True(r.Success, Codes(r));
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));
            Assert.Equal(mvid, Records(file).Single().Expansion!.Key);
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT2.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 007", stdout);
        });
    }

    [Fact]   // §10.5 step (3): the skeleton's own group writes the same expansion; the assembly that holds it references the group's previous build.
    public void TheSkeletonsOwnGroup_ImportsAnExpansionThatReferencesItsPreviousBuild()
    {
        WithDirectory("expansion-cycle", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            Assert.True(Compile(dir, "CLIENT1", Client("CLIENT1")).Success);
            string file = Path.Combine(dir, "HOLD-BOX.dll");
            var written = File.GetLastWriteTimeUtc(file);
            Thread.Sleep(50);
            var r = Compile(dir, "LIBX", Box + Hold() + "\n" + Client("LIBMAIN", 9));
            Assert.True(r.Success, Codes(r));
            Assert.DoesNotContain(r.Warnings, w => w.Contains("COBOL5969"));
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));
            Assert.DoesNotContain(Records(Path.Combine(dir, "LIBX.dll")), x => x.Kind is RepositoryUnitKind.Expansion);   // imported, not copied
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "LIBX.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 009", stdout);
        });
    }

    [Fact]   // §10.5 "Stale expansions": another skeleton source under one name and triple — REPO-9 names the component and the stale callers.
    public void AnExpansionWhoseSkeletonChanged_IsStale_NamedByRepo9_AndReplaced()
    {
        WithDirectory("expansion-stale", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            Assert.True(Compile(dir, "CLIENT1", Client("CLIENT1")).Success);
            var oldKey = Records(Path.Combine(dir, "HOLD-BOX.dll")).Single().Expansion!.Key;
            Assert.True(Compile(dir, "LIBX", Box + Hold("INVOKE ITEM \"SHOW\" DISPLAY \"HELD\".")).Success);
            var r = Compile(dir, "CLIENT2", Client("CLIENT2"));
            Assert.True(r.Success, Codes(r));
            string stale = Assert.Single(r.Warnings, w => w.Contains("COBOL5969"));
            Assert.Contains("is stale", stale);
            Assert.Contains("skeleton source", stale);
            Assert.Contains("CLIENT1.dll", stale);   // the assembly compiled against the earlier class
            Assert.NotEqual(oldKey, Records(Path.Combine(dir, "HOLD-BOX.dll")).Single().Expansion!.Key);
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT2.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 042\nHELD", stdout);
        });
    }

    [Fact]   // §10.5 (4b): an expansion whose actual is a class of the expanding group lives in the group's assembly.
    public void AnExpansionThatNamesAClassOfTheGroup_LivesInTheGroupsAssembly()
    {
        WithDirectory("expansion-in-group", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            string own = Box.Replace("BOX", "LBOX");
            var client = Client("CLIENT3").Replace("CLASS BOX", "CLASS LBOX").Replace("REFERENCE BOX", "REFERENCE LBOX")
                .Replace("INVOKE BOX", "INVOKE LBOX").Replace("USING BOX", "USING LBOX");
            var r = Compile(dir, "CLIENT3", own + client);
            Assert.True(r.Success, Codes(r));
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-LBOX.dll")));
            var inGroup = Records(Path.Combine(dir, "CLIENT3.dll")).Single(x => x.Kind is RepositoryUnitKind.Expansion);
            Assert.False(inGroup.Expansion!.OwnAssembly);
            Assert.Equal(["LBOX"], inGroup.Expansion.ActualIdentities.ToList());
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT3.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("LBOX 042", stdout);
        });
    }

    [Fact]   // §6.5, §7.6: the same library and caller in two directories write byte-identical expansion assemblies, and the key is a function of meaning alone.
    public void TheExpansionAssembly_IsDeterministic_AcrossGroupsAndDirectories()
    {
        WithDirectory("expansion-determinism", dir =>
        {
            string a = Path.Combine(dir, "a"), b = Path.Combine(dir, "b");
            foreach (string d in new[] { a, b })
            {
                var lib = Compile(d, "LIBX", Box + Hold());
                Assert.True(lib.Success, Codes(lib));
                var client = Compile(d, "CLIENT1", Client("CLIENT1"));
                Assert.True(client.Success, Codes(client));
            }
            Assert.Equal(File.ReadAllBytes(Path.Combine(a, "HOLD-BOX.dll")), File.ReadAllBytes(Path.Combine(b, "HOLD-BOX.dll")));
            // Two groups with different names for the caller and a different number: the expansion's text and surfaces are the same.
            Assert.True(Compile(Path.Combine(dir, "c"), "LIBX", Box + Hold()).Success);
            Assert.True(Compile(Path.Combine(dir, "c"), "ALTCLIENT", Client("ALTCLIENT", 1)).Success);
            Assert.Equal(Records(Path.Combine(a, "HOLD-BOX.dll")).Single().Expansion!.Key,
                Records(Path.Combine(dir, "c", "HOLD-BOX.dll")).Single().Expansion!.Key);
        });
    }

    [Fact]   // §10.5: a file of the expansion's own name that is not an expansion is not ours to overwrite.
    public void AnAssemblyThatIsNoExpansionOfThatName_IsNeverOverwritten()
    {
        WithDirectory("expansion-foreign", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            File.WriteAllText(Path.Combine(dir, "HOLD-BOX.dll"), "not an assembly");
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"));
            Assert.False(r.Success);
            Assert.Contains("COBOL2240", Codes(r));
            Assert.Equal("not an assembly", File.ReadAllText(Path.Combine(dir, "HOLD-BOX.dll")));
        });
    }

    [Fact]   // A repository with no skeleton of the name: REPO-1 at the specifier, as for any class the repository does not answer.
    public void ASkeletonNothingAnswers_IsRepo1()
    {
        WithDirectory("skeleton-repo1", dir =>
        {
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"));
            Assert.False(r.Success);
            Assert.Contains("COBOL5962", Codes(r));
        });
    }
}
