## CI is bounded and its caches are saved from main only (PB4324, pipeline step 1; closes PB4227)

Step 1 of the pipeline redesign (PB4323). Merge-group run 41226218068 (PR #893) sat 45 minutes in `actions/checkout` of a job
whose code was fine, and the queue removed the PR; the workflow had no `timeout-minutes`. Re-probed on this build:
`grep -c timeout-minutes` on both workflows printed 0, and the merge-group data of 2026-10-31 (3,127 runs, the review's M2) gave
each job's p90.

**What changed.**
- Every job of `build-and-test.yml` and `toolchain-freshness.yml` states `timeout-minutes`, about twice its p90. They are not the
  review's 15 and 20: the Guard job's NIST loop alone is 14.1 minutes and the Linux unit job 12.2 at p90, so 15 would have failed
  both on every run. `guard` and `windows-build-test` 30, `greenfield-unit`, `windows-conformance`, `windows-self-tests` 25,
  `greenfield-conformance` 15, the cache jobs 20, the small jobs 10. Step 6 (PB4329) re-shards the matrix and tightens them
  with it.
- Every `actions/checkout` and `setup-toolchain` step is killed after 5 minutes, and every job's first two steps are a checkout
  (`id: checkout`, `continue-on-error: true`) and the same checkout run `if: steps.checkout.outcome != 'success'`. One hang or
  transient failure costs at most 10 minutes and no queue verdict. `supersede`'s retry is `checkout-retry`, itself
  `continue-on-error`, and the steps after it run when either attempt succeeded (PB3820's rule kept).
- Python joins the toolchain cache: the action restores `$RUNNER_TOOL_CACHE/Python/<version>` with the install-tool tools,
  before `actions/setup-python`, which then finds it and downloads nothing (48.9 s in each of the eight Windows jobs).
- Caches are saved from main only. The matrix jobs' NuGet and GnuCOBOL tarball steps are `actions/cache/restore`; the
  toolchain action's save requires `github.ref == 'refs/heads/main'`; a new `package-cache` job (push to main, both OSes, a
  whole checkout) restores-or-saves NuGet (`dotnet restore` of the solution) and the tarball. It is a job of its own because
  `toolchain-cache` is a sparse-checkout job and NuGet's key hashes every csproj. A queue branch's copy was never read again and
  held the repository at 10.77 GB against a 10 GB budget.

**Drift tests.** New `CiBoundsDriftTests` (not `CiWorkflowDriftTests`, which holds the workflow READER's arms): every job states
a timeout, every checkout and toolchain step one of at most 5 minutes, every job retries its checkout once with the same `with:`
block, no `actions/cache@` and no `actions/cache/save@` without the main ref, Python's directory is in the toolchain cache and
restored before `setup-python`; one fixture arm fires each audit on the shape it forbids. Existing tests moved with the shape:
`ToolchainManifestDriftTests.AuditJobs` lets the checkout's retry stand between the checkout and the toolchain step,
`SparseCheckoutAudit.SparseJobs` reads one list per job and no list as step text (the retry repeats the list; a new arm holds it),
`SparseCheckoutDriftTests` counts jobs and removes both of `supersede`'s lists, and `LandingPipelineDriftTests` reads the
`supersede` conditions over either checkout.

**What the self-review found.** The retry's duplicated sparse list first made the sparse audit read the second list as step
text and name a wrong "reached from"; the audit now excludes every list. `toolchain-cache`'s job condition now also requires the
main ref, so a manual dispatch from a branch saves nothing.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` at 0f50f458f, run 000001-20261101T022406Z (Unit
5,638/39,075, Conformance 62/20,362, Characterization 36/36, Linux half GREEN); the six `CiBoundsDriftTests` ran and passed in leg 1.

**Not verifiable off CI.** setup-python finding the restored Windows tool-cache directory, and the first `package-cache` run,
happen on the first push to main after landing; the first merge group after it reads their effect.
