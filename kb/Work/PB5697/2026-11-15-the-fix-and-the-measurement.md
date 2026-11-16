## The fix and the measurement (2026-11-15, branch pb5697-gate-host-exhaustion)

**A correction to the note.** `Insufficient quota to complete the requested service` is the system's message for
ERROR_PAGEFILE_QUOTA (1454, 0x800705AE) and ERROR_WORKING_SET_QUOTA (1453, 0x800705AD), not ERROR_NOT_ENOUGH_QUOTA (1816,
"Not enough quota is available to process this command"). `ctypes.FormatError` on this host prints both messages.
0x800705AF is ERROR_COMMITMENT_LIMIT (1455), and 0x8007000E is E_OUTOFMEMORY.

**The fix.**
- **The signatures.** `scripts/host_resources.py` `SIGNATURES` is the one table. Its primary rows are 1453/1454 as text or
  HRESULT, 1455 and 0x8007000E. Its secondary rows are an empty output, a silent non-zero exit and a torn Brotli decode,
  and they count only beside a primary row.
- **The gate's verdict.** When every red is the host's, the verdict is `NOT RUN [host-exhausted]` (ENVIRONMENT, exit 2).
  It names the resource and the unjudged cases (`<run>/unjudged.txt`). A test red beside them keeps the gate RED and
  names them apart.
- **The landing and the merge queue.** The landing refuses such a record (exit 14). The merge-group classifier reads the
  same table (INFRA).
- **The slot admission.** A waiter first in line with a slot free waits until the host's free commit, less what the
  holders may still commit, covers `GATE_NEED_BYTES`.
- **The cache sibling.** A blob that does not decode is a miss in the compiled-program cache.

**The incident re-judged.** Run 000004-20261115T203340Z's own trx files, read through `failed_texts` and `judge`:
- resources commit and memory;
- 23 Conformance cases unjudged: 4 primary (2 quota, 1 CoreLib 0x8007000E, 1 hostfxr 0x800705AF) and 19 secondary
  (12 empty outputs, 3 silent exits, 4 torn decodes);
- one genuine test red, `Unit.FunctionTypeSettlementTests.AFrozenArgumentDoesNotChangeTheCallsType`. It is an
  `Assert.Equal(True, False)` whose message carries no host text, because the test drops the compile's detail.

So that gate now reads RED for the Unit case alone, with the 23 named apart. Whether that Unit case was the host too
cannot be told from what it recorded. A test that drops a failed compile's diagnostics hides a host signature; that is a
gap in the test's message, not in the classifier.

**The measurement.** Each whole-population gate's tree was sampled read-only every 2 s, from slot to verdict
(`host_resources.tree_commit`: a Toolhelp snapshot, then PrivateUsage summed over the holder pid's descendants).
- Run 000004-20261116T000125Z (agent-a36228bd06e2f81ca): GREEN, Conformance 26,277, build 106 s, tests 538 s. Peak tree
  commit **7.5 GiB**.
- Run 000004-20261116T000652Z (wf_f60b9cb3-27d-27): GREEN, Conformance 26,182, build 2.9 s, tests 643 s. Peak tree
  commit **8.6 GiB**.
- The two together peaked at **12.1 GiB**.
- This branch's own gate, run 000001-20261116T004607Z, was sampled the same way from its start. It ran the whole
  population, with the script self-tests beside the test hosts because the change touches tooling. Its whole tree
  peaked at **12.1 GiB**, and its slot-held job (the build and the test hosts, `peak_commit_bytes` in its record) at
  **7.6 GiB**.
- Two earlier gates were sampled only in their Conformance tails (pids 498152 and 501472, from 16:00 PST). They peaked at
  2.6 GiB each.
- The samples are in the coordination scratch directory (`op-20261115/pb5697/waiting-sample.tsv`,
  `others-sample.tsv`).
- Every gate after this change records its own job peak (`peak_commit_bytes`, PeakJobMemoryUsed).

**The host.** The figures are the coordinator's, 15:45 PST, with no gate holding a slot, confirmed by GetPerformanceInfo
here.
- The commit limit is **137.7 GiB**: 127.7 GB of RAM plus a 10.25 GB paging file. The paging file grows on demand; the
  limit read 147.9 GiB at 16:00.
- 81 GiB was committed already. Processes held 26.5 GiB of it and the kernel pools the rest (paged about 21 to 26 GB,
  nonpaged about 17 to 21 GB).
- The pool tags: ReFS (Reff 5.6 GB in 4.5 million live allocations), the filter manager, File, MmSt, MmCa (about 4.5
  million each) and Toke (3.6 GB). That comes to about 8 KB of kernel pool per cached file on the ReFS Dev Drive, charged
  to commit and trimmed only under physical pressure.
- So the baseline moves by tens of GB with how many files the host has touched since boot. The paging-file size, the
  owner's machine setting, sets the limit.
- The mechanism reads the limit and the charge live, so it stays correct whatever the paging file is set to.

**The threshold.** `GATE_PEAK_COMMIT_BYTES` = 13 GiB, the largest whole-tree peak (12.1 GiB) rounded up. The whole tree
is what the admission reads of a holder. `ADMISSION_MARGIN_BYTES` = 8 GiB, which covers the pool growth a gate's files
cause outside its tree and the host's own work. So `GATE_NEED_BYTES` = 21 GiB, stated in `host_resources.py` and
nowhere else.

**The cap of 3.** On commit alone it fits this host with nothing else running: 81 + 3 × 13 = 120 GiB of the 137.7 GiB
limit. What exhausted the host was three gates beside two probe fleets. At 16:19 an interactive program committing
30 GiB brought free commit down to 26.5 GiB with two gates in their tails. The admission is what keeps a third gate
from starting into that, so no change to the owner's default is recommended. The temporary cap of 2 can expire on
Monday as set, once this lands.
