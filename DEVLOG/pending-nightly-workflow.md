## PB4332: a nightly workflow runs what no landing waits for (step 9 of the pipeline redesign)

Step 9 of PB4323. The pipeline had two daily schedules' worth of work and one scheduled workflow: `toolchain-freshness.yml`
asked each publisher for its newest release, while the NIST corpus through the `cobol` CLI ran in every merge group (734 s
at JOBS=4, the group's longest serial job once step 6 splits the rest), the Windows script self-tests ran on every group,
and the GnuCOBOL differential ran only when an operator started a battery. None of the three needs a landing to wait for it.

**What landed.** `.github/workflows/nightly.yml`, 09:17 UTC and `workflow_dispatch`, independent jobs (no `needs`, so one
red never hides another's verdict), each with `timeout-minutes`:

- `guard`: `scripts/guard-fast.sh` on Linux, the job body of build-and-test.yml's `guard`. That job stays in the merge
  group until step 6 (PB4329) removes it, as owner decision 2 orders; the nightly job is its one home afterwards.
- `self-tests`: both OSes, `self_tests.py` (nothing reused), `line_endings.py --check`, then a solution build and
  `self_tests.py --built`. The merge group will run the Windows self-tests only when a tooling path changed (step 3), and
  never ran `--built` on Windows at all; this is where both are seen daily.
- `differential`: Windows, `SKIP_TESTS=1 SKIP_GUARD=1 bash scripts/battery.sh`, so the corpus fetch, the build and phase 3
  keep the battery's one definition instead of a second copy. Windows because `gnucobol_differential.py` defaults to the
  `cobol.exe` apphost and the committed baseline was measured there.
- `shard-spread`: the new `scripts/shard_weights.py report`. It lists the last 24 hours of successful merge-group runs of
  build-and-test.yml, downloads their `test-results-<OS>-<job>` artifacts, reads each shard's wall from its trx
  `<Times>`, and prints `=== SHARD SPREAD: <slowest over mean>x WITHIN|OVER 1.50x ... ===`. Exit 1 above 1.5 (the weights
  file wants regenerating, PB4323 R7), 2 when a download or trx cannot be read (UNMEASURED: a report that cannot see is
  never a green), 0 NOT MEASURED on a day nothing ran the matrix. PB4328 adds `generate` to the same file and reuses
  `shard_walls` and `trx_times`.
- `latest` and `images`: moved verbatim from `toolchain-freshness.yml`, which is deleted. Two daily schedules would be two
  mechanisms for one job, so every reference moved with it (toolchain.py's messages and comments, toolchain.env, the
  setup-toolchain action, two test doc comments, DESIGN-test-build-ci.md, DOC_INDEX.md, plan section 0).

**Re-probe.** The workflow cannot run off GitHub, so the probe is its commands: `shard_weights.py --self-test` (every
arm: .NET's seven-digit fractional times, summed trx per artifact, an inert run, exactly-the-threshold, a download that
finds no artifact versus one that fails) and `SKIP_TESTS=1 SKIP_GUARD=1 bash scripts/battery.sh` on this tree: 221 s, 1323
cases. That probe is RED, truthfully: three per-case flips since battery #91 (run_extensions:2172 and run_misc:5307
AGREE_ACCEPT to WE_REJECT_THEY_ACCEPT on COBOL0899 `CALL USING OMITTED`; syn_file:431 WE_ACCEPT_THEY_REJECT to AGREE_REJECT
on COBOL5701), landed since the baseline of 2026-10-27. The nightly shows them each morning until a battery adjudicates and
re-baselines; that is the job, not a defect of it. They are reported as leads, not adjudicated here.

**Sweep.** Every workflow in `.github/workflows/` is read by the drift tests that enumerate `*.yml` (CiArtifactDriftTests,
DotnetSdkInstallDriftTests, ToolchainManifestDriftTests, SparseCheckoutDriftTests, ReadyToRunRuntimeDriftTests,
FilterPopulationGuardDriftTests): the new file meets them (toolchain-manifest images and installs, artifacts through
`upload-results`, the two sparse jobs listing what their scripts import). No new diagnostic code was needed.

**What the gate found.** The first gate was RED on `GateLegDriftTests.EveryTrxReader_IsTheOneReader_ThatNamesTheFileItCannotRead`:
`shard_weights.py` parsed trx with its own XML parser. It now reads through `test_population.trx_root` (kb/Work PB3661), the
one reader, so a torn trx is an `UnreadableTrx` naming the file and the day is UNMEASURED (exit 2) rather than a bare parse
error. `test_population.py` is deleted by step 5 (PB4328); that note's Sites now say the reader moves first and its
importers (`profile-test-parallelism.py`, `shard_weights.py`) are re-pointed.
