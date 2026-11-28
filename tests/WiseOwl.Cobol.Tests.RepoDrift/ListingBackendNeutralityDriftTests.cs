// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE LISTING IS BACKEND-NEUTRAL (DESIGN-listing.md section 6.9; the owner, 2026-11-18: "It also makes no sense to link to C# code
/// when we switch to the IL generation"). No section, renderer, link or schema field names, reads or points into generated source or any
/// Roslyn type: the object-code map is read from what every backend produces, the assembly and its portable PDB. Held here by structure:
/// <list type="number">
/// <item>the built <c>WiseOwl.Cobol.Listing</c> assembly (the model and its renderers) references no <c>Microsoft.CodeAnalysis</c>
/// assembly;</item>
/// <item>every symbol the documents of the builder's namespace (<c>WiseOwl.Cobol.Compiler.Listing</c>, in the Compiler) name is bound by
/// Roslyn's semantic model, and none is declared in a <c>Microsoft.CodeAnalysis</c> assembly or in the backend's namespace
/// (<c>WiseOwl.Cobol.Compiler.CodeGen</c>);</item>
/// <item>no document of either names a <c>.g.cs</c> file.</item>
/// </list>
/// Each rule is red on its planted counterexample (<see cref="TheChecks_SeeACounterexample"/>).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ListingBackendNeutralityDriftTests
{
    private const string BuilderNamespace = "WiseOwl.Cobol.Compiler.Listing";
    private static readonly Regex GeneratedSourceName = new(@"\.g\.cs\b", RegexOptions.CultureInvariant);

    [Fact]
    public void TheListingAssembly_ReferencesNoRoslynAssembly()
    {
        string listing = Path.Combine(AppContext.BaseDirectory, "WiseOwl.Cobol.Listing.dll");
        using var pe = new PEReader(File.OpenRead(listing));
        var metadata = pe.GetMetadataReader();
        var references = metadata.AssemblyReferences.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name)).ToList();
        Assert.Contains("System.Runtime", references);   // the read saw the references
        Assert.DoesNotContain(references, r => r.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBuilder_NamesNoRoslynOrBackendSymbol_AndNoGeneratedSource()
    {
        var documents = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains($"namespace {BuilderNamespace};", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(documents, d => d.EndsWith("ObjectCodeMapReader.cs", StringComparison.Ordinal));
        Assert.Empty(Offences(documents));
        foreach (string listing in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Listing"), "*.cs", SearchOption.AllDirectories))
            Assert.DoesNotMatch(GeneratedSourceName, File.ReadAllText(listing));
    }

    /// <summary>A builder document that names a Roslyn type, one that names a backend type and one that names a <c>.g.cs</c> file are each
    /// reported.</summary>
    [Fact]
    public void TheChecks_SeeACounterexample()
    {
        string dir = ScratchDirectory.Create("listing-neutrality");
        try
        {
            string roslyn = Path.Combine(dir, "Roslyn.cs"), backend = Path.Combine(dir, "Backend.cs"), generated = Path.Combine(dir, "Generated.cs");
            File.WriteAllText(roslyn, $"namespace {BuilderNamespace};\ninternal static class PlantedA {{ public static object Of(string s) => Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseStatement(s); }}\n");
            File.WriteAllText(backend, $"namespace {BuilderNamespace};\ninternal static class PlantedB {{ public static object Of() => new WiseOwl.Cobol.Compiler.CodeGen.CodeWriter(); }}\n");
            File.WriteAllText(generated, $"namespace {BuilderNamespace};\ninternal static class PlantedC {{ public const string Path = \"out/prog.g.cs\"; }}\n");
            var offences = Offences([roslyn, backend, generated]);
            Assert.Contains(offences, o => o.Contains("Roslyn.cs", StringComparison.Ordinal) && o.Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal));
            Assert.Contains(offences, o => o.Contains("Backend.cs", StringComparison.Ordinal) && o.Contains("WiseOwl.Cobol.Compiler.CodeGen", StringComparison.Ordinal));
            Assert.Contains(offences, o => o.Contains("Generated.cs", StringComparison.Ordinal) && o.Contains(".g.cs", StringComparison.Ordinal));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>Each symbol <paramref name="documents"/> name that a Roslyn assembly or the backend's namespace declares, and each
    /// <c>.g.cs</c> they name. The documents are compiled against the built product (as the test assembly the Compiler grants its
    /// internals to, so every internal type resolves); a document that does not bind is itself an offence, since an unbound name
    /// could be anything.</summary>
    private static List<string> Offences(IReadOnlyList<string> documents)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var trees = documents.Select(d => CSharpSyntaxTree.ParseText(File.ReadAllText(d), options, path: d)).ToList();
        // The project's implicit global usings, as the SDK wrote them for the build the test runs against (any configuration).
        string globalUsings = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "obj"), "WiseOwl.Cobol.Compiler.GlobalUsings.g.cs",
            SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).First();
        var usings = CSharpSyntaxTree.ParseText(File.ReadAllText(globalUsings), options, path: globalUsings);
        string platform = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var references = platform.Split(Path.PathSeparator)
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .Where(IsManagedAssembly)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        var compilation = CSharpCompilation.Create(typeof(ListingBackendNeutralityDriftTests).Assembly.GetName().Name, [.. trees, usings], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable,
                metadataImportOptions: MetadataImportOptions.All));
        var offences = new List<string>();
        foreach (var diagnostic in compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
            offences.Add($"{diagnostic.Location.GetLineSpan()}: does not bind: {diagnostic.GetMessage()}");
        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (model.GetSymbolInfo(node).Symbol is not { } symbol) continue;
                string? assembly = symbol.ContainingAssembly?.Name;
                string ns = (symbol as INamespaceSymbol ?? symbol.ContainingNamespace)?.ToDisplayString() ?? "";
                if (assembly?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true
                    || ns == "WiseOwl.Cobol.Compiler.CodeGen" || ns.StartsWith("WiseOwl.Cobol.Compiler.CodeGen.", StringComparison.Ordinal))
                    offences.Add($"{tree.FilePath}({node.GetLocation().GetLineSpan().StartLinePosition}): names {symbol.ToDisplayString()} of {assembly} ({ns})");
            }
            foreach (Match name in GeneratedSourceName.Matches(tree.GetText().ToString()))
                offences.Add($"{tree.FilePath}: names a .g.cs file at {name.Index}");
        }
        return offences;
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var pe = new PEReader(File.OpenRead(path));
            return pe.HasMetadata;
        }
        catch (BadImageFormatException) { return false; }
    }
}
