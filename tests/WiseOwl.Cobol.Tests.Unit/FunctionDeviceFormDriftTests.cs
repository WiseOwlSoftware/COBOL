// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE DESCRIPTION OF A NUMERIC FUNCTION'S RETURNED-VALUE ITEM IS ONE CATALOG COLUMN, AND THE NEXT FUNCTION IS AUTOMATIC
/// (kb/Work PB3514; ISO/IEC 1989:2023 §15.4 and §15.4.1; docs/CONFORMANCE.md DOC-A.1-92). DISPLAY shows a numeric function's value
/// by the description GnuCOBOL 3.2.0 gives its temporary: derived from the value (<see cref="FunctionDeviceForm.Value"/>, the default
/// of every row, so a function added tomorrow needs no edit), a copy of an argument's item (<see cref="FunctionDeviceForm.Argument"/>), an
/// unsigned DISPLAY integer of fixed width (<see cref="FunctionDeviceForm.FixedDigits"/>), or the literal the binder folds the call to
/// (<see cref="FunctionDeviceForm.Literal"/>). A row that is anything but the default is a decision about ONE function, so this holds
/// the set of them against the three places that must agree: the renderer that builds the arguments' fields
/// (<c>FunctionDeviceText</c>), the binder that folds, and the documented determination.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FunctionDeviceFormDriftTests
{
    private static readonly Regex Row = new("^\\s*Add\\(new\\(\"(?<name>[A-Z0-9-]+)\",.*$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Every catalog row's name and the text of its declaration, read from the source so a row cannot hide.</summary>
    private static Dictionary<string, string> Rows()
    {
        string src = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Row.Matches(src)) rows[m.Groups["name"].Value] = m.Value;
        Assert.True(rows.Count > 80, "the catalog scan found suspiciously few rows: " + rows.Count);
        return rows;
    }

    private static List<string> RowsDeclaring(FunctionDeviceForm form) =>
        [.. Rows().Where(r => r.Value.Contains($"DeviceForm: FunctionDeviceForm.{form}", StringComparison.Ordinal)).Select(r => r.Key).Order(StringComparer.Ordinal)];

    [Fact]
    public void TheRowsThatSayOtherwiseAreExactlyTheDocumentedSet()
    {
        Assert.Equal(["ABS", "MAX", "MEAN", "MEDIAN", "MIN"], RowsDeclaring(FunctionDeviceForm.Argument));
        Assert.Equal(["DATE-OF-INTEGER", "DAY-OF-INTEGER"], RowsDeclaring(FunctionDeviceForm.FixedDigits));
        Assert.Equal(["BYTE-LENGTH", "LENGTH"], RowsDeclaring(FunctionDeviceForm.Literal));
        Assert.Empty(RowsDeclaring(FunctionDeviceForm.Value));   // the default is never written
    }

    /// <summary>The renderer builds an argument-copy field for the runtime methods of the Argument rows, and for no others: a row that
    /// says Argument without a case would fall to the value's description silently, and a case without the row is dead.</summary>
    [Fact]
    public void EveryArgumentRowHasACaseInTheRenderer_AndNoOtherHas()
    {
        string renderer = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "FunctionDeviceText.cs"));
        var cases = Regex.Matches(renderer, "case \\(_, \"(?<method>\\w+)\"\\)").Select(m => m.Groups["method"].Value).Order(StringComparer.Ordinal).ToList();
        var declared = RowsDeclaring(FunctionDeviceForm.Argument)
            .Select(name => { Assert.True(IntrinsicCatalog.TryGet(name, out var sig), name); return sig.RuntimeMethod; })
            .Order(StringComparer.Ordinal).ToList();
        Assert.Equal(declared, cases);
    }

    [Fact]
    public void AFixedDigitsRowNamesItsDigits_AndNoOtherRowDoes()
    {
        foreach (var (name, text) in Rows())
        {
            Assert.True(IntrinsicCatalog.TryGet(name, out var sig), name);
            Assert.Equal(sig.DeviceForm == FunctionDeviceForm.FixedDigits, sig.DeviceDigits > 0);
            Assert.Equal(text.Contains("DeviceDigits:", StringComparison.Ordinal), sig.DeviceDigits > 0);
        }
    }

    /// <summary>The binder folds LENGTH, BYTE-LENGTH and the ALGEBRAIC family. GnuCOBOL folds the first two into literals (so DISPLAY shows the
    /// literal) and evaluates the others at run time (so DISPLAY shows the returned value's description): the Literal rows are the fold
    /// rows GnuCOBOL folds, and the binder names the function it folded so DISPLAY can tell.</summary>
    [Fact]
    public void TheLiteralRowsAreTheFoldsGnuCobolMakes_AndTheBinderNamesEveryFold()
    {
        var folds = Rows().Keys.Where(n => { IntrinsicCatalog.TryGet(n, out var sig); return sig.Bind == IntrinsicBind.Fold; }).Order(StringComparer.Ordinal).ToList();
        Assert.Contains("LENGTH", folds);
        foreach (string name in folds.Where(n => n is not ("BYTE-LENGTH" or "LENGTH")))
        {
            IntrinsicCatalog.TryGet(name, out var sig);
            Assert.True(sig.DeviceForm == FunctionDeviceForm.Value || sig.ResultCategory != PicCategory.Numeric,
                $"{name} is a numeric fold GnuCOBOL does not make, so DISPLAY shows its value's description, not a literal");
        }
        string binder = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"));
        Assert.Contains("FoldedFunction = sig.Name", binder, StringComparison.Ordinal);
        Assert.Contains("FoldedFunction = l.FoldedFunction", binder, StringComparison.Ordinal);
    }

    /// <summary>The determination is documented: DOC-A.1-92 names every function whose row says otherwise.</summary>
    [Fact]
    public void TheDeterminationNamesEveryFunctionThatSaysOtherwise()
    {
        string doc = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        string row = string.Join("\n", doc.Split('\n').Where(l => l.StartsWith("| DOC-A.1-92 |", StringComparison.Ordinal)));
        Assert.False(row.Length == 0, "DOC-A.1-92 is gone from docs/CONFORMANCE.md");
        foreach (string name in new[] { "Argument", "FixedDigits", "Literal" }.SelectMany(f => RowsDeclaring(Enum.Parse<FunctionDeviceForm>(f))))
            Assert.True(row.Contains(name, StringComparison.Ordinal), $"DOC-A.1-92 does not name {name}, whose catalog row gives its returned value a description of its own (kb/Work PB3514)");
    }
}
