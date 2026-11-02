// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The data-binding state of ONE bound unit (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296,
/// step 1 of the <see cref="DataBinder"/> extract): the facts the data pipeline produces, in the two shapes the code has.
/// <list type="bullet">
/// <item>A VALUE SLICE is a record of the facts ONE producer writes, frozen when its producer is done and read only after
/// it: the unit's checking flags (<see cref="UnitChecking"/>, written by <see cref="CheckingFold"/>).</item>
/// <item>A FACT SET (<see cref="FactSet{T}"/>) accumulates across phases and is append-only, with an ordered read: the
/// four below, each with its declared appending phases.</item>
/// </list>
/// The procedure-bind state is NOT here: it is <see cref="ProcedureBindState"/>, which no data phase reads and which never
/// joins the bound unit's result.
/// </summary>
public sealed class DataBindState
{
    /// <summary>What the unit inherits from the programs that contain it (<see cref="InheritedGlobals"/>), written by
    /// <see cref="InheritStep"/> before the unit's first data phase; empty for an outermost program and an OO element.</summary>
    public InheritedGlobals Inherited { get; } = new();

    /// <summary>The check that a per-unit resolve pass appends only to the fact sets it declared
    /// (<see cref="Passes.PassViews"/>); opened by <c>DataBinder.BindResolve</c> around each pass.</summary>
    internal AppendGate Gate { get; } = new();

    /// <summary>The unit's checking slice (<see cref="Binding.UnitChecking"/>): <see cref="UnitChecking.None"/> until
    /// <see cref="CheckingFold"/> writes it, once, after the unit's data phases; read by the reference resolver while the
    /// procedure division binds and by code generation.</summary>
    public UnitChecking UnitChecking
    {
        get => field ?? UnitChecking.None;
        internal set => field = field is null ? value
            : throw new InvalidOperationException("DataBindState.UnitChecking is written once, by CheckingFold (kb/Work PB2296)");
    }

    /// <summary>Group items used as a WHOLE character-image operand (MOVE / DISPLAY / compare / ACCEPT / record I-O /
    /// whole-group CORRESPONDING pair / boundary formal). Appended at FilesResolved by
    /// <c>MarkFileRecordImageLeavesPass</c> (the FILE record areas) and at UsageCollected by
    /// <see cref="Passes.UsageCollectionPass"/> (the boundary formals and the whole-group operands of the bound tree);
    /// read whole by <c>StorageFormPass</c>, which promotes the leaves of each group to their character image (ISO
    /// §14.9.25.4 GR4: a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).</summary>
    public FactSet<DataItem> WholeGroupReferenced { get; }

    /// <summary>Every elementary item some bind-time rule forces to store its CHARACTER IMAGE: Tier-B REDEFINES leaves
    /// (appended at RedefinesClassified, <c>ClassifyRedefinesClassesPass</c>), CALL-cell REDEFINES leaves, FILE record leaves
    /// and report print faces (appended at FilesResolved; <see cref="FactSetContract"/> holds the phases), figurative-fill and
    /// ref-mod-store receivers (appended at ProcedureBound, <c>MoveBinder</c>). Read EARLY by
    /// <see cref="ReferenceResolver"/>'s <c>NumericImagePlace</c> wrap decision (<see cref="IReadsEarly{T}"/>: a fact an
    /// earlier statement's bind recorded is visible to a later statement's), and read whole by <c>StorageFormPass</c>,
    /// which unions it with the compiler-temp re-sync and the whole-group promotion to compute
    /// <see cref="DataItem.Storage"/>.</summary>
    public FactSet<DataItem> ImageForcedItems { get; }

    /// <summary>ELEMENTARY items a CHARACTER CHANNEL can write (kb/Work PB992), appended at UsageCollected by
    /// <see cref="Passes.UsageCollectionPass"/> beside <see cref="WholeGroupReferenced"/> and promoted by
    /// <c>StorageFormPass</c> exactly as a whole-referenced group's leaves are. A channel that delivers CHARACTERS rather
    /// than a value — §14.9.25.4 GR4's group move into an elementary receiver, and §14.2.3 GR8's shared storage between a
    /// BY REFERENCE argument and its formal parameter, where §14.8.2.3.2 rule 1 admits a formal of any description "of
    /// the same length", and the CALL RETURNING receiver — can deposit content that is not a numeric value, and a native
    /// carrier holds only a value.</summary>
    public FactSet<DataItem> CharacterChannelItems { get; }

    /// <summary>The parse nodes (and subscript tokens) the binder RESOLVED to a function activation or an object-property
    /// accessor invocation although the text carries no syntactic marker of one: a function-identifier written without
    /// the word FUNCTION (§8.4.3.2.3 SR2 — <c>UPPER-CASE (X)</c> under <c>REPOSITORY. FUNCTION ALL INTRINSIC</c>) and a
    /// property reference (§8.4.3.9 — <c>prop OF obj</c>, whose get/set accessor is a METHOD invocation). Appended at
    /// ProcedureBound, where the binder makes the decision (<c>IntrinsicBinder.KeywordOmittedFunction</c>,
    /// <c>ReferenceResolver.IsFunctionBearing</c>, <c>ReferenceResolver.ResolveObjectProperty</c>), and read whole by
    /// <see cref="Validation.FlagConformancePass"/> for ISO §7.3.14.4 GR4 b ("the source element calls any function, or
    /// invokes any method") instead of re-deriving the rule from syntax. Identity-keyed: a node is a call site or it is
    /// not.</summary>
    public FactSet<object> ActivationSites { get; }

    /// <summary>An empty state: every fact set empty, each checking its appends through <see cref="Gate"/>.</summary>
    public DataBindState()
    {
        WholeGroupReferenced = new(FactSetKind.WholeGroupReferenced, Gate);
        ImageForcedItems = new(FactSetKind.ImageForcedItems, Gate);
        CharacterChannelItems = new(FactSetKind.CharacterChannelItems, Gate);
        ActivationSites = new(FactSetKind.ActivationSites, Gate, ReferenceEqualityComparer.Instance);
    }
}
