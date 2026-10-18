// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO STATEMENT READS OR WRITES THE WHOLE FILE: THE KEYED STORE IS READ AND WRITTEN ONE PAGE AT A TIME, IN ONE PLACE
/// (DESIGN-runtime-library.md 2.2.1.11 witness (d) and 2.2.1.13, in docs/rearchitecture; kb/Work PB3330, PB3335). The
/// owner's bar for the keyed store engine (PB3330): <i>"no statement reads or writes the whole file"</i>. Before the
/// engine a keyed connector loaded the whole physical file into memory at OPEN and wrote it back whole at CLOSE (and,
/// for a file another run unit could write, at every statement), so a statement on a large shared file cost O(file).
/// The shape that made that possible is what this test bans, measured three ways:
/// <list type="bullet">
///   <item>Positional host I/O (<c>RandomAccess.Read</c>/<c>Write</c>/<c>GetLength</c>/<c>SetLength</c>/<c>FlushToDisk</c>)
///   appears under <c>Runtime/IO</c> ONLY in <c>Store/</c>'s page medium, and every read and write there moves exactly one
///   page (<c>PageHeader.PageSize</c> bytes).</item>
///   <item>No buffer under <c>Runtime/IO</c> is rented from the shared array pool at a size computed from a length — the
///   shape the whole-store read (<c>ReadImage</c>) had.</item>
///   <item>The whole-store model's members (2.2.1.13's deletion list) are declared nowhere under <c>Runtime/IO</c>: its
///   codec, its boundary arithmetic, its load and persist, its generation probe and its in-memory record table.</item>
/// </list>
/// </summary>
public sealed class WholeStoreBanDriftTests
{
    private static IEnumerable<(string Path, CompilationUnitSyntax Root)> IoSources()
    {
        string io = TestRepo.Src("Cobol.Net.Runtime", "IO");
        return Directory.GetFiles(io, "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetRelativePath(io, f).Replace('\\', '/'),
                (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot()));
    }

    private static string Where(string path, Microsoft.CodeAnalysis.SyntaxNode node) =>
        $"{path}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node}";

    [Fact]
    public void PositionalHostIo_IsTheStoresPageMediums_OnePagePerCall()
    {
        var outside = new List<string>();
        var transfers = new List<(string Path, InvocationExpressionSyntax Call)>();
        foreach (var (path, root) in IoSources())
            foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is not MemberAccessExpressionSyntax
                    { Expression: IdentifierNameSyntax { Identifier.Text: "RandomAccess" }, Name.Identifier.Text: var verb })
                    continue;
                if (!path.StartsWith("Store/", StringComparison.Ordinal)) outside.Add(Where(path, call));
                else if (verb is "Read" or "Write") transfers.Add((path, call));
            }
        Assert.True(outside.Count == 0, "Positional host I/O outside the keyed store's page medium (2.2.1.11 (d)): a "
            + "keyed file is read and written through IO/Store/ only, one page per call:\n  " + string.Join("\n  ", outside));
        Assert.True(transfers.Count >= 2, $"The sweep found {transfers.Count} page transfers in IO/Store/ — the page medium "
            + "moved, and this test no longer measures anything.");
        var wide = transfers
            .Where(t => !OnePage(t.Call))
            .Select(t => Where(t.Path, t.Call)).ToList();
        Assert.True(wide.Count == 0, "A page-medium transfer not bounded to one page (PageHeader.PageSize):\n  "
            + string.Join("\n  ", wide));
    }

    /// <summary>Whether a <c>RandomAccess.Read</c>/<c>Write</c> moves at most one page: its span argument is, or slices
    /// from, a span cut to <c>PageHeader.PageSize</c> in the same method.</summary>
    private static bool OnePage(InvocationExpressionSyntax call)
    {
        var method = call.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (method is null || call.ArgumentList.Arguments.Count < 2) return false;
        string span = call.ArgumentList.Arguments[1].Expression.ToString();
        if (span.Contains("[..PageHeader.PageSize]", StringComparison.Ordinal)) return true;
        string root = span.Split('[')[0];
        return method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Any(v => v.Identifier.Text == root
            && v.Initializer?.Value.ToString().Contains("[..PageHeader.PageSize]", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void NoBufferIsRentedAtALengthsSize()
    {
        var rents = IoSources().SelectMany(s => s.Root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(c => c.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Rent" } m
                    && m.Expression.ToString().Contains("ArrayPool", StringComparison.Ordinal)
                    && c.ArgumentList.ToString() is var args
                    && (args.Contains("Length", StringComparison.Ordinal) || args.Contains("PageCount", StringComparison.Ordinal)))
                .Select(c => Where(s.Path, c)))
            .ToList();
        Assert.True(rents.Count == 0, "A pooled buffer sized by a file's length — the whole-store read's shape "
            + "(2.2.1.11 (d)):\n  " + string.Join("\n  ", rents));
    }

    /// <summary>2.2.1.13's deletion list: members of the whole-store model, by name. Each name is distinctive except the
    /// ones in <see cref="ConnectorOnlyNames"/>, which are ordinary words elsewhere and banned only on the keyed connectors
    /// and the store.</summary>
    private static readonly string[] DeletedNames =
    [
        "ComposeStore", "DecodeStore", "DecodeHeader", "WriteHeader", "ReadHeader", "GapTag", "GapBytes", "EncodeGap",
        "ImageMedium", "MaxStoreBytes", "HeaderBytes", "OrdinalSlots", "GenerationProbeBytes", "DecodeGeneration",
        "PersistIfChanged", "PersistIsReachable", "ReadImage", "ReadThroughAreas", "WriteThroughAreas", "ReadGeneration",
        "Reload", "PersistFrames", "StoreHolds", "HostFaultObserver", "HostFaultPoint", "PersistedVersion",
        "VersionAtEntry", "KeyedStoreTable",
    ];

    private static readonly string[] ConnectorOnlyNames =
        ["Load", "Persist", "Fill", "ReadAt", "Version", "Generation", "Mutated", "ReleaseMint", "_storeHeaderBytes", "_areas"];

    private static readonly string[] ConnectorAndStoreTypes =
        ["KeyedConnector", "RelativeConnector", "IndexedConnector", "KeyedStore", "RelativeStore", "IndexedStore", "StoreTable"];

    private static IEnumerable<string> DeclaredNames(MemberDeclarationSyntax m) => m switch
    {
        BaseTypeDeclarationSyntax t => [t.Identifier.Text],
        MethodDeclarationSyntax x => [x.Identifier.Text],
        PropertyDeclarationSyntax p => [p.Identifier.Text],
        FieldDeclarationSyntax f => f.Declaration.Variables.Select(v => v.Identifier.Text),
        EventFieldDeclarationSyntax e => e.Declaration.Variables.Select(v => v.Identifier.Text),
        _ => [],
    };

    [Fact]
    public void TheWholeStoreModel_HasNoSurvivor()
    {
        var survivors = new List<string>();
        int typesSeen = 0;
        foreach (var (path, root) in IoSources())
            foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                string? owner = member.Parent is BaseTypeDeclarationSyntax t ? t.Identifier.Text : null;
                if (member is BaseTypeDeclarationSyntax type && ConnectorAndStoreTypes.Contains(type.Identifier.Text)) typesSeen++;
                foreach (string name in DeclaredNames(member))
                {
                    bool banned = DeletedNames.Contains(name)
                        || (owner is not null && ConnectorAndStoreTypes.Contains(owner) && ConnectorOnlyNames.Contains(name));
                    if (banned)
                        survivors.Add($"{path}:{member.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {owner}.{name}");
                }
            }
        Assert.True(typesSeen >= ConnectorAndStoreTypes.Length, $"The sweep found {typesSeen} of the "
            + $"{ConnectorAndStoreTypes.Length} connector and store types — one moved, and this test no longer measures it.");
        Assert.True(survivors.Count == 0, "A member of the deleted whole-store model (2.2.1.13) is declared again — "
            + "no wrapper, no shim, no reader of the old format (CLAUDE.md rule 4):\n  " + string.Join("\n  ", survivors));
    }
}
