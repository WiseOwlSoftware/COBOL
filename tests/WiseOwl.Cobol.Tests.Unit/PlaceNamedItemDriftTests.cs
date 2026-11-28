// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ "WHICH DATA-NAME DOES THIS REFERENCE NAME?" HAS ONE ANSWER, <see cref="Place.NamedItem"/> (kb/Work PB5801 review
/// R1-1). A place answers three questions: <see cref="Place.Item"/> (attributes and storage), <see cref="Place.DenotedItem"/>
/// (the whole declared item, null for a reference-modified slice, §2023:8.4.3.3.4 GR5) and <see cref="Place.NamedItem"/> (the
/// data-name written, under any decoration, the level-66 alias when the resolver recorded one). A rule over what a
/// statement REFERENCES asks the third. PB5801's first fix asked <see cref="Place.DenotedItem"/> for §1985:XIII.3.20.3 SR5's
/// USE half and let <c>DISPLAY WS-KEY (1:1)</c> escape; asked of <see cref="Place.Item"/> before it, <c>DISPLAY RX</c> of an
/// alias control was read as the renamed item; and INITIALIZE's RENAMES screen carried its own decorator unwrap.
/// <para>The facts: the accessor sees through every decoration to a recorded alias; and no compiler file outside
/// <c>Place.cs</c> reads <see cref="Place.DenotesAs"/> or unwraps decorators to find a denoted item, so a second
/// hand-written answer is a failing test.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PlaceNamedItemDriftTests
{
    private static readonly string CompilerRoot = TestRepo.Src("WiseOwl.Cobol.Compiler");

    private static DataItem Item(string name) => new() { Level = 5, CobolName = name, ClrName = name };

    [Fact]
    public void NamedItem_SeesThroughEveryDecoration_ToTheRecordedAlias()
    {
        var renamed = Item("WS-KEY");
        var alias = new DataItem { Level = 66, CobolName = "RX", ClrName = "RX" };
        var plain = new MemberPlace(new AccessPath([]), renamed);
        var aliased = plain with { DenotesAs = alias };
        Position one = new PositionConstant(1);

        Assert.Same(renamed, plain.NamedItem);
        Assert.Same(alias, aliased.NamedItem);
        // A reference-modified reference names its identifier-1: the slice denotes no declared item, the name does not change.
        var slice = new RefModPlace(plain, one, one);
        Assert.Null(slice.DenotedItem);
        Assert.Same(renamed, slice.NamedItem);
        var aliasSlice = new RefModPlace(aliased, one, one);
        Assert.Null(aliasSlice.DenotedItem);
        Assert.Same(alias, aliasSlice.NamedItem);
        // Stacked decorations answer the same.
        Assert.Same(alias, new NumericImagePlace(new RefModPlace(aliased, one, null)).NamedItem);
    }

    private static IEnumerable<(string File, int Line, string Text)> CodeLines() =>
        Directory.EnumerateFiles(CompilerRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                        && !f.Contains(Path.Combine("bin", ""), StringComparison.Ordinal)
                        && Path.GetFileName(f) != "Place.cs")
            .SelectMany(f => File.ReadAllLines(f)
                .Select((text, i) => (File: f, Line: i + 1, Text: text.Trim()))
                .Where(l => !l.Text.StartsWith("//", StringComparison.Ordinal)));

    /// <summary>Reading <c>DenotesAs</c> (other than the one resolver's <c>with { DenotesAs = … }</c>) or walking
    /// <c>PlaceDecorator.Inner</c> while asking <c>DenotedItem</c> is a second answer to the named-item question.</summary>
    [Fact]
    public void NoCompilerFile_AnswersTheNamedItemQuestionByHand()
    {
        var read = new Regex(@"\.DenotesAs\b|\bDenotesAs\s*\?\?|DenotedItem\s+is\s+null\s*&&[^;]*PlaceDecorator", RegexOptions.CultureInvariant);
        var sites = CodeLines().Where(l => read.IsMatch(l.Text)).ToList();
        Assert.True(sites.Count == 0,
            "the data-name a reference names is asked of Place.NamedItem, never by reading DenotesAs or unwrapping decorators "
            + "(kb/Work PB5801 review R1-1). Found:\n"
            + string.Join("\n", sites.Select(s => $"  {Path.GetRelativePath(CompilerRoot, s.File)}:{s.Line}: {s.Text}")));
    }
}
