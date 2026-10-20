## PB3471: one file-test harness and one organization enum for all three organizations

Four test classes that measure the file lock, the sharing refusals and the record locks over Sequential, Relative and Indexed
(`FileLockPostureDriftTests`, `SharingRefusalStatusTests`, `PermanentErrorOutranksRecordLockTests`,
`RunUnitFileLockDriftTests`) each kept private `Register`, `Seed` and (three of them) their own `Org { Sequential, Relative,
Indexed }` enum, `RunUnitFileLockDriftTests` borrowing `FileLockPostureDriftTests.Org`. PB3459 left them because its `KeyedOrg`
had no Sequential arm.

**Re-probe.** All copies present on this build. The note's fix shape, a Sequential member in `KeyedOrg`, would have been wrong:
ten keyed-store theories (`KeyedStoreReloadFailureDriftTests`, `StoreCoherenceTests`, `ReadOnlyKeyedWriterStoreMutexTests`,
`KeyedClosePermanentErrorWitnessTests`) iterate `Enum.GetValues<KeyedOrg>()` and would have run over a sequential file.

**Fix (rule 5).** `KeyedOrg` became `FileOrg { Sequential, Relative, Indexed }`, the one organization enum, with
`FileHarness.KeyedOrgs` for the keyed-store theories; `KeyedFileHarness` became `FileHarness`. `Register` has a Sequential arm and
a throwing default; the 8-character record (key in 1-4, payload in 5-8) serves all three organizations, so the four classes'
4-character records are gone. `Delete` and the keyed `Read` assert the organization has a key (§14.9.10.3 SR1). `ReadNext` is
new and replaces the three private copies in `IndexedEngineTests`, `RelativeEngineTests` and
`PermanentErrorOutranksRecordLockTests`. The four classes call the harness; every private copy and all four `Org` enums are
deleted, with no forwarder. The drift guard `EveryOrganisationsHandleAccessLiesInThatBand` still pins the count of concrete
`FileConnector` organizations against `FileOrg`, and its message now points at `FileOrg` and `FileHarness.Register`.

**Rows.** None claimed, none closed. **Goldens.** Test refactor only: the 1,746 cases of the six touched classes pass
unchanged. The sibling sweep found no other class spanning the three organizations; 17 other test classes keep a private
`Tmp`/`TryDelete` (a different job, reported as a lead). **Sort and file I/O tests (I11).** The change reaches no new runtime
path; the abnormal-path cells (61/62 sharing refusals, '51' record locks, '34' replay, '43') are the same cells over the shared
helpers.
