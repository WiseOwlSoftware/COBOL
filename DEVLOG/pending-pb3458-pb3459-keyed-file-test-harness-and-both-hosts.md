## PB3458 and PB3459: one keyed-file test harness, and the READ ONLY writer's refusal theory on both region hosts

Two low leads from the branch review of PB3414, both in the keyed-store tests.

**PB3458.** `A_refusal_at_every_page_write_of_a_WRITE_is_30_and_gives_the_store_mutex_back` never called
`RegionHostSeam.Use`, so it ran on the machine's region host only while the PB3414 entry and note said both hosts. The
lock-file host's store mutex is a different object, so the claim is host-specific; the theory now takes `Shapes()`
(organization x sharing x region host) and forces the host before its `OperationsOfAWrite` measurement and its loop over
every refused page write and sync. 8 cases, all green on both hosts; the PB3414 text needed no change.

**PB3459.** Five test classes carried private copies of the keyed-file helpers and the store-mutex probe. They are now
`tests/Cobol.Net.Tests.Unit/KeyedFileHarness.cs` (`KeyedOrg`, `Tmp`, `TryDelete`, `LockModeRequiredBy`, `Register` with
`optional`, `access` and `alternateKey`, `Rec`, `Position`, `Write`, `Rewrite`, `Delete`, `Read`, `Seed`, `SeedRecords`,
`MutexHeld`) and every copy is deleted, with no forwarder left. The sweep found three more classes with the same keyed
helpers (`CrossRunUnitKeyedStoreDriftTests`, `KeyedClosePermanentErrorWitnessTests`, `ReadOnlyWriterReleaseDriftTests`)
and converted them too. The three-organization classes (`FileLockPostureDriftTests`, `SharingRefusalStatusTests`,
`PermanentErrorOutranksRecordLockTests`, whose `Org` includes Sequential) were left. One check tightened: the retry
witness in `CrossRunUnitKeyedStoreDriftTests` asserted "not Held"; through `MutexHeld` it now also fails on an error
answer from the host.

Self-review: no production code changed; the seven converted classes' 231 cases pass. No inventory row claimed or closed.
