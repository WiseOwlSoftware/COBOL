// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB2101, slice 5 of the §8.13 external repository (docs/rearchitecture/DESIGN-external-repository.md §11.2, §17.1
/// "Run-time"): EVERY fault on the way to an activation is one of the CALL's own conditions. The faults here are not COBOL
/// programs (a module whose registrar throws, a registered program whose dependency assembly is missing, a class whose static
/// initializer throws, a module of an older schema), so they are C# fixtures, built with Roslyn like
/// <c>NonCobolElement</c> builds every non-COBOL element, beside a COBOL main that CALLs them; the cases that ARE COBOL programs
/// are the suite's multi-unit cases (conformance-suite/wiseowl/cases/8.13-...).
/// <para>ISO/IEC 1989:2023 §14.9.4.4 GR3 b) (cite.py OK, located under 3)): "If the program cannot be located or identifier-1
/// references a zero-length item, the EC-PROGRAM-NOT-FOUND exception condition is set to exist"; GR3 c): "If the program is located
/// but the resources necessary to execute the program are not available, the EC-PROGRAM-RESOURCES exception condition is set to
/// exist" (OK, 3) c)), the resources the implementor's (Annex A.1 item 14: docs/CONFORMANCE.md DOC-A.1-14 takes GnuCOBOL's answer: a
/// module that cannot be loaded is not located).</para>
/// </summary>
public sealed class RunTimeLocateFaultTests
{
    private static readonly string Version = RuntimeAbi.Version.ToString();

    /// <summary>A COBOL main that CALLs <paramref name="callee"/> with EC-PROGRAM-NOT-FOUND checking on, so the miss is a
    /// fatal condition whose message the run unit's abnormal ending prints.</summary>
    private static string CallerOf(string id, string callee) => $"""
        >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        PROCEDURE DIVISION.
            DISPLAY "BEFORE"
            CALL "{callee}"
            DISPLAY "AFTER"
            STOP RUN.
        END PROGRAM {id}.
        """;

    /// <summary>A hand-built module as the compiler emits one: the assembly record (this runtime's version and call ABI unless
    /// <paramref name="schema"/> or <paramref name="version"/> says another), its <c>Units</c> index naming
    /// <paramref name="unit"/>, and the registration member <c>EnsureRegistered()</c> over <c>RegisterModule</c> with
    /// <paramref name="register"/> as its registrar body. <paramref name="members"/> are further members of the type
    /// <c>Cobol.&lt;ns&gt;.Fixture</c>.</summary>
    private static string Module(string ns, string unit, string register, string members = "",
        int schema = CobolRepositoryAttribute.CurrentSchemaVersion, string? version = null)
    {
        string row = RepositoryIndex.EncodeUnit(RepositoryUnitKind.Program, unit, unit);
        return $$"""
            [assembly: WiseOwl.Cobol.Runtime.Repository.CobolRepository({{schema}}, "{{version ?? Version}}", {{RuntimeAbi.CallAbi}}, Registrar = "Cobol.{{ns}}.__CobolModule", Units = new string[] { "{{row}}" })]
            namespace Cobol.{{ns}}
            {
                public static class __CobolModule
                {
                    public static void EnsureRegistered() =>
                        WiseOwl.Cobol.Runtime.ProgramRegistry.RegisterModule("Cobol.{{ns}}.__CobolModule", "{{version ?? Version}}", {{RuntimeAbi.CallAbi}}, Register);
                    private static void Register()
                    {
                        {{register}}
                    }
                }
                {{members}}
            }
            """;
    }

    /// <summary>Register the program <paramref name="unit"/> whose instance is made by <c>new Fixture()</c>.</summary>
    private static string RegisterFixture(string unit) =>
        $"WiseOwl.Cobol.Runtime.ProgramRegistry.Register(WiseOwl.Cobol.Runtime.ProgramTable.OutermostPath(\"{unit}\"), \"{unit}\", null, false, false, false, _ => new Fixture());";

    /// <summary>The program class: an <c>ICobolProgram</c> whose members do nothing. <paramref name="body"/> is the class body's
    /// first lines (a field or a static initializer).</summary>
    private static string ProgramClass(string body) => $$"""
        public sealed class Fixture : WiseOwl.Cobol.Runtime.ICobolProgram
        {
            {{body}}
            public void Call(WiseOwl.Cobol.Runtime.CobolArg[] args, WiseOwl.Cobol.Runtime.CobolArg? returning) { }
            public void Activate() { }
            public void CloseFiles() { }
        }
        """;

    private static (int exit, string stdout, string detail) RunMain(string dir, string main, string callee)
    {
        NonCobolElement.CompileCobol(dir, main, CallerOf(main, callee));
        return CutRunner.RunExit(Path.Combine(dir, main + ".dll"), dir);
    }

    /// <summary>A module whose registrar throws is not located (DESIGN-external-repository §11.2): EC-PROGRAM-NOT-FOUND naming
    /// the module and the fault, through the probe. It reaches the registrar: the locator found the unit by the module's own
    /// index, so the reason is the registrar's, not a miss.</summary>
    [Fact]
    public void ARegistrarThatThrows_ThroughTheProbe_IsProgramNotFoundNamingTheFault()
    {
        string dir = ScratchDirectory.Create("pb2101reg");
        try
        {
            NonCobolElement.CompileCSharp(dir, "P5398R", Module("P5398R", "P5398R", "throw new System.InvalidOperationException(\"the fixture registrar fails\");"), runtimeConfigOf: null);
            var (exit, stdout, detail) = RunMain(dir, "P5398RM", "P5398R");
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("Cobol.P5398R.__CobolModule", detail, StringComparison.Ordinal);
            Assert.Contains("the fixture registrar fails", detail, StringComparison.Ordinal);
            Assert.DoesNotContain("EC-PROGRAM-RESOURCES", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The same registrar fault reached through a host's own <c>EnsureRegistered()</c>, the member a host composing a
    /// run unit calls (§11.4): before slice 5 the registrar's exception escaped as itself; it is now the CALL's condition, a
    /// <see cref="CobolCallException"/> with EC-PROGRAM-NOT-FOUND that the host handles and continues after.</summary>
    [Fact]
    public void ARegistrarThatThrows_ThroughAHostsEnsureRegistered_IsAProgramNotFoundCondition()
    {
        const string host = """
            using System;

            public static class P5398Host
            {
                public static int Main()
                {
                    WiseOwl.Cobol.Runtime.RunUnit.Run(_ =>
                    {
                        try { Cobol.P5398H.__CobolModule.EnsureRegistered(); Console.WriteLine("REGISTERED"); }
                        catch (WiseOwl.Cobol.Runtime.CobolCallException e) { Console.WriteLine(e.EcName + ": " + e.Message); }
                    });
                    Console.WriteLine("HOST DONE");
                    return 0;
                }
            }
            """;
        string dir = ScratchDirectory.Create("pb2101host");
        try
        {
            NonCobolElement.CompileCobol(dir, "P5398HM", CallerOf("P5398HM", "P5398HX"));
            NonCobolElement.CompileCSharp(dir, "P5398H", Module("P5398H", "P5398H", "throw new System.InvalidOperationException(\"the host's registrar fails\");"), runtimeConfigOf: null);
            NonCobolElement.CompileCSharp(dir, "P5398HOST", host, "P5398HM", "P5398H");
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P5398HOST.dll"), dir);
            Assert.Equal(0, exit);
            Assert.Contains("EC-PROGRAM-NOT-FOUND: module Cobol.P5398H.__CobolModule cannot run in this run unit", stdout, StringComparison.Ordinal);
            Assert.Contains("the host's registrar fails", stdout, StringComparison.Ordinal);
            Assert.EndsWith("HOST DONE", stdout.TrimEnd(), StringComparison.Ordinal);
            Assert.DoesNotContain("REGISTERED", stdout, StringComparison.Ordinal);
            _ = detail;
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A REGISTERED program whose dependency assembly is missing is not located either: the CLR loads the program
    /// class and what it uses lazily, so the same missing assembly fails inside the registrar or at the instance creation
    /// depending on when it is first used, and the condition must not depend on that (DESIGN-external-repository §11.2).</summary>
    [Fact]
    public void ARegisteredProgramWhoseDependencyIsMissing_IsProgramNotFoundWithTheLoadersMessage()
    {
        const string dependency = "namespace P5398Dep { public sealed class Needed { public int Value => 1; } }";
        string dir = ScratchDirectory.Create("pb2101dep");
        try
        {
            NonCobolElement.CompileCSharp(dir, "P5398Dep", dependency, runtimeConfigOf: null);
            NonCobolElement.CompileCSharp(dir, "P5398D", Module("P5398D", "P5398D", RegisterFixture("P5398D"),
                ProgramClass("private readonly P5398Dep.Needed _needed = new();")), runtimeConfigOf: null, "P5398Dep");
            File.Delete(Path.Combine(dir, "P5398Dep.dll"));   // deployed without its dependency
            var (exit, stdout, detail) = RunMain(dir, "P5398DM", "P5398D");
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("could not be loaded", detail, StringComparison.Ordinal);
            Assert.Contains("P5398Dep", detail, StringComparison.Ordinal);   // the loader's own words name the missing assembly
            Assert.DoesNotContain("EC-PROGRAM-RESOURCES", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A registered program whose class's static initializer throws: the CLR remembers the failure, so it is the
    /// same condition at every activation: EC-PROGRAM-NOT-FOUND with the initializer's own message.</summary>
    [Fact]
    public void AProgramWhoseStaticInitializerThrows_IsProgramNotFound()
    {
        string dir = ScratchDirectory.Create("pb2101cctor");
        try
        {
            NonCobolElement.CompileCSharp(dir, "P5398S", Module("P5398S", "P5398S", RegisterFixture("P5398S"),
                ProgramClass("static Fixture() => throw new System.InvalidOperationException(\"the static initializer fails\");")), runtimeConfigOf: null);
            var (exit, stdout, detail) = RunMain(dir, "P5398SM", "P5398S");
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("the static initializer fails", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A module of the previous schema beside the caller (version 1 has no <c>Units</c>): EC-PROGRAM-NOT-FOUND
    /// saying so (§6.6: a module of another schema is no answer to the probe; recompiling is the cure).</summary>
    [Fact]
    public void AModuleOfAnotherSchema_IsProgramNotFoundWithTheSchemaVersion()
    {
        string dir = ScratchDirectory.Create("pb2101schema");
        try
        {
            NonCobolElement.CompileCSharp(dir, "P5398V", Module("P5398V", "P5398V", RegisterFixture("P5398V"),
                ProgramClass(""), schema: 1), runtimeConfigOf: null);
            var (exit, stdout, detail) = RunMain(dir, "P5398VM", "P5398V");
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("schema v1", detail, StringComparison.Ordinal);
            Assert.Contains("recompile", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>A file named after the program that is no managed assembly at all (a truncated or foreign image) is not
    /// located: its reason is in the message (DOC-A.1-14).</summary>
    [Fact]
    public void AFileNamedAfterTheProgramThatIsNotAnAssembly_IsProgramNotFoundWithAReason()
    {
        string dir = ScratchDirectory.Create("pb2101foreign");
        try
        {
            NonCobolElement.CompileCobol(dir, "P5398FM", CallerOf("P5398FM", "P5398F"));
            File.WriteAllText(Path.Combine(dir, "P5398F.dll"), "this is not an assembly");
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P5398FM.dll"), dir);
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("not a managed assembly", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The feature switch off (§15.5): nothing is loaded and the reason says why. An application publishing trimmed
    /// or ahead of time sets <c>WiseOwl.Cobol.Runtime.DynamicProbe</c> to false in its runtime configuration, which this
    /// application's does too: its module is beside it, and is NOT located.</summary>
    [Fact]
    public void WithTheDynamicProbeSwitchedOff_ASiblingModuleIsNotLoaded_AndTheReasonSaysSo()
    {
        string dir = ScratchDirectory.Create("pb2101off");
        try
        {
            NonCobolElement.CompileCobol(dir, "P5398OM", CallerOf("P5398OM", "P5398O"));
            NonCobolElement.CompileCobol(dir, "P5398O", """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. P5398O.
                PROCEDURE DIVISION.
                    DISPLAY "SIBLING RAN"
                    GOBACK.
                END PROGRAM P5398O.
                """);
            string config = Path.Combine(dir, "P5398OM.runtimeconfig.json");
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(config))!;
            var options = json["runtimeOptions"]!.AsObject();
            var properties = options["configProperties"]?.AsObject() ?? new System.Text.Json.Nodes.JsonObject();
            properties["WiseOwl.Cobol.Runtime.DynamicProbe"] = false;
            options["configProperties"] = properties;
            File.WriteAllText(config, json.ToJsonString());
            var (exit, stdout, detail) = CutRunner.RunExit(Path.Combine(dir, "P5398OM.dll"), dir);
            Assert.NotEqual(0, exit);
            Assert.Equal("BEFORE", stdout);
            Assert.Contains("EC-PROGRAM-NOT-FOUND", detail, StringComparison.Ordinal);
            Assert.Contains("the dynamic probe is disabled", detail, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
