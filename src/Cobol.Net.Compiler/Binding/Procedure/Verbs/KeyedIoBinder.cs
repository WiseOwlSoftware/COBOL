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

        // §14.9.30 SR6 forbids NEXT/PREVIOUS/AT END under ACCESS RANDOM and the formats keep INVALID KEY off the
        // sequential read — but the CCVS-85 corpus is lenient about phrase placement (the L1–L3 leniency family),
        // so misplaced phrases are TOLERATED and bound: the emitter's status-first branches make a phrase that
        // cannot fire ('2x' on a sequential read, '1x' on a random read) simply dead, never silently rerouted.
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
                return new BoundUnsupported($"READ KEY '{keyRef.GetText()}' (no matching key of reference)");
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
            host.SeqIo.WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal()), invalid)
        { Lock = lock_, Retry = retry };
    }

    /// <summary>Bind a REWRITE on a RELATIVE/INDEXED file. §14.9.35 SR2: the INVALID KEY phrases shall not be
    /// specified for a relative-organization file in SEQUENTIAL access mode (its rewrite has no key condition —
    /// only '43'/'49' logic errors, which route to exception processing).</summary>
    public BoundStatement BindRewrite(Core.RewriteStatementContext rw, FileModel file, Place record,
        BoundRecordLock lock_, RetrySpec? retry)
    {
        KeyedValidateFile(file);
        // §14.9.35 SR2 forbids INVALID KEY for a relative file in sequential access — tolerated in the default
        // (CCVS-lenient) mode like the L1 leniency that parsed it: a sequential-access relative REWRITE can only
        // raise 4x statuses, so the bound phrase is dead in the status-first branches, never silently rerouted.
        KeyedInvalidKey? invalid =
            rw.rewriteInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;
        return new BoundKeyedRewrite(file, record,
            host.SeqIo.WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal()), invalid)
        { Lock = lock_, Retry = retry };
    }

    // ── DELETE (ISO §14.9.10 Formats 1 and 2) ──────────────────────────────────────────────────────────────────

    /// <summary>Bind <c>DELETE file RECORD</c> (ISO §14.9.10 Format 1). SR1: never for sequential organization;
    /// SR2: no INVALID KEY phrases in sequential access mode (the deletion target is the prior READ's record, GR2
    /// — there is no key condition to raise).</summary>
    public BoundStatement BindDelete(Core.DeleteStatementContext del)
    {
        string name = del.fileName().GetText();
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"DELETE of undeclared file '{name}'");
        if (file.IsSequential)
        {
            ctx.Edition.Error("COBOLNET0865", $"DELETE RECORD shall not be specified for sequential-organization "
                + $"file '{name}' (ISO §14.9.10 SR1)");
            return new BoundUnsupported($"DELETE on sequential file '{name}'");
        }
        KeyedValidateFile(file);
        // §14.9.10 SR2 forbids INVALID KEY in sequential access mode — tolerated in the default (CCVS-lenient)
        // mode: a sequential-access DELETE raises only 4x statuses, so the phrase is dead, never misrouted.
        KeyedInvalidKey? invalid =
            del.deleteInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), PhraseBlocks.StartsWithNot(ik)) : null;
        return new BoundKeyedDelete(file, invalid)
        { Retry = fileLock.BindVerbRetry(del.retryPhrase()) };   // §14.7.9 / §14.9.10 GR6
    }

    /// <summary>Bind <c>DELETE FILE file</c> (ISO §14.9.10 Format 2 — COBOL-2023). The construct parses at every
    /// edition; its introduction gate moved to the post-bind VersionConformancePass (rearch PHASE-03 Step 14b),
    /// firing on the self-identifying <see cref="BoundKeyedDeleteFile"/> node (COBOLNET0900 below 2023).</summary>
    public BoundStatement BindDeleteFile(Core.DeleteFileStatementContext df)
    {
        string name = df.fileName().GetText();
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"DELETE FILE of undeclared file '{name}'");
        List<BoundStatement>? on = null, notOn = null;
        if (df.deleteFileOnException() is { } ex)
            (on, notOn) = PhraseBlocks.Split(ex.statementBlock(), PhraseBlocks.StartsWithNot(ex), b => host.BindBlocks([b]));
        return new BoundKeyedDeleteFile(file, on, notOn)
        { Retry = fileLock.BindVerbRetry(df.retryPhrase()) };   // §14.7.9 / §14.9.10 GR15 — the '62' re-attempt
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind START. SR1: access shall be sequential or dynamic; SR3: NOT EQUAL is not a valid operator;
    /// SR5: a relative operand shall be the RELATIVE KEY item; SR6: an indexed operand names a key or a
    /// leftmost-coincident shorter item (a generic key, matched by storage position); GR8/GR15: a missing KEY
    /// phrase means EQUAL on the relative key / prime key. FIRST/LAST are 2002+ (edition-gated here).</summary>
    public BoundStatement BindStart(Core.StartStatementContext st)
    {
        string name = st.fileName().GetText();
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"START of undeclared file '{name}'");
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
            if (operand is null)
                return new BoundUnsupported($"START on '{name}' with no resolvable RELATIVE KEY item");
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
                return new BoundUnsupported($"START KEY operand '{operand.Item.CobolName}' (not a key of '{name}')");
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
