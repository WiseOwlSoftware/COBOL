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
    /// (SR1); the scan uses the table's FIRST index — unless VARYING names another index OF THE SAME TABLE, which
    /// then IS the search index (GR8a); VARYING a different table's index or a data item increments that item in
    /// step with the search index (GR8b/c). SEARCH ALL (Format 2) is the binary-search wave (needs OCCURS KEY
    /// capture); NOT AT END is a non-ISO extension — both fail loud by name.</summary>
    public BoundStatement BindSearch(Core.SearchStatementContext s)
    {
        var drefs = s.dataReference();
        string tableName = drefs[0].cobolWord()?.GetText() ?? drefs[0].GetText();
        if (!ctx.Symbols.TryResolve(tableName, ctx.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH table '{tableName}' without INDEXED BY (ISO §14.9.37 SR1)");
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
            if (host.IndexFieldOf(v) is { } vix)
            {
                if (table.IndexNames.Any(n => ctx.Symbols.IndexCellOf(n, ctx.ActiveScope) == vix)) searchIx = vix;   // same table (GR8a)
                else also = new SetIndexTarget(vix);                                          // other table (GR8b)
            }
            else if (ctx.Refs.Resolve(v) is { } p) also = new SetPlaceTarget(p);                  // data item (GR8c)
            else return new BoundUnsupported($"SEARCH VARYING '{v.GetText()}'");
        }

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = host.BindBlocks(ae.statementBlock());
        }
        var whens = s.searchWhenClause()
            .Select(wc => new BoundSearchWhen(host.BindCondition(wc.condition()), host.BindBlocks(wc.statementBlock())))
            .ToList();
        return new BoundSearch(searchIx, table.Occurs ?? 0, also, atEnd, whens,
            DependCount: OdoModel.SearchBound(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ctx.Refs.TablePath(table) : null);   // EC-FLOW-SEARCH bracket (GR31, D9)
    }

    /// <summary>Bind <c>SEARCH ALL</c> (ISO §14.9.37 Format 2 — the binary-search form). The initial index setting
    /// is ignored (GR9) and the technique is implementor-specified: this implementation scans from occurrence 1,
    /// conformant since Format 2 requires the table ordered by its OCCURS KEYs (SR7) and the WHEN tests key
    /// equality. Bound onto the same <see cref="BoundSearch"/> machinery with <c>FromStart</c>.</summary>
    public BoundStatement BindSearchAll(Core.SearchAllStatementContext s)
    {
        string tableName = s.dataReference().cobolWord()?.GetText() ?? s.dataReference().GetText();
        if (!ctx.Symbols.TryResolve(tableName, ctx.ActiveScope, out var candidates)
            || candidates.FirstOrDefault(i => i.IsTable) is not { } table)   // fixed OR dynamic (D9)
            return new BoundUnsupported($"SEARCH ALL of non-table '{tableName}'");
        if (table.IndexNames.Count == 0)
            return new BoundUnsupported($"SEARCH ALL table '{tableName}' without INDEXED BY (ISO §14.9.37 SR1)");
        if (table.IsDynamicTable && ctx.Refs.TablePath(table) is null)   // nested dynamic — see BindSearch (review #5, D9)
            return new BoundUnsupported($"SEARCH ALL of the dynamic-capacity table '{tableName}' nested under another "
                + "table (the scan bound over its current capacity needs a subscripted access path — a later increment)");

        List<BoundStatement>? atEnd = null;
        if (s.searchAtEndClause() is { } ae)
        {
            if (ae.NOT() is not null) return new BoundUnsupported("SEARCH NOT AT END (non-ISO extension)");
            atEnd = host.BindBlocks(ae.statementBlock());
        }
        var whens = s.searchAllWhenClause()
            .Select(wc => new BoundSearchWhen(host.BindCondition(wc.condition()), host.BindBlocks(wc.statementBlock())))
            .ToList();
        return new BoundSearch(ctx.Symbols.IndexCellOf(table.IndexNames[0], ctx.ActiveScope), table.Occurs ?? 0,
            AlsoVaried: null, atEnd, whens, FromStart: true, DependCount: OdoModel.SearchBound(table, ctx.Refs),
            DynTable: table.IsDynamicTable ? ctx.Refs.TablePath(table) : null);   // EC-FLOW-SEARCH bracket (GR31, D9)
    }
}
