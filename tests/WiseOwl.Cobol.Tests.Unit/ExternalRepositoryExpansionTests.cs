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

    private static CompilerDriver.Result Compile(string dir, string name, string source, int level = 2014, bool permissive = false)
    {
        Directory.CreateDirectory(dir);
        string src = Path.Combine(dir, name + ".cob");
        File.WriteAllText(src, source);
        return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, name + ".dll"), DialectLevel: level,
            SourceFormat: InitialReferenceFormat.Free, Permissive: permissive));
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

    /// <summary>kb/Work PB4502 review R2-1: the expansion's own assembly is written by a nested compilation, and a write the host
    /// refuses there (a directory standing where its `.g.cs` goes) is the group's OutputNotWritable, exit 73, never a BackendError
    /// (exit 70, Roslyn refusing generated C#). Nothing of the group's own module is written.</summary>
    [Fact]
    public void AnExpansionAssemblyTheHostRefuses_IsOutputNotWritable_NotABackendError()
    {
        WithDirectory("expansion-refused", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            Directory.CreateDirectory(Path.Combine(dir, "HOLD-BOX.g.cs"));
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"));
            Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, r.Status);
            Assert.Contains(r.Errors, e => e.Contains("cannot write the output of", StringComparison.Ordinal)
                && e.Contains("HOLD-BOX", StringComparison.Ordinal));
            Assert.False(File.Exists(Path.Combine(dir, "CLIENT1.dll")));
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

    [Theory]   // kb/Work PB4650, ISO §9.3.12: a group that WRITES the class an existing expansion was compiled against elsewhere does not import it.
    [InlineData("CLIENT1", "CLIENT2")]
    [InlineData("CLIENT2", "CLIENT1")]
    public void AnExpansionCompiledAgainstALibrarysClass_IsNotImported_ByAGroupThatWritesThatClassItself(string first, string second)
    {
        WithDirectory("expansion-other-home-group", dir =>
        {
            // CLIENT1 expands HOLD over the library's BOX; CLIENT2 carries its own, identical, BOX and expands HOLD over that one. Same
            // triple, same key (the key hashes the class's record, not where it lives), two different CLR classes named BOX.
            Assert.True(Compile(dir, "LIBX", Box + Hold()).Success);
            string Source(string id, int number) => id == "CLIENT2" ? Box + Client(id, number) : Client(id, number);
            var made = Compile(dir, first, Source(first, 42));
            Assert.True(made.Success, Codes(made));
            string file = Path.Combine(dir, "HOLD-BOX.dll");
            bool firstWroteTheFile = first == "CLIENT1";
            Assert.Equal(firstWroteTheFile, File.Exists(file));
            var written = firstWroteTheFile ? File.GetLastWriteTimeUtc(file) : default;
            Thread.Sleep(50);
            var r = Compile(dir, second, Source(second, 7));
            Assert.True(r.Success, Codes(r));
            Assert.DoesNotContain(r.Errors.Concat(r.Warnings), m => m.Contains("COBOL5969") || m.Contains("CS0012"));   // no stale copy was named
            // The group that writes BOX has the expansion in its own assembly; the other imported nothing it could not name.
            var inGroup = Assert.Single(Records(Path.Combine(dir, "CLIENT2.dll")), x => x.Kind is RepositoryUnitKind.Expansion);
            Assert.False(inGroup.Expansion!.OwnAssembly);
            // CLIENT2 first: CLIENT1 finds CLIENT2's BOX (CLIENT2.dll is searched too) and so imports the expansion CLIENT2.dll holds, which is
            // the class over that very BOX; nothing is written. CLIENT1 first: its file is the only one.
            Assert.Equal(firstWroteTheFile, File.Exists(file));
            if (firstWroteTheFile) Assert.Equal(written, File.GetLastWriteTimeUtc(file));   // the file CLIENT1 depends on is not written over
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "CLIENT2.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal(first == "CLIENT2" ? "BOX 042" : "BOX 007", stdout);
            var (exit1, stdout1, stderr1) = Run(Path.Combine(dir, "CLIENT1.dll"), dir);
            Assert.True(exit1 == 0, stderr1);
            Assert.Equal(first == "CLIENT1" ? "BOX 042" : "BOX 007", stdout1);
        });
    }

    [Fact]   // kb/Work PB4650, ISO §9.3.12: the same, where the other home is a second library's class — the file (a) would write is not written over.
    public void AnExpansionCompiledAgainstAnotherAssemblysClass_IsNotImported_NorWrittenOver()
    {
        WithDirectory("expansion-other-home-library", dir =>
        {
            // HOLD in one library; the identical BOX in two others. CLIENT1 expands over the first BOX, CLIENT4 over the second.
            string holds = Path.Combine(dir, "holds"), boxes1 = Path.Combine(dir, "boxes1"), boxes2 = Path.Combine(dir, "boxes2"), outs = Path.Combine(dir, "out");
            Assert.True(Compile(holds, "LIBH", Hold()).Success);
            Assert.True(Compile(boxes1, "LIBB", Box).Success);
            Assert.True(Compile(boxes2, "LIBB2", Box).Success);
            CompilerDriver.Result Make(string id, string boxes)
            {
                Directory.CreateDirectory(outs);
                string src = Path.Combine(outs, id + ".cob");
                File.WriteAllText(src, Client(id));
                return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(outs, id + ".dll"), DialectLevel: 2014,
                    SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [holds, boxes, outs]));
            }
            var first = Make("CLIENT1", boxes1);
            Assert.True(first.Success, Codes(first));
            string file = Path.Combine(outs, "HOLD-BOX.dll");
            var written = File.GetLastWriteTimeUtc(file);
            var key = Records(file).Single().Expansion!.Key;
            Thread.Sleep(50);
            var second = Make("CLIENT4", boxes2);
            Assert.True(second.Success, Codes(second));
            Assert.DoesNotContain(second.Errors.Concat(second.Warnings), m => m.Contains("COBOL5969") || m.Contains("CS0012"));
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));
            Assert.Equal(key, Records(file).Single().Expansion!.Key);
            var inGroup = Assert.Single(Records(Path.Combine(outs, "CLIENT4.dll")), x => x.Kind is RepositoryUnitKind.Expansion);
            Assert.False(inGroup.Expansion!.OwnAssembly);
        });
    }

    private static string Counter(string helperEntry = "") => $"""
        IDENTIFICATION DIVISION.
        CLASS-ID. CNT INHERITS FROM BASE USING ELEM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE
            CLASS ELEM{helperEntry}.
        FACTORY.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N PIC 9(3) VALUE 0.
        PROCEDURE DIVISION.
        METHOD-ID. BUMP.
        PROCEDURE DIVISION.
            ADD 1 TO N
            DISPLAY "N=" N.
        END METHOD BUMP.
        END FACTORY.
        END CLASS CNT.

        """;

    private const string Helper = """
        IDENTIFICATION DIVISION.
        CLASS-ID. HELPER INHERITS FROM BASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BASE.
        END CLASS HELPER.

        """;

    private static string CountingProgram(string id, bool callsSub) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BOX
            CLASS CNT
            CLASS CNT-BOX EXPANDS CNT USING BOX{(callsSub ? "\n    PROGRAM SUBP" : "")}.
        PROCEDURE DIVISION.
            INVOKE CNT-BOX "BUMP"
            {(callsSub ? "CALL SUBP\n    INVOKE CNT-BOX \"BUMP\"\n    STOP RUN." : "GOBACK.")}
        END PROGRAM {id}.
        """;

    [Theory]   // kb/Work PB4650, ISO §9.3.12: whether an expansion is the class this compilation names does not depend on what its code happens to reference.
    [InlineData(false, 2014, 2014)]   // the formal is named by no member: the expansion's assembly has no reference to BOX's
    [InlineData(true, 2014, 2014)]    // a skeleton REPOSITORY entry from another library that no member uses
    [InlineData(false, 2014, 2023)]   // the skeleton's state is not the groups': the expansion is made apart, in an assembly of its own
    public void AnExpansionOverTheSameClasses_IsImported_WhateverItsCodeReferences(bool unusedResolution, int libraryLevel, int clientLevel)
    {
        WithDirectory("expansion-same-homes", dir =>
        {
            if (unusedResolution) Assert.True(Compile(dir, "LIBY", Helper).Success);
            string counter = Counter(unusedResolution ? "\n    CLASS HELPER" : "");
            Assert.True(Compile(dir, "LIBX", Box + counter, level: libraryLevel).Success);
            var sub = Compile(dir, "SUBP", CountingProgram("SUBP", callsSub: false), level: clientLevel);
            Assert.True(sub.Success, Codes(sub));
            var again = Compile(dir, "SUBP", CountingProgram("SUBP", callsSub: false), level: clientLevel);   // an unchanged recompile
            Assert.True(again.Success, Codes(again));
            var main = Compile(dir, "MAINP", CountingProgram("MAINP", callsSub: true), level: clientLevel);
            Assert.True(main.Success, Codes(main));
            Assert.DoesNotContain(Records(Path.Combine(dir, "MAINP.dll")), x => x.Kind is RepositoryUnitKind.Expansion);   // imported, not made again
            var (exit, stdout, stderr) = Run(Path.Combine(dir, "MAINP.dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("N=001\nN=002\nN=003", stdout);   // one class, one factory count
        });
    }

    [Fact]   // kb/Work PB4650: an expansion is made over the homes of ALL the classes it names, not any one of them (one assembly shared by two in only one of the compilations).
    public void AnExpansionOverADifferentHomeOfOneNamedClass_IsNotImported_ThoughAnotherNamedClassSharesTheHome()
    {
        WithDirectory("expansion-homes-set", dir =>
        {
            string holds = Path.Combine(dir, "holds"), liba = Path.Combine(dir, "liba"), libb = Path.Combine(dir, "libb"), outs = Path.Combine(dir, "out");
            CompilerDriver.Result Make(string at, string id, string source, params string[] searched)
            {
                Directory.CreateDirectory(at);
                string src = Path.Combine(at, id + ".cob");
                File.WriteAllText(src, source);
                return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(at, id + ".dll"), DialectLevel: 2014,
                    SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [.. searched]));
            }
            // HOLD's code uses HELPER; BOX is in two libraries and HELPER in one of them: CLIENT1 holds {LIBB's BOX, LIBA's HELPER},
            // CLIENT2 holds {LIBA's BOX, LIBA's HELPER}: the same assembly set is not the same two homes.
            Assert.True(Make(liba, "LIBA", Box + Helper).Success);
            Assert.True(Make(libb, "LIBB", Box).Success);
            string hold = Hold().Replace("CLASS ELEM.", "CLASS ELEM\n    CLASS HELPER.").Replace("01 ITEM USAGE OBJECT REFERENCE ELEM.",
                "01 ITEM USAGE OBJECT REFERENCE ELEM.\n01 H USAGE OBJECT REFERENCE HELPER.");
            var libh = Make(holds, "LIBH", hold, liba);
            Assert.True(libh.Success, Codes(libh));
            string Program(string id) => Client(id, 7).Replace("CLASS BOX", "CLASS BOX\n    CLASS HELPER");
            var first = Make(outs, "CLIENT1", Program("CLIENT1"), holds, libb, liba, outs);
            Assert.True(first.Success, Codes(first));
            var second = Make(outs, "CLIENT2", Program("CLIENT2"), holds, liba, outs);
            Assert.True(second.Success, Codes(second));
            Assert.DoesNotContain(second.Errors.Concat(second.Warnings), m => m.Contains("COBOL5969") || m.Contains("CS0012"));
            Assert.Single(Records(Path.Combine(outs, "CLIENT2.dll")), x => x.Kind is RepositoryUnitKind.Expansion);   // made again, in the group
        });
    }

    [Fact]   // kb/Work PB4650: the refusal states the reason that applies when the file (a) would write holds the same expansion made over other homes.
    public void AnExpansionWhoseFileHoldsTheSameExpansionOverAnotherHome_IsRefusedWithThatReason_WhenItCannotLiveInTheGroup()
    {
        WithDirectory("expansion-file-held-foreign-state", dir =>
        {
            string holds = Path.Combine(dir, "holds"), boxes1 = Path.Combine(dir, "boxes1"), boxes2 = Path.Combine(dir, "boxes2"), outs = Path.Combine(dir, "out");
            Assert.True(Compile(holds, "LIBH", Hold(), level: 2014).Success);
            Assert.True(Compile(boxes1, "LIBB", Box).Success);
            Assert.True(Compile(boxes2, "LIBB2", Box).Success);
            CompilerDriver.Result Make(string id, string boxes, int level)
            {
                Directory.CreateDirectory(outs);
                string src = Path.Combine(outs, id + ".cob");
                File.WriteAllText(src, Client(id));
                return CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(outs, id + ".dll"), DialectLevel: level,
                    SourceFormat: InitialReferenceFormat.Free, RepositoryPaths: [holds, boxes, outs]));
            }
            Assert.True(Make("CLIENT1", boxes1, 2014).Success);
            var r = Make("CLIENT4", boxes2, 2023);   // HOLD is of 2014: its expansion cannot be bound in this group, and the file it would go to is taken
            Assert.False(r.Success);
            string refusal = Assert.Single(r.Errors, m => m.Contains("COBOL2240"));
            Assert.Contains("holds the same expansion compiled against other assemblies than this compilation's classes", refusal);
            Assert.DoesNotContain("names a class or interface of this compilation group", refusal);
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
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
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

    [Fact]   // REPO-12: an existing <name>.dll that another runtime major wrote is replaced, with the warning; never silently.
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

    /// <summary>kb/Work PB4502 review R3-1: when the host refuses the GROUP's own `.g.cs` after the expansion's assembly is written,
    /// the OutputNotWritable result keeps what the write phase had gathered: the expansion's warning, which the group's report has
    /// already withdrawn (PB4469), and the expansion's files.</summary>
    [Fact]
    public void AGroupOutputTheHostRefuses_KeepsTheExpansionsWarningAndFiles()
    {
        WithDirectory("expansion-warning-refused", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(RelationOfLiterals), level: 2023).Success);
            Directory.CreateDirectory(Path.Combine(dir, "CLIENT1.g.cs"));
            var r = Compile(dir, "CLIENT1", Client("CLIENT1"), level: 2023);
            Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, r.Status);
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL3146", StringComparison.Ordinal));
            Assert.EndsWith("[in the expansion 'HOLD-BOX']", warning, StringComparison.Ordinal);
            Assert.Contains(r.OutputFiles, f => Path.GetFileName(f) == "HOLD-BOX.dll");
        });
    }

    [Fact]   // kb/Work PB4469: a check-only compilation runs no sub-compilation, so the group's bind of the text is the one report there is.
    public void ACheckOnlyCompilation_StillReportsAnOwnAssemblyExpansionsWarning_Once()
    {
        WithDirectory("expansion-warning-check-only", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(RelationOfLiterals), level: 2023).Success);
            string src = Path.Combine(dir, "CLIENT1.cob");
            File.WriteAllText(src, Client("CLIENT1"));
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "CLIENT1.dll"), DialectLevel: 2023,
                SourceFormat: InitialReferenceFormat.Free, CheckOnly: true));
            Assert.True(r.Success, Codes(r));
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL3146", StringComparison.Ordinal));
            Assert.StartsWith($"{src}(8,5): warning COBOL3146:", warning, StringComparison.Ordinal);
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
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));   // placement (b): the group's bind is the only one that sees the text
            int entryLine = source.Split('\n').ToList().FindIndex(l => l.Contains("EXPANDS HOLD", StringComparison.Ordinal)) + 1;   // the entry begins at column 5
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL3146", StringComparison.Ordinal));
            Assert.StartsWith($"{Path.Combine(dir, "CLIENT3.cob")}({entryLine},5): warning COBOL3146:", warning, StringComparison.Ordinal);
        });
    }

    /// <summary>A skeleton text that raises two warnings of two kinds at COBOL-2023: COBOL3146 (the BIND's, ISO §8.8.4.2.1: "A relation
    /// condition shall contain at least one reference to an operand that is not a literal") and COBOL0903 (the parse-tree arm of the edition
    /// pass: NEXT SENTENCE is archaic, Annex F.1), so a placement that reports one kind and not the other shows.</summary>
    private const string BindAndEditionWarnings = "IF 1 = 1 NEXT SENTENCE ELSE DISPLAY \"A\".\n        INVOKE ITEM \"SHOW\".";

    /// <summary>Two places of one skeleton text that raise the same words (COBOL3146 twice, ISO §8.8.4.2.1).</summary>
    private const string TwoRelationsOfLiterals = "IF 1 = 1 DISPLAY \"A\".\n        IF 1 = 1 DISPLAY \"B\".\n        INVOKE ITEM \"SHOW\".";

    /// <summary>The client of <see cref="Client"/> with its own class LBOX as the actual: the expansion names a class of the group, so it
    /// lives in the group's assembly (placement (b)).</summary>
    private static string InGroupClient(string id) => Box.Replace("BOX", "LBOX") + Client(id).Replace("CLASS BOX", "CLASS LBOX")
        .Replace("REFERENCE BOX", "REFERENCE LBOX").Replace("INVOKE BOX", "INVOKE LBOX").Replace("USING BOX", "USING LBOX");

    /// <summary>The 1-based line of <paramref name="source"/> that holds the EXPANDS entry (which begins at column 5).</summary>
    private static int EntryLine(string source) =>
        source.Split('\n').ToList().FindIndex(l => l.Contains("EXPANDS HOLD", StringComparison.Ordinal)) + 1;

    private static string[] WithCode(CompilerDriver.Result r, string code) => [.. r.Warnings.Where(w => w.Contains(code, StringComparison.Ordinal))];

    [Theory]   // kb/Work PB4483, ISO §9.3.12: an imported expansion is a class this compilation compiles nothing of; the client that made it reported its text.
    [InlineData("CLIENT1", "CLIENT2")]
    [InlineData("CLIENT2", "CLIENT1")]
    public void AnImportedExpansion_ReportsNoWarningOfItsText_WhicheverClientMadeIt(string first, string second)
    {
        WithDirectory("expansion-warning-imported", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(BindAndEditionWarnings), level: 2023).Success);
            var made = Compile(dir, first, Client(first), level: 2023);
            Assert.True(made.Success, Codes(made));
            foreach (string code in new[] { "COBOL3146", "COBOL0903" })
            {
                string once = Assert.Single(WithCode(made, code));
                Assert.StartsWith($"{Path.Combine(dir, first + ".cob")}(8,5): warning {code}:", once, StringComparison.Ordinal);
                Assert.EndsWith("[in the expansion 'HOLD-BOX']", once, StringComparison.Ordinal);
            }
            var written = File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll"));
            var imported = Compile(dir, second, Client(second, 7), level: 2023);
            Assert.True(imported.Success, Codes(imported));
            Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll")));   // imported, not made again
            Assert.Empty(WithCode(imported, "COBOL3146"));
            Assert.Empty(WithCode(imported, "COBOL0903"));
            var (exit, stdout, stderr) = Run(Path.Combine(dir, second + ".dll"), dir);
            Assert.True(exit == 0, stderr);
            Assert.Equal("BOX 007", stdout);
        });
    }

    [Fact]   // kb/Work PB4483: the text of a repository skeleton expanded in the group's assembly gets the edition pass's parse-tree arm too.
    public void AnExpansionInTheGroupsAssembly_ReportsItsTextsEditionWarning_AtTheEntry_AsItsOwnAssemblyWould()
    {
        WithDirectory("expansion-edition-in-group", dir =>
        {
            Assert.True(Compile(dir, "LIBH", Hold(BindAndEditionWarnings), level: 2023).Success);
            string source = InGroupClient("CLIENT3");
            var r = Compile(dir, "CLIENT3", source, level: 2023);
            Assert.True(r.Success, Codes(r));
            Assert.False(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));   // placement (b)
            string entry = $"{Path.Combine(dir, "CLIENT3.cob")}({EntryLine(source)},5)";
            foreach (string code in new[] { "COBOL3146", "COBOL0903" })
            {
                string once = Assert.Single(WithCode(r, code));
                Assert.StartsWith($"{entry}: warning {code}:", once, StringComparison.Ordinal);
                Assert.EndsWith("[in the expansion 'HOLD-BOX']", once, StringComparison.Ordinal);
            }
        });
    }

    [Fact]   // kb/Work PB4483: two places of a repository text that raise the same words are two reports, in each placement and in a check-only compile.
    public void TwoPlacesOfARepositoryTextRaisingOneWarning_AreTwoReports_InEveryPlacement()
    {
        WithDirectory("expansion-two-places", dir =>
        {
            Assert.True(Compile(dir, "LIBX", Box + Hold(TwoRelationsOfLiterals), level: 2023).Success);
            string src = Path.Combine(dir, "CLIENT1.cob");
            File.WriteAllText(src, Client("CLIENT1"));
            var checkOnly = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "CLIENT1.dll"), DialectLevel: 2023,
                SourceFormat: InitialReferenceFormat.Free, CheckOnly: true));   // the group's bind is the only report
            var ownAssembly = Compile(dir, "CLIENT1", Client("CLIENT1"), level: 2023);   // the expansion's own compilation reports
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
            string apart = Path.Combine(dir, "in-group");   // a search space of its own: one skeleton HOLD in each
            Assert.True(Compile(apart, "LIBH", Hold(TwoRelationsOfLiterals), level: 2023).Success);
            string inGroupSource = InGroupClient("CLIENT3");
            var inGroup = Compile(apart, "CLIENT3", inGroupSource, level: 2023);   // the group's bind compiles the text
            Assert.False(File.Exists(Path.Combine(apart, "HOLD-BOX.dll")));
            foreach (var (r, file, line, name) in new[] { (checkOnly, src, 8, "HOLD-BOX"), (ownAssembly, src, 8, "HOLD-BOX"),
                (inGroup, Path.Combine(apart, "CLIENT3.cob"), EntryLine(inGroupSource), "HOLD-BOX") })
            {
                Assert.True(r.Success, Codes(r));
                var two = WithCode(r, "COBOL3146");
                Assert.Equal(2, two.Length);
                Assert.All(two, w => Assert.StartsWith($"{file}({line},5): warning COBOL3146:", w, StringComparison.Ordinal));
                Assert.All(two, w => Assert.EndsWith($"[in the expansion '{name}']", w, StringComparison.Ordinal));
            }
        });
    }

    /// <summary>A client with its own class LBOX that expands two repository skeletons whose texts declare the same reserved word: HOLD
    /// over LIBX's BOX (placement (a): an assembly of its own, then an import) and KEEP over LBOX (placement (b): the group's assembly),
    /// and carries <paramref name="declaration"/> (a data entry, or none) in its own WORKING-STORAGE.</summary>
    private static string TwoExpansionsClient(string declaration) => Box.Replace("BOX", "LBOX") + $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLIENTW.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS BOX
            CLASS LBOX
            CLASS HOLD
            CLASS KEEP
            CLASS HOLD-BOX EXPANDS HOLD USING BOX
            CLASS KEEP-LBOX EXPANDS KEEP USING LBOX.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 HB USAGE OBJECT REFERENCE HOLD-BOX.
        01 KL USAGE OBJECT REFERENCE KEEP-LBOX.
        {declaration}
        PROCEDURE DIVISION.
            INVOKE HOLD-BOX "NEW" RETURNING HB
            INVOKE KEEP-LBOX "NEW" RETURNING KL
            STOP RUN.
        END PROGRAM CLIENTW.
        """;

    [Theory]   // kb/Work PB4483 review R1-1, ISO §8.9: the once-per-word COBOL0901 belongs to each text; a report the group withdraws hides none.
    [InlineData(false)]
    [InlineData(true)]
    public void AReservedWordOfAWithdrawnExpansionText_DoesNotHideTheSameWordInAnotherText(bool declares) =>
        AssertWordReportedOncePerText(declares, "EDITING", "COBOL0901");

    /// <summary>The removed U+30FB (Annex E.2 item 4; a COBOL-2023 removal, COBOL0902, a warning under <c>--permissive</c>) as the last
    /// character of a word: Annex B.3 item 3 permits it only inside a word.</summary>
    private const string RemovedCharacterWord = "ABC・";

    [Theory]   // kb/Work PB4649, ISO §8.3.2.1 + Annex B.3: the word-character screen walks the text of an expansion of a repository skeleton, per text.
    [InlineData(false)]
    [InlineData(true)]
    public void ARemovedCharacterOfAnExpansionText_IsReportedAtTheEntry_InEveryPlacement_AndOncePerText(bool declares) =>
        AssertWordReportedOncePerText(declares, RemovedCharacterWord, "COBOL0902");

    /// <summary>Two repository skeletons (HOLD and KEEP) whose texts hold the word <paramref name="word"/>, a client expanding HOLD in an
    /// assembly of its own (then imported) and KEEP in the group's assembly, and (when <paramref name="declares"/>) the client declaring the
    /// word too: the diagnostic <paramref name="code"/> of the word is reported once by each text that the compilation answers for, at the
    /// place that text is written (the EXPANDS entry for a repository text), and by no other.</summary>
    private static void AssertWordReportedOncePerText(bool declares, string word, string code)
    {
        WithDirectory("expansion-word-per-text", dir =>
        {
            string hold = Hold().Replace("01 ITEM USAGE OBJECT REFERENCE ELEM.", $"01 ITEM USAGE OBJECT REFERENCE ELEM. 01 {word} PIC X.");
            var library = Compile(dir, "LIBX", Box + hold + "\n" + hold.Replace("HOLD", "KEEP"), level: 2023, permissive: true);
            Assert.True(library.Success, Codes(library));
            string source = TwoExpansionsClient(declares ? $"01 {word} PIC X." : "");
            var lines = source.Split('\n').ToList();
            string file = Path.Combine(dir, "CLIENTW.cob");
            string At(string text) => $"{file}({lines.FindIndex(l => l.Contains(text, StringComparison.Ordinal)) + 1},";
            var apart = Compile(dir, "CLIENTW", source, level: 2023, permissive: true);   // writes HOLD-BOX.dll: its own compilation reports
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
            var written = File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll"));
            var imported = Compile(dir, "CLIENTW", source, level: 2023, permissive: true);   // the same key: HOLD-BOX.dll is imported
            Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll")));
            foreach (var (r, holdReports) in new[] { (apart, 1), (imported, 0) })
            {
                Assert.True(r.Success, Codes(r));
                var reports = WithCode(r, code);
                Assert.All(reports, w => Assert.Contains($"'{word}'", w, StringComparison.Ordinal));
                string keep = Assert.Single(reports, w => w.EndsWith("[in the expansion 'KEEP-LBOX']", StringComparison.Ordinal));
                Assert.StartsWith($"{At("EXPANDS KEEP")}5): warning {code}:", keep, StringComparison.Ordinal);
                var holdBox = reports.Where(w => w.EndsWith("[in the expansion 'HOLD-BOX']", StringComparison.Ordinal)).ToArray();
                Assert.Equal(holdReports, holdBox.Length);
                Assert.All(holdBox, w => Assert.StartsWith($"{At("EXPANDS HOLD")}5): warning {code}:", w, StringComparison.Ordinal));
                var own = reports.Where(w => !w.Contains("[in the expansion", StringComparison.Ordinal)).ToArray();
                Assert.Equal(declares ? 1 : 0, own.Length);
                Assert.All(own, w => Assert.StartsWith(At($"01 {word} PIC X."), w, StringComparison.Ordinal));
                Assert.Equal(1 + holdReports + own.Length, reports.Length);
            }
        });
    }

    [Fact]   // kb/Work PB4483: a skeleton written in the group is the user's own text, reported where it is written, wherever its expansion is placed.
    public void ASkeletonWrittenInTheGroup_IsReportedWhereItIsWritten_WhetherItsExpansionIsWrittenApartOrImported()
    {
        WithDirectory("expansion-own-skeleton", dir =>
        {
            Assert.True(Compile(dir, "LIBB", Box).Success);
            string source = Hold(BindAndEditionWarnings) + "\n" + Client("HCLIENT");
            int ifLine = source.Split('\n').ToList().FindIndex(l => l.Contains("IF 1 = 1", StringComparison.Ordinal)) + 1;
            string at = $"{Path.Combine(dir, "LIBH.cob")}({ifLine},";
            var apart = Compile(dir, "LIBH", source, level: 2023);   // BOX is LIBB's: placement (a) writes HOLD-BOX.dll
            Assert.True(File.Exists(Path.Combine(dir, "HOLD-BOX.dll")));
            var written = File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll"));
            var imported = Compile(dir, "LIBH", source, level: 2023);   // the same key: HOLD-BOX.dll is the class
            Assert.Equal(written, File.GetLastWriteTimeUtc(Path.Combine(dir, "HOLD-BOX.dll")));
            foreach (var r in new[] { apart, imported })
            {
                Assert.True(r.Success, Codes(r));
                Assert.StartsWith($"{at}5): warning COBOL3146:", Assert.Single(WithCode(r, "COBOL3146")), StringComparison.Ordinal);
                Assert.StartsWith($"{at}14): warning COBOL0903:", Assert.Single(WithCode(r, "COBOL0903")), StringComparison.Ordinal);
                Assert.DoesNotContain(r.Warnings, w => w.Contains("[in the expansion", StringComparison.Ordinal));
            }
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
