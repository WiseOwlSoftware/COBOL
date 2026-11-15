# PB5500 - sibling sweep: tree kills that return before the tree is gone

Swept `scripts/` for every ProcessJob user and every other tree kill (`grep -rn "ProcessJob|process_job|kill_tree|TerminateJobObject|taskkill" scripts`).

**Fixed by the contract (no change at the call site).**
- `scripts/gate.py` `_captured`: reads its TemporaryFile after the `with ProcessJob` block; it now reads the tree's complete
  output, never output an orphan is still writing.
- `scripts/gate.py` `run_host` and `stop_hosts`: the host's `<asm>.log` is complete once the block ends.
- `scripts/self_tests.py` HANG stop (`job.kill()` then `read(out_path)`): reads the stopped self-test's whole output.

**Same shape, not changed here (files not in this change).**
- `scripts/self_tests.py` `finally: shutil.rmtree(out_dir, ignore_errors=True)`: `ignore_errors` hides a file still held at
  cleanup, the very symptom of this defect, and leaks the directory silently. With the contract fixed nothing of the tree can
  hold it; the `ignore_errors` should go so a holder outside the tree is loud. Its own change, with the HANG path's error
  handling in view.
- `scripts/detached.py` `kill_tree` (taskkill /T /F, or killpg): also returns before the tree is gone. Callers:
  `scripts/orchestrator/review_lane.py` `run_agent` (reads the agent's log after `proc.wait()`, the root only), already owned
  by kb/Work PB5114 (open); `scripts/gate_slot.py` `Holder.kill` (self-test helper; Windows relies on the holder's job
  closing) and `scripts/gate.py` self-test follower arm (kills a follower then checks the detached gate survived; no file
  of the killed tree is read). PB5114's fix (moving these to ProcessJob) inherits this contract.
