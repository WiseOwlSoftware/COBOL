// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking verb binder (ISO §14.9.47 UNLOCK, the READ/WRITE/REWRITE
/// record-lock phrases §14.9.30/.51/.35, the RETRY phrase §14.7.9). The SHARING clause + OPEN SHARING phrase +
/// connector-registry semantics live in DataBinder / BindOpen / the runtime; this collaborator binds UNLOCK,
/// validates the per-verb lock-phrase syntax rules (COBOLNET1512), and supplies the lock/RETRY services the
/// verb binders thread into their bound nodes — READ/WRITE/REWRITE carry <c>Lock</c>+<c>Retry</c> (all
/// organizations), DELETE RECORD / DELETE FILE carry <c>Retry</c> (§14.9.10 GR6/GR15) — which the emitters
/// route through the runtime's governed entries (ReadLockGovern/ReadShared/WriteShared/RewriteShared/
/// DeleteShared). <c>BindRetry</c> stays a shared spine member on the host (OPEN consumes it directly).
/// </summary>
internal sealed class FileLockBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>UNLOCK file [RECORD[S]]</c> (ISO §14.9.47): release this connector's record locks on the
    /// file. SR1 — not a sort/merge file.</summary>
    public BoundStatement BindUnlock(Core.UnlockStatementContext ul)
    {
        // UNLOCK is a COBOL-2002 introduction (§14.9.47), parsed at ALL editions (superset grammar). The edition
        // gate moved to the post-bind VersionConformancePass (rearch PHASE-03 Step 14b) — it fires on the
        // self-identifying BoundUnlock node this method produces, so the binder is edition-agnostic here.
        string name = ul.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639); the old
        // BoundUnsupported staged an UNDECLARED file-name to a run-time loud with no compile-time word.
        if (!ctx.Validation.ResolveFile(name, "UNLOCK", out var file)) return new BoundNop();
        if (file.IsSortMerge)
            ctx.Edition.Error("COBOLNET1512", $"UNLOCK may not name the sort/merge file '{name}' "
                + "(ISO §14.9.47 SR1)");
        return new BoundUnlock(file, ul.RECORDS() is not null || ul.RECORD() is not null);
    }

    /// <summary>Validate a verb's record-lock phrase against the file's effective LOCK MODE (ISO §14.9.30 SR3/SR4
    /// / §14.9.51 SR22 / §14.9.35 SR4 → COBOLNET1512): IGNORING LOCK and WITH LOCK are mutually exclusive; and no
    /// explicit lock phrase is permitted when the effective LOCK MODE is AUTOMATIC (the lock is implicit). Called
    /// from the READ/WRITE/REWRITE binders, which thread the returned <see cref="BoundRecordLock"/> into their
    /// bound nodes for the emitters' governed-verb routing (§9.1.16).</summary>
    public BoundRecordLock CheckRecordLockPhrase(FileModel file, Core.RecordLockPhraseContext? lock_, string verb)
    {
        if (lock_ is null) return BoundRecordLock.None;
        // The record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK) is a COBOL-2002 introduction; its edition
        // gate (RecordLockPhrase2002) fires on RECOGNITION in the VersionConformancePass parse-arm
        // (VisitRecordLockPhrase, the verb named from the parent statement type); Step 14h.4a. This method keeps only
        // the §14.9.30/.51/.35 SR validation (IGNORING/WITH-LOCK exclusivity vs the effective LOCK MODE).
        bool ignoring = lock_.IGNORING() is not null;
        bool noLock = lock_.NO() is not null;
        var kind = ignoring ? BoundRecordLock.IgnoringLock
            : noLock ? BoundRecordLock.WithNoLock
            : BoundRecordLock.WithLock;
        if (file.LockMode is { Kind: LockKind.Automatic })
            ctx.Edition.Error("COBOLNET1512", $"{verb} on file '{file.CobolName}': an explicit record-lock "
                + "phrase (IGNORING LOCK / WITH LOCK / WITH NO LOCK) may not be specified when the file's LOCK "
                + "MODE is AUTOMATIC (ISO §14.9.30 SR4 / §14.9.51 SR22 / §14.9.35 SR4)");
        return kind;
    }

    /// <summary>Bind a RETRY phrase on a verb (READ/WRITE/REWRITE/DELETE) — the same shape as OPEN's; the count
    /// bounds a re-attempt, SECONDS/FOREVER are single-run-unit no-ops (residue). Returns null when absent.</summary>
    public RetrySpec? BindVerbRetry(Core.RetryPhraseContext? rp) => rp is null ? null : host.BindRetry(rp);
}
