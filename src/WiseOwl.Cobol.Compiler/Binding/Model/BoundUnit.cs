// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>One program unit of the compilation group: its identity, containment, PROGRAM-ID attributes
/// (ISO §11.10 / §8.6.6), bound model, and the GLOBAL bridges its class must emit (§13.18.27 GR2).
/// <para>Relocated from the emitter's private <c>CallUnit</c> (rearch PHASE-06 Step 2 — the BINDER owns the bound
/// model; the emitter consumes it read-only through <see cref="BoundCompilation"/>).</para></summary>
internal sealed class BoundUnit
{
    /// <summary>program-name-1 / user-function-name-1 as WRITTEN — the declared COBOL name. This is what a
    /// reference by a user-defined WORD resolves against (§10.7.3 SR2's END PROGRAM match, §14.9.4.3 SR15's
    /// AS NESTED literal, <c>FUNCTION user-function-name</c>) and what FUNCTION MODULE-NAME reports
    /// (CONFORMANCE.md DOC-A.1-135, the §15.65.4 r4 determination). NEVER the AS literal.</summary>
    public required string Name;
    /// <summary>The name externalized to the operating environment: the <c>AS</c> phrase's literal-1 when the
    /// identification division specifies one, else <see cref="Name"/> (ISO §11.10.4 GR1 "Literal-1, if
    /// specified, is the name of the program that is externalized to the operating environment"; §11.5.4 GR1/GR2
    /// for a function; §8.3.2.2 2) is the rule that makes the two distinct). This is what a reference by a
    /// LITERAL resolves against, and §8.3.2.2 makes that exhaustive: "Externalized names shall be referenced
    /// in a source element only: 1) in the AS phrase in a repository paragraph entry, 2) in the AS phrase in
    /// an EXTERNAL clause, 3) as program-name in a CALL statement, 4) as program-name in a CANCEL statement,
    /// 5) as program-name in a program-address-identifier, 6) as method-name in an INVOKE statement or inline
    /// method invocation.  All other references to names for which externalization is permitted shall be
    /// specified using the user-defined words, as opposed to the externalized names." It is also the key of
    /// §12.3.8.4 GR10 a)'s program-definition search. kb/Work PB303.</summary>
    public required string ExternalizedName;
    public required string ClassName;
    /// <summary>Where this unit's source text STARTS in the compilation group — the character offset of its first
    /// token, a total order over every source element of the group (§12.3.8.4 GR10 a) / GR11 a): "a program
    /// definition specified PREVIOUSLY in the same compilation group" means a definition whose position is less
    /// than the referencing element's; kb/Work PB989). A contained program's own context carries no start token,
    /// so this reads the first token under it.</summary>
    public required int SourcePosition;
    public BoundUnit? Parent;
    public List<BoundUnit> Children = [];
    public bool Initial, Common, Recursive;
    /// <summary>True for a FUNCTION-ID unit (ISO §9.4 — a user-defined function; program-shaped except it
    /// RETURNs a value and always possesses the recursive attribute).</summary>
    public bool IsFunction;
    /// <summary>True for a prototype definition — FUNCTION-ID … IS PROTOTYPE or PROGRAM-ID … IS PROTOTYPE
    /// (ISO §11.10.2 Format 2; kb/Work PB894) — a signature-only unit (§10.6.2 SR4: LINKAGE-only data + a
    /// header-only procedure division, screened by <c>PrototypeUnitRules</c>). A function prototype contributes
    /// its signature to the user-function table (M2-UDF-3), a program prototype to the REPOSITORY program-specifier
    /// resolution (§12.3.8.4 GR10 b)); neither emits a body nor registers in the run unit — the definition
    /// (in-group, else separately compiled) is the activation target. <see cref="IsFunction"/> says which kind.</summary>
    public bool IsPrototype;
    public DataBinder Data = null!;
    public ReferenceResolver Refs = null!;
    /// <summary>The Places of this unit's data items, built once binding is done (<c>PlaceTablePass</c>) - what the code
    /// generator reads in place of asking the resolver (kb/Work PB2375).</summary>
    public PlaceTable Places = null!;
    public BoundProgram Bound = null!;
    public List<CallBridge> Bridges = [];
    /// <summary>The unit's external-repository record (ISO §8.13; DESIGN-external-repository §6.1), written by the binder at
    /// the end of the bind for every unit the compilation publishes (<see cref="BoundCompilation.EntryUnits"/>) when the
    /// compilation updates the repository (<see cref="BoundCompilation.PublishesSignatures"/>); null otherwise. A backend
    /// transcribes it and never encodes one (kb/Work PB4319).</summary>
    public RepositoryRecord? Record;

    /// <summary>The §14.8.2 description of each FORMAL, in <c>Linkage.Formals</c> order, and the §14.8.3 description of the
    /// RETURNING item (<see cref="ReturningDescription"/>), each null for an item with no crossing form: what §14.9.4.4 GR3 d)
    /// compares at a CALL located by name at run time. Built by the binder's one description builder
    /// (<c>ActivationDescriptions</c>) at the end of the bind (<c>BackendAnswers</c>; kb/Work PB2403) for a unit a backend
    /// describes: one that checks EC-PROGRAM-ARG-MISMATCH itself (its registration states them) or publishes a host entry
    /// (<see cref="BoundCompilation.EntryUnits"/>, whose arguments state them). Empty for any other unit.</summary>
    public IReadOnlyList<WiseOwl.Cobol.Runtime.ActivationDescription?> FormalDescriptions = [];
    /// <summary>The RETURNING item's description (see <see cref="FormalDescriptions"/>); null when the unit has none or is not
    /// described.</summary>
    public WiseOwl.Cobol.Runtime.ActivationDescription? ReturningDescription;
    /// <summary>The pairs of formals (earlier position, later position) whose descriptions are the same in both directions of
    /// §9.3.8.2.3's pair mode (<c>OoConformance.SignatureDifference</c>): the host entry gives one variable passed to both one
    /// storage (DESIGN-external-repository §4.3 "One storage for one variable passed twice"), and refuses it for any other pair.
    /// Written at the end of the bind for an entry unit (<see cref="BoundCompilation.EntryUnits"/>); empty for any other.</summary>
    public IReadOnlySet<(int Earlier, int Later)> FormalsOfOneDescription = new HashSet<(int, int)>();
    /// <summary>The classes and interfaces of the unit's PROCEDURE DIVISION header RAISING phrase, resolved (ISO §14.6.13.1.5
    /// items 1 and 3; <see cref="RaisingObject"/>), written at the end of the bind; empty when the header names none.</summary>
    public IReadOnlyList<RaisingObject> RaisingObjects = [];

    /// <summary>What this program, function or prototype unit is, as the repository records it (DESIGN-external-repository
    /// §5.1). The one answer: the unit's record, the module's <c>Units</c> index and every <c>[CobolUnit]</c> read it.</summary>
    public RepositoryUnitKind RepositoryKind => (IsFunction, IsPrototype) switch
    {
        (false, false) => RepositoryUnitKind.Program,
        (true, false) => RepositoryUnitKind.Function,
        (false, true) => RepositoryUnitKind.ProgramPrototype,
        (true, true) => RepositoryUnitKind.FunctionPrototype,
    };

    /// <summary>The run-unit-unique registry key, the program table's containment path id (§8.4.6.3, the scope of
    /// program-names), formed by the run-time table's own two formers: an outermost unit's is rooted at its EXTERNALIZED
    /// name, which alone identifies it in a run unit (§8.3.2.2 2), so two separately compiled modules declaring the same
    /// program-name never share one (kb/Work PB2843). A run-time datum, never C# text (DESIGN-backend-abstraction §1.1).</summary>
    public string RegistryKey => Parent is null
        ? WiseOwl.Cobol.Runtime.ProgramTable.OutermostPath(ExternalizedName)
        : WiseOwl.Cobol.Runtime.ProgramTable.ContainedPath(Parent.RegistryKey, Name);

    /// <summary>⛔ THE ONE ANCHOR OF A CONTAINER MEMBER (kb/Work PB1133): how a program <paramref name="depth"/>
    /// containment levels INSIDE this unit reaches this unit's class member <paramref name="csMember"/> — through the
    /// chain of containing instances for an instance member, through this unit's CLASS for a static one
    /// (<see cref="StorageTierSlice.IsStaticMember"/>: ISO §13.5.4 GR1's static data is one copy on the class, which no
    /// instance names). Every place a contained program reaches into a container's data asks here: the GLOBAL bridges
    /// (<see cref="LinkageBinder.GlobalBridgesOf"/>) and the inherited FILE STATUS places (<c>ProgramEmitter</c>). The
    /// answer is structure, which each backend spells (kb/Work PB4320).</summary>
    public ContainerAnchor AnchorOf(string csMember, int depth) =>
        Data.State.StorageTier.IsStaticMember(csMember) ? new ContainerAnchor.Static(ClassName) : new ContainerAnchor.Instance(depth);

    /// <summary>The FORMAL PARAMETERS of containing programs that this unit sees as GLOBAL names (ISO §13.18.27.4 GR2),
    /// read off the bridges that reach them (kb/Work PB2096). Their member names are Uid-keyed and bridged under the same
    /// names, so a contained program renders them as the container does: a forward of one BY REFERENCE passes on the
    /// incoming carrier, its presence (§8.8.4.8.4 GR1c) and the argument area it occupies (§14.2.3 GR8).</summary>
    public IEnumerable<LinkageFormal> InheritedFormals => Bridges.Select(b => b.Formal).OfType<LinkageFormal>().Distinct();
}

/// <summary>One inherited-GLOBAL bridge a nested class emits: a property aliasing the containing instance's
/// member (ISO §13.18.27 GR2 — the name is visible in every contained program; the STORAGE stays the
/// container's). Built ONLY by <c>LinkageBinder.GlobalBridgesOf</c>, the one list of the members a reference to a
/// global root can render (kb/Work PB1009). <paramref name="Formal"/> is set when the bridged member belongs to a
/// FORMAL PARAMETER of the container — its carrier (<see cref="CallBridgeKind.Carrier"/>, whose cell type the emitter
/// derives from it), its argument area (<see cref="CallBridgeKind.ArgumentArea"/>) or an area formal's data-address
/// pointer (<see cref="CallBridgeKind.Address"/>) — so the contained program knows the GLOBAL formals in its scope
/// (<see cref="BoundUnit.InheritedFormals"/>; kb/Work PB2096). <paramref name="MemberClrName"/> is the member's CLR name, which
/// the bridge declares under the same name, and <paramref name="Anchor"/> is how the contained program reaches the
/// container's member of that name (<see cref="BoundUnit.AnchorOf"/>) — two facts the backend joins (kb/Work
/// PB4320).</summary>
internal sealed record CallBridge(string MemberClrName, ContainerAnchor Anchor, CallBridgeKind Kind, DataItem? Item,
    LinkageFormal? Formal = null);

/// <summary>The member shape a <see cref="CallBridge"/> aliases (kb/Work PB1009).</summary>
internal enum CallBridgeKind
{
    /// <summary>A global root's own typed field (<c>ref {ElementType}</c>).</summary>
    Field,
    /// <summary>A Tier-B class's string backing (<c>ref string</c>).</summary>
    Backing,
    /// <summary>The <c>StorageCell</c> behind a cell-backed class (EXTERNAL / BASED / ADDRESS-OF-taken) — a
    /// get-only alias, since two of the three are computed properties.</summary>
    Cell,
    /// <summary>A BASED class's implicit data-address pointer (<c>ref ManagedPointer</c>, ISO §13.18.5.4 GR2).</summary>
    Address,
    /// <summary>A carrier-resident LINKAGE formal's <c>ManagedPointer&lt;T&gt;</c> carrier (its field IS
    /// <c>carrier.Value</c>).</summary>
    Carrier,
    /// <summary>A carrier-resident BY REFERENCE formal's ARGUMENT AREA (<c>ref CellPointer?</c>,
    /// <c>LinkageFormal.ArgumentAreaField</c>): what a contained program's forward of the GLOBAL formal passes on, so an
    /// area formal of the next activation occupies the argument's own storage (ISO §14.2.3 GR8; kb/Work PB2096).</summary>
    ArgumentArea,
    /// <summary>An INDEXED BY <c>long</c> field of a global table.</summary>
    Index,
    /// <summary>A GLOBAL formal's omitted-argument presence member (a plain <c>bool</c>, not a ref — kb/Work PB971).</summary>
    Presence,
}
