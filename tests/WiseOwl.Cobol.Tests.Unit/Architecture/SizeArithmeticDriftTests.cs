// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A DATA ITEM'S SIZE HAS ONE COMPUTATION (kb/Work PB5695 and its review R1-1). Every size the compiler takes of an item — its
/// image, its bytes, its storage with every overlay, an OCCURS product, a group sum, a REDEFINES comparison, an offset, a bit
/// extent — is computed in 64 bits by <c>RecordLayout</c>'s size section and narrowed to <see cref="int"/> by ONE function,
/// <c>RecordLayout.Narrow</c>, which refuses a value no item within the limit has. Before it, the sums were written where each
/// was needed, in <see cref="int"/>: <c>DataItem.ByteWidth</c> summed with a checked <c>Enumerable.Sum</c>, so an over-limit group
/// with a REDEFINES over it crashed the compiler with an OverflowException, and the image width wrapped
/// (<c>05 E PIC X(1000) OCCURS 2000000000</c> read as −1,454,759,936).
/// <para><b>What this test is.</b> A TEXT test over <c>src/WiseOwl.Cobol.Compiler</c>, chosen over a semantic (Roslyn) one for its
/// cost: it bans the SHAPES the sums had — an OCCURS count defaulted to one, an OCCURS value or a width multiplied, a width summed
/// by <c>Enumerable.Sum</c> or accumulated by <c>+=</c>, a width clamped by <c>(int)Math.Min</c> — outside
/// <c>Binding/Model/RecordLayout.cs</c>, on every line that is not wholly a comment. It cannot see arithmetic on a local that holds
/// a width under another name; the shapes it names are the ones every one of the sites the review found had.</para>
/// </summary>
public sealed class SizeArithmeticDriftTests
{
    private const string Widths =
        "(?:ByteWidth|ImageWidth|PhysicalWidth|PhysicalOccurrenceWidth|ElementaryByteWidth|ElementaryImageWidth|AreaWidth"
        + "|StrideBits|WidthBits|RunBits|ExtentBits|TableBits|OccupiedBytes)";

    /// <summary>The banned shapes, each with its name.</summary>
    private static readonly (string Name, Regex Shape)[] Banned =
    [
        ("an OCCURS count defaulted to one (RecordLayout.Occurrences)", new(@"\bOccurs\s*\?\?\s*1\b", RegexOptions.Compiled)),
        ("an OCCURS value multiplied (RecordLayout.Times)",
            new(@"\bOccurs!?\.Value\s*(?:-\s*1\s*\)?\s*)?\*|\*\s*\(?\s*[\w.]*Occurs!?\.Value", RegexOptions.Compiled)),
        ("a width multiplied (RecordLayout.Product / Times)",
            new(@"\b" + Widths + @"\b(?:\([^()]*\))?\s*\*(?!=)|\*\s*\(?\s*[\w.!]*\b" + Widths + @"\b", RegexOptions.Compiled)),
        ("a width summed by Enumerable.Sum (RecordLayout.OverStorageChildren / Plus)",
            new(@"\.Sum\([^;]*\b(?:" + Widths[3..^1] + @"|StandardDataFormatCharacters|PerOccurrence)\b", RegexOptions.Compiled)),
        ("a width accumulated by += (RecordLayout.Plus)", new(@"\+=\s*[^;]*\b" + Widths + @"\b", RegexOptions.Compiled)),
        ("a width clamped by (int)Math.Min (RecordLayout.Narrow / SaturatedBound)",
            new(@"\(int\)\s*Math\.(?:Min|Clamp)\([^;]*(?:Width|Extent|RecordSize)", RegexOptions.Compiled)),
    ];

    private static string? ShapeOf(string line)
    {
        string code = line.Split("//", 2)[0];
        string t = code.Trim();
        if (t.Length == 0 || t.StartsWith('*') || t.StartsWith("/*", StringComparison.Ordinal)) return null;
        return Banned.FirstOrDefault(b => b.Shape.IsMatch(code)).Name;
    }

    [Fact]
    public void NoSizeArithmetic_OutsideRecordLayout()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        string sep = Path.DirectorySeparatorChar.ToString();
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (file.Contains(sep + "obj" + sep) || file.Contains(sep + "bin" + sep)) continue;
            if (Path.GetFileName(file) == "RecordLayout.cs") continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (ShapeOf(lines[i]) is { } shape)
                    offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}  [{shape}]  {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} site(s) compute a data item's size outside RecordLayout's one 64-bit computation:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}Compute it with RecordLayout (Times, Product, Plus, OverStorageChildren, the Exact* measures) and narrow "
            + "it with RecordLayout.Narrow (kb/Work PB5695).");
    }

    /// <summary>The test can fail: every shape the review found is caught (the lines as they stood before the fix).</summary>
    [Theory]
    [InlineData(": Children.Where(c => c.RedefinesTargetName is null).Sum(c => c.ByteWidth * (c.Occurs ?? 1));")]
    [InlineData("cls.Classify(tier, cls.Members.Max(m => m.ByteWidth * (m.Occurs ?? 1)), reject);")]
    [InlineData("running += PhysicalOccurrenceWidth(c) * (c.Occurs ?? 1);")]
    [InlineData("int Width(DataItem d) => d.ByteWidth * (d.Occurs ?? 1);")]
    [InlineData("(c.Occurs!.Value - 1) * (StrideBits(c) - WidthBits(c))));")]
    [InlineData("foreach (var m in run) bits += RunBits(m);")]
    [InlineData("IsVariableLengthRecord(item) ? (int)Math.Min(int.MaxValue, MaxDynamicExtent(item)) : item.ByteWidth;")]
    [InlineData("item.IsGroup ? PhysicalChildrenOf(item).Sum(f => f.Width) : item.ByteWidth;")]
    public void TheBannedShapes_AreCaught(string line) => Assert.NotNull(ShapeOf(line));

    /// <summary>The narrowing accepts every measure an item within the limit has and refuses the next value, a negative one and a
    /// saturated one as an internal compiler error, never a clamp or a wrap.</summary>
    [Fact]
    public void Narrow_AcceptsTheLargestAdmittedMeasure_AndRefusesEveryOther()
    {
        Assert.Equal(RecordLayout.MaxMeasure, RecordLayout.Narrow(RecordLayout.MaxMeasure));
        Assert.Equal(8L * RecordLayout.MaxStorageBytes, RecordLayout.MaxMeasure);
        Assert.True(RecordLayout.MaxMeasure <= int.MaxValue);
        foreach (long refused in new[] { RecordLayout.MaxMeasure + 1, -1L, long.MaxValue })
            Assert.Contains("internal compiler error", Assert.Throws<InvalidOperationException>(() => RecordLayout.Narrow(refused)).Message);
    }
}
