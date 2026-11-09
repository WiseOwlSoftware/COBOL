// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// One PROCEDURE DIVISION USING formal parameter (ISO §2023:14.2.2 SR1 — a level-01/77 LINKAGE item) and its
/// caller-storage carrier (INTERPROGRAM_DESIGN D1/D2).
/// </summary>
/// <param name="Item">The LINKAGE-section 01/77 item the header USING operand names.</param>
/// <param name="Position">The 0-based positional slot (ISO §2023:14.2.3 GR2 — correspondence is positional, never by name).</param>
/// <param name="CarrierField">The emitted <c>ManagedPointer&lt;T&gt;</c> field name (<c>__lnk{Uid}</c> — Uid-keyed so a
/// contained program's ref-bridge of a GLOBAL formal cannot collide with its own formals; kb/Work PB1009).</param>
/// <param name="CarrierResident">True when the formal is CARRIER-RESIDENT: an elementary formal whose every
/// reference reads/writes the caller's storage through <c>carrier.Value</c> (the design's "one unavoidable
/// indirection" — per-access aliasing per ISO §2023:14.2.3 GR8). False for a group formal, a REDEFINED elementary
/// formal and an addressed one: those are AREA formals (<see cref="IsArea"/>), whose description is laid over the
/// argument's own storage cell — or, for a variable-length group the cell cannot carry, the one formal shape that
/// still round-trips its §2023:8.5.1.12 image at the activation boundary.</param>
/// <param name="ByValue">True when the header names this formal in a BY VALUE phrase (ISO §2023:14.2.2 using-phrase;
/// §2023:14.2.3 GR4 transitivity resolved at bind). The activated element then operates on a VALUE COPY — a detached
/// cell conformed to the formal's description (§2023:14.2.3 GR10: the argument is the sending operand of a COMPUTE
/// without ROUNDED into the allocated record); the callee's stores never reach the caller's storage.</param>
public sealed record LinkageFormal(DataItem Item, int Position, string CarrierField, bool CarrierResident,
    bool ByValue = false, bool Optional = false)
{
    /// <summary>The presence fact every consumer reads (§2023:8.8.4.8.4 GR1) — the program arm's null carrier.</summary>
    public OmittedProbe Probe => new OmittedProbe.Carrier(CarrierField);

    /// <summary>⛔ True for an AREA formal (kb/Work PB2087): its class is described like a BASED item's, and its carrier
    /// field IS that item's implicit data-address pointer, set at activation to the argument's own storage area
    /// (<c>CobolArgAdapt.Area</c>) — ISO §2023:14.2.3 GR8, "the activated runtime element operates as if the formal
    /// parameter occupies the same storage area as the argument". Derived from the class the binder claimed
    /// (<c>PtrBindBasedAndAddressablesPass</c>), so the emitter and the binder cannot disagree about which
    /// formals are areas.</summary>
    public bool IsArea => !CarrierResident && Item.Class?.BasedPointerField == CarrierField;

    /// <summary>⛔ The member holding a BY REFERENCE carrier-resident formal's ARGUMENT AREA (kb/Work PB2089) — the
    /// <c>CobolArg.Area</c> it arrived with, or null when its argument's storage is not a cell. The formal's storage IS
    /// that area (ISO §2023:14.2.3 GR8), so a forward of the formal BY REFERENCE passes it on and an area formal of the next
    /// activated element is laid over the same positions; without it the forward crossed through the carrier alone and
    /// that area formal held a copy whose copy-back undid every store made through another description meanwhile.</summary>
    public string ArgumentAreaField => $"__lna{Item.Uid}";

    /// <summary>True when the formal keeps <see cref="ArgumentAreaField"/>: a carrier-resident BY REFERENCE formal (an
    /// area formal's carrier IS its area; a BY VALUE formal's storage is a detached copy, §2023:14.2.3 GR10).</summary>
    public bool KeepsArgumentArea => CarrierResident && !ByValue;
}

/// <summary>The synthesized run-unit backing of one EXTERNAL record (ISO §2023:13.18.22 / §2023:8.6.7): the emitter
/// renders <c>private ref string {BackingClrName} =&gt; ref ExternalStore.Cell({ExternalName}, «seed»).Ref;</c>
/// and every reference windows it through the Tier-B view machinery. <see cref="Record"/> carries the record's own
/// DataItem, which is ALL the emitter needs: the seed is composed by the ONE Tier-B seeder
/// (<c>GroupImageCodec.ImageInitOf</c>), with VALUEs suppressed for a plain external item — whose VALUE takes
/// effect only during INITIALIZE, not at initial state (§2023:13.18.63 GR4a) — and honoured for a CONSTANT RECORD,
/// which §2023:11.9.10.4 GR7 DOES initialize at initial state (the one external exception: external WS items are not
/// initialized at initial state "except for those with the CONSTANT RECORD clause"). ⛔ There is no precomputed
/// InitImage field any more: it was a SECOND seeder, hand-rolled in the binder from a char-fill model that
/// predated the pinned byte forms (kb/Work PB164).</summary>
/// <param name="CellClrName">The <c>StorageCell</c> property the backing is defined over
/// (<c>RedefinesClass.BackingCellClrName</c>) — the cell holds BOTH halves of the one shared area, its byte image
/// and its MANAGED SLOTS, so a pointer-class member of an external record addresses the same storage every other
/// describer does (kb/Work PB231).</param>
public sealed record CallExternalBacking(string BackingClrName, string CellClrName, string ExternalName, int Width,
                                         DataItem Record);

/// <summary>The kind of thing an EXTERNAL entry externalizes (ISO §2023:8.3.2.2 list item 2: "data-names, file-names, and
/// record-names of items described with the EXTERNAL attribute"). A record is a data item, so a data-name and a
/// record-name are ONE kind; a file connector is the other. §2023:8.3.2.2 requires "all instances of a given name that
/// is externalized to the operating environment" to "identify the same kind of entity or item", so these two kinds
/// and the definition kinds (program, function, class, interface) are all distinct kinds of one namespace
/// (<c>BinderDriver.CheckDefinitionNameUniqueness</c>; kb/Work PB1404).</summary>
internal enum ExternalizedSubjectKind { DataItem, File }

/// <summary>One EXTERNAL file connector or record of a source element, as the externalized-name namespace sees it
/// (<see cref="LinkageBinder.ExternalizedSubjects"/>). <paramref name="Name"/> is the externalized name (§2023:13.18.22.4
/// GR5); <paramref name="Word"/> the entry's own user-defined word; <paramref name="Spelling"/> how a diagnostic
/// names the entry back to the user.</summary>
internal readonly record struct ExternalizedSubject(
    ExternalizedSubjectKind Kind, string Name, string Word, string Spelling, DiagnosticCursor At);

/// <summary>One FUNCTION-ID unit's activation signature (ISO §2023:9.4 user-defined functions; M2-UDF-1): the
/// registered function name, its PROCEDURE DIVISION RETURNING item (whose description the caller-side result
/// temporary clones — §2023:8.4.3.2.4 GR1), and its positional USING formals (§2023:14.8.2). Built by the run-unit
/// emitter BETWEEN the DATA and PROCEDURE bind phases, so a <c>FUNCTION user-name(args)</c> reference binds
/// against signatures of function units defined ANYWHERE in the compilation group, including after the caller.
/// <paramref name="Returning"/> is null only for the ill-formed no-RETURNING function (COBOL1507 — already
/// diagnosed; call sites fail loud without re-reporting).</summary>
/// <param name="Name">user-function-name-1 as written — the key every <c>FUNCTION word(…)</c> reference
/// resolves against (ISO §2023:8.4.6.6 / §2023:8.4.6.7 scope the WORD, never the AS literal).</param>
/// <param name="Externalized">The name the function is externalized under (§2023:11.5.4 GR1: "literal-1, if
/// specified, is the name of the function that is externalized to the operating environment"; else the word).
/// The activation the emitter writes names the callee THIS way, because that is the key the run-unit registry
/// holds it under — the two must not drift (kb/Work PB303).</param>
/// <param name="DefinitionCompiledHere">True when the signature is a DEFINITION this compilation group compiles
/// (§2023:12.3.8.4 GR11 a), or §2023:8.4.6.6's containing definition), so a reference to it activates the function whose formals
/// were checked at bind time (<c>RepositoryOrigin.IsDefinitionCompiledHere</c>; DESIGN-external-repository §4.2).</param>
public sealed record UserFunctionSignature(
    string Name, string Externalized, DataItem? Returning, IReadOnlyList<LinkageFormal> Formals, bool DefinitionCompiledHere)
{
    /// <summary>The items the signature describes: each formal parameter's, then the RETURNING item's when there is one.</summary>
    public IEnumerable<DataItem> SignatureItems =>
        Returning is { } returning ? Formals.Select(formal => formal.Item).Append(returning) : Formals.Select(formal => formal.Item);
}

/// <summary>ONE Format-2 callee's activation signature: its positional USING formals and its PROCEDURE DIVISION
/// RETURNING item. §2023:14.9.4.3 SR25 makes BOTH §2023:14.8.2, Parameters and §2023:14.8.3, Returning items apply to a Format-2
/// CALL, so wherever the callee is statically known the whole conformance regime is a BIND-time check — and the
/// signature carries both halves, exactly as <see cref="UserFunctionSignature"/> does.
/// <para>TWO producers reach the same shape, which is why this type is not named for either of them (kb/Work
/// PB237): <c>AS NESTED</c> resolves its callee through the containment tree (§2023:14.9.4.3 SR15, PB131 + PB204), and
/// <c>program-prototype-name-1</c> resolves it through the REPOSITORY program-specifier and §2023:12.3.8.4 GR10's
/// search of the compilation group. Every §2023:14.8.2/§2023:14.8.3 check in <c>CallBinder.BindCall</c> reads THIS record,
/// so the prototype arm inherited the whole set the day it was wired in.</para>
/// <paramref name="Returning"/> is null when the callee's header declares no returning item, which §2023:14.8.3.1
/// makes a conformance fact in its own right ("if and only if").</summary>
public sealed record CalleeSignature(IReadOnlyList<LinkageFormal> Formals, DataItem? Returning)
{
    /// <summary>The items the signature describes: each formal parameter's, then the RETURNING item's when there is one
    /// (the same roster <see cref="UserFunctionSignature.SignatureItems"/> gives a function).</summary>
    public IEnumerable<DataItem> SignatureItems =>
        Returning is { } returning ? Formals.Select(formal => formal.Item).Append(returning) : Formals.Select(formal => formal.Item);
}

/// <summary>One §2023:12.3.8.2 program-specifier as WRITTEN — <c>PROGRAM program-prototype-name-1 [AS literal-3]</c>
/// (kb/Work PB237). <paramref name="ExternalizedName"/> is literal-3 when the AS phrase is present and the
/// prototype name itself otherwise; §2023:12.3.8.4 GR10 NOTE 1 states that identity outright ("Literal-3, if specified,
/// is the externalized name of the program prototype; otherwise, the externalized name is
/// program-prototype-name-1"). Collected by <c>DataBinder.BindProgramSpecifier</c>; RESOLVED by
/// <c>BinderDriver.ProgramPrototypesOf</c>. <paramref name="At"/> is the REPOSITORY entry, where a diagnostic about its
/// resolution (REPO-1 and the external repository's, DESIGN-external-repository §14) is reported.</summary>
public sealed record ProgramSpecifier(string Name, string ExternalizedName, Antlr4.Runtime.ParserRuleContext At);

/// <summary>The kind of a REPOSITORY specifier whose <c>AS literal-n</c> was refused (§2023:12.3.8.3 SR2) and reported where it is
/// written: the key of <c>EditionContext.NoteSpecifierRefusalReported</c> / <c>NoteCauseReportedEarlier</c>, so a reference through
/// the refused word is a silent cascade (kb/Work PB4936). The next kind that needs it (class, interface and property specifiers,
/// kb/Work PB4773) is a member here and a method on <c>ConfigurationSlice</c>.</summary>
internal enum RefusedSpecifierKind
{
    /// <summary>A PROGRAM specifier whose <c>AS literal-3</c> was refused.</summary>
    Program,

    /// <summary>A FUNCTION specifier whose <c>AS literal-5</c> was refused.</summary>
    Function,
}

/// <summary>Why a WRITTEN program-specifier registers no program prototype although it declares its word (kb/Work PB4708):
/// the one fact <c>ConfigurationSlice.WhyNoPrototype</c> reads to name the right refusal. The third route — no provider answers it,
/// REPO-1 at the specifier (§2023:12.3.8.3 SR14) — has no member: it is the specifier having a row and no entry here.</summary>
internal enum ProgramSpecifierRefusal
{
    /// <summary>Its <c>AS literal-3</c> was refused (§2023:12.3.8.3 SR2), so it has no row of <c>ConfigurationSlice.ProgramSpecifiers</c>.</summary>
    Literal3,

    /// <summary>No provider answers it and the external repository's own finding refuses it, which suppresses REPO-1
    /// (<c>RepositoryResolver.ReportUnanswered</c>).</summary>
    RepositoryFinding,
}

/// <summary>One §2023:12.3.8.2 user-defined-function-specifier as WRITTEN — <c>FUNCTION function-prototype-name-1 [AS
/// literal-5]</c>. <paramref name="ExternalizedName"/> is literal-5 when the AS phrase is present and the name itself
/// otherwise (§2023:12.3.8.4 GR11 NOTE 2). <paramref name="At"/> is the REPOSITORY entry, where a diagnostic about its
/// resolution (<c>BinderDriver.UserFunctionsOf</c>, COBOL2969) is reported. Collected by
/// <c>DataBinder</c>'s REPOSITORY loop; RESOLVED by <c>RepositoryResolver.ResolveFunction</c>.</summary>
internal sealed record FunctionSpecifier(string Name, string ExternalizedName, Antlr4.Runtime.ParserRuleContext At);

/// <summary>One RESOLVED program prototype, as §2023:12.3.8.4 GR10 defines it: the declared program-prototype-name, the
/// externalized name that identifies the program to be called, and the <paramref name="Details"/> GR10 took — the
/// program's <see cref="CalleeSignature"/> and where it came from: a) / b) this compilation group, c) the external
/// repository entry that publishes it (<see cref="RepositoryResolver.ResolveProgram"/>; kb/Work PB237, PB2103;
/// DESIGN-external-repository §8.2). A specifier no provider answers is refused (REPO-1) and registers no prototype, so
/// every prototype carries its signature.</summary>
internal sealed record ProgramPrototype(string Name, string ExternalizedName, ProgramAnswer Details)
{
    /// <summary>The program's signature.</summary>
    public CalleeSignature Signature => Details.Signature;

    /// <summary>True when the program GR10 names is a DEFINITION this compilation group compiles (GR10 a), or §2023:8.4.6.8's
    /// containing program), so the program a CALL through the prototype activates by its externalized name is the one
    /// whose formals the bind checked (DESIGN-external-repository §4.2).</summary>
    public bool DefinitionCompiledHere => Details.Origin.IsDefinitionCompiledHere;
}

/// <summary>
/// The LINKAGE SECTION / EXTERNAL / GLOBAL binder of one source element (INTERPROGRAM_DESIGN D1–D5;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), kb/Work PB2359). It owns the LINKAGE facts (the roots,
/// the PROCEDURE DIVISION USING formals and RETURNING item, the §2023:13.7.3 SR4/SR5 referenceability sets), the EXTERNAL
/// records' backings, the GLOBAL roots and the fields the emitter must not declare, and every writer of them; the rest
/// of the data binder reaches it through <see cref="DataBinder.Linkage"/>, and the emitter reads the facts from there.
/// LINKAGE 01/77 items bind into the ordinary storage forest (so every verb, REDEFINES tier, level-88, and
/// OCCURS path works unchanged — ISO §2023:13.7: linkage entries are ordinary record descriptions whose STORAGE
/// belongs to the caller); the PROCEDURE DIVISION header's USING/RETURNING operands then designate which roots
/// are formal parameters (§2023:14.2.2 SR1) and how each is carried. EXTERNAL level-01/77 records become Tier-B
/// string windows over a run-unit <c>ExternalStore</c> cell (§2023:8.6.7 — one storage copy per name per run unit);
/// GLOBAL level-01/77 roots are collected for the emitter's containment inheritance (§2023:13.18.27 GR1–2).
/// <para>It binds INSIDE the data binder's passes, over the forest <paramref name="data"/> owns: the name tables, the
/// entry binder and the cell-backing forcer are the binder's, the linkage facts are this type's.</para>
/// </summary>
internal sealed class LinkageBinder(DataBinder data)
{
    /// <summary>The LINKAGE SECTION's top-level (01/77) items, in source order (ISO §2023:13.7).</summary>
    public IReadOnlyList<DataItem> Roots => _roots;
    private readonly List<DataItem> _roots = [];

    /// <summary>The PROCEDURE DIVISION USING formals, positional (ISO §2023:14.2.3 GR2).</summary>
    public IReadOnlyList<LinkageFormal> Formals => _formals;
    private readonly List<LinkageFormal> _formals = [];

    /// <summary>The PROCEDURE DIVISION RETURNING item (a LINKAGE 01/77; ISO §2023:14.2.3 GR6 — its storage is
    /// allocated in the activated element), or null. COBOL-2002+ (the grammar gates the clause).</summary>
    public DataItem? Returning { get; private set; }

    /// <summary>The C# names of class-level fields the <c>FieldEmitter</c> must NOT declare because another
    /// mechanism provides the member: a carrier-resident LINKAGE formal has no field, because its storage is reached
    /// through its carrier (<see cref="DataItem.ResidentCarrier"/> — the caller owns the storage, ISO §2023:13.7.1 /
    /// §2023:14.2.3 GR8), and an inherited
    /// GLOBAL table's index field is a <c>ref</c>-bridge to the containing instance (ISO §2023:13.18.27 GR2 —
    /// global index-names are shared, never duplicated). Written by this binder and by the OO binder
    /// (<see cref="SuppressRootField"/>). The inherited-index seeding member this comment once named was deleted by
    /// kb/Work PB919, whose per-table index declarations now carry an inherited GLOBAL index-name.</summary>
    public IReadOnlySet<string> SuppressedRootFields => _suppressedRootFields;
    private readonly HashSet<string> _suppressedRootFields = new(StringComparer.Ordinal);

    /// <summary>Declare that the member <paramref name="clrName"/> is provided by another mechanism, so the
    /// <c>FieldEmitter</c> must not declare it (<see cref="SuppressedRootFields"/>).</summary>
    internal void SuppressRootField(string clrName) => _suppressedRootFields.Add(clrName);

    /// <summary>Level-1 roots carrying an ADMITTED GLOBAL clause (ISO §2023:13.18.27.3 SR1 b) — file, working-storage,
    /// local-storage or linkage section), plus the records of a GLOBAL FD (§2023:13.18.27.4 GR1) — visible to every
    /// directly/indirectly contained program (GR1–2); the emitter injects them into contained units' binders and
    /// bridges their fields into the nested classes.</summary>
    public IReadOnlyList<DataItem> GlobalRoots => _globalRoots;
    private readonly List<DataItem> _globalRoots = [];

    /// <summary>⛔ THE ONE LIST OF THIS UNIT'S MEMBERS THAT A REFERENCE TO GLOBAL ROOT <paramref name="g"/> CAN
    /// RENDER (kb/Work PB1009) — each becomes a bridge in a contained program, re-anchored by
    /// <paramref name="anchorOf"/> (<c>BoundUnit.AnchorOf</c>: the <c>__outer</c> chain for an instance member, the
    /// container's class name for a static one — kb/Work PB1133) (ISO §2023:13.18.27.4 GR2: "A statement in a program
    /// contained directly or indirectly within a program that describes a global name may reference that name
    /// without describing it again" — the storage stays the container's). A reference renders the root's STORAGE member and, for the residences that
    /// have them, the members around it, so every residence §2023:13.18.27.3 SR1 b) admits (file, working-storage,
    /// local-storage, linkage) is answered here:
    /// <list type="bullet">
    /// <item>a Tier-B class (REDEFINES / EXTERNAL / BASED / ADDRESS-OF-taken) → its string backing, plus the
    /// <c>StorageCell</c> behind it when the class is cell-backed (a pointer-class member's slot and an
    /// <c>ADDRESS OF</c> both name the cell), plus a BASED class's implicit data-address pointer (every
    /// reference displaces by it, and SET ADDRESS OF / ALLOCATE / FREE write it);</item>
    /// <item>a CARRIER-RESIDENT formal → its <c>ManagedPointer</c> carrier (its "field" IS
    /// <c>carrier.Value</c>, so the bridge is the carrier, never the accessor text), and, BY REFERENCE, the argument
    /// area it occupies (kb/Work PB2096);</item>
    /// <item>any other root → its field;</item>
    /// <item>a guarded formal → its omitted-presence member (kb/Work PB971); a table → its index fields.</item>
    /// </list>
    /// ⛔ A new residence adds its member HERE, and <c>GlobalBridgeResidenceDriftTests</c> is red until it does.
    /// <para>Each bridge carries its member's NAME and its <see cref="ContainerAnchor"/> as two facts, and the backend
    /// spells the access (kb/Work PB4320 — the bridge used to hold the joined text <c>anchor + member</c>).</para></summary>
    internal IEnumerable<CallBridge> GlobalBridgesOf(DataItem g, Func<string, ContainerAnchor> anchorOf)
    {
        foreach (var m in GlobalBridgeMembersOf(g))
            yield return new CallBridge(m.MemberClrName, anchorOf(m.MemberClrName), m.Kind, m.Item, m.Formal);
    }

    /// <summary>One member <see cref="GlobalBridgesOf"/> bridges, before it is anchored in a contained program.</summary>
    private readonly record struct GlobalBridgeMember(string MemberClrName, CallBridgeKind Kind, DataItem? Item,
        LinkageFormal? Formal = null);

    /// <summary>The members of <see cref="GlobalBridgesOf"/>, unanchored — also the names
    /// <see cref="InheritedMemberNamesOf"/> reserves, so the two cannot list different members.</summary>
    private IEnumerable<GlobalBridgeMember> GlobalBridgeMembersOf(DataItem g)
    {
        if (g.Class is { Tier: RedefinesTier.StringCanonical } cls)
        {
            yield return new(cls.BackingClrName, CallBridgeKind.Backing, null);
            if (cls.IsCellBacked)
                yield return new(cls.BackingCellClrName, CallBridgeKind.Cell, null);
            // An AREA formal's carrier IS this pointer (LinkageFormal.IsArea), so the bridge names the formal too.
            if (cls.BasedPointerField is { } addr)
                yield return new(addr, CallBridgeKind.Address, null,
                    _formals.FirstOrDefault(f => f.IsArea && f.CarrierField == addr));
        }
        else
        {
            // A Tier-A (Alias) redefiner owns NO member: every reference to it renders its canonical's storage
            // (PlaceBuilder.PlaceForItem's accessItem), so that is the member a bridge must name — the
            // redefiner's own name is one the container never declares (CS1061, kb/Work PB1523). §2023:13.18.27.4 GR3:
            // only the SUBJECT of the REDEFINES carries the global attribute, so the canonical need not be global
            // itself; the storage it names is the one both entries share (§2023:13.18.44.4 GR1).
            var anchor = AnchorOf(g);
            if (_formals.FirstOrDefault(f => f.CarrierResident && ReferenceEquals(f.Item, anchor)) is { } rf)
            {
                yield return new(rf.CarrierField, CallBridgeKind.Carrier, anchor, rf);
                // Its argument area (kb/Work PB2096): a contained program's BY REFERENCE forward of this formal passes
                // the area on, as the container's own forward does (ISO §2023:14.2.3 GR8).
                if (rf.KeepsArgumentArea)
                    yield return new(rf.ArgumentAreaField, CallBridgeKind.ArgumentArea, anchor, rf);
            }
            else
                yield return new(anchor.ClrName, CallBridgeKind.Field, anchor);
            if (!ReferenceEquals(anchor, g))
                foreach (var idx in IndexDeclaration.Under(anchor))
                    yield return new(idx.Cell, CallBridgeKind.Index, null);
            if (anchor.OmittedGuard is { } ag && !ReferenceEquals(anchor, g))
                yield return new(ag.Presence, CallBridgeKind.Presence, anchor);
        }
        // A GLOBAL FORMAL PARAMETER of the container: its guarded references in the contained program read the
        // container's presence member under the same Uid-keyed name (kb/Work PB971).
        if (g.OmittedGuard is { } og)
            yield return new(og.Presence, CallBridgeKind.Presence, g);
        foreach (var idx in IndexDeclaration.Under(g))
            yield return new(idx.Cell, CallBridgeKind.Index, null);
    }

    /// <summary>The item whose emitted member a reference to <paramref name="item"/> renders: itself, except a Tier-A
    /// (Alias) REDEFINES view, which owns no member and reads its canonical's (ISO §2023:13.18.44.4 GR1 — one storage
    /// area; the one rule <c>PlaceBuilder.PlaceForItem</c> applies to every reference).</summary>
    private static DataItem AnchorOf(DataItem item) =>
        item.Class is { Tier: RedefinesTier.Alias } cls && !item.IsCanonical ? cls.Canonical : item;


    /// <summary>⛔ EVERY C# MEMBER NAME THIS UNIT'S GLOBAL ROOT <paramref name="g"/> OCCUPIES IN A CONTAINED UNIT'S
    /// CLASS — the bridge members <see cref="GlobalBridgesOf"/> emits AND the root's own C# name (with its REDEFINES
    /// canonical's), because that name is the STEM every derived member is spelled from: a REDEFINES class's
    /// <c>_redef_</c> backing, its <c>_scell_</c> cell, a BASED class's <c>__addr_</c> pointer
    /// (<see cref="Model.RedefinesClass.BackingClrName"/>). A contained unit uniques its local roots against these, so
    /// a local stem never equals an inherited one and no derived member can re-spell a bridge (those prefixes cannot
    /// begin a COBOL word). Reserving only the bridge members left a local <c>01 G</c> + <c>01 K REDEFINES G</c> the
    /// stem <c>G</c>, and its class backing re-spelled the container's <c>_redef_G</c> bridge (CS0102; wave 69 Z,
    /// found by the train-68b review).</summary>
    internal IEnumerable<string> InheritedMemberNamesOf(DataItem g)
    {
        foreach (var member in GlobalBridgeMembersOf(g)) yield return member.MemberClrName;
        // A carrier-resident formal declares no member of its own name and derives none from it: its one member is
        // the carrier, a bridge member above (kb/Work PB4320).
        if (g.ResidentCarrier is null) yield return g.ClrName;
        if (g.Class is { } cls) yield return cls.Canonical.ClrName;
    }


    /// <summary>The EXTERNAL records' synthesized run-unit backings (ISO §2023:13.18.22; emitted as
    /// <c>ref</c>-properties over <c>ExternalStore</c>). (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<CallExternalBacking> ExternalBackings => _externalBackings;
    private readonly List<CallExternalBacking> _externalBackings = [];

    /// <summary>
    /// Bind the LINKAGE SECTION and the PROCEDURE DIVISION header's USING/RETURNING operands. Runs inside
    /// <c>Bind</c> right after WORKING-STORAGE (so linkage items join the same forest, name index, and
    /// post-build passes — REDEFINES classification, SIGN inheritance, index resolution all apply, ISO §2023:13.7.3).
    /// </summary>
    internal void CallBindLinkage(Core.ProgramUnitContext program, CsNameScope rootNames)
    {
        if (program.dataDivision()?.linkageSection() is { } ls)
            _roots.AddRange(data.BindEntries(ls.dataDescriptionEntry(), rootNames, EntrySection.Linkage));

        if (program.procedureDivision() is not { } pd)
        {
            SealLinkageRules(_roots, [], null, data.UnitIsFunction);
            AnyLengthValidateUnit();
            return;
        }

        // The using-phrase modes (ISO §2023:14.2.2 :23636 — { [BY REFERENCE] {[OPTIONAL] d}… | BY VALUE {d}… }…):
        // BY REFERENCE / BY VALUE are TRANSITIVE across the parameters that follow until the other phrase
        // appears; BY REFERENCE is assumed before the first phrase (§2023:14.2.3 GR4). Threaded flat over the
        // grammar's one-parameter-per-node shape (the CALL callArgument precedent).
        int pos = 0;
        bool byValue = false;
        var header = new ProcedureHeaderScreen(data.Edition, "PROCEDURE DIVISION");   // §2023:14.2.2 SR1/SR5/SR6 — the ONE screen (kb/Work PB1145)
        // The names any ADDRESS OF in this unit or a contained one takes (the SAME scan the pointer pass forces
        // cells from — a GLOBAL formal can be addressed from a contained program, kb/Work PB1009/PB1019).
        // Asked only of a unit that has formals to classify (kb/Work PB4302): the set is read per formal, and a procedure
        // division with no USING phrase would otherwise collect every contained program's names to read none of them.
        var formals = pd.usingClause()?.usingParameter() ?? [];
        var addressed = formals.Length == 0
            ? new HashSet<string>(CobolNames.Comparer)
            : data.ClaimScans.AddressTargets(pd)
                .Concat(program.nestedProgram().SelectMany(data.ClaimScans.AddressTargets))
                .Select(t => t.Name).ToHashSet(CobolNames.Comparer);
        foreach (var prm in formals)
        {
            using var _ = data.Edition.At(prm);
            Core.DataReferenceContext dref;
            bool optional = false;
            if (prm.usingByValue() is { } vb) { byValue = true; dref = vb.dataReference(); }
            else if (prm.usingByReference() is { } rb)
            {
                byValue = false;
                dref = rb.dataReference();
                optional = rb.OPTIONAL() is not null;
            }
            else { dref = prm.dataReference(); optional = prm.OPTIONAL() is not null; }
            // §2023:14.2.2's OPTIONAL phrase now CARRIES (kb/Work PB133 wave C): the formal records the flag,
            // the SR24/count checks read it at the AS NESTED bind, and the §2023:8.8.4.8 omitted-argument
            // condition tests the carrier's presence — the staged OptionalFormal rejection is lifted.

            string pname = dref.GetText();
            var item = FindLinkageRoot(pname);
            if (item is null)
            {
                // §2023:14.2.2 SR1: each formal parameter shall be a level-01/77 entry in the LINKAGE SECTION.
                data.Edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot,
                    $"PROCEDURE DIVISION USING parameter '{pname}' is not a level-01/77 LINKAGE SECTION item "
                    + "(ISO §2023:14.2.2 SR1)");
                pos++;
                continue;
            }
            // SR1: uniqueness, and no REDEFINES / BASED clause — the ONE header screen. A BASED formal's flag clears
            // there: a carrier-resident formal's ClrName rewrite would poison the based class's BackingClrName into
            // invalid C#, so the entry binds as an ordinary (already-diagnosed) formal. A REPEATED formal is not
            // admitted a second time (`USING A A` reached the backend as a duplicate carrier member).
            if (!header.AdmitFormal(pname, item)) { pos++; continue; }
            if (optional && byValue) header.OptionalNeedsByReference(pname);   // §2023:14.2.1: OPTIONAL is a BY REFERENCE word

            if (byValue)
            {
                // §2023:14.2.2 SR2 (:23664): "Each data-name-1 specified in a BY VALUE phrase shall be defined as a
                // data item of class numeric, message-tag, object, or pointer." Class message-tag is the MCS
                // module (not modeled — undeclarable here, so unreachable). Class numeric (fixed-point or floating-
                // point) and the MANAGED classes (object / the three pointer categories) are the CARRIED legs of the
                // §2023:14.2.3 GR10 detached-cell value copy — GR10 names both: "a COMPUTE statement without the ROUNDED
                // phrase" for the numeric one, "a SET statement" for class object or pointer, which is the
                // reference copy CobolArgAdapt.SlotValue performs (kb/Work PB663). The FLOATING-POINT usage is the
                // same COMPUTE on the float lane — landed by FloatResultant into a detached float cell
                // (CobolArgAdapt.NumValue; kb/Work PB1114).
                data.CompleteDescriptionAhead(item);   // a TYPE-typed formal has no class until its subject is composed (kb/Work PB3222)
                header.ByValueClass(pname, item);   // the class set is the header screen's, shared with the method arm (PB1051)
            }

            // ⛔ THE CARRIER NAME IS Uid-KEYED, NOT POSITIONAL (kb/Work PB1009). A carrier-resident formal's
            // "field" IS `carrier.Value`, and a GLOBAL formal (§2023:13.18.27.3 SR1 b) admits GLOBAL in the linkage
            // section) is referenced from every contained program by that same text through a ref-bridge — so
            // a positional `__lnkp0` collided with the contained program's OWN first formal: its references to
            // either item read one carrier, and the bridge declared a second member of the same name. The
            // Uid-keyed name (the `__omit{Uid}` presence member's rule) cannot collide within a group.
            string carrier = $"__lnk{item.Uid}";
            // Carrier-resident = per-access aliasing of the caller's storage (design D1: "refs to LK-CTR
            // read/write LK_CTR.Value — the one unavoidable indirection"). Available for an elementary
            // fixed-point/character formal NOT overlaid by another linkage entry; a group formal (a different
            // C# struct type than the caller's — every cross-program group IS a category reinterpretation) and
            // a redefined formal are AREA formals instead, laid over the argument's own storage cell
            // (LinkageFormal.IsArea; PtrBindBasedAndAddressables claims the class — kb/Work PB2087). A BY VALUE
            // formal is carrier-resident over its DETACHED value-copy cell (§2023:14.2.3 GR10 — same per-access
            // mechanism, different carrier: CobolArgAdapt.NumValue instead of the aliasing Num view).
            bool resident = FormalIsCarrierResident(item, _roots, addressed);
            if (resident)
            {
                // Every place built over the item starts at the carrier and steps to the caller's storage
                // (PlaceBuilder.AddRoot, a CarrierValueSegment) — the item keeps its own name, which no member
                // declares (kb/Work PB4320).
                item.ResidentCarrier = new FormalCarrier.Member(carrier);
                _suppressedRootFields.Add(item.ClrName);
            }
            _formals.Add(new LinkageFormal(item, pos, carrier, resident, byValue, optional));
            pos++;
        }

        // PROCEDURE DIVISION RETURNING (§2023:14.2) + RAISING (§2023:14.2.2) — the COBOL-2002 introduction gates are now
        // VersionConformancePass ParseArm.VisitReturning/RaisingClause (14g.4, recognition, InMethodDefinition-guarded
        // so only PROGRAM-unit PDs gate — this CallBindLinkage runs for program units only, matching that scope). The
        // RETURNING §2023:14.2.3 GR6/SR1 resolution + the RAISING EC semantics (StatementBinder.Exceptions) stay bind-time.
        if (pd.returningClause()?.dataReference() is { } rref)
        {
            using var returningAt = data.Edition.At(rref);   // the RETURNING item's own position, as each USING parameter's (kb/Work PB2034)
            // §2023:14.2.3 GR6: the returning item's storage is allocated IN THE ACTIVATED element — it stays an
            // ordinary callee-local field; its value transfers out at termination (GR7, the Call copy-out).
            Returning = FindLinkageRoot(rref.GetText());
            if (Returning is null)
                data.Edition.Error(DiagnosticCatalog.ProcedureHeaderItemNotLinkageRoot,
                    $"PROCEDURE DIVISION RETURNING item '{rref.GetText()}' is not a level-01/77 LINKAGE SECTION "
                    + "item (ISO §2023:14.2.2 SR1)");
            else
                header.CheckReturning(rref.GetText(), Returning);
        }

        SealLinkageRules(_roots, _formals.ConvertAll(f => f.Item), Returning, data.UnitIsFunction);
        AnyLengthValidateUnit();
    }

    /// <summary>The ANY LENGTH placement sweep of this unit's forest (ISO §2023:13.18.2.3 SR2/SR3/SR4) — runs at the
    /// end of <see cref="CallBindLinkage"/>, when the unit's sections AND its PD-header formals are bound but no
    /// method data has (methods sweep in <c>OoDataBinder.BindMethodData</c>). SR2: an elementary level-1 LINKAGE entry of a
    /// FUNCTION or a CONTAINED program — never an outermost program (the §2023:13.18.2.3 NOTE: a prototype-less CALL
    /// cannot associate arguments with an ANY LENGTH formal) and never the object/factory paragraph of a class.
    /// SR3 (contained program): referenced in the PD header as a formal WITH THE BY REFERENCE PHRASE (stated
    /// or implied — a BY VALUE formal does not qualify, SR3a) or as the RETURNING item. SR4 (function): as a
    /// BY REFERENCE formal only — a function's ANY LENGTH RETURNING is illegal.
    /// A violation clears <see cref="DataItem.IsAnyLength"/> (the IsBased discipline: the item binds as its
    /// ordinary one-character shape under an already-failed compile — never a half-varying state).</summary>
    private void AnyLengthValidateUnit()
    {
        foreach (var root in data.State.Forest.Roots)
        {
            if (!root.IsAnyLength) continue;
            using var _ = data.Edition.At(root);
            string where = $"data item '{root.CobolName ?? "FILLER"}'";
            if (!_roots.Contains(root))
                data.Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may be specified only in an "
                    + "elementary level 1 entry in the LINKAGE SECTION (ISO §2023:13.18.2.3 SR2)");
            else if (root.IsGroup)
                data.Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the subject of an ANY LENGTH clause shall be "
                    + "ELEMENTARY — this entry has subordinate items (ISO §2023:13.18.2.3 SR2)");
            else if (data.Oo.IsClassUnit)
                // This binder's own LINKAGE is the OBJECT/FACTORY paragraph's, never a method's (method
                // linkage binds via OoDataBinder.BindMethodData into OoMethodBinding.LinkageRoots, swept there).
                data.Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may be specified only in the "
                    + "linkage section of a function, a contained program, or a method that is not a property "
                    + "method — not in a factory or instance definition (ISO §2023:13.18.2.3 SR2)");
            else if (!data.UnitIsContained && !data.UnitIsFunction)
                data.Edition.Error(DiagnosticCatalog.AnyLengthPlacement, $"{where}: the ANY LENGTH clause may not be specified in an "
                    + "OUTERMOST program — only in a function, a contained program, or a method "
                    + "(ISO §2023:13.18.2.3 SR2 and its NOTE)");
            else
            {
                // SR3a/SR4 require the BY REFERENCE phrase (stated or implied) — a BY VALUE formal never
                // qualifies (its length is fixed by the value copy, not the caller's argument).
                bool formal = Formals.Any(f => ReferenceEquals(f.Item, root) && !f.ByValue);
                bool returning = ReferenceEquals(Returning, root);
                if (data.UnitIsFunction ? !formal : !(formal || returning))
                    data.Edition.Error(DiagnosticCatalog.AnyLengthSubjectNotInHeader, data.UnitIsFunction
                        ? $"{where}: in a FUNCTION the subject of an ANY LENGTH clause shall be referenced in "
                          + "the procedure division header as a formal parameter with the BY REFERENCE phrase "
                          + "(ISO §2023:13.18.2.3 SR4)"
                        : $"{where}: in a contained program the subject of an ANY LENGTH clause shall be "
                          + "referenced in the procedure division header as a BY REFERENCE formal parameter or "
                          + "as the RETURNING item (ISO §2023:13.18.2.3 SR3)");
                else
                    continue;   // conformant — keep the flag. A RETURNING item (SR3 b)) is seeded with the
                                // ACTIVATOR's receiver length at activation entry (§2023:13.18.2.4 GR1 b); ProgramEmitter.EmitCallMethod).
            }
            root.IsAnyLength = false;
        }
    }

    /// <summary>The LINKAGE records the procedure division may NOT reference (ISO §2023:13.7.3 SR4), keyed by identity — the
    /// roots left over once every a)–e) leg has been asked. Empty for a unit whose linkage is all referenceable, so
    /// the per-reference question is one count test. Sealed by <see cref="SealLinkageRules"/> for a program or
    /// function (<see cref="CallBindLinkage"/>) and for each method (<c>OoDataBinder.BindMethodData</c>).</summary>
    private readonly HashSet<DataItem> _unreferenceableLinkageRoots = new(ReferenceEqualityComparer.Instance);

    /// <summary>A FUNCTION's formal parameters (ISO §2023:13.7.3 SR5), keyed by identity.</summary>
    private readonly HashSet<DataItem> _functionFormals = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ THE ONE PLACE §2023:13.7.3 SR4 AND SR5 ARE DECIDED (kb/Work PB1249 / PB1250), from the facts every header
    /// arm knows once its USING and RETURNING phrases are bound: the unit's linkage roots, its formal operands and its
    /// RETURNING item. Called by the program/function arm (<see cref="CallBindLinkage"/>) and the method arm
    /// (<c>OoDataBinder.BindMethodData</c>), so the two cannot disagree.
    /// <para>SR4 is per RECORD, because a), b) and d) are all "is, or is under, X": a root is referenceable when it is a
    /// header operand (a)), BASED ("a based data item may be referenced as described in 13.18.5"), or defined with a
    /// REDEFINES whose object is referenceable (c)); everything beneath it (b), d)), its RENAMES entries (c)) and its
    /// condition-names and index-names (e)) follow its record.</para>
    /// <para>SR5 bars a function's FORMAL PARAMETERS as receivers; its RETURNING item is not a formal parameter.</para></summary>
    internal void SealLinkageRules(IReadOnlyList<DataItem> roots, IReadOnlyList<DataItem> formalItems,
        DataItem? returning, bool function)
    {
        bool Satisfied(DataItem root) =>
            root.IsBased || ReferenceEquals(root, returning) || formalItems.Any(f => ReferenceEquals(f, root));
        foreach (var root in roots)
        {
            // c): a REDEFINES whose object is a)-satisfying. The object is the ORIGINAL entry (§2023:13.18.44.3 SR11 refuses
            // a redefinition of a redefinition), so one step reaches it.
            bool viaObject = root.RedefinesTargetName is { } target
                && roots.FirstOrDefault(o => !ReferenceEquals(o, root)
                    && CobolNames.Same(o.CobolName, target)) is { } original
                && Satisfied(original);
            if (!Satisfied(root) && !viaObject) _unreferenceableLinkageRoots.Add(root);
        }
        if (function)
            foreach (var formal in formalItems) _functionFormals.Add(formal);
    }

    /// <summary>The record of a LINKAGE item the procedure division may not reference (ISO §2023:13.7.3 SR4), or null when
    /// the reference is permitted — or the item is no linkage item of THIS source element (a GLOBAL linkage item a
    /// contained program inherits is reached through §2023:13.18.27.4 GR2, not through this rule).</summary>
    internal DataItem? UnreferenceableLinkageRecord(DataItem item) =>
        _unreferenceableLinkageRoots.Count != 0 && _unreferenceableLinkageRoots.Contains(item.Root) ? item.Root : null;

    /// <summary>True when <paramref name="item"/> IS a FUNCTION formal parameter — the operand SR5 forbids as a receiver
    /// (whatever its reference form: whole, or a reference-modified part). An item SUBORDINATE to a group parameter is
    /// not "a formal parameter of a function": the same clause's SR4 b) says "subordinate to an operand of the USING
    /// phrase" when it means one, and SR5 does not.</summary>
    internal bool IsFunctionFormal(DataItem item) =>
        _functionFormals.Count != 0 && _functionFormals.Contains(item);

    private DataItem? FindLinkageRoot(string name) =>
        _roots.FirstOrDefault(r => CobolNames.Same(r.CobolName, name));

    /// <summary>
    /// Collect the roots whose EXTERNAL / GLOBAL clause the clause-placement screen ADMITTED
    /// (<see cref="DataItem.HasExternalClause"/> / <see cref="DataItem.HasGlobalClause"/> — ISO §2023:13.18.22.3 SR1,
    /// §2023:13.18.27.3 SR1 b), §2023:13.16.3 SR5–SR7) and re-base each EXTERNAL record onto a run-unit <c>ExternalStore</c>
    /// cell. Runs at the END of <c>Bind</c>
    /// (after REDEFINES classification) so an EXTERNAL record that is also a redefines anchor folds its whole
    /// class onto the shared backing. The mechanism is the existing Tier-B string-canonical machinery: the
    /// backing becomes a <c>ref</c>-property over the external cell, every member an (offset,width) window —
    /// ONE storage copy per name per run unit (§2023:8.6.7), not reset by CANCEL (§2023:14.9.5 GR8). FD-level EXTERNAL
    /// joins here too: the FD's record area is EXTERNAL record data (ISO §2023:13.18.22.4 GR4b — one record area per
    /// run unit, shared by every program describing the file), re-based through the SAME mechanism with the cell
    /// keyed by the file's externalized name (GR5 — so two programs' differently-named records over one EXTERNAL
    /// FD still share the one area; IC227A). FD-level GLOBAL is handled by the emitter's containment merge
    /// (<c>CallBindUnit</c> — §2023:13.18.27.4 GR1 makes the file-name and record-names global names).
    /// </summary>
    internal void CallBindExternalAndGlobal(Core.ProgramUnitContext program)
    {
        // ⛔ READ FROM THE BOUND ITEMS, NEVER FROM THE PARSE TREE (kb/Work PB518 / PB519). The clause-placement
        // screen (DataBinder.ClausePlacement.cs) has already ADMITTED or REFUSED every EXTERNAL / GLOBAL clause at
        // its entry — level 1 (§2023:13.16.3 SR6, §2023:13.18.22.3 SR1, §2023:13.18.27.3 SR1 b)), the data-name format (SR7), the
        // section (EXTERNAL: working-storage only), and EXTERNAL×REDEFINES/BASED (SR5) — and only an admitted
        // clause sets HasExternalClause / HasGlobalClause. The parse-tree scan this replaces re-derived the rule
        // as `continue` filters, so a misplaced clause was accepted and silently annulled; it never read the FILE
        // SECTION, so a GLOBAL record there (legal per SR1 b)) was UNDEFINED in every containee; and it re-based
        // an EXTERNAL REDEFINES entry, folding the redefines ANCHOR onto the external cell and discarding its VALUE.
        // The population is every residence SR1 b) admits: working-storage, local-storage, linkage and file.
        foreach (var item in data.State.Forest.WorkingStorageRoots.Concat(data.State.Forest.LocalStorageRoots).Concat(_roots)
                     .Concat(data.State.Files.Declared.SelectMany(f => f.Records)))
        {
            if (item.HasGlobalClause && !_globalRoots.Contains(item)) _globalRoots.Add(item);
            if (!item.HasExternalClause) continue;
            // §2023:13.16.3 SR5 makes this unreachable (the screen refuses EXTERNAL beside REDEFINES), and it must STAY
            // unreachable: re-basing a redefinER re-homes its whole class — the anchor included — onto the cell.
            if (item.RedefinesTargetName is not null)
                throw new InvalidOperationException(
                    $"EXTERNAL record '{item.CobolName}' also redefines '{item.RedefinesTargetName}' — the §2023:13.16.3 SR5 "
                    + "clause-placement screen should have refused its EXTERNAL clause (DataBinder.ClausePlacement.cs)");
            CallMakeExternal(item);
        }

        // Records EXTERNAL BY TYPE (ISO §2023:13.18.22 GR3; P10 Step 16): a record whose TYPE clause names an
        // EXTERNAL type declaration is itself external "subject to the same rules" — ExpandType marked it
        // (the record's own entry carries no EXTERNAL clause, so the parse-tree scan above cannot see it).
        // Re-based through the SAME cell mechanism, keyed by the record's own name (GR5). An entry that ALSO
        // carries its own EXTERNAL clause was already re-based by the scan above — skip the double registration.
        // ⛔ ONE CLAIMANT PER RECORD (kb/Work PB5003): a record of an EXTERNAL FD is claimed by the FD arm below — its area is
        // keyed by the FILE's externalized name (GR4b/GR5), so a record that is ALSO external by its type
        // (`FD F IS EXTERNAL. 01 R TYPE TG.` with `TG IS TYPEDEF STRONG EXTERNAL`) must not take a second cell and backing
        // under its own name: both declared `_scell_R` / `_redef_R` (CS0102).
        var externalFdRecords = data.State.Files.Declared.Where(f => f.IsExternal).SelectMany(f => f.Records).ToHashSet();
        foreach (var item in data.State.Forest.Roots)
            if (item is { ExternalFromType: true, HasExternalClause: false, CobolName: not null } && !externalFdRecords.Contains(item))
                CallMakeExternal(item);

        // EXTERNAL FDs: the record area is run-unit storage keyed by the externalized FILE name (§2023:13.18.22.4
        // GR4 b) / GR5 — the records of every describer alias it). The GR6 same-byte-count conformance check across
        // describers is EC-band work (§2023:14.8.4) — ExternalStore.Describe.
        // ⛔ THE WHOLE AREA, BOTH HALVES (kb/Work PB1026; determination D-FRA). The CHARACTER half — every record
        // with a window, already one implicit-redefinition class (LinkImplicitRecordArea) — is ONE cell keyed
        // "FD::<name>", re-based through its anchor. Each OUT-OF-LINE record (FileModel.IsOutOfLineRecord — a
        // dynamic-length record, a variable-length group, a pointer-class record) is not part of that class, so it
        // gets a cell of its OWN, keyed "FD::<name>#<k>" by its ordinal k among the file's out-of-line records in
        // declaration order: the key a describer can compute from its own FD (GR5 externalizes the FILE name, never
        // a record name — two describers name their records as they like, IC227A), and the cell carries the
        // record's content and its dynamic-length items (StorageCell.DynAt) or its pointer value (SlotAt), so GR4 b)'s
        // "the data contained in all record description entries" is ONE copy per run unit. It used to be staged
        // loud as COBOL0899: the character half alone would have split the area in two.
        foreach (var file in data.State.Files.Declared)
            if (file is { IsExternal: true, ExternalName: { } extName } && file.Records.Count > 0)
            {
                if (file.CharacterAnchor is { } anchor) CallMakeExternal(anchor, "FD::" + extName);
                int k = 0;
                foreach (var record in file.Records.Where(FileModel.IsOutOfLineRecord))
                    CallMakeExternal(record, $"FD::{extName}#{++k}");
            }

        // GLOBAL FDs: the record-names of a GLOBAL FD are GLOBAL names (ISO §2023:13.18.27.4 GR1 — the file-name and the
        // record-names described subordinate to the FD are global names): the records join the GLOBAL roots so
        // contained programs reach the OWNER's record area through the standard containment bridges
        // (§2023:13.18.27 GR2 — container storage, contained visibility). The file-NAME half of the rule is the
        // contained unit's VisibleFiles lookup (InheritedGlobals.Files).
        foreach (var file in data.State.Files.Declared)
            if (file.IsGlobal)
                foreach (var rec in file.Records)
                    if (!GlobalRoots.Contains(rec))
                        _globalRoots.Add(rec);

        CheckExternalizedNameUniqueness();
    }

    /// <summary>ISO §2023:13.18.22.3 syntax rule 2: <i>"In the same source element, the externalized name of the
    /// subject of the entry that includes the EXTERNAL clause shall not be the same as the externalized name of
    /// any other entry that includes the EXTERNAL clause."</i>
    /// <para>⛔ THE RULE ONLY BECAME VIOLABLE WHEN THE `AS literal-1` PHRASE GAINED A GRAMMAR (kb/Work PB511):
    /// without it every externalized name was §2023:13.18.22.4 GR5's default — the subject's own data-name or
    /// file-name — and two entries in one source element cannot share one of those (§2023:8.4.2.2 already rejects
    /// the duplicate declaration). With the phrase in, two entries CAN name one cell, and the consequence is
    /// not a diagnostic-quality one: the run-unit <c>ExternalStore</c> keys by exactly this name, so two
    /// differently-shaped records would silently alias one another's storage — a wrong answer, not a
    /// rejection. That is why this is screened rather than assumed.</para>
    /// <para>The population is the entries GR5 gives an externalized name to — file connectors and records.
    /// A level-1 TYPE DECLARATION may also carry the EXTERNAL clause (SR1), but GR5 names only "the file
    /// connector or record", and GR3 externalizes the records that REFERENCE such a type under their own
    /// names, so a type declaration's literal-1 has no externalized name to collide with.</para>
    /// <para>⛔ ONE COMPARISON, THE RUN-UNIT STORE'S (kb/Work PB1236): case-INSENSITIVE. The default externalized name
    /// is the subject's data-name (§2023:13.18.22.4 GR5), and a data-name's case never matters (§2023:8.3.2), so
    /// <c>ExternalTable</c> keys its cells and describers with <see cref="CobolNames.Comparer"/> — that
    /// is what makes <c>01 XV EXTERNAL</c> in one program and <c>01 Xv EXTERNAL</c> in another ONE record. The
    /// uniqueness screen this method is protects exactly that store, so it must ask the same question: the
    /// ordinal comparison it used to make let <c>EXTERNAL</c> and <c>EXTERNAL AS "a"</c> pass the screen and then
    /// silently alias one cell of the first-created width. §2023:8.3.2.2 2) leaves the formation of an externalized name
    /// to the implementor, which makes one comparison a choice — and the store's is the one already load-bearing.</para>
    /// </summary>
    private void CheckExternalizedNameUniqueness()
    {
        // Almost every source element declares no EXTERNAL subject at all; this pass runs once per unit,
        // so it costs nothing when there is nothing to compare.
        if (_externalBackings.Count == 0 && !data.State.Files.Declared.Any(f => f.IsExternal)) return;
        var seen = new Dictionary<string, string>(CobolNames.Comparer);   // the ExternalTable's own comparer
        void Claim(string externalized, string where, DiagnosticCursor at)
        {
            if (seen.TryGetValue(externalized, out var prior))
            {
                using var _ = data.Edition.At(at);   // an unset cursor leaves the current position standing
                data.Edition.Error(DiagnosticCatalog.ExternalizedNameNotUnique,
                    $"{where}: the externalized name '{externalized}' is already the externalized name of "
                    + $"{prior} in this source element; \"In the same source element, the externalized name of "
                    + "the subject of the entry that includes the EXTERNAL clause shall not be the same as the "
                    + "externalized name of any other entry that includes the EXTERNAL clause\" "
                    + "(ISO §2023:13.18.22.3 SR2). The EXTERNAL clause's AS phrase names the run-unit cell the "
                    + "subject shares (§2023:13.18.22.4 GR5), so two subjects claiming one name would share one "
                    + "storage area.");
                return;
            }
            seen[externalized] = where;
        }

        foreach (var subject in ExternalizedSubjects())
            Claim(subject.Name, subject.Spelling, subject.At);
    }

    /// <summary>⛔ THE ONE LIST OF THIS SOURCE ELEMENT'S EXTERNALIZED DATA-SIDE NAMES — ISO §2023:8.3.2.2's list item 2,
    /// "data-names, file-names, and record-names of items described with the EXTERNAL attribute" (kb/Work PB1404).
    /// Each subject is a file connector or a record that §2023:13.18.22.4 GR5 gives an externalized name: the AS literal
    /// when the entry writes one, else the entry's own name, in the one case-insensitive spelling the run-unit
    /// <c>ExternalStore</c> keys by. The FD-record cells ("FD::" keys) are the FILE's name, never a name of their
    /// own, so they are not subjects. Two consumers read it and no one else builds the population: the
    /// in-element uniqueness screen above (§2023:13.18.22.3 SR2) and the group-wide externalized-name namespace
    /// (<c>BinderDriver.CheckDefinitionNameUniqueness</c>, §2023:8.3.2.2 — "all instances of a given name that is
    /// externalized … shall identify the same kind of entity or item"), whose list item 1 already feeds it.</summary>
    internal IEnumerable<ExternalizedSubject> ExternalizedSubjects()
    {
        foreach (var file in data.State.Files.Declared)
            if (file is { IsExternal: true, ExternalName: { } extName })
                yield return new ExternalizedSubject(ExternalizedSubjectKind.File, extName,
                    file.CobolName ?? extName, $"file '{file.CobolName}'", file.EntryAt);
        foreach (var backing in _externalBackings)
            if (!backing.ExternalName.StartsWith("FD::", StringComparison.Ordinal))
                yield return new ExternalizedSubject(ExternalizedSubjectKind.DataItem, backing.ExternalName,
                    backing.Record.CobolName ?? backing.ExternalName, $"data item '{backing.Record.CobolName}'",
                    backing.Record.DeclaredAt);
    }

    /// <summary>Re-base one EXTERNAL record onto the run-unit external cell (see
    /// <see cref="CallBindExternalAndGlobal"/>). <paramref name="externalName"/> overrides the cell key (the
    /// FD-record case keys by the FILE's externalized name, §2023:13.18.22.4 GR5; a WS record keys by its own name).
    /// A record with a national or pointer-class leaf cannot be carried by the shared byte cell — rejected
    /// loud, conformant-but-unimplemented. Every NUMERIC leaf rides it on its pinned byte form (kb/Work
    /// PB164) and every BOOLEAN leaf, USAGE BIT included, rides it on the §2023:8.5.1.6.3 packing (kb/Work
    /// PB231). The population is <see cref="DataBinder.ByteWindowResidueOf"/>'s, never a second list.</summary>
    private void CallMakeExternal(DataItem item, string? externalName = null)
    {
        if (data.ForceStringCanonical(item, "EXTERNAL record") is not { } cls)
        {
            // The silent-skip left the record as ORDINARY storage — the program ran with its EXTERNAL
            // sharing semantics silently dropped (the W1-test finding, Phase 4a). Loud at bind: the
            // interpolated reason names the actual leaf (the RESIDUE-11 national layout, the bit-packing
            // residue, or a pointer-class leaf with no byte form).
            data.Edition.Error(DiagnosticCatalog.ExternalRecordNotCellBacked, $"EXTERNAL record '{item.CobolName}' cannot be cell-backed — "
                + $"{item.Class?.RejectReason ?? "unsupported leaf"} — recognized but not yet implemented");
            return;
        }
        // ISO §2023:13.18.22.4 GR5, THE ONE PLACE ITS TWO SENTENCES ARE APPLIED (kb/Work PB511): the cell key is
        // literal-1 when the entry's EXTERNAL clause writes `AS literal-1`, and the record's own data-name
        // otherwise. `externalName` overrides both — it is the FD-record case, whose key is the FILE
        // connector's externalized name (GR4b/GR5's first alternative), already resolved at the FD entry.
        _externalBackings.Add(new CallExternalBacking(
            cls.BackingClrName, cls.BackingCellClrName,
            externalName ?? item.ExternalizedAs ?? CobolNames.UpperFold(item.CobolName!),
            cls.Width, item));
    }


    /// <summary>⛔ THE ONE CARRIER-RESIDENCY TEST of a USING formal, for a program, a function AND a method (kb/Work
    /// PB2087): carrier-resident = per-access aliasing of the caller's storage through the formal's carrier (design D1:
    /// "refs to LK-CTR read/write LK_CTR.Value — the one unavoidable indirection"). Available for an elementary
    /// fixed-point or character formal NOT overlaid by another linkage entry; a group formal (a different C# struct type
    /// than the caller's — every cross-program group IS a category reinterpretation) and a redefined formal are AREA
    /// formals instead, laid over the argument's own storage cell (PtrBindBasedAndAddressables claims the class).
    /// <para>⛔ The elementary/group test is STRUCTURAL (no subordinate entries), not <c>IsElementary</c> alone: the
    /// program arm runs inside BindDeclarations, BEFORE UsageInheritancePass, and a level-01 that wrote a PICTURE-less
    /// usage — <c>01 L USAGE BINARY-SHORT. 05 LA.</c> — still carries the elementary profile bind synthesized for it
    /// until §2023:13.18.60.4 GR1 sheds it, so an <c>IsElementary</c> verdict alone went stale (a compiler
    /// NullReferenceException on legal source, kb/Work PB495).</para>
    /// <para>⛔ AN ADDRESSED FORMAL IS NOT RESIDENT (kb/Work PB1019). ISO §2023:8.4.3.11.3 SR1 admits a linkage-section item
    /// as identifier-1 of ADDRESS OF, and §2023:8.4.3.11.4 GR1 makes the result "the address of identifier-1" — an address
    /// the BASED machinery dereferences to a StorageCell, which a carrier accessor does not have; so an addressed formal
    /// is an AREA formal and its address IS the argument's (§2023:14.2.3 GR8).</para></summary>
    internal static bool FormalIsCarrierResident(DataItem item, IEnumerable<DataItem> linkageRoots, IReadOnlySet<string> addressed) =>
        item.Children.Count == 0 && item.IsElementary && item.Pic is { IsFloat: false }
        && !addressed.Contains(item.CobolName ?? "")
        && !linkageRoots.Any(r => !ReferenceEquals(r, item) && r.RedefinesTargetName is { } t && CobolNames.Same(t, item.CobolName));

}
