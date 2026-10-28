// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The module's repository header and unit index, read WITHOUT loading the assembly (DESIGN-external-repository §5.4 item 3,
/// §7.4, §15.1, §17.2; kb/Work PB2100): <see cref="RepositoryIndex"/> reads what the compiler's
/// <c>RepositoryAttributeRenderer</c> wrote. The index equals the record set — every <c>[CobolUnit]</c> on a host entry is a
/// row of <c>Units</c> and every row has its entry — and the unit-row encoding round-trips whatever an externalized name
/// holds, because the three fields of a row split on <c>:</c> and a name may contain it (an AS literal's content,
/// <c>ExternalizedName.Screen</c>).
/// </summary>
public sealed class RepositoryIndexTests : WiseOwlCobolTestBase
{
    // A prototype beside its definition (one unit, the definition's), a main program, and a function.
    private const string Group = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. psub IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 A PIC 9(4).
               PROCEDURE DIVISION USING A.
               END PROGRAM psub.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PMAIN.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY. PROGRAM psub.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 W PIC 9(4) VALUE 7.
               PROCEDURE DIVISION.
                   CALL psub USING W.
                   STOP RUN.
               END PROGRAM PMAIN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PSUB.
               DATA DIVISION.
               LINKAGE SECTION.
               01 B PIC 9(4).
               PROCEDURE DIVISION USING B.
                   DISPLAY "PSUB " B
                   GOBACK.
               END PROGRAM PSUB.
               IDENTIFICATION DIVISION.
               FUNCTION-ID. FTWICE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 N PIC 9(4).
               01 R PIC 9(4).
               PROCEDURE DIVISION USING N RETURNING R.
                   COMPUTE R = N * 2
                   GOBACK.
               END FUNCTION FTWICE.
               """;

    [Fact]
    public void ACompiledModule_CarriesItsHeaderAndUnitIndex_ReadWithoutLoadingIt_AndTheIndexEqualsTheRecordSet()
    {
        var (ok, stdout, detail) = CompileAndRun(Group, dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("PSUB 0007", stdout);
        string dll = Path.Combine(TempDir, "prog.dll");

        var outcome = RepositoryIndex.Open(dll);
        Assert.Equal(RepositoryReadFailure.None, outcome.Failure);
        using RepositoryIndex index = outcome.Index!;
        Assert.Equal(CobolRepositoryAttribute.CurrentSchemaVersion, index.SchemaVersion);
        Assert.Equal(RuntimeAbi.Version.ToString(), index.RuntimeVersion);
        Assert.Equal(RuntimeAbi.CallAbi, index.CallAbi);
        Assert.Equal("Cobol.prog.__CobolModule", index.Registrar);
        Assert.Equal(2023, index.Edition);
        Assert.False(string.IsNullOrEmpty(index.CompilerVersion));
        Assert.True(index.HasSignatures);   // --repository-update on, the default (DESIGN-external-repository §9.1)
        Assert.Contains("WiseOwl.Cobol.Runtime", index.AssemblyReferences);

        // The prototype psub is not a unit of its own: its definition PSUB publishes the one entry (§7.2, kb/Work PB3872).
        Assert.Equal(
            [new RepositoryUnitRow(RepositoryUnitKind.Program, "PMAIN", "PMAIN"),
             new RepositoryUnitRow(RepositoryUnitKind.Program, "PSUB", "PSUB"),
             new RepositoryUnitRow(RepositoryUnitKind.Function, "FTWICE", "FTWICE")],
            index.Units);

        // The record set: the [CobolUnit] of every type, read by reflection from the bytes (the file stays unlocked).
        Assembly loaded = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(dll)));
        var records = loaded.GetTypes()
            .Select(t => t.GetCustomAttribute<CobolUnitAttribute>())
            .OfType<CobolUnitAttribute>()
            .Select(u => new RepositoryUnitRow(u.Kind, u.ExternalizedName, u.Name))
            .ToList();
        Assert.Equal(index.Units.OrderBy(r => r.ExternalizedName, StringComparer.Ordinal),
            records.OrderBy(r => r.ExternalizedName, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(RepositoryUnitKind.Program, "PROG", "PROG")]
    [InlineData(RepositoryUnitKind.FunctionPrototype, "a:b", "word")]
    [InlineData(RepositoryUnitKind.Class, "back\\slash", "colon:word")]
    [InlineData(RepositoryUnitKind.Interface, "\\:\\\\:", ":\\")]
    [InlineData(RepositoryUnitKind.ParameterizedClass, "", "")]
    [InlineData(RepositoryUnitKind.ClrMethod, "x y  z", "über")]
    public void AUnitRow_RoundTrips_WhateverTheNamesHold(RepositoryUnitKind kind, string externalizedName, string word)
    {
        string row = RepositoryIndex.EncodeUnit(kind, externalizedName, word);
        Assert.True(RepositoryIndex.TryDecodeUnit(row, out RepositoryUnitRow decoded), row);
        Assert.Equal(new RepositoryUnitRow(kind, externalizedName, word), decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x:a:b")]            // the kind is not a number
    [InlineData("99:a:b")]           // a kind this schema does not define
    [InlineData("-1:a:b")]
    [InlineData("0:a")]              // one field short
    [InlineData("0:a:b:c")]          // one field too many: an unescaped colon
    [InlineData("0:a\\:b")]          // the escaped colon is part of the one name
    [InlineData("0:a:b\\")]          // a dangling escape
    [InlineData("0:a:b\\x")]         // an unknown escape
    public void ARowTheSchemaDoesNotWrite_IsRefused(string row) =>
        Assert.False(RepositoryIndex.TryDecodeUnit(row, out _), row);

    [Fact]
    public void WhatIsNotARepositoryEntry_IsATypedOutcome_NeverAnException()
    {
        using var notAnImage = new MemoryStream([1, 2, 3, 4]);
        Assert.Equal(RepositoryReadFailure.NotManagedAssembly, RepositoryIndex.Open(notAnImage).Failure);

        // A managed assembly that carries no repository record is not an entry.
        var plain = Compile("public static class Plain { }");
        Assert.Equal(RepositoryReadFailure.NoRepositoryRecord, RepositoryIndex.Open(new MemoryStream(plain)).Failure);
    }

    /// <summary>§7.5 and §15.1: the entry is read into memory and the file closed, so the next build can replace it while a
    /// compilation still holds the index. The copy is deleted (Windows refuses while any handle is open) and the metadata is
    /// still readable.</summary>
    [Fact]
    public void TheIndex_HoldsNoHandleOnTheFile_AndItsMetadataStaysReadable()
    {
        string copy = Path.Combine(TempDir, "entry.dll");
        File.WriteAllBytes(copy, Compile("[assembly: WiseOwl.Cobol.Runtime.Repository.CobolRepository(2, \"1.0.0.0\", 1, Registrar = \"R\")]"));
        using RepositoryIndex index = RepositoryIndex.Open(copy).Index!;
        File.Delete(copy);
        Assert.False(File.Exists(copy));
        Assert.Equal("repository_index_probe", index.Metadata.GetString(index.Metadata.GetAssemblyDefinition().Name));
        Assert.Equal("R", index.Registrar);
        Assert.Empty(index.Units);   // an absent Units argument lists nothing
    }

    [Fact]
    public void AnIndexRowTheReaderCannotDecode_IsMalformed_AndTooManyUnitsIsCapped()
    {
        string header = $"[assembly: WiseOwl.Cobol.Runtime.Repository.CobolRepository(2, \"1.0.0.0\", 1, Units = new string[] {{ ";
        var bad = RepositoryIndex.Open(new MemoryStream(Compile(header + "\"not a row\" })]")));
        Assert.Equal(RepositoryReadFailure.Malformed, bad.Failure);
        Assert.Contains("not a row", bad.Detail, StringComparison.Ordinal);

        string rows(int n) => string.Join(", ", Enumerable.Range(0, n).Select(i => $"\"0:P{i}:P{i}\""));
        using var atCap = RepositoryIndex.Open(new MemoryStream(Compile(header + rows(RepositoryIndex.MaxUnits) + " })]"))).Index!;
        Assert.Equal(RepositoryIndex.MaxUnits, atCap.Units.Count);
        var over = RepositoryIndex.Open(new MemoryStream(Compile(header + rows(RepositoryIndex.MaxUnits + 1) + " })]")));
        Assert.Equal(RepositoryReadFailure.TooManyUnits, over.Failure);
    }

    /// <summary>A library of <paramref name="source"/> that references the runtime, as bytes.</summary>
    internal static byte[] Compile(string source)
    {
        string frameworkDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(Path.GetDirectoryName(p), frameworkDir, StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(CobolRepositoryAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create("repository_index_probe", [CSharpSyntaxTree.ParseText(source)], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        Assert.True(emitted.Success, string.Join("; ", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return image.ToArray();
    }
}
