## Entry 2349 — 2026-10-22 10:41 PDT — PB3498: the refused second attempt of FileConnector.Open is pinned, and so is the sibling publication loop

**The lead.** The branch review of PB3482 (lead R1-1) found that `FileConnector.Open`'s second attempt, the OPEN made again
with `FileLockPosture.WhenOwedWritesAreAdmitted` after the host refused the first with the request's Table 19 column held,
had no test for the case where that second attempt is refused too. The PB3482 report had called it unconstructible.

**Re-probe.** The behaviour is correct as it stands; only the witnesses were missing. A mutation that adds a third attempt
turns the new tests red, and so does one that stops restoring the kept column after an unsuccessful attempt.

**Why the report said "no constructible case".** On a real host no outside handle can refuse the second attempt after the
first and still sit beside the INPUT opener the second attempt needs: the second attempt's share mode (`ReadWrite`) admits
every handle that is compatible with that opener's own handle. The case is reachable two ways, both now tests:

- A third run unit publishes a column Table 19 refuses against the request between the two attempts. A new test seam,
  `RunUnitFileLock.HostObserver` (an `AsyncLocal`, the pattern of `KeyedStore.MediumObserver`), wraps the host a lock is
  established through; `ColumnRequestLog` counts the requests and can send the third run unit in at a chosen request, so
  the second attempt is refused at the column ('61', exactly two requests, nothing left behind, the file as it was; the
  same OPEN succeeds once the third run unit has gone, which is the control).
- A connector whose CLOSE under commit control kept a read-only handle and column re-OPENs I-O while an outside reader
  refuses writers: the keyed OPEN gives its kept handle back to try the new share mode, is refused, and again on the second
  attempt. The host is asked four times (replace the kept column, restore it, replace it, restore it), the kept column and
  handle are exactly what the CLOSE left, the connector's re-OPEN INPUT adopts the kept handle and the ROLLBACK gives both
  back. With the third run unit racing the second attempt there are three requests and the kept column stands.

**Sibling sweep.** The other bounded attempt loops: `SidecarLockFiles.Publish` exhaustion is pinned already
(`RegionLocksDriftTests`); `HandleColumnLockHost.Publish` (64 tries) was not. An outside program holding the column's whole
byte range exclusively exhausts it: the answer is `Unavailable`, never a refusal, a fresh request leaves no handle on the
file, and a superseding request leaves the superseded column standing.
`HandleColumnLockHostDriftTests.APublicationThatEveryTryCollidesInIsUnavailableAndLeavesWhatWasHeldStanding` pins it, and
changing the answer to `Refused` turns it red. `FileRegistry.RetryLoop` is the user-visible RETRY discipline, pinned by its
own witnesses (PB1590, PB3416).

**Goldens.** None: no COBOL source or diagnostic changes; the witnesses are unit tests. The tests run on Windows for the
host-specific halves and assert the advisory-lock host's single attempt elsewhere.

**Rows.** None claimed or closed (`closes_rows: []` with its reason).

