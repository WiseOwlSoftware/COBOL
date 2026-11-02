## Entry 2962 — 2026-11-01 16:45 PST — PB4400-PB4405 filed: the leads of PB4323 step 8 and of PB4395, and three amendments

The operator filed the leads that the step 8 report (w20261807a-PB4331) and the combined PB4395 branch (w20261810i,
landed in #991) left for it, each site re-read on main 10ae76f52 first. Leads already in the register became amendments.

- **PB4400:** since the landing gates nothing (PB4331), `gate_slot.py`'s LANDING gate class has no starter; it, its
  self-test arms and DECLARED entries, the design text, and test data planting `GATE RED` landing outcomes are dead.
- **PB4401:** `land_lane.classify_red` reads no job, step or artifact facts. A run whose every shard died before
  uploading reads UNCLASSIFIED instead of INFRA, and a merged-tree build failure reads INFRA and is re-queued once.
- **PB4402:** the code lane, CI's shards and `linux-gate.sh` still fetch the GnuCOBOL corpus, which only
  `corpus_sweep.py --self-test` reads now. Nightly's self-tests job has no tarball cache. The tooling lane's gate time is
  not yet measured.
- **PB4403:** `corpus_sweep.py` keeps `EXTERNAL_CORPUS_ARMS` outside `self_test_arms.py`'s DECLARED register.
- **PB4404:** `ArchReviewFleetDriftTests` runs the R2 scripts' self-tests from the Unit assembly, which keeps
  `coord.py`, `budget.py` and `model_rules.json` in the code lane (the Mythos lane's lead).
- **PB4405:** DESIGN-orchestrator-loop.md's per-step records and `test-weights.json` still name the deleted classes.
- **Amendments:**
  - PB3725: the merged count read 0 again.
  - PB3182: a rule moved into a script self-test drops out of `drift_rules.py`.
  - PB4198: account 2 has also lost its 5-hour limit.

