// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The INITIALIZE verb binder (P7 Step 10e — a real collaborator over <see cref="BinderContext"/>):
/// the ONE <c>_initializeLoopVar</c> owner (program-unique <c>__iniN</c> loop variables — one instance per
/// unit or the emitted locals renumber/redeclare); <c>ActiveScope</c> is read through ctx PER CALL, never
/// captured (the OO roster mutates it). The bound records stayed in <c>Binding/Bound/BoundInitialize.cs</c>;
/// the 0830–0833 2002-surface gate block moved VERBATIM (Exec Step E folds it).</summary>
internal sealed class InitializeBinder(BinderContext ctx, StatementBinder host)
{
    private int _initializeLoopVar;   // program-unique loop-variable counter (__iniN — `__` cannot occur in COBOL names)

    /// <summary>What the statement's phrases select, threaded through the expansion: the receiver filter
    /// (ISO §14.9.20 GR5c) and the sending-operand precedence VALUE → REPLACING → category default (GR6).
    /// <para>⛔ EVERY CATEGORY SLOT IS A <see cref="InitializeCategorySet"/>, BECAUSE category-name IS A SET
    /// (§14.9.20.2's choice-indicator brace; §5.2.6.4). <paramref name="ValueCategories"/> EMPTY with
    /// <paramref name="ToValue"/> means <c>ALL TO VALUE</c> — §14.9.20.4 GR2's "if ALL is specified in the VALUE
    /// phrase it is as if all of the categories listed in category-name were specified", represented as the
    /// absence of a restriction. ⛔ A BARE <c>TO VALUE</c> IS NOT ALL AND IS NOT CONFORMING: §14.9.20.2's brace
    /// requires one of ALL / category-name and §5.2.6.3 makes that mandatory, so <c>Bind</c> rejects it
    /// (COBOLNET1981) and only then recovers as if ALL. The former code read it as ALL on the strength of a
    /// "§14.9.20.2 note 2" the clause does not have (kb/Work PB415).</para>
    /// <para>⛔ <paramref name="HasReplacing"/> IS THE PHRASE'S PRESENCE, NOT <paramref name="Replacements"/>'s
    /// COUNT. §14.9.20.4 GR5c4's premise is "neither the REPLACING phrase nor the VALUE phrase is specified" — a
    /// REPLACING phrase whose every item was dropped (the §14.9.20.3 SR6 duplicate-category skip) is still
    /// SPECIFIED, so an item of a non-matching category is left unchanged by GR5c and must NOT collect the GR6c
    /// category default. Reading the count made the two indistinguishable.</para></summary>
    private readonly record struct InitializeSpec(
        bool WithFiller, bool ToValue, InitializeCategorySet ValueCategories,
        IReadOnlyList<(InitializeCategorySet Cats, BoundOperand Value)> Replacements, bool HasReplacing, bool ToDefault);

    /// <summary>⛔ WHY a possible receiving-operand IS a receiving-operand — ISO §14.9.20.4 GR5c's four
    /// alternatives, collapsed to the ONE fact §14.9.20.4 GR6 then asks for. GR6 is not a free precedence over the
    /// phrases: its arms are literally keyed on the qualification ("if the data item qualifies as a
    /// receiving-operand because of the VALUE phrase" → GR6a; "does not qualify because of the VALUE phrase, but
    /// does qualify because of the REPLACING phrase" → GR6b; "does not qualify in accordance with General rules 6a
    /// and 6b" → GR6c). So the qualification TEST and the SENDER CHOICE are two rules, and this enum is the value
    /// that carries the first one's answer into the second.
    /// <para>⛔ THE DEFECT THIS SHAPE EXISTS TO PREVENT (kb/Work PB418): the two rules used to be ONE method, whose
    /// VALUE arm returned a sender only when the item carried a Format-1 VALUE clause. That made "has a VALUE
    /// clause" the qualification premise where GR5c1 states a THREE-way test, so (i) a pointer / object-reference
    /// receiver — which §13.18.63.3 SR9 and §8.4.3.10.3 forbid a VALUE clause on, and which GR5c1a therefore
    /// qualifies CATEGORICALLY — was never nulled, and (ii) a table-format VALUE (GR5c1c) was invisible, so
    /// <c>ALL TO VALUE</c> stored nothing and <c>ALL TO VALUE THEN TO DEFAULT</c> stored SPACES over the very
    /// values it exists to restore. A fused predicate cannot say "already qualified through VALUE, keep that
    /// sender"; an ordered qualification can, and every future category word (kb/Work PB415) is one case in
    /// <see cref="Qualify"/> rather than a new arm in a sender.</para></summary>
    private enum InitializeQualification
    {
        /// <summary>GR5c matched no alternative — the item is left unchanged.</summary>
        None,
        /// <summary>GR5c1 — the VALUE phrase (categorically, GR5c1a; or by a Format-1 / Format-2 VALUE clause,
        /// GR5c1b / GR5c1c). Sender: GR6a.</summary>
        ViaValue,
        /// <summary>GR5c2 — the REPLACING phrase names this item's category. Sender: GR6b.</summary>
        ViaReplacing,
        /// <summary>GR5c3 (the DEFAULT phrase) or GR5c4 (neither REPLACING nor VALUE specified). Sender: GR6c's
        /// category-default fill table — the two alternatives are ONE member because GR6c does not distinguish
        /// them ("does not qualify in accordance with General rules 6a and 6b").</summary>
        ViaDefault,
    }

    /// <summary>One entered OCCURS dimension on the way down to an elementary receiver, in §13.18.63.3 SR20's order
    /// (most inclusive first) — the key a Format-2 (table) VALUE plan is looked up by. A dimension the expansion
    /// itself loops over is known only as its bind-time loop VARIABLE (the occurrence is a run-time value, so the
    /// GR5c1c test becomes an emitted branch); a dimension identifier-1 pinned with its own INTEGER-LITERAL
    /// subscript is known as that number and is resolved entirely at bind time. Both null = an occurrence identifier-1
    /// supplied as a run-time expression, which no bind-time lookup can key.</summary>
    private readonly record struct OccurrenceDim(string? Var, int? Fixed);

    /// <summary>Bind INITIALIZE (ISO §14.9.20). The COBOL-85 surface — identifier-1‥n (full data references:
    /// qualification AND subscripts) and the REPLACING phrase — binds completely; the 2002+ phrases (WITH FILLER,
    /// [ALL | category] TO VALUE, the THEN connective, THEN TO DEFAULT — Annex E additions) are edition-gated and
    /// bind for 2002+ targets. The whole receiver expansion happens here; the emitter renders each store through
    /// the canonical MOVE path.</summary>
    public BoundStatement Bind(Core.InitializeStatementContext ini)
    {
        bool withFiller = ini.FILLER() is not null;
        var toValue = ini.initializeCategoryToValue();
        var replacing = ini.initializeReplacingPhrase();
        bool toDefault = ini.initializeDefaultPhrase() is not null;

        // initialize-{filler,to-value,to-default,then-replacing}-2002 (§14.9.20): the pass owns the four
        // post-85-surface edition gates (Exec Step E).

        var replacements = new List<(InitializeCategorySet Cats, BoundOperand Value)>();
        // §14.9.20.3 SR6 "the same category shall not be repeated in a REPLACING phrase" and §5.2.6.4's "any
        // single alternative shall be specified only once" are ONE rule over ONE accumulator: the union of every
        // category already named in this phrase, whether by an earlier item or by an earlier word of THIS item.
        var namedSoFar = default(InitializeCategorySet);
        if (replacing is not null)
            foreach (var item in replacing.initializeReplacingItem())
            {
                var cats = CategorySetOf(item.initializeCategory(), ref namedSoFar);
                if (cats.IsEmpty) continue;   // every word was a repeat — reported by the pure check
                // §14.9.20.3 SR4 makes identifier-2 "the SENDING item" of a MOVE, so a function-identifier
                // is admissible (§8.4.3.1.2 Format 1; §8.4.3.2.3 SR1 bars one only from a RECEIVING
                // operand). It was a COBOL0001 parse error before — fix-queue PB10.
                var lit = item.literal();
                BoundOperand value = item.inlineMethodInvocation() is { } iimi
                        ? host.Oo.OoInlineInvocationOperand(iimi)   // §8.4.3.1.2 Format 4; kb/Work PB428
                    : item.functionCall() is { } ifc ? host.Intrinsic.IntrinsicOperand(ifc)
                    : lit is not null ? host.Expr.LiteralOperand(lit)
                    : item.dataReference() is { } sref ? host.Expr.FieldOperand(sref)
                    : new BoundOperandError("INITIALIZE REPLACING sending operand");
                // ISO §14.9.20.3 SR3 — "for each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE,
                // or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2
                // shall be specified". literal-1 is what the rule excludes, and it excludes it because GR4 makes
                // the implicit statement a SET for exactly these five categories and no literal is a SET sending
                // operand (§14.9.39). Unreachable until the words became spellable (kb/Work PB415).
                string senderText = lit?.GetText() ?? item.dataReference()?.GetText()
                    ?? item.functionCall()?.GetText() ?? "the sending operand";
                if (lit is not null)
                    ctx.Validation.CheckInitializeReplacingSetCategoryIdentifier(cats, lit.GetText());
                else
                    CheckSetFormCategoryAgreement(cats, value, senderText);
                // ISO §14.9.20.3 SR4's SECOND paragraph — the MOVE half, for every category-name that is not one
                // of GR4's five SET-form ones. It is asked HERE, over the phrase, because the rule's receiving
                // operand is "an item of the specified category" and not any item identifier-1 happens to
                // contain: the pair is legal or illegal before a single receiver has been walked (kb/Work PB416).
                CheckReplacingMoveValidity(cats, value, senderText);
                replacements.Add((cats, value));
            }

        // ISO §14.9.20.2 — the VALUE phrase's `{ ALL | category-name }` is a BRACE, and §5.2.6.3 makes one of its
        // alternatives mandatory. The grammar keeps the subrule optional purely so this rejection can NAME the
        // rule (see the CobolData.g4 note); the recovery is ALL, which is what the omission used to mean silently.
        var valueCats = default(InitializeCategorySet);
        if (toValue is not null)
        {
            if (toValue.initializeCategory() is { } vc) valueCats = CategorySetOf(vc);
            else if (toValue.ALL() is null) ctx.Validation.CheckInitializeValueChoicePresent();
        }

        var spec = new InitializeSpec(withFiller, toValue is not null, valueCats, replacements,
            HasReplacing: replacing is not null, toDefault);

        // ⛔ ONE BoundInitialize PER identifier-1, NOT one flat expansion — ISO §14.9.20.4 GR3: "the result of
        // executing this INITIALIZE statement is the same as if a separate INITIALIZE statement had been written
        // for each identifier-1 in the same order as specified in the INITIALIZE statement. If an implicit
        // INITIALIZE statement results in the execution of a declarative procedure that executes a RESUME
        // statement with the NEXT STATEMENT phrase, processing resumes at the next implicit INITIALIZE statement,
        // if any." The flat list satisfied sentence 1 (source order) and could not express sentence 2: with one
        // statement site, a `-2` resume action fell out past EVERY remaining identifier-1 (kb/Work PB419).
        // BoundImplicitSeries IS the per-implicit-statement boundary, and it collapses to the bare node for the
        // one-operand case the rule's "more than one" premise excludes.
        var members = new List<BoundStatement>();
        foreach (var dref in ini.initializeOperandList().dataReference())
        {
            var actions = new List<InitializeAction>();
            BindInitializeTarget(dref, spec, actions);   // GR3 — per identifier-1, in source order
            members.Add(new BoundInitialize(actions));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary>The ALLOCATE based-item INITIALIZED lowering (ISO §14.9.3 GR7): "the allocated storage is
    /// initialized as if an INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO DEFAULT statement were
    /// executed" — bind EXACTLY that statement's expansion over the based item (the ONE INITIALIZE mechanism:
    /// WITH FILLER, the bare-ALL TO VALUE, no REPLACING, THEN TO DEFAULT), sequenced by the ALLOCATE bind
    /// AFTER the allocation so every store windows the freshly-addressed cell through the implicit
    /// data-address pointer (GR4a). An unresolvable/exotic shape expands to the loud error action — never a
    /// silent skip.</summary>
    public BoundInitialize BindAllocateInitialized(Core.DataReferenceContext basedRef)
    {
        // ValueCategories empty = ALL (§14.9.20.4 GR2), which is the category-name §14.9.3.4 GR7's quoted
        // statement writes: "INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO DEFAULT".
        var spec = new InitializeSpec(WithFiller: true, ToValue: true, ValueCategories: default,
            Replacements: [], HasReplacing: false, ToDefault: true);
        var actions = new List<InitializeAction>();
        BindInitializeTarget(basedRef, spec, actions);
        return new BoundInitialize(actions);
    }

    /// <summary>Expand one identifier-1 (ISO §14.9.20 GR5): resolve its FULL data reference (qualification +
    /// subscripts — the legacy binder's name-only resolution was a gap, not behavior), then walk its subtree in
    /// definition order (GR8) collecting the per-elementary stores. identifier-1 itself MAY have / sit under a
    /// REDEFINES (GR5a3's exclusion applies only BELOW it).</summary>
    private void BindInitializeTarget(Core.DataReferenceContext dref, in InitializeSpec spec, List<InitializeAction> actions)
    {
        // INITIALIZE of a WHOLE OCCURS DYNAMIC table (ISO §14.9.20 GR10; data-model D9): the whole-table reference
        // does not resolve to an element Place (it is guarded loud), so detect it by name and expand the element's
        // stores under a RUN-TIME loop 1‥Capacity (RefReceiving within bounds does not grow). The stores are the
        // INITIALIZE statement's own (category defaults / REPLACING / VALUE-phrase) — NOT the OCCURS grow-seed.
        if (dref.dataReferenceSuffix().Length == 0
            && ctx.Symbols.TryResolve(dref.cobolWord()?.GetText() ?? dref.GetText(), ctx.ActiveScope, out var dyns)
            && dyns.FirstOrDefault(i => i.IsDynamicTable) is { } dtbl && ctx.Refs.TablePath(dtbl) is { } dtp)
        {
            string v = $"__ini{_initializeLoopVar++}";
            var body = new List<InitializeAction>();
            var tblPath = ReferenceResolver.BuildTablePath(dtbl)!;
            // identifier-1 carries no subscript on this arm (the guard above), so the ONE dimension the expansion
            // enters is the whole occurrence key a Format-2 VALUE on the element is looked up by.
            ExpandInitialize(new InitializeDynCursor(tblPath.Add(new DynTableSegment(v)), dtbl),
                spec, body, dtbl, [new OccurrenceDim(v, null)], identifier1: true);
            if (body.Count > 0 && DynCapacity(dtbl, tblPath) is { } cap) actions.Add(new InitializeLoop(v, cap, body));
            else if (body.Count > 0) actions.Add(new InitializeErrorAction(
                $"INITIALIZE of the dynamic-capacity table '{dtbl.CobolName ?? dtp}' (no capacity register)"));
            return;
        }
        // ⛔ identifier-1 RESOLVES THROUGH THE ONE RECEIVING CHOKEPOINT, BECAUSE §14.9.20.3 SR7 SAYS IT IS THE
        // RECEIVING OPERAND (kb/Work PB416). The statement used to call ctx.Refs.Resolve directly, which answers
        // "where does this reference live" and nothing else, so every receiving-operand PROHIBITION the compiler
        // owns was invisible here: a CONSTANT RECORD was silently overwritten at run time (§13.18.15.3 SR2 /
        // COBOLNET1548 — whose own doc claimed INITIALIZE as a caller), and the same held for a constant-name,
        // LINE-COUNTER, PAGE-COUNTER and the OCCURS DYNAMIC CAPACITY register. SR7 is not one check but a FUNNEL:
        // the standard's receiving-operand prohibitions are a growing set, and routing through ResolveReceiving
        // is what makes the NEXT one automatic instead of a second copy bolted onto this verb.
        // A null return is already diagnosed there (an undefined name by the resolver, a resolved-but-unsupported
        // receiver shape as COBOLNET0899), so the old run-time InitializeErrorAction staging is gone with it —
        // §4.2.2 puts a syntax-rule violation in the compile-time mechanism.
        if (host.Expr.ResolveReceiving(dref) is not { } place) return;
        // ISO §14.9.20.3 SR5: "The data description entry for the data item referenced by identifier-1 shall not
        // contain a RENAMES clause." ⛔ ON THE RESOLVED PATH, WHICH IS THE ONLY ARM THAT CAN REACH IT: the check
        // used to sit in the resolve-FAILURE arm above, and a level-66 entry RESOLVES — so the rule never fired
        // and the violation shipped as an unhandled NotImplementedCobolFeatureException naming a missing
        // compiler feature instead of the reader's illegal program (kb/Work PB416).
        if (!ctx.Validation.CheckInitializeTargetRenames(dref.GetText(), place.Item)) return;
        // ISO §14.9.20.3 SR1 — the class screen, asked of the ONE §8.5.2.1 Table-2 classifier. See
        // DiagnosticCatalog.InitializeTargetClass for why this cannot be folded into GR5a1's exclusion below.
        if (IntrinsicArgumentRules.ClassOfPlace(place) is CobolClass.Index)
        {
            ctx.Edition.Error(DiagnosticCatalog.InitializeTargetClass,
                $"INITIALIZE '{dref.GetText()}' — identifier-1 is of class index, the one class ISO "
                + "§14.9.20.3 SR1's list excludes (it admits a strongly-typed item and classes alphabetic, "
                + "alphanumeric, boolean, message-tag, national, numeric, object and pointer); only SET, SEARCH, "
                + "a relation condition and the argument positions of §13.18.60.3 SR10 may reference an index "
                + "data item");
            return;
        }
        if (place is RefModPlace)
        {
            // A reference-modified identifier-1 is the single receiver, of category alphanumeric (ISO §8.4.3.3.4 GR6 —
            // a reference-modifier defines a unique alphanumeric data item); no VALUE clause attaches to it, so
            // GR5c1b and GR5c1c are both false and GR5c1a does not name alphanumeric.
            var rq = Qualify(InitializeCategory.Alphanumeric, effectiveValue: null, spec);
            if (SenderFor(rq, InitializeCategory.Alphanumeric, effectiveValue: null, spec) is { } src)
                actions.Add(new InitializeStore(place, src));
            return;
        }
        // ⛔ THE SWITCH IS OVER THE STORAGE FORM, SO IT ASKS THE UNDECORATED PLACE (kb/Work PB393). identifier-1
        // is a RECEIVING operand (§14.9.20.3 SR7), and none of the decorations change where its members live:
        // an OdoGroupPlace answers §13.18.38.4 GR8 about the group's EXTENT — which GR8b already fixes at the
        // maximum for a receiving operand, and which the child walk below applies per-table for GR8a — and a
        // bit/national group's image view is a §14.9.20.4 GR1 non-question ("identifier-1 is processed as a
        // group item"). Switching on the decorated place sent every one of them to the default arm, which
        // emitted a loud that aborted the run unit on plain COBOL-85 source.
        InitializeCursor? cursor = place.Undecorated switch
        {
            MemberPlace mp => new InitializeMemberCursor(mp.Path, mp.MemberItem),
            // The cell path rides along so a pointer-class receiver inside the class reaches the area's managed
            // slots (kb/Work PB231) — ALLOCATE … INITIALIZED lowers to exactly this expansion (§14.9.3.4 GR7).
            RedefViewPlace rv => new InitializeViewCursor(rv.Backing, rv.OffsetExpr, rv.ViewItem.ClassOffset, rv.ViewItem, "",
                Cell: ReferenceResolver.BuildCellPath(rv.ViewItem.Class)),
            DynTablePlace dp => new InitializeDynCursor(dp.Path, dp.Item),
            _ => null,
        };
        if (cursor is null)
        {
            actions.Add(new InitializeErrorAction($"INITIALIZE target '{dref.GetText()}' (unsupported place kind)"));
            return;
        }
        ExpandInitialize(cursor, spec, actions, place.Item, SeedOccurrenceKey(place), identifier1: true);
    }

    /// <summary>The occurrence key identifier-1's OWN reference already pins (ISO §13.18.63.3 SR20 keys a Format-2
    /// VALUE by "one subscript … for each OCCURS clause for the subject of the entry or superordinate to that
    /// entry, specified in the same order as a subscripted reference to the subject of the entry would be
    /// specified" — which IS the order the resolved access path lays its table segments down in). The expansion
    /// appends one dimension per loop it enters BELOW this; together they are the full tuple
    /// <see cref="TableValuePlan"/> is looked up by.
    /// <para>An integer-literal subscript resolves at bind time and needs no emitted test; ANY OTHER subscript
    /// expression is carried as the dimension's run-time index and is tested exactly like a loop variable — it is
    /// the same one-based occurrence number, written in the same scope, and it is already the text the resolved
    /// place splices into its own subscript position. A storage form with no access path (the Tier-B window
    /// cursor) yields the empty key, which is a length mismatch against any non-empty plan and is the ONE shape
    /// <see cref="ExpandTableValue"/> has to fail loud on.</para></summary>
    private static IReadOnlyList<OccurrenceDim> SeedOccurrenceKey(Place place)
    {
        // ⛔ THE SWITCH ASKS THE UNDECORATED PLACE, for the same reason BindInitializeTarget's does (kb/Work
        // PB393): a reference modifier, an ODO extent or a group-image view decorates WHERE the members are
        // READ, never where they LIVE, and a decorated place sent to a catch-all arm loses the subscripts
        // identifier-1 actually wrote.
        AccessPath? path = place.Undecorated switch
        {
            MemberPlace mp => mp.Path,
            DynTablePlace dp => dp.Path,
            _ => null,
        };
        if (path is null) return [];
        var key = new List<OccurrenceDim>();
        foreach (var seg in path.Segments)
        {
            string? ix = seg switch
            {
                FixedTableSegment f => f.OneBasedIndex,
                DynTableSegment d => d.OneBasedIndex,
                _ => null,
            };
            if (ix is null) continue;
            key.Add(int.TryParse(ix.Trim(), out int n) ? new OccurrenceDim(null, n) : new OccurrenceDim(ix, null));
        }
        return key;
    }

    /// <summary>The <see cref="AllCount"/> a subordinate table's per-occurrence loop is bounded by — ISO
    /// §14.9.20.4 GR8's "the number of occurrences initialized is determined by the rules of the OCCURS clause
    /// for a RECEIVING data item", resolved against §13.18.38.4 (kb/Work PB393):
    /// <list type="bullet">
    ///   <item>Format 1 — GR4's fixed count.</item>
    ///   <item>Format 2 with data-name-1 OUTSIDE identifier-1's group — GR8a: "only that part of the table area
    ///     that is specified by the value of the data item referenced by data-name-1 at the start of the
    ///     operation will be used", i.e. the CURRENT count (clamped, EC-BOUND-ODO outside per GR7).</item>
    ///   <item>Format 2 with data-name-1 INSIDE the group — GR8b: "If the group is a receiving operand, the
    ///     maximum length of the group will be used", i.e. integer-2. identifier-1 IS a receiving operand
    ///     (§14.9.20.3 SR7), so the receiving arm is the only reachable one — and it is also the only SAFE one:
    ///     the walk initializes data-name-1 itself in definition order (GR8's own sentence), so a current-count
    ///     bound read afterwards would be reading a zero this very statement had just stored.</item>
    ///   <item>Format 4 — §14.9.20.4 GR10's current capacity.</item>
    /// </list>
    /// <see langword="null"/> when the count cannot be modelled (a dynamic table with no reachable capacity
    /// register, or an unresolvable data-name-1) — the caller stages the named loud.</summary>
    private AllCount? TableCount(DataItem table, DataItem? identifier1, AccessPath? tablePath) =>
        table.IsDynamicTable ? (tablePath is null ? null : DynCapacity(table, tablePath))
        : table.OccursSpec is { DependingName: not null, Depending: { } dep } odo
            && identifier1 is not null && !OdoModel.IsWithin(dep, identifier1)          // GR8a
            ? (ctx.Refs.ResolveItem(dep) is { } depPlace
                ? new AllCount.Odo(depPlace, odo.Min, table.Occurs ?? odo.Max) : null)
        : table.Occurs is { } n ? new AllCount.Fixed(n)                                  // GR4 / GR8b
        : null;

    /// <summary>A dynamic-capacity table's current-capacity count (ISO §13.18.38.4 GR15 — the register is minted
    /// for every Format-4 table whether or not CAPACITY IN names it).</summary>
    private static AllCount? DynCapacity(DataItem table, AccessPath tablePath) =>
        table.OccursSpec?.CapacityRegister is { } reg
            ? new AllCount.Capacity(new CapacityRegisterPlace(tablePath, reg)) : null;

    /// <summary>The recursive receiver walk (ISO §14.9.20 GR5), in definition order (GR8). Exclusions: GR5a2 —
    /// an explicit-or-implicit FILLER elementary item (a null <see cref="DataItem.CobolName"/>) unless WITH FILLER
    /// (identifier-1 itself is always named, so the test applies only to contained items); GR5a3 — a subordinate
    /// whose entry has REDEFINES, and with it its whole subtree (level-66 entries are not storage children,
    /// §13.18.45); GR5a1 — items that are not valid MOVE receivers (an index data item, §14.9.25.3 SR). A child
    /// with an OCCURS clause expands one loop per dimension (GR5b2 — every occurrence).</summary>
    private void ExpandInitialize(InitializeCursor cur, in InitializeSpec spec, List<InitializeAction> actions,
        DataItem? identifier1Item, IReadOnlyList<OccurrenceDim> key, bool identifier1 = false)
    {
        DataItem item = cur.Item;
        if (item.IsElementary)
        {
            if (!identifier1 && item.CobolName is null && !spec.WithFiller) return;            // GR5a2
            if (InitializeItemCategory(item) is not { } cat) return;                            // GR5a1
            // ⛔ GR5c1c MAKES THE RECEIVER'S QUALIFICATION AND SENDER PER-OCCURRENCE, and only a Format-2 (table)
            // VALUE can do that — every other alternative of GR5c answers the same for all occurrences. So the
            // per-occurrence expansion is entered ONLY here, and only under the VALUE phrase; this is the same
            // fast-path guard `ValueInitializer.FieldInit` takes on the declaration lane, keyed on the same fact.
            if (spec.ToValue && item.TableValuePlan is { } plan)
            {
                ExpandTableValue(cur, item, cat, spec, actions, key, plan);
                return;
            }
            if (ElementaryAction(cur, item, cat, spec, item.ValueAt(default)) is { } single) actions.Add(single);
            return;
        }
        if (!item.IsGroup) return;
        foreach (var child in item.Children)
        {
            if (!(child.IsGroup || child.IsElementary)) continue;                               // no storage
            if (child.RedefinesTargetName is not null || child.Renames is not null) continue;   // GR5a3
            if (cur.Child(child) is not { } childCur)
            {
                actions.Add(new InitializeErrorAction(
                    $"INITIALIZE receiver '{child.CobolName ?? "FILLER"}' (unwired REDEFINES storage tier)"));
                continue;
            }
            // ⛔ A DYNAMIC-CAPACITY CHILD IS SPECIFIED, NOT DEFERRED (kb/Work PB393). §14.9.20.4 GR10: "When a
            // group containing a dynamic-capacity table is initialized, all the elements of the table up to
            // current capacity, if any, are initialized, whether or not the INITIALIZED phrase is present in the
            // OCCURS clause, and the current capacity of the table is left unchanged." It is the SAME
            // per-occurrence loop every other dimension takes — only the count differs — so it uses the same
            // InitializeLoop over an AllCount.Capacity, and the element cursor is the dynamic one (its stores go
            // through RefReceiving, which within the bound never grows the table: GR10's "left unchanged").
            // This replaced a staged COBOLNET1527 loud that predated GR10 being read.
            if (child.IsDynamicTable)
            {
                string dv = $"__ini{_initializeLoopVar++}";
                var dbody = new List<InitializeAction>();
                if (childCur.StoragePath is { } dtp)
                {
                    ExpandInitialize(new InitializeDynCursor(dtp.Add(new DynTableSegment(dv)), child),
                        spec, dbody, identifier1Item, [.. key, new OccurrenceDim(dv, null)]);
                    if (dbody.Count > 0 && TableCount(child, identifier1Item, dtp) is { } dcount)
                        actions.Add(new InitializeLoop(dv, dcount, dbody));
                    else if (dbody.Count > 0)
                        actions.Add(new InitializeErrorAction($"INITIALIZE of the group member "
                            + $"'{child.CobolName ?? "FILLER"}' (dynamic-capacity table with no capacity register)"));
                }
                else
                    actions.Add(new InitializeErrorAction($"INITIALIZE of the group member "
                        + $"'{child.CobolName ?? "FILLER"}' (a dynamic-capacity table in a storage form with no "
                        + "table path — ISO §14.9.20.4 GR10)"));
                continue;
            }
            if (child.IsTable)
            {
                string v = $"__ini{_initializeLoopVar++}";
                var body = new List<InitializeAction>();
                ExpandInitialize(childCur.Indexed(v), spec, body, identifier1Item,
                    [.. key, new OccurrenceDim(v, null)]);
                // GR5b2 over the GR8 count — fixed (GR4), current (GR8a) or maximum (GR8b).
                if (body.Count > 0 && TableCount(child, identifier1Item, childCur.StoragePath) is { } count)
                    actions.Add(new InitializeLoop(v, count, body));
                else if (body.Count > 0)
                    actions.Add(new InitializeErrorAction($"INITIALIZE of the table '{child.CobolName ?? "FILLER"}' "
                        + "(its OCCURS DEPENDING ON object is not resolvable — ISO §14.9.20.4 GR8)"));
            }
            else
                ExpandInitialize(childCur, spec, actions, identifier1Item, key);
        }
    }

    // ── GR5c (qualification) and GR6 (sender), which are TWO rules and used to be one method (kb/Work PB418) ────

    /// <summary>ISO §14.9.20.4 GR5c — "each possible receiving-operand is a receiving-operand if at least one of
    /// the following is true", evaluated in the rule's own order so the answer also carries GR6's arm selector.
    /// <paramref name="effectiveValue"/> is the VALUE-clause literal that applies TO THIS OCCURRENCE (
    /// <see cref="DataItem.ValueAt"/> — the Format-1 text, or the Format-2 literal keyed to the occurrence's
    /// subscript tuple), so GR5c1b and GR5c1c are ONE test over the one carrier-agnostic reader rather than two
    /// readers that can disagree.</summary>
    private static InitializeQualification Qualify(InitializeCategory cat, string? effectiveValue,
        in InitializeSpec spec)
    {
        // GR5c1 — "The VALUE phrase is specified, the category of the elementary data item is one of the categories
        // specified or implied in the VALUE phrase, and one of the following is true": GR5c1a (categorical),
        // GR5c1b (a data-item format VALUE clause) or GR5c1c (a table format VALUE clause keyed to this occurrence).
        // ALL implies every category (GR2), which is the EMPTY ValueCategories set; otherwise it is §5.2.6.4
        // membership in the category-name, which may name several categories at once.
        if (spec.ToValue && (spec.ValueCategories.IsEmpty || spec.ValueCategories.Contains(cat))
            && (QualifiesCategorically(cat) || effectiveValue is not null))
            return InitializeQualification.ViaValue;
        // GR5c2 — "The REPLACING phrase is specified and the category of the elementary data item is one of the
        // categories specified in the REPLACING phrase".
        foreach (var (rcats, _) in spec.Replacements)
            if (rcats.Contains(cat)) return InitializeQualification.ViaReplacing;
        // GR5c3 — "The DEFAULT phrase is specified"; GR5c4 — "Neither the REPLACING phrase nor the VALUE phrase is
        // specified" (the bare COBOL-85 form, which defaults everything GR5a did not exclude).
        if (spec.ToDefault || (!spec.ToValue && !spec.HasReplacing)) return InitializeQualification.ViaDefault;
        return InitializeQualification.None;
    }

    /// <summary>ISO §14.9.20.4 GR5c1a — "Either the category of the elementary data item is data-pointer,
    /// message-tag, object-reference, or program-pointer". These categories are receiving-operands under the VALUE
    /// phrase UNCONDITIONALLY, and the rule exists because they can never satisfy GR5c1b or GR5c1c: §13.18.63.3 SR9
    /// forbids a VALUE clause on FUNCTION-POINTER / MESSAGE-TAG / OBJECT-REFERENCE / PROGRAM-POINTER outright, and
    /// for a plain USAGE POINTER the only writable value, the predefined address NULL, may appear per §8.4.3.10.3
    /// "only as a sending operand in an INITIALIZE or a SET statement", in an argument, or in a relation condition —
    /// never in a VALUE clause. Reading GR5c1b as the whole premise therefore left this arm 100% dead, and
    /// <c>INITIALIZE p ALL TO VALUE</c> silently did nothing to a pointer (kb/Work PB418).
    /// <para>⛔ FUNCTION-POINTER IS DELIBERATELY ABSENT, and that is the PRINTED standard, not a transcription
    /// loss: GR4's SET-form list, GR6a1's sender list and GR6c's fill table all name function-pointer, and GR5c1a
    /// alone does not — verified against the licensed PDF (§14.9.20.4 GR5c1a, printed folio 639). A FUNCTION-POINTER
    /// item is consequently NOT a receiving operand under the VALUE phrase alone (SR9 denies it GR5c1b and GR5c1c
    /// too); it is still one under REPLACING (GR5c2), DEFAULT (GR5c3) and the bare form (GR5c4), where GR6c gives
    /// it the predefined address NULL. Do not "even up" this list — the rule is quoted, not paraphrased.</para>
    /// <para>MESSAGE-TAG is in the rule and is named here for fidelity, but no elementary item can reach this test
    /// with that category: no <see cref="PicCategory"/> carries message-tag (the owner-declined MCS facility,
    /// docs/CONFORMANCE.md §4), so the DATA DIVISION refuses such an item before any statement sees it.</para></summary>
    private static bool QualifiesCategorically(InitializeCategory cat) =>
        cat is InitializeCategory.DataPointer or InitializeCategory.MessageTag
            or InitializeCategory.ObjectReference or InitializeCategory.ProgramPointer;

    /// <summary>ISO §14.9.20.4 GR6 — "the sending-operand in each implicit MOVE and SET statement", whose three
    /// arms are keyed on WHY the item qualified, not on which phrases were written: GR6a "if the data item
    /// qualifies as a receiving-operand because of the VALUE phrase", GR6b "if the data item does not qualify …
    /// because of the VALUE phrase, but does qualify because of the REPLACING phrase", GR6c "if the data item does
    /// not qualify in accordance with General rules 6a and 6b". Null when GR5c left the item unchanged, and for
    /// GR6a1/GR6a2 whose sending-operand is the predefined NULL the SET arm materializes.</summary>
    private static BoundOperand? SenderFor(InitializeQualification q, InitializeCategory cat, string? effectiveValue,
        in InitializeSpec spec) => q switch
    {
        // GR6a3 — "the sending-operand is determined by the literal in the VALUE clause specified in the data
        // description entry of the data item. If the data item is a table element, the literal in the VALUE clause
        // that corresponds to the occurrence being initialized determines the sending-operand." Both carriers
        // arrive here already resolved to that occurrence's literal. A null value means the item qualified through
        // GR5c1a, whose senders are GR6a1/GR6a2's predefined NULL — an InitializeSetNull, not an operand.
        InitializeQualification.ViaValue => effectiveValue is { } raw ? InitializeValueOperand(raw) : null,
        // GR6b — "the literal-1 or identifier-2 associated with the category specified in the REPLACING phrase".
        InitializeQualification.ViaReplacing => ReplacementFor(cat, spec),
        // GR6c fill table: ZEROES for numeric/numeric-edited AND boolean (boolean zeros — the figurative
        // materializes '0' fill against the boolean receiver); SPACES for the character categories, which
        // the table lists row by row — alphabetic, alphanumeric, alphanumeric-edited, national and
        // national-edited, the last two "Figurative constant national SPACES" (national spaces under the
        // D-N4 Latin-1 identity). The pointer/object-reference rows of the same table are the predefined NULLs,
        // reached through the SET arm rather than an operand.
        InitializeQualification.ViaDefault =>
            cat is InitializeCategory.Numeric or InitializeCategory.NumericEdited or InitializeCategory.Boolean
                ? new BoundFigurative('Z')
                : new BoundFigurative('S'),
        _ => null,
    };

    /// <summary>The REPLACING operand named for <paramref name="cat"/> (ISO §14.9.20.4 GR6b — "the literal-1 or
    /// identifier-2 associated with the category specified in the REPLACING phrase"); null is unreachable once
    /// <see cref="Qualify"/> has answered <c>ViaReplacing</c>, which it does only on a match. The scan is over
    /// category-name SETS, and §14.9.20.3 SR6 makes at most one of them contain <paramref name="cat"/>.</summary>
    private static BoundOperand? ReplacementFor(InitializeCategory cat, in InitializeSpec spec)
    {
        foreach (var (rcats, value) in spec.Replacements)
            if (rcats.Contains(cat)) return value;
        return null;
    }

    /// <summary>ONE elementary receiver's action under ISO §14.9.20.4 GR4/GR5c/GR6 at ONE occurrence, or null when
    /// GR5c leaves it unchanged. <paramref name="effectiveValue"/> is the VALUE-clause literal applying to that
    /// occurrence (<see cref="DataItem.ValueAt"/>).</summary>
    private static InitializeAction? ElementaryAction(InitializeCursor cur, DataItem item, InitializeCategory cat,
        in InitializeSpec spec, string? effectiveValue)
    {
        var q = Qualify(cat, effectiveValue, spec);
        if (q is InitializeQualification.None) return null;                                     // GR5c — left unchanged
        // §14.9.20.4 GR4: "if the category of a receiving-operand is data-pointer, function-pointer, message-tag,
        // object-reference, or program-pointer, the implicit statement is SET receiving-operand TO sending-operand"
        // — NOT a MOVE, so it must not route through the InitializeStore MOVE path (GR5a1 keeps these as receiving
        // operands, not MOVE-receiver-excluded). (CODE-SPEC-AUDIT CA2.)
        if (InitializeCategories.IsSetForm(cat))
            // GR6a1/GR6a2 and every pointer row of GR6c's table give the predefined NULL. GR6b does NOT: under
            // REPLACING the sending operand is identifier-2, an implicit `SET receiver TO identifier-2`, and
            // writing NULL there would be a wrong answer rather than a missing one. kb/Work PB415 made the five
            // category-names spellable, so the arm PB418 staged LOUD is now the real SET. ⛔ THE ERROR ARM IS A
            // "CANNOT HAPPEN" GUARD, NOT A STAGED FEATURE: §14.9.20.3 SR3 (COBOLNET1982) has already refused
            // literal-1 for these categories, and SR4's category agreement (COBOLNET1983) refuses every sender
            // that is not a data item of the named category — a function-identifier among them, since no SET
            // format admits one (§14.9.39). It stays because `EmitAction`'s switch has no default arm, so a
            // non-place operand reaching the emitter would be dropped in silence rather than diagnosed.
            return q is InitializeQualification.ViaReplacing
                ? ReplacementFor(cat, spec) is BoundFieldOperand { Place: var sp }
                    ? new InitializeSetFrom(cur.ToPlace(), sp)                                  // GR4 + GR6b
                    : new InitializeErrorAction($"INITIALIZE REPLACING {cat} … BY a function-identifier into "
                        + $"'{item.CobolName ?? "FILLER"}' (ISO §14.9.20.4 GR4 makes the implicit statement a SET, "
                        + "and §14.9.39 admits no function-identifier as a SET sending operand)")
                : new InitializeSetNull(cur.ToPlace());                                         // GR4 SET … TO the predefined NULL
        if (SenderFor(q, cat, effectiveValue, spec) is not { } source) return null;
        // §14.9.20.4 GR7: "when a dynamic-length elementary item is initialized, its length is set to zero"
        // (overrides the GR6c figurative SPACE fill — an empty sender flows through the same dynamic-length store
        // to length 0; §8.3.3.6.4 GR3 would otherwise leave it at length 1). (CODE-SPEC-AUDIT CA1.)
        // ⚠ GR7 IS WRITTEN UNCONDITIONALLY AND THE VALUE / REPLACING ARMS OF THAT READING ARE UNADJUDICATED: GR6a3
        // requires a sender that "produces the same result as the initial value of the data item as produced by the
        // application of the VALUE clause", and §8.6.4 makes that a NON-ZERO length for a dynamic-length item that
        // carries a VALUE clause, so an unconditional GR7 contradicts them both. The behaviour here is unchanged
        // from before the GR5c/GR6 split and is pinned only on the category-default branch
        // (conformance:2014/initialize_dynamic_length); the conflict is an OPEN OWNER DETERMINATION recorded on
        // kb/Work PB418, and the row GR-14.9.20.4-7 stays PARTIAL until it is answered.
        if (item.IsDynamicLength) source = new BoundStringLiteral("");
        return new InitializeStore(cur.ToPlace(), source);
    }

    /// <summary>⛔ THE Format-2 (table) VALUE ARM OF GR5c/GR6, WHICH IS THE ONLY PER-OCCURRENCE ONE (ISO §14.9.20.4
    /// GR5c1c + GR6a3). The receiver's qualification and its sending-operand both depend on the occurrence, so one
    /// bind-time action cannot carry the whole element: this composes an
    /// <see cref="InitializeOccurrenceSelect"/> — one arm per DISTINCT literal (the occurrences sharing it
    /// coalesced) plus the fall-through for every occurrence the clause does not key, which GR5c re-qualifies from
    /// scratch (REPLACING, DEFAULT, or nothing).
    /// <para>The per-occurrence literal comes from <see cref="DataItem.ValueAt"/> through
    /// <see cref="TableValuePlan.LiteralAt"/> — the SAME reader <c>ValueInitializer</c> and <c>GroupImageCodec</c>
    /// use for the declaration lane, so a table element's INITIALIZE-restored value and its initial value cannot
    /// disagree. Before this arm existed the lane read only <see cref="DataItem.RawValue"/>, the Format-1 carrier,
    /// and a Format-2 element therefore failed GR5c1 entirely: <c>ALL TO VALUE</c> emitted no store at all, and
    /// <c>ALL TO VALUE THEN TO DEFAULT</c> qualified it through GR5c3 and wrote SPACES over the values the
    /// statement exists to restore (kb/Work PB418; the same root kb/Work PB499 records from the VALUE-clause side).</para>
    /// <para>Every dimension of the plan has to be identified: the ones the expansion loops over by their loop
    /// variable, the ones identifier-1 pinned by its own subscript (an integer literal folds at bind time, any
    /// other expression is tested at run time). A key SHORTER than the plan's dimension list is the one shape
    /// nothing can answer — a storage form that carries no access path — and it is staged LOUD rather than
    /// resolved to an arbitrary literal, because §14.9.20.4 GR6a3 names ONE literal per occurrence and a lane
    /// that cannot identify the occurrence has no honest answer.</para></summary>
    private static void ExpandTableValue(InitializeCursor cur, DataItem item, InitializeCategory cat,
        in InitializeSpec spec, List<InitializeAction> actions, IReadOnlyList<OccurrenceDim> key, TableValuePlan plan)
    {
        // The plan is keyed by the subject's FULL OCCURS chain in §13.18.63.3 SR20's order, so the key the walk
        // carries has to be the same length and every dimension has to be identified.
        bool keyed = key.Count == plan.Dims.Count;
        if (keyed)
            foreach (var d in key)
                if (d.Var is null && d.Fixed is null) { keyed = false; break; }
        if (!keyed)
        {
            actions.Add(new InitializeErrorAction($"INITIALIZE … TO VALUE of '{item.CobolName ?? "FILLER"}', whose "
                + "table-format VALUE clause is keyed to occurrences identifier-1 does not pin at compile time "
                + "(ISO §14.9.20.4 GR5c1c/GR6a3)"));
            return;
        }

        // The dimensions identifier-1 PINNED select a bind-time slice of the plan; the ones the expansion loops
        // over stay as run-time tests, in the same most-inclusive-first order the arms' tuples are written in.
        var vars = new List<string>();
        var varAt = new List<int>();
        for (int i = 0; i < key.Count; i++)
            if (key[i].Var is { } v) { vars.Add(v); varAt.Add(i); }

        // Deterministic codegen: the plan's map has no defined enumeration order, so walk its tuples in odometer
        // order (Subscripts.Compare — §13.18.63.4 GR12's own fill order) before grouping.
        var tuples = plan.Literals.Keys.ToList();
        tuples.Sort(Subscripts.Compare);

        var order = new List<string>();
        var byLiteral = new Dictionary<string, List<Subscripts>>(StringComparer.Ordinal);
        foreach (var subs in tuples)
        {
            if (subs.Count != key.Count) continue;                       // not this plan's shape — LiteralAt agrees
            bool inSlice = true;
            for (int i = 0; i < key.Count; i++)
                if (key[i].Fixed is { } f && subs[i] != f) { inSlice = false; break; }
            if (!inSlice) continue;
            string lit = plan.Literals[subs];
            if (!byLiteral.TryGetValue(lit, out var group)) { byLiteral[lit] = group = []; order.Add(lit); }
            var proj = new int[vars.Count];
            for (int j = 0; j < vars.Count; j++) proj[j] = subs[varAt[j]];
            group.Add(new Subscripts(proj));
        }

        // GR5c re-asked for every occurrence the clause does NOT key: GR5c1c is false there, so the item falls to
        // GR5c2/c3/c4 exactly as if it carried no VALUE clause at all.
        var otherwise = ElementaryAction(cur, item, cat, spec, effectiveValue: null);

        // Every dimension pinned at bind time — the occurrence is known, so there is no test to emit.
        if (vars.Count == 0)
        {
            var only = order.Count > 0 ? ElementaryAction(cur, item, cat, spec, order[0]) : otherwise;
            if (only is not null) actions.Add(only);
            return;
        }
        var arms = new List<InitializeOccurrenceArm>();
        foreach (string lit in order)
            if (ElementaryAction(cur, item, cat, spec, lit) is { } act)
                arms.Add(new InitializeOccurrenceArm(byLiteral[lit], act));
        if (arms.Count == 0)
        {
            if (otherwise is not null) actions.Add(otherwise);
            return;
        }
        actions.Add(new InitializeOccurrenceSelect(vars, arms, otherwise));
    }

    /// <summary>The INITIALIZE category of an elementary item (ISO §8.5.2 via §14.9.20.2 category-name), or null
    /// when the item is excluded (GR5a1 — an index data item is not a valid MOVE receiver; only SET stores it,
    /// §14.9.39 GR2b). Alphanumeric-edited = an X/A picture with insertion symbols; alphabetic = an all-A picture
    /// (the COBOL-85 alphabetic-edited category folds into alphanumeric-edited per the 2023 categorization).</summary>
    private static InitializeCategory? InitializeItemCategory(DataItem item) => item.Pic switch
    {
        { Usage: Usage.Index } => null,
        { Category: PicCategory.Numeric } => InitializeCategory.Numeric,
        { Category: PicCategory.NumericEdited } => InitializeCategory.NumericEdited,
        { Category: PicCategory.Alphanumeric, EditMask: not null } => InitializeCategory.AlphanumericEdited,
        { Category: PicCategory.Alphanumeric, IsAlphabetic: true } => InitializeCategory.Alphabetic,
        { Category: PicCategory.Alphanumeric } => InitializeCategory.Alphanumeric,
        { Category: PicCategory.Boolean } => InitializeCategory.Boolean,     // GR6c: boolean → ZEROES
        // §8.5.2.11 national-edited is its OWN category, and the `EditMask: not null` pair is what the model
        // carries it as (the same shape alphanumeric-edited uses, one arm above). GR6c fills it with "Figurative
        // constant national SPACES" — the same fill as national — but GR5c's category-name MATCH must tell the
        // two apart, which a folded arm could not (kb/Work PB492).
        { Category: PicCategory.National, EditMask: not null } => InitializeCategory.NationalEdited,
        { Category: PicCategory.National } => InitializeCategory.National,   // GR6c: national → SPACES
        // GR4/GR6c: pointer & object-reference receivers are initialized by an implicit SET … TO the predefined
        // NULL (data-pointer/program-pointer → NULL address, object-reference → NULL reference), NOT a MOVE.
        { Category: PicCategory.Pointer } => InitializeCategory.DataPointer,
        { Category: PicCategory.ProgramPointer } => InitializeCategory.ProgramPointer,
        { Category: PicCategory.FunctionPointer } => InitializeCategory.FunctionPointer,
        { Category: PicCategory.ObjectReference } => InitializeCategory.ObjectReference,
        _ => null,
    };

    /// <summary>ISO §14.9.20.3 SR4 for the SET-form categories — "for each of the categories data-pointer,
    /// function-pointer, message-tag, object-reference, and program-pointer specified in the REPLACING phrase, a
    /// SET statement with identifier-2 as the sending operand and an item of the specified category as the
    /// receiving operand shall be valid". §14.9.39's pointer and object-reference formats admit only a sending
    /// operand of the receiver's own category, so category agreement is the NECESSARY condition, checked here
    /// (an object-reference pair's §9.3.8.2 class conformance is the OO lane's, and is not re-stated).
    /// <para>It is checked at BIND time rather than left to the backend because <see cref="InitializeSetFrom"/>
    /// renders the SET statement's own straight copy: a mismatched sender would otherwise reach Roslyn as a
    /// CS-level type error on emitted code, which names no COBOL rule.</para></summary>
    private void CheckSetFormCategoryAgreement(InitializeCategorySet cats, BoundOperand value, string senderText)
    {
        foreach (var cat in InitializeCategories.All)
        {
            if (!cats.Contains(cat) || !InitializeCategories.IsSetForm(cat)) continue;
            var senderCat = value is BoundFieldOperand { Place.Item: { } si } ? InitializeItemCategory(si) : null;
            if (senderCat != cat)
                ctx.Validation.CheckInitializeReplacingSetCategoryAgrees(cat, senderCat, senderText);
        }
    }

    /// <summary>⛔ ISO §14.9.20.3 SR4's MOVE HALF — "For each of the other categories specified in the REPLACING
    /// phrase, a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the specified
    /// category as the receiving operand shall be valid" — the twin of
    /// <see cref="CheckSetFormCategoryAgreement"/>, which answers SR4's FIRST paragraph for GR4's five SET-form
    /// categories. §14.9.20.4 GR4's "as though a series of implicit MOVE or SET statements" is what makes a rule
    /// about MOVE a rule about INITIALIZE.
    /// <para>The question is asked of <see cref="MoveTable16"/>, the SAME screen <c>MoveBinder</c> asks, so the
    /// implicit MOVE and the explicit one the programmer could write instead cannot answer differently. Before
    /// this, <c>MoveTable16</c> had four call sites and INITIALIZE was none of them while
    /// <c>InitializeEmitter</c> synthesised its <c>BoundMove</c> directly — so every cell an explicit MOVE
    /// refuses, <c>INITIALIZE … REPLACING</c> accepted and STORED: a <c>PIC A(4)</c> item ended up holding
    /// <c>0000</c>, which no conforming program can produce and a later class-ALPHABETIC test then reads as
    /// false (kb/Work PB416).</para>
    /// <para>ONE message per REPLACING item, naming the first offending category — the same posture
    /// §14.9.20.3 SR3's check takes, since one item's category-name may be a SET (§5.2.6.4) and a reader fixing
    /// the phrase fixes it once.</para>
    /// <para>⚠ SR4 IS ASKED OF EVERY RULE THAT CAN BE ANSWERED FROM (sender, receiver CATEGORY), AND ONE MOVE
    /// RULE CANNOT BE: §14.9.25.3 SR5's figurative→numeric prohibition, whose three EDITION rows live in
    /// <c>VersionConformancePass.GateMove</c> and are re-derived from a bound MOVE node this statement never
    /// builds. <c>INITIALIZE g REPLACING NUMERIC DATA BY SPACE</c> is therefore still un-gated at 2023 where the
    /// explicit MOVE is COBOLNET0902 — recorded on kb/Work PB416's report as the version lane's own row, not
    /// papered over here with a second, edition-blind copy of the rule.</para></summary>
    private void CheckReplacingMoveValidity(InitializeCategorySet cats, BoundOperand value, string senderText)
    {
        // ⛔ SR4'S SECOND PARAGRAPH IS WHAT THIS METHOD IMPLEMENTS, AND IT SAYS "the OTHER categories"
        // (kb/Work PB423). The MOVE question is not asked at all for the five categories SR4's FIRST paragraph
        // routes to a SET — data-pointer, function-pointer, message-tag, object-reference and program-pointer —
        // so a REPLACING phrase naming ONLY those has no implicit MOVE to be valid. The per-category loop below
        // already honoured that (Table16Receiver answers null for them); the SR1 CLASS screen did not, because
        // it was hoisted above the loop as "asked once" back when the only class it could refuse was INDEX, and
        // INDEX is not one of INITIALIZE's thirteen category-names. The moment SR1's screen gained its pointer
        // and object arms, `INITIALIZE g REPLACING DATA-POINTER DATA BY PTR` — the exact shape SR4's first
        // paragraph and §14.9.20.4 GR4 describe, and which conformance:2002/pb415_initialize_replacing_set_form
        // pins — was refused as an invalid MOVE. The hoist stays (one message per REPLACING item, §5.2.6.4's
        // category-name is a SET), guarded by the paragraph that owns it.
        bool anyMoveFormCategory = false;
        foreach (var cat in InitializeCategories.All)
            if (cats.Contains(cat) && !InitializeCategories.IsSetForm(cat)) { anyMoveFormCategory = true; break; }
        if (anyMoveFormCategory && MoveTable16.SenderClassRefusal(value) is { } classRefusal)
        {
            ctx.Edition.Error(DiagnosticCatalog.InitializeReplacingMoveInvalid,
                $"INITIALIZE REPLACING … BY {senderText}: ISO §14.9.20.3 SR4 requires the implicit MOVE to be "
                + $"valid, and {classRefusal}");
            return;
        }
        var senderPos = MoveTable16.SenderPosition(value);
        foreach (var cat in InitializeCategories.All)
        {
            if (!cats.Contains(cat) || InitializeCategories.Table16Receiver(cat) is not { } recvPos) continue;
            string? refusal = MoveTable16.ShapeRefusal(value, recvPos) ?? MoveTable16.Refusal(senderPos, recvPos);
            if (refusal is null) continue;
            ctx.Edition.Error(DiagnosticCatalog.InitializeReplacingMoveInvalid,
                $"INITIALIZE REPLACING {InitializeCategories.Spelling(cat)} … BY {senderText}: ISO §14.9.20.3 SR4 "
                + $"requires that `MOVE {senderText} TO <an item of category "
                + $"{InitializeCategories.Spelling(cat)}>` be valid, and {refusal}");
            return;   // one message per REPLACING item, naming the first offending category
        }
    }

    /// <summary>Decode ONE grammar <c>category-name</c> — the §5.2.6.4 SET of category words, accumulated left to
    /// right. <paramref name="namedSoFar"/> is the running union that §14.9.20.3 SR6 ("the same category shall not
    /// be repeated in a REPLACING phrase") and §5.2.6.4 ("any single alternative shall be specified only once")
    /// BOTH test against — one accumulator for one rule, so a word repeated within a category-name and a category
    /// repeated across REPLACING items are diagnosed identically. A repeated word is dropped, never doubled.</summary>
    private InitializeCategorySet CategorySetOf(Core.InitializeCategoryContext cat, ref InitializeCategorySet namedSoFar)
    {
        var set = default(InitializeCategorySet);
        foreach (var name in cat.initializeCategoryName())
        {
            var one = InitializeCategoryOf(name);
            if (!ctx.Validation.CheckInitializeCategoryUnique(namedSoFar, one)) continue;
            set = set.With(one);
            namedSoFar = namedSoFar.With(one);
        }
        return set;
    }

    /// <summary>The VALUE phrase's category-name (ISO §14.9.20.2). Its words are governed by §5.2.6.4's "only
    /// once" too, but by nothing else — SR6 is written about the REPLACING phrase alone — so the accumulator is
    /// local to this one category-name.</summary>
    private InitializeCategorySet CategorySetOf(Core.InitializeCategoryContext cat)
    {
        var namedSoFar = default(InitializeCategorySet);
        return CategorySetOf(cat, ref namedSoFar);
    }

    /// <summary>Decode ONE printed category-name word (ISO §14.9.20.2 "where category-name is:"). Every one of
    /// the thirteen is a required word with one hyphenated spelling, so this is a total token switch — the
    /// two-token <c>ALPHANUMERIC EDITED</c> / <c>NUMERIC EDITED</c> forms the rule used to carry are in no
    /// edition of the standard and are gone with the EDITED lexer token (kb/Work PB415).</summary>
    private static InitializeCategory InitializeCategoryOf(Core.InitializeCategoryNameContext cat) =>
        cat.ALPHABETIC() is not null ? InitializeCategory.Alphabetic
        : cat.ALPHANUMERIC_EDITED() is not null ? InitializeCategory.AlphanumericEdited
        : cat.ALPHANUMERIC() is not null ? InitializeCategory.Alphanumeric
        : cat.BOOLEAN() is not null ? InitializeCategory.Boolean
        : cat.DATA_POINTER() is not null ? InitializeCategory.DataPointer
        : cat.FUNCTION_POINTER() is not null ? InitializeCategory.FunctionPointer
        : cat.MESSAGE_TAG() is not null ? InitializeCategory.MessageTag
        : cat.NATIONAL_EDITED() is not null ? InitializeCategory.NationalEdited
        : cat.NATIONAL() is not null ? InitializeCategory.National
        : cat.NUMERIC_EDITED() is not null ? InitializeCategory.NumericEdited
        : cat.NUMERIC() is not null ? InitializeCategory.Numeric
        : cat.OBJECT_REFERENCE() is not null ? InitializeCategory.ObjectReference
        : InitializeCategory.ProgramPointer;

    /// <summary>The bound sending operand for a VALUE-qualified receiver (ISO §14.9.20 GR6a3 — "a literal that,
    /// when moved to the receiving-operand with a MOVE statement, produces the same result as … the VALUE clause"):
    /// the raw VALUE text decodes to the figurative / ALL-literal / string / numeric operand the MOVE path already
    /// renders, so TO VALUE re-produces the program-start state through MOVE semantics.</summary>
    private static BoundOperand InitializeValueOperand(string raw)
    {
        string t = raw.Trim();
        if (InitializeFigurativeKind(t) is { } kind) return new BoundFigurative(kind);
        if (t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && t.Length > 3)
        {
            string rest = t[3..].TrimStart();
            // ALL "literal" / ALL 'literal' repeats to the receiver width (§8.3.3.6.4 GR2); ALL <figurative-word> ≡ the bare word.
            if (CobolLiteral.IsStringLiteral(rest)) return BoundAllLiteral.Of(rest);   // the category rides on literal-1 (PB71)
            if (InitializeFigurativeKind(rest) is { } k) return new BoundFigurative(k);
        }
        // N"…"/B"…" (and the apostrophe forms) VALUE clauses re-produce their category-tagged literal (declaration-time
        // validation — 0898/0900 — already ran in DataBinder; no re-gating here). Test the prefix letter, then the codec.
        if (t.Length >= 3 && t[0] is 'N' or 'n' && CobolLiteral.IsStringLiteral(t))
            return new BoundStringLiteral(CobolLiteral.Decode(t)) { Category = PicCategory.National };
        if (t.Length >= 3 && t[0] is 'B' or 'b' && CobolLiteral.IsStringLiteral(t))
            return new BoundStringLiteral(CobolLiteral.Decode(t)) { Category = PicCategory.Boolean };
        if (CobolLiteral.IsStringLiteral(t)) return new BoundStringLiteral(CobolLiteral.Decode(t));
        return new BoundNumericLiteral(t);
    }

    /// <summary>A figurative-constant word's <see cref="BoundFigurative"/> kind, or null when the text is not a
    /// figurative word (ISO §8.3.3.6.2 — the singular/plural forms are alternatives of one format).</summary>
    private static char? InitializeFigurativeKind(string word) => word.ToUpperInvariant() switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => 'Z',
        "SPACE" or "SPACES" => 'S',
        "HIGH-VALUE" or "HIGH-VALUES" => 'H',
        "LOW-VALUE" or "LOW-VALUES" => 'L',
        "QUOTE" or "QUOTES" => 'Q',
        "NULL" or "NULLS" => 'N',
        _ => null,
    };

    // ── Receiver cursors: extend the RESOLVED identifier-1 place member-by-member ────────────────────────────

    /// <summary>A bind-time lvalue cursor over the receiving subtree: it extends the already-resolved identifier-1
    /// <see cref="Place"/> (so a qualified / subscripted identifier-1 expands from its built access path — no
    /// re-resolution), materializing each elementary receiver as a typed place. <see cref="Child"/> returns null
    /// for a storage form not yet wired (the caller fails loud).</summary>
    private abstract record InitializeCursor(DataItem Item)
    {
        public abstract InitializeCursor? Child(DataItem child);
        public abstract InitializeCursor Indexed(string indexVar);
        public abstract Place ToPlace();

        /// <summary>The cursor's STRUCTURAL access path, or null for a storage form that has none — the Tier-B
        /// window cursor, whose receivers are character windows over one string backing. It is what a
        /// dynamic-capacity child needs (kb/Work PB393): its CAPACITY register and its per-occurrence element
        /// path are both built from the table's own path, and taking it from the cursor rather than from
        /// <c>ReferenceResolver.BuildTablePath</c> is what keeps a SUBSCRIPTED or qualified identifier-1's
        /// indices on the path.</summary>
        public virtual AccessPath? StoragePath => null;
    }

    /// <summary>A plain member-access cursor, mirroring <c>ReferenceResolver.AccessPath</c>: <c>CsName</c> segments
    /// chained with <c>.</c>, each OCCURS level routed through the ref-returning <c>CobolTable.At</c> (benign
    /// subscripting, ISO §8.4.2.3.4 GR2). Entering a Tier-B (string-canonical) REDEFINES class — whose ONE stored
    /// string backing lives in the containing struct (COBOLNET_DESIGN §4.2) — switches to a
    /// <see cref="InitializeViewCursor"/>; an unwired Tier-C / Rejected class yields null (loud).</summary>
    private sealed record InitializeMemberCursor(AccessPath Path, DataItem Item) : InitializeCursor(Item)
    {
        public override InitializeCursor? Child(DataItem child)
        {
            if (child.Class is { } cls)
            {
                if (cls.Tier == RedefinesTier.StringCanonical && child.IsCanonical)
                    return new InitializeViewCursor(Path.Add(new MemberSegment(cls.BackingCsName)),
                        child.ClassOffset.ToString(), child.ClassOffset, child, "",
                        Cell: ReferenceResolver.BuildCellPath(cls));
                if (cls.Tier != RedefinesTier.Alias || !child.IsCanonical) return null;
            }
            return new InitializeMemberCursor(Path.Add(new MemberSegment(child.CsName)), child);
        }

        public override InitializeCursor Indexed(string indexVar) =>
            this with { Path = Path.Add(new FixedTableSegment(indexVar)) };

        public override Place ToPlace() => new MemberPlace(Path, Item);

        public override AccessPath? StoragePath => Path;
    }

    /// <summary>A cursor at one occurrence of an OCCURS DYNAMIC table (data-model D9): entered at the element level
    /// with the two direction-specific accessor paths already applied (<c>{tbl}.RefSending(v)</c> /
    /// <c>{tbl}.RefReceiving(v)</c> for the loop variable <c>v</c>). Children append their <c>.CsName</c> to both;
    /// a nested FIXED OCCURS below the element wraps both in <c>CobolTable.At</c>. Yields a <see cref="DynTablePlace"/>
    /// so an INITIALIZE store writes through <c>RefReceiving</c> (within the 1‥Capacity bound, so no growth). A
    /// REDEFINES view under the element is not wired here (null → loud; inc-5 territory).</summary>
    private sealed record InitializeDynCursor(AccessPath Path, DataItem Item) : InitializeCursor(Item)
    {
        public override InitializeCursor? Child(DataItem child) =>
            child.Class is null
                ? new InitializeDynCursor(Path.Add(new MemberSegment(child.CsName)), child)
                : null;

        public override InitializeCursor Indexed(string indexVar) =>
            this with { Path = Path.Add(new FixedTableSegment(indexVar)) };

        public override Place ToPlace() => new DynTablePlace(Path, Item);

        public override AccessPath? StoragePath => Path;
    }

    /// <summary>A cursor inside a Tier-B REDEFINES class: every receiver is a (offset, width) character window over
    /// the class's ONE string backing. The window offset = the entry place's offset expression + (this item's
    /// in-class offset − the entry item's) + Σ (indexVar − 1) × per-occurrence width for each OCCURS level crossed
    /// (ISO §13.18.44 — a redefined table lays its occurrences end-to-end in the one backing; the same arithmetic
    /// as <c>ReferenceResolver.PlaceForItem</c>).</summary>
    /// <param name="Cell">The class's backing <c>StorageCell</c> path, for the three cell-backed surfaces — what a
    /// pointer-class receiver's managed slot addresses (kb/Work PB231; <see cref="SlotWindow"/>). Null for a plain
    /// REDEFINES class, which cannot hold such a member.</param>
    private sealed record InitializeViewCursor(
        AccessPath Backing, string BaseExpr, int BaseOffset, DataItem Item, string OccursTerms,
        string OccursBitTerms = "", AccessPath? Cell = null) : InitializeCursor(Item)
    {
        public override InitializeCursor? Child(DataItem child) =>
            ReferenceEquals(child.Class, Item.Class) ? this with { Item = child } : null;

        // The byte stride and its BIT twin, accumulated together so a USAGE BIT receiver's occurrences are
        // displaced by §8.5.1.6.3's "next bit position" rather than by its byte ceiling (kb/Work PB203).
        public override InitializeCursor Indexed(string indexVar) => this with
        {
            OccursTerms = $"{OccursTerms} + ({indexVar} - 1) * {Item.ImageWidth}",
            OccursBitTerms = $"{OccursBitTerms} + ({indexVar} - 1) * {BitLayout.StrideBits(Item)}",   // §13.18.1.4 GR2
        };

        public override Place ToPlace()
        {
            int delta = Item.ClassOffset - BaseOffset;
            // ⛔ THROUGH THE ONE WINDOW BUILDER (kb/Work PB203). `BaseExpr - BaseOffset` is the entry place's
            // RUNTIME displacement with its static in-class offset removed — exactly what the builder needs,
            // because a bit member's own position is already carried in BITS by DataItem.ClassBitOffset and
            // re-deriving it from a byte expression would round a sub-byte member down to its containing byte.
            return RedefViewPlace.For(Backing, Item,
                $"{BaseExpr}{(delta != 0 ? $" + {delta}" : "")}{OccursTerms}",
                BaseExpr == BaseOffset.ToString() ? null : $"{BaseExpr} - {BaseOffset}", OccursBitTerms, Cell);
        }
    }
}
