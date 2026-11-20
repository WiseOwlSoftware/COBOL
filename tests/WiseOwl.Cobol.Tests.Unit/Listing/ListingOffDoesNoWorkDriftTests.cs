// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ NO LISTING ASKED, NO LISTING WORK — BY STRUCTURE (DESIGN-listing.md section 9). A compilation whose
/// <c>Options.Listing</c> is null builds the front end the driver builds (<see cref="CompilerDriver.NewFrontend"/>) with no
/// listing recording: every run of the text stages traces into <see cref="TextStageTrace.None"/>, the parse collects no
/// <see cref="SourceUnitFacts"/>, no file is hashed and no clock is read for a program without WHEN-COMPILED; and the
/// compilation returns no listing and writes no <c>.lst</c>. With a listing asked, the same calls record each fact, so the
/// test can tell the two apart (each assertion below fails on the listing side).
/// </summary>
public sealed class ListingOffDoesNoWorkDriftTests : WiseOwlCobolTestBase
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. NOWORK.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
                   COPY NOWORKC.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   DISPLAY X.
                   STOP RUN.
        """;

    private (CompilerDriver.Options Options, string Source) Write(WiseOwl.Cobol.Listing.ListingRequest? listing)
    {
        string src = Path.Combine(TempDir, "nowork.cob");
        File.WriteAllText(src, Source);
        File.WriteAllText(Path.Combine(TempDir, "NOWORKC.cpy"), "       01 X PIC X(4) VALUE \"ABCD\".\n");
        return (new CompilerDriver.Options(src, DialectLevel: 85, CopyPaths: [TempDir], Listing: listing), src);
    }

    [Fact]
    public void WithoutAListing_TheFrontEndRecordsNothing()
    {
        var (options, src) = Write(listing: null);
        var inputs = new CompilationInputs();
        var frontend = CompilerDriver.NewFrontend(options, inputs, recordsListing: options.Listing is not null);
        Assert.False(frontend.RecordsListing);
        Assert.NotNull(frontend.Parse(src, new DiagnosticBag()));
        Assert.Same(TextStageTrace.None, frontend.ListingTrace);
        Assert.Null(frontend.UnitFacts);
        Assert.Empty(inputs.FileDigests);
        Assert.False(inputs.ReadsCompilationTime);
    }

    /// <summary>The control: with a listing asked, the same front end records the inclusion and the units.</summary>
    [Fact]
    public void WithAListing_TheFrontEndRecords()
    {
        var (options, src) = Write(new WiseOwl.Cobol.Listing.ListingRequest());
        var inputs = new CompilationInputs { DigestsFiles = true };
        var frontend = CompilerDriver.NewFrontend(options, inputs, recordsListing: options.Listing is not null);
        Assert.NotNull(frontend.Parse(src, new DiagnosticBag()));
        Assert.NotSame(TextStageTrace.None, frontend.ListingTrace);
        Assert.Single(frontend.ListingTrace.Inclusions);
        Assert.Equal("NOWORK", Assert.Single(frontend.UnitFacts!.Units).Name);
        Assert.Equal(2, inputs.FileDigests.Count);
    }

    [Fact]
    public void WithoutAListing_TheCompilationReturnsAndWritesNone()
    {
        var (options, _) = Write(listing: null);
        var result = CompilerDriver.Compile(options);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Null(result.Listing);
        Assert.DoesNotContain(result.OutputFiles, f => f.EndsWith(ListingOutput.Extension(ListingFormats.Text), StringComparison.Ordinal));
        Assert.False(result.Inputs.ReadsCompilationTime);
        Assert.Empty(result.Inputs.FileDigests);
    }

    /// <summary>A 2023 program that reaches EVERY recording point of step 4a (DESIGN-listing.md section 4.4): a LISTING directive,
    /// PUSH and POP of its state, a LISTING OFF in library text (the end-of-inclusion restore), a PAGE directive, a DISPLAY
    /// directive with no UPON phrase, COPY ... SUPPRESS REPLACING and a REPLACE statement.</summary>
    private (CompilerDriver.Options Options, string Source) WriteHooks(WiseOwl.Cobol.Listing.ListingRequest? listing)
    {
        string src = Path.Combine(TempDir, "hooks.cob");
        File.WriteAllText(src, string.Join("\n",
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. HOOKS.",
            "       REPLACE ==:V:== BY ==VALUE 1==.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       >>PUSH LISTING",
            "       >>LISTING OFF",
            "       01 A PIC 9 :V:.",
            "       >>POP LISTING",
            "       >>PAGE",
            "       >>DISPLAY \"HOOKS\"",
            "       COPY HOOKBOOK SUPPRESS REPLACING ==:N:== BY ==B==.",
            "       PROCEDURE DIVISION.",
            "           STOP RUN."));
        File.WriteAllText(Path.Combine(TempDir, "HOOKBOOK.cpy"), "       >>LISTING OFF\n       01 :N: PIC X.\n");
        return (new CompilerDriver.Options(src, DialectLevel: 2023, CopyPaths: [TempDir], CheckOnly: true, Listing: listing), src);
    }

    /// <summary>Without a listing, a program that reaches every step-4a recording point compiles with the front end recording
    /// into <see cref="TextStageTrace.None"/>, whose recording methods throw: so each recording call asks
    /// <see cref="TextStageTrace.Records"/> first (DESIGN-listing.md section 9), and the DISPLAY directive's LISTING destination is
    /// a <see cref="CompileOutputStream.Listing"/> transfer for the command line to write.</summary>
    [Fact]
    public void WithoutAListing_TheDirectiveHooksRecordNothing()
    {
        var (options, src) = WriteHooks(listing: null);
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: false);
        var bag = new DiagnosticBag();
        Assert.NotNull(frontend.Parse(src, bag));
        Assert.DoesNotContain(bag.Diagnostics, d => d.IsError);
        Assert.Same(TextStageTrace.None, frontend.ListingTrace);
        Assert.Equal([new CompileOutputLine(CompileOutputStream.Listing, "HOOKS")], bag.CompileOutput);
    }

    /// <summary>The control: with a listing, the same program records each fact, and transfers no line to an output stream.</summary>
    [Fact]
    public void WithAListing_TheDirectiveHooksRecord()
    {
        var (options, src) = WriteHooks(new WiseOwl.Cobol.Listing.ListingRequest());
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs { DigestsFiles = true }, recordsListing: true);
        var bag = new DiagnosticBag();
        Assert.NotNull(frontend.Parse(src, bag));
        var trace = frontend.ListingTrace;
        Assert.Equal([TextListingSwitchCause.Directive, TextListingSwitchCause.Pop, TextListingSwitchCause.Directive,
            TextListingSwitchCause.InclusionEnd], trace.ListingSwitches.Select(s => s.Cause));
        Assert.Single(trace.PageDirectives);
        Assert.Equal("HOOKS", Assert.Single(trace.Displays).Text);
        Assert.True(Assert.Single(trace.Inclusions).Suppress);
        Assert.Equal(2, trace.ReplacedLines.Count);   // the REPLACE match on A's line, the REPLACING match in the library text
        Assert.Empty(bag.CompileOutput);
    }

    /// <summary>The bind of <see cref="Source"/> under an edition context that does (<paramref name="recordsSymbols"/>) or does not
    /// ask for the symbol table, through the front end and the binder the driver uses.</summary>
    private WiseOwl.Cobol.Binding.BinderDriver Bind(bool recordsSymbols, bool recordsNames = true, bool recordsReferences = false)
    {
        var (options, src) = Write(listing: null);
        var frontend = CompilerDriver.NewFrontend(options, new CompilationInputs(), recordsListing: false);
        var tree = frontend.Parse(src, new DiagnosticBag());
        Assert.NotNull(tree);
        var binder = new WiseOwl.Cobol.Binding.BinderDriver();
        var edition = new WiseOwl.Cobol.Binding.EditionContext(85, false) { LineMap = frontend.LineMap, RecordsSymbols = recordsSymbols, RecordsNames = recordsNames,
            RecordsReferences = recordsReferences };
        binder.Bind(tree, edition, frontend.Directives);
        Assert.False(edition.HasErrors, string.Join("\n", edition.Diagnostics));
        return binder;
    }

    /// <summary>Section 9: a bind that no listing section reads the symbol table of finishes none (<c>EditionContext.RecordsSymbols</c>
    /// false, which the driver sets only through <c>ListingFacts.ReadsSymbols</c> of a request): no snapshot, no symbol-table work.</summary>
    [Fact]
    public void WithoutASymbolTableSection_TheBindFinishesNoSymbolTable()
    {
        Assert.Null(Bind(recordsSymbols: false).Symbols);
        Assert.False(ListingFacts.ReadsSymbols(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.Source) }));
    }

    /// <summary>The control: asked, the same bind finishes the table, the program, the copied item X and the paragraph MAIN-PARA
    /// declared in it (each an entry of the bind's, DESIGN-listing.md section 6.7.1, "Step 6b, the entry ids").</summary>
    [Fact]
    public void WithASymbolTableSection_TheBindFinishesTheSymbolTable()
    {
        var symbols = Bind(recordsSymbols: true).Symbols;
        Assert.NotNull(symbols);
        Assert.Equal([(WiseOwl.Cobol.Symbols.NameClass.Program, "NOWORK"), (WiseOwl.Cobol.Symbols.NameClass.Data, "X"),
                (WiseOwl.Cobol.Symbols.NameClass.Procedure, "MAIN-PARA")],
            symbols.Entries.Select(e => (e.Class, e.Name)));
        Assert.True(ListingFacts.ReadsSymbols(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.DataMap) }));
        Assert.True(ListingFacts.ReadsSymbols(new ListingRequest()));   // the standard sections hold the program map
    }

    /// <summary>Section 9 (step 7b): a bind no cross-reference reads records no occurrence and links no place, the data map's own bind
    /// included; asked, the same bind records the program's one reference, DISPLAY's X, read.</summary>
    [Fact]
    public void WithoutTheCrossReference_TheBindRecordsNoOccurrence()
    {
        var store = Bind(recordsSymbols: true).Session.OoClasses.Entries;
        Assert.False(store.RecordsReferences);
        Assert.Empty(store.Occurrences());
        Assert.Equal(0, store.LinkMark);
        Assert.False(ListingFacts.ReadsReferences(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.DataMap) }));
        Assert.False(ListingFacts.ReadsReferences(new ListingRequest()));

        var recorded = Bind(recordsSymbols: true, recordsReferences: true).Session.OoClasses.Entries;
        Assert.Equal(WiseOwl.Cobol.Symbols.SymbolUse.Read, Assert.Single(recorded.Occurrences()).Occurrence.Use);
        Assert.True(ListingFacts.ReadsReferences(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.CrossReference) }));
    }

    /// <summary>Section 9: the program map alone finishes the program entries and no other name; only the data map reads them.</summary>
    [Fact]
    public void ThePrograms_WithoutTheDataMap_FinishOnlyTheProgramEntries()
    {
        var symbols = Bind(recordsSymbols: true, recordsNames: false).Symbols;
        Assert.Equal([(WiseOwl.Cobol.Symbols.NameClass.Program, "NOWORK")], symbols!.Entries.Select(e => (e.Class, e.Name)));
        Assert.False(ListingFacts.ReadsNames(new ListingRequest()));
        Assert.True(ListingFacts.ReadsNames(new ListingRequest { Sections = ListingSectionSet.All }));
    }
}
