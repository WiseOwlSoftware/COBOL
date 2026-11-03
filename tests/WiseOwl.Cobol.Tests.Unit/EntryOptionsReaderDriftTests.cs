// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
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
/// reads goes through it too, and this test refuses the entry binder reading the unit's slice directly. The entry binder is
/// every file the tree declares as one (<see cref="EntryBinderFiles"/>), not a list of names (kb/Work PB4472).
/// </summary>
public sealed partial class EntryOptionsReaderDriftTests
{
    private static string BindingDir => TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding");

    private static string Binder(string file) => File.ReadAllText(Path.Combine(BindingDir, file));

    /// <summary>The source with every <c>//</c> line comment (so every <c>///</c> doc comment) removed, so a fact NAMED in prose is
    /// not counted as a read.</summary>
    private static string Code(string text) =>
        string.Join('\n', text.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.IndexOf("//", StringComparison.Ordinal) is var i and >= 0 ? l[..i] : l));

    /// <summary>The ENTRY BINDER, derived from the tree and never named: every top-level <c>Binding/*.cs</c> file that declares a
    /// <c>partial class DataBinder</c>, or a class built over the one <c>DataBinder</c> of its unit (a primary constructor taking
    /// it: <c>LinkageBinder</c>, <c>ConstantEntryBinder</c>, ...). A new partial or a new entry binder joins the scan the moment it is
    /// written. <c>OoDataBinder.cs</c> is the one file left out: it IS the place the unit's slice is read (second fact). The
    /// <c>Procedure/</c>, <c>Validation/</c> and <c>Passes/</c> subtrees are the statement and pass halves, whose combiners
    /// (<c>BinderContext.Options</c>, <c>StorageFormPass</c>) override the slice with the method's model themselves.</summary>
    private static IEnumerable<string> EntryBinderFiles() =>
        Directory.EnumerateFiles(BindingDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(p => Path.GetFileName(p) != "OoDataBinder.cs")
            .Where(p => EntryBinderDeclaration().IsMatch(Code(File.ReadAllText(p))));

    [GeneratedRegex(@"\bpartial\s+class\s+DataBinder\b|\bclass\s+\w+\s*\(\s*DataBinder\s+\w+")]
    private static partial Regex EntryBinderDeclaration();

    /// <summary>The mentions of the unit's slice in code: the <c>Options</c> member of the unit's <c>ConfigurationSlice</c>, reached
    /// however it is spelled (<c>State.Configuration.Options</c>, a local <c>configuration.Options</c>, a bare <c>Options</c>). The one
    /// legal mention in the entry binder is the unit binder WRITING its own slice once, <c>configuration.Options = ...</c>
    /// (<c>DataBinder.cs</c>), which no entry reads.</summary>
    private static int SliceMentions(string code) =>
        SliceMention().Matches(code).Count - SliceWrite().Matches(code).Count;

    [GeneratedRegex(@"\bOptions\b")]
    private static partial Regex SliceMention();

    [GeneratedRegex(@"\bconfiguration\.Options\s*=(?!=)")]
    private static partial Regex SliceWrite();

    [Fact]
    public void TheEntryBinder_ReadsNoOptionsFactFromTheUnitsSliceDirectly()
    {
        string[] files = [.. EntryBinderFiles().Select(f => Path.GetFileName(f))];
        // The scan reached the whole entry binder: every DataBinder partial by its name, and the entry binders built over it.
        string[] partials = [.. Directory.EnumerateFiles(BindingDir, "DataBinder*.cs").Select(f => Path.GetFileName(f))];
        Assert.NotEmpty(partials);
        Assert.All(partials, f => Assert.Contains(f, files));
        Assert.Contains("LinkageBinder.cs", files);
        Assert.Contains("ConstantEntryBinder.cs", files);

        string[] readers = [.. files.Where(f => SliceMentions(Code(Binder(f))) != 0)];
        Assert.True(readers.Length == 0,
            $"the entry binder reads the unit's OPTIONS slice directly (use Oo.EntryOptions, kb/Work PB4464) in: {string.Join(", ", readers)}");
        string code = string.Join('\n', files.Select(f => Code(Binder(f))));
        Assert.Contains("Oo.EntryOptions.FloatBinaryEndianness", code);
    }

    /// <summary>The reader test is not vacuous (kb/Work PB4472): the function that guards the tree reports a direct read planted
    /// in any partial, however the slice is spelled, and passes the legal write, the entry-options route and a mention in prose.</summary>
    [Theory]
    [InlineData("var e = State.Configuration.Options.FloatBinaryEndianness;", 1)]
    [InlineData("var c = State.Configuration; var e = c.Options.FloatBinaryEndianness;", 1)]
    [InlineData("var e = Options.FloatBinaryEndianness;", 1)]
    [InlineData("var e = (x ? State.Configuration.Options : y).FloatBinaryEndianness;", 1)]
    [InlineData("var e = Oo.EntryOptions.FloatBinaryEndianness;", 0)]
    [InlineData("configuration.Options = OptionsBinder.BindParagraph(p, Edition, configuration.InheritedOptions, f);", 0)]
    [InlineData("var e = Oo.EntryOptions.FloatBinaryEndianness; // State.Configuration.Options is the unit's, not the entry's", 0)]
    public void ASlicePlantedInAnyPartial_IsReported(string line, int mentions) =>
        Assert.Equal(mentions, SliceMentions(Code(line)));

    [Fact]
    public void EntryOptions_IsTheBindingMethodsModelElseTheUnits()
    {
        string text = Binder("OoDataBinder.cs");
        Assert.Contains("internal OptionsModel EntryOptions => BindingMethod?.MethodOptions ?? Options;", text);
    }
}
