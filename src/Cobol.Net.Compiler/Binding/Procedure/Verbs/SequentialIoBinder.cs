// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The sequential file-I/O verb binder (P7 Step 10h — a real collaborator over
/// <see cref="BinderContext"/>, carved from the core partial; ISO §14.9.27/.7/.51/.30/.35; COBOLNET_DESIGN
/// §8): OPEN/CLOSE/WRITE/READ/REWRITE with the keyed reroutes (the emitter-mirror SeqIo↔KeyedIo cycle —
/// KeyedIo reaches <see cref="WriteSource"/> back through the host accessor), the FROM operand, ADVANCING
/// (the SPECIAL-NAMES mnemonic zero-advance via <see cref="BinderContext.Mnemonics"/>), and the SD loud-guard
/// seam. The WRITE SR13/SR18/SR19 + OPEN SR8 checks live in <c>StatementValidation</c> (pure checks);
/// <c>BindRetry</c> stays a shared spine member on the host.</summary>
internal sealed class SequentialIoBinder(BinderContext ctx, StatementBinder host, KeyedIoBinder keyedIo,
    FileLockBinder fileLock)
{
    public BoundStatement BindOpen(Core.OpenStatementContext o)
    {
        var opens = new List<(FileModel, BoundOpenMode, string?)>();
        SharingMode? sharing = null;
        RetrySpec? retry = null;
        foreach (var clause in o.openClause())
        {
            BoundOpenMode mode = MapOpenMode(clause.openMode());
            // OPEN SHARING phrase (ISO §14.9.27) overrides the SELECT SHARING clause for this OPEN; the RETRY
            // phrase (ISO §14.7.9) governs a locked-file re-attempt. Both are per-statement.
            // OPEN SHARING phrase (§14.9.27) — COBOL-2002. The edition gate moved to the post-bind
            // VersionConformancePass (Step 14c), firing on BoundOpen.SharingOverride; the binder just records it.
            if (clause.sharingPhrase() is { } sp && sp.sharingMode() is { } sm)
                sharing = MapSharingMode(sm);
            if (clause.retryPhrase() is { } rp) retry = host.BindRetry(rp);
            foreach (var spec in clause.openFileSpec())
            {
                string name = spec.dataReference().GetText();
                if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
                    return new BoundUnsupported($"OPEN of undeclared file '{name}'");
                // §14.9.27 SR8: OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause.
                ctx.Validation.CheckOpenSharingAllOther(file, sharing ?? file.Sharing);   // §14.9.27 SR8 — pure check
                opens.Add((file, mode, UnsupportedOrg(file, "OPEN")));
            }
        }
        return new BoundOpen(opens) { SharingOverride = sharing, Retry = retry };
    }

    /// <summary>Map a SHARING mode context (ISO §12.4.5.15) at the binder layer (the DataBinder twin serves the
    /// SELECT clause).</summary>
    private static SharingMode MapSharingMode(Core.SharingModeContext m) =>
        m.READ() is not null ? SharingMode.ReadOnly
        : m.NO() is not null ? SharingMode.NoOther
        : SharingMode.AllOther;

    public BoundStatement BindClose(Core.CloseStatementContext c)
    {
        var closes = new List<(FileModel, BoundCloseKind)>();
        foreach (var phrase in c.closeFilePhrase())
        {
            string name = phrase.fileName().GetText();
            if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
                return new BoundUnsupported($"CLOSE of undeclared file '{name}'");
            // §13.4.6.3 SR3: an SD file-name in a CLOSE — the statement previously compiled and ran against an
            // unregistered connector whose fail-open status read '00' (kb/Work PB140).
            if (ctx.Validation.ScreenSortMergeFile(file, "CLOSE") is { } sd)
                return new BoundUnsupported(sd);
            // §14.9.6.3 SR1: "The NO REWIND, REEL, and UNIT phrases may be used only with files that are of
            // sequential organization" (record and line sequential both, §9.1.7.2). WITH LOCK is not
            // organization-restricted. The old acceptance degraded to a stale FILE STATUS value at run time.
            if (phrase.closeOption() is { } o && o.LOCK() is null && !file.IsSequential)
            {
                ctx.Edition.Error(DiagnosticCatalog.ClosePhraseOrganization,
                    $"CLOSE '{name}' with the {(o.REWIND() is not null ? "NO REWIND" : "REEL/UNIT")} phrase — "
                    + $"the phrase may be used only with a sequential-organization file (ISO §14.9.6.3 SR1)");
                return new BoundUnsupported($"CLOSE phrase on non-sequential file '{name}'");
            }
            // The four Table-14 forms (§14.9.6.4 GR3, Non-unit column — every file here is a disk file):
            // REEL/UNIT [FOR REMOVAL] all map to symbol e (successful no-op, file stays open, '07' — the FOR
            // REMOVAL variant's Non-unit cell is the SAME e, which is why opt.REMOVAL() deliberately has no
            // consumer); WITH NO REWIND is c,g (the file IS closed and the status is '07' — it previously fell
            // into the Normal arm and reported '00', kb/Work PB141).
            BoundCloseKind kind = phrase.closeOption() is { } opt
                ? opt.LOCK() is not null ? BoundCloseKind.WithLock
                : opt.REEL() is not null || opt.UNIT() is not null ? BoundCloseKind.ReelUnit
                : opt.REWIND() is not null ? BoundCloseKind.NoRewind
                : BoundCloseKind.Normal
                : BoundCloseKind.Normal;
            closes.Add((file, kind));
        }
        return new BoundClose(closes)
        {
            // §14.9.6.4 GR5 (report files): the raise site is the emitter's; the CHECKING state is bind-time.
            ReportNotTerminatedCheck = ctx.EcState.Turn.Enabled("EC-REPORT-NOT-TERMINATED", null, c.Start.Line),
        };
    }

    public BoundStatement BindWrite(Core.WriteStatementContext w)
    {
        Place? record = null;
        FileModel? file = null;
        if (w.recordName()?.dataReference() is { } rn && ctx.Refs.Resolve(rn) is { } place)
        {
            record = place;
            file = FileOfRecord(place);
        }
        else if (w.fileName() is { } fn && ctx.Data.FilesByName.TryGetValue(fn.GetText(), out var f) && f.AreaRecord is { } far)
        {
            // The file-name fallback has no named record — write the WHOLE record area through the largest
            // record's view (FileModel.AreaRecord, ISO §13.4.2).
            file = f;
            record = ctx.Refs.ResolveItem(far);
        }
        if (file is null || record is null)
            return new BoundUnsupported($"WRITE record '{w.recordName()?.GetText() ?? w.fileName()?.GetText()}' not resolvable to a file");
        // The WRITE lock/RETRY phrases (§14.9.51 Format 1/2 — [retry-phrase] [WITH LOCK | WITH NO LOCK]) bind
        // for EVERY organization; the emitter routes a lock-relevant statement through the governed runtime entry.
        BoundRecordLock wlock = fileLock.CheckRecordLockPhrase(file, w.recordLockPhrase(), "WRITE");   // §14.9.51 SR22 → COBOLNET1512
        RetrySpec? wretry = fileLock.BindVerbRetry(w.retryPhrase());                                    // §14.7.9 / §14.9.51 GR16
        if (!file.IsSequential) return keyedIo.BindWrite(w, file, record, wlock, wretry);   // relative/indexed WRITE (ISO 14.9.51 GR29-42)

        // END-OF-PAGE phrases (ISO §14.9.51 GR27b/GR28): blocks[0] = AT EOP, blocks[1] = NOT AT EOP — the grammar
        // rule `writeAtEndOfPage : AT? (END_OF_PAGE|EOP) statementBlock (NOT AT? (END_OF_PAGE|EOP) statementBlock)?`
        // fixes that order (the readAtEnd block shape).
        List<BoundStatement>? atEop = null, notAtEop = null;
        if (w.writeAtEndOfPage() is { } eop)
        {
            // SR19 (the silent-drop bug class): the END-OF-PAGE / NOT END-OF-PAGE phrase requires a LINAGE clause
            // in the file's file description entry — a bind-time rejection, never a dropped branch.
            ctx.Validation.CheckWriteEopLinage(file);                                     // SR19 — pure check
            ctx.Validation.CheckWriteEopAdvancingPage(AnyAdvancePage(w.writeBeforeAfter()));   // SR18 — pure check
            (atEop, notAtEop) = PhraseBlocks.Split(eop.statementBlock(), PhraseBlocks.StartsWithNot(eop), b => host.BindBlocks([b]));
        }
        // SR13: with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic (the
        // implementor positioning rules and the logical-page model are mutually exclusive).
        ctx.Validation.CheckWriteAdvancingMnemonic(file, w.writeBeforeAfter()?.writeAdvancePhrase()
            .Any(p => p.dataReference() is { } mref && ctx.Mnemonics.Of(p).ContainsKey(mref.GetText())) ?? false);   // SR13 — pure check

        var (beforeAdv, afterAdv) = BindAdvancingPair(w.writeBeforeAfter());
        return new BoundWrite(file, record, WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal(), w.writeFrom()?.functionCall()),
            beforeAdv, UnsupportedOrg(file, "WRITE"), atEop, notAtEop)
        { Lock = wlock, Retry = wretry, AfterAdvancing = afterAdv };
    }

    public BoundStatement BindRead(Core.ReadStatementContext r)
    {
        string name = r.fileName().GetText();
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"READ of undeclared file '{name}'");
        if (!file.IsSequential) return keyedIo.BindRead(r, file);   // relative/indexed READ F1/F2 (ISO 14.9.30; KeyedIo partial)
        Place? into = r.readInto()?.dataReference() is { } d ? ctx.Refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        // The READ lock phrases bind on the sequential organization too — §14.9.30's GR7–GR12 lock rules are
        // ALL-FORMATS rules, and the sequential record's lock identity is its ordinal position (§9.1.16).
        return new BoundRead(file, into, atEnd, notAtEnd, UnsupportedOrg(file, "READ"))
        {
            Lock = fileLock.CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ"),   // §14.9.30 SR3/SR4 → COBOLNET1512
            Retry = fileLock.BindVerbRetry(r.retryPhrase()),                             // §14.7.9 / §14.9.30 GR9
            AdvancingOnLock = r.readAdvancingOnLock() is not null,                       // §14.9.30 GR22
        };
    }

    public BoundStatement BindRewrite(Core.RewriteStatementContext rw)
    {
        Place? record = rw.recordName()?.dataReference() is { } rn ? ctx.Refs.Resolve(rn) : null;
        FileModel? file = record is not null ? FileOfRecord(record) : null;
        if (file is null || record is null)
            return new BoundUnsupported($"REWRITE record '{rw.recordName()?.GetText()}' not resolvable to a file");
        BoundRecordLock rlock = fileLock.CheckRecordLockPhrase(file, rw.recordLockPhrase(), "REWRITE");   // §14.9.35 SR4 → COBOLNET1512
        RetrySpec? rretry = fileLock.BindVerbRetry(rw.retryPhrase());                                      // §14.7.9 / §14.9.35 GR11
        if (!file.IsSequential) return keyedIo.BindRewrite(rw, file, record, rlock, rretry);   // relative/indexed REWRITE (ISO 14.9.35 GR18-25)
        // §14.9.35.3 SR2, arm (a) — "Neither the INVALID KEY phrase nor the NOT INVALID KEY phrase shall be
        // specified for a REWRITE statement that references a file with SEQUENTIAL ORGANIZATION". This arm
        // dropped the parsed phrase on the floor with no diagnostic at all, which is a strictly worse shape than
        // its relative twin in KeyedIoBinder (which at least bound it as dead): the rule has TWO arms and only
        // one of them was ever noticed (kb/Work PB144 — the two-arm dispatch again). A sequential REWRITE raises
        // only 4x statuses, so nothing is rerouted either way; the phrase has no representation on BoundRewrite
        // and needs none, because there is no '2x' invalid-key condition for it to carry.
        if (rw.rewriteInvalidKeyPhrase() is { } ik)
            ctx.Validation.ScreenForbiddenPhrase(true,
                PhraseBlocks.StartsWithNot(ik) ? "NOT INVALID KEY" : "INVALID KEY", "REWRITE",
                "a file with sequential organization", "ISO §14.9.35.3 SR2");
        return new BoundRewrite(file, record, WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal(), rw.rewriteFrom()?.functionCall()),
            UnsupportedOrg(file, "REWRITE"))
        { Lock = rlock, Retry = rretry };
    }

    /// <summary>The FROM operand of a WRITE/REWRITE (a data reference or a literal), or null when absent.</summary>
    /// <summary>The ONE sending-operand binder behind every <c>… FROM</c> phrase (WRITE / REWRITE / RELEASE,
    /// sequential and keyed alike). <paramref name="fc"/> is the function-identifier arm (fix-queue PB10):
    /// §14.9.51.4 GR5a makes <c>WRITE … FROM identifier-1</c> equivalent to
    /// <c>MOVE identifier-1 TO record-name-1</c>, and §8.4.3.1.2 Format 1 makes a function-identifier an
    /// IDENTIFIER that §8.4.3.2.3 SR1 bars only from RECEIVING operands — so it is admissible here and was
    /// rejected outright before. Threaded through THIS helper rather than each of the four call sites, so the
    /// next FROM phrase inherits it.</summary>
    public BoundOperand? WriteSource(Core.DataReferenceContext? dref, Core.LiteralContext? lit,
                                     Core.FunctionCallContext? fc = null) =>
        fc is not null ? host.Intrinsic.IntrinsicOperand(fc)
        : lit is not null ? host.Expr.LiteralOperand(lit)
        : dref is not null ? host.Expr.FieldOperand(dref) : null;

    /// <summary>Bind the <c>{BEFORE|AFTER} ADVANCING …</c> phrase (ISO §14.9.46), or null for a plain WRITE.
    /// An ADVANCING operand naming a SPECIAL-NAMES mnemonic (<c>XXXXX073 IS MNEMONIC-NAME</c>, SQ207M) positions
    /// per the IMPLEMENTOR's rules for the associated feature (§14.9.46 GR — mnemonic-name-1); this
    /// implementation's rule, inherited from the legacy oracle and encoded by the NIST goldens, is a ZERO-line
    /// advance (the write lands on the current line).</summary>
    private static bool AnyAdvancePage(Core.WriteBeforeAfterContext? wba) =>
        wba?.writeAdvancePhrase().Any(p => p.PAGE() is not null) ?? false;

    /// <summary>Bind the ADVANCING phrase(s) of a WRITE (ISO §14.9.51). One phrase → (that, null); the COBOL-2023
    /// combined form → (the BEFORE phrase, the AFTER phrase). SR17: the combined form may not carry PAGE and shall
    /// be one BEFORE + one AFTER; a malformed pair (two BEFOREs / two AFTERs / PAGE) is rejected COBOLNET0862.</summary>
    private (BoundAdvancing? Before, BoundAdvancing? After) BindAdvancingPair(Core.WriteBeforeAfterContext? wba)
    {
        if (wba is null) return (null, null);
        var phrases = wba.writeAdvancePhrase();
        if (phrases.Length == 1) return (BindAdvancing(phrases[0]), null);
        // Combined BEFORE AND AFTER (SR17): exactly one BEFORE and one AFTER, neither PAGE.
        var beforeP = phrases.FirstOrDefault(p => p.BEFORE() is not null);
        var afterP = phrases.FirstOrDefault(p => p.AFTER() is not null);
        if (beforeP is null || afterP is null || AnyAdvancePage(wba))
        {
            ctx.Edition.Error("COBOLNET0862", "the combined WRITE … BEFORE ADVANCING … AFTER ADVANCING form "
                + "(ISO §14.9.51 SR17) shall specify one BEFORE and one AFTER phrase and shall not specify PAGE");
            return (BindAdvancing(phrases[0]), null);
        }
        return (BindAdvancing(beforeP), BindAdvancing(afterP));
    }

    private BoundAdvancing? BindAdvancing(Core.WriteAdvancePhraseContext? wba)
    {
        if (wba is null) return null;
        bool before = wba.BEFORE() is not null;
        if (wba.PAGE() is not null) return new BoundAdvancing(before, true, null);
        BoundOperand lines =
            wba.integerLiteral() is { } il ? new BoundNumericLiteral(il.GetText())
            : wba.dataReference() is { } d ? ctx.Mnemonics.Of(wba).ContainsKey(d.GetText())
                ? new BoundNumericLiteral("0") : host.Expr.FieldOperand(d)
            : wba.literal() is { } lit ? host.Expr.LiteralOperand(lit)
            : new BoundNumericLiteral("1");
        return new BoundAdvancing(before, false, lines);
    }

    private static BoundOpenMode MapOpenMode(Core.OpenModeContext m) =>
        m.OUTPUT() is not null ? BoundOpenMode.Output
        : m.EXTEND() is not null ? BoundOpenMode.Extend
        : m.I_O() is not null ? BoundOpenMode.IO
        : BoundOpenMode.Input;

    /// <summary>The owning <see cref="FileModel"/> of a record reference: the file whose records include the
    /// reference's top-level (01) record. Null if the reference is not an FD record.</summary>
    public FileModel? FileOfRecord(Place record)
    {
        DataItem root = record.Item;
        while (root.Parent is { } p) root = p;
        foreach (var f in ctx.Data.Files)
            if (f.Records.Contains(root)) return f;
        // An inherited GLOBAL FD's record (ISO §13.18.30 — the record-names of a GLOBAL FD are GLOBAL names):
        // the owning file is a CONTAINER's FileModel, present in this unit only through the FilesByName merge
        // (CallBindUnit) — a contained program's WRITE/REWRITE of the owner's record resolves to the owner's
        // ONE connector (IC233A's family; never a second mapping mechanism).
        foreach (var f in ctx.Data.FilesByName.Values)
            if (f.Records.Contains(root)) return f;
        return null;
    }

    /// <summary>The §13.4.6.3 SR3/SR4 sort-merge screen (a BIND-TIME error, kb/Work PB140 — routed through the
    /// ONE <c>StatementValidation.ScreenSortMergeFile</c>), returning the message the verb's bound node keeps
    /// as its loud belt. Every ISO §12.4.5.10 organization (sequential, line sequential, relative, indexed)
    /// has a dedicated bind/emit path — the relative/indexed verbs route through the KeyedIo partial —
    /// so this seam fires only for an SD file, and stays the single seam a future organization gates on.</summary>
    private string? UnsupportedOrg(FileModel file, string verb) => ctx.Validation.ScreenSortMergeFile(file, verb);
}
