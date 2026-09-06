// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// SORT / MERGE / RELEASE / RETURN emission (ISO/IEC 1989:2023 §14.9.40 / §14.9.24 / §14.9.32 / §14.9.34; P7
/// Step 9e — a real collaborator over the per-unit <see cref="EmitContext"/>): the release / sequence / return
/// phases (SORT GR9) over the in-memory per-SD image store (<c>CobolNet.Runtime.IO.CobolSort</c>). USING/GIVING
/// route through the SAME runtime file verbs as explicit OPEN/READ/WRITE/CLOSE — the GR12/GR15 "as if" semantics,
/// so FILE STATUS (and the G6 USE declaratives when they land) apply to the implicit I/O for free. INPUT/OUTPUT
/// PROCEDURE run as bounded dispatcher ranges (<c>__Dispatch(start, end)</c>) — the dispatcher's bounded return
/// IS the GR11/GR14 compiler-inserted return mechanism. Format 2 sorts the typed element array in place with a
/// typed comparer (COBOLNET_DESIGN §8.2).
/// </summary>
internal sealed class SortEmitter(EmitContext ctx, DispatchState dispatch,
    SequentialIoEmitter seqIo, MoveEmitter move, ArithmeticEmitter arith)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the RETURN AT END /
    /// NOT AT END phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>SORT Format 1 (ISO §14.9.40 GR9 — the three phases): (a) release — the USING files' records via
    /// implicit OPEN INPUT / READ / RELEASE / CLOSE (GR12), or the INPUT PROCEDURE as a bounded dispatch (GR11);
    /// (b) sequence — one stable key sort (GR8; stability = the GR3 DUPLICATES IN ORDER order, safe without the
    /// phrase per GR4); (c) return — the GIVING files via implicit OPEN OUTPUT / RETURN / WRITE / CLOSE, each file
    /// receiving the FULL sorted result (GR15), or the OUTPUT PROCEDURE whose RETURN statements pull records (GR14).</summary>
    public void EmitSort(BoundSort so)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(so.File);
        w.Line($"{RuntimeApi.SortInit(sd, WeightsExpr(so.Collating), NatWeightsExpr(so.Collating))};   // SORT {so.File.CobolName} (ISO §14.9.40; BOTH GR5 sequences snapshotted — §14.6.6 r5)");

        // Phase a — release (GR9a). USING/GIVING files must not be open when their phase starts (GR9a/GR9c —
        // EC-SORT-MERGE-FILE-OPEN; EC checking OFF by default, COBOLNET_DESIGN §18.16 — seam in CobolSort).
        if (so.Using.Count > 0)
            foreach (var input in so.Using)
                EmitInputFile(input, sd, so.RecordWidth, so.Varying is not null);
        else if (so.InputProcedure is { } ip && ip.Start <= ip.End)
            w.Line($"{dispatch.DispatchName}({ip.Start}, {ip.End});   // INPUT PROCEDURE (GR11 — the bounded return IS the inserted return mechanism)");

        // Phase b — sequence (GR9b).
        w.Line($"{RuntimeApi.SortSort(sd, KeysExpr(so.Keys), so.DuplicatesInOrder ? "true" : "false")};   // the GR5 sequences are the Init snapshot's (§14.6.6 r5)");

        // Phase c — return (GR9c).
        if (so.Giving.Count > 0)
            foreach (var output in so.Giving)
                EmitGivingFile(output, sd);
        else if (so.OutputProcedure is { } op && op.Start <= op.End)
            w.Line($"{dispatch.DispatchName}({op.Start}, {op.End});   // OUTPUT PROCEDURE (GR14 — RETURNs request the next sorted record)");

        w.Line($"{RuntimeApi.SortClose(sd)};");
    }

    /// <summary>MERGE (ISO §14.9.24): obtain every USING file's records via the implicit OPEN INPUT / READ / CLOSE
    /// (GR7), k-way-merge the pre-sorted streams — equal keys keep USING-file order, all of one file before the
    /// next (GR4) — then write the FULL merged result to every GIVING file (GR12) or run the OUTPUT PROCEDURE
    /// (GR8/GR9). Unordered input is GR6's EC-SORT-MERGE-SEQUENCE (checking OFF, COBOLNET_DESIGN §18.16 — seam in
    /// the runtime's <c>Merge</c>).</summary>
    public void EmitMerge(BoundMerge mg)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(mg.File);
        w.Line($"{RuntimeApi.SortInit(sd, WeightsExpr(mg.Collating), NatWeightsExpr(mg.Collating))};   // MERGE {mg.File.CobolName} (ISO §14.9.24; BOTH GR5 sequences snapshotted — §14.6.6 r5)");
        foreach (var input in mg.Using)
        {
            w.Line($"{RuntimeApi.SortNextInput(sd)};   // a new pre-sorted USING stream (GR4 — file order breaks ties)");
            EmitInputFile(input, sd, mg.RecordWidth, mg.Varying is not null);
        }
        w.Line($"{RuntimeApi.SortMerge(sd, KeysExpr(mg.Keys))};   // the GR5 sequences are the Init snapshot's");
        if (mg.Giving.Count > 0)
            foreach (var output in mg.Giving)
                EmitGivingFile(output, sd);   // GR12 — each file-name-4 receives the WHOLE merged result
        else if (mg.OutputProcedure is { } op && op.Start <= op.End)
            w.Line($"{dispatch.DispatchName}({op.Start}, {op.End});   // OUTPUT PROCEDURE (GR9)");
        w.Line($"{RuntimeApi.SortClose(sd)};");
    }

    /// <summary>The implicit USING transfer for one input file (SORT GR12 / MERGE GR7): OPEN INPUT, READ-loop
    /// releasing each record, CLOSE — through the ordinary runtime file verbs so status (and declaratives,
    /// later) behave exactly as for explicit I/O. The loop ends on AT END or ANY unsuccessful read (a missing
    /// file's failed OPEN makes the first READ unsuccessful — never a spin). The file's FILE STATUS item observes
    /// the final (CLOSE) status, the only value visible after the statement.</summary>
    private void EmitInputFile(FileModel input, string sdLit, int sdWidth, bool varying)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(input);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        // §12.4.5.3 GR3 names SORT and MERGE beside OPEN, so the implicit open associates with the file the
        // ELEMENT EXECUTING THE SORT/MERGE names — its own ASSIGN specification and LINAGE operands (PB673).
        w.Line($"{RuntimeApi.FileOpenInput(f, seqIo.ExecutingElementArgs(input))};   // implicit OPEN INPUT (ISO §14.9.40 GR12a / §14.9.24 GR7a)");
        seqIo.EmitUseHook(input);   // a failed implicit OPEN reaches a USE declarative (GR12a)
        // "false" = §14.9.30.4 GR19's NEXT: the implicit SORT/MERGE USING retrieval is the forward walk of
        // §14.9.43.4 ("the records … are transferred … in the order in which they are made available"), never a
        // statement-written direction — this loop renders no READ statement of the program's.
        using (w.Block($"while ({RuntimeApi.FileRead(f, "false", tmp)})"))
        {
            // GR12b: a record larger/smaller than the SD's record range ⇒ EC-SORT-MERGE-RELEASE (checking OFF,
            // §18.16). Fixed SD: the short record space-fills right to the fixed length (GR7c/MERGE GR2c — the
            // Store pad); varying SD: each record releases at the size it was READ (GR12b — LastReadLength is
            // the frame length on a varying input file, the record width on a fixed one; Read pads the area).
            w.Line($"{RuntimeApi.SortRelease(sdLit, varying
                ? RuntimeApi.StrRefMod(tmp, "1", $"(int){RuntimeApi.FileLastReadLength(f)}")
                : RuntimeApi.StrStore(tmp, $"{sdWidth}"))};");
        }
        w.Line($"{RuntimeApi.FileClose(f)};   // implicit CLOSE (GR12c / GR7c)");
        seqIo.EmitStoreFileStatus(input);
        // GR12b: the implicit READ is "as if with the AT END phrase" - at-end never fires a declarative; any
        // OTHER read/close failure still does.
        seqIo.EmitUseHook(input, atEndHandled: true);
    }

    /// <summary>The implicit GIVING transfer for one output file (SORT GR15 / MERGE GR12): REWIND the return
    /// cursor (EACH file receives the full result), OPEN OUTPUT, RETURN→WRITE loop, CLOSE. A fixed-length GIVING
    /// file space-fills a shorter returned record to its record width (GR16c / MERGE GR13c — the connector's
    /// fixed-width fit); a relative GIVING file's key sequence 1..n (GR15b) is the G5 relative slice.</summary>
    private void EmitGivingFile(FileModel output, string sdLit)
    {
        var w = ctx.Writer;
        string f = FileKeyExpr(output);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        w.Line($"{RuntimeApi.SortRewind(sdLit)};   // each GIVING file receives the FULL result (GR15 / MERGE GR12)");
        w.Line($"{RuntimeApi.FileOpenOutput(f, seqIo.ExecutingElementArgs(output))};   // implicit OPEN OUTPUT (GR15a)");
        seqIo.EmitUseHook(output);   // a failed implicit OPEN reaches a USE declarative (GR15a)
        using (w.Block($"while ({RuntimeApi.SortReturn(sdLit, tmp)})"))
            w.Line($"{RuntimeApi.FileWrite(f, tmp, null, seqIo.LinageArg(output))};   // implicit WRITE without optional phrases (GR15b)");
        w.Line($"{RuntimeApi.FileClose(f)};   // implicit CLOSE (GR15c)");
        seqIo.EmitStoreFileStatus(output);
        seqIo.EmitUseHook(output);
    }

    /// <summary>RELEASE (ISO §14.9.32): FROM first MOVEs into the record (GR4 — identical to the explicit MOVE),
    /// then the record's character image goes to the sort store (GR2). A varying SD releases the leading
    /// DEPENDING-ON characters (§13.18.43 GR13 — the current value of the data item names the released length);
    /// a fixed SD releases the named record's image (a shorter secondary record space-fills via the sort store's
    /// fixed-compare space extension; §14.9.40 GR7c).</summary>
    public void EmitRelease(BoundRelease rl)
    {
        var w = ctx.Writer;
        if (rl.From is { } from) move.Emit(new BoundMove(from, [rl.Record]));   // GR4a: MOVE x TO record-name-1
        string sd = FileKeyExpr(rl.File);
        string image = OperandText.RecordAreaImage(rl.Record);   // THE ONE record-area channel (kb/Work PB327)
        if (rl.Varying is { Depending: { } dep })
        {
            // §13.18.43 GR13a: the released record's length = the RECORD VARYING DEPENDING ON item's current value.
            // Out-of-range lengths are EC-SORT-MERGE-RELEASE (GR14b; checking OFF, §18.16).
            w.Line($"{RuntimeApi.SortRelease(sd, RuntimeApi.StrRefMod(image, "1", $"(int){RuntimeApi.TableOcc(PlaceRenderer.Read(dep))}"))};");
            return;
        }
        // GR13b/c (no DEPENDING — incl. a varying m-TO-n SD): the named record's own size; the image renders at
        // exactly that width, so the release carries it.
        w.Line($"{RuntimeApi.SortRelease(sd, image)};");
    }

    /// <summary>RETURN (ISO §14.9.34): pull the next record in key order into the SD record area (GR3); a varying
    /// SD restores the returned record's length into the DEPENDING item (§13.18.43 GR15); INTO then MOVEs the
    /// record area to the receiver (GR5 — skipped when at end); control goes to NOT AT END / AT END (GR3/GR4).
    /// At end the record area is undefined and further RETURNs are EC-SORT-MERGE-RETURN (GR3; checking OFF,
    /// §18.16 — the store deterministically reports at-end again).</summary>
    public void EmitReturn(BoundReturn rt)
    {
        var w = ctx.Writer;
        string sd = FileKeyExpr(rt.File);
        string tmp = $"__srt{ctx.Names.NextSort()}";
        using (w.Block($"if ({RuntimeApi.SortReturn(sd, tmp)})"))
        {
            seqIo.EmitImageInto(rt.RecordArea, tmp);   // GR3 — made available in the record area
            if (rt.Varying is { Depending: { } dep })   // §13.18.43 GR15 — the length restored into DEPENDING
                arith.StoreArith(dep, new NumX(RuntimeApi.SortLastReturnedLength(sd), 0), CobolRounding.Truncation);
            if (rt.Into is { } into)             // GR5 — RETURN then MOVE record-area → identifier-1
                move.Emit(new BoundMove(new BoundFieldOperand(rt.RecordArea), [into]));
            if (rt.NotAtEnd is { } not) Statements.EmitStatementList(not);
        }
        // §14.9.34.4 GR3 — the at end condition: control transfers to imperative-statement-1, and on return from
        // it to the end of the statement. ⚠ The null guard is now DEFENSIVE ONLY, and must not become an empty
        // else that reads as licensed: §14.9.34.2 prints the AT END line unbracketed, so the phrase is required
        // and COBOLNET1850 (SortBinder.BindReturn → StatementValidation.ScreenOmittedRequiredPhrase) rejects the
        // program before codegen. An empty arm here is exactly the wrong answer kb/Work PB350 reports — control
        // falling through onto a record area the same rule leaves undefined.
        using (w.Block("else"))
        {
            if (rt.AtEnd is { } at) Statements.EmitStatementList(at);
        }
    }

    /// <summary>SORT Format 2 — the in-place TABLE sort (ISO §14.9.40 GR18–GR24): a stable
    /// (<c>Enumerable.OrderBy</c>) typed-comparer sort over the element array, copied back into the table (GR24).
    /// Stability preserves the pre-sort relative order of equal keys (GR3c DUPLICATES IN ORDER; without the phrase
    /// the order is undefined, GR4 — stable is conformant). Numeric keys compare by value (GR19 → the relation-
    /// condition rules, §8.8.4.2 — never collated); character keys compare under the GR5-resolved sequence.</summary>
    public void EmitTableSort(BoundTableSort ts)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextSort();
        // The element's STORAGE type is final only after the post-bind whole-group analysis (StoreAsImage) —
        // read it now, at emit time, never at bind time.
        string elem = ts.Table.ElementType;
        // The GR5 carriers are materialized BEFORE the comparer lambda (a local declared inside it would be
        // rebuilt on every comparison) and only for the classes this statement's keys actually use. `declared`
        // is scoped to THIS statement: two keys of one class share the one carrier, and the next statement's
        // carriers are named off its own `id`, so nothing crosses the boundary.
        var declared = new HashSet<string>();
        var weightsArg = ts.Keys
            .Select(k => TableWeightsArg(ts.Collating, CollatingSelection.Of(k.Key.OperandPic), id, declared))
            .ToList();
        w.Line($"var __ta{id} = {ts.ArrayPath};   // SORT table (ISO §14.9.40 Format 2 — in place, GR18/GR24)");
        w.Line($"System.Comparison<{elem}> __tc{id} = (__a, __b) =>");
        w.Line("{");
        w.Indent();
        w.Line("int __c;");
        for (int i = 0; i < ts.Keys.Count; i++)
        {
            var key = ts.Keys[i];
            // GR2: key significance = statement order; GR19a/b: DESCENDING inverts the per-key result.
            w.Line($"__c = {TableCompare(key, "__a", "__b", weightsArg[i])};");
            w.Line($"if (__c != 0) return {(key.Descending ? "-__c" : "__c")};");
        }
        w.Line("return 0;   // GR19c — equal on every key; OrderBy stability keeps the pre-sort order (GR3c)");
        w.Outdent();
        w.Line("};");
        // Through CobolTable.Sorted, never a bare Enumerable.OrderBy: the framework's array sort re-throws a
        // comparer's exception as InvalidOperationException, which would hide a key comparison's fatal COBOL
        // exception condition from the statement guard (kb/Work PB230 — measured, not deduced).
        w.Line($"var __ts{id} = {RuntimeApi.TableSorted($"__ta{id}", $"__tc{id}")};");
        w.Line($"System.Array.Copy(__ts{id}, __ta{id}, __ts{id}.Length);   // GR24 — placed back in data-name-2");
    }

    /// <summary>The shorter-operand extension a BOOLEAN comparison takes (ISO §8.8.4.2.8 rule 2 — "as though the
    /// shorter operand were extended on the right by sufficient boolean zeros"), spelled exactly as the
    /// relation-condition renderer spells it. Never combined with a collating sequence: GR5 names no sequence for
    /// class boolean, so the two arguments are mutually exclusive by construction.</summary>
    private const string BooleanPad = ", pad: '0'";

    /// <summary>One table-sort key's <c>int</c> comparison expression over element variables <paramref name="a"/>
    /// and <paramref name="b"/>, dispatched on the key's CLASS (ISO §14.9.40 GR19 → the relation-condition rules):
    /// numeric keys compare by VALUE (§8.8.4.2.4 — never through a collating sequence; an image-stored zoned leaf
    /// decodes via its profile), national keys compare their national character positions under the GR5 national
    /// sequence (§8.8.4.2.9), boolean keys compare by boolean value with the shorter operand extended by boolean
    /// ZEROS and no sequence (§8.8.4.2.8), and alphanumeric/ordinary-group keys compare as characters under the GR5
    /// alphanumeric sequence (§8.8.4.2.7). <paramref name="weightsArg"/> already carries that choice.</summary>
    private static string TableCompare(BoundTableSortKey key, string a, string b, string weightsArg)
    {
        string pa = key.MemberPath.Length == 0 ? a : $"{a}.{key.MemberPath}";
        string pb = key.MemberPath.Length == 0 ? b : $"{b}.{key.MemberPath}";
        DataItem k = key.Key;
        // ⛔ A GROUP-USAGE GROUP IS AN ELEMENTARY OPERAND (§13.18.29.4 GR1b/GR2b; D20/PB79) AND MUST NOT TAKE THE
        // AsImage() ARM BELOW (kb/Work PB678, the shape kb/Work PB327 fixed one channel over): a NATIONAL group's
        // operand value is its m national POSITIONS — the generated AsNat() — never AsImage()'s 2m UTF-16BE bytes;
        // a BIT group's is its boolean positions — AsBits() — never the packed bytes. Reading those through
        // AsImage() would compare a national group against the ALPHANUMERIC weights of its byte pairs, which is
        // exactly the defect this dispatch removes, one category over.
        if (k.IsAsIfElementary)
            return k.GroupUsage is GroupUsage.National
                ? RuntimeApi.StrCompare($"{pa}.AsNat()", $"{pb}.AsNat()", weightsArg)
                : RuntimeApi.StrCompare($"{pa}.AsBits()", $"{pb}.AsBits()", BooleanPad + weightsArg);
        // §8.8.4.2.8 — a boolean comparison extends the shorter operand on the RIGHT with boolean zeros, and
        // takes no collating sequence (weightsArg is empty for the boolean class).
        if (k.OperandPic is { Category: PicCategory.Boolean })
            return RuntimeApi.StrCompare(pa, pb, BooleanPad + weightsArg);
        // ⛔ V59 RESIDUE FIX (DA5): IsImageCapable, not the pre-V59 IsCharacterImage. §14.9.40.4 GR8
        // (`cite.py`-verified) makes a key comparison IDENTICAL to a relation condition — key data items are
        // "compared according to the rules for comparison of operands in a relation condition" — and a GROUP
        // operand in a relation condition is class alphanumeric (§8.8.4.2.3 SR2) compared over its
        // representation, which `OperandText` already renders through `AsImage()` gated on IsImageCapable. Testing
        // the stricter predicate here therefore made SORT and `IF a > b` DISAGREE about the very same two group
        // operands: the IF compared their byte images while the SORT threw "no whole-group character image" —
        // a claim V59 falsified, since the codec is emitted for exactly IsImageCapable items. A group with a
        // dynamic member or pointer/object-class leaf is still genuinely imageless and stays loud (kb/Work
        // PB164 + R40 — every NUMERIC leaf kind joined the image), so the wording matches the predicate tested.
        // ⚠ NOT A BUG, AND DELIBERATE: a big-endian two's-complement image is NOT order-preserving across zero,
        // so a group key holding a NEGATIVE binary leaf orders by its bytes rather than its value. That is what
        // the standard prescribes — GR8 defers to the relation-condition rules, and those make a GROUP
        // alphanumeric — and it is exactly what the same group compared with IF already does. A program wanting
        // value order names the ELEMENTARY numeric item as the key, which takes the by-value arm below.
        // (Reaching here the group is an ORDINARY one — the GROUP-USAGE arms above own the national and bit
        // groups, which are elementary operands of their own class rather than alphanumeric byte images.)
        if (k.IsGroup)
            return k.IsImageCapable
                ? RuntimeApi.StrCompare($"{pa}.AsImage()", $"{pb}.AsImage()", weightsArg)
                : LoudValue("int", TierCIsland.Reason($"table-sort key '{k.CobolName}' over a mixed-usage group"));
        return k.Pic switch
        {
            // sending: true — a key comparison REFERENCES the content of both operands, so §14.6.13.2 rule 2
            // applies to a windowed numeric key exactly as it does to an arithmetic operand (kb/Work PB230).
            { Category: PicCategory.Numeric, IsFloat: false } when k.StoreAsImage =>
                $"{RuntimeApi.NumParseImage(pa, k.ProfileName, sending: true)}.CompareTo({RuntimeApi.NumParseImage(pb, k.ProfileName, sending: true)})",
            { Category: PicCategory.Numeric, IsFloat: false } => $"({pa}).CompareTo({pb})",
            { Category: PicCategory.Numeric } => $"({pa}).CompareTo({pb})",   // COMP-1/2 — IEEE value order
            _ => RuntimeApi.StrCompare(pa, pb, weightsArg),
        };
    }

    /// <summary>The runtime key-descriptor array literal for a statement's compile-time key descriptors
    /// (offset/length BYTE windows into the SD record image + the class that selects the comparator and the
    /// numeric profile that decodes a numeric window, ISO §14.9.40.3 SR6e / GR5 / GR8).</summary>
    private static string KeysExpr(IReadOnlyList<BoundSortMergeKey> keys) =>
        RuntimeApi.SortKeyArray(keys.Select(k =>
            $"new({k.Offset}, {k.Length}, {(k.Descending ? "true" : "false")}, {RuntimeApi.SortKeyClass(k.Class)}, "
            + $"{(k.Item is { } ki ? ki.ProfileName : "default")})"));

    /// <summary>The ALPHANUMERIC collation argument for a statement's GR5-resolved sequence: <c>null</c> for the
    /// native order, the compiled <c>__COLLATE</c> carrier when the resolved sequence IS the program collating
    /// sequence, else the statement alphabet's own inline carrier (a non-PCS statement alphabet has no emitted
    /// field — ST108A/ST137A shape).</summary>
    private string WeightsExpr(SortCollation c) =>
        c.Alphanumeric is not { } def ? "null"
        : ReferenceEquals(def, ctx.Data.Collating) ? "__COLLATE"
        : CollationEmit.New(def);

    /// <summary>Its NATIONAL twin (ISO §14.9.40.4 GR5 — the sequence for keys of class national, determined
    /// SEPARATELY): <c>__COLLATE_NAT</c> when the resolved sequence IS the program's national collating sequence,
    /// else the statement alphabet-name-2's own inline carrier, else <c>null</c> for the native national order —
    /// which on the D-N1/D-N3 substrate IS code-point order.</summary>
    private string NatWeightsExpr(SortCollation c) =>
        c.National is not { } def ? "null"
        : ReferenceEquals(def, ctx.Data.NationalCollating) ? "__COLLATE_NAT"
        : CollationEmit.New(def);

    /// <summary>The trailing collation argument a TABLE-sort key comparison takes, chosen by the key's CLASS
    /// (ISO §14.9.40.4 GR5 through the ONE classifier): the alphanumeric carrier for an alphabetic/alphanumeric or
    /// ordinary group key, the national carrier for a key of class national, and NOTHING for the classes GR5 names
    /// no sequence for — a numeric key never reaches here and a BOOLEAN key compares by value (§8.8.4.2.8,
    /// "regardless of their usage"), which is why an alphabet reordering '0' and '1' must not touch it.
    /// A non-PCS statement alphabet is materialized once per statement into a local carrier.</summary>
    private string TableWeightsArg(SortCollation c, CollatingClass cls, int id, HashSet<string> declared) => cls switch
    {
        CollatingClass.Alphanumeric => CarrierArg(c.Alphanumeric, ctx.Data.Collating, "__COLLATE", $"__sw{id}",
            "statement COLLATING SEQUENCE (GR5a)", CollationEmit.New, declared),
        CollatingClass.National => CarrierArg(c.National, ctx.Data.NationalCollating, "__COLLATE_NAT", $"__swn{id}",
            "statement COLLATING SEQUENCE FOR NATIONAL (GR5a)", CollationEmit.New, declared),
        _ => "",
    };

    /// <summary>One trailing carrier argument, emitting the local at most once per statement (two keys of one
    /// class must not declare <c>__sw{id}</c> twice — CS0128).</summary>
    private string CarrierArg<T>(T? def, T? programSequence, string programField, string local, string why,
        Func<T, string> render, HashSet<string> declared) where T : class
    {
        if (def is null) return "";
        if (ReferenceEquals(def, programSequence)) return $", {programField}";
        if (declared.Add(local))
            ctx.Writer.Line($"CobolCollation {local} = {render(def)};   // {why}");
        return $", {local}";
    }
}
