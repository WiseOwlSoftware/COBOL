// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// SORT / MERGE / RELEASE / RETURN emission (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34): the
/// release / sequence / return phases (SORT GR9) over the in-memory per-SD image store
/// (<c>CobolNet.Runtime.IO.CobolSort</c>). USING/GIVING route through the SAME <c>CobolFile</c> verbs as explicit
/// OPEN/READ/WRITE/CLOSE — the GR12/GR15 "as if" semantics, so FILE STATUS (and the G6 USE declaratives when they
/// land) apply to the implicit I/O for free. INPUT/OUTPUT PROCEDURE run as bounded dispatcher ranges
/// (<c>__Dispatch(start, end)</c>) — the dispatcher's bounded return IS the GR11/GR14 compiler-inserted return
/// mechanism. Format 2 sorts the typed element array in place with a typed comparer (COBOLNET_DESIGN §8.2).
/// </summary>
public sealed partial class CSharpEmitter
{
    private int _sortCounter;   // unique-name counter for sort-family temporaries

    /// <summary>SORT Format 1 (ISO §14.9.40 GR9 — the three phases): (a) release — the USING files' records via
    /// implicit OPEN INPUT / READ / RELEASE / CLOSE (GR12), or the INPUT PROCEDURE as a bounded dispatch (GR11);
    /// (b) sequence — one stable key sort (GR8; stability = the GR3 DUPLICATES IN ORDER order, safe without the
    /// phrase per GR4); (c) return — the GIVING files via implicit OPEN OUTPUT / RETURN / WRITE / CLOSE, each file
    /// receiving the FULL sorted result (GR15), or the OUTPUT PROCEDURE whose RETURN statements pull records (GR14).</summary>
    private void EmitSort(BoundSort so)
    {
        var w = _ctx.Writer;
        string sd = CsLiteral(so.File.CobolName);
        w.Line($"CobolSort.Init({sd});   // SORT {so.File.CobolName} (ISO §14.9.40)");

        // Phase a — release (GR9a). USING/GIVING files must not be open when their phase starts (GR9a/GR9c —
        // EC-SORT-MERGE-FILE-OPEN; EC checking OFF by default, COBOLNET_DESIGN §18.16 — seam in CobolSort).
        if (so.Using.Count > 0)
            foreach (var input in so.Using)
                SortEmitInputFile(input, sd, so.RecordWidth, so.Varying is not null);
        else if (so.InputProcedure is { } ip && ip.Start <= ip.End)
            w.Line($"__Dispatch({ip.Start}, {ip.End});   // INPUT PROCEDURE (GR11 — the bounded return IS the inserted return mechanism)");

        // Phase b — sequence (GR9b).
        w.Line($"CobolSort.Sort({sd}, {SortKeysExpr(so.Keys)}, {SortWeightsExpr(so.Collating)}, "
            + $"{(so.DuplicatesInOrder ? "true" : "false")});");

        // Phase c — return (GR9c).
        if (so.Giving.Count > 0)
            foreach (var output in so.Giving)
                SortEmitGivingFile(output, sd);
        else if (so.OutputProcedure is { } op && op.Start <= op.End)
            w.Line($"__Dispatch({op.Start}, {op.End});   // OUTPUT PROCEDURE (GR14 — RETURNs request the next sorted record)");

        w.Line($"CobolSort.Close({sd});");
    }

    /// <summary>MERGE (ISO §14.9.24): obtain every USING file's records via the implicit OPEN INPUT / READ / CLOSE
    /// (GR7), k-way-merge the pre-sorted streams — equal keys keep USING-file order, all of one file before the
    /// next (GR4) — then write the FULL merged result to every GIVING file (GR12) or run the OUTPUT PROCEDURE
    /// (GR8/GR9). Unordered input is GR6's EC-SORT-MERGE-SEQUENCE (checking OFF, COBOLNET_DESIGN §18.16 — seam in
    /// <c>CobolSort.Merge</c>).</summary>
    private void EmitMerge(BoundMerge mg)
    {
        var w = _ctx.Writer;
        string sd = CsLiteral(mg.File.CobolName);
        w.Line($"CobolSort.Init({sd});   // MERGE {mg.File.CobolName} (ISO §14.9.24)");
        foreach (var input in mg.Using)
        {
            w.Line($"CobolSort.NextInput({sd});   // a new pre-sorted USING stream (GR4 — file order breaks ties)");
            SortEmitInputFile(input, sd, mg.RecordWidth, mg.Varying is not null);
        }
        w.Line($"CobolSort.Merge({sd}, {SortKeysExpr(mg.Keys)}, {SortWeightsExpr(mg.Collating)});");
        if (mg.Giving.Count > 0)
            foreach (var output in mg.Giving)
                SortEmitGivingFile(output, sd);   // GR12 — each file-name-4 receives the WHOLE merged result
        else if (mg.OutputProcedure is { } op && op.Start <= op.End)
            w.Line($"__Dispatch({op.Start}, {op.End});   // OUTPUT PROCEDURE (GR9)");
        w.Line($"CobolSort.Close({sd});");
    }

    /// <summary>The implicit USING transfer for one input file (SORT GR12 / MERGE GR7): OPEN INPUT, READ-loop
    /// releasing each record, CLOSE — through the ordinary <c>CobolFile</c> verbs so status (and declaratives,
    /// later) behave exactly as for explicit I/O. The loop ends on AT END or ANY unsuccessful read (a missing
    /// file's failed OPEN makes the first READ unsuccessful — never a spin). The file's FILE STATUS item observes
    /// the final (CLOSE) status, the only value visible after the statement.</summary>
    private void SortEmitInputFile(FileModel input, string sdLit, int sdWidth, bool varying)
    {
        var w = _ctx.Writer;
        string f = CsLiteral(input.CobolName);
        string tmp = $"__srt{_sortCounter++}";
        w.Line($"CobolFile.OpenInput({f});   // implicit OPEN INPUT (ISO §14.9.40 GR12a / §14.9.24 GR7a)");
        using (w.Block($"while (CobolFile.Read({f}, out var {tmp}))"))
        {
            // GR12b: a record larger/smaller than the SD's record range ⇒ EC-SORT-MERGE-RELEASE (checking OFF,
            // §18.16). Fixed SD: the short record space-fills right to the fixed length (GR7c/MERGE GR2c — the
            // Store pad); varying SD: each record releases at the size it was READ (GR12b — the sequential
            // connector returns the input file's record width).
            w.Line(varying
                ? $"CobolSort.Release({sdLit}, {tmp});"
                : $"CobolSort.Release({sdLit}, CobolString.Store({tmp}, {sdWidth}));");
        }
        w.Line($"CobolFile.Close({f});   // implicit CLOSE (GR12c / GR7c)");
        EmitStoreFileStatus(input);
    }

    /// <summary>The implicit GIVING transfer for one output file (SORT GR15 / MERGE GR12): REWIND the return
    /// cursor (EACH file receives the full result), OPEN OUTPUT, RETURN→WRITE loop, CLOSE. A fixed-length GIVING
    /// file space-fills a shorter returned record to its record width (GR16c / MERGE GR13c — the connector's
    /// fixed-width fit); a relative GIVING file's key sequence 1..n (GR15b) is the G5 relative slice.</summary>
    private void SortEmitGivingFile(FileModel output, string sdLit)
    {
        var w = _ctx.Writer;
        string f = CsLiteral(output.CobolName);
        string tmp = $"__srt{_sortCounter++}";
        w.Line($"CobolSort.Rewind({sdLit});   // each GIVING file receives the FULL result (GR15 / MERGE GR12)");
        w.Line($"CobolFile.OpenOutput({f});   // implicit OPEN OUTPUT (GR15a)");
        using (w.Block($"while (CobolSort.Return({sdLit}, out var {tmp}))"))
            w.Line($"CobolFile.Write({f}, {tmp});   // implicit WRITE without optional phrases (GR15b)");
        w.Line($"CobolFile.Close({f});   // implicit CLOSE (GR15c)");
        EmitStoreFileStatus(output);
    }

    /// <summary>RELEASE (ISO §14.9.32): FROM first MOVEs into the record (GR4 — identical to the explicit MOVE),
    /// then the record's character image goes to the sort store (GR2). A varying SD releases the leading
    /// DEPENDING-ON characters (§13.18.43 GR13 — the current value of the data item names the released length);
    /// a fixed SD releases the named record's image (a shorter secondary record space-fills via the sort store's
    /// fixed-compare space extension; §14.9.40 GR7c).</summary>
    private void EmitRelease(BoundRelease rl)
    {
        var w = _ctx.Writer;
        if (rl.From is { } from) EmitMove(new BoundMove(from, [rl.Record]));   // GR4a: MOVE x TO record-name-1
        string sd = CsLiteral(rl.File.CobolName);
        string image = OperandText.AsString(new BoundFieldOperand(rl.Record));
        if (rl.Varying is { } v)
        {
            // §13.18.43 GR13: the released record's length = the RECORD VARYING DEPENDING ON item's current value.
            // Out-of-range lengths are EC-SORT-MERGE-RELEASE (min {v.Min} max {v.Max}; checking OFF, §18.16).
            w.Line($"CobolSort.Release({sd}, CobolString.RefMod({image}, 1, (int)CobolTable.Occ({v.Depending.Read()})));");
            return;
        }
        w.Line($"CobolSort.Release({sd}, {image});");
    }

    /// <summary>RETURN (ISO §14.9.34): pull the next record in key order into the SD record area (GR3); a varying
    /// SD restores the returned record's length into the DEPENDING item (§13.18.43 GR15); INTO then MOVEs the
    /// record area to the receiver (GR5 — skipped when at end); control goes to NOT AT END / AT END (GR3/GR4).
    /// At end the record area is undefined and further RETURNs are EC-SORT-MERGE-RETURN (GR3; checking OFF,
    /// §18.16 — the store deterministically reports at-end again).</summary>
    private void EmitReturn(BoundReturn rt)
    {
        var w = _ctx.Writer;
        string sd = CsLiteral(rt.File.CobolName);
        string tmp = $"__srt{_sortCounter++}";
        using (w.Block($"if (CobolSort.Return({sd}, out var {tmp}))"))
        {
            EmitImageInto(rt.RecordArea, tmp);   // GR3 — made available in the record area
            if (rt.Varying is { } v)             // §13.18.43 GR15 — each record's length restored into DEPENDING
                StoreArith(v.Depending, new NumX($"CobolSort.LastReturnedLength({sd})", 0), CobolRounding.Truncation);
            if (rt.Into is { } into)             // GR5 — RETURN then MOVE record-area → identifier-1
                EmitMove(new BoundMove(new BoundFieldOperand(rt.RecordArea), [into]));
            if (rt.NotAtEnd is { } not) EmitStatementList(not);
        }
        using (w.Block("else"))
        {
            if (rt.AtEnd is { } at) EmitStatementList(at);
        }
    }

    /// <summary>SORT Format 2 — the in-place TABLE sort (ISO §14.9.40 GR18–GR24): a stable
    /// (<c>Enumerable.OrderBy</c>) typed-comparer sort over the element array, copied back into the table (GR24).
    /// Stability preserves the pre-sort relative order of equal keys (GR3c DUPLICATES IN ORDER; without the phrase
    /// the order is undefined, GR4 — stable is conformant). Numeric keys compare by value (GR19 → the relation-
    /// condition rules, §8.8.4.2 — never collated); character keys compare under the GR5-resolved sequence.</summary>
    private void EmitTableSort(BoundTableSort ts)
    {
        var w = _ctx.Writer;
        int id = _sortCounter++;
        // The element's STORAGE type is final only after the post-bind whole-group analysis (StoreAsImage) —
        // read it now, at emit time, never at bind time.
        string elem = ts.Table.ElementType;
        string weightsArg = SortTableWeightsArg(ts.Collating, id);
        w.Line($"var __ta{id} = {ts.ArrayPath};   // SORT table (ISO §14.9.40 Format 2 — in place, GR18/GR24)");
        w.Line($"System.Comparison<{elem}> __tc{id} = (__a, __b) =>");
        w.Line("{");
        w.Indent();
        w.Line("int __c;");
        foreach (var key in ts.Keys)
        {
            // GR2: key significance = statement order; GR19a/b: DESCENDING inverts the per-key result.
            w.Line($"__c = {SortTableCompare(key, "__a", "__b", weightsArg)};");
            w.Line($"if (__c != 0) return {(key.Descending ? "-__c" : "__c")};");
        }
        w.Line("return 0;   // GR19c — equal on every key; OrderBy stability keeps the pre-sort order (GR3c)");
        w.Outdent();
        w.Line("};");
        w.Line($"var __ts{id} = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy("
            + $"__ta{id}, __e => __e, System.Collections.Generic.Comparer<{elem}>.Create(__tc{id})));");
        w.Line($"System.Array.Copy(__ts{id}, __ta{id}, __ts{id}.Length);   // GR24 — placed back in data-name-2");
    }

    /// <summary>One table-sort key's <c>int</c> comparison expression over element variables <paramref name="a"/>
    /// and <paramref name="b"/>: numeric keys compare by VALUE (ISO §14.9.40 GR19 → §8.8.4.2.4 — never through a
    /// collating sequence; an image-stored zoned leaf decodes via its profile), character/group keys compare as
    /// alphanumeric under the statement's resolved sequence (§8.8.4.2.7).</summary>
    private static string SortTableCompare(BoundTableSortKey key, string a, string b, string weightsArg)
    {
        string pa = key.MemberPath.Length == 0 ? a : $"{a}.{key.MemberPath}";
        string pb = key.MemberPath.Length == 0 ? b : $"{b}.{key.MemberPath}";
        DataItem k = key.Key;
        if (k.IsGroup)
            return k.IsCharacterImage
                ? $"CobolString.Compare({pa}.AsImage(), {pb}.AsImage(){weightsArg})"
                : LoudValue("int", $"table-sort key '{k.CobolName}' over a mixed-usage group (Tier-C byte island, deferred)");
        return k.Pic switch
        {
            { Category: PicCategory.Numeric, IsFloat: false } when k.StoreAsImage =>
                $"CobolNum.ParseDisplay({pa}, {k.ProfileName}).CompareTo(CobolNum.ParseDisplay({pb}, {k.ProfileName}))",
            { Category: PicCategory.Numeric, IsFloat: false } => $"({pa}).CompareTo({pb})",
            { Category: PicCategory.Numeric } => $"({pa}).CompareTo({pb})",   // COMP-1/2 — IEEE value order
            _ => $"CobolString.Compare({pa}, {pb}{weightsArg})",
        };
    }

    /// <summary>The <c>CobolSort.Key[]</c> literal for a statement's compile-time key descriptors (offset/length
    /// windows into the SD record image + the numeric sign decode, ISO §14.9.40.3 SR6e / GR8).</summary>
    private static string SortKeysExpr(IReadOnlyList<BoundSortMergeKey> keys) =>
        "new CobolSort.Key[] { " + string.Join(", ", keys.Select(k =>
            $"new({k.Offset}, {k.Length}, {(k.Descending ? "true" : "false")}, {(k.Numeric ? "true" : "false")}, "
            + $"{(k.Signed ? "true" : "false")}, NumericSign.{k.SignKind})")) + " }";

    /// <summary>The weights argument for the statement's GR5-resolved collating sequence: <c>null</c> for the
    /// native order, the compiled <c>__COLLATE</c> field when the resolved sequence IS the program collating
    /// sequence, else the statement alphabet's own inline table (a non-PCS statement alphabet has no emitted
    /// field — ST108A/ST137A shape).</summary>
    private string SortWeightsExpr(CollatingTable? table) =>
        table is null ? "null"
        : ReferenceEquals(table, _ctx.Data.Collating) ? "__COLLATE"
        : $"new ushort[] {{ {string.Join(", ", table.Positions)} }}";

    /// <summary>The trailing <c>CobolString.Compare</c> weights argument for a TABLE sort (emitting a local
    /// <c>__sw</c> table for a non-PCS statement alphabet), or empty for the native order.</summary>
    private string SortTableWeightsArg(CollatingTable? table, int id)
    {
        if (table is null) return "";
        if (ReferenceEquals(table, _ctx.Data.Collating)) return ", __COLLATE";
        _ctx.Writer.Line($"ushort[] __sw{id} = {{ {string.Join(", ", table.Positions)} }};   // statement COLLATING SEQUENCE (GR5a)");
        return $", __sw{id}";
    }
}
