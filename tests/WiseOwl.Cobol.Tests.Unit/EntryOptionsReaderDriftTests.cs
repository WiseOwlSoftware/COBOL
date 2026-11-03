// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY OPTIONS FACT A DATA ENTRY'S DESCRIPTION READS COMES FROM THE MODEL OF THE SOURCE ELEMENT THE ENTRY IS IN (kb/Work
/// PB4464). ISO §11.9.4 GR1 makes an OPTIONS paragraph govern the source element it is written in, and a METHOD is a source
/// element of its own, so its LOCAL-STORAGE and LINKAGE entries are described under the METHOD's model, not the class
/// half's. <c>DataBinder.BindEntry</c> read the class half's model (<c>State.Configuration.Options</c>) for the FLOAT-BINARY
/// endianness of a standard binary floating-point item with no endianness-phrase, so a method's own
/// <c>FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT</c> never reached its items: a silent wrong byte order. The entry binder now
/// asks <c>OoDataBinder.EntryOptions</c> (the binding method's model, else the unit's), the data-phase twin of the statement
/// half (<c>ProcedureBindState.Options</c>) and the emission half (<c>EmitContext.Options</c>); a new OPTIONS fact an entry
/// reads goes through it too, and this test refuses the entry binder reading the unit's slice directly.
/// </summary>
public sealed class EntryOptionsReaderDriftTests
{
    private static string Binder(string file) => File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", file));

    [Fact]
    public void TheEntryBinder_ReadsNoOptionsFactFromTheUnitsSliceDirectly()
    {
        // The entry binder is the partial class DataBinder (every DataBinder*.cs) and the entry binders extracted from it.
        string[] files = [.. Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding"), "DataBinder*.cs").Select(Path.GetFileName)!,
            "LinkageBinder.cs", "ConstantEntryBinder.cs"];
        string code = string.Join('\n', files.SelectMany(f => Binder(f).Split('\n'))
            .Select(l => l.IndexOf("//", StringComparison.Ordinal) is var i and >= 0 ? l[..i] : l));
        Assert.DoesNotContain("Configuration.Options", code);
        Assert.Contains("Oo.EntryOptions.FloatBinaryEndianness", code);
    }

    [Fact]
    public void EntryOptions_IsTheBindingMethodsModelElseTheUnits()
    {
        string text = Binder("OoDataBinder.cs");
        Assert.Contains("internal OptionsModel EntryOptions => BindingMethod?.MethodOptions ?? Options;", text);
    }
}
