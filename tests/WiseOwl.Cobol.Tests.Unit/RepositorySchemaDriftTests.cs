// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Runtime.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPOSITORY SCHEMA HAS ONE WRITER AND ONE READER, AND A READER THAT LOADS NOTHING CAN DECODE IT
/// (DESIGN-external-repository §6, §6.2, §7.4, §15.1, §17.2, §17.3; kb/Work PB2100). Three facts a change to the schema could
/// break silently:
/// <list type="bullet">
/// <item>every attribute of <c>WiseOwl.Cobol.Runtime.Repository</c> is applied <c>sealed</c> and not inherited, and every
/// property is a primitive, a <c>string</c>, an <c>int</c>-backed enum or a one-dimensional array of those — never a
/// <c>System.Type</c> (ECMA-335 §II.23.3; a reader decodes it from metadata without loading the assembly, §6);</item>
/// <item>every settable property of every attribute is written by <c>RepositoryAttributeRenderer</c> and read by its reader
/// (<c>RepositoryReader</c>, or <see cref="RepositoryIndex"/> for the assembly record) — a property only one half names is a
/// fact one side drops (§6.2 "the ONE place attribute syntax is written … and the reader (the ONE decoder) are their two
/// halves");</item>
/// <item>repository metadata is read only through the two halves of §7.4: no source file but <c>RepositoryIndex.cs</c> and
/// <c>RepositoryReader.cs</c> names <c>PEReader</c> or <c>MetadataReader</c> (read as Roslyn tokens, so a comment is not a
/// use; §17.3's read-through test, with the halves slice 4 has; slices 5 and 7 add their callers).</item>
/// </list>
/// </summary>
public sealed class RepositorySchemaDriftTests
{
    private static readonly Type[] Attributes = [.. typeof(CobolRepositoryAttribute).Assembly.GetTypes()
        .Where(t => t.Namespace == typeof(CobolRepositoryAttribute).Namespace && typeof(Attribute).IsAssignableFrom(t))
        .OrderBy(t => t.Name, StringComparer.Ordinal)];

    [Fact]
    public void EverySchemaAttribute_IsSealed_NotInherited_AndTypedForAReaderThatLoadsNothing()
    {
        Assert.True(Attributes.Length >= 10, $"only {Attributes.Length} schema attributes were found — the scan measured nothing");
        // The two abstract classes state the description fields and the formal fields once; neither is applied.
        Assert.Equal([typeof(CobolDescriptionFieldsAttribute), typeof(CobolFormalFieldsAttribute)], Attributes.Where(t => t.IsAbstract));
        foreach (var attribute in Attributes.Where(t => !t.IsAbstract))
        {
            Assert.True(attribute.IsSealed, $"{attribute.Name} is not sealed");
            var usage = attribute.GetCustomAttribute<AttributeUsageAttribute>(inherit: false);
            Assert.True(usage is { Inherited: false }, $"{attribute.Name} does not declare Inherited = false");
            Assert.Single(attribute.GetConstructors());
            foreach (var property in attribute.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name != nameof(Attribute.TypeId)))
                Assert.True(Encodable(property.PropertyType), $"{attribute.Name}.{property.Name} is a {property.PropertyType.Name}, which a custom-attribute argument cannot hold for a reader that loads nothing");
        }

        static bool Encodable(Type t) =>
            t.IsArray ? t.GetArrayRank() == 1 && Element(t.GetElementType()!) : Element(t);

        static bool Element(Type t) =>
            t.IsEnum ? Enum.GetUnderlyingType(t) == typeof(int)
                : t.IsPrimitive && t != typeof(IntPtr) && t != typeof(UIntPtr) || t == typeof(string);
    }

    [Fact]
    public void EverySettableProperty_IsWrittenByTheRenderer_AndReadByItsReader()
    {
        string renderer = NamesOf(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "RepositoryAttributeRenderer.cs"));
        string reader = NamesOf(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Repository", "RepositoryReader.cs"));
        string index = NamesOf(TestRepo.Src("WiseOwl.Cobol.Runtime", "Repository", "RepositoryIndex.cs"));
        var missing = new List<string>();
        int checkedProperties = 0;
        foreach (var attribute in Attributes.Where(t => !t.IsAbstract))
            foreach (var property in attribute.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
            {
                checkedProperties++;
                // The property is named through the attribute or the class that declares it (the description fields' base).
                string[] spellings = [$"{attribute.Name}.{property.Name}", $"{property.DeclaringType!.Name}.{property.Name}"];
                if (!spellings.Any(s => renderer.Contains(s, StringComparison.Ordinal)))
                    missing.Add($"{attribute.Name}.{property.Name}: RepositoryAttributeRenderer never writes it");
                string decoder = attribute == typeof(CobolRepositoryAttribute) ? index : reader;
                if (!spellings.Any(s => decoder.Contains(s, StringComparison.Ordinal)))
                    missing.Add($"{attribute.Name}.{property.Name}: {(attribute == typeof(CobolRepositoryAttribute) ? "RepositoryIndex" : "RepositoryReader")} never reads it");
            }
        Assert.True(checkedProperties >= 80, $"only {checkedProperties} settable properties were checked — the scan measured nothing");
        Assert.True(missing.Count == 0, string.Join("\n", missing));

        // Every nameof(X.P) of the file, as "X.P" lines: what the file names, never what its comments say.
        static string NamesOf(string path) => string.Join("\n",
            Regex.Matches(File.ReadAllText(path), @"nameof\((\w+)\.(\w+)\)").Select(m => $"{m.Groups[1].Value}.{m.Groups[2].Value}"));
    }

    [Fact]
    public void RepositoryMetadata_IsReadOnlyThroughTheTwoHalves()
    {
        string[] halves = ["RepositoryIndex.cs", "RepositoryReader.cs"];
        var readers = new List<string>();
        int files = 0;
        foreach (string path in Directory.EnumerateFiles(TestRepo.At("src"), "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                         && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            files++;
            var tokens = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText is "PEReader" or "MetadataReader");
            if (tokens.Any()) readers.Add(Path.GetRelativePath(TestRepo.Root, path).Replace('\\', '/'));
        }
        Assert.True(files > 500, $"only {files} source files were scanned — the scan measured nothing");
        Assert.Equal(halves, readers.Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }
}
