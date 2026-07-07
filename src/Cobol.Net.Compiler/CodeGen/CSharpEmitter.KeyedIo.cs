// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// RELATIVE + INDEXED file-I/O emission (ISO/IEC 1989:2023 §14.9.10/.30/.35/.41/.51; COBOLNET_DESIGN §8.3): every
/// keyed verb renders the SSOT <b>status-first</b> shape — perform the operation, capture the two-character I-O
/// status, store the FILE STATUS item, then branch: AT END on first character <c>'1'</c> (statuses 10/14,
/// §9.1.13.4), INVALID KEY on <c>'2'</c> ONLY (§9.1.13.5/§9.1.14 — a 3x/4x failure routes to exception processing,
/// never the INVALID KEY imperative; the legacy's any-failure IsInvalidKey is a known deviation, pinned to spec),
/// and the NOT-phrases on success (<c>'0'</c>, §9.1.14 final rule). OPEN/CLOSE flow through the existing
/// <c>BoundOpen</c>/<c>BoundClose</c> emission — the <c>CobolFile</c> facade dispatches to the keyed connectors.
/// </summary>
public sealed partial class CSharpEmitter
{
    private int _keyedSeq;   // unique-name counter for keyed status/image temporaries (__kstN / __kimN)

    // ── Registration (Main start) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Register a relative/indexed connector (called from <c>EmitFileRegistration</c>): the host path,
    /// record width, access mode, and the key geometry — the RELATIVE KEY digit capacity (drives statuses 14/24,
    /// §9.1.13.4/§14.9.51 GR29a), or the prime + alternate key (offset, length) ranges of the record's character
    /// image (§12.4.5.12 / §12.4.5.6; offsets computed against the generated codec's deterministic layout).</summary>
    private void KeyedEmitRegistration(CodeWriter w, FileModel file)
    {
        if (file.Records.Count == 0) return;   // a SELECT with no FD — never opened with data (sequential convention)
        string name = FileKeyExpr(file), assign = CsLiteral(file.AssignTarget);
        int access = (int)file.AccessMode;   // FileAccessMode ordinals mirror the runtime KeyedAccess enum
        string opt = file.Optional ? "true" : "false";
        // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) for the GR14/§14.9.35
        // GR20 '44' boundary checks; the keyed frames already carry per-record lengths.
        string vary = file.Varying is not null ? $", {file.VaryMin}, {file.VaryMax}" : "";
        if (file.Organization == FileOrganization.Relative)
        {
            int digits = file.RelativeKeyItem?.Pic?.Digits ?? 0;
            w.Line($"CobolFile.RegisterRelative({name}, {assign}, {file.RecordWidth}, {opt}, {access}, {digits}{vary});");
            return;
        }
        if (file.RecordKeyItem is not { } pk || KeyedImageOffset(pk) is not { } pkOff)
        {
            w.Line(LoudStmt($"indexed file '{file.CobolName}': RECORD KEY missing or not locatable in the record "
                + "image (ISO §12.4.5.12)"));
            return;
        }
        w.Line($"CobolFile.RegisterIndexed({name}, {assign}, {file.RecordWidth}, {opt}, {access}, "
            + $"{pkOff}, {pk.ImageWidth}{vary});");
        foreach (var (alt, dups) in file.AlternateKeys)
        {
            if (KeyedImageOffset(alt) is not { } aOff)
            {
                w.Line(LoudStmt($"indexed file '{file.CobolName}': ALTERNATE RECORD KEY '{alt.CobolName}' not "
                    + "locatable in the record image (ISO §12.4.5.6)"));
                continue;
            }
            w.Line($"CobolFile.AddAlternateKey({name}, {aOff}, {alt.ImageWidth}, {(dups ? "true" : "false")});");
        }
    }

    // ── READ (ISO §14.9.30) ────────────────────────────────────────────────────────────────────────────────────

    private void KeyedEmitRead(BoundKeyedRead rd)
    {
        var w = _ctx.Writer;
        FileModel file = rd.File;
        string name = FileKeyExpr(file);
        int id = _keyedSeq++;
        string st = $"__kst{id}", img = $"__kim{id}";
        // The RECORD AREA is the LARGEST record description (FileModel.AreaRecord, ISO §13.4.2 — multi-01 FDs
        // share one area; a READ makes the record available in the WHOLE area, so a shorter Records[0] must not
        // truncate the splice — RL106A's 56/102-char pair left a stale tail).
        Place? area = file.AreaRecord is { } ar ? _refs.ResolveItem(ar) : null;

        if (rd.Kind == KeyedReadKind.Random && file.Organization == FileOrganization.Relative)
        {
            // §14.9.30 GR29 — the random read positions to the RRN held by the RELATIVE KEY item. The slot number
            // travels through the TYPED field (PIC-aware by construction — the field IS a scaled long), never a
            // byte decode (the legacy proved byte-decoding COMP relative keys breaks; brief §2.1).
            if (KeyedRrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative READ on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"CobolFile.SetRelativeKey({name}, {rrn});");
        }
        switch (rd.Kind)
        {
            case KeyedReadKind.Next:
                w.Line($"var {st} = CobolFile.ReadKeyedNext({name}, out var {img});");
                break;
            case KeyedReadKind.Previous:
                w.Line($"var {st} = CobolFile.ReadKeyedPrevious({name}, out var {img});");
                break;
            default:
                // Indexed Format 2: the key VALUE is the key field's current content in the record area
                // (§14.9.30 GR32) — pass the area image; the connector slices the key-of-reference range.
                string keyImage = area is not null ? OperandText.AsString(new BoundFieldOperand(area)) : "\"\"";
                w.Line($"var {st} = CobolFile.ReadKeyed({name}, {rd.KeyIndex}, {keyImage}, out var {img});");
                break;
        }
        // §9.1.16 record-lock governance (Phase 4d): a sharing-active file (or a READ carrying an explicit
        // lock/RETRY phrase) has its just-read status adjusted — 51 when another connector holds the record's
        // lock (unless IGNORING LOCK), else the WITH LOCK / AUTOMATIC lock is acquired. Runs BEFORE the success
        // block so a 51 denial leaves the record area untouched (the record is not made available).
        if (file.Sharing != SharingMode.None || file.LockMode is not null
            || rd.Lock != BoundRecordLock.None || rd.Retry is not null)
        {
            var (retryKind, retryAmount) = RenderRetry(rd.Retry);
            w.Line($"{st} = CobolFile.ReadLockGovern({name}, {st}, {RuntimeRecordLock(rd.Lock)}, "
                + $"{retryKind}, {retryAmount});");
        }
        using (w.Block($"if ({st}[0] == '0')"))
        {
            if (area is not null) EmitImageInto(area, img);
            EmitReadLengthStore(file);   // §13.18.43 GR15 — the just-read length into DEPENDING
            // §14.9.30 GR25 — a sequential READ of a relative file MOVEs the RRN of the record made available
            // into the RELATIVE KEY data item (MOVE rules — the canonical numeric store path).
            if (rd.Kind != KeyedReadKind.Random && file.Organization == FileOrganization.Relative
                && file.RelativeKeyItem is { } rk && _refs.ResolveItem(rk) is { } rkPlace)
                StoreArith(rkPlace, new NumX($"CobolFile.RelativeSlot({name})", 0), CobolRounding.Truncation);
        }
        EmitStoreFileStatus(file);
        EmitUseHook(file, atEndHandled: rd.AtEnd is not null, invalidKeyHandled: rd.InvalidKey?.Invalid is not null);

        // The §9.1.14 / §14.9.30 GR24 transfer-of-control branches, uniform across the read kinds (a phrase
        // whose status family cannot arise for this kind — e.g. INVALID KEY on a sequential read — is simply
        // dead, the leniency-tolerant rendering of a CCVS-misplaced phrase): '2x' → INVALID KEY imperative;
        // '0x' (success) → INTO move + NOT AT END / NOT INVALID KEY; '1x' (10/14, the at-end family,
        // §9.1.13.4) → AT END imperative; any other unsuccessful status takes NO branch (exception
        // processing, §9.1.14 final rule item 1).
        bool into = rd.Into is not null && area is not null;
        bool hasInv = rd.InvalidKey?.Invalid is not null;
        if (hasInv)
            using (w.Block($"if ({st}[0] == '2')"))
                EmitStatementList(rd.InvalidKey!.Invalid!);
        if (into || rd.NotAtEnd is not null || rd.InvalidKey?.NotInvalid is not null)
            using (w.Block($"{(hasInv ? "else " : "")}if ({st}[0] == '0')"))
            {
                if (into) EmitMove(new BoundMove(new BoundFieldOperand(area!), [rd.Into!]));   // GR4 — READ INTO is READ then MOVE
                if (rd.NotAtEnd is { } nae) EmitStatementList(nae);                  // §14.9.30 — NOT AT END on success
                if (rd.InvalidKey?.NotInvalid is { } nik) EmitStatementList(nik);    // §9.1.14 — success only
            }
        if (rd.AtEnd is { } at)
            using (w.Block($"if ({st}[0] == '1')"))
                EmitStatementList(at);                                                // §14.9.30 GR24c
    }

    // ── WRITE (ISO §14.9.51) ───────────────────────────────────────────────────────────────────────────────────

    private void KeyedEmitWrite(BoundKeyedWrite wr)
    {
        var w = _ctx.Writer;
        FileModel file = wr.File;
        string name = FileKeyExpr(file);
        if (wr.From is { } from) EmitMove(new BoundMove(from, [wr.Record]));   // FROM is an implicit MOVE (GR4)
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.51 GR29b/GR32 — random/dynamic: the runtime element pre-set the RELATIVE KEY item with the
            // RRN to write; read it through the typed field.
            if (KeyedRrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative WRITE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"CobolFile.SetRelativeKey({name}, {rrn});");
        }
        int id = _keyedSeq++;
        string st = $"__kst{id}";
        string wimg = OperandText.AsString(new BoundFieldOperand(wr.Record));
        w.Line(VaryingLengthArg(file) is { } wlen
            ? $"var {st} = CobolFile.WriteKeyed({name}, {wimg}, {wlen});"   // §13.18.43 GR13a
            : $"var {st} = CobolFile.WriteKeyed({name}, {wimg});");
        // §14.9.51 GR29a/GR30 — sequential access (incl. EXTEND): the released RRN is MOVEd into the RELATIVE KEY
        // item during execution of the WRITE.
        if (file.Organization == FileOrganization.Relative && file.AccessMode == FileAccessMode.Sequential
            && file.RelativeKeyItem is { } rk && _refs.ResolveItem(rk) is { } rkPlace)
            using (w.Block($"if ({st}[0] == '0')"))
                StoreArith(rkPlace, new NumX($"CobolFile.RelativeSlot({name})", 0), CobolRounding.Truncation);
        EmitStoreFileStatus(file);
        EmitUseHook(file, invalidKeyHandled: wr.InvalidKey?.Invalid is not null);
        KeyedEmitInvalid(st, wr.InvalidKey);
    }

    // ── REWRITE (ISO §14.9.35) ─────────────────────────────────────────────────────────────────────────────────

    private void KeyedEmitRewrite(BoundKeyedRewrite rw)
    {
        var w = _ctx.Writer;
        FileModel file = rw.File;
        string name = FileKeyExpr(file);
        if (rw.From is { } from) EmitMove(new BoundMove(from, [rw.Record]));
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.35 GR21 — random/dynamic: the slot to replace is named by the RELATIVE KEY item.
            if (KeyedRrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative REWRITE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"CobolFile.SetRelativeKey({name}, {rrn});");
        }
        int id = _keyedSeq++;
        string st = $"__kst{id}";
        string rimg = OperandText.AsString(new BoundFieldOperand(rw.Record));
        w.Line(VaryingLengthArg(file) is { } rlen
            ? $"var {st} = CobolFile.RewriteKeyed({name}, {rimg}, {rlen});"   // §13.18.43 GR13a / §14.9.35 GR20
            : $"var {st} = CobolFile.RewriteKeyed({name}, {rimg});");
        EmitStoreFileStatus(file);
        EmitUseHook(file, invalidKeyHandled: rw.InvalidKey?.Invalid is not null);
        KeyedEmitInvalid(st, rw.InvalidKey);
    }

    // ── DELETE RECORD / DELETE FILE (ISO §14.9.10) ─────────────────────────────────────────────────────────────

    private void KeyedEmitDelete(BoundKeyedDelete del)
    {
        var w = _ctx.Writer;
        FileModel file = del.File;
        string name = FileKeyExpr(file);
        if (file.Organization == FileOrganization.Relative && file.AccessMode != FileAccessMode.Sequential)
        {
            // §14.9.10 GR4 — relative random/dynamic deletes the record named by the RELATIVE KEY item.
            if (KeyedRrn(file) is not { } rrn)
            {
                w.Line(LoudStmt($"relative DELETE on '{file.CobolName}' without a resolvable RELATIVE KEY item"));
                return;
            }
            w.Line($"CobolFile.SetRelativeKey({name}, {rrn});");
        }
        // §14.9.10 GR3 — indexed random/dynamic deletes by the PRIME RECORD KEY's current content; DELETE carries
        // no record operand, so the key value is sliced from the record area image (GR8 — the area is unchanged).
        // The RECORD AREA is the LARGEST record description (FileModel.AreaRecord, ISO §13.4.2 — multi-01 FDs
        // share one area; a READ makes the record available in the WHOLE area, so a shorter Records[0] must not
        // truncate the splice — RL106A's 56/102-char pair left a stale tail).
        Place? area = file.AreaRecord is { } ar ? _refs.ResolveItem(ar) : null;
        string image = area is not null ? OperandText.AsString(new BoundFieldOperand(area)) : "\"\"";
        int id = _keyedSeq++;
        string st = $"__kst{id}";
        w.Line($"var {st} = CobolFile.DeleteRecord({name}, {image});");
        EmitStoreFileStatus(file);
        EmitUseHook(file, invalidKeyHandled: del.InvalidKey?.Invalid is not null);
        KeyedEmitInvalid(st, del.InvalidKey);
    }

    private void KeyedEmitDeleteFile(BoundKeyedDeleteFile df)
    {
        var w = _ctx.Writer;
        // §14.9.10 Format 2 applies to EVERY organization (GR13/GR14/GR16): the sequential connector now
        // exposes its host path, so CobolFile.DeleteFile handles it uniformly with relative/indexed (DEVLOG
        // 612, Phase-4 track d). After a successful delete, the same name's next OPEN INPUT reports '35'
        // (file not available) — the golden's round-trip. (Multiple file-names, §14.9.10 GR — `DELETE FILE
        // f1 f2…` — need the fileName+ grammar; a documented follow-up.)
        int id = _keyedSeq++;
        string st = $"__kst{id}";
        w.Line($"var {st} = CobolFile.DeleteFile({FileKeyExpr(df.File)});");
        EmitStoreFileStatus(df.File);
        if (df.OnException is null) EmitUseHook(df.File);   // the ON EXCEPTION phrase suppresses the declarative entirely (§9.1.13.1)
        // §9.1.13.1/§14.9.10: ON EXCEPTION runs on an unsuccessful completion; '05' (absent file) is a SUCCESSFUL
        // completion (GR14) and takes the NOT ON EXCEPTION path.
        if (df.OnException is { } on)
        {
            using (w.Block($"if ({st}[0] != '0')")) EmitStatementList(on);
            if (df.NotOnException is { } not)
                using (w.Block("else")) EmitStatementList(not);
        }
        else if (df.NotOnException is { } not)
            using (w.Block($"if ({st}[0] == '0')")) EmitStatementList(not);
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    private void KeyedEmitStart(BoundKeyedStart sta)
    {
        var w = _ctx.Writer;
        FileModel file = sta.File;
        string name = FileKeyExpr(file);
        int id = _keyedSeq++;
        string st = $"__kst{id}";
        if (sta.Mode != KeyedStartMode.Key)
            w.Line($"var {st} = CobolFile.StartFirstLast({name}, {(sta.Mode == KeyedStartMode.Last ? "true" : "false")});");
        else if (file.Organization == FileOrganization.Relative)
        {
            // §14.9.41 GR9/GR10 — numeric comparison against the RELATIVE KEY item's value (typed read).
            string rrn = $"(long)({NumericRenderer.Align(_num.FieldNum(sta.Operand!), 0)})";
            w.Line($"var {st} = CobolFile.StartRelative({name}, {CsLiteral(sta.Op)}, {rrn});");
        }
        else
        {
            // §14.9.41 GR17 — the comparison uses the operand's current content for the leftmost LENGTH
            // characters (the WITH LENGTH count, else the operand's own length — a generic key compares short).
            // The key comparison collates NATIVE at COBOL-85 (§12.4.5.7 file collating; the program collating
            // sequence does NOT silently apply to keys — brief risk note).
            string len = sta.Length is { } le
                ? $"(int)({NumericRenderer.Align(_num.Render(le), 0)})"
                : sta.Operand!.Item.ImageWidth.ToString();
            w.Line($"var {st} = CobolFile.StartIndexed({name}, {sta.KeyIndex}, {CsLiteral(sta.Op)}, "
                + $"{OperandText.AsString(new BoundFieldOperand(sta.Operand!))}, {len});");
        }
        EmitStoreFileStatus(file);
        EmitUseHook(file, invalidKeyHandled: sta.InvalidKey?.Invalid is not null);
        KeyedEmitInvalid(st, sta.InvalidKey);   // §14.9.41 GR6 — transfer per §9.1.14
    }

    // ── Shared keyed emission helpers ──────────────────────────────────────────────────────────────────────────

    /// <summary>Emit the §9.1.14 transfer-of-control contract over a captured status local: the INVALID KEY
    /// imperative on the <c>'2x'</c> family ONLY (§9.1.13.5 — statuses 30/4x route to exception processing, not
    /// this branch); the NOT INVALID KEY imperative ONLY on successful completion (<c>'0x'</c>, §9.1.14 final
    /// rule item 2).</summary>
    private void KeyedEmitInvalid(string st, KeyedInvalidKey? ik)
    {
        if (ik is null) return;
        var w = _ctx.Writer;
        if (ik.Invalid is { } inv)
        {
            using (w.Block($"if ({st}[0] == '2')")) EmitStatementList(inv);
            if (ik.NotInvalid is { } not)
                using (w.Block($"else if ({st}[0] == '0')")) EmitStatementList(not);
        }
        else if (ik.NotInvalid is { } not)
            using (w.Block($"if ({st}[0] == '0')")) EmitStatementList(not);
    }

    /// <summary>The C# <c>long</c> expression reading the file's RELATIVE KEY item (the RRN travels through the
    /// typed field — §12.4.5.13; the item is OUTSIDE the record, SR3), or null when the file has none.</summary>
    private string? KeyedRrn(FileModel file)
    {
        if (file.RelativeKeyItem is not { } rk || _refs.ResolveItem(rk) is not { } place) return null;
        return $"(long)({NumericRenderer.Align(_num.FieldNum(place), 0)})";
    }

    /// <summary>The item's character offset within the record AREA image (its own 01's image layout — secondary
    /// 01s under an FD are synthesized REDEFINES of the first, so an in-root offset IS the area offset). Mirrors
    /// the binder's <c>KeyedAreaOffset</c> and the generated codec's deterministic pre-order layout: each child
    /// contributes ImageWidth × OCCURS, a REDEFINES child overlays its target's offset (ISO §13.18.44 GR1).</summary>
    private static int? KeyedImageOffset(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        int? found = null;
        var offsets = new Dictionary<DataItem, int>();
        Walk(root, 0);
        return found;

        void Walk(DataItem node, int off)
        {
            if (found is not null) return;
            offsets[node] = off;
            if (ReferenceEquals(node, item)) { found = off; return; }
            int running = off;
            foreach (var c in node.Children)
            {
                int cOff = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out int tOff) ? tOff : running;
                Walk(c, cOff);
                if (found is not null) return;
                if (c.RedefinesTarget is null) running += c.ImageWidth * (c.Occurs ?? 1);
            }
        }
    }

    /// <summary>NEXT SENTENCE detection inside keyed-I/O phrase bodies (AT END / NOT AT END / INVALID KEY /
    /// NOT INVALID KEY / ON EXCEPTION) — the keyed arm of <c>ContainsNextSentence</c>, so every nesting container
    /// the keyed binder produces is covered (a missed container would emit a label-less goto, loud at compile).</summary>
    private static bool KeyedHasNextSentence(BoundStatement s) => s switch
    {
        BoundKeyedRead r => KeyedNs(r.AtEnd) || KeyedNs(r.NotAtEnd)
                            || KeyedNs(r.InvalidKey?.Invalid) || KeyedNs(r.InvalidKey?.NotInvalid),
        BoundKeyedWrite wr => KeyedNs(wr.InvalidKey?.Invalid) || KeyedNs(wr.InvalidKey?.NotInvalid),
        BoundKeyedRewrite rw => KeyedNs(rw.InvalidKey?.Invalid) || KeyedNs(rw.InvalidKey?.NotInvalid),
        BoundKeyedDelete d => KeyedNs(d.InvalidKey?.Invalid) || KeyedNs(d.InvalidKey?.NotInvalid),
        BoundKeyedStart st => KeyedNs(st.InvalidKey?.Invalid) || KeyedNs(st.InvalidKey?.NotInvalid),
        BoundKeyedDeleteFile df => KeyedNs(df.OnException) || KeyedNs(df.NotOnException),
        _ => false,
    };

    private static bool KeyedNs(IReadOnlyList<BoundStatement>? list) =>
        list is not null && ContainsNextSentence(list);
}
