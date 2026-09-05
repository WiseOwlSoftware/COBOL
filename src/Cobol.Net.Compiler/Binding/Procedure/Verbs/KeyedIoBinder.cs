// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  RELATIVE + INDEXED file organizations — the keyed-I/O bound nodes and binder (ISO/IEC 1989:2023 §9.1.7.3/.4,
//  §12.4.5, §14.9.10 DELETE, §14.9.30 READ F1/F2, §14.9.35 REWRITE, §14.9.41 START, §14.9.51 WRITE; COBOLNET_DESIGN
//  §8). The sequential subsystem is extended, never forked: BindRead/BindWrite/BindRewrite route non-sequential
//  organizations here; OPEN/CLOSE flow through the existing nodes (the CobolFile facade dispatches by connector).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>P7 Step 10h: a real collaborator over <see cref="BinderContext"/> — the SeqIo↔KeyedIo cycle
/// mirrors the emitter side (SequentialIoBinder holds THIS binder for the org reroutes; the WRITE/REWRITE
/// FROM operand reaches back through <c>host.SeqIo.WriteSource</c>). The one-report-per-file
/// <c>_keyedCheckedFiles</c> memo keeps its per-unit lifetime (ONE instance per binder). The nine bound
/// types stayed in <c>Binding/Bound/BoundKeyedIo.cs</c> — the VersionConformancePass edition gates fire on
/// their SHAPE (Kind/Mode/Length/AdvancingOnLock).</summary>
internal sealed class KeyedIoBinder(BinderContext ctx, StatementBinder host, FileLockBinder fileLock)
{
    /// <summary>Files already semantically checked by <see cref="KeyedValidateFile"/> (one report per file).</summary>
    private readonly HashSet<FileModel> _keyedCheckedFiles = [];

    // ── READ (ISO §14.9.30 Formats 1 and 2 on relative/indexed organizations) ─────────────────────────────────

    /// <summary>Bind a READ of a RELATIVE/INDEXED file. The format decision follows the syntax rules: an explicit
    /// NEXT/PREVIOUS is sequential (GR19); ACCESS SEQUENTIAL implies NEXT (SR8); ACCESS DYNAMIC implies NEXT only
    /// when an AT END / NOT AT END phrase is present — otherwise it is a Format-2 random read (SR9); ACCESS RANDOM
    /// is always Format 2 and forbids NEXT/PREVIOUS/AT END (SR6).</summary>
    public BoundStatement BindRead(Core.ReadStatementContext r, FileModel file)
    {
        KeyedValidateFile(file);
        Place? into = r.readInto()?.dataReference() is { } d ? ctx.Refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        KeyedInvalidKey? invalid =
            r.readInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;

        bool next = r.readDirection()?.NEXT() is not null;
        bool previous = r.readDirection()?.PREVIOUS() is not null;
        // READ PREVIOUS (§14.9.30 Format 1) is a COBOL-2002 introduction; the edition gate moved to the post-bind
        // VersionConformancePass (Step 14c), firing on BoundKeyedRead.Kind == Previous.

        // §14.9.30.3 SR6 — "None of the phrases ADVANCING, AT END, NEXT, NOT AT END, or PREVIOUS shall be
        // specified if ACCESS MODE RANDOM is specified in the file control entry for file-name-1." Gated through
        // the ONE severity seam (kb/Work PB144): an error under strict, a warning under --permissive with the
        // bind UNCHANGED, because the CCVS-85 corpus is lenient about phrase placement (the L1–L3 family) and
        // the emitter's status-first branches make a phrase that cannot fire ('1x' on a random read) simply
        // dead, never silently rerouted. SR10/SR11 (the KEY phrase) are hard errors below and always were.
        if (file.AccessMode == FileAccessMode.Random)
        {
            var present = new List<string>();
            if (r.readAdvancingOnLock() is not null) present.Add("ADVANCING");
            if (r.readAtEnd() is { } ae6) present.Add(PhraseBlocks.StartsWithNot(ae6) ? "NOT AT END" : "AT END");
            if (next) present.Add("NEXT");
            if (previous) present.Add("PREVIOUS");
            if (present.Count > 0)
                ctx.Validation.ScreenForbiddenPhrase(true, string.Join(" / ", present), "READ",
                    "a file whose file control entry specifies ACCESS MODE RANDOM", "ISO §14.9.30.3 SR6");
        }
        KeyedReadKind kind = file.AccessMode switch
        {
            FileAccessMode.Random => KeyedReadKind.Random,
            // §14.9.30 SR9 — dynamic: NEXT implied only when an AT END / NOT AT END phrase is present; a bare
            // READ under dynamic access is the Format-2 random read.
            FileAccessMode.Dynamic => previous ? KeyedReadKind.Previous
                : next || r.readAtEnd() is not null ? KeyedReadKind.Next
                : KeyedReadKind.Random,
            // §14.9.30 SR8 — sequential access: NEXT implied.
            _ => previous ? KeyedReadKind.Previous : KeyedReadKind.Next,
        };

        int keyIndex = -1;   // the prime record key (GR31) / the relative key item (GR29) when no KEY phrase
        if (r.readKey()?.dataReference() is { } keyRef)
        {
            // SR10: the KEY phrase only for indexed organization; SR11: it names a declared (prime or alternate)
            // key — matched by STORAGE POSITION, not name (§12.4.5.12 GR4: the key's character positions are
            // implicitly keys in EVERY record description of the file; covers REDEFINES and duplicate names).
            if (file.Organization != FileOrganization.Indexed)
                ctx.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}': the KEY phrase may be "
                    + "specified only when ORGANIZATION IS INDEXED (ISO §14.9.30 SR10)");
            else if (kind != KeyedReadKind.Random)
                ctx.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}' is a Format-2 phrase and "
                    + "cannot combine with NEXT/PREVIOUS/AT END (ISO §14.9.30 general formats)");
            else if (ctx.Refs.Resolve(keyRef) is not { } keyPlace || Model.RecordLayout.KeyIndexByPosition(file, keyPlace.Item) is not { } ki)
            {
                ctx.Edition.Error("COBOLNET0864", $"READ … KEY IS {keyRef.GetText()} on '{file.CobolName}': the "
                    + "operand shall be the RECORD KEY or an ALTERNATE RECORD KEY of the file (ISO §14.9.30 SR11)");
                return new BoundNop();   // reported above — not a deferral (kb/Work PB236)
            }
            else keyIndex = ki;
        }
        // READ … ADVANCING ON LOCK (§14.9.30 record-lock phrase, COBOL-2002); the edition gate moved to the
        // post-bind VersionConformancePass (Step 14c), firing on BoundKeyedRead.AdvancingOnLock.
        return new BoundKeyedRead(file, kind, keyIndex, into, atEnd, notAtEnd, invalid)
        {
            Lock = fileLock.CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ"),
            Retry = fileLock.BindVerbRetry(r.retryPhrase()),
            AdvancingOnLock = r.readAdvancingOnLock() is not null,
        };
    }

    // ── WRITE / REWRITE (ISO §14.9.51 / §14.9.35 on relative/indexed organizations) ────────────────────────────

    /// <summary>Bind a WRITE on a RELATIVE/INDEXED file (called from <c>BindWrite</c> with the record, owning
    /// file, and the already-validated lock/RETRY phrases resolved — §14.9.51 [retry-phrase] [WITH LOCK|WITH NO
    /// LOCK]). The print-control phrases (ADVANCING / END-OF-PAGE) apply to sequential print files only and fail
    /// loud here.</summary>
    public BoundStatement BindWrite(Core.WriteStatementContext w, FileModel file, Place record,
        BoundRecordLock lock_, RetrySpec? retry)
    {
        KeyedValidateFile(file);
        if (w.writeBeforeAfter() is not null || w.writeAtEndOfPage() is not null)
            return new BoundUnsupported($"WRITE ADVANCING / END-OF-PAGE on {file.Organization} file "
                + $"'{file.CobolName}' (ISO §14.9.51 — print-control phrases are for sequential print files)");
        KeyedInvalidKey? invalid =
            w.writeInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;
        return new BoundKeyedWrite(file, record,
            host.SeqIo.WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal(), w.writeFrom()?.functionCall()), invalid)
        { Lock = lock_, Retry = retry };
    }

    /// <summary>Bind a REWRITE on a RELATIVE/INDEXED file. §14.9.35 SR2: the INVALID KEY phrases shall not be
    /// specified for a relative-organization file in SEQUENTIAL access mode (its rewrite has no key condition —
    /// only '43'/'49' logic errors, which route to exception processing).</summary>
    public BoundStatement BindRewrite(Core.RewriteStatementContext rw, FileModel file, Place record,
        BoundRecordLock lock_, RetrySpec? retry)
    {
        KeyedValidateFile(file);
        // §14.9.35.3 SR2, arm (b) — "… or a file with relative organization and sequential access mode". Arm (a),
        // sequential ORGANIZATION, never reaches here and is screened at SequentialIoBinder.BindRewrite, which
        // dropped the phrase entirely; the rule has two arms and only this one was even commented (kb/Work
        // PB144). Gated through the ONE severity seam: error under strict, warning + unchanged bind under
        // --permissive, where the phrase stays dead in the status-first branches.
        KeyedInvalidKey? invalid = null;
        if (rw.rewriteInvalidKeyPhrase() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(
                file.Organization == FileOrganization.Relative
                    && file.AccessMode == FileAccessMode.Sequential,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "REWRITE",
                "a file with relative organization and sequential access mode", "ISO §14.9.35.3 SR2");
            invalid = KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundKeyedRewrite(file, record,
            host.SeqIo.WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal(), rw.rewriteFrom()?.functionCall()), invalid)
        { Lock = lock_, Retry = retry };
    }

    // ── DELETE (ISO §14.9.10 Formats 1 and 2) ──────────────────────────────────────────────────────────────────

    /// <summary>Bind <c>DELETE file RECORD</c> (ISO §14.9.10 Format 1). SR1: never for sequential organization;
    /// SR2: no INVALID KEY phrases in sequential access mode (the deletion target is the prior READ's record, GR2
    /// — there is no key condition to raise).</summary>
    public BoundStatement BindDelete(Core.DeleteStatementContext del)
    {
        string name = del.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
        if (!ctx.Validation.ResolveFile(name, "DELETE", out var file)) return new BoundNop();
        // §13.4.6.3 SR3: an SD file (its SELECT may even carry ORGANIZATION RELATIVE/INDEXED) previously bound
        // and ran against an unregistered connector — the fail-open status read '00' (kb/Work PB140).
        if (ctx.Validation.ScreenSortMergeFile(file, "DELETE") is not null)
            return new BoundNop();   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
        if (file.IsSequential)
        {
            ctx.Edition.Error("COBOLNET0865", $"DELETE RECORD shall not be specified for sequential-organization "
                + $"file '{name}' (ISO §14.9.10 SR1)");
            return new BoundNop();   // reported above — not a deferral (PB236)
        }
        KeyedValidateFile(file);
        // §14.9.10.3 SR2 — the INVALID KEY / NOT INVALID KEY phrases shall not be specified for a DELETE RECORD
        // that references a file in SEQUENTIAL ACCESS MODE. Gated through the ONE severity seam (kb/Work PB144):
        // an error under strict, a warning under --permissive with the bind unchanged — a sequential-access
        // DELETE raises only 4x statuses, so the tolerated phrase stays dead in the status-first branches and is
        // never silently rerouted.
        KeyedInvalidKey? invalid = null;
        if (del.deleteInvalidKeyPhrase() is { } ik)
        {
            ctx.Validation.ScreenForbiddenPhrase(file.AccessMode == FileAccessMode.Sequential,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "DELETE RECORD",
                "a file that is in sequential access mode", "ISO §14.9.10.3 SR2");
            invalid = KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik));
        }
        return new BoundKeyedDelete(file, invalid)
        { Retry = fileLock.BindVerbRetry(del.retryPhrase()) };   // §14.7.9 / §14.9.10 GR6
    }

    /// <summary>Bind <c>DELETE FILE file</c> (ISO §14.9.10 Format 2 — COBOL-2023). The construct parses at every
    /// edition; its introduction gate moved to the post-bind VersionConformancePass (rearch PHASE-03 Step 14b),
    /// firing on the self-identifying <see cref="BoundKeyedDeleteFile"/> node (COBOLNET0900 below 2023).</summary>
    public BoundStatement BindDeleteFile(Core.DeleteFileStatementContext df)
    {
        // §14.9.10.2 Format 2 (kb/Work PB134): [OVERRIDE] {file-name-1}…. GR12: multiple names execute as if
        // a separate DELETE FILE statement had been written for EACH, in order — each element re-binds the
        // phrase blocks, the exact textual-duplication semantics the rule states. The OVERRIDE phrase precedes
        // the WHOLE name list, so GR12's as-if duplication carries it onto every element (kb/Work PB196).
        bool overridden = df.OVERRIDE() is not null;   // §14.9.10.4 GR18 second sentence
        var steps = new List<BoundStatement>();
        foreach (var fn in df.fileName())
        {
            string name = fn.GetText();
            // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639). GR12's as-if
            // duplication means each name is its own statement, so a bad one drops its own element and the
            // others still bind.
            if (!ctx.Validation.ResolveFile(name, "DELETE FILE", out var file)) continue;
            // §14.9.10.3 SR3 / §13.4.6.3 SR3: DELETE FILE of the sort-merge file rejects at bind time —
            // it previously compiled and the statement's TWO status channels answered oppositely (kb/Work PB140).
            if (ctx.Validation.ScreenSortMergeFile(file, "DELETE FILE") is not null)
                continue;   // the screen REPORTED; a loud runtime stage on top would re-answer it (PB236)
            List<BoundStatement>? on = null, notOn = null;
            if (df.deleteFileOnException() is { } ex)
                (on, notOn) = PhraseBlocks.Split(ex.statementBlock(), PhraseBlocks.StartsWithNot(ex), b => host.BindBlocks([b]));
            steps.Add(new BoundKeyedDeleteFile(file, on, notOn)
            {
                Retry = fileLock.BindVerbRetry(df.retryPhrase()),   // §14.7.9 / §14.9.10 GR15 — the '62' re-attempt
                Override = overridden,                              // §14.9.10.4 GR18 — suppress the attribute match
            });
        }
        return steps.Count == 1 ? steps[0] : new BoundSequence(steps);
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind START. SR1: access shall be sequential or dynamic; SR3: NOT EQUAL is not a valid operator;
    /// SR5: a relative operand shall be the RELATIVE KEY item; SR6: an indexed operand names a key or a
    /// leftmost-coincident shorter item (a generic key, matched by storage position); GR8/GR15: a missing KEY
    /// phrase means EQUAL on the relative key / prime key. FIRST/LAST are 2002+ (edition-gated here).</summary>
    public BoundStatement BindStart(Core.StartStatementContext st)
    {
        string name = st.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639).
        if (!ctx.Validation.ResolveFile(name, "START", out var file)) return new BoundNop();
        if (file.IsSequential)
            return new BoundUnsupported($"START on sequential-organization file '{name}' "
                + "(ISO §14.9.41 SR2 FIRST/LAST positioning — a later slice)");
        KeyedValidateFile(file);
        if (file.AccessMode == FileAccessMode.Random)
            ctx.Edition.Error("COBOLNET0862", $"START on '{name}': the access mode shall be sequential or "
                + "dynamic (ISO §14.9.41 SR1)");
        KeyedInvalidKey? invalid =
            st.startInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;

        if (st.FIRST() is not null || st.LAST() is not null)
        {
            // START FIRST/LAST (§14.9.41) is a COBOL-2002 introduction; the edition gate moved to the post-bind
            // VersionConformancePass (Step 14c), firing on BoundKeyedStart.Mode ∈ {First, Last}.
            return new BoundKeyedStart(file, st.LAST() is not null ? KeyedStartMode.Last : KeyedStartMode.First,
                "==", -1, null, null, invalid);
        }

        var kp = st.startKeyPhrase();
        string op = kp?.comparisonOperator() is { } oc ? ConditionBinder.MapOperator(oc.GetText()) : "==";   // GR8/GR15 — EQUAL
        if (op == "!=")
        {
            ctx.Edition.Error("COBOLNET0862", $"START on '{name}': the relational operator shall not be "
                + "'IS NOT EQUAL TO' (ISO §14.9.41 SR3)");
            op = "==";
        }
        Place? operand = kp?.dataReference() is { } dref ? ctx.Refs.Resolve(dref) : null;
        if (kp is not null && operand is null)
            return new BoundUnsupported($"START KEY operand '{kp.dataReference().GetText()}'");

        // WITH LENGTH (§14.9.41 GR13–GR14 partial-key count) is a COBOL-2002 introduction; the edition gate moved
        // to the post-bind VersionConformancePass (Step 14c), firing on BoundKeyedStart.Length != null.
        BoundExpr? length = kp?.startWithLength()?.arithmeticExpression() is { } le ? host.Expr.BindExpr(le) : null;
        if (length is not null && file.Organization != FileOrganization.Indexed)
            ctx.Edition.Error("COBOLNET0862", $"START … WITH LENGTH on '{name}': the LENGTH phrase requires "
                + "indexed organization (ISO §14.9.41 SR8)");

        if (file.Organization == FileOrganization.Relative)
        {
            if (operand is not null && !ReferenceEquals(operand.Item, file.RelativeKeyItem))
                ctx.Edition.Error("COBOLNET0862", $"START on '{name}': data-name-1 shall be the RELATIVE KEY "
                    + $"item '{file.RelativeKeyItem?.CobolName ?? "(none)"}' (ISO §14.9.41 SR5)");
            operand ??= file.RelativeKeyItem is { } rk ? ctx.Refs.ResolveItem(rk) : null;
            // ⛔ GR8's SUBSTITUTION HAS NO OPERAND, SO THE STATEMENT HAS NO MEANING (kb/Work PB236, row
            // GR-14.9.41.4-8). §14.9.41.4 GR8: "If the KEY phrase is omitted, the START statement behaves as
            // though KEY IS EQUAL TO data-name-1 had been specified, where data-name-1 is the name of the key
            // specified in the RELATIVE KEY clause associated with file-name-1." A relative file in ACCESS
            // SEQUENTIAL may legally omit the RELATIVE KEY clause (§12.4.5.13 imposes no requirement to write
            // it, and KeyedValidateFile only requires it for random/dynamic), so this program is syntactically
            // legal and semantically empty: there is no data-name-1 to substitute. §4.2.2 ¶4 leaves flagging a
            // general rule to the implementor ("An implementation may, but is not required to, flag violations
            // of such rules"), and the alternative to flagging is emitting code that cannot work — so this
            // rejects, in the SAME COBOLNET0862 channel this method already uses for SR1/SR3/SR5/SR6/SR8, and
            // no longer as a silent compile followed by a run-time abort.
            if (operand is null)
            {
                ctx.Edition.Error("COBOLNET0862", $"START on '{name}': the KEY phrase is omitted, so ISO "
                    + "§14.9.41.4 GR8 substitutes KEY IS EQUAL TO the RELATIVE KEY item — but this file control "
                    + "entry has no RELATIVE KEY clause, so there is no key to compare. Add a RELATIVE KEY "
                    + "clause to the SELECT, or write an explicit KEY phrase.");
                return new BoundNop();
            }
            return new BoundKeyedStart(file, KeyedStartMode.Key, op, -1, operand, null, invalid);
        }

        int keyIndex = -1;
        if (operand is not null)
        {
            if (Model.RecordLayout.KeyIndexByPosition(file, operand.Item) is not { } ki)
            {
                ctx.Edition.Error("COBOLNET0862", $"START on '{name}': '{operand.Item.CobolName}' neither names "
                    + "a record key nor begins at the leftmost character position of one with a length not "
                    + "greater than that key (ISO §14.9.41 SR6)");
                return new BoundNop();   // reported above — not a deferral (PB236)
            }
            keyIndex = ki;
        }
        else
            operand = file.RecordKeyItem is { } pk ? ctx.Refs.ResolveItem(pk) : null;   // GR15 — prime key EQUAL
        if (operand is null)
            return new BoundUnsupported($"START on '{name}' with no resolvable RECORD KEY item");
        return new BoundKeyedStart(file, KeyedStartMode.Key, op, keyIndex, operand, length, invalid);
    }

    // ── Shared keyed-I/O helpers ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Build the INVALID/NOT INVALID pair from the phrase's statement blocks — the shared two-branch
    /// shape via the ONE <see cref="PhraseBlocks.Split"/> extractor (P7 Step 10b).</summary>
    private KeyedInvalidKey KeyedInvalidPhrase(Core.StatementBlockContext[] blocks, bool notFirst)
    {
        var (inv, not) = PhraseBlocks.Split(blocks, notFirst, b => host.BindBlocks([b]));
        return new KeyedInvalidKey(inv, not);
    }

    /// <summary>One-time semantic checks of a keyed file's control entry, run on its first keyed verb (the
    /// FILE-CONTROL clauses are captured by DataBinder; the rules live with the verbs that need them):
    /// an INDEXED file requires a RECORD KEY (ISO §12.4.5.1 Format 1 — RECORD KEY is a required clause);
    /// a RELATIVE file in random/dynamic access requires a RELATIVE KEY (§12.4.5.13); the RELATIVE KEY item is an
    /// unsigned integer without 'P' (§12.4.5.13 SR2) and shall NOT be defined within a record of the file
    /// (§12.4.5.13 SR3 — it lives outside, typically WORKING-STORAGE).</summary>
    private void KeyedValidateFile(FileModel file)
    {
        if (!_keyedCheckedFiles.Add(file)) return;
        if (file.Organization == FileOrganization.Indexed && file.HasFd && file.RecordKeyItem is null)
            ctx.Edition.Error("COBOLNET0863", $"indexed file '{file.CobolName}' has no RECORD KEY clause "
                + "(ISO §12.4.5.1 Format 1 — RECORD KEY is required for ORGANIZATION INDEXED)");
        if (file.Organization != FileOrganization.Relative) return;
        if (file.RelativeKeyItem is null && file.AccessMode != FileAccessMode.Sequential)
            ctx.Edition.Error("COBOLNET0863", $"relative file '{file.CobolName}' is ACCESS {file.AccessMode} "
                + "but has no RELATIVE KEY clause (ISO §12.4.5.13 — required for random/dynamic access)");
        if (file.RelativeKeyItem is not { } rk) return;
        if (rk.Pic is not { Category: PicCategory.Numeric, Scale: 0, Signed: false })
            ctx.Edition.Error("COBOLNET0863", $"RELATIVE KEY '{rk.CobolName}' shall be an unsigned integer "
                + "without the symbol 'P' (ISO §12.4.5.13 SR2)");
        DataItem root = rk;
        while (root.Parent is { } p) root = p;
        if (file.Records.Contains(root))
            ctx.Edition.Error("COBOLNET0863", $"RELATIVE KEY '{rk.CobolName}' shall not be defined within a "
                + $"record description of file '{file.CobolName}' (ISO §12.4.5.13 SR3)");
    }
}
