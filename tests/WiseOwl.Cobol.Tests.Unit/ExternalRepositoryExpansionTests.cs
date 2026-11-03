// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Cli;
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

    [Fact]   // §12.3.8.4 GR2: the expansion specifier's own AS literal externalizes the EXPANSION (the file, the unit, the identity).
    public void TheExpansionSpecifiersAsLiteral_IsTheExpansionsExternalizedName()
    {
        WithDirectory("expansion-as", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1").Replace("CLASS HOLD-BOX EXPANDS", "CLASS HOLD-BOX AS \"Boxed-Holder\" EXPANDS"));
            Assert.True(r.Success, Codes(r));
            var own = Records(Path.Combine(dir, "Boxed-Holder.dll")).Single();
            Assert.Equal("Boxed-Holder", own.ExternalizedName);
            Assert.Equal("HOLD-BOX", own.Name);
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 042", stdout);
        });
    }

    [Fact]   // §9.3.13: the interface arm — a parameterized INTERFACE is a skeleton too, and its expansion an assembly of its own that a class of the group implements.
    public void AParameterizedInterface_IsPublished_ExpandedFromTheRepository_AndImplementedByAGroupClass()
    {
        const string iput = """
            IDENTIFICATION DIVISION.
            INTERFACE-ID. IPUT USING T.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS T.
            PROCEDURE DIVISION.
            METHOD-ID. PUT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X USAGE OBJECT REFERENCE T.
            PROCEDURE DIVISION USING X.
            END METHOD PUT.
            END INTERFACE IPUT.

            """;
        const string client = """
            IDENTIFICATION DIVISION.
            CLASS-ID. PUTTER INHERITS FROM BASE.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BASE
                CLASS BOX
                INTERFACE IPUT
                INTERFACE IPUT-BOX EXPANDS IPUT USING BOX.
            OBJECT.
                IMPLEMENTS IPUT-BOX.
            PROCEDURE DIVISION.
            METHOD-ID. PUT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X USAGE OBJECT REFERENCE BOX.
            PROCEDURE DIVISION USING X.
                DISPLAY "PUTTER PUT".
                INVOKE X "SHOW".
            END METHOD PUT.
            END OBJECT.
            END CLASS PUTTER.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFMAIN.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS BOX
                CLASS PUTTER
                INTERFACE IPUT
                INTERFACE IPUT-BOX EXPANDS IPUT USING BOX.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 B USAGE OBJECT REFERENCE BOX.
            01 P USAGE OBJECT REFERENCE PUTTER.
            01 I USAGE OBJECT REFERENCE IPUT-BOX.
            PROCEDURE DIVISION.
                INVOKE BOX "NEW" RETURNING B
                INVOKE B "SETV" USING 5
                INVOKE PUTTER "NEW" RETURNING P
                SET I TO P
                INVOKE I "PUT" USING B
                STOP RUN.
            END PROGRAM IFMAIN.
            """;
        WithDirectory("interface-skeleton", dir =>
        {
            var lib = Compile(dir, "LIBX", Box + iput);
            Assert.True(lib.Success, Codes(lib));
            Assert.Contains(Records(Path.Combine(dir, "LIBX.dll")), x => x.Kind is RepositoryUnitKind.ParameterizedInterface && x.ExternalizedName == "IPUT");
            var r = Compile(dir, "CLIENT1", client);
            Assert.True(r.Success, Codes(r));
            var own = Records(Path.Combine(dir, "IPUT-BOX.dll")).Single();
            Assert.Equal(RepositoryUnitKind.Expansion, own.Kind);
            Assert.Null(own.Class!.FactoryType);   // an interface expansion
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("PUTTER PUT\nBOX 005", stdout);
        });
    }

    /// <summary>CRATE: another class of the library, so an expansion of HOLD with it as the actual is another class than HOLD-BOX.</summary>
    private static readonly string Crate = Box.Replace("BOX", "CRATE");

    private static string ClientOfCrate(string id) => Client(id).Replace("CLASS BOX", "CLASS CRATE").Replace("REFERENCE BOX", "REFERENCE CRATE")
        .Replace("INVOKE BOX", "INVOKE CRATE").Replace("USING BOX", "USING CRATE").Replace("\"BOX ", "\"CRATE ");

    private static CompilerDriver.Result CompileSearching(string dir, string name, string source, string repository, int level = 2014)
    {
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, name + ".cob");
        File.WriteAllText(src, source);
        return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: level,
            SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [repository]));
    }

    [Fact]   // §9.3.12 "shall not": a different class under the name of the file placement (a) would overwrite is refused, and the file stays.
    public void ASameNameExpansionOfOtherActuals_AtTheFilePlacementAWouldOverwrite_IsRefused_AndTheFileIsLeftAlone()
    {
        WithDirectory("expansion-same-name-target", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Crate + Hold()).Success);
            Assert.True(Compile(dir, "CLIENTBOX", Client("CLIENTBOX")).Success);
            string file = Path.Combine(dir, "HOLD-BOX.dll");
            byte[] before = File.ReadAllBytes(file);
            var r = Compile(dir, "CLIENTCRATE", ClientOfCrate("CLIENTCRATE"));
            Assert.False(r.Success);
            string refusal = Assert.Single(r.Errors.Concat(r.Warnings), m => m.Contains("COBOL2240"));
            Assert.Contains("shall not have the same externalized object-class-name", refusal);
            Assert.Contains("REPOSITORY CLASS 'HOLD-BOX'", refusal);
            Assert.Equal(before, File.ReadAllBytes(file));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENTBOX.dll"), dir);   // the first group's class is the one the file still holds
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 042", stdout);
        });
    }

    [Fact]   // §9.3.12 "shall not", §10.5: the same refusal where the other class lives in an assembly the group references (the skeleton's own).
    public void ASameNameExpansionInAnAssemblyTheGroupReferences_IsRefused()
    {
        WithDirectory("expansion-same-name-referenced", dir =>
        {
            // The library itself expands HOLD with its own class BOX (placement (b): HOLD-BOX lives in LIBX.dll); a client of that library,
            // which references LIBX.dll for the skeleton and for CRATE, asks for a HOLD-BOX of CRATE: the two share its run unit.
            Assert.True(Compile(dir, "LIBX", Box + Crate + Hold() + "\n" + Client("LIBMAIN")).Success);
            Assert.Contains(Records(Path.Combine(dir, "LIBX.dll")), x => x.Kind is RepositoryUnitKind.Expansion && x.ExternalizedName == "HOLD-BOX");
            var r = Compile(dir, "CLIENTCRATE", ClientOfCrate("CLIENTCRATE"));
            Assert.False(r.Success);
            string refusal = Assert.Single(r.Errors, m => m.Contains("COBOL2240"));
            Assert.Contains("LIBX.dll", refusal);
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
        });
    }

    [Fact]   // §10.5, REPO-2: a same-name, different-triple expansion that is merely elsewhere in the search space is a warning; this one is made as usual.
    public void ASameNameExpansionElsewhereInTheSearchSpace_IsRepo2_AndTheExpansionIsMade()
    {
        WithDirectory("expansion-same-name-elsewhere", dir =>
        {
            string a = Path.Combine(dir, "a"), b = Path.Combine(dir, "b");
            Assert.True(Compile(a, "LIBX", Box + Crate + Hold()).Success);
            Assert.True(Compile(a, "CLIENTBOX", Client("CLIENTBOX")).Success);
            var r = CompileSearching(b, "CLIENTCRATE", ClientOfCrate("CLIENTCRATE"), a);
            Assert.True(r.Success, Codes(r));
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL5963"));
            Assert.Contains("HOLD-BOX", warning);
            Assert.Equal(["CRATE"], Records(Path.Combine(b, "HOLD-BOX.dll")).Single().Expansion!.ActualIdentities.ToList());
            Assert.Equal(["BOX"], Records(Path.Combine(a, "HOLD-BOX.dll")).Single().Expansion!.ActualIdentities.ToList());   // the other class is untouched
        });
    }

    [Fact]   // REPO-12: an existing <name>.dll that an older runtime major wrote is replaced, with the warning; never silently.
    public void AnExpansionFileAnOtherRuntimeMajorWrote_IsReplacedUnderRepo12()
    {
        WithDirectory("expansion-skewed-target", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            Assert.True(Compile(dir, "CLIENT1", Client("CLIENT1")).Success);
            string file = Path.Combine(dir, "HOLD-BOX.dll");
            Assert.True(RuntimeVersionSkew.Patch(file, System.Text.Encoding.UTF8) > 0, "the expansion records its runtime version nowhere in UTF-8");
            byte[] skewed = File.ReadAllBytes(file);
            var r = Compile(dir, "CLIENT2", Client("CLIENT2", 7));
            Assert.True(r.Success, Codes(r));
            string skew = Assert.Single(r.Warnings, w => w.Contains("COBOL5972"));
            Assert.Contains("is replaced", skew);
            Assert.NotEqual(skewed, File.ReadAllBytes(file));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT2.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 007", stdout);
        });
    }

    [Fact]   // §12.3.8.4 GR5, §10.5 "Residual": a skeleton compiled under another state whose expansion names a class of the group is refused, naming the entry's kind.
    public void AnImportedSkeletonUnderAnotherCompileState_WhoseExpansionNamesAGroupClass_IsRefused()
    {
        WithDirectory("expansion-foreign-state", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(), level: 2014).Success);
            string own = Box.Replace("BOX", "LBOX");
            var client = Client("CLIENT3").Replace("CLASS BOX", "CLASS LBOX").Replace("REFERENCE BOX", "REFERENCE LBOX")
                .Replace("INVOKE BOX", "INVOKE LBOX").Replace("USING BOX", "USING LBOX");
            var r = Compile(dir, "CLIENT3", own + client, level: 2023);
            Assert.False(r.Success);
            string refusal = Assert.Single(r.Errors, m => m.Contains("COBOL2240"));
            Assert.Contains("REPOSITORY CLASS 'HOLD-BOX'", refusal);
            Assert.Contains("another compile state", refusal);
        });
    }

    [Fact]   // The same refusal for an INTERFACE expansion names an INTERFACE entry, not a class.
    public void TheForeignStateRefusal_OfAnInterfaceExpansion_NamesAnInterfaceEntry()
    {
        const string iput = """
            IDENTIFICATION DIVISION.
            INTERFACE-ID. IPUT USING T.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS T.
            PROCEDURE DIVISION.
            METHOD-ID. PUT.
            DATA DIVISION.
            LINKAGE SECTION.
            01 X USAGE OBJECT REFERENCE T.
            PROCEDURE DIVISION USING X.
            END METHOD PUT.
            END INTERFACE IPUT.

            """;
        const string client = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFMAIN.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                CLASS LBOX
                INTERFACE IPUT
                INTERFACE IPUT-LBOX EXPANDS IPUT USING LBOX.
            PROCEDURE DIVISION.
                STOP RUN.
            END PROGRAM IFMAIN.
            """;
        WithDirectory("interface-foreign-state", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + iput, level: 2014).Success);
            var r = Compile(dir, "CLIENT3", Box.Replace("BOX", "LBOX") + client, level: 2023);
            Assert.False(r.Success);
            Assert.Contains("REPOSITORY INTERFACE 'IPUT-LBOX'", Assert.Single(r.Errors, m => m.Contains("COBOL2240")));
        });
    }

    [Fact]   // §10.5, §7.2: a numeric literal argument to a method of an expansion in another assembly crosses through the method's own profile.
    public void ANumericLiteralArgument_ToAMethodOfAnExpansionInAnotherAssembly_Compiles()
    {
        string hold = Hold().Replace("METHOD-ID. SHOW-IT.", """
            METHOD-ID. SETN.
            DATA DIVISION.
            LINKAGE SECTION.
            01 N PIC 9(3).
            PROCEDURE DIVISION USING N.
                DISPLAY "N " N.
            END METHOD SETN.
            METHOD-ID. SHOW-IT.
            """);
        WithDirectory("expansion-literal-argument", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + hold).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1").Replace("INVOKE HB \"SHOW-IT\"", "INVOKE HB \"SETN\" USING 42\n    INVOKE HB \"SHOW-IT\""));
            Assert.True(r.Success, Codes(r));
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));   // placement (a)
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("N 042\nBOX 042", stdout);
            // The second group imports the same expansion (placement Imported): the same crossing.
            var again = Compile(dir, "CLIENT2", Client("CLIENT2").Replace("INVOKE HB \"SHOW-IT\"", "INVOKE HB \"SETN\" USING 7\n    INVOKE HB \"SHOW-IT\""));
            Assert.True(again.Success, Codes(again));
            var (exit2, stdout2, stderr2) = Run(Path.Combine(dir, "CLIENT2.dll"), dir);
            Assert.True(exit2 == 0, stderr2);
            Assert.Equal("N 007\nBOX 042", stdout2);
        });
    }

    [Fact]   // §9.3.12, §10.5 (4a): the expansion's text reaches its own compilation with every literal intact, whatever its length and spaces.
    public void AnExpansionWhoseSkeletonHoldsLongLiteralsWithSpaces_IsWrittenIntact()
    {
        // 140 characters with a space after every second one, so any 200-position fill point of the expansion text lands inside a literal.
        string literal = string.Concat(Enumerable.Range(0, 35).Select(i => $"A{i % 10} B{i % 10} ")).TrimEnd();
        string show = $"DISPLAY \"{literal}\"\n        DISPLAY \"{literal}\"\n        DISPLAY \"{literal}\"\n        INVOKE ITEM \"SHOW\".";
        WithDirectory("expansion-long-literals", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(show)).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"));
            Assert.True(r.Success, Codes(r));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal($"{literal}\n{literal}\n{literal}\nBOX 042", stdout);
        });
    }

    [Fact]   // §6.2.3.1, §6.5 8): the free-form lines of an expansion's text read back as the text does, for a literal of any length.
    public void TheFreeFormLines_OfALiteralLongerThanALine_ReadBackAsTheLiteral()
    {
        string literal = string.Concat(Enumerable.Range(0, 220).Select(i => i % 7 == 0 ? "it''s " : $"w{i} ")).TrimEnd();   // doubled apostrophes straddle fill points
        string text = $"IDENTIFICATION DIVISION. PROGRAM-ID. LONGLIT. PROCEDURE DIVISION. DISPLAY '{literal}' STOP RUN. END PROGRAM LONGLIT.";
        string lines = ExpansionAssemblies.FreeForm(text);
        Assert.All(lines.Split('\n'), line => Assert.True(line.Length <= 200, $"{line.Length} positions"));
        Assert.True(lines.Split('\n').Length > 3);
        WithDirectory("freeform-long-literal", dir =>
        {
            var r = Compile(dir, "LONGLIT", lines + "\n", level: 2023);
            Assert.True(r.Success, Codes(r));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "LONGLIT.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal(literal.Replace("''", "'"), stdout);
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

    [Fact]   // §9.3.12, §10.5 (kb/Work PB4457): the warnings of an expansion compiled in an assembly of its own are kept, at the EXPANDS entry.
    public void AWarningOfAnExpansionInAnAssemblyOfItsOwn_IsCarriedAndReportedAtTheExpandsEntry()
    {
        // NEXT SENTENCE is obsolete at COBOL-2023 (COBOL0903, a warning, Annex F.1): the skeleton's text carries it into the expansion.
        string show = "IF 1 = 1 NEXT SENTENCE ELSE DISPLAY \"A\".\n        INVOKE ITEM \"SHOW\".";
        WithDirectory("expansion-warning", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(show), level: 2023).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"), level: 2023);
            Assert.True(r.Success, Codes(r));
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));   // placement (a): the warning is the sub-compilation's
            // The EXPANDS entry is `CLASS HOLD-BOX EXPANDS HOLD USING BOX.` on line 8 of the client, column 5; the expansion is named.
            string entry = Path.Combine(dir, "CLIENT1.cob") + "(8,5)";
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL0903", StringComparison.Ordinal));
            Assert.StartsWith($"{entry}: warning COBOL0903:", warning, StringComparison.Ordinal);
            Assert.EndsWith("[in the expansion 'HOLD-BOX']", warning, StringComparison.Ordinal);
            Assert.DoesNotContain(r.Warnings, w => System.Text.RegularExpressions.Regex.IsMatch(w, "expansion-[0-9a-f]{32}\\.cob"));   // none names the throw-away file
        });
    }

    /// <summary>A skeleton whose text raises a warning at every edition: a relation of two literals (COBOL3146, ISO §8.8.4.2.1 — "A relation
    /// condition shall contain at least one reference to an operand that is not a literal"), which the BIND raises, so the group's bind of the
    /// expansion's text and the expansion's own compilation both do.</summary>
    private const string RelationOfLiterals = "IF 1 = 1 DISPLAY \"A\".\n        INVOKE ITEM \"SHOW\".";

    [Fact]   // kb/Work PB4469: the group binds an own-assembly expansion's text to type its callers and the expansion's compilation binds it again; one report.
    public void AWarningBothTheGroupAndTheExpansionRaise_IsReportedOnce_AtTheExpandsEntry()
    {
        WithDirectory("expansion-warning-once", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(RelationOfLiterals), level: 2023).Success);
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"), level: 2023);
            Assert.True(r.Success, Codes(r));
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));   // placement (a): the expansion's compilation owns the text's warnings
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL3146", StringComparison.Ordinal));
            Assert.StartsWith($"{Path.Combine(dir, "CLIENT1.cob")}(8,5): warning COBOL3146:", warning, StringComparison.Ordinal);
            Assert.EndsWith("[in the expansion 'HOLD-BOX']", warning, StringComparison.Ordinal);
        });
    }

    [Fact]   // kb/Work PB4469: the same text in the group's own assembly (arm b) is reported once, at the same column as arm (a) reports it.
    public void AWarningOfAnExpansionInTheGroupsAssembly_IsReportedOnce_AtTheExpandsEntrysColumn()
    {
        WithDirectory("expansion-warning-in-group", dir =>
        {
            Assert.True(Compile(dir, "LIBH", Hold(RelationOfLiterals), level: 2023).Success);
            string own = Box.Replace("BOX", "LBOX");
            var client = Client("CLIENT3").Replace("CLASS BOX", "CLASS LBOX").Replace("REFERENCE BOX", "REFERENCE LBOX")
                .Replace("INVOKE BOX", "INVOKE LBOX").Replace("USING BOX", "USING LBOX");
            string source = own + client;
            var r = Compile(dir, "CLIENT3", source, level: 2023);
            Assert.True(r.Success, Codes(r));
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-LBOX.dll")));   // placement (b): the group's bind is the only one that sees the text
            int entryLine = source.Split('\n').ToList().FindIndex(l => l.Contains("EXPANDS HOLD", StringComparison.Ordinal)) + 1;   // the entry begins at column 5
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL3146", StringComparison.Ordinal));
            Assert.StartsWith($"{Path.Combine(dir, "CLIENT3.cob")}({entryLine},5): warning COBOL3146:", warning, StringComparison.Ordinal);
        });
    }

    [Fact]   // PB4457: an error of the sub-compilation is reported at the EXPANDS entry whatever shape the position takes.
    public void ADiagnosticOfAnExpansionText_IsRelabelledToTheExpandsEntry()
    {
        string temp = Path.Combine(Path.GetTempPath(), "expansion-0123456789abcdef.cob");
        var entry = new WiseOwl.Cobol.Frontend.Common.SourceLocation("client.cob", 0, 7, 4);   // line 8, column 5 as a user reads it
        string At(string diagnostic) => ExpansionAssemblies.AtTheEntry(diagnostic, temp, "HOLD-BOX", entry);

        // A positioned diagnostic: the throw-away file and its line are replaced; the expansion is named.
        Assert.Equal("client.cob(8,5): error COBOL2690: boom (ISO §6.4.2) [in the expansion 'HOLD-BOX']",
            At($"{temp}(41,9): error COBOL2690: boom (ISO §6.4.2)"));
        Assert.Equal("client.cob(8,5): warning COBOL0903: obsolete [in the expansion 'HOLD-BOX']", At($"{temp}(3,1): warning COBOL0903: obsolete"));
        // A diagnostic that names no position takes the entry's.
        Assert.Equal("client.cob(8,5): error COBOL5962: nothing [in the expansion 'HOLD-BOX']", At("error COBOL5962: nothing"));
        // A mention of the file inside a message is not left pointing at a file that is gone.
        string mention = At($"error COBOL0001: cannot read {temp}");
        Assert.DoesNotContain("expansion-0123456789abcdef", mention, StringComparison.Ordinal);
        Assert.Contains("the text of the expansion 'HOLD-BOX'", mention, StringComparison.Ordinal);
        // The header line and the Roslyn diagnostics of the generated C# are not positions in the text.
        Assert.Equal("the expansion assembly 'HOLD-BOX.dll' (ISO §9.3.12):", At("the expansion assembly 'HOLD-BOX.dll' (ISO §9.3.12):"));
        Assert.Equal("HOLD-BOX.g.cs(12,3): error CS0103: no", At("HOLD-BOX.g.cs(12,3): error CS0103: no"));
    }

    [Fact]   // PB4457: the CLI names the generated C# only when there is a file to open.
    public void TheBackendFailureHeadline_NamesTheGeneratedCSharp_OnlyWhenItExists()
    {
        Assert.Equal("error: backend compilation failed:", Program.BackendFailureHeadline(null));
        Assert.Equal("error: backend compilation failed:", Program.BackendFailureHeadline(""));
        Assert.Equal("error: backend compilation failed:", Program.BackendFailureHeadline(Path.Combine(Path.GetTempPath(), "no-such-file.g.cs")));
        WithDirectory("headline", dir =>
        {
            string cs = Path.Combine(dir, "X.g.cs");
            File.WriteAllText(cs, "// generated");
            Assert.Equal($"error: backend compilation failed (generated C# at {cs}):", Program.BackendFailureHeadline(cs));
        });
    }
}
