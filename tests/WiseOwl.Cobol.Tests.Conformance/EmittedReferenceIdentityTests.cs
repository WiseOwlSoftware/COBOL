// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB4047 — an emitted assembly is compiled against the framework's REFERENCE assemblies. An ordinary MSBuild
/// .NET host project compiles against the targeting pack, so an emitted assembly that referenced an implementation
/// assembly (<c>System.Private.CoreLib</c>, which the compiler's own process runs on) failed that host with CS0012 the
/// moment it used one of the assembly's types (DOC-A.1-141 names a .NET host as the activating element; the generated
/// public class is its one interface). The host tests compile their C# host against exactly that reference set
/// (<see cref="NonCobolElement.CompileCSharp"/>); this test pins the cause directly, on the emitted metadata.
/// </summary>
public sealed class EmittedReferenceIdentityTests
{
    private const string Program = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4047REF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY W
           GOBACK.
       """;

    [Fact]
    public void AnEmittedAssembly_ReferencesNoImplementationAssembly_AndTheFrameworkAsSystemRuntime()
    {
        string dir = ScratchDirectory.Create("pb4047ref");
        try
        {
            NonCobolElement.CompileCobol(dir, "PB4047REF", Program);
            using var stream = File.OpenRead(Path.Combine(dir, "PB4047REF.dll"));
            using var pe = new PEReader(stream);
            var md = pe.GetMetadataReader();
            var names = md.AssemblyReferences.Select(h => md.GetString(md.GetAssemblyReference(h).Name)).ToList();
            Assert.DoesNotContain(names, n => n.StartsWith("System.Private.", StringComparison.Ordinal));
            Assert.Contains("System.Runtime", names);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
