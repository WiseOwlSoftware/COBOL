// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking verb binder (Phase-4 track (d) / M2-FILE-1; ISO §14.9.47 UNLOCK,
/// the READ/WRITE/REWRITE record-lock phrases §14.9.30/.51/.35, the RETRY phrase §14.7.9). The SHARING clause +
/// OPEN SHARING phrase + connector-registry semantics live in DataBinder / BindOpen / the runtime; this partial
/// binds UNLOCK and validates the per-verb lock-phrase syntax rules (COBOLNET1512). In the single-run-unit model
/// the record-lock EFFECT of READ/WRITE/REWRITE is a documented no-op (named residue) — only the SR validation
/// and UNLOCK/OPEN-SHARING legs are observable — so the lock phrases are validated but not threaded into the
/// verbs' bound nodes.
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>Bind <c>UNLOCK file [RECORD[S]]</c> (ISO §14.9.47): release this connector's record locks on the
    /// file. SR1 — not a sort/merge file.</summary>
    private BoundStatement BindUnlock(Core.UnlockStatementContext ul)
    {
        // UNLOCK is a COBOL-2002 introduction (§14.9.47), parsed at ALL editions (superset grammar). The edition
        // gate moved to the post-bind VersionConformancePass (rearch PHASE-03 Step 14b) — it fires on the
        // self-identifying BoundUnlock node this method produces, so the binder is edition-agnostic here.
        string name = ul.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"UNLOCK of undeclared file '{name}'");
        if (file.IsSortMerge)
            data.Edition.Error("COBOLNET1512", $"UNLOCK may not name the sort/merge file '{name}' "
                + "(ISO §14.9.47 SR1)");
        return new BoundUnlock(file, ul.RECORDS() is not null || ul.RECORD() is not null);
    }

    /// <summary>Validate a verb's record-lock phrase against the file's effective LOCK MODE (ISO §14.9.30 SR3/SR4
    /// / §14.9.51 SR22 / §14.9.35 SR4 → COBOLNET1512): IGNORING LOCK and WITH LOCK are mutually exclusive; and no
    /// explicit lock phrase is permitted when the effective LOCK MODE is AUTOMATIC (the lock is implicit). Called
    /// from the READ/WRITE/REWRITE binders. Returns the <see cref="BoundRecordLock"/> for documentation (the
    /// runtime effect is a single-run-unit no-op — named residue).</summary>
    private BoundRecordLock CheckRecordLockPhrase(FileModel file, Core.RecordLockPhraseContext? lock_, string verb)
    {
        if (lock_ is null) return BoundRecordLock.None;
        // The record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK) is a COBOL-2002 introduction — bind-time
        // gate at the ONE funnel all READ/WRITE/REWRITE verbs call (rearch migration Cluster 10; the parse-time
        // {is2002()}? predicate is gone). Fixes a latent gap: ReservedWordEditionHints never had a record-lock signature.
        ConstructRegistry.Check(data.Edition.Edition, data.Edition, Constructs.RecordLockPhrase2002, $"a record-lock phrase on {verb}");
        bool ignoring = lock_.IGNORING() is not null;
        bool noLock = lock_.NO() is not null;
        var kind = ignoring ? BoundRecordLock.IgnoringLock
            : noLock ? BoundRecordLock.WithNoLock
            : BoundRecordLock.WithLock;
        if (file.LockMode is { Kind: LockKind.Automatic })
            data.Edition.Error("COBOLNET1512", $"{verb} on file '{file.CobolName}': an explicit record-lock "
                + "phrase (IGNORING LOCK / WITH LOCK / WITH NO LOCK) may not be specified when the file's LOCK "
                + "MODE is AUTOMATIC (ISO §14.9.30 SR4 / §14.9.51 SR22 / §14.9.35 SR4)");
        return kind;
    }

    /// <summary>Bind a RETRY phrase on a verb (READ/WRITE/REWRITE/DELETE) — the same shape as OPEN's; the count
    /// bounds a re-attempt, SECONDS/FOREVER are single-run-unit no-ops (residue). Returns null when absent.</summary>
    private RetrySpec? BindVerbRetry(Core.RetryPhraseContext? rp) => rp is null ? null : BindRetry(rp);
}
