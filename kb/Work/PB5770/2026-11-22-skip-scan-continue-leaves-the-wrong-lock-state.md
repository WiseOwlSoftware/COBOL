## The GR22 skip-scan leaves RecordLockHeldBeforeRead at the skipped record's value (2026-11-22)

Review lead R2-1 of the PB5733 report (w20262068b-PB5733-report.md), filed by the enqueue unit's lead clerk; same file and
mechanism family as this note (`FileRegistry.ApplyPostReadLockActions`). Re-read on main 2026-11-22: `FileRegistry.cs` line ~2400
still assigns `c.RecordLockHeldBeforeRead ??= ...`.

In PB5733's GR22 skip-scan, the `continue` after a refused post-retrieval grant leaves `FileConnector.RecordLockHeldBeforeRead` set
to the SKIPPED record's value. Because `ApplyPostReadLockActions` assigns it with `??=`, the next record's value is never recorded,
and `ReadExceedsRecordMaximum` then restores the wrong lock state (a lock the program never held, or the loss of one it did).
Clear the field at the `continue`, or assign with `=` per record, and add a two-record skip-scan test whose second record is
read-exceeds-maximum. A separate item of the same report, the sequential WRITE/REWRITE completion-time lock window (the result of
`_physical.LockRecord` dropped in `WriteSharedInStatement` and `RewriteSharedInStatement`, root a cross-run-unit statement hold for
`SequentialConnector` like `KeyedConnector.BeginStatement`, PB2660), is the same root as this note with a different fix shape and
needs a runtime redesign; cite §2002:12.3.4.8.3 GR8 and §2023:9.1.16.
