// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;


namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, its subscript list (<c>subscriptPart</c>, one parsed item per subscript — kb/Work PB2113) and
///         its reference modifier (<c>refModPart</c>).</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/>, interpret
///         the subscripts to C# index expressions, and build the member-access path with each subscript attached to
///         its OCCURS level (outer→inner).</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, a reference-modified reference (<c>(s:l)</c> — G2-1c), or a subscript form not yet handled — so the
/// caller emits a loud not-implemented guard rather than silently mis-binding.
/// <para><b>This type is the funnel, and the rules live in the parts it composes</b> (DESIGN-architecture-review §8.3 (4),
/// kb/Work PB2299, PB2371–PB2375): <see cref="Names"/> (§2023:8.4.2.2 qualification), <see cref="Subscripts"/> (§2023:8.4.2.3, with the
/// position binder reference modification shares), <see cref="RefMods"/> (§2023:8.4.3.3), <see cref="ObjectProperties"/> (§2023:8.4.3.9),
/// <see cref="Counters"/> (the REPORT SECTION sum counters, §2023:13.18.54.4 GR5) and <see cref="Places"/> (the model-layer item-to-Place
/// builder). What stays here is the order they are asked in — the one identification step <c>Identify</c> — the three entries that
/// answer a closed <see cref="RefResolution"/>, the register views that are no data item (CAPACITY, EXCEPTION-OBJECT, DEBUG-ITEM)
/// and the per-resolution state the parts share (<see cref="Probing"/>, <see cref="Diagnosed"/>, <see cref="SegmentDeferred"/>).</para>
/// </summary>
public sealed class ReferenceResolver(DataBinder data)
{

    /// <summary>⛔ THE ODO LENGTH IS EVALUATED BEFORE THE REFERENCE MODIFIER (ISO §2023:14.6.4 steps 6 then 7 — "length
    /// evaluation for an occurs-depending group item", then "reference modification", each done in full before the
    /// next; kb/Work PB1123). A reference modifier whose position carries a function activation has that activation
    /// hoisted ahead of the statement as a PRE-op (<see cref="MaterializeSegment"/>), so it would run BEFORE the
    /// occurs-depending group's length is read at the operand site. This hook — set by the statement binder, the
    /// same one-way edge as <see cref="MaterializeSegment"/> — pins the length first: given the occurs-depending
    /// group's place, the index in the pending pre-ops where the reference modifier's own pre-ops begin, and the
    /// line, it registers a copy of the DEPENDING ON object into a compiler temp at that index and returns the place
    /// whose extent reads the temp. Null when absent (a data-division resolver): the pre-D18 answer.</summary>
    internal Func<OdoGroupPlace, int, int, Place>? FreezeOdoExtent { get; set; }
    /// <summary>A SYNTHETIC copy of <paramref name="dref"/> without its LAST <c>dataReferenceSuffix</c> — for a
    /// caller whose resolved SYMBOL has shown that the trailing word the parser attached as a qualifier belongs to
    /// the enclosing construct instead (kb/Work PB843: a THROUGH range's <c>IN alphabet-name-1</c>). The copy ADOPTS
    /// the original children (the <c>BinderDriver.Reparent</c> technique) and never mutates the parse tree, so every
    /// resolver walk — each reads <c>dataReferenceSuffix()</c> off the node it is handed — sees the reference the
    /// symbol decided, with no second resolution path to keep in step.</summary>
    internal static Core.DataReferenceContext WithoutTrailingSuffix(Core.DataReferenceContext dref)
    {
        var cut = new Core.DataReferenceContext(dref.Parent as ParserRuleContext, dref.invokingState);
        int keep = dref.ChildCount - 1;
        for (int i = 0; i < keep; i++)
            switch (dref.GetChild(i))
            {
                case ParserRuleContext rc: cut.AddChild(rc); break;
                case ITerminalNode t: cut.AddChild(t); break;
            }
        cut.Start = dref.Start;
        cut.Stop = dref.GetChild(keep - 1) switch
        {
            ParserRuleContext rc => rc.Stop,
            ITerminalNode t => t.Symbol,
            _ => dref.Start,
        };
        return cut;
    }

    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported
    /// here — the DEMANDING form: a name that identifies NO declared item reports <c>COBOL1639</c> (kb/Work
    /// R30 — §2023:8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource"; before
    /// this, a typo in any reference position compiled clean and threw at RUN time). Every OTHER null return —
    /// a special register routed by the caller, an unsupported subscript/RENAMES/Tier-C shape — is feature-debt
    /// staging and stays silent, so the caller's loud posture is unchanged. A caller asking "IS this a data
    /// item?" with a legal alternative on no (the SET format sniffs, the INVOKE class-name receiver, the
    /// boolean/float reroutes) reads <see cref="Probe"/> instead.</summary>
    public RefResolution Resolve(Core.DataReferenceContext dref) => ResolveImpl(dref, report: true);

    /// <summary>The SPECULATIVE form of <see cref="Resolve"/>: identical resolution, but a name that identifies
    /// no item returns null SILENTLY — for type-discriminating probes whose null arm continues to a legal
    /// alternative reading (INVOKE's class-name receiver, SET's format sniffs, EXCEPTION-OBJECT, the
    /// boolean-operand predicate that is documented diagnostic-free). Never use it where a data item is
    /// REQUIRED — that silence is exactly the R30 defect.
    /// <para>⛔ IT RETURNS A SNIFF, NOT A <see cref="Place"/>, AND THE TYPE IS THE FIX (kb/Work PB221). The
    /// documented contract has always been "a probe is a TYPE-DISCRIMINATING sniff whose Place is DISCARDED
    /// after reading its Item" — but it returned a <c>Place</c>, and FOUR callers (CallBinder's BY CONTENT arm,
    /// OoBinder's INVOKE receiver and SET object-reference sender, PtrBinder's SET UP/DOWN first target) simply
    /// committed it into the bound tree. Everything <c>Probing</c> suppresses to keep a probe pure then went
    /// missing from the committed bind: the §2023:8.8.1.1 position screen and the §2023:13.18.38.3 r7 index-name screen
    /// never ran (<c>CALL "S" USING BY CONTENT E(XE)</c> with <c>XE PIC X(4)</c> compiled clean while the
    /// adjacent <c>BY REFERENCE</c> operand drew COBOL0844 — one statement, two verdicts), and the D18
    /// materializer's <c>return "1"</c> short-circuit made <c>E(FUNCTION INTEGER(2))</c> bind occurrence ONE — a
    /// WRONG ANSWER, live since PB157 landed the flag. A comment cannot hold that invariant; a return type can.
    /// A caller that has finished discriminating asks <see cref="Resolve"/> for the Place that enters the tree,
    /// which is the pattern <c>SetBinder.BindSetLocale</c> already used.</para></summary>
    public ProbeResult? Probe(Core.DataReferenceContext dref) =>
        ResolveImpl(dref, report: false).Place is { } p
            ? new ProbeResult(p.Item, p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category,
                SectionDataItem.NonSectionKind(p))
            : null;

    /// <summary>The <see cref="DataItem"/> a written data reference names, asked SILENTLY (kb/Work PB4090): the §2023:8.4.2.2
    /// lookup <see cref="Resolve"/> performs, with the declaration and nothing built from it — no
    /// <see cref="Place"/>, no shape screen, no COBOL1639 on a name that identifies nothing. For a caller whose
    /// question is about the DECLARATION (what USAGE does this item carry?) and whose format has a legal reading on
    /// which the name is no data item at all; the SEND statement's TO operand, which is a message-server-name-1 in
    /// Format 1 and a message-tag data-name-2 in Format 2 (§2023:14.9.38.2), is the one such caller. Null for a special register, a name nothing declares and an ambiguous reference.</summary>
    internal DataItem? ProbeDeclaration(Core.DataReferenceContext dref)
    {
        DataReferenceCst written = dref;
        if (written.Register != SpecialRegister.None || written.BaseName is not { } name) return null;
        bool savedProbing = Probing;
        Probing = true;
        try { return Names.FindItem(name, ReadWritten(dref).Qualifiers); }
        finally { Probing = savedProbing; }
    }

    /// <summary>⛔ THE ONE OWN-ITEM TEST (kb/Work PB3153): does the REFERRING element itself describe an item that
    /// <paramref name="dref"/> names, either a data item (<see cref="Probe"/>) or a constant-name (a bare word only,
    /// <see cref="ConstantEntryBinder.ConstantOf"/>)? A contained program or a method sees the SPECIAL-NAMES and REPOSITORY
    /// names its container declares, and "a source element's reference to such a name, even when it is a different
    /// type of user-defined word, is to the item which that source element describes" (§2023:8.4.6.1). So every position
    /// whose format admits both an inherited configuration-section name and an operand asks THIS first, and takes
    /// the configuration-section reading (which records the §2023:8.3.2.2 use) only on a false answer. Both halves are
    /// needed: a constant-name "may be used anywhere that a format specifies a literal of the class and category of
    /// constant-name-1" (§2023:13.10.3 SR2), and a constant is no data item, so a probe alone misses it (the round-2
    /// review finding: an own <c>01 LVL CONSTANT AS 1</c> in STANDARD-COMPARE's argument-4 position was claimed as
    /// the inherited ordering-name). Diagnostic-free, like <see cref="Probe"/>.</summary>
    public bool DescribesOwnItem(Core.DataReferenceContext dref) =>
        data.Constants.ConstantOf(dref) is not null || Probe(dref) is not null;

    /// <summary>What a <see cref="Probe"/> may tell its caller: the resolved <see cref="DataItem"/>, the
    /// operand CATEGORY of the reference (§2023:8.4.3.3.4 GR6 for a reference-modified one — <see cref="RefModPlace"/>'s
    /// own reader — else <see cref="DataItem.OperandPic"/>, the D20 one reader, so a GROUP-USAGE group sniffs as
    /// the boolean / national operand it is), and — <paramref name="NonSectionKind"/> — what the reference is when
    /// it is NOT "a data item defined in the file, working-storage, local-storage, or linkage section" (the
    /// <see cref="SectionDataItem"/> question an activation's keyword-less argument asks BEFORE it chooses its
    /// passing mode, kb/Work PB1137). Deliberately NOT a <see cref="Place"/>: a probe is unscreened, so a Place it
    /// produced must never reach the bound tree (kb/Work PB221).</summary>
    public readonly record struct ProbeResult(DataItem Item, PicCategory? OperandCategory, string? NonSectionKind);

    /// <summary>The source nodes this resolver has already DIAGNOSED — a data reference (an undefined name,
    /// <see cref="ReportUnidentified"/>, or a rejected reference shape: SR3 ref-mod-of-ref-mod, SR1 identifier-1
    /// exclusion) or a reference modifier (an omitted leftmost-position, <see cref="ReadRefMod"/>): one report per
    /// source node even when a statement binder resolves the same node more than once, and the fact the receiving
    /// chokepoint asks (<see cref="WasDiagnosed"/>) so that a null it gets back is EITHER already reported OR
    /// reported there — never a silently dropped receiver (kb/Work PB70).</summary>
    internal HashSet<ParserRuleContext> Diagnosed { get; } = [];

    /// <summary>True when a diagnostic has been emitted for <paramref name="dref"/> by this resolver — the
    /// receiving chokepoint reports an undiagnosed null itself (recognized-not-implemented shape).</summary>
    public bool WasDiagnosed(Core.DataReferenceContext dref) => Diagnosed.Contains(dref);

    /// <summary>True while the CURRENT resolution is a <see cref="Probe"/> — the R30 purity flag (kb/Work
    /// PB157). A probe is a TYPE-DISCRIMINATING sniff whose Place is discarded after reading its Item, so in
    /// probe mode the resolver must be SIDE-EFFECT-FREE: no diagnostics beyond none, no OO property temp/op
    /// registration (the orphan op made OoWrapPropertyOps prepend a GET §2023:8.4.3.9.4 GR2 forbids — or reject a
    /// WITH NO GET property), and no D18 subscript materialization (a function-bearing subscript would bind —
    /// and later ACTIVATE — twice). Save/restored, not cleared: a COMMIT resolution can nest inside hooks.</summary>
    internal bool Probing { get; set; }

    /// <summary>The report whose REPORT SECTION clause operands are being bound (ISO §2023:13.18.53.3 SR4 — the sum counters a
    /// SOURCE operand may name are those "defined in the current report"), or null in the procedure division and
    /// everywhere else. Set only by <c>ReportWriterBinder</c>, for the span of its own clause binding; it narrows the
    /// candidates of a sum-counter reference to that report, so a name two reports share is unambiguous there
    /// (kb/Work PB1292).</summary>
    internal ReportModel? EnclosingReport { get; set; }

    /// <summary>The VARYING counters the report-section expression being bound may name, by data-name (ISO §2023:13.18.64.3
    /// SR2 — a counter "may be referenced only within the current entry or a subordinate entry"), or null outside a
    /// report entry's clause. Set only by <c>ReportWriterBinder</c>. A counter is no data item (§2023:13.18.64.4 GR1 — "a
    /// temporary integer data item") and lives in no name table, so every reader of a written NAME asks here: the
    /// operand and expression binders through <c>ReportWriterBinder.VaryingExpr</c>, and the subscript renderer
    /// (<see cref="ResolveSubscriptName"/>), which is how a counter subscripts a source item (GR4's NOTE).</summary>
    internal IReadOnlyDictionary<string, ReportVaryingModel>? VaryingScope { get; set; }

    /// <summary>Set when a subscript or reference-modifier SEGMENT of the current resolution failed because one of
    /// its own operands is a deferred shape (the materializer's expression came back
    /// <see cref="BoundExprError.IsUnbuilt"/>), so the segment's null is a DEFERRAL, not a refusal (kb/Work PB1030).
    /// Save/restored per resolution exactly as <see cref="Probing"/> is: the materializer can re-enter this
    /// resolver for a nested reference.</summary>
    internal bool SegmentDeferred { get; set; }

    /// <summary>Called by the segment materializer when a segment's expression came back UNBUILT (not refused) —
    /// see <see cref="SegmentDeferred"/>.</summary>
    internal void NoteSegmentDeferred() => SegmentDeferred = true;

    // ── The parts this funnel composes (DESIGN-architecture-review §8.3 (4), kb/Work PB2299, PB2371–PB2375) ──────

    private CounterResolver? _counters;

    /// <summary>The REPORT SECTION sum-counter references (<see cref="CounterResolver"/>).</summary>
    internal CounterResolver Counters => _counters ??= new(this, data);

    private ObjectPropertyResolver? _objectProperties;

    /// <summary>The OBJECT PROPERTY references and object views (<see cref="ObjectPropertyResolver"/>).</summary>
    internal ObjectPropertyResolver ObjectProperties => _objectProperties ??= new(this, data);

    private NameResolution? _names;

    /// <summary>The QUALIFICATION lookups and their reports (<see cref="NameResolution"/>, ISO §2023:8.4.2.2).</summary>
    internal NameResolution Names => _names ??= new(this, data);

    private RefModBinder? _refMods;

    /// <summary>REFERENCE MODIFICATION: the SR screens and the GR5 view (<see cref="RefModBinder"/>, ISO §2023:8.4.3.3).</summary>
    internal RefModBinder RefMods => _refMods ??= new(this, data);

    private PlaceBuilder? _places;

    /// <summary>THE ONE ITEM-TO-PLACE BUILDER (<see cref="PlaceBuilder"/>, a model-layer type), over the facts of this
    /// binding as they stand at the moment of each resolution.</summary>
    internal PlaceBuilder Places => _places ??= new(data);

    private SubscriptBinder? _subscripts;

    /// <summary>SUBSCRIPTS and the position binder both subscripts and reference modifiers use
    /// (<see cref="SubscriptBinder"/>, ISO §2023:8.4.2.3).</summary>
    internal SubscriptBinder Subscripts => _subscripts ??= new(this, data);

    internal RefResolution ResolveImpl(Core.DataReferenceContext dref, bool report)
    {
        // ⛔ EVERY DIAGNOSTIC THE RESOLUTION OF A REFERENCE REPORTS IS ABOUT THAT REFERENCE, SO IT IS POSITIONED AT THE
        // REFERENCE'S FIRST TOKEN HERE, AT THE ONE CHOKEPOINT (kb/Work PB4383). Positioning was each statement binder's
        // job, so a multi-operand statement (`DISPLAY A` with a continuation line `B C`) reported COBOL1639 for B and C
        // at the statement's first line, and the next verb's binder would have had to remember again. A synthetic
        // reference with no token leaves the position standing (DiagnosticCursorAt.At).
        using var _ = data.Edition.At(dref);
        bool savedProbing = Probing, savedSegment = SegmentDeferred;
        Probing = !report;
        SegmentDeferred = false;
        try
        {
            var answer = ResolveImplCore(dref, report);
            // ⛔ A DEFERRAL GOES ON THE LEDGER HERE, BEFORE ANY CALLER SEES IT (kb/Work PB1030): the statement
            // funnel announces it (COBOL1756) whatever the caller then does with the answer — a caller that
            // drops it (`.OfType<Place>()`) cannot make it silent. A probe records nothing (R30 purity).
            if (report && answer.Outcome == RefOutcome.Deferred) data.Edition.NoteUnbuilt(answer.Feature);
            return answer;
        }
        finally { Probing = savedProbing; SegmentDeferred = savedSegment; }
    }

    /// <summary>The non-place answer for a SEGMENT (subscript or reference-modifier) the renderer could not
    /// render: deferred when the resolver has no materializer (a data-division resolver) or the segment's own
    /// operand is deferred; otherwise REPORTED — the materializer reports every segment it refuses (kb/Work
    /// PB1030: a segment that is not an arithmetic expression is not a subscript, ISO §2023:8.4.2.3.2).</summary>
    internal RefResolution SegmentFailure(string written) =>
        Subscripts.MaterializeSegment is null ? RefResolution.Deferred(DeferredShape.SegmentWithoutStatement, written)
        : SegmentDeferred ? RefResolution.Deferred(DeferredShape.DeferredSegmentOperand, written)
        : RefResolution.Refused(written);

    /// <summary>The non-place answer when <see cref="PlaceForItem"/> built no place: its deferred shape, or —
    /// when it names none — the entry whose declaration was refused (a REJECTED REDEFINES view, Tier D
    /// <see cref="RedefinesTier.Rejected"/>; a level-66 RENAMES entry whose operands did not resolve). A reference
    /// to it is reported here, once per reference (COBOL2364), because the statement holding it must say why
    /// it cannot bind.</summary>
    private RefResolution ItemFailure(Core.DataReferenceContext dref, PlaceGap gap)
    {
        string written = DataBinder.WrittenText(dref);
        if (gap.Deferred is { } shape) return RefResolution.Deferred(shape, written);
        Names.ReportRefusedDeclaration(dref, gap.Refused.CobolName ?? gap.Refused.ClrName, gap.Refused.Class?.RejectReason);
        return RefResolution.Refused(written);
    }

    /// <summary>What <see cref="Identify"/> concluded about a written data reference: EITHER a final
    /// <paramref name="Answer"/> (the name is no storage data item — a register, a sum counter, a refused reference —
    /// or the reference was refused) OR the data <paramref name="Item"/> the name identifies, with the reference
    /// <paramref name="Written"/> and its subscripts rendered to <paramref name="IndexExprs"/> (every OCCURS level
    /// bound, §2023:8.4.2.3.3 SR3; empty for a property temp, whose subscripts were identifier-3's). Never both.</summary>
    private readonly record struct Identified(
        RefResolution? Answer, DataItem? Item, WrittenReference Written, List<Position>? IndexExprs, Place? ImplicitPlace = null);

    private RefResolution ResolveImplCore(Core.DataReferenceContext dref, bool report)
    {
        var id = Identify(dref, report, forAddressOf: false);
        if (id.Answer is { } answer) return answer;
        // What follows the identification is the PLACE half: the item's place, then the reference-modification tail.
        var item = id.Item!;
        var written = id.Written;
        var cleanRef = written.RefModPart;   // the reference modifier (refModSpec : leftmost : length)

        // ⛔ EVERY NAMED ITEM TAKES THE SAME TAIL — a level-66 RENAMES alias included (kb/Work PB1380). The alias's
        // place is built by PlaceForItem, the ONE item→place builder, and then meets the reference-modification
        // tail below like any other identifier. It used to be composed in an arm of its own HERE that returned
        // before that tail, so `RN(2:3)` bound the WHOLE alias — ISO §2023:8.4.3.3.4 GR5's "unique data item that is a
        // subset of the data item referenced by identifier-1" was never created, `MOVE RN(2:3) TO X` moved every
        // character of the span, and `RN(2:N)` with N = 0 overwrote the span instead of raising EC-BOUND-REF-MOD
        // (§2023:7.3.23.3 GR1, directive omitted). Both forms were dropped: the THROUGH form's RenamesPlace AND the
        // no-THROUGH form's forward to data-name-2's place.
        // An implicitly-defined name that is no storage item (DEBUG-CONTENTS) brings its own place; every other item's is built.
        PlaceGap gap = default;
        var inner = id.ImplicitPlace ?? Places.PlaceForItem(item, id.IndexExprs!, out gap);
        if (inner is null) return ItemFailure(dref, gap);

        if (cleanRef is null) return RefResolution.Resolved(inner, "");
        // identifier-1's DESCRIPTION is the item its place stands for, not the name as written: for a no-THROUGH
        // RENAMES alias that is data-name-2, whose attributes §2023:13.18.45.4 GR1 makes data-name-1's own; for a
        // THROUGH alias it is the alias itself (an alphanumeric group item, GR2); for every other item the two
        // are the same item. The syntactic path, the data-division path (ResolveItemRefMod) and the resolved-place
        // path (RefModOf) all read it this way, so SR1 and the view are asked of one description.
        DataItem described = inner.Item;
        int preOpMark = data.Procedure.PendingPreOps.Mark();   // the reference modifier's own pre-ops (function activations) start here
        if (RefMods.ReadScreenedRefMod(dref, written, described, out var refusal) is not { } spec) return refusal!;
        // §2023:14.6.4 steps 6 → 7: an occurs-depending group's length is evaluated BEFORE the reference modifier, so when
        // the modifier registered pre-ops (a function whose side effect may change the DEPENDING ON object) the length
        // is pinned ahead of them (kb/Work PB1123).
        if (inner is OdoGroupPlace odo && data.Procedure.PendingPreOps.AnyAfter(preOpMark) && FreezeOdoExtent is { } freeze)
            inner = freeze(odo, preOpMark, dref.Start.Line);
        return RefMods.RefModView(described, inner, spec) is { } view
            ? RefResolution.Resolved(view, "")
            : RefResolution.Deferred(DeferredShape.NumericRefModSubstrate, DataBinder.WrittenText(dref));
    }

    /// <summary>⛔ THE ONE IDENTIFICATION STEP OF A WRITTEN DATA REFERENCE (kb/Work PB3287) — every entry that
    /// resolves an <i>identifier</i> asks it, so a screen added here reaches them all: <see cref="ResolveImplCore"/>
    /// (<see cref="Resolve"/> and <see cref="Probe"/>) takes the place half after it, and
    /// <see cref="ResolveForAddressOf"/> the ADDRESS OF half. The steps, in the order the name classes are asked: the
    /// special registers and the implicitly-defined names that are no storage item (CAPACITY register, EXCEPTION-OBJECT,
    /// DEBUG-ITEM, a REPORT SECTION sum counter), the §2023:8.4.3.3.3 SR3 reference-modification count, the §2023:8.4.2.2
    /// qualified or unqualified lookup with its one report (<see cref="ReportUnidentified"/>), the §2023:13.7.3 SR4 LINKAGE
    /// screen (<see cref="ScreenLinkageReference"/>) and the written subscripts (<see cref="ReadSubscripts"/>).
    /// <para>⛔ THE SECOND COPY OF THIS SEQUENCE IS WHAT THIS REPLACED. ResolveForAddressOf re-sequenced the front half
    /// by hand and dropped two of its screens: <c>SET P TO ADDRESS OF L-REC</c> over a LINKAGE record the procedure
    /// division may not reference (§2023:13.7.3 SR4) compiled clean, and an ambiguous or index-name operand was told it
    /// "names none" where <c>MOVE</c> drew COBOL1639 / COBOL1637.</para>
    /// <para><paramref name="forAddressOf"/> is the one place the operand differs: §2023:8.4.3.11.3 SR1 admits only a data
    /// item of the file, working-storage, local-storage or linkage section, so an object-property or object-view
    /// chain (§2023:8.4.3.9, §2023:8.4.3.5) is not tried — it would synthesize a property temp, and register a pending
    /// §2023:8.4.3.9.4 operation, for an operand the caller must refuse — and a name that is one of those (the answer is a
    /// place, or a chain) draws the caller's own operand-shape refusal, not "not defined".</para></summary>
    private Identified Identify(Core.DataReferenceContext dref, bool report, bool forAddressOf)
    {
        DataReferenceCst r = dref;
        // The three answers (kb/Work PB1030). The written text is built only on a failure path.
        RefResolution Refused() => RefResolution.Refused(DataBinder.WrittenText(dref));
        RefResolution Deferred(DeferredShape shape) => RefResolution.Deferred(shape, DataBinder.WrittenText(dref));
        static RefResolution Resolved(Place place) => RefResolution.Resolved(place, "");
        static Identified Ends(RefResolution answer) => new(answer, null, default, null);
        // A special register — LINAGE-COUNTER (I-O control system, ISO §2023:8.4.3.14), LINE-/PAGE-COUNTER (Report
        // Writer control system, ISO §2023:8.4.3.15) — is runtime-sourced, never a storage Place; the binder routes it
        // to BoundLinageCounterRef / BoundReportCounterRef (StatementBinder.ReportWriter.cs). The early return is
        // LOAD-BEARING for the QUALIFIED form (`LINAGE-COUNTER OF file`, `LINE-COUNTER OF report`): there
        // r.BaseName is the FILE-/REPORT-NAME qualifier and would otherwise mis-resolve here as a base data-name.
        if (r.Register != SpecialRegister.None) return Ends(Deferred(DeferredShape.UnroutedSpecialRegister));
        // No base word outside a special register is a parse-error recovery node: the parser reported it.
        if (r.BaseName is not { } name) return Ends(Refused());

        // The OCCURS DYNAMIC CAPACITY register (ISO §2023:13.18.38 GR15 / §2023:8.5.1.9.1; data-model D9): an implicitly-
        // defined VIEW over the owning dynamic table's current capacity — never a storage item, so it is not in
        // ByName and is resolved HERE (before ordinary name lookup) to a CapacityRegisterPlace whose Read() emits
        // {tablePath}.Capacity. The NAME identifies it outright (§2023:13.18.38.3 SR30 first sentence — "Data-name-3
        // shall not be defined elsewhere in the source element"), so every other question is a predicate over the
        // reference AS WRITTEN and belongs to this one screen, not to a fall-through (kb/Work PB457).
        if (CapacityRegisterFor(dref) is { } capReg)
            return Ends(CapacityPlaceOf(dref, capReg) is { } capPlace ? Resolved(capPlace) : Refused());

        // The PREDEFINED OBJECT REFERENCE EXCEPTION-OBJECT (ISO §2023:8.4.3.6; kb/Work PB922): §2023:8.4.3.6.3 SR2 —
        // "EXCEPTION-OBJECT is implicitly described as class object and category object reference, as an external
        // data item, and as a universal object reference" — so the STANDARD declares this name, no data
        // description entry does, and it is resolved HERE (before ordinary name lookup, the CAPACITY / DEBUG-ITEM
        // pattern) to a read-only VIEW over the run unit's one instance (§2023:8.4.3.6.4 GR2).
        // ⛔ IT IS RESOLVED IN THE ONE RESOLVER SO EVERY CALLER INHERITS IT. Before this, only SET asked the
        // question (OoBinder's own arm), so every OTHER reference — `IF EXCEPTION-OBJECT = NULL`, an INVOKE
        // receiver, a function argument, and even the ILLEGAL `MOVE U TO EXCEPTION-OBJECT` — drew COBOL1639
        // "'EXCEPTION-OBJECT' is not defined … Check the spelling, or declare the item", advice the very next
        // diagnostic (COBOL0901, "is a reserved word … cannot be used as a user-defined word") forbids taking.
        // The RECEIVING half of SR1 is screened at the ONE receiving chokepoint (ExpressionBinder.ResolveReceiving);
        // this arm is what makes the SENDING half legal source rather than a typo.
        // The predicate is <see cref="IsExceptionObjectRegister"/> — the ONE answer every caller asks for.
        if (IsExceptionObjectRegister(dref)) return Ends(Resolved(new ExceptionObjectPlace(data.State.Implicit.ExceptionObjectRegister)));

        // The X3.23-1985 DEBUG-ITEM special register / member (VCR Table 7 row 7.17): an IMPLICITLY-defined read-only
        // VIEW over the program-instance __dbgItem — not in ByName, so resolved HERE (before ordinary name lookup) to
        // a DebugRegisterPlace. Registered ONLY when a procedure-subject debugging declarative is active under WITH
        // DEBUGGING MODE (DebugRegisters empty otherwise → this never fires for a non-debug program). The unqualified,
        // unsubscripted form is covered, with or without a reference modifier: DEBUG-CONTENTS (1:12) is the reference
        // modification of an alphanumeric item like any other (§2023:8.4.3.3.3 SR1; X3.23-1985 USE FOR DEBUGGING general rule 10
        // describes DEBUG-CONTENTS as PICTURE X(n), kb/Work PB4858), so the register's place meets the reference-modification
        // tail below. A qualified DEBUG-* falls through to loud.
        if (data.State.Implicit.DebugRegisters.TryGetValue(name, out var dbg) && dref.dataReferenceSuffix().All(suffix => suffix.refModPart() is not null))
        {
            var debugWritten = ReadWritten(dref);
            if (!RefMods.ScreenRefModCount(dref, debugWritten, name)) return Ends(Refused());
            return new Identified(null, dbg.Item, debugWritten, [], new DebugRegisterPlace(dbg.Item, dbg.Member));
        }

        // A REPORT SECTION SUM COUNTER (ISO §2023:13.18.54.4 GR5 — the data-name after the level number names the
        // COUNTER, not the printable item; GR12 permits procedure division statements to read and alter it): an
        // IMPLICITLY-defined VIEW over the report engine's counter, not in ByName, so it is resolved HERE — the
        // CAPACITY-register pattern — to a ReportSumCounterPlace whose read/write are SumImage/SetSumImage
        // (kb/Work PB840). Its qualifiers are the report group entries above it and, outermost, its REPORT-NAME
        // (§2023:8.4.2.2.3 SR4; §2023:8.4.2.2.2 Format 1's file-report-qualifier — kb/Work PB1454). A REPEATING entry's
        // counter is a table and takes the ordinary subscripts (kb/Work PB1271).
        if (Counters.SumCounterFor(dref, name, report) is { } sumCounter) return Ends(sumCounter);

        // The reference AS WRITTEN, read by the ONE decomposition (kb/Work PB443 — see WrittenReference).
        var written = ReadWritten(dref);
        var qualifiers = written.Qualifiers;

        // ISO §2023:8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
        // grammar cannot express this (dataReferenceSuffix* and qualification's own (subscriptPart|refModPart)*
        // both admit unlimited ref-mods), so it is COUNTED (WrittenReference.RefModCount). Before that count,
        // `??=` kept the FIRST of each of the two carriers a ref-mod then had and one outranked the other, so
        // `MOVE A (3:4)(2:2)` COMPILED CLEAN and returned A(2:2) — a silent wrong value, not a composition and not
        // a rejection.
        if (!RefMods.ScreenRefModCount(dref, written, name)) return Ends(Refused());

        // A reference whose chain ends in a non-word object (SELF, SUPER, a view, a function, NULL) is never a data name.
        DataItem? item = written.PropertyObject is not null ? null
            : qualifiers.Count > 0 ? Names.ResolveQualified(name, qualifiers) : Names.ResolveUnqualified(name);
        // §2023:8.4.3.1.2 Format 7 (kb/Work PB1425): `property-name-1 OF identifier-3` is textually a qualified data
        // reference, so it legitimately FAILS normal qualification. ResolveObjectProperty splits the chain — identifier-3
        // takes the qualifiers and the subscripts (§2023:8.4.3.1.4 GR1 a) before d)) — and synthesizes the §2023:8.4.3.9.4 GR1–GR3
        // temp; the rest of THIS method gives the temp the reference-modification tail (§2023:8.4.3.1.4 GR1 g)), which
        // §2023:8.4.3.9.3 SR5/SR6 permit on the property value. A view of a qualified data item written `A OF G AS C` arrives
        // here too, and is that view.
        bool objectProperty = false;
        if (item is null && forAddressOf)
        {
            // An ADDRESS OF operand is a data item of a section (§2023:8.4.3.11.3 SR1): a property or a view of one is not,
            // and trying it would register a pending §2023:8.4.3.9.4 operation for an operand the caller refuses. The
            // caller's own operand-shape refusal names the rule, so a chain stays silent here; any other name that
            // resolves to nothing is reported like every other identifier's.
            if (report && !ObjectProperties.IsObjectViewOfQualifiedItem(dref) && !ObjectProperties.IsObjectPropertyReference(dref))
                Names.ReportUnidentified(dref, name, qualifiers);
            return Ends(Refused());
        }
        if (item is null)
        {
            if (ObjectProperties.ViewOfQualifiedItem(dref, report) is { } viewOfItem) return Ends(viewOfItem);
            var (temp, final) = ObjectProperties.ResolveObjectProperty(dref, name, written);
            if (final is not null) return Ends(final);
            objectProperty = (item = temp) is not null;
        }
        if (item is null)
        {
            // The NAME resolves to nothing — a typo or a mis-qualification, never a feature gap (kb/Work R30).
            // Every later null in this method is an unsupported-shape staging of a name that DID resolve.
            if (report) Names.ReportUnidentified(dref, name, qualifiers);
            return Ends(Refused());
        }

        List<Position> indexExprs = [];   // a property temp has no OCCURS: the written subscripts were identifier-3's
        if (!objectProperty)
        {
            if (report) Names.ScreenLinkageReference(dref, item);
            if (Subscripts.ReadSubscripts(dref, item, written.SubscriptGroup, out var subscripts) is { } subscriptFailure)
                return Ends(subscriptFailure);
            indexExprs = subscripts;
        }
        return new Identified(null, item, written, indexExprs);
    }

    /// <summary>The place for an already-resolved <paramref name="item"/> using the SUBSCRIPTS of
    /// <paramref name="dref"/> — the condition-name-with-subscripts form (ISO §2023:8.4.2.3 Format 2): a level-88
    /// reference's subscripts identify the occurrence of its CONDITIONAL VARIABLE. The same closed answer as
    /// <see cref="Resolve"/> (kb/Work PB1030), with a deferral put on the unbuilt ledger the same way.</summary>
    public RefResolution ResolveForItem(Core.DataReferenceContext dref, DataItem item)
    {
        bool savedSegment = SegmentDeferred;
        SegmentDeferred = false;
        try
        {
            // SR2 names the CONDITIONAL VARIABLE for a condition-name reference (§2023:8.4.2.3 Format 2), which is
            // exactly the item this entry is handed — so the ONE subscript reading applies unchanged (kb/Work PB877).
            var answer = Subscripts.ReadSubscripts(dref, item, SubscriptBinder.SubscriptGroupOf(dref), out var indexExprs)
                ?? (Places.PlaceForItem(item, indexExprs, out var gap) is { } place
                    ? RefResolution.Resolved(place, "") : ItemFailure(dref, gap));
            if (answer.Outcome == RefOutcome.Deferred) data.Edition.NoteUnbuilt(answer.Feature);
            return answer;
        }
        finally { SegmentDeferred = savedSegment; }
    }

    /// <summary>⛔ THE ONE DECOMPOSITION OF A <c>dataReference</c> AS WRITTEN (kb/Work PB443). Three resolutions
    /// each wrote this walk out for themselves — <see cref="ResolveImplCore"/>, <see cref="ResolveForAddressOf"/>
    /// and <see cref="SubscriptGroupOf"/> — and a fourth, SEARCH's identifier-1, wrote none at all and took
    /// <c>dref.cobolWord()</c>, the BASE WORD, so every suffix the programmer wrote was discarded unread. Reading
    /// a written reference is ONE job; the §2023:8.4.2.2 lookup and every shape rule are predicates over the result.
    /// <para>Mirrors the grammar exactly: <c>dataReference : cobolWord dataReferenceSuffix*</c> with
    /// <c>dataReferenceSuffix : subscriptPart | refModPart | qualification</c>, and <c>qualification</c> carrying
    /// its OWN <c>(subscriptPart | refModPart)*</c> tail. ⛔ Counts REF-MODS ONLY: a subscript followed by a
    /// ref-mod (<c>T(I) (2:3)</c>) is the legal §2023:8.4.3.1.4 GR1 a→g order and must stay untouched.</para></summary>
    internal static WrittenReference ReadWritten(Core.DataReferenceContext dref)
    {
        List<string>? qualifiers = null;
        Core.SubscriptPartContext? subCtx = null;
        Core.RefModPartContext? cleanRef = null;
        Core.PropertyObjectContext? propertyObject = null;
        int refModCount = 0;

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                (qualifiers ??= []).Add(q.cobolWord().Name());
                if (q.subscriptPart() is [var qs, ..]) subCtx ??= qs;
                refModCount += q.refModPart().Length;
                if (q.refModPart().Length > 0) cleanRef ??= q.refModPart()[0];
            }
            else if (suffix.refModPart() is { } rmp) { refModCount++; cleanRef ??= rmp; }
            else if (suffix.subscriptPart() is { } s) subCtx ??= s;
            else if (suffix.propertyObject() is { } po)
            {
                propertyObject ??= po;
                // §2023:8.4.3.1.4 GR1 g): a reference modifier written after a function object's arguments applies to the
                // whole identifier, last — never to the object reference the function returns (kb/Work PB1425).
                foreach (var resultRef in ObjectPropertyResolver.ResultRefModsOf(po)) { refModCount++; cleanRef ??= resultRef; }
            }
        }
        return new WrittenReference(qualifiers, subCtx, cleanRef, refModCount, propertyObject);
    }

    /// <summary>The user-defined word a <c>dataReference</c> names, as written — or null for the LINAGE-COUNTER,
    /// LINE-COUNTER and PAGE-COUNTER special-register alternatives (§2023:8.4.3.14; §2023:8.4.2.2.2 Format 8, the
    /// qualified-report-counter), which name a register, not a word. ⛔ In those alternatives <c>dref.cobolWord()</c> is the QUALIFIER (<c>LINAGE-COUNTER OF F</c> gives
    /// F, the file-name) or absent, never the referenced name, so a query of what the word is declared as asks this
    /// (kb/Work PB1906, review w20261769b R1-1: <c>SET LINAGE-COUNTER TO TRUE</c> crashed on the absent word).</summary>
    internal static string? WrittenWordOf(Core.DataReferenceContext dref) =>
        dref.LINAGE_COUNTER() is null && dref.LINE_COUNTER() is null && dref.PAGE_COUNTER() is null
            ? dref.cobolWord().Name()
            : null;

    /// <summary>A reference written where a general format prints a <b>table</b> operand, resolved and read: the
    /// <see cref="DataItem"/> the qualifiers single out, plus the two facts the statement's own syntax rules are
    /// about — how many subscripts were written, and whether a reference modification was.</summary>
    /// <param name="Item">The item the §2023:8.4.2.2 candidate-set match singles out.</param>
    /// <param name="SubscriptCount">Subscripts AS WRITTEN, 0 for an unsubscripted reference.</param>
    /// <param name="IsReferenceModified">True when the reference carries a reference modification.</param>
    public readonly record struct TableOperand(DataItem Item, int SubscriptCount, bool IsReferenceModified);

    /// <summary>⛔ THE ONE RESOLUTION OF A REFERENCE WRITTEN WHERE A GENERAL FORMAT PRINTS A <b>TABLE</b> OPERAND
    /// (kb/Work PB443) — SEARCH's and SEARCH ALL's identifier-1, the Format-2 SORT's data-name-2 and, for the same
    /// reason, a SORT/MERGE KEY (kb/Work PB1173): a statement whose rule is about the NAMED ITEM — whether it may be a
    /// key at all — asks what the name denotes before any occurrence of it is resolved. It is deliberately NOT <see cref="Resolve"/>: a
    /// table operand names the TABLE, not one occurrence of it, and ISO §2023:14.9.37.3 SR2 says identifier-1 "shall
    /// not be subscripted at the level for which the SEARCH is applicable" — so there is no subscript for the
    /// searched level and therefore no <see cref="Place"/> to build. What such a statement needs is the data item
    /// plus the reference AS WRITTEN.
    /// <para>⛔ THE QUALIFICATION IS THE ORDINARY ONE, AND THAT IS THE WHOLE POINT. Before this, SEARCH reduced
    /// identifier-1 to <c>drefs[0].cobolWord()</c> — its BASE WORD — and took
    /// <c>candidates.FirstOrDefault(i =&gt; i.IsTable)</c>, so with two groups each declaring a table <c>E</c>,
    /// <c>SEARCH E IN G2</c> searched whichever <c>E</c> was declared FIRST: a wrong answer on legal, unambiguous
    /// COBOL, decided by declaration order, with the qualifier that would have settled it thrown away unread
    /// (§2023:8.4.2.2 — qualification establishes uniqueness; §2023:14.9.37.4 GR1 — the search varies the first index
    /// associated with identifier-1, and for a qualified reference identifier-1 is the QUALIFIED table).</para>
    /// <para>Reporting posture — the DEMANDING one of <see cref="Resolve"/>: a name that identifies no item, or
    /// that qualification cannot single out, draws COBOL1639 here, so a null the caller gets back is already
    /// reported (<see cref="WasDiagnosed"/> answers true). A SPECIAL REGISTER returns null having reported
    /// NOTHING — it identifies a resource, just not one with a data description, and the verdict that belongs to
    /// it is the statement's own operand rule, written in the one check catalog.</para></summary>
    public TableOperand? ResolveTableOperand(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var written = ReadWritten(dref);
        if (Names.FindReportedItem(dref, name, written.Qualifiers) is not { } item) return null;
        // The subscripts AS WRITTEN, split by the ONE declaration-informed splitter (kb/Work PB136) — the same
        // reading SR8/SR9 already demand of a Format-2 key subscript, for the same reason: the RENDERED index
        // expression has erased how many operands the programmer wrote.
        return new TableOperand(item, Subscripts.SubscriptSegmentsOf(written.SubscriptGroup)?.Count ?? 0,
            written.IsReferenceModified);
    }

    /// <summary>Build the <see cref="Place"/> for a by-name reference with ALREADY-RENDERED C# index expressions
    /// (one per OCCURS level, outermost first). A level-66 RENAMES entry is built by the same
    /// <see cref="PlaceForItem"/> as every other item (kb/Work PB1380); it is never a table element, so a written
    /// subscript on one builds no place.</summary>
    internal Place? ResolveByName(string name, IReadOnlyList<string> qualifiers, IReadOnlyList<Position> indexExprs) =>
        Names.FindItem(name, qualifiers) is { } item ? Places.PlaceForItem(item, indexExprs) : null;

    /// <summary>Resolve an <c>ADDRESS OF</c> operand (ISO §2023:8.4.3.11) to its item plus the OCCURS displacement
    /// of its subscripts — the <c>(idx − 1) × width</c> character-position terms (<see cref="OffsetTerm"/>) within
    /// the item's storage class, or a null displacement for an unsubscripted (possibly OF/IN-qualified) reference. The
    /// address of occurrence k is the class cell displaced by the SAME in-class occurrence law the Tier-B view
    /// window uses (<see cref="PlaceForItem"/> — a table lays its occurrences end-to-end in the ONE cell
    /// image), so the two share one formula, <see cref="PositionOffset"/>.
    /// <para>⛔ A REFERENCE-MODIFIED OPERAND IS LEGAL (kb/Work PB1407): §2023:8.4.3.11.3 SR4 a) itself speaks of
    /// "subscripting and reference modification in identifier-1", and identifier-1 is a general identifier. It goes
    /// through the ONE ref-mod admission <see cref="ReadScreenedRefMod"/> that <see cref="Resolve"/> uses (SR3
    /// count, SR1 exclusion, the spec, the literal range screen); the address is that of its leftmost position,
    /// which the caller adds to the item's.</para>
    /// Null overall = an unresolvable name, a subscript-count mismatch, or a reference refused by one of those
    /// screens — the caller reports loud, never a wrong address.</summary>
    internal AddressOfOperand? ResolveForAddressOf(Core.DataReferenceContext dref)
    {
        // ⛔ THE ONE IDENTIFICATION STEP (kb/Work PB3287): the special-register and implicit-name screens, the §2023:8.4.2.2
        // lookup and its report (an ambiguous or index-name operand is told so, as MOVE tells it), the §2023:13.7.3 SR4
        // LINKAGE screen (a record the procedure division may not reference is not addressable either) and the
        // §2023:8.4.2.3.3 SR2/SR3/SR5 subscript screen (kb/Work PB681: the omitted-list arm used to return the address of
        // the table's FIRST occurrence for `ADDRESS OF E`). An answer is an operand that is no data item of a section
        // (§2023:8.4.3.11.3 SR1) or a reference already refused: null, and the caller names the operand-shape rule unless
        // the resolver has (WasDiagnosed). What stays HERE is the address-specific tail.
        var id = Identify(dref, report: true, forAddressOf: true);
        if (id.Item is not { } item || id.ImplicitPlace is not null) return null;   // a register is no data item of a section
        var written = id.Written;
        var subCtx = written.SubscriptGroup;
        var exprs = id.IndexExprs!;
        RefModSpec? refMod = null;
        Place? bitPlace = null;
        OdoGroupPlace? extent = null;
        // The reference as written, for the statement that holds the operand to name it among its operands (X3.23-1985 USE FOR DEBUGGING
        // general rule 5: "explicitly references"; kb/Work PB4855); null for a shape the place builder cannot model.
        var operandPlace = Places.PlaceForItem(item, exprs);
        if (written.IsReferenceModified || BitLayout.IsBitItem(item))
        {
            var inner = operandPlace;
            if (written.IsReferenceModified)
            {
                if (RefMods.ReadScreenedRefMod(dref, written, inner?.Item ?? item, out _) is not { } spec) return null;
                refMod = spec;
                // §2023:8.4.3.3.4 GR5 / 5): the area a position is tested against is identifier-1's CURRENT size, and an
                // occurs-depending group's is a run-time value (§2023:13.18.38.4 GR8) — the same extent a read of the
                // reference measures through its sending image (kb/Work PB1969).
                extent = inner as OdoGroupPlace;
            }
            if (BitLayout.IsBitItem(item)) bitPlace = inner;
        }
        if (subCtx is null) return new AddressOfOperand(item, null, refMod, bitPlace, extent, operandPlace);
        // The in-class OCCURS levels outer→inner — the PlaceForItem Tier-B walk (same layout, same formula).
        var occursLevels = new List<DataItem>();
        bool underDynamicTable = false;
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, item.Class); n = n.Parent)
        {
            if (n.Occurs is not null) occursLevels.Add(n);
            underDynamicTable |= n.IsDynamicTable;
        }
        // SR6's refusal is the binder's, and it needs the operand: a dynamic level has no fixed stride to render.
        if (underDynamicTable) return new AddressOfOperand(item, null, refMod, bitPlace, extent, operandPlace);
        occursLevels.Reverse();
        if (occursLevels.Count != exprs.Count) return null;   // wrong subscript count → loud
        // The ONE displacement term of a fixed level (FixedLevelTerm), the one the Tier-B view window takes.
        var disp = occursLevels.Select((lv, k) => PlaceBuilder.FixedLevelTerm(lv, exprs[k])).ToList();
        return new AddressOfOperand(item, disp, refMod, bitPlace, extent, operandPlace);
    }

    /// <summary>What an <c>ADDRESS OF identifier-1</c> operand (ISO §2023:8.4.3.11) resolves to: the item, the OCCURS
    /// displacement of its subscripts, the reference modification when one is written, and, for a bit item, the
    /// place its §2023:8.4.3.11.3 SR4 alignment proof walks.</summary>
    /// <param name="CurrentExtent">The occurs-depending group place whose CURRENT extent bounds a written reference
    /// modifier (§2023:8.4.3.3.4 5) — "a position outside the area of identifier-1", the area being the group's current
    /// size under §2023:13.18.38.4 GR8); null for every operand whose size is a compile-time fact.</param>
    /// <param name="Item">The data item the reference names (the table ELEMENT's item for a subscripted one).</param>
    /// <param name="OccursDisplacement">The <c>(idx − 1) × width</c> byte terms within the item's storage class, or null
    /// for an unsubscripted reference — and for an item at or under a dynamic-capacity table, which §2023:8.4.3.11.3 SR6
    /// refuses before any address is formed (no occurrence of one has a fixed stride).</param>
    /// <param name="RefMod">The screened reference modifier, or null when none is written. §2023:8.4.3.11.4 GR1's
    /// "address of identifier-1" is then the address of the unique data item reference modification creates
    /// (§2023:8.4.3.3.4 GR5), i.e. of its leftmost position.</param>
    /// <param name="Written">The item's place with its subscripts as the operand wrote them, for every item the place builder models; the bound
    /// <c>BoundAddressOf.Written</c>, the operand the statement explicitly references.</param>
    /// <param name="SubscriptedPlace">The item's place with its subscripts and WITHOUT the reference modification,
    /// built only for a bit item (SR4's proof is a walk over a place); null for any other item and for a shape the
    /// place builder cannot model, which the proof then accepts rather than reject what it cannot prove.</param>
    internal readonly record struct AddressOfOperand(
        DataItem Item, IReadOnlyList<OffsetTerm>? OccursDisplacement, RefModSpec? RefMod, Place? SubscriptedPlace,
        OdoGroupPlace? CurrentExtent, Place? Written);

    /// <summary>Why a written reference that NAMES a CAPACITY register still has no place (kb/Work PB457). Each arm
    /// is a syntax rule, not a hole: the register's name identifies it outright (§2023:13.18.38.3 SR30 first sentence),
    /// so a reference that names it is never "not defined".</summary>
    internal enum CapacityRefFault
    {
        /// <summary>A legal reference — <see cref="CapacityRef.Place"/> is non-null.</summary>
        None,
        /// <summary>§2023:13.18.38.3 SR31 — "Data-name-3 shall not be subscripted."</summary>
        Subscripted,
        /// <summary>§2023:8.4.3.3.3 SR1 — the register is not an identifier-1 reference modification may name.</summary>
        RefModified,
        /// <summary>§2023:13.18.38.3 SR30 second sentence with §2023:8.4.2.2.3 SR4 — the written OF/IN qualifiers do not name
        /// successively more inclusive context of the register's implied position.</summary>
        Qualifiers,
        /// <summary>§2023:8.4.2.2.3 SR1 — the written qualifiers reach more than one register of the nearest declaring
        /// element: the TYPE / SAME AS copies of one written entry each carry the register (§2023:13.18.38.3 SR30 second
        /// sentence, kb/Work PB2988), so "uniqueness shall be established through a sequence of qualifiers".</summary>
        Ambiguous,
        /// <summary>The register's table is itself subordinate to a table, so SR30 puts the register inside that
        /// outer table: §2023:8.4.2.3.3 SR3/SR5 then REQUIRE a subscript that §2023:13.18.38.3 SR31 FORBIDS, and no reference
        /// form is writable. ⚠ DETERMINATION — see the diagnostic's own note.</summary>
        UnderATable,
    }

    /// <summary>A written reference matched against the source element's named CAPACITY registers: the owning
    /// dynamic-capacity <paramref name="Table"/>, the <paramref name="Register"/> view item, the
    /// <paramref name="Place"/> when the reference is legal, and the <paramref name="Fault"/> otherwise.
    /// <paramref name="Reached"/> is every register's table of the nearest declaring element the written qualifiers
    /// reach (more than one only under <see cref="CapacityRefFault.Ambiguous"/>). <paramref name="Place"/> is non-null
    /// exactly when <paramref name="Fault"/> is <see cref="CapacityRefFault.None"/>: an ambiguous reference has no
    /// place until the reporting site's <c>--permissive</c> disposition picks a register, and that register is then
    /// judged by <see cref="CapacityRegisterPlaceOf"/> like an unambiguous one (kb/Work PB2988 review R1-1).</summary>
    internal readonly record struct CapacityRef(
        DataItem Table, DataItem Register, CapacityRegisterPlace? Place, CapacityRefFault Fault,
        NameCandidates<DataItem> Reached);

    /// <summary>⛔ THE ONE MATCH OF A WRITTEN REFERENCE AGAINST THE NAMED OCCURS DYNAMIC CAPACITY REGISTERS
    /// (kb/Work PB457) — a PURE check over the registers the ONE name lookup
    /// <see cref="Model.SymbolTable.CapacityCandidates"/> reaches in the active scope (the unit's
    /// <see cref="ForestSlice.CapacityRegisters"/>, or a method's own, kb/Work PB3165) with NO side effects (unlike
    /// the full <see cref="Resolve"/> pipeline, which routes an unresolved qualified name through the
    /// property-reference hook and enqueues a pending op). The SET Format 14 reroute peek uses this so it never
    /// mints a spurious property temp/op for a non-capacity target (data-model D9; OCCURS DYNAMIC review #7).
    /// <see langword="null"/> means the base name names NO register; a non-null result with a null
    /// <see cref="CapacityRef.Place"/> is a rule violation the reporting caller states.
    /// <para>⛔ IT MATCHES THE REFERENCE, NOT A BARE NAME. The prior shape was
    /// <c>r.HasNoSuffix &amp;&amp; CapacityRegisters.TryGetValue(...) &amp;&amp; BuildTablePath(table) is { }</c> —
    /// two conjuncts that each DELETED a reference form instead of judging it. <c>HasNoSuffix</c> is a test on the
    /// PARSE shape and <c>dataReferenceSuffix</c> carries QUALIFICATION as well as subscripts, so the legal
    /// <c>SET WS-CAP OF WS-TABLE TO 7</c> (§2023:13.18.38.3 SR30 with §2023:8.4.2.2.3 SR2 — "A name may be qualified even
    /// though it does not need qualification") failed it; the zero-index <c>BuildTablePath</c> returned null for
    /// every table with a table ancestor. Because the register is off <c>ByName</c>, failing either conjunct was
    /// not a fallback but the END of resolution, and the general resolver then said COBOL1639 "is not defined"
    /// — false about a name the OCCURS clause declares.</para>
    /// <para>The qualifier test is the ONE §2023:8.4.2.2 matcher (<see cref="DataBinder.QualifierChainMatches"/>), read
    /// off the register's real <see cref="DataItem.Parent"/> — SR30's "treated as though implicitly defined at the
    /// same level as the entry containing the OCCURS clause", set where the register is minted
    /// (<c>DataBinder.Odo.cs</c>). Within ONE source element two registers share a spelling only when they are the
    /// TYPE / SAME AS copies of one written entry (SR30's first sentence forbids every other duplicate; kb/Work
    /// PB2988), and then SR30's second sentence makes them told apart by qualification: the registers the written
    /// qualifiers reach in the nearest declaring element are counted, and more than one is §2023:8.4.2.2.3 SR1's
    /// ambiguity (<see cref="CapacityRefFault.Ambiguous"/>), judged by the ONE verdict
    /// <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>.</para>
    /// <para>⛔ ACROSS SOURCE ELEMENTS IT IS §2023:8.4.6.2.1 (kb/Work PB1674). A contained program also sees every
    /// container's GLOBAL record's registers, nearest container first after its own (<see cref="ForestSlice.CapacityRegisters"/>),
    /// and rule 1 applies "the normal rules for qualification" over that whole set before rule 3 picks the nearest
    /// declaring element: the first register the written qualifiers reach is the candidate (the nearest one when
    /// none does, so the qualifier fault is stated about it), and an ordinary data-name of the spelling in a NEARER
    /// element takes the reference instead (<see cref="Model.SymbolTable.NearerDataNameHides"/> — the rule the
    /// index-name class already obeys), handing it back to the ordinary lookup.</para></summary>
    internal CapacityRef? CapacityRegisterFor(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.BaseName is not { } name || data.Symbols.CapacityCandidates(name, data.ActiveScope) is not { } tables) return null;
        var written = ReadWritten(dref);
        var reached = tables.FindAll(t => t.OccursSpec?.CapacityRegister is { } cand
                                          && data.QualifierChainMatches(cand, written.Qualifiers));
        // The nearest declaring element's reached registers (the list is in nearest-element order, §2023:8.4.6.2.1 3)), or
        // the nearest register at all when the qualifiers reach none, so the qualifier fault is stated about it.
        DataItem table = reached.Count > 0 ? reached[0] : tables[0];
        int depth = data.State.Forest.DeclaringDepth(table);
        reached.RemoveAll(t => data.State.Forest.DeclaringDepth(t) != depth);
        if (table.OccursSpec?.CapacityRegister is not { } reg
            || data.Symbols.NearerDataOrConditionNameHides(name, written.Qualifiers, depth)) return null;
        var place = CapacityRegisterPlaceOf(table);
        var fault =
              written.SubscriptGroup is not null                     ? CapacityRefFault.Subscripted
            : written.RefModCount > 0                                ? CapacityRefFault.RefModified
            : reached.Count == 0                                     ? CapacityRefFault.Qualifiers
            : reached.Count > 1                                      ? CapacityRefFault.Ambiguous
            : place is null                                          ? CapacityRefFault.UnderATable
            : CapacityRefFault.None;
        return new CapacityRef(table, reg, fault is CapacityRefFault.None ? place : null, fault,
            new NameCandidates<DataItem>(reached));
    }

    /// <summary>⛔ THE ONE JUDGMENT OF A REGISTER ITSELF, once the written reference has picked it: the place of
    /// <paramref name="table"/>'s CAPACITY register, or <see langword="null"/> when the table is subordinate to a
    /// table (<see cref="CapacityRefFault.UnderATable"/>: §2023:13.18.38.3 SR30 puts the register inside that outer table,
    /// where §2023:8.4.2.3.3 SR3 requires the subscript §2023:13.18.38.3 SR31 forbids). It is asked of the register a unique
    /// reference names AND of the one the <c>--permissive</c> disposition of an ambiguous reference picks, because
    /// ambiguity is a fault of the WRITTEN reference and does not excuse the register it resolves to (kb/Work PB2988
    /// review R1-1: when only the unique arm asked it, the permissive arm handed back a null place with no
    /// diagnostic, COBOL2362).</summary>
    private CapacityRegisterPlace? CapacityRegisterPlaceOf(DataItem table) =>
        table.OccursSpec?.CapacityRegister is { } reg && PlaceBuilder.BuildTablePath(table) is { } path
            ? new CapacityRegisterPlace(path, reg) : null;

    /// <summary>⛔ <b>THE ONE TEST for "this written reference IS the predefined object reference
    /// EXCEPTION-OBJECT"</b> (ISO §2023:8.4.3.6; kb/Work PB922) — spelling, FORM and EDITION together, so no caller can
    /// hold a different opinion about what the name is. It sits beside <see cref="CapacityRegisterFor"/> because it
    /// answers the same shape of question for the other implicitly-declared name, and because the RESOLVER is where
    /// the answer belongs: §2023:8.4.3.6.3 SR2 declares the name, no data description entry does, and a caller that does
    /// not ask gets "not defined" about a name the standard itself declares.
    /// <para>⛔ THE EDITION IS PART OF THE QUESTION, AND LEAVING IT OUT OF ONE ARM REJECTED LEGAL SOURCE. The
    /// object-orientation facility — and with it §2023:8.9's reservation of the word — arrived in COBOL-2002, so at
    /// <c>--std 85</c> EXCEPTION-OBJECT is an ORDINARY user-defined word: <c>01 EXCEPTION-OBJECT PIC X(4).</c>
    /// followed by <c>MOVE "ABCD" TO EXCEPTION-OBJECT</c> is conforming '85 source. When only the resolver arm
    /// carried the gate and the receiving screen compared the spelling by itself, that program drew
    /// COBOL2196 — SR1 quoted at a program that never referenced the register
    /// (feedback_two_arm_dispatch, feedback_edition_gate_sweep). From 2002 the reservation makes the name
    /// undeclarable, so there is nothing left for the interception to shadow.</para>
    /// <para>Only the plain unqualified / unsubscripted / unreference-modified form is the register:
    /// §2023:8.4.3.6.2's general format is the bare word, so a suffixed occurrence is some other (undefined)
    /// reference and must keep the ordinary resolution.</para></summary>
    internal bool IsExceptionObjectRegister(Core.DataReferenceContext dref) =>
        data.Edition.Edition.Has(2002)
        && dref.dataReferenceSuffix().Length == 0
        && Procedure.OoBinder.OoIsExceptionObject(dref.GetText(), data.CobolWords);

    /// <summary>The place of one entry of the X3.23-1985 DEBUG-ITEM group (kb/Work PB4903), reached by the entry the group lists
    /// (MOVE CORRESPONDING's pair walk), or <see langword="null"/> for an item that is no register entry.</summary>
    internal DebugRegisterPlace? DebugRegisterPlaceOf(DataItem entry) => data.State.Implicit.DebugPlaceOf(entry);

    /// <summary>The places of the named entries of the X3.23-1985 DEBUG-ITEM group, in the order of its description (general rule 10);
    /// the FILLER entries are not receivers (INITIALIZE: §2023:14.9.20.4 GR5a2) and have no place.</summary>
    internal IEnumerable<DebugRegisterPlace> DebugItemMemberPlaces() =>
        data.State.Implicit.DebugItemMembers.Select(m => new DebugRegisterPlace(m.Item, m.Member));

    /// <summary>The place a matched <see cref="CapacityRef"/> resolves to, REPORTING its fault when it has one —
    /// the single reporting site for every ill-formed reference to a named CAPACITY register (kb/Work PB457). Every
    /// arm names the rule the reference breaks; none of them can be "not defined", because the name IS defined.
    /// <para>⚠ <b>DETERMINATION (<see cref="CapacityRefFault.UnderATable"/>)</b> — a dynamic-capacity table nested
    /// within another table is legal to DEFINE (§2023:8.5.1.9.1 item 3: it "may be nested in any combination to the same
    /// number of levels as a fixed-capacity table") and its register may be NAMED, but no reference to that register
    /// is writable. §2023:13.18.38.3 SR30 puts the register at the same level as the OCCURS entry — inside the outer
    /// table, and deliberately unlike the occurs-depending item, which §2023:13.18.38.3 SR20 forces OUTSIDE the table —
    /// so §2023:8.4.2.3.3 SR3 ("the number of subscripts shall equal the number of OCCURS clauses in the description of
    /// the table element being referenced") and SR5 ("Each table element reference shall be subscripted except
    /// when such reference appears" — seven contexts, none of them this) require one subscript per enclosing table,
    /// while §2023:13.18.38.3 SR31 forbids any. The reading REJECTED was "the bare name designates the capacity of every
    /// occurrence", the occurs-depending analogy: it has no textual support, and it contradicts the per-occurrence
    /// capacity model §2023:15.3's table(ALL) enumeration already rests on (kb/Work PB62) — each outer occurrence holds
    /// its own <c>CobolDynTable</c> with its own capacity, so one SET cannot mean all of them.</para></summary>
    private Place? CapacityPlaceOf(Core.DataReferenceContext dref, CapacityRef cap)
    {
        if (cap.Fault is CapacityRefFault.None) return cap.Place;
        // R30 purity: a probe never diagnoses, and a written reference is reported once (kb/Work PB157). Every fault
        // but Ambiguous yields null at every ask; Ambiguous's --permissive pick must be the SAME place at every ask,
        // reported or not (the index-name sibling of wave 1073 review R1-1).
        bool report = !Probing && Diagnosed.Add(dref);
        if (!report && cap.Fault is not CapacityRefFault.Ambiguous) return null;
        string text = DataBinder.WrittenText(dref);
        string subject = cap.Register.CobolName ?? cap.Register.ClrName;
        string table = cap.Table.CobolName ?? cap.Table.ClrName;
        void ReportUnderATable() =>
            data.Edition.Error(DiagnosticCatalog.CapacityRegisterUnderTable,
                $"'{text}': '{subject}' is the CAPACITY register of '{table}', which is itself subordinate to a "
                + "table, so ISO §2023:13.18.38.3 SR30 places the register inside that outer table. ISO §2023:8.4.2.3.3 "
                + "SR3 and SR5 then require one subscript per enclosing OCCURS clause, and ISO §2023:13.18.38.3 SR31 "
                + "forbids subscripting data-name-3 — no reference form is writable. Name the register only on "
                + "a dynamic-capacity table that has no table ancestor, or read the capacity with FUNCTION "
                + "LENGTH over the subscripted inner table instead.");
        switch (cap.Fault)
        {
            case CapacityRefFault.Subscripted:
                data.Edition.Error(DiagnosticCatalog.CapacityRegisterSubscripted,
                    $"'{text}': '{subject}' is the CAPACITY register of the dynamic-capacity table '{table}' and "
                    + "ISO §2023:13.18.38.3 SR31 says \"Data-name-3 shall not be subscripted\". Write the register's "
                    + "name alone (optionally qualified), and subscript the TABLE's elements instead.");
                return null;
            case CapacityRefFault.RefModified:
                // §2023:8.4.3.3.3 SR1 lives in ONE place — RefModExclusion — and the register answers it like any item.
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{text}': reference modification of {RefModBinder.RefModExclusion(cap.Register) ?? "a CAPACITY register"} "
                    + "is not permitted (ISO §2023:8.4.3.3.3 SR1)");
                return null;
            case CapacityRefFault.Qualifiers:
                string innermost = cap.Register.Parent is { CobolName: { } p }
                    ? $"; the innermost permitted qualifier is '{p}'" : "";
                data.Edition.Error(DiagnosticCatalog.CapacityRegisterQualifier,
                    $"'{text}': the written qualifiers do not name context of '{subject}'. ISO §2023:13.18.38.3 SR30 "
                    + "treats a CAPACITY register \"as though implicitly defined at the same level as the entry "
                    + $"containing the OCCURS clause\", so '{subject}' stands beside '{table}' and its qualifiers "
                    + $"are the group names above it{innermost}. ISO §2023:8.4.2.2.3 SR4 requires them \"in the order "
                    + "of successively more inclusive levels in the hierarchy\".");
                return null;
            case CapacityRefFault.Ambiguous:
                // §2023:8.4.2.2.3 SR1, through the ONE verdict every name class shares. Its --permissive disposition
                // picks the first declared register, which is then judged like a unique reference's: a register
                // under a table is still COBOL2126, never a null place with no diagnostic (review R1-1).
                if (data.UniqueOrReportAmbiguous(cap.Reached, "the CAPACITY register reference", text, out _, report)
                    is not { } picked) return null;
                if (CapacityRegisterPlaceOf(picked) is { } pickedPlace) return pickedPlace;
                if (report) ReportUnderATable();
                return null;
            case CapacityRefFault.UnderATable:
                ReportUnderATable();
                return null;
            default:
                // CapacityRefFault.None cannot reach here (its Place is returned before the switch), and every other
                // member has a case above — CapacityRegisterReferenceDriftTests pins that, so a new fault cannot
                // ship diagnostic-less. This arm is an internal-error backstop, never a user-reachable path.
                throw new InvalidOperationException(
                    $"CapacityRefFault.{cap.Fault} carries no diagnostic (kb/Work PB457).");
        }
    }

}

/// <summary>A <c>dataReference</c> AS WRITTEN: the qualifier chain, the subscript group and the
/// reference-modification carriers, exactly as the source spells them and before any name is looked up.
/// <para>⛔ The <b>written</b> reference is a thing in its own right, and it is what a statement's own syntax
/// rules are about. ISO §2023:14.9.37.3 SR1 ("Identifier-1 shall not be reference-modified"), SR2 ("… shall not be
/// subscripted at the level for which the SEARCH is applicable") and SR3 ("… contained within one or more
/// other tables, for which the subscripting is still required") are three predicates over THIS, not over the
/// resolved item and not over a rendered access path — which is why SEARCH, which read only
/// <c>cobolWord()</c>, could enforce none of them and searched whichever table was declared first
/// (kb/Work PB443).</para></summary>
/// <param name="Written">The OF/IN qualifiers in written order, inner → outer, or null when the reference
/// carries none — the common case, which therefore allocates nothing.</param>
/// <param name="SubscriptGroup">The first <c>subscriptPart</c>: the reference's subscript list. Taken from the
/// base word's own suffix or from a qualification's suffix tail (<c>K OF E (IX)</c> hangs it off the
/// qualification).</param>
/// <param name="RefModPart">The first <c>refModPart</c> (<c>leftmost : length</c> as expressions).</param>
/// <param name="RefModCount">How many reference modifications the whole reference carries — §2023:8.4.3.3.3 SR3
/// admits at most one, and the count is the only way to see a second one.</param>
/// <param name="PropertyObject">The non-word object the qualifier chain ends in (<c>OF SELF</c>, <c>OF SUPER</c>,
/// <c>OF U AS C</c>, <c>OF FUNCTION F</c>, <c>OF NULL</c>) — §2023:8.4.3.1.2 Format 7's identifier-3 where a word cannot
/// spell it (kb/Work PB1425).</param>
internal readonly record struct WrittenReference(
    List<string>? Written,
    Core.SubscriptPartContext? SubscriptGroup,
    Core.RefModPartContext? RefModPart,
    int RefModCount,
    Core.PropertyObjectContext? PropertyObject)
{
    /// <summary>The shared empty qualifier list for an unqualified reference. Read-only by contract: every
    /// consumer (<see cref="ResolveQualified"/>, <see cref="ReportUnidentified"/>) only enumerates it.</summary>
    private static readonly List<string> NoQualifiers = [];

    /// <summary>The OF/IN qualifiers, inner → outer; empty for an unqualified reference.</summary>
    public List<string> Qualifiers => Written ?? NoQualifiers;

    /// <summary>True when the reference carries a reference modification in any of its spellings.</summary>
    public bool IsReferenceModified => RefModCount > 0;
}
