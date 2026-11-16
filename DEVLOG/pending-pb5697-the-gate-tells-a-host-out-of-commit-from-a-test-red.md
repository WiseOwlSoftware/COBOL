## PB5697: the gate tells a host out of commit from a test red, and a gate waits for the commit it needs

**What was wrong.** Three whole-population gates at once, beside two probe fleets, ran this host out of commit on
2026-11-15. One gate (run 000004-20261115T203340Z of the golden-2002-3 integrator) went `RED` with 24 failed cases, none
of them the branch's: `Insufficient quota to complete the requested service` from `Directory.CreateDirectory`, CoreLib
that would not load (`0x8007000E`), hostfxr that would not load (`0x800705AF`, the paging file too small), and behind
them empty outputs, silent non-zero exits and torn Brotli reads. A reader of `RED` gates again or hunts a defect that is
not there. The slot cap of 3 counts gates; nothing read what the host could hold. The note called the first error
ERROR_NOT_ENOUGH_QUOTA (1816); its message is ERROR_PAGEFILE_QUOTA (1454) and ERROR_WORKING_SET_QUOTA (1453).

**What changed.**
- `scripts/host_resources.py` holds the one table of host-resource signatures. Primary rows are the host's own errors
  (1453/1454, 1455 `0x800705AF`, `0x8007000E`), each naming its resource, commit or memory. Secondary rows (empty output,
  silent exit, torn decode) count only in a run that also shows a primary row. `judge` is the one judgement over them.
- The gate (`gate.py _verdict`) reads it. When every red is the host's, the verdict is `NOT RUN [host-exhausted]` (exit 2,
  an ENVIRONMENT cause). It names the resource and the unjudged cases, writes them to `<run>/unjudged.txt` and the record,
  and says what to do: wait for the host, then gate again. The next gate runs only what this one did not pass, because the
  carry ignores the verdict. A test red beside them keeps the gate RED and names the unjudged cases apart. The gate never
  re-runs a case itself. On run 000004's own trx files the judgement finds 23 Conformance cases unjudged (commit and
  memory) and the one Unit failure (`FunctionTypeSettlementTests`, an `Assert.Equal` with no host text) a test red. That
  run would read RED for that case alone, with the 23 named apart.
- The landing refuses such a record as it refuses any tip that no GREEN record covers (exit 14). The merge-group
  classifier (`land_lane.classify_red`) reads the same table: a bounce whose every red case is the runner's host running
  out is INFRA.
- The slot admission (`gate_slot.acquire`) applies when a waiter is first in line and a slot is free. It takes the slot
  only when the host's free commit, less what each holder may still commit up to one gate's peak, covers
  `host_resources.GATE_NEED_BYTES`. Otherwise it gives the slot back, waits and says why. `gate_slot.py status` prints
  the admission. Every gate now records its slot-held tree's peak commit (`peak_commit_bytes`, the job's
  `PeakJobMemoryUsed`).
- Sibling: the compiled-program cache crashed a case on `Decoder ran into invalid data`, because BrotliStream throws
  InvalidOperationException and the catch knew only InvalidDataException. A blob that does not decode is now a miss: it
  is removed and stored again (`CompiledProgramCacheDriftTests.ABlobThatDoesNotDecode_Misses_AndCompilesForReal`).

**The measurement** is in kb/Work/PB5697's addendum.
