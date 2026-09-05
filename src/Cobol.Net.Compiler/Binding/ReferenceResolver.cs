// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, and the subscript / reference-modification token group (a flat SUBSCRIPT-mode stream the
///         binding layer interprets — the same grammar shape the legacy compiler proved over 364 NIST tests).</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/>, interpret
///         the subscripts to C# index expressions, and build the member-access path with each subscript attached to
///         its OCCURS level (outer→inner).</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, a reference-modified reference (<c>(s:l)</c> — G2-1c), or a subscript form not yet handled — so the
/// caller emits a loud not-implemented guard rather than silently mis-binding.
/// </summary>
/// <summary>Which ORDINAL-POSITION rule a rendered segment is subject to. The two positions share one token
/// renderer and one integrality rule, and differ only in the Table 13 condition a non-integer value sets:
/// §8.4.2.3.4 GR1b names EC-BOUND-SUBSCRIPT for a subscript, §8.4.3.3.4 rule 5)c) names EC-BOUND-REF-MOD for a
/// leftmost-position/length (fix-queue PB41). Carried by parameter rather than a field — the renderer is
/// re-entrant through <see cref="ReferenceResolver.ReadRefMod(CobolParserCore.RefModPartContext)"/>, and ambient
/// state goes stale across a re-entrant descent (the ExpressionBinder OperandContext discipline).</summary>
internal enum SegmentPosition
{
    /// <summary>A subscript (ISO §8.4.2.3.2 <c>arithmetic-expression-1</c>) — EC-BOUND-SUBSCRIPT.</summary>
    Subscript,

    /// <summary>A reference-modifier leftmost-position or length (§8.4.3.3.3 SR4) — EC-BOUND-REF-MOD.</summary>
    RefMod,
}

public sealed class ReferenceResolver(DataBinder data)
{
    /// <summary>The D18 hook that MATERIALIZES a subscript / ref-mod segment the token renderer cannot render
    /// (fix-queue PB17): given the segment's verbatim source text and its line, it re-parses the text through
    /// <c>SubscriptExpressionFragment</c>, binds it through the ONE <c>ExpressionBinder.BindExpr</c>, synthesizes
    /// the §15.4 temporary via <c>DataBinder.CreateCompilerTemp</c>, registers the store as a statement-scoped
    /// pending PRE-op on <see cref="DataBinder.PendingPreOps"/>, and returns the temp — which this resolver then
    /// renders as an ordinary data-name read.
    /// <para>⛔ IT IS A HOOK, NOT A COLLABORATOR REFERENCE, because the binder dependency is ONE-WAY:
    /// <c>StatementBinder(DataBinder, ReferenceResolver)</c>. StatementBinder installs it in its constructor (the
    /// <c>ConditionRenderer.Calls</c> property-wire precedent). Null on the DATA-division resolution paths
    /// (<c>DataBinder.Constants</c>/<c>Ptr</c> build a throwaway resolver with no procedure binder), where the
    /// old loud posture stands — a VALUE/ADDRESS OF subscript cannot carry a function activation anyway.</para>
    /// <para>⚠ Binding happens HERE, at resolve time, NOT at the drain. That is load-bearing for nesting: the UDF
    /// precedent's own words are "a nested call registers while its consumer's arguments bind, so it precedes the
    /// consumer in the sequence". Deferring the bind to the drain would append an INNER segment's temp AFTER its
    /// consumer's — <c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c> would store in the wrong order.</para></summary>
    /// <remarks>⛔ THE POSITION RIDES THE HOOK (kb/Work PB170/PB172). It used to be <c>Func&lt;string, int,
    /// DataItem?&gt;</c>, so ONE hook served BOTH <see cref="SegmentPosition.Subscript"/> and
    /// <see cref="SegmentPosition.RefMod"/> and the binder hard-coded the index-name window context for both —
    /// but §13.18.38.3 r7's five contexts list "as a subscript" and do NOT list a reference-modification
    /// position, so an index-name in a ref-mod bound was wrongly admitted. One hook, two positions, one context
    /// was the defect; the position is now part of the hook's question.</remarks>
    internal Func<string, SegmentPosition, int, DataItem?>? MaterializeSegment { get; set; }
    /// <summary>The object-property reference BINDER (ISO §8.4.3.9; deep-dive D-P2): when normal
    /// qualification fails and the single qualifier is a class-name (factory form) or a TYPED object
    /// reference (instance form) whose roster carries an accessor for the head word under the PINNED
    /// §11.7.4 GR1a names, synthesize the compiler temp, register the pending op (drained into the
    /// GR1/GR2/GR3 BoundSequence by StatementBinder once the carrying statement's polarity is known), and
    /// return the temp so the statement binds over its Place. Returns null when the shape is NOT a property
    /// reference (the caller keeps its generic unknown-name diagnosis). SR checks here: SR1 (:7376) the
    /// REPOSITORY property-specifier, SR2 (:7378) no universal receiver — both COBOLNET0843; SR3/SR4
    /// (accessor existence) belong to the drain, where the sending/receiving polarity is known.</summary>
    private DataItem? OoTryBindPropertyReference(string name, List<string> qualifiers)
    {
        if (qualifiers.Count != 1 || data.OoClasses is not { } table) return null;
        string recv = qualifiers[0];

        OoClassSymbol? cls = table.Find(recv);
        bool factory = cls is not null;                      // prop OF Class-name → the FACTORY accessors (SR3/SR4 "or in the factory object")
        DataItem? recvItem = null;
        if (cls is null)
        {
            recvItem = data.Symbols.TryResolve(recv, data.ActiveScope, out var recvItems) ? recvItems[0] : null;
            if (recvItem?.Pic is not { Category: PicCategory.ObjectReference } rp) return null;
            if (rp.ObjectClassName is not { } cn)
            {
                // The shape IS a property reference on a universal receiver — SR2 rejects it by name.
                // (Silently null under a probe — the committing resolution reports; kb/Work PB157.)
                if (!_probing)
                    data.Edition.Error("COBOLNET0843",
                        $"the object-property reference '{name}' OF '{recv}': the receiving identifier shall "
                        + "not be a universal object reference (ISO §8.4.3.9.3 SR2)");
                return null;
            }
            cls = table.Find(cn);
            if (cls is null) return null;                    // interface-typed receivers: property prototypes are a later refinement (0899 at the interface)
        }

        string getName = NamingConvention.GetAccessorName(name), setName = NamingConvention.SetAccessorName(name);
        var get = factory ? cls.FindFactoryMethod(getName) : cls.FindMethod(getName);
        var set = factory ? cls.FindFactoryMethod(setName) : cls.FindMethod(setName);
        if (get is null && set is null) return null;         // not a property of the roster → generic diagnosis

        var model = get?.Binding!.Returning ?? set!.Binding!.Formals[0].Item;
        // R30 PURITY (kb/Work PB157): a PROBE gets the property's MODEL item — the accessor's own
        // description, carrying the category the sniff asks about — with NO temp, NO pending op and NO
        // diagnostics. The committing resolution that follows does all three exactly once. (The orphan
        // op a probing registration left behind classified as StoreKind.None and made OoWrapPropertyOps
        // prepend a GET that §8.4.3.9.4 GR2 says a write-only occurrence must not invoke.)
        if (_probing) return model.IsGroup ? null : model;

        if (!data.OoRepositoryProperties.Contains(name))
            data.Edition.Error("COBOLNET0843",
                $"the object-property reference '{name}' OF '{recv}' requires a PROPERTY specifier in the "
                + "REPOSITORY paragraph (ISO §8.4.3.9.3 SR1; §12.3.8)");

        if (model.IsGroup)
        {
            data.Edition.Error(DiagnosticCatalog.OoGroupValuedProperty,
                $"the object-property reference '{name}' OF '{recv}': a GROUP-valued property reference "
                + "(the §8.4.3.9.4 temps over a group description) is a later refinement of the OO wave");
            return null;
        }

        var temp = data.OoCreatePropertyTemp(model, name);
        data.OoPendingPropertyOps.Add(new DataBinder.OoPendingPropertyOp(
            temp,
            recvItem is null ? null : PlaceForItem(recvItem, []),
            cls.CsName, factory, get, set, name, recv));
        return temp;
    }

    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported
    /// here — the DEMANDING form: a name that identifies NO declared item reports <c>COBOLNET1639</c> (kb/Work
    /// R30 — §8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource"; before
    /// this, a typo in any reference position compiled clean and threw at RUN time). Every OTHER null return —
    /// a special register routed by the caller, an unsupported subscript/RENAMES/Tier-C shape — is feature-debt
    /// staging and stays silent, so the caller's loud posture is unchanged. A caller asking "IS this a data
    /// item?" with a legal alternative on no (the SET format sniffs, the INVOKE class-name receiver, the
    /// boolean/float reroutes) reads <see cref="Probe"/> instead.</summary>
    public Place? Resolve(Core.DataReferenceContext dref) => ResolveImpl(dref, report: true);

    /// <summary>The SPECULATIVE form of <see cref="Resolve"/>: identical resolution, but a name that identifies
    /// no item returns null SILENTLY — for type-discriminating probes whose null arm continues to a legal
    /// alternative reading (INVOKE's class-name receiver, SET's format sniffs, EXCEPTION-OBJECT, the
    /// boolean-operand predicate that is documented diagnostic-free). Never use it where a data item is
    /// REQUIRED — that silence is exactly the R30 defect.
    /// <para>⛔ IT RETURNS A SNIFF, NOT A <see cref="Place"/>, AND THE TYPE IS THE FIX (kb/Work PB221). The
    /// documented contract has always been "a probe is a TYPE-DISCRIMINATING sniff whose Place is DISCARDED
    /// after reading its Item" — but it returned a <c>Place</c>, and FOUR callers (CallBinder's BY CONTENT arm,
    /// OoBinder's INVOKE receiver and SET object-reference sender, PtrBinder's SET UP/DOWN first target) simply
    /// committed it into the bound tree. Everything <c>_probing</c> suppresses to keep a probe pure then went
    /// missing from the committed bind: the §8.8.1.1 position screen and the §13.18.38.3 r7 index-name screen
    /// never ran (<c>CALL "S" USING BY CONTENT E(XE)</c> with <c>XE PIC X(4)</c> compiled clean while the
    /// adjacent <c>BY REFERENCE</c> operand drew COBOLNET0844 — one statement, two verdicts), and the D18
    /// materializer's <c>return "1"</c> short-circuit made <c>E(FUNCTION INTEGER(2))</c> bind occurrence ONE — a
    /// WRONG ANSWER, live since PB157 landed the flag. A comment cannot hold that invariant; a return type can.
    /// A caller that has finished discriminating asks <see cref="Resolve"/> for the Place that enters the tree,
    /// which is the pattern <c>SetBinder.BindSetLocale</c> already used.</para></summary>
    public ProbeResult? Probe(Core.DataReferenceContext dref) =>
        ResolveImpl(dref, report: false) is { } p
            ? new ProbeResult(p.Item, p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category)
            : null;

    /// <summary>What a <see cref="Probe"/> may tell its caller: the resolved <see cref="DataItem"/> and the
    /// operand CATEGORY of the reference (§8.4.3.3.4 GR6 for a reference-modified one — <see cref="RefModPlace"/>'s
    /// own reader — else <see cref="DataItem.OperandPic"/>, the D20 one reader, so a GROUP-USAGE group sniffs as
    /// the boolean / national operand it is). Deliberately NOT a <see cref="Place"/>: a probe is unscreened, so a
    /// Place it produced must never reach the bound tree (kb/Work PB221).</summary>
    public readonly record struct ProbeResult(DataItem Item, PicCategory? OperandCategory);

    /// <summary>The drefs this resolver has already DIAGNOSED (an undefined name — <see cref="ReportUnidentified"/> —
    /// or a rejected reference shape: SR3 ref-mod-of-ref-mod, SR1 identifier-1 exclusion): one report per source
    /// reference even when a statement binder resolves the same node more than once, and the fact the receiving
    /// chokepoint asks (<see cref="WasDiagnosed"/>) so that a null it gets back is EITHER already reported OR
    /// reported there — never a silently dropped receiver (kb/Work PB70).</summary>
    private readonly HashSet<Core.DataReferenceContext> _diagnosed = [];

    /// <summary>True when a diagnostic has been emitted for <paramref name="dref"/> by this resolver — the
    /// receiving chokepoint reports an undiagnosed null itself (recognized-not-implemented shape).</summary>
    public bool WasDiagnosed(Core.DataReferenceContext dref) => _diagnosed.Contains(dref);

    /// <summary>The COBOLNET1639 report for a reference no declaration identifies (kb/Work R30): "not defined"
    /// when the bare name exists nowhere; "does not uniquely identify" when it exists but the qualifiers or an
    /// ambiguity defeat resolution (§8.4.2.2 — qualification shall establish uniqueness).</summary>
    private void ReportUnidentified(Core.DataReferenceContext dref, string name, List<string> qualifiers)
    {
        if (!_diagnosed.Add(dref)) return;
        // kb/Work R32 — a name DECLARED in the SCREEN SECTION is not undefined. Since kb/Work PB260 the
        // section itself is REFUSED (COBOLNET1560), so the compile already fails with the true cause; adding
        // the §8.4.2.1 "is not defined" verdict on top would send the user hunting a declaration that is right
        // there. Suppressing it here is cascade control, not leniency. The SAME shape for a
        // declared ALPHABET-NAME referenced in a data position (kb/Work R38 — GnuCOBOL's INSPECT CONVERTING
        // alphabet extension): declared-in-another-namespace is a different verdict than "not defined", and
        // whether the construct is admitted is R38's open adjudication, not this diagnostic's.
        if (data.ScreenNames.Contains(name)
            || data.Alphabets.ContainsKey(name) || data.NationalAlphabets.ContainsKey(name)) return;
        string text = dref.GetText();
        string msg;
        if (!data.Symbols.TryResolve(name, data.ActiveScope, out var candidates))
            msg = $"'{text}' is not defined — no declaration in this source element gives the name '{name}', so "
                + "the statement's reference identifies no resource (ISO §8.4.2.1: \"a statement shall contain a "
                + "reference that uniquely identifies that resource\"). Check the spelling, or declare the item.";
        else if (qualifiers.Count == 0)
            msg = $"'{text}' does not uniquely identify a data item — {candidates.Count} declarations share the "
                + "name and no qualification distinguishes them (ISO §8.4.2.2 — qualification shall establish "
                + "uniqueness).";
        else
        {
            int matches = 0;
            foreach (var c in candidates) if (QualifierChainMatches(c, qualifiers)) matches++;
            msg = matches > 1
                ? $"'{text}' does not uniquely identify a data item — {matches} declarations of '{name}' match "
                  + "the written qualifiers (ISO §8.4.2.2 — qualification shall establish uniqueness; write "
                  + "further qualifiers to single one out)."
                : $"'{text}' does not uniquely identify a data item — '{name}' is declared, but not under the "
                  + $"given qualifier{(qualifiers.Count > 1 ? "s" : "")} ({string.Join(" OF ", qualifiers)}) "
                  + "(ISO §8.4.2.2 — qualification shall establish uniqueness).";
        }
        data.Edition.Error(DiagnosticCatalog.UndefinedReference, msg);
    }

    /// <summary>True while the CURRENT resolution is a <see cref="Probe"/> — the R30 purity flag (kb/Work
    /// PB157). A probe is a TYPE-DISCRIMINATING sniff whose Place is discarded after reading its Item, so in
    /// probe mode the resolver must be SIDE-EFFECT-FREE: no diagnostics beyond none, no OO property temp/op
    /// registration (the orphan op made OoWrapPropertyOps prepend a GET §8.4.3.9.4 GR2 forbids — or reject a
    /// WITH NO GET property), and no D18 subscript materialization (a function-bearing subscript would bind —
    /// and later ACTIVATE — twice). Save/restored, not cleared: a COMMIT resolution can nest inside hooks.</summary>
    private bool _probing;

    private Place? ResolveImpl(Core.DataReferenceContext dref, bool report)
    {
        bool savedProbing = _probing;
        _probing = !report;
        try { return ResolveImplCore(dref, report); }
        finally { _probing = savedProbing; }
    }

    private Place? ResolveImplCore(Core.DataReferenceContext dref, bool report)
    {
        DataReferenceCst r = dref;
        // A special register — LINAGE-COUNTER (I-O control system, ISO §8.4.3.14), LINE-/PAGE-COUNTER (Report
        // Writer control system, ISO §8.4.3.15) — is runtime-sourced, never a storage Place; the binder routes it
        // to BoundLinageCounterRef / BoundReportCounterRef (StatementBinder.ReportWriter.cs). The early return is
        // LOAD-BEARING for the QUALIFIED form (`LINAGE-COUNTER OF file`, `LINE-COUNTER OF report`): there
        // r.BaseName is the FILE-/REPORT-NAME qualifier and would otherwise mis-resolve here as a base data-name.
        if (r.Register != SpecialRegister.None) return null;
        if (r.BaseName is not { } name) return null;

        // The OCCURS DYNAMIC CAPACITY register (ISO §13.18.38 GR15 / §8.5.1.9.1; data-model D9): an implicitly-
        // defined VIEW over the owning dynamic table's current capacity — never a storage item, so it is not in
        // ByName and is resolved HERE (before ordinary name lookup) to a CapacityRegisterPlace whose Read() emits
        // {tablePath}.Capacity. An unqualified, unsubscripted reference is the covered form; a register nested under
        // a fixed OCCURS (whose ancestor levels would need subscripts) or an OF/IN-qualified reference falls through
        // to loud (AccessPath null / normal resolution fails) — a later refinement.
        if (CapacityRegisterFor(dref) is { } capReg) return capReg;

        // The X3.23-1985 DEBUG-ITEM special register / member (VCR Table 7 row 7.17): an IMPLICITLY-defined read-only
        // VIEW over the program-instance __dbgItem — not in ByName, so resolved HERE (before ordinary name lookup) to
        // a DebugRegisterPlace. Registered ONLY when a procedure-subject debugging declarative is active under WITH
        // DEBUGGING MODE (DebugRegisters empty otherwise → this never fires for a non-debug program). Only the plain
        // unqualified/unsubscripted form is covered — a reference-modified/qualified DEBUG-* falls through to loud.
        if (data.DebugRegisters.TryGetValue(name, out var dbg) && dref.dataReferenceSuffix().Length == 0)
            return new DebugRegisterPlace(dbg.Item, dbg.Member);

        var qualifiers = new List<string>();
        Core.SubscriptOrRefModContext? subCtx = null;    // the subscript group (no depth-0 colon)
        Core.SubscriptOrRefModContext? refCtx = null;    // a reference-modification group (start : length)
        Core.RefModPartContext? cleanRef = null;         // the refModPart form (parsed arithmeticExpression : ...)

        // ISO §8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
        // grammar cannot express this (dataReferenceSuffix* and qualification's own (subscriptPart|refModPart)*
        // both admit unlimited ref-mods), so it is counted here. Before this count, `??=` kept the FIRST of each
        // carrier and the DEFAULT-mode form then outranked the SUBSCRIPT-mode one, so `MOVE A (3:4)(2:2)`
        // COMPILED CLEAN and returned A(2:2) — a silent wrong value, not a composition and not a rejection.
        // ⛔ Counts REF-MODS ONLY: a subscript followed by a ref-mod (T(I) (2:3)) is the legal §8.4.3.1.4 GR1
        // a→g order and must stay untouched.
        int refModCount = 0;
        void Classify(Core.SubscriptOrRefModContext s)
        {
            if (HasDepth0Colon(s)) { refModCount++; refCtx ??= s; } else subCtx ??= s;
        }

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                qualifiers.Add(q.cobolWord().Name());
                foreach (var sp in q.subscriptPart()) if (sp.subscriptOrRefMod() is { } qs) Classify(qs);
                refModCount += q.refModPart().Length;
                if (q.refModPart().Length > 0) cleanRef ??= q.refModPart()[0];
            }
            else if (suffix.refModPart() is { } rmp) { refModCount++; cleanRef ??= rmp; }
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s) Classify(s);
        }
        if (refModCount > 1)
        {
            if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                    $"'{name}' carries {refModCount} reference modifications; a reference-modified item cannot itself "
                    + "be reference-modified (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier instead.");
            return null;
        }

        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        // The object-property fallback (§8.4.3.9.2 — `prop OF {class-name | identifier}` is textually a
        // qualified data reference, so it legitimately FAILS normal qualification): the hook synthesizes the
        // GR1–GR3 temp and the rest of THIS method gives the temp the full normal tail (subscript rejection —
        // a temp has no OCCURS — and reference-modification, which SR5/SR6 permit on the property value).
        item ??= OoTryBindPropertyReference(name, qualifiers);
        if (item is null)
        {
            // The NAME resolves to nothing — a typo or a mis-qualification, never a feature gap (kb/Work R30).
            // Every later null in this method is an unsupported-shape staging of a name that DID resolve.
            if (report) ReportUnidentified(dref, name, qualifiers);
            return null;
        }

        List<string> indexExprs = [];
        if (subCtx is not null)
        {
            var (e, isRefMod) = InterpretSubscripts(subCtx);
            if (isRefMod || e is null) return null;   // unsupported subscript form → loud
            indexExprs = e;
        }

        // A level-66 RENAMES alias (ISO §13.18.45): one elementary-alphanumeric view COMPOSED over the spanned
        // leaves — reads concatenate their images, writes distribute slices back. This slice covers STRING-VALUED
        // leaves (X / edited / StoreAsImage); a typed-numeric leaf in the span fails loud (the image codecs for a
        // composed numeric leaf are a later slice). No subscripts: a RENAMES operand cannot have/live under OCCURS.
        if (item.Renames is { } ren)
        {
            if (indexExprs.Count > 0) return null;
            // The no-THRU form is an ALIAS: the 66 has the SAME description as the renamed item (§13.18.45 GR1)
            // — forward to its place outright (numeric stays numeric: NC252A's ADD 3500 TO RENAME-12 over a
            // PIC 9(4); a group forwards as the group). Only the THRU form composes an alphanumeric span (GR2).
            if (ren.Thru is null) return ren.From is { } fwd ? PlaceForItem(fwd, []) : null;
            if (ren.Span.Count == 0) return null;
            var leafPlaces = new List<Place>(ren.Span.Count);
            var widths = new List<int>(ren.Span.Count);
            foreach (var part in ren.Span)
            {
                var leaf = part.Leaf;
                if (part.Occurrence is { } occIdx)
                {
                    // ONE occurrence (or the one-and-only cell) of the leaf, possibly a partial slice of it (kb/Work
                    // PB96): the cell's place, then its ref-mod view when the part does not cover the whole cell.
                    if (PlaceForItem(leaf, leaf.Occurs is null ? [] : [occIdx.ToString()]) is not { } cellRaw) return null;
                    Place cell = cellRaw;
                    bool cellString = data.IsImageBackedEarly(leaf) || cell is RedefViewPlace
                        || leaf.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                            or PicCategory.National or PicCategory.Boolean;
                    if (!cellString)
                    {
                        if (leaf.Pic is not { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false })
                            return null;
                        cell = new NumericImagePlace(cell);
                    }
                    if (part.IsPartial) cell = new RefModPlace(cell, part.Start.ToString(), part.Length.ToString());
                    leafPlaces.Add(cell);
                    widths.Add(part.Length);
                    continue;
                }
                // An OCCURS leaf inside the span contributes EVERY occurrence in order (§13.18.45 — the alias
                // covers the whole fixed-size area; NC252A's RENAME-7 over TABLE-ITEM-2 OCCURS 5).
                int occ = leaf.Occurs ?? 1;
                for (int k = 1; k <= occ; k++)
                {
                    if (PlaceForItem(leaf, leaf.Occurs is null ? [] : [k.ToString()]) is not { } lpRaw) return null;
                    Place lp = lpRaw;
                    bool stringValued = data.IsImageBackedEarly(leaf) || lp is RedefViewPlace
                        || leaf.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                            or PicCategory.National or PicCategory.Boolean;
                    // A typed NUMERIC-DISPLAY leaf participates through its character image (the alias is an
                    // alphanumeric view of the span, §13.18.45 — NC252A's PIC 999 leaves under RENAMES-TEST-1).
                    if (!stringValued)
                    {
                        if (leaf.Pic is not { Category: PicCategory.Numeric, Usage: Usage.Display, IsFloat: false })
                            return null;
                        lp = new NumericImagePlace(lp);
                    }
                    widths.Add(leaf.ImageWidth);   // a whole part: every occurrence, at the leaf's width (kb/Work PB96)
                    leafPlaces.Add(lp);
                }
            }
            return new RenamesPlace(leafPlaces, item, widths);
        }

        if (PlaceForItem(item, indexExprs) is not { } inner) return null;

        if (refCtx is null && cleanRef is null) return inner;
        // ⛔ ISO §8.4.3.3.3 SR1 — WHAT identifier-1 MAY BE, in ONE place (kb/Work PB70). An excluded shape is a
        // bind-time rejection (COBOLNET1647), never the run-time NotImplemented a sending ref-mod used to reach nor
        // the silent drop a receiving one fell into.
        if (RefModExclusion(item) is { } why)
        {
            if (!_probing && _diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{dref.GetText()}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            return null;
        }
        // Reference modification is over a character string. A GROUP (SR1 — "an alphanumeric group item" / "a group
        // item that is neither a strongly-typed group nor a variable-length group") is viewed through its character
        // IMAGE — the unique data item is an elementary alphanumeric item over the group's positions (§8.4.3.3.4
        // GR6); a Tier-B view (RedefViewPlace) is already a character window. A NUMERIC USAGE-DISPLAY item is viewed
        // through its character image likewise (NC224A's TEST-1-DATA(3:) over PIC 9(6)); a numeric item of usage
        // NATIONAL (SR1 admits it) has no national image channel yet — the loud stage.
        if (item.IsGroup)
        {
            // ⛔ A BIT GROUP TAKES BIT POSITIONS, NOT CHARACTER POSITIONS (kb/Work PB173). §8.4.3.3.3 SR1's last
            // sentence admits a bit group as identifier-1 "treated as [an] elementary data item", §13.18.29.4
            // GR1b makes that item PICTURE 1(m) of usage bit, and §8.4.3.3.4 GR5a then says in so many words:
            // "If the usage of identifier-1 is bit, positions used in evaluation are bit positions". So it wraps
            // as its UNPACKED boolean string, never as AsImage()'s ceil(m/8) packed characters — the units the
            // boolean channel already counts (ConditionBinder's widths read OperandPic.Length, which is
            // ExtentBits) and the units RefModPlace.Category already reports.
            // A NATIONAL group keeps GroupImagePlace, and that asymmetry is DERIVED, not an oversight:
            // §13.18.29.4 GR2b's as-if PICTURE N(m) is in NATIONAL positions and DataItem.IsCharacterImage
            // guarantees a national leaf contributes ImageWidth = Length character positions, "never
            // byte-doubled", so its AsImage() IS its national-position string.
            // ⛔ A TIER-B WINDOW NEEDS NO WRAP IN EITHER UNIT, and for the BIT one that is now a derived fact
            // rather than an untested omission (kb/Work PB203 closing PB173's open "RELATED" question): a
            // RedefViewPlace over a bit member reads its BOOLEAN CARRIER — CobolBits.ReadWindow over the class
            // backing — so the RefModPlace built below slices §8.4.3.3.4 GR5a's bit positions structurally,
            // exactly as BitImagePlace's AsBits() does for a struct-stored bit group. Measured: with
            // `01 A PIC X(2). 01 BV REDEFINES A GROUP-USAGE BIT. 05 BV1 PIC 1(8). 05 BV2 PIC 1(8).` holding
            // B"0100100001001001", `BV(1:3)` is B"010" and `MOVE ALL B"0" TO BV(2:3)` zeroes positions 2-4.
            if (inner is not RedefViewPlace)
                inner = item.IsAsIfElementary && item.GroupUsage is GroupUsage.Bit
                    ? new BitImagePlace(inner)
                    : new GroupImagePlace(inner);
        }
        else if (item.Pic?.Category is PicCategory.Numeric)
        {
            if (item.Pic is not { Usage: Usage.Display, IsFloat: false }) return null;
            // P5.7: the bind-time wrap decision reads the COLLECTED early facts (same mid-bind timing the
            // deleted flag had — MarkRefModStoreImage records the SAME item during this statement's bind).
            if (!data.IsImageBackedEarly(item) && inner is not RedefViewPlace) inner = new NumericImagePlace(inner);
        }
        // National/boolean items reference-modify in their OWN character positions (§8.4.3.3 GR1/GR5a — a
        // national position is one UTF-16 char, a bit position one '0'/'1' char, under D-N1/D-B1); alphanumeric,
        // alphabetic, alphanumeric-edited and numeric-edited items are character strings already.
        if (cleanRef is not null)
            return ReadRefMod(cleanRef) is { } cs
                ? new RefModPlace(inner, cs.Start, cs.Length) { AllowZeroLength = cs.AllowZeroLength }
                : null;
        return ReadRefMod(refCtx!) is { } ss
            ? new RefModPlace(inner, ss.Start, ss.Length) { AllowZeroLength = ss.AllowZeroLength }
            : null;
    }

    /// <summary>ISO §8.4.3.3.3 SR1, read as an EXCLUSION test: the reason <paramref name="item"/> may NOT be
    /// identifier-1 of a reference modification, or null when it may. Admitted: a boolean, national, alphanumeric or
    /// alphabetic item (bullets 1–4 — this model's <see cref="PicCategory.Alphanumeric"/> covers alphabetic and
    /// alphanumeric-edited, <see cref="PicCategory.National"/> covers national-edited); a numeric-edited item and a
    /// numeric item of usage DISPLAY or NATIONAL, each "not subordinate to a strongly-typed group item" (bullets 5,
    /// 8); a group that is neither strongly typed nor variable-length (bullet 9; §8.5.1.12 — a variable-length group
    /// has a dynamic-length item or a dynamic-capacity table subordinate to it). Everything else — a numeric item of
    /// BINARY / PACKED / COMP-5 / float usage, an index item, a pointer, an object reference — is excluded. Bit and
    /// national GROUPS are "treated as elementary" by SR1's last sentence, and GROUP-USAGE IS modelled — kb/Work
    /// PB79 landed 2026-08-18 (DEVLOG 1317): <c>AsIfPic</c> / <c>OperandPic</c> / <c>IsAsIfElementary</c> carry the
    /// as-if description, so this predicate's group bullet admits them for the right reason. Their SUBSTRATE then
    /// differs by usage: a bit group wraps as a <c>BitImagePlace</c> over its unpacked boolean string, because
    /// §8.4.3.3.4 GR5a evaluates a bit item's positions as BIT positions (kb/Work PB173), while a national group
    /// keeps <c>GroupImagePlace</c> — §13.18.29.4 GR2b's as-if PICTURE N(m) is already in national positions.</summary>
    internal static string? RefModExclusion(DataItem item)
    {
        if (item.IsGroup)
            return StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item"
                : HasVariableLengthSubordinate(item)
                    ? "a variable-length group item (ISO §8.5.1.12 — a dynamic-length item or a dynamic-capacity table is subordinate to it)"
                : null;
        // A leaf subordinate to a strongly-typed group (its own class/category still decides bullets 1–4).
        bool underStrong = StrongTypeModel.IsStronglyTyped(item);
        return item.Pic switch
        {
            null => "an item without character positions",
            { Category: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } => null,
            { Category: PicCategory.NumericEdited } =>
                underStrong ? "a numeric-edited item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric, Usage: Usage.Index } => "an index data item (class index, ISO §13.18.60)",
            { Category: PicCategory.Numeric, Usage: Usage.Display or Usage.National, IsFloat: false } =>
                underStrong ? "a numeric item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric } p =>
                $"a numeric item of USAGE {p.Usage} (SR1 admits usage DISPLAY or NATIONAL only)",
            { Category: PicCategory.ObjectReference } => "an object reference",
            { Category: PicCategory.Pointer or PicCategory.ProgramPointer } => "a pointer",
            { } p => $"an item of category {p.Category}",
        };
    }

    /// <summary>§8.5.1.12 — a variable-length group has a dynamic-length elementary item or a dynamic-capacity table
    /// as a subordinate (at any depth).</summary>
    internal static bool HasVariableLengthSubordinate(DataItem group)
    {
        foreach (var c in group.Children)
            if (c.IsDynamicLength || c.IsDynamicTable || (c.IsGroup && HasVariableLengthSubordinate(c))) return true;
        return false;
    }

    // ── The ONE reference-modification reader (ISO §8.4.3.3.2) ───────────────────────────────────────────────
    // A ref-mod reaches the binder through TWO source carriers, decided by the lexer at the '(' and frozen there:
    // the DEFAULT-mode PARSED form (`refModPart : (LPAREN | FNARG_LPAREN) refModSpec (RPAREN | FNARG_RPAREN)` —
    // BOTH paren flavours, because a ref-mod written directly after a ZERO-ARGUMENT function name is delimited
    // by the argument-list twins the lexer cannot rule out, §8.4.3.2.3 SR6's catalog precondition; PB48)
    // and the SUBSCRIPT-mode CAPTURED token group (a depth-0 SUB_COLON). Both reduce to the same
    // <see cref="RefModSpec"/> through the same segment renderer, so the rule "how a ref-mod's start and length
    // are read off the source" is written down ONCE. Both overloads are internal because the intrinsic binder
    // reads the SAME two carriers for a ref-modified FUNCTION RESULT (§8.4.3.3.3 SR2, fix-queue PB8) — a second
    // copy of this reader there is exactly the one-rule-two-places defect PB4 was.

    /// <summary>Read the PARSED DEFAULT-mode <c>refModPart</c> form. Null when a start/length expression uses a
    /// form the segment renderer does not handle, so the caller fails loud rather than emitting a wrong slice.</summary>
    internal RefModSpec? ReadRefMod(Core.RefModPartContext rmp)
    {
        var rmExprs = rmp.refModSpec().arithmeticExpression();
        if (rmExprs.Length == 0) return null;
        var startToks = new List<IToken>();
        CollectLeafTokens(rmExprs[0], startToks);
        if (RenderSegment(startToks, SegmentPosition.RefMod) is not { } rmStart) return null;
        string? rmLen = null;
        if (rmExprs.Length > 1)
        {
            var lenToks = new List<IToken>();
            CollectLeafTokens(rmExprs[1], lenToks);
            if (RenderSegment(lenToks, SegmentPosition.RefMod) is not { } l) return null;
            rmLen = l;
        }
        // §7.3.23 / §8.4.3.3.4 item 5c: the ref-mod allows a zero-length result iff REF-MOD-ZERO-LENGTH is ON at
        // this site's source line (the group's compile-time directive fold; OFF everywhere by default).
        return new RefModSpec(rmStart, rmLen, data.RefModZeroLength.IsOnAt(rmp.Start.Line));
    }

    /// <summary>Read the SUBSCRIPT-mode CAPTURED group form. The caller has already established the group IS a
    /// ref-mod (<see cref="HasDepth0Colon"/>); null on an unrenderable segment.</summary>
    internal RefModSpec? ReadRefMod(Core.SubscriptOrRefModContext group)
    {
        var (rm, _) = InterpretSubscripts(group);
        return rm is { Count: > 0 }
            ? new RefModSpec(rm[0], rm.Count > 1 ? rm[1] : null, data.RefModZeroLength.IsOnAt(group.Start.Line))
            : null;
    }

    /// <summary>
    /// Build the typed <see cref="Place"/> for an already-resolved <paramref name="item"/> with its subscript index
    /// expressions, honoring the REDEFINES machinery (COBOLNET_DESIGN §3.4 / §4.2):
    /// <list type="bullet">
    ///   <item>a Tier-B (string-canonical) view → a <see cref="RedefViewPlace"/> (offset,width) window over the
    ///         class's ONE string backing (the canonical too, so exactly one stored member);</item>
    ///   <item>a Tier-A (alias) view → the canonical's ONE stored field, reinterpreted through the view's own
    ///         Pic/scale/profile (the place carries the VIEW's <see cref="DataItem"/>);</item>
    ///   <item>any other item → a plain <see cref="MemberPlace"/>.</item>
    /// </list>
    /// Returns <see langword="null"/> for a form not handled in this slice — a subscripted Tier-B view, a whole-OCCURS
    /// reference, a not-yet-wired Tier-C / Rejected view, or a subscript-count mismatch — so the caller fails loud.
    /// This is the ONE item→<see cref="Place"/> builder: both the syntactic <see cref="Resolve"/> path and the by-item
    /// <see cref="ResolveItem"/> path go through it, so EVERY consumer (verb operands, level-88 / SET conditional
    /// variables, FD record areas) sees identical view resolution.
    /// </summary>
    private Place? PlaceForItem(DataItem item, IReadOnlyList<string> indexExprs)
    {
        // A WHOLE (unsubscripted) OCCURS DYNAMIC table reference has no element access — it would otherwise fold to a
        // MemberPlace wrapping the bare CobolDynTable<T> object, which is uncompilable in any value context. Fail
        // LOUD here (data-model D9); FUNCTION LENGTH and other whole-table operations route to a dedicated
        // DynWholeTablePlace in a later increment. A SUBSCRIPTED dynamic element (indexExprs non-empty) is inc 3's
        // access path and is NOT caught by this guard.
        if (item.IsDynamicTable && indexExprs.Count == 0) return null;
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class. Fail loud if the parent path is unavailable (e.g. it is itself within an
            // OCCURS), rather than emit an unqualified reference that does not exist in scope.
            if (BuildBackingPath(sc) is not { } backing) return null;
            // A SUBSCRIPTED view: each OCCURS level on the item's path WITHIN the class displaces the window by
            // (occurrence − 1) × that level's per-occurrence width — the redefined table lays its occurrences
            // end-to-end in the ONE backing (ISO §13.18.44). ClassOffset is the occurrence-1 position; subscripts
            // map to the in-class OCCURS levels outer→inner, exactly as in AccessPath.
            var occursLevels = new List<DataItem>();
            for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, sc); n = n.Parent)
                if (n.Occurs is not null) occursLevels.Add(n);
            occursLevels.Reverse();
            if (occursLevels.Count != indexExprs.Count) return null;   // wrong subscript count → loud
            string offset = item.ClassOffset.ToString();
            // The BIT twin of the same displacement, for a USAGE BIT member (kb/Work PB203): a bit item's
            // occurrences lie at successive BIT positions (§8.5.1.6.3's "next bit position in storage"; the same
            // stride GroupImageCodec.EmitRunMemberFromBits distributes a run's occurrences at), so the per-level
            // stride is its bit extent — `PIC 1(4) USAGE BIT OCCURS 6` strides 4 bits, not the 1 byte its
            // ImageWidth ceiling reports.
            string bitTerms = "";
            for (int k = 0; k < occursLevels.Count; k++)
            {
                offset += $" + ({indexExprs[k]} - 1) * {occursLevels[k].ImageWidth}";
                bitTerms += $" + ({indexExprs[k]} - 1) * {BitLayout.WidthBits(occursLevels[k])}";
            }
            // A BASED class's window is displaced by the data-address pointer's runtime offset (ISO §13.18.5
            // — the view addresses wherever the pointer currently points; Phase-4b increment 2). The backing
            // property renders FIRST in both Read and Write, so the Deref null/bounds traps (GR3/GR4) fire
            // before the null-lenient OffsetOf.
            string? based = null;
            if (sc.BasedPointerField is { } addr)
            {
                based = $"CobolPtr.OffsetOf({addr})";
                offset = $"{based} + {offset}";
            }
            // (whole-group image analysis moved OUT of resolve to the post-bind UsageCollectionPass, PHASE-05 Step 5)
            // A class-tier GROUP holding an occurs-depending table is an ODO operand exactly like a struct group
            // (kb/Work PB80: a BASED record — string-canonical — sent its MAXIMUM image; §13.18.38.4 GR8 does not
            // care how the group is stored). ONE wrap rule for both storage shapes.
            return WrapIfOdoGroup(RedefViewPlace.For(backing, item, offset, based, bitTerms), item);
        }
        // A Tier-A view forwards to the canonical (a numeric view reinterprets the shared unscaled value via its own
        // scale, for free). A not-yet-wired (Tier-C) / Rejected view is loud.
        if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
            return null;
        DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
            ? ac.Canonical : item;
        // A subscripted element whose access path crosses an OCCURS DYNAMIC level (data-model D9): the sending and
        // receiving accessors differ (RefSending vs RefReceiving — the latter grows-and-seeds), so build BOTH paths
        // and return a direction-carrying DynTablePlace. (A dynamic element is never an ODO subject, and a group
        // containing a dynamic table is not image-capable, so the ODO-wrap / whole-group paths below do not apply.)
        for (DataItem? n = accessItem; n is not null; n = n.Parent)
            if (n.IsDynamicTable)
            {
                if (BuildAccessPath(accessItem, indexExprs) is not { } dynPath) return null;
                return new DynTablePlace(dynPath, item);
            }
        // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
        if (BuildAccessPath(accessItem, indexExprs) is not { } path) return null;
        // (Resolving a group no longer mutates WholeGroupReferenced — the "which groups are whole-image operands"
        // analysis is the post-bind UsageCollectionPass, which walks the BOUND tree and collects ONLY true
        // whole-group operands, not every RESOLVED group. PHASE-05 Step 5, §14.9.25.4 MOVE GR4.)
        return WrapIfOdoGroup(new MemberPlace(path, item), item);
    }

    /// <summary>A group whose subtree contains an occurs-depending table is an ODO operand (ISO §13.18.38 GR8): wrap
    /// it so the sending slice / receiving direction-split applies — whatever the group's storage shape (a record
    /// struct member, or a Tier-B / BASED class-tier window; kb/Work PB80). data-name-1 is resolved post-build,
    /// declared anywhere outside the table (SR20), and read at the operation site via CobolTable.Occ
    /// (storage-form-agnostic). Any other place passes through unchanged.</summary>
    private Place WrapIfOdoGroup(Place place, DataItem item) =>
        item.IsGroup && OdoModel.TableUnder(item) is { OccursSpec.Depending: { } dep } table
            && ResolveItem(dep) is { } depPlace
            ? OdoModel.WrapGroup(place, depPlace, item, table)
            : place;

    /// <summary>A <see cref="Place"/> for an already-resolved item with no subscripts (e.g. a level-88's conditional
    /// variable, a SET condition's variable, or an FD record area) — view-aware via <see cref="PlaceForItem"/>, so a
    /// REDEFINES view resolves to its window / canonical exactly as a verb operand does. <see langword="null"/> if the
    /// item is within an OCCURS table (a subscripted reference is then required) or is an unhandled view form.</summary>
    public Place? ResolveItem(DataItem item) => PlaceForItem(item, []);

    /// <summary>The place for an already-resolved <paramref name="item"/> using the SUBSCRIPTS of
    /// <paramref name="dref"/> — the condition-name-with-subscripts form (ISO §8.4.2.3 Format 2): a level-88
    /// reference's subscripts identify the occurrence of its CONDITIONAL VARIABLE. Null for an unhandled subscript
    /// form (the caller fails loud).</summary>
    public Place? ResolveForItem(Core.DataReferenceContext dref, DataItem item)
    {
        Core.SubscriptOrRefModContext? subCtx = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s && !HasDepth0Colon(s)) subCtx ??= s;
            else if (suffix.qualification() is { } q)
                foreach (var sp in q.subscriptPart())
                    if (sp.subscriptOrRefMod() is { } qs && !HasDepth0Colon(qs)) subCtx ??= qs;
        }
        List<string> indexExprs = [];
        if (subCtx is not null)
        {
            var (e, isRefMod) = InterpretSubscripts(subCtx);
            if (isRefMod || e is null) return null;
            indexExprs = e;
        }
        return PlaceForItem(item, indexExprs);
    }

    // ── Intrinsic-argument entries (ISO §15.3; consumed by StatementBinder.Intrinsics.cs) ─────────────────
    // The function-argument mini-parser resolves identifiers from flat SUBSCRIPT-mode tokens, where no
    // dataReference parse context exists — these thin internal entries expose the SAME private resolution
    // (ResolveUnqualified/ResolveQualified → PlaceForItem → RenderSegment) so argument references see identical
    // view/qualification/subscript semantics as every verb operand (singular-pattern rule).

    /// <summary>Resolve a (possibly OF/IN-qualified) data-name to its <see cref="DataItem"/>, or null. Used by
    /// the table(ALL) expansion (§15.3) to read the OCCURS counts before building per-occurrence places.</summary>
    internal DataItem? FindItem(string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count > 0 ? ResolveQualified(name, [.. qualifiers]) : ResolveUnqualified(name);

    /// <summary>Build the <see cref="Place"/> for a by-name reference with ALREADY-RENDERED C# index expressions
    /// (one per OCCURS level, outermost first). A level-66 RENAMES alias stays null (loud) — its composition
    /// lives only on the parse-context path.</summary>
    internal Place? ResolveByName(string name, IReadOnlyList<string> qualifiers, IReadOnlyList<string> indexExprs) =>
        FindItem(name, qualifiers) is { Renames: null } item ? PlaceForItem(item, indexExprs) : null;

    /// <summary>Render one subscript token segment to a C# index expression (the private
    /// <see cref="RenderSegment"/>), or null when the segment uses an unhandled form (caller fails loud).</summary>
    internal string? RenderIndexSegment(List<IToken> tokens) =>
        RenderSegment(tokens, SegmentPosition.Subscript);

    /// <summary>Resolve an <c>ADDRESS OF</c> operand (ISO §8.4.3.11) to its item plus the OCCURS displacement
    /// of its subscripts — <c>(idx − 1) × width [+ …]</c> character positions within the item's storage class,
    /// or a null displacement for an unsubscripted (possibly OF/IN-qualified) reference. The address of
    /// occurrence k is the class cell displaced by the SAME in-class occurrence arithmetic the Tier-B view
    /// window uses (<see cref="PlaceForItem"/> — a table lays its occurrences end-to-end in the ONE cell
    /// image), so the two share one formula; the displacement string is the D10 transitional index carrier
    /// (a rendered expression, like <see cref="FixedTableSegment.OneBasedIndex"/>). Null overall = an
    /// unresolvable name, a subscript-count mismatch, or a reference-modified operand (ref-mod addresses a
    /// character SPAN, not a data item — a named residue) — the caller reports loud, never a wrong address.</summary>
    internal (DataItem Item, string? OccursDisplacement)? ResolveForAddressOf(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        if (r.Register != SpecialRegister.None || r.BaseName is not { } name) return null;
        var qualifiers = new List<string>();
        Core.SubscriptOrRefModContext? subCtx = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                qualifiers.Add(q.cobolWord().Name());
                if (q.refModPart().Length > 0) return null;   // ref-mod → loud (a span, not an item)
                foreach (var sp in q.subscriptPart())
                    if (sp.subscriptOrRefMod() is { } qs)
                    {
                        if (HasDepth0Colon(qs)) return null;
                        subCtx ??= qs;
                    }
            }
            else if (suffix.refModPart() is not null) return null;
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s)
            {
                if (HasDepth0Colon(s)) return null;
                subCtx ??= s;
            }
        }
        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        if (item is null) return null;
        if (subCtx is null) return (item, null);
        var (exprs, isRefMod) = InterpretSubscripts(subCtx);
        if (isRefMod || exprs is null) return null;
        // The in-class OCCURS levels outer→inner — the PlaceForItem Tier-B walk (same layout, same formula).
        var occursLevels = new List<DataItem>();
        for (DataItem? n = item; n is not null && ReferenceEquals(n.Class, item.Class); n = n.Parent)
            if (n.Occurs is not null) occursLevels.Add(n);
        occursLevels.Reverse();
        if (occursLevels.Count != exprs.Count) return null;   // wrong subscript count → loud
        string disp = string.Join(" + ", occursLevels.Select((lv, k) => $"({exprs[k]} - 1) * {lv.ImageWidth}"));
        return (item, disp);
    }

    /// <summary>The STRUCTURAL access path to a Tier-B/Tier-C class's single stored backing field (the
    /// <see cref="RedefViewPlace"/> twin of the old string <c>BackingPath</c>). The backing is emitted in the
    /// canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable (the canonical is within an OCCURS table).</summary>
    private static AccessPath? BuildBackingPath(RedefinesClass cls) =>
        cls.Canonical.Parent is not { } parent
            ? new AccessPath([new RootFieldSegment(cls.BackingCsName)])
            : BuildAccessPath(parent, []) is { } parentPath ? parentPath.Add(new MemberSegment(cls.BackingCsName)) : null;

    // ── Name resolution ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to
    /// disambiguate) — through the ONE scope-aware <see cref="Model.SymbolTable"/> (P7 Step 10a, the DEVLOG-773
    /// pickup): the §11.7 GR5 method-overlay-first precedence (a method-local name shadows object data; sibling
    /// methods' names are invisible) lives in <c>TryResolve</c>, no longer duplicated here.</summary>
    private DataItem? ResolveUnqualified(string name)
    {
        if (!data.Symbols.TryResolve(name, data.ActiveScope, out var list)) return null;
        if (list.Count > 1)
        {
            // §8.4.2.2.1 (kb/Work R33): "Qualification of a user-defined name is required unless … 1) No
            // other name has the identical spelling." TryResolve returns ONE namespace tier — the §8.4.6.2.1
            // rule-3a method overlay OR the unit map, never a mix — so a plural list is genuine same-tier
            // ambiguity, not legal scope shadowing. Measured before enforcing: ZERO of 762 corpus+NIST
            // programs hit this (the note's blast-radius sweep). Strict: null → ReportUnidentified's
            // N-declarations arm (dead until now — R30 built it, this makes it reachable).
            // --permissive: the traditional first-declared match, warned (the DA6/R29 disposition shape).
            if (!data.Edition.Permissive) return null;
            if (_probing) return list[0];   // R30 purity: the committing resolution warns (kb/Work PB157)
            data.Edition.Warning(DiagnosticCatalog.UndefinedReference,
                $"'{name}' is declared {list.Count} times and referenced without qualification — "
                + "ISO §8.4.2.2.1 requires qualification when spellings collide; --permissive resolves to "
                + "the first declaration");
        }
        return list[0];
    }

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> (ISO §8.4.2.2) by CANDIDATE-SET matching:
    /// every in-scope declaration of <paramref name="name"/> whose ancestor chain carries each written
    /// qualifier in order (inner → outer, §8.4.2.2.2 Format 1 — qualifiers need not be consecutive levels),
    /// the OUTERMOST qualifier optionally the owning FILE's name (§8.4.2.2 — the FD/SD is the highest
    /// permissible qualifier; SQ207M's <c>WRITE PRINT-REC IN PRINT-FILE</c>). Exactly ONE survivor resolves;
    /// zero or several is a resolution failure the caller reports (kb/Work R30).
    /// <para>⛔ THE UNIQUENESS DEMANDED IS OF THE MATCH, NEVER OF A QUALIFIER NAME IN ISOLATION (kb/Work
    /// R31): §8.4.2.2.1 — "Identical user-defined names may be specified in a source unit; however,
    /// uniqueness shall be established through qualification". The prior walk resolved the outermost
    /// qualifier as a unique unqualified name FIRST, so legal <c>Z IN X</c> — two Xs, exactly one holding a
    /// Z — was rejected (the differential's syn_definition:931 flip, GnuCOBOL's own "Unique reference with
    /// ambiguous qualifiers" case). Level-66 RENAMES aliases are registered names with the owning record as
    /// <see cref="DataItem.Parent"/>, so <c>HARRY OF A-GLOB</c> (NC209A) matches through the same chain.</para>
    /// </summary>
    private DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        List<DataItem> survivors = [];
        if (data.Symbols.TryResolve(name, data.ActiveScope, out var candidates))
            foreach (var cand in candidates)
                if (QualifierChainMatches(cand, qualifiers) && !survivors.Contains(cand))
                    survivors.Add(cand);
        return survivors.Count == 1 ? survivors[0] : null;
    }

    /// <summary>True when every qualifier names strictly-superordinate context of <paramref name="cand"/>,
    /// consumed inner → outer; when the data ancestors are exhausted, the OUTERMOST qualifier may instead
    /// name the file whose FD/SD owns the candidate's record.</summary>
    private bool QualifierChainMatches(DataItem cand, List<string> qualifiers)
    {
        DataItem? anc = cand.Parent;
        for (int qi = 0; qi < qualifiers.Count; qi++)
        {
            string q = qualifiers[qi];
            while (anc is not null && !string.Equals(anc.CobolName, q, StringComparison.OrdinalIgnoreCase))
                anc = anc.Parent;
            if (anc is not null) { anc = anc.Parent; continue; }
            // Data ancestors exhausted: only the OUTERMOST remaining qualifier may be the file name.
            if (qi != qualifiers.Count - 1 || !data.FilesByName.TryGetValue(q, out var file)) return false;
            DataItem root = cand;
            while (root.Parent is { } p) root = p;
            return file.Records.Contains(root);
        }
        return true;
    }

    // ── Access-path construction (subscripts attach to OCCURS levels, outer→inner) ───────────────────────

    /// <summary>
    /// The C# member-access path for an item: a static field at the root, else <c>Parent.Child</c> chained, with
    /// each <paramref name="indexExprs"/> entry inserted as <c>[expr - 1]</c> at its OCCURS level (outermost first).
    /// Returns <see langword="null"/> if the subscript count does not match the table's OCCURS dimension.
    /// </summary>
    /// <summary>The C# field path to a WHOLE table's field (the bare <c>.CsName</c> chain, NO subscript wrap) — for
    /// the OCCURS DYNAMIC CAPACITY-register view (<c>{path}.Capacity</c>), and (later increments) FUNCTION LENGTH,
    /// the SEARCH bound, and INITIALIZE of a dynamic table. Returns <see langword="null"/> when an ancestor is
    /// ITSELF a table (fixed or dynamic): a subscript would be required, so a whole-table reference is ambiguous and
    /// the caller fails loud (ISO §13.18.38; data-model D9).</summary>
    internal string? TablePath(DataItem table)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = table; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        for (int i = 0; i < chain.Count - 1; i++)   // any table STRICTLY above → ambiguous whole-table reference
            if (chain[i].IsTable) return null;
        return string.Join(".", chain.Select(n => n.CsName));
    }

    /// <summary>The <see cref="CapacityRegisterPlace"/> for an unqualified, unsubscripted reference to an OCCURS
    /// DYNAMIC CAPACITY register, or null — a PURE check over <see cref="DataBinder.CapacityRegisters"/> with NO side
    /// effects (unlike the full <see cref="Resolve"/> pipeline, which routes an unresolved qualified name through the
    /// property-reference hook and enqueues a pending op). The SET Format 14 reroute peek uses this so it never mints
    /// a spurious property temp/op for a non-capacity target (data-model D9; OCCURS DYNAMIC review #7).</summary>
    internal CapacityRegisterPlace? CapacityRegisterFor(Core.DataReferenceContext dref)
    {
        DataReferenceCst r = dref;
        return r.HasNoSuffix && r.BaseName is { } name
            && data.CapacityRegisters.TryGetValue(name, out var capTable)
            && capTable.OccursSpec?.CapacityRegister is { } capReg
            && BuildTablePath(capTable) is { } capPath
            ? new CapacityRegisterPlace(capPath, capReg) : null;
    }

    /// <summary>The STRUCTURAL access path for an item — the <see cref="MemberPlace"/>/<see cref="DynTablePlace"/>
    /// twin of the string <see cref="AccessPath"/>: each chain node is a field segment, each OCCURS level a fixed or
    /// dynamic table segment carrying its (D10 transitional) index string. Null on a subscript-count mismatch.</summary>
    private static AccessPath? BuildAccessPath(DataItem item, IReadOnlyList<string> indexExprs)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        if (chain.Count(n => n.IsTable) != indexExprs.Count) return null;   // wrong number of subscripts
        var segs = new List<AccessSegment>();
        int si = 0;
        bool first = true;
        foreach (var seg in chain)
        {
            segs.Add(first ? new RootFieldSegment(seg.CsName) : new MemberSegment(seg.CsName));
            first = false;
            if (seg.Occurs is not null) segs.Add(new FixedTableSegment(indexExprs[si++]));       // fixed OCCURS → CobolTable.At
            else if (seg.IsDynamicTable) segs.Add(new DynTableSegment(indexExprs[si++]));         // dynamic OCCURS → RefSending/RefReceiving
        }
        return new AccessPath(segs);
    }

    /// <summary>The STRUCTURAL whole-table path (no subscript wraps) — the <see cref="CapacityRegisterPlace"/> twin of
    /// the string <see cref="TablePath"/> (also the base of a whole-dynamic-table INITIALIZE element path). Null when
    /// an ancestor is itself a table (an ambiguous whole-table reference).</summary>
    internal static AccessPath? BuildTablePath(DataItem table) => BuildTablePath(table, []);

    /// <summary>The STRUCTURAL whole-table path to a table that may itself lie under OTHER tables — one index
    /// expression per enclosing table level, outermost first (the D10 transitional string carrier): the
    /// <see cref="CapacityRegisterPlace"/> of a NESTED dynamic-capacity table for a table(ALL) enumeration (ISO §15.3
    /// — each outer occurrence has its own capacity; kb/Work PB62). Null when fewer indices are supplied than there
    /// are enclosing tables — the zero-index form is exactly the whole-table ambiguity the one-argument overload
    /// reports.</summary>
    internal static AccessPath? BuildTablePath(DataItem table, IReadOnlyList<string> outerIndexExprs)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = table; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();
        var segs = new List<AccessSegment>();
        bool first = true;
        int oi = 0;
        foreach (var n in chain)
        {
            segs.Add(first ? new RootFieldSegment(n.CsName) : new MemberSegment(n.CsName));
            first = false;
            if (ReferenceEquals(n, table)) break;
            if (!n.IsTable) continue;
            if (oi >= outerIndexExprs.Count) return null;   // an enclosing table with no index — ambiguous
            segs.Add(n.Occurs is not null
                ? new FixedTableSegment(outerIndexExprs[oi++])
                : new DynTableSegment(outerIndexExprs[oi++]));
        }
        return new AccessPath(segs);
    }

    private static string? AccessPath(DataItem item, IReadOnlyList<string> indexExprs,
        AccessDir dir = AccessDir.Sending)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();   // root-first

        // Count ANY table level (fixed OR dynamic) — a dynamic table IS an OCCURS dimension though its Occurs is
        // null (DataItem.IsTable guidance: use IsTable at subscript-arity sites).
        int occursLevels = chain.Count(n => n.IsTable);
        if (occursLevels != indexExprs.Count) return null;   // wrong number of subscripts

        string path = "";
        int si = 0;
        foreach (var seg in chain)
        {
            path += path.Length == 0 ? seg.CsName : "." + seg.CsName;
            // A FIXED OCCURS routes through the ref-returning CobolTable.At (ISO §8.4.2.3.4 GR2): an out-of-range
            // occurrence continues benignly with subscript checking off (COBOL-85 semantics — conditions and FAIL
            // paths legally evaluate one-past-the-end references), instead of a raw CLR IndexOutOfRangeException.
            if (seg.Occurs is not null) path = $"CobolTable.At({path}, {indexExprs[si++]})";
            // A DYNAMIC OCCURS (§8.5.1.9.2/.9.3, D9) has direction-specific accessors: RefSending on a read (benign
            // scratch on OOB), RefReceiving on a write (grows-and-seeds past the current capacity). The direction is
            // fixed at build time and carried by the DynTablePlace's two paths.
            else if (seg.IsDynamicTable)
                path = $"{path}.{(dir == AccessDir.Sending ? "RefSending" : "RefReceiving")}({indexExprs[si++]})";
        }
        return path;
    }

    // ── Subscript interpretation (the flat SUBSCRIPT-mode token stream) ──────────────────────────────────

    /// <summary>
    /// Interpret the flat subscript/ref-mod token sequence. Returns (index expressions, isRefMod): a depth-0
    /// <c>SUB_COLON</c> marks reference modification (handled in a later slice, so the C# list is null). Otherwise
    /// each comma- or multi-space-separated segment is rendered to a C# <c>long</c> index expression; a segment that
    /// cannot be rendered yields a null list (→ the caller fails loud).
    /// </summary>
    /// <summary>True if the flat token stream has a depth-0 <c>SUB_COLON</c> — i.e. it is a reference modification
    /// (<c>start:length</c>) rather than a subscript list. Internal because the keyword-omitted FUNCTION path
    /// asks the SAME question of a captured group (fix-queue PB8): with the FUNCTION keyword omitted,
    /// <c>CURRENT-DATE (1:8)</c> captures its ref-mod in SUBSCRIPT mode exactly as a data reference would, and
    /// only this test separates it from an argument list.</summary>
    internal static bool HasDepth0Colon(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) return true;
        }
        return false;
    }

    private (List<string>? Exprs, bool IsRefMod) InterpretSubscripts(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        int colonIdx = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) { colonIdx = i; break; }
        }
        if (colonIdx >= 0)   // reference modification: start [: length]
        {
            if (RenderSegment(tokens.GetRange(0, colonIdx), SegmentPosition.RefMod) is not { } start)
                return (null, true);
            var result = new List<string> { start };
            var lengthTokens = tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1);
            if (lengthTokens.Any(t => t.Type != Core.SUB_WS))
            {
                if (RenderSegment(lengthTokens, SegmentPosition.RefMod) is not { } len) return (null, true);
                result.Add(len);
            }
            return (result, true);
        }

        var exprs = new List<string>();
        foreach (var seg in SplitSubscriptTokens(tokens,
                     name => ResolveUnqualified(name) is { IsTable: false }))   // kb/Work PB136 — declaration-informed '(' splitting
        {
            if (RenderSegment(seg, SegmentPosition.Subscript) is not { } e) return (null, false);
            exprs.Add(e);
        }
        return (exprs, false);
    }

    // Internal (not private): the intrinsic-argument binder (StatementBinder.Intrinsics.cs) flattens and splits
    // the SAME SUBSCRIPT-mode token streams for FUNCTION argument lists (ISO §15.3) — one splitter, not two.
    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split a flat token list into subscript segments on depth-0 comma / multi-space boundaries (a faithful
    /// reduction of the legacy <c>ExpressionBinder.SplitSubscriptTokens</c>: a single space inside a relative
    /// subscript such as <c>I + 1</c> does not split; a separator space before a new operand does).</summary>
    internal static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens, Func<string, bool>? parenSplitsAfterName = null)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;

        // kb/Work PB136: does a depth-0 '(' BEGIN A NEW SEGMENT? After ')' or a literal, always — neither can
        // take a subscript, so the paren can only open a parenthesized-expression subscript (the spec's own
        // NOTE 2 form, `DOG (XCOUNTER (- YCOUNTER))`, was rejected with a wrong-count error). After an
        // IDENTIFIER it is AMBIGUOUS with the identifier's own subscript (`DOG (BAKER (I) 3)`), so the split
        // is DECLARATION-INFORMED: a name that carries no OCCURS cannot be subscripted, so its '(' starts a
        // new segment; with no lookup (the intrinsic-argument caller) the old join stands.
        bool LParenStartsNew()
        {
            var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
            if (lastNonWs is null) return false;
            return lastNonWs.Type switch
            {
                Core.SUB_RPAREN or Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.SUB_DECIMALLIT
                    or Core.SIGNED_DECIMALLIT or Core.SUB_STRINGLIT => true,
                // The predicate answers true ONLY for a name that RESOLVES to a non-table data item: an
                // unresolved name may be a function reference (`FUNCTION INTEGER (X)` — the first cut split
                // a function from its own argument list and six goldens went red on "0 given"), and a TABLE
                // name owns its paren as a subscript. Unknown → no split → the D18 loud names the operand.
                Core.SUB_IDENTIFIER when parenSplitsAfterName is not null
                    && !lastNonWs.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)
                    => parenSplitsAfterName(lastNonWs.Text),
                _ => false,
            };
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == Core.SUB_LPAREN)
            {
                if (depth == 0 && current.Count > 0 && LParenStartsNew())
                {
                    segments.Add(current);
                    current = [];
                }
                depth++; current.Add(t); continue;
            }
            if (t.Type == Core.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == Core.SUB_COMMA || t.Type == Core.SUB_SEMICOLON))
            {
                if (current.Count > 0) { segments.Add(current); current = []; }
                continue;
            }
            if (depth == 0 && t.Type == Core.SUB_WS)
            {
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == Core.SUB_WS) next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
                    // A trailing operator OR a trailing OF/IN continues the SAME segment (`I + 1` relative
                    // subscripts; `SUB1 OF GRP` qualified subscripts, ISO §8.4.2.3.2) — and a pending OF/IN also
                    // continues into its qualifier identifier. The FUNCTION keyword (a plain SUB_IDENTIFIER in
                    // SUBSCRIPT mode) also continues: the following name belongs to a nested intrinsic call in a
                    // FUNCTION argument list — `SQRT(FUNCTION SQRT(F))` is ONE argument (ISO §15.3; the legacy
                    // splitter's endsWithFunction rule, dropped in the original subscript-only reduction).
                    bool continues = lastNonWs is not null &&
                        (lastNonWs.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR or Core.SUB_SLASH
                            or Core.SUB_POWER or Core.SUB_OF or Core.SUB_IN
                         || (lastNonWs.Type == Core.SUB_IDENTIFIER
                             && lastNonWs.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)));
                    int nextType = tokens[next].Type;
                    // The new-segment starters: every token that can BEGIN an operand — identifiers, all four
                    // numeric-literal shapes, string literals (intrinsic arguments may be space-separated,
                    // ISO §15's general formats), and the ALL subscript word (§15.3 table(ALL) arguments).
                    if (!continues && (nextType is Core.SIGNED_INTEGERLIT or Core.SIGNED_DECIMALLIT
                            or Core.SUB_IDENTIFIER or Core.SUB_INTEGERLIT or Core.SUB_DECIMALLIT
                            or Core.SUB_STRINGLIT or Core.SUB_ALL
                        // kb/Work PB136 — the spaced NOTE 2 form: a '(' after this WS opens a NEW subscript
                        // under the same declaration-informed rule as the unspaced arm above.
                        || (nextType == Core.SUB_LPAREN && LParenStartsNew())))
                    {
                        segments.Add(current);
                        current = [];
                        i = next - 1;   // skip consumed WS
                        continue;
                    }
                    // A following OF/IN never splits — `name OF qualifier` stays one segment.
                }
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Render one subscript / ref-mod segment to a C# <c>long</c> position expression, or
    /// <see langword="null"/> if it uses a form neither the token renderer nor the D18 materialization route
    /// handles (so the caller fails loud). The renderer proper is a fast path for the shapes that map to C# text
    /// one token at a time — integer literals, data-name / index-name references, <c>+ - * /</c> and parentheses,
    /// i.e. the relative-subscript and simple-index forms. <b>EVERYTHING ELSE routes through
    /// <see cref="MaterializeViaFragment"/></b>, which re-parses and binds it properly; the renderer is an
    /// optimization over that route, never the arbiter of what is legal in the position (fix-queue PB42).
    /// <para>⚠ THAT INVARIANT WAS A CLAIM, NOT A FACT, until kb/Work PB170. A name the renderer CAN render never
    /// reached the binder, so nothing ever applied §8.8.1.1 to it and <c>T(XE)</c> with <c>XE PIC X(4)</c>
    /// compiled clean under STRICT — the renderer WAS deciding the position's legality, by omission. It holds now
    /// because <see cref="ScreenPositionOperandClass"/> asks the same classifier the binder would have asked, so
    /// the fast path and the D18 route reach the same verdict rather than two different ones.</para>
    /// <para>⛔ THE FAST PATH'S SCREENS ARE DEFERRED TO ITS COMMIT POINT (kb/Work PB220). They used to fire
    /// INSIDE the token loop, and the loop's five exits to D18 are ORDER-DEPENDENT — a later token with no case
    /// arm (<c>**</c>), an unresolvable name, or a scaled operand in a compound abandons the whole fast path
    /// AFTER an earlier name was already screened, and the D18 route then screens the same operand again through
    /// the expression binder. Measured: <c>MOVE E(XE ** 2) TO R</c> with <c>XE PIC X(4)</c> emitted COBOLNET0844
    /// TWICE (and Error+Warning under <c>--permissive</c>, which is worse than either lane alone). A
    /// <c>_diagnosed</c>-style dedupe cannot fix it — the second diagnostic comes from a different class over a
    /// different bound operand — so the screens are QUEUED here and flushed only when this method actually
    /// returns a rendered segment. Every D18 reroute discards the queue, which makes the deduplication a
    /// property of the control flow rather than of a set, and makes the NEXT late exit automatic.</para></summary>
    private string? RenderSegment(List<IToken> tokens, SegmentPosition position)
    {
        var sb = new System.Text.StringBuilder();
        List<PendingScreen>? pending = null;
        // ⛔ A COMPOUND segment (one carrying an arithmetic operator) whose operands include a SCALED item cannot
        // be rendered operand-by-operand, because §8.4.2.3.4 GR1b tests the integrality of THE RESULT of the whole
        // expression, not of each operand: `W-E(W-P + W-Q)` with W-P = W-Q = 1.5 has the integral result 3.0 and
        // is a legal subscript, while de-scaling each operand first yields 1 + 1 = 2 AND raises the condition
        // twice on source that never violated it. Such a segment routes to the D18 materializer, which evaluates
        // the expression at full precision into the §15.4 temp and applies the integrality rule exactly once — to
        // the result, where the standard applies it. A SINGLE scaled operand needs no such detour: it IS the
        // result, so the direct read below is equivalent and cheaper.
        bool compound = tokens.Any(t => t.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR
            or Core.SUB_SLASH or Core.SUB_POWER or Core.PLUS or Core.MINUS or Core.STAR or Core.SLASH);
        // kb/Work PB136: a QUOTIENT-bearing segment routes to D18 UNCONDITIONALLY — the token splice would be
        // C# integer division over long reads, truncating where §8.4.2.3.4 GR1b evaluates the exact result of
        // the whole expression and requires EC-BOUND-SUBSCRIPT on a non-integer (`E((W-A + W-B) / 2)` with the
        // sum 7 silently selected occurrence 3). The scaled-operand routing above (PB41) caught only segments
        // whose OPERANDS are scaled; an all-integer quotient is exactly the case it could not see.
        if (tokens.Any(t => t.Type is Core.SUB_SLASH or Core.SLASH))
            return MaterializeViaFragment(tokens, position);
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            switch (t.Type)
            {
                case Core.SUB_WS: sb.Append(' '); break;
                case Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT or Core.INTEGERLIT: sb.Append(t.Text); break;
                case Core.SUB_PLUS or Core.PLUS: sb.Append(" + "); break;
                case Core.SUB_MINUS or Core.MINUS: sb.Append(" - "); break;
                case Core.SUB_STAR or Core.STAR: sb.Append(" * "); break;
                case Core.SUB_SLASH or Core.SLASH: sb.Append(" / "); break;
                // GROUPING-PAREN-ONLY (fix-queue PB48): the argument-list twins FNARG_LPAREN/FNARG_RPAREN are
                // deliberately absent. A segment carrying them is function-bearing, and the `default:` arm below
                // routes ANY unrenderable token to D18 — which is where such a segment belongs anyway — so
                // adding them here would render a function call's parens into a token-by-token string that the
                // rest of this switch cannot complete. PB42's rule ("can the renderer render it") is what makes
                // the omission safe rather than lucky.
                case Core.SUB_LPAREN or Core.LPAREN: sb.Append('('); break;
                case Core.SUB_RPAREN or Core.RPAREN: sb.Append(')'); break;
                case Core.SUB_IDENTIFIER or Core.IDENTIFIER:
                {
                    // Gather `name (OF|IN qualifier)*` — a QUALIFIED data-name subscript (ISO §8.4.2.3.2).
                    string name = t.Text;
                    var qualifiers = new List<string>();
                    int j = i;
                    while (true)
                    {
                        int k = j + 1;
                        while (k < tokens.Count && tokens[k].Type == Core.SUB_WS) k++;
                        if (k >= tokens.Count || tokens[k].Type is not (Core.SUB_OF or Core.SUB_IN or Core.OF or Core.IN)) break;
                        int m = k + 1;
                        while (m < tokens.Count && tokens[m].Type == Core.SUB_WS) m++;
                        if (m >= tokens.Count || tokens[m].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) break;
                        qualifiers.Add(tokens[m].Text);
                        j = m;
                    }
                    i = j;
                    // A FUNCTION-BEARING segment cannot be rendered token-by-token (the head word is a function
                    // name, not a data-name), so the WHOLE segment routes to D18 rather than this arm failing.
                    if (IsFunctionBearing(tokens)) return MaterializeViaFragment(tokens, position);
                    // ⛔ AN UNRESOLVABLE NAME ROUTES TO D18 — IT IS NOT A VERDICT (fix-queue PB50). This arm used
                    // to `return null`, which is the caller's LOUD posture, and that made it the one place in
                    // this renderer that decided a segment was unrenderable without asking D18 — contradicting
                    // the rule stated ten lines below it ("EVERY token the renderer cannot render ROUTES TO
                    // D18"). SUBSCRIPT mode has no ZERO token, so the figurative arrives as a plain
                    // SUB_IDENTIFIER, resolves to no data item, and `E(ZERO + 1)` ABORTED AT RUN TIME —
                    // §8.8.1.1 admits "the figurative constant ZERO" as an arithmetic operand and §8.4.2.3.2
                    // makes a subscript an arithmetic expression, so that is legal source.
                    // ⚠ A GENUINELY undefined name keeps the SAME posture, verified rather than assumed:
                    // `E(NOSUCHNAME + 1)` aborted at run time before this change and aborts at run time after
                    // it. Only the message improved — it now names `NOSUCHNAME` instead of the whole reference
                    // text, because the fragment binder reaches the individual operand. (That an undefined
                    // subscript name is a RUN-TIME abort at all is a separate, pre-existing wrong-stage
                    // defect; it is recorded in PB50's note, not fixed here.)
                    if (ResolveSubscriptName(name, qualifiers, position, ref pending, out bool scaled) is not { } readExpr)
                        return MaterializeViaFragment(tokens, position);
                    // A scaled operand inside a compound segment — evaluate the whole expression instead (above).
                    if (scaled && compound) return MaterializeViaFragment(tokens, position);
                    sb.Append(readExpr);
                    break;
                }
                // ⛔ EVERY token the renderer cannot render ROUTES TO D18 — the gate asks "can this be rendered",
                // never "is this one of a listed set" (fix-queue PB42). The listed-set version shipped for one
                // commit and dropped two shapes of plain legal arithmetic on the floor: `W-E(W-I ** 2)` and
                // `W-E(2.0)` each compiled clean and threw at RUN TIME, because `**` had no case arm and a
                // decimal literal had none either — while §8.8.1.1 admits "a numeric literal … separated by
                // arithmetic operators" and §8.3.2.4.2 lists `**` as one, so both are arithmetic-expression-1
                // under §8.4.2.3.2 and legal in the position.
                // ⚠ THIS IS NOT THE UNAUDITED-TABLE MISTAKE PB1 TAUGHT, and the reason is structural: the
                // materializer re-parses the segment through `subscriptExpressionFragment : arithmeticExpression
                // EOF`, so THE ARITHMETIC-EXPRESSION GRAMMAR IS THE ADJUDICATOR. A shape §8.8.1.1 does not admit
                // — an alphanumeric literal, an ALL figurative constant (legal only in the §8.4.2.3.3 r6
                // positions) — cannot parse as an arithmetic expression, so the fragment returns null and the
                // caller keeps the exact loud posture it had. Nothing is admitted by assertion; it is admitted by
                // parsing, which is why the NEXT arithmetic token needs no edit here at all.
                default: return MaterializeViaFragment(tokens, position);
            }
        }
        string expr = sb.ToString().Trim();
        if (expr.Length == 0) return null;   // the caller's loud posture — nothing was rendered, so nothing is screened
        if (pending is not null && !_probing)   // R30 purity: a probe never diagnoses (kb/Work PB157)
            foreach (var ps in pending)
                if (ps.Item is { } it) ScreenPositionOperandClass(it, ps.Name, position);
                else IndexNameInPositionError(ps.Name, position);
        return expr;
    }

    /// <summary>A screen the fast path OWES once it commits to rendering the segment — either the §8.8.1.1 class
    /// screen over a resolved item (<see cref="ScreenPositionOperandClass"/>) or the §13.18.38.3 r7 index-name
    /// screen (<see cref="IndexNameInPositionError"/>, <c>Item</c> null). Queued rather than emitted so an exit
    /// to D18 later in the token loop cannot leave a diagnostic behind for the D18 route to duplicate — see
    /// <see cref="RenderSegment"/>.</summary>
    private readonly record struct PendingScreen(DataItem? Item, string Name);

    /// <summary>True when this segment contains a FUNCTION-IDENTIFIER (ISO §8.4.3.1.2 Format 1) and therefore
    /// belongs to the D18 materialization route rather than the token renderer: either the explicit
    /// <c>FUNCTION</c> keyword (a plain <c>SUB_IDENTIFIER</c> in SUBSCRIPT mode), or the §8.4.3.2.3 SR2
    /// keyword-omitted form — a REPOSITORY-declared intrinsic or a user-function name, immediately followed by a
    /// left parenthesis, that is NOT shadowed by a declared data item (a declared item always wins, exactly as in
    /// <c>IntrinsicBinder.KeywordOmittedFunction</c>; the two must not drift apart, which is why both ask the
    /// question the same way).</summary>
    private bool IsFunctionBearing(List<IToken> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type is not (Core.SUB_IDENTIFIER or Core.IDENTIFIER)) continue;
            string w = tokens[i].Text;
            if (w.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)) return true;
            int k = i + 1;
            while (k < tokens.Count && tokens[k].Type == Core.SUB_WS) k++;
            // GROUPING-PAREN-ONLY (fix-queue PB48): this arm detects the KEYWORD-OMITTED form `name(args)`,
            // which by definition has no FUNCTION token before the name — so its '(' is never retyped
            // FNARG_LPAREN (the lexer's mark keys on exactly that token). The explicit-keyword form is caught by
            // the `w == "FUNCTION"` test above and never reaches here.
            if (k >= tokens.Count || tokens[k].Type is not (Core.SUB_LPAREN or Core.LPAREN)) continue;
            if (data.Symbols.TryResolve(w, data.ActiveScope, out _)) continue;   // a declared item wins
            if (data.UserFunctionNames.Contains(w)
                || ((data.RepositoryAllIntrinsic || data.RepositoryIntrinsics.Contains(w))
                    && IntrinsicCatalog.TryGet(w, out _)))
                return true;
        }
        return false;
    }

    /// <summary>The D18 route (fix-queue PB17, widened by PB42): materialize ANY segment the token renderer
    /// cannot render into the §15.4 temporary, and render the segment as that temp's ordinary position read. The
    /// segment's VERBATIM source text is recovered from the char stream (the <c>IntrinsicBinder.ReparseArgs</c>
    /// idiom — spacing is significant, e.g. <c>A - 4</c> vs <c>A -4</c>) and handed to the
    /// <see cref="MaterializeSegment"/> hook.
    /// <para>⛔ IT IS DELIBERATELY NOT GATED ON A TOKEN LIST. The hook re-parses the text through
    /// <c>subscriptExpressionFragment : arithmeticExpression EOF</c>, so the ARITHMETIC-EXPRESSION GRAMMAR decides
    /// admissibility: a shape §8.8.1.1 does not admit cannot parse, the hook returns null, and the caller keeps
    /// its loud posture unchanged. That is what lets a function-identifier, <c>**</c>, a decimal literal and every
    /// future arithmetic form share ONE route with no per-token edit — and why widening the gate cannot repeat
    /// PB1's unaudited-table mistake.</para>
    /// <para>Null when the hook is absent (a data-division resolver builds a throwaway resolver with no procedure
    /// binder) or the fragment fails to parse or bind — in every such case the caller's loud posture is exactly
    /// what it was before D18.</para></summary>
    private string? MaterializeViaFragment(List<IToken> tokens, SegmentPosition position)
    {
        // R30 PURITY (kb/Work PB157): a probe must not BIND the segment — the hook registers a §15.4 pre-op
        // and a function-bearing segment would activate TWICE (once per probe+commit). The probe's Place is
        // discarded after its Item is read, so a dummy occurrence expression is never emitted.
        if (_probing) return "1";
        if (MaterializeSegment is null || tokens.Count == 0) return null;
        var first = tokens[0];
        if (first.InputStream is not { } stream) return null;
        string text = stream.GetText(
            new Antlr4.Runtime.Misc.Interval(first.StartIndex, tokens[^1].StopIndex));
        return MaterializeSegment(text, position, first.Line) is { } temp ? PositionRead(temp, position) : null;
    }

    /// <summary>A subscript data-name → its C# read expression: an INDEXED BY index-name (a <c>long</c> field), or
    /// a (possibly OF/IN-qualified, ISO §8.4.2.3.2) numeric data item read; <see langword="null"/> if unresolvable.
    /// A data-item read is wrapped in <c>CobolTable.Occ(…)</c> — overload resolution converts a STRING-stored
    /// occurrence number (a leaf the post-bind whole-group analysis flags <see cref="DataItem.StoreAsImage"/>, a
    /// decision NOT yet made when this bind-time text is produced) exactly as a native <c>long</c>.</summary>
    /// <param name="scaled">Set when the resolved operand carries a nonzero PICTURE scale, so the caller can send a
    /// COMPOUND segment to the D18 materializer instead (the §8.4.2.3.4 GR1b result-vs-operand distinction).</param>
    private string? ResolveSubscriptName(string name, List<string> qualifiers, SegmentPosition position,
        ref List<PendingScreen>? pending, out bool scaled)
    {
        scaled = false;
        // An index-name is an occurrence number by construction (§13.18.38) and a constant-name substitutes an
        // INTEGER literal — neither can be scaled, so both keep the fast path.
        // ⛔ BUT ONLY IN THE POSITION r7 LISTS (kb/Work PB170). §13.18.38.3 r7's five contexts include "as a
        // subscript" and do NOT include a reference-modification position, and this line returned the index
        // field regardless of `position` — so `W(IX:2)` compiled clean. The R16 screen for exactly this rule
        // already existed (ExpressionBinder.ScreenIndexNameOperand); it simply was not applied here.
        if (qualifiers.Count == 0 && data.Symbols.TryResolveIndex(name, data.ActiveScope, out var field))
        {
            if (position == SegmentPosition.Subscript) return field;
            (pending ??= []).Add(new PendingScreen(null, name));
            return field;   // keep rendering: a null here would re-route to D18 and screen the operand twice
        }
        // An INTEGER constant-name in a subscript position substitutes its integer literal (ISO §13.10.3 SR2 /
        // §13.10.4 GR1/GR3 — a subscript is a literal position, §8.4.2.3.2) — the literal text IS the C# read.
        if (qualifiers.Count == 0
            && data.FindConstant(name) is { Category: PicCategory.Numeric, IsInteger: true } k)
            return k.Text;
        DataItem? item = qualifiers.Count == 0 ? ResolveUnqualified(name) : ResolveQualified(name, qualifiers);
        if (item is null) return null;
        if (!IntrinsicArgumentRules.IsArithmeticOperandClass(item)) (pending ??= []).Add(new PendingScreen(item, name));
        scaled = item.Pic?.Scale > 0;
        return PositionRead(item, position);
    }

    /// <summary>⛔ §13.18.38.3 r7 AT THE TOKEN RENDERER'S REF-MOD BOUND — <b>ONE RULE, ONE LANE POSTURE, DECIDED
    /// BY THE SLOT KIND</b> (kb/Work PB219). r7 is enforced at four sites and they were not all on the same
    /// disposition; the axis that settles each one is what the slot IS, never which route reached it:
    /// <list type="bullet">
    ///   <item><b>An ARITHMETIC-expression position</b> — <c>ExpressionBinder.IndexNameExpr</c> (R29) and THIS
    ///   site. §8.4.3.3.3 SR4 makes a reference-modification leftmost-position/length an arithmetic expression,
    ///   so the ref-mod bound is R29's family, not R16's. Posture: strict REJECTS with the r7 citation;
    ///   <c>--permissive</c> WARNS and computes the occurrence number (the documented GnuCOBOL coercion). The
    ///   emit floor is identical either way — this method's caller renders <c>field</c> in both lanes.</item>
    ///   <item><b>An IDENTIFIER slot</b> — <c>ExpressionBinder.ScreenIndexNameOperand</c> (R16: DISPLAY, MOVE,
    ///   STRING, the STOP RUN/GOBACK status operand) and <c>InspectBinder</c>. Posture: an unconditional Error in
    ///   BOTH lanes, and the written reason is that THERE IS NO COERCION TO OFFER — the slot needs an identifier,
    ///   and an occurrence number is not one. That is a leniency this compiler declines to invent, not an
    ///   oversight; <c>dialect_two_axes</c> constrains the leniencies you implement, it does not require one.</item>
    /// </list>
    /// Before this, the ref-mod fast path carried the R16 posture while its OWN D18 route carried R29's, so
    /// under <c>--permissive</c> <c>W(IX:2)</c> was a hard error and <c>W(IX / 1:2)</c> — the same rule, the same
    /// position — warned and compiled, keyed on nothing but whether the token renderer could render the
    /// bound.</summary>
    private void IndexNameInPositionError(string name, SegmentPosition position)
    {
        string what = $"the index-name '{name}' is not an identifier (ISO §8.4.3.1.2) and a "
            + "reference-modification leftmost-position/length is not one of §13.18.38.3 r7's five contexts (a "
            + "subscript, PERFORM/SEARCH VARYING, SET, a relation condition) — §8.4.3.3.3 SR4 makes both bounds "
            + "arithmetic expressions";
        if (data.Edition.Permissive)
            data.Edition.Warning(DiagnosticCatalog.IndexNameContext,
                $"{what}; accepted under --permissive, computing the occurrence number");
        else
            data.Edition.Error(DiagnosticCatalog.IndexNameContext,
                $"{what}. SET a data item to the index first (SET data-item TO {name})");
    }

    /// <summary>⛔ THE §8.8.1.1 CLASS SCREEN FOR THE TOKEN RENDERER'S FAST PATH (kb/Work PB170) — the funnel entry
    /// that never reached the funnel.
    /// <para><b>The chain, every link cite.py-checked.</b> §8.4.2.3.2's subscript general format is exactly three
    /// alternatives — <c>ALL | arithmetic-expression-1 | index-name-1 [{+|-} integer-1]</c> — so a bare data-name
    /// subscript is admitted ONLY as arithmetic-expression-1; §8.8.1.1 then admits "an identifier referencing a
    /// NUMERIC data item, a numeric literal, the figurative constant ZERO"; §8.5.2.1 Table 2 puts category
    /// alphanumeric and alphanumeric-edited in class ALPHANUMERIC. §8.4.3.3.3 SR4 ("leftmost-position and length
    /// shall be arithmetic expressions") carries the identical rule to the ref-mod bounds. So <c>T(XE)</c> and
    /// <c>W(XE:2)</c> with <c>XE PIC X(4)</c> are illegal, and <c>T(IDX)</c> with an index DATA item is illegal
    /// twice over (§13.18.60.3 SR10's closed reference list has no subscript entry).</para>
    /// <para><b>Why the screen has to be HERE.</b> <see cref="ResolveSubscriptName"/> renders the position read
    /// straight from the token — <c>ExpressionBinder.OperandRef</c> never runs, because the operand never enters
    /// the expression binder at all. Measured on 9a89fbd1: <c>E(XE)</c> compiled clean and digit-decoded "0002"
    /// to occurrence 2, and so did the COMPOUND <c>E(XE + 1)</c> — the renderer routes to the screened D18 path
    /// only for a slash, a function, an unresolvable name, a SCALED operand inside a compound, or a token with no
    /// case arm, and plain <c>+</c> over an unscaled alphanumeric name is none of those.</para>
    /// <para>⚠ THE VERDICT IS <see cref="IntrinsicArgumentRules"/>'s, deliberately: a category switch written here
    /// would have been the fifth place answering "is this operand class numeric". Routing the segment to D18
    /// instead — which would reuse OperandRef's screen with no new code — was REJECTED: it materializes a §15.4
    /// temp and a PendingPreOp for every permissive alphanumeric subscript, changing permissive-lane emitted text
    /// and adding an integrality check where <c>CobolTable.Occ(string)</c> has none. Screening in place keeps the
    /// emit floor byte-identical.</para>
    /// <para>⚠ AND THE RENDERER'S OWN INVARIANT IS RESTORED, not abandoned: <see cref="RenderSegment"/> documents
    /// itself as "an optimization over that route, never the arbiter of what is legal in the position", and this
    /// defect falsified it. Asking the ONE classifier the question the binder would have asked is what makes the
    /// sentence true again — the fast path now reaches the same verdict, not a different one.</para>
    /// <para>⚠ PRECONDITION: the class question is asked in <see cref="ResolveSubscriptName"/>
    /// (<c>IntrinsicArgumentRules.IsArithmeticOperandClass</c>) and only a REJECTED item is queued, so this
    /// method composes the message and nothing else. The <c>_probing</c> purity guard lives at the queue's flush
    /// in <see cref="RenderSegment"/> — ONE place, since that is also where the ordering fix lives
    /// (kb/Work PB220).</para></summary>
    private void ScreenPositionOperandClass(DataItem item, string name, SegmentPosition position)
    {
        // ⛔ NO GROUP / POINTER / OBJECT-REFERENCE ARM, AND THAT IS A REACHABILITY FACT, NOT AN OMISSION
        // (kb/Work PB201). This method runs only when the fast path COMMITS to rendering the segment, and
        // <see cref="HasPositionOverload"/> lets it commit only for a carrier <c>CobolTable.Occ</c> declares a
        // parameter for — <c>long</c>, <c>string</c>, <c>Int128</c>, <c>ulong</c>, <c>UInt128</c>. A group's
        // carrier is its per-program <c>record struct</c> and a pointer's is <c>ManagedPointer</c>, so
        // both now route to D18 and are screened by <c>ExpressionBinder.OperandRef</c> instead — the same
        // COBOLNET0844 over the same §8.8.1.1, minus this method's position phrase.
        // ⚠ THE INTERSECTION IS NARROWER THAN THAT LIST, and it is the second precondition that narrows it: only
        // an item <c>IntrinsicArgumentRules.IsArithmeticOperandClass</c> REJECTED is ever queued, and the three
        // wide/unsigned carriers belong to class NUMERIC items, which it accepts. So what reaches HERE is
        // exactly: an index DATA item (an <c>IndexCell</c> is a <c>long</c>) and the string-carrier categories
        // — alphanumeric, national, boolean, numeric-edited and their edited forms.
        string what = item.Pic is { Usage: Usage.Index }
                ? $"item '{name}', an index data item (class index, ISO §8.5.2.1 Table 2; §13.18.60.3 SR10 admits "
                  + "an index data item only in SEARCH/SET, a relation condition, or an intrinsic argument)"
            : $"item '{name}' of class "
              + $"{IntrinsicArgumentRules.ClassOfItem(item)?.ToString().ToLowerInvariant() ?? "unknown"} "
              + "(ISO §8.5.2.1 Table 2)";
        string where = position == SegmentPosition.Subscript
            ? "a subscript is arithmetic-expression-1 (ISO §8.4.2.3.2)"
            : "a reference-modification leftmost-position/length is an arithmetic expression (ISO §8.4.3.3.3 SR4)";
        // The SAME dialect gate the expression-binder screen carries (dialect_two_axes — every leniency is
        // dialect-gated): --permissive keeps the CobolTable.Occ(string) digit decode, which is the leniency
        // already implemented by that overload, and the emitted text is unchanged either way.
        if (data.Edition.Permissive)
            data.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand — {where}, and ISO §8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO; accepted under --permissive, decoding its digit characters as an "
                + "unsigned integer");
        else
            data.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: {where}, and ISO §8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO. --permissive accepts it as a digit-decoding extension");
    }

    /// <summary>⛔ THE ONE ORDINAL-POSITION READ (fix-queue PB41): an already-resolved numeric item → the C#
    /// <c>long</c> expression for the POSITION it denotes, in either position kind.
    /// <para>A COBOL.NET numeric item stores UNSCALED — <c>PIC 9V9 VALUE 2.0</c> is the field <c>20L</c> at scale
    /// 1 — so the item's VALUE and its STORAGE are different numbers whenever the PICTURE has a <c>V</c>. Both
    /// position clauses are about the VALUE: §8.4.2.3.4 GR1b makes the subscript "the result of the evaluation of
    /// arithmetic-expression-1", and §8.4.3.3.4 rule 5)c) says the same for a leftmost-position/length. Reading the
    /// storage instead is what made <c>W-E(W-S)</c> with <c>W-S = 2.0</c> index occurrence 20 and return the
    /// out-of-range scratch.</para>
    /// <para>A scale-0 item (the overwhelming majority) keeps the EXACT previous text — the bare
    /// <c>CobolTable.Occ(path)</c> — so the generated C# for ordinary subscripts is byte-identical and no
    /// de-scaling division is emitted where none is needed. A scaled item passes its scale to the overload that
    /// de-scales and raises the position's own Table 13 condition on a fractional value.</para>
    /// <para>⚠ The runtime call is spelled out rather than routed through <c>RuntimeApi</c>: this text is produced
    /// at BIND time (the D10 transitional string carrier) and the binder cannot reference the CodeGen assembly.
    /// When PHASE 15 CUT 2.5 removes the SUBSCRIPT lexer mode and the carrier becomes <c>BoundExpr</c>, this
    /// rendering moves to the renderer with the rest of it.</para></summary>
    private string? PositionRead(DataItem item, SegmentPosition position)
    {
        // Scale 0 — an integer item — has no integrality question to answer, so neither position can raise and the
        // position kind is irrelevant: keep the ONE historical text unchanged for both.
        int scale = item.Pic?.Scale ?? 0;
        // ⛔ THE BET ON OVERLOAD RESOLUTION IS ONLY GOOD FOR THE CARRIERS THAT HAVE AN OVERLOAD (kb/Work PB201).
        if (!HasPositionOverload(item, scale)) return null;
        if (AccessPath(item, []) is not { } path) return null;
        if (scale <= 0) return $"CobolTable.Occ({path})";
        return position == SegmentPosition.Subscript
            ? $"CobolTable.Occ({path}, {scale})"
            : $"CobolString.RefModPosition({path}, {scale})";
    }

    /// <summary>⛔ THE FAST PATH'S ADMISSION TEST (kb/Work PB201): can the C# text <see cref="PositionRead"/> is
    /// about to emit actually BIND against the operand's generated field? The bind-time renderer names the field
    /// and lets C# overload resolution supply the conversion — the deliberate design that lets ONE text serve a
    /// carrier the post-bind whole-group analysis has not chosen yet — but that bet is good ONLY for the carrier
    /// types the emitted helper declares a parameter for.
    /// <para><b>The overload sets are the whole rule.</b> <c>CobolTable.Occ</c> takes
    /// <c>long | string | Int128 | ulong | UInt128</c> unscaled and <c>long | string | Int128</c> with a scale;
    /// <c>CobolString.RefModPosition</c> takes the scaled three. The remaining carriers
    /// <see cref="DataItem.ElementType"/> can produce — <c>double</c>/<c>float</c> (a COMP-1/COMP-2
    /// leaf), <c>ManagedPointer</c>/<c>ProgramPointer</c>, an object reference, and a GROUP's <c>record struct</c>
    /// name — have none, and the emitted text was therefore not C# that compiles.
    /// ⚠ The FLOAT exclusion is a RULE, not a missing overload: a <c>double</c> operand can be
    /// fractional and §8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the expression "does not result in an
    /// integer", a test the scale-less overload does not perform — so a float belongs on the D18
    /// route, where the §15.4 temp applies the rule once, to the result. The unsigned-binary carriers are
    /// admitted UNSCALED only for the same reason from the other side: there is no scaled overload for them, and
    /// a scaled one has an integrality question to answer.
    /// MEASURED, six shapes, before this fix: <c>TE(FD1)</c> with <c>FD1 USAGE COMP-2</c>,
    /// <c>TE(BIG)</c> with <c>BIG PIC 9(20) COMP</c> and <c>TE(W-U)</c> with
    /// <c>W-U USAGE BINARY-DOUBLE UNSIGNED</c> each failed the BACKEND with
    /// <c>CS1503 cannot convert from 'double'/'System.Int128'/'ulong' to 'long'</c> — in the DEFAULT lane, on source
    /// §8.8.1.1 and §8.4.2.3.4 GR1b make perfectly legal — <c>W(FD1:2)</c> the same at a ref-mod bound, and a
    /// GROUP subscript under <c>--permissive</c> emitted the record struct (<c>CS1503 '_T_0' to 'long'</c>) or, for
    /// a class-tier BASED group, a name with no C# field at all (<c>CS0103</c>).</para>
    /// <para><b>Why a route and not only wider overloads.</b> The three INTEGER carriers above genuinely were
    /// missing overloads and got them (they are also needed by the second emitter, <c>RuntimeApi.TableOcc</c>,
    /// which renders the OCCURS DEPENDING current count at CODEGEN time and has no route to fall back to). But
    /// overloads alone can never close this: a group's carrier can never have a runtime overload, being a
    /// per-program generated type, and a pointer has no numeric value to convert at all. The
    /// route already exists and is the documented posture: <see cref="RenderSegment"/> is "an optimization over
    /// that route, never the arbiter of what is legal in the position", so an operand it cannot render is
    /// <see cref="MaterializeViaFragment"/>'s, where <c>ExpressionBinder</c> screens it under §8.8.1.1 and
    /// <c>NumericRenderer.FieldNum</c> — THE ONE numeric read — supplies the carrier-correct decode (the float
    /// sending check, the wide tier, and a group's §8.5.2.1 alphanumeric IMAGE, which is exactly the digit decode
    /// the <c>--permissive</c> message promises and the fast path never performed).</para>
    /// <para>⚠ Returning FALSE is always sound and never a verdict: the segment reroutes to D18, whose loud
    /// posture on failure is the caller's pre-existing one. The pre-promotion reading of <c>ElementType</c> is
    /// likewise safe in the one direction it can be wrong — promotion only ever moves a leaf TO
    /// <c>CharImage</c>/<c>string</c>, which is in the set, so a leaf admitted here stays admitted.</para></summary>
    private static bool HasPositionOverload(DataItem item, int scale)
    {
        string carrier = item.ElementType;
        foreach (string admitted in scale > 0 ? ScaledPositionCarriers : UnscaledPositionCarriers)
            if (admitted == carrier) return true;
        return false;
    }

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>)</c> declares a parameter for — the ONE list
    /// <see cref="HasPositionOverload"/> reads for a scale-0 operand, exposed so
    /// <c>PositionCarrierOverloadDriftTests</c> can compare it
    /// to the runtime method's ACTUAL overloads by reflection. Adding an overload without widening this list
    /// leaves the fast path routing a carrier it could now render; widening this list without the overload puts
    /// the CS1503 back. The test fails on either.</summary>
    internal static readonly string[] UnscaledPositionCarriers =
        ["long", "string", "Int128", "ulong", "UInt128"];

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>, int)</c> and
    /// <c>CobolString.RefModPosition(<i>x</i>, int)</c> declare a parameter for. It is NOT a superset of the
    /// unscaled list: the unsigned-binary carriers have no scaled overload (a scaled operand has an integrality
    /// question, so D18 is the right route for them), while <c>Int128</c> must be here because the D18 §15.4
    /// segment temp is a 30-digit / scale-9 item — the wide tier — and
    /// <see cref="MaterializeViaFragment"/> reads it back through <see cref="PositionRead"/>, so dropping it
    /// would break the very route this guard sends work to.</summary>
    internal static readonly string[] ScaledPositionCarriers = ["long", "string", "Int128"];
}
