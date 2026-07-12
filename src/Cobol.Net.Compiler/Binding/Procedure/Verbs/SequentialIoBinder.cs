// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
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
            BoundCloseKind kind = phrase.closeOption() is { } opt
                ? opt.LOCK() is not null ? BoundCloseKind.WithLock
                : opt.REEL() is not null || opt.UNIT() is not null ? BoundCloseKind.ReelUnit
                : BoundCloseKind.Normal
                : BoundCloseKind.Normal;
            closes.Add((file, kind));
        }
        return new BoundClose(closes);
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
        fileLock.CheckRecordLockPhrase(file, w.recordLockPhrase(), "WRITE");   // §14.9.51 SR22 → COBOLNET1512
        if (!file.IsSequential) return keyedIo.BindWrite(w, file, record);   // relative/indexed WRITE (ISO 14.9.51 GR29-42)

        // END-OF-PAGE phrases (ISO §14.9.51 GR27b/GR28): blocks[0] = AT EOP, blocks[1] = NOT AT EOP — the grammar
        // rule `writeAtEndOfPage : AT? (END_OF_PAGE|EOP) statementBlock (NOT AT? (END_OF_PAGE|EOP) statementBlock)?`
        // fixes that order (the readAtEnd block shape).
        List<BoundStatement>? atEop = null, notAtEop = null;
        if (w.writeAtEndOfPage() is { } eop)
        {
            // SR19 (the silent-drop bug class): the END-OF-PAGE / NOT END-OF-PAGE phrase requires a LINAGE clause
            // in the file's file description entry — a bind-time rejection, never a dropped branch.
            ctx.Validation.CheckWriteEopLinage(file);                                     // SR19 — pure check
            ctx.Validation.CheckWriteEopAdvancingPage(w.writeBeforeAfter()?.PAGE() is not null);   // SR18 — pure check
            (atEop, notAtEop) = PhraseBlocks.Split(eop.statementBlock(), PhraseBlocks.StartsWithNot(eop), b => host.BindBlocks([b]));
        }
        // SR13: with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES mnemonic (the
        // implementor positioning rules and the logical-page model are mutually exclusive).
        ctx.Validation.CheckWriteAdvancingMnemonic(file, w.writeBeforeAfter() is { } adv
            && adv.dataReference() is { } mref && ctx.Mnemonics.Of(adv).ContainsKey(mref.GetText()));   // SR13 — pure check

        return new BoundWrite(file, record, WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal()),
            BindAdvancing(w.writeBeforeAfter()), UnsupportedOrg(file, "WRITE"), atEop, notAtEop);
    }

    public BoundStatement BindRead(Core.ReadStatementContext r)
    {
        string name = r.fileName().GetText();
        if (!ctx.Data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"READ of undeclared file '{name}'");
        if (!file.IsSequential) return keyedIo.BindRead(r, file);   // relative/indexed READ F1/F2 (ISO 14.9.30; KeyedIo partial)
        fileLock.CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ");   // §14.9.30 SR3/SR4 → COBOLNET1512 (sequential leg: SR-validated, effect residue)
        Place? into = r.readInto()?.dataReference() is { } d ? ctx.Refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
            (atEnd, notAtEnd) = PhraseBlocks.Split(ae.statementBlock(), PhraseBlocks.StartsWithNot(ae), b => host.BindBlocks([b]));
        return new BoundRead(file, into, atEnd, notAtEnd, UnsupportedOrg(file, "READ"));
    }

    public BoundStatement BindRewrite(Core.RewriteStatementContext rw)
    {
        Place? record = rw.recordName()?.dataReference() is { } rn ? ctx.Refs.Resolve(rn) : null;
        FileModel? file = record is not null ? FileOfRecord(record) : null;
        if (file is null || record is null)
            return new BoundUnsupported($"REWRITE record '{rw.recordName()?.GetText()}' not resolvable to a file");
        fileLock.CheckRecordLockPhrase(file, rw.recordLockPhrase(), "REWRITE");   // §14.9.35 SR4 → COBOLNET1512
        if (!file.IsSequential) return keyedIo.BindRewrite(rw, file, record);   // relative/indexed REWRITE (ISO 14.9.35 GR18-25)
        return new BoundRewrite(file, record, WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal()),
            UnsupportedOrg(file, "REWRITE"));
    }

    /// <summary>The FROM operand of a WRITE/REWRITE (a data reference or a literal), or null when absent.</summary>
    public BoundOperand? WriteSource(Core.DataReferenceContext? dref, Core.LiteralContext? lit) =>
        lit is not null ? host.Expr.LiteralOperand(lit) : dref is not null ? host.Expr.FieldOperand(dref) : null;

    /// <summary>Bind the <c>{BEFORE|AFTER} ADVANCING …</c> phrase (ISO §14.9.46), or null for a plain WRITE.
    /// An ADVANCING operand naming a SPECIAL-NAMES mnemonic (<c>XXXXX073 IS MNEMONIC-NAME</c>, SQ207M) positions
    /// per the IMPLEMENTOR's rules for the associated feature (§14.9.46 GR — mnemonic-name-1); this
    /// implementation's rule, inherited from the legacy oracle and encoded by the NIST goldens, is a ZERO-line
    /// advance (the write lands on the current line).</summary>
    private BoundAdvancing? BindAdvancing(Core.WriteBeforeAfterContext? wba)
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

    /// <summary>A loud-reason string when <paramref name="file"/>'s organization is not yet implemented (relative /
    /// indexed in the sequential slice), so the verb emits a runtime not-implemented guard; null when supported.</summary>
    internal static string? UnsupportedOrg(FileModel file, string verb) =>
        // A sort-merge (SD) file may be referenced ONLY by SORT/MERGE/RELEASE/RETURN (ISO §13.4.6 SR3/SR4).
        // Every ISO §12.4.5.10 organization (sequential, line sequential, relative, indexed) now has a dedicated
        // bind/emit path — the relative/indexed verbs route through the KeyedIo partial and OPEN/CLOSE flow
        // through the CobolFile facade's keyed registries. Retained as the single seam a future organization
        // gates on (loud, never silent).
        file.IsSortMerge ? $"{verb} on sort-merge file '{file.CobolName}' — an SD file-name may appear only in SORT/MERGE/RELEASE/RETURN (ISO §13.4.6 SR3/SR4)"
        : null;
}
