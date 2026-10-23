## PB3533: a path names a physical file only when it is asked - the kept state and the store table tell a replaced file by its host identity

**The defect (kb/Work PB3533, a review lead of PB3493/PB3494).** What a CLOSE under commit control keeps was matched to its
physical file by the path alone, and so was the run unit's store table. On Linux a file can be removed (or renamed away)
under an open handle and another made at its path; the connector's re-OPEN then adopted the kept handle on the removed file,
read its records and committed to it, and a second connector of the run unit opening the path attached to the removed file's
store.

**Re-probe.** Ten new witnesses, committed alone on the unfixed runtime (6df15af12) and run on CI's Linux unit leg under WSL,
failed and nothing else did (35588 passed): the re-OPEN kept the transaction; a re-OPEN of a file another run unit holds under
SHARING WITH NO OTHER answered '00' where §9.1.13.9 1) owes '61'; the second connector read `0002A002`, the removed file's
record, instead of the new file's `0002B002`. On Windows the kept handle's share mode admits no delete, so the host refuses the
replacement and each witness checks that instead.

**Fix.** The host's identity of a physical file (`PhysicalFileIdentity`: `statx`'s device and inode on Linux, `FILE_ID_INFO`'s
volume serial and 128-bit file id on Windows) in one place, `IO/Sharing/HostFileIdentity.cs`, which is now also the runtime's
only `statx` binding (`OfdRegionLocks` reads its file type there), asked through `HostFile.IdentityOf` and
`HostFile.IdentityAt` (the Windows path probe opens with no access, so no share mode refuses it or is disturbed by it).
`KeptFile` carries the identity of the file it was kept on, and an OPEN that finds a present file asks
`FileConnector.EndWhatWasKeptOfAnotherFile` before anything reads the kept state: a path that names another file ends the kept
state (PB3493's decision for a kept file that is not there), whatever the OPEN answers, because §14.9.27.4 GR25 speaks of the
file the path names. It ends rather than retires, since `PhysicalFileTable` still holds one physical file per path. The sibling
arm: `StoreTable` entries carry the identity their store was opened on, an attachment through a handle on another file at the
path gets a store of its own, `Detach` is by store, and `FileRegistry.TransactionHolders` tells transactions apart by store,
so a COMMIT reaches both files'. `KeptIsOfAnotherFile` became `KeptIsOfAnotherPath`. DESIGN-runtime-library.md 2.2.1.7 and
2.2.1.10 say all of it, including what stays path-keyed (Table 19's arbitration and the record-lock table).

**A gate red on the way, fixed at its root.** The first gate (both hosts) failed
`HostWitnessTraitDriftTests.EveryFileSystemRaceWitness_CarriesTheHostTrait`, naming `ConstantNameLiteralPositionDriftTests`: its
type-declaration pattern read `internal readonly record struct PhysicalFileIdentity` as a type named `struct`, the first
`record struct` in `IO/Sharing/` it reached, so every concurrent test that says "struct" became a race witness. The pattern now
reads `record struct` and `record class` as one keyword pair. Before that red, the ten new cases passed on Linux at the fix
commit.

**Goldens.** None: the change is the runtime's host file I/O layer, below any COBOL source a conformance case can write (an
outside party replacing a file mid-run). Witnesses: `CommitRollbackRuntimeTests` (re-OPEN after removal or rename, INPUT and
I-O, both organizations; its '39' and '61' refusals; two connectors on a replaced path), `PhysicalFileIdentityDriftTests`
(the identity primitives on both hosts), `CrossRunUnitKeyedStoreDriftTests` (the store table). No inventory row re-verdicted.

**Gates.** Windows (run 000003-20261023T173714Z): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN ... Conformance 10,427/18,190 · Unit 34,885/35,565 · Characterization 5/36 cases ran ...`, the thirteen added or changed cases among them. Linux (`--legs unit,conformance`): `=== LINUX GATE: GREEN (legs selftests unit conformance selftests-built; HEAD 97a867292) ===`.
