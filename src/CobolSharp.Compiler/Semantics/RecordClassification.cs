// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// The representation a data item gets in the .NET-native data model
/// (<c>docs/DATA_MODEL_ARCHITECTURE.md</c> §3).
/// </summary>
internal enum RepresentationKind
{
    /// <summary>Typed-native: the item maps to a native .NET value/field (the default).</summary>
    Typed,
    /// <summary>A byte-island: the item keeps a byte image because the COBOL semantics observe its bytes
    /// (REDEFINES/RENAMES type-puns, file records, pointers, CALL-aliased / LINKAGE storage, etc.).</summary>
    ByteIsland,
}

/// <summary>The per-item representation map produced by <see cref="RecordClassificationPass"/>.</summary>
internal sealed class RecordClassification(IReadOnlyDictionary<DataSymbol, RepresentationKind> representations)
{
    private readonly IReadOnlyDictionary<DataSymbol, RepresentationKind> _rep = representations;

    /// <summary>The representation assigned to <paramref name="item"/> (defaults to byte-island if unseen — the
    /// conservative "any doubt → byte" default).</summary>
    public RepresentationKind Get(DataSymbol item) =>
        _rep.TryGetValue(item, out var r) ? r : RepresentationKind.ByteIsland;

    public bool IsByteIsland(DataSymbol item) => Get(item) == RepresentationKind.ByteIsland;
    public bool IsTyped(DataSymbol item) => Get(item) == RepresentationKind.Typed;

    /// <summary>Count of items classified as a byte-island (for assertions / diagnostics).</summary>
    public int ByteIslandCount => _rep.Values.Count(r => r == RepresentationKind.ByteIsland);

    /// <summary>
    /// Fail-fast soundness check on the produced classification (run in <c>Binder.Bind</c> on every program —
    /// a permanent internal-consistency net that also exercises the classifier across the whole corpus). Two
    /// invariants the fixpoint guarantees, so a violation is an internal compiler error, never valid output:
    /// (1) a typed item's REDEFINES target is typed (byte propagates up the REDEFINES class); (2) a typed item's
    /// parent is typed (a byte island makes all subordinates byte — downward transitivity). Only classified
    /// items participate (membership-checked) so an item outside this map — which <see cref="Get"/> reports as
    /// byte by the conservative default — never produces a false positive. Throws
    /// <see cref="InvalidOperationException"/> on violation.
    /// </summary>
    public void ValidateInvariants()
    {
        var classified = new HashSet<DataSymbol>(_rep.Keys, ReferenceEqualityComparer.Instance);
        foreach ((DataSymbol item, RepresentationKind kind) in _rep)
        {
            if (kind != RepresentationKind.Typed)
                continue;
            if (item.Redefines is { } target && classified.Contains(target) && IsByteIsland(target))
                throw new InvalidOperationException(
                    $"RecordClassification invariant violated: typed item '{item.DisplayName}' REDEFINES " +
                    $"byte-island '{target.DisplayName}' (byte must propagate across the REDEFINES class).");
            if (item.Parent is { } parent && classified.Contains(parent) && IsByteIsland(parent))
                throw new InvalidOperationException(
                    $"RecordClassification invariant violated: typed item '{item.DisplayName}' is a subordinate " +
                    $"of byte-island '{parent.DisplayName}' (byte must propagate to all subordinates).");
        }
    }
}

/// <summary>
/// Assigns each data item a <see cref="RepresentationKind"/> (ADR §3). The default is
/// <see cref="RepresentationKind.Typed"/>; an item (transitively, with its REDEFINES class and subordinates)
/// is demoted to a <see cref="RepresentationKind.ByteIsland"/> when the COBOL semantics genuinely observe its
/// bytes. The pass is conservative and monotone: representations only move typed → byte, never back, so the
/// propagation fixpoint terminates (lattice height 1).
///
/// <para><b>Phase A (data-division triggers).</b> Observable from the data division alone: REDEFINES (1),
/// RENAMES/66 (2), FD/SD file records (5), IS EXTERNAL / IS GLOBAL (8), LINKAGE-SECTION items (12), and edited
/// items (13).</para>
///
/// <para><b>Phase B (procedure-division scan).</b> A bound-tree walk over the procedure division marks the
/// triggers that are only observable from how an item is <i>used</i>: reference modification of a non-string
/// item (3), <c>CALL … USING … BY REFERENCE</c> arguments (11), the ODO-whole-group operand (15), and a group
/// MOVE that reinterprets bytes under a dissimilar layout (4a). Per ADR §3.4 a group <i>comparison</i> /
/// class-condition / <c>CORRESPONDING</c> does <b>not</b> force byte-backing (a typed group materializes its
/// canonical byte image on demand; CORR lowers field-wise), so only the dissimilar-layout group-MOVE
/// <i>destination</i> is demoted. Three deliberately omitted triggers: the write-pattern perf peephole (14, ADR
/// §2.6 — a performance optimization, never a correctness trigger, so omitting it cannot corrupt; it is coupled
/// to the Stage-3 typed-string codegen that does not exist yet); ADDRESS OF (6 — unreachable today, since
/// <c>SET ADDRESS OF</c> is parsed but not yet bound; DEVLOG 389); and the future trigger #16
/// <c>USE FOR DEBUGGING</c> / <c>DEBUG-ITEM</c> (ADR §12 tracked completeness investigation — the monitored
/// item's character image populates <c>DEBUG-CONTENTS</c>, a byte-observability path; unreachable today because
/// <see cref="Bound.BoundUseStatement"/> is a stub that does not yet bind a DEBUGGING ON target). All three
/// must be closed before a Stage-3 typed flip of an item they could reach; until then Stage 2 keeps everything
/// byte-backed, so the gap changes no behavior.</para>
///
/// <para><b>Phase C (cross-edge fixpoint).</b> A same-layout group MOVE is a value-type struct copy only if
/// <i>both</i> ends share a representation (a byte source preserves its exact, possibly non-canonical, bytes;
/// a typed destination would normalize them — ISO §14.9.25 GR4). Phase B records each such MOVE as a
/// representation <i>edge</i>; the fixpoint then propagates byte across the edge (byte on either end demotes
/// both) together with the structural closure (REDEFINES class + downward island-membership). Both moves are
/// monotone typed → byte, so the combined fixpoint terminates.</para>
///
/// <para>Per ADR §3 the classifier must be <i>complete</i> before any Stage-3 typed flip; the pass is built and
/// unit-tested but <b>not yet consumed by codegen</b> (Stage 2: everything stays byte-backed regardless), so its
/// behavior is observed only by its tests until Stage 3 wires it in.</para>
/// </summary>
internal sealed class RecordClassificationPass
{
    /// <summary>
    /// Phase-A-only entry point (no procedure-division scan). Equivalent to calling
    /// <see cref="Classify(IReadOnlyList{DataSymbol}, Func{DataSymbol, CobolCategory}, IEnumerable{BoundStatement}, Func{DataSymbol, ValueTuple{int, int}?}?)"/>
    /// with no statements and no layout accessor.
    /// </summary>
    public RecordClassification Classify(IReadOnlyList<DataSymbol> items, Func<DataSymbol, CobolCategory> categoryOf)
        => Classify(items, categoryOf, Array.Empty<BoundStatement>(), layoutOf: null);

    /// <summary>
    /// Classifies <paramref name="items"/> (every data item in the program, in declaration order) running
    /// Phase A, then the Phase-B procedure-division scan over <paramref name="procedureStatements"/>, then the
    /// Phase-C combined fixpoint.
    /// <para><paramref name="categoryOf"/> yields an item's resolved <see cref="CobolCategory"/> — in the
    /// pipeline it is <c>s =&gt; model.GetStorageLocation(s)?.Pic.Category</c>.</para>
    /// <para><paramref name="procedureStatements"/> is the procedure division's top-level statements — in the
    /// pipeline <c>boundProgram.Paragraphs.SelectMany(p =&gt; p.Sentences).SelectMany(s =&gt; s.Statements)</c>;
    /// the walker recurses every nested statement/expression.</para>
    /// <para><paramref name="layoutOf"/> yields an item's <c>(Offset, Length)</c> within its storage area
    /// (in the pipeline <c>s =&gt; model.GetStorageLocation(s) is {} l ? (l.Offset, l.Length) : null</c>); it is
    /// used only by the §3.4 same-layout test. When it is <c>null</c> or cannot resolve a member, two distinct
    /// groups are treated as dissimilar (any doubt → byte).</para>
    /// </summary>
    public RecordClassification Classify(
        IReadOnlyList<DataSymbol> items,
        Func<DataSymbol, CobolCategory> categoryOf,
        IEnumerable<BoundStatement> procedureStatements,
        Func<DataSymbol, (int Offset, int Length)?>? layoutOf = null)
    {
        var rep = new Dictionary<DataSymbol, RepresentationKind>(ReferenceEqualityComparer.Instance);
        foreach (DataSymbol it in items)
            rep[it] = RepresentationKind.Typed;

        // declaration-index map (for the RENAMES FROM..THRU span)
        var index = new Dictionary<DataSymbol, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < items.Count; i++)
            index[items[i]] = i;
        int Order(DataSymbol s) => index.TryGetValue(s, out int i) ? i : -1;

        bool Mark(DataSymbol? s)
        {
            if (s is null || !rep.TryGetValue(s, out var cur) || cur == RepresentationKind.ByteIsland)
                return false;
            rep[s] = RepresentationKind.ByteIsland;
            return true;
        }

        // ── Phase A: data-division triggers ──
        foreach (DataSymbol it in items)
        {
            // (5) FD/SD file record storage — the disk image is bytes (ISO §13.18.42).
            // (12) LINKAGE items — caller-owned storage; layout cannot be renegotiated (ADR §3.12).
            if (it.Area is StorageAreaKind.FileSection or StorageAreaKind.LinkageSection)
                Mark(it);

            // (8) IS EXTERNAL / IS GLOBAL — one canonical cross-program representation (ISO §13.18.22/.27).
            if (it.IsExternal || it.IsGlobal)
                Mark(it);

            // (1) REDEFINES — the redefiner and its target are the same storage; whole class is one island.
            if (it.Redefines is not null)
            {
                Mark(it);
                Mark(it.Redefines);
            }

            // (2) RENAMES (level 66) — an alphanumeric view over a raw slice (ISO §13.18.43); byte the renaming
            // item and the items it spans, so a later typed flip cannot make the view read re-encoded bytes.
            if (it.Renames is not null)
            {
                Mark(it);
                MarkRenamesSpan(it, items, Order, Mark);
            }

            // (13) edited items carry a stored character image / edit pattern (ISO §14.9.25) — keep byte-backed.
            CobolCategory cat = categoryOf(it);
            if (cat is CobolCategory.NumericEdited or CobolCategory.AlphanumericEdited or CobolCategory.NationalEdited)
                Mark(it);
        }

        // ── Phase B: procedure-division scan → direct demotions + same-representation edges ──
        var scanner = new ProcedureScanner(categoryOf, layoutOf);
        foreach (BoundStatement st in procedureStatements)
            scanner.ScanStatement(st);
        foreach (DataSymbol d in scanner.Demote)
            Mark(d);

        // ── Phase C: combined fixpoint (REDEFINES-class closure + downward transitivity + struct-copy edges) ──
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (DataSymbol it in items)
            {
                if (rep[it] != RepresentationKind.ByteIsland)
                {
                    // a typed item whose REDEFINES target is already an island must itself become one
                    if (it.Redefines is not null && rep.TryGetValue(it.Redefines, out var t) && t == RepresentationKind.ByteIsland)
                        changed |= Mark(it);
                    continue;
                }

                // it is a byte island → its REDEFINES target and all its subordinates are byte windows too
                if (it.Redefines is not null)
                    changed |= Mark(it.Redefines);
                foreach (DataSymbol child in it.Children)
                    changed |= Mark(child);
            }

            // a same-layout group MOVE is a struct copy only if both ends share a representation: byte on
            // either end forces the other to byte too (ADR §3.4 / §2.4, ISO §14.9.25 GR4).
            foreach ((DataSymbol a, DataSymbol b) in scanner.Edges)
            {
                if (rep.GetValueOrDefault(a) == RepresentationKind.ByteIsland ||
                    rep.GetValueOrDefault(b) == RepresentationKind.ByteIsland)
                {
                    changed |= Mark(a);
                    changed |= Mark(b);
                }
            }
        }

        return new RecordClassification(rep);
    }

    /// <summary>Marks the declaration-order range a RENAMES item spans (FROM … THRU), inclusive.</summary>
    private static void MarkRenamesSpan(DataSymbol renamingItem, IReadOnlyList<DataSymbol> items,
        Func<DataSymbol, int> order, Func<DataSymbol?, bool> mark)
    {
        RenamesInfo info = renamingItem.Renames!;
        if (info.FromSymbol is null)
            return;
        mark(info.FromSymbol);
        if (info.ThruSymbol is null)
            return;
        mark(info.ThruSymbol);

        int from = order(info.FromSymbol), thru = order(info.ThruSymbol);
        if (from < 0 || thru < 0)
            return;
        if (from > thru)
            (from, thru) = (thru, from);
        for (int i = from; i <= thru; i++)
            mark(items[i]);
    }

    /// <summary>
    /// Walks the procedure-division bound tree, collecting the Phase-B byte demotions (<see cref="Demote"/>)
    /// and the same-layout group-MOVE representation edges (<see cref="Edges"/>). Stateless w.r.t. the
    /// representation map — the caller applies the demotions and runs the fixpoint over the edges.
    /// </summary>
    private sealed class ProcedureScanner(
        Func<DataSymbol, CobolCategory> categoryOf,
        Func<DataSymbol, (int Offset, int Length)?>? layoutOf)
    {
        /// <summary>Items the procedure division proves must be byte-backed (triggers 3 / 4a / 11 / 15).</summary>
        public HashSet<DataSymbol> Demote { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>Same-layout group-MOVE pairs that must share a representation (Phase-C edges).</summary>
        public List<(DataSymbol Source, DataSymbol Target)> Edges { get; } = [];

        // ── statements ──

        public void ScanStatement(BoundStatement? s)
        {
            switch (s)
            {
                case null:
                    return;

                case BoundCompoundStatement c:
                    ScanStatements(c.Statements);
                    break;

                case BoundDisplayStatement d:
                    ScanExpressions(d.Operands);
                    break;

                case BoundMoveStatement m:
                    HandleMove(m);
                    break;

                case BoundCorrespondingStatement corr:
                    // CORR lowers to per-matching-field elementary moves (ADR §2.4): the group operands are
                    // not whole-group byte operands, so only their subscripts and the SIZE ERROR body recurse.
                    ScanSubscripts(corr.SourceGroupExpr);
                    ScanSubscripts(corr.TargetGroupExpr);
                    ScanSizeError(corr.SizeError);
                    break;

                case BoundPerformStatement p:
                    ScanExpression(p.TimesExpression);
                    ScanExpression(p.UntilCondition);
                    ScanVarying(p.Varying);
                    if (p.InlineStatements is not null)
                        ScanStatements(p.InlineStatements);
                    break;

                case BoundWriteStatement w:
                    ScanExpression(w.From);
                    ScanExpression(w.AdvancingExpression);
                    ScanStatements(w.InvalidKey);
                    ScanStatements(w.NotInvalidKey);
                    ScanStatements(w.AtEndOfPage);
                    ScanStatements(w.NotAtEndOfPage);
                    break;

                case BoundIfStatement iff:
                    ScanExpression(iff.Condition);
                    ScanStatements(iff.ThenStatements);
                    if (iff.ElseStatements is not null)
                        ScanStatements(iff.ElseStatements);
                    break;

                case BoundGoToStatement g:
                    ScanExpression(g.DependingOn);
                    break;

                case BoundGoBackStatement gb:
                    ScanExpression(gb.Returning);
                    break;

                case BoundReadStatement r:
                    ScanExpression(r.Into);
                    ScanStatements(r.AtEnd);
                    ScanStatements(r.NotAtEnd);
                    ScanStatements(r.InvalidKey);
                    ScanStatements(r.NotInvalidKey);
                    break;

                case BoundRewriteStatement rw:
                    ScanExpression(rw.From);
                    ScanStatements(rw.InvalidKey);
                    ScanStatements(rw.NotInvalidKey);
                    break;

                case BoundInitializeStatement init:
                    // INITIALIZE writes its targets field-wise (not a whole-byte operation); only the
                    // REPLACING values carry expressions to recurse.
                    foreach (BoundInitializeCategoryReplacement rep in init.CategoryReplacements)
                        ScanExpression(rep.Value);
                    break;

                case BoundSetIndexStatement si:
                    ScanExpression(si.Target);
                    ScanExpression(si.Value);
                    break;

                case BoundAcceptStatement a:
                    ScanExpression(a.Target);
                    break;

                case BoundInspectStatement insp:
                    HandleInspect(insp);
                    break;

                case BoundArithmeticStatement ar:
                    ScanExpressions(ar.Operands);
                    ScanExpression(ar.Receiver);
                    foreach (BoundArithmeticTarget t in ar.Targets)
                        ScanExpression(t.Target);
                    ScanExpression(ar.RemainderTarget);
                    ScanSizeError(ar.SizeError);
                    break;

                case BoundEvaluateStatement ev:
                    HandleEvaluate(ev);
                    break;

                case BoundSearchStatement se:
                    ScanExpression(se.Table);
                    ScanExpression(se.Index);
                    ScanExpression(se.VaryingSymbol);
                    foreach (BoundSearchWhenClause when in se.Whens)
                    {
                        ScanExpression(when.Condition);
                        ScanStatements(when.Statements);
                    }
                    ScanStatements(se.AtEnd);
                    break;

                case BoundSearchAllStatement sa:
                    ScanExpression(sa.Table);
                    ScanExpression(sa.Index);
                    foreach (BoundSearchWhenClause when in sa.Whens)
                    {
                        ScanExpression(when.Condition);
                        ScanStatements(when.Statements);
                    }
                    ScanStatements(sa.AtEnd);
                    break;

                case BoundStringStatement str:
                    foreach (BoundStringSending sending in str.Sendings)
                    {
                        ScanExpression(sending.Value);
                        ScanExpression(sending.Delimiter);
                    }
                    ScanExpression(str.Into);
                    ScanExpression(str.Pointer);
                    ScanStatements(str.OnOverflow);
                    ScanStatements(str.NotOnOverflow);
                    break;

                case BoundUnstringStatement us:
                    ScanExpression(us.Source);
                    foreach (BoundUnstringDelimiter delim in us.Delimiters)
                        ScanExpression(delim.Expr);
                    foreach (BoundUnstringInto into in us.Intos)
                    {
                        ScanExpression(into.Target);
                        ScanExpression(into.CountIn);
                        ScanExpression(into.DelimiterIn);
                    }
                    ScanExpression(us.Pointer);
                    ScanExpression(us.Tallying);
                    ScanStatements(us.OnOverflow);
                    ScanStatements(us.NotOnOverflow);
                    break;

                case BoundDeleteStatement del:
                    ScanStatements(del.InvalidKey);
                    ScanStatements(del.NotInvalidKey);
                    break;

                case BoundStartStatement startStmt:
                    ScanExpression(startStmt.KeyCondition);
                    ScanStatements(startStmt.InvalidKey);
                    ScanStatements(startStmt.NotInvalidKey);
                    break;

                case BoundReturnStatement ret:
                    ScanExpression(ret.Into);
                    ScanStatements(ret.AtEnd);
                    ScanStatements(ret.NotAtEnd);
                    break;

                case BoundReleaseStatement rel:
                    ScanExpression(rel.From);
                    break;

                case BoundCallStatement call:
                    HandleCall(call);
                    break;

                case BoundGenerateStatement gen:
                    foreach (BoundReportLine line in gen.Lines)
                        foreach (BoundReportField field in line.Fields)
                            ScanExpression(field.Source);
                    break;

                // statements carrying neither nested statements nor data-bearing expressions:
                // Stop, Alter, Exit*, NextSentence, Open, Close, Entry, Cancel, SetSwitch, SetCondition,
                // Use, Initiate, Terminate, DeleteFile, Sort, TableSort, Merge.
                default:
                    break;
            }
        }

        private void ScanStatements(IEnumerable<BoundStatement> statements)
        {
            foreach (BoundStatement st in statements)
                ScanStatement(st);
        }

        // ── expressions ──

        private void ScanExpression(BoundExpression? e)
        {
            switch (e)
            {
                case null:
                    return;

                case BoundReferenceModificationExpression rm:
                    // (3) refmod base is byte unless it is a proven-homogeneous character string (a single
                    // elementary alphanumeric / national / alphabetic item) — a typed long/decimal/bool has no
                    // positional character image to slice (ISO §8.4.3.3.4 GR2). Groups (not elementary) and
                    // numeric-DISPLAY / edited / boolean items therefore demote.
                    if (!IsHomogeneousStringField(rm.Base.Symbol))
                        Demote.Add(rm.Base.Symbol);
                    ScanExpression(rm.Base);
                    ScanExpression(rm.Start);
                    ScanExpression(rm.Length);
                    break;

                case BoundIdentifierExpression id:
                    // (15) a group transitively containing an OCCURS DEPENDING ON item, referenced as a whole
                    // operand, cannot be one typed shape (sender = current count, receiver = MAX; ISO §13.18.39.3).
                    if (id.Symbol.IsGroup && ContainsOdo(id.Symbol))
                        Demote.Add(id.Symbol);
                    // (S4) a fixed OCCURS table referenced as a WHOLE operand (not subscripted) needs its contiguous
                    // byte image — a typed `T[]` has no whole-table byte home — so keep it byte-backed; only
                    // exclusively-element-accessed (`ARR(i)`) tables flip. (docs/RECORD_STRUCT_STORAGE_DESIGN.md §9.3)
                    if (id.Symbol.Occurs is not null && !id.IsSubscripted)
                        Demote.Add(id.Symbol);
                    // (S4) likewise a whole reference to a GROUP containing fixed OCCURS tables demotes those tables —
                    // the whole-group op reads/writes their bytes, which a typed array would not maintain.
                    if (id.Symbol.IsGroup && !id.IsSubscripted)
                        DemoteFixedOccursTables(id.Symbol);
                    if (id.Subscripts is not null)
                        ScanExpressions(id.Subscripts);
                    break;

                case BoundBinaryExpression b:
                    ScanExpression(b.Left);
                    ScanExpression(b.Right);
                    break;

                case BoundFunctionCallExpression f:
                    ScanExpressions(f.Arguments);
                    break;

                case BoundAbbreviatedExpression ab:
                    ScanExpression(ab.Right);
                    break;

                case BoundClassConditionExpression cc:
                    ScanExpression(cc.Subject);
                    break;

                case BoundUserClassConditionExpression uc:
                    ScanExpression(uc.Subject);
                    break;

                case BoundSignConditionExpression sc:
                    ScanExpression(sc.Subject);
                    break;

                case BoundConditionNameExpression cn:
                    ScanExpression(cn.ParentExpression);
                    break;

                // leaves with no data-bearing children: literal, figurative, switch, linage/line/page counters.
                default:
                    break;
            }
        }

        private void ScanExpressions(IEnumerable<BoundExpression> expressions)
        {
            foreach (BoundExpression e in expressions)
                ScanExpression(e);
        }

        private void ScanSubscripts(BoundIdentifierExpression? id)
        {
            if (id?.Subscripts is not null)
                ScanExpressions(id.Subscripts);
        }

        // ── per-statement helpers ──

        private void HandleMove(BoundMoveStatement m)
        {
            // generic recursion handles refmod (3) and ODO-whole-group (15) anywhere in source/targets.
            ScanExpression(m.Source);
            ScanExpressions(m.Targets);

            // (4a) a group MOVE destination must hold the raw moved image unless the source is an
            // unsubscripted group of provably-identical layout (a value-type struct copy, ADR §2.4). The
            // identical-layout case is kept typed but recorded as a Phase-C edge (both ends must share a
            // representation). Everything else — dissimilar layout, a non-group/figurative source, a subscripted
            // group occurrence, or an unknown layout — demotes the destination (any doubt → byte).
            foreach (BoundExpression target in m.Targets)
            {
                if (target is not BoundIdentifierExpression tid || !tid.Symbol.IsGroup)
                    continue;

                if (m.Source is BoundIdentifierExpression sid && sid.Symbol.IsGroup &&
                    !tid.IsSubscripted && !sid.IsSubscripted && SameLayout(sid.Symbol, tid.Symbol))
                    Edges.Add((sid.Symbol, tid.Symbol));
                else
                    Demote.Add(tid.Symbol);
            }
        }

        private void HandleCall(BoundCallStatement call)
        {
            foreach (BoundCallArgument arg in call.Arguments)
            {
                ScanExpression(arg.Expression);

                // (11) a BY REFERENCE argument aliases this storage region into the callee, whose LINKAGE
                // re-description is unknowable here — an unconditional byte trigger (ISO §14.2.3 GR8). BY
                // CONTENT / BY VALUE pass a copy and do not alias.
                if (arg.Mode == ParameterMode.ByReference && BaseSymbolOf(arg.Expression) is { } sym)
                    Demote.Add(sym);
            }
            ScanExpression(call.ReturningTarget);
            ScanStatements(call.OnException);
            ScanStatements(call.NotOnException);
        }

        private void HandleInspect(BoundInspectStatement insp)
        {
            ScanExpression(insp.Target);
            foreach (BoundInspectTallyingItem t in insp.Tallying)
            {
                ScanExpression(t.Counter);
                ScanPattern(t.Pattern);
                ScanRegion(t.Region);
            }
            foreach (BoundInspectReplacingItem r in insp.Replacing)
            {
                ScanPattern(r.Pattern);
                ScanPattern(r.Replacement);
                ScanRegion(r.Region);
            }
            if (insp.Converting is { } conv)
            {
                ScanPattern(conv.FromSet);
                ScanPattern(conv.ToSet);
                ScanRegion(conv.Region);
            }
        }

        private void ScanPattern(InspectPatternValue? pattern)
        {
            if (pattern?.DataRef is { } dataRef)
                ScanExpression(dataRef);
        }

        private void ScanRegion(BoundInspectRegion region)
        {
            ScanPattern(region.BeforePattern);
            ScanPattern(region.AfterPattern);
        }

        private void HandleEvaluate(BoundEvaluateStatement ev)
        {
            ScanExpressions(ev.Subjects);
            foreach (BoundEvaluateWhen when in ev.Whens)
            {
                foreach (BoundEvaluateCondition cond in when.SubjectConditions)
                {
                    switch (cond)
                    {
                        case BoundEvaluateValueCondition vc:
                            ScanExpressions(vc.Values);
                            foreach (BoundEvaluateRange range in vc.Ranges)
                            {
                                ScanExpression(range.From);
                                ScanExpression(range.To);
                            }
                            break;
                        case BoundEvaluateConditionWhen cw:
                            ScanExpression(cw.Condition);
                            break;
                    }
                }
                ScanStatements(when.Statements);
            }
            if (ev.WhenOther is not null)
                ScanStatements(ev.WhenOther);
        }

        private void ScanVarying(BoundPerformVarying? v)
        {
            if (v is null)
                return;
            ScanExpression(v.IndexExpression);
            ScanExpression(v.Initial);
            ScanExpression(v.Step);
            ScanExpression(v.UntilCondition);
            ScanVarying(v.Next);
        }

        private void ScanSizeError(BoundSizeErrorClause? se)
        {
            if (se is null)
                return;
            ScanStatements(se.OnSizeError);
            ScanStatements(se.NotOnSizeError);
        }

        // ── classification predicates ──

        /// <summary>The base data item of an expression that can be a CALL BY REFERENCE argument.</summary>
        private static DataSymbol? BaseSymbolOf(BoundExpression e) => e switch
        {
            BoundIdentifierExpression id => id.Symbol,
            BoundReferenceModificationExpression rm => rm.Base.Symbol,
            _ => null,
        };

        /// <summary>True for a single elementary item whose typed form is a UTF-16 <c>string</c> (PIC X / A / N,
        /// non-edited) — the only refmod base that stays typed (ADR §3.3a, §1.2).</summary>
        private bool IsHomogeneousStringField(DataSymbol s) =>
            s.IsElementary &&
            categoryOf(s) is CobolCategory.Alphanumeric or CobolCategory.National or CobolCategory.Alphabetic;

        /// <summary>True if <paramref name="group"/> transitively contains an OCCURS DEPENDING ON item.</summary>
        private static bool ContainsOdo(DataSymbol group)
        {
            foreach (DataSymbol child in group.Children)
            {
                if (child.Occurs?.DependingOnSymbol is not null || child.Occurs?.DependingOnName is not null)
                    return true;
                if (ContainsOdo(child))
                    return true;
            }
            return false;
        }

        /// <summary>S4: demote every fixed-OCCURS (non-DEPENDING-ON) elementary table at or beneath
        /// <paramref name="group"/> to byte — used when the group is referenced as a whole operand, so the table's
        /// contiguous byte image (which the whole-group op reads) is kept maintained rather than flipped to a
        /// typed array. (docs/RECORD_STRUCT_STORAGE_DESIGN.md §9.3)</summary>
        private void DemoteFixedOccursTables(DataSymbol group)
        {
            foreach (DataSymbol child in group.Children)
            {
                if (child.Occurs is { DependingOnSymbol: null, DependingOnName: null })
                    Demote.Add(child);
                DemoteFixedOccursTables(child);
            }
        }

        /// <summary>
        /// True if two groups have an identical byte layout: same total length and the same ordered sequence of
        /// (relative offset, length, category, usage) over their elementary descendants. Differing SYNC-aligned
        /// offsets therefore count as dissimilar even when the declared fields match (ADR §3.4). Returns false
        /// (dissimilar) whenever the layout accessor is absent or cannot resolve a member — any doubt → byte.
        /// </summary>
        private bool SameLayout(DataSymbol a, DataSymbol b)
        {
            if (layoutOf is null)
                return false;
            List<(int, int, CobolCategory, UsageKind)>? sa = LayoutSignature(a);
            List<(int, int, CobolCategory, UsageKind)>? sb = LayoutSignature(b);
            return sa is not null && sb is not null && sa.SequenceEqual(sb);
        }

        private List<(int RelOffset, int Length, CobolCategory Category, UsageKind Usage)>? LayoutSignature(DataSymbol group)
        {
            if (layoutOf!(group) is not { } groupLoc)
                return null;
            var sig = new List<(int, int, CobolCategory, UsageKind)> { (0, groupLoc.Length, CobolCategory.Unknown, UsageKind.Display) };
            return AppendLeaves(group, groupLoc.Offset, sig) ? sig : null;
        }

        private bool AppendLeaves(DataSymbol item, int baseOffset,
            List<(int, int, CobolCategory, UsageKind)> sig)
        {
            foreach (DataSymbol child in item.Children)
            {
                if (child.IsGroup)
                {
                    if (!AppendLeaves(child, baseOffset, sig))
                        return false;
                    continue;
                }
                if (layoutOf!(child) is not { } loc)
                    return false;
                sig.Add((loc.Offset - baseOffset, loc.Length, categoryOf(child), child.Usage));
            }
            return true;
        }
    }
}
