// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ WHAT THE DATA BINDING OF ONE SOURCE ELEMENT HANDS A BACKEND (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1),
/// step 11 of the <c>DataBinder</c> extract; kb/Work PB2361; DESIGN-backend-abstraction.md §1.1): the frozen value slices and
/// the answers code generation reads, made ONCE by <c>DataBinder.Result</c> after the group tail, when the procedure binders'
/// two late doors (<see cref="ForestSlice.AddTemporaryRoot"/>, <see cref="StorageTierSlice.ClaimTemporaryCell"/>) are shut.
/// It holds no binder, no reference resolver and no parse context, so a backend that is not Roslyn's reads it without binding
/// anything: <see cref="BoundUnit.Data"/>, <see cref="OoClassUnit.Data"/> / <see cref="OoClassUnit.FactoryData"/> and
/// <see cref="BoundCompilation.InterfaceData"/> carry it in place of the binder.
/// <para>A fact a backend needs that is not here is ADDED here, written by <c>DataBinder.Result</c>; an emitter never reaches
/// back into the binder: a code-generation type that named <c>DataBinder</c>, <c>DataBindState</c> or a concern binder would
/// be a pair the tolerated <c>CodeGen → Binding</c> rows do not list, which <c>ArchitectureEdgesDriftTests</c> refuses. The
/// binder's own working state, the fact sets the passes append to and the procedure-bind state never join it. Its text
/// members are classified by the backend contract (<c>BackendContractDriftTests</c>).</para>
/// </summary>
internal sealed class DataBindResult
{
    /// <summary>The configuration facts the backend reads (<see cref="ConfigurationFacts"/>).</summary>
    public required ConfigurationFacts Configuration { get; init; }

    /// <summary>The type forest and its name tables (<see cref="ForestSlice"/>), frozen.</summary>
    public required ForestSlice Forest { get; init; }

    /// <summary>The files the element declares (<see cref="FileSlice"/>), frozen.</summary>
    public required FileSlice Files { get; init; }

    /// <summary>The static channels, cell backings and bridges (<see cref="StorageTierSlice"/>), frozen.</summary>
    public required StorageTierSlice StorageTier { get; init; }

    /// <summary>The report description entries the element declares and the GLOBAL ones it inherits (<see cref="ReportScope"/>).</summary>
    public required ReportScope Reports { get; init; }

    /// <summary>The element's checking flags (<see cref="Model.UnitChecking"/>).</summary>
    public required UnitChecking UnitChecking { get; init; }

    /// <summary>The LINKAGE / EXTERNAL facts the backend reads (<see cref="LinkageFacts"/>).</summary>
    public required LinkageFacts Linkage { get; init; }

    /// <summary>The range-collation carriers of the ALPHABET clauses a relation reads (ISO §12.3.7; keyed by alphabet-name).</summary>
    public required IReadOnlyDictionary<string, RangeCollationCarrier> RangeCollations { get; init; }

    /// <summary>The element's APPLY COMMIT clause (ISO §12.4.6.4), or null when it writes none.</summary>
    public required ApplyCommitModel? ApplyCommit { get; init; }

    /// <summary>The edition the element was bound under (85, 2002, 2014 or 2023).</summary>
    public required int DialectLevel { get; init; }

    /// <summary>The compilation's over-punch convention (kb/Work PB803).</summary>
    public required SignEncoding SignEncoding { get; init; }

    /// <summary>The run-time representation of a USAGE DISPLAY numeric item (<see cref="NumericDisplayForm"/>).</summary>
    public required NumericDisplayForm NumericDisplay { get; init; }

    /// <summary>The <c>&gt;&gt;LEAP-SECOND</c> state at this element (ISO §7.3.17): every §15.3 date/time function that reads a
    /// seconds subfield is told it.</summary>
    public required bool LeapSecond { get; init; }

    /// <summary>The element's WORKING-STORAGE and FILE SECTION are STATIC (a RECURSIVE-and-not-INITIAL program or a function;
    /// ISO §13.5.4 GR1, §14.6.2.3.3).</summary>
    public required bool UnitStaticWs { get; init; }

    /// <summary>⛔ THE ONE condition for both EMITTING <c>__ResetStatics</c> and REGISTERING it as the element's initial-state
    /// hook (kb/Work PB168): static storage exists, or the unit-scoped file-registration flag does, so a run-unit re-run and a
    /// post-CANCEL activation re-register their connectors (§14.6.2.3.2).</summary>
    public required bool EmitsStaticReset { get; init; }

    /// <summary>The dynamic tables whose named CAPACITY register THIS element declares (its own and its methods'): the
    /// registers its own class emits a profile for (ISO §13.18.38 GR15; kb/Work PB3165).</summary>
    public required IReadOnlyList<DataItem> DeclaredCapacityRegisterTables { get; init; }

    /// <summary>The §8.6.4 storage duration of each cell of <see cref="StorageTierSlice.AddressableBackings"/>, keyed by its
    /// cell field (kb/Work PB1216): classified once, when the result is made.</summary>
    public required IReadOnlyDictionary<string, CellLifetime> CellLifetimes { get; init; }

    /// <summary>The storage duration of the cell <paramref name="cellField"/> (<see cref="CellLifetimes"/>); a cell the
    /// element does not back is an internal error, never a default duration.</summary>
    public CellLifetime LifetimeOfCell(string cellField) =>
        CellLifetimes.TryGetValue(cellField, out var lifetime) ? lifetime
            : throw new InvalidOperationException($"DataBindResult: '{cellField}' is not a cell of this element's AddressableBackings");

    /// <summary>The cell fields whose life ends with <paramref name="lifetime"/>'s event, in declaration order.</summary>
    public IEnumerable<string> CellFieldsOf(CellLifetime lifetime) =>
        StorageTier.AddressableBackings.Where(b => LifetimeOfCell(b.CellField) == lifetime).Select(static b => b.CellField);
}

/// <summary>The configuration facts of one source element a backend reads (kb/Work PB2361), copied from its
/// <c>ConfigurationSlice</c> when the result is made: the slice itself stays with the binder, because it also holds the
/// declaration tables and the name-use recorder of the binding phases.</summary>
/// <param name="Options">The element's OPTIONS model (ISO §11.9.4).</param>
/// <param name="Classification">The OBJECT-COMPUTER CHARACTER CLASSIFICATION clause (ISO §12.3.6), or null.</param>
/// <param name="Collating">The PROGRAM COLLATING SEQUENCE for alphanumeric comparisons (ISO §12.3.6), or null.</param>
/// <param name="NationalCollating">The PROGRAM COLLATING SEQUENCE for national comparisons, or null.</param>
/// <param name="DecimalPointIsComma">SPECIAL-NAMES DECIMAL-POINT IS COMMA (ISO §12.3.7).</param>
/// <param name="ProgramSpecifierNames">The externalized names of the REPOSITORY program-specifiers (ISO §12.3.8.4 GR10), in
/// the order they were written.</param>
internal sealed record ConfigurationFacts(
    OptionsModel Options,
    ClassificationSpec? Classification,
    AlphabetDef? Collating,
    NationalAlphabetDef? NationalCollating,
    bool DecimalPointIsComma,
    IReadOnlyList<string> ProgramSpecifierNames);

/// <summary>The LINKAGE / EXTERNAL facts of one source element a backend reads (kb/Work PB2359, PB2361), the read-only views
/// of its <c>LinkageBinder</c> when the result is made.</summary>
/// <param name="Roots">The LINKAGE SECTION roots.</param>
/// <param name="Formals">The USING formals, in header order.</param>
/// <param name="Returning">The RETURNING item, or null.</param>
/// <param name="SuppressedRootFields">The root fields the emitter must not declare.</param>
/// <param name="ExternalBackings">The EXTERNAL records' run-unit backings (ISO §13.18.22).</param>
internal sealed record LinkageFacts(
    IReadOnlyList<DataItem> Roots,
    IReadOnlyList<LinkageFormal> Formals,
    DataItem? Returning,
    IReadOnlySet<string> SuppressedRootFields,
    IReadOnlyList<CallExternalBacking> ExternalBackings);
