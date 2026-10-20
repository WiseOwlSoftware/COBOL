## PB3411 discharged: the files design already names the current IsHostStoreFailure predicate

PB3411 said `docs/COBOLNET_FILES_DESIGN.md:2502` listed `OutOfMemoryException` in `KeyedConnector.IsHostStoreFailure`'s predicate. Re-probed on origin/main (ed4477ef2): it does not. The doc reads "`IOException`, `UnauthorizedAccessException` - one predicate, `KeyedConnector.IsHostStoreFailure`" (lines 2502-2503), matching `KeyedConnector.cs:455-456` (`failure is IOException or UnauthorizedAccessException`). Commit 057e8ce9a, which filed the PB3416-PB3419 leads, had already dropped the word and did not flip the note.

Sibling sweep: `OutOfMemoryException` appears in no other doc as a keyed-store host-failure reason. The one runtime catch of it under `IO/` is `FileConnector.cs:861`, OPEN's input-output area allocation (ISO 12.4.5.14.3, checked with `cite.py --check`), a different mechanism with its own comment, so it stays.

Register: PB3411 flips to `landed` with `closes_rows: []` and a reason (no inventory row was claimed). No code or design text changed. `work.py check` passes. Gate: see the landing's verdict.
