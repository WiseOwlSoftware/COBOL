// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SEARCH / SEARCH ALL verb binder (P7 Step 10m — a real collaborator over
/// <see cref="BinderContext"/>, on the shared <c>BoundSearch</c> machinery; the scope-aware index-cell
/// resolution rides <c>ctx.Symbols.IndexCellOf</c>, the dynamic-table bound <c>OdoModel.SearchBound</c>).</summary>
internal sealed class SearchBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind a serial SEARCH (ISO §14.9.37 Format 1). The searched operand names a table with INDEXED BY
    /// (SR2); the scan uses the table's FIRST index (§14.9.37.4 GR3 a) — unless VARYING names another index OF THE
    /// SAME TABLE, which then IS the search index (GR3 c) 1.: "If index-name-1 is specified in the INDEXED BY
    /// phrase in the OCCURS clause associated with identifier-1, the index referenced by index-name-1 is the
    /// search index"); VARYING a different table's index (GR3 c) 2.) or a data item (GR3 b) increments that item
    /// in step with the search index. NOT AT END is a non-ISO extension — it fails loud by name.</summary>
    public BoundStatement BindSearch(Core.SearchStatementContext s)
    {
        var drefs = s.dataReference();
        string tableName = drefs[0].cobolWord()?.GetText() ?? drefs[0].GetText();
        if (!ctx.Symbols.TryResolve(tableName, ctx.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH table '{tableName}' without INDEXED BY (ISO §14.9.37.3 SR2)");
        // A dynamic table NESTED under another table has no whole-table path (TablePath null), so the AT-END bound
        // (§8.5.1.9.1 current capacity) and the EnterSearch/ExitSearch bracket cannot be addressed by name — a
        // subscripted capacity path over the enclosing indices is a later increment. Reject rather than let
        // SearchBound fall back to Count=0 and silently scan ZERO occurrences (OCCURS DYNAMIC review #5; D9).
        if (table.IsDynamicTable && ctx.Refs.TablePath(table) is null)
            return new BoundUnsupported($"SEARCH of the dynamic-capacity table '{tableName}' nested under another "
                + "table (the scan bound over its current capacity needs a subscripted access path — a later increment)");

        string searchIx = ctx.Symbols.IndexCellOf(table.IndexNames[0], ctx.ActiveScope);   // scope-aware (method cell first, M2-OO-1h step 4)
        BoundSetTarget? also = null;
        if (drefs.Length > 1)   // the VARYING phrase
        {
            var v = drefs[1];
            if (host.Expr.IndexFieldOf(v) is { } vix)
            {
                if (table.IndexNames.Any(n => ctx.Symbols.IndexCellOf(n, ctx.ActiveScope) == vix)) searchIx = vix;   // same table (GR3 c) 1.)
                else also = new SetIndexTarget(vix);                                          // other table (GR3 c) 2.)
            }
            else if (ctx.Refs.Resolve(v) is { } p) also = new SetPlaceTarget(p);                  // data item (GR3 b)
            else return new BoundUnsupported($"SEARCH VARYING '{v.GetText()}'");
        }

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = host.BindBlocks(ae.statementBlock());
        }
        // A WHEN condition re-evaluates on every scan pass — §14.9.37.4 GR1, "Any subscripting specified in a WHEN
        // phrase is evaluated each time the conditions in that WHEN phrase are evaluated", over GR4's "The process
        // is then repeated using the new index setting" — so a user-function reference inside it activates per
        // pass: the per-evaluation wrapper (§8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2).
        var whens = s.searchWhenClause()
            .Select(wc =>
            {
                int udfMark = host.Udf.PendingCount;
                var cond = host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(wc.condition()), udfMark);
                return new BoundSearchWhen(cond, host.BindBlocks(wc.statementBlock()));
            })
            .ToList();
        return new BoundSearch(searchIx, table.Occurs ?? 0, also, atEnd, whens,
            DependItem: OdoModel.SearchDepending(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ctx.Refs.TablePath(table) : null,   // EC-FLOW-SEARCH bracket (GR31, D9)
            // §14.9.37.4 GR4: an out-of-range initial index (SEARCH-INDEX) / a scan advancing off the end (NO-MATCH)
            // sets the nonfatal range EC when checking is enabled — captured at the statement line (F10/CONTINUE template).
            CheckSearchIndex: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-INDEX", null, s.Start.Line),
            CheckSearchNoMatch: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-NO-MATCH", null, s.Start.Line));
    }

    /// <summary>Bind <c>SEARCH ALL</c> (ISO §14.9.37 Format 2 — the ordered-table search form). The initial index
    /// setting is ignored and this implementation scans from occurrence 1; both are inside the latitude
    /// §14.9.37.4 GR9 grants — "A non serial type of search operation MAY take place. The initial setting of the
    /// search index is ignored. Its setting is varied during the search operation in a manner specified by the
    /// implementor" — so a serial probe is one permitted technique, not a concession.
    /// <para>⛔ The LATITUDE IS THE TECHNIQUE, NOT THE RANGE. The same rule's next sentence — "At no time is it
    /// set to a value that exceeds the value that corresponds to the last element of the table or is less than
    /// the value that corresponds to the first element of the table" — is a hard bound on whatever technique is
    /// chosen, and it is why <c>ControlFlowEmitter.EmitAllScan</c> is a SEPARATE lowering from the Format-1
    /// serial scan rather than the same loop under a flag (kb/Work PB447).</para>
    /// <para>⛔ THE CONFORMANCE ARGUMENT DOES NOT REST ON SYNTAX RULE 7, AND MUST NOT BE "RESTORED" TO (kb/Work
    /// PB445). SR7 requires only that the OCCURS clause CARRY a KEY phrase — a requirement on the data
    /// description entry, saying nothing about how the data is ordered at run time. The proposition a search
    /// technique would need is §14.9.37.4 GR5 a) ("The contents of each key data item referenced in the WHEN
    /// phrase shall be sequenced in the table according to the ASCENDING or DESCENDING phrase"), and that is a
    /// condition the PROGRAM shall satisfy, never a guarantee the compiler may rely on — GR6 exists precisely to
    /// define behaviour when it is false, and GR6 a) 2. ends "It is undefined which of these alternatives
    /// occurs". A serial scan over unsequenced data therefore lands inside GR6 a)'s first alternative, which is
    /// an outcome the standard explicitly allows. The technique needs no syntax rule at all.</para>
    /// <para>SR7–SR13 are screened by <c>ctx.Validation.CheckSearchAllFormat2</c> (the ONE Format-2 operand model,
    /// COBOLNET1964–1966); bound onto the same <see cref="BoundSearch"/> node with <c>IsAll</c>, which selects the
    /// Format-2 lowering.</para></summary>
    public BoundStatement BindSearchAll(Core.SearchAllStatementContext s)
    {
        string tableName = s.dataReference().cobolWord()?.GetText() ?? s.dataReference().GetText();
        if (!ctx.Symbols.TryResolve(tableName, ctx.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH ALL of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH ALL table '{tableName}' without INDEXED BY (ISO §14.9.37.3 SR2)");
        if (table.IsDynamicTable && ctx.Refs.TablePath(table) is null)   // nested dynamic — see BindSearch (review #5, D9)
            return new BoundUnsupported($"SEARCH ALL of the dynamic-capacity table '{tableName}' nested under another "
                + "table (the scan bound over its current capacity needs a subscripted access path — a later increment)");

        // The Format-2 operand rules (ISO §14.9.37.3 SR7–SR13; kb/Work PB445). Screened BEFORE the WHEN phrases
        // bind, so a violation is reported against the source's own operands rather than after whatever the
        // general condition binder made of them; the bind proceeds either way (§4.2.2 ¶2 — the indication is at
        // compile time, and one compile reports every violation it can see).
        ctx.Validation.CheckSearchAllFormat2(s, table, ctx.Refs);

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = host.BindBlocks(ae.statementBlock());
        }
        // The Format-2 WHEN re-evaluates per probe of this implementation's scan technique (GR9 — the search
        // technique is implementor-specified), so a user-function reference activates per probe: the same
        // per-evaluation wrapper as Format 1 (§8.4.3.2.4 GR1/GR6a).
        var whens = s.searchAllWhenClause()
            .Select(wc =>
            {
                int udfMark = host.Udf.PendingCount;
                var cond = host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(wc.condition()), udfMark);
                return new BoundSearchWhen(cond, host.BindBlocks(wc.statementBlock()));
            })
            .ToList();
        return new BoundSearch(ctx.Symbols.IndexCellOf(table.IndexNames[0], ctx.ActiveScope), table.Occurs ?? 0,
            AlsoVaried: null, atEnd, whens, IsAll: true, DependItem: OdoModel.SearchDepending(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ctx.Refs.TablePath(table) : null,   // EC-FLOW-SEARCH bracket (GR31, D9)
            // SEARCH ALL forces the index to 1 (GR9 ignores the initial setting) so SEARCH-INDEX can never arise;
            // only an unsuccessful scan (incl. an empty table) sets EC-RANGE-SEARCH-NO-MATCH.
            CheckSearchIndex: false,
            CheckSearchNoMatch: ctx.EcState.Turn.Enabled("EC-RANGE-SEARCH-NO-MATCH", null, s.Start.Line));
    }
}
