# DESIGN — Test, Build & CI Architecture

Status: AS BUILT. Section 0 is the pipeline as it is (kb/Work PB4323, 2026-10-31); CLAUDE.md "Testing" states its rule.
Every later section describes the mechanism it names as it is now; how it got here lives in `DEVLOG/` and in git.
Scope: the harness, build and CI machinery that keeps the battery green: the three test projects, the NIST and
characterization nets, the verdict-evidence rules every harness obeys, the gate and its slot cap, the compiled-program
cache, the landing pipeline through GitHub's merge queue, and the inert lane.

Section numbers are anchors. Code, workflows and tests cite them (`DESIGN-test-build-ci.md §3.14.6`), so nothing
renumbers: a section that was retired leaves its number unused.

---

## 0. THE PIPELINE AS IT IS (owner decisions 2026-10-31, kb/Work PB4323; steps 1 to 9 of PIPELINE-REVIEW.md §10)

The CLAUDE.md "Testing" section is the rule; this section is the design it rests on, in the order a change meets it.
Every later section describes one mechanism of it in detail.

1. **The one gate, at the branch's head** (`pwsh scripts/build-local.ps1 -Priority BelowNormal`, `scripts/gate.py`;
   §3.13, §3.14; kb/Work PB4328). THE POPULATION IS THE PLAN (owner decision 1 of PB4323): the gate decides a lane from
   the diff's paths (`scripts/inert_lane.py classify`, §3.18). An INERT change runs the tree audits alone (about 40 s,
   no slot). A TOOLING-LANE change (every non-inert path in `scripts/`, `.github/`, `tools/` or `.claude/` and none the
   build or a compiler test reads, `CODE_READS`; kb/Work PB4395) runs the audits with the script self-tests whose
   recorded inputs it touches, no slot and no build. Any other change builds the solution once and runs the WHOLE Unit, Conformance and Characterization
   populations on Windows, three assemblies at once under a gate slot (cap 3), the classes red last time first and then
   longest first by `tests/_shared/test-weights.json` (`GateOrderer`), each population checked by name against its
   listing (`scripts/test_population.py check`); the audits run beside the build and the hosts, and a TOOLING change
   (`scripts/`, `.github/`, `tools/`, `.claude/`, any `.py`/`.ps1`/`.sh`, the data `TOOLING_READS` names) adds the script
   self-tests whose recorded inputs it touches (kb/Work PB2914, PB4326). There is no impact map, no tier, no selected leg
   and no Linux half per change (kb/Work PB4327); `bash scripts/linux-gate.sh` reproduces a Linux CI red by hand (§3.15).
   A gate runs detached, records its head, its measured tree and its base, and a re-gate of an unchanged tree answers
   `REUSED` from its GREEN record (§3.14.12).
2. **The landing reads that record and gates nothing** (`python scripts/orchestrator/enqueue.py land <branch>`, §3.17.2
   step 3; kb/Work PB3612, PB4331). It refuses a tip no GREEN record covers (exit 14) and, before it reads the record
   and before every later write of the worktree, a gate of the worktree that runs or is starting (exit 12, whatever
   record exists, kb/Work PB4204); it merges `origin/main` in through the merge drivers, stamps the DEVLOG entry, runs
   the landing checks and queues one pull request in GitHub's merge queue. It never gates the merged head, a true
   textual merge in a shared file included, and a lane MANDATORY-PRACTICES I10 does not review gates its own tip.
3. **The pull request's own run is the cheap jobs** (`changes` and `ci-gate`, plus `toolchain-cache` on a push to main;
   kb/Work PB4330), and its caches are saved from main only (kb/Work PB4324).
4. **The merge group is the whole-population net on both operating systems and the ONLY run of the merged tree, of
   Linux and of Release** (`.github/workflows/build-and-test.yml`, §1.4; kb/Work PB4329): seven Windows and five Linux
   weighted shards, each dealt test classes by their measured weight (`scripts/shard_weights.py`), their union checked
   against one listing per OS and assembly in `ci-gate`; the audits with every Linux self-test; the Windows self-tests
   when a tooling path changed; `timeout-minutes` on every job and a bounded, retried checkout (kb/Work PB4324).
5. **THE RULE** (owner 2026-10-31, replacing the CI invariant of 2026-10-04): every case the merge group runs on
   Windows already ran green at the pushed head, on this host, under the same `toolchain.env`. The merge group is the
   FIRST run of (a) the merged tree, (b) Linux, (c) Release with a cold compile cache, (d) the Windows tooling
   self-tests on a tooling change and (e) GitHub's runners; a red there is one of those five, a flake or
   infrastructure, never an untested case. kb/Work PB1957 is the record of each class's differences and rate.
6. **Every merge-queue red is classified before anyone is blamed** (`scripts/orchestrator/land_lane.py`, the queue
   watch; kb/Work PB4271, PB4331): the failed run's red cases (downloaded into the worktree's `TestResults/ci-bounce`,
   which the finisher's gate runs first, and trusted only when `fetch_bounce_results` answers that they are the failed run's:
   a failed download leaves an older bounce's results there, and the red is then UNCLASSIFIED) are compared with the
   branch's GREEN record. INFRA (no test failed, or the run
   timed out) and RUNNER (every red case ran GREEN in the record, or the record is the inert or the tooling lane's) are re-queued once
   with no agent and filed against the pipeline (`landing_measure.py`'s `reds`, the `infrastructure` cause); GENUINE (a
   case the record listed but did not run), MERGE (a case it lacks), UNCLASSIFIED and a second red of a re-queued
   landing go to the enqueue unit's finisher with the class on the BOUNCED line. No flake is retried inside a run: a
   flake is a kb/Work defect fixed at its root.
7. **The nightly runs what no landing waits for** (`.github/workflows/nightly.yml`, §1.4a; kb/Work PB4332): the CLI
   NIST guard (`scripts/guard-fast.sh`), every self-test on both operating systems, the GnuCOBOL differential, the
   toolchain's latest-version check and the shard-weights report. It blocks nothing.
8. **Per accumulated batch**, before a release: the comprehensive battery plus the GnuCOBOL differential in its own
   worktree (plan section 9; the `gate` skill).

---

## 1. Current state

### 1.1 Test projects (three)
| Project | Role |
|---|---|
| `tests/WiseOwl.Cobol.Tests.Conformance` | End-to-end program behavior against COMMITTED goldens only: the differential feature tests (`*DifferentialTests.cs`), `NistDifferentialTests` (the corpus goldens, `[MemberData]` over `corpus.tsv`, §3.2), `VersionMatrixTests`, `ConformanceSuiteTests` (the one runner of `conformance-suite/cases`), `EditionHarness`, `CompilerUnderTest`/`CutRunner`. The theory-heavy families are PARTITIONED across xUnit collections (§3.11). |
| `tests/WiseOwl.Cobol.Tests.Unit` | Runtime kernels, the drift tests (`ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, the CI and gate drift tests named below), `CheckOnlyCompileTests`, the CLI parser, `EditionContextTests`. |
| `tests/WiseOwl.Cobol.Tests.Characterization` | The behavior-neutrality harness (§3.3): diagnostic and emitted-C# snapshots over `tests/characterization/`. |

Every one is gated whole (§3.13), runs under `GateTestFramework` (§3.14.3) and carries a `GateLegDriftTests` (§3.14.4).

### 1.2 The differential oracle
`CompilerUnderTest.cs` defines `WiseOwlCobolCompiler` (drives `CompilerDriver`) and `CutRunner`. The `*DifferentialTests.cs`
compile each source and compare its normalized stdout (`CutRunner.Normalize` = the guard's `normalize()`: drop CR,
per-line trailing-trim) with a committed golden under `tests/differential/` (`DifferentialGolden.Assert`). There is no
second compiler to compare against, so a new golden is spec-derived and never baked from another engine.

`NistDifferentialTests` compares output to the committed goldens `tests/nist/valid/*.txt`. Its program set and every
chain's predecessors come from `tests/nist/corpus.tsv` through `CorpusManifest`; the guard derives its population from the
same file (`scripts/guard-population.sh#guard_nist_population`: the asserted rows plus their declared chain predecessors),
so the two NIST legs run one population by construction (kb/Work PB925, PB3383). `CorpusManifestTests.GuardNistPopulation_*`
keep the derivation in place and the script's `--self-test` proves it.

### 1.3 Guard scripts (bash, Linux, over the `cobol` CLI)
⛔ **The NIST leg drives `cobol` (`src/WiseOwl.Cobol.Cli`)** and no other compiler (kb/Work PB750, PB2109). A guard that
drives a binary lacking `WiseOwl.Cobol.Compiler`, the Roslyn code generator that IS the product, is structurally blind to
every codegen defect and reports a true statement about the wrong thing. There is ONE guard and ONE runner of NIST programs
(kb/Work PB3383): a serial twin that ran every program in one shared directory let a program meet the files of those before
it, and was retired.
- `scripts/guard-compiler.sh` — ⭐ the ONE place that answers "which compiler is this gate measuring?". `guard_assert_compiler_identity`
  reads the resolved CLI's own `.deps.json` and REFUSES to run when the closure lacks `WiseOwl.Cobol.Compiler`, so a path typo,
  a stale bin directory or a project rename never silently points the gate at another binary. Its `--self-test` proves the
  refusal on synthetic manifests.
- `scripts/guard-compile.sh` — the compile invocation, written once (`cobol` takes the test name as `--nist NAME`; its parser
  binds the next token, so `--nist prog.cob` would consume the SOURCE).
- `scripts/guard-fast.sh` — THE NIST guard: builds the CLI under test, compiles the population (and, under a `GUARD_TESTS`
  subset, the subset's declared predecessors), runs EVERY PROGRAM AS ITS OWN JOB across the cores and diffs against
  `tests/nist/valid/` (the `divergent` goldens included, since those are exactly what the compiler must reproduce; a
  `TERMINATES` row must instead end naming its declared exception), then audits every verdict ABSOLUTELY against the manifest
  (`guard-nist-audit.sh`) and runs the golden-cleanliness sweep. Every summary line names the compiler: `=== NIST (cobol): … ===`.
- `scripts/guard-run-group.sh` — the one runner: each named program in its own clean scratch directory after exactly its
  declared predecessors (NistRunner's rule); runs the per-test EVIDENCE RULES that `guard-verdict.sh` holds (§3.10).
- `scripts/guard-nist-audit.sh` — the population, manifest and expectation audit; `scripts/guard-population.sh` — the ONE
  shell reader of the manifest (the population, the chain predecessors and the `TERMINATES` rows).
- `scripts/guard-verify.sh --self-test` — the guard's instrument self-test, discovered by `scripts/self_tests.py` and run
  again by `guard-fast.sh` before it measures (§3.10).
- `scripts/version-continuity-sweep.sh` — INV-1: one warm `cobol check-batch` over ~350 programs × 4 editions (no Roslyn),
  fails on any `BREAKS`.

**NIST is measured twice, by two paths, and they are ONE POPULATION.** `NistDifferentialTests` (6 partitions) drives
`CompilerDriver` IN-PROCESS over the 364 green and divergent rows on both OSes; `guard-fast.sh` drives the `cobol` CLI as a
separate process over the whole 376-program in-scope corpus from bash on Linux. Neither subsumes the other (one covers the
library API and Windows, the other the shipped exe, the CCVS chain-isolation model and the compile+run health of the
golden-less programs), and both derive their population AND expected verdict from `tests/nist/corpus.tsv`.
`CorpusManifestTests` asserts that structurally (guard population ⊇ every asserted program, ⊆ the manifest, surplus only
`pending` rows, neither guard hard-coding a CLI path); `guard-nist-audit.sh` asserts it dynamically, per program.

### 1.4 CI (`.github/workflows/build-and-test.yml`) — a gated matrix behind ONE required check
Ten jobs. `changes` decides whether the matrix runs at all; `supersede` (merge group only) cancels the runs the queue replaced; `audits` runs in every merge group and on no pull request or verified push (kb/Work PB4330); then, concurrently, `linux-shard` (ubuntu, 5 weighted shards,
Debug, INV-1-strong a step of each) · `windows-shard` (windows, 7 weighted shards, Release warnings-as-errors) ·
`linux-built-self-tests` (`self_tests.py --built` after a build) · `windows-self-tests` (every script self-test
on a Windows checkout, no build, kb/Work PB3581); `toolchain-cache` and `package-cache` (push to main only, both OSes: save the
manifest's tools, Python's directory, the NuGet packages and the GnuCOBOL tarball under main, below); and finally `ci-gate`,
which also checks that the shards ran the discovered population exactly once (below). The NIST corpus through the `cobol`
CLI (`guard`) runs in the nightly (owner 2026-10-31, kb/Work PB4323 decision 2), whose job is its one home (§1.4a). NuGet cached (restored by every matrix job, saved by `package-cache` alone); `Generated/` regenerated per checkout (java+pwsh prerequisites). A second workflow,
`nightly.yml`, runs once a day and blocks nothing (§1.4a).

**⛔ CI and every local host run the tool versions ONE manifest names, and each is the tool's latest release (kb/Work
PB4007; `scripts/toolchain_audit.py`).** The owner, 2026-10-28: "CI tooling should match local tooling versions -
always", then "CI and local should always be using the latest version". `toolchain.env` (the repository root) names the
exact version of every tool a CI job or a local gate runs (the .NET SDK, Python, the Oracle JDK, pwsh, each image's git,
gh, jq, uv, semgrep) and the two runner images (`ubuntu-26.04`, `windows-2025-vs2026`); it is the only place a version
is written. `scripts/toolchain.py`'s `TOOLS` is the one table of the tools: each row names a tool's manifest key, the
command it reports its version with, how a host gets it (`install-tool.sh`, `actions/setup-python`, the runner image,
`uv tool`) and whether it runs in CI. The manifest must name exactly its keys; `install-tool.sh`'s `TOOL_INSTALLABLE`
must be exactly its install-tool rows (`toolchain.py check` refuses otherwise, in every CI job and every gate); the
action installs what `toolchain.py ci-installs` prints and the host setups loop over `install-tool.sh --list`; and the
drift tests read the rows from `toolchain.py`'s text, keeping no list of their own. So a new tool is one row, one
manifest line and, for an install-tool row, one `tool_spec` arm. Every job of every workflow runs
`.github/actions/setup-toolchain` as the step right after its checkout (and the checkout's one retry, below). The action installs Python with
`actions/setup-python` at the manifest's version, and every `ci-installs` tool (the SDK, the JDK, pwsh, gh and jq) with
`scripts/install-tool.sh`, all restored from one Actions cache entry keyed on the manifest's version lines, the OS and the
architecture. The entry also holds Python's directory in the runner's tool cache (`$RUNNER_TOOL_CACHE/Python/<version>`), restored
before `actions/setup-python`, which then finds it and downloads nothing. It puts them first on `PATH` (with `DOTNET_ROOT` and `JAVA_HOME`), then runs `python scripts/toolchain.py
check --scope ci`, which fails the job unless every tool it installed reports the manifest's version. git is the one tool
CI cannot install: `actions/checkout` runs the image's git before any repository file exists, and no publisher ships a
Linux git of a chosen version (the Ubuntu image lacks the zlib and libcurl headers a source build needs). So the manifest
names each image's git, `GIT_LINUX_VERSION` and `GIT_WINDOWS_VERSION` (git's own release, without Git for Windows'
`.windows.N` suffix), one per image because GitHub rolls the two on different weeks, and each host installs that release.
A job on an image whose git moved warns and runs: a failure there would stop every landing until a bump, and with the
two images moving on different weeks no single value could clear it. The nightly's `images` job (§1.4a) holds each image strictly.
On the hosts, `scripts/wsl/setup-wsl.sh` and `scripts/cloud/setup-env.sh` install from the manifest (WSL's Python and semgrep through
uv; semgrep runs on its own uv-managed CPython, `SEMGREP_PYTHON_VERSION`, until upstream ships a cp315 wheel). The
Windows dev box installs with winget. The gate holds every host to the manifest: before its audits, `gate.py`
runs `toolchain.check --scope gate`, stops `NOT RUN [toolchain]` (an ENVIRONMENT cause) on any mismatch, and names the
toolchain it ran under in its verdict line and its record. `scripts/python-resolve.sh` and `.ps1` accept exactly
`PYTHON_VERSION`. The SessionStart readiness check (`scripts/hooks/tooling_check.py`) prints the same comparison for
every tool, ASK-OWNER on a mismatch. Identical holds at every commit; LATEST holds within a day. `nightly.yml`'s `latest` job
runs `python scripts/toolchain.py latest` every day. It asks each publisher for its newest stable release (the newest
SDK of the .NET release index's GA channel of the manifest's major; actions/python-versions' stable list, so the target
is one setup-python can install; Oracle's current releases; GitHub's `releases/latest`; each runner image's readme for
its git; PyPI for semgrep). The run is red, naming each tool, while the manifest is behind or a publisher cannot be read.
A newer GA .NET major is a warning on the run, never red: moving to it also moves `global.json` and every
TargetFramework, an owner's decision, and a standing red would hide the next release `bump` can take. Its `images` job
runs `toolchain.py check --scope image` on each runner image, red while the image's git differs from the manifest's.
`python scripts/toolchain.py bump` moves the manifest and its cloud copy forward within the .NET major. It does so in a
change of its own that carries the hosts' update and lands like any other, so CI and the hosts move together. The drift test holds the rest. The manifest names each tool once, exactly; the
cloud setup carries the manifest's block byte for byte; every job sets up the toolchain right after its checkout and runs
on a manifest image. No workflow or action installs a tool any other way: no `actions/setup-*` but the action's own
Python step, no package manager, no `dotnet tool install`. Every command a step runs is a manifest tool, a repository
script, or the image's shell and utilities, so a new tool fails the test on the day it is added. The host setups write no
version of their own.

**⛔ The .NET SDK is installed one way, never through the aka.ms redirect (kb/Work PB3308;
`scripts/toolchain_audit.py`).** `toolchain.env` pins the exact SDK version (one `global.json`'s `10.0.100` and
`rollForward: latestMinor` accept; the audit checks it), and `scripts/install-tool.sh` lists the SDK's feeds,
`builds.dotnet.microsoft.com` first and `ci.dot.net/public` second. Every tool the script installs is downloaded from the
first feed that serves it and its checksum file (the SDK's `.sha512`, the JDK's and uv's `.sha256`, or the release's
checksum list: PowerShell's UTF-16 `hashes.sha256`, gh's `checksums.txt`, jq's `sha256sum.txt`). The script checks the
asset against it, unpacks it, and only then writes a completion marker that records where the executable is. A transfer
that breaks off is retried on the same feed, while a refusal (an HTTP error, a 404 or a 409) moves on to the next feed.
A merge group's run restores only what its own ref or main saved, and its ref is new every time, so `toolchain-cache`
saves the tools (and Python's directory) under main on every push to main: a merge group then finds them even while every feed refuses. A warm-up
that fails warns and passes, because the merge group's own install still tries the feeds. `scripts/wsl/setup-wsl.sh`
calls the script. `scripts/cloud/setup-env.sh` runs before the repository is cloned, so it carries the script's install
block byte for byte between the `tool-install` markers. An exact version never goes through aka.ms.

**⛔ CI is bounded, and every cache is saved from main only (kb/Work PB4227, PB4324; `scripts/ci_bounds.py`).** Every job states a `timeout-minutes`, about twice its measured p90
(2026-10-31, 18 to 70 runs a job): `changes`, `supersede`, `audits` and `ci-gate` 10 minutes, `linux-shard` and
`linux-built-self-tests` 15, `windows-shard`, `toolchain-cache` and `package-cache` 20, `windows-self-tests` 25 (the
weighted shards of kb/Work PB4329 took the review's bounds, 15 minutes a Linux shard and 20 a Windows one, with their
size: about 4 minutes of a shard's tests where the name split's longest job, Windows Unit, ran 8); in the nightly (§1.4a), `latest` and `images` 10, `shard-spread` 15, and `guard`, `self-tests` and `differential` 60,
the most any job may state (the nightly blocks no landing, so its bound only stops a hang). Every `actions/checkout` step and every `setup-toolchain`
step is killed after 5 minutes; a job's first step is a checkout with `id: checkout` and `continue-on-error: true`, and its
second is the same checkout (the same `with:` block, which the audit compares) run `if: steps.checkout.outcome !=
'success'`, so one transient failure or hang costs at most 10 minutes and the job, not the queue's verdict, absorbs it (the
`supersede` job's retry is `checkout-retry` and is itself `continue-on-error`, kb/Work PB3820). Caches: the matrix jobs only
RESTORE (`actions/cache/restore`; `actions/cache@` is refused, since it saves on every ref), and a merge group's ref is new
every time, so a copy it saved was never read again and only filled the 10 GB budget. The saves are the `setup-toolchain` action's (when `github.ref` is main) and the `toolchain-cache` and
`package-cache` jobs', which run on a push to main alone: main's push run skips the matrix, so these jobs are what keeps the
caches a merge group restores. A save runs only after the step that produced what it stores succeeded: `package-cache`'s
`dotnet restore` and GnuCOBOL fetch continue on error, so their saves require `steps.restore.outcome` and
`steps.fetch.outcome` to be `success`, since a failed restore would store a partial package set under the exact key and a
run that hits an exact key never saves it again (kb/Work PB4348). `scripts/ci_bounds.py` holds all of it: `--check` over every workflow and action, and a
`--self-test` that fires each audit on the shape it forbids before it runs them over the real tree (kb/Work PB4395 moved
the audits out of the compiler's test assembly).

**⛔ The tests run as weighted shards, and `ci-gate` proves they ran the population (kb/Work PB4329, step 6 of PB4323;
`GateLegDriftTests` `Shard_*`, `FilterPopulationGuardDriftTests`).** Each shard job builds the solution and runs every
test assembly (Unit, Characterization, Conformance, and on Linux INV-1-strong's `FullyQualifiedName~NistProgram_MatchesGolden`
under `--std 2023 --permissive`) with `WISEOWL_COBOL_SHARD=k/N` in its environment, N = 5 on Linux and 7 on Windows. The
test framework of each host (`tests/_shared/GateLegs.cs`: `GateShard`, `TestWeights`) runs the cases of the test classes
the DEAL gives shard k: every class `tests/_shared/test-weights.json` names, of every assembly, heaviest first to the
lightest shard so far (ties to the lowest), then the run's classes the file does not name, in ordinal order, at its
`default_seconds`. The deal is a function of the file (embedded in every test assembly by `tests/Directory.Build.props`,
so a host deals by the weights it was built with), N and the classes present, and every shard of one run sees the same
three, so the shards partition the population; discovery never sees the variable, so `--list-tests` stays the whole
population. A malformed value, or a shard variable beside the local gate's leg handshake, refuses every case (an
execution error, never a silent whole run). The weights are each class's median seconds in the merge group, per run and
OS the sum over that run's trx (INV-1's re-run of a NIST class included, since it runs in its class's shard), written by
`python scripts/shard_weights.py generate` from the newest three successful merge-group runs (1,088 classes on
2026-11-01, dealing 5 and 7 shards within 1 % of the mean). A stale weight costs the balance, never a case: `ci-gate`
downloads every `test-results-<OS>-shard-<k>` artifact and runs `test_population.py check` per OS and assembly, the
shards' trx against the one listing shard 1 made, and INV-1's five trx against the Conformance listing's
`NistProgram_MatchesGolden` cases (`--method`), so a case dealt to no shard or two is a named NEVER RAN or RAN TWICE.
`WISEOWL_COBOL_SHARD` is a scrubbed variable (`test_population.is_scrubbed`), so no local caller of `dotnet test` can run
a part of an assembly through it, and `audit-callers` allows a workflow to set it only where the same workflow runs the
check. The nightly's `shard-spread` job (§1.4a) is the regeneration trigger.

**⛔ An artifact never decides a verdict (kb/Work PB3140; `CiArtifactDriftTests`).** Every test job uploads its
results (TRX, population listing, shard log, harness observation log) `if: ${{ always() }}`, so a red job's results
can be read without a re-run. Every upload goes through ONE step, the composite action `.github/actions/upload-results`:
it tries the upload three times (30 s, then 90 s apart; each retry `overwrite: true`), and when all three fail it warns
and passes, because a job's verdict is its tests'. The one artifact a verdict does depend on, a test shard's
trx and shard 1's listings, is checked by the job that reads it: `ci-gate` downloads through
`.github/actions/download-results` (three attempts; only the third failure fails the step), and a shard whose upload
was lost is absent from the union it checks, so the guard fails there and names the missing cases. No workflow names the raw upload or download action; the
drift test holds every step, and both actions' retry shape, to this rule. (`actions/cache` needs no such step: a
cache restore or save that fails only warns. The one network fetch a job makes itself, the GnuCOBOL testsuite on a
cache miss, retries the same way inside `scripts/fetch-gnucobol-tests.ps1`: three attempts before it fails.)

**⛔ `ci-gate` is a REQUIRED status check on `main`** (owner decision 2026-09-06, question 23; `kb/Work/PB796`),
with `enforce_admins: true` — an administrator exemption would be worthless when every push to this repository is
made with the owner's credentials. It is the terminal job: `if: always()`, `needs:` every other job, and it PASSES
only when each of them is `success` or `skipped` (a whitelist, so `cancelled` and any future result value fail).
It is the only name in the file a protection rule can require — the conformance jobs are matrices whose check
names carry the shard AND its filter text (`Greenfield conformance (Linux, sharded) (corpus-nist, FullyQualified…`,
measured on `5bf5a7f1`), so any shard edit renames the context, and every matrix job is skipped on a docs-only
push, which a required check reads as "never reported".

**Whether the matrix runs is the inert lane's decision (§3.18).** `changes` decides it by calling `scripts/inert_lane.py classify` over `base .. github.sha` (§3.18: the one
classification, which the local gate and the Linux gate call too), and outputs `run_matrix`; every other job carries
`needs: changes` + `if: needs.changes.outputs.run_matrix == 'true'`. A merge group's base is its
`merge_group.base_sha`, so a group of inert pull requests skips the matrix; a pull request never
runs it (its run is `changes` and `ci-gate`, §3.17.6); a push to main diffs from `before`, and with no resolvable `before` the
matrix runs. `changes` also short-circuits a push whose sha already carries a green run of the workflow from an event
other than `pull_request` — the merge group's, since the queue advances main to the very commit it tested — so main's
push run does not pay for the matrix a second time, and it carries the drift check that asserts every job id is in
`ci-gate`'s `needs` list.

**The landing protocol is `enqueue.py land` into GitHub's merge queue (§3.17):** main takes only pull requests the queue
tested (the ruleset binds administrators), so the push trigger is `branches: [ main ]` and the one push it sees is the
merge-queue commit. `cancel-in-progress` is restricted to `pull_request`, because a cancelled push run leaves its sha
permanently unlandable.

### 1.4a The nightly (`.github/workflows/nightly.yml`) — what no landing waits for (kb/Work PB4332; PB4323 step 9)

One scheduled workflow (09:17 UTC, and `workflow_dispatch`) that no branch protection, ruleset or merge queue names, so a
red job is a morning's reading and never a refused landing. Its jobs have no `needs` (one red never hides another's
verdict) and each is bounded as §1.4 states (`timeout-minutes`, a checkout retried once, 5 minutes on each checkout and
toolchain step), and only RESTORES its caches: main's push run saves them (`toolchain-cache`, `package-cache`), and main
is the ref a scheduled run reads:

| job | measures | red when |
|---|---|---|
| `guard` | the NIST corpus through the `cobol` CLI (`scripts/guard-fast.sh`, Linux, about 12 minutes at JOBS=4) | the guard is not ALL GREEN (a REGRESSION, DIFF, FAIL*, audit finding, build warning, or its own instrument self-tests) |
| `self-tests` (both OSes) | every script self-test (`scripts/self_tests.py`, nothing reused) and `line_endings.py --check`, then the `--built` phase after a solution build | any self-test is red on either OS: the Windows ones whether or not the day's landings touched a tooling path |
| `differential` (Windows) | the GnuCOBOL differential: `scripts/battery.sh` with `SKIP_TESTS=1 SKIP_GUARD=1`, so the citation audits, the corpus fetch, the solution build and phase 3, the battery's one definition and never a copy | a per-case verdict flip against the committed baseline, or a case with no compiler verdict. Windows, because the baseline was measured there and `gnucobol_differential.py` defaults to the `cobol.exe` apphost |
| `shard-spread` | `scripts/shard_weights.py report`: for each OS of the newest 8 successful merge-group runs of the last 24 hours that ran the matrix (one run's artifacts, about 200 MB, are downloaded and deleted before the next), the slowest `test-results-<OS>-shard-<k>` shard's wall (the sum of its trx `<Times>`) over the mean shard wall | the worst spread exceeds 1.5 (exit 1; the weights file wants regenerating, `shard_weights.py generate`), or a download or trx cannot be read (exit 2, UNMEASURED). NOT MEASURED and green on a day with no matrix run. Live while `tests/_shared/test-weights.json` exists and `build-and-test.yml` names `WISEOWL_COBOL_SHARD` (the weighted shards, PB4329), since regenerating that file is its remedy |
| `latest` | `toolchain.py latest`: is `toolchain.env` at every publisher's newest release (§1.4) | the manifest is behind a tool, or a publisher cannot be read |
| `images` (both OSes) | `toolchain.py check --scope image`: each runner image's own git against `toolchain.env` (§1.4) | the image's git differs from the manifest's |

`latest` and `images` were the `toolchain-freshness.yml` workflow; folding them in keeps one daily schedule. The Guard
job's one home is the nightly: the weighted-shard step (PB4329) took it out of the merge group, whose shards run the same
NIST programs in-process.

### 1.5 Drift discipline (a genuine strength — preserve it)
`ConstructRegistryDriftTests` binds `constructs.json` ↔ in-code `ConstructRegistry` both directions;
`conformance-suite/harness/generate.py --check` refuses every on-disk file no test declares (it replaced the corpus's
`Manifest_CoversEveryProgram_NoOverlap`, kb/Work PB3177);
`ReservedWordsDriftTests` binds `reserved-words.json`. Nothing silently undiscovered.

### 1.7 The fleet build guard (`scripts/hooks/fleet_active_build.py`) — a PreToolUse gate on `dotnet *`
Wired in `.claude/settings.json` for both `Bash(dotnet *)` and `PowerShell(dotnet *)`. It **denies** `dotnet
build|test|clean|publish|run|msbuild` while a live subagent is probing the binaries the build would replace: ~6 rebuilds under
a running 60-agent fleet made all 60 verdicts unusable (2026-08-04, PB15). Liveness is transcript MTIME within 120 s.
**The unit of the freeze is the WORKING TREE, not the session (2026-09-01).** It denies iff some *foreign* live agent works in
the caller's own tree, and both trees are derived, never listed:
- the caller's tree is the nearest ancestor holding a `.git` entry (a directory in the main checkout, a file in a worktree) of
  WHERE THE BUILD RUNS: the project a `dotnet` command names, else the directory the shell is in when it runs it. That
  directory comes from `scripts/hooks/shell_location.py`, the ONE parser of both shells' command lines and location changes
  (`cd`, `pushd`/`popd`, a subshell, `bash -c`; `Set-Location`/`sl`/`chdir`/`Push-Location`, `pwsh -WorkingDirectory`,
  `Start-Process -WorkingDirectory`, cmd.exe's `cd /d`), shared by `worktree_rm_allow.py`, `forbidden_commands.py` (every rule;
  kb/Work PB3095, PB3198) and `status_guard.py` (kb/Work PB2599). It is the ONE model of a command line those rules read
  (kb/Work PB3198): heredoc bodies cut out at their own delimiter lines, and every place a shell takes a command line from
  walked as one (`$( … )`, backquote and PowerShell expressions and script blocks; `bash -c`, `pwsh -Command` /
  `-EncodedCommand`, `cmd /c`, `eval`, `Invoke-Expression`, also behind `timeout`/`env`/`xargs`/`wsl -e`), each step marked with
  how many substitutions enclose it and a walk carrying what its own stdin holds;
- a foreign agent's tree is the tree of the directory **the agent was launched in**, which its own transcript records on its
  first record (`cwd`; `launch_cwd`, `agent_place`; kb/Work PB3770), however the host starts the agent
  (`Agent(isolation="worktree")`, a rolling wave's `wf_<run>-<n>`, a named `git worktree add`, the main checkout).
So N implementer agents in N worktrees build in parallel, a main-tree build is denied by a live main-tree agent, and a
main-tree build is *allowed* while only worktree agents are live. A build outside every working tree (a read-only adjudicator's
scratch probe) writes next to the projects of its graph (the project or solution it names, or the only one in the directory,
and the closure of their `ProjectReference`s, solution members and a file-based app's `#:project` directives:
`build_places`), so it is denied only by an agent working in a tree such a project references. An output path on the command
line is one more place the build writes (kb/Work PB3811): `-o`, `--output`, `--artifacts-path` and the `OUTPUT_PATH_PROPERTIES`
(`OutDir`, `OutputPath`, `PublishDir`, …); words after a bare `--` are the program's own. Fail-open on any error; an
**unknown** answer reverts to the old session-wide deny rather than to an allow: a directory the command line does not
determine, an agent whose transcript names no launch directory or whose launch directory no longer exists, a scratch graph the
guard cannot read to the end (a missing project, a `$(…)` in a reference, an `<Import>`, an output property set in a project
file, a `Directory.Build.props`/`.targets`/`.rsp` in an ancestor directory), and a command line that moves the output without
saying where (a variable in the path, a response file, `-p:UseArtifactsOutput=…`, an environment variable MSBuild reads as an
output property). `--self-test` fires every branch over real temporary worktrees, scratch projects and agent transcripts, and
is the only thing that proves the ALLOW arm — three of this hook's four defects were it failing closed.

---

## 3. The mechanisms

### 3.0 Guiding principle: three neutrality gates, ranked
A change to the compiler is behavior-neutral iff, in order of authority:
1. **Output goldens** (primary, spec-anchored): NIST `valid/*.txt`, the conformance suite's `.out` files and the negative
   `.err` expectations. They are the ISO-conforming oracle and stay byte-identical across a refactor.
2. **Diagnostic goldens** (primary): every emitted diagnostic (code, message, location) for a fixed corpus of positive and
   negative programs is snapshotted; a refactor does not change the diagnostic surface.
3. **Emitted-C# snapshots** (secondary, reviewed): the generated `.g.cs` for a representative program set is snapshotted. A
   pure reorganization leaves it byte-identical; a deliberate change of emission form RE-BASELINES it with explicit review,
   and gate (1) proves the re-baseline preserves behavior. The runtime version a module records (`CobolRepository`,
   `EnsureRegistered`) is the package version, which every runtime major bump raises (DESIGN-external-repository.md §15.4):
   the snapshot holds it as `{RUNTIME-VERSION}`, substituted only where it is the runtime's own version, so a bump
   re-baselines nothing and an emitter stamping any other version still diffs.

Gates (1) and (2) are hard (red = stop). Gate (3) is a reviewed diff, never auto-accepted, never silently drifting.

### 3.1 Test project taxonomy
The three projects of section 1.1 live under one namespace root `WiseOwl.Cobol.Tests.*`. `ConformanceSuiteTests` is the one
suite runner (it replaced `CorpusRunnerTests`, kb/Work PB3177), and no test compares against a live second compiler.

### 3.2 The green-corpus single source of truth: `tests/nist/corpus.tsv`
Replace the three copies with ONE declarative manifest, consumed everywhere.

```
# tests/nist/corpus.tsv — the ONE list of NIST programs and their status.
# name  suite  status         chain-preds(space-joined, '-' if none)   golden(valid|none)   note
NC101A  NC     green          -                                        valid                first NC program
ST103A  ST     green          ST101A ST102A                            valid                SORT USING/GIVING chain
IX999Z  IX     pending        -                                        none                 not yet compiling
```

- `status ∈ {green, pending, divergent}`. `green` ⇒ a `[Theory]` row asserting golden match. `divergent` ⇒ also
  asserted, but the golden departs from the output first recorded for it (an ISO re-baseline, a CCVS defect or a
  declared termination) and the note carries the ISO citation. `pending` ⇒ catalogued, not asserted (the mass-red
  guard).
- `chain-preds` is the chain table `NistDifferentialTests` and the guard's program runner read: a program runs in its
  own clean directory after exactly its declared predecessors, so no order carries a chain (kb/Work PB3383). The runner
  runs those predecessors ungraded and, like `NistRunner` (`[chain p] …`), never grades a consumer over a predecessor
  that did not run to a normal end or has no compiled program: the consumer's verdict is that failure, named, under
  the §3.10 evidence rules (`guard_predecessor_verdict` / `guard_predecessor_compile_verdict` in
  `scripts/guard-verdict.sh`; kb/Work PB3385).
- `NistDifferentialTests` reads `corpus.tsv` via `[MemberData]`, NOT a hand-maintained `[InlineData]` list.
- The bash guard reads the same file for its population (the asserted rows plus their declared predecessors,
  kb/Work PB925) and its EXPECTED verdicts: a plain
  `divergent` row expects `MATCH` (the golden IS the ISO-conforming output WiseOwl COBOL must reproduce).
  ⛔ **A fact about the rows is DERIVED, through one reader** — `scripts/guard-population.sh`, sourced by
  `guard-fast.sh`, `guard-run-group.sh` and the audit. Until kb/Work PB898 the guard held the divergent names as a
  hand-written string that `guard-fast.sh` `sed`-ed out of it while `guard-nist-audit.sh` derived the same fact
  from the manifest — three writings, no comparison — and the string had drifted a program behind (`SQ212A`).
  That exemption list was retired with the legacy engine (kb/Work PB2109); the one-reader rule stays.
- ⛔ **A `divergent` row whose note begins `TERMINATES EC-<NAME>` is the other kind** (kb/Work PB1955): its golden
  records a run that continued past a fatal I-O status nothing covers, and WiseOwl COBOL's ISO §9.1.13.1 choice
  (Annex A.1 item 103) ends the run unit there. The run must exit non-zero naming that whole exception-name
  (`TERMINATES <EC>`, scored by `guard-verdict.sh#guard_termination_verdict`, called by both runners). The shell
  reader is `guard-population.sh` (`GUARD_TERMINATES_MARKER`, `guard_terminating`), which the audit also asks; the
  C# reader is `CorpusRow.ExpectedTermination` (used by `NistDifferentialTests`); `CorpusManifestTests` holds the two
  grammars equal and refuses a malformed or non-fatal marker.
- A drift test (`CorpusManifestTests`) asserts: every `tests/nist/programs/*.cob` is listed; every `green` row has a
  `valid/<name>.txt`; every `divergent` row has a non-empty note containing a `§` citation; and
  `GuardScripts_CarryNoHandMaintainedNistNameList` fails on ANY NIST-shaped name list written out by hand in any
  `scripts/guard*.sh`, so the next such list cannot be a different variable and go unseen.

This kills smell #3 and makes "add a green program" a one-line manifest edit.

#### 3.2.1 The written-file goldens: the net grades EVERYTHING a CCVS program writes (kb/Work PB804)
`tests/nist/valid/<TEST>.txt` is the program's REPORT: its print file (`<test>.txt` in the run directory), or its
stdout when it has none — the whole discovery rule, with no "first `*.txt`" or `print-file.txt` guess. The report is
one file of the run, and the differential used to read only it: RW104A's report-writer output (`xxxxx049.txt`) lost a
field's COLUMN under `--permissive` (PB792) while `rw104a.txt` stayed byte-identical, and the battery was green over it.

So **every file the program WROTE** — created or modified since just before it ran, which excludes the compiler's
artefacts and a chain predecessor's files — plus its console output beside a print file or after an abnormal end (`(stdout)`) is
compared with `tests/nist/valid-files/<TEST>/<file>`, and the two sets must be EQUAL: a file written with no golden,
a golden no longer written and one different byte are each red. 150 programs write one or more: the sequential,
relative and indexed files of the SQ/RL/IX/ST/SM/IC families, the OBSQ chain, the RW report files. The next
program that writes a second file is graded with no change anywhere.
- **Two arms, one rule.** `NistRunner`/`NistOutputs` (`tests/WiseOwl.Cobol.Tests.Conformance/NistRunner.cs`, graded by
  `NistDifferentialTests`) and `guard-verdict.sh#guard_files_apply` (called by `guard-run-group.sh`, so CI's `guard` job
  grades the same files). `NistWrittenFilesTests` and the `guard-verify.sh --self-test` cases are the
  twins that make every way of being wrong red; each was shown RED against the code before it.
- **Bytes, not text.** The files are compared byte-for-byte (`-text` in `.gitattributes`): a sequential or relative
  file has no line terminator, an indexed store is binary. They pin WiseOwl COBOL's own file format (the standard
  leaves the storage layout to the implementor, ISO §13.18.43.4 GR2); the standard-defined content is what the CCVS
  program reads back and reports in its print file. Two exceptions, each because the HOST decides the bytes: a TEXT
  file (only tab, line feed, form feed, carriage return and printable ASCII) is compared with each CR LF taken as LF,
  because a line sequential file ends its records with the host's newline (DOC-A.1-114; ISO §12.4.5.10.3 GR2, "an
  implementor-defined line delimiter") — RW101A to RW104A's report files are CR LF on Windows and LF on Linux, and the
  goldens hold LF — while a lone CR is record data and still counts, and a BINARY file is never normalized, so a
  newline translation in a record file is a difference. And `(stdout)` is compared on the acceptance basis (the
  console's line ends are the host's too: carriage returns dropped, per-line trailing spaces stripped, trailing
  newlines ignored, and a console of only blanks printed nothing and is no written file). `NistOutputs.Canonical`,
  `NistOutputs.StdoutBasis` and `guard-verdict.sh#guard_canonical_into` are the one rule in its two arms.
  `guard_files_apply` has its own failure modes, each a `FILES NO-VERDICT` and never a pass or a difference: a missing
  run directory or marker, a `find` that fails, a canonicalization that fails (`guard-verify.sh` drives each through the
  function with the failing step shimmed, beside the control with nothing failing).
- **The population includes the 15 guard-asserted `pending` rows.** `OBSQ1A` and `SQ106A` … `SQ227A` have a golden and
  were run by the guard only; the first Linux run of the written-files arm failed on them for having no golden to
  grade against, and the differential now runs them as green rows (they match on both hosts).
- **Re-capture** after a deliberate format change: `WISEOWL_COBOL_UPDATE_NIST_FILES=1 dotnet test … --filter
  NistDifferentialTests`, then review `git diff tests/nist/valid-files` — the same discipline as
  `WISEOWL_COBOL_UPDATE_SNAPSHOTS`.
- **A `TERMINATES` row is graded for files too** (review round 1 of PB804; the owner's PB3353 requirement that the abnormal paths are covered as fully as the success paths). Its run ends abnormally by declaration, and what it had written by then — the closed data files, the print file, the console output — is graded with the same rule: a run that exits non-zero has NO report to separate out, so `NistRunner` returns every file it wrote (print file included) and its console output whenever it printed anything (`NistRunResult.Written`), and `guard_files_apply` is called with an empty report name on the termination arm. ST146A is the witness: `xxxxx014.txt` (SQ-FS1, closed before the end), `xxxxx015.txt` and its partial `st146a.txt` have goldens. `NC401M` terminates before it writes any file, so it has no directory.

### 3.3 The characterization harness (`WiseOwl.Cobol.Tests.Characterization`)
The behavior-neutrality proof for refactors. `CharacterizationCorpus.cs` discovers `tests/characterization/**/*.cob` (with a
per-file `.std` directive); `positive/*.cob` compile clean and have their emitted C# snapshotted, `negative/*.cob` must
diagnose and have their diagnostic list snapshotted. The snapshots are the committed `Snapshots/*.diag.txt` and
`Snapshots/*.g.cs.txt`.

- `DiagnosticSnapshotTests` compiles each program, formats its diagnostics deterministically
  (`{code}\t{severity}\t{line}:{col}\t{message}`) and asserts equality with `Snapshots/<name>.diag.txt`.
- `EmittedCSharpSnapshotTests` emits the C# for each positive program and asserts equality with `Snapshots/<name>.g.cs.txt`
  (line endings normalized). Roslyn is NOT invoked: it snapshots the emitter's output string, which is cheap and
  deterministic.
- **Re-baselining** is a deliberate, reviewed action: `WISEOWL_COBOL_UPDATE_SNAPSHOTS=1 dotnet test …/Characterization`
  rewrites the files and the diff shows in the pull request. Gate (1) proves the re-baseline preserves behavior. Never run
  in CI; CI always compares. The corpus stays small and representative (one program per feature family), not the NIST set.

### 3.5 Diagnostic-code registry
The compiler's diagnostics are a first-class catalogue, `DiagnosticCatalog` (`src/WiseOwl.Cobol.Editions/Diagnostics/`): each
rule has a `DiagnosticDescriptor` (`Code`, a stable `Id`, `Severity`, `Title`, `IsoSection`, `SuppressKey`), so the version matrix, `--suppress` and per-rule tests key on an address instead of a literal.
`DiagnosticCatalog.All` reflects the public descriptor fields, so a new descriptor needs no hand-kept list.
- `DiagnosticRegistryDriftTests` (Unit): every descriptor has a unique `Id`; codes are unique outside the declared split
  families; every code the compiler emits is a registered descriptor; a split code has no bare emit literal in the compiler.
- `docs/DIAGNOSTICS.md` is a RENDERED view (`pwsh scripts/gen-diagnostics-doc.ps1`, §3.17.4), never committed.
- The diagnostic goldens of gate (2) key on these stable codes.

**The wire encoding of a diagnostic is part of the diagnostic** (kb/Work PB899). A message carries its ISO citation, so it
carries non-ASCII (`(ISO §11.10.3 SR5–6)` holds U+00A7 and U+2013). `WiseOwl.Cobol.Runtime.IO.StandardStreams.EnsureUtf8()` is
the ONE place that rule is written, and BOTH process entries call it: `Program.Main` in the CLI before it parses an argument,
and `ProgramTable.RunModule` for a compiled program's `Main` (never a host entry such as `RunMain`, whose host owns its
console: kb/Work PB3193). No golden can see a violation: the corpus runner matches `.err` files on a pure-ASCII code
substring and the harness decodes every capture as UTF-8. The witness is therefore a BYTE assertion through a redirected
stream, `CliDiagnosticEncodingTests`, which drives the CLI from a console forced to cp437 on Windows and FAILS if that code
page was not actually in force (a child with no console inherits .NET's UTF-8 default and would pass on a broken build).

### 3.9 DEVLOG discipline
The migration roadmap and live state are `docs/REARCHITECTURE_PLAN.md` (section 0 is the live state, never a worklist). The
only history is `DEVLOG/`, one file per entry (§3.17.4, kb/Work PB3025): one entry per landing on main, checked by
`enqueue.py land` and CI through `python scripts/devlog.py check` (kb/Work PB2605). `docs/DOC_INDEX.md` names the one
canonical doc per subsystem.

### 3.10 THE VERDICT-EVIDENCE INVARIANT (the instrument gate — plan §11 A12/A12b/A12c/A12d/A12e)

> **A missing observation is not a negative observation.** Every harness in this repo produces verdicts about
> the compiler. Each of them had the same defect, and each of them had it silently: an outcome the harness
> *failed to observe* was folded into a bucket that means *the compiler did something wrong*. This section is
> the design rule that closes the class, and every gate below implements it.

**The rule.** A verdict about the compiler is produced ONLY from an observation the harness actually made.

| verdict | the evidence it requires | with nothing else |
|---|---|---|
| ACCEPT / compiled | the process exited 0 **and** the artifact it claims to have produced exists | NO-VERDICT |
| REJECT / compile failed | the process exited non-zero **and** emitted at least one diagnostic line | NO-VERDICT |
| RUN, for MATCH / DIFF | the program **ran to completion** — not killed, not timed out, not failed to launch — and a non-zero exit carries a **diagnostic**, exactly as a rejection must | NO-VERDICT |
| COMPARE, for MATCH / DIFF | output that **exists and has bytes**, and a comparison that **completed**: the normalization ran to the end and `diff` reported *same* or *different*, never *could not tell* | NO-VERDICT |
| *(no line at all)* | — | ⛔ NO-VERDICT, and LOUD: a missing verdict is a failure, never a subtraction |

⚠ **All THREE arms, not two.** The compile and run arms were hardened in 2026-08-03's instrument wave; the
COMPARE arm was left with the original rule — every non-zero `diff` exit meant "not this file" — for thirteen
months. `kb/Work/PB473`: battery #43's only red, `IF141A`, produced a report **byte-identical to its golden**
and was scored `DIFF — REGRESSION!`. Two arms fixed and one left is this repository's most reproducible defect
shape; when a rule has arms, count them.

A NO-VERDICT is never MATCH and never REGRESSION. It is an explicit statement that the run learned nothing,
and it fails the gate as UNRESOLVED so it gets read rather than absorbed.

**Five corollaries, each earned by a defect.**

1. **Assert the POPULATION, not just the failure count.** A verdict computed from the results that *arrived*
   cannot see a program that produced none — losing one lowers MATCH and still passes. `guard-fast.sh` printed
   `=== ALL GREEN ===` at 352 MATCH against a 353 baseline exactly this way. Every iterating harness asserts
   that its results are a **partition of its declared population**: one verdict per member, no strays.
2. **Compare against a COMMITTED MANIFEST, never a remembered number.** "353 MATCH" was a fact in a document.
   The expected verdict of every NIST program is *derivable* — `tests/nist/corpus.tsv` status/golden columns
   crossed with the population — so `scripts/guard-nist-audit.sh` compares per-program and self-updates the
   moment a golden lands. It also cross-checks the manifest against what is on disk, so a golden that vanishes
   cannot quietly turn its program into an expected `NO BASELINE`.
3. **Prove the instrument can fail, and prove it fails for the right reason.** `--self-test` on the audit runs
   eleven synthetic runs each built to break exactly one check, and asserts each produces *its own named
   finding* — the first draft had two cases "passing" because an unrelated bug had already reddened the
   control. `gnucobol_differential.py` runs an **evidence control** at startup (one program that must be
   accepted, one that must be rejected *with a reason*) and refuses to score anything if this build cannot be
   told apart, because `has_evidence` would otherwise reclassify every genuine rejection as a lost result.
4. **⛔ THE POPULATION A CLAIM IS ABOUT MUST BE THE POPULATION THE INSTRUMENT OPENED — and two instruments
   answering about "the corpus" must not each define it** (kb/Work PB209). Corollary 1 makes a harness assert
   its OWN declared population; it says nothing about whether that declaration matches the corpus the claim is
   written about. The differential defined its 1,323 cases inline in a `ProcessPoolExecutor` worker, so every
   reachability sweep had to re-invent the same noun and re-invented it as `find … -name '*.cob'` — which
   returns **two files** over a tree whose programs are `AT_DATA` heredocs inside `.at` wrappers. Two waves
   therefore proved a shape absent from a corpus the gate then found it in, twice. The rule: **one executable
   definition of a population, called by every reader**, a per-population count PRINTED on every run so a
   contribution of two files cannot be reported as a corpus, and a drift test binding the readers together.
   And a population that cannot be measured is not a population of zero — `corpus_sweep.py` REFUSES to report
   hit counts when its population check fails, because the clean zero from a reader that opened nothing is
   indistinguishable from evidence of absence.
5. **⛔ THE COMPARISON IS AN OBSERVATION TOO — and the plumbing that carries it must not be able to lose data
   silently** (kb/Work PB473). Corollaries 1–4 are about the SUBJECT (did the program run, was the population
   whole). This one is about the INSTRUMENT'S OWN WIRING. The NIST guards compared with
   `diff <(normalize golden) <(normalize actual)`, so both sides arrived over a `/dev/fd/N` process
   substitution. A short delivery there is **indistinguishable from a difference**: `diff` prints nothing on
   stderr and reports *different* over input that is identical. Measured: the compare alone, hammered 3000×
   at `-P32` with nothing else running, never failed; the same loop under the battery's concurrent `dotnet`
   load failed **1 in 640**, and battery #43 lost that coin flip at 1 in 376. The rule: **a verdict-bearing
   comparison reads REAL FILES whose writes have completed**, checks the normalization's own exit status, and
   distinguishes the comparator's *"different"* (exit 1) from its *"could not tell"* (anything else). Detecting
   the loss is not enough — remove the mechanism that can lose it, and keep a drift test so it cannot come back
   (`guard-verify.sh` fails on any `diff <(…)` / `comm … <(…)` in a verdict path). The same reasoning retired
   the last one in `guard-fast.sh`'s lost-result computation, where a short read would have shrunk the set of
   programs to re-observe.

**Where it is implemented.**

| site | what it does now |
|---|---|
| `tests/_shared/ProcessObservation.cs` | **THE one child-process observer.** It replaced seven copies of "start `dotnet`, wait N s, return whatever came back". A run that does not complete raises `HarnessNonObservationException` and never returns partial output for a caller to compare. It retries **once, serialized**, first: that re-attempts a measurement that did not complete, not a failed assertion. A caller that runs a PROGRAM in a directory names it (`runDirectory:`), and the retry starts from the state that directory had before the first attempt (`RunDirectoryState`; a larger file the killed attempt changed makes the retry a non-observation, never a measurement over another input: kb/Work PB3388). `EveryRunOfACompiledProgram_NamesItsRunDirectory` keeps every `dotnet <dll>` call site on it. **The budget is a rule over the WORK, never one fixed number** (`tests/_shared/ObservationBudget.cs`, kb/Work PB3108): one harness unit (120 s, `WISEOWL_COBOL_RUN_TIMEOUT_MS`) for the observed process plus one per serial round of the child processes it runs, `1 + ceil(children / parallel)` units. A timeout reports the budget, the work it covered, the process's own processor time and the host's load across the attempt (`HostLoad`), so load is told from a hang. Every retry and non-observation is appended to `WISEOWL_COBOL_HARNESS_LOG`, which the gate (`gate.py`'s `leg_env`), every CI test step (`scripts/test_linux_gate.py`) and the Linux reproduction tool point into their results directory; CI uploads it with the TRX, so the rate is measurable and a CI non-observation is readable afterwards. |
| `tests/_shared/CiWorkflow.cs` (`CiWorkflowDriftTests`) | **THE one reader of a CI workflow's shape** (kb/Work PB3819): the jobs under the top-level `jobs:` key, each job's `steps:` list, each step's own keys (`Value("uses")`, `Value("continue-on-error")`, `Input("overwrite")` for a `with:` input) and the comments. **A comment or blank line belongs to what follows it**: a banner above a job documents that job. A shape the line reader cannot read (no `jobs:`, a job with no id, a flow-style `steps:`) raises `InvalidDataException`, never an empty answer. `CiWorkflowDriftTests` holds its arms on a fixture workflow and sweeps `tests/` for a job-key regex, a `jobs:` anchor or a hand-found `steps:` key of any other file. |
| `ProcessObservationDriftTests` | Keeps the extraction collapsed (the `TestRepoDriftTests` pattern): no test source may start a process under its own bounded wait. Plus five behavioural facts, including "a process that never finishes RAISES instead of returning empty output" and "`Observe` reports a timeout with an **empty** stdout" — if that ever returns content, the defect is back. |
| `scripts/guard-nist-audit.sh` | The population + manifest + expectation audit, consumed by the guard so the rule is written once. A plain `divergent` row expects `MATCH` (PB750) and a `TERMINATES` row expects `TERMINATES <EC>` (PB1955), the second set asked of `guard-population.sh`; a verdict word it does not know (the retired `LEGACY DIVERGENT` included) is an UNRECOGNIZED finding. `--self-test` proves all sixteen checks can fail, including the two `divergent` arms and the five TERMINATES arms. `guard-fast.sh` runs it, with `guard-population.sh --self-test` and `guard-verify.sh --self-test`, before every guard run. |
| `scripts/guard-compiler.sh` | ⭐ **THE one answer to "which compiler is this gate measuring?"** (kb/Work/PB750): `cobol`. `guard_assert_compiler_identity` reads the resolved CLI's own `.deps.json` and REFUSES to run when the closure does not contain `WiseOwl.Cobol.Compiler`. Earned by the guards hard-coding the legacy CLI, so `guard NIST: 353 MATCH` measured the ORACLE for the whole rearchitecture and battery #58's NC215A wrong answer was invisible to it. `--self-test` (discovered by `scripts/self_tests.py`, and run by `guard-verify.sh` too) proves the refusal fires on synthetic manifests. |
| `scripts/guard-compile.sh` | The compile invocation for one NIST program, written once (`cobol` needs `--nist NAME` because its parser binds the next token). Called by the guard's parallel compile and its serial re-observation retry, two call sites that would otherwise each have had to be kept in step by hand. |
| `scripts/guard-verdict.sh` | ⭐ **THE evidence rules for the NIST guard, written ONCE and sourced by its one program runner** (`feedback_one_rule_one_place`). `guard_compile_verdict` (compile arm), `guard_output_verdict` (run + compare arms: normalization, candidate resolution, the FAIL*/footer rules, the verdict), `guard_preserve` (keep a non-MATCH's evidence). It reports through `GUARD_VERDICT` / `GUARD_CLASS` (`match` · `regression` · `no-verdict`) so each caller keeps its own recording and counting, and every function is option-local (`local -`) and returns 0: a scoring routine that can abort a `set -e` caller is not a scoring routine. **The comparison materializes both normalized sides into real files** — corollary 5 — and reads `diff`'s exit status explicitly. |
| `scripts/guard-baselines.sh` | ⭐ **THE baseline-cleanliness audit, written ONCE and called by the guard** (`feedback_one_rule_one_place`). Empty / `FAIL*` / non-zero-footer baselines are refused, unless the program is declared `CCVS-DEFECT` in `tests/nist/corpus.tsv` — the same declaration, read from the same file, that `CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects` states as a set equality. The complement is audited here too (a declaration with nothing to describe is a defect). Exits with the FAILURE COUNT, so each caller keeps its own aggregation. |
| `scripts/guard-run-group.sh` | ⭐ **The ONE runner of NIST programs** (kb/Work PB3383): each named program in its own clean `mktemp` directory after exactly its declared predecessors. A serial twin, `scripts/guard.sh`, carried a COPY of the rules "kept character-for-character in step" by prose — which had already drifted (one `normalize()` had a `[ -f ]` guard, the other did not) and kept both copies of the compare-arm hole — and ran every program in ONE shared directory, so IX115A graded over the `tf024.txt` IX114A fenced; it is retired, and `guard-verify.sh` fails on any other guard script that runs a NIST program. Compile diagnostics are captured (`<TEST>.compile.log` + `.compile.rc`) instead of `/dev/null`; the run is bounded by `timeout` and its exit status kept instead of `\|\| true`; **any non-MATCH's report, streams and both normalized sides are copied into a run-scoped forensic directory** before the program's dir dies with it (attributing battery #43 cost hours because that directory was already gone). |
| `scripts/guard-fast.sh` | THE NIST guard. Runner stderr captured instead of discarded (a runner could die and take its verdicts with it in silence); the audit gates `ALL GREEN`. ⛔ **FULL FAN-OUT IS KEPT AND THE LOST OBSERVATIONS ARE RE-TAKEN INSTEAD** — capping `-P` would pay for the damage on every run to protect against something the evidence rules now DETECT. Contention can no longer corrupt a verdict, only lose one, so step 3b re-runs serially just the lost programs and the members that declare one of them as a predecessor. See this table's isolation row below. |
| `scripts/guard-fast.sh` isolation | ⭐ **Every program is its own job** (kb/Work PB3334, PB3383): its runner gives it a clean directory and runs its DECLARED predecessors (`corpus.tsv` `chain-preds`) there first, so no grouping and no order carries a chain, and a non-predecessor cannot touch a consumer's file at all. It replaced the chain graph's connected components (332 groups over 376 programs, each serial in one directory), which had replaced a hand-written "these six suites run serially" list. Justified by evidence, not guessed: `NistDifferentialTests` runs every asserted program in a per-program directory with only its declared predecessors and is green. A missing predecessor edge shows as a consumer DIFF, never a false green. The leg is throughput-bound on `dotnet` cold-start, not tail-bound; the isolation is kept for correctness and to retire a hand-maintained list, not for speed. |
| `scripts/guard-verify.sh` | `--self-test`, discovered by `scripts/self_tests.py` (every gate and CI's audits) and run again by `guard-fast.sh` before it measures. **The evidence-rule witnesses** — synthetic runs through the REAL `guard-run-group.sh` over a fake repo root with `dotnet`/`diff` shims on `PATH` and a fake `corpus.tsv`, one rule per case, no corpus and no compiler: absent report → NO-VERDICT, 0-byte report → NO-VERDICT, `diff` forced to exit 2 → NO-VERDICT, `rc≠0` with empty stderr → NO-VERDICT, **a genuinely wrong report → REGRESSION** and `rc≠0` *with* a reason → REGRESSION (the discriminators, without which the fix could turn every regression into a NO-VERDICT and look green), plus the compile arm, the FAIL*/footer rules, the written-files and termination arms, the forensics, **the run directory** (a consumer is not corrupted by a non-predecessor that wrote its file, and gets its declared predecessor's file when run alone — both red against the pre-PB3334 shared-directory runner — with a discriminator proving the consumer needs the file) and the structural checks (no process substitution in a verdict path; one copy of the rules; one runner of NIST programs). Re-run them against an old runner with `GUARD_GROUP_RUNNER=<old copy>`. |
| `scripts/gnucobol_differential.py` | A rejection needs a non-zero exit **and** a diagnostic; an acceptance needs the artifact. Evidence-free compiles are retried once and then bucketed `NO_COMPILER_EVIDENCE`, which counts as a harness failure and is **named for re-run**, never folded into a divergence bucket. Its population is no longer its own: it filters with `gnucobol_extract.differential_cases()` before dispatch, so `len(payload)` IS `cases run` and the corpus has one definition. |
| `scripts/gnucobol_extract.py` | **THE external-corpus population, as an API** — `primary_source` (the one "is this a case" predicate, returning the `(member, compile-check)` pair the caller actually needs so nothing re-derives half of it), `differential_cases` (the 1,323), `iter_programs` (the 1,611 COBOL members, what a sweep must read). Both readers call it; there is no second place to define the corpus. |
| `scripts/corpus_sweep.py` | The reachability instrument and its lock. The sweep prints a per-population census on EVERY run and refuses to report hit counts when its population check fails. Its `--self-test` asserts the sweep's live external population equals the differential's COMMITTED per-case baseline — two independently produced numbers, so agreement is evidence — obtaining the corpus through `external_corpus.ensure` first. The check was **proved failing first**, driven against the old `*.cob` reader, where it reported `{"external": 2, "baseline": 1323, "state": "drift"}`; the self-test's emptied-extraction arm keeps that red permanently reachable, and its census arms run on a planted tree. An absent corpus is a LOUD failure, never a skip. |
| `scripts/fetch-gnucobol-tests.ps1` | **THE fetch the population gate depends on, and a gate in its own right** (kb/Work PB897). Extraction lands in a STAGING directory and is swapped in only after tar succeeded **and** the payload was verified (nothing is deleted before its replacement exists); the archive is named RELATIVE to its own directory, which neither GNU tar nor bsdtar misreads (⛔ `--force-local` is the wrong repair: bsdtar rejects it); and the tar is chosen by ATTEMPTING the work down an ordered candidate list rather than by trusting `Get-Command` (Git-for-Windows' GNU tar cannot read a `.tar.xz`). Every failure prints one `FETCH FAILED: <cause>` line and exits non-zero, and `build-local.{ps1,sh}` both surface it: the gate goes RED with `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`. `-SelfTest` drives the REAL `Invoke-CorpusExtraction` over a synthetic `.tar.xz`, failure branches included, and `scripts/external_corpus.py --self-test` runs it and reads its named check lines. |
| `scripts/external_corpus.py` | **THE one rule for obtaining the corpus in a tree** (`kb/Work/PB2611`): present when `tests/external/gnucobol/tests/testsuite.src` exists, otherwise the fetch script runs there, and a fetch that exits non-zero, exits 0 with no corpus or cannot start is ONE named red, `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`. The gate driver (`Host.fetch_corpus`), `battery.sh` (phase −½, before the Unit leg and the differential), `record_impact_map.py`, `linux-gate.sh` and the cloud session hook all apply it; battery #89 read NOT GREEN at a green tree because `battery.sh` alone had no fetch. Its `--self-test` drives every arm (`corpus_sweep.py --self-test` holds them declared), runs the fetch's `-SelfTest`, refuses a `--force-local` tar invocation and keeps any other script under `scripts/` from naming the fetch script; CI's two fetch steps are the recorded exception (a fresh checkout never holds the corpus, and the step's exit status is the red). |

**What this does not claim.** The invariant makes a false GREEN and a false RED *visible*; it does not by itself
prove the battery is deterministic. That is the measurement A12/A12d asks for, and it is recorded in plan §11
beside the row, not here.

### 3.11 TEST-COLLECTION PARALLELISM — the partitioned-theory mechanism (plan §11 A13)

> **A test CLASS is a scheduling unit, not just a namespace.** xUnit 2.9.2 parallelizes at TEST-COLLECTION granularity and
> by default **each test class is one collection**, so every test in a class, every row of a `[Theory]` included, runs
> SERIALLY ON ONE THREAD. A fat class caps an assembly's whole wall clock, and nothing in the normal output says so.

**The instrument.** `scripts/profile-test-parallelism.py <run.trx>` reads a trx and prints, per class, `tests / sum(s) /
span(s)` plus the assembly's average concurrency. Read the two time columns together: `sum ≈ span` means the class ran
serially and is a splitting candidate; `span << sum` means it was already spread across threads.

**The mechanism** — `tests/_shared/TestPartitioning.cs`, linked into every project under `tests/`. xUnit v2 offers two levers
and only one can split: `[Collection]` MERGES classes, so it never divides one. The naive class split duplicates the test
bodies, which is how a split rots, so the tests live ONCE in an abstract generic base and each partition is one line:

```csharp
public abstract class FamilyTestsBase<TSlot> where TSlot : ITestPartitionSlot
{
    public const int Partitions = 12;                     // read by the drift audit

    [PartitionedRowSource(nameof(Rows))]                  // the UNPARTITIONED set
    public static IEnumerable<object[]> AllRows() => …;

    public static IEnumerable<object[]> Rows() => TestPartitioning.SliceRows<TSlot>(AllRows(), Partitions);

    [Theory][MemberData(nameof(Rows))] public void TheTest(…) { … }   // written once
}

public sealed class FamilyTests_P0 : FamilyTestsBase<Slot0>;          // one line per collection
```

Row `i` belongs to slot `i % Partitions` — a STRIDE, not a contiguous block, because theory rows are ordered by construct or
program name and adjacent rows have correlated cost. ⭐ **Why it works:** static members of a CLOSED generic type are
per-type-argument, and xUnit resolves `[MemberData]` against `testMethod.DeclaringType`, which for a method inherited from
`FamilyTestsBase<Slot3>` is the *closed* type (proved with a standalone probe on xunit 2.9.2 / runner 2.8.2: 3 partitions
over 9 rows gave 9 tests; open-type resolution would have given 27 tests and 18 failures).

**⛔ The invariant, and its gate.** A partitioned family FAILS OPEN: delete one partition class and the rows it owned are
never run, with no error and a smaller, plausible test count. `TestPartitionAudit`, run per assembly by
`TestPartitionCoverageDriftTests`, closes that. It is SHAPE-DRIVEN, not registered: it finds every family by structure (a
top-level, author-written, abstract generic base with one type parameter constrained to `ITestPartitionSlot`), so a NEW
family is covered the moment it is written. Five checks, one per way a family loses rows silently or rebuilds the serial
pole it exists to remove:

| check | what it catches |
|---|---|
| the row source yields NOTHING | every check below would compare an empty union against an empty source and report green |
| slot indices ≠ {0 … `Partitions`−1} | a deleted, duplicated or mis-slotted partition class |
| an EMPTY partition over a non-empty source | more partitions declared than the source can fill, which the union check cannot see |
| a partition outside its even SHARE (⌊rows/P⌋ or ⌈rows/P⌉) | a sliced member that filters by something other than the stride, so one partition grows into the family's pole while the union still covers every row once (kb/Work PB4146) |
| union of the partitions ≠ the source as a MULTISET | rows dropped or double-run, each named in the failure |

Plus a ladder check, once and centrally: `Slot9.Index => 8` would put two classes in one partition and corrupt EVERY family.
Roslyn emits a nested `<>O` delegate-cache class inside any generic type that caches a lambda, and a nested type inherits its
enclosing type's generic parameter with its constraints, so the audit skips compiler-generated nested types.

**⭐ How many partitions: the host's parallelism, never the row total (kb/Work PB4146).** A partition's time is its family's
time over `Partitions` (P), and the leg's ideal wall is the leg's time over the host's T workers, so a partition stays under
that wall while **P ≥ T × (the family's share of the leg's work)**. Neither side moves when a family grows with the leg. A
family's `Partitions` is therefore chosen from the gate host's parallelism and the family's share, kept at its measured
floor by a drift test (`ConformancePartitioningDriftTests`: a floor of 24 for the conformance suite, 8 for the optional-word sweep,
kb/Work PB2527), and re-measured when the host or the shares change, never raised because rows were added. No test bounds
the rows a partition holds: an absolute bound guarded nothing the floor and the share check do not, and it turned every
golden-adding branch red once the suite crossed it.
- **A row is the smallest unit of work the theory can judge alone.** The share check balances rows, so it balances time only
  when rows cost about the same: the optional-word sweep's row is one SPELLING, not a whole format (a format of seven
  optional words is 128 compiles, 41–59 s).
- **The partition classes are written, not generated.** The count changes only with the host, and the suite's rows exist
  only at test time (`SuiteManifest` runs `generate.py` in the test process), while xUnit v2 makes a collection of a
  compile-time class.

#### 3.11.1 What is partitioned
| family | `Partitions` | notes |
|---|---|---|
| `VersionMatrixTests_P0 … _P11` | 12 | the continuity cells, once the lexer lock was removed (§3.14.5) |
| `NistDifferentialTests_P0 … _P5` | 6 | the 364 asserted programs |
| `ConformanceSuiteTests_P0 … _P31` | 32 | one partition per processor of the 32-way gate host, held above its floor of 24 (kb/Work PB2527, PB4146) |
| `OptionalWordSubsetDriftTests_P0 … _P7` | 8 | each an eighth of the SPELLINGS (kb/Work PB4146) |
| `StorageFormNistEquivalenceTests_P0 … _P15` | 16 | slice-`[Fact]`s of the corpus (kb/Work PB4325) |
| `BothRunnersVerdictDriftTests_P0 … _P7` | 8 | `run.py` once per edition its share names; suites generated once per test host (`SuiteManifest.Generated`) |
| `RunUnitFileLockPairDriftTests_P0 … _P7` | 8 | 768 (sharing × open mode) pairs, 96 each |

Four serial classes have no row set to slice (their cases are different programs), so they are split into classes along the
cases themselves over one abstract harness holding the helpers (kb/Work PB4325): the keyed-store witnesses, 
`CommitRollbackRuntimeHarness`, `SequentialCommitControlHarness` and the `UsageInheritance` equivalence. The cut is made by
cost; a case added to a class grows that class alone, and a class that outgrows its share is split again at a test boundary.
Whole-corpus assertions stay OFF the partitioned base and run ONCE (`VersionMatrixTests`' catalogue facts, the
alternative-sign-convention test, `BothRunners_RunExactlyTheSameTests`): an inherited `[Fact]` would run N times. A sliced
sweep keeps a POPULATION assertion per partition (its proportional share rounded UP), so a partition that silently stopped
binding goes RED instead of green-and-empty (§3.10 corollary 1).

**Per-row parallelism was measured against the partitions and not adopted** (kb/Work PB4150, PB4195, 2026-10-30/31). On the
shared host the arm that collapsed the suite into one class under `Meziantou.Xunit.ParallelTestFramework` led by about 12 %
(p = 0.07); on a host reserved overnight, with the gate cap at 1, six alternated rounds measured 133.2 s (partitions, sd 5.8)
against 129.7 s (per-row, sd 4.0), 2.6 % and inside the noise: the shared host's lead was contention. Its four costs, each
witnessed, are what any future proposal must answer: (1) under xunit 2.9.2's default algorithm it is unbounded (1,720
threads in the first run; it needs `parallelAlgorithm: aggressive` to keep the cap); (2) it names its own
`[assembly: Xunit.TestFramework]`, and an assembly names exactly one (`GateTestFramework`, §3.14.3); (3) it schedules the long
pole last (the runner-drift comparison queued behind thousands of rows); (4) it removes the bound on a heavy family (three
NIST rows of one partition ran together at 52–56 s each against 12–24 s). The remaining levers reduce the WORK (§3.12).

#### 3.11.2 ⛔ Filters must key on the METHOD, never on `Class.Method`
A partition class is `VersionMatrixTests_P0`, so `FullyQualifiedName~VersionMatrixTests` still selects it, but
`~VersionMatrixTests.Cobol85Program_…` can never match `VersionMatrixTests_P0.Cobol85Program_…`, and **vstest answers a filter
that matches nothing with a SILENT GREEN**. A filter that must survive a re-partition keys on the method name alone, which is
unique in the tree (INV-1-strong's `~NistProgram_MatchesGolden` is one); the population check (§3.14.4) catches this class of
error for every whole-assembly run.

### 3.12 THE COMPILED-PROGRAM CACHE — reducing the WORK of a re-gate (kb/Work PB985)

The largest repeated work in a gate is recompiling programs when only a test changed (a probe template, a stale test-ref, a
pinned expectation): thousands of cases recompiled although no compiler or runtime bit changed.
`tests/WiseOwl.Cobol.Tests.Conformance/CompiledProgramCache.cs` makes a re-gate reuse every compiled program. **A hit still
RUNS the program** — runtime behaviour is what a case checks; only the compile is skipped.

**Where a case's time goes (measured first, PB985 obligation 1; Debug build, serial, a loaded host).** A sampled
corpus case: front end + bind ≈ 60 ms, emit + Roslyn + packaging ≈ 40 ms, the program RUN (a `dotnet` child
process) ≈ 176 ms. A sampled NIST case: front end + bind ≈ 634 ms, emit + Roslyn ≈ 98 ms, run ≈ 203 ms. So
**Roslyn emit is NOT the dominant cost — the COBOL front end is**, and the cache is sized to skip the WHOLE compile
(front end, bind, emit), not only the backend. The run is irreducible; it bounds what a cache can recover.

**The key is complete, by construction and by test.** A cache that omits one compiler input returns a stale
program — a green verdict about code that no longer exists — so the key is everything the output depends on:

| axis | how it is keyed | proof (`CompiledProgramCacheDriftTests`) |
|---|---|---|
| the compiler's own bits | SHA-256 of every non-framework assembly in `WiseOwl.Cobol.Compiler`'s reference closure (compiler, front end, editions, runtime, Roslyn, ANTLR) and of the runtime every program is compiled against and deployed with (`AssemblyPackager.RuntimePath`, the ReadyToRun image of §3.16, which is not the assembly loaded in-process) + the exact shared-framework build + culture + working directory (`CompiledProgramCache.ToolFiles`) | a byte flipped in one closure assembly, and in the deployed runtime, changes the fingerprint; every loaded `WiseOwl.Cobol.*` product assembly is in the closure |
| every compile option | **reflection** over `CompilerDriver.Options`, rendered per TYPE; an unknown type THROWS | every property is flipped by type-derived value and must change the key — an option added tomorrow is covered with no edit |
| the source | its full path and its content hash | an edit misses; the same text at another path misses |
| copybooks, probes, environment | the compiler's OWN record, `CompilerDriver.Result.Inputs` (below), re-verified on every lookup | an edited copybook misses; a copybook that newly SHADOWS the one found misses; a `>>DEFINE … PARAMETER` variable set/changed misses |
| the clock | never keyed — a compilation that read it (WHEN-COMPILED) is **never stored** | the WHEN-COMPILED program misses every time; a program without it never reads the clock |
| the output DIRECTORY | deliberately NOT keyed (only the output file name) | the same programs compiled cold into two directories agree byte-for-byte in every output file, diagnostic and warning |

**`CompilationInputs` — the compiler states its own inputs.** The one structural change to the product: every
AMBIENT read a compilation makes — the source and copybook reads, every copybook-search probe (including the ones
that found nothing — an absent answer is an input), the NIST default-library probe, a directive's environment
variable, the WHEN-COMPILED clock — goes through `src/WiseOwl.Cobol.Frontend/Pipeline/CompilationInputs.cs`, which
performs the read AND records it; `CompilerDriver.Result.Inputs` carries the record and `Result.OutputFiles` every
file the compile wrote (the backend and the packager report their own writes). That is the dependency list any
incremental build needs (the `gcc -MD` shape), not a test-only affordance; the cache re-derives nothing. The
WHEN-COMPILED stamp is captured LAZILY (still once per compilation), so only a compilation that renders it depends on
the clock. Two source-form drift tests keep "complete" true: an ambient-read API anywhere in
`WiseOwl.Cobol.Compiler`/`Frontend`/`Editions` outside `CompilationInputs.cs` is red unless its line says why it is not
a compilation input (the compiler's own deployment, its own output), and the compiler may expose no public mutable
static. A third keeps the SCOPE true: the one direct `CompilerDriver.Compile` in the Conformance assembly is
`CompiledProgramCache.CompileCold`.

**What is not keyed, and why that is safe.** Roslyn compiles a program against the host's trusted-platform set,
which in a test host includes the TEST assemblies — deliberately unkeyed, or a test-only fix could never hit. The
only way one could shape the output is by the program BINDING to it, which would appear as an assembly reference;
the cache reads the produced assembly's reference table and refuses to store a program that references anything
outside the keyed closure or the framework. A backend failure is never stored. Runtime-library code the compiler
calls at compile time is covered by the closure hash (its bits) — its own ambient reads are run-time behaviour by
construction and are not scanned; a compile-time call into one would be a defect the ambient scan does not see.

**Stable source paths.** A harness that wrote its program into a fresh random directory had a different
`SourcePath` — a different key — every run. `CompiledProgramCache.StageSource(plannedPath, text)` places the text
at a CONTENT-addressed path (`<store>/src/<hash>/<name>`), alone in its directory, so the same text compiles to the
same key every run; the run directory stays per-case. With the cache OFF it writes to `plannedPath` exactly as
before, so a cold run keeps the historical layout.

**Store, concurrency, eviction.** Default store `.cache/compiled-programs/` under the worktree root (git-ignored):
parallel worktrees never share one, and a removed worktree takes its store with it (`WISEOWL_COBOL_COMPILE_CACHE_DIR`
relocates it). Entries (`entries/<key>.json`) name content-addressed, Brotli-compressed blobs (`blobs/<sha>`) — the
1.8 MB runtime every program deploys is stored once. Every write is temp-file-then-rename, so parallel test
collections and parallel test hosts only ever see whole files; a torn or evicted entry is a MISS, never an error —
correctness never depends on the cache. Once per test process, under an exclusive lock file, a store over its cap
(`WISEOWL_COBOL_COMPILE_CACHE_MAX_MB`, default 4096) drops its least-recently-used half and every blob no survivor
names (blobs younger than ten minutes are spared for a concurrent writer), and staged sources untouched for a week
go. Each process appends its hit/miss tally to `stats.log`.

**The switch.** `WISEOWL_COBOL_COMPILE_CACHE=off` disables it, `=on` forces it. Unset, it is ON for local gates
(`build-local`) and OFF under CI (`CI` set — GitHub Actions always sets it); `scripts/battery.sh`
exports `off`, so **the battery and CI run COLD** (PB985 obligation 4 — the owner may decide otherwise).

**Measured (kb/Work PB985, 2026-09-22)** — the whole Conformance assembly, 7,951 cases, three back-to-back runs on one unchanged
tree at `BelowNormal` on the shared 32-core host: **cold, cache off: 22 m 53 s**; **first run, cache on and empty: 21 m 31 s**
(the store's overhead is inside the noise); **re-gate, warm, no product change: 1 m 27 s** (13,133 hits, 27 misses: the 6
WHEN-COMPILED programs and the few harnesses whose sources live in a per-case random directory). The compile, not the run, was
the wall-clock pole under full parallel load. The whole store was 49 MB.

**⛔ A Debug build must not depend on the commit it was built at.** The SDK stamps the commit into every assembly
(`AssemblyInformationalVersion` "1.0.0+&lt;sha&gt;", and the Source Link map whose PDB checksum the assembly embeds), so a
test-only COMMIT would change every compiler bit, miss every case and recompile every project. `Directory.Build.props` therefore
sets `EnableSourceControlManagerQueries=false` (and drops the revision suffix) for **Debug only**; Release, the shipped build,
keeps its stamp. `ADebugBuild_DoesNotStampTheCommit` pins it on the built bits. Re-measured on the real flow (populate at one
commit, a TEST-ONLY commit, rebuild, whole assembly): build 5 s + 1 m 18 s, 13,133 hits / 27 misses, 7,952 green.

### 3.13 THE POPULATION IS THE SELECTION — the gate runs every case (kb/Work PB4328, step 5 of PB4323)

**The decision.** Owner decision 1 of PB4323 (2026-10-31, Mythos session): the local gate runs the WHOLE Windows
population for a change a test can read, not its own tests and not a plan's leg 1. The arithmetic behind it
(PIPELINE-REVIEW.md §3.3): PB2515's own 48-verdict sample (2026-10-07) had 12 changes in 48 red outside their own tests,
and the impact map that aimed leg 1 since PB3663 missed five reds the population would have run (I14, I17, I25, I29, I31
of the review's incident survey). The selection machinery is gone: the order plan (`gate_plan.py`, its tiers, budgets,
collection cap and whole-tier switch), the impact map (`impacted_tests.py`, `impact_store.py`, `record_impact_map.py`,
`impact_lane.py`, `tools/impact/`, `cs_members.py`, `adopt_main.py`'s feed), the shared timings store
(`.git/cobol-gate-timings/`), the leg handshake and identity records (`GateHandshake`, `GatePlan`, `GateIdentityRecord`,
`GateNameKey`), the implementer scope `leg1|whole` and the light slot pool of `gate_slot.py`. Their C# drift tests went
with them (`ImpactedTestsDriftTests`, `ImpactMapLaneDriftTests`, the name-key, leg and handshake arms of
`GateLegDriftTests`).

**The acceptance measurement** (2026-11-01 12:07–12:43 UTC, before anything was deleted; one worktree's build, the
compile cache OFF so every run is cold as after a compiler change, BelowNormal, the box otherwise idle: gate slots 0
held at every start and end; evidence `E:\COBOL-coord\scratch\w-p5\w20261805a\m\*\result.json`). Every run was GREEN:
Conformance 20,468, Unit 39,237, Characterization 37 cases.

| run | xunit threads per assembly | Conformance s | Unit s | Characterization s | wall s |
|---|---|---|---|---|---|
| one gate's three populations | 32 (the default) | 246.1 | 138.1 | 4.0 | **247.1** |
| one gate's three populations | 16 | 243.2 | 139.1 | 4.0 | 244.2 |
| one gate's three populations | 8 | 279.1 | 147.1 | 4.0 | 280.1 |
| three whole gates started together | 32 (the default) | 501.2–502.2 | 277.1–277.2 | 5.0 | **503.2** |
| three whole gates started together | 16 | 871.4–879.3 | 247.1–275.1 | 5.0 | 880.5 |

**The settings fixed on them.** `maxParallelThreads` stays xunit's default, the processor count (32 here, 4 on a GitHub
runner): 16 threads equals it alone and costs 75 % under three gates (Conformance's cases wait on child processes, so
fewer threads overlap fewer waits), and a checked-in `xunit.runner.json` would also bind CI's runners. The gate cap stays
3 (§3.14.6): three gates together take 503 s each, 2.04 times one gate alone, so they finish 741 − 503 = 238 s sooner
than three run one after another. The review's bound for three overlapped gates was 14.6 min; the measured is 8.4.

**What the gate runs now** (`scripts/gate.py`; §3.14.1–3.14.3): the inert lane runs the audits alone (§3.18); every other
change builds once and runs the three populations whole, three hosts at once, the audits beside them, the population
checked by name (§3.14.4). Ordering is the one lever left (§3.14.2): the classes red at the previous run first, then
longest first by the committed weights file. The growth trigger of PB4323 §3.4 (step 10, kb/Work PB4333) reads the
`timings.tests_s` of every GREEN code-lane gate, kept beyond its worktree (§3.14.13).

### 3.14 THE WHOLE-POPULATION GATE, WHOLE-SUITE SPEED AND THE GATE CAP (kb/Work PB1708, PB4328)

#### 3.14.1 The contract

- **Every gate of a change a test can read runs the WHOLE discovered population** of Conformance, Unit and
  Characterization, every time (§3.13; owner decision 1 of PB4323). Ordering changes WHEN a test runs, never WHETHER.
- ⛔ **Sound by construction: every discovered test case runs exactly once.** One host per assembly runs all of it; the
  gate CHECKS it on every run by name (§3.14.4) and a drift test proves the check fires.
- ⛔ **Nothing narrows a gate's host.** The one variable a test host's framework reads from the gate,
  `WISEOWL_COBOL_GATE_FIRST`, only ORDERS (§3.14.2); CI's shard variable `WISEOWL_COBOL_SHARD` divides a merge-group run
  whose union `ci-gate` checks (§1.4); every other caller scrubs both and the MSBuild VSTest channels, and every
  whole-assembly run — the battery and CI as well as the gate — asserts its population with one tool (§3.14.4).
- **GREEN only when every host is green and every population is exact.** A gate is done only on `GREEN`.
- **The verdict line counts the whole population, by discovered case:** `=== BUILD-LOCAL GATE: GREEN — Conformance
  20,468/20,468 · Unit 39,237/39,237 · Characterization 37/37 cases ran (skipped 0) · slot 1 of 3 (…) · toolchain …
  (no class first; run 000002-…) ===`. A case "ran" when it has a Passed or Failed result; a case whose only result is
  NotExecuted (a `[Fact(Skip)]`) is counted SEPARATELY as skipped, never as ran (`b3`).
- **The gate has ONE shape** (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; §3.17.3): `build-local.ps1` takes no mode
  and no scope (`-Priority` only sets the process priority) — the audits, a gate slot (§3.14.6), the worktree's gate
  lock, the populations and their check, for an implementer's commit and a re-gate after a fix alike. The battery and CI
  do not use the driver.
- **A gate outlives its caller, and a measured tree is gated once** (kb/Work PB3536; §3.14.12): it runs DETACHED in its
  run directory and its verdict file is the one thing any reader waits on, and a re-gate of an unchanged tree answers
  from the recorded GREEN, saying `REUSED` (never from a record of schema below 5, which a gate before PB4328 wrote and
  which may be a `LEG 1 ONLY` one).

#### 3.14.2 The order: the classes red last time first, then longest first (kb/Work PB4328)

- **The first classes** (`scripts/gate.py` `Gate._first`): every test class with a red case in the worktree's previous
  gate run (`TestResults/build-local/`, the newest earlier run with a verdict and a trx), its newest hand-run Linux gate
  (`TestResults/linux-gate/`) or the failed merge-group run that bounced its pull request (fetched into
  `TestResults/ci-bounce/` by `queue_facts.fetch_bounce_results`, kb/Work PB3644), read by `test_population.red_classes`
  (the class of each trx definition with a Failed result, through the one trx reader, kb/Work PB3661). They are written
  one per line to `<run>/first-classes.txt`, whose path every host gets as `WISEOWL_COBOL_GATE_FIRST`.
  `test_population.TRX_WRITERS` names every live tool that writes a trx file and what the gate takes from it, and
  `test_population.py --self-test` holds it equal to the tools (`audit-trx-writers` over the real tree). A trx the gate cannot read
  is named on the console and KEPT in the run directory as `unreadable-<its directory>-<its name>.bad` (never `.trx`:
  later readers glob a run's trx files), and the gate goes on without it: this orders the run, it never selects it.
- **The orderer** (tests/_shared/GateLegs.cs `GateOrderer`, installed by `GateAssemblyRunner` in every run): the test
  collections holding a first class go first, then every collection longest first by the committed weights file
  (`tests/_shared/test-weights.json`, each class's median merge-group seconds, written by `scripts/shard_weights.py
  generate`, kb/Work PB4329; a collection weighs its classes' sum, a class the file does not name its default), ties in
  xunit's order. Longest first keeps a long collection from starting last and running alone at the end. ⛔ Its output is
  checked to be a permutation of its input (`GateOrder.Permutation`): a dropped or repeated collection throws, and xunit
  then keeps its own complete order. A first-classes file that cannot be read keeps xunit's order too, with a diagnostic
  message; neither can cost a case.

#### 3.14.3 How the hosts run on this runner (xunit 2.9.2 · xunit.runner.visualstudio 2.8.2 · Test.Sdk 17.11.1)

- **One host per assembly, three at once**: `dotnet test <project> --no-build --results-directory <run> --logger
  trx;LogFileName=<assembly>.trx`, its whole output to `<run>/<assembly>.log`, inside the gate slot (`Slot.spawn_kwargs`)
  at the gate's priority, each host leading its own process group so a RED audit stops its whole tree
  (`Host.stop_hosts`, kb/Work PB4326). Each host's report is printed through `scripts/test_leg_report.py` (a green host
  its verdict line, a red one its complete output; kb/Work PB1573), in a fixed order.
- ⛔ **THE ONLY PLACE A TEST HOST'S ENVIRONMENT IS MADE** (`gate.host_env`): the scrubbed environment
  (`test_population.scrubbed_env`: no `WISEOWL_COBOL_GATE_*`, `WISEOWL_COBOL_SHARD`, `VSTest*` or `RunSettingsFilePath`),
  in English, with the gate's own `WISEOWL_COBOL_GATE_FIRST` when it has first classes and the harness observation log
  in the run directory, one per host (kb/Work PB3108).
- **The listings** (`dotnet test --list-tests`, scrubbed, one per assembly, kept as `<run>/listing-<assembly>.txt`) run
  BESIDE the hosts, and so do, when the change touches tooling, the self-tests that read the built assemblies
  (`self_tests.py --built`, kb/Work PB4326); both are joined after the hosts.
- **The threads**: xunit's default `maxParallelThreads`, the processor count, measured best (§3.13); no
  `xunit.runner.json` caps it. The test hosts run Server GC (kb/Work PB2526).
- **The record** (`verdict.json`, schema 5): the verdict, its line and exit code, the measured head and trees, the base,
  the toolchain, the slot, the lane, whether tooling was touched, the first classes, each host's exit, wall and finish,
  the timings (`build_s`, `tests_s`, `first_red_s`, `wall_s`, the waits beside), `assembly_wall_s`,
  `effective_concurrency` and each population's counts.
- **A gate that measured nothing ends `NOT RUN [<cause>]`**, never GREEN and never RED. `gate.NOT_RUN_CAUSES` is the ONE table
  of causes, each with a class: ENVIRONMENT (the host, not the tree: `settings`, `interpreter`, `toolchain`, `disk`, `tree`,
  `launch`, `run`, `merge-blocked`, `host`, `sdk`) or BRANCH (the tree's own scripts or merged tree: `driver`,
  `merge-failed`, `population`, `arguments`, `verdict`, `unclassified`). The landing judges a NOT RUN by its class (§3.17.2
  step 3), and a NOT RUN verdict naming no known cause is `unclassified`.

#### 3.14.4 Soundness: the population check — ONE tool for every whole-assembly run

`scripts/test_population.py` (a module and a CLI) answers one question for any run: did the union of its trx files
execute exactly the discovered population? Its callers are the gate driver (per assembly: the module's
`list_population` and `check`), the battery (`battery.sh` PHASE 1, all three assemblies — `check --project … --trx …`) and
CI's `ci-gate` job (`check --listing … --trx …` per OS and test assembly over the weighted shards' trx files, and `--method
NistProgram_MatchesGolden` over INV-1-strong's, kb/Work PB4329; a bare count cannot see a dropped test offset by one that
ran twice). Shard 1 of each OS lists its build's population (`test_population.py list`, 2 s) and uploads it beside its trx as
`test-results-<OS>-conformance-<shard>`, so the checking job needs no .NET and no build of its own, and the listings
of one platform must be identical — the shards ran one build — or the check is an error. It also owns the ONE parser
of a `--list-tests` output (`parse_listing`); `filter_population.py` imports it.
- **The population** is `dotnet test --list-tests` — vstest discovery, which never calls the executor and so cannot see
  the leg filter — listed by the tool itself in a SCRUBBED environment (§3.14.3), because the VSTest environment
  channel narrows discovery exactly as it narrows the run.
- **The executed set** is the multiset union of the trx files' TEST DEFINITIONS (`<UnitTest name>`, one per
  discovered case that executed). Not the RESULT names: xunit lists a theory whose data it cannot serialize as ONE
  case and then reports one result per row, so Unit's results carry 33 names `--list-tests` never printed and miss 2
  it did, while its 29,703 definitions equal the listing exactly (`b8`).
- **One spelling** (kb/Work PB4274): the two sides are compared in the trx logger's spelling (`trx_spelling`), which is
  what the logger did to the listed name, so a case always meets itself. The rewrite cannot be inverted (the logger escapes no backslash), so two LISTED names it spells alike are an error naming both,
  never one merged count.
- a name short → `NEVER RAN` (red, each named) — unless the run was stopped early (a red audit stops the hosts), when the
  remainder is reported as NOT RUN and the verdict is already RED;
- a name over → `RAN TWICE` (red, named), whether or not the run stopped early;
- a definition the listing never printed → `NOT IN THE POPULATION` (red, named: a trx of another build or assembly),
  whether or not the run stopped early;
- a definition with no Passed or Failed result → counted as SKIPPED on the verdict line, never as ran;
- an input it cannot read (a missing or non-trx file, a listing without vstest's marker, an EMPTY population, listings that
  disagree) → exit 2, an error, never a finding. Each run prints one line,
  `=== POPULATION <label>: EXACT — 9,317/9,317 cases ran (skipped 0) across 1 trx ===` or `RED — …` followed by the named
  cases.

**The drift tests that prove it fails** (`GateLegDriftTests`, one per assembly, each arm one method named for its number,
for the test assemblies' half; the scripts' half is their own self-tests', kb/Work PB4395):
(2) the orderer runs the named classes first, then longest first, and its output is a permutation of its input (a planted
non-permutation throws); (3) `gate.py --self-test` and `test_population.py --self-test` declare every arm (a dropped case,
a duplicated case, a skipped case, a foreign definition, a red audit that stops the hosts, a second gate in the same worktree
refused, one shape for every gate): `scripts/self_test_arms.py`'s `DECLARED`, the one register of the arms each pipeline
self-test must declare (kb/Work PB4395 moved it out of the compiler's test assembly); (4) every test project under `tests/` links `GateLegs.cs` and names `GateTestFramework`;
(5) no discovered display name in a gated assembly contains the repository root (the audit reads the attribute and xunit's
own discovery with theories pre-enumerated, `tests/_shared/GateLegAudit.cs`, and matches the root as TEXT in its written, `/`
and xunit-escaped forms, never through the host's path rules; Unit's witness plants a Windows, a POSIX and the host's own
root, so the arm is proven on each OS CI runs); (6) every `dotnet test` caller scrubs `WISEOWL_COBOL_GATE_*`, `VSTest*` and
`RunSettingsFilePath` (`test_population.py audit-callers`, §3.14.3, run over the real tree by `test_population.py
--self-test`); plus `Shard_*` for CI's deal (§1.4), and `test_population.py --self-test`'s real-tree arms that hold
`test_population.TRX_WRITERS` and the one trx reader (§3.14.2).

#### 3.14.5 Whole-suite speed: what was measured, and what it fixed

A whole-Conformance run is bound by what its threads wait on and by the lock-free-ness of the compiler's hot path, so each
speed lever below was found by measuring a run first (`scripts/profile-test-parallelism.py`, stack samples, an in-process
scaling probe), never by assuming.

- **M6 — after warm-up, lexing performs NO ATN simulation (kb/Work PB1715).** The twelve continuity partitions were the
  critical path, 4.5 s a row in one process against 0.97 s alone, with the test host using ~3.8 of 24 cores: 28 of 30
  single-thread stack samples sat in ANTLR's `LexerATNSimulator.ComputeStartState` / `AddDFAState`, half blocked in
  `Monitor.Enter`. ANTLR 4.13.2 caches a mode's start state only when its closure carries no semantic context, and never
  stores a DFA edge whose target configuration set passed through a predicate (`AddDFAEdge` returns early when
  `q.hasSemanticContext`), so every token that REACHED a predicate recomputed under `lock (dfa.states)`. That is a PRODUCT
  defect (the `cobol` CLI paid it serially, any parallel host paid the lock), fixed in `CobolLexer.g4` by removing the
  predicates from the lexer's hot paths rather than by sharding processes (which would route around the defect):
  `FNARG_SEPARATOR` and `DEFINED` match the same text as their fallback, so an ACTION picks the outcome (`DEFINED` is a
  virtual token that `IDENTIFIER`'s action retypes when the directive expression is primed; DESIGN-frontend-grammar.md
  §3.3e); the four `FN_SIGNED_*` rules stay unconditional default-mode rules whose action (`OnSignedLiteral`) keeps the
  signed literal where `SignedLiteralCanStart()` holds, and otherwise cuts the token back to its sign and re-seeks. Actions
  do not suppress DFA edges. `LexerDfaCacheDriftTests` pins the invariant: after one warm-up pass over the suite's sources,
  no ASCII character of a re-lex leaves the cached DFA, and `s0` is non-null for every mode. Its instrument is a probe on
  the INPUT side (`Lexer.NextToken` and `LexerATNSimulator.Match` each `Mark()` the `ICharStream`, which then walks the
  cached DFA over the coming characters exactly as `ExecATN` will), because in ANTLR 4.13.2 `MatchATN`, `ComputeTargetState`
  and `ExecATN` are not virtual and no subclass can count them. The next predicate on a common prefix is red wherever in the
  rule it sits. DFA edges cover characters 0–127 only and the EOF transition is never cached, so those are REPORTED, never
  asserted. The one-thread wall of the scaling probe fell from 110.1 s to 3.4 s, with ZERO contended `Monitor` acquisitions.
  Sharing one front end across editions was REJECTED as unsound (edition state is cached in the front end).
- **Server GC (kb/Work PB2526).** Once the lexer lock was gone, 69 % of a 32-thread compile probe's wall was workstation-GC
  pause; `tests/Directory.Build.props` sets `ServerGarbageCollection` for every test host (Conformance 318 s to 255 s, Unit
  74 s to 56 s, identical pass sets), and each gated assembly's `GateLegDriftTests` asserts its host's runtimeconfig carries
  `System.GC.Server`.
- **M7 — the long poles.** `pb505-table-value-dynamic-span-levels` (266 s; 29.6 s alone through the CLI for a 26-line
  program) and `DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) were one product defect: `TableValueOdometer.Resolve`
  filled up to 64,000,000 elements before reaching the SR23 violation that `COBOL1946` reports (ISO §13.18.63.3 SR23). The
  fix (kb/Work PB1716, PB1722) computes the phrase's element count as the mixed-radix distance from subscript-1 to
  subscript-2 BEFORE anything is read, returns none for a phrase SR20/SR21/SR23 reject, and keeps the PHRASES instead of an
  element map; the SR23 shape lives once in `TableValueOdometer.UnboundedBelow`. The pb505 program now rejects in 0.49 s. The
  battery summary prints the five slowest tests (a REPORT, never a wall-clock assertion), so the next pole is seen when it
  appears.
- **The gate-cost levers (kb/Work PB2524–PB2527, 2026-10-07).** The audits run before the slot (about 230 s of every slot
  hold became 0), `audit_doc_citations.py --check` runs in-process (78.2 s to 2.5 s, byte-identical output over 949
  citations), and the compiled-program cache (§3.12) and the ReadyToRun runtime (§3.16) cut the rest. Evidence:
  `docs/rearchitecture/evidence/w1033-gate-levers/`.
- **The compiled-program cache (§3.12)** already stores check-only results, so a TEST-ONLY re-gate hits every continuity
  cell; a gate after a product change misses by design.

#### 3.14.6 The gate cap (M2)

`scripts/gate_slot.py` is ONE counting semaphore for the whole repository, served FIFO. It rations builds and test legs, so
the cap bounds the concurrent solution builds as well as the test legs (an inert-lane gate runs neither and takes no slot).
- **Slots.** N lock files `<git common dir>/cobol-gate-slots/<k>.lock`, each held by an exclusive OS file lock (`LockFile` on
  Windows, `flock` on Linux), so a crashed holder releases its slot when it dies and no stale-pid cleanup exists. The holder
  writes its label (who, pid, since when) at the start of the file; on Windows the lock covers one byte far past the end of
  the file, so the label stays readable. The label is informational: a slot is free exactly when its lock can be taken.
- **Tickets.** A waiter first takes a TICKET: under an exclusive lock on `tickets.lock` it increments the monotonic counter in
  `tickets.seq` (rewritten atomically; a counter that is not a number is an error, never a silent reset, since a reset could
  hand a new waiter a number below a live one), creates `ticket-<seq>.lock` and holds it for as long as it waits. A ticket is
  LIVE while its file is locked, so a waiter that dies drops out of the queue by itself. A waiter may take a free slot only
  when no live ticket is served before it, and it drops its ticket only AFTER it holds the slot; a gate that finishes and
  re-gates at once therefore queues BEHIND everyone already waiting. `gate-slot: waiting, k ahead (ticket t, cap N, …)` is
  printed each time `k` changes, `gate-slot: took slot k of N (ticket t, waited s)` on acquisition, and a waiting gate says
  WHY it waits (a count that rises while it waits is a landing ticket numbered after its own and served first; the gate next
  in line says it waits for the first released slot).
- **Two classes, landing first (kb/Work PB3103).** A ticket is `landing` (`ticket-<seq>-landing.lock`, selected by
  `COBOL_GATE_CLASS=landing`) or `implementer`; every live landing ticket is served before every implementer ticket, FIFO
  within a class, with the same cap. ⚠ Since the landing stopped gating (kb/Work PB4331) nothing sets that variable, so the
  class is served but never used.
- **What holds it.** A Python caller uses `GateSlots.for_repo().acquire(label)` (released by its `with` block or the holder's
  death); a shell caller wraps a command with `gate_slot.py run [--label TEXT] -- <command>`. `gate.py` takes the slot after
  the worktree's gate lock and its audits and BEFORE the build, and holds it through the population check. The slot is held
  by the gate's whole PROCESS TREE. On Windows, acquisition puts the holder ITSELF into a Job object with
  `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` before it spawns anything, so no grandchild can escape and a dead driver takes every
  `dotnet` process with it. On Linux the slot's descriptor is inherited by the children (every spawn passes
  `**slot.spawn_kwargs()`), and an `flock` lives until the last descriptor closes, so an ORPHANED Linux tree keeps its slot
  until it exits. ⛔ `spawn_kwargs` also turns PERSISTENT BUILD SERVERS off in every child (`MSBUILDDISABLENODEREUSE=1`,
  `UseSharedCompilation=false`): a reused MSBuild node or VBCSCompiler started inside the slot is a descendant of the holder
  that OTHER worktrees' builds connect to, so the job would kill it in the middle of another gate's build. Acquisition is
  nested in one fixed order (worktree lock, ticket, slot), so no two gates can wait on each other in a cycle.
- **Long holds are flagged, never killed (kb/Work PB3310).** Every released hold records its duration per kind in
  `holds.json` (the last 50 per kind); `gate_slot.py status` marks a held slot older than 5 times the median of its kind
  (at least 5 recorded) `LONG HOLD`, and the session probe prints it. The flag is advice; the operator verifies the holder's
  pid before stopping anything.
- **The cap is ONE shared setting** (kb/Work PB2514): `cobol-gate-slots/settings.json`, written only by `gate_slot.py set-cap N
  [--until ISO] [--why TEXT]` (under `settings.lock`, replaced atomically) and re-read by every waiter on every poll, so a raise
  reaches the queue at once (while each gate read N from its own environment, a cap-1 gate at the head waited for slot 1
  while slot 3 stood free, blocking every cap-3 gate behind it). An expiry (an ISO time with its UTC offset) returns the
  CODED DEFAULT with no one acting; `clear [cap]` removes the setting (the one repair of a malformed file); malformed settings
  stop the gate with the reason. ⛔ THE CODED DEFAULT IS THE OWNER'S DECISION (2026-10-10, kb/Work PB2514), in `gate_slot.py`
  and nowhere else: `DEFAULT_SLOTS = 3`. A setting lives in the slot directory's OWN file, never as a new key of another
  tool's settings (every worktree reads it with its own copy of `gate_slot.py` and refuses an unknown key).
- **Why 3.** At cap 1 implementer gates waited 62 % of their wall time (the w1033 survey), and a whole-Conformance
  leg stayed within 1.25× of its quiet time only with no other gate beside it (1.24× and 1.30× with one implementer gate,
  1.58× with two, 1.56× with three: kb/Work PB1720, evidence `m13`). Re-measured for the whole-population gate (§3.13): three gates together take
  503 s each, 2.04 times one gate alone, so they finish 238 s sooner than three run one after another. **The host's
  throughput at N concurrent legs (kb/Work PB4181, 2026-10-31, the reserved host):** N copies of the Conformance assembly
  started together inside one slot, runs per hour = N × 3600 / the batch's wall:

  | N | 1 | 2 | 3 | 4 | 6 | 8 |
  |---|---|---|---|---|---|---|
  | batch wall, rep 1 / rep 2 (s) | 125 / 123 | 213 / 217 | 308 / 298 | 421 / 389 | 618 / 594 | 841 / 824 |
  | runs per hour (mean) | 29.0 | 33.5 | 35.7 | 35.6 | 35.7 | 34.6 |

  Throughput rises 23 % from one leg to three and not at all beyond (one quiet leg already keeps most of the 32 processors
  busy, counting the compiled programs it runs as children). At N = 8 ten runs ended RED on a HARNESS NON-OBSERVATION (a
  compiled program not finished in two 120 s attempts at 77–87 % host load), never on a value. The owner kept the cap at 3.
- **Who takes one.** Every gate (one shape, kb/Work PB3022). The BATTERY never takes a slot and never waits.
  `gate_slot.py status` prints the cap with who set it, EVERY held slot (one taken under an earlier, higher cap included) and
  the live tickets in queue order. ⛔ The cap does not span operating systems: a Windows lock and a WSL lock on a drvfs mount
  do not see each other, so the hand-run Linux tool (§3.15) is not counted against it.
- **`gate_slot.py --self-test` declares fourteen arms** against throwaway repositories, each with one linked worktree: FIFO
  order (three waiters behind a holder are served in ticket order, and the holder re-gating the moment it releases is served
  LAST), release on a killed holder, a dead waiter leaving the queue, the orphaned tree (the job kills the orphaned child and
  grandchild on Windows; on Linux the tree keeps the slot until it exits), sharing across worktrees, the SHARED CAP (a gate
  at the head under cap 1 and one behind it both take a slot the moment `set-cap 3` is written from the linked worktree),
  the EXPIRY and the refusals, STATUS (a slot held above a lowered cap is shown), the CODED DEFAULTS, CLEAR, LANDING FIRST,
  LONG HOLDS, and A WAITING GATE SAYS WHY. Each arm was seen RED on a planted defect (no ticket check, a ticket file counted
  live unlocked, no Job object, the descriptor withheld from Linux children, the per-worktree git dir, the cap read once per
  process, `status` probing only the slots below the cap). It passes on Windows and under WSL, `self_tests.py` runs it in every
  gate and in CI's Linux and Windows jobs, and `scripts/self_test_arms.py` holds the arms by name, in order, and the ALL
  GREEN count to them WITHOUT re-running it (kb/Work PB2882, PB4395). Every helper process it starts exits once its sentinel file is deleted, so a red
  arm leaves nothing running on the host.

**Retirement is measured** (owner, 2026-09-28): the cap goes when a whole cold Conformance gate at `BelowNormal` costs under
6 minutes on the shared host, which M6 and M7 are what can meet.

#### 3.14.10 The self-test phase: a PASS reused for unchanged inputs, one machine-wide budget, no verdict from the clock (kb/Work PB2914)

Every gate runs the ~70 script self-tests (§3.14.3), and a self-test is judged by what it does, never by how long it takes:
a test that fails with no regression, which the owner forbids (2026-09-25), must not sit inside the gate. Three rules follow.

**1. Reuse — who records, who reuses.** `scripts/self_tests.py` TRACES every self-test it runs and records each PASS with
the content of everything it read, in ONE store shared by every checkout of the repository:
`<git common dir>/cobol-self-test-passes/<platform>/<test digest>/<deps digest>.json` (the newest PASS_KEEP per self-test).
`scripts/self_test_inputs.py` owns what "read" means:
- **traced** — a `sitecustomize` tracer first on PYTHONPATH records, through the interpreter's audit hooks (PEP 578), each
  file a Python process opens (a module's `__pycache__` file counts as its source), each directory it lists and each path it
  hands a child process, inside the repository;
- **static** — what no audit hook sees: the self-test's own file, the runner itself (`self_tests.py`, `self_test_inputs.py`),
  `# SELF-TEST-READS: <path> …` header declarations, and for a PowerShell or bash self-test the closure over the files its
  CODE mentions (comments dropped);
- **git** — a `git` command a traced process runs inside the repository reads what `git_reads` says: nothing for its
  location, configuration, worktrees or a commit id; the NAMES (`ls-files`) or the CONTENT (`diff`, `ls-files -s`,
  `show <rev>:<path>`, any other command) under its pathspec, the whole tree when it names none.
A record holds each file's git blob id (the working tree's content id for an edited or untracked file; `-` where the run looked
and found nothing) and a digest of what it saw of each directory (`Tree.digest`). `--reuse` reports a self-test REUSED, unrun,
while a record's inputs are ALL unchanged on the tree being gated. A self-test that RUNS says why on its console line (kb/Work
PB3061): `first run: no PASS recorded for <platform>, Python <v>`, or `re-run: N input(s) changed since the newest PASS — a,
b, c (+k more)`. **Every gate reuses** (`gate.py` adds `SELF_TESTS_REUSE` to its SELF-TESTS audit); **CI's `audits` and
`windows-self-tests` jobs and the hand-run Linux tool never reuse**, so a PASS reused past a read nothing traced (a PowerShell
or bash process's own reads of a path built at run time, `os.stat`-style probes, a Python child started with `-I`) is caught
in the merge group. The `--built` phase reads build output no record covers: it always runs and never records. A Python
self-test whose trace does not show its own file had no tracer, and records no PASS.

**2. The machine-wide budget.** A self-test waits for one of `BUDGET_CAP` places (one per core) before it starts, and holds
it to its end: one OS file lock per place under `<git common dir>/cobol-self-test-slots/` (gate_slot.py's `ExclusiveLock`,
so a dead holder's place is freed by the OS). N gates therefore share the cores instead of multiplying them. ⚖ It is a budget
of its own and not the gate slot: the slot rations builds and test legs and is held for a whole gate, so moving the
self-tests inside it would make a red self-test wait behind up to three other gates' builds, which is what moving the audits
out of the slot stopped (fail fast, kb/Work PB2523, PB2524). ⚖ One place per core, and a run says whether the cores were its
limit (kb/Work PB3585): the GREEN verdict line reports `the host's P processors u% busy` across the phase (`HostCpu`); near
100 % the cores were the limit, and `--budget N` measures a wider width for one run (a measurement only; no gate or CI job
passes it). Measured 2026-10-24 on CI's 4-core Windows runner, all four places were busy from the 20th second until the last
long self-test, so the places were never short of work; what costs time is the ORDER (a self-test starts in discovery order,
so one discovered late runs alone for the last 50 s).

**3. No verdict from the clock.** A self-test is never RED for being slow. A run still going `HANG_AFTER_S = 3600` s after it
took its place (counted from the place, so waiting behind other gates never counts; far above every PASS measured) is stopped
and reported HANG with its output so far: `=== SELF-TESTS: HANG — … ===`, exit 3, the gate's reason `SELF-TESTS HANG`, never
`SELF-TESTS RED`.

**Proof.** `self_tests.py --self-test` (`fleet_arms`) runs the phase from four CONCURRENT simulated gates over one planted
repository: a full run records; the gates on the unchanged tree re-run nothing; a changed input (a file read through a name
built at run time, which only the trace sees) re-runs exactly its self-test in every gate, at most the budget's two at a
time; an edited import re-runs only its importer; a planted 60-s self-test under a 1-s detector is HANG; a malformed store
record is skipped. `self_test_inputs.py --self-test` proves the static rule, the git classification and the tracer;
`gate.py --self-test` proves every gate reuses and that a hung self-test is `SELF-TESTS HANG`; `SelfTestDiscoveryDriftTests`
holds the arms by name and forbids `--reuse` in CI and the Linux tool.

**4. One scratch-repository fixture (kb/Work PB3584).** A self-test that needs a git repository builds and drives it through
`scripts/scratch_repo.py`'s `ScratchRepo`, the one place a self-test's git process starts. `init` and `clone` write the
identity, no automatic maintenance (`maintenance.auto = false`, `gc.auto = 0`) and no signing into the new repository's OWN
config, so every git process in it behaves the same whoever runs the self-test. Every command runs without git's
repository-selection variables, a failed one raises with git's own message (`run` is the unchecked form), and `isolate()`
drops the variables from a process whose code under test runs git itself. `scratch_repo.py --audit` reads every self-test's
syntax tree (a `subprocess` call whose argv is a git argv, a repository created around the fixture, or a nested helper over
the subject's own git runner is a finding); `SelfTestFixtureDriftTests` runs it and holds the fixture's rules to that one
file. **A self-test's scratch directory goes with it (kb/Work PB3194):** `scratch_dir(prefix)` makes it in the machine-wide
temp directory and registers `remove_tree` for the process's exit; `remove_tree` makes a refused entry writable and removes
it again, and prints what still cannot go (git's read-only objects defeat `shutil.rmtree(ignore_errors=True)` on Windows,
silently: one host's temp directory held 7,359 such directories on 2026-10-23). The audit's SCRATCH arm finds a bare
`tempfile.mkdtemp` and any `rmtree(ignore_errors=True)` in self-test code.
**A C# test's scratch directory follows the same rules: `tests/_shared/ScratchDirectory.cs`.** `ScratchDirectory.Create(prefix)`
makes every scratch directory under ONE directory per test host, `<temp>/wiseowl-cobol-tests/<pid>-<id>`, beside
`<pid>-<id>.lock`, which the host holds exclusively until its process ends; `Remove(dir)` deletes a directory whole,
read-only entries included, and reports what it cannot (`HarnessLog.ReportLeft`), never throwing from a `finally` or a
`Dispose`; the host's directory goes at exit, and every host's first `Create` REAPS each host directory whose lock is free or
gone. `LoadAssembly(path)` loads a compiled program from its bytes, so the host keeps no mapping of the file (a witness that
loaded a program by path kept it mapped, and every delete failed on Windows in silence). `ScratchDirectoryDriftTests` holds
every C# file under `tests/` to it (no recursive delete, no temp directory of its own, no assembly load from a path, no delete
in a `try` whose catches are all empty).

**5. One git process per question kind, never one per object (kb/Work PB3582).** A Windows process start costs tens of
milliseconds where Linux pays about one, so a script that asks git one question per branch, commit or file is bounded by
process creation (one run summed the script self-tests at 1,444 s on CI's Windows runner and 144 s on Linux, with 3,048 git
processes in `prune_worktrees.py`'s self-test alone). `scripts/git_batch.py` answers a whole list from one process each:
`Ancestry` (one `cat-file --batch-check` and one `git log --stdin <tips> --not <base>`, so `on_base`, `beyond`,
`is_ancestor`, the count ahead and the subjects are read from memory; a tip that names no commit raises, never reads "on the
base"), `diff_names` / `diff_patches` (one `diff-tree --stdin --always` over `<commit> <parent>` pairs), `object_ids` and
`tree_entries` / `blobs` (one `cat-file --batch` each). The survey, the landing check, the planner and the ledger read through
it, `git_batch.py --self-test` holds each reader to the one-process form, and `GitBatchDriftTests` keeps per-object ancestry
questions out of them (1,246, 846, 510 and 286 processes for the four self-tests above, down from 3,048, 1,471, 646, 363).

⛔ **A drift test holds a script self-test's arms from its SOURCE and never runs it a second time (kb/Work PB2882).** The
runner is the one place a script self-test RUNS. What it cannot see is an arm deleted from a self-test (which still exits 0),
so a Unit drift test names the arms it requires and `SelfTestArmSource.AssertDeclares` finds each label among the script's
string literals. A drift test that ran the self-test again did so under the test harness's fixed 120 s per-process budget and
went red under load with every arm passing.

**The case layout (kb/Work PB3726).** A converted script's self-test is a directory of case files under `scripts/selftests/`,
found by this runner's rule (b); the layout, its collector and its drift test are DESIGN-orchestrator-loop.md §16.2.

**5. The clock proof (kb/Work PB4358).** *The defect it removes.* PB4353's self-test planted merge-group runs dated the
day before it was written and read them through a real 24-hour window. It passed on its branch, in its merge group and
on main, then went RED for every pull request at 2026-11-01T09:00Z (#960 and #961 bounced with no fault of their own).
Nothing ran a self-test against any clock but today's, and after the expiry the local gates kept answering REUSED/GREEN
from a PASS recorded before it (the PASS key is file content only) while CI, which never reuses, ran it fresh: the local
gate and CI disagreed for a second reason (the CI invariant, kb/Work PB1957).
*The rule* (`scripts/self_test_clock.py`, run by `self_tests.py` `Runner.run_one`): a Python self-test whose run opened
a tracked `.py` file (the runner's own files excluded) that CAN read the wall clock (`reads_wall_clock`: `now`, `utcnow`,
`today`, `time.time/time_ns/localtime/gmtime/ctime/asctime/strftime`) is run a second time, in the same budget place,
with the clock moved forward `CLOCK_SHIFT_DAYS` = 400 in every Python process it starts, and passes only if both runs do.
The shim is the first part of the runner's `sitecustomize` (the tracer's directory), so a child inherits it; it moves
`datetime.datetime.now/utcnow/today`, `datetime.date.today`, `time.time/time_ns` and the argument-less
`time.localtime/gmtime/ctime/asctime/strftime`, once each (the self-test checks every one against the unshifted clock),
and leaves `time.monotonic` alone. The detection over-approximates on purpose (an extra run is harmless; the second run is
the semantic check, so nothing is judged from intent); a file that does not parse reads no clock.
*Reuse.* A PASS records `clock: {shift_days, real, until}` (the day the proof reaches, `Clock.until`: recorded at + 400 days for
a shifted self-test, the day it was recorded for a clock that cannot be shifted), and `PassStore.lookup` refuses a record
past it (`re-run: the clock proof of the PASS of ... ended ...`), so a reused PASS cannot outlive the proof it rests on.
Only a self-test that cannot read a clock records none and is reused whatever the day. The schema is 3: a PASS recorded
before the proof is not reused.
*The opt-out, declared.* A self-test that compares the Python clock with a REAL one the shift cannot move declares it, with
the reason, among its first five lines: `# SELF-TEST-CLOCK: real — <why>`, and is not shifted. It is
not reused past the day it was recorded either (review R1-2: a declared PASS with no horizon would be answered REUSED
forever while CI runs the self-test fresh). Measured 2026-11-01 over every self-test (PB4353's shifted-clock sweep, repeated
through this runner): the Python ones that compare file modification times, a process's start time or git commit times with
the clock declare it (`gate_slot.py`, `prune_worktrees.py`, `orchestrator/test_plan_wave.py`, `orchestrator/test_alloc.py`;
`spec/impact_store.py` did until PB4328 deleted it), and so do the runner's own self-test (`self_tests.py`: its arms nest
runners that move the clock themselves and compare it with modification times) and this module's (`self_test_clock.py`: its
arms compare child processes' clocks with its own); every other shifted self-test passed. The runner removes
`SHIFT_ENV` from the environment of every first run, so a run is at the real clock whatever process the runner is in.
Measured at the first gate that ran it: 49 of 138 self-tests shifted, which adds about 30 % to the summed self-test seconds
of a full run (CI; the shifted ones took 46 % of the summed seconds, each run twice) and nothing to a gate whose reused
PASSes are inside their horizon.
*Each reading is the one shifted instant.* The shim derives `datetime.now/utcnow/today` and `date.today` from
`time.time() + shift` through the host zone AT that instant (`fromtimestamp`), never from today's wall time plus a
`timedelta`: the sum is an hour off `time.time()` whenever the host zone's offset 400 days on differs from today's (a
daylight-saving change between now and then), which would make the module's own self-test red on those dates on a
local zone while CI (UTC) stayed green (review R1-1).
*Residue, named.* A PowerShell or bash self-test is not shifted (`Get-Date` and `date` read a clock no Python process
moves; the two PowerShell ones that compare a meter reading with the clock are the 2026-11-01 sweep's other false reds).
One whose CLOSURE reads a clock is held to the day it was recorded, like a declared real clock. The closure is what the
PASS is already keyed on (`Deriver.closure`: the files its text names, transitively, and the directories it reads whole,
so a sourced helper, a fake and a library are in it) plus the tracked `.py` files its Python children opened (the
tracer's trace); a script of it matching `SCRIPT_CLOCK_RE` (`Get-Date`, `[DateTime]`, `date`, `$SECONDS`, `stat`,
`-mtime`...) or a Python file of it that reads the wall clock makes it so (review R2-1: the first cut read only the
self-test's own text, so `test_orchestrate_lane.ps1`, whose clock reads are in `orchestrate.ps1` and its fakes, recorded no
horizon and was reused forever). Measured: 19 of the 23 PowerShell and bash self-tests are day-bound, an over-approximation
(`date` in a comment) that costs one re-run a day each. A script the closure does not name (a path built from a variable
at run time) is the remaining residue. A clock that is not Python's (an mtime, a process start time, a commit time) cannot be
shifted by this shim: the self-test that reads one declares it real. CI's merge group and the nightly still run every
self-test at today's clock. A clock every process agrees on (libfaketime on Linux, the note's first candidate) is the
root fix for the PowerShell and bash ones.
*No arm limits how long a process takes.* The module's arms compare each child reading with the interval the PARENT's own
clock brackets the child's run in (a slow start-up only widens it), and an allowance (`RESOLUTION`, two seconds in the module's arms, one in the planted `test_child.py`) covers the clocks'
rounding and the whole-second readers, never slowness (review R1-3; the owner's no-wall-clock rule).
*Proof.* `self_test_clock.py --self-test` (the detection's arms; the shim in a child process against the parent's
bracketing clock, every function once; a shift across a daylight-saving change under a POSIX `TZ` with a daylight rule;
the horizon, the day-bound clock) and `self_tests.py --self-test` (`fleet_arms`: a fixture written at arm time and compared
with the wall clock is RED in the proof run and records no PASS; the same test deriving its fixture from the clock, a child
process that must agree with its parent, a self-test that cannot read the clock and one declared real; an mtime comparison
undeclared is RED; the recorded horizon; reuse inside it and a fresh run past it; a declared-real PASS of an earlier day is
not reused; a bash self-test reading `date` is day-bound, so is one that sources a helper reading `date` and one whose
traced Python child opened a file that reads the clock).

#### 3.14.11 The one Python interpreter the gate scripts run (kb/Work PB1637, PB3197)

**The rule.** A name on PATH is not an interpreter: on the Windows dev box `python3` is the Microsoft Store alias,
which runs, prints "Python was not found" and exits non-zero; a stock Ubuntu host has no `python`; and in the cloud
image an older `python3` can stay first on PATH. So every gate script runs ONE interpreter, resolved by PROBING each
candidate, in order an explicit `$PYTHON`, then `python3`, then `python`, by running it: a candidate passes only when it
is exactly `toolchain.env`'s `PYTHON_VERSION`, the interpreter CI runs (kb/Work PB4007, §1.4); nothing runnable is a
loud refusal, never a skipped check.

**Where it lives.** The probe has to run before any Python can, so it is written once per shell that starts a gate:
`scripts/python-resolve.sh`, sourced by every bash script that runs Python (`build-local.sh`, `linux-gate.sh`,
`battery.sh`, `measure-battery-determinism.sh`, and CI's conformance-shard population steps),
and its PowerShell twin `scripts/python-resolve.ps1`, dot-sourced by `build-local.ps1`. Each sets `PY`; the script then
runs `"$PY"` (bash) or `& $PY` (PowerShell). A script with a verdict line of its own keeps it: `build-local.sh` and
`linux-gate.sh` source the resolver in a subshell and `build-local.ps1` catches the twin's terminating error, so a host
with no runnable Python ends `=== BUILD-LOCAL GATE: NOT RUN [interpreter] — … ===` or `=== LINUX GATE: NOT RUN
[interpreter] (…) ===`, exit 2, as
every other NOT RUN does. `linux-gate.sh --self-test` drives the resolver through that path with stand-in interpreters
(a `python3` below the floor is passed over, `$PYTHON` is probed first, none is NOT RUN with exit 2). Only a provisioner
that installs the interpreter itself (`scripts/cloud/setup-env.sh`, `scripts/wsl/setup-wsl.sh`: `uv python install`)
names one directly. The rule covers the scripts; CI's other workflow steps name the runner image's interpreter
(`python3` on ubuntu, `python` on windows) and are not scanned.

**The audit.** `scripts/toolchain_audit.py` (its `--self-test`, run by `self_tests.py`): no bash script under `scripts/` and no
PowerShell twin of one names an interpreter as a command (`python3 x.py`, `command -v python`, `& python`); every script
that runs `$PY` sources its shell's resolver; and the two resolvers probe the same candidates in the same order with the
same floor probe.

#### 3.14.12 A gate outlives its caller, and a measured tree is gated once (kb/Work PB3536)

A workflow agent's return kills the process TREE of every background command it started (the harness walks the tree; the
agent's shell is in no job object). A gate whose caller returned mid-audit therefore had no verdict and cost a finisher whose
only job was to re-run the same gate at the same head (wave 1150, 2026-10-22). The bar: the gate's result is PERSISTED and
AWAITED by the orchestrator, never left to an agent's turn budget.

**1. Detached, and followed.** `gate.py` (what `build-local.ps1` and `.sh` run) makes the run directory and starts the gate
itself (`--run <run>`) through `scripts/detached.py`, THE one launch that outlives its caller: a launcher that starts the
target and exits at once (Windows: its own process group and a console with no window, out of the caller's job where the job
allows it; POSIX: its own session). The landing lane starts its runners through the same function (DESIGN-orchestrator-loop.md
4.9). The run directory holds `gate.log`, `gate.pid` and, at the end, `verdict.json`. The caller FOLLOWS it: it echoes the log
as it grows and exits with the gate's own exit code once the verdict file is written, so every caller that blocks on the
`=== BUILD-LOCAL GATE: ` line (agents, `enqueue.py`, a person) sees the same thing; a caller that is killed leaves the gate
running to its verdict. The gate writes `gate.pid` itself, UNDER its worktree's gate lock, so a pid file proves the gate took
the lock. ⛔ **The window before the lock (kb/Work PB3707).** The run directory exists and its child is alive for the moment
of interpreter start before the child takes the lock; neither fact says a gate is coming. The launcher therefore records the
child it spawned in `gate.spawn`, and the child's first act under the lock replaces it with `gate.pid`. `gate.gate_phase`
reads a run by THREE facts, never a pid alone (Windows reuses pids): the gate lock is held by a gate writing this run
(RUNNING); else the run has no `gate.pid`, is younger than `START_WINDOW_S` (60 s, read from the stamp in its name) and its
spawn record names a live process (STARTING); else the gate DIED. `enqueue.gate_busy` reads RUNNING and STARTING as a verdict
still owed (exit 12, never exit 14), and `--await` reports STARTING PENDING. To stop a gate, end the process `gate.pid` names.

**2. One way to wait: the verdict file.** `gate.watch` (the CLI `gate.py --await <run> [--worktree W] [--wait-s N]`) reads the
run directory, never a log's tail: the record's line and exit code once `verdict.json` is there; `NOT RUN [verdict]` (exit 2,
BRANCH) when the gate DIED without one; PENDING (exit 4; exit 3 is BUSY, kb/Work PB3527) when the wait passes first; `NOT RUN
[run]` (exit 2, ENVIRONMENT) when there is no such directory. A relative `<run>` (`TestResults/build-local/<run>`) names a run
of the worktree that gated it, never of the reader's checkout: `gate.resolve_run` is the one resolution, and the follower, the
rolling wave's await and `reports.Report.gate_state` all read through `watch`.

**3. The handoff.** An implementer or finisher that reaches its TURN CAP while its gate still runs on its committed head, with
nothing uncommitted, returns status `GATE-PENDING` with `gate_run` and `head`, its report's header saying `Status:
GATE-PENDING` and `Gate run: <run>` (MANDATORY-PRACTICES P2's one exception). The rolling wave (`wf_rolling_wave.js`
`settleGate`) awaits the run inside the agent's own ceiling, one window at a time: GREEN continues exactly as a DONE return
carrying that verdict (the finished test, then the branch review), any other verdict sends ONE gate finisher
(`gate_fix_rounds`), and no verdict by the last window, or the ceiling, is STALLED. The persisted verdict also needs no
awaiter: `reports.Report.finished` calls a GATE-PENDING report finished exactly when its run's verdict is GREEN for the
report's HEAD and measured that commit's own tree, unchanged while it ran.

**4. Reuse: a measured tree is gated once.** Every verdict record (schema 5, `gate.RECORD_SCHEMA`) carries the HEAD, the TREE
the gate measured at its start and again at its end (`Host.measured_tree`: the git tree id of the working tree, tracked files
as on disk and untracked files git does not ignore, written through a private copy of the index that keeps the index file's
own mtime, git's racy-git reference; a path `coord.NEVER_WORK` names measures at HEAD's content, so the harness's edit of the
tracked `.claude/settings.local.json` is no difference and a GREEN at a clean commit measures as that commit's own tree), the
base it planned against and its exit code. A gate whose tree and base equal those of an earlier GREEN run in this worktree
whose tree did not change while it ran, written by a gate that ran the whole population (schema 5 on, kb/Work PB4328), takes
the worktree's gate lock, answers from that record WITHOUT running an audit, a slot, a build or a host, and says so: `===
BUILD-LOCAL GATE: GREEN — REUSED (kb/Work PB3536): tree … at head … was gated in run <the measuring run> and has not changed
since, so nothing ran (run <this run>); that gate: … ===`. It is the self-test reuse rule (§3.14.10) extended to the gate,
keyed on what was measured rather than on a branch or a head; a RED is never reused. A reused record carries its own seconds
(`timings.wall_s`), so `landing_measure.py` counts what each gate cost.

#### 3.14.13 The growth trigger, and the program differential it would build (kb/Work PB4333, step 10 of PB4323)

The whole-population gate grows with the suite (Conformance: 10,503 cases on 2026-10-05, 20,406 on 2026-10-31), the owner's
"drift back to more delay" built in. PIPELINE-REVIEW.md §3.4 answers with a TRIGGER, and nothing is built until it fires:
**the weekly median of the code lane's test phase (`timings.tests_s`) exceeds 6 minutes, or Conformance exceeds 1.5 times
its 20,406 cases (30,609).**

**The mechanism** (`scripts/gate_growth.py`). A gate's `verdict.json` goes with its worktree when the branch lands, so a
week of them is never on disk at once. `Gate._record_growth` therefore writes one small file per GREEN code-lane gate that
ran its hosts (a RED, a REUSED verdict and the inert lane are no sample) under `<git common dir>/cobol-gate-growth/`: the
run's UTC start, `tests_s` and the Conformance cases discovered. `gate_growth.py status` reads the trailing 7 days and
prints one line, which `session-probe.ps1` shows every session: **WITHIN**; **FIRED** (the population arm needs no window;
the time arm needs a complete one); **PARTIAL** (the samples span under 7 days or number under 10: the median is shown, never
judged, because one afternoon of a dozen concurrent gates is not a week: the first reading, 2026-11-01, was a 7.7-minute
median over 16 gates in a single day, the box's three-gate steady state of §3.14.5); or **NOT MEASURED**. FIRED exits 1 and
names the next step.

**The differential, if it fires** (not built). The merge group writes, per main commit, a manifest of case to hash of the
emitted program; a local code-lane gate whose diff leaves `src/WiseOwl.Cobol.Runtime` alone compiles the population, and
runs only the Conformance cases whose program bytes differ from main's, plus every case that is not a compiled program. It
is sound only while the other inputs of a case are byte-identical to main's: the runtime, and the harness (the test
assembly's code, its goldens, the build properties). It is built only if the median fraction of changed programs over the
newest 20 non-runtime landings is under 10 % (`python scripts/arch/emit_fraction.py`, which captures each landing and its
first parent through the architecture oracle, `scripts/arch/capture_oracle.py`: one capture mechanism).

**What was measured on 2026-11-01** (the oracle's capture of each of the 20 newest non-runtime landings of `8fd4ad387`,
2026-10-31 to 2026-11-01, and of its first parent: a population of about 15,700 cases of which about 4,700 emit a
program; evidence in kb/Work PB4333): the
median fraction of changed programs is 0.00 % (0.03 % counting the programs a landing adds, which have no entry in main's
manifest); the largest is 0.08 % (0.34 %); 2 of the 20 landings changed the emitted C# of any program (1 and 4 programs).
The review's three landings (0.0, 0.4, 0.9 %) were measured the same way. So the 10 % criterion is met by two orders of
magnitude. What the criterion did NOT measure is the other half of soundness: of the 54 code-lane landings since
2026-10-31, 28 (52 %) leave the runtime AND the Conformance harness alone (`tests/_shared`, the Conformance assembly's
code, `conformance-suite/harness`, the build properties); the other 26 change an input the differential must treat as
"run everything" unless a finer rule is proved. A builder starts there: the selection is only as sound as the
classification of a harness change, which is the failure class that ended the impact map (kb/Work PB2696, PB4228).

### 3.15 THE LINUX REPRODUCTION TOOL — CI's Linux legs under WSL, run by hand (kb/Work PB1732, PB4327)

> ⛔ **The Linux gate is a HAND-RUN reproduction tool, and no per-change gate runs it (owner 2026-10-31, kb/Work PB4327;
> PIPELINE-REVIEW.md section 7).** From 2026-10-11 to 2026-10-31 the local gate ran a LINUX HALF OF THE TARGETED LEG in a WSL
> clone; it re-ran the leg Windows had just run, cost 167 s a gate serially and was red once in 78 records (a
> population-check false red), and no compiler test was red on Linux alone in October's 31 incidents. CI's merge group runs
> the whole Linux population and every Linux self-test (the `audits` job). `linux-gate.sh` keeps its whole-leg form as the
> tool that reproduces a Linux CI red on this box (`reference wsl_linux_repro`); a host-I-O group runs its unit and
> conformance legs at its own gate (MANDATORY-PRACTICES I8). **Accepted risk:** a Linux-only compiler red is found in the
> queue at the cost of one group, expected 0 to 1 a month; if two months show more than 2 a month, the response is a nightly
> whole-population WSL run on main, never a per-change Linux half.

CI runs most of its test jobs on `ubuntu-26.04` (pinned), and every local gate runs on the Windows host. **`scripts/linux-gate.sh`**
(`bash scripts/linux-gate.sh [--legs unit,characterization,conformance,guard] [--nice]` at a tree's root; on a host that is
not Linux it runs itself under WSL through `wsl.exe -d Ubuntu --cd <tree> --exec bash -lc …`, kb/Work PB2999) runs the test
projects CI's Linux jobs run and the scripts they run: leg `guard` runs `bash scripts/guard-fast.sh` with a `TMPDIR` private to
the clone, leg `selftests` runs `scripts/self_tests.py` in the clone as CI's `audits` job does, and `selftests-built` runs
`self_tests.py --built` against the one build (kb/Work PB2563, PB2879). `scripts/test_linux_gate.py` holds the default legs equal to
the workflows': every `dotnet test` project of a Linux job of `build-and-test.yml` and every `run: bash scripts/….sh` step of
a Linux job of it or of `nightly.yml` is a default leg.

**How: a Linux clone of the commit, never the Windows tree.** The tree's COMMITTED HEAD is cloned `--shared` (it borrows the
Windows repository's object store read-only) into `~/linux-gate/<tree>.<pid>/<tree>` on the Linux filesystem, the solution is
built there ONCE with the Linux SDK, and every leg runs in PARALLEL against that build, each to its own log (234 s against
351 s serially). The git-ignored GnuCOBOL corpus is copied in or fetched as CI fetches it. Commit before running:
uncommitted changes are counted, reported and not tested. Two earlier designs failed, each measured: (1) Windows-built
binaries run `--no-build` bake `[CallerFilePath]` goldens in as `E:\…` paths, which are relative on Linux (363 false reds);
(2) the Windows tree with `GIT_DIR`/`GIT_WORK_TREE` exported let those variables reach every test process, and
`gate_slot.py`'s self-test wrote `core.worktree` into the REAL repository's shared `.git/config` (2026-09-29). So the script
exports nothing; its only git calls on the tree are READS (HEAD, the git directory, the count of uncommitted changes, the
tripwire below), all through one function, `wgit`, the TREE'S OWN git without optional locks (a Windows-drive tree is read by
`git.exe` through WSL interop: Linux git re-hashed ~16,000 tracked files across 9P for one `status`, 34 s, kb/Work PB2880).
`scripts/test_linux_gate.py` holds every git call on the tree to `wgit` and every arm of it to `--no-optional-locks`. A self-test
builds its repositories through `scratch_repo.py` (§3.14.10), and the script snapshots the real repository's `HEAD` and
`core.worktree` before the legs and is RED (`repository-written`) when either changed.

**The gate leaves nothing behind on the Linux filesystem (kb/Work PB3194).** One built gate is about 1 GB and 80,000 inodes;
345 leftover clones once filled the disk and every Linux gate on the host ended NOT RUN until someone deleted clones by hand.
Now: each gate works in its own `~/linux-gate/<tree>.<pid>/` (the clone, the guard leg's `guard-tmp` beside it, and an `owner`
file naming the gate's process by pid and start time, so a reused pid is never mistaken for it); it removes that directory on
every exit path (the EXIT trap after its verdict, and `on_signal` for HUP, INT and TERM, which first ends the legs' process
trees read from `/proc`; the one build runs in the background because bash defers a trap until a foreground command returns);
a gate killed outright is REAPED by the next gate (an owner that no longer runs, or no owner file and this tree's or older than
3 hours); and it refuses, `=== LINUX GATE: NOT RUN [disk] (…) ===`, below 1,000,000 free inodes or 20 GB free (`disk` is an
ENVIRONMENT cause in `gate.NOT_RUN_CAUSES`). `linux-gate.sh --self-test` drives each exit path on real directories and
processes and each bound over a stand-in `df`; its signal arm is SYNCHRONIZED through fifos, never timed, and its grandchild
takes the first signal with `sigwait` (a `bash -c` witness with a TERM trap was wrong: 2 of 256 runs recorded PROBE although
the TERM had been sent). That arm reads `/proc`, which Cygwin serves slowly, so on a Windows host it prints `n/a` and runs in
the Linux `selftests` leg and CI's audits job.

**Line endings.** Every tracked text file is LF on every checkout (`.gitattributes` `* text=auto eol=lf`, kb/Work PB3029,
§3.17.4); a CRLF script dies at its first line under bash.

**Verdict.** One `=== LINUX GATE: GREEN|RED|NOT RUN [<cause>] ===` line naming the HEAD it tested; NOT RUN is never green and
its cause's class is `gate.NOT_RUN_CAUSES`' (§3.14.3). All four legs run by default, about 3–4 minutes for the three test
assemblies and about 6 with the guard leg (2026-10-04: unit 137 s, characterization 10 s, conformance 127 s, guard 81 s).
**Limits.** The local WSL distro is the same Ubuntu release as the pinned runner but runs Microsoft's WSL kernel, and the tool
runs Debug where CI also runs Release (kb/Work PB1957 lists every remaining difference): it catches platform assumptions, and
CI's merge-group run remains the proof.

### 3.16 THE READYTORUN RUNTIME — the gates and the package run the same precompiled runtime (kb/Work PB2528)

**Owner decision 2026-10-07:** "Build the COBOL runtime ReadyToRun for both the gates and the shipped WiseOwl.COBOL
package (rebuilt automatically whenever runtime code changes)?" — "Yes, gates and package". Every Conformance case
runs its compiled program as a child process, `dotnet <prog>.dll`, with `WiseOwl.Cobol.Runtime.dll` beside it, so the
JIT compiling the runtime's methods was paid at every program start; the w1033 gate-speed diagnosis put the child at
60–75 % of a case's single-thread cost.

**The mechanism (one producer, one selector, two guards):**
- **Producer: `src/WiseOwl.Cobol.Runtime/WiseOwl.Cobol.Runtime.csproj`.** After its own compile, target
  `CobolRuntimeReadyToRunImages` runs the SDK's ReadyToRun publish of the same project once per platform in
  `CobolRuntimeReadyToRunIdentifiers` (the root `Directory.Build.props`: `win-x64;linux-x64`, the operating systems
  CI runs on), in Release whatever the outer configuration, framework-dependent, into `bin/ReadyToRun/<rid>/`, the
  platforms in parallel. Crossgen2 compiles each platform's image from any host (a Windows build produces the
  Linux image too). The images carry no source-control stamp, as Debug assemblies do not (§3.12): an image is a
  function of its sources, so a commit that does not touch the runtime leaves it byte-identical. Incremental through
  the SDK's own Inputs/Outputs: the compile and crossgen rerun exactly when the runtime's sources or build files
  change, with no manual step. `RuntimeIdentifiers` and `PublishReadyToRun` are static properties so that restore
  fetches the crossgen2 pack and each platform's runtime pack (`DisableTransitiveFrameworkReferenceDownloads` keeps
  it to `Microsoft.NETCore.App`).
- **Flow.** Target `_CobolReadyToRunContent` adds each image as a content item linked at
  `ReadyToRun/<rid>/WiseOwl.Cobol.Runtime.dll`, so it reaches the output of every referencing project (the compiler,
  the CLI, the three test hosts) and the CLI's publish output, which is what the `WiseOwl.COBOL` tool package packs
  (`PackAsTool` publishes, then packs; DESIGN-USER-DOCUMENTATION §6).
- **Selector: `src/WiseOwl.Cobol.Compiler/CodeGen/AssemblyPackager.cs#RuntimePath`.** This platform's image
  (`ReadyToRunPlatform`: the operating-system family and processor architecture, which is what decides whether an
  image's native code is usable, not `RuntimeInformation.RuntimeIdentifier`, which a distribution-built .NET reports as
  e.g. `ubuntu.26.04-x64`); on a platform with no image, the portable assembly beside the compiler, the same source
  compiled by the JIT. `RoslynBackend` binds every program against that one file (the host's own copy of the runtime
  is dropped from the reference set) and `Package` deploys it, so a program runs with the file it was compiled
  against. The compiled-program cache keys on it (`CompiledProgramCache.ToolFiles`, §3.12), because it is not the
  assembly loaded into the test host.
- **Guard 1: `CobolAssertReadyToRunRuntimePublished`** in `src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj` fails a CLI publish
  that would ship without any declared platform's image.
- **Guard 2: `ReadyToRunRuntimeDriftTests`** (Unit) reads the PE headers rather than the build's naming: the runtime
  the compiler deploys is this platform's ReadyToRun image (managed native header present, machine field equal to
  the architecture combined with the operating-system code of the .NET runtime's `pedecoder.h`) with no source stamp;
  every declared platform's image is in the test host and in the CLI output, byte-identical; and the declared
  platforms cover every `runs-on:` runner in `.github/workflows/` (a runner the test cannot name fails it).
- **Off switch: `CobolRuntimeReadyToRun=false`.** It is the condition of the CLI's publish guard (Guard 1);
  nothing in the tree sets it now (the impact recorder that did was deleted with the impact map, kb/Work PB4328).

**What the gate certifies.** The gate's programs load the Release ReadyToRun runtime, not the Debug build of it: the same image
the package ships. `src/WiseOwl.Cobol.Runtime` has no `Debug.Assert`, and CI's Windows Conformance shards already ran Release,
so no check is lost; the Debug portable assembly is still what the test hosts load in-process.

**Measured** (2026-10-07, shared host; 24 stride-sampled `tests/conformance/2023` programs, 21 runs each, the two runtimes
interleaved): a program start's median fell from 104.7 ms (Debug portable runtime) to 74.5 ms (ReadyToRun), −29 %. A change to a
runtime source costs about **+4 s** of build (two Release compiles and two crossgens, in parallel); a no-op build is unchanged,
and a clean restore downloads the crossgen2 pack and the two runtime packs once.

### 3.17 THE LANDING PIPELINE — one whole-population gate at the head, the merge group as the first run of the merged tree, one pull request per branch through GitHub's merge queue (kb/Work PB3015, PB4323; owner decisions 2026-10-11 and 2026-10-31)

#### 3.17.1 The decisions this section stands on

Measured on 2026-10-10 (frozen: `evidence/fleet-optimization/2026-10-11-pipeline-measurement-2026-10-10.md`): of 567 lander
minutes, 233 (41 %) were local whole-population gate runs (7–13 min on a quiet host, 25–53 min under load, 8 of 15 RED, mostly
from fixed time limits under load) and 94 (16.5 %) were a direct-push landing script holding a lease while CI ran the same
population a third time. The owner, 2026-10-11:

- "We are free to completely rearchitect the build pipeline as needed to be maximally productive and minimally costly."
- The principle: "We need local testing to confirm the targeted changes are correct. The CI / CD should primarily ensure no
  regression on the remaining codebase. Another consideration, local runs are free." (The repository is PUBLIC, so
  GitHub-hosted standard runners are free for it.)
- Landing goes through a **GitHub merge queue** on `main`; the DEVLOG becomes **one file per entry**, rendered as a view;
  **one pull request per finished branch**, and the train and the lander role retire.

The operator's constraint (2026-10-11): `E:` is ReFS and CI's Windows runner is NTFS; they delete differently (the PB2917 race
reproduced here in 20 of 7,200 probes and may not on NTFS), so the Windows file-system race witnesses run in the local gate and
PB1957's local-versus-CI survey lists the filesystem.

**Narrowed on 2026-10-31 (kb/Work PB4323, the redesign from measurement; §0).** The local run is no longer a targeted leg: the
owner's decision 1 made the population the selection (kb/Work PB4328), the Linux half left the per-change gate (kb/Work
PB4327), the landing gates nothing (kb/Work PB4331), and the CI invariant of 2026-10-04 became THE RULE of §0 item 5 ("This does
weaken the CI must pass if local passes rule. I'm okay with that"). The merge queue, one pull request per branch and the
one-file-per-entry DEVLOG stand.

#### 3.17.2 The contract — what every change goes through, in order

1. **Implement**: an implementer in its worktree; its gate is the one whole-population gate (§0 item 1), its report `Status:
   DONE` with the O11 header; it writes its own **DEVLOG entry file** (§3.17.4) and flips its notes.
2. **Review on the branch** (MANDATORY-PRACTICES I10; kb/Work PB3027): a read-only reviewer (`cobol-reviewer`, the `review`
   skill's "branch form": the full-code pass over `origin/main...HEAD`, the drift rules of the changed files, fixed time limits
   in tests, `cite.py --check`, the other dimensions where the diff warrants) runs THE MOMENT the branch is DONE, inside the
   wave's slot for that group (`wf_rolling_wave.js` `reviewed()`). Every decision is a line in
   `<scratch>/review-w<wave>-<letter>.jsonl`, appended through `scripts/orchestrator/review_findings.py`, which computes the
   verdict from the file and stamps it into the report's header (`**Review:** CLEAN <reviewer> <head>` or `FINDINGS <n>
   <reviewer> <head> <file>`), read back through `reports.parse_review`, the one reader.
   FINDINGS go to a REVIEW FINISHER (a fresh implementer: a workflow cannot message an agent that has returned), who merges the
   branch, answers every finding by its id (fixed at its root, or `no_change_needed` with the reason), re-gates and returns
   DONE; the branch is reviewed again, at most `review_fix_rounds` (2) times, after which the FINDINGS stamp stays and
   `plan_wave.py` plans a finisher for it while the stamp covers the branch tip. **The rounds converge (kb/Work PB3381, owner
   2026-10-18: "We should not be holding finished work this long").** Round 1 is the whole branch; later rounds review ONLY
   `git diff <the head the last round reviewed>..HEAD` and the report's answers. Every finding carries a severity (critical,
   high, medium, low, info) and the verdict is a threshold (`review_findings.py verdict --blocking`): critical, high and medium
   block in every round a finisher can still answer, critical and high only in the round after the last finisher; low and info
   never block, and each non-blocking one is a LEAD the stamp lists under `## Review findings` and the wave returns in
   `leads`. **Nothing is held for a dependency successor** (a one-file chain lands ONCE instead, with its last link: kb/Work PB4396, `prune_worktrees.chain_links`): the successor starts once the predecessor's review loop has ended,
   merges the REVIEWED head, and its pull request merges on top of the predecessor's. Unanswered findings have ONE source (the
   predecessor's findings file) and ONE reader (`review_findings.last_verdict`: round, branch, head, verdict and the number of
   blocking findings), which the wave's `predecessorEnd` and `prune_worktrees.py` `carrying_findings` both call; a report
   stamp is only their rendering. While the last verdict is FINDINGS with blocking findings at a head a successor contains, the
   survey holds the successor IN FLIGHT (`waits_on`); `capped` (`review_findings.capped`, the round after the last finisher) is
   the predecessor's next-wave finisher's, an uncapped one is the successor's review's (`prune_worktrees.review_answers`).
   Every content problem in a findings file is a TEAR (unreadable, a byte-order mark, bytes that are not UTF-8, no final
   newline, no record, a line that is not a record under `review_findings.SCHEMA`), and a TORN file holds every branch it names:
   `last_verdict` returns TORN, `unanswered`, naming the file, line, offset and reason, and never raises; every writer refuses
   a torn file BEFORE writing, naming `review_findings.py repair <file>`, which only the operator runs (the review lane alone
   mends a file torn at its LAST line, kb/Work PB3608). `review_findings.py --self-test` cuts a findings file at every byte
   offset and mutates every schema field. Only a branch stamped CLEAN at its tip is landable, and the landing step ENFORCES it:
   `enqueue.py land` refuses (exit 11, UNREVIEWED) a branch whose newest CLEAN stamp names no commit of it, or after which it
   gained a commit that is not main's, a clean merge of main (its tree the auto-merge of its parents) or register-only; the
   lanes I10 does not review (the registrar, the golden lane, the clerk) name themselves with `--lane`.
   **REGISTER-ONLY commits (kb/Work PB3849).** One coverage rule answers for the review gate, the landing's reading of the gate
   record, the report's coverage and the branch survey: `prune_worktrees.mechanical` counts a commit as covered by the head
   before it when it is main's, a clean merge of main through the drivers, or REGISTER-ONLY: every path it changes is a DEVLOG
   entry or a `kb/Work/*.md` note (`prune_worktrees.register_path`). What can be wrong with such a commit is audited
   mechanically at every landing and in CI's `audits` job (`devlog.py check`, `work.py check` and, through it,
   `work_inventory.py`).
3. **Enqueue** (`scripts/orchestrator/enqueue.py land <branch>` — a SCRIPT, deterministic, no agent, started by the loop's
   landing lane the moment the branch is WAITING TO LAND; DESIGN-orchestrator-loop.md 4.9): fetch; the review stamp (step 2,
   exit 11); the branch's LAND LOCK (`<git dir>/cobol-land.lock`; a second `land` of the branch is exit 13, never a red); then
   **READ THE RECORD OF THE BRANCH'S OWN LOCAL GATE AND GATE NOTHING** (owner 2026-10-23, kb/Work PB3612: "Developers merge
   separate branches into a continually moving main all the time without problems … Why don't we do that?"; the landing lane
   landed 1.47 changes an hour while it re-gated). A branch is gated locally ONCE, at its head, by its implementer or finisher:
   `enqueue.tested_locally` reads the worktree's newest `TestResults/build-local/*/verdict.json` runs and refuses a tip no run
   covers (exit 14, UNTESTED, before anything is merged; the report gains `BLOCKED: enqueue — no local gate is recorded GREEN …`
   and a finisher gates the tip). A run covers the tip when it read GREEN with exit 0, measured the COMMITTED head's own tree
   (`tree` equals `head_tree`), and its head IS the tip or every later commit is main's, a clean merge of main or register-only
   (`prune_worktrees.unreviewed_since`). A record the branch's older driver wrote before a field the landing requires existed
   (`gate.LANDING_FIELDS`, schema below `LANDABLE_SCHEMA`) is its own reason, "written by a gate older than this lander"
   (`gate.predates_landing`), and its remedy is to merge origin/main into the branch FIRST and gate the merged head. A later RED
   does not unrecord an earlier GREEN of the same head. ⛔ **BEFORE it reads the record, and before every later write of the
   worktree, it asks the gate lock** (`enqueue.gate_busy`): exit 12, never a red, when a gate of the worktree runs (the gate
   lock held, `gate.live_gate`; never a process id, which Windows reuses, kb/Work PB3662) or is STARTING (spawned moments ago,
   kb/Work PB3707), WHATEVER record exists (kb/Work PB4204: an older GREEN record once let a landing merge main into a worktree
   under a running gate); the lane retries it once the lock is free. A lane I10 does not review gates its own tip.
   Then **MERGE `origin/main` into the branch** — not a rebase (a branch that merged a same-file predecessor carries conflict
   resolutions in its merge commits, which a rebase discards and asks for again, and the queue lands by merge commit anyway)
   **THROUGH THE MERGE DRIVERS** (`scripts/merge_drivers.py`, kb/Work PB3610): the files two landings both edit are merged by
   key, not by line: the keyed catalogues (`constructs.json`), the work notes two branches append to, the runtime ABI pin
   (`RuntimeAbi.pin`, measured by `scripts/abi_pin.py` on the merged tree before the merge commit) and the public-API baselines
   (`src/**/PublicAPI.*.txt`, a SET of symbol lines). `DRIVERS` is the registry, `.gitattributes` repeats its patterns,
   `merge_drivers.py --check` and `MergeDriverDriftTests` hold the two together, and git is given the drivers on its command
   line (`-c merge.<name>.driver=…`) and the attributes with them (`-c attr.tree=…`), so no checkout carries configuration that
   can go stale. A new driver lands alone (DESIGN-orchestrator-loop.md 16.9). The review's coverage rule reads the same
   `is_drivers_merge`, built on `merged_tree` (kb/Work PB3713). PROSE HAS NO DRIVER, ON PURPOSE (kb/Work PB3643):
   `merge_drivers.py --hunks <file>…` replays history's merges and showed that over `CONFORMANCE.md`,
   `DESIGN-runtime-library.md` and this document 115 conflict hunks were two sides REVISING text the base already held (against
   3 where both inserted at one place), so a union merge would keep a stale paragraph beside its replacement in a document that
   describes the current compiler; such a conflict is a decision about what the text now says, the finisher's. What is left in
   conflict is real: exit 4, the merge is aborted, the report gains `BLOCKED: enqueue … conflict in <files>`, and a
   `cobol-finisher` resolves with `merge_drivers.py merge origin/main --keep`. A CLEAN merge that could not be COMPLETED is no
   conflict (kb/Work PB3673): `merge_drivers.merge` answers `clean`, `conflicts` or `incomplete` (abi_pin's `Failure(cause, why)`),
   and the cause is read from the `merge-*` rows of `gate.NOT_RUN_CAUSES`: `merge-blocked` (ENVIRONMENT: a held build output,
   a full disk, no SDK) is exit 2, retried once by the lane; `merge-failed` (BRANCH: the merged tree's pin, build or measuring
   test) is exit 5, a finisher's. THE QUEUE'S CONFLICTS ARE SEEN BEFORE THEY HAPPEN (kb/Work PB3723): GitHub's merge group runs
   no driver and removes an entry that conflicts (`merge_conflict`) with no event any reader sees, so
   `merge_drivers.foresee_queue` replays that build with git's own line merge (`probe(drivers=False)`) on the queue's own order
   and heads, `prune_worktrees.foresee_queued` asks it on every survey, and a queued head that conflicts reads BOUNCED (a
   FORESEEN CONFLICT) while the entry ahead is still queued. The landing's last line, `=== ENQUEUE: <OUTCOME> (<branch>) ===`,
   has ONE home, `scripts/verdict_line.py` (kb/Work PB3714), and `LandingLaneDriftTests` keeps every other script from spelling
   the prefix.
   Then `devlog.py stamp` numbers the entry (once: a stamped entry keeps its number, PB3123) and the landing checks run
   (`enqueue.py checks`): `devlog.py check` (exactly one new entry file, stamped and well-formed), `work.py check` (run by the
   merged head's own copy, so a note main gained after the branch's gate meets a rule the branch added there, never first in
   CI, PB3389), `rename_diagnostic_prefix.py --check` (PB3575), `rename_retired_names.py --check` (PB3837) and
   `landing_check.py` (a path no landing carries, `coord.NEVER_WORK`; a public-API symbol listed twice, PB3694; the R3 file-set
   partition, PB2118). Two refusals are exits that are not reds, retried by the lane: exit 15 when the branch merged a
   predecessor whose pull request is still open and so adds that pull request's stamped DEVLOG entry beside its own
   (`enqueue.open_predecessors`, PB3145), and exit 16 when an open pull request that ENTERS the queue
   (`enqueue.driver_file_holders`, `queue_facts.PullRequest.entering`) changes a file a merge driver covers that the branch
   changes too (the queue merges with plain git, so the two would conflict inside it; the repository's queue-entry lock covers
   the look and the queueing, kb/Work PB3642). A branch whose tip is already inside origin/main ends exit 0 NOTHING TO LAND
   (PB3157). A gate NOT RUN, which measured nothing, is judged by its cause's class (PB3526): exit 2, no red, for an
   ENVIRONMENT cause or a BRANCH cause main fails the same way at the same step, else exit 5. Main moving again while the pull
   request is queued costs no re-gate: the merge group integrates the branch on the current main and runs the whole population.
   Finally `gh pr create` (title = the branch's one-line subject, body = the forensic message) and `gh pr merge --auto`, so
   the pull request joins the queue. No lease, no local whole population, no waiting for CI. That the pull request then
   ENTERS the queue is the landing lane's QUEUE WATCH (DESIGN-orchestrator-loop.md 4.9 step 4, kb/Work PB3527): on every tick
   it reads the queue's entries and the head's check-runs (`queue_facts.py`), waits while any runs (PB4052), and past a
   10-minute grace after their work ended re-runs a stuck check, then closes, reopens and re-queues the pull request, then
   asks the operator. A HOST-I-O group (a file of its set under `src/WiseOwl.Cobol.Runtime/IO/`) runs the Linux unit and
   conformance legs from WSL at its own gate (MANDATORY-PRACTICES I8; `make_dispatch_specs.host_io`, kb/Work PB3381).
4. **The merge queue** (kb/Work PB3024): GitHub builds the merge group (several queued pull requests on top of `main`), the
   workflow runs on the `merge_group` event (the matrix of §1.4), `ci-gate` stays the one required check, and main advances in
   order. A red bounces only the pull request that caused it (GitHub re-tests the rest). **The landing lane classifies the red
   before anyone is blamed** (§0 item 6; `land_lane.classify_red`, kb/Work PB4271, PB4331); INFRA and RUNNER are re-queued
   once, and the rest go to a finisher, whose fixed branch is reviewed again at its new tip (step 2) and enqueued again.
   **What the queue stacks (kb/Work PB3107, PB3123; owner 2026-10-13: "Due to our local testing, we should rarely see
   failures").** A merge group is built ON TOP of every group queued ahead of it, so the tree a group tests is one no branch
   ever checked. THE RULE for every check a landing or a merge group runs: **a check's verdict on a pull request may not depend
   on what else is queued.** A check that holds a branch to a committed record of its own tree fails it, because no such record
   describes the stacked tree; so does one that orders landings by a value fixed before the queue orders them. The two such
   checks were redesigned, never relaxed into a retry: (a) the arch oracle commits nothing: its baseline is the capture of a
   change's own base commit, made on demand by `compare_oracle.py` (a capture is a pure function of its commit,
   DESIGN-architecture-review.md §4.1); (b) the DEVLOG number is STAMP order, not merge order: `devlog.py check` holds a new
   entry above every entry of the tree it was STAMPED on (the first parent of the commit that added its file) and unique on
   the landing's tree, never above the group's base. `devlog.py --self-test` builds the stacked trees in both orders, and
   `LandingPipelineDriftTests` keeps the oracle's record out of the tree. The witness-loss audit and the `changes` job's
   docs-only decision read base..head, and a group's `base_sha` is the head of the group ahead of it, so the range is this
   pull request's own change whatever is queued with it; `landing_check.py` reads the in-flight branches on purpose, and only
   locally.
   **Superseded runs are cancelled (kb/Work PB3769; owner 2026-10-25).** When an entry ahead fails or leaves, GitHub rebuilds
   every group behind it with new commits and new runs, and the old runs kept holding runners under the concurrent-job limit.
   A `concurrency` group cannot catch them (each group's ref `gh-readonly-queue/main/pr-<n>-<base sha>` is unique), so every
   merge-group run starts with the `supersede` job (no `needs`): `scripts/orchestrator/cancel_superseded.py` lists the
   unfinished merge-group runs, THEN reads the head commit of every current queue entry (`queue_facts.live_heads`, in that
   order, because a run created after the read would otherwise look superseded), and cancels each run of the queue on main
   whose head commit no entry heads, never its own run and never a live entry's. ⛔ It cancels NOTHING when the queue cannot be
   read whole, reads empty, or does not hold the run's own head commit; each is a warning annotation. Every outcome exits 0 and
   EVERY step is `continue-on-error` (the checkout too, kb/Work PB3820): housekeeping never decides a verdict, and the job
   stays a `needs` of `ci-gate` because the drift check wires every job. ⛔ The job checks out only `scripts/orchestrator` and
   `scripts/standard_streams.py` (a sparse checkout), so a module its scripts import that the list leaves out would make it
   cancel nothing while the run stays green: `scripts/sparse_checkout.py` (kb/Work PB3787) reads the closure of every
   sparse-checkout job and refuses a file it runs or imports that the list does not cover (§3.17.8).
5. **On `main`**: the merged commit is the tested merge-group tree. The `push` run keeps only `changes` + `ci-gate` (the record
   the probe reads) and the cache jobs (`toolchain-cache`, `package-cache`); the matrix and the audits run on `merge_group` and
   `workflow_dispatch`. On `pull_request` only the required check runs (`changes` + `ci-gate`, kb/Work PB4330), so twenty pull
   requests do not queue twenty full matrices behind the organization's 60-concurrent-job limit (§3.17.6).

The implementer's whole-population gate stays first for every change (the owner's principle); CI is the regression net and
the only run of the merged tree.

#### 3.17.3 The gate driver's ONE mode (PB3022)

- **One shape.** The gate has no mode and no scope: the whole-population gate of §3.13–3.14 inside a gate slot, for an
  implementer's commit, a finisher's re-gate and a landing's own-tip gate alike (kb/Work PB3022, PB4328).
- **No Linux half.** The Linux half of the targeted leg (kb/Work PB3612) was removed (kb/Work PB4327, §3.15): measured against
  78 gate records it cost 167 s a gate, re-ran the leg Windows had just run and was red once, a false red. `LinuxLeg`,
  `Host.runs_linux_leg`/`linux_leg`/`linux_gate_holder`, `plan_leg`, the `linux` field of `verdict.json` and `linux-gate.sh`'s
  `targeted`/`inert` forms are deleted, and `scripts/test_linux_gate.py` holds them gone. The risk accepted is §3.15's.
- **Host-only witnesses and tree-wide sweeps are named by trait** (kb/Work PB3022): every Unit test class that drives the
  file-sharing layer's host types from concurrent workers carries `[Trait("Host", "ReFS")]`, and every drift test that
  `drift_rules.py --sweeps` classifies as a tree-wide sweep carries `[Trait("Sweep", "tree")]`; membership is DERIVED from the
  trait, never listed, and `drift_rules.py --check` (the RENDERED VIEWS audit, in every lane) asserts it over every test
  project's sources, with a planted fixture for each shape in its `--self-test`. The whole-population gate runs them all; CI runs the same
  tests on NTFS, where the race may not reproduce.
- **Audits only when their inputs changed** (§3.14.10): a script self-test, a drift-rules regeneration or a citation check with
  inputs unchanged since a recorded PASS is reused; CI's `audits` job runs them all on every merge group.
- **No fixed time limit in any test** (owner rule 2026-09-25): the Unit drift tests that re-ran a script self-test hold its
  arms from source (`SelfTestArmSource`); regex match timeouts are `RegexOptions.NonBacktracking` (linear, so no clock); a
  generator's stream wait is unbounded; `NoWallClockAssertionDriftTests.NoTestSetsALiteralTimeLimit` fails on a new literal
  limit in any shape. What remains is the ONE harness budget rule (`ObservationBudget`, §3.10), a hang guard whose expiry is
  a named non-observation, never a value.

#### 3.17.4 Conflict-free landing files — the tree holds SOURCES, views are RENDERED (PB3025, PB3026)

Two queued pull requests that both insert at the top of one file collide, and the second is bounced; the queue is only as
parallel as the files are disjoint. The rule: a landing adds or edits files that are ITS OWN; anything derived is rendered on
demand and not committed.

- **DEVLOG (PB3025).** `DEVLOG/NNNN-<slug>.md`, one file per landing, header `## Entry NNNN — YYYY-MM-DD HH:MM TZ — Title`.
  The implementer writes the entry WITHOUT a number, as `DEVLOG/pending-<slug>.md`; the number is stamped AT ENQUEUE
  (`devlog.py stamp`, called by `enqueue.py`: `alloc.py devlog` allocates `NNNN`, the file is renamed and its header
  completed, one commit on the branch before the pull request opens), so number order IS STAMP order (gaps are fine). It is
  not merge order, and the check never asks it to be (kb/Work PB3123, §3.17.2 step 4). `python scripts/devlog.py render
  [--since NNNN] [--out <file>]` prints the descending narrative (`DEVLOG/README.md` first, then the entries newest first;
  `DEVLOG.rendered.md` is gitignored). `devlog.py check` is the landing rule: exactly one new, stamped entry file in the pull
  request (new meaning a header the base's history lacks), numbered above every number of the tree it was stamped on, header
  well-formed; a `pending-` file, a stray file, an entry whose header disagrees with its name, two files with one number or a
  deleted entry is a red. It runs in the landing checks and in CI's `audits` job on every merge group, and its tree half
  (`lint`) runs over the repository's own `DEVLOG/` in `devlog.py --self-test`, so every gate holds the whole log to it.
  A branch that merged an unlanded predecessor lands it THROUGH itself, so `devlog.py stamp` folds every pending file the
  branch carries into its one entry, oldest first, each body under its own title (MANDATORY-PRACTICES L8); a predecessor
  reviewed CLEAN lands on its own instead (PB3381), a branch is landed ONCE (PB3693: the survey holds a WAITING TO LAND
  successor behind its predecessor, `enqueue.py land` waits with exit 15 while an open pull request already adds the stamped
  entry of a pending title the branch carries, and `stamp` drops a pending entry whose title is already a stamped entry). The
  stamp is all or nothing (PB3367): its git writes retry on `index.lock`, a failure restores the pending bytes and resets the
  index (exit 3, which `land` maps to exit 2), and an interrupted stamp is completed by the next run.
- **Line endings (PB3029; owner 2026-10-11: "standardize on a single line-ending format"): LF everywhere.** `.gitattributes`
  opens with `* text=auto eol=lf`, and a file that must keep its bytes is declared `-text` there with its reason.
  `scripts/line_endings.py --check` (a gate audit, CI's `audits` job and a `windows-self-tests` step) refuses a CR in a tracked
  text file's blob or checkout and a Python text-mode write that does not state `newline=`; `--fix` rewrites a checkout made
  before the rule and re-states the index entry of each file whose cleaned content IS the staged blob (a plain `git update-index`:
  `--refresh` trusts the stale recorded size and answers "needs update" for exactly these files), `--fix-writers` adds
  `newline="\n"` to a new writer, and the PowerShell generators of tracked files write through `scripts/LfText.psm1`
  (`Set-LfContent`). `merge.renormalize` is the documented per-clone setting (CONTRIBUTING.md).
- **Generated indexes leave the tree (PB3026):** a derived index is a RENDERED VIEW: gitignored, rendered on demand by its
  generator, never committed. `scripts/rendered_views.py` holds the ONE registry (`VIEWS`): `docs/DRIFT_RULES.md`
  (`drift_rules.py --out`), the `kb/Conformance` burn-down (`gen_conformance_notes.py --out`), `docs/DIAGNOSTICS.md`
  (`gen-diagnostics-doc.ps1`), the `kb/Reference` and `kb/Grammar` vault views, and the ledger page. A new generated index is
  one row there plus a `.gitignore` entry. `rendered_views.py --check` is the gate's `RENDERED VIEWS` audit (it runs before the
  slot) and a step of CI's `audits` job, and enforces per view: not tracked (`git ls-files` ∩ outputs = ∅), gitignored when
  inside the tree, its generator's own `--check` renders it in memory, plus a sweep that no tracked `docs/` or `kb/` markdown
  declares itself generated without a row. Nothing compares a view with a copy on disk, so "absent" and "stale" no longer
  exist; a reader that needs a view refuses with the render command when it is absent, never reads an empty one as "no
  diagnostics". NOT views, and tracked on purpose: generated SOURCE the build compiles (`*.g.cs`, `*.Table.cs`: a clone must
  build with no pre-step; each has its own drift test), DATA extracted from the licensed standard, and the gating status
  block embedded in `VERSION_CHANGE_REFERENCE.md`.
- **Plan §0 and other shared prose** are edited only by the loop's own `ledger`/§0 refresh pull request, never inside a fix
  pull request; a fix edits its notes, its code, its tests, its goldens and the design doc of its subsystem. `CONFORMANCE.md`
  becomes one source file per determination with a rendered index (DESIGN-orchestrator-loop.md §16.8, kb/Work PB3726, PB3738).

#### 3.17.5 The loop's units and the fleet (PB3023, PB3024, PB3027; DESIGN-orchestrator-loop.md §3, §4.7, §9)

- `enqueue` (`units/enqueue.md`) is the supervisor's **landing lane** (PB3103, `land_lane.py`, DESIGN-orchestrator-loop.md
  §4.9): beside whatever unit runs, the lane runs `enqueue.py land` for every WAITING TO LAND branch (`prune_worktrees.py`'s
  survey, in harm order) the moment it is ready and retries what a script can see end; the unit reads `land_lane.py status` and
  dispatches ONE `cobol-finisher` (`finisher-brief.md`) for a lane attempt stopped on a conflict, a red gate, a DEVLOG or oracle
  refusal, or a queue bounce the lane did not re-queue (§0 item 6; `prune_worktrees.py` BOUNCED comes from one GraphQL read of
  the open pull requests: in the merge queue → ENQUEUED, removed from it at the branch's tip → BOUNCED with GitHub's reason).
  While the queue is not ACTIVE, `enqueue.py land` refuses before it merges anything (exit 8): main takes only queued pull
  requests (the ruleset binds administrators).
- There is no train, no lander role and no direct-push landing script, landing lease or `ci/**` branch (kb/Work PB3023,
  PB3024; CLAUDE.md rule 4: deleted, never wrapped). `golden-lane-brief.md` is the lane-1 integrator landing through
  `enqueue.py land --lane golden`; `cobol-finisher` resolves a conflict, fixes a red or a bounce, gates and returns DONE
  (turn cap 160), its commits reviewed on the branch before the lane lands the new tip. `wf_rolling_wave.js` starts no lander:
  a wave produces DONE branches reviewed CLEAN, and the lane lands each as it comes back.
- **Measurement** (PB3028): `landing_measure.py` keeps one record per pull request in `<coord>/landing-measurements.jsonl`,
  built up by patches because no tool sees a whole landing: `enqueue.py queue` writes the enqueue time, each carried change's
  DONE (`done_times`: the committer time of the HEAD commit its DONE implementer report names, never the report file's time)
  and the stage times (`stage_times`, kb/Work PB3638: `dispatched`, `review_clean`; a time out of order stays null, and a
  re-enqueue after a bounce keeps the first `enqueued`), and the worktree's local gate seconds (the sum of its `verdict.json`
  `timings.wall_s`); `complete` reads `mergedAt`, the FAILED merge-group runs of the pull request (a run GitHub cancelled
  because an earlier entry failed is a ripple, not a bounce), the FAILED `pull_request` runs on its head branch
  (`pre_queue_reds`, PB3096) and the successful run's wall; and each bounce is recorded with its class (`reds`, `record_red`,
  PB4331: INFRA and RUNNER also count as the `infrastructure` cause). A field never observed stays null and prints UNMEASURED
  (a missing observation is not a zero); a bounce with no named cause prints UNATTRIBUTED. `summary` prints changes landed per
  hour, median and p90 minutes DONE → on main, bounces per landing and their causes, pull-request reds before the queue,
  merge-group CI wall, local gate minutes and the stages of the owner's fix-cycle bar (kb/Work PB3638: dispatch → enqueued
  against 30 minutes and enqueued → merged against 15, a median over its budget printed OVER). The design is judged by
  changes landed per hour and minutes per landing.

#### 3.17.6 CI and repository settings (PB3024)

- `build-and-test.yml`: the `changes` job runs the matrix on `merge_group` (base = the group's `base_sha`, so a group of
  inert-lane pull requests skips it, §3.18) and `workflow_dispatch`, only the required check (`changes`, `ci-gate`) on
  `pull_request`, and on `push` to main the already-verified short-circuit (the merge-queue commit IS the commit main advances
  to: each `push` run finds its merge group's green run on the same sha and skips the matrix). ⛔ The short-circuit counts only
  a successful run of the workflow from an event other than `pull_request`: a pull request's cheap `ci-gate` is attached to
  its HEAD commit, so a check-run lookup would read it as a verdict on the matrix. `push.branches` is `[ main ]`, and the
  DEVLOG-entry check runs only on `merge_group` (PB4330). `audits` runs `if: github.event_name == 'merge_group' ||
  needs.changes.outputs.run_matrix == 'true'`: every merge group, and any other event only when its matrix runs, never a pull
  request or a verified push. `ci-gate`'s `needs:` and its drift check (every job wired) cover every job; the `supersede` job
  (kb/Work PB3769) runs on `merge_group` only and is the one job granted `actions: write` (with `contents: read` for its
  sparse checkout and `pull-requests: read` for the queue's entries).
- A **ruleset** on `main` (classic branch protection has no merge queue, and GitHub offers a merge queue only to an
  ORGANIZATION-owned repository, so the repository is `WiseOwlSoftware/COBOL`: owner decision 2026-10-11, kb/Work PB3078)
  requires a pull request and the merge queue with merge method **merge commit**: the branch's commits stay ancestors of
  `main`, so the branch lifecycle (`prune_worktrees.py`'s landed test, `carried()`, `git cherry`) keeps working offline and
  `git log --first-parent main` reads one merge commit per landing. GitHub pairs the MERGE_MESSAGE title only with PR_TITLE
  (its three pairs are PR_TITLE+PR_BODY, PR_TITLE+BLANK and MERGE_MESSAGE+PR_TITLE), so the forensic record of a landing is the
  branch's own commits beneath the merge commit and the pull request body; `ruleset.py` refuses any pair outside GitHub's
  three before it calls GitHub. Squash was rejected (a squashed head is never an ancestor of main, so every lifecycle tool
  would key on the merged-PR record instead). Required status check `ci-gate` on the merge group, enforce for administrators
  (the owner's credentials make every push), minimum group size 1, maximum 5, check timeout 60 min.
  `scripts/orchestrator/ruleset-main.json` is the committed ruleset and `ruleset.py check|apply` compares and applies it;
  `enqueue.py queue` refuses (exit 8) before any check, push or pull request unless `ruleset.py check` reports the queue
  ACTIVE, and no other tracked file opens or queues a pull request (`LandingPipelineDriftTests`, §3.17.8).
- **The concurrent-job budget and the queue's build width (kb/Work PB3773).** The organization is on GitHub Team: standard
  hosted runners run **60 jobs at once**. `max_entries_to_build` stays **5**, measured against 60 on 2026-10-31 over the day's
  60 merge-group runs: a full group is 23 jobs with a peak of 18-19 running at once, an inert-lane group (§3.18) 13 jobs; five
  groups at once peaked at 40 running jobs, every job started within a second of its run's creation, and a full group's wall
  was 13-16 minutes, its unthrottled time. The budget holds three full groups at once; the day's landings never put more than
  two in flight. Narrow the width to 3 only if a measured day shows full groups waiting for runners; `ruleset.py check` binds
  the committed value.
- `guard` (12.8 min) was the long pole of a merge-group run; kb/Work PB4329 moved it to the nightly (owner decision, kb/Work
  PB4323) and divided the merge group's tests into weighted shards (§1.4).

#### 3.17.8 Drift tests that pin the design

`LandingPipelineDriftTests`: the workflow has a `merge_group` trigger, a `pull_request` arm that skips the matrix, and a
short-circuit that never counts a pull request's run; no tracked file outside the narrative paths (docs, kb, history, tests)
opens or queues a pull request except `enqueue.py`, and its queue guard precedes its checks, push and pull-request calls; the
self-test arms of `enqueue.py` and `ruleset.py` are declared; the direct-push landing script (both forms), the landing lease,
the train applier and the landing oracle do not exist, the push trigger is `[ main ]` with no `ci/**`, and no tracked file
outside the history paths names them; no generated index is tracked; `NoTrackedRecord_DescribesOneBranchsTree` (no committed
arch-oracle baseline and `capture_oracle.py` has no `--record`, the rule of §3.17.2 step 4). Beside it:
the ONE SHAPE audit (`gate.py --audit-shape`: no live line names a gate mode; kb/Work PB4395), `drift_rules.py --check`'s trait rules (the traits of §3.17.3), `devlog.py
--self-test`'s `lint` arm over the repository's own `DEVLOG/`, `TheMergeGroupRun_CancelsTheRunsTheQueueSuperseded_AndNothingElse`
(the `supersede` job runs on `merge_group` alone with no `needs`, runs `cancel_superseded.py` with the run's id and head
commit, is the only job with `actions: write`, has every step `continue-on-error` and is a `needs` of `ci-gate`; the arms of
`cancel_superseded.py` and `queue_facts.live_heads` are declared) and `scripts/sparse_checkout.py --self-test` (over the workflow's jobs from
the one reader `scripts/ci_workflow.py`): every job whose checkout has a `sparse-checkout` list lists
each repository file its steps name, each file its local action names and each repository module a Python script it runs
imports, transitively (a function body's lazy import is followed, kb/Work PB3810), and sets `sparse-checkout-cone-mode: false`.

### 3.18 THE INERT LANE — a change no test reads runs the audits and nothing else (kb/Work PB2878)

**Why.** Owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a full CI
run? I would think not." Of the 300 landings of 2026-10-16..23, 137 changed only paths CI's doc-only regex named, and each
still paid a full local gate. And the regex was not a classification: `docs/` and `kb/Work/` are test INPUTS, so CI skipped the
very tests that read them (kb/Work PB3603).

**The classification — ONE module, `scripts/inert_lane.py`.** A path is INERT when it lies under an `INERT` entry and under no
`READ` entry; each entry names who reads it (an audit, a person; a READ entry names the tests that read it). A change is in the
inert lane when its changed paths (`git diff --name-only --no-renames`, so a code file renamed into an inert directory shows
its old name too) are non-empty and all inert. The INERT entries are `DEVLOG/` and `kb/` (the knowledge base, THE work register
`kb/Work/` among it). NOT inert, measured on 2026-10-23 by deleting each candidate and running the three assemblies, then by the
watch below over the whole Linux population: `docs/` (SortRuntimeDocShapeDriftTests and GateLegDriftTests read every doc, and
twenty-odd classes read CONFORMANCE.md, the rule catalog or VERSION_CHANGE_REFERENCE.md); `.claude/` and the root `*.md` files;
the `tools/claude-skills` pin, which the self-tests initialize. Two sweeps moved to make `kb/` inert:
- **The conflict-marker sweep**, which read EVERY tracked file, became an audit (`scripts/conflict_markers.py --check`), which
  also closes the hole that it never ran in CI on a change whose matrix was skipped.
- **The register's cross-checks against the inventory** (every defective-verdict row claimed by a live note; every landed
  `kind: defect` note naming the rows it closed, or why none, and every row so named closed in the inventory) are
  `scripts/spec/work_inventory.py`, called by `work.py check`, so the register has ONE reader and the checks are audits: the
  gate's `WORK REGISTER` audit runs them, and CI's `audits` job in every merge group. The tree sweeps of
  `LandingPipelineDriftTests`, `LandingLaneDriftTests` and `LandingMeasureDriftTests` set `kb/` aside whole, and
  `rendered_views.py --check` (the gate's `RENDERED VIEWS` audit and a step of CI's `audits` job) replaced the Unit test that
  read the vault, so no test reads it.

**Who calls it — every runner, no list of its own.**

| Runner | Inert lane | Full lane |
|---|---|---|
| CI `changes` (`classify --base <group base or before> --head <sha>`) | `run_matrix=false`: `audits` (merge group) + `ci-gate` | the matrix |
| local gate (`gate.py`, `Host.lane`, over the measured working tree against the base) | the audits, then `… GREEN — INERT LANE (kb/Work PB2878): …`, no slot, build or test leg | the ordered gate |
| hand-run Linux tool (`linux-gate.sh`) | classifies nothing (kb/Work PB4327): it runs the legs it is asked to | every leg, under the watch |

The audits run in every lane of the local gate (they are where a register note, a citation or a DEVLOG entry is checked); CI's
`audits` job runs in every merge group, whatever `changes` decided, and on no pull request and no verified push (kb/Work
PB4330).

**The tooling flag (kb/Work PB4326).** A full-lane change also says whether it TOUCHES TOOLING: a path under `scripts/`,
`.github/`, `tools/`, `.claude/`, or `.gitattributes` / `toolchain.env`, ending `.py`, `.ps1` or `.sh` anywhere, or under a
`TOOLING_READS` entry (a path outside the scripts that a script self-test reads as data, each entry naming its readers:
`tests/_shared/TestRepo.cs`, the grammar, `.agent-fleet.json`, `docs/CONFORMANCE.md`, `tests/version-matrix/`, …). `classify`
prints `tooling=true|false`; an empty or unlistable change touches tooling, and an inert change never does. The script
self-tests read tooling and those entries, so only a change that touches one can turn one red: the gate's `SELF-TESTS` audit
(`TOOLING_ONLY`) and its post-build self-tests run only then, and CI's `windows-self-tests` job runs only then
(`needs.changes.outputs.tooling == 'true'`; the Linux self-tests of the `audits` job run in every merge group). A change that
touches no tooling saves the median gate 147 s of self-tests and 145 s of waiting for the post-build ones (PIPELINE-REVIEW,
step 3).

**The proof of the tooling flag (kb/Work PB4326, review round 1 finding R1-1).** A path prefix alone was refuted by the PASS
store's recorded inputs (§3.14.10): `tests/_shared/TestRepo.cs` is an input of `drift_rules.py`'s self-test and the `.g4` files
of `grammar_source.py`'s. `scripts/self_tests.py` therefore applies `Classification.unclassified_reads` to what every
self-test it RUNS observably read (its trace, not a guess): a self-test that passes but read a tracked path that is not
tooling, under no `TOOLING_READS` entry and under no INERT entry is RED, naming the path, until the path gets an entry naming
its reader; `inert_lane.py check` holds every entry to a tracked path. The accepted residue, named (owner 2026-10-31, PB4323: a
merge-group red may be one of five first-run classes): a DECLARED POPULATION reader (`# SELF-TEST-POPULATION: tree — <why>` in
the self-test's first lines) is not judged, since no set of paths bounds it, and the declaration is held to the trace (review
round 2, R2-1): a self-test that declares it and ran no git command over the whole repository is RED, and one that ran such a
command without declaring it is judged like any other; the `DEVLOG/` and `kb/` corpora, which every landing grows and
`devlog.py check` and `work.py check` govern in every lane; and a directory listing, which changes when a file is added and
not when a listed file is edited.

**The script self-tests run on Linux in the inert lane too: in CI (kb/Work PB3646, PB4327).** CI's `audits` job runs
`self_tests.py` on Linux in every merge group, and those self-tests are audits that read the inert paths by design (the
register, the DEVLOG, the skills submodule), so the inert watch cannot vouch for them and a `kb/` or `DEVLOG/` change can turn
them red on Linux. The Windows gate's inert lane ends after its audits and takes no slot, and the merge group is the first
Linux run of a notes-only change. `inert_lane.py --self-test` (its `wiring` arm) holds the CI job and the local lane's lack of
a Linux step.

**The tooling lane (kb/Work PB4395; PIPELINE-REVIEW §13, owner 2026-11-01 "Build the approved lane").** A change whose every
non-inert path lies in a tooling tree (`TOOLING_UNDER`: `scripts/`, `.github/`, `tools/`, `.claude/`) and under no
`CODE_READS` entry is the TOOLING LANE (`lane=tooling`): the gate joins the audits, the script self-tests among them, and ends,
with no corpus fetch, slot, build or test host (`gate.py` `_tooling`), and its record says `lane: tooling`. `CODE_READS` is
the tooling the compiler's build or tests read, each entry naming its readers: the scripts a csproj `Exec` runs, the generators
and extractors whose output a C# test checks (`gen-reserved-words.ps1`, `gen-constructs.ps1`, `scripts/spec/`, ...),
`filter_population.py` (its self-test reads the built assemblies, `SELF-TEST-NEEDS: build`), `.gitattributes` and
`toolchain.env`. A change to one is the code lane with the self-tests beside it. A script file outside the tooling trees
(`conformance-suite/harness/*.py`, a `.sh` under `tests/`) and a `TOOLING_READS` entry lie in trees the compiler reads, so they
are never the tooling lane. The proof is the watch below, which also watches every tooling-lane path, and its static twin, the
`INERT LANE` audit (`inert_lane.py check`, in every gate's audits and CI's `audits` job): every csproj `Exec` of a script,
every build-needing self-test's script and every script or directory a compiler test names through `TestRepo.<dir>(...)` with
literal arguments lies under a CODE_READS entry. The residue, named: a Windows-only compiler test that reads a tooling path
through a computed name (the watch runs on Linux only). A whole-tree CODE_READS entry names the pipeline classes still in the
compiler's test assembly that walk it; kb/Work PB4395 item 1 moves each into the self-test of the script it checks, and the
entry narrows when its last walker leaves. CI runs the whole population for a tooling-lane change (it is the regression net),
and the landing reads a tooling-lane record as it reads an inert one (`land_lane.red_facts`: no compiler test reads the
change, `inert_lane.TEST_FREE_LANES`).

**The proof — the inert watch.** A list of "what no test reads" that nobody proves drifts from the tests, and a test that
starts reading an inert path would let an inert change land untested. So every whole-assembly test run on Linux runs under
`inert_lane.py watch`: an inotify watch (through `ctypes`) of every directory an inert or tooling-lane tracked file lies in and
of every such directory itself (a submodule's whole checked-out tree), so an OPEN of a watched file or a LISTING of a watched
directory, by the test or by any process it starts, is recorded; the run is RED naming each such path (`opened: …`), and an
overflowed event queue is RED too (it proves nothing). The watched command runs with a fresh `PYTHONPYCACHEPREFIX`, so a
script a test imports is opened as source at least once per run, never served unseen from an earlier run's `__pycache__`.
CI's four Linux `dotnet test` steps run under it, and the hand-run Linux tool runs one watch over its parallel test legs (the
`inert-watch` leg) in a clone that runs those legs and nothing else, with the script self-tests and the guard in a second
clone with its own build (kb/Work PB3529). Windows has no open events, so the Windows runs carry no watch. The answer to a RED
watch is a READ entry naming the reader (the path leaves the inert lane) or making the reader an audit, and for a tooling-lane
path a CODE_READS entry naming it or making the reader a script's self-test, never deleting the watch.

**Drift test.** `inert_lane.py --self-test`'s `wiring` arm (the real tree; kb/Work PB4395 moved it out of the compiler's test
assembly, where `InertLaneDriftTests` held it): the `changes` job classifies through the module and carries no path regex;
every `dotnet test` line of a Linux job runs under the watch; `gate.py` classifies through the module and its inert and
tooling lanes take no slot and build nothing, and `linux-gate.sh` classifies nothing but watches its test legs; CI's `audits`
job runs the self-tests and `inert_lane.py check`. The `INERT LANE` audit (`inert_lane.py check`) holds every entry to a
tracked path, every READ entry under an INERT one and every tooling the build or a compiler test is seen to read under
CODE_READS. The classification's logic and the watch's firing (a read, a listing, a READ path, a tooling-lane import served
from a stale bytecode cache, a CODE_READS read, the command's own exit code) are `inert_lane.py --self-test`'s arms, run by
the self-test runner in every gate that touches tooling and in CI (the inotify arms on Linux).
`SelfTestDiscoveryDriftTests` holds both self-test legs of the Linux tool to the audit clone.

**What the lane covers.** A notes landing changes `DEVLOG/` and `kb/Work/` (107 of the 137 landings above changed only those),
so it is the inert lane: the audits and nothing else. A change to `docs/`, a skill or a brief is still the full lane until its
readers become audits or gain READ entries, one measured path at a time (an INERT entry is added only with a GREEN Linux watch
run: `bash scripts/linux-gate.sh --nice`, its `inert-watch` leg).

---

## 4. Standing risks
1. **Snapshot brittleness.** Emitted-C# snapshots (gate 3) diff on almost every emitter refactor. Gate (3) is reviewed, never
   a hard red on its own while gates (1) and (2) are green, but a gate-(3) diff with NO corresponding source change IS a red
   (unexpected drift). The characterization corpus stays small and representative.
2. **A Linux-only compiler red is found in the queue** (§3.15's accepted risk), classified and re-queued or finished by the
   landing lane (§0 item 6).
3. **A merge-group red is one of five first-run classes, a flake, or infrastructure** (§0 item 5). A flake is a kb/Work defect
   fixed at its root, never retried inside the run; kb/Work PB1957 records each class's differences and rate.
4. **CI time grows with the suite.** The growth trigger (§3.14.13) decides when the program differential is built.
