## PB3415: the comments say which predicate the sequential release asks

kb/Work PB3415 (MINOR, process-only, comment accuracy; the Mythos lane's PB3335 review lead R5-L2). Two summaries still
described the sequential release as asking `FileLockPosture.AdmitsAnotherWriter`, which PB1587 changed:
`StoreCoherenceTests`'s class summary said a `SHARING WITH READ ONLY` sequential writer "still buffers" (its own test at
the end of the class asserts that writer hands the record over at the WRITE), and `AdmitsAnotherWriter`'s summary called
itself "the predicate the sequential write path needs".

**Re-probe.** On ed4477ef2, `git grep -n "sequential organization keeps"` hit StoreCoherenceTests.cs:38, and
`SequentialConnector.ReleaseRecord` flushed under `AdmitsAnotherRunUnit` (§14.9.51.4 GR12, "The successful execution of
a WRITE statement releases a logical record to the operating environment"; §9.1.15 2), the INPUT connector admitted
beside a READ ONLY writer; both `cite.py --check` OK).

**Fix.** Comments and the design doc only. `AdmitsAnotherWriter`'s summary now names its four callers
(`HostFile.OpenConnectorWriteStream`'s shared append stream, `ReleaseRecord`'s ordinal off the medium, the OPEN EXTEND's
mint seed, `EnsureReaderCoherent`'s re-anchoring) and says it does not decide the release; `AdmitsAnotherRunUnit`'s
summary names its two (`KeyedConnector.TakeFileLock`, the release's flush). The test class summary and the test
method's summary say the same. Sibling sweep, `git grep -n AdmitsAnother src tests docs`: DESIGN-runtime-library.md's
PB3335 paragraph counted three `AdmitsAnotherWriter` callers and missed the release's ordinal, fixed; every other
comment (KeyedConnector, SequentialConnector, FileSupport, ReadOnlyWriterReleaseDriftTests, RelativeEngineTests) was
accurate.

**Rows.** None (`closes_rows: []`, comment accuracy only). Self-review: no behaviour change; the caller sets the comments
name are the ones `The_keyed_store_asks_about_any_run_unit_and_the_sequential_release_about_any_other_connector` pins
file by file.
