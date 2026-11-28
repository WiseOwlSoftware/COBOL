// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>What the front end's LISTED pass leaves for the listing (DESIGN-listing.md section 4.4): every file's
/// reference-format map, the text stages' trace, the parse's unit facts, the resultant line map, the resultant text it maps and
/// what the lexer and the parse read each stretch of that text as (section 7.2), all of one pass.</summary>
internal sealed record FrontendFacts(string SourcePath, IReadOnlyDictionary<string, ReferenceFormatMap> Formats,
    TextStageTrace Trace, SourceUnitFacts? Units, SourceLineMap LineMap, string ResultantText,
    IReadOnlyList<SourceTextSpan>? TextClasses);

/// <summary>
/// ⛔ THE LISTING'S COLLECTOR (DESIGN-listing.md section 4.6): <c>CompilerDriver</c>'s body DEPOSITS each fact here as its phase
/// finishes, and the one exit (<see cref="ListingWriter.Write"/>) builds the listing from whatever it holds. What is listed is
/// decided by this STATE, never by which return of the body was taken: no source text deposited, no listing; source deposited,
/// a listing whose every section is present or says why not. A compilation without a listing has no collector at all.
/// </summary>
/// <remarks>It names no type of the compiler pipeline (<c>CompilerDriver</c>): the builder is a layer below the composition root
/// (docs/rearchitecture/architecture-edges.json, layer <c>compiler.listing</c>), so the driver hands it plain values.</remarks>
/// <param name="request">The listing asked for.</param>
/// <param name="inputs">The compilation's ambient-input record.</param>
/// <param name="sourcePath">The source path as the compilation read it.</param>
/// <param name="outputDll">The assembly the compilation writes: the listing's default base.</param>
/// <param name="edition">The targeted edition.</param>
/// <param name="clock">The compile-clock seam (<c>IntrinsicBinder.CompileClock</c>), read through
/// <see cref="CompilationInputs.ReadCompilationTime"/> so the listing and WHEN-COMPILED share one instant.</param>
/// <param name="options">The options section's rows (DESIGN-listing.md section 6.4), built by the composition root, which alone
/// may name the compilation's options type.</param>
internal sealed class ListingFacts(ListingRequest request, CompilationInputs inputs, string sourcePath, string outputDll,
    Editions.EditionInfo edition, Func<DateTimeOffset> clock, IReadOnlyList<ListingOption> options)
{
    /// <summary>The options section's rows: every option of the compilation, then the compiler and the host.</summary>
    public IReadOnlyList<ListingOption> Options { get; } = options;

    /// <summary>The source path as the compilation read it.</summary>
    public string SourcePath { get; } = sourcePath;

    /// <summary>The assembly the compilation writes.</summary>
    public string OutputDll { get; } = outputDll;

    /// <summary>The targeted edition.</summary>
    public Editions.EditionInfo Edition { get; } = edition;

    /// <summary>The compilation's one instant (DESIGN-listing.md section 5): whichever reader comes first captures it.</summary>
    public DateTimeOffset CompilationTime() => Inputs.ReadCompilationTime(clock);

    /// <summary>The listing asked for.</summary>
    public ListingRequest Request { get; } = request;

    /// <summary>The compilation's ambient-input record: its file digests and its one instant (DESIGN-listing.md section 5).</summary>
    public CompilationInputs Inputs { get; } = inputs;

    /// <summary>Every diagnostic the compilation reported of its source (<c>CompilerDriver.Result.Diagnostics</c>): deposited by
    /// the one exit once the body has returned, since every return carries them (DESIGN-listing.md section 6.3).</summary>
    public IReadOnlyList<Frontend.Diagnostics.Diagnostic> Diagnostics { get; private set; } = [];

    /// <summary>Deposit the compilation's diagnostics.</summary>
    public void DepositDiagnostics(IReadOnlyList<Frontend.Diagnostics.Diagnostic> diagnostics) => Diagnostics = diagnostics;

    /// <summary>The finished symbol table (DESIGN-listing.md section 6.7.1), deposited once the bind finished with no error; null
    /// otherwise, and always when no section of the listing reads it (<see cref="ReadsSymbols"/>).</summary>
    public Symbols.SymbolTableSnapshot? Symbols { get; private set; }

    /// <summary>Deposit the finished symbol table.</summary>
    public void DepositSymbols(Symbols.SymbolTableSnapshot symbols) => Symbols = symbols;

    /// <summary>Whether a section the listing holds reads the symbol table: the data map, and the program map's attributes
    /// (DESIGN-listing.md sections 6.5 and 6.6). The bind finishes the table only then (<c>EditionContext.RecordsSymbols</c>).</summary>
    public static bool ReadsSymbols(ListingRequest request) =>
        ReadsNames(request) || request.Sections.Contains(ListingSection.Programs);

    /// <summary>Whether a section the listing holds reads every declared name of the symbol table, not only its program entries:
    /// the data map and the cross-reference (DESIGN-listing.md section 9).</summary>
    public static bool ReadsNames(ListingRequest request) =>
        request.Sections.Contains(ListingSection.DataMap) || ReadsReferences(request);

    /// <summary>Whether a section the listing holds reads the symbol table's occurrences: the cross-reference, which also gives the
    /// source lines their name uses and their statement margin (DESIGN-listing.md sections 6.7.1 and 9). The bind records them only
    /// then (<c>EditionContext.RecordsReferences</c>).</summary>
    public static bool ReadsReferences(ListingRequest request) => request.Sections.Contains(ListingSection.CrossReference);

    /// <summary>The assembly the compilation wrote and its portable PDB (DESIGN-listing.md section 6.9), deposited once the backend wrote
    /// them; null when no assembly was written (a failed compilation, a check-only compilation).</summary>
    public (string Assembly, string? Pdb)? ObjectCode { get; private set; }

    /// <summary>Deposit the assembly the backend wrote and its PDB, or null for an assembly written without one.</summary>
    public void DepositObjectCode(string assembly, string? pdb) => ObjectCode = (assembly, pdb);

    /// <summary>Whether a section the listing holds reads the assembly and its PDB: the object-code map and the object code
    /// (DESIGN-listing.md section 9: the PDB is read only then).</summary>
    public static bool ReadsObjectCode(ListingRequest request) =>
        request.Sections.Contains(ListingSection.ObjectMap) || request.Sections.Contains(ListingSection.ObjectCode);

    /// <summary>The front end's listed pass, or null while no source text has been read.</summary>
    public FrontendFacts? Frontend { get; private set; }

    /// <summary>Deposit the front end's listed pass, once <c>Frontend.Parse</c> has returned: nothing when the source text was
    /// never read (no reference-format map of the source exists).</summary>
    public void DepositFrontend(Frontend.Frontend frontend)
    {
        if (!frontend.ReferenceFormats.ContainsKey(SourcePath)) return;
        Frontend = new FrontendFacts(SourcePath, frontend.ReferenceFormats, frontend.ListingTrace, frontend.UnitFacts,
            frontend.LineMap, frontend.ResultantText, frontend.TextClasses);
    }
}
