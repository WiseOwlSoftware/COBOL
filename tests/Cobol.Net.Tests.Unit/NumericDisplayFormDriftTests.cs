// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE FORM A DISPLAY STATEMENT GIVES A NUMERIC OPERAND IS DISPLAY'S AND NOBODY ELSE'S, AND ONE PLACE CHOOSES IT
/// (kb/Work PB3505, owner decision PB3467; ISO §14.9.11.4 GR1, Annex A.1 item 56). <c>--numeric-display</c> selects
/// between the decimal device form and the digit image for DISPLAY's GR1 conversion ONLY: a MOVE of the same item to an
/// alphanumeric receiver is §14.9.25.4 GR6's, a comparison's text is §8.8.4.2's, a record image is the item's storage -
/// none of them may change with the option. So the option is read by the DISPLAY emitter alone
/// (<c>AcceptDisplayEmitter</c> asks <c>OperandText.AsDeviceString</c>), the device-form flag reaches <c>FieldAsString</c>
/// from that one entry, and the runtime's decimal formatter is named from one façade method. A second reader of the option
/// would make a MOVE's result depend on it - the wrong answer this guard exists to keep out. And every value of the option
/// is pinned by a WiseOwl conformance test, so a form added to the enum cannot ship untested.
/// <para>ACCEPT's GR1 conversion of a numeric receiver (§14.9.1.4 GR1; kb/Work PB3513) is the same latitude's other
/// direction and takes the same option from the same emitter file: <c>decimal</c> reads DISPLAY's decimal form back
/// (<c>AcceptSource.DeviceNumeric</c>, named from one façade method), <c>digits</c> keeps the digit window.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NumericDisplayFormDriftTests
{
    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    private static string Code(string file) =>
        string.Join("\n", File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

    private static List<string> Readers(string pattern) =>
        [.. CompilerSources().Where(f => Regex.IsMatch(Code(f), pattern)).Select(f => Path.GetFileName(f))];

    [Fact]
    public void TheOption_IsReadByTheDisplayEmitterAlone()
    {
        // EmitContext.NumericDisplay is the one property over the per-compilation context; the DISPLAY emitter its one reader.
        Assert.Equal(["AcceptDisplayEmitter.cs"], Readers(@"\bctx\.NumericDisplay\b|\.Edition\.NumericDisplay\b").Where(f => f != "EmitCore.cs").Order());
    }

    [Fact]
    public void TheDeviceForm_IsAskedForByAsDeviceString_AndNowhereElse()
    {
        Assert.Equal(["AcceptDisplayEmitter.cs", "OperandText.cs"], Readers(@"\bAsDeviceString\(").Order());
        // The flag that turns a numeric item's image into the device form is passed by name from that one entry.
        Assert.Equal(["OperandText.cs"], Readers(@"\bdeviceComma:\s*num\.UnitDecimalPointIsComma\b"));
        // The runtime's decimal formatter is spelled in the façade, and reached from OperandText through it.
        Assert.Equal(["OperandText.cs", "RuntimeApi.cs"], Readers(@"\bNumDisplayDecimal\(").Order());
        Assert.Equal(["RuntimeApi.cs"], Readers(@"nameof\(CobolNum\.DisplayDecimal"));
    }

    [Fact]
    public void TheAcceptReader_IsAskedForFromTheAcceptEmitterAndNamedFromOneFacadeMethod()
    {
        // ACCEPT's decimal reader is spelled in the façade and asked for by the ACCEPT emitter's one numeric arm, the arm that
        // also reads the option (TheOption_IsReadByTheDisplayEmitterAlone), so the form ACCEPT reads and the form DISPLAY
        // writes are chosen by the same value in the same file.
        Assert.Equal(["AcceptDisplayEmitter.cs", "RuntimeApi.cs"], Readers(@"\bAcceptDeviceNumeric\(").Order());
        Assert.Equal(["RuntimeApi.cs"], Readers(@"nameof\(AcceptSource\.DeviceNumeric\)"));
        Assert.Equal(new[] { "CobolNum.DeviceDecimal.cs", "AcceptSource.cs" }.Order(),
            Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(f => Regex.IsMatch(Code(f), @"\bParseDeviceDecimal\("))
                .Select(f => Path.GetFileName(f)).Order());
    }

    [Fact]
    public void EveryNonDefaultValueOfTheOption_IsPinnedByAWiseOwlConformanceTest()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.Suite("wiseowl", "options.json")));
        var pinned = doc.RootElement.EnumerateObject()
            .Select(test => test.Value.TryGetProperty("numeric-display", out var v) ? v.GetString() : null)
            .Where(v => v is not null).ToHashSet(StringComparer.Ordinal);
        foreach (string spelling in NumericDisplayFormOption.OptionSpellings.Skip(1))   // the first is the default, pinned by every test that sets no option
            Assert.True(pinned.Contains(spelling),
                $"--numeric-display {spelling} is pinned by no wiseowl/options.json entry (kb/Work PB3505): add a conformance test that sets it");
        Assert.Equal(NumericDisplayForm.Decimal, Enum.GetValues<NumericDisplayForm>()[0]);   // the default is the first member, as TryParse assumes
    }
}
